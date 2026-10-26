# Handdarstellung in der Stationsansicht — 25.10.2026

**Queststart ohne unsichtbare Dialogsperre — 25.10.2026:** Die reine Handkamera empfängt mit `eventMask=0` keine automatischen Unity-Mausereignisse. 207 gezielte Handschuhprüfungen im korrigierten Build, einschließlich Kamera-Strahlraster bei Fenster-/Vollbildwechsel, bestanden. [Nachweis](Pruefungen/Queststart-Mira-2026-10-25.json). Der Queststillstand hatte die separat behobene Dialogsperre als Ursache.

**Drohnenleitstand, taktische Übersicht und Arbeitshandschuh — 25.10.2026:** Der neue dunkle gepolsterte Handschuh wurde im finalen `drone-console-build-05` mit 56 gezielten Assertions geprüft. 1444 Vertices; echte Innen-/Außen-/Terminal-/Raumplan-/Nahwandaufnahmen gesichert, bestehende Sichtbarkeits-, Kamera-, Bewegungs- und Cleanup-Verträge erhalten. Der Griff bleibt leer; ToolMount ist Vorbereitung für spätere Werkzeuge. [Gesamtnachweis](Pruefungen/Drohnenleitstand-VR-Hand-2026-10-25.json). Quellenbasierte Vorschauen und ältere Abnahmen unten sind als solche zu unterscheiden.

## Aktuelles Verhalten

In der begehbaren Station erscheint rechts unten eine rechte Hand in einem dunklen, gepolsterten Arbeitshandschuh. Vier gegliederte Stofffinger und der darüberliegende Daumen bilden einen lockeren geschlossenen Griff. Ein grauer Anzugärmel führt von unten rechts zur Manschette. Die am 25.10.2026 gezeigte Handschuhreferenz bestimmt diese Überarbeitung; der Griff bleibt ohne Werkzeug sichtbar.

Das prozedurale Modell besitzt 1.444 Vertices, 2.784 Dreiecke, einen MeshRenderer und vier matte Materialien für Stofffinger, flexible Falten, dunkle Polster und Ärmel. Eine deterministisch erzeugte, gemeinsam verwendete 128 × 128-Stofftextur ergänzt die Oberfläche. Die gebogenen Finger verwenden fortgeführte Querschnittsachsen, damit sich das Mesh beim Einrollen nicht verdreht. Es hat keine Collider und wirft oder empfängt keine Szenenschatten. Es ist eine Darstellung für die bestehende First-Person-Ansicht, keine neue VR-Steuerung.

Leichte Ruhebewegungen, Gehbewegungen und eine verzögerte Reaktion auf den Blickwechsel geben der Hand Bewegung. Die tatsächliche Positionsänderung und Blickbewegung der Innenansicht treiben diese Darstellung an; Teleports werden nicht als Schritte interpretiert. Der zusätzliche Bewegungsoffset bleibt auf 1,8 cm begrenzt, der zusätzliche Winkel auf 4°. Bei pausierter Simulation läuft die Bewegung nicht weiter. Die Komponente bietet `ReducedMotion` und `MotionStrength` als Vorbereitung für weitere Einstellungen; derzeit gibt es dafür keinen neuen Eintrag im Einstellungsmenü.

Die Hand wird in der Außenansicht, im Startmenü, während Intro und Erinnerungsszenen, bei Mira-Dialogen sowie in Terminals, Raumplanung und anderen blockierenden Menüs ausgeblendet. Nach dem Schließen gelten weiterhin die vorhandenen Eingabesperren des jeweiligen Menüs. Die neue Pose liegt unterhalb und rechts vom Fadenkreuz; ihre HUD-Verträglichkeit wurde in den im aktuellen Nachweis verlinkten Unity-Aufnahmen geprüft.

Eine eigene Kamera rendert ausschließlich Layer 31 nach der Weltkamera mit eigenem kurzem Tiefenbereich. Dadurch verschwindet die Hand nicht in einer nahen Wand. FOV, Seitenverhältnis, Bildausschnitt und Ausgabeziel folgen der Hauptkamera; deren Near-/Far-Einstellungen bleiben unverändert. Layer 31 wird aus der Hauptkamera ausgeschlossen und beim Deaktivieren oder Entfernen der Komponente entsprechend dem ursprünglichen Zustand wiederhergestellt. Kamera, Mesh, private Materialien und die erzeugte Stofftextur werden beim Entfernen freigegeben.

## Vorbereitung für Werkzeuge

`ToolMount` ist ein leerer Transform als späterer visueller Griff-/Werkzeuganschluss. Er sitzt im Modellraum bei `(0; 0,055; 0,043)` m; seine lokale Y-Achse zeigt entlang der Modell-X-Achse durch die eingerollten Finger. Die tatsächlichen Meshpunkte lassen um diese Achse einen freien Radius von ungefähr 17,5 mm. Ein späteres Werkzeug muss zu diesem Griff und seiner Orientierung passen. Es gibt damit noch keinen funktionierenden Scanner, keine Werkzeugauswahl, kein Inventar und keine Greif- oder Reparaturaktion. Diese Mechaniken und die dazugehörigen Animationen müssen separat entworfen und implementiert werden.

