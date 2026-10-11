# Wartungsdrohne – erster Reparatur-Showcase

## Nutzerentscheidung und Umsetzung vom 11.10.2026

Der importierte Wartungs-/Transportdrohnenrumpf dient als gemeinsame Basis für Wartung und späteren Bergbau. Für den ersten Showcase bekommt er ein separat bewegliches Schweißwerkzeug, Manövrierdüsen und einen sichtbaren Reparaturauftrag an der Stationshülle. Ein austauschbarer Bohrerkopf zeigt die erste Mining-Variante. Es wurde kein neues Tripo-Modell generiert und kein Credit ausgegeben.

Aktuelle Nutzerkorrektur: Der Laser sitzt **vorne rechts neben Greifer und Sensoren**, statt an der gegenüberliegenden Rückseite. Die Vorderseite des importierten Modells entspricht im normierten Modellkoordinatensystem +Z. Der Werkzeugkopf zeigt ebenfalls nach +Z. Die Drohne steigt zunächst auf, dreht sich auf freier Höhe um180° und fliegt mit der Vorderseite zur Reparaturstelle. Nach der Arbeit setzt sie rückwärts auf die sichere Position über dem Dock zurück, dreht dort in die Parkausrichtung und senkt sich erst anschließend ab. Die Drehung erfolgt um den gemessenen unteren Modellmittelpunkt, nicht um den versetzten FBX-Ursprung. Werkzeug und Sensoren arbeiten damit zur gleichen Seite. [Prüfung der Frontkorrektur](Pruefungen/Wartungs-Frontlaser-2026-10-11.json).

## Start und Bedienung

`Start-Wartungsdrohne-Showcase.cmd` startet `Builds/MaintenanceShowcase/SpaceMiner.exe -maintenanceShowcase` in der normalen `AsteroidBelt`-Spielszene. Eigener Development-Build, keine Änderung an den bereitgestellten normalen Windows-/Release-Builds oder der bisherigen Tripo-Größenprobe. Die Kampagnensimulation pausiert; Einstellungen, Test-Saves und Cinematic-Freigaben verwenden eigene Showcasepfade/-schlüssel.

Die rechte Sidebar zeigt Flugphase, Akku, Nahtfortschritt und abgeschlossene Durchläufe. Dort lässt sich der Ablauf pausieren, im Dock neu starten oder zwischen Schweiß- und Bohrerkopf wechseln. Der Mining-Umbau bleibt als Werkzeugansicht im Dock; der Bohrer dreht sich unabhängig vom Rumpf. Zur Wartung zurückwechseln startet den Reparaturablauf erneut.

**Laser im Detail ansehen** schaltet zur nahen Werkzeugansicht mit weiterhin frei drehbarer Kamera und Zoom; **Gesamte Drohne zeigen** kehrt zur Übersicht zurück. Die Nahansicht folgt dem Werkzeug während des Fluges.

- Rechte Maustaste ziehen: Kamera drehen; Mausrad: Abstand.
- F8: Begehen mit vorhandener Spielbewegung; WASD/Maus/Shift, Tab gibt die Maus frei. F8 zurück zur Modellansicht.
- F11 / Alt+Enter: vorhandene Vollbildsteuerung.

## Ablauf und Maßstab

| Phase | Zeit | Darstellung |
| --- | ---: | --- |
| Laden | 3 s | Drohne steht in ihrer Tripo-Ladebucht, Showcaseakku füllt sich |
| Abheben | 3 s | Auf sichere Höhe steigen, dort zur Hülle wenden; Manövrierdüsen |
| Anflug | 4 s | Mit Greifer-/Sensorseite voraus zur Hülle; seitliches Werkzeug fährt aus |
| Untersuchung | 2 s | Dünner Sensorstrahl fährt über den Riss |
| Schweißen | 9 s | Kurzer Lichtbogen zur Naht, Kontaktlicht und Funken ohne Schwerkraft |
| Kontrolle | 2 s | Fertige Naht prüfen |
| Rückflug | 4 s | Werkzeug einziehen, rückwärts von der Hülle auf freie Dockposition |
| Landung | 3 s | Zuerst auf Höhe in Parkrichtung drehen, danach zur exakten anfänglichen Parkstellung absenken |

Ein Durchlauf dauert etwa 30 Sekunden und wiederholt sich. Dies ist eine Präsentationszeit, keine beschlossene Spielbalance. Eine neue Runde zeigt erneut einen kosmetischen Riss. Akku: +5 Prozentpunkte/s beim Laden, −0,32/s in den übrigen Phasen; keine Anbindung an Stationsenergie oder reale Drohnenversorgung.

