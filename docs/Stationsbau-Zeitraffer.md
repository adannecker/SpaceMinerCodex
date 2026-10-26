# Stationsausbau: Fertigung, Drohnenmontage und Zeitraffer

Neu25.10.2026: Die Rohstofflagerregel aus dem Nutzerauftrag vom24.10. gilt auch für die S03-Bauprobe: erste5,33m-Stufe statt bisher16m, weiterhin vier getrennte Rollen und zweischichtige Wabenhülle.777 Teile/100 Lieferungen,5973kg/25h30min Entwurfswerte,1280× etwa1min12s. Das Wasserlager zeigt gleichzeitig ein intaktes und zwei zerstörte Drittel mit drei eigenen Seitenanschlüssen. [Aktueller Prüfbericht](Pruefungen/Rohstofflager-Drittel-2026-10-25.json), [Lageraufbau/Bedienung](Ringstation-Ausbau.md#rohstofflager-in-drei-kurzen-ausbaustufen-24102026).29 Bauproben- und217 Stationsprüfungen derselben finalen Development-Binärdatei Exit0. Die Abnahmeabschnitte mit28/195 beziehungsweise14/193 Prüfungen unten beschreiben die früheren Größenstände.

Stand25.10.2026, Wabenhülle/vier Baurollen und verkürzte Rohstofflagerstufe. Nutzer möchte vor Texturen den Bauablauf einer Erweiterung durchdenken und als Zeitraffer sehen: Werkstoffdrucker fertigt Bauteile, ein ganzer Raum darf nicht auf einmal entstehen. Außerdem sollen die drei beschädigten Reaktorgänge unterschiedliche Bruchbilder erhalten.

## Vorschlag für das spätere Spiel

### Bestätigte Hülle und vorgeschlagene Druckabschnitte

Nutzerrückmeldung24.10.2026: Die zweischichtige Ausführung gefällt und wird als Gestaltungsentscheidung festgehalten: innere druckhaltende Hülle, separate äußere Schutzlage. Anzahl/Material der technischen Unterlagen und genaue Schutzwirkung sind damit nicht bemessen.

Der Nutzer diskutiert große, normalerweise offene Schleusen für einfachen Zugang trotz möglicher Lecks. Vorschlag, noch keine Implementierungsfreigabe: kurze Verbindungskammer mit zwei großen druckdichten Schotten. Bei gleichem Druck stehen beide offen; normale Wege brauchen keinen Schleusenzyklus. Soll ein druckloser Raum betreten oder ein Modul bewusst isoliert werden, arbeitet dieselbe Verbindung als echte Schleuse mit Druckausgleich und gegenseitiger Türverriegelung. Voraussetzung sind ausreichend Kammerraum für Mensch/Drohne samt Ladung sowie Luftführung, Ventile und Antrieb; zwei Türen allein ergeben noch keine funktionierende Schleuse.

Für das Spiel vorgeschlagene Zustände: Durchgang offen, wird gesichert, isoliert, Druckausgleich, Schleusenfahrt, blockiert/defekt. Ein Leck sperrt Aufträge/Verkehr in den betroffenen Abschnitt und löst das Sichern seiner Grenzen aus. Türbereich/Belegung und Druckdifferenz sind zu prüfen; keine Zusage, dass ein riesiges offenes Tor bei beliebig schnellem Druckverlust noch rechtzeitig schließt. Stromausfall-/Notantrieb und manuelle Bedienung bleiben auszuarbeiten. Vor Wiederöffnung erst Leck reparieren und Druck angleichen.

Zusätzlich braucht der Ring selbst Druckgrenzen: Bei einem Leck im umlaufenden Gang müssen dessen beide Nachbargrenzen schließen, einschließlich absperrbarer Lüftungs-/Medienwege. Nur Türen an äußeren Anbauten isolieren keinen Ringschaden. Gefährdete/drucklose Baustellen und Außenzugänge bleiben normalerweise geschlossen; komfortable offene Durchgänge gelten für sichere Bereiche mit gleichem Druck. Große Türöffnung passend zur tatsächlichen Transporthülle, nicht automatisch der gesamte9m-Ringquerschnitt. Konkrete Tür-/Kammermaße und Steuerung noch offen.

Fachliche Grundlage: NASA unterscheidet Zugang/Abtrennung druckbeaufschlagter Volumina, verlangt Druckausgleich vor dem Öffnen und freie, von beiden Seiten bedienbare Durchgänge auch unter Berücksichtigung von Robotern. [NASA Architecture, Abschnitt8.4](https://www.nasa.gov/reference/8-0-architecture-vol-2/). Die hier vorgeschlagene automatische Spielsteuerung ist unsere Gestaltung, keine Behauptung über die reale ISS-Steuerung. Aktueller Unity-Stand bleibt bei den bisherigen offenen Portalen/festen Bruchschotten; diese Diskussion ergänzt noch keine Drucksimulation.

Eine Erweiterung wird ein **Bauauftrag mit Stückliste und einzelnen Arbeitsaufträgen**. Der Werkstoffdrucker ist eine Fertigungszelle mit begrenztem Bauraum, Materialzufuhr, Roboterkopf und Prüfstation. Er verarbeitet vorbereiteten Werkstoff wie Metallhalbzeug/Draht oder Polymermaterial; Rohgestein braucht zunächst Aufbereitung. Kurze Profile, Knoten, Halterungen und Paneele sind die erste Produktfamilie. Elektronik, Ventile, Dichtungen und andere Spezialkomponenten kommen abhängig vom Technologiestand aus dem Lager. Nicht alle Stoffe oder Geräte kann derselbe frühe Drucker herstellen.

| Schritt | Arbeit und sichtbares Ergebnis | Voraussetzung für den nächsten Schritt |
| --- | --- | --- |
| Planung/Absicherung | Anschluss scannen, Bauraum und Drohnenwege prüfen, Stückliste und Montagefolge erstellen; bestehendes Stationsschott geschlossen halten | Anschluss tragfähig, freie Flugwege, Material-/Energieversorgung vorgesehen |
| Anschluss vorbereiten | Materialpalette und Fertigungszelle bereitstellen; erste kleine Knoten/Adapter montieren | Geometrie vermessen, sichere mechanische Verbindung |
| Tragwerk | Profile einzeln fertigen, abkühlen, prüfen; Drohne bringt ein Rack, zweite Drohne richtet Teile aus und fügt sie | Zusammenhängende tragfähige Struktur, geprüfte Verbindungen |
| Boden | Kleine Boden-/Sandwichplatten in das Tragwerk einsetzen | Befestigung geprüft; noch kein bewohnbarer Innenraum |
| Medien | Strom-/Datenkanäle, Rohrhalter und vorgefertigte Geräte einbauen, bevor die Hülle den Zugang verdeckt | Elektrische Trennung, Anschlüsse und Leitungswege geprüft |
| Druckhülle | Seiten, Stirnwand und Dach Platte für Platte schließen; Nähte/Dichtungen bearbeiten | Hülle vollständig, Verbindungen zugänglich geprüft |
| Außenschutz | Separate Isolier-/Schutzlagen und austauschbare Abdeckungen anbringen | Schutz und Wartungszugänge vollständig |
| Abnahme/Anschluss | Struktur-/Nahtkontrolle, stufenweise Dicht-/Druckprüfung und Medienprüfung; danach Druck-/Versorgungsabgleich und Freigabe des Durchgangs | Erst nach erfolgreicher Abnahme das Stationsschott öffnen |

Die Montage ist Arbeit im Vakuum; ein schon sichtbarer Boden macht den Raum nicht automatisch bewohnbar. Im späteren Spiel müssen auch zusätzliche Masse, Lage der Station, Drucklasten, Wärmeabfuhr sowie Schatten auf Solarfeldern berücksichtigt werden. Die heutigen Spielwerte2kW Reaktor plus0,5kW Solar sind noch keine ausgelegte Versorgung einer schnellen Metallfertigung. Höhere Fertigungsleistung, langsamerer Betrieb und/oder Puffer müssten als eigene Projektentscheidung bilanziert werden.

Spielerisch bleiben montierte Bauteile bei Unterbrechungen erhalten. Ein Auftrag kann auf Werkstoff, Energie, freien Drucker, Transport, Montage oder Abnahme warten. Schlechte Teile werden vor dem Einbau ausgesondert; fehlerhafte Verbindungen lösen Nacharbeit aus. Zunächst reicht eine verständliche Stückliste mit diesen Zuständen, bevor eine vollständige Produktionswirtschaft entsteht. Das ist ein Vorschlag, keine bereits implementierte Ressourcen-/Forschungsmechanik.

## Zweischichtige Wabenhülle im Spiel

Auf aktuellen Nutzerauftrag besteht die sichtbare Stationshülle aus zwei geometrisch getrennten Lagen mit sechseckigen Paneelen. Die Ringsektoren und bisherigen Raummaße bleiben als Grundstruktur erhalten; die Räume werden nicht zu sechseckigen Einzelzimmern. `HoneycombHull.cs` schneidet ein regelmäßiges Wabenraster an geraden, kreisförmigen und ringförmigen Rändern passend zu. Vollständige Paneelrückseiten treffen sich; nur die Vorderflächen sind für sichtbare Fasen/Fugen eingezogen. Damit sind die dunklen Fugen keine absichtlich offenen Löcher durch die Drucklage. Materialien und Schutzwirkung sind vorläufige Gestaltung, keine technische Auslegung.

`RingWalkthrough.cs` verwendet die Waben für die bestehende Startstation: Ringböden/-decken und geschlossene Wandflächen, Kommandozentrale um ihre Fenster herum, äußere Reparaturhalle mit ihren drei Toren, offenes Außendock ohne neues Dach, Wasserlager, innere Reparaturwerkstatt, erhaltene Reaktorgangstücke, Wartungskragen und Reaktorkapselabdeckung. Glas, Toröffnungen, Solarpanelflächen und die drei bestehenden Ganglücken werden freigehalten. Die einzelnen Ausbaustufen behalten ihre radialen Grenzen. Drucklage und äußere Schutzlage sind eigene Meshes; die äußere Lage liegt 28cm außerhalb der bisherigen Hüllfläche. Bodenmarkierungen bleiben über der neuen Oberfläche. Frühere Hüllrenderer sind dort ausgeblendet, ihre Kollisionskörper bleiben bestehen. Pro Fläche zusammengefasste Waben vermeiden ein eigenes GameObject je Stationstile.

Der nominelle Reaktorkernblock bleibt Ø8m ×6m. Seine zusätzliche Wabenabdeckung ist äußerlich etwas größer (obere Seitenlage bis ungefähr r4,37m, obere Decklage bis y6,61m); der begehbare Kragen bleibt frei. Das detaillierte R01-Konzept aus dem anderen Chat wurde weder ersetzt noch importiert. Aus den Paneelen folgt keine bestätigte Materialstärke, Strahlenabschirmung, Wärmebilanz oder Druckfestigkeit.

## Laufende Bauprobe mit vier Drohnenrollen

`Start-Stationsbau-Zeitraffer.cmd` startet den Development-Spielbuild mit `-stationConstructionPreview`. Die Vorschau baut nur in dieser Sitzung eine5,33m-Rohstofflagerstufe am freien S03-Anschluss/60° neben der Drohnenhalle. Die normale Station erhält diese Erweiterung nicht als gespeicherten Ausbau.

| Rolle | Ort und Aufgabe | Sichtbare Übergabe |
| --- | --- | --- |
| B01 Fertigungsdrohne | Innere S02-Werkstatt, bedient Fertigungszelle und Kontrolle | Kleine Teile entstehen auf dem Druckbett; Spezialtechnik wird aus Lagerbestand geprüft |
| B02 Bereitstellungsdrohne | Innerer Transport vom Drucker über Ring und Reparaturhalle | Greift die Teile, hebt sie ab, bringt sie zur Materialkammer und legt sie auf die Übergabeauflage |
| B03 Lastdrohne | Außen, vier Greifer, mehrere Teile pro Lieferung | Wartet vor der Kammer, übernimmt die bereitgelegte Ladung, fährt heraus, steigt auf und fliegt zur Baustelle |
| B04 Schweiß-/Montagedrohne | An der Außenbaustelle | Richtet die angelieferten Teile einzeln aus, fügt und dichtet die Verbindungen mit sichtbarem Werkzeug |

Die vier Körper sind Konzeptfahrzeuge dieser Probe, keine neu aktivierten Kampagnendrohnen. B01 bedient eine Fertigungszelle mit vorbereitetem Werkstoff, keinen universellen Rohgestein-zu-Elektronik-Drucker. Die bestehende Flotte, Wasser- und Akkubestände bleiben von der Ablaufprobe getrennt.

Zwischen Halle und Außendock sitzt eine4m lange,4m breite Übergabekammer um r56,5m auf der30°-Achse. Zwei große Schiebetore arbeiten im Ablauf gegenseitig verriegelt: B02 liefert durch das offene Innentor, verlässt die Kammer, Innentor schließt; eine ausdrücklich simulierte Druckwechselphase folgt; erst dann öffnet das Außentor für B03. B03 greift, fährt vollständig heraus, Außentor schließt; der beladene Aufstieg erfolgt vor der Kammer, erst danach der Flug über die Station. Der Rückflug endet ebenfalls mit einer senkrechten Absenkung. Die Kammer wird als offener Schnitt gezeigt und ist noch keine physikalisch luftdichte, frei bedienbare Kampagnenschleuse. Die allgemeine Diskussion über normalerweise offene Verbindungen bleibt davon getrennt.

Ein Druckraum von2,4m je Achse begrenzt jedes Einzelteil. Die Probe enthält777 Teile in100 Lieferungen und5973kg vorläufige Modellmasse. Höchstens acht Teile werden flach gepackt, die Stapelhöhe einschließlich Zwischenraum ist auf1,8m begrenzt, damit die interne Drohne unter den bestehenden3,1m-Portalen samt bis auf2,92m heruntergezogenen Kupplungsrahmen bleibt. Jedes Teil geht als derselbe Bauteilgegenstand vom Druckbett über Bereitstellung und Außentransport bis zur Montage; kein Gesamtmodul wächst auf Raumgröße. Nach Montage eines Racks wird dessen Geometrie für die Darstellung zusammengefasst. Neustart verwendet die ursprünglichen Einzelteile erneut, ohne fortlaufend zusätzliche Modelle anzulegen.

Boden, Dach, beide Seiten und Stirnwand bekommen jeweils innere Druckpaneele und äußere Schutzpaneele, einschließlich der Unterseite. Die Paneele selbst sind geschlossen und besitzen sichtbare Fasen. In dieser Neubauprobe beträgt der Abstand der Paneelmittelebenen28cm bei8cm geometrischer Paneeldicke (20cm freier Zwischenraum); das ist ein Modellwert. Beim bestehenden Stationsblockout kommen die bereits vorhandenen Hüll-/Trägermaße hinzu. Der Bau folgt weiterhin Anschluss, Tragwerk, Boden, Medien, Druckhülle, Außenschutz und simulierter Abnahme.

Der Modellablauf umfasst25h30min und dauert bei1280× ungefähr1min12s. Diese Zeit ist keine technische Bauzeitberechnung oder freigegebene Produktionsbalance. Abnahme, Material-/Druck-/Stromprüfung bleiben simuliert; das bestehende S03-Stationsschott bleibt geschlossen. Der Anbau wird keine begehbare Kampagnenfläche und verursacht keine echten Inventarkosten.

Bedienung: Leertaste/Pause,80×/320×/1280×/5120×, Neustart, Ansichten Baustelle/Drucker/interne Transportstrecke/Übergabekammer/gesamte Station. **Hülle geschlossen zeigen / Schnittansicht zeigen** schaltet die vorhandenen Stationsdächer sichtbar um. Rechte Maustaste dreht, Mausrad zoomt, F11 Vollbild, Escape Spielmenü. Rechts stehen Bauphase, aktuelle Drohnenrolle, Teilefortschritt und Torzustände; B01–B04 werden zusätzlich am Modell beschriftet. Bei der Druckeransicht schaut die Kamera steiler durch das offene Gestell. Am Ende bleibt die Probe stehen.

Eigene Einstellungen und Savepfade liegen unter `Logs/StationConstruction-settings` und `Logs/StationConstruction/Saves`. Die Bauprobe beendet ihre speicherbare Spielsitzung und hält die Kampagnensimulation an. Normale Einstiege starten weiterhin die bisherige Kampagne mit der neuen Hüllgeometrie und unveränderter Layoutrevision6.

## Unterschiedliche Gangschäden

Die geschlossenen Not-Schotten und bisherigen Bodenlücken bleiben bestehen.90° bekommt einen asymmetrischen Schälriss mit aufgebogener Dachhaut und ungleichen Trägerresten.180° zeigt Torsion mit auseinander verdrehten Seitenresten und mehreren Scherben.270° hat eine eingedrückte Dachplatte, kurze Stümpfe und geknickte abgerissene Leitungen. Diese zusätzlichen Reste sind visuelle Geometrie außerhalb des zugänglichen Weges; die bestehende Zugangssicherung bleibt maßgeblich. Keine Texturarbeit oder neue Drucksimulation.

## Quellen und Einordnung

Die Trennung von Herstellen, Zusammenbauen und Prüfen orientiert sich an realen ISAM-Arbeiten. NASA beschreibt die robotische Zusammenfügung separat angelieferter Bauteile und ARMADAS-Bauroboter: [NASA ISAM](https://www.nasa.gov/isam/). Das Archinaut-Technologieprojekt nennt Herstellung, Handhabung, Montage gedruckter/vorgefertigter Teile und Inspektion als eigene Teilsysteme: [NASA TechPort](https://techport.nasa.gov/projects/93903).

Die OSAM-2-Seite dokumentiert thermische Vakuumtests und gedruckte Träger; das Vorhaben endete2023 **vor einer Flugdemonstration**. Unsere vollautomatische bemannte Raumerweiterung ist damit nicht als heute nachgewiesene Fähigkeit behauptet. [NASA OSAM-2](https://www.nasa.gov/mission/on-orbit-servicing-assembly-and-manufacturing-2-osam-2/). Die Anforderungen an Entwicklung, Herstellung und Prüfung additiver Raumfahrtbauteile behandelt [NASA-STD-6030](https://standards.nasa.gov/standard/NASA/NASA-STD-6030). Quellen online am24.10.2026 gelesen; konkrete Geometrie, Zeiten und Masse dieser Probe sind eigene Spielvorschläge.

## Wiederaufbau und Prüfung

Quellen: `HoneycombHull.cs` samt.meta (Raster, zugeschnittene geschlossene Waben und Kreisrand), `StartStationCheck.cs` (reguläre Hüllen-/Wegeprüfung), `StationConstructionPreview.cs` samt.meta (isolierter Start, Teileplan, Fertigung/Transport/Montage, UI und explizite Prüfung), `PlayerSettings.cs` (eigener Einstellungspfad), `RingWalkthrough.cs` (drei verschiedene Schadensformen). Bestehende Szene unverändert. Bei geschlossenem Editor vorhandene Szene über `SpaceMiner.Editor.StartMenuBuild.Run` als Development bauen. Die Quelle bleibt6000.4.7f1; auf diesem Rechner wird die vorhandene6000.6.4f1-Buildkopie benutzt.

`-stationConstructionCheck` prüft Teil-/Rackgröße, Produktionsreihenfolge, Pause/Reset, Abnahmevoraussetzungen, Kampagnenisolation und vollständigen Aufbau und erzeugt direkte3D-Renderbilder unter `Logs/StationConstruction`. `-startStationCheck` prüft die weiterhin begehbare Hauptstation und ihre Bruchschotten. Aktueller Sitzungsnachweis und tatsächliche Prüfgrenzen stehen in [Prüfbericht](Pruefungen/Stationsbau-Zeitraffer-2026-10-24.json).

Historischer erster Bauprobenstand vor dem Vier-Rollen-Umbau: Development-Build Exit0,14 Bauprobenprüfungen und193 Stationsprüfungen derselben Binärdatei Exit0. Zehn direkte Renderbilder der Bauprobe und sechs der normalen Station gesichert; Drucker, Zwischenbau, fertiger Anbau und verschiedene Schäden visuell geprüft. Die erste Bauraumprüfung überschätzte gedrehte Paneele durch Rotation ihrer achsparallelen Hüllbox; Vermessung auf tatsächliche Meshpunkte korrigiert und danach vollständig neu bestanden. Die Station meldet weiterhin15 fehlgeschlagene versteckte ScreenCapture-Aufnahmen, deshalb nur direkte3D-Renderbilder als Bildbelege. Keine neue GUI-/manuelle Eingabeabnahme, kein Release- oder Leistungsbenchmark. Vorherigen Spielbuild bewahrt; kein Commit/Push.


## Abnahme der ersten Waben-/Vier-Rollen-Fassung (24.10.2026)

24.10.2026: Finales Development-Build Exit0,28 Bauprobenprüfungen und195 Stationsprüfungen derselben Binärdatei Exit0. Geprüft sind unter anderem Randabdeckung, Kreiszuschnitt, allseitige Außenhülle, tatsächliche Teil-/Stapelgrößen, kontinuierliche Übergaben, Torverriegelung,1001 Trajektorienproben der ersten Lieferung einschließlich echter Stationskollisionen der Transportkörper samt Lastaufnahme, vollständiger Aufbau und Kampagnenisolation. Die internen Rahmen reichen tatsächlich bis2,92m herunter: Warteposition/Flughöhe und Ladung daran angepasst. Der Außenstart liegt vor den Ladesäulen; erst außerhalb der Kammer steigen und danach abfliegen. Fertige Lieferungen nutzen wiederverwendbare zusammengefasste Geometrie; Reset erzeugt keine zusätzlichen Körper.

11 relevante Quell-/Metadateien zur Buildkopie identisch,247 ausgelieferte Builddateien SHA256-identisch.21 direkte3D-Renderbilder und Iterationslogs unter [Prüfbericht](Pruefungen/Wabenhuelle-Baudrohnen-2026-10-24.json), ausgewählte Produktions-/Übergabe-/Hüllenansichten visuell geprüft. Normale Stationsprobe weiterhin15 fehlgeschlagene versteckte ScreenCapture-Aufnahmen; kein neuer GUI-/manueller Eingabe-, Release- oder Leistungsbenchmark. Vorheriger Build unter `Builds/Windows-before-honeycomb-20261024` bewahrt, geprüfter Build unter `Builds/Windows`. Quellversion unverändert, fremde Reaktorarbeit bewahrt, kein Commit/Push. Die14/193-Prüfung oben ist der historische Stand vor diesem Umbau.
