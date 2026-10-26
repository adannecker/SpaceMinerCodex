"""Generate offline German lip timings from the *assigned* intro recordings.

Download Rhubarb from https://github.com/DanielSWolf/rhubarb-lip-sync/releases
and pass its executable using --rhubarb. No network service or runtime dependency.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
import uuid
import wave


def meta(path):
    target = Path(str(path) + '.meta')
    if not target.exists():
        target.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '\n', encoding='ascii')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--rhubarb', type=Path, required=True)
    parser.add_argument('--clips', nargs='+', help='Only regenerate these assigned clip names, e.g. mira_12 mira_13')
    parser.add_argument('--quests', action='store_true', help='Also include assigned quest and workflow recordings')
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    resources = root / 'Assets/SpaceMiner/Resources'
    destination = resources / 'MiraVisemes'
    destination.mkdir(exist_ok=True)
    meta(destination)
    process = subprocess.run([str(args.rhubarb.resolve()), '--version'], text=True, capture_output=True, check=True)
    version = (process.stdout + process.stderr).strip()
    cues = json.loads((resources / 'Intro/intro.json').read_text(encoding='utf-8-sig'))['Cues']
    if args.quests:
        catalog = json.loads((resources / 'MiraQuests/catalog.json').read_text(encoding='utf-8-sig'))
        cues += [cue for quest in catalog['Quests'] for cue in quest['Cues']] + catalog['Helpers']
    assigned = [cue for cue in cues if cue.get('Audio')]
    if args.clips:
        requested = set(args.clips)
        known = {Path(cue['Audio']).stem for cue in assigned}
        if requested - known:
            parser.error('Clips are not assigned in intro.json: ' + ', '.join(sorted(requested - known)))
        assigned = [cue for cue in assigned if Path(cue['Audio']).stem in requested]
    report = []
    for cue in assigned:
        source = resources / (cue['Audio'] + '.wav')
        with wave.open(str(source)) as wav:
            duration = wav.getnframes() / wav.getframerate()
        with tempfile.TemporaryDirectory(prefix='mira-viseme-') as temporary:
            output = Path(temporary) / 'timing.json'
            subprocess.run([str(args.rhubarb.resolve()), '-r', 'phonetic', '-f', 'json', '--threads', '2',
                            '--quiet', '-o', str(output), str(source)], check=True)
            result = json.loads(output.read_text(encoding='utf-8-sig'))
        shapes = result['mouthCues']
        end = 0.
        for shape in shapes:
            assert shape['value'] in 'ABCDEFGHX', shape
            assert abs(shape['start'] - end) < .001 and shape['end'] > shape['start'], shape
            end = shape['end']
        assert shapes[0]['start'] == 0 and abs(end-duration) <= .011, (end, duration)
        data = dict(generator=version, recognizer='phonetic', source=cue['Audio'],
                    audioSha256=hashlib.sha256(source.read_bytes()).hexdigest(),
                    duration=duration, mouthCues=shapes)
        target = destination / (source.stem + '.json')
        target.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
        meta(target)
        report.append(dict(clip=source.stem, cues=len(shapes), duration=duration))
        print(source.stem, len(shapes), 'mouth cues', flush=True)
    assert len(report) == len(assigned)
    print(json.dumps(dict(generator=version, recordings=report), indent=2))


if __name__ == '__main__':
    main()
