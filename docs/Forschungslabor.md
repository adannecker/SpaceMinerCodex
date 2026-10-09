# Forschungslabor im Spiel

Stand 09.10.2026. Die bestätigte kompakte Ansicht ersetzt den bisherigen Spiel-Techtree in Editor, Development und Release. Elf Chapter-1-Bereiche mit 159 Kategorieeinträgen und zentrierten realistischen Miniaturen. Die Wasseraufbereitung ist als gemeinsame Fähigkeit referenziert; vorhandene Grundlagen starten als bekannt. Forschungsmodell und Balance bleiben vorläufig. Reale Materialproduktion wird dadurch nicht eingeführt; die echte Wasserabbau-Erfahrung bleibt angebunden.

## Bedienung

Techtree über das Symbol neben Settings oder **T** öffnen/schließen. Die Taste ist in Konfiguration → Steuerung neu belegbar. Bestehende eigene Belegungen bleiben erhalten; ist T bereits belegt, erhält der neue Eintrag eine andere freie Taste. Hauptmenü, Einstellungen und laufende Storysequenzen sperren den Kurzbefehl. Links einen der elf Bereiche auswählen. Alle Bäume beginnen mit einheitlichen 72×72-Icons in UI-Einheiten und kompakten Abständen; große Bäume dürfen über die sichtbare Fläche hinausreichen. Mausrad/Zoom und zweiachsiges Ziehen verschieben die Ansicht ohne Scrollbalken. **1:1 Ansicht** setzt Zoom und Verschiebung zurück.

Rechts bleibt eine feste, bei Bedarf scrollbare Informationsleiste frei. Klick auf einen Knoten zeigt Beschreibung, Stand, Dauer, Bedingungen, Hinweise und mögliche Aktionen. Gesperrte Technologien und ihre Verbindungen sind grau, bleiben zur Erklärung ihrer Voraussetzungen anklickbar. Bekannte und aktuell erforschbare Technologien sind heller. X oder Escape leert zuerst die Auswahl; erneutes Escape schließt den Techtree. Der Baum bleibt bei ausgewählten Details bedienbar. Gelber umlaufender Rand zeigt Fortschritt; beim Wasserabbauwissen stammt er aus echten Abbaureisen. Dort zeigt die Leiste echte Erfahrung, Förderrate, Energiebedarf und die wählbaren Schwerpunkte. Ältere schwebende Klickkarten sind ersetzt.

**Testlabor** ist nur im Editor und Development-Player sichtbar. Release verwendet ausschließlich die Spielansicht. Folgende Debugfunktionen betreffen das unabhängige Forschungsmodell:

Die beiden Debug-Häkchen simulieren Daten/Proben/Praxis und geeignete Arbeitsmittel. Erst nach bekannten Vorgängern und erfüllten Bedingungen lässt sich ein Auftrag einreihen. Versorgungs-Aus stoppt alle Projekte. Warteschlange anklicken zeigt ihre Detailkarte; dort abbrechen, wobei Teilfortschritt erhalten bleibt.

Pause, 1×, 5×, 30×, 120× und 600× steuern ausschliesslich das Labor. Parallel schaltet ein bis drei Plätze als Debugannahme um, unabhängig von real installierten CPUs. Das Labor läuft mit unskalierter Zeit nur solange es geöffnet ist; die bestehende Menü-Pausenregel gilt weiterhin für die eigentliche Spielwelt.

Sichern/Laden nutzt eine separate Datei research-laboratory.json in Application.persistentDataPath. Laden pausiert das Tempo. Neustart mit Rückfrage setzt ausschliesslich den Laborstand zurück. Laborfortschritt bleibt beim Schliessen während derselben Spielsession erhalten; Sichern ist ausdrücklich manuell. Kein Eintrag im echten SaveGameStore.

## Vorläufiges Modell

Die Quellknoten stammen aus Techtree/entwuerfe-2026-10-08.json, als Laufzeitkatalog unter Assets/SpaceMiner/Resources/Research/catalog.json übernommen. Hauptpfade sind für diese Simulation Forschungsbedingungen, keine bestätigten naturwissenschaftlichen Produktionsrezepte. Weitere bekannte Bedingungen bleiben als Text sichtbar; einige zentrale zusätzliche Wissensbedingungen (CPU, Parallelrechnen, Schnellladung, Modulmontage, Glasdom) sind explizit modelliert. Daten und Arbeitsmittel fassen noch offene externe Bedingungen zusammen; keine erfundenen Materialrezepte oder Kosten.

