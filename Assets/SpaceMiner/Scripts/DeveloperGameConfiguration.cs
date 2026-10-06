using UnityEngine;
namespace SpaceMiner
{
    // Balancing is deliberately separate from player preferences and never persisted to their file.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public sealed class DeveloperGameConfiguration
    {
        public float SimulationRate;
        public float MiningRate;
        public DeveloperGameConfiguration Copy() => new DeveloperGameConfiguration { SimulationRate = SimulationRate, MiningRate = MiningRate };
        public static DeveloperGameConfiguration Capture(WaterScenario scenario) => new DeveloperGameConfiguration
        { SimulationRate = scenario.SimulationRate, MiningRate = scenario.Worker.MiningRateKgPerSecond };
        public void Apply(WaterScenario scenario)
        {
            scenario.SimulationRate = Mathf.Clamp(SimulationRate, 0, 1000);
            scenario.Worker.MiningRateKgPerSecond = Mathf.Clamp(MiningRate, .01f, 10);
        }
    }
#endif
}
