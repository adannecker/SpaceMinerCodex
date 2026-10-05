using System;
using System.Collections;
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
            var scenario = FindFirstObjectByType<WaterScenario>();
            yield return null;
            yield return CheckIntro(scenario);
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
            if (!running) yield break;
            camera.Overview();
            yield return new WaitForEndOfFrame();
            Capture("Logs/prototype-overview.png");
            if (!running) yield break;
            camera.Zoom(-1000);
            yield return new WaitForEndOfFrame();
            Capture("Logs/prototype-far-zoom.png");
            if (!running) yield break;
            camera.Focus(GameObject.Find("Drone 1").GetComponent<SpaceObject>());
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
                var controller = GetComponent<OrbitCamera>();
                var objects = FindObjectsByType<SpaceObject>(FindObjectsSortMode.None);
                Require(objects.Length == 23, "12 asteroids, a ship and ten drones");
                var asteroids = GameObject.Find("Asteroids (1 unit = 1 metre)");
                Require(asteroids.transform.childCount == 12, "12 asteroids");
                foreach (Transform body in asteroids.transform)
                {
                    var generator = body.GetComponent<AsteroidGenerator>();
                    Require(generator != null && generator.Type != null, "configured generated asteroid " + body.name);
                    Require(body.GetComponent<LODGroup>().GetLODs().Length == 4, "four asteroid LODs");
                    Require(body.GetComponent<MeshCollider>().sharedMesh != null && body.GetComponent<SphereCollider>() == null, "irregular picking collider");
                    Require(body.GetComponent<Renderer>().sharedMaterial.shader.name == "SpaceMiner/Asteroid Surface", "asteroid PBR shader");
                }
                var largest = GameObject.Find("A-12 / Grossasteroid").GetComponent<SpaceObject>();
                Require(Mathf.Approximately(largest.transform.localScale.x, 5000), "5 km asteroid scale");
                var drone = GameObject.Find("Drone 1").GetComponent<SpaceObject>();
                Require(Mathf.Approximately(drone.GetComponentInChildren<Renderer>().bounds.size.x, 2), "2 m drone scale");
                controller.ResetView();
                Vector3 home = transform.position;
                Require(Mathf.Approximately(controller.Distance, 140), "home distance");
                controller.Zoom(1);
                Require(controller.Distance < 140, "zoom in");
                controller.Zoom(-1);
                Require(Mathf.Abs(controller.Distance - 140) < 0.01f, "zoom out");
                controller.Zoom(1000);
                Require(controller.Distance == OrbitCamera.MinimumDistance, "minimum zoom clamp");
                controller.Zoom(-1000);
                Require(controller.Distance == OrbitCamera.MaximumDistance, "maximum zoom clamp");
                Require(controller.Distance == 1000000f, "1000 km zoom range");
                var view = GetComponent<Camera>();
                foreach (Transform child in asteroids.transform)
                {
                    Vector3 projected = view.WorldToViewportPoint(child.position);
                    float radius = child.GetComponent<SpaceObject>().DiameterMeters * 0.5f;
                    Require(projected.z - radius > view.nearClipPlane && projected.z + radius < view.farClipPlane,
                        "far zoom retains " + child.name + " inside clipping range");
                }
                Require(view.nearClipPlane > 3f, "far zoom adjusts depth range");
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
                controller.Focus(drone);
                Require(controller.Pivot == drone.transform.position && controller.Selected == drone, "focus selects drone");
                Physics.SyncTransforms();
                Require(Physics.Raycast(view.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)), out RaycastHit hit), "object raycast");
                Require(hit.collider.GetComponentInParent<SpaceObject>() == drone, "object picking targets drone");
                controller.Focus(largest);
                Require(controller.Distance > 5000 && view.farClipPlane > controller.Distance + 5000, "large asteroid focus and far clip");
                return true;
            }
            catch (Exception error)
            {
                Fail(error.ToString());
                return false;
            }
        }

        private static void Capture(string path)
        {
            Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(path, image.EncodeToPNG());
            Destroy(image);
        }

        private IEnumerator CheckIntro(WaterScenario scenario)
        {
            IntroSequence intro = GetComponent<IntroSequence>();
            OrbitCamera camera = GetComponent<OrbitCamera>();
            if (!Guard(() =>
            {
                Require(intro != null && IntroSequence.IsPlaying && IntroSequence.BlocksGameplay, "intro blocks gameplay at startup");
                Require(intro.CueCount == 16 && intro.HasCompleteVoiceTrack, "all Mira captions have local voice clips");
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
                while (intro.CueIndex < 4) intro.AdvancePlayback(intro.CueDuration);
            })) yield break;
            yield return new WaitForSecondsRealtime(2.8f);
            yield return new WaitForEndOfFrame();
            Capture("Logs/intro-ship.png");
            if (!Guard(() =>
            {
                while (intro.CueIndex < 7) intro.AdvancePlayback(intro.CueDuration);
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
                Require(scenario.AssignTankOrder(source) && drone.Phase == DronePhase.Outbound, "tank order launches worker");
                Require(!scenario.AssignTankOrder(source), "busy worker rejects duplicate order");
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
                Until(scenario, () => drone.Phase == DronePhase.Mining, 3000);
                Require(drone.Speed == 0 && drone.TargetDistance == 0, "arrival brakes to rest");
                scenario.Advance(200);
                Require(drone.CargoKg > 19 && drone.CargoKg < 21, "mining fills cargo at configured rate");
                Require(Mathf.Abs(source.InitialRawKg - source.RemainingRawKg - drone.CargoKg) < 0.1f, "mined cargo removed from deposit");
                Require(scenario.WaterLiters == 20, "cargo is not credited before delivery");
                Require(drone.MiningSecondsRemaining > 0 && drone.PhaseProgress > 0, "mining progress and remaining time");
            })) yield break;
            yield return new WaitForEndOfFrame();
            Capture("Logs/scenario-mining.png");
            if (!Guard(() =>
            {
                Until(scenario, () => drone.Phase == DronePhase.Unloading, 3000);
                Require(Vector3.Distance(drone.transform.position, drone.HomePosition) < 0.01f && drone.Speed == 0, "loaded drone returns to dock");
                Require(scenario.Deliveries == 0 && scenario.WaterLiters == 20, "water waits for processing at dock");
                scenario.Advance(30);
                Require(scenario.Deliveries == 1 && scenario.WaterLiters > 59 && drone.Phase == DronePhase.Servicing, "first delivery enters ship tank");
                float water = scenario.WaterLiters, fuel = drone.FuelLiters, energy = drone.BatteryKwh;
                scenario.Advance(2);
                Require(drone.FuelLiters > fuel && scenario.WaterLiters < water && drone.BatteryKwh > energy, "service draws ship water and charges battery");
                camera.Focus(source.Info);
            })) yield break;
            yield return new WaitForEndOfFrame();
            Capture("Logs/scenario-asteroid.png");
            if (!Guard(() =>
            {
                Until(scenario, () => scenario.QuestComplete, 150000);
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
                scenario.ResetScenario();
                source.Mine(source.RemainingRawKg - 1);
                Require(scenario.AssignTankOrder(source), "small deposit can be assigned");
                Until(scenario, () => !drone.HasTankOrder && drone.IsReady, 25000);
                Require(!source.CanMineWater && scenario.Deliveries == 1 && !scenario.QuestComplete, "depleted source returns partial load and ends order");
                scenario.ResetScenario();
                scenario.AssignTankOrder(source);
                scenario.Advance(60);
                drone.ReturnToShip();
                Until(scenario, () => drone.IsReady, 25000);
                Require(!drone.HasTankOrder && Vector3.Distance(drone.transform.position, drone.HomePosition) < 0.01f, "cancelled flight safely returns to dock");
                Require(drone.CanCompleteTrip(source, out _), "cancelled drone is recharged for another order");
                scenario.ResetScenario();
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
