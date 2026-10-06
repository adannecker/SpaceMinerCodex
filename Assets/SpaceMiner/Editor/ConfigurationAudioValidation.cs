using System;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace SpaceMiner.Editor
{
    public static class ConfigurationAudioValidation
    {
        // Build the current scene as-is, without Setup regenerating shared assets.
        public static void Build()
        {
            SettingsValidation.BuildChecks();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/SpaceMiner/Scenes/AsteroidBelt.unity" },
                locationPathName = "Builds/Windows/SpaceMiner.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Configuration audio Windows build failed");
        }
    }
}
