# Mira: Kopf und Schultern in 3D

Aktueller Nutzerentscheid 08.10.2026: 3D-Arbeit vorerst zurückgestellt. Dialoge werden mit gezeichneten Mira-Porträts umgesetzt, siehe MiraAvatar.md. Quellen und bisherige Demos bleiben erhalten; die folgenden Abschnitte dokumentieren den bisherigen Versuch.

Entscheidung 08.10.2026: In einen hochwertigen menschlich wirkenden 3D-Dialogavatar investieren. Mira bleibt freundlich und als dieselbe Figur erkennbar; später kann ein physischer Roboterkörper ergänzt werden. Der vorhandene Sketch ist Identitätsreferenz, kein 3D-Modell. Bestehende 2D-Testszene bleibt zum Vergleich erhalten.

## Gestaltung und Qualitätsziel

- Freundliches erwachsenes Gesicht mit sanften Augen, natürlicher Mundform, kurzem seitlich zurückgelegtem Haar und dezenten cyanfarbenen KI-Konturen nach MiraSketch.png.
- Echter modellierter Kopf, Hals und Schultern, von vorn und schräg überzeugend; keine Bildfläche mit vorgetäuschter Tiefe.
- Haut mit zurückhaltender Mikrostruktur und weichen Schatten, getrennte Augen mit Iris/Pupille und Hornhaut, saubere Haar-Silhouette. Erst menschliche Materialfassung beurteilen, danach Hologrammvariante.
- Animierbarer Kiefer, Lippen und Lider; Ruhepose, Blinzeln, dezentes Lächeln und Sprachformen. Lautstärke allein dient nur als Rückfalllösung. Kein Fertigversprechen für phonetische Synchronisierung ohne entsprechende Zeitdaten.
- Stimme und Texte bleiben die vorhandenen Aoede-Aufnahmen. Kein neuer TTS-Auftrag erforderlich.

## Modellierungsroute

Beim ersten Umgebungstest waren keine passenden Werkzeuge oder Charaktermodelle vorhanden. Die kostenlose portable Blender/MPFB-Route wurde danach ausdrücklich autorisiert und eingerichtet (siehe unten).

Vorgeschlagene kostenlose Route: portables Blender im ignorierten work-Bereich und MPFB/MakeHuman als menschliche Basis; zugeschnittenes Asset und Quelldaten danach unter AvatarLab versionieren. Die offiziellen Kernassets sind CC0; Lizenz jedes zusätzlichen Haars/Materials separat dokumentieren. Quellen: https://static.makehumancommunity.org/mpfb/docs/getting_started.html und https://static.makehumancommunity.org/mpfb/faq/use_in_closed_source.html. Code-/Add-on-Lizenzen von der Lizenz des exportierten Modells unterscheiden.

Ziel bleibt eine bearbeitbare Modellquelle, wiederverwendbare Unity-Geometrie, Materialtexturen, Gesichtsanimationen, ein eigener Kopf-Schulter-Testbereich und Quellen-/Lizenznachweis. Die unten beschriebene erste Demo dient der Beurteilung; die endgültige Mira-Gestaltung ist noch offen.

## Laufzeit und Szenentrennung

Der vorhandene AvatarLab-Player lädt ausschließlich seine kleine Szene; kein OrbitCamera oder WaterScenario. Die Planeten-/Settings-/Musik-Laufzeitinstaller setzen eine OrbitCamera voraus und erzeugen dort keine Spielwelt. Für eine spätere Hauptspielintegration genügt kein Kamerawechsel: ausgeblendete Objekte können weiterhin Update/Simulation ausführen.

Für eine reine Dialogszene entweder die Spielszene entladen (mit separat gesichertem Spielzustand) oder vollständig deaktivieren und ihr Simulationssystem ausdrücklich pausieren. Nur einen kleinen Zustandsdienst persistent halten, nicht das Weltobjekt. Bei Rückkehr denselben Stations-/Drohnen-/Ressourcenstand wiederherstellen. Noch kein Eingriff in die laufende Hauptspielszene vorgenommen.

Vor Integration nachweisen: keine aktiven Planeten-/Asteroiden-/Drohnen-Simulationen im Dialogmodus, korrektes Zurückkehren inklusive Ressourcen und Aufträgen, keine doppelten Installer, keine weiterlaufenden Hintergrund-Audioquellen. Renderkosten des Avatars anschließend im isolierten Player messen. Keine Framerate- oder Leistungszusage ohne Profiling.

