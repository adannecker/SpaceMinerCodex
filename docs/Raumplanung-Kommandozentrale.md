# Raumplanung der Kommandozentrale — 25.10.2026

**Strompult, 25.10.2026:** Die bisherige Akku-Box ist jetzt das zusätzliche **STROMVERSORGUNG**-Terminal. Der Plan behält acht Elemente sowie die stabile Kennung `battery`; gespeicherte Position, Größe und Drehung bleiben erhalten. Die echten Akkus stehen im Ring gegenüber der Zentrale. Bedienung, Telemetrie und aktueller Prüfstatus: [Stromversorgung](Stromversorgung-Steuerpult-2026-10-25.md). Die untenstehenden früheren Prüfzahlen gelten für ihre jeweiligen historischen Builds.

**Erweitert am25.10.2026:** F7 bietet inzwischen vier Raumreiter mit insgesamt31 bearbeitbaren Gruppen. Die folgenden acht Boxen bleiben die Kommandozentrale innerhalb dieses gemeinsamen Planers. Aktuelle Bedienung, getrennte Speicherdateien und Prüfstand: [Raumplanung der Station](Raumplanung-Station.md). Frühere Prüfzahlen dieser Datei betreffen ihre jeweiligen Vorgängerbuilds.

## Beschlossener Umfang

Die Kommandozentrale erhält zunächst einfache, einzeln beschriftete Boxen. Ihre Position und Größe sollen gemeinsam im Grundriss geplant werden, bevor wir die eigentlichen Geräte gestalten. Das große Steuerpult bleibt als Ausgangsanordnung mittig; die Nebenfunktionen liegen am Rand. Die zwölf freistehenden Sektorpulte aus dem Nutzerbild entfallen. Stromkreise, Wasserventile und Sektordaten bleiben erhalten und lassen sich über die bestehende Stationsplanung auswählen.

Der erste Planer bearbeitet genau diese acht vorhandenen Elemente:

| Stabile Kennung | Frontbeschriftung | Vorhandene Funktion |
| --- | --- | --- |
| `main-console` | STEUERPULT | Zentrales Stations-/Scannerpult |
| `drones` | DROHNEN | Drohnenverwaltung |
| `planning` | STATIONSPLANUNG | Stationszustand, Aufträge und Raumplanung |
| `research` | MIRA / TECHTREE | Forschung |
| `production` | FERTIGUNG | Produktionsaufträge und Lager |
| `printer` | MATERIALDRUCKER | Platzhalter für den Materialdrucker |
| `food` | NAHRUNGSSYNTHESE | Platzhalter für die Nahrungsanlage |
| `battery` | STROMVERSORGUNG | Zusätzliches Terminal für Akkuplätze, Ladestand, Reaktor und Solarfelder |

Jede Box besitzt einen eigenen festen Collider und ein Display an ihrer Vorderseite. Die nun fünf Nebenpulte und das zentrale Scannerpult behalten ihre Funktionen. Ihre Bedienpunkte folgen der Box beim Verschieben, Drehen und Ändern der Tiefe. Das Strompult öffnet sich beim E-Hinweis und zeigt das echte Stationsnetz; es enthält keinen zusätzlichen Akkuvorrat. Fertige Gerätegestaltung ist weiterhin offen.

## Bedienung

Im laufenden Spiel öffnet **F7** den Raumplaner. Alternativ gibt es am Stationsplanungspult den Eintrag **Raumplanung Kommandozentrale**. Startmenü, Intro und andere geöffnete Hauptmenüs müssen zuvor geschlossen sein.

| Aktion | Bedienung |
| --- | --- |
| Element auswählen | Box im Grundriss oder ihren Eintrag links anklicken |
| Position ändern | Linke Maustaste auf der Box halten und ziehen |
| Breite und Tiefe ändern | Eine gelbe Ecke der ausgewählten Box ziehen; die gegenüberliegende Ecke bleibt stehen |
| Genau eingeben | Rechts Position X/Z, Breite, Tiefe, Höhe und Drehung ändern, dann **Maße / Position anwenden** oder Enter |
| Frei drehen | Den grünen Ring **Drehen** der ausgewählten Box mit der linken Maustaste ziehen; Position und Größe bleiben fest |
| Winkel ausrichten | Beim Drehen **Shift** halten: an nahen Wänden oder der Ausrichtung benachbarter Boxen einrasten; sonst 15°-Raster |
| Winkel genau einstellen | Winkel eingeben oder die Schaltflächen für 15° beziehungsweise 90° verwenden; auch die Schaltflächen berücksichtigen Shift |
| Einrasten | Beim Ziehen **Shift** halten: 0,25-m-Raster, passende nahe Boxkanten beziehungsweise die tatsächliche Raumwand |
| Frei platzieren | Ohne Shift ziehen; gebrochene Meterwerte bleiben erhalten |
| Ansicht vergrößern | Mausrad über dem Grundriss |
| Grundriss verschieben | Mittlere oder rechte Maustaste halten und ziehen |
| Gesamtansicht | **Einpassen** |
| Rückgängig / wiederholen | Schaltflächen links oder Strg+Z / Strg+Y; Strg+Shift+Z wiederholt ebenfalls |
| Laufendes Ziehen abbrechen | Escape; stellt die Anordnung vor dem Ziehen wieder her |
| Planer schließen | F7, **Schließen** oder Escape ohne laufenden Ziehvorgang |

