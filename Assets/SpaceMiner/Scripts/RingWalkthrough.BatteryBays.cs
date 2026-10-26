using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    internal sealed partial class RingWalkthrough
    {
        internal const float BatteryBayUMin = 2.2f, BatteryBayUMax = 4.8f;
        internal const float BatteryBayRear = 23.8f, BatteryBayFront = 28.3f, BatteryBayClearHeight = 3.8f;
        internal const float BatteryBayWallDepth = .22f, BatteryBayCollarRadius = 29.1f;
        private readonly List<Transform> batteryBayAnchors = new List<Transform>();
        private readonly List<Transform> batteryBayFrames = new List<Transform>();
        internal IReadOnlyList<Transform> BatteryBayAnchors => batteryBayAnchors;
        internal IReadOnlyList<Transform> BatteryBayFrames => batteryBayFrames;
        // Axis0..3; positive side is left when looking from the ring towards the reactor.
        internal Transform GetBatteryBayAnchor(int axis, int side)
            => axis >= 0 && axis < 4 && batteryBayAnchors.Count == 8 ? batteryBayAnchors[axis * 2 + (side > 0 ? 0 : 1)] : null;
        internal static bool ContainsBatteryBayFloor(Vector3 stationLocal, bool allowDamagedAxis = false)
        {
            if (stationLocal.y < -.2f || stationLocal.y > .5f) return false;
            float angle = Mathf.Atan2(stationLocal.x, stationLocal.z) * Mathf.Rad2Deg;
            int axis = (Mathf.RoundToInt(angle / 90) + 4) % 4;
            if (axis == 1 && !allowDamagedAxis) return false;
            Vector3 p = Quaternion.Euler(0, -axis * 90, 0) * stationLocal;
            float u = Mathf.Abs(p.x);
            return u >= BatteryBayUMin + .3f && u <= BatteryBayUMax - .3f && p.z >= BatteryBayRear + .3f
                && p.z <= Mathf.Sqrt(29.4f * 29.4f - u * u);
        }
        private static Func<float, float> BatteryUAngle(float axis, float u)
            => radius => axis + Mathf.Asin(Mathf.Clamp(u / radius, -1, 1)) * Mathf.Rad2Deg;
        private static Func<float, float> BatteryFixedAngle(float angle) => radius => angle;

        // Each wall layer uses asin(u/r) at its actual radius. A constant-angle cut
        // would leave projecting edges in the two skins of a rectangular bay front.
        private void BuildBatteryBayRingWall(Transform parent, float axis, float begin, float end, Material surface)
        {
            var farLeft = BatteryUAngle(axis, -BatteryBayUMax); var nearLeft = BatteryUAngle(axis, -BatteryBayUMin);
            var nearRight = BatteryUAngle(axis, BatteryBayUMin); var farRight = BatteryUAngle(axis, BatteryBayUMax);
            BatteryWallPiece(parent,"Ringwand / Akkubucht links aussen",BatteryFixedAngle(begin),farLeft,0,5.5f,surface);
            BatteryWallPiece(parent,"Ringwand / Akkubucht links Sturz",farLeft,nearLeft,BatteryBayClearHeight,5.5f,surface);
            BatteryWallPiece(parent,"Ringwand / Akkubucht linker Servicepfeiler",nearLeft,BatteryUAngle(axis,-1.5f),0,5.5f,surface);
            BatteryWallPiece(parent,"Ringwand / Akkubucht rechter Servicepfeiler",BatteryUAngle(axis,1.5f),nearRight,0,5.5f,surface);
            BatteryWallPiece(parent,"Ringwand / Akkubucht rechts Sturz",nearRight,farRight,BatteryBayClearHeight,5.5f,surface);
            BatteryWallPiece(parent,"Ringwand / Akkubucht rechts aussen",farRight,BatteryFixedAngle(end),0,5.5f,surface);
        }
        private void BatteryWallPiece(Transform parent,string name,Func<float,float> begin,Func<float,float> end,float low,float high,Material surface)
        {
            var node=BatteryArcMesh(name,parent,28.9f,29.1f,begin,end,low,high,surface,true);
            node.GetComponent<MeshRenderer>().enabled=false;
            foreach(bool outside in new[]{false,true})
            {
                float face=outside?28.62f:29.145f,normal=outside?-1:1;
                float width=face*(end(face)-begin(face))*Mathf.Deg2Rad;
                Skin(node,outside?"Waben / Akkubucht Ring-Aussenschutz":"Waben / Akkubucht Ring-Drucklage",width,high-low,
                    (p,d)=>{float r=face+normal*d;return At(r,Mathf.Lerp(begin(r),end(r),p.x/width),low+p.y);},outside?hullB:hullA);
            }
            HullSupportFrames++;
            foreach(float y in new[]{low+.065f,high-.065f})
                BatteryArcMesh("Ringrahmen / beide Wandlagen geschlossen",node,28.68f,29.10f,begin,end,y-.065f,y+.065f,frame,false);
            float span=(end(29)-begin(29))*Mathf.Deg2Rad*29;
            int ribs=Mathf.Max(1,Mathf.CeilToInt(span/2.5f));
            for(int i=1;i<ribs;i++)
                Box("Ringrahmen / verbundene Abstandsrippe",node,At(28.89f,Mathf.Lerp(begin(29),end(29),(float)i/ribs),(low+high)/2),
                    new Vector3(.12f,high-low,.42f),dark,Mathf.Lerp(begin(29),end(29),(float)i/ribs),false);
        }
        private Transform BatteryArcMesh(string name,Transform parent,float ri,float ro,Func<float,float> begin,Func<float,float> end,
            float low,float high,Material surface,bool collide)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            int steps=Mathf.Max(1,Mathf.CeilToInt(Mathf.Max(end(ri)-begin(ri),end(ro)-begin(ro))/1.5f));
            for(int i=0;i<steps;i++)
            {
                float a=(float)i/steps,b=(float)(i+1)/steps;
                var ia=At(ri,Mathf.Lerp(begin(ri),end(ri),a),high);var oa=At(ro,Mathf.Lerp(begin(ro),end(ro),a),high);
                var ib=At(ri,Mathf.Lerp(begin(ri),end(ri),b),high);var ob=At(ro,Mathf.Lerp(begin(ro),end(ro),b),high);
                var down=Vector3.up*(low-high);
                Quad(ia,oa,ob,ib);Quad(ib+down,ob+down,oa+down,ia+down);
                Quad(ia+down,oa+down,oa,ia);Quad(oa+down,ob+down,ob,oa);
                Quad(ob+down,ib+down,ib,ob);Quad(ib+down,ia+down,ia,ib);
            }
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();StationSurfaceMaterials.WorldUvs(mesh);meshes.Add(mesh);
            var node=Child(name,parent);node.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;node.gameObject.AddComponent<MeshRenderer>().sharedMaterial=surface;
            if(collide)node.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh;return node;
        }
        private void BuildBatteryBayDamagedRemnants(Transform parent,float axis)
        {
            // Keep the absent ring floor and locked boundary bulkheads. Clip the
            // remaining inner wall fragments around the new, separately sealed bays.
            var far=BatteryUAngle(axis,-BatteryBayUMax);var near=BatteryUAngle(axis,-BatteryBayUMin);
            BatteryArcMesh("Wandhaut / ausgeschnittener Rest",parent,29.1f,29.3f,BatteryFixedAngle(axis-14),far,0,4.8f,hullA,false);
            BatteryArcMesh("Wandhaut / Buchtsturz im Rest",parent,29.1f,29.3f,far,BatteryFixedAngle(axis-7),BatteryBayClearHeight,4.8f,hullA,false);
            BatteryArcMesh("Offener Ringtraeger / links der Bucht",parent,29.08f,29.32f,BatteryFixedAngle(axis-14),far,.1f,.4f,frame,false);
            BatteryArcMesh("Offener Ringtraeger / Servicepfeiler",parent,29.08f,29.32f,near,BatteryUAngle(axis,1.5f),.1f,.4f,frame,false);
            BatteryArcMesh("Offener Ringtraeger / rechts der Bucht",parent,29.08f,29.32f,BatteryUAngle(axis,BatteryBayUMax),BatteryFixedAngle(axis+14),.1f,.4f,frame,false);
        }
        private void BuildBatteryBayHulls(Transform parent)
        {
            for(int axis=0;axis<4;axis++)for(int side=1;side>=-1;side-=2)
            {
                float x0=side>0?BatteryBayUMin:-BatteryBayUMax,x1=side>0?BatteryBayUMax:-BatteryBayUMin;
                var bay=Child("Akkubucht / "+(axis*90)+" / "+(side>0?"links":"rechts"),parent);bay.localRotation=Quaternion.Euler(0,axis*90,0);
                batteryBayFrames.Add(bay);BuildBatteryHull(bay,x0,x1);
                var anchor=Child("Detaileinsatz / Frontboden",bay);anchor.localPosition=new Vector3((x0+x1)/2,.06f,28.10f);anchor.localRotation=Quaternion.Euler(0,180,0);
                batteryBayAnchors.Add(anchor);
            }
            // This axis never ran BuildSegmentInterfaces because its ring floor is
            // ruptured. Add the missing locked front shutter, without repairing it.
            Gate(parent,"Reaktorschott / 90",At(28.75f,90),90,2.9f,3.1f,true,true);
        }
        private void BuildBatteryHull(Transform bay,float x0,float x1)
        {
            float centre=(x0+x1)/2,width=x1-x0,depth=BatteryBayFront-BatteryBayRear;
            var floorBody=Box("Akkubucht Druckboden",bay,new Vector3(centre,-.06f,(BatteryBayRear+BatteryBayFront)/2),new Vector3(width+.44f,.12f,depth+.22f),hullA);
            floorBody.GetComponent<Renderer>().enabled=false;
            var roofBody=Box("Akkubucht Druckdach",bay,new Vector3(centre,3.86f,(BatteryBayRear+BatteryBayFront)/2),new Vector3(width+.44f,.12f,depth+.22f),hullA);
            roofBody.GetComponent<Renderer>().enabled=false;roofs.Add(roofBody.gameObject);
            foreach(bool outside in new[]{false,true})
            {
                Skin(bay,outside?"Akkubucht Boden / Schutzhaut":"Akkubucht Boden / Druckhaut",width+.44f,depth+.22f,
                    (p,d)=>new Vector3(x0-.22f+p.x,(outside?-.32f:-.04f)+(outside?-d:d),BatteryBayRear-.11f+p.y),outside?hullB:hullA);
                var ceiling=Child(outside?"Akkubucht Dach / Schutzhaut":"Akkubucht Dach / Druckhaut",bay);roofs.Add(ceiling.gameObject);
                Skin(ceiling,"Waben / geschlossene Dachlage",width+.44f,depth+.22f,
                    (p,d)=>new Vector3(x0-.22f+p.x,(outside?4.08f:3.84f)+(outside?d:-d),BatteryBayRear-.11f+p.y),outside?hullB:hullA);
            }
            foreach(float x in new[]{x0,x1})
            {
                float outward=x==x0?-1:1;
                BatterySide(bay,x,outward,BatteryBayRear,BatteryBayFront);
                BatterySide(bay,x,outward,BatteryBayFront-.05f,Mathf.Sqrt(BatteryBayCollarRadius*BatteryBayCollarRadius-x*x));
            }
            var back=Box("Akkubucht hintere Druckwand",bay,new Vector3(centre,1.9f,BatteryBayRear-.04f),new Vector3(width,3.8f,.08f),hullA);back.GetComponent<Renderer>().enabled=false;
            foreach(bool outside in new[]{false,true})
                Skin(bay,outside?"Akkubucht Rueckwand / Schutzhaut":"Akkubucht Rueckwand / Druckhaut",width,3.8f,
                    (p,d)=>new Vector3(x0+p.x,p.y,(outside?BatteryBayRear-.18f:BatteryBayRear-.04f)+(outside?-d:d)),outside?hullB:hullA);
            foreach(float x in new[]{x0-.11f,x1+.11f})
                Box("Akkubucht / geschlossener hinterer Eckpfosten",bay,new Vector3(x,1.9f,BatteryBayRear-.11f),new Vector3(.22f,3.8f,.22f),frame);
            foreach(float z in new[]{BatteryBayRear+.06f,25.9f,BatteryBayFront-.06f})
            {
                Box("Akkubucht Tragwerk / Bodenquertraeger",bay,new Vector3(centre,-.20f,z),new Vector3(width+.44f,.16f,.12f),frame,0,false);
                var rib=Box("Akkubucht Tragwerk / Dachquertraeger",bay,new Vector3(centre,4,z),new Vector3(width+.44f,.16f,.12f),frame,0,false);roofs.Add(rib.gameObject);
            }
            BatteryCollarDeck(bay,x0-.22f,x1+.22f,false);BatteryCollarDeck(bay,x0-.22f,x1+.22f,true);
            // Jambs bridge both curved wall skins and the entire collar depth.
            foreach(var span in new[]{new Vector2(x0-.22f,x0),new Vector2(x1,x1+.22f)})
                BatteryArcMesh("Akkubucht / integrierte zweilagige Portalzarge",bay,28.5f,29.28f,BatteryUAngle(0,span.x),BatteryUAngle(0,span.y),0,3.8f,frame,true);
            var lintel=BatteryArcMesh("Akkubucht / integrierter Portalsturz",bay,28.5f,29.28f,BatteryUAngle(0,x0-.22f),BatteryUAngle(0,x1+.22f),3.8f,4.02f,frame,true);roofs.Add(lintel.gameObject);
            HullSupportFrames+=4;
        }
        private void BatterySide(Transform parent,float x,float outward,float from,float to)
        {
            float length=to-from;
            var body=Box("Akkubucht seitliche Druckwand",parent,new Vector3(x+outward*.04f,1.9f,(from+to)/2),new Vector3(.08f,3.8f,length),hullA);body.GetComponent<Renderer>().enabled=false;
            foreach(bool outside in new[]{false,true})
            {
                float face=x+outward*(outside?.18f:.04f),normal=outside?outward:-outward;
                Skin(parent,outside?"Akkubucht Seite / Schutzhaut":"Akkubucht Seite / Druckhaut",length,3.8f,
                    (p,d)=>new Vector3(face+normal*d,p.y,from+p.x),outside?hullB:hullA);
            }
            foreach(float y in new[]{.08f,3.72f})Box("Akkubucht Tragwerk / Wandgurt",parent,new Vector3(x+outward*.11f,y,(from+to)/2),new Vector3(.06f,.16f,length),frame,0,false);
            foreach(float z in new[]{from+.06f,to-.06f})Box("Akkubucht Tragwerk / Wandrippe",parent,new Vector3(x+outward*.11f,1.9f,z),new Vector3(.06f,3.8f,.12f),frame,0,false);
        }
        private void BatteryCollarDeck(Transform parent,float x0,float x1,bool ceiling)
        {
            const float back=28.25f;
            CollarLayer(ceiling?"Akkubucht / geschlossener Dachkragen":"Akkubucht / fugenloser Bodenkragen",ceiling?3.8f:-.12f,ceiling?3.92f:0,ceiling?hullA:floor,true);
            CollarLayer(ceiling?"Akkubucht / Dachkragen Schutzlage":"Akkubucht / Bodenkragen Schutzlage",ceiling?4.08f:-.36f,ceiling?4.16f:-.28f,hullB,false);
            float midX=(x0+x1)/2,front=Mathf.Sqrt(BatteryBayCollarRadius*BatteryBayCollarRadius-midX*midX);
            var support=Box("Akkubucht / angebundener Kragenquertraeger",parent,new Vector3(midX,ceiling?4:-.20f,(back+front)/2),new Vector3(x1-x0,.16f,.12f),frame,0,false);
            if(ceiling)roofs.Add(support.gameObject);
            void CollarLayer(string name,float low,float high,Material surface,bool collide)
            {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d){int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});triangles.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});}
            for(int i=0;i<8;i++)
            {
                float a=Mathf.Lerp(x0,x1,i/8f),b=Mathf.Lerp(x0,x1,(i+1)/8f);
                float za=Mathf.Sqrt(BatteryBayCollarRadius*BatteryBayCollarRadius-a*a),zb=Mathf.Sqrt(BatteryBayCollarRadius*BatteryBayCollarRadius-b*b);
                Vector3 al=new Vector3(a,low,back),bl=new Vector3(b,low,back),ar=new Vector3(a,low,za),br=new Vector3(b,low,zb),up=Vector3.up*(high-low);
                Quad(al,ar,br,bl);Quad(al+up,bl+up,br+up,ar+up);Quad(al,bl,bl+up,al+up);Quad(ar,ar+up,br+up,br);
                Quad(al,al+up,ar+up,ar);Quad(bl,br,br+up,bl+up);
            }
            var mesh=new Mesh{name="Akkubucht gekruemmter Ringanschluss"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();StationSurfaceMaterials.WorldUvs(mesh);meshes.Add(mesh);
            var node=Child(name,parent);
            node.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;node.gameObject.AddComponent<MeshRenderer>().sharedMaterial=surface;
            if(collide)node.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh;
            if(ceiling)roofs.Add(node.gameObject);
            }
        }
    }
}
