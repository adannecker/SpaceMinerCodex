# SpaceMiner Ideenbacklog

## Nächster Sprint: verbindlicher Umfang und Arbeitsreihenfolge – 26.10.2026

**SP-01 abgeschlossen –26.10.2026:** R03-Gestaltung vom Nutzer bestätigt und im Spiel für R-01/zehn optionale Fahrprobenakteure übernommen. 1523 Playerprüfungen (949 Verkehr, 477 Strom/R-01, 97 Kampagne), beide Builds und drei finale Playerläufe erfolgreich. [Spielintegration und Prüfgrenzen](Innendrohne-I75-R03-Spielintegration-2026-10-26.md). Dock-/Werkzeugfunktionen und Energieauslegung bleiben eigene offene Sprintpakete.

**Nutzerentscheidung:** Die zuletzt genannten Aufgaben sollen im nächsten Sprint eingebaut und priorisiert fertiggestellt werden. Sie sind damit Sprintumfang. Die folgende Arbeitsreihenfolge wurde aus technischen Abhängigkeiten abgeleitet; alle Positionen gehören zum Sprintziel. Status: SP-01 abgeschlossen; SP-02 bis SP-10 weiterhin offen. Kein Start-/Enddatum oder belastbarer Aufwand vereinbart; diese Planung ist keine automatische Terminierung.

| Reihenfolge / ID | Arbeitspaket | Fertig, wenn … |
| --- | --- | --- |
| 1 / SP-01 | I75-Unterseite und Texturen korrigieren | das gemeldete Loch am Mesh geprüft und geschlossen ist, die fehlerhaften Texturstellen korrigiert sind und Unterseite, Materialübergänge sowie Exporte in der drehbaren Vorschau geprüft wurden; die bestätigte Korrekturrevision ist im Spiel übernommen. **Erfüllt mit R03 am26.10.2026**, siehe Spielintegrationsbericht. |
| 2 / SP-02 | Energieauslegung plausibilisieren | Stationsverbrauch und Reaktorerzeugung nachvollziehbar bilanziert sind; Innendrohnen mit Schweben/Fahrt, Bereitschaft, Werkzeug und Transportlast samt Akku, Einsatzdauer und gleichzeitigen Verbrauchern berechnet und im Spiel konsistent abgebildet sind. Keine stillschweigende neue Reaktorleistung: Änderungen aus der Rechnung begründen und dokumentieren. |
| 3 / SP-03 | Reale Ladegeschwindigkeiten umsetzen | aktuelle belastbare Technikquellen, Akkutyp und Ladeverlauf dokumentiert sind; Teil-/Vollladung, Verluste, Temperatur-/Leistungsgrenzen und parallele Ladungen berücksichtigt werden; Simulation und UI dieselben Werte nutzen. Spielzeit und reale Dauer getrennt prüfen. |
| 4 / SP-04 | I75-Docking und Laden | passende Dockmodelle mit sicheren Zufahrten abseits der Fahrspuren im Spiel stehen; Anfahren, Belegen, Laden, Freigeben und Rückkehrreserve funktionieren, ohne Türen/Spuren zu blockieren; die Energie aus der echten Stationsbilanz stammt. Kontakt- versus stationäre Induktionsladung begründet auswählen. Dynamische Induktion im Sprint bewerten, nicht ohne weitere Entscheidung als Standard bauen. |
| 5 / SP-05 | Steuerpultmodell am Konzept ausrichten | 3D-Modell und freigegebenes Konzept gemeinsam abgeglichen, nötige Form-/Material-/Displaykorrekturen abgeschlossen und die passenden Touchpulte im Spiel integriert sind. |
| 6 / SP-06 | Stromversorgungs-UI überarbeiten | Formen, Abschnitte und Gestaltung am Konzept ausgerichtet sind und die tatsächlichen Energie-/Ladedaten weiterhin korrekt und bedienbar dargestellt werden. |
| 7 / SP-07 | Scannerkonsole mit Animation und Fundübersicht | Konsole im Spiel nutzbar ist, tatsächliches Aufladen und Scannen sichtbar animiert sind, erkennbare Fundarten dargestellt werden und gefundene Objekte für bekannte Details anklickbar sind; Reichweite, Ausbau und unbekannte Daten bleiben berücksichtigt. |
| 8 / SP-08 | Stationsplanung samt Reparaturauswahl | Planmodell die tatsächliche Station mit Ausbau und Schäden erkennbar wiedergibt; kaputte Bauteile einzeln auswählbar sind, passende reale Reparaturaufträge ausgelöst werden können und Fortschritt/Reparaturzustand nachgeführt werden. |
| 9 / SP-09 | Hochauflösenden Mira-Avatar integrieren | die vorhandene High-Fassung im vorgesehenen Hauptspiel verwendet wird und Materialien, Animation, Dialogdarstellung und Laufzeitkosten dort geprüft sind. Bereits vorhandene Dialogstudio-/Questspielintegration weiterverwenden und verbleibende Lücken prüfen. |
| 10 / SP-10 | Wechselbare I75-Werkzeuge | gemeinsame Aufnahme und getrennte Bau-/Laser-, Demontage- und Transportmodule einschließlich aufrechtem Wabenträger modelliert und im Spiel nutzbar sind; Umrüsten in der Werkstatt, jeweils eine Aufgabenrolle, Last-/Energiebilanz und Arbeitsabläufe nachvollziehbar funktionieren. |

