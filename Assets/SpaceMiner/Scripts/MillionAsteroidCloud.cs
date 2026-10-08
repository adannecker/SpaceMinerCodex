using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceMiner
{
    // Stress-test representation: compact positions for every body, point meshes at
    // distance, and the existing template geometry locally. No million GameObjects.
    public sealed class MillionAsteroidCloud : MonoBehaviour
    {
        public struct Body { public Vector3 Position; public float Diameter; }
        public Body[] Bodies { get; private set; }
        public Bounds Bounds { get; private set; }
        public float GenerationSeconds { get; private set; }
        public int PointDrawCalls { get; private set; }
        public int LocalMeshCount { get; private set; }
        public int Count => Bodies == null ? 0 : Bodies.Length + 100;
        public int OuterBodyIndex { get; private set; }
        private readonly List<Mesh> points = new List<Mesh>();
        private readonly Dictionary<Vector3Int, List<int>> cells = new Dictionary<Vector3Int, List<int>>();
        private readonly Dictionary<int, SpaceObject> selected = new Dictionary<int, SpaceObject>();
        private AsteroidGenerator[] templates;
        private Material pointMaterial;
        private Material[] surfaceMaterials;
        private readonly Matrix4x4[] matrices = new Matrix4x4[128];
        private readonly List<int> near = new List<int>();
        private readonly MaterialPropertyBlock[] properties = new MaterialPropertyBlock[100];
        private Camera view;
        private OrbitCamera orbit;
        private const float CellSize = 30000f;
        private const float LocalRange = 60000f;
        public const float Radius = 600000f;
        private Vector3Int Cell(Vector3 p) => new Vector3Int(Mathf.FloorToInt(p.x / CellSize), Mathf.FloorToInt(p.y / CellSize), Mathf.FloorToInt(p.z / CellSize));

        public void Initialize(int count, AsteroidGenerator[] sources)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            templates = sources;
            view = GetComponent<Camera>(); orbit = GetComponent<OrbitCamera>();
            pointMaterial = new Material(Resources.Load<Shader>("MillionCloudPoints"));
            surfaceMaterials = new Material[100];
            for (int i = 0; i < 100; i++)
            {
                surfaceMaterials[i] = new Material(sources[i].Type.SurfaceMaterial) { enableInstancing = true };
                properties[i] = new MaterialPropertyBlock();
                AsteroidGenerator.ApplySurface(properties[i], sources[i].Type.Surface);
            }
            Bodies = new Body[count - 100];
            var random = new System.Random(1000071423);
            Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 40000f);
            const int chunkSize = 16000;
            float outerRadius = 0;
            for (int start = 0; start < Bodies.Length; start += chunkSize)
            {
                int length = Mathf.Min(chunkSize, Bodies.Length - start);
                var vertices = new Vector3[length]; var colors = new Color32[length]; var indices = new int[length];
                for (int j = 0; j < length; j++)
                {
                    int index = start + j;
                    float r = Mathf.Sqrt(Mathf.Lerp(0.0025f, 1f, (float)random.NextDouble())) * Radius;
                    int arm = index % 5;
                    float angle = arm * Mathf.PI * 0.4f + r / Radius * Mathf.PI * 7f + ((float)random.NextDouble() - 0.5f) * 0.42f;
                    var p = new Vector3(Mathf.Sin(angle) * r, ((float)random.NextDouble() - 0.5f) * (10000f + r * 0.25f), Mathf.Cos(angle) * r);
                    float diameter = index % 1000 == 999 ? 5000f : Mathf.Lerp(100f, 650f, Mathf.Pow((float)random.NextDouble(), 2));
                    Bodies[index] = new Body { Position = p, Diameter = diameter };
                    if (r > outerRadius) { outerRadius = r; OuterBodyIndex = index; }
                    vertices[j] = p; indices[j] = j;
                    string type = sources[index % 100].Type.DisplayName;
                    colors[j] = type == "Eisreich" ? new Color32(116, 178, 208, 255) : type == "Metallreich" ? new Color32(166, 151, 125, 255) : new Color32(113, 119, 130, 255);
                    var cell = Cell(p);
                    if (!cells.TryGetValue(cell, out var bucket)) cells[cell] = bucket = new List<int>();
                    bucket.Add(index);
                    bounds.Encapsulate(new Bounds(p, Vector3.one * diameter));
                }
                var mesh = new Mesh { name = "Million cloud " + start, indexFormat = IndexFormat.UInt32 };
                mesh.vertices = vertices; mesh.colors32 = colors;
                mesh.SetIndices(indices, MeshTopology.Points, 0); mesh.RecalculateBounds(); mesh.UploadMeshData(true);
                points.Add(mesh);
            }
            Bounds = bounds;
            GenerationSeconds = (float)watch.Elapsed.TotalSeconds;
            Debug.Log("MILLION CLOUD READY: " + Count + " bodies in " + GenerationSeconds.ToString("0.00") + " seconds; " + points.Count + " point batches");
        }

        private void LateUpdate()
        {
            if (Bodies == null) return;
            var scanner = FindFirstObjectByType<WaterScenario>()?.Scanner;
            if (scanner != null && !scanner.ShowsUnknown) { PointDrawCalls = LocalMeshCount = 0; return; }
            PointDrawCalls = points.Count;
            foreach (Mesh mesh in points) Graphics.DrawMesh(mesh, Matrix4x4.identity, pointMaterial, 0, view, 0, null, ShadowCastingMode.Off, false);
            LocalMeshCount = 0;
            if (orbit.Distance > 150000f) return;
            near.Clear();
            var center = Cell(view.transform.position);
            for (int x = -2; x <= 2; x++) for (int y = -2; y <= 2; y++) for (int z = -2; z <= 2; z++)
                if (cells.TryGetValue(center + new Vector3Int(x,y,z), out var bucket))
                    foreach (int index in bucket)
                        if ((Bodies[index].Position - view.transform.position).sqrMagnitude < LocalRange * LocalRange) near.Add(index);
            for (int variant = 0; variant < 100; variant++)
            {
                int used = 0;
                foreach (int index in near)
                {
                    if (index % 100 != variant) continue;
                    Body body = Bodies[index];
                    float distance = Vector3.Distance(body.Position, view.transform.position);
                    float pixels = body.Diameter * view.pixelHeight / (2f * Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * 0.5f) * Mathf.Max(1f, distance));
                    if (pixels < 3f) continue;
                    matrices[used++] = Matrix4x4.TRS(body.Position, Rotation(index), Vector3.one * body.Diameter);
                    LocalMeshCount++;
                    if (used == matrices.Length) { DrawLocal(variant, used); used = 0; }
                }
                if (used > 0) DrawLocal(variant, used);
            }
        }

        private static Quaternion Rotation(int index) => Quaternion.Euler(index % 360, (index * 17) % 360, (index * 31) % 360);
        private void DrawLocal(int variant, int count)
        {
            Graphics.DrawMeshInstanced(templates[variant].BakedMeshes[2], 0, surfaceMaterials[variant], matrices, count, properties[variant], ShadowCastingMode.Off, true, 0, view, LightProbeUsage.Off);
        }

        public SpaceObject Inspect(int index)
        {
            if (selected.TryGetValue(index, out var info)) return info;
            Body body = Bodies[index]; var source = templates[index % 100];
            var item = new GameObject("A-" + (index + 101).ToString("0000000"));
            item.transform.SetPositionAndRotation(body.Position, Rotation(index)); item.transform.localScale = Vector3.one * body.Diameter;
            info = item.AddComponent<SpaceObject>(); info.DisplayName = item.name; info.DiameterMeters = body.Diameter; info.Description = source.Type.DisplayName + " / ungescannt";
            item.AddComponent<MeshCollider>().sharedMesh = source.BakedMeshes[2];
            var resource = item.AddComponent<AsteroidResource>(); resource.WaterFraction = source.Type.WaterFraction; resource.ResetDeposit();
            selected.Add(index, info); return info;
        }

        public SpaceObject Pick(Vector3 mouse, Camera camera)
        {
            int best = -1; float nearest = float.PositiveInfinity;
            // Picking scans compact data only on a click, never on every frame.
            for (int i = 0; i < Bodies.Length; i++)
            {
                Vector3 point = camera.WorldToScreenPoint(Bodies[i].Position);
                if (point.z < camera.nearClipPlane || point.z > camera.farClipPlane) continue;
                float radius = Mathf.Max(6f, Bodies[i].Diameter * camera.pixelHeight / (4f * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * point.z));
                float dx = point.x - mouse.x, dy = point.y - mouse.y;
                if (dx*dx + dy*dy <= radius*radius && point.z < nearest) { nearest = point.z; best = i; }
            }
            return best < 0 ? null : Inspect(best);
        }

        private void OnDestroy()
        {
            foreach (Mesh mesh in points) Destroy(mesh);
            if (pointMaterial != null) Destroy(pointMaterial);
            if (surfaceMaterials != null) foreach (Material material in surfaceMaterials) Destroy(material);
            foreach (SpaceObject info in selected.Values) if (info != null) Destroy(info.gameObject);
        }
    }
}
