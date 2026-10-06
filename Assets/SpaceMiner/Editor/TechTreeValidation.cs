using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace SpaceMiner.Editor
{
    public static class TechTreeValidation
    {
        [MenuItem("Space Miner/Techtree prüfen und bauen")]
        public static void Build()
        {
            Run();
            foreach(bool development in new[]{true,false}) {
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes=new[]{"Assets/SpaceMiner/Scenes/AsteroidBelt.unity"},
                    locationPathName=development?"Builds/Windows/SpaceMiner.exe":"Builds/TechTreeRelease/SpaceMiner.exe",
                    target=BuildTarget.StandaloneWindows64,
                    options=development?BuildOptions.Development:BuildOptions.None
                });
                if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Techtree build failed");
            }
            Debug.Log("TECHTREE DEVELOPMENT AND RELEASE BUILDS PASSED");
        }
        public static void Run()
        {
            var ids=new HashSet<string>();
            foreach(var n in TechnologyCatalog.TierOne)Require(ids.Add(n.Id),"duplicate node id");
            var active=new HashSet<int>();var done=new HashSet<int>();
            for(int i=0;i<TechnologyCatalog.TierOne.Length;i++)Visit(i,active,done);
            Rect field=new Rect(0,0,1178,350);Rect[] rects=TechnologyLayout.Nodes(field);var edges=TechnologyLayout.Edges(field,rects);
            var outputs=new HashSet<string>();var inputs=new HashSet<string>();
            foreach(var edge in edges) {
                Require(edge.Source>=0&&edge.Target>=0,"missing node");
                var points=edge.Points;
                foreach(var p in points)Require(!float.IsNaN(p.x)&&!float.IsNaN(p.y)&&field.Contains(p),"route outside graph");
                for(int i=1;i<points.Length;i++)Require(Mathf.Abs(points[i].x-points[i-1].x)<.1f||Mathf.Abs(points[i].y-points[i-1].y)<.1f,"diagonal wire");
                Require(outputs.Add(edge.Source+":"+points[0].y),"shared output dock");
                Require(inputs.Add(edge.Target+":"+points[points.Length-1].y),"shared input dock");
                if(Mathf.Abs(rects[edge.Source].center.y-rects[edge.Target].center.y)<1)Require(points.Length==2,"same-level route has unnecessary elbows");
            }
            Require(edges.FindAll(e=>e.Source==0).Count==6,"station needs six output docks");
            Debug.Log("TECHTREE DATA AND ROUTING CHECKS PASSED: "+rects.Length+" nodes, "+edges.Count+" edges");
        }
        private static void Visit(int index,HashSet<int> active,HashSet<int> done)
        {
            if(done.Contains(index))return;Require(active.Add(index),"cycle in research graph");
            foreach(string parent in TechnologyCatalog.TierOne[index].Parents){int p=TechnologyCatalog.IndexOf(parent);Require(p>=0,"unknown prerequisite");Visit(p,active,done);}
            active.Remove(index);done.Add(index);
        }
        private static void Require(bool value,string message){if(!value)throw new Exception("Techtree validation: "+message);}
    }
}