Testdauer je Knoten: 3 + 1,5 × lokale Nummer in Minuten. Ausgewählte Analyse-/Steuerungs-/Planungs-/Optimierungsfähigkeiten haben bis zu fünf Stufen, mit Faktor 1,65 pro weiterer Stufe. Die angezeigten zehn Prozent Modellleistung pro Verbesserungsstufe sind ein vorgeschlagener Kennwert, kein Bonus auf reale Anlagen. Die vollständige 40-Stunden-Spielbalance ist damit noch nicht bestätigt.

Mira gewinnt eine Forschungsstufe je zehn abgeschlossene Projekte; Stufe 2 wird dadurch automatisch erreicht. Fünf Prozent höhere Bearbeitungsrate je zusätzlicher Mira-Stufe, maximal doppelte Rate. Diese Schwellen/Boni sind Laborwerte. Erstforschung braucht keine neu gebaute CPU. Praktische Reparatur, Hardwareinstallation, Energie-/Wärmebilanz und Rollenumbau bleiben separate Spielaufgaben.

## Prüfung und historische Entwicklung

Aktuelle Integration und Prüfungen: [Techtree und Mira-Visier](Pruefungen/Techtree-Mira-Visier-2026-10-09.json). `UnifiedPresentationCheck` prüft mit `-presentationCheck` die echte Spielansicht aller elf Bäume, Sidebar, gesperrte/erfüllte Voraussetzungen, Menü-/Story-Eingabesperren, Untertitel und alle drei Mira-Kontexte. Screenshots unter `Logs/Presentation-<Breite>x<Höhe>`, isolierte Settings und Testspielstände. `ResearchSimulationValidation.BuildGameAndDemo` baut Development, Release und die eigenständige Demo in der vorhandenen Buildkopie. Die folgenden datierten Abschnitte dokumentieren frühere Entwicklungsstände; die Bedienung oben ist maßgeblich.

SpaceMiner.Editor.ResearchSimulationValidation.Run prüft Katalog, Zyklusfreiheit, Erreichbarkeit aller Einträge, Daten-/Hardwarebedingungen, Pause/Versorgung, Abschluss, Export/Import, Verbesserungsgrenze und unabhängige Zeitschrittgrössen. Build baut die vorhandene Szene nach Builds/ResearchLaboratory (Development) und Builds/ResearchRelease (Release), ohne Szenenneugenerierung.

Der Development-Player unterstützt -researchLabCheck für Menüwechsel, Eingabesperre und Screenshots Logs/research-laboratory.png sowie research-materials.png. Tatsächliche Ergebnisse dieser Arbeit stehen im Projektmemory. Release enthält keinen Laborzugang.

Betroffene Quellen: ResearchSimulation.cs (Zustand/Engine), ResearchLaboratory.cs (Debug-UI), ResearchTreeViewport.cs (Geometrie/Zoom/Gesten), ResearchIconCatalog.cs und TechnologyIcons.cs (Piktogramme), TechTreeMenu.cs (Menüintegration), ResearchSimulationValidation.cs (Prüfung/Build), Resources/Research/catalog.json (Entwurfsdaten).


Prüfung am 09.10.2026: Unity 6000.4.7f1 Modellprüfung und Development-/Release-Build bestanden (Logs/research-laboratory-verified-build.log). Beide sichtbaren Playerprüfungen bestanden (research-verified-researchLabCheck.log und research-verified-techTreeCheck.log), Screenshots visuell kontrolliert. Der alte Techtree-Test wurde nach einem fehlgeschlagenen Versuch um die heutige Konsole-/Scanvorbereitung ergänzt; der wiederholte Test bestätigt Menüpause mit echtem Wasserauftrag. Keine vollständige neue Gameplay-/Habitat-Prüfsuite.

Direkter lokaler Start: Builds/ResearchLaboratory/SpaceMiner.exe -researchLab öffnet das Labor in einer neuen Demosession. Regulärer Zugang bleibt über das Techtree-Symbol.

