# Begehbare Drohnenhalle — Materialexperiment vom 19.10.2026

Aktuell25.10.2026: [Neue Stationswerkstatt](Stationswerkstatt-2026-10-25.md). Originale Hallenmöbel wandbündig, neun Ersatzwerkzeug-Aufnahmen, wandseitige Schrottannahme, zwei neue Zellen mit vier Bodenrobotern. Nur Platz01 betriebsbereit; die echte Außendrohne besucht ihn durch die größere Schleuse. Aktuelle Hauptstation Layout11; das separate ursprüngliche Materialexperiment bleibt eigenständig. Frühere Angaben folgen.

Aktuell25.10.2026: [Rahmen-/Flottennacharbeit](Stationsrahmen-und-Drohnenflotte-2026-10-25.md). In der Hauptstation zeigen Boden, Wand und Decke der ausgearbeiteten Halle wieder ihre ursprünglichen Innenpaneele; zusätzliche Wabenlagen dort nur außen. Dazwischen sichtbare Stützprofile und Randabschlüsse. Alle zehn Außenplätze jetzt mit ausgearbeitetem Tripo-Modell und importiertem Ladedock, ausschließlich01 aktiv. Die isolierte Hallenexperiment-EXE wurde weiterhin nicht neu gebaut.

Aktualisierung25.10.2026 der Kampagnenintegration: [Keilstation mit Außenschleuse](Station-Keilmodule-Umsetzung-2026-10-25.md). Zur Bay jetzt über eine eigene Zweitür-Kammer; Seitentore als Vakuumgrenze geschlossen/verriegelt, bisheriges Mitteltor offen innerhalb der Kammer. E an der Schleusentür startet den Durchgang. Gemeinsame Bay-Oberflächen auch an weiteren Stationsflächen;133+29 aktuelle Prüfungen Exit0. Die folgenden drei frei bedienbaren Außentore beschreiben den Stand19.10.; die isolierte Hallenprobe wurde nicht neu gebaut.

Der Nutzer beauftragt einen großen, frei beurteilbaren Versuch für Hallen, Materialien und Texturen der Raumstation. Zunächst entstand ein eigenständiger Ausschnitt der Drohnenwerkstatt mit eigener Blender-Quelle, Unity-Szene und Anwendung. Nach positiver Rückmeldung wird derselbe Hallenausschnitt am19.10.2026 auf ausdrücklichen Wunsch in die tatsächliche Startstation übernommen.

## In der Station vom Ring aus betreten

`Start-Drohnenhalle-Station.cmd` startet den normalen Development-Spielbuild direkt im Ring vor dem Eingang S02, mit Blick in die neue Halle. Normale Starts und `Start-Startstation-Dev.cmd` beginnen weiterhin an ihrem bisherigen Einstieg. Von der Kommandozentrale aus am Ring nach rechts zum Drohnendeck gehen.

Der äußere erste Werkstattabschnitt bei Radius38–54m wird durch das detaillierte Modell ersetzt. Dahinter bleibt das vorhandene offene Außendock bei54–70m mit zehn echten Drohnenplätzen erhalten. Die Ringöffnung ist auf den rund6,6m breiten Halleneingang abgestimmt; Bodenhöhe und Maßstab sind unverändert. Die geschlossene Kulisse des isolierten Versuchs (hintere Anschlusswand/-seiten/-decke, Schild und kurzer Außensteg) ist aus dem Stationsprefab entfernt. Alte Hallenwände, Dach und Reparaturbänke werden dort nicht zusätzlich erzeugt. Serviceschränke seitlich versetzt, damit alle drei äußeren Torwege frei bleiben.

