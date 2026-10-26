"""Export reviewed visible project prose; never raw sessions or internal messages.

A register export_review.through gates each source. MainDev's curated prefix is
retained, with only the reviewed continuation reproducibly regenerated.
"""
import argparse
import hashlib
import json
import os
import re
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from urllib.parse import parse_qsl, urlsplit, urlunsplit

ROOT = Path(__file__).resolve().parents[1]
CONTINUATION = '<!-- reviewed-project-continuation -->'
TAGS = ('environment_context', 'in-app-browser-context', 'external_codex_apps_open_page',
        'external_codex_apps_writing_block_edits', 'app-context', 'oai-mem-citation')
URL = re.compile(r'https?://[^\s<>"\[\]`]+')
EMAIL = re.compile(r'(?<![\w.-])[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}')
SECRET = re.compile(r'\b(?:sk-[A-Za-z0-9_-]{20,}|gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{20,}|AIza[A-Za-z0-9_-]{30,}|AKIA[A-Z0-9]{16})\b')
SENSITIVE_QUERY = re.compile(r'(?i)(token|signature|credential|password|secret|authorization|api.?key|x-amz-|x-goog-|googleaccessid|access.?key|^sig$|^se$|^sp$|^sv$)')


def timestamp(value):
    return datetime.fromisoformat(value.replace('Z', '+00:00'))


def clean_text(body, audit):
    if body.lstrip().startswith('# AGENTS.md instructions for '):
        audit['instruction_messages_omitted'] += 1
        return ''
    for tag in TAGS:
        body, n = re.subn(r'(?s)<' + re.escape(tag) + r'\b[^>]*>.*?</' + re.escape(tag) + '>', '', body)
        audit['context_blocks_omitted'] += n
    def answer(match):
        try:
            replies = json.loads(match.group(1))
            audit['answer_envelopes_simplified'] += 1
            return '\n'.join('Frage: ' + str(r.get('question', '')) + '\nAntwort: ' + str(r.get('answer', '')) for r in replies)
        except (ValueError, TypeError, AttributeError):
            audit['unreadable_answer_envelopes'] += 1
            return '[Antwortumschlag nicht rekonstruierbar.]'
    body = re.sub(r'(?s)<send_user_message_question_reply>\s*(.*?)\s*</send_user_message_question_reply>', answer, body)
    if body.lstrip().startswith(('# Files mentioned by the user:', '# Files pasted by the user:')) and '## My request:' in body:
        body = '[Anhänge im Ursprungschat; nicht rekonstruiert.]\n\n' + body.split('## My request:', 1)[1].strip()
        audit['attachment_preambles_omitted'] += 1
    body, n = re.subn(r'(?s)<image\b[^>]*>.*?</image>', '[Bildanhang im Ursprungschat; nicht rekonstruiert.]', body)
    audit['attachment_envelopes_omitted'] += n
    def safe_url(match):
        value = match.group(0)
        try:
            parts = urlsplit(value)
            if parts.username or parts.password:
                audit['credential_urls_redacted'] += 1
                return '[URL mit Zugangsdaten entfernt]'
            if any(SENSITIVE_QUERY.search(key) for key, _ in parse_qsl(parts.query, keep_blank_values=True)):
                audit['signed_url_queries_redacted'] += 1
                return urlunsplit((parts.scheme, parts.netloc, parts.path, '', '')) + ' [temporäre Zugangsparameter entfernt]'
        except ValueError:
            audit['invalid_urls_omitted'] += 1
            return '[URL ausgelassen]'
        return value
    body = URL.sub(safe_url, body)
    body, n = EMAIL.subn('[E-Mail-Adresse entfernt]', body)
    audit['email_addresses_redacted'] += n
    body, n = SECRET.subn('[Zugangsschlüssel entfernt]', body)
    audit['credential_strings_redacted'] += n
    body, n = re.subn(r'(?i)\bBearer\s+[A-Za-z0-9._~+/-]+=*', '[Autorisierung entfernt]', body)
    audit['bearer_strings_redacted'] += n
    return '\n'.join(line.rstrip() for line in body.strip().splitlines()).strip()


