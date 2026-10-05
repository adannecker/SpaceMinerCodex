using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceMiner.Editor
{
    public static class AsteroidAssets
    {
        public const string Root = "Assets/SpaceMiner/Asteroids";
        public const string CatalogPath = Root + "/AsteroidCatalog.asset";

        public static AsteroidCatalog EnsureDefaults()
        {
            foreach (string folder in new[] { Root, Root+"/Types", Root+"/Materials", Root+"/Textures", Root+"/Meshes", Root+"/Prefabs" })
                Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            Shader shader = Shader.Find("SpaceMiner/Asteroid Surface");
            if (shader == null) throw new InvalidOperationException("Asteroiden-Shader konnte nicht geladen werden.");
            Texture2D detail = EnsureDetail();
            AsteroidType ice = EnsureType("IceRich", "Eisreich", type =>
            {
                type.Shape.AxisScale = new Vector3(1.4f,0.83f,0.93f);
                type.Shape.LobeStrength = 0.43f;
                type.Shape.Asymmetry = 0.2f;
                type.Shape.MacroAmplitude = 0.22f;
                type.Shape.FracturePlanes = 8;
                type.Shape.FractureDepth = 0.16f;
                type.Shape.CraterCount = 9;
                type.Shape.CraterRadius = new Vector2(0.1f,0.3f);
                type.Shape.CraterDepth = 0.16f;
                type.Surface.Crust = Layer(new Color(0.14f,0.135f,0.125f),0.02f,0.12f);
                type.Surface.Exposure = Layer(new Color(0.72f,0.75f,0.77f),0f,0.22f);
                type.Surface.Accent = Layer(new Color(0.09f,0.085f,0.08f),0f,0.08f);
                type.Surface.ExposureCount = 3;
                type.Surface.ExposureRadius = new Vector2(0.24f,0.33f);
                type.Surface.ExposureRecess = 0.09f;
                type.Surface.AccentCount = 3;
                type.Surface.DetailStrength = 0.65f;
                type.WaterFraction = 0.8f;
            });
            AsteroidType rock = EnsureType("Rocky", "Felsig", type =>
            {
                type.Shape.AxisScale = new Vector3(1.4f,0.8f,1f);
                type.Shape.LobeStrength = 0.04f;
                type.Shape.MacroAmplitude = 0.14f;
                type.Shape.FracturePlanes = 14;
                type.Shape.FractureDepth = 0.24f;
                type.Shape.RidgeAmplitude = 0.04f;
                type.Shape.CraterCount = 4;
                type.Shape.CraterRadius = new Vector2(0.18f,0.48f);
                type.Shape.CraterDepth = 0.22f;
                type.Surface.Crust = Layer(new Color(0.34f,0.31f,0.27f),0f,0.12f);
                type.Surface.Exposure = Layer(new Color(0.42f,0.39f,0.34f),0f,0.17f);
                type.Surface.Accent = Layer(new Color(0.19f,0.175f,0.15f),0f,0.07f);
                type.Surface.ExposureCount = 4;
                type.Surface.ExposureRadius = new Vector2(0.3f,0.6f);
                type.Surface.ExposureRecess = 0.025f;
                type.Surface.AccentCount = 4;
            });
            AsteroidType metal = EnsureType("MetalRich", "Metallreich", type =>
            {
                type.Shape.AxisScale = new Vector3(1.1f,0.82f,0.95f);
                type.Shape.LobeStrength = 0.08f;
                type.Shape.MacroAmplitude = 0.16f;
                type.Shape.Asymmetry = 0.13f;
                type.Shape.FracturePlanes = 10;
                type.Shape.FractureDepth = 0.2f;
                type.Shape.CraterCount = 10;
                type.Shape.CraterRadius = new Vector2(0.08f,0.32f);
                type.Surface.Crust = Layer(new Color(0.2f,0.205f,0.21f),0.2f,0.2f);
                type.Surface.Exposure = Layer(new Color(0.46f,0.47f,0.48f),0.88f,0.48f);
                type.Surface.Accent = Layer(new Color(0.28f,0.255f,0.22f),0.6f,0.3f);
                type.Surface.ExposureCount = 4;
                type.Surface.ExposureRadius = new Vector2(0.35f,0.58f);
                type.Surface.ExposureRecess = 0.025f;
                type.Surface.AccentCount = 2;
                type.Surface.DetailStrength = 0.55f;
                type.Surface.BumpStrength = 0.24f;
            });
            foreach (AsteroidType type in new[] { ice,rock,metal }) EnsureMaterial(type,shader,detail);
            AsteroidCatalog catalog = AssetDatabase.LoadAssetAtPath<AsteroidCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<AsteroidCatalog>();
                AssetDatabase.CreateAsset(catalog,CatalogPath);
            }
            foreach (AsteroidType type in new[] { ice,rock,metal })
                if (!catalog.Types.Exists(entry => entry != null && entry.Type == type))
                    catalog.Types.Add(new AsteroidCatalog.Entry { Type = type });
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static AsteroidType.SurfaceLayer Layer(Color color, float metal, float smooth)
            => new AsteroidType.SurfaceLayer { Color = color, Metallic = metal, Smoothness = smooth };

        private static AsteroidType EnsureType(string name, string displayName, Action<AsteroidType> configure)
        {
            string path = Root+"/Types/"+name+".asset";
            AsteroidType type = AssetDatabase.LoadAssetAtPath<AsteroidType>(path);
            if (type != null) return type;
            type = ScriptableObject.CreateInstance<AsteroidType>();
            type.name = name;
            type.DisplayName = displayName;
            configure(type);
            AssetDatabase.CreateAsset(type,path);
            return type;
        }

        public static void EnsureMaterial(AsteroidType type, Shader shader = null, Texture2D detail = null)
        {
            if (shader == null) shader = Shader.Find("SpaceMiner/Asteroid Surface");
            if (detail == null) detail = EnsureDetail();
            if (type.SurfaceMaterial == null)
            {
                var material = new Material(shader) { name = type.name+" Surface" };
                string path = AssetDatabase.GenerateUniqueAssetPath(Root+"/Materials/"+SafeName(type.name)+".mat");
                AssetDatabase.CreateAsset(material,path);
                type.SurfaceMaterial = material;
                EditorUtility.SetDirty(type);
            }
            Material target = type.SurfaceMaterial;
            if (target.shader != shader) return; // Custom material implementations stay owned by the author.
            target.SetTexture("_DetailTex",detail);
            SetLayer(target,"_Crust",type.Surface.Crust);
            SetLayer(target,"_Exposure",type.Surface.Exposure);
            SetLayer(target,"_Accent",type.Surface.Accent);
            target.SetFloat("_DetailSize",type.Surface.DetailSizeMeters);
            target.SetFloat("_DetailStrength",type.Surface.DetailStrength);
            target.SetFloat("_BumpStrength",type.Surface.BumpStrength);
            target.SetFloat("_Faceting",type.Surface.Faceting);
            EditorUtility.SetDirty(target);
        }

        private static void SetLayer(Material material, string prefix, AsteroidType.SurfaceLayer layer)
        {
            material.SetColor(prefix+"Color",layer.Color);
            material.SetFloat(prefix+"Metallic",layer.Metallic);
            material.SetFloat(prefix+"Smoothness",layer.Smoothness);
        }

        private static Texture2D EnsureDetail()
        {
            const string path = Root+"/Textures/RegolithDetail.asset";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            const int size = 512;
            var height = new float[size*size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float u = x/(float)size, v = y/(float)size;
                height[y*size+x] = PeriodicNoise(u,v,8)*0.30f + PeriodicNoise(u,v,32)*0.33f
                    + PeriodicNoise(u,v,96)*0.24f + PeriodicNoise(u,v,192)*0.13f;
            }
            var pixels = new Color[size*size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = height[y*size+(x+1)%size]-height[y*size+(x+size-1)%size];
                float dy = height[((y+1)%size)*size+x]-height[((y+size-1)%size)*size+x];
                pixels[y*size+x] = new Color(Mathf.Clamp01(0.5f+dx*3),Mathf.Clamp01(0.5f+dy*3),height[y*size+x],1);
            }
            var texture = new Texture2D(size,size,TextureFormat.RGBA32,true,true) {
                name = "Regolith Detail (RG slope, B height)", wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            texture.SetPixels(pixels);
            texture.Apply(true,false);
            AssetDatabase.CreateAsset(texture,path);
            return texture;
        }

        private static float PeriodicNoise(float u, float v, int cells)
        {
            float px = u*cells, py = v*cells;
            int x = Mathf.FloorToInt(px), y = Mathf.FloorToInt(py);
            float fx = px-x, fy = py-y;
            fx = fx*fx*(3-2*fx); fy = fy*fy*(3-2*fy);
            return Mathf.Lerp(Mathf.Lerp(Hash(x,y,cells),Hash(x+1,y,cells),fx),
                Mathf.Lerp(Hash(x,y+1,cells),Hash(x+1,y+1,cells),fx),fy);
        }

        private static float Hash(int x, int y, int cells)
        {
            unchecked
            {
                uint n = (uint)((x%cells)*374761393+(y%cells)*668265263+cells*31);
                n = (n^(n>>13))*1274126177;
                return (n^(n>>16))/(float)uint.MaxValue;
            }
        }

        public static void Bake(AsteroidGenerator generator)
        {
            if (generator.Type == null) throw new InvalidOperationException("Zuerst einen Asteroidentyp auswählen.");
            EnsureMaterial(generator.Type);
            string typeGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(generator.Type));
            if (string.IsNullOrEmpty(typeGuid)) throw new InvalidOperationException("Den Asteroidentyp zuerst als Asset speichern.");
            string prefix = Root+"/Meshes/"+typeGuid+"_"+generator.Seed+"_S"+generator.HighestSubdivision;
            AsteroidMeshBuilder.Result result = AsteroidMeshBuilder.Build(generator.Type,generator.Seed,generator.HighestSubdivision);
            for (int i = 0; i < result.Levels.Length; i++)
            {
                string path = prefix+"_LOD"+i+".asset";
                Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (existing == null) AssetDatabase.CreateAsset(result.Levels[i],path);
                else
                {
                    EditorUtility.CopySerialized(result.Levels[i],existing);
                    UnityEngine.Object.DestroyImmediate(result.Levels[i]);
                    result.Levels[i] = existing;
                    EditorUtility.SetDirty(existing);
                }
            }
            generator.SetBakedMeshes(result.Levels);
            generator.Generate();
            EditorUtility.SetDirty(generator);
        }

        public static void UpgradeBelt()
        {
            AsteroidCatalog catalog = EnsureDefaults();
            var settings = AssetDatabase.LoadAssetAtPath<BeltSettings>("Assets/SpaceMiner/BeltSettings.asset");
            if (settings == null) throw new InvalidOperationException("BeltSettings fehlt.");
            if (settings.Catalog == null) settings.Catalog = catalog;
            var root = GameObject.Find("Asteroids (1 unit = 1 metre)");
            if (root == null) throw new InvalidOperationException("Asteroidenwurzel fehlt in der Szene.");
            for (int i = 0; i < settings.Asteroids.Length; i++)
            {
                BeltSettings.Asteroid entry = settings.Asteroids[i];
                if (entry.Type == null)
                    entry.Type = i < 3 ? AssetDatabase.LoadAssetAtPath<AsteroidType>(Root+"/Types/IceRich.asset") : settings.Catalog.Choose(71423+i*101);
                if (entry.Seed == 0) entry.Seed = 71423+i*101;
                settings.Asteroids[i] = entry;
                // Transform.Find interprets '/' as a hierarchy separator, while
                // names such as "A-12 / Grossasteroid" contain a literal slash.
                Transform bodyTransform = null;
                foreach (Transform child in root.transform)
                    if (child.name == entry.Name) { bodyTransform = child; break; }
                if (bodyTransform == null) continue;
                GameObject body = bodyTransform.gameObject;
                var oldCollider = body.GetComponent<SphereCollider>();
                if (oldCollider != null) UnityEngine.Object.DestroyImmediate(oldCollider);
                var generator = body.GetComponent<AsteroidGenerator>();
                if (generator == null)
                {
                    generator = body.AddComponent<AsteroidGenerator>();
                    generator.Type = entry.Type;
                    generator.Seed = entry.Seed;
                    generator.DiameterMeters = entry.DiameterMeters;
                    if (i < 3) generator.WaterFractionOverride = i == 1 ? 0.65f : 0.8f;
                    body.transform.localRotation = Quaternion.Euler((entry.Seed%37)*7,(entry.Seed%53)*5,(entry.Seed%29)*11);
                }
                // Preserve the established starting deposits on the first migration as well.
                if (i < 3 && generator.WaterFractionOverride < 0) generator.WaterFractionOverride = i == 1 ? 0.65f : 0.8f;
                // The scene is authoritative for already edited positions, sizes, types and seeds.
                Bake(generator);
            }
            EditorUtility.SetDirty(settings);
            ConfigureLighting();
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureLighting()
        {
            RenderSettings.ambientLight = new Color(0.105f,0.105f,0.105f);
            var sunObject = GameObject.Find("Sun");
            if (sunObject != null)
            {
                var sun = sunObject.GetComponent<Light>();
                sun.color = new Color(1,0.97f,0.93f);
                sun.intensity = 1.8f;
                sun.shadows = LightShadows.Soft;
                sun.shadowResolution = UnityEngine.Rendering.LightShadowResolution.VeryHigh;
                sun.shadowBias = 0.08f;
                sun.shadowNormalBias = 0.15f;
            }
            var fillObject = GameObject.Find("Reflected Light");
            if (fillObject != null)
            {
                var fill = fillObject.GetComponent<Light>();
                fill.color = new Color(0.8f,0.8f,0.8f);
                fill.intensity = 0.10f;
            }
        }

        public static void CreateSamples()
        {
            EnsureDefaults();
            foreach (string typeName in new[] { "IceRich", "Rocky", "MetalRich" })
            {
                var type = AssetDatabase.LoadAssetAtPath<AsteroidType>(Root+"/Types/"+typeName+".asset");
                for (int variant = 0; variant < 3; variant++)
                {
                    int size = new[] {100,1000,5000}[variant];
                    string path = Root+"/Prefabs/"+typeName+"_"+size+"m.prefab";
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) continue;
                    var item = new GameObject(typeName+" "+size+" m");
                    try
                    {
                        var generator = item.AddComponent<AsteroidGenerator>();
                        generator.Type = type;
                        generator.Seed = 1907+variant*997;
                        generator.DiameterMeters = size;
                        Bake(generator);
                        PrefabUtility.SaveAsPrefabAsset(item,path);
                    }
                    finally { UnityEngine.Object.DestroyImmediate(item); }
                }
            }
            AssetDatabase.SaveAssets();
        }

        public static void RebuildSamples()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab",new[] { Root+"/Prefabs" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var generator = contents.GetComponent<AsteroidGenerator>();
                    if (generator != null) { Bake(generator); PrefabUtility.SaveAsPrefabAsset(contents,path); }
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Space Miner/Asteroiden/Materialien und Beispiel-Prefabs erzeugen")]
        public static void GenerateLibrary() { CreateSamples(); }

        [MenuItem("Space Miner/Asteroiden/Asteroidenfeld aktualisieren")]
        public static void UpdateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            UpgradeBelt();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        [MenuItem("GameObject/Space Miner/Prozeduraler Asteroid", false, 10)]
        public static void CreateAsteroid()
        {
            AsteroidCatalog catalog = EnsureDefaults();
            var item = new GameObject("Prozeduraler Asteroid");
            Undo.RegisterCreatedObjectUndo(item,"Asteroid erzeugen");
            var generator = item.AddComponent<AsteroidGenerator>();
            generator.Type = Selection.activeObject as AsteroidType ?? catalog.Choose(71423);
            Bake(generator);
            Selection.activeGameObject = item;
        }

        private static string SafeName(string value)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) value = value.Replace(c,'_');
            return value;
        }
    }

    [CustomEditor(typeof(AsteroidGenerator))]
    public sealed class AsteroidGeneratorInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("Typ, Seed oder Größe ändern, dann backen. Vier LODs und ein MeshCollider werden automatisch erstellt.",MessageType.Info);
            if (GUILayout.Button("Meshes und Oberfläche neu backen"))
            {
                var generator = (AsteroidGenerator)target;
                Undo.RecordObject(generator,"Asteroidenparameter ändern");
                AsteroidAssets.EnsureDefaults();
                AsteroidAssets.Bake(generator);
                AssetDatabase.SaveAssets();
                if (generator.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
            }
        }
    }
}
