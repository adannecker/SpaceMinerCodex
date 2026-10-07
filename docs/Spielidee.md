# Space Miner — Spielidee und erster Meilenstein

## Ausgangslage

Die Spielfigur überlebt eine Explosion und erwacht auf einer beschädigten modularen Raumstation im Notstrombetrieb. Zehn kleine Drohnen sind vorhanden, davon ist im ersten Szenario nur Drohne 01 funktionsfähig. Aus diesen Resten entwickelt die Spielfigur schrittweise eine überlebensfähige Infrastruktur in einem Asteroidengürtel.

Energie, Nahrung und Trinkwasser werden zentrale Bedürfnisse. Neue Sensoren erschließen zunehmend verschiedene Ressourcen. Wissensartefakte liefern benötigtes Wissen für weitere technologische Entwicklung.

Der Bordcomputer heißt **Mira**. Sie begleitet den Spieler mit einer sanften, warmen und ruhigen weiblichen Stimme und spricht ihn mit „du“ an. Ihre Sprechertexte und die Inszenierung entstehen in [docs/Dialoge](Dialoge/README.md).

Zunächst bleibt das Spiel in einem Asteroidengürtel. Reisen in andere Sternsysteme gehören nicht zum ersten Umfang. Drohnen beginnen bei etwa 2 × 2 Metern, einzelne Asteroiden können 5 Kilometer Durchmesser erreichen.

## Steuerung und Logistik

Die Spielfigur übernimmt zunächst die Rolle des Kommandanten aus der Außenansicht. Spätere Produktionslinien werden über eine Planungsansicht, sinngemäß einen Planungstisch, entworfen. Drohnen im Schiffsinneren führen die Transport- und Arbeitsaufträge aus. Die Logistik soll vollständig auf Drohnen beruhen; Förderbänder sind nicht vorgesehen.

Für den ersten Prototyp nutzen Außendrohnen elektrisch erhitztes Wasser als ausgestoßene Reaktionsmasse. Sie benötigen sowohl Strom als auch Treibwasser. Ein späterer Technologiebaum kann weitere Antriebe freischalten.

## Beschlossene Startbedingungen

- Die Stromversorgung kombiniert vorhandene Solarflächen und einen kleinen Kernreaktor. Batterien speichern Energie und werden auch für die Drohnen geladen.
- Das Schiff beginnt im Notstrombetrieb. Die internen Schiffsbatterien müssen erst wieder aufgeladen werden. Welche Quelle den Notstrom liefert und wie Solarflächen und Kernreaktor anfangs nutzbar sind, ist noch abzustimmen.
- Von zehn vorhandenen Drohnen funktioniert im ersten Szenario nur Drohne 01 (Nutzerentscheidung vom 05.10.2026). Die übrigen neun können später repariert werden. Reichweite und Ladeprioritäten werden später weiter ausgearbeitet.
- Die Sicherung einer Wasserquelle ist das erste Versorgungsziel.
- Der Wasserprototyp verwendet einstellbare Entwurfswerte für Tank, Masse, Schub, Batterie und Verbrauch. Diese sind Spielannahmen und werden bei späterer technischer Ausarbeitung überprüft; die aktuellen Werte und Vereinfachungen stehen im README. Der Notstrombetrieb und die Schiffsbatterien sind noch nicht als eigenes System umgesetzt.

## Technologische Leitlinien für spätere Entwicklung

Der Technologiebaum soll möglichst realitätsnah sein. Für die spätere Ausarbeitung müssen reale Verfahren und ihre Voraussetzungen recherchiert und mit belastbaren Quellen belegt werden. Die folgenden Punkte sind Entwurfsziele, noch kein wissenschaftlich geprüfter Technologiebaum:

