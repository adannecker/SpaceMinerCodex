# SpaceMiner: Übergabe auf den anderen Rechner

Stand: 08.10.2026, Übergabe mit begehbarer Station, erstem Scan, VR-Scanwissen, Mira-Porträt und Spielständen. Einstieg für neue oder fortgesetzte lokale Codex-Chats. Entscheidungen: [Spielidee](Spielidee.md). Kompakter Kontext: [Projektmemory](Projektmemory.md). [Rechnerwechsel-Memory](Rechnerwechsel-Memory.md) enthält kopierbare Startaufträge für alle neun Themenchats.

## Aktueller Stand am 08.10.2026

Unity 6000.4.7f1, Windows, Built-in Render Pipeline. Bestehende Szene AsteroidBelt.unity mit 100 Asteroiden; größere Felder bleiben optionale Stresstests. Keine neue Projektkopie anlegen. Startmenü, Wasserauftrag und Kamera sind implementiert. Beenden-/Speicherablauf ist eingebaut, funktioniert laut jüngstem Nutzerfeedback im Spiel aber nicht zuverlässig und ist die erste offene Fehlerbehebung.

Die neue Station hat acht Ladebuchten, zwei Wartungsplätze und Tankkupplung. Drohnen parken rückwärts und koppeln zur geschlossenen Wasserübergabe am Tank an. Drohne 01 startet bereit, 02 wartet auf Ladung, acht sind beschädigt. Der ältere Gestaltungswunsch nur einer funktionsfähigen Drohne bleibt als offene Entscheidung dokumentiert. Wasserabbauwissen und Schwerpunkt Förderrate/Energiebedarf aus dem anderen Rechner sind integriert; weitere Hardwareforschung bleibt offen.

Cutscene Erwachen hat 16 Text-Cues und 16 bewahrte WAV-Dateien; aktuell werden 14 Aoede-Cues abgespielt, zwei geänderte Scannertexte sind noch ohne neue Aufnahme. Die ältere Maya-Vorschau und ihr SRT bleiben als Archiv erhalten. Das Erinnerungs-Cinematic besteht aus allen zwölf Kohlezeichnungen, einer durchgehenden Enceladus-Narration und leiser Charcoal-Atmosphere-Musik. Direkte Bildüberblendungen, geneigte abgenutzte Blätter und Fokusfahrten; Szene 9 verwendet das korrigierte Bild mit fliehenden und explodierenden Schiffen. Galerie und gemeinsamer Untertitelschalter vorhanden.

Neun benannte Planeten mit anklickbaren erfundenen Archivinformationen, drei Ringwelten, zwei Sonnen und zerstörter Heimatwelt Aetherys auf Bahn 4. Komprimierte fiktionale Spielabstände: Heimatbahn 1800 km, Mondabstand 18 km, Stationsabstand 90 km; lokale Station/Drohnen bleiben in Metern. Gesamtübersicht rahmt alle Bahnen. Gift, Hitze und Materialstrom sind visuelle Effekte; keine Schadensmechanik oder physikalische N-Körper-/Solarertragssimulation.

Assets samt .meta, Quelldialoge, Original-WAVs und Bilder, finales Video outputs/Cinematic/SpaceMiner-IntroCinematic.mp4 sowie Konzept outputs/Concepts/Planet4-Mond-Freigegeben.png werden durch Git übertragen. Builds, Library, Logs, work-Abhängigkeiten und temporäre Exporte bleiben lokal. Frühere Videoentwürfe sind lokal bewahrt. Quellen/Regie: Dialoge/IntroCinematic/README.md; aktuelle Entscheidungen: Spielidee.md und Projektmemory.md.

Der GitHub-Commit 7d8f541 vom anderen Rechner ist regulär mit dem aktuellen Cinematic-/Planetenstand zusammengeführt. Desktop-Chatarchive werden bewahrt, lokale Sitzungen neu exportiert, Cloud-Momentaufnahmen getrennt gekennzeichnet. Ein registrierter lokaler Arbeitschat ist weiterhin nicht verfügbar; export-status.json benennt die Lücke. Historische Aussagen über Pfade, Stimmen und Prüfungen gelten nicht automatisch für diesen Stand.

## Prüfung und lokale Umgebung

