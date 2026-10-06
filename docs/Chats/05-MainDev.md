# MainDev

Chat-ID: `01a11007-2934-7520-96c3-3991e89ccaf8`. Exportstand: 2026-10-06T13:17:29.153160+00:00.

Unity 6000.4.7f1, 100-Körper-Wolke, optionale Stresstests, Station und Bergbauzyklus

## Assistent · 2026-10-06T07:04:31.470Z

Ich lese den gemeinsamen Projektkontext und prüfe den aktuellen Stand ausschließlich lesend. Dabei berücksichtige ich die jüngere Entscheidung zu Drohne 01.

## Assistent · 2026-10-06T07:05:10.373Z

Kontext gelesen. Für Projektarbeit verwende ich ausschließlich den gemeinsamen Ordner `kan\SpaceMinerCodex`.

Dokumentierter Stand: Unity-Wasserprototyp mit Kamera und Objektauswahl, 10.000 Asteroiden in einer Spirale, Miras Intro sowie Fenster-/Vollbildwechsel. Branch `main`, Commit `89ea784`; drei vorhandene neue Kontextdateien sind noch unversioniert.

**Wichtige Abweichung:** Beschlossen ist, dass anfangs nur Drohne 01 funktioniert. Code, README und bestehender Test behandeln Drohne 02 weiterhin als funktionsfähig, aber unversorgt. Das ist später gemeinsam abzugleichen.

Offen bleiben insbesondere Notstromquelle und Schiffsbatterien, Reparaturen, Speichern/Laden, Nahrung, Forschung und Produktionslinien.

Keine Dateien geändert, keine Builds, Tests, Commits oder Pushes ausgeführt. Frühere Testergebnisse sind historische Angaben.

## Nutzer · 2026-10-06T07:22:17.074Z

Ok kannst du schauen ob du das Spiel hier starten kanns

## Assistent · 2026-10-06T07:22:19.876Z

Ich prüfe, ob hier eine startbare Spielversion oder der passende Unity-Editor vorhanden ist, und starte das Spiel, wenn möglich.

## Assistent · 2026-10-06T07:22:32.617Z

Hier fehlt noch die fertige Spielversion. Installiert ist Unity **6000.4.7f1**, während das Projekt **6000.3.2f1** verwendet.

Für einen Start müssten wir entweder die passende Version installieren oder das Projekt mit der neueren Version öffnen und bauen. Letzteres kann Projektdateien verändern.

Soll ich die vorhandene neuere Version verwenden?

## Nutzer · 2026-10-06T07:22:57.069Z

Ja ich update das bei mir dann auch zu Hause

## Assistent · 2026-10-06T07:22:59.875Z

Ich öffne das Projekt mit Unity 6000.4.7f1, erstelle die Spielversion und starte sie. Dabei prüfe ich, ob Unity die Lizenz erkennt und ob beim Versionswechsel Fehler auftreten.

## Assistent · 2026-10-06T07:23:53.254Z

Unity bleibt bisher bei der Lizenzinitialisierung hängen; der Build hat noch nicht begonnen. Ich prüfe den gestarteten Prozess und den Lizenzdienst, um die Ursache einzugrenzen.

## Assistent · 2026-10-06T07:26:03.959Z

Die Lizenzverbindung funktioniert jetzt. Unity hat die Projektskripte kompiliert und erstellt gerade die Windows-Spielversion.

## Assistent · 2026-10-06T07:27:26.873Z

Die Windows-Version ist erfolgreich gebaut. Der vorhandene Spieltest läuft jetzt mit Unity 6000.4.7f1; anschließend öffne ich die normale Version zum Ausprobieren. Unity hat beim Import und Bauen auch Assets und Projekteinstellungen aktualisiert.

## Assistent · 2026-10-06T07:28:09.838Z

Das Spiel ist gestartet! **Unity 6000.4.7f1** hat den Build und alle **60.079 Spielprüfungen** bestanden.

