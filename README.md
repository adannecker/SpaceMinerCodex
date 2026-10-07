# Space Miner — Wasserprototyp

Ein 3D-Aufbau-/Survival-Spiel in einem Asteroidengürtel mit beschädigter modularer Raumstation. Die erste Spielschleife schickt eine Drohne zu einer Eisquelle und lässt sie den Stationstank befüllen.

Für den aktuellen Rechnerwechsel und den Kontext aller bekannten Themenchats: [Projektübergabe](docs/Projektuebergabe.md), [Projektmemory](docs/Projektmemory.md) und [Chatverzeichnis](docs/Chats/README.md). Die neueren Archive ergänzen die drei ursprünglichen Chatverläufe; neue lokale Chats können damit auf einem anderen Rechner fortsetzen.

## Entwicklungsumgebung

- Projektversion: Unity **6000.4.7f1**. Die passende lokale Installation beim Rechnerwechsel prüfen.
- Windows 64 Bit, Built-in Render Pipeline, C#.
- Visual Studio Community 2022 ist auf diesem Rechner installiert. Das Unity-Paket für die Visual-Studio-Anbindung ist im Projekt eingetragen; Unity lädt es beim ersten Import. Keine kostenpflichtigen Assets nötig.
- **1 Unity-Einheit = 1 Meter.** Der aktuelle Test zeigt 100 Asteroiden als räumliche Wolke rund um die Station, mit 100 m bis 5 km Durchmesser. Eine Bibliothek aus 100 individuellen Formen wird mit verschiedenen Größen und Drehungen wiederverwendet. Die etwa 60 m breite Raumstation besitzt einen Drohnendock mit zwei Reihen zu je vier Ladebuchten und zwei Wartungsbuchten für die zehn vorhandenen Drohnen. Drohne 01 ist versorgt und einsatzbereit. Drohne 02 funktioniert, wartet aber ohne Ladung und Treibstoff; acht weitere sind defekt. Diese ältere Zwei-Drohnen-Logik bleibt gegenüber der Ein-Drohnen-Spielidee abzugleichen.

## Starten

1. Unity Hub öffnen → **Projects → Add → Add project from disk** → diesen Ordner auswählen.
2. Mit Unity **6000.4.7f1** öffnen und den ersten Import abwarten. Die Prototypszene wird beim ersten Öffnen automatisch erstellt.
3. Szene `Assets/SpaceMiner/Scenes/AsteroidBelt.unity` öffnen. Falls sie noch nicht vorhanden ist: Menü **Space Miner → Prototyp einrichten**.
4. Oben auf **Play** klicken, dann in die **Game**-Ansicht klicken, damit sie Tastatureingaben erhält.

Alternativ kann das gebaute Spiel unter `Builds/Windows/SpaceMiner.exe` direkt gestartet werden.

Beim Start erscheint ein kleines Startmenü mit **Demo starten**, **Konfiguration** und **Beenden**. Demo starten beginnt **Miras Intro** mit Texteinblendungen, einer ersten deutschen weiblichen Teststimme und Kamerafahrten. **Esc** überspringt das Intro und wechselt direkt ins Spiel. Währenddessen pausieren Simulation und Kamerasteuerung. Nach dem letzten Satz startet das Spiel automatisch.

Lokale Desktop-Vorschau vom 06.10.2026: Der Stand `5cfd179` wurde mit der hier installierten Unity-Version **6000.6.4f1** in einer separaten Kopie unter `Builds/LocalPreview-6000.6.4f1` gebaut. `Builds/Windows` enthält diese neue Spielversion; die vorherige liegt unter `Builds/Windows-before-current-preview`. Die gemeinsame Projektversion bleibt 6000.4.7f1. Development-/Release-Build und Techtree-Datenprüfung erfolgreich; der automatische Spieltest endete ohne Abschlussbericht und gilt nicht als bestanden. Die normale Spielversion wurde anschließend sichtbar gestartet.

Die anschließend ergänzte Dock-Version enthält rückwärts geparkte Drohnen, eine direkte Tankkupplung, Klammern mit fest dimensionierten Schuhen sowie kantige Eisstücke im sichtbaren Frachtraum hinter einer verglasten Klappe. Finale Development-/Release-Builds und **819 Spielprüfungen** auf diesem Desktop mit Unity 6000.6.4f1 bestanden, inklusive Wasserbilanz und Rückwärtsparken. Diese überarbeitete Version liegt jetzt unter `Builds/Windows`; ihr Vorgänger unter `Builds/Windows-before-drone-dock`. Details: [Bergbaudrohnen](docs/Bergbaudrohnen.md).

