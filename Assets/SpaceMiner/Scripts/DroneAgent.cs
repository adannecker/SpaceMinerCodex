using UnityEngine;

namespace SpaceMiner
{
    public enum DronePhase { Ready, Outbound, Mining, Returning, Unloading, Servicing, Disabled, Stranded, WaitingForPower, Docking, Undocking, Turning, Parking, Launching, TankDocking, TankUndocking, Berthing, StationWork }

    public sealed class DroneAgent : MonoBehaviour
    {
        public WaterScenario Scenario;
        public bool IsOperational;
        public bool NeedsInitialCharge;
        public Vector3 HomePosition;
        public Quaternion HomeRotation = Quaternion.identity;
        public static readonly Vector3 CargoOutletOffset = new Vector3(0,0.53f,-0.92f);
        // Runtime sizing assumptions; old saves retain their own twelve hardware values.
        public float DryMassKg = 250f;
        public float FuelCapacityLiters = 15f;
        public float BatteryCapacityKwh = 16f;
        public float CargoCapacityKg = 1200f; // Mechanical payload limit; volume can stop loading earlier.
        public float CruiseSpeed = 2f;
        public float ThrustNewtons = 5f;
        public float ExhaustSpeed = 1000f;
        public float HeatingKwhPerLiter = 1.2f;
        public float MiningRateKgPerSecond = 0.5f;
        public float MiningPowerKw = 4f;
        public float OnboardPowerKw = 0.05f;
        [Range(0f, .5f)] public float ReserveFraction = .1f;
        public float EffectiveMiningRate => MiningRateKgPerSecond * Scenario.Mining.RateMultiplier;
        public float EffectiveMiningPower => MiningPowerKw * Scenario.Mining.PowerMultiplier;
        public float EnergyReserveKwh => BatteryCapacityKwh * ReserveFraction;
        public float FuelReserveLiters => FuelCapacityLiters * ReserveFraction;
        public DroneCargoProfile CargoProfile { get; private set; } = DroneCargoProfile.Campaign();
        public float CargoWaterMassFraction => CargoKg <= .0001f && Target == null ? .8f : cargoWaterFraction;
        public float EffectiveCargoCapacityKg => CargoProfile.CapacityKg(CargoWaterMassFraction, CargoCapacityKg);
        public float CargoVolumeM3 => CargoProfile.OccupiedVolumeM3(CargoKg, CargoWaterMassFraction);
        // New profiles show checked-volume occupancy (full plan = 90%). Legacy
        // previews retain their old relative mass display without inflating cargo.
        public float CargoFillFraction => Mathf.Clamp01(CargoProfile.VolumeLimited
            ? CargoVolumeM3 / CargoProfile.UsableVolumeM3 : CargoKg / Mathf.Max(.001f, CargoCapacityKg));
        public float CargoCapacityFor(AsteroidResource source) => CargoProfile.CapacityKg(source != null ? source.WaterFraction : CargoWaterMassFraction, CargoCapacityKg);
        public void UseLegacyCargoProfile() { CargoProfile = DroneCargoProfile.Legacy(); }
        // Called only when a genuinely new campaign is accepted, before its supply reset.
        // General simulation resets and saved-game loads intentionally do not call this.
        public void InitializeNewCampaignHardware()
        {
            DryMassKg = 250f; FuelCapacityLiters = 15f; BatteryCapacityKwh = 16f;
            CargoCapacityKg = 1200f; CruiseSpeed = 2f; ThrustNewtons = 5f; ExhaustSpeed = 1000f;
            HeatingKwhPerLiter = 1.2f; MiningRateKgPerSecond = .5f; MiningPowerKw = 4f;
            OnboardPowerKw = .05f; ReserveFraction = .1f; CargoProfile = DroneCargoProfile.Campaign();
        }
        // Explicit opt-in for an old save. Resource amounts are preserved exactly;
        // a larger legacy store must be used first rather than silently discarded.
        public bool ApplyNewSizingAtDock(out string reason)
        {
            bool parked = Phase == DronePhase.Ready || Phase == DronePhase.Disabled
                || Phase == DronePhase.Servicing || Phase == DronePhase.WaitingForPower;
            if (!parked || HasTankOrder || CargoKg > .0001f || Vector3.Distance(transform.position, HomePosition) > .05f)
            { reason = "Auslegung nur ohne Auftrag und Fracht am eigenen Dock umstellen."; return false; }
            if (BatteryKwh > 16f || FuelLiters > 15f)
            { reason = "Vorhandene Vorräte übersteigen die neue Kapazität; erst verbrauchen."; return false; }
            InitializeNewCampaignHardware();
            if (IsOperational && Phase == DronePhase.Ready) Phase = DronePhase.Servicing;
            reason = IsOperational ? "Neue Testauslegung übernommen; vorhandene Vorräte erhalten. Reguläres Laden und Betanken aus Stationsbeständen."
                : "Neue Testauslegung übernommen; vorhandene Vorräte erhalten. Drohne bleibt inaktiv.";
            return true;
        }
        public float RequiredReturnEnergyKwh => ReturnBudget(CargoKg).Energy + EnergyReserveKwh;
        public float RequiredReturnFuelLiters => ReturnBudget(CargoKg).Fuel + FuelReserveLiters;
        public float RequiredDockReturnEnergyKwh=>DockReturnBudget(CargoKg).Energy+EnergyReserveKwh;
        public float RequiredDockReturnFuelLiters=>DockReturnBudget(CargoKg).Fuel+FuelReserveLiters;
        public string ReturnReason { get; private set; } = "";

