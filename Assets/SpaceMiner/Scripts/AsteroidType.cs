using System;
using UnityEngine;

namespace SpaceMiner
{
    [CreateAssetMenu(menuName = "Space Miner/Asteroiden/Neuer Typ", fileName = "AsteroidType")]
    public sealed class AsteroidType : ScriptableObject
    {
        public string DisplayName = "Neuer Asteroidentyp";
        public Material SurfaceMaterial;
        public ShapeSettings Shape = new ShapeSettings();
        public SurfaceSettings Surface = new SurfaceSettings();
        [Range(0f, 1f)] public float WaterFraction;

        [Serializable]
        public sealed class ShapeSettings
        {
            public Vector3 AxisScale = new Vector3(1.25f, 0.8f, 0.95f);
            [Range(0f, 0.3f)] public float AxisVariation = 0.12f;
            [Range(0f, 0.8f)] public float LobeStrength = 0.1f;
            [Range(0f, 0.4f)] public float Asymmetry = 0.12f;
            [Range(0f, 0.35f)] public float MacroAmplitude = 0.17f;
            [Range(1f, 6f)] public float MacroFrequency = 2.5f;
            [Range(0f, 0.15f)] public float RidgeAmplitude = 0.055f;
            [Range(3f, 18f)] public float RidgeFrequency = 9f;
            [Range(0f, 0.06f)] public float RubbleAmplitude = 0.018f;
            [Range(0f, 1f)] public float TerraceStrength = 0.28f;
            [Range(0, 20)] public int FracturePlanes = 9;
            [Range(0f, 0.3f)] public float FractureDepth = 0.14f;
            [Range(0, 16)] public int CraterCount = 5;
            public Vector2 CraterRadius = new Vector2(0.13f, 0.38f);
            [Range(0f, 0.3f)] public float CraterDepth = 0.15f;
        }

        [Serializable]
        public sealed class SurfaceLayer
        {
            public Color Color = new Color(0.32f, 0.29f, 0.25f);
            [Range(0f, 1f)] public float Metallic;
            [Range(0f, 1f)] public float Smoothness = 0.15f;
        }

        [Serializable]
        public sealed class SurfaceSettings
        {
            public SurfaceLayer Crust = new SurfaceLayer();
            public SurfaceLayer Exposure = new SurfaceLayer { Color = new Color(0.65f, 0.68f, 0.7f) };
            public SurfaceLayer Accent = new SurfaceLayer { Color = new Color(0.19f, 0.18f, 0.17f) };
            [Range(0, 8)] public int ExposureCount = 2;
            public Vector2 ExposureRadius = new Vector2(0.2f, 0.3f);
            [Range(0f, 0.2f)] public float ExposureRecess = 0.065f;
            [Range(0, 8)] public int AccentCount = 3;
            public Vector2 AccentRadius = new Vector2(0.22f, 0.4f);
            [Range(0f, 1f)] public float DetailStrength = 0.65f;
            [Min(0.25f)] public float DetailSizeMeters = 32f;
            [Range(0f, 1f)] public float BumpStrength = 0.3f;
            [Range(0f, 1f)] public float Faceting = 0.4f;
        }
    }
}
