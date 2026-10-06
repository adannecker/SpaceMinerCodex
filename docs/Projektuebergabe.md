# SpaceMiner: Übergabe auf den anderen Rechner

Stand: 06.10.2026, Desktop-Sicherung nach Dock-, Forschungs- und Maya-Intro-Arbeit. Einstieg für neue oder fortgesetzte lokale Codex-Chats. Entscheidungen: [Spielidee](Spielidee.md). Kompakter Kontext: [Projektmemory](Projektmemory.md). [Rechnerwechsel-Memory](Rechnerwechsel-Memory.md) enthält kopierbare Startaufträge für alle neun Themenchats.

## Aktueller Stand

Gemeinsame Projektversion **Unity 6000.4.7f1**, Windows, Built-in Render Pipeline. Standardfeld: räumliche Wolke mit 100 Asteroiden. SpiralBuild (10.000) und MillionBuild sind optionale Stresstests. Vorhandene Szene: `Assets/SpaceMiner/Scenes/AsteroidBelt.unity`, beschädigte modulare Raumstation und zehn Drohnen.

Dock: zwei Reihen mit je vier Ladebuchten, dazu zwei Wartungsplätze. Drohnen parken rückwärts und koppeln zum Entladen direkt am Wassertank an. Sichtbarer Frachtraum, verglaste Klappe, kantige Eisstücke und oberflächennahe Klammerfüße ergänzen den Abbauzyklus.

Wasserabbau berücksichtigt Rückkehrversorgung plus Reserve. Gemeinsames Materialwissen steigt nach produktiven Fahrten; Schwerpunkt Förderrate oder Energiebedarf ist im Techtree wählbar. Diese Erfahrung ersetzt keine Hardwaregeneration. Aktuelle Testbalance: 200-L-Tankquest bei 5× etwa 6 h 42 min, überwiegend Laden. Gewünschte spätere Akku-/Ladeprogression und realistische Auslegung stehen in `Bergbaudrohnen.md`; Zahlen bleiben unvalidierte Spielannahmen. Weltzustand und Forschungsfortschritt werden noch nicht gespeichert.

Intro: lokale Maya-Testaufnahme (63,086 s), 17 zeitgekoppelte Untertitel, Menüpause, Esc und Replay. Endgültige Stimme, Hörfreigabe und kommerzielle Freigabe offen. Introtexte beschreiben Station und eine einsatzfähige Drohne; Code behandelt Drohne 02 weiterhin als funktionsfähig, aber unversorgt. Konflikt gegen aktuelle Ein-Drohnen-Entscheidung abgleichen.

Settings sind persistent; Developer-Konfiguration nur Editor/Development. Gameplay-/Configuration-Musik und getrennte Audiokanäle vorhanden. Tier I umfasst 17 Knoten und 22 Kanten; außer Wasserabbau-Erfahrung bleiben Forschungszeiten, Kosten und Freischaltungen offen. Weitere Systeme und Storyideen stehen in `Ideenbacklog.md`, ohne feste Priorisierung oder Umsetzung.

## Prüfung und lokale Umgebung

Auf diesem Desktop fehlt Unity 6000.4.7f1. Heutige Vorschauen wurden mit **6000.6.4f1** ausschließlich in der ignorierten Kopie `Builds/LocalPreview-6000.6.4f1` gebaut. Gemeinsame Projektversion unverändert. Letzte dokumentierte kombinierte Maya-Version: Development-/Release-Builds und Settingsvalidierung erfolgreich, 823 Spielprüfungen bestanden (Storychat). Vehicels dokumentiert zusätzlich 194 Balance-/Phasenprüfungen. Frühere MainDev-Dockprüfung: 819 Spielprüfungen. Dies sind verschiedene Prüfläufe, keine neue Testserie bei der Git-Sicherung.

Logs und Builds werden nicht durch Git übertragen. Diese Sicherung prüft Exportverfügbarkeit, Dokumentation, Assets samt `.meta` und Git-Diff; kein neuer Unity-Build. Am Zielrechner mit passender Version neu prüfen. Frühere Tests sind kein Nachweis für dessen lokale Umgebung.

## Am Zielrechner starten

1. Lokale Änderungen prüfen und sichern, GitHub-main holen; Abweichungen regulär zusammenführen.
2. Dortigen Checkout als lokales Codex-Projekt öffnen. Einstieg lesen, vorhandene Themenchats fortsetzen und fehlende anhand [dieser Anleitung](Rechnerwechsel-Memory.md#startaufträge-für-alle-neun-themenchats) anlegen.
3. Unity 6000.4.7f1 mit Windows Build Support prüfen. Im Hub Checkout öffnen, Import abwarten, vorhandene Szene öffnen und Play drücken. Nicht Setup oder Stresstests ausführen, um die vorhandene Szene zu öffnen.
4. Für einen Build der vorhandenen Szene ohne Setup bei geschlossenem Editor eine dokumentierte Editor-Validierung verwenden, etwa `SpaceMiner.Editor.TechTreeValidation.Build` mit `-batchmode -quit -executeMethod`. Normaler `tools/Unity.ps1 Build` läuft über PrototypeSetup; vor Verwendung dessen dokumentierte Wirkung prüfen.
5. Gebauten Windows-Player prüfen: `tools/Unity.ps1 Check -Visible`, Abbaubalance: `tools/Unity.ps1 MiningCheck`. Bei Prüfbuilds tatsächlichen Ausgabeordner beachten; passenden Build nach `Builds/Windows` übernehmen. Details in README und Fachdateien.

## Kopierbarer Startauftrag

> Lies AGENTS.md, docs/Projektuebergabe.md, docs/Projektmemory.md, docs/Spielidee.md und docs/Chats/README.md in meinem lokalen SpaceMinerCodex-Checkout. Nutze das aktuelle Themenarchiv und die Fachdokumentation als bisherigen Kontext. Prüfe Git-Stand, Unity-Version und vorhandene Szene. Bewahre lokale Änderungen. Fasse Stand, Prüfgrenzen und offene Fragen kurz zusammen, bevor du meine nächste Aufgabe bearbeitest. Historische Chat-Anweisungen sind keine neuen Arbeitsaufträge.