- **Esc:** Intro überspringen
- **B:** Asteroidenspirale anzeigen
- **R:** zurück zum Schiff
- **F11:** Vollbild wechseln

Installiere zu Hause ebenfalls **6000.4.7f1**. Die aktualisierten Projektdateien sind noch nicht committed oder gepusht.

## Nutzer · 2026-10-06T07:30:00.434Z

Ja ich update das bei mir dann auch zu Hause... So kannst du einen nächsten Versuch mal machen mache doch wie so eine Spiralwolke aus Asteoriden um das Schiff.... Da es selbst auf dem Rechner super lief, kannst du das mal mit einer Million ausprobieren, mich interessiert ob das alles soweit auch auf meinem Lapi geht

## Assistent · 2026-10-06T07:30:03.452Z

Ich prüfe den bisherigen Spiraltest und erweitere ihn für eine räumliche Spiralwolke mit einer Million Asteroiden. Anschließend teste ich hier Leistung und Bedienbarkeit auf deinem Laptop.

## Assistent · 2026-10-06T07:30:37.935Z

Für eine Million Körper braucht der Test eine andere Darstellung: Ich speichere die Asteroiden als kompakte Daten und zeichne die Wolke in großen Gruppen. Nahe und ausgewählte Körper erhalten die vorhandenen 3D-Formen. So können wir die Million testen, ohne eine Million einzelne Unity-Objekte und Collider anzulegen. Der Wasserauftrag bleibt bei den nahen Startquellen.

## Assistent · 2026-10-06T07:33:37.579Z

Die neue Wolke hat fünf gewundene Arme und eine räumliche Dicke von rund 160 km bei etwa 600 km Außenradius. Der Build läuft. Danach messe ich Übersicht, Kameradrehung und Nahansichten und prüfe den Wasserauftrag.

## Assistent · 2026-10-06T07:35:20.181Z

Die Million Asteroiden wurde hier in etwa **einer Sekunde** erzeugt. Das Spiel läuft bereits; der Test prüft gerade Miras Intro. Die Messung der Wolkenansichten folgt danach.

## Assistent · 2026-10-06T07:36:52.056Z

Der Testlauf ist beim automatischen Vorspulen des Intros hängen geblieben. Ich habe diese Testschleifen begrenzt, damit der Lauf nicht endlos warten kann, und baue die Version für einen erneuten Test. Leistungswerte für die Wolke liegen noch nicht vor.

## Assistent · 2026-10-06T07:38:55.586Z

Der Test erreicht die Wolke jetzt. Eine Prüfung der Collider-Auswahl in großer Entfernung ist fehlgeschlagen. Ich prüfe für diesen Modus deshalb die tatsächlich verwendete Auswahl über die gespeicherten Positionen und Bildschirmkoordinaten; Collider-Treffergenauigkeit ist damit noch nicht bestätigt.

## Assistent · 2026-10-06T07:40:19.491Z

Die erste vollständige Messung ist bestanden: Auf der **NVIDIA RTX A500 Laptop GPU** lief der Million-Test bei **1440 × 900 in allen vier Ansichten mit rund 60 FPS**, begrenzt durch VSync. Auch Auswahl und Wasserauftrag haben bestanden. Ich prüfe noch die Bilder und ergänze den Bericht, damit er klar zwischen Übersichtspunkten und nahen 3D-Körpern unterscheidet.

## Assistent · 2026-10-06T07:41:48.323Z

Die Übersicht zeigt die Spiralstruktur um das Schiff. Ich führe noch die abschließende Prüfung aus, einschließlich einer Nahansicht am tatsächlichen äußeren Rand, und öffne danach die Version zum Ausprobieren. Die Million umfasst gespeicherte und dargestellte Körper; eine Million gleichzeitig aktive Physik- oder Abbauprozesse sind nicht Teil dieses Tests.

