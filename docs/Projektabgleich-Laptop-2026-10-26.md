# Projektabgleich auf dem Laptop — 26.10.2026

## Übernommener Stand und lokale Besonderheit

Auf Nutzerauftrag den bestehenden gemeinsamen Checkout aktualisiert, Dokumentation und Themenchats abgeglichen. `origin/main` wurde frisch abgerufen und `main` per Fast-forward von `13fc488` auf **`659b514`** gebracht. Drei neue Commits: `bfedfba` (Mira/Tripo), `bba0a6b` (Drohnenhalle), `659b514` (gemeinsamer Projekt-/Rechnerwechselstand). Gegenüber dem Ausgangscommit 2.055 Dateien, 320.800 eingefügte und 549 entfernte Zeilen; viele davon Modelle, Metadaten, Gesprächsarchive und Prüfbelege, nicht ausschließlich neuer Spielcode.

Die seit 12.10. lokal vorhandene, ausdrücklich beauftragte Unity-Migration auf **6000.6.5f1 / 3ff58d469c8a** war noch nicht committed. Vor dem Update alle 16 gemeldeten Dateien als ZIP/Patch unter `Logs/Projektabgleich-2026-10-26` und zusätzlich als Git-Stash gesichert. Nach dem Fast-forward wieder angewendet; drei Dokumentationskonflikte in README, Projektmemory und Projektübergabe durch Erhalt beider Ergänzungen aufgelöst. Der Stash bleibt als Rücksicherung erhalten. Keine Spielskripte oder Modelle für den Abgleich geändert.

**Versionskonflikt geklärt:** Der geholte Remote-Commit enthält noch 6000.4.7f1 und Berichte über isolierte Builds mit 6000.6.4f1. Der hiesige Arbeitsbaum behält die bereits beschlossene 6000.6.5f1-Migration und die Versionsprüfung in `tools/Unity.ps1`. Die Remote-Berichte bleiben historische Nachweise ihrer jeweiligen Binärdatei. Der jetzt kombinierte Quellstand wurde in diesem Abgleich nicht mit Unity gebaut oder als Player gestartet. Insbesondere sind die 413 Prüfungen vom 12.10. keine Abnahme des am 26.10. geholten Codes. Keine weitere Editorinstallation oder Migration beschlossen.

## Was hinzugekommen ist

| Bereich | Tatsächlich vorhandene Umsetzung | Noch abzugrenzen |
| --- | --- | --- |
| Station | Ausgearbeitete Drohnenhalle im Ring, Keil-Lager, zweilagige Hülle, beschädigte/gesperrte Bereiche, Druckschotten, Bay-Schleuse, Außendock und detaillierte Flotte. Innenlayout aktuell Revision 15. | Physikalische Atmosphäre, Magnetführung und allgemeine Tragfähigkeitsberechnung fehlen. |
| Aufbauphase | `StationOperations` verbindet endliche Bestände, Versorgung, Aufträge, Bergung, Teilefertigung, Forschung, Reparatur und Bau. Neun aktuelle Quests; gespeicherter Kampagnenzustand. | Keine vollständig balancierte Chapter-1-Kampagne oder bestätigte 40-Stunden-Spielzeit. |
| Eis und Abbau | Volumenprofil mit 1,458 m³ Prüfbereich, 90 % Zielbelegung und erklärter Packungsannahme; A01-Beispiel rund 1.033 kg Rohgemisch. Getrennte Annahme, Brecher, Verdichtung, drei Kaltzellen, Reststofferhalt und stromabhängige Flüssigwasseraufbereitung. | Dichten, Werkzeugleistung, Förder- und Laderaten sind Auslegungsannahmen. Neue Standards ersetzen alte Save-Hardware nicht ungefragt. |
| Energie | Reaktor, Solarwinkel zur tatsächlichen Hauptsonne, gemeinsame Erzeugungsdaten für Bilanz/UI, gespeiste Verbraucher, Akkuanzeige und echte Sitzungs-Leistungshistorie. Acht Akkubuchten, bestücktes Paar bei 0° / Indizes 0 und 1 direkt gegenüber dem Zentraleneingang. | Gemeinsame 24-kWh-Bank, keine erfundenen Einzelzellwerte oder nutzbare Reserveenergie. Keine automatische Sonnennachführung. |
| Innendrohnen | Zehn I75 in eigener Fahrprobe, vier 1,5-m-Spuren, Überholen, Tür-/Raumzufahrten und gemeinsame Freigabereihenfolge für Konfliktzonen. | Fahrprobe ist kein produktiver Kampagnen-Flottenzuwachs und kein Nachweis unbegrenzter Deadlockfreiheit. Docking, Energie und Werkzeuge bleiben Sprintarbeit. |
| Bedienung | Drohnenleitstand, datenabhängige taktische Außenansicht, Handschuh, F7-Raumplaner für vier Räume mit 31 Modellgruppen, Speichern/Migration. Innen Space zum Springen, außen Space hoch / Shift+Space herunter; Pause über die Uhr. | Alte Space-Pause- und kleine-Wohnmodul-Beschreibungen gelten für Vorgängerstände. Raumpläne und persönliche Spielstände getrennt behandeln. |
| Mira | Gemeinsames Dialogmodul für Spiel/Studio, Bild/Low/High, Lippen-/Gesichts-/Körperregie; neun Questgespräche mit 22 neuen WAVs, zwei ergänzte Introzeilen. | High ist in Studio/Questspiel auswählbar bzw. gesetzt. Reguläre Hauptspiel-Vorgabe bleibt Bild; High-Hauptspielabgleich ist SP-09. |
| Modelle | Native Blender-/FBX-/GLB-/PBR-Quellen, Akkubucht mit LODs, Touchpult und bestätigte I75-R02-Oberfläche, lokale drehbare Vorschau. | Touchpult und I75-R02 sind noch nicht in die normalen Unity-Spielzuweisungen übernommen. R02-Unterseite/Texturfehler sind SP-01. |
| Forschung | Elf Kategorien / 159 Katalogeinträge und kompaktes Labor; zusätzliche echte Kampagnen-Technologie-/Auftragsbedingungen in `StationOperations`. | Labor und Kampagnenfortschritt sind getrennte Systeme. Die vollständige Baumökonomie ist nicht durch das Labor umgesetzt. |

