# Touch-Steuerpult: Blender-Modell — 26.10.2026

## Auftrag und Lieferumfang

Der Nutzer hat das [Touch-Konzept V2](Steuerpult-Konzept-2026-10-26.md) zur möglichst getreuen Umsetzung freigegeben. Anschließend präzisiert: **zuerst in Blender gestalten und rechts zeigen**. Daher wird zunächst das tatsächliche Modell samt Materialvorschau geliefert; die Einbindung in die Kommandoraum-Pulte folgt gesondert. Bestehende Laufzeitoberflächen, Raumpläne und Spielbuilds werden durch diesen Modellschritt nicht umgebaut.

Die Bauform übernimmt die niedrige, nach hinten ansteigende Touchfläche, helle tragende Metallwangen, Graphitverkleidung, zurückgesetzten Fußraum, Wartungsflächen, kleine Befestigungen und die Frontbeschriftung. Keine physischen Knöpfe, Tasten oder Drehregler. PBR-Oberflächen mit gebürstetem Metall, matter Lackierung und dezenten Gebrauchsspuren sind selbst erzeugt; das KI-Konzept ist die Gestaltungsreferenz, keine auf das Objekt geklebte Textur.

## Native Quelle und Vorschau

![Blender-Modell mit beispielhaftem Touchscreen](../Design/Models/TouchConsole/Renders/04-demo-screen-fixture-not-gameplay.png)

[Gerenderte Vorderansicht](../Design/Models/TouchConsole/Renders/05-demo-front-fixture-not-gameplay.png) · [gerenderte Seitenansicht](../Design/Models/TouchConsole/Renders/06-demo-side-fixture-not-gameplay.png).

| Datei | Zweck |
| --- | --- |
| `Design/Models/TouchConsole/TouchConsole.blend` | Bearbeitbares Grundmodell, ohne vorgetäuschte Liveanzeigen |
| `Design/Models/TouchConsole/TouchConsole-Preview.blend` | Beleuchtete Vorschau mit separatem beispielhaftem Touchscreen |
| `tools/CreateTouchConsole.py` | Reproduzierbare Geometrie-, UV-, PBR- und Exporterzeugung |
| `Design/Models/TouchConsole/model-report.json` | Maße, Materialgruppen, Anker, LOD-Zahlen und Exportprüfungen |
| `Design/Models/TouchConsole/Renders/` | Echte Blender-Renderings des Modells |
| `Assets/SpaceMiner/Resources/TouchConsole/` | Vorbereitete FBX-/PBR-Exporte samt Metas; noch keinem Spielpult zugewiesen |

Die nativen `.blend`-Dateien bleiben außerhalb von `Assets`. Ein späterer Unity-Import der FBX-Exporte setzt daher keine lokal installierte Blender-Version voraus. Das gemeinsame Material benutzt einen 2048²-PBR-Atlas; Glas und Frontplakette sind getrennt ansprechbar.

## Modellaufbau

Hüllmaß **1,50 × 1,14 × 1,00 m** (Breite × Gesamthöhe × Tiefe). Die Glasfläche steigt um **12°** nach hinten an. Nutzbare Bildschirmfläche etwa **1,23 × 0,79 m**. Vorderseite ist Unity-lokal **−Z**, Ursprung ist der Bodenmittelpunkt. Der technische Bericht enthält die genauen Glas-/Beschriftungsanker für die spätere Integration.

Sechs Meshgruppen in LOD0: `Chassis`, `AlloyFrame`, `ServicePanels`, `Fasteners`, `TouchGlass`, `FrontPlaque`. LOD2 lässt die einzelnen Befestigungen weg. Drei vorbereitete Detailstufen: **17.268 / 3.232 / 472 Dreiecke**. Das ist eine geometrische Vorsorge, noch kein Laufzeitnachweis für Distanzwechsel oder Spielleistung.

Das Exportglas und die Frontplakette bleiben blank, damit im Spiel echte Daten und die jeweilige Funktion erscheinen können. Die getrennte Blender-Vorschau darf die Beschriftung **STROMVERSORGUNG** und eine beispielhafte Oberfläche zeigen. Deren 50-%-Akku und ausgeschaltete Reserve sind bewusst gewählte Vorschauwerte, keine aus einem laufenden Spiel gemessenen Daten. Die Vorschau-UI wird nicht in die FBX-Geometrie exportiert. Andere Pultfunktionen können später dieselbe Modellquelle nutzen.

## Prüfung und Grenzen

Abgeschlossen mit Blender **4.5.14 LTS**: alle drei FBX-Dateien erneut importiert und endliche Koordinaten, erhaltene Hüllmaße, UV-Schichten und Dreieckszahlen geprüft. Die **31** im endgültigen Modellbericht protokollierten Dateien wurden in dieser Sitzung erneut per SHA256 abgeglichen. Keine Abweichung. Native Modell- und Vorschaudatei sind komprimiert gespeichert; Texturen bleiben enthalten. [Prüfnachweis](Pruefungen/Steuerpult-Blender-2026-10-26.json), [detaillierter Modellbericht](../Design/Models/TouchConsole/model-report.json).

Die endgültigen Blender-Ansichten 04, 05 und 06 wurden visuell geprüft: dunkel durchgehende, flache Touchanzeige ohne Plattenwirkung, richtig lesbare Frontbeschriftung, helle Metallfassung und geschlossener Unterbau. Beim Vorschauabgleich korrigiert wurden ein zu hoch liegender verdeckter Chassisdeckel, gespiegelte Vorschautexte und die zunächst zu reflektierende/erhabene Demo-UI. Die korrigierte Vorschau verwendet flache selbstleuchtende UI-Flächen; ihre Daten bleiben ausdrücklich Beispiele. Echte PBR-Materialien des Grundmodells bleiben separat erhalten.

Keine Unity-Build-, Spiel-, Kamera-Ergonomie-, Eingabe- oder FPS-Abnahme für dieses Modell. Noch keine Live-UI und kein Umbau der fünf vorhandenen Nebenpulte. Ein vorbereiteter, unintegrierter Checkentwurf wurde vollständig außerhalb von `Assets` unter `work/TouchConsoleDeferred` abgelegt; er beeinflusst die Unity-Kompilation nicht. Kein Commit/Push.
