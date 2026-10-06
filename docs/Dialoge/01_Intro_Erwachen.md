# Intro — Erwachen

## Status und Rahmen

- **Status:** Entwurf 04 / Maya-Testaufnahme vom 06.10.2026. Neuer Sprechertext mit Raumstation und einer einsatzfähigen Drohne; erste ElevenLabs-Aufnahme auf Nutzerauftrag eingebaut. Endgültige Stimmwahl bleibt offen.
- **Auslöser:** Beginn eines neuen Spiels.
- **Sprecher:** Mira, Bordcomputer mit weiblicher Stimme.
- **Sprechweise:** sanft, warm und ruhig. Sorge bleibt hörbar, ohne dramatischen Alarmton. Natürliche Stimme mit dezenter technischer Klanggestaltung.
- **Anrede:** du.
- **Dauer der Testaufnahme:** 63,086 Sekunden; anschließend 1,2 Sekunden Ausklang im Spiel.
- **Ziel:** Die Ausgangslage verständlich machen, den Bordcomputer als Begleiterin vorstellen und zur Suche nach Wasser hinführen.
- **Grundlage:** [Beschlossene Spielbedingungen](../Spielidee.md).

## 1. Eine Stimme im Dunkeln

**Bild und Ton:** Schwarzer Bildschirm. Ein leises Lüftergeräusch, dann ein einzelner elektrischer Impuls. Kein lauter Auftakt. Die Stimme beginnt, bevor das erste Bild sichtbar wird.

**Einblendung:** `NOTFALLSYSTEM AKTIV`

**Sprechertext:**

> Du kannst mich hören … gut.
>
> Ich bin Mira. Dein Bordcomputer.
>
> Lass dir Zeit. Du musst noch nichts tun.
>
> Du hast die Explosion überlebt.

**Regie:** Nach dem letzten Satz eine längere Pause. Die erste Ansprache klingt vorsichtig und erleichtert.

## 2. Die Raumstation

**Bild und Ton:** Die beschädigte Raumstation wird langsam sichtbar. Schwache Positionslichter; Funken und Systemgeräusche bleiben geplante Inszenierung.

**Einblendungen, nacheinander:**

- `STATIONSSTATUS: BESCHÄDIGT`
- `STROMVERSORGUNG: NOTBETRIEB`

**Sprechertext:**

> Unsere Raumstation ist schwer beschädigt. Viele Systeme antworten nicht mehr.
>
> Wir arbeiten auf Notstrom. Die internen Batterien müssen erst wieder aufgeladen werden.
>
> Die Lebenserhaltung funktioniert noch. Aber unsere Vorräte sind begrenzt.

**Regie:** Sachlich und behutsam. Die Bestätigung zur Lebenserhaltung gibt einen kurzen Moment der Ruhe; der folgende Satz führt die Unsicherheit ein.

## 3. Was uns geblieben ist

**Bild und Ton:** Die Kamera öffnet den Blick auf den Asteroidengürtel. Eine einsatzfähige Drohne steht an der Station bereit; die übrigen warten auf Reparatur. Die ältere Gameplaylogik mit Drohne 02 als unversorgter Reserve ist noch abzugleichen.

**Einblendungen, nacheinander:**

- `POSITION: ASTEROIDENGÜRTEL`
- `DROHNEN: 1 VON 10 EINSATZFÄHIG`
- `AKTUELL EINSETZBAR: 1`

**Sprechertext:**

> Um uns herum liegt ein Asteroidengürtel.
>
> Von unseren zehn Drohnen ist nur eine einsatzfähig. Die anderen müssen wir reparieren.
>
> Für diese eine haben wir noch Treibstoff und Energie.
>
> Mit ihr können wir Rohstoffe bergen … und damit beginnen, die Station wieder aufzubauen.
>
> Wir müssen ihre Einsätze sorgfältig planen.
>
> Für Reparaturen brauchen wir Material. Für neue Technik brauchen wir Wissen. Vielleicht finden wir Antworten in alten Aufzeichnungen und Artefakten.

**Regie:** Etwas zuversichtlicher. Die Drohnen wirken wie eine konkrete Möglichkeit, tätig zu werden. Kein schneller Wechsel vieler Anzeigen.

## 4. Der erste Schritt

**Bild und Ton:** Eine Drohne löst sich vorsichtig von ihrer Halterung. Die Kamera bewegt sich in die spätere Standardansicht bei der Station. Die Entriegelung ist weiterhin ein Inszenierungsentwurf.

**Einblendung:** `ERSTES ZIEL: EINE WASSERQUELLE FINDEN`

**Sprechertext:**

> Zuerst brauchen wir eine verlässliche Wasserquelle.
>
> Wir beginnen mit den nahen Asteroiden.
>
> Ein Schritt nach dem anderen …
>
> Ich bin hier.

**Regie:** Eine kleine Pause vor dem letzten Satz. Warm und schlicht, ohne Pathos. Anschließend einige Sekunden Raum lassen, bevor die Bedienoberfläche erscheint.

## Übergang ins Spiel — Entwurf