Die Station verwendet ihre bestehende Kamera, Bewegung, Dev-Sprint, Zustandsanzeige und Vollbildsteuerung. Keine zweite Testkamera und keine Experiment-Sidebar. **E** bedient in Tornähe das nächste Drohnentor; Pult und bisherige Schleusenbedienung bleiben erreichbar. Alle drei Tore beginnen in dieser zugänglichen Fassung offen. Ein geschlossener Flügel blockiert die reale Spielerfigur; die Belegungserkennung verhindert Schließen auf den Besucher. Torzustände werden derzeit nicht im Spielstand gespeichert und beginnen beim Neuaufbau wieder offen.

`StationDroneHall.cs` steuert ausschließlich das Modul und bezieht die Besucherposition vom existierenden `StationInteriorMode`. Das Ressourcenprefab `Assets/SpaceMiner/Resources/Station/DetailedDroneHall.prefab` wird durch `HallExperimentBuild.PrepareStationPrefab` aus dem Hallenimport und seinen Materialien erzeugt. `HallExperimentBuild.BuildStation` bereitet dieses Prefab vor und baut anschließend den normalen Development-Spielbuild. `RingWalkthrough` nutzt es für die Kampagnen-Startstation; die separate zweistöckige Ausbau-Größenprobe bleibt bei ihrer bisherigen Geometrie.

Layoutrevision4 aktiviert die vorhandene Freiraumprüfung für ältere Spielstände: freie Ringpositionen bleiben erhalten, Positionen in neuem Mobiliar werden sicher zum Stationspult versetzt. Ressourcen, Akku und gespeicherte Ausdauer werden weiter über die bestehenden Save-Funktionen behandelt. Keine neuen Produktions-/Reparaturökonomien oder zusätzlichen Tripo-Aufträge.

Aktuelle Integrationsprüfung und Bilder: [Stations-Prüfbericht](Pruefungen/Drohnenhalle-Station-2026-10-19.json). Die unten verlinkte36-Prüfungen-Abnahme dokumentiert den vorherigen eigenständigen Versuch, nicht die Stationsintegration.

Finale Prüfung19.10.2026: Stationsbuild und vollständiger137-Prüfungen-Lauf jeweils Exit0. Alle drei Außenportale, Ring→Halle→Ring mit realer Kapsel, Torblockade/-öffnung/Belegungsschutz, Spielstände einschließlich neuer Schrankvolumen sowie echter Wasserflug bestanden.15 aktuelle Stationsbilder, davon Eingang/Hallenarbeitsplatz/Front/Außendock repräsentativ visuell geprüft. Die separate Hallenprobe mit denselben aktualisierten Modellquellen ebenfalls neu gebaut und36 Prüfungen/Exit0; sieben weitere Bilder. Quellprojektversion unverändert. Die ältere Einzelabnahme bleibt als historischer Bericht erhalten.

## Sicherungsumfang im Repository

Auf ausdrücklichen Nutzerauftrag werden Einzelversuch und Stationsintegration gemeinsam eingecheckt und gepusht: native Blender-Datei mit eingebetteter Albedo, reproduzierbarer Modellgenerator, FBX, alle vier Texturkarten, Unity-Materialien, Szenen-/Ressourcenprefab und sämtliche zugehörigen `.meta`-Dateien. Dazu gehören Import-/Buildcode, Torsteuerung, Stationsanbindung, Laufzeitprüfungen, beide Startdateien, Fach-/Übergabedokumentation und die historischen sowie aktuellen Prüfberichte mit Bildern und Logs.

Vor dem Sicherungscommit am19.10.2026 wurden alle98 im aktuellen Integrationsbericht aufgeführten Quellen-/Asset-/Nachweisdateien erneut per SHA256 gegen den Abnahmestand verglichen; alle identisch. Neue Unity-Asset-/Metadatenpaare vollständig. Für den Sicherungsauftrag wurden keine neuen Unity-Builds oder Laufzeittests ausgeführt; die oben beschriebenen137+36 Prüfungen gehören zur unmittelbar vorherigen Implementierungsabnahme. Commitkennung und Pushstand sind dem Git-Verlauf beziehungsweise Remote zu entnehmen.

