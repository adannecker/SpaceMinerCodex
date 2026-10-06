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
- Beschlossene Ausbaurichtung vom 06.10.2026: Für die erste Abbauauslegung dient ungefähr 1:2 zwischen aktiver Werkzeugzeit und Nachladezeit ihrer verbrauchten Energie als Startziel, zuzüglich Flugverbrauch und Ladeverlusten. Akku und Ladestation sollen in späteren Tiers deutlich verbessert werden können. Kapazität, zulässige Lade-/Entladeleistung und Ladegerät sind getrennte Eigenschaften; konkrete Tiernummern, Technik und Faktoren bleiben offen. Details in [Bergbaudrohnen](Bergbaudrohnen.md) und [Techtree](Techtree.md); bisherige Spielwerte noch unverändert.
- Größenverhältnisse bleiben nachvollziehbar. Für größere Räume werden wir später prüfen, ob ein verschiebbarer Weltursprung oder getrennte Simulations-/Darstellungskoordinaten nötig sind.

## Meilenstein 01 — jetzt

1. Entwicklungsumgebung und neues Unity-Projekt einrichten.
2. Eine startbare 3D-Weltraumszene mit wenigen verteilten Asteroiden; Kugeln reichen zunächst aus.
3. Kamera mit Zoom, Rotation, Verschieben und Bewegung; eine Taste stellt die Standardansicht wieder her.

Ergänzende Testhilfen: einfache Schiff-/Drohnenplatzhalter für den Maßstab, Gesamtübersicht und Objektfokus. Noch keine Survival- oder Forschungsmechanik.

## Meilenstein 02 — Wasser sichern

Eine Eisquelle auswählen und Drohne 01 mit dem Befüllen des Schiffstanks beauftragen. Die Drohne fliegt zur Quelle, baut ein Eisgemisch ab, bringt die Ladung zurück und übergibt das gewonnene Wasser. Am Schiff lädt sie ihre Batterie und füllt Treibwasser nach. Der Auftrag wiederholt die Flüge bis zum vollen Tank oder bis die Quelle erschöpft ist.

Ein Balken über der Drohne zeigt ihren Phasenfortschritt. Die Objektanzeige zeigt Batterie, Treibwasser, Geschwindigkeit, Ladung, Zielentfernung, Ankunftszeit und verbleibende Abbauzeit. Die Aufgabenliste zeigt die Versorgung des Schiffstanks. Ein Zeitraffer macht die bei kleinem Schub langen Flug- und Ladezeiten testbar.

Asteroiden zeigen nur bekannte Bestandteile: beispielsweise 80 % Wasser und 20 % unbekannt. Forschung soll die unbekannten Bestandteile später auflösen. Derzeit ist der Wasserscan für drei Testquellen vorgegeben; es gibt noch keine Forschungsmechanik.

Die Grafik bleibt aus austauschbaren Platzhaltern aufgebaut. Konzepte für Gesteins-, Eis- und Metallasteroiden können parallel entwickelt und später als Unity-Modelle integriert werden.

## Raumstation: erster visueller Prototyp (06.10.2026)

Die Basis ist nach Nutzerentscheidung eine modulare Raumstation. Ein geschirmter Reaktor sitzt im Zentrum, daneben ein Wasser-/Eisbehälter. Vier Verbindungsgänge führen zum umlaufenden Zugangsring; sechs verschlossene Anschlüsse ermöglichen später weitere Module. Zwei beschädigte Solarflügel zeigen fehlende Zellen und leicht schiefe Halterungen. Die erste Spielgrafik verwendet einfache Grundkörper, etwa 60 m Gesamtbreite. Antrieb, Verteidigung, Modulbau und Reparaturen sind spätere Systeme; der Ring rotiert derzeit nicht. Frühere Schiffbezeichnungen in historischen Texten und Mira-Testsprachdateien werden später abgeglichen.

## Drohnendock und sichtbare Ladung (06.10.2026)

Am Dock stehen auf Nutzerauftrag zwei Reihen mit je vier Ladeplätzen. Die Drohnen parken rückwärts teilweise in den Buchten. Für die zehn bisher vorhandenen Drohnen ergänzt der Prototyp zwei Wartungsplätze. Zum Entladen koppelt die Drohne direkt am Wassertank an; Eis wird durch eine kurze geschlossene Übergabe statt durch freien Wurf entladen. Die Klammern sollen oberflächennah bleiben, die Eisstücke kräftiger und kantiger aussehen. Eine bewegliche Ladeklappe und ein einsehbarer Frachtraum zeigen den Füllstand. Diese Darstellung verändert die zentrale Ressourcenbilanz nicht.

## Geplante Ausbaustufen aus dem Ideenbacklog

Steam-Integration ist als spätere Ausbaustufe vorgesehen. Funktionsumfang, technische Anbindung und Zeitpunkt sind offen; Einzelheiten im [Ideenbacklog](Ideenbacklog.md#steam-integration).

Die Sonne soll sichtbar sein. Ergänzend könnten weitere sichtbare Planeten die Sonne umkreisen; Anzahl, Umlaufdarstellung und Einfluss auf die Spielsimulation sind offen. Daraus ergibt sich noch keine geplante Reise zu diesen Planeten.

Entwurfsrichtung vom 06.10.2026: Batteriewechselstation sowie Reparatur-, Erkundungs-/Scan- und Schrottsammeldrohnen. Rollenwechsel und erforschte Ausstattungsverbesserungen wie größere Frachträume benötigen Umbauaufträge. Neue technische Drohnengenerationen entstehen ausschließlich durch Wiederverwertung und Neubau; das Verhältnis zur separat geplanten Abbau-Erfahrung bleibt offen.

Eine Kartenübersicht soll Asteroiden nach Eigenschaften markieren und Befehle an ausgewählten Objekten erlauben. Nach Erforschung der Stationsbewegung gehören dazu das Navigieren auf die sonnenabgewandte Seite eines Asteroiden und das Verlassen seines Schattens. Sichtbare Sonne und ausrichtbare Solarpanels ergänzen die Energieversorgung. Solarstürme, Schutz hinter Asteroiden beziehungsweise spätere Schutzschilde sowie eindringende Körper mit Kollisionen und beweglichen Fragmenten sind weitere Ereignisideen. Spätere Scanner sollen Bahnen und Einschläge in einer Kartensimulation vorhersagen und sichere Positionen berechnen. Details und offene Spielregeln stehen im [Ideenbacklog](Ideenbacklog.md); noch keine Implementierung oder feste Priorisierung.

Weitere Storyidee vom 06.10.2026: Ein ferner, explodierter und in große Teile gespaltener Planet ist sichtbar, wegen einer Strahlungszone zunächst aber unzugänglich. Das Intro erzählt das Unglück als kurze Cinematic aus stilisierten Bildern mit Mira als Erzählerin. Zusammenhang zwischen Planetenzerstörung und Stationsunglück, Strahlungsursache und spätere Zugangsvoraussetzungen bleiben offen. Die Inszenierung ist noch nicht umgesetzt; Details im [Ideenbacklog](Ideenbacklog.md#zerstörter-planet-und-intro) und unter [Story und Dialoge](Dialoge/README.md#geplante-erweiterung-des-intros).