## Betroffene Dateien

- `Assets/SpaceMiner/Scripts/StationFirstPersonHands.cs` und `.meta`: Modell, Kameradarstellung, Sichtbarkeit und Bewegung.
- `Assets/SpaceMiner/Scripts/StationInteriorMode.cs`: erzeugt und initialisiert die Komponente an der vorhandenen Innenansicht; Zugriff über `Hands`.
- `Assets/SpaceMiner/Scripts/StationFirstPersonHandsCheck.cs` und `.meta`: gezielte Development-Prüfung mit `-stationHandsCheck`.
- Die gemeinsame Build-/Prüfisolation berücksichtigt den Prüfflag bei Einstellungen und Raumplandateien; die Prüfung verwendet eigene Savegame- und Cinematic-Präferenzpfade.

## Prüfung der aktuellen Überarbeitung

Die aktuelle Mesh-Generierung wurde ohne Start eines Unity-Prozesses aus dem tatsächlichen C#-Quelltext extrahiert und gegen einen isolierten Mathematik-/Mesh-Rahmen kompiliert und ausgeführt. Das ergab 1.444 endliche Vertices, 2.784 Dreiecke und vier Submeshes. Der freie Griffraum wurde anhand dieser Meshpunkte nachgerechnet. Eine Software-Vorschau mit der neuen Pose bei 1600 × 1000 liegt unter `Logs/HandWorkGlovePreview/01-source-mesh-camera-preview.png`; die Detailansicht heißt `02-source-mesh-detail-preview.png`.

Diese beschrifteten Vorschauen sind **keine Unity-Aufnahmen**. Sie zeigen weder das tatsächliche Unity-Material und dessen Stofftextur noch Szenenlicht und HUD. Der überarbeitete Development-Check prüft zusätzlich die vier dunklen Materialien, die gemeinsame Stofftextur, den freien Griffraum und die Texturfreigabe. Zum Zeitpunkt dieser Offline-Vorprüfung standen Unity-Build und Laufzeitabnahme noch aus; deren abgeschlossene Ergebnisse stehen im aktuellen Nachweis am Dokumentanfang.

## Bereits geprüfter technischer Ausgangsstand

Der damalige lokale Development-Build 02 mit Unity 6000.6.4f1 wurde am 25.10.2026 erfolgreich erstellt. `Logs/StationHands/result.json` meldete **52 bestandene Assertions** und 900 Vertices; das zugehörige Laufzeitlog ist `Logs/debris-hands-hands-02.log`. Diese Ergebnisse betreffen das vorherige Handschuhmodell und sind keine Abnahme der neuen Form.

Geprüft wurden Geometrie, fehlende Collider/Schatten, getrennte Kameralayer und Projektion, begrenzte Ruhe-/Geh-/Blickbewegung, Pause und reduzierte Bewegung, echte Innen-/Außenwechsel, Terminal-/F7-/Settings-Sichtbarkeit sowie das Wiederherstellen der Kameramaske und die Freigabe der privaten Kamera und des Meshes. Sechs native Framebuffer-Aufnahmen wurden gespeichert und visuell geprüft:

| Datei unter `Logs/StationHands/` | Inhalt |
| --- | --- |
| `01-idle-hand-1600.png` | Hand in der tatsächlichen Innenansicht |
| `02-walk-look-hand.png` | Ansicht nach tatsächlichen Geh- und Blickänderungen |
| `03-terminal-no-hand.png` | Echtes geöffnetes Stationsterminal ohne Hand |
| `04-roomplan-no-hand.png` | Tatsächlicher F7-Raumplaner ohne Hand |
| `05-declared-close-wall-fixture.png` | Hand vor einer ausdrücklich für den Test erzeugten nahen Fläche |
| `06-outside-no-hand.png` | Außenansicht ohne Hand |

Die Nahwandaufnahme ist eine **explizite temporäre Prüffläche** vor der Kamera, kein Foto einer regulären Stationswand. Sie bestätigt die Tiefentrennung der Darstellung; es wurde damit keine physische Hand-Wand-Kollision umgesetzt. Die Aufnahme kann deshalb die Welt vollständig verdecken, während die Hand sichtbar bleibt.

In den sechs damals geprüften Bildern wurden keine fehlerhafte Handüberlagerung oder durch die Hand verdeckte Bedienelemente festgestellt. Das prozedurale Handschuhmodell bleibt ein Gestaltungsstand. Eine manuelle Komfortabnahme in längeren Spielsitzungen, FPS-/Leistungsmessung, Abnahme anderer Auflösungen und Bildschirmformate, VR-/Headset-Prüfung sowie eine Tool-Gameplay-Abnahme wurden hierfür nicht durchgeführt. Einzelbilder belegen außerdem keine subjektive Qualität des Bewegungsgefühls.
