# Mira: Questaufnahmen vom26.10.2026

22 einzelne Google-Aoede-WAVs sind im Original-ZIP `mira_quests_aoede_20261026.zip` gesichert und identisch unter `Assets/SpaceMiner/Resources/MiraQuests/` eingebaut. Sprechertexte liegen daneben als TXT; `recordings.json` enthält Text, Stimme/Modell, Sprechregie, Dauer und SHA256. Der im Studio erzeugte Bergungsabschluss besitzt eine eigene dokumentierte Stilanweisung. LINEAR16, mono,22050Hz. Keine erneute Erzeugung fertiger Clips bei der Fortsetzung.

`Komplette_Quests/` enthält neun unabhängige WAV-Hördateien: je Einführung,0,75s Pause und Abschluss aus den Originalen. Das Studio verwendet dieselben Einzelaufnahmen, mit seinen normalen Abschnittspausen. Im Spiel folgt der Erfolg erst auf den echten Questabschluss. `complete-quests.json` erfasst die Komplettdateien mit Prüfsummen.

Reproduktion: `tools/GenerateMiraQuestVoice.py` und begrenzte Fehlerfortsetzung `ResumeMiraQuestVoice.py` in autorisierter Cloud Shell; bestehende gcloud-Anmeldung, keine Schlüsseldateien. `ImportMiraQuestVoice.py` prüft ZIP-Inhalt, Textidentität, Audiohashes, Format und plausible Sprechlänge vor dem Einbau und erzeugt die Komplettdateien. `GenerateMiraVisemes.py --quests --clips ...` analysiert nur die neuen Aufnahmen lokal mit Rhubarb. Das Spiel ruft keinen Sprachdienst auf.

[Bedienung und Prüfung](../../../MiraAvatar.md#questgespräche-26102026).618 Assertions in acht finalen Playerläufen bestanden; keine subjektive Hörabnahme oder genaue automatische Transkription behauptet.
