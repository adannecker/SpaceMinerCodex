# Drohne: Frachtvolumen, Wasserantrieb und Akku — 25.10.2026

**Vollständige Eisannahme und Kaltlager, 25.10.2026:** Die nachfolgende 90-%-Drohnenauslegung ist jetzt mit der [kompletten Anlage](Eislager-Slot01-Auslegung-2026-10-25.md) verbunden. Der volle tatsächliche A01-Umlauf liefert 1033,326 kg Rohladung mit dem normalen 200-L-Verbrauchspuffer. Ergebnis:4719s Simulationszeit,4,406 kWh/7,308 L am Heimdock; diese Vorräte liegen über der geschützten Reserve. Rund 826,661 L Wasserpotenzial sind erst nach gesonderter Aufbereitung verfügbar. Auch eine Rückkehr mit über 1 t Restladung bei blockierter Annahme wurde tatsächlich geflogen. [Finaler Nachweis mit Fixtures und Grenzen](Pruefungen/Eisannahme-und-Kaltlager-2026-10-25.json). Hardware und 34min 27s Basis-Schürfzeit unten bleiben aktuell;225 kg-Kleinauftrag und 2000 L-Empfangsfixture beschreiben ausschließlich die frühere Abnahme.

## Umgesetzte 90-%-Testauslegung, 25.10.2026

Nach der folgenden Analyse hat der Nutzer **90 % Füllziel und die Umsetzung einschließlich sinnvoller Schürfzeit** bestätigt. Die unten stehenden 50-kg-/8-kWh-/10-L-Zahlen beschreiben die frühere Hardware. Die neue Auslegung verbindet Frachtvolumen und mechanische Lastgrenze; sie ist eine nachvollziehbare Spielauslegung, keine zertifizierte Bergbaudrohne.

| Größe | Neuer Standard |
| --- | ---: |
| Trockene Drohne einschließlich Hardware | 250 kg |
| Rechteckiger Fracht-Prüfbereich | 1,458 m³ |
| Maximale betriebliche Belegung | 90 % = 1,3122 m³ |
| Feststoffanteil der Schüttung | 75 %; verbleibender Anteil Hohlräume |
| Mechanische Lastgrenze | 1200 kg |
| Akku / Treibwasser | 16 kWh / 15 L |
| Reisegeschwindigkeit / Schub | 2 m/s / 5 N |
| Effektive Ausströmgeschwindigkeit | 1000 m/s |
| Elektrische Heizer-Auslegung | 1,2 kWh pro L verbrauchtem Treibwasser |
| Basis-Förderrate / Werkzeugleistung | 0,5 kg/s / 4 kW |
| Bordversorgung / geschützte Reserve | 50 W / je 10 % Akku und Treibwasser |

Für Wasser-Massenanteil `w` ergibt sich die Feststoffdichte aus `1 / (w/917 + (1-w)/2500)` kg/m³. Das Frachtziel ist das kleinere aus mechanischer Lastgrenze und `1,458 × 0,90 × 0,75 × Feststoffdichte`. **90 % geometrische Belegung und 75 % Packung werden getrennt gerechnet.** 917 kg/m³ Eis und vorläufig 2500 kg/m³ Restmaterial sowie die rechteckige Hüllgeometrie bleiben Auslegungsannahmen.

| Material | Maximale Rohladung | Basis-Schürfzeit | Werkzeugenergie |
| --- | ---: | ---: | ---: |
| Reines Eis | 902,47 kg | 30 min 5 s | 2,01 kWh |
| 80 % Wasser / 20 % Rest, nach Masse | 1033,33 kg | 34 min 27 s | 2,30 kWh |
| 65 % Wasser / 35 % Rest, nach Masse | 1159,42 kg | 38 min 39 s | 2,58 kWh |
| Erste 180-L-Lieferung in den bestehenden Tank, 80/20 | 225 kg | 7 min 30 s | 0,50 kWh |