        public DronePhase Phase { get; private set; }
        public AsteroidResource Target { get; private set; }
        public float FuelLiters { get; private set; }
        public float BatteryKwh { get; private set; }
        public float CargoKg { get; private set; }
        public float CargoWaterLiters => CargoKg * cargoWaterFraction;
        public bool RawIceDelivery=>rawIceDelivery;
        public bool DeliveringExistingCargo=>retainedCargoFlight;
        public float Speed { get; private set; }
        public float BurnedWaterLiters { get; private set; }
        public bool HasTankOrder { get; private set; }
        public bool IsReady => Phase == DronePhase.Ready;
        public float PhaseElapsed => phaseSeconds;
        public Vector3 SurfacePoint { get; private set; }
        public Vector3 SurfaceNormal { get; private set; }
        private Vector3 miningAxisOffset;
        private float miningStandOff = 2.4f;
        private Vector3 cargoPortOffset = CargoOutletOffset;
        private DroneFlightPlanner navigator;
        internal DroneFlightPlanner Navigation=>navigator;
        private Vector3[] route;
        private int routeIndex;
        private float launchDistance=1.8f,launchLift,holdHeight=8f;
        private float unloadStart=25f,unloadEnd=215f;
        internal void ConfigureUnloading(float start,float end) { unloadStart=start; unloadEnd=end; }
        internal void ConfigureNavigation(DroneFlightPlanner planner) { navigator=planner; launchDistance=6; launchLift=1.1f; holdHeight=12; }
        internal Vector3 HomeHoldPoint=>HomeApproachPoint+Vector3.up*holdHeight;
        // Presentation geometry varies by hull. Defaults preserve existing drones and save behavior.
        internal void ConfigureToolGeometry(Vector3 axisOffset, float standOff, Vector3 outlet)
        { miningAxisOffset=axisOffset; miningStandOff=standOff; cargoPortOffset=outlet; }
        public Vector3 CargoOutletPoint => transform.TransformPoint(cargoPortOffset);
        private Transform Socket => Scenario != null ? Scenario.GetComponent<StationVisual>()?.TankSocket : null;
        public Vector3 TankOutward => Socket != null ? Socket.forward : Vector3.back;
        public Quaternion TankDockRotation => Facing(TankOutward);
        public Vector3 TankDockPoint => TankInlet - TankDockRotation * cargoPortOffset;
        public Vector3 TankApproachPoint => TankDockPoint + TankOutward * 4f + Vector3.up * 0.6f;
        public Vector3 HomeApproachPoint => HomePosition + HomeRotation * Vector3.forward * launchDistance+Vector3.up*launchLift;
        public float UnloadProgress => Phase == DronePhase.Unloading ? rawIceDelivery?Mathf.Clamp01(1-CargoKg/Mathf.Max(.001f,unloadInitialCargoKg))
            : Mathf.Clamp01((phaseSeconds-unloadStart)/Mathf.Max(1f,unloadEnd-unloadStart)) : 0f;
        public Vector3 TankInlet
        {
            get
            {
                return Socket != null ? Socket.position : HomePosition + Vector3.forward * 3.6f;
            }
        }
        public SpaceObject Info => GetComponent<SpaceObject>();
        public float MiningSecondsRemaining => Phase == DronePhase.Mining ? Mathf.Min(Mathf.Max(0f, cargoGoal - CargoKg) / EffectiveMiningRate,
            Mathf.Max(0f, BatteryKwh - RequiredReturnEnergyKwh) * 3600f / (EffectiveMiningPower + OnboardPowerKw)) : 0f;
        public float TargetDistance => IsFlying ? Vector3.Distance(transform.position, destination) : 0f;
        public bool IsFlying => Phase == DronePhase.Outbound || Phase == DronePhase.Returning || Phase == DronePhase.Parking;
        public float PhaseProgress => IsFlying ? Mathf.Clamp01(1f - TargetDistance / Mathf.Max(0.01f, legDistance))
            : Phase == DronePhase.Mining ? Mathf.Clamp01(CargoKg / Mathf.Max(0.01f, cargoGoal))
            : Phase == DronePhase.Unloading ? Mathf.Clamp01(phaseSeconds / 240f)
            : IsManeuver ? Mathf.Clamp01(phaseSeconds / 120f)
            : Phase == DronePhase.Servicing ? Mathf.Min(BatteryKwh / BatteryCapacityKwh, FuelLiters / FuelCapacityLiters)
            : Phase == DronePhase.Ready ? 1f : 0f;

        public string Status => Phase == DronePhase.Outbound ? "Anflug"
            : Phase == DronePhase.Docking ? "Klammer andocken / Bohrer ausfahren"
            : Phase == DronePhase.Undocking ? "Werkzeuge einziehen / Abdocken"
            : Phase == DronePhase.Turning ? "Zum Tank ausrichten"
            : Phase == DronePhase.Parking ? "Zum Ladeplatz"
            : Phase == DronePhase.Launching ? "Aus Ladebucht ausfahren"
            : Phase == DronePhase.Berthing ? "Rückwärts in Ladebucht einparken"
            : Phase == DronePhase.TankDocking ? "Rückwärts am Tank ankoppeln"
            : Phase == DronePhase.TankUndocking ? "Tankkupplung lösen"
            : Phase == DronePhase.Mining ? "Eisabbau"
            : Phase == DronePhase.Returning ? "Rückflug"
            : Phase == DronePhase.Unloading ? "Eis am Wassertank entladen"
            : Phase == DronePhase.Servicing ? "Laden / Auftanken"
            : Phase == DronePhase.Disabled ? "Defekt"
            : Phase == DronePhase.Stranded ? "Versorgung erschöpft"
            : Phase == DronePhase.WaitingForPower ? "Wartet auf Ladung"
            : "Bereit";

        private Vector3 destination;
        private float legDistance;
        private float phaseSeconds;
        private float cargoGoal;
        private float cargoWaterFraction;
        private bool rawIceDelivery;
        private bool retainedCargoFlight;
        private float unloadInitialCargoKg;
        private float finalRawGoal;
        private Vector3 maneuverStart;
        private Quaternion maneuverRotation;
        private bool parkingAbove;
        private bool outboundAbove;
        private bool productiveTrip;
        private bool cancelledTrip;
        private MeshCollider workingCollider;
        private Mesh originalColliderMesh;
        private bool IsManeuver => Phase == DronePhase.Docking || Phase == DronePhase.Undocking || Phase == DronePhase.Turning
            || Phase == DronePhase.Launching || Phase == DronePhase.TankDocking || Phase == DronePhase.TankUndocking || Phase == DronePhase.Berthing;

