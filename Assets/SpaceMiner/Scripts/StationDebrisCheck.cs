#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    // Isolated acceptance of the actual first scan, visible wrecks, courier jobs and save data.
    public sealed class StationDebrisCheck : MonoBehaviour
    {
        private const string Folder = "Logs/StationDebris";
        private WaterScenario scenario;
        private StationOperations ops;
        private int checks;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        { if (Array.IndexOf(Environment.GetCommandLineArgs(), "-stationDebrisCheck") >= 0) new GameObject("Station debris acceptance").AddComponent<StationDebrisCheck>(); }

        private void Require(bool value, string description)
        { if (!value) throw new Exception("DEBRIS: " + description); checks++; Debug.Log("DEBRIS OK: " + description); }
        private static T Copy<T>(T value) => JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
        private void SamePath(Vector3[] actual, Vector3[] expected, string description)
        {
            Require(actual != null && expected != null && actual.Length == expected.Length, description + " / waypoint count");
            for (int i = 0; i < actual.Length; i++) Require((actual[i] - expected[i]).sqrMagnitude < .000001f, description + " / waypoint " + i);
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(Folder); File.Delete(Folder + "/result.json"); File.Delete(Folder + "/error.txt");
            yield return new WaitForSecondsRealtime(1);
            FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride = Path.GetFullPath(Folder + "/Saves");
            CinematicLibrary.PreferencePrefix = "SpaceMiner.Test.StationDebris." + Guid.NewGuid().ToString("N");
            FindFirstObjectByType<StartMenu>().StartDemo(); FindFirstObjectByType<IntroSequence>().Skip();
            yield return null; yield return null;
            try
            {
                Check();
                File.WriteAllText(Folder + "/result.json", "{\"passed\":true,\"checks\":" + checks + ",\"scope\":\"five deterministic unknown wrecks; real first scan and collision selection; actual drone retrieval; mid-flight and legacy waypoint persistence; five finite 12 kg + 2 parts recoveries; explicit 80 L test supply\"}");
                Debug.Log("STATION DEBRIS CHECK PASSED: " + checks); Application.Quit(0);
            }
            catch (Exception exception)
            { File.WriteAllText(Folder + "/error.txt", exception.ToString()); Debug.LogException(exception); Application.Quit(1); }
        }

        private void Check()
        {
            scenario = FindFirstObjectByType<WaterScenario>(); ops = scenario.Operations;
            scenario.SetSimulationRate(0); scenario.Scanner.DismissDialogue();
            Require(ops != null && ops.Live, "live campaign installed for this test flag");
            Require(SettingsStore.PathName.StartsWith(Path.GetFullPath(Folder), StringComparison.OrdinalIgnoreCase), "settings isolated from user preferences");
            foreach (var plan in ops.RoomPlans) Require(plan.SavePath.StartsWith(Path.GetFullPath(Folder), StringComparison.OrdinalIgnoreCase), "room plan isolated: " + plan.RoomId);
            Require(!scenario.Scanner.FirstScanComplete && ops.KnownDebrisCount == 0, "fresh campaign has no wreck knowledge");
            for (int i = 0; i < 5; i++)
            {
                string id = "debris-" + i; var root = ops.DebrisRoot(id);
                Require(root != null && !root.gameObject.activeInHierarchy, "unknown contact hidden: " + id);
                Require(!ops.IsDebrisKnown(id) && !ops.SelectDebrisContact(id), "unknown contact cannot be selected: " + id);
                foreach (var collider in root.GetComponentsInChildren<Collider>(true)) Require(!collider.gameObject.activeInHierarchy, "unknown collider inactive: " + id);
                Require(!ops.Element(id).use.Contains("12") && !ops.Element(id).use.Contains("Ersatzteile"), "unknown contents not disclosed: " + id);
                Require(!ops.Enqueue(StationJobKind.Salvage, id), "unknown salvage rejected: " + id);
            }
            var first = ops.DebrisRoot("debris-0"); Render("01-before-scan", first.position + Vector3.one * 7, first.position);
            var mode = StationInteriorMode.Current;
            mode.RestoreInterior(ops.Layout.SpawnPosition, false, ops.Layout.SpawnYaw, 0);
            Require(mode.NearConsole && mode.OpenConsole(), "physical console reached from current authored room plan");
            scenario.Scanner.Advance(15);
            Require(scenario.Scanner.Ready && scenario.Scanner.Scan(), "actual powered first scan succeeds");
            scenario.Scanner.DismissDialogue(); mode.CloseConsole();
            Require(scenario.Scanner.FirstScanComplete && ops.KnownDebrisCount == 5, "first scan adds five unknown wreck contacts");
            Require(scenario.Scanner.KnownCount == Array.FindAll(scenario.Asteroids, a => a.IsScanned).Length, "asteroid scanner count remains asteroid-only");

            var positions = new List<Vector3>(); var sizes = new List<Vector3>();
            for (int i = 0; i < 5; i++)
            {
                string id = "debris-" + i; var root = ops.DebrisRoot(id); positions.Add(root.position);
                Require(root.gameObject.activeInHierarchy && ops.IsDebrisKnown(id), "scanned wreck visible: " + id);
                Require(ops.Element(id).name == "Unbekanntes Teil " + (i + 1).ToString("00"), "scan identifies only contact, not composition: " + id);
                Require(ops.Element(id).use.Contains("unbekannt") && !ops.Element(id).use.Contains("12"), "scan retains unknown mass and contents: " + id);
                Vector3 local = ops.Layout.transform.InverseTransformPoint(root.position);
                Require(new Vector2(local.x, local.z).magnitude > 140 && local.magnitude < ScannerProgression.InitialRangeMeters, "contact outside station, solar and doors but in scanner range: " + id);
                foreach (var asteroid in scenario.Asteroids) Require(Vector3.Distance(root.position, asteroid.transform.position) >= asteroid.Info.DiameterMeters * .5f + 23.9f, "contact separated from asteroid: " + id + " / " + asteroid.name);
                var renderers = root.GetComponentsInChildren<Renderer>(); Require(renderers.Length >= 5, "actual broken frame and skin geometry: " + id);
                var bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds); sizes.Add(bounds.size);
                Require(Pick(root, id), "real world collider picks the correct contact: " + id);
                Require(ops.SelectDebrisContact(id) && Camera.main.GetComponent<OrbitCamera>().Selected == null, "contact selection clears prior ship/drone selection: " + id);
                Render("02-contact-" + (i + 1).ToString("00"), bounds.center + new Vector3(1.6f, 1.5f, -1.8f).normalized * Mathf.Max(3.8f, bounds.size.magnitude * 1.4f), bounds.center);
            }
            float minY = float.MaxValue, maxY = float.MinValue; foreach (var position in positions) { minY = Mathf.Min(minY, position.y); maxY = Mathf.Max(maxY, position.y); }
            Require(maxY - minY > 100, "irregular contacts span a three-dimensional cloud");
            for (int i = 0; i < 5; i++) for (int j = i + 1; j < 5; j++) Require(Vector3.Distance(positions[i], positions[j]) > 64.9f, "wrecks do not overlap: " + i + " / " + j);
            float minSize = float.MaxValue, maxSize = 0; foreach (var size in sizes) { minSize = Mathf.Min(minSize, size.magnitude); maxSize = Mathf.Max(maxSize, size.magnitude); }
            Require(maxSize > minSize * 1.5f, "small and large fragments have materially different dimensions");

            // Explicit fixture: later salvage tests need dock refuelling, independently of the water quest.
            scenario.WaterTank.Set(80); ops.State.power.storedKwh = 20;
            var scanned = SaveGameStore.Capture(scenario, "Trümmer / Scan", "Isolierter Abnahmestand mit 80 L Testwasser");
            float initialScrap = ops.State.inventory.scrap.amount, initialParts = ops.State.inventory.parts.amount;
            var order = StartJob("debris-0");
            var legacy = SaveGameStore.Capture(scenario, "Trümmer / alter Flugweg", "Explizite alte Pickup-Koordinaten als Migrationsfixture");
            var oldJob = legacy.stationCampaign.orders.Find(o => o.id == order.id);
            Vector3 oldOrigin = ops.Layout.transform.TransformPoint(RingWalkthrough.At(100, 12, 9));
            oldJob.flightPath[oldJob.flightPath.Length - 1] = oldOrigin + ops.Layout.transform.up * 5;
            var oldPath = (Vector3[])oldJob.flightPath.Clone();
            SaveGameStore.Validate(legacy, scenario); SaveGameStore.Apply(Copy(legacy), scenario);
            SamePath(ops.State.orders.Find(o => o.id == order.id).flightPath, oldPath, "legacy active salvage path retained");
            Require(Vector3.Distance(ops.DebrisOrigin("debris-0"), oldOrigin) < .001f, "legacy pickup remains at its saved old position");
            Require(ops.State.inventory.scrap.amount == initialScrap && ops.State.inventory.parts.amount == initialParts, "legacy in-flight load grants no resources");
            SaveGameStore.Apply(Copy(scanned), scenario); scenario.Scanner.DismissDialogue();
            Require(Vector3.Distance(ops.DebrisOrigin("debris-0"), positions[0]) < .001f, "no saved flight restores deterministic new position");

            for (int i = 0; i < 5; i++)
            {
                string id = "debris-" + i; WaitReady(); order = StartJob(id);
                Require(order.flightPath.Length >= 5, "salvage has actual navigation waypoints: " + id);
                Require(Vector3.Distance(order.flightPath[order.flightPath.Length - 1], ops.DebrisOrigin(id) + ops.Layout.transform.up * 5) < .001f, "navigation ends at this irregular contact: " + id);
                if (i == 0)
                {
                    scenario.Advance(280);
                    Require(!order.complete && scenario.Worker.Phase == DronePhase.StationWork, "real drone reaches pickup during first salvage");
                    Require(Vector3.Distance(ops.DebrisRoot(id).position, scenario.Worker.transform.TransformPoint(new Vector3(0, 2, 0))) < .001f, "real fragment is carried by drone, not copied");
                    Require(ops.State.inventory.scrap.amount == initialScrap && ops.State.inventory.parts.amount == initialParts, "carried but undelivered wreck grants no materials");
                    var mid = SaveGameStore.Capture(scenario, "Trümmer / unterwegs", "Echter laufender Bergungsauftrag");
                    var waypoints = (Vector3[])order.flightPath.Clone(); float elapsed = order.elapsed; Vector3 dronePosition = scenario.Worker.transform.position;
                    Vector3 cargoPosition = ops.DebrisRoot(id).position;
                    string path = SaveGameStore.Write(mid, directory: Path.GetFullPath(Folder + "/Saves"));
                    scenario.Advance(210);
                    Require(Vector3.Distance(ops.DebrisRoot(id).position, cargoPosition) > 1, "carried wreck genuinely moved before loading older paused save");
                    var loaded = SaveGameStore.Read(path); SaveGameStore.Apply(loaded, scenario); scenario.Scanner.DismissDialogue(); order = ops.State.orders.Find(o => o.id == order.id);
                    SamePath(order.flightPath, waypoints, "mid-flight disk save retains exact route");
                    Require(Mathf.Abs(order.elapsed - elapsed) < .001f && Vector3.Distance(scenario.Worker.transform.position, dronePosition) < .001f, "mid-flight disk load retains work progress and drone pose");
                    Require(Vector3.Distance(ops.DebrisRoot(id).position, cargoPosition) < .001f, "paused disk load immediately restores carried body as well as drone");
                }
                for (int step = 0; step < 200 && !order.complete; step++) scenario.Advance(5);
                Require(order.complete && !order.cancelled, "actual salvage completes: " + id + " / " + order.status);
                Require(ops.State.debrisBatches == i + 1 && ops.State.collected.Contains(id), "finite contact consumed exactly once: " + id);
                Require(Mathf.Abs(ops.State.inventory.scrap.amount - initialScrap - (i + 1) * 12) < .001f && Mathf.Abs(ops.State.inventory.parts.amount - initialParts - (i + 1) * 2) < .001f, "unchanged finite 12 kg and 2 parts output: " + id);
                Require(!ops.DebrisRoot(id).gameObject.activeInHierarchy && !ops.SelectDebrisContact(id) && !ops.Enqueue(StationJobKind.Salvage, id), "collected wreck hidden and duplicate job rejected: " + id);
                Require(ops.Element(id).use.Contains("12 kg") && ops.Element(id).use.Contains("Untersuchung"), "retrieval reveals usable yield while material analysis remains explicit: " + id);
                Require(ops.KnownDebrisCount == 4 - i, "remaining contacts count decreases: " + id);
            }
            Require(!ops.Enqueue(StationJobKind.Salvage), "five-contact field cannot regenerate unlimited salvage");
            var final = SaveGameStore.Capture(scenario, "Trümmer / vollständig geborgen", "Endliche Ausbeute");
            SaveGameStore.Apply(Copy(final), scenario); scenario.Advance(1);
            Require(ops.State.debrisBatches == 5 && ops.KnownDebrisCount == 0 && ops.State.inventory.scrap.amount == initialScrap + 60 && ops.State.inventory.parts.amount == initialParts + 10, "completed-save reload grants no duplicate materials or contacts");
        }

        private StationWorkOrder StartJob(string id)
        {
            Require(ops.Enqueue(StationJobKind.Salvage, id), "enqueue actual salvage: " + id);
            var order = ops.State.orders[ops.State.orders.Count - 1]; scenario.Advance(1);
            Require(order.started && !order.cancelled && scenario.Worker.Phase == DronePhase.StationWork, "actual worker starts salvage: " + id + " / " + order.status);
            return order;
        }
        private void WaitReady()
        {
            for (int step = 0; step < 1000 && !scenario.Worker.IsReady; step++) scenario.Advance(5);
            Require(scenario.Worker.IsReady, "actual dock services drone before next trip");
        }
        private bool Pick(Transform root, string id)
        {
            Physics.SyncTransforms();
            var directions = new[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
            foreach (var collider in root.GetComponentsInChildren<Collider>()) foreach (var direction in directions)
            {
                var bounds = collider.bounds; float length = bounds.extents.magnitude + 2;
                if (Physics.Raycast(bounds.center + direction * length, -direction, out var hit, length * 2, 1 << 25, QueryTriggerInteraction.Ignore)
                    && hit.collider.transform.IsChildOf(root) && ops.ElementAtHit(hit) == id) return true;
            }
            return false;
        }
        private void Render(string name, Vector3 eye, Vector3 target)
        {
            var camera = new GameObject("Wreck acceptance render").AddComponent<Camera>(); camera.CopyFrom(Camera.main); camera.enabled = false;
            camera.transform.position = eye; camera.transform.LookAt(target); camera.fieldOfView = 55; camera.nearClipPlane = .04f; camera.farClipPlane = 20000;
            var output = new RenderTexture(1200, 900, 24); var active = RenderTexture.active; camera.targetTexture = output;
            ops.RefreshViewLighting(camera, false);
            try
            {
                camera.Render(); RenderTexture.active = output; var image = new Texture2D(1200, 900, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1200, 900), 0, 0); image.Apply(); File.WriteAllBytes(Folder + "/" + name + ".png", image.EncodeToPNG()); Destroy(image);
            }
            finally { RenderTexture.active = active; camera.targetTexture = null; output.Release(); Destroy(output); Destroy(camera.gameObject); ops.RefreshViewLighting(Camera.main, StationInteriorMode.IsInside); }
        }
    }
}
#endif
