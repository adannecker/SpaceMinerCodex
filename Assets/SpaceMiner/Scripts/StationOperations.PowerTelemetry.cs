using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    public sealed partial class StationOperations
    {
        public StationPowerHistory PowerHistory { get; } = new StationPowerHistory();
        private StationCampaignState telemetryState;
        private readonly List<StationPowerConsumer> powerConsumers = new List<StationPowerConsumer>();
        public IReadOnlyList<StationPowerConsumer> PowerConsumers => powerConsumers;
        private void ObservePower(float seconds, float loss, float work, float charging)
        {
            if (telemetryState != State) { telemetryState = State; PowerHistory.Reset(); }
            PowerHistory.Observe(seconds, State.power.GenerationKw, State.power.ConsumptionKw);
            powerConsumers.Clear();
            float scanner = Scenario.Scanner.Ready ? 0 : Scenario.Scanner.ChargePowerKw;
            float basic = .53f + loss + scanner;
            // Mirror the existing priority allocation; never charge energy a second time.
            float jobs = State.power.JobPowered ? work : 0;
            float basicActual = Mathf.Max(0, State.power.ConsumptionKw - jobs - State.power.ChargeAllocationKw);
            float ratio = basic > 0 ? Mathf.Clamp01(basicActual / basic) : 0;
            powerConsumers.Add(new StationPowerConsumer("life", "Lebenserhaltung", .35f, .35f * ratio));
            powerConsumers.Add(new StationPowerConsumer("systems", "Stationssysteme", .18f, .18f * ratio));
            if (loss > 0) powerConsumers.Add(new StationPowerConsumer("loss", "Havarieverluste", loss, loss * ratio));
            powerConsumers.Add(new StationPowerConsumer("scanner", "Scannerladung", scanner, scanner * ratio));
            powerConsumers.Add(new StationPowerConsumer("ice", "Eisannahme / Wasser", preparedIcePowerKw,
                State.power.JobPowered ? preparedIcePowerKw : 0));
            foreach (var order in State.orders)
                if (order.started && !order.complete && !order.cancelled && SupplyAvailable(order))
                {
                    var recipe = StationRecipes.Get(order.kind);
                    if (recipe.PowerKw > 0) powerConsumers.Add(new StationPowerConsumer("order-" + order.id,
                        recipe.Name, recipe.PowerKw, State.power.JobPowered ? recipe.PowerKw : 0));
                }
            powerConsumers.Add(new StationPowerConsumer("charging", "Drohnen-Ladestation", charging, State.power.ChargeAllocationKw));
        }
    }
}