        public float ArrivalSeconds
        {
            get
            {
                if (!IsFlying) return 0f;
                float acceleration = ThrustNewtons / (DryMassKg + FuelLiters + CargoKg);
                float distance = TargetDistance;
                float brakeDistance = Speed * Speed / (2f * acceleration);
                if (distance <= brakeDistance + 0.5f) return Speed / acceleration;
                float peak = Mathf.Min(CruiseSpeed, Mathf.Sqrt(acceleration * distance + Speed * Speed * 0.5f));
                float rampDistance = Mathf.Max(0f, (2f * peak * peak - Speed * Speed) / (2f * acceleration));
                return Mathf.Max(0f, peak - Speed) / acceleration + peak / acceleration
                    + Mathf.Max(0f, distance - rampDistance) / Mathf.Max(0.01f, peak);
            }
        }

        public float PhaseSecondsRemaining => IsFlying ? ArrivalSeconds
            : Phase == DronePhase.Mining ? MiningSecondsRemaining
            : Phase == DronePhase.Unloading ? Mathf.Max(0f, 240f - phaseSeconds)
            : IsManeuver ? Mathf.Max(0f, 120f - phaseSeconds)
            : Phase == DronePhase.Servicing ? Mathf.Max((BatteryCapacityKwh - BatteryKwh) / Scenario.DroneChargePowerKw * 3600f,
                Scenario.WaterLiters > 0f ? FuelCapacityLiters - FuelLiters : float.PositiveInfinity)
            : 0f;

        public bool CanCompleteTrip(AsteroidResource source, out string reason)
        {
            if (source == null || !source.CanMineWater) { reason = "Kein abbaubares Wasservorkommen."; return false; }
            Vector3 approach = ApproachPoint(source);
            Vector3 above = HomeHoldPoint;
            bool rawDelivery=HasTankOrder?rawIceDelivery:Scenario.UsesIcePlant;
            float targetCargo = PlannedCargoGoal(source,rawDelivery);
            if (targetCargo <= CargoKg + .0001f) { reason = rawDelivery?"Roh-Eisannahme geschlossen oder ohne freien Puffer.":"Kein Platz für eine weitere Wasserlieferung."; return false; }
            Budget outbound = FlightBudget(CargoKg, 240f, HomeApproachPoint, above, approach);
            Budget returning = ReturnBudget(targetCargo, source.WaterFraction,rawDelivery);
            float minimumKg = Mathf.Min(1f, targetCargo - CargoKg);
            float estimatedEnergy = outbound.Energy + returning.Energy + EnergyReserveKwh
                + minimumKg / EffectiveMiningRate * (EffectiveMiningPower + OnboardPowerKw) / 3600f;
            if (FuelLiters < outbound.Fuel + returning.Fuel + FuelReserveLiters) { reason = "Zu wenig Treibwasser für Hinflug, Rückkehr und Reserve."; return false; }
            if (BatteryKwh < estimatedEnergy) { reason = "Zu wenig Energie für Hinflug, Abbau, Rückkehr und Reserve."; return false; }
            reason = "";
            return true;
        }

        public void Assign(AsteroidResource source)
        {
            if (source == null || !source.CanMineWater) return;
            Target = source;
            cargoWaterFraction = source.WaterFraction;
            rawIceDelivery=Scenario.UsesIcePlant;
            retainedCargoFlight=false;
            HasTankOrder = true;
            BeginOutbound();
        }

        public bool DeliverExistingCargo(out string reason)
        {
            if(!Scenario.UsesIcePlant||!IsOperational||!IsReady||HasTankOrder||CargoKg<=.0001f
                ||Vector3.Distance(transform.position,HomePosition)>.05f)
            {reason="Bestandsfracht nur mit bereiter Drohne am eigenen Dock zur Eisannahme senden.";return false;}
            float accepted=Mathf.Min(CargoKg,(float)Scenario.RawReceivingCapacity(cargoWaterFraction));
            if(accepted<=0){reason="Roh-Eisannahme geschlossen oder voll.";return false;}
            Budget loaded=FlightBudget(CargoKg,navigator!=null?570f:480f,HomeApproachPoint,HomeHoldPoint,TankApproachPoint);
            Budget parking=FlightBudget(CargoKg-accepted,240f,TankApproachPoint,HomeHoldPoint,HomeApproachPoint);
            if(FuelLiters<loaded.Fuel+parking.Fuel+FuelReserveLiters||BatteryKwh<loaded.Energy+parking.Energy+EnergyReserveKwh)
            {reason="Versorgung für beladene Annahmefahrt, Rückkehr und Reserve reicht nicht.";return false;}
            rawIceDelivery=retainedCargoFlight=true;HasTankOrder=false;productiveTrip=cancelledTrip=false;Target=null;
            BeginManeuver(DronePhase.Launching);reason="Bestandsfracht fährt zur Roh-Eisannahme; kein neuer Abbauauftrag.";return true;
        }