Unveränderte gleichmäßige Modellskalierung: Drohne inklusive vorhandener Arme ca. 2 × 2,20 × 1,69 m; Bucht ca. 2,65 × 3,33 × 0,73 m. Das neue Anbauwerkzeug erweitert die äußere Hülle. Flugweg vor der Werkstatthalle bei 33,3°/58 m, zwischen zwei Schleusen. Die Reparaturposition wird per Raycast auf der echten Außenwand gefunden. Eine konservative Körperbox prüft Stationsgeometrie und hält den Ablauf bei Hindernissen an. Dies ist kein allgemeines Navigations-/Ausweichsystem und prüft noch nicht die exakte Werkzeuggeometrie gegen sämtliche Objekte.

## Technischer Umfang und Grenzen

Auf Nutzerwunsch wurde der zunächst einfache Zylinderkopf durch ein detailliertes eigenständiges Laser-/Schweißmodul ersetzt (`MaintenanceLaserVisual.cs`). Dunkelblaues Gehäuse mit gefasten Kanten und abnehmbaren Seitendeckeln, bearbeitete Metallringe, sieben Kühlrippen, geriffelter Fokusring, verjüngte Keramikdüse, eingelassene cyanfarbene Optik, Sechskantschrauben, Lüftungsschlitze, Kupfer-Kühlleitungen, flexible Stromrückführungen und amberfarbene Sicherheitsmarkierungen passen zur industriellen Drohne. Eigene Meshes ohne Primitive-Collider; PBR-Oberflächen mit feiner Bürstung, Mikro-Normalen und unterschiedlichen Metall-/Rauheitswerten. Der Lichtbogen beginnt an der tatsächlichen neuen Düsenöffnung. Keine neue Tripo-Generierung oder Credit-Ausgabe. Dies ist ein direkt im Projekt modelliertes Anbauteil; die Originaldrohne bleibt unverändert.

`MaintenanceDroneShowcase.cs` ergänzt den importierten Ein-Mesh-Rumpf um unabhängige prozedurale Teile: montierte Werkzeugkassette, Teleskoparm, Schweißkopf, austauschbarer Motor/Bohrer und Düsen. Die bereits im FBX modellierten Arme bleiben unbeweglich; das FBX wurde nicht in Einzelteile zerlegt. Keine Blender-Rig-/Retopologiearbeit. Neue Primitive erhalten keine bleibenden Kollisionskörper. Die rund 500.000 Dreiecke des Rumpfs sind weiterhin nicht optimiert.

### Werkzeugkassette und Einfahren (11.10.2026)

Die einfache gelbe Anschlussbox ist durch ein größeres, hohles Anbaugehäuse ersetzt. Blau-graue lackierte Panzerplatten, gefaste Metallkanten, ein verschraubter Wartungsdeckel, Lüftungsschlitze, kleine Sicherheitsmarken und eine Statusleuchte greifen den Stil der Drohnenhülle auf. Zwei Sattelplatten mit Schrauben, Isolierpads und tragenden Abstandshaltern befestigen das Modul an tatsächlich abgetasteten Stellen des Original-Meshs. Die Originaldrohne bleibt unverändert; keine neue Tripo-Generierung.

Gehäuse etwa0,36×0,38×0,96m, mit Rückdeckel und offener Frontführung statt eines geschlossenen Vollblocks. Fester Aktuator, hohle äußere Teleskophülse und polierter Innenkolben bewegen sich auf einer gemeinsamen Achse. Eingefahren verschwinden beide beweglichen Hülsen bis auf die kurze Werkzeugkupplung im Gehäuse; der Laserkopf bleibt außerhalb der Öffnung. Die seitliche Position wird aus einem konservativen Dreiecks-Hüllbereich des importierten Rumpfs berechnet, statt nur den Werkzeugmittelpunkt zu platzieren. Aktuell: Werkzeugachse rechts bei etwa0,755m und Höhe1,30m; echte Sattelkontakte rechts bei etwa0,464/0,467m. Der Montageort liegt über den unbeweglichen Greifern. Arbeitsposition entsprechend angeglichen.

Die Stationsprüfung reserviert zusätzlich zur Drohnenkörperbox den Raum des vollständig ausgefahrenen Werkzeugs beim gesamten Flug und Wenden. Das ist weiterhin eine konservative Showcaseprüfung, keine allgemeine Drohnen-Wegfindung oder genaue Dockschienen-Kollision. Die Detailkamera folgt der Drohnenausrichtung und zeigt auch im Parkzustand die montierte Seite mit dem gesamten Gehäuse. Der frühere144-Prüfungen-Lauf kontrollierte keinen vollständigen Laserkopf-Abstand zur Drohne; die vom Nutzer beobachtete Einfahrüberschneidung wird deshalb separat durch neue Prüfungen und eine Park-Nahaufnahme abgedeckt.

