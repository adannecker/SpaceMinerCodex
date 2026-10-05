# Space Miner — Wasserprototyp

Ein 3D-Aufbau-/Survival-Spiel in einem Asteroidengürtel. Die erste Spielschleife schickt eine Drohne zu einer Eisquelle und lässt sie den Schiffstank befüllen.

## Entwicklungsumgebung

- Unity **6000.3.2f1**, auf diesem Rechner bereits installiert.
- Windows 64 Bit, Built-in Render Pipeline, C#.
- Visual Studio Community 2022 ist auf diesem Rechner installiert. Das Unity-Paket für die Visual-Studio-Anbindung ist im Projekt eingetragen; Unity lädt es beim ersten Import. Keine kostenpflichtigen Assets nötig.
- **1 Unity-Einheit = 1 Meter.** Der aktuelle Test erzeugt beim Spielstart 10.000 Asteroiden in einer Spirale um das Schiff, mit 100 m bis 5 km Durchmesser. Eine Bibliothek aus 100 individuellen Formen wird mit verschiedenen Größen und Drehungen wiederverwendet. Dazu kommen ein 24 m langes Platzhalterschiff und zehn 2 × 2 × 2 m große Drohnen. Drohne 01 ist versorgt und einsatzbereit. Drohne 02 funktioniert, wartet aber ohne Ladung und Treibstoff; acht weitere sind defekt.

## Starten

1. Unity Hub öffnen → **Projects → Add → Add project from disk** → diesen Ordner auswählen.
2. Mit Unity **6000.3.2f1** öffnen und den ersten Import abwarten. Die Prototypszene wird beim ersten Öffnen automatisch erstellt.
3. Szene `Assets/SpaceMiner/Scenes/AsteroidBelt.unity` öffnen. Falls sie noch nicht vorhanden ist: Menü **Space Miner → Prototyp einrichten**.
4. Oben auf **Play** klicken, dann in die **Game**-Ansicht klicken, damit sie Tastatureingaben erhält.

Alternativ kann das gebaute Spiel unter `Builds/Windows/SpaceMiner.exe` direkt gestartet werden.

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
| Tab | Drohne 01 fokussieren und ihr folgen |
| Leertaste | Simulation pausieren / mit 100× fortsetzen |
| F11 oder Alt + Enter | Zwischen Fenster und Vollbild wechseln, auch während des Intros |
| H | Informationsanzeige ein-/ausblenden |
| Escape | Im Intro: überspringen und ins Spiel wechseln. Im Spiel: Windows-Spiel schließen; im Editor über Play stoppen |

Die Kamera kann von **3 Metern bis 1.000 Kilometern** Abstand herauszoomen. Die Sichtweite und die Bewegungsgeschwindigkeit passen sich dem Zoomabstand an. **B** bringt dich zur Übersicht über das aktuelle Testfeld, **R** zurück zum Schiff. Bei sehr großen Abständen werden die maßstabsgetreuen Objekte entsprechend klein.

Unten rechts lässt sich auch per Knopf zwischen Fenster und Vollbild wechseln. Vollbild nutzt die Bildschirmauflösung ohne Fensterrahmen. Beim Zurückwechseln wird die zuvor verwendete Fenstergröße wiederhergestellt; das Fenster kann an seinen Rändern vergrößert und verkleinert werden. Die Oberfläche passt sich an die verfügbare Breite und Höhe an. Dieser Wechsel gilt für die gebaute Spielversion; im Unity-Editor wird die Game-Ansicht weiterhin vom Editor verwaltet.

## Bearbeiten und bauen

- Kamera: `Assets/SpaceMiner/Scripts/OrbitCamera.cs`.
- Intro: `Assets/SpaceMiner/Scripts/IntroSequence.cs`; Sprechertext und Regie stehen in [docs/Dialoge/01_Intro_Erwachen.md](docs/Dialoge/01_Intro_Erwachen.md). Nach Textänderungen die lokalen Sprachdateien mit `powershell.exe -NoProfile -File .\tools\GenerateIntro.ps1` neu erzeugen und das Spiel bauen. Die Teststimme ist Microsoft Hedda Desktop; Sprachdateien sind im Build enthalten und benötigen beim Spielen keinen Sprachdienst.
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

Das **100-Asteroiden-Testfeld** verwendet drei Materialfamilien (eisreich, felsig, metallreich), individuelle Form-Seeds und vier Detailstufen pro Körper. `HundredBuild` ergänzt das ursprüngliche Feld reproduzierbar auf 100 Körper, deaktiviert die Spirale und baut das Spiel. Die ersten zwölf Körper bleiben erhalten; die weiteren 88 verteilen sich über ungefähr 30 × 8 × 18 km, mit mindestens 400 m Abstand zwischen ihren begrenzenden Kugeln. Bereits bearbeitete Szenenobjekte werden beim erneuten Bauen beibehalten. Neue Quellen bleiben ungescannt. **B** berechnet die Feldansicht automatisch für die aktuelle Ausdehnung und das Fensterformat; **R** kehrt zum Schiff zurück. Oben in der Mitte stehen Objektzahl und aktuelle Bildrate.

