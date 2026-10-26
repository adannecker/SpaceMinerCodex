# Raumplanung für vier Stationsräume — 25.10.2026

**Ergänzung Strompult, 25.10.2026:** Der ehemalige Akku-Platzhalter in der Kommandozentrale ist jetzt das **STROMVERSORGUNG**-Pult. Die acht Kommandoboxen und alle 31 Raumgruppen bleiben erhalten. Alte Plan-ID `battery`, gespeicherte Maße, Position und Drehung bleiben gültig; die echte Bank steht gegenüber der Zentrale im Ring. [Bedienung, Architektur und aktueller Prüfstatus](Stromversorgung-Steuerpult-2026-10-25.md). Die Prüfstände am Ende dieser Datei beschreiben ihre jeweiligen Vorgängerbuilds.

## Umfang

**F7** öffnet einen gemeinsamen Raumplaner. Die Reiter oben wählen zwischen **Kommandozentrale**, **Drohnenbay**, **Drohnenwerkstatt** und **Interner Drohnenwerkstatt**. Jeder Raum besitzt einen eigenen Grundriss, eigene Elemente und eine eigene gespeicherte Entwurfsdatei.

Die Kommandozentrale behält ihre beschrifteten Planungsboxen. In den drei weiteren Räumen werden die vorhandenen ausgearbeiteten Modelle als zusammengehörige Raumgruppen bearbeitet. Der Grundriss ist deren Bearbeitungsansicht; akzeptierte Änderungen bewegen, drehen und skalieren die tatsächlich gebundenen Modelle einschließlich ihrer untergeordneten Collider.

Werkbankkorrektur25.10.2026: Die importierte Tischlänge wird vor dem Einpassen auf die Planbreite ausgerichtet; die beiden Schubladenblöcke stehen nebeneinander und ihre Fronten zeigen zur markierten Vorderkante. Gespeicherte Raumpläne werden dafür nicht umgeschrieben. Die zwei ursprünglichen Serviceschränke bleiben statische Hallenausstattung außerhalb des sieben Elemente umfassenden Werkstattkatalogs; sie besitzen bislang keine eigene Spielmechanik.

| Raum | Elemente | Bearbeitete Gruppen |
| --- | ---: | --- |
| Kommandozentrale | 8 | Steuerpult, Drohnenverwaltung, Stationsplanung, Techtree, Fertigung, Materialdrucker, Nahrungsanlage und Stromversorgungspult |
| Drohnenbay | 10 | Die zehn vorhandenen Drohnenplätze mit ihren realen Dockgruppen |
| Drohnenwerkstatt | 7 | Zwei Umbauplätze, Werkzeugregal, Schrottanalyse, Schrottbehälter und zwei Werkbänke |
| Interne Drohnenwerkstatt | 6 | R-01-Ladeplatz, interne Reparaturbank, defekte Anlage sowie Behälter für C, Eisen und unbekanntes Material |

Insgesamt sind **31 vorhandene Gruppen** vorgesehen. Die Werkstatt bleibt bei zwei Umbauplätzen, davon zunächst einer betriebsbereit. Die Raumplanung schaltet weder den zweiten Umbauplatz noch zusätzliche Drohnen frei. Eine Größenänderung verändert den geometrischen Entwurf, nicht automatisch Lagerkapazität, Ladeleistung, Arbeitsgeschwindigkeit oder Forschungsstand.

Alle zehn Bay-Plätze liegen auf **demselben Deck**. Die beiden radial versetzten Reihen haben im Ausgangsmodell dieselbe Dockwurzelhöhe von 1 m über dem Deck (`BerthHeight=1`); sie bilden keine zwei übereinanderliegenden Ebenen.

## Bedienung

