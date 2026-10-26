# Stationsenergie: Auslegung für eine Person und je eine Drohne

**Aktualisierung nach Folgeauftrag —26.10.2026:** Beide Sonnen und die Energieskala sind inzwischen festgelegt und im Spiel/Strominterface integriert; [aktuelle Stern-/PV-Auslegung und Nachweis](Doppelsonnen-Energie-2026-10-26.md). Der Rechner und JSON-/CSV-Ergebnisdateien enthalten bereits die neuen0,360375/0,118446 Sonnenleuchtkräfte. Abschnitte unten über unbekannte Sterne, `null`, das1,0/0,5-Beispiel und damalige reine Hauptsonnen-Runtime sind **historischer Ausgangspunkt vor diesem Folgeauftrag**, keine aktuellen Parameter. Die Stationslasten,40-kWe-Reaktor,3/12-kW-Drohnenlader und4-kWh-I75 bleiben Vorschläge. Der ursprüngliche Bericht folgt zur Nachvollziehbarkeit.

26.10.2026. **Analytische Auslegung, keine Änderung der Spielparameter.** Nutzer legt als Anfang eine Person, eine Außen- und eine Innendrohne fest; Besatzung und Flotte wachsen später dynamisch. Jede neue Technologie benötigt ein begründetes Stromprofil. Beide Sonnen und deren tatsächliche Positionen sind zu berücksichtigen. Die unten vorgeschlagenen Hardwarewerte sind keine bereits bestätigte Reaktor-, Akku-, Magnet- oder Sternauslegung.

**Ergebnis der Referenzlastrechnung:** etwa **4,2 kW Bereitschaft**, **7,8 kW bei belegter Station mit laufender Eis-/Wasseranlage**, **25,1 kW mit beiden Schnellladern**, **31,2 kW bei zusätzlich laufender Sauerstoffcharge**. Gleichzeitige Fertigungs-/Ladespitzen können **43,4 kW** erreichen. Diese Szenarien enthalten verschiedene Gleichzeitigkeiten; sie sind keine gemessene Tageslastkurve.

Die absoluten Solar-kW sind im vorhandenen Entwurf **noch nicht physikalisch bestimmbar**: Entfernungen, Richtungen und Flächen sind vorhanden, Sternleuchtkräfte und eine physikalische Interpretation der ausdrücklich komprimierten Himmelskörper fehlen. Die gesonderten Solar-kW-Beispiele unten sind deshalb ausdrücklich bedingt, keine tatsächliche oder neu beschlossene Spielleistung.

## Quellen, Rechner und Geltungsbereich

- [Reproduzierbarer Rechner](../tools/AssessStationEnergy.py), nur Python-Standardbibliothek; Aufruf `python tools/AssessStationEnergy.py`.
- [Parameter und Technologieprofile](../Design/Energy/station-energy-reference-2026-10-26.json), einschließlich Herkunft, Annahmen und Unsicherheit je Verbraucher.
- [Berechnung, Quellhashes und Prüfungen](Pruefungen/Stationsenergie-Rechnung-2026-10-26.json), [Verbraucherkatalog als CSV](Pruefungen/Stationsenergie-Technologien-2026-10-26.csv).

Die Rechnung liest den gemeinsamen Quellstand. Die parallele adaptive Beleuchtung wurde beim Lesen bereits berücksichtigt: deren 90-W-Gesamtbudget bleibt ausdrücklich ein Prototypwert. Kein Unity-Build oder neuer Playerlauf für diese Rechnung; keine Spielstände, Raumpläne, Assets oder C#-Spielquellen durch diesen Auftrag geändert. Historische Flugmessungen werden als solche verwendet.

## 1. Bestand und tatsächliche Sonnenentfernungen

`RuinedWorld.cs` deklariert `AstronomicalUnit=1800`, Mondabstand18 und Planetenabstand zur Station90, jeweils **komprimierte Darstellungs-km**. `InitialHome` und beide Sternvektoren ergeben, von der Stationsmitte am lokalen Ursprung aus:

