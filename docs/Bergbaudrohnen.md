# Sichtbarer Bergbauablauf

Stand 06.10.2026. `DroneAgent` steuert die Schritte, `MiningDroneVisual` stellt Werkzeuge und Materialbewegung dar. Die Animation folgt der Simulationszeit und bleibt bei Spielpause, pausierendem Settings-Menü und Intro stehen.

1. Die Drohne steckt rückwärts in ihrer Ladebucht und blickt zur freien Mittelgasse. Zunächst vorwärts ausfahren, oberhalb des Docks steigen und dann zur Eisquelle fliegen.
2. Anflug bis fünf Meter vor die Oberfläche und Bremsung. Nur der bearbeitete Asteroid bekommt vorübergehend seinen detaillierten LOD-0-Mesh als Collider, damit Werkzeuge und sichtbare Nahoberfläche übereinstimmen. Nach dem Abdocken wird der ursprüngliche Collider wiederhergestellt.
3. Andocken: auf 2,4 m Arbeitsabstand heranfahren und ausrichten, drei Klammerstreben ausfahren, anschließend den teleskopischen Bohrer ausfahren. Die Schuhe sind vom skalierten Teleskoparm getrennt und richten sich nach der Oberflächennormale. Ihr Mittelpunkt bleibt 9 cm vor der Oberfläche; die Greifbacken behalten ihre feste Größe.
4. Abbau: Bohrer dreht und vibriert; Eiskristalle spritzen vom Kontaktpunkt weg. Größere kantige Eisbrocken werden vom Gelenkarm durch die offene Klappe in den hohlen Frachtraum gelegt. Bis zu 24 sichtbare Stücke stellen den aktuellen Füllstand dar; die geschlossene Klappe besitzt ein Sichtfenster.
5. Abdocken: Bohrer und Klammer einziehen, bis fünf Meter zurücksetzen und zum Tank ausrichten.
6. Rückflug zum festen Tankanschluss, dann drehen und rückwärts in die Führungen fahren. Der hintere Frachtanschluss liegt beim Entladen genau am Tankempfänger. Erst bei vollständig gekoppelter Drohne öffnet sich die Ladeklappe. Die Stücke laufen durch den kurzen abgedeckten Übergabekanal; es gibt keinen freien Wurf zwischen Drohne und Tank. Die sichtbare Ladung nimmt beim Entladen ab, die Wasserbilanz wird wie bisher am Ende der Übergabe gebucht.
7. Tankkupplung lösen, oberhalb des Docks zurückfliegen, zur Mittelgasse absteigen und rückwärts in die eigene Ladebucht einparken. Erst danach beginnt die Versorgung; gegebenenfalls folgt der nächste Abbauflug.

Ein Dock am Modulanschluss enthält zwei gegenüberliegende Reihen mit je vier Ladebuchten. Die zehn vorhandenen Drohnen bleiben erhalten; zwei zusätzliche Wartungsbuchten nehmen die übrigen beschädigten Drohnen auf. `StationVisual` stellt die zehn Parkpositionen und den Tankanschluss bereit; `WaterScenario` ordnet die Drohnen diesen Positionen zu.

Ausparken, Andocken, Abdocken, Wenden sowie Tankankopplung/-lösung und Rückwärtsparken dauern jeweils 120 Spielsekunden, Entladen 240. Bei 100× entsprechen diese Schritte 1,2 bzw. 2,4 echten Sekunden. Ein sichtbarer Sammelzyklus dauert 120 Spielsekunden. Die Förderrate bleibt 0,1 kg/s, die Frachtkapazität 50 kg. Abbruch beim Abbau löst erst die Werkzeuge und liefert die Teilbeladung ab.

Die Eisstücke sind wiederverwendete grafische Stellvertreter, keine zusätzlichen Ressourcenobjekte. Masse und Wasser werden ausschließlich durch die vorhandene Förder-/Frachtlogik bilanziert. Gelenke, Andockmanöver und Transfer sind vereinfachte Animationen; Oberflächenverformung, vollständige Kollisionsvermeidung und physische Greiferbindungen sind weiterhin offen.

## Wiederhergestellter Dock-Stand, 07.10.2026