def read_messages(path, thread):
    review = thread['export_review']
    through = timestamp(review['through'])
    after = timestamp(review['after']) if review.get('after') else None
    audit, messages = Counter(), []
    identity_checked = False
    for line in path.open(encoding='utf-8'):
        try:
            entry = json.loads(line)
        except json.JSONDecodeError:
            audit['malformed_records_omitted'] += 1
            continue
        msg = entry.get('payload', {})
        if not identity_checked:
            if entry.get('type') != 'session_meta' or msg.get('id') != thread['id']:
                raise ValueError('Session identity mismatch: ' + thread['id'])
            identity_checked = True
        if entry.get('type') != 'response_item' or msg.get('type') != 'message':
            continue
        role = msg.get('role')
        if role not in ('user', 'assistant'):
            audit['nonvisible_roles_omitted'] += 1
            continue
        if role == 'assistant' and (msg.get('channel') not in (None, 'final', 'commentary') or msg.get('recipient') not in (None, 'all')):
            audit['internal_or_tool_messages_omitted'] += 1
            continue
        stamp = entry.get('timestamp')
        if not stamp:
            audit['undated_messages_omitted'] += 1
            continue
        when = timestamp(stamp)
        if when > through:
            audit['after_review_messages_omitted'] += 1
            continue
        if after and when <= after:
            audit['curated_prefix_raw_messages_omitted'] += 1
            continue
        if stamp in review.get('omit_timestamps', []):
            audit['review_exclusions'] += 1
            continue
        body = '\n'.join(p.get('text', '') for p in msg.get('content', []) if p.get('type') in ('input_text', 'output_text', 'text'))
        body = clean_text(body, audit)
        if body:
            messages.append((role, stamp, body))
        else:
            audit['empty_messages_omitted'] += 1
    if not identity_checked:
        raise ValueError('Empty source: ' + thread['id'])
    return messages, dict(sorted((k, v) for k, v in audit.items() if v))


def render_messages(messages):
    lines = []
    for role, stamp, body in messages:
        lines += [f"## {'Nutzer' if role == 'user' else 'Assistent'} · {stamp}", '', body, '']
    return '\n'.join(lines).rstrip() + '\n'


