using System;
using UnityEngine;

namespace SpaceMiner
{
    // Percentages: stamina/health are reserves; fatigue/hunger/thirst are needs.
    // Only stamina has a running mechanic until food, drink, sleep and damage exist.
    [Serializable]
    public sealed class PlayerVitals
    {
        public const float WalkSpeed = 2.2f;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public const float SprintSpeed = 10.4f;
#else
        public const float SprintSpeed = 5.2f;
#endif
        public const float SprintDrain = 18f, RecoveryRate = 12f, RecoveryWait = 1.5f, RestartThreshold = 20f;
        public float Stamina = 100, Fatigue, Hunger, Thirst, Health = 100;
        public float RecoveryDelay;
        public bool Exhausted;
        [NonSerialized] public bool IsSprinting;

        public void Reset()
        {
            Stamina = Health = 100;
            Fatigue = Hunger = Thirst = RecoveryDelay = 0;
            Exhausted = IsSprinting = false;
        }

        // Returns the part of this walking step that can actually be sprinted.
        public float Advance(float seconds, bool wantsSprint, bool moving)
        {
            IsSprinting = false;
            if (!Finite(seconds) || seconds <= 0) return 0;
            if (Exhausted && !wantsSprint && Stamina >= RestartThreshold) Exhausted = false;
            float sprintSeconds = 0;
            if (moving && wantsSprint && !Exhausted && Stamina > 0)
            {
                sprintSeconds = Mathf.Min(seconds, Stamina / SprintDrain);
                Stamina = Mathf.Max(0, Stamina - sprintSeconds * SprintDrain);
                RecoveryDelay = RecoveryWait;
                if (Stamina <= .0001f) { Stamina = 0; Exhausted = true; }
                IsSprinting = !Exhausted;
            }
            float rest = seconds - sprintSeconds;
            float delay = Mathf.Min(RecoveryDelay, rest);
            RecoveryDelay -= delay;
            Stamina = Mathf.Min(100, Stamina + (rest - delay) * RecoveryRate);
            return sprintSeconds;
        }

        public PlayerVitals Copy() => new PlayerVitals
        {
            Stamina = Stamina, Fatigue = Fatigue, Hunger = Hunger, Thirst = Thirst,
            Health = Health, RecoveryDelay = RecoveryDelay, Exhausted = Exhausted
        };

        public bool IsValid => Percent(Stamina) && Percent(Fatigue) && Percent(Hunger) && Percent(Thirst) && Percent(Health)
            && Finite(RecoveryDelay) && RecoveryDelay >= 0 && RecoveryDelay <= RecoveryWait;
        private static bool Percent(float value) => Finite(value) && value >= 0 && value <= 100;
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
