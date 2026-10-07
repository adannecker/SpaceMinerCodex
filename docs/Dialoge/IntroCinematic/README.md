# Erinnerungen – vollstaendiges Intro-Cinematic

Stand: 07.10.2026. Auf Nutzerauftrag als zusammenhängendes Cinematic eingebaut.

Startmenü → Cinematics → Cinematics → **Erinnerungen · Teile 1–12**. Sofort verfügbar, ohne vorherigen Storydurchlauf. Nach Ende oder Escape zurück in die Galerie. Mira/Aoede bleibt eine eigene Sequenz.

Die drei vom Nutzer gelieferten Kohlezeichnungen und vorhandenen deutschen Enceladus-Aufnahmen liegen in `Assets/SpaceMiner/Resources/MemoryCinematic`. Originalaufnahmen hier bleiben erhalten. `MemoryCinematic` präsentiert gekrümmte, leicht geneigte Blätter mit ausgefranster Alphakante und dunkleren Gebrauchsspuren. Langsame UV-Fokusfahrten: Familie/Stadt, Sternenhimmel, Forschungsschiff. Jede Aufnahme läuft vollständig; Schwarzblenden verbinden die Szenen. Keine neuen TTS-Aufrufe.

`RuinedWorld` ergänzt zur Laufzeit die vorhandene Spielszene um Sonne/Corona, getrennte Planetenhälften, glühende Bruchflächen, Trümmer und einen halb zerstörten Mond. Grünlicher heißer Auswurf signalisiert Gefahr; glühende Partikel strömen vom Mond zum Planeten. Rein visuell: keine neue Temperatur-, Gift- oder Schadensmechanik; der Sonnenenergie-Entwurf ist weiterhin separat offen.

Prüfung: Windows-Development-Build und `-memoryCinematicCheck` (vollständiger Ablauf, drei Szenen, Rückkehr, Wiederholung/Abbruch, Weltelemente; Screenshots unter `outputs/Cinematic`). Endgültige Testergebnisse stehen im Projektmemory.

Update 07.10.2026: Galerieeintrag in Cinematics verschoben. Papier rund 4 Prozent kleiner, Neigung etwa 3 Grad, sanft geglaettete Schwarzblenden mit je 2,2 Sekunden. Satzweise Untertitel folgen den vorhandenen WAVs mit approximativen Satzzeiten und dem gemeinsamen Accessibility-Schalter; dieser gilt auch fuer Miras Cutscene. Neues Sonnensystem: zentrale Sonne, neun geneigte Bahnen, drei Ringplaneten; Planet 4 und Mond verwenden unregelmaessige Kontinentfragmente mit geschlossenen Felskanten. Der Kamera-Fernbereich wurde erweitert. Visuelle vereinfachte Umsetzung nach dem freigegebenen Konzept, keine neue Schadens- oder Solarleistungsmechanik.

Szene 4: [Eine neue Energiequelle](04_Eine_neue_Energiequelle.md), Enceladus, Gemini 3.1 Flash TTS Preview, 9,440 s. WAV und kompletter Kohlezeichnungs-Prompt gespeichert; endet bei Eine neue Energiequelle. Noch nicht in das dreiteilige Cinematic integriert und nicht subjektiv hoergeprueft.

Sonnensystem-Update: Himmelskoerper verwenden nun echte Kilometer-Abstaende und Groessen mit eigener Hintergrundkamera; siehe docs/Spielidee.md. Sonne ist die einzige externe Lichtquelle. Die bisherigen komprimierten Entfernungsangaben oben sind historisch ueberholt.

Szene 5: [Eine Zukunft ohne Grenzen](05_Eine_Zukunft_ohne_Grenzen.md), Enceladus, Gemini 3.1 Flash TTS Preview, 15,200 s. WAV technisch vollstaendig geprueft und Kohlezeichnungs-Prompt gespeichert; noch ohne Hoerfreigabe/Unity-Integration.

Szene 6: [Die Warnungen](06_Die_Warnungen.md), Enceladus, Gemini 3.1 Flash TTS Preview, 11.520 s. WAV vollstaendig technisch geprueft, Bildprompt gespeichert. Szene-5-Nutzerbild als 05_Eine_Zukunft_ohne_Grenzen.png gesichert. Noch ohne Unity-Integration.

Szene 7: [Wir irrten uns](07_Wir_irrten_uns.md), Enceladus, 9.280 s. WAV und Kohle-Bildprompt gespeichert; noch ohne Unity-Integration.

