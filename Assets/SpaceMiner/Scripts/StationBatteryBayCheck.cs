#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    // Isolated scene/model acceptance. Bank views must not become another inventory.
    public sealed class StationBatteryBayCheck : MonoBehaviour
    {
        private const string Folder = "Logs/StationBatteryBays";
        private StationOperations operations;
        private WaterScenario scenario;
        private StationInteriorLayout layout;
        private StationInteriorMode interior;
        private OrbitCamera orbit;
        private int checks, frustumWarnings;
        private bool failed;
        private readonly List<string> pictures = new List<string>();
        [Serializable] private sealed class Report
        {
            public bool passed;
            public int checks, layout, frustumWarnings;
            public string[] pictures;
            public string scope = "Eight real reactor-axis battery housings, ring-wall apertures, pressure backing, lane and character collisions; imported three-LOD inserts; two installed banks at0 degrees across the command entrance, active01 bound to the existing24kWh power store and02 isolated. Explicit zero-power/7.25kWh old-save fixtures are restored, no resource grants. Four native framebuffer views use the normal game camera, without an additional render camera or lamp. Programmatic scene/controller acceptance, not native OS input, manual comfort, headset or FPS acceptance.";
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-stationBatteryBayCheck") >= 0)
                new GameObject("Station battery bay acceptance").AddComponent<StationBatteryBayCheck>();
        }
        private void OnEnable() => Application.logMessageReceived += ObserveLog;
        private void OnDisable() => Application.logMessageReceived -= ObserveLog;
        private void ObserveLog(string message, string stack, LogType type)
        { if (message.IndexOf("Screen position out of view frustum", StringComparison.OrdinalIgnoreCase) >= 0) frustumWarnings++; }
        private void Require(bool condition, string label)
        { if (!condition) throw new Exception("BATTERY BAYS: " + label); checks++; Debug.Log("BATTERY BAYS OK: " + label); }
        private bool Guard(Action action)
        {
            try { action(); return true; }
            catch (Exception error) { failed = true; File.WriteAllText(Folder + "/error.txt", error.ToString()); Debug.LogException(error); Application.Quit(1); return false; }
        }
        private Vector3 World(Vector3 local) => layout.transform.TransformPoint(local);
        private Vector3 Spoke(int axis, Vector3 local) => Quaternion.Euler(0, axis * 90, 0) * local;
        private static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
        private static SavedGame Copy(SavedGame saved) => JsonUtility.FromJson<SavedGame>(JsonUtility.ToJson(saved));
        private string Campaign() => JsonUtility.ToJson(operations.Capture());
        private void UpdateBanks() { foreach (var bank in operations.BatteryBays) bank.UpdateState(); }
        private static long Triangles(Renderer renderer)
        {
            var filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return 0;
            long count = 0; for (int i = 0; i < filter.sharedMesh.subMeshCount; i++) count += (long)filter.sharedMesh.GetIndexCount(i) / 3;
            return count;
        }
        private void CheckAssetsAndPlacement()
        {
            var geometry = layout.Geometry;
            Require(operations.Live && geometry.BatteryBayAnchors.Count == 8 && geometry.BatteryBayFrames.Count == 8 && operations.BatteryBays.Count == 8,
                "eight real housing anchors, frames and bound runtime views");
            int installed = 0, coupled = 0;
            var roots = new HashSet<Transform>(); var identities = new HashSet<string>();
            for (int index = 0; index < 8; index++)
            {
                int axis = index / 2, side = index % 2 == 0 ? 1 : -1;
                var anchor = geometry.BatteryBayAnchors[index]; var frame = geometry.BatteryBayFrames[index]; var bank = operations.BatteryBays[index];
                Vector3 local = layout.transform.InverseTransformPoint(anchor.position);
                Require((local - Spoke(axis, new Vector3(side * 3.5f, .06f, 28.10f))).magnitude < .005f,
                    "mount follows the actual rotated spoke / " + index);
                Require(Quaternion.Angle(anchor.rotation, layout.transform.rotation * Quaternion.Euler(0, axis * 90 + 180, 0)) < .01f,
                    "service face points toward the ring / " + index);
                Require(bank.Index == index && bank.InstanceRoot != null && roots.Add(bank.InstanceRoot) && !string.IsNullOrEmpty(bank.SelectableId)
                    && identities.Add(bank.SelectableId), "one stable unique runtime view and selectable identity / " + index);
                Require(bank.Installed == (index == StationBatteryPlacement.ActiveIndex || index == StationBatteryPlacement.ReserveIndex) && bank.Coupled == (index == StationBatteryPlacement.ActiveIndex), "only opposing pair installed and only01 coupled / " + index);
                installed += bank.Installed ? 1 : 0; coupled += bank.Coupled ? 1 : 0;
                Bounds bounds = bank.ModelBounds;
                Require(Finite(bounds.center.x) && Finite(bounds.center.y) && Finite(bounds.center.z) && Finite(bounds.size.x) && Finite(bounds.size.y)
                    && Finite(bounds.size.z) && bounds.size.x > 0 && bounds.size.y > 0 && bounds.size.z > 0, "finite positive actual model bounds / " + index);
                Require(bank.HighDetailTriangles >= (bank.Installed ? 10000 : 1000) && bank.DetailRendererCount >= 4,
                    "actual detailed imported model, rather than a primitive substitute / " + index);
                Require(bounds.size.x <= 2.36f && bounds.size.y <= 3.4f && bounds.size.z <= 3.3f && bounds.min.y >= -.02f,
                    "instance-local model fits the declared service insert envelope / " + index);
                foreach (var rack in bank.InstanceRoot.GetComponentsInChildren<Transform>(true)) if (rack.name == "Rack")
                    Require(rack.gameObject.activeSelf == bank.Installed, "only installed banks contain a visible cassette rack / " + index);
                var groups = bank.InstanceRoot.GetComponentsInChildren<LODGroup>(true);
                Require(groups.Length == 1 && groups[0].lodCount == 3, "one actual three-level LOD controller / " + index);
                var seen = new HashSet<Renderer>(); long previous = long.MaxValue;
                foreach (var lod in groups[0].GetLODs())
                {
                    long triangles = 0;
                    Require(lod.renderers != null && lod.renderers.Length > 0, "nonempty LOD / " + index);
                    foreach (var renderer in lod.renderers)
                    {
                        Require(renderer != null && seen.Add(renderer), "no renderer is assigned to duplicate LOD geometry / " + index);
                        triangles += Triangles(renderer);
                    }
                    Require(triangles > 0 && triangles < previous, "strictly decreasing real LOD triangle budget / " + index);
                    previous = triangles;
                }
                float maxRadius = 0; bool finiteVertices = true;
                foreach (var filter in frame.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mesh = filter.sharedMesh; if (mesh == null) continue;
                    // Runtime shell meshes are readable. Imported meshes may deliberately
                    // disable CPU copies; their own oriented mesh bounds remain available.
                    if (mesh.isReadable) foreach (var vertex in mesh.vertices)
                    {
                        Vector3 p = layout.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                        finiteVertices &= Finite(p.x) && Finite(p.y) && Finite(p.z);
                        maxRadius = Mathf.Max(maxRadius, new Vector2(p.x, p.z).magnitude);
                    }
                    else for (int corner = 0; corner < 8; corner++)
                    {
                        var e = mesh.bounds.extents;
                        Vector3 p = mesh.bounds.center + new Vector3((corner & 1) == 0 ? -e.x : e.x, (corner & 2) == 0 ? -e.y : e.y, (corner & 4) == 0 ? -e.z : e.z);
                        p = layout.transform.InverseTransformPoint(filter.transform.TransformPoint(p));
                        maxRadius = Mathf.Max(maxRadius, new Vector2(p.x, p.z).magnitude);
                    }
                }
                Require(finiteVertices && maxRadius > 23 && maxRadius <= 29.301f, "finite complete housing and insert remain inside r29.3 / " + index + " / r=" + maxRadius);
                string names = ""; foreach (var transform in frame.GetComponentsInChildren<Transform>(true)) names += transform.name + "\n";
                Require(names.Contains("Druckhaut") && names.Contains("Schutzhaut") && names.Contains("Tragwerk"), "pressure skin, protection and connecting load frame / " + index);
            }
            Require(installed == 2 && coupled == 1, "two installed modules and no extra contributing battery");
            Require(layout.transform.InverseTransformPoint(geometry.BatteryBayAnchors[0].position).x > 0
                && layout.transform.InverseTransformPoint(geometry.BatteryBayAnchors[0].position).z > 0
                && layout.transform.InverseTransformPoint(layout.Console.position).z > 0,
                "active01 shares command axis and faces its entrance across the corridor");
            foreach (string lod in new[] { "BatteryBay/BatteryBay_LOD0", "BatteryBay/BatteryBay_LOD1", "BatteryBay/BatteryBay_LOD2" })
                Require(Resources.Load<GameObject>(lod) != null, "imported LOD resource exists / " + lod);
        }
        private bool FrameRay(Transform frame, Vector3 from, Vector3 direction, float length)
        {
            foreach (var hit in Physics.RaycastAll(frame.TransformPoint(from), frame.TransformDirection(direction), length, 1 << 25, QueryTriggerInteraction.Ignore))
                if (hit.collider.transform.IsChildOf(frame) && hit.collider.enabled && hit.collider.name.Contains("Druck")) return true;
            return false;
        }
        private void CheckPhysics()
        {
            Physics.SyncTransforms();
            for (int index = 0; index < 8; index++)
            {
                int axis = index / 2, side = index % 2 == 0 ? 1 : -1;
                var frame = layout.Geometry.BatteryBayFrames[index];
                foreach (float u in new[] { side * 2.55f, side * 3.5f, side * 4.45f }) foreach (float y in new[] { .5f, 1.6f, 3.35f })
                {
                    Vector3 from = World(Spoke(axis, new Vector3(u, y, 29.7f)));
                    Vector3 direction = layout.transform.TransformDirection(Spoke(axis, Vector3.back));
                    Require(!Physics.Raycast(from, direction, 1.04f, 1 << 25, QueryTriggerInteraction.Ignore),
                        "real opening has no residual ring-wall collider / " + index + " / u=" + u + " / y=" + y);
                }
                Vector3 room = new Vector3(side * 3.5f, 1.5f, 26);
                Require(FrameRay(frame, room, Vector3.back, 2.5f), "physical sealed rear pressure wall / " + index);
                Require(FrameRay(frame, room, Vector3.left, 1.6f) && FrameRay(frame, room, Vector3.right, 1.6f), "both pressure side walls collide / " + index);
                Require(FrameRay(frame, room, Vector3.down, 1.65f) && FrameRay(frame, room, Vector3.up, 2.5f), "floor and roof pressure backing collide / " + index);
            }
            foreach (var gate in layout.Geometry.PressureGates) if (gate.Label.StartsWith("Reaktorschott /"))
            {
                Require(Mathf.Abs(gate.ClearWidth - 2.9f) < .001f && gate.LeafCount == 8, "compact reactor shutter preserves2.9m clear width with eight leaves / " + gate.Label);
                float max = 0;
                foreach (var collider in gate.GetComponentsInChildren<BoxCollider>()) if (collider.name == "Druckschott / Schiebefluegel")
                {
                    for (int corner = 0; corner < 8; corner++)
                    {
                        var e = collider.size * .5f;
                        var p = collider.center + new Vector3((corner & 1) == 0 ? -e.x : e.x, (corner & 2) == 0 ? -e.y : e.y, (corner & 4) == 0 ? -e.z : e.z);
                        max = Mathf.Max(max, Mathf.Abs(gate.transform.InverseTransformPoint(collider.transform.TransformPoint(p)).x));
                    }
                }
                Require(max > 1.4f && max <= 1.951f, "real leaf envelopes fit pockets clear of bay jambs / " + gate.Label + " / u=" + max);
            }
            for (int sector = 0; sector < 12; sector++) if (!RingWalkthrough.DamagedRingSector(sector))
                foreach (float delta in new[] { -6f, 0, 6f }) foreach (float radius in new[] { 30.05f, 32.2f, 35.4f, 36.9f })
                {
                    Vector3 feet = World(RingWalkthrough.At(radius, sector * 30 + delta, .04f));
                    // Existing wall utilities and fittings already occupy some
                    // outer-lane edge samples. Assert the actual regression:
                    // no new bay hull or insert projects into any ring lane.
                    bool bayObstruction = false;
                    foreach (var hit in Physics.OverlapCapsule(feet + Vector3.up * .33f, feet + Vector3.up * 1.5f,
                        .28f, 1 << 25, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.GetComponentInParent<StationBatteryBay>() != null) bayObstruction = true;
                        foreach (var bayFrame in layout.Geometry.BatteryBayFrames)
                            if (hit.transform.IsChildOf(bayFrame)) bayObstruction = true;
                    }
                    Require(layout.ContainsPosition(feet) && !bayObstruction,
                        "battery additions do not obstruct existing ring pedestrian/drone lanes / " + sector + " / r=" + radius + " / delta=" + delta);
                }
            Require(!layout.ContainsPosition(World(RingWalkthrough.At(33.5f, 90, .04f)))
                && operations.Element("sector-3").condition < .3f && operations.Element("service-1").condition < .3f,
                "empty90-degree bay neither repairs nor opens the damaged ring or service route");
            var damagedGate = layout.Geometry.PressureGates.Find(g => g.Label == "Reaktorschott / 90");
            Require(damagedGate != null && damagedGate.Locked && !damagedGate.TargetOpen, "damaged90-degree front retains its locked physical pressure door");
        }
        private void CheckSharedPowerAndSaves()
        {
            var original = Copy(SaveGameStore.Capture(scenario, "Battery acceptance original", ""));
            string inventory = JsonUtility.ToJson(operations.State.inventory), campaign = Campaign();
            var power = operations.State.power;
            Require(Mathf.Abs(power.capacityKwh - 24) < .001f && Mathf.Abs(power.storedKwh - 12) < .02f, "normal campaign retains existing24kWh capacity and12kWh stock");
            for (int i = 0; i < 30; i++) UpdateBanks();
            Require(ReferenceEquals(power, operations.State.power) && Campaign() == campaign, "bank views do not allocate, refill or consume another inventory");
            string active = operations.BatteryBays[StationBatteryPlacement.ActiveIndex].VisualState.ToString(), offline = operations.BatteryBays[StationBatteryPlacement.ReserveIndex].VisualState.ToString();
            Require(active != offline, "active and isolated installed modules have distinct actual visual state");
            try
            {
                // Declared blackout fixture: no imported energy, no optional second store.
                power.storedKwh = 0; power.Advance(1, 0, .53f, 0, 0); UpdateBanks();
                Require(power.storedKwh == 0 && !power.LifePowered && !operations.BatteryBays[StationBatteryPlacement.ReserveIndex].Coupled,
                    "isolated02 cannot supply the explicit empty-store/no-generation blackout fixture");
                Require(operations.BatteryBays[StationBatteryPlacement.ActiveIndex].VisualState.ToString() != active, "active module display follows actual power blackout");
                Require(operations.BatteryBays[StationBatteryPlacement.ReserveIndex].VisualState.ToString() == offline && JsonUtility.ToJson(operations.State.inventory) == inventory,
                    "blackout leaves offline02 and resource inventories unchanged");
                var legacy = Copy(original); legacy.stationCampaign.power.storedKwh = 7.25f;
                SaveGameStore.Validate(legacy, scenario); SaveGameStore.Apply(legacy, scenario); scenario.Scanner.DismissDialogue(); operations.ClosePanel(); UpdateBanks();
                Require(Mathf.Abs(operations.State.power.storedKwh - 7.25f) < .0001f && operations.State.power.capacityKwh == 24,
                    "old global power store loads without a bank reset or free fill; explicit7.25kWh fixture");
                Require(operations.BatteryBays[StationBatteryPlacement.ActiveIndex].Coupled && !operations.BatteryBays[StationBatteryPlacement.ReserveIndex].Coupled && JsonUtility.ToJson(operations.State.inventory) == inventory,
                    "existing save keeps active/offline binding and shared inventories");
                string file = SaveGameStore.Write(SaveGameStore.Capture(scenario, "Battery acceptance roundtrip", "explicit7.25kWh stock fixture"), false, Path.GetFullPath(Folder + "/Saves"));
                var roundtrip = SaveGameStore.Read(file); SaveGameStore.Validate(roundtrip, scenario);
                Require(roundtrip.stationCampaign.power.storedKwh == 7.25f && roundtrip.stationCampaign.power.capacityKwh == 24,
                    "disk roundtrip preserves the one existing power store");
            }
            finally { SaveGameStore.Apply(original, scenario); scenario.Scanner.DismissDialogue(); operations.ClosePanel(); UpdateBanks(); }
            Require(Campaign() == campaign && operations.State.power.storedKwh == original.stationCampaign.power.storedKwh,
                "all declared power fixtures restore original campaign resources");
        }
        private void CheckCharacter()
        {
            interior.CloseConsole();
            interior.RestoreInterior(World(new Vector3(3.5f, .04f, 33.3f)), false, layout.transform.eulerAngles.y + 270, 0);
            Vector3 before = interior.FeetPosition;
            for (int step = 0; step < 60; step++) interior.Walk(Vector2.up, 1f / 60);
            Vector3 after = layout.transform.InverseTransformPoint(interior.FeetPosition);
            Require(Vector3.Distance(before, interior.FeetPosition) > 5 && after.y > -.2f && new Vector2(after.x, after.z).magnitude > 30,
                "actual CharacterController walks across intact ring in front of0-degree pair / distance="
                + Vector3.Distance(before, interior.FeetPosition) + " / local=" + after);
            interior.RestoreInterior(World(Spoke(2, new Vector3(3.5f, .04f, 30.4f))), false, layout.transform.eulerAngles.y, 0);
            for (int step = 0; step < 100; step++) interior.Walk(Vector2.up, 1f / 60);
            after = Quaternion.Euler(0, -180, 0) * layout.transform.InverseTransformPoint(interior.FeetPosition);
            Require(after.z >= 24 && after.z < 26.2f && after.y > -.2f && Mathf.Abs(after.x - 3.5f) < .1f,
                "actual walker enters empty service bay and is physically stopped by its rear enclosure");
            interior.RestoreInterior(World(new Vector3(3.5f, .04f, 30.4f)), false, layout.transform.eulerAngles.y + 180, 0);
            for (int step = 0; step < 90; step++) interior.Walk(Vector2.up, 1f / 60);
            after = layout.transform.InverseTransformPoint(interior.FeetPosition);
            Require(after.z >= 26.9f && after.z < 28.2f && after.y > -.2f && Mathf.Abs(after.x - 3.5f) < .1f,
                "installed cassette collider preserves the front maintenance zone and prevents walking through batteries");
            interior.RestoreInterior(World(RingWalkthrough.At(30.4f, 180, .04f)), false, layout.transform.eulerAngles.y, 0);
            for (int step = 0; step < 60; step++) interior.Walk(Vector2.up, 1f / 60);
            after = layout.transform.InverseTransformPoint(interior.FeetPosition);
            var closedGate = layout.Geometry.PressureGates.Find(g => g.Label == "Reaktorschott / 180");
            Vector3 gateFeet = closedGate.transform.InverseTransformPoint(interior.FeetPosition);
            Require(closedGate.Locked && !closedGate.TargetOpen && gateFeet.z > .05f && gateFeet.z < 1.2f && after.y > -.2f,
                "real walker remains outside the actual closed180-degree shutter plane / gate-local=" + gateFeet);
        }
        private void InteriorView(float angle, float radius, float yawOffset = 180)
        {
            operations.ClosePanel(); scenario.Scanner.DismissDialogue();
            interior.RestoreInterior(World(RingWalkthrough.At(radius, angle, .04f)), false, layout.transform.eulerAngles.y + angle + yawOffset, -3);
            Camera.main.fieldOfView = 76;
        }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.2f); yield return new WaitForEndOfFrame();
            string path = Path.GetFullPath(Folder + "/" + name + ".png"); ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSecondsRealtime(.3f);
            Guard(() =>
            {
                var image = new Texture2D(2, 2);
                try
                {
                    Require(File.Exists(path) && image.LoadImage(File.ReadAllBytes(path)) && image.width == Screen.width && image.height == Screen.height,
                        "native game-camera framebuffer / " + name);
                    int visible = 0; foreach (var p in image.GetPixels32()) if (p.r > 40 || p.g > 40 || p.b > 40) visible++;
                    Require(visible > 1000, "framebuffer contains actual lit scene / " + name); pictures.Add(name + ".png");
                }
                finally { Destroy(image); }
            });
        }
        private IEnumerator Start()
        {
            Directory.CreateDirectory(Folder); File.Delete(Folder + "/result.json"); File.Delete(Folder + "/error.txt");
            yield return new WaitForSecondsRealtime(1);
            if (!Guard(() =>
            {
                FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride = Path.GetFullPath(Folder + "/Saves");
                CinematicLibrary.PreferencePrefix = "SpaceMiner.Test.BatteryBays." + Guid.NewGuid().ToString("N");
                Require(SettingsStore.PathName.StartsWith(Path.GetFullPath(Folder), StringComparison.OrdinalIgnoreCase), "isolated battery acceptance settings");
                FindFirstObjectByType<StartMenu>().StartDemo(); FindFirstObjectByType<IntroSequence>().Skip();
            })) yield break;
            yield return null; yield return null;
            if (!Guard(() =>
            {
                operations = StationOperations.Current; scenario = operations.Scenario; layout = operations.Layout; interior = StationInteriorMode.Current; orbit = Camera.main.GetComponent<OrbitCamera>();
                scenario.SetSimulationRate(0); scenario.Scanner.DismissDialogue(); operations.ClosePanel();
                foreach (var room in operations.RoomPlans) Require(room.SavePath.StartsWith(Path.GetFullPath(Folder), StringComparison.OrdinalIgnoreCase), "room plan isolated / " + room.RoomId);
                UpdateBanks(); CheckAssetsAndPlacement(); CheckPhysics(); CheckSharedPowerAndSaves();
            })) yield break;
            // Closing a real terminal consumes that frame's movement input.
            // The walkthrough must begin after that normal input guard expires.
            yield return null; yield return null;
            if (!Guard(CheckCharacter)) yield break;
            Screen.SetResolution(1440, 900, FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(.3f);
            Screen.SetResolution(1600, 1000, FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(.6f);
            if (!Guard(() => { orbit.ShowHud = false; InteriorView(0, 33.5f); })) yield break;
            yield return Capture("01-gegenueber-zentrale-01-aktiv-02-aus"); if (failed) yield break;
            if (!Guard(() => InteriorView(180, 33.5f))) yield break;
            yield return Capture("02-hundertachtziggrad-leere-buchten"); if (failed) yield break;
            if (!Guard(() =>
            {
                interior.RestoreInterior(World(new Vector3(3.5f, .04f, 30.1f)), false, layout.transform.eulerAngles.y + 180, -4); Camera.main.fieldOfView = 58;
            })) yield break;
            yield return Capture("03-aktive-kassetten-und-servicefront"); if (failed) yield break;
            if (!Guard(() =>
            {
                interior.Exit(true); orbit.ResetView(); orbit.FocusPoint(World(new Vector3(0, 1.8f, 25.7f)), 12); orbit.Orbit(new Vector2(164, -26));
                operations.RefreshViewLighting(Camera.main, false);
            })) yield break;
            yield return Capture("04-aussen-doppelhuelle-und-anschluesse"); if (failed) yield break;
            if (!Guard(() =>
            {
                Require(frustumWarnings == 0, "no screen-frustum warnings across real camera views");
                File.WriteAllText(Folder + "/result.json", JsonUtility.ToJson(new Report { passed = true, checks = checks, layout = StationInteriorLayout.Revision, pictures = pictures.ToArray(), frustumWarnings = frustumWarnings }, true));
                Debug.Log("STATION BATTERY BAY CHECK PASSED: " + checks + " checks / " + pictures.Count + " native framebuffer captures"); Application.Quit(0);
            })) yield break;
        }
    }
}
#endif
