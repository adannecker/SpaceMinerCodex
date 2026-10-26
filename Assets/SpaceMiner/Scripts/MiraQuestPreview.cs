using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    // Real campaign with a separate save/settings namespace; no injected resources.
    public sealed class MiraQuestPreview : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-miraQuestPreview") >= 0)
                new GameObject("Mira quest game preview").AddComponent<MiraQuestPreview>();
        }
        private IEnumerator Start()
        {
            yield return null;
            FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride = Path.GetFullPath("Logs/MiraQuestPreview/Saves");
            CinematicLibrary.PreferencePrefix = "SpaceMiner.MiraQuestPreview.";
            var scenario = FindFirstObjectByType<WaterScenario>();
            scenario.Scanner.DialogueModule.Portrait.SetMode(MiraVisualMode.HighModel);
            FindFirstObjectByType<StartMenu>().StartDemo();
            FindFirstObjectByType<IntroSequence>().Skip();
        }
    }
}