        [System.Serializable]
        public sealed class SavedState
        {
            public Vector3 position, destination, surfacePoint, surfaceNormal, maneuverStart;
            public Quaternion rotation, maneuverRotation;
            public DronePhase phase;
            public string target, returnReason;
            public float fuel, battery, cargo, speed, burnedWater, legDistance, phaseSeconds, cargoGoal, cargoWaterFraction, finalRawGoal;
            public bool tankOrder, parkingAbove, outboundAbove, productiveTrip, cancelledTrip;
            public bool operational, needsInitialCharge;
            public Vector3[] route;
            public int routeIndex;
            public float[] hardware;
            public DroneCargoProfile.SavedState cargoProfile;
            public bool rawIceDelivery;
            public bool retainedCargoFlight;
            public int iceDeliveryVersion;
            public float unloadInitialCargoKg;
        }
        public SavedState CaptureState() => new SavedState {
            position=transform.position, rotation=transform.rotation, phase=Phase, target=Target == null ? null : Target.name,
            fuel=FuelLiters, battery=BatteryKwh, cargo=CargoKg, speed=Speed, burnedWater=BurnedWaterLiters, tankOrder=HasTankOrder,
            destination=destination, legDistance=legDistance, phaseSeconds=phaseSeconds, cargoGoal=cargoGoal,
            cargoWaterFraction=cargoWaterFraction, finalRawGoal=finalRawGoal, surfacePoint=SurfacePoint, surfaceNormal=SurfaceNormal,
            maneuverStart=maneuverStart, maneuverRotation=maneuverRotation, parkingAbove=parkingAbove, outboundAbove=outboundAbove,
            productiveTrip=productiveTrip, cancelledTrip=cancelledTrip, returnReason=ReturnReason,
            hardware=new[]{DryMassKg,FuelCapacityLiters,BatteryCapacityKwh,CargoCapacityKg,CruiseSpeed,ThrustNewtons,ExhaustSpeed,HeatingKwhPerLiter,MiningRateKgPerSecond,MiningPowerKw,OnboardPowerKw,ReserveFraction},
            cargoProfile=CargoProfile.Capture(),
            rawIceDelivery=rawIceDelivery,retainedCargoFlight=retainedCargoFlight,iceDeliveryVersion=1,unloadInitialCargoKg=unloadInitialCargoKg,
            operational=IsOperational,needsInitialCharge=NeedsInitialCharge,route=route,routeIndex=routeIndex
        };
        public void RestoreState(SavedState state)
        {
            ReleaseWorkingSurface();
            IsOperational=state.operational;NeedsInitialCharge=state.needsInitialCharge;
            var h=state.hardware;
            DryMassKg=h[0];FuelCapacityLiters=h[1];BatteryCapacityKwh=h[2];CargoCapacityKg=h[3];CruiseSpeed=h[4];ThrustNewtons=h[5];
            ExhaustSpeed=h[6];HeatingKwhPerLiter=h[7];MiningRateKgPerSecond=h[8];MiningPowerKw=h[9];OnboardPowerKw=h[10];ReserveFraction=h[11];
            CargoProfile=DroneCargoProfile.Restore(state.cargoProfile);
            // Existing LIVE journeys keep phase, cargo and supplies, but their next
            // physical unloading now preserves both constituents through the plant.
            rawIceDelivery=Scenario.UsesIcePlant&&(state.rawIceDelivery||state.tankOrder||state.cargo>0);
            retainedCargoFlight=state.retainedCargoFlight;
            unloadInitialCargoKg=state.unloadInitialCargoKg;
            if(rawIceDelivery&&state.phase==DronePhase.Unloading&&unloadInitialCargoKg<state.cargo)unloadInitialCargoKg=state.cargo;
            transform.SetPositionAndRotation(state.position,state.rotation); Phase=state.phase;
            Target=System.Array.Find(Scenario.Asteroids,a=>a.name==state.target);
            FuelLiters=state.fuel; BatteryKwh=state.battery; CargoKg=state.cargo; Speed=state.speed;
            BurnedWaterLiters=state.burnedWater; HasTankOrder=state.tankOrder; ReturnReason=state.returnReason;
            destination=state.destination;legDistance=state.legDistance;phaseSeconds=state.phaseSeconds;
            cargoGoal=state.cargoGoal;cargoWaterFraction=state.cargoWaterFraction;finalRawGoal=state.finalRawGoal;
            SurfacePoint=state.surfacePoint;SurfaceNormal=state.surfaceNormal;maneuverStart=state.maneuverStart;
            maneuverRotation=state.maneuverRotation;parkingAbove=state.parkingAbove;outboundAbove=state.outboundAbove;
            productiveTrip=state.productiveTrip;cancelledTrip=state.cancelledTrip;
            route=state.route;routeIndex=state.routeIndex;
            if(Scenario.Drones!=null && System.Array.IndexOf(Scenario.Drones,this)>0 && (Scenario.Operations==null||!Scenario.Operations.CanUseDrone(System.Array.IndexOf(Scenario.Drones,this)))) LockInactive();
            if(Target!=null && (Phase==DronePhase.Launching || Phase==DronePhase.Outbound || Phase==DronePhase.Docking || Phase==DronePhase.Mining || Phase==DronePhase.Undocking)) UseWorkingSurface();
            GetComponent<MiningDroneVisual>()?.RefreshPose();
            GetComponent<TripoMiningDroneVisual>()?.RestoreCargoPresentation();
        }

        internal void LockInactive()
        {
            ReleaseWorkingSurface(); IsOperational=false; NeedsInitialCharge=false;
            HasTankOrder=false; Target=null; Speed=0; Phase=DronePhase.Disabled;
            transform.SetPositionAndRotation(HomePosition,HomeRotation);
            GetComponent<MiningDroneVisual>()?.RefreshPose();
        }
        internal bool StartStationWork()
        {
            if(!IsReady||HasTankOrder||BatteryKwh<EnergyReserveKwh+.35f||FuelLiters<FuelReserveLiters+.25f)return false;
            BatteryKwh-=.35f;FuelLiters-=.25f;Phase=DronePhase.StationWork;Speed=0;return true;
        }
        internal void ResumeStationWork(){Phase=DronePhase.StationWork;}
        internal void SetStationWorkPose(Vector3 position,float seconds){Speed=seconds>0?Vector3.Distance(transform.position,position)/seconds:0;transform.SetPositionAndRotation(position,HomeRotation);}
        internal void FinishStationWork(){transform.SetPositionAndRotation(HomePosition,HomeRotation);Speed=0;Phase=DronePhase.Servicing;}
        internal void ActivateAtDock(){IsOperational=true;NeedsInitialCharge=true;FuelLiters=BatteryKwh=0;Phase=DronePhase.Servicing;}

        internal void RebindHome(Transform berth)
        {
            HomePosition = berth.position; HomeRotation = berth.rotation;
            MigrateStationPose();
        }
        internal void MigrateStationPose()
        {
            // Preserve supplies, cargo, orders and service progress when upgrading a legacy save.
            if (Phase == DronePhase.Ready || Phase == DronePhase.Disabled || Phase == DronePhase.WaitingForPower || Phase == DronePhase.Servicing)
                transform.SetPositionAndRotation(HomePosition, HomeRotation);
            else if (Phase == DronePhase.Parking)
                BeginFlight(parkingAbove?HomeHoldPoint:HomeApproachPoint, DronePhase.Parking);
            else if (Phase == DronePhase.Berthing || Phase == DronePhase.Launching)
            {
                transform.SetPositionAndRotation(HomeApproachPoint, HomeRotation);
                BeginManeuver(Phase);
            }
            else if (Phase == DronePhase.Returning) BeginFlight(TankApproachPoint, DronePhase.Returning);
            else if (Phase == DronePhase.TankDocking || Phase == DronePhase.TankUndocking || Phase == DronePhase.Unloading)
            {
                transform.SetPositionAndRotation(TankDockPoint, TankDockRotation);
                maneuverStart = TankDockPoint; maneuverRotation = TankDockRotation;
            }
            GetComponent<MiningDroneVisual>()?.RefreshPose();
            GetComponent<TripoMiningDroneVisual>()?.Refresh(0);
        }