- Wissen, Maschinen, Ressourcen und verfügbare Energie bilden getrennte Voraussetzungen. Ein Wissensfund allein erschafft keine fertige Anlage.
- Sensoren geben Informationen mit begrenzter Reichweite und Sicherheit; Materialgewinnung und Verarbeitung sind eigene Schritte.
- Lebensunterhalt und Produktion verbrauchen Ressourcen. Rückgewinnung hat Verluste.
- Spielvereinfachungen und spekulative Technologien werden ausdrücklich dokumentiert.
- Größenverhältnisse bleiben nachvollziehbar. Für größere Räume werden wir später prüfen, ob ein verschiebbarer Weltursprung oder getrennte Simulations-/Darstellungskoordinaten nötig sind.

## Meilenstein 01 — jetzt

- [x] Entwicklungsumgebung und Unity-Projekt eingerichtet; das bestehende gemeinsame Projekt wird weiterverwendet.
- [x] Startbare 3D-Weltraumszene mit verteilten Asteroiden; inzwischen 100 Körper im aktiven Modus.
- [x] Kamera mit Zoom, Rotation, Verschieben, Bewegung und Wiederherstellung der Standardansicht.

Ergänzende Testhilfen: einfache Schiff-/Drohnenplatzhalter für den Maßstab, Gesamtübersicht und Objektfokus. Noch keine Survival- oder Forschungsmechanik.

## Meilenstein 02 — Wasser sichern

Eine Eisquelle auswählen und Drohne 01 mit dem Befüllen des Schiffstanks beauftragen. Die Drohne fliegt zur Quelle, baut ein Eisgemisch ab, bringt die Ladung zurück und übergibt das gewonnene Wasser. Am Schiff lädt sie ihre Batterie und füllt Treibwasser nach. Der Auftrag wiederholt die Flüge bis zum vollen Tank oder bis die Quelle erschöpft ist.

Ein Balken über der Drohne zeigt ihren Phasenfortschritt. Die Objektanzeige zeigt Batterie, Treibwasser, Geschwindigkeit, Ladung, Zielentfernung, Ankunftszeit und verbleibende Abbauzeit. Die Aufgabenliste zeigt die Versorgung des Schiffstanks. Ein Zeitraffer macht die bei kleinem Schub langen Flug- und Ladezeiten testbar.

Asteroiden zeigen nur bekannte Bestandteile: beispielsweise 80 % Wasser und 20 % unbekannt. Forschung soll die unbekannten Bestandteile später auflösen. Derzeit ist der Wasserscan für drei Testquellen vorgegeben; es gibt noch keine Forschungsmechanik.

Die Grafik bleibt aus austauschbaren Platzhaltern aufgebaut. Konzepte für Gesteins-, Eis- und Metallasteroiden können parallel entwickelt und später als Unity-Modelle integriert werden.

## Raumstation: erster visueller Prototyp (06.10.2026)

Die Basis ist nach Nutzerentscheidung eine modulare Raumstation. Ein geschirmter Reaktor sitzt im Zentrum, daneben ein Wasser-/Eisbehälter. Vier Verbindungsgänge führen zum umlaufenden Zugangsring; sechs verschlossene Anschlüsse ermöglichen später weitere Module. Zwei beschädigte Solarflügel zeigen fehlende Zellen und leicht schiefe Halterungen. Die erste Spielgrafik verwendet einfache Grundkörper, etwa 60 m Gesamtbreite. Antrieb, Verteidigung, Modulbau und Reparaturen sind spätere Systeme; der Ring rotiert derzeit nicht. Frühere Schiffbezeichnungen in historischen Texten und Mira-Testsprachdateien werden später abgeglichen.

## Sonne und Stationsausrichtung — bestätigter Entwurf (07.10.2026)

Die Lichtquelle soll als sichtbare Sonne erscheinen. Solarleistung hängt von Sonnenentfernung, Ausrichtung, intakter Panel-Fläche und Abschattung ab. Die Station beginnt mit langsamer Eigenrotation. Zunächst wird die ganze Station über mit Wasser und elektrischer Energie betriebene Dampf-Lagedüsen ausgerichtet: bestehende Rotation abbremsen, drehen und wieder abbremsen. Eine gehaltene Orientierung benötigt ohne Störung keinen dauernden Schub. Startwasser und Restenergie müssen eine Versorgungssackgasse verhindern; konkrete Werte bleiben offen.

