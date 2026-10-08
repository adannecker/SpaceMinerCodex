using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    // Opt-in integration check in the actual Windows player. Normal games never run this.
    public sealed class PrototypeSmokeTest : MonoBehaviour
    {
        private bool running;
        private int checks;

        private IEnumerator Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-spaceMinerSmokeTest") < 0) yield break;
            running = true;
            Application.logMessageReceived += OnLog;
            Directory.CreateDirectory("Logs");
            File.Delete("Logs/smoke-test-result.json");
            File.Delete("Logs/smoke-test-error.txt");
            var scenario = FindFirstObjectByType<WaterScenario>();
            File.Delete("Logs/asteroid-" + scenario.Asteroids.Length + "-performance.json");
            yield return null;
            yield return CheckIntro(scenario);
            PrepareScan(scenario);
            if (!running) yield break;
            scenario.SimulationRate = 0;
            yield return null;
            yield return new WaitForEndOfFrame();
            bool passed = CheckSceneAndCamera();
            if (!passed || !running) yield break;
            var camera = GetComponent<OrbitCamera>();
            camera.ResetView();
            yield return new WaitForEndOfFrame();
            Capture("Logs/prototype-home.png");
            var dock=scenario.GetComponent<StationVisual>().DroneDock;
            CaptureView("Logs/dock-charging.png",dock.TransformPoint(new Vector3(12,15,15)),dock.position+Vector3.up);
            if (!running) yield break;
            camera.Overview();
            yield return new WaitForEndOfFrame();
            Capture("Logs/prototype-overview.png");
            if (!running) yield break;
            var spiral = GetComponent<SpiralBelt>();
            if (spiral != null && spiral.IsReady && !Guard(() => Require(spiral.LastVisibleCount == spiral.AsteroidCount - 100 && spiral.LastDrawCalls > 0,
                "all 9900 additional bodies submitted in instanced overview"))) yield break;
            yield return MeasureAsteroidPerformance(camera);
            if (!running) yield break;
            camera.Zoom(-1000);
            yield return new WaitForEndOfFrame();
            Capture("Logs/prototype-far-zoom.png");
            if (!running) yield break;
            camera.Focus(GameObject.Find("Drone 1").GetComponent<SpaceObject>());
            yield return new WaitForEndOfFrame();
            camera.Orbit(new Vector2(150,-18));
            yield return new WaitForEndOfFrame();
            Capture("Logs/prototype-drone.png");
            if (!running) yield break;
            foreach (string typeName in new[] { "Eisreich", "Felsig", "Metallreich" })
            {
                var root = GameObject.Find("Asteroids (1 unit = 1 metre)").transform;
                foreach (Transform body in root)
                {
                    var generator = body.GetComponent<AsteroidGenerator>();
                    if (generator == null || generator.Type.DisplayName != typeName) continue;
                    camera.Focus(body.GetComponent<SpaceObject>());
                    camera.Zoom(1);
                    yield return new WaitForEndOfFrame();
                    Capture("Logs/asteroid-" + typeName + ".png");
                    break;
                }
            }
            if (!running) yield break;
            yield return CheckWaterScenario(scenario, camera);
            if (!running) yield break;
            File.WriteAllText("Logs/smoke-test-result.json", "{\"passed\":true,\"checks\":" + checks + "}");
            Debug.Log("SPACE MINER SMOKE TEST PASSED: " + checks + " checks");
            running = false;
            Application.Quit(0);
        }

        private bool CheckSceneAndCamera()
        {
            try
            {
                var station = GameObject.Find("Stranded Ship");
                if (station.GetComponent<StationVisual>() != null) { Require(station.transform.Find("Station Geometry/Access Ring").childCount >= 24, "station access ring" ); Require(station.transform.Find("Station Geometry/Water Ice Tank") != null, "station water tank" ); Require(station.transform.Find("Station Geometry/Solar Wing Left") != null && station.transform.Find("Station Geometry/Solar Wing Right") != null, "two station solar wings" ); }
                var controller = GetComponent<OrbitCamera>();
                var objects = FindObjectsByType<SpaceObject>(FindObjectsSortMode.None);
                var spiral = GetComponent<SpiralBelt>();
                if (spiral != null && spiral.Cloud != null) return CheckMillionCloud(spiral, controller);
                bool spiralMode = spiral != null && spiral.IsReady;
                int expected = spiralMode ? 10000 : 100;
                Require(objects.Length == expected + 11, expected + " asteroids, a ship and ten drones");
                var asteroids = GameObject.Find("Asteroids (1 unit = 1 metre)");
                Require(asteroids.transform.childCount == expected, expected + " asteroids");
                var shapes = new HashSet<string>();
                var meshes = new HashSet<Mesh>();
                var types = new HashSet<AsteroidType>();
                foreach (Transform body in asteroids.transform)
                {
                    var generator = body.GetComponent<AsteroidGenerator>();
                    var instance = body.GetComponent<SpiralAsteroid>();
                    if (instance != null)
                    {
                        Require(instance.Template != null && instance.Template.BakedMeshes.Length == 4, "instanced body has four shared LODs");
                        Require(body.GetComponent<MeshCollider>().sharedMesh == instance.Template.BakedMeshes[2], "instanced body has matching picking geometry");
                        Require(body.GetComponent<SpaceObject>() != null && body.GetComponent<AsteroidResource>() != null, "instanced body can be selected and inspected");
                        Require(instance.Template.Type.SurfaceMaterial.enableInstancing, "instancing material preserved in build");
                        continue;
                    }
                    Require(generator != null && generator.Type != null, "configured generated asteroid " + body.name);
                    types.Add(generator.Type);
                    shapes.Add(generator.Type.name + ":" + generator.Seed);
                    meshes.Add(body.GetComponent<MeshFilter>().sharedMesh);
                    Require(body.GetComponent<LODGroup>().GetLODs().Length == 4, "four asteroid LODs");
                    Require(body.GetComponent<MeshCollider>().sharedMesh != null && body.GetComponent<SphereCollider>() == null, "irregular picking collider");
                    Require(body.GetComponent<Renderer>().sharedMaterial.shader.name == "SpaceMiner/Asteroid Surface", "asteroid PBR shader");
                }
                Require(shapes.Count == 100 && meshes.Count == 100, "100 distinct seeds and baked high-detail meshes");
                Require(types.Count >= 3, "ice, rock and metal variants represented");
                if (spiralMode)
                {
                    ValidateSpiral(asteroids.transform, spiral);
                }
                else
                {
                    var octants = new HashSet<int>();
                    for (int i = 3; i < asteroids.transform.childCount; i++)
                    {
                        var body = asteroids.transform.GetChild(i).GetComponent<SpaceObject>();
                        Vector3 p = body.transform.position;
                        octants.Add((p.x >= 0 ? 1 : 0) | (p.y >= 0 ? 2 : 0) | (p.z >= 0 ? 4 : 0));
                        Require(p.magnitude <= 18000.1f && p.magnitude >= 4000f + body.DiameterMeters * 0.5f - 0.1f, "cloud body inside spherical shell");
                        for (int j = 0; j < i; j++)
                        {
                            var other = asteroids.transform.GetChild(j).GetComponent<SpaceObject>();
                            if (Vector3.Distance(body.transform.position, other.transform.position) <
                                (body.DiameterMeters + other.DiameterMeters) * 0.5f + 399f)
                                throw new Exception("Test asteroids overlap: " + body.name + " / " + other.name);
                        }
                    }
                    Require(octants.Count == 8, "cloud surrounds ship in all eight octants");
                    Require(true, "all 97 non-starter cloud asteroids have at least 400 metres clearance");
                }
                var largest = GameObject.Find("A-12 / Grossasteroid").GetComponent<SpaceObject>();
                Require(Mathf.Approximately(largest.transform.localScale.x, 5000), "5 km asteroid scale");
                var drone = GameObject.Find("Drone 1").GetComponent<SpaceObject>();
                var droneBounds = new Bounds(Vector3.zero, Vector3.zero);
                foreach (var filter in drone.GetComponentsInChildren<MeshFilter>()) {
                    Bounds b=filter.sharedMesh.bounds;
                    for(int c=0;c<8;c++) droneBounds.Encapsulate(drone.transform.InverseTransformPoint(filter.transform.TransformPoint(
                        b.center+Vector3.Scale(b.extents,new Vector3((c&1)==0?-1:1,(c&2)==0?-1:1,(c&4)==0?-1:1)))));
                }
                Require(Mathf.Abs(droneBounds.size.x - 2f) < 0.02f, "2 m drone width" );
                Require(drone.transform.Find("Mining Drone Geometry/Mining Drill") != null && drone.transform.Find("Mining Drone Geometry/Gripper Arm") != null, "mining drone drill and gripper" );
                controller.ResetView();
                Vector3 home = transform.position;
                Require(Mathf.Approximately(controller.Distance, 140), "home distance");
                var homeView = GetComponent<Camera>();
                Require(homeView.nearClipPlane >= 1f && homeView.nearClipPlane < 2f,
                    "station view reserves depth precision for small surface details");
                foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (light.enabled && light.type == LightType.Directional)
                        Require((light.cullingMask & ((1 << 29) | (1 << 28))) == 0,
                            "local sunlight excludes kilometre-scale shadow rendering");
                controller.Zoom(1);
                Require(controller.Distance < 140, "zoom in");
                controller.Zoom(-1);
                Require(Mathf.Abs(controller.Distance - 140) < 0.01f, "zoom out");
                controller.Zoom(1000);
                Require(controller.Distance == OrbitCamera.MinimumDistance, "minimum zoom clamp");
                controller.Zoom(-1000);
                Require(controller.Distance == OrbitCamera.MaximumDistance, "maximum zoom clamp");
                Require(controller.Distance == OrbitCamera.MaximumDistance, "maximum zoom range");
                var view = GetComponent<Camera>();
                foreach (Transform child in asteroids.transform)
                {
                    Vector3 projected = view.WorldToViewportPoint(child.position);
                    float radius = child.GetComponent<SpaceObject>().DiameterMeters * 0.5f;
                    Require(projected.z - radius > view.nearClipPlane && projected.z + radius < view.farClipPlane,
                        "far zoom retains " + child.name + " inside clipping range");
                }
                Require(view.nearClipPlane > 3f, "far zoom adjusts depth range");
                Require(view.nearClipPlane >= controller.Distance * 0.005f,
                    "far zoom retains proportional depth precision");
                controller.Focus(drone);
                Require(view.nearClipPlane <= 0.1f && controller.Distance <= 6f, "drone detail restored after far zoom");
                controller.ResetView();
                controller.Zoom(-1);
                float normalZoom = controller.Distance;
                controller.ResetView();
                controller.Zoom(-1, true);
                Require(controller.Distance > normalZoom, "shift wheel accelerates zoom");
                controller.Zoom(1, true);
                Require(Mathf.Abs(controller.Distance - 140f) < 0.01f, "fast zoom is reversible");
                controller.ResetView();
                controller.Orbit(new Vector2(30, 10));
                Require(Vector3.Distance(transform.position, home) > 1f, "orbit moves camera");
                Require(Vector3.Dot(transform.forward, (controller.Pivot - transform.position).normalized) > 0.999f, "orbit looks at pivot");
                controller.Pan(new Vector2(100, 40));
                Require(controller.Pivot.sqrMagnitude > 1f, "pan moves pivot");
                Vector3 beforeMove = controller.Pivot;
                controller.Move(Vector3.forward, 1, false);
                float normalTravel = Vector3.Distance(controller.Pivot, beforeMove);
                Require(normalTravel > 1, "keyboard movement");
                beforeMove = controller.Pivot;
                controller.Move(Vector3.forward, 1, true);
                Require(Mathf.Abs(Vector3.Distance(controller.Pivot, beforeMove) / normalTravel - 3) < 0.01f, "shift speed multiplier");
                controller.ResetView();
                Require(Vector3.Distance(transform.position, home) < 0.001f && controller.Pivot == Vector3.zero, "reset restores home");
                controller.Overview();
                foreach (Transform child in asteroids.transform)
                {
                    Vector3 projected = view.WorldToViewportPoint(child.position);
                    Require(projected.z > 0 && projected.x > 0 && projected.x < 1 && projected.y > 0 && projected.y < 1, "overview includes " + child.name);
                }
                if (spiralMode)
                {
                    foreach (int index in new[] { 100, 4998, 9998 })
                    {
                        var target = asteroids.transform.GetChild(index).GetComponent<SpaceObject>();
                        Require(spiral.PickOverview(view.WorldToScreenPoint(target.transform.position), view) == target,
                            "tiny overview symbol is selectable at index " + index);
                    }
                }
                controller.Focus(drone);
                Require(controller.Pivot == drone.transform.position && controller.Selected == drone, "focus selects drone");
                Physics.SyncTransforms();
                Require(Physics.Raycast(view.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)), out RaycastHit hit), "object raycast");
                Require(hit.collider.GetComponentInParent<SpaceObject>() == drone, "object picking targets drone");
                controller.Focus(largest);
                Require(controller.Distance > 5000 && view.farClipPlane > controller.Distance + 5000, "large asteroid focus and far clip");
                if (spiralMode)
                {
                    foreach (int index in new[] { 100, 4999, 9999 })
                    {
                        var target = asteroids.transform.GetChild(index).GetComponent<SpaceObject>();
                        controller.Focus(target);
                        Require(Physics.Raycast(view.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)), out RaycastHit picked)
                            && picked.collider.GetComponent<SpaceObject>() == target, "instanced asteroid picking at index " + index);
                    }
                }
                return true;
            }
            catch (Exception error)
            {
                Fail(error.ToString());
                return false;
            }
        }

        [Serializable]
        private sealed class PerformanceSample
        {
            public string view;
            public int frames;
            public float averageFps;
            public float p95FrameMs;
            public int visibleInstancedBodies, instancedDrawCalls;
            public int submittedPointBodies, pointDrawCalls, localMeshBodies;
        }

        [Serializable]
        private sealed class PerformanceReport
        {
            public int asteroidCount, width, height, vSyncCount;
            public string gpu;
            public float outerRadiusMeters, spiralTurns;
            public PerformanceSample[] samples;
            public float generationSeconds;
            public long managedMemoryBytes, unityAllocatedMemoryBytes;
            public string representation;
        }

        private IEnumerator MeasureAsteroidPerformance(OrbitCamera camera)
        {
            var samples = new List<PerformanceSample>();
            var spiral = GetComponent<SpiralBelt>();
            foreach (string name in new[] { "overview", "orbit-overview", "near-5km-asteroid", "outer-arm" })
            {
                if (name == "near-5km-asteroid") camera.Focus(GameObject.Find("A-12 / Grossasteroid").GetComponent<SpaceObject>());
                else if (name == "outer-arm") camera.Focus(spiral != null && spiral.Cloud != null ? spiral.Cloud.Inspect(spiral.Cloud.OuterBodyIndex) : GameObject.Find("Asteroids (1 unit = 1 metre)").transform.GetChild(FindFirstObjectByType<WaterScenario>().Asteroids.Length - 1).GetComponent<SpaceObject>());
                else camera.Overview();
                // Exclude captures, shader warmup and changes of view from the measurement.
                for (int i = 0; i < 30; i++) yield return null;
                var times = new List<float>();
                float seconds = 0;
                while (seconds < 3f && running)
                {
                    if (name == "orbit-overview") camera.Orbit(new Vector2(12f * Time.unscaledDeltaTime, 0));
                    yield return null;
                    float dt = Time.unscaledDeltaTime;
                    times.Add(dt * 1000f);
                    seconds += dt;
                }
                if (!running) yield break;
                times.Sort();
                samples.Add(new PerformanceSample { view = name, frames = times.Count,
                    averageFps = times.Count / seconds, p95FrameMs = times[Mathf.Clamp(Mathf.CeilToInt(times.Count * 0.95f) - 1, 0, times.Count - 1)],
                    visibleInstancedBodies = spiral != null && spiral.Cloud == null ? spiral.LastVisibleCount : 0,
                    instancedDrawCalls = spiral != null && spiral.Cloud == null ? spiral.LastDrawCalls : 0,
                    submittedPointBodies = spiral != null && spiral.Cloud != null ? spiral.Cloud.Bodies.Length : 0,
                    pointDrawCalls = spiral != null && spiral.Cloud != null ? spiral.Cloud.PointDrawCalls : 0,
                    localMeshBodies = spiral != null && spiral.Cloud != null ? spiral.Cloud.LocalMeshCount : 0 });
                if (spiral != null && spiral.Cloud != null)
                {
                    if (name == "overview" || name == "outer-arm")
                    {
                        yield return new WaitForEndOfFrame(); Capture("Logs/million-" + name + ".png");
                    }
                    if (name == "outer-arm" && !Guard(() => Require(spiral.Cloud.LocalMeshCount > 0, "local 3D meshes render at outer cloud body"))) yield break;
                }
            }
            int count = spiral != null && spiral.IsReady ? spiral.AsteroidCount : FindFirstObjectByType<WaterScenario>().Asteroids.Length;
            File.WriteAllText("Logs/asteroid-" + count + "-performance.json", JsonUtility.ToJson(new PerformanceReport {
                asteroidCount = count, width = Screen.width, height = Screen.height, gpu = SystemInfo.graphicsDeviceName,
                outerRadiusMeters = spiral != null ? spiral.OuterRadius : 0, spiralTurns = spiral != null ? spiral.LastAngle / (Mathf.PI * 2f) : 0,
                vSyncCount = QualitySettings.vSyncCount, samples = samples.ToArray(),
                generationSeconds = spiral != null && spiral.Cloud != null ? spiral.Cloud.GenerationSeconds : 0,
                managedMemoryBytes = GC.GetTotalMemory(false), unityAllocatedMemoryBytes = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),
                representation = spiral != null && spiral.Cloud != null ? "999900 compact bodies / point overview / local template meshes / lazy selection colliders" : "GameObjects and instanced meshes" }, true));
            camera.Overview();
        }

        private bool CheckMillionCloud(SpiralBelt spiral, OrbitCamera camera)
        {
            var cloud = spiral.Cloud;
            Require(cloud.Count == 1000000 && cloud.Bodies.Length == 999900, "exactly one million stored bodies");
            Require(GameObject.Find("Asteroids (1 unit = 1 metre)").transform.childCount == 100, "only 100 physical starter templates");
            camera.Overview();
            var view = GetComponent<Camera>();
            float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
            foreach (var body in cloud.Bodies)
            {
                if (float.IsNaN(body.Position.x) || float.IsInfinity(body.Position.x) || !cloud.Bounds.Contains(body.Position) || body.Diameter < 100 || body.Diameter > 5000)
                    throw new Exception("Invalid compact cloud body");
                minY = Mathf.Min(minY, body.Position.y); maxY = Mathf.Max(maxY, body.Position.y);
            }
            Require(maxY - minY > 100000, "three dimensional cloud thickness over 100 km");
            Require(camera.Distance > 1000000 && camera.Distance < OrbitCamera.MaximumDistance, "whole cloud fits camera range");
            foreach (int index in new[] { 0, 12345, 499999, 999899 })
            {
                var body = cloud.Bodies[index];
                var p = view.WorldToViewportPoint(body.Position);
                Require(p.z > 0 && p.x > 0 && p.x < 1 && p.y > 0 && p.y < 1, "sample inside overview");
                var identity = cloud.Inspect(index);
                Require(cloud.Inspect(index) == identity && identity.transform.position == body.Position, "stable selected identity and position");
                Require(identity.GetComponent<AsteroidResource>() != null && identity.GetComponent<MeshCollider>().sharedMesh != null, "selected body resource and collider");
                camera.Focus(identity); Physics.SyncTransforms();
                Require(camera.Selected == identity && camera.Pivot == body.Position, "distant focus");
                Require(cloud.Pick(view.WorldToScreenPoint(body.Position), view) != null, "focused compact body can be picked on screen");
                camera.Overview();
            }
            var picked = cloud.Pick(view.WorldToScreenPoint(cloud.Bodies[12345].Position), view);
            Require(picked != null && picked.GetComponent<AsteroidResource>() != null, "cloud screen picking");
            camera.ResetView(); Require(camera.Distance == 140f, "home restored");
            camera.Focus(GameObject.Find("Drone 1").GetComponent<SpaceObject>());
            Require(camera.Distance == 6f && view.nearClipPlane <= 0.1f, "drone detail after cloud overview");
            camera.Overview();
            return true;
        }

        private void ValidateSpiral(Transform root, SpiralBelt spiral)
        {
            var cells = new Dictionary<Vector3Int, List<SpaceObject>>();
            float previousRadius = 0;
            const float cellSize = 6000f;
            for (int i = 0; i < root.childCount; i++)
            {
                var body = root.GetChild(i).GetComponent<SpaceObject>();
                Vector3 p = body.transform.position;
                var cell = new Vector3Int(Mathf.FloorToInt(p.x / cellSize), Mathf.FloorToInt(p.y / cellSize), Mathf.FloorToInt(p.z / cellSize));
                for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++) for (int z = -1; z <= 1; z++)
                    if (cells.TryGetValue(cell + new Vector3Int(x, y, z), out var neighbours))
                        foreach (var other in neighbours)
                            if (Vector3.Distance(p, other.transform.position) < (body.DiameterMeters + other.DiameterMeters) * 0.5f + SpiralBelt.Clearance - 1f)
                                throw new Exception("Spiral clearance failed: " + body.name + " / " + other.name);
                if (!cells.TryGetValue(cell, out var bucket)) cells[cell] = bucket = new List<SpaceObject>();
                bucket.Add(body);
                float radius = new Vector2(p.x, p.z).magnitude;
                if (radius <= previousRadius) throw new Exception("Spiral radius must increase at " + body.name);
                previousRadius = radius;
                if (i >= 3)
                {
                    float theta = (radius - 1500f) / SpiralBelt.RadialGrowth;
                    if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(Mathf.Sin(theta), Mathf.Cos(theta)) * radius) > 5f)
                        throw new Exception("Body is not on the spiral: " + body.name);
                }
            }
            Require(true, "all 10000 bodies have at least 200 m clearance, increasing radii and spiral positions");
            Require(spiral.OuterRadius < 250000f && spiral.LastAngle > Mathf.PI * 8, "spiral has multiple turns within the camera range");
        }

        private static void Capture(string path)
        {
            if (path.Contains("asteroid-") || path.Contains("dock-")
                || Array.IndexOf(Environment.GetCommandLineArgs(), "-spaceMinerOffscreenCheck") >= 0)
            {
                // Hidden test windows may have no presented backbuffer; render asset QA offscreen.
                Camera camera = Camera.main;
                RenderTexture previousTarget = camera.targetTexture;
                RenderTexture previousActive = RenderTexture.active;
                var target = RenderTexture.GetTemporary(1440,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
                var renderedImage = new Texture2D(1440,900,TextureFormat.RGB24,false);
                try
                {
                    camera.targetTexture = target;
                    camera.Render();
                    RenderTexture.active = target;
                    renderedImage.ReadPixels(new Rect(0,0,1440,900),0,0);
                    renderedImage.Apply();
                    Color[] samples = renderedImage.GetPixels();
                    int visible = 0;
                    for (int i = 0; i < samples.Length; i+=37)
                        if (samples[i].maxColorComponent > 0.025f) visible++;
                    if (visible < 300) throw new Exception("Offscreen asteroid rendering produced an empty image.");
                    File.WriteAllBytes(path,renderedImage.EncodeToPNG());
                }
                finally
                {
                    camera.targetTexture = previousTarget;
                    RenderTexture.active = previousActive;
                    RenderTexture.ReleaseTemporary(target);
                    Destroy(renderedImage);
                }
                return;
            }
            Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
            if (path == "Logs/million-overview.png")
            {
                int lit = 0;
                for (int y = image.height / 4; y < image.height * 3 / 4; y += 2)
                    for (int x = image.width * 3 / 10; x < image.width * 7 / 10; x += 2)
                        if (image.GetPixel(x, y).maxColorComponent > 0.1f) lit++;
                if (lit < 2000) throw new Exception("Million cloud overview is visually empty: " + lit + " lit samples");
            }
            File.WriteAllBytes(path, image.EncodeToPNG());
            Destroy(image);
        }

        private static void CaptureView(string path,Vector3 position,Vector3 target)
        {
            var camera=Camera.main; Vector3 previousPosition=camera.transform.position;
            Quaternion previousRotation=camera.transform.rotation;
            try { camera.transform.position=position; camera.transform.LookAt(target); Capture(path); }
            finally { camera.transform.SetPositionAndRotation(previousPosition,previousRotation); }
        }

        private IEnumerator CheckIntro(WaterScenario scenario)
        {
            IntroSequence intro = GetComponent<IntroSequence>();
            OrbitCamera camera = GetComponent<OrbitCamera>();
            if (!Guard(() =>
            {
                Require(intro != null && IntroSequence.IsPlaying && IntroSequence.BlocksGameplay, "intro blocks gameplay at startup");
                Require(intro.CueCount == 16 && intro.VoicedCueCount == 14, "original voice clips and two new scan text cues");
                Require(GetComponent<AudioSource>().isPlaying, "Mira voice starts playing");
            })) yield break;
            float simulationRate = scenario.SimulationRate;
            yield return new WaitForSecondsRealtime(1.1f);
            yield return new WaitForEndOfFrame();
            Capture("Logs/intro-opening.png");
            if (!Guard(() =>
            {
                Require(scenario.SimulationRate == simulationRate && !scenario.SourceAssigned, "intro preserves simulation state");
                intro.AdvancePlayback(intro.CueDuration - 1.2f);
            })) yield break;
            yield return new WaitForSecondsRealtime(1.1f);
            yield return new WaitForEndOfFrame();
            Capture("Logs/intro-mira.png");
            if (!Guard(() =>
            {
                intro.Skip();
                Require(!IntroSequence.IsPlaying && IntroSequence.BlocksGameplay, "skip consumes completion frame so Escape cannot quit game");
                Require(!GetComponent<AudioSource>().isPlaying, "skip stops speech immediately");
                Require(camera.Distance == 140f && camera.Pivot == Vector3.zero, "skip restores gameplay camera");
            })) yield break;
            yield return null;
            if (!Guard(() =>
            {
                Require(!IntroSequence.BlocksGameplay, "gameplay resumes on next frame");
                intro.PlayIntro();
                for (int step = 0; IntroSequence.IsPlaying && intro.CueIndex < 4 && step < 20; step++) intro.AdvancePlayback(intro.CueDuration + 0.01f);
            })) yield break;
            yield return new WaitForSecondsRealtime(2.8f);
            yield return new WaitForEndOfFrame();
            Capture("Logs/intro-ship.png");
            if (!Guard(() =>
            {
                for (int step = 0; IntroSequence.IsPlaying && intro.CueIndex < 7 && step < 20; step++) intro.AdvancePlayback(intro.CueDuration + 0.01f);
            })) yield break;
            yield return new WaitForSecondsRealtime(2.8f);
            yield return new WaitForEndOfFrame();
            Capture("Logs/intro-belt.png");
            if (!Guard(() =>
            {
                intro.AdvancePlayback(1000f);
                Require(!IntroSequence.IsPlaying && !GetComponent<AudioSource>().isPlaying, "timed completion enters gameplay and stops speech");
                intro.Skip();
                Require(camera.Distance == 140f, "repeated skip leaves gameplay camera stable");
            })) yield break;
            yield return null;
            Guard(() => Require(!IntroSequence.BlocksGameplay && scenario.SimulationRate == simulationRate, "timed completion releases gameplay without changing speed"));
        }

        private IEnumerator CheckWaterScenario(WaterScenario scenario, OrbitCamera camera)
        {
            DroneAgent drone = scenario.Worker;
            AsteroidResource source = GameObject.Find("A-01").GetComponent<AsteroidResource>();
            bool prepared = Guard(() =>
            {
                Require(scenario.Drones.Length == 10, "ten starting drones");
                var station=scenario.GetComponent<StationVisual>();
                Require(station.DroneDock.childCount >= 13, "eight charging bays and two maintenance bays on a common dock");
                for(int i=0;i<scenario.Drones.Length;i++) {
                    Require(Vector3.Distance(scenario.Drones[i].transform.position,station.Berth(i).position)<0.01f,
                        "parked drone belongs to its berth " + i);
                    Require(Quaternion.Angle(scenario.Drones[i].transform.rotation,station.Berth(i).rotation)<0.1f,
                        "parked drone faces out of its berth " + i);
                }
                int operational = 0;
                foreach (DroneAgent unit in scenario.Drones) if (unit.IsOperational) operational++;
                Require(operational == 2 && drone.IsReady, "two functional drones, worker ready");
                DroneAgent waiting = scenario.Drones[1];
                Require(waiting.NeedsInitialCharge && waiting.Phase == DronePhase.WaitingForPower
                    && waiting.BatteryKwh == 0 && waiting.FuelLiters == 0 && !waiting.IsReady, "drone 02 waits for power and fuel");
                Require(source.KnownWaterPercent == 80 && source.UnknownPercent == 20, "water known, other composition hidden");
                AsteroidResource rock = scenario.Asteroids[3];
                Require(rock.KnownWaterPercent == 0 && rock.UnknownPercent == 100, "unscanned composition remains unknown");
                Require(!scenario.AssignTankOrder(rock) && drone.IsReady && scenario.WaterLiters == 20, "unconfirmed source rejects order without spending water");
                Require(scenario.AssignTankOrder(source) && drone.Phase == DronePhase.Launching, "tank order leaves charging berth first");
                Require(!scenario.AssignTankOrder(source), "busy worker rejects duplicate order");
                Until(scenario,()=>drone.Phase==DronePhase.Outbound,3000);
                scenario.Advance(60);
                Require(drone.Speed > 0 && drone.FuelLiters < 10 && drone.BatteryKwh < 8, "acceleration consumes both fuel and battery");
                Require(drone.ArrivalSeconds > 0 && drone.TargetDistance > 0 && drone.PhaseProgress > 0, "flight status advances with ETA");
                camera.Focus(drone.Info);
            });
            if (!prepared) yield break;
            yield return new WaitForEndOfFrame();
            Capture("Logs/scenario-flight.png");
            if (!Guard(() =>
            {
                Until(scenario, () => drone.Phase == DronePhase.Docking, 3000);
                Require(drone.CargoKg == 0, "no mining before docking");
                scenario.Advance(100);
                drone.GetComponent<MiningDroneVisual>().RefreshPose();
                Require(drone.GetComponent<MiningDroneVisual>().ClampDeployed, "clamp locks before drill deployment");
            })) yield break;
            yield return new WaitForEndOfFrame();
            Capture("Logs/scenario-docking.png");
            if (!Guard(() =>
            {
                Until(scenario, () => drone.Phase == DronePhase.Mining, 3000);
                Require(drone.Speed == 0 && drone.TargetDistance == 0, "arrival brakes to rest");
                Require(Vector3.Distance(drone.transform.position, drone.SurfacePoint + drone.SurfaceNormal * 2.4f) < 0.01f, "drone stands at surface working distance");
                scenario.Advance(200);
                Require(drone.CargoKg > 19 && drone.CargoKg < 21, "mining fills cargo at configured rate");
                Require(Mathf.Abs(source.InitialRawKg - source.RemainingRawKg - drone.CargoKg) < 0.1f, "mined cargo removed from deposit");
                Require(scenario.WaterLiters == 20, "cargo is not credited before delivery");
                Require(drone.MiningSecondsRemaining > 0 && drone.PhaseProgress > 0, "mining progress and remaining time");
                var visual = drone.GetComponent<MiningDroneVisual>();
                visual.RefreshPose();
                Require(visual.IceSprayActive && visual.HatchOpen && visual.ChunkVisible, "ice spray and arm cargo transfer active");
                Require(visual.VisibleCargoPieces>0 && visual.VisibleCargoPieces<24,"visible cargo hold fills with mining inventory");
                Require(source.GetComponent<MeshCollider>().sharedMesh==source.GetComponent<AsteroidGenerator>().BakedMeshes[0],
                    "working collider matches detailed visible asteroid surface");
                for(int i=0;i<3;i++) {
                    var shoe=drone.transform.Find("Mining Drone Geometry/Clamp shoe " + i);
                    var surface=source.GetComponent<Collider>();
                    Require(surface.Raycast(new Ray(shoe.position-shoe.forward, shoe.forward),out RaycastHit hit,2f)
                        && Vector3.Dot(shoe.position-hit.point,hit.normal)>0.055f,"clamp shoe stays outside asteroid surface " + i);
                }
                var bit = drone.transform.Find("Mining Drone Geometry/Mining Drill");
                Require(Vector3.Distance(bit.position + drone.transform.forward * 0.55f, visual.ContactPoint) < 0.04f, "deployed drill touches collider surface");
                camera.ResetView(); camera.Focus(drone.Info);
                float heading = Mathf.Atan2(drone.transform.forward.x,drone.transform.forward.z)*Mathf.Rad2Deg;
                camera.Orbit(new Vector2(heading+24f+85f,12f));
            })) yield break;
            yield return new WaitForEndOfFrame();
            Capture("Logs/scenario-mining.png");
            CaptureView("Logs/dock-cargo.png",drone.transform.TransformPoint(new Vector3(3,3,-4)),
                drone.transform.TransformPoint(new Vector3(0,0.6f,-0.15f)));
            if (!Guard(() =>
            {
                Until(scenario, () => drone.Phase == DronePhase.Undocking, 3000);
                float cargo = drone.CargoKg;
                scenario.Advance(65);
                drone.GetComponent<MiningDroneVisual>().RefreshPose();
                Require(!drone.GetComponent<MiningDroneVisual>().ClampDeployed && !drone.GetComponent<MiningDroneVisual>().IceSprayActive, "tools retract before retreat");
                Until(scenario, () => drone.Phase == DronePhase.Turning, 3000);
                Quaternion rotation = drone.transform.rotation;
                scenario.Advance(60);
                Require(Quaternion.Angle(rotation,drone.transform.rotation)>1f && drone.CargoKg==cargo, "smooth turn retains cargo");
                Until(scenario, () => drone.Phase == DronePhase.Unloading, 3000);
                Require(Vector3.Distance(drone.transform.position, drone.TankDockPoint) < 0.01f && drone.Speed == 0, "loaded drone returns to tank inlet");
                Require(Vector3.Distance(drone.CargoOutletPoint,drone.TankInlet)<0.01f
                    && Quaternion.Angle(drone.transform.rotation,drone.TankDockRotation)<0.1f,"cargo outlet physically mates to tank receiver");
                Require(scenario.Deliveries == 0 && scenario.WaterLiters == 20, "water waits for processing at dock");
                scenario.Advance(100);
                var unloadVisual=drone.GetComponent<MiningDroneVisual>(); unloadVisual.RefreshPose();
                Require(unloadVisual.TankCoupled && unloadVisual.HatchOpen && unloadVisual.VisibleCargoPieces<24,
                    "coupled unloading drains visible cargo inside transfer channel");
                camera.ResetView(); camera.Focus(drone.Info); camera.Orbit(new Vector2(5,8)); camera.Zoom(-3);
            })) yield break;
            yield return new WaitForEndOfFrame();
            Capture("Logs/scenario-unloading.png");
            CaptureView("Logs/dock-tank-coupling.png",drone.transform.TransformPoint(new Vector3(3.5f,3.3f,1.5f)),
                (drone.transform.position+drone.TankInlet)*0.5f);
            if (!Guard(() =>
            {
                Until(scenario, () => drone.Phase == DronePhase.Servicing, 3000);
                Require(scenario.Deliveries == 1 && scenario.WaterLiters > 20 && scenario.WaterLiters < 59
                    && drone.Phase == DronePhase.Servicing, "energy-limited partial first delivery enters station tank");
                Require(scenario.Mining.CompletedTrips == 1 && drone.BatteryKwh >= drone.EnergyReserveKwh
                    && drone.FuelLiters >= drone.FuelReserveLiters, "productive parked sortie earns knowledge and preserves reserves");
                Require(Vector3.Distance(drone.transform.position,drone.HomePosition)<0.01f
                    && Quaternion.Angle(drone.transform.rotation,drone.HomeRotation)<0.1f,"charging starts only after reverse parking");
                float water = scenario.WaterLiters, fuel = drone.FuelLiters, energy = drone.BatteryKwh;
                scenario.Advance(2);
                Require(drone.FuelLiters > fuel && scenario.WaterLiters < water && drone.BatteryKwh > energy, "service draws ship water and charges battery");
                camera.Focus(source.Info);
            })) yield break;
            yield return new WaitForEndOfFrame();
            Capture("Logs/scenario-asteroid.png");
            if (!Guard(() =>
            {
                Until(scenario, () => scenario.QuestComplete, 300000);
                Until(scenario, () => drone.IsReady, 3000);
                Require(drone.IsReady && !drone.HasTankOrder && scenario.Deliveries > 4, "tank order completes across multiple trips");
                Require(Mathf.Abs(scenario.WaterLiters - 200) < 0.001f, "tank capacity reached without overflow");
                float harvestedWater = (source.InitialRawKg - source.RemainingRawKg) * source.WaterFraction;
                float remaining = scenario.WaterLiters + drone.FuelLiters + drone.CargoWaterLiters;
                Require(Mathf.Abs(20 + 10 + harvestedWater - drone.BurnedWaterLiters - remaining) < 0.2f, "water conserved across mining, cargo, refuelling and thrust");
                Require(drone.FuelLiters >= 0 && drone.FuelLiters <= 10 && drone.BatteryKwh >= 0 && drone.BatteryKwh <= 8, "drone inventories stay within capacities");
                Require(!scenario.AssignTankOrder(source), "full tank rejects further harvesting");
                camera.ResetView();
                camera.Select(drone.Info);
            })) yield break;
            yield return new WaitForEndOfFrame();
            Capture("Logs/scenario-complete.png");
            Guard(() =>
            {
                scenario.ResetScenario(); PrepareScan(scenario);
                source.Mine(source.RemainingRawKg - 1);
                Require(scenario.AssignTankOrder(source), "small deposit can be assigned");
                Until(scenario, () => !drone.HasTankOrder && drone.IsReady, 25000);
                Require(!source.CanMineWater && scenario.Deliveries == 1 && !scenario.QuestComplete, "depleted source returns partial load and ends order");
                scenario.ResetScenario(); PrepareScan(scenario);
                scenario.AssignTankOrder(source);
                scenario.Advance(60);
                drone.ReturnToShip();
                Until(scenario, () => drone.IsReady, 25000);
                Require(!drone.HasTankOrder && Vector3.Distance(drone.transform.position, drone.HomePosition) < 0.01f, "cancelled flight safely returns to dock");
                Require(drone.CanCompleteTrip(source, out _), "cancelled drone is recharged for another order");
                scenario.ResetScenario(); PrepareScan(scenario);
                scenario.AssignTankOrder(source);
                Until(scenario, () => drone.Phase == DronePhase.Mining, 3000);
                scenario.Advance(50);
                drone.ReturnToShip();
                Require(drone.Phase == DronePhase.Undocking && drone.CargoKg > 0, "mining cancellation releases clamp with partial cargo");
                Until(scenario, () => drone.IsReady, 25000);
                Require(scenario.Deliveries == 1 && !drone.HasTankOrder, "cancelled mining delivers partial load and parks");
                Require(scenario.Mining.CompletedTrips == 0, "cancelled sorties do not farm experience");
                scenario.ResetScenario(); PrepareScan(scenario);
                camera.ResetView();
            });
        }

        private void Until(WaterScenario scenario, Func<bool> condition, float maximumSeconds)
        {
            for (float elapsed = 0; elapsed < maximumSeconds && !condition(); elapsed += 0.25f) scenario.Advance(0.25f);
            Require(condition(), "simulation reached expected state within time budget");
        }

        private bool Guard(Action action)
        {
            try { action(); return true; }
            catch (Exception error) { Fail(error.ToString()); return false; }
        }

        private static void PrepareScan(WaterScenario scenario)
        {
            scenario.Scanner.Advance(10); var interior=StationInteriorMode.Current; interior.Enter(true); interior.RestoreInterior(interior.Layout.Console.position+interior.Room.forward*-1.5f,false); scenario.Scanner.OpenConsole(); scenario.Scanner.Scan();
            scenario.Scanner.OpenVirtualView(); scenario.Scanner.SetDebugVisibility(true); scenario.Scanner.DismissDialogue();
        }

        private void Require(bool condition, string description)
        {
            if (!condition) throw new Exception("Check failed: " + description);
            checks++;
        }

        private void OnLog(string message, string stack, LogType type)
        {
            if (running && (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)) Fail(message + "\n" + stack);
        }

        private void Fail(string error)
        {
            running = false;
            File.WriteAllText("Logs/smoke-test-result.json", "{\"passed\":false}");
            File.WriteAllText("Logs/smoke-test-error.txt", error);
            Application.Quit(1);
        }

        private void OnDestroy() { Application.logMessageReceived -= OnLog; }
    }
}
