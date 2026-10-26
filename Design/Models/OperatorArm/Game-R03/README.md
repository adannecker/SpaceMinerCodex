# OperatorArm R03 – bewegliche Spielfassung

26.10.2026. Grundlage bleibt der ausdrücklich angenommene **R03**, ohne die separaten R04-Ergänzungen. Nach späterer Nutzerkorrektur zeigt das Spiel **nur den Arm von rechts**. Hochauflösendes Original und vereinfachte linke Blenderquelle bleiben unverändert. Keine erneute Tripo-Generierung oder weiteren Credits.

## Dateien und Wiederaufbau

- `OperatorArm-R03-Game.blend`: vereinfachte native Quelle mit UVs, Stoff-/Metallmaterial und Gewichten für Ellenbogen, Handgelenk und Finger.
- `OperatorArm-R03-Game.fbx`: eigenständiger Rig-/Materialaustausch mit eingebetteten 2K-Texturen; kein Laufzeitasset.
- `ArmMesh.bin` und `report.json`: linke Quelldaten in Metern, Normalen, UVs, normalisierte Gewichte und Gelenk-/Fingerkuppenpositionen.
- `ShoulderSleeve.blend`, `.bin`, `ShoulderSleeve-report.json`: getrennte schmale Stoffverlängerung, ohne Brust oder Torso. Der unveränderte R03-Ärmelrand wird direkt am Originalprofil abgetastet.
- `RestPose.png`, `ArticulatedPose.png`, `ShoulderSleeve-Join.png`: tatsächliche Blenderansichten; keine Belege der späteren Spielkameraperspektive.

Blender 4.5.14: zuerst `--background --python-exit-code 1 --python tools/BuildOperatorArmR03.py`, danach `--background --python-exit-code 1 --python tools/BuildOperatorArmSleeveR03.py`. Gespeicherte Quellen können direkt importiert werden. Anschließend in der festgelegten Unityversion das Menü **SpaceMiner → Models → Prepare accepted Operator Arm R03** beziehungsweise `SpaceMiner.Editor.OperatorArmR03Importer.Prepare()` verwenden. Der gemeinsame Arm-/Pultbuilder lautet `SpaceMiner.Editor.OperatorArmBuild.Build()`.

## Vertrag für die Laufzeit

`Resources.Load<GameObject>("OperatorArmR03/Arm")` lädt die **native rechte Ableitung**: Der Importer spiegelt X in Meshpunkten, Normalen, Gelenken und Markern, korrigiert Dreiecksrichtungen und berechnet Tangenten neu. Objekt- und Knochenskalierungen bleiben positiv. Das Prefab heißt `OperatorArm_R03_Right`. Der Import erhält bestehende Asset-GUIDs; beim Ersetzen eines kleineren Meshs werden sämtliche alten Kanäle vorher geleert.

Der ursprüngliche Renderer hat 45.000 Dreiecke, 31.464 Unity-Vertices und 17 Knochen. 2K-Farb-, Normal- und Metall-/Glanzkarten erhalten die angenommene Stoff-/Titanoberfläche. Der zweite Renderer `SleeveContinuation` führt den Stoff schmal aus dem unteren rechten Bildrand. Er verwendet **UpperArm** und **SleeveShoulder**, eine matte Stoffoberfläche und keinen breiten Torso. Die Hauptarmgeometrie wird nicht gestreckt.

Quellkoordinaten vor der rechten Ableitung: +Z zu den Fingern, +Y zum Handrücken, +X zum Daumen, Ursprung am oberen Ärmelabschluss. Gesamtlänge etwa 78,3 cm, Oberarmknochen 23,85 cm, Unterarmknochen 28,64 cm. Der Ärmel ist bereits angewinkelt; IK verwendet die tatsächliche Ruhestellung.

Hierarchie: `Skeleton/UpperArm/Forearm/Wrist`, darunter `Thumb01/02` und je drei Gelenke für `Index`, `Middle`, `Ring`, `Little`. `TipIndex`, `TipMiddle`, `TipRing`, `TipThumb`, `TipLittle` folgen den letzten Fingerknochen. Die ersten drei bestimmen reale gewichtete Hautpunkte für die Berührung; keine getrennten Weltkoordinaten festschreiben. Der zusätzliche Marker `SleeveProximalEnd` prüft den außerhalb der Ansicht liegenden Stoffabschluss.

Spielereingabe, Reichweiten-/Sichtprüfung, Kontakt und einmaliges Öffnen gehören zur gemeinsamen First-Person-Interaktion. **E** berührt die echte Fläche, **Esc** bricht ab. Bewegung und Modalwechsel lösen keinen zweiten Auftrag aus.

## Prüfung und Grenzen

Der Quellbericht hält SHA256 des Originals, tatsächliche Meshgrößen, Gewichte und den FBX-Roundtrip fest. Aktuelle Rechtsarm-, Material-, Naht-, Kontakt- und UI-Prüfungen mit echten Spielbildern stehen in der [aktuellen Fachdokumentation](../../../../docs/Pult-Metallblau-und-Rechtsarm-2026-10-26.md). Die früheren 616 Prüfungen mit linkem Arm und Torso bleiben im [ursprünglichen Integrationsbericht](../../../../docs/Operator-Arm-Spielintegration-2026-10-26.md).

Die Vereinfachung ersetzt keine vollständige manuelle Retopologie; extreme Gelenkwinkel sind nicht freigegeben. Kein vollständiger Spielercharakter, funktionales Handgelenk-HUD oder Werkzeug. Automatisierte Controller-/Framebufferprüfungen sind keine manuelle Komfort-, Headset- oder FPS-Abnahme.
