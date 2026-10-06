# Settings und Configuration

Das Menü nutzt die bestehende Unity IMGUI-Technologie, ohne zusätzliche UI-Pakete oder Szenenänderungen. SettingsMenu installiert sich beim Laden einer Spielszene mit OrbitCamera. Der Settings-Knopf ist auch ohne HUD erreichbar. F10 öffnet/schließt das Menü ausschließlich im Editor und in Development Builds; Escape verwirft Änderungen und konsumiert den Schließframe.

SpaceMinerPlayerSettings enthält getrennte Gameplay-, Graphics-, Audio-, Controls-, Interface- und Accessibility-Daten. SettingsStore validiert Werte, schreibt zunächst eine temporäre Datei und ersetzt danach player-settings.json unter Application.persistentDataPath mit Backup. Nur erfolgreich gespeicherte Werte werden angewendet. Cancel verändert keine Laufzeitwerte; Defaults bearbeitet zunächst den Entwurf. Apply lässt das Menü geöffnet.

Aktiv angebunden: Menüpause der Simulation, VSync, FPS-Limit, Unity-Qualitätsstufe, Gesamtlautstärke, Maus-/Zoomempfindlichkeit, vertikale Achse, HUD-Skalierung/Sichtbarkeit sowie Menü-Kontrast/Textgröße. Audio-Unterkanäle, Autosave, Rebinding und andere noch fehlende Spielsysteme werden nicht als scheinbar funktionsfähige Optionen angeboten. Die Menügröße passt sich dem Fenster an, Inhalte scrollen. Das Intro pausiert samt Stimme, solange das Menü offen ist.

DeveloperGameConfiguration enthält separate, nicht persistierte Live-Testwerte für Simulationsgeschwindigkeit und Drohnen-Abbaurate. Typ, Seite und F10-Eingabe sind mit UNITY_EDITOR || DEVELOPMENT_BUILD geschützt. Defaults für diese Werte erfasst die erste Öffnung im aktuellen Szenario; keine Änderung der Balancing-Assets. Release-Builds enthalten nur Player-Settings. Das vorhandene BuildWindows erzeugt weiterhin Development Builds; für Release BuildOptions.None wählen.

Erweiterungen: neue Kategorie-Daten in SpaceMinerPlayerSettings, Validierung und Laufzeitbindung ergänzen; konsumierende Spielsysteme können SettingsStore.Applied abonnieren. Balancing bleibt außerhalb der Player-Datei.

## Modulare Erweiterung (06.10.2026)

Die vorhandene Architektur wurde erweitert, ohne UI-Pakete oder ein neues Unity-Projekt einzuführen:

- `SpaceMinerPlayerSettings`: kategorisierte Player-Daten mit Validierung und kompatiblen Standardwerten für ältere JSON-Dateien.
- `PlayerSettingsFile` / `SettingsStore`: Dateiablage, Backup und Verwaltung der angewendeten Werte.
- `SettingsRuntime`: zentrale Anwendung auf Grafik, Audio, Kamera/HUD und DisplayModeController; konsumierende Systeme können weiterhin SettingsStore.Applied verwenden.
- `SettingsSession`: unabhängiger Entwurf, RestoreDefaults, Cancel und Apply; Developer-Transaktion getrennt von der Player-Datei. Menücode enthält keine Dateizugriffe und keine direkten Gameplay-Mutationen.
- `SpaceMinerUi`: wiederverwendbare Panels, Buttons, Tabs, Slider, Toggles, Dropdowns, Tooltips, Dialograhmen, Header-Stile und Statusfarben. Cyan für System/Navigation, Amber für Produktion, Rot für Fehler, Grün für Erfolg, Grau für deaktivierte Elemente. Ein gemeinsamer Stil berücksichtigt Textgröße und Kontrast; andere IMGUI-Ansichten können ihn schrittweise übernehmen.
- `SettingsMenu`: Navigation, Datenbindung und modale Eingaben auf den vorhandenen Systemen.

