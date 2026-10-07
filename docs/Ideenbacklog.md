# SpaceMiner Ideenbacklog

Stand: 07.10.2026. Diese Ausbaustufen erweitern Drohneneinsätze, Stationssteuerung und Gefahren im Asteroidengürtel. Die folgenden Ideen stammen aus dem Ideenbacklog-Chat; Prioritäten, Kosten, Forschungsstufen und konkrete Umsetzung sind noch offen. Der aktuelle Umsetzungsstand steht unten; übrige Ausbaustufen bleiben offen.

## Drohnenversorgung und Spezialisierung

### Batteriewechselstation

Eine spätere Ausbaustufe ermöglicht den Austausch der Drohnenbatterie an einer Wechselstation. Damit kann die Drohne mit einer geladenen Batterie wieder starten, während die entnommene Batterie geladen wird. Wechselzeit, Batterievorrat und Ladeleistung bleiben auszuarbeiten.

### Drohnen für bestimmte Aufgaben

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

Bildstil, Bildfolge, Dauer und Sprechertext bleiben auszuarbeiten. Dabei ist festzulegen, was Mira über das Unglück weiß und ob die Bilder bekannte Ereignisse oder eine unvollständige Rekonstruktion zeigen. Der zerstörte Planet kann Teil dieser Bildfolge sein; sein Zusammenhang mit der Stationskatastrophe bleibt offen. Die bisherige überspringbare Intro-Sequenz ist die Grundlage für die spätere Erweiterung, siehe [Story und Dialoge](Dialoge/README.md#geplante-erweiterung-des-intros).

## Steam Integration

Idee vom 06.10.2026: SpaceMiner soll später eine Steam-Integration erhalten. Der konkrete Funktionsumfang und der Zeitpunkt sind noch offen. Erfolge, Cloud-Spielstände und Workshop sind mögliche Ausbaustufen zur späteren Auswahl, noch keine beschlossenen Anforderungen.

Technische Anbindung und Voraussetzungen werden bei der Ausarbeitung festgelegt. Cloud-Spielstände setzen eine eigene Spielstandspeicherung voraus; Workshop-Inhalte benötigen ein festgelegtes System für unterstützte Erweiterungen.

## Zusammenhang der Ausbaustufen

Drohnenforschung eröffnet neue Rollen und Ausstattung; Umbauaufträge setzen sie an vorhandenen Drohnen um. Neue Generationen benötigen Wiederverwertung und Neubau. Scans liefern Informationen für Kartenmarkierungen und spätere Gefahrenprognosen. Erforschbare Stationsbewegung ermöglicht sowohl die Flucht in eine Schutzposition als auch das Verlassen eines Schattens für bessere Solarversorgung.

Diese Abhängigkeiten beschreiben den Entwurf, noch keine festgelegte Reihenfolge im Technologiebaum.

## Umsetzungsstand 07.10.2026

- [x] Neun benannte Planeten auf sichtbaren Bahnen und anklickbares Planetenarchiv; komprimierte Spielabstände.
- [x] Zwei sichtbare Sonnen mit externer Beleuchtung; ausrichtungsabhängiger Solarertrag bleibt offen.
- [x] Zerbrochene Heimatwelt Aetherys, beschädigter Mond, Hitze-/Giftdunst und Materialstrom als visuelle Kulisse; Schaden/Strahlungszone noch offen.
- [x] Zwölfteilige Kohlezeichnungssequenz mit Enceladus, durchgehender Aufnahme, leiser Musik, direkten Überblendungen und MP4-Export.
- [x] Neue Station mit acht Ladebuchten, zwei Wartungsplätzen und Tankkupplung; Wasserübergabe mit Drohnen.
- [x] Wasserabbauwissen mit Förderrate-/Energieschwerpunkt im Techtree.

Ältere offene Beschreibungen des Planeten und Bildintros oben werden durch diesen Stand ersetzt. Batteriewechsel, Rollenumbau, spätere Hardwaretiers, Strahlungsschaden, Solarleistung und Ereignisvorhersage sind weiterhin auszuarbeiten.
