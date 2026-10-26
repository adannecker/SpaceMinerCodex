# BatteryBay: Modellquelle und Unity-Einsatz

Stand 25.10.2026. Projektspezifisch erzeugtes Blender-/PBR-Modell für die acht Akkubuchten. Die Ringdruckhülle und ihre Öffnungen entstehen separat in `RingWalkthrough.BatteryBays.cs`. Das Modell enthält keine Zellchemie, Kapazitätsberechnung oder Einzelbank-Simulation.

**Prüfstand:** Blender-Export, separater FBX-Roundtrip und native Unity-Playeransichten geprüft. `battery-bays-build-07`: 550 gezielte Buchtenprüfungen sowie sechs Regressionen, insgesamt 1925 Assertions / Exit 0, 39 gesicherte Bilder. [Nachweis und Grenzen](../../../docs/Pruefungen/Akkubuchten-2026-10-25.json), [gesamte Platzierung und Umsetzung](../../../docs/Akkubuchten-Konzept-2026-10-25.md). Keine neue manuelle oder FPS-Abnahme.

## Dateien

| Zweck | Projektpfad |
| --- | --- |
| Native bearbeitbare Quelle | `Design/Models/BatteryBay/BatteryBay.blend` |
| Generator | `tools/CreateBatteryBay.py` |
| Maße, Gruppen, Dreiecke und Asset-SHA256 | `Design/Models/BatteryBay/model-report.json` |
| Unity-Modelle | `Assets/SpaceMiner/Resources/BatteryBay/BatteryBay_LOD0.fbx`, `BatteryBay_LOD1.fbx`, `BatteryBay_LOD2.fbx` |
| Materialien | `Assets/SpaceMiner/Resources/BatteryBay/Materials/BatteryBay_Surface.mat`, `BatteryBay_Display.mat`, `BatteryBay_Status.mat` |
| PBR-Texturen | `Assets/SpaceMiner/Resources/BatteryBay/Textures/BatteryBay_Albedo.png`, `BatteryBay_Normal.png`, `BatteryBay_MetalSmooth.png`, `BatteryBay_MetalRough.png` |
| Lokale Blender-Vorschauen | `Logs/BatteryBayAsset/01-installed-three-quarter.png`, `02-front-service-view.png`, `03-empty-frame-housing.png`, `04-cassette-detail.png` |

Zu den Unity-Ordnern, FBX-, Material- und Texturdateien gehören die vorhandenen `.meta`-Dateien, einschließlich `Assets/SpaceMiner/Resources/BatteryBay.meta`. Zusammen übertragen und bestehende GUIDs erhalten. Die native `.blend` liegt bewusst außerhalb von `Assets`, damit Unity für den Import der gelieferten FBX-Dateien keine lokale Blender-Installation benötigt. Vorschauen unter `Logs` sind lokale Arbeitsbelege; sie ersetzen keine Unity-Aufnahmen und werden nicht als Git-übertragene Dateien vorausgesetzt.

## Achsen, Pivot und Einbau

Die dokumentierten Assetachsen sind **+X Breite, +Y oben, +Z in die Bucht**, Front **−Z**. Pivot: vorn mittig auf Bodenhöhe. Der Generator verwendet in Blender `(x, −z, y)` und exportiert FBX mit `-Z forward / Y up` bei Metermaßstab.

Alle drei Detailstufen besitzen dieselbe Envelope:

| Achse | Minimum | Maximum |
| --- | --- | --- |
| X | −1,15 m | +1,15 m |
| Y | 0 m | 3,22 m |
| Z | −0,029 m | 3,14 m |

Damit beträgt die gesamte Ausdehnung **2,30 × 3,22 × 3,169 m**. Der negative Z-Anteil ist der kleine Display-/Linsenvorstand. Vor den Kassetten ist ungefähr 1,1 m Servicefläche frei; die vorderste Griffkante liegt bei Modell-z1,114. Die freie achteckige Frontöffnung ist 1,98 m breit. Der Frame besitzt eine 0,15 m hohe Schwelle.

Runtime-Anker: speichenlokal `(u = ±3,5; y = 0,06; z = 28,10) m`, Drehung Achswinkel +180°. Der vollständige Einsatz liegt dann bei `|u| = 2,35–4,65 m`, y0,06–3,28 und Speichen-z24,96–28,129. Er passt damit in die lichte 2,6 × 3,8 m große, 4,5 m tiefe Bucht. Importachsen, Einpassung und begehbare Wartungsflächen sind im Unity-Player geprüft. Die Runtime erstellt offene MeshCollider für Frame/Housing und BoxCollider nur an installierten Kassetten; der Export enthält weiterhin keine Collider.

## Exportgruppen und Detailstufen

Die exportierten Wurzeln heißen `BatteryBay_LOD0`, `BatteryBay_LOD1` und `BatteryBay_LOD2`. Darunter liegen:

```text
BatteryBay_LOD0                 # entsprechend LOD1 / LOD2
├─ Frame
├─ Housing
├─ ActiveDisplay
├─ StatusLenses
└─ Rack
   ├─ Backplane
   ├─ Cassette01
   ├─ Cassette02
   ├─ Cassette03
   ├─ Cassette04
   ├─ CoolingLoop
   └─ CoveredBusbar
```

