# Stationskonsole, VR-Scan und Spielstände

Stand 08.10.2026. ScannerProgression, SaveGameStore und SaveGameMenu ergänzen das bestehende Hauptspiel.

## Einstieg und erste Aufgaben

Intro und Spiel beginnen im vorhandenen begehbaren Wohnmodul. WASD/Maus bewegt die Figur, E öffnet das Stationspult nur innerhalb von 2,4 m. Dort wird der erste Scan ausgeführt. V wechselt zwischen Station und virtueller Aussenansicht. Die Quest steht links; Intro und Questdialoge zeigen Miras vorhandene gezeichnete Porträtvariante. Das Fenster zeigt die tatsächliche Umgebung; die virtuelle Aussenansicht zeigt Asteroiden ausschliesslich nach einem Scan.

Der Scanner startet mit 97 % seines eigenen 0,1-kWh-Speichers. 1,08 kW Ladeleistung ergänzen die fehlenden 0,003 kWh in zehn Sekunden Spielzeit bei 1×. Intro, Menüs und Pause halten die Ladung an; Zeitraffer beschleunigt sie. Ein Scan verbraucht den vollen Speicher. Wiederholte Scans/neue Scanner-Tiers sind noch nicht freigeschaltet. Die Scannerleistung wird noch nicht von einem gemeinsamen Stationsenergienetz abgezogen.

Die Startreichweite beträgt nach Nutzerentscheidung fest 10 km. Die zehn nächsten Startkontakte (darunter A-01 bis A-03) liegen darin; weitere bislang innerhalb dieses Radius liegende Körper werden beim Szenariostart reproduzierbar ausserhalb platziert. Mindestens 400 m Abstand zwischen begrenzenden Kugeln und die acht Raumoktanten bleiben erhalten. Die bestehende Szene wird nicht überschrieben. Der Scan prüft tatsächliche Entfernung statt einer Trefferzahlbegrenzung. Spätere Tierreichweiten bleiben Balancingarbeit.

Der erste Scan bestätigt Ortung und grobe Form sowie Wasser bei A-01 bis A-03. Weitere Materialien bleiben unbekannt. In der VR tragen Kontakte eine einfarbige, unkartierte Oberfläche. Oberflächenkartierung und Drohnen-Livebilder sind noch nicht implementiert. Arbeitsaufträge setzen gescannte und als Wasserquelle bestätigte Kontakte voraus.

Debug in der Aussenansicht zeigt sämtliche Asteroiden und die ursprüngliche Planetendarstellung, ohne Scandaten, Wasserwissen oder Arbeitsfreigaben zu vergeben. Der Modus wird nicht als Spielfortschritt gespeichert. Die vorhandene Planetenübersicht verwendet Archivdaten; in VR werden diese Formen ohne Oberflächentexturen angezeigt. Der Nahbereichsscan entdeckt keine Planeten. Optischer Fensterblick und Intro behalten die tatsächliche Darstellung.

Mira-Dialoge begleiten den ersten Scanauftrag, das Scanergebnis mit Wasserauftrag, den Drohnenstart, die erste Lieferung und den vollen Tank. Die neuen Sätze sind zunächst Textdialoge. Zwei geänderte Intro-Cues haben bewusst keine alte, inhaltlich abweichende Sprachaufnahme; die übrigen 14 Aoede-Cues bleiben erhalten. Neue Aoede-Aufnahmen stehen aus.

## Speichern und Laden

- F5 öffnet Speichern, F9 Laden. An der Stationskonsole gibt es dafür auch Schaltflächen.
- Im Startmenü steht Spielstand laden. Demo starten beginnt einen neuen Durchlauf; ein vorhandener Durchlauf wird vorher automatisch gesichert.
- Jeder manuelle Spielstand erhält eine eigene Datei, einen frei eingegebenen Namen (80 Zeichen), einen Kommentar (2000 Zeichen) und Datum/Uhrzeit.
- Die Ladeliste zeigt ersten Scan, Wasserabbau-Level, Erfahrungsfortschritt, Tankfüllung und Lieferungen. Weitere Techtree-Knoten sind derzeit Entwürfe; sie werden nicht als erforscht behauptet.
- Autosave erfolgt alle 60 Sekunden aktiver Spielzeit in Echtzeit, beim Verlassen und vor dem Wechsel zu einem anderen Spielstand. Der vorherige Autosave bleibt als .bak erhalten und kann ebenfalls geladen werden.
- Verlassen über Escape führt zur Speicherabfrage; Anwendungsschliessen während des Spiels ebenfalls. Abbrechen lässt den aktuellen Durchlauf bestehen.

