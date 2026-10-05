# Space Miner — Spielidee und erster Meilenstein

## Ausgangslage

Die Spielfigur überlebt eine Explosion und erwacht auf einem beschädigten Raumschiff im Notstrombetrieb. Zehn kleine Drohnen sind vorhanden, davon ist im ersten Szenario nur Drohne 01 funktionsfähig. Aus diesen Resten entwickelt die Spielfigur schrittweise eine überlebensfähige Infrastruktur in einem Asteroidengürtel.

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

1. Entwicklungsumgebung und neues Unity-Projekt einrichten.
2. Eine startbare 3D-Weltraumszene mit wenigen verteilten Asteroiden; Kugeln reichen zunächst aus.
3. Kamera mit Zoom, Rotation, Verschieben und Bewegung; eine Taste stellt die Standardansicht wieder her.

Ergänzende Testhilfen: einfache Schiff-/Drohnenplatzhalter für den Maßstab, Gesamtübersicht und Objektfokus. Noch keine Survival- oder Forschungsmechanik.

## Meilenstein 02 — Wasser sichern

Eine Eisquelle auswählen und Drohne 01 mit dem Befüllen des Schiffstanks beauftragen. Die Drohne fliegt zur Quelle, baut ein Eisgemisch ab, bringt die Ladung zurück und übergibt das gewonnene Wasser. Am Schiff lädt sie ihre Batterie und füllt Treibwasser nach. Der Auftrag wiederholt die Flüge bis zum vollen Tank oder bis die Quelle erschöpft ist.

Ein Balken über der Drohne zeigt ihren Phasenfortschritt. Die Objektanzeige zeigt Batterie, Treibwasser, Geschwindigkeit, Ladung, Zielentfernung, Ankunftszeit und verbleibende Abbauzeit. Die Aufgabenliste zeigt die Versorgung des Schiffstanks. Ein Zeitraffer macht die bei kleinem Schub langen Flug- und Ladezeiten testbar.

Asteroiden zeigen nur bekannte Bestandteile: beispielsweise 80 % Wasser und 20 % unbekannt. Forschung soll die unbekannten Bestandteile später auflösen. Derzeit ist der Wasserscan für drei Testquellen vorgegeben; es gibt noch keine Forschungsmechanik.

Die Grafik bleibt aus austauschbaren Platzhaltern aufgebaut. Konzepte für Gesteins-, Eis- und Metallasteroiden können parallel entwickelt und später als Unity-Modelle integriert werden.
