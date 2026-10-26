# Mira — Dialoge für die ersten neun Quests

Stand: 26.10.2026. Nutzerwunsch: Für jede Quest erklärt Mira, was konkret zu tun ist und warum. **Vertont und in Spiel sowie Dialogstudio umgesetzt.** Die neun vorhandenen Quests haben bereits kurze, anklickbare Erklärtexte. Diese Fassung ergänzt Motivation, Handlungsfolge und eine eigene Abschlussreaktion. Sie verändert weder Questbedingungen noch Balance.

Grundlage: `StationOperations.Ui.cs` (Questliste und `QuestDone`), `StationOperations.cs` (Voraussetzungen/Abschluss), `StationWork.cs` (Rezepte), `StationOperations.DroneConsole.cs`, `StationOperations.DroneCommands.cs` und `StationOperations.TacticalOverview.cs`. Der aktuelle Code entscheidet über tatsächlich mögliche Aktionen. Spätere Chapter-1-Ziele sind noch keine zusätzlichen implementierten Quests.

## Gemeinsame Regie und Einbindung

Mira spricht Deutsch, duzt den Spieler und erklärt aus ihrer eigenen Perspektive. Warm, ruhig, verständlich; bei konkreten Arbeitsanweisungen konzentriert, bei Erfolgen dezent ermutigend. Keine künstliche Alarmstimme oder großen Jubelgesten. Kleine Kopfbewegungen und Blickpausen unterstützen die Aussage. Körper- und Lippenzeitdaten entstehen erst zur tatsächlichen Aufnahme; hier stehen keine erfundenen Zeitmarken.

Die Einführungen beantworten **Anlass → Handlung → Nutzen**. Der kurze Questtext bleibt als abhakbares Ziel sichtbar; technische Rezeptmengen und aktuelle Messwerte stehen zusätzlich im Pult. Tasten gehören in den dynamischen Bedienhinweis, damit eine Aufnahme nach einer Neubelegung weiter stimmt. Drohnenaufträge werden am realen Drohnenleitstand erteilt. Die virtuelle Außenansicht dient zum Beobachten und Fokussieren.

Auslösung im Spiel: Einführung einmal beim erstmaligen Aktivwerden, auf Klick erneut abrufbar; Abschluss einmal beim Wechsel der echten Questbedingung von offen zu erfüllt. Bei geladenen abgeschlossenen Quests keine alten Erfolgsansagen nachholen. Ereignisse sammeln, während Mira spricht; keine laufende Aufnahme überschreiben. Bei übersprungenem Intro bleibt die erste Quest erklärt. Die automatische Regie und ihre gespeicherte Anzeigehistorie sind implementiert; bei bereits erledigten Aufgaben wiederholt ein Klick die Abschlussfassung. Die vorhandene Aufgabenliste lässt sich bereits anklicken.

Die lineare Reihenfolge ist eine Orientierung. Arbeiten dürfen im Spiel parallel erfolgen. Bereits erledigte Schritte werden übersprungen; keine neue Arbeit oder erneute Materialausgabe allein für einen Dialog verlangen. Ein Abschluss darf auch dann funktionieren, wenn die nächste Quest schon erledigt ist. Die bestehende Liste bleibt nach allen neun Abschlüssen auf dem letzten Index; der Dialogdispatcher prüft deshalb zusätzlich, ob die letzte Aufgabe tatsächlich erledigt ist, und startet dann keine neue Einführung.

IDs beginnen bei `quest_00`, entsprechend den vorhandenen Codeindizes. Es folgen **18 Haupttexte und vier ergänzende Übergänge**. Eine maschinenlesbare Textfassung liegt daneben in `02_Aufbauphase_Questdialoge.json`. Audiofelder verweisen auf die lokalen Ressourcen unter `MiraQuests/`. Vollständige Hördateien je Quest liegen zusätzlich unter `Sprachproben/Quests_Aoede_2026-10-26/Komplette_Quests/`.

## 1. Umgebung scannen

**Ziel:** Ersten Nahbereichsscan am Scannerpult erfolgreich auslösen. Erfüllt bei `FirstScanComplete`.

