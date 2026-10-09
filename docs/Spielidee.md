# Space Miner — Spielidee und erster Meilenstein

## Ausgangslage

Die Spielfigur überlebt eine Explosion und erwacht auf einer beschädigten modularen Raumstation im Notstrombetrieb. Zehn kleine Drohnen sind vorhanden, davon ist im ersten Szenario nur Drohne 01 funktionsfähig. Aus diesen Resten entwickelt die Spielfigur schrittweise eine überlebensfähige Infrastruktur in einem Asteroidengürtel.

Energie, Nahrung und Trinkwasser werden zentrale Bedürfnisse. Neue Sensoren erschließen zunehmend verschiedene Ressourcen. Wissensartefakte liefern benötigtes Wissen für weitere technologische Entwicklung.

Der Bordcomputer heißt **Mira**. Sie begleitet den Spieler mit einer sanften, warmen und ruhigen weiblichen Stimme und spricht ihn mit „du“ an. Ihre Sprechertexte und die Inszenierung entstehen in [docs/Dialoge](Dialoge/README.md).

Nutzerentscheidung und Umsetzung vom 09.10.2026: Die bestätigte kompakte Techtree-Ansicht ersetzt die bisherige Spielübersicht auch in Release. Nicht verfügbare Technologien sind grau, bleiben zur Erklärung anklickbar. Alle Details stehen ganz rechts in einer festen scrollbaren Seitenleiste. **T** öffnet und schließt den Baum, in der Konfiguration neu belegbar. Alle bestehenden Mira-Dialoge, das Erwachen und die Erinnerungs-Zwischenszenen nutzen denselben Helmvisier-Rahmen: gezeichnete Mira links, Titel und Text daneben, lange Texte scrollbar. Vorhandene Sprecheraufnahmen, Storytexte und Ablauf bleiben erhalten. Modellgrenzen und Prüfung: [Forschungslabor](Forschungslabor.md), [Dialoge](Dialoge/README.md).

Aktuelle Nutzerentscheidung vom 08.10.2026: 3D-Avatar vorerst zurückgestellt. Mira verwendet die freigegebene Zeichnung als Identitätsvorlage und statische Ausdrucksvarianten für ihre gesprochenen Dialoge: freundlich, besorgt, konzentriert und ermutigend, mit weichen Bildüberblendungen. Keine Mundanimation in dieser Fassung. Die bisherigen 3D-Quellen bleiben für später erhalten. Dialogvorschau im isolierten AvatarLab; gezeichnetes Porträt inzwischen im Hauptspiel-Intro und in Questdialogen integriert. Details: [MiraAvatar](MiraAvatar.md), bisherige 3D-Versuche: [Mira3D](Mira3D.md).

Zunächst bleibt das Spiel in einem Asteroidengürtel. Reisen in andere Sternsysteme gehören nicht zum ersten Umfang. Drohnen beginnen bei etwa 2 × 2 Metern, einzelne Asteroiden können 5 Kilometer Durchmesser erreichen.

## Kapitel und Veröffentlichung — Entwurfsrichtung vom 08.10.2026

Der Nutzer möchte die Hauptgeschichte und den Ausbau in aufeinander aufbauende Chapters gliedern und jedes Kapitel als DLC veröffentlichen. Geplanter Preisanker: etwa **5 EUR je Kapitel**, mit einem Ziel von **40 Stunden Spielspaß in der Hauptgeschichte pro Kapitel**. Preis und Spielzeit sind Entwurfsziele, noch kein geprüftes Leistungsversprechen oder bestätigtes wirtschaftliches Modell.

| Kapitel | Vom Nutzer vorgesehener Schwerpunkt |
| --- | --- |
| Chapter 1 | Die beschädigte Station innerhalb des Asteroidenfeldes wieder fit bekommen. Schwerpunkt: Planung, Produktion, Sammeln, Reparieren, Erkunden und erste Ansätze von Forschung. |
| Chapter 2 | Mobilität der Station im Gebiet der näheren Planeten. |

Weitere Kapitel sind vorgesehen, ihre Inhalte noch offen. Der vorhandene erste Entwicklungsumfang bleibt im Asteroidenfeld; die spätere Mobilität erweitert die Reichweite erst in Chapter 2. Die schon vorhandene Planetenkulisse ist kein Nachweis implementierter Reisen oder eines fertigen Kapitels.

Chapter 1 soll nach ausdrücklicher Nutzerentscheidung vor allem Planungsarbeit fördern: komplexe, realitätsnahe Produktionsketten vorbereiten, Ressourcen sammeln, beschädigte Technik reparieren und die Umgebung erkunden. Forschung beginnt mit ersten Ansätzen und unterstützt diesen Aufbau. Abhängigkeiten und begrenzte Ressourcen sollen nachvollziehbare Prioritäten erfordern. Konkrete Quests, Forschungsgrenzen und Produktionsrezepte bleiben auszuarbeiten; Stationsreisen zu den näheren Planeten gehören zum geplanten Chapter 2.

