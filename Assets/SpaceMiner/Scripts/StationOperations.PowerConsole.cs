using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    public sealed partial class StationOperations
    {
        private readonly PowerConsoleView powerView = new PowerConsoleView();
        private string powerSelection => powerView.Selection;
        private string powerModelSelection;
        private Rect powerDashboardBounds;
        private StationPowerModelView powerStationView, powerItemView;
        private readonly Dictionary<string, Rect> powerHitAreas = new Dictionary<string, Rect>();
        private GUIStyle powerSmall => powerView.Small;
        public bool PowerPanelOpen => Live && panel == StationTerminalKind.Power;
        public Rect PowerDashboardBounds => PowerPanelOpen ? powerDashboardBounds : default;
        public int PowerDashboardLastRenderedFrame => powerView.LastRenderedFrame;
        public string PowerSelection => powerSelection;
        internal int PowerPreviewMeshCount => powerStationView?.SharedMeshCount ?? 0;
        internal int PowerDetailMeshCount => powerItemView?.SharedMeshCount ?? 0;
        internal IReadOnlyDictionary<string, Rect> PowerHitAreas => powerHitAreas;
        internal void ScrollPowerDashboard(Vector2 p) => powerView.Scroll(p);
        public bool OpenPowerConsole()
        {
            if (!Live || !StationInteriorMode.IsInside || ForeignModal || Scenario.Scanner.Dialogue != null) return false;
            var terminal = NearestTerminal(StationInteriorMode.Current.FeetPosition);
            if (terminal == null || terminal.kind != StationTerminalKind.Power) return false;
            RefreshSupplyState(); OpenTerminal(terminal); return PowerPanelOpen;
        }
        internal bool SelectPowerItem(string id) { BindPowerView(); return powerView.SelectPowerItem(id); }
        private void DisposePowerModels(bool releaseView = false)
        {
            if (releaseView) powerView.Dispose();
            powerStationView?.Dispose(); powerItemView?.Dispose();
            powerStationView = powerItemView = null; powerModelSelection = null; powerHitAreas.Clear();
        }
        private IEnumerable<Transform> ReactorModelRoots()
        {
            var old = transform.Find("Station Geometry");
            if (old != null) foreach (Transform child in old)
                if (child.name == "Shielded Reactor" || child.name.StartsWith("Reactor", StringComparison.Ordinal)) yield return child;
            if (Layout.Geometry != null) foreach (var child in Layout.Geometry.Root.GetComponentsInChildren<Transform>())
                if (child.name == "Reaktorkapsel / zweilagige Wabenabdeckung") yield return child;
        }
        private IEnumerable<Transform> PowerStationModelRoots()
        {
            // The station's actual architecture and installed equipment, not nearby salvage contacts.
            yield return Layout.Geometry.Root;
            foreach (Transform child in Layout.transform)
                if (child.name.StartsWith("Solar", StringComparison.Ordinal) || child.name.StartsWith("Campaign water storage", StringComparison.Ordinal)) yield return child;
            foreach (var bay in batteryBays) yield return bay.transform;
            foreach (var root in ReactorModelRoots()) yield return root;
        }
        private void PreparePowerModels()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (powerStationView == null) powerStationView = new StationPowerModelView(Layout.transform, PowerStationModelRoots(), true);
            if (powerModelSelection == powerSelection) return;
            powerItemView?.Dispose(); powerItemView = null; powerModelSelection = powerSelection;
            if (powerSelection.StartsWith("battery-bay-", StringComparison.Ordinal))
            {
                int index = int.Parse(powerSelection.Substring(12));
                if (index < batteryBays.Count) powerItemView = new StationPowerModelView(batteryBays[index].transform,
                    new[] { batteryBays[index].transform }, false);
            }
            else if (powerSelection == "reactor") powerItemView = new StationPowerModelView(Layout.transform, ReactorModelRoots(), false);
            else if (powerSelection == "solar-a" || powerSelection == "solar-b")
            {
                var root = powerSelection == "solar-a" ? SolarPanelA : SolarPanelB;
                if (root != null) powerItemView = new StationPowerModelView(root, new[] { root }, false);
            }
        }
        private void MapItem(Rect map, string id, Vector3 world, string caption, Color color, float offsetY = 0, float offsetX = 0)
        {
            if (powerStationView?.Texture == null) return;
            var point = powerStationView.Project(world, map);
            float width = id.StartsWith("battery", StringComparison.Ordinal) ? 42 : 100;
            var r = new Rect(Mathf.Clamp(point.x - width / 2 + offsetX, map.x + 3, map.xMax - width - 3),
                Mathf.Clamp(point.y - 14 + offsetY, map.y + 4, map.yMax - 32), width, 28);
            if (id == "solar-a") { r.x = map.xMax - width - 8; r.y = map.center.y - 14; }
            if (id == "solar-b") { r.x = map.x + 8; r.y = map.center.y - 14; }
            r = FreePowerCallout(r, map);
            PowerLine(point, r.center, color, 1); powerHitAreas[id] = r;
            var button = new GUIStyle(powerSelection == id ? ui.Primary : ui.Button)
            { padding = new RectOffset(2, 2, 1, 1), alignment = TextAnchor.MiddleCenter, wordWrap = false, fontSize = powerSmall.fontSize };
            while (button.fontSize > 12 && button.CalcSize(new GUIContent(caption)).x > width - 4) button.fontSize--;
            if (GUI.Button(r, new GUIContent(caption, caption), button))
            { SelectPowerItem(id); SettingsUiAudio.Activate(); }
        }
        private Rect FreePowerCallout(Rect anchor, Rect viewport)
        {
            bool Clear(Rect candidate)
            {
                foreach (var other in powerHitAreas.Values) if (candidate.Overlaps(other)) return false;
                return true;
            }
            if (Clear(anchor)) return anchor;
            // Labels may move away from their projected point, retaining a leader line.
            // This also keeps all11 targets individually reachable in short, wide windows.
            for (int radius = 1; radius <= 12; radius++) for (int direction = 0; direction < 8; direction++)
            {
                float angle = direction * Mathf.PI / 4;
                var candidate = anchor;
                candidate.x = Mathf.Clamp(anchor.x + Mathf.Cos(angle) * radius * 32, viewport.x + 3, viewport.xMax - anchor.width - 3);
                candidate.y = Mathf.Clamp(anchor.y + Mathf.Sin(angle) * radius * 32, viewport.y + 3, viewport.yMax - anchor.height - 3);
                if (Clear(candidate)) return candidate;
            }
            return anchor;
        }
        private void DrawPowerStation(Rect r)
        {
            PowerPanel(r, SpaceMinerUi.Cyan);
            GUI.Label(new Rect(r.x + 14, r.y + 8, r.width - 28, 30), "STATION / ANLAGE AUSWÄHLEN", powerSmall);
            var map = new Rect(r.x + 5, r.y + 41, r.width - 10, r.height - 49);
            PowerModel(map, powerStationView); powerHitAreas.Clear();
            for (int i = 0; i < batteryBays.Count; i++)
            {
                var bay = batteryBays[i]; var data = CaptureBatteryBay(i);
                var local = Layout.transform.InverseTransformPoint(bay.transform.position);
                float offsetX = bay.Axis % 2 == 0 ? Mathf.Sign(local.x) * 26 : bay.Axis == 1 ? 26 : -26;
                MapItem(map, "battery-bay-" + i, bay.transform.TransformPoint(new Vector3(0, 2, 0)),
                    (i + 1).ToString("00"), data.Coupled ? SpaceMinerUi.Amber : data.Installed ? SpaceMinerUi.Cyan : SpaceMinerUi.Disabled,
                    bay.Axis % 2 == 0 ? (bay.Axis == 0 ? -16 : 16) : -Mathf.Sign(local.z) * 22, offsetX);
            }
            MapItem(map, "reactor", Layout.transform.TransformPoint(Vector3.up * 5), "REAKTOR", SpaceMinerUi.Cyan);
            if (SolarPanelA != null) MapItem(map, "solar-a", SolarPanelA.position, "SOLAR A", SpaceMinerUi.Cyan);
            if (SolarPanelB != null) MapItem(map, "solar-b", SolarPanelB.position, "SOLAR B", SpaceMinerUi.Cyan);
        }
        private static void PowerLine(Vector2 a, Vector2 b, Color color, float thickness = 2)
            => PowerConsoleView.PowerLine(a, b, color, thickness);
        private static void PowerModel(Rect r, StationPowerModelView view) => PowerConsoleView.PowerModel(r, view);
        private static void PowerPanel(Rect r, Color accent)
        {
            UiChrome.Panel(r, accent, SettingsStore.Current.Accessibility.HighContrast);
        }
        private void BindPowerView()
        {
            powerView.SetData(new PowerConsoleData {
                Generation = CaptureGeneration(), History = PowerHistory, Consumers = powerConsumers,
                Battery = CaptureBatteryBay, ConsumptionKw = State.power.ConsumptionKw,
                HasStorageFlow=true,StorageFlowKw=State.power.StorageFlowKw,CurtailedKw=State.power.CurtailedKw,
                LifePowered = State.power.LifePowered, Accessibility = SettingsStore.Current.Accessibility,
                StatusLine = SettingsStore.Current.Gameplay.PauseInMenu
                    ? "Menüpause · Verlauf und Simulation ruhen bis zum Schließen"
                    : "LIVE / Anlagenstatus und tatsächliche Stromzuteilung",
                Close = ClosePanel,
                OpenUtilities = () => { panel = StationTerminalKind.Utilities; panelScroll = Vector2.zero; DisposePowerModels(); },
                PrepareModels = _ => PreparePowerModels(), DrawStation = DrawPowerStation,
                DetailModel = () => powerItemView
            });
        }
        private void DrawPowerConsole(float w, float h)
        {
            powerDashboardBounds = new Rect((UiLayout.Width - w) / 2, (UiLayout.Height - h) / 2, w, h);
            BindPowerView(); powerView.Draw(w, h);
        }
    }
}