Der spätere Stand vom 06.10.2026 ergänzt Wasserabbau-Erfahrung, Rückkehrreserve und das durchgehende Maya-Testintro mit zeitgekoppelten Untertiteln. Letzte dokumentierte Vorschau: 823 Spielprüfungen; Abbaubalance separat mit 194 Prüfungen. Die Übergabe nennt Herkunft und Grenzen dieser Ergebnisse. Alle neun aktuellen Themenchats sind als Textarchive gesichert; kopierbare Startaufträge zum Fortsetzen beziehungsweise Anlegen am anderen Rechner stehen in [Rechnerwechsel-Memory](docs/Rechnerwechsel-Memory.md). Builds werden nicht durch Git übertragen und müssen dort neu erstellt werden.

Bei jedem Start beginnt **Miras Intro** mit Texteinblendungen, einer ersten deutschen weiblichen Teststimme und Kamerafahrten. **Esc** überspringt das Intro und wechselt direkt ins Spiel. Währenddessen pausieren Simulation und Kamerasteuerung. Nach dem letzten Satz startet das Spiel automatisch.

## Steuerung

| Eingabe | Funktion |
| --- | --- |
| Mausrad | Rein-/rauszoomen |
| Shift + Mausrad | Vierfach schneller zoomen |
| Rechte Maustaste halten und ziehen | Um den Ansichtsmittelpunkt drehen |
| Mittlere Maustaste halten und ziehen | Ansicht verschieben |
| W / A / S / D | Vorwärts / links / rückwärts / rechts, relativ zur Kamera |
| Q / E | Abwärts / aufwärts, relativ zur Kamera |
| Shift halten | Schneller bewegen |
| R oder Pos1 | Standardansicht beim Schiff wiederherstellen |
| B | Übersicht über das gesamte Testfeld |
| Linksklick, dann F | Objekt auswählen und darauf zoomen |
| Tab | Außen Drohne 01 verfolgen; innen Maus freigeben / einfangen |
| V | Im Spiel zwischen Kommandanten-Außenansicht und Stations-Innenansicht wechseln |
| E | Innen in der Nähe das Stationspult bedienen oder die Schleuse starten |
| Leertaste | Simulation pausieren / mit vorherigem Tempo fortsetzen |
| F11 oder Alt + Enter | Zwischen Fenster und Vollbild wechseln, auch während des Intros |
| H | Informationsanzeige ein-/ausblenden |
| Escape | Im Intro: überspringen. Im Spiel: pausierte Beenden-Rückfrage; erneut Escape oder Weiterspielen bricht ab |

Die Kamera kann von **3 Metern bis 1.000 Kilometern** Abstand herauszoomen. Die Sichtweite und die Bewegungsgeschwindigkeit passen sich dem Zoomabstand an. **B** bringt dich zur Übersicht über das aktuelle Testfeld, **R** zurück zum Schiff. Bei sehr großen Abständen werden die maßstabsgetreuen Objekte entsprechend klein.

Unten rechts lässt sich auch per Knopf zwischen Fenster und Vollbild wechseln. Vollbild nutzt die Bildschirmauflösung ohne Fensterrahmen. Beim Zurückwechseln wird die zuvor verwendete Fenstergröße wiederhergestellt; das Fenster kann an seinen Rändern vergrößert und verkleinert werden. Die Oberfläche passt sich an die verfügbare Breite und Höhe an. Dieser Wechsel gilt für die gebaute Spielversion; im Unity-Editor wird die Game-Ansicht weiterhin vom Editor verwaltet.

## Bearbeiten und bauen