Die Schürfzeit folgt der real gewonnenen Rohmasse: `Masse / Förderrate`. Werkzeugenergie: `Zeit × 4 kW`; 50 W Bordlast kommen hinzu. Die angenommene mechanische Gewinnung entspricht **8 kJ/kg bzw. 2,22 kWh/t**. Das ist eine erste Auslegung für bereits gebrochenes, zugängliches Eisgestein mit bestehender Verankerung, keine materialunabhängig nachgewiesene Gesteinsfräsleistung. Härte, Zerkleinerung, Greiferleistung und spätere Werkzeugforschung müssen künftig materialabhängige Raten liefern. Eine tatsächliche Schmelz-/Reinigungsanlage ist darin nicht enthalten. Bei Erfahrung ändern sich Rate bzw. Leistung nach dem vorhandenen Forschungsmodell.

**Flugbilanz:** Das Spiel berücksichtigt wachsende Masse, Beschleunigung und Bremsen pro Flugabschnitt, elektrische Heizerenergie sowie Bordlast. Hinflug wird leer, Rückflug bis zum Tank beladen und der Weg Tank → Dock mit tatsächlicher Restladung budgetiert. Die Rückkehrabschätzung enthält 15 % Treibwasseraufschlag zusätzlich zur geschützten 10-%-Kapazitätsreserve. Manöver im Nahbereich bleiben vereinfachte Animationen; es gibt keine vollständige Orbital-, Asteroidengravitations- oder Wärmeabfuhrsimulation. Die NASA-Quellen weiter unten stützen Raketengleichung und Wasserantrieb-Größenordnung, nicht die Skalierung dieser konkreten Hardware.

Die unabhängige Rechnung in `tools/AssessDroneSortie.py` liefert für je 10 km Hin- und Rückweg und vier ideale Brennphasen bei 80/20-Ladung rund **6,24 L und 9,96 kWh insgesamt**, ohne zusätzliche Dock-/Ausweichmanöver. 5 m/s wären mit derselben Ladung und Hardware bereits zu teuer; deshalb 2 m/s als Anfangswert. [Rechenwerte und Annahmen](Pruefungen/Drohnen-90-Prozent-Rechnung-2026-10-25.json). Der tatsächliche Stationsflug mit Wegpunkten wird separat im Spiel geprüft.

**Laden und Empfang:** Der bisherige 200-L-Verbrauchswassertank bleibt erhalten. Bei 20 L Bestand beendet ein Wasserauftrag das Schürfen bereits nach 225 kg Rohstoff, also 180 L Wasserpotenzial. 90 % sind ein maximales Frachtziel, kein Auftrag, über den Bedarf hinaus abzubauen. Das [große Kaltlager samt Aufbereitung](Eislager-Slot01-Auslegung-2026-10-25.md) ist weiterhin Planung; die Prototypentladung schreibt direkt den Wasseranteil gut und erhält den unbekannten Rest noch nicht als eigenen Lagerbestand. 2 kW nominelle Dockladeleistung bedeuten ideal acht Stunden für einen ganz leeren 16-kWh-Akku; Stationszuteilung kann verlängern. Das frühere Verhältnis 15 min Werkzeugbetrieb : 30 min Laden gilt nur für 4-kW-Werkzeugenergie zu 2-kW-Laden, nicht für die gesamte Flugbilanz.

**Architektur und alte Spielstände:** `DroneCargoProfile` kapselt Dichten, Packung, Volumen und Füllziel; `DroneAgent` bleibt für Flug, Arbeit und Versorgung zuständig. Das zwölfteilige gespeicherte Hardwarefeld wird erhalten, das versionierte Frachtprofil kommt optional hinzu. Alte Spielstände behalten Hardware, Fracht und Versorgung. Über **Drohnenverwaltung → Neue 90-%-Auslegung am leeren Dock übernehmen** können sie ausdrücklich umgestellt werden; dabei werden Akku und Treibwasser nicht gratis aufgefüllt. Ein echter neuer Kampagnenstart erhält die neuen Standards für alle zehn Drohnen; nur 01 ist anfänglich aktiv. Die eigenständige 12-kg-Modellvorschau behält ihre alte massenbezogene Füllung.

Neue Profile zeigen acht wiederverwendete Fülllagen mit tatsächlicher volumenabhängiger Höhe. UI zeigt effektive materialabhängige kg-Grenze, belegtes Volumen und Füllziel; der Developer-Slider bezeichnet ausdrücklich die mechanische Nutzlast. Die sichtbaren Brocken sind Stellvertreter, keine separat bilanzierten Ressourcen oder physikalische Kornsimulation.

