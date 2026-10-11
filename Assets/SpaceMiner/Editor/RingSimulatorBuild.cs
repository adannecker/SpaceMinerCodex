using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SpaceMiner.Editor
{
    public static class RingSimulatorBuild
    {
        public static void Build()
        {
            string original=PlayerSettings.productName;
            try{
                PlayerSettings.productName="SpaceMiner Ringbau Test";
                BuildPlayer("Builds/RingSimulator/SpaceMiner.exe",true,true);
                PlayerSettings.productName=original;
                BuildPlayer("Builds/RingSimulatorGame/SpaceMiner.exe",true,false);
                BuildPlayer("Builds/RingSimulatorRelease/SpaceMiner.exe",false,false);
                Debug.Log("RING SIMULATOR DEMO, GAME AND RELEASE BUILDS PASSED");
            }finally{PlayerSettings.productName=original;}
        }
        private static void BuildPlayer(string path,bool development,bool demo)
        {
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=new[]{"Assets/SpaceMiner/Scenes/AsteroidBelt.unity"},locationPathName=path,
                target=BuildTarget.StandaloneWindows64,options=development?BuildOptions.Development:BuildOptions.None,
                extraScriptingDefines=demo?new[]{"RING_SIMULATOR_DEMO"}:Array.Empty<string>()});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Ring simulator build failed: "+path);
        }
    }
}
