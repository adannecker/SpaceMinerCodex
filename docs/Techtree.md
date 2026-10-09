# Tier-I-Techtree

Bildvorschläge für alle elf bisher besprochenen Chapter-1-Bereiche samt gemeinsamer Einstiegsübersicht und Knotenzählern: [Techtree-Galerie und Ausarbeitung vom 08.10.2026](Techtree/README.md). Enthält alle zwölf ausgewählten PNGs und dokumentierte Voraussetzungen; Gestaltungsvorschläge, keine neue Menü-/Gameplayimplementierung.

## Bedienung und bestätigte Darstellung

Das Verzweigungs-Symbol rechts oben, unmittelbar links neben dem Settings-Zahnrad, öffnet den Forschungsbaum. Beide Einstiege zeigen nur ein Symbol und besitzen einen Tooltip; sie bleiben auch bei verborgenem HUD verfügbar.

Tier I läuft von links nach rechts. Die Icons tragen keine dauerhaften Textbeschriftungen. Seit 09.10.2026 öffnet ausschliesslich ein Klick Namen, Status, Voraussetzungen und praktische Nutzung als Overlay. Hover öffnet keine Information. Vorausgehende Verbindungen werden hervorgehoben. Das × in der Information löst die Auswahl, das × oben rechts oder Escape schliesst das Menü.

Jede Verbindung besitzt eigene kleine quadratische Eingangs- und Ausgangsanschlüsse. Mehrfachanschlüsse sind versetzt. Kanten sind rechtwinklig; Verbindungen auf gleicher Höhe laufen direkt waagrecht und benötigen keine mittigen Anschlüsse. Navy-Flächen, Amber-Akzente, Cyan-Systemkanten und UI-Töne verwenden den vorhandenen Settings-Stil.

Settings und Techtree sind gegenseitig ausschliessende Menüs. Kamera, Auswahl, HUD und Display-Shortcuts werden modal gesperrt, auch im Schliessframe. Die Simulation folgt der vorhandenen Player-Einstellung `PauseInMenu`. Das Intro pausiert einschliesslich Stimme. Der vorhandene Configuration-Hintergrund wird auch im Techtree genutzt.

## Umfang und offene Forschung

Wasserabbau besitzt seit 06.10.2026 eine erste Erfahrungsprogression. „Wasserabbau optimieren“ zeigt Wissenslevel, Fahrtenfortschritt, tatsächliche Förderrate und Abbauleistung. Klicken heftet das Overlay an; darin wählt der Spieler Förderrate oder Energieeffizienz für den nächsten Aufstieg. Gemeinsames Materialwissen startet auf Level 1, 20 produktive beendete Fahrten ergeben einen Aufstieg. Förderrate steigt pro zugeordnetem Level um 15 %, elektrische Abbauleistung sinkt bei Effizienz um 15 %; beide werden auf dem vorherigen Wert fortgeschrieben. Schwerpunktwechsel erhält gelernte Verbesserungen. Dies verändert keine Hardwaregeneration und verbraucht keine Forschungspunkte. Details, Testparameter und gemessene Zeiten: [Bergbaudrohnen](Bergbaudrohnen.md).

Die anderen Forschungsfelder bleiben Entwürfe ohne Forschungsverbrauch, Forschungszeiten oder zusätzliche Freischaltungen. Speichern/Laden fehlt auch für das neue Abbauwissen. Die genaue Technologiekette, Materialeigenschaften und Tier-Aufstieg bleiben offen. Reale Verfahren müssen vor ihrer Umsetzung fachlich recherchiert werden.

Grundprinzip für spätere Progression: Tätigkeit erzeugt fachbezogenes Wissen, Entdeckung öffnet Forschungswege, Forschung erweitert Möglichkeiten. Wissen, Maschinen, Ressourcen und Energie sind getrennte Voraussetzungen. Vorhandene Technik wird nicht erneut erforscht. Frühere Tiers sollen nachholbar bleiben. Tier II bis VI sind noch nicht ausgearbeitet.

## Wiederkehrende Reparaturen und Lernen durch Tätigkeit (08.10.2026)