        public void ResetDrone()
        {
            ReleaseWorkingSurface();
            transform.position = HomePosition;
            transform.rotation = HomeRotation;
            Phase = !IsOperational ? DronePhase.Disabled : NeedsInitialCharge ? DronePhase.WaitingForPower : DronePhase.Ready;
            FuelLiters = IsOperational && !NeedsInitialCharge ? FuelCapacityLiters : 0f;
            BatteryKwh = IsOperational && !NeedsInitialCharge ? BatteryCapacityKwh : 0f;
            CargoKg = Speed = BurnedWaterLiters = phaseSeconds = 0f;
            rawIceDelivery=retainedCargoFlight=false;unloadInitialCargoKg=0;
            HasTankOrder = false;
            Target = null;
            productiveTrip = cancelledTrip = false;
            ReturnReason = "";
            route=null;routeIndex=0;
        }

        private Vector3 ApproachPoint(AsteroidResource source)
        {
            Vector3 towardHome = (HomePosition - source.transform.position).normalized;
            var surface = source.GetComponent<Collider>();
            if (surface != null)
            {
                float reach = Mathf.Max(source.Info.DiameterMeters, surface.bounds.extents.magnitude) * 2f;
                var ray = new Ray(source.transform.position + towardHome * reach, -towardHome);
                if (surface.Raycast(ray, out RaycastHit hit, reach * 2))
                {
                    SurfacePoint = hit.point;
                    SurfaceNormal = hit.normal;
                    return hit.point + hit.normal * 5f;
                }
            }
            SurfaceNormal = towardHome;
            SurfacePoint = source.transform.position + towardHome * source.Info.DiameterMeters * 0.5f;
            return SurfacePoint + towardHome * 5f;
        }

        private void BeginOutbound()
        {
            if (Target == null || !Target.CanMineWater)
            {
                HasTankOrder = false;
                Phase = DronePhase.Ready;
                Scenario.Notify("Das Wasservorkommen ist erschöpft. Wähle eine andere Eisquelle.");
                return;
            }
            cargoWaterFraction = Target.WaterFraction;
            // The final delivery fits the remaining tank space; earlier trips use the full cargo hold.
            finalRawGoal = rawIceDelivery?Mathf.Min(CargoCapacityFor(Target),(float)Scenario.RawReceivingCapacity(cargoWaterFraction))
                : Mathf.Min(CargoCapacityFor(Target), Mathf.Max(0, Scenario.TankCapacityLiters - Scenario.WaterLiters) / cargoWaterFraction);
            cargoGoal = PlannedCargoGoal(Target,rawIceDelivery);
            productiveTrip = cancelledTrip = false;
            ReturnReason = "";
            UseWorkingSurface();
            ApproachPoint(Target);
            BeginManeuver(DronePhase.Launching);
        }

        private float PlannedCargoGoal(AsteroidResource source,bool rawDelivery)
        {
            if (source == null || source.WaterFraction <= 0) return CargoKg;
            float tankLimit = rawDelivery?(float)Scenario.RawReceivingCapacity(source.WaterFraction)
                : Mathf.Max(0, Scenario.TankCapacityLiters - Scenario.WaterLiters) / source.WaterFraction;
            return Mathf.Min(CargoCapacityFor(source), tankLimit, CargoKg + source.RemainingRawKg);
        }

        // Only the working asteroid needs an exact near-surface collider. Restore the shared LOD after mining.
        private void UseWorkingSurface()
        {
            ReleaseWorkingSurface();
            var generator = Target.GetComponent<AsteroidGenerator>();
            var instance = Target.GetComponent<SpiralAsteroid>();
            if (generator == null && instance != null) generator = instance.Template;
            var collider = Target.GetComponent<MeshCollider>();
            if (collider == null || generator == null || generator.BakedMeshes == null || generator.BakedMeshes.Length == 0) return;
            workingCollider = collider; originalColliderMesh = collider.sharedMesh;
            collider.sharedMesh = generator.BakedMeshes[0];
        }

        private void ReleaseWorkingSurface()
        {
            if (workingCollider != null) workingCollider.sharedMesh = originalColliderMesh;
            workingCollider = null; originalColliderMesh = null;
        }

        private void BeginFlight(Vector3 point, DronePhase phase)
        {
            bool vertical=(point-HomeHoldPoint).sqrMagnitude<.01f && (transform.position-HomeApproachPoint).sqrMagnitude<.01f
                || (point-HomeApproachPoint).sqrMagnitude<.01f && (transform.position-HomeHoldPoint).sqrMagnitude<.01f;
            route=navigator!=null && !vertical?navigator.Plan(transform.position,point):new[]{point}; routeIndex=0;
            StartLeg(route[0],phase);
        }
        private void StartLeg(Vector3 point,DronePhase phase)
        {
            destination = point;
            legDistance = Vector3.Distance(transform.position, point);
            Speed = 0f;
            phaseSeconds = 0f;
            Phase = phase;
        }

