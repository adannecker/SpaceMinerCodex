# SpaceMiner auf einem anderen Rechner weiterentwickeln

Stand: 05.10.2026. Diese Datei ist der Einstieg für einen neuen Entwicklungs-Chat. Parallel laufende Arbeiten können den Stand weiter verändern; vor einem Commit die betreffenden Chats abschließen lassen.

## Spielidee und Entscheidungen

Weltraum-Aufbau-/Survival-Spiel in einem Asteroidengürtel. Nach einer Explosion beginnt der Spieler auf einem beschädigten Schiff. Er übernimmt zunächst die Außenansicht als Kommandant. Alle Transporte und späteren Produktionslinien basieren auf Drohnen; Förderbänder sind nicht vorgesehen. Essen, Trinken, Energie, Forschung und Wissensartefakte sollen schrittweise hinzukommen. Reisen in andere Sternsysteme sind zunächst außerhalb des Umfangs.

Zehn Drohnen, davon zunächst **nur Drohne 01 einsatzfähig**. Drohnen sind etwa 2 × 2 × 2 m groß, Asteroiden etwa 100 m bis 5 km. Eine Unity-Einheit ist ein Meter. Solarflächen und ein vorhandener kleiner Kernreaktor sollen die Stromversorgung bilden. Die vollständigen Leitlinien stehen in [Spielidee.md](Spielidee.md).

## Implementierter Stand

- Startbare Windows-Version mit 10.000 Asteroiden in einer Spirale um das Schiff, knapp zehn Windungen und etwa 186 km Außenradius. Die Szene speichert 100 individuelle Formvorlagen (drei Materialfamilien, vier Detailstufen); zur Laufzeit entstehen 9.900 weitere auswählbare Körper mit gemeinsamen Meshes und GPU-Instancing. Dazu kommen Schiff und zehn Drohnen. B zeigt die Spirale von oben, R kehrt zum Schiff zurück; Objektzahl und FPS stehen im HUD. F11 oder Alt+Enter wechselt zwischen Fenster und Vollbild.
- Flüssige Kamera: drehen, verschieben, bewegen, fokussieren und von 3 m bis 1.000 km zoomen. Fokussierte Drohnen werden verfolgt.
- Wasserauftrag: Eisquelle auswählen, hinfliegen, bremsen, abbauen, zurückfliegen, Wasser abliefern, Batterie laden und Treibwasser nachtanken. Der Auftrag läuft bis zum vollen Tank oder einer erschöpften Quelle.
- Tank- und Aufgabenanzeige, Balken über der Drohne, Objektinformationen mit Batterie, Treibwasser, Ladung, Geschwindigkeit, Zielentfernung und Zeitangaben. Asteroiden zeigen bekannten Wasseranteil und unbekannten Rest.
- Einstellbare Spielannahmen: 200-L-Schiffstank mit 20 L Startwasser, Drohne 200 kg Trockenmasse, 50 kg Ladung, 10 L Treibwasser, 8 kWh Batterie, höchstens 5 m/s, 5 N Schub. Geradlinige Flüge mit vereinfachter Physik, keine vollständige Orbitalmechanik.
- Miras Intro mit Untertiteln, Kamerafahrten und lokal erzeugter deutscher Teststimme ist hinzugekommen. Die Sprachdateien liegen im Projekt; beim Spielen ist kein Sprachdienst erforderlich. Esc überspringt das Intro.

Der Integrationstest mit 10.000 Asteroiden bestand hier 60.079 Laufzeitprüfungen, einschließlich Objektzahl, Detailstufen, Collider, Spiralpositionen, Mindestabständen von 200 m, Sichtbarkeit, Auswahl kleiner Übersichtssymbole und Auswahl per Raycast bis zum äußeren Arm, Intro und Wasserauftrag. Eine kurze Messung bei 1440 × 900 auf einer AMD Radeon RX 9070 XT ergab etwa 164–165 FPS in Feldansicht, Kameradrehung, Nahansicht eines 5-km-Asteroiden und äußerem Arm, bei aktiviertem VSync. Die 9.900 zusätzlichen Körper benötigen in der Übersicht 100 instanzierte Zeichenaufrufe; die ursprünglichen 100 Körper werden zusätzlich regulär dargestellt. Details: `Logs/asteroid-10000-performance.json` (lokal, nicht in Git). Nach weiteren Änderungen erneut bauen und prüfen.

## Parallel bearbeitete Bereiche

Die Chats **Story, Dialoge & Bordcomputer** und **Asteroidenvarianten entwerfen** haben Intro und Asteroidenbibliothek fertiggestellt.