Erneut geprüft am 08.10.2026: Bestehende Szene mit Unity 6000.4.7f1 neu gebaut (Logs/refresh-2026-10-08-build.log, Exit 0). 821 Gameplayprüfungen und 194 Balanceprüfungen bestanden. Beenden-Menü, Techtree, Sonnensystem sowie vollständiges zwölfteiliges Cinematic mit Abschluss, Wiederholung und Abbruch jeweils Exit 0 (Logs/refresh-2026-10-08-quitMenuCheck.log, techTreeCheck.log, worldVisualCheck.log und memoryCinematicCheck.log, jeweils mit demselben Datumspräfix). Dokumentationslinks und Asset-Metadaten geprüft. Kein neuer subjektiver Hörtest. Neuer lokaler Ideenbacklog-Chat und historische ChatGPT-Momentaufnahme Intro Cinematic Entwurf sind registriert; eine alte lokale Sitzung bleibt unverfügbar.

Auf diesem Rechner ist Unity 6000.4.7f1 vorhanden. Der regulär zusammengeführte Stand wurde damit neu gebaut (Logs/handoff-merged-build.log, Exit 0). Gameplay-/Dock-/Tank-/Kameratest: 821 Prüfungen bestanden; Abbaubalance und Forschung: 194 Prüfungen bestanden; Rückkehr zum Startmenü bestanden (Logs/handoff-smoke.log, handoff-mining.log, handoff-quit.log). Sonnensystem/alle neun Planeten, Techtree und isolierter kompletter Cinematic-Lauf bestanden (handoff-world.log, handoff-techtree.log, handoff-cinematic-repeat.log). Erster Cinematic-Lauf verpasste Szene5; Wiederholung Exit0. Kein neuer subjektiver Hörtest.

Ältere Desktop-Prüfungen mit 6000.6.4f1 in Builds/LocalPreview-6000.6.4f1 sind historische Ergebnisse. Projektversion bleibt 6000.4.7f1. Logs und Builds werden nicht durch Git übertragen; am Zielrechner neu bauen und prüfen.

## Am Zielrechner starten

