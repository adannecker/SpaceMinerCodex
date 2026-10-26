using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SpaceMiner.Editor
{
    // Approved authored R03; stable prefab GUID, metre scale and material batching.
    public static class IndoorDroneTrafficBuild
    {
        private const string Folder = "Assets/SpaceMiner/Resources/IndoorDroneI75";
        private const string SourceFolder = "Assets/SpaceMiner/Art/IndoorDroneI75/R03";
        [Serializable] private sealed class Surface { public string name; public float[] baseColor, emission; public float metallic, roughness; }
        [Serializable] private sealed class Manifest { public string revision; public float length, width, height; public int triangles; public Surface[] materials; }
        private static string Key(string name) => new string(name.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        private static Bounds BoundsIn(Transform root)
        {
            bool found = false; Bounds bounds = default;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var box = filter.sharedMesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var sign = new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1);
                    var p = root.InverseTransformPoint(filter.transform.TransformPoint(box.center + Vector3.Scale(box.extents, sign)));
                    if (!found) { bounds = new Bounds(p, Vector3.zero); found = true; } else bounds.Encapsulate(p);
                }
            }
            if (!found) throw new Exception("R03 import has no meshes.");
            return bounds;
        }
        private static Mesh SaveMesh(Mesh mesh, string name)
        {
            mesh.name = name;
            string path = Folder + "/R03/" + name + ".asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (old == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            EditorUtility.CopySerialized(mesh, old); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(old); return old;
        }
        private static Material SaveMaterial(Surface surface, int index)
        {
            string path = Folder + "/R03/Materials/R03_" + index.ToString("00") + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(material, path); }
            material.name = surface.name;
            material.color = new Color(surface.baseColor[0], surface.baseColor[1], surface.baseColor[2], 1).gamma;
            material.SetFloat("_Metallic", surface.metallic); material.SetFloat("_Glossiness", 1 - surface.roughness);
            material.enableInstancing = true;
            var glow = new Color(surface.emission[0], surface.emission[1], surface.emission[2]);
            if (glow.maxColorComponent > 0) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", glow.gamma); }
            else { material.DisableKeyword("_EMISSION"); material.SetColor("_EmissionColor", Color.black); }
            EditorUtility.SetDirty(material); return material;
        }
        public static void Prepare()
        {
            string pinned = File.ReadLines("ProjectSettings/ProjectVersion.txt").First(s => s.StartsWith("m_EditorVersion: ")).Substring("m_EditorVersion: ".Length).Trim();
            if (Application.unityVersion != pinned) throw new Exception("Use project-pinned Unity " + pinned + " for R03.");
            Directory.CreateDirectory(Folder + "/R03/Materials"); AssetDatabase.Refresh();
            string source = SourceFolder + "/I75_V02_Surface_R03.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(source);
            importer.isReadable = true; importer.importCameras = false; importer.importLights = false; importer.importAnimation = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; importer.SaveAndReimport();
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(SourceFolder + "/materials.json"));
            if (manifest.revision != IndoorDroneVisual.Revision || manifest.materials.Length != 9) throw new Exception("R03 material manifest mismatch.");
            var materials = manifest.materials.Select(SaveMaterial).ToArray();
            var materialMap = manifest.materials.Select((s, i) => (key: Key(s.name), index: i)).ToDictionary(x => x.key, x => x.index);
            var root = new GameObject("I75Traffic"); GameObject model = null;
            try
            {
                model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(source), root.transform, false);
                var face = model.GetComponentsInChildren<Transform>().Single(t => Key(t.name) == "blindmagneticmatingface");
                var rear = model.GetComponentsInChildren<Transform>().Single(t => Key(t.name) == "r03sealedrearequipmentcover");
                Vector3 forward = root.transform.InverseTransformPoint(face.position) - root.transform.InverseTransformPoint(rear.position); forward.y = 0;
                model.transform.localRotation = Quaternion.FromToRotation(forward.normalized, Vector3.forward) * model.transform.localRotation;
                var rotated = BoundsIn(root.transform); model.transform.localScale *= manifest.length / rotated.size.z;
                var measured = BoundsIn(root.transform); model.transform.localPosition -= new Vector3(measured.center.x, measured.min.y, measured.center.z);
                measured = BoundsIn(root.transform);
                if (Mathf.Abs(measured.size.x - manifest.width) > .003f || Mathf.Abs(measured.size.y - manifest.height) > .003f)
                    throw new Exception("R03 import axes or metre scale incorrect: " + measured);
                if (root.transform.InverseTransformPoint(face.position).z < .3f) throw new Exception("Tool coupling must face +Z.");
                var groups = new Dictionary<int, List<CombineInstance>>(); int triangles = 0;
                foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
                {
                    var renderer = filter.GetComponent<MeshRenderer>(); var sourceMaterials = renderer.sharedMaterials;
                    for (int sub = 0; sub < filter.sharedMesh.subMeshCount; sub++)
                    {
                        string key = Key(sourceMaterials[sub].name);
                        if (!materialMap.TryGetValue(key, out int index)) throw new Exception("Unmapped R03 FBX material: " + sourceMaterials[sub].name);
                        if (!groups.TryGetValue(index, out var list)) groups[index] = list = new List<CombineInstance>();
                        list.Add(new CombineInstance { mesh = filter.sharedMesh, subMeshIndex = sub, transform = root.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix });
                        triangles += filter.sharedMesh.GetTriangles(sub).Length / 3;
                    }
                }
                // This pinned FBX importer produces eight fewer triangles than Blender.
                // Keep that measured imported topology explicit rather than relaxing the check.
                if (manifest.triangles != 87564 || triangles != 87556) throw new Exception("Imported R03 triangle mismatch: " + triangles);
                var merged = new Dictionary<int, Mesh>();
                foreach (var pair in groups)
                {
                    var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
                    mesh.CombineMeshes(pair.Value.ToArray(), true, true); mesh.RecalculateBounds(); merged.Add(pair.Key, mesh);
                }
                Object.DestroyImmediate(model); model = null;
                var indicatorList = new List<Renderer>(); var lods = new List<LOD>(); var counts = new List<string>();
                Mesh highBody = null;
                for (int level = 0; level < 3; level++)
                {
                    var renderers = new List<Renderer>(); int levelTriangles = 0;
                    foreach (bool status in new[] { false, true })
                    {
                        var pieces = new List<CombineInstance>(); var surfacesForMesh = new List<Material>(); var temporary = new List<Mesh>();
                        foreach (var pair in merged.OrderBy(p => p.Key))
                        {
                            var definition = manifest.materials[pair.Key]; bool light = definition.emission.Any(v => v > 0);
                            if (light != status || level > 0 && Key(definition.name).Contains("durableivorylettering")) continue;
                            Mesh mesh = pair.Value;
                            if (level > 0 && !status) { mesh = StationLodBuild.Simplify(mesh, level == 1 ? 55 : 20, "R03 temporary LOD"); temporary.Add(mesh); }
                            pieces.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.identity }); surfacesForMesh.Add(materials[pair.Key]);
                        }
                        var combined = new Mesh { indexFormat = IndexFormat.UInt32 };
                        combined.CombineMeshes(pieces.ToArray(), false, true); combined.RecalculateBounds();
                        foreach (var temp in temporary) Object.DestroyImmediate(temp);
                        combined = SaveMesh(combined, "R03_LOD" + level + (status ? "_Indicators" : "_Body"));
                        levelTriangles += combined.triangles.Length / 3;
                        var child = new GameObject(combined.name); child.layer = 26; child.transform.SetParent(root.transform, false);
                        child.AddComponent<MeshFilter>().sharedMesh = combined;
                        var r = child.AddComponent<MeshRenderer>(); r.sharedMaterials = surfacesForMesh.ToArray(); renderers.Add(r);
                        if (status) indicatorList.Add(r); else if (level == 0) highBody = combined;
                    }
                    lods.Add(new LOD(level == 0 ? .12f : level == 1 ? .035f : .001f, renderers.ToArray()));
                    counts.Add("LOD" + level + "=" + levelTriangles + " triangles / 2 renderers");
                }
                foreach (var mesh in merged.Values) Object.DestroyImmediate(mesh);
                root.layer = 26;
                var lod = root.AddComponent<LODGroup>(); lod.SetLODs(lods.ToArray()); lod.fadeMode = LODFadeMode.None; lod.RecalculateBounds();
                var visual = root.AddComponent<IndoorDroneVisual>(); visual.modelBounds = measured; visual.indicators = indicatorList.ToArray(); visual.highBody = highBody;
                PrefabUtility.SaveAsPrefabAsset(root, Folder + "/I75Traffic.prefab");
                Directory.CreateDirectory("Logs/I75-R03-Integration");
                File.WriteAllText("Logs/I75-R03-Integration/model-import.txt", "R03 approved model; front+Z, metre scale, floor pivot; bounds=" + measured + "\nSource triangles=" + manifest.triangles + "; Unity imported=" + triangles + "\n" + string.Join("\n", counts));
                foreach (string line in counts) Debug.Log("I75 R03 " + line);
            }
            finally { if (model != null) Object.DestroyImmediate(model); Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        }
        public static void Run() { Prepare(); StartMenuBuild.Run(); }
        public static void BuildIntegration()
        {
            Prepare();
            foreach (bool release in new[] { false, true })
            {
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { "Assets/SpaceMiner/Scenes/AsteroidBelt.unity" },
                    locationPathName = "Builds/I75R03" + (release ? "Release" : "Development") + "/SpaceMiner.exe",
                    target = BuildTarget.StandaloneWindows64, options = release ? BuildOptions.None : BuildOptions.Development });
                if (report.summary.result != BuildResult.Succeeded) throw new Exception("R03 player build failed.");
            }
        }
    }
}
