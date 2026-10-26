# Adaptive Innenbeleuchtung — 26.10.2026

Nutzerauftrag: angenehmere Beleuchtung, Arbeitslicht beim Betreten eines Raums, sonst gedimmt, mit geringerem Strombedarf. Der Nutzer hat vor dem Neustart bestätigt, dass seine Raumplatzierungen gespeichert sind.

## Verhalten

Der tatsächliche Innenraum-Charakter bestimmt den beleuchteten Aufenthaltsbereich. Zentrale, Werkstatt, Hangar, äußere Module, innere Bereiche und einzelne Ringsektoren werden über die bestehende radiale Stationsgeometrie unterschieden. Eine Vorschaukamera beziehungsweise das Zoomen in der Außenansicht aktiviert keine fremden Räume. Es handelt sich um feste geometrische Bereiche, nicht um eine neue automatische Raumerkennung für frei umgebaute Wände oder eine Drohnen-Belegungserkennung.

Besuchter Bereich fährt in ungefähr0,6 Sekunden auf Arbeitslicht. Nach Verlassen bleiben1,5 Sekunden Nachlauf, danach dimmt er in etwa1,6 Sekunden auf12Prozent. Leere Bereiche behalten Orientierungslicht; außerhalb der Station bleibt kein Bereich durch die Kamera belegt. Leuchtflächen dimmen optisch mit. Bestehende Netzverbindungen und Versorgung gelten weiterhin; getrennte Räume erhalten keine nutzbare neue Energiequelle. Vorhandenes Helm-/Notlicht bleibt bestehen.

Vier zusätzliche Leuchten verteilen warmes Licht über die Zentrale. Schwächeres globales Innen-Fülllicht vermeidet die vorherige gleichmäßig helle, flache Darstellung. Arbeitslicht nach Bildkontrolle aufgehellt; nur eine nahe aktive Leuchte wirft weiche Schatten, höchstens vier aktive Pixellichter und zwölf sichtbare lokale Lichtquellen. Dekorative Leuchtflächen werfen keine Schatten auf ihre innenliegende Punktlichtquelle. Entfernte Lichtquellen werden zur Darstellung begrenzt; diese Kamerabegrenzung erzeugt keine fiktive zusätzliche Stromersparnis.

## Strombilanz: ausdrücklich vorläufiger Spielwert

Die bisherigen pauschalen180W Stationssystembedarf werden für den Prototyp in90W sonstige Systeme und ein stationweites90W-Lichtbudget bei Vollbeleuchtung aufgeteilt. Kein zusätzlicher Erzeuger oder höhere Nennleistung. Der aktuelle Lichtanteil ergibt sich aus den tatsächlichen Helligkeitsstufen der netzverbundenen Leuchten. Diese Aufteilung ist eine Balancingannahme, keine technisch belegte Leistungs-/Luxauslegung und kein vom Energiechat abschließend festgelegter Lampenkatalog.

`LightingDemandKw` geht in denselben Versorgungsschritt ein wie die übrigen Verbraucher; die Stromoberfläche nennt **Adaptive Innenbeleuchtung** getrennt von Stationssystemen. Änderungen während Pause aktualisieren die Anzeige, ohne Simulationszeit, Energie oder Arbeit fortzuschreiben. Das Dimmen spart im Spiel damit tatsächlich angeforderte Leistung. Die15 Prüfungen vergleichen unter anderem besetzte Zentrale mit leerer Station; konkrete Werte gehören zum Prüfbericht, nicht als allgemeine Stationsmessung in eine Beschreibung.

## Dateien und Prüfung

Neue Logik: `StationOperations.Lighting.cs`; bestehende Geometrie, Updateablauf, Strombilanz und Telemetrie in `StationOperations.Geometry.cs`, `.Ui.cs`, `StationOperations.cs`, `.PowerTelemetry.cs`. Eigener opt-in-Test `AdaptiveLightingCheck.cs`, isolierte Settings/Saves/Pläne und `AdaptiveLightingBuild.cs`. Keine Änderung der Unity-Version6000.6.5f1, Raumpläne, Pultkennungen, Saveformate oder Sprachdateien.

Erster Testlauf scheiterte, weil der neue Checkflag durch die bestehende Live-Simulationsschutzliste ausgeschlossen war. Freigabe gezielt ergänzt und tatsächlichen aktiven Betrieb erneut geprüft. Erste Bildkontrolle war noch zu dunkel; Arbeitslicht danach gezielt aufgehellt und Selbstabschattung der Leuchtflächen verhindert. Ein späterer Ausschaltcheck zeigte eine verzögerte sichtbare Aktualisierung; TogglePower aktualisiert nun synchron. Ein Unity-Batchstart endete vor dem Import mit1; unveränderter Neustart erfolgreich.

Abschliessender Development-Build mit Unity6000.6.5f1 Exit0 (`Logs/adaptive-lighting-verified-retry-build.log`).16 adaptive Lichtprüfungen und477 bestehende Strom-/Speicherprüfungen in zwei finalen Läufen Exit0, insgesamt493. Prüfung umfasst besetzte und leere Räume, weiche Übergänge, zweiten Raum, Vorschaukamera ohne Belegung, Pausen ohne Energieverbrauch, eigenen Lichtverbraucher, Summenbilanz und sofortige Stromabschaltung. Im isolierten Ausgangszustand angeforderte Lichtleistung rund24W bei besetzter Zentrale gegenüber9,4W bei leerer Station; keine allgemeine technische Stationsmessung. Hauptpult-/Werkstattbilder kontrolliert, finale aufgehellte Zentrale erneut visuell geprüft. [Prüfbericht](Pruefungen/Adaptive-Innenbeleuchtung-2026-10-26.json).

Geprüfter Player nach Builds/Windows übernommen, alle247 Dateien SHA256-gleich. Drei gespeicherte Nutzerraumpläne vor/nach Übernahme hashgleich. Vorgänger unter Builds/Windows-before-adaptive-lighting-20261026 bewahrt; Spiel nach ausdrücklicher Nutzerbestätigung zum Speichern/Neustart wieder sichtbar gestartet. Keine Lux-/Lumen-/physikalische Energieauslegung, FPS-, Headset-, Release- oder natürliche Gesamtkampagnenabnahme. Kein Commit/Push.
