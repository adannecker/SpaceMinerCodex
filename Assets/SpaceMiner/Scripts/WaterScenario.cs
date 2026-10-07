using System;
using UnityEngine;

namespace SpaceMiner
{
    public sealed class WaterScenario : MonoBehaviour
    {
        public float TankCapacityLiters = 200f;
        public float StartingWaterLiters = 20f;
        public int OperationalDrones = 1;
        public float ReactorPowerKw = 2f;
        public float SolarPowerKw = 0.5f;
        public float DroneChargePowerKw = 2f;
        public float SimulationRate = 1f;
        public MiningResearch Mining = new MiningResearch();
        private float resumeRate = 1f;

        public float WaterLiters { get; private set; }
        public int Deliveries { get; private set; }
        public float DeliveredLiters { get; private set; }
        public bool SourceAssigned { get; private set; }
        public bool QuestComplete => WaterLiters >= TankCapacityLiters - 0.001f;
        public string Message { get; private set; } = "Wähle eine Eisquelle und starte den Tankauftrag.";
        public DroneAgent Worker { get; private set; }
        public DroneAgent[] Drones { get; private set; }
        public AsteroidResource[] Asteroids { get; private set; }

        private void Awake()
        {
            InitializeObjects();
            SimulationRate = 1f; // Legacy scene stores 100x; normal play now starts at 1x.
            ResetScenario();
        }

        private void Update()
        {
            if (IntroSequence.BlocksGameplay || SettingsMenu.PausesSimulation) return;
            if (!SettingsMenu.BlocksInput && Input.GetKeyDown(KeyCode.Space)) TogglePause();
            if (!SettingsMenu.BlocksInput && Input.GetKeyDown(KeyCode.Tab) && Worker != null)
                Camera.main.GetComponent<OrbitCamera>().Focus(Worker.Info);
            Advance(Time.unscaledDeltaTime * SimulationRate);
        }

        public void SetSimulationRate(float rate) { SimulationRate = rate; if (rate > 0) resumeRate = rate; }
        public void TogglePause() { if (SimulationRate > 0) { resumeRate = SimulationRate; SimulationRate = 0; } else SimulationRate = resumeRate; }

        public void Advance(float seconds)
        {
            // Simulation time is independent of camera/UI time. Small steps keep braking stable.
            while (seconds > 0.0001f)
            {
                float step = Mathf.Min(seconds, 0.25f);
                Worker.Tick(step);
                seconds -= step;
            }
        }

        public bool AssignTankOrder(AsteroidResource source)
        {
            if (QuestComplete) return Reject("Der Schiffstank ist bereits voll.");
            if (source == null || !source.CanMineWater) return Reject("Kein bestätigtes, abbaubares Wasservorkommen.");
            if (!Worker.IsOperational) return Reject("Drohne 01 muss zuerst repariert werden.");
            if (!Worker.IsReady) return Reject("Drohne 01 führt bereits einen Auftrag aus oder lädt noch.");
            if (!Worker.CanCompleteTrip(source, out string reason)) return Reject(reason);
            Worker.Assign(source);
            SourceAssigned = true;
            Message = "Tankauftrag gestartet: " + source.Info.DisplayName;
            return true;
        }

        private bool Reject(string reason) { Message = reason; return false; }

        public float DepositWater(float liters)
        {
            float accepted = Mathf.Min(Mathf.Max(0f, liters), TankCapacityLiters - WaterLiters);
            WaterLiters += accepted;
            DeliveredLiters += accepted;
            if (accepted > 0f) Deliveries++;
            Message = QuestComplete ? "Auftrag abgeschlossen: Der Wassertank ist voll." : "Wasser abgeliefert. Drohne wird versorgt.";
            return accepted;
        }

        public float TakeFuelWater(float requested)
        {
            float amount = Mathf.Min(Mathf.Max(0f, requested), WaterLiters);
            WaterLiters -= amount;
            return amount;
        }

        public void Notify(string message) { Message = message; }

        public void ResetScenario()
        {
            WaterLiters = StartingWaterLiters;
            Deliveries = 0;
            DeliveredLiters = 0f;
            SourceAssigned = false;
            Mining.Reset();
            Message = "Wähle eine Eisquelle und starte den Tankauftrag.";
            foreach (AsteroidResource asteroid in Asteroids) asteroid.ResetDeposit();
            foreach (DroneAgent drone in Drones) drone.ResetDrone();
        }