| Aktion | Bedienung |
| --- | --- |
| Raum wählen | Reiter oben anklicken |
| Element wählen | Im Grundriss oder in der Liste links anklicken |
| Verschieben | Linke Maustaste auf der Grundfläche halten und ziehen |
| Breite und Tiefe ändern | Eine gelbe Ecke ziehen; die gegenüberliegende Ecke bleibt stehen |
| Frei drehen | Den grünen Ring **Drehen** an der markierten Vorderkante ziehen; Mittelpunkt und Größe bleiben unverändert |
| Ausrichten | Während des Verschiebens, Skalierens oder Drehens **Shift** halten |
| Exakte Werte | Rechts X/Z, Breite, Tiefe, Höhe und Winkel eingeben; **Maße / Position anwenden** oder Enter |
| Winkel schrittweise ändern | Schaltflächen für 15° beziehungsweise 90°; mit Shift werden Ausrichtungsbezüge berücksichtigt |
| Zoomen | Mausrad über dem Grundriss |
| Ansicht verschieben | Mittlere oder rechte Maustaste halten und ziehen |
| Gesamtansicht | **Einpassen** |
| Rückgängig / wiederholen | Schaltflächen links, Strg+Z / Strg+Y; auch Strg+Shift+Z wiederholt |
| Laufende Geste abbrechen | Escape stellt die Anordnung vor dem Ziehen wieder her |
| Planer schließen | F7, **Schließen** oder Escape ohne laufende Geste |
| Aktiven Raum speichern | **Raumplan speichern** oder Strg+S |
| Gespeicherten Raum laden | **Gespeicherten Plan laden** |

Ohne Shift bleiben freie Positionen und beliebige Winkel erhalten. Beim Verschieben verwendet Shift ein Raster von 0,25 m sowie nahe Objektkanten und die tatsächliche Raumgrenze. Beim Skalieren rasten Breite und Tiefe in 0,25-m-Schritten ein.

Ergänzung25.10.2026: Der Drehgriff sitzt immer vor der markierten Vorderkante (lokales −Z, bei den Kommandoboxen die Displayseite) und dreht mit dem Element mit. Seine kurze Verbindung beginnt mittig an dieser Kante; der Griff hält32 Bildpunkte Abstand. Er wird beim Verschieben/Zoomen der Ansicht nicht an eine andere Bildschirmkante versetzt. Gerät die Vorderseite aus dem sichtbaren Ausschnitt, die Ansicht verschieben oder **Einpassen** verwenden.

Beim Drehen richtet Shift eine Kante an nahen geraden Wänden, der lokalen Tangente einer gebogenen Innen-/Außenwand oder der Ausrichtung eines benachbarten Elements aus. Parallele und rechtwinklige Ausrichtungen sind möglich; die Vorderseite behält ihre konkrete Orientierung. Wandbezüge gelten bis 2,5 m Abstand, Objektbezüge bis 3 m Abstand zwischen den Grundflächen. Innerhalb von 8° werden gültige Bezüge nach Winkel und Abstand ausgewählt. Sonst gilt ein 15°-Raster. Hilfslinien und Statuszeile zeigen den Bezug. Entfernte Elemente ziehen die Drehung nicht an. Numerisch angewendete Winkel bleiben exakt.

Ein vollständiger Ziehvorgang ist ein Rückgängig-Schritt. Beim Raumwechsel wird eine noch laufende Geste abgebrochen; die letzte Auswahl wird je Raum gemerkt. Der Rückgängig-/Wiederholen-Verlauf wird beim Wechsel geleert, damit ein alter Raumverlauf niemals auf einen anderen Raum angewendet wird. Bereits angenommene Änderungen des vorherigen Raums bleiben im laufenden Spiel erhalten.

## Raumkoordinaten und Grundrisse

Alle Maße sind Meter. X/Z beziehen sich auf den **lokalen Bezugsrahmen des gewählten Raums**, nicht immer auf die globalen Stationsachsen. X verläuft seitlich, +Z entlang der nach außen gerichteten Raumachse. Position bezeichnet die Mitte der Unterseite auf dem Boden; gedreht wird um die Hochachse dieses Raums.

