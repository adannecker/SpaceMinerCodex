using System;
using UnityEngine;

namespace SpaceMiner
{
    // Sizing assumptions for broken ice/regolith, not measured asteroid material.
    // Water fractions are MASS fractions; porosity is represented once by packing.
    public sealed class DroneCargoProfile
    {
        public const float DefaultUsableVolumeM3 = 1.458f;
        public const float DefaultTargetFillFraction = .9f;
        public const float DefaultPackingFraction = .75f;
        public const float DefaultIceDensityKgM3 = 917f;
        public const float DefaultRestDensityKgM3 = 2500f;

        public bool VolumeLimited { get; private set; }
        public float UsableVolumeM3 { get; private set; }
        public float TargetFillFraction { get; private set; }
        public float PackingFraction { get; private set; }
        public float IceDensityKgM3 { get; private set; }
        public float RestDensityKgM3 { get; private set; }

        [Serializable] public sealed class SavedState
        {
            // No field initializers: JsonUtility can instantiate an absent inline
            // object. Only version 1 declares actual saved configuration.
            public int version;
            public bool volumeLimited;
            public float usableVolumeM3, targetFillFraction, packingFraction;
            public float iceDensityKgM3, restDensityKgM3;
        }

        private DroneCargoProfile(bool volumeLimited)
        {
            VolumeLimited = volumeLimited;
            UsableVolumeM3 = DefaultUsableVolumeM3;
            TargetFillFraction = DefaultTargetFillFraction;
            PackingFraction = DefaultPackingFraction;
            IceDensityKgM3 = DefaultIceDensityKgM3;
            RestDensityKgM3 = DefaultRestDensityKgM3;
        }
        public static DroneCargoProfile Campaign() => new DroneCargoProfile(true);
        // Preserve old/special hardware and live cargo; do not silently upgrade it.
        public static DroneCargoProfile Legacy() => new DroneCargoProfile(false);
        public DroneCargoProfile Copy() => Restore(Capture());
        public bool Valid => IsValidState(Capture());

        public float SolidDensityKgM3(float waterMassFraction)
        {
            float water = Mathf.Clamp01(waterMassFraction);
            return 1f / (water / IceDensityKgM3 + (1f - water) / RestDensityKgM3);
        }
        public float OccupiedVolumeM3(float massKg, float waterMassFraction) =>
            Mathf.Max(0, massKg) / (SolidDensityKgM3(waterMassFraction) * PackingFraction);
        public float CapacityKg(float waterMassFraction, float mechanicalLimitKg) =>
            VolumeLimited ? Mathf.Min(mechanicalLimitKg, UsableVolumeM3 * TargetFillFraction
                * PackingFraction * SolidDensityKgM3(waterMassFraction)) : mechanicalLimitKg;

        public SavedState Capture() => new SavedState {
            version = 1, volumeLimited = VolumeLimited, usableVolumeM3 = UsableVolumeM3,
            targetFillFraction = TargetFillFraction, packingFraction = PackingFraction,
            iceDensityKgM3 = IceDensityKgM3, restDensityKgM3 = RestDensityKgM3
        };
        public static bool HasData(SavedState state) => state != null && state.version == 1;
        public static bool IsValidState(SavedState state)
        {
            if (state == null) return true;
            if (state.version == 0) return !state.volumeLimited && state.usableVolumeM3 == 0
                && state.targetFillFraction == 0 && state.packingFraction == 0
                && state.iceDensityKgM3 == 0 && state.restDensityKgM3 == 0;
            bool Positive(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value > 0;
            return state.version == 1 && Positive(state.usableVolumeM3)
                && Positive(state.targetFillFraction) && state.targetFillFraction <= 1
                && Positive(state.packingFraction) && state.packingFraction <= 1
                && Positive(state.iceDensityKgM3) && Positive(state.restDensityKgM3);
        }
        public static DroneCargoProfile Restore(SavedState state)
        {
            if (!IsValidState(state)) throw new ArgumentException("Ungültiges Drohnen-Frachtprofil.");
            if (!HasData(state)) return Legacy();
            return new DroneCargoProfile(state.volumeLimited) {
                UsableVolumeM3 = state.usableVolumeM3, TargetFillFraction = state.targetFillFraction,
                PackingFraction = state.packingFraction, IceDensityKgM3 = state.iceDensityKgM3,
                RestDensityKgM3 = state.restDensityKgM3
            };
        }
    }
}