        private void InitializeObjects()
        {
            Transform root = GameObject.Find("Asteroids (1 unit = 1 metre)").transform;
            Asteroids = new AsteroidResource[root.childCount];
            for (int i = 0; i < root.childCount; i++)
            {
                GameObject body = root.GetChild(i).gameObject;
                var resource = body.GetComponent<AsteroidResource>();
                if (resource == null) resource = body.AddComponent<AsteroidResource>();
                var generator = body.GetComponent<AsteroidGenerator>();
                if (generator == null)
                {
                    var instance = body.GetComponent<SpiralAsteroid>();
                    if (instance != null) generator = instance.Template;
                }
                bool startingSource = body.name == "A-01" || body.name == "A-02" || body.name == "A-03";
                resource.WaterFraction = generator != null && generator.Type != null
                    ? (generator.WaterFractionOverride >= 0 ? generator.WaterFractionOverride : generator.Type.WaterFraction)
                    : startingSource ? (body.name == "A-02" ? 0.65f : 0.8f) : 0f;
                bool icy = resource.WaterFraction > 0;
                resource.WaterIdentified = startingSource && icy;
                resource.InitialRawKg = icy ? 10000f : 0f;
                if (icy)
                {
                    resource.Info.Description = (generator != null ? generator.Type.DisplayName : "Eisreicher Asteroid")
                        + (resource.WaterIdentified ? " / einfacher Wasserscan" : " / ungescannt");
                }
                Asteroids[i] = resource;
            }

            Drones = new DroneAgent[10];
            var station = GetComponent<StationVisual>();
            if (station != null) station.Build();
            Material hull = GameObject.Find("Drone 1").GetComponentInChildren<Renderer>().sharedMaterial;
            for (int i = 0; i < Drones.Length; i++)
            {
                string name = "Drone " + (i + 1);
                GameObject body = GameObject.Find(name);
                if (body == null)
                {
                    body = new GameObject(name);
                    body.transform.position = new Vector3(-9f + ((i - 2) % 4) * 5f, -5f, -15f - ((i - 2) / 4) * 5f);
                    var shape = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    shape.name = "Body";
                    shape.transform.SetParent(body.transform, false);
                    shape.transform.localScale = Vector3.one * 2f;
                    shape.GetComponent<Renderer>().sharedMaterial = hull;
                    body.AddComponent<SpaceObject>();
                }
                var info = body.GetComponent<SpaceObject>();
                info.DisplayName = "DROHNE " + (i + 1).ToString("00");
                info.DiameterMeters = 2f;
                info.Description = i < OperationalDrones ? "2 × 2 × 2 m / einsatzfähig"
                    : i == 1 ? "2 × 2 × 2 m / funktionsfähig, wartet auf Versorgung" : "2 × 2 × 2 m / Havarieschaden";
                var agent = body.GetComponent<DroneAgent>();
                if (agent == null) agent = body.AddComponent<DroneAgent>();
                agent.Scenario = this;
                agent.IsOperational = i < OperationalDrones || i == 1;
                agent.NeedsInitialCharge = i == 1 && i >= OperationalDrones;
                var berth = station != null ? station.Berth(i) : null;
                agent.HomePosition = berth != null ? berth.position : body.transform.position;
                agent.HomeRotation = berth != null ? berth.rotation : Quaternion.identity;
                Drones[i] = agent;
                var miningVisual = body.GetComponent<MiningDroneVisual>();
                if (miningVisual == null) miningVisual = body.AddComponent<MiningDroneVisual>();
                miningVisual.Build(agent);
                if (!agent.IsOperational || agent.NeedsInitialCharge)
                {
                    var properties = new MaterialPropertyBlock();
                    properties.SetColor("_Color", new Color(0.17f, 0.2f, 0.23f));
                    properties.SetColor("_EmissionColor", Color.black);
                    foreach (Renderer renderer in body.GetComponentsInChildren<Renderer>()) renderer.SetPropertyBlock(properties);
                }
            }
            Worker = Drones[0];
        }

        public static string Duration(float seconds)
        {
            if (float.IsInfinity(seconds) || float.IsNaN(seconds)) return "—";
            int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return total >= 3600 ? (total / 3600) + " h " + ((total % 3600) / 60) + " min"
                : total >= 60 ? (total / 60) + " min " + (total % 60) + " s" : total + " s";
        }
    }
}