Ansichtsprüfung am 09.10.2026: alle elf Gesamtbäume bei zwei Flächengrössen, Zoom-Höhenlimit, Klick/Drag-Unterscheidung und rein horizontale Verschiebung geprüft. Alle 159 Einträge besitzen eine explizite Zuordnung zu insgesamt 73 lokalen technischen Piktogrammen. Development/Release und beide sichtbaren Menüprüfungen bestanden: Logs/research-panels-final-build.log und research-panels-final-*.log. Gesamtansicht Materialien, Zoomansicht Verbindungen sowie Klickkarten visuell geprüft; abgeschnittene Beschriftungen aus der ersten Bildkontrolle korrigiert und erneut geprüft. Bildschirmaufnahmen: research-materials-overview.png, research-connections-zoom.png, research-laboratory.png und research-materials.png im lokalen Logs-Ordner.

## Eigenständige Darstellungsvorschau (09.10.2026)

`prototypes/techtree-viewer` lädt den Forschungskatalog direkt und zeigt elf einzelne Bäume mit 36 technischen Miniaturbildern, getrennten orthogonalen Andockverbindungen und Klickdetails. Start/Bedienung/Bildprovenienz: `prototypes/techtree-viewer/README.md`. Vorschau ohne Unity-Build, Forschungssimulation bleibt im bestehenden Unity-Labor. Nach Nutzerentscheidung folgen Beenden-/Speicherdialog und allgemeine Spielauflösung erst danach. Geometrie aller elf Bäume sowie Browserbedienung bei fünf Fenstergrössen geprüft; keine Unity-Prüfung in diesem Vorschau-Durchgang.

Iconrevision 09.10.2026: Vorschau nutzt jetzt 159 eigene technische SVG-Dateien in Blau mit gelben Funktionsakzenten anstelle des wiederholten Bildatlas. Generator und Manifest liegen im Vorschauprojekt. Verwandte Technologien teilen Objektfamilien, individuelle Details/Funktionszeichen unterscheiden die Karten. Vollständigkeit, keine identischen SVG-Inhalte und Palette automatisch geprüft; Detaildialog visuell geprüft. Keine Änderung am Unity-Labor.

Realistische Icons v3 (09.10.2026): Nutzer bevorzugt die räumliche Bildsprache, weniger bunt und ohne wiederverwendete Bilder. Aktive Vorschau lädt drei neue Imagegen-Atlanten mit 159 getrennten Bildbereichen (realistic-v3.json), nicht mehr die SVGs. Blau-graue Industrieobjekte und kleine gelbe Akzente, zusätzliche Entsättigung. Prompts/Provenienz im Vorschau-Assets-Ordner. 13 Node-Tests bestanden; Raster und Detailansicht visuell geprüft. Kein Unity-Eingriff.

## Startbare Unity-Techtree-Demo

Die bestätigte realistische Bildsprache der Webvorschau ist in das bestehende Forschungslabor übernommen: 159 individuell zugeordnete Miniaturbereiche, entsättigtes Blau/Grau und gelbe Fortschrittsakzente. Orthogonale Kataloglinien mit separaten Eingangs-/Ausgangsports werden aus derselben geprüften Geometrie wie im Webprojekt geladen. Zusätzliche, vom Simulationsmodell ergänzte Freischaltungsbedingungen stehen weiterhin in den Klickdetails, nicht als nachträglich kreuzende Linien im Baum.

Direkter Start nach lokalem Build: `powershell -ExecutionPolicy Bypass -File tools/Start-TechTreeDemo.ps1`. Startet `Builds/TechTreeDemo/SpaceMiner.exe -researchLab`, ohne Intro direkt im Labor. Alternativ im Editor oder Development-Player über den Techtree-Knopf und Forschungslabor. Forschung einreihen: Knoten anklicken, Daten/Probe und Arbeitsmittel im Debugoverlay bereitstellen, Vorgänger entwickeln, Auftrag einreihen; Tempo bis 600× und bis drei parallele Aufträge. Geeignete Punkte lassen sich bis Stufe 5 verbessern. Labor-Sichern/Laden betrifft ausschliesslich den separaten Laborstand.

