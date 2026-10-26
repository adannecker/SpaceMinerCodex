#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using UnityEngine;
using Object=UnityEngine.Object;

namespace SpaceMiner
{
    internal static class BinaryStarPowerChecks
    {
        internal static void Run(StationOperations ops,Action<bool,string> require)
        {
            var stars=BinaryStarEnergy.Observe(Vector3.zero);
            require(stars.Known&&stars.Primary.Id!=stars.Companion.Id,"two independently identified physical stars");
            require(Math.Abs(stars.Primary.DisplayDistanceKm-1875.92794)<.01&&Math.Abs(stars.Companion.DisplayDistanceKm-1830.67008)<.01,"compressed source distances retained");
            require(Math.Abs(stars.Primary.DistanceAu-1.0421822)<.00001&&Math.Abs(stars.Companion.DistanceAu-1.0170389)<.00001,"explicit display-to-physical energy scale");
            require(stars.TotalIrradianceWm2>500&&stars.TotalIrradianceWm2<700,"combined quiet-state bolometric flux below terrestrial solar reference");
            require(stars.Primary.LuminositySolar<1&&stars.Companion.LuminositySolar<1,"both K-dwarf luminosities derived from radius and temperature");
            var near=new StarEnergyView("a","a",5000,.8,new Vector3(0,0,1000),Vector3.zero);
            var far=new StarEnergyView("b","b",5000,.8,new Vector3(0,0,2000),Vector3.zero);
            require(Math.Abs(near.IrradianceWm2/far.IrradianceWm2-4)<1e-8,"inverse-square distance law");
            var doubled=new StarEnergyView("a","a",5000,1.6f,new Vector3(0,0,1000),Vector3.zero);
            require(Math.Abs(doubled.LuminositySolar/near.LuminositySolar-4)<.00001,"radius squared determines luminosity");
            var front=new SolarFieldView(stars,stars.Primary.Direction,375.3464f,386.386f,1,1,1,1);
            var blocked=new SolarFieldView(stars,stars.Primary.Direction,375.3464f,386.386f,1,1,0,1);
            var dark=new SolarFieldView(stars,-(stars.Primary.Direction+stars.Companion.Direction).normalized,375.3464f,386.386f,1,1,1,1);
            require(front.Primary.AvailableKw>0&&front.Companion.AvailableKw>0,"both stars contribute when facing the pair");
            require(blocked.Primary.AvailableKw==0&&blocked.Companion.AvailableKw>0,"obscuring one star leaves the other contribution");
            require(dark.AvailableKw==0,"both stars behind panel givezero");
            require(front.EffectiveAreaM2<=front.GeometricAreaM2,"missing cells not invented by a full-health flag");
            var damaged=new SolarFieldView(stars,stars.Primary.Direction,375.3464f,386.386f,.4f,1,1,1);
            require(Mathf.Abs(damaged.EffectiveAreaM2-386.386f*.4f)<.001f,"electrical damage and missing geometry counted once");
            var off=new SolarFieldView(stars,stars.Primary.Direction,375.3464f,386.386f,0,1,1,1);
            require(off.AvailableKw==0,"zero usable hardware meanszero photovoltaic energy");
            var opposed=new BinaryStarView(near,new StarEnergyView("c","c",5000,.8,new Vector3(0,0,-1000),Vector3.zero));
            float one=(float)(near.IrradianceWm2*386.386*.30*.85/1000);
            require(Mathf.Abs(BinaryStarEnergy.BestFieldKw(opposed,386.386f)-one)<.001f,"opposite stars cannot cancel or illuminate one face simultaneously");
            var bank=new StationPowerSystem{capacityKwh=24,storedKwh=12};bank.Advance(60,100,.5f,0,0);
            require(Mathf.Abs(bank.storedKwh-12.2f)<.0001f&&Mathf.Abs(bank.StorageFlowKw-12)<.0001f,"24kWh bank accepts only0.5C despite100kW generation");
            require(Mathf.Abs(bank.GenerationKw-bank.ConsumptionKw-bank.CurtailedKw-bank.StorageFlowKw)<.0001f,"excess generation explicitly curtailed with conserved energy");
            float stock=bank.storedKwh;bank.Advance(0,100,.5f,0,0);
            require(bank.storedKwh==stock,"zero-duration observations grant no storage energy");
            bank.storedKwh=23.9f;bank.Advance(600,100,.5f,0,0);
            require(bank.storedKwh==24&&bank.CurtailedKw>90,"full bank rejects excess generation without overcharge");
            Scene(ops,require);
            var observed=ops.CaptureGeneration();
            var report=new Observation{primaryFlux=observed.Stars.Primary.IrradianceWm2,companionFlux=observed.Stars.Companion.IrradianceWm2,
                primaryLuminosity=observed.Stars.Primary.LuminositySolar,companionLuminosity=observed.Stars.Companion.LuminositySolar,
                displayPrimaryKm=observed.Stars.Primary.DisplayDistanceKm,displayCompanionKm=observed.Stars.Companion.DisplayDistanceKm,
                primaryAu=observed.Stars.Primary.DistanceAu,companionAu=observed.Stars.Companion.DistanceAu,
                solarA=observed.SolarA.OutputKw,solarB=observed.SolarB.OutputKw,potentialA=observed.SolarA.PotentialKw,potentialB=observed.SolarB.PotentialKw,
                areaA=observed.SolarA.SolarDetails.GeometricAreaM2,areaB=observed.SolarB.SolarDetails.GeometricAreaM2,
                effectiveA=observed.SolarA.SolarDetails.EffectiveAreaM2,effectiveB=observed.SolarB.SolarDetails.EffectiveAreaM2,
                primaryVisibilityA=observed.SolarA.SolarDetails.Primary.Visibility,companionVisibilityA=observed.SolarA.SolarDetails.Companion.Visibility,
                primaryVisibilityB=observed.SolarB.SolarDetails.Primary.Visibility,companionVisibilityB=observed.SolarB.SolarDetails.Companion.Visibility};
            File.WriteAllText("Logs/StationPowerConsole/binary-star-observation.json",JsonUtility.ToJson(report,true));
        }
        [Serializable] private sealed class Observation
        {
            public double primaryFlux,companionFlux,primaryLuminosity,companionLuminosity,displayPrimaryKm,displayCompanionKm,primaryAu,companionAu;
            public float solarA,solarB,potentialA,potentialB,areaA,areaB,effectiveA,effectiveB,primaryVisibilityA,companionVisibilityA,primaryVisibilityB,companionVisibilityB;
        }
        private static void Scene(StationOperations ops,Action<bool,string> require)
        {
            var scenario=ops.Scenario;
            var original=SaveGameStore.Capture(scenario,"Binary-star original","");
            string state=JsonUtility.ToJson(ops.Capture());
            string selection=ops.PowerSelection;
            var panel=ops.SolarPanelB;Quaternion rotation=panel.rotation;GameObject blocker=null;
            try
            {
                var stars=ops.CaptureGeneration().Stars;
                require(stars.Known,"game dashboard uses same physical star definitions");
                var element=ops.Element("solar-b");var sector=ops.Element("sector-9");
                element.condition=sector.condition=1;sector.power=true;
                panel.rotation=Quaternion.FromToRotation(panel.up,stars.Primary.Direction)*panel.rotation;
                ops.InvalidateSolarSamples();ops.RefreshSupplyState();
                var source=ops.CaptureGeneration().SolarB;
                require(source.SolarDetails.Known&&Mathf.Abs(source.SolarDetails.GeometricAreaM2-375.3464f)<.01f,"actual34 rendered PV tiles measured in metres");
                require(Vector3.Distance(source.PanelNormal,panel.up)<.0001f,"actual panel transform controls physical projection");
                require(source.OutputKw>20,"actual aligned wing supplies substantial physical power");
                require(Mathf.Abs(source.OutputKw-source.SolarDetails.Primary.AvailableKw-source.SolarDetails.Companion.AvailableKw)<.0001f,"panel output equals its two per-star contributions");
                blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.name="Declared solar ray fixture / primary only";
                blocker.transform.SetPositionAndRotation(panel.position+stars.Primary.Direction*300,Quaternion.LookRotation(stars.Primary.Direction));
                blocker.transform.localScale=new Vector3(50,50,1);blocker.GetComponent<Renderer>().enabled=false;
                Physics.SyncTransforms();ops.InvalidateSolarSamples();ops.RefreshSupplyState();
                var shadow=ops.CaptureGeneration().SolarB.SolarDetails;
                require(shadow.Primary.Visibility<.01f&&shadow.Companion.Visibility>.5f,"real physics blocker shadows primary while companion remains visible");
                require(shadow.Primary.AvailableKw==0&&shadow.Companion.AvailableKw>0,"physical shadow affects only the intercepted star");
                Object.DestroyImmediate(blocker);blocker=null;Physics.SyncTransforms();ops.InvalidateSolarSamples();
                sector.power=false;ops.RefreshSupplyState();source=ops.CaptureGeneration().SolarB;
                require(!source.Connected&&source.OutputKw==0&&source.PotentialKw>0,"sector disconnection leaves observable solar potential but no supply");
                sector.power=true;
                float[] gain=new float[2];
                for(int i=0;i<2;i++)
                {
                    panel.rotation=Quaternion.FromToRotation(panel.up,i==0?stars.Primary.Direction:-(stars.Primary.Direction+stars.Companion.Direction).normalized)*panel.rotation;
                    ops.State.power.storedKwh=4;ops.InvalidateSolarSamples();ops.RefreshSupplyState();
                    scenario.Advance(60);gain[i]=ops.State.power.storedKwh-4;
                    require(gain[i]<=.2001f&&ops.State.power.storedKwh>=0,"actual60-second campaign respects battery power and energy limits / "+i);
                }
                require(gain[0]>gain[1]+.05f,"real campaign gains more energy from an aligned than a reversed panel");
                require(ops.SelectPowerItem("sun-primary")&&ops.SelectPowerItem("sun-companion"),"both stars selectable in shared power interface");
            }
            finally
            {
                if(blocker!=null)Object.DestroyImmediate(blocker);
                panel.rotation=rotation;SaveGameStore.Apply(original,scenario);
                scenario.Scanner.DismissDialogue();ops.ClosePanel();ops.InvalidateSolarSamples();ops.RefreshSupplyState();
                ops.SelectPowerItem(selection);
            }
            require(Quaternion.Angle(panel.rotation,rotation)<.001f&&JsonUtility.ToJson(ops.Capture())==state,"physical solar fixtures restore original geometry, resources and campaign exactly");
        }
    }
}
#endif