Alle Maße sind Meter. X verläuft seitlich, Z radial im Stationskoordinatensystem. Die Position bezeichnet die Mitte der Boxunterseite; sie bleibt auf dem Boden. Die Drehung erfolgt um die Hochachse. Eine helle Kante im Grundriss markiert die beschriftete Vorderseite.

Akzeptierte Änderungen erscheinen sofort an den echten Boxen im Raum. Ungültige Eingaben erhalten eine Erklärung; beim Ziehen kann die abgelehnte Position als rote Vorschau erscheinen. Die zuletzt gültige Boxposition bleibt erhalten. Ein kompletter Verschiebe-, Größen- oder Drehvorgang lässt sich als ein Schritt rückgängig machen. Escape während des Drehens stellt den vorherigen Winkel wieder her.

Die Winkelausrichtung verwendet die tatsächlichen schrägen Seitenwände und die lokale Tangente der gekrümmten Innen-/Außenwand. Benachbarte Boxen liefern ihre eigene Ausrichtung; parallele und rechtwinklige Kanten sind möglich. Referenzen müssen räumlich nahe sein: Wandabstand höchstens2,5m, Objektkanten höchstens3m. Innerhalb von8° werden gültige Kandidaten nach Winkel- und Abstandsnähe ausgewählt; andernfalls gilt das15°-Raster. Grüne Hilfslinien und die Statuszeile zeigen Winkel und Bezug. Entfernte Objekte beeinflussen die Drehung nicht. Die numerische Winkelangabe bleibt exakt und wird beim Anwenden nicht automatisch eingerastet.

## Raumgrenzen und sicheres Begehen

Der Grundriss entspricht dem gebogenen Kommandosegment von Radius 38 bis 54 m mit ±13,5° Winkelbereich. Die Prüfung berücksichtigt die gedrehten Boxen, die gekrümmte Innen-/Außenkante und die schrägen Seitenwände. Vorgesehen sind mindestens 25 cm Abstand zur Raumgrenze, mindestens 15 cm Abstand zwischen Boxen, Mindestmaße von 0,4 m und maximal 4,4 m Höhe. Der gekennzeichnete Eingang bleibt frei.

Der Mindestabstand von 15 cm ist **keine Garantie für einen begehbaren Gang**. Der Planer berechnet noch keine Laufwege, Arbeitsräume oder Erreichbarkeit sämtlicher Fronten. Neben einer Box kann Platz fehlen, obwohl sich ihre Grundflächen nicht überschneiden. In diesem Fall lässt sich F7 erneut öffnen und die Anordnung verbessern.

Die Simulation pausiert während der Raumplanung auch dann, wenn die normale Menüpause ausgeschaltet ist. Laufbewegung und die anderen Spieloberflächen sind gesperrt. Beim Schließen bleiben die bisherige Simulationsgeschwindigkeit, Blickrichtung und eine weiterhin freie Spielerposition erhalten. Falls eine Box über dem Spieler platziert wurde, wird er auf freien Boden versetzt. Auch ältere gespeicherte Innenpositionen dürfen nicht in einer inzwischen verschobenen Box enden. Der reguläre Einstieg liegt vor dem Steuerpult; fehlt dort echte Standfreiheit, dient der freigehaltene Eingang als Ersatz.

## Raumplan speichern und auf einen anderen Rechner mitnehmen

**Raumplan speichern** oder Strg+S schreibt den aktuellen Entwurf. **Gespeicherten Plan laden** übernimmt ihn wieder in die echten Boxen. **Startanordnung** stellt die acht Ausgangspositionen her; diese Änderung kann rückgängig gemacht werden und überschreibt die gespeicherte Datei erst nach erneutem Speichern.

