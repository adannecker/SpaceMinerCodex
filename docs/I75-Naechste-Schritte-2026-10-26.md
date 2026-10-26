# I75: nächste To-dos – Docking, Energie und Werkzeuge

26.10.2026. Nutzer beauftragt die Dokumentation der nächsten Aufgaben nach Freigabe der [R02-Gestaltung](Innendrohne-I75-Textur-R02-2026-10-26.md): passende Andock-/Ladestellen und anschließend die Werkzeugausrüstung. Induktive Ladung während der Bewegung wird ausdrücklich als Möglichkeit erfragt; eine bestimmte Ladetechnik ist damit noch nicht beschlossen. Alle folgenden Umsetzungspunkte sind offen.

## 1. Docking- und Ladeplätze

- [ ] Zum 75-cm-Grundkörper passende Andockgeometrie entwerfen: Einfahrt, Ausrichtung, mechanischer Anschlag/Halt, Ein- und Ausfahrfreiraum, erreichbare Lade-/Serviceanschlüsse.
- [ ] Plätze neben der Durchgangsfahrbahn vorsehen. Wartende oder ladende Drohnen dürfen weder Fahrspuren noch Werkstatttüren oder Wechselzonen blockieren. Zufahrt, Reservierung, Warteschlange und Ausfahrt gemeinsam mit der Verkehrssteuerung planen.
- [ ] Ladedock als getrennte modellierbare Baugruppe bauen, mit sichtbarer Belegung und Ladezustand. Wartungs- und spätere Werkzeugwechselplätze mitdenken; Ladeplatz und Werkzeugwechsel sind nicht automatisch dieselbe Einrichtung.
- [ ] Ladekontakte versus stationäres induktives Ladepad vergleichen und auswählen. Beide benötigen passende Versorgung, einen Empfänger/Anschluss an der Drohne und eine Lade-/Akkuregelung.
- [ ] Akkukapazität, Ladeleistung, Wirkungsgrad, Temperatur-/Ladebegrenzung und Stationsanschluss gemeinsam auslegen. Werte einstellbar halten; keine Zahlen aus der Außendrohne ungeprüft übernehmen.
- [ ] Ergänzender Nutzerauftrag vom26.10.2026: Verbrauch für Schweben/Fahren, Bereitschaft, Werkzeuge und Transportlast berechnen; Energie pro Einsatz, Laufzeit und gleichzeitige Drohnen in die Stations-/Reaktorbilanz aufnehmen. Ladegeschwindigkeit anhand zum Umsetzungszeitpunkt aktueller belastbarer Akku-/Ladegerätequellen auslegen, inklusive Ladeverlauf, Teil-/Vollladung, Verlusten und verfügbarer Netzleistung. Noch keine neuen Verbrauchs-, Kapazitäts- oder Ladewerte beschlossen.
- [ ] Anfahrreserve inklusive möglicher Wartezeit, automatisches Anfahren bei niedrigem Akkustand, Verhalten bei besetztem oder stromlosem Dock und Fortsetzen nach dem Laden definieren.
- [ ] Reale Stationsenergie und verfügbare Anschlussleistung abrechnen. Kein zusätzlicher Gratisakku, keine kostenlose Versorgung und kein stiller Eingriff in die bisherige gemeinsame24-kWh-Bank.

## 2. Induktive Versorgung auf der Fahrstrecke – offene Ausbauoption

