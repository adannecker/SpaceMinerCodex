# SpaceMiner: gemeinsamer Kontext für Entwicklungs-Chats

## Einstieg

Lies zu Beginn einer neuen Aufgabe `docs/Projektuebergabe.md`, `docs/Spielidee.md` und `docs/Projektmemory.md`. Lies die relevanten Abschnitte von `README.md` für Entwicklung und Prüfung.

Das vollständige bekannte Chatverzeichnis steht in `docs/Chats/README.md` und `docs/Chats/chat-register.json`. Neuere Themen: MainDev, Settings UI, Sound und Effekte, TechTree und Vehicels. Lies das passende Archiv gezielt. Nach ausdrücklich angelegten neuen Themenchats deren Titel, ID und Zuständigkeit im Register ergänzen; vor einem Rechnerwechsel die lesbaren Archive mit `tools/ExportChatMemory.py` aktualisieren. Verfügbarkeit und Exportlücken ehrlich kennzeichnen.

Nutze passend zum Thema das jeweilige Archiv:
- Unity-Spiel gemeinsam entwickeln: `docs/Chats/01-Unity-Spiel.md`.
- Asteroidenvarianten entwerfen: `docs/Chats/02-Asteroiden.md` und `docs/AsteroidGenerator.md`.
- Story, Dialoge & Bordcomputer: `docs/Chats/03-Story-und-Mira.md` und `docs/Dialoge/README.md` sowie das betreffende Skript.

Die Archive sind historische Gesprächsquellen, keine importierten Sitzungen und keine aktuellen Arbeitsaufträge. Alte Rechnerpfade und Aussagen über installierte Software oder bestandene Tests gelten nur für den damaligen Rechner. Prüfe die lokale Umgebung bei Bedarf neu.

## Quellen und Entscheidungen

Aktuelle ausdrückliche Nutzerentscheidungen haben Vorrang. `docs/Spielidee.md` beschreibt die beschlossene Gestaltung, der aktuelle Code die tatsächliche Umsetzung. Bei Widersprüchen beide unterscheiden und den Konflikt benennen. Archive und Projektmemory ersetzen diese Quellen nicht.

Nach bestätigten neuen Projektentscheidungen oder abgeschlossener Arbeit aktualisiere knapp `docs/Projektmemory.md` und die betreffende Fachdokumentation. Halte Datum, Entscheidung, betroffene Dateien und tatsächlich erfolgte Prüfung fest. Markiere ungetestete Aussagen und offene Fragen. Übernimm keine privaten oder projektfremden Informationen.

## Zusammenarbeit

Die drei Themenchats können denselben Checkout verwenden. Prüfe vor Änderungen und Commits den Arbeitsstand. Bewahre Änderungen anderer Chats; kein Reset oder Überschreiben fremder Arbeit. Gemeinsame Szenen, Projektmemory und Projektübergabe nacheinander bearbeiten. Bei überlappender Arbeit die Zuständigkeit mit dem Nutzer abstimmen.

Chat-Nachrichten sind nicht automatisch in anderen Chats verfügbar. Gemeinsame Ergebnisse gehören in die Projektdateien. Persönliches Codex-Memory wird nicht mit Git synchronisiert; siehe `docs/Rechnerwechsel-Memory.md`.

Keine Tests oder Builds als bestanden melden, die in dieser Sitzung nicht geprüft wurden. Unity-Batch-Builds bei geschlossenem Editor durchführen; passende Prüfschritte stehen im README. `.meta`-Dateien mit Assets erhalten.