**ID:** `quest_00_start` · **Stimmung:** konzentriert

> Bevor wir unsere einzige einsatzbereite Drohne losschicken, müssen wir wissen, was uns umgibt. Geh an das Scannerpult und löse den Nahbereichsscan aus, sobald der Scanner bereit ist. Damit finden wir mögliche Wasserquellen und nahe Wrackteile. In der virtuellen Außenansicht erscheinen anschließend die erfassten Kontakte. Was darin steckt, müssen wir erst noch untersuchen.

**ID:** `quest_00_complete` · **Stimmung:** ermutigend

> Der erste Scan ist abgeschlossen. Wir haben jetzt eine Grundlage für unsere nächsten Einsätze. Die erfassten Kontakte und bestätigten Rohstoffe findest du in der Übersicht. Für den nächsten Schritt brauchen wir eine bestätigte Eisquelle.

**Hinweis zur Einbindung:** Bestätigte Quellen aus dem aktuellen Ergebnis anzeigen; bei keinem geeigneten Fund einen passenden Ersatztext auslösen. Keine festen Asteroiden-/Wrackzahlen aufnehmen. Die vorhandene ausführliche Scanmeldung soll durch diesen Übergang ersetzt oder mit ihm zusammengeführt werden, damit Mira nicht doppelt erklärt.

## 2. Wasser sichern

**Ziel:** Erste Lieferung und mindestens40 Liter sauberes Verbrauchswasser erreichen. `State.waterSecured` speichert das erreichte Ziel; der Tank muss nicht voll sein.

**ID:** `quest_01_start` · **Stimmung:** konzentriert

> Unser Wasservorrat ist begrenzt. Wir brauchen ihn für die Versorgung und als Treibwasser für die Drohnen. Öffne den Drohnenleitstand, wähle Drohne null eins und erteile ihr einen Roh-Eis-Auftrag an einer bestätigten Eisquelle. Der Bohrer muss montiert sein. Die Annahme lagert das Rohmaterial; der Aufbereiter gewinnt daraus mit Stationsstrom sauberes Wasser. Unser erstes Ziel ist eine Lieferung und eine Reserve von vierzig Litern. Das große Roh-Eis-Lager muss dafür nicht voll werden.

**ID:** `quest_01_complete` · **Stimmung:** ermutigend

> Die erste Lieferung ist angekommen, und unser erstes Wasserziel ist erreicht. Das gibt uns etwas Spielraum. Behalte die Aufbereitung im Blick: Roh-Eis im Lager und nutzbares Wasser im Tank sind zwei verschiedene Vorräte.

**Hinweis zur Einbindung:** Keine Aussage „Tank voll“. Das Ziel bleibt erfüllt, auch wenn später Wasser verbraucht wird. Fehlt bei der Wiederholung noch die Lieferung oder das Flüssigwasser, den jeweiligen echten Fortschritt daneben anzeigen. Während Mira oder ein Pult pausiert, keinen vermeintlichen Arbeitsfortschritt versprechen.

## 3. Versorgung diagnostizieren

**Ziel:** Auftrag „Versorgung diagnostizieren“ abschließen. Erfüllt bei `State.diagnosed`.

**ID:** `quest_02_start` · **Stimmung:** konzentriert

> Bevor wir weitere Anlagen zuschalten, sollten wir unsere Versorgung prüfen. Starte die Versorgungsdiagnose am Stationsplanungspult. Sie hilft uns, die nächsten Arbeiten vorzubereiten. Am Strompult kannst du Erzeugung, Verbrauch und Akkureserve verfolgen. Prüfe auch die Verbindungen der benötigten Sektoren. Eine intakte Maschine kann nicht arbeiten, wenn ihr Strom oder Wasser fehlt.

**ID:** `quest_02_complete` · **Stimmung:** ermutigend

> Die Versorgungsdiagnose ist abgeschlossen. Damit können wir die weiteren Inbetriebnahmen planen. Die Diagnose selbst hat noch nichts repariert. Wenn eine Arbeit wartet, prüfen wir zuerst ihren Versorgungsweg und die verfügbaren Reserven.

