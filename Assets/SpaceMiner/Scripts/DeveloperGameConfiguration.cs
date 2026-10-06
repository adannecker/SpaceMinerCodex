using UnityEngine;
namespace SpaceMiner
{
    // Balancing is deliberately separate from player preferences and never persisted to their file.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public sealed class DeveloperGameConfiguration
    {
        public float SimulationRate;
        public float MiningRate;
        public float MiningPower, OnboardPower, Reserve, BatteryCapacity, FuelCapacity, CargoCapacity, CruiseSpeed, Thrust, HeatingEnergy;
        public int TripsPerLevel;
        public float LevelGain;
        public DeveloperGameConfiguration Copy() => (DeveloperGameConfiguration)MemberwiseClone();
        public static DeveloperGameConfiguration Capture(WaterScenario scenario) => new DeveloperGameConfiguration
        {
            SimulationRate = scenario.SimulationRate, MiningRate = scenario.Worker.MiningRateKgPerSecond,
            MiningPower = scenario.Worker.MiningPowerKw, OnboardPower = scenario.Worker.OnboardPowerKw,
            Reserve = scenario.Worker.ReserveFraction, BatteryCapacity = scenario.Worker.BatteryCapacityKwh,
            FuelCapacity = scenario.Worker.FuelCapacityLiters, CargoCapacity = scenario.Worker.CargoCapacityKg,
            CruiseSpeed = scenario.Worker.CruiseSpeed, Thrust = scenario.Worker.ThrustNewtons,
            HeatingEnergy = scenario.Worker.HeatingKwhPerLiter, TripsPerLevel = scenario.Mining.TripsPerLevel,
            LevelGain = scenario.Mining.ImprovementPerLevel
        };
        public void Apply(WaterScenario scenario)
        {
            scenario.SetSimulationRate(Mathf.Clamp(SimulationRate, 0, 1000));
            scenario.Worker.MiningRateKgPerSecond = Mathf.Clamp(MiningRate, .01f, 10);
            // Physical capacities/flight parameters change only while idle, preserving a live sortie's budget.
            if (scenario.Worker.IsReady) {
                bool capacitiesChanged = !Mathf.Approximately(scenario.Worker.BatteryCapacityKwh, BatteryCapacity)
                    || !Mathf.Approximately(scenario.Worker.FuelCapacityLiters, FuelCapacity)
                    || !Mathf.Approximately(scenario.Worker.CargoCapacityKg, CargoCapacity);
                scenario.Worker.MiningPowerKw = Mathf.Clamp(MiningPower, 1, 150);
                scenario.Worker.OnboardPowerKw = Mathf.Clamp(OnboardPower, 0, 1);
                scenario.Worker.ReserveFraction = Mathf.Clamp(Reserve, 0, .5f);
                scenario.Worker.BatteryCapacityKwh = Mathf.Clamp(BatteryCapacity, 1, 30);
                scenario.Worker.FuelCapacityLiters = Mathf.Clamp(FuelCapacity, 2, 30);
                scenario.Worker.CargoCapacityKg = Mathf.Clamp(CargoCapacity, 5, 150);
                scenario.Worker.CruiseSpeed = Mathf.Clamp(CruiseSpeed, .5f, 10);
                scenario.Worker.ThrustNewtons = Mathf.Clamp(Thrust, 1, 50);
                scenario.Worker.HeatingKwhPerLiter = Mathf.Clamp(HeatingEnergy, .15f, 2);
                if (capacitiesChanged) scenario.Worker.ResetDrone();
            }
            scenario.Mining.TripsPerLevel = Mathf.Clamp(TripsPerLevel, 1, 100);
            scenario.Mining.ImprovementPerLevel = Mathf.Clamp(LevelGain, 0, .5f);
        }
    }
#endif
}