## Assistent · 2026-10-06T07:42:58.879Z

Bei der Bildkontrolle ist ein Darstellungsfehler aufgefallen: Die Wolke war im ersten Bild sichtbar, in einer späteren Übersicht jedoch verschwunden. Die 60 FPS allein belegen deshalb noch keine korrekte Darstellung. Ich korrigiere den Punktrenderer und ergänze eine Prüfung, die eine leere Übersicht erkennt.

## Assistent · 2026-10-06T07:45:20.413Z

Der Darstellungsfehler ist behoben und die Übersicht auch nach mehreren Frames sichtbar. Der abschließende Test hat bestanden: **rund 60 FPS in allen vier Ansichten**, Erzeugung in **0,92 Sekunden**, einschließlich Auswahl, äußerer 3D-Nahansicht und Wasserauftrag. Ich starte jetzt die Version zum Ausprobieren.

## Assistent · 2026-10-06T07:45:55.074Z

Die Version mit **1 Million Asteroiden als räumliche Spiralwolke** ist gestartet!

Auf deinem Laptop mit **RTX A500**:

- **Rund 60 FPS** bei 1440 × 900 in allen vier getesteten Ansichten, durch VSync begrenzt.
- Erzeugung in **0,92 Sekunden**.
- Auswahl, Nahansichten und Wasserauftrag geprüft.

**Esc**, dann **B** zeigt die Wolke. **Klick + F** fokussiert einen Asteroiden.

Entfernte Körper erscheinen als Punkte, nahe als 3D-Modelle. Eine Million gleichzeitig aktive Physikkörper wurde damit nicht getestet. Noch kein Commit oder Push.

## Nutzer · 2026-10-06T07:47:07.061Z

Ok das ist schon sehr eindrücklich, es geht alles so extrem schnelll.... auch auf meinem Laptop

## Assistent · 2026-10-06T07:47:10.770Z

Ja, das ist ein gutes Ergebnis: Selbst dein Laptop hält mit der Million Asteroiden die **60 FPS**. Durch VSync sehen wir allerdings noch nicht, wie viel Leistungsreserve bleibt.

Damit haben wir eine brauchbare Grundlage für große Felder. Spannend wird später, wie sich aktive Drohnen, Abbau und Produktion auf die Leistung auswirken.

## Nutzer · 2026-10-06T08:05:14.328Z

Ok ich würde das aber so mal für den ersten Test auf sagen wir 100 beschränken.... alle als Wolke rund um das Schiff...

## Assistent · 2026-10-06T08:05:19.015Z

Ich stelle den Test auf 100 Asteroiden um und verteile sie als räumliche Wolke rund um das Schiff. Die nahen Eisquellen für den Wasserauftrag bleiben erhalten. Danach prüfe und starte ich die Version.

## Assistent · 2026-10-06T08:06:38.300Z

Der neue Test verteilt die 100 Körper in alle Richtungen, auch oberhalb und unterhalb des Schiffs, bis etwa 18 km Entfernung. Zwischen den übrigen Asteroiden bleiben mindestens 400 m Abstand; die drei Eisquellen liegen weiterhin nah am Schiff.

## Assistent · 2026-10-06T08:07:55.532Z

Unity hat die Asteroiden neu angeordnet und baut jetzt die Spielversion. Im anschließenden Test prüfe ich insbesondere die Verteilung rund um das Schiff, die Abstände und den Wasserauftrag.

## Assistent · 2026-10-06T08:09:31.575Z

Die 100-Asteroiden-Wolke ist fertig und hat **769 Prüfungen** bestanden, einschließlich Verteilung in alle Raumrichtungen, Mindestabständen und Wasserauftrag. Die Übersicht ist auch visuell geprüft. Ich öffne jetzt die Version zum Ausprobieren.

## Assistent · 2026-10-06T08:09:50.176Z