| Größe | Hauptsonne | Begleiter |
|---|---:|---:|
| Positionsvektor in Darstellungs-km | (−1343,6899; 1,7459; 1309,0455) | (−1103,6899; 301,7459; 1429,0455) |
| Tatsächlicher Abstand im Spielentwurf, Vektorlänge | **1875,92794 km** | **1830,67008 km** |
| Verhältnis zur im Code so benannten Spiel-AE1800 | 1,042182 | 1,017039 |
| Dargestellter Sternradius | 55 km | 39,6 km |

Der Winkel zwischen beiden Sonnenrichtungen beträgt **12,3894°**. Das sind keine zwei gegenüberliegenden Sonnen. `SkyCamera` stellt diese Kulisse getrennt von den lokalen Metermaßen der Station dar. Die Wörter „Spiel-AE“ und „Darstellungs-km“ begründen keine Gleichsetzung mit echten astronomischen Einheiten.

Leuchtkraft, Spektrum und reale Radien/Temperaturen sind nicht festgelegt. Unity-Lichtintensität2,4/1,1 und warme/kühle Farbwerte sind keine physikalischen W/m² oder Sternluminositäten. Aus Abständen allein folgt deshalb kein eindeutiger PV-Ertrag. Reale sonnenähnliche Sterne in nur rund1800km Entfernung wären ebenfalls keine plausible Umgebung für diese Station. In der Ergebnisdatei bleibt `actual_absolute_solar_output_kw` ausdrücklich `null`.

Im bestehenden Spiel ist nur die Hauptsonne in `CaptureGeneration()` berücksichtigt; Flächen-/Entfernungs-/Begleiterphysik fehlen. Nominal stehen in der Szene2kW Reaktor, insgesamt0,5kW Solar und2kW Drohnenladung. Reaktorzustand0,82 und Solarfeldzustände A0,65/B0,81 sind Schaden, keine unabhängig geregelte Reaktorauslastung.

## 2. Flächen und Solarformel für beide Sterne

`StationVisual.cs` baut pro Flügel einen11,4×10,7m-Rahmen, nach innenraumseitiger Skalierung2× in X/Z also **22,8×21,4m =487,92m² Rahmenfläche**. Ein gezeichnetes PV-Segment misst anschließend2,86×3,86m =11,0396m². Die vorhandenen Lücken sind bereits abgezogen:

| Feld | Segmente vorhanden/voll | Vorhandene PV-Fläche | PV-Fläche nach vollständiger Wiederherstellung |
|---|---:|---:|---:|
| A / rechts | 32/35 | **353,2672 m²** | 386,386 m² |
| B / links | 34/35 | **375,3464 m²** | 386,386 m² |
| Zusammen | 66/70 | **728,6136 m²** | 772,772 m² |

Rahmenfläche darf nicht vollständig als Solarzellfläche gerechnet werden. Für jeden Flügel und jede Sonne gilt:

`P = A_PV × η_Zelle × η_Panel/MPPT × Σ [1361W/m² × (L_i/L_Sonne) / (r_i/AE)² × max(0, n·s_i) × Sichtbarkeit_i]`.

