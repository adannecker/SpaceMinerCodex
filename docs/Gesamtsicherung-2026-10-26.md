# Gesamtsicherung für den Rechnerwechsel — 26.10.2026

Der gemeinsame Stand aller Projekt-Themen wird zusammen gesichert: Unity-Quellen, Station/Versorgung/Drohnen/Planer, Mira/Quest-Audio, Optionen/Oberflächen, native Modellquellen, Exporte/PBR, Konzepte, Werkzeuge, Raumpläne, lesbare Gesprächsarchive und tatsächliche Prüfbelege. Dieser Sicherungsauftrag setzt den nächsten Sprint noch nicht um.

## Zuerst auf dem anderen Rechner lesen

1. [Rechnerwechsel-Anleitung](Rechnerwechsel-Memory.md): Clone/Pull, lokale Einrichtung, neutraler Unity-Build und kopierbare Startaufträge für die neun Themenchats.
2. [Chat-Gesamtauswertung](Chat-Auswertung-2026-10-26.md): aktuelle Entscheidungen, historische Widersprüche, fehlende Quellen und fachliche Zuständigkeiten.
3. [Dienste, Webseiten und lokale Werkzeuge](Dienste-und-Webseiten.md): Unity, GitHub, Codex/ImageGen, Blender, Tripo, Google-TTS, Suno/ElevenLabs-Historie, Steamworks und optionale Werkzeuge; persönliche Anmeldung separat.
4. [Modellkatalog](Modellkatalog-Rechnerwechsel-2026-10-26.md): Blender/FBX/GLB, Texturen/Materialien, Generatoren, aktuelle Spielzuweisung und noch offene Integration.
5. [Nächster Sprint SP-01…SP-10](Ideenbacklog.md#nächster-sprint-verbindlicher-umfang-und-arbeitsreihenfolge--26102026): beschlossener Umfang samt Abhängigkeiten und Fertig-Kriterien. Vorhandene Teilfunktionen wiederverwenden.

## Quellen und Modelle

**41 Modelldateien einschließlich 18 nativer Blender-Szenen** erfasst; 447 Modell-/Material-/Textur-/Nachweisdateien mit SHA256 gesichert. Native TouchConsole, BatteryBay, I75-R01/R02, Halle und historische Mira-Ausarbeitungen bleiben erhalten. Beide Mira-Tripo-Originale, externe Drohne/Ladeaufnahme und ihre Originalkarten liegen ausgepackt im Projekt. Sämtliche 27 Einträge der fünf ignorierten Tripo-ZIPs sind CRC-geprüft und ausgepackt bytegleich vorhanden; keine native Quelle versteckt sich nur im ZIP.

93 relevante YAML-/Metadateien auf Auflösung geprüft; keine fehlenden Meta-Paare, keine fehlenden GUID-Ziele im geprüften Umfang, keine Asset-GUID-Duplikate. Keine nicht ignorierte Datei erreicht 100 MiB. Große historische Quellen bleiben unverändert erhalten; keine Modellvereinfachung oder Neutexturierung für diesen Sicherungsauftrag. [Modellnachweis](Pruefungen/Modelldateien-Rechnerwechsel-2026-10-26.json).

**Integrationsgrenzen:** TouchConsole und I75-R02 sind zunächst Modellvorschauen. Die Fahrprobe benutzt noch die bisherige I75-V02-Zuweisung. Das neue Pultmodell enthält keine echte Live-Spielanzeige; die Blender-Demo ist separat. Der Reaktor ist im Spiel prozedural, seine ausgearbeitete Konzeptzeichnung ist kein bereits vorhandenes Blender-Reaktormodell. High-/Low-Mira und Questmodule existieren; der Sprint verlangt den verbleibenden Hauptspielabgleich.

## Projektchats und Entscheidungen

Alle **24 vorhandenen Archive** gelesen; neun lokale registrierte Quellen mit **1.312 geprüften Textnachrichten** aktualisiert. Der bereits bereinigte MainDev-Ursprung bleibt erhalten. Neun ältere Quellen nur als Archiv bewahrt, drei Cloud-Momentaufnahmen ausdrücklich historisch; ein alter Arbeitschat hat weiterhin keine verfügbare Quelle. Die jüngere Ideenbacklog-Sitzung ist lokal nicht verfügbar, ihr bisheriges Archiv bleibt erhalten. Kein vollständiger Export unerreichbarer Sitzungen behauptet.

Die Archive enthalten sichtbare Projekttexte; keine Analyse-/System-/Werkzeugprotokolle oder rohen Sitzungsdateien. Export nach festgehaltenen Reviewgrenzen, bekannte Zugangsdaten-/E-Mail-/signierte-URL-Muster bereinigt. **25 synthetische Exporterprüfungen bestanden**, 24 Archivhashes protokolliert. Das sind überprüfbare Quellen für neue Chats, kein automatischer Sitzungsimport. [Exportstatus](Chats/export-status.json), [Exportprüfung](Chats/export-verification-2026-10-26.json).

Wichtige spätere Entscheidungen stehen vor alten Notizen: Akkupaar **0° / Index 0–1 direkt über den Ringgang gegenüber dem Zentraleneingang**; 180° ist überholt. Nur Außendrohne01 zu Beginn produktiv; weitere Aktivierungen bedingt, zehn I75 in der Verkehrsprobe sind keine zehn produktiven Kampagnendrohnen. Nächster Sprint enthält zehn vereinbarte Pakete; neue Energie-/Laderaten und eine automatische Sonnennachführung sind nicht stillschweigend beschlossen.

## Frische gemeinsame Buildprüfung

In dieser Sicherungsrunde wurde der vollständige aktuelle Stand erneut gebaut, nicht nur frühere Einzeltests übernommen:

- **Development-Build erfolgreich / Unity Exit0**, bestehende Kampagnenszene mit `SpaceMiner.Editor.StartMenuBuild.Run`, ohne Setup/Neugenerierung.
- Quelle weiterhin **6000.4.7f1**; Builder **6000.6.4f1 in isolierter Kopie**. Nur dort Editorversion und automatisch aufgelöste eingebaute Testpakete geändert; tatsächlicher Stage-Lock gesichert. Keine Spielskript-/Assetabweichung im Snapshot.
- **477 Strompult- plus 53 Mira-Questintegrationsprüfungen = 530**, beide Prozesse Exit0 auf derselben Binärdatei.
- **1.949 Hauptquelldateien und drei Nutzerraumpläne unverändert**, 247 separat kopierte Playerdateien hashgleich. Lokales Ergebnis `Builds/TransferPreview-20261026`; vorhandener `Builds/Windows`-Player bleibt erhalten.
- 14 aktuelle Strompultbilder gesichert, Übersicht gesichtet. Zwei schwarze Bilder des versteckten Mira-Laufs ausdrücklich als Sichtnachweis verworfen; dessen 53 AudioSource-/Quest-/Saveprüfungen bestanden. Keine neue Mira-Sicht-, Hörkomfort-, native OS-Eingabe-, Headset-, Langzeit-, Release- oder FPS-Abnahme.

[Vollständiger Build-/Testnachweis mit Manifesten, Logs und Bildern](Pruefungen/Rechnerwechsel-Build-2026-10-26.json). Der Quell-Editor 6000.4.7f1 und der neue Zielrechner sind durch diese Prüfung nicht automatisch abgenommen.

## Git-Umfang und persönliche Daten

Gesichert wird der gesamte nicht ignorierte Projektstand einschließlich aller darin enthaltenen Modelle und früheren Prüfbelege, ungefähr 2,5 GiB Arbeitsbaum zur Auditzeit; das ist nicht die Größe des neuen Git-Pakets. Vor dem Commit wurden Dateigrößen und konkrete Zugangsdatenmuster geprüft, ohne Schlüsselwerte auszugeben. [Übertragungsprüfung](Pruefungen/Rechnerwechsel-Dateipruefung-2026-10-26.json). Musterprüfung ist kein allgemeiner Nachweis beliebiger geheimer Prosa.

Builds/Library/gewöhnliche Logs, heruntergeladene Programme und Abhängigkeiten unter `work`, Browserprofile, Zugangsdaten und persönliche Save-/Settingsdateien bleiben außerhalb des Git-Transfers. Relevante bestehende Modell-, Audio- und Viewerresultate sowie notwendige Generatoren sind im Projekt; externe Tools werden neu eingerichtet. Für die Fortsetzung derselben persönlichen Kampagne Saves zusätzlich bewusst sichern. Ein neues Online-Generieren ist für normalen Import/Build nicht nötig.

Branch **main**, Remote `https://github.com/adannecker/SpaceMinerCodex.git`. Ausgangscommit vor dieser Sicherung `bba0a6b`; Fetch zeigte vor Beginn keine lokalen/Remote-Zusatzcommits. Der Sicherungscommit wird mit `git log -1 --oneline` identifiziert. Nach erfolgreichem Push müssen `HEAD` und `origin/main` gleich sein; das abschließende Sicherungsprotokoll im Chat nennt den tatsächlich bestätigten Hash. Keine älteren „kein Commit/Push“-Absätze als Status dieser Gesamtsicherung lesen; sie beschreiben den damaligen Einzelauftrag.