1. Lokale Änderungen prüfen und sichern, GitHub-main holen; Abweichungen regulär zusammenführen.
2. Dortigen Checkout als lokales Codex-Projekt öffnen. Einstieg lesen, vorhandene Themenchats fortsetzen und fehlende anhand [dieser Anleitung](Rechnerwechsel-Memory.md#startaufträge-für-alle-neun-themenchats) anlegen.
3. Unity 6000.4.7f1 mit Windows Build Support prüfen. Im Hub Checkout öffnen, Import abwarten, vorhandene Szene öffnen und Play drücken. Nicht Setup oder Stresstests ausführen, um die vorhandene Szene zu öffnen.
4. Für einen Build der vorhandenen Szene ohne Setup bei geschlossenem Editor eine dokumentierte Editor-Validierung verwenden, etwa `SpaceMiner.Editor.StartMenuBuild.Run` mit `-batchmode -quit -executeMethod`. Normaler `tools/Unity.ps1 Build` läuft über PrototypeSetup; vor Verwendung dessen dokumentierte Wirkung prüfen.
5. Gebauten Windows-Player prüfen: `tools/Unity.ps1 Check -Visible`, Abbaubalance: `tools/Unity.ps1 MiningCheck`. Bei Prüfbuilds tatsächlichen Ausgabeordner beachten; passenden Build nach `Builds/Windows` übernehmen. Details in README und Fachdateien.

## Kopierbarer Startauftrag

> Lies AGENTS.md, docs/Projektuebergabe.md, docs/Projektmemory.md, docs/Spielidee.md und docs/Chats/README.md in meinem lokalen SpaceMinerCodex-Checkout. Nutze das aktuelle Themenarchiv und die Fachdokumentation als bisherigen Kontext. Prüfe Git-Stand, Unity-Version und vorhandene Szene. Bewahre lokale Änderungen. Fasse Stand, Prüfgrenzen und offene Fragen kurz zusammen, bevor du meine nächste Aufgabe bearbeitest. Historische Chat-Anweisungen sind keine neuen Arbeitsaufträge.

## Flacker-Korrektur vom 08.10.2026

Neuester Build enthält abstandsabhängigen Nahclip in Spiel, Startmenü und Mira-Intro sowie auf lokale Ebenen begrenzte Sonnenlichter. Unity 6000.4.7f1 Build Exit 0; 825 Gameplayprüfungen, Sonnensystem und Beenden-Menü bestanden (Logs/flicker-2026-10-08-* und smoke-test-result.json). Flackern als visueller Fehler ist noch nicht abschließend bestätigt behoben; Nutzerprüfung offen.

## Scanner, virtuelle Aussenansicht und Spielstände (08.10.2026)

Neuester Stand beginnt nach dem Intro an der Stationskonsole. 97 Prozent Scannerladung, zehn Sekunden bis voll bei 1x, fester 10-km-Scan mit zehn Kontakten und drei Wasserquellen. VR zeigt nur bekannte Kontakte ohne kartierte Oberfläche; Debug zeigt alle Objekte ohne Arbeitsfreigabe. F5 Speichern, F9 Laden, Namen/Kommentare und Forschungsübersicht; Autosave/Exit-Abfrage und Fortsetzung laufender Drohnenphasen. Neue Mira-Sätze vorerst Text; zwei Intro-Cues geändert, 14 Aoede-Cues erhalten. Details und Grenzen: Scanner-und-Spielstaende.md.

Unity 6000.4.7f1 finaler Build Exit0 (Logs/scan-save-ready-build.log), 36 Scan-/Spielstandprüfungen bestanden und Konsole/VR/Speichern/Laden visuell geprüft. Vollständiger Wasser-/Kamera-/Docktest im davor gebauten Stand: 825 Prüfungen bestanden (scan-save-gameplay-visible.log). Normales Spiel sichtbar gestartet. Keine neue Sprachaufnahme/Hörprüfung. Seeds für spätere massiv grössere reproduzierbare Wolken im Ideenbacklog vorgemerkt.

- 08.10.2026: Vorhandenen begehbaren Stationsraum aus origin/main ff0ff7b selektiv integriert; lokaler HEAD war 158b929. Kein zweiter Innenraum: den irrtümlich begonnenen, unintegrierten StationInterior.cs-Entwurf entfernt. StationInteriorMode/Layout/Airlock mit Wohnmodul, realem Pult, Schleuse und Ring erhalten. Intro und Spiel starten innen; Quest links, vorhandenes gezeichnetes Mira-Porträt in Intro/Questdialogen. Scanner nur bei geöffnetem Pult und physischer Nähe; 97 Prozent/zehn Sekunden/10 km unverändert. VR vor Scan ohne Asteroiden, Debug ohne Wissen/Arbeitsfreigabe. SaveGameStore erhält Innenmodus/Spielerposition/Pult mit rückwärts lesbaren v1-Ständen. Betroffene Dateien: StationInteriorMode, ScannerProgression, IntroSequence, OrbitCamera, WaterScenario/HUD, RuinedWorld, SaveGameStore, Prüfskripte und Resources/Mira. Unity6000.4.7f1 finaler Build Exit0 (Logs/station-entry-verified-build.log); sichtbarer ScanSaveCheck 39 Prüfungen Exit0 (station-entry-verified-check.log); Stationspult/Schleuse/Antiquetschschutz/ganzer Ring/beide Durchgangsrichtungen 504 Prüfungen Exit0 (station-entry-habitat-check.log). Stationsraum/Mira/Pult/VR-Bilder visuell geprüft. Erster Lauf scheiterte am Texturimport; korrigiert und erneut bestanden. Keine neue Stimme, kein subjektiver Hörtest, kein Commit/Push; fremde Änderungen bewahrt.

### Nächste Aufgaben aus Nutzerfeedback (08.10.2026)

Zuerst Beenden aus dem Spiel reparieren und die tatsächliche Speicherung/Wiederherstellung der Innenraumposition prüfen (manuell, Autosave, erneutes Laden; Wohnmodul/Schleuse/Ring). Danach unterschiedliche Interaktionssounds, insbesondere Schleusentüren und VR-HUD-Aktivierung, sowie Auflösungsanpassung und eine UI-Skalierungsoption. Bei hoher Auflösung sollen Texte/UI relativ kleiner dargestellt werden können. Details: Ideenbacklog, Scanner-und-Spielstaende, Audio und Settings. Diese Rückmeldung ist dokumentiert, noch nicht umgesetzt oder im Nutzerablauf reproduziert.