1361W/m² bei1AE ist die NASA-Referenz. Moderne Raumfahrtzellen liegen beispielsweise um30%; der [AZUR-Herstellerkatalog2025](https://www.azurspace.com/media/uploads/file_links/file/bdb_00010890-01-00_generalinformationazur.pdf) dokumentiert passende Größenordnungen. **30% Zellwirkungsgrad und85% verbleibender Panel-/Temperatur-/MPPT-Faktor sind hier Planungsannahmen**, keine gemessenen Materialeigenschaften des Spielmodells. Die anschließende Stationsverteilung mit95% wird getrennt gerechnet. Quellen: [NASA-Strahlung](https://earth.gsfc.nasa.gov/climate/projects/solar-irradiance/science), [NASA-PV-Übersicht](https://www.nasa.gov/smallsat-institute/sst-soa/power-subsystems/).

Bei **einer** Sonne liefern0/30/60/75/85/90° von der **Flächennormalen**100/86,6/50/25,9/8,7/0% des ausgerichteten Ertrags. Eine direkt angestrahlte Fläche entspricht0°, eine Kante90°. Die Rückseite liefert hier0. Doppelter Abstand zu derselben Sonne ergibt ein Viertel ihres Beitrags. Beide Beiträge werden jeweils mit eigener Richtung, Entfernung und Abschattung addiert; eine verdeckte Sonne schaltet die andere nicht aus.

**Ausschließlich als Sensitivitätsbeispiel, nicht als Interpretation der tatsächlichen km:** Man *könnte* die1800Darstellungs-km auf eine echte AE normieren und Hauptsonne1,0/Begleiter0,5 Sonnenleuchtkräfte annehmen. Dann wären die rechnerischen Entfernungen1,042/1,017AE. Diese Normierung und Leuchtkräfte sind **nicht bestätigt**. Unter genau diesen Zusatzannahmen ergeben sich:

| Beispiel | Feld A | Feld B | Zusammen |
|---|---:|---:|---:|
| Vorhandene Flächen, aktuelle Neigung | 3,87 kW | 17,80 kW | **21,68 kW** |
| Beide Flügel unabhängig optimal ausgerichtet | 171,24 kW | 181,94 kW | **353,18 kW** |
| Ganze Station optimal drehen, Flügelneigung beibehalten | — | — | **348,45 kW** |

Die Ausgangsnormalen sind A=(−0,08716;0,97789;−0,19008), B=(0,05234;0,99316;0,10439). Zur Hauptsonne stehen die Flächen bei93,97°/87,92°, zum Begleiter bei86,25°/77,66°: daher der starke Ausrichtungseffekt. Im Beispiel stammen17,33kW des Ausgangsertrags allein vom Begleiter. Halbiert man dessen angenommene Leuchtkraft auf0,25, sinkt der Ausgangsertrag auf13,01kW; bei1,0 steigt er auf39,00kW. Dies zeigt ausdrücklich, warum eine eindeutige absolute Aussage ohne diese Entwurfsgröße nicht möglich ist.

Fehlende Segmente wurden schon geometrisch abgezogen. Die alten Schadensfaktoren0,65/0,81 werden nicht nochmals ungeprüft darübermultipliziert; sie könnten dieselben Ausfälle enthalten. Zusätzliche Alterung/elektrische Schäden benötigen eigene Zustände. Teilabschattung ist bei Reihenschaltungen nicht generell proportional zur sichtbaren Fläche; Bypassdioden/Strings/MPPT-Auslegung bleiben offen. Direkte Schattenprüfung wurde hier nicht ausgeführt.

## 3. Elektrischer Stationsbedarf

Alle Angaben sind kW elektrisch, nicht kWh Akkukapazität. Bereitschaft enthält mittlere Sauerstoffproduktion und geparkte Fahrzeuge; Lastspitzen und Ladephasen sind gesondert. Die Werte gelten für einen begrenzten aktiv beleuchteten Teil der großen Station, nicht für überall volle Arbeitsbeleuchtung.

| Verbraucher | Bereitschaft bzw. Tagesmittel | Arbeitszustand / Spitze | Herleitung oder Grenze |
|---|---:|---:|---|
| Rechner, Mira, Netzwerk und Pulte | 0,60 | 1,20 /2,0 | Budget für noch unbestimmte Rechnerhardware |
| Adaptive LED-Beleuchtung | 0,16 | 1,00 /2,0 |100m² Arbeitszone300lx,100m² Gang100lx,1400m² Hintergrund10lx;140lm/W, Nutzungsfaktor0,5, Wartung0,8;30W Markierungen |
| Umluft, CO₂-/Spurenstoffentfernung | 0,65 | bis1,50 | Infrastrukturansatz; Gasfluss und konkrete Anlage noch nicht dimensioniert |
| Brauchwasserrecycling/Desinfektion | 0,15 | bis0,60 | Tagesmittel; getrennt von neu angeliefertem Eis |
| Sauerstoffgewinnung | **0,242 im Tagesmittel** | **6,0 während einer Charge** |0,84kg O₂/Person/Tag; rund6,9kWh/kg O₂ |
| Kaltlager und Steuerung | 1,48 | bis2,50 | konservatives150K-Beispiel unten |
| Wärmeregelung/Pumpen | 0,35 | 0,50 /1,0 | nur elektrische Pumpen/Regelung; keine freie unbegrenzte Heizung |
| Ventile, Türen, Sensorik, Docksteuerung | 0,10 | 0,15 /1,0 | Mittelwert und kurzzeitige Motorlast getrennt |
| Scanner | 0,03 | 1,20 beim Laden |0,1kWh Speicher;97→100% in10s erfordern1,08kW gespeicherte Ladeleistung, bei90% Wirkungsgrad1,20kW Eingang |
| Brecher + Verdichter + Eisaufbereiter |0 bzw. separat | **0,2+0,2+1,5=1,9** | bestehende erste Prozessauslegung;0,20kWh/L,7,5L/h |
| Metallteilefertigung |0,03 | **2,4 typisch /8,5 Spitze** | EOS-M290-Referenz; konkrete Spielrate nicht damit validiert |
| Mechanisches Sortieren |0,02 |0,8 /1,5 | kein Schmelzen oder chemisches Raffinieren enthalten |
| Nahrungsverarbeitung |0,03 |1,2 /2,0 | vorbereitete Ausgangsstoffe; vollständige Nahrungssynthese aus Asteroidenmaterial bleibt unvalidiert |

LED-Effizienz ist am [Signify-Herstellerprogramm](https://www.signify.com/global/prof/indoor-luminaires/surface-mounted/philips-coreline-surface-mounted-gen-4/LP_CF_SM136V_EU/family) orientiert. Sauerstoffbedarf: [NASA-Auslegung](https://ntrs.nasa.gov/api/citations/20190027610/downloads/20190027610.pdf);55,2kWh/kg H₂ als Elektrolyse-Systemreferenz: [DOE2024](https://www.hydrogen.energy.gov/docs/hydrogenprogramlibraries/pdfs/24005-clean-hydrogen-production-cost-pem-electrolyzer.pdf). Acht Kilogramm Sauerstoff fallen pro Kilogramm Wasserstoff an; daraus folgt das Tagesmittel. Eine kleine Anlage kann zusätzliche Skalierungsverluste haben. CO₂-Abscheidung, Luftversorgung und Wasserrecycling sind damit nicht kostenlos erledigt. [NASA-ECLSS-Funktionsübersicht](https://www.nasa.gov/reference/environmental-control-and-life-support-systems-eclss/).

Kaltlagerbeispiel:200m² thermisch relevante Fläche, angenommener effektiver U-Wert0,01W/(m²K), Kabine293K und Lager150K ergeben286W Wärmeeintrag. Bei angenommener Kälteleistungszahl0,20 sind1,43kW elektrisch plus50W Steuerung nötig. Isolierung, Wärmebrücken und Lagertemperatur sind nicht vermessen. Ein dichtes Lager bei250K wäre erheblich sparsamer. **1,48kW ist deshalb eine transparente konservative Variante, keine Eigenschaft des eingebauten Modells.**

Die [aktuelle EOS-Herstellerangabe](https://www.eos.info/metal-solutions/metal-printers/data-sheets/sds-eos-m-290) nennt2,4kW typisch/8,5kW maximal. Das beweist insbesondere nicht, dass das bisherige Rezept2kg Metall in4min bei1,4kW fertigen kann. Energiekosten, Dauer, Materialprozess und Druckrate müssen später gemeinsam korrigiert werden. Dasselbe gilt für vollständige Nahrungssynthese und materialabhängiges Raffinieren.

Aus den Profilen, Bordlasten und5% Stationsverteilungsverlust ergeben sich4,19/7,76/25,12/31,18/43,38kW für die eingangs genannten Szenarien. Die letzte Zahl kombiniert den Sauerstoffbatch, beide Lader,8,5kW Druckerspitze,0,8kW Sortierung,1,2kW Nahrung und1,2kW Scanner. Nicht jede denkbare Einschaltspitze ist damit modelliert. Sauerstoff-Mittelwert und Vollcharge ersetzen einander; sie werden nicht addiert.

## 4. Drohnenakkus und Schnellladen

Industrielle Vergleichssysteme besitzen3–3,3kW bzw.12kW Ausgangsleistung. Wiferion nennt typischerweise91% Quelle-zu-Batterie-Wirkungsgrad und bis93%; dies ist eine Leistungsreferenz, keine Weltraumqualifikation oder Festlegung auf induktives Laden. [Hersteller-FAQ](https://www.wiferion.com/en/faq/faq-etalink/), [12-kW-System](https://www.wiferion.com/en/news/logimat-best-product-2020-etalink-12000/).

| Größe | Außendrohne | Innendrohne I75 / R-01 |
|---|---:|---:|
| Akku | **16kWh, bestehende Hardware** | **4kWh als Vorschlag**, bisher keine eigene Akkusimulation |
| Ladegerät, DC-Ausgang | **12kW** | **3kW** |
| Stationsbusbedarf bei Volllast | **13,19kW** | **3,30kW** |
| Park-/Bordlast während Laden |50W |80W |
| Tatsächlich gespeicherte Leistung in CC-Phase |11,711kW |2,862kW |
|10→80% | **57,4min** | **58,7min** |
|10→100% mit erklärter Ladekurve | **90,6min** | **93,9min** |
| Stationsbusenergie10→80% |12,61kWh |3,23kWh |

Rechnung:91% Ladesystemwirkungsgrad, Bordlast vom DC-Ausgang abziehen,98% elektrische-zu-chemische Speicherwirkung. Bis80% konstante Leistung; danach eine ausdrücklich vereinfachte abfallende Leistung bis20% am angezeigten Vollpunkt. Keine exakte Herstellerkurve für diese fiktiven Akkupacks. BMS, Zellen, Temperatur und Kühlung können begrenzen; [EnerSys-BMS-Handbuch](https://www.enersys.com/48ef4a/globalassets/documents/product-documentation/_enersys/glob/legacy/batteries/nexsys/nexsys-ion/glob-en-om-nex-ion-0323.pdf). Gerade die letzten Prozent brauchen länger. Wenn die Station nur3kW zuweisen kann, macht ein12-kW-Ladeschild daraus keine12kW.

Die Innendrohne muss beim Laden mechanisch aufliegen;80W Parklast bedeuten ausgeschaltete Hubmagnete. Die vom Hersteller angegebenen induktiven Ladespalte liegen im Zentimeterbereich und sind nicht gleich der8cm-Fahr-Schwebehöhe. Empfänger/Andockposition und eine mögliche Seitenkopplung bleiben auszulegen.

**Schweben ist derzeit die größte offene Fahrzeugannahme.** Für ein herkömmliches Kupferelektromagnet-Beispiel mit100kg Gesamtmasse,1g und8cm effektivem Spalt liefert `F=B²A/(2μ₀)` rund0,248T. Vier angenommene0,01m²-Polflächen und die dokumentierten Wicklungsannahmen ergeben bereits rund2,69kW Kupferverluste; die3-kW-Fahrannahme ist damit nur eine erste Größenordnung, kein Nachweis einer passenden Konstruktion. Randfelder, Rückspalte, Erwärmung und reale Mechanik fehlen. Unterstützende Permanentmagnete oder ein viel kleinerer Spalt können den Bedarf stark senken. Die aktuelle frei schwebende R-01-Bahn ist keine technisch belegte Magnetführung.

Mit4kWh,70% Betriebsfenster und3kW wären **56min Fahrt ohne zusätzliches Werkzeug** möglich.1,5kW Werkzeugzusatz verkürzt dies auf rund37min. Masse, Akku und Fahrleistung sind noch zu bestätigen. Keine Leistungsdaten aus der viel größeren Außendrohne ungeprüft auf I75 übertragen.

Flug/Werkzeugkosten aus der Fahrzeugbatterie werden nicht gleichzeitig als Stationsnetzlast gebucht. Das Netz bezahlt später das Nachladen inklusive Verlusten. Historischer voller Außenauftrag:11,594kWh in78,65min; kein erneuter Flugversuch in dieser Sitzung. Bei pausenlos wiederholten Arbeits-/Ladezyklen ergibt die Beispielrechnung rund14,9kW mittlere Gesamtversorgung für1Person/1Außen-/1Innendrohne,29,9kW für3/3/3 und80,9kW für6/10/10. Dies setzt genügend Docks voraus und ist kein typischer Quest-Tagesbedarf. Die zusätzliche0,4kW/Person ist ein grober skalierbarer Ansatz, noch keine Auslegung weiterer Habitate.

## 5. Reaktor, Wärme und Stationsakku

**Vorschlag für die Referenzrechnung:40kW elektrische Netto-Maximalleistung**, mit eigenem Sollwert und einer technisch noch festzulegenden Anfahr-/Rampenbegrenzung. NASA entwickelt eine40-kWe-Klasse; das belegt die Größenordnung, keinen bereits lieferbaren oder für eine bewohnte Stationsmitte qualifizierten Reaktor. [NASA-Fission-Surface-Power](https://www.nasa.gov/exploration-systems-development-mission-directorate/fission-surface-power/).

`P_Reaktor =40kW × verfügbare_Hardwarefraktion × Auslastung`, sofern angeschlossen und gekühlt. Schaden und Auslastung bleiben eigene Zustände:

| Auslastung | intakt | mit bisherigem Zustandsfaktor0,82 |
|---|---:|---:|
|20% |8kW |6,56kW |
|25% |10kW |8,20kW |
|50% |20kW |16,40kW |
|75% |30kW |24,60kW |
|100% |40kW |32,80kW |

20% als mögliche untere Betriebsgrenze ist eine Annahme; reale Mindestlast, Hot-Standby, Startzeit und Abschaltzustand bleiben offen.25% liefern im Beispiel für einfachen Stationsbetrieb genug, aber nicht für beide Schnelllader ohne weitere Quellen.31,2kW Sauerstoff-/Ladeszenario benötigt bei0,82-Verfügbarkeit rund95% Auslastung. Alle Spitzen43,4kW passen selbst bei intakter40-kW-Anlage nicht gleichzeitig: Aufträge/Lader müssen begrenzt oder über Solar/Akku ergänzt werden.

Bei angenommenem25% Netto-Wirkungsgrad entstehen am Vollpunkt160kW thermisch,40kW elektrisch und120kW direkte Abwärme. Wird auch die gesamte elektrische Leistung lokal verheizt, sind bis160kW abzuführen. In externen Akkus gespeicherte und später außerhalb verbrauchte Energie ist dagegen nicht sofort Stationswärme. Solarwärmeeintrag, Kabinenwärmeverluste und Rückgewinnung sind zusätzliche getrennte Terme. Im Reaktor integrierte Hilfsaggregate sind im Netto-Wirkungsgrad enthalten und werden nicht nochmals als freie Stationslast abgezogen.

Mit Emissionsgrad0,9 und ideal freier kalter Weltraumsicht erfordern120/160kW bei350K rund157/209m², bei400K rund92/122m², bei450K rund57/76m² **tatsächlich abstrahlende Oberfläche**. Grundlage ist `Q=εσA(T⁴−T_Hintergrund⁴)`. Das [bisherige Radiatorkonzept](Reaktor-Konzept-R01-2026-10-24.md) mit vier4×2m-Kassetten hätte32m² pro Seite, ideal64m² mit zwei freien Seiten. Es ist damit nicht automatisch für diese40-kWe-Auslegung ausreichend. Sichtfaktoren, Sonnenbestrahlung, Gehäuseabschattung und Strahlenschutz sind nicht validiert. Wärmeableitung ist notwendiger Bestandteil des späteren Reaktormodells.

Die vorhandene **24-kWh-Stationsbank bleibt unverändert**; anfänglich12kWh. Bei20% Reserve reichen24→4,8kWh ohne Erzeuger im4,2-kW-Bereitschaftsfall rund **4h35min**, vom aktuellen50%-Start bis Reserve nur **1h43min**. Mit laufender Eis-/Wasseranlage sind es rund2h28min bzw.56min. Diese Zahlen enthalten bereits die angenommene95%-Verteilung.

Der bestehende Simulationscode begrenzt Akkuentladung außerdem auf **8kW**. Die Bank kann daher die25-kW-Gesamtlast der Schnellladephase allein überhaupt nicht liefern, unabhängig von ihrer verbleibenden kWh-Menge. Ein Ausbau benötigt getrennt größere Energie *und* höhere zulässige Ein-/Ausgangsleistung.

Umgekehrt darf ein gutes Solarfeld nicht beliebig schnell den24-kWh-Akku füllen. Als konservativ zu prüfender Ansatz wäre etwa0,5C bzw.12kW gespeicherte Ladeleistung möglich; BMS/Zelltyp fehlen noch. Bei hunderten kW PV-Potenzial müsste MPPT den Rest abregeln, sobald Verbraucher und begrenzte Akkuladung versorgt sind. Überschuss ist verfügbare, aber nicht abgenommene Leistung, keine unbegrenzt gespeicherte Energie.

## 6. Vertrag für jede spätere Technologie

Jede Technologie erhält künftig mindestens: Anzahl/Skalierungsregel; Aus-, Bereitschafts-, Arbeits- und Spitzenleistung; Einschaltenergie; Prozessenergie je kg/L/Stück plus Durchsatz/Dauer; Wirkungsgrad; abgegebene Wärme; Energie-/Speicherquelle; Priorität und Verhalten bei Leistungsbegrenzung; Quelle, Datum, Annahmen und Unsicherheit. Lader benötigen zusätzlich Spannung/Strom, Akku-kWh, BMS-/Temperaturgrenzen und Ladekurve. Verbraucher auf Fahrzeugen und Stationsverbraucher dürfen nicht doppelt abgerechnet werden. **Unbekannter Bedarf bleibt als unbekannt markiert und wird nicht still zu0W.**

Dynamische Bilanz: `P(t)=P_feste_Infrastruktur + Σ Anzahl_i(t)×P_Betriebszustand_i(t)`, mit individuellen Tages-/Prozesszyklen. Besatzung verändert insbesondere O₂, Wasser, CO₂ und Nutzung; zusätzliche Räume verändern Licht, Luft und Wärmeströme. Forschung darf Wirkungsgrad, Kapazität, Durchsatz und Leistungsgrenzen ändern, ersetzt aber keine Energiebilanz.

SP-02/03 haben damit eine nachvollziehbare erste Rechnung und einen Katalog, sind **nicht fertig implementiert**. Offen bleiben insbesondere absolute Sternparameter, Magnet-/Gravitationsprinzip, reale I75-Masse und Akkus, thermische Hardware, zusätzliche Habitate sowie materialabhängige Fertigungszeiten.22 analytische Grenz-/Erhaltungsprüfungen bestanden. Keine neue Unity-, Spiel-, Leistungs- oder Hardwareabnahme und kein Commit/Push.