**Technisch plausibel:** Industrielle Systeme übertragen Energie entweder kontinuierlich über einen Linienleiter entlang der Strecke oder punktuell an Ladeplätzen. Herstellerbeispiele sind [SEW MOVITRANS line](https://www.sew-eurodrive.de/movitrans-line/) und [MOVITRANS spot](https://www.sew-eurodrive.de/produkte/energieuebertragung-und-energieversorgung/movitrans-spot/movitrans-spot.html); [Funktionsübersicht](https://www.sew-eurodrive.de/movitrans/). Diese Beispiele belegen das Prinzip, keine passende Auslegung für unsere Drohne.

- [ ] Ausgerüstete Ladeabschnitte mit Einspeisung/Leistungselektronik und einem gesonderten Empfänger am Fahrzeug entwerfen. Die bisher sichtbaren Magnetleitungen bewirken allein keine Energieübertragung.
- [ ] Empfängerhöhe, Luftspalt, seitliche Ausrichtung und Zusammenwirken mit Magnetführung/Schweben prüfen. Die sichtbaren8cm Bodenfreiheit sind bislang ein Darstellungswert, keine bestätigte elektrische Kopplungsdistanz.
- [ ] Fahrstreckenversorgung von tatsächlicher Akkuladung unterscheiden: Die eingehende Leistung kann zunächst den Bordbetrieb decken; nur ein Überschuss nach Verlusten und Verbrauch kann innerhalb der Akkugrenzen laden. Kurze Ladeabschnitte liefern entsprechend wenig Energie; Fahrtempo und Aufenthaltszeit berücksichtigen.
- [ ] Unversorgte/beschädigte Abschnitte, Schottübergänge, Lastspitzen und Rückkehr zum festen Ladeplatz prüfen. Energiebedarf und Netzverluste bleiben Bestandteil der Stationsbilanz.
- [ ] Als möglichen späteren Tier-Ausbau bewerten, nicht als bereits freigegebene Standardausstattung behandeln. Verbesserungen von Akku und Ladetechnik gemeinsam balancieren.

Vorschlag für den ersten Prototyp: ein fester Ladeplatz, der auch induktiv arbeiten könnte. Dynamische Einspeisung später getrennt ausprobieren. Ein Dock bleibt unabhängig davon als Park-, Wartungs- und gegebenenfalls Umrüstplatz sinnvoll. Diese Reihenfolge und die Ladetechnik sind Vorschläge, keine zusätzliche Nutzerentscheidung.

## 3. Gemeinsame Werkzeugaufnahme

- [ ] Befestigungspunkte am R02-Modell festlegen: mechanische Führung/Verriegelung, Strom-/Datenschnittstelle, Werkzeug-Pivot und Arbeits-/Verstaumaße.
- [ ] Werkzeuge als eigene Baugruppen mit beweglichen Gelenken, sinnvollen Kollisionskörpern und klaren Aufnahmen modellieren. Grundkörper und Werkzeug bleiben getrennt wiederverwendbar.
- [ ] Umrüsten in der Werkstatt planen: vorhandene Werkzeugkassette ablegen, andere verriegeln, Wechselzeit und reale Werkzeugbestände/Kosten berücksichtigen.
- [ ] Grundregel erhalten: **eine Drohne hat jeweils eine Aufgabenrolle**; Rollenwechsel erfordert Umrüstung. Hilfsgreifer eines Werkzeugs erlauben keinen zweiten unabhängigen Auftrag.

### Idee: Werkzeugwechseldock mit Kran und Montagearm – 26.10.2026

Vom Nutzer als Gestaltungsidee für die internen Drohnen eingebracht: Die Drohne fährt an ein Dock; ein stationsseitiger Kran mit Montagearm holt das Werkzeug, setzt es an und befestigt es. Falls bereits ein Werkzeug montiert ist, wird dieses zuerst abgenommen. Die Idee gehört zur Ausarbeitung von Sprintpaket SP-10, mit Platz-/Dockabstimmung zu SP-04; genaue Mechanik und Maße sind noch offen.

Vorgesehener Ablauf zur Ausarbeitung und Animation:

1. Drohne fährt ein und dockt in einer definierten Montageposition an.
2. Falls vorhanden: Arm hält das bisherige Werkzeug, löst dessen Befestigung/Schnittstellen und legt es auf einem Werkzeugplatz ab.
3. Kran und Montagearm holen das gewünschte separate Werkzeug von seinem Lagerplatz und führen es an die gemeinsame Aufnahme.
4. Werkzeug ausrichten, ansetzen und mechanisch befestigen; Energie-/Datenschnittstellen verbinden.
5. Als Ausarbeitungsvorschlag: Verriegelung und Verbindung prüfen, Arm/Kran aus dem Fahrraum zurückziehen und die Drohne zur Ausfahrt freigeben.

- [ ] Getrennte bewegliche Baugruppen für Kran, Montagearm, Greifer, Werkzeugablagen und Dock modellieren; Bewegungsraum und Zugänglichkeit der Befestigungen berücksichtigen. Ob Kran und Arm eine gemeinsame Einheit bilden, bleibt auszuarbeiten.
- [ ] Werkzeugwechsel als sichtbaren Ablauf mit realen Werkzeugbeständen planen; kein bloßer Modelltausch ohne Abnahme-/Anbauvorgang. Eine Aufgabenrolle pro Drohne bleibt erhalten.
- [ ] Halten/Fixieren der Drohne während der Montage, freie Werkzeugschnittstelle, fehlendes Werkzeug und unterbrochenen Wechsel berücksichtigen. Wechselzeit und Energiebedarf zusammen mit der Stationsbilanz auslegen.

Nur Idee und offene Ausarbeitungsaufgaben dokumentiert; kein Dock-/Kranmodell, keine Animation oder Wechselsteuerung erstellt.

## 4. Aufgabenspezifische Module

| Offenes Modul | Geplante Funktion / nötige Ausarbeitung |
| --- | --- |
| Bau / Montage / Laser | Gelenkarm, Füge-/Laserkopf und Haltefunktion; Bauteile positionieren, fügen und bearbeiten. Tatsächliche Teile und passende Arbeitsenergie erforderlich. |
| Demontage | Schraub-/Drehmomentwerkzeug, Gegenhalter, gegebenenfalls Trennwerkzeug und Aufnahme demontierter Teile. Verschraubungen lösen und Materialtrennung als unterschiedliche Arbeitsgänge behandeln. |
| Transport | Greifer mit mechanischer Lastauflage und Sicherung. Aufnehmen, halten, transportieren und ablegen; auch den bereits gewünschten Träger für aufrechte Wabenpaneele berücksichtigen. |

- [ ] Werkzeugmasse, Last, Schwerpunkt, Reichweite, Arbeitskräfte und Stützenbedarf prüfen. Werkzeugleistung und Ladebilanz zusammen simulieren; keine unbegrenzte Arbeitsenergie aus der Magnetführung annehmen.
- [ ] Animationen, Lade-/Umrüstzustände und tatsächliche produktive Aufträge anschließend in Unity anschließen. Die bisherigen zehn Fahrprobenakteure sind dadurch noch keine produktiven Zusatzdrohnen.

## Status und Quellenprüfung

### Ergänzende Modellkorrekturen – Nutzerhinweise vom26.10.2026

- [ ] R02-Texturen von allen Seiten prüfen; einige Stellen passen laut Nutzer noch nicht. Betroffene Flächen dokumentieren und UV-/Materialübergänge nacharbeiten.
- [ ] Vom Nutzer gemeldetes Loch an der Unterseite am Mesh prüfen und schließen. Anschließend Normalen, Texturen und Exporte kontrollieren. Noch keine unabhängige Sicht-/Geometrieprüfung dieses Befunds erfolgt.

Weitere neue Aufgaben für Avatar, Steuerpult und Stromoberfläche stehen im [Ideenbacklog](Ideenbacklog.md#neue-prüf--und-überarbeitungsaufgaben--26102026). Die R02-Freigabe als gestalterische Grundlage bleibt bestehen; die offenen Detailmängel werden damit nicht als behoben behandelt.

Nur To-do-/Konzeptdokumentation ergänzt; keine Dock-/Werkzeugmodelle oder Energiesimulation implementiert. Hersteller-Suchauszüge von SEW am26.10.2026 geprüft; keine Leistungs-/Effizienzwerte oder Ladezeiten für I75 daraus übernommen. Andere zuerst gefundene Hersteller-/Forschungsseiten waren beim Vollabruf teilweise nicht erreichbar und werden nicht als zusätzliche Auslegungsnachweise verwendet. Keine neuen Spieltests oder Builds.