Asteroidenkonzepte liegen unter `docs/Art/Asteroiden`. Der erweiterbare Generator, Asteroidentypen, Oberflächenshader, Detailstufen, Collider und Editor-Werkzeuge liegen unter `Assets/SpaceMiner`. `.\tools\Unity.ps1 SpiralBuild` aktiviert die Spirale mit 10.000 Körpern, `.\tools\Unity.ps1 HundredBuild` kehrt zum ursprünglichen Test mit 100 Körpern zurück. Ein normales `Build` behält den gewählten Modus bei. In Unity erscheint die Spirale nach Play; die 9.900 zusätzlichen Körper werden zur Laufzeit erzeugt. Im normalen Szene-Editor stehen die 100 Vorlagen in ihrer ursprünglichen Anordnung.

Mira spricht ruhig und warm mit weiblicher Stimme und duzt den Spieler. Sprechertexte und Regie stehen in `docs/Dialoge`; das Laufzeitskript und die WAV-Dateien liegen unter `Assets/SpaceMiner/Resources/Intro`. Die aktuelle Windows-Teststimme kann später ersetzt werden.

## GitHub und Projektdateien

Repository: [adannecker/SpaceMinerCodex](https://github.com/adannecker/SpaceMinerCodex). Der erste Upload ist ein Sicherungsstand während der abschließenden Asteroidenarbeiten.

Zum Entwicklungsprojekt gehören `Assets/` einschließlich aller `.meta`-Dateien, `Packages/`, `ProjectSettings/`, `tools/`, `docs/`, README und die Git-Konfiguration. Die bestehende `.gitignore` schließt Unity-Cache, temporäre Dateien, lokale Editor-Einstellungen, Logs und Builds aus. Unity stellt seinen Cache nach dem Klonen wieder her. Die fertige EXE wird auf dem Zielrechner neu gebaut.

Die drei lokalen Projektchats sind unter [Chats/README.md](Chats/README.md) als lesbare Textarchive gesichert. Diese Archive werden mit Git übertragen. Sie stellen keine automatisch fortsetzbaren Codex-Sitzungen her. Sie enthalten die Gesprächstexte, keine vollständigen Werkzeugprotokolle oder binären Chat-Anhänge. Bilder und Sprachdateien, die ins Projekt übernommen wurden, bleiben reguläre Projektassets.

Zum Aktualisieren der Archive auf dem ursprünglichen Rechner: `.\tools\ExportProjectChats.ps1`. Den gesamten persönlichen `.codex`-Ordner nicht ins Repository kopieren; er gehört nicht zum Unity-Projekt.

## Auf dem anderen Windows-Rechner starten

1. Git und Unity Hub einrichten. Im Hub Unity **6000.3.2f1** sowie Windows Build Support installieren und Unity mit dem eigenen Konto anmelden.
2. `git clone https://github.com/adannecker/SpaceMinerCodex.git` ausführen (oder mit GitHub Desktop klonen); den neuen lokalen Ordner im Unity Hub hinzufügen.
3. In Codex den geklonten Projektordner öffnen. Beispiel für den neuen Chat: **„Lies docs/Projektuebergabe.md, README.md und docs/Spielidee.md. Prüfe den aktuellen Projektstand und setze die Entwicklung von SpaceMiner mit mir fort.“**
4. Projekt mit Unity 6000.3.2f1 öffnen, Import abwarten, Szene `Assets/SpaceMiner/Scenes/AsteroidBelt.unity` öffnen und Play drücken.
5. Für die Windows-Version bei geschlossenem Unity-Editor `.\tools\Unity.ps1 Build` und anschließend `.\tools\Unity.ps1 Check` ausführen. `.\tools\Unity.ps1 AsteroidCheck` prüft den Generator, sobald dessen Integration abgeschlossen ist.

Das Hilfsskript verwendet standardmäßig den üblichen Windows-Installationspfad von Unity. Falls nötig, `-UnityPath 'D:\...\Editor\Unity.exe'` angeben. Andere Betriebssysteme können die Unity-Projektdateien öffnen, benötigen aber angepasste Build-Werkzeuge; die beiliegenden PowerShell-Hilfen bauen Windows-Versionen.

Vor dem Rechnerwechsel Änderungen committen und pushen. Auf dem anderen Rechner zuerst den aktuellen Stand holen und dortige Änderungen ebenfalls committen und pushen. So bleibt der Wechsel nachvollziehbar.

## Noch ausstehende Spielsysteme

Speichern/Laden, Reparaturen, vollständiger Notstrombetrieb und Schiffsbatterien, Nahrung und Trinkwasseraufbereitung, Forschung und Wissensartefakte, weitere Rohstoffe und Produktionslinien, Kollisionsvermeidung und realistischere Raumfahrtphysik.