## Tatsächlich eingerichtet am 08.10.2026

Portable Blender4.5.14LTS und MPFB2.0.17 im ignorierten work/AvatarTools; separate Benutzerkonfiguration, Erweiterungsrepository und Assetdaten dort. Start: tools/MiraBlender.ps1. Autorisierte Kernpakete makehuman_system_assets_cc0, faceunits01, visemes02 geladen; offizielle Blender-SHA256 geprüft. Setup-Test erzeugt19158Vertices. Menschliche Ausgangsbasis mit Game-Engine-Rig, Haut/Augen/Haaren/Zähnen/Kleidung und80ShapeKeys erstellt; docs/Art/Mira3D/Mira-AnatomicalBase.blend mit eingebetteten Texturen, PNG und base-report.json. Dies ist noch keine freigegebene Mira-Gestaltung und kein Unity-3D-Dialogavatar. Gesichtsähnlichkeit, Haare, KI-Konturen, Kopf-Schulter-Zuschnitt, Unity-Materialien und Dialoganbindung bleiben zu bearbeiten. Keine Käufe, keine globale Blender-Installation, bestehendes Spiel nicht neu gebaut. Lizenztext im Modellordner; reproduzierbarer Generator tools/CreateMiraBase.py.

## Erste Unity-3D-Demo, 08.10.2026

Assets/SpaceMiner/AvatarLab/Mira3DDemo.unity und Builds/Mira3DDemo/Mira3DDemo.exe zeigen den echten Kopf-/Schulterzuschnitt mit den 16 vorhandenen Aoede-Dialogen. Portrait mit Maus ziehen, R setzt die Drehung zurück, Leertaste pausiert, Escape schließt. Export: tools/ExportMiraDemo.py; Builder: SpaceMiner.Editor.Mira3DDemoBuild.Build. Bearbeitbare Quelle: docs/Art/Mira3D/Mira-Demo-Authoring.blend. Vier Unity-Morphs für Kiefer, Rundung und beide Lider; Lautstärke steuert die Sprachbewegung, keine phonetisch genaue Synchronisation. Gesicht und Kleidung sind weiterhin die generische MakeHuman-Basis, keine fertige Mira-Ähnlichkeit. Transparente Hornhautschale für die erste Demo ausgelassen, damit die Iris sichtbar bleibt.

Nach Rechnerabsturz Wiederherstellungsbuild mit Exit0 und -miraAvatarCheck mit Exit0 geprüft (Logs/mira-3d-recovery-build.log, Logs/mira-3d-recovery-check.log). Prüfung bestätigt 16 Clips, Sprachbewegung, Pause und fehlende WaterScenario/RuinedWorld-Instanzen; Screenshot visuell kontrolliert. Hauptspiel nicht neu gebaut oder integriert.

## Ruhendes Porträt v02 — 08.10.2026

Nach Nutzerbestätigung zuerst die Ähnlichkeit zur früheren Skizze bearbeiten, erst danach die Animation verfeinern. Neue separate Quelle: [Mira-Portrait-v02.blend](Art/Mira3D/Mira-Portrait-v02.blend). Ansichten: [Porträt](Art/Mira3D/Mira-Portrait-v02.png), [schräge Ansicht](Art/Mira3D/Mira-Portrait-v02-Side.png). Diese Bilder sind Renderings des bearbeitbaren 3D-Modells.

14 anatomische Formziele verändern Wangen, Kinn, Lippen, Nase und Augen leicht; bestehendes Gesichtsrig und Shape Keys bleiben in der Quelle. Dezentes asymmetrisches Lächeln. CC0-Haar short03 mit verkürzter und angehobener Vordersträhne, dunklerer matterer Materialfassung und feinen geometrischen Strähnen. Eigene modellierte Schulter-/Kragengeometrie, auf die Oberflächen projizierte cyanfarbene Gesichts-/Kragenkonturen. Haut mit dezenter prozeduraler Mikrostruktur und Subsurface Scattering, weiche Flächenlichter. Die vorhandene anatomische Basis bleibt separat erhalten.

