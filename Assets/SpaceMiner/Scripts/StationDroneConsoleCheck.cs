#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    // Commands use the same physical terminal guard and simulation as the player UI.
    public sealed class StationDroneConsoleCheck : MonoBehaviour
    {
        private const string Folder = "Logs/StationDroneConsole";
        private StationOperations ops;
        private WaterScenario scenario;
        private int checks;
        private float questSeconds;
        private double receiptKg;
        private bool failed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-stationDroneConsoleCheck") >= 0)
                new GameObject("Drone console acceptance").AddComponent<StationDroneConsoleCheck>();
        }
        private void Require(bool value, string message)
        { if (!value) throw new Exception("DRONE CONSOLE: " + message); checks++; Debug.Log("DRONE CONSOLE OK: " + message); }
        private bool Guard(Action action)
        {
            try { action(); return true; }
            catch (Exception e) { failed = true; File.WriteAllText(Folder + "/error.txt", e.ToString()); Debug.LogException(e); Application.Quit(1); return false; }
        }
        private static SavedGame Copy(SavedGame state) => JsonUtility.FromJson<SavedGame>(JsonUtility.ToJson(state));
        private IEnumerator AcknowledgeMiraThroughApi()
        {
            string before = JsonUtility.ToJson(scenario.Worker.CaptureState());
            scenario.Scanner.DismissDialogue(); yield return null;
            Guard(() => Require(scenario.Scanner.Dialogue == null && JsonUtility.ToJson(scenario.Worker.CaptureState()) == before,
                "explicit API acknowledgement closes rendered Mira without replaying or changing the drone mission; native button acceptance is separate"));
        }
        private void OpenRealConsole()
        {
            // Normal save loading deliberately opens Mira's acknowledgement. The
            // operator dismisses that modal before approaching/using another desk.
            if (scenario.Scanner.Dialogue != null)
            {
                Require(!ops.DroneConsoleCanCommand, "Mira acknowledgement blocks commands until dismissed");
                scenario.Scanner.DismissDialogue();
            }
            ops.ClosePanel(); StationInteriorMode.Current.CloseConsole();
            var terminal = Array.Find(ops.GetComponentsInChildren<StationTerminal>(), t => t.kind == StationTerminalKind.Drones);
            Require(terminal != null, "physical drone terminal exists");
            var feet = terminal.transform.TransformPoint(new Vector3(0, .04f, -1.55f));
            StationInteriorMode.Current.RestoreInterior(feet, false, terminal.transform.eulerAngles.y, 0);
            Physics.SyncTransforms();
            Require(ops.NearestTerminal(StationInteriorMode.Current.FeetPosition) == terminal, "physical drone terminal reachable by E interaction");
            ops.OpenTerminal(terminal);
            Require(ops.DroneConsoleCanCommand, "inside drone terminal authorizes command facade");
        }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.25f); yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Folder + "/" + name + ".png"));
            yield return new WaitForSecondsRealtime(.4f);
            Guard(() =>
            {
                var image = new Texture2D(2, 2);
                try
                {
                    Require(image.LoadImage(File.ReadAllBytes(Folder + "/" + name + ".png")), name + " screenshot readable");
                    int bright = 0; foreach (var p in image.GetPixels32()) if (p.r > 40 || p.g > 40 || p.b > 40) bright++;
                    Require(bright > 1000, name + " contains rendered UI");
                }
                finally { Destroy(image); }
            });
        }
        private string MissionState() => "t=" + questSeconds + " phase=" + scenario.Worker.Phase + " battery=" + scenario.Worker.BatteryKwh
            + " fuel=" + scenario.Worker.FuelLiters + " water=" + scenario.WaterLiters + " stationBattery=" + ops.State.power.storedKwh
            + " plant=" + ops.IcePlant.Status + " reason=" + scenario.Worker.ReturnReason;
        private IEnumerator Start()
        {
            Directory.CreateDirectory(Folder); File.Delete(Folder + "/result.json"); File.Delete(Folder + "/error.txt");
            yield return new WaitForSecondsRealtime(1);
            if (!Guard(() =>
            {
                FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride = Path.GetFullPath(Folder + "/Saves");
                CinematicLibrary.PreferencePrefix = "SpaceMiner.Test.DroneConsole.";
                FindFirstObjectByType<StartMenu>().StartDemo(); FindFirstObjectByType<IntroSequence>().Skip();
                FindFirstObjectByType<WaterScenario>().SetSimulationRate(0);
            })) yield break;
            yield return null; yield return null;
            if (!Guard(() =>
            {
                scenario = FindFirstObjectByType<WaterScenario>(); ops = scenario.Operations; scenario.SetSimulationRate(0); scenario.Scanner.DismissDialogue();
                Require(ops.Live, "live operations enabled for isolated console check");
                Require(Math.Abs(scenario.WaterLiters - 20) < .001 && Math.Abs(ops.State.power.storedKwh - 12) < .01 && ops.IcePlant.ReceivedWaterKg == 0, "normal initial water, battery and empty plant; no resource fixtures");
                int active = 0; foreach (var d in scenario.Drones) if (d.IsOperational) active++;
                Require(active == 1 && scenario.Worker.IsOperational && !ops.State.helper && !ops.State.second, "only external 01 starts active");
                Require(ops.ConsoleWaterTargets().Count == 0, "unscanned sources excluded from console targets");
                var mode = StationInteriorMode.Current; mode.RestoreInterior(ops.Layout.SpawnPosition, false, ops.Layout.SpawnYaw, 0);
                Require(mode.OpenConsole(), "actual scanner console opens at physical command-room approach");
                scenario.Advance(11); Require(scenario.Scanner.Scan(), "real initial scan charges from station and grants actual water knowledge");
                scenario.Scanner.DismissDialogue(); mode.CloseConsole(); OpenRealConsole();
                Require(ops.SelectConsoleDrone("drone-0") && !ops.SelectConsoleDrone("drone-01") && !ops.SelectConsoleDrone("unknown"), "fleet selection accepts stable IDs and rejects aliases");
                Require(ops.ConsoleTargets(StationJobKind.Salvage).Count == 5, "scan exposes five finite unknown wreck contacts");
                string droneBefore = JsonUtility.ToJson(scenario.Worker.CaptureState()), campaignBefore = JsonUtility.ToJson(ops.Capture());
                var view = ops.ConsoleDrone("drone-0"); ops.ConsoleWaterTargets(); ops.ConsoleTargets(StationJobKind.Repair); ops.ConsoleOrders("drone-0");
                Require(droneBefore == JsonUtility.ToJson(scenario.Worker.CaptureState()) && campaignBefore == JsonUtility.ToJson(ops.Capture()), "fleet read model and target queries do not mutate simulation or create routes");
                Require(view.HasOnboardStores && view.BatteryKwh == scenario.Worker.BatteryKwh && view.FuelLiters == scenario.Worker.FuelLiters && view.CargoKg == scenario.Worker.CargoKg, "overview uses actual stores");
                Require(!ops.ConsoleDrone("R-01").HasOnboardStores, "internal R01 has no invented independent battery or water inventory");
                Require(!ops.TryConsoleWaterOrder("debris-0", out string wrong) && wrong.Contains("Eisquelle") && campaignBefore == JsonUtility.ToJson(ops.Capture()), "water command rejects a wreck target without resource or queue changes");
                ops.ClosePanel(); mode.Exit(true);
                Require(!ops.TryConsoleWaterOrder("A-01", out _) && !ops.TryConsoleOrder(StationJobKind.Salvage, "debris-0", out _)
                    && !ops.TryConsoleRefit("Greifer", out _) && !ops.DeliverConsoleCargo(out _) && !ops.ApplyConsoleSizingAtDock(out _), "VR and closed terminal cannot issue drone commands");
                OpenRealConsole(); var targets = ops.ConsoleWaterTargets();
                Require(targets.Exists(t => t.Id == "A-01"), "console exposes saved asteroid identity A-01");
                SettingsStore.Current.Gameplay.PauseInMenu = true; SettingsStore.Current.Interface.Scale = 1;
                Screen.SetResolution(1440, 900, FullScreenMode.Windowed);
            })) yield break;
            yield return new WaitForSecondsRealtime(.3f);
            Screen.SetResolution(1600, 1000, FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(.6f);
            if (!Guard(() =>
            {
                Require(ops.TryConsoleWaterOrder("A-01", out string started), "existing guarded water command accepts actual A01 mission: " + started);
                Require(scenario.Worker.Phase == DronePhase.Launching && scenario.Worker.HasTankOrder && scenario.Worker.Target.name == "A-01" && scenario.Worker.CaptureState().cargoGoal > 1033, "real command starts genuine reserve-checked A01 mission");
                Require(ops.ConsoleDrone("drone-0").HasActivity && ops.ConsoleDrone("drone-0").ActivityId == "water:drone-0", "active flight read model links the actual water mission");
                Require(!ops.PanelOpen && scenario.Scanner.Dialogue != null && !ops.DroneConsoleCanCommand, "successful water command yields panel ownership to Mira without hidden menu pause");
            })) yield break;
            int acknowledgementFrame = Time.frameCount;
            yield return null; yield return null;
            if (!Guard(() =>
            {
                Require(scenario.Scanner.Dialogue != null && scenario.Scanner.DialogueCanRender && scenario.Scanner.LastDialogueRenderedFrame > acknowledgementFrame, "mission acknowledgement remains visible for at least two real frames without direct dismissal");
                Require(!SettingsMenu.BlocksInput && !SettingsMenu.PausesSimulation && !ops.DroneConsoleCanCommand, "no invisible station panel owns input or menu pause underneath Mira");
                Require(!ops.TryConsoleWaterOrder("A-01", out _), "Mira acknowledgement rejects a duplicate command while visible");
            })) yield break;
            yield return Capture("00-water-command-mira-visible"); if (failed) yield break;
            yield return AcknowledgeMiraThroughApi(); if (failed) yield break;
            float phaseBeforeResume = scenario.Worker.PhaseElapsed;
            scenario.SetSimulationRate(1); yield return new WaitForSecondsRealtime(.2f); scenario.SetSimulationRate(0);
            if (!Guard(() =>
            {
                Require(scenario.Worker.PhaseElapsed > phaseBeforeResume && !ops.PanelOpen, "normal Update resumes mission after explicit acknowledgement with default menu pause enabled");
                OpenRealConsole(); Require(!ops.TryConsoleWaterOrder("A-01", out string busy) && busy.Contains("bereits"), "busy drone gives concrete rejection after normal terminal reopen");
            })) yield break;
            yield return Capture("01-extern-eisflug-gestartet"); if (failed) yield break;
            ops.RequestConsoleTargetFilterFocus(); yield return new WaitForSecondsRealtime(.2f);
            if (!Guard(() =>
            {
                Require(ops.ConsoleActualFilterFocus && StationOperations.DroneConsoleTextInputActive, "real named target filter owns text focus and suppresses E-to-close shortcut");
                ops.SelectConsoleDrone("drone-1"); Require(!StationOperations.DroneConsoleTextInputActive, "selection change clears previous target-filter shortcut guard");
                ops.SelectConsoleDrone("drone-0");
            })) yield break;
            ops.RequestConsoleTargetFilterFocus(); yield return new WaitForSecondsRealtime(.2f);
            if (!Guard(() =>
            {
                Require(ops.ConsoleActualFilterFocus && StationOperations.DroneConsoleTextInputActive, "filter can regain actual GUI focus after selection change");
                ops.ClosePanel(); Require(!StationOperations.DroneConsoleTextInputActive, "closed console has no stale target-filter keyboard guard");
                OpenRealConsole(); Require(!StationOperations.DroneConsoleTextInputActive, "reopened console clears stale filter focus");
            })) yield break;
            ops.ConsoleDetailTab = 1; yield return Capture("02-externe-ausruestung"); if (failed) yield break;
            ops.ConsoleDetailTab = 2; yield return Capture("03-externe-diagnose"); if (failed) yield break;
            ops.SelectConsoleDrone("drone-1"); yield return Capture("04-drohne-02-voraussetzungen"); if (failed) yield break;
            ops.SetConsoleDomain(true); ops.ConsoleDetailTab = 0; yield return Capture("05-interne-flotte"); if (failed) yield break;
            ops.ConsoleDetailTab = 1; yield return Capture("06-interne-faehigkeiten"); if (failed) yield break;
            if (!Guard(() => { ops.SetConsoleDomain(false); Require(ops.ConsoleSelectedDroneId == "drone-1", "domain switch preserves last external stable selection"); ops.SelectConsoleDrone("drone-0"); ops.ConsoleDetailTab = 0; })) yield break;
            ops.ClosePanel(); StationInteriorMode.Current.Exit(true); ops.SelectTacticalTab(TacticalOverviewTab.Quests);
            yield return Capture("07-vr-erster-auftrag"); if (failed) yield break;
            // Only elapsed simulation time advances the first quest: no water, power,
            // helper, tool or material fixtures and no direct plant receipt are used.
            DronePhase previousPhase = scenario.Worker.Phase;
            bool deliveryModalArmed = false, deliveryModalChecked = false;
            while (questSeconds < 26000 && !ops.State.waterSecured)
            {
                if (!deliveryModalArmed && scenario.Worker.Phase == DronePhase.Unloading && scenario.Deliveries == 0)
                {
                    if (!Guard(() => { OpenRealConsole(); SettingsStore.Current.Gameplay.PauseInMenu = false; deliveryModalArmed = true; })) yield break;
                }
                if (!Guard(() =>
                {
                    scenario.Advance(40); questSeconds += 40;
                    if (!deliveryModalArmed || deliveryModalChecked) scenario.Scanner.DismissDialogue();
                    if (scenario.Worker.Phase != previousPhase) { Debug.Log("DRONE CONSOLE QUEST TRACE: " + MissionState()); previousPhase = scenario.Worker.Phase; }
                    if (scenario.Worker.Phase == DronePhase.Stranded) throw new Exception("Natural first mission stranded: " + MissionState());
                    if (!ResourceContainer.Finite(scenario.WaterLiters) || scenario.WaterLiters < 0 || scenario.WaterLiters > scenario.TankCapacityLiters + .001f
                        || ops.State.power.storedKwh < 0 || ops.State.power.storedKwh > ops.State.power.capacityKwh + .001f) throw new Exception("Resource bounds: " + MissionState());
                })) yield break;
                if (deliveryModalArmed && !deliveryModalChecked && scenario.Deliveries > 0)
                {
                    int deliveryFrame = Time.frameCount;
                    yield return null; yield return null;
                    if (!Guard(() =>
                    {
                        Require(ops.PanelOpen && scenario.Scanner.Dialogue != null, "real first raw-delivery notification arrives while drone terminal remains open");
                        Require(scenario.Scanner.DialogueCanRender && scenario.Scanner.LastDialogueRenderedFrame > deliveryFrame && !ops.DroneConsoleCanCommand, "open terminal cannot hide or swallow Mira; dialogue actually renders and blocks commands for two frames");
                    })) yield break;
                    yield return Capture("11-delivery-mira-over-open-terminal"); if (failed) yield break;
                    yield return AcknowledgeMiraThroughApi(); if (failed) yield break;
                    if (!Guard(() =>
                    {
                        Require(ops.PanelOpen && ops.DroneConsoleCanCommand, "explicit Mira acknowledgement restores the existing drone terminal without forced-close workaround");
                        Require(scenario.Worker.Target.name == "A-01" && scenario.Worker.HasTankOrder, "delivery notification preserves the existing mission target and does not issue a different trip");
                        ops.ClosePanel(); StationInteriorMode.Current.Exit(true); SettingsStore.Current.Gameplay.PauseInMenu = true;
                        deliveryModalChecked = true;
                    })) yield break;
                }
                if (((int)questSeconds) % 200 == 0) yield return null;
            }
            if (!Guard(() =>
            {
                receiptKg = ops.IcePlant.ReceivedWaterKg + ops.IcePlant.ReceivedRestKg;
                Require(ops.State.waterSecured && scenario.Deliveries > 0 && scenario.WaterLiters >= 40, "first quest completes naturally through real flight, raw receipt and paid processing: " + MissionState());
                Require(deliveryModalChecked, "first-delivery modal regression exercised during the natural water quest");
                Require(receiptKg > 1000 && ops.IcePlant.EnergyKwh > 0 && ops.IcePlant.ExportedWaterKg > 20, "full-scale raw delivery and paid clean-water output remain distinct");
                Require(scenario.TankCapacityLiters == 200 && ops.IcePlant.ColdRawKg < 1100 && !ops.State.helper && !ops.State.second, "first quest needs neither giant full storage nor free helper/second activation");
                Require(StationIcePlant.Valid(ops.IcePlant.Capture()) && StationOperations.Valid(ops.Capture()), "natural first-quest mass ledger and campaign state valid");
            })) yield break;
            yield return Capture("08-vr-wasserversorgung-gesichert"); if (failed) yield break;
            if (!Guard(CheckCommandsAndQueue)) yield break;
            OpenRealConsole(); ops.SetConsoleDomain(false); ops.SelectConsoleDrone("drone-0"); ops.ConsoleDetailTab = 0; ops.ConsoleCompactOrders = false;
            if (!Guard(() => { SettingsStore.Current.Accessibility.TextScale = 1.4f; Screen.SetResolution(1280, 720, FullScreenMode.Windowed); CheckLayout(); })) yield break;
            yield return new WaitForSecondsRealtime(.5f); yield return Capture("09-grosse-schrift-1280"); if (failed) yield break;
            if (!Guard(() =>
            {
                Require(ops.ConsoleLastLayout.Compact && ops.ConsoleDetailsVisible && !ops.ConsoleOrdersVisible, "real 1280-pixel window reflows to a broad details pane despite wide virtual UI canvas");
                CheckRenderedLayout();
            })) yield break;
            ops.ConsoleCompactOrders = true;
            yield return Capture("10-kleine-ansicht-auftraege"); if (failed) yield break;
            if (!Guard(() =>
            {
                Require(ops.ConsoleLastLayout.Compact && !ops.ConsoleDetailsVisible && ops.ConsoleOrdersVisible, "compact navigation actually replaces details with orders at same resolution");
                CheckRenderedLayout(); CheckDifferentCaptures();
                File.WriteAllText(Folder + "/result.json", "{\"passed\":true,\"checks\":" + checks + ",\"naturalWaterQuestSeconds\":" + questSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + ",\"receivedRawKg\":" + receiptKg.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ",\"scope\":\"guarded actual water command, two-frame real Mira render over closed/open station terminal, explicit API acknowledgement and natural Update resume; paid first water quest, read-only fleet, real queue/refit/cancel and responsive captures; native mouse/keyboard button acceptance is separate\"}");
            })) yield break;
            Application.Quit(0);
        }
        private void CheckCommandsAndQueue()
        {
            // Separate command fixtures start only after the natural first-quest proof.
            var baseline = SaveGameStore.Capture(scenario, "console command baseline", "after natural first quest");
            OpenRealConsole();
            ops.SelectConsoleDrone("drone-2"); int count = ops.State.orders.Count;
            Require(!ops.TryConsoleOrder(StationJobKind.Salvage, "debris-0", out _) && ops.State.orders.Count == count, "locked 03 cannot silently route work to 01");
            ops.SelectConsoleDrone("drone-0");
            string tool = ops.State.tools[0]; int stock = ops.State.spareGrippers;
            Require(ops.TryConsoleRefit("Greifer", out string why) && why.Contains("R-01"), "real refit may wait transparently for R01");
            var refit = ops.State.orders[ops.State.orders.Count - 1];
            Require(ops.State.tools[0] == tool && ops.State.spareGrippers == stock && !refit.started, "refit click neither changes tool nor grants or consumes hardware early");
            Require(ops.ConsoleOrders("drone-0").Contains(refit) && ops.ConsoleOrders("R-01").Contains(refit), "shared actual refit visible for target external drone and R01 service");
            Require(!ops.TryConsoleRefit("Greifer", out _), "duplicate refit rejected");
            Require(!ops.TryConsoleRefit("Laser", out _), "unresearched laser or conflicting refit rejected");
            Require(ops.CancelConsoleOrder(refit.id, out _) && refit.complete && refit.cancelled && ops.State.tools[0] == tool, "pending refit cancels through existing queue with unchanged hardware");
            Require(ops.RemoveConsoleHistory(refit.id, out _) && !ops.State.orders.Contains(refit) && ops.State.tools[0] == tool && ops.State.spareGrippers == stock, "cancelled completed refit history removes without touching hardware");
            Require(!ops.RemoveConsoleHistory(refit.id, out _), "removed history cannot be replayed or removed twice");
            ops.SetConsoleDomain(true);
            Require(ops.TryConsoleOrder(StationJobKind.PrintParts, "", out _) && ops.TryConsoleOrder(StationJobKind.Recycle, "", out _), "internal production uses real recipe queue even while R01 is offline");
            var pending = ops.ConsoleOrders("R-01").FindAll(o => !o.started && !o.complete); var first = pending[pending.Count - 2]; var second = pending[pending.Count - 1];
            Require(ops.MoveConsoleOrder(second.id, -1, out _) && ops.State.orders.IndexOf(second) < ops.State.orders.IndexOf(first), "up priority changes actual pending queue");
            Require(ops.PlaceConsoleOrder(second.id, first.id, out _) && ops.State.orders.IndexOf(second) > ops.State.orders.IndexOf(first), "drag placement changes actual queue, no shadow priorities");
            Require(ops.CancelConsoleOrder(second.id, out _) && second.complete && second.cancelled, "pending internal cancellation uses real order state");
            Require(!ops.RemoveConsoleHistory(first.id, out _), "waiting production order cannot be removed as history");
            Require(ops.TryConsoleOrder(StationJobKind.ActivateHelper, "", out _), "R01 real commissioning can be queued");
            scenario.Advance(.25f); var commissioning = ops.State.orders.Find(o => !o.complete && o.kind == StationJobKind.ActivateHelper);
            Require(commissioning.started && !ops.State.helper && commissioning.reserved.Length == 1, "commissioning reserves real parts before completion");
            Require(!ops.RemoveConsoleHistory(commissioning.id, out _), "active commissioning with reserved parts cannot be removed");
            Require(!ops.MoveConsoleOrder(commissioning.id, -1, out _) && !ops.PlaceConsoleOrder(commissioning.id, first.id, out _), "active work cannot be reordered or dragged");
            Require(ops.RecallConsoleDrone(out _) && commissioning.cancelled, "internal recall requests backend safe completion");
            Require(!ops.RemoveConsoleHistory(commissioning.id, out _), "cancel request cannot bypass safe finish and material return");
            scenario.Advance(.25f);
            Require(commissioning.complete && (commissioning.reserved == null || commissioning.reserved.Length == 0) && ops.RemoveConsoleHistory(commissioning.id, out _), "cancelled running work removes only after safe completion and released reservation");
            Require(ops.TryConsoleOrder(StationJobKind.ActivateHelper, "", out _), "real new commissioning can follow cancelled history");
            var finished = ops.State.orders[ops.State.orders.Count - 1]; scenario.Advance(121);
            Require(finished.complete && !finished.cancelled && ops.State.helper, "commissioning really completes before successful history removal");
            float partsAfter = ops.State.inventory.parts.amount;
            Require(ops.RemoveConsoleHistory(finished.id, out _) && ops.State.helper && ops.State.inventory.parts.amount == partsAfter, "completed successful history removes without replaying outputs or refunding consumed parts");
            ops.SelectConsoleDrone("drone-1"); float battery = scenario.Drones[1].BatteryKwh, fuel = scenario.Drones[1].FuelLiters;
            Require(ops.TryConsoleOrder(StationJobKind.ActivateSecond, "", out _) && !ops.State.second && scenario.Drones[1].BatteryKwh == battery && scenario.Drones[1].FuelLiters == fuel, "02 real commissioning queue grants neither activation nor free dock stores");
            var secondJob = ops.State.orders[ops.State.orders.Count - 1]; Require(ops.CancelConsoleOrder(secondJob.id, out _), "02 pending commissioning cancels normally");
            CheckLegacyFoodHistory();
            SaveGameStore.Apply(Copy(baseline), scenario); OpenRealConsole();
            Require(ops.State.orders.Count == baseline.stationCampaign.orders.Count && ops.State.helper == baseline.stationCampaign.helper, "isolated command fixtures restore pre-test campaign through normal save loader");
        }
        private void CheckLegacyFoodHistory()
        {
            ops.SetConsoleDomain(true);
            // A historical record from the old schema is evidence, not a request to
            // manufacture food. Neither deletion nor migration may replay its recipe.
            var record = new StationWorkOrder(StationJobKind.Food, "")
            {
                started = true, complete = true, actorId = "R-01", elapsed = StationRecipes.Get(StationJobKind.Food).Seconds,
                status = "Legacy: abgeschlossen", reserved = null
            };
            ops.State.foodProduced = false; ops.State.orders.Add(record);
            float food = ops.State.inventory.food.amount, water = scenario.WaterLiters, carbon = ops.State.inventory.carbon.amount;
            Require(ops.GetTacticalQuests()[6].Complete, "legacy successful food record proves completed food quest");
            Require(ops.RemoveConsoleHistory(record.id, out _) && ops.State.foodProduced && ops.GetTacticalQuests()[6].Complete, "deleting legacy food history preserves durable quest completion");
            Require(ops.State.inventory.food.amount == food && scenario.WaterLiters == water && ops.State.inventory.carbon.amount == carbon, "legacy food history deletion creates no food and charges no ingredients");
            var saved = SaveGameStore.Capture(scenario, "food completion round trip", "history already removed");
            Require(saved.stationCampaign.foodProduced, "normal capture retains food completion after record removal");
            SaveGameStore.Apply(Copy(saved), scenario); OpenRealConsole();
            Require(ops.State.foodProduced && ops.GetTacticalQuests()[6].Complete && !ops.State.orders.Exists(o => o.id == record.id), "normal save restore retains completed food quest without history record");
            Require(ops.State.inventory.food.amount == food && scenario.WaterLiters == water && ops.State.inventory.carbon.amount == carbon, "food completion save restore does not replay historical output");
        }
        private void CheckLayout()
        {
            foreach (var size in new[] { new Vector2(1600, 1000), new Vector2(1280, 720), new Vector2(1024, 720), new Vector2(640, 720) })
            {
                var layout = DroneConsoleLayout.For(size.x, size.y);
                foreach (var r in new[] { layout.Header, layout.Domain, layout.Fleet, layout.Details, layout.Orders, layout.Footer })
                    Require(r.x >= 0 && r.y >= 0 && r.xMax <= size.x && r.yMax <= size.y && r.width > 0 && r.height > 0, "console panel inside viewport " + size);
                Require(!layout.Fleet.Overlaps(layout.Details) && layout.Details.yMax < layout.Footer.y && layout.Orders.yMax < layout.Footer.y, "fleet/details/footer separated " + size);
                Require(layout.Compact || !layout.Details.Overlaps(layout.Orders), "details and orders either separate or explicit compact tabs " + size);
            }
        }
        private void CheckRenderedLayout()
        {
            var layout = ops.ConsoleLastLayout;
            float width = UiLayout.Width - 32, height = UiLayout.Height - 32;
            foreach (var r in new[] { layout.Header, layout.Domain, layout.Fleet, layout.Details, layout.Orders, layout.Footer, layout.CompactNavigation })
                Require(r.x >= 0 && r.y >= 0 && r.xMax <= width && r.yMax <= height && r.width > 0 && r.height > 0, "actual compact panel remains inside rendered window with enlarged text");
            Require(layout.Fleet.xMax < layout.Details.xMin && layout.CompactNavigation.yMax < layout.Details.yMin && layout.Details.yMax < layout.Footer.yMin,
                "actual compact fleet, navigation, scroll pane and footer remain separate");
        }
        private void CheckDifferentCaptures()
        {
            var details = new Texture2D(2, 2); var orders = new Texture2D(2, 2);
            try
            {
                Require(details.LoadImage(File.ReadAllBytes(Folder + "/09-grosse-schrift-1280.png")) && orders.LoadImage(File.ReadAllBytes(Folder + "/10-kleine-ansicht-auftraege.png")), "both compact navigation images decode");
                Require(details.width == orders.width && details.height == orders.height, "compact details and orders use identical framebuffer dimensions");
                var a = details.GetPixels32(); var b = orders.GetPixels32(); int changed = 0;
                for (int y = details.height / 6; y < details.height * 4 / 5; y++) for (int x = details.width / 3; x < details.width * 9 / 10; x++)
                {
                    int i = y * details.width + x;
                    if (Math.Abs(a[i].r - b[i].r) + Math.Abs(a[i].g - b[i].g) + Math.Abs(a[i].b - b[i].b) > 60) changed++;
                }
                Require(changed > 1000, "actual compact detail and order panes render different content, not two captures of unchanged layout");
            }
            finally { Destroy(details); Destroy(orders); }
        }
    }
}
#endif