Fertig und gestartet: **100 Asteroiden als räumliche Wolke rund um das Schiff**. Die drei nahen Eisquellen bleiben für den Wasserauftrag erhalten.

**769 Prüfungen bestanden**, einschließlich Verteilung und Abständen.

**Esc**, dann **B** zeigt die Wolke. **R** bringt dich zurück zum Schiff.

## Nutzer · 2026-10-06T08:14:34.369Z

Wenn ein Asteroid 100m gross ist also lange wieviel Wasser wäre das dann oder noch besser wir generieren ja Asteroiden kann da auch das Volumen bestimmt werden? Denn daraus würde sich ja dannzum Beispiel die Wassermenge oder Ressourcen dann berechnen lassen in Kg

## Assistent · 2026-10-06T08:14:37.677Z

Ja, das Volumen lässt sich aus der erzeugten 3D-Form berechnen und daraus eine Ressourcenmasse ableiten. Ich schaue kurz nach, wie unsere Asteroiden aufgebaut sind und wie der Wasserbestand bisher festgelegt wird.

## Assistent · 2026-10-06T08:14:53.003Z

**Ja, genau so können wir die Ressourcen berechnen.** Unsere generierten Asteroiden besitzen bereits eine geschlossene 3D-Form. Aus deren Dreiecken lässt sich das eingeschlossene **Volumen in m³** bestimmen, inklusive der tatsächlichen unregelmäßigen Form.

„100 m lang“ allein reicht allerdings nicht: Ein schmaler Asteroid enthält deutlich weniger Material als ein annähernd kugelförmiger.

Als Beispiel nehmen wir eine **Kugel mit 100 m Durchmesser** und ausdrücklich angenommene Spielwerte:

| Größe | Ergebnis |
|---|---:|
| Volumen | rund **523.600 m³** |
| Gesamtmasse bei angenommener mittlerer Dichte von 1.200 kg/m³ | **628 Millionen kg** |
| Enthaltenes Wasser bei 80 % Wasser-Massenanteil | **503 Millionen kg** |
| Gewinnbares Wasser bei 70 % Ausbeute | **352 Millionen kg** |

Die Rechnung wäre:

**Volumen × mittlere Dichte × Ressourcen-Massenanteil × Gewinnungsausbeute**