Beim Auswählen der Station sollen aktuelle und maximal mögliche Solarleistung, Ausrichtung und Abschattung je Flügel, Rotationsrichtung und -geschwindigkeit sowie Wasser und Lagedüsenstatus sichtbar sein. Eine unabhängige manuelle Panel-Ausrichtung muss zunächst erforscht und die Mechanik repariert werden; automatische Sonnennachführung folgt später. Bewegung der Asteroiden ist eine spätere Erweiterung. Diese Entscheidungen aus MainDev sind Entwurfsstand und noch nicht als Spielsystem umgesetzt.

## Sichtbare Heimatwelt und Sonne (07.10.2026)

Auf Nutzerauftrag als Laufzeitkulisse ergänzt: sichtbare Sonne, zerbrochener Planet mit auseinandergerissenen Hälften und glühendem Kern, halb zerstörter Mond, giftig wirkender grünlicher Auswurf sowie glühender Materialstrom vom Mond zum Planeten. Diese Darstellung ist visuell; Temperatur, Gift, Schaden und das oben beschriebene Sonnenenergie-/Ausrichtungssystem sind weiterhin nicht als Mechanik implementiert. Die ersten drei Erinnerungsbilder mit Enceladus-Narration sind sofort in der Cutscene-Galerie abspielbar; siehe docs/Dialoge/IntroCinematic/README.md.

## Ideensammlung: Umsetzungsstand (07.10.2026)

- [x] Wasserauftrag mit Drohnenflug, Eisabbau, Rücktransport und Tankübergabe als Prototyp.
- [x] Modulare Raumstation und Bergbaudrohnen als erste Spielgrafik.
- [x] Mira-Erwachen mit freigegebener Aoede-Stimme und Galerie-Wiederholung.
- [x] Erste drei Kohlezeichnungen mit Enceladus als sofort verfügbares Cinematic; abgenutzte, gewölbte und geneigte Bildblätter, Fokusfahrten und weichere Schwarzblenden.
- [x] Gemeinsamer Untertitel-Schalter für Cutscenes und Cinematics; Erinnerungssequenz mit satzweisen Untertiteln.
- [x] Große zentrale Sonne und neun Planeten auf unterschiedlichen geneigten Umlaufbahnen; Planeten 3, 7 und 8 mit Ringen.
- [x] Planet 4 als zerstörte Heimatwelt nach freigegebenem Konzept: unregelmäßige Kontinentfragmente, tiefe glühende Spalten, beschädigter Mond, giftiger Dunst und Mond-zu-Planet-Materialstrom. Vereinfachte 3D-Umsetzung; keine vollständig ausgearbeitete Oberfläche.
- [ ] Weitere neun Teile des Intro-Cinematics: Teil 4 aufgenommen und Bildprompt vorbereitet; Bild, Hoerfreigabe und Integration noch offen.
- [ ] Gift-, Hitze- und Schadensmechanik sowie entsprechende Warnungen.
- [x] Erde/Sonne- und Erde/Mond-Abstaende als Bezug, Station mit fuenf Mondabstaenden; Sonnenrichtung als externe Beleuchtungsquelle.
- [ ] Abstands-/ausrichtungsabhängige Solarleistung, Stationsrotation, Lagedüsen und Panelsteuerung.
- [ ] Forschungsmechanik hinter dem vorhandenen Techtree-Entwurf; Reparatur und Ausbau der Station und Drohnen.

Aktueller Spielmassstab (07.10.2026, ersetzt den vorherigen astronomischen Entwurf): Auf ausdruecklichen Nutzerwunsch sind die Himmelsabstaende und Koerpergroessen fuer die Sichtbarkeit komprimiert. Heimatweltbahn 1800km, Mondabstand 18km, Stationsabstand zum Planeten 90km. Hauptsonnenradius 55km, Planetenradius 4km, Mondradius 1,1km. Neun Bahnen bei 650 / 1050 / 1400 / 1800 / 2500 / 3500 / 4700 / 6100 / 7800km; diese Werte sind fiktionale Darstellungsmasse, keine echten astronomischen Abstaende. Umlaufbewegungen bleiben vereinfacht, keine N-Koerper-Simulation. Der vorherige Entwurf mit 1 AE und 384400km Mondabstand bleibt im Projektmemory historisch nachvollziehbar.

