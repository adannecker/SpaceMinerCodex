# Gesicherter Entwicklungsstand vom 12.10.2026

Diese Übersicht beschreibt die gemeinsam entwickelten Änderungen seit dem letzten eingecheckten Stand `824ff8d`. Aktuelle Gestaltung steht in [Spielidee](Spielidee.md), Bedienung und Umsetzung in den verlinkten Fachdateien. Ältere Abnahmen bleiben als Verlauf erhalten und gelten jeweils für die darin bezeichnete Binärdatei.

## Ringstation und Bewegung

- Ringbau-Simulator mit übereinanderliegenden Ringen, produktspezifischen Außenlagern und innerer Trennung/Fertigung. Eigene Testressourcen und Baufortschritt; noch keine vollständige Kampagnenproduktion.
- Begehbarer Entwurf mit zwölf sichtbaren Segmentverbindungen,29/38m Innen-/Außenradius,9m Verkehrsbreite und16m Tiefe je radialer Ausbaustufe. Diskrete Aufzüge zwischen Innenbereichen; getrennte Modulhüllen und deutlichere Kupplungen außen/innen.
- Hauptspiel verwendet den gebauten Startumfang: Wohnen links der Kommandozentrale, Drohnendeck rechts, innere Werkstätten/Versorgung. Vorhandene Türen zur Größenprobe offen. Kommandozentrale mit Fenstern und erstem bedienbaren Pult; drei äußere Drohnenschleusen und zwei Dockreihen mit zusammen zehn Plätzen.
- Sprinten und HUD unten links für Stamina, Müdigkeit, Hunger, Durst und Gesundheit. Stamina verbraucht sich/erholt sich; die übrigen Werte sind gespeicherte Grundlagen. Gehen2,2m/s; Sprint10,4m/s in Editor/Development,5,2m/s in Release. Blicksteuerung und Vollbild auch im Rundgang. Spielstände erhalten Körperstatus und Stationslayout; ältere Positionen werden bei Bedarf auf einen sicheren Startpunkt migriert.

Dateien: `RingConstruction*`, `RingWalkthrough*`, `StationInteriorLayout.cs`, `StationInteriorMode.cs`, `StationVisual.cs`, `StationAirlock.cs`, `PlayerVitals*`, `SaveGameStore.cs`, `DisplayModeController.cs` und ihre Integration in Settings/Techtree. [Ringstation-Ausbau](Ringstation-Ausbau.md), [Spielerzustand/Speicherung](Scanner-und-Spielstaende.md), [Forschungslabor](Forschungslabor.md).

## Tripo-Modelle und Wartungsdrohne

Beide vom Nutzer gelieferten ZIP-Exporte geprüft und die FBX-Modelle samt ursprünglichen4K-Texturen, Unity-Materialien, Importmetadaten und Ressourcen-Prefabs übernommen. Die Spielprobe verwendet2K-Importe. ZIPs bleiben lokale Sicherungen; die verwendeten Quellassets sind vollständig versioniert. Importprüfung: `tools/ImportTripoModels.py`; Unity-Aufbereitung/Build: `TripoModelBuild`.

Die Wartungsdrohne startet aus der importierten Ladebucht, untersucht eine Außenwand, schweißt und kehrt zum Laden zurück. Frontwerkzeug neben Sensoren/Greifern: hohle, verschraubte Kassette mit passend abgenutztem Blaulack, Teleskoparm und detailliertem Laser-/Schweißkopf. Bewegte Teile verschwinden beim Einfahren im ausreichend großen Gehäuse. Die Reparatur ist ein Showcase; reguläre Kampagnenwirtschaft und persistente Variantenumbauten sind noch offen. [Wartungsdrohnen-Showcase](Wartungsdrohnen-Showcase.md), [Originalexporte](../Imports/Tripo/README.md).

## Aktuelle Mining-Drohne

Der gleiche importierte Rumpf trägt einen detaillierten Bohrer rechts und einen beweglichen Sammelarm links. Transparenter Dachbehälter1,32×1,64×0,90m mit öffnendem Oberdeckel; Box und seitliche Teleskop-Verankerungen sitzen über verschraubten Trägern auf tatsächlich ermittelten Hüllflächen. Blaue Anbauten besitzen eigene Lack-/Metall-/Normaltexturen. Originalrumpf bleibt unverändert.

Nur produktives Bohren erzeugt jeweils einen unregelmäßigen Brocken, der vom Greifer aufgenommen und abgelegt wird, bevor das nächste Stück entsteht. Zwölf Plätze in zwei Lagen füllen sich ausschließlich durch diese Ablage. Die Größenklassen werden an die tatsächliche Boxgeometrie angepasst; zusätzliche frei wegfliegende Bohrfragmente sind entfernt. Die echte Abbaulogik entnimmt12kg Eisgestein und liefert9,6L Wasser; sichtbare Stücke erzeugen keine zusätzliche Ressourcenmasse. Zwölfkg sind weiterhin vorläufige Testkapazität.