Dateien liegen unter Application.persistentDataPath/Saves. JSON-Version 1 speichert Scannerladung, Scanwissen, Quellenbestände, Wasservorrat, Lieferungen, Abbauwissen und sämtliche laufenden Drohnenphasen samt Fracht, Position, Flug-/Manöverzustand und Hardwareparametern. Debugsicht und persönliche Audio-/Grafikeinstellungen gehören nicht zum Spielstand. Neue Spielstände erhalten Innen-/Aussenmodus, Position der Figur und ein geöffnetes Pult. Alte JSON-Version-1-Spielstände bleiben lesbar; beim Laden alter Konsolenstände startet die Figur im Wohnmodul. Die Aussenkamera kehrt in ihre Standardansicht zurück.

Geschrieben wird über eine temporäre Datei mit anschliessendem atomarem Austausch. Beim Laden werden Version, Feldzugehörigkeit, Kontakt-IDs, Zahlenwerte und Drohnenziele vor der Zustandsänderung geprüft. Spielstände eines anderen Asteroidenfeldes werden abgewiesen.

## Prüfung

ScanSaveCheck (-scanSaveCheck) prüft im Windows-Player Ladung, Scan, Sichtbarkeit, Debuggrenzen, Auftragsfreigabe, Namen/Kommentare, Forschungsstand, Autosave-Sicherung und Fortsetzung mehrerer Drohnenphasen nach Laden. Die Dateien des Prüflaufs liegen isoliert im temporären Cache; normale Spielstände werden dabei nicht überschrieben.

Erster Unity-6000.4.7f1-Build nach Sandboxabsturz ausserhalb der Sandbox erfolgreich, erster Player-Lauf mit noch kalibrierter 8,84-km-Reichweite: 31 Prüfungen bestanden. Finaler Unity-6000.4.7f1-Build erfolgreich (Logs/scan-save-ready-build.log, Exit 0). Finaler sichtbarer Scan-/Spielstandtest: 36 Prüfungen bestanden, feste Reichweite 10000 m (Logs/scan-save-ready-check.log, scan-save-result.json). Prüfung umfasst auch Autosave nach 60 Sekunden, Speicherabfrage beim Anwendungsschliessen und Abbrechen. Stationskonsole mit 97-%-Anzeige/Mira-Dialog, virtuelle Kontakte sowie Speicher-/Ladedialoge visuell geprüft. Vollständiger Kamera-/Docking-/Wasserauftrag: 825 Prüfungen bestanden (scan-save-gameplay-visible.log); nach diesem Lauf nur kleine Dialog-/Speicherprüfergänzungen, keine Flug-/Abbaulogikänderung. Ein vorheriger Offscreen-Gesamtlauf scheiterte an leerer Aufnahme beim extremen Fernzoom und wird nicht als bestanden gewertet. Normales Spiel anschliessend sichtbar gestartet. Kein neuer Hörtest und keine neue Aoede-Aufnahme.

## Vorgemerkte Wolken-Seeds

Nutzeridee vom 08.10.2026: Später sollen frei wählbare Seeds eine reproduzierbare Asteroidenwolke mit massiv mehr Körpern definieren. Anzahl, Ausdehnung, Dichte, Ressourcenverteilung und sichere Startregion sind gemeinsam auszuarbeiten. Spielstände müssen Seed und Generatorversion für dieselbe Welt enthalten. Der aktuelle interne Platzierungsseed ist noch keine solche Spielerfunktion.

Aktuelle Innenraumintegration vom 08.10.2026: station-entry-verified-build.log Exit0; Scanner-/Saveprüfung39 und Habitatprüfung504 bestanden. Mira/Quest/Stationspult/VR-Bilder visuell geprüft. Vorheriger Texturimportfehler korrigiert.

## Offene Nutzerprüfung vom 08.10.2026

Trotz bestandenem automatischem Positions-Roundtrip meldet der Nutzer weiteren Bedarf bei der Innenraumposition. Als nächste Fehlerprüfung vorgemerkt: tatsächlich gespeicherte Position nach manuellem Speichern/Autosave und erneutem Laden im Wohnmodul, in der Schleuse und im Ring wiederherstellen; unerwünschten Spawn-Reset und Blickrichtung prüfen. Zusätzlich funktioniert laut Rückmeldung Beenden aus dem Spiel nicht. Den kompletten Beenden-/Speicherablauf in Innenraum, Pult und VR prüfen und reparieren. Ursachen in diesem Dokumentationsschritt nicht untersucht. Siehe Ideenbacklog.

Finale Git-Übergabe erneut geprüft: handoff-station-final-build.log Exit0, handoff-scanSaveCheck.log39 und handoff-stationHabitatCheck.log504 bestanden; zusätzliche Beleuchtungsprüfung bestanden. Siehe versionierten Bericht in Pruefungen/Stationsstart-2026-10-08.json.
