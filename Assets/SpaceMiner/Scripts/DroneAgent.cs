using UnityEngine;

namespace SpaceMiner
{
    public enum DronePhase { Ready, Outbound, Mining, Returning, Unloading, Servicing, Disabled, Stranded, WaitingForPower, Docking, Undocking, Turning, Parking, Launching, TankDocking, TankUndocking, Berthing }

    public sealed class DroneAgent : MonoBehaviour
    {
        public WaterScenario Scenario;
        public bool IsOperational;
        public bool NeedsInitialCharge;
        public Vector3 HomePosition;
        public Quaternion HomeRotation = Quaternion.identity;
        public static readonly Vector3 CargoOutletOffset = new Vector3(0,0.53f,-0.92f);
        public float DryMassKg = 200f;
        public float FuelCapacityLiters = 10f;
        public float BatteryCapacityKwh = 8f;
        public float CargoCapacityKg = 50f;
        public float CruiseSpeed = 5f;
        public float ThrustNewtons = 5f;
        public float ExhaustSpeed = 1000f;
        public float HeatingKwhPerLiter = 0.2f;
        public float MiningRateKgPerSecond = 0.1f;
        public float MiningPowerKw = 80f;
        public float OnboardPowerKw = 0.05f;
        [Range(0f, .5f)] public float ReserveFraction = .1f;
        public float EffectiveMiningRate => MiningRateKgPerSecond * Scenario.Mining.RateMultiplier;
        public float EffectiveMiningPower => MiningPowerKw * Scenario.Mining.PowerMultiplier;
        public float EnergyReserveKwh => BatteryCapacityKwh * ReserveFraction;
        public float FuelReserveLiters => FuelCapacityLiters * ReserveFraction;
        public float RequiredReturnEnergyKwh => ReturnBudget(CargoKg).Energy + EnergyReserveKwh;
        public float RequiredReturnFuelLiters => ReturnBudget(CargoKg).Fuel + FuelReserveLiters;
        public string ReturnReason { get; private set; } = "";

