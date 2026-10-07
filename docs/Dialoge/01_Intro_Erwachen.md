# Intro — Erwachen

## Status und Rahmen

- **Status:** Entwurf 03, zur gemeinsamen Überarbeitung. Mira ist als Name beschlossen und ihre Vorstellung ergänzt; Notstrom und die Versorgung nur einer Drohne sind eingearbeitet.
- **Auslöser:** Beginn eines neuen Spiels.
- **Sprecher:** Mira, Bordcomputer mit weiblicher Stimme.
- **Sprechweise:** sanft, warm und ruhig. Sorge bleibt hörbar, ohne dramatischen Alarmton. Natürliche Stimme mit dezenter technischer Klanggestaltung.
- **Anrede:** du.
- **Geschätzte Dauer:** etwa 90–120 Sekunden einschließlich Pausen und Bildübergängen; nach einer Probeaufnahme abstimmen.
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

## 2. Das Schiff

**Bild und Ton:** Das beschädigte Schiff wird langsam sichtbar. Schwache Positionslichter, vereinzelte Funken. Das Summen der Systeme tritt etwas deutlicher hervor.

**Einblendungen, nacheinander:**

- `SCHIFFSSTATUS: BESCHÄDIGT`
- `STROMVERSORGUNG: NOTBETRIEB`

**Sprechertext:**

> Unsere Station ist schwer beschädigt. Viele Systeme antworten nicht mehr.
>
> Wir arbeiten auf Notstrom. Die internen Stationsbatterien müssen erst wieder aufgeladen werden.
>
> Die Lebenserhaltung arbeitet noch. Aber unsere Vorräte werden nicht für immer reichen.

**Regie:** Sachlich und behutsam. Die Bestätigung zur Lebenserhaltung gibt einen kurzen Moment der Ruhe; der folgende Satz führt die Unsicherheit ein.

## 3. Was uns geblieben ist

**Bild und Ton:** Die Kamera öffnet den Blick auf den Asteroidengürtel. Danach sehen wir zwei kleine Drohnen am Schiff. Die erste aktiviert ihre Positionslichter. Die zweite bleibt in ihrer Halterung; eine Anzeige meldet, dass ihre Batterie geladen werden muss.

**Einblendungen, nacheinander:**

- `POSITION: ASTEROIDENGÜRTEL`
- `DROHNEN: 2 VON 10 FUNKTIONSFÄHIG`
- `AKTUELL EINSETZBAR: 1`

**Sprechertext:**

> Um uns herum liegt ein Asteroidengürtel.
>
> Von unseren zehn Drohnen ist nur Drohne 01 funktionsfähig. Die übrigen neun müssen wir reparieren.
>
> Treibwasser und Energie sind begrenzt. Wir müssen ihre Einsätze sorgfältig planen.
>
> Mit ihr können wir die Umgebung untersuchen und Rohstoffe bergen.
>
> Für weitere Reparaturen brauchen wir Material. Für neue Technik brauchen wir Wissen. Vielleicht finden wir etwas davon in alten Aufzeichnungen und Artefakten.

**Regie:** Etwas zuversichtlicher. Die Drohnen wirken wie eine konkrete Möglichkeit, tätig zu werden. Kein schneller Wechsel vieler Anzeigen.

## 4. Der erste Schritt

**Bild und Ton:** Eine Drohne löst sich vorsichtig von ihrer Halterung. Die Kamera bewegt sich in die spätere Standardansicht beim Schiff.

**Einblendung:** `ERSTES ZIEL: EINE WASSERQUELLE FINDEN`

**Sprechertext:**

> Zuerst brauchen wir eine verlässliche Wasserquelle.
>
> Wir sehen uns die nahen Asteroiden an.
>
> Ein Schritt nach dem anderen.
>
> Ich bin hier.

**Regie:** Eine kleine Pause vor dem letzten Satz. Warm und schlicht, ohne Pathos. Anschließend einige Sekunden Raum lassen, bevor die Bedienoberfläche erscheint.

## Übergang ins Spiel — Entwurf