Veröffentlichungsbedingung nach Nutzerbestätigung vom 08.10.2026: erst launchen, wenn einige hundert Tests den vorgesehenen Umfang und die Qualität bestätigen. Für das Ziel von 40 Stunden Spielspaß sind echte Spieldurchläufe und Spielerfeedback erforderlich; bestandene automatische Funktionsprüfungen allein bestätigen weder Spielspaß noch Hauptgeschichtsdauer. Anzahl der Testpersonen, Verteilung der Tests und genaue Freigabekriterien sind noch auszuarbeiten. Vorhandene historische Prüfzahlen sind kein Nachweis für diese Kapitel-Freigabe.

Noch auszuarbeiten: konkrete Kapitelabschlüsse und Hauptquests, sinnvoll gefüllte Spielzeit statt verlängerter Wartezeiten, Übergabe des Stations-/Forschungsfortschritts zwischen Kapiteln, Abgrenzung kleiner Lagekorrekturen/Ausrichtung gegenüber Reisen sowie eine Grundspiel- beziehungsweise Trägerlösung für die Kapitel-DLCs. Keine Kapitel-, DLC-, Kauf- oder Reisemechanik mit diesem Eintrag implementiert; keine Spielzeitmessung.

## Chapter 1: Drohnen, Mira-Forschung und Ausbauziele (08.10.2026)

Die folgenden Punkte erweitern die beschlossene Kapitelrichtung als Gestaltungsplan; konkrete Quests, Rezepte, Kosten und Abschlusswerte sind noch offen. Keine neue Gameplayimplementierung durch diese Dokumentation.

### Vier Drohnenarten

Nach ausdrücklicher Klärung meint der Nutzer **vier Arten**, nicht vier einzelne Drohnen. Die Anzahl bleibt offen; die bisherigen zehn Außendrohnen werden dadurch nicht auf drei reduziert.

| Art | Einsatzgebiet | Vorgesehene Aufgabe |
| --- | --- | --- |
| Abbaudrohne | Außen | Bekannte Rohstoffe abbauen und zur Station bringen. |
| Bau-/Reparaturdrohne | Außen | Beschädigte Außenanlagen reparieren und neue Anlagen/Module anbauen. |
| Erkundungsdrohne | Außen | Untersuchen und weitere Erkundungs-/Sensordaten gewinnen. |
| Kleine Helferdrohne | Innen | Einfache interne Arbeiten und Umbau einer Außendrohne im Dock. |

Drohnen sollen zwischen den Rollen umbaubar sein; die Rollen werden durch Forschung erschlossen. Vorhandene Abbaukompetenz bleibt Anfangswissen. Für Drohnen ist ein eigener Technologiebaum mit Zweigen für jede Art vorgesehen. Weitere Drohnenarten gehören in spätere Kapitel; die ältere separate Schrottsammeldrohne aus dem Backlog ist damit nicht als fünfte Chapter-1-Art eingeplant. Forschung macht einen Umbau möglich; Helferdrohne, Dock, passende Teile und Versorgung führen ihn physisch aus. Kosten, Dauer, Rückgewinnung und die technische Grenze beim Wechsel zwischen kleiner Innen- und Außendrohne sind noch auszuarbeiten. Die bestehende Regel Umbau versus Wiederverwertung/Neubau neuer Hardwaregenerationen bleibt erhalten.

### Mira als KI und Forschungsbegleiterin

Mira soll als KI der Station mit Avatar auftreten und die Forschung begleiten. Sie erkennt Möglichkeiten, schlägt Projekte vor und bearbeitet Forschung unter zusätzlichem Energiebedarf. Ihre aktuelle gezeichnete Avatar-Darstellung bleibt erhalten; die erzählerische KI-Rolle legt noch keine echte Online-KI-Anbindung fest.

Erstes Nutzerbeispiel: Mira schlägt vor, die zunächst für Abbau vorgesehenen Außendrohnen zu Bau-/Reparaturdrohnen umzubauen. Sie erforscht die Umfunktionierung; die kleine interne Helferdrohne muss aktiviert werden und kann danach im Dock den Umbau durchführen. Aktivierungsvoraussetzungen der Helferdrohne bleiben offen. Die vorhandene R-01-Darstellung besitzt derzeit keine Arbeitslogik.

Ausarbeitungsvorschläge: Der Spieler bestätigt Projekte und weist Energie zu. Mira verwendet vorhandene Archive, erkannte Schäden, Sensordaten und später Materialproben als Wissensgrundlagen. Forschung, fehlende Daten und fehlende Hardware werden getrennt angezeigt; Kosten/Leistung, Dauer, Unterbrechung und parallele Projekte bleiben offen. Einfache erste Aktivierung der Helferdrohne durch Anschließen/Aufladen und Systemprüfung, gegebenenfalls kleiner manueller Reparaturauftrag; noch keine bestätigte Quest oder Materialliste.

### Mira verbessern: Forschungsfähigkeit und Rechnerausbau

