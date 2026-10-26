using UnityEditor;
using UnityEditor.Build.Reporting;

namespace SpaceMiner.Editor
{
    // Existing scene only. Do not run PrototypeSetup or regenerate player-authored room plans.
    public static class PowerDashboardBuild
    {
        public static void Run() => Build("Builds/PowerDashboard/SpaceMiner.exe", BuildOptions.Development);
        public static void Release() => Build("Builds/PowerDashboardRelease/SpaceMiner.exe", BuildOptions.None);
        public static void All() { Run(); Release(); }
        private static void Build(string path, BuildOptions options)
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/SpaceMiner/Scenes/AsteroidBelt.unity" },
                locationPathName = path, target = BuildTarget.StandaloneWindows64, options = options
            });
            if (report.summary.result != BuildResult.Succeeded) throw new System.Exception("Power dashboard build failed: " + report.summary.result);
        }
    }
}