- Kamera: `Assets/SpaceMiner/Scripts/OrbitCamera.cs`.
- Intro: `Assets/SpaceMiner/Scripts/IntroSequence.cs`; Sprechertext und Regie stehen in [docs/Dialoge/01_Intro_Erwachen.md](docs/Dialoge/01_Intro_Erwachen.md). Aktuell spielt die lokale Maya-Testaufnahme durchgehend; 17 Untertitel folgen ihren gemessenen Zeitmarken und der Audioposition. Nach Textänderungen eine passende Aufnahme und neue Zeitmarken bereitstellen. `tools/GenerateIntro.ps1` schützt die importierte Aufnahme vor Überschreiben mit der alten Hedda-Teststimme. Audio ist im Build enthalten und benötigt beim Spielen keinen Sprachdienst.
- Asteroidenpositionen und Größen werden beim Einrichten aus `Assets/SpaceMiner/BeltSettings.asset` übernommen. Danach lassen sie sich direkt in der Szene im Inspector bearbeiten und speichern.
- **Space Miner → Windows-Spiel bauen** erzeugt die Windows-Version. Vorher die Szene speichern.
- Asteroiden verwenden jetzt prozedurale Formen, vier LODs und gemischte PBR-Oberflächen. Neun Beispiel-Prefabs und drei erweiterbare Typen liegen unter `Assets/SpaceMiner/Asteroids`; Anleitung: [Asteroidengenerator](docs/AsteroidGenerator.md).
- Das Einrichten erhält eine bereits vorhandene Szene. Für eine neue Feldgenerierung die Szene bewusst umbenennen und erneut einrichten.
- Projekt über **Edit → Preferences → External Tools → External Script Editor** mit Visual Studio oder VS Code verbinden. Zum Kompilieren genügt Unity; C#-Projektdateien kann der Editor zusätzlich generieren.
- Für die Unity-Debugger-Anbindung in Visual Studio gegebenenfalls im **Visual Studio Installer → Modify** die Workload **Game development with Unity / Spieleentwicklung mit Unity** ergänzen.

## Lizenz beim ersten Start

Falls Unity meldet, dass keine aktive Editor-Lizenz vorhanden ist: Unity Hub öffnen, anmelden und **Settings → Licenses** prüfen. Bei Unity Personal aktiviert die Anmeldung normalerweise die Lizenz. Falls nötig: **Add license → Get a free personal license**, Bedingungen lesen und selbst bestätigen. Offizielle Anleitung: https://docs.unity.com/en-us/hub/manage-license.

PowerShell aus dem Projektordner:

```powershell
.\tools\Unity.ps1 Setup
.\tools\Unity.ps1 Build
.\tools\Unity.ps1 HundredBuild
.\tools\Unity.ps1 SpiralBuild
.\tools\Unity.ps1 Open
.\tools\Unity.ps1 Play
.\tools\Unity.ps1 Check
.\tools\Unity.ps1 AsteroidCheck
.\tools\Unity.ps1 Check -Visible
```

Batch-Builds bei geschlossenem Unity-Editor ausführen. Das Skript `Check` prüft im Windows-Spiel Introstart, Sprachdateien, Überspringen und automatischen Übergang, die Kamera sowie den vollständigen Tankauftrag, Wasserbilanz, Versorgung, erschöpfte Quellen und Auftragsabbruch. Screenshots und Ergebnis liegen in `Logs/`. Für die Bildkontrolle `Check -Visible` verwenden: Ein verborgenes Spielfenster kann schwarze Screenshots liefern. `Play` öffnet die normale, sichtbare Spielversion.

Das **100-Asteroiden-Testfeld** verwendet drei Materialfamilien (eisreich, felsig, metallreich), individuelle Form-Seeds und vier Detailstufen pro Körper. `HundredBuild` ergänzt das ursprüngliche Feld reproduzierbar auf 100 Körper, deaktiviert die Spirale und baut das Spiel. Die drei nahen Eisquellen bleiben erhalten; die weiteren 97 Körper verteilen sich in einer kugelförmigen Wolke in allen acht Raumoktanten bis 18 km Entfernung vom Schiff, mit mindestens 400 m Abstand zwischen ihren begrenzenden Kugeln. HundredBuild ordnet die Körper reproduzierbar neu an; normale Builds behalten die Positionen bei. Neue Quellen bleiben ungescannt. **B** berechnet die Feldansicht automatisch für die aktuelle Ausdehnung und das Fensterformat; **R** kehrt zum Schiff zurück. Oben in der Mitte stehen Objektzahl und aktuelle Bildrate.