**Hinweis zur Einbindung:** Die Diagnose setzt aktuell einen Zustandswert; keine erfundenen Messberichte, neue Energie oder automatische Schadensbehebung behaupten. Strom-/Wassersektoren bleiben einzeln bedienbar.

## 4. Trümmer bergen

**Ziel:** Einen verfügbaren bekannten Wrackkontakt bergen; erfüllt bei `State.debrisBatches > 0`.

**ID:** `quest_03_start` · **Stimmung:** konzentriert

> Für Reparaturen und die interne Helferdrohne brauchen wir brauchbare Teile. In den georteten Trümmern könnten noch welche erhalten sein. Wähle am Drohnenleitstand einen verfügbaren Wrackkontakt und erteile einen Bergungsauftrag. Drohne null eins braucht dafür einen Bohrer oder Greifer. Was wir tatsächlich verwenden können, zeigt sich nach der Bergung. Unbekanntes Material halten wir getrennt.

**ID:** `quest_03_complete` · **Stimmung:** ermutigend

> Die erste Bergung ist abgeschlossen. Die geborgenen Bestände sind erfasst. Geprüfte Ersatzteile können wir direkt für die ersten Inbetriebnahmen verwenden. Den übrigen Schrott müssen wir erst aufbereiten, bevor daraus neues Material wird.

**Hinweis zur Einbindung:** Der gegenwärtige Bergungsauftrag liefert zwölf Einheiten Schrott und zwei Ersatzteile. Diese Prototypmengen bleiben im Rezept-/Ergebnisfeld; unbekannte Kontakte verraten vor ihrer Bergung keine Inhaltsliste.

## 5. Interne R-01 aktivieren

**Ziel:** Inbetriebnahme der internen Helferdrohne abschließen; erfüllt bei `State.helper`. Aktuell zwei geprüfte Ersatzteile und Stationsstrom, keine bereits aktive R-01 erforderlich.

**ID:** `quest_04_start` · **Stimmung:** ermutigend

> Wir können die kleine interne Helferdrohne wieder in Betrieb nehmen. Sie wird hier als R null eins geführt. Wähle sie im internen Bereich des Drohnenleitstands aus und beauftrage ihre Inbetriebnahme. Dafür brauchen wir zwei geprüfte Ersatzteile. Sobald sie arbeitet, kann sie interne Arbeitsaufträge übernehmen: Material aufbereiten, Teile fertigen und unsere Außendrohnen umrüsten. So werden wir Schritt für Schritt unabhängiger von den geborgenen Vorräten.

**ID:** `quest_04_complete` · **Stimmung:** ermutigend

> R null eins ist in Betrieb. Damit haben wir Unterstützung innerhalb der Station. Du kannst ihr jetzt Arbeitsaufträge zuweisen. Plane ihre Reihenfolge bewusst: Eine einzelne Helferdrohne kann immer nur eine Arbeit gleichzeitig übernehmen.

**Hinweis zur Einbindung:** Kampagnen-R-01 und die zehn I75 der separaten Fahrprobe nicht verwechseln. Die Inbetriebnahme erzeugt keine zusätzliche Flotte. „R null eins“ ist die gesprochene Form; in der UI bleibt „R-01“.

## 6. Kohlenstoff beschaffen

**Ziel:** Erste bekannte Kohlenstoff-Prozessprobe bergen; erfüllt bei `State.carbonBatches > 0`. Diagnose und freie Außendrohne mit Bohrer erforderlich.

**ID:** `quest_05_start` · **Stimmung:** konzentriert

> Wasser allein reicht für unsere weitere Versorgung nicht aus. Als nächsten Ausgangsstoff brauchen wir Kohlenstoff. Nach der Versorgungsdiagnose stehen uns dafür bekannte Prozessproben zur Verfügung. Wähle eine davon am Drohnenleitstand und schicke eine freie Außendrohne mit Bohrer hin. Wir können den nutzbaren Kohlenstoffanteil gewinnen. Was wir noch nicht bestimmen können, bleibt getrennt im Lager.

**ID:** `quest_05_complete` · **Stimmung:** ermutigend

