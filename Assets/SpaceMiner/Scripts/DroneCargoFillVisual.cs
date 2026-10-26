using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceMiner
{
    // A small, deterministic rubble bed. Only the upper partial layer changes height;
    // the finished layers keep their shape while the real cargo volume rises/falls.
    public sealed class DroneCargoFillVisual : MonoBehaviour
    {
        private const int LayerCount = 8, Columns = 4, Rows = 5;
        private readonly Transform[] layers = new Transform[LayerCount];
        private readonly Mesh[] meshes = new Mesh[LayerCount];
        private Bounds space;
        private Material fillMaterial;
        private Texture2D palette;
        public float FillFraction { get; private set; }
        public int VisiblePieces { get; private set; }

        public static DroneCargoFillVisual Create(Transform parent, Bounds interior, Material material)
        {
            var root = new GameObject("Volumetric cargo / packed rubble layers");
            root.transform.SetParent(parent, false);
            var result = root.AddComponent<DroneCargoFillVisual>();
            result.Build(interior, material); return result;
        }
        private void Build(Bounds interior, Material material)
        {
            space = interior;
            Material surface = material;
            if (material.HasProperty("_MainTex"))
            {
                // A four-pixel palette works with the existing included shader;
                // one material still draws the entire layer in one call.
                palette = new Texture2D(4, 1, TextureFormat.RGBA32, false, true)
                    { name = "Cargo rubble / subtle stone tones", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                palette.SetPixels(new[] { new Color(.78f,.78f,.78f,1), new Color(.86f,.86f,.86f,1),
                    new Color(.94f,.94f,.94f,1), Color.white }); palette.Apply(false, true);
                fillMaterial = new Material(material) { name = "Cargo rubble / shared stone palette", mainTexture = palette };
                if (fillMaterial.HasProperty("_Glossiness")) fillMaterial.SetFloat("_Glossiness", .18f);
                surface = fillMaterial;
            }
            float height = space.size.y / LayerCount;
            for (int i = 0; i < LayerCount; i++)
            {
                var layer = new GameObject("Packed ice-regolith / layer " + (i + 1));
                layer.transform.SetParent(transform, false); layers[i] = layer.transform;
                layer.transform.localPosition = new Vector3(space.min.x, space.min.y + i * height, space.min.z);
                meshes[i] = BuildLayer(new Vector3(space.size.x, height, space.size.z), i);
                layer.AddComponent<MeshFilter>().sharedMesh = meshes[i];
                var renderer = layer.AddComponent<MeshRenderer>(); renderer.sharedMaterial = surface;
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                layer.SetActive(false);
            }
        }
        public void SetFill(float fraction)
        {
            FillFraction = float.IsNaN(fraction) || float.IsInfinity(fraction) ? 0 : Mathf.Clamp01(fraction);
            VisiblePieces = 0;
            for (int i = 0; i < LayerCount; i++)
            {
                float height = Mathf.Clamp01(FillFraction * LayerCount - i);
                bool visible = height > .0001f;
                if (layers[i].gameObject.activeSelf != visible) layers[i].gameObject.SetActive(visible);
                layers[i].localScale = new Vector3(1, height, 1);
                if (visible) VisiblePieces += Columns * Rows;
            }
        }
        // These bounds are read from the currently rendered mesh transforms, not
        // reconstructed from a desired percentage, so acceptance can inspect reality.
        public bool TryGetBounds(Transform relative, out Bounds bounds)
        {
            bounds = default; bool found = false;
            for (int i = 0; i < LayerCount; i++)
            {
                if (!layers[i].gameObject.activeSelf) continue;
                Bounds local = meshes[i].bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 p = local.center + Vector3.Scale(local.extents, new Vector3((corner & 1) == 0 ? -1 : 1,
                        (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                    p = relative.InverseTransformPoint(layers[i].TransformPoint(p));
                    if (found) bounds.Encapsulate(p); else { bounds = new Bounds(p, Vector3.zero); found = true; }
                }
            }
            return found;
        }
        private static Mesh BuildLayer(Vector3 size, int seed)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var uv = new List<Vector2>();
            var random = new System.Random(1973 + seed * 271);
            float Next(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
            float cellX = size.x / Columns, cellZ = size.z / Rows;
            var grid = new Vector2[Columns + 1, Rows + 1];
            for (int z = 0; z <= Rows; z++) for (int x = 0; x <= Columns; x++)
                grid[x,z] = new Vector2(x * cellX + (x == 0 || x == Columns ? 0 : Next(-.32f,.32f) * cellX),
                    z * cellZ + (z == 0 || z == Rows ? 0 : Next(-.32f,.32f) * cellZ));
            Vector2 texturePoint = Vector2.zero;
            void Triangle(Vector3 a, Vector3 b, Vector3 c)
            {
                int first = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
                uv.Add(texturePoint); uv.Add(texturePoint); uv.Add(texturePoint);
                triangles.Add(first); triangles.Add(first + 1); triangles.Add(first + 2);
            }
            for (int z = 0; z < Rows; z++) for (int x = 0; x < Columns; x++)
            {
                texturePoint = new Vector2((random.Next(4) + .5f) / 4, .5f);
                var corners = new[] { grid[x,z], grid[x+1,z], grid[x+1,z+1], grid[x,z+1] };
                Vector2 centre = (corners[0] + corners[1] + corners[2] + corners[3]) * .25f;
                var bottom = new Vector3[4];
                var top = new Vector3[4];
                float peakHeight = x + z * Columns == (seed * 7 + 3) % (Columns * Rows) ? 1 : Next(.70f,.99f);
                for (int corner = 0; corner < 4; corner++)
                {
                    Vector2 p = corners[corner];
                    p = new Vector2(Mathf.Clamp(p.x, .003f, size.x - .003f), Mathf.Clamp(p.y, .003f, size.z - .003f));
                    Vector2 inset = Vector2.MoveTowards(p, centre, Next(.001f,.005f));
                    // Keep the outer footprint exactly at its previous safety inset.
                    if (p.x > .003f && p.x < size.x - .003f) p.x = inset.x;
                    if (p.y > .003f && p.y < size.z - .003f) p.y = inset.y;
                    bottom[corner] = new Vector3(p.x, 0, p.y);
                    Vector2 upper = Vector2.Lerp(p, centre, Next(.035f,.19f));
                    top[corner] = new Vector3(upper.x, size.y * peakHeight * Next(.57f,.91f), upper.y);
                }
                Vector3 peak = new Vector3(centre.x + Next(-cellX, cellX) * .19f,
                    size.y * peakHeight, centre.y + Next(-cellZ, cellZ) * .19f);
                Triangle(bottom[0], bottom[1], bottom[2]); Triangle(bottom[0], bottom[2], bottom[3]);
                for (int corner = 0; corner < 4; corner++)
                {
                    int next = (corner + 1) % 4;
                    Triangle(bottom[corner], top[corner], top[next]); Triangle(bottom[corner], top[next], bottom[next]);
                    Triangle(top[corner], peak, top[next]);
                }
            }
            var mesh = new Mesh { name = "Packed rubble / stable low-poly layer " + seed };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.SetUVs(0, uv);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        private void OnDestroy()
        {
            foreach (var mesh in meshes) if (mesh != null) Destroy(mesh);
            if (fillMaterial != null) Destroy(fillMaterial);
            if (palette != null) Destroy(palette);
        }
    }
}
