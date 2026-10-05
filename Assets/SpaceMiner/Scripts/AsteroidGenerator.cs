using System;
using UnityEngine;

namespace SpaceMiner
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public sealed class AsteroidGenerator : MonoBehaviour
    {
        public AsteroidType Type;
        public int Seed = 71423;
        [Min(1)] public float DiameterMeters = 100;
        [Tooltip("-1 verwendet den Typwert; sonst eigener Ressourcenanteil für diesen Körper.")]
        [Range(-1f, 1f)] public float WaterFractionOverride = -1f;
        [Range(3, 6)] public int HighestSubdivision = 5;
        [Tooltip("Editor-gebackene Meshes. Bei Typ-/Seed-Änderungen neu backen.")]
        public Mesh[] BakedMeshes;
        [SerializeField, HideInInspector] private GameObject[] lodObjects;
        [SerializeField, HideInInspector] private AsteroidType bakedType;
        [SerializeField, HideInInspector] private int bakedSeed, bakedSubdivision;
        private Mesh[] ownedMeshes;

        private void Awake()
        {
            if (Type != null) Generate();
        }

        public void SetBakedMeshes(Mesh[] meshes)
        {
            BakedMeshes = meshes;
            bakedType = Type;
            bakedSeed = Seed;
            bakedSubdivision = HighestSubdivision;
        }

        public void Generate(bool preferBaked = true)
        {
            if (Type == null || Type.SurfaceMaterial == null) throw new InvalidOperationException("Asteroidentyp und Oberflächenmaterial fehlen.");
            if (!(DiameterMeters > 0) || float.IsInfinity(DiameterMeters)) throw new ArgumentOutOfRangeException(nameof(DiameterMeters));
            ReleaseOwnedMeshes();
            bool baked = preferBaked && bakedType == Type && bakedSeed == Seed && bakedSubdivision == HighestSubdivision && BakedMeshes != null && BakedMeshes.Length == 4;
            if (baked) foreach (Mesh mesh in BakedMeshes) if (mesh == null) baked = false;
            Mesh[] meshes = baked ? BakedMeshes : (ownedMeshes = AsteroidMeshBuilder.Build(Type, Seed, HighestSubdivision).Levels);
            transform.localScale = Vector3.one * DiameterMeters;
            var renderers = new Renderer[4];
            if (lodObjects == null || lodObjects.Length != 3) lodObjects = new GameObject[3];
            for (int level = 0; level < 4; level++)
            {
                GameObject item = gameObject;
                if (level > 0)
                {
                    item = lodObjects[level-1];
                    if (item == null)
                    {
                        item = new GameObject("Asteroid LOD " + level, typeof(MeshFilter), typeof(MeshRenderer));
                        item.transform.SetParent(transform, false);
                        lodObjects[level-1] = item;
                    }
                    item.transform.localPosition = Vector3.zero;
                    item.transform.localRotation = Quaternion.identity;
                    item.transform.localScale = Vector3.one;
                }
                item.GetComponent<MeshFilter>().sharedMesh = meshes[level];
                var renderer = item.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = Type.SurfaceMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
                var block = new MaterialPropertyBlock();
                ApplySurface(block, Type.Surface);
                block.SetFloat("_BodySize", DiameterMeters);
                renderer.SetPropertyBlock(block);
                renderers[level] = renderer;
            }
            GetComponent<MeshCollider>().sharedMesh = null;
            GetComponent<MeshCollider>().sharedMesh = meshes[2];
            var group = GetComponent<LODGroup>();
            if (group == null) group = gameObject.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.None;
            group.SetLODs(new[] {
                new LOD(0.30f, new[] { renderers[0] }), new LOD(0.12f, new[] { renderers[1] }),
                new LOD(0.035f, new[] { renderers[2] }), new LOD(0.000001f, new[] { renderers[3] }) });
            group.RecalculateBounds();
            var info = GetComponent<SpaceObject>();
            if (info == null) info = gameObject.AddComponent<SpaceObject>();
            if (string.IsNullOrEmpty(info.DisplayName)) info.DisplayName = gameObject.name;
            info.DiameterMeters = DiameterMeters;
            info.Description = Type.DisplayName + " / prozedural";
        }

        public static void ApplySurface(MaterialPropertyBlock block, AsteroidType.SurfaceSettings surface)
        {
            SetLayer(block, "_Crust", surface.Crust);
            SetLayer(block, "_Exposure", surface.Exposure);
            SetLayer(block, "_Accent", surface.Accent);
            block.SetFloat("_DetailStrength", surface.DetailStrength);
            block.SetFloat("_DetailSize", surface.DetailSizeMeters);
            block.SetFloat("_BumpStrength", surface.BumpStrength);
            block.SetFloat("_Faceting", surface.Faceting);
        }

        private static void SetLayer(MaterialPropertyBlock block, string prefix, AsteroidType.SurfaceLayer layer)
        {
            // Property blocks pass raw shader constants; profile swatches are authored in sRGB.
            block.SetColor(prefix + "Color", QualitySettings.activeColorSpace == ColorSpace.Linear ? layer.Color.linear : layer.Color);
            block.SetFloat(prefix + "Metallic", layer.Metallic);
            block.SetFloat(prefix + "Smoothness", layer.Smoothness);
        }

        private void ReleaseOwnedMeshes()
        {
            if (ownedMeshes == null) return;
            foreach (Mesh mesh in ownedMeshes)
                if (mesh != null) { if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh); }
            ownedMeshes = null;
        }

        private void OnDestroy() { ReleaseOwnedMeshes(); }
    }
}
