using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner.Editor
{
    // Non-destructive prototype rig for this particular, normalized H2.5 bust.
    // These are surface shapes, not a replacement for anatomical retopology.
    internal static class MiraTripoFaceShapes
    {
        private static float Ease(float value) => Mathf.SmoothStep(0, 1, Mathf.Clamp01(value));
        private static float Bell(float value) => Ease(1 - value * value);
        private static float Front(Vector3 v) => Ease((v.z - .105f) / .035f);

        internal static void Refine(Mesh mesh)
        {
            var positions = new List<Vector3>(mesh.vertices);
            var normals = new List<Vector3>(mesh.normals);
            var uv = new List<Vector2>(mesh.uv);
            var triangles = new List<int>(mesh.triangles);
            bool InFace(Vector3 p) => p.y > .215f && p.y < .49f && Mathf.Abs(p.x + .025f) < .14f && p.z > .105f;
            for (int pass = 0; pass < 3; pass++)
            {
                var midpoints = new Dictionary<ulong, int>();
                ulong Key(int a, int b) => ((ulong)(uint)Mathf.Min(a,b) << 32) | (uint)Mathf.Max(a,b);
                void Split(int a, int b)
                {
                    if (!InFace(positions[a]) || !InFace(positions[b]) || (positions[a]-positions[b]).sqrMagnitude < .000025f) return;
                    ulong key = Key(a,b); if (midpoints.ContainsKey(key)) return;
                    midpoints.Add(key, positions.Count);
                    positions.Add((positions[a]+positions[b])*.5f);
                    normals.Add((normals[a]+normals[b]).normalized);
                    uv.Add((uv[a]+uv[b])*.5f);
                }
                for (int i=0; i<triangles.Count; i+=3)
                { Split(triangles[i],triangles[i+1]); Split(triangles[i+1],triangles[i+2]); Split(triangles[i+2],triangles[i]); }
                var refined = new List<int>(triangles.Count*2);
                void Tri(int a,int b,int c) { refined.Add(a);refined.Add(b);refined.Add(c); }
                for (int i=0; i<triangles.Count; i+=3)
                {
                    int a=triangles[i],b=triangles[i+1],c=triangles[i+2];
                    bool ab=midpoints.TryGetValue(Key(a,b),out int d), bc=midpoints.TryGetValue(Key(b,c),out int e), ca=midpoints.TryGetValue(Key(c,a),out int f);
                    int mask=(ab?1:0)|(bc?2:0)|(ca?4:0);
                    switch(mask)
                    {
                        case 0: Tri(a,b,c);break;
                        case 1: Tri(a,d,c);Tri(d,b,c);break;
                        case 2: Tri(a,b,e);Tri(a,e,c);break;
                        case 4: Tri(a,b,f);Tri(f,b,c);break;
                        case 3: Tri(d,b,e);Tri(a,d,c);Tri(d,e,c);break;
                        case 5: Tri(a,d,f);Tri(d,b,c);Tri(d,c,f);break;
                        case 6: Tri(f,e,c);Tri(a,b,f);Tri(b,e,f);break;
                        default: Tri(a,d,f);Tri(d,b,e);Tri(f,e,c);Tri(d,e,f);break;
                    }
                }
                triangles=refined;
            }
            mesh.Clear();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(positions);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);
            mesh.RecalculateBounds();mesh.RecalculateTangents();
        }

        internal static void Add(Mesh target)
        {
            Vector3[] vertices=target.vertices;
            // Establish the vertex layout before Unity allocates blend-shape buffers.
            var weights=new BoneWeight[vertices.Length];
            for(int i=0;i<weights.Length;i++)
            {
                float head=Ease((vertices[i].y-.235f)/.065f);
                weights[i]=new BoneWeight{boneIndex0=0,weight0=1-head,boneIndex1=1,weight1=head};
            }
            target.boneWeights=weights;
            target.bindposes=new[]{Matrix4x4.identity,Matrix4x4.Translate(new Vector3(0,-.245f,0))};
            var normalProbe=new Mesh{indexFormat=target.indexFormat,vertices=vertices,triangles=target.triangles};
            normalProbe.RecalculateNormals();var baseNormals=normalProbe.normals;
            void Shape(string name,Func<Vector3,Vector3> deform)
            {
                var delta=new Vector3[vertices.Length];int count=0;
                for(int i=0;i<vertices.Length;i++){delta[i]=deform(vertices[i]);if(delta[i].sqrMagnitude>1e-10f)count++;}
                if(count<10)throw new Exception("Empty Mira expression: "+name);
                Vector3[] normalDelta=null;
                if(name.StartsWith("Blink",StringComparison.Ordinal))
                {
                    var deformed=new Vector3[vertices.Length];
                    for(int i=0;i<vertices.Length;i++)deformed[i]=vertices[i]+delta[i];
                    normalProbe.vertices=deformed;normalProbe.RecalculateNormals();normalDelta=normalProbe.normals;
                    for(int i=0;i<normalDelta.Length;i++)normalDelta[i]-=baseNormals[i];
                }
                target.AddBlendShapeFrame(name,100,delta,normalDelta,null);
            }
            float Mouth(Vector3 v) => Bell((v.x+.025f)/.078f)*Bell((v.y-(.292f+.18f*(v.x+.02f)))/.036f)*Front(v);
            Shape("LipClose", v => {
                float seam=.290f+.18f*(v.x+.02f), d=v.y-seam;
                return new Vector3(0,-Mathf.Clamp(d,-.008f,.008f)*.78f,0)*Mouth(v)*Bell(d/.022f);
            });
            Shape("Round",v=>new Vector3(-(v.x+.025f)*.15f,0,.002f)*Mouth(v));
            Shape("Wide",v=>new Vector3((v.x+.025f)*.075f,0,-.0005f)*Mouth(v));
            Shape("Smile",v=> {
                float corners=Bell((Mathf.Abs(v.x+.025f)-.036f)/.028f)*Bell((v.y-(.291f+.18f*(v.x+.02f)))/.040f)*Front(v);
                return new Vector3(Mathf.Sign(v.x+.025f)*.0015f,.003f,0)*corners;
            });
            Shape("BrowConcern",v=> {
                float left=Bell((v.x-.012f)/.043f)*Bell((v.y-(.452f+.20f*(v.x-.012f)))/.025f);
                float right=Bell((v.x+.068f)/.041f)*Bell((v.y-(.431f+.20f*(v.x+.068f)))/.025f);
                return Vector3.up*(.0035f*(left+right)*Front(v));
            });
            Shape("BrowFocus",v=> {
                float w=Bell((v.x+.025f)/.11f)*Bell((v.y-(.443f+.20f*(v.x+.025f)))/.027f)*Front(v);
                return Vector3.down*(.0018f*w);
            });
            Vector2[] eyes={new Vector2(.023f,.408f),new Vector2(-.083f,.389f)};
            for(int eye=0;eye<eyes.Length;eye++)
            {
                Vector2 center=eyes[eye];
                Shape(eye==0?"BlinkLeft":"BlinkRight",v=> {
                    float x=(v.x-center.x)/.033f;
                    float line=center.y+.20f*(v.x-center.x)-.002f+.003f*Mathf.Min(1,x*x);
                    float y=v.y-line;
                    // Collapse the eye aperture; surrounding skin supplies the upper/lower lid.
                    float close= y>=0 ? Ease((.030f-y)/.016f) : Ease((.020f+y)/.010f);
                    return new Vector3(0,-y*.98f,0)*close*Ease((1-Mathf.Abs(x))/.23f)*Ease((v.z-.07f)/.045f);
                });
            }
            float EyeMask(Vector3 v)
            {
                float w=0;
                foreach(var eye in eyes)w+=Bell((v.x-eye.x)/.028f)*Bell((v.y-(eye.y+.003f+.20f*(v.x-eye.x)))/.012f);
                return Mathf.Clamp01(w)*Ease((v.z-.07f)/.045f);
            }
            Shape("LookLeft",v=>Vector3.right*(.002f*EyeMask(v)));
            Shape("LookRight",v=>Vector3.left*(.002f*EyeMask(v)));
            Shape("LookUp",v=>Vector3.up*(.0012f*EyeMask(v)));
            Shape("LookDown",v=>Vector3.down*(.0012f*EyeMask(v)));
            UnityEngine.Object.DestroyImmediate(normalProbe);
        }
    }
}
