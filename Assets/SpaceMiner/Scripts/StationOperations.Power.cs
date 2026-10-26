using UnityEngine;

namespace SpaceMiner
{
    public sealed partial class StationOperations
    {
        private Transform solarPanelA, solarPanelB;
        // Persistent source IDs follow their existing sector nodes: A is at
        // 90 degrees / sector3 (+X), B at270 degrees / sector9 (-X).
        public Transform SolarPanelA => FindSolarPanel(ref solarPanelA, "Solar Wing Right");
        public Transform SolarPanelB => FindSolarPanel(ref solarPanelB, "Solar Wing Left");
        public StationGenerationSnapshot GenerationSnapshot => CaptureGeneration();

        public StationGenerationSnapshot CaptureGeneration()
        {
            var sun = RenderSettings.sun;
            // RuinedWorld.Start installs the real sunlight. Before that point use
            // its same fixed world direction, never an interior or helmet light.
            Vector3 sunDirection = sun != null ? -sun.transform.forward : RuinedWorld.SunPosition.normalized;
            var reactor = Element("reactor");
            return new StationGenerationSnapshot(
                StationGeneratorView.Reactor("reactor", "Reaktor", Scenario != null ? Scenario.ReactorPowerKw : 0,
                    reactor != null ? reactor.condition : 0, reactor != null && reactor.built && Powered(reactor.sector)),
                CaptureSolar("solar-a", "Solarfeld A", SolarPanelA, sunDirection),
                CaptureSolar("solar-b", "Solarfeld B", SolarPanelB, sunDirection));
        }

        private StationGeneratorView CaptureSolar(string id, string name, Transform panel, Vector3 sunDirection)
        {
            var element = Element(id);
            return StationGeneratorView.Solar(id, name, Scenario != null ? Scenario.SolarPowerKw * .5f : 0,
                element != null ? element.condition : 0, element != null && element.built && Powered(element.sector),
                panel != null, panel != null ? panel.up : Vector3.zero, sunDirection);
        }

        private Transform FindSolarPanel(ref Transform cached, string name)
        {
            if (Layout == null) return null;
            if (cached == null || !cached.IsChildOf(Layout.transform)) cached = Layout.transform.Find(name);
            return cached;
        }
    }
}
