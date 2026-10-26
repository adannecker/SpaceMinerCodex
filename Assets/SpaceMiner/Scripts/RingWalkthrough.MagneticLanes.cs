using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    internal sealed partial class RingWalkthrough
    {
        private Material laneBed,magnetMetal,magnetSignal;
        private void BuildMagneticLanes(Transform parent,float begin,float end)
        {
            if(laneBed==null)
            {
                laneBed=Surface(new Color(.085f,.115f,.135f),.35f);
                magnetMetal=Surface(new Color(.29f,.36f,.39f),.7f);
                magnetSignal=Surface(new Color(.10f,.32f,.36f),.5f);
            }
            float[] radii={30.05f,30.65f,30.85f,32.35f,33.85f,35.35f,36.85f};
            for(int line=0;line<radii.Length;line++)
                FloorArc(parent,"Bodenmarkierung / durchgehend / "+(line+1),radii[line],.05f,begin,end,.012f,amber);
            var ties=new List<Vector3>();var arrows=new List<Vector3>();
            for(int lane=0;lane<StationRingLanes.Count;lane++)
            {
                float center=StationRingLanes.Center(lane);
                // Flush inserts, no rails projecting into walking or cargo clearance.
                FloorArc(parent,"Magnetfahrbahn / Spur "+(lane+1),center-.58f,1.16f,begin,end,.008f,laneBed);
                foreach(float side in new[]{-1f,1f})
                {
                    float rail=center+side*StationRingLanes.MagnetGauge*.5f;
                    FloorArc(parent,"Magnetleitung / Spur "+(lane+1)+" / "+side,rail-.04f,.08f,begin,end,.018f,magnetMetal);
                    FloorArc(parent,"Magnetstatus / Spur "+(lane+1)+" / "+side,rail-.005f,.01f,begin,end,.020f,magnetSignal);
                }
                for(float a=begin+.75f;a<end;a+=1.5f)
                {
                    var r=At(1,a);var t=Quaternion.Euler(0,90,0)*r;
                    var p=At(center,a,.016f);
                    AddFlatQuad(ties,p-r*.29f-t*.022f,p+r*.29f-t*.022f,p+r*.29f+t*.022f,p-r*.29f+t*.022f);
                }
                for(float a=begin+5;a<end;a+=10)
                {
                    var r=At(1,a);var t=Quaternion.Euler(0,90,0)*r*StationRingLanes.Direction(lane);
                    var p=At(center,a,.023f);
                    // Open chevrons keep both magnetic tracks visible under the arrow.
                    foreach(float side in new[]{-1f,1f})
                    {
                        var tip=p+t*.28f;var tail=p-t*.16f+r*side*.23f;
                        var cross=(Quaternion.Euler(0,90,0)*(tip-tail)).normalized*.025f;
                        AddFlatQuad(arrows,tail-cross,tip-cross,tip+cross,tail+cross);
                    }
                }
            }
            FlatDetails(parent,"Magnetleiter / flache Querverbinder",ties,magnetMetal);
            FlatDetails(parent,"Magnetfahrbahn / Richtungspfeile",arrows,amber);
            var junctions=new List<Vector3>();var crossRails=new List<Vector3>();
            foreach(float angle in StationRingLanes.JunctionAngles)
            {
                float a=Mathf.Repeat(angle,360);if(a>end)a-=360;
                if(a<begin||a>=end)continue;
                // Flat cross-feed conductors and dashed orange limits identify the
                // room accesses/turnarounds where traffic may cross the direction pairs.
                foreach(float side in new[]{-1f,1f})
                {
                    float offset=side*StationRingLanes.MagnetGauge*.5f;
                    var r=At(1,a);var t=Quaternion.Euler(0,90,0)*r;
                    var from=At(31.6f,a,.025f)+t*offset;var to=At(36.1f,a,.025f)+t*offset;
                    AddFlatQuad(crossRails,from-t*.04f,to-t*.04f,to+t*.04f,from+t*.04f);
                    for(float radius=30.9f;radius<36.8f;radius+=.5f)
                    {
                        var p=At(radius,a+side*3.5f,.026f);var radial=At(1,a+side*3.5f);var tangent=Quaternion.Euler(0,90,0)*radial;
                        AddFlatQuad(junctions,p-radial*.15f-tangent*.018f,p+radial*.15f-tangent*.018f,p+radial*.15f+tangent*.018f,p-radial*.15f+tangent*.018f);
                    }
                }
            }
            if(junctions.Count>0)FlatDetails(parent,"Magnetwechselzone / orange Quergrenzen",junctions,amber);
            if(crossRails.Count>0)FlatDetails(parent,"Magnetwechselzone / flache Querleiter",crossRails,magnetMetal);
        }
        private static void AddFlatQuad(List<Vector3> vertices,Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        { vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d); }
        private void FlatDetails(Transform parent,string name,List<Vector3> vertices,Material material)
        {
            var triangles=new List<int>();
            for(int i=0;i<vertices.Count;i+=4)triangles.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});
            FloorMesh(parent,name,vertices.ToArray(),triangles.ToArray(),material);
        }
        private void FloorArc(Transform parent,string name,float radius,float width,float begin,float end,float y,Material material)
        {
            int steps=Mathf.CeilToInt((end-begin)*4);
            var vertices=new Vector3[(steps+1)*2];var triangles=new int[steps*6];
            for(int i=0;i<=steps;i++)
            {
                float a=Mathf.Lerp(begin,end,(float)i/steps);vertices[i*2]=At(radius,a,y);vertices[i*2+1]=At(radius+width,a,y);
                if(i==steps)continue;
                int v=i*2,k=i*6;triangles[k]=v;triangles[k+1]=v+1;triangles[k+2]=v+2;
                triangles[k+3]=v+1;triangles[k+4]=v+3;triangles[k+5]=v+2;
            }
            FloorMesh(parent,name,vertices,triangles,material);
        }
        private void FloorMesh(Transform parent,string name,Vector3[] vertices,int[] triangles,Material material)
        {
            var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
            var root=Child(name,parent);root.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=root.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
