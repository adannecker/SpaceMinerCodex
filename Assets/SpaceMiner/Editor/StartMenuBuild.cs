using UnityEditor;
using UnityEditor.Build.Reporting;

namespace SpaceMiner.Editor
{
    public static class StartMenuBuild
    {
        public static void Run()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/SpaceMiner/Scenes/AsteroidBelt.unity" },
                locationPathName = "Builds/Windows/SpaceMiner.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new System.Exception("Start menu build failed");
        }
    }
}
