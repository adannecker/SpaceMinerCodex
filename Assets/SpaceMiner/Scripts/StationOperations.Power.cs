using UnityEngine;

namespace SpaceMiner
{
    public sealed partial class StationOperations
    {
        private Transform solarPanelA, solarPanelB;
        private readonly SolarFieldSampler solarSamplerA=new SolarFieldSampler(),solarSamplerB=new SolarFieldSampler();
        internal void InvalidateSolarSamples(){solarSamplerA.Invalidate();solarSamplerB.Invalidate();}
        // Persistent source IDs follow their existing sector nodes: A is at
        // 90 degrees / sector3 (+X), B at270 degrees / sector9 (-X).
        public Transform SolarPanelA => FindSolarPanel(ref solarPanelA, "Solar Wing Right");
        public Transform SolarPanelB => FindSolarPanel(ref solarPanelB, "Solar Wing Left");
        public StationGenerationSnapshot GenerationSnapshot => CaptureGeneration();

        public StationGenerationSnapshot CaptureGeneration()
        {
            var stars=BinaryStarEnergy.Observe(transform.position);
            var reactor = Element("reactor");
            return new StationGenerationSnapshot(
                StationGeneratorView.Reactor("reactor", "Reaktor", Scenario != null ? Scenario.ReactorPowerKw : 0,
                    reactor != null ? reactor.condition : 0, reactor != null && reactor.built && Powered(reactor.sector)),
                CaptureSolar("solar-a", "Solarfeld A", SolarPanelA, stars,solarSamplerA),
                CaptureSolar("solar-b", "Solarfeld B", SolarPanelB, stars,solarSamplerB),stars);
        }

        private StationGeneratorView CaptureSolar(string id, string name, Transform panel, BinaryStarView stars,SolarFieldSampler sampler)
        {
            var element = Element(id);
            float condition=element!=null?element.condition:0;
            // Preserve the existing save field and its relative developer overrides.
            float calibration=Scenario!=null?Mathf.Max(0,Scenario.SolarPowerKw)/BinaryStarEnergy.LegacyNeutralSolarSetting:0;
            var field=sampler.Capture(panel,stars,condition,calibration);
            return StationGeneratorView.SolarPhysical(id,name,condition,element!=null&&element.built&&Powered(element.sector),
                panel!=null?panel.up:Vector3.zero,stars,field);
        }

        private Transform FindSolarPanel(ref Transform cached, string name)
        {
            if (Layout == null) return null;
            if (cached == null || !cached.IsChildOf(Layout.transform)) cached = Layout.transform.Find(name);
            return cached;
        }
    }
}
