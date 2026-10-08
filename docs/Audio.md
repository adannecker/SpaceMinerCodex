# Audio: Splashscreen, Weltraum und Configuration-Menü

## Player-Audiokanäle (06.10.2026)

Master, Hintergrund/Musik, Effekte, Stimmen und UI haben gespeicherte Lautstärken und eigene An/Aus-Werte. Änderungen im Settings-Menü werden live vorgehört, Cancel stellt angewendete Werte wieder her. PlayerAudio verwaltet Vorschau und Kanalpegel; ConfigurationAudio multipliziert beide Track-Fades mit dem Hintergrundpegel, ohne die Loopposition zu ändern. Miras Quelle ist über PlayerAudioSource an Voices gebunden. SettingsUiAudio erzeugt eigene kurze technische Hover-/Aktivierungstöne, begrenzt Hover-Wiederholungen und verwendet Ui. Der Effects-Adapter ist für künftige Maschinen-/Drohnengeräusche vorgesehen; derzeit existieren solche Geräusche noch nicht. Originale und freigegebene Loops unverändert. Früherer Hinweis „kein zusätzlicher Audioregler“ ist überholt. Development/Release/Windows mit Unity 6000.4.7f1 erfolgreich gebaut. Laufzeit-Audioprüfung mit zusätzlichen Musikregler-/Mute-/Cancel-Fällen bestanden (Logs/mining-pulse-audio.log); subjektive Hörfreigabe neuer UI-Töne offen.

Stand 07.10.2026. Drei Hintergrundtracks für Splashscreen, Spiel und Settings-/Techtree-Menü. UI-Sounds und Player-Audiokanäle sind zusätzlich im aktuellen Code vorhanden; Maschinen-/Drohnen-Sounddesign und Audio-Debug-Menü bleiben spätere Aufgaben.

`ConfigurationAudio.cs` installiert die Menüquelle und die weiter unten beschriebene Gameplay-Quelle an der vorhandenen OrbitCamera. Sie beobachtet SettingsMenu.IsOpen. Öffnen startet den Menütrack mit 1,2 Sekunden Fade bis 0,35 Source-Lautstärke; Schließen blendet aus und stoppt danach. Erneutes Öffnen während des Fades verwendet dieselbe Menüquelle und setzt die Wiedergabe fort. Nach vollständigem Stop beginnt sie erneut am Trackanfang. Unskalierte Zeit lässt die Fades bei pausierter Simulation funktionieren. PlayerAudio kombiniert Master- und Kanalpegel; Settings unterstützen sofortige Vorschau, Apply und Wiederherstellung bei Cancel. Cancel/Defaults behalten die bestehende Settings-Transaktion. Keine zusätzlichen Mixer-Gruppen oder funktionslosen Regler.

## Track und Hörfreigabe

Das unveränderte Original `Deep Space Configuration.wav` liegt unter `Assets/SpaceMiner/Resources/Audio`; SHA256 `46CA7B0696827F64ED2FB2D3504F506B6CAB24FC1FFFD0FC7CABE548E8F3133B` stimmt mit der bereitgestellten lokalen Quelldatei überein. WAV bleibt Masterformat. Unity importiert den Track als Streaming/Vorbis, Stereo, ohne Normalisierung.

Die 172-Sekunden-Datei `Deep-Space-Configuration-Loop.wav` wurde am 06.10.2026 auf Nutzerauftrag lokal erstellt. `tools/CreateConfigurationLoop.py` verwendet den Originalbereich 8..188 Sekunden und überblendet dessen letzte acht Sekunden mit den ersten acht Sekunden per Sinus/Kosinus (Equal Power). Die Ausgabe beginnt mit dem Mittelteil und endet mit der Überblendung. Stereo, 48 kHz, 16 Bit PCM bleiben erhalten, ohne zusätzliche verlustbehaftete Kompression.

Die WAV samt Streaming-Import-.meta liegt unter `Assets/SpaceMiner/Resources/Audio/Deep-Space-Configuration-Loop.wav`. Die Komponente bevorzugt diesen Ressourcenpfad automatisch. Original bleibt unverändert erhalten. Technische Prüfung: 172 Sekunden, maximaler absoluter Samplewert 19310, keine übersteuerten Samples, Grenzdifferenz 86/100 Sampleeinheiten. Loop-SHA256: `0b5304ef8905172d91958cfc96b69c69cafcc514433ee062dd013bbc780514f0`. Bericht: `Logs/configuration-loop-report.json`. Hörprobe mit drei Übergängen: `Logs/configuration-loop-seams.wav`. Nutzer hat am 06.10.2026 alle drei Übergänge angehört und für gut befunden; Verwendung als Configuration-Hintergrund freigegeben.

## Technische Prüfung

