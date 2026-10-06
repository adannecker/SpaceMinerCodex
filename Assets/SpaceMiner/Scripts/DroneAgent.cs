using UnityEngine;

namespace SpaceMiner
{
    public enum DronePhase { Ready, Outbound, Mining, Returning, Unloading, Servicing, Disabled, Stranded, WaitingForPower, Docking, Undocking, Turning, Parking }

    public sealed class DroneAgent : MonoBehaviour
    {
        public WaterScenario Scenario;
        public bool IsOperational;
        public bool NeedsInitialCharge;
        public Vector3 HomePosition;
        public float DryMassKg = 200f;
        public float FuelCapacityLiters = 10f;
        public float BatteryCapacityKwh = 8f;
        public float CargoCapacityKg = 50f;
        public float CruiseSpeed = 5f;
        public float ThrustNewtons = 5f;
        public float ExhaustSpeed = 1000f;
        public float HeatingKwhPerLiter = 1.1f;
        public float MiningRateKgPerSecond = 0.1f;

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
        public Vector3 TankDockPoint => TankInlet + Vector3.back * 3.6f + Vector3.up * 1.5f;
        public Vector3 TankInlet
        {
            get
            {
                var tank = GameObject.Find("Stranded Ship")?.transform.Find("Station Geometry/Water Ice Tank");
                return tank != null ? tank.position + Vector3.back * 1.65f + Vector3.up * 2.8f : HomePosition + Vector3.forward * 3.6f;
            }
        }
        public SpaceObject Info => GetComponent<SpaceObject>();
        public float MiningSecondsRemaining => Phase == DronePhase.Mining ? Mathf.Max(0f, cargoGoal - CargoKg) / MiningRateKgPerSecond : 0f;
        public float TargetDistance => IsFlying ? Vector3.Distance(transform.position, destination) : 0f;
        public bool IsFlying => Phase == DronePhase.Outbound || Phase == DronePhase.Returning || Phase == DronePhase.Parking;
        public float PhaseProgress => IsFlying ? Mathf.Clamp01(1f - TargetDistance / Mathf.Max(0.01f, legDistance))
            : Phase == DronePhase.Mining ? Mathf.Clamp01(CargoKg / Mathf.Max(0.01f, cargoGoal))
            : Phase == DronePhase.Unloading ? Mathf.Clamp01(phaseSeconds / 240f)
            : Phase == DronePhase.Docking || Phase == DronePhase.Undocking || Phase == DronePhase.Turning ? Mathf.Clamp01(phaseSeconds / 120f)
            : Phase == DronePhase.Servicing ? Mathf.Min(BatteryKwh / BatteryCapacityKwh, FuelLiters / FuelCapacityLiters)
            : Phase == DronePhase.Ready ? 1f : 0f;

        public string Status => Phase == DronePhase.Outbound ? "Anflug"
            : Phase == DronePhase.Docking ? "Klammer andocken / Bohrer ausfahren"
            : Phase == DronePhase.Undocking ? "Werkzeuge einziehen / Abdocken"
            : Phase == DronePhase.Turning ? "Zum Tank ausrichten"
            : Phase == DronePhase.Parking ? "Zum Ladeplatz"
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
            : Phase == DronePhase.Docking || Phase == DronePhase.Undocking || Phase == DronePhase.Turning ? Mathf.Max(0f, 120f - phaseSeconds)
            : Phase == DronePhase.Servicing ? Mathf.Max((BatteryCapacityKwh - BatteryKwh) / Scenario.DroneChargePowerKw * 3600f,
                Scenario.WaterLiters > 0f ? FuelCapacityLiters - FuelLiters : float.PositiveInfinity)
            : 0f;

        public bool CanCompleteTrip(AsteroidResource source, out string reason)
        {
            float estimatedFuel = (DryMassKg + CargoCapacityKg + FuelCapacityLiters)
                * (Mathf.Exp(4f * CruiseSpeed / ExhaustSpeed) - 1f) * 1.15f;
            float distance = Vector3.Distance(HomePosition, ApproachPoint(source));
            float estimatedSeconds = 4f * CruiseSpeed * (DryMassKg + CargoCapacityKg + FuelCapacityLiters) / ThrustNewtons
                + 2f * distance / CruiseSpeed + CargoCapacityKg / MiningRateKgPerSecond + 600f;
            float estimatedEnergy = estimatedFuel * HeatingKwhPerLiter + estimatedSeconds * 0.05f / 3600f
                + CargoCapacityKg / MiningRateKgPerSecond * 0.3f / 3600f;
            if (FuelLiters < estimatedFuel) { reason = "Zu wenig Treibwasser für einen sicheren Hin- und Rückflug."; return false; }
            if (BatteryKwh < estimatedEnergy) { reason = "Batterie zuerst aufladen: Hin- und Rückflug brauchen mehr Energie."; return false; }
            reason = "";
            return true;
        }

