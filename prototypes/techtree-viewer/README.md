# Techtree-Vorschau

Eigenständige Darstellung aller elf Kapitel-1-Bäume. Keine Unity-Kopie und kein Unity-Build erforderlich. Der Server liest den aktuellen Forschungskatalog direkt aus `Assets/SpaceMiner/Resources/Research/catalog.json`.

Start in diesem Ordner: `npm start`, anschliessend http://127.0.0.1:4317 öffnen. Voraussetzung: Node.js. Keine zusätzlichen Pakete. Geometrieprüfung: `npm test`.

Gesamtbaum standardmässig vollständig sichtbar. Zoom endet an der verfügbaren Höhe. Ausschliesslich horizontal per Ziehen verschieben, ohne Scrollbalken. Details öffnen sich per Klick oder Tastatur. Individuelle Ein-/Ausgangsports und orthogonale Linien; auch zusätzliche lokale Elternverbindungen werden dargestellt. Die Vorschau stellt Katalogentwürfe dar und simuliert keine Forschung oder Spielstände. Kosten, Balancingzeiten und externe Freischaltungen sind hier keine verbindliche Spielaussage.

36 technische Miniaturbilder in `assets/technology-atlas-v1.png`, explizite Zuordnung für alle 159 Einträge in `assets/atlas.json`. Bilder wurden mit dem eingebauten Imagegen am 09.10.2026 erzeugt. Mehrere passende Technologien verwenden dasselbe Motiv. Der Atlas bleibt für eine spätere Unity-Integration wiederverwendbar. Prompt: siehe `assets/image-generation.md`.

Beenden-/Speicherdialog und allgemeine Auflösungsskalierung des Unity-Spiels sind nach Nutzerentscheidung anschliessende Aufgaben. Diese Vorschau verändert das Unity-Spiel nicht.

## Iconrevision 09.10.2026

Aktive Darstellung: 159 einzeln zugeordnete SVG-Techniksymbole unter `assets/icons/`, Manifest `assets/icons.json`, reproduzierbar mit `node generate-icons.mjs`. Gemeinsame Objektfamilien (z.B. Solarflächen oder Speicher) bleiben erkennbar; individuelle Schaltungsdetails und Funktionszeichen unterscheiden die Technologien. Eng begrenzte Palette: Navy/dunkles Blau/helles Blau, Gelb als Funktionsakzent. Keine identisch wiederverwendete Icondatei. Der ältere 36-Bilder-Atlas ist als früherer Entwurf erhalten, wird von der Vorschau nicht mehr geladen.

Prüfung: zwölf Node-Tests bestanden, einschliesslich 159 unterschiedlicher SVG-Inhalte, vollständiger Katalogzuordnung und Palettenprüfung. Detailansicht im Browser gerendert und visuell kontrolliert. Keine Unity-Integration in diesem Durchgang.

## Realistische Icons v3 (09.10.2026)

Nutzerkorrektur: räumliche, realistischere Miniaturbilder bevorzugt; SVG-Linienicons werden nicht mehr aktiv angezeigt. Drei neue, mit Built-in Imagegen erzeugte Bildatlanten: `assets/realistic-v3-1.png` bis `realistic-v3-3.png`. 159 separat zugeordnete Bildbereiche, keine Wiederverwendung derselben Zelle. Varianten verwandter Technologien zeigen andere Bauformen und Ausstattung. Entsättigte blau-graue Metall-/Glasoptik, kleine gelbe Akzente; CSS reduziert die Sättigung zusätzlich. Bilder sind illustrative technische Entwürfe, keine verbindlichen Baupläne oder chemischen Prozessdarstellungen.

Vollständiger Prompt-Satz/Provenienz: `assets/realistic-v3-prompts.json`. Tatsächlich erzeugte Raster wurden visuell kontrolliert und im Manifest berücksichtigt: 10×6,10×6,9×6. Manifest `assets/realistic-v3.json`, reproduzierbare Zuordnung `node build-realistic-manifest.mjs`. Ältere SVG-/Bildentwürfe bleiben erhalten. Prüfung: 13 Node-Tests bestanden, darunter vollständige 159er-Zuordnung mit eindeutigen Bildbereichen; Atlanten und Detailkarte visuell geprüft. Unity nicht geändert.

## Kompakte Ansicht (aktuell09.10.2026)

Einheitliche72px-Ausgangsicons über alle Kategorien,25% kleinere Ausgangsgeometrie,1:1-Reset statt automatischer Gesamteinpassung. Grosse Bäume ohne Scrollbalken in beiden Richtungen ziehen. Direkte Endpunktgruppen nutzen einen gemeinsamen Ausgang und eine horizontale Verteilerlinie mit unten andockenden Karten. Aktuelle Regel ersetzt frühere Höhenbegrenzung/Autoeinpassung/nur horizontale Navigation. Unity-Demo nutzt denselben Export. Siehe docs/Forschungslabor.md für Prüfung und Auflösungsskalierung.
