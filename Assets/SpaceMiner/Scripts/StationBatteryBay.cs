using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceMiner
{
    public enum BatteryBayVisualState { Empty, Active, Isolated, Discharged }

    // Physical presentation of the existing shared station accumulator. An
    // installed reserve does not own another reservoir or create free energy.
    public sealed class StationBatteryBay : MonoBehaviour
    {
        public int Index { get; private set; }
        public int Axis => Index / 2;
        public bool Installed => Index == StationBatteryPlacement.ActiveIndex || Index == StationBatteryPlacement.ReserveIndex;
        public bool Coupled => Index == StationBatteryPlacement.ActiveIndex && owner != null;
        public BatteryBayVisualState VisualState => !Installed ? BatteryBayVisualState.Empty
            : Index == StationBatteryPlacement.ReserveIndex ? BatteryBayVisualState.Isolated
            : owner != null && (owner.State.power.storedKwh > .001f || owner.State.power.GenerationKw > .001f)
                ? BatteryBayVisualState.Active : BatteryBayVisualState.Discharged;
        public string SelectableId => "battery-bay-" + Index;
        public Transform InstanceRoot => transform;
        public Bounds ModelBounds { get; private set; } // instance-local metres, highest LOD
        public int HighDetailTriangles { get; private set; }
        public int DetailRendererCount { get; private set; }
        public LODGroup DetailLods { get; private set; }
        public string Caption => owner != null ? owner.CaptureBatteryBay(Index).Caption : "AKKUBUCHT";
        private StationOperations owner;
        private readonly List<Renderer> indicators = new List<Renderer>();
        private MaterialPropertyBlock properties;
        private StationDisplay label;
        private float refreshAt;
        private BatteryBayVisualState shownState;
        private int shownCharge = -2;

        internal void Initialize(StationOperations operations, int index, Font font, Material letters,
            Material panel, Material trim)
        {
            owner = operations; Index = index; gameObject.layer = 25;
            properties = new MaterialPropertyBlock();
            gameObject.AddComponent<StationSelectable>().elementId = SelectableId;
            var surface = Resources.Load<Material>("BatteryBay/Materials/BatteryBay_Surface");
            var display = Resources.Load<Material>("BatteryBay/Materials/BatteryBay_Display");
            var signal = Resources.Load<Material>("BatteryBay/Materials/BatteryBay_Status");
            if (surface == null || display == null || signal == null)
                throw new InvalidOperationException("Battery bay PBR materials missing; run tools/CreateBatteryBay.py.");
            display.EnableKeyword("_EMISSION"); signal.EnableKeyword("_EMISSION");
            var levels = new LOD[3];
            for (int level = 0; level < 3; level++)
            {
                var prefab = Resources.Load<GameObject>("BatteryBay/BatteryBay_LOD" + level);
                if (prefab == null) throw new InvalidOperationException("Battery bay LOD missing: " + level);
                var instance = Instantiate(prefab, transform, false); instance.name = "Battery bay / LOD " + level;
                foreach (var node in instance.GetComponentsInChildren<Transform>(true))
                {
                    node.gameObject.layer = 25;
                    if (!Installed && node.name == "Rack") node.gameObject.SetActive(false);
                }
                var renderers = instance.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    var mats = renderer.sharedMaterials;
                    for (int m = 0; m < mats.Length; m++)
                    {
                        string name = mats[m] != null ? mats[m].name : "";
                        mats[m] = name.Contains("Display") ? display : name.Contains("Status") ? signal : surface;
                    }
                    renderer.sharedMaterials = mats;
                    renderer.shadowCastingMode = level == 0 ? ShadowCastingMode.On : ShadowCastingMode.Off;
                    renderer.receiveShadows = true;
                    if (renderer.name.Contains("ActiveDisplay") || renderer.name.Contains("StatusLenses")) indicators.Add(renderer);
                    if (level != 0) continue;
                    DetailRendererCount++;
                    var filter = renderer.GetComponent<MeshFilter>();
                    if (filter != null && filter.sharedMesh != null) HighDetailTriangles += filter.sharedMesh.triangles.Length / 3;
                }
                levels[level] = new LOD(level == 0 ? .22f : level == 1 ? .07f : .004f, renderers);
                if (level == 0) MeasureModel(instance.transform);
            }
            DetailLods = gameObject.AddComponent<LODGroup>();
            DetailLods.fadeMode = LODFadeMode.None; DetailLods.SetLODs(levels); DetailLods.RecalculateBounds();
            label = StationDisplay.Create(Caption, transform, new Vector3(0, 3.015f, -.073f),
                Quaternion.identity, .04f, font, letters, panel, trim, 25, 1.2f);
            shownState = (BatteryBayVisualState)(-1); UpdateState();
        }
        private void MeasureModel(Transform high)
        {
            bool found = false; Bounds bounds = default;
            foreach (var filter in high.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null) continue;
                var b = filter.sharedMesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 p = b.center + Vector3.Scale(b.extents, new Vector3((corner & 1) == 0 ? -1 : 1,
                        (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                    p = transform.InverseTransformPoint(filter.transform.TransformPoint(p));
                    if (!found) { bounds = new Bounds(p, Vector3.zero); found = true; } else bounds.Encapsulate(p);
                }
                // Collision follows the installed cassette envelope rather than
                // sealing the complete front aperture with an invisible box.
                if (Installed && IsRack(filter.transform, high))
                {
                    var collider = filter.gameObject.AddComponent<BoxCollider>(); collider.center = b.center; collider.size = b.size;
                }
                else if (filter.name == "Housing" || filter.name == "Frame")
                {
                    // Preserve the genuinely open service front and collide with
                    // its authored side walls, rear wall and bevelled threshold.
                    filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
                }
            }
            ModelBounds = bounds;
        }
        private static bool IsRack(Transform node, Transform stop)
        {
            for (var t = node; t != null && t != stop; t = t.parent) if (t.name == "Rack") return true;
            return false;
        }
        private void Update()
        {
            if (Time.unscaledTime < refreshAt) return;
            refreshAt = Time.unscaledTime + .25f; UpdateState();
        }
        public void UpdateState()
        {
            var state = VisualState;
            int charge = owner != null && Coupled ? Mathf.RoundToInt(owner.CaptureBatteryBay(Index).ChargeFraction * 100) : -1;
            if (state == shownState && charge == shownCharge) return;
            shownState = state;
            shownCharge = charge;
            owner?.RefreshTacticalContacts();
            Color color = state == BatteryBayVisualState.Active ? new Color(.08f, .85f, .6f)
                : state == BatteryBayVisualState.Empty ? new Color(.08f, .12f, .14f) : new Color(.7f, .29f, .035f);
            foreach (var renderer in indicators)
            {
                bool lens = renderer.name.Contains("StatusLenses");
                properties.Clear(); properties.SetColor("_Color", lens ? color : new Color(.018f, .034f, .045f));
                properties.SetColor("_EmissionColor", lens ? color * (state == BatteryBayVisualState.Active ? .8f : .2f) : Color.black);
                renderer.SetPropertyBlock(properties);
            }
            if (label != null) label.SetCaption(Caption);
        }
    }
}
