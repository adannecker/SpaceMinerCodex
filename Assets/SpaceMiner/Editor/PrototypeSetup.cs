using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SpaceMiner.Editor
{
    public static class PrototypeSetup
    {
        public const string ScenePath = "Assets/SpaceMiner/Scenes/AsteroidBelt.unity";
        private const string GeneratedPath = "Assets/SpaceMiner/Generated";

        [InitializeOnLoadMethod]
        private static void PrepareFirstOpen()
        {
            if (Application.isBatchMode || File.Exists(ScenePath)) return;
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                {
                    PrepareFirstOpen();
                    return;
                }
                if (!File.Exists(ScenePath)) Setup();
            };
        }

        [MenuItem("Space Miner/Prototyp einrichten")]
        public static void Setup()
        {
            Directory.CreateDirectory("Assets/SpaceMiner/Scenes");
            Directory.CreateDirectory(GeneratedPath);
            AssetDatabase.Refresh();
            PlayerSettings.companyName = "Space Miner";
            PlayerSettings.productName = "Space Miner - Prototyp 01";
            PlayerSettings.bundleVersion = "0.2.0";
            PlayerSettings.defaultScreenWidth = 1440;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            // No external input package is required: use Unity's built-in mouse/keyboard input.
            var playerSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var activeInput = playerSettings.FindProperty("activeInputHandler");
            if (activeInput != null) { activeInput.intValue = 0; playerSettings.ApplyModifiedPropertiesWithoutUndo(); }
            EditorSettings.serializationMode = SerializationMode.ForceText;
            QualitySettings.vSyncCount = 1;
            QualitySettings.antiAliasing = 4;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowCascades = 4;
            QualitySettings.shadowDistance = 1000;

            if (File.Exists(ScenePath))
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                Scene existing = EditorSceneManager.OpenScene(ScenePath);
                AsteroidAssets.UpgradeBelt();
                AsteroidAssets.CreateSamples();
                EnsureScenario();
                EditorSceneManager.SaveScene(existing);
                Debug.Log("SPACE MINER: Existing scene upgraded with procedural asteroids and water scenario.");
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
                AssetDatabase.SaveAssets();
                return;
            }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.16f, 0.19f, 0.24f);
            RenderSettings.fog = false;
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.004f, 0.008f, 0.017f);
            camera.fieldOfView = 50f;
            camera.allowHDR = false;
            var controller = cameraObject.AddComponent<OrbitCamera>();
            controller.ResetView();
            cameraObject.AddComponent<PrototypeSmokeTest>();

            CreateLight("Sun", new Vector3(28, -40, 0), new Color(1f, 0.91f, 0.79f), 1.5f);
            CreateLight("Reflected Light", new Vector3(160, 120, 0), new Color(0.35f, 0.56f, 0.9f), 0.3f);

            BeltSettings settings = AssetDatabase.LoadAssetAtPath<BeltSettings>("Assets/SpaceMiner/BeltSettings.asset");
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<BeltSettings>();
                AssetDatabase.CreateAsset(settings, "Assets/SpaceMiner/BeltSettings.asset");
            }
            var asteroidRoot = new GameObject("Asteroids (1 unit = 1 metre)");
            Material[] rockMaterials =
            {
                CreateMaterial("Rock Iron", new Color(0.27f, 0.25f, 0.23f), 0.23f),
                CreateMaterial("Rock Silicate", new Color(0.39f, 0.34f, 0.29f), 0.05f),
                CreateMaterial("Rock Carbon", new Color(0.2f, 0.23f, 0.26f), 0.08f)
            };
            for (int i = 0; i < settings.Asteroids.Length; i++)
            {
                BeltSettings.Asteroid asteroid = settings.Asteroids[i];
                GameObject body = Primitive(PrimitiveType.Sphere, asteroid.Name, asteroidRoot.transform,
                    asteroid.PositionMeters, Vector3.one * asteroid.DiameterMeters, rockMaterials[i % rockMaterials.Length]);
                var info = body.AddComponent<SpaceObject>();
                info.DisplayName = asteroid.Name;
                info.DiameterMeters = asteroid.DiameterMeters;
                info.Description = "Asteroid / Kugelplatzhalter";
            }

            CreateShip();
            CreateStars(camera);
            AsteroidAssets.UpgradeBelt();
            AsteroidAssets.CreateSamples();
            EnsureScenario();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("SPACE MINER: Scene ready, 12 asteroids, ship and two 2 m drones.");
        }

        [MenuItem("Space Miner/Windows-Spiel bauen")]
        public static void BuildWindows()
        {
            Setup();
            Directory.CreateDirectory("Builds/Windows");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/SpaceMiner.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Space Miner build failed: " + report.summary.result);
            Debug.Log("SPACE MINER BUILD PASSED: " + report.summary.totalSize + " bytes");
        }

        private static void EnsureScenario()
        {
            GameObject ship = GameObject.Find("Stranded Ship");
            if (ship.GetComponent<WaterScenario>() == null) ship.AddComponent<WaterScenario>();
            GameObject camera = GameObject.Find("Main Camera");
            if (camera.GetComponent<WaterScenarioHud>() == null) camera.AddComponent<WaterScenarioHud>();
            if (camera.GetComponent<IntroSequence>() == null) camera.AddComponent<IntroSequence>();
        }

        private static void CreateLight(string name, Vector3 rotation, Color color, float intensity)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(rotation);
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }

        private static Material CreateMaterial(string name, Color color, float metallic, bool glow = false)
        {
            string path = GeneratedPath + "/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var material = new Material(Shader.Find("Standard")) { name = name, color = color };
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", 0.22f);
            if (glow)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.5f);
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject Primitive(PrimitiveType kind, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject item = GameObject.CreatePrimitive(kind);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = scale;
            item.GetComponent<Renderer>().sharedMaterial = material;
            return item;
        }

        private static void CreateShip()
        {
            Material hull = CreateMaterial("Ship Hull", new Color(0.38f, 0.46f, 0.51f), 0.65f);
            Material dark = CreateMaterial("Ship Frame", new Color(0.07f, 0.1f, 0.13f), 0.7f);
            Material amber = CreateMaterial("Emergency Light", new Color(1f, 0.32f, 0.055f), 0f, true);
            Material blue = CreateMaterial("Drone Light", new Color(0.1f, 0.72f, 1f), 0f, true);
            Material panel = CreateMaterial("Solar Panel", new Color(0.035f, 0.075f, 0.15f), 0.45f);
            var ship = new GameObject("Stranded Ship");
            var shipInfo = ship.AddComponent<SpaceObject>();
            shipInfo.DisplayName = "HAVARIST / Schiff";
            shipInfo.DiameterMeters = 24f;
            shipInfo.Description = "24 m Laenge / Platzhalter";
            Primitive(PrimitiveType.Cube, "Hull", ship.transform, Vector3.zero, new Vector3(5, 4, 18), hull);
            Primitive(PrimitiveType.Cube, "Nose", ship.transform, new Vector3(0, 0, 10), new Vector3(4, 3, 4), dark);
            Primitive(PrimitiveType.Cube, "Rear", ship.transform, new Vector3(0, 0, -10), new Vector3(7, 5, 4), dark);
            Primitive(PrimitiveType.Cube, "Emergency Strip", ship.transform, new Vector3(0, 2.05f, -3), new Vector3(3, 0.15f, 6), amber);
            Primitive(PrimitiveType.Cube, "Solar Panel L", ship.transform, new Vector3(-7, 0, 0), new Vector3(8, 0.2f, 9), panel);
            Primitive(PrimitiveType.Cube, "Solar Panel R (damaged)", ship.transform, new Vector3(5.5f, 0, 2), new Vector3(5, 0.2f, 5), panel);
            for (int i = 0; i < 2; i++)
            {
                var drone = new GameObject("Drone " + (i + 1));
                drone.transform.position = new Vector3(i == 0 ? -17 : 15, i == 0 ? 5 : -3, i == 0 ? -7 : 7);
                var info = drone.AddComponent<SpaceObject>();
                info.DisplayName = "DROHNE 0" + (i + 1);
                info.DiameterMeters = 2;
                info.Description = "2 x 2 x 2 m / Platzhalter";
                Primitive(PrimitiveType.Cube, "Body", drone.transform, Vector3.zero, Vector3.one * 2f, hull);
                Primitive(PrimitiveType.Cube, "Status Light", drone.transform, new Vector3(0, 0, -1.01f), new Vector3(0.75f, 0.4f, 0.04f), blue);
            }
        }

        private static void CreateStars(Camera camera)
        {
            const int count = 1600;
            const float radius = 45000f;
            var random = new System.Random(71423);
            var vertices = new Vector3[count * 4];
            var colors = new Color[count * 4];
            var triangles = new int[count * 6];
            for (int i = 0; i < count; i++)
            {
                float z = (float)random.NextDouble() * 2f - 1f;
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float planar = Mathf.Sqrt(1f - z * z);
                Vector3 direction = new Vector3(planar * Mathf.Cos(angle), z, planar * Mathf.Sin(angle));
                Vector3 tangent = Vector3.Cross(direction, Mathf.Abs(z) > 0.9f ? Vector3.right : Vector3.up).normalized;
                Vector3 bitangent = Vector3.Cross(direction, tangent);
                float size = Mathf.Lerp(12f, 36f, (float)random.NextDouble());
                Vector3 centre = direction * radius;
                int v = i * 4;
                vertices[v] = centre - tangent * size - bitangent * size;
                vertices[v + 1] = centre + tangent * size - bitangent * size;
                vertices[v + 2] = centre + tangent * size + bitangent * size;
                vertices[v + 3] = centre - tangent * size + bitangent * size;
                float brightness = Mathf.Lerp(0.25f, 0.8f, (float)random.NextDouble());
                Color color = new Color(brightness * 0.83f, brightness * 0.91f, brightness);
                for (int k = 0; k < 4; k++) colors[v + k] = color;
                int t = i * 6;
                triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
                triangles[t + 3] = v; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
            }
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(GeneratedPath + "/Starfield.asset");
            if (mesh == null)
            {
                mesh = new Mesh { name = "Starfield", vertices = vertices, colors = colors, triangles = triangles };
                mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, GeneratedPath + "/Starfield.asset");
            }
            Material material = AssetDatabase.LoadAssetAtPath<Material>(GeneratedPath + "/Stars.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("SpaceMiner/Starfield"));
                AssetDatabase.CreateAsset(material, GeneratedPath + "/Stars.mat");
            }
            var stars = new GameObject("Distant Stars", typeof(MeshFilter), typeof(MeshRenderer), typeof(Starfield));
            stars.GetComponent<MeshFilter>().sharedMesh = mesh;
            stars.GetComponent<MeshRenderer>().sharedMaterial = material;
            stars.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            stars.GetComponent<MeshRenderer>().receiveShadows = false;
            stars.GetComponent<Starfield>().View = camera;
        }
    }
}
