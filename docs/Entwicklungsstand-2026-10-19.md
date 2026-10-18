# Sicherung: Tripo-Mira und animierte Dialogvorschau

Sicherungsauftrag vom 19.10.2026. Enthält die zusammenhängenden Mira-Arbeiten vom 18.10.: Tripo-Referenz und Modellversuche, erfolgreicher H2.5-Export, erste Unity-Dialogprobe und die Erweiterung um Kopf, Augen, Mimik und Sprachformen. Der vorherige Gesamtstand für Station und Drohnen bleibt in [Entwicklungsstand vom 12.10.](Entwicklungsstand-2026-10-12.md) dokumentiert.

## Enthaltene Quellen und Assets

| Bestandteil | Ablage |
| --- | --- |
| Ursprüngliche Identitätsreferenz, bereits im Repository | `Assets/SpaceMiner/AvatarLab/MiraSketch.png` |
| Vorbereitete Bildreferenz, Prompt und Tripo-Ansichten beider Modellversuche | `docs/Art/Mira3D/Tripo-Mira-2026-10-18-*` und `Tripo-Mira-H25-2026-10-18-*` |
| Unveränderte H2.5-Originale: FBX und vier 4K-Texturen | `docs/Art/Mira3D/Tripo-H25-2026-10-18/` |
| Unity-Import, Originaltexturen, erzeugtes Mesh, PBR-Material, Metallic/Smoothness und Porträt-RenderTexture; jeweils mit Metadaten | `Assets/SpaceMiner/AvatarLab/TripoH25/` und zugehörige `.meta` |
| Bearbeitbare Dialog-Testszene | `Assets/SpaceMiner/AvatarLab/MiraTripoDemo.unity` mit `.meta` |
| Sprach-/Gesichtssteuerung und kompletter Dialogcheck | `MiraAvatar3D.cs`, `MiraDialoguePreview.cs`, `MiraFacePerformance.cs`, `MiraTripoDialogueCheck.cs` in `AvatarLab/` |
| Reproduzierbarer Szenen-/Meshaufbau | `Assets/SpaceMiner/Editor/MiraTripoDemoBuild.cs` und `MiraTripoFaceShapes.cs` mit `.meta` |
| 425 Mundintervalle für 14 zugeordnete Aoede-Aufnahmen | `Assets/SpaceMiner/Resources/MiraVisemes/` mit Metadaten und WAV-Quellhashes |
| Lokale Neuberechnung der Zeitdaten | `tools/GenerateMiraVisemes.py` |
| Starteinstieg | `Start-Mira-Tripo-Gespraech.cmd` |
| Modellinventar, Abnahmen, Bilder und originale Prüfprotokolle | `docs/Pruefungen/Mira-Tripo*` |

H2.5-Modell-ID: `8773b4ce-9c36-4961-b105-2115aca8ec31`. Das zuerst erzeugte H3.1-Modell ist mit Referenz, Ansichten und Bericht dokumentiert; dessen Export war gesperrt, daher gibt es dafür keine lokale FBX. Die erfolgreiche H2.5-Fassung ist vollständig als entpackte Quelldateien enthalten. Das zusätzliche ZIP unter `Imports/Tripo` ist eine lokale, ignorierte Sicherung derselben Quellen.

## Verhalten und Bedienung

Die Anwendung startet mit dem ersten Erwachen-Dialog: 16 Textabschnitte, 14 bestehende Sprachaufnahmen und zwei geänderte Scanneranweisungen mit Lesepause. Kopfbewegung am Hals bei ruhigen Schultern, Blinzeln, kleine Blickwechsel und sanfte Dialogmimik. Die Lippen verwenden zeitlich zugeordnete Lautformen statt ausschließlich Lautstärke. Die bisherige Mira-Darstellung im Hauptspiel bleibt erhalten.

- Schaltflächen für Pause/Fortsetzen, erneut hören, Anfang und vorherigen/nächsten Abschnitt.
- Leertaste pausiert, Escape schließt. Porträt mit gedrückter linker Maustaste drehen; R setzt die Drehung zurück.
- Kopf, Augen und Mimik getrennt abschaltbar; Stimmenlautstärke und Mundstärke einstellbar.

