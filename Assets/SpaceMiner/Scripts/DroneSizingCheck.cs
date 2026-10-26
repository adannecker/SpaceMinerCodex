#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    // Exercises the actual campaign worker and physics route. Storage fixtures are explicit.
    public sealed class DroneSizingCheck : MonoBehaviour
    {
        private const string Folder = "Logs/DroneSizing";
        private WaterScenario scenario;
        private StationOperations ops;
        private DroneAgent drone;
        private AsteroidResource source;
        private int checks;
        private readonly Report report = new Report();

        [Serializable] private sealed class Report
        {
            public bool passed;
            public int checks;
            public float rawKg, fillFraction, occupiedM3, deliveredWaterLiters, receivedRawKg;
            public float simulationSeconds, miningSeconds, batteryAtDockKwh, fuelAtDockLiters;
            public float consumedBatteryKwh, burnedWaterLiters, observedMiningEnergyKwh;
            public float minimumBatteryKwh = float.MaxValue, minimumFuelLiters = float.MaxValue;
            public string scope = "Actual A01 launch, mining, loaded return, raw ice receipt and berthing with normal 200 L consumption-water tank and real cold-storage receiver; no 2000 L tank or mined-material fixture. Volume/mixture/mechanical constraints; real disk saves in mining and return; old twelve-value hardware and optional profiles; actual new Demo after legacy load. Detailed processing/mass/energy/geometry checks belong to StationIcePlantCheck; no structural certification or complete orbital/thermal claim.";
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        { if (Array.IndexOf(Environment.GetCommandLineArgs(), "-stationDroneSizingCheck") >= 0) new GameObject("Drone sizing acceptance").AddComponent<DroneSizingCheck>(); }
        private void Require(bool value, string label)
        { if (!value) throw new Exception("DRONE SIZING: " + label); checks++; Debug.Log("DRONE SIZING OK: " + label); }
        private static T Copy<T>(T value) => JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
        private static bool Near(float a, float b, float tolerance = .001f) => Mathf.Abs(a - b) <= tolerance;
        private string MissionDiagnostic() => "t=" + report.simulationSeconds.ToString("F2") + " phase=" + drone.Phase
            + " cargo=" + drone.CargoKg.ToString("F6") + " reason=" + drone.ReturnReason
            + " battery=" + drone.BatteryKwh.ToString("F6") + "/dockRequired=" + drone.RequiredDockReturnEnergyKwh.ToString("F6")
            + " fuel=" + drone.FuelLiters.ToString("F6") + "/dockRequired=" + drone.RequiredDockReturnFuelLiters.ToString("F6")
            + " order=" + drone.HasTankOrder + " deliveries=" + scenario.Deliveries + " trips=" + scenario.Mining.CompletedTrips;
        private void SaveRestore(string name, bool mining)
        {
            var saved = SaveGameStore.Capture(scenario, name, "Isolierter echter Flug / Volumenabnahme");
            var state = saved.drones[0]; float deposit = source.RemainingRawKg;
            var visual = drone.GetComponent<TripoMiningDroneVisual>(); Bounds visibleBounds = visual.CargoFillBounds;
            string path = SaveGameStore.Write(saved, directory: Path.GetFullPath(Folder + "/Saves"));
            scenario.Advance(mining ? 20 : 10);
            Require(mining ? drone.CargoKg > state.cargo + 1 : Vector3.Distance(drone.transform.position, state.position) > .05f, "simulation genuinely advances before restoring " + name);
            var fromDisk = SaveGameStore.Read(path); SaveGameStore.Apply(fromDisk, scenario); scenario.Scanner.DismissDialogue();
            Require(drone.Phase == state.phase && Near(drone.CargoKg, state.cargo) && Near(drone.BatteryKwh, state.battery) && Near(drone.FuelLiters, state.fuel), "disk load retains phase, mass, battery and propellant: " + name);
            Require(Vector3.Distance(drone.transform.position, state.position) < .001f && Near(source.RemainingRawKg, deposit), "disk load retains actual pose and remaining source: " + name);
            var restored = drone.CaptureState();
            Require(Near(restored.cargoGoal, state.cargoGoal) && Near(restored.cargoWaterFraction, state.cargoWaterFraction), "disk load retains composition and planned mass: " + name);
            Require(JsonUtility.ToJson(restored.cargoProfile) == JsonUtility.ToJson(state.cargoProfile), "disk load retains exact volume profile: " + name);
            Require(Vector3.Distance(visual.CargoFillBounds.min, visibleBounds.min) < .001f && Vector3.Distance(visual.CargoFillBounds.max, visibleBounds.max) < .001f && visual.PackedCargoInside, "disk load restores actual rendered cargo bounds immediately: " + name);
            Require(restored.routeIndex == state.routeIndex && (restored.route == null ? state.route == null : state.route != null && restored.route.Length == state.route.Length), "disk load retains route index and shape: " + name);
            if (state.route != null) for (int i = 0; i < state.route.Length; i++) Require((restored.route[i] - state.route[i]).sqrMagnitude < .000001f, "disk load retains waypoint " + i + ": " + name);
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(Folder); File.Delete(Folder + "/result.json"); File.Delete(Folder + "/error.txt");
            yield return new WaitForSecondsRealtime(1);
            FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride = Path.GetFullPath(Folder + "/Saves");
            CinematicLibrary.PreferencePrefix = "SpaceMiner.Test.DroneSizing." + Guid.NewGuid().ToString("N");
            FindFirstObjectByType<StartMenu>().StartDemo(); FindFirstObjectByType<IntroSequence>().Skip();
            yield return null; yield return null;
            try
            {
                Check(); report.passed = true; report.checks = checks;
                File.WriteAllText(Folder + "/result.json", JsonUtility.ToJson(report, true));
                Debug.Log("DRONE SIZING CHECK PASSED: " + checks); Application.Quit(0);
            }
            catch (Exception exception)
            { File.WriteAllText(Folder + "/error.txt", exception.ToString()); Debug.LogException(exception); Application.Quit(1); }
        }

        private void Check()
        {
            scenario = FindFirstObjectByType<WaterScenario>(); ops = scenario.Operations; drone = scenario.Worker;
            scenario.SetSimulationRate(0); scenario.Scanner.DismissDialogue();
            Require(ops != null && ops.Live, "live campaign enabled for sizing flag");
            Require(SettingsStore.PathName.StartsWith(Path.GetFullPath(Folder), StringComparison.OrdinalIgnoreCase), "settings isolated from user");
            foreach (var plan in ops.RoomPlans) Require(plan.SavePath.StartsWith(Path.GetFullPath(Folder), StringComparison.OrdinalIgnoreCase), "room plan isolated: " + plan.RoomId);
            Require(drone.IsReady && drone.CargoKg == 0 && drone.CargoProfile.VolumeLimited && drone.CargoProfile.Valid, "fresh worker has valid empty volume-limited cargo hold");
            Require(Near(drone.DryMassKg, 250) && Near(drone.FuelCapacityLiters, 15) && Near(drone.BatteryCapacityKwh, 16) && Near(drone.CargoCapacityKg, 1200), "campaign uses agreed mass, water, battery and mechanical capacities");
            Require(Near(drone.CruiseSpeed, 2) && Near(drone.ThrustNewtons, 5) && Near(drone.ExhaustSpeed, 1000) && Near(drone.HeatingKwhPerLiter, 1.2f), "campaign uses agreed flight and heating parameters");
            Require(Near(drone.MiningRateKgPerSecond, .5f) && Near(drone.MiningPowerKw, 4) && Near(drone.OnboardPowerKw, .05f) && Near(drone.ReserveFraction, .1f), "campaign uses agreed extraction and protected reserves");
            var profile = drone.CargoProfile;
            Require(Near(profile.UsableVolumeM3, 1.458f) && Near(profile.TargetFillFraction, .9f) && Near(profile.PackingFraction, .75f), "cargo envelope and fill assumptions are explicit");
            Require(Near(profile.CapacityKg(.8f, 1200), 1033.326f, .01f), "80/20 volume target is independently calculated 1033.326 kg");
            Require(Near(profile.CapacityKg(1, 1200), 902.466f, .01f), "pure ice volume target is independently calculated 902.466 kg");
            Require(Near(profile.CapacityKg(.65f, 1200), 1159.415f, .01f), "65/35 volume target is independently calculated 1159.415 kg");
            Require(Near(profile.CapacityKg(0, 1200), 1200) && Near(profile.CapacityKg(.8f, 700), 700), "mechanical payload limit can stop loading before volume target");
            Require(Near(profile.OccupiedVolumeM3(1033.326f, .8f), 1.3122f, .0001f), "80/20 target occupies 1.3122 cubic metres, not the full box");
            Require(Near(profile.OccupiedVolumeM3(0, .8f), 0) && Near(drone.CargoFillFraction, 0), "empty mass has zero occupied volume and visible fill");

            var mode = StationInteriorMode.Current; mode.RestoreInterior(ops.Layout.SpawnPosition, false, ops.Layout.SpawnYaw, 0);
            Require(mode.OpenConsole(), "actual scanner console opens"); scenario.Scanner.Advance(15);
            Require(scenario.Scanner.Scan(), "actual first scan finds starter resources"); scenario.Scanner.DismissDialogue(); mode.CloseConsole();
            source = Array.Find(scenario.Asteroids, a => a.name == "A-01");
            Require(source != null && source.CanMineWater && Near(source.WaterFraction, .8f), "real A01 is scanned and identified as 80 percent water");
            Require(Near(drone.CargoCapacityFor(source), 1033.326f, .01f), "real source uses the volume-aware mass capacity");

            // Raw ice is received independently of the small consumption-water tank.
            Require(Near(scenario.TankCapacityLiters, 200) && ops.IcePlant != null, "normal 200 L consumption tank and separate cold-storage plant exist");
            var baseline = SaveGameStore.Capture(scenario, "Sizing / normaler Empfänger", "Normaler Flüssigtank und separates Roh-Eislager");
            Require(scenario.AssignTankOrder(source), "normal raw-ice receiver accepts a real water order");
            Require(Near(drone.CaptureState().cargoGoal, 1033.326f, .01f), "normal cold receiver permits a 90-percent box independently of 200 L clean tank");
            SaveGameStore.Apply(Copy(baseline), scenario); scenario.Scanner.DismissDialogue();
            CheckSaveCompatibility(baseline);

            float sourceAtStart = source.RemainingRawKg;
            Require(scenario.AssignTankOrder(source), "normal campaign accepts complete 90-percent cold-storage mission");
            Require(Near(drone.CaptureState().cargoGoal, 1033.326f, .01f), "actual outbound goal is the 90-percent box mass");
            bool savedMining = false, savedReturn = false, full = false, delivered = false;
            var visited = new HashSet<DronePhase>(); float miningStartEnergy = 0, miningEndEnergy = 0;
            float startBattery = drone.BatteryKwh, startFuel = drone.FuelLiters;
            DronePhase? previousPhase = null; string previousReason = null;
            while (report.simulationSeconds < 120000)
            {
                var phase = drone.Phase; visited.Add(phase);
                if (phase != previousPhase || drone.ReturnReason != previousReason) { Debug.Log("DRONE SIZING TRACE: " + MissionDiagnostic()); previousPhase = phase; previousReason = drone.ReturnReason; }
                if (phase == DronePhase.Stranded) throw new Exception("DRONE SIZING: actual sortie stranded / " + MissionDiagnostic());
                bool parked = phase == DronePhase.Ready || phase == DronePhase.Servicing || phase == DronePhase.WaitingForPower;
                if (parked && !drone.HasTankOrder && Vector3.Distance(drone.transform.position, drone.HomePosition) < .05f
                    && (drone.CargoKg > .001f || scenario.Deliveries < 1 || scenario.Mining.CompletedTrips < 1))
                    throw new Exception("DRONE SIZING: unfinished or cancelled full mission returned to dock / " + MissionDiagnostic());
                if (phase == DronePhase.Mining && miningStartEnergy == 0) miningStartEnergy = drone.BatteryKwh;
                if (!savedMining && phase == DronePhase.Mining && drone.CargoKg > 250)
                {
                    string busyState = JsonUtility.ToJson(drone.CaptureState());
                    Require(!drone.ApplyNewSizingAtDock(out string busyReason) && !string.IsNullOrEmpty(busyReason) && JsonUtility.ToJson(drone.CaptureState()) == busyState, "hardware upgrade cannot reset an active loaded mining trip");
                    SaveRestore("Mining / echte Viertelladung", true); savedMining = true;
                    Render("01-mining-save-load", true);
                }
                if (!full && phase == DronePhase.Undocking)
                {
                    full = true; miningEndEnergy = drone.BatteryKwh; report.rawKg = drone.CargoKg; report.fillFraction = drone.CargoFillFraction; report.occupiedM3 = drone.CargoVolumeM3;
                    Require(Near(drone.CargoKg, 1033.326f, .02f) && Near(drone.CargoFillFraction, .9f, .00005f), "actual extraction reaches 90-percent target before return reserve stops it: " + drone.ReturnReason);
                    Require(drone.CargoKg < drone.CargoCapacityKg && Near(sourceAtStart - source.RemainingRawKg, drone.CargoKg, .01f), "source depletion equals actual raw cargo and remains below mechanical payload");
                    Require(drone.BatteryKwh >= drone.RequiredReturnEnergyKwh - .001f && drone.FuelLiters >= drone.RequiredReturnFuelLiters - .001f, "full cargo retains computed return energy, propellant and protected reserves");
                    var visual = drone.GetComponent<TripoMiningDroneVisual>();
                    Require(visual != null && visual.PackedCargoInside, "full visible cargo remains inside actual checked box bounds");
                    var visible = visual.CargoFillBounds;
                    Require(visible.size.x > 1.16f && visible.size.z > 1.46f && Near(visible.min.y, .07f, .001f) && Near(visible.max.y, .79f, .002f), "actual full rubble meshes reach expected 90-percent height and span the box floor");
                    Render("02-full-box-90-percent", true); Render("03-full-drone", false);
                }
                if (!savedReturn && full && phase == DronePhase.Returning && drone.PhaseElapsed < .1f && drone.TargetDistance > 5)
                { SaveRestore("Rückflug / volle Box", false); savedReturn = true; Render("04-loaded-return-save-load", false); }
                if (!delivered && scenario.Deliveries > 0 && drone.CargoKg < .001f)
                {
                    delivered = true; report.deliveredWaterLiters = scenario.DeliveredLiters;
                    report.receivedRawKg = (float)(ops.IcePlant.ReceivedWaterKg + ops.IcePlant.ReceivedRestKg);
                    Require(Near(report.receivedRawKg, 1033.326f, .03f) && drone.CargoKg < .001f, "actual unloading transfers the complete raw mixture to plant and empties the carrier");
                    Require(scenario.DeliveredLiters < 5 && ops.IcePlant.ReceivedRestKg > 206.5 && ops.IcePlant.IntakeKg + ops.IcePlant.CrushedKg + ops.IcePlant.ColdRawKg > 1020, "raw arrival preserves unknown matter and creates no instantaneous 827 L clean water");
                }
                if (delivered && scenario.Mining.CompletedTrips == 1 && (phase == DronePhase.Servicing || phase == DronePhase.Ready)) break;
                // Finish berthing on a simulation boundary, before service can refill propellant.
                float seconds = phase == DronePhase.Mining || phase == DronePhase.Berthing ? .25f : 2;
                float batteryBefore = drone.BatteryKwh;
                scenario.Advance(seconds);
                if (phase == DronePhase.Mining) { report.miningSeconds += seconds; report.observedMiningEnergyKwh += Mathf.Max(0, batteryBefore - drone.BatteryKwh); }
                report.simulationSeconds += seconds;
                report.minimumBatteryKwh = Mathf.Min(report.minimumBatteryKwh, drone.BatteryKwh); report.minimumFuelLiters = Mathf.Min(report.minimumFuelLiters, drone.FuelLiters);
            }
            Require(full && delivered && savedMining && savedReturn, "actual mission traverses full extraction, disk-resumed return and unloading");
            Require(scenario.Mining.CompletedTrips == 1 && scenario.Deliveries == 1 && Vector3.Distance(drone.transform.position, drone.HomePosition) < .01f, "productive real worker finishes at its physical planned dock exactly once");
            foreach (var phase in new[] { DronePhase.Launching, DronePhase.Outbound, DronePhase.Docking, DronePhase.Mining, DronePhase.Undocking, DronePhase.Returning, DronePhase.TankDocking, DronePhase.Unloading, DronePhase.TankUndocking, DronePhase.Parking, DronePhase.Berthing }) Require(visited.Contains(phase), "actual mission visits " + phase);
            Require(drone.BatteryKwh >= drone.EnergyReserveKwh - .001f && drone.FuelLiters >= drone.FuelReserveLiters - .001f, "complete mission returns with protected energy and water reserves");
            Require(report.miningSeconds > 2000 && report.miningSeconds < 2100, "measured level-one extraction time agrees with 0.5 kg per second");
            Require(report.observedMiningEnergyKwh > 2.25f && report.observedMiningEnergyKwh < 2.4f && miningStartEnergy > miningEndEnergy + 2.25f, "measured mining consumes about 2.325 kWh rather than old 80 kW budget");
            report.batteryAtDockKwh = drone.BatteryKwh; report.fuelAtDockLiters = drone.FuelLiters; report.consumedBatteryKwh = startBattery - drone.BatteryKwh; report.burnedWaterLiters = drone.BurnedWaterLiters;
            Require(startBattery > drone.BatteryKwh && Near(startFuel - drone.FuelLiters, drone.BurnedWaterLiters, .02f), "actual sortie consumes battery and accounts burned propellant without dock refill");
            var complete = SaveGameStore.Capture(scenario, "Sizing / sicher zurück", "Vollständiger Flug mit echter Quelle");
            SaveGameStore.Apply(Copy(complete), scenario);
            Require(scenario.Mining.CompletedTrips == 1 && scenario.Deliveries == 1 && Near(drone.BatteryKwh, report.batteryAtDockKwh) && Near(drone.FuelLiters, report.fuelAtDockLiters), "completed save reload duplicates no delivery, experience, battery or propellant");
        }

        private void CheckSaveCompatibility(SavedGame baseline)
        {
            var custom = Copy(baseline); var profile = custom.drones[0].cargoProfile;
            profile.usableVolumeM3 = 1.3f; profile.targetFillFraction = .87f; profile.packingFraction = .68f; profile.restDensityKgM3 = 2600;
            string path = SaveGameStore.Write(custom, directory: Path.GetFullPath(Folder + "/Saves")); SaveGameStore.Apply(SaveGameStore.Read(path), scenario);
            Require(JsonUtility.ToJson(drone.CaptureState().cargoProfile) == JsonUtility.ToJson(profile), "nondefault volume and density assumptions survive real disk roundtrip");
            var legacy = Copy(baseline); var state = legacy.drones[0];
            state.cargoProfile = null; state.hardware = new[] { 200f, 10, 8, 50, 5, 5, 1000, .2f, .1f, 80, .05f, .1f };
            state.fuel = 6; state.battery = 6; state.cargo = 12; state.cargoGoal = state.finalRawGoal = 50; state.cargoWaterFraction = .8f;
            state.phase = DronePhase.Ready; state.tankOrder = false; state.target = null; state.speed = 0;
            string legacyPath = SaveGameStore.Write(legacy, directory: Path.GetFullPath(Folder + "/Saves")); SaveGameStore.Apply(SaveGameStore.Read(legacyPath), scenario);
            Require(!drone.CargoProfile.VolumeLimited && Near(drone.CargoKg, 12) && Near(drone.CargoWaterLiters, 9.6f) && Near(drone.BatteryKwh, 6) && Near(drone.FuelLiters, 6), "legacy missing profile preserves actual cargo, water potential and partial supplies");
            var restored = drone.CaptureState(); Require(restored.hardware.Length == 12, "legacy twelve-value hardware schema stays unchanged");
            for (int i = 0; i < 12; i++) Require(Near(restored.hardware[i], state.hardware[i]), "legacy saved hardware preserved at index " + i);
            Require(Near(drone.EffectiveCargoCapacityKg, 50) && Near(drone.CargoFillFraction, .24f), "legacy hardware retains mass-limited capacity and existing proportional preview");
            var migrated = SaveGameStore.Capture(scenario, "Sizing / altes Profil erneut gespeichert", "Keine kostenlose Hardwareaufrüstung");
            SaveGameStore.Apply(Copy(migrated), scenario);
            Require(!drone.CargoProfile.VolumeLimited && Near(drone.CargoKg, 12) && Near(drone.BatteryKwh, 6) && Near(drone.FuelLiters, 6), "saving migrated legacy cargo does not convert it or refill supplies");
            string loadedLegacy = JsonUtility.ToJson(drone.CaptureState());
            Require(!drone.ApplyNewSizingAtDock(out string cargoReason) && !string.IsNullOrEmpty(cargoReason) && JsonUtility.ToJson(drone.CaptureState()) == loadedLegacy, "explicit legacy hardware upgrade rejects remaining cargo without dropping it");
            legacy.drones[0].cargoProfile = new DroneCargoProfile.SavedState(); SaveGameStore.Apply(Copy(legacy), scenario);
            Require(!drone.CargoProfile.VolumeLimited && Near(drone.CargoKg, 12), "Unity zero-version inline placeholder is treated as absent legacy profile");
            var emptyLegacy = Copy(legacy); emptyLegacy.drones[0].cargo = 0;
            SaveGameStore.Apply(Copy(emptyLegacy), scenario);
            scenario.ResetScenario();
            Require(!drone.CargoProfile.VolumeLimited && Near(drone.DryMassKg, 200) && Near(drone.CargoCapacityKg, 50)
                && Near(drone.BatteryCapacityKwh, 8) && Near(drone.FuelCapacityLiters, 10), "ordinary simulation reset deliberately retains legacy hardware and cargo profile");
            SaveGameStore.Apply(Copy(emptyLegacy), scenario);
            string legacyFileBeforeNewGame = File.ReadAllText(legacyPath);
            var start = FindFirstObjectByType<StartMenu>(); start.ReturnToStart(); start.StartDemo();
            FindFirstObjectByType<IntroSequence>().Skip(); scenario.SetSimulationRate(0);
            Require(!StartMenu.IsOpen && drone.CargoProfile.VolumeLimited && Near(drone.DryMassKg, 250)
                && Near(drone.CargoCapacityKg, 1200) && Near(drone.BatteryKwh, 16) && Near(drone.FuelLiters, 15),
                "actual new Demo after legacy load initializes current campaign hardware and fresh starting supplies");
            Require(Array.TrueForAll(scenario.Drones, item => item.CargoProfile.VolumeLimited && Near(item.CargoCapacityKg, 1200)
                && Near(item.BatteryCapacityKwh, 16) && Near(item.FuelCapacityLiters, 15)), "new campaign initializes current hardware for active and inactive drones");
            Require(File.ReadAllText(legacyPath) == legacyFileBeforeNewGame, "starting a new campaign does not rewrite the older manual save");
            SaveGameStore.Apply(emptyLegacy, scenario); float waterBeforeUpgrade = scenario.WaterLiters;
            Require(drone.ApplyNewSizingAtDock(out string upgradeReason), "explicit empty docked legacy upgrade succeeds: " + upgradeReason);
            Require(drone.CargoProfile.VolumeLimited && Near(drone.DryMassKg, 250) && Near(drone.CargoCapacityKg, 1200) && Near(drone.BatteryCapacityKwh, 16) && Near(drone.FuelCapacityLiters, 15), "explicit dock upgrade applies new hardware and volume model");
            Require(Near(drone.BatteryKwh, 6) && Near(drone.FuelLiters, 6) && Near(scenario.WaterLiters, waterBeforeUpgrade) && drone.CargoKg == 0 && drone.Phase == DronePhase.Servicing, "explicit upgrade preserves partial battery, fuel and station water and requests real servicing");
            SaveGameStore.Apply(Copy(baseline), scenario); scenario.Scanner.DismissDialogue();
            string before = JsonUtility.ToJson(drone.CaptureState());
            var invalid = Copy(baseline); invalid.drones[0].cargoProfile.packingFraction = 1.01f;
            bool rejected = false; try { SaveGameStore.Apply(invalid, scenario); } catch (IOException) { rejected = true; }
            Require(rejected && JsonUtility.ToJson(drone.CaptureState()) == before, "corrupt profile is rejected before mutating live drone");
            invalid = Copy(baseline); invalid.drones[0].cargo = 1100; invalid.drones[0].cargoWaterFraction = .8f;
            rejected = false; try { SaveGameStore.Apply(invalid, scenario); } catch (IOException) { rejected = true; }
            Require(rejected && JsonUtility.ToJson(drone.CaptureState()) == before, "save below mechanical limit but above volume target is rejected atomically");
            Require(drone.CargoProfile.VolumeLimited && Near(drone.CargoCapacityKg, 1200) && drone.CargoKg == 0, "new campaign profile restored after isolated legacy fixtures");
        }

        private void Render(string file, bool close)
        {
            var camera = new GameObject("Drone sizing render").AddComponent<Camera>(); camera.CopyFrom(Camera.main); camera.enabled = false;
            Vector3 target = drone.transform.TransformPoint(close ? new Vector3(0, 2.5f, -.1f) : new Vector3(0, 1, 0));
            camera.transform.position = target + drone.transform.TransformDirection(close ? new Vector3(2.1f, 2.5f, -3) : new Vector3(5.5f, 4, -6.5f)); camera.transform.LookAt(target);
            camera.fieldOfView = close ? 45 : 52; camera.nearClipPlane = .04f; camera.farClipPlane = 20000;
            var texture = new RenderTexture(1600, 1000, 24); var previous = RenderTexture.active; camera.targetTexture = texture; ops.RefreshViewLighting(camera, false);
            try
            {
                camera.Render(); RenderTexture.active = texture; var pixels = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); pixels.Apply(); File.WriteAllBytes(Folder + "/" + file + ".png", pixels.EncodeToPNG()); Destroy(pixels);
            }
            finally { RenderTexture.active = previous; camera.targetTexture = null; texture.Release(); Destroy(texture); Destroy(camera.gameObject); ops.RefreshViewLighting(Camera.main, StationInteriorMode.IsInside); }
        }
    }
}
#endif
