# Prozedurale Asteroiden

Die Stiltafeln liegen in `docs/Art/Asteroiden`. Das erste umgesetzte System erstellt geschlossene, unregelmäßige Meshes und mischt Kruste, Freilegungen und Akzente mit einem gemeinsamen PBR-Shader für die Built-in Render Pipeline.

## Fertige Bibliothek

Unter `Assets/SpaceMiner/Asteroids` liegen nach **Space Miner → Prototyp einrichten**:

- `Types`: Eisreich, Felsig und Metallreich als editierbare `AsteroidType`-Assets.
- `Materials`: drei Oberflächenmaterialien, jeweils mit drei konfigurierbaren Schichten.
- `Textures/RegolithDetail.asset`: nahtlose lineare Detailtextur mit Mipmaps; R/G speichern Oberflächenneigungen, B die Höhe.
- `Meshes`: dauerhaft gespeicherte Meshes für das Testfeld und die Beispiel-Prefabs.
- `Prefabs`: neun Beispiele, drei Typen × 100 m, 1 km und 5 km.
- `AsteroidCatalog.asset`: erweiterbare Liste von Typen mit Gewichten für reproduzierbare Zufallsauswahl.

Die ursprüngliche Zwölf-Körper-Szene erhielt den Generator; der aktuelle aktive Modus enthält 100 Körper. Beim Einrichten bleiben vorhandene Positionen erhalten. Schon konfigurierte Generatoren werden beim Einrichten mit ihren aktuellen Szenenparametern neu gebacken. Bestehende Beispiel-Prefabs werden erhalten.

Neue Einträge in `BeltSettings.Asteroids` werden beim Einrichten zusätzlich erzeugt. Ihre Position, Größe, Typ und Seed stammen aus dem Eintrag. Fehlender Typ und Seed null erhalten Startwerte. Vorhandene Szenenobjekte werden erhalten; das Entfernen eines Listeneintrags löscht daher keine bereits bearbeiteten Objekte. Die Beispielbibliothek lässt sich über **Space Miner → Asteroiden → Vorhandene Beispiel-Prefabs neu backen** nach Profiländerungen aktualisieren.

## Einen Asteroiden erstellen

1. **GameObject → Space Miner → Prozeduraler Asteroid** wählen. Ein im Project-Fenster ausgewähltes Typasset wird übernommen.
2. Im `AsteroidGenerator` Typ, Seed und `Diameter Meters` einstellen.
3. **Meshes und Oberfläche neu backen** drücken und die Szene speichern.

Ein Seed erzeugt bei unverändertem Profil dieselbe Form und dieselben Materialflächen. Die größte lokale Achsenausdehnung des detailliertesten Meshes wird auf einen Meter normiert; die Objektgröße skaliert sie auf die gewünschten Meter. Rotierte Welt-Bounding-Boxen können größere Achsenausdehnungen besitzen. Oberflächendetails verwenden einen gesonderten Maßstab in Metern.

Die erzeugten Meshes sind echte Unity-Meshassets. Fertige Prefabs benötigen beim Laden keine neue Geometrieberechnung. Prozedurale Materialmasken werden beim Laden aus dem Profil auf die Renderer angewendet. Wer ein Profil geometrisch ändert, muss die betroffenen Meshes neu backen; reine Oberflächenfarben und Materialwerte brauchen keine neue Geometrie. Seed-, Typ- und Auflösungswechsel werden auch zur Laufzeit erkannt.

## Weitere Arten hinzufügen

Am einfachsten ein bestehendes Typasset duplizieren und umbenennen. Ein eigenes Material zuweisen oder das Materialfeld leeren; beim ersten Backen entsteht ein eigenes Material. So bleiben andere Profile unabhängig. Alternativ **Create → Space Miner → Asteroiden → Neuer Typ** verwenden.

Im Profil einstellen:

- **Form:** Achsenverhältnisse, deren Variation, gelappte Formen, Asymmetrie, großflächige Unregelmäßigkeit, kleinere Rücken, gestufte Klippen, feiner geometrischer Schutt, Bruchflächen und Einschlagsmulden.
- **Oberfläche:** Farbe, Metallic und Smoothness für Kruste, Freilegungen und Akzente; Anzahl, Radien und Vertiefung der Freilegungen.
- **Details:** Kontrast, Detailgröße in Metern, Stärke des feinen Reliefs und Gewichtung geometrischer Facetten in der Beleuchtung.
- **Wasseranteil:** ein Ressourcen-Entwurfswert für das bestehende Szenario; sichtbare Eisfläche und innerer Wasseranteil sind getrennte Größen.

Ein Körper kann über `Water Fraction Override` einen eigenen Ressourcenanteil erhalten; -1 übernimmt den Typwert. Die bestehenden Startquellen behalten 80 %, 65 % und 80 % Wasser. Das ist vom Flächenanteil der sichtbaren Eisfenster unabhängig.

Das neue Asset in die `Types`-Liste eines `AsteroidCatalog` aufnehmen und sein Gewicht festlegen. Gewicht null deaktiviert die Zufallsauswahl. Neue Typen benötigen keinen Enum-Eintrag und keinen neuen Generatorzweig. Bestehende Asteroiden behalten ihre bereits zugewiesenen Typen; für einen Wechsel das neue Profil ausdrücklich im Generator auswählen und neu backen. Im Code kann `catalog.Choose(seed)` für neue Felder verwendet werden.