Build bei geschlossenem Unity-Editor: Unity6000.4.7f1 mit `-batchmode -quit -projectPath <Repository> -executeMethod SpaceMiner.Editor.ResearchSimulationValidation.BuildDemo -logFile <Logdatei>`. Baut nur die Development-Demo, ohne Szene neu zu erzeugen. Importkonfiguration der Atlanten wird dabei geprüft. Reproduzierbarer Datenexport nach Katalog-/Layoutänderungen: `node prototypes/techtree-viewer/export-unity.mjs`.

Neue Quellen: ResearchMiniatures.cs, Resources/Research/miniatures.json und layouts.json, drei realistic-v3-PNGs mit .meta, Vorschauexport, Start-TechTreeDemo.ps1. ResearchLaboratory.cs und ResearchTreeViewport.cs nutzen diese Daten. Die Desktop-Korrekturen an Beenden/Spielständen und gemeinsamer UI-Skalierung werden beim Projektabgleich vom09.10.2026 mit diesem Labor zusammengeführt; neueste Prüfergebnisse stehen in der Projektübergabe.

Demoprüfung am09.10.2026: Unity6000.4.7f1 Development-Demobuild und Modell-/Ansichts-/159er-Bildprüfung bestanden (Logs/techtree-realistic-demo-build-verified.log, Exit0). Sichtbarer Playercheck ebenfalls Exit0 (techtree-realistic-demo-player-verified.log), Gesamtansicht und Klickkarte visuell kontrolliert. Atlas-Sicherheitsabstand nach Bildkontrolle angepasst und erneut geprüft. 13 Node-Tests bestanden. Kein Release-Build oder allgemeiner Spiel-Beenden-/UI-Test in diesem Durchgang.

## Kompakte 1:1-Ansicht (09.10.2026, ersetzt bisherige Einpassung)

Neue Nutzerentscheidung: alle Bäume starten mit identischer Icongrösse; Icons und Layoutabstände sind gegenüber der vorherigen Ausgangsgeometrie um25% reduziert (72×72 statt96×96 in UI-Einheiten). Keine automatische Vollbaumverkleinerung oder Breitenstreckung mehr. Manuell gewählter Zoom skaliert alle Icons gleich. 1:1 Ansicht setzt den Ausgangszustand zurück. Überstehende grosse Bäume dürfen abgeschnitten sichtbar sein und lassen sich ohne Scrollbalken in beiden Richtungen ziehen. Diese Entscheidung ersetzt die frühere Höhenbegrenzung und rein horizontale Navigation.

Bei Gruppen ab drei direkten Endpunkten mit jeweils genau einem Eingang wird ein gemeinsamer Ausgang mit horizontaler Verteilerlinie und von unten andockenden Karten verwendet. Andere echte Verzweigungen behalten getrennte Ports. Materialien enthält dadurch zwölf gebündelte Anschlüsse; gemeinsame Linienabschnitte sind absichtlich dieselbe Verbindung, keine Kreuzung unabhängiger Zweige. Canvas füllt in der Unity-Demo die verfügbare Fläche; Schrift und Bedienelemente verwenden eine separate, an der Auflösung orientierte UI-Skalierung statt der bisherigen festen1250×680-Demofläche.

Prüfung: 13 Node-Tests bestanden, inklusive erlaubter gemeinsamer Verteilerports und unabhängiger kreuzungsfreier Linien. Browser bei768×900,1280×800,1920×1080: alle elf Bäume verwenden72px-Ausgangsicons; Zweiachsen-Ziehen bei320×700 geprüft. Unity6000.4.7f1 Modell-/Geometrie-/Einheitsgrössenprüfung und Demobuild Exit0 (Logs/techtree-compact-demo-build.log). Sichtbarer Playercheck Exit0 (techtree-compact-demo-player.log), gerenderte kompakte Materialansicht bei1600×900 visuell geprüft. Keine allgemeine Beenden-Fehlerbehebung oder Releaseprüfung.


## Korrektur der Icon-Ausschnitte (09.10.2026)

Unregelmäßige Abstände in den generierten Atlanten verursachten nach oben verschobene Motive und sichtbare Fragmente der nächsten Reihe. Vorschau und ResearchMiniatures.cs verwenden jetzt gemessene Pixelrechtecke aus realistic-v3-crops.json/miniatures.json und eine zentrierte, proportionale Einpassung. Ein Hologrammtisch über zwei Rasterzellen wird als ganzes Motiv genutzt; Frachtraumumbau nutzt eine bisher unzugeordnete Frachtplattform. PNG-Originale, Katalog und Baumgeometrie bleiben erhalten.

