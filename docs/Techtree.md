# Tier-I-Techtree

## Bedienung und bestätigte Darstellung

Das Verzweigungs-Symbol rechts oben, unmittelbar links neben dem Settings-Zahnrad, öffnet den Forschungsbaum. Beide Einstiege zeigen nur ein Symbol und besitzen einen Tooltip; sie bleiben auch bei verborgenem HUD verfügbar.

Tier I läuft von links nach rechts. Die Icons tragen keine dauerhaften Textbeschriftungen. Hover zeigt Namen, Status, Voraussetzungen und praktische Nutzung; Klick heftet die Information an. Vorausgehende Verbindungen werden hervorgehoben. Das × in der Information löst die Auswahl, das × oben rechts oder Escape schliesst das Menü.

Jede Verbindung besitzt eigene kleine quadratische Eingangs- und Ausgangsanschlüsse. Mehrfachanschlüsse sind versetzt. Kanten sind rechtwinklig; Verbindungen auf gleicher Höhe laufen direkt waagrecht und benötigen keine mittigen Anschlüsse. Navy-Flächen, Amber-Akzente, Cyan-Systemkanten und UI-Töne verwenden den vorhandenen Settings-Stil.

Settings und Techtree sind gegenseitig ausschliessende Menüs. Kamera, Auswahl, HUD und Display-Shortcuts werden modal gesperrt, auch im Schliessframe. Die Simulation folgt der vorhandenen Player-Einstellung `PauseInMenu`. Das Intro pausiert einschliesslich Stimme. Der vorhandene Configuration-Hintergrund wird auch im Techtree genutzt.

## Umfang und offene Forschung

Dieser Schritt setzt die bestätigte Darstellung ins Spiel um. Noch keine Knowledge-Kurve, Forschungsverbrauch, Forschungszeiten, Speicherung oder zusätzliche Gameplay-Freischaltungen. Das Menü zeigt vorhandene Startfähigkeiten und geplante Forschungsfelder, keine erfundenen Erfahrungswerte oder laufenden Forschungsfortschritte. Der vorhandene Eisauftrag bleibt die aktuelle Spielmechanik. Die genaue Technologiekette ist ein Entwurf; Materialeigenschaften und Tier-Aufstieg bleiben offen. Reale Verfahren müssen vor ihrer Umsetzung fachlich recherchiert werden.

Grundprinzip für spätere Progression: Tätigkeit erzeugt fachbezogenes Wissen, Entdeckung öffnet Forschungswege, Forschung erweitert Möglichkeiten. Wissen, Maschinen, Ressourcen und Energie sind getrennte Voraussetzungen. Vorhandene Technik wird nicht erneut erforscht. Frühere Tiers sollen nachholbar bleiben. Tier II bis VI sind noch nicht ausgearbeitet.

## Dateien und Prüfung

- `TechnologyCatalog.cs`: Tier-I-Anzeigeeinträge, Voraussetzungskanten und `TechnologyLayout`; kein Simulationszustand.
- `TechnologyIcons.cs`: lokale Piktogramme ohne Schriftglyphen-/Downloadabhängigkeit.
- `TechTreeMenu.cs`: IMGUI-Menü, Overlays und modale Bedienung; Laufzeitinstallation mit Settings.
- `SettingsMenu.cs`, `IntroSequence.cs`, `ConfigurationAudio.cs`, `SettingsUiAudio.cs`: gezielte Menüintegration.
- `Editor/TechTreeValidation.cs`: Daten-/Routingprüfung und Development-/Release-Build ohne Szenen-Neugenerierung.

Bei geschlossenem Unity-Editor `SpaceMiner.Editor.TechTreeValidation.Build` im Batchmodus ausführen. Development unter `Builds/Windows`, Release unter `Builds/TechTreeRelease`. Das Development-Spiel mit `-techTreeCheck` prüft Menüintegration und erzeugt `Logs/techtree-*.png`. Der vollständige Spieltest bleibt `tools/Unity.ps1 Check -Visible`. Nur tatsächlich ausgeführte Ergebnisse werden im Projektmemory festgehalten.

### Prüfung am 06.10.2026

Unity 6000.4.7f1: finale Development- und Release-Builds erfolgreich; Daten-/Routingprüfung mit 17 Knoten und 22 Kanten bestanden. Menütest einschliesslich aktiver Drohnenpause, Ressourcenbilanz, Settings-Wechsel und Schliessframe mit Exit 0 bestanden. Die bestehende Spielintegration bestand 789 Prüfungen; anschliessend wurde nur der Overlay-Eingabeschutz geändert, erneut gebaut und menügeprüft. Einstieg, Baum, Overlay und 140%-Schrift mit High Contrast anhand der Screenshots visuell kontrolliert. Protokolle: `Logs/techtree-complete-build.log`, `Logs/techtree-complete-check.log`, `Logs/techtree-smoke.log`. Eine rein subjektive Hörprüfung wurde in dieser Sitzung nicht vorgenommen.
