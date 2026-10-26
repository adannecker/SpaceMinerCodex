# Mira: gesicherte Aufnahmen herunterladen

**Gelöst am26.10.2026:** ZIP über private Cloud-Shell-Dateivorschau lokal übernommen, beide WAVs anhand obiger Prüfsummen validiert und mit Lippenprofilen in Spiel/Demo eingebaut. Die folgenden Schritte dokumentieren den vorherigen Exportstand. Der temporäre Exportserver wurde nach erfolgreichem Download beendet.

Stand 25.10.2026: Zwei neue Aufnahmen sind erzeugt und in Google Cloud Shell gespeichert. Noch keine lokale WAV, kein Einbau oder neuer Unity-Test. Die bestehenden14 Intro-Aufnahmen und `intro.json` sind unverändert.

## Gesicherte Dateien

Cloud-Shell-Verzeichnis: `~/mira_aoede_20261025_d8a7/`

| Datei | Bytes | Dauer | SHA256 |
| --- | ---: | ---: | --- |
| mira_12.wav | 247016 | 5,600272s | a8caf9071555e139764ff0b96ebb0bda2078696d7130a876748d7fdda19c1d02 |
| mira_13.wav | 541604 | 12,280272s | 8454b7450bb2ce21a26b24715cab27a15ae6cfa47b6273d07a84a51a5449f438 |

Beide Dateien in Cloud Shell mit Python `wave` gelesen: RIFF WAVE, LINEAR16, mono,22050Hz. ZIP `mira_intro_scan_aoede.zip` (549442Bytes) enthält diese WAVs und `manifest.json`. Cloud-Shell-Dateiliste und Prüfsummen wurden in der Oberfläche geprüft. Kein subjektiver Hörtest.

## Download fortsetzen

Die fertigen Audioergebnisse bleiben in den beiden Google-Media-Studio-Tabs erhalten. Sie müssen nicht erneut erzeugt werden. Das Cloud-Shell-ZIP ist zusätzlich gesichert.

1. [Cloud Shell](https://shell.cloud.google.com/?authuser=1&project=yt-publischer&show=ide%2Cterminal) mit dem für die Aufnahme verwendeten Konto öffnen; falls nötig in einem normalen Browser.
2. Menü **Weitere Cloud-Shell-Einstellungen → Download → Dateibrowser** öffnen.
3. Ordner `mira_aoede_20261025_d8a7`, dann die einzelne Datei **mira_intro_scan_aoede.zip** auswählen. Den Dateinamen im Übertragungsdialog prüfen; nicht das gesamte Homeverzeichnis auswählen.
4. In Downloads speichern. Der aktuelle In-app-Browser meldete gestoppte Downloads. Auch bei Cloud-Shell-Status „Erfolg“ war bei der lokalen Prüfung keine Datei angekommen.

## Anschließender Einbau

- Nur die zwei WAVs und das Manifest aus dem ZIP entnehmen; Dateigrößen und SHA256 gegen obige Werte prüfen.
- Originalexport zusätzlich in diesem Dokumentationsordner sichern. Alte unzugeordnete WAVs sind unter `previous-unassigned/` gesichert; `.meta` der Runtime-Assets erhalten.
- Zwei neue WAVs unter `Assets/SpaceMiner/Resources/Intro/` übernehmen und Cues12/13 in `intro.json` zuordnen. Bestehende14 Audiohashes gegen `existing-audio-hashes.json` prüfen.
- Nur für diese zwei Clips Rhubarb-Lippenzeitdaten erzeugen; Regieprofile und bisherige14/2-Testannahmen an16/0 anpassen. Gemeinsames Dialogstudio und separaten Spiel-Prüfbuild bauen und prüfen.
- Auftragsdialoge stehen erst als Inventar bereit; variable Scannergebnisse und ungeeignete Prototypentexte vor weiterer Generierung redigieren.

Der Nutzer hat die Cloud-Shell-Autorisierung ausdrücklich erteilt. Es wurden keine API-Schlüssel, IAM-Rollen oder Abrechnungsänderungen eingerichtet. Die Speicherung hat vorhandene Studio-Audiodaten übernommen und keine zusätzliche TTS-Generierung ausgelöst.