Lider und Blick sind noch Oberflächenformen auf dem Tripo-Mesh. Unruhige Lidkanten, fehlende separate Augäpfel/Hornhaut, Zahnreihen, Zahnfleisch und Zunge bleiben offen. F/V/L werden angenähert. Die Kieferdeformation bleibt klein; keine Zusage anatomisch fertiger oder perfekt synchroner Sprache. [Fachbeschreibung, Recherche und Aufbau](Mira3D.md#kopf-augen-mimik-und-sprachformen-18102026).

## Bauen und fortsetzen

Gemeinsame Quellprojektversion bleibt **6000.4.7f1**. Die letzte tatsächlich geprüfte lokale Vorschau wurde mit **6000.6.4f1** in der separaten Kopie `Builds/LocalPreview-6000.6.4f1` gebaut. Am Zielrechner passende Unity-Installation prüfen; bei einer abweichenden Editorversion eine eigene Kopie aus `Assets`, `Packages` und `ProjectSettings` verwenden, statt das gemeinsame Quellprojekt zu migrieren.

Bei geschlossenem Editor im gewählten Projekt `SpaceMiner.Editor.MiraTripoDemoBuild.Build` über Unity-Batchmode mit `-executeMethod` ausführen. Ausgabe relativ zum gewählten Projekt: `Builds/MiraTripoDemo/MiraTripoDemo.exe`. Der Builder erzeugt nur die neue Testszene und ihre Darstellungsassets. Eine in einer separaten Kopie gebaute Ausgabe für den vorhandenen Starteinstieg vollständig nach `Builds/MiraTripoDemo` im Hauptcheckout kopieren.

Prüfmodi derselben EXE: `-miraTripoCheck` für das vollständige Gespräch, `-miraAvatarCheck` für den allgemeinen Avatarcheck. Den Player mit dem Hauptcheckout als Arbeitsverzeichnis starten; Ergebnisse landen unter `Logs/MiraTripoMotion` beziehungsweise `Logs`. Die dauerhaften Nachweise liegen separat unter `docs/Pruefungen`.

Die Zeitdaten sind bereits enthalten. Rhubarb wird zum normalen Bauen und Spielen nicht benötigt. Bei geänderten Sprachaufnahmen lokal neu erzeugen: `python tools/GenerateMiraVisemes.py --rhubarb <Pfad-zur-rhubarb.exe>`. Verwendete Werkzeugversion, Downloadquelle und Prüfsumme stehen in `Mira3D.md`; das Werkzeug selbst liegt nur unter dem ignorierten `work`-Verzeichnis.

## Tatsächliche Prüfungen und Sicherungsgrenzen

Letzte Unity-Abnahme **18.10.2026**: Development-Build/Exit0, vollständiger Gesprächslauf mit **542 Prüfungen**/Exit0 und allgemeiner Avatarcheck derselben Binärdatei/Exit0. 14 vertonte und zwei unvertonte Abschnitte, Pause/Ruhe/Navigation, Formgrenzen, Kopf-/Schultertrennung und Bewegungsschalter geprüft. 13 Prüfbilder sowie Logs und Hashes im [Animationsbericht](Pruefungen/Mira-Tripo-Animation-2026-10-18.json). Der [erste Dialogbericht](Pruefungen/Mira-Tripo-Dialog-2026-10-18.json) dokumentiert die ältere RMS-Fassung mit 67 Prüfungen.

Am **19.10.2026** für die Sicherung erneut kontrolliert: alle 64 Quellen-/Assethashes des letzten Abnahmeberichts stimmen; neue Unity-Dateien und Metadaten sind vollständig gepaart; JSON-Berichte sind lesbar. Die 14 Zeitdateien werden zusätzlich gegen die tatsächlich zugeordneten WAVs geprüft. Kein neuer Unity-Build oder neuer Laufzeittest für diesen reinen Sicherungsauftrag. [Sicherungsinventar](Pruefungen/Mira-Sicherung-2026-10-19.json) enthält die aufgenommenen Dateien und ihre Git-Objektkennungen.

Die Git-Objektkennungen berücksichtigen die bestehende Zeilenendennormalisierung; SHA256-Werte beziehen sich auf die lokal geprüften Dateibytes. Originale Prüflogs bleiben durch `.gitattributes` bytegetreu erhalten. Die vorhandene Ausnahme für leere Unity-YAML-Werte wurde auf Szenen und Meshassets erweitert, damit deren unveränderte Serializer-Ausgabe die Whitespace-Prüfung besteht.

Lokale EXE-Builds, Unity-Library, Werkzeuginstallationen und allgemeine Arbeitslogs bleiben wie im Projekt vorgesehen ignoriert. Alle zum Neubauen nötigen Modelle, Texturen, Szenen, Metadaten, Zeitdaten und Quellen sowie ausgewählte dauerhafte Prüflogs sind enthalten. Alte Berichte behalten ihren damaligen Commit-/Pushstatus; sie beschreiben ihren Erstellungszeitpunkt. Chatarchive wurden für diesen Sicherungsauftrag nicht neu exportiert. Keine neue Tripo-Ausgabe oder Sprachgenerierung.
