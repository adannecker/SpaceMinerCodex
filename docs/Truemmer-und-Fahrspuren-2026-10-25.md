# Trümmerkontakte und durchgehende Fahrspuren — 25.10.2026

**Aktueller Fahrbahnstand — 25.10.2026:** Die vorherigen zwei 3-m-Drohnenspuren sind auf Nutzerwunsch durch **vier 1,5-m-Magnetspuren, zwei je Richtung**, mit orangefarbenen Grenzen, Fußabstand-gerechten Magnetleitern, Richtungspfeilen und Wechselzonen ersetzt. Fußgängerstreifen/Puffer bleiben erhalten. [Vier Magnetspuren: aktuelle Umsetzung und Nachweis](Vier-Magnetspuren-2026-10-25.md). Ältere Zwei-Spur-Beschreibungen und damalige Tests unten sind historisch; Trümmerlogik unverändert.

Die fünf bisher gleichförmig nebeneinander stehenden Außenplatten sind durch fünf unterschiedlich große Wrackteile ersetzt. Blech, offener Rahmen, gebrochener Fachwerkträger, abgehobene Wabenhaut und eine beschädigte Kassette verteilen sich deterministisch auf verschiedene Höhen und Seiten der Station. Lage und Drehung sind unregelmäßig; Asteroiden und andere Kontakte erhalten Abstand.

Vor dem ersten Stationsscan bleiben diese fünf Kontaktobjekte in diesem Prototyp ausgeblendet und sind nicht auswählbar oder als Bergungsziel verfügbar. Der erste echte Scan entdeckt sie als **Unbekanntes Teil 01–05**, getrennt vom Asteroidenzähler. Die Kontaktanzeige verrät zunächst Lage und grobe Form; Zusammensetzung und verwertbare Menge bleiben unbekannt. Erst die Bergung legt die nutzbare Ausbeute offen, die stoffliche Analyse bleibt eine eigene Arbeit.

Die bestehende Bergung verwendet weiterhin reale Drohnenbewegung, Rückkehr, Entladung und endliche Funde. Die fünf stabilen IDs `debris-0` bis `debris-4` bleiben erhalten. Pro Kontakt gelten im aktuellen Wirtschaftsprototyp weiterhin 12 kg Schrott und zwei Teile; diese Werte sind keine neue realistische Massenmodellierung. Ein geladener Flug stellt die tatsächliche Route und das mitgeführte Wrack auch im pausierten Zustand wieder her. Alte Abholkoordinaten werden berücksichtigt.

Die fünf Bodenmarkierungen des gesunden Rings folgen nun konstanten konzentrischen Radien statt vor jedem Sektorstoß einzuziehen. Dadurch verschwinden die Zacken und die zusätzliche cyanfarbene Querlinie an den Fugen. Laufspur, Puffer und zwei Drohnenspuren passen weiterhin in die vorhandenen sieben Meter breiten Schotte; Türen und Kollisionskörper wurden dafür nicht verkleinert. Die früheren verjüngten Spuren in älteren Dokumenten beschreiben den Vorgängerstand.

Betroffene Hauptdateien: `StationOperations.Debris.cs`, `StationOperations.Geometry.cs`, `StationOperations.Ui.cs`, `ScannerProgression.cs`, `RingWalkthrough.cs`; Prüfläufe `StationDebrisCheck.cs` und `StationWorkshopCheck.cs`. Die neuen Hand-/Trümmerprüfungen haben getrennte Einstellungen und Raumplanpfade.

## Tatsächliche Prüfung

Finaler Development-Build `debris-hands-build-02`, separat mit Unity 6000.6.4f1 gebaut, Quellprojekt 6000.4.7f1 und Layout 13 erhalten. Der neue Trümmerlauf besteht 685 Assertions: darunter 500 einzelne Abstandsprüfungen zu 100 Asteroiden, echter Erstscan, Collider-Selektion, fünf vollständige Bergungen, endliche Ausbeute sowie laufende, alte und pausiert geladene Transporte. Für spätere Bergungen wurden ausdrücklich 80 L Wasser und 20 kWh als Prüfbestände gesetzt; das ist kein natürlicher vollständiger Prologdurchlauf.

Der Werkstattlauf besteht 2127 Assertions einschließlich 265 zusätzlicher Prüfungen an den echten Fahrspurmeshes: konstante Radien, Sektoranschlüsse und Durchlassfreiheit. Bild `workshop/10-breites-ringschott.png` zeigt die sauberen Bögen. Weitere achtteilige Gesamtprüfung und gesicherte Ergebnisse: [aktueller Nachweis](Pruefungen/Truemmer-Hand-und-Fahrspuren-2026-10-25.json).

Die Trümmerbilder sind gezielte Geometrieaufnahmen ohne HUD; sie belegen keine manuelle Markerbedienung. Selektion und Aufträge wurden über Spiel-APIs und Raycasts geprüft. Materialien bleiben einfache Prototypoberflächen. Keine lückenlose Prüfung der vollständigen Drohnenhülle gegen alle Bergungsrouten, keine belastbare Bergungsphysik, keine neue Release-, Langzeit-, 100k- oder FPS-Abnahme.

Das neue [Drohneninterface](Drohnenleitstand-Konzept-2026-10-25.md) und die veränderte Notversorgung-vor-Reparatur von Drohne 02 bleiben zunächst ein Konzept. Das bestehende Unity-Drohnenpult wurde durch diese Bilder nicht ersetzt.