Neu angebunden: Kamerageschwindigkeit, Mira-Untertitel, Auflösung sowie Fenster/Randlos/Exclusive-Fullscreen. Display-Einstellungen werden im Windows-Spiel angewendet, nicht auf die Game-Ansicht des Editors. Standard `DisplayMode=-1` und Auflösung `0×0` behalten die aktuelle Startanzeige; explizite Player-Auswahl wird gespeichert. F11/Alt+Enter bleiben eine vorübergehende Umschaltung. Intro und Settings-Menü verwenden getrennte GUI-Tiefen; das Intro zeichnet hinter einem geöffneten Menü nicht weiter.

Nicht implementierte Beispiele bleiben bewusst ausstehend: Übersetzungen/Sprache, Autosave/Spielstandspeicherung, Tutorials, vollständiges Rebinding, Camera Shake sowie Music/Effects/UI-Kanäle. Dafür fehlen derzeit die zugehörigen Spiel-/Audiosysteme. Der Master-Regler ist wirksam. Developer-Balancing wird weiterhin nur während der Sitzung angewendet; keine Änderung an Asteroiden- oder Szenenassets.

Ergänzung: Die wiederverwendbaren Komponenten setzen GUI-Zustand nach modalen Ansichten zurück. Navigation und das Öffnen von Dropdowns beenden den aktuellen IMGUI-Durchlauf, damit Unity den neuen Inhalt im nächsten Layoutdurchlauf korrekt berechnet. Textgröße betrifft alle gemeinsamen Text-/Buttonstile; High Contrast verstärkt Panel und Text. Erfolgreiches Apply erscheint grün, Speicherfehler rot.

## Austauschbare Speicherung und Offline-Grundlage

PlayerSettingsService arbeitet ausschließlich mit IPlayerSettingsStorage und einer getrennten Laufzeitanwendung. LocalPlayerSettingsStorage ist der erste Adapter: JSON unter Application.persistentDataPath/player-settings.json. SettingsStore bleibt die kompatible Anlaufstelle für bestehende UI-/Audio-/Spielsysteme. Apply validiert einen unabhängigen Entwurf, speichert ihn und aktiviert ihn erst nach erfolgreichem Schreiben; Cancel schreibt nichts. Fehler werden im Menü gemeldet. Ein neuer Service beziehungsweise Spielstart lädt die gespeicherten Einstellungen automatisch. Standardwerte werden mit Restore Defaults und anschließend Apply dauerhaft gespeichert.

Ein anderer Speicheradapter kann über den Konstruktor von PlayerSettingsService eingebunden werden, ohne Menü und Spielregeln umzubauen. Der aktuelle Vertrag ist synchron für lokale Speicherung. Ein tatsächlicher Cloud-/Serveradapter benötigt zusätzlich asynchrone Aufrufe, Fehler-/Konfliktbehandlung und Authentifizierung; Netzwerkzugriffe gehören nicht blockierend in den Unity-Frame.

Für spätere Spielstände gilt dieselbe Trennung, aber mit eigenen Weltzustandsdaten und eigenem Speichervertrag. Player-Settings enthalten keine Ressourcen, Drohnenzustände oder Balancing-Daten. Die vorhandene Simulation wird in diesem Schritt nicht ersetzt: weitere Spielaufträge sollen langfristig über eine Command-Schicht laufen, Simulationsergebnisse über Zustandsdaten in die Darstellung gelangen. Server-Multiplayer benötigt zusätzlich Zuständigkeit und Synchronisation der Simulation; ein Speicherwechsel allein stellt noch keinen Multiplayer her. Noch keine Spielstandspeicherung oder Serververbindung implementiert.

SettingsValidation prüft nun auch Speichern/Neustart mit einer echten isolierten Datei, dauerhaft gespeicherte Defaults, austauschbaren Adapter sowie Lade-/Schreibfehler ohne Veränderung laufender Settings bei Schreibfehlern. Die echten Player-Dateien werden von diesen Tests nicht verändert.

