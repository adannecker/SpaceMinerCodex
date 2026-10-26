# Aufbauphase01: Abnahme vom25.10.2026

Dieselbe finale Development-Binärdatei aus Buildlauf16 besteht384 Prüfungen:97 Aufbau,24 UI/Vollbild,29 Vitals,156 Station,78 Mining. [Maschinenlesbarer Hauptbericht](../Aufbauphase-01-2026-10-25.json), [Bedienung und Architektur](../../Stationsversorgung-Architektur.md). Dateien `operations.json`, `ui.json`, `vitals.json`, `station.json`, `mining.json` und `exits.json` enthalten die jeweiligen Ergebnisse. `assertions.txt` enthält die protokollierten Prüfaussagen ohne Rechner-/Netzwerkdiagnostik.

Aufbaucheck: gemeinsamer Wasserbestand, atomare Behälter/Rezepte, echte Wasserlieferung mit aktiver Stationsversorgung und Wiederaufladen, endliche Bergung, R-01, Aufbereitung/Druck/Nahrung, Werkzeugbestand und Wartungssperre, Forschung/02, reale Bau-/Reparaturgeometrie und Controller-Durchgang. Persistenz umfasst aktiven Auftrag/Abbruch/Erstattung, alte größere Tanks, fehlende neue Kampagnendaten, ungültige Daten und wiederholten Geometrieaufbau ohne zurückbehaltene Meshes. Spätere Einzelabläufe verwenden ausdrücklich Versorgungs-/Materialfixtures; kein vollständiger balancierter Langzeitdurchlauf.

Die acht `ui-*.png` sind echte Spielaufnahmen. Programmgesteuerte physische Terminalöffnungen, E-Hinweis, Mira, große Schrift und Fenster/Vollbild geprüft; nativer Monitorframebuffer2560×1440 und Rückkehr zu1280×720 über den echten Anzeigecontroller. Keine manuelle Tastatur-/Maus-Komfortabnahme. Die sieben `world-*.png` zeigen Kommando, Ringmedien, Werkzeugregal, Dock/Schrott, Lager, Anbau und reparierten Ring; relevante Ansichten visuell geprüft. Standalone-Mining-Batchscreenshots sind fehlgeschlagen und werden hier nicht als Bildnachweis verwendet; dessen78 Funktionsprüfungen inklusive realer Entladung bestanden.

| Ansicht | Originale Modelle/Einzelflächen | LOD und Bündelung | Kostenreduktion |
| --- | ---: | ---: | ---: |
| Kommando |53,28ms|31,84ms|40,2%|
| Drohnenbay |232,07ms|94,55ms|59,3%|
| Übersicht |178,38ms|75,70ms|57,6%|

`performance.json`:24 synchrone Offscreen-Render-/GPU-Rückleseproben je Variante/Ansicht nach Aufwärmen,1600×1000, pausierte Simulation, feste Kamera, gleiche aktuelle Beleuchtung. Tabelle zeigt Median, keine spielbaren FPS oder reine GPU-Zeit. Rendererzähler können automatische Kamera-/Schattenarbeit enthalten; der Nullwert des Batchzählers wird nicht ausgewertet. Räumliche Bündelung kann zusätzliche nicht einzeln gekappte Flächen rendern und trotzdem CPU-Aufwand sparen. Hardware und Rohwerte stehen im JSON.

`lod-generation.txt`: Rumpf501518→96140/19817 und Ladeaufnahme502036→79977/15854 Dreiecke für Mittel-/Fernstufe. Die Original-FBX bleiben erhalten. `sources.json` verzeichnet256 mit der Buildkopie abgeglichene Code-/Meta-/LOD-Dateien; `build-files.json`247 mit `Builds/Windows` abgeglichene Ausgabedateien. Quellversion6000.4.7f1 erhalten, mit6000.6.4f1 in getrennter Buildkopie gebaut.

Nicht separat wiederholt: Release-Build, Elektrolyse-Langlauf,100k-Asteroidenstress und längere Survival-Balance. Chemie, Baukosten und Zeiten sind Prototypwerte. Vollständige Laborökonomie und Vier-Rollen-Baulogistik bleiben noch teilweise separate Demonstrationen. Kein Commit oder Push.