Der **10.000-Asteroiden-Spiraltest** wird mit `SpiralBuild` oder **Space Miner → Spirale mit 10000 Asteroiden bauen** aktiviert. In der Szene sind weiterhin nur die 100 Vorlagen gespeichert. `SpiralBelt` platziert sie zur Laufzeit neu und ergänzt 9.900 auswählbare Körper mit Ressourcenstatus und passenden Mesh-Collidern. Ihre Darstellung nutzt gemeinsame Meshes und Materialien, GPU-Instancing, Kamera-Culling und vier automatische Detailstufen. Die Körper liegen auf einer Archimedes-Spirale in der X/Z-Ebene mit kleinen Höhenabweichungen und mindestens 200 m Abstand zwischen ihren begrenzenden Kugeln. Der Außenradius beträgt ungefähr 186 km, das Band hat knapp zehn Windungen. Die ersten drei Eisquellen bilden einen nahen Startabschnitt innerhalb der bisherigen Drohnenreichweite. **B** zeigt die Spirale schräg von oben; Drehen, Zoomen, Anklicken und **F** funktionieren auch am äußeren Arm. Die Anordnung ist eine Testgeometrie und keine Simulation natürlicher Umlaufbahnen. Ein normales `Build` behält den gewählten Feldmodus bei.

Sehr kleine instanzierte Körper werden in der Übersicht mit mindestens 1,8 Pixeln dargestellt. Für solche Symbole ergänzt die Auswahl einen Klickbereich von sechs Pixeln um ihren Mittelpunkt. In der Nähe gilt wieder die maßstabsgetreue Geometrie; physische Durchmesser, Collider und Ressourcen ändern sich durch die Übersichtssymbole nicht.

`Check -Visible` prüft die jeweilige Objektzahl, Detailstufen, Collider, Abstände und Kamerasichtbarkeit. Im Spiralmodus prüft es außerdem steigende Radien, die Spiralgleichung, die Darstellung aller 9.900 zusätzlichen Körper in der Übersicht und Auswahl per Raycast bis zum äußeren Arm. Eine kurze Leistungsmessung zeichnet je drei Sekunden Feldansicht, Kameradrehung, Nahansicht eines 5-km-Asteroiden und äußerer Arm in `Logs/asteroid-100-performance.json` bzw. `Logs/asteroid-10000-performance.json` auf. Darin stehen Auflösung, Grafikkarte, VSync, durchschnittliche FPS und das 95. Perzentil der Bilddauer; im Spiralmodus zusätzlich sichtbare Instanzen und gebündelte Zeichenaufrufe. Die Messung enthält Darstellung und Oberfläche bei pausierter Simulation; sie ist ein kurzer Funktionstest auf dem jeweiligen Rechner.

## Aktueller Umfang

1. Eine der Eisquellen **A-01, A-02 oder A-03** anklicken oder rechts aus der Liste auswählen.
2. **Drohne 01: Schiffstank befüllen** drücken. Die Drohne fliegt los, bremst, baut ab, kehrt zurück und bereitet das Wasser auf.
3. Drohne anklicken oder links auswählen, um Batterie, Treibwasser, Ladung, Geschwindigkeit, Entfernung und Zeitangaben zu sehen. **Tab** folgt ihr mit der Kamera.
4. Laden und Nachtanken erfolgen automatisch. Weitere Flüge füllen den Tank. Leertaste und die Knöpfe unten rechts steuern die Simulationszeit; Kamera und Anzeigen bleiben unabhängig davon bedienbar.

Der Balken über der Drohne zeigt den Fortschritt ihrer aktuellen Phase. Die Aufgabenliste zeigt Eiszuweisung, erste Lieferung und den vollen Tank. A-01 und A-03 enthalten im Test 80 % Wasser und 20 % unbekannte Bestandteile, A-02 65 % Wasser. Andere Asteroiden sind vollständig unbekannt. Wasserabbau-Erfahrung verbessert wahlweise Förderrate oder Energieeffizienz; weitere Forschung und zusätzliche Scans sind noch nicht spielbar.

Die Zahlen sind einstellbare Spielannahmen: Schiffstank 200 L mit 20 L Startbestand; Drohne 200 kg Trockenmasse, 50 kg Ladung, 10 L Treibwasser und 8 kWh Batterie. Fluggeschwindigkeit höchstens 5 m/s, Schub 5 N, angenommene Ausströmgeschwindigkeit 1.000 m/s. Beschleunigen und Bremsen kosten Treibwasser sowie elektrische Energie. Beim Nachtanken wird Wasser aus dem Schiffstank entnommen. Der gemeldete Solar-/Reaktorstrom beträgt zusammen 2,5 kW, davon nutzt die Ladestation 2 kW. Ein komplexes Stromnetz gibt es noch nicht.