Reproduzieren: `./tools/MiraBlender.ps1 -Background -Script tools/CreateMiraPortrait.py`. Bericht: Art/Mira3D/portrait-v02-report.json. Tatsächlich geprüft: Blender4.5.14, letzter Modellierungs-/Renderlauf Exit0, zwei 1000×1100-Cycles-Renderings mit 48 Samples und Denoising; beide visuell kontrolliert. Die Darstellungen wurden nach Haar-/Kragenfehlern mehrfach korrigiert. Kein neuer Unity-Build oder Sprach-/Leistungstest für v02. Der bisherige Mira3DDemo-Player verwendet weiterhin die erste Modellfassung.

Qualitätsgrenze: erste Ähnlichkeitsrevision, noch keine freigegebene finale Mira. Haarvolumen und Strähnen wirken noch kompakter als die Skizze; Ausdruck, Augen-/Haardetails und Hautmaterial können nach Nutzerbeurteilung weiter verfeinert werden. Phonetische Lippenanimation, Unity-Materialübertragung und spätere Hauptspielintegration bleiben separate nächste Schritte.

## Nutzerkorrektur und Porträt v03 — 08.10.2026

Rückmeldung: Gesicht, Haare, Augen und Nase noch näher an die Skizze bringen; Kragen/Schultern zu massig und vom Hals gelöst. Eigene Revision [Mira-Portrait-v03.blend](Art/Mira3D/Mira-Portrait-v03.blend), [Porträt](Art/Mira3D/Mira-Portrait-v03.png) und [schräge Ansicht](Art/Mira3D/Mira-Portrait-v03-Side.png). v02 bleibt erhalten.

Formziele stärker auf ovales Gesicht, feinere Nase mit etwas mehr Länge/Tiefe, mandelförmigere Augen und leichtes Lächeln abgestimmt. Irisfarbe gedämpft graugrün, CC0-Brauen eyebrow005; seitlich gelegte short03-Haarbasis mit angehobener Vordersträhne und dezenter Kronenvariation. Stoffgeometrie direkt von der ausgewerteten Hals-/Schulteranatomie abgeleitet, sauber an den Porträtgrenzen geschnitten, leicht radial erweitert und mit 1,2 mm Stoffdicke versehen. Verdeckte Haut unter dem Outfit ausgeblendet. Damit ersetzt die körpernahe Form den früheren breiten elliptischen Kragen-/Schulterkegel. Engere Bildfassung zeigt Kopf/Schultern statt viel Brustbereich.

Reproduktion: `./tools/MiraBlender.ps1 -Background -Script tools/CreateMiraPortraitV03.py`; portrait-v03-report.json dokumentiert Parameter. Tatsächlich geprüft: letzter Blenderlauf Exit0, beide 1000×1100-Renderings mit Cycles48Samples visuell kontrolliert. Zwischenstände mit Haut-/Stoffüberschneidungen wurden korrigiert; Stoffmesh ohne geerbte Normalen-/Materialdaten neu aufgebaut, schrumpfende Unterteilung entfernt. Kein Unity-Build oder animierter v03-Test; bestehende Sprachdemo verwendet weiterhin ihr ursprüngliches Modell. Ähnlichkeit bleibt zur Nutzerbeurteilung offen.

## Geöffneter V-Kragen v04 — 08.10.2026

Nach Nutzerkorrektur den Kragen als echte V-Öffnung modelliert: etwa 6,3 cm tief, mit leicht ausgestelltem oberen Rand (bis 4,5 mm Abstand). Hals und Schulterform dezent angepasst; Hals und Schlüsselbeinbereich bleiben hinter der Öffnung sichtbar. Dünne Stoffgeometrie mit dunkler Innenseite, cyanfarbene V-Kontur. Gesicht, Augen und Haare übernehmen die v03-Gestaltung.

Quelle: [Mira-Portrait-v04.blend](Art/Mira3D/Mira-Portrait-v04.blend); [Vorderansicht](Art/Mira3D/Mira-Portrait-v04.png), [schräge Ansicht](Art/Mira3D/Mira-Portrait-v04-Side.png). Generator: tools/CreateMiraPortraitV04.py; Parameterbericht: Art/Mira3D/portrait-v04-report.json. Tatsächlich geprüft: Blenderlauf Exit0, beide 1000×1100-Renderings mit 48 Samples visuell kontrolliert. V-Rand stellenweise noch kantig; weitere Oberflächenverfeinerung möglich. v03 erhalten. Noch keine Übertragung in den Unity-Player oder neue Sprach-/Animationsprüfung.

