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
            public int checks, vertices, triangles, bones, continuationVertices, continuationTriangles;
            public string unityVersion;
            public string[] pictures;
            public string scope = "Actual selected R03 native mirrored RIGHT arm with a narrow sleeve continuation and no torso, normalized skeletal weights, source-derived PBR textures and bounded geometry; actual posed cuff join and authored proximal sleeve endpoint beyond viewport; inside/outside, intro and real modal visibility; bounded idle/walk/look controller, pause and reduced motion; camera mask and instance cleanup while shared assets survive. Screenshots use the real framebuffer. Wall-depth picture includes a declared temporary foreground plane. Console contact has a separate stationConsoleTouchCheck. No actual tool, manual comfort, FPS or headset acceptance.";
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
            catch (Exception exception)
            {
                if (!failed)
                {
                    failed = true; File.WriteAllText(Folder + "/error.txt", exception.ToString()); Debug.LogException(exception);
                    StartCoroutine(CaptureFailureAndQuit());
                }
                return false;
            }
        }
        private IEnumerator CaptureFailureAndQuit()
        {
            yield return new WaitForSecondsRealtime(.10f);
            string path = Path.GetFullPath(Folder + "/failure-current-run.png"); File.Delete(path);
            ScreenCapture.CaptureScreenshot(path);
            File.WriteAllText(Folder + "/failure-current-run.txt", DateTime.UtcNow.ToString("O") + "\nUnity " + Application.unityVersion + "\nChecks " + checks);
            yield return new WaitForSecondsRealtime(.45f); Application.Quit(1);
        }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.2f); yield return new WaitForEndOfFrame();
            string path = Path.GetFullPath(Folder + "/" + name + ".png"); File.Delete(path); ScreenCapture.CaptureScreenshot(path);
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
            var renderers = hands.ModelRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Require(renderers.Length == 2, "native mirrored right R03 arm plus one narrow authored sleeve continuation");
            Require(hands.ModelRoot.GetComponentsInChildren<MeshRenderer>(true).Length == 0,
                "view contains only articulated arm clothing without an additional body shell");
            var renderer = OperatorArmSleeveAcceptance.OriginalArm(hands); var mesh = renderer.sharedMesh;
            Require(mesh != null && hands.VertexCount == mesh.vertexCount && hands.VertexCount > 5000 && hands.VertexCount < 70000,
                "actual imported gameplay arm with bounded vertex count");
            Require(mesh.triangles.Length / 3 > 10000 && mesh.triangles.Length / 3 <= 55000,
                "optimized R03 derivative remains within 55k triangle budget");
            Require(renderer.name.IndexOf("R03", StringComparison.OrdinalIgnoreCase) >= 0
                || mesh.name.IndexOf("R03", StringComparison.OrdinalIgnoreCase) >= 0
                || hands.ModelRoot.name.IndexOf("R03", StringComparison.OrdinalIgnoreCase) >= 0,
                "selected R03 source identity retained rather than unapproved R04");
            Require(mesh.bounds.size.magnitude > .55f && mesh.bounds.size.magnitude < 1.1f,
                "arm has human forearm, glove and partial upper arm dimensions");
            Require(hands.ModelRoot.GetComponentsInChildren<Collider>(true).Length == 0, "view arm has no physics colliders");
            Require(renderer.shadowCastingMode == ShadowCastingMode.Off && !renderer.receiveShadows,
                "view arm does not cast or receive scene shadows");
            Require(renderer.sharedMaterials.Length == 1, "one batched PBR arm material");
            Require(hands.IsRightHand && renderer.transform.lossyScale.x > 0 && renderer.transform.lossyScale.y > 0 && renderer.transform.lossyScale.z > 0,
                "native right-hand asset uses positive transforms rather than negative-scale skinning");
            var material = renderer.sharedMaterials[0];
            var continuation = hands.SleeveContinuation;
            Require(continuation != null && continuation != renderer && continuation.sharedMesh != null
                && continuation.sharedMesh.vertexCount > 500 && continuation.sharedMesh.vertexCount < 12000
                && continuation.sharedMesh.triangles.Length / 3 < 7000,
                "small real clothing continuation retains the original R03 geometry budget");
            Require(continuation.sharedMesh.vertexCount == 4928 && continuation.sharedMesh.triangles.Length / 3 == 2494,
                "runtime narrow sleeve matches the authored revision without cached torso vertex buffers");
            var sleeveMaterial = continuation.sharedMaterial;
            Require(continuation.sharedMaterials.Length == 1 && sleeveMaterial != null && sleeveMaterial != material
                && sleeveMaterial.mainTexture == material.mainTexture && sleeveMaterial.shader.name == "Standard"
                && sleeveMaterial.GetFloat("_Mode") == 0 && sleeveMaterial.GetFloat("_Metallic") < .01f
                && sleeveMaterial.GetFloat("_Glossiness") > .1f && sleeveMaterial.GetFloat("_Glossiness") < .25f
                && sleeveMaterial.GetTexture("_BumpMap") == null && sleeveMaterial.GetTexture("_MetallicGlossMap") == null
                && !sleeveMaterial.IsKeywordEnabled("_NORMALMAP") && !sleeveMaterial.IsKeywordEnabled("_METALLICGLOSSMAP")
                && sleeveMaterial.GetFloat("_SpecularHighlights") < .01f && sleeveMaterial.IsKeywordEnabled("_SPECULARHIGHLIGHTS_OFF"),
                "narrow sleeve uses the original cloth atlas with its own opaque matte fabric material");
            Require(continuation.bones.Length == 2 && hands.SleeveShoulder != null
                && continuation.bones[0].name == "UpperArm" && continuation.bones[1] == hands.SleeveShoulder,
                "continuation joins the real upper arm to a proximal sleeve anchor");
            Require(continuation.sharedMesh.bounds.size.x < .36f,
                "sleeve remains arm-width without a broad chest or torso volume");
            var sleeveWeights = continuation.sharedMesh.boneWeights;
            bool validSleeveWeights = sleeveWeights.Length == continuation.sharedMesh.vertexCount;
            foreach (BoneWeight w in sleeveWeights)
                validSleeveWeights &= !float.IsNaN(w.weight0 + w.weight1 + w.weight2 + w.weight3)
                    && Mathf.Abs(w.weight0 + w.weight1 + w.weight2 + w.weight3 - 1) < .001f
                    && w.weight0 >= 0 && w.weight1 >= 0 && w.weight2 >= 0 && w.weight3 >= 0
                    && w.weight2 + w.weight3 < .00001f
                    && (w.weight0 == 0 || w.boneIndex0 >= 0 && w.boneIndex0 < 2)
                    && (w.weight1 == 0 || w.boneIndex1 >= 0 && w.boneIndex1 < 2);
            Require(validSleeveWeights, "every continuation vertex has finite normalized two-bone skin weights");
            Require(Vector3.Distance(hands.SleeveShoulder.position, hands.BodyShoulderPosition) < .003f,
                "real proximal sleeve follows its lower-right view anchor");
            var continuity = OperatorArmSleeveAcceptance.Measure(hands, source);
            File.WriteAllText(Folder + "/sleeve-metrics.json", JsonUtility.ToJson(continuity, true));
            Debug.Log("HANDS SLEEVE METRICS: " + JsonUtility.ToJson(continuity));
            Require(continuity.joinVertices == 128 && continuity.maximumJoinGap < .020f,
                "posed cloth overlaps the actual R03 cuff without a detached joint");
            Require(continuity.proximalEndVertices >= 32 && continuity.proximalMarkerToMeshGap < .010f && continuity.proximalEndOutsideView,
                "narrow sleeve continues beyond the view instead of ending in a floating cuff");
            Require(material.mainTexture != null && material.mainTexture.width == 2048 && material.mainTexture.height == 2048,
                "source-derived 2048 square R03 basecolour map");
            Require(material.GetTexture("_BumpMap") != null && material.GetTexture("_MetallicGlossMap") != null,
                "authored normal and metallic smoothness maps retained");
            Require(renderer.bones.Length >= 17 && renderer.bones.Length <= 32 && mesh.bindposes.Length == renderer.bones.Length,
                "bounded arm, wrist and articulated finger skeleton with bind poses");
            foreach (string name in new[] { "UpperArm", "Forearm", "Wrist", "Index01", "Index02", "Index03", "Middle01", "Ring01", "Little01", "Thumb01" })
                Require(Array.Exists(renderer.bones, bone => bone != null && bone.name == name), "real anatomical bone / " + name);
            int thumbIndex = Array.FindIndex(renderer.bones, bone => bone != null && bone.name == "Thumb01");
            int littleIndex = Array.FindIndex(renderer.bones, bone => bone != null && bone.name == "Little01");
            float thumbX = mesh.bindposes[thumbIndex].inverse.MultiplyPoint3x4(Vector3.zero).x;
            float littleX = mesh.bindposes[littleIndex].inverse.MultiplyPoint3x4(Vector3.zero).x;
            Require(thumbX < littleX - .04f, "actual mirrored right-hand bind anatomy places thumb opposite little finger");
            var weights = mesh.boneWeights;
            Require(weights.Length == mesh.vertexCount, "every arm vertex has skeletal skin weights");
            foreach (var weight in weights)
            {
                float sum = weight.weight0 + weight.weight1 + weight.weight2 + weight.weight3;
                if (float.IsNaN(sum) || Mathf.Abs(sum - 1) > .001f || weight.weight0 < 0 || weight.weight1 < 0 || weight.weight2 < 0 || weight.weight3 < 0)
                    throw new Exception("Non-normalized or negative arm skin weight");
                if (weight.weight0 > 0 && weight.boneIndex0 >= renderer.bones.Length
                    || weight.weight1 > 0 && weight.boneIndex1 >= renderer.bones.Length
                    || weight.weight2 > 0 && weight.boneIndex2 >= renderer.bones.Length
                    || weight.weight3 > 0 && weight.boneIndex3 >= renderer.bones.Length)
                    throw new Exception("Arm skin weight references missing bone");
            }
            Require(true, "finite normalized skin weights reference existing rig bones");
            foreach (string finger in new[] { "Index", "Middle", "Ring" })
            {
                int influenced = 0;
                foreach (var weight in weights)
                {
                    if (weight.weight0 > .1f && renderer.bones[weight.boneIndex0].name.StartsWith(finger, StringComparison.Ordinal)
                        || weight.weight1 > .1f && renderer.bones[weight.boneIndex1].name.StartsWith(finger, StringComparison.Ordinal)
                        || weight.weight2 > .1f && renderer.bones[weight.boneIndex2].name.StartsWith(finger, StringComparison.Ordinal)
                        || weight.weight3 > .1f && renderer.bones[weight.boneIndex3].name.StartsWith(finger, StringComparison.Ordinal)) influenced++;
                }
                Require(influenced > 40, "touch finger actually deforms weighted model vertices / " + finger);
            }
            foreach (var p in mesh.vertices)
                if (float.IsNaN(p.x) || float.IsInfinity(p.x) || float.IsNaN(p.y) || float.IsInfinity(p.y) || float.IsNaN(p.z) || float.IsInfinity(p.z))
                    throw new Exception("Invalid imported arm vertex");
            Require(mesh.vertexCount == hands.VertexCount, "finite imported R03 vertices");
            Require(hands.ToolMount != null && hands.ToolMount.childCount == 0, "future visual tool mount exists without pretending to have a working tool");
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
            var posed = new Mesh(); renderer.BakeMesh(posed);
            foreach (Vector3 vertex in posed.vertices)
            {
                Vector3 pixel = hands.ViewCamera.WorldToViewportPoint(transform.TransformPoint(vertex));
                if (pixel.z <= 0 || pixel.y < 0 || pixel.y > 1 || pixel.x < 0 || pixel.x > 1) continue;
                minX = Mathf.Min(minX, pixel.x); maxY = Mathf.Max(maxY, pixel.y); visible++;
            }
            Destroy(posed);
            Require(visible > 60 && minX > .48f && maxY < .65f,
                "posed RIGHT R03 glove is visibly framed below and right of the crosshair");
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
            foreach (string previousPicture in Directory.GetFiles(Folder, "*.png", SearchOption.TopDirectoryOnly)) File.Delete(previousPicture);
            File.Delete(Folder + "/failure-current-run.png"); File.Delete(Folder + "/failure-current-run.txt"); File.Delete(Folder + "/sleeve-metrics.json");
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
                planeMaterial = new Material(OperatorArmSleeveAcceptance.OriginalArm(hands).sharedMaterials[0])
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
            int vertices = hands.VertexCount; Camera overlay = hands.ViewCamera;
            var armRenderer = OperatorArmSleeveAcceptance.OriginalArm(hands);
            Mesh sharedMesh = armRenderer.sharedMesh; int triangles = sharedMesh.triangles.Length / 3, bones = armRenderer.bones.Length;
            Mesh sharedContinuation = hands.SleeveContinuation.sharedMesh;
            int continuationVertices = hands.SleeveContinuation.sharedMesh.vertexCount;
            int continuationTriangles = hands.SleeveContinuation.sharedMesh.triangles.Length / 3;
            Material sharedMaterial = armRenderer.sharedMaterials[0]; Texture sharedTexture = sharedMaterial.mainTexture;
            GameObject armInstance = hands.ModelRoot.gameObject;
            Destroy(hands); yield return null; yield return null;
            if (!Guard(() =>
            {
                Require(overlay == null && armInstance == null && (source.cullingMask & (1 << 31)) != 0,
                    "destroy frees private arm and camera instances and restores original source mask");
                Require(sharedMesh != null && sharedContinuation != null && sharedMaterial != null && sharedTexture != null,
                    "destroying view instance preserves imported shared R03 assets");
                Require(frustumWarnings == 0, "no implicit mouse/projection frustum warnings across the complete hand scenario");
                File.WriteAllText(Folder + "/result.json", JsonUtility.ToJson(new Report { passed = true, checks = checks, vertices = vertices,
                    triangles = triangles, bones = bones, unityVersion = Application.unityVersion,
                    continuationVertices = continuationVertices, continuationTriangles = continuationTriangles,
                    pictures = new[] { "01-idle-hand-1600.png", "02-walk-look-hand.png", "03-terminal-no-hand.png", "04-roomplan-no-hand.png", "05-declared-close-wall-fixture.png", "06-outside-no-hand.png" } }, true));
                Debug.Log("FIRST PERSON HANDS CHECK PASSED: " + checks); Application.Quit();
            })) yield break;
        }
    }
}
#endif
