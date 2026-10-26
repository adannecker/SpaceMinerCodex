"""Generate separately checkpointed Mira recordings in an authorized Google Cloud Shell.

Uses the existing gcloud login; never creates or writes credentials. No automatic
retry after an uncertain request: inspect its pending marker before resuming.
"""
import argparse
import base64
import hashlib
import io
import json
from pathlib import Path
import subprocess
import urllib.error
import urllib.request
import wave


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--catalog', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--project', required=True)
    parser.add_argument('--limit', type=int, default=0)
    args = parser.parse_args()
    catalog = json.loads(args.catalog.read_text(encoding='utf-8-sig'))
    style = ('Du bist Mira, der weibliche Bordcomputer einer beschädigten Raumstation. '
             'Sprich auf Deutsch sanft, warm, angenehm und ruhig, mit natürlicher klarer '
             'Aussprache und einer vollen weichen Stimme. Du sprichst persönlich zu einem '
             'Menschen und duzt ihn. Vermittle behutsam Sicherheit, ohne die ernste Situation '
             'zu verharmlosen. Natürliches mittleres Sprechtempo, kurze sinnvolle Pausen. '
             'Arbeitsanweisungen verständlich und konzentriert, Erfolge dezent ermutigend. '
             'Kein Werbeton, keine epische Chronistenstimme, kein Flüstern oder dramatischer '
             'Alarm. Lies ausschließlich den Sprechertext; keine zusätzlichen Worte, '
             'Musik oder Geräusche. Trockene klare Sprachaufnahme ohne Hall oder Echo.')
    args.output.mkdir(parents=True, exist_ok=True)
    manifest_path = args.output / 'recordings.json'
    manifest = json.loads(manifest_path.read_text()) if manifest_path.exists() else {
        'Voice': 'Aoede', 'Model': 'gemini-3.1-flash-tts-preview',
        'Language': 'de-DE', 'Style': style, 'Recordings': []}
    completed = {entry['Id']: entry for entry in manifest['Recordings']}
    count = 0
    for entry in catalog['Dialogue']:
        ident, text = entry['Id'], entry['Text']
        assert ident.replace('_', '').isalnum()
        target = args.output / (ident + '.wav')
        text_hash = hashlib.sha256(text.encode()).hexdigest()
        if ident in completed:
            previous = completed[ident]
            assert previous['TextSHA256'] == text_hash
            assert hashlib.sha256(target.read_bytes()).hexdigest() == previous['SHA256']
            print('SKIP', ident, flush=True)
            continue
        pending = args.output / (ident + '.pending')
        if pending.exists() or target.exists():
            raise RuntimeError('Uncertain prior request; inspect before retrying: ' + ident)
        pending.write_text(text_hash, encoding='ascii')
        token = subprocess.run(['gcloud', 'auth', 'print-access-token'], check=True,
                               capture_output=True, text=True).stdout.strip()
        body = {'input': {'text': text, 'prompt': style},
                'voice': {'languageCode': 'de-DE', 'name': 'Aoede',
                          'modelName': manifest['Model']},
                'audioConfig': {'audioEncoding': 'LINEAR16', 'sampleRateHertz': 22050,
                                'speakingRate': 1.0, 'volumeGainDb': 0}}
        request = urllib.request.Request('https://texttospeech.googleapis.com/v1/text:synthesize',
            data=json.dumps(body).encode(), headers={'Authorization': 'Bearer ' + token,
            'x-goog-user-project': args.project, 'Content-Type': 'application/json'})
        print('REQUEST', ident, len(text), 'characters', flush=True)
        try:
            with urllib.request.urlopen(request, timeout=240) as response:
                payload = json.load(response)
        except urllib.error.HTTPError as error:
            details = json.loads(error.read().decode()).get('error', {})
            print('API_ERROR', error.code, details.get('status'), details.get('message'), flush=True)
            raise SystemExit(2)
        finally:
            token = None
        audio = base64.b64decode(payload['audioContent'], validate=True)
        with wave.open(io.BytesIO(audio)) as wav:
            assert wav.getnchannels() == 1 and wav.getsampwidth() == 2
            duration = wav.getnframes() / wav.getframerate()
            rate = wav.getframerate()
            assert .5 < duration < 180
        with target.open('xb') as stream:
            stream.write(audio)
        (args.output / (ident + '.txt')).write_text(text + '\n', encoding='utf-8')
        manifest['Recordings'].append({'Id': ident, 'Text': text, 'TextSHA256': text_hash,
            'Bytes': len(audio), 'SHA256': hashlib.sha256(audio).hexdigest(),
            'DurationSeconds': duration, 'SampleRate': rate})
        manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
        pending.unlink()
        print('SAVED', ident, round(duration, 3), 'seconds', hashlib.sha256(audio).hexdigest(), flush=True)
        count += 1
        if args.limit and count >= args.limit:
            break
    print('READY', len(manifest['Recordings']), 'of', len(catalog['Dialogue']), flush=True)


if __name__ == '__main__':
    main()
