using System;
using UnityEngine;

namespace SpaceMiner
{
    [CreateAssetMenu(menuName = "Space Miner/Asteroidenfeld", fileName = "BeltSettings")]
    public sealed class BeltSettings : ScriptableObject
    {
        [Serializable]
        public struct Asteroid
        {
            public string Name;
            public Vector3 PositionMeters;
            [Min(1f)] public float DiameterMeters;
            public AsteroidType Type;
            public int Seed;
        }

        public AsteroidCatalog Catalog;

        public Asteroid[] Asteroids =
        {
            new Asteroid { Name = "A-01", PositionMeters = new Vector3(-170, 45, 420), DiameterMeters = 100 },
            new Asteroid { Name = "A-02", PositionMeters = new Vector3(280, -70, 800), DiameterMeters = 260 },
            new Asteroid { Name = "A-03", PositionMeters = new Vector3(-560, -180, 1300), DiameterMeters = 420 },
            new Asteroid { Name = "A-04", PositionMeters = new Vector3(800, 260, 1900), DiameterMeters = 650 },
            new Asteroid { Name = "A-05", PositionMeters = new Vector3(-1500, 300, 2800), DiameterMeters = 900 },
            new Asteroid { Name = "A-06", PositionMeters = new Vector3(150, -750, 3600), DiameterMeters = 1200 },
            new Asteroid { Name = "A-07", PositionMeters = new Vector3(2400, 450, 4200), DiameterMeters = 1500 },
            new Asteroid { Name = "A-08", PositionMeters = new Vector3(-3000, -650, 4700), DiameterMeters = 1800 },
            new Asteroid { Name = "A-09", PositionMeters = new Vector3(1000, 1800, 6100), DiameterMeters = 1100 },
            new Asteroid { Name = "A-10", PositionMeters = new Vector3(-2200, 1000, 7400), DiameterMeters = 2200 },
            new Asteroid { Name = "A-11", PositionMeters = new Vector3(4400, -1000, 8000), DiameterMeters = 2600 },
            new Asteroid { Name = "A-12 / Grossasteroid", PositionMeters = new Vector3(5500, 900, 4200), DiameterMeters = 5000 }
        };
    }
}
