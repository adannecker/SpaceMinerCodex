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
        // Serialized legacy calibration:0.5 is100% of the physical PV model, not0.5kW.
        // Keep field/name for existing saves; actual kW come from CaptureGeneration().
        public float SolarPowerKw = 0.5f;
        public float DroneChargePowerKw = 2f;
        public float SimulationRate = 1f;
        public MiningResearch Mining = new MiningResearch();
        private float resumeRate = 1f;

        private readonly FluidTank waterTank=new FluidTank(StationResources.Water,200,20);
        public FluidTank WaterTank {get{waterTank.capacity=TankCapacityLiters;return waterTank;}}
        public float WaterLiters=>WaterTank.amount;
        public StationOperations Operations {get;internal set;}
        public bool UsesIcePlant=>Operations!=null&&Operations.Live&&Operations.IcePlant!=null;
        public float AllocatedChargeKw=>Operations!=null&&Operations.Live?Operations.ChargePerDroneKw:DroneChargePowerKw;
        public int Deliveries { get; private set; }
        public float DeliveredLiters { get; private set; }
        public bool SourceAssigned { get; private set; }
        public bool QuestComplete => WaterLiters >= TankCapacityLiters - 0.001f;
        public string Message { get; private set; } = "Wähle eine Eisquelle und starte den Tankauftrag.";
        public DroneAgent Worker { get; private set; }
        public DroneAgent[] Drones { get; private set; }
        public AsteroidResource[] Asteroids { get; private set; }
        public ScannerProgression Scanner { get; private set; }

        private void Awake()
        {
            InitializeObjects();
            SimulationRate = 1f; // Legacy scene stores 100x; normal play now starts at 1x.
            ResetScenario();
            Scanner = gameObject.AddComponent<ScannerProgression>();
            Scanner.Initialize(this);
            if (Camera.main.GetComponent<SaveGameMenu>() == null) Camera.main.gameObject.AddComponent<SaveGameMenu>();
            if (Camera.main.GetComponent<ScanSaveCheck>() == null) Camera.main.gameObject.AddComponent<ScanSaveCheck>();
        }

        private void Update()
        {
            if (IntroSequence.BlocksGameplay || SettingsMenu.PausesSimulation) return;
            if (!SettingsMenu.BlocksInput && !StationOperations.TacticalTextInputActive && !StationInteriorMode.IsInside && Input.GetKeyDown(KeyCode.Tab) && Worker != null)
                Camera.main.GetComponent<OrbitCamera>().FollowDrone(Worker);
            Advance(Time.unscaledDeltaTime * SimulationRate);
        }

        public void SetSimulationRate(float rate) { SimulationRate = rate; if (rate > 0) resumeRate = rate; }
        public void TogglePause() { if (SimulationRate > 0) { resumeRate = SimulationRate; SimulationRate = 0; } else SimulationRate = resumeRate; }

        public void Advance(float seconds)
        {
            if(!ResourceContainer.Finite(seconds)||seconds<=0)return;
            // Simulation time is independent of camera/UI time. Small steps keep braking stable.
            while (seconds > 0.0001f)
            {
                float step = Mathf.Min(seconds, 0.25f);
                Operations?.Prepare(step);
                foreach(var drone in Drones)if(drone.IsOperational)drone.Tick(step);
                GetComponent<CampaignDroneFleet>()?.Advance(step);
                Operations?.Advance(step);
                Scanner?.Advance(step);
                seconds -= step;
            }
        }

        public bool AssignTankOrder(AsteroidResource source)
        {
            return AssignTankOrder(source,0);
        }
        public bool AssignTankOrder(AsteroidResource source,int droneIndex)
        {
            if(droneIndex<0||droneIndex>=Drones.Length||droneIndex>0&&(Operations==null||!Operations.CanUseDrone(droneIndex)))return Reject("Drohne nicht freigeschaltet.");
            var assigned=Drones[droneIndex];
            if(Operations!=null&&Operations.IsRefitting(droneIndex))return Reject("Werkzeugumbau läuft: Drohne bleibt im Dock.");
            if (!UsesIcePlant&&QuestComplete) return Reject("Der Schiffstank ist bereits voll.");
            if (source == null || !source.CanMineWater) return Reject("Kein bestätigtes, abbaubares Wasservorkommen.");
            if (!assigned.IsOperational) return Reject("Drohne muss zuerst repariert werden.");
            if (!assigned.IsReady) return Reject("Drohne führt bereits einen Auftrag aus oder lädt noch.");
            if (assigned.CargoKg>.001f) return Reject("Restladung zuerst an der Eisannahme abgeben.");
            foreach(var other in Drones)if(other!=assigned&&other.HasTankOrder)return Reject("Entladeplatz belegt: Wasserauftrag läuft bereits.");
            if(Operations!=null&&Operations.State.tools[droneIndex]!="Bohrer")return Reject("Für Eisabbau ist der Bohrer erforderlich.");
            if (!assigned.CanCompleteTrip(source, out string reason)) return Reject(reason);
            assigned.Assign(source);
            SourceAssigned = true;
            Message = (UsesIcePlant?"Einzelne Roh-Eislieferung gestartet: ":"Tankauftrag gestartet: ") + source.Info.DisplayName;
            Scanner?.Say("DROHNE 01 UNTERWEGS", "Der Auftrag ist bestätigt. Drohne 01 fliegt zur gescannten Wasserquelle. Ihre Nahbereichssensoren helfen beim Annähern. Wir überwachen Batterie, Treibwasser und Rückkehrreserve.");
            return true;
        }

        private bool Reject(string reason) { Message = reason; return false; }
        public double RawReceivingCapacity(double waterFraction)=>UsesIcePlant?Operations.RawReceivingCapacity(waterFraction):0;
        public float DepositRawIce(float rawKg,float waterFraction)
        {
            return (float)DepositRawIce(rawKg,rawKg,waterFraction,out _);
        }
        public double DepositRawIce(float cargoBefore,double requestedRawKg,float waterFraction,out float remainingCargo)
        {
            remainingCargo=cargoBefore;
            if(!UsesIcePlant||!Operations.IceReceivingAvailable||cargoBefore<=0||requestedRawKg<=0)return 0;
            double limit=Math.Min(cargoBefore,Math.Min(requestedRawKg,RawReceivingCapacity(waterFraction)));
            if(limit<=0)return 0;
            float remainder=(float)Math.Max(0,(double)cargoBefore-limit);
            if((double)cargoBefore-remainder>limit)remainder+=Math.Abs(remainder)*1.2e-7f;
            double actual=(double)cargoBefore-remainder;
            if(actual<=0)return 0;
            double accepted=Operations.IcePlant.Receive(actual,waterFraction);
            if(accepted!=actual)throw new InvalidOperationException("Rohannahme änderte ihre reservierte Kapazität während einer atomaren Übergabe.");
            remainingCargo=remainder;return actual;
        }
        public void RecordRawDelivery(){Deliveries++;Message="Roh-Eis an der geschlossenen Annahme übernommen; Aufbereitung liefert Wasser nach Bedarf.";}
        internal void TransferProcessedWater()
        {
            if(!UsesIcePlant||!Operations.WaterConnected(0)||!Operations.WaterConnected(7))return;
            double limit=Math.Min(Operations.IcePlant.CleanWaterPendingKg,WaterTank.Free);
            if(limit<=0)return;
            float before=WaterLiters,desired=(float)((double)before+limit);
            if((double)desired-before>limit)desired-=Math.Abs(desired)*1.2e-7f;
            float transfer=Math.Max(0,desired-before);if(transfer<=0)return;
            WaterTank.Add(transfer);double actual=(double)WaterLiters-before;
            Operations.IcePlant.ExportCleanWater(actual);DeliveredLiters+=(float)actual;
        }

        public float DepositWater(float liters)
        {
            float accepted = Mathf.Min(Mathf.Max(0f, liters), TankCapacityLiters - WaterLiters);
            WaterTank.Add(accepted);
            DeliveredLiters += accepted;
            if (accepted > 0f) Deliveries++;
            Message = QuestComplete ? "Auftrag abgeschlossen: Der Wassertank ist voll." : "Wasser abgeliefert. Drohne wird versorgt.";
            return accepted;
        }

        public float TakeFuelWater(float requested)
        {
            float amount = Mathf.Min(Mathf.Max(0f, requested), WaterLiters);
            WaterTank.Remove(amount);
            return amount;
        }

        public void Notify(string message) { Message = message; }
        public void RestoreSupply(float water, int deliveries, float delivered, bool assigned)
        { WaterTank.Set(water); Deliveries = deliveries; DeliveredLiters = delivered; SourceAssigned = assigned; }

        public void ResetScenario()
        {
            StationInteriorMode.Current?.Vitals.Reset();
            WaterTank.Set(StartingWaterLiters);
            Deliveries = 0;
            DeliveredLiters = 0f;
            SourceAssigned = false;
            Mining.Reset();
            Message = "Wähle eine Eisquelle und starte den Tankauftrag.";
            foreach (AsteroidResource asteroid in Asteroids) asteroid.ResetDeposit();
            foreach (DroneAgent drone in Drones) drone.ResetDrone();
            GetComponent<CampaignDroneFleet>()?.ResetPresentation();
            Operations?.ResetCampaign();
            Scanner?.ResetProgress();
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
                resource.IsStarterWaterSource = startingSource;
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
                agent.IsOperational = i == 0;
                agent.NeedsInitialCharge = false;
                var berth = station != null ? station.Berth(i) : null;
                agent.HomePosition = berth != null ? berth.position : body.transform.position;
                agent.HomeRotation = berth != null ? berth.rotation : Quaternion.identity;
                Drones[i] = agent;
                var miningVisual = body.GetComponent<MiningDroneVisual>();
                if (miningVisual == null) miningVisual = body.AddComponent<MiningDroneVisual>();
                miningVisual.Build(agent);
                info.Description = i == 0 ? "Einheitliches Bergbaumodell / Drohne 01 einsatzbereit" : "Einheitliches Bergbaumodell / inaktiv, Havarieschaden";
                if (i > 0)
                {
                    var properties = new MaterialPropertyBlock();
                    properties.SetColor("_Color", Color.Lerp(new Color(.32f,.38f,.42f),new Color(.47f,.51f,.53f),(i%4)/3f));
                    foreach (Renderer renderer in body.GetComponentsInChildren<Renderer>())
                        if (renderer.name == "Main chassis" || renderer.name.StartsWith("Cargo side wall")) renderer.SetPropertyBlock(properties);
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
