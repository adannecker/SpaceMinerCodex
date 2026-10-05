using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    [CreateAssetMenu(menuName = "Space Miner/Asteroiden/Typenkatalog", fileName = "AsteroidCatalog")]
    public sealed class AsteroidCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public AsteroidType Type;
            [Min(0f)] public float Weight = 1f;
        }

        public List<Entry> Types = new List<Entry>();

        public AsteroidType Choose(int seed)
        {
            double total = 0;
            foreach (Entry entry in Types)
                if (entry != null && entry.Type != null && entry.Weight > 0 && !float.IsInfinity(entry.Weight)) total += entry.Weight;
            if (total <= 0) throw new InvalidOperationException("Asteroidenkatalog benötigt mindestens einen gültigen Typ mit positivem Gewicht.");
            double choice = new System.Random(seed).NextDouble() * total;
            AsteroidType last = null;
            foreach (Entry entry in Types)
            {
                if (entry == null || entry.Type == null || !(entry.Weight > 0) || float.IsInfinity(entry.Weight)) continue;
                last = entry.Type;
                choice -= entry.Weight;
                if (choice <= 0) return entry.Type;
            }
            return last;
        }
    }
}