def export(root, session_dirs, stamp):
    folder = root / 'docs/Chats'
    register = json.loads((folder / 'chat-register.json').read_text(encoding='utf-8-sig'))
    sources = {t['id']: [] for t in register['threads']}
    for directory in session_dirs:
        if directory.is_dir():
            for path in directory.rglob('*.jsonl'):
                for identity in sources:
                    if path.name.endswith(identity + '.jsonl') and path not in sources[identity]:
                        sources[identity].append(path)
    index = ['# Projektchats und gemeinsames Chatmemory', '', f'Exportstand: {stamp}.', '',
        'Lesbare Projektmomentaufnahmen, kein importierbares Codex-Sitzungsformat. Nur registrierte und bis zum festgehaltenen Zeitpunkt geprüfte Nutzer-/Assistententexte. System-, Analyse-, Werkzeug- und Umgebungstexte werden ausgelassen. Persönliche E-Mail-Adressen und erkannte Zugangsschlüssel/temporäre URL-Parameter werden bereinigt. Anhänge werden nicht rekonstruiert; übernommene Assets liegen im Projekt.', '',
        'Aktuell: [Projektmemory](../Projektmemory.md), [Projektübergabe](../Projektuebergabe.md). Themenabgleich und Konflikte: [Chat-Auswertung vom 26.10.2026](../Chat-Auswertung-2026-10-26.md). Historische Tests/Pfade sind keine heutige Abnahme. Chat-IDs erzeugen auf einem anderen Rechner keine Sitzungen.', '',
        'Aktueller Laptop-Kontext, umbenannte und neue Themenchats: [Kontextupdate vom 26.10.2026](Kontextupdate-2026-10-26.md). [Lokaler Projektabgleich](../Projektabgleich-Laptop-2026-10-26.md) erklärt den erhaltenen Unity-Versionsstand und die hiesige Erreichbarkeit; ältere Exportzahlen gelten für ihren jeweiligen Rechner.', '',
        '## Ursprüngliche Archive vom 05.10.2026', '',
        '- [Unity-Spiel gemeinsam entwickeln](01-Unity-Spiel.md)',
        '- [Asteroidenvarianten entwerfen](02-Asteroiden.md)',
        '- [Story, Dialoge & Bordcomputer](03-Story-und-Mira.md)', '',
        'Die drei Ursprungsarchive bleiben erhalten. Startaufträge: [neun aktuelle Themenchats](../Rechnerwechsel-Memory.md#startaufträge-für-alle-neun-themenchats). Bestehende fortsetzen; nur fehlende ausdrücklich neu anlegen.', '',
        '## Registrierte Archive und Exportverfügbarkeit', '']
    report = []
    for thread in register['threads']:
        saved = folder / thread['file']
        row = {k: thread[k] for k in ('id', 'title', 'file')}
        matches = sources[thread['id']]
        if thread.get('source') == 'cloud_snapshot':
            row['status'] = 'cloud_snapshot_preserved' if saved.is_file() else 'unavailable'
            row['source_snapshot_date'] = thread.get('snapshot_date')
            detail = f"Cloud-Textmomentaufnahme vom {thread.get('snapshot_date', 'unbekannt')} bewahrt; kein neuer Cloud-Abruf, Anhänge fehlen"
        elif len(matches) != 1:
            row['status'] = 'preserved' if saved.is_file() else 'unavailable'
            row['local_source_matches'] = len(matches)
            detail = 'bisheriges Archiv bewahrt; lokale Ursprungssitzung fehlt oder ist mehrdeutig' if saved.is_file() else 'weder eindeutige lokale Sitzung noch bestehendes Textarchiv verfügbar'
        elif not thread.get('export_review', {}).get('through'):
            row['status'] = 'preserved_needs_review' if saved.is_file() else 'unavailable_needs_review'
            detail = 'kein neuer Export ohne dokumentierte Projektsichtung; bisheriges Archiv bewahrt'
        else:
            messages, audit = read_messages(matches[0], thread)
            review, prefix = thread['export_review'], ''
            prior = saved.read_text(encoding='utf-8-sig') if saved.is_file() else ''
            prior_checked = prior.split(CONTINUATION, 1)[1] if review.get('after') and CONTINUATION in prior else ('' if review.get('after') else prior)
            prior_count = len(re.findall(r'(?m)^## (?:Nutzer|Assistent) · ', prior_checked))
            if len(messages) < prior_count or not messages:
                # Missing/truncated sources must not silently replace a fuller archive.
                row.update(status='preserved_incomplete_source', candidate_messages=len(messages), previous_messages=prior_count)
                row['archive_sha256'] = hashlib.sha256(saved.read_bytes()).hexdigest() if saved.is_file() else None
                index.append(f"- [{thread['title']}]({thread['file']}) — bisheriges Archiv wegen unvollständiger lokaler Quelle bewahrt. {thread['topic']}.")
                report.append(row)
                continue
            if review.get('after'):
                if not saved.is_file():
                    raise ValueError('Curated base missing: ' + thread['file'])
                prefix = prior.split(CONTINUATION, 1)[0].rstrip() + '\n\n'
                row['curated_prefix_sha256'] = hashlib.sha256(prefix.rstrip().encode('utf-8')).hexdigest()
                header = CONTINUATION + '\n\n# Geprüfte Projektfortsetzung\n\n' + f"Exportstand: {stamp}. Geprüfter Zeitraum: nach {review['after']} bis {review['through']}.\n\n" + 'Der vorhandene bereinigte Projektteil bleibt oben unverändert. Frühere Rohtexte werden bewusst nicht erneut importiert.\n\n'
                row['status'] = 'curated_extended'
            else:
                header = f"# {thread['title']}\n\nChat-ID: `{thread['id']}`. Exportstand: {stamp}.\n\n" + thread['topic'] + f"\n\nProjekttexte bis {review['through']} gesichtet. Bereinigung/Auslassungen: `export-status.json`; Anhänge nicht rekonstruiert.\n\n"
                row['status'] = 'exported_reviewed'
            saved.write_text(prefix + header + render_messages(messages), encoding='utf-8')
            row.update(messages=len(messages), reviewed_through=review['through'], sanitization=audit)
            detail = f"kuratierter Ursprung bewahrt, {len(messages)} geprüfte Fortsetzungsnachrichten ergänzt" if prefix else f"{len(messages)} geprüfte Textnachrichten exportiert"
            detail += f"; gesichtet bis {review['through']}"
        if saved.is_file():
            row['archive_sha256'] = hashlib.sha256(saved.read_bytes()).hexdigest()
            index.append(f"- [{thread['title']}]({thread['file']}) — {detail}. {thread['topic']}.")
        else:
            index.append(f"- **{thread['title']}** (`{thread['id']}`) — {detail}. {thread['topic']}.")
        report.append(row)
    index += ['', '## Aktualisieren', '',
        'Mit Python: `python tools/ExportChatMemory.py`. Standardmäßig werden `CODEX_HOME/sessions` und `CODEX_HOME/archived_sessions` nach registrierten IDs durchsucht. Optional `--sessions <Ordner>` (mehrfach). Keine Server-/Cloud-Abfragen.', '',
        'Neue Texte zuerst auf Projektbezug und sensible Inhalte prüfen, dann `export_review.through` im Register auf den tatsächlich gesichteten Zeitpunkt setzen. Spätere ungesichtete Nachrichten bleiben ausgeschlossen. MainDev verwendet zusätzlich `export_review.after`: alter kuratierter Abschnitt erhalten, geprüfte Fortsetzung reproduzierbar erneuert. Fehlende oder mehrdeutige Quellen überschreiben keine Archive.', '',
        'Synthetische Filter-/Lückentests ohne echte Sitzungsdaten: `python tools/ExportChatMemory.py --self-test`. `ExportProjectChats.ps1` ist historisch und überschreibt diese Übersicht; nicht hierfür verwenden. Rohsitzungen und private Codex-Verzeichnisse gehören nicht ins Repository.', '']
    (folder / 'README.md').write_text('\n'.join(index), encoding='utf-8')
    (folder / 'export-status.json').write_text(json.dumps({'exported_at': stamp, 'scope': 'registered_reviewed_visible_project_messages_only', 'raw_session_files_committed': False, 'threads': report}, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print('Exportstatus: ' + ', '.join(f'{k}={v}' for k, v in sorted(Counter(t['status'] for t in report).items())))


def self_test():
    """Repeatable synthetic checks; no user-session directory is accessed."""
    import tempfile
    checks = []
    def check(value):
        assert value
        checks.append(True)
    stats = Counter()
    text = clean_text('<environment_context>PRIVATE</environment_context>Projekt '
        'https://e.test/file.glb?X-Amz-Signature=SECRET user@example.test Bearer abcdefg sk-' + 'a' * 25, stats)
    for absent in ('PRIVATE', 'SECRET', 'user@example.test', 'abcdefg', 'sk-'):
        check(absent not in text)
    check('https://e.test/file.glb' in text)
    check(not clean_text('# AGENTS.md instructions for C:/x\nsecret', Counter()))
    text = clean_text('<send_user_message_question_reply>[{"question":"Projekt?","answer":"Ja","questionItemId":"PRIVATE"}]</send_user_message_question_reply>', Counter())
    check('Antwort: Ja' in text)
    check('PRIVATE' not in text)
    with tempfile.TemporaryDirectory() as temporary:
        root = Path(temporary)
        folder, sessions = root / 'docs/Chats', root / 'sessions'
        folder.mkdir(parents=True)
        sessions.mkdir()
        threads = [{'id': i, 'title': i, 'file': i + '.md', 'topic': 'synthetic project'}
            for i in ('current', 'missing', 'cloud', 'curated', 'ambiguous', 'not-reviewed', 'absent')]
        for thread in threads:
            identity = thread['id']
            if identity != 'absent':
                (folder / thread['file']).write_text('CURATED BASE\n', encoding='utf-8')
            if identity not in ('not-reviewed', 'missing', 'cloud', 'absent'):
                thread['export_review'] = {'through': '2026-10-26T00:00:01Z'}
            if identity == 'curated':
                thread['export_review']['after'] = '2026-10-25T00:00:00Z'
            if identity == 'cloud':
                thread.update(source='cloud_snapshot', snapshot_date='2026-10-07')
        def make_source(identity, path):
            rows = [{'type': 'session_meta', 'payload': {'id': identity}}]
            for role, channel, body, stamp, recipient in [
                ('system', None, 'INSTRUCTION', '2026-10-26T00:00:00Z', None),
                ('assistant', 'analysis', 'REASONING', '2026-10-26T00:00:00Z', None),
                ('assistant', 'commentary', 'TOOL', '2026-10-26T00:00:00Z', 'tool'),
                ('assistant', 'final', 'Visible result', '2026-10-26T00:00:00Z', None),
                ('user', None, 'Later unseen', '2026-10-27T00:00:00Z', None),
                ('user', None, 'Old raw', '2026-10-24T00:00:00Z', None)]:
                rows.append({'type': 'response_item', 'timestamp': stamp, 'payload': {
                    'type': 'message', 'role': role, 'channel': channel, 'recipient': recipient,
                    'content': [{'type': 'text', 'text': body}]}})
            path.write_text('\n'.join(json.dumps(row) for row in rows) + '\nMALFORMED\n', encoding='utf-8')
        for identity in ('current', 'curated', 'ambiguous', 'not-reviewed'):
            make_source(identity, sessions / ('rollout-' + identity + '.jsonl'))
        (sessions / 'duplicate').mkdir()
        make_source('ambiguous', sessions / 'duplicate/rollout-ambiguous.jsonl')
        (folder / 'chat-register.json').write_text(json.dumps({'threads': threads}), encoding='utf-8')
        export(root, [sessions], '2026-10-26T00:00:01Z')
        report = json.loads((folder / 'export-status.json').read_text(encoding='utf-8'))
        rows = {thread['id']: thread for thread in report['threads']}
        check(rows['current']['messages'] == 2)
        check(rows['curated']['messages'] == 1)
        check((folder / 'curated.md').read_text(encoding='utf-8').startswith('CURATED BASE\n\n'))
        check((folder / 'missing.md').read_text(encoding='utf-8') == 'CURATED BASE\n')
        check(rows['cloud']['status'] == 'cloud_snapshot_preserved')
        check(rows['ambiguous']['status'] == 'preserved')
        check(rows['not-reviewed']['status'] == 'preserved_needs_review')
        check(rows['absent']['status'] == 'unavailable')
        check(rows['current']['sanitization']['malformed_records_omitted'] == 1)
        all_text = ''.join(path.read_text(encoding='utf-8') for path in folder.glob('*.md'))
        for absent in ('INSTRUCTION', 'REASONING', 'TOOL', 'Later unseen'):
            check(absent not in all_text)
        before = (folder / 'curated.md').read_bytes()
        export(root, [sessions], '2026-10-26T00:00:01Z')
        check((folder / 'curated.md').read_bytes() == before)
        try:
            read_messages(sessions / 'rollout-current.jsonl', {'id': 'wrong',
                'export_review': {'through': '2026-10-26T00:00:01Z'}})
        except ValueError:
            check(True)
        else:
            raise AssertionError('identity mismatch accepted')
        (folder / 'current.md').write_text((folder / 'current.md').read_text(encoding='utf-8') + '\n## Nutzer · 2026-10-26T00:00:00Z\n\nExtra preserved text\n', encoding='utf-8')
        export(root, [sessions], '2026-10-26T00:00:01Z')
        rows = json.loads((folder / 'export-status.json').read_text(encoding='utf-8'))['threads']
        check(next(row for row in rows if row['id'] == 'current')['status'] == 'preserved_incomplete_source')
    print(f'Synthetic exporter assertions passed: {len(checks)}')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--sessions', type=Path, action='append')
    parser.add_argument('--self-test', action='store_true')
    args = parser.parse_args()
    if args.self_test:
        self_test()
        return
    codex_root = Path(os.environ.get('CODEX_HOME', str(Path.home() / '.codex')))
    export(ROOT, args.sessions or [codex_root / 'sessions', codex_root / 'archived_sessions'], datetime.now(timezone.utc).isoformat())


if __name__ == '__main__':
    main()