        public void Tick(float seconds)
        {
            if (!IsOperational || Phase == DronePhase.Disabled || Phase == DronePhase.Stranded) return;
            if(Phase==DronePhase.Unloading&&!rawIceDelivery&&Scenario.Operations!=null&&Scenario.Operations.Live&&!Scenario.Operations.WaterConnected(7))return;
            // Check before spending another waiting timestep. The route starts at
            // this receiver, not at the asteroid used for the earlier sortie.
            if(rawIceDelivery&&Phase==DronePhase.Unloading&&CargoKg>0&&phaseSeconds>=unloadStart
                &&Scenario.RawReceivingCapacity(cargoWaterFraction)<=0&&DockWaitReserveReached(seconds))
                BeginLoadedDockReturn("Annahme blockiert: Rückkehrreserve erreicht. Restladung bleibt an Bord.");
            if (IsFlying || IsManeuver || Phase == DronePhase.Unloading)
            {
                float demand = OnboardPowerKw * seconds / 3600f;
                if (BatteryKwh < demand) { Strand(); return; }
                BatteryKwh -= demand;
            }
            switch (Phase)
            {
                case DronePhase.Launching:
                    phaseSeconds += seconds;
                    transform.rotation = HomeRotation;
                    transform.position=launchLift>0?(phaseSeconds<35?Vector3.Lerp(maneuverStart,HomePosition+Vector3.up*launchLift,Mathf.SmoothStep(0,1,phaseSeconds/35)):
                        Vector3.Lerp(HomePosition+Vector3.up*launchLift,HomeApproachPoint,Mathf.SmoothStep(0,1,(phaseSeconds-35)/85))):Vector3.Lerp(maneuverStart,HomeApproachPoint,Mathf.SmoothStep(0,1,phaseSeconds/120));
                    if (phaseSeconds >= 120f) { outboundAbove = true; BeginFlight(HomeHoldPoint, DronePhase.Outbound); }
                    break;
                case DronePhase.TankDocking:
                    phaseSeconds += seconds;
                    transform.rotation = Quaternion.Slerp(maneuverRotation,TankDockRotation,Mathf.SmoothStep(0,1,phaseSeconds/45f));
                    transform.position = Vector3.Lerp(maneuverStart,TankDockPoint,Mathf.SmoothStep(0,1,Mathf.Clamp01((phaseSeconds-45f)/75f)));
                    if (phaseSeconds >= 120f) { transform.rotation = TankDockRotation; BeginManeuver(DronePhase.Unloading); }
                    break;
                case DronePhase.TankUndocking:
                    phaseSeconds += seconds;
                    transform.position = Vector3.Lerp(maneuverStart,TankApproachPoint,Mathf.SmoothStep(0,1,Mathf.Clamp01((phaseSeconds-30f)/90f)));
                    if (phaseSeconds >= 120f) { parkingAbove = true; BeginFlight(HomeHoldPoint, DronePhase.Parking); }
                    break;
                case DronePhase.Berthing:
                    phaseSeconds += seconds;
                    transform.rotation = Quaternion.Slerp(maneuverRotation,HomeRotation,Mathf.SmoothStep(0,1,phaseSeconds/45f));
                    transform.position=launchLift>0?(phaseSeconds<100?Vector3.Lerp(maneuverStart,HomePosition+Vector3.up*launchLift,Mathf.SmoothStep(0,1,Mathf.Clamp01((phaseSeconds-45)/55))):
                        Vector3.Lerp(HomePosition+Vector3.up*launchLift,HomePosition,Mathf.SmoothStep(0,1,(phaseSeconds-100)/20))):Vector3.Lerp(maneuverStart,HomePosition,Mathf.SmoothStep(0,1,Mathf.Clamp01((phaseSeconds-45)/75)));
                    if (phaseSeconds >= 120f) {
                        transform.rotation = HomeRotation;
                        if (productiveTrip && !cancelledTrip&&!retainedCargoFlight) Scenario.Mining.CompleteTrip();
                        productiveTrip = false;
                        retainedCargoFlight=false;
                        Phase = !rawIceDelivery&&Scenario.QuestComplete ? DronePhase.Ready : DronePhase.Servicing;
                    }
                    break;
                case DronePhase.Docking:
                    phaseSeconds += seconds;
                    float docking = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(phaseSeconds / 60f));
                    transform.position = Vector3.Lerp(maneuverStart, SurfacePoint + SurfaceNormal * miningStandOff
                        - Facing(-SurfaceNormal) * miningAxisOffset, docking);
                    transform.rotation = Quaternion.Slerp(maneuverRotation, Facing(-SurfaceNormal), docking);
                    if (phaseSeconds >= 120f) BeginManeuver(DronePhase.Mining);
                    break;
                case DronePhase.Undocking:
                    phaseSeconds += seconds;
                    transform.position = Vector3.Lerp(maneuverStart, SurfacePoint + SurfaceNormal * 5f,
                        Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((phaseSeconds - 60f) / 60f)));
                    if (phaseSeconds >= 120f) { ReleaseWorkingSurface(); BeginManeuver(DronePhase.Turning); }
                    break;
                case DronePhase.Turning:
                    phaseSeconds += seconds;
                    transform.rotation = Quaternion.Slerp(maneuverRotation, Facing(TankApproachPoint - transform.position),
                        Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(phaseSeconds / 120f)));
                    if (phaseSeconds >= 120f) BeginFlight(TankApproachPoint, DronePhase.Returning);
                    break;
                case DronePhase.Outbound:
                case DronePhase.Returning:
                case DronePhase.Parking:
                    Fly(seconds);
                    break;
                case DronePhase.Mining:
                    MineWithinBudget(seconds);
                    break;
                case DronePhase.Unloading:
                    phaseSeconds += seconds;
                    transform.rotation = TankDockRotation;
                    if(rawIceDelivery&&CargoKg>0){
                        if(Vector3.Distance(CargoOutletPoint,TankInlet)>.025f){ReturnReason="Rohannahme wartet auf geschlossene Kupplung";break;}
                        if(phaseSeconds>220f){phaseSeconds=unloadEnd;break;}
                        float activeSeconds=Mathf.Max(0,Mathf.Min(seconds,phaseSeconds-unloadStart));
                        double requested=(double)unloadInitialCargoKg/Mathf.Max(1,unloadEnd-unloadStart)*activeSeconds;
                        // Backpressure is a physical capacity constraint. The exact
                        // transfer can be slightly smaller solely because CargoKg is
                        // a float; that rounding is not a blocked receiving machine.
                        double receivingSpace=Scenario.RawReceivingCapacity(cargoWaterFraction);
                        bool backpressure=receivingSpace+1e-7<System.Math.Min(CargoKg,requested);
                        double rawAccepted=Scenario.DepositRawIce(CargoKg,requested,cargoWaterFraction,out float remaining);
                        if(rawAccepted>0){if(!productiveTrip)Scenario.RecordRawDelivery();productiveTrip=true;CargoKg=remaining;}
                        if(CargoKg>0){
                            phaseSeconds=Mathf.Min(phaseSeconds,unloadEnd);
                            if(requested>0&&backpressure&&DockWaitReserveReached(seconds))
                                BeginLoadedDockReturn("Annahme zu langsam: Rückkehrreserve erreicht. Restladung bleibt an Bord.");
                            else ReturnReason=rawAccepted<=0?"Eisannahme voll / Restladung bleibt an Bord":"Roh-Eis wird geschlossen übergeben";
                            break;
                        }
                    }
                    if (phaseSeconds >= 240f)
                    {
                        if(rawIceDelivery){
                            HasTankOrder=false;BeginManeuver(DronePhase.TankUndocking);break;
                        }
                        float accepted = Scenario.DepositWater(CargoWaterLiters);
                        productiveTrip = accepted > .001f;
                        CargoKg = Mathf.Max(0f, CargoKg - accepted / Mathf.Max(0.001f, cargoWaterFraction));
                        if (Scenario.QuestComplete) HasTankOrder = false;
                        BeginManeuver(DronePhase.TankUndocking);
                    }
                    break;
                case DronePhase.Servicing:
                    if(Scenario.Operations==null||!Scenario.Operations.Live||Scenario.Operations.WaterConnected(1))FuelLiters += Scenario.TakeFuelWater(Mathf.Min(FuelCapacityLiters - FuelLiters, seconds));
                    BatteryKwh = Mathf.Min(BatteryCapacityKwh, BatteryKwh + Scenario.AllocatedChargeKw * seconds / 3600f);
                    if (FuelLiters >= FuelCapacityLiters - 0.001f && BatteryKwh >= BatteryCapacityKwh - 0.001f)
                    {
                        Phase = DronePhase.Ready;
                        if (HasTankOrder)
                        {
                            if (CanCompleteTrip(Target, out string reason)) BeginOutbound();
                            else { HasTankOrder = false; Scenario.Notify(reason); }
                        }
                    }
                    else if (Scenario.WaterLiters <= 0f && FuelLiters < FuelCapacityLiters - 0.001f)
                    {
                        HasTankOrder = false;
                        Phase = DronePhase.Ready;
                        Scenario.Notify("Kein Wasser zum Nachtanken verfügbar.");
                    }
                    break;
            }
        }

        private struct Budget { public float Fuel, Energy; }

        // Conservative full-mass estimates include every accelerate/brake leg and onboard power.
        private Budget FlightBudget(float cargo, float maneuverSeconds, params Vector3[] points)
        {
            float mass = DryMassKg + FuelCapacityLiters + cargo;
            float acceleration = Mathf.Max(.0001f, ThrustNewtons / mass);
            float deltaV = 0f, duration = maneuverSeconds;
            for (int i = 1; i < points.Length; i++) {
                bool vertical=(points[i]-points[i-1]).sqrMagnitude>0 && Vector3.ProjectOnPlane(points[i]-points[i-1],Vector3.up).sqrMagnitude<.01f;
                var path=navigator!=null && !vertical?navigator.Plan(points[i-1],points[i]):new[]{points[i]};
                Vector3 from=points[i-1];
                foreach(var point in path) {
                    float distance = Vector3.Distance(from,point);
                    float peak = Mathf.Min(CruiseSpeed, Mathf.Sqrt(acceleration * distance));
                    deltaV += 2f * peak;
                    duration += 2f * peak / acceleration + Mathf.Max(0f, distance - peak * peak / acceleration) / Mathf.Max(.001f, peak); from=point;
                }
            }
            float fuel = mass * (1f - Mathf.Exp(-deltaV / ExhaustSpeed)) * 1.15f;
            return new Budget { Fuel = fuel, Energy = fuel * HeatingKwhPerLiter + duration * OnboardPowerKw / 3600f };
        }

        private Budget ReturnBudget(float cargo) => ReturnBudget(cargo, cargoWaterFraction,rawIceDelivery);
        private Budget DockReturnBudget(float cargo)=>FlightBudget(cargo,240f,TankApproachPoint,HomeHoldPoint,HomeApproachPoint);
        private bool DockWaitReserveReached(float seconds)
        {
            Budget home=DockReturnBudget(CargoKg);
            float nextWait=OnboardPowerKw*Mathf.Max(.25f,seconds)/3600f;
            return BatteryKwh<=home.Energy+EnergyReserveKwh+nextWait+.001f
                ||FuelLiters<=home.Fuel+FuelReserveLiters+.0001f;
        }
        private void BeginLoadedDockReturn(string reason)
        {
            HasTankOrder=false;cancelledTrip=true;ReturnReason=reason;Scenario.Notify(reason);
            BeginManeuver(DronePhase.TankUndocking);
        }
        private Budget ReturnBudget(float cargo, float waterFraction,bool rawDelivery)
        {
            // Only the approach to the receiving tank carries the collected load.
            // If the tank cannot take everything, keep the remainder on the home legs.
            float available = Mathf.Max(0, Scenario.TankCapacityLiters - Scenario.WaterLiters);
            float deliveredRaw = rawDelivery?Mathf.Min(cargo,(float)Scenario.RawReceivingCapacity(waterFraction))
                : waterFraction > 0 ? Mathf.Min(cargo, available / waterFraction) : 0;
            Budget loaded = FlightBudget(cargo, navigator != null ? 690f : 600f,
                SurfacePoint + SurfaceNormal * 5f, TankApproachPoint);
            Budget parking = FlightBudget(Mathf.Max(0, cargo - deliveredRaw), 240f,
                TankApproachPoint, HomeHoldPoint, HomeApproachPoint);
            return new Budget { Fuel = loaded.Fuel + parking.Fuel, Energy = loaded.Energy + parking.Energy };
        }

        private void MineWithinBudget(float seconds)
        {
            float planned = Mathf.Min(EffectiveMiningRate * seconds, cargoGoal - CargoKg, Target.RemainingRawKg);
            float power = EffectiveMiningPower + OnboardPowerKw;
            bool Fits(float kg) {
                Budget returning = ReturnBudget(CargoKg + kg);
                return BatteryKwh >= returning.Energy + EnergyReserveKwh + kg / EffectiveMiningRate * power / 3600f
                    && FuelLiters >= returning.Fuel + FuelReserveLiters;
            }
            bool limited = !Fits(planned);
            if (limited) {
                float lo = 0, hi = planned;
                for (int i = 0; i < 16; i++) { float mid = (lo + hi) * .5f; if (Fits(mid)) lo = mid; else hi = mid; }
                planned = lo;
            }
            float mined = Target.Mine(planned);
            float workingSeconds = mined / EffectiveMiningRate;
            phaseSeconds += workingSeconds;
            BatteryKwh = Mathf.Max(0, BatteryKwh - workingSeconds * power / 3600f);
            CargoKg += mined;
            if (limited || CargoKg >= cargoGoal - .001f || !Target.CanMineWater) {
                ReturnReason = limited ? "Rückkehrversorgung und Reserve erreicht" : !Target.CanMineWater ? "Quelle erschöpft" : "Ladeziel erreicht";
                Scenario.Notify(ReturnReason + ". Drohne kehrt zum Tank zurück.");
                BeginManeuver(DronePhase.Undocking);
            }
        }

        private void Fly(float seconds)
        {
            bool dockVertical=navigator!=null && (Phase==DronePhase.Outbound && outboundAbove || Phase==DronePhase.Parking && !parkingAbove);
            if(dockVertical && Quaternion.Angle(transform.rotation,HomeRotation)>.1f)
            { transform.rotation=Quaternion.RotateTowards(transform.rotation,HomeRotation,seconds*2); return; }
            Vector3 offset = destination - transform.position;
            float remaining = offset.magnitude;
            float acceleration = ThrustNewtons / (DryMassKg + FuelLiters + CargoKg);
            float wantedSpeed = Mathf.Min(CruiseSpeed, Mathf.Sqrt(2f * acceleration * remaining));
            float nextSpeed = Mathf.MoveTowards(Speed, wantedSpeed, acceleration * seconds);
            float travel = (Speed + nextSpeed) * 0.5f * seconds;
            bool arrived = remaining <= Mathf.Max(travel, 0.05f);
            float deltaV = Mathf.Abs(nextSpeed - Speed) + (arrived ? nextSpeed : 0f);
            if (!Burn(deltaV)) { Strand(); return; }
            Speed = nextSpeed;
            if(dockVertical) transform.rotation=HomeRotation;
            else if (offset.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Facing(offset), seconds * 2f);
            transform.position += remaining > 0.0001f ? offset.normalized * Mathf.Min(travel, remaining) : Vector3.zero;
            if (arrived)
            {
                transform.position = destination;
                Speed = 0f;
                phaseSeconds = 0f;
                if(route!=null && routeIndex+1<route.Length) { StartLeg(route[++routeIndex],Phase); return; }
                if (Phase == DronePhase.Outbound) {
                    if(retainedCargoFlight){outboundAbove=false;BeginFlight(TankApproachPoint,DronePhase.Returning);}
                    else if (outboundAbove) { outboundAbove = false; BeginFlight(ApproachPoint(Target),DronePhase.Outbound); }
                    else BeginManeuver(DronePhase.Docking);
                }
                else if (Phase == DronePhase.Parking)
                {
                    if (parkingAbove) { parkingAbove = false; BeginFlight(HomeApproachPoint, DronePhase.Parking); }
                    else BeginManeuver(DronePhase.Berthing);
                }
                else BeginManeuver(DronePhase.TankDocking);
            }
        }

        private bool Burn(float deltaV)
        {
            float fuel = (DryMassKg + CargoKg + FuelLiters) * (1f - Mathf.Exp(-deltaV / ExhaustSpeed));
            float energy = fuel * HeatingKwhPerLiter;
            if (FuelLiters < fuel || BatteryKwh < energy) return false;
            FuelLiters -= fuel;
            BatteryKwh -= energy;
            BurnedWaterLiters += fuel;
            return true;
        }

        public void ReturnToShip()
        {
            if (!IsOperational || (!HasTankOrder&&!retainedCargoFlight)) return;
            if(rawIceDelivery&&Phase==DronePhase.Unloading){
                Budget home=DockReturnBudget(CargoKg);
                if(BatteryKwh<home.Energy+EnergyReserveKwh||FuelLiters<home.Fuel+FuelReserveLiters){
                    ReturnReason="Beladener Heimflug samt Reserve noch nicht gesichert; weiter entladen statt unsicher abdocken.";
                    Scenario.Notify(ReturnReason);return;
                }
                BeginLoadedDockReturn("Auftrag beendet. Rückflug mit tatsächlicher Restladung und geschützter Reserve.");return;
            }
            HasTankOrder = false;
            cancelledTrip = true;
            Scenario.Notify("Auftrag beendet. Drohne kehrt mit ihrer Ladung zurück.");
            if (IsFlying)
            {
                if (!Burn(Speed)) { Strand(); return; }
                BeginManeuver(DronePhase.Turning);
            }
            else if (Phase == DronePhase.Launching) { parkingAbove = false; BeginFlight(HomeApproachPoint,DronePhase.Parking); }
            else if (Phase == DronePhase.Mining || Phase == DronePhase.Docking) BeginManeuver(DronePhase.Undocking);
        }

        private void BeginManeuver(DronePhase phase)
        {
            maneuverStart = transform.position;
            maneuverRotation = transform.rotation;
            phaseSeconds = Speed = 0f;
            Phase = phase;
            if(phase==DronePhase.Unloading)unloadInitialCargoKg=CargoKg;
        }

        private static Quaternion Facing(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.0001f) return Quaternion.identity;
            return Quaternion.LookRotation(direction, Mathf.Abs(Vector3.Dot(direction.normalized, Vector3.up)) > 0.98f ? Vector3.right : Vector3.up);
        }

        private void Strand()
        {
            ReleaseWorkingSurface();
            Phase = DronePhase.Stranded;
            HasTankOrder = false;
            Scenario.Notify("Drohne 01 braucht Hilfe: Energie oder Treibwasser erschöpft.");
        }
    }
}
