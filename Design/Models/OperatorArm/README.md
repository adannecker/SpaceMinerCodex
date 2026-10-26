# Operator-Arm: Variantenvergleich

## Dateien

- Revision-R03/OperatorArm-R03.glb und .blend: vom Nutzer übernommene Tripo-Bildrekonstruktion mit durchgehendem Stoffärmel/Handschuh und Unterarmdisplay. Unverändert erhalten.
- Revision-R04/OperatorArm-R04.blend und OperatorArm-R04-Preview.glb: erste lokale Blender-Verbesserung, zusätzliche flache Graphit-/Titanprotektoren, kleine Cyan-/Amber-Markierungen und Servicebefestigungen. Ausgangsmesh samt UVs und Material bleibt erhalten. Noch nicht gestalterisch freigegeben. Frühere OperatorArm-R04.glb bleibt als erste lokale Zwischenfassung erhalten, wird nicht mehr angezeigt.
- Original: gesicherter erster kostenloser Retry, ebenfalls nicht die freigegebene Grundlage.
- Revision-R02: verworfener textbasierter Roboterarm-Entwurf.
- Concept: tatsächlich hochgeladene Bildvorlage für R03.

## Vorschau

Aus dem Repository `work/AvatarTools/blender-4.5.14-windows-x64/4.5/python/bin/python.exe tools/ServeOperatorArm.py` starten. Nur lokal unter http://127.0.0.1:8786/. Der Viewer nutzt vorhandene Three.js-Komponenten und Bibliotheken des Projekts; Modellvarianten über das Auswahlfeld, freie Drehung, Zoom, feste Ansichten und Drahtgitter. Bestehende Pult-/Drohnenviewer bleiben unverändert.

## Grenzen

Keine Unity-Zuweisung, kein Rig, keine Finger-/Ellenbogenanimation und kein funktionales HUD. Maßstab ist noch nicht festgelegt. Hochauflösendes Konzeptmesh mit etwa 1,88 Millionen Dreiecken; Retopologie/LOD vor tatsächlichem Spieleinsatz nötig. Keine weiteren Tripo-Credits für R04 verwendet. Reproduzierbar mit tools/EnhanceOperatorArmR04.py, Blender4.5.14. Prüfbericht liegt in Revision-R04/report.json; lokale Renderaufnahme front.png.
