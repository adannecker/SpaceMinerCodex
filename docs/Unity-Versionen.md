# Bewusste Unity-Versionswahl

**Späterer Integrationsnachweis26.10.2026:** Der kombinierte Laptopstand einschließlich I75-R03 wurde jetzt mit der festgelegten6000.6.5f1 als Development und Release gebaut; 1523 Playerprüfungen (949 Verkehr, 477 Strom/R-01, 97 Kampagne), drei grafische Development-Läufe/Exit0. Kein erneuter Versionswechsel und keine vollständige Neuabnahme aller älteren Features. [Spielintegration und Prüfgrenzen](Innendrohne-I75-R03-Spielintegration-2026-10-26.md). Frühere Abgleichsgrenzen unten bleiben als Historie erhalten.


Nutzerentscheidung vom 12.10.2026: jetzt auf die aktuellste stabile Unity-Version wechseln; künftige Versionswechsel proaktiv prüfen und bewusst entscheiden.

## Verbindliche Regel für alle Rechner und Chats

Abgleich vom26.10.2026: Der Remote-Commit659b514 enthält noch6000.4.7f1; seine neueren Prüfnachweise stammen aus isolierten6000.6.4f1-Kopien auf dem anderen Rechner. Beim hiesigen Projektupdate wurde die vorhandene, beschlossene6000.6.5f1-Migration nach Sicherung wieder angewendet. Der kombinierte hiesige Quellstand ist noch nicht mit Unity geprüft. Die folgenden erfolgreichen Migrationstests gelten ausschließlich für den Stand vom12.10.; sie bestätigen keine spätere Integration. Migration/Wissensupdate noch lokal und uncommittet, siehe [Projektabgleich](Projektabgleich-Laptop-2026-10-26.md).

`ProjectSettings/ProjectVersion.txt` legt die gemeinsame Editorversion samt Revision fest. `tools/Unity.ps1` liest diese Version für Start und Build, statt eine frühere Version fest einzubauen. Ein fehlender Editor führt zu einer erklärenden Fehlermeldung; es gibt keinen automatischen Wechsel auf eine andere installierte Version. `-UnityPath` ist für abweichende Installationsorte vorgesehen, nicht als stiller Versionswechsel.

Vor neuen Entwicklungsabschnitten und Rechnerwechseln die festgelegte Version mit dem offiziellen [Releasearchiv](https://unity.com/releases/editor/archive) abgleichen. Neue stabile Versionen, Nutzen, bekannte Probleme und Paketkompatibilität benennen und eine konkrete Versionswahl vorschlagen. Ohne ausdrücklichen Upgradeauftrag bleibt die festgelegte Version erhalten. Alpha/Beta/Preview werden nur auf ausdrücklichen Wunsch gewählt. Pakete werden separat bewertet und nicht pauschal mitaktualisiert.

## Ablauf eines beschlossenen Wechsels

1. Arbeitsstand prüfen, fremde Änderungen erhalten und Ausgangscommit festhalten.
2. Zielversion und Revision aus dem offiziellen Katalog bestimmen; Editor zusätzlich installieren, bisherige Installation vorerst behalten.
3. Bei geschlossenem Editor das vorhandene Projekt mit der Zielversion importieren. Keine Szene über PrototypeSetup neu erzeugen.
4. Projektversion, notwendige Paket-/API-Anpassungen und automatisch migrierte Dateien prüfen. Development und Release der vorhandenen Szene bauen; betroffene Playerprüfungen ausführen.
5. Tatsächliche Ergebnisse und Grenzen in Projektmemory/Prüfbericht festhalten. Beide Rechner verwenden anschliessend dieselbe festgelegte Version; lokale Builds werden neu erstellt.

## Wechsel am 12.10.2026

Ausgangscommit `13fc488`, bisheriger Quelleditor 6000.4.7f1. Neue verbindliche Version: **6000.6.5f1**, Revision **3ff58d469c8a**, stabile Veröffentlichung vom 08.10.2026. Der offizielle Releasekatalog wurde am 12.10.2026 abgefragt; 6000.7 war nur als Alpha/Beta verfügbar. Installation über Unity Hub und Migration des gemeinsamen Checkouts abgeschlossen. Die bisherige Installation6000.4.7f1 bleibt erhalten.

Unity migrierte `ProjectSettings.asset` auf Serialisierungsversion30 und legte `PhysicsCoreProjectSettings2D.asset` sowie `ProjectAuditorSettings.asset` an. `Packages/manifest.json` blieb unverändert; der Editor aktualisierte seine eingebauten Testabhängigkeiten im Lockfile (Test Framework1.8.0, NUnit2.1.0). Keine manuellen Paketupdates, keine Spielcodeänderung und keine Szenenneugenerierung erforderlich.

Settings-/Forschungs-/Viewportprüfungen, Development-/Release-Build, Techtree-Demobuild und Mining-Build erfolgreich. Sichtbarer Development-Player: Startstation121 und gemeinsame Präsentation28 Checks bestanden. Mining69 sowie Balance195 Checks bestanden. Startstation, Techtree-Sidebar und Mining-Frachtbox anhand neuer Screenshots visuell geprüft. Versionssperre gegen6000.4.7f1 und PowerShell-Syntax geprüft. Nachweis: [Unity-Upgrade](Pruefungen/Unity-Upgrade-2026-10-12.json).

Normale Startdateien verwenden die neu bereitgestellten Builds unter `Builds/Windows` und `Builds/WindowsRelease`; Vorgänger liegen daneben mit Suffix `before-unity-6000.6.5f1-*`. `Start-TechTree-Test.cmd` und `Start-Mining-Drohne.cmd` verwenden ebenfalls neu gebaute Anwendungen. Andere separate Showcases wurden nicht neu gebaut. Kein gesonderter Release-Spieltest, kein vollständiger manueller Tastendurchlauf, Hörtest oder Leistungsbenchmark. Historische Prüfungen mit6000.4.7f1/6000.6.4f1 bleiben als solche erhalten.