`TripoDockPreview.cs` übernimmt gemeinsamen Import, Plattformhöhenmessung, Kamera und F8-Spielbewegung. Im Wartungsmodus blendet die Außenansicht die normale Scannerbedienung aus; Begehen aktiviert sie wieder. Die normale Größenprobe bleibt über ihren bisherigen Start verfügbar. `PlayerSettings.cs` ergänzt den isolierten Wartungs-Einstellungspfad. `TripoModelBuild.BuildMaintenanceShowcase` baut den Showcase aus der vorhandenen Spielszene. Assets und ihre Metadaten bleiben erhalten.

Der sichtbare Hüllenriss und seine Reparatur sind kosmetisch. Keine Änderung an Kampagnenzustand, Reparaturkosten, XP, Forschungsbedingungen oder Spielständen. Die bestehenden `DroneAgent`-Bergbaudrohnen werden nicht ersetzt. Der Mining-Kopf demonstriert Umbau und Rotation; Asteroidenkontakt, Halteklammern, Materialtransport und Frachtübergabe mit diesem Rumpf folgen später. Echte Dockkontakte, Schienen-/Werkzeugfreiheit, Innenverkehr und allgemeine Wegfindung bleiben offen.

## Prüfung

Werkzeugkassette: finaler Development-Build und vollständiger1600×1000-Lauf mit168 Prüfungen/Exit0. Darin elf Einfahrpositionen mit Abstand der tatsächlich gerenderten Laserkopfvertices zum Originalhüllbereich und zur Gehäuseöffnung, Park-Grundfläche, Unterbringung der beweglichen Hülsen sowie123 Flug-/Wendeproben mit konservativem Körper- und Werkzeugraum. Neun Bilder gesichert; eingefahrenes Modul, ausgefahrener Arm und Schweißen visuell geprüft. [Aktueller Gehäuse-Prüfbericht](Pruefungen/Wartungsmodul-2026-10-11.json). Die folgenden Berichte beschreiben frühere Fassungen.

Frontkorrektur: Development-Build erfolgreich, vollständiger1600×1000-Prüflauf mit144 Prüfungen/Exit0. Darin123 Positions-/Ausrichtungsproben gegen Stationsgeometrie einschließlich Wendemanövern, tatsächliche Vorderseitenausrichtung zur Hülle, Front-/Seitenposition des Lasers, Ausrichtung der Düsenöffnung sowie Wiederherstellung von Parkposition und Parkdrehung. Acht Bilder gesichert, Dock/Schweißen/Werkzeugnahaufnahme visuell geprüft. [Aktueller Frontlaser-Prüfbericht](Pruefungen/Wartungs-Frontlaser-2026-10-11.json). Die folgenden ursprünglichen Prüfstände sind historisch.

Die spätere Laserüberarbeitung wird zusätzlich in einer Werkzeug-Nahaufnahme festgehalten. [Aktueller Laser-Prüfbericht](Pruefungen/Wartungs-Laser-2026-10-11.json) dokumentiert Build, Durchlauf und tatsächlich visuell geprüfte Ansichten. Das Werkzeug umfasst16.880 Dreiecke. Strahlkern und transparente Corona beginnen an der Düsenöffnung; Funken nutzen eine weich auslaufende Partikeltextur statt sichtbarer Rechtecke. Der folgende ursprüngliche Showcasebericht beschreibt den Stand vor dieser Überarbeitung.

Der Development-Prüfmodus `-maintenanceShowcaseCheck` durchläuft den tatsächlichen zeitgesteuerten Ablauf und erfasst Dock, Abheben, Untersuchung, Schweißen, fertige Naht, Rückkehr und Mining-Kopf. Er prüft isolierte Einstellungen, echte Außenwand, abgetastete konservative Flugkörperbox, Lichtbogenkontakt/-reichweite, Partikel, Pause ohne Rest-Effekte, vollständige Reparaturzeit, exakte Dockrückkehr, Aufladen und Werkzeugwechsel. Protokolle und Bilder liegen unter `Logs/MaintenanceShowcase`; dauerhaft übernommene Ergebnisse stehen in [Prüfbericht](Pruefungen/Wartungsdrohne-2026-10-11.json).

Prüfergebnisse gelten ausschließlich für die dort genannten Läufe. Quellprojekt bleibt Unity6000.4.7f1; lokale Builds verwenden die getrennte vorhandene6000.6.4f1-Kopie. Kein Release-Test, keine manuelle Eingabeabnahme und kein Performancebenchmark.
