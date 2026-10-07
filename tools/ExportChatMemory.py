"""Export only explicitly registered project conversations; never tool/reasoning data."""
import argparse
import json
import os
import re
from datetime import datetime, timezone
from pathlib import Path

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--sessions', type=Path, default=Path(os.environ.get('CODEX_HOME', str(Path.home() / '.codex'))) / 'sessions')
args = parser.parse_args()
register = json.loads((root / 'docs/Chats/chat-register.json').read_text(encoding='utf-8-sig'))
paths = list(args.sessions.rglob('*.jsonl'))
stamp = datetime.now(timezone.utc).isoformat()
index = ['# Projektchats und gemeinsames Chatmemory', '', f'Exportstand: {stamp}.', '',
         'Lesbare Momentaufnahmen, kein importierbares Codex-Sitzungsformat. Exportiert werden ausschließlich Nutzer- und Assistententexte der ausdrücklich registrierten Projektchats. Systemanweisungen, interne Überlegungen, Werkzeugprotokolle und Umgebungskontext werden ausgelassen. Anhänge werden nicht rekonstruiert; übernommene Assets liegen im Projekt.', '',
         'Aktueller Stand: [Projektmemory](../Projektmemory.md), [Projektübergabe](../Projektuebergabe.md). Historische Aussagen und alte Pfade können überholt sein. Chat-IDs dienen der Zuordnung auf dem Ursprungsrechner und stellen auf einem anderen Rechner keine Sitzungen her.', '',
         '## Ursprüngliche Archive vom 05.10.2026', '',
         '- [Unity-Spiel gemeinsam entwickeln](01-Unity-Spiel.md)',
         '- [Asteroidenvarianten entwerfen](02-Asteroiden.md)',
         '- [Story, Dialoge & Bordcomputer](03-Story-und-Mira.md)', '',
         'Startaufträge für die neun aktuellen Themenchats: [Chats am anderen Rechner anlegen](../Rechnerwechsel-Memory.md#startaufträge-für-alle-neun-themenchats). Vorhandene Themenchats dort fortsetzen; nur fehlende neu anlegen.', '',
         '## Registrierte Archive und Exportverfügbarkeit', '']
report = []
for thread in register['threads']:
    if thread.get('source') == 'cloud_snapshot':
        available = (root / 'docs/Chats' / thread['file']).is_file()
        index.append(f"- [{thread['title']}]({thread['file']}) — historische Cloud-Textmomentaufnahme vom {thread.get('snapshot_date', 'unbekannt')}; Anhänge nicht enthalten. {thread['topic']}." if available else f"- **{thread['title']}**: Cloud-Archiv fehlt.")
        continue
    matches = [p for p in paths if p.name.endswith(thread['id'] + '.jsonl')]
    if len(matches) != 1:
        saved = root / 'docs/Chats' / thread['file']
        status = 'preserved' if saved.is_file() else 'unavailable'
        if saved.is_file():
            index.append(f"- [{thread['title']}]({thread['file']}) — vorhandener Export bewahrt; Ursprungssitzung auf diesem Rechner nicht eindeutig verfügbar. {thread['topic']}.")
        else:
            index.append(f"- **{thread['title']}** (`{thread['id']}`): weder lokaler Sitzungsexport noch bestehendes Textarchiv verfügbar. {thread['topic']}.")
        report.append({'id': thread['id'], 'title': thread['title'], 'file': thread['file'], 'status': status})
        continue
    lines = matches[0].read_text(encoding='utf-8').splitlines()
    meta = json.loads(lines[0])
    if meta.get('payload', {}).get('id') != thread['id']:
        raise ValueError('Session identity mismatch')
    archive = [f"# {thread['title']}", '', f"Chat-ID: `{thread['id']}`. Exportstand: {stamp}.", '', thread['topic'], '']
    count = 0
    for line in lines:
        try:
            entry = json.loads(line)
        except json.JSONDecodeError:
            continue
        msg = entry.get('payload', {})
        if entry.get('type') != 'response_item' or msg.get('type') != 'message' or msg.get('role') not in ('user', 'assistant'):
            continue
        if msg.get('channel') == 'analysis':
            continue
        body = '\n'.join(p.get('text', '') for p in msg.get('content', []) if p.get('type') in ('input_text', 'output_text', 'text'))
        for tag in ('environment_context', 'in-app-browser-context', 'external_codex_apps_open_page'):
            body = re.sub(r'(?s)<' + tag + r'\b[^>]*>.*?</' + tag + '>', '', body)
        if body.lstrip().startswith('# AGENTS.md instructions for '):
            continue
        body = '\n'.join(line.rstrip() for line in body.strip().splitlines())
        if not body:
            continue
        count += 1
        archive.extend([f"## {'Nutzer' if msg['role'] == 'user' else 'Assistent'} · {entry.get('timestamp', '')}", '', body, ''])
    (root / 'docs/Chats' / thread['file']).write_text('\n'.join(archive).rstrip() + '\n', encoding='utf-8')
    index.append(f"- [{thread['title']}]({thread['file']}) — {count} Textnachrichten. {thread['topic']}.")
    report.append({'id': thread['id'], 'title': thread['title'], 'file': thread['file'], 'status': 'exported', 'messages': count})
    print(f"{thread['title']}: {count}")
index += ['', '## Aktualisieren', '', 'Mit einer lokalen Python-Installation: `python tools/ExportChatMemory.py`. Optional `--sessions <lokaler Sitzungsordner>`. Neue bestätigte Projektchats zuerst in `chat-register.json` aufnehmen. Die drei ursprünglichen Archive werden dabei bewahrt. `ExportProjectChats.ps1` ist der ältere Exporter ausschließlich für die drei Ursprungssitzungen und überschreibt diese Übersicht; hierfür nicht verwenden.', '']
(root / 'docs/Chats/README.md').write_text('\n'.join(index), encoding='utf-8')
(root / 'docs/Chats/export-status.json').write_text(json.dumps({'exported_at': stamp, 'threads': report}, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print('Exportstatus: ' + ', '.join(f"{status}={sum(t['status'] == status for t in report)}" for status in ('exported', 'preserved', 'unavailable')))