Die zusammenhaengende Stations-/Drohnenfassung aus origin/main 7d8f541 ist lokal integriert: StationVisual, DroneAgent, MiningDroneVisual, WaterScenario, MiningResearch und CargoGlass samt vorhandenen .meta-Dateien. Zwei Reihen mit je vier Ladebuchten und zwei Wartungsplaetzen; Drohnen stehen in den zugeordneten Berth-Posen, fahren aus und parken rueckwaerts ein. Am Tank koppelt der Frachtanschluss direkt an TankSocket; TankDocking/TankUndocking und geschlossene Uebergabe ersetzen den alten freien Eiswurf. Zugehoerige Energie-/Rueckkehrreserve und Abbau-Erfahrungslogik dieser Fassung wurden mit uebernommen, damit keine voneinander abweichenden Andocksubsysteme entstehen. Neuere Maya-Stimme, fremde Settings- und Techtree-Aenderungen des Remote-Commits wurden nicht pauschal eingespielt; lokale Aoede-Aufnahme und lokale UI bleiben erhalten.

PrototypeSmokeTest uebernimmt die neueren Dock-/Tank-/Ressourcenpruefungen und bewahrt die bisherige Intro-Pruefung fuer die lokale Stimme. In dieser Sitzung 821 Pruefungen bestanden, Exit0 (Logs/station-playable-smoke.log, smoke-test-result.json). Nachfolgend nur Kameraausrichtung/Pruefrender korrigiert; finaler Weltcheck erfolgreich (station-playable-world-check.log), Station mit neuem Dock visuell geprueft (outputs/Cinematic/station-lighting.png).

Darstellung: `MiningDroneVisual`, `StationVisual` und `Resources/CargoGlass.shader`. Ablauf: `DroneAgent` und `WaterScenario`. `PrototypeSmokeTest` prüft zusätzlich Parkpositionen, Ausparken, detaillierte Arbeitsoberfläche, Klammerabstand, sichtbare Füllung, bündigen Tankanschluss und Parken vor dem Laden. Nahaufnahmen: `dock-charging.png`, `dock-cargo.png` und `dock-tank-coupling.png` im jeweiligen lokalen `Logs`-Ordner.

Lokale Prüfung 06.10.2026: Mit Unity 6000.6.4f1 in der separaten Vorschaukopie erfolgreiche Development-/Release-Builds, Techtree-Datenprüfung und finaler Smoke-Test (`passed=true`, 819 Prüfungen, Prozess-Exit 0). Alle drei Nahaufnahmen visuell geprüft. Protokolle unter `Builds/LocalPreview-6000.6.4f1/Logs/drone-dock-build.log` und `drone-dock-check.log`. Gemeinsame Projektversion weiterhin 6000.4.7f1; auf dieser Version wurde hier kein Build durchgeführt.

## Beschlossene Richtung: Abbau-Erfahrung (06.10.2026)

Der Spieler soll einstellen können, welche Eigenschaft die Abbau-Erfahrung verbessert: Förderrate oder Energiebedarf beim Abbau. Die Begründung ist ein besseres Verständnis des bearbeiteten Materials. Förderrate beschreibt kg/s; die elektrische Abbauleistung beschreibt die Leistungsaufnahme in kW. Als Verbesserungsrichtungen dienen mehr kg/s bei gleicher Leistungsaufnahme bzw. weniger kW bei gleicher Förderrate. Eine höhere elektrische Leistungsaufnahme allein ist kein Erfahrungsbonus.

Ziel für die Versorgung: Die Drohne soll nur so lange abbauen, wie Energie und Treibwasser für die Rückkehr einschließlich Bremsen, Stationsmanövern und einstellbarer Sicherheitsreserve erhalten bleiben. Anfangs darf die Energie den Einsatz vor einem vollen Frachtraum beenden. Flug und Abbau sowie die Progressionsparameter sollen für Entwicklung und Simulation einstellbar sein.

Auf Nutzerauftrag als erste Testbalance implementiert. `MiningResearch` hält gemeinsames Wasserabbauwissen, getrennt von technischen Drohnengenerationen. Start bei Wissenslevel 1; eine produktive, vollständig beendete Fahrt zählt nach dem Einparken. Abgebrochene oder leere Fahrten zählen nicht. 20 Fahrten ergeben einen Levelaufstieg (5 Prozentpunkte Fortschritt pro Fahrt). Im Techtree bei „Wasserabbau optimieren“ wählt der Spieler den Schwerpunkt für den nächsten Aufstieg. Jeder Förderrate-Level multipliziert die Basisrate mit 1,15; jeder Effizienz-Level multipliziert die Abbauleistung mit 0,85. Erlernte Verbesserungen bleiben beim Wechsel erhalten. Level 5 benötigt vier Aufstiege, also 80 Fahrten. Diese Werte bleiben Testannahmen; Wissen wird beim Weltneustart zurückgesetzt, Spielstandspeicherung fehlt weiterhin.

