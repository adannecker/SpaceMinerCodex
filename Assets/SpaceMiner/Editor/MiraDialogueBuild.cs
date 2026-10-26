using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceMiner.Editor
{
    public static class MiraDialogueBuild
    {
        public const string ScenePath = "Assets/SpaceMiner/DialogueLab/MiraDialogueDemo.unity";

        public static void CompileCheck()
        {
            if (MiraQuestDialogues.Catalog.Quests.Length != 9) throw new InvalidOperationException("Quest catalog invalid");
            Debug.Log("MIRA QUEST COMPILATION AND CATALOG PASSED");
        }

        public static void BuildQuestPlayers()
        {
            int recordings = 0;
            foreach (var line in MiraDialogueScript.LoadAwakening().Cues) { ValidateRecording(line); recordings++; }
            foreach (var quest in MiraQuestDialogues.Catalog.Quests)
                foreach (var line in quest.Cues) { ValidateRecording(line); recordings++; }
            foreach (var line in MiraQuestDialogues.Catalog.Helpers) { ValidateRecording(line); recordings++; }
            if (recordings != 38) throw new InvalidOperationException("Expected 38 local Mira recordings");
            Build(); BuildGame();
            Debug.Log("MIRA QUEST PLAYERS BUILT: " + recordings + " verified recordings and lip profiles");
        }
        private static void ValidateRecording(MiraDialogueLine line)
        {
            var clip = Resources.Load<AudioClip>(line.Audio);
            var json = clip == null ? null : Resources.Load<TextAsset>("MiraVisemes/" + clip.name);
            if (clip == null || json == null) throw new InvalidOperationException("Missing Mira voice/timing: " + line.Audio);
            var timing = JsonUtility.FromJson<MiraFacePerformance.Timing>(json.text);
            if (timing?.mouthCues == null || Mathf.Abs(timing.duration - clip.length) > .03f)
                throw new InvalidOperationException("Mira timing/voice duration mismatch: " + line.Audio);
        }

        [MenuItem("Space Miner/Mira/Dialogstudio öffnen")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath)) CreateScene();
            else EditorSceneManager.OpenScene(ScenePath);
        }

        private static void CreateScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Dialogstudio background").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.015f, .027f, .045f);
            camera.cullingMask = 0;
            camera.gameObject.AddComponent<AudioListener>();
            new GameObject("Mira dialogstudio").AddComponent<MiraDialogueDemo>();
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        public static void Build()
        {
            MiraDialogueModelsBuild.Prepare();
            CreateScene();
            // Unity also persists engine window preferences by company/product.
            // Give the demo its own identity, then restore the game's editor setting.
            string product = PlayerSettings.productName;
            try
            {
                PlayerSettings.productName = "Mira Dialogstudio";
                BuildPlayer(ScenePath, "Builds/MiraDialogueDemo/MiraDialogueDemo.exe");
            }
            finally { PlayerSettings.productName = product; AssetDatabase.SaveAssets(); }
            Debug.Log("MIRA DIALOGUE DEMO BUILD PASSED");
        }

        public static void BuildGame()
        {
            MiraDialogueModelsBuild.Prepare();
            BuildPlayer("Assets/SpaceMiner/Scenes/AsteroidBelt.unity", "Builds/MiraDialogueGame/SpaceMiner.exe");
            Debug.Log("MIRA DIALOGUE GAME BUILD PASSED");
        }

        private static void BuildPlayer(string scene, string target)
        {
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { scene }, locationPathName = target,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
            });
            if (result.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Mira dialogue build failed: " + result.summary.result);
        }
    }
}
