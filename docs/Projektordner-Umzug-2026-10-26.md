# Lokaler Projektordner: Umzug nach C:\temp

<!-- ROOT_MOVE_STATUS_BEGIN -->
**Umzug abgeschlossen:** physischer Checkout `C:\temp\SpaceMinerCodex`; alter Pfad als NTFS-Junction erhalten. SHA256, HEAD, Arbeitsstand und Git-Konnektivitaet im Helfer geprueft; `Logs/ProjectRoot-20261026/after.json` enthaelt den tatsaechlichen Nachweis. Anschliessend nur diese Ordnerdokumentation aktualisiert.
<!-- ROOT_MOVE_STATUS_END -->

Der bisherige Source-Ordner liegt bereits ausserhalb beider OneDrive-Wurzeln und ist kein Ordnerverweis. Ein Umzug allein beseitigt dort deshalb keine nachgewiesene OneDrive-Synchronisierung. Der ausdruecklich gewuenschte neue Ort wird trotzdem vorbereitet.

## Ausfuehren

1. Unity und Spiele schliessen. ChatGPT/Codex **vollstaendig beenden**, auch eventuellen Hintergrund-/Traybetrieb.
2. `C:\temp\SpaceMiner-Projekt-umziehen.cmd` starten und das Ergebnis im Fenster abwarten.
3. Bei Erfolg liegt der gesamte bestehende Checkout physisch unter `C:\temp\SpaceMinerCodex`; alte Pfade bleiben als Verzeichnisverweis auf denselben Checkout gueltig. Danach Codex wieder oeffnen.

Der Helfer liegt ausserhalb des zu verschiebenden Verzeichnisses. Er erstellt vor dem Eingriff eine frische SHA256-Liste der versionierten/unversionierten Projektdateien und aller Git-Dateien, erhaelt HEAD und den uncommitteten Arbeitsstand und verschiebt dasselbe Verzeichnis auf C:. Ignorierte Library-/Build-/Work-/Log-Dateien ziehen damit ebenfalls mit; keine parallele Unity-Kopie, keine Loeschung oder automatische Zusammenfuehrung mit einem vorhandenen Ziel. Anschliessend prueft er Dateien, Git-Arbeitsstand und Git-Konnektivitaet, schreibt `after.json` und aktualisiert erst dann die lokale Ordnerdokumentation. Sechs lokale Modell-Viewer werden mit ihren bisherigen Ports/Revisionen wieder gestartet. Der alte Pfad wird eine NTFS-Junction auf den neuen.

Die Codex-Projekt-ID und alle Chat-Zuordnungen werden nicht geaendert. Die Projektliste kann deshalb weiterhin den bisherigen Alias anzeigen. Das unterstuetzt die bestehenden Chats mit ihrem gespeicherten Arbeitsverzeichnis. Ein spaeterer Wechsel des sichtbaren Hauptordners erfolgt ueber **Projekt bearbeiten → Ordner hinzufuegen → als Hauptordner festlegen**, siehe [offizielle OpenAI Docs zu Projekten](https://learn.chatgpt.com/docs/projects). Historische Pfade in Chatarchiven/Buildnachweisen werden nicht pauschal ersetzt.

## Bisherige Pruefung und Grenzen

- Vorbereitetes Anfangsmanifest: **7039 Projekt-/Git-Dateien**, HEAD `659b514f79878df772dbc73a1bea130b1594ea7c`, Branch `main`; spaetere Helferdateien/Dokumentation werden beim tatsaechlichen Lauf neu erfasst.
- Microsoft [Sysinternals Handle](https://learn.microsoft.com/en-us/sysinternals/downloads/handle) zeigt offene Zugriffe von `ChatGPT.exe`, `codex.exe`, `node.exe` und `node_repl.exe` auf den Checkout. Nur lesend diagnostiziert, keine Handles gewaltsam geschlossen, keine Chatprozesse beendet. Lokaler Auszug unter `Logs/ProjectRoot-20261026/open-handles.txt`.
- Modell-Viewer nach beiden gescheiterten Versuchen wieder gestartet. UI-Stilauftrag zuvor abgeschlossen: drei Builds,535 numerische Player-/UIchecks und36 Studioaufnahmen; [separater Nachweis](Pruefungen/Interface-Stil-2026-10-26.json).
- `CheckOnly` des Umzugshelfers ist ein lesender Bereitschaftstest. Parser-/Dateipruefung und dieser Test ersetzen keine erfolgreiche Verschiebung. Diese ist erst durch `after.json` und den Abschlussstatus oben bestaetigt.

Kein Commit/Push durch Vorbereitung oder Helfer. Persoenliches Codex-Memory wird nicht geaendert. Der Starthelfer erzwingt kein Beenden der App und aendert keine Codex-Datenbank.
