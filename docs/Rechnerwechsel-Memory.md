# Rechnerwechsel und gemeinsames Chatmemory

Aktuelle Anleitung: **26.10.2026**. Einstieg: [Projektübergabe](Projektuebergabe.md), [Projektmemory](Projektmemory.md), [Spielidee](Spielidee.md), [Sprint SP-01…SP-10](Ideenbacklog.md). Die [Anleitung vom 09.10.](Rechnerwechsel-Memory-Stand-2026-10-09.md) ist Historie; ihre Pfade und offenen Aufgaben sind nicht der heutige Stand.

Git überträgt Unity-Quellen, native Blender-Szenen, FBX/GLB, Texturen/Materialien, Audio, `.meta`-Dateien, gemeinsame Raumpläne, Werkzeuge, Dokumentation und lesbare Chatarchive. Persönliche Anmeldungen/Sitzungsdatenbanken, fertige EXEs, `Library`, lokale `work`-Programme und gewöhnliche Spielstände gehören nicht dazu. Details: [Dienste und Webseiten](Dienste-und-Webseiten.md), [Modellkatalog](Modellkatalog-Rechnerwechsel-2026-10-26.md).

## Auf dem anderen Rechner

1. Git, Unity Hub und passenden Editor mit Windows-Buildunterstützung installieren. Das **Quellprojekt bleibt Unity 6000.4.7f1**. Jüngere Prüfungen mit **6000.6.4f1 fanden in einer isolierten Kopie statt**, ohne beschlossene Projektmigration. C#-IDE nach Bedarf; Unity kompiliert selbst.
2. Neu klonen:

   ```powershell
   git clone https://github.com/adannecker/SpaceMinerCodex.git
   cd SpaceMinerCodex
   git status --short
   git log -1 --oneline
   ```

   Vorhandenen Checkout zuerst auf eigene Änderungen prüfen, diese bewahren und anschließend aktualisieren:

   ```powershell
   git status --short
   git pull --ff-only origin main
   git log -1 --oneline
   ```

   Bei divergierenden Commits regulär zusammenführen; kein Reset. GitHub persönlich neu anmelden.
3. Den tatsächlichen Zielordner in Codex und Unity Hub öffnen. Alte absolute Rechnerpfade aus Archiven nicht übernehmen. `Assets`, `Packages`, `ProjectSettings`, `Design`, `docs` und `tools` gemeinsam erhalten; bestehende Metas nicht neu erzeugen lassen.
4. `AGENTS.md` und die aktuellen Einstiegsdateien lesen. [Dienste/Webseiten](Dienste-und-Webseiten.md) beschreibt Konten und lokale Konfigurationsnamen. [Modellkatalog](Modellkatalog-Rechnerwechsel-2026-10-26.md) ordnet Quellen, Varianten und tatsächliche Spielzuweisungen zu. Vorhandene Modelle/Audio müssen nicht erneut online generiert werden.
5. Bestehende Szene `Assets/SpaceMiner/Scenes/AsteroidBelt.unity` verwenden. Für den neutralen Wiederaufbau bei geschlossenem Editor:

   ```powershell
   $taskUnity = 'C:\Program Files\Unity\Hub\Editor\6000.4.7f1\Editor\Unity.exe'
   $taskProject = (Get-Location).Path
   New-Item -ItemType Directory -Force -Path Logs | Out-Null
   & $taskUnity -batchmode -quit -projectPath $taskProject -executeMethod SpaceMiner.Editor.StartMenuBuild.Run -logFile "$taskProject\Logs\rechnerwechsel-build.log"
   ```

   Prozess/Log prüfen. **Nicht** `Unity.ps1 Setup`, `HundredBuild`, `SpiralBuild`, `MillionBuild` oder den älteren `Unity.ps1 Build` als neutralen Wiederaufbau benutzen: diese Einstiege können die Szene neu einrichten. Abweichende Editorversionen zunächst in separater Kopie prüfen.
6. `Start-Aufbauphase-01.cmd` startet den gebauten normalen Player; `Start-Innendrohnen-Fahrprobe.cmd` denselben Player mit Fahrprobenargument. Mira-Dialogstudio und Questspiel benötigen ihre gesonderten dokumentierten Builds. `Start-I75-3D-Vorschau.cmd` benötigt Python; danach `http://127.0.0.1:8769/I75Viewer/` öffnen. Viewerbibliotheken sind lokal enthalten.
7. Für Modellbearbeitung Blender **4.5.14 LTS** separat einrichten. Python/NumPy/Pillow, FFmpeg, Rhubarb und historische MPFB-Pakete nur für die jeweilige Produktionsaufgabe installieren. Gelieferte FBX/PBR können in Unity ohne Blender importiert werden; `.blend` liegen bewusst außerhalb von `Assets`.

