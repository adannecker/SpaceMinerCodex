using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SpaceMiner.Editor
{
    // Build the existing scenes; never regenerate campaign or console contents.
    public static class InterfaceBuild
    {
        public static void Build()
        {
            string version = File.ReadLines("ProjectSettings/ProjectVersion.txt")
                .First(line => line.StartsWith("m_EditorVersion: ")).Substring(17).Trim();
            if (Application.unityVersion != version) throw new InvalidOperationException("Use pinned Unity " + version);
            Player("Assets/SpaceMiner/Scenes/AsteroidBelt.unity", "Builds/InterfaceDevelopment/SpaceMiner.exe", true);
            Player("Assets/SpaceMiner/Scenes/AsteroidBelt.unity", "Builds/InterfaceRelease/SpaceMiner.exe", false);
            string name = PlayerSettings.productName;
            try
            {
                PlayerSettings.productName = "SpaceMiner Konsolenstudio";
                Player("Assets/SpaceMiner/ConsoleLab/ConsoleStudio.unity", "Builds/InterfaceStudio/ConsoleStudio.exe", true);
            }
            finally { PlayerSettings.productName = name; AssetDatabase.SaveAssets(); }
        }

        private static void Player(string scene, string path, bool development)
        {
            if (!File.Exists(scene)) throw new FileNotFoundException("Existing scene missing", scene);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { scene }, locationPathName = path, target = BuildTarget.StandaloneWindows64,
                options = development ? BuildOptions.Development : BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Interface build failed: " + path);
            Debug.Log("INTERFACE BUILD SUCCESS: " + path);
        }
    }
}
