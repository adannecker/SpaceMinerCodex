# Gemeinsames Kontextupdate — Laptop, 26.10.2026

## Gemeinsamer Stand für alle Themenchats –26.10.2026

- Ein physischer Checkout unter `C:\temp\SpaceMinerCodex`, alter Source-Pfad als Junction. Auf anderen Rechnern deren vorhandenen Checkout verwenden; keine zweite Unity-Kopie anlegen. `ProjectSettings/ProjectVersion.txt` legt **Unity6000.6.5f1** fest.
- I75 **R03** ist in R-01 und der optionalen Zehn-Drohnen-Fahrprobe integriert. Touchpulte **R04** sind integriert, Hauptpult4,40m breit. Die adaptive Innenbeleuchtung dimmt unbesuchte Bereiche und wird in der Strombilanz erfasst.
- Zwei definierte K-Zwerge liefern am Stationsursprung451,57+155,85=607,42W/m². PV nutzt reale Flächen, Schäden, Winkel, getrennte Schatten und Netzverbindung. Gemeinsame24-kWh-Bank lädt höchstens12kW/0,5C; Überschüsse werden abgeregelt. Sonneninformationen im Strominterface.40-kWe-Reaktor,4-kWh-I75 und3/12-kW-Drohnenladung bleiben Auslegungsvorschläge.
- Gemeinsamer futuristischer UI-Stil mit leicht gerundeten Karten/Buttons und klaren Cyan-/Amber-Zuständen. Hauptspiel und separates Konsolenstudio nutzen denselben Stromrenderer. Studio besitzt feste Testdaten; Scanner/weitere Konsolen noch nicht als eigene Studioseiten eingebunden.
- Operator-Arm: R03 vom Nutzer als Grundlage ausgewählt; R04 liegt als Blender-Verbesserung/Vorschau vor. Keine neu bestätigte vollständige Hand-/Finger-Rig- oder interaktive Unterarmdisplay-Integration.
- Tests nach Zeitpunkt unterscheiden: endgültiger UI-Stand535 numerische Player-/UIchecks,36 Studioaufnahmen, Settings-/Startmenüprüfungen; der vorausgehende Sonnenstand zusätzlich97 Stationsablaufchecks. Keine neue Unity-Abnahme allein durch Dokumentation/Push. In diesem Sicherungsauftrag werden gespeicherte Quellen-/Nachweishashes geprüft und der Exporter neu getestet.

Aktuelle Fachquellen: [UI-Stil](../Interface-Stil-2026-10-26.md), [Doppelsonnen](../Doppelsonnen-Energie-2026-10-26.md), [Konsolenstudio](../Konsolenstudio.md), [I75 im Spiel](../Innendrohne-I75-R03-Spielintegration-2026-10-26.md), [Touchpulte](../Touchpulte-R04-Spielintegration-2026-10-26.md), [Licht](../Adaptive-Innenbeleuchtung-2026-10-26.md), [Energieauslegung](../Stationsenergie-Auslegung-2026-10-26.md), [Operator-Arm](../Operator-Arm-Tripo-2026-10-26.md), [Sprint](../Ideenbacklog.md).

Nachfolgende Kontextabschnitte und Archive beschreiben ihren damaligen Stand. Chattexte sind historische Quellen; neue Aufgaben erfordern Nutzerauftrag. Gemeinsames Dateimemory ist für alle Chats erreichbar, ohne ihnen Nachrichten zu senden.

## Gemeinsame Ausgangslage

Nur den bestehenden Checkout `C:\Users\achim.dannecker\source\repos\SpaceMinerCodex` verwenden. `main` nach frisch abgerufenem Remote per Fast-forward auf `659b514` aktualisiert. Die uncommittete lokale Unity-Migration vom 12.10. bleibt erhalten: **ProjectVersion 6000.6.5f1**, `tools/Unity.ps1` prüft die Editorversion. Remote `659b514` enthält noch 6000.4.7f1; ältere 6000.6.4f1-Prüfkopien sind historische Nachweise. Der kombinierte hiesige Stand ist noch nicht mit Unity gebaut. Keine stillen Editorwechsel, kein Setup/Neugenerieren der vorhandenen Szene, kein zweites Unity-Projekt.