Die lokale Station, Drohnen und Asteroiden bleiben in Metern. Eine getrennte Kilometer-Kamera stellt Himmelskoerper dar und uebernimmt Orientierung, Blickwinkel und in Kilometer umgerechnete Position der Spielkamera. Zwei sichtbare Sonnen: warme Hauptsonne und kuehler Begleiter, beide mit gerichteter Beleuchtung auch fuer Planeten und Ringe. Schattenseiten werden fuer die Lesbarkeit grafisch aufgehellt; Stationsverkleidung reagiert matter auf diffuses Licht. Das ist keine photometrisch genaue Simulation, keine Doppelstern-N-Koerper-Simulation und noch kein Solarenergieertrag.

Heimatwelt ansehen richtet die Kamera mit 12 Grad Sichtfeld auf Planet 4. Sonnensystem rahmt alle neun Bahnen; beim Herauszoomen erscheinen Bahnlinien und Planetennamen. Zur Station bzw. R stellt die normale Stationsansicht wieder her; Fokus und Felduebersicht beenden ebenfalls die Vergroesserung. Station etwa 60m, Drohnen etwa 2m, Asteroiden bis 5km bleiben erhalten. Andocken bleibt im lokalen Meterraum praezise.

Das freigegebene Planet-/Mond-Konzept liegt unter outputs/Concepts/Planet4-Mond-Freigegeben.png. Gezielte Nah- und Stationsansichten liegen unter outputs/Cinematic.

Neuere Station am 07.10.2026 aus origin/main 7d8f541 integriert: zwei Reihen mit je vier Ladebuchten, zwei Wartungsplaetze und Tankkupplung. Zugehoerige Drohnenlogik uebernommen: Ausfahren, rueckwaerts Einparken, Tank-An-/Abkoppeln und geschlossene Frachtuebergabe; lokale Aoede-Stimme und Cinematic bewahrt. Beenden im Spiel fuehrt zum pausierten Startbildschirm und erhaelt den laufenden Zustand; das eigentliche Schliessen der Anwendung bleibt im Startmenue.

## Planetenarchiv (07.10.2026)

Auf Nutzerauftrag erhalten alle neun Planeten exotische Namen und erfundene Archivtexte. Anklicken des Planeten oder seines Namens in der Gesamtuebersicht oeffnet ein scrollbares Infofenster mit Typ, Besonderheiten, Geschichte und Status. Planet ansehen richtet die Kamera darauf. Texte sind Weltgestaltung; vermutete Vorkommen, Gefahren und Archive sind noch keine freigeschalteten Spielmechaniken. Inhalte liegen in PlanetLore.cs, Auswahl und Infofenster in RuinedWorld.cs.

| Bahn | Name | Charakter |
| --- | --- | --- |
| 1 | Veyrath | Glut- und Metallwelt, der erste Funken |
| 2 | Soryn | Bernsteinwolken, Kristallregen und Korrosion |
| 3 | Ilythra | Frostwelt mit hellem Ring und alten Funksignalen |
| 4 | Aetherys | Zerstörte Heimatwelt, offene Glut und verlorene Archive |
| 5 | Kharuun | Rote Schluchten, Magnetstürme und versunkene Forschungsstation |
| 6 | Oruvex | Goldener Gasriese mit dauerhaftem Sturmauge |
| 7 | Nymara | Türkisfarbener Ringriese, verschollenes Observatorium |
| 8 | Vaelora | Kobaltblauer Eisriese, Polarlichter und schräger Ring |
| 9 | Xhal'Tir | Violette Grenzwelt, Zwielicht und ungeklärtes Tiefenecho |
