# Tripo-Modelle für die gemeinsame Größenprobe

Beide Nutzer-ZIPs wurden am11.10.2026 geprüft und hier als lokale Original-Sicherungen abgelegt:

- `SpaceMiner_Wartungsdrohne_V1`: Modell `0532b2bc-c766-4c29-82fa-0b5784bbb02b`.
- `SpaceMiner_Ladebucht_V1`: Modell `2183ed5b-fe13-44ac-89a8-60d99b3b5e4f`.

Exportwahl: FBX, Blender, 4K Current. Die ZIPs sind Git-ignoriert; verifizierte FBX-/Texturquellen und Metadaten liegen unter `Assets/SpaceMiner/Art/Tripo`, Unity-Prefabs unter `Assets/SpaceMiner/Resources/Tripo`. Die zuvor gescheiterten Browserdownloads sind durch die vom Nutzer bereitgestellten Dateien ersetzt.

Prüfung wiederholen: `python tools/ImportTripoModels.py` (Pillow erforderlich), optional `--drone` und `--cradle` mit anderen ZIP-Pfaden. CRC, sichere Pfade, FBX-Signatur und4K-Texturen werden geprüft; abweichende vorhandene Quelldateien werden nicht überschrieben. Gemeinsame Spielprobe über `Start-Tripo-Groessenprobe.cmd`. Tatsächliche Passung, freie Stellfläche und Arme separat prüfen; keine weitere Tripo-Generierung durchgeführt.

Details: [Ringstation-Ausbau](../../docs/Ringstation-Ausbau.md).
