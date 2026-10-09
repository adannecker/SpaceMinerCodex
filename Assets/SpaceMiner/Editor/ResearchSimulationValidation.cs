using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace SpaceMiner.Editor
{
    public static class ResearchSimulationValidation
    {
        public static void Run()
        {
            SettingsValidation.Run();
            foreach(var size in new[]{new Vector2(1440,900),new Vector2(1600,1000),new Vector2(2560,1440)}){
                var bounds=MiraVisorOverlay.Bounds(size.x,size.y);var overlay=MiraVisorOverlay.Arrange(bounds);
                Require(bounds.x>=0&&bounds.y>=0&&bounds.xMax<=size.x&&bounds.yMax<=size.y,"visor stays on screen");
                Require(overlay.Portrait.xMax<overlay.Body.x&&overlay.Body.width>0&&overlay.Body.height>0,"portrait left of readable text");
                Require(bounds.Contains(overlay.Footer.center)&&bounds.Contains(overlay.Portrait.center),"visor content inside frame");
            }
            Require(Resources.Load<Texture2D>("Mira/MiraDialoguePortraits")!=null,"shared Mira portrait available");
            ConfigureMiniatures();
            using(var pictures=new ResearchMiniatures())Require(pictures.Count==159,"159 realistic miniatures");
            var catalog=JsonUtility.FromJson<ResearchCatalog>(Resources.Load<TextAsset>("Research/catalog").text);
            var sim=new ResearchSimulation(catalog);
            Require(catalog.trees.Length==11&&sim.Definitions.Count==159,"complete catalog");
            var active=new HashSet<string>();var done=new HashSet<string>();
            foreach(var id in sim.Definitions.Keys)Visit(id,sim,active,done);
            Require(sim.State("mira-01").level==1,"start research known");
            Require(sim.State("drohnen-01").level==1,"mining known");
            string root=sim.Definitions.Values.First(n=>!n.known&&n.parents.Length==0).id;
            Require(!sim.Enqueue(root),"evidence gate");
            sim.State(root).evidence=true;Require(!sim.Enqueue(root),"hardware gate");
            sim.State(root).hardware=true;Require(sim.Enqueue(root),"ready enqueue");
            Require(!sim.Enqueue(root),"duplicate queue");
            sim.Speed=0;sim.Advance(100);Require(sim.State(root).work==0,"pause");
            sim.Speed=1;sim.Powered=false;sim.Advance(100);Require(sim.State(root).work==0,"power gate");
            sim.Powered=true;sim.Advance(10);Require(sim.State(root).work==10,"real progress");
            sim.Advance(10000);Require(sim.State(root).level==1,"completion");
            string exported=sim.Export();var loaded=new ResearchSimulation(catalog);loaded.Import(exported);
            Require(loaded.State(root).level==1&&loaded.Completions==sim.Completions&&loaded.Speed==0,"roundtrip paused");
            string before=loaded.Export();
            try{loaded.Import("{\"version\":99}");throw new Exception("bad import accepted");}catch(Exception){Require(loaded.Export()==before,"invalid import atomic");}
            // Traverse all nodes using the real gates; deadlock indicates broken prerequisites.
            sim.Reset();int rounds=0;
            while(sim.Definitions.Keys.Any(id=>sim.State(id).level==0)&&rounds++<200){
                int count=sim.Completions;
                foreach(var id in sim.Definitions.Keys){sim.State(id).evidence=true;sim.State(id).hardware=true;if(sim.State(id).level==0)sim.Enqueue(id);}
                sim.Speed=600;sim.Advance(10000);
                Require(sim.Completions>count,"all branches reachable");
            }
            Require(sim.Definitions.Keys.All(id=>sim.State(id).level>0),"all research completed");
            var upgrade=sim.Definitions.Values.First(n=>n.upgrade);
            for(int i=sim.State(upgrade.id).level;i<5;i++){Require(sim.Enqueue(upgrade.id),"upgrade queued");sim.Advance(10000);}
            Require(sim.State(upgrade.id).level==5&&!sim.Enqueue(upgrade.id),"upgrade cap");
            // Split vs bulk elapsed time with parallel jobs and Mira level transitions.
            var a=new ResearchSimulation(catalog);var b=new ResearchSimulation(catalog);
            foreach(var model in new[]{a,b}){
                model.Slots=3;model.Speed=30;
                foreach(var id in model.Definitions.Keys){model.State(id).evidence=true;model.State(id).hardware=true;if(model.State(id).level==0)model.Enqueue(id);}
            }
            a.Advance(300);for(int i=0;i<3000;i++)b.Advance(.1f);
            Require(a.Completions==b.Completions&&a.Queue.SequenceEqual(b.Queue),"chunk invariant");
                        bool testedPan=false;
            // Simulation adds inferred progression gates; wires represent the approved source catalog.
            var drawingCatalog=JsonUtility.FromJson<ResearchCatalog>(Resources.Load<TextAsset>("Research/catalog").text);
            foreach(var tree in drawingCatalog.trees){
                foreach(var size in new[]{new Vector2(974,428),new Vector2(700,330)}){
                    var viewport=new Rect(30,40,size.x,size.y);
                    var graph=new ResearchTreeViewport();graph.SetTree(tree);
                    Require(graph.Edges.Count==tree.nodes.Sum(n=>n.parents.Count(p=>tree.nodes.Any(local=>local.id==p))),"all local connectors "+tree.id+" actual="+graph.Edges.Count+" expected="+tree.nodes.Sum(n=>n.parents.Count(p=>tree.nodes.Any(local=>local.id==p))));
                    foreach(var edge in graph.Edges)for(int i=1;i<edge.points.Length;i++){
                        var from=edge.points[i-1];var to=edge.points[i];
                        Require(Mathf.Approximately(from.x,to.x)||Mathf.Approximately(from.y,to.y),"orthogonal connector");
                        foreach(var rect in graph.Nodes.Values){
                            bool hits=Mathf.Approximately(from.y,to.y)?from.y>rect.yMin+.01f&&from.y<rect.yMax-.01f&&Mathf.Max(from.x,to.x)>rect.xMin+.01f&&Mathf.Min(from.x,to.x)<rect.xMax-.01f:
                                from.x>rect.xMin+.01f&&from.x<rect.xMax-.01f&&Mathf.Max(from.y,to.y)>rect.yMin+.01f&&Mathf.Min(from.y,to.y)<rect.yMax-.01f;
                            Require(!hits,"connector avoids card");
                        }
                    }
                    foreach(var id in graph.Nodes.Keys){
                        var r=graph.NodeRect(id,viewport);
                        Require(Mathf.Approximately(r.width,72)&&Mathf.Approximately(r.height,72),"uniform compact icon "+id);
                        Require(!string.IsNullOrEmpty(ResearchIconCatalog.For(id)),"semantic icon "+id);
                    }
                    string first=graph.Nodes.Keys.First(id=>viewport.Contains(graph.NodeRect(id,viewport).center));
                    var point=graph.NodeRect(first,viewport).center;
                    graph.Press(point,viewport);Require(graph.Release(point,viewport)==first,"click selection");
                    graph.SetZoom(1,viewport);
                    foreach(var id in graph.Nodes.Keys){
                        var r=graph.NodeRect(id,viewport);
                        Require(Mathf.Approximately(r.width,108),"consistent manual zoom "+id);
                    }
                    if(graph.Bounds.width*graph.Scale(viewport)>viewport.width+10){
                        var beforeY=graph.NodeRect(first,viewport).y;
                        graph.Press(viewport.center,viewport);graph.Drag(viewport.center+new Vector2(-500,180),viewport);
                        Require(graph.Release(viewport.center+new Vector2(-500,180),viewport)==null,"drag never selects");
                        Require(graph.Pan<=0&&graph.PanY<=0,"bounded two-axis pan");
                        testedPan=true;
                    }
                    graph.Reset();Require(graph.Zoom==.5f&&graph.Pan==0&&graph.PanY==0,"reset uniform 1:1");
                }
            }
            Require(testedPan,"zoom and horizontal pan exercised");
            Debug.Log("RESEARCH VIEWPORT CHECKS PASSED: 11 compact trees, uniform 72px icons, two view sizes, manual zoom, click/drag and bounded pan");
            Debug.Log("RESEARCH SIMULATION CHECKS PASSED: 159 entries, 11 trees, all reachable, gates/pause/save/upgrade/time checks");
        }
        private static void Visit(string id,ResearchSimulation sim,HashSet<string> active,HashSet<string> done)
        {
            if(done.Contains(id))return;
            Require(active.Add(id),"cycle "+id);
            foreach(var parent in sim.Definitions[id].parents){Require(sim.Definitions.ContainsKey(parent),"unknown parent");Visit(parent,sim,active,done);}
            active.Remove(id);done.Add(id);
        }
        private static void Require(bool value,string message){if(!value)throw new Exception("Research simulation: "+message);}
        private static void ConfigureMiniatures()
        {
            AssetDatabase.Refresh();
            for(int i=1;i<=3;i++){
                string path="Assets/SpaceMiner/Resources/Research/realistic-v3-"+i+".png";
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                if(importer.isReadable&&!importer.mipmapEnabled&&importer.textureCompression==TextureImporterCompression.Uncompressed)continue;
                importer.textureType=TextureImporterType.Default;importer.isReadable=true;importer.mipmapEnabled=false;
                importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
        }
        public static void BuildDemo()
        {
            Run();
            string productName=PlayerSettings.productName;
            try{
            PlayerSettings.productName="SpaceMiner TechTree Test";
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=new[]{"Assets/SpaceMiner/Scenes/AsteroidBelt.unity"},locationPathName="Builds/TechTreeDemo/SpaceMiner.exe",
                target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development,extraScriptingDefines=new[]{"TECHTREE_DEMO"}});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Techtree demo build failed");
            Debug.Log("REALISTIC TECHTREE DEMO BUILD PASSED");
            }finally{PlayerSettings.productName=productName;}
        }
        public static void Build()
        {
            Run();
            foreach(bool dev in new[]{true,false}){
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                    scenes=new[]{"Assets/SpaceMiner/Scenes/AsteroidBelt.unity"},
                    locationPathName=dev?"Builds/ResearchLaboratory/SpaceMiner.exe":"Builds/ResearchRelease/SpaceMiner.exe",
                    target=BuildTarget.StandaloneWindows64,options=dev?BuildOptions.Development:BuildOptions.None});
                if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Research build failed");
            }
            Debug.Log("RESEARCH DEVELOPMENT AND RELEASE BUILDS PASSED");
        }
        public static void BuildGameAndDemo(){Build();BuildDemo();}
    }
}
