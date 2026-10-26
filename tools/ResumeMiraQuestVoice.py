"""Resume a checkpointed Google batch; retry only explicitly rejected TTS requests.

Uncertain network failures remain pending. At most two retries per text; quota,
authentication and billing errors stop immediately. Existing WAVs are skipped.
"""
import argparse
from pathlib import Path
import re
import subprocess
import sys
import time


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--catalog', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--project', required=True)
    args = parser.parse_args()
    command = [sys.executable, str(Path(__file__).with_name('GenerateMiraQuestVoice.py')),
               '--catalog', str(args.catalog), '--output', str(args.output), '--project', args.project]
    while True:
        output = []
        with subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True) as process:
            for line in process.stdout:
                output.append(line); print(line, end='', flush=True)
            code = process.wait()
        if code == 0:
            return
        message = ''.join(output)
        requests = re.findall(r'^REQUEST (\w+) ', message, re.MULTILINE)
        if code != 2 or not requests or not re.search(r'^API_ERROR (400|500|503|504) ', message, re.MULTILINE):
            raise SystemExit(code)
        ident = requests[-1]
        previous = list(args.output.glob(ident + '.http_error_*'))
        if len(previous) >= 2:
            print('STOP: repeated rejected request', ident, flush=True)
            raise SystemExit(code)
        pending = args.output / (ident + '.pending')
        pending.rename(args.output / (ident + '.http_error_' + str(len(previous) + 1).zfill(2)))
        print('RETRY_AFTER_REJECTION', ident, 'in 20 seconds', flush=True)
        time.sleep(20)


if __name__ == '__main__':
    main()
