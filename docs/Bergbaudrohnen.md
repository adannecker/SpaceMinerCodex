# Sichtbarer Bergbauablauf

Stand 06.10.2026. `DroneAgent` steuert die Schritte, `MiningDroneVisual` stellt Werkzeuge und Materialbewegung dar. Die Animation folgt der Simulationszeit und bleibt bei Spielpause, pausierendem Settings-Menü und Intro stehen.

1. Anflug bis fünf Meter vor die per Collider ermittelte Oberfläche und Bremsung.
2. Andocken: auf 2,4 m Arbeitsabstand heranfahren und ausrichten, drei Klammerstreben mit Greifbacken ausfahren, anschließend den teleskopischen Bohrer zur tatsächlichen Oberfläche ausfahren.
3. Abbau: Bohrer dreht und vibriert; kleine Eiskristalle spritzen vom Kontaktpunkt weg. Wiederholt entstehen größere Eisbrocken. Der Gelenkarm greift sie, schwenkt zur offenen Frachtluke und legt sie hinein.
4. Abdocken: Bohrer und Klammer einziehen, bis fünf Meter zurücksetzen und gleichmäßig zum Tank ausrichten.
5. Rückflug zum oberen Entladeanschluss des Wasser-/Eistanks. Die Frachtluke öffnet sich, Eisbrocken wandern in den sichtbaren Tankanschluss. Nach dem Entladen wird der Wasseranteil wie bisher dem Stationsvorrat gutgeschrieben.
6. Oberhalb des Stationsrings zum Ladeplatz fliegen, Batterie laden und Treibwasser ergänzen; gegebenenfalls den nächsten Abbauflug starten.

Andocken, Abdocken und Wenden dauern jeweils 120 Spielsekunden, Entladen 240. Bei 100× entsprechen diese Schritte 1,2 bzw. 2,4 echten Sekunden. Ein sichtbarer Sammelzyklus dauert 120 Spielsekunden. Die Förderrate bleibt 0,1 kg/s, die Frachtkapazität 50 kg. Abbruch beim Abbau löst erst die Werkzeuge und liefert die Teilbeladung ab.

Die Eisstücke sind wiederverwendete grafische Stellvertreter, keine zusätzlichen Ressourcenobjekte. Masse und Wasser werden ausschließlich durch die vorhandene Förder-/Frachtlogik bilanziert. Gelenke, Andockmanöver und Transfer sind vereinfachte Animationen; Oberflächenverformung, vollständige Kollisionsvermeidung und physische Greiferbindungen sind weiterhin offen.

## Wiederhergestellter Dock-Stand, 07.10.2026

Die zusammenhaengende Stations-/Drohnenfassung aus origin/main 7d8f541 ist lokal integriert: StationVisual, DroneAgent, MiningDroneVisual, WaterScenario, MiningResearch und CargoGlass samt vorhandenen .meta-Dateien. Zwei Reihen mit je vier Ladebuchten und zwei Wartungsplaetzen; Drohnen stehen in den zugeordneten Berth-Posen, fahren aus und parken rueckwaerts ein. Am Tank koppelt der Frachtanschluss direkt an TankSocket; TankDocking/TankUndocking und geschlossene Uebergabe ersetzen den alten freien Eiswurf. Zugehoerige Energie-/Rueckkehrreserve und Abbau-Erfahrungslogik dieser Fassung wurden mit uebernommen, damit keine voneinander abweichenden Andocksubsysteme entstehen. Neuere Maya-Stimme, fremde Settings- und Techtree-Aenderungen des Remote-Commits wurden nicht pauschal eingespielt; lokale Aoede-Aufnahme und lokale UI bleiben erhalten.

PrototypeSmokeTest uebernimmt die neueren Dock-/Tank-/Ressourcenpruefungen und bewahrt die bisherige Intro-Pruefung fuer die lokale Stimme. In dieser Sitzung 821 Pruefungen bestanden, Exit0 (Logs/station-playable-smoke.log, smoke-test-result.json). Nachfolgend nur Kameraausrichtung/Pruefrender korrigiert; finaler Weltcheck erfolgreich (station-playable-world-check.log), Station mit neuem Dock visuell geprueft (outputs/Cinematic/station-lighting.png).
