using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceMiner
{
    public sealed class StationInteriorCheck : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private int checks;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-stationInteriorCheck") >= 0) SceneManager.sceneLoaded += Loaded;
        }
        static void Loaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= Loaded;
            new GameObject("Interior and drone flight check").AddComponent<StationInteriorCheck>();
        }
        void Require(bool condition, string label)
        {
            checks++;
            if (!condition) throw new Exception("Interior/flight check: " + label);
        }
        bool Guard(Action action)
        {
            try { action(); return true; }
            catch (Exception error) { Debug.LogException(error); Application.Quit(1); return false; }
        }
        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1);
            var mode = FindFirstObjectByType<StationInteriorMode>();
            var scenario = FindFirstObjectByType<WaterScenario>();
            var menu = FindFirstObjectByType<StartMenu>();
            var orbit = FindFirstObjectByType<OrbitCamera>();
            var camera = Camera.main;
            string folder = Path.GetFullPath("Logs/Interior");
            Directory.CreateDirectory(folder);
            if (!Guard(() => { Require(mode != null && scenario != null && menu != null, "components available"); Require(!mode.Enter(), "menu prevents entry"); menu.StartDemo(); FindFirstObjectByType<IntroSequence>().Skip(); scenario.SetSimulationRate(0); })) yield break;
            yield return null;
            yield return null;
            mode.RestoreInterior(mode.Layout.Console.position-mode.Room.forward*1.5f,false); mode.OpenConsole(); scenario.Scanner.Advance(10); scenario.Scanner.Scan(); scenario.Scanner.DismissDialogue(); mode.Exit();
            Vector3 outside = camera.transform.position;
            float fov = camera.fieldOfView;
            if (!Guard(() => { Require(mode.Enter(), "enter empty habitat"); Require(!orbit.enabled && StationInteriorMode.IsInside, "only first-person camera controls pose"); })) yield break;
            yield return null;
            Capture(camera, Path.Combine(folder, "interior.png"));
            Vector3 spawn = mode.FeetPosition;
            if (!Guard(() => { mode.Walk(Vector2.up, .25f); Require(Vector3.Distance(spawn, mode.FeetPosition) > .3f, "walking moves player"); })) yield break;
            foreach (Vector2 direction in new[] { Vector2.up, Vector2.right, Vector2.down, Vector2.left })
            {
                for (int step = 0; step < 32; step++)
                {
                    if (!Guard(() => mode.Walk(direction, .1f, true))) yield break;
                    yield return null;
                }
                if (!Guard(() =>
                {
                    Vector3 local = mode.Room.InverseTransformPoint(mode.FeetPosition);
                    Require(Mathf.Abs(local.x) < 3.85f && Mathf.Abs(local.z) < 4.85f, "walls contain walker");
                    Require(local.y > -.1f && local.y < .15f, "floor contains walker");
                })) yield break;
            }
            var settings = FindFirstObjectByType<SettingsMenu>();
            Vector3 beforePause = mode.FeetPosition;
            if (!Guard(() => { settings.Open(); mode.Walk(Vector2.up, 1); Require(mode.FeetPosition == beforePause, "settings pauses walking"); })) yield break;
            yield return null;
            if (!Guard(() => { Require(Cursor.lockState == CursorLockMode.None, "menu releases cursor"); settings.Cancel(); })) yield break;
            yield return null;
            yield return null;
            if (!Guard(() =>
            {
                mode.Exit(); Require(orbit.enabled && !StationInteriorMode.IsInside, "return to commander mode");
                Require(Vector3.Distance(camera.transform.position, outside) < .001f && Mathf.Abs(camera.fieldOfView - fov) < .001f, "restore exterior view");
                Require(scenario.AssignTankOrder(scenario.Asteroids[0]), "start real water flight");
                scenario.Advance(121); Require(scenario.Worker.Phase == DronePhase.Outbound, "drone left berth");
                scenario.Advance(60); Require(scenario.Worker.Phase == DronePhase.Outbound, "flight away from charging dock");
                orbit.Focus(scenario.Worker.Info);
            })) yield break;
            yield return null;
            yield return null;
            Vector3 followOffset = camera.transform.position - scenario.Worker.transform.position;
            for (int sample = 0; sample < 12; sample++)
            {
                scenario.Advance(.5f);
                yield return null;
                yield return null;
                if (!Guard(() =>
                {
                    Require(Vector3.Distance(camera.transform.position - scenario.Worker.transform.position, followOffset) < .001f, "flight camera follows without stale pose");
                    Require(camera.nearClipPlane <= .1f && QualitySettings.shadowDistance <= 25, "near-flight detail and shadow coverage");
                    var floor = scenario.Worker.transform.Find("Mining Drone Geometry/Rear transfer floor");
                    Require(floor != null && floor.localPosition.y + floor.localScale.y * .5f > .42f, "transfer floor does not share chassis plane");
                    if (sample % 3 == 0) Capture(camera, Path.Combine(folder, "drone-flight-" + sample + ".png"));
                })) yield break;
            }
            float beforeFlight = scenario.Worker.TargetDistance;
            if (!Guard(() => { Require(mode.Enter(), "enter during active flight"); scenario.SetSimulationRate(1); })) yield break;
            yield return new WaitForSecondsRealtime(.5f);
            if (!Guard(() => { Require(scenario.Worker.TargetDistance < beforeFlight, "drone continues working inside"); menu.ReturnToStart(); })) yield break;
            yield return null;
            yield return null;
            if (!Guard(() => { Require(!StationInteriorMode.IsInside && orbit.enabled && Cursor.lockState == CursorLockMode.None, "return to menu cleans up first-person state"); })) yield break;
            File.WriteAllText(Path.Combine(folder, "result.json"), "{\"passed\":true,\"checks\":" + checks + "}");
            Debug.Log("STATION INTERIOR CHECK PASSED: " + checks + " checks; walking, collisions, menu, view restore, drone flight and working simulation");
            Application.Quit(0);
        }
        static void Capture(Camera camera, string path)
        {
            var target = new RenderTexture(960, 600, 24);
            var pixels = new Texture2D(960, 600, TextureFormat.RGB24, false);
            var previous = camera.targetTexture;
            var active = RenderTexture.active;
            var sky = FindFirstObjectByType<RuinedWorld>().SkyCamera;
            var previousSky = sky.targetTexture;
            try
            {
                sky.targetTexture = target; sky.Render();
                camera.targetTexture = target; camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 960, 600), 0, 0); pixels.Apply();
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                sky.targetTexture = previousSky; camera.targetTexture = previous; RenderTexture.active = active;
                target.Release(); Destroy(target); Destroy(pixels);
            }
        }
#endif
    }
}
