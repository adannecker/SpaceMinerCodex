using System;
using UnityEngine;

namespace SpaceMiner
{
    // Shared attachment sizing against the original imported hull triangles.
    internal static class DroneToolMount
    {
        internal static void FindMountSurface(Transform body, Transform rig, float axisY, out float envelope, out float rearMount, out float frontMount)
        {
            envelope = rearMount = frontMount = float.NegativeInfinity;
            foreach(var filter in body.GetComponentsInChildren<MeshFilter>())
            {
                var mesh=filter.sharedMesh;
                var matrix=rig.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                var vertices=mesh.vertices; var triangles=mesh.triangles;
                for(int i=0;i<vertices.Length;i++) vertices[i]=matrix.MultiplyPoint3x4(vertices[i]);
                for(int i=0;i<triangles.Length;i+=3)
                {
                    Vector3 a=vertices[triangles[i]], b=vertices[triangles[i+1]], c=vertices[triangles[i+2]];
                    Vector3 min=Vector3.Min(a,Vector3.Min(b,c)), max=Vector3.Max(a,Vector3.Max(b,c));
                    if(max.y<axisY-.23f || min.y>axisY+.25f || max.z<-.16f || min.z>2.22f) continue;
                    envelope=Mathf.Max(envelope,max.x);
                    rearMount=Mathf.Max(rearMount,SurfaceX(a,b,c,axisY,.08f));
                    frontMount=Mathf.Max(frontMount,SurfaceX(a,b,c,axisY,.58f));
                }
            }
            if(float.IsInfinity(envelope) || float.IsInfinity(rearMount) || float.IsInfinity(frontMount))
                throw new Exception("Tool cassette needs two actual hull mounting surfaces");
        }
        private static float SurfaceX(Vector3 a, Vector3 b, Vector3 c, float y, float z)
        {
            // Ray / triangle intersection along X, expressed as barycentric coordinates in the YZ plane.
            float det=(b.z-c.z)*(a.y-c.y)+(c.y-b.y)*(a.z-c.z);
            if(Mathf.Abs(det)<.00000001f) return float.NegativeInfinity;
            float u=((b.z-c.z)*(y-c.y)+(c.y-b.y)*(z-c.z))/det;
            float v=((c.z-a.z)*(y-c.y)+(a.y-c.y)*(z-c.z))/det;
            if(u<0 || v<0 || u+v>1) return float.NegativeInfinity;
            return a.x*u+b.x*v+c.x*(1-u-v);
        }
        internal static float RoofSurface(Transform body,Transform rig,float x,float z)
        {
            float top=float.NegativeInfinity;
            foreach(var filter in body.GetComponentsInChildren<MeshFilter>())
            {
                var mesh=filter.sharedMesh; var vertices=mesh.vertices; var triangles=mesh.triangles;
                var matrix=rig.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                for(int i=0;i<vertices.Length;i++) vertices[i]=matrix.MultiplyPoint3x4(vertices[i]);
                for(int i=0;i<triangles.Length;i+=3)
                {
                    // Swap X/Y to reuse the ray/triangle barycentric intersection helper.
                    Vector3 Swap(Vector3 p)=>new Vector3(p.y,p.x,p.z);
                    top=Mathf.Max(top,SurfaceX(Swap(vertices[triangles[i]]),Swap(vertices[triangles[i+1]]),Swap(vertices[triangles[i+2]]),x,z));
                }
            }
            if(float.IsInfinity(top)) throw new Exception("Cargo mount has no original hull roof below it");
            return top;
        }
    }
}
