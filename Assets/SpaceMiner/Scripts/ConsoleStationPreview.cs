using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpaceMiner
{
    // Builds the game's geometry through its own builders; never installs campaign behaviours.
    internal sealed class ConsoleStationPreview : IDisposable
    {
        private readonly GameObject root;
        private readonly StationInteriorLayout layout;
        private readonly Dictionary<string, Transform> equipment = new Dictionary<string, Transform>();
        private readonly List<Transform> stationParts = new List<Transform>();
        public StationPowerModelView Station { get; }
        public StationPowerModelView Detail { get; private set; }
        private string selection;
        public Transform SolarA => equipment["solar-a"];
        public Transform SolarB => equipment["solar-b"];
        public ConsoleStationPreview()
        {
            root = new GameObject("Studio / genuine start-station geometry");
            root.transform.position = new Vector3(0, -12000, 0);
            root.AddComponent<SpaceObject>();
            var station = root.AddComponent<StationVisual>();
            layout = root.AddComponent<StationInteriorLayout>();
            layout.BuildModelPreview(station);
            stationParts.Add(layout.Geometry.Root);
            foreach (Transform child in root.transform)
                if (child.name.StartsWith("Solar", StringComparison.Ordinal) || child.name.StartsWith("Campaign water storage", StringComparison.Ordinal))
                    stationParts.Add(child);
            equipment["solar-a"] = root.transform.Find("Solar Wing Right");
            equipment["solar-b"] = root.transform.Find("Solar Wing Left");
            // Existing reactor blockout and its shield; a future authored model replaces these same roots.
            var reactor = new GameObject("Studio / reactor assembly").transform;
            reactor.SetParent(root.transform, false);
            var old = root.transform.Find("Station Geometry");
            var reactorParts = new List<Transform>();
            foreach (Transform child in old)
                if (child.name == "Shielded Reactor" || child.name.StartsWith("Reactor", StringComparison.Ordinal)) reactorParts.Add(child);
            foreach (var child in reactorParts) child.SetParent(reactor, true);
            foreach (var child in layout.Geometry.Root.GetComponentsInChildren<Transform>())
                if (child.name == "Reaktorkapsel / zweilagige Wabenabdeckung") { reactorParts.Add(child); break; }
            // Keep the ring-owned shield in place; include it explicitly in the detail view.
            equipment["reactor"] = reactor; stationParts.Add(reactor);
            reactorDetailRoots = reactorParts;
            for (int i = 0; i < layout.Geometry.BatteryBayAnchors.Count; i++)
            {
                var anchor = layout.Geometry.BatteryBayAnchors[i];
                var bay = Object.Instantiate(Resources.Load<GameObject>("BatteryBay/BatteryBay_LOD0"), anchor, false);
                bay.name = "Studio / battery bay " + i;
                foreach (var t in bay.GetComponentsInChildren<Transform>(true))
                    if (!StationBatteryPlacement.Installed(i) && t.name == "Rack") t.gameObject.SetActive(false);
                equipment["battery-bay-" + i] = bay.transform;
            }
            foreach (var light in root.GetComponentsInChildren<Light>()) light.enabled = false;
            foreach (var collider in root.GetComponentsInChildren<Collider>()) collider.enabled = false;
            Station = new StationPowerModelView(root.transform, stationParts, true);
        }
        private readonly List<Transform> reactorDetailRoots;
        public void Select(string id)
        {
            if (selection == id) return;
            selection = id; Detail?.Dispose(); Detail = null;
            if (equipment.TryGetValue(id, out var item))
                Detail = new StationPowerModelView(id == "reactor" ? root.transform : item,
                    id == "reactor" ? reactorDetailRoots : new[] { item }, false);
        }
        public Vector3 Position(string id) => equipment.TryGetValue(id, out var item) ? item.position : root.transform.position;
        public void Dispose()
        {
            Detail?.Dispose(); Station?.Dispose(); Object.Destroy(root);
        }
    }
}