**Tatsächliche Abnahme:** `drone-sizing-build-02`, Development mit Unity 6000.6.4f1 in separater Buildkopie; Quellprojekt 6000.4.7f1 und Layout13 unverändert. Acht Prüfläufe derselben Binärdatei: **98 Auslegung + 2127 Werkstatt + 97 Aufbau + 177 Station + 794 Raumplanung + 28 UI + 685 Bergung + 29 Vitals = 4035 Assertions**, alle Exit0. Die Anzahl enthält viele einzelne Kollisions-/Abstands-/Interlock-Prüfungen. 52 neue Bilder gesichert, Fracht und Drohnenverwaltung visuell geprüft; 260 Quelldateien und 247 ausgelieferte Dateien per SHA256 abgeglichen. Drei gespeicherte Nutzer-Raumpläne unverändert.

Im echten A01-Flug mit ausdrücklich **2000-L-Empfangstank-Fixture**, 20 L Anfangswasser und 20 kWh Stationspuffer: 1033,326 kg abgebaut, 90 % Boxbelegung, 826,661 L Wasser übertragen, echte Rückkehr zum geplanten Dock. Gesamte Mission rund **78 min 39 s Spielzeit**, einschließlich animierter Dockvorgänge. Gemessener Gesamtverbrauch **11,594 kWh und 7,692 L Treibwasser**; Rest am Dock **4,406 kWh und 7,308 L**, vor Nachladen. Werkzeug plus Bordlast während des abgetasteten Schürfens rund 2,324 kWh. Kleine Integrations-/Abtastabweichung: 2065 s gemessene Schürfschritte gegenüber 2066,652 s Masse-/Ratenrechnung. Echte Disk-Saves beim Abbau und Rückflug, Legacy-Erhalt, atomare Ablehnung ungültiger Profile, Dock-Upgrade ohne Gratisvorräte und echter Neustart nach Legacy-Load geprüft. Der normale 200-L-Tank begrenzt das Auftragsziel im selben Prüflauf nachweislich auf 225 kg.

[Vollständiger Nachweis mit Logs, Bildern und Manifesten](Pruefungen/Drohnen-90-Prozent-Umsetzung-2026-10-25.json). Geprüfter Build unter `Builds/Windows`, Vorgänger `Builds/Windows-before-drone-sizing-20261025` erhalten; Start über `Start-Aufbauphase-01.cmd`. Neue Prüfung: EXE ohne `-batchmode` mit **`-stationDroneSizingCheck`**, Ergebnis/Saves/Einstellungen/Raumplan-Fixtures isoliert unter `Logs/DroneSizing`. Keine neue Release-/VR-/manuelle Komfort-/100k-/Langzeit-/FPS-Abnahme und keine erneute separate 12-kg-Showcaseprüfung; kein Commit/Push. Vorherige Prüfaussagen unten gelten ausschließlich für den früheren Analysestand.

## Historische Ausgangsanalyse

**Analytische Prüfung, noch keine neue Hardwareentscheidung oder Parameteränderung.** Anlass ist die Kombination aus 50 kg Fracht, 10 L Treibwasser und 8 kWh Batterie. Die Prüfung verbindet aktuellen Quellcode, vorhandene Geometrienachweise und öffentlich dokumentierte Technik. Zahlen beziehen sich auf Standardhardware; gespeicherte Hardware und Developer-Einstellungen können abweichen.

## Behälter und Größenverhältnis

Die Kampagnenflotte verwendet `TripoMiningDroneVisual` mit gleichmäßig auf 2 m Breite normiertem Rumpf. Die historische tatsächliche Rumpfmessung desselben Prefabs vom 11.10.2026 beträgt 2,000 × 1,690 × 2,201 m ohne spätere Dachbox und Werkzeuge. Diese Messung wurde heute nicht im laufenden Spiel wiederholt.

Die Dachbox ist laut aktuellem `MaintenanceLaserVisual.cs` nominal **1,32 × 1,64 × 0,90 m** groß: äußere rechteckige Hüllbox **1,948 m³ bzw. rund 1950 L**, ohne überstehende Schienen/Griffe. `PackedCargoInside` prüft die Brocken gegen x ±0,60 m, y 0,065–0,875 m und z ±0,75 m: **1,20 × 1,50 × 0,81 m = 1,458 m³**. Das ist ein konservativer rechteckiger Geometrieprüfbereich, kein exakt integrierter nutzbarer Innenraum.