Die Kommandozentrale liegt an der bisherigen 0°-Achse. Drohnenbay, Drohnenwerkstatt und interne Werkstatt verwenden einen um 30° gedrehten Bezugsrahmen um die Stationsmitte. Daher sind beispielsweise X/Z-Werte eines Bay-Elements nicht direkt mit den globalen X/Z-Werten eines Kommandopults vergleichbar. Der Planer berücksichtigt diese Rahmen beim Bearbeiten der realen Modelle.

| Raum | Aktueller Grundriss im lokalen Rahmen |
| --- | --- |
| Kommandozentrale | Ringkeil, Radius 38–54 m, ±13,5° |
| Drohnenbay | Ringkeil, Radius 60–84 m, ±13,5° |
| Drohnenwerkstatt | Ringkeil, Radius 38–54 m, ±13,5° |
| Interne Drohnenwerkstatt | Ringkeil, Radius 18–29 m, ±13,5° |

Der gemeinsame Planer liest Grundriss, Mittelpunkt, Ansichtsausdehnung und Wandgrenzen aus der jeweiligen `RoomDefinition`. Er unterstützt auch rechteckige Definitionen. Die vier aktuell eingebundenen Stationsräume verwenden die oben aufgeführten Ringkeile.

## Freigehaltene Wege und laufende Arbeiten

Die schattierten, beschrifteten Sperrflächen bleiben bei Verschieben, Skalieren, Drehen und beim Laden eines Plans frei:

- In der Kommandozentrale der vorhandene Eingang.
- In der Drohnenwerkstatt eine **3,4 m breite mittlere Fahrspur** zwischen Ringzugang und Schleuse.
- In der Bay der Zugang zur Schleuse, eine **3,4 m breite Querfahrspur** und zwei **3,1 m breite seitliche Fahrspuren**.
- In der internen Werkstatt der Zugang und die interne Drohnenfahrspur.

Zusätzlich prüft das Modell gedrehte Grundflächen gegen Raumgrenzen und andere Gruppen. Standardwerte sind 25 cm Randabstand, 15 cm Abstand zwischen Elementen und 0,4 m Mindestmaß. Die maximale Höhe beträgt in der Kommandozentrale 4,4 m, in den drei zusätzlichen Räumen 5,1 m. Ein Mindestabstand zwischen Elementen ersetzt außerhalb der ausdrücklich geschützten Flächen keine vollständige Laufwegplanung.

Für die vorhandenen Bay-Drohnen gilt abweichend mindestens **3,4 m Breite × 3,4 m Höhe × 5,4 m Tiefe je Platz**. Der Mindestkörper gehört sowohl zur Planvalidierung beim Start und Laden als auch zur Absicherung laufender Änderungen. Damit kann ein Entwurf den tatsächlichen Drohnenkörper nicht durch zu klein angegebene Planmaße umgehen; ein zu kleiner gespeicherter Plan wird abgelehnt.

Gesunde Ringschotte besitzen eine **7 m breite lichte Öffnung**. Je vier ineinandergreifende Teleskopblätter pro Seite, insgesamt acht, ziehen sich in die seitlichen Kassetten zurück. Die Bodenmarkierungen verjüngen ihren Verlauf vor den Schotten und laufen durch die tatsächlich freie Öffnung; die Markierungen sind kein zusätzlicher physischer Rand. Die schmalen Reaktorübergänge behalten ihre bisherige Breite und ihren Mechanismus. Havarierte, gesperrte Übergänge bleiben gesperrt.

Änderungen an gerade benutzten Einrichtungen werden mit Erklärung abgelehnt:

- Die Kommandozentrale wird während eines laufenden R-01-Auftrags gegen Änderungen gesperrt.
- Die gesamte Bay wird gegen Änderungen gesperrt, solange eine der Drohnen unterwegs oder im Umbau ist. Vor dem Umplanen müssen alle Drohnen sicher angedockt sein.
- Die Drohnenwerkstatt wird während laufender R-01-Arbeit oder eines laufenden Umbauauftrags gegen Änderungen gesperrt.
- Die interne Werkstatt wird während laufender R-01-Arbeit gegen Änderungen gesperrt.