Nutzerentscheidung vom 08.10.2026: Mira erhält einen eigenen Entwicklungsbaum. Wiederholte Forschung verbessert ihre Effizienz und Geschwindigkeit; bestimmte Projekte verlangen eine Forschungsstufe, beispielsweise Stufe 2. Der gelbe/amberfarbene Erfahrungsrand zeigt den Fortschritt wie bei anderen Tätigkeiten. Zusätzliche CPUs und mehr verfügbare elektrische Leistung sollen ihre Performance steigern; neue selbst hergestellte CPUs benötigen passende Chipherstellung. Forschungsstufe, installierte Hardware und nutzbare Versorgung werden getrennt geführt. Forschungsstufe 2 ist keine automatische Chapter-2-Zuordnung. Konkrete Boni, Erfahrungsschwellen, CPU-/Energiewerte und Chapter-1-Grenzen bleiben offen. Weitere Äste und Versorgungs-/Kühlbedingungen als Vorschläge: [Mira-Entwicklung](Mira-Entwicklung.md). Keine Gameplayimplementierung.

### Entwicklungsbereiche und mögliche Abschlussziele

Auf Nutzerauftrag vom 08.10.2026 für alle bisher besprochenen Bereiche Bildvorschläge samt gemeinsamer Forschungsübersicht ausgearbeitet: [Techtree-Galerie](Techtree/README.md). Bereichseinstiege zeigen beispielsweise Mira 0/12; Zähler unterscheiden erreichte Knoten von den wiederholten Erfahrungsleveln am gelben Rand. Nullwerte sind Mockup-Beispiele, bekanntes Startwissen bleibt verfügbar. Konkrete Knoten, Nenner und Kapitelgrenzen sind Gestaltungsvorschläge, keine bestätigten Freischaltungen oder implementierte neue Menüführung.

| Bereich | Nutzerwunsch für den Ausbau | Möglicher Chapter-1-Abschluss, noch Vorschlag |
| --- | --- | --- |
| Drohnen | Vier Rollen, Umbau und Forschungszweige je Art. | Alle vier Rollen verfügbar und im Einsatz. |
| Stromversorgung | Batteriekapazität, Solarpanels und weitere Energieentwicklung. | Energie trägt Versorgung, Forschung und erste Produktion verlässlich. |
| Ressourcenwissen | Lernen, was verarbeitet werden kann. | Mehrere bekannte Stoffe mit erschlossenen Verarbeitungswegen. |
| Produktion und Lagerung | Komplexe Fertigung; Containertypen passend zu den Ressourcen. | Mehrere verbundene Herstellungsstufen und passende Lager. |
| Station | Weitere anbaubare Ringe und Erweiterungen. | Erste funktionsfähige Erweiterung angebaut. |
| Scanner/Erkundung | Reichweite und Arten erfassbarer Informationen verbessern. | Weiteres Arbeitsgebiet entdeckt und ausreichend untersucht. |

Auch nicht identifizierte Funde sollen in geeigneten Behältern verwahrt werden können. Herkunft, Identifikation und Verarbeitbarkeit sind getrennte Informationen; Untersuchung muss vor Nutzung unbekannter Stoffe ausgearbeitet werden. Für die genaue Containerauslegung sind Stoffeigenschaften und Lagerbedingungen zu berücksichtigen; keine pauschalen Behälterrezepte beschlossen.

Ein Glasdom ist gewünscht; ob er bereits am Anfang vorhanden ist, bleibt offen. Vorschlag: anfangs beschädigt oder abgeschaltet, Wiederherstellung als sichtbarer Meilenstein. Zweck, Versorgung und Bauanforderungen noch festzulegen. Als ergänzender Abschlussbereich sollten Wasser, Luft und Nahrung aus den bisherigen Survival-Zielen berücksichtigt werden; ihre konkrete Chapter-1-Ausgestaltung ist noch offen.

Vorgeschlagener früher Fortschrittsbogen: erste Versorgung sichern → Helferdrohne aktivieren und Reparaturumbau erforschen → im Dock umbauen → beschädigte Solarflächen reparieren → zusätzliche Energie für Erkundung, Materialanalyse und Produktion nutzen. Dies ist eine Reihenfolge zur Diskussion, noch kein finaler Questplan. Die Rückkopplung zwischen Forschung, realem Umbau und Energieausbau soll Planung ermöglichen; der Chapter-1-Abschluss bleibt eine tragfähige Station im Asteroidenfeld, Reisen folgen in Chapter 2.

## Wiederverwertung (08.10.2026)

Nutzerentscheidung: eigener Entwicklungsbereich für Wiederverwertung. Aus Trümmern vollständige nutzbare Bestandteile wie Akkus und Hüllenplatten gewinnen; außerdem kleinere Bestandteile und schließlich Rohstoffe zurückgewinnen. Die gezielte Rückgewinnung eines Rohstoffs setzt dessen Erforschung voraus. Vorschlag: verzweigte Wege Wiederverwendung, Präzisionsdemontage und stoffgerechte Rückgewinnung mit Diagnose, passenden Anlagen und Materialwissen. Unbekannte Anteile bleiben getrennt gelagert; Mengen, Verfahren, Fundorte und Kapitelgrenzen sind offen. Details: [Wiederverwertung](Wiederverwertung.md). Keine neue Gameplaymechanik implementiert.

## Rohstoffwissen vor Nutzung (08.10.2026)

