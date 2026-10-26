#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    public sealed class StationCameraVerticalCheck : MonoBehaviour
    {
        private const string Folder = "Logs/CameraVertical";
        private WaterScenario scenario;
        private StationOperations operations;
        private StationInteriorMode interior;
        private OrbitCamera orbit;
        private int checks;
        private bool failed;
        [Serializable] private sealed class Report
        {
            public bool passed;
            public int checks;
            public string scope = "Real outside camera controller held-Space path: world-Y ascent/descent, unchanged orbit/view, equal Shift descent speed, frame-step integration and zoom/settings scaling. Simulation pause stays unchanged; manual translation releases actual drone follow. Real inside/settings/Mira/rendered-search guards and invalid inputs preserve camera state. Native before/after framebuffer screenshots. Programmatic input/controller checks; no native OS key injection, headset or manual comfort acceptance. Interior jump is checked separately.";
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        { if (Array.IndexOf(Environment.GetCommandLineArgs(), "-stationCameraVerticalCheck") >= 0) new GameObject("Outside vertical camera acceptance").AddComponent<StationCameraVerticalCheck>(); }
        private void Require(bool condition, string label)
        { if (!condition) throw new Exception("CAMERA VERTICAL: " + label); checks++; Debug.Log("CAMERA VERTICAL OK: " + label); }
        private bool Guard(Action action)
        {
            try { action(); return true; }
            catch (Exception error) { failed = true; File.WriteAllText(Folder + "/error.txt", error.ToString()); Debug.LogException(error); Application.Quit(1); return false; }
        }
        private static bool Near(Vector3 a, Vector3 b, float epsilon = .002f) => (a - b).sqrMagnitude <= epsilon * epsilon;
        private string DomainSnapshot()
        {
            var saved = SaveGameStore.Capture(scenario, "Camera is not an economy command", ""); saved.savedAt = "";
            return JsonUtility.ToJson(saved);
        }
        private void Blocked(string label)
        {
            var pivot = orbit.Pivot; var position = orbit.transform.position; var following = orbit.FollowingTarget;
            Require(!orbit.CanUseOutsideTranslation && !orbit.ApplySpaceTranslation(true, false, 1) && !orbit.ApplySpaceTranslation(true, true, 1)
                && Near(orbit.Pivot, pivot) && Near(orbit.transform.position, position) && orbit.FollowingTarget == following, label);
        }
        private void CheckController()
        {
            Require(operations.Live && !StationInteriorMode.IsInside && orbit.CanUseOutsideTranslation, "real operative outside context");
            Require(SettingsStore.PathName.StartsWith(Path.GetFullPath(Folder), StringComparison.OrdinalIgnoreCase), "isolated settings path");
            foreach (var room in operations.RoomPlans) Require(room.SavePath.StartsWith(Path.GetFullPath(Folder), StringComparison.OrdinalIgnoreCase), "isolated room plan / " + room.RoomId);
            float priorSpeed = SettingsStore.Current.Controls.CameraSpeed;
            try
            {
                SettingsStore.Current.Controls.CameraSpeed = 1;
                string before = DomainSnapshot();
                orbit.ResetView(); orbit.Orbit(new Vector2(77, -60));
                Require(Mathf.Abs(orbit.transform.forward.y) > .5f, "actual camera deliberately tilted away from horizontal");
                Vector3 pivot = orbit.Pivot, eye = orbit.transform.position; Quaternion rotation = orbit.transform.rotation; float distance = orbit.Distance;
                Require(orbit.ApplySpaceTranslation(true, false, 1), "held Space ascends through actual controller path");
                Vector3 ascent = orbit.Pivot - pivot;
                Require(ascent.y > 0 && Mathf.Abs(ascent.x) < .00001f && Mathf.Abs(ascent.z) < .00001f, "ascent uses world-Y independently of camera pitch and yaw");
                Require(Near(orbit.transform.position - eye, ascent) && orbit.Distance == distance && Quaternion.Angle(rotation, orbit.transform.rotation) < .001f,
                    "whole orbit translates without zoom or rotation drift");
                Require(orbit.ApplySpaceTranslation(true, true, 1) && Near(orbit.Pivot, pivot) && Near(orbit.transform.position, eye),
                    "Shift+Space descends at the same speed rather than adding a triple boost");
                Require(!orbit.ApplySpaceTranslation(false, true, 1) && Near(orbit.Pivot, pivot), "Shift alone is not vertical movement");
                orbit.ResetView(); for (int frame = 0; frame < 60; frame++) orbit.ApplySpaceTranslation(true, false, 1f / 60);
                Vector3 at60 = orbit.Pivot;
                orbit.ResetView(); for (int frame = 0; frame < 144; frame++) orbit.ApplySpaceTranslation(true, false, 1f / 144);
                Require(Near(orbit.Pivot, at60), "one second at60 and144 controller steps yields the same displacement");
                orbit.ResetView(); orbit.ApplySpaceTranslation(true, false, 1); float nearDistance = orbit.Distance, nearTravel = orbit.Pivot.y;
                orbit.ResetView(); orbit.Zoom(-10); float farDistance = orbit.Distance; orbit.ApplySpaceTranslation(true, false, 1);
                Require(Mathf.Abs(orbit.Pivot.y / nearTravel - farDistance / nearDistance) < .001f, "zoom-dependent speed remains proportional above the near speed floor");
                SettingsStore.Current.Controls.CameraSpeed = 2; orbit.ResetView(); orbit.ApplySpaceTranslation(true, false, 1);
                Require(Mathf.Abs(orbit.Pivot.y / nearTravel - 2) < .001f, "actual camera speed preference scales vertical movement");
                SettingsStore.Current.Controls.CameraSpeed = 1; orbit.ResetView(); orbit.Zoom(1000); orbit.ApplySpaceTranslation(true, false, .5f);
                Require(Mathf.Abs(orbit.Pivot.y - 1) < .001f, "near zoom retains a usable2m-per-second speed floor");
                orbit.ResetView(); Require(orbit.FollowDrone(scenario.Worker), "actual drone following starts");
                Require(!orbit.ApplySpaceTranslation(false, false, 1) && orbit.IsFollowing, "no held movement preserves follow");
                Vector3 target = scenario.Worker.transform.position;
                Require(orbit.ApplySpaceTranslation(true, false, .5f) && !orbit.IsFollowing && orbit.Pivot.y > target.y
                    && Mathf.Abs(orbit.Pivot.x - target.x) < .001f && Mathf.Abs(orbit.Pivot.z - target.z) < .001f,
                    "manual ascent releases follow from latest real target position");
                Require(orbit.Selected == scenario.Worker.Info, "manual translation preserves selected drone information");
                Require(orbit.FollowDrone(scenario.Worker), "follow available again for invalid-input guard");
                pivot = orbit.Pivot;
                Require(!orbit.MoveWorldVertical(float.NaN, 1) && !orbit.MoveWorldVertical(1, float.PositiveInfinity)
                    && !orbit.MoveWorldVertical(1, -1) && !orbit.MoveWorldVertical(1, 0) && !orbit.MoveWorldVertical(1, float.MaxValue)
                    && Near(orbit.Pivot, pivot) && orbit.IsFollowing, "invalid steps and overflow cannot corrupt pose or cancel follow");
                orbit.StopFollow(); scenario.SetSimulationRate(0); orbit.ResetView();
                Require(orbit.ApplySpaceTranslation(true, false, .25f) && scenario.SimulationRate == 0, "paused simulation permits camera movement without toggling pause");
                Require(DomainSnapshot() == before, "camera Space movement changes no saved resources, flight or simulation state");
            }
            finally { SettingsStore.Current.Controls.CameraSpeed = priorSpeed; }
        }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.2f); yield return new WaitForEndOfFrame();
            string path = Path.GetFullPath(Folder + "/" + name + ".png"); ScreenCapture.CaptureScreenshot(path); yield return new WaitForSecondsRealtime(.3f);
            Guard(() =>
            {
                var texture = new Texture2D(2, 2);
                try
                {
                    Require(File.Exists(path) && texture.LoadImage(File.ReadAllBytes(path)) && texture.width == Screen.width && texture.height == Screen.height, "real native screenshot / " + name);
                    int visible = 0; foreach (var pixel in texture.GetPixels32()) if (pixel.r > 45 || pixel.g > 45 || pixel.b > 45) visible++;
                    Require(visible > 1000, "actual scene and HUD rendered / " + name);
                }
                finally { Destroy(texture); }
            });
        }
        private IEnumerator Start()
        {
            Directory.CreateDirectory(Folder); File.Delete(Folder + "/result.json"); File.Delete(Folder + "/error.txt");
            yield return new WaitForSecondsRealtime(1);
            FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride = Path.GetFullPath(Folder + "/Saves");
            CinematicLibrary.PreferencePrefix = "SpaceMiner.Test.CameraVertical." + Guid.NewGuid().ToString("N");
            FindFirstObjectByType<StartMenu>().StartDemo(); FindFirstObjectByType<IntroSequence>().Skip(); yield return null; yield return null;
            if (!Guard(() =>
            {
                operations = StationOperations.Current; scenario = operations.Scenario; orbit = Camera.main.GetComponent<OrbitCamera>(); interior = StationInteriorMode.Current;
                scenario.SetSimulationRate(0); scenario.Scanner.DismissDialogue(); operations.ClosePanel(); interior.Exit();
            })) yield break;
            yield return null; yield return null;
            if (!Guard(CheckController)) yield break;
            if (!Guard(() =>
            {
                orbit.ShowHud = true; scenario.Scanner.DismissDialogue(); operations.ClosePanel(); interior.Exit();
                operations.SelectTacticalTab(TacticalOverviewTab.Objects);
            })) yield break;
            // The real GUI/backbuffer must exist before asking a named text control for focus.
            Screen.SetResolution(1440, 900, FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(.35f);
            Screen.SetResolution(1600, 1000, FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(.6f);
            operations.SetTacticalContactQuery(TacticalObjectFilter.Station); operations.FocusTacticalSearch(); yield return null; yield return null;
            if (!Guard(() => { Require(StationOperations.TacticalTextInputActive, "actual drawn search holds focus / HUD=" + orbit.ShowHud
                + " tab=" + operations.TacticalTab + " inside=" + StationInteriorMode.IsInside + " panel=" + operations.PanelOpen
                + " dialogue=" + (scenario.Scanner.Dialogue != null) + " keyboard=" + GUIUtility.keyboardControl);
                Blocked("Space/Shift+Space cannot move camera while typing in real search"); operations.ReleaseTacticalSearchFocus(); })) yield break;
            yield return null; yield return null;
            var settings = FindFirstObjectByType<SettingsMenu>(); settings.Open(); yield return null; yield return null;
            if (!Guard(() => Blocked("actual settings modal blocks outside Space in either direction"))) yield break;
            settings.Cancel(); yield return null; yield return null;
            scenario.Scanner.Say("KAMERA-ABNAHME", "Deklarierte Dialogfixture für die Eingabesicherheit."); yield return null; yield return null;
            if (!Guard(() => { Require(scenario.Scanner.DialogueCanRender, "real Mira acknowledgement visible"); Blocked("Mira acknowledgement blocks outside vertical input"); scenario.Scanner.DismissDialogue(); })) yield break;
            yield return null; yield return null;
            if (!Guard(() => { Require(interior.Enter(), "actual interior mode entered"); Blocked("inside Space belongs exclusively to interior movement"); })) yield break;
            interior.Exit(); yield return null; yield return null;
            if (!Guard(() => { orbit.enabled = false; Blocked("disabled outside controller rejects programmatic movement"); orbit.enabled = true; operations.FocusTacticalContact("station"); orbit.Orbit(new Vector2(-8, -28)); })) yield break;
            yield return Capture("01-before-world-vertical"); if (failed) yield break;
            if (!Guard(() => Require(orbit.ApplySpaceTranslation(true, false, .5f), "held ascent works after all UI closes"))) yield break;
            yield return Capture("02-after-world-ascent"); if (failed) yield break;
            if (!Guard(() =>
            {
                File.WriteAllText(Folder + "/result.json", JsonUtility.ToJson(new Report { passed = true, checks = checks }, true));
                Debug.Log("CAMERA VERTICAL CHECK PASSED: " + checks); Application.Quit(0);
            })) yield break;
        }
    }
}
#endif