Die aktuelle Materialbasis hat drei gemischte Schichten pro Profil. Die Anzahl der Profile ist offen. Eine spätere vierte Materialschicht oder eine neue Formfamilie mit Höhlen würde den Shader beziehungsweise den Formalgorithmus erweitern.

## Laufzeitverwendung

```csharp
// Ein inaktives Objekt verhindert eine vorzeitige Awake-Generierung bei der Konfiguration.
var body = new GameObject("Asteroid");
body.SetActive(false);
var generator = body.AddComponent<AsteroidGenerator>();
generator.Type = catalog.Choose(seed);
generator.Seed = seed;
generator.DiameterMeters = 700f;
body.transform.position = positionMeters;
body.SetActive(true); // Awake erstellt alle LODs und den MeshCollider.
```

`Generate(false)` erzwingt neue Laufzeitmeshes; wiederholte Generierung verwendet die vorhandenen LOD-Kinder und entsorgt nur eigene Laufzeitmeshes. Gebackene Meshassets werden nicht gelöscht. Alle Unity-Meshoperationen erfolgen auf dem Hauptthread. Große Felder sollten gebackene Varianten wiederverwenden oder das Erzeugen über mehrere Frames verteilen.

## Detailstufen und Physik

Bei der Standardauflösung 5 entstehen:

| Stufe | Dreiecke | Wechsel bei Bildschirmhöhenanteil |
| --- | ---: | ---: |
| LOD 0 | 20.480 | 30 % |
| LOD 1 | 5.120 | 12 % |
| LOD 2 | 1.280 | 3,5 % |
| LOD 3 | 320 | bis unterhalb praktischer Sichtbarkeit |

Alle Stufen werten dasselbe Formfeld und dieselben Materialregionen aus und verwenden dieselbe Normalisierung. Große Formmerkmale und Freilegungen bleiben dadurch örtlich konsistent. Umschaltungen sind zunächst hart; geometrisches Detail kann dabei sichtbar wechseln. Die Physik verwendet LOD 2 als statischen, nichtkonvexen MeshCollider für Auswahl und Oberflächenanflug. Bewegliche Asteroiden mit Rigidbody benötigen später ein gesondertes Kollisionskonzept.

Das feine Relief wird triplanar projiziert und besitzt keine klassische UV-Naht. Es ist eine wiederverwendbare generierte Detailbasis, noch kein individuelles Scan-Texturset. Die Oberfläche der Eiskörper bleibt überwiegend dunkle Kruste; helle Flächen liegen lokal an Vertiefungen. Metall erscheint nur in freigelegten Regionen deutlich metallisch, Staub bleibt rau.

Eine zweite Projektion liefert größere Oberflächenunregelmäßigkeiten relativ zum Körper. Farben im Profil werden als sRGB-Farben bearbeitet und für die lineare Beleuchtung umgerechnet. Sonnenschatten sind aktiv; ihre Reichweite passt sich dem Kameraabstand an. Der Shader benötigt Shader Model 3.5; der Windows-Build wurde auf dem vorhandenen Direct3D-System geprüft.

## Prüfen

```powershell
.\tools\Unity.ps1 Setup
.\tools\Unity.ps1 AsteroidCheck
.\tools\Unity.ps1 Build
.\tools\Unity.ps1 Check
```

`AsteroidCheck` prüft geschlossene Topologie, nichtdegenerierte Dreiecke, Größen, Seed-Reproduzierbarkeit, LOD-Konsistenz, begrenzte Eisflächen, einen neuen unregistrierten Typ, wiederholte Generierung und Collider-Raycast. Dabei werden auch die Beispiel-Prefabs aktualisiert. Der Spieltest ergänzt die bestehenden Kamera-, Intro- und Wasserprüfungen um Generator-, Material- und Colliderprüfungen und speichert Ansichten der drei Materialfamilien in `Logs/asteroid-*.png`. Diese Ansichten werden ohne HUD separat gerendert, damit auch ein verborgenes Testfenster gültige Bilder liefert; ein praktisch leeres Rendering führt zum Testfehler.

Die Konzepte geben die gemeinsame Richtung vor. Das ist die erste prozedurale Umsetzung; Nahdetails, Materialvariation und Fernlesbarkeit können anhand der echten Spielansichten weiter abgestimmt werden.

## Scan-Startregion und spätere Wolken-Seeds (08.10.2026)

Das aktive 100er-Szenario erhält zur Laufzeit zehn Startkontakte innerhalb des festen Scanner-Radius von 10 km. Zusätzliche Kontakte im Radius werden reproduzierbar ausserhalb platziert; 400 m Kugelabstand und ursprüngliche Oktanten bleiben erhalten. Assets/Szenendaten werden dadurch nicht umgeschrieben. ScannerProgression.ArrangeIntroCloud verwendet dafür intern Seed 7301. Die expliziten Spiral-/Millionen-Stresstests bleiben eigene Geometrien.

Vorgemerkte Nutzeridee: Später eine frei wählbare Seed für die gesamte Wolke mit massiv mehr Asteroiden. Das ist getrennt von den bereits vorhandenen Form-Seeds einzelner Körper. Parameter, Generatorversion, Startregion und Seed im Spielstand bleiben auszuarbeiten; siehe Ideenbacklog und Scanner-und-Spielstaende.