> Die erste Kohlenstoffprobe ist geborgen. Der nutzbare Anteil ist eingelagert, der unbekannte Rest bleibt erhalten. Damit haben wir einen weiteren Ausgangsstoff für unsere vorhandene Syntheseanlage. Für Grundnahrung brauchen wir außerdem Wasser, Strom und unsere Nährstoffzusätze.

**Hinweis zur Einbindung:** Kein reiner Kohlenstoffasteroid oder Diamantfund behauptet. Die sechs bekannten Prozessproben sind endliche Quellen des derzeitigen Prototyps; unbekannte Zusammensetzungen nicht vorwegnehmen.

## 7. Grundnahrung herstellen

**Ziel:** Einen nicht abgebrochenen Auftrag „Grundnahrung synthetisieren“ abschließen; erfüllt bei `State.foodProduced` beziehungsweise dem entsprechenden abgeschlossenen Auftrag.

**ID:** `quest_06_start` · **Stimmung:** konzentriert

> Unsere Notrationen reichen nicht unbegrenzt. Die vorhandene Syntheseanlage kann uns zusätzliche Grundnahrung liefern. Öffne die Fertigung und beauftrage eine erste Charge. R null eins übernimmt die Arbeit. Wir brauchen Kohlenstoff, Wasser, Strom und eine Nährstoffkartusche aus dem Vorrat. Diese Zusätze können wir noch nicht selbst herstellen. Achte deshalb auf ihren Bestand und darauf, dass die Wasserleitung zur Anlage offen ist.

**ID:** `quest_06_complete` · **Stimmung:** ermutigend

> Die erste Charge Grundnahrung ist fertig und eingelagert. Das entlastet unseren Rationsvorrat. Vollständig unabhängig sind wir damit noch nicht: Die Nährstoffkartuschen sind endlich. Für eine dauerhafte Versorgung brauchen wir später weitere Möglichkeiten.

**Hinweis zur Einbindung:** Aktuelles Spielrezept:0,4kg Kohlenstoff,1L Wasser und eine Kartuscheneinheit ergeben vier Portionen; R-01/Strom/Leitungen/Anlagenzustand sind echte Voraussetzungen. Die vereinfachte Spielchemie bleibt in der Fachdokumentation als Prototyp gekennzeichnet. Mira behauptet weder eine vollständige Ernährung aus C/H/O noch eine neu validierte chemische Synthese. Keine spontan schon verfügbaren Samen oder Kulturen erfinden.

## 8. Zweite Drohne aktivieren

**Ziel:** Drohne02 instand setzen; erfüllt bei `State.second`. Voraussetzungen aktuell Diagnose, aktive R-01, zwei Ersatzteile, mindestens40L Wasser und6kWh Stationsreserve. Danach laufen echtes Laden und Betanken.

**ID:** `quest_07_start` · **Stimmung:** konzentriert

> Mit einer zweiten Außendrohne könnten wir mehrere Aufgaben parallel erledigen. Lass R null eins deshalb Drohne null zwei instand setzen. Wähle sie im Drohnenleitstand und plane die Instandsetzung. Dafür brauchen wir die abgeschlossene Versorgungsdiagnose, zwei Ersatzteile und ausreichende Reserven: mindestens vierzig Liter Wasser und sechs Kilowattstunden im Stationsakku. Nach der Reparatur muss sie am Dock noch geladen und betankt werden. Erst dann planen wir ihren ersten Einsatz.

**ID:** `quest_07_complete` · **Stimmung:** ermutigend

> Die Instandsetzung von Drohne null zwei ist abgeschlossen. Prüfe jetzt ihren Dockstatus. Laden und Betanken benötigen weiterhin unsere Stationsvorräte. Sobald sie einsatzbereit ist, kannst du ihr einen eigenen Auftrag geben.

**Hinweis zur Einbindung:** Reparaturabschluss ist nicht gleich Einsatzbereitschaft. Keine kostenlose Ladung oder sofort vollgetankte Drohne zusagen. Der im Drohneninterface vorgeschlagene alternative Ablauf „Notversorgung vor Werkstatt“ ist noch kein Ersatz für das aktuelle Backend.