Im Projekt liegt der gemeinsame Entwurf unter:

`Design/RoomPlans/command-room-layout.json`

Die Datei ist für Git vorgesehen und kann mit dem Projekt auf den anderen Rechner übertragen werden. Nach einem Pull verwendet der nächste Spielstart den dort gespeicherten Raumplan. Ein bereits laufendes Spiel übernimmt die Datei mit **Gespeicherten Plan laden**. Nicht gespeicherte Änderungen im laufenden Planer werden nicht durch Git übertragen.

Die JSON-Datei verwendet Schema 1 und die acht stabilen Kennungen aus der Tabelle. Pro Element enthält sie Position, Größe und Drehung. Beschriftungen bleiben den bekannten Elementen zugeordnet. Beim erneuten Speichern wird die vorherige gültige Datei als `command-room-layout.json.bak` erhalten; diese Sicherung und vorübergehende Schreibdateien bleiben lokal und werden von Git ausgeschlossen. Fehlerhafte oder unbekannte Pläne werden mit Hinweis abgelehnt, ohne die derzeitige gültige Anordnung teilweise zu ersetzen. Beim Programmstart bleibt bei ungültiger Datei die Startanordnung verfügbar.

Alte Pläne mit `battery` und der Beschriftung **AKKU / 24 kWh** bleiben gültig. Beim Laden übernimmt das Strompult exakt deren Maße und Pose. Die neue Beschriftung wird aus dem bekannten Katalog im Arbeitsspeicher eingesetzt; dadurch wird die Datei weder automatisch geändert noch als ungespeicherter räumlicher Entwurf markiert. Die Kampagnenkennung `battery` bezeichnet unabhängig davon weiterhin den echten Akku in der gegenüberliegenden Wandnische; R-01 repariert diese Anlage vor Ort.

Ein verteilter Spielbuild ohne erreichbares Quellprojekt verwendet stattdessen seinen Benutzerdatenordner `CommandRoomLayout`. Prüfläufe besitzen eigene Dateien unter `Logs/CommandRoomPlannerCheck`.

Der Entwurf ist von Kampagnen-Spielständen getrennt. Ein gewöhnlicher Spielstand speichert weiterhin Ressourcen, Forschung, Aufträge und Spielerzustand; er ersetzt den gemeinsamen Raumplan nicht. Für einen Rechnerwechsel deshalb den gespeicherten Raumplan und die gewünschten Spielstände getrennt berücksichtigen.

## Technischer Aufbau und Grenzen

- `CommandRoomLayout`: Datenkopien, vollständige Planvalidierung, Zuordnung zu den echten Boxen und atomisches Speichern/Laden.
- `CommandRoomPlanner`: Grundriss, Eingaben, Ziehen, Größenänderung, Einrasten und Rückgängig-Verlauf; verwendet dieselbe Datenprüfung wie das Laden.
- `StationOperations.RoomPlan`: Einfache Boxen und mitbewegte Bedienpunkte der vorhandenen Funktionen, einschließlich des Strompults mit erhaltener Plan-ID `battery`.
- `StationInteriorLayout` / `StationInteriorMode`: aktuell Layoutrevision 15, sichere Einstiegs-/Ladepositionen und Schutz vor einer über dem Spieler platzierten Box; die erste Planerumsetzung verwendete Revision 12.
- `CommandRoomPlannerCheck`: automatisierte Akzeptanzprüfung an den tatsächlichen Boxen, Bedienpunkten, Collider-Geometrien, Dateien und Oberflächen.

Bewegliche Planungsboxen sind vom statischen Zusammenfassen der Stationsgeometrie ausgenommen. Der übrige Stationsaufbau verwendet weiterhin seine bisherigen Optimierungen. Das Quellprojekt bleibt bei Unity 6000.4.7f1; lokale Development-Builds entstehen mit 6000.6.4f1 in einer getrennten Buildkopie.

Dies ist ein kleiner Raumplaner für die acht bestehenden Kommandofunktionen. Neue Objekte, beliebige Räume, Wand-/Türumbauten, ein vollständiges CAD-System, automatische Kabel-/Rohrführung und ein Laufwegplaner gehören noch nicht dazu. Die Boxgröße ist zunächst eine Entwurfsgröße und verändert keine Behälterkapazitäten, Herstellungsrezepte, Forschungsfreigaben oder sonstigen Kampagnenwerte.

## Prüfung

