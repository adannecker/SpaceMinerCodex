# Rechnerwechsel und Memory zusammenführen

## Aktuelle Empfehlung für die Unity-Entwicklung

Auf beiden Rechnern ein lokales Codex-Projekt mit dem dortigen Git-Checkout öffnen. Vor dem Wechsel committen und pushen; am Zielrechner lokale Änderungen prüfen und den Git-Stand holen. AGENTS.md, Projektuebergabe.md, Projektmemory.md und Chats/README.md lesen lassen. Projektwissen und alle bekannten Chatarchive werden gemeinsam mit Git übertragen. Persönliches Memory muss dafür nicht zusammengeführt werden; der unten stehende persönliche Merge-Auftrag ist optional. Builds und Cache werden lokal neu erzeugt. Unity-Version: 6000.4.7f1.

Das Register enthält auch Settings UI, Sound und Effekte, TechTree, Vehicels und den GitHub-/Übergabechat. Die aktuelle Exporthilfe ist tools/ExportChatMemory.py; der ältere Drei-Chat-Exporter überschreibt die neue Übersicht und ist hierfür nicht zu verwenden.

## Was übertragen wird

Git überträgt `AGENTS.md`, `docs/Projektmemory.md`, Fachdokumentation und die drei Chatarchive. Neue Chats lesen diese Dateien als gemeinsamen Projektkontext. Die ursprünglichen Sitzungen werden dadurch nicht importiert. Persönliches Codex-Memory und lokale Chat-Sitzungen sind getrennte, rechnerspezifische Daten.

## Auf dem Ausgangsrechner

1. Laufende Arbeiten abschließen und den Git-Arbeitsstand prüfen. Gemeinsame Dateien erst nach Abstimmung zusammenführen.
2. Auf dem Rechner mit den ursprünglichen drei Sitzungen bei Bedarf `tools/ExportProjectChats.ps1` ausführen. Das Skript nicht ungeprüft auf dem neuen Rechner ausführen: dort fehlen die Ursprungssitzungen möglicherweise.
3. Neue bestätigte Entscheidungen in `docs/Projektmemory.md` und den passenden Fachdateien festhalten. Keine privaten Daten in das Repository aufnehmen.
4. Die gewünschten Änderungen committen und pushen. Erst danach sind sie auf dem anderen Rechner abrufbar. Das Anlegen der Dokumentation allein überträgt sie noch nicht.

## Auf dem anderen Rechner

1. SpaceMinerCodex als lokalen Projektordner in Codex öffnen.
2. Vor dem Aktualisieren lokale Änderungen prüfen und sichern. Den aktuellen GitHub-Stand holen und mit den lokalen Änderungen zusammenführen. Konflikte inhaltlich prüfen, nicht pauschal eine Seite wählen.
3. `AGENTS.md`, `docs/Projektuebergabe.md`, `docs/Spielidee.md` und `docs/Projektmemory.md` lesen lassen. Anschließend pro Themenchat das passende Archiv lesen lassen.
4. Lokale Unity-Installation und erforderliche Prüfungen neu feststellen. Alte Installationspfade und Leistungsmessungen nicht als lokale Tatsachen übernehmen.

## Kopierbarer Auftrag für Codex auf dem anderen Rechner

> Öffne meinen lokalen SpaceMinerCodex-Checkout. Lies zuerst AGENTS.md, docs/Projektuebergabe.md, docs/Spielidee.md, docs/Projektmemory.md und docs/Chats/README.md. Prüfe den Git-Arbeitsstand und führe den aktuellen GitHub-Stand mit lokalen Änderungen zusammen, ohne ungesicherte Arbeit zu überschreiben. Bei unauflösbaren fachlichen Widersprüchen frage mich. Vergleiche danach das vorhandene persönliche Codex-Memory nur hinsichtlich SpaceMiner mit diesen Projektquellen. Nutze die drei Chatarchive für historische Begründungen. Die aktuelle Nutzerentscheidung und Spielidee bestimmen die Gestaltung; Code bestimmt die tatsächliche Umsetzung. Kennzeichne veraltete Einträge, alte Rechnerpfade und nur historisch bestandene Tests. Aktualisiere das gemeinsame Projektmemory nur mit bestätigten, projektbezogenen Erkenntnissen. Ich beauftrage dich ausdrücklich, auch das persönliche Memory für SpaceMiner entsprechend zu ergänzen oder zu korrigieren: verwende dafür den von deiner Umgebung vorgesehenen Memory-Update-Mechanismus; falls nur Update-Notizen erlaubt sind, lege eine solche Notiz an und bearbeite die generierten Memory-Dateien nicht direkt. Kopiere keinen vollständigen .codex-Ordner und keine persönlichen Daten in Git. Berichte die zusammengeführten Entscheidungen, offenen Konflikte und ob die persönliche Memory-Aktualisierung direkt erfolgt oder lediglich als Update-Notiz vorbereitet wurde. Committe und pushe erst, wenn ich dich damit beauftrage.

## Startaufträge für die drei Themenchats

Alle drei Chats im selben lokalen SpaceMinerCodex-Projekt erstellen. Der Anfangsauftrag lautet jeweils:

- **Unity-Spiel gemeinsam entwickeln:** Lies AGENTS.md und die dort genannten gemeinsamen Einstiegsdateien sowie docs/Chats/01-Unity-Spiel.md. Nutze sie als Kontext für Spielsysteme und Unity-Integration. Bestätige kurz den aktuellen Stand und offene Fragen; beginne noch keine neue Implementierung.
- **Asteroidenvarianten entwerfen:** Lies AGENTS.md und die gemeinsamen Einstiegsdateien sowie docs/Chats/02-Asteroiden.md und docs/AsteroidGenerator.md. Nutze sie als Kontext für Generator, Materialien und Asteroidenbibliothek. Bestätige kurz den aktuellen Stand; beginne noch keine neue Implementierung.
- **Story, Dialoge & Bordcomputer:** Lies AGENTS.md und die gemeinsamen Einstiegsdateien sowie docs/Chats/03-Story-und-Mira.md, docs/Dialoge/README.md und docs/Dialoge/01_Intro_Erwachen.md. Nutze sie als Kontext für Mira und die Sprechertexte. Prüfe die ältere Zwei-Drohnen-Darstellung gegen die jüngere Ein-Drohnen-Entscheidung. Bestätige kurz den Stand und den Konflikt; ändere noch keine Texte oder Assets.

## Aktueller Ausgangsordner

Der gemeinsame Checkout auf diesem Rechner liegt seit 06.10.2026 unter C:\Users\achim.dannecker\source\repos\SpaceMinerCodex. Auf dem anderen Rechner den dortigen lokalen Checkout verwenden.