Diese Prüfung gilt auch für Laden, Startanordnung sowie Rückgängig/Wiederholen, sobald dadurch ein gesperrtes Element verändert würde. Identische Bestände müssen nicht geändert werden. Zum Umplanen einer benutzten Einrichtung den Planer schließen, den Auftrag abschließen oder sicher abbrechen und danach erneut F7 öffnen.

Die Simulation pausiert während der Raumplanung unabhängig von der normalen Menüpausen-Einstellung. Spielerbewegung und andere Hauptmenüs sind gesperrt. Eine weiterhin freie Spielerposition und Blickrichtung bleiben beim Schließen erhalten; der vorhandene Schutz gegen einen überbauten Spieler wird weiter verwendet.

## Speichern und Rechnerwechsel

**Speichern betrifft jeweils den ausgewählten Raum.** Zum Mitnehmen sämtlicher Entwürfe jeden geänderten Raum speichern. Ein Raumwechsel oder das Schließen des Planers schreibt die Dateien nicht automatisch.

| Raum | Gemeinsame Entwurfsdatei |
| --- | --- |
| Kommandozentrale | `Design/RoomPlans/command-room-layout.json` |
| Drohnenbay | `Design/RoomPlans/drone-bay-layout.json` |
| Drohnenwerkstatt | `Design/RoomPlans/drone-workshop-layout.json` |
| Interne Drohnenwerkstatt | `Design/RoomPlans/internal-workshop-layout.json` |

Der bisherige Kommandoraum-Pfad bleibt erhalten. Jede Datei enthält die passende Raumkennung und die bekannten Elementkennungen dieses Raums. Ein fremder oder unvollständiger Raumplan wird vollständig abgelehnt, statt einzelne Teile in den falschen Raum zu übernehmen. Beschriftungen bleiben dem bekannten Elementkatalog zugeordnet.

Der Kommandoraum-Eintrag `battery` bleibt als stabile Plan-ID bestehen und bezeichnet jetzt das Strompult. Alte Schema-1-Pläne werden mit unveränderten Maßen und Posen geladen; die kanonische Beschriftung lautet im Arbeitsspeicher **STROMVERSORGUNG**. Das Laden schreibt keine Datei. Das Pult und sein E-Bedienpunkt folgen weiterhin den Raumplanänderungen. Der tatsächliche gemeinsame Stationsakku ist davon unabhängig und wird in seiner Wandnische gegenüber der Zentrale repariert.

Die Dateien unter `Design/RoomPlans` sind für Git vorgesehen. Nach dem Übertragen verwendet der nächste Spielstart die gespeicherten Entwürfe. Ein laufendes Spiel kann sie mit **Gespeicherten Plan laden** je Raum übernehmen. Nicht gespeicherte Änderungen sind lediglich im laufenden Prozess vorhanden. Beim erneuten Speichern wird die vorherige Datei als `.json.bak` gesichert; Sicherungen und temporäre Schreibdateien bleiben lokal.

Ein verteilter Build ohne erreichbares Quellprojekt verwendet stattdessen den Benutzerordner `CommandRoomLayout` innerhalb von `Application.persistentDataPath`. Automatisierte Raumprüfungen erhalten getrennte Dateien unter `Logs`.

**Raumpläne sind von Kampagnen-Spielständen getrennt.** Ressourcen, Forschung, Aufträge und Spielerzustand gehören weiterhin zum Spielstand. Ein Spielstand ersetzt die gemeinsamen Raumdateien nicht. Für einen Rechnerwechsel die gespeicherten Raumpläne mit dem Projekt und die gewünschten Kampagnen-Spielstände gesondert berücksichtigen.

