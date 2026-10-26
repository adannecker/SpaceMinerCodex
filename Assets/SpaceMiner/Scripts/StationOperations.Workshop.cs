using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceMiner
{
    public sealed partial class StationOperations
    {
        private Transform workshop, scrapService, workshopCell;
        private readonly List<WorkshopArm> workshopArms=new List<WorkshopArm>();
        private readonly List<Transform> workshopToolStock=new List<Transform>();
        private readonly HashSet<Transform> workshopMoving=new HashSet<Transform>();
        private readonly List<Renderer> scrapContents=new List<Renderer>();
        private sealed class WorkshopArm
        {
            public int cell,side;
            public Transform lift,shoulder,elbow,wrist;
            public float extension;
        }
        internal int WorkshopCellCount=>2;
        internal int FunctionalWorkshopCells=>Powered(1)&&State.power.JobPowered&&Element("hall").condition>=.3f?1:0;
        internal int WorkshopArmCount=>workshopArms.Count;
        internal Transform WorkshopRoot=>workshop;
        internal Vector3 WorkshopServicePoint(StationJobKind kind)
            =>(kind==StationJobKind.Recycle?scrapService:workshopCell).position;
        internal bool ExcludeWorkshopDynamic(MeshFilter filter)
        {for(var t=filter.transform;t!=null&&t!=presentation;t=t.parent)if(workshopMoving.Contains(t))return true;return false;}

        private void BuildWorkshopInterior()
        {
            workshop=Group("Drohnenwerkstatt / zwei Umbaustationen",presentation,RingWalkthrough.At(38,30),Quaternion.Euler(0,30,0));
            RelocateAuthoredWorkshopFurniture();
            foreach(float side in new[]{-1f,1f})
            {
                var sign=Group("Geschlossenes Reservetor / Display",presentation,RingWalkthrough.At(53.5f,30+side*6.5f,4.5f),Quaternion.Euler(0,30+side*6.5f,0));
                Text("RESERVETOR / GESPERRT",sign,Vector3.zero,.035f);
            }
            for(int cell=0;cell<2;cell++)BuildWorkshopCell(cell,new Vector3(cell==0?-4.3f:4.3f,0,8.1f));
            // Each sloping side wall receives a rack. Nine actual stock mounts, not a picture of stock.
            var rack=Group("Wandregal / 3 Bohrer 3 Greifer 3 Laser",workshop,new Vector3(9.3f,0,3.1f),Quaternion.Euler(0,76.5f,0));Selectable(rack,"hall");
            Shape("Werkzeugwand",rack,new Vector3(0,1.85f,.16f),new Vector3(4.45f,3.5f,.14f),graphite,true);
            for(int row=0;row<3;row++){
                Shape("Wandregal Fach",rack,new Vector3(0,.33f+row*1.05f,-.2f),new Vector3(4.5f,.12f,.85f),alloy,true);
                for(int item=0;item<3;item++){
                    var root=Group((row==0?"Bohrer":row==1?"Greifer":"Laser")+" / Bestand "+(item+1),rack,new Vector3((item-1)*1.38f,.58f+row*1.05f,-.13f),Quaternion.Euler(0,90,0));
                    root.localScale=Vector3.one*.54f;var tool=root.gameObject.AddComponent<MaintenanceLaserVisual>();
                    if(row==0)tool.BuildMiningHead();else if(row==1)tool.BuildMiningArm();else tool.Build();
                    foreach(var renderer in root.GetComponentsInChildren<Renderer>())renderer.gameObject.layer=25;
                    tool.CombineStaticSurfaces();workshopToolStock.Add(root);
                }
                Text(row==0?"BOHRER / 3 AUFNAHMEN":row==1?"GREIFER / 3 AUFNAHMEN":"LASER / 3 AUFNAHMEN",rack,new Vector3(0,.17f+row*1.05f,-.65f),.035f);
            }
            BuildWallScrapStation();
        }

        private void RelocateAuthoredWorkshopFurniture()
        {
            var hall=Layout.Geometry.DetailedHall;if(hall==null)return;
            for(int side=0;side<2;side++)wallBenches[side]=Group(side==0?"Originale Wandwerkbank links":"Originale Wandwerkbank rechts",workshop,Vector3.zero,Quaternion.identity);
            foreach(var t in hall.GetComponentsInChildren<Transform>(true)){
                if(t.name.StartsWith("Lettering__",StringComparison.Ordinal)){t.gameObject.SetActive(false);continue;}
                // Replace the old single showroom cradle and its independent scale drone.
                if(t.name.StartsWith("DroneStand",StringComparison.Ordinal)||t.name.StartsWith("CradleGuide",StringComparison.Ordinal)
                    ||t.name.StartsWith("COL_DroneStand",StringComparison.Ordinal)||t.name.StartsWith("Massstabsreferenz",StringComparison.Ordinal)){
                    t.gameObject.SetActive(false);continue;
                }
                string n=t.name;
                bool bench=n.StartsWith("Workbench__",StringComparison.Ordinal)||n.StartsWith("ToolCabinet__",StringComparison.Ordinal)
                    ||n.StartsWith("ToolDrawer__",StringComparison.Ordinal)||n.StartsWith("DrawerHandle__",StringComparison.Ordinal)
                    ||n.StartsWith("BenchTools__",StringComparison.Ordinal)||n.StartsWith("UtilityStands__",StringComparison.Ordinal)
                    ||n.StartsWith("COL_Workbench__",StringComparison.Ordinal);
                if(!bench)continue;
                var filter=t.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)continue;
                // Split the two imported benches before fitting them into their room plans.
                // Their authored length runs along hall Z, not the plan's width axis X.
                SplitAuthoredBench(t,filter.sharedMesh,hall.transform);t.gameObject.SetActive(false);
            }
        }

        private void BuildWorkshopCell(int index,Vector3 position)
        {
            var cell=Group("Umbauplatz "+(index+1).ToString("00")+(index==0?" / betriebsbereit":" / offline"),workshop,position,Quaternion.identity);Selectable(cell,"hall");
            Shape("Versenkte Serviceplattform",cell,new Vector3(0,.065f,0),new Vector3(3.55f,.13f,4),graphite,true);
            foreach(float side in new[]{-1f,1f}){
                Shape("Aufnahmeschiene",cell,new Vector3(side*1.45f,.16f,0),new Vector3(.14f,.08f,3.6f),alloy);
                Shape("Werkplatzmarkierung",cell,new Vector3(side*1.88f,.012f,0),new Vector3(.06f,.02f,4.5f),index==0?cyan:amber);
            }
            // The supply manifold grows from the floor, away from the central through-route.
            var supply=Group("Bodenversorgung / Strom Wasser Daten",cell,new Vector3(0,0,-2.9f),Quaternion.identity);
            Shape("Bodendurchführung",supply,new Vector3(0,.08f,0),new Vector3(2.8f,.16f,.65f),graphite,true);
            foreach(float x in new[]{-.55f,.55f}){
                Shape(x<0?"Wassersteigleitung":"Geschützter Stromkanal",supply,new Vector3(x,.78f,0),new Vector3(.15f,1.4f,.15f),x<0?blue:cableSurface,true);
                Shape("Serviceflansch",supply,new Vector3(x,.28f,0),new Vector3(.3f,.12f,.3f),alloy);
            }
            Shape("Versorgungssäule",supply,new Vector3(0,.85f,0),new Vector3(1.9f,.65f,.5f),alloy,true);
            Shape("Arbeitsplatzsignal",supply,new Vector3(0,1.08f,-.275f),new Vector3(1.45f,.08f,.04f),index==0?cyan:amber);
            Text(index==0?"UMBAU 01 / R-01": "UMBAU 02 / OFFLINE",supply,new Vector3(0,1.45f,0),.035f);
            if(index==1){
                Shape("Fehlende Steuerkassette",supply,new Vector3(0,.83f,-.29f),new Vector3(.65f,.32f,.08f),graphite);
                Text("STEUERUNG DEFEKT",cell,new Vector3(0,2.9f,0),.045f);
            }
            for(int side=-1;side<=1;side+=2){BuildFloorRobot(cell,index,side);BuildToolChanger(cell,side);}
            if(index==0)workshopCell=Group("R-01 Übergabepunkt / Umbau01",cell,new Vector3(2.75f,1.25f,-3.1f),Quaternion.identity);
        }

        private void BuildFloorRobot(Transform cell,int index,int side)
        {
            var baseRoot=Group(side<0?"Roboter links / Bodenaufnahme":"Roboter rechts / Bodenaufnahme",cell,new Vector3(side*2.03f,0,-.25f),Quaternion.identity);
            Shape("Geschlossener Bodensockel",baseRoot,new Vector3(0,.14f,0),new Vector3(.65f,.28f,.85f),alloy,true);
            Shape("Teleskop-Manschette",baseRoot,new Vector3(0,.38f,0),new Vector3(.42f,.55f,.42f),graphite);
            var lift=Group("Ausfahrbarer Roboterhub",baseRoot,new Vector3(0,.45f,0),Quaternion.identity);workshopMoving.Add(lift);
            Shape("Hubkolben",lift,new Vector3(0,.4f,0),new Vector3(.22f,.8f,.22f),alloy);
            Shape("Abgedichteter Hubkopf",lift,new Vector3(0,.87f,0),new Vector3(.44f,.22f,.5f),blue);
            var shoulder=Group("Schultergelenk",lift,new Vector3(0,.88f,0),Quaternion.identity);
            Joint(shoulder,Vector3.zero,.38f);
            Shape("Oberarm",shoulder,new Vector3(0,.52f,0),new Vector3(.22f,1.03f,.3f),alloy);
            Line("Oberarm Hydraulik",shoulder,new Vector3(.15f,.1f,.14f),new Vector3(.15f,.9f,.14f),.07f,blue);
            var elbow=Group("Ellbogengelenk",shoulder,new Vector3(0,1.02f,0),Quaternion.identity);
            Joint(elbow,Vector3.zero,.31f);
            Shape("Unterarm",elbow,new Vector3(0,.46f,0),new Vector3(.18f,.9f,.24f),alloy);
            Line("Flexibler Versorgungskanal",elbow,new Vector3(0,.1f,.18f),new Vector3(0,.83f,.18f),.055f,cableSurface);
            var wrist=Group("Universal-Werkzeugkupplung",elbow,new Vector3(0,.9f,0),Quaternion.identity);
            Joint(wrist,Vector3.zero,.22f);BuildSimpleEffector(wrist,side<0?0:1);
            workshopArms.Add(new WorkshopArm{cell=index,side=side,lift=lift,shoulder=shoulder,elbow=elbow,wrist=wrist});
            // Each articulation retains its own local batch; no animated part is baked into station infrastructure.
            CombineRigidWorkshopParts(wrist);CombineRigidWorkshopParts(elbow);CombineRigidWorkshopParts(shoulder);CombineRigidWorkshopParts(lift);
        }

        private void Joint(Transform parent,Vector3 position,float diameter)
        {
            var joint=Shape("Abgedichteter Gelenkantrieb",parent,position,new Vector3(diameter,diameter*.45f,diameter),graphite,false,PrimitiveType.Cylinder);
            joint.localRotation=Quaternion.Euler(90,0,0);
            Shape("Gelenkdeckel",parent,position+Vector3.forward*diameter*.26f,new Vector3(diameter*.7f,diameter*.7f,.04f),alloy);
        }
        private void BuildToolChanger(Transform cell,int side)
        {
            var rack=Group("Automatischer Werkzeugwechsel "+(side<0?"links":"rechts"),cell,new Vector3(side*1.8f,.75f,-2.9f),Quaternion.identity);
            Shape("Wechselmagazin",rack,new Vector3(0,.6f,0),new Vector3(.82f,1.6f,.48f),graphite,true);
            for(int tool=0;tool<3;tool++){
                var holder=Group(tool==0?"Greifwerkzeug":tool==1?"Schweißwerkzeug":"Reparatursonde",rack,new Vector3(0,.13f+tool*.5f,.28f),Quaternion.Euler(90,0,0));
                Shape("Schnellkupplung",holder,Vector3.zero,new Vector3(.3f,.14f,.3f),alloy);BuildSimpleEffector(holder,tool);
            }
            Text("WERKZEUGWECHSEL",rack,new Vector3(0,1.7f,0),.023f);
        }
        private void BuildSimpleEffector(Transform parent,int kind)
        {
            var root=Group("Werkzeug / "+kind,parent,new Vector3(0,.14f,0),Quaternion.identity);
            Shape("Kupplungsring",root,new Vector3(0,.08f,0),new Vector3(.19f,.16f,.19f),blue,false,PrimitiveType.Cylinder);
            if(kind==0){foreach(float side in new[]{-1f,1f}){
                Shape("Greiffinger",root,new Vector3(side*.16f,.3f,0),new Vector3(.07f,.35f,.12f),alloy);
                Shape("Greifbacke",root,new Vector3(side*.105f,.48f,0),new Vector3(.14f,.08f,.14f),graphite);
            }}else{
                Shape(kind==1?"Schweißkopf":"Diagnosekopf",root,new Vector3(0,.25f,0),new Vector3(.16f,.24f,.2f),alloy);
                Shape(kind==1?"Schweißdüse":"Messsonde",root,new Vector3(0,.45f,0),new Vector3(.055f,.22f,.055f),kind==1?amber:cyan);
            }
        }
        private void CombineRigidWorkshopParts(Transform frame)
        {
            foreach(var surface in new[]{alloy,graphite,blue,cyan,amber,cableSurface}){
                var combine=new List<CombineInstance>();var sources=new List<Renderer>();
                foreach(Transform child in frame){var filter=child.GetComponent<MeshFilter>();var renderer=child.GetComponent<MeshRenderer>();
                    if(filter==null||renderer==null||!renderer.enabled||renderer.sharedMaterial!=surface)continue;
                    combine.Add(new CombineInstance{mesh=filter.sharedMesh,transform=frame.worldToLocalMatrix*child.localToWorldMatrix});sources.Add(renderer);
                }
                if(combine.Count<2)continue;var mesh=new Mesh{name="Roboter / starre Gelenkbauteile",indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(combine.ToArray(),true,true);ownedMeshes.Add(mesh);
                var batch=Group(mesh.name,frame,Vector3.zero,Quaternion.identity);batch.gameObject.layer=25;batch.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;batch.gameObject.AddComponent<MeshRenderer>().sharedMaterial=surface;
                foreach(var renderer in sources)renderer.enabled=false;
            }
        }

        private void BuildWallScrapStation()
        {
            var root=Group("Wandstation / Schrottanalyse und Annahme",workshop,new Vector3(-10.8f,0,10.2f),Quaternion.Euler(0,-76.5f,0));Selectable(root,"scrap");
            Shape("Analyseschrank",root,new Vector3(-1.1f,1.05f,.15f),new Vector3(2.1f,2.1f,1.2f),alloy,true);
            Shape("Geschlossene Prozesskammer",root,new Vector3(-1.1f,1.05f,-.5f),new Vector3(1.7f,.85f,.08f),graphite);
            Shape("Anlieferfach R-01",root,new Vector3(-1.1f,.65f,-.78f),new Vector3(1.7f,.12f,.6f),blue,true);
            Shape("Analysedisplay",root,new Vector3(-1.1f,1.8f,-.49f),new Vector3(1.4f,.22f,.05f),cyan);
            Text("SCHROTTANALYSE / R-01",root,new Vector3(-1.1f,2.55f,0),.035f);
            var bin=Group("Unidentifizierter Schrott / geschlossener Behälter",root,new Vector3(1.3f,0,0),Quaternion.identity);Selectable(bin,"scrap");
            Shape("Geschlossener Annahmebehälter",bin,new Vector3(0,.65f,0),new Vector3(1.9f,1.3f,1.35f),graphite,true);
            Shape("Dichter Einfüllverschluss",bin,new Vector3(0,1.34f,0),new Vector3(1.85f,.13f,1.3f),alloy,true);
            Shape("Füllstandfenster",bin,new Vector3(0,.8f,-.69f),new Vector3(1.35f,.52f,.04f),blue);
            for(int i=0;i<5;i++){
                var contents=Shape("Schrott-Füllstand "+i,bin,new Vector3((i-2)*.24f,.8f,-.72f),new Vector3(.18f,.36f,.035f),amber);
                scrapContents.Add(contents.GetComponent<Renderer>());workshopMoving.Add(contents);
            }
            Text("? / UNIDENTIFIZIERT",bin,new Vector3(0,1.9f,0),.035f);
            scrapService=Group("R-01 / Schrott Übergabepunkt",root,new Vector3(-1.1f,1.25f,-1.25f),Quaternion.identity);
        }
        private void RefreshWorkshopVisuals()
        {
            int[] stock={State.spareDrills,State.spareGrippers,State.spareLasers};
            for(int i=0;i<workshopToolStock.Count;i++)workshopToolStock[i].gameObject.SetActive(i%3<stock[i/3]);
            float fill=State.inventory.scrap.amount/State.inventory.scrap.capacity;
            for(int i=0;i<scrapContents.Count;i++)scrapContents[i].enabled=fill>(i+.01f)/scrapContents.Count;
            AdvanceWorkshopVisuals(0);
        }
        private void AdvanceWorkshopVisuals(float seconds)
        {
            var order=State.orders.Find(o=>o.kind==StationJobKind.Refit&&o.started&&!o.complete&&!o.cancelled&&WorkshopRefitState.HasData(o.workshop)&&o.workshop.stage==3);
            bool working=order!=null&&Powered(1)&&State.power.JobPowered;
            foreach(var arm in workshopArms){
                float target=arm.cell==0&&working?1:0;arm.extension=Mathf.MoveTowards(arm.extension,target,Mathf.Min(seconds,.1f)*.8f);
                float p=arm.extension,wave=working?Mathf.Sin(order.elapsed*.55f+arm.side)*4*p:0;
                arm.lift.localPosition=new Vector3(0,.1f+p*.45f,0);
                arm.shoulder.localRotation=Quaternion.Euler(0,0,arm.side*(10+25*p)+wave);
                arm.elbow.localRotation=Quaternion.Euler(-15*p,0,-arm.side*(160-205*p)-wave*.5f);
                arm.wrist.localRotation=Quaternion.Euler(0,order==null?0:order.elapsed*10,arm.side*45*p);
            }
        }
    }
}
