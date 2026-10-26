using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    public sealed partial class StationOperations
    {
        private readonly List<CommandRoomLayout> roomPlans = new List<CommandRoomLayout>();
        public IReadOnlyList<CommandRoomLayout> RoomPlans => roomPlans;
        private Transform primaryRepairCell, helperDock;
        private readonly Transform[] wallBenches = new Transform[2];
        private readonly List<Transform> materialStores = new List<Transform>();

        private string RoomPlanPath(string id)
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args,"-stationRoomPlanningCheck") >= 0)
                return Path.GetFullPath("Logs/StationRoomPlanning/" + id + "-layout.json");
            if (Array.IndexOf(args,"-stationDebrisCheck") >= 0)
                return Path.GetFullPath("Logs/StationDebris/" + id + "-layout.json");
            if (Array.IndexOf(args,"-stationHandsCheck") >= 0)
                return Path.GetFullPath("Logs/StationHands/" + id + "-layout.json");
            if (Array.IndexOf(args,"-stationConsoleTouchCheck") >= 0)
                return Path.GetFullPath("Logs/StationConsoleTouch/" + id + "-layout.json");
            if (Array.IndexOf(args,"-stationDroneSizingCheck") >= 0)
                return Path.GetFullPath("Logs/DroneSizing/" + id + "-layout.json");
            if (Array.IndexOf(args,"-stationIcePlantCheck") >= 0)
                return Path.GetFullPath("Logs/StationIcePlant/" + id + "-layout.json");
            if (Array.IndexOf(args,"-stationDroneConsoleCheck") >= 0)
                return Path.GetFullPath("Logs/StationDroneConsole/" + id + "-layout.json");
            if (Array.IndexOf(args,"-stationTacticalCheck") >= 0)
                return Path.GetFullPath("Logs/StationTactical/" + id + "-layout.json");
            if (Array.IndexOf(args,"-stationCameraVerticalCheck") >= 0)
                return Path.GetFullPath("Logs/CameraVertical/" + id + "-layout.json");
            if (Array.IndexOf(args,"-stationPowerConsoleCheck") >= 0)
                return Path.GetFullPath("Logs/StationPowerConsole/" + id + "-layout.json");
            if (Array.IndexOf(args,"-stationIndoorTrafficCheck") >= 0)
                return Path.GetFullPath("Logs/StationIndoorTraffic/" + id + "-layout.json");
            if (Array.IndexOf(args,"-stationBatteryBayCheck") >= 0)
                return Path.GetFullPath("Logs/StationBatteryBays/" + id + "-layout.json");
            if (Array.IndexOf(args,"-commandRoomPlannerCheck") >= 0)
                return Path.GetFullPath("Logs/CommandRoomPlannerCheck/" + (id == "command-room" ? "command-room-layout.json" : id + "-layout.json"));
            if (Array.Exists(args,a => a.EndsWith("Check",StringComparison.OrdinalIgnoreCase)))
                return Path.GetFullPath("Logs/RoomPlanAcceptance/" + id + "-layout.json");
            return null;
        }

        private Transform RoomFrame(string name)
            => Group(name,presentation,Vector3.zero,Quaternion.Euler(0,30,0));

        private static CommandRoomLayoutItem RoomItem(string id,string label,float x,float z,float w,float h,float d,float yaw=0)
            => new CommandRoomLayoutItem { id=id,label=label,position=new Vector3(x,0,z),size=new Vector3(w,h,d),yaw=yaw };
        private static RoomKeepout Lane(string label,float x,float z,float w,float d)
            => new RoomKeepout { label=label,center=new Vector2(x,z),size=new Vector2(w,d) };

        private CommandRoomLayout InstallRoom(Transform frame,RoomDefinition definition)
        {
            definition.MaximumHeight=5.1f;
            if(definition.Id=="drone-bay")definition.MinimumItemSize=new Vector3(3.4f,3.4f,5.4f);
            var plan=frame.gameObject.AddComponent<CommandRoomLayout>();
            plan.InitializeForRoom(frame,definition,RoomPlanPath(definition.Id));roomPlans.Add(plan);
            return plan;
        }

        private void BuildRoomPlanning()
        {
            roomPlans.Clear();roomPlans.Add(RoomPlan);
            RoomPlan.EditGuard=item=>State.orders.Exists(o=>o.started&&!o.complete&&o.actorId=="R-01")
                ? "R-01 arbeitet. Raumplan erst nach dem Auftrag ändern." : null;
            BuildBayPlan();BuildWorkshopPlan();BuildInternalPlan();
            var planner=Layout.GetComponent<CommandRoomPlanner>() ?? Layout.gameObject.AddComponent<CommandRoomPlanner>();
            planner.ConfigureRooms(roomPlans);
        }

        private void BuildWorkshopPlan()
        {
            var frame=RoomFrame("Raumplanung / Drohnenwerkstatt");
            var items=new[] {
                RoomItem("repair-01","UMBAUPLATZ 01",-4.3f,46.1f,5,4.3f,7.4f),
                RoomItem("repair-02","UMBAUPLATZ 02 / OFFLINE",4.3f,46.1f,5,4.3f,7.4f),
                RoomItem("tool-rack","WERKZEUGREGAL",8.4f,42,4.8f,3.8f,1.1f,103.5f),
                RoomItem("scrap-analysis","SCHROTTANALYSE",-10,48.8f,2.5f,2.8f,1.5f,-103.5f),
                RoomItem("scrap-bin","SCHROTTBEHÄLTER",-10.5f,51.7f,1.9f,2,1.4f,-103.5f),
                RoomItem("bench-left","WERKBANK LINKS",-5,52.1f,3.1f,1.5f,1),
                RoomItem("bench-right","WERKBANK RECHTS",5,52.1f,3.1f,1.5f,1)
            };
            var plan=InstallRoom(frame,RoomDefinition.Annular("drone-workshop","Drohnenwerkstatt",38,54,13.5f,items,
                Lane("Drohnenfahrspur / Zugang zur Schleuse",0,46,3.4f,16)));
            var cells=new[] { ChildNamed(workshop,"Umbauplatz 01 / betriebsbereit"),ChildNamed(workshop,"Umbauplatz 02 / offline") };
            primaryRepairCell=cells[0];
            for(int i=0;i<2;i++)plan.RegisterObject(items[i].id,cells[i]);
            var rack=ChildNamed(workshop,"Wandregal / 3 Bohrer 3 Greifer 3 Laser");
            FitRoomObject(rack,frame,items[2]);plan.RegisterObject(items[2].id,rack);
            var analysis=ChildNamed(workshop,"Wandstation / Schrottanalyse und Annahme");
            var bin=ChildNamed(analysis,"Unidentifizierter Schrott / geschlossener Behälter");
            bin.SetParent(workshop,true);
            FitRoomObject(analysis,frame,items[3]);FitRoomObject(bin,frame,items[4]);
            plan.RegisterObject(items[3].id,analysis);plan.RegisterObject(items[4].id,bin);
            for(int i=0;i<2;i++) {
                if(wallBenches[i]==null)throw new InvalidOperationException("Importierte Wandwerkbank fehlt.");
                FitRoomObject(wallBenches[i],frame,items[5+i]);plan.RegisterObject(items[5+i].id,wallBenches[i]);
            }
            plan.EditGuard=item => State.orders.Exists(o=>o.started&&!o.complete&&o.actorId=="R-01" || o.started&&!o.complete&&o.kind==StationJobKind.Refit)
                ? "Werkstatt wird benutzt. Auftrag erst abschließen oder sicher abbrechen." : null;
        }

        private void BuildInternalPlan()
        {
            var frame=RoomFrame("Raumplanung / interne Drohnenwerkstatt");
            var inner=FindGeometry("Interne Drohnen / Reparaturplatz");
            var pad=inner.Find("R01 Ladepodest");
            helperDock=Group("R-01 / verschiebbarer Heimanker",frame,new Vector3(-3,0,24.5f),Quaternion.identity);
            pad.SetParent(helperDock,true);
            var bench=Group("Interne Reparaturbank / Raumgruppe",frame,new Vector3(3.5f,0,24.5f),Quaternion.identity);
            inner.Find("Interne Reparaturbank").SetParent(bench,true);inner.Find("Werkbank Deckplatte").SetParent(bench,true);
            var facility=FindGeometry("Defekte Anlage");
            var items=new List<CommandRoomLayoutItem> {
                RoomItem("helper-dock","R-01 LADEPLATZ",-3,24.5f,1.2f,1.2f,1.2f),
                RoomItem("repair-bench","INTERNE REPARATURBANK",3.5f,24.5f,2.4f,1.1f,2),
                RoomItem("offline-facility","DEFEKTE ANLAGE",0,20.4f,2.3f,1.6f,2)
            };
            string[] ids={"carbon-store","iron-store","unknown-store"},labels={"C / KONZENTRAT","Fe / EISEN","? / UNBEKANNT"};
            for(int i=0;i<3;i++) {
                Vector3 p=frame.InverseTransformPoint(materialStores[i].position);
                items.Add(RoomItem(ids[i],labels[i],p.x,p.z,1.15f,1.5f,1.2f));
            }
            var plan=InstallRoom(frame,RoomDefinition.Annular("internal-workshop","Interne Drohnenwerkstatt",18,29,13.5f,items.ToArray(),
                Lane("Zugang / interne Drohnenfahrspur",0,27,2.8f,4)));
            plan.RegisterObject("helper-dock",helperDock);plan.RegisterObject("repair-bench",bench);plan.RegisterObject("offline-facility",facility);
            for(int i=0;i<3;i++)plan.RegisterObject(ids[i],materialStores[i]);
            plan.EditGuard=item=>State.orders.Exists(o=>o.started&&!o.complete&&o.actorId=="R-01")
                ? "R-01 ist unterwegs. Internen Auftrag zuerst abschließen oder sicher abbrechen." : null;
            plan.Changed+=()=> {if(!State.orders.Exists(o=>o.started&&!o.complete&&o.actorId=="R-01"))PlaceHelperAtHome();};
            PlaceHelperAtHome();
        }

        private void BuildBayPlan()
        {
            var frame=RoomFrame("Raumplanung / Drohnenbay");
            var fleet=Scenario.GetComponent<CampaignDroneFleet>();
            var defaults=new CommandRoomLayoutItem[10];
            for(int i=0;i<10;i++) {
                var p=frame.InverseTransformPoint(fleet.BerthRoot(i).position);
                defaults[i]=RoomItem("berth-"+(i+1).ToString("00"),"DROHNENPLATZ "+(i+1).ToString("00"),p.x,p.z,3.4f,3.4f,5.4f);
            }
            var plan=InstallRoom(frame,RoomDefinition.Annular("drone-bay","Drohnenbay",60,RingWalkthrough.BayOuterRadius,13.5f,defaults,
                Lane("Zugang / Schleuse",0,62,5,4),Lane("Drohnenfahrspur quer",0,73,32,3.4f),
                Lane("Drohnenfahrspur links",-3.3f,73,3.1f,18),Lane("Drohnenfahrspur rechts",3.3f,73,3.1f,18)));
            for(int i=0;i<10;i++)plan.RegisterObject(defaults[i].id,fleet.BerthRoot(i));
            plan.EditGuard=item=> {
                for(int index=0;index<Scenario.Drones.Length;index++) {
                    var drone=Scenario.Drones[index];
                    bool docked=drone.Phase==DronePhase.Ready||drone.Phase==DronePhase.Disabled||drone.Phase==DronePhase.WaitingForPower||drone.Phase==DronePhase.Servicing;
                    if(!docked||IsRefitting(index))return "Drohnenbay im Einsatz. Alle Drohnen vor dem Umplanen sicher zurückkehren lassen.";
                }
                if(item.size.x<3.4f-.0001f||item.size.y<3.4f-.0001f||item.size.z<5.4f-.0001f)
                    return "Die vorhandene Drohne benötigt mindestens 3,4 m Breite / Höhe und 5,4 m Tiefe. Den Ladeplatz vergrößern oder verschieben.";
                return null;
            };
            plan.Changed+=fleet.RefreshPlannedBerths;fleet.RefreshPlannedBerths();
        }

        private Transform FindGeometry(string name)
        {
            foreach(var node in Layout.Geometry.Root.GetComponentsInChildren<Transform>(true))if(node.name==name)return node;
            throw new InvalidOperationException("Raumelement fehlt: "+name);
        }

        private static Transform ChildNamed(Transform parent,string name)
        {foreach(Transform child in parent)if(child.name==name)return child;throw new InvalidOperationException("Raumgruppe fehlt: "+name);}

        private Vector3 HelperHomePoint(float height)
            =>helperDock!=null?helperDock.TransformPoint(Vector3.up*height):Layout.Habitat.TransformPoint(new Vector3(-3,height,0));
        private void PlaceHelperAtHome()
        {if(Layout.RepairDrone!=null)Layout.RepairDrone.SetPositionAndRotation(HelperHomePoint(State.helper?1:.6f),helperDock!=null?helperDock.rotation:Layout.Habitat.rotation);}

        private void SplitAuthoredBench(Transform source,Mesh mesh,Transform hallFrame)
        {
            var vertices=mesh.vertices;var uv=mesh.uv;var renderer=source.GetComponent<MeshRenderer>();
            bool solid=source.GetComponent<Collider>()!=null;
            for(int side=0;side<2;side++)for(int sub=0;sub<mesh.subMeshCount;sub++) {
                var points=new List<Vector3>();var tex=new List<Vector2>();var indices=new List<int>();
                var triangles=mesh.GetTriangles(sub);
                for(int t=0;t<triangles.Length;t+=3) {
                    Vector3 mean=Vector3.zero;for(int n=0;n<3;n++)mean+=hallFrame.InverseTransformPoint(source.TransformPoint(vertices[triangles[t+n]]));
                    if((mean.x<0?0:1)!=side)continue;
                    for(int n=0;n<3;n++) {
                        int index=triangles[t+n];var p=hallFrame.InverseTransformPoint(source.TransformPoint(vertices[index]));
                        float sign=side==0?-1:1;
                        // Authored centres: x=+/-7, z=5.7. Rotate each inward-facing
                        // bench into width X, depth Z and display/front -Z without shear.
                        points.Add(new Vector3(-sign*(p.z-5.7f),p.y,sign*(p.x-sign*7)));
                        indices.Add(points.Count-1);tex.Add(uv.Length>index?uv[index]:Vector2.zero);
                    }
                }
                if(points.Count==0)continue;
                var part=new Mesh{name="Originale Werkbank / getrennte Raumgruppe"};part.SetVertices(points);part.SetUVs(0,tex);part.SetTriangles(indices,0);part.RecalculateNormals();part.RecalculateBounds();part.RecalculateTangents();ownedMeshes.Add(part);
                var node=Group(source.name+" / "+side,wallBenches[side],Vector3.zero,Quaternion.identity);node.gameObject.layer=25;
                node.gameObject.AddComponent<MeshFilter>().sharedMesh=part;
                if(renderer!=null&&renderer.enabled)node.gameObject.AddComponent<MeshRenderer>().sharedMaterial=renderer.sharedMaterials[Mathf.Min(sub,renderer.sharedMaterials.Length-1)];
                if(solid)node.gameObject.AddComponent<MeshCollider>().sharedMesh=part;
            }
        }

        // Fit the original mesh groups into their first planned footprint. Subsequent edits
        // scale the wrapper, preserving all furniture parts, colliders and service anchors.
        private static void FitRoomObject(Transform root,Transform frame,CommandRoomLayoutItem item)
        {
            root.SetParent(frame,true);root.localScale=Vector3.one;
            bool found=false;Bounds bounds=default;
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>()) {
                var renderer=filter.GetComponent<Renderer>();if(filter.sharedMesh==null||renderer==null||!renderer.enabled||filter.GetComponent<TextMesh>()!=null)continue;
                var b=filter.sharedMesh.bounds;var m=root.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                for(int n=0;n<8;n++) {var p=m.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3((n&1)==0?-1:1,(n&2)==0?-1:1,(n&4)==0?-1:1)));if(!found){bounds=new Bounds(p,Vector3.zero);found=true;}else bounds.Encapsulate(p);}
            }
            if(!found)throw new InvalidOperationException("Leere Raumgruppe: "+item.id);
            var size=bounds.size;var scale=new Vector3(item.size.x/Mathf.Max(.01f,size.x),item.size.y/Mathf.Max(.01f,size.y),item.size.z/Mathf.Max(.01f,size.z));
            root.localRotation=Quaternion.Euler(0,item.yaw,0);root.localScale=scale;
            root.localPosition=item.position-root.localRotation*Vector3.Scale(new Vector3(bounds.center.x,bounds.min.y,bounds.center.z),scale);
        }
    }
}