Codeabgleich unter anderem: `StationInteriorLayout.cs`, `StationOperations*.cs`, `StationIcePlant.cs`, `DroneCargoProfile.cs`, `StationBatteryPlacement.cs`, `StationPowerHistory.cs`, `StationIndoorTraffic.Crossings.cs`, `MiraDialogueModule.cs`, `MiraQuestDialogues.cs`, `MiraQuestPreview.cs` und die bestehenden Bau-/Startmethoden. Der Abgleich ist eine gezielte Architektur- und Datenflussanalyse, kein vollständiges Review jeder der 320.800 hinzugekommenen Zeilen.

## Verbindlicher nächster Umfang

Der [Sprintplan SP-01 bis SP-10](Ideenbacklog.md#nächster-sprint-verbindlicher-umfang-und-arbeitsreihenfolge--26102026) bleibt maßgeblich: I75-Mesh/Texturen, Energieauslegung, realistische Ladung, Docks, Touchpulte, Strom-UI, Scanner, Stationsplanung/Reparatur, High-Mira und Werkzeugwechsel. Dieser Abgleich beginnt die Sprintimplementierung nicht. Bestehende Teilfunktionen weiterverwenden; fertig erst nach Integration und passender Funktions-/Sichtprüfung im tatsächlichen Spiel.

## Gemeinsamer Wissensstand und Chats

[Aktuelles Kontextupdate und Zuständigkeiten](Chats/Kontextupdate-2026-10-26.md). Bestehende lokale Themenchats werden weiterverwendet. Drei gezielte Ergänzungen trennen Stationsbau, Energieauslegung und Modellproduktion vom allgemeinen MainDev. Historische Desktop-IDs bleiben Quellen; unerreichbare Chats werden nicht als aktualisiert ausgegeben. Git überträgt Wissensdateien und lesbare Archive, keine laufenden Chatkontexte oder persönlichen Anmeldungen.

Die früheren Exportberichte vom 26.10. dokumentieren den anderen Rechner. Beim hiesigen Export bleiben dessen ausführlichere Archive erhalten, wenn lokale Quellen kürzer oder nicht geprüft sind. Keine fremden Chats, Rohsitzungen oder internen Werkzeug-/Analysetexte importieren. Das persönliche Codex-Memory ist kein gemeinsam synchronisierter Wissensspeicher.

## Prüfung dieses Abgleichs

Prüfungen, Dateiinventar und Chatbestätigungen stehen im [Abgleichbericht](Pruefungen/Projektabgleich-Laptop-2026-10-26.json). Erneut bestanden: 14 Node-Tests der Techtree-Darstellung, 25 synthetische Exporterprüfungen, Syntaxprüfung von Unity.ps1 und Transferaudit ohne Treffer der geprüften Zugangsdatenmuster/Dateigrößenblocker. 423 JSON-Dateien erfolgreich gelesen; keine fehlenden Asset-Metas oder doppelten Asset-GUIDs gefunden. Drei gemeinsame Raumpläne inhaltlich mit HEAD identisch. 13 READMEs abgeglichen, aktive Einstiegshinweise korrigiert. Die ersten Exporterprüfungen scheiterten an Sandbox-Tempberechtigungen; Wiederholung mit Tempordner unter Logs erfolgreich.

Alle elf anderen Themenchats haben den Kontext lesend bestätigt; zusammen mit MainDev zwölf aktuelle Themen. Neun historische lokale Archive um geprüfte Kontexttexte ergänzt, drei neue Initialisierungen exportiert. Neun nur als Archiv verfügbare Quellen und drei Cloud-Momentaufnahmen erhalten, ein alter Arbeitschat weiter ohne Quelle. Die 21 im Register vorhandenen Archivpräfixe blieben als normalisierter UTF-8-Text hashgleich; drei zusätzliche Ursprungsarchive unverändert erhalten. IDs, Originaltitel/Umbenennungen und Bestätigungen im Register/Bericht.

Die mitgelieferten 530 kombinierten Playerprüfungen wurden als historische Belege gelesen, hier nicht wiederholt. Kein Unity-Build, Playerlauf, Hörtest, Leistungsbenchmark oder Modell-Neuexport in diesem Auftrag. Kein Commit/Push dieses Abgleichs; lokale Migration und neue Wissensdateien sind deshalb noch nicht durch Git auf dem anderen Rechner verfügbar.