        public DronePhase Phase { get; private set; }
        public AsteroidResource Target { get; private set; }
        public float FuelLiters { get; private set; }
        public float BatteryKwh { get; private set; }
        public float CargoKg { get; private set; }
        public float CargoWaterLiters => CargoKg * cargoWaterFraction;
        public float Speed { get; private set; }
        public float BurnedWaterLiters { get; private set; }
        public bool HasTankOrder { get; private set; }
        public bool IsReady => Phase == DronePhase.Ready;
        public float PhaseElapsed => phaseSeconds;
        public Vector3 SurfacePoint { get; private set; }
        public Vector3 SurfaceNormal { get; private set; }
        public Vector3 CargoOutletPoint => transform.TransformPoint(CargoOutletOffset);
        private Transform Socket => Scenario != null ? Scenario.GetComponent<StationVisual>()?.TankSocket : null;
        public Vector3 TankOutward => Socket != null ? Socket.forward : Vector3.back;
        public Quaternion TankDockRotation => Facing(TankOutward);
        public Vector3 TankDockPoint => TankInlet - TankDockRotation * CargoOutletOffset;
        public Vector3 TankApproachPoint => TankDockPoint + TankOutward * 4f + Vector3.up * 0.6f;
        public Vector3 HomeApproachPoint => HomePosition + HomeRotation * Vector3.forward * 1.8f;
        public float UnloadProgress => Phase == DronePhase.Unloading ? Mathf.Clamp01((phaseSeconds-25f)/190f) : 0f;
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
            Vector3 above = HomeApproachPoint + Vector3.up * 8f;
            Budget outbound = FlightBudget(CargoCapacityKg, 240f, HomeApproachPoint, above, approach);
            Budget returning = ReturnBudget(CargoCapacityKg);
            float minimumKg = Mathf.Min(1f, source.RemainingRawKg);
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
            HasTankOrder = true;
            BeginOutbound();
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
            public float[] hardware;
        }
        public SavedState CaptureState() => new SavedState {
            position=transform.position, rotation=transform.rotation, phase=Phase, target=Target == null ? null : Target.name,
            fuel=FuelLiters, battery=BatteryKwh, cargo=CargoKg, speed=Speed, burnedWater=BurnedWaterLiters, tankOrder=HasTankOrder,
            destination=destination, legDistance=legDistance, phaseSeconds=phaseSeconds, cargoGoal=cargoGoal,
            cargoWaterFraction=cargoWaterFraction, finalRawGoal=finalRawGoal, surfacePoint=SurfacePoint, surfaceNormal=SurfaceNormal,
            maneuverStart=maneuverStart, maneuverRotation=maneuverRotation, parkingAbove=parkingAbove, outboundAbove=outboundAbove,
            productiveTrip=productiveTrip, cancelledTrip=cancelledTrip, returnReason=ReturnReason,
            hardware=new[]{DryMassKg,FuelCapacityLiters,BatteryCapacityKwh,CargoCapacityKg,CruiseSpeed,ThrustNewtons,ExhaustSpeed,HeatingKwhPerLiter,MiningRateKgPerSecond,MiningPowerKw,OnboardPowerKw,ReserveFraction},
            operational=IsOperational,needsInitialCharge=NeedsInitialCharge
        };
        public void RestoreState(SavedState state)
        {
            ReleaseWorkingSurface();
            IsOperational=state.operational;NeedsInitialCharge=state.needsInitialCharge;
            var h=state.hardware;
            DryMassKg=h[0];FuelCapacityLiters=h[1];BatteryCapacityKwh=h[2];CargoCapacityKg=h[3];CruiseSpeed=h[4];ThrustNewtons=h[5];
            ExhaustSpeed=h[6];HeatingKwhPerLiter=h[7];MiningRateKgPerSecond=h[8];MiningPowerKw=h[9];OnboardPowerKw=h[10];ReserveFraction=h[11];
            transform.SetPositionAndRotation(state.position,state.rotation); Phase=state.phase;
            Target=System.Array.Find(Scenario.Asteroids,a=>a.name==state.target);
            FuelLiters=state.fuel; BatteryKwh=state.battery; CargoKg=state.cargo; Speed=state.speed;
            BurnedWaterLiters=state.burnedWater; HasTankOrder=state.tankOrder; ReturnReason=state.returnReason;
            destination=state.destination;legDistance=state.legDistance;phaseSeconds=state.phaseSeconds;
            cargoGoal=state.cargoGoal;cargoWaterFraction=state.cargoWaterFraction;finalRawGoal=state.finalRawGoal;
            SurfacePoint=state.surfacePoint;SurfaceNormal=state.surfaceNormal;maneuverStart=state.maneuverStart;
            maneuverRotation=state.maneuverRotation;parkingAbove=state.parkingAbove;outboundAbove=state.outboundAbove;
            productiveTrip=state.productiveTrip;cancelledTrip=state.cancelledTrip;
            if(Target!=null && (Phase==DronePhase.Launching || Phase==DronePhase.Outbound || Phase==DronePhase.Docking || Phase==DronePhase.Mining || Phase==DronePhase.Undocking)) UseWorkingSurface();
            GetComponent<MiningDroneVisual>()?.RefreshPose();
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
            HasTankOrder = false;
            Target = null;
            productiveTrip = cancelledTrip = false;
            ReturnReason = "";
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
            finalRawGoal = Mathf.Min(CargoCapacityKg, (Scenario.TankCapacityLiters - Scenario.WaterLiters) / cargoWaterFraction);
            cargoGoal = Mathf.Min(Target.RemainingRawKg, finalRawGoal);
            productiveTrip = cancelledTrip = false;
            ReturnReason = "";
            UseWorkingSurface();
            ApproachPoint(Target);
            BeginManeuver(DronePhase.Launching);
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
            destination = point;
            legDistance = Vector3.Distance(transform.position, point);
            Speed = 0f;
            phaseSeconds = 0f;
            Phase = phase;
        }