Nutzerentscheidung: Anlagen werden durch Beanspruchung verschlissen und benötigen auch nach der ersten Wiederherstellung wiederkehrende Reparaturen. Wiederholte Reparaturen erhöhen die praktische Erfahrung und machen ihre Ausführung effizienter. Dieses Lernen durch Tätigkeit soll überall dort gelten, wo es fachlich sinnvoll ist, beispielsweise auch bei Abbau und Fertigung. Neue Verfahren und Hardware bleiben eigene Forschungs-/Umbaufortschritte.

Am jeweiligen Techtree-Icon wächst ein gelber beziehungsweise amberfarbener Fortschrittsrand bis zum nächsten Erfahrungslevel. Beim Levelaufstieg bleibt die erlernte Verbesserung erhalten; für das folgende Level beginnt ein neuer Füllzyklus (Darstellungsvorschlag). Die vorhandenen versetzten Anschlussknoten und Voraussetzungskanten müssen dabei sichtbar bleiben. Das Hover-/Klick-Overlay soll Tätigkeit, Erfahrungslevel, Fortschritt, aktuellen Effizienzgewinn und die nächste Verbesserung erklären. Anlagenzustand/Verschleiß und Erfahrung sind getrennte Werte; der gelbe Rand zeigt den Lernfortschritt.

Ausarbeitungsvorschläge: tatsächlich erledigte Arbeit als Lernquelle zählen; Reparaturerfahrung kann Arbeitszeit oder vermeidbare Material-/Energieverluste verbessern. Erforderliche Ersatzteile werden dadurch nicht beliebig eingespart. Gemeinsames Stationswissen passt zum vorhandenen Wasserabbauwissen, ist für Reparaturen aber noch nicht festgelegt. Offen: welche Tätigkeiten eigene Erfahrung führen, Gewichtung/Levelschwellen, genaue Boni und Grenzen, Verschleißmodell, Wartungsintervalle sowie manuelle beziehungsweise automatische Aufträge. Noch keine allgemeine Erfahrungs-, Verschleiß- oder Randdarstellung implementiert.

## Chapter-1-Forschungsbereiche und Mira (08.10.2026)

Nutzerentscheidung: Mira ist die KI der Station mit Avatar und begleitet Forschung durch Vorschläge und Projekte mit zusätzlichem Energiebedarf. Erstes Projekt ist die Umfunktionierung der vorhandenen Abbau-Außendrohnen zur Bau-/Reparaturrolle; die aktivierte kleine interne Helferdrohne setzt den Umbau im Dock um. Forschung und Hardwareausführung bleiben getrennt. Spielerbestätigung/Energiezuteilung, Nutzung von Archiven/Sensordaten/Proben, Forschungszeiten und Parallelisierung sind Ausarbeitungsvorschläge beziehungsweise offene Regeln. Noch keine Online-KI oder Forschungsmechanik implementiert.

Für Drohnen ist ein eigener Baum mit Zweigen für Abbau, Bau/Reparatur, Erkundung und interne Helfer vorgesehen. Vier Arten, Anzahl der Fahrzeuge offen; weitere Arten später. Abbau ist vorhandenes Anfangswissen. Die technische Umbaubarkeit zwischen kleineren Innen- und Außendrohnen ist noch zu prüfen; keine neue Generation allein durch Rollenwechsel.