Beim Laden werden abgestellte Drohnen ihrem aktuell geplanten Heimdock zugeordnet, auch wenn der Spielstand dieselbe Layoutrevision trägt und noch die alte Dockposition enthält. Kraftstoff, Akku und Ladung stammen weiterhin aus dem Spielstand. Laufende Flüge und Schleusentransite behalten ihre bisherige Wiederherstellung; ein Raum darf während eines solchen Einsatzes nicht umgeplant werden.

## Technischer Aufbau und Grenzen

- `RoomDefinition.cs`: Kennung, Name, gebogener oder rechteckiger Grundriss, Maße, Ausgangselemente und geschützte Fahrflächen.
- `CommandRoomLayout.cs`: eine Instanz je Raum; Planvalidierung, Änderungssperren, Datenkopien, Bindung an Boxen oder vorhandene Modellgruppen sowie atomisches Speichern/Laden.
- `CommandRoomPlanner.cs`: gemeinsame F7-Oberfläche, Raumreiter, Grundriss und Bearbeitungsgesten. Weder Spielfigur noch Spielkamera werden für die Draufsicht versetzt.
- `StationOperations.RoomPlan.cs`: acht Kommandoboxen und ihre Bedienpunkte; die bisherige Plan-ID `battery` bindet das zusätzliche Strompult, ohne gespeicherte Raumpläne umzuschreiben.
- `StationOperations.Rooms.cs`: Einbindung der zusätzlichen realen Raumgruppen, geschützte Fahrspuren, Einsatzsperren und bewegliche Stationsanker.
- `CampaignDroneFleet` und die vorhandenen Werkstatt-/R-01-Abläufe: Anbindung der geplanten Dock- und Arbeitsgruppen an die jeweilige Darstellung und Benutzung.
- `StationDockInfrastructure`: mitbewegte Dockanschlüsse, Statusdarstellung der realen Versorgung und kosmetische Anschlussstücke unter dem Deck.
- `StationPressureGate` / `RingWalkthrough`: Teleskopschotte mit freiem Durchgang und angepasste Bodenmarkierungen der Ringzuläufe.

Die bearbeitbaren Gruppen bleiben einzeln beweglich und werden nicht mit der statischen Stationshülle zusammengefasst. Hülle, Wände, Türen und Schleusen sind kein frei bearbeitbarer Bestandteil dieses Planers. Die bestehenden Modelle werden beim Ändern der Maße mit ihrer Gruppe skaliert; das ist geometrische Raumplanung und noch keine Neuauslegung der jeweiligen Anlage.

Die Dockanschlüsse folgen ihrer geplanten Gruppe. Ihre kosmetischen Wasser-/Strom-Anbindungen werden unterhalb des begehbaren Decks mitgeführt; sichtbar bleiben die Steigstücke zu den Anschlüssen. Die Versorgungsanzeigen beziehen sich weiterhin auf den gemeinsamen Wasser- und Strombestand. Diese Anschlussdarstellung besitzt keinen eigenen Vorrat und bildet keinen allgemeinen Rohr- oder Kabelplaner. Eine automatische Neuplanung beliebiger Stationsleitungen und eine vollständige Prüfung aller möglichen Laufwege sind weiterhin nicht enthalten.

Die historische erste Umsetzung für nur acht Kommandoboxen ist in [Raumplanung-Kommandozentrale.md](Raumplanung-Kommandozentrale.md) beschrieben. Ihre früheren Prüfzahlen gelten nicht automatisch für die Erweiterung auf vier Räume.

## Prüfung

Werkbankkorrektur25.10.2026, `workbench-build-02`: **1862 Werkstatt+794 Vier-Raum=2656 Assertions, beide Exit0**. Echte Tischmaße/Arbeitshöhe, zwei getrennte Unterschränke über die Breite, vorne liegende Schubladen/Griffe sowie bestehender Umbau-/Schleusenablauf und vier Raumkataloge geprüft. Drei gespeicherte Nutzerpläne SHA256-unverändert,246 Quellen und247 Builddateien abgeglichen.16 Bilder; Nahansichten teils durch Reparatursäulen verdeckt, sichtbare Teile geprüft. [Aktueller Nachweis](Pruefungen/Werkbank-Ausrichtung-2026-10-25.json). Weitere Vollsuiten und manuelle Komfort-/Release-/FPS-Abnahme diesmal nicht wiederholt; folgende Prüfstände sind historisch.

