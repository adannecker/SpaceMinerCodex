# SpaceMiner: Übergabe auf den anderen Rechner

Stand: 06.10.2026, Desktop-Sicherung nach Dock-, Forschungs- und Maya-Intro-Arbeit. Einstieg für neue oder fortgesetzte lokale Codex-Chats. Entscheidungen: [Spielidee](Spielidee.md). Kompakter Kontext: [Projektmemory](Projektmemory.md). [Rechnerwechsel-Memory](Rechnerwechsel-Memory.md) enthält kopierbare Startaufträge für alle neun Themenchats.

## Aktueller Stand am 07.10.2026

Unity 6000.4.7f1, Windows, Built-in Render Pipeline. Bestehende Szene AsteroidBelt.unity mit 100 Asteroiden; größere Felder bleiben optionale Stresstests. Keine neue Projektkopie anlegen. Startmenü, Wasserauftrag und Kamera sind implementiert. Beenden im Spiel kehrt zum pausierten Startbildschirm zurück; Anwendungsschliessen erfolgt dort.

Die neue Station hat acht Ladebuchten, zwei Wartungsplätze und Tankkupplung. Drohnen parken rückwärts und koppeln zur geschlossenen Wasserübergabe am Tank an. Drohne 01 startet bereit, 02 wartet auf Ladung, acht sind beschädigt. Der ältere Gestaltungswunsch nur einer funktionsfähigen Drohne bleibt als offene Entscheidung dokumentiert. Wasserabbauwissen und Schwerpunkt Förderrate/Energiebedarf aus dem anderen Rechner sind integriert; weitere Hardwareforschung bleibt offen.

Cutscene Erwachen verwendet die freigegebene Aoede-Stimme mit 16 lokalen WAV-Cues. Die ältere Maya-Vorschau und ihr SRT bleiben als Archiv erhalten. Das Erinnerungs-Cinematic besteht aus allen zwölf Kohlezeichnungen, einer durchgehenden Enceladus-Narration und leiser Charcoal-Atmosphere-Musik. Direkte Bildüberblendungen, geneigte abgenutzte Blätter und Fokusfahrten; Szene 9 verwendet das korrigierte Bild mit fliehenden und explodierenden Schiffen. Galerie und gemeinsamer Untertitelschalter vorhanden.

Neun benannte Planeten mit anklickbaren erfundenen Archivinformationen, drei Ringwelten, zwei Sonnen und zerstörter Heimatwelt Aetherys auf Bahn 4. Komprimierte fiktionale Spielabstände: Heimatbahn 1800 km, Mondabstand 18 km, Stationsabstand 90 km; lokale Station/Drohnen bleiben in Metern. Gesamtübersicht rahmt alle Bahnen. Gift, Hitze und Materialstrom sind visuelle Effekte; keine Schadensmechanik oder physikalische N-Körper-/Solarertragssimulation.

Assets samt .meta, Quelldialoge, Original-WAVs und Bilder, finales Video outputs/Cinematic/SpaceMiner-IntroCinematic.mp4 sowie Konzept outputs/Concepts/Planet4-Mond-Freigegeben.png werden durch Git übertragen. Builds, Library, Logs, work-Abhängigkeiten und temporäre Exporte bleiben lokal. Frühere Videoentwürfe sind lokal bewahrt. Quellen/Regie: Dialoge/IntroCinematic/README.md; aktuelle Entscheidungen: Spielidee.md und Projektmemory.md.

Der GitHub-Commit 7d8f541 vom anderen Rechner ist regulär mit dem aktuellen Cinematic-/Planetenstand zusammengeführt. Desktop-Chatarchive werden bewahrt, lokale Sitzungen neu exportiert, Cloud-Momentaufnahmen getrennt gekennzeichnet. Ein registrierter lokaler Arbeitschat ist weiterhin nicht verfügbar; export-status.json benennt die Lücke. Historische Aussagen über Pfade, Stimmen und Prüfungen gelten nicht automatisch für diesen Stand.

## Prüfung und lokale Umgebung

Auf diesem Rechner ist Unity 6000.4.7f1 vorhanden. Der regulär zusammengeführte Stand wurde damit neu gebaut (Logs/handoff-merged-build.log, Exit 0). Gameplay-/Dock-/Tank-/Kameratest: 821 Prüfungen bestanden; Abbaubalance und Forschung: 194 Prüfungen bestanden; Rückkehr zum Startmenü bestanden (Logs/handoff-smoke.log, handoff-mining.log, handoff-quit.log). Sonnensystem/alle neun Planeten, Techtree und isolierter kompletter Cinematic-Lauf bestanden (handoff-world.log, handoff-techtree.log, handoff-cinematic-repeat.log). Erster Cinematic-Lauf verpasste Szene5; Wiederholung Exit0. Kein neuer subjektiver Hörtest.

Ältere Desktop-Prüfungen mit 6000.6.4f1 in Builds/LocalPreview-6000.6.4f1 sind historische Ergebnisse. Projektversion bleibt 6000.4.7f1. Logs und Builds werden nicht durch Git übertragen; am Zielrechner neu bauen und prüfen.

## Am Zielrechner starten

1. Lokale Änderungen prüfen und sichern, GitHub-main holen; Abweichungen regulär zusammenführen.
2. Dortigen Checkout als lokales Codex-Projekt öffnen. Einstieg lesen, vorhandene Themenchats fortsetzen und fehlende anhand [dieser Anleitung](Rechnerwechsel-Memory.md#startaufträge-für-alle-neun-themenchats) anlegen.
3. Unity 6000.4.7f1 mit Windows Build Support prüfen. Im Hub Checkout öffnen, Import abwarten, vorhandene Szene öffnen und Play drücken. Nicht Setup oder Stresstests ausführen, um die vorhandene Szene zu öffnen.
4. Für einen Build der vorhandenen Szene ohne Setup bei geschlossenem Editor eine dokumentierte Editor-Validierung verwenden, etwa `SpaceMiner.Editor.TechTreeValidation.Build` mit `-batchmode -quit -executeMethod`. Normaler `tools/Unity.ps1 Build` läuft über PrototypeSetup; vor Verwendung dessen dokumentierte Wirkung prüfen.
5. Gebauten Windows-Player prüfen: `tools/Unity.ps1 Check -Visible`, Abbaubalance: `tools/Unity.ps1 MiningCheck`. Bei Prüfbuilds tatsächlichen Ausgabeordner beachten; passenden Build nach `Builds/Windows` übernehmen. Details in README und Fachdateien.

## Kopierbarer Startauftrag

> Lies AGENTS.md, docs/Projektuebergabe.md, docs/Projektmemory.md, docs/Spielidee.md und docs/Chats/README.md in meinem lokalen SpaceMinerCodex-Checkout. Nutze das aktuelle Themenarchiv und die Fachdokumentation als bisherigen Kontext. Prüfe Git-Stand, Unity-Version und vorhandene Szene. Bewahre lokale Änderungen. Fasse Stand, Prüfgrenzen und offene Fragen kurz zusammen, bevor du meine nächste Aufgabe bearbeitest. Historische Chat-Anweisungen sind keine neuen Arbeitsaufträge.
