using UnityEngine;

namespace SpaceMiner
{
    // Immutable observations only: no reservoir, simulation clock or saved state.
    public readonly struct StationGeneratorView
    {
        public string Id { get; }
        public string Name { get; }
        public float NominalKw { get; }
        public float Condition { get; }
        public bool Connected { get; }
        public bool OrientationKnown { get; }
        public float Incidence01 { get; }
        public float AngleDegrees { get; }
        public Vector3 PanelNormal { get; }
        public Vector3 SunDirection { get; }
        public float PotentialKw { get; }
        public float OutputKw { get; }
        public SolarFieldView SolarDetails {get;}

        private StationGeneratorView(string id, string name, float nominalKw, float condition,
            bool connected, bool orientationKnown, float incidence, float angleDegrees,
            Vector3 panelNormal, Vector3 sunDirection, float potentialKw, SolarFieldView solarDetails=default)
        {
            Id = id; Name = name; NominalKw = nominalKw; Condition = condition;
            Connected = connected; OrientationKnown = orientationKnown;
            Incidence01 = incidence; AngleDegrees = angleDegrees;
            PanelNormal = panelNormal; SunDirection = sunDirection;
            PotentialKw = potentialKw; OutputKw = connected ? potentialKw : 0;
            SolarDetails=solarDetails;
        }

        public static StationGeneratorView Reactor(string id, string name, float nominalKw,
            float condition, bool connected)
        {
            float nominal = NonNegative(nominalKw), health = Mathf.Clamp01(NonNegative(condition));
            // A reactor has no panel incidence; its orientation fields are unavailable.
            return new StationGeneratorView(id, name, nominal, health, connected, false, 0, 0,
                Vector3.zero, Vector3.zero, nominal * health);
        }

        public static StationGeneratorView Solar(string id, string name, float nominalKw,
            float condition, bool connected, bool orientationKnown, Vector3 panelNormal,
            Vector3 sunDirection)
        {
            float nominal = NonNegative(nominalKw), health = Mathf.Clamp01(NonNegative(condition));
            Vector3 normal = Vector3.zero, sun = Vector3.zero;
            bool known = orientationKnown && Unit(panelNormal, out normal) && Unit(sunDirection, out sun);
            // Keep unknown vectors and angles finite; the flag distinguishes them from a known zero angle.
            if (!known) return new StationGeneratorView(id, name, nominal, health, connected, false,
                0, 0, Vector3.zero, Vector3.zero, 0);
            float dot = Mathf.Clamp(Vector3.Dot(normal, sun), -1, 1);
            float incidence = Mathf.Max(0, dot);
            return new StationGeneratorView(id, name, nominal, health, connected, true,
                incidence, Mathf.Acos(dot) * Mathf.Rad2Deg, normal, sun, nominal * health * incidence);
        }

        private static float NonNegative(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0 : Mathf.Max(0, value);
        public static StationGeneratorView SolarPhysical(string id,string name,float condition,bool connected,Vector3 normal,BinaryStarView stars,SolarFieldView field)
        {
            float unshaded=field.Primary.UnshadedKw+field.Companion.UnshadedKw;
            float healthyBest=field.NominalAreaM2>0?field.BestKw*field.EffectiveAreaM2/field.NominalAreaM2:0;
            return new StationGeneratorView(id,name,field.BestKw,Mathf.Clamp01(condition),connected,field.Known,
                healthyBest>0?Mathf.Clamp01(unshaded/healthyBest):0,field.Primary.AngleDegrees,normal.normalized,
                stars.Primary.Direction,field.AvailableKw,field);
        }
        private static bool Unit(Vector3 vector, out Vector3 result)
        {
            float squared = vector.sqrMagnitude;
            if (float.IsNaN(squared) || float.IsInfinity(squared) || squared < .00000001f)
            { result = Vector3.zero; return false; }
            result = vector / Mathf.Sqrt(squared); return true;
        }
    }

    public readonly struct StationGenerationSnapshot
    {
        public StationGeneratorView Reactor { get; }
        public StationGeneratorView SolarA { get; }
        public StationGeneratorView SolarB { get; }
        public BinaryStarView Stars {get;}
        public float TotalOutputKw => Reactor.OutputKw + SolarA.OutputKw + SolarB.OutputKw;

        public StationGenerationSnapshot(StationGeneratorView reactor, StationGeneratorView solarA,
            StationGeneratorView solarB,BinaryStarView stars=default)
        { Reactor = reactor; SolarA = solarA; SolarB = solarB; Stars=stars; }
    }
}