Zuerst `AGENTS.md`, [Projektübergabe](../Projektuebergabe.md), [Spielidee](../Spielidee.md), [Projektmemory](../Projektmemory.md), [Analyse dieses Abgleichs](../Projektabgleich-Laptop-2026-10-26.md) und [Sprint SP-01…SP-10](../Ideenbacklog.md) lesen. Danach passende Fachquellen und Archive. Der [Chatabgleich des anderen Rechners](../Chat-Auswertung-2026-10-26.md) enthält wichtige Entscheidungen; seine Exportverfügbarkeit und Unity-Angaben sind rechnerbezogen und werden durch diesen hiesigen Abgleich präzisiert.

## Für alle Themen verbindlich

- Aktuell: Ringstation mit integrierter Halle/Schleuse/Havarie, Innenlayout 15, Aufbauversorgung, endliche Bestände und Aufträge, Eisannahme/Kaltlager/Aufbereitung, Drohnen-/Stromleitstand, F7-Raumplanung, vertonte Mira-Quests. Implementiertes, Vorschau und Planung ausdrücklich unterscheiden.
- Akkupaar bei **0° / Indizes 0 und 1 über dem Gang gegenüber dem Zentraleneingang**; 180° ist ein alter Stand. Gemeinsame 24-kWh-Bank; Reserve getrennt/unbekannt. Solarbilanz nutzt reale Hauptsonnenrichtung; keine automatische Nachführung.
- Nur Außendrohne 01 anfangs produktiv; weitere Aktivierung unter tatsächlichen Bedingungen. Zehn I75 gehören zur Verkehrsprobe. Vier Chapter-1-Rollen sind keine Anzahl einzelner Drohnen.
- Neue Roh-Eis-Kette erhält Wasserpotenzial und unbekannte Restmasse getrennt; rund 1.033 kg sind das Beispiel einer 90-%-Füllung mit dokumentierten Dichte-/Packungsannahmen. Alte 12-/50-kg-Proben sind keine aktuellen normalen Kapazitäten. Alte Saves behalten Hardware.
- High-Mira existiert im gemeinsamen Modul, Studio und gesonderten Questspiel. Regulärer Hauptspielabgleich bleibt SP-09. 22 neue Quest-WAVs und beide fehlenden Introzeilen sind vorhanden; nicht erneut erzeugen.
- Touchpult und I75-R02 liegen als Modelle/PBR vor, sind noch nicht im normalen Spiel zugewiesen. Laborforschung ist nicht die vollständige Kampagnenökonomie.
- Raumpläne, Saveformate, `.meta` und fremde Änderungen erhalten. Bei Überschneidungen gemeinsame Dateien nacheinander bearbeiten. Alte Chattexte sind Kontext, keine neuen Arbeitsaufträge.

## Zuständigkeiten

