#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceMiner
{
    public sealed class StationFirstPersonHandsCheck : MonoBehaviour
    {
        private const string Folder = "Logs/StationHands";
        private int checks;
        private int frustumWarnings;
        private bool failed;
        private StationInteriorMode mode;
        private StationFirstPersonHands hands;
        private WaterScenario scenario;
        private Camera source;
        private void OnEnable() => Application.logMessageReceived += ObserveLog;
        private void OnDisable() => Application.logMessageReceived -= ObserveLog;
        private void ObserveLog(string message, string trace, LogType type)
        {
            if (message.IndexOf("Screen position out of view frustum", StringComparison.OrdinalIgnoreCase) >= 0) frustumWarnings++;
        }
        [Serializable] private sealed class Report
        {
            public bool passed;
            public int checks, vertices;
            public string[] pictures;
            public string scope = "Actual padded dark work-glove mesh with curled fingers and empty future grip; inside/outside, intro and real modal visibility; bounded idle/walk/look controller, pause and reduced motion; camera mask/resource cleanup. Screenshots use the real framebuffer. Wall-depth picture includes a declared temporary foreground plane. No actual tool, manual comfort, FPS or tool gameplay acceptance.";
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-stationHandsCheck") >= 0)
                new GameObject("First person hands acceptance").AddComponent<StationFirstPersonHandsCheck>();
        }
        private void Require(bool condition, string label)
        {
            checks++; if (!condition) throw new Exception("HANDS: " + label);
            Debug.Log("HANDS OK: " + label);
        }
        private bool Guard(Action action)
        {
            try { action(); return true; }
            catch (Exception exception) { failed = true; File.WriteAllText(Folder + "/error.txt", exception.ToString()); Debug.LogException(exception); Application.Quit(1); return false; }
        }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.2f); yield return new WaitForEndOfFrame();
            string path = Path.GetFullPath(Folder + "/" + name + ".png"); ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSecondsRealtime(.3f);
            Guard(() =>
            {
                var texture = new Texture2D(2, 2);
                try
                {
                    Require(File.Exists(path) && texture.LoadImage(File.ReadAllBytes(path)), "framebuffer screenshot readable / " + name);
                    Require(texture.width == Screen.width && texture.height == Screen.height, "native framebuffer dimensions / " + name);
                    int lit = 0; foreach (var pixel in texture.GetPixels32()) if (pixel.r > 45 || pixel.g > 45 || pixel.b > 45) lit++;
                    Require(lit > 1000, "real scene and glove/UI rendered / " + name);
                }
                finally { Destroy(texture); }
            });
        }
        private void CheckGeometry()
        {
            Require(hands.VertexCount > 1000 && hands.VertexCount < 3000, "bounded single work-glove mesh including cloth folds and curled fingers");
            Require(hands.ModelRoot.GetComponentsInChildren<MeshRenderer>(true).Length == 1, "single glove renderer");
            Require(hands.ModelRoot.GetComponentsInChildren<Collider>(true).Length == 0, "view glove has no physics colliders");
            var renderer = hands.ModelRoot.GetComponentInChildren<MeshRenderer>(true);
            Require(renderer.shadowCastingMode == ShadowCastingMode.Off && !renderer.receiveShadows, "view glove does not cast or receive scene shadows");
            Require(renderer.sharedMaterials.Length == 4, "finger textile, flexible folds, dark padding and suit sleeve materials");
            Require(renderer.sharedMaterials[2].color.maxColorComponent < .20f && renderer.sharedMaterials[0].color.maxColorComponent < .35f,
                "work glove has dark padding and grey textile rather than pale rigid armour");
            Require(renderer.sharedMaterials[0].mainTexture != null && renderer.sharedMaterials[0].mainTexture.width == 128
                && renderer.sharedMaterials[3].mainTexture == renderer.sharedMaterials[0].mainTexture, "small shared cloth texture supplies stable woven surface detail");
            var mesh = hands.ModelRoot.GetComponentInChildren<MeshFilter>(true).sharedMesh;
            foreach (var p in mesh.vertices) if (float.IsNaN(p.x) || float.IsInfinity(p.x) || float.IsNaN(p.y) || float.IsInfinity(p.y) || float.IsNaN(p.z) || float.IsInfinity(p.z)) throw new Exception("Invalid glove vertex");
            Require(mesh.vertexCount == hands.VertexCount, "finite glove vertices");
            Require(hands.ToolMount != null && hands.ToolMount.childCount == 0, "future visual tool mount exists without pretending to have a working tool");
            Vector3 grip=renderer.transform.InverseTransformPoint(hands.ToolMount.position);
            Vector3 gripAxis=renderer.transform.InverseTransformDirection(hands.ToolMount.up).normalized;
            float nearestGrip=float.PositiveInfinity;int surrounding=0;
            foreach(var vertex in mesh.vertices)
            {
                Vector3 offset=vertex-grip;float axial=Vector3.Dot(offset,gripAxis);if(Mathf.Abs(axial)>.058f)continue;
                float radial=(offset-gripAxis*axial).magnitude;nearestGrip=Mathf.Min(nearestGrip,radial);
                if(radial<.058f)surrounding++;
            }
            Require(nearestGrip>.014f && surrounding>200, "actual curled glove surrounds an empty grip cylinder without closing or faking a tool");
            Require(hands.ViewCamera.cullingMask == 1 << StationFirstPersonHands.ViewLayer && (source.cullingMask & hands.ViewCamera.cullingMask) == 0,
                "hand layer is exclusive to the overlay camera");
            Require(hands.ViewCamera.eventMask == 0, "render-only glove camera is excluded from implicit Unity mouse events");
            Require(hands.ViewCamera.clearFlags == CameraClearFlags.Depth && hands.ViewCamera.depth > source.depth, "hand has independent near depth after world rendering");
            Require(Mathf.Abs(hands.ViewCamera.fieldOfView - source.fieldOfView) < .001f && Mathf.Abs(hands.ViewCamera.aspect - source.aspect) < .001f,
                "hand projection follows actual camera FOV and aspect");
            Require(hands.ViewCamera.nearClipPlane < source.nearClipPlane && hands.ViewCamera.farClipPlane < 3,
                "short hand depth range leaves world projection intact");
            float minX = 1, maxY = 0; int visible = 0;
            var transform = renderer.transform;
            foreach (Vector3 vertex in mesh.vertices)
            {
                Vector3 pixel = hands.ViewCamera.WorldToViewportPoint(transform.TransformPoint(vertex));
                if (pixel.z <= 0 || pixel.y < 0 || pixel.y > 1 || pixel.x < 0 || pixel.x > 1) continue;
                minX = Mathf.Min(minX, pixel.x); maxY = Mathf.Max(maxY, pixel.y); visible++;
            }
            Require(visible > 60 && minX > .55f && maxY < .48f, "visible glove remains below/right of the crosshair");
        }
        private void CheckScreenRays(string label)
        {
            foreach (Camera camera in new[] { source, hands.ViewCamera })
            {
                Rect rect = camera.pixelRect;
                Require(rect.width > 0 && rect.height > 0 && camera.fieldOfView > 0 && camera.fieldOfView < 180,
                    label + " valid native camera viewport / " + camera.name);
                for (int y = 0; y < 5; y++) for (int x = 0; x < 7; x++)
                {
                    var point = new Vector3(Mathf.Lerp(rect.xMin + 1, rect.xMax - 1, x / 6f),
                        Mathf.Lerp(rect.yMin + 1, rect.yMax - 1, y / 4f), 0);
                    Ray ray = camera.ScreenPointToRay(point);
                    bool finite = true;
                    for (int axis = 0; axis < 3; axis++)
                        finite &= !float.IsNaN(ray.origin[axis]) && !float.IsInfinity(ray.origin[axis])
                            && !float.IsNaN(ray.direction[axis]) && !float.IsInfinity(ray.direction[axis]);
                    Require(finite && ray.direction.sqrMagnitude > .99f && ray.direction.sqrMagnitude < 1.01f
                        && Vector3.Dot(ray.direction, camera.transform.forward) > 0,
                        label + " finite forward screen ray / " + camera.name + " / " + x + "," + y);
                }
            }
            Require(hands.ViewCamera.eventMask == 0, label + " overlay cannot receive implicit mouse events");
            Require(frustumWarnings == 0, label + " no Unity screen-frustum warning during native viewport transition or ray grid");
        }
        private void CheckMotion()
        {
            hands.ReducedMotion = false; hands.MotionStrength = 1;
            for (int i = 0; i < 1000; i++)
            {
                hands.AdvanceMotion(1f / 120, new Vector2(Mathf.Sin(i) * 12, 20), new Vector2(Mathf.Sin(i * .13f) * 900, Mathf.Cos(i) * 900), true, false);
                if (hands.MotionOffset.magnitude > StationFirstPersonHands.MaximumMotionOffset + .00001f
                    || hands.MotionAngles.magnitude > StationFirstPersonHands.MaximumMotionAngle + .00001f) throw new Exception("Unbounded glove motion");
            }
            Require(hands.MotionOffset.magnitude > .0001f && hands.MotionAngles.magnitude > .1f, "walking and looking produce bounded visible motion");
            Vector3 position = hands.MotionOffset, angle = hands.MotionAngles;
            for (int i = 0; i < 100; i++) hands.AdvanceMotion(.02f, Vector2.one * 99, Vector2.one * 900, true, true);
            Require(position == hands.MotionOffset && angle == hands.MotionAngles, "paused hand pose does not advance or ease in the background");
            hands.ReducedMotion = true; hands.AdvanceMotion(.02f, Vector2.one, Vector2.one, false, false);
            Require(hands.MotionOffset == Vector3.zero && hands.MotionAngles == Vector3.zero, "reduced-motion seam removes sway and bob");
            hands.ReducedMotion = false; hands.MotionStrength = 0; hands.AdvanceMotion(.02f, Vector2.one, Vector2.one, false, false);
            Require(hands.MotionOffset == Vector3.zero, "zero motion strength remains static");
            hands.MotionStrength = 1; for (int i = 0; i < 120; i++) hands.AdvanceMotion(1f / 120, Vector2.zero, Vector2.zero, false, false);
            Require(hands.MotionOffset.magnitude > .0001f && hands.MotionOffset.magnitude < .002, "idle breathing is subtle");
        }
        private IEnumerator Start()
        {
            Directory.CreateDirectory(Folder); File.Delete(Folder + "/error.txt"); File.Delete(Folder + "/result.json");
            yield return new WaitForSecondsRealtime(1);
            if (!Guard(() =>
            {
                scenario = FindFirstObjectByType<WaterScenario>(); source = Camera.main; mode = StationInteriorMode.Current;
                Require(mode != null && mode.Hands != null, "actual interior installs first person component"); hands = mode.Hands;
                Require(!hands.IsVisible && !hands.ViewCamera.enabled, "hands hidden at start menu / intro");
                Require(hands.ViewCamera.eventMask == 0, "hidden render-only overlay also has no mouse event mask");
                FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride = Path.GetFullPath(Folder + "/Saves");
                CinematicLibrary.PreferencePrefix = "SpaceMiner.Test.Hands.";
                FindFirstObjectByType<StartMenu>().StartDemo(); FindFirstObjectByType<IntroSequence>().Skip();
            })) yield break;
            yield return null; yield return null;
            if (!Guard(() =>
            {
                scenario.Scanner.DismissDialogue(); mode.RestoreInterior(mode.Layout.transform.TransformPoint(new Vector3(0, .05f, 44)), false, mode.Layout.transform.eulerAngles.y, 0);
                scenario.SetSimulationRate(0);
            })) yield break;
            Screen.SetResolution(2560, 1440, FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(.5f);
            if (!Guard(() => CheckScreenRays("2560x1440 requested window"))) yield break;
            Screen.SetResolution(2560, 1440, FullScreenMode.FullScreenWindow); yield return new WaitForSecondsRealtime(.5f);
            if (!Guard(() => CheckScreenRays("native borderless fullscreen"))) yield break;
            Screen.SetResolution(1440, 900, FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(.25f);
            Screen.SetResolution(1600, 1000, FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(.5f);
            float near = source.nearClipPlane, far = source.farClipPlane;
            if (!Guard(() => { Require(hands.IsVisible && hands.ViewCamera.enabled, "glove visible during actual interior gameplay"); CheckGeometry(); CheckMotion(); })) yield break;
            yield return Capture("01-idle-hand-1600"); if (failed) yield break;
            if (!Guard(() => scenario.SetSimulationRate(1))) yield break;
            for (int i = 0; i < 24; i++) { mode.Walk(Vector2.right, .015f); mode.Look(new Vector2(.3f, .035f)); yield return null; }
            if (!Guard(() => { Require(hands.IsVisible, "glove follows real walking and mouse look"); scenario.SetSimulationRate(0); })) yield break;
            yield return Capture("02-walk-look-hand"); if (failed) yield break;
            if (!Guard(() =>
            {
                var terminal = StationOperations.Current.GetComponentInChildren<StationTerminal>(); StationOperations.Current.OpenTerminal(terminal);
            })) yield break;
            yield return null; yield return null;
            if (!Guard(() => Require(!hands.IsVisible && !hands.ViewCamera.enabled, "real station terminal hides hand and overlay camera"))) yield break;
            yield return Capture("03-terminal-no-hand"); if (failed) yield break;
            StationOperations.Current.ClosePanel(); yield return null; yield return null;
            var planner = FindFirstObjectByType<CommandRoomPlanner>();
            if (!Guard(() => Require(planner.Open(), "real room planner opens"))) yield break;
            yield return null; yield return null;
            if (!Guard(() => Require(!hands.IsVisible, "room planner hides the hand"))) yield break;
            yield return Capture("04-roomplan-no-hand"); if (failed) yield break;
            planner.Close(); yield return null; yield return null;
            var settings = FindFirstObjectByType<SettingsMenu>(); settings.Open(); yield return null; yield return null;
            if (!Guard(() => Require(!hands.IsVisible, "settings hide hand"))) yield break;
            settings.Cancel(); yield return null; yield return null;
            if (!Guard(() => Require(hands.IsVisible, "closing modals restores hand only after input guards release"))) yield break;
            GameObject plane = null; Material planeMaterial = null;
            if (!Guard(() =>
            {
                plane = GameObject.CreatePrimitive(PrimitiveType.Cube); plane.name = "Acceptance fixture / close foreground surface";
                Destroy(plane.GetComponent<Collider>()); plane.layer = 25;
                plane.transform.SetPositionAndRotation(source.transform.position + source.transform.forward * .13f, source.transform.rotation);
                plane.transform.localScale = new Vector3(.5f, .5f, .015f);
                // Clone a rendered glove material so shader stripping cannot remove the fixture's shader.
                planeMaterial = new Material(hands.ModelRoot.GetComponentInChildren<MeshRenderer>(true).sharedMaterials[0])
                    { name = "Acceptance fixture / foreground surface", color = new Color(.10f, .17f, .22f) };
                planeMaterial.SetColor("_EmissionColor", new Color(.04f, .07f, .09f));
                plane.GetComponent<Renderer>().sharedMaterial = planeMaterial;
                Require(hands.ViewCamera.clearFlags == CameraClearFlags.Depth && hands.IsVisible, "near world surface cannot depth-clip view glove");
            })) yield break;
            yield return Capture("05-declared-close-wall-fixture"); if (failed) yield break; Destroy(plane); Destroy(planeMaterial);
            mode.Exit(); yield return null; yield return null;
            if (!Guard(() => Require(!hands.IsVisible && !hands.ViewCamera.enabled, "outside mode has no floating hand"))) yield break;
            yield return Capture("06-outside-no-hand"); if (failed) yield break;
            mode.Enter(); yield return null; yield return null;
            if (!Guard(() =>
            {
                Require(hands.IsVisible, "returning inside restores view glove");
                Require(Mathf.Abs(source.nearClipPlane - near) < .0001f && Mathf.Abs(source.farClipPlane - far) < 1, "hand never changes world near/far settings");
                int mask = source.cullingMask; hands.enabled = false;
                Require(!hands.IsVisible && !hands.ViewCamera.enabled && (source.cullingMask & int.MaxValue) == (mask & int.MaxValue), "disable restores source mask without changing other layers");
                Require((source.cullingMask & (1 << 31)) != 0, "original layer31 bit restored on disable");
                hands.enabled = true;
            })) yield break;
            yield return null; yield return null;
            if (!Guard(() => Require(hands.IsVisible && (source.cullingMask & (1 << 31)) == 0, "reenable restores exclusive hand rendering"))) yield break;
            int vertices = hands.VertexCount; Camera overlay = hands.ViewCamera; Mesh mesh = hands.ModelRoot.GetComponentInChildren<MeshFilter>().sharedMesh;
            Texture cloth=hands.ModelRoot.GetComponentInChildren<MeshRenderer>().sharedMaterials[0].mainTexture;
            Destroy(hands); yield return null; yield return null;
            if (!Guard(() =>
            {
                Require(overlay == null && mesh == null && (source.cullingMask & (1 << 31)) != 0, "destroy frees private camera/mesh and restores original source mask");
                Require(cloth==null,"destroy frees the private shared cloth texture");
                Require(frustumWarnings == 0, "no implicit mouse/projection frustum warnings across the complete hand scenario");
                File.WriteAllText(Folder + "/result.json", JsonUtility.ToJson(new Report { passed = true, checks = checks, vertices = vertices,
                    pictures = new[] { "01-idle-hand-1600.png", "02-walk-look-hand.png", "03-terminal-no-hand.png", "04-roomplan-no-hand.png", "05-declared-close-wall-fixture.png", "06-outside-no-hand.png" } }, true));
                Debug.Log("FIRST PERSON HANDS CHECK PASSED: " + checks); Application.Quit();
            })) yield break;
        }
    }
}
#endif
