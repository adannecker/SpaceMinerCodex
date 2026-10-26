using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    public sealed partial class StationOperations
    {
        private readonly List<StationBatteryBay> batteryBays = new List<StationBatteryBay>();
        public IReadOnlyList<StationBatteryBay> BatteryBays => batteryBays;
        public StationBatteryBayView CaptureBatteryBay(int index)
            => index < 0 || index >= 8 ? null : new StationBatteryBayView(index, State.power.storedKwh,
                State.power.capacityKwh, Element("battery")?.condition ?? 0, State.power.GenerationKw > .001f);
        private void BuildBatteryBays()
        {
            var anchors = Layout.Geometry.BatteryBayAnchors;
            if (anchors.Count != 8) throw new InvalidOperationException("Station requires eight sealed battery bay anchors.");
            for (int i = 0; i < anchors.Count; i++)
            {
                var root = Group("Akkubucht / " + i, anchors[i], Vector3.zero, Quaternion.identity);
                var bay = root.gameObject.AddComponent<StationBatteryBay>();
                bay.Initialize(this, i, signageFont, signageMaterial, graphite, i == StationBatteryPlacement.ReserveIndex ? amber : cyan);
                batteryBays.Add(bay); Selectable(Layout.Geometry.BatteryBayFrames[i], bay.SelectableId);
                // The real diffuser uses the same powered, distance-limited
                // eight-light budget as the rest of the station interior.
                LightAt(Layout.transform.InverseTransformPoint(root.TransformPoint(new Vector3(0, 2.84f, .72f))),
                    new Color(.78f, .88f, 1), 1.5f, 5.3f);
            }
        }
        public StationBatteryBay BatteryBay(string id)
        {
            if (id == null || !id.StartsWith("battery-bay-", StringComparison.Ordinal)
                || !int.TryParse(id.Substring(12), out int index) || index < 0 || index >= batteryBays.Count) return null;
            return batteryBays[index];
        }
        private bool DrawBatteryBayDetails(string id)
        {
            var bay = BatteryBay(id); if (bay == null) return false;
            GUILayout.Label("AKKUBUCHT / " + bay.Caption, ui.Text);
            GUILayout.Label("Reaktorachse " + (bay.Axis * 90) + "° · " + (bay.Index % 2 == 0 ? "links" : "rechts")
                + " vom Ring aus gesehen\nEigene Wartungsfront / geschlossene Doppelhülle", ui.Small);
            if (!bay.Installed) GUILayout.Label("Freier Einbauplatz. Keine Batterie und keine zusätzliche Speicherkapazität installiert.", ui.Small);
            else if (bay.Index == StationBatteryPlacement.ReserveIndex) GUILayout.Label("Akku 02 ist ausgeschaltet und elektrisch getrennt. Eine spätere Inbetriebnahme braucht einen eigenen Ausbauauftrag.", ui.Small);
            else
            {
                GUILayout.Label("Stationsakku 01 / 4 Zellmodule zugeschaltet\nEnergie " + State.power.storedKwh.ToString("0.0") + " / "
                    + State.power.capacityKwh.ToString("0.0") + " kWh\nZustand " + (Element("battery").condition * 100).ToString("0.0")
                    + " %\nNetzanschluss: " + (bay.Coupled ? "verbunden" : "getrennt"), ui.Small);
                ActionButton(StationJobKind.Repair, "battery");
            }
            return true;
        }
    }
}
