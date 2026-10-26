using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceMiner.Editor
{
    // Offline vertex clustering. Original imported meshes remain intact for close views and collision.
    public static class StationLodBuild
    {
        private sealed class Cluster {public Vector3 position,normal;public Vector2 uv;public int count;}
        private static Mesh Simplify(Mesh source,int resolution,string name)
        {
            var vertices=source.vertices;var normals=source.normals;var uv=source.uv;Bounds bounds=source.bounds;
            float step=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z)/resolution;
            var cells=new Dictionary<(int,int,int,int,int),int>();var groups=new List<Cluster>();var map=new int[vertices.Length];
            for(int i=0;i<vertices.Length;i++)
            {
                var p=vertices[i]-bounds.min;var tex=uv.Length==vertices.Length?uv[i]:Vector2.zero;
                // Preserve broad atlas islands as well as spatial shape; prevents distant texture smearing.
                var key=(Mathf.FloorToInt(p.x/step),Mathf.FloorToInt(p.y/step),Mathf.FloorToInt(p.z/step),Mathf.FloorToInt(tex.x*16),Mathf.FloorToInt(tex.y*16));
                if(!cells.TryGetValue(key,out int index)){index=groups.Count;cells.Add(key,index);groups.Add(new Cluster());}
                map[i]=index;var c=groups[index];c.position+=vertices[i];c.normal+=normals.Length==vertices.Length?normals[i]:Vector3.up;c.uv+=tex;c.count++;
            }
            var result=new Mesh{name=name,indexFormat=IndexFormat.UInt32};var positions=new Vector3[groups.Count];var directions=new Vector3[groups.Count];var coords=new Vector2[groups.Count];
            for(int i=0;i<groups.Count;i++){var c=groups[i];positions[i]=c.position/c.count;directions[i]=c.normal.normalized;coords[i]=c.uv/c.count;}
            result.vertices=positions;result.normals=directions;result.uv=coords;result.subMeshCount=source.subMeshCount;
            for(int sub=0;sub<source.subMeshCount;sub++){
                var original=source.GetTriangles(sub);var triangles=new List<int>();var faces=new HashSet<(int,int,int)>();
                for(int t=0;t<original.Length;t+=3){int a=map[original[t]],b=map[original[t+1]],c=map[original[t+2]];if(a==b||b==c||a==c||Vector3.Cross(positions[b]-positions[a],positions[c]-positions[a]).sqrMagnitude<1e-16f)continue;if(!faces.Add((a,b,c)))continue;triangles.Add(a);triangles.Add(b);triangles.Add(c);}
                result.SetTriangles(triangles,sub);
            }
            result.RecalculateBounds();result.RecalculateTangents();result.Optimize();return result;
        }
        public static void Run()
        {
            const string folder="Assets/SpaceMiner/Resources/StationLOD";Directory.CreateDirectory(folder);var lines=new List<string>();
            foreach(string family in new[]{"MaintenanceDrone","ChargingCradle"})
            {
                var prefab=Resources.Load<GameObject>("Tripo/"+family);var original=prefab.GetComponentInChildren<MeshFilter>().sharedMesh;
                foreach(var pair in new[]{(resolution:100,label:"Medium"),(resolution:40,label:"Far")}){
                    var mesh=Simplify(original,pair.resolution,family+pair.label);string path=folder+"/"+mesh.name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}
                    lines.Add(family+" "+pair.label+": "+original.triangles.Length/3+" -> "+mesh.triangles.Length/3+" triangles");
                }
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/station-lod-generation.txt",lines);foreach(string line in lines)Debug.Log(line);
        }
    }
}
