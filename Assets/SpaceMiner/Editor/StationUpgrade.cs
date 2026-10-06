using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SpaceMiner.Editor
{
    public static class StationUpgrade
    {
        [MenuItem("Space Miner/Einfache Raumstation einsetzen und bauen")]
        public static void BuildStation()
        {
            var scene = EditorSceneManager.OpenScene("Assets/SpaceMiner/Scenes/AsteroidBelt.unity");
            var root = GameObject.Find("Stranded Ship");
            if (root == null) throw new InvalidOperationException("Bestehende Versorgungsbasis fehlt.");
            if (root.GetComponent<StationVisual>() == null) root.AddComponent<StationVisual>();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/SpaceMiner/Scenes/AsteroidBelt.unity" },
                locationPathName = "Builds/Windows/SpaceMiner.exe", target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development });
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Raumstationsbuild fehlgeschlagen.");
        }
    }
}
