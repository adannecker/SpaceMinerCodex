# Innendrohnen: Blockaden an Wechselzonen

Nutzermeldung vom 25.10.2026: mehrere I75 stehen im Ringgang, eine Drohne quer im Fahrweg. [Originalbild](Art/Station/Innendrohnen-Deadlock-Nutzermeldung-2026-10-25.png). Build14 hatte lokale Abstands-/Ausweichregeln ohne zentrale Vergabe der Konfliktbereiche. Sein bestandener Ein-Runden-Lauf war kein ausreichender Nachweis für dauerhaften Verkehrsfluss. Das genaue zeitliche Entstehen der abgebildeten Situation wurde nicht aufgezeichnet.

## Korrektur und Regeln

`StationIndoorTraffic.Crossings.cs` ergänzt eine gemeinsame FIFO-Reihenfolge für Durchfahrt, Raumzufahrt und Wenden an den sieben vorhandenen Wechselzonen. Jede Drohne fordert rechtzeitig eine Freigabe an. Erst wenn der Konfliktbereich und der Abfahrtspunkt frei sind, wird eine einzelne Durchfahrt freigegeben. Andere Drohnen warten davor. Die Reservierung bleibt während des Querens und Einfädelns bestehen und endet erst nach dem Verlassen des Bereichs. Es gibt kein zeitgesteuertes Freigeben einer noch besetzten Kreuzung und kein Zurücksetzen oder Teleportieren festgefahrener Fahrzeuge im Spielbetrieb.

Die Freigabe ist konservativ: jeweils eine Drohne im gesamten Konfliktbereich, auch bei reinen Durchfahrten. FIFO behandelt Raumverkehr und durchfahrende Fahrzeuge gemeinsam. Jede Drohne hält höchstens eine Zonenreservierung. Zwischen den Wechselzonen bleiben beide Richtungspaare mit Überholspur bestehen. Innerhalb der Reservierung beginnt kein neuer Überholvorgang. Eine schon überholende Drohne behält beim Warten ihre Spur; der erzwungene Rückwechsel vor einer Wende beginnt erst mit Freigabe. Damit schiebt sich eine wartende Überholerin nicht vorzeitig seitlich vor den bereits berechtigten Verkehr. Spielerabstand, Menüpause, echte Schottsteuerung und feste Hindernisse bleiben berücksichtigt.

Anforderung bei bis zu 8° Abstand, Haltelinie 5,5° vor dem Zonenmittelpunkt, exklusiver Konfliktbereich bis 4,5° beiderseits bei Radien 29,5 bis 38 m. Das umfasst die markierte Zone von ±3,5° samt Abstand zu Ein-/Ausfahrten. Maße sind geometrische Prototypwerte. Die Bodenmarkierung bleibt erhalten.

Dateien: `StationIndoorTraffic.cs` und neue Teilklasse `StationIndoorTraffic.Crossings.cs` samt `.meta`; erweiterte Abnahme in `StationIndoorTrafficCheck.cs`. `Diagnostic()` protokolliert bei einem Prüfungsfehler Positionen, Ziele, Wartetickets und Reservierungen.

## Tatsächlich geprüft und ausgeliefert

Aktuell **indoor-traffic-build-16**, Development, unter `Builds/Windows`. Start über **`Start-Innendrohnen-Fahrprobe.cmd`**. Der vorherige Build14 bleibt vollständig in `Builds/Windows-before-indoor-deadlock-20261025` erhalten.

Der finale grafische Player-Lauf besteht **905 Checks / Exit 0**, vier native Spielbilder gesichtet. Mindestens drei vollständige Routenrunden je Drohne, **334 abgeschlossene Zonenfreigaben**, **zehn Überholvorgänge**, 9.744 Positionssamples mit 46.958 einzelnen geprüften Spurpositionen außerhalb der markierten Wechselzonen. Kleinster gemessener Mittelpunktabstand **0,950 m**. Alle 120 simulierten Sekunden muss jede einzelne Drohne mehr als zwei Meter Fortschritt zeigen; ein nur teilweise weiterfahrender Verkehr gilt nicht als bestanden. Bodenunterstützung, feste Geometrie, Richtungspaare, gegenseitiger Abstand, Pause, Batteriestandorte und die gezielten echten Straßen-/Schott-Durchfahrten sind eingeschlossen.

Zusätzlich gezielt aufgebaut: I-08 bei 53° auf der Fahrspur, schnellere I-09 bei 52,2° auf der Überholspur. Beide müssen die Wende bei 60° innerhalb von 60 simulierten Sekunden verlassen. Bestanden. Nur diese Testaufstellung versetzt zwei Akteure vor dem Start; im Spielbetrieb gibt es keine solchen Versetzungen. Der konstruierte Konflikt ist kein aufgezeichnetes Replay des Nutzerscreenshots. Der anschließende Mehr-Runden-Lauf verwendet isolierte Kopien aller drei tatsächlichen Nutzerraumpläne. Die Originalpläne bleiben per SHA256 unverändert.

Laufdauer rund **797 simulierte Sekunden** bei Spielrate 1× und `Time.timeScale=6` zur Beschleunigung. Daraus folgt keine Abnahme normaler FPS oder linearer Fahrzeiten bei anderen Spielraten. Mehrere bestandene Runden beweisen keine unbegrenzte Deadlockfreiheit; Spieler und gesperrte Wege dürfen weiterhin Warten verursachen.

Zwischenbuild15 mit erster Reservierungssteuerung scheiterte im verlängerten Lauf an fehlendem individuellem Fortschritt von I-01 an Zone30°. Dieser Versuch gilt ausdrücklich nicht als bestanden; danach wurde der vorzeitige Rückwechsel eingeschränkt und der gezielte Überholkonflikt ergänzt. [Fehlerprotokoll](Pruefungen/Innendrohnen-Deadlock-2026-10-25/failed-build15/error.txt). Der alte vollständige Werkstattcheck mit seiner veralteten Forderung nach Layoutrevision14 wurde hier nicht erneut ausgeführt; nur die relevanten realen Straßenprüfungen sind Teil des bestandenen Laufs.

[Prüfnachweis](Pruefungen/Innendrohnen-Deadlock-2026-10-25.json), [Bilder, Logs und Quellenmanifest](Pruefungen/Innendrohnen-Deadlock-2026-10-25/). **247 ausgelieferte Builddateien SHA256-abgeglichen**; 377 Dateien im Quellsnapshot, 368 identisch zum aktuellen Arbeitsstand. Sieben parallele Mira-Dateien bleiben erhalten; der Build nutzt dort den älteren Snapshot. Zwei ältere Editor-Hilfsdateien nur in der Buildkopie sind gesondert dokumentiert und werden nicht als Playercode ausgeliefert.

Quelle Unity6000.4.7f1, Buildkopie6000.6.4f1, Layout15. Unverändert: vier1,5-m-Spuren, I75-Maße, zehn reine Probefahrzeuge und 24-kWh-Batteriebilanz. Keine physikalische Magnetregelung, keine neue Headset-/FPS-/native Eingabeabnahme, kein Commit/Push.
