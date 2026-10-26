"""Read-only audit of Git transfer candidates; never prints potential secret values.

Run before committing: python tools/AuditProjectTransfer.py
Generated report stays under ignored Logs/. Builds, caches and local credentials
are deliberately outside the source transfer. No network or Git mutation.
"""
from __future__ import annotations

import json
import re
import subprocess
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TEXT_EXTENSIONS = {
    '.md', '.txt', '.json', '.py', '.ps1', '.cs', '.yaml', '.yml', '.mat',
    '.meta', '.shader', '.uss', '.uxml', '.html', '.js', '.cjs', '.cmd',
    '.log', '.toml', '.csv', '.srt', '.xml', '.gitattributes', '.gitignore',
}
SECRET_PATTERNS = {
    'google-api-key': re.compile(r'AIza[0-9A-Za-z_-]{35}'),
    'openai-api-key': re.compile(r'\bsk-(?:proj-|svcacct-)?[A-Za-z0-9_-]{32,}'),
    'github-token': re.compile(r'\b(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{30,})'),
    'private-key': re.compile(r'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----'),
    'literal-bearer': re.compile(r'(?i)Bearer\s+[A-Za-z0-9_.-]{32,}'),
    'signed-download-url': re.compile(r'(?i)(?:X-Amz-Signature|X-Goog-Signature|AWSAccessKeyId)=([A-Za-z0-9%/+_-]{12,})'),
}


def git_paths(*arguments: str) -> list[str]:
    result = subprocess.run(['git', *arguments, '-z'], cwd=ROOT, check=True, capture_output=True)
    return [p.decode('utf-8') for p in result.stdout.split(b'\0') if p]


def main() -> int:
    candidates = sorted(set(git_paths('ls-files', '--cached', '--others', '--exclude-standard')))
    files = [(name, ROOT / name) for name in candidates if (ROOT / name).is_file()]
    sizes = [(name, path.stat().st_size) for name, path in files]
    forbidden_files = []
    findings = []
    for name, path in files:
        lower = path.name.lower()
        if lower in {'.env', '.env.local', 'credentials.json', 'token.json', 'cookies.json', 'cookies.txt'} or path.suffix.lower() in {'.pfx', '.p12', '.pem', '.key'}:
            forbidden_files.append(name)
        if path.suffix.lower() not in TEXT_EXTENSIONS and lower not in {'.gitignore', '.gitattributes'}:
            continue
        if path.stat().st_size > 12 * 1024 * 1024:
            continue
        content = path.read_text(encoding='utf-8-sig', errors='replace')
        for kind, pattern in SECRET_PATTERNS.items():
            lines = [number for number, line in enumerate(content.splitlines(), 1) if pattern.search(line)]
            if lines:
                findings.append({'path': name, 'kind': kind, 'lines': lines})
    oversize = [{'path': name, 'bytes': size} for name, size in sizes if size >= 100 * 1024 * 1024]
    large = [{'path': name, 'bytes': size} for name, size in sizes if size >= 50 * 1024 * 1024]
    groups = Counter()
    for name, size in sizes:
        groups[name.split('/')[0]] += size
    report = {
        'scope': 'Git tracked and non-ignored untracked regular files; values of matches omitted',
        'files': len(files), 'bytes': sum(size for _, size in sizes),
        'groupsBytes': dict(groups.most_common()), 'largeFiles': large,
        'githubSizeBlockers': oversize, 'credentialFileNames': forbidden_files,
        'secretLikeMatches': findings,
        'passed': not (oversize or forbidden_files or findings),
    }
    out = ROOT / 'Logs' / 'TransferAudit' / 'candidate-audit.json'
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(report, ensure_ascii=False, indent=2))
    return 0 if report['passed'] else 1


if __name__ == '__main__':
    raise SystemExit(main())
