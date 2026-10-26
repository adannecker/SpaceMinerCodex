# Eigenständiges Konsolenstudio — 26.10.2026

**Gemeinsamer futuristischer UI-Stil —26.10.2026:** Leicht gerundete Karten/Buttons, feine Cyan-Rahmen, Amber-Auswahl, dezente Flächen und klare Fokus-/Hoverzustände über gemeinsame `SpaceMinerUi`/`UiChrome`-Bausteine. HUD, Menüs, Strom-/Drohnenkonsole und Konsolenstudio angepasst; bestehende Positionen, Klickbereiche, Daten und Skalierung erhalten. Finale drei Builds Unity6000.6.5f1;535 vorhandene numerische Playerchecks,36 Studioaufnahmen sowie Settings-/Startmenüprüfungen bestanden. Drei Nutzerraumpläne erhalten, vorige Player gesichert, normale Startversionen aktualisiert. [Gestaltung, Bild und Grenzen](Interface-Stil-2026-10-26.md), [Nachweis](Pruefungen/Interface-Stil-2026-10-26.json). Kein Commit/Push.

**Doppelsonnen und Strominterface —26.10.2026:** Beide Sterne sind jetzt physikalisch definiert:5000K/0,80R☉ und4200K/0,65R☉; am Stationsursprung451,57+155,85=607,42W/m².1800 Darstellungs-km=1Energie-AE, lokale Meter bleiben unverändert. Echte PV-Flächen, zwei Winkel, Schatten, Zustand und Netzverbindung bestimmen die Erzeugung. Neuer Reiter **SONNEN2**, gleiche Ansicht im Konsolenstudio. Akku lädt höchstens0,5C/12kW, Überschuss wird abgeregelt. Drei aktuelle Builds mit Unity6000.6.5f1;507 Strom-/Sonnenchecks,97 Stationschecks und36 Studioaufnahmen bestanden,22 analytische Checks. Vorige Player gesichert, drei Originalraumpläne erhalten. [Werte, Bedienung und Grenzen](Doppelsonnen-Energie-2026-10-26.md), [Prüfnachweis](Pruefungen/Doppelsonnen-Energie-2026-10-26.json). Frühere Angaben „Sternwerte unbekannt“/1,0+0,5L☉ beschreiben den vorherigen Stand. Weitere Verbraucher-/Reaktor-/Drohnenauslegung bleibt offen; keine Dosissimulation, kein Commit/Push.

Auf Nutzerauftrag eine eigene Anwendung für Layouttests der Konsolen angelegt. Start über `Start-Konsolenstudio.cmd`, Player unter `Builds/ConsoleStudio/ConsoleStudio.exe`. Enthalten ist zunächst die Stromversorgung. Scanner, Drohnenleitstand, Stationsplanung und Sektornetz sind noch nicht als eigene Studioseiten eingebunden.

## Bedienung

- Die Testleiste schaltet Normalbetrieb, vollen Akku, Strommangel/leeren Akku und lange Verbrauchernamen um.
- Schrift zwischen 100 und 140 Prozent umschalten; UI zwischen 75, 100 und 150 Prozent. Fenstergrössen 1280×720, 1600×1000 und 1920×1080 anwählen oder das Fenster frei vergrössern/verkleinern.
- **F1** blendet die Testleiste aus/ein. Ohne Leiste verwendet die Konsole dieselben Fensterabstände wie im Spiel.
- Erzeuger, Speicher und Verbraucher sowie einzelne Anlagen auswählen; Listen und den rechten Inspektor scrollen. **Übersicht** schliesst die Konsole zur Studioübersicht. Escape kehrt zur offenen Konsole mit Testleiste zurück.

Alle Werte und der Verlauf sind feste Testfälle, keine Auslegung oder aktuellen Spielmessungen. Die linke Stationsansicht verwendet ein ausdrücklich beschriftetes Testschema; echte Stations-/Detailmodelle sind in dieser ersten Studiofassung nicht geladen. Die Anwendung testet deren Flächenbelegung, nicht die Modellprojektion. Der Sektornetzknopf erklärt die noch fehlende Studioseite.

## Gemeinsamer Code und Integration

`PowerConsoleView.cs` enthält den aus der Spielkonsole übernommenen Renderer, Auswahl, Kategorien, Graph, Kennzahlen, Listen, Inspektor und Scrollbereiche. `PowerConsoleData` liefert Beobachtungen und Aktionen des jeweiligen Hosts.

`StationOperations.PowerConsole.cs` bindet im Hauptspiel weiterhin echte Erzeugungsdaten, gemeinsame Bank, Verbraucher, Reparatur-/Versorgungszustände und die vorhandenen `StationPowerModelView`-Kameras an. Interaktionsprüfung und Modell-Callouts bleiben im Spieladapter. Keine zweite unabhängige Kopie des Konsolenlayouts.

`ConsoleStudio.cs` hostet denselben Renderer mit Testdaten in einer leeren Szene ohne `WaterScenario` oder `StationOperations`. Keine Kampagne, Raumplanbearbeitung oder Speicherung persönlicher Einstellungen. Schrift/UI-Tests sind nur im Arbeitsspeicher. Ein eigener Unity-Produktname trennt die Engine-Fensterpräferenzen vom Hauptspiel.

## Entwicklung und Prüfung

Unity gemäss `ProjectSettings/ProjectVersion.txt`, aktuell 6000.6.5f1. Editor vor Batch-Build schliessen.

- `SpaceMiner.Editor.ConsoleStudioBuild.Build` erzeugt nur die eigene Studio-Szene `Assets/SpaceMiner/ConsoleLab/ConsoleStudio.unity` und den separaten Development-Player. Die Hauptspielszene wird nicht neu erzeugt; `Builds/Windows` wird nicht ersetzt.
- Unity-Menü **Space Miner → Konsolenstudio öffnen** öffnet die eigene Szene im bestehenden Projekt.
- Playerargument `-consoleStudioCheck` rendert vier Testfälle und verschiedene Auswahlen; Ergebnisse/Bilder unter `Logs/ConsoleStudio`. Der Test prüft auch, dass keine Kampagnensimulation vorhanden ist.
- `SpaceMiner.Editor.ConsoleStudioBuild.BuildRegression` baut die vorhandene Hauptspielszene separat unter `Builds/ConsoleStudioGameCheck`, damit die bestehende `-stationPowerConsoleCheck`-Prüfung den gemeinsamen Renderer mit echten Daten prüfen kann.

Final mit Unity6000.6.5f1: Studio und separater Hauptspiel-Prüfbuild erfolgreich gebaut. Sichtbarer Studio-Lauf Exit0, vier Testfälle und 28 Aufnahmen bei 1600×1000 und 1280×720, einschliesslich 140%-Schrift. Normalbetrieb, lange Texte und Solarinspektor visuell geprüft. Bestehende Hauptspielprüfung: 477 Checks bestanden, 14 Aufnahmen. Drei Nutzerraumpläne hashgleich. Ein erster versteckter Lauf zeichnete keine UI; nach sichtbaren Bildprüfungen die Linienzeichnung für skalierte/verschachtelte GUI-Gruppen korrigiert und final erneut gebaut/geprüft. [Nachweis und ausgewählte Bilder](Pruefungen/Konsolenstudio-2026-10-26.json).

`ConsoleStudioBuild.BuildAll` baut beide Prüfplayer nacheinander. Reguläre Hauptspielinstallation nicht ersetzt. Keine Release-, FPS-, manuelle OS-Eingabe-/Komfortabnahme oder Spielbalanceprüfung; weitere Konsolen bleiben offen. Kein Commit/Push.