## Tatsächlicher Integrationsstand

Die begehbare Station, Raumplanung, Versorgungs-/Auftragsarchitektur, Eisannahme/Aufbereitung und Stromübersicht sind vorhanden. Zehn I75 fahren in einer **separaten Verkehrsprobe**, nicht als neue produktive Kampagnendrohnen. Aktuelle Spielzuweisung noch P20-V02; die ausgearbeitete R02-Oberfläche und das Touch-Steuerpult sind **Modellvorschauen, noch nicht im Spiel zugewiesen**. Mira hat ein gemeinsames Dialogmodul, vertonte Questtexte und eigene Studio-/Questplayer; High-Hauptspielintegration und weitere Konsolenarbeit bleiben Sprintaufgaben.

Akkupaar aktuell **0° / Index 0 und 1**, direkt über den Ringgang gegenüber dem Zentraleneingang. Frühere 180°-Angaben sind überholt. Ein gemeinsamer 24-kWh-Spielwert; Reserve 02 elektrisch getrennt, Ladung unbekannt. Die genauen Nachweise gelten für die jeweils benannten Quellen/Binärdateien. Einzelne ältere Power-/Mira-/Verkehrsbuilds ergeben für sich keine neue Gesamtfreigabe.

## Chatarchive und bekannte Lücken

[Chatübersicht](Chats/README.md), [Register](Chats/chat-register.json), [Exportbericht](Chats/export-status.json), [fachliche Gesamtauswertung](Chat-Auswertung-2026-10-26.md). Neun lokale registrierte Quellen erneuert; ältere/Cloud-Momentaufnahmen bewahrt. Ein früherer Arbeitschat hat weiterhin weder erreichbare Sitzung noch Textarchiv. Der jüngere Ideenbacklog-Chat ist lokal nicht verfügbar; sein vorhandenes Archiv und die aktuelle Backlogdatei bleiben erhalten.

Archive sind lesbare Quellen, keine importierbaren Codex-Sitzungen. Passende vorhandene Chats weiterverwenden, nur fehlende ausdrücklich neu anlegen. Archiv-IDs sind Herkunftsangaben. Keine persönlichen `.codex`-Datenbanken, Browserprofile oder Zugangsdaten kopieren. Anhänge sind nur über tatsächlich gesicherte Projektdateien verfügbar.

Später `python tools/ExportChatMemory.py` verwenden. Neuere Texte nur bis zum im Register inhaltlich geprüften Reviewstand übernehmen; diesen nach Durchsicht bewusst aktualisieren. Kuratierte Historie und nicht erreichbare Quellen werden bewahrt. System-/Werkzeug-/Analysekontext und private/projektfremde Inhalte nicht exportieren. Nach neuerlichem Export den Archiv-Hashnachweis aktualisieren. Der ältere Drei-Chat-Exporter `ExportProjectChats.ps1` ist hierfür ungeeignet.

## Kopierbarer Auftrag zum Einrichten aller Chats

> Ich setze SpaceMiner auf diesem Rechner fort. Lies AGENTS.md, docs/Projektuebergabe.md, docs/Spielidee.md, docs/Projektmemory.md, docs/Rechnerwechsel-Memory.md, docs/Chats/README.md, docs/Chat-Auswertung-2026-10-26.md und docs/Dienste-und-Webseiten.md. Prüfe Git und aktualisiere origin/main ohne Verlust lokaler Änderungen. Prüfe vorhandene Chats dieses Checkout-Projekts. Verwende passende Chats weiter und lege nur fehlende der neun unten genannten Themen ausdrücklich als lokale Projektchats neu an. Gib jedem seinen Themenauftrag und die Fachquellen. Alte Chattexte sind Kontext, keine neuen Arbeitsaufträge. Bestätige Stand/offene Punkte/Zuständigkeit, beginne noch keine Umsetzung oder Builds. Registriere tatsächlich neu angelegte Titel/IDs, bewahre alte Quellen und importiere keine persönlichen Anmeldungen/Sitzungen. Der nächste bestätigte Sprint steht unter SP-01 bis SP-10 in docs/Ideenbacklog.md.