Standardmäßig läuft die Simulation mit 1×; unten rechts stehen 0,5×, 1×, 2×, 3× und 5× zur Auswahl. Zeitangaben stehen in **Spielzeit**. Es gibt geradlinige Flüge mit Beschleunigungs- und Bremsphasen; Orbitalmechanik, Kollisionsvermeidung, bewegliche Asteroiden, Reparaturen, Nahrung und Trinkwasseraufbereitung folgen später. Auftragsabbruch vereinfacht das Wendemanöver, berücksichtigt aber den Treibstoff für das Abbremsen. Entladen dauert 240 Spielsekunden; Verunreinigungen werden bislang nicht als eigene Ressource verwaltet. Neue Spielstarts setzen Weltzustand und Abbauwissen zurück; Speichern ist noch nicht implementiert. Station und Drohnen verwenden einfache austauschbare Grundkörper.

Die langfristige Spielidee und fachlichen Leitlinien stehen in [docs/Spielidee.md](docs/Spielidee.md).

## Rechnerwechsel und Chatarchive

Die Anleitung für GitHub und die Fortsetzung auf einem anderen Rechner steht in [docs/Projektuebergabe.md](docs/Projektuebergabe.md). Registrierte Projektchats werden als lesbare Textarchive unter [docs/Chats](docs/Chats/README.md) gesichert. Lokale Archive lassen sich mit `python tools/ExportChatMemory.py` aktualisieren; separat abgerufene Cloud-Momentaufnahmen bleiben dabei erhalten. Die Archive dienen als Kontext für neue Chats; sie sind kein Codex-Sitzungsimport.


## Million-Asteroiden-Spiralwolke (06.10.2026)

`./tools/Unity.ps1 MillionBuild` aktiviert den Laptop-Stresstest mit exakt 1.000.000 Körpern. Fünf gewundene Arme bilden eine räumliche Wolke mit etwa 600 km Außenradius und bis zu 160 km Dicke. Die 100 Startvorlagen und die nahen Eisquellen bleiben erhalten. `SpiralBuild` stellt den bisherigen 10.000-Körper-Test wieder her; `HundredBuild` den 100-Körper-Test. Normales `Build` behält den aktiven Modus bei. Der Kamera-Zoom reicht jetzt bis 5.000 km.

Die zusätzlichen 999.900 Körper haben gespeicherte Positionen und Durchmesser. In der Ferne werden sie als farbige Punkte in 63 Mesh-Gruppen dargestellt. Innerhalb von 60 km um die Kamera werden Körper mit mindestens drei Pixeln projizierter Größe aus den vorhandenen 100 Formen als instanzierte 3D-Meshes dargestellt. Ausgewählte Körper bekommen bei Bedarf eine stabile Identität, Ressourcenanzeige und einen Collider. Die Auswahl nutzt ergänzend Bildschirmkoordinaten; die Collider-Treffergenauigkeit in großen Weltkoordinaten ist nicht bestätigt.

Dies ist ein Darstellungs- und Bedienungstest, keine Simulation einer Million aktiver Förderstellen oder beweglicher Physikkörper. Für die zufällige Wolke sind Mindestabstände und Kollisionsfreiheit nicht garantiert; der frühere 200-m-Abstandstest gilt nur für den 10.000-Körper-Modus. Die 100 Startkörper liegen im bisherigen nahen Spiralabschnitt. Die neue Wolke beginnt etwa 30 km entfernt. Neue Körper sind ungescannt. Beim Anklicken wird einmalig über die kompakten Daten gesucht; sehr große Entfernungen und viele Auswahlen benötigen bei späterem Ausbau weitere Optimierungen.

`Check -Visible` prüft im Millionenmodus Anzahl und gültige Positionen sämtlicher gespeicherter Körper, räumliche Dicke, Übersicht, Auswahl/Fokus, lokale 3D-Darstellung, Intro und Wasserauftrag. `Logs/asteroid-1000000-performance.json` enthält GPU, Auflösung, VSync, vier kurze Ansichtsproben, Generierungszeit und Speicherwerte. Punkt-Übermittlungen sind getrennt von lokalen 3D-Meshes ausgewiesen. Übermittelte Punkte sind keine gemessene Zahl tatsächlich sichtbarer Pixel. Die Messung verwendet pausierte Simulation und VSync; sie misst keine unbeschränkte maximale Bildrate.

## Settings / Configuration

Rechts oben liegen zwei Symbolknöpfe: Settings und Tier-I-Techtree. Hover zeigt Informationen, Klick heftet sie an; Escape oder × schliesst den Baum. „Wasserabbau optimieren“ besitzt jetzt Erfahrung und einen wählbaren Schwerpunkt; weitere Forschungsfelder und Forschungskosten bleiben Entwürfe. Bedienung, Architektur und Prüfschritte: [Techtree](docs/Techtree.md).

