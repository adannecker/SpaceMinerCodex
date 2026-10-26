# Reaktor: Größenreferenz und Modellauftrag

Umsetzung24.10.2026 nach weiterem Nutzerauftrag: Die Modellhülle Ø8m ×6m, Wartungskragen Ø16m und vier3m-Servicegänge sind als einfache begehbare Grundstruktur im Hauptspiel integriert. Das ausgearbeitete Reaktordesign bleibt separat.2kW Spielleistung unverändert; keine bestätigte Abschirmung. [Aktueller Stand und Prüfung](Ringstation-Ausbau.md#v4-grundstruktur-im-hauptspiel-24102026). Die folgenden Angaben zum7m-Platzhalter beschreiben den Ausgangsstand vor diesem Umbau.

Stand 24.10.2026. Auf Nutzerfrage nach realistischen Abmessungen für die Gestaltung in einem anderen Chat. Konkrete neue Modellmaße sind Vorschläge; weder Reaktorleistung noch Station wurden dadurch geändert.

## Tatsächlicher Projektstand

`WaterScenario.cs` setzt derzeit 2 kW Reaktorleistung und 0,5 kW Solarleistung als Spielwerte an. Der Hauptzylinder in `StationVisual.cs` hat nominal Ø7 m × 5 m Höhe (Unity-Zylinder: Skalierung 7/2,5/7; ohne Kappen/Mast). Das ist ein visueller Platzhalter, keine aus Leistung oder Strahlenschutz abgeleitete Konstruktion. Die bisherigen Stationsbilder schlagen ein Reaktorgehäuse Ø8 m und einen Wartungskragen Ø16 m vor.

## Reale Referenz, kein fertiger Stationsreaktor

Die NASA-Studie *A Deployable 40 kWe Lunar Fission Surface Power Concept* beschreibt ein Konzept mit 40 kW elektrischer Nutzleistung. Das verpackte Reaktorsystemelement misst 4,61 m Länge × 1,60 m Breite × 1,60 m Höhe. Es enthält außer dem eigentlichen Reaktor unter anderem Abschirmung, Wärmetauscher, Stirling-Konverter und Pumpentechnik; die ausgefalteten Radiatoren und die übrigen Systemelemente sind damit nicht vollständig abgedeckt. Die Studie nennt 133,4 m² Haupt-Radiatorfläche für den betrachteten Mondpol-Betrieb. Diese Fläche ist ein Referenzfall, keine pauschale Vorgabe für unser Raumfahrzeug. [NASA-Originalstudie, Abschnitte III.A.3/4, Seiten 4–5](https://ntrs.nasa.gov/api/citations/20220004670/downloads/40%20kW%20Deployable%20FSP%20Paper_FINAL.pdf).

Die Studie setzt rund 1 km Abstand zur Besatzung und eine gerichtete Schattenabschirmung voraus. Bei unserem bewohnten Ring um einen zentralen Reaktor schützt eine solche gerichtete Abschirmung nicht alle Aufenthaltsbereiche. Eine Rundumabschirmung mit passenden Durchführungen und Schutz der Servicegänge müsste separat ausgelegt werden; aus der Referenz lassen sich weder sichere Wandstärken noch ein sicheres Gehäusemaß ableiten. Alternativ könnte der aktive Reaktor mit Abstand auf der Stationsachse sitzen und der zentrale Knoten nur Verteilung/Wartung beherbergen. [NASA-Originalstudie, Abschnitt III.A.1, Seite 3](https://ntrs.nasa.gov/api/citations/20220004670/downloads/40%20kW%20Deployable%20FSP%20Paper_FINAL.pdf).

## Vorschlag für ein zunächst gestaltbares Modell

| Bauteil | Modellvorgabe | Bedeutung |
| --- | --- | --- |
| Zentrales geschlossenes Reaktormodul | Ø8 m × 6 m Höhe | Äußere Gestaltungs-/Einbauhülle mit Technik und reserviertem Abschirmraum; keine verifizierte Schutzdimension |
| Wartungs-/Anschlusskragen | Ø16 m außen | Rund um das Gehäuse; nicht als frei zugänglicher Raum direkt am laufenden Kern verstehen |
| Vier Serviceanschlüsse | auf 0/90/180/270° | Passend zu den Stationsachsen; Gänge mit 3 m lichter Breite als bisheriger Entwurf |
| Radiatoren | separat modellieren | Anschlüsse vorsehen; Fläche/Temperatur/Leistung später gemeinsam auslegen |

Die 40-kW-Klasse dient hier nur als reale Größenreferenz. Eine neue Nennleistung, Versorgung des gesamten späteren Produktionsbetriebs oder die Erklärung „beschädigt auf 2 kW“ ist damit nicht beschlossen. Gehäuse und Kühler sollten austauschbar sein, falls Leistung/Abschirmung später andere Maße erfordern.

## Kopierbarer Auftrag für den anderen Chat

> Entwirf ein modulares ziviles Reaktorgehäuse für SpaceMiner, passend zur industriellen Ringstation. Einheit: Meter, Unity-Maßstab 1 Einheit = 1 m. Geschlossenes zylindrisches Hauptmodul mit 8 m Außendurchmesser und 6 m Höhe, separater Wartungs-/Anschlusskragen mit 16 m Außendurchmesser. Vier klar erkennbare Serviceanschlüsse in 90°-Abständen; Stationsgänge sollen später 3 m lichte Breite haben. Dunkelblaue/helle Metallpaneele, dezente cyanfarbene Statusanzeigen, mechanische Wartungsklappen, Strom-/Kühlleitungsanschlüsse und getrennt gestaltete Radiatoren. Keine offen sichtbaren leuchtenden Brennelemente. Hauptkörper, Kragen, Anschlussmodule und Radiatoren als getrennte Teile aufbauen. Maße sind vorläufige Spiel-Modellvorgaben, keine technisch nachgewiesene Reaktor- oder Strahlenschutzauslegung. Die 40-kW-NASA-Studie ist eine Größenreferenz, keine Vorlage für eine bereits sichere zentrale Stationsinstallation. Zunächst Außenansicht und maßbeschriftete Vorder-/Seiten-/Draufsicht erstellen.

Quellen am 24.10.2026 online geprüft; Projektwerte im Code gelesen. Noch kein Reaktormodell, keine Unity-Implementierung und keine thermische oder Strahlenschutzberechnung. Kein anderer Chat wurde dafür angelegt oder angeschrieben.

## Ausgearbeitetes Konzept R01 (24.10.2026)

Auf direkten Nutzerauftrag in Vehicels: [Konzeptbild, exakter Maßplan und Integrationsübergabe](Reaktor-Konzept-R01-2026-10-24.md), mit getrennten Hüllen-/Kragen-/Service-/Radiatorbaugruppen und numerischer JSON-Spezifikation. Bild dient der Gestaltung, SVG/JSON den Modellmaßen. Neue ergänzende Kragen-/Anschlusshöhen und Radiatorgrößen sind Vorschläge; Abweichungen zum inzwischen von MainDev eingebauten V4-Blockout sind ausdrücklich beschrieben. Keine neue Reaktorleistung oder verifizierte Strahlenschutz-/Wärmeauslegung, kein Detailmodell durch diesen Konzeptauftrag eingebaut.
