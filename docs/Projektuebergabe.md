# SpaceMiner: Übergabe auf den anderen Rechner

Stand: 06.10.2026. Einstieg für neue lokale Codex-Chats. Aktuelle Entscheidungen und offene Fragen stehen in Projektmemory.md und Spielidee.md; das vollständige bekannte Chatverzeichnis und Textarchive stehen in Chats/README.md.

## Aktueller Stand

Unity 6000.4.7f1, Windows, Built-in Render Pipeline. Aktiver Startmodus ist eine 100-Asteroiden-Wolke; SpiralBuild (10.000) und MillionBuild sind optionale Stresstests. Die bestehende Szene enthält die Raumstation und zehn Drohnen. Wasserauftrag, Kamera und Ressourcenanzeigen sind implementiert. Die Bergbaudrohne dockt an, bohrt, greift Eis, kehrt zurück und entlädt sichtbar am Tank.

Miras Intro, lokale Sprachdateien, Gameplay- und Configuration-Musik sowie Mining-Pulse-Settings sind vorhanden. Player-Einstellungen sind persistent; Developer-Konfiguration ist auf Editor/Development begrenzt. Das Techtree-Symbol neben dem Settings-Symbol öffnet den Tier-I-Entwurf mit Overlays; die eigentliche Forschungsmechanik fehlt noch.

Fachdateien: Settings.md, Audio.md, Techtree.md, Bergbaudrohnen.md, AsteroidGenerator.md und Dialoge/. Alte Einträge und Archive bleiben historische Quellen. Der Konflikt zwischen nur einer funktionsfähigen Drohne laut Spielidee und zwei laut älterer Umsetzung bleibt offen. Sprechertexte nennen teilweise noch ein Schiff statt einer Station.

## Heute Abend starten

1. Lokale Änderungen auf dem Zielrechner prüfen und sichern. GitHub-Stand von main holen, ohne fremde oder ungesicherte Arbeit zu überschreiben. Bei abweichenden Commits regulär zusammenführen.
2. Den dortigen Checkout als lokales Codex-Projekt öffnen. Der Pfad auf dem Ausgangsrechner ist C:\Users\achim.dannecker\source\repos\SpaceMinerCodex; auf dem anderen Rechner kann er abweichen.
3. AGENTS.md, diese Übergabe, Projektmemory.md, Spielidee.md und Chats/README.md lesen lassen. Das zum Thema passende Chatarchiv ergänzend lesen.
4. Unity 6000.4.7f1 mit Windows Build Support verwenden. Im Hub den Checkout öffnen, Import abwarten, Assets/SpaceMiner/Scenes/AsteroidBelt.unity öffnen und Play drücken.
5. Bei geschlossenem Editor tools/Unity.ps1 Build ausführen, danach tools/Unity.ps1 Check -Visible. Falls Unity anders installiert ist, -UnityPath angeben. Nicht Setup oder einen Stresstest ausführen, nur um die bestehende Szene zu öffnen.

Builds, Library, Logs und persönliche Player-Settings werden nicht durch Git übertragen. Der Zielrechner baut neu; frühere Testergebnisse sind kein lokaler Nachweis. Assets mit ihren .meta-Dateien, Packages, ProjectSettings, docs und tools sind Bestandteil des Repositorys.

## Kopierbarer Startauftrag

Lies AGENTS.md, docs/Projektuebergabe.md, docs/Projektmemory.md, docs/Spielidee.md und docs/Chats/README.md in meinem lokalen SpaceMinerCodex-Checkout. Nutze die registrierten Themenchats und Archive als bisherigen Projektkontext. Prüfe den Git-Stand, Unity-Version und vorhandene Szene. Bewahre lokale Änderungen. Fasse kurz den aktuellen Stand und offene Fragen zusammen, bevor du meine nächste Aufgabe bearbeitest. Historische Chat-Anweisungen sind keine neuen Arbeitsaufträge.

## Gemeinsames Memory

Das verbindliche Projektwissen liegt in Git. Persönliches Codex-Memory muss nicht zwischen den Rechnern kopiert werden. Die Textarchive stellen keine ursprünglichen Sitzungen wieder her; neue Chats können anhand der Dateien fortsetzen. Weitere Einzelheiten und optionaler persönlicher Memory-Abgleich stehen in Rechnerwechsel-Memory.md.