Tatsächlich geprüft: 14 Node-Tests inklusive Ausschnittgrenzen/Überschneidungen/Unity-Exportgleichheit bestanden; Energie, Mira, Drohnen und die Werkzeuganpassung-Detailkarte im Browser visuell kontrolliert. Keine erfassten Browserwarnungen/-fehler. Lokales Prüfbild Logs/techtree-centered-2026-10-09.jpg. Unity in dieser Korrektur nicht neu gebaut oder im Player geprüft; vorhandene EXE enthält die Korrektur noch nicht.


## Separate Testanwendung mit Spielansicht (09.10.2026)

Start über `Start-TechTree-Test.cmd`, `tools/Start-TechTreeDemo.ps1` oder direkt `Builds/TechTreeDemo/SpaceMiner.exe`. Der spezielle Development-Build öffnet ohne zusätzliche Startargumente die neue Spielansicht: elf Bereiche mit Zählern, zentrierte Miniaturen, Klickdetails mit Voraussetzungen, Zoom/zweiachsiges Ziehen und umlaufender gelber Fortschrittsrand. Ein abgebrochener Auftrag behält seinen Teilfortschritt samt Rand. Escape schließt zunächst die Detailkarte, danach das Menü; über das Techtree-Symbol lässt es sich wieder öffnen.

**Testlabor** öffnet die vorhandenen Tempo-/Versorgungs-/Daten-/Arbeitsmittelregler; **Spielansicht** kehrt zum gleichen Forschungsstand zurück und setzt Tempo auf1×. In der Spielansicht sind Daten und Arbeitsmittel reine Statusanzeigen. Ohne erfüllte Bedingungen bleibt Forschung gesperrt. Zum Ausprobieren im Testlabor einen Knoten wählen, Daten und Arbeitsmittel setzen und den Auftrag einreihen; Vorgänger bleiben erforderlich. Die simulierten Forschungsbedingungen, Zeiten und Boni sind weiter vorläufig. Die normale Release-Spielübersicht und reale Produktions-/Spielstandsysteme wurden dafür nicht ersetzt.

Gleiche Unity-Komponenten wie das Forschungslabor (ResearchLaboratory/ResearchTreeViewport/ResearchMiniatures/SpaceMinerUi), mit eigener Präsentationsart statt einer Webeinbettung. BuildDemo verwendet den Produktnamen `SpaceMiner TechTree Test` und die nur für diesen Build gesetzte Definition TECHTREE_DEMO; reguläre Builds übernehmen den Direktstart nicht. Eigener Application.persistentDataPath trennt Einstellungen, Laborsicherung und eventuelle Testspielstände vom Spiel. Älterer Demoordner ist als TechTreeDemo-before-game-view-20261009-224213 bewahrt.

Atlas-Ausschnitte werden jetzt pro PNG anhand der tatsächlich leeren Zeilenabstände ermittelt. Die frühere gemeinsame Grenztabelle erfasste bei Atlas2 noch Bildreste; korrigiert und im finalen Player geprüft. Unity normalisiert die Ausschnitte anhand der Originalbildgröße, unabhängig von Importskalierung. Die PNG-Dateien bleiben unverändert.

Tatsächlich geprüft: Unity6000.6.4f1 in der vorhandenen lokalen Buildkopie, Quell-Projektversion6000.4.7f1 unverändert. Modell-/Viewportvalidierung und Development-Demobuild Exit0. Spielansichtschecks bei1600×1000 und1280×800, bisheriger Laborcheck und bisheriger Techtree-Menücheck jeweils Exit0.14 Node-Tests bestanden. Drohnenübersichten, Klickkarte und partieller gelber Rand visuell kontrolliert. Alle247 bereitgestellten Builddateien per SHA256 mit dem geprüften Build verglichen. Kein Release-Build, keine vollständige Gameplay-Regression und keine manuelle Tastatur-/Hörfreigabe. Nachweis: [Techtree-Testanwendung-2026-10-09.json](Pruefungen/Techtree-Testanwendung-2026-10-09.json).