`ConfigurationAudioCheck.cs` ist nur im Editor/Development enthalten und läuft bei explizitem Startargument `-configurationAudioCheck`. Es prüft Bootstrap, Track, Startzustand, Fade bei Time.timeScale=0, wiederholtes Öffnen, Mastermute, Fade-out, erneutes Öffnen während Fade, Schleifengrenze, Stop und Neustart. Es verändert keine gespeicherten Settings. Der Test bestätigt Wiedergabestatus und Laufzeitwerte, keine akustische Qualität.

Abschluss 06.10.2026 nach Abstimmung der parallelen Arbeiten: Unity 6000.4.7f1 hat SettingsDevelopment, SettingsRelease und die normale Windows-Version erfolgreich gebaut; Settings-Daten-/Speicherprüfungen bestanden. Protokoll: `Logs/configuration-audio-complete-build.log`. Audio-Laufzeittest in der finalen normalen Windows-Version mit Exitcode 0 bestanden: explizite 172-s-Loop-Datei, vor Menüöffnung stumm, Einblenden bei pausierter Zeit, wiederholtes Öffnen mit einer Menüquelle, Mastermute, Ausblenden, Wiederöffnung während Fade, Schleifengrenze, Stop und Neustart. Protokoll: `Logs/configuration-audio-complete-check.log`. Nutzer-Hörfreigabe liegt vor; kein erneuter vollständiger Spielintegrationstest dieses Abschlusslaufs.

`SpaceMiner.Editor.ConfigurationAudioValidation.Build` baut SettingsDevelopment, SettingsRelease und die normale Windows-Version aus der aktuellen Szene ohne Setup/Neugenerierung gemeinsamer Assets. Bei geschlossenem Editor und abgestimmter Build-Zuständigkeit verwenden. Danach `Builds/Windows/SpaceMiner.exe -configurationAudioCheck` ausführen.

## Allgemeine Weltraum-Ambience: Asteroid Solitude

Nutzerwahl vom 06.10.2026: `Asteroid Solitude.wav` als allgemeiner Hintergrund. Das Original aus Downloads ist unverändert unter Resources/Audio erhalten (359,72 s, Stereo, 48 kHz, 16 Bit PCM; SHA256 `7869c80f25946b70b65c5c68a9190b6ca25222a62dab60f1d3b2c9ba7354844e`). Anfang und Ende unterscheiden sich im Pegel; deshalb separate `Asteroid-Solitude-Loop.wav` erstellt. Reproduktion:

```text
python tools/CreateConfigurationLoop.py --source "Asteroid Solitude.wav" --target "Asteroid-Solitude-Loop.wav" --start 5 --end 350 --report-prefix asteroid-solitude-loop
```

Bereich 5..350 s, acht Sekunden Equal-Power-Überblendung, Loopdauer 337 s (5:37). Peak 19816, kein Clipping, Schnittdifferenz 76/-96 Sampleeinheiten. Loop-SHA256 `e16590e8923d086a27d57f3e28550c8234b288ed429d96f56ddcc84d03701acd`. Bericht Logs/asteroid-solitude-loop-report.json; Hörprobe mit drei Übergängen Logs/asteroid-solitude-loop-seams.wav. Hörfreigabe dieser neuen Schnittfassung noch offen.

Die bestehende ConfigurationAudio-Komponente verwaltet zusätzlich eine eigene Gameplay-AudioSource. Start mit Fade bis 0,25; während Miras Intro Ziel 0,10. Menüöffnung blendet Gameplay aus und pausiert es, gleichzeitig startet die Menümusik. Beim Schließen läuft die Ambience an der erhaltenen Position weiter und die Menümusik stoppt nach dem Fade. Auch bei pausierter Simulation wirksam (unskalierte Zeit). Beide Quellen sind Stereo/2D, Streaming/Vorbis und nutzen den vorhandenen Master. Dieser historische Integrationsschritt änderte keine Szene; heute sind alle Player-Audiokanäle vorhanden. Diese zwei Quellen dienen den zwei unterschiedlichen Hintergrundtracks, nicht einer Double-Source-Looptechnik.

ConfigurationAudioCheck ist um Gameplay-Track, Intro-Absenkung, Menüpause/Positionsfortsetzung, zweite Schleifengrenze und Stop beider Quellen beim Deaktivieren erweitert. Am 06.10.2026 mit Unity 6000.4.7f1 alle drei Builds (SettingsDevelopment, SettingsRelease und normale Windows-Version) erfolgreich; bestehende Settings-Validierung bestanden. Erweiterter Audio-Laufzeittest im normalen Windows-Player bestanden, Exitcode 0. Protokolle: Logs/space-ambience-build.log und Logs/space-ambience-check.log. Kein erneuter vollständiger Spielintegrationstest; Hörprüfung der neuen Loopfassung weiterhin offen.

