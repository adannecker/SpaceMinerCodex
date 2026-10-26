# Operator-Arm: Variantenvergleich

## Spielfassung R03

Auf Nutzerauftrag vom 26.10.2026 wird der angenommene R03 mit Arm-/Fingergelenken und Dreifinger-Touchbedienung der E-Konsolen verwendet. Die spätere Korrektur verlangt **nur den Arm von rechts**, ohne breiten Torso. Der Unity-Importer spiegelt das linke Original geometrisch und erhält positive Objekt-/Knochenskalierungen; native Originale bleiben unverändert. [Game-R03](Game-R03/README.md) enthält optimierte Meshdaten, Blender-/FBX-Fassung, schmale Ärmelverlängerung und Wiederaufbauhinweise; [aktuelle Darstellung und Prüfung](../../../docs/Pult-Metallblau-und-Rechtsarm-2026-10-26.md). R04 des Arms bleibt eine separate, nicht bestätigte Modellvariante.

## Dateien

- Revision-R03/OperatorArm-R03.glb und .blend: vom Nutzer übernommene Tripo-Bildrekonstruktion mit durchgehendem Stoffärmel/Handschuh und Unterarmdisplay. Unverändert erhalten.
- Revision-R04/OperatorArm-R04.blend und OperatorArm-R04-Preview.glb: erste lokale Blender-Verbesserung, zusätzliche flache Graphit-/Titanprotektoren, kleine Cyan-/Amber-Markierungen und Servicebefestigungen. Ausgangsmesh samt UVs und Material bleibt erhalten. Noch nicht gestalterisch freigegeben. Frühere OperatorArm-R04.glb bleibt als erste lokale Zwischenfassung erhalten, wird nicht mehr angezeigt.
- Original: gesicherter erster kostenloser Retry, ebenfalls nicht die freigegebene Grundlage.
- Revision-R02: verworfener textbasierter Roboterarm-Entwurf.
- Concept: tatsächlich hochgeladene Bildvorlage für R03.

## Vorschau

Aus dem Repository `work/AvatarTools/blender-4.5.14-windows-x64/4.5/python/bin/python.exe tools/ServeOperatorArm.py` starten. Nur lokal unter http://127.0.0.1:8786/. Der Viewer nutzt vorhandene Three.js-Komponenten und Bibliotheken des Projekts; Modellvarianten über das Auswahlfeld, freie Drehung, Zoom, feste Ansichten und Drahtgitter. Bestehende Pult-/Drohnenviewer bleiben unverändert.

## Grenzen der früheren Konzeptdateien

Die hochauflösenden Konzeptdateien R03/R04 haben keine Unity-Zuweisung, kein Rig und kein funktionales HUD. Ihr Maßstab ist nicht festgelegt. Das Konzeptmesh hat etwa1,88 Millionen Dreiecke; die bewegliche Spielfassung wurde deshalb separat vereinfacht. Keine weiteren Tripo-Credits für R04 verwendet. R04 reproduzierbar mit tools/EnhanceOperatorArmR04.py, Blender4.5.14. Prüfbericht liegt in Revision-R04/report.json; lokale Renderaufnahme front.png.
