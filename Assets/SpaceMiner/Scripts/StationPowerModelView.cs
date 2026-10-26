using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SpaceMiner
{
    // Render the actual game's shared meshes/materials, without cloning any behaviours,
    // changing real layers/LOD/lighting, or granting an inventory to a preview object.
    internal sealed class StationPowerModelView : IDisposable
    {
        private sealed class Part
        {
            public Mesh Mesh;
            public MeshRenderer Source;
            public Material[] Materials;
        }
        private readonly List<Part> parts = new List<Part>();
        private readonly Transform reference;
        private readonly GameObject rig;
        private readonly Camera camera;
        private readonly Light key, fill;
        private readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
        private RenderTexture texture;
        private Bounds bounds;
        private int renderedFrame = -1;
        private float yaw, pitch;
        private readonly Vector3 origin = new Vector3(0, -30000, 0);
        public Texture Texture => texture;
        public int MeshCount => parts.Count;
        public int SharedMeshCount { get; private set; }
        public StationPowerModelView(Transform coordinates, IEnumerable<Transform> roots, bool station)
        {
            reference = coordinates; yaw = station ? 0 : -15; pitch = station ? 64 : 18;
            rig = new GameObject("Strompult / isolierte Modellkamera") { hideFlags = HideFlags.HideAndDontSave };
            rig.transform.position = origin;
            camera = rig.AddComponent<Camera>(); camera.enabled = false; camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.012f, .028f, .044f);
            camera.orthographic = true; camera.nearClipPlane = .05f; camera.farClipPlane = 1500;
            camera.allowHDR = false; camera.useOcclusionCulling = false;
            key = MakeLight("Modelllicht", new Vector3(45, -35, 0), 1.3f);
            fill = MakeLight("Modell-Fülllicht", new Vector3(20, 140, 0), .8f);
            var seen = new HashSet<MeshRenderer>(); bool hasBounds = false;
            foreach (var root in roots)
            {
                if (root == null || !root.gameObject.activeInHierarchy) continue;
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(false))
                {
                    if (!seen.Add(renderer) || renderer.GetComponent<TextMesh>() != null) continue;
                    var filter = renderer.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null) continue;
                    var group = renderer.GetComponentInParent<LODGroup>();
                    if (group != null)
                    {
                        var levels = group.GetLODs();
                        if (levels.Length > 0 && Array.IndexOf(levels[0].renderers, renderer) < 0) continue;
                    }
                    if (!renderer.enabled && group == null) continue;
                    parts.Add(new Part { Mesh = filter.sharedMesh, Source = renderer, Materials = renderer.sharedMaterials });
                    var worldBounds = renderer.bounds;
                    for (int corner = 0; corner < 8; corner++)
                    {
                        var sign = new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1);
                        var point = reference.InverseTransformPoint(worldBounds.center + Vector3.Scale(worldBounds.extents, sign));
                        if (!hasBounds) { bounds = new Bounds(point, Vector3.zero); hasBounds = true; } else bounds.Encapsulate(point);
                    }
                }
            }
            SharedMeshCount = parts.Count;
            if (!hasBounds) bounds = new Bounds(Vector3.zero, Vector3.one);
        }
        private Light MakeLight(string name, Vector3 rotation, float intensity)
        {
            var go = new GameObject(name); go.transform.SetParent(rig.transform, false);
            go.transform.localRotation = Quaternion.Euler(rotation);
            var light = go.AddComponent<Light>(); light.type = LightType.Directional; light.cullingMask = 1 << 31;
            light.intensity = intensity; light.shadows = LightShadows.None; light.enabled = false; return light;
        }
        public Vector2 Project(Vector3 worldPoint, Rect viewport)
        {
            var point = camera.WorldToViewportPoint(origin + reference.InverseTransformPoint(worldPoint));
            return new Vector2(viewport.x + point.x * viewport.width, viewport.y + (1 - point.y) * viewport.height);
        }
        public void Orbit(float delta) { yaw += delta; renderedFrame = -1; }
        public void Render(int width, int height)
        {
            width = Mathf.Clamp(width, 128, 1600); height = Mathf.Clamp(height, 128, 1200);
            if (texture == null || texture.width != width || texture.height != height)
            {
                if (texture != null) { camera.targetTexture = null; texture.Release(); Object.Destroy(texture); }
                texture = new RenderTexture(width, height, 24) { antiAliasing = 2, name = "Echte Stationsmodelle / Strompult" };
                texture.Create(); camera.targetTexture = texture; renderedFrame = -1;
            }
            if (renderedFrame == Time.frameCount) return;
            camera.aspect = (float)width / height;
            var rotation = Quaternion.Euler(pitch, yaw, 0);
            camera.transform.SetPositionAndRotation(origin + bounds.center - rotation * Vector3.forward * 450, rotation);
            // Fit all eight corners in the orthographic camera, at the current aspect ratio.
            float extent = .2f;
            for (int i = 0; i < 8; i++)
            {
                var sign = new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1);
                var point = Quaternion.Inverse(rotation) * Vector3.Scale(bounds.extents, sign);
                extent = Mathf.Max(extent, Mathf.Abs(point.y), Mathf.Abs(point.x) / camera.aspect);
            }
            camera.orthographicSize = extent * 1.08f;
            key.enabled = fill.enabled = true;
            try
            {
                var offset = Matrix4x4.Translate(origin) * reference.worldToLocalMatrix;
                foreach (var part in parts)
                {
                    if (part.Source == null) continue;
                    part.Source.GetPropertyBlock(properties);
                    for (int sub = 0; sub < part.Mesh.subMeshCount && sub < part.Materials.Length; sub++)
                        if (part.Materials[sub] != null)
                            Graphics.DrawMesh(part.Mesh, offset * part.Source.localToWorldMatrix, part.Materials[sub], 31,
                                camera, sub, properties, ShadowCastingMode.Off, false, null, LightProbeUsage.Off);
                }
                camera.Render(); renderedFrame = Time.frameCount;
            }
            finally { key.enabled = fill.enabled = false; }
        }
        public void Dispose()
        {
            camera.targetTexture = null;
            if (texture != null) { texture.Release(); Object.Destroy(texture); }
            Object.Destroy(rig); parts.Clear();
        }
    }
}