**Gestaltungsidee zu SP-10 vom26.10.2026:** Werkzeugwechseldock mit stationsseitigem Kran und Montagearm: Drohne dockt an, vorhandenes Werkzeug gegebenenfalls zuerst abnehmen/ablegen, neues Werkzeug holen, ansetzen und befestigen. Sichtbar animierten Wechsel und reale Werkzeugplätze ausarbeiten, gemeinsam mit SP-04. [Ablauf und offene Mechanik](I75-Naechste-Schritte-2026-10-26.md#idee-werkzeugwechseldock-mit-kran-und-montagearm--26102026). Vom Nutzer als Idee eingebracht, genaue Konstruktion noch offen.

Abhängigkeiten: SP-02/03 liefern die Werte für SP-04/06/07/10; SP-01 ist Modellgrundlage für Dock und Werkzeuge, SP-05 die gemeinsame Pultgrundlage. Werkzeugannahmen bereits bei SP-02 einbeziehen und nach SP-10 mit dem tatsächlichen Modell abgleichen. Bestehende Implementierungen vorab sichten und weiterverwenden; bereits erledigte Teilfunktionen nicht neu bauen.

**Gemeinsames Fertig-Kriterium:** Integration im tatsächlichen Spiel statt ausschließlich Konzept/Einzelvorschau, nachvollziehbare Daten statt Beispieldaten, passende Funktions- und Sichtprüfung mit dokumentiertem Ergebnis. Betroffene Spielstände, Raumpläne und bestehende Änderungen anderer Chats erhalten. Ein Punkt bleibt offen, solange Umsetzung oder erforderliche Prüfung fehlt. Vor Sprintabschluss einen zusammenhängenden Ablauf mit Laden, Einsatz, Konsole und Reparatur prüfen und den tatsächlich geprüften Build dokumentieren.

Die Detailaufgaben und Verweise folgen unten. Ältere offene Ideen außerhalb dieses Umfangs werden dadurch nicht automatisch in den Sprint übernommen. In dieser Sitzung nur Sprintplanung dokumentiert und mit den genannten Aufgaben abgeglichen; noch keine Umsetzung, Aufwandsschätzung oder neuen Spieltests/Builds.

## Neue Prüf- und Überarbeitungsaufgaben – 26.10.2026

Auf Nutzerauftrag als offene To-dos festgehalten; die ursprüngliche Nennreihenfolge unten bleibt erhalten, die aktuelle Sprintreihenfolge steht darüber. Noch keine Fehlerbehebung, Integration oder neue Energieberechnung erfolgt.

- [x] Innendrohne: Texturen prüfen und nacharbeiten. – mit bestätigter R03 und Spielübernahme erledigt; geschlossene Strukturteile, Schrift/Materialien und Exporte geprüft. [Spielintegration und Prüfgrenzen](Innendrohne-I75-R03-Spielintegration-2026-10-26.md).
- [x] Innendrohne: Unterseite schließen. – mit bestätigter R03 und Spielübernahme erledigt; geschlossene Strukturteile, Schrift/Materialien und Exporte geprüft. [Spielintegration und Prüfgrenzen](Innendrohne-I75-R03-Spielintegration-2026-10-26.md).
- [ ] **Hochauflösenden Avatar ins Spiel integrieren.** Vorhandenen hochauflösenden Mira-Avatar übernehmen; bisherige Modellwahl-/Dialogstudioarbeit berücksichtigen und tatsächliche Hauptspielintegration, Animationen, Materialien und Laufzeitkosten prüfen. [Mira-Avatar](MiraAvatar.md), [3D-Stand](Mira3D.md).
- [ ] **Steuerpult: 3D-Modell und Konzeptbild erneut gemeinsam prüfen.** Form, Proportionen, Materialien und Displayeinbau abgleichen; nötige Korrekturen vor weiterer Integration festhalten. [Konzept](Steuerpult-Konzept-2026-10-26.md), [Blender-Modell](Steuerpult-Blender-2026-10-26.md).
- [ ] **Stromversorgungs-UI näher ans Konzept bringen.** Formen, Abschnittsaufteilung und Gestaltung der tatsächlichen Oberfläche mit dem Konzept vergleichen und überarbeiten; vorhandene reale Datenanbindung erhalten. [UI-Konzept](Stromversorgung-UI-Konzept-2026-10-25.md), [Umsetzung](Stromversorgung-UI-2026-10-26.md).
- [ ] **Stromverbrauch und Reaktorerzeugung erneut plausibilisieren.** Stationsverbraucher und Erzeugung gemeinsam bilanzieren; für Innendrohnen Schweben/Fahren, Bereitschaft, Werkzeuge und Transportlast sowie Akkukapazität, Einsatzdauer, Energie pro Einsatz und Ladung berechnen. Gleichzeitige Drohnen und Ladeverluste berücksichtigen. Bestehende Werte sind bis zur Prüfung keine neu bestätigte Auslegung; der Prüfauftrag setzt noch keine neue Reaktorleistung. [Versorgungsarchitektur](Stationsversorgung-Architektur.md), [I75-Ladeaufgaben](I75-Naechste-Schritte-2026-10-26.md).
- [ ] **Aufladen an heute mögliche Ladegeschwindigkeiten anpassen.** Bei Umsetzung aktuelle belastbare Akku-/Ladegerätequellen recherchieren, passenden Akkutyp wählen und daraus Ladeleistung, Ladeverlauf, Temperaturgrenzen, Wirkungsgrad und Ladezeiten ableiten. Vergleichbare Ladezustandsbereiche unterscheiden (z. B. Teil- und Vollladung); verfügbare Stationsleistung und parallele Ladungen begrenzen den Durchsatz. Spielzeit und reale Dauer bei Zeitbeschleunigung getrennt ausweisen. Noch keine neue Rate festgelegt.

## Scannerkonsole mit animiertem Scan und Fundübersicht – 26.10.2026

Weiterer Nutzerwunsch zur offenen Aufgabenliste; noch keine Umsetzung:

- [ ] Eine passend zu den Kommandopulten gestaltete Scannerkonsole ausarbeiten und ins Spiel integrieren; bestehende Scannerfunktion und Kampagnendaten verwenden.
- [ ] Das Aufladen des Scanner-Energiespeichers sichtbar animieren und mit dem tatsächlichen Ladefortschritt verbinden.
- [ ] Den anschließenden Scan anschaulich animieren: Fortschritt, erfasster Bereich und eintreffende Kontakte. „Alles scannen“ bleibt auf die tatsächliche Reichweite und Fähigkeiten des Scanners begrenzt.
- [ ] Anzeigen, welche Arten von Funden mit dem aktuellen Ausbau bereits erkennbar sind; mögliche Erkennungen von tatsächlich gefundenen Objekten unterscheiden. Genauere Kategorien anhand des vorhandenen Scan-/Forschungsstands ausarbeiten.
- [ ] Nach dem Scan eine übersichtliche, interaktiv anklickbare Fundübersicht anbieten. Auswahl öffnet die bekannten Details des Fundes und seine Position; unbekannte Bestandteile bleiben als unbekannt gekennzeichnet. Konkrete Detailfelder und Darstellung noch ausarbeiten.

[Scanner-Fachdokumentation und bestehender Stand](Scanner-und-Spielstaende.md). Keine zusätzlichen Fundarten, Reichweiten, Ladezeiten oder Energiewerte beschlossen.

## Stationsplanung näher am tatsächlichen Stationsmodell – 26.10.2026

Weiterer Nutzerwunsch zur offenen Aufgabenliste; noch keine Umsetzung:

- [ ] Das Modell in der Stationsplanung an Form, Proportionen, Anordnung und tatsächlichen Ausbauzustand der im Spiel gebauten Station annähern.
- [ ] Auch beschädigte Teile und fehlende/zerstörte Abschnitte nachvollziehbar darstellen; intakte und reparierte Teile entsprechend dem echten Zustand anzeigen.
- [ ] Beschädigte Bauteile in der Planansicht einzeln auswählbar machen und die Auswahl mit dem passenden realen Reparaturziel verbinden, damit dessen Reparatur geplant/beauftragt werden kann.
- [ ] Vorhandene Reparaturvoraussetzungen, Aufträge und Fortschritte verwenden; die Planansicht nach tatsächlich erfolgter Reparatur aktualisieren. Bestehende auswählbare Schadenszustände berücksichtigen, statt eine zweite Reparatursimulation anzulegen.

[Vorhandene Stationsplanung und Reparaturzustände](Stationsversorgung-Architektur.md). Keine neuen Reparaturkosten oder Freischaltungen festgelegt.

## Nächste I75-Arbeiten – 26.10.2026

Nutzerauftrag nach Freigabe von R02: passende Docking-/Ladeplätze und anschließend die Werkzeugmodule ausarbeiten. [Verbindliche offene Aufgabenliste](I75-Naechste-Schritte-2026-10-26.md).

- [ ] Dockgeometrie, sichere Zufahrt abseits der Durchgangsspuren, Belegung/Warteschlange und Rückkehr bei niedrigem Akku planen.
- [ ] Ladeverfahren wählen: Kontakte oder stationäres induktives Pad; kapazitäts-/leistungsbegrenzte echte Stationsbilanz.
- [ ] Induktive Streckenversorgung während der Fahrt als mögliche spätere Ausbaustufe prüfen; noch keine Technikauswahl oder Leistungswerte beschlossen.
- [ ] Gemeinsame wechselbare Werkzeugaufnahme und separate Bau-/Laser-, Demontage- und Transportkassetten einschließlich aufrechtem Wabenträger modellieren und auslegen. Weiterhin eine Aufgabenrolle pro Drohne, Wechsel durch Umrüstung.

Nur To-dos dokumentiert, noch keine Dock-/Werkzeugumsetzung. Frühere Ideen folgen darunter.

Stand: 08.10.2026. Diese Ausbaustufen erweitern Drohneneinsätze, Stationssteuerung und Gefahren im Asteroidengürtel. Die folgenden Ideen stammen aus dem Ideenbacklog-Chat; Prioritäten, Kosten, Forschungsstufen und konkrete Umsetzung sind noch offen. Der aktuelle Umsetzungsstand steht unten; übrige Ausbaustufen bleiben offen.

## Drohnenversorgung und Spezialisierung

### Batteriewechselstation

Eine spätere Ausbaustufe ermöglicht den Austausch der Drohnenbatterie an einer Wechselstation. Damit kann die Drohne mit einer geladenen Batterie wieder starten, während die entnommene Batterie geladen wird. Wechselzeit, Batterievorrat und Ladeleistung bleiben auszuarbeiten.

### Drohnen für bestimmte Aufgaben

Chapter-1-Abgrenzung vom 08.10.2026: vier Arten, nämlich Abbau, Bau/Reparatur und Erkundung außen sowie eine kleine Helferdrohne innen. Gemeint sind Arten, nicht vier einzelne Fahrzeuge; Anzahl offen. Ein Drohnen-Technologiebaum soll Zweige für jede Art erhalten. Mira erforscht den Rollenumbau mit zusätzlichem Energiebedarf; die aktivierte interne Helferdrohne baut im Dock um. Aktivierung und genaue technische Grenzen der Umbaubarkeit bleiben offen. Weitere Arten, darunter die folgende ältere separate Schrottsammelrolle, sind für spätere Kapitel vorgemerkt. Zielbild: [Spielidee](Spielidee.md#chapter-1-drohnen-mira-forschung-und-ausbauziele-08102026).

Vorhandene Drohnen werden für ihren Einsatzzweck umgebaut:

- Reparaturdrohnen führen Reparaturaufträge aus.
- Erkundungs- und Scandrohnen erkunden das Feld und untersuchen Objekte.
- Schrottsammeldrohnen sammeln Schrott zur weiteren Verwertung.

Ein Rollenwechsel benötigt einen Umbauauftrag. Welche Werkzeuge, Materialien und Einrichtungen die einzelnen Rollen voraussetzen, ist noch offen.

### Umbau und neue Drohnengenerationen

Größere Frachträume und weitere erforschbare Ausstattungsverbesserungen werden durch einen beauftragten Umbau eingebaut. Forschung stellt die Möglichkeit bereit; die vorhandene Drohne erhält die Ausstattung erst durch den Umbau.

Neue technische Drohnenlevels beziehungsweise Generationen entstehen ausschließlich durch Wiederverwertung und Neubau. Eine vorhandene Drohne steigt nicht unmittelbar auf die neue technische Generation auf. Rückgewinnungsquote, Baukosten und Umgang mit vorhandener Ausstattung bleiben offen.

Die bereits geplante Abbau-Erfahrung betrifft besseres Materialverständnis und Verbesserungen von Förderrate oder Energiebedarf, siehe [Bergbaudrohnen](Bergbaudrohnen.md#beschlossene-richtung-abbau-erfahrung-06102026). Wie diese Erfahrung mit technischen Drohnenlevels zusammenwirkt und ob sie bei Umbau oder Neubau erhalten bleibt, ist noch abzustimmen.

## Kartenübersicht und Stationsbefehle

### Übersicht und Asteroidenmarkierungen

Eine Karte zeigt das Asteroidenfeld, die Station und Drohnen in einer gemeinsamen Übersicht. Asteroiden lassen sich nach verschiedenen Eigenschaften markieren. Mögliche Eigenschaften sind bekannte Ressourcen und Scanstatus; die konkrete Auswahl und Filterbedienung sind offen. Unbekannte Eigenschaften sollen durch die Karte nicht automatisch aufgedeckt werden, entsprechend der bestehenden Spielidee zu Sensorwissen.

### Befehle über ausgewählte Objekte

Nach einem Klick auf einen Asteroiden können kontextbezogene Befehle erteilt werden. Ein Beispiel ist: „Navigiere auf die sonnenabgewandte Seite dieses Asteroiden.“ Nach Erforschung der Stationsbewegung lässt sich damit auch die Station gezielt hinter einem Asteroiden positionieren.

Die Karte soll außerdem ermöglichen, die Station aus einem Asteroidenschatten herauszubewegen. Stationsbewegung und das Drehen der Station zur Ausrichtung sind getrennte Fähigkeiten. Reichweite, Flugweg, Verbrauch und Befehlsausführung bleiben auszuarbeiten.

## Sonne und Solarenergie

### Sichtbare Sonne und ausrichtbare Solarflächen

Die Sonne soll im Spiel sichtbar sein und als Orientierung für Sonnenrichtung, Schatten und Panelausrichtung dienen. Solarpanels sollen zur Sonne ausgerichtet werden können.

Der frühere MainDev-Entwurf sieht zunächst das Ausrichten der gesamten Station mit Düsen vor; unabhängig bewegliche Solarflügel folgen nach Forschung und Reparatur, automatische Sonnennachführung später. Diese Reihenfolge ist historisch im [MainDev-Archiv](Chats/05-MainDev.md) festgehalten und wird durch die neuen Ideen ergänzt. Die Station soll zum Gewinnen von Solarenergie aus dem Schatten bewegt werden können.

### Mögliche Planeten auf Umlaufbahnen

Ergänzende Idee vom 06.10.2026: Weitere sichtbare Planeten könnten die Sonne umkreisen und das umgebende Planetensystem erkennbar machen. Anzahl, Erscheinungsbild, Umlaufbahnen und Zeitmaßstab bleiben offen. Ebenfalls offen ist, ob die Umlaufbewegung zunächst nur dargestellt wird oder später Auswirkungen auf die Spielsimulation hat. Eine Erreichbarkeit der Planeten ist damit noch nicht festgelegt; der erste Spielumfang bleibt im Asteroidengürtel.

## Ereignisse und Gefahrenvorhersage

### Solarsturm und Schutzposition

Ein Solarsturm kann ein Ereignis auslösen. Solange kein Schutzschild verfügbar ist, soll die Station auf der sonnenabgewandten Seite eines Asteroiden Schutz suchen können. Der Kartenbefehl zur Positionierung hinter einem Asteroiden unterstützt dieses Manöver.

Schutzschild, Wirksamkeit des Asteroidenschutzes, Vorwarnzeit und mögliche Schäden sind offene Spielregeln. Die Abschirmung wird vor der Umsetzung fachlich geprüft; sie ist hier eine gewünschte Spielmechanik.

### Eindringender Körper und Kollisionen

Ein von außen ins Asteroidenfeld fliegender Körper kann Objekte rammen. Getroffene Asteroiden können zerstört werden, in Fragmente zerfallen und anschließend bewegliche Trümmer bilden. Größe, Geschwindigkeit, Häufigkeit und Folgen eines solchen Ereignisses bleiben offen.

### Scanner und Vorhersagesimulation

Eine spätere Scannertechnologie erfasst die Bahn des eindringenden Körpers und prognostiziert Einschläge. Eine Simulation auf der Karte zeigt den erwarteten Weg, mögliche Kollisionen sowie die anschließenden Bewegungen von Asteroiden und Fragmenten. Sie soll daraus einen sicheren Aufenthaltsort für die Station berechnen können.

Wie weit vorausgesagt werden kann und wie Scanqualität und unbekannte Objekte die Verlässlichkeit beeinflussen, ist offen. Die Vorhersage soll eine Schutzentscheidung und einen anschließenden Navigationsauftrag ermöglichen.

## Zerstörter Planet und Intro

### Ferner Planet mit Strahlungszone

In der Entfernung ist ein explodierter, in große Teile gespaltener Planet sichtbar. Von ihm beziehungsweise seiner Umgebung geht im Spiel eine Strahlungsgefahr aus, die eine Annäherung zunächst verhindert. So ist das Gebiet bereits sichtbar, bevor es zugänglich wird.

Ursache der Planetenzerstörung, Art und Quelle der Strahlung sowie die späteren Voraussetzungen für eine sichere Annäherung bleiben offen. Ein Zusammenhang mit dem Unglück der Station ist noch nicht festgelegt. Die Strahlungszone ist eine Story- und Spielidee; eine physikalische Erklärung muss gesondert ausgearbeitet werden.

### Kurze Cinematic mit Mira und stilisierten Bildern

Das Intro soll das Unglück mit stilisierten Bildern erzählen, begleitet von Miras Stimme. Daraus entsteht eine kurze filmische Eröffnungssequenz, die in die beschädigte Station und den Spielbeginn überleitet.

Bildstil, Bildfolge, Dauer und Sprechertext bleiben auszuarbeiten. Dabei ist festzulegen, was Mira über das Unglück weiß und ob die Bilder bekannte Ereignisse oder eine unvollständige Rekonstruktion zeigen. Der zerstörte Planet kann Teil dieser Bildfolge sein; sein Zusammenhang mit der Stationskatastrophe bleibt offen. Die bisherige überspringbare Intro-Sequenz ist die Grundlage für die spätere Erweiterung, siehe [Story und Dialoge](Dialoge/IntroCinematic/README.md).

## Steam Integration

Statusprüfung22.10.2026: Steamworks-Partnerregistrierung im angemeldeten Browser bereits begonnen, Abschluss/Freischaltung noch ausstehend. Die Anwendungsliste leitete zurück zur Steamworks-Startseite; SpaceMiner-App-ID und hochgeladene Builds deshalb weiterhin nicht bestätigt. Noch keine nachgewiesene technische Steam-Anbindung im Spiel. Ausschliesslich gelesen; keine Vereinbarungen angenommen, Angaben geändert oder Dateien hochgeladen. Private Konto-/Bank-/Steuer-/Identitätsdaten gehören nicht ins Projektarchiv.


Idee vom 06.10.2026: SpaceMiner soll später eine Steam-Integration erhalten. Der konkrete Funktionsumfang und der Zeitpunkt sind noch offen. Erfolge, Cloud-Spielstände und Workshop sind mögliche Ausbaustufen zur späteren Auswahl, noch keine beschlossenen Anforderungen.

Technische Anbindung und Voraussetzungen werden bei der Ausarbeitung festgelegt. Cloud-Spielstände setzen eine eigene Spielstandspeicherung voraus; Workshop-Inhalte benötigen ein festgelegtes System für unterstützte Erweiterungen.

## Zusammenhang der Ausbaustufen

Drohnenforschung eröffnet neue Rollen und Ausstattung; Umbauaufträge setzen sie an vorhandenen Drohnen um. Neue Generationen benötigen Wiederverwertung und Neubau. Scans liefern Informationen für Kartenmarkierungen und spätere Gefahrenprognosen. Erforschbare Stationsbewegung ermöglicht sowohl die Flucht in eine Schutzposition als auch das Verlassen eines Schattens für bessere Solarversorgung.

Diese Abhängigkeiten beschreiben den Entwurf, noch keine festgelegte Reihenfolge im Technologiebaum.

## Umsetzungsstand 07.10.2026

- [x] Neun benannte Planeten auf sichtbaren Bahnen und anklickbares Planetenarchiv; komprimierte Spielabstände.
- [x] Zwei sichtbare Sonnen mit externer Beleuchtung; seit26.10.2026 zusätzlich physikalischer Ertrag beider Sterne nach Fläche, Ausrichtung, Schatten und Netzverbindung sowie Sonneninformationen im Strominterface. Unabhängige Nachführung bleibt offen.
- [x] Zerbrochene Heimatwelt Aetherys, beschädigter Mond, Hitze-/Giftdunst und Materialstrom als visuelle Kulisse; Schaden/Strahlungszone noch offen.
- [x] Zwölfteilige Kohlezeichnungssequenz mit Enceladus, durchgehender Aufnahme, leiser Musik, direkten Überblendungen und MP4-Export.
- [x] Neue Station mit acht Ladebuchten, zwei Wartungsplätzen und Tankkupplung; Wasserübergabe mit Drohnen.
- [x] Wasserabbauwissen mit Förderrate-/Energieschwerpunkt im Techtree.

Ältere offene Beschreibungen des Planeten und Bildintros oben werden durch diesen Stand ersetzt. Batteriewechsel, Rollenumbau, spätere Hardwaretiers, Strahlungsschaden und Ereignisvorhersage sind weiterhin auszuarbeiten. Die Solarleistung ist inzwischen integriert; weitere Steuerung/Nachführung bleibt offen, siehe Doppelsonnen-Nachtrag unten.

## Reproduzierbare Asteroidenwolken mit Seeds (08.10.2026)

Vorgemerkt auf Wunsch des Nutzers: Eine später frei wählbare Seed definiert reproduzierbar eine Asteroidenwolke. Diese soll massiv mehr Asteroiden als das aktuelle 100er-Feld enthalten. Körperzahl, Ausdehnung, Dichte, Form- und Ressourcenverteilung, sichere Stationsregion sowie Generatorversion und Speicherung des Seeds sind noch auszuarbeiten. Gleicher Seed und gleiche Generatorversion sollen dieselbe Wolke erzeugen. Keine neue Millionenkörper-Welt oder Seed-Eingabe in dieser Aufgabe implementiert. Der erste Scanner verwendet jetzt einen festen 10-km-Radius mit zehn vorbereiteten Kontakten und drei Wasserquellen.

## Nächste Implementierungen: Rückmeldung zum Stationsstart (08.10.2026)

Status: Alle vier Punkte am 08.10.2026 auf diesem Desktop umgesetzt und technisch geprüft; subjektive Hörfreigabe der neuen Stationssounds und zusätzliche native Regler-Klickprüfung noch offen. Details und Prüfgrenzen: [Scanner und Spielstände](Scanner-und-Spielstaende.md).

1. **Beenden aus dem laufenden Spiel reparieren — umgesetzt.** Menüanbindung an die vorhandene Speicherinstanz korrigiert; aktive Sitzung wird beim Verlassen beendet. Speicherabfrage, Abbrechen und Verlassen aus Innenansicht, Pult und VR geprüft; echte Windows-Bedienung einschließlich Alt+F4 bestanden.
2. **Innenraumposition zuverlässig speichern und wiederherstellen — umgesetzt.** Manuelle Saves und Autosaves erhalten Position, Blickrichtung und Schleusenphase im Wohnmodul, an beiden bewegten Türen, beim Druckausgleich und im Ring. Laden der Dateien in einem zweiten Player-Prozess sowie Fortsetzung laufender Drohnenaufträge bestanden. Alte v1-Spielstände bleiben lesbar. Lokaler Prüfeditor: 6000.6.4f1; neuer Stand mit Projekteditor 6000.4.7f1 noch ungeprüft.
3. **Unterschiedliche Interaktionssounds — umgesetzt.** Türmotoren für Öffnen/Schliessen, Dichtungsanschlag, Druckausgleich, Pult, VR und Scanbestätigung plus leise Innenambience. Master/Effects, Live-Mute und Cancel angebunden. Technische Prüfung bestanden, subjektive Hörfreigabe offen. Details: [Audio](Audio.md).
4. **UI an Auflösung anpassen und UI-Skalierung anbieten — umgesetzt.** Interface-Regler 75–150 Prozent, Standard 100, live mit Apply/Cancel; Quest, Mira, Pult und Menüs/HUD gemeinsam angebunden. Hohe Auflösungen bleiben kompakt, kleine Fenster passen die UI ein. 32 Player-Bilder mit vergrösserter Schrift und skalierte Trefferflächen geprüft; native Zusatzprüfung wegen Windows-Sperre offen. Details: [Settings](Settings.md).

### Analytischer Fortschritt SP-02/03 –26.10.2026

[Stationsenergie-Auslegung](Stationsenergie-Auslegung-2026-10-26.md), [Parameter-/Technologiekatalog](../Design/Energy/station-energy-reference-2026-10-26.json) und reproduzierbarer Rechner liegen vor: Start1Person/je1Drohne, tatsächliche Doppelsonnengeometrie,22 analytische Prüfungen. Größenordnung4,2kW Bereitschaft/7,8kW Eis-Wasserbetrieb/25,1kW beide Schnelllader; Spitzen und Wärme getrennt.3/12kW-Ladereferenz,40kWe-Reaktor und4kWh-I75 sind Vorschläge; keine neue Hardwarefreigabe. Absolute Solar-kW bleiben ohne Leuchtkräfte/physikalische Entwurfsskala unbekannt; AE-Normierung nur ausdrücklich bedingtes Beispiel. SP-02/03 bleiben offen: Stern-/Magnet-/Kälte-/Akkuparameter entscheiden und danach in die vorhandene Simulation integrieren/prüfen. Keine Unity-/Saveänderung in dieser Rechenrunde.


### Umgesetzter Teil von SP-02/06: Doppelsonnen –26.10.2026

Sternwerte und Energieskala jetzt entschieden, beide Sonnen real in PV-Bilanz und Strominterface integriert; [aktuelle Definition/Bedienung/Prüfung](Doppelsonnen-Energie-2026-10-26.md).451,57+155,85W/m²; Flächen, Zustand, Winkel, getrennte Schatten und Sektornetz berücksichtigt. Neuer SONNEN-Reiter auch im gemeinsamen Konsolenstudio. Endlicher0,5C-Ladestrom und Abregelung umgesetzt.507 Strom-/Sonnenchecks,97 Stationschecks,36 Studioaufnahmen und22 analytische Checks bestanden. Die ältere Aussage „absolute Solar-kW unbekannt“ ist hiermit überholt. SP-02/03 bleiben insgesamt offen: reale dynamische Verbraucherprofile, Magnet-/Kälte-/Akku- und Drohnenladeauslegung sowie Reaktorregelung und deren Integration;40kWe/4kWh/3-12kW noch Vorschläge. SP-06 hat jetzt diese Sonneninformationen, keine pauschale Freigabe aller übrigen Sprintkriterien.
