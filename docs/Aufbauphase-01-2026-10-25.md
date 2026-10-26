# Aufbauphase 01: Versorgung, Werkstatt, Forschung und Stationsplanung

Vom Nutzer am25.10.2026 bestätigt und zur gründlichen Umsetzung beauftragt. Ergänzung: eigenes Techtree-Terminal; beim Annähern an jeden Arbeitsplatz ein eindeutiger Hinweis „E – … öffnen“. Dieses Dokument unterscheidet Auftrag und tatsächlichen Prüfstand. Ausgangspunkt: Layout9, mittigere Anbaurahmen, zehn detaillierte Außendrohnen, nur01 aktiv; Leistungsprobleme gemeldet.

## Bestätigter Umfang

1. Innen stimmige Grund-/Arbeits-/Notbeleuchtung, außen Positions-, Dock- und Arbeitsleuchten; kein unmotiviertes Flackern. Kabeltrassen und getrennte Rohrleitungen im Wand-/Deckeneck, mit Sektor-Trennschaltern/Absperrventilen und nachvollziehbaren Versorgungswirkungen. Wenige kurze Entladungen an versorgten Schäden, nach Isolation aus.
2. Werkzeugregale in der Drohnenhalle für Greifer, Bohrer und Laser/Schweißwerkzeug, echter Werkzeugbestand und Umbauaufträge. Materialdrucker im rückwärtigen Kommandobereich; erste einfache Reparaturteile, keine universelle Chip-/Akkufertigung.
3. Wenige nahe Trümmer als bergbare Ressourcen, gesicherte Annahme unbekannten Schrotts, Untersuchung/Wiederverwendung/stoffgerechte Aufbereitung. Eigene Lager für unbekanntes Material, Kohlenstoff und Eisen, Flüssigkeiten in geeigneten Tanks. Drohnen transportieren feste Stoffe/Teile, Leitungen Strom/Flüssigkeiten; keine Förderbänder.
4. Gemeinsame Stationsbilanz: Solar/Reaktor, aktuelle Erzeugung/Verbrauch, Akku in kWh/%, Wasser, Luftreserve und Nahrung. Produktion, Forschung und Laden benötigen tatsächliche Versorgung. Erste Akkus als geschützter Technikschrank, später erweiterbar. Erste Fassung mit Reservoiren und Sektorstatus, ohne vollständige Gas-/Rohrströmungsphysik.
5. HUD oben mittig wichtigste Ressourcen; Aufgaben links ein-/ausklappbar, Klick öffnet Erläuterung mit Mira; Vitalwerte unten links. Auswahl rechts mit Status/Aktionen. Funktion innen/außen/Fenster/Vollbild und mit UI-Skalierung. Developer-Rundgang weiterhin schnell/ohne Staminaverbrauch.
6. Drei eigene Arbeitsplätze neben vorhandenem Scanner: Drohnenpult, Stationsplanungspult, Techtree-/Forschungsterminal. Annäherung/E-Hinweis; E öffnet passende Oberfläche, E/Escape schließt, Maus frei, keine Bewegung oder versehentlichen Welteingaben durch Dialoge.
7. Drohnenverwaltung mit Zustand, Werkzeug, Ladung, Reserven, Auftrag und Aktivierung.01 anfangs aktiv,02 erst nach Diagnose/Reparatur und ausreichender Wasser-/Energiereserve. Kleine interne R01 mit vorhandenen/geborgenen Teilen aktivieren; führt interne Transporte/Fertigung/Aufbereitung und Werkzeugumbauten aus. Weitere Außendrohnen zunächst inaktiv.
8. Stationselemente/Segmente/Module auswählbar, Identität/Art/Verwendung/Zustand/Versorgung/Bestand/Aufträge sichtbar. Reparieren, Abschalten, Absperren, Erweitern gemäß Voraussetzungen. Sehr langsamer, nutzungsabhängiger Verschleiß; passive Struktur deutlich langsamer als Maschinen. Frühzeitige Warnungen, praktische Erfahrung getrennt von Forschung.
9. Arbeitswarteschlange per Drag-and-drop priorisieren, Voraussetzungen bleiben verbindlich. Zustände für Material/Drohne/Energie/aktive Arbeit/Abschluss/Blockade. Sichere Unterbrechung, keine doppelte Buchung von Ressourcen/Arbeitern. Ziele und ausführbare Einzelaufträge getrennt.
10. Forschung schaltet Bauarten/Rezepte/Werkzeugrollen frei, im kategorisierten Baumenü auswählbar. Mira begleitet mit Dialogen. Vorhandener großer Technologiebaum bleibt erreichbar; echte Aufbau-Freischaltungen müssen an das Kampagnenmodell gebunden sein und gespeichert werden.

## Fortschrittsbogen und Nahrung

