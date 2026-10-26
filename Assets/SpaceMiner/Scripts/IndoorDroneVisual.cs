using UnityEngine;

namespace SpaceMiner
{
    // Shared authored visual only. Activation, energy and work remain campaign-owned.
    public sealed class IndoorDroneVisual : MonoBehaviour
    {
        public const string Revision = "R03";
        public const float BodyLength = .75f, OverallLength = .783f, Width = .482f, Height = .437103f;
        public Bounds modelBounds;
        public Mesh highBody;
        public Renderer[] indicators;
        private MaterialPropertyBlock block;
        private bool initialized, operational;
        private void Awake() => SetOperational(true);
        public void SetOperational(bool enabled)
        {
            if (initialized && operational == enabled) return;
            initialized = true; operational = enabled;
            block ??= new MaterialPropertyBlock();
            foreach (var indicator in indicators)
            {
                if (indicator == null) continue;
                indicator.GetPropertyBlock(block);
                block.SetColor("_Color", enabled ? indicator.sharedMaterial.color : new Color(.09f, .11f, .12f));
                block.SetColor("_EmissionColor", enabled ? indicator.sharedMaterial.GetColor("_EmissionColor") : Color.black);
                indicator.SetPropertyBlock(block);
            }
        }
    }
}