`DroneAgent` berechnet den Rückkehrbedarf anhand der wachsenden Frachtmasse, aller Beschleunigungs-/Bremsstrecken, Bordversorgung und Stationsmanöver, mit konservativem 15-%-Aufschlag auf die geschätzte Flug-Treibwassermenge. Zusätzlich bleiben standardmäßig 10 % der Batterie- und Treibwasserkapazität geschützt. Die Fördermenge eines Simulationsschritts wird vor dem Verbrauch begrenzt, auch bei großen Schritten. Ein Start benötigt Versorgung für die gesamte Fahrt plus einen kleinen sinnvollen Abbau, nicht für einen vollen Frachtraum.

Testwerte: 0,1 kg/s Basisförderrate, 80 kW Abbauleistung, 0,05 kW Bordversorgung, 0,2 kWh Antriebsenergie pro Liter Treibwasser. Batterie 8 kWh, Treibwasser 10 L, Fracht 50 kg, Geschwindigkeit 5 m/s, Schub 5 N und Ladestation 2 kW bleiben erhalten. Diese Energiewerte sind einstellbare Spielannahmen, keine überprüfte technische Auslegung. Antriebsverbrauch fällt beim Beschleunigen und Bremsen an; Gleiten benötigt nur Bordversorgung. Die bestehenden kurzen Andockbewegungen sind weiterhin animiert und keine vollständige Antriebssimulation.

Spieltempo: 0,5×, 1×, 2×, 3× und 5×; Standard 1×. Pause merkt sich das vorherige Tempo. Settings → Developer enthält Förderrate, Abbau-/Bordleistung, Reserve, Batterie, Treibwasser, Fracht, Fluggeschwindigkeit, Schub, Antriebsenergie sowie Fahrten/Level und Levelbonus. Physische Parameter werden nur bei bereiter Drohne angewendet; Kapazitätsänderungen füllen die Testvorräte neu. Player-Settings speichern diese Entwicklungswerte nicht. Die übrigen Techtree-Forschungsfelder bleiben Entwürfe.

## Realismus als Auslegungsziel (06.10.2026)

Nutzerpriorität: Flug, Abbau, Versorgung und Weiterentwicklung sollen möglichst realistisch bleiben. Die bisherigen 80 kW Abbau und 0,2 kWh/L Antrieb sind weiterhin unvalidierte Testannahmen. Aus der bestandenen Spielsimulation folgt keine technische Machbarkeit dieser Auslegung.

