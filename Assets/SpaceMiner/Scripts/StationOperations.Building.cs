using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    public sealed partial class StationOperations
    {
        private readonly List<Mesh> constructedMeshes=new List<Mesh>();
        internal int OwnedMeshCount=>ownedMeshes.Count;
        private float ExpansionAngle(StationElementState e){var bits=e.id.Split('-');return e.id.StartsWith("water-")?RingWalkthrough.StorageAngle(int.Parse(bits[1])):e.sector*30+(bits.Length>2?(int.Parse(bits[2])-1)*8.5f:0);}
        private Transform WedgeSkin(string name,Transform root,float width,float depth,Func<Vector2,float,Vector3> mapping,bool collide)
        {
            var mesh=HoneycombHull.Mesh(name,HoneycombHull.Cells(width,depth),mapping);ownedMeshes.Add(mesh);constructedMeshes.Add(mesh);var child=Group(name,root,Vector3.zero,Quaternion.identity);child.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;child.gameObject.AddComponent<MeshRenderer>().sharedMaterials=new[]{alloy,graphite};if(collide){child.gameObject.layer=25;child.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh;}return child;
        }
        private void BuildExpansion(string id)
        {
            if(expansions.ContainsKey(id))return;var e=Element(id);bool storage=id.StartsWith("water-");float height=storage?5.5f:4.4f,axis=ExpansionAngle(e),half=storage?4.85f:4.05f,ri=38.3f,ro=storage?54:46.3f,span=half*2*Mathf.Deg2Rad,mid=(ri+ro)*.5f;
            var root=Group("Passgenaues Versorgungsmodul / "+id,presentation,Vector3.zero,Quaternion.identity);Selectable(root,id);expansions[id]=root.gameObject;
            WedgeSkin("Druckboden",root,mid*span,ro-ri,(p,d)=>RingWalkthrough.At(ri+p.y,axis-half+p.x/mid*Mathf.Rad2Deg,-.04f+d),true);
            WedgeSkin("Boden-Außenschutz",root,mid*span,ro-ri,(p,d)=>RingWalkthrough.At(ri+p.y,axis-half+p.x/mid*Mathf.Rad2Deg,-.4f-d),false);
            foreach(float a in new[]{axis-half,axis+half}){
                Vector3 tangent=Quaternion.Euler(0,a,0)*Vector3.right*(a<axis?-1:1);
                WedgeSkin("Druckwand",root,ro-ri,height,(p,d)=>RingWalkthrough.At(ri+p.x,a,p.y)+tangent*d,true);
                WedgeSkin("Wand-Außenschutz",root,ro-ri,height,(p,d)=>RingWalkthrough.At(ri+p.x,a,p.y)+tangent*(.3f+d),false);
                Line("Seitenrahmen",root,RingWalkthrough.At(ri,a,height+.2f),RingWalkthrough.At(ro,a,height+.2f),.18f,alloy);
                foreach(float r in new[]{ri,mid,ro})Shape("Tragende Abstandsrippe",root,RingWalkthrough.At(r,a,height+.2f),new Vector3(.2f,.4f,.2f),alloy);
            }
            WedgeSkin("Druckdecke",root,mid*span,ro-ri,(p,d)=>RingWalkthrough.At(ri+p.y,axis-half+p.x/mid*Mathf.Rad2Deg,height-d),true);
            WedgeSkin("Dach-Außenschutz",root,mid*span,ro-ri,(p,d)=>RingWalkthrough.At(ri+p.y,axis-half+p.x/mid*Mathf.Rad2Deg,height+.4f+d),false);
            WedgeSkin("Druckstirnwand",root,ro*span,height,(p,d)=>RingWalkthrough.At(ro+d,axis-half+p.x/ro*Mathf.Rad2Deg,p.y),true);
            WedgeSkin("Stirn-Außenschutz",root,ro*span,height,(p,d)=>RingWalkthrough.At(ro+.3f+d,axis-half+p.x/ro*Mathf.Rad2Deg,p.y),false);
            var connector=Group("Bodenschwelle / bündiger Anschluss",root,RingWalkthrough.At(38.2f,axis),Quaternion.Euler(0,axis,0));Shape("Übergangsplatte",connector,new Vector3(0,-.04f,0),new Vector3(2.6f,.08f,1.2f),alloy,true);
            RefreshExpansionProgress(id);
        }
        private void RefreshExpansionProgress(string id)
        {
            if(!expansions.TryGetValue(id,out var root))return;var state=Element(id);var job=State.orders.Find(o=>o.kind==StationJobKind.Expand&&o.target==id&&o.started&&!o.complete);
            float progress=state.built?1:job==null?0:Mathf.Clamp01((job.elapsed/StationRecipes.Get(job.kind).Seconds-.25f)/.5f);
            int index=0,total=root.transform.childCount;foreach(Transform child in root.transform){child.gameObject.SetActive(progress>=(float)index/total);foreach(var collider in child.GetComponentsInChildren<Collider>(true))collider.enabled=state.built;index++;}
            if(state.built){OpenModuleAccess(state);State.inventory.scrap.capacity=150+50*State.elements.FindAll(e=>e.id.StartsWith("expansion-")&&e.built).Count;}
        }
        private readonly HashSet<string> openedPorts=new HashSet<string>();
        private sealed class OriginalWall {internal MeshFilter Filter;internal Mesh Mesh;internal bool HadCollider,WasBox;internal Vector3 Center,Size;}
        private readonly List<OriginalWall> originalWalls=new List<OriginalWall>();
        private readonly List<StationPressureGate> addedGates=new List<StationPressureGate>();
        private readonly List<GameObject> suppressedWrecks=new List<GameObject>();
        private void RepairRingShell(StationElementState e)
        {
            string id="repair-deck-"+e.sector;if(expansions.ContainsKey(id))return;float axis=e.sector*30,span=30*Mathf.Deg2Rad;
            foreach(var t in Layout.Geometry.Root.GetComponentsInChildren<Transform>())if(t.name=="Ring S"+(e.sector+1).ToString("00")+" / DRUCKLOS GESPERRT"){t.gameObject.SetActive(false);suppressedWrecks.Add(t.gameObject);}
            var root=Group("Wiederhergestellte Ringhülle",presentation,Vector3.zero,Quaternion.identity);Selectable(root,e.id);expansions[id]=root.gameObject;
            foreach(bool exterior in new[]{false,true}){
                WedgeSkin(exterior?"Boden-Außenschutz":"Druckboden",root,33.5f*span,9,(p,d)=>RingWalkthrough.At(29+p.y,axis-15+p.x/33.5f*Mathf.Rad2Deg,exterior?-.4f-d:-.04f+d),!exterior);
                WedgeSkin(exterior?"Dach-Außenschutz":"Druckdecke",root,33.5f*span,9,(p,d)=>RingWalkthrough.At(29+p.y,axis-15+p.x/33.5f*Mathf.Rad2Deg,exterior?5.9f+d:5.5f-d),!exterior);
                foreach(float r in new[]{29f,38f})WedgeSkin(exterior?"Wand-Außenschutz":"Druckwand",root,r*span,5.5f,(p,d)=>RingWalkthrough.At(r+(r<33?-1:1)*(exterior?.3f+d:d),axis-15+p.x/r*Mathf.Rad2Deg,p.y),!exterior);
            }
            foreach(float angle in new[]{axis-15,axis,axis+15})Line("Tragrahmen zwischen Hülllagen",root,RingWalkthrough.At(29,angle,5.7f),RingWalkthrough.At(38,angle,5.7f),.2f,alloy);
            RefreshRingGates();
        }
        private void Suppress(string name)
        {foreach(var t in Layout.Geometry.Root.GetComponentsInChildren<Transform>())if(t.name==name){t.gameObject.SetActive(false);suppressedWrecks.Add(t.gameObject);}}
        private void RepairWaterModule(StationElementState e)
        {
            if(expansions.ContainsKey(e.id))return;int slot=int.Parse(e.id.Substring(6));Suppress("Lagerkeil "+(slot+1)+" / HAVARIERT");BuildExpansion(e.id);
            foreach(var gate in Layout.Geometry.PressureGates)if(gate!=null&&gate.Label=="Wasserlager "+(slot+1).ToString("00")+" / HAVARIE"){gate.SetSafetyLock(false);gate.Request(true,gate.transform.position+gate.transform.forward*3);gate.Advance(2,null);}
            int repaired=State.elements.FindAll(x=>x.id.StartsWith("water-")&&x.condition>=.6f).Count;Scenario.TankCapacityLiters=200+100*repaired;
            var root=expansions[e.id].transform;var tank=Shape("Zusätzlicher isolierter Wassertank / 100 L",root,RingWalkthrough.At(49,ExpansionAngle(e)+2.3f,.7f),new Vector3(.8f,.7f,.8f),blue,true,PrimitiveType.Cylinder);Selectable(tank,e.id);
        }
        private void RepairService(StationElementState e)
        {
            int axis=int.Parse(e.id.Substring(8));if(axis==0||expansions.ContainsKey(e.id))return;
            Suppress("Reaktor-Servicegang "+(axis+1));var root=Group("Wiederhergestellter Servicegang / "+axis,presentation,Vector3.zero,Quaternion.Euler(0,axis*90,0));Selectable(root,e.id);expansions[e.id]=root.gameObject;
            const float from=7.8f,to=29.2f;float mid=(from+to)*.5f,length=to-from;
            Shape("Druckboden",root,new Vector3(0,-.04f,mid),new Vector3(3.1f,.08f,length),alloy,true);
            Shape("Bodenschutz",root,new Vector3(0,-.4f,mid),new Vector3(3.7f,.12f,length),graphite);
            foreach(float side in new[]{-1f,1f}){
                Shape("Druckwand",root,new Vector3(side*1.6f,1.6f,mid),new Vector3(.16f,3.2f,length),alloy,true);
                Shape("Wand-Außenschutz",root,new Vector3(side*1.9f,1.6f,mid),new Vector3(.12f,3.2f,length),graphite);
                Line("Stromkabel",root,new Vector3(side*1.4f,2.9f,from),new Vector3(side*1.4f,2.9f,to),.07f,cableSurface);
                Line("Wasserleitung",root,new Vector3(side*1.4f,2.7f,from),new Vector3(side*1.4f,2.7f,to),.12f,blue);
            }
            Shape("Druckdecke",root,new Vector3(0,3.3f,mid),new Vector3(3.4f,.16f,length),alloy,true);Shape("Dachschutz",root,new Vector3(0,3.7f,mid),new Vector3(3.9f,.12f,length),graphite);
            for(float z=8;z<29;z+=3){foreach(float side in new[]{-1f,1f})Shape("Abstandsrahmen",root,new Vector3(side*1.75f,1.6f,z),new Vector3(.3f,3.7f,.16f),alloy);Shape("Beleuchtungsband",root,new Vector3(0,3.16f,z),new Vector3(.8f,.05f,.15f),white);}
            foreach(var gate in Layout.Geometry.PressureGates)if(gate!=null&&gate.Label=="Reaktorschott / "+(axis*90)){gate.SetSafetyLock(false);gate.Request(true,gate.transform.position+gate.transform.forward*3);gate.Advance(2,null);}
        }
        private void RefreshRingGates()
        {
            foreach(var gate in Layout.Geometry.PressureGates){if(gate==null)continue;Vector3 local=Layout.transform.InverseTransformPoint(gate.transform.position);float radius=new Vector2(local.x,local.z).magnitude;if(Mathf.Abs(radius-33.5f)>.5f)continue;float angle=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;int next=(Mathf.RoundToInt((angle+15)/30)+12)%12,previous=(next+11)%12;gate.SetSafetyLock(Element("sector-"+next).condition<.6f||Element("sector-"+previous).condition<.6f);}
        }
        private void OpenModuleAccess(StationElementState e)
        {
            if(!openedPorts.Add(e.id))return;float axis=ExpansionAngle(e);var plane=Group("Geöffneter Anschluss / "+e.id,presentation,RingWalkthrough.At(38.05f,axis),Quaternion.Euler(0,axis,0));
            foreach(var filter in Layout.Geometry.Root.GetComponentsInChildren<MeshFilter>()){
                bool wall=false;for(var parent=filter.transform;parent!=null&&parent!=Layout.Geometry.Root;parent=parent.parent)if(parent.name.Contains("Geschlossener Ausbauanschluss")||parent.name.StartsWith("Anbauslot ")||e.id.StartsWith("water-")&&parent.name.StartsWith("Ringwand Lager")){wall=true;break;}if(!wall)continue;
                var before=filter.sharedMesh;var mesh=StationAperture.Cut(filter,plane,1.25f,3.2f);if(mesh==null)continue;ownedMeshes.Add(mesh);constructedMeshes.Add(mesh);filter.sharedMesh=mesh;
                var collider=Array.Find(filter.GetComponents<Collider>(),c=>c.enabled);if(!originalWalls.Exists(w=>w.Filter==filter)){var box=collider as BoxCollider;originalWalls.Add(new OriginalWall{Filter=filter,Mesh=before,HadCollider=collider!=null,WasBox=box!=null,Center=box!=null?box.center:Vector3.zero,Size=box!=null?box.size:Vector3.zero});}
                if(collider!=null){collider.enabled=false;Destroy(collider);if(mesh.vertexCount>=3){var replaced=filter.gameObject.AddComponent<MeshCollider>();replaced.sharedMesh=mesh;}}
            }
            var gate=plane.gameObject.AddComponent<StationPressureGate>();gate.Configure(2.5f,3.2f,false,"Modulschott / "+e.name,alloy,cyan);Layout.Geometry.PressureGates.Add(gate);addedGates.Add(gate);
        }
        private void ClearBuiltPresentation()
        {
            foreach(var wreck in suppressedWrecks)if(wreck!=null)wreck.SetActive(true);suppressedWrecks.Clear();
            foreach(var gate in addedGates)if(gate!=null){Layout.Geometry.PressureGates.Remove(gate);gate.gameObject.SetActive(false);Destroy(gate.gameObject);}addedGates.Clear();openedPorts.Clear();
            foreach(var wall in originalWalls)if(wall.Filter!=null&&wall.Mesh!=null){wall.Filter.sharedMesh=wall.Mesh;foreach(var c in wall.Filter.GetComponents<Collider>()){c.enabled=false;Destroy(c);}if(wall.HadCollider){if(wall.WasBox){var box=wall.Filter.gameObject.AddComponent<BoxCollider>();box.center=wall.Center;box.size=wall.Size;}else wall.Filter.gameObject.AddComponent<MeshCollider>().sharedMesh=wall.Mesh;}}
            originalWalls.Clear();foreach(var pair in expansions)if(pair.Value!=null){pair.Value.SetActive(false);Destroy(pair.Value);}expansions.Clear();
            foreach(var mesh in constructedMeshes){ownedMeshes.Remove(mesh);if(mesh!=null)Destroy(mesh);}constructedMeshes.Clear();
        }
        private void ReleaseBuiltMeshes(GameObject root)
        {foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true)){var mesh=filter.sharedMesh;if(!constructedMeshes.Remove(mesh))continue;ownedMeshes.Remove(mesh);Destroy(mesh);}}
        internal bool ContainsExpansion(Vector3 world)
        {
            Vector3 p=Layout.transform.InverseTransformPoint(world);float r=new Vector2(p.x,p.z).magnitude;if(p.y<-.2f||p.y>.5f)return false;float a=Mathf.Atan2(p.x,p.z)*Mathf.Rad2Deg;
            foreach(var e in State.elements)if(e.id.StartsWith("service-")&&e.condition>=.6f){int axis=int.Parse(e.id.Substring(8));Vector3 local=Quaternion.Euler(0,-axis*90,0)*p;if(local.z>=7.8f&&local.z<29.4f&&Mathf.Abs(local.x)<1.18f)return true;}
            foreach(var e in State.elements)if(e.id.StartsWith("water-")&&e.condition>=.6f&&r>=37.6f&&r<53.5f&&Mathf.Abs(Mathf.DeltaAngle(a,ExpansionAngle(e)))<4.3f)return true;
            if(r<37.6f||r>45.9f)return false;
            foreach(var e in State.elements)if(e.id.StartsWith("expansion-")&&e.built&&Mathf.Abs(Mathf.DeltaAngle(a,ExpansionAngle(e)))<3.6f)return true;return false;
        }
    }
}
