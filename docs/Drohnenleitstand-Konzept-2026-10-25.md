# Drohnenleitstand — Konzept vom 25.10.2026

**Drohnenleitstand, taktische Übersicht und Arbeitshandschuh — 25.10.2026:** Der Nutzer hat die Umsetzung des Leitstands jetzt bestätigt. Die [reale Oberfläche](Drohnenleitstand-Umsetzung-2026-10-25.md) ist mit vorhandener Flotte, Ausrüstung, Diagnose, passenden Aufträgen und neuer taktischer Übersicht verbunden. [Aktueller Nachweis](Pruefungen/Drohnenleitstand-VR-Hand-2026-10-25.json). Die folgenden sechs Bilder bleiben Konzeptbilder; zusätzliche Hardware für R-01 und die vorgeschlagene Notversorgung-vor-Reparatur-Sequenz für 02 sind weiterhin Entwurf, keine vorgetäuschten Live-Funktionen.

**Historischer Status des ursprünglichen Entwurfs:** Der Nutzer möchte zuerst ein Konzept für eine bildschirmfüllende Verwaltung interner und externer Drohnen. Parallel beauftragt sind verstreute unterschiedlich große Wrackteile mit Erstscan-Erkennung und eine dezent bewegte Hand in der Innenansicht. Das bestehende Spielpult bleibt in diesem Arbeitsstand bestehen. Die neue Drohne-02-Abfolge ist ein Vorschlag, keine bereits implementierte Änderung ihrer Inbetriebnahme.

## Aufbau des Terminals

Das Drohnenpult öffnet mit **E** einen bildschirmfüllenden Leitstand. E/Escape kehrt an dieselbe Spielerposition und Blickrichtung zurück. Oben stehen Bereichswahl, tatsächlicher Zeit-/Pausenzustand, Stationsreserve und Werkstattverfügbarkeit. Die Hand verschwindet während der Bedienung.

| Bereich | Inhalt und Zweck |
| --- | --- |
| Linke Spalte: Flotte | Namen/IDs, Einsatzstatus, Rolle/Werkzeug und aktueller Auftrag. Einsatzfähige und wiederherstellbare Einheiten vorn; weitere defekte Einheiten kompakt aufklappbar. Auswahl bleibt beim Wechsel der Detailreiter bestehen. |
| Mitte: gewählte Drohne | Reiter **Übersicht**, **Ausrüstung**, **Diagnose**. Später echtes drehbares Modell mit markierten Aufnahmen; im Konzept eine Modulübersicht. |
| Rechte Spalte: Aufträge | Laufender Auftrag getrennt von wartenden Aufträgen, Ziel, Fortschritt, konkrete Wartegründe, Voraussetzungen und Priorität. Ziehen zum Umordnen, zusätzlich bedienbare Auf-/Ab-Tasten. |

Die oberen Reiter **Externe Drohnen** und **Interne Drohnen** verwenden dieselbe Bedienlogik, zeigen aber nur passende Ausrüstung, Ziele und Aufträge. Im ersten Umfang gibt es zehn Außeneinheiten und R-01 als interne Einheit. Weitere interne Drohnen werden später ergänzt; keine erfundene zusätzliche aktive Flotte.

Bei geringerer Auflösung oder größerer Schrift dürfen Spalten beziehungsweise Detailbereiche umbrechen. Im Spiel soll die verfügbare Bildschirmfläche genutzt werden; nicht die bisherige lange Liste mit einer überwiegend leeren Auftragsspalte. Eine größere Schrift darf keine Aktionen abschneiden.

## Übersicht, Ausrüstung und Diagnose

- **Übersicht:** Zustand, Heimatdock, aktuelle Tätigkeit, Akku in kWh, bei Außendrohnen Treibwasser in Litern, Fracht/Traglast, Geschwindigkeit, Zielentfernung und geschätzte Restzeit. Zustands-/Fehlerdaten werden nur angezeigt, soweit diagnostiziert; kein frei erfundener Prozentwert für unbekannte Defekte.
- **Ausrüstung:** vorhandene und geplante Werkzeuge getrennt. Sichtbare Aufnahmen für Hauptwerkzeug, Fracht-/Transportaufnahme und später Akku/Tank/Sensorik. Außen zunächst Bohrer, Greifer und Laser; innen Transportgreifer, Reparaturkopf und spätere Fertigungsmodule. Rollen entstehen aus Fähigkeiten und Ausrüstung, nicht nur aus einem umbenannten Schalter.
- **Diagnose:** Zustand pro System, konkrete Fehler, benötigte Teile, letzte Wartung und zulässige Tätigkeiten. Eine defekte Arbeitsaufnahme kann beispielsweise Außenaufträge sperren, obwohl ein kontrollierter Werkstattflug möglich bleibt.