## Annäherung an die Skizze v05 — 08.10.2026

Schmalerer Kinnbereich/ovalere Kopfform, leicht stärkeres asymmetrisches Lächeln und geänderte Brauen-/Nasenform. Kragen seitlich/hinten um bis zu 13 mm zum offenen Stehkragen angehoben, V schmaler und etwas tiefer. Haaroberfläche dezent gewellt, dunkler gefasst und zwölf feine geometrische Strähnen ergänzt. Die Frisur bleibt deutlich glatter/kompakter als die lockere Skizze; keine finale Ähnlichkeit behauptet.

Quelle: [Mira-Portrait-v05.blend](Art/Mira3D/Mira-Portrait-v05.blend), [Vorderansicht](Art/Mira3D/Mira-Portrait-v05.png), [schräge Ansicht](Art/Mira3D/Mira-Portrait-v05-Side.png). Generator tools/CreateMiraPortraitV05.py, Parameterbericht Art/Mira3D/portrait-v05-report.json. Erstes Rendering zeigte Haar-/Kragenartefakte, anschließend korrigiert. Letzter Blenderlauf Exit0; beide finalen 1000×1100-Cycles-Renderings visuell kontrolliert. v04 erhalten; keine neue Unity-/Animationsprüfung oder Hauptspielintegration.

## Haare, Wimpern und Wangen v06 — 08.10.2026

Auf Nutzerwunsch Wangenknochen stärker und etwas höher geformt, Wangenvolumen leicht reduziert. CC0-Wimpern eyelashes04 an die aktuelle Gesichtsform angepasst. Zusätzliche feine gewellte Haarsträhnen mit verjüngten Spitzen über die Vorderfrisur verteilt. Eine zunächst zu auffällige Strähnenbündelung im ersten Render korrigiert; Haargrundvolumen bleibt noch deutlich kompakter als die Skizze.

Quelle: [Mira-Portrait-v06.blend](Art/Mira3D/Mira-Portrait-v06.blend), [Vorderansicht](Art/Mira3D/Mira-Portrait-v06.png), [schräge Ansicht](Art/Mira3D/Mira-Portrait-v06-Side.png). Generator tools/CreateMiraPortraitV06.py und Bericht Art/Mira3D/portrait-v06-report.json. Tatsächlich geprüft: letzter Blenderlauf Exit0 und beide finalen 1000×1100-Renderings visuell kontrolliert. v05 erhalten; keine neue Unity-/Animationsprüfung oder Hauptspielintegration.

## Verlängerte Wimpern und Haarbewegung v07 — 08.10.2026

Wimperngeometrie vor dem Lid um Faktor 1,25 verlängert. Mehr feine gewellte geometrische Haarsträhnen über die Vorderfrisur verteilt; Grundvolumen weiterhin zu geschlossen gegenüber MiraSketch. Separate Modellierungs-Bewegungsvorschau: 24 gerenderte Frames mit submillimetergrossem periodischem Schwingen der Strähnen, als GIF mit 80 ms pro Frame. Keine physikalische Haarsimulation oder Unity-Übertragung; bestehende Sprachdemo unverändert.

Quelle [Mira-Portrait-v07.blend](Art/Mira3D/Mira-Portrait-v07.blend), Standbilder Mira-Portrait-v07.png und -Side.png, [Bewegungsvorschau](Art/Mira3D/Mira-Hair-v07-Loop.gif). Reproduzierbar durch tools/CreateMiraPortraitV07.py und tools/CreateMiraHairPreview.py. Letzter Blenderlauf Exit0, beide Standbilder visuell kontrolliert; GIF-Erstellung Exit0 und 24 Frames geprüft. Die reduzierte 480×528-Vorschau verwendet 8 Cycles-Samples; keine neue Unity-/Sprach-/Leistungsprüfung. v06 und fremde Arbeit erhalten.

## Aufgelockerte Frisur und Hautstruktur v08 — 08.10.2026