50 kg entsprechen darin nur 34,3 kg/m³. 50 kg flüssiges Wasser sind näherungsweise 50 L und würden rund 3,4 % dieses Bereichs belegen. Eine nahezu volle Box mit 50 kg dichten Eis-/Gesteinsbrocken ist deshalb optisch nicht stimmig. Eine niedrige mechanische Anfangs-Traglast wäre denkbar; dann müsste die Box bei 50 kg sichtbar wenig gefüllt sein.

Aktuell existiert nur ein kg-Limit. Zwölf sichtbare Brocken bilden den prozentualen Füllstand ab; Materialdichte, Hohlräume und belegtes Volumen werden nicht berechnet. Der ältere separate Mining-Showcase verwendet 12 kg, die Kampagne 50 kg; der Developer-Slider endet bei 150 kg.

**Vorschlag:** `CargoUsableVolumeM3` und `MaxPayloadKg` getrennt führen und beim zuerst erreichten Limit stoppen. 1,3–1,45 m³ sind ein zunächst zu vermessender Planraum; **250–500 kg** eine weitere Auslegungsvariante, keine nachgewiesene Tragfähigkeit. Füllung folgt Material und Schüttdichte. Struktur, Greifer, Flugmasse und Rückkehrreserve müssen die gewählte Nutzlast unterstützen.

## Aktuelle Hardware und NASA-Bezug

Nachrechnung mit den späteren Eislager-Annahmen vom 25.10.2026: Vom 1,458-m³-Brockenprüfbereich bleiben bei 95 % betrieblicher Füllfreigabe 1,3851 m³. Bei 75 % Feststoffpackung passen darin rechnerisch rund **953 kg reines Eis** oder **1091 kg 80/20-Rohgemisch**, letzteres mit angenommener Reststoffdichte 2500 kg/m³. Das Rohgemisch enthält rund 873 kg potenziell gewinnbares Wasser. Bei 65–85 % Packung liegt die Rohmasse etwa zwischen 945 und 1236 kg. Dies ist eine räumliche Abschätzung, keine Nutzlastfreigabe; der Prüfbereich ist kein exakt vermessener Nutzraum. Aktuell erlaubt das Spiel weiterhin 50 kg. Die vorher vorgeschlagenen 250–500 kg bezeichnen eine zu prüfende mechanische/flugtechnische Nutzlastvariante, nicht das geometrische Füllmaximum.

`DroneAgent.cs`: 200 kg trocken, 10 L Wasser als etwa 10 kg gerechnet, 8 kWh Akku, 50 kg Fracht, 5 m/s Reisegeschwindigkeit, 5 N Schub, 1000 m/s effektive Ausströmgeschwindigkeit, 0,2 kWh elektrische Energie pro Liter Treibwasser, 80 kW Abbauleistung, 0,1 kg/s Förderrate, 50 W Bordlast und 10 % geschützte Akku-/Wasserreserve.

8 kWh ist gespeicherte **Energie**, kW ist **Leistung**. kW/s wäre eine Leistungsänderung und keine Akkukapazität. 8 kWh entsprechen 28,8 MJ.