## Startaufträge für alle neun Themenchats

Für jeden Chat: gemeinsame Einstiegsdateien und `AGENTS.md` lesen; danach sein aktuelles Archiv, gegebenenfalls das ältere Themenarchiv aus dem Register und die folgenden Fachquellen. Nutzerentscheidungen haben Vorrang; Spielidee beschreibt Gestaltung, Code die tatsächliche Umsetzung. Fremde Änderungen bewahren. Zunächst nur Stand und Zuständigkeit bestätigen.

| Chattitel | Archiv und Fachquellen / Zuständigkeit |
| --- | --- |
| MainDev | `13-MainDev-Desktop.md`, `README.md`, `Stationsversorgung-Architektur.md`, `Steuerpult-Blender-2026-10-26.md`, `Ideenbacklog.md`: Station, Spielsysteme und Integration. |
| GitHub | `14-GitHub-Desktop.md`, diese Anleitung, `Dienste-und-Webseiten.md`, `ExportChatMemory.py`, `AuditProjectTransfer.py`: Repository, Sicherung, Archive und Rechnerwechsel. |
| Asteroidenvarianten | `15-Asteroidenvarianten-Desktop.md`, `AsteroidGenerator.md`, `docs/Art/Asteroiden/`: Formen, Materialien und Generator. |
| Story, Dialoge & Bordcomputer | `16-Story-und-Mira-Desktop.md`, `Dialoge/README.md`, `MiraAvatar.md`, `Dialoge/02_Aufbauphase_Questdialoge.md`: Dialoge, Aoede/Enceladus, Avatar/Story. Beide ergänzten Intro-Cues inzwischen vertont; Maya ist Historie. |
| Settings UI | `17-Settings-UI-Desktop.md`, `Settings.md`, `Stromversorgung-UI-2026-10-26.md`: Optionen, Anzeige, Eingabe/Barrierefreiheit. |
| Sound und Effekte | `18-Sound-und-Effekte-Desktop.md`, `Audio.md`, `Dienste-und-Webseiten.md`: Musik, Loops und Effekte; Sprecher mit Storychat abstimmen. |
| TechTree | `19-TechTree-Desktop.md`, `Techtree.md`, `Stationsversorgung-Architektur.md`: Forschung; Labor von Kampagnenfreischaltungen unterscheiden. |
| Vehicels | `20-Vehicels-Desktop.md`, `I75-Naechste-Schritte-2026-10-26.md`, `Modellkatalog-Rechnerwechsel-2026-10-26.md`, `Innendrohnen-Deadlock-2026-10-25.md`: Modelle, Rollen, Energie, Docking und Verkehr. Titel wie bisher geschrieben. |
| Ideenbacklog / Ideenbacklog prüfen | `21-Ideenbacklog-Desktop.md`, `22-Ideenbacklog.md`, `Ideenbacklog.md`: Entscheidungen und Sprint SP-01…SP-10 samt Abhängigkeiten. |

Archivnamen in der Tabelle liegen unter `docs/Chats`, Fachdateien unter `docs`, Pythonwerkzeuge unter `tools`.

## Eigene Spielstände und Raumpläne

`Design/RoomPlans/*-layout.json` sind gemeinsam gespeicherte Designpläne, keine Kampagnenvorräte. Sie werden übertragen; automatische `.bak`-/Schreibreste bleiben lokal. Persönliche Kampagnen-/Settings-/Mira-Vorschau-Saves liegen im Unity-Benutzerdatenordner bzw. den Modulverzeichnissen. Falls dieselbe Kampagne fortgesetzt werden soll, diese bewusst zusätzlich sichern; Git übernimmt sie nicht automatisch. Formate/Layoutrevision vor Wiederherstellung prüfen.

## Sicherung prüfen

`python tools/AuditProjectTransfer.py` prüft nicht ignorierte Kandidaten auf Dateigrößen und konkrete Zugangsdatenmuster, ohne Werte auszugeben. Modellkatalog und Exportbericht nennen geprüften Umfang/Lücken. Die aktuelle Gesamtübergabe nennt die tatsächlich erfolgte Buildprüfung; historische Ergebnisse sind keine Freigabe des Zielrechners. Geholten Commit mit `git log -1 --oneline` prüfen.