Der **10.000-Asteroiden-Spiraltest** wird mit `SpiralBuild` oder **Space Miner → Spirale mit 10000 Asteroiden bauen** aktiviert. In der Szene sind weiterhin nur die 100 Vorlagen gespeichert. `SpiralBelt` platziert sie zur Laufzeit neu und ergänzt 9.900 auswählbare Körper mit Ressourcenstatus und passenden Mesh-Collidern. Ihre Darstellung nutzt gemeinsame Meshes und Materialien, GPU-Instancing, Kamera-Culling und vier automatische Detailstufen. Die Körper liegen auf einer Archimedes-Spirale in der X/Z-Ebene mit kleinen Höhenabweichungen und mindestens 200 m Abstand zwischen ihren begrenzenden Kugeln. Der Außenradius beträgt ungefähr 186 km, das Band hat knapp zehn Windungen. Die ersten drei Eisquellen bilden einen nahen Startabschnitt innerhalb der bisherigen Drohnenreichweite. **B** zeigt die Spirale schräg von oben; Drehen, Zoomen, Anklicken und **F** funktionieren auch am äußeren Arm. Die Anordnung ist eine Testgeometrie und keine Simulation natürlicher Umlaufbahnen. Ein normales `Build` behält den gewählten Feldmodus bei.

Sehr kleine instanzierte Körper werden in der Übersicht mit mindestens 1,8 Pixeln dargestellt. Für solche Symbole ergänzt die Auswahl einen Klickbereich von sechs Pixeln um ihren Mittelpunkt. In der Nähe gilt wieder die maßstabsgetreue Geometrie; physische Durchmesser, Collider und Ressourcen ändern sich durch die Übersichtssymbole nicht.

`Check -Visible` prüft die jeweilige Objektzahl, Detailstufen, Collider, Abstände und Kamerasichtbarkeit. Im Spiralmodus prüft es außerdem steigende Radien, die Spiralgleichung, die Darstellung aller 9.900 zusätzlichen Körper in der Übersicht und Auswahl per Raycast bis zum äußeren Arm. Eine kurze Leistungsmessung zeichnet je drei Sekunden Feldansicht, Kameradrehung, Nahansicht eines 5-km-Asteroiden und äußerer Arm in `Logs/asteroid-100-performance.json` bzw. `Logs/asteroid-10000-performance.json` auf. Darin stehen Auflösung, Grafikkarte, VSync, durchschnittliche FPS und das 95. Perzentil der Bilddauer; im Spiralmodus zusätzlich sichtbare Instanzen und gebündelte Zeichenaufrufe. Die Messung enthält Darstellung und Oberfläche bei pausierter Simulation; sie ist ein kurzer Funktionstest auf dem jeweiligen Rechner.

## Aktueller Umfang

1. Eine der Eisquellen **A-01, A-02 oder A-03** anklicken oder rechts aus der Liste auswählen.
2. **Drohne 01: Schiffstank befüllen** drücken. Die Drohne fliegt los, bremst, baut ab, kehrt zurück und bereitet das Wasser auf.
3. Drohne anklicken oder links auswählen, um Batterie, Treibwasser, Ladung, Geschwindigkeit, Entfernung und Zeitangaben zu sehen. **Tab** folgt ihr mit der Kamera.
4. Laden und Nachtanken erfolgen automatisch. Weitere Flüge füllen den Tank. Leertaste und die Knöpfe unten rechts steuern die Simulationszeit; Kamera und Anzeigen bleiben unabhängig davon bedienbar.

Der Balken über der Drohne zeigt den Fortschritt ihrer aktuellen Phase. Die Aufgabenliste zeigt Eiszuweisung, erste Lieferung und den vollen Tank. A-01 und A-03 enthalten im Test 80 % Wasser und 20 % unbekannte Bestandteile, A-02 65 % Wasser. Andere Asteroiden sind vollständig unbekannt. Forschung und zusätzliche Scans sind noch nicht spielbar.

Die Zahlen sind einstellbare Spielannahmen: Schiffstank 200 L mit 20 L Startbestand; Drohne 200 kg Trockenmasse, 50 kg Ladung, 10 L Treibwasser und 8 kWh Batterie. Fluggeschwindigkeit höchstens 5 m/s, Schub 5 N, angenommene Ausströmgeschwindigkeit 1.000 m/s. Beschleunigen und Bremsen kosten Treibwasser sowie elektrische Energie. Beim Nachtanken wird Wasser aus dem Schiffstank entnommen. Der gemeldete Solar-/Reaktorstrom beträgt zusammen 2,5 kW, davon nutzt die Ladestation 2 kW. Ein komplexes Stromnetz gibt es noch nicht.

Standardmäßig läuft die Simulation mit 100× Zeitraffer. Zeitangaben stehen in **Spielzeit**. Es gibt geradlinige Flüge mit Beschleunigungs- und Bremsphasen; Orbitalmechanik, Kollisionsvermeidung, bewegliche Asteroiden, Reparaturen, Nahrung und Trinkwasseraufbereitung folgen später. Auftragsabbruch vereinfacht das Wendemanöver, berücksichtigt aber den Treibstoff für das Abbremsen. Die 30 Sekunden Wasseraufbereitung sind ein Spielwert; Verunreinigungen werden bislang nicht als eigene Ressource verwaltet. Neue Spielstarts setzen den Zustand zurück; Speichern ist noch nicht implementiert. Die Asteroiden besitzen prozedurale Meshes und Materialien; Schiff und Drohnen sind weiterhin Platzhalter.

Die langfristige Spielidee und fachlichen Leitlinien stehen in [docs/Spielidee.md](docs/Spielidee.md).

## Rechnerwechsel und Chatarchive

Die Anleitung für GitHub und die Fortsetzung auf einem anderen Rechner steht in [docs/Projektuebergabe.md](docs/Projektuebergabe.md). Die drei Projektchats werden als lesbare Textarchive unter [docs/Chats](docs/Chats/README.md) gesichert. Auf dem ursprünglichen Rechner lassen sie sich mit `.\tools\ExportProjectChats.ps1` aktualisieren. Die Archive dienen als Kontext für neue Chats; sie sind kein Codex-Sitzungsimport.

