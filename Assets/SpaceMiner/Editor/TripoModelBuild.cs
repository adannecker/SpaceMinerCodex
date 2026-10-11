using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SpaceMiner.Editor
{
    public static class TripoModelBuild
    {
        [Serializable] private sealed class ImportedModel
        {
            public string name, source, prefab;
            public Vector3 boundsSize, boundsCentre;
            public int meshes, triangles, vertices;
        }
        [Serializable] private sealed class ImportReport { public ImportedModel[] models; }

        public static void Prepare()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Directory.CreateDirectory("Assets/SpaceMiner/Resources/Tripo");
            var report = new ImportReport { models = new[] { PrepareModel("MaintenanceDrone"), PrepareModel("ChargingCradle") } };
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/tripo-unity-import.json", JsonUtility.ToJson(report, true));
            AssetDatabase.SaveAssets();
            Debug.Log("TRIPO IMPORT PREPARED: " + JsonUtility.ToJson(report));
        }

        private static ImportedModel PrepareModel(string name)
        {
            string directory = "Assets/SpaceMiner/Art/Tripo/" + name;
            string fbx = Directory.GetFiles(directory, "*.fbx").Single().Replace('\\', '/');
            var importer = (ModelImporter)AssetImporter.GetAtPath(fbx);
            importer.isReadable = true; // Bounds/centre-deck measurement for this explicit model probe.
            importer.importAnimation = false;
            importer.addCollider = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
            foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
            {
                var textureImporter = AssetImporter.GetAtPath(file.Replace('\\', '/')) as TextureImporter;
                if (textureImporter == null) continue;
                textureImporter.maxTextureSize = 2048;
                textureImporter.textureCompression = TextureImporterCompression.CompressedHQ;
                textureImporter.sRGBTexture = file.Contains("basecolor");
                textureImporter.textureType = file.Contains("_normal.") ? TextureImporterType.NormalMap : TextureImporterType.Default;
                textureImporter.SaveAndReimport();
            }
            Texture2D Texture(string suffix) => AssetDatabase.LoadAssetAtPath<Texture2D>(Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                .Single(p => p.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)).Replace('\\', '/'));
            string materialPath = directory + "/PreviewPBR.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null) { material = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(material, materialPath); }
            material.color = Color.white;
            material.mainTexture = Texture("_basecolor.JPEG");
            material.SetTexture("_BumpMap", Texture("_normal.PNG")); material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap", Texture("StandardMetallicSmoothness.png")); material.EnableKeyword("_METALLICGLOSSMAP");
            material.SetFloat("_GlossMapScale", .75f); material.SetFloat("_BumpScale", .4f);
            EditorUtility.SetDirty(material);
            var instance = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));
            instance.name = name;
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new Exception("Empty Tripo model: " + name);
            Bounds bounds = renderers[0].bounds; foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            var meshes = instance.GetComponentsInChildren<MeshFilter>().Select(m => m.sharedMesh).ToArray();
            if (meshes.Any(m => m == null || m.vertexCount == 0)) throw new Exception("Missing mesh: " + name);
            string prefab = "Assets/SpaceMiner/Resources/Tripo/" + name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, prefab);
            var record = new ImportedModel { name = name, source = fbx, prefab = prefab, boundsSize = bounds.size, boundsCentre = bounds.center,
                meshes = meshes.Length, triangles = meshes.Sum(m => m.triangles.Length / 3), vertices = meshes.Sum(m => m.vertexCount) };
            UnityEngine.Object.DestroyImmediate(instance);
            return record;
        }

        public static void Build()
        {
            Prepare();
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/SpaceMiner/Scenes/AsteroidBelt.unity" },
                locationPathName = "Builds/TripoModels/SpaceMiner.exe", target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development });
            if (result.summary.result != BuildResult.Succeeded) throw new Exception("Tripo model probe build failed");
            Debug.Log("TRIPO MODEL GAME BUILD PASSED");
        }

        public static void BuildMaintenanceShowcase()
        {
            Prepare();
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/SpaceMiner/Scenes/AsteroidBelt.unity" },
                locationPathName = "Builds/MaintenanceShowcase/SpaceMiner.exe", target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development });
            if (result.summary.result != BuildResult.Succeeded) throw new Exception("Maintenance showcase build failed");
            Debug.Log("MAINTENANCE SHOWCASE BUILD PASSED");
        }
        public static void BuildMiningShowcase()
        {
            Prepare();
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[] { "Assets/SpaceMiner/Scenes/AsteroidBelt.unity" },
                locationPathName="Builds/TripoMining/SpaceMiner.exe",target=BuildTarget.StandaloneWindows64,
                options=BuildOptions.Development });
            if(result.summary.result!=BuildResult.Succeeded) throw new Exception("Tripo mining build failed");
            Debug.Log("TRIPO MINING BUILD PASSED");
        }
    }
}