Szene 8: [Die Evakuierung](08_Die_Evakuierung.md), Enceladus, 24.800 s. WAV und Kohle-Bildprompt gespeichert; noch ohne Unity-Integration. Nutzerbild Szene 7 als 07_Wir_irrten_uns.png gesichert.

Szene 9: [Unsere Welt zerbrach](09_Unsere_Welt_zerbrach.md), Enceladus, 9.680 s. WAV und Kohle-Bildprompt gespeichert; noch ohne Unity-Integration.

Szene 10: [Die Stille](10_Die_Stille.md), Enceladus, 14.800 s. WAV und Kohle-Bildprompt gespeichert; noch ohne Unity-Integration.

Szene 11: [Am Rande des Asteroidenguertels](11_Am_Rande_des_Asteroidenguertels.md), Enceladus, 19.720 s. WAV und Kohle-Bildprompt gespeichert; noch ohne Unity-Integration.

Szene 12: [Nun liegt es an dir](12_Nun_liegt_es_an_dir.md), Enceladus, 21.200 s. WAV und Kohle-Bildprompt gespeichert; noch ohne Unity-Integration.

## Vollstaendige Integration und Video, 07.10.2026

Alle zwoelf gelieferten Kohlebilder und deutschen Enceladus-Aufnahmen sind nun integriert. Produktionshinweise weiter oben, die Szenen 4–12 als noch nicht integriert bezeichnen, sind damit ueberholt. Szene 4 aus dem gelieferten Originalbild gesichert. Galerie weiterhin sofort verfuegbar; Papierkanten, Woelbung, Neigung, sanfte Fokusbewegungen und je 2,2 Sekunden Schwarzblende bleiben erhalten. Vollstaendiger Text der aktuellen Szene wird ueber den gemeinsamen Untertitel-Schalter angezeigt; keine behaupteten Wortzeitmarken.

Video: outputs/Cinematic/SpaceMiner-IntroCinematic.mp4, 1280x720/30 fps, H.264/AAC, 205,367 s, optionale deutsche Untertitel. Unity MemoryCinematicExport rendert dieselbe Kamera, dasselbe Papier und dieselben Animationen frameweise; tools/ExportCinematicVideo.py ordnet Originalton und Pausen anhand video-timing.csv zu. SRT liegt daneben. Video ohne Spielmenue/ESC-Einblendung und ohne Zusatzmusik. Encoder projektlokal unter work/video-dependencies (imageio-ffmpeg 0.6.0).

Pruefung: Windows-Build erfolgreich (Logs/cinematic12-build.log), Unity-Videoexport aller 12 Szenen erfolgreich (Logs/cinematic12-export.log), komplette MP4-Dekodierung ohne Fehler (Logs/cinematic12-video.log), gerenderte Szenenuebersicht visuell geprueft (outputs/Cinematic/cinematic12-overview.jpg). Spielablauf-Pruefung siehe Projektmemory.

Finale Spielpruefung: Logs/cinematic12-check.log, Exit 0; alle 12 Szenen, Abschluss, Rueckkehr, Wiederholung/Abbruch, Untertitel-Schalter und Sonnensystem bestanden.

## Ueberarbeitung 07.10.2026: direkte Anschluesse

Aufnahmen 2 und 7 neu als v2 mit natuerlicher mitteltiefer Anfangsstimmlage; Aufnahme 12 neu mit fluessigem Schlusssatz, zusaetzlich 390ms reine Stille vor zu retten gekuerzt. Original-WAVs bleiben erhalten, v2-WAVs liegen hier. Im Spiel sichere stille Raender mit mindestens 120ms vor und 200ms nach erkannter Sprachaktivitaet, kurzer Anfangsschutz bei sehr fruehem Einsatz. Keine Lautstaerkerampe oder Sprachueberlappung. Alle Stimmen vorab als PCM geladen; Wechsel wartet auf tatsaechliches Audioende. SceneGap 150ms statt 2,4s. Erinnerte Bilder werden 1,2s direkt ineinander ueberblendet; keine Schwarzblende zwischen Szenen, nur kurze Film-Anfangs-/Endblende. Vorige Seite und neue Seite verwenden getrennte Bildausschnitte, Papierneigung geht weich ueber.