`Frame` und `Housing` bleiben dauerhaft sichtbar. `Rack` umfasst die gesamte installierte Ausstattung und wird bei den sechs leeren Buchten ausgeblendet. `ActiveDisplay` und `StatusLenses` erhalten Zustandsfarben über die Runtime. Die FBX-Modelle enthalten keine Ladestand-/kWh-Texte; das Spiel setzt seine eigenen Beschriftungen. Zur Vermeidung doppelter Objektnamen sind die Kinder der nicht sichtbaren LOD1-/LOD2-Quellen in der nativen `.blend` nach dem Export mit `SourceLOD1_` beziehungsweise `SourceLOD2_` versehen; die FBX-Gruppen behalten die obigen Namen.

| Detailstufe | Dreiecke, vollständig bestückt | Vertices | Meshobjekte |
| --- | ---: | ---: | ---: |
| LOD0 | 19.841 | 11.985 | 11 |
| LOD1 | 12.264 | 6.672 | 11 |
| LOD2 | 2.612 | 1.640 | 11 |

Runtime: `StationBatteryBay.cs` lädt alle drei Stufen, richtet einen `LODGroup` mit Schwellen 0,22 / 0,07 / 0,004 ein und schaltet Zustandsanzeigen über `MaterialPropertyBlock`. Leere Buchten enthalten durch das ausgeblendete Rack weniger sichtbare Geometrie als die Tabelle. Aktuell besitzt das Paar **0° direkt gegenüber dem Zentraleneingang** vier Kassetten je Bucht: **Index0 aktiv, Index1 ausgeschaltet/getrennt**, festgelegt in `StationBatteryPlacement.cs`. Die frühere Platzierung bei180°/Index4–5 ist ersetzt. Die Zustandsanzeige repräsentiert den bereits vorhandenen gemeinsamen 24-kWh-Speicher und erzeugt keinen weiteren Energiespeicher.

Die FBX-Dateien enthalten **keine Collider**. Die Runtime verwendet die echte Frame-/Housing-Meshform für die offene Front und einzelne Collider für installierte Rackgruppen. Ein Collider über die gesamte Modell-AABB würde den Servicezugang unsichtbar verschließen und darf dafür nicht verwendet werden. Die stationäre Druckhülle liefert zusätzlich ihre eigenen Collider.

## PBR und Materialzuordnung

Gemeinsamer Atlas: 2048 × 2048 Pixel, deterministisch prozedural erzeugt, Seed `251025`. Drei Materialien: `BatteryBay_Surface`, `BatteryBay_Display`, `BatteryBay_Status`.

- `BatteryBay_Albedo.png`: sRGB, RGB-Farbe, Alpha1.
- `BatteryBay_Normal.png`: linear, Tangentennormalen mit +Y-Konvention.
- `BatteryBay_MetalSmooth.png`: linear; Rot = Metallanteil, Alpha = Glätte für Unity Standard.
- `BatteryBay_MetalRough.png`: linear; Rot = Metallanteil, Grün = Rauheit, Blau = 1; mitgelieferte Alternative, nicht zusätzlich zur MetalSmooth-Map anzuschließen.

Der Generator erzeugt die Material- und Import-Metadaten mit stabilen GUIDs und Materialzuordnungen. Bereits bestehende GUIDs werden erhalten. Die Runtime ordnet Surface/Display/Status explizit den gelieferten Materialien zu. Vorschauleuchten und Vorschauboden werden nicht in die FBX-Dateien exportiert.

## Reproduktion

Mit Blender 4.5.14 LTS aus dem Projektwurzelverzeichnis, hier mit dem lokal vorhandenen portablen Blender:

```powershell
& '.\work\HallExperiment\blender-4.5.14-windows-x64\blender.exe' --background --python-exit-code 1 --python '.\tools\CreateBatteryBay.py'
```

Das erzeugt native Quelle, drei FBX-Dateien, Texturen, Materialien, `.meta`-Dateien, Bericht und vier lokale Vorschauen. Für einen reinen Neu-Export ohne Renderzeit:

```powershell
& '.\work\HallExperiment\blender-4.5.14-windows-x64\blender.exe' --background --python-exit-code 1 --python '.\tools\CreateBatteryBay.py' -- --skip-render
```

Nur Vorschauen aus der vorhandenen `.blend` rendern:

```powershell
& '.\work\HallExperiment\blender-4.5.14-windows-x64\blender.exe' --background --python-exit-code 1 --python '.\tools\CreateBatteryBay.py' -- --render-only
```

Der lokale Blenderpfad ist kein über Git installierter Projektbestandteil. Auf einem anderen Rechner den Pfad zu einer passenden Blenderinstallation einsetzen. Nach einem beabsichtigten Neu-Export Bericht, FBX, Texturen, Materialien und Metadaten gemeinsam prüfen; manuelle Modelländerungen werden beim vollständigen Generieren überschrieben.