        public void Assign(AsteroidResource source)
        {
            Target = source;
            cargoWaterFraction = source.WaterFraction;
            HasTankOrder = true;
            BeginOutbound();
        }

        public void ResetDrone()
        {
            transform.position = HomePosition;
            transform.rotation = Quaternion.identity;
            Phase = !IsOperational ? DronePhase.Disabled : NeedsInitialCharge ? DronePhase.WaitingForPower : DronePhase.Ready;
            FuelLiters = IsOperational && !NeedsInitialCharge ? FuelCapacityLiters : 0f;
            BatteryKwh = IsOperational && !NeedsInitialCharge ? BatteryCapacityKwh : 0f;
            CargoKg = Speed = BurnedWaterLiters = phaseSeconds = 0f;
            HasTankOrder = false;
            Target = null;
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
            BeginFlight(ApproachPoint(Target), DronePhase.Outbound);
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
            if (IsFlying || Phase == DronePhase.Mining)
            {
                float demand = (0.05f + (Phase == DronePhase.Mining ? 0.3f : 0f)) * seconds / 3600f;
                if (BatteryKwh < demand) { Strand(); return; }
                BatteryKwh -= demand;
            }
            switch (Phase)
            {
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
                    if (phaseSeconds >= 120f) BeginManeuver(DronePhase.Turning);
                    break;
                case DronePhase.Turning:
                    phaseSeconds += seconds;
                    transform.rotation = Quaternion.Slerp(maneuverRotation, Facing(TankDockPoint - transform.position),
                        Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(phaseSeconds / 120f)));
                    if (phaseSeconds >= 120f) BeginFlight(TankDockPoint, DronePhase.Returning);
                    break;
                case DronePhase.Outbound:
                case DronePhase.Returning:
                case DronePhase.Parking:
                    Fly(seconds);
                    break;
                case DronePhase.Mining:
                    phaseSeconds += seconds;
                    float mined = Target.Mine(Mathf.Min(MiningRateKgPerSecond * seconds, cargoGoal - CargoKg));
                    CargoKg += mined;
                    if (CargoKg >= cargoGoal - 0.001f || !Target.CanMineWater)
                        BeginManeuver(DronePhase.Undocking);
                    break;
                case DronePhase.Unloading:
                    phaseSeconds += seconds;
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Facing(TankInlet - transform.position), seconds * 2f);
                    if (phaseSeconds >= 240f)
                    {
                        float accepted = Scenario.DepositWater(CargoWaterLiters);
                        CargoKg = Mathf.Max(0f, CargoKg - accepted / Mathf.Max(0.001f, cargoWaterFraction));
                        if (Scenario.QuestComplete) HasTankOrder = false;
                        parkingAbove = true;
                        BeginFlight(HomePosition + Vector3.up * 11f, DronePhase.Parking);
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
                if (Phase == DronePhase.Outbound) BeginManeuver(DronePhase.Docking);
                else if (Phase == DronePhase.Parking)
                {
                    if (parkingAbove) { parkingAbove = false; BeginFlight(HomePosition, DronePhase.Parking); }
                    else Phase = Scenario.QuestComplete ? DronePhase.Ready : DronePhase.Servicing;
                }
                else BeginManeuver(DronePhase.Unloading);
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
            Scenario.Notify("Auftrag beendet. Drohne kehrt mit ihrer Ladung zurück.");
            if (IsFlying)
            {
                if (!Burn(Speed)) { Strand(); return; }
                BeginManeuver(DronePhase.Turning);
            }
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
            Phase = DronePhase.Stranded;
            HasTankOrder = false;
            Scenario.Notify("Drohne 01 braucht Hilfe: Energie oder Treibwasser erschöpft.");
        }
    }
}
