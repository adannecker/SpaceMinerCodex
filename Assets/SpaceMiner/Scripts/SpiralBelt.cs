using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceMiner
{
    [DefaultExecutionOrder(-500)]
    public sealed class SpiralBelt : MonoBehaviour
    {
        public int AsteroidCount = 10000;
        public const int TemplateCount = 100;
        public const float RadialGrowth = 3000f;
        public const float Clearance = 200f;
        public const float MinimumOverviewPixels = 1.8f;
        public int LastVisibleCount { get; private set; }
        public int LastDrawCalls { get; private set; }
        public float OuterRadius { get; private set; }
        public float LastAngle { get; private set; }
        public Bounds FieldBounds { get; private set; }
        public bool IsReady { get; private set; }

        private struct Body
        {
            public Vector3 position;
            public float diameter;
            public Matrix4x4 matrix;
            public int variant;
        }
        private sealed class Batch
        {
            public readonly Matrix4x4[] Matrices = new Matrix4x4[128];
            public int Count;
            public Mesh Mesh;
            public RenderParams Parameters;
            public Bounds Bounds;
        }
        private Body[] bodies;
        private SpaceObject[] identities;
        private Batch[] batches;
        private readonly Plane[] planes = new Plane[6];
        private Camera view;
        public MillionAsteroidCloud Cloud { get; private set; }
        private readonly List<Material> materials = new List<Material>();

        public struct Placement
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public float Diameter, Angle;
        }

        public static Placement[] CreateLayout(int count)
        {
            var result = new Placement[count];
            var random = new System.Random(1000071423);
            float angle = 0.5f, previousDiameter = 420f;
            for (int i = 0; i < count; i++)
            {
                float diameter = i < 3 ? new[] { 100f, 260f, 420f }[i]
                    : i == 11 || i % 1000 == 999 ? 5000f
                    : i % 200 == 199 ? 1800f : Mathf.Lerp(100f, 650f, Mathf.Pow((float)random.NextDouble(), 2f));
                Vector3 position;
                if (i < 3)
                {
                    // Small starter section within the worker drone's existing range.
                    float a = i * 0.25f, r = new[] { 450f, 850f, 1450f }[i];
                    position = new Vector3(Mathf.Sin(a) * r, 0, Mathf.Cos(a) * r);
                }
                else
                {
                    // Walk an Archimedean spiral by arc length, leaving room for both neighbours.
                    // Conservative extra margin covers the chord/arc difference near the centre.
                    float remaining = (previousDiameter + diameter) * 0.5f + Clearance + 80f;
                    while (remaining > 0)
                    {
                        float radius = 1500f + RadialGrowth * angle;
                        float step = Mathf.Min(50f, remaining);
                        angle += step / Mathf.Sqrt(radius * radius + RadialGrowth * RadialGrowth);
                        remaining -= step;
                    }
                    float r = 1500f + RadialGrowth * angle;
                    position = new Vector3(Mathf.Sin(angle) * r,
                        Mathf.Sin(angle * 0.6f) * 450f + ((float)random.NextDouble() - 0.5f) * 120f, Mathf.Cos(angle) * r);
                }
                result[i] = new Placement { Position = position, Diameter = diameter, Angle = angle,
                    Rotation = Quaternion.Euler((float)random.NextDouble() * 360f, (float)random.NextDouble() * 360f, (float)random.NextDouble() * 360f) };
                previousDiameter = diameter;
            }
            return result;
        }

        private void Awake()
        {
            if (AsteroidCount <= 0) return;
            if (!SystemInfo.supportsInstancing) throw new InvalidOperationException("Das Spiralfeld benötigt GPU-Instancing.");
            var root = GameObject.Find("Asteroids (1 unit = 1 metre)").transform;
            if (root.childCount != TemplateCount || AsteroidCount < TemplateCount)
                throw new InvalidOperationException("Das Spiralfeld benötigt die Bibliothek mit 100 Szenenasteroiden.");
            var templates = new AsteroidGenerator[TemplateCount];
            batches = new Batch[TemplateCount * 4];
            for (int i = 0; i < templates.Length; i++)
            {
                templates[i] = root.GetChild(i).GetComponent<AsteroidGenerator>();
                var source = templates[i];
                var material = new Material(source.Type.SurfaceMaterial) { enableInstancing = true };
                materials.Add(material);
                var properties = new MaterialPropertyBlock();
                AsteroidGenerator.ApplySurface(properties, source.Type.Surface);
                for (int level = 0; level < 4; level++)
                    batches[i * 4 + level] = new Batch { Mesh = source.BakedMeshes[level], Parameters = new RenderParams(material) {
                        matProps = properties, receiveShadows = true, lightProbeUsage = LightProbeUsage.Off,
                        shadowCastingMode = level == 3 ? ShadowCastingMode.Off : ShadowCastingMode.On } };
            }
            int physicalCount = AsteroidCount >= 100000 ? TemplateCount : AsteroidCount;
            Placement[] layout = CreateLayout(physicalCount);
            bodies = new Body[physicalCount - TemplateCount];
            identities = new SpaceObject[physicalCount];
            var bounds = new Bounds(Vector3.zero, Vector3.one * 24f);
            for (int i = 0; i < physicalCount; i++)
            {
                Placement placement = layout[i];
                int variant = i % TemplateCount;
                GameObject item;
                if (i < TemplateCount)
                {
                    item = templates[i].gameObject;
                    templates[i].DiameterMeters = placement.Diameter;
                }
                else
                {
                    item = new GameObject("A-" + (i + 1).ToString("00000"));
                    item.transform.SetParent(root, false);
                    var info = item.AddComponent<SpaceObject>();
                    info.DisplayName = item.name;
                    info.DiameterMeters = placement.Diameter;
                    info.Description = templates[variant].Type.DisplayName + " / ungescannt";
                    item.AddComponent<SpiralAsteroid>().Template = templates[variant];
                    item.AddComponent<MeshCollider>().sharedMesh = templates[variant].BakedMeshes[2];
                    bodies[i - TemplateCount] = new Body { position = placement.Position, diameter = placement.Diameter,
                        variant = variant, matrix = Matrix4x4.TRS(placement.Position, placement.Rotation, Vector3.one * placement.Diameter) };
                }
                item.transform.SetPositionAndRotation(placement.Position, placement.Rotation);
                item.transform.localScale = Vector3.one * placement.Diameter;
                identities[i] = item.GetComponent<SpaceObject>();
                bounds.Encapsulate(new Bounds(placement.Position, Vector3.one * placement.Diameter));
            }
            FieldBounds = bounds;
            OuterRadius = new Vector2(layout[layout.Length - 1].Position.x, layout[layout.Length - 1].Position.z).magnitude;
            LastAngle = layout[layout.Length - 1].Angle;
            if (physicalCount != AsteroidCount)
            {
                Cloud = gameObject.AddComponent<MillionAsteroidCloud>();
                Cloud.Initialize(AsteroidCount, templates);
                bounds.Encapsulate(Cloud.Bounds);
                FieldBounds = bounds;
                OuterRadius = MillionAsteroidCloud.Radius;
                LastAngle = Mathf.PI * 7f;
            }
            IsReady = true;
            Debug.Log("SPIRAL BELT READY: " + AsteroidCount + " bodies, " + OuterRadius.ToString("0") + " m outer radius");
        }

        private void LateUpdate()
        {
            if (!IsReady) return;
            if (Cloud != null) { LastVisibleCount = Cloud.Count - TemplateCount; LastDrawCalls = Cloud.PointDrawCalls; return; }
            if (view == null) view = Camera.main;
            GeometryUtility.CalculateFrustumPlanes(view, planes);
            foreach (Batch batch in batches) batch.Count = 0;
            LastVisibleCount = 0;
            LastDrawCalls = 0;
            Vector3 cameraPosition = view.transform.position, forward = view.transform.forward;
            float projection = 0.5f / Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * 0.5f);
            foreach (Body body in bodies)
            {
                float radius = body.diameter * 0.5f;
                bool visible = true;
                for (int plane = 0; plane < 6; plane++)
                    if (planes[plane].GetDistanceToPoint(body.position) < -radius) { visible = false; break; }
                if (!visible) continue;
                float depth = Mathf.Max(0.01f, Vector3.Dot(body.position - cameraPosition, forward));
                float height = body.diameter * projection / depth;
                int lod = height >= 0.30f ? 0 : height >= 0.12f ? 1 : height >= 0.035f ? 2 : 3;
                Batch batch = batches[body.variant * 4 + lod];
                if (batch.Count == batch.Matrices.Length) Submit(batch);
                // Strategic overview marker: keep subpixel bodies visible, while colliders,
                // resource sizes and close views retain their physical metre scale.
                float visualDiameter = Mathf.Max(body.diameter, MinimumOverviewPixels * depth / (view.pixelHeight * projection));
                var matrix = body.matrix;
                float scale = visualDiameter / body.diameter;
                matrix.m00 *= scale; matrix.m01 *= scale; matrix.m02 *= scale;
                matrix.m10 *= scale; matrix.m11 *= scale; matrix.m12 *= scale;
                matrix.m20 *= scale; matrix.m21 *= scale; matrix.m22 *= scale;
                if (batch.Count == 0) batch.Bounds = new Bounds(body.position, Vector3.one * visualDiameter);
                else batch.Bounds.Encapsulate(new Bounds(body.position, Vector3.one * visualDiameter));
                batch.Matrices[batch.Count++] = matrix;
                LastVisibleCount++;
            }
            foreach (Batch batch in batches) if (batch.Count > 0) Submit(batch);
        }

        private void Submit(Batch batch)
        {
            batch.Parameters.worldBounds = batch.Bounds;
            batch.Parameters.camera = view;
            Graphics.RenderMeshInstanced(batch.Parameters, batch.Mesh, 0, batch.Matrices, batch.Count);
            LastDrawCalls++;
            batch.Count = 0;
        }

        public SpaceObject PickOverview(Vector3 mouse, Camera camera)
        {
            if (!IsReady) return null;
            if (Cloud != null) return Cloud.Pick(mouse, camera);
            SpaceObject best = null;
            float nearestSquared = 36f;
            float projection = camera.pixelHeight * 0.5f / Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            foreach (SpaceObject info in identities)
            {
                Vector3 point = camera.WorldToScreenPoint(info.transform.position);
                if (point.z < camera.nearClipPlane || point.z > camera.farClipPlane) continue;
                // This supplements only tiny overview symbols; nearby geometry uses raycasts.
                if (info.DiameterMeters * projection / point.z > 6f) continue;
                float squared = (new Vector2(point.x, point.y) - new Vector2(mouse.x, mouse.y)).sqrMagnitude;
                if (squared < nearestSquared) { nearestSquared = squared; best = info; }
            }
            return best;
        }

        private void OnDestroy()
        {
            foreach (Material material in materials) Destroy(material);
        }
    }
}
