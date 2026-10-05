# SpaceMiner auf einem anderen Rechner weiterentwickeln

Stand: 05.10.2026. Diese Datei ist der Einstieg für einen neuen Entwicklungs-Chat. Parallel laufende Arbeiten können den Stand weiter verändern; vor einem Commit die betreffenden Chats abschließen lassen.

## Spielidee und Entscheidungen

Weltraum-Aufbau-/Survival-Spiel in einem Asteroidengürtel. Nach einer Explosion beginnt der Spieler auf einem beschädigten Schiff. Er übernimmt zunächst die Außenansicht als Kommandant. Alle Transporte und späteren Produktionslinien basieren auf Drohnen; Förderbänder sind nicht vorgesehen. Essen, Trinken, Energie, Forschung und Wissensartefakte sollen schrittweise hinzukommen. Reisen in andere Sternsysteme sind zunächst außerhalb des Umfangs.

Zehn Drohnen, davon zunächst **nur Drohne 01 einsatzfähig**. Drohnen sind etwa 2 × 2 × 2 m groß, Asteroiden etwa 100 m bis 5 km. Eine Unity-Einheit ist ein Meter. Solarflächen und ein vorhandener kleiner Kernreaktor sollen die Stromversorgung bilden. Die vollständigen Leitlinien stehen in [Spielidee.md](Spielidee.md).

## Implementierter Stand

- Startbare Windows-Version und Unity-Szene mit zwölf Asteroiden, Schiff und zehn Drohnen.
- Flüssige Kamera: drehen, verschieben, bewegen, fokussieren und von 3 m bis 1.000 km zoomen. Fokussierte Drohnen werden verfolgt.
- Wasserauftrag: Eisquelle auswählen, hinfliegen, bremsen, abbauen, zurückfliegen, Wasser abliefern, Batterie laden und Treibwasser nachtanken. Der Auftrag läuft bis zum vollen Tank oder einer erschöpften Quelle.
- Tank- und Aufgabenanzeige, Balken über der Drohne, Objektinformationen mit Batterie, Treibwasser, Ladung, Geschwindigkeit, Zielentfernung und Zeitangaben. Asteroiden zeigen bekannten Wasseranteil und unbekannten Rest.
- Einstellbare Spielannahmen: 200-L-Schiffstank mit 20 L Startwasser, Drohne 200 kg Trockenmasse, 50 kg Ladung, 10 L Treibwasser, 8 kWh Batterie, höchstens 5 m/s, 5 N Schub. Geradlinige Flüge mit vereinfachter Physik, keine vollständige Orbitalmechanik.
- Miras Intro mit Untertiteln, Kamerafahrten und lokal erzeugter deutscher Teststimme ist hinzugekommen. Die Sprachdateien liegen im Projekt; beim Spielen ist kein Sprachdienst erforderlich. Esc überspringt das Intro.

Der Wasserprototyp bestand hier zunächst 80 Laufzeitprüfungen. Nach Intro- und Asteroiden-Ergänzungen meldete der lokale Integrationstest zuletzt 140 bestandene Prüfungen; die separate Geometrieprüfung meldete 57 Checks. Nach weiteren Änderungen erneut bauen und prüfen.

## Parallel bearbeitete Bereiche

Der Chat **Story, Dialoge & Bordcomputer** hat das Intro fertiggestellt. **Asteroidenvarianten entwerfen** prüft bei Vorbereitung dieser Übergabe noch die abschließende Darstellung.

Asteroidenkonzepte liegen unter `docs/Art/Asteroiden`. Ein erweiterbarer Generator, Asteroidentypen, Oberflächenshader, Detailstufen, Collider und Editor-Werkzeuge entstehen unter `Assets/SpaceMiner`. Vor dem Upload den finalen Import- und Prüfstand des Asteroiden-Chats übernehmen. Bereits vorhandener Quellcode allein bedeutet nicht, dass alle generierten Assets und Tests fertig sind.

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