Der dezente Settings-Knopf rechts oben öffnet die persönlichen Einstellungen auch bei verborgenem HUD. Im Editor und in Development Builds funktioniert zusätzlich **F10**. **Escape / Cancel** verwirft den Entwurf; **Defaults** lädt Standardwerte in den Entwurf, **Apply Changes** speichert und aktiviert sie. Settings bleiben über Spielstarts hinweg erhalten. Die Simulation pausiert im Menü standardmäßig; das lässt sich unter Gameplay abschalten. Das Intro pausiert währenddessen samt Stimme.

Sechs Kategorien: Gameplay, Graphics, Audio, Controls, Interface, Accessibility. Developer / Debug enthält separate Live-Testwerte für Simulationsgeschwindigkeit und Abbaurate, ohne Player-Speicherung. Developer-Seite und F10 werden aus Release-Builds ausgeschlossen. Vorhandenes Build erzeugt weiterhin Development. `SpaceMiner.Editor.SettingsValidation.BuildChecks` baut ohne Szenenneugenerierung geprüfte Development-/Release-Versionen unter `Builds/SettingsDevelopment` und `Builds/SettingsRelease`; `SettingsValidation.Run` prüft Daten und Dateispeicherung. Details und Erweiterung: [docs/Settings.md](docs/Settings.md).

Erweiterung der Settings: Wiederverwendbare IMGUI-Komponenten und Farben in SpaceMinerUi, transaktionaler Entwurf in SettingsSession und zentrale Anwendung in SettingsRuntime. Grafik enthält jetzt Auflösung und Fenster/Randlos/Vollbild; Controls enthält Kamerageschwindigkeit; Accessibility enthält Untertitel in Cutscenes und Cinematics. Details und noch fehlende Spielsysteme stehen in docs/Settings.md.

Die Settings-Speicherung besitzt jetzt eine austauschbare Anbindung über IPlayerSettingsStorage/PlayerSettingsService. Standard bleibt lokale JSON-Speicherung: Apply sichert die Werte, beim Spielstart werden sie geladen. Adapterwechsel erfordert keine Menüänderung. Für Spielstände und einen späteren Server bleiben eigene Zustandsdaten, Simulationsanbindung und gegebenenfalls asynchrone Netzwerkaufrufe erforderlich; Details in docs/Settings.md.

## Einfache Raumstation

Die Versorgungsbasis ist nun eine einfache modulare Raumstation: Reaktor im Zentrum, umlaufender Zugangsring mit sechs Modulanschlüssen, Wasser-/Eistank und zwei beschädigte Solarflügel. StationVisual erzeugt die Grafik aus Grundkörpern beim Start; der bestehende Wasserauftrag bleibt erhalten. Unity-Menü Space Miner → Einfache Raumstation einsetzen und bauen setzt das Component in die bestehende Szene und baut, ohne Asteroiden neu zu erzeugen. Antrieb, Verteidigung und Modulbau sind noch nicht umgesetzt. Ältere Intro-Sprachdateien verwenden teilweise noch Schiffbezeichnungen.

## Beleuchtung und Flimmerprüfung (07.10.2026)

Die beiden Sonnenlichter verwenden feste Weltrichtungen. Himmelsobjekte auf Layer 29 und Sterne auf Layer 28 bleiben außerhalb der lokalen Beleuchtung und Schattenberechnung; ihre eigenen Shader beleuchten sie separat. Die Himmelskamera folgt nach der Menü-/Spielkamera, anschließend folgt das Sternfeld. Angepasste Schattenauflösung und Bias-Werte sowie eine zum Fokusabstand passende Near-Clip-Grenze reduzieren Schattenflimmern und konkurrierende Tiefenwerte dünner Stationsbauteile. Dieselbe Tiefeneinstellung gilt im Startmenü, Intro und Spiel; bei Drohnen-Nahansicht bleiben mindestens 5 cm Near Clip möglich.

Im Development-Player prüft `-batchmode -lightingVisualCheck` die gerenderte Beleuchtung einer unveränderten Station bei wechselnder Beobachterkamera auf zwei Qualitätsstufen, mit den echten lokalen Schattenwerfern. Eine feste Prüfkamera und gleichbleibende Schattenreichweite isolieren dabei die Beleuchtung von der Kameraperspektive. Ein separater Render-Test prüft zwei 1 cm getrennte Flächen in 140 m Abstand über 24 Positionen. Szenenbilder und Testbilder stehen relativ zum Arbeitsordner unter `Logs/Lighting`; Szenenbilder enthalten keine IMGUI-Oberfläche. Dieser Test ersetzt keine visuelle Beurteilung während freier Kamerabewegung.