`Builds`, `Library`, portable Blender-Binärdateien und lokale Hilfs-/Prüfskripte unter `work` bleiben nach Projektkonvention ignoriert. Der übertragbare Wiederaufbau benötigt die unten beschriebenen Generator-/Buildmethoden; fertige lokale EXE-Dateien werden am anderen Rechner neu erzeugt. Bestehende Tripo-Drohnenquellen sind bereits im Repository enthalten, keine weiteren externen Modelldownloads nötig.

## Start und Bedienung

`Start-Hallenexperiment.cmd` startet `Builds/HallExperiment/HallExperiment.exe`. Die Anwendung beginnt mit freier Maus und Übersicht. Rechts stehen vier Blickpunkte, Materialvergleich, Bauteildetails, Lichtwahl, Vollbild und Beenden zur Verfügung.

| Eingabe | Funktion |
| --- | --- |
| Tab / Begehen | Maus erfassen, frei gehen und umsehen |
| WASD / Maus | Bewegen / Blickrichtung |
| Shift | Dev-Sprint mit Ausdauerverbrauch |
| Esc / Tab | Maus freigeben |
| E in Tornähe | Nächstes Tor öffnen oder schließen |
| 1–4 | Übersicht, Arbeitsplatz, Schleusen, Materialdetail |
| T | Ausgearbeitete Materialien / schlichte Flächen |
| L | Warmes Arbeitslicht / kühle Inspektion / Notlicht |
| F11 / Alt+Enter | Gemeinsame Vollbildsteuerung |

Im Gehmodus erscheinen die fünf bekannten Zustandsanzeigen. Ausdauer ist an die bestehende `PlayerVitals`-Mechanik angebunden; Hunger, Durst, Müdigkeit und Gesundheit bleiben hier Ausgangswerte. Kein Kampagnenspielstand und keine dauerhaften Einstellungen werden geschrieben.

## Raum und Bauteile

Die Maße folgen dem bestehenden ersten äußeren Hallenmodul: Radius 38–54 m, Winkelausschnitt ±13,5°, Tiefe 16 m und Höhe 5,5 m. Der Ausschnitt ist gebogen; die drei äußeren Portale sind rund 3,2 m breit und 3,4 m hoch. Ein kurzer Außensteg dient der Durchgangsprüfung, kein vollständiges Außendock.

Modelliert sind einzeln gefaste Boden- und Deckenkassetten, unterteilte Wandpaneele, Rippen, Schrauben, Lüftungslamellen, Kabelrinnen, Versorgungsrohre, Bodenmarkierungen, zwei Werkbänke, Schränke, Arbeitsplatz und ein Deckenträger mit Hebezeug. Vorhandene Tripo-Wartungsdrohne als stationäre Größenreferenz auf dem Prüfstand; ungefähr 2,5 m maximale Grundrissausdehnung. Kein neuer Tripo-Auftrag und kein Creditverbrauch.

Die Torflügel sind separate bewegliche Objekte. Das mittlere Tor beginnt offen. Geschlossene Tore haben Collider; offene Tore geben den Weg auf den Außensteg frei. Bei belegtem Durchgang wird Schließen abgewiesen beziehungsweise ein bereits schließendes Tor wieder geöffnet. Das ist eine lokale Torsteuerung ohne Drucksimulation, Drohnenverkehr oder echte Werkstattaufträge.

Der Blender-Bericht zählt709 erzeugte Ausgangsobjekte und aktuell181 exportierte Meshobjekte mit64.392 Vertices nach Anwendung der Modifikatoren und Gruppierung. Das Schild der geschlossenen Testkulisse ist seit der Integration einzeln entfernbar; zuvor180 Meshobjekte. Unity erstellt im eigenständigen Versuch157 sichtbare Hallenrenderer und30 Collider; die Stationsfassung entfernt anschließend die Testkulisse. Die detaillierte Referenzdrohne kommt zusätzlich hinzu; diese Zahlen sind keine Aussage über die Optimierung einer ganzen Station.

