using System;
using UnityEngine;

namespace SpaceMiner
{
    // Physical energy coordinates are independent of the deliberately compressed sky.
    // Local station geometry remains in metres; rendering light intensities are never watts.
    public readonly struct StarEnergyView
    {
        public readonly string Id, Name;
        public readonly double TemperatureK, RadiusSolar, LuminositySolar, DisplayDistanceKm, DistanceAu, IrradianceWm2;
        public readonly Vector3 Direction;
        internal StarEnergyView(string id, string name, double temperature, double radius, Vector3 displayPosition, Vector3 observerMeters)
        {
            Id=id;Name=name;TemperatureK=temperature;RadiusSolar=radius;
            LuminositySolar=radius*radius*Math.Pow(temperature/5772.0,4);
            double x=displayPosition.x*BinaryStarEnergy.MetresPerDisplayKm-observerMeters.x;
            double y=displayPosition.y*BinaryStarEnergy.MetresPerDisplayKm-observerMeters.y;
            double z=displayPosition.z*BinaryStarEnergy.MetresPerDisplayKm-observerMeters.z;
            double distance=Math.Sqrt(x*x+y*y+z*z);
            Direction=new Vector3((float)(x/distance),(float)(y/distance),(float)(z/distance));
            DistanceAu=distance/BinaryStarEnergy.AuMetres;
            DisplayDistanceKm=distance/BinaryStarEnergy.MetresPerDisplayKm;
            IrradianceWm2=1361.0*LuminositySolar/(DistanceAu*DistanceAu);
        }
        public double LuminosityWatts=>LuminositySolar*3.828e26;
    }
    public readonly struct BinaryStarView
    {
        public readonly StarEnergyView Primary, Companion;
        public bool Known=>Primary.Id!=null;
        public BinaryStarView(StarEnergyView primary,StarEnergyView companion){Primary=primary;Companion=companion;}
        public double TotalIrradianceWm2=>Primary.IrradianceWm2+Companion.IrradianceWm2;
    }
    public readonly struct SolarStarContribution
    {
        public readonly float AngleDegrees, Incidence, Visibility, UnshadedKw, AvailableKw;
        public SolarStarContribution(StarEnergyView star,Vector3 normal,float area,float efficiency,float netFactor,float visibility)
        {
            float projection=Mathf.Clamp(Vector3.Dot(normal.normalized,star.Direction),-1,1);
            AngleDegrees=Mathf.Acos(projection)*Mathf.Rad2Deg;Incidence=Mathf.Max(0,projection);
            Visibility=Mathf.Clamp01(visibility);
            UnshadedKw=(float)(area*efficiency*netFactor*star.IrradianceWm2*Incidence/1000);
            AvailableKw=UnshadedKw*Visibility;
        }
    }
    public readonly struct SolarFieldView
    {
        public readonly bool Known;
        public readonly float GeometricAreaM2, EffectiveAreaM2, NominalAreaM2, Efficiency, NetFactor, BestKw;
        public readonly SolarStarContribution Primary, Companion;
        public SolarFieldView(BinaryStarView stars,Vector3 normal,float geometricArea,float fullArea,float condition,float scale,float primaryVisibility,float companionVisibility)
        {
            Known=stars.Known;GeometricAreaM2=geometricArea;NominalAreaM2=fullArea;
            // Condition is the electrically usable fraction of the original array.
            // min() accounts for missing geometry and damage exactly once.
            EffectiveAreaM2=Mathf.Min(geometricArea,fullArea*Mathf.Clamp01(condition));
            Efficiency=BinaryStarEnergy.CellEfficiency;NetFactor=BinaryStarEnergy.PanelNetFactor;
            Primary=new SolarStarContribution(stars.Primary,normal,EffectiveAreaM2,Efficiency,NetFactor*scale,primaryVisibility);
            Companion=new SolarStarContribution(stars.Companion,normal,EffectiveAreaM2,Efficiency,NetFactor*scale,companionVisibility);
            BestKw=BinaryStarEnergy.BestFieldKw(stars,fullArea)*scale;
        }
        public float AvailableKw=>Primary.AvailableKw+Companion.AvailableKw;
    }
    public static class BinaryStarEnergy
    {
        public const double AuMetres=149597870700.0;
        public static double MetresPerDisplayKm=>AuMetres/RuinedWorld.AstronomicalUnit;
        public const float CellEfficiency=.30f, PanelNetFactor=.85f, FullPanelAreaM2=386.386f;
        public const float LegacyNeutralSolarSetting=.5f;
        public static BinaryStarView Observe(Vector3 observerMeters)=>new BinaryStarView(
            new StarEnergyView("sun-primary","Hauptsonne A",5000,.80,RuinedWorld.SunPosition,observerMeters),
            new StarEnergyView("sun-companion","Begleitstern B",4200,.65,RuinedWorld.CompanionPosition,observerMeters));
        public static float BestFieldKw(BinaryStarView stars,float area)
        {
            var a=stars.Primary;var b=stars.Companion;
            Vector3 weighted=a.Direction*(float)a.IrradianceWm2+b.Direction*(float)b.IrradianceWm2;
            float At(Vector3 n)=>(float)(a.IrradianceWm2*Mathf.Max(0,Vector3.Dot(n,a.Direction))+b.IrradianceWm2*Mathf.Max(0,Vector3.Dot(n,b.Direction)));
            float best=Mathf.Max(At(a.Direction),At(b.Direction));
            if(weighted.sqrMagnitude>1e-8f)best=Mathf.Max(best,At(weighted.normalized));
            return best*area*CellEfficiency*PanelNetFactor/1000;
        }
    }
}
