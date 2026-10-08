using System;
using UnityEngine;

namespace SpaceMiner
{
    public enum MiningFocus { Throughput, Efficiency }

    // Material knowledge is separate from the drone's hardware generation and player preferences.
    [Serializable]
    public sealed class MiningResearch
    {
        public int TripsPerLevel = 20;
        public float ImprovementPerLevel = .15f;
        public MiningFocus Focus;
        public int CompletedTrips { get; private set; }
        public int ThroughputLevels { get; private set; }
        public int EfficiencyLevels { get; private set; }
        public int Level => 1 + ThroughputLevels + EfficiencyLevels;
        public int TripsIntoLevel { get; private set; }
        public float Progress => TripsIntoLevel / (float)Mathf.Max(1, TripsPerLevel);
        public float RateMultiplier => Mathf.Pow(1f + Mathf.Clamp(ImprovementPerLevel, 0, 1), ThroughputLevels);
        public float PowerMultiplier => Mathf.Pow(1f - Mathf.Clamp(ImprovementPerLevel, 0, .9f), EfficiencyLevels);
        public void CompleteTrip()
        {
            CompletedTrips++;
            TripsIntoLevel++;
            while (TripsIntoLevel >= Mathf.Max(1, TripsPerLevel))
            {
                TripsIntoLevel -= Mathf.Max(1, TripsPerLevel);
                if (Focus == MiningFocus.Throughput) ThroughputLevels++; else EfficiencyLevels++;
            }
        }
        public void Reset()
        {
            CompletedTrips = TripsIntoLevel = ThroughputLevels = EfficiencyLevels = 0;
        }
        public void Restore(int trips, int throughput, int efficiency, int progress)
        { CompletedTrips = trips; ThroughputLevels = throughput; EfficiencyLevels = efficiency; TripsIntoLevel = progress; }
    }
}
