using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace SpaceMiner.Editor
{
    public static class AsteroidValidation
    {
        private static int checks;

        public static void Run()
        {
            Directory.CreateDirectory("Logs");
            try
            {
                AsteroidCatalog catalog = AsteroidAssets.EnsureDefaults();
                foreach (string name in new[] { "IceRich", "Rocky", "MetalRich" })
                {
                    var type = AssetDatabase.LoadAssetAtPath<AsteroidType>(AsteroidAssets.Root+"/Types/"+name+".asset");
                    Shader shader = type.SurfaceMaterial.shader;
                    Check(shader != null && shader.isSupported,"supported asteroid shader");
                    foreach (var message in ShaderUtil.GetShaderMessages(shader))
                        Check(message.severity != ShaderCompilerMessageSeverity.Error,"shader compilation: "+message.message);
                    AsteroidMeshBuilder.Result first = AsteroidMeshBuilder.Build(type,1907);
                    AsteroidMeshBuilder.Result same = AsteroidMeshBuilder.Build(type,1907);
                    AsteroidMeshBuilder.Result other = AsteroidMeshBuilder.Build(type,1908);
                    try
                    {
                        Vector3 extent = first.Levels[0].bounds.size;
                        Check(Mathf.Abs(Mathf.Max(extent.x,Mathf.Max(extent.y,extent.z))-1)<0.00001f,"normalized 1 m largest extent");
                        Check(first.Levels[0].bounds.center.sqrMagnitude<0.000001f,"centred origin");
                        bool different = false;
                        Vector3[] a = first.Levels[0].vertices, b = same.Levels[0].vertices, c = other.Levels[0].vertices;
                        Color[] ac = first.Levels[0].colors, bc = same.Levels[0].colors;
                        for (int i = 0; i < a.Length; i++)
                        {
                            if (a[i] != b[i] || ac[i] != bc[i]) throw new Exception("Seed reproducibility failed.");
                            different |= (a[i]-c[i]).sqrMagnitude>0.00001f;
                        }
                        Check(different,"different seeds create different geometry");
                        int previous = int.MaxValue;
                        for (int level = 0; level < 4; level++)
                        {
                            Mesh mesh = first.Levels[level];
                            Check(mesh.triangles.Length<previous,"LOD triangle count decreases");
                            previous = mesh.triangles.Length;
                            ValidateTopology(mesh);
                            // Every lower LOD vertex is inherited from the highest level, including material masks.
                            Vector3[] vertices = mesh.vertices;
                            Color[] colors = mesh.colors;
                            for (int i = 0; i < vertices.Length; i++)
                                if ((vertices[i]-a[i]).sqrMagnitude>0.00000001f || colors[i] != ac[i])
                                    throw new Exception("LOD field or material masks drifted.");
                            checks++;
                        }
                        if (name=="IceRich")
                        {
                            float ice = 0;
                            foreach (Color color in ac) ice+=color.r;
                            ice/=ac.Length;
                            Check(ice>0.005f && ice<0.10f,"ice is localized beneath mostly dark crust");
                        }
                    }
                    finally { Release(first); Release(same); Release(other); }
                }
                // New definitions use exactly the same generator and may have no exposed layer.
                var custom = ScriptableObject.CreateInstance<AsteroidType>();
                custom.Surface.ExposureCount = 0;
                custom.Surface.AccentCount = 0;
                custom.Shape.LobeStrength = 0.65f;
                AsteroidMeshBuilder.Result customResult = AsteroidMeshBuilder.Build(custom,int.MinValue,3);
                try
                {
                    ValidateTopology(customResult.Levels[0]);
                    foreach (Color color in customResult.Levels[0].colors)
                        if (color.r != 0 || color.g != 0) throw new Exception("Disabled surface regions leaked.");
                    checks++;
                }
                finally { Release(customResult); UnityEngine.Object.DestroyImmediate(custom); }
                var firstType = catalog.Choose(8123);
                Check(firstType != null && catalog.Choose(8123)==firstType,"weighted catalog reproducibility");
                var generated = new GameObject("validation asteroid");
                try
                {
                    var generator = generated.AddComponent<AsteroidGenerator>();
                    generator.Type = firstType;
                    generator.DiameterMeters = 5000;
                    generator.HighestSubdivision = 3;
                    generator.Generate(false);
                    int count = generated.transform.childCount;
                    generator.Generate(false);
                    Check(generated.transform.childCount==count && count==3,"regeneration reuses LOD children");
                    Check(generated.GetComponent<MeshCollider>().sharedMesh!=null,"physical picking mesh");
                    Check(generated.GetComponent<LODGroup>().GetLODs().Length==4,"four configured rendering levels");
                    Check(generated.GetComponent<SpaceObject>().DiameterMeters==5000,"5 km metadata");
                    Physics.SyncTransforms();
                    var collider = generated.GetComponent<MeshCollider>();
                    Check(collider.Raycast(new Ray(Vector3.back*10000,Vector3.forward),out _,20000),"ray hits irregular body");
                }
                finally { UnityEngine.Object.DestroyImmediate(generated); }
                File.WriteAllText("Logs/asteroid-validation.json","{\"passed\":true,\"checks\":"+checks+"}");
                Debug.Log("SPACE MINER ASTEROID VALIDATION PASSED: "+checks+" checks");
            }
            catch (Exception error)
            {
                File.WriteAllText("Logs/asteroid-validation.json","{\"passed\":false}");
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        private static void ValidateTopology(Mesh mesh)
        {
            var edges = new Dictionary<long,int>();
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            foreach (Vector3 vertex in vertices)
                if (float.IsNaN(vertex.sqrMagnitude) || float.IsInfinity(vertex.sqrMagnitude)) throw new Exception("Non-finite vertex.");
            for (int i = 0; i < triangles.Length; i+=3)
            {
                int a=triangles[i], b=triangles[i+1], c=triangles[i+2];
                Vector3 normal = Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]);
                if (normal.sqrMagnitude<0.000000000001f) throw new Exception("Degenerate triangle.");
                AddEdge(edges,a,b); AddEdge(edges,b,c); AddEdge(edges,c,a);
            }
            foreach (int count in edges.Values) if (count!=2) throw new Exception("Mesh is not a closed two-manifold surface.");
            Check(vertices.Length-edges.Count+triangles.Length/3==2,"closed sphere topology");
        }

        private static void AddEdge(Dictionary<long,int> edges, int a, int b)
        {
            long key=((long)Mathf.Min(a,b)<<32)|(uint)Mathf.Max(a,b);
            edges.TryGetValue(key,out int count);
            edges[key]=count+1;
        }
        private static void Release(AsteroidMeshBuilder.Result result)
        { foreach (Mesh mesh in result.Levels) UnityEngine.Object.DestroyImmediate(mesh); }
        private static void Check(bool condition,string description)
        { if (!condition) throw new Exception("Asteroid check failed: "+description); checks++; }
    }
}
