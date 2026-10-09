# Story, Dialoge und Cinematics

## Gemeinsame Mira-Visierdarstellung (09.10.2026)

Alle vorhandenen Mira-Dialoge im Spiel, die 16 Erwachen-Cues und alle zwölf Erinnerungs-Szenen verwenden `MiraVisorOverlay.cs`: gezeichnetes Porträt links im cyanfarbenen Rahmen, Titel und Text rechts daneben, darunter Kanal/Bedienhinweis. Lange Texte sind scrollbar; Textgröße und Untertitelschalter werden berücksichtigt. Die Questleiste bleibt separat. Innenraum-Bedienhinweise werden während eines Mira-Dialogs ausgeblendet, damit sie den Visierbereich freihalten. Statische Ausdrucksvarianten passen zum jeweiligen Abschnitt. Keine neue Sprachaufnahme, keine Änderung von Storytext, Regie, Enceladus-Narration oder Musik. Der bereits exportierte Cinematic-Film bleibt unverändert; die neue Darstellung betrifft die Unity-Wiedergabe. Prüfung: [Techtree/Mira](../Pruefungen/Techtree-Mira-Visier-2026-10-09.json).

## Geplante Forschungsdialoge mit Mira (08.10.2026)

Mira soll als Stations-KI mit Avatar Forschungsvorschläge machen und Projekte unter zusätzlichem Energiebedarf begleiten. Erstes Nutzerbeispiel: Abbaudrohnen zu Außen-Reparaturdrohnen umfunktionieren; eine aktivierte interne Helferdrohne setzt den Umbau im Dock um. Spätere Dialoge sollen Anlass, Voraussetzungen und Fortschritt verständlich machen. Noch keine neuen Forschungsdialoge aufgenommen oder implementiert. Zielbild und offene Helferaktivierung: [Spielidee](../Spielidee.md#chapter-1-drohnen-mira-forschung-und-ausbauziele-08102026).

## Aktuelle Sequenzen

Aktueller Stand 08.10.2026: [Erwachen und erster Auftrag](01_Intro_Erwachen.md) verwenden die freigegebene Aoede-Stimme mit 14 unveränderten lokalen WAV-Cues. Zwei neue Scan-Introtexte und die ergänzten Questdialoge sind vorerst Text; passende neue Aoede-Aufnahmen fehlen noch. Hedda-Originale liegen unter Sprachproben/Hedda_Original. Die ältere Maya-Vorschau (Resources/Intro/mira_maya_preview.mp3 und 01_Intro_Maya.srt) bleibt als historische Alternative archiviert; sie ist nicht die aktive Stimme.

Das [zwölfteilige Erinnerungs-Cinematic](IntroCinematic/README.md) erzählt die Vorgeschichte mit Kohlezeichnungen, durchgehender Enceladus-Narration und leiser Charcoal-Atmosphere-Musik. Alle Originalbilder, WAV-Dateien, Revisionen und Bildprompts liegen unter IntroCinematic. Szene 9 verwendet die korrigierte v2-Fassung. Das fertige Video ist outputs/Cinematic/SpaceMiner-IntroCinematic.mp4.

Beide Sequenzen sind in der Galerie für Cutscenes und Cinematics enthalten. Untertitel werden gemeinsam in den persönlichen Einstellungen geschaltet. Regie und Sprechertexte sind Projektquellen; historische Chatarchive ersetzen die aktuelle Umsetzung nicht. Weitere Dialogentwürfe bleiben als solche gekennzeichnet.