Weitere Entwicklungsbereiche: Stromversorgung mit Batterien/Solartechnik, Ressourcenwissen und Verarbeitbarkeit, komplexe Produktion mit passenden Containertypen, Stationsringe/Module sowie Scannerreichweite und erfassbare Informationen. Struktur als einzelne Bäume oder gemeinsame Darstellung, konkrete Knoten, Tiers und Chapter-1-Grenzen bleiben offen. Detailliertes Zielbild und vorgeschlagene erste Questfolge: [Spielidee](Spielidee.md#chapter-1-drohnen-mira-forschung-und-ausbauziele-08102026). Glasdom bei Spielstart und Helferaktivierung offen.

Im TechTree-Gespräch bestätigte Arbeitsgliederung: Energieversorgung; Drohnen und Robotik; Scanner und Erkundung; Materialkunde und Aufbereitung; Produktion und Fertigung; Lagerung und Logistik; Lebenserhaltung und Nahrung; Stationsbau und Infrastruktur. Mira begleitet die Bereiche als gemeinsame Forschungsinstanz. Ein generiertes Energiebaum-Bild dient der Diskussion: Solar, Speicher, Stromverteilung und Wärmeabfuhr mit Voraussetzungen aus Drohnen/Produktion. Seine konkreten Kanten, Boni und Kapitelabschlussbedingungen sind noch nicht beschlossen.

## Mira-Entwicklung und Forschungsstufen (08.10.2026)

Nutzerentscheidung: eigener Entwicklungsbaum für Mira. Wiederholte Forschungsarbeit soll wie andere Tätigkeiten die Effizienz verbessern und Forschung beschleunigen; einzelne Technologien verlangen eine Mindest-Forschungsstufe, beispielsweise Stufe 2. Gelber/amberfarbener Erfahrungsrand entsprechend dem gemeinsamen Darstellungsprinzip. Zusätzliche CPUs und mehr verfügbare elektrische Leistung erhöhen die Performance; selbst hergestellte neue CPUs setzen geeignete Chipherstellung voraus. Forschungsstufe ist eine Fähigkeit Miras, getrennt von Kapitelnummer und Techtree-Tier. Vorschlag mit Forschungsfähigkeit, Rechenhardware, Energie/Kühlung, Archiven, Analyse/Planung und Koordination: [Mira-Entwicklung](Mira-Entwicklung.md). Exakte Erfahrungsschwellen, Geschwindigkeitsboni, Leistungswerte, Freischaltungen, Parallelisierung und Chapter-1-Grenzen offen; keine Implementierung.

## Beschlossene Ausbaurichtung: Akku und Ladestation (06.10.2026)

Der Nutzer bestätigt ungefähr 1:2 als Startziel für aktive Abbauzeit gegenüber dem Nachladen der dafür verbrauchten Energie. Flug, Bordversorgung, Ladeverluste und Ladeendphase kommen hinzu; dies ist kein fest programmierter Wartezeitfaktor und noch nicht in die aktuelle Testbalance übernommen.

Akku und Ladestation sollen durch spätere Tiers deutlich verbessert werden können. Für die Ausarbeitung getrennt führen: Akkukapazität in kWh für längere Einsätze; zulässige Lade-/Entladeleistung für stärkere Werkzeuge und Schnellladung; Ladeleistung der Station in kW für kürzere Aufenthalte. Konkrete Technik, Tiernummern, Faktoren, Kosten und Freischaltungen bleiben offen. Energieerzeugung, Stromverteilung und Wärmeabfuhr müssen die jeweilige Ausbaustufe unterstützen. Ein größerer Akku allein ist kein Schnellladebonus; Ladestation und Akku müssen zueinander passen.

Diese Fortschritte gehören zur erforschten Hardware und den bestehenden Umbau-/Neubauprinzipien, getrennt von der bereits implementierten Materialerfahrung. Die geplante Batteriewechselstation bleibt eine zusätzliche Logistikoption, siehe [Ideenbacklog](Ideenbacklog.md). Noch keine neuen Techtree-Knoten oder Hardwareeffekte implementiert.

## Wiederverwertung als eigener Bereich (08.10.2026)

Nutzerentscheidung: neunter Entwicklungsbereich Wiederverwertung. Trümmer liefern vollständige Bestandteile wie Akkus und Hüllenplatten; weiterer Pfad über kleinere Teile bis zu nutzbaren Rohstoffen. Für gezielte Rohstoffrückgewinnung muss der betreffende Stoff erforscht sein. Konkreter Vorschlag mit gemeinsamer Bergung/Diagnose, den drei Wegen Wiederverwendung, Demontage und Materialrückgewinnung sowie externen Voraussetzungen: [Wiederverwertung](Wiederverwertung.md). Unbekannte Anteile bleiben getrennte Funde/Fraktionen. Ganze geprüfte Teile wiederverwenden und ihre Rohstoffe isolieren sind unterschiedliche Fähigkeiten; Rohstoffausgänge können geeignete Verbindungen/Legierungen sein. Knoten, Ausbeuten und Kapitelgrenzen noch offen; keine Implementierung.

## Rohstofferkennung, Trennung und nutzbare Werkstoffe (08.10.2026)

Nutzerentscheidung: Für die einzelnen Anlagen benötigte Rohstoffe/Werkstoffe sammeln, zugängliche Bezugswege durch Abbau beziehungsweise Bergung vorsehen und ihre Erkennung sowie Trennung vor der Nutzung erforschen. Materialnachweis, Gehalt/Bindungsform, Trenn-/Gewinnungsverfahren und nutzbare Produktqualität sind getrennte Voraussetzungen. Bekannte Starttechnik wird nicht neu erfunden. Erste Material-/Bauteilsammlung, vorgeschlagene Energiebaum-Abhängigkeiten und fachliche Quellen: [Werkstoffe und Rohstoffe](Werkstoffe-und-Rohstoffe.md). Mengen, konkrete Fundorte, Akku-/Solartechnik und Grenzen der Eigenfertigung bleiben offen; keine neuen Spielrezepte oder Forschungsmechaniken implementiert.

## Verbindungen und Manufaktur-Zwischenprodukte (08.10.2026)

Auf Nutzerauftrag zusätzlicher Bild-/Strukturentwurf mit21 Knoten: organische Grundchemie, Silikonchemie, Akkumaterialien, Isolierpolymere sowie Oxide/Harze. Stoffkenntnis, Zwischenprodukte, Verfahren und reale Herstellung getrennt. Bauartabhängige Beispiele, noch keine verbindlichen Rezepte oder Chapter-1-Freischaltungen. Elektrolyt aus getrennten Leitsalz-/Lösungsmittelzweigen; Kathodenmaterial ist ein eigener Pfad. Struktur, Quellen und offene Anforderungen: [Verbindungen und Zwischenprodukte](Verbindungen-und-Zwischenprodukte.md).

## Realistische Produktionsketten und Solartechnik (08.10.2026)

Nutzerentscheidung: komplexe Produktion mit möglichst realen Zutatenlisten; Zeitraffer wird befürwortet. Für neue Rezepte technische Bauart und Funktion wählen, dann Produktbestandteile, Prozesshilfsstoffe, Maschinen, Energie/Wärmeabfuhr und Fertigungswissen getrennt ausarbeiten. Rückgewinnung, Verluste und Reinheit sollen nachvollziehbar sein; konkrete Anforderungen und Mengen bleiben zu prüfen.

Chapter 1 setzt den Fokus auf Planung, Produktion, Sammeln, Reparieren und Erkunden; Forschung beginnt dort mit ersten Ansätzen. Welche Herstellungsstufen schon eigenständig möglich sind und welche zunächst Bergung/Reparatur erfordern, muss gegen diesen Kapitelumfang ausgearbeitet werden. Daraus ergeben sich noch keine festen Tierzuordnungen oder Forschungskosten.

Erste recherchierte Orientierung, keine beschlossene Spielrezeptur: Bei kristallinen Silizium-Modulen folgen auf die Siliziumreinigung Kristall-/Waferfertigung und Zellfertigung mit Dotierung, Oberflächenschichten und Kontakten. Typische terrestrische Modulmontage nutzt Kupferverbinder, Lötmaterial, Glas, Einkapselungspolymere, Rückseite, Aluminiumrahmen und Anschlusskomponenten. Prozessgase können teilweise im Kreislauf geführt werden. Quelle: [US DOE, Solar Photovoltaic Manufacturing Basics](https://www.energy.gov/cmei/systems/solar-photovoltaic-manufacturing-basics).

Diese terrestrische Bauweise ist nicht automatisch eine Stationsrezeptur. Raumfahrt-PV muss unter anderem Vakuum, Strahlung und Temperaturwechsel berücksichtigen; Silizium und Mehrfachsolarzellen sind unterschiedliche Technologiepfade. Quelle: [US DOE, Space Photovoltaics Basics](https://www.energy.gov/cmei/systems/space-photovoltaics-basics). Raumfahrttaugliche Bauart, genaue Materialien und Mengen sind offen.

Ausarbeitungsvorschlag: frühe Reparaturen mit geborgenen Bauteilen und später zunehmende Eigenfertigung. Dies ist noch keine Nutzerentscheidung zur Freischaltreihenfolge. Keine neuen Produktionsanlagen, Rezepte oder Techtree-Knoten implementiert. Prüfung: genannte Primärquellen am 08.10.2026 gelesen, Dokumentationsdiff geprüft; keine Materialbilanzrechnung, Builds oder Spieltests.

## Nahrungspfade und chemische Synthese (08.10.2026)

Nutzer sieht drei Entwicklungspfade vor: chemische Synthese, Photosynthese mit Pflanzen/Algen sowie mikrobiologische Nahrungserzeugung im Bioreaktor. Samen beziehungsweise geeignete lebende Kulturen müssen erst entdeckt und geborgen werden; Raumschifftrümmer späterer Chapters sind dafür vorgesehen. Chemische Synthese als früher Einstieg ist ein Vorschlag, keine fertig ausgelegte oder implementierte Ernährungskette.

Recherchierte chemische Grundlage: Methanol kann aus CO₂ und Wasserstoff hergestellt werden ([US DOE, Solar Fuels](https://www.energy.gov/science/doe-explainssolar-fuels)); Wasserstoff und Sauerstoff sind über Wasserelektrolyse zugänglich. Methanol wird industriell katalytisch zu Formaldehyd umgesetzt ([Johnson Matthey, FORMOX](https://matthey.com/en/products-and-markets/chemicals/process-licensing/formox-formaldehyde-process)). Vorschlag für die Station: Wasser aufbereiten und elektrolysieren → Wasserstoff mit einer verfügbaren CO₂-Quelle zu Methanol umsetzen → Formaldehyd herstellen → Zuckerbildung → Trennung, Reinigung und Qualitätsprüfung. Herkunft und Ausbeute der Kohlenstoffquelle, Anlagenausstattung, Katalysatoren, Energiebedarf und Wärmeabfuhr sind offen; keine Mengen oder Tiernummern beschlossen.

Die Formose-Reaktion bildet aus Formaldehyd komplexe Gemische, darunter Zucker. Selektivität und Prozesskontrolle sind Forschungsfragen; daraus folgt keine bereits nachgewiesene, verlässliche Lebensmittelproduktion für die Station ([Communications Chemistry, 2025](https://www.nature.com/articles/s42004-025-01560-9)). Formaldehyd ist ein Prozesszwischenprodukt, keine Nahrung. Lebensmittelreinheit und weitere Nährstoffe wie Proteine, Fette, Vitamine und Mineralstoffe müssen separat gelöst werden. Frühe vorhandene Syntheseanlage und Notrationen als Überbrückung nur Ausarbeitungsvorschlag.

Asteroidenbezug: Hexamethylentetramin (HMT) wurde in drei kohlenstoffreichen Meteoriten nachgewiesen; es kann unter geeigneten Bedingungen mit flüssigem Wasser Formaldehyd und Ammoniak freisetzen. Freies Formaldehyd ist flüchtig ([NASA, 2020](https://www.nasa.gov/solar-system/key-building-block-for-organic-molecules-discovered-in-meteorites/)). Das stützt organische Vorläufer als möglichen Fundstoff, belegt aber weder ergiebige Formaldehydlagerstätten noch wirtschaftliche Extraktion. Asteroidenabbau solcher Stoffe und industrielle Stationsausbeute bleiben zu prüfen. Keine lebenden Mikroorganismen aus diesem Nachweis abgeleitet.

Nutzer bestätigt Wasser und Kohlenstoff als gesuchte Grundrohstoffe, mit Energie für die chemische Verarbeitung. CO₂ muss dann nicht als eigene Lagerstätte vorliegen: Sauerstoff aus Wasserelektrolyse kann geeigneten elementaren Kohlenstoff zu CO₂ oxidieren. Die Rohstoffform bestimmt jedoch die tatsächliche Prozesskette; Kohlenstoff in organischen Verbindungen oder Carbonaten ist nicht ohne Aufbereitung als reines C verfügbar. Wasser/Kohlenstoff decken nur die Elementbasis C/H/O für mögliche Kohlenhydrate, nicht die vollständige Nährstoff- oder Anlagenversorgung.

Recherche zur Kohlenstoffform: Ryugu-Proben enthalten komplexe organische Materie in mineralischem Material ([Nature Communications, 2024](https://www.nature.com/articles/s41467-024-50004-w)) und Calcium-/Magnesiumcarbonate ([Nature Geoscience, 2023](https://www.nature.com/articles/s41561-023-01226-y)). Elementarer Kohlenstoff kann als Graphit oder Diamant vorliegen; Graphit und kleine Diamanten sind unter anderem in Ureilit-Meteoriten nachgewiesen ([PNAS, 2020](https://pmc.ncbi.nlm.nih.gov/articles/PMC7568235/)). Solche Einschlüsse belegen keinen ganzen Diamantasteroiden oder ergiebige abbaubare Lagerstätten. Spielvorschlag: zuerst kohlenstoffhaltiges Material orten, später Bindungsform, Gehalt und erforderliche Aufbereitung analysieren. Diamanten als gesonderte Werkzeug-/Technikressource nur mögliche Ausarbeitung, keine Nutzerentscheidung.

Prüfung: genannte Quellen am 08.10.2026 gelesen und Dokumentationsdiff geprüft; keine Material-/Energiebilanz, neue Spielrezepte, Builds oder Laufzeittests.

## Dateien und Prüfung

- `TechnologyCatalog.cs`: Tier-I-Anzeigeeinträge, Voraussetzungskanten und `TechnologyLayout`; kein Simulationszustand.
- `MiningResearch.cs`: gemeinsames Wasserabbauwissen, gewählter Lernschwerpunkt und verdiente Verbesserungslevel; Anwendung durch `DroneAgent`.
- `TechnologyIcons.cs`: lokale Piktogramme ohne Schriftglyphen-/Downloadabhängigkeit.
- `TechTreeMenu.cs`: IMGUI-Menü, Overlays und modale Bedienung; Laufzeitinstallation mit Settings.
- `SettingsMenu.cs`, `IntroSequence.cs`, `ConfigurationAudio.cs`, `SettingsUiAudio.cs`: gezielte Menüintegration.
- `Editor/TechTreeValidation.cs`: Daten-/Routingprüfung und Development-/Release-Build ohne Szenen-Neugenerierung.

Bei geschlossenem Unity-Editor `SpaceMiner.Editor.TechTreeValidation.Build` im Batchmodus ausführen. Development unter `Builds/Windows`, Release unter `Builds/TechTreeRelease`. Das Development-Spiel mit `-techTreeCheck` prüft Menüintegration und erzeugt `Logs/techtree-*.png`. Der vollständige Spieltest bleibt `tools/Unity.ps1 Check -Visible`. Nur tatsächlich ausgeführte Ergebnisse werden im Projektmemory festgehalten.

### Prüfung am 06.10.2026

Unity 6000.4.7f1: finale Development- und Release-Builds erfolgreich; Daten-/Routingprüfung mit 17 Knoten und 22 Kanten bestanden. Menütest einschliesslich aktiver Drohnenpause, Ressourcenbilanz, Settings-Wechsel und Schliessframe mit Exit 0 bestanden. Die bestehende Spielintegration bestand 789 Prüfungen; anschliessend wurde nur der Overlay-Eingabeschutz geändert, erneut gebaut und menügeprüft. Einstieg, Baum, Overlay und 140%-Schrift mit High Contrast anhand der Screenshots visuell kontrolliert. Protokolle: `Logs/techtree-complete-build.log`, `Logs/techtree-complete-check.log`, `Logs/techtree-smoke.log`. Eine rein subjektive Hörprüfung wurde in dieser Sitzung nicht vorgenommen.

Lokale Erweiterung Wasserabbau: Development-/Release-Builds und Routingprüfung mit Unity 6000.6.4f1 in separater Vorschaukopie bestanden; 191 Balanceprüfungen und 821 Spielprüfungen bestanden. Neues Wissensoverlay und Schwerpunktknöpfe bei normaler und 140-%-Schrift/High Contrast visuell geprüft. Letzte Änderung nur am Overlaylayout; erneut gebaut und Balance-/Bildprüfung bestanden. Lokale Logs: `Builds/LocalPreview-6000.6.4f1/Logs/mining-*`. Projektversion 6000.4.7f1 blieb unverändert und wurde hier nicht geprüft.

## Debug-Forschungslabor (09.10.2026)

Die elf neuen Entwurfsbereiche sind im Editor/Development-Player als separate Forschungssimulation erreichbar: Techtree → Forschungslabor [Debug]. Alle 159 Kategorieeinträge, Startwissen, Wissensbedingungen, Warteschlange, Pause/Tempo, Verbesserungen und getrennte Laborsicherung. Bedienung, Modellgrenzen und Prüfung: [Forschungslabor](Forschungslabor.md). Die ursprüngliche Spielübersicht und echte Wasserabbau-Erfahrung bleiben erhalten; vollständige Hardwareproduktion ist weiter offen.
