using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceMiner
{
    // Reusable geometric service: preserve UVs and submeshes while opening an installed wall.
    internal static class StationAperture
    {
        private struct Point {internal Vector3 p,n;internal Vector2 uv;}
        private static List<Point> Clip(List<Point> input,int axis,float edge,bool above)
        {
            var result=new List<Point>();for(int i=0;i<input.Count;i++){var a=input[i];var b=input[(i+1)%input.Count];bool ai=above?a.p[axis]>=edge:a.p[axis]<=edge,bi=above?b.p[axis]>=edge:b.p[axis]<=edge;if(ai)result.Add(a);if(ai!=bi){float t=(edge-a.p[axis])/(b.p[axis]-a.p[axis]);result.Add(new Point{p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t).normalized,uv=Vector2.Lerp(a.uv,b.uv,t)});}}return result;
        }
        internal static Mesh Cut(MeshFilter filter,Transform plane,float halfWidth,float height)
        {
            var source=filter.sharedMesh;var positions=source.vertices;var normals=source.normals;var uv=source.uv;
            Vector3 center=plane.InverseTransformPoint(filter.transform.TransformPoint(source.bounds.center));
            if(Mathf.Abs(center.z)>1.8f+source.bounds.size.magnitude*.5f||center.y>height+source.bounds.size.magnitude*.5f)return null;
            var vertices=new List<Vector3>();var normalList=new List<Vector3>();var tex=new List<Vector2>();var submeshes=new List<int[]>();bool touched=false;
            for(int sub=0;sub<source.subMeshCount;sub++){
                var triangles=source.GetTriangles(sub);var output=new List<int>();
                void Emit(List<Point> polygon){for(int k=1;k+1<polygon.Count;k++)foreach(int j in new[]{0,k,k+1}){var v=polygon[j];output.Add(vertices.Count);vertices.Add(filter.transform.InverseTransformPoint(plane.TransformPoint(v.p)));normalList.Add(filter.transform.InverseTransformDirection(plane.TransformDirection(v.n)));tex.Add(v.uv);}}
                for(int t=0;t<triangles.Length;t+=3){var polygon=new List<Point>();for(int j=0;j<3;j++){int index=triangles[t+j];polygon.Add(new Point{p=plane.InverseTransformPoint(filter.transform.TransformPoint(positions[index])),n=plane.InverseTransformDirection(filter.transform.TransformDirection(normals.Length==positions.Length?normals[index]:Vector3.forward)),uv=uv.Length==positions.Length?uv[index]:Vector2.zero});}
                    bool near=false;foreach(var v in polygon)if(Mathf.Abs(v.p.z)<1.3f)near=true;if(!near){Emit(polygon);continue;}
                    var inside=Clip(Clip(Clip(Clip(polygon,0,-halfWidth,true),0,halfWidth,false),1,-.06f,true),1,height,false);if(inside.Count==0){Emit(polygon);continue;}touched=true;
                    Emit(Clip(polygon,0,-halfWidth,false));polygon=Clip(polygon,0,-halfWidth,true);Emit(Clip(polygon,0,halfWidth,true));polygon=Clip(polygon,0,halfWidth,false);Emit(Clip(polygon,1,-.06f,false));polygon=Clip(polygon,1,-.06f,true);Emit(Clip(polygon,1,height,true));
                }
                submeshes.Add(output.ToArray());
            }
            if(!touched)return null;var mesh=new Mesh{name=source.name+" / geöffneter Modulanschluss",indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetNormals(normalList);mesh.SetUVs(0,tex);mesh.subMeshCount=submeshes.Count;for(int i=0;i<submeshes.Count;i++)mesh.SetTriangles(submeshes[i],i);mesh.RecalculateBounds();mesh.RecalculateTangents();return mesh;
        }
    }
}