Ein Werkzeugwechsel erzeugt einen **Umbauauftrag** mit Zielwerkstatt, Werkzeugbestand, benötigter Servicekraft und Zeit. Bis zum abgeschlossenen Umbau bleibt das alte Werkzeug montiert. Das zurückgegebene Werkzeug geht wieder ins Lager. Forschung, Aufnahme-Kompatibilität, Teilebestand und Werkstattbelegung werden vor Freigabe geprüft. Keine kostenlose sofortige Umrüstung durch einen UI-Klick.

## Aufträge nach Arbeitsbereich

| Externe Drohnen | Interne Drohnen |
| --- | --- |
| Wasser/Eis an bekannten Quellen abbauen | Material zwischen Anlagen, Lagern und Werkstatt transportieren |
| Unbekannte Wrackkontakte bergen | Geborgenen Schrott analysieren und aufbereiten |
| Außenhülle und Versorgung reparieren | Teile fertigen und Anlagen intern warten |
| Später Bauteile transportieren, montieren und schweißen | Drohnen vorbereiten, Werkzeuge/Teile bereitstellen und intern umrüsten |

Der Ablauf lautet **Drohne → Tätigkeit → passendes Ziel → Voraussetzungen/Kosten → Auftrag vormerken**. Zielauswahl und Werkzeuge müssen zur Tätigkeit passen. Ein Wasserauftrag darf keinen Trümmerkontakt als Ziel erhalten. Bei unbekannten Wracks sind nach dem ersten Scan Lage, grobe Form und Größe bekannt; Inhalt und verwertbare Menge bleiben bis zur Bergung unbekannt.

Wartende Aufträge zeigen einen verständlichen Grund, beispielsweise „Greifer fehlt“, „40 L Stationsreserve unterschritten“, „Werkstatt belegt“, „Schleuse besetzt“, „Ziellager voll“ oder „R-01 offline“. Ein höher priorisierter Auftrag unterbricht keine laufende Arbeit. Die Reihenfolge gilt für den nächsten **ausführbaren** Auftrag der jeweiligen Drohne beziehungsweise Anlage; verschiedene freie Drohnen dürfen parallel arbeiten.

Abbruch eines vorgemerkten Auftrags entfernt ihn. Ein aktiver Auftrag erhält eine sichere Rückkehr-/Abschlussanforderung. Bereits verbrauchte Energie und bereits entnommenes Treibwasser werden nicht pauschal erstattet; unbenutzte reservierte Materialien werden nach sicherem Abschluss genau einmal freigegeben.

## Wiederinbetriebnahme von Drohne02

**Vorschlag:** Drohne02 ist anfangs leer und für Außenarbeit gesperrt. Ihre Flugsteuerung und ihr Antrieb sind noch soweit intakt, dass sie nach Diagnose und Notversorgung kontrolliert zur Werkstatt gelangen kann. Ergibt die Diagnose einen flugunfähigen Defekt, braucht sie stattdessen Reparatur am Dock oder später ein Schleppsystem. Strom allein darf eine mechanisch flugunfähige Drohne nicht freischalten.

1. **Vorprüfung:** Antrieb, Navigation, Akku-/Tankanschluss und sichere Transferfähigkeit feststellen. Werkstatt01 und den geplanten Transfer anfragen.
2. **Notversorgung am Dock:** Strom und Wasser aus den realen Stationsbeständen zuführen. Lebenserhaltung und eine definierte Stationsreserve haben Vorrang. Ziel ist die für den berechneten Transfer samt Sicherheitsreserve nötige Versorgung, nicht zwingend Vollfüllung.
3. **Werkstattfahrt:** Dock lösen, freie Fahr-/Flugspur benutzen, in der tatsächlichen Zweitürschleuse ausschleusen/einschleusen und am Reparaturplatz ankommen. Noch keine Freigabe für Bergung oder Abbau.
4. **Reparatur und Ausrüstung:** R-01 und die vorhandenen Werkstattarme verwenden reservierte Teile und Werkzeuge. Bei Stromausfall oder fehlendem Material sicher warten. Laufende Einrichtungen gegen Raumplanänderung sperren.
5. **Prüfung:** Antrieb, Navigation, Werkzeug und Versorgung prüfen. Erst danach „einsatzbereit“ und für normale Außenaufträge auswählbar. Anschließend zum Heimatdock zurückkehren und regulär versorgen.

Der Entwurf übernimmt **6 kWh/40 L** nur als erkennbare Beispielschwelle aus dem bestehenden Prototyp. Für die spätere Umsetzung sind Stationsreserve, tatsächlich übertragene Energie/Wassermenge und berechneter Transferbedarf getrennte Werte. Versorgungsbilanz und Auftragsreservierungen dürfen keine Bestände doppelt verbuchen.

