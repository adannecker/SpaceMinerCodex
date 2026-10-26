using UnityEngine;

namespace SpaceMiner
{
    // A view of the existing energy reservoir, never another saved inventory.
    // A cassette is a cell module; individual electrochemical cells are not simulated.
    public sealed class StationBatteryBayView
    {
        public int Index { get; }
        public int BankNumber { get; }
        public int ModuleSlots => 4;
        public int InstalledModules => Installed ? ModuleSlots : 0;
        public int ActiveModules => Coupled ? InstalledModules : 0;
        public bool Installed => Index == StationBatteryPlacement.ActiveIndex || Index == StationBatteryPlacement.ReserveIndex;
        public bool Coupled => Index == StationBatteryPlacement.ActiveIndex;
        public bool ChargeKnown => Coupled;
        public float ChargeFraction { get; }
        public float StoredKwh { get; }
        public float CapacityKwh { get; }
        public float Condition { get; }
        public string Caption { get; }
        internal StationBatteryBayView(int index, float stored, float capacity, float condition, bool generating)
        {
            Index = index; BankNumber = index == StationBatteryPlacement.ActiveIndex ? 1 : index == StationBatteryPlacement.ReserveIndex ? 2 : 0;
            if (ChargeKnown)
            {
                CapacityKwh = Mathf.Max(0, capacity); StoredKwh = Mathf.Clamp(stored, 0, CapacityKwh);
                ChargeFraction = CapacityKwh > 0 ? StoredKwh / CapacityKwh : 0;
                Condition = Mathf.Clamp01(condition);
            }
            Caption = !Installed ? "LEER / RESERVEPLATZ" : !Coupled ? "02 AUS / —"
                : "01 " + (StoredKwh > .001f || generating ? "AKTIV " : "LEER ")
                    + Mathf.RoundToInt(ChargeFraction * 100) + "%";
        }
    }
}
