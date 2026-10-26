using UnityEngine;

namespace SpaceMiner
{
    // Reuse the authored bay's restrained painted metal, without depending on Editor-only assets.
    internal static class StationSurfaceMaterials
    {
        private static Material paint;
        internal static Material Create(Color tint, float metallic, bool glow = false)
        {
            if (paint == null)
            {
                var prefab = Resources.Load<GameObject>("Station/DetailedDroneHall");
                if (prefab != null) foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                    foreach (var material in renderer.sharedMaterials)
                        if (material != null && material.name.ToLowerInvariant().Contains("paint")) { paint = material; break; }
            }
            var result = !glow && paint != null ? new Material(paint) : new Material(Shader.Find("Standard"));
            result.name = glow ? "Station steady signal" : "Station / bay painted alloy";
            result.color=tint; result.SetFloat("_Metallic",metallic); result.SetFloat("_Glossiness",.25f);
            if (glow) { result.EnableKeyword("_EMISSION"); result.SetColor("_EmissionColor",tint*.8f); }
            return result;
        }
        internal static void WorldUvs(Mesh mesh)
        {
            var vertices=mesh.vertices; var normals=mesh.normals; var uv=new Vector2[vertices.Length];
            for(int i=0;i<vertices.Length;i++)
            {
                var n=normals[i];var p=vertices[i];
                uv[i]=Mathf.Abs(n.y)>.6f ? new Vector2(p.x,p.z)*.2f
                    : Mathf.Abs(n.x)>Mathf.Abs(n.z) ? new Vector2(p.z,p.y)*.2f : new Vector2(p.x,p.y)*.2f;
            }
            mesh.uv=uv;
        }
    }
}