Nach erneuter Bildreferenz Haargrundform mit breiteren Wellen/unregelmässigem Volumen verändert, zusätzliche freie gewellte Kronensträhnen. Erste Strähnen zu drahtig, danach verfeinert/unregelmässiger verteilt. Grundfrisur bleibt gegenüber Referenz zu geschlossen. Gesicht/Hals: stärkere feine Poren, variierende Rauheit, flache prozedurale horizontale Falten auf Halshöhe begrenzt; bewusst diskret, keine tiefen Altersfalten.

Quelle [Mira-Portrait-v08.blend](Art/Mira3D/Mira-Portrait-v08.blend), Mira-Portrait-v08.png/-Side.png; Generator tools/CreateMiraPortraitV08.py, Report portrait-v08-report.json. Erster Generatorfehler bei Rundung der Strähnenspitzen korrigiert. Letzter Blenderlauf Exit0 und beide finalen Renderings visuell geprüft. Keine neue animierte Schleife, Unity-Übertragung oder Sprachprüfung; v07 erhalten.

## Gesprochene v08-Szene in Unity — 08.10.2026

Aktuelle v08-Quelle in die bestehende isolierte Mira3DDemo übertragen. tools/ExportMiraDemo.py exportiert nun Porträt, Kragen und vereinte Kontur-/Haargeometrie mit Materialfarben; reduzierte Kurvenunterteilung ergibt etwa82.000 Vertices statt über1,5Millionen Haarvertices. Builder Mira3DDemoBuild.cs übernimmt Farben/Rauheit, MiraAvatar3D.cs ergänzt leichte Bewegung des zusammengefassten Haar-Detailobjekts. Bestehende16 Aoede-Cues des Erwachens, Mundbewegung/Blenden/Blinzeln und Dialogbedienung erhalten. Mund bleibt amplitudengesteuert, keine phonetisch genaue Synchronisation; Wimpern besitzen noch keine eigenen Lidmorphs. Unity-Materialfassung einfacher als Blender, prozedurale Hautporen/Halsfalten noch nicht in Texturen gebacken. Keine Leistungszusage ohne Profiling.

Build mit Unity6000.4.7f1 beendet mit Returncode0 (Logs/mira-v08-speech-build.log). Sichtbarer Playercheck Exit0 und MIRA AVATAR CHECK PASSED:16 Cues, Mundbewegung, Pause, fehlende Weltinstanzen (Logs/mira-v08-speech-check.log). Screenshot visuell kontrolliert und gespeichert als Art/Mira3D/Mira-Spoken-v08-Unity.png. Hauptspiel nicht neu gebaut. Player Builds/Mira3DDemo/Mira3DDemo.exe startet direkt mit erstem Dialog; Leertaste Pause, Escape schliesst.

## Unity-Darstellungsfehler korrigiert — 08.10.2026

Nutzer meldet spitze Zähne, schwarze Brauen-/Wimpernstreifen und mützenartige Frisur. ExportMiraDemo.py: untere Zahnreihe eigener JawOpen-Morph, Kronenhöhe unten leicht reduziert, Haaroberfläche aufgelockert/angehoben und Material aufgehellt. MiraAvatar3D.cs: kleinere Kieferöffnung (38 statt70 Prozent des Morphmaximums), untere Zähne folgen. Mira3DDemoBuild.cs: Alpha-Texturen unkomprimiert, Transparenzbehandlung, keine Mipmaps für Cutouts, getrennte Cutoffs, vierfaches MSAA, keine störenden Haar-/Wimpernschatten. Keine fertige realistische Frisur behauptet: Grundhaar bleibt sichtbar zu flächig; Brauen/Wimpern in kleiner Vorschau noch begrenzte Auflösung, eigene Lidmorphs weiterhin offen.

Ersten Compilerfehler bei Rendererzuweisung korrigiert. Letzter Unitybuild Exit0, sichtbarer Playercheck Exit0/PASSED, Screenshot Art/Mira3D/Mira-Spoken-MaterialFix-Unity.png visuell geprüft. Logs/mira-speech-material-fix-build.log und -check.log. Hauptspiel/fremde Änderungen bewahrt; kein Commit/Push.

## Neue Frisur, Zahnkronen und Unity-Haut v09 — 08.10.2026

