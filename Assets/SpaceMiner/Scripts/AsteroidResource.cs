using UnityEngine;

namespace SpaceMiner
{
    public sealed class AsteroidResource : MonoBehaviour
    {
        [Range(0f, 1f)] public float WaterFraction;
        public bool WaterIdentified;
        public bool IsScanned;
        public bool IsStarterWaterSource;
        public float InitialRawKg = 10000f;
        private double remainingRawKg;
        public float RemainingRawKg => (float)remainingRawKg;
        public SpaceObject Info => GetComponent<SpaceObject>();
        public bool CanMineWater => IsScanned && WaterIdentified && WaterFraction > 0f && RemainingRawKg > 0.001f;
        public float KnownWaterPercent => WaterIdentified ? WaterFraction * 100f : 0f;
        public float UnknownPercent => 100f - KnownWaterPercent;

        public void ResetDeposit() { remainingRawKg = InitialRawKg; }
        public void RestoreDeposit(float amount) { remainingRawKg = amount; }

        public float Mine(float requestedKg)
        {
            float amount = Mathf.Min(Mathf.Max(0f, requestedKg), RemainingRawKg);
            remainingRawKg -= amount;
            return amount;
        }
    }
}