Das Bild geht ohne Schnitt in die steuerbare Außenansicht über. Im aktuellen Wasserprototyp sind erste Eisquellen schon bekannt: Der Spieler wählt eine Quelle und weist Drohne 01 den Tankauftrag zu. Ein eigener erster Scan bleibt ein späterer Entwicklungsschritt. Die zweite funktionsfähige Drohne wartet auf Ladung und Treibstoffversorgung.

## Erste Umsetzung im Prototyp

- Das Intro startet bei jedem Spielstart automatisch und endet nach dem letzten Sprecherabschnitt.
- **Esc** überspringt es jederzeit, stoppt die Stimme und stellt die normale Spielansicht her. Erst ein erneuter Tastendruck im Spiel kann das Windows-Spiel schließen.
- Kamerasteuerung, Spielanzeigen und Simulation bleiben während des Intros gesperrt.
- Sprechertexte und Sprachabschnitte werden mit `tools/GenerateIntro.ps1` aus dieser Datei erzeugt. Die WAV-Dateien und das Laufzeitskript liegen in `Assets/SpaceMiner/Resources/Intro/`.
- Die erste Sprachfassung verwendet **Microsoft Hedda Desktop**, eine lokal installierte deutsche weibliche Systemstimme mit etwas verlangsamtem Tempo. Sie dient als Teststimme; die gewünschte natürliche, sanfte Endfassung kann später die einzelnen WAV-Dateien ersetzen.
- Schwarzer Auftakt, Texteinblendungen mit Überblendungen sowie einfache Kamerafahrten beim Schiff und im Gürtel sind umgesetzt. Funken, Lüftergeräusche und eine animierte Entriegelung der Drohne gehören noch zur geplanten Inszenierung.
- Drohne 02 ist funktionsfähig, wartet aber ohne Energie und Treibstoff. Ihr Aufladen und Freischalten sowie ein vollständiges Notstrom-/Schiffsbatteriesystem sind spätere Spielschritte.

## Noch abzustimmen

- Wie viel weiß er über die Ursache der Explosion? Dieses Intro nennt bewusst keine Ursache.
- Die funktionierende Lebenserhaltung und die Darstellung mit Funken sind erzählerische Annahmen dieses Entwurfs; genaue Schäden und Restvorräte sind noch offen.
- Welche Quelle den Notstrom liefert und wie die vorhandenen Solarflächen und der kleine Kernreaktor anfangs nutzbar sind, ist noch offen. Ladezeiten, Ladeprioritäten und die Reichweite der ersten Drohne müssen abgestimmt werden. Die Aussage zur Versorgung einer Drohne legt keine feste Zahl möglicher Flüge fest.
- Sind alte Aufzeichnungen eine Form der Wissensartefakte? Die Formulierung ist ein Vorschlag.


## Aktuelle Sprachfassung vom 07.10.2026

Aoede (Gemini 3.1 Flash TTS Preview) wurde vom Nutzer für Mira freigegeben. Die 79,920-s-Gesamtaufnahme ist an gemessenen Sprechpausen auf die 16 vorhandenen Laufzeitabschnitte verteilt; Untertitel in intro.json auf Station und nur Drohne 01 abgeglichen. Alte Hedda-WAVs sind unter Sprachproben/Hedda_Original gesichert. Die obigen Angaben zur zweiten funktionsfähigen Drohne, Schiffsdarstellung und Windows-Teststimme beschreiben den historischen Stand und sind überholt. Das Original-Aoede-Audio samt Regie liegt unter Sprachproben/Mira_Erwachen_Aoede_Test01.*. GenerateIntro.ps1 erzeugt weiterhin Hedda und darf die freigegebene Fassung nicht unbeabsichtigt ersetzen. Der Spielcode und die bestehenden Asset-Metadaten bleiben erhalten. Eine subjektive Prüfung sämtlicher Schnittstellen bleibt offen.

Prüfung 07.10.2026: Windows-Build erfolgreich; Spielintegrationstest Check erfolgreich (Logs/smoke-test-result.json). Audiogrenzen anhand gemessener Stille festgelegt; subjektive Hörprüfung sämtlicher Schnitte noch offen.
