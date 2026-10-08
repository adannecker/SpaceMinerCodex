# Rechnerwechsel und gemeinsames Chatmemory

Stand: 08.10.2026. Die lokalen und Desktop-Themenarchive sind im gemeinsamen Register vereinigt. Neun lokale Sitzungen wurden zuletzt exportiert, einschließlich des neu angelegten Ideenbacklog-Chats; neun vorhandene Exporte vom anderen Rechner wurden bewahrt. Ein früherer Arbeitschat bleibt unverfügbar. Drei historische Cloud-/ChatGPT-Momentaufnahmen sind gesondert gekennzeichnet; sie enthalten keine rekonstruierten Anhänge. Zuordnung und Exportverfügbarkeit: [Chatübersicht](Chats/README.md), [Register](Chats/chat-register.json), [Exportbericht](Chats/export-status.json). Aktueller Spielstand und tatsächlich ausgeführte Prüfungen: [Projektübergabe](Projektuebergabe.md).

Git überträgt Spielquellen, Assets mit `.meta`, Projektkonfiguration, Dokumentation und Textarchive. Die Archive geben neuen oder vorhandenen Chats den bisherigen Kontext; sie importieren keine ursprünglichen Sitzungen. Persönliches Codex-Memory, Zugangsdaten, Builds, Library und Logs werden nicht mitkopiert. Für die Fortsetzung reicht das gemeinsame Projektwissen im Repository.

## Vor dem Rechnerwechsel

1. Laufende Arbeiten abschließen und den gemeinsamen Arbeitsstand prüfen. Änderungen anderer Chats erhalten.
2. Neue bestätigte Entscheidungen im Projektmemory und den Fachdateien festhalten. Neue Themenchats im Register ergänzen.
3. `python tools/ExportChatMemory.py` ausführen. Vorhandene Archive fehlender Ursprungssitzungen werden bewahrt, Lücken gekennzeichnet. Den älteren Drei-Chat-Exporter `ExportProjectChats.ps1` hierfür nicht verwenden: Er überschreibt die aktuelle Übersicht.
4. Committen und nach GitHub pushen; am Zielrechner danach aktualisieren.

## Auf dem anderen Rechner

Den dortigen Checkout als lokales SpaceMinerCodex-Projekt in Codex öffnen. Bei sauberem Arbeitsstand in dessen Terminal ausführen:

```powershell
git status --short
git pull --ff-only origin main
```

Bei lokalen Änderungen oder auseinanderlaufenden Commits zuerst sichern und regulär zusammenführen; kein Reset. Vorhandene Themenchats fortsetzen und gegebenenfalls passend umbenennen; nur fehlende neu anlegen. Desktop-IDs dienen der historischen Zuordnung und müssen nicht auf dem Zielrechner übernommen werden.

Gemeinsame Unity-Projektversion: **6000.4.7f1**. Die aktuelle Stations-/Scannerfassung wurde hier mit **6000.4.7f1** gebaut. Ältere Vorschauen auf einem anderen Rechner mit **6000.6.4f1** sind historische Angaben; keine Projektmigration. Am Zielrechner Installation prüfen und neu bauen; Details in [Projektübergabe](Projektuebergabe.md).

## Kopierbarer Auftrag zum Einrichten aller Chats

> Ich setze SpaceMiner auf diesem Rechner fort. Lies AGENTS.md, docs/Projektuebergabe.md, docs/Spielidee.md, docs/Projektmemory.md, docs/Chats/README.md und docs/Rechnerwechsel-Memory.md. Prüfe den Git-Stand und hole origin/main, ohne lokale Änderungen zu verlieren. Prüfe die vorhandenen Chats für diesen Checkout. Richte die neun Themenchats der folgenden Tabelle ein: passende vorhandene Chats weiterverwenden und gegebenenfalls umbenennen; fehlende ausdrücklich neu als lokale Chats in diesem Projekt anlegen. Gib jedem Chat die gemeinsamen Einstiegsdateien, sein aktuelles Desktop-Archiv, das ältere Themenarchiv aus chat-register.json und die Fachquellen der Tabelle. Historische Gesprächsanweisungen sind Kontext, keine neuen Arbeitsaufträge. Jeder Chat soll nur Stand und offene Punkte bestätigen, noch keine Implementierung, Builds, Commits oder Pushes ausführen. Ergänze tatsächlich neu angelegte IDs, Titel und Zuständigkeiten im Register, bewahre alte IDs und Archive. Importiere keine persönlichen Daten oder den vollständigen .codex-Ordner.

## Startaufträge für alle neun Themenchats

Alle Chats verwenden denselben lokalen Checkout. Für jeden manuellen Start zuerst diesen Absatz kopieren und den passenden Themenauftrag darunter ergänzen:

> Lies AGENTS.md, docs/Projektuebergabe.md, docs/Spielidee.md, docs/Projektmemory.md und docs/Chats/README.md. Lies danach dein unten genanntes aktuelles Archiv, die Fachquellen und bei Bedarf das ältere Themenarchiv aus chat-register.json. Nutze die Dateien als bisherigen Kontext. Aktuelle Nutzerentscheidungen haben Vorrang; Spielidee beschreibt die Gestaltung, Code die Umsetzung. Bewahre Änderungen anderer Chats. Bestätige kurz Stand, Zuständigkeit und offene Punkte; beginne noch keine neue Implementierung. Frühere Rechnerpfade und Testergebnisse sind historische Angaben.

