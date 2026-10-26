#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    // Acceptance runs only with its explicit flag. All disk output is isolated
    // from the operator's campaign, settings and authored room plans.
    public sealed class StationPowerConsoleCheck : MonoBehaviour
    {
        private const string Folder = "Logs/StationPowerConsole";
        private StationOperations ops;
        private WaterScenario scenario;
        private StationInteriorMode mode;
        private StationTerminal terminal;
        private int checks, frustumWarnings;
        private bool failed;
        private readonly List<string> pictures = new List<string>();

        [Serializable] private sealed class Report
        {
            public bool passed;
            public int checks, layout, frustumWarnings;
            public string[] pictures;
            public string scope = "Physical battery placement and four authored cassette meshes per installed bank, physical command-room power terminal, read-only live power dashboard and existing shared energy store. Explicit load, state-of-charge, panel-normal, damage and network fixtures are restored. Genuine R-01 battery repair uses a declared helper/repair-technology/three-parts fixture with original energy; actual trajectory sampled every0.25s, collision/support and damaged-sector avoidance checked, then original campaign restored. Isolated old-save and room-plan roundtrips preserve resources and placement. Native framebuffer capture at1600x1000 and1280x720 including enlarged text. Programmatic scene/controller and framebuffer acceptance, not native OS input, manual comfort, headset or FPS acceptance.";
        }
        [Serializable] private sealed class LegacyPlan
        {
            public int schema = 1;
            public string room = "command-room";
            public CommandRoomLayoutItem[] items;
        }
        [Serializable] private sealed class RepairRouteReport
        {
            public float seconds, recipeSeconds, minimumServiceDistance, travelledMeters;
            public int movingSamples;
            public Vector3 servicePointLocal, homeLocal;
            public Vector3[] actualStationLocalSamples;
            public string fixture = "Copied isolated campaign: R-01 enabled, repair technology available,3parts, battery condition0.5; original energy/water and all other geometry retained. Real repair order, no teleporting helper or manually advancing work elapsed. All original campaign data restored afterward.";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-stationPowerConsoleCheck") >= 0)
                new GameObject("Station power console acceptance").AddComponent<StationPowerConsoleCheck>();
        }
        private void OnEnable() => Application.logMessageReceived += ObserveLog;
        private void OnDisable() => Application.logMessageReceived -= ObserveLog;
        private void ObserveLog(string message, string stack, LogType type)
        {
            if (message.IndexOf("Screen position out of view frustum", StringComparison.OrdinalIgnoreCase) >= 0) frustumWarnings++;
        }
        private void Require(bool condition, string label)
        {
            if (!condition) throw new Exception("POWER CONSOLE: " + label);
            checks++; Debug.Log("POWER CONSOLE OK: " + label);
        }
        private bool Guard(Action action)
        {
            try { action(); return true; }
            catch (Exception error)
            {
                failed = true; File.WriteAllText(Folder + "/error.txt", error.ToString());
                Debug.LogException(error); Application.Quit(1); return false;
            }
        }
        private static T Copy<T>(T source) => JsonUtility.FromJson<T>(JsonUtility.ToJson(source));
        private string Campaign() => JsonUtility.ToJson(ops.Capture());
        private Vector3 World(Vector3 local) => ops.Layout.transform.TransformPoint(local);
        private void UpdateBanks() { foreach (var bank in ops.BatteryBays) bank.UpdateState(); }
        private void CheckNewPowerTelemetry()
        {
            var history = new StationPowerHistory();
            history.Observe(0, 1.65f, 2.25f);
            Require(history.Samples.Count == 1 && history.Seconds == 0, "new session has one real point, no invented historical curve");
            history.Observe(0, 1.7f, 2.3f);
            Require(history.Samples.Count == 1, "paused observation replaces point without adding elapsed time");
            history.Observe(10, 2, 1);
            Require(history.Trend(true).Contains("zunehmend") && history.Trend(false).Contains("abnehmend"), "independent generator/consumer trends follow measured changes");
            for (int n = 0; n < 500; n++) history.Observe(10, n % 2, 2);
            Require(history.Samples.Count <= 240 && history.Samples[1].Seconds >= history.Seconds - 1800, "live history is bounded by count and30-minute simulation window");
            Require(Math.Abs(StationPowerHistory.RemainingSeconds(12, 24, -.6f) - 72000) < .01, "12kWh at0.6kW deficit yields20 hours of game time");
            Require(Math.Abs(StationPowerHistory.RemainingSeconds(6, 24, 2) - 32400) < .01, "charge forecast uses free capacity, not current stock");
            Require(double.IsNaN(StationPowerHistory.RemainingSeconds(12, 24, 0)), "balanced net power has no fabricated finite deadline");
            Require(StationPowerHistory.Forecast(24, 24, 1).Contains("voll") && StationPowerHistory.Forecast(0, 24, -1).Contains("leer"), "full/empty boundaries have explicit states");
            history.Reset(); Require(history.Samples.Count == 0 && history.Seconds == 0, "history can reset on load/new session");
            ops.RefreshSupplyState(); float sum = 0;
            foreach (var item in ops.PowerConsumers) sum += item.SuppliedKw;
            Require(Mathf.Abs(sum - ops.State.power.ConsumptionKw) < .0001f, "actual consumer allocation adds up to the one simulation power bill");
            Require(ops.PowerConsumers.Count >= 5 && !ops.SelectPowerItem("battery-bay-99") && !ops.SelectPowerItem("consumer:missing"), "invalid selections are rejected without resource mutation");
            var saved = Copy(SaveGameStore.Capture(scenario, "Before real telemetry steps", ""));
            try
            {
                for (int n = 0; n < 6; n++) scenario.Advance(10);
                Require(ops.PowerHistory.Seconds >= 60 && ops.PowerHistory.Samples.Count > 2, "real campaign simulation steps supply the rolling graph while the panel is closed");
                float billed = 0; foreach (var item in ops.PowerConsumers) billed += item.SuppliedKw;
                Require(Mathf.Abs(billed - ops.State.power.ConsumptionKw) < .0001f, "live changing consumer demand still sums to simulated supply");
            }
            finally { SaveGameStore.Apply(saved, scenario); scenario.Scanner.DismissDialogue(); ops.ClosePanel(); ops.RefreshSupplyState(); }
            Require(ops.PowerHistory.Seconds == 0 && ops.PowerHistory.Samples.Count == 1, "loading clears the previous session power history instead of connecting incompatible saves");
        }
        private static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);

        private void CheckPlacementAndMeshes()
        {
            Require(ops.BatteryBays.Count == 8, "all eight physical battery places exist");
            int installed = 0, active = 0;
            foreach (var bank in ops.BatteryBays)
            {
                Require(bank.Installed == (bank.Index == StationBatteryPlacement.ActiveIndex || bank.Index == StationBatteryPlacement.ReserveIndex), "only actual command-entrance pair is populated / " + bank.Index);
                Require(bank.Coupled == (bank.Index == StationBatteryPlacement.ActiveIndex), "only01 feeds the existing station bus / " + bank.Index);
                installed += bank.Installed ? 1 : 0; active += bank.Coupled ? 1 : 0;
                Require(bank.DetailLods != null && bank.DetailLods.lodCount == 3, "distance LOD retained / " + bank.Index);
                var highRenderers = bank.DetailLods.GetLODs()[0].renderers;
                var highRenderer = highRenderers != null ? Array.Find(highRenderers, renderer => renderer != null) : null;
                var highRoot = highRenderer != null ? highRenderer.transform : null;
                while (highRoot != null && highRoot.parent != null && highRoot.parent != bank.InstanceRoot) highRoot = highRoot.parent;
                Require(highRoot != null && highRoot.parent == bank.InstanceRoot,
                    "authored highest-detail model exists through actual LOD renderer hierarchy / " + bank.Index);
                int visibleCassettes = 0;
                foreach (var node in highRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (!node.name.StartsWith("Cassette", StringComparison.Ordinal)) continue;
                    var mesh = node.GetComponent<MeshFilter>();
                    Require(mesh != null && mesh.sharedMesh != null && mesh.sharedMesh.vertexCount > 500,
                        "actual detailed authored cassette mesh / " + bank.Index + " / " + node.name);
                    if (node.gameObject.activeInHierarchy) visibleCassettes++;
                }
                Require(visibleCassettes == (bank.Installed ? 4 : 0), "visible cassette count matches physical installed state / " + bank.Index);
            }
            Require(installed == 2 && active == 1, "two physically populated places, one contributing bank and six empty places");
            Vector3 command = ops.Layout.transform.InverseTransformPoint(ops.Layout.Console.position);
            foreach (int index in new[] { 0, 1 })
            {
                Vector3 bank = ops.Layout.transform.InverseTransformPoint(ops.BatteryBays[index].transform.position);
                Require(command.z > 38 && bank.z > 23 && bank.z < 29 && Mathf.Abs(bank.x) > 2 && Mathf.Abs(bank.x) < 5,
                    "installed bank is genuinely opposite command centre / " + index);
                Require(Vector2.Dot(new Vector2(command.x, command.z).normalized, new Vector2(bank.x, bank.z).normalized) > .95f,
                    "command and populated banks share the entrance axis across the ring corridor / " + index);
            }
        }

        private void CheckTerminal()
        {
            var all = ops.GetComponentsInChildren<StationTerminal>();
            var found = Array.FindAll(all, item => item.kind.ToString() == "Power");
            Require(found.Length == 1, "one additional explicit power terminal exists"); terminal = found[0];
            Require(terminal.target == "power-console", "power terminal has its own observation target, rather than a physical battery in command room");
            Vector3 local = ops.Layout.transform.InverseTransformPoint(terminal.transform.position);
            Require(local.z > 38 && local.z < 54 && Mathf.Abs(local.x) < 13, "power desk is inside command room");
            var block = terminal.GetComponentInParent<CommandRoomBlock>();
            Require(block != null && block.roomId == "command-room", "power desk remains part of editable command-room plan");
            Require(ops.RoomPlan.BoundRoot("battery") == block.transform && ops.RoomPlan.Get("battery").label.Contains("STROM"),
                "old editable battery placeholder is replaced by the explicitly labelled power desk");
            int banksInsideCommand = 0;
            foreach (var bank in ops.BatteryBays)
                if (ops.Layout.transform.InverseTransformPoint(bank.transform.position).z > 38) banksInsideCommand++;
            Require(banksInsideCommand == 0, "no physical battery remains in command room");
            ApproachTerminal();
            Require(ops.NearestTerminal(mode.FeetPosition) == terminal, "real physical desk can be reached without blocked E line of sight");
            string prompt = ops.InteractionPrompt(mode.FeetPosition);
            Require(prompt.StartsWith("E", StringComparison.Ordinal) && prompt.IndexOf("STROM", StringComparison.OrdinalIgnoreCase) >= 0,
                "contextual E prompt identifies the power desk");
            string before = Campaign(); ops.OpenTerminal(terminal);
            Require(ops.PanelOpen && StationOperations.BlocksInput && Cursor.lockState == CursorLockMode.None,
                "physical power terminal captures gameplay input and releases pointer");
            Require(Campaign() == before, "opening observational dashboard changes no campaign resources");
        }
        private void ApproachTerminal()
        {
            ops.ClosePanel(); scenario.Scanner.DismissDialogue(); mode.CloseConsole();
            mode.RestoreInterior(terminal.transform.TransformPoint(new Vector3(0, .04f, -1.55f)), false, terminal.transform.eulerAngles.y, 0);
            Physics.SyncTransforms();
        }

        private void CheckLegacyPersistence()
        {
            var original = Copy(SaveGameStore.Capture(scenario, "Power console original", ""));
            string campaign = Campaign(), inventory = JsonUtility.ToJson(ops.State.inventory);
            try
            {
                var legacy = Copy(original);
                legacy.stationCampaign.power.storedKwh = 7.25f;
                SaveGameStore.Validate(legacy, scenario); SaveGameStore.Apply(legacy, scenario);
                scenario.Scanner.DismissDialogue(); ops.ClosePanel(); UpdateBanks();
                Require(ops.State.power.capacityKwh == 24 && Mathf.Abs(ops.State.power.storedKwh - 7.25f) < .0001f,
                    "existing campaign format preserves explicit7.25kWh stock without bank reset or free energy");
                Require(ReferenceEquals(scenario.WaterTank, ops.State.inventory.water)
                    && JsonUtility.ToJson(ops.State.inventory) == inventory,
                    "power terminal adds no resource store and old-save load preserves original shared inventory");
                string path = SaveGameStore.Write(SaveGameStore.Capture(scenario, "Power console roundtrip", "explicit7.25kWh fixture"), false, Path.GetFullPath(Folder + "/Saves"));
                var loaded = SaveGameStore.Read(path); SaveGameStore.Validate(loaded, scenario);
                Require(loaded.stationCampaign.version == original.stationCampaign.version
                    && loaded.stationCampaign.power.storedKwh == 7.25f && loaded.stationCampaign.power.capacityKwh == 24,
                    "disk save roundtrip keeps campaign format and the one existing station energy store");
            }
            finally
            {
                SaveGameStore.Apply(original, scenario); scenario.Scanner.DismissDialogue(); ops.ClosePanel(); UpdateBanks();
            }
            Require(Campaign() == campaign, "save fixtures restore every original campaign resource");

            // Explicit old schema1 fixture, never the operator's authored plan.
            string fixturePath = Path.GetFullPath(Folder + "/legacy-schema1-command-room.json");
            var items = CommandRoomLayout.CreateDefaults();
            var oldBattery = Array.Find(items, item => item.id == "battery");
            Require(oldBattery != null, "legacy battery plan identity remains accepted");
            oldBattery.label = "AKKU /24kWh";
            oldBattery.position += new Vector3(.05f, 0, .1f); oldBattery.yaw = 267;
            Require(CommandRoomLayout.ValidatePlan(items, out string validation), "declared legacy operator pose is valid / " + validation);
            File.WriteAllText(fixturePath, JsonUtility.ToJson(new LegacyPlan { items = items }, true));
            string beforeLoad = File.ReadAllText(fixturePath);
            var host = new GameObject("Isolated legacy schema1 room-plan fixture");
            try
            {
                var legacyPlan = host.AddComponent<CommandRoomLayout>(); legacyPlan.Initialize(ops.Layout.transform, fixturePath);
                Require(string.IsNullOrEmpty(legacyPlan.StartupNotice) && !legacyPlan.HasUnsavedChanges,
                    "existing schema1 plan loads without fallback or an implicit migration write");
                Require(File.ReadAllText(fixturePath) == beforeLoad, "loading legacy room plan leaves its disk bytes untouched");
                foreach (var previous in items)
                {
                    var current = legacyPlan.Get(previous.id);
                    Require(current != null && current.position == previous.position && current.size == previous.size
                        && Mathf.Abs(Mathf.DeltaAngle(current.yaw, previous.yaw)) < .001f,
                        "legacy room-plan pose and dimensions survive reinterpretation / " + previous.id);
                }
                var newPower = legacyPlan.Get("battery");
                Require(newPower.label.IndexOf("STROM", StringComparison.OrdinalIgnoreCase) >= 0,
                    "legacy physical battery plan slot now labels the power console");
                Require(legacyPlan.Save(out string reason), "explicit isolated room-plan save succeeds / " + reason);
                var disk = JsonUtility.FromJson<LegacyPlan>(File.ReadAllText(fixturePath));
                var diskPower = Array.Find(disk.items, item => item.id == "battery");
                Require(disk.schema == 1 && disk.items.Length == items.Length && diskPower.position == oldBattery.position
                    && diskPower.size == oldBattery.size && Mathf.Abs(Mathf.DeltaAngle(diskPower.yaw, oldBattery.yaw)) < .001f,
                    "explicit plan save keeps schema1, all items and original operator power-desk pose");
            }
            finally { Destroy(host); }
        }

        private void CheckReadModelsAndLiveCharge()
        {
            string campaign = Campaign(); var sharedPower = ops.State.power;
            Require(ops.CaptureBatteryBay(-1) == null && ops.CaptureBatteryBay(8) == null,
                "invalid battery place IDs do not alias the active energy store");
            int installedModules = 0, activeModules = 0;
            for (int index = 0; index < 8; index++)
            {
                var bank = ops.CaptureBatteryBay(index);
                Require(bank.Index == index && bank.ModuleSlots == 4 && bank.Installed == (index == StationBatteryPlacement.ActiveIndex || index == StationBatteryPlacement.ReserveIndex),
                    "read model represents actual place and four physical cassette slots / " + index);
                Require(bank.InstalledModules == (bank.Installed ? 4 : 0) && bank.ActiveModules == (index == StationBatteryPlacement.ActiveIndex ? 4 : 0),
                    "read-model cassette counts match actual populated and coupled banks / " + index);
                Require(bank.BankNumber == (index == StationBatteryPlacement.ActiveIndex ? 1 : index == StationBatteryPlacement.ReserveIndex ? 2 : 0) && bank.Coupled == (index == StationBatteryPlacement.ActiveIndex),
                    "bank identity and shared-bus connection are explicit / " + index);
                installedModules += bank.InstalledModules; activeModules += bank.ActiveModules;
                if (index == StationBatteryPlacement.ActiveIndex)
                {
                    Require(bank.ChargeKnown && Mathf.Abs(bank.StoredKwh - sharedPower.storedKwh) < .0001f
                        && Mathf.Abs(bank.CapacityKwh - sharedPower.capacityKwh) < .0001f,
                        "active bank reads the one actual station energy store");
                    Require(Mathf.Abs(bank.ChargeFraction - sharedPower.storedKwh / sharedPower.capacityKwh) < .0001f
                        && Mathf.Abs(bank.Condition - ops.Element("battery").condition) < .0001f,
                        "charge and hardware health are separate real values");
                }
                else Require(!bank.ChargeKnown && bank.StoredKwh == 0,
                    "disconnected or empty place neither invents measured charge nor owns usable energy / " + index);
            }
            Require(installedModules == 8 && activeModules == 4, "eight physical cassettes installed, four coupled");
            var generation = ops.CaptureGeneration();
            foreach (var source in new[] { (id: "solar-a", panel: ops.SolarPanelA), (id: "solar-b", panel: ops.SolarPanelB) })
            {
                var p = ops.Layout.transform.InverseTransformPoint(source.panel.position);
                int sector = (Mathf.RoundToInt(Mathf.Atan2(p.x, p.z) * Mathf.Rad2Deg / 30) + 12) % 12;
                Require(sector == ops.Element(source.id).sector,
                    "real solar geometry retains its existing saved field, network and repair identity / " + source.id);
            }
            foreach (var source in new[] { generation.Reactor, generation.SolarA, generation.SolarB })
            {
                Require(Finite(source.NominalKw) && Finite(source.OutputKw) && Finite(source.Condition) && source.NominalKw >= 0
                    && source.OutputKw >= 0 && source.OutputKw <= source.NominalKw + .0001f,
                    "generation metrics finite and bounded / " + source.Id);
                Require(source.Condition == ops.Element(source.Id).condition && source.Connected == ops.Powered(ops.Element(source.Id).sector),
                    "source damage and station-bus connection reflect actual domain / " + source.Id);
            }
            Require(generation.SolarA.OrientationKnown && generation.SolarB.OrientationKnown,
                "both solar sources have actual geometric orientation");
            Require(Mathf.Abs(generation.TotalOutputKw - ops.State.power.GenerationKw) < .0001f,
                "dashboard source sum equals energy simulation generation");
            for (int repeat = 0; repeat < 20; repeat++)
            {
                ops.CaptureGeneration(); for (int index = 0; index < 8; index++) ops.CaptureBatteryBay(index); UpdateBanks();
            }
            Require(ReferenceEquals(sharedPower, ops.State.power) && Campaign() == campaign,
                "dashboard observations and label refresh neither allocate nor change energy or inventory");
            float originalCharge = sharedPower.storedKwh;
            try
            {
                foreach (float fraction in new[] { .97f, .23f })
                {
                    sharedPower.storedKwh = sharedPower.capacityKwh * fraction; UpdateBanks();
                    var bank = ops.CaptureBatteryBay(StationBatteryPlacement.ActiveIndex); string percent = Mathf.RoundToInt(fraction * 100) + "%";
                    Require(Mathf.Abs(bank.ChargeFraction - fraction) < .0001f && bank.Caption.Replace(" ", "").Contains(percent),
                        "explicit charge fixture updates actual read-model percentage / " + percent);
                    Require(ops.BatteryBays[StationBatteryPlacement.ActiveIndex].Caption == bank.Caption,
                        "physical active-bank status uses same live charge as dashboard / " + percent);
                    var display = Array.Find(ops.BatteryBays[StationBatteryPlacement.ActiveIndex].GetComponentsInChildren<StationDisplay>(), item => item.Caption == bank.Caption);
                    Require(display != null && display.Front.text == bank.Caption && display.Back.text == bank.Caption,
                        "both rendered display faces update when charge changes within the same active state / " + percent);
                    Require(!ops.CaptureBatteryBay(StationBatteryPlacement.ReserveIndex).ChargeKnown && ops.CaptureBatteryBay(StationBatteryPlacement.ReserveIndex).ActiveModules == 0,
                        "active charge fixture leaves disconnected02 unmeasured and inactive");
                }
            }
            finally { sharedPower.storedKwh = originalCharge; UpdateBanks(); }
            Require(Campaign() == campaign, "charge-display fixtures restore original campaign exactly");
        }

        private void CheckRealSolarAndPowerFixtures() => BinaryStarPowerChecks.Run(ops,Require);

        private void CheckDashboardFrame(int afterFrame)
        {
            Require(ops.PowerPanelOpen && ops.PowerDashboardLastRenderedFrame > afterFrame,
                "actual power dashboard completed a repaint after opening/resizing");
            Rect bounds = ops.PowerDashboardBounds;
            Require(bounds.width > 300 && bounds.height > 200 && bounds.xMin >= 0 && bounds.yMin >= 0
                && bounds.xMax <= UiLayout.Width + .1f && bounds.yMax <= UiLayout.Height + .1f,
                "responsive dashboard remains within current logical screen at current text scale");
            Require(StationOperations.OwnsScreenPoint(new Vector3(bounds.center.x * UiLayout.Scale(),
                Screen.height - bounds.center.y * UiLayout.Scale(), 0)), "power dashboard owns its actual pointer area");
            var hits = new List<KeyValuePair<string, Rect>>(ops.PowerHitAreas);
            for (int a = 0; a < hits.Count; a++) for (int b = a + 1; b < hits.Count; b++)
                Require(!hits[a].Value.Overlaps(hits[b].Value), "independent station hotspots do not cover one another / " + hits[a].Key + " / " + hits[b].Key);
        }

        private void CheckRealBatteryRepair()
        {
            var original = Copy(SaveGameStore.Capture(scenario, "Before isolated physical battery repair", ""));
            string campaign = Campaign();
            var plans = new List<string>(); foreach (var room in ops.RoomPlans) plans.Add(JsonUtility.ToJson(new LegacyPlan { room = room.RoomId, items = room.Snapshot() }));
            try
            {
                var fixture = Copy(original); fixture.stationCampaign.helper = true;
                fixture.stationCampaign.inventory.parts.Set(3);
                fixture.stationCampaign.elements.Find(item => item.id == "battery").condition = .5f;
                fixture.stationCampaign.orders.Clear();
                if (!fixture.stationCampaign.technologies.Contains("repair")) fixture.stationCampaign.technologies.Add("repair");
                SaveGameStore.Apply(fixture, scenario); scenario.SetSimulationRate(0); scenario.Scanner.DismissDialogue(); ops.ClosePanel();
                Physics.SyncTransforms();
                var target = ops.BatteryServicePoint;
                Vector3 targetLocal = ops.Layout.transform.InverseTransformPoint(target);
                Require(Vector3.Distance(targetLocal, new Vector3(3.5f, 1.31f, 27.4f)) < .015f,
                    "actual internal repair target is physical01 service zone opposite command room");
                var internalRoom = Array.Find(new List<CommandRoomLayout>(ops.RoomPlans).ToArray(), item => item.RoomId == "internal-workshop");
                Require(internalRoom != null, "real internal workshop plan supplies helper home anchor");
                Vector3 home = internalRoom.BoundRoot("helper-dock").TransformPoint(Vector3.up);
                Require(Vector3.Distance(ops.Layout.RepairDrone.position, home) < .02f,
                    "declared helper activation fixture starts R-01 at actual planned home");
                int repairs = ops.State.repairExperience;
                Require(ops.Enqueue(StationJobKind.Repair, "battery"), "enqueue genuine targeted battery repair");
                var order = ops.State.orders[ops.State.orders.Count - 1];
                float duration = StationRecipes.Get(StationJobKind.Repair).Seconds;
                var route = new List<Vector3>(); route.Add(ops.Layout.transform.InverseTransformPoint(ops.Layout.RepairDrone.position));
                float seconds = 0, previousProgress = 0, travelled = 0, minimumDistance = float.PositiveInfinity;
                int moving = 0, targetSamples = 0;
                bool progressMonotonic = true, supported = true, contained = true, avoidsDamaged = true, clear = true;
                string firstProblem = "";
                while (!order.complete && seconds < duration + 180)
                {
                    Vector3 before = ops.Layout.RepairDrone.position;
                    scenario.Advance(.25f); seconds += .25f;
                    Vector3 point = ops.Layout.RepairDrone.position;
                    var local = ops.Layout.transform.InverseTransformPoint(point); route.Add(local);
                    progressMonotonic &= order.elapsed >= previousProgress; previousProgress = order.elapsed;
                    float leg = Vector3.Distance(before, point); travelled += leg;
                    minimumDistance = Mathf.Min(minimumDistance, Vector3.Distance(point, target));
                    if (Vector3.Distance(point, target) < .05f) targetSamples++;
                    if (leg < .00001f) continue;
                    moving++;
                    Vector3 flat = World(new Vector3(local.x, .04f, local.z));
                    bool inside = ops.Layout.ContainsPosition(flat);
                    bool floor = Physics.Raycast(point + Vector3.up * .03f, Vector3.down, 2.5f, 1 << 25, QueryTriggerInteraction.Ignore);
                    float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
                    bool safeSector = Mathf.Abs(Mathf.DeltaAngle(angle, 90)) > 15;
                    bool hit = Physics.SphereCast(before, .27f, (point - before).normalized, out var obstacle, leg, 1 << 25, QueryTriggerInteraction.Ignore);
                    contained &= inside; supported &= floor; avoidsDamaged &= safeSector; clear &= !hit;
                    if (firstProblem == "" && (!inside || !floor || !safeSector || hit))
                        firstProblem = "t=" + seconds + " local=" + local + " inside=" + inside + " floor=" + floor + " safeSector="
                            + safeSector + " collider=" + (hit ? obstacle.collider.name : "none");
                }
                File.WriteAllText(Folder + "/battery-repair-route.json", JsonUtility.ToJson(new RepairRouteReport
                {
                    seconds = seconds, recipeSeconds = duration, minimumServiceDistance = minimumDistance,
                    travelledMeters = travelled, movingSamples = moving, servicePointLocal = targetLocal,
                    homeLocal = ops.Layout.transform.InverseTransformPoint(home), actualStationLocalSamples = route.ToArray()
                }, true));
                Require(order.started && order.actorId == "R-01", "real battery repair reserves internal R-01 rather than external drone or command-room proxy");
                Require(order.complete && !order.cancelled, "actual R-01 repair completes within recipe plus path reserve / " + order.status
                    + " / elapsed=" + order.elapsed + " / " + firstProblem);
                Require(progressMonotonic && moving > 100 && travelled > 30 && travelled < 100, "actual helper travels outward and returns with monotonically earned repair progress");
                Require(minimumDistance < .03f && targetSamples > 10, "R-01 physically reaches and works at installed battery service point");
                Require(contained && supported, "sampled real route stays over existing supported interior floor, including real bay aperture / " + firstProblem);
                Require(avoidsDamaged && ops.Element("sector-3").condition < .3f,
                    "real route bypasses inaccessible90-degree ring without repairing or crossing it / " + firstProblem);
                Require(clear, "actual moving segments do not cross walls, closed doors, hardware or vacuum shortcuts / " + firstProblem);
                Require(Vector3.Distance(ops.Layout.RepairDrone.position, home) < .03f,
                    "completed repair returns R-01 to same actual workshop home");
                Require(ops.Element("battery").condition > .999f && ops.State.repairExperience == repairs + 1
                    && Mathf.Abs(ops.State.inventory.parts.amount - 1) < .0001f,
                    "completed real repair improves bank health and consumes exactly two declared parts");
                Require(ops.State.power.Valid && ops.State.power.capacityKwh == original.stationCampaign.power.capacityKwh
                    && !ops.BatteryBays[StationBatteryPlacement.ReserveIndex].Coupled, "real repair retains original energy system and keeps reserve02 isolated");
            }
            finally
            {
                SaveGameStore.Apply(original, scenario); scenario.SetSimulationRate(0); scenario.Scanner.DismissDialogue();
                ops.ClosePanel(); ops.RefreshSupplyState(); UpdateBanks(); Physics.SyncTransforms();
            }
            Require(Campaign() == campaign && ops.State.helper == original.stationCampaign.helper,
                "full original campaign and inactive helper policy restored after physical repair fixture");
            for (int index = 0; index < ops.RoomPlans.Count; index++)
                Require(plans[index] == JsonUtility.ToJson(new LegacyPlan { room = ops.RoomPlans[index].RoomId, items = ops.RoomPlans[index].Snapshot() }),
                    "physical repair leaves planned room geometry unchanged / " + ops.RoomPlans[index].RoomId);
        }

        private IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.25f); yield return new WaitForEndOfFrame();
            string path = Path.GetFullPath(Folder + "/" + name + ".png"); ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSecondsRealtime(.35f);
            Guard(() =>
            {
                var pixels = new Texture2D(2, 2);
                try
                {
                    Require(File.Exists(path) && pixels.LoadImage(File.ReadAllBytes(path)), "native framebuffer saved / " + name);
                    Require(pixels.width == Screen.width && pixels.height == Screen.height, "native framebuffer dimensions / " + name);
                    int visible = 0; foreach (var pixel in pixels.GetPixels32()) if (pixel.r > 45 || pixel.g > 45 || pixel.b > 45) visible++;
                    Require(visible > 1000, "framebuffer contains lit scene and actual UI / " + name); pictures.Add(name + ".png");
                }
                finally { Destroy(pixels); }
            });
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(Folder); File.Delete(Folder + "/result.json"); File.Delete(Folder + "/error.txt");
            yield return new WaitForSecondsRealtime(1);
            if (!Guard(() =>
            {
                FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride = Path.GetFullPath(Folder + "/Saves");
                CinematicLibrary.PreferencePrefix = "SpaceMiner.Test.PowerConsole." + Guid.NewGuid().ToString("N");
                Require(SettingsStore.PathName.StartsWith(Path.GetFullPath(Folder), StringComparison.OrdinalIgnoreCase), "settings isolated from user settings");
                FindFirstObjectByType<StartMenu>().StartDemo(); FindFirstObjectByType<IntroSequence>().Skip();
            })) yield break;
            yield return null; yield return null;
            if (!Guard(() =>
            {
                ops = StationOperations.Current; scenario = ops.Scenario; mode = StationInteriorMode.Current;
                scenario.SetSimulationRate(0); scenario.Scanner.DismissDialogue(); ops.ClosePanel();
                Require(ops.Live, "isolated flag starts real live campaign");
                foreach (var plan in ops.RoomPlans) Require(plan.SavePath.StartsWith(Path.GetFullPath(Folder), StringComparison.OrdinalIgnoreCase), "isolated room-plan path / " + plan.RoomId);
                Require(StationOperations.Valid(ops.Capture()), "initial real campaign remains valid");
                UpdateBanks(); CheckPlacementAndMeshes(); CheckTerminal(); CheckLegacyPersistence();
                CheckReadModelsAndLiveCharge(); CheckRealSolarAndPowerFixtures(); CheckRealBatteryRepair();
                CheckNewPowerTelemetry();
                SettingsStore.Current.Interface.Scale = 1;
                SettingsStore.Current.Accessibility.TextScale = 1;
                SettingsStore.Current.Gameplay.PauseInMenu = true;
                Screen.SetResolution(1440, 900, FullScreenMode.Windowed);
            })) yield break;
            yield return new WaitForSecondsRealtime(.3f);
            Screen.SetResolution(1600, 1000, FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(.6f);
            if (!Guard(() =>
            {
                Require(Screen.width == 1600 && Screen.height == 1000, "actual1600x1000 native game framebuffer established");
                mode.RestoreInterior(World(RingWalkthrough.At(33.5f, 0, .04f)), false, ops.Layout.transform.eulerAngles.y+180, -3);
                Camera.main.fieldOfView = 76;
            })) yield break;
            yield return Capture("01-gegenueber-zentrale-echte-50-prozent"); if (failed) yield break;
            if (!Guard(ApproachTerminal)) yield break;
            yield return Capture("02-strompult-in-zentrale-e-hinweis"); if (failed) yield break;
            string readonlyCampaign = Campaign();
            int opened = Time.frameCount;
            if (!Guard(() => Require(ops.OpenPowerConsole(), "physical power-console interaction opens guarded dashboard"))) yield break;
            yield return null; yield return null;
            if (!Guard(() =>
            {
                CheckDashboardFrame(opened);
                Vector3 feet = mode.FeetPosition; mode.Walk(Vector2.up, .1f);
                Require(mode.FeetPosition == feet && SettingsMenu.BlocksInput,
                    "opened actual power desk blocks CharacterController movement");
            })) yield break;
            yield return Capture("03-stromuebersicht-1600x1000"); if (failed) yield break;
            if (!Guard(() =>
            {
                Require(ops.PowerPreviewMeshCount > 100 && ops.PowerDetailMeshCount > 5, "overview and selected bank render the actual shared game meshes");
                Require(ops.PowerHitAreas.Count == 11, "eight real battery slots, reactor and both solar panels have projected selection areas");
                foreach (var item in ops.PowerHitAreas)
                    Require(item.Value.width > 0 && item.Value.height > 0, "projected hotspot has usable dimensions / " + item.Key);
            })) yield break;
            foreach (string id in new[] { "reactor", "solar-a", "solar-b", "battery-bay-1", "battery-bay-2", "consumer:life", "sun-primary", "sun-companion" })
            {
                if (!Guard(() => Require(ops.SelectPowerItem(id), "inspector selection / " + id))) yield break;
                yield return null; yield return null;
                yield return Capture("detail-" + id.Replace(":", "-")); if (failed) yield break;
                if (!id.StartsWith("consumer:") && !id.StartsWith("sun-") && !Guard(() => Require(ops.PowerDetailMeshCount > 0, "selected item reuses original mesh data / " + id))) yield break;
            }
            if (!Guard(() => { ops.SelectPowerItem("battery-bay-0"); ops.ScrollPowerDashboard(new Vector2(0, 10000)); })) yield break;
            yield return Capture("detail-bankbilanz"); if (failed) yield break;
            if (!Guard(() => ops.ScrollPowerDashboard(Vector2.zero))) yield break;
            float stock = ops.State.power.storedKwh;
            if (!Guard(() => { ops.State.power.storedKwh = ops.State.power.capacityKwh * .97f; UpdateBanks(); })) yield break;
            yield return Capture("04-explizite-97-prozent-anzeigefixture"); if (failed) yield break;
            if (!Guard(() => { ops.State.power.storedKwh = stock; UpdateBanks(); })) yield break;
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(.6f);
            int resized = Time.frameCount;
            yield return null; yield return null;
            if (!Guard(() =>
            {
                Require(Screen.width == 1280 && Screen.height == 720, "actual1280x720 native game framebuffer established");
                CheckDashboardFrame(resized);
            })) yield break;
            yield return Capture("05-stromuebersicht-1280x720"); if (failed) yield break;
            if (!Guard(() => { SettingsStore.Current.Accessibility.TextScale = 1.4f; ops.ScrollPowerDashboard(Vector2.zero); })) yield break;
            int scaled = Time.frameCount; yield return null; yield return null;
            if (!Guard(() => CheckDashboardFrame(scaled))) yield break;
            yield return Capture("06-stromuebersicht-grosse-schrift-oben"); if (failed) yield break;
            if (!Guard(() => ops.ScrollPowerDashboard(new Vector2(0, 100000)))) yield break;
            yield return Capture("07-stromuebersicht-grosse-schrift-unten"); if (failed) yield break;
            foreach(string starId in new[]{"sun-primary","sun-companion"})
            {
                if(!Guard(()=>{Require(ops.SelectPowerItem(starId),"large-text star inspector / "+starId);ops.ScrollPowerDashboard(Vector2.zero);}))yield break;
                yield return null;yield return null;
                yield return Capture("08-large-"+starId);if(failed)yield break;
                if(!Guard(()=>ops.ScrollPowerDashboard(new Vector2(0,100000))))yield break;
                yield return Capture("09-large-"+starId+"-bottom");if(failed)yield break;
            }
            if (!Guard(() =>
            {
                Require(Campaign() == readonlyCampaign, "rendering, changing resolution and scrolling dashboard preserve campaign resources");
                ops.ClosePanel();
            })) yield break;
            yield return null; yield return null;
            if (!Guard(() =>
            {
                Require(!ops.PowerPanelOpen && !StationOperations.BlocksInput && !SettingsMenu.BlocksInput,
                    "closing power dashboard releases all gameplay input guards on subsequent frame");
                Vector3 feet = mode.FeetPosition; mode.Walk(Vector2.down, .05f);
                Require(Vector3.Distance(feet, mode.FeetPosition) > .03f, "actual CharacterController can walk away after power terminal closes");
                mode.Exit(true);
                Require(!ops.OpenPowerConsole() && !ops.PanelOpen, "tactical outside view cannot remotely open physical power console");
                mode.RestoreInterior(ops.Layout.SpawnPosition, false, ops.Layout.SpawnYaw, 0);
                Require(!ops.OpenPowerConsole() && !ops.PanelOpen, "being inside but away from physical desk cannot open power console");
                ApproachTerminal(); scenario.Scanner.Say("Power acceptance modal fixture", "Isolated test of terminal input ownership.");
                Require(!ops.OpenPowerConsole() && !ops.PanelOpen, "Mira modal prevents a hidden power panel opening beneath dialogue");
                scenario.Scanner.DismissDialogue();
                Require(Campaign() == readonlyCampaign, "terminal navigation and ownership guards grant no resources");
            })) yield break;
            if (!Guard(() =>
            {
                Require(frustumWarnings == 0, "no camera screen-frustum warnings");
                File.WriteAllText(Folder + "/result.json", JsonUtility.ToJson(new Report
                    { passed = true, checks = checks, layout = StationInteriorLayout.Revision, pictures = pictures.ToArray(), frustumWarnings = frustumWarnings }, true));
                Debug.Log("STATION POWER CONSOLE CHECK PASSED: " + checks + " checks / " + pictures.Count + " native framebuffer captures");
                Application.Quit(0);
            })) yield break;
        }
    }
}
#endif