Aktuelles MP4: 170,267s (ca. 2:50), weiterhin 720p/30fps mit optionalen deutschen Untertiteln. Vorige Videofassung und Wiedergabe-WAVs liegen unter outputs/Cinematic/BeforeRevision. Audio-Revision und Randzeiten: audio-revision.json. Alle PCM-Aufnahmen im zusammengesetzten Ton bytegleich enthalten (Logs/cinematic-revision-audio-check.txt). Vollstaendiger Video-Decode und visueller Ueberblendungsnachweis: Logs/cinematic-revision-video.log, outputs/Cinematic/revision-dissolve.png. Build erfolgreich nach Anpassung an aktuelle Unity-SampleSettings-API (Logs/cinematic-revision-build.log). Subjektive Klangbewertung der neuen Stimmen bleibt beim Nutzer.

Finaler Spieltest nach Revision: alle 12 Szenen, Abschluss/Rueckkehr, Wiederholung/Abbruch, Untertitel, 50-Prozent-Ueberblendung mit voller Papierdeckkraft und Sonnensystem bestanden, Exit 0 (Logs/cinematic-revision-check.log).

Durchgehende Gesamtaufnahme: [Sprechertext und Einstellungen](00_Gesamter_Sprechertext.md), 00_Gesamter_Sprechertext_Enceladus.wav, 135.280 s, ein TTS-Durchlauf mit Enceladus. Inzwischen in Spiel/Video eingesetzt, siehe folgenden Stand. Szene-9-Pruefscreenshot scene-9.png auf neue Fassung video-scene-9.png aktualisiert.

## Aktueller Stand 07.10.2026: Gesamtstimme und Musik

Spiel und Video verwenden jetzt eine einzige unveraenderte Enceladus-Gesamtaufnahme, keine Einzelstimmenwechsel oder Sprachfades. Zwoelf Bildwechsel nach lokal erkannten Wortzeiten in Resources/MemoryCinematic/timing.json; 1,2s direkte Ueberblendungen bleiben erhalten. Wortzeiten dienen der Szenenzuordnung, nicht als wortgenaue Untertitel. Die neue Szene-9-Fassung mit fliehenden und explodierenden Schiffen ist enthalten.

Gelieferte Charcoal Atmosphere.wav unveraendert hier archiviert. Aufbereitete atmosphere.wav: Stereo/22050Hz, Grundpegel 7,5 Prozent des Originalsignals, waehrend Sprache weich auf etwa 4,1 Prozent abgesenkt, 3s Ein- und 5s Ausblende. Stimme im Mix mit Faktor 0,85 ohne Lautstaerkerampe. Spiel nutzt getrennte vorhandene Stimmen-/Hintergrundregler und pausiert beide Quellen gemeinsam im Einstellungsmenue. Reproduzierbare Vorbereitung: python tools/PrepareCinematicMusic.py (numpy und FFmpeg wie beim Videoexport); lokaler Abgleich: work/AlignFullNarration.py und outputs/Cinematic/full-voice-transcript.json. Erkannter Text ersetzt nicht den kanonischen Sprechertext.

Neues Video outputs/Cinematic/SpaceMiner-IntroCinematic.mp4: 135,767s, 1280x720/30fps, H.264/AAC mit optionalen deutschen Untertiteln. Gesamtmix separat als IntroCinematic-mix.wav. Vorige Videofassung als BeforeRevision/SpaceMiner-before-continuous-score.mp4 erhalten. Voller MP4-Decode ohne Fehler, Mixpeak 0,8589 ohne neue Uebersteuerung (Logs/cinematic-score-video.log). Originalmusik und Gesamtstimme per SHA256 gegen Kopien geprueft. Build und Unity-Export erfolgreich (cinematic-score-build.log, cinematic-score-export.log), kompletter Spieltest aller zwoelf Szenen, Abschluss/Rueckkehr, Wiederholung/Abbruch, Untertitel, durchgehende Stimme, volle Papierdeckkraft bei halber Ueberblendung und Sonnensystem erfolgreich, Exit 0 (cinematic-score-check.log). Szene9 und Ueberblendung visuell geprueft; keine subjektive Hoerfreigabe behauptet.

Sicherung: Originale und Revisionen, aktuelle Unity-Assets, finales MP4, Mix und SRT sind versioniert. work-Abhängigkeiten, ASR-Zwischendaten, QA-Bilder und BeforeRevision-Videos bleiben lokal; die geprüften kanonischen Szenenzeiten sind in timing.json enthalten.
