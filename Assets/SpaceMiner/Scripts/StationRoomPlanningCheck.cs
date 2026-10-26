#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SpaceMiner
{
    // Runs against the actual four room catalogues, models, colliders and campaign agents.
    public sealed class StationRoomPlanningCheck : MonoBehaviour
    {
        private const string Folder = "Logs/StationRoomPlanning";
        private readonly string[] roomIds = { "command-room", "drone-bay", "drone-workshop", "internal-workshop" };
        private readonly List<string> pictures = new List<string>();
        private int checks, editedModels;
        private bool failed;
        private StationOperations operations;
        private WaterScenario scenario;
        private CommandRoomPlanner planner;
        private CampaignDroneFleet fleet;
        private StationDockInfrastructure infrastructure;
        private StationInteriorMode mode;
        private CommandRoomLayout[] rooms;

        [Serializable] private sealed class Result
        {
            public bool passed;
            public int checks, editedModels;
            public string[] rooms, pictures;
            public string scope = "Four real room catalogues, authored meshes/colliders and per-room frames; " +
                "every item resized/moved/rotated then restored; boundaries and protected walking/drone lanes; " +
                "isolated per-room disk save/load, old command-room v1 JSON, atomic rejected loads, separate " +
                "history and selection, physical inactive berth/cradle/feed/home relocation, busy berth lock. " +
                "Programmatic controller acceptance and actual player screenshots; no manual comfort, " +
                "FPS, complete logistics balance or long-running production claim.";
        }
        [Serializable] private sealed class LegacyCommandPlan
        {
            public int schema = 1;
            public string room = "command-room";
            public CommandRoomLayoutItem[] items;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-stationRoomPlanningCheck") >= 0)
                new GameObject("Four station rooms acceptance").AddComponent<StationRoomPlanningCheck>();
        }
        private void Require(bool condition, string label)
        {
            checks++;
            if (!condition) throw new Exception("ROOM PLANNING: " + label);
            Debug.Log("ROOM PLANNING OK: " + label);
        }
        private bool Guard(Action action)
        {
            try { action(); return true; }
            catch (Exception error)
            {
                failed = true; File.WriteAllText(Path.Combine(Folder, "error.txt"), error.ToString());
                Debug.LogException(error); Application.Quit(1); return false;
            }
        }
        private static bool Close(Vector3 a, Vector3 b, float tolerance = .008f) => Vector3.Distance(a, b) < tolerance;
        private static string Signature(CommandRoomLayout room) => JsonUtility.ToJson(new LegacyCommandPlan { room = room.RoomId, items = room.Snapshot() });
        private CommandRoomLayout Room(string id) => rooms.Single(room => room.RoomId == id);

        private void CheckCatalogues()
        {
            Require(rooms.Length == 4 && roomIds.All(id => rooms.Count(room => room.RoomId == id) == 1), "exactly four distinct requested rooms");
            Require(planner.Rooms.Count == 4 && roomIds.All(id => planner.Rooms.Any(room => room.RoomId == id)), "all four live rooms appear in the editor");
            Require(rooms.Select(room => Path.GetFullPath(room.SavePath)).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 4, "every room has a separate plan file");
            foreach (var room in rooms)
            {
                string isolated = Path.GetFullPath(Folder) + Path.DirectorySeparatorChar;
                Require(Path.GetFullPath(room.SavePath).StartsWith(isolated, StringComparison.OrdinalIgnoreCase), "isolated plan path / " + room.RoomId);
                Require(CommandRoomLayout.ValidatePlan(room.Snapshot(), room.Definition, out string reason), "valid installed catalogue / " + room.RoomId + " / " + reason);
                Require(room.FloorOutline().Length >= 4 && room.ViewExtents.x > 1 && room.ViewExtents.y > 1, "real floor outline and view extent / " + room.RoomId);
                Require(room.Definition.Keepouts.Length > 0, "protected entry or drone route / " + room.RoomId);
                foreach (var item in room.Items)
                {
                    Transform root = room.BoundRoot(item.id);
                    Require(root != null && root.gameObject.activeInHierarchy, "live item / " + room.RoomId + " / " + item.id);
                    Require(root.GetComponent<CommandRoomBlock>()?.id == item.id && root.GetComponent<CommandRoomBlock>()?.roomId == room.RoomId,
                        "room-qualified item identity / " + room.RoomId + " / " + item.id);
                    Require(Close(root.position, room.Frame.TransformPoint(item.position)), "floor pose agrees with actual room frame / " + item.id);
                    Require(Quaternion.Angle(root.rotation, room.Frame.rotation * Quaternion.Euler(0, item.yaw, 0)) < .03f, "actual planned heading / " + item.id);
                    Require(RepresentativeRenderer(root) != null, "visible real mesh geometry / " + item.id);
                    Require(RepresentativeCollider(root) != null, "real collider moves with item / " + item.id);
                    if (room.RoomId != "command-room")
                        Require(root.GetComponent<MeshFilter>() == null && root.childCount > 0
                            && !root.GetComponentsInChildren<Transform>(true).Any(child => child.name == "Blockoutbody"),
                            "authored object hierarchy retained instead of a proxy cube / " + item.id);
                }
            }
            Require(Room("command-room").Items.Count == 8, "existing eight command blocks remain compatible");
            Require(Room("drone-bay").Items.Count == 10, "ten individual real drone berths");
            foreach (string id in new[] { "repair-01", "repair-02", "tool-rack", "scrap-analysis", "scrap-bin", "bench-left", "bench-right" })
                Require(Room("drone-workshop").Get(id) != null, "external workshop contains " + id);
            foreach (string id in new[] { "helper-dock", "repair-bench", "offline-facility", "carbon-store", "iron-store", "unknown-store" })
                Require(Room("internal-workshop").Get(id) != null, "internal workshop contains " + id);
        }

        private static Renderer RepresentativeRenderer(Transform root)
            => root.GetComponentsInChildren<MeshFilter>().Where(filter => filter.sharedMesh != null && filter.GetComponent<Renderer>() != null
                && filter.GetComponent<Renderer>().enabled && filter.gameObject.activeInHierarchy)
                .Select(filter => filter.GetComponent<Renderer>()).OrderByDescending(renderer => renderer.bounds.size.sqrMagnitude).FirstOrDefault();
        private static Collider RepresentativeCollider(Transform root)
            => root.GetComponentsInChildren<Collider>().Where(collider => collider.enabled && !collider.isTrigger && collider.gameObject.activeInHierarchy)
                .OrderByDescending(collider => collider.bounds.size.sqrMagnitude).FirstOrDefault();

        private CommandRoomLayoutItem Shrunk(CommandRoomLayout room, string id)
        {
            var item = room.Get(id);
            foreach (float factor in new[] { .97f, .99f, .995f, 1.005f, 1.01f })
            {
                var candidate = item.Copy(); candidate.size *= factor;
                if (room.Validate(candidate, out _)) return candidate;
            }
            throw new Exception("No valid resize for " + room.RoomId + " / " + id);
        }
        private CommandRoomLayoutItem Moved(CommandRoomLayout room, string id)
        {
            var item = room.Get(id);
            foreach (float step in new[] { .05f, .02f, .01f })
                foreach (var direction in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back,
                    new Vector3(1,0,1).normalized, new Vector3(-1,0,-1).normalized })
                {
                    var candidate = item.Copy(); candidate.position += direction * step;
                    if (room.Validate(candidate, out _)) return candidate;
                }
            throw new Exception("No valid move for " + room.RoomId + " / " + id);
        }
        private CommandRoomLayoutItem Rotated(CommandRoomLayout room, string id)
        {
            var item = room.Get(id);
            foreach (float angle in new[] { .5f, -.5f, .25f, -.25f, .1f, -.1f })
            {
                var candidate = item.Copy(); candidate.yaw += angle;
                if (room.Validate(candidate, out _)) return candidate;
            }
            throw new Exception("No valid rotation for " + room.RoomId + " / " + id);
        }
        private void CheckEveryLiveItem()
        {
            foreach (var room in rooms)
            {
                var baseline = room.Snapshot();
                foreach (var source in baseline)
                {
                    var otherRooms = rooms.Where(other => other != room).ToDictionary(other => other.RoomId, Signature);
                    var root = room.BoundRoot(source.id); var renderer = RepresentativeRenderer(root); var collider = RepresentativeCollider(root);
                    Vector3 originalSize = renderer.bounds.size;
                    var resized = Shrunk(room, source.id); float factor = resized.size.x / source.size.x;
                    Require(room.TrySet(resized, out string reason), "resize actual item / " + source.id + " / " + reason);
                    Physics.SyncTransforms();
                    Require(Close(renderer.bounds.size, originalSize * factor, .025f), "authored mesh scales by planned size / " + source.id);
                    if (room.RoomId == "drone-bay")
                    {
                        int index = int.Parse(source.id.Substring(6)) - 1; var drone = scenario.Drones[index];
                        string hardwareObstruction = "Navigation fehlt";
                        bool fits = drone.Navigation != null && drone.Navigation.PoseClear(drone.HomePosition, drone.HomeRotation, out hardwareObstruction);
                        Require(fits, "real unscaled drone fits resized berth / " + source.id + " / " + hardwareObstruction);
                    }
                    Vector3 beforeMesh = renderer.bounds.center, beforeSolid = collider.bounds.center;
                    var moved = Moved(room, source.id); Vector3 delta = room.Frame.TransformVector(moved.position - resized.position);
                    Require(room.TrySet(moved, out reason), "move actual item / " + source.id + " / " + reason); Physics.SyncTransforms();
                    Require(Close(renderer.bounds.center, beforeMesh + delta) && Close(collider.bounds.center, beforeSolid + delta),
                        "mesh and solid collider translate together / " + source.id);
                    Quaternion priorMesh = renderer.transform.rotation, priorCollider = collider.transform.rotation;
                    var rotated = Rotated(room, source.id); float turn = Mathf.Abs(Mathf.DeltaAngle(moved.yaw, rotated.yaw));
                    Require(room.TrySet(rotated, out reason), "rotate actual item / " + source.id + " / " + reason); Physics.SyncTransforms();
                    Require(Mathf.Abs(Quaternion.Angle(renderer.transform.rotation, priorMesh) - turn) < .08f
                        && Mathf.Abs(Quaternion.Angle(collider.transform.rotation, priorCollider) - turn) < .08f,
                        "authored mesh and collision turn together / " + source.id);
                    Require(otherRooms.All(pair => Signature(Room(pair.Key)) == pair.Value), "editing one room cannot modify another / " + source.id);
                    Require(room.TryApply(baseline, out reason), "restore actual item after edit / " + source.id + " / " + reason);
                    editedModels++;
                }
            }
        }

        private void CheckRejectedEdits()
        {
            foreach (var room in rooms)
            {
                string before = Signature(room); var item = room.Items[0].Copy(); item.position = new Vector3(0, 0, -100);
                Require(!room.TrySet(item, out string reason) && !string.IsNullOrEmpty(reason), "outside room rejected / " + room.RoomId);
                Require(Signature(room) == before, "outside rejection is atomic / " + room.RoomId);
                if (room.RoomId == "drone-bay")
                {
                    var tooSmall = room.Items[0].Copy(); tooSmall.size *= .9f;
                    Require(!room.TrySet(tooSmall, out reason) && !string.IsNullOrEmpty(reason) && Signature(room) == before,
                        "bay hardware cannot shrink below the full imported drone envelope");
                    Require(room.Definition.MinimumItemSize.x >= 3.4f && room.Definition.MinimumItemSize.y >= 3.4f
                        && room.Definition.MinimumItemSize.z >= 5.4f, "bay definition carries physical minimum dimensions before runtime guards");
                    var tinyPlan = room.Snapshot(); tinyPlan[0] = tooSmall;
                    Require(!CommandRoomLayout.ValidatePlan(tinyPlan, room.Definition, out reason),
                        "static geometry validation rejects undersized hardware before an edit guard exists");
                    string initialPath = Path.GetFullPath(Path.Combine(Folder, "initial-load-tiny-bay.json"));
                    string tinyJson = JsonUtility.ToJson(new LegacyCommandPlan { room = room.RoomId, items = tinyPlan }, true);
                    File.WriteAllText(initialPath, tinyJson);
                    var holder = new GameObject("Unbound startup-plan validation fixture");
                    try
                    {
                        var fresh = holder.AddComponent<CommandRoomLayout>();
                        fresh.InitializeForRoom(room.Frame, room.Definition, initialPath);
                        Require(!string.IsNullOrWhiteSpace(fresh.StartupNotice) && fresh.EditGuard == null,
                            "fresh room rejects too-small saved hardware before installing runtime callbacks");
                        Require(CommandRoomLayout.ValidatePlan(fresh.Snapshot(), fresh.Definition, out reason)
                            && fresh.Get(tooSmall.id).size == room.Definition.Defaults.First(item => item.id == tooSmall.id).size,
                            "invalid startup file falls back to physically safe catalogue defaults");
                        Require(Signature(room) == before && File.ReadAllText(initialPath) == tinyJson,
                            "unbound startup validation neither moves live objects nor overwrites the rejected file");
                    }
                    finally { Destroy(holder); }
                }
                var bad = room.Snapshot(); bad[bad.Length - 1].id = bad[0].id;
                Require(!room.TryApply(bad, out reason) && Signature(room) == before, "duplicate identity is atomic / " + room.RoomId);
                foreach (var lane in room.Definition.Keepouts)
                {
                    item = room.Items[0].Copy(); item.position = new Vector3(lane.center.x, 0, lane.center.y);
                    item.size = Vector3.one * .4f; item.yaw = lane.yaw;
                    Require(!room.TrySet(item, out reason) && !string.IsNullOrEmpty(reason), "protected " + lane.label + " rejected / " + room.RoomId);
                    Require(Signature(room) == before, "protected route rejection preserves live state / " + room.RoomId);
                    Vector3 centre = room.Frame.TransformPoint(new Vector3(lane.center.x, 0, lane.center.y));
                    Require(!Physics.CheckCapsule(centre + Vector3.up * .35f, centre + Vector3.up * 1.5f,
                        .27f, (1 << 25) | (1 << 30), QueryTriggerInteraction.Ignore),
                        "protected route centre has actual standing collision clearance / " + room.RoomId + " / " + lane.label);
                }
            }
        }

        private void CheckHistoryIsolation()
        {
            for (int i = 0; i < rooms.Length; i++)
            {
                var room = rooms[i]; var next = rooms[(i + 1) % rooms.Length]; var original = room.Snapshot();
                Require(planner.SelectRoom(room.RoomId) && planner.Select(room.Items[0].id), "select actual room and item / " + room.RoomId);
                var candidate = Shrunk(room, planner.SelectedId);
                Require(planner.ApplyEdit(candidate.position, candidate.size, candidate.yaw, out string reason) && planner.CanUndo,
                    "editor owns a real room edit / " + room.RoomId + " / " + reason);
                string edited = Signature(room), nextBefore = Signature(next);
                Require(planner.Undo() && Signature(room) == JsonUtility.ToJson(new LegacyCommandPlan { room = room.RoomId, items = original }),
                    "undo restores only the selected room / " + room.RoomId);
                Require(planner.Redo() && Signature(room) == edited, "redo restores selected room edit / " + room.RoomId);
                Require(planner.SelectRoom(next.RoomId) && !planner.CanUndo && !planner.CanRedo && !planner.Undo(), "room switch isolates history / " + next.RoomId);
                Require(Signature(room) == edited && Signature(next) == nextBefore, "history cannot apply an old room snapshot to a different room");
                Require(room.TryApply(original, out reason), "restore room after history exercise / " + room.RoomId);
            }
        }

        private void CheckPersistence()
        {
            foreach (var room in rooms)
            {
                var baseline = room.Snapshot(); var change = Shrunk(room, baseline[0].id);
                Require(room.TrySet(change, out string reason) && room.Save(out reason), "save actual edited room / " + room.RoomId + " / " + reason);
                string accepted = Signature(room), disk = File.ReadAllText(room.SavePath);
                Require(!room.HasUnsavedChanges && disk.Contains("\"" + room.RoomId + "\""), "saved room identity and clean marker / " + room.RoomId);
                Require(room.TryApply(baseline, out reason) && room.HasUnsavedChanges, "restore defaults without erasing saved room / " + room.RoomId);
                Require(room.Load(out reason) && Signature(room) == accepted && !room.HasUnsavedChanges, "load correct per-room model poses / " + room.RoomId + " / " + reason);
                File.WriteAllText(room.SavePath, "{ broken");
                Require(!room.Load(out reason) && Signature(room) == accepted, "corrupt room plan preserves every current object / " + room.RoomId);
                File.WriteAllText(room.SavePath, disk.Replace("\"" + room.RoomId + "\"", "\"wrong-room\""));
                Require(!room.Load(out reason) && Signature(room) == accepted, "another room's file cannot replace this room / " + room.RoomId);
                File.WriteAllText(room.SavePath, disk);
                Require(room.Load(out reason) && room.TryApply(baseline, out reason) && room.Save(out reason), "restore isolated saved baseline / " + room.RoomId);
            }
            var command = Room("command-room"); string saved = File.ReadAllText(command.SavePath);
            File.WriteAllText(command.SavePath, JsonUtility.ToJson(new LegacyCommandPlan { items = CommandRoomLayout.CreateDefaults() }, true));
            Require(command.Load(out string legacyReason) && CommandRoomLayout.ValidatePlan(command.Snapshot(), out _), "old command-room v1 file remains readable / " + legacyReason);
            File.WriteAllText(command.SavePath, saved); Require(command.Load(out legacyReason), "restore prior command plan after legacy exercise");
        }

        private void CheckBerths()
        {
            var bay = Room("drone-bay");
            Require(scenario.Drones.Length == 10 && fleet.ModelCount == 10 && infrastructure.FeedCount == 10, "ten real agents, cradles and feeds");
            float low = scenario.Drones.Min(drone => bay.Frame.InverseTransformPoint(drone.HomePosition).y);
            float high = scenario.Drones.Max(drone => bay.Frame.InverseTransformPoint(drone.HomePosition).y);
            Require(high - low < .05f && low >= 0 && high < 2, "all ten home anchors share the accessible deck level");
            for (int i = 0; i < 10; i++)
            {
                var root = bay.BoundRoot("berth-" + (i + 1).ToString("00"));
                Require(root != null && fleet.BerthRoot(i).IsChildOf(root) && fleet.CradleRoot(i).IsChildOf(root)
                    && infrastructure.FeedRoot(i).IsChildOf(root), "berth includes actual cradle and utility feed / " + (i + 1));
            }
            Require(scenario.Worker.Navigation != null, "real imported-hull navigation is available for aisle acceptance");
            foreach (float row in new[] { RingWalkthrough.UpperBerthRadius, RingWalkthrough.LowerBerthRadius })
                foreach (float side in new[] { -1f, 1f })
                {
                    float x = side * RingWalkthrough.BerthSpacing * .5f;
                    Vector3 point = bay.Frame.TransformPoint(new Vector3(x, RingWalkthrough.BerthHeight, row));
                    Require(scenario.Worker.Navigation.PoseClear(point, bay.Frame.rotation, out string obstruction),
                        "full imported drone hull clears row aisle at x=" + x + ", z=" + row + " / " + obstruction);
                }
            var baseline = bay.Snapshot(); var berth = bay.Get("berth-02"); var changed = Shrunk(bay, berth.id);
            Require(bay.TrySet(changed, out string reason), "prepare inactive berth clearance / " + reason);
            changed = Moved(bay, berth.id); int index = 1;
            Vector3 home = scenario.Drones[index].HomePosition, cradle = fleet.CradleRoot(index).position, feed = infrastructure.FeedRoot(index).position;
            var visibleFeed = RepresentativeRenderer(infrastructure.FeedRoot(index));
            Require(visibleFeed != null, "actual utility geometry remains rendered in movable berth cluster");
            Vector3 feedMesh = visibleFeed.bounds.center;
            var priorWorkerNavigation = scenario.Worker.Navigation;
            Vector3 priorWorkerHome = scenario.Worker.HomePosition; var priorWorkerPhase = scenario.Worker.Phase;
            Vector3 delta = bay.Frame.TransformVector(changed.position - bay.Get(berth.id).position);
            Require(bay.TrySet(changed, out reason), "move inactive berth / " + reason); Physics.SyncTransforms();
            Require(!ReferenceEquals(scenario.Worker.Navigation, priorWorkerNavigation)
                && Close(scenario.Worker.HomePosition, priorWorkerHome) && scenario.Worker.Phase == priorWorkerPhase,
                "inactive berth edit invalidates other navigation caches while preserving their home and phase");
            Require(Close(scenario.Drones[index].HomePosition, home + delta) && Close(fleet.CradleRoot(index).position, cradle + delta)
                && Close(infrastructure.FeedRoot(index).position, feed + delta), "inactive home, charging cradle and utility feed move together");
            Require(Close(visibleFeed.bounds.center, feedMesh + delta), "visible feed mesh follows the berth rather than a stale static batch");
            Require(Close(scenario.Drones[index].transform.position, scenario.Drones[index].HomePosition) && !scenario.Drones[index].IsOperational,
                "inactive drone follows new physical home and stays inactive");
            Require(bay.TryApply(baseline, out reason), "restore real berth after moving it");

            var workerState = scenario.Worker.CaptureState(); changed = Shrunk(bay, "berth-01");
            Require(bay.TrySet(changed, out reason) && bay.Save(out reason) && bay.TryApply(baseline, out reason), "save alternate home before explicit busy-phase fixture");
            string busyOwnPlan = File.ReadAllText(bay.SavePath);
            var neighbourChange = Shrunk(bay, "berth-02");
            Require(bay.TrySet(neighbourChange, out reason) && bay.Save(out reason) && bay.TryApply(baseline, out reason),
                "save alternate inactive neighbour before busy-phase fixture");
            string busyNeighbourPlan = File.ReadAllText(bay.SavePath);
            File.WriteAllText(bay.SavePath, busyOwnPlan);
            try
            {
                scenario.Worker.ResumeStationWork(); string before = Signature(bay);
                Require(!bay.TrySet(changed, out reason) && !string.IsNullOrWhiteSpace(reason) && Signature(bay) == before,
                    "working drone locks its own berth without moving geometry");
                Require(!bay.TrySet(neighbourChange, out reason) && !string.IsNullOrWhiteSpace(reason) && Signature(bay) == before,
                    "working drone also locks an inactive neighbour that could obstruct its planned route");
                Require(bay.TryApply(baseline, out reason), "unchanged busy berth is allowed in a whole-room snapshot");
                Require(!bay.Load(out reason) && Signature(bay) == before, "loading a saved alternate home cannot bypass the busy lock");
                File.WriteAllText(bay.SavePath, busyNeighbourPlan);
                Require(!bay.Load(out reason) && Signature(bay) == before,
                    "loading an inactive neighbour's changed layout cannot bypass another drone's flight lock");
            }
            finally { scenario.Worker.RestoreState(workerState); }
            Require(bay.TryApply(baseline, out reason) && bay.Save(out reason), "restore isolated bay plan after busy-phase fixture");
        }

        private IEnumerator CaptureRoom(string id, int number)
        {
            if (!Guard(() =>
            {
                Require(planner.SelectRoom(id), "select screenshot room / " + id);
                Require(planner.Select(planner.Layout.Items[0].id), "select live item for visible rotation handle / " + id);
            })) yield break;
            yield return new WaitForSecondsRealtime(.35f); yield return new WaitForEndOfFrame();
            if (!Guard(() =>
            {
                Require(planner.PlanViewport.width > 250 && planner.PlanViewport.height > 200, "usable room viewport / " + id);
                Require(planner.PlanViewport.Contains(planner.GetRotationHandlePlanPoint()), "visible rotation handle / " + id);
                foreach (var item in planner.Layout.Items)
                    foreach (var corner in planner.GetItemPlanPolygon(item.id)) Require(planner.PlanViewport.Contains(corner), "fitted floor footprint / " + id + " / " + item.id);
            })) yield break;
            string name = number.ToString("00") + "-" + id + ".png";
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Folder, name)));
            yield return new WaitForSecondsRealtime(.45f);
            Guard(() =>
            {
                var image = new Texture2D(2, 2);
                try
                {
                    Require(File.Exists(Path.Combine(Folder, name)) && image.LoadImage(File.ReadAllBytes(Path.Combine(Folder, name))), "actual player screenshot exists / " + id);
                    Require(image.width >= 1280 && image.height >= 720, "native screenshot dimensions / " + id);
                    var pixels = image.GetPixels32(); int lit = 0;
                    for (int i = 0; i < pixels.Length; i += 97) if (pixels[i].r > 35 || pixels[i].g > 35 || pixels[i].b > 35) lit++;
                    Require(lit > 100, "screenshot contains visible room-plan UI / " + id); pictures.Add(name);
                }
                finally { Destroy(image); }
            });
        }

        private void CheckCampaignRestoreAtMovedHome()
        {
            var bay = Room("drone-bay"); var baseline = bay.Snapshot(); var worker = scenario.Worker;
            Require(worker.Phase == DronePhase.Ready, "stationary-save regression starts with a genuinely ready drone");
            var saved = SaveGameStore.Capture(scenario, "Raumplanung / altes Drohnenheim", "Current-revision stationary-home regression");
            Require(saved.interiorLayout == StationInteriorLayout.Revision, "stationary-home regression uses the current layout revision");
            string file = SaveGameStore.Write(saved, directory: Path.GetFullPath(Path.Combine(Folder, "Saves")));
            Vector3 oldPosition = saved.drones[0].position; CommandRoomLayoutItem moved = null;
            var original = bay.Get("berth-01");
            foreach (float distance in new[] { .5f, .4f, .35f })
            {
                foreach (var direction in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
                {
                    var candidate = original.Copy(); candidate.position += direction * distance;
                    if (bay.Validate(candidate, out _)) { moved = candidate; break; }
                }
                if (moved != null) break;
            }
            Require(moved != null && bay.TrySet(moved, out _), "move actual ready berth far enough to break the old charging-distance check");
            Vector3 newHome = worker.HomePosition; Quaternion newHeading = worker.HomeRotation;
            Require(Vector3.Distance(newHome, oldPosition) > .3f, "old saved pose lies outside the new physical charging contact");
            try
            {
                var disk = SaveGameStore.Read(file); SaveGameStore.Validate(disk, scenario); SaveGameStore.Apply(disk, scenario);
                infrastructure.Refresh(); Physics.SyncTransforms();
                Require(Close(worker.HomePosition, newHome) && Close(worker.transform.position, newHome)
                    && Quaternion.Angle(worker.transform.rotation, newHeading) < .02f,
                    "same-revision campaign load places ready drone at its currently planned real home");
                Require(worker.Phase == DronePhase.Ready && worker.IsOperational && !worker.HasTankOrder,
                    "stationary-home reconciliation preserves ready phase and operational status");
                Require(infrastructure.FeedConnected(0) && infrastructure.FeedPowered(0),
                    "restored stationary drone has real charging contact at the moved berth");
                Require(Mathf.Abs(worker.BatteryKwh - disk.drones[0].battery) < .0001f
                    && Mathf.Abs(worker.FuelLiters - disk.drones[0].fuel) < .0001f
                    && Mathf.Abs(worker.CargoKg - disk.drones[0].cargo) < .0001f
                    && Mathf.Abs(scenario.WaterLiters - disk.water) < .0001f
                    && Mathf.Abs(operations.State.power.storedKwh - disk.stationCampaign.power.storedKwh) < .0001f,
                    "pose reconciliation neither consumes nor grants fuel, battery, cargo, station water or stored electricity");
            }
            finally
            {
                if (!bay.TryApply(baseline, out string reason)) throw new Exception("Could not restore regression berth: " + reason);
                SaveGameStore.Apply(SaveGameStore.Read(file), scenario); scenario.Scanner.DismissDialogue(); infrastructure.Refresh();
            }
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(Folder); File.Delete(Path.Combine(Folder, "result.json")); File.Delete(Path.Combine(Folder, "error.txt"));
            yield return new WaitForSecondsRealtime(1);
            if (!Guard(() =>
            {
                FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride = Path.GetFullPath(Path.Combine(Folder, "Saves"));
                CinematicLibrary.PreferencePrefix = "SpaceMiner.Test.StationRoomPlanning." + Guid.NewGuid() + ".";
                Require(SettingsStore.PathName.StartsWith(Path.GetFullPath(Folder), StringComparison.OrdinalIgnoreCase), "test settings are isolated");
                FindFirstObjectByType<StartMenu>().StartDemo(); FindFirstObjectByType<IntroSequence>().Skip();
            })) yield break;
            yield return null; yield return null;
            if (!Guard(() =>
            {
                mode = StationInteriorMode.Current; scenario = FindFirstObjectByType<WaterScenario>(); operations = scenario.Operations;
                planner = FindFirstObjectByType<CommandRoomPlanner>(); rooms = operations.RoomPlans.ToArray();
                fleet = FindFirstObjectByType<CampaignDroneFleet>(); infrastructure = FindFirstObjectByType<StationDockInfrastructure>();
                Require(operations.Live && planner != null && fleet != null && infrastructure != null, "live game systems installed");
                scenario.SetSimulationRate(0); scenario.Scanner.DismissDialogue();
                Require(planner.Open() && SettingsMenu.BlocksInput && SettingsMenu.PausesSimulation, "four-room planner blocks input and pauses simulation");
                CheckCatalogues(); CheckEveryLiveItem(); CheckRejectedEdits(); CheckHistoryIsolation(); CheckPersistence(); CheckBerths(); CheckCampaignRestoreAtMovedHome();
                Screen.SetResolution(1598, 998, FullScreenMode.Windowed); SettingsStore.Current.Accessibility.TextScale = 1;
            })) yield break;
            yield return new WaitForSecondsRealtime(.3f); Screen.SetResolution(1600, 1000, FullScreenMode.Windowed);
            yield return new WaitForSecondsRealtime(.4f);
            for (int i = 0; i < roomIds.Length; i++) { yield return CaptureRoom(roomIds[i], i + 1); if (failed) yield break; }
            if (!Guard(() =>
            {
                planner.Close();
                File.WriteAllText(Path.Combine(Folder, "result.json"), JsonUtility.ToJson(new Result
                { passed = true, checks = checks, editedModels = editedModels, rooms = roomIds, pictures = pictures.ToArray() }, true));
                Debug.Log("STATION ROOM PLANNING CHECK PASSED: " + checks + " checks / " + editedModels + " actual edited objects");
            })) yield break;
            Application.Quit(0);
        }
    }
}
#endif