## First-Person-Innenansicht (08.10.2026)

Im laufenden Spiel wechselt **V** oder der Knopf unten in der Mitte in das angeschlossene Wohnmodul am 330°-Ringanschluss. Der Raum hat etwa 8 × 10 m Grundfläche, 3 m Höhe, ein Frontfenster, geschlossene Seiten mit freien Geräteflächen und konstante Beleuchtung. Innen bewegen die konfigurierten WASD-Tasten den Spieler, die Maus steuert den Blick und Shift erhöht das Gehtempo. **Tab** gibt die Maus frei beziehungsweise fängt sie wieder ein. **V** führt zur vorherigen Außenansicht zurück. Der Wasserauftrag arbeitet währenddessen weiter. Beim Wechsel zum Startmenü werden Innenmodus und Mausfang aufgeräumt.

Mit **E** nahe der rückwärtigen Tür startet die elektrische Schleuse: erste Tür öffnen, eintreten, erste Tür schließen, angezeigter Druckausgleich, zweite Tür öffnen und weitergehen. Der Durchgang funktioniert in beide Richtungen; die Türen öffnen nie gleichzeitig und reagieren beim Schließen auf Spielerannäherung. Der angeschlossene Ring ist vollständig begehbar. Druckausgleich ist eine zeitgesteuerte Darstellung, noch keine Atmosphäre-/Gasbilanz.

Mit **E** nahe dem Pult öffnet sich die Stationssteuerung: bestätigte Eisquelle wählen, vorhandenen Wasserauftrag übernehmen, Drohne zurückrufen, Tank-/Batteriestatus lesen und Simulationsgeschwindigkeit ändern. **E / Escape** schließt das Pult; außerhalb des Pults öffnet Escape das bestehende Menü. Links steht die kleine Reparaturdrohne R-01 auf einem Ladebord; sie hat keine Arbeitslogik.

`StationInteriorMode`, `StationInteriorLayout` und `StationAirlock` ergänzen Raum, Ring, Türen und CharacterController beim Laden der bestehenden Szene. Alte massive Ringsegmente werden ersetzt, Träger enden an den neuen Korridorwänden. Es wird dieselbe Kamera wie außen verwendet, jeweils mit einem aktiven Controller. Gehen mit lokaler Schwerkraft ist eine Prototypannahme; Stationsrotation und künstliche Schwerkraft bleiben Entwurfsstand. Der Innen-/Außenwechsel ist unmittelbar, V/Tab/E sind feste Modustasten. Weitere Innenraum-Produktionssysteme sind offen.

Development-Prüfung: Player mit `-batchmode -stationInteriorCheck` starten. Sie prüft Boden-/Wandkollisionen, Menüpause, Kamerarückkehr, weiterarbeitende Drohne und Kamera-Nachführung während eines echten Wasserflugs. Ergebnis und gerenderte Szenenbilder: `Logs/Interior` relativ zum Arbeitsordner. Die Bilder enthalten die 3D-Szene; die laufende Mausbedienung und das verbleibende visuelle Drohnenflimmern werden zusätzlich am sichtbaren Player beurteilt.

Zusätzliche Development-Prüfung: `-stationHabitatCheck` prüft Pultauftrag, inaktive Reparaturdrohne, beide Schleusenrichtungen, Türverriegelung, Wiederöffnen bei Spielerannäherung und einen vollständigen physischen Rundgang im Ring. Bericht/Bilder stehen in `Logs/Habitat`. Sichtbar ausführen, um auch den Pult-Oberflächenscreenshot zu erhalten; `-batchmode` eignet sich für Logik- und 3D-Renderprüfungen.

## Bergbaudrohnen

Alle zehn Drohnen verwenden einen einfachen 2 m breiten Bergbaukörper mit Frachtbehälter, Antriebsdüsen, Greifarm und Bohrer. MiningDroneVisual erzeugt die Grafik aus Grundkörpern. Der vollständige sichtbare Ablauf umfasst Andockklammer, ausfahrenden/vibrierenden Bohrer am Collider-Kontakt, Eiskristallspray, eingesammelte Brocken durch die Frachtluke, Abdocken und Wenden sowie Entladen am oberen Tankanschluss. Ressourcenverbrauch und Förderrate kommen weiterhin aus DroneAgent. Einzelheiten, Zeiten und Grenzen: [Bergbaudrohnen](docs/Bergbaudrohnen.md). Tab fokussiert Drohne 01; rechte Maustaste dreht die Ansicht auf die Werkzeuge.


