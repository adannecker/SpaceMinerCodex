using UnityEngine;

namespace SpaceMiner
{
    public sealed partial class StationOperations
    {
        private double RawIceOnStationKg => IcePlant == null ? 0 : IcePlant.IntakeKg + IcePlant.CrushedKg + IcePlant.ColdRawKg;
        private void DrawIcePlantSummary()
        {
            var plant = IcePlant;
            if (plant == null) return;
            GUILayout.Label("Kaltlager " + plant.ColdRawKg.ToString("0.0") + " kg roh / "
                + plant.ColdVolumeM3.ToString("0.00") + " m³\nVerbrauchswasser "
                + Scenario.WaterLiters.ToString("0.0") + " / " + Scenario.TankCapacityLiters.ToString("0")
                + " L\n" + plant.Status, ui.Small);
        }

        private void DrawIcePlantPanel()
        {
            var plant = IcePlant;
            if (plant == null) return;
            GUILayout.Label("EISANNAHME / KALTLAGER 01", ui.Text);
            GUILayout.Label("Geschlossene Übergabe → Brechen → Verdichten → Kaltzellen → Wasseraufbereitung", ui.Small);
            DrawIcePlantSummary();
            GUILayout.Label("Annahme " + plant.IntakeKg.ToString("0.0") + " / "
                + StationIcePlant.IntakeCapacityKg.ToString("0") + " kg\nZerkleinert "
                + plant.CrushedKg.ToString("0.0") + " / " + StationIcePlant.CrushedCapacityKg.ToString("0")
                + " kg\nKaltraum " + plant.ColdVolumeM3.ToString("0.00") + " / "
                + StationIcePlant.ColdCapacityM3.ToString("0.00") + " m³", ui.Small);
            double potentialWater = 0;
            for (int index = 0; index < 3; index++)
            {
                foreach (var batch in plant.State.cells[index].batches) potentialWater += batch.waterKg;
                GUILayout.Label("Zelle " + (index + 1).ToString("00") + " · "
                    + plant.GetCellRawKg(index).ToString("0.0") + " kg · "
                    + plant.GetCellVolumeM3(index).ToString("0.00") + " / "
                    + StationIcePlant.CellCapacityM3(index).ToString("0.00") + " m³", ui.Small);
            }
            GUILayout.Label("Wasserpotenzial im Kaltlager: " + potentialWater.ToString("0.0")
                + " L; noch kein sauberes Wasser.\nUnbekannter Rückstand: "
                + plant.ResidueKg.ToString("0.00") + " / "
                + StationIcePlant.ResidueCapacityKg.ToString("0") + " kg", ui.Small);
            GUILayout.Label("Anlage aktuell " + plant.ActualPowerKw.ToString("0.00")
                + " kW · Prozessenergie " + plant.EnergyKwh.ToString("0.00")
                + " kWh\nWasseraufbereiter: 1,5 kW · 0,20 kWh/L · bis 7,5 L pro Spielstunde\n"
                + "Nur der Bedarf des Flüssigwasserpuffers wird aufbereitet.", ui.Small);
            if (GUILayout.Button(plant.ReceivingEnabled ? "Rohannahme / Kaltmaschinen pausieren" : "Rohannahme / Kaltmaschinen einschalten", ui.Button))
                plant.ReceivingEnabled = !plant.ReceivingEnabled;
            if (GUILayout.Button(plant.ProcessingEnabled ? "Wasseraufbereiter pausieren" : "Wasseraufbereiter einschalten", ui.Button))
                plant.ProcessingEnabled = !plant.ProcessingEnabled;
            GUILayout.Label("Unbekannte Rückstände bleiben im lokalen Behälter erhalten. Ihre spätere Analyse und der Abtransport gehören zur internen Drohnenlogistik.", ui.Small);
            GUILayout.Label("Kein freies Wegwerfen: Bei vollem Rückstandsbehälter, vollem Wassertank oder abgesperrter Leitung pausiert die betreffende Stufe. Die geschlossene Annahme und die drei Kaltzellen halten Material zurück.", ui.Small);
        }
    }
}