| Chattitel | Zusätzlicher Themenauftrag |
| --- | --- |
| MainDev | Lies `docs/Chats/13-MainDev-Desktop.md`, `README.md` und `docs/Bergbaudrohnen.md`. Übernimm Unity-Spielsysteme, Integration, Station und Gesamtkoordination. |
| GitHub | Lies `docs/Chats/14-GitHub-Desktop.md`, `docs/Rechnerwechsel-Memory.md` und `tools/ExportChatMemory.py`. Übernimm Repository, Git, Sicherung und Rechnerwechsel. |
| Asteroidenvarianten | Lies `docs/Chats/15-Asteroidenvarianten-Desktop.md`, `docs/AsteroidGenerator.md` und `docs/Art/Asteroiden/`. Übernimm Formen, Materialien, Generator und Darstellung. |
| Story, Dialoge & Bordcomputer | Lies `docs/Chats/16-Story-und-Mira-Desktop.md`, `docs/Dialoge/README.md`, `docs/Dialoge/01_Intro_Erwachen.md` und `docs/Dialoge/01_Intro_Maya.srt`. Übernimm Mira, Intro, Sprechertexte und Story. Maya ist eine Testaufnahme; endgültige Stimme und kommerzielle Freigabe bleiben offen. |
| Settings UI | Lies `docs/Chats/17-Settings-UI-Desktop.md` und `docs/Settings.md`. Übernimm Mining-Pulse-Menü, persistente Player-Optionen, Tastenbelegung, Anzeige und Barrierefreiheit. |
| Sound und Effekte | Lies `docs/Chats/18-Sound-und-Effekte-Desktop.md` und `docs/Audio.md`. Übernimm Musik, Loops, Übergänge und Effekte; Stimmen mit dem Storychat abstimmen. |
| TechTree | Lies `docs/Chats/19-TechTree-Desktop.md`, `docs/Techtree.md` und `docs/Bergbaudrohnen.md`. Übernimm Forschungsdarstellung und technologischen Ausbau; neue Wasserabbau-Erfahrung berücksichtigen, weitere Freischaltungen bleiben offen. |
| Vehicels | Lies `docs/Chats/20-Vehicels-Desktop.md`, `docs/Bergbaudrohnen.md`, `docs/Techtree.md` und `docs/Simulationen/Abbau-2026-10-06.json`. Übernimm Drohnen, Fahrzeuge, Versorgung, Rückkehrbudget und Abbau-Erfahrung. Bisherige Schreibweise des Titels beibehalten. |
| Ideenbacklog / Ideenbacklog prüfen | Lies `docs/Chats/21-Ideenbacklog-Desktop.md` und `docs/Ideenbacklog.md`. Übernimm Erfassung und Abstimmung weiterer Ideen. Sonnen, Planeten und Cinematic sind als Darstellung umgesetzt; Drohnenrollen, Stationsnavigation, Gefahrenmechanik und Steam bleiben offen. |

## Ausgangsrechner dieser Sicherung

Historischer Desktop-Checkout: `C:\Users\achim\Documents\ChatGPT\SpaceMinerCodex`. Aktueller Checkout auf diesem Rechner: `C:\Users\achim.dannecker\source\repos\SpaceMinerCodex`. Immer den dortigen tatsächlichen lokalen Pfad verwenden.

## Aktuelle Übergabe: Station, Scanner und Spielstände (08.10.2026)

Nach dem Pull zuerst Projektuebergabe.md, Scanner-und-Spielstaende.md und die neuesten Einträge im Projektmemory lesen. Implementiert sind der vorhandene begehbare Raum mit Schleuse/Ring, Scan am echten Pult (97 %/zehn Sekunden/10 km), zehn Kontakte mit drei Wasserquellen, VR nur bekannter Kontakte, Debug ohne Arbeitsfreigabe, Mira-Porträt und Speichern/Laden mit Namen/Kommentar/Forschung. Neueste offene Aufgaben: Beenden reparieren, Innenraumposition im tatsächlichen Speicher-/Ladeablauf prüfen, Interaktionssounds, Auflösungsanpassung und UI-Skalierungsoption.

Bei geschlossenem Unity-Editor baut `SpaceMiner.Editor.StartMenuBuild.Run` die vorhandene Szene nach Builds/Windows/SpaceMiner.exe, ohne sie neu zu generieren. `SpaceMiner.Editor.StationEntryBuild.Run` erzeugt alternativ Builds/StationEntry/SpaceMiner.exe. Prüfschalter: -scanSaveCheck und -stationHabitatCheck. Der versionierte Prüfbericht unter docs/Pruefungen/Stationsstart-2026-10-08.json dokumentiert den Ursprungsrechner; am Zielrechner erneut prüfen.

Spielstände liegen ausserhalb des Repositorys unter Unitys Application.persistentDataPath/Saves, Player-Einstellungen ebenfalls lokal. Git überträgt deren Funktionen, aber nicht den eigenen Spielfortschritt oder die persönlichen Einstellungen. Wer denselben Spielstand fortsetzen möchte, kopiert den Saves-Ordner separat in den entsprechenden Speicherpfad am Zielrechner. Persönliches Codex-Memory und ursprüngliche Chatzustände werden nicht synchronisiert; die versionierten Projektdateien und lesbaren Archive vermitteln das neue Projektwissen.