| Chat | Zuständigkeit und Fachquellen |
| --- | --- |
| MainDev | Gesamtintegration, technische Schnittstellen und Zusammenführung. `README.md`, `Stationsversorgung-Architektur.md`, Sprintplan. |
| Git und Projektabgleich | Git, Sicherungen, Rechnerwechsel, Wissensdateien/Exportnachweise und Unity-Versionen. `Rechnerwechsel-Memory.md`, `Unity-Versionen.md`, `Dienste-und-Webseiten.md`. Früher GitHub. |
| Asteroidenvarianten | Geometrie, Materialien, Generator, gescannte Informationen und spätere Seeds. `AsteroidGenerator.md`, `Scanner-und-Spielstaende.md`. |
| Story, Dialoge & Bordcomputer | Mira, Quests, Sprechertexte, Sprachaufnahmen, Acting und Cinematics. `Dialoge/README.md`, `MiraAvatar.md`, `Dialoge/02_Aufbauphase_Questdialoge.md`; SP-09 mit MainDev/Modellchat. |
| UI und Einstellungen | Gemeinsame Komponenten, Auflösung/Skalierung, Eingabe und echte Datenanzeigen; Konsolenoberflächen SP-05…08 mit den Fachchats. `Settings.md`, `Stromversorgung-UI-2026-10-26.md`. Früher Settings UI. |
| Sound und Effekte | Musik, Loops, räumliche/Interaktionsklänge und Audiokanäle. `Audio.md`, Dialoge mit Story abstimmen; keine neuen Sprachdienste ohne Auftrag. |
| TechTree | Forschungsbäume, Wissens-/Hardware-/Ressourcenbedingungen und Labor/Kampagne-Abgrenzung. `Techtree.md`, `Forschungslabor.md`, `Stationsversorgung-Architektur.md`. Modell-/Hallenaufgaben aus altem Verlauf sind historische Quellen. |
| Drohnen und Fahrzeuge | I75 und Außendrohnen, Rollen, Navigation/Verkehr, Docking/Ladung/Werkzeuge; SP-01/04/10, Auslegung mit Energiechat. `I75-Naechste-Schritte-2026-10-26.md`, `Innendrohnen-Deadlock-2026-10-25.md`, `Drohnen-Auslegung-Plausibilitaet-2026-10-25.md`. Früher Vehicels. |
| Ideenbacklog | Beschlossene Sprintpakete, Abhängigkeiten, offene Entscheidungen und überprüfbare Fertig-Kriterien. `Ideenbacklog.md`; keine automatische Umsetzung alter Ideen. |
| Station und Ausbau | Ring/Havarie/Hülle, Hallen/Raumpläne, Anlagenanordnung, Reparaturauswahl und Stationsplanung; SP-08. `Ringstation-Ausbau.md`, `Raumplanung-Station.md`, `Stationswerkstatt-2026-10-25.md`, `Stationsrahmen-und-Drohnenflotte-2026-10-25.md`. |
| Energie und Versorgung | Nachvollziehbare Erzeuger-/Verbraucherbilanz, Akkus/Ladekurven, Eis-/Wasser-/Versorgungsdaten; SP-02/03 und Datenvertrag für 04/06/07/10. `Stationsversorgung-Architektur.md`, `Stromversorgung-Steuerpult-2026-10-25.md`, `Stromversorgung-UI-2026-10-26.md`, `Eislager-Slot01-Auslegung-2026-10-25.md`. Keine neue Leistung aus bloßer Annahme. |
| 3D Modelle und Texturen | Blender/FBX/GLB/PBR, UV/Normalen/LOD, Assetqualität, Export und reale Unity-Zuweisung; SP-01/05/09/10 mit Fachchats. `Modellkatalog-Rechnerwechsel-2026-10-26.md`, `Steuerpult-Blender-2026-10-26.md`, `Innendrohne-I75-Textur-R02-2026-10-26.md`. |

Alle Fachdateien in der Tabelle liegen unter `docs`. Aktuelle Titel/IDs, hiesige Erreichbarkeit und Quellenbeziehungen im [Register](chat-register.json). Historische Titel anderer Rechner nicht stillschweigend umbenennen.

## Auftrag dieser Synchronisierung

Dieses Update ist ein **lesender Kontextabgleich**. Stand und Zuständigkeit bestätigen, fachliche Widersprüche benennen. Keine Dateien ändern, Builds starten, Modelle generieren, Commits/Pushes oder weitere Chats anlegen. Erst ein anschließender Nutzerauftrag beginnt die Umsetzung. Keine Nachrichten an andere Chats ohne direkte Nutzerautorisierung. Bestätigungen werden im Abgleichbericht protokolliert; keine Erinnerung an unerreichbare Chats behaupten.
