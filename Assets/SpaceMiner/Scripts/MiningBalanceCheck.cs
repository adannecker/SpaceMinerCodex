#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    // Opt-in benchmark uses the same scene, inventories and 0.25-second integration as normal play.
    public sealed class MiningBalanceCheck : MonoBehaviour
    {
        [Serializable] public sealed class Timing { public float speed; public double tankSeconds, firstLevelSeconds, level5ThroughputSeconds, level5EfficiencySeconds; }
        [Serializable] public sealed class Result
        {
            public string mode, source;
            public double seconds, firstLevelSeconds;
            public double flightSeconds, miningSeconds, servicingSeconds, maneuverAndUnloadSeconds;
            public int deliveries, completedTrips, level;
            public float firstCargoKg, deliveredLiters, fuelBurnedLiters, minimumEnergyAtHome, minimumFuelAtHome;
            public float flightEnergyKwh, miningEnergyKwh, otherEnergyKwh;
        }
        [Serializable] public sealed class Report
        {
            public bool passed;
            public int checks;
            public string unityVersion, assumptions;
            public float stepSeconds = .25f, startingWaterLiters = 20, normalTankLiters = 200;
            public int tripsPerLevel = 20;
            public float levelGain = .15f, miningPowerKw = 80, driveEnergyKwhPerLiter = .2f, reserveFraction = .1f;
            public List<Result> runs = new List<Result>();
            public List<Timing> timings = new List<Timing>();
        }
        private readonly Report report = new Report();
        private bool failed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-miningBalanceCheck") >= 0)
                new GameObject("Mining Balance Check").AddComponent<MiningBalanceCheck>();
        }
        private IEnumerator Start()
        {
            Directory.CreateDirectory("Logs");
            File.Delete("Logs/mining-balance-result.json");
            File.Delete("Logs/mining-balance-error.txt");
            Application.logMessageReceived += OnLog;
            yield return null;
            var scenario = FindFirstObjectByType<WaterScenario>();
            FindFirstObjectByType<IntroSequence>()?.Skip();
            scenario.SetSimulationRate(0);
            CheckProgression();
            scenario.SetSimulationRate(.5f); scenario.TogglePause(); Require(scenario.SimulationRate == 0, "pause");
            scenario.TogglePause(); Require(scenario.SimulationRate == .5f, "resume remembers half speed"); scenario.SetSimulationRate(0);
            var source = Array.Find(scenario.Asteroids, a => a.Info.DisplayName == "A-01");
            if (source == null) source = Array.Find(scenario.Asteroids, a => a.CanMineWater);
            Require(source != null && source.WaterFraction == .8f, "known 80 percent water source");
            scenario.Worker.BatteryCapacityKwh = .1f; scenario.ResetScenario(); PrepareScan(scenario);
            Require(!scenario.AssignTankOrder(source), "reject unaffordable round trip before launch");
            scenario.Worker.BatteryCapacityKwh = 8; scenario.ResetScenario(); PrepareScan(scenario);
            Require(scenario.AssignTankOrder(source), "assign reserve-safe trip");
            while (scenario.Worker.Phase != DronePhase.Mining) scenario.Advance(.25f);
            scenario.Worker.Tick(10000f);
            Require(scenario.Worker.Phase == DronePhase.Undocking && scenario.Worker.CargoKg < 50,
                "large mining step stops before cargo is full");
            Require(scenario.Worker.BatteryKwh >= scenario.Worker.RequiredReturnEnergyKwh - .001f
                && scenario.Worker.FuelLiters >= scenario.Worker.RequiredReturnFuelLiters - .001f, "large step protects both return budgets");
            yield return Run(scenario, source, "tank", MiningFocus.Throughput, 200, 1);
            yield return Run(scenario, source, "level5-throughput", MiningFocus.Throughput, 100000, 5);
            yield return Run(scenario, source, "level5-efficiency", MiningFocus.Efficiency, 100000, 5);
            if (failed) yield break;
            foreach (float speed in new[] { .5f, 1f, 2f, 3f, 5f }) report.timings.Add(new Timing {
                speed = speed, tankSeconds = report.runs[0].seconds / speed,
                firstLevelSeconds = report.runs[1].firstLevelSeconds / speed,
                level5ThroughputSeconds = report.runs[1].seconds / speed,
                level5EfficiencySeconds = report.runs[2].seconds / speed
            });
            report.unityVersion = Application.unityVersion;
            report.assumptions = "A-01, 80% water, one drone; level 1 initially, 20 productive parked sorties/level; +15% rate or -15% mining power per learned level, compounded. Tank: 20->200 L. Level benchmarks: 100000 L storage to allow continued deliveries, unchanged source/start water/charge/flight; no intro, user delays or menu pauses. Real time = simulation time / speed. Gameplay knowledge resets with a new world; no hardware generation upgrades.";
            scenario.TankCapacityLiters = 200; scenario.ResetScenario(); PrepareScan(scenario);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-miningLogicOnly") >= 0) {
                report.passed = true;
                File.WriteAllText("Logs/mining-balance-result.json", JsonUtility.ToJson(report, true));
                Debug.Log("MINING BALANCE LOGIC CHECK PASSED: " + report.checks + " checks");
                Application.Quit(0); yield break;
            }
            FindFirstObjectByType<OrbitCamera>().ResetView();
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/mining-hud.png"));
            yield return new WaitForSecondsRealtime(.3f);
            var tree = FindFirstObjectByType<TechTreeMenu>(); tree.PreviewMiningForCheck(false);
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/mining-knowledge.png"));
            yield return new WaitForSecondsRealtime(.3f); tree.PreviewMiningForCheck(true);
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/mining-knowledge-large.png"));
            yield return new WaitForSecondsRealtime(.3f); tree.Close();
            if (failed) yield break;
            report.passed = true;
            File.WriteAllText("Logs/mining-balance-result.json", JsonUtility.ToJson(report, true));
            Debug.Log("MINING BALANCE CHECK PASSED: " + report.checks + " checks");
            Application.Quit(0);
        }
        private void CheckProgression()
        {
            var knowledge = new MiningResearch();
            for (int i = 0; i < 19; i++) knowledge.CompleteTrip();
            Require(knowledge.Level == 1 && Mathf.Approximately(knowledge.Progress, .95f), "nineteen sorties are 95 percent of first level");
            knowledge.CompleteTrip();
            Require(knowledge.Level == 2 && Mathf.Approximately(knowledge.RateMultiplier, 1.15f), "twentieth sortie earns throughput level");
            knowledge.Focus = MiningFocus.Efficiency;
            Require(Mathf.Approximately(knowledge.RateMultiplier, 1.15f) && knowledge.PowerMultiplier == 1, "focus change preserves earned bonuses");
            for (int i = 0; i < 20; i++) knowledge.CompleteTrip();
            Require(knowledge.Level == 3 && Mathf.Approximately(knowledge.RateMultiplier, 1.15f)
                && Mathf.Approximately(knowledge.PowerMultiplier, .85f), "next earned level applies only chosen focus");
            knowledge.Reset(); Require(knowledge.Level == 1 && knowledge.CompletedTrips == 0, "new world resets knowledge");
        }
        private IEnumerator Run(WaterScenario scenario, AsteroidResource source, string mode, MiningFocus focus, float capacity, int targetLevel)
        {
            scenario.TankCapacityLiters = capacity; scenario.ResetScenario(); PrepareScan(scenario); scenario.Mining.Focus = focus;
            Require(scenario.AssignTankOrder(source), mode + " starts");
            var drone = scenario.Worker;
            var result = new Result { mode = mode, source = source.Info.DisplayName, minimumEnergyAtHome = 8, minimumFuelAtHome = 10 };
            double elapsed = 0;
            int parked = 0, chunk = 0;
            while (elapsed < 2000000 && !failed) {
                float battery = drone.BatteryKwh; DronePhase phase = drone.Phase;
                scenario.Advance(.25f); elapsed += .25;
                if (phase == DronePhase.Mining) result.miningSeconds += .25;
                else if (phase == DronePhase.Servicing) result.servicingSeconds += .25;
                else if (phase == DronePhase.Outbound || phase == DronePhase.Returning || phase == DronePhase.Parking) result.flightSeconds += .25;
                else result.maneuverAndUnloadSeconds += .25;
                float spent = Mathf.Max(0, battery - drone.BatteryKwh);
                if (phase == DronePhase.Mining) result.miningEnergyKwh += spent;
                else if (phase == DronePhase.Outbound || phase == DronePhase.Returning || phase == DronePhase.Parking) result.flightEnergyKwh += spent;
                else result.otherEnergyKwh += spent;
                RequireAlive(drone);
                if (result.firstCargoKg == 0 && drone.Phase == DronePhase.Undocking && drone.CargoKg > 0) result.firstCargoKg = drone.CargoKg;
                if (scenario.Mining.CompletedTrips != parked) {
                    parked = scenario.Mining.CompletedTrips;
                    result.minimumEnergyAtHome = Mathf.Min(result.minimumEnergyAtHome, drone.BatteryKwh);
                    result.minimumFuelAtHome = Mathf.Min(result.minimumFuelAtHome, drone.FuelLiters);
                    Require(drone.BatteryKwh >= drone.EnergyReserveKwh - .001f && drone.FuelLiters >= drone.FuelReserveLiters - .001f,
                        mode + " parked trip " + parked + " preserves reserves");
                }
                if (result.firstLevelSeconds == 0 && scenario.Mining.Level >= 2) result.firstLevelSeconds = elapsed;
                if (mode == "tank" ? scenario.QuestComplete : scenario.Mining.Level >= targetLevel) break;
                if (++chunk == 10000) { chunk = 0; yield return null; }
            }
            Require(mode == "tank" ? scenario.QuestComplete : scenario.Mining.Level == targetLevel, mode + " reaches target");
            result.seconds = elapsed; result.deliveries = scenario.Deliveries; result.completedTrips = scenario.Mining.CompletedTrips;
            result.level = scenario.Mining.Level; result.deliveredLiters = scenario.DeliveredLiters; result.fuelBurnedLiters = drone.BurnedWaterLiters;
            Require(Math.Abs(result.flightSeconds + result.miningSeconds + result.servicingSeconds + result.maneuverAndUnloadSeconds - elapsed) < .001,
                mode + " phase times account for the entire run");
            float minedWater = (source.InitialRawKg - source.RemainingRawKg) * source.WaterFraction;
            Require(Mathf.Abs(20 + 10 + minedWater - drone.BurnedWaterLiters - scenario.WaterLiters - drone.FuelLiters - drone.CargoWaterLiters) < .5f,
                mode + " conserves water across full benchmark");
            if (mode != "tank") Require(result.completedTrips == 80 && result.deliveries == 80 && result.firstLevelSeconds > 0, "level five needs eighty productive sorties");
            report.runs.Add(result);
        }
        private static void RequireAlive(DroneAgent drone)
        {
            if (drone.Phase == DronePhase.Stranded || (!drone.HasTankOrder && drone.IsReady && !drone.Scenario.QuestComplete))
                throw new Exception("Mining benchmark stalled: " + drone.Scenario.Message);
        }
        private static void PrepareScan(WaterScenario scenario)
        { var interior=StationInteriorMode.Current; interior.Enter(true); interior.RestoreInterior(interior.Layout.Console.position+interior.Room.forward*-1.5f,false); scenario.Scanner.OpenConsole(); scenario.Scanner.Advance(10); scenario.Scanner.Scan(); }

        private void Require(bool condition, string message) { if (!condition) throw new Exception("Mining check failed: " + message); report.checks++; }
        private void OnLog(string condition, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception) return;
            failed = true; File.WriteAllText("Logs/mining-balance-error.txt", condition + "\n" + stack); Application.Quit(1);
        }
        private void OnDestroy() { Application.logMessageReceived -= OnLog; }
    }
}
#endif
