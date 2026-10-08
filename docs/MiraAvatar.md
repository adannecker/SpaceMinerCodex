# Mira: interne Dialog-Vorschau

Stand: 08.10.2026. Aktuelle Nutzerentscheidung: 3D-Avatar zurückgestellt, gezeichnete Mira mit statischen Ausdrucksvarianten für gesprochene Dialoge. Der freigegebene Sketch bleibt Identitätsvorlage.

## Testbereich

Alles bleibt im gemeinsamen Unity-Projekt. `Assets/SpaceMiner/AvatarLab/MiraAvatarLab.unity` enthält nur Kamera, Avatar und Aoede-Dialogplayer. Bestehende Spielszene, IntroSequence und Startmenü werden nicht geändert. Im Editor: **Space Miner → Mira → Avatar-Testszene öffnen**, danach Play. Separater Windows-Testplayer: `Builds/MiraAvatarLab/MiraAvatarLab.exe`. Builds werden nicht mit Git übertragen.

Die Vorschau verwendet dieselben 16 lokalen WAV-Cues und Texte aus `Resources/Intro/intro.json` wie Erwachen. Start automatisch ab dem ersten Satz; Pause/Weiter, erneutes Hören, vorheriger/nächster Satz und Neustart vorhanden. Leertaste pausiert; Escape schließt den Testplayer. Lautstärke und Mundempfindlichkeit sind nur Vorschauwerte, keine gespeicherten Spielerpräferenzen.

## Avatar

Aktuelle Bildfassung: MiraDialoguePortraits.png, transparente 2×2-Bildtafel. Oben links freundlich, oben rechts besorgt, unten links konzentriert, unten rechts ermutigend. MiraAvatar.DialoguePortraits aktiviert die statische Darstellung; SetMood wechselt mit 0,45 Sekunden weicher Überblendung. Keine Mund-Patches, Blinzeln oder schwebende Bewegung in diesem Modus. Die ältere Expressions-/3D-Logik bleibt für archivierte Demos verfügbar. MiraDialoguePreview ordnet die16 Aoede-Sätze zu:0–2 freundlich,3–6 besorgt,7–13 konzentriert,14–15 ermutigend. Text und Sprachdateien unverändert. Hauptspielintegration weiterhin offen.

Neue Bildtafel mit Built-in Imagegen aus MiraSketch erzeugt. Vollständiger Prompt: Art/Mira3D/mira-dialogue-portraits-prompt.txt. Identität, wilde kurze Frisur, Kohle-/Graphitstil und cyanfarbene Konturen beibehalten; nur dezente Gesichtsausdrücke geändert. Keine neue TTS-Generierung oder externes Haarasset verwendet.

`MiraAvatar.cs` ist wiederverwendbar: eine Dialog-AudioSource an Speech binden, Textur Expressions zuweisen und Draw(Rect) aus dem Dialog-UI aufrufen. Eine Integration ins Hauptspiel steht noch aus.

Historische Expressions-Fassung: Mundöffnung folgt der geglätteten RMS-Lautstärke des tatsächlichen AudioSource-Ausgangs. Drei gezeichnete Mundstellungen, Blinzeln und minimale Kopfbewegung; keine phonetische Synchronisierung. Diese Patch-Darstellung wird in der aktuellen statischen Bilddialogvorschau nicht verwendet.

## Bildquellen und Regie

Historische Expressions-Bildquelle: Built-in Imagegen, Referenz Mira-Sketch. Original erhalten als `AvatarLab/MiraSketch.png`; Vierertextur `MiraExpressions.png`, Reihenfolge oben links Ruhe, oben rechts kleine Mundöffnung, unten links größere Mundöffnung, unten rechts Blinzeln. Aktuelle Bildquelle ist MiraDialoguePortraits.png, beschrieben oben. Kein CLI/API-Fallback.

Generierungsprompt: Edit the reference into a production 2x2 expression sprite sheet of the SAME MIRA portrait for a simple speaking game avatar. Exactly four equally sized square cells arranged in a perfectly regular 2 by 2 grid. Same friendly human female face, same graphite/charcoal sketch, same short hair, same cyan circuit contours, same slightly turned head, same framing, identical scale and exact registration of head and facial landmarks in every cell. Preserve identity. Remove the paper completely: genuinely transparent background around the head and shoulders in every cell; keep pale offwhite skin shading inside face. Top-left cell: eyes open, gentle closed-lip resting smile. Top-right: eyes open and lips slightly parted for speaking with a small dark opening. Bottom-left: eyes open, mouth naturally moderately open while speaking, dark mouth interior and a subtle upper tooth edge, no exaggerated grin. Bottom-right: same closed-lip resting smile as top-left but both eyelids naturally closed for blinking. These are animation poses; change ONLY mouth inside mouth area and eyelids inside eye area, everything else absolutely fixed. Complete head plus collar and shoulders in each cell, no cropped hair, no text or borders or padding between cells, no extra faces. Output a clean expression sheet, not an illustration of a sheet on a desk.

## Prüfen und bauen

Aktuelle Prüfung 08.10.2026: erster Unitylauf in der Sandbox abgestürzt; dort versehentlich noch die alte Playerfassung geprüft, kein Nachweis für die neuen Bilder. Wiederholter Build ausserhalb der Sandbox mit Unity6000.4.7f1 Exit0, Logs/mira-portraits-build-retry.log. Anschliessend neue sichtbare Vorschau Exit0/PASSED, Logs/mira-portraits-check-final.log:16 Sprachclips, Pause/Sprachsignal, Wechsel zu besorgt/konzentriert/ermutigend und fehlende Spielweltinstanzen. Screenshot Logs/mira-avatar-preview.png visuell geprüft. Neue Asset-.meta vorhanden, git diff --check ohne Formatfehler. Kein subjektiver Hörtest oder Hauptspielbuild; Quellen der3D-Demo erhalten.

Bei geschlossenem Unity-Editor Batchmethode `SpaceMiner.Editor.MiraAvatarLabBuild.Build` verwenden. Sie erzeugt ausschließlich die Avatar-Testszene und baut nach `Builds/MiraAvatarLab`; normale BuildSettings und Spielszene bleiben erhalten. Opt-in-Test `-miraAvatarCheck` prüft alle 16 Sprachressourcen, Mundreaktion, Ruhe bei Pause und Satzwechsel. Sichtbare Prüfung nötig: versteckte Fenster können schwarze Screenshots liefern. Logs `mira-avatar-build.log`, `mira-avatar-check.log`, Bild `Logs/mira-avatar-preview.png`. Tatsächlich ausgeführte Ergebnisse stehen im Projektmemory.
