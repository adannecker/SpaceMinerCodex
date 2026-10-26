#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    public sealed class StationIcePlantCheck : MonoBehaviour
    {
        private const string Folder = "Logs/StationIcePlant";
        private WaterScenario scenario;
        private StationOperations ops;
        private DroneAgent drone;
        private int checks;
        private bool failed;
        private readonly Result result = new Result();
        [Serializable] private sealed class Result
        {
            public bool passed;
            public int checks, coldMeshVertices, serviceCapsules;
            public double actualReceivedRawKg, actualWaterPotentialKg, actualRestKg, actualProcessedWaterKg;
            public double domainProcessedWaterKg, domainResidueKg, domainProcessingEnergyKwh;
            public float missionSeconds, batteryAtDockKwh, propellantAtDockLiters;
            public float automaticHeavyReturnBatteryKwh, automaticHeavyReturnFuelLiters;
            public string scope = "Real scanned A01 90-percent sortie with normal 200 L consumption tank; coupled raw receipt, all material retained, separate paid processing. Analytical domain steps cover mixed batches, finite cold volume, buffers, blackout/valves/start-stop, rest-bin backpressure and disk saves. Declared material fixtures isolate later processing. Full-intake case A uses unchanged real arriving supplies and physically flies the automatic heavy return; independent waiting/stop/resume/save case B explicitly adds 1 kWh within battery capacity. Real cold-cell vertices, service capsules, ring gate and revision-13 occupant migration. Actual contextual E-terminal reachability and Production/ice view, native framebuffer screenshots after backbuffer recreation. Programmatic controller tests, not manual keyboard comfort, structural/thermal certification or FPS acceptance.";
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        { if (Array.IndexOf(Environment.GetCommandLineArgs(), "-stationIcePlantCheck") >= 0) new GameObject("Cold ice plant acceptance").AddComponent<StationIcePlantCheck>(); }
        private void Require(bool condition, string label)
        { if (!condition) throw new Exception("ICE PLANT: " + label); checks++; Debug.Log("ICE PLANT OK: " + label); }
        private bool Guard(Action action)
        {
            try { action(); return true; }
            catch (Exception exception) { failed = true; File.WriteAllText(Folder + "/error.txt", exception.ToString()); Debug.LogException(exception); Application.Quit(1); return false; }
        }
        private static T Copy<T>(T value) => JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
        private static bool Near(double a, double b, double tolerance = 1e-6) => Math.Abs(a - b) <= tolerance;
        private string MissionDiagnostic(float seconds) => "t=" + seconds.ToString("F2") + " phase=" + drone.Phase
            + " cargo=" + drone.CargoKg.ToString("F6") + " reason=" + drone.ReturnReason
            + " battery=" + drone.BatteryKwh.ToString("F6") + "/dockRequired=" + drone.RequiredDockReturnEnergyKwh.ToString("F6")
            + " fuel=" + drone.FuelLiters.ToString("F6") + "/dockRequired=" + drone.RequiredDockReturnFuelLiters.ToString("F6")
            + " order=" + drone.HasTankOrder + " deliveries=" + scenario.Deliveries + " trips=" + scenario.Mining.CompletedTrips;
        private static IEnumerable<StationIcePlant.Batch> Batches(StationIcePlant.SavedState state)
        {
            foreach (var batch in state.intake) yield return batch;
            foreach (var batch in state.crushed) yield return batch;
            foreach (var cell in state.cells) foreach (var batch in cell.batches) yield return batch;
        }
        private void Conserves(StationIcePlant plant, string label)
        {
            double water = plant.CleanWaterPendingKg + plant.ExportedWaterKg, rest = plant.ResidueKg + plant.ExportedRestKg;
            foreach (var batch in Batches(plant.State)) { water += batch.waterKg; rest += batch.restKg; }
            // Double-domain rounding budget: 1 mg absolute plus 1e-10 of cumulative mass.
            double tolerance = 1e-6 + (plant.ReceivedWaterKg + plant.ReceivedRestKg) * 1e-10;
            Require(Near(water, plant.ReceivedWaterKg, tolerance) && Near(rest, plant.ReceivedRestKg, tolerance), "separate water/rest mass ledger: " + label);
            Require(StationIcePlant.Valid(plant.Capture()), "valid serialized mass/energy ledger: " + label);
        }
        private StationIcePlant ColdFixture(double raw, double fraction, double residue = 0, double exported = 0)
        {
            var state = StationIcePlant.CreateState();
            state.cells[0].batches.Add(new StationIcePlant.Batch(raw * fraction, raw * (1 - fraction)));
            state.residueKg = residue; state.exportedWaterKg = exported;
            state.receivedWaterKg = raw * fraction + exported; state.receivedRestKg = raw * (1 - fraction) + residue;
            state.crusherEnergyKwh = state.compactorEnergyKwh = (raw + residue + exported) * .0025;
            state.processingEnergyKwh = exported * .20;
            return new StationIcePlant(state);
        }
        private void Frozen(StationIcePlant plant, double seconds, bool power, bool water, double tankSpace, string label)
        {
            string before = JsonUtility.ToJson(plant.Capture()); plant.Advance(seconds, power, water, tankSpace);
            Require(JsonUtility.ToJson(plant.Capture()) == before && plant.ActualPowerKw == 0, "paused without mass loss or energy charge: " + label);
            Conserves(plant, label);
        }
        private void DomainDisk(StationIcePlant plant, string stage)
        {
            string path = Path.GetFullPath(Folder + "/domain-" + stage + ".json"); File.WriteAllText(path, JsonUtility.ToJson(plant.Capture(), true));
            var loaded = new StationIcePlant(JsonUtility.FromJson<StationIcePlant.SavedState>(File.ReadAllText(path)));
            Require(JsonUtility.ToJson(loaded.Capture()) == JsonUtility.ToJson(plant.Capture()), "disk roundtrip retains all batches and reservations: " + stage);
            plant.Advance(120, true, true, 100); loaded.Advance(120, true, true, 100);
            Require(JsonUtility.ToJson(loaded.Capture()) == JsonUtility.ToJson(plant.Capture()), "resumed domain step equals uninterrupted process: " + stage);
            Conserves(loaded, "resumed " + stage);
        }
        private void CheckDomain()
        {
            var plant = new StationIcePlant();
            Require(plant.IntakeKg == 0 && plant.ColdRawKg == 0 && plant.ResidueKg == 0 && plant.EnergyKwh == 0, "new cold plant starts genuinely empty");
            Require(Near(StationIcePlant.ColdCapacityM3, 257.109766) && plant.State.cells.Count == 3, "three cells share declared 257.109766 cubic metres");
            Require(Near(StationIcePlant.CellCapacityM3(0), 76.258180639, 1e-8) && Near(StationIcePlant.CellCapacityM3(1), 84.774222835, 1e-8) && Near(StationIcePlant.CellCapacityM3(2), 96.077362768, 1e-8), "widening geometry gives three unequal cell volumes rather than equal thirds");
            Require(Near(plant.Receive(1000, .8), 1000) && Near(plant.ReceivedWaterKg, 800) && Near(plant.ReceivedRestKg, 200), "raw receipt retains both known water potential and unknown rest");
            Require(plant.CleanWaterPendingKg == 0 && plant.ExportedWaterKg == 0 && plant.EnergyKwh == 0, "raw receipt does not melt free water");
            Require(Near(plant.MaxReceivableRawKg(.65), 500) && Near(plant.Receive(900, .65), 500), "partial intake obeys actual 1500 kg chamber limit");
            Require(Near(plant.ReceivedWaterKg, 1125) && Near(plant.ReceivedRestKg, 375) && plant.State.intake.Count == 2, "different incoming compositions remain distinct batches");
            string full = JsonUtility.ToJson(plant.Capture());
            Require(plant.Receive(1, .8) == 0 && plant.Receive(double.NaN, .8) == 0 && plant.Receive(1, double.NaN) == 0 && JsonUtility.ToJson(plant.Capture()) == full, "full/invalid intake is rejected without mutation");
            Conserves(plant, "mixed intake"); DomainDisk(plant, "intake");
            double[] fractions = { .65, 1, 0 }, referenceVolumes = { 1.1317775354416575, 1.4540167211922936, .5333333333333333 };
            for (int sample = 0; sample < fractions.Length; sample++)
            {
                var mixed = new StationIcePlant(); mixed.Receive(1000, fractions[sample]); mixed.ProcessingEnabled = false;
                mixed.Advance(45000, true, false, 0); mixed.Advance(45000, true, false, 0);
                Require(Near(mixed.ColdRawKg, 1000) && Near(mixed.ColdVolumeM3, referenceVolumes[sample], 1e-6), "actual cold chain keeps composition-specific occupied volume: " + fractions[sample]);
                Require(Near(mixed.State.crusherEnergyKwh, 2.5) && Near(mixed.State.compactorEnergyKwh, 2.5), "all mixed raw kilograms pay both mechanical stages: " + fractions[sample]);
                mixed.ProcessingEnabled = true; mixed.Advance(120, true, true, 100);
                Require(fractions[sample] == 0 ? mixed.CleanWaterPendingKg == 0 : Near(mixed.CleanWaterPendingKg, .25), "refiner extracts only actual water fraction: " + fractions[sample]);
                Conserves(mixed, "different mixture " + fractions[sample]);
            }

            var stages = new StationIcePlant(); stages.Receive(1000, .8); stages.State.processingEnabled = false;
            stages.Advance(3600, true, false, 0);
            Require(Near(stages.CrushedKg, 80) && Near(stages.IntakeKg, 920) && stages.ColdRawKg == 0, "one analytical hour crushes 80 kg and leaves stage transfer explicit");
            Require(Near(stages.State.crusherEnergyKwh, .2) && stages.State.compactorEnergyKwh == 0, "crusher pays actual 0.2 kWh for its first hour");
            DomainDisk(stages, "crusher");
            stages.Advance(3600, true, false, 0);
            Require(stages.ColdRawKg > 79 && stages.ColdVolumeM3 > .09 && stages.State.compactorEnergyKwh > .19, "compaction moves charged mixed matter into finite cold volume");
            Conserves(stages, "crusher and compactor");
            stages.State.receivingEnabled = false; Frozen(stages, 86400, true, false, 0, "intake/compaction stop switch");
            Require(stages.Receive(1, .8) == 0, "stopped receiver rejects docking material");
            stages.State.receivingEnabled = true; Frozen(stages, 86400, false, true, 100, "blackout with material in stages");

            var processor = ColdFixture(1000, .8);
            Frozen(processor, 86400, true, true, 0, "full consumption-water tank");
            Frozen(processor, 86400, true, false, 100, "closed water-sector valve");
            processor.State.processingEnabled = false; Frozen(processor, 86400, true, true, 100, "water processor stop switch"); processor.State.processingEnabled = true;
            Require(Near(processor.RequestedPowerKw(100, true), 1.5), "cold-only water demand requests 1.5 kW");
            for (int i = 0; i < 30; i++)
            {
                processor.Advance(120, true, true, 100);
                Require(processor.CleanWaterPendingKg <= .250000001 && processor.CleanWaterPendingKg > .24999, "small clean-water buffer meters a real two-minute process batch " + i);
                processor.ExportCleanWater(processor.CleanWaterPendingKg);
            }
            Require(Near(processor.ExportedWaterKg, 7.5) && Near(processor.State.processingEnergyKwh, 1.5) && Near(processor.ResidueKg, 1.875), "one hour produces exactly 7.5 kg water, retains 1.875 kg rest and costs 1.5 kWh");
            var largeBatch = ColdFixture(50000, 1); largeBatch.Advance(1.0 / 60, true, true, 100);
            Require(Near(largeBatch.CleanWaterPendingKg, .0000347222222222222, 1e-11) && largeBatch.State.processingEnergyKwh > .0000069, "one 60-FPS step extracts and pays for real water from a fifty-tonne batch");
            Require(largeBatch.ColdRawKg < 50000 && Near(largeBatch.State.processingEnergyKwh, largeBatch.CleanWaterPendingKg * .20, 1e-12), "tiny fractional processing preserves material and correct energy rather than stalling"); Conserves(largeBatch, "fifty-tonne batch at 60 FPS");
            result.domainProcessedWaterKg = processor.ExportedWaterKg; result.domainResidueKg = processor.ResidueKg; result.domainProcessingEnergyKwh = processor.State.processingEnergyKwh;
            Conserves(processor, "one hour metered refining"); processor.Advance(60, true, true, 100); DomainDisk(processor, "refiner");
            double pending = processor.CleanWaterPendingKg; Require(Near(processor.ExportCleanWater(.01), Math.Min(.01, pending)), "clean-water export removes only actually accepted quantity"); Conserves(processor, "partial clean export");
            var unknown = new SolidStore(StationResources.Unknown, 2, 1.99f); double residueBefore = processor.ResidueKg; float unknownBefore = unknown.amount;
            double movedRest = processor.TransferResidueTo(unknown, 1);
            Require(movedRest > .009 && Near(movedRest, (double)unknown.amount - unknownBefore, 1e-12) && Near(residueBefore - processor.ResidueKg, movedRest, 1e-12), "partially full unknown container receives exactly representable rest mass");
            unknown.Set(2); residueBefore = processor.ResidueKg;
            Require(processor.TransferResidueTo(unknown, 1) == 0 && processor.ResidueKg == residueBefore && processor.TransferResidueTo(new FluidTank(StationResources.Water, 10), 1) == 0, "full or incompatible rest destination preserves all residue"); Conserves(processor, "exported unknown residue");

            var blockedRest = ColdFixture(1000, .8, StationIcePlant.ResidueCapacityKg, StationIcePlant.ResidueCapacityKg * 4);
            Frozen(blockedRest, 86400, true, true, 100, "full unidentified-rest bin");
            var fullColdState = StationIcePlant.CreateState();
            double fullRawKg = 0;
            for (int i = 0; i < 3; i++) { double raw = StationIcePlant.CellCapacityM3(i) * 787.4759549326739; fullRawKg += raw; fullColdState.cells[i].batches.Add(new StationIcePlant.Batch(raw * .8, raw * .2)); }
            fullColdState.crushed.Add(new StationIcePlant.Batch(80, 20));
            fullColdState.receivedWaterKg = fullRawKg * .8 + 80; fullColdState.receivedRestKg = fullRawKg * .2 + 20;
            fullColdState.crusherEnergyKwh = (fullRawKg + 100) * .0025; fullColdState.compactorEnergyKwh = fullRawKg * .0025;
            var fullCold = new StationIcePlant(fullColdState); fullCold.State.processingEnabled = false;
            Require(Near(fullCold.ColdVolumeM3, 257.109766, 1e-6) && Near(fullRawKg, 202467.7585, .001), "reference 202467.759 kg 80/20 mixture occupies whole permitted cold volume");
            Frozen(fullCold, 86400, true, false, 0, "full cold cells backpressure compactor");
            var nearFullState = fullCold.Capture(); var last = nearFullState.cells[2].batches[0];
            last.waterKg -= 8; last.restKg -= 2; nearFullState.receivedWaterKg -= 8; nearFullState.receivedRestKg -= 2;
            nearFullState.crusherEnergyKwh -= .025; nearFullState.compactorEnergyKwh -= .025;
            var nearFull = new StationIcePlant(nearFullState); nearFull.Advance(3600, true, false, 0);
            Require(Near(nearFull.ColdVolumeM3, 257.109766, 1e-6) && Near(nearFull.CrushedKg, 90, 1e-5), "near-full cell accepts only remaining ten kilograms without losing the other ninety"); Conserves(nearFull, "partial compaction to full cell");
            var corrupt = processor.Capture(); corrupt.cells[0].batches[0].restKg += 1; Require(!StationIcePlant.Valid(corrupt), "mass creation in stored batch rejected");
            corrupt = processor.Capture(); corrupt.processingEnergyKwh = -1; Require(!StationIcePlant.Valid(corrupt), "negative process energy rejected");
            corrupt = processor.Capture(); corrupt.receivedWaterKg = double.NaN; Require(!StationIcePlant.Valid(corrupt), "nonfinite double ledger rejected");
            Require(StationIcePlant.Valid(null) && StationIcePlant.Valid(new StationIcePlant.SavedState()) && new StationIcePlant(new StationIcePlant.SavedState()).IntakeKg == 0, "missing or empty Unity placeholder migrates to empty version-one plant");
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(Folder); File.Delete(Folder + "/result.json"); File.Delete(Folder + "/error.txt"); yield return new WaitForSecondsRealtime(1);
            FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride = Path.GetFullPath(Folder + "/Saves");
            CinematicLibrary.PreferencePrefix = "SpaceMiner.Test.StationIcePlant." + Guid.NewGuid().ToString("N");
            FindFirstObjectByType<StartMenu>().StartDemo(); FindFirstObjectByType<IntroSequence>().Skip(); yield return null; yield return null;
            try
            {
                scenario = FindFirstObjectByType<WaterScenario>(); ops = scenario.Operations; drone = scenario.Worker; scenario.SetSimulationRate(0); scenario.Scanner.DismissDialogue();
                Require(ops != null && ops.Live && ops.IcePlant != null, "normal live campaign installs actual cold plant");
                Require(SettingsStore.PathName.StartsWith(Path.GetFullPath(Folder), StringComparison.OrdinalIgnoreCase), "settings isolated");
                foreach (var plan in ops.RoomPlans) Require(plan.SavePath.StartsWith(Path.GetFullPath(Folder), StringComparison.OrdinalIgnoreCase), "room plan isolated: " + plan.RoomId);
                CheckDomain(); CheckGeometry();
                var mode = StationInteriorMode.Current; mode.RestoreInterior(ops.Layout.SpawnPosition, false, ops.Layout.SpawnYaw, 0); Require(mode.OpenConsole(), "actual scanner console reached");
                scenario.Scanner.Advance(15); Require(scenario.Scanner.Scan(), "actual first scan before ice mission"); scenario.Scanner.DismissDialogue(); mode.CloseConsole();
                var baseline = SaveGameStore.Capture(scenario, "Eisanlage / leer", "Normale Kampagne ohne Lagerfixture");
                CheckCampaignPersistence(baseline); SaveGameStore.Apply(Copy(baseline), scenario); scenario.Scanner.DismissDialogue();
                CheckActualMission();
            }
            catch (Exception exception) { failed = true; File.WriteAllText(Folder + "/error.txt", exception.ToString()); Debug.LogException(exception); Application.Quit(1); }
            if (failed) yield break;
            yield return CheckTerminalUi();
            if (failed) yield break;
            Guard(() => { result.passed = true; result.checks = checks; File.WriteAllText(Folder + "/result.json", JsonUtility.ToJson(result, true)); Debug.Log("STATION ICE PLANT CHECK PASSED: " + checks); Application.Quit(0); });
        }

        private IEnumerator CheckTerminalUi()
        {
            // Recreate the native framebuffer, as the established UI acceptance does.
            // A hidden Windows launch at unchanged dimensions can otherwise capture black.
            Screen.SetResolution(1440, 900, FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(.35f);
            Screen.SetResolution(1600, 1000, FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(.7f);
            var mode = StationInteriorMode.Current; var terminal = ops.IcePlantTerminal;
            if (!Guard(() =>
            {
                scenario.SetSimulationRate(0); scenario.Scanner.DismissDialogue(); ops.ClosePanel();
                mode.RestoreInterior(ops.Layout.SpawnPosition, false, ops.Layout.SpawnYaw, 0); Physics.SyncTransforms();
                Require(terminal != null && ops.NearestTerminal(mode.FeetPosition) != terminal && !ops.InteractionPrompt(mode.FeetPosition).Contains("Eisannahme") && !ops.PanelOpen, "remote command-room feet offer no ice-plant E interaction or open panel");
                Vector3 feet = terminal.transform.TransformPoint(new Vector3(0, .03f, -.50f)) + ops.IcePlantFrame.forward * .75f;
                Require(ops.Layout.HasStandingClearance(feet), "physical ice terminal approach has real floor and capsule clearance");
                Quaternion look = Quaternion.LookRotation(terminal.transform.TransformPoint(new Vector3(0, 1.35f, -.05f)) - (feet + Vector3.up * 1.6f));
                mode.RestoreInterior(feet, false, look.eulerAngles.y, Mathf.DeltaAngle(0, look.eulerAngles.x)); Physics.SyncTransforms();
                Require(Vector3.Distance(mode.FeetPosition, feet) < .01f && ops.NearestTerminal(mode.FeetPosition) == terminal, "real safe service-lane approach resolves exactly the new ice terminal");
                Require(ops.InteractionPrompt(mode.FeetPosition).StartsWith("E") && ops.InteractionPrompt(mode.FeetPosition).Contains("Eisannahme"), "actual service-pult interaction exposes E and Eisannahme label");
            })) yield break;
            yield return CaptureNative("06-native-ice-terminal-e-prompt"); if (failed) yield break;
            if (!Guard(() =>
            {
                ops.OpenTerminal(ops.NearestTerminal(mode.FeetPosition));
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                object panel = typeof(StationOperations).GetField("panel", flags).GetValue(ops);
                string selected = (string)typeof(StationOperations).GetField("selected", flags).GetValue(ops);
                Require(ops.PanelOpen && panel is StationTerminalKind kind && kind == StationTerminalKind.Production && selected == "ice-plant", "physical ice terminal opens actual Production panel and ice-plant view");
                Require(StationOperations.BlocksInput && Cursor.lockState == CursorLockMode.None && Cursor.visible, "opened ice plant panel captures gameplay and exposes the real pointer");
            })) yield break;
            yield return CaptureNative("07-native-ice-plant-production-view"); if (failed) yield break;
            if (!Guard(() =>
            {
                ops.ClosePanel(); Require(!ops.PanelOpen, "ice terminal panel closes normally");
                Require(ops.NearestTerminal(mode.FeetPosition) == terminal && ops.InteractionPrompt(mode.FeetPosition).Contains("Eisannahme"), "physical terminal remains reachable after panel closes");
            })) yield break;
        }
        private IEnumerator CaptureNative(string file)
        {
            yield return new WaitForSecondsRealtime(.35f); yield return new WaitForEndOfFrame();
            string path = Path.GetFullPath(Folder + "/" + file + ".png"); ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSecondsRealtime(.4f);
            Guard(() =>
            {
                var image = new Texture2D(2, 2);
                try
                {
                    Require(image.LoadImage(File.ReadAllBytes(path)) && image.width == Screen.width && image.height == Screen.height, "native framebuffer screenshot readable at actual dimensions: " + file);
                    int bright = 0; foreach (var pixel in image.GetPixels32()) if (pixel.r > 40 || pixel.g > 40 || pixel.b > 40) bright++;
                    Require(bright > 1000, "native screenshot contains actual rendered interface/scene pixels: " + file);
                }
                finally { Destroy(image); }
            });
        }

        private void CheckCampaignPersistence(SavedGame baseline)
        {
            var stage = new StationIcePlant(); stage.Receive(1000, .8);
            for (int step = 0; step < 3; step++)
            {
                if (step > 0) stage.Advance(3600, true, true, 100);
                var save = Copy(baseline); save.name = "Anlagenstufe " + step; save.stationCampaign.icePlant = stage.Capture(); SaveGameStore.Apply(save, scenario); scenario.Scanner.DismissDialogue();
                string state = JsonUtility.ToJson(ops.IcePlant.Capture()); float water = scenario.WaterLiters;
                string path = SaveGameStore.Write(SaveGameStore.Capture(scenario, save.name, "Annahme/Brechen/Kaltlager"), directory: Path.GetFullPath(Folder + "/Saves"));
                scenario.Advance(1); SaveGameStore.Apply(SaveGameStore.Read(path), scenario); scenario.Scanner.DismissDialogue();
                Require(JsonUtility.ToJson(ops.IcePlant.Capture()) == state && Near(scenario.WaterLiters, water), "campaign disk load retains exact plant stage and water without double processing: " + step);
                Conserves(ops.IcePlant, "campaign load " + step);
            }
            var refining = ColdFixture(1000, .8); refining.Advance(60, true, true, 100);
            var refinerSave = Copy(baseline); refinerSave.stationCampaign.icePlant = refining.Capture(); SaveGameStore.Apply(refinerSave, scenario);
            string refinerPath = SaveGameStore.Write(SaveGameStore.Capture(scenario, "Aufbereitung / laufend", "Halbe saubere Puffercharge"), directory: Path.GetFullPath(Folder + "/Saves"));
            scenario.Advance(5); SaveGameStore.Apply(SaveGameStore.Read(refinerPath), scenario);
            Require(Near(ops.IcePlant.CleanWaterPendingKg, .125) && Near(ops.IcePlant.State.processingEnergyKwh, .025), "active refiner pending water and paid energy roundtrip exactly");

            var old = Copy(baseline); old.stationCampaign.icePlant = null; old.water = 37; old.stationCampaign.inventory.water.Set(37);
            old.drones[0].cargoProfile = null; old.drones[0].hardware = new[] { 200f, 10, 8, 50, 5, 5, 1000, .2f, .1f, 80, .05f, .1f };
            old.drones[0].cargo = 12; old.drones[0].cargoWaterFraction = .8f; old.drones[0].battery = 6; old.drones[0].fuel = 6;
            SaveGameStore.Apply(Copy(old), scenario);
            Require(ops.IcePlant.IntakeKg == 0 && ops.IcePlant.ColdRawKg == 0 && ops.IcePlant.EnergyKwh == 0 && Near(scenario.WaterLiters, 37), "old absent plant adds empty facility without changing stored clean water");
            Require(Near(drone.CargoKg, 12) && Near(drone.CargoWaterLiters, 9.6, 1e-5) && Near(drone.BatteryKwh, 6) && Near(drone.FuelLiters, 6) && Near(drone.CargoCapacityKg, 50) && !drone.CargoProfile.VolumeLimited, "old plant migration preserves legacy hardware, cargo and partial supplies");
            CheckRetainedCargoTrip(old); SaveGameStore.Apply(Copy(old), scenario);
            old.drones[0].phase = DronePhase.Unloading; old.drones[0].phaseSeconds = 200; old.drones[0].tankOrder = true; old.drones[0].target = "A-01";
            old.drones[0].position = drone.TankDockPoint; old.drones[0].rotation = drone.TankDockRotation;
            old.drones[0].rawIceDelivery = false; old.drones[0].iceDeliveryVersion = 0;
            SaveGameStore.Apply(Copy(old), scenario);
            Require(drone.Phase == DronePhase.Unloading && drone.RawIceDelivery && Near(drone.CargoKg, 12) && Near(drone.BatteryKwh, 6) && Near(drone.FuelLiters, 6) && Near(scenario.WaterLiters, 37), "legacy active dock save migrates delivery mode without changing phase, cargo or supplies");
            float oldCargo = drone.CargoKg; scenario.Advance(.25f);
            Require(ops.IcePlant.ReceivedWaterKg + ops.IcePlant.ReceivedRestKg > 0 && Near(oldCargo - drone.CargoKg, ops.IcePlant.ReceivedWaterKg + ops.IcePlant.ReceivedRestKg, 1e-8) && scenario.DeliveredLiters < .001, "legacy continuous transfer preserves actual mass and creates no instantaneous clean water");
            for (int i = 0; i < 800 && drone.CargoKg > 0; i++) scenario.Advance(.25f);
            Require(Near(ops.IcePlant.ReceivedWaterKg, 9.6, 1e-5) && Near(ops.IcePlant.ReceivedRestKg, 2.4, 1e-5) && drone.CargoKg < .0001 && scenario.DeliveredLiters < .5, "legacy real unloading eventually retains both fractions rather than directly crediting 9.6 L"); Conserves(ops.IcePlant, "legacy raw receipt");
            SaveGameStore.Apply(Copy(baseline), scenario);
            string beforePlant = JsonUtility.ToJson(ops.IcePlant.Capture()), beforeDrone = JsonUtility.ToJson(drone.CaptureState()); float beforeWater = scenario.WaterLiters;
            var invalid = Copy(baseline); invalid.stationCampaign.icePlant = stage.Capture(); invalid.stationCampaign.icePlant.receivedRestKg += 1;
            bool rejected = false; try { SaveGameStore.Apply(invalid, scenario); } catch (IOException) { rejected = true; }
            Require(rejected && JsonUtility.ToJson(ops.IcePlant.Capture()) == beforePlant && JsonUtility.ToJson(drone.CaptureState()) == beforeDrone && scenario.WaterLiters == beforeWater, "invalid plant save rejected before mutating campaign, hardware or supplies");
            CheckOccupantMigration(baseline);
        }

        private void CheckActualMission()
        {
            var source = Array.Find(scenario.Asteroids, a => a.name == "A-01");
            Require(scenario.TankCapacityLiters == 200 && ops.IcePlant.ReceivedWaterKg == 0 && ops.IcePlant.ColdRawKg == 0, "real mission starts normal 200 L tank and empty receiving/cold cells");
            Require(source != null && source.CanMineWater && scenario.AssignTankOrder(source), "real scanned A01 receives actual drone order");
            Require(Near(drone.CaptureState().cargoGoal, 1033.326, .02), "raw warehouse permits 90-percent box despite small clean-water tank");
            bool miningSave = false, flightSave = false, full = false, receipt = false, partialChecked = false;
            double rawAtStart = source.RemainingRawKg;
            DronePhase? previousPhase = null; string previousReason = null;
            while (result.missionSeconds < 120000)
            {
                if (drone.Phase != previousPhase || drone.ReturnReason != previousReason) { Debug.Log("ICE PLANT MISSION TRACE: " + MissionDiagnostic(result.missionSeconds)); previousPhase = drone.Phase; previousReason = drone.ReturnReason; }
                if (drone.Phase == DronePhase.Stranded) throw new Exception("ICE PLANT: actual drone stranded / " + MissionDiagnostic(result.missionSeconds));
                bool parked = drone.Phase == DronePhase.Ready || drone.Phase == DronePhase.Servicing || drone.Phase == DronePhase.WaitingForPower;
                if (parked && !drone.HasTankOrder && Vector3.Distance(drone.transform.position, drone.HomePosition) < .05f
                    && (drone.CargoKg > .001f || scenario.Deliveries < 1 || scenario.Mining.CompletedTrips < 1))
                    throw new Exception("ICE PLANT: unfinished or cancelled full mission returned to dock / " + MissionDiagnostic(result.missionSeconds));
                if (!miningSave && drone.Phase == DronePhase.Mining && drone.CargoKg > 250) { MissionDisk("mining"); miningSave = true; }
                if (!full && drone.Phase == DronePhase.Undocking)
                {
                    full = true; Require(Near(drone.CargoKg, 1033.326, .02) && Near(drone.CargoFillFraction, .9, .00005), "real mining fills box to 90 percent");
                    Require(Near(rawAtStart - source.RemainingRawKg, drone.CargoKg, .01), "asteroid loss equals actual raw cargo"); Render("01-full-raw-cargo", drone.transform.position + drone.transform.TransformDirection(new Vector3(4, 5, -6)), drone.transform.TransformPoint(new Vector3(0, 1.7f, 0)));
                }
                if (!flightSave && full && drone.Phase == DronePhase.Returning && drone.TargetDistance > 5) { MissionDisk("loaded-return"); flightSave = true; }
                if (!partialChecked && drone.Phase == DronePhase.Unloading && drone.PhaseElapsed >= 20 && drone.CargoKg > 1000)
                { CheckPartialRawReceipt(); partialChecked = true; }
                if (!receipt && scenario.Deliveries > 0 && drone.CargoKg < .001f)
                {
                    receipt = true; result.actualReceivedRawKg = ops.IcePlant.ReceivedWaterKg + ops.IcePlant.ReceivedRestKg; result.actualWaterPotentialKg = ops.IcePlant.ReceivedWaterKg; result.actualRestKg = ops.IcePlant.ReceivedRestKg;
                    Require(Near(result.actualReceivedRawKg, 1033.326, .03) && result.actualRestKg > 206.5 && drone.CargoKg < .001, "coupled actual unloading transfers full water/rest mixture into machine");
                    Require(scenario.DeliveredLiters < 5 && ops.IcePlant.ExportedWaterKg < 5 && ops.IcePlant.IntakeKg + ops.IcePlant.CrushedKg + ops.IcePlant.ColdRawKg > 1020, "raw docking creates no instantaneous tankful of free clean water");
                    Conserves(ops.IcePlant, "actual raw receipt"); RenderPlant("02-actual-raw-receipt"); MissionDisk("receipt");
                }
                if (receipt && scenario.Mining.CompletedTrips == 1 && (drone.Phase == DronePhase.Servicing || drone.Phase == DronePhase.Ready)) break;
                float seconds = drone.Phase == DronePhase.Mining || drone.Phase == DronePhase.Berthing ? .25f : 2; scenario.Advance(seconds); result.missionSeconds += seconds;
            }
            Require(miningSave && flightSave && full && receipt && partialChecked && scenario.Deliveries == 1 && scenario.Mining.CompletedTrips == 1, "complete normal mission actually mines, returns, unloads and berths once");
            Require(Vector3.Distance(drone.transform.position, drone.HomePosition) < .01f && drone.BatteryKwh >= drone.EnergyReserveKwh - .001 && drone.FuelLiters >= drone.FuelReserveLiters - .001, "real worker returns to planned berth with protected reserves");
            result.batteryAtDockKwh = drone.BatteryKwh; result.propellantAtDockLiters = drone.FuelLiters;
            Conserves(ops.IcePlant, "complete mission"); CheckLivePowerAndControls();
        }
        private void CheckRetainedCargoTrip(SavedGame old)
        {
            var source = Array.Find(scenario.Asteroids, a => a.name == "A-01"); float remaining = source.RemainingRawKg;
            int experience = scenario.Mining.CompletedTrips, deliveries = scenario.Deliveries;
            Require(!scenario.AssignTankOrder(source) && Near(drone.CargoKg, 12), "legacy rest cargo cannot be silently mixed or reinterpreted by a new mining order");
            Require(drone.DeliverExistingCargo(out string reason), "actual legacy rest cargo can leave dock for raw receiver: " + reason);
            Require(drone.DeliveringExistingCargo && !drone.HasTankOrder && drone.Target == null && Near(drone.CargoKg, 12), "retained-cargo mission has its own identity and no mining source");
            bool saved = false; float seconds = 0;
            while (seconds < 40000)
            {
                if (drone.Phase == DronePhase.Stranded) throw new Exception("ICE PLANT: retained legacy cargo trip stranded");
                if (!saved && drone.Phase == DronePhase.Returning)
                { MissionDisk("retained-legacy-cargo-flight"); Require(drone.DeliveringExistingCargo && !drone.HasTankOrder, "disk save preserves retained-cargo route without creating source order"); saved = true; }
                if (drone.CargoKg == 0 && Vector3.Distance(drone.transform.position, drone.HomePosition) < .01f && (drone.Phase == DronePhase.Servicing || drone.Phase == DronePhase.Ready)) break;
                scenario.Advance(drone.Phase == DronePhase.Berthing ? .25f : 2); seconds += 2;
            }
            Require(saved && drone.CargoKg == 0 && Near(ops.IcePlant.ReceivedWaterKg, 9.6, 1e-5) && Near(ops.IcePlant.ReceivedRestKg, 2.4, 1e-5), "legacy rest cargo physically reaches raw receiver completely");
            Require(scenario.Deliveries == deliveries + 1 && scenario.Mining.CompletedTrips == experience && source.RemainingRawKg == remaining, "retained-cargo delivery creates no free mining experience, source loss or extra events");
            Conserves(ops.IcePlant, "retained legacy shipment");
            SaveGameStore.Apply(Copy(old), scenario); scenario.Scanner.DismissDialogue();
        }
        private void MissionDisk(string stage)
        {
            var save = SaveGameStore.Capture(scenario, "Eisanlage / " + stage, "Echte laufende Mission"); string path = SaveGameStore.Write(save, directory: Path.GetFullPath(Folder + "/Saves"));
            scenario.Advance(2); SaveGameStore.Apply(SaveGameStore.Read(path), scenario); scenario.Scanner.DismissDialogue();
            Require(Near(drone.CargoKg, save.drones[0].cargo, .0001) && Near(drone.BatteryKwh, save.drones[0].battery, .0001) && Near(drone.FuelLiters, save.drones[0].fuel, .0001) && drone.Phase == save.drones[0].phase, "real mission disk load preserves cargo and supplies: " + stage);
            Require(JsonUtility.ToJson(ops.IcePlant.Capture()) == JsonUtility.ToJson(save.stationCampaign.icePlant), "real mission disk load preserves plant contents and paid energy: " + stage);
        }
        private void CheckPartialRawReceipt()
        {
            var original = SaveGameStore.Capture(scenario, "Annahme / vor echter Übergabe", "Echte angedockte volle Drohne");
            var intake = new StationIcePlant(); intake.Receive(1499, .8);
            var partial = Copy(original); partial.stationCampaign.icePlant = intake.Capture(); partial.stationCampaign.icePlant.processingEnabled = false; partial.drones[0].phaseSeconds = 200;
            // Case A: real arriving battery and propellant remain untouched. A physically
            // blocked receiver must cause the conservative automatic return, not unsafe waiting.
            SaveGameStore.Apply(partial, scenario); float cargo = drone.CargoKg; double priorReceived = ops.IcePlant.ReceivedWaterKg + ops.IcePlant.ReceivedRestKg;
            Require(Near(drone.BatteryKwh, original.drones[0].battery) && Near(drone.FuelLiters, original.drones[0].fuel), "blocked-receiver case A uses unmodified real arriving energy and propellant");
            scenario.Advance(.25f); double received = ops.IcePlant.ReceivedWaterKg + ops.IcePlant.ReceivedRestKg - priorReceived;
            Require(received > .999 && received <= 1.00001 && Near(cargo - drone.CargoKg, received, .0002), "real coupled drone transfers only actual one-kilogram intake space");
            Require(drone.Phase == DronePhase.TankUndocking && !drone.HasTankOrder && drone.CargoKg > 1000 && scenario.Deliveries == original.deliveries + 1, "actual low-wait-reserve partial shipment automatically undocks with remainder and one event");
            float restAboard = drone.CargoKg;
            var source = Array.Find(scenario.Asteroids, body => body.name == "A-01"); float sourceBefore = source.RemainingRawKg;
            int experienceBefore = scenario.Mining.CompletedTrips; float returnSeconds = 0; DronePhase? previousReturnPhase = null;
            while (returnSeconds < 20000 && !(Vector3.Distance(drone.transform.position, drone.HomePosition) < .01f && (drone.Phase == DronePhase.Servicing || drone.Phase == DronePhase.Ready)))
            {
                if (drone.Phase != previousReturnPhase) { Debug.Log("ICE PLANT HEAVY RETURN TRACE: " + MissionDiagnostic(returnSeconds)); previousReturnPhase = drone.Phase; }
                if (drone.Phase == DronePhase.Stranded) throw new Exception("ICE PLANT: heavy cancelled receiver return stranded / " + MissionDiagnostic(returnSeconds));
                float seconds = drone.Phase == DronePhase.Berthing ? .25f : 2; scenario.Advance(seconds); returnSeconds += seconds;
            }
            Require(Vector3.Distance(drone.transform.position, drone.HomePosition) < .01f && Near(drone.CargoKg, restAboard, .0001) && drone.CargoKg > 1000, "actual automatic heavy receiver trip returns all retained cargo to physical home");
            Require(drone.BatteryKwh >= drone.EnergyReserveKwh - .001 && drone.FuelLiters >= drone.FuelReserveLiters - .001, "actual >1000 kg automatic return preserves energy and propellant reserves / " + MissionDiagnostic(returnSeconds));
            result.automaticHeavyReturnBatteryKwh = drone.BatteryKwh; result.automaticHeavyReturnFuelLiters = drone.FuelLiters;
            double transferred = ops.IcePlant.ReceivedWaterKg + ops.IcePlant.ReceivedRestKg - 1499;
            Require(Near(drone.CargoKg + transferred, original.drones[0].cargo, .002) && source.RemainingRawKg == sourceBefore && scenario.Mining.CompletedTrips == experienceBefore, "automatic heavy return conserves depot plus aboard mass without extra source depletion or experience");
            Conserves(ops.IcePlant, "heavy automatic return");

            // Case B is an independent, declared energy fixture for waiting controls.
            // It is never used as evidence of the naturally arriving vehicle's safety.
            partial = Copy(partial); partial.drones[0].battery = original.drones[0].battery + 1;
            Require(partial.drones[0].battery <= partial.drones[0].hardware[2], "waiting-control fixture adds exactly 1 kWh within actual battery capacity");
            SaveGameStore.Apply(partial, scenario); scenario.Advance(.25f);
            Require(drone.Phase == DronePhase.Unloading && drone.CargoKg > 1000 && scenario.Deliveries == original.deliveries + 1, "separate plus-one-kWh fixture can safely wait after partial receipt");
            ops.IcePlant.ReceivingEnabled = false; restAboard = drone.CargoKg; double energy = ops.IcePlant.EnergyKwh;
            scenario.Advance(1); Require(drone.CargoKg == restAboard && drone.Phase == DronePhase.Unloading && ops.IcePlant.EnergyKwh == energy, "stopped raw receiver retains actual remainder aboard without silent discard");
            MissionDisk("partial-docked-receipt-plus-one-kWh-fixture");
            Require(Near(drone.CargoKg, restAboard, .0001) && scenario.Deliveries == original.deliveries + 1, "energy-fixture partial dock save preserves remainder and event identity");
            ops.IcePlant.ReceivingEnabled = true; scenario.Advance(2);
            Require(drone.CargoKg < restAboard && scenario.Deliveries == original.deliveries + 1, "resumed small intake transfers do not duplicate delivery events");
            Conserves(ops.IcePlant, "partial waiting energy fixture"); RenderPlant("05-partial-intake-retained-cargo-plus-one-kWh-fixture");
            restAboard = drone.CargoKg; drone.ReturnToShip();
            Require(drone.CargoKg == restAboard && drone.Phase == DronePhase.TankUndocking && !drone.HasTankOrder, "explicit cancel with waiting fixture retains all aboard mass while undocking");
            SaveGameStore.Apply(original, scenario); scenario.Scanner.DismissDialogue();
        }

        // Implemented against the authored transforms in StationOperations.IcePlantGeometry.
        private void CheckGeometry()
        {
            Require(ops.IcePlantFrame != null && ops.IcePlantColdCells.Count == 3, "three real cold cell models installed in the existing storage wedge");
            float tangent = Mathf.Tan(4.85f * Mathf.Deg2Rad), cosine = Mathf.Cos(4.85f * Mathf.Deg2Rad);
            foreach (var cell in ops.IcePlantColdCells) foreach (var filter in cell.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.GetComponent<TextMesh>() != null || filter.sharedMesh == null || filter.GetComponentInParent<StationDisplay>() != null) continue;
                foreach (var vertex in filter.sharedMesh.vertices)
                {
                    Vector3 p = ops.IcePlantFrame.InverseTransformPoint(filter.transform.TransformPoint(vertex)); result.coldMeshVertices++;
                    if (p.z < 38.9f || p.z > 51.3f || p.y < .02f || p.y > 5.05f || Mathf.Abs(p.x) > p.z * tangent - .15f / cosine)
                        throw new Exception("ICE PLANT: actual cold mesh outside storage wedge: " + filter.name + " / " + p);
                }
            }
            Require(result.coldMeshVertices > 100, "actual tank/cell mesh vertices lie within the wedge, above floor and below roof");
            Physics.SyncTransforms(); Require(ops.IcePlantServicePath.Length >= 4, "actual service path spans the complete machine room");
            foreach (Vector3 feet in ops.IcePlantServicePath)
            {
                result.serviceCapsules++; var hits = Physics.OverlapCapsule(feet + Vector3.up * .29f, feet + Vector3.up * 1.5f, .27f, 1 << 25, QueryTriggerInteraction.Ignore);
                Require(hits.Length == 0, "actual human service capsule unobstructed at " + ops.IcePlantFrame.InverseTransformPoint(feet) + (hits.Length == 0 ? "" : " / " + hits[0].name));
            }
            for (int i = 1; i < ops.IcePlantServicePath.Length; i++)
            {
                Vector3 from = ops.IcePlantServicePath[i - 1], delta = ops.IcePlantServicePath[i] - from;
                bool blocked = Physics.CapsuleCast(from + Vector3.up * .29f, from + Vector3.up * 1.5f, .27f, delta.normalized, out var hit, delta.magnitude, 1 << 25, QueryTriggerInteraction.Ignore);
                Require(!blocked, "complete service capsule sweep between checkpoints " + i + (blocked ? " / " + hit.collider.name : ""));
            }
            var entry = ops.IcePlantEntryPath;
            Require(entry.Length >= 4 && Vector3.Distance(entry[0], ops.IcePlantFrame.TransformPoint(new Vector3(0, .03f, 37.2f))) < .02f
                && Vector3.Distance(entry[entry.Length - 1], ops.IcePlantServicePath[0]) < .02f, "entry route starts in real ring and connects continuously to complete service path");
            for (int i = 0; i < entry.Length; i++)
            {
                result.serviceCapsules++; var hits = Physics.OverlapCapsule(entry[i] + Vector3.up * .29f, entry[i] + Vector3.up * 1.5f, .27f, 1 << 25, QueryTriggerInteraction.Ignore);
                Require(hits.Length == 0, "actual entry capsule clear at checkpoint " + i + (hits.Length == 0 ? "" : " / " + hits[0].name));
                if (i == 0) continue;
                Vector3 from = entry[i - 1], delta = entry[i] - from;
                bool blocked = Physics.CapsuleCast(from + Vector3.up * .29f, from + Vector3.up * 1.5f, .27f, delta.normalized, out var hit, delta.magnitude, 1 << 25, QueryTriggerInteraction.Ignore);
                Require(!blocked, "actual ring, vestibule and service-lane connection swept continuously: " + i + (blocked ? " / " + hit.collider.name : ""));
            }
            var socket = scenario.GetComponent<StationVisual>().TankSocket;
            Require(socket != null && Vector3.Distance(socket.position, ops.Layout.transform.TransformPoint(RingWalkthrough.At(54.8f, 200, 3.3f))) < .3f, "original actual exterior cargo coupling position preserved");
            RenderPlant("03-full-storage-geometry");
        }
        private void CheckOccupantMigration(SavedGame baseline)
        {
            var cell = ops.IcePlantColdCells[1]; var renderer = cell.GetComponentInChildren<Renderer>(); Vector3 centre = ops.IcePlantFrame.InverseTransformPoint(renderer.bounds.center);
            var unsafeFeet = ops.IcePlantFrame.TransformPoint(new Vector3(centre.x, .03f, centre.z));
            Require(!ops.Layout.HasStandingClearance(unsafeFeet), "new actual cold-cell floor blocks old interior standing pose");
            var old = Copy(baseline); old.interiorLayout = 13; old.hasInteriorState = old.insideStation = old.hasInteriorView = true; old.console = false; old.interiorFeet = unsafeFeet;
            SaveGameStore.Apply(old, scenario); Require(Vector3.Distance(StationInteriorMode.Current.FeetPosition, ops.Layout.SpawnPosition) < .02f, "old revision-13 pose inside new tank relocates to protected spawn");
            Vector3 safe = ops.IcePlantServicePath[ops.IcePlantServicePath.Length / 2]; Require(ops.Layout.HasStandingClearance(safe), "authored service lane supplies genuinely safe legacy pose");
            old.interiorFeet = safe; SaveGameStore.Apply(old, scenario); Require(Vector3.Distance(StationInteriorMode.Current.FeetPosition, safe) < .02f, "old revision-13 safe service-lane pose is preserved");
        }
        private void CheckLivePowerAndControls()
        {
            var baseline = SaveGameStore.Capture(scenario, "Eisanlage / nach Flug", "Unveränderte echte Anlieferung");
            var cold = ColdFixture(1000, .8); var fixture = Copy(baseline); fixture.stationCampaign.icePlant = cold.Capture(); fixture.drones[0].tankOrder = false; fixture.water = 100; fixture.stationCampaign.inventory.water.Set(100); fixture.stationCampaign.power.storedKwh = 20;
            SaveGameStore.Apply(fixture, scenario); scenario.Scanner.DismissDialogue();
            double before = ops.IcePlant.ExportedWaterKg; scenario.Advance(120);
            Require(ops.IcePlant.ExportedWaterKg > before + .24 && ops.IcePlant.EnergyKwh > cold.EnergyKwh + .04 && ops.State.power.ConsumptionKw >= 1.5, "live processor consumes station electricity and exports only paid water");
            result.actualProcessedWaterKg = ops.IcePlant.ExportedWaterKg; Conserves(ops.IcePlant, "actual station-powered processing"); RenderPlant("04-live-processing");
            ops.ToggleWater(7); double energy = ops.IcePlant.EnergyKwh, output = ops.IcePlant.ExportedWaterKg; scenario.Advance(60);
            Require(Near(ops.IcePlant.EnergyKwh, energy) && Near(ops.IcePlant.ExportedWaterKg, output), "actual closed water valve pauses refining without conversion"); ops.ToggleWater(7);
            ops.IcePlant.State.processingEnabled = false; energy = ops.IcePlant.EnergyKwh; scenario.Advance(60); Require(Near(ops.IcePlant.EnergyKwh, energy), "live processor stop control pauses actual paid conversion"); ops.IcePlant.State.processingEnabled = true;
            ops.TogglePower(7); energy = ops.IcePlant.EnergyKwh; output = ops.IcePlant.ExportedWaterKg + ops.IcePlant.CleanWaterPendingKg; scenario.Advance(60);
            Require(Near(ops.IcePlant.EnergyKwh, energy) && Near(ops.IcePlant.ExportedWaterKg + ops.IcePlant.CleanWaterPendingKg, output), "actual isolated storage sector performs no new paid conversion"); ops.TogglePower(7);
            scenario.ReactorPowerKw = scenario.SolarPowerKw = 0; ops.State.power.storedKwh = 0; energy = ops.IcePlant.EnergyKwh; output = ops.IcePlant.ExportedWaterKg + ops.IcePlant.CleanWaterPendingKg;
            scenario.Advance(5); Require(Near(ops.IcePlant.EnergyKwh, energy) && Near(ops.IcePlant.ExportedWaterKg + ops.IcePlant.CleanWaterPendingKg, output) && !ops.State.power.JobPowered, "actual station blackout grants no free plant energy or new water");
            var fullTank = Copy(fixture); fullTank.water = fullTank.tankCapacity; fullTank.stationCampaign.inventory.water.Set(fullTank.water);
            SaveGameStore.Apply(fullTank, scenario); energy = ops.IcePlant.EnergyKwh; output = ops.IcePlant.ExportedWaterKg; scenario.Advance(.25f);
            Require(Near(ops.IcePlant.EnergyKwh, energy) && Near(ops.IcePlant.ExportedWaterKg, output), "actual initially full clean tank stops conversion before occupant demand resumes");
            SaveGameStore.Apply(baseline, scenario); scenario.Scanner.DismissDialogue();
        }
        private void RenderPlant(string file)
        {
            if (file.StartsWith("05-"))
            { Render(file, ops.IcePlantFrame.TransformPoint(new Vector3(5, 7, 62)), ops.IcePlantFrame.TransformPoint(new Vector3(0, 3.3f, 55))); return; }
            if (file.StartsWith("04-"))
            { Render(file, ops.IcePlantServicePath[6] + Vector3.up * 1.72f, ops.IcePlantProcessor.position + Vector3.up * 1.6f); return; }
            if (file.StartsWith("03-"))
            { Render(file, ops.IcePlantEntryPath[2] + Vector3.up * 1.67f, ops.IcePlantServicePath[8] + Vector3.up * 1.57f); return; }
            Render(file, ops.IcePlantServicePath[7] + Vector3.up * 1.72f, ops.IcePlantProcessor.position + Vector3.up * 1.55f);
        }
        private void Render(string file, Vector3 eye, Vector3 target)
        {
            var camera = new GameObject("Ice plant evidence camera").AddComponent<Camera>(); camera.CopyFrom(Camera.main); camera.enabled = false; camera.transform.position = eye; camera.transform.LookAt(target); camera.fieldOfView = 65; camera.nearClipPlane = .04f; camera.farClipPlane = 20000;
            var texture = new RenderTexture(1600, 1000, 24); var previous = RenderTexture.active; camera.targetTexture = texture; ops.RefreshViewLighting(camera, true);
            try { camera.Render(); RenderTexture.active = texture; var pixels = new Texture2D(1600, 1000, TextureFormat.RGB24, false); pixels.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); pixels.Apply(); File.WriteAllBytes(Folder + "/" + file + ".png", pixels.EncodeToPNG()); Destroy(pixels); }
            finally { RenderTexture.active = previous; camera.targetTexture = null; texture.Release(); Destroy(texture); Destroy(camera.gameObject); ops.RefreshViewLighting(Camera.main, StationInteriorMode.IsInside); }
        }
    }
}
#endif