## Materialien und Herkunft

Eigene Palette: warmes helles Lackmetall, dunkles Blau, gebürsteter/dunkler Stahl, Gummi, Warnorange und cyanfarbene Anzeigen. Gefaste Kanten, Fugen, Schrauben und Leitungen sind Geometrie. Die Flächen nutzen UVs in Modellmetern, Standard-PBR-Materialien und eine lokale Reflexionssonde.

`PaintedAlloy_Albedo.png` ist ein unverändertes Bild aus dem eingebauten ImageGen-Werkzeug. Vorgabe: quadratische, flache, möglichst kachelbare Albedotextur aus neutralem hellgrauem pulverbeschichtetem Aluminium, feine Körnung und wenige Kratzer, ohne Paneelfugen, Schrauben, Schrift, Perspektive oder eingebrannte Beleuchtung. Gewünscht waren 2048 px; tatsächlich geliefert wurden **1254×1254 px**. Die Bilddatei wird unverändert übernommen. Nahtlosigkeit wurde nicht messtechnisch nachgewiesen.

`MicroNormal.png`, `Paint_MetalSmooth.png` und `Steel_MetalSmooth.png` sind unabhängig davon durch den Unity-Builder erzeugte 512-px-Karten. Ihre Werte sind eine gestalterische PBR-Näherung, keine aus einer realen Oberfläche vermessenen Materialdaten. Farbkarten werden als sRGB importiert, Datenkarten linear, die Normalenkarte als Normal Map. Alle Karten werden ausdrücklich als Texture2D importiert. Ein anfänglich falscher Cubemap-Import und ein beim Build entfernter Sternshader wurden vor der endgültigen Prüfung korrigiert.

Beleuchtung: lokale Arbeitslichter, gerichtetes kaltes Außenlicht, Emissionsflächen, Echtzeitreflexion. Keine gebackene Global Illumination, kein aufwendiges Postprocessing und kein Leistungsbudget für die gesamte Station. Hauptziel sind Maßstab, Modulsprache, Oberflächenwirkung und Vergleichbarkeit.

## Quellen und Wiederaufbau

| Datei / Verzeichnis | Zweck |
| --- | --- |
| `docs/Art/HallExperiment/Station-Hall-Experiment-v01.blend` | Native bearbeitbare Blender-Quelle, Textur eingebettet |
| `docs/Art/HallExperiment/model-report.json` | Geometriemaße und Exportinventar |
| `tools/CreateHallExperiment.py` | Reproduzierbarer Blender-Modellgenerator |
| `Assets/SpaceMiner/Art/HallExperiment` | FBX, Texturen, Materialien, Unity-Metadaten |
| `Assets/SpaceMiner/Editor/HallExperimentBuild.cs` | Import, PBR-Karten, Szene und Windows-Build |
| `Assets/SpaceMiner/HallLab/HallExperimentView.cs` | Bewegung, Tore, Vergleich und UI |
| `Assets/SpaceMiner/HallLab/HallExperimentCheck.cs` | Opt-in-Laufzeitprüfung und Bilder |
| `Assets/SpaceMiner/HallLab/HallExperiment.unity` | Eigenständige Versuchsszene |

Blender **4.5.14 LTS** wurde für diesen Versuch als offizielle portable Ausgabe lokal nach `work/HallExperiment` entpackt. Hersteller-SHA256 der ZIP bestätigt: `B9533D2397AC1984DB4466FB23A7A4649391CCA93F6E84209F9BCC60D071C8B9`. Keine globale Blender-Installation, kein MCP-Add-on erforderlich. Werkzeuge und Builddateien bleiben nach Projektkonvention Git-ignoriert.

