# Projektchats und gemeinsames Chatmemory

Exportstand: 2026-10-06T13:17:29.153160+00:00.

Lesbare Momentaufnahmen, kein importierbares Codex-Sitzungsformat. Exportiert werden ausschließlich Nutzer- und Assistententexte der ausdrücklich registrierten Projektchats. Systemanweisungen, interne Überlegungen, Werkzeugprotokolle und Umgebungskontext werden ausgelassen. Anhänge werden nicht rekonstruiert; übernommene Assets liegen im Projekt.

Aktueller Stand: [Projektmemory](../Projektmemory.md), [Projektübergabe](../Projektuebergabe.md). Historische Aussagen und alte Pfade können überholt sein. Chat-IDs dienen der Zuordnung auf dem Ursprungsrechner und stellen auf einem anderen Rechner keine Sitzungen her.

## Ursprüngliche Archive vom 05.10.2026

- [Unity-Spiel gemeinsam entwickeln](01-Unity-Spiel.md)
- [Asteroidenvarianten entwerfen](02-Asteroiden.md)
- [Story, Dialoge & Bordcomputer](03-Story-und-Mira.md)

## Neuere Chats vom 06.10.2026

- [GitHub](04-GitHub.md) — 51 Textnachrichten. Repository, Projektkontext, Rechnerwechsel und gemeinsame Sicherung.
- [MainDev](05-MainDev.md) — 59 Textnachrichten. Unity 6000.4.7f1, 100-Körper-Wolke, optionale Stresstests, Station und Bergbauzyklus.
- [Asteroidenvarianten](06-Asteroidenvarianten.md) — 7 Textnachrichten. Asteroidenformen, Materialien und Generator.
- [Story, Dialoge & Bordcomputer](07-Story-und-Mira.md) — 12 Textnachrichten. Mira, Intro und Sprechertexte; Abgleich der Drohnenzahl offen.
- [Settings UI](08-Settings-UI.md) — 64 Textnachrichten. Mining-Pulse-Stil, Player-Settings, modulare UI und Audiokanäle.
- [Sound und Effekte](09-Sound-und-Effekte.md) — 29 Textnachrichten. Configuration- und Gameplay-Musik, Loop-Dateien und Audioübergänge.
- [TechTree](10-TechTree.md) — 35 Textnachrichten. Tier-I-Entwurf und integriertes Techtree-Menü; Forschungsmechanik offen.
- [Vehicels](11-Vehicels.md) — 25 Textnachrichten. Separater Themenchat für Fahrzeuge; bisher überwiegend übernommener Kontext.
- **SpaceMinerCodex – lokaler Arbeitschat** (`01a110eb-3487-7061-8a24-86afe58bdc1f`): kein eindeutiges lokales Archiv verfügbar. Früher angelegt; derzeit nicht in aktiver Chatliste oder lokalem Sitzungsbestand bestätigt.

## Aktualisieren

Mit einer lokalen Python-Installation: `python tools/ExportChatMemory.py`. Optional `--sessions <lokaler Sitzungsordner>`. Neue bestätigte Projektchats zuerst in `chat-register.json` aufnehmen. Die drei ursprünglichen Archive werden dabei bewahrt. `ExportProjectChats.ps1` ist der ältere Exporter ausschließlich für die drei Ursprungssitzungen und überschreibt diese Übersicht; hierfür nicht verwenden.