Am Lageranschluss fährt eine Faltenbalg-Manschette an die geschlossene Heckluke und zeigt den angesaugten Zustand. Erst nach Kontakt öffnet die bündige Rückwand auf maximal90% in zwei kurzen Seitenschienen. Dieselben Stücke werden einzeln durch den Lagerkanal entladen. Danach schließt die Luke, anschließend löst sich die Manschette. Ansaugung/Greifen sind kinematische Animationen. [Vollständiger Ablauf und Grenzen](Mining-Drohne-Tripo.md).

Optionale statische Wegplanung berücksichtigt Stations-/Asteroiden-/Dockcollider, geparkte Drohnen, Umwege, freie Ausrichtungspunkte und gerade Dockmanöver. Zusätzliche Pose-/Vertexprüfungen stoppen bei einer Blockade. Allgemeines dynamisches Ausweichen mehrerer fliegender Drohnen und vollständige physische Dreieckkollision sind noch offen. Flackerquellen durch überlagerte Ladeaufnahme, Glasflächen, LOD und Lacknormalen wurden reduziert. ESC gibt die Maus im Beenden-Menü frei.

## Starten und erneut bauen

| Einstieg | Zweck | Buildmethode |
| --- | --- | --- |
| `Start-Ringbau-Test.cmd` | Planung/Simulator | `SpaceMiner.Editor.RingSimulatorBuild.Build` |
| `Start-Ring-Rundgang.cmd` | Offener Größenentwurf | gleiche Simulator-Buildmethode |
| `Start-Startstation-Dev.cmd` / `Start-Startstation.cmd` | Startstation Development/Release | siehe Ringstation-Fachdokumentation |
| `Start-Tripo-Groessenprobe.cmd` | Drohne/Ladebucht gemeinsam | `SpaceMiner.Editor.TripoModelBuild.Build` |
| `Start-Wartungsdrohne-Showcase.cmd` | Frontlaser/Reparatur | `SpaceMiner.Editor.TripoModelBuild.BuildMaintenanceShowcase` |
| `Start-Mining-Drohne.cmd` | Vollständiger Eisabbau/Transport | `SpaceMiner.Editor.TripoModelBuild.BuildMiningShowcase` |

Lokale EXE-Builds liegen unter `Builds` und werden nicht mit Git übertragen. Auf einem anderen Rechner bei geschlossenem Editor neu bauen. Quellprojekt bleibtUnity6000.4.7f1; dokumentierte lokale Builds stammen aus der separaten6000.6.4f1-Kopie. Mining-Vorschau: rechte Maustaste drehen, Mausrad Abstand, F11 Vollbild, ESC Menü; rechte Sidebar mit Pause/Neustart und Detailansichten. Zeitraffung2× Abbau,20× Entladen,80× Flug/Manöver.

## Prüfung und Übertragung

Letzter abgeschlossener Build vor der Sicherung: Mining-Development mit69 Ablauf-/Geometrieprüfungen und195 Balanceprüfungen derselben Binärdatei, jeweils Exit0. Zwölf Stücke erzeugt/abgelegt/entladen,12kg→9,6L, exakte Dockrückkehr.23 Bilder;247 bereitgestellte Builddateien sowie vier Quellen samt.meta SHA256-gleich. [Prüfbericht und Bilder](Pruefungen/Mining-Einzelbrocken-und-Manschette-2026-10-12.json). Die früheren Wartungs-, Startstations-, Ring-, UI- und Körperstatusprüfungen stehen mit Datum und eigenem Buildbezug unter `docs/Pruefungen`; sie sind keine erneute Abnahme der letzten Mining-Binärdatei.

Der Sicherungsauftrag selbst ändert Dokumentation und Git-Verwaltung. JSON-Dateien, Unity-Metadaten, Quellhashes und vorgemerkte Änderungen werden vor dem Commit geprüft; kein zusätzlicher Spielbuild allein für diese Dokumentation. Dauerhaft gesicherte Prüfprotokolle unter `docs/Pruefungen` werden mit versioniert; gewöhnliche Laufzeitlogs, Builds, Caches und persönliche Einstellungen bleiben ausgenommen. Chatarchive haben ihren eigenen älteren Exportstand und werden durch diese Zusammenfassung nicht als aktuell ausgegeben.

`.gitattributes` erhält die gesicherten Rohprotokolle bytegetreu ohne Zeilenenden-Normalisierung. Unity-generierte.meta-, Material- und Prefab-Dateien dürfen die von Unity geschriebenen Leerzeichen an leeren YAML-Werten behalten; Code und Dokumentation unterliegen weiterhin der normalen Leerraumprüfung. Die ursprünglichen Importmetadaten und ihre geprüften Hashes werden dadurch nicht verändert.