**Aktueller Drehgriff-Build `roomrotation-build-01`:694 Raumplaner- und28 UI-Assertions, beide Exit0.** Frei über359°/0° auf23,7° drehen, fixe Position/Größe, reale3D-Rotation, Undo/Redo und Abbruch geprüft. Shift trifft die beiden tatsächlichen schrägen Seitenwände bei103,5°/256,5°, eine Kreiswand-Tangente bei5° sowie die Ausrichtung einer nahen17°-Box. Eine entfernte Box beeinflusst die Ausrichtung nicht; eine ungültige Drehung wird ohne teilweise Änderung abgelehnt. Bestehende Speicher-/Lade-/Modal-/Bedienpunktprüfungen bleiben Bestandteil des Planerlaufs. UI prüft erneut echte native Vollbildumschaltung/Fensterrückkehr.

Elf neue Bilder gesichert, beide Grundrisse mit sichtbarem Drehgriff visuell geprüft:1600×1000 sowie1280×720 mit1,4-facher Schrift.240 Quell-/Meta-/LOD-/Shaderdateien und247 ausgelieferte Dateien SHA256-abgeglichen; gemeinsamer Entwurf unverändert. [Aktueller Bericht mit Bildern, Logs und Manifesten](Pruefungen/Raumplanung-Drehen-2026-10-25.json). `Builds/Windows`, Vorgänger `Windows-before-roomrotation-20261025` erhalten. Quelle6000.4.7f1, separate Buildkopie6000.6.4f1; Layout12 und Schema1 unverändert. Weitere Stations-/Werkstatt-/Aufbau-/Vitals-Läufe wurden für diese gezielte Eingabeänderung nicht wiederholt. Die folgende umfassendere Abnahme gilt für den Vorgänger.

Finaler Development-Build `roomplan-build-04`, Unity6000.6.4f1 in der separaten Buildkopie; Quellversion6000.4.7f1 unverändert. Derselbe Build besteht **545 Raumplaner-,97 Aufbau-,1506 Werkstatt-,156 Stations-,29 Spielerwerte- und28 UI-Prüfungen**, sämtliche Prozess-Exitcodes0. Die2361 Assertions enthalten wiederholte Verriegelungs- und einzelne Displayprüfungen und bezeichnen keine2361 verschiedenen Spielszenarien.

Die neue Abnahme prüft acht echte Boxen samt Größe, Pose, Collider und Frontdisplay; freie0,37-m-Verschiebung gegenüber Shift-Raster und Kantenmagneten; tatsächliche Zeigerpfade für Größenänderung, Drehung, numerische Eingaben, Undo/Redo und Abbruch. Hinzu kommen vollständige Ablehnung ungültiger Pläne, isoliertes Festplatten-Speichern/Laden und beschädigte Dateien, reale Menüpause trotz abgeschalteter normaler Menüpause, unveränderte freie Spieler-/Kameraposition und sichere Ersatzposition nach Überbauen. Ein besonderes gültiges Konsolenlayout hat vor seiner Front keinen Wandabstand: geprüft ist der freie Eingang als tatsächlicher Spawn-/Ladeersatz. Alle vier Nebenpulte bleiben über ihre Vorderseiten bedienbar; das Drohnenpult wurde dafür zusätzlich verschoben und in der Tiefe vergrößert. Auch der verschobene zentrale Scanner bleibt bedienbar.

Elf neue Bilder sind im [Prüfbericht samt Logs und SHA256-Manifesten](Pruefungen/Raumplanung-2026-10-25.json) gesichert. Grundrisse1600×1000 und1280×720 mit1,4-facher Schrift sowie die 3D-Boxenansicht wurden visuell kontrolliert; UI-Regression einschließlich echter nativer2560×1440-Vollbildumschaltung und Fensterrückkehr bestanden. Quellen/Metas/LOD/Shader stimmen mit der Buildkopie überein, die bereitgestellten Dateien mit dem geprüften Build. Auslieferung `Builds/Windows`, vorheriger Build `Windows-before-roomplan-20261025-1030` erhalten. Start weiterhin über `Start-Aufbauphase-01.cmd`.

Das ist eine programmatische Controller-Abnahme mit gerenderten Bildern, keine manuelle Maus-/Tastatur-Komfortabnahme. Kein neuer Release-/100k-/Langzeit-Balance- oder FPS-Test. Der bestehende Aufbaucheck erzeugt synchrone Renderdiagnostik; sie wird hier nicht als spielbare Bildrate bewertet. Kein Commit/Push.