Nachkorrektur25.10.2026, `frontrotation-build-01`: Drehgriff und Verbindungssteg an die markierte Vorderkante gesetzt. Vorderachse und32px-Abstand vor und während realer freier/Shift-Drehgesten geprüft. **718 Planer+794 Vier-Raum+28 UI=1540 Assertions, alle Exit0**;15 neue Bilder, relevante Frontgriffbilder bei1600/1280 und Bay visuell geprüft.246 Quellen und247 ausgelieferte Dateien SHA256-abgeglichen, Nutzerplan unverändert. `Builds/Windows`, Vorgänger `Windows-before-frontrotation-20261025` gesichert. [Aktueller Nachweis](Pruefungen/Raumplanung-Drehgriff-Vorne-2026-10-25.json). Die folgenden Vollsuiten wurden für diese gezielte Griffänderung nicht wiederholt und beschreiben den vorherigen Build.

Abnahme25.10.2026 mit `stationrooms-build-06`: Development6000.6.4f1 aus separater Buildkopie, Quellprojekt6000.4.7f1 unverändert. Sieben Läufe derselben Binärdatei bestanden: **794 Vier-Raum-,694 bisherige Planer-,1844 Werkstatt-,97 Aufbau-,177 Stations-,29 Vitals- und28 UI-Prüfungen; zusammen3663 Assertions, jeweils Exit0**.

Geprüft sind alle31 realen Modellbindungen einschließlich Verschieben/Skalieren/Drehen und Wiederherstellen, Raumgrenzen und geschützte Fahrflächen, getrenntes Speichern/Laden, alte Kommandoplan-Dateien, verworfene ungültige Pläne, Hardware-Mindestmaße bereits beim Start und Einsatzsperren. Ein tatsächlich verschobener Dockplatz nimmt Drohne/Ladeaufnahme/Medienanschluss mit; ein älterer Kampagnenstand derselben Revision lädt die geparkte Drohne am neuen Heimdock mit unveränderten Vorräten. Die breiten Ringfugen wurden mit Spieler- und Drohnenkapseln sowie echtem CharacterController durchlaufen. Werkstattfahrt durch die verriegelte Schleuse, Roboter, Rückkehr und sicherer Abbruch sowie bestehender Wasserflug, Produktion, Vitalwerte und nativer Vollbildwechsel sind enthalten.

43 neue Bilder gesichert; die vier Grundrisse, große1280er-Schriftansicht sowie relevante 3D-/HUD-/Vollbildbilder visuell geprüft.246 Quellen-/Meta-/LOD-/Shaderdateien zur Buildkopie und247 ausgelieferte Dateien SHA256-abgeglichen. Build unter `Builds/Windows`, Vorgänger `Builds/Windows-before-stationrooms-20261025` erhalten. Der zuvor gespeicherte Nutzer-Kommandoplan vom11:30 blieb bewahrt; die isolierten Prüfläufe verwenden eigene Dateien unter `Logs`. [Prüfbericht mit Ergebnissen, Logs, Bildern und Manifesten](Pruefungen/Stationsraeume-2026-10-25.json).

Dies ist eine programmatische Laufzeitabnahme mit Bildprüfung, keine manuelle Komfortabnahme. Keine neue Release-,100k-,Langzeit-Balance- oder spielbare FPS-Messung. Beliebige Anordnungen besitzen weiterhin keinen vollständigen Laufwegsolver; die beschriebenen Fahrflächen und Kollisionsgrenzen bleiben maßgeblich. Kein Commit/Push.