Auf Nutzerauftrag komplette Frisur neu aufgebaut: alte short03-Hülle und aufgesetzte Kurven entfernt, 225 überlappende geführte Haarflächen mit eigener transparenter Strähnentextur. Erste flache Vorderpartie korrigiert, Strähnen folgen nun dem Kopf. Neuer Generator tools/RebuildMiraHairV09.py, Quelle Art/Mira3D/Mira-Portrait-v09.blend und -Side.png. Haartextur Assets/SpaceMiner/AvatarLab/Model3D/MiraHairLocks.png prozedural mit PIL erzeugt. Neue Frisur ist eine erste technische Fassung, noch nicht die lockere gewellte Referenzqualität; Spitzen und Seiten bleiben weiter verfeinerbar.

Alte Zähne durch20 kleinere abgerundete Zahnkronen mit leicht elfenbeinfarbenem Material ersetzt; untere Reihe folgt weiter JawOpen. MiraSkin.shader ergänzt im Unity-Player feine UV-basierte Poren, kleine Rauheitsunterschiede und diskrete höhenbegrenzte Halsstruktur. Nach erstem sichtbarem Test körnige Struktur abgeschwächt. Keine zusätzliche globale Blenderinstallation benötigt; portable4.5.14 wurde tatsächlich verwendet. Kein neues Unity-Projekt oder Hauptspielbuild.

Prüfung: Blender-Modelllauf/Export Exit0, Blenderansicht visuell geprüft. Letzter Unitybuild Exit0 (Logs/mira-v09-speech-build.log); letzter sichtbarer Dialogcheck Exit0/PASSED (mira-v09-speech-check.log), Screenshot Mira-Spoken-v09-Unity.png kontrolliert.16 Aoede-Cues, Mund/Pause und Szenenisolation geprüft; keine Leistungs-/phonetische Lippensynchronitätszusage. v08 erhalten, kein Commit/Push.

## Nutzerkorrektur: 3D-Avatar mit statischer Frisur v10 — 08.10.2026

Nutzer präzisiert: weiterhin 3D-Avatar, Frisur der gezeichneten Mira als unbeweglich modellierte Frisur; keine Rückkehr zum gezeichneten 2D-Avatar. Zwischenzeitlicher 2D-Build aus Missverständnis bleibt nur lokal, wurde nicht als neue aktive Lösung gestartet. Änderung am 2D-Idleverhalten wieder zurückgenommen. MiraAvatar3D.cs entfernt Haar-Detailrotation; Frisur folgt nur dem Kopf, Mund/Blinzeln bleiben aktiv.

RebuildMiraHairV10.py erstellt separate Quelle Mira-Portrait-v10.blend und -Side.png mit mehr Volumen, seitlichem Schwung und unregelmässigeren Wellen; eigene fein auslaufende Textur MiraStaticHairLocks.png. Erste Annäherung, keine exakte Übernahme/Referenzqualität behauptet; Wellen/Spitzen weiter verfeinerbar. ExportMiraDemo.py verwendet v10. Keine zweite Unity-Projektkopie, Hauptspiel unverändert.

Blender-Modell/Export Exit0, Ansicht visuell kontrolliert. Letzter Unitybuild Exit0 und sichtbarer Dialogcheck Exit0/PASSED (Logs/mira-static-hair-v10-build.log/-check.log), Screenshot Art/Mira3D/Mira-Spoken-StaticHair-v10.png visuell geprüft. Kein Commit/Push; bisherige Quellen erhalten.

## Ruhendes Porträt erneut bearbeiten, v11 — 08.10.2026

Nach Nutzerbestätigung zuerst die statische Ähnlichkeit bearbeiten, Sprachdemo vorerst unverändert. tools/SculptMiraPortraitV11.py erzeugt getrennte Quelle Mira-Portrait-v11.blend und -Side.png mit geführten Lockenkurven und feinen Haarbündeln. Zwei tatsächliche Blenderläufe Exit0; beide Entwürfe visuell geprüft. Ergebnis noch nicht brauchbar/freigegeben: aufgesetzte Strähnenform und unnatürliche Spitzen; keine Übertragung nach Unity. Ausführung technisch erfolgreich bedeutet keine gestalterische Freigabe. Nutzer fragt nach Modellbibliotheken; Assetbasis als möglicher nächster Weg, noch kein Kauf/Download/Installation. Bestehende Demo und andere Quellen erhalten.