## Settings-Abschluss mit Haupteinstieg

## Mining Pulse und Audio-Vorschau (06.10.2026)

Nutzerwahl: Mining Pulse mit Amber-Akzenten, Cyan-Systemkanten und horizontaler Kategorienavigation. Audio enthält Master, Hintergrund/Musik, Effekte, Stimmen/Mira und UI, jeweils mit Prozentregler und An/Aus. PlayerAudio trennt sofort hörbare Vorschau von gespeicherten Werten. Cancel/Escape verwirft die Vorschau; Apply speichert sie; Defaults stellt die Vorschau auf Standards. Die Hintergrundkanäle multiplizieren separate Fade-Hüllkurven, sodass Mute und Lautstärkeänderungen die Trackposition nicht zurücksetzen. Mira nutzt PlayerAudioSource; spätere Maschinen-/Drohneneffekte können denselben Adapter mit Effects verwenden. Noch keine Maschinen-/Drohnensounds vorhanden. SettingsUiAudio erzeugt kurze dezente technische Hover-/Aktivierungstöne lokal und verwendet den UI-Kanal. Die beiden vorhandenen Hintergrund-WAVs bleiben unverändert.

Die früheren Aussagen über fehlende Audio-Unterkanäle sind damit überholt. Unity 6000.4.7f1: Development/Release/Windows erfolgreich, Datenvalidierung samt Legacy-Audio, Live-Vorschau/Cancel und Kanal-Dateiroundtrip bestanden. Laufzeitprüfung beider Hintergrundtracks einschließlich sofortigem Kanalpegel/Mute/Cancel bestanden, ebenso 769 Release-Spielprüfungen. Menü und große Kontrastschrift visuell geprüft; anschließende reine Abstands-/Farbkorrekturen erneut erfolgreich gebaut (Logs/mining-pulse-polish-build.log). Subjektive Hörfreigabe der neuen technischen UI-Töne noch offen.

Der dezente Settings-Button liegt jetzt rechts oben oberhalb der Objektinformationen, auch bei verborgenem HUD. Der frühere Einstieg unten rechts ist entfernt. Controls bietet persistente Belegung von zehn Kameraaktionen mit Buchstaben/Ziffern/Pfeiltasten; bei Konflikten werden Tasten getauscht. Escape bricht die Erfassung ab, Systemtasten (Shift, Tab, Space, F10/F11) bleiben reserviert. PlayerInput liest nur angewendete Bindungen; Defaults stellt die ursprüngliche Belegung wieder her. Gameplay kann Steuerungshinweise ein-/ausblenden. Bestehende Auflösung-/Grafik-, Masteraudio-, UI- und Accessibility-Settings bleiben angebunden. Audioquellen und Loop-Dateien werden vom separaten Sound-System verwaltet.

06.10.2026: Development/Release und Daten-/Speichertests einschließlich Tasten-Konflikt/Legacy/Reservierung bestanden. Nach Sound-Abschluss Release-Spieltest mit 769 Prüfungen und Menü-/Cancel-Prüfung bestanden; Hauptansicht und Controls-Seite visuell geprüft. Screenshots: Logs/settings-entry.png, settings-category-3.png. Nicht vorhandene Spielsysteme (Autosave, Übersetzungen, zusätzliche Audiokanäle, Shake) bleiben separat ausstehend.

## Techtree-Einstieg (06.10.2026)

Rechts oben zeigen Settings und der benachbarte Forschungsbaum jetzt ausschliesslich Piktogramme mit Tooltips. Das Zahnrad sitzt ganz rechts, das Verzweigungs-Symbol direkt links daneben. Der Techtree teilt UI-Stil, Menü-Hintergrundaudio und modale Eingabe mit Settings; die Menüs schliessen sich gegenseitig. Player-Settings und ihre Speicherung bleiben eigenständig. Einzelheiten und Prüfungen: [Techtree](Techtree.md).
