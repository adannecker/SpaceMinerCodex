# Drohnenleitstand: Umsetzung vom 25.10.2026

**Queststart ohne unsichtbare Dialogsperre — 25.10.2026:** Nach erfolgreichem Auftrag schließt sich der Leitstand jetzt automatisch und Mira erscheint. **Verstanden** anklicken, dann mit **V** außen beobachten und bei Bedarf **100×** wählen. Der Wasserauftrag arbeitet weiter; ein erneuter Start derselben beschäftigten Drohne wird abgewiesen. Der Nutzer meldet einen Stillstand bei „Eine Ladung Roh-Eis starten“. Ursache war eine unsichtbare modale Sperre: Der Auftrag öffnete Mira, das weiter offene Drohnenpult unterdrückte seine eigene Darstellung, und die zusammengefasste Eingabesperre verhinderte zugleich Miras Darstellung. Der bisherige API-Test hatte den Dialog sofort automatisch bestätigt und diesen echten UI-Übergang übersehen. `StationOperations.DroneCommands` schließt nach erfolgreichem dialogöffnendem Auftrag das Pult, `.DroneConsole` beendet den laufenden IMGUI-Durchlauf sauber. `ScannerProgression.DialogueCanRender` unterscheidet Stationspult und dessen Schließframe von fremden sichtbaren Menüs; auch eine Liefermeldung während eines offenen Pults bleibt darstellbar. Die rein visuelle Handkamera setzt `eventMask=0`; sie empfängt keine unnötigen Unity-Mausereignisse. Die neue Regression startet über den validierten Befehlsweg und hält Mira über mehrere echte Repaint-Frames unbestätigt. Erst danach bestätigt sie über die Dialog-API und prüft Eingabefreigabe sowie den natürlichen Wasserumlauf. Zusätzlich Fenster-/Vollbild-Kamerastrahlen, Hand, taktische Übersicht und UI geprüft. Befehls-/Dialog-API und Framebuffer sind keine native Schaltflächen-, OS-Maus-/Tastatur- oder manuelle Komfortabnahme. [Nachweis](Pruefungen/Queststart-Mira-2026-10-25.json). Vier gezielte grafische Prüfläufe derselben korrigierten Development-Binärdatei bestanden: 551 Assertions / Exit 0, 32 gesicherte Bilder. 280 Quellen-/Metadateien und 247 ausgelieferte Dateien SHA256-abgeglichen; drei Nutzerraumpläne unverändert. Quelle Unity 6000.4.7f1 unverändert, getrennte Buildkopie 6000.6.4f1. Frühere Gesamtsuiten wurden nicht erneut ausgeführt und gelten für ihre damaligen Builds. Kein Commit/Push.

**Drohnenleitstand, taktische Übersicht und Arbeitshandschuh — 25.10.2026:** Implementiert und in `drone-console-build-05` geprüft. 12 Prüfläufe bestanden: 5 betroffene Prüfungen auf der finalen Development-Binärdatei wiederholt; 7 unveränderte Grundprüfungen aus `drone-console-build-04` nach Quellvergleich übernommen (4686 Assertions / Exit 0, 81 gesicherte Bilder). 280 Quellen-/Metadateien und 247 ausgelieferte Dateien SHA256-abgeglichen. Alle drei gespeicherten Nutzerraumpläne unverändert. Quelle Unity 6000.4.7f1, getrennte Buildkopie 6000.6.4f1, Innenlayout 14 und Raumplan-Schema 1 erhalten. Automatisierte Controller-/Framebufferprüfung; keine neue manuelle Komfort-, Headset-, Release-, 100k-Render- oder FPS-Abnahme. Kein Commit/Push. Der natürliche erste Wasserquest wurde ohne zusätzliche Strom-/Wasservorräte nach rund 4.87 Spielstunden abgeschlossen. [Vollständiger Nachweis](Pruefungen/Drohnenleitstand-VR-Hand-2026-10-25.json). Die folgenden Abschnitte beschreiben die tatsächlich vorhandenen Befehle und die weiterhin offenen Erweiterungen.

Die freigegebene Oberfläche verwendet die bestehenden Drohnen, Ressourcen, Werkstattfahrten und Arbeitsaufträge. Sie führt keine zweite Flotte, kein zweites Werkzeuglager und keine zusätzliche Wasserwarteschlange ein. Der Einstieg erfolgt am Drohnenpult in der Kommandozentrale oder über dessen Verknüpfung am Scannerpult. E beziehungsweise Escape schließt das Pult. Die virtuelle Außenansicht dient der Übersicht; Befehle sind am geöffneten Innenpult möglich.