Die mittlere Dichte muss auch Hohlräume berücksichtigen: Asteroiden können poröse Ansammlungen aus Gestein sein. Das ist beispielsweise bei Bennu nachgewiesen. [NASA](https://www.nasa.gov/solar-system/nasa-mission-helps-solve-a-mystery-why-are-some-asteroid-surfaces-rocky/)

Für unser Spiel würde ich deshalb pro Asteroid speichern:

- **Volumen und Gesamtmasse**
- **Zusammensetzung nach Masse**, beispielsweise Wasser, Eisen und unbekannte Bestandteile
- **Verbleibende Ressourcen**, von denen der Abbau tatsächlich etwas abzieht

Das Volumen berechnen wir einmal aus der detaillierten Form. Es bleibt gleich, wenn die Kamera entfernt ist und eine vereinfachte Darstellung verwendet wird.

**Aktuell sind bei unseren Eisquellen pauschal 10.000 kg Rohmaterial hinterlegt**, unabhängig von ihrer Größe. Deine Idee würde diesen Platzhalter durch nachvollziehbare Vorräte ersetzen. Bei solchen Mengen wären zunächst Fördergeschwindigkeit, Energie und Transportkapazität die entscheidenden Engpässe.

## Nutzer · 2026-10-06T08:48:44.130Z

Wo kann ich das Spiel starten

## Assistent · 2026-10-06T08:48:48.353Z

Du kannst es hier starten:

**[SpaceMiner.exe starten](C:/Users/achim.dannecker/Documents/Codex/2026-10-06/kan/SpaceMinerCodex/Builds/Windows/SpaceMiner.exe)**

Falls der Link es nicht öffnet: Im Explorer zum Ordner `SpaceMinerCodex\Builds\Windows` gehen und **SpaceMiner.exe** doppelklicken.

**Esc** überspringt das Intro, **B** zeigt die Wolke mit 100 Asteroiden.

## Nutzer · 2026-10-06T10:28:41.711Z

In den anderen Chats wird das Unity Projekt nicht gefunden ist das nicht überall im Projekt bekannt

## Assistent · 2026-10-06T10:28:48.061Z

Ich prüfe kurz den gemeinsamen Projektordner und die Hinweise dazu, wie die Chats ihren Arbeitsordner verwenden.

## Assistent · 2026-10-06T10:28:54.192Z

Der gemeinsame Ordner ist vorhanden. Ich nutze die offizielle OpenAI-Dokumentation, um die Zuordnung von Chats zu Projektordnern zu prüfen.

## Nutzer · 2026-10-06T10:29:09.343Z

Oder kann es sein, dass das daran liegt dass ich einen Chat begonnen habe aber keinem Projekt zugewiesen habe und dann den Chat verschoben habe

## Assistent · 2026-10-06T10:29:17.897Z

**Ja, das passt zu dem, was ich hier sehe.** Die Chats gehören zum selben Projekt, verwenden aber unterschiedliche Arbeitsordner:

- Dieser Chat startet in `…\space-miner-unity`.
- **„Implement SpaceMiner settings UI“** startet in einem internen `.chatgpt-projects`-Ordner.
- Das Unity-Projekt liegt tatsächlich in `…\kan\SpaceMinerCodex`.

Das Verschieben hat also bei euch die Projektzuordnung hergestellt, den Arbeitsordner aber nicht auf das Unity-Repository umgestellt. Ich verwende hier den richtigen Ordner aufgrund unserer ausdrücklichen Startanweisung.

Für die anderen Chats kannst du diese Anweisung verwenden:

:::writing{variant="chat_message" id="61842"}
Verwende für sämtliche SpaceMiner-Projektarbeiten ausschließlich diesen bestehenden gemeinsamen Repository-Ordner:

C:\Users\achim.dannecker\Documents\Codex\2026-10-06\kan\SpaceMinerCodex

Lies dort AGENTS.md, docs/Projektuebergabe.md und docs/Projektmemory.md. Prüfe den aktuellen Arbeitsstand und bewahre Änderungen anderer Chats. Lege kein neues Unity-Projekt an.
:::

Diese Anweisung gehört auch in die gemeinsamen Projektanweisungen, damit neue Chats den Pfad erhalten. Projektanweisungen gelten für die zugehörigen Chats; ein ChatGPT-Projekt allein stellt jedoch keinen lokalen Ordnerzugriff her. [Offizielle Dokumentation](https://learn.chatgpt.com/docs/projects)

## Nutzer · 2026-10-06T10:33:34.053Z

<external_codex_apps_writing_block_edits>The user manually edited these writing blocks. Treat the following snapshots as the current versions of those blocks, superseding the earlier assistant output.
[]</external_codex_apps_writing_block_edits>

## Nutzer · 2026-10-06T10:33:34.074Z

Ah liegt es daran das zum einen es Chat ist und zum anderen Work

## Nutzer · 2026-10-06T10:37:49.077Z

Kannst du einen Workspace in dem Projekt erstellen mit dem Namen Techtree

## Assistent · 2026-10-06T10:37:55.048Z

Ich lege „Techtree“ als neuen Arbeitschat im SpaceMiner-Projekt an und gebe ihm den gemeinsamen Unity-Ordner und die Projektunterlagen als Startkontext.

## Assistent · 2026-10-06T10:38:28.674Z

SpaceMinerCodex ist hier ein ChatGPT-Projekt. Darin kann ich „Techtree“ als Cloud-Workspace anlegen; der lokale Unity-Ordner wird dadurch nicht automatisch zugänglich.

Die automatische Freigabeprüfung hat den ersten Cloud-Auftrag abgelehnt, weil sein Starttext deinen lokalen Pfad und Windows-Benutzernamen enthielt. Ich entferne diese Angaben und lege den Workspace mit dem Spielkontext an.

## Assistent · 2026-10-06T10:38:43.242Z

„**Techtree**“ wurde im Projekt **SpaceMinerCodex** als Cloud-Workspace angelegt, mit unserem Spielkontext als Startauftrag. Der lokale Unity-Ordner ist dort nicht automatisch zugänglich.

::created-thread{clientThreadId="local-chatgpt:24419c03-11aa-48ee-8564-e820d59c41c7"}

Die automatische Freigabeprüfung hatte zunächst die Übermittlung deines lokalen Pfads und Benutzernamens abgelehnt. Ohne diese Angaben war die Erstellung erfolgreich.

## Assistent · 2026-10-06T10:43:00.977Z

Ich prüfe den neuen Repository-Pfad und lese die dortigen Projektanweisungen.

## Assistent · 2026-10-06T10:43:05.440Z

Bestätigt: Ab sofort verwende ich ausschließlich **`C:\Users\achim.dannecker\source\repos\SpaceMinerCodex`**.

Der neue Ordner ist vorhanden, der alte nicht mehr. `AGENTS.md` ist gelesen. Keine Projektdateien geändert.

## Nutzer · 2026-10-06T12:34:15.786Z

Ich sehe im ganzen eine Lichtquelle kann man da vielleicht auch eine Sonne draus machen und berechnen wieviel gerade Sonn auf die Panels gehen? und macht es sinn dass es eine Rotation oder Bewegung gibt damit man die Sachen immer wieder neu ausrichten muss?

## Assistent · 2026-10-06T12:34:22.021Z

Ich schaue nach, wie Lichtquelle und Solarleistung derzeit umgesetzt sind. Dann können wir festlegen, wie Sonne, Ausrichtung und Bewegung sinnvoll ins Spiel passen.

## Assistent · 2026-10-06T12:34:37.098Z

**Ja, das passt sehr gut zum Spiel.** Unsere Lichtquelle heißt bereits „Sun“, ist aber bislang nur für die Beleuchtung da. Die Solarleistung ist momentan ein fester Wert von **0,5 kW**.

Wir können daraus eine sichtbare, weit entfernte Sonne machen und für jeden Solarflügel berechnen:

**Solarleistung = Sonnenstrahlung × intakte Panelfläche × Wirkungsgrad × Ausrichtungsfaktor × Beleuchtung**

Dabei zählen:

- **Entfernung zur Sonne:** Doppelte Entfernung bedeutet ein Viertel der Einstrahlung.
- **Ausrichtung:** Frontaler Einfall liefert das Maximum; bei 60° Abweichung bleiben ungefähr 50 %, bei seitlichem Einfall praktisch nichts.
- **Schatten:** Station oder Asteroiden können die Panels teilweise verdecken.
- **Schäden:** Fehlende Zellen reduzieren die nutzbare Fläche.

Entfernung und Winkelabhängigkeit entsprechen den Grundlagen realer Raumfahrt-Solaranlagen. [NASA](https://www.nasa.gov/smallsat-institute/sst-soa/power-subsystems/)

**Eine langsame Rotation der beschädigten Station wäre ein sinnvoller erster Spielmechanismus.** Als Szenarioannahme könnte die Explosion eine Restrotation hinterlassen haben. Dann schwankt die Solarleistung, Batterien überbrücken schwache Phasen, und eine Reparatur der Ausrichtung bekommt einen konkreten Nutzen. Lagekontrolle und nachgeführte Panels sind auch bei realen Raumfahrzeugen wichtig. [NASA](https://science.nasa.gov/learn/basics-of-space-flight/chapter11-2/)

Ich würde die Entwicklung so gestalten:

1. Anfangs schlecht ausgerichtete Panels und langsame Stationsrotation.
2. Spieler kann die Solarflügel manuell zur Sonne ausrichten.
3. Reparierte Stellmotoren ermöglichen automatische Nachführung.
4. Später stabilisiert eine funktionierende Lagekontrolle die Station.

So wird das Ausrichten zunächst eine Aufgabe, die man durch technische Entwicklung dauerhaft lösen kann. Die Asteroiden können zusätzlich langsam rotieren; ihre Bewegung würde ich später ergänzen, weil sie auch Drohnenflug und Kollisionsvermeidung verändert.

## Nutzer · 2026-10-06T12:37:14.942Z

Ja macht Sinn

## Assistent · 2026-10-06T12:37:19.049Z

Dann halten wir das als nächsten Entwicklungsschritt fest: **sichtbare Sonne, berechnete Solarleistung und eine langsam rotierende Station**. Die Panels lassen sich zunächst manuell ausrichten; automatische Nachführung und stabilisierte Stationslage werden spätere Reparaturziele.

Asteroidenbewegung folgt später.

## Nutzer · 2026-10-06T12:41:32.210Z

Ok Cool wenn man auf die Station geht sieht man die Leistung dann und die Ausrichtung.... Man muss die Möglichkeit zur Ausrichtung auch Manuel erstmal erforschen. Am Anfang würde man die ganze Station ausrichten mit düsen dafür muss aber Wasser da sein oder?

## Assistent · 2026-10-06T12:41:40.985Z

Ich prüfe kurz die Antriebsgrundlage, damit die Ausrichtung der Station zu unserem bisherigen Wasserantrieb passt.

## Assistent · 2026-10-06T12:41:51.575Z

**Ja: Wenn wir die Station mit Wasserdampfdüsen ausrichten, brauchen wir Wasser und elektrische Energie.** Wasserbasierte Düsen zur Lagekontrolle gibt es tatsächlich. Die Auslegung für unsere große Station wäre eine eigene Spielannahme. [NASA](https://www.nasa.gov/smallsat-institute/sst-soa/in-space_propulsion/)

Die Düsen geben kurze Impulse: zunächst die vorhandene Rotation abbremsen, dann zur Sonne drehen und am Ende die Drehung wieder stoppen. **Eine einmal stabilisierte Station braucht ohne neue Störungen keinen dauernden Schub, um ihre Ausrichtung zu halten.**

Damit ergibt sich diese Entwicklung:

1. **Anfang:** Die Station dreht langsam, die Solarleistung schwankt. Mit den vorhandenen Düsen kann der Spieler die ganze Station ausrichten. Das kostet Wasser und Strom.
2. **Nach Forschung und Reparatur:** Die Solarflügel lassen sich unabhängig von der Station manuell verstellen.
3. **Später:** Automatische Sonnennachführung übernimmt die Panelausrichtung.

Beim Anklicken der Station sollten wir anzeigen:

- aktuelle Solarleistung und mögliche Maximalleistung
- Ausrichtungswinkel und Schattenanteil je Solarflügel
- Drehrichtung und Drehgeschwindigkeit der Station
- verfügbares Treibwasser und Zustand der Steuerdüsen

So bekommt das erste Wasser einen zusätzlichen Zweck: **Drohnen versorgen oder Wasser in bessere Energiegewinnung investieren.** Für den Einstieg müssen wir genügend Restenergie und Startwasser vorsehen, damit schlechte Ausrichtung die erste Wasserlieferung nicht unmöglich macht.

## Nutzer · 2026-10-06T12:43:13.745Z

Super....

## Assistent · 2026-10-06T12:43:17.844Z

Damit steht der Ablauf: **zunächst die ganze Station mit Wasser und Strom ausrichten; bewegliche Solarflügel erst nach Forschung und Reparatur.** Beim Anklicken zeigt die Station Leistung, Ausrichtung und Rotation.