## Splashscreen: Orbal Observation (07.10.2026)

Nutzerwahl: Orbal Observation.wav spielt im Splash-/Startmenü. Original unverändert unter Assets/SpaceMiner/Resources/Audio erhalten: 208,56 s, Stereo/48 kHz/16 Bit PCM, SHA256 defd799b425f64945ba15b4011f5fdff1565620a3646892ca9e47a4f8ac82938. Separate Orbal-Observation-Loop.wav: Bereich 5..198 s, acht Sekunden Equal-Power-Überblendung, 185 s (3:05), Peak 19231, kein Clipping, Schnittdifferenz -91/-198. Loop-SHA256 48c62deb8deff018dd8d10d1461b26a3069ee99123e4cbaaf0195c947d5a0e6f. Reproduktion mit tools/CreateConfigurationLoop.py --source "Orbal Observation.wav" --target "Orbal-Observation-Loop.wav" --start 5 --end 198 --report-prefix orbal-observation-loop. Hörprobe: Logs/orbal-observation-loop-seams.wav; subjektive Hörprüfung dieser Schnittfassung noch offen.

ConfigurationAudio verwaltet nun drei getrennte Trackquellen. Splash nutzt Orbal Observation mit 1,2-s-Fade bis 0,35. Settings oder Techtree haben Vorrang und verwenden die bisherige Configuration-Musik. Während Konfiguration im Startmenü wird Splash nach Fade pausiert und kehrt an derselben Position zurück. Demo starten blendet Splash aus und stoppt es; Gameplay-Ambience übernimmt, im Intro weiterhin leiser. Alle Hintergrundquellen berücksichtigen PlayerAudio.Background (Live-Preview, Mute und gespeicherte Werte) sowie den Master. Frühere Hinweise ohne Unterkanäle beschreiben den älteren Stand. Keine Szene, Startmenügestaltung oder Player-Speicherung geändert.

Unity 6000.4.7f1: normale Windows-Development-Version mit StartMenuBuild.Run erfolgreich gebaut (Logs/splash-audio-build.log). Gezielter SplashAudioCheck mit Exit 0 bestanden (Logs/splash-audio-check.log): eigener 185-s-Track, ausschließlich Splash-Audio vor Start, Hintergrund-Mute ohne Stop, Loopgrenze, Configuration-Wechsel/Pause, Rückkehr an erhaltener Position und Übergang ins Intro. Test schreibt keine gespeicherten Player-Settings. Kein Release-Neubuild oder vollständiger Spielintegrationstest in diesem Arbeitslauf.

## Cinematic und Bordcomputer: aktueller Stand 07.10.2026

Erwachen verwendet Aoede mit 16 unveränderten lokalen WAV-Cues. Die Maya-Aufnahme bleibt archiviert. Das zwölfteilige Erinnerungs-Cinematic verwendet eine durchgehende Enceladus-Aufnahme (135,280s), keine Einzelclip-Stimmenwechsel und keine Sprachfades. Originalmusik Charcoal Atmosphere.wav ist unter docs/Dialoge/IntroCinematic archiviert. Aufbereitete Stereo-Atmosphäre mit22050Hz: Grundpegel7,5%, unter Sprache weich auf4,125% abgesenkt,3s ein/5s aus; Stimme Faktor0,85. Vorhandene Stimmen-/Hintergrundregler bleiben getrennt; gemeinsame Menüpause. Quelle, Regie, Revisionen und Video: Dialoge/IntroCinematic/README.md. Werkzeuge: PrepareCinematicMusic.py und ExportCinematicVideo.py unter tools; NumPy und FFmpeg erforderlich.

## Scan-Questtexte (08.10.2026)

Der neue Einstieg verändert zwei Intro-Cues zum ersten Nahbereichsscan. Diese verwenden keine inhaltlich abweichenden alten WAVs. Die übrigen 14 Aoede-Cues bleiben unverändert. Neue Questdialoge für Scan, Wasserauftrag und Abschluss sind vorerst lesbare Mira-Texte; neue Aoede-Aufnahmen und ihre Hörfreigabe stehen aus. Alle 16 bisherigen WAV-Assets bleiben bewahrt.

## Vorgemerkt: Stationsinteraktionen (08.10.2026)

Unterschiedliche Sounds für Stationsaktionen ergänzen, insbesondere Schleusentüren öffnen/schliessen und VR-HUD aktivieren. Ereignisse sollen hörbar unterscheidbar sein und die vorhandene Effektlautstärke beachten. Weitere Aktionen und konkrete Soundassets noch ausarbeiten. Nur vorgemerkt, keine neuen Sounds implementiert oder angehört.