## 9. Station reparieren und Reparaturrolle erforschen

**Ziel:** Reparaturrolle erforschen und ein Stationselement tatsächlich instand setzen; erfüllt erst bei `State.repairExperience > 0`. Forschung allein reicht nicht. Zugängliche innere Ziele kann R-01 übernehmen; für Außenarbeiten sind freigeschaltete Rolle und montierter Laser an einer Außendrohne nötig.

**ID:** `quest_08_start` · **Stimmung:** konzentriert

> Jetzt können wir beginnen, die Station gezielt instand zu setzen. Starte am Forschungsterminal die Reparaturrolle. Ich erarbeite die nötigen Verfahren; dafür brauchen wir zusätzliche Energie. Danach wählst du am Stationsplanungspult ein beschädigtes, erreichbares Element und beauftragst die Reparatur. Für Außenarbeiten muss R null eins zuerst eine freie Außendrohne im Dock mit dem Laserwerkzeug ausrüsten. Halte außerdem geprüfte Ersatzteile bereit. Erst eine tatsächlich ausgeführte Reparatur bringt das betroffene Element wieder in Ordnung.

**ID:** `quest_08_complete` · **Stimmung:** ermutigend

> Unser erster Reparaturauftrag ist abgeschlossen. Wir haben nicht nur ein Verfahren erforscht, sondern ein beschädigtes Element tatsächlich wiederhergestellt. Diese Erfahrung können wir für die nächsten Arbeiten nutzen. Die übrigen Schäden bleiben bestehen. Entscheide als Nächstes, welche Reparatur unserer Versorgung am meisten hilft.

**Hinweis zur Einbindung:** Keine vollständige Stationsheilung, automatische Leitungsverbindung oder zusätzliche Kapazität bei jeder beliebigen Reparatur behaupten. Auswirkungen hängen vom tatsächlich reparierten Element ab. Der Abschluss der neun aktuellen Quests ist noch kein definierter Abschluss von Chapter1.

## Ergänzende Übergänge für mehrteilige Aufgaben

Diese vier Hinweise schließen Lücken zwischen bestehenden Quests. Sie sind keine neuen Questbedingungen. Nur passend zum aktuellen Fortschritt und einmal auslösen; auf Wunsch erneut abrufbar. Bereits erledigte Teilaufgaben nicht erneut verlangen.

### Schrott aufbereiten

**ID:** `quest_support_recycle` · **Stimmung:** konzentriert

> Wir können die geprüften Ersatzteile direkt verwenden. Den übrigen Schrott muss R null eins zuerst analysieren und trennen. Beauftrage die Aufbereitung in der Fertigung. Verwertbares Material kommt in die passenden Behälter, unbekannte Rückstände bleiben getrennt. So gewinnen wir unter anderem Eisen für neue Reparaturteile.

**Auslöservorschlag:** R-01 aktiv, Schrott vorhanden, erste Aufbereitung noch nicht erledigt. Aktuelles Rezept braucht zehn Schrotteinheiten. Kein freies oder unendlich wiederholbares Material versprechen.

### Reparaturteile fertigen

**ID:** `quest_support_print_parts` · **Stimmung:** konzentriert

> Wir müssen nicht jedes Reparaturteil in einem Wrack finden. Aus aufbereitetem Eisen kann der Materialdrucker einfache neue Teile fertigen. Plane den Auftrag in der Fertigung. R null eins braucht dafür Zeit, Strom und einen freien Lagerplatz. Für komplexe Bauteile fehlen uns weiterhin die passenden Verfahren.

**Auslöservorschlag:** R-01 aktiv, genug Eisen für das tatsächliche Rezept, noch kein erfolgreicher Druckauftrag. Gegenwärtig zwei Eisen-Einheiten für zwei Teile. Diese Fähigkeit ersetzt keine Elektronik-/Akkufertigung.

### Reparaturrolle erforscht

**ID:** `quest_support_repair_researched` · **Stimmung:** ermutigend

