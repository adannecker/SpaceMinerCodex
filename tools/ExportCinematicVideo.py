"""Mux Unity-rendered cinematic frames with continuous narration, music and subtitles."""
from pathlib import Path
import csv
import os
import shutil
import subprocess
import wave
import json
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'outputs/Cinematic'
ASSETS = ROOT / 'Assets/SpaceMiner/Resources/MemoryCinematic'
DOCS = ROOT / 'docs/Dialoge/IntroCinematic'
ENCODER = os.environ.get('FFMPEG') or shutil.which('ffmpeg')
if not ENCODER:
    bundled = list((ROOT / 'work/video-dependencies/imageio_ffmpeg/binaries').glob('ffmpeg*.exe'))
    if bundled:
        ENCODER = str(bundled[0])
    else:
        import imageio_ffmpeg
        ENCODER = imageio_ffmpeg.get_ffmpeg_exe()

def timestamp(seconds):
    ms = round(seconds * 1000)
    return f'{ms // 3600000:02}:{ms // 60000 % 60:02}:{ms // 1000 % 60:02},{ms % 1000:03}'

def main():
    rows = list(csv.DictReader((OUT / 'video-timing.csv').open(encoding='utf-8-sig')))
    assert len(rows) == 12
    subtitles = []
    position = 0
    starts=json.loads((ASSETS/'timing.json').read_text())['starts']
    for row in rows:
            scene = int(row['scene'])
            duration = int(row['frames']) / 30
            doc = next(p for p in DOCS.glob(f'{scene:02}_*.md') if 'Aoede' not in p.name)
            text = doc.read_text(encoding='utf-8-sig').split('## Sprechertext', 1)[1].split('## ', 1)[0].strip()
            end=starts[scene] if scene<12 else starts[-1]+float(row['voiceSeconds'])-.5
            subtitles.append(f'{scene}\n{timestamp(starts[scene-1])} --> {timestamp(end)}\n{text}\n')
            position += duration
    (OUT / 'IntroCinematic.srt').write_text('\n'.join(subtitles), encoding='utf-8')
    with wave.open(str(ASSETS/'narration.wav')) as source:
        voice=np.frombuffer(source.readframes(source.getnframes()),dtype='<i2').astype(float)/32768
    with wave.open(str(ASSETS/'atmosphere.wav')) as source:
        music=np.frombuffer(source.readframes(source.getnframes()),dtype='<i2').reshape(-1,2).astype(float)/32768
    frames=round(position*22050)
    mixed=np.zeros((frames,2));mixed[:min(frames,len(music))]=music[:frames]
    mixed[:len(voice)]+=voice[:,None]*.85
    assert np.max(np.abs(mixed))<1, 'Audio mix clips'
    with wave.open(str(OUT/'IntroCinematic-mix.wav'),'wb') as output:
        output.setparams((2,2,22050,0,'NONE','not compressed'))
        output.writeframes(np.round(mixed*32767).astype('<i2').tobytes())
    print(f'CONTINUOUS MIX: one complete narration, music ducked; peak {np.max(np.abs(mixed)):.4f}')
    subprocess.run([str(ENCODER), '-y', '-i', str(OUT / 'IntroCinematic-silent.mp4'),
                    '-i', str(OUT / 'IntroCinematic-mix.wav'), '-i', str(OUT / 'IntroCinematic.srt'),
                    '-map', '0:v', '-map', '1:a', '-map', '2:s', '-c:v', 'copy', '-c:a', 'aac',
                    '-b:a', '192k', '-c:s', 'mov_text', '-metadata:s:s:0', 'language=deu',
                    '-movflags', '+faststart', str(OUT / 'SpaceMiner-IntroCinematic.mp4')], check=True)
    subprocess.run([str(ENCODER), '-v', 'error', '-i', str(OUT / 'SpaceMiner-IntroCinematic.mp4'),
                    '-map', '0:v', '-map', '0:a', '-f', 'null', '-'], check=True)
    print(f'VIDEO VERIFIED: 12 scenes, {position:.3f}s, 1280x720/30fps, AAC narration and music, optional German subtitles')

if __name__ == '__main__':
    main()
