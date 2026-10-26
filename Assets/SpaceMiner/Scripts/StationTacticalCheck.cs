#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace SpaceMiner
{
    public sealed class StationTacticalCheck : MonoBehaviour
    {
        private const string Folder = "Logs/StationTactical";
        private StationOperations ops;
        private WaterScenario scenario;
        private OrbitCamera orbit;
        private StationInteriorMode mode;
        private AsteroidResource known;
        private int checks;
        private bool failed;
        [Serializable] private sealed class Report
        {
            public bool passed;
            public int checks, actualKnownAsteroids, pagedReferenceFixture;
            public string scope = "Actual first scan, knowledge-safe filters/search/paging, active external/internal fleet and real campaign quest progress. Explicit focus snapshots, actual launched drone tracking and stopping, real R-01 identity guard, unchanged domain snapshots during observation/navigation. A declared100000-reference fixture checks bounded page/index behaviour, not100000 rendered bodies or FPS. Real native framebuffer captures at1600x1000 and1280x720 with enlarged text. Quest threshold and R-01 readiness use declared campaign-state fixtures; no new economy, VR-headset or manual comfort acceptance.";
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        { if (Array.IndexOf(Environment.GetCommandLineArgs(), "-stationTacticalCheck") >= 0) new GameObject("Tactical outside overview acceptance").AddComponent<StationTacticalCheck>(); }
        private void Require(bool value, string label)
        { if (!value) throw new Exception("TACTICAL: " + label); checks++; Debug.Log("TACTICAL OK: " + label); }
        private bool Guard(Action action)
        {
            try { action(); return true; }
            catch (Exception exception) { failed = true; File.WriteAllText(Folder + "/error.txt", exception.ToString()); Debug.LogException(exception); Application.Quit(1); return false; }
        }
        private static T Copy<T>(T value) => JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
        private string DomainSignature()
        {
            var state = SaveGameStore.Capture(scenario, "Read-only observation", ""); state.savedAt = "";
            return JsonUtility.ToJson(state);
        }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.2f); yield return new WaitForEndOfFrame();
            string path = Path.GetFullPath(Folder + "/" + name + ".png"); ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSecondsRealtime(.35f);
            Guard(() =>
            {
                var pixels = new Texture2D(2, 2);
                try
                {
                    Require(File.Exists(path) && pixels.LoadImage(File.ReadAllBytes(path)), "actual screenshot readable / " + name);
                    Require(pixels.width == Screen.width && pixels.height == Screen.height, "native framebuffer dimensions / " + name);
                    int lit = 0; foreach (var p in pixels.GetPixels32()) if (p.r > 45 || p.g > 45 || p.b > 45) lit++;
                    Require(lit > 1000, "actual scene and tactical UI rendered / " + name);
                }
                finally { Destroy(pixels); }
            });
        }
        private void CheckFreshKnowledge()
        {
            Require(ops.Live, "real live campaign enabled for isolated flag");
            Require(SettingsStore.PathName.StartsWith(Path.GetFullPath(Folder), StringComparison.OrdinalIgnoreCase), "settings isolated from user preferences");
            foreach (var plan in ops.RoomPlans) Require(plan.SavePath.StartsWith(Path.GetFullPath(Folder), StringComparison.OrdinalIgnoreCase), "room plan isolated / " + plan.RoomId);
            Require(!scenario.Scanner.FirstScanComplete, "new campaign has no scan knowledge");
            Require(ops.GetTacticalContacts(TacticalObjectFilter.Station).TotalCount > 15, "station structure available without remote work buttons");
            Require(ops.GetTacticalContacts(TacticalObjectFilter.Asteroids).TotalCount == 0, "unscanned asteroid catalogue hidden");
            Require(ops.GetTacticalContacts(TacticalObjectFilter.Wrecks).TotalCount == 0, "unknown wreck catalogue hidden");
            scenario.Scanner.SetDebugVisibility(true);
            Require(ops.GetTacticalContacts(TacticalObjectFilter.Asteroids | TacticalObjectFilter.Wrecks).TotalCount == 0,
                "debug full visibility does not manufacture contact knowledge");
            Require(!ops.FocusTacticalContact("asteroid-0") && !ops.SelectTacticalObject("debris-0"), "unknown contacts rejected by navigation APIs too");
            Require(ops.GetTacticalDrones().Count == 1 && ops.GetTacticalDrones()[0].Id == "drone-0", "only actual operative drone01 in separate fleet");
            Require(!ops.FollowTacticalDrone("drone-1") && !ops.FollowTacticalDrone("station") && !ops.FollowTacticalDrone("R-01"), "inactive or non-drone follow requests rejected");
            Require(!orbit.FollowInternalDrone(ops.Layout.transform), "an arbitrary station transform cannot masquerade as R-01");
            CheckExteriorGuards();
            var before = DomainSignature();
            Require(ops.FocusTacticalContact("station") && !orbit.IsFollowing, "station focus is one snapshot");
            Require(ops.SelectTacticalObject("command") && ops.TacticalSelectedObject == "command", "world selection can change without moving camera");
            var point = orbit.Pivot; Require(ops.SelectTacticalObject("water") && orbit.Pivot == point, "selection preserves camera pivot");
            ops.GetTacticalQuests(); ops.GetTacticalDrones(); ops.SelectTacticalTab(TacticalOverviewTab.Quests); ops.StopTacticalFollow();
            Require(DomainSignature() == before, "queries, focus, selection and tabs do not mutate saved campaign or drone state");
            scenario.Scanner.SetDebugVisibility(false); ops.SetTacticalContactQuery(TacticalObjectFilter.All); ops.FocusTacticalContact("station"); orbit.Orbit(new Vector2(-8, -28));
        }
        private static Vector3 ScreenPoint(Vector2 point) => new Vector3(point.x * UiLayout.Scale(), Screen.height - point.y * UiLayout.Scale(), 0);
        private void CheckExteriorGuards()
        {
            Require(StationOperations.TacticalViewActive && scenario.Scanner.VirtualView, "actual tactical outside context active");
            Rect toolbar = scenario.Scanner.VirtualToolbarRect;
            Require(!toolbar.Overlaps(ops.TacticalOverviewBounds), "virtual toolbar and tactical panel do not overlap");
            Require(scenario.Scanner.OwnsScreenPoint(ScreenPoint(toolbar.center)), "actual virtual toolbar captures its pointer area");
            Require(StationOperations.OwnsScreenPoint(ScreenPoint(ops.TacticalOverviewBounds.center)), "actual tactical panel captures its pointer area");
            var planner = FindFirstObjectByType<CommandRoomPlanner>();
            Require(planner != null && !CommandRoomPlanner.IsOpen && !planner.Open() && !CommandRoomPlanner.IsOpen,
                "room planning cannot open from read-only tactical view");
            var tree = FindFirstObjectByType<TechTreeMenu>();
            Require(tree != null, "real technology system installed on settings UI root");
            Require(!TechTreeMenu.ShortcutAllowed && !TechTreeMenu.IsOpen, "technology shortcut denied in tactical view");
            tree.ToggleFromShortcut(); Require(!TechTreeMenu.IsOpen, "actual technology shortcut handler stays closed outside");
            var terminal = Array.Find(ops.GetComponentsInChildren<StationTerminal>(), item => item.kind == StationTerminalKind.Drones);
            ops.OpenTerminal(terminal);
            Require(!ops.PanelOpen && !ops.DroneConsoleCanCommand, "physical drone console cannot be opened or commanded remotely");
        }
        private void PerformRealScan()
        {
            mode.RestoreInterior(ops.Layout.SpawnPosition, false, ops.Layout.SpawnYaw, 0);
            Require(mode.NearConsole && mode.OpenConsole(), "physical scanner console reached");
            scenario.Scanner.Advance(15);
            Require(scenario.Scanner.Ready && scenario.Scanner.Scan(), "real powered first scan");
            scenario.Scanner.DismissDialogue(); mode.CloseConsole(); mode.Exit();
            known = Array.Find(scenario.Asteroids, rock => rock.IsScanned && rock.WaterIdentified && rock.IsStarterWaterSource);
            Require(known != null, "actual starter source known after scan");
            int count = Array.FindAll(scenario.Asteroids, rock => rock.IsScanned).Length;
            var page = ops.GetTacticalContacts(TacticalObjectFilter.Asteroids, "", 0, 4);
            Require(page.TotalCount == count && page.Items.Count <= 4, "catalogue matches actual IsScanned domain flags and bounded rows");
            var seen = new HashSet<string>();
            for (int i = 0; i < page.PageCount; i++) foreach (var contact in ops.GetTacticalContacts(TacticalObjectFilter.Asteroids, "", i, 4).Items)
            {
                Require(seen.Add(contact.Id), "page IDs do not repeat / " + contact.Id);
                int index = int.Parse(contact.Id.Substring(9)); Require(scenario.Asteroids[index].IsScanned, "every row has actual scan knowledge");
                Require(!contact.Knowledge.Contains("Restdichte") && !contact.Knowledge.Contains("Ersatzteile"), "contact rows do not expose unknown material contents");
            }
            Require(seen.Count == count, "all known contacts reachable through pages");
            Require(ops.GetTacticalContacts(TacticalObjectFilter.Wrecks).TotalCount == 5, "first scan exposes five unknown wreck contacts");
            foreach (var contact in ops.GetTacticalContacts(TacticalObjectFilter.Wrecks).Items)
                Require(contact.Knowledge.Contains("Inhalt unbekannt") && !contact.Knowledge.Contains("12 kg"), "wreck content remains unknown / " + contact.Id);
            var unknown = Array.Find(scenario.Asteroids, rock => !rock.IsScanned);
            if (unknown != null)
            {
                int index = Array.IndexOf(scenario.Asteroids, unknown); scenario.Scanner.SetDebugVisibility(true);
                Require(!ops.FocusTacticalContact("asteroid-" + index), "debug cannot focus an unscanned row through tactical API");
                Require(!unknown.WaterIdentified && !unknown.IsScanned, "debug has not granted analysis knowledge"); scenario.Scanner.SetDebugVisibility(false);
            }
            var result = ops.GetTacticalContacts(TacticalObjectFilter.Asteroids, known.Info.DisplayName);
            Require(result.TotalCount > 0, "case-insensitive name search finds scanned source");
            Require(ops.GetTacticalContacts(TacticalObjectFilter.All, "no-such-contact-9876").TotalCount == 0, "empty search result remains empty");
            Require(ops.GetTacticalContacts(TacticalObjectFilter.None).TotalCount == 0, "all filters can be deliberately hidden");
            Require(ops.GetTacticalContacts(TacticalObjectFilter.All, "", int.MaxValue, 999).Items.Count <= 24, "adversarial page parameters bounded safely");
            ops.SetTacticalContactQuery(TacticalObjectFilter.Asteroids); ops.FocusTacticalContact("station");
        }
        private void CheckLargeReferencePaging()
        {
            var original = scenario.Asteroids; var property = typeof(WaterScenario).GetProperty("Asteroids", BindingFlags.Instance | BindingFlags.Public);
            Require(property != null, "large catalogue fixture can be isolated without scene construction");
            try
            {
                // Stress the index only. These are explicitly100000 references to an existing
                // scanned source, not100000 rendered or simulated asteroid game objects.
                var fixture = new AsteroidResource[100000]; for (int i = 0; i < fixture.Length; i++) fixture[i] = known;
                property.SetValue(scenario, fixture); ops.RefreshTacticalContacts();
                var first = ops.GetTacticalContacts(TacticalObjectFilter.Asteroids, "", 0, 8);
                Require(first.TotalCount == fixture.Length && first.Items.Count == 8 && first.PageCount == 12500, "100000-reference catalogue yields only8 GUI contact models");
                int revision = ops.TacticalQueryRevision;
                for (int i = 0; i < 12; i++) ops.GetTacticalContacts(TacticalObjectFilter.Asteroids, "", 0, 8);
                Require(ops.TacticalQueryRevision == revision, "repaints reuse matching index instead of rescanning100000 entries");
                var last = ops.GetTacticalContacts(TacticalObjectFilter.Asteroids, "", int.MaxValue, 8);
                Require(last.PageIndex == 12499 && last.Items.Count == 8 && last.Items[7].Id == "asteroid-99999", "last bounded page reachable without truncating catalogue");
            }
            finally { property.SetValue(scenario, original); ops.RefreshTacticalContacts(); }
            Require(ReferenceEquals(scenario.Asteroids, original), "large reference fixture restored before real simulation");
        }
        private void CheckQuestThresholds()
        {
            var baseline = SaveGameStore.Capture(scenario, "Threshold fixture baseline", "");
            try
            {
                // These explicit supply/count fixtures isolate the two actual quest conditions.
                ops.State.waterSecured = false; scenario.RestoreSupply(40, 0, 0, false);
                var quest = ops.GetTacticalQuests()[1]; Require(!quest.Complete && Mathf.Abs(quest.Progress - .5f) < .0001f, "40L without first raw delivery is only one fulfilled condition");
                scenario.RestoreSupply(20, 1, 0, true); quest = ops.GetTacticalQuests()[1];
                Require(!quest.Complete && Mathf.Abs(quest.Progress - .75f) < .0001f,
                    "first delivery plus20L reports actual three-quarter quest progress");
                scenario.RestoreSupply(40, 1, 0, true); ops.Prepare(0);
                quest = ops.GetTacticalQuests()[1]; Require(quest.Complete && quest.Progress == 1 && ops.State.waterSecured, "real operations latch requires first delivery and40L clean water");
                scenario.RestoreSupply(10, 1, 0, true); Require(ops.GetTacticalQuests()[1].Complete, "completed water objective remains latched when water is later consumed");
                var helper = Copy(ops.Capture()); helper.helper = true; ops.Restore(helper);
                Require(ops.GetTacticalDrones().Count == 2 && ops.GetTacticalDrones()[1].Id == "R-01", "real helper readiness flag reveals actual internal drone");
                Require(ops.FollowTacticalDrone("R-01") && orbit.FollowingTarget == ops.Layout.RepairDrone, "explicit internal follow accepts only actual R-01 model");
                ops.StopTacticalFollow();
            }
            finally { SaveGameStore.Apply(baseline, scenario); scenario.Scanner.DismissDialogue(); mode.Exit(); }
        }
        private void LaunchActualMission()
        {
            var planning = Array.Find(ops.GetComponentsInChildren<StationTerminal>(), terminal => terminal.kind == StationTerminalKind.Planning);
            mode.RestoreInterior(planning.transform.TransformPoint(new Vector3(0, .04f, -1.55f)), false, planning.transform.eulerAngles.y, 0);
            Require(ops.NearestTerminal(mode.FeetPosition) == planning, "physical planning terminal reached for actual diagnostic fixture");
            ops.OpenTerminal(planning); Require(ops.Enqueue(StationJobKind.Diagnose), "real diagnostic order created at physical planning terminal"); ops.ClosePanel();
            var terminal = Array.Find(ops.GetComponentsInChildren<StationTerminal>(), item => item.kind == StationTerminalKind.Drones);
            mode.RestoreInterior(terminal.transform.TransformPoint(new Vector3(0, .04f, -1.55f)), false, terminal.transform.eulerAngles.y, 0);
            Require(ops.NearestTerminal(mode.FeetPosition) == terminal, "physical drone terminal reached for actual flight");
            ops.OpenTerminal(terminal); Require(scenario.AssignTankOrder(known), "actual water mission assigned from physical drone terminal"); ops.ClosePanel();
            scenario.Scanner.DismissDialogue(); mode.Exit();
            Require(ops.FollowTacticalDrone("drone-0") && orbit.FollowingTarget == scenario.Worker.transform, "operative external drone explicitly followed");
            scenario.Advance(15);
            Require(scenario.Worker.HasTankOrder && scenario.Worker.Phase != DronePhase.Ready, "real flight is under way");
            var quest = ops.GetTacticalQuests()[2]; Require(quest.Progress > .25f && quest.Progress < .5f && !quest.Complete, "diagnostic quest uses actual running order elapsed time");
        }
        private IEnumerator CheckSearchFocus()
        {
            ops.FocusTacticalSearch(); yield return null; yield return null;
            if (!Guard(() => Require(StationOperations.TacticalTextInputActive, "actual rendered search field acquires keyboard focus"))) yield break;
            orbit.ShowHud = false; yield return null; yield return null;
            if (!Guard(() => Require(!StationOperations.TacticalTextInputActive, "hidden HUD releases search input rather than blocking camera"))) yield break;
            orbit.ShowHud = true; yield return null; yield return null;
            if (!Guard(() => Require(!StationOperations.TacticalTextInputActive, "showing HUD does not resurrect invisible search focus"))) yield break;
            ops.FocusTacticalSearch(); yield return null; yield return null;
            var settings = FindFirstObjectByType<SettingsMenu>(); settings.Open(); yield return null; yield return null;
            if (!Guard(() => Require(!StationOperations.TacticalTextInputActive, "real settings modal owns input instead of hidden tactical search"))) yield break;
            settings.Cancel(); yield return null; yield return null;
            ops.FocusTacticalSearch(); yield return null; yield return null;
            if (!Guard(() =>
            {
                var quit = FindFirstObjectByType<QuitMenu>(); Require(StationOperations.TacticalTextInputActive, "search refocuses after closing actual modal");
                quit.HandleEscape(); Require(!StationOperations.TacticalTextInputActive && !QuitMenu.IsOpen, "Escape first releases search without opening quit modal");
            })) yield break;
            ops.FocusTacticalSearch(); yield return null; yield return null;
            Guard(() => Require(ops.HandleTacticalSearchInput(new Vector2(UiLayout.Width - 20, UiLayout.Height * .5f), false, true)
                && !StationOperations.TacticalTextInputActive, "outside pointer input releases actual search focus"));
        }
        private IEnumerator Start()
        {
            Directory.CreateDirectory(Folder); File.Delete(Folder + "/result.json"); File.Delete(Folder + "/error.txt");
            yield return new WaitForSecondsRealtime(1);
            FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride = Path.GetFullPath(Folder + "/Saves");
            CinematicLibrary.PreferencePrefix = "SpaceMiner.Test.StationTactical." + Guid.NewGuid().ToString("N");
            FindFirstObjectByType<StartMenu>().StartDemo(); FindFirstObjectByType<IntroSequence>().Skip(); yield return null; yield return null;
            if (!Guard(() =>
            {
                ops = StationOperations.Current; scenario = ops.Scenario; orbit = Camera.main.GetComponent<OrbitCamera>(); mode = StationInteriorMode.Current;
                scenario.SetSimulationRate(0); scenario.Scanner.DismissDialogue(); ops.ClosePanel(); mode.Exit(); CheckFreshKnowledge();
            })) yield break;
            Screen.SetResolution(1440, 900, FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(.35f);
            Screen.SetResolution(1600, 1000, FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(.6f);
            yield return CheckSearchFocus(); if (failed) yield break;
            yield return Capture("01-station-before-scan-1600"); if (failed) yield break;
            if (!Guard(() => { PerformRealScan(); CheckLargeReferencePaging(); })) yield break;
            yield return Capture("02-scanned-contact-catalogue-1600"); if (failed) yield break;
            Vector3 asteroidPosition = known.transform.position, snapshotPivot = Vector3.zero;
            if (!Guard(() => { orbit.Focus(known.Info); snapshotPivot = orbit.Pivot; known.transform.position += Vector3.right * 3; Require(!orbit.IsFollowing, "asteroid focus never starts follow"); })) yield break;
            yield return null; yield return null;
            bool focusStable = (orbit.Pivot - snapshotPivot).sqrMagnitude < .000001f; known.transform.position = asteroidPosition;
            if (!Guard(() => { Require(focusStable, "moving focused asteroid leaves snapshot camera pivot unchanged"); CheckQuestThresholds(); LaunchActualMission(); })) yield break;
            yield return null; yield return null;
            if (!Guard(() => Require((orbit.Pivot - scenario.Worker.transform.position).sqrMagnitude < .000001f, "late camera follows actual launched drone pose"))) yield break;
            yield return Capture("03-active-drone-real-flight-1600"); if (failed) yield break;
            Vector3 stopped = Vector3.zero;
            if (!Guard(() => { var before = DomainSignature(); ops.StopTacticalFollow(); stopped = orbit.Pivot; Require(!orbit.IsFollowing && scenario.Worker.HasTankOrder, "stop-follow releases camera without cancelling work"); Require(DomainSignature() == before, "stop-follow changes no economy, task or drone state"); scenario.Advance(2); })) yield break;
            yield return null; yield return null;
            if (!Guard(() => { Require((orbit.Pivot - stopped).sqrMagnitude < .000001f, "stopped camera remains still while drone continues actual flight"); ops.FocusTacticalContact("station"); ops.SelectTacticalTab(TacticalOverviewTab.Quests); })) yield break;
            yield return Capture("04-real-quest-progress-1600"); if (failed) yield break;
            SettingsStore.Current.Accessibility.TextScale = 1.4f; Screen.SetResolution(1280, 720, FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(.6f);
            if (!Guard(() =>
            {
                Require(!ops.TacticalOverviewBounds.Overlaps(ops.HudResourceBounds), "enlarged-text tactical panel clears resource HUD");
                var vitalsBounds = (Rect)typeof(StationOperations).GetProperty("OutsideVitalsRect", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ops);
                Require(ops.TacticalOverviewBounds.yMax <= vitalsBounds.yMin - 17.9f, "tactical panel clears actual enlarged vitals HUD by18px");
                Require(ops.TacticalOwnsScreenPoint(ops.TacticalOverviewBounds.center) && !ops.TacticalOwnsScreenPoint(new Vector2(UiLayout.Width - 20, UiLayout.Height * .5f)), "only actual tactical bounds capture pointer input");
                Require(ops.TacticalQuestHeader.Contains("1/9"), "quest header reflects actual first scan and remaining campaign objectives");
                CheckExteriorGuards();
            })) yield break;
            yield return Capture("05-large-text-quests-1280"); if (failed) yield break;
            ops.SelectTacticalTab(TacticalOverviewTab.Drones); yield return Capture("06-large-text-active-drone-1280"); if (failed) yield break;
            if (!Guard(() =>
            {
                File.WriteAllText(Folder + "/result.json", JsonUtility.ToJson(new Report { passed = true, checks = checks,
                    actualKnownAsteroids = scenario.Scanner.KnownCount, pagedReferenceFixture = 100000 }, true));
                Debug.Log("TACTICAL OVERVIEW CHECK PASSED: " + checks); Application.Quit(0);
            })) yield break;
        }
    }
}
#endif
