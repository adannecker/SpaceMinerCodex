# Gemeinsame Sicherung nach lokalem Umzug –26.10.2026

Dieser Stand bündelt die seit659b514 entstandene Arbeit aller Themenchats: Unity6000.6.5f1, I75-R03 samt Spielintegration/Verkehrskorrektur, R04-Touchpulte, adaptive Innenbeleuchtung, Doppelsonnenphysik, gemeinsamer gerundeter UI-Stil und Konsolenstudio. Operator-Arm-R03 ist die ausgewählte Modellgrundlage, R04 eine gespeicherte Vorschau. Fachquellen und offene Sprintarbeit stehen im [gemeinsamen Kontext](Chats/Kontextupdate-2026-10-26.md).

Physischer Checkout auf diesem Rechner: `C:\temp\SpaceMinerCodex`. Der bisherige Source-Pfad ist eine Junction auf denselben Checkout.7044 Projekt-/Git-Dateien, HEAD, Arbeitsstand und Git-Konnektivität wurden beim tatsächlichen Umzug erhalten/geprüft; der [Nachweis](Pruefungen/Projektordner-Umzug-2026-10-26.json) ist jetzt als abgeschlossen aktualisiert.

## Prüfung in diesem Sicherungsauftrag

- Aktueller Remote-Basisstand vor Sicherung: `659b514f79878df772dbc73a1bea130b1594ea7c`; kein abweichender Remote-Commit beim Fetch.
- Alle422 C#-/Meta-Dateien des finalen UI-Builds und dessen versionierte Prüfbelege per SHA256 bestätigt. Der vorherige Sonnenbuild hat erwartete spätere Änderungen in sechs UI-Dateien; sein Nachweis bleibt historisch.
- Kein neuer Unity-Build/Playerlauf in diesem Sicherungsauftrag. Vorliegender finaler UI-Stand:535 numerische Spiel-/UIchecks,36 Studioaufnahmen sowie Settings-/Startmenüprüfungen. Historische Nachweise werden nicht als neue Abnahme ausgegeben.
- Exporter nach Ausschluss persönlicher Memory-Citations neu geprüft:25 Selbsttest-Assertions bestanden. Fünf neue sichtbare Projektfortsetzungen reviewed; registrierter Gesamtexport enthält12 erreichbare geprüfte Quellen, neun unverändert erhaltene Archive, drei Cloud-Snapshots und einen nicht verfügbaren Chat. Keine Rohsitzungen, System-/Analyse-/Toolnachrichten oder Anhangrekonstruktion.
- Neue Unity-Dateien besitzen `.meta`. Dateien und Inhalte werden vor dem Staging auf GitHub-Grössenlimits und erkennbare Zugangsschlüssel geprüft. Persönliches Memory liegt ausserhalb Git und bekommt auf Nutzerauftrag eine separate lokale Aktualisierungsnotiz.

## Übertragung und Fortsetzen

Die drei grossen Operator-Arm-Blenderdateien werden per Git LFS gespeichert, insgesamt rund0,52GiB. `.gitattributes` enthält die passende Regel. Neue Dateiversionen belegen erneut ihren vollständigen Speicher; Kostenbudgets wurden nicht geändert. [GitHub-LFS-Kontingente und Abrechnung](https://docs.github.com/en/billing/concepts/product-billing/git-lfs).

Auf dem anderen Rechner vor dem Pull lokale Änderungen sichern; dann Git LFS installieren/initialisieren und die Daten abrufen:

```powershell
git lfs install
git pull --ff-only origin main
git lfs pull
git lfs fsck
```

Unity mit der Version aus `ProjectSettings/ProjectVersion.txt` öffnen. Player müssen dort aus den vorhandenen Szenen neu gebaut werden. `Builds`, `Library`, gewöhnliche `Logs`, lokale `work`-Werkzeuge, persönliche Einstellungen/Spielstände und persönliche Codex-Sitzungen werden nicht gepusht. Der gemeinsame Arbeitsstand bleibt ein Checkout; keine alternative Unity-Kopie anlegen.

Commitidentifikation: der Git-Commit, der diesen Bericht enthält. Der tatsächliche Push wird nach dem Commit gegen `origin/main` geprüft und im Abschluss gemeldet. [Maschinenlesbarer Sicherungsnachweis](Pruefungen/Gesamtsicherung-Lokal-2026-10-26.json), [Rechnerwechsel](Rechnerwechsel-Memory.md).