Zur weiteren Auslegung vorgeschlagen, noch nicht als konkrete Umsetzung beschlossen: mechanisches Gewinnen und Transportieren von Eis vom thermischen Aufbereiten in der Station unterscheiden; Förderrate und Energiebedarf aus Material, Werkzeug und Wirkungsgrad gemeinsam ableiten. Das NASA-NIAC-Konzept [Aqua Factorem](https://www.nasa.gov/general/aqua-factorem-ultra-low-energy-lunar-water-extraction/) untersucht diese Trennung für eisführenden Mondregolith; dessen Leistungsprognosen sind weder ein Nachweis für unsere Asteroidendrohne noch unmittelbar übertragbar.

Aktuelle Größenordnung: 80 kW aus 8 kWh entsprechen 10C und ideal nur sechs Minuten Betrieb; 2 kW Laden entsprechen 0,25C und ideal vier Stunden von leer auf voll, ohne Verluste oder Ladeendphase. Höhere Ladeleistung setzt passende Zellen, Wärmeabfuhr und Stationsversorgung voraus. Die derzeitigen 2 kW Reaktor plus 0,5 kW Solar können höhere Ladeleistung nicht dauerhaft bereitstellen; Stationspuffer und Wechselakkus verschieben Wartezeiten, ersetzen aber keine Energieerzeugung. Als Herstellerbeispiel nennt der als Konzept gekennzeichnete [EaglePicher-SAR-10215-Entwurf](https://www.eaglepicher.com/sites/default/files/SAR%2010215%201120.pdf) getrennte Grenzen für Lade-, Dauerentlade- und Pulsstrom; diese sind kein Nachweis für ein konkretes Drohnenpaket.

Wärmehaushalt berücksichtigen: Im Vakuum gibt es keine Kühlung durch umgebende Luft; gespeicherte Abwärme muss über Wärmeleitung und letztlich Wärmestrahlung abgeführt werden. Siehe [NASA Thermal Control](https://www.nasa.gov/smallsat-institute/sst-soa/thermal-control/). Zu prüfen bleiben außerdem Verdampfung/Erwärmung des Treibwassers, Werkzeugreaktionskräfte und Verankerung sowie begrenzte, materialabhängige Lerngewinne statt unbegrenzt sinkender Leistungsaufnahme. Kleinere erste Lieferziele und eine ausdrücklich angezeigte Zeitraffung sind mögliche Spielgestaltungsoptionen; keine neue Zeitumrechnung oder Questgröße beschlossen.

Prüfung dieser Einordnung: NASA- und Herstellerquellen gelesen und mit den dokumentierten Spielparametern verglichen; keine Parameteränderung, kein neuer Build oder Laufzeittest.

## Beschlossenes Startziel und spätere Energie-Upgrades (06.10.2026)

Nach Vergleich mit Akkuwerkzeugen bestätigt der Nutzer ungefähr 1:2 als erste Auslegungsrichtung: beispielsweise 15 Minuten aktiver Werkzeugbetrieb und etwa 30 Minuten Nachladen der dafür verbrauchten Energie. Flug/Bordversorgung, Verluste und Ladeendphase zusätzlich bilanzieren. Orientierung liefern die veröffentlichten [STIHL-Arbeits-/Ladezeiten](https://www.stihl.co.uk/en/support/faq/battery-working-times), etwa MSA 220 mit AP 300 S bis 37 Minuten Arbeit und AL 501 mit 30/45 Minuten bis 80/100 %; dies ist ein Werkzeugvergleich, kein Machbarkeitsnachweis für Asteroidenabbau. Verhältnis aus Leistungsaufnahme und effektiver Ladeleistung ableiten, nicht als pauschale Wartezeit setzen. Keine absolute Abbauleistung oder neue Förderrate beschlossen; die bisherigen 80/2-kW-Testwerte und Messberichte bleiben bis zur neuen Auslegung unverändert.

Akku und Ladestation sollen in späteren Tiers deutlich verbessert werden können. Kapazität, zulässige Akku-Lade-/Entladeleistung und Stationsladeleistung getrennt entwickeln und mit Stationsversorgung/Wärmehaushalt abstimmen. Hardwareprogression bleibt getrennt von Materialerfahrung; konkrete Tierzuordnung und Verbesserungsfaktoren offen. Details in [Techtree](Techtree.md). Diese Entscheidung ist dokumentiert, noch keine neue Balance oder Tiermechanik implementiert.

## Gemessene Abbauzeiten (06.10.2026)

Simulation im tatsächlichen Windows-Spiel mit A-01 (80 % Wasser), einer Drohne und 0,25-s-Schritten, einschließlich Flug, Manövern, Entladen, Nachtanken und Batterieladen. Die Tabelle zeigt ungefähre echte Wartezeit ohne Intro, Bedienpausen und Menüpausen; Spielzeit wird durch das gewählte Tempo geteilt. Tank: von 20 auf 200 L, neun Lieferungen, erste Ladung 28,99 kg Eisgemisch. Treibwasser-Nachfüllung wird vom Stationstank abgezogen.

| Tempo | Tank voll | Erster Aufstieg (1 → 2) | Level 5, Förderrate | Level 5, Effizienz |
| --- | --- | --- | --- | --- |
| 0,5× | 67 h 00 min | 158 h 03 min | 653 h 34 min | 657 h 25 min |
| 1× | 33 h 30 min | 79 h 01 min | 326 h 47 min | 328 h 43 min |
| 2× | 16 h 45 min | 39 h 31 min | 163 h 23 min | 164 h 21 min |
| 3× | 11 h 10 min | 26 h 20 min | 108 h 56 min | 109 h 34 min |
| 5× | 6 h 42 min | 15 h 48 min | 65 h 21 min | 65 h 45 min |

Wichtige Grenze: Mit dem normalen 200-L-Tank endet der Auftrag vor dem ersten Levelaufstieg. Die Levelmessungen verwenden ausschließlich im Prüflauf einen 100.000-L-Tank für fortlaufende Lieferungen; keine neue Lager- oder Verbrauchsmechanik im normalen Spiel. Startvorrat, Quelle, Flug und Ladeleistung bleiben identisch. Gemessene Spielsekunden: Tank 120.605,5; erster Aufstieg 284.478,25; Level 5 Förderrate 1.176.413,25 und Effizienz 1.183.354,75. Der Abbau verbraucht etwa 90 % der Einsatzenergie, die anschließende Ladung mit 2 kW verlängert die Zyklen stark. Die aktuelle Testbalance ist entsprechend langsam.

Ergänzte Phasenmessung für dieselbe vollständige Tankquest (A-01, neun Lieferungen):

| Tätigkeit | Spielzeit / echte Zeit bei 1× | Anteil | Echte Zeit bei 5× |
| --- | --- | --- | --- |
| Akkuladen und paralleles Nachtanken | 28 h 33 min 34 s | 85,25 % | 5 h 42 min 43 s |
| Flug einschließlich Rückkehr zum Ladeplatz | 1 h 35 min 27 s | 4,75 % | 19 min 05 s |
| Abbau | 43 min 05 s | 2,14 % | 8 min 37 s |
| Ausparken, Andocken, Wenden, Entladen und Einparken | 2 h 38 min | 7,86 % | 31 min 36 s |

Exakte Phasenwerte im JSON: 102.814 s Versorgung, 5.726,75 s Flug, 2.584,75 s Abbau und 9.480 s Manöver/Entladen. Die bereits volle Startbatterie benötigt vor dem ersten Flug keine Ladezeit; zwischen den neun Lieferungen werden acht Ladezyklen gemessen. Nachtanken läuft gleichzeitig mit dem Laden und ist nach wenigen Sekunden beendet; die Versorgung wird durch das Laden begrenzt. Bei 80 kW Abbau und 2 kW Ladeleistung erfordert eine Minute Abbauenergie ungefähr 40 Minuten Ladezeit, zuzüglich weiterer Verbraucher.

Prüfung dieser ergänzten Messung: Phasenzähler in MiningBalanceCheck ergänzt; Development-/Release-Vorschau mit Unity 6000.6.4f1 erfolgreich gebaut. Verborgener Logiklauf mit 194 Prüfungen und Prozess-Exit 0 bestanden, Summe der Phasenzeiten entspricht in allen drei Durchläufen exakt der Gesamtzeit. Alle drei bisherigen Gesamtzeiten unverändert. Keine Gameplay-/Balanceparameter geändert und kein erneuter vollständiger Spieltest für diese reine Messergänzung. Bericht aktualisiert; aktuelle normale Spielversion bleibt unverändert.

Reproduzierbarer Bericht: [Abbau-Simulation](Simulationen/Abbau-2026-10-06.json). Mit aktueller Development-Version `tools/Unity.ps1 MiningCheck` (verborgene Logikprüfung) oder `tools/Unity.ps1 MiningCheck -Visible` (zusätzlich Menübilder). `MiningBalanceCheck` prüft Levelgrenzen, Schwerpunktwechsel, Reset, Startablehnung, Reserve bei großem Abbauschritt, 80 produktive Fahrten je Schwerpunkt, sichere Rückkehr und Wasserbilanz. Ergebnis 191 Prüfungen, Prozess-Exit 0. Vollständiger Spieltest: 821 Prüfungen, Prozess-Exit 0. Development-/Release-Builds mit Unity 6000.6.4f1 in separater Vorschaukopie erfolgreich; anschließend nur Menülayout korrigiert, erneut gebaut und Balance-/Bildprüfung bestanden. HUD, Tempoauswahl und Wissensoverlay einschließlich 140-%-Schrift/High Contrast visuell geprüft. Logs unter `Builds/LocalPreview-6000.6.4f1/Logs/mining-*`. Gemeinsame Projektversion weiterhin 6000.4.7f1, hier nicht geprüft.

## Darstellung beim Flug (07.10.2026)

Nach Nutzerrückmeldung flimmerte vor allem der fliegende Drohnenkörper. Die obere Fläche des hinteren Frachtkanalbodens lag exakt auf der Chassis-Oberseite (y = 0,40 m); der Kanalboden liegt jetzt darüber (Oberseite 0,44 m). MiningDroneVisual aktualisiert bewegliche Präsentationsteile vor der nachfolgenden Kamera. Die minimale Schattenreichweite der OrbitCamera beträgt nun 20 statt 80 m, damit die 6-m-Nahansicht feinere lokale Schatten erhält. Die bereits korrigierte Tiefenregel bleibt erhalten (6 cm Near Clip bei Drohnenfokus). Verbrauch, Flugbahn, Förderung und Ressourcenbilanz wurden nicht geändert.
StationInteriorCheck prüft einen echten Wasserflug nach dem Ausparken und weiteren 60 Spielsekunden. Zwölf aufeinanderfolgende Schritte erhalten den Kameraabstand ohne veraltete Folgeposition; Szenenbilder liegen unter Logs/Interior der jeweiligen Player-Arbeitsumgebung. Finale lokale 6000.6.4f1-Vorschau: 59 Innenraum-/Flugchecks und 821 vollständige Spielchecks mit Exit 0. Darstellungen im Flug und Innenraum visuell gelesen; subjektive Beurteilung verbleibenden Flimmerns durch den Nutzer noch offen.
