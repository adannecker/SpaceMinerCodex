#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SpaceMiner
{
    // Exercises the same activation controller as the native E key, against real
    // R04 consoles. No synthetic hand, screen, pose or successful callback fixture.
    public sealed class StationConsoleTouchCheck : MonoBehaviour
    {
        private const string Folder = "Logs/StationConsoleTouch";
        private readonly List<string> pictures = new List<string>();
        private int checks, contacts;
        private bool failed;
        private StationInteriorMode mode;
        private StationOperations operations;
        private StationFirstPersonHands hands;
        private WaterScenario scenario;
        private Camera camera;
        private void OnEnable() => Application.logMessageReceived += ObserveLog;
        private void OnDisable() => Application.logMessageReceived -= ObserveLog;
        private void ObserveLog(string text, string trace, LogType type)
        {
            if (type != LogType.Exception || failed) return;
            failed = true; Directory.CreateDirectory(Folder);
            File.WriteAllText(Folder + "/error.txt", text + "\n" + trace); Application.Quit(1);
        }

        [Serializable] private sealed class Report
        {
            public bool passed;
            public int checks, contacts, completedTouches, cancelledTouches;
            public string unityVersion;
            public string[] pictures;
            public string scope = "Actual native mirrored RIGHT R03 skinned arm with a narrow sleeve and no torso, six real metallic blue R04 consoles and physical ice-plant wall terminal; same controller entry as E; reach/contact/retract, three independent fingertip contacts, once-only modal opening, frozen walking, reachable range, wall occlusion, exterior/settings/Escape cancellation, moved/rotated/resized room-plan console and a real adjacent unreachable-console candidate. Escape uses the real QuitMenu dispatch controller. Isolated settings/saves/plans. Controller and framebuffer acceptance; no native OS keyboard, manual comfort, headset or performance acceptance.";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-stationConsoleTouchCheck") >= 0)
                new GameObject("R03 console touch acceptance").AddComponent<StationConsoleTouchCheck>();
        }
        private void Require(bool condition, string label)
        {
            checks++;
            if (!condition) throw new Exception("CONSOLE TOUCH: " + label);
            Debug.Log("CONSOLE TOUCH OK: " + label);
        }
        private bool Guard(Action action)
        {
            if (failed) return false;
            try { action(); return true; }
            catch (Exception error)
            {
                failed = true;
                File.WriteAllText(Folder + "/error.txt", error.ToString());
                Debug.LogException(error); Application.Quit(1); return false;
            }
        }
        private bool PanelOpen => mode.ConsoleOpen || operations.PanelOpen;
        private void ClosePanels() { mode.CloseConsole(); operations.ClosePanel(); }
        private void Approach(TouchConsoleVisual visual, float distance = .95f)
        {
            ClosePanels();
            mode.RestoreInterior(visual.transform.TransformPoint(new Vector3(0, .05f, -distance)), false,
                visual.transform.eulerAngles.y, 35);
            Physics.SyncTransforms();
        }
        private void TakePicture(string name)
        {
            pictures.Add(name + ".png");
            string path = Path.GetFullPath(Folder + "/" + name + ".png"); File.Delete(path);
            ScreenCapture.CaptureScreenshot(path);
        }
        private void CheckConsoleMaterials(TouchConsoleVisual visual)
        {
            Material[] materials = visual.GetComponentsInChildren<MeshRenderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials).Where(material => material != null).Distinct().ToArray();
            var surface = materials.FirstOrDefault(material => material.name == "TouchConsole_Surface");
            var display = materials.FirstOrDefault(material => material.name == "TouchConsole_Display");
            Require(surface != null && surface.mainTexture != null && surface.mainTexture.name == "TouchConsole_BlueAlbedo",
                "real R04 housing uses the dedicated metallic blue atlas / " + visual.transform.parent.name);
            Require(surface.GetTexture("_MetallicGlossMap") != null && surface.GetTexture("_MetallicGlossMap").name == "TouchConsole_MetalSmooth"
                && surface.GetTexture("_BumpMap") != null && surface.IsKeywordEnabled("_METALLICGLOSSMAP") && surface.IsKeywordEnabled("_NORMALMAP"),
                "real blue housing retains authored metal/smoothness and normal surface detail / " + visual.transform.parent.name);
            Require(display != null && display.color.b > display.color.r * 2 && display.color.b > display.color.g
                && display.IsKeywordEnabled("_EMISSION") && display.GetColor("_EmissionColor").b > display.GetColor("_EmissionColor").r
                && display.GetFloat("_GlossyReflections") < .01f && display.IsKeywordEnabled("_GLOSSYREFLECTIONS_OFF")
                && display.GetFloat("_SpecularHighlights") < .01f && display.IsKeywordEnabled("_SPECULARHIGHLIGHTS_OFF"),
                "touch glass remains cool blue without warm external glossy reflections / " + visual.transform.parent.name);
        }
        private void CheckContacts(StationConsoleTouchSurface surface, string label)
        {
            Require(hands.IsRightHand, "touch is performed with the right arm / " + label);
            Require(hands.ActiveTouchSurface == surface && hands.ActiveConsoleVisual == surface.ConsoleVisual,
                "same real console owns the touch / " + label);
            Require(hands.TouchPoints != null && hands.TouchPoints.Length == 3, "exactly three articulated fingertip points / " + label);
            Require(hands.ContactNormal.sqrMagnitude > .99f && hands.ContactNormal.sqrMagnitude < 1.01f,
                "finite unit touch normal / " + label);
            Require(surface.TryGetTouchPoint(camera, out var point, out var normal), "real glass remains reachable / " + label);
            Require(Vector3.Distance(point, hands.ContactPoint) < .08f && Vector3.Dot(normal, hands.ContactNormal) > .99f,
                "contact belongs to the actual authored screen plane / " + label);
            for (int i = 0; i < 3; i++)
            {
                Vector3 tip = hands.TouchPoints[i];
                float depth = Mathf.Abs(Vector3.Dot(tip - hands.ContactPoint, hands.ContactNormal));
                Require(!float.IsNaN(depth) && depth < .022f, "fingertip reaches glass within 22 mm / " + label + " / " + i);
                Require(Vector3.Distance(tip, hands.ContactPoint) < .15f, "fingertip stays on the chosen screen patch / " + label + " / " + i);
                for (int j = 0; j < i; j++)
                    Require(Vector3.Distance(tip, hands.TouchPoints[j]) > .008f,
                        "three distinct fingertip contacts / " + label + " / " + j + "," + i);
            }
            var skin = OperatorArmSleeveAcceptance.OriginalArm(hands);
            var baked = new Mesh();
            try
            {
                skin.BakeMesh(baked); Vector3[] vertices = baked.vertices;
                for (int finger = 0; finger < 3; finger++)
                {
                    float nearest = float.PositiveInfinity;
                    foreach (Vector3 vertex in vertices)
                        nearest = Mathf.Min(nearest, Vector3.Distance(skin.transform.TransformPoint(vertex), hands.TouchPoints[finger]));
                    Require(nearest < .018f, "actual posed glove surface follows fingertip marker within 18 mm / " + label + " / " + finger);
                }
            }
            finally { Destroy(baked); }
            var continuity = OperatorArmSleeveAcceptance.Measure(hands, camera);
            Require(continuity.joinVertices == 128 && continuity.maximumJoinGap < .020f,
                "actual narrow sleeve remains joined to R03 at touch / " + label);
            Require(hands.SleeveShoulder != null && Vector3.Distance(hands.SleeveShoulder.position, hands.BodyShoulderPosition) < .003f,
                "touch keeps proximal sleeve on its lower-right anchor / " + label);
            Require(continuity.proximalEndVertices >= 32 && continuity.proximalMarkerToMeshGap < .010f && continuity.proximalEndOutsideView,
                "narrow sleeve exits the frame with no floating sleeve end / " + label);
            contacts++;
        }
        private IEnumerator Activate(TouchConsoleVisual visual, string label, bool capture)
        {
            Approach(visual); yield return ActivateSurface(visual.TouchSurface, label, capture);
        }
        private IEnumerator ActivateSurface(StationConsoleTouchSurface surface, string label, bool capture,
            StationTerminal expectedTerminal = null)
        {
            yield return null; yield return null;
            int completed = hands.CompletedTouches;
            if (!Guard(() =>
            {
                Require(hands.IsVisible && !PanelOpen, "real arm visible before E / " + label);
                Require(mode.TryActivateConsole(), "E controller starts touch / " + label);
                Require(hands.TouchInProgress && !PanelOpen, "interface waits for real gesture / " + label);
                Require(!mode.TryActivateConsole(), "repeated E cannot start a second callback / " + label);
                Vector3 feet = mode.FeetPosition;
                mode.Walk(Vector2.one, .2f, true);
                Require(Vector3.Distance(feet, mode.FeetPosition) < .001f, "walking remains frozen without teleporting / " + label);
                float yaw = mode.ViewYaw, pitch = mode.ViewPitch;
                mode.Look(new Vector2(25, 15));
                Require(Mathf.Abs(Mathf.DeltaAngle(yaw, mode.ViewYaw)) < .001f && Mathf.Abs(pitch - mode.ViewPitch) < .001f,
                    "manual mouse look cannot tear the hand away during contact / " + label);
            })) yield break;
            bool reached = false, contact = false, retracted = false;
            float deadline = Time.realtimeSinceStartup + 5;
            while (hands.TouchInProgress && Time.realtimeSinceStartup < deadline)
            {
                string phase = hands.TouchPhase.ToString();
                reached |= phase == "Reach"; retracted |= phase == "Retract";
                if (phase == "Contact" && !contact)
                {
                    contact = true;
                    if (!Guard(() => { CheckContacts(surface, label); Require(!PanelOpen, "no modal replaces visible touch / " + label); })) yield break;
                    if (capture) { yield return new WaitForEndOfFrame(); TakePicture(label + "-contact"); }
                }
                yield return null;
            }
            if (!Guard(() =>
            {
                Require(!hands.TouchInProgress && reached && contact && retracted, "reach, contact and retract complete / " + label);
                Require(hands.CompletedTouches == completed + 1 && PanelOpen, "one completed gesture opens exactly one console / " + label);
                if (expectedTerminal != null)
                {
                    var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                    var actualPanel = typeof(StationOperations).GetField("panel", flags).GetValue(operations);
                    var actualTarget = (string)typeof(StationOperations).GetField("selected", flags).GetValue(operations);
                    Require(actualPanel is StationTerminalKind kind && kind == expectedTerminal.kind && actualTarget == expectedTerminal.target,
                        "real terminal opens its own production panel and target / " + label);
                }
            })) yield break;
            yield return null; yield return null;
            if (!Guard(() => Require(!hands.IsVisible && !hands.ViewCamera.enabled, "open console hides view arm / " + label))) yield break;
            if (capture) { yield return new WaitForEndOfFrame(); TakePicture(label + "-panel"); }
            ClosePanels(); yield return null; yield return null;
        }
        private IEnumerator Start()
        {
            Directory.CreateDirectory(Folder);
            File.Delete(Folder + "/error.txt"); File.Delete(Folder + "/result.json");
            foreach (string previousPicture in Directory.GetFiles(Folder, "*.png", SearchOption.TopDirectoryOnly)) File.Delete(previousPicture);
            yield return new WaitForSecondsRealtime(1);
            if (!Guard(() =>
            {
                mode = StationInteriorMode.Current; camera = Camera.main; operations = StationOperations.Current;
                Require(mode != null && mode.Hands != null && operations != null && operations.Live, "actual live station and hand controller installed");
                hands = mode.Hands; scenario = operations.Scenario;
                FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride = Path.GetFullPath(Folder + "/Saves");
                CinematicLibrary.PreferencePrefix = "SpaceMiner.Test.ConsoleTouch." + Guid.NewGuid() + ".";
                Require(Path.GetFullPath(SettingsStore.PathName).StartsWith(Path.GetFullPath(Folder), StringComparison.OrdinalIgnoreCase), "settings isolated from personal settings");
                foreach (var plan in operations.RoomPlans)
                {
                    Require(Path.GetFullPath(plan.SavePath).StartsWith(Path.GetFullPath(Folder), StringComparison.OrdinalIgnoreCase), "room plan isolated / " + plan.RoomId);
                    plan.ResetDefaults();
                }
                FindFirstObjectByType<StartMenu>().StartDemo(); FindFirstObjectByType<IntroSequence>().Skip();
            })) yield break;
            yield return null; yield return null;
            scenario.Scanner.DismissDialogue(); scenario.SetSimulationRate(0);
            Screen.SetResolution(1600, 1000, FullScreenMode.Windowed);
            yield return new WaitForSecondsRealtime(.4f);
            var consoles = mode.Layout.GetComponentsInChildren<TouchConsoleVisual>().OrderByDescending(item => item.Main).ToArray();
            if (!Guard(() => Require(consoles.Length == 6, "one main and five functional authored R04 consoles"))) yield break;
            if (!Guard(() => { foreach (var console in consoles) CheckConsoleMaterials(console); })) yield break;
            for (int index = 0; index < consoles.Length; index++)
            {
                yield return Activate(consoles[index], "0" + (index + 1) + "-" + (consoles[index].Main ? "main" : consoles[index].GetComponentInParent<CommandRoomBlock>().id), true);
                if (failed) yield break;
            }
            var plantTerminal = operations.IcePlantTerminal;
            if (!Guard(() =>
            {
                Require(plantTerminal != null && plantTerminal.kind == StationTerminalKind.Production && plantTerminal.target == "ice-plant",
                    "actual ice-plant production terminal installed");
                var stance = plantTerminal.transform.TransformPoint(new Vector3(0, .05f, -.60f));
                ClosePanels(); mode.RestoreInterior(stance, false, plantTerminal.transform.eulerAngles.y, 16);
                Require(Vector3.Distance(mode.FeetPosition, stance) < .01f, "ice-plant terminal stance belongs to the real maintenance aisle");
                Physics.SyncTransforms();
            })) yield break;
            yield return ActivateSurface(plantTerminal.GetComponent<StationConsoleTouchSurface>(), "08-ice-plant", true, plantTerminal);
            if (failed) yield break;

            var mainConsole = consoles.First(item => item.Main);
            var nearbyConsole = consoles.First(item => item.GetComponentInParent<CommandRoomBlock>().id == "planning");
            var nearbyBlock = nearbyConsole.GetComponentInParent<CommandRoomBlock>();
            var nearbyRoom = operations.RoomPlans.First(plan => plan.RoomId == nearbyBlock.roomId);
            var nearbyOriginal = nearbyRoom.Get(nearbyBlock.id); var packed = nearbyOriginal.Copy();
            packed.position = new Vector3(0, 0, 46); packed.yaw = 0;
            if (!Guard(() => Require(nearbyRoom.TrySet(packed, out string packedReason), "real room plan accepts adjacent console fixture / " + packedReason))) yield break;
            Approach(mainConsole); yield return null; yield return null;
            if (!Guard(() =>
            {
                var nearbyTerminal = operations.GetComponentsInChildren<StationTerminal>().First(item => item.kind == StationTerminalKind.Planning);
                Require(Vector3.Distance(nearbyTerminal.transform.position, mode.FeetPosition) < 2.4f && !hands.CanReachConsole(nearbyConsole.TouchSurface),
                    "adjacent real terminal is in prompt range but physically unreachable from the main-pult stance");
                Require(mode.TrySelectConsole(out var selectedTerminal, out var selectedSurface) && selectedTerminal == null && selectedSurface == mainConsole.TouchSurface,
                    "unreachable adjacent console does not shadow the reachable main pult");
            })) yield break;
            yield return Activate(mainConsole, "09-adjacent-console", true); if (failed) yield break;
            if (!Guard(() => Require(nearbyRoom.TrySet(nearbyOriginal, out string nearbyRestored), "adjacent console fixture restored / " + nearbyRestored))) yield break;

            var chosen = consoles.First(item => !item.Main);
            Approach(chosen, 2.5f); yield return null; yield return null;
            if (!Guard(() => Require(!mode.TryActivateConsole() && !hands.TouchInProgress && !PanelOpen, "out-of-reach E cannot stretch the arm or open a remote console"))) yield break;

            Approach(chosen); yield return null; yield return null;
            GameObject obstruction = null;
            if (!Guard(() =>
            {
                Require(chosen.TryGetTouchPoint(camera, out var point, out _), "unobstructed real screen reachable before wall fixture");
                obstruction = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstruction.name = "Acceptance fixture / console line-of-sight blocker"; obstruction.layer = 25;
                obstruction.transform.position = Vector3.Lerp(camera.transform.position, point, .5f);
                obstruction.transform.rotation = camera.transform.rotation; obstruction.transform.localScale = new Vector3(.65f, .65f, .08f);
                Physics.SyncTransforms();
                Require(!mode.TryActivateConsole() && !hands.TouchInProgress, "wall blocks console touch and callback");
            })) yield break;
            Destroy(obstruction); yield return null; yield return null;

            Approach(chosen); yield return null; yield return null;
            int cancelled = hands.CancelledTouches, completedBeforeCancel = hands.CompletedTouches;
            if (!Guard(() => Require(mode.TryActivateConsole(), "start cancellation via exterior transition"))) yield break;
            yield return null; mode.Exit(); yield return null; yield return null;
            if (!Guard(() => Require(!hands.TouchInProgress && !hands.IsVisible && !PanelOpen && hands.CancelledTouches == cancelled + 1,
                "V/exterior cancels gesture, hides hand and consumes callback"))) yield break;
            yield return new WaitForSecondsRealtime(1.5f);
            if (!Guard(() => Require(!PanelOpen && hands.CompletedTouches == completedBeforeCancel, "cancelled gesture never opens a deferred console"))) yield break;

            Approach(chosen); yield return null; yield return null;
            cancelled = hands.CancelledTouches; completedBeforeCancel = hands.CompletedTouches;
            if (!Guard(() => Require(mode.TryActivateConsole(), "start cancellation via real Escape dispatcher"))) yield break;
            yield return null;
            if (!Guard(() =>
            {
                FindFirstObjectByType<QuitMenu>().HandleEscape();
                Require(!QuitMenu.IsOpen && !hands.TouchInProgress && !PanelOpen && hands.CancelledTouches == cancelled + 1,
                    "Escape cancels gesture before QuitMenu can open a dialog");
            })) yield break;
            yield return new WaitForSecondsRealtime(1.1f);
            if (!Guard(() => Require(!QuitMenu.IsOpen && !PanelOpen && hands.CompletedTouches == completedBeforeCancel,
                "Escape leaves no quit dialog or deferred console callback"))) yield break;

            Approach(chosen); yield return null; yield return null;
            cancelled = hands.CancelledTouches;
            if (!Guard(() => Require(mode.TryActivateConsole(), "start cancellation via settings"))) yield break;
            FindFirstObjectByType<SettingsMenu>().Open(); yield return null; yield return null;
            if (!Guard(() => Require(!hands.TouchInProgress && !hands.IsVisible && !PanelOpen && hands.CancelledTouches == cancelled + 1,
                "settings cancels gesture while preserving menu ownership"))) yield break;
            FindFirstObjectByType<SettingsMenu>().Cancel(); yield return null; yield return null;
            yield return new WaitForSecondsRealtime(1.3f);
            if (!Guard(() => Require(!PanelOpen && hands.IsVisible, "closing settings restores idle arm without deferred action"))) yield break;

            Approach(chosen); yield return null; yield return null;
            cancelled = hands.CancelledTouches;
            if (!Guard(() => Require(mode.TryActivateConsole(), "start cancellation when hand component disables"))) yield break;
            hands.enabled = false; yield return null;
            if (!Guard(() => Require(!hands.TouchInProgress && !hands.IsVisible && !PanelOpen && hands.CancelledTouches == cancelled + 1,
                "component disable consumes pending callback and restores camera ownership"))) yield break;
            hands.enabled = true; yield return null; yield return null;

            Approach(chosen); yield return null; yield return null;
            cancelled = hands.CancelledTouches;
            if (!Guard(() => Require(mode.TryActivateConsole(), "start cancellation when physical target disappears"))) yield break;
            chosen.gameObject.SetActive(false); yield return null; yield return null;
            if (!Guard(() => Require(!hands.TouchInProgress && !PanelOpen && hands.CancelledTouches == cancelled + 1,
                "disabled physical screen cancels rather than opening a ghost console"))) yield break;
            chosen.gameObject.SetActive(true); yield return null; yield return null;

            var plannedConsole = consoles.First(item => item.Main);
            var block = plannedConsole.GetComponentInParent<CommandRoomBlock>();
            var room = operations.RoomPlans.First(plan => plan.RoomId == block.roomId);
            var original = room.Get(block.id); var edited = original.Copy();
            edited.yaw += 12; edited.position += new Vector3(.2f, 0, .2f); edited.size.z -= .2f;
            bool changed = room.TrySet(edited, out string reason);
            if (!changed) { edited.yaw = original.yaw - 12; changed = room.TrySet(edited, out reason); }
            if (!Guard(() => Require(changed, "real room planner accepts moved, resized and rotated console / " + reason))) yield break;
            yield return Activate(plannedConsole, "07-edited-room-plan", true); if (failed) yield break;
            if (!Guard(() => Require(room.TrySet(original, out string restored), "room-plan fixture restored / " + restored))) yield break;

            yield return new WaitForSecondsRealtime(.5f);
            if (!Guard(() =>
            {
                foreach (string picture in pictures)
                {
                    string path = Path.GetFullPath(Folder + "/" + picture);
                    var pixels = new Texture2D(2, 2);
                    try
                    {
                        Require(File.Exists(path) && pixels.LoadImage(File.ReadAllBytes(path)), "real framebuffer file / " + picture);
                        Require(pixels.width == Screen.width && pixels.height == Screen.height, "native framebuffer size / " + picture);
                        Require(pixels.GetPixels32().Count(pixel => pixel.r > 45 || pixel.g > 45 || pixel.b > 45) > 1000, "visible real framebuffer / " + picture);
                    }
                    finally { Destroy(pixels); }
                }
                File.WriteAllText(Folder + "/result.json", JsonUtility.ToJson(new Report {
                    passed = true, checks = checks, contacts = contacts, completedTouches = hands.CompletedTouches,
                    cancelledTouches = hands.CancelledTouches, unityVersion = Application.unityVersion, pictures = pictures.ToArray()
                }, true));
                Debug.Log("STATION CONSOLE TOUCH CHECK PASSED: " + checks); Application.Quit(0);
            })) yield break;
        }
    }
}
#endif
