using UnityEditor;
using UnityEditor.Build.Reporting;
namespace SpaceMiner.Editor
{
    public static class AdaptiveLightingBuild
    {
        public static void Build()
        {
            TouchConsoleBuild.Prepare();
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/SpaceMiner/Scenes/AsteroidBelt.unity"},locationPathName="Builds/AdaptiveLighting/SpaceMiner.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(result.summary.result!=BuildResult.Succeeded)throw new System.Exception("Adaptive lighting build failed");
        }
    }
}
