#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SpaceMiner
{
    // Exercises the same controller used by mouse and numeric editing; no replacement
    // geometry, mock input model or campaign resource fixtures are needed here.
    public sealed class CommandRoomPlannerCheck : MonoBehaviour
    {
        private const string Folder = "Logs/CommandRoomPlannerCheck";
        private int checks;
        private bool failed;
        private readonly List<string> pictures = new List<string>();
        private StationInteriorMode mode;
        private StationInteriorLayout interior;
        private StationOperations operations;
        private WaterScenario scenario;
        private CommandRoomLayout plan;
        private CommandRoomPlanner planner;
        private Vector3 priorFeet, priorCamera;
        private Quaternion priorCameraRotation;
        private float priorYaw, priorPitch, priorRate;
        private float pausedElapsed, pausedWater, pausedBattery;

        [Serializable] private sealed class Result
        {
            public bool passed;
            public int checks, layoutRevision;
            public string[] pictures;
            public string scope = "Eight real labelled room blocks and colliders, live terminal/scanner approaches, " +
                "actual planner pointer move/resize, rotation/numeric edits, undo/redo, rejected atomic edits, " +
                "isolated disk save/load and corrupt-file recovery, modal movement and simulation guards, " +
                "1600x1000 and 1280x720 at 1.4 text scale, direct 3D room render. Programmatic controller " +
                "acceptance; no manual mouse comfort, performance or production model claim.";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-commandRoomPlannerCheck") >= 0)
                new GameObject("Command room planner acceptance").AddComponent<CommandRoomPlannerCheck>();
        }

        private void Require(bool condition, string label)
        {
            checks++;
            if (!condition) throw new Exception("COMMAND ROOM: " + label);
            Debug.Log("COMMAND ROOM OK: " + label);
        }

        private bool Guard(Action action)
        {
            try { action(); return true; }
            catch (Exception exception)
            {
                failed = true;
                File.WriteAllText(Path.Combine(Folder, "error.txt"), exception.ToString());
                Debug.LogException(exception); Application.Quit(1); return false;
            }
        }

        private string Signature() => string.Join("|", plan.Snapshot().Select(item => JsonUtility.ToJson(item)));
        private Vector3 World(Vector3 position) => interior.transform.TransformPoint(position);
        private static bool Close(Vector3 a, Vector3 b, float tolerance = .002f) => Vector3.Distance(a, b) < tolerance;

        private void CheckBindings()
        {
            Require(plan.Items.Count == 8, "exactly eight separate room elements");
            Require(CommandRoomLayout.ValidatePlan(plan.Snapshot(), out string reason), "room footprint validation / " + reason);
            foreach (var item in plan.Items)
            {
                Transform root = plan.BoundRoot(item.id);
                Require(root != null && root.gameObject.activeInHierarchy, "live block root / " + item.id);
                Require(root.GetComponent<CommandRoomBlock>()?.id == item.id, "stable block identity / " + item.id);
                Require(Close(root.position, World(item.position)), "planned and actual floor position agree / " + item.id);
                Require(Quaternion.Angle(root.rotation, interior.transform.rotation * Quaternion.Euler(0, item.yaw, 0)) < .02f,
                    "planned and actual rotation agree / " + item.id);
                var solids = root.GetComponentsInChildren<BoxCollider>().Where(c => c.enabled && !c.isTrigger).ToArray();
                Require(solids.Length == 1, "one simple solid block collider / " + item.id);
                var solid = solids[0];
                Require(Close(Vector3.Scale(solid.size, solid.transform.lossyScale), item.size),
                    "actual collider follows all three planned dimensions / " + item.id);
                Require(Close(solid.bounds.center, World(item.position + Vector3.up * item.size.y * .5f)),
                    "solid block remains supported by floor / " + item.id);
                var body = solid.GetComponent<MeshRenderer>();
                Require(body != null && body.enabled, "movable block remains individually rendered / " + item.id);
                var displays = root.GetComponentsInChildren<StationDisplay>();
                Require(displays.Length == 1 && displays[0].Caption == item.label, "front display names the element / " + item.id);
                var display = displays[0];
                Require(root.InverseTransformPoint(display.transform.position).z < -item.size.z * .5f,
                    "caption is physically in front of its block / " + item.id);
                Require(Vector3.Dot(display.transform.forward, root.forward) > .999f && display.Front.transform.localScale.x > 0,
                    "front text is upright and not mirrored / " + item.id);
            }
            Require(operations.GetComponentsInChildren<StationTerminal>().Count(t => t.kind == StationTerminalKind.Utilities) == 0,
                "the twelve unwanted freestanding sector consoles are removed");
            Require(operations.GetComponentsInChildren<StationTerminal>().Count(t => t.GetComponentInParent<CommandRoomBlock>() != null) == 5,
                "five functional command terminals including power");
            var outline = CommandRoomLayout.RoomOutline();
            Require(outline.Length >= 32 && outline.All(p => p.y > 36 && p.y <= 54.01f), "ground plan follows curved room geometry");
            Vector3 entry = World(new Vector3(0, 1.2f, 39.6f));
            Require(!Physics.CheckBox(entry, new Vector3(1.8f, .85f, 1), interior.transform.rotation, 1 << 25, QueryTriggerInteraction.Ignore),
                "real entrance walking volume remains clear");
        }

        private void Reject(CommandRoomLayoutItem[] malformed, string label)
        {
            string before = Signature();
            Require(!plan.TryApply(malformed, out string reason) && !string.IsNullOrWhiteSpace(reason), label + " rejected with explanation");
            Require(Signature() == before, label + " leaves every live placement unchanged");
        }

        private void CheckRejectedPlans()
        {
            var candidate = plan.Get("main-console"); candidate.position = Vector3.zero;
            Require(plan.Get("main-console").position.z > 40, "Get returns a copy rather than writable internal state");
            Require(!plan.TrySet(candidate, out _), "single outside element rejected");
            Reject(plan.Snapshot().Take(7).ToArray(), "missing element");
            var copy = plan.Snapshot(); copy[0].id = copy[1].id; Reject(copy, "duplicate identity");
            copy = plan.Snapshot(); copy[0].id = "unknown"; Reject(copy, "unknown identity");
            copy = plan.Snapshot(); copy[0] = null; Reject(copy, "null element");
            copy = plan.Snapshot(); copy[0].position = new Vector3(0, 0, 100); Reject(copy, "outside outer arc");
            copy = plan.Snapshot(); copy[0].position = new Vector3(0, 0, 53.4f); copy[0].size = new Vector3(2, 1, 2);
            Reject(copy, "centre inside but corner outside curved wall");
            copy = plan.Snapshot(); copy[0].position = new Vector3(0, 0, 39.7f); Reject(copy, "entrance obstruction");
            copy = plan.Snapshot(); copy[0].position = copy[1].position; copy[0].size = copy[1].size; copy[0].yaw = copy[1].yaw;
            Reject(copy, "overlapping blocks");
            copy = plan.Snapshot(); copy[0].position += Vector3.right * .25f; copy[7].position = Vector3.zero;
            Reject(copy, "otherwise valid first edit followed by invalid last edit");
            copy = plan.Snapshot(); copy[0].size = new Vector3(0, 1, 1); Reject(copy, "zero width");
            copy = plan.Snapshot(); copy[0].size = new Vector3(1, 5, 1); Reject(copy, "ceiling overrun");
            copy = plan.Snapshot(); copy[0].position += Vector3.up; Reject(copy, "floating block");
            copy = plan.Snapshot(); copy[0].position.x = float.NaN; Reject(copy, "NaN position");
            copy = plan.Snapshot(); copy[0].size.z = float.PositiveInfinity; Reject(copy, "infinite dimension");
            copy = plan.Snapshot(); copy[0].yaw = float.NaN; Reject(copy, "NaN rotation");
            CheckBindings();
        }

        private void CheckPointerAndHistory()
        {
            Require(planner.Select("main-console"), "select central block in actual controller");
            var original = plan.Get("main-console"); string initial = Signature();
            Vector2 centre = planner.WorldToPlanScreen(new Vector2(original.position.x, original.position.z));
            Vector2 shift = planner.WorldToPlanScreen(new Vector2(original.position.x + .37f, original.position.z)) - centre;
            Require(planner.BeginPointerEdit(centre), "pointer hits live central footprint");
            Require(planner.DragPointer(centre + shift, false, out string reason), "same UI drag path moves footprint freely without Shift / " + reason);
            planner.EndPointerEdit();
            Require(Mathf.Abs(plan.Get("main-console").position.x - original.position.x - .37f) < .002f,
                "free drag preserves fractional metres rather than snapping");
            string moved = Signature();
            Require(planner.Undo() && Signature() == initial, "drag is one undoable operation");
            Require(planner.Redo() && Signature() == moved, "redo restores the complete drag");
            Require(planner.Undo() && Signature() == initial, "return to baseline before resize");

            Require(planner.BeginPointerEdit(centre), "same footprint starts Shift drag");
            Require(planner.DragPointer(centre + shift, true, out reason), "same UI drag supports Shift snapping / " + reason);
            planner.EndPointerEdit();
            Require(Mathf.Abs(plan.Get("main-console").position.x - original.position.x - .25f) < .002f,
                "holding Shift snaps fractional drag to quarter-metre grid");
            Require(planner.Undo() && Signature() == initial, "Shift drag remains one undoable operation");

            Require(planner.Select("battery"), "select neighbouring wall block for magnetic alignment");
            var battery = plan.Get("battery");
            Vector2 batteryCentre = planner.WorldToPlanScreen(new Vector2(battery.position.x, battery.position.z));
            Vector2 batteryTarget = planner.WorldToPlanScreen(new Vector2(battery.position.x + .37f, battery.position.z));
            Require(planner.BeginPointerEdit(batteryCentre) && planner.DragPointer(batteryTarget, true, out reason),
                "Shift drag reaches neighbouring wall equipment alignment / " + reason);
            planner.EndPointerEdit();
            var placedBattery = plan.Get("battery"); var production = plan.Get("production");
            Vector2[] batteryEdges = CommandRoomLayout.Footprint(placedBattery), productionEdges = CommandRoomLayout.Footprint(production);
            float batteryLow = batteryEdges.Min(p => p.x), batteryHigh = batteryEdges.Max(p => p.x);
            float productionLow = productionEdges.Min(p => p.x), productionHigh = productionEdges.Max(p => p.x);
            Require(Mathf.Abs(batteryLow - productionLow) < .002f && Mathf.Abs(batteryHigh - productionHigh) < .002f,
                "nearby matching box edges magnetically align beyond the quarter-metre grid; actual centre x=" + placedBattery.position.x.ToString("R")
                + ", expected x=" + production.position.x.ToString("R") + ", actual x edges=" + batteryLow.ToString("R") + ".." + batteryHigh.ToString("R")
                + ", target x edges=" + productionLow.ToString("R") + ".." + productionHigh.ToString("R"));
            Require(planner.Undo() && Signature() == initial, "magnetic placement also undoes atomically");
            Require(planner.Select("main-console"), "return to central block resize");

            Vector2[] corners = planner.GetItemPlanPolygon("main-console");
            Require(corners.Length == 4, "real UI supplies four resize corners");
            Vector2 worldCorner = planner.PlanScreenToWorld(corners[0]);
            Vector2 worldCentre = new Vector2(original.position.x, original.position.z);
            Vector2 resizedCorner = worldCorner + (worldCorner - worldCentre).normalized * .28f;
            Require(planner.BeginPointerEdit(corners[0]), "pointer hits actual resize handle");
            Require(planner.DragPointer(planner.WorldToPlanScreen(resizedCorner), false, out reason), "corner resize uses real pointer path / " + reason);
            planner.EndPointerEdit();
            var resized = plan.Get("main-console");
            Require(resized.size.x > original.size.x + .1f && resized.size.z > original.size.z + .04f,
                "corner drag enlarges width and depth");
            var resizedPolygon = planner.GetItemPlanPolygon("main-console");
            Require(Vector2.Distance(resizedPolygon[2], corners[2]) < 1.2f, "resize keeps opposite corner fixed");
            string expanded = Signature();
            Require(planner.Undo() && Signature() == initial, "resize undoes exactly");
            Require(planner.Redo() && Signature() == expanded, "resize redoes exactly");
            Require(planner.Undo(), "restore before rotation");

            Require(planner.RotateSelected(15, out reason), "real numeric rotation action / " + reason);
            Require(Mathf.Abs(Mathf.DeltaAngle(plan.Get("main-console").yaw, original.yaw + 15)) < .01f,
                "central block rotates by requested degrees");
            string rotated = Signature();
            Require(planner.Undo() && Signature() == initial, "rotation undoes exactly");
            Require(planner.Redo() && Signature() == rotated, "rotation redoes exactly");
            Require(planner.Undo(), "restore before final numeric edit");

            Require(planner.ApplyEdit(original.position + Vector3.right * .5f,
                original.size + new Vector3(.2f, .15f, .1f), original.yaw + 5, out reason), "position, size and yaw apply together / " + reason);
            string accepted = Signature();
            Require(!planner.ApplyEdit(new Vector3(0, 0, 200), original.size, 0, out reason) && Signature() == accepted,
                "invalid numeric edit is rejected without partial geometry changes");

            centre = planner.WorldToPlanScreen(new Vector2(plan.Get("main-console").position.x, plan.Get("main-console").position.z));
            Require(planner.BeginPointerEdit(centre), "second drag begins");
            Require(planner.DragPointer(centre + shift * .5f, false, out reason), "second drag temporarily moves block / " + reason);
            planner.EndPointerEdit(true);
            Require(Signature() == accepted, "cancelled pointer drag restores pre-drag placement");
            CheckBindings();
        }

        private void CheckPersistence()
        {
            string expectedPrefix = Path.GetFullPath(Folder) + Path.DirectorySeparatorChar;
            Require(Path.GetFullPath(plan.SavePath).StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase), "room plan disk writes isolated from personal plan");
            Require(planner.SaveLayout(out string reason) && File.Exists(plan.SavePath), "save real edited layout to JSON / " + reason);
            string accepted = Signature(), disk = File.ReadAllText(plan.SavePath);
            Require(!plan.HasUnsavedChanges, "successful disk save marks current layout clean");
            Require(planner.SaveLayout(out reason) && File.ReadAllText(plan.SavePath + ".bak") == disk,
                "second atomic save preserves the previous valid plan as backup / " + reason);
            planner.ResetLayout();
            Require(Signature() != accepted && plan.HasUnsavedChanges, "default reset updates live blocks without erasing saved plan");
            Require(planner.LoadLayout(out reason) && Signature() == accepted, "disk load restores exact positions, dimensions and rotations / " + reason);
            Require(!plan.HasUnsavedChanges, "loaded plan is marked saved");
            foreach (string invalid in new[] { "{broken", "{}", "{\"schema\":999,\"room\":\"command-room\",\"items\":[]}" })
            {
                File.WriteAllText(plan.SavePath, invalid);
                Require(!planner.LoadLayout(out reason) && !string.IsNullOrWhiteSpace(reason), "invalid JSON/schema is explained safely");
                Require(Signature() == accepted, "failed disk load preserves every current placement");
            }
            File.WriteAllText(plan.SavePath, disk);
            Require(planner.LoadLayout(out reason) && Signature() == accepted, "valid saved file remains reloadable after corrupt-file exercise / " + reason);
            File.WriteAllText(Path.Combine(Folder, "accepted-room-plan.json"), disk);
            CheckBindings();
        }

        private Vector2 RotationPointerTarget(float wantedYaw)
        {
            var item = plan.Get(planner.SelectedId);
            Vector2 centre = new Vector2(item.position.x, item.position.z);
            Vector2 handle = planner.PlanScreenToWorld(planner.GetRotationHandlePlanPoint());
            Vector2 offset = handle - centre;
            Vector3 rotated = Quaternion.Euler(0, Mathf.DeltaAngle(item.yaw, wantedYaw), 0) * new Vector3(offset.x, 0, offset.y);
            return planner.WorldToPlanScreen(centre + new Vector2(rotated.x, rotated.z));
        }

        private void RotateThroughHandle(float wantedYaw, bool shift, string label)
        {
            CheckFrontHandle(label + " / before drag");
            Vector2 handle = planner.GetRotationHandlePlanPoint(), target = RotationPointerTarget(wantedYaw);
            Require(planner.PlanViewport.Contains(handle), "rotation handle is visible inside actual plan / " + label);
            Require(planner.BeginPointerEdit(handle), "rotation handle accepts a real pointer gesture / " + label);
            Require(planner.DragPointer(target, shift, out string reason), "rotation pointer gesture accepted / " + label + " / " + reason);
            CheckFrontHandle(label + " / during drag");
            planner.EndPointerEdit();
        }

        private void CheckFrontHandle(string label)
        {
            var corners = planner.GetItemPlanPolygon(planner.SelectedId);
            Vector2 front = (corners[0] + corners[1]) * .5f, rear = (corners[2] + corners[3]) * .5f;
            Vector2 stem = planner.GetRotationHandlePlanPoint() - front;
            Require(Vector2.Dot(stem.normalized, (front - rear).normalized) > .9999f,
                "rotation handle attaches to the marked display front and follows its heading / " + label);
            Require(Mathf.Abs(stem.magnitude - 32) < .02f,
                "front handle keeps its screen-space clearance while rotating / " + label);
        }

        private void RotationFixture(Action<CommandRoomLayoutItem[]> configure, string label)
        {
            var fixture = CommandRoomLayout.CreateDefaults(); configure(fixture);
            Require(plan.TryApply(fixture, out string reason), "valid explicit rotation fixture / " + label + " / " + reason);
        }

        private void CheckRotationGestures()
        {
            var accepted = plan.Snapshot();
            RotationFixture(items => Array.Find(items, item => item.id == "main-console").yaw = 359, "free wraparound rotation");
            Require(planner.Select("main-console"), "select central box for actual rotation handle");
            var original = plan.Get("main-console"); string initial = Signature();
            RotateThroughHandle(23.7f, false, "free 23.7 degrees across zero");
            var rotated = plan.Get("main-console");
            Require(Mathf.Abs(Mathf.DeltaAngle(rotated.yaw, 23.7f)) < .02f,
                "free handle rotation preserves arbitrary angle across 359/0 boundary; actual=" + rotated.yaw.ToString("R"));
            Require(Close(rotated.position, original.position) && Close(rotated.size, original.size),
                "rotating with the handle preserves exact box centre and all dimensions");
            Require(Quaternion.Angle(plan.BoundRoot("main-console").rotation,
                interior.transform.rotation * Quaternion.Euler(0, rotated.yaw, 0)) < .02f,
                "interactive handle rotates the real 3D box rather than only the plan drawing");
            string rotatedSignature = Signature();
            Require(planner.Undo() && Signature() == initial, "whole rotation gesture is one undo operation");
            Require(planner.Redo() && Signature() == rotatedSignature, "rotation gesture redoes exact angle and placement");
            Require(planner.Undo() && Signature() == initial, "restore wraparound angle before cancelled rotation");
            Vector2 handle = planner.GetRotationHandlePlanPoint(), target = RotationPointerTarget(1);
            Require(planner.BeginPointerEdit(handle) && planner.DragPointer(target, false, out _), "second rotation crosses 359 to one degree");
            Require(Mathf.Abs(Mathf.DeltaAngle(plan.Get("main-console").yaw, 1)) < .02f, "pointer wraparound uses shortest signed angular difference");
            planner.EndPointerEdit(true);
            Require(Signature() == initial, "Escape/cancel restores the entire pre-rotation snapshot");

            foreach (bool right in new[] { true, false })
            {
                RotationFixture(items => { }, right ? "right sloping side wall" : "left sloping side wall");
                string id = right ? "planning" : "drones";
                Require(planner.Select(id), "select wall-adjacent box / " + id);
                var before = plan.Get(id);
                RotateThroughHandle(right ? 101 : 259, true, "Shift actual sloping wall / " + id);
                var after = plan.Get(id); float expected = right ? 103.5f : 256.5f;
                Require(Mathf.Abs(Mathf.DeltaAngle(after.yaw, expected)) < .02f,
                    "Shift aligns with real sloping wall rather than the 15-degree grid / " + id
                    + "; actual=" + after.yaw.ToString("R") + ", expected=" + expected.ToString("R"));
                Require(Close(after.position, before.position) && Close(after.size, before.size), "wall rotation leaves centre and size fixed / " + id);
            }

            RotationFixture(items =>
            {
                var main = Array.Find(items, item => item.id == "main-console");
                float angle = 5 * Mathf.Deg2Rad;
                main.position = new Vector3(Mathf.Sin(angle) * 52.6f, 0, Mathf.Cos(angle) * 52.6f);
                main.size = new Vector3(1.4f, 1.2f, 1); main.yaw = 0;
            }, "curved outer wall away from symmetry axis");
            Require(planner.Select("main-console"), "select box near curved wall");
            var curveBefore = plan.Get("main-console");
            RotateThroughHandle(11, true, "Shift curved outer wall tangent");
            var curveAfter = plan.Get("main-console");
            float tangent = Mathf.Atan2(curveBefore.position.x, curveBefore.position.z) * Mathf.Rad2Deg;
            Require(Mathf.Abs(Mathf.DeltaAngle(curveAfter.yaw, tangent)) < .02f,
                "Shift follows local curved-wall tangent away from cardinal axes; actual=" + curveAfter.yaw.ToString("R")
                + ", expected=" + tangent.ToString("R"));
            Require(Close(curveAfter.position, curveBefore.position) && Close(curveAfter.size, curveBefore.size),
                "curved-wall alignment does not translate or resize the box");

            foreach (bool near in new[] { true, false })
            {
                RotationFixture(items =>
                {
                    var main = Array.Find(items, item => item.id == "main-console");
                    main.position = new Vector3(0, 0, 49); main.size = new Vector3(1.4f, 1.2f, 1); main.yaw = 0;
                    var food = Array.Find(items, item => item.id == "food");
                    food.position = new Vector3(near ? 2.8f : 8, 0, 49); food.size = Vector3.one; food.yaw = 17;
                }, near ? "near non-grid neighbour" : "distant non-grid neighbour");
                Require(planner.Select("main-console"), "select box for neighbouring rotation alignment");
                RotateThroughHandle(14, true, near ? "near 17-degree box" : "far 17-degree box");
                float actual = plan.Get("main-console").yaw, expected = near ? 17 : 15;
                Require(Mathf.Abs(Mathf.DeltaAngle(actual, expected)) < .02f,
                    (near ? "nearby box supplies its actual non-grid orientation" : "distant box does not magnetize rotation; ordinary 15-degree grid remains")
                    + "; actual=" + actual.ToString("R") + ", expected=" + expected.ToString("R"));
            }

            RotationFixture(items =>
            {
                var food = Array.Find(items, item => item.id == "food");
                food.position = new Vector3(3, 0, 49); food.size = Vector3.one; food.yaw = 0;
            }, "rotation would collide with neighbouring box");
            Require(planner.Select("main-console"), "select collision test box");
            string collisionInitial = Signature(); handle = planner.GetRotationHandlePlanPoint(); target = RotationPointerTarget(26.5f);
            Require(planner.BeginPointerEdit(handle), "begin rotation with real neighbouring collider");
            Require(!planner.DragPointer(target, false, out string collisionReason) && !string.IsNullOrWhiteSpace(collisionReason),
                "rotated collision is rejected with an explanation / " + collisionReason);
            Require(Signature() == collisionInitial, "rejected rotation leaves every position, size and angle unchanged");
            planner.EndPointerEdit();
            Require(Signature() == collisionInitial, "ending rejected gesture does not commit invalid geometry");
            Require(plan.TryApply(accepted, out string restoreReason), "restore accepted arrangement after rotation fixtures / " + restoreReason);
            Require(planner.Select("main-console"), "central rotation handle visible in final planner captures");
            CheckBindings();
        }

        private void CheckTerminals()
        {
            foreach (var terminal in operations.GetComponentsInChildren<StationTerminal>().Where(t => t.GetComponentInParent<CommandRoomBlock>() != null))
            {
                var block = terminal.GetComponentInParent<CommandRoomBlock>();
                Require(block != null, "functional terminal belongs to one planned block / " + terminal.kind);
                Vector3 feet = terminal.transform.TransformPoint(new Vector3(0, .04f, -.9f));
                Require(interior.HasStandingClearance(feet), "real terminal front has standing room / " + terminal.kind);
                mode.RestoreInterior(feet, false, terminal.transform.eulerAngles.y, 0); Physics.SyncTransforms();
                Require(operations.NearestTerminal(mode.FeetPosition) == terminal, "E target reachable at planned front / " + terminal.kind);
                Require(operations.InteractionPrompt(mode.FeetPosition).Contains(terminal.label), "correct interaction hint / " + terminal.kind);
                operations.OpenTerminal(operations.NearestTerminal(mode.FeetPosition));
                Require(operations.PanelOpen && SettingsMenu.BlocksInput, "original terminal panel still opens / " + terminal.kind);
                operations.ClosePanel();
            }
        }

        private void CheckPlayerOverlapProtection()
        {
            var beforePlan = plan.Snapshot(); Vector3 beforeFeet = mode.FeetPosition;
            float yaw = mode.ViewYaw, pitch = mode.ViewPitch;
            var main = plan.Get("main-console");
            Vector3 localFeet = interior.transform.InverseTransformPoint(beforeFeet);
            main.position = new Vector3(localFeet.x, 0, localFeet.z);
            Require(plan.TrySet(main, out string reason), "valid layout may deliberately place a block over the walker / " + reason);
            mode.RevalidateRoomPlanPosition(); Physics.SyncTransforms();
            Require(!Close(mode.FeetPosition, beforeFeet) && !interior.OverlapsCommandBox(mode.FeetPosition)
                && interior.HasStandingClearance(mode.FeetPosition), "overlapped walker is relocated once to genuinely clear floor");
            Require(Mathf.Abs(mode.ViewYaw - yaw) < .001f && Mathf.Abs(mode.ViewPitch - pitch) < .001f,
                "block overlap recovery preserves current looking angles");
            Vector3 recovered = mode.FeetPosition; mode.RevalidateRoomPlanPosition();
            Require(Close(mode.FeetPosition, recovered), "safe standing position is not repeatedly reset by planning");
            mode.RestoreInterior(beforeFeet, false, yaw, pitch);
            Require(!interior.OverlapsCommandBox(mode.FeetPosition) && interior.HasStandingClearance(mode.FeetPosition),
                "restoring an old saved position inside a new block also chooses clear floor");
            Require(plan.TryApply(beforePlan, out reason), "restore accepted room after overlap fixture / " + reason);
            mode.RestoreInterior(beforeFeet, false, yaw, pitch);
            Require(Close(mode.FeetPosition, beforeFeet), "unobstructed saved location remains exact after restoring room");
        }

        private void CheckProtectedSpawnFallback()
        {
            var beforePlan = plan.Snapshot(); Vector3 beforeFeet = mode.FeetPosition;
            float yaw = mode.ViewYaw, pitch = mode.ViewPitch;
            var main = plan.Get("main-console");
            main.position = new Vector3(4, 0, 39.4f); main.size = new Vector3(1, 1.2f, 1); main.yaw = 0;
            Require(plan.TrySet(main, out string reason), "small central console near the inner wall is a valid room placement / " + reason);
            Transform root = plan.BoundRoot("main-console");
            Vector3 nominalSpawn = root.TransformPoint(new Vector3(0, .05f, -main.size.z * .5f - 1.1f));
            Require(!interior.HasStandingClearance(nominalSpawn), "nominal console-front spawn would intersect the real static wall");
            Vector3 fallback = World(new Vector3(0, .05f, 40));
            Require(Close(interior.SpawnPosition, fallback) && interior.HasStandingClearance(interior.SpawnPosition),
                "spawn selects the protected entrance when console-front floor has no actual clearance");
            mode.RestoreInterior(World(main.position + Vector3.up * .05f), false, yaw, pitch);
            Require(Close(mode.FeetPosition, fallback) && interior.HasStandingClearance(mode.FeetPosition),
                "restoring an obstructed player uses the physically clear fallback rather than the wall");
            Require(plan.TryApply(beforePlan, out reason), "restore room after protected-spawn fixture / " + reason);
            mode.RestoreInterior(beforeFeet, false, yaw, pitch);
            Require(Close(mode.FeetPosition, beforeFeet), "protected-spawn fixture restores original player position");
        }

        private IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.35f); yield return new WaitForEndOfFrame();
            string file = Path.Combine(Folder, name + ".png"); ScreenCapture.CaptureScreenshot(Path.GetFullPath(file));
            yield return new WaitForSecondsRealtime(.45f);
            Guard(() =>
            {
                var pixels = new Texture2D(2, 2);
                try
                {
                    Require(File.Exists(file) && pixels.LoadImage(File.ReadAllBytes(file)), "screenshot is readable / " + name);
                    Require(pixels.width == Screen.width && pixels.height == Screen.height, "UI capture uses actual framebuffer / " + name);
                    int lit = 0; foreach (var pixel in pixels.GetPixels32()) if (pixel.r > 45 || pixel.g > 45 || pixel.b > 45) lit++;
                    Require(lit > 10000, "capture contains rendered planner rather than a black hidden window / " + name);
                    pictures.Add(name + ".png");
                }
                finally { Destroy(pixels); }
            });
        }

        private void CaptureRoom()
        {
            var camera = new GameObject("Command blockout inspection camera").AddComponent<Camera>(); camera.CopyFrom(Camera.main); camera.enabled = false;
            camera.transform.position = World(new Vector3(0, 3.1f, 39.6f)); camera.transform.LookAt(World(new Vector3(0, 1.25f, 47.3f)));
            camera.fieldOfView = 91; camera.nearClipPlane = .06f; camera.farClipPlane = 500;
            var render = new RenderTexture(1600, 1000, 24); var prior = RenderTexture.active; camera.targetTexture = render;
            operations.RefreshViewLighting(camera, true);
            try
            {
                camera.Render(); RenderTexture.active = render;
                var pixels = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
                try
                {
                    pixels.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); pixels.Apply();
                    int lit = 0; foreach (var pixel in pixels.GetPixels32()) if (pixel.r > 35 || pixel.g > 35 || pixel.b > 35) lit++;
                    Require(lit > 10000, "direct 3D room render contains lit blockout geometry");
                    const string name = "03-kommando-boxen-3d.png"; File.WriteAllBytes(Path.Combine(Folder, name), pixels.EncodeToPNG()); pictures.Add(name);
                }
                finally { Destroy(pixels); }
            }
            finally
            {
                RenderTexture.active = prior; camera.targetTexture = null; render.Release(); Destroy(render); Destroy(camera.gameObject);
                operations.RefreshViewLighting(Camera.main, StationInteriorMode.IsInside);
            }
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(Folder); File.Delete(Path.Combine(Folder, "result.json")); File.Delete(Path.Combine(Folder, "error.txt"));
            yield return new WaitForSecondsRealtime(1);
            if (!Guard(() =>
            {
                FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride = Path.GetFullPath(Path.Combine(Folder, "Saves"));
                CinematicLibrary.PreferencePrefix = "SpaceMiner.Test.CommandRoomPlanner." + Guid.NewGuid() + ".";
                Require(SettingsStore.PathName.StartsWith(Path.GetFullPath(Folder), StringComparison.OrdinalIgnoreCase), "acceptance settings isolated");
                FindFirstObjectByType<StartMenu>().StartDemo(); FindFirstObjectByType<IntroSequence>().Skip();
            })) yield break;
            yield return null; yield return null;
            if (!Guard(() =>
            {
                mode = StationInteriorMode.Current; interior = mode.Layout; scenario = FindFirstObjectByType<WaterScenario>(); operations = scenario.Operations;
                scenario.SetSimulationRate(0); scenario.Scanner.DismissDialogue();
                plan = operations.RoomPlan; planner = FindFirstObjectByType<CommandRoomPlanner>();
                Require(plan != null && planner != null && operations.Live, "live room data, controller and campaign installed");
                Require(planner.Layout == plan, "planner edits the real room data");
                plan.ResetDefaults(); Physics.SyncTransforms(); CheckBindings(); CheckRejectedPlans();
                mode.RestoreInterior(World(new Vector3(0, .05f, 44)), false, interior.SpawnYaw + 17, -7);
                priorFeet = mode.FeetPosition; priorYaw = mode.ViewYaw; priorPitch = mode.ViewPitch;
                priorCamera = Camera.main.transform.position; priorCameraRotation = Camera.main.transform.rotation;
                priorRate = 3; scenario.SetSimulationRate(priorRate); SettingsStore.Current.Gameplay.PauseInMenu = false;
                Require(planner.Open() && CommandRoomPlanner.IsOpen, "developer planner opens from interior");
                Require(SettingsMenu.BlocksInput && SettingsMenu.PausesSimulation, "planner always blocks walking and pauses simulation even when menu pause disabled");
                pausedElapsed = operations.State.elapsed; pausedWater = scenario.WaterLiters; pausedBattery = operations.State.power.storedKwh;
                mode.Walk(Vector2.up, .4f, true);
                Require(Close(mode.FeetPosition, priorFeet), "real CharacterController movement blocked by planner");
            })) yield break;
            // Recreate the Windows backbuffer: unchanged hidden launch dimensions may capture black.
            Screen.SetResolution(1440, 900, FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(.3f);
            Screen.SetResolution(1600, 1000, FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(.55f);
            if (!Guard(() =>
            {
                Require(operations.State.elapsed == pausedElapsed && scenario.WaterLiters == pausedWater && operations.State.power.storedKwh == pausedBattery,
                    "actual frame updates leave campaign clock and resources paused");
                Require(planner.PlanViewport.width > 350 && planner.PlanViewport.height > 250, "actual rendered ground plan has useful area");
                CheckPointerAndHistory(); CheckRotationGestures(); CheckPersistence();
            })) yield break;
            yield return Capture("01-grundriss-1600");
            if (failed) yield break;
            if (!Guard(() =>
            {
                SettingsStore.Current.Accessibility.TextScale = 1.4f; Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            })) yield break;
            yield return new WaitForSecondsRealtime(.5f);
            if (!Guard(() =>
            {
                Require(planner.PlanViewport.width > 250 && planner.PlanViewport.height > 200, "ground plan remains usable at 1280 and enlarged text");
                foreach (var item in plan.Items)
                    foreach (var corner in planner.GetItemPlanPolygon(item.id))
                        Require(planner.PlanViewport.Contains(corner), "all plan handles stay inside viewport / " + item.id);
            })) yield break;
            yield return Capture("02-grundriss-1280-grosse-schrift");
            if (failed) yield break;
            if (!Guard(() =>
            {
                planner.Close(); Require(!CommandRoomPlanner.IsOpen, "planner closes");
                Require(Close(mode.FeetPosition, priorFeet) && Mathf.Abs(mode.ViewYaw - priorYaw) < .001f && Mathf.Abs(mode.ViewPitch - priorPitch) < .001f,
                    "closing planner preserves exact player location and looking angles");
                Require(Close(Camera.main.transform.position, priorCamera) && Quaternion.Angle(Camera.main.transform.rotation, priorCameraRotation) < .01f,
                    "closing planner preserves camera pose");
                Require(scenario.SimulationRate == priorRate, "closing restores the previous simulation speed");
                scenario.SetSimulationRate(0); SettingsStore.Current.Accessibility.TextScale = 1;
                var droneBox = plan.Get("drones"); droneBox.position.x = -7.5f; droneBox.size.z = 3;
                Require(plan.TrySet(droneBox, out string reason), "resize terminal beyond old interaction radius with enough room / " + reason);
            })) yield break;
            yield return null; yield return null;
            if (!Guard(CheckTerminals)) yield break;
            // Each terminal close also consumes its frame. The central scanner uses
            // the public input guard and must be approached on a subsequent frame.
            yield return null; yield return null;
            if (!Guard(() =>
            {
                var main = plan.Get("main-console"); var root = plan.BoundRoot("main-console");
                Require(interior.Console.IsChildOf(root) || interior.Console == root, "scanner interaction anchor follows moved central console");
                Vector3 approach = root.TransformPoint(new Vector3(0, .04f, -main.size.z * .5f - .75f));
                Require(interior.HasStandingClearance(approach), "moved central console has real front walking clearance");
                mode.RestoreInterior(approach, false, root.eulerAngles.y, 0);
                Require(mode.NearConsole && mode.OpenConsole() && mode.ConsoleOpen, "original central scanner opens at its edited position");
                mode.CloseConsole(); CaptureRoom(); CheckPlayerOverlapProtection(); CheckProtectedSpawnFallback();
                File.WriteAllText(Path.Combine(Folder, "result.json"), JsonUtility.ToJson(new Result
                { passed = true, checks = checks, layoutRevision = StationInteriorLayout.Revision, pictures = pictures.ToArray() }, true));
                Debug.Log("COMMAND ROOM PLANNER CHECK PASSED: " + checks + " checks");
            })) yield break;
            Application.Quit(0);
        }
    }
}
#endif