Nutzerentscheidung: benötigte Rohstoffe und Werkstoffe je Anlage sammeln und ihre Zugänglichkeit durch Abbau beziehungsweise Bergung planen. Neue Stoffe müssen erkannt und geeignete Trenn-/Aufbereitungsverfahren erforscht werden, bevor sie als nutzbare Materialien eingesetzt werden können. Ortung, Zusammensetzung, Gewinnbarkeit und Produktqualität sind getrennt. Die vorhandene Wasser-/Starttechnik bleibt Anfangswissen. Erste Sammlung für die Energieversorgung: [Werkstoffe und Rohstoffe](Werkstoffe-und-Rohstoffe.md). Konkrete Materiallisten, Fundorte, Verfahren und Mengen sind Vorschläge beziehungsweise offen; noch keine neue Gameplaymechanik.

## Wartung und praktische Erfahrung (08.10.2026)

Nutzerentscheidung: Beanspruchung führt zu Verschleiß; Reparaturen sind eine wiederkehrende Aufgabe über den Stationsaufbau hinaus. Wiederholte Reparaturen verbessern die praktische Erfahrung und damit die Effizienz. Lernen durch wiederholte Tätigkeit soll dort, wo sinnvoll, auch für weitere Arbeiten gelten. Ein sich füllender gelber/amberfarbener Rand am jeweiligen Techtree-Icon zeigt den Fortschritt zum nächsten Erfahrungslevel; das Info-Overlay erklärt Level und Effizienzverbesserungen. Erfahrung, Anlagenzustand und neue technische Forschung bleiben getrennt. Konkrete Boni, Schwellen, Verschleißregeln und Wartungsorganisation sind offen; noch keine entsprechende Gameplay-/UI-Erweiterung implementiert. Details: [Techtree](Techtree.md#wiederkehrende-reparaturen-und-lernen-durch-tätigkeit-08102026).

## Nahrung: drei Entwicklungspfade (08.10.2026)

Vom Nutzer vorgesehene Entwicklungsrichtung: chemische Synthese, photosynthetische Nahrungserzeugung mit Pflanzen/Algen und mikrobiologische Produktion in Bioreaktoren bilden drei Nahrungspfade. Samen und geeignete lebende Mikroorganismen sollen erst gefunden werden, beispielsweise bei der Bergung von Raumschifftrümmern in späteren Chapters. Organische Verbindungen in Asteroiden sind kein Nachweis lebender Kulturen.

Chemische Synthese ist als möglicher früher Pfad vorgeschlagen, weil sie keine Samen oder lebenden Kulturen benötigt. Das bedeutet nicht, dass das Verfahren technisch einfacher ist. Eine vorhandene, wiederherstellbare Syntheseanlage mit gespeichertem Verfahrenswissen und anfänglichen Notrationen wäre ein Ausarbeitungsvorschlag; Startausstattung, Chapter-Zuordnung und vollständige Nährstoffversorgung bleiben offen. Zucker allein deckt keine vollständige Ernährung ab.

Formaldehyd kann aus Methanol hergestellt werden; Methanol wiederum ist aus CO₂ und Wasserstoff zugänglich. Für eine mögliche Stationskette müssen Wasseraufbereitung/Elektrolyse, Kohlenstoffquelle, Katalysatoren, Energie, Wärmeabfuhr und Produktreinigung ausgelegt werden. Zuckerbildung aus Formaldehyd ist chemisch bekannt, aber eine zuverlässige lebensmitteltaugliche Produktion damit noch nicht als Spielrezept abgesichert. Recherche und offene Anforderungen: [Techtree](Techtree.md#nahrungspfade-und-chemische-synthese-08102026). Keine Nahrungsmechanik implementiert.

Nutzer bestätigt Wasser und Kohlenstoff als die beiden zu findenden Grundrohstoffe dieser frühen chemischen Richtung; Energie treibt die Verarbeitung an. Präzisierung: Wasser ist eine Verbindung, Kohlenstoff ein Element, im Asteroidenmaterial häufig an andere Elemente gebunden. Kohlenstoffform, Gehalt und Aufbereitung bleiben zu untersuchen; keine reine Kohlenstofflagerstätte oder Diamantenressource beschlossen. Die zwei Grundrohstoffe betreffen die C/H/O-Basis einer möglichen Kohlenhydratproduktion, nicht sämtliche Anlagenmaterialien, Hilfsstoffe oder eine vollständige Ernährung.

## Steuerung und Logistik

Die Spielfigur beginnt in der Station. Die frei navigierbare Außenansicht soll einen virtuellen, aus bekannten Messdaten aufgebauten Raum darstellen; ein Blick aus dem Fenster zeigt dagegen nur den tatsächlichen sichtbaren und erkennbaren Ausschnitt. Spätere Produktionslinien werden über eine Planungsansicht, sinngemäß einen Planungstisch, entworfen. Drohnen im Schiffsinneren führen die Transport- und Arbeitsaufträge aus. Die Logistik soll vollständig auf Drohnen beruhen; Förderbänder sind nicht vorgesehen. Der Einstieg verwendet den vorhandenen begehbaren Stationsinnenraum aus Commit ff0ff7b mit Wohnmodul, Schleuse und Stationsring. Der erste Nahbereichsscan wird nach Annäherung über E am realen Stationspult ausgeführt. Die virtuelle Aussenansicht zeigt bis dahin keine Asteroiden; Debugsicht erteilt keine Arbeitsfreigabe. Die aktuelle Quest bleibt am linken Rand; Mira begleitet sie mit dem gezeichneten Porträt.

### Scanner und bekannte Arbeitsgebiete (Entscheidung 08.10.2026)

- Der virtuelle Nahraum enthält zunächst nur die Station; die Planetenübersicht bleibt beim Herauszoomen verfügbar. Herkunft und Detailumfang der anfänglichen Planetendaten sind noch auszuarbeiten.
- Erste Aufgabe ist ein Nahbereichsscan, der die ersten Asteroiden erreichbar entdeckt. Scanner besitzen einen eigenen aufladbaren Energiespeicher; Startladung 97 Prozent, volle Ladung nach zehn Sekunden bei 1×. Implementierter Einstieg: 0,1 kWh eigener Speicher, 1,08 kW Ladeleistung, ein erster Kugelscan mit fest 10 km Reichweite für zehn Kontakte einschliesslich drei Wasserquellen. Die zehn Startkontakte liegen innerhalb von 10 km, übrige Körper ausserhalb; spätere Tierwerte bleiben zu balancieren.
- Ortung und grobe Forminformation sind von Oberflächenkartierung, visueller Erkundung und Materialanalyse getrennt. Ohne Kartierung zeigt die virtuelle Ansicht nur den jeweils bekannten Detailstand, gegebenenfalls eine grobe Form ohne Textur. Spätere Erkundungsdrohnen ergänzen Oberflächendaten und Bilder, bei Bedarf mit eigener Beleuchtung.
- Arbeitsdrohnen dürfen nur bereits gescannte Bereiche anfliegen. Welche Datenqualität und Aktualität einen Bereich für Arbeitsflüge freigibt, bleibt festzulegen; ein Kontakt ist nicht automatisch eine sichere Bohrstelle.
- Später entwickelte Drohnen ermöglichen eine Verbindung mit visueller Live-Ansicht. Grundlegende autonome Navigation setzt keinen für den Spieler verfügbaren Videostream voraus.
- Scannertechnik soll in jedem Tier einen eigenen Techtree erhalten. Details werden im Techtree-Themenchat ausgearbeitet. Effizienz und regelmäßige Aktualisierung werden für bewegte Objekte und kontinuierliche Überwachung wichtig.

Stationskonsole, erster Scan, datenabhängige VR-Sicht und Auftragsfreigabe sind implementiert. Oberflächenkartierung und Drohnen-Livebilder stehen aus. Debugsicht zeigt alle Objekte ohne Wissens-/Arbeitsfreigabe. Details und Prüfung: [Scanner und Spielstände](Scanner-und-Spielstaende.md). Nahbereichssensorik und Voraussetzungen für sicheres Bohren bleiben Gegenstand weiterer Ausarbeitung.

Für den ersten Prototyp nutzen Außendrohnen elektrisch erhitztes Wasser als ausgestoßene Reaktionsmasse. Sie benötigen sowohl Strom als auch Treibwasser. Ein späterer Technologiebaum kann weitere Antriebe freischalten.

## Beschlossene Startbedingungen

- Die Stromversorgung kombiniert vorhandene Solarflächen und einen kleinen Kernreaktor. Batterien speichern Energie und werden auch für die Drohnen geladen.
- Das Schiff beginnt im Notstrombetrieb. Die internen Schiffsbatterien müssen erst wieder aufgeladen werden. Welche Quelle den Notstrom liefert und wie Solarflächen und Kernreaktor anfangs nutzbar sind, ist noch abzustimmen.
- Von zehn vorhandenen Drohnen funktioniert im ersten Szenario nur Drohne 01 (Nutzerentscheidung vom 05.10.2026). Die übrigen neun können später repariert werden. Reichweite und Ladeprioritäten werden später weiter ausgearbeitet.
- Die Sicherung einer Wasserquelle ist das erste Versorgungsziel.
- Der Wasserprototyp verwendet einstellbare Entwurfswerte für Tank, Masse, Schub, Batterie und Verbrauch. Diese sind Spielannahmen und werden bei späterer technischer Ausarbeitung überprüft; die aktuellen Werte und Vereinfachungen stehen im README. Der Notstrombetrieb und die Schiffsbatterien sind noch nicht als eigenes System umgesetzt.

## Technologische Leitlinien für spätere Entwicklung

Der Technologiebaum soll möglichst realitätsnah sein. Für die spätere Ausarbeitung müssen reale Verfahren und ihre Voraussetzungen recherchiert und mit belastbaren Quellen belegt werden. Die folgenden Punkte sind Entwurfsziele, noch kein wissenschaftlich geprüfter Technologiebaum:

Bestätigung vom 08.10.2026: Der Nutzer möchte komplexe Produktionsketten, deren Zutatenlisten eng an den tatsächlich benötigten Materialien liegen. Ein Solarpanel soll beispielsweise passende Zwischenprodukte, Werkstoffe und Fertigungsschritte voraussetzen. Zeitraffer wird ausdrücklich befürwortet, um längere Abläufe überbrücken zu können. Detailrezepte hängen von der gewählten Technologie ab; Mengen, Fertigungsanlagen, Verluste und Balance bleiben auszuarbeiten. Erste recherchierte Orientierung und Abgrenzung zwischen terrestrischen Modulen und Raumfahrttechnik: [Techtree](Techtree.md#realistische-produktionsketten-und-solartechnik-08102026). Noch keine zusätzliche Produktionsmechanik mit dieser Entscheidung implementiert.

- Wissen, Maschinen, Ressourcen und verfügbare Energie bilden getrennte Voraussetzungen. Ein Wissensfund allein erschafft keine fertige Anlage.
- Sensoren geben Informationen mit begrenzter Reichweite und Sicherheit; Materialgewinnung und Verarbeitung sind eigene Schritte.
- Lebensunterhalt und Produktion verbrauchen Ressourcen. Rückgewinnung hat Verluste.
- Spielvereinfachungen und spekulative Technologien werden ausdrücklich dokumentiert.
- Beschlossene Ausbaurichtung vom 06.10.2026: Für die erste Abbauauslegung dient ungefähr 1:2 zwischen aktiver Werkzeugzeit und Nachladezeit ihrer verbrauchten Energie als Startziel, zuzüglich Flugverbrauch und Ladeverlusten. Akku und Ladestation sollen in späteren Tiers deutlich verbessert werden können. Kapazität, zulässige Lade-/Entladeleistung und Ladegerät sind getrennte Eigenschaften; konkrete Tiernummern, Technik und Faktoren bleiben offen. Details in [Bergbaudrohnen](Bergbaudrohnen.md) und [Techtree](Techtree.md); bisherige Spielwerte noch unverändert.
- Größenverhältnisse bleiben nachvollziehbar. Für größere Räume werden wir später prüfen, ob ein verschiebbarer Weltursprung oder getrennte Simulations-/Darstellungskoordinaten nötig sind.

## Meilenstein 01 — jetzt

- [x] Entwicklungsumgebung und Unity-Projekt eingerichtet; das bestehende gemeinsame Projekt wird weiterverwendet.
- [x] Startbare 3D-Weltraumszene mit verteilten Asteroiden; inzwischen 100 Körper im aktiven Modus.
- [x] Kamera mit Zoom, Rotation, Verschieben, Bewegung und Wiederherstellung der Standardansicht.

Ergänzende Testhilfen: einfache Schiff-/Drohnenplatzhalter für den Maßstab, Gesamtübersicht und Objektfokus. Noch keine vollständige Survival-Mechanik; Wasserabbauwissen ist als erste Erfahrungsprogression implementiert.

## Meilenstein 02 — Wasser sichern

Eine Eisquelle auswählen und Drohne 01 mit dem Befüllen des Schiffstanks beauftragen. Die Drohne fliegt zur Quelle, baut ein Eisgemisch ab, bringt die Ladung zurück und übergibt das gewonnene Wasser. Am Schiff lädt sie ihre Batterie und füllt Treibwasser nach. Der Auftrag wiederholt die Flüge bis zum vollen Tank oder bis die Quelle erschöpft ist.

Ein Balken über der Drohne zeigt ihren Phasenfortschritt. Die Objektanzeige zeigt Batterie, Treibwasser, Geschwindigkeit, Ladung, Zielentfernung, Ankunftszeit und verbleibende Abbauzeit. Die Aufgabenliste zeigt die Versorgung des Schiffstanks. Ein Zeitraffer macht die bei kleinem Schub langen Flug- und Ladezeiten testbar.

Asteroiden zeigen nur bekannte Bestandteile: beispielsweise 80 % Wasser und 20 % unbekannt. Forschung soll die unbekannten Bestandteile später auflösen. Der erste aktiv ausgelöste Scan bestätigt Wasser bei drei vorbereiteten Testquellen. Weiterer Scan-/Forschungsausbau bleibt offen; Wasserabbau-Erfahrung ist bereits implementiert.

Die Grafik bleibt aus austauschbaren Platzhaltern aufgebaut. Konzepte für Gesteins-, Eis- und Metallasteroiden können parallel entwickelt und später als Unity-Modelle integriert werden.

## Raumstation: erster visueller Prototyp (06.10.2026)

Die Basis ist nach Nutzerentscheidung eine modulare Raumstation. Ein geschirmter Reaktor sitzt im Zentrum, daneben ein Wasser-/Eisbehälter. Vier Verbindungsgänge führen zum umlaufenden Zugangsring; sechs verschlossene Anschlüsse ermöglichen später weitere Module. Zwei beschädigte Solarflügel zeigen fehlende Zellen und leicht schiefe Halterungen. Die erste Spielgrafik verwendet einfache Grundkörper, etwa 60 m Gesamtbreite. Antrieb, Verteidigung, Modulbau und Reparaturen sind spätere Systeme; der Ring rotiert derzeit nicht. Frühere Schiffbezeichnungen in historischen Texten und Mira-Testsprachdateien werden später abgeglichen.

## Sonne und Stationsausrichtung — bestätigter Entwurf (07.10.2026)

Die Lichtquelle soll als sichtbare Sonne erscheinen. Solarleistung hängt von Sonnenentfernung, Ausrichtung, intakter Panel-Fläche und Abschattung ab. Die Station beginnt mit langsamer Eigenrotation. Zunächst wird die ganze Station über mit Wasser und elektrischer Energie betriebene Dampf-Lagedüsen ausgerichtet: bestehende Rotation abbremsen, drehen und wieder abbremsen. Eine gehaltene Orientierung benötigt ohne Störung keinen dauernden Schub. Startwasser und Restenergie müssen eine Versorgungssackgasse verhindern; konkrete Werte bleiben offen.

Beim Auswählen der Station sollen aktuelle und maximal mögliche Solarleistung, Ausrichtung und Abschattung je Flügel, Rotationsrichtung und -geschwindigkeit sowie Wasser und Lagedüsenstatus sichtbar sein. Eine unabhängige manuelle Panel-Ausrichtung muss zunächst erforscht und die Mechanik repariert werden; automatische Sonnennachführung folgt später. Bewegung der Asteroiden ist eine spätere Erweiterung. Diese Entscheidungen aus MainDev sind Entwurfsstand und noch nicht als Spielsystem umgesetzt.

## Sichtbare Heimatwelt und Sonne (07.10.2026)

Auf Nutzerauftrag als Laufzeitkulisse ergänzt: sichtbare Sonne, zerbrochener Planet mit auseinandergerissenen Hälften und glühendem Kern, halb zerstörter Mond, giftig wirkender grünlicher Auswurf sowie glühender Materialstrom vom Mond zum Planeten. Diese Darstellung ist visuell; Temperatur, Gift, Schaden und das oben beschriebene Sonnenenergie-/Ausrichtungssystem sind weiterhin nicht als Mechanik implementiert. Alle zwölf Erinnerungsbilder mit durchgehender Enceladus-Narration sind sofort im Cinematics-Bereich der Galerie abspielbar; siehe docs/Dialoge/IntroCinematic/README.md.

## Ideensammlung: Umsetzungsstand (07.10.2026)

- [x] Wasserauftrag mit Drohnenflug, Eisabbau, Rücktransport und Tankübergabe als Prototyp.
- [x] Modulare Raumstation und Bergbaudrohnen als erste Spielgrafik.
- [x] Mira-Erwachen mit freigegebener Aoede-Stimme und Galerie-Wiederholung.
- [x] Zwölf Kohlezeichnungen mit Enceladus als sofort verfügbares Cinematic; abgenutzte, gewölbte und geneigte Bildblätter, Fokusfahrten und direkte Bildüberblendungen.
- [x] Gemeinsamer Untertitel-Schalter für Cutscenes und Cinematics; Erinnerungssequenz mit vollständigem Text der aktuellen Szene; keine wortgenauen Zeitmarken.
- [x] Große zentrale Sonne und neun Planeten auf unterschiedlichen geneigten Umlaufbahnen; Planeten 3, 7 und 8 mit Ringen.
- [x] Planet 4 als zerstörte Heimatwelt nach freigegebenem Konzept: unregelmäßige Kontinentfragmente, tiefe glühende Spalten, beschädigter Mond, giftiger Dunst und Mond-zu-Planet-Materialstrom. Vereinfachte 3D-Umsetzung; keine vollständig ausgearbeitete Oberfläche.
- [x] Alle zwölf Teile des Intro-Cinematics integriert, durchgehende Enceladus-Narration, Charcoal-Atmosphere-Musik und direkter Bildüberblendung; korrigierte Szene 9.
- [ ] Gift-, Hitze- und Schadensmechanik sowie entsprechende Warnungen.
- [x] Komprimierte Spielabstände mit fünf Mondabständen zur Station; zwei Sonnen als externe Beleuchtung. Der astronomische Entwurf wurde auf Nutzerwunsch ersetzt.
- [ ] Abstands-/ausrichtungsabhängige Solarleistung, Stationsrotation, Lagedüsen und Panelsteuerung.
- [x] Wasserabbauwissen mit Förderrate-/Energieschwerpunkt im Techtree; weitere Hardwareforschung, Reparatur und Ausbau bleiben offen.

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

## Drohnendock und sichtbare Ladung (06.10.2026)

Am Dock stehen auf Nutzerauftrag zwei Reihen mit je vier Ladeplätzen. Die Drohnen parken rückwärts teilweise in den Buchten. Für die zehn bisher vorhandenen Drohnen ergänzt der Prototyp zwei Wartungsplätze. Zum Entladen koppelt die Drohne direkt am Wassertank an; Eis wird durch eine kurze geschlossene Übergabe statt durch freien Wurf entladen. Die Klammern sollen oberflächennah bleiben, die Eisstücke kräftiger und kantiger aussehen. Eine bewegliche Ladeklappe und ein einsehbarer Frachtraum zeigen den Füllstand. Diese Darstellung verändert die zentrale Ressourcenbilanz nicht.

## Geplante Ausbaustufen aus dem Ideenbacklog

Steam-Integration ist als spätere Ausbaustufe vorgesehen. Funktionsumfang, technische Anbindung und Zeitpunkt sind offen; Einzelheiten im [Ideenbacklog](Ideenbacklog.md#steam-integration).

Die Sonne soll sichtbar sein. Ergänzend könnten weitere sichtbare Planeten die Sonne umkreisen; Anzahl, Umlaufdarstellung und Einfluss auf die Spielsimulation sind offen. Daraus ergibt sich noch keine geplante Reise zu diesen Planeten.

Entwurfsrichtung vom 06.10.2026: Batteriewechselstation sowie Reparatur-, Erkundungs-/Scan- und Schrottsammeldrohnen. Rollenwechsel und erforschte Ausstattungsverbesserungen wie größere Frachträume benötigen Umbauaufträge. Neue technische Drohnengenerationen entstehen ausschließlich durch Wiederverwertung und Neubau; das Verhältnis zur separat geplanten Abbau-Erfahrung bleibt offen.

Eine Kartenübersicht soll Asteroiden nach Eigenschaften markieren und Befehle an ausgewählten Objekten erlauben. Nach Erforschung der Stationsbewegung gehören dazu das Navigieren auf die sonnenabgewandte Seite eines Asteroiden und das Verlassen seines Schattens. Sichtbare Sonne und ausrichtbare Solarpanels ergänzen die Energieversorgung. Solarstürme, Schutz hinter Asteroiden beziehungsweise spätere Schutzschilde sowie eindringende Körper mit Kollisionen und beweglichen Fragmenten sind weitere Ereignisideen. Spätere Scanner sollen Bahnen und Einschläge in einer Kartensimulation vorhersagen und sichere Positionen berechnen. Details und offene Spielregeln stehen im [Ideenbacklog](Ideenbacklog.md); noch keine Implementierung oder feste Priorisierung.

Weitere Storyidee vom 06.10.2026: Ein ferner, explodierter und in große Teile gespaltener Planet ist sichtbar, wegen einer Strahlungszone zunächst aber unzugänglich. Das Intro erzählt das Unglück als kurze Cinematic aus stilisierten Bildern mit Mira als Erzählerin. Zusammenhang zwischen Planetenzerstörung und Stationsunglück, Strahlungsursache und spätere Zugangsvoraussetzungen bleiben offen. Die Inszenierung ist noch nicht umgesetzt; Details im [Ideenbacklog](Ideenbacklog.md#zerstörter-planet-und-intro) und unter [Story und Dialoge](Dialoge/README.md#geplante-erweiterung-des-intros).

## Spielstände (08.10.2026)

Manuelle Spielstände erhalten Namen und Kommentar. Laden zeigt Scanstatus, Wasserabbau-Level, Forschungsfortschritt und Vorräte. Autosave alle 60 Sekunden aktiver Spielzeit sowie beim Verlassen; Speicherabfrage vor der Rückkehr zum Startmenü und beim Anwendungsschliessen. Laufende Drohnenaufträge, Fracht und Scanwissen werden gespeichert. Weitere Techtree-Felder bleiben Entwürfe. Details: [Scanner und Spielstände](Scanner-und-Spielstaende.md).


### Bestehender Innenraum aus dem anderen Rechner

Ergänzung vom 07./08.10.2026 auf Nutzerwunsch: Zwischen Außenansicht und First-Person-Innenansicht der Station wechseln können. Das Wohnmodul sitzt am Ringanschluss und besitzt nur ein Frontfenster; die geschlossenen Seiten bleiben für spätere Geräte vorgesehen. Eine elektrische Schleuse mit zwei nacheinander öffnenden Schiebetüren verbindet es mit dem vollständig begehbaren Ring. Ruhige Beleuchtung, eine kleine ausgeschaltete Reparaturdrohne und ein erstes Stationspult sind eingebaut. Am Pult lassen sich der vorhandene Wasserauftrag übernehmen und Außendrohne/Tank/Simulation steuern. V und ein Bildschirmknopf wechseln die Ansicht, E bedient Pult und Schleuse. Bewegung mit WASD und Maus, Wände/Boden mit Kollision und gleichzeitiger Weiterbetrieb der Außendrohnen sind umgesetzt. Gehen mit lokaler Schwerkraft und angezeigter zeitgesteuerter Druckausgleich bleiben Prototypannahmen; Stationsrotation, Gasbilanz, Reparatur- und Innenraum-Produktionslogik sind offen. Bedienung und Prüfung stehen im README.

## Techtree-Bedienung (09.10.2026)

Aktuelle Nutzerentscheidung: technische, gegenständliche Piktogramme entsprechend den Bildentwürfen. Der vollständige gewählte Forschungsbaum wird standardmässig eingepasst. Zoom darf die nutzbare Höhe nicht überschreiten; Verschieben nur horizontal durch Klicken und Ziehen, ohne sichtbare Scrollbalken. Knotendetails erst beim Anklicken als schwebendes Overlay, nicht beim Hover. Das Forschungslabor setzt diese Bedienung um; auch die bisherige Tier-I-Spielübersicht zeigt Details nur noch nach Klick. Katalog/Forschungswerte bleiben als Debugsimulation getrennt von tatsächlichem Bau und Ressourcenverbrauch. Details: [Forschungslabor](Forschungslabor.md).

Techtree-Demo-Ansicht, Entscheidung09.10.2026: einheitliche kompakte1:1-Ausgangsicons72×72, reduzierte Abstände, grosse Bäume ohne automatische Verkleinerung per Ziehen erreichbar. Gruppen vieler direkter Endpunkte docken unter einer gemeinsamen horizontalen Verteilerlinie an. Demo-Canvas und Schriften skalieren mit der Auflösung. Diese Entscheidung ersetzt die vorherige Vollbaum-Einpassung und rein horizontale Navigation in der Forschungsvorschau/-demo. Details und tatsächliche Prüfungen: docs/Forschungslabor.md.
