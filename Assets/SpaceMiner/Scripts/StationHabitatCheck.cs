using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceMiner
{
    public sealed class StationHabitatCheck : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private int checks;
        private bool failed;
        private string folder;
        private StationInteriorMode mode;
        private StationAirlock airlock;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-stationHabitatCheck") >= 0) SceneManager.sceneLoaded += Loaded;
        }
        static void Loaded(Scene scene, LoadSceneMode loadMode)
        {
            SceneManager.sceneLoaded -= Loaded;
            new GameObject("Habitat airlock and console check").AddComponent<StationHabitatCheck>();
        }
        private void Require(bool condition, string label)
        {
            checks++; if (!condition) throw new Exception("Habitat check: " + label);
        }
        private bool Guard(Action action)
        {
            try { action(); return true; }
            catch (Exception error) { failed = true; Debug.LogException(error); Application.Quit(1); return false; }
        }
        private bool Step(Vector2 walk, float seconds = .1f)
        {
            return Guard(() =>
            {
                mode.Walk(walk, seconds);
                airlock.Advance(seconds, mode.FeetPosition);
                Require(!(airlock.HabitatOpen > .001f && airlock.RingOpen > .001f), "interlocked doors never open together");
            });
        }
        private IEnumerator WaitFor(AirlockPhase phase)
        {
            for (int step = 0; step < 100 && airlock.Phase != phase; step++)
            { if (!Step(Vector2.zero)) yield break; yield return null; }
            Guard(() => Require(airlock.Phase == phase, "reach " + phase + " from " + airlock.Phase));
        }
        private IEnumerator Start()
        {
            folder = Path.GetFullPath("Logs/Habitat"); Directory.CreateDirectory(folder);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-batchmode") < 0) Screen.SetResolution(1280, 800, FullScreenMode.Windowed);
            yield return new WaitForSecondsRealtime(1);
            mode = FindFirstObjectByType<StationInteriorMode>();
            var scenario = FindFirstObjectByType<WaterScenario>();
            var menu = FindFirstObjectByType<StartMenu>();
            if (!Guard(() => { Require(mode != null && scenario != null, "playable components"); menu.StartDemo(); FindFirstObjectByType<IntroSequence>().Skip(); scenario.SetSimulationRate(0); })) yield break;
            yield return null; yield return null;
            if (!Guard(() =>
            {
                Require(StationInteriorMode.IsInside, "start inside attached habitat"); airlock = mode.Airlock;
                Require(mode.Layout.RepairDrone.GetComponentInChildren<DroneAgent>() == null && scenario.Drones.Length == 10, "repair drone remains an inactive model");
                Require(!mode.OpenConsole(), "console requires physical proximity");
            })) yield break;
            yield return null; yield return null;
            Capture(Camera.main, "habitat-front.png");
            for (int step = 0; step < 16; step++) { mode.Walk(Vector2.up, .1f); yield return null; }
            if (!Guard(() =>
            {
                Require(mode.OpenConsole(), "use physical command desk");
                Vector3 before = mode.FeetPosition; mode.Walk(Vector2.up, 1); Require(mode.FeetPosition == before, "console holds walking input");
                scenario.Scanner.Advance(10); Require(scenario.Scanner.Scan(), "scan at physical console"); scenario.Scanner.DismissDialogue();
                Require(mode.AcceptWaterOrder(scenario.Asteroids[0]) && scenario.Worker.HasTankOrder, "console starts real water order");
                Require(!mode.AcceptWaterOrder(scenario.Asteroids[0]), "busy drone rejects duplicate order");
            })) yield break;
            yield return new WaitForSecondsRealtime(.3f);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, "console-ui.png"));
            yield return new WaitForSecondsRealtime(.3f);
            if (!Guard(() =>
            {
                FindFirstObjectByType<QuitMenu>().HandleEscape();
                Require(!mode.ConsoleOpen && !QuitMenu.IsOpen, "Escape closes desk without opening quit menu");
                scenario.ResetScenario(); mode.Exit(); Require(mode.Enter(), "reset habitat walk for airlock");
            })) yield break;
            for (int step = 0; step < 16; step++) { if (!Step(Vector2.down)) yield break; yield return null; }
            if (!Guard(() =>
            {
                Require(mode.Room.InverseTransformPoint(mode.FeetPosition).z > StationAirlock.HabitatDoorZ + .2f, "closed door blocks player");
                Require(airlock.Request(mode.FeetPosition) && airlock.TowardsRing, "request habitat-to-ring passage");
                Require(!airlock.Request(mode.FeetPosition), "second request cannot bypass an active cycle");
            })) yield break;
            yield return WaitFor(AirlockPhase.AwaitingEntry); if (failed) yield break;
            if (!Guard(() => Require(airlock.HabitatOpen == 1 && airlock.RingOpen == 0, "only entry door opens"))) yield break;
            for (int step = 0; step < 40 && mode.Room.InverseTransformPoint(mode.FeetPosition).z > -6.12f; step++)
            { if (!Step(Vector2.down)) yield break; yield return null; }
            if (!Guard(() => Require(mode.Room.InverseTransformPoint(mode.FeetPosition).z <= -6.12f, "enter airlock chamber"))) yield break;
            if (!Step(Vector2.up, .25f)) yield break;
            if (!Guard(() => Require(airlock.Phase == AirlockPhase.OpeningEntry || airlock.Phase == AirlockPhase.AwaitingEntry, "entry door reopens on player proximity"))) yield break;
            yield return WaitFor(AirlockPhase.AwaitingEntry); if (failed) yield break;
            for (int step = 0; step < 8; step++) { if (!Step(Vector2.down)) yield break; yield return null; }
            yield return WaitFor(AirlockPhase.Equalizing); if (failed) yield break;
            if (!Guard(() => Require(airlock.HabitatOpen == 0 && airlock.RingOpen == 0, "equalize only with both doors shut"))) yield break;
            yield return WaitFor(AirlockPhase.AwaitingExit); if (failed) yield break;
            if (!Guard(() => Require(airlock.HabitatOpen == 0 && airlock.RingOpen == 1, "exit door opens after entry closes"))) yield break;
            mode.Look(new Vector2(180, 0)); yield return null; yield return null;
            Capture(Camera.main, "airlock-to-ring.png"); mode.Look(new Vector2(-180, 0));
            for (int step = 0; step < 22; step++) { if (!Step(Vector2.down)) yield break; yield return null; }
            yield return WaitFor(AirlockPhase.Idle); if (failed) yield break;
            if (!Guard(() => Require(mode.Room.InverseTransformPoint(mode.FeetPosition).z < -11.45f, "walk through portal into ring"))) yield break;
            mode.Look(new Vector2(90, 0)); yield return null; yield return null;
            Capture(Camera.main, "ring-corridor.png");
            // A complete physical lap catches gaps, reversed floor faces and surviving solid ring segments.
            for (int point = 0; point <= 36; point++)
            {
                Vector3 target = mode.Layout.Ring.TransformPoint(Quaternion.Euler(0, 330 + point * 10, 0) * Vector3.forward * 12);
                for (int step = 0; step < 24; step++)
                {
                    Vector3 delta = target - mode.FeetPosition; delta.y = 0;
                    if (delta.magnitude < .25f) break;
                    Vector3 local = Quaternion.Inverse(Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0)) * delta.normalized;
                    if (!Step(new Vector2(local.x, local.z), .15f)) yield break;
                    yield return null;
                }
                if (!Guard(() =>
                {
                    Vector3 local = mode.Layout.Ring.InverseTransformPoint(mode.FeetPosition);
                    Vector3 delta = target - mode.FeetPosition; delta.y = 0;
                    Require(delta.magnitude < .5f, "complete ring waypoint " + point + "; feet " + local + "; remaining " + delta.magnitude);
                    Require(local.y > -.1f && local.y < .15f, "continuous floor around ring " + point);
                })) yield break;
            }
            mode.Look(new Vector2(-90, 0));
            if (!Guard(() => Require(airlock.Request(mode.FeetPosition) && !airlock.TowardsRing, "request reverse passage"))) yield break;
            yield return WaitFor(AirlockPhase.AwaitingEntry); if (failed) yield break;
            for (int step = 0; step < 18; step++) { if (!Step(Vector2.up)) yield break; yield return null; }
            yield return WaitFor(AirlockPhase.AwaitingExit); if (failed) yield break;
            for (int step = 0; step < 22; step++) { if (!Step(Vector2.up)) yield break; yield return null; }
            yield return WaitFor(AirlockPhase.Idle); if (failed) yield break;
            if (!Guard(() =>
            {
                Require(mode.Room.InverseTransformPoint(mode.FeetPosition).z > -4.25f, "return to habitat through both doors");
                mode.Exit(); Require(!StationInteriorMode.IsInside && !mode.ConsoleOpen, "exit restores commander controls");
            })) yield break;
            File.WriteAllText(Path.Combine(folder, "result.json"), "{\"passed\":true,\"checks\":" + checks + "}");
            Debug.Log("STATION HABITAT CHECK PASSED: " + checks + " checks; desk, inactive repair drone, interlock, anti-crush, full ring lap and reverse passage");
            Application.Quit(0);
        }
        private void Capture(Camera camera, string name)
        {
            var target = new RenderTexture(1280, 800, 24); var pixels = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            var previous = camera.targetTexture; var active = RenderTexture.active;
            var sky = FindFirstObjectByType<RuinedWorld>().SkyCamera; var previousSky = sky.targetTexture;
            try
            {
                sky.targetTexture = target; sky.Render(); camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1280, 800), 0, 0); pixels.Apply(); File.WriteAllBytes(Path.Combine(folder, name), pixels.EncodeToPNG());
            }
            finally { sky.targetTexture = previousSky; camera.targetTexture = previous; RenderTexture.active = active; target.Release(); Destroy(target); Destroy(pixels); }
        }
#endif
    }
}