Speichern/Laden muss Phase, übertragene Mengen, Reservierungen, Ziel-/Heimatdock, reale Pose und belegte Schleuse wiederherstellen. Ein Abbruch während des Ladens stoppt die weitere Entnahme. Während des Transfers wird ein sicherer Haltepunkt oder die Rückkehr verwendet. Nach begonnener Werkstattarbeit muss die Drohne vor dem Abflug wieder transferfähig sein.

**Abgrenzung zum aktuellen Spielcode:** Der bisherige `ActivateSecond`-Auftrag schaltet02 nach einer internen Instandsetzung frei und lädt sie anschließend am Dock. Werkzeugumbauten besitzen bereits eine reale Werkstattfahrt. Die oben vorgeschlagene Notversorgung-vor-Reparatur ist noch ein neuer, eigener Ablauf und im aktuellen Arbeitsstand nicht umgesetzt.

## Architekturvorschlag für die spätere Umsetzung

Die Oberfläche fragt Zustände ab und sendet Befehle; sie verschiebt keine Drohnen und schreibt nicht unmittelbar in Ressourcenbestände.

| Verantwortung | Vorgeschlagene Anbindung |
| --- | --- |
| Gemeinsame Flottenansicht | Stabile Drohnen-ID, Innen/Außen, Status und Fähigkeiten über eine gemeinsame Leseschnittstelle; Adapter für vorhandenen `DroneAgent` und die interne R-01 |
| Fähigkeiten/Ausrüstung | Aufnahmen und kompatible Module als Daten; Arbeits-/Transferfähigkeit getrennt von allgemeiner Einsatzfreigabe |
| Befehle und Aufträge | Flottenservice validiert Befehle und nutzt die vorhandene `StationWorkOrder`-/`StationOperations`-Warteschlange |
| Werkstatt und Logistik | Gemeinsame Reservierung von Platz, Werkzeug, Material und Servicekraft; vorhandene Schleusen-/Routenmechanik weiterverwenden |
| Wiederinbetriebnahme | Eigener gespeicherter Phasenzustand für02; kein zweites unabhängiges Energie- oder Wasserinventar |

Umsetzung nach Bestätigung in überschaubaren Schritten: zuerst Vollbildansicht mit vorhandenen Live-Daten und Aufträgen, danach echte Ausrüstungsverwaltung beider Drohnentypen, anschließend der neue02-Transfer-/Reparaturablauf. Vorhandene Wasseraufträge, Speicherstände und die derzeitige Startregel „nur01 einsatzfähig“ müssen erhalten bleiben.

## Interaktiver Entwurf und Abnahme

Die Gesprächsvorschau enthält Beispieldaten und lokale Bedienzustände: Flottenwahl, Detailreiter, Ausrüstungsaufträge, passende Ziele, Priorität/Abbruch und die schrittweise Wiederinbetriebnahme02 einschließlich R-01-Abhängigkeit. Sie ist **nicht mit dem Spiel verbunden**. Zahlen der internen Drohne und spätere Moduldaten sind Platzhalter für die Gestaltung, keine beschlossenen Hardwarewerte.

Die Vorschau wurde im isolierten Browser mit 16 Prüfungen auf diese Interaktionen, fehlende JavaScript-Fehler, dunkle/helle Darstellung und Breiten 1024/736/320 geprüft. Das ist eine Konzeptprüfung und kein Nachweis für ein bereits neues Unity-Drohneninterface.

Auf ausdrücklichen Nutzerwunsch wurden sechs einzelne Konzeptbilder aus demselben Entwurf gerendert und visuell kontrolliert. Alle zeigen Beispieldaten; die schematische Modulübersicht kann später durch das tatsächliche Drohnenmodell mit auswählbaren Aufnahmen ersetzt werden.

| Ansicht | Bild |
| --- | --- |
| Externe Flotte und Status | [01](Art/Interface/Drohnenleitstand/01-externe-flotte.png) |
| Werkzeugwechsel als Werkstattauftrag | [02](Art/Interface/Drohnenleitstand/02-externe-ausruestung.png) |
| Auftragsprioritäten, Zielauswahl und Diagnose | [03](Art/Interface/Drohnenleitstand/03-auftraege-und-diagnose.png) |
| Interne Drohne R-01 und Inbetriebnahme | [04](Art/Interface/Drohnenleitstand/04-interne-flotte.png) |
| Interne Ausrüstung und Logistik | [05](Art/Interface/Drohnenleitstand/05-interne-ausruestung-und-logistik.png) |
| Drohne 02: Notversorgung und Reparaturphasen | [06](Art/Interface/Drohnenleitstand/06-drohne-02-notversorgung.png) |

Die parallel umgesetzten Spieländerungen und deren tatsächlicher Build-/Prüfstand werden gesondert in der Projektübergabe und im aktuellen Prüfnachweis festgehalten.
