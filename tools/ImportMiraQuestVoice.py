"""Validate a Cloud Shell Mira export and install the exact reviewed quest recordings."""
import argparse
from array import array
import hashlib
import io
import json
from pathlib import Path
import shutil
import uuid
import wave
import zipfile


def sha(data):
    return hashlib.sha256(data).hexdigest()


def meta(path):
    target = Path(str(path) + '.meta')
    if not target.exists():
        target.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '\n', encoding='ascii')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('archive', type=Path)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    draft = json.loads((root / 'docs/Dialoge/02_Aufbauphase_Questdialoge.json').read_text(encoding='utf-8'))
    texts = {line['Id']: line['Text'] for line in draft['Dialogue']}
    with zipfile.ZipFile(args.archive) as archive:
        names = set(archive.namelist())
        expected = {'recordings.json'} | {ident + suffix for ident in texts for suffix in ('.wav', '.txt')}
        assert names == expected, (names - expected, expected - names)
        assert archive.testzip() is None
        manifest = json.loads(archive.read('recordings.json'))
        records = manifest['Recordings']
        assert len(records) == len(texts) and {r['Id'] for r in records} == set(texts)
        prepared = []
        for record in records:
            ident = record['Id']
            assert record['Text'] == texts[ident]
            assert record['TextSHA256'] == sha(texts[ident].encode())
            assert archive.read(ident + '.txt').decode().strip() == texts[ident]
            data = archive.read(ident + '.wav')
            assert len(data) == record['Bytes'] and sha(data) == record['SHA256']
            with wave.open(io.BytesIO(data)) as wav:
                assert (wav.getnchannels(), wav.getsampwidth(), wav.getframerate()) == (1, 2, 22050)
                duration = wav.getnframes() / wav.getframerate()
                assert abs(duration - record['DurationSeconds']) < .0001
                assert .5 < duration < 180
                samples = array('h', wav.readframes(wav.getnframes()))
                assert max(abs(sample) for sample in samples) > 500, ident + ': silent recording'
                assert sum(abs(sample) >= 32760 for sample in samples) / len(samples) < .01, ident + ': clipped recording'
                assert .8 < len(texts[ident].split()) / duration < 4.5, ident + ': implausible spoken length'
            prepared.append((ident, data))
    # All input validation precedes any runtime asset write.
    destination = root / 'Assets/SpaceMiner/Resources/MiraQuests'
    for ident, data in prepared:
        target = destination / (ident + '.wav')
        if target.exists() and target.read_bytes() != data:
            raise RuntimeError('Existing different recording must be preserved/reviewed: ' + str(target))
    source = root / 'docs/Dialoge/Sprachproben/Quests_Aoede_2026-10-26'
    source.mkdir(parents=True, exist_ok=True)
    saved = source / args.archive.name
    if args.archive.resolve() != saved.resolve():
        shutil.copy2(args.archive, saved)
    (source / 'recordings.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    for ident, data in prepared:
        target = destination / (ident + '.wav')
        target.write_bytes(data)
        meta(target)
        (source / (ident + '.txt')).write_text(texts[ident] + '\n', encoding='utf-8')
    # A portable full WAV per quest: original introduction, short pause, original success.
    complete_folder = source / 'Komplette_Quests'
    complete_folder.mkdir(exist_ok=True)
    audio = dict(prepared)
    complete = []
    for quest in range(9):
        ident = 'quest_' + str(quest).zfill(2)
        path = complete_folder / (ident + '_komplett.wav')
        with wave.open(str(path), 'wb') as out:
            out.setnchannels(1); out.setsampwidth(2); out.setframerate(22050)
            for index, suffix in enumerate(('_start', '_complete')):
                if index: out.writeframes(bytes(round(.75 * 22050) * 2))
                with wave.open(io.BytesIO(audio[ident + suffix])) as clip:
                    out.writeframes(clip.readframes(clip.getnframes()))
        complete.append({'Id': ident, 'File': str(path.relative_to(root)), 'SHA256': sha(path.read_bytes())})
    (source / 'complete-quests.json').write_text(json.dumps(complete, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print('Validated and installed', len(prepared), 'recordings; saved', len(complete), 'complete quest WAVs.')


if __name__ == '__main__':
    main()
