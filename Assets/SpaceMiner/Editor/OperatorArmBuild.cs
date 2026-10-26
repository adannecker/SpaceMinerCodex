using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SpaceMiner.Editor
{
    // Rebuild existing campaign scenes; arm preparation never regenerates the station.
    public static class OperatorArmBuild
    {
        public static void Build()
        {
            string version = File.ReadLines("ProjectSettings/ProjectVersion.txt")
                .First(line => line.StartsWith("m_EditorVersion: ")).Substring(17).Trim();
            if (Application.unityVersion != version)
                throw new InvalidOperationException("Use pinned Unity " + version);
            TouchConsoleBuild.Prepare();
            OperatorArmR03Importer.Prepare();
            Player("Builds/OperatorArmDevelopment/SpaceMiner.exe", true);
            Player("Builds/OperatorArmRelease/SpaceMiner.exe", false);
        }

        private static void Player(string path, bool development)
        {
            const string scene = "Assets/SpaceMiner/Scenes/AsteroidBelt.unity";
            if (!File.Exists(scene)) throw new FileNotFoundException("Existing scene missing", scene);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { scene }, locationPathName = path,
                target = BuildTarget.StandaloneWindows64,
                options = development ? BuildOptions.Development : BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Operator arm build failed: " + path);
            Debug.Log("OPERATOR ARM BUILD SUCCESS: " + path);
        }
    }
}