## Bedienung

- Oben stehen Stationsenergie, aktuelle Erzeugung/Last, Flüssigwasserreserve und die vorhandenen Werkstattplätze. Innen/Außen wechselt den Arbeitsbereich und behält die zuletzt ausgewählte externe ID.
- Links stehen die tatsächlichen zehn Außendrohnen oder R-01. Die anfangs zusätzlich defekten Einheiten sind ausklappbar. Nur 01 besitzt anfangs eine Einsatzfreigabe.
- Die Mitte besitzt Übersicht, Ausrüstung und Diagnose. Angezeigt werden reale Akku-/Treibwassermengen, Rohmasse, physische Boxfüllung, Geschwindigkeit, Zielentfernung und die geschätzte aktuelle Phasen-/Arbeitsrestzeit. Werkstattwege und Wartezeit sind nicht in der reinen Servicezeit enthalten. Eine Ladezeitschätzung verwendet die aktuelle Stromzuteilung, keinen angenommenen freien Ladestrom.
- Rechts stehen die tatsächlichen Aufträge der ausgewählten Einheit. Wartende Aufträge lassen sich an der Überschrift ziehen oder mit Auf/Ab priorisieren. Laufende Arbeit wird nicht verschoben. Abbrechen nutzt den bestehenden sicheren Abschluss und die vorhandene Materialrückgabe.
- Abgeschlossene Historieneinträge können entfernt werden, sobald keine Materialreservierung mehr besteht. Das gibt Platz innerhalb des vorhandenen 80-Einträge-Limits frei, ohne Arbeitsergebnisse erneut anzuwenden oder Verbrauch zu erstatten. Wartende und laufende Arbeit lässt sich so nicht überspringen.

Der Nahrungs-Meilenstein wird unabhängig von der sichtbaren Historie erhalten (`foodProduced`). Ein vorhandener erfolgreicher Nahrungsauftrag aus einem alten Spielstand wird beim Laden beziehungsweise vor dem Entfernen als Nachweis übernommen. Sein Rezept wird dabei nicht erneut ausgeführt.

- Unter der Auftragsliste werden Tätigkeit und ein fachlich passendes bekanntes Ziel gewählt. Der Zielfilter durchsucht bekannte Ziele. Unbekannte Wasserquellen, geborgene/zugewiesene Wracks und Ziele aus dem falschen Drohnenbereich werden nicht als neue Ziele angeboten.
- Während der Zielfilter Texteingaben erhält, dient E als Buchstabe; Escape schließt das Pult weiterhin. Auswahlwechsel und Schließen setzen den Filterfokus zurück.
- Bei weniger als 1300 realen Fensterpixeln wechseln Details und Aufträge über eigene Reiter; die globale UI-Skalierung wird dabei nicht verdoppelt. Die drei Bereiche besitzen unabhängige Scrollflächen; vergrößerte Schrift bleibt bedienbar.

## Erste Wasseraufgabe

1. Am Scannerpult mit **E** interagieren, die etwa zehn Sekunden Bereitschaft abwarten und **Nahbereichsscan ausführen** wählen.
2. **Drohnenleitstand öffnen**, **Extern → 01** wählen; bei schmalem Fenster zunächst auf **AUFTRÄGE** wechseln.
3. **Eis / Wasser**, eine bestätigte Quelle wie **A-01** und **Eine Ladung Roh-Eis starten** wählen.
4. Nach erfolgreichem Start schließt sich das Pult automatisch. Miras sichtbare Meldung mit **Verstanden** bestätigen. Bei später erneut geöffnetem Pult gilt weiterhin die eingestellte Menüpause.
5. Mit **V** außen beobachten. Falls die Uhr **PAUSE** zeigt, mit Leertaste oder **1×** fortsetzen; **100×** beschleunigt den Versuch.

Dieser Befehl startet genau einen vorhandenen, physikalisch budgetierten `DroneAgent`-Auftrag. Die Quellen-ID ist `AsteroidResource.name`, identisch zur gespeicherten Asteroiden-ID. Für diese Wasseraufgabe müssen weder R-01 noch zusätzliche Anlagen eingeschaltet werden; die vorhandenen Anlagen verarbeiten die Lieferung bei ausreichendem Stationsstrom automatisch.