### Aktueller Dock- und Sonnensystemstand (07.10.2026)

Die neuere Station besitzt acht Ladebuchten, zwei Wartungsplaetze und eine direkte Tankkupplung mit den zugehoerigen Andockmanoevern. Lokale Aoede-Stimme und Intro-Cinematic bleiben erhalten. Beenden aus dem Spiel fuehrt zum pausierten Startbildschirm; die Anwendung wird dort ueber die eigene Beenden-Funktion geschlossen.

Das Sonnensystem verwendet nun bewusst komprimierte Spielabstaende. Heimatwelt ansehen zeigt Planet 4; Sonnensystem rahmt neun sichtbare Bahnlinien und markierte Planeten. R fuehrt zur Station zurueck. Lokale Dockinggeometrie bleibt in Metern. Werte und Grenzen: docs/Spielidee.md; Docking: docs/Bergbaudrohnen.md. Build station-playable-system-build.log und finaler Weltcheck station-playable-world-check.log erfolgreich. Rueckkehr-zum-Startbildschirm-Test station-playable-quit-check.log erfolgreich; Docking-/Kamera-/Wasserauftrag 821 Checks in station-playable-smoke.log bestanden.

Abbau-Testbalance: 80 kW Abbauleistung, 0,05 kW Bordsysteme und 0,2 kWh/L Antriebsenergie. Die Drohne beendet den Abbau vor Verbrauch der berechneten Rückkehrversorgung plus 10 % Reserve. Erste Fahrt mit A-01: knapp 29 von 50 kg Fracht. 20 produktive beendete Fahrten ergeben einen Wissenslevel, mit +15 % Förderrate oder -15 % Abbauleistung im gewählten Schwerpunkt. Testwerte stehen in Settings → Developer (Editor/Development); sie werden nicht als Player-Settings gespeichert.

`tools/Unity.ps1 MiningCheck` simuliert Tankfüllung und Levelaufstieg im aktuellen Development-Spiel. `-Visible` ergänzt Menübilder. Bericht und Tempovergleich: [Bergbaudrohnen](docs/Bergbaudrohnen.md#gemessene-abbauzeiten-06102026). Bei 5× dauert die 200-L-Tankfüllung aktuell etwa 6 h 42 min; Laden mit 2 kW macht die Testbalance sehr langsam. Der normale Tankauftrag endet vor dem ersten Wissenslevel; die Levelmessung nutzt ausschließlich im Prüflauf ein größeres Lager.

## Aktueller Einstieg und Sicherung (07.10.2026)

Verbindlicher Einstieg: docs/Projektuebergabe.md. Zwölfteilige Erinnerungssequenz mit durchgehender Narration und Musik: docs/Dialoge/IntroCinematic/README.md. Wasserabbauwissen ist inzwischen angebunden; ältere Aussagen zum reinen Techtree-Entwurf gelten nur für Hardwareforschung. Aoede bleibt die aktuelle Erwachen-Stimme, Maya ist eine archivierte Vorschau.

Bei geschlossenem Editor baut SpaceMiner.Editor.StartMenuBuild.Run die vorhandene Szene ohne Neugenerierung. Zusätzliche Player-Prüfargumente: -worldVisualCheck, -quitMenuCheck, -memoryCinematicCheck, -miningBalanceCheck -miningLogicOnly und -techTreeCheck; Logs im lokalen Logs-Ordner. Chatarchive: python tools/ExportChatMemory.py; bestehende fremde Rechnerexporte bleiben erhalten.

Der finale MP4-Export und Mix liegen unter outputs/Cinematic und sind versioniert. Reproduzierbarer Videoexport benötigt Python mit numpy, imageio-ffmpeg (oder FFMPEG als Pfad/ffmpeg im PATH), den Unity-Zwischenexport IntroCinematic-silent.mp4 und optional dessen Bildzeitdaten. Encoder-/Whisper-Caches und ältere lokale Exporte werden nicht eingecheckt. Original-WAVs, aktuelle Bilder und Metadaten stehen unter docs/Dialoge/IntroCinematic sowie Assets/SpaceMiner/Resources/MemoryCinematic.
