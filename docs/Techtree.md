# Tier-I-Techtree

## Bedienung und bestätigte Darstellung

Das Verzweigungs-Symbol rechts oben, unmittelbar links neben dem Settings-Zahnrad, öffnet den Forschungsbaum. Beide Einstiege zeigen nur ein Symbol und besitzen einen Tooltip; sie bleiben auch bei verborgenem HUD verfügbar.

Tier I läuft von links nach rechts. Die Icons tragen keine dauerhaften Textbeschriftungen. Hover zeigt Namen, Status, Voraussetzungen und praktische Nutzung; Klick heftet die Information an. Vorausgehende Verbindungen werden hervorgehoben. Das × in der Information löst die Auswahl, das × oben rechts oder Escape schliesst das Menü.

Jede Verbindung besitzt eigene kleine quadratische Eingangs- und Ausgangsanschlüsse. Mehrfachanschlüsse sind versetzt. Kanten sind rechtwinklig; Verbindungen auf gleicher Höhe laufen direkt waagrecht und benötigen keine mittigen Anschlüsse. Navy-Flächen, Amber-Akzente, Cyan-Systemkanten und UI-Töne verwenden den vorhandenen Settings-Stil.

Settings und Techtree sind gegenseitig ausschliessende Menüs. Kamera, Auswahl, HUD und Display-Shortcuts werden modal gesperrt, auch im Schliessframe. Die Simulation folgt der vorhandenen Player-Einstellung `PauseInMenu`. Das Intro pausiert einschliesslich Stimme. Der vorhandene Configuration-Hintergrund wird auch im Techtree genutzt.

## Umfang und offene Forschung

Wasserabbau besitzt seit 06.10.2026 eine erste Erfahrungsprogression. „Wasserabbau optimieren“ zeigt Wissenslevel, Fahrtenfortschritt, tatsächliche Förderrate und Abbauleistung. Klicken heftet das Overlay an; darin wählt der Spieler Förderrate oder Energieeffizienz für den nächsten Aufstieg. Gemeinsames Materialwissen startet auf Level 1, 20 produktive beendete Fahrten ergeben einen Aufstieg. Förderrate steigt pro zugeordnetem Level um 15 %, elektrische Abbauleistung sinkt bei Effizienz um 15 %; beide werden auf dem vorherigen Wert fortgeschrieben. Schwerpunktwechsel erhält gelernte Verbesserungen. Dies verändert keine Hardwaregeneration und verbraucht keine Forschungspunkte. Details, Testparameter und gemessene Zeiten: [Bergbaudrohnen](Bergbaudrohnen.md).

Die anderen Forschungsfelder bleiben Entwürfe ohne Forschungsverbrauch, Forschungszeiten oder zusätzliche Freischaltungen. Speichern/Laden fehlt auch für das neue Abbauwissen. Die genaue Technologiekette, Materialeigenschaften und Tier-Aufstieg bleiben offen. Reale Verfahren müssen vor ihrer Umsetzung fachlich recherchiert werden.

Grundprinzip für spätere Progression: Tätigkeit erzeugt fachbezogenes Wissen, Entdeckung öffnet Forschungswege, Forschung erweitert Möglichkeiten. Wissen, Maschinen, Ressourcen und Energie sind getrennte Voraussetzungen. Vorhandene Technik wird nicht erneut erforscht. Frühere Tiers sollen nachholbar bleiben. Tier II bis VI sind noch nicht ausgearbeitet.

## Beschlossene Ausbaurichtung: Akku und Ladestation (06.10.2026)

Der Nutzer bestätigt ungefähr 1:2 als Startziel für aktive Abbauzeit gegenüber dem Nachladen der dafür verbrauchten Energie. Flug, Bordversorgung, Ladeverluste und Ladeendphase kommen hinzu; dies ist kein fest programmierter Wartezeitfaktor und noch nicht in die aktuelle Testbalance übernommen.

Akku und Ladestation sollen durch spätere Tiers deutlich verbessert werden können. Für die Ausarbeitung getrennt führen: Akkukapazität in kWh für längere Einsätze; zulässige Lade-/Entladeleistung für stärkere Werkzeuge und Schnellladung; Ladeleistung der Station in kW für kürzere Aufenthalte. Konkrete Technik, Tiernummern, Faktoren, Kosten und Freischaltungen bleiben offen. Energieerzeugung, Stromverteilung und Wärmeabfuhr müssen die jeweilige Ausbaustufe unterstützen. Ein größerer Akku allein ist kein Schnellladebonus; Ladestation und Akku müssen zueinander passen.

Diese Fortschritte gehören zur erforschten Hardware und den bestehenden Umbau-/Neubauprinzipien, getrennt von der bereits implementierten Materialerfahrung. Die geplante Batteriewechselstation bleibt eine zusätzliche Logistikoption, siehe [Ideenbacklog](Ideenbacklog.md). Noch keine neuen Techtree-Knoten oder Hardwareeffekte implementiert.

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