Das Bild geht ohne Schnitt in die steuerbare Außenansicht über. Im aktuellen Wasserprototyp sind erste Eisquellen schon bekannt: Der Spieler wählt eine Quelle und weist Drohne 01 den Tankauftrag zu. Ein eigener erster Scan bleibt ein späterer Entwicklungsschritt. Im älteren Code wartet Drohne 02 noch auf Ladung und Treibstoffversorgung; diese Abweichung zur Spielidee wurde durch die Audioarbeit nicht verändert.

## Erste Umsetzung im Prototyp

- Das Intro startet bei jedem Spielstart automatisch und endet nach dem letzten Sprecherabschnitt.
- **Esc** überspringt es jederzeit, stoppt die Stimme und stellt die normale Spielansicht her. Erst ein erneuter Tastendruck im Spiel kann das Windows-Spiel schließen.
- Kamerasteuerung, Spielanzeigen und Simulation bleiben während des Intros gesperrt.
- Die aktive Sprachfassung verwendet die unveränderte Nutzeraufnahme **Maya – Supportive Agent**, ElevenLabs, als `Assets/SpaceMiner/Resources/Intro/mira_maya_preview.mp3`. Die langsamere Einstellung wurde vom Nutzer beschrieben; die vollständigen Anbieterparameter wurden nicht separat exportiert. Dateiname des Originals: `ElevenLabs_2026-10-06T21_00_12_Maya - Supportive Agent_pvc_sp100_s61_sb91_v4.mp3`.
- `intro.json` verweist über `Track` auf die durchgehende Aufnahme. 17 Untertitel mit `StartSeconds` folgen der tatsächlichen AudioSource-Position. Menüpause, Esc und Wiedergabeneustart bleiben unterstützt. Alte Hedda-WAVs bleiben als historische Testassets erhalten; `tools/GenerateIntro.ps1` schützt jetzt die importierte Aufnahme samt Zeitmarken vor Überschreiben.
- Zeitmarken wurden lokal mit dem vorhandenen FFmpeg-Whisper-Filter (öffentliches mehrsprachiges Base-Modell) und Pausenerkennung (`silencedetect`, −35 dB, Mindestpause 0,12 s) ermittelt. Automatische Transkriptionsfehler wie „Lasst ihr Zeit“ wurden gegen den vorliegenden Sprechertext korrigiert. Untertiteldatei: [01_Intro_Maya.srt](01_Intro_Maya.srt). Zeitmarken sind aus der Aufnahme abgeleitete Schätzungen; der Nutzer beurteilt die Feinabstimmung beim Anhören.
- MP3-Kopie und Original haben denselben SHA256: `5A7C060282DBB7CB44A3AAA4841A5E30C184EB9460B3AA8CC6ED92513152B399`. Die Aufnahme ist eine Hör-/Integrationsprobe; kommerzielle Freigabe wurde nicht bestätigt.
- Schwarzer Auftakt, Texteinblendungen mit Überblendungen sowie einfache Kamerafahrten bei der Station und im Gürtel sind umgesetzt. Funken, Lüftergeräusche und eine animierte Entriegelung der Drohne gehören noch zur geplanten Inszenierung.
- Drohne 02 ist funktionsfähig, wartet aber ohne Energie und Treibstoff. Ihr Aufladen und Freischalten sowie ein vollständiges Notstrom-/Schiffsbatteriesystem sind spätere Spielschritte.

## Prüfung der Maya-Integration am 06.10.2026

Separate Desktop-Vorschau mit Unity 6000.6.4f1, gemeinsame Projektversion 6000.4.7f1 unverändert. Development-/Release-Builds und Settingsvalidierung erfolgreich; vollständiger Spieltest mit Prozess-Exit 0 und 823 Prüfungen bestanden, einschließlich lokaler Maya-Aufnahme, Zeitpositionswechsel, Esc, Neustart und automatischem Übergang. Intro-Bilder für Vorstellung und Stationsstatus visuell geprüft. Logs: `Builds/LocalPreview-6000.6.4f1/Logs/mira-maya-build.log`, `mira-maya-check.log`, `smoke-test-result.json`. Der erste Sandbox-Build scheiterte an der Lizenzinitialisierung; der erfolgreiche Build lief außerhalb der Sandbox. Hörbeurteilung und finale Freigabe der Untertitelfeinabstimmung erfolgen durch den Nutzer. Aufnahmeschutz des alten Generators geprüft. Neue Version nach `Builds/Windows` übernommen, vorherige Version separat gesichert.

## Noch abzustimmen

- Wie viel weiß er über die Ursache der Explosion? Dieses Intro nennt bewusst keine Ursache.
- Die funktionierende Lebenserhaltung und die Darstellung mit Funken sind erzählerische Annahmen dieses Entwurfs; genaue Schäden und Restvorräte sind noch offen.
- Welche Quelle den Notstrom liefert und wie die vorhandenen Solarflächen und der kleine Kernreaktor anfangs nutzbar sind, ist noch offen. Ladezeiten, Ladeprioritäten und die Reichweite der ersten Drohne müssen abgestimmt werden. Die Aussage zur Versorgung einer Drohne legt keine feste Zahl möglicher Flüge fest.
- Sind alte Aufzeichnungen eine Form der Wissensartefakte? Die Formulierung ist ein Vorschlag.