Scan/Bestandsaufnahme → Wasser sichern → Energie stabilisieren → wenige Trümmer bergen → interne R01 aktivieren → Kohlenstoffversorgung und Grundnahrung → optionale02 → Schrott-/Materialkreislauf → Stationsreparatur/Forschung/Ausbau. Arbeiten dürfen parallel laufen, solange Versorgung/Arbeiter reichen. Alle Startarbeiten müssen ohne bereits funktionierende neue Fertigung möglich sein.

Frühe chemische Nahrung bleibt bestehende, wiederherstellbare Anlage mit Notrationen und begrenzten Vorratszusätzen. Wasser/geeigneter Kohlenstoff/Energie liefern eine vereinfachte C/H/O-Grundlage, keine belegte vollständige Ernährung. Konkrete lebensmitteltaugliche Chemie ist nicht nachgewiesen; Spielrezept ausdrücklich Prototypabstraktion, Hilfsstoffe endlich. Pflanzen/Algen/Mikroorganismen bleiben spätere Pfade mit erst zu findenden Kulturen/Samen. Keine automatische freie Nahrung oder unbegrenzte Nährstoffquelle.

## Arbeitsblöcke und Abnahme

- A: Bestandsaufnahme/Leistungsreferenz, persistentes gemeinsames Zustands-/Auftragsmodell, Ressourcenbilanz.
- B: Terminals/E-Hinweise, HUD/Mira/Aufträge/Auswahl, tatsächliche Werkzeug-/Forschungs-/Baumenüaktionen.
- C: sichtbare Infrastruktur/Beleuchtung, Regale/Anlagen/Lager/Trümmer und bewegte Arbeitsdarstellung.
- D: vollständiger Fortschrittslauf, Speichern/Laden/Migration/Pause/Zeitraffung/UI-Skalierung, vorhandene Wasser-/Schleusen-/Bewegungsregressionen.
- E: erste Optimierung mit gemessenem Vergleich: Modell-LOD, Material-/Meshwiederverwendung, Lichtbudget, Collider-/Berechnungslast. Optik und spielrelevante Geometrie schützen.

Zahlen für Kapazitäten, Rezepte, Kosten, Dauer, Verschleiß und Startvorräte sind einstellbare Prototypwerte. Bestehende Saves müssen sichere Startwerte erhalten; keine alten Forschungs-/Nahrungsannahmen als fertige Mechanik ausgeben. Kosten erst bei tatsächlich möglichem Arbeitsbeginn reservieren, bei Abbruch korrekt zurückgeben beziehungsweise Teilergebnis erhalten. Nicht wiederholte Tests deutlich benennen. Fremde Reaktorarbeit bewahren, kein Commit/Push ohne weiteren Auftrag.

## Tatsächlicher Stand

25.10.2026: Erste spielbare Aufbauphase umgesetzt. [Bedienung, Klassen und Werte](Stationsversorgung-Architektur.md), [Prüfbericht](Pruefungen/Aufbauphase-01-2026-10-25.json). Finale Development-Binärdatei:97 Aufbau-,24 UI/Vollbild-,29 Vitals-,156 Stations- und78 Miningprüfungen, insgesamt384/Exit0. Echte aktive Wasserversorgung und spätere Arbeitskette, Speichermigration, reservierte Abbrüche und Wartungssperre geprüft.15 Stations-/UI-Bilder;256 Quellen/Metas/LOD-Assets und247 ausgelieferte Dateien abgeglichen. `Start-Aufbauphase-01.cmd` startet den aktuellen Build mit eigenen Vorschau-Saves.

Beleuchtung/Medien, Arbeitsplätze/Anlagen/Lager/Funde, R-01/02, Kampagnenforschung, reale Reparaturen/Bauabschnitte und Ressourcen-/Auftrags-HUD sind vorhanden. Neue Räume werden am echten Ringportal geöffnet; reparierte Lager erweitern den Tank. Erste LOD-/Material-/Meshoptimierung senkt synchrone Renderkosten in drei Ansichten um rund40–59%; kein FPS-Nachweis. Die lange Bauprobe mit vier Spezialrollen ist weiterhin eine separate Demonstration; die Kampagne zeigt zunächst vereinfachte Montage mit den freischaltbaren Startakteuren. Die vollständige Ökonomie des großen Labor-Techtrees ist noch nicht migriert.

Weitere Grenzen: Wasser-/Energie-/Materialfixtures für spätere Einzelprüfungen; kein kompletter balancierter Langzeitdurchlauf, kein separater Elektrolyse-Langlauf, kein neuer Release-/100k-Stresstest oder manueller Tastatur-/Maus-Komforttest. Standalone-Mining-Batchbilder nicht zur Bildabnahme verwendet. Chemie, Mengen, Massen und Baukosten bleiben Prototypabstraktionen. Alte156er-/263er-Nachweise sind durch den eigenen neuen Aufbaucheck ergänzt, nicht rückwirkend erweitert. Dokumentation/Übergabe/Memory aktualisiert; fremde Reaktorarbeit bewahrt; kein Commit/Push.