        public void Tick(float seconds)
        {
            if (!IsOperational || Phase == DronePhase.Disabled || Phase == DronePhase.Stranded) return;
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
                    transform.position = Vector3.Lerp(maneuverStart, HomeApproachPoint, Mathf.SmoothStep(0,1,phaseSeconds/120f));
                    if (phaseSeconds >= 120f) { outboundAbove = true; BeginFlight(HomeApproachPoint + Vector3.up * 8f, DronePhase.Outbound); }
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
                    if (phaseSeconds >= 120f) { parkingAbove = true; BeginFlight(HomeApproachPoint + Vector3.up * 8f, DronePhase.Parking); }
                    break;
                case DronePhase.Berthing:
                    phaseSeconds += seconds;
                    transform.rotation = Quaternion.Slerp(maneuverRotation,HomeRotation,Mathf.SmoothStep(0,1,phaseSeconds/45f));
                    transform.position = Vector3.Lerp(maneuverStart,HomePosition,Mathf.SmoothStep(0,1,Mathf.Clamp01((phaseSeconds-45f)/75f)));
                    if (phaseSeconds >= 120f) {
                        transform.rotation = HomeRotation;
                        if (productiveTrip && !cancelledTrip) Scenario.Mining.CompleteTrip();
                        productiveTrip = false;
                        Phase = Scenario.QuestComplete ? DronePhase.Ready : DronePhase.Servicing;
                    }
                    break;
                case DronePhase.Docking:
                    phaseSeconds += seconds;
                    float docking = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(phaseSeconds / 60f));
                    transform.position = Vector3.Lerp(maneuverStart, SurfacePoint + SurfaceNormal * 2.4f, docking);
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
                    if (phaseSeconds >= 240f)
                    {
                        float accepted = Scenario.DepositWater(CargoWaterLiters);
                        productiveTrip = accepted > .001f;
                        CargoKg = Mathf.Max(0f, CargoKg - accepted / Mathf.Max(0.001f, cargoWaterFraction));
                        if (Scenario.QuestComplete) HasTankOrder = false;
                        BeginManeuver(DronePhase.TankUndocking);
                    }
                    break;
                case DronePhase.Servicing:
                    FuelLiters += Scenario.TakeFuelWater(Mathf.Min(FuelCapacityLiters - FuelLiters, seconds));
                    BatteryKwh = Mathf.Min(BatteryCapacityKwh, BatteryKwh + Scenario.DroneChargePowerKw * seconds / 3600f);
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
                float distance = Vector3.Distance(points[i - 1], points[i]);
                float peak = Mathf.Min(CruiseSpeed, Mathf.Sqrt(acceleration * distance));
                deltaV += 2f * peak;
                duration += 2f * peak / acceleration + Mathf.Max(0f, distance - peak * peak / acceleration) / Mathf.Max(.001f, peak);
            }
            float fuel = mass * (1f - Mathf.Exp(-deltaV / ExhaustSpeed)) * 1.15f;
            return new Budget { Fuel = fuel, Energy = fuel * HeatingKwhPerLiter + duration * OnboardPowerKw / 3600f };
        }

        private Budget ReturnBudget(float cargo) => FlightBudget(cargo, 840f,
            SurfacePoint + SurfaceNormal * 5f, TankApproachPoint, HomeApproachPoint + Vector3.up * 8f, HomeApproachPoint);

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
            if (offset.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Facing(offset), seconds * 2f);
            transform.position += remaining > 0.0001f ? offset.normalized * Mathf.Min(travel, remaining) : Vector3.zero;
            if (arrived)
            {
                transform.position = destination;
                Speed = 0f;
                phaseSeconds = 0f;
                if (Phase == DronePhase.Outbound) {
                    if (outboundAbove) { outboundAbove = false; BeginFlight(ApproachPoint(Target),DronePhase.Outbound); }
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
            if (!IsOperational || !HasTankOrder) return;
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