> Die Reparaturverfahren sind ausgearbeitet. Für eine Außenreparatur wählst du jetzt eine freie Außendrohne am Dock und beauftragst den Umbau auf das Laserwerkzeug. R null eins führt den Werkzeugwechsel aus. Für eine zugängliche Innenreparatur kannst du stattdessen R null eins einsetzen. Wähle ein beschädigtes Ziel und halte die benötigten Ersatzteile bereit.

**Auslöservorschlag:** Technologie `repair` neu freigeschaltet und noch keine erste Reparatur abgeschlossen. Noch offene feinere Variante: Bei bereits montiertem Laser den Umbauhinweis durch eine passende gesprochene Teilfortschrittsfassung ersetzen; aktuell bleibt der Text eine allgemeine Anleitung. Dieses Ereignis ist nicht der Abschluss von Quest9.

### Laserumbau abgeschlossen

**ID:** `quest_support_laser_ready` · **Stimmung:** konzentriert

> Das Laserwerkzeug ist montiert. Wähle jetzt im Drohnenleitstand ein bekanntes, beschädigtes Stationselement als Reparaturziel und erteile den Auftrag. Prüfe vorher die benötigten Ersatzteile und die Einsatzbereitschaft der Drohne. Mit diesem Werkzeug übernimmt sie Reparaturarbeiten; für den nächsten Eisabbau braucht sie wieder den Bohrer.

**Auslöservorschlag:** Erster erfolgreicher Laserumbau, noch keine abgeschlossene Außenreparatur. Keine neu aufgefüllten Batterie-/Treibwasservorräte behaupten.

## Weitere Dialoglücken und nächste Schritte

- Die neun aktuellen Quests sind hier vollständig als vertontes Sprechertextskript abgedeckt. Ihre18 Haupttexte und vier Übergänge sind aufgenommen und lokal zugeordnet, mit3580 Rhubarb-Intervallen sowie Stimmung/Gesten im gemeinsamen Modul. Automatische Auslösung, Wiederabruf und gespeicherte Anzeigehistorie sind vorhanden.
- Bestehende Scan-, Start-, Liefer-, Wasserabschluss- und Lade-Spielstandsmeldungen mit diesem Skript zusammenführen. Vor allem die alten Aufforderungen, Außenaufträge in der virtuellen Ansicht zu erteilen, und „Wassertank voll“ dürfen nicht parallel widersprüchlich erscheinen.
- Variable Zahlen, Tasten, Füllstände und Rezeptwerte aus dem echten Spielzustand anzeigen. Für aufgenommene variierende Aussagen passende Text-/Audiofassungen vorsehen; niemals dynamische Untertitel unter einer anderslautenden festen Aufnahme abspielen.
- Energieausfall, fehlende Teile, belegte Drohne, falsches Werkzeug, voller Behälter oder abgesperrte Leitung sollen einen konkreten kurzen Hinweis erhalten, wenn sie die ausgewählte Arbeit tatsächlich blockieren. Keine ständigen gesprochenen Wiederholungen bei jedem Simulationsschritt. Diese situationsabhängigen Hilfevarianten sind noch nicht ausgearbeitet.
- Erkundungsdrohnen, weitere Forschungszweige, größere Energieversorgung, neue Behälter und Stationsausbau gehören zum geplanten Chapter1. Sobald daraus konkrete neue Quests entstehen, bekommen sie nach demselben Muster eigene Texte. Die neun vorhandenen Quests bilden noch keine40-Stunden-Hauptgeschichte.
- Die beiden ergänzten Intro-Aufnahmen sind lokal eingebaut und besitzen121 Mundintervalle. Das Erwachen verwendet jetzt16 Stimmen.

**Prüfung dieser Fassung:** Text und Ziele gegen die aktuellen Codequellen abgeglichen; neun Questindizes und22 eindeutige Text-IDs geprüft. Alle WAVs/Metadaten/Textzuordnungen/Prüfsummen und Lippenprofildauern geprüft, beide Player gebaut.618 Assertions in acht finalen Läufen bestanden; Details und Grenzen in `../Pruefungen/Mira-Questdialoge-2026-10-26.json`. Kein subjektiver Hörtest.