Mit lokal vorhandenem Blender den Generator aus dem Projektverzeichnis ausführen:

```powershell
& '<Blender-Pfad>/blender.exe' --background --python tools/CreateHallExperiment.py
```

Bei geschlossenem Unity-Editor `SpaceMiner.Editor.HallExperimentBuild.Build` per `-batchmode -quit -projectPath <Projekt> -executeMethod ... -logFile <Log>` aufrufen. `Prepare` erzeugt nur Assets und Szene. Quellprojektversion bleibt 6000.4.7f1; hier wird mit dem vorhandenen Editor 6000.6.4f1 in der separaten Kopie `Builds/LocalPreview-6000.6.4f1` gebaut. Am Zielrechner zuerst passende lokale Editorversion prüfen.

Der Development-Player prüft mit `-hallExperimentCheck` opt-in Bindungen, Maße, Texturen, Boden, tatsächliche Kapselbewegung an geschlossenem und offenem Tor, Torbewegung, Belegungsschutz, Sprint/Ausdauer sowie Vergleichs- und Cursorzustände. Er schreibt `Logs/HallExperiment/result.json` und sieben Laufzeitbilder; Erfolg ist Exit0 und `HALL EXPERIMENT CHECK PASSED`. Aufruf aus dem Projektverzeichnis, damit die Prüfdateien dort landen. Aktuelle tatsächliche Ergebnisse und Grenzen stehen im zugehörigen [Prüfbericht](Pruefungen/Hallenexperiment-2026-10-19.json).

Offen für weiteres Nutzerfeedback: Materialfarben und Abnutzung, Beleuchtung, Detailsprache, Größenwirkung und gewünschte Qualität weiterer Räume. Die erste Drohnenhalle ist inzwischen ausdrücklich zur Integration freigegeben; weitere Räume bleiben unverändert. Keine subjektive Abnahme von Haptik, manueller Eingabe oder Leistung durch automatisierte Prüfungen ersetzen.


## Bestandsanalyse vom24.10.2026

Auf Nutzerfrage erneute Prüfung von Dokumentation, Tor-/Ring-/Spieleranbindung und vorhandenen lokalen Dateien. Aktueller Commit bba0a6b enthält die Integration der detaillierten Drohnenhalle S02 in die echte Startstation; separate Hallenprobe und zehn äußere Dockplätze bleiben vorhanden. Alle65 Quellen-/Assetdateien,33 Prüfbelege und494 ausgelieferten Builddateien stimmen heute per SHA256 mit dem Integrationsbericht vom19.10.2026 überein. Beide Starteinstiege und EXE-Dateien vorhanden. Aufnahme des Arbeitsplatzes erneut angesehen. Keine neue Implementierung, kein Unity-Build oder Laufzeittest;137 Stations-/36 Einzelprüfungen bleiben Ergebnisse vom19.10.2026. Offene Grenzen: Torzustände beim Neuaufbau wieder offen, keine Drucksimulation oder Werkstatt-/Reparaturökonomie. [Bestandsnachweis](Pruefungen/Drohnenhalle-Bestandsanalyse-2026-10-24.json).


Developer-Rundgang24.10.2026: Direkteinstieg `Start-Drohnenhalle-Station.cmd` nutzt nun den neu gebauten Hauptstations-Development-Player mit6,6m/s beim Gehen,20,8m/s mit Shift und100% Stamina. Stations-/Hallenmaße bleiben unverändert.29 Körperzustands-/Bewegungsprüfungen und137 Stationsprüfungen Exit0; keine neue Bild-/manuelle Eingabeabnahme (ScreenCapture-Warnungen im versteckten Prüflauf). Separate Hallenprobe bleibt bei ihrer bisherigen Bewegung. [Prüfnachweis](Pruefungen/Developer-Bewegung-2026-10-24.json). Die vorherige Bestandsanalyse beschreibt den Zustand vor dieser anschliessenden Codeänderung.
