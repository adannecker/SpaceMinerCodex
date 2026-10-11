using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    // Explicit, isolated size probe in the real campaign scene. Never replaces a gameplay drone.
    public sealed class TripoDockPreview : MonoBehaviour
    {
        private const string Evidence = "Logs/TripoModels";
        private readonly SpaceMinerUi ui = new SpaceMinerUi();
        private StationInteriorMode interior;
        private OrbitCamera orbit;
        private WaterScenario scenario;
        private Camera cameraView;
        private Transform pair, cradle, drone;
        private Bounds cradleBounds, droneBounds;
        private float droneScale = 1, yaw = 205, pitch = 30, distance = 5.8f, deckHeight;
        private bool ready, separated, outlines = true, walking;
        private Material lineMaterial;
        private GameObject outlineRoot;
        private Vector2 sidebarScroll;
        private MaintenanceDroneShowcase showcase;
        private bool toolCloseup;
        [Serializable] private sealed class Measurement
        {
            public Vector3 cradleMetres, droneMetres;
            public float droneRelativeScale, centreDeckHeight, lateralOuterMargin, longitudinalOuterMargin;
            public bool archiveModelsLoaded, uniformScale, outerFootprintContained;
            public string limitation = "Outer bounding boxes only: rails, arms, charging connector and entry clearance require visual/mesh inspection.";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-tripoModelPreview") >= 0 || Array.IndexOf(args, "-tripoModelCheck") >= 0
                || Array.IndexOf(args, "-maintenanceShowcase") >= 0 || Array.IndexOf(args, "-maintenanceShowcaseCheck") >= 0)
                new GameObject("Tripo dock model size probe").AddComponent<TripoDockPreview>();
        }
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1);
            bool maintenanceMode = Array.IndexOf(Environment.GetCommandLineArgs(), "-maintenanceShowcase") >= 0
                || Array.IndexOf(Environment.GetCommandLineArgs(), "-maintenanceShowcaseCheck") >= 0;
            var saves = FindFirstObjectByType<SaveGameMenu>();
            if (saves != null) saves.StorageDirectoryOverride = Path.GetFullPath((maintenanceMode ? "Logs/MaintenanceShowcase" : Evidence) + "/Saves");
            CinematicLibrary.PreferencePrefix = maintenanceMode ? "SpaceMiner.MaintenanceShowcase." : "SpaceMiner.TripoProbe.";
            FindFirstObjectByType<StartMenu>().StartDemo();
            FindFirstObjectByType<IntroSequence>().Skip();
            yield return null; yield return null;
            try
            {
                interior = StationInteriorMode.Current; orbit = FindFirstObjectByType<OrbitCamera>();
                scenario = FindFirstObjectByType<WaterScenario>(); cameraView = Camera.main;
                scenario.Scanner.DismissDialogue(); scenario.SetSimulationRate(0);
                var root = new GameObject("Tripo comparison at exterior dock"); pair = root.transform;
                pair.SetParent(interior.Layout.transform, false);
                // Between the middle and right airlock: work on solid hull, with a clear approach.
                float angle = maintenanceMode ? 33.3f : 30;
                pair.localPosition = RingWalkthrough.At(maintenanceMode ? 58 : 57, angle, .04f);
                pair.localRotation = Quaternion.Euler(0, angle, 0);
                cradle = ImportedModel("ChargingCradle");
                drone = ImportedModel("MaintenanceDrone");
                Normalise(cradle, 2.65f); Normalise(drone, 2);
                cradleBounds = LocalBounds(cradle, pair);
                // Sample the actual central platform surface instead of using the outer rim's top.
                foreach (var filter in cradle.GetComponentsInChildren<MeshFilter>())
                {
                    var collider = filter.gameObject.AddComponent<MeshCollider>(); collider.sharedMesh = filter.sharedMesh;
                    collider.gameObject.layer = 30;
                }
                Physics.SyncTransforms();
                Vector3 from = pair.TransformPoint(new Vector3(0, cradleBounds.max.y + 2, 0));
                if (!Physics.Raycast(from, -pair.up, out RaycastHit hit, 5, 1 << 30)) throw new Exception("No central cradle floor detected");
                deckHeight = pair.InverseTransformPoint(hit.point).y + .015f;
                foreach (var collider in cradle.GetComponentsInChildren<Collider>()) Destroy(collider);
                foreach (var filter in cradle.GetComponentsInChildren<MeshFilter>()) filter.gameObject.layer = 0;
                lineMaterial = new Material(Shader.Find("Sprites/Default"));
                UpdatePlacement(); SetWalking(false);
                KeyLight("Tripo key light", new Vector3(-3, 6, 2), .8f);
                KeyLight("Tripo fill light", new Vector3(4, 3, -2), .35f);
                if (maintenanceMode)
                {
                    outlines = false; outlineRoot.SetActive(false);
                    showcase = root.AddComponent<MaintenanceDroneShowcase>();
                    showcase.Initialise(pair, drone, deckHeight);
                    scenario.Scanner.enabled = false;
                    yaw = 120; pitch = 22; distance = 8; ApplyCamera();
                }
                ready = true;
            }
            catch (Exception e) { Debug.LogException(e); Application.Quit(1); yield break; }
            yield return null; yield return null;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-tripoModelCheck") >= 0)
                yield return ValidateAndCapture();
        }
        private Transform ImportedModel(string resource)
        {
            var wrapper = new GameObject(resource + " / metre-scale placement").transform;
            wrapper.SetParent(pair, false);
            // Keep the FBX root's Blender axis conversion and scale intact inside our wrapper.
            Instantiate(Resources.Load<GameObject>("Tripo/" + resource), wrapper, false);
            return wrapper;
        }
        private void KeyLight(string label, Vector3 position, float intensity)
        {
            var light = new GameObject(label).AddComponent<Light>(); light.transform.SetParent(pair, false);
            light.transform.localPosition = position; light.transform.LookAt(pair.TransformPoint(Vector3.up));
            light.type = LightType.Spot; light.range = 18; light.spotAngle = 85; light.intensity = intensity;
            light.shadows = LightShadows.None;
        }
        internal static Bounds LocalBounds(Transform model, Transform relative)
        {
            Bounds result = default; bool first = true;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                Bounds b = renderer.localBounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 p = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    p = relative.InverseTransformPoint(renderer.transform.TransformPoint(p));
                    if (first) { result = new Bounds(p, Vector3.zero); first = false; } else result.Encapsulate(p);
                }
            }
            if (first || result.size.x <= 0 || result.size.z <= 0) throw new Exception("Invalid Tripo mesh bounds");
            return result;
        }
        private void Normalise(Transform model, float width)
        {
            model.localPosition = Vector3.zero; model.localRotation = Quaternion.identity; model.localScale = Vector3.one;
            model.localScale = Vector3.one * (width / LocalBounds(model, pair).size.x);
            var bounds = LocalBounds(model, pair);
            model.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }
        private void UpdatePlacement()
        {
            Normalise(drone, 2 * droneScale);
            drone.localPosition += new Vector3(separated ? 3.4f : 0, separated ? 0 : deckHeight, 0);
            cradleBounds = LocalBounds(cradle, pair); droneBounds = LocalBounds(drone, pair);
            DrawOutlines();
        }
        private void DrawOutlines()
        {
            if (outlineRoot != null) { outlineRoot.SetActive(false); Destroy(outlineRoot); }
            outlineRoot = new GameObject("Metre-scale outer footprints"); outlineRoot.transform.SetParent(pair, false);
            Footprint(cradleBounds, new Color(.25f, .85f, 1)); Footprint(droneBounds, new Color(1, .7f, .2f));
            outlineRoot.SetActive(outlines);
        }
        private void Footprint(Bounds b, Color color)
        {
            var line = new GameObject("Outer footprint").AddComponent<LineRenderer>(); line.transform.SetParent(outlineRoot.transform, false);
            line.useWorldSpace = false; line.loop = true; line.positionCount = 4; line.widthMultiplier = .018f;
            line.sharedMaterial = lineMaterial; line.startColor = line.endColor = color;
            float y = b.min.y + .035f;
            line.SetPositions(new[] { new Vector3(b.min.x, y, b.min.z), new Vector3(b.max.x, y, b.min.z), new Vector3(b.max.x, y, b.max.z), new Vector3(b.min.x, y, b.max.z) });
        }
        private void SetWalking(bool value)
        {
            walking = value;
            if (showcase != null) scenario.Scanner.enabled = value;
            orbit.ShowHud = value;
            if (value)
            {
                Vector3 feet = pair.TransformPoint(new Vector3(0, 0, 4.6f));
                Vector3 target = pair.TransformPoint(Vector3.up * 1.1f);
                Quaternion look = Quaternion.LookRotation(target - (feet + Vector3.up * 1.65f));
                cameraView.rect = new Rect(0, 0, 1, 1); interior.enabled = true;
                interior.RestoreInterior(feet, false, look.eulerAngles.y, 3);
            }
            else { interior.Exit(true); interior.enabled = false; orbit.enabled = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; ApplyCamera(); }
        }
        private void ApplyCamera()
        {
            cameraView.rect = new Rect(0, 0, Mathf.Clamp01((Screen.width - 365f) / Screen.width), 1);
            Vector3 focus = pair.TransformPoint(showcase != null ? (toolCloseup ? showcase.ToolFocus : new Vector3(0, 1.65f, -1.5f)) : new Vector3(separated ? 1.6f : 0, 1, 0));
            Quaternion toolView = showcase != null && toolCloseup ? showcase.ToolCameraRotation : Quaternion.identity;
            cameraView.transform.position = focus + pair.rotation * toolView * Quaternion.Euler(pitch, yaw, 0) * Vector3.back * distance;
            cameraView.transform.LookAt(focus); cameraView.fieldOfView = 50; cameraView.nearClipPlane = .04f;
        }
        private void Update()
        {
            if (!ready || StartMenu.IsOpen || SettingsMenu.IsOpen || SaveGameMenu.IsOpen) return;
            if (showcase != null && showcase.IsChecking) { ApplyCamera(); return; }
            if (Input.GetKeyDown(KeyCode.F8)) SetWalking(!walking);
            if (walking || SettingsMenu.BlocksInput) return;
            if (Input.GetMouseButton(1)) { yaw += Input.GetAxisRaw("Mouse X") * 3; pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * 2, 2, 85); }
            if (Input.mousePosition.x < Screen.width - 365) distance = Mathf.Clamp(distance - Input.mouseScrollDelta.y * .45f, toolCloseup ? .65f : 3, 16);
            ApplyCamera();
        }
        private static string Size(Vector3 p) => p.x.ToString("0.00") + " × " + p.z.ToString("0.00") + " × " + p.y.ToString("0.00") + " m";
        private void OnGUI()
        {
            if (!ready || StartMenu.IsOpen || SettingsMenu.IsOpen || SaveGameMenu.IsOpen) return;
            ui.Configure(SettingsStore.Current.Accessibility);
            GUI.depth = -100;
            ui.Panel(new Rect(Screen.width - 355, 65, 340, Screen.height - 85));
            GUILayout.BeginArea(new Rect(Screen.width - 343, 77, 316, Screen.height - 109));
            sidebarScroll = GUILayout.BeginScrollView(sidebarScroll);
            GUILayout.Label(showcase != null ? "WARTUNGSDROHNE" : "DROHNE + LADEBUCHT", ui.Heading);
            if (showcase != null)
            {
                GUI.enabled = !showcase.IsChecking;
                showcase.DrawControls(ui);
                if (GUILayout.Button(toolCloseup ? "Gesamte Drohne zeigen" : "Laser im Detail ansehen", ui.Button)) SetToolCloseup(!toolCloseup);
                if (GUILayout.Button(walking ? "Modellansicht · F8" : "Begehen · F8", ui.Button)) SetWalking(!walking);
                GUILayout.Space(14);
                GUILayout.Label("Drohne: " + Size(droneBounds.size) + "\nBucht: " + Size(cradleBounds.size), ui.Small);
                GUILayout.Label(walking ? "WASD / Maus / Shift wie im Spiel. Tab gibt die Maus frei. F8 zurück." :
                    "Rechte Maustaste ziehen: drehen\nMausrad: Abstand\nF11: Vollbild", ui.Small);
                GUILayout.EndScrollView(); GUILayout.EndArea(); return;
            }
            GUILayout.Label("Größenprobe am Außendock\nBreite × Tiefe × Höhe", ui.Small);
            GUILayout.Space(14);
            GUILayout.Label("Ladebucht\n" + Size(cradleBounds.size), ui.Text);
            GUILayout.Label("Drohne mit ausgestellten Armen\n" + Size(droneBounds.size), ui.Text);
            GUILayout.Label("Drohnenmaßstab " + Mathf.RoundToInt(droneScale * 100) + " %", ui.Small);
            GUI.enabled = showcase == null;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("− 10 %", ui.Button)) { droneScale = Mathf.Max(.5f, droneScale - .1f); UpdatePlacement(); }
            if (GUILayout.Button("+ 10 %", ui.Button)) { droneScale = Mathf.Min(1.6f, droneScale + .1f); UpdatePlacement(); }
            GUILayout.EndHorizontal();
            if (GUILayout.Button(separated ? "In Ladebucht stellen" : "Nebeneinander zeigen", ui.Button)) { separated = !separated; ComparisonCamera(); UpdatePlacement(); }
            GUI.enabled = true;
            if (GUILayout.Button(walking ? "Modellansicht · F8" : "Begehen · F8", ui.Button)) SetWalking(!walking);
            if (GUILayout.Button(outlines ? "Maßrahmen aus" : "Maßrahmen an", ui.Button)) { outlines = !outlines; outlineRoot.SetActive(outlines); }
            if (showcase == null && GUILayout.Button("Ausgangsmaße", ui.Button)) { droneScale = 1; separated = false; UpdatePlacement(); }
            GUILayout.Space(12);
            GUILayout.Label("Die Bucht ist auf 2,65 m Außenbreite skaliert. Die Drohne auf 2 m Gesamtbreite einschließlich Arme. Proportionen bleiben erhalten.", ui.Small);
            GUILayout.Label("Maßrahmen zeigen Außenmaße. Freie Stellfläche, Führungsschienen und Ladekontakt bitte gemeinsam beurteilen. Arme noch nicht animiert; vorderer Buchtrand geschlossen.", ui.Small);
            GUILayout.Label(walking ? "WASD / Maus / Shift wie im Spiel. Tab gibt die Maus frei. F8 zurück." : "Rechte Maustaste ziehen: drehen\nMausrad: Abstand\nF11: Vollbild", ui.Small);
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
        private Measurement Measurements() => new Measurement {
            cradleMetres = cradleBounds.size, droneMetres = droneBounds.size, droneRelativeScale = droneScale,
            centreDeckHeight = deckHeight, lateralOuterMargin = (cradleBounds.size.x - droneBounds.size.x) / 2,
            longitudinalOuterMargin = (cradleBounds.size.z - droneBounds.size.z) / 2,
            archiveModelsLoaded = true, uniformScale = drone.localScale.x == drone.localScale.y && drone.localScale.y == drone.localScale.z,
            outerFootprintContained = !separated && droneBounds.min.x >= cradleBounds.min.x && droneBounds.max.x <= cradleBounds.max.x
                && droneBounds.min.z >= cradleBounds.min.z && droneBounds.max.z <= cradleBounds.max.z };
        private void ComparisonCamera()
        {
            yaw = separated ? 180 : 205; pitch = separated ? 58 : 30; distance = separated ? 7.3f : 5.8f;
        }
        internal void SetToolCloseup(bool value)
        {
            toolCloseup = value; yaw = value ? 60 : 120; pitch = value ? 17 : 22; distance = value ? 1.85f : 8;
            if (walking) SetWalking(false);
            ApplyCamera();
        }
        private IEnumerator Capture(string file)
        {
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.GetFullPath(Evidence + "/" + file));
            yield return new WaitForSecondsRealtime(.6f);
        }
        private IEnumerator ValidateAndCapture()
        {
            Directory.CreateDirectory(Evidence);
            var measurement = Measurements();
            if (Mathf.Abs(cradleBounds.size.x - 2.65f) > .002f || Mathf.Abs(droneBounds.size.x - 2) > .002f || !measurement.uniformScale
                || cradleBounds.size.y > 1 || cradleBounds.size.z < 3 || droneBounds.size.y > 1.9f || deckHeight > .4f
                || !measurement.outerFootprintContained || !SettingsStore.PathName.Contains("TripoModels-settings"))
            { Debug.LogError("Tripo scale measurement failed"); Application.Quit(1); yield break; }
            File.WriteAllText(Evidence + "/measurements.json", JsonUtility.ToJson(measurement, true));
            yield return Capture("01-parked.png");
            yaw = 180; pitch = 82; ApplyCamera(); yield return Capture("02-top.png");
            separated = true; ComparisonCamera(); UpdatePlacement(); ApplyCamera(); yield return Capture("03-side-by-side.png");
            separated = false; ComparisonCamera(); droneScale = 1.3f; UpdatePlacement(); ApplyCamera();
            var larger = Measurements(); if (Mathf.Abs(larger.droneMetres.x - 2.6f) > .002f) { Application.Quit(1); yield break; }
            yield return Capture("04-larger-drone.png");
            droneScale = 1; UpdatePlacement(); SetWalking(true); yield return Capture("05-walking.png");
            Debug.Log("TRIPO MODEL CHECK PASSED: models, uniform metre-scale normalization, central deck, scale adjustment, separated and walking views");
            Application.Quit(0);
        }
        private void OnDestroy() { if (lineMaterial != null) Destroy(lineMaterial); ui.Dispose(); }
    }
}