Die [NASA-Übersicht, Tabelle 4-7](https://www.nasa.gov/smallsat-institute/sst-soa/in-space_propulsion/) führt Wasser-Resistojets auf: ARM-A mit 100 s spezifischem Impuls sowie Comet-1000 mit 175 s, 17 mN und maximal 50 W. NASA kennzeichnet solche Gerätedaten als aus den angegebenen Quellen übernommen. Sie stützen die Größenordnung eines Wasser-Dampf-Antriebs, validieren aber keine skalierte 5-N-Bergbaudrohne. Unser ve = 1000 m/s entspricht etwa 102 s spezifischem Impuls.

Die [NASA-Raketengleichung](https://science.nasa.gov/learn/basics-of-space-flight/chapter3-2/) lautet ohne äußere Kräfte `deltaV = ve * ln(m_start / m_ende)`. Treibstoff begrenzt Geschwindigkeitsänderungen, nicht direkt Kilometer. Gleiten zwischen Schubphasen benötigt kein zusätzliches Treibwasser. Zeit, Bordenergie, Navigation, Gravitation und relative Bewegung begrenzen die praktische Mission trotzdem.

## Hinflug und Rückkehr

10 L liefern mit dem heutigen ve ideal insgesamt **48,79 m/s deltaV** bei leerer Drohne (210 → 200 kg) oder **39,22 m/s** bei schon vorhandenen 50 kg Fracht (260 → 250 kg). Diese Werte gelten jeweils bei gleichem Beladungszustand über den gesamten Verbrauch.

Eine Sammelmission lädt dagegen erst am Ziel zu. Vier Manöver sind erforderlich: beschleunigen, am Ziel bremsen, zurück beschleunigen, an der Station bremsen. Bei 5 m/s sind das 20 m/s Geschwindigkeitsänderung. Mit 50 kg erst am Ziel aufgenommen kostet dies ideal **4,656 L**, vor Docking, Ausweichen, Rotation und Reserve.

5 N bei etwa 210 kg ergeben 0,0238 m/s²: auf 5 m/s ungefähr 210 s und 525 m; Bremsen benötigt ähnlich viel Strecke. Bei etwa 260 kg werden daraus 260 s und 650 m. Kurze Strecken erreichen 5 m/s nicht. 5 N bei ve = 1000 m/s benötigen 5 g Wasser/s: **33 min 20 s tatsächliche Schubzeit** aus 10 L, Gleitphasen zusätzlich.

Beispiel für je 10 km hin und zurück bei 5 m/s und 5 N:

| Frachtaufnahme am Ziel | Wasser, ideal | Flugzeit, grob | Restwasser |
| --- | ---: | ---: | ---: |
| 50 kg | 4,66 L | 74,5 min | 5,34 L |
| 250 kg | 6,65 L | 77,8 min | 3,35 L |
| 500 kg | 9,13 L | 82,0 min | 0,87 L |

500 kg unterschreiten schon ohne Docking die vorhandene 1-L-Reserve. Bei **2 m/s** benötigt dieselbe 500-kg-Mission dagegen ideal **3,67 L** und etwa **173 min**. Zeit kann Treibwasser sparen.

Annahmen: lokale geradlinige Fahrt zwischen zunächst relativ ruhenden Objekten; keine vollständige Orbitalplanung, Asteroidengravitation oder Landung. Zeiten verwenden konservativ konstante Startmasse je Richtung, Wasser vier aufeinanderfolgende ideale Geschwindigkeitsänderungen mit sinkender Masse und Zuladung in der Mitte. Wegpunktstopps und Sicherheitsaufschläge kommen zusätzlich hinzu. Die Beispiele sind eigene Rechnungen, keine NASA-Missionsmessungen.

## Akku und fehlende Heizbilanz

| Last | Ideale Laufzeit mit 8 kWh |
| --- | ---: |
| 50 W Bordversorgung | 160 h |
| 1 kW Gesamtlast | 8 h |
| 5 kW Gesamtlast | 1 h 36 min |
| 80 kW Bohrer + 50 W Bordlast | knapp 6 min |
| Dasselbe mit 10 % Akku geschützt | 5 min 24 s, Rückkehr zusätzlich abziehen |

50 kg Abbau dauern bei 0,1 kg/s bereits 500 s und kosten **11,12 kWh** Werkzeug-/Bordenergie. Mit 8 kWh sind 50 kg ohne Fortschrittsbonus selbst ohne Flug nicht erreichbar. 80 kW sind prinzipiell mögliche hohe Werkzeugleistung, verlangen aber ein passendes 10C-Akkusystem und Wärmeabfuhr. Aus bestandenen Spielprüfungen folgt diese technische Machbarkeit nicht.

Bei idealen 2 kW effektiver Ladeleistung dauert leer → voll vier Stunden; Stationszuteilung und Verluste können verlängern. Das bereits bestätigte frühere Ziel von etwa **15 min Werkzeugbetrieb zu 30 min Nachladen** (Bergbaudrohnen.md) wird mit 80/2 kW nicht erreicht: deren Leistungsrelation ergibt 1:40. Neue absolute Leistungen und Förderraten sind weiterhin offen.

Die heutigen **0,2 kWh/L** Antriebsenergie sind für eine Dampfmaschine ohne zusätzliche Wärmequelle zu günstig. Die [Bosch-Dampftabelle nach IAPWS](https://www.boiler-planning.com/en/hotwater/tools/basic-principles-of-water-vapour/water-vapour-table.html) ergibt bei rund 100 °C und 1 bar bereits 2257,5 kJ/kg bzw. **0,627 kWh/kg reine Verdampfung**; von 20 °C flüssig bis zu diesem Dampfzustand ungefähr **0,72 kWh/kg**. Im Vakuum ändern sich die Betriebsbedingungen, die Verdampfungsenergie entfällt dadurch nicht.

Die Düse wandelt einen Teil der Dampfenthalpie in gerichtete Bewegung um; diese Strahlenergie darf nicht pauschal vollständig noch einmal addiert werden. Temperatur, Druck, Überhitzung, Verluste, thermische Speicherung und externe Wärmequellen gemeinsam auslegen. Für 4,66 L wären in diesem Referenzfall schon ungefähr **3,35 kWh Wärme** nötig, während das Spiel nur 0,93 kWh berechnet. Dieser 100-°C-Referenzfall garantiert nicht ve = 1000 m/s, sondern zeigt die fehlende Heizgrößenordnung. Eine neue pauschale kWh/L-Zahl wird hier nicht beschlossen.

Wasser-Elektrolyse und anschließende Verbrennung von H₂/O₂ ist eine andere Auslegung. Der [NASA-NIAC-Bericht zur Treibstoffproduktion](https://www.nasa.gov/wp-content/uploads/2015/05/niac_2015_phasei_lewis_storablepropellants_tagged.pdf), Abschnitt 5, verwendet 286 kJ/mol bzw. 15,9 MJ/kg Wasser als Enthalpiekosten: rund 4,4 kWh/kg Gesamtenergiegrößenordnung, also rund 44 kWh für 10 kg. Elektrische/thermische Anteile hängen vom Prozess ab. Stationär erzeugte und getrennt gespeicherte Gase wären bereits energetisch aufgeladener Treibstoff; ein einzelner 8-kWh-Akku könnte nicht die gesamten 10 kg ohne weitere Energiezufuhr aufbereiten. Mögliche spätere Technik, keine neue Startentscheidung.

## Konsequenz und tatsächliche Prüfung

Modellgröße spricht für mehr als 50 kg. 250 kg sind bei 5 m/s/10 L als Rechenvariante weniger knapp als 500 kg; 500 kg brauchen langsameres Tempo, mehr Wasser oder effizienteren Antrieb sowie passende Energieversorgung. Strukturtragfähigkeit bleibt offen. Erst Volumen-/Massenlimits und Darstellung, dann Antriebs-/Wärme-/Werkzeugbudget gemeinsam festlegen.

Der eigentliche Miningflug verwendet bereits F/m und Raketengleichung. Stationsarbeit/Bergung/Refit verbrauchen dagegen pauschal 0,35 kWh und 0,25 L am Start; kurze Dockmanöver sind animiert. Diese Wege benötigen dieselbe Budgetbasis, bevor Missionen physikalisch vergleichbar sind. Bereite Drohnen verbrauchen derzeit keine laufende Bordenergie; die 160-h-Zahl setzt ausdrücklich kontinuierliche 50 W voraus.

Quellprüfung: DroneAgent, MaintenanceLaserVisual, TripoMiningDroneVisual, CampaignDroneFleet, DeveloperGameConfiguration, WorkshopRefit und vorhandene Fachdocs/Rumpfmessung. Reproduzierbare Rechnungen: `tools/AssessDroneSizing.py`; [Analysewerte](Pruefungen/Drohnen-Auslegung-Analyse-2026-10-25.json). NASA-/Bosch-Quellen am 25.10.2026 gelesen. Skript liest Standardwerte und schreibt ausschließlich Analyse-JSON. Keine Modelle, Raumpläne oder Spielparameter geändert; kein neuer Unity-Build oder Laufzeittest. `debris-hands-build-02` bleibt der vorhandene Spielstand.
