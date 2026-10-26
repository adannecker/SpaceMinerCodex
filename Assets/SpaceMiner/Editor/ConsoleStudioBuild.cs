using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceMiner.Editor
{
    public static class ConsoleStudioBuild
    {
        private const string ScenePath = "Assets/SpaceMiner/ConsoleLab/ConsoleStudio.unity";
        public static void BuildAll() { Build(); BuildRegression(); }
        [MenuItem("Space Miner/Konsolenstudio öffnen")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CreateScene();
        }
        private static void CreateScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)); AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Studio background").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.cullingMask = 0;
            camera.backgroundColor = new Color(.008f, .018f, .03f);
            new GameObject("Console studio").AddComponent<ConsoleStudio>();
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        public static void Build()
        {
            if (Application.unityVersion != "6000.6.5f1") throw new InvalidOperationException("Use project Unity 6000.6.5f1");
            CreateScene();
            string product = PlayerSettings.productName;
            try
            {
                PlayerSettings.productName = "SpaceMiner Konsolenstudio";
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { ScenePath }, locationPathName = "Builds/ConsoleStudio/ConsoleStudio.exe",
                    target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
                });
                if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Console studio build failed");
            }
            finally { PlayerSettings.productName = product; AssetDatabase.SaveAssets(); }
        }
        public static void BuildRegression()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/SpaceMiner/Scenes/AsteroidBelt.unity" },
                locationPathName = "Builds/ConsoleStudioGameCheck/SpaceMiner.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Console game regression build failed");
        }
    }
}