Bohrer, freie Drohne, vorhandene Restfracht, reale Annahmekapazität und Hin-/Rückflugreserve werden durch den bestehenden Backendpfad geprüft. Die Rohlieferung geht in die geschlossene Eisannahme; erst die mit Strom bezahlte Aufbereitung erzeugt sauberes Wasser. Die Aufgabe bleibt **erste Lieferung und 40 L Flüssigwasser**, kein voll gefülltes Kaltlager. Die Oberfläche vergibt weder Wasser noch Strom beim Auftragserteilen.

## Werkzeuge und R-01

Bohrer, Greifer und Laser benutzen den bestehenden Werkzeugbestand und einen echten `Refit`-Arbeitsauftrag. Das montierte Werkzeug bleibt bis zum erfolgreichen Abschluss unverändert. Laser setzt die erforschte Reparaturrolle voraus. Bereits vorgemerkte Umbauten werden angezeigt; inkompatible oder doppelte Befehle werden mit Grund abgewiesen.

Die interne R-01 besitzt im bestehenden Prototyp keinen eigenen Akku, Treibwassertank, gespeicherten Systemzustand oder modularen Werkzeugvorrat. Die Ansicht zeigt deshalb Stationsversorgung und Bestandsfähigkeiten. Weitere interne Werkzeugmodule sind als späterer Ausbau gekennzeichnet, ohne vorgetäuschte Wechselaktion. R-01 kann die vorhandenen internen Rezepte, Anlagenwartung und Außendrohnenumbauten übernehmen. Gemeinsame Umbauaufträge erscheinen sowohl beim betroffenen Außenfahrzeug als auch bei der internen Servicekraft, bleiben aber ein einziger Backendauftrag.

## Drohne 02

Die Voraussetzungskarte bildet den vorhandenen Ablauf ab: Versorgungsdiagnose, R-01 in Betrieb, mindestens 40 L Wasser und 6 kWh Stationsreserve sowie zwei Ersatzteile; anschließend der vorhandene 300-s-Instandsetzungsauftrag bei 0,5 kW. Nach seiner Fertigstellung wird 02 tatsächlich am Dock geladen und betankt. Der Leitstand gewährt keine kostenlose Freischaltung oder Füllung.

Der im Konzept beschriebene zusätzliche Notversorgungsflug **vor** der Reparatur bleibt ein eigener späterer Ablauf. Einzelsystem-Schadensprozente und Wartungshistorie werden noch nicht separat simuliert und nicht aus UI-Beispieldaten ergänzt. 03 bis 10 besitzen noch keinen freigegebenen Instandsetzungsablauf.

## Architektur und Prüfung

- `DroneFleetReadModel.cs`: flüchtige Lesedaten sowie auflösungsabhängige Bereichsanordnung; keine Persistenz oder eigenen Inventare.
- `StationOperations.DroneCommands.cs`: stabile Auswahl, reine Abfragen, passende Ziele und schmale validierte Befehle an die bestehenden Operations. Alle mutierenden öffentlichen Befehle prüfen `DroneConsoleCanCommand`.
- `StationOperations.DroneConsole.cs`: Vollbilddarstellung, Detailreiter, Ziel-/Tätigkeitswahl und Priorisierung.
- `StationDroneConsoleCheck.cs`: gezielte Development-Prüfung mit `-stationDroneConsoleCheck`, Grafik aktiv, ohne `-batchmode`. Ergebnisordner `Logs/StationDroneConsole`.

Die neue Prüfung soll den echten Weg vom Scanner über den neuen Befehlsweg bis `waterSecured` ohne Wasser-/Energie-Fixtures durchlaufen. Erst danach werden unabhängige Queue-, Umbau-, Abbruch- und Befehlsgrenztests ausgeführt und über den normalen Save-Loader zurückgesetzt. Sie enthält Screenshots der sechs Ansichten, der ersten Quest in der Außenübersicht und der vergrößerten Schrift bei 1280 × 720. Eine manuelle Maus-/Tastaturabnahme wird dadurch nicht behauptet.

**Historischer Stand vor der Buildabnahme:** Quellen waren implementiert und lesend geprüft. Maßgeblich für tatsächlich erfolgte Laufzeitprüfungen ist der aktuelle Nachweis am Anfang dieses Dokuments.
