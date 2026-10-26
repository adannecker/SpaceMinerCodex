using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceMiner
{
    public enum StationTerminalKind { Drones, Planning, Research, Utilities, Production, Power }
    public sealed class StationTerminal:MonoBehaviour {public StationTerminalKind kind;public string target,label;}
    public sealed class StationSelectable:MonoBehaviour {public string elementId;}
    public sealed partial class StationOperations
    {
        private Transform presentation,helperCargo;
        public CommandRoomLayout RoomPlan {get;private set;}
        private readonly List<StationTerminal> terminals=new List<StationTerminal>();
        private readonly List<Light> lamps=new List<Light>();
        private readonly Dictionary<Light,int> lampSectors=new Dictionary<Light,int>();
        private readonly List<Material> ownedMaterials=new List<Material>();
        private readonly Dictionary<string,Transform> debris=new Dictionary<string,Transform>();
        private readonly Dictionary<string,Vector3> debrisOrigins=new Dictionary<string,Vector3>();
        private readonly Dictionary<string,GameObject> expansions=new Dictionary<string,GameObject>();
        private readonly List<Mesh> ownedMeshes=new List<Mesh>();
        private readonly List<Renderer> sparkRenderers=new List<Renderer>();
        private Material alloy,graphite,blue,cyan,amber,white,cableSurface;
        private Font signageFont;
        private Material signageMaterial;
        private Color exteriorAmbient;
        private bool dimmedInterior;
        private readonly List<Light> exteriorSuns=new List<Light>();
        private readonly List<int> exteriorMasks=new List<int>();
        private Light legacyFill;
        private float lightClock;
        private string initialJson;
        public int LampCount=>lamps.Count;
        public int TerminalCount=>terminals.Count;
        private Material Material(string label,Color color,bool glow=false)
        {var m=new Material(Shader.Find("Standard")){name=label,color=color};m.SetFloat("_Metallic",glow?0:.5f);m.SetFloat("_Glossiness",.3f);m.enableInstancing=true;if(glow){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*1.7f);}ownedMaterials.Add(m);return m;}
        private Transform Group(string name,Transform parent,Vector3 position,Quaternion rotation)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;t.localRotation=rotation;return t;}
        private Transform Shape(string name,Transform parent,Vector3 position,Vector3 scale,Material surface,bool solid=false,PrimitiveType type=PrimitiveType.Cube)
        {var g=GameObject.CreatePrimitive(type);g.name=name;g.layer=25;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=surface;if(!solid){g.GetComponent<Collider>().enabled=false;Destroy(g.GetComponent<Collider>());}return g.transform;}
        private void Text(string label,Transform parent,Vector3 position,float size=.1f)
        {StationDisplay.Create(label,parent,position,Quaternion.identity,size,signageFont,signageMaterial,graphite,cyan,25);}
        private void FontRebuilt(Font font){if(font==signageFont&&signageMaterial!=null)signageMaterial.mainTexture=font.material.mainTexture;}
        private void Line(string label,Transform parent,Vector3 start,Vector3 end,float width,Material surface)
        {var t=Shape(label,parent,(start+end)/2,new Vector3(width,Vector3.Distance(start,end),width),surface);t.localRotation=Quaternion.FromToRotation(Vector3.up,end-start);}
        private void Selectable(Transform target,string id){var s=target.GetComponent<StationSelectable>()??target.gameObject.AddComponent<StationSelectable>();s.elementId=id;}
        private void AddTerminal(StationTerminalKind kind,string label,Vector3 position,float heading,string target="")
        {
            if(kind!=StationTerminalKind.Utilities&&RoomPlan!=null)
            {
                string id=CommandPlanId(kind);
                var box=CreateCommandBlock(id,string.IsNullOrEmpty(target)?"command":target);
                var control=Group("Bedienpunkt",box,Vector3.zero,Quaternion.identity);
                var terminal=control.gameObject.AddComponent<StationTerminal>();terminal.kind=kind;terminal.target=target;terminal.label=label;terminals.Add(terminal);
                return;
            }
            var root=Group(label,presentation,position,Quaternion.Euler(0,heading,0));var term=root.gameObject.AddComponent<StationTerminal>();term.kind=kind;term.target=target;term.label=label;terminals.Add(term);
            Shape("Pultsockel",root,new Vector3(0,.5f,0),new Vector3(1.2f,1,.65f),alloy,true);
            var screen=Group("Geneigter Monitor",root,new Vector3(0,1.15f,.05f),Quaternion.Euler(-15,0,0));
            Shape("Bildschirmgehäuse",screen,Vector3.zero,new Vector3(1.15f,.7f,.1f),graphite,true);
            StationDisplay.Create(label,screen,new Vector3(0,0,-.07f),Quaternion.identity,.045f,signageFont,signageMaterial,graphite,cyan,25,1.05f);
            Selectable(root,string.IsNullOrEmpty(target)?"command":target);
        }
        private void LightAt(Vector3 position,Color color,float power,float radius)
        {
            var t=Group("Adaptive Arbeitsleuchte",presentation,position,Quaternion.identity);
            var l=t.gameObject.AddComponent<Light>();l.type=LightType.Point;l.color=color;power*=2.2f;
            l.intensity=power*IdleLightingLevel;l.range=radius;l.shadows=LightShadows.None;
            l.renderMode=LightRenderMode.ForceVertex;l.cullingMask=(1<<25)|(1<<26)|(1<<30);
            lamps.Add(l);lampRatings[l]=power;
            lampSectors[l]=((Mathf.RoundToInt(Mathf.Atan2(position.x,position.z)*Mathf.Rad2Deg/30)%12)+12)%12;
            var face=Shape("Leuchtfläche",t,Vector3.zero,new Vector3(.8f,.07f,.2f),color.r>color.b?amber:white).GetComponent<Renderer>();
            // The point emitter sits inside this decorative lens; it must not shadow itself.
            face.shadowCastingMode=ShadowCastingMode.Off;lampFaces[l]=face;
        }
        private void BuildPresentation()
        {
            presentation=Group("Station / Versorgung und Aufbau",Layout.transform,Vector3.zero,Quaternion.identity);
            signageFont=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");signageMaterial=new Material(Resources.Load<Shader>("RingWalkthroughText"));signageMaterial.mainTexture=signageFont.material.mainTexture;ownedMaterials.Add(signageMaterial);Font.textureRebuilt+=FontRebuilt;
            exteriorAmbient=RenderSettings.ambientLight;foreach(var light in FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional){if(light.name=="Stations-Innenlicht")legacyFill=light;else{exteriorSuns.Add(light);exteriorMasks.Add(light.cullingMask);}}
            alloy=Material("Gemeinsame Stationslackierung",new Color(.2f,.28f,.34f));graphite=Material("Gemeinsame dunkle Anlagen",new Color(.045f,.07f,.085f));blue=Material("Wasserleitung",new Color(.1f,.33f,.48f));
            cyan=Material("Ruhige Funktionsanzeige",new Color(.12f,.65f,.72f),true);amber=Material("Notlicht und Stromtrasse",new Color(.9f,.42f,.1f),true);white=Material("Arbeitslicht diffus",new Color(.72f,.84f,.92f),true);
            cableSurface=Material("Isolierte Stromkabel",new Color(.38f,.27f,.1f));
            RoomPlan=Layout.gameObject.AddComponent<CommandRoomLayout>();
            RoomPlan.Initialize(Layout.transform,RoomPlanPath("command-room"));Layout.RoomPlan=RoomPlan;
            var main=Layout.Geometry.CommandConsole;
            RoomPlan.Register("main-console",main,main.Find("Blockoutbody"),main.GetComponentInChildren<StationDisplay>().transform);
            TouchConsoleVisual.Install(main,true).Bind(main.GetComponentInChildren<StationDisplay>(),main.Find("Blockoutbody"),RoomPlan.Get("main-console").size);
            Layout.Console.SetParent(main,false);
            RoomPlan.Changed+=UpdateCommandInteractionPoints;
            AddTerminal(StationTerminalKind.Drones,"DROHNEN",new Vector3(-10.1f,0,47),270);
            AddTerminal(StationTerminalKind.Planning,"STATIONSPLANUNG",new Vector3(10.1f,0,47),90);
            AddTerminal(StationTerminalKind.Research,"MIRA / TECHTREE",new Vector3(9.1f,0,43),90);
            AddTerminal(StationTerminalKind.Production,"FERTIGUNG",new Vector3(-9.1f,0,43),270,"printer");
            CreateCommandBlock("printer","printer");CreateCommandBlock("food","food");
            AddTerminal(StationTerminalKind.Power,"STROMVERSORGUNG",Vector3.zero,270,"power-console");
            UpdateCommandInteractionPoints();
            for(int s=0;s<12;s++)
            {
                bool damaged=RingWalkthrough.DamagedRingSector(s);float angle=s*30;
                LightAt(RingWalkthrough.At(33.5f,angle,4.9f),damaged?new Color(1,.35f,.12f):new Color(.75f,.85f,1),damaged?.5f:1.2f,10);
                for(int side=0;side<2;side++)for(int rib=0;rib<8;rib++)
                {
                    if(damaged&&rib>=2&&rib<=5)continue;
                    float a=angle-15+rib*3.75f,b=a+3.75f,r=side==0?30:37;
                    Line(side==0?"Kabeltrasse / Strom":"Isolierte Wasserleitung",presentation,RingWalkthrough.At(r,a,4.8f),RingWalkthrough.At(r,b,4.8f),side==0?.07f:.16f,side==0?cableSurface:blue);
                    if(rib%2==0)Shape("Rohrhalter",presentation,RingWalkthrough.At(r,a,4.8f),new Vector3(.24f,.4f,.16f),alloy);
                }
                var valves=Group("Absperrventil / Sektor "+s,presentation,RingWalkthrough.At(37,angle+7,4.8f),Quaternion.Euler(0,angle,0));Selectable(valves,"sector-"+s);
                Shape("Ventilblock",valves,Vector3.zero,new Vector3(.35f,.3f,.4f),alloy);
                var wheel=Shape("Verriegelbarer Ventilantrieb",valves,new Vector3(-.2f,0,0),new Vector3(.25f,.08f,.25f),amber,false,PrimitiveType.Cylinder);wheel.localRotation=Quaternion.Euler(0,0,90);
                if(damaged){var sparks=Shape("Kurze elektrische Entladung",presentation,RingWalkthrough.At(36.7f,angle-6,4.6f),new Vector3(.015f,.32f,.015f),white);sparkRenderers.Add(sparks.GetComponent<Renderer>());}
            }
            for(int i=0;i<3;i++)LightAt(new Vector3((i-1)*4,4.8f,45),new Color(.9f,.8f,.65f),1.5f,12);
            foreach(float x in new[]{-8f,8f})foreach(float z in new[]{42f,49f})LightAt(new Vector3(x,4.7f,z),new Color(1,.89f,.76f),1.35f,10);
            foreach(float r in new[]{42f,49f,59f,67f})LightAt(RingWalkthrough.At(r,30,r>54?4.8f:4.7f),new Color(.65f,.8f,1),1.2f,11);
            foreach(float a in new[]{0f,90f,180f,270f})
            {LightAt(RingWalkthrough.At(20,a,2.7f),a==0?new Color(.7f,.82f,1):new Color(1,.4f,.15f),.65f,8);Shape("Außenposition",presentation,RingWalkthrough.At(54,a,5.9f),new Vector3(.25f,.12f,.25f),cyan);}
            BuildWorkshopInterior();BuildStores();BuildRoomPlanning();BuildDebris();BuildBatteryBays();
            foreach(var collider in Layout.Geometry.Root.GetComponentsInChildren<Collider>())
            {
                if(collider.GetComponentInParent<StationSelectable>()!=null)continue;
                Vector3 p=Layout.transform.InverseTransformPoint(collider.bounds.center);int sector=((Mathf.RoundToInt(Mathf.Atan2(p.x,p.z)*Mathf.Rad2Deg/30)%12)+12)%12;
                string id=p.magnitude>38&&sector==0?"command":p.magnitude>38&&sector==1?"hall":p.magnitude>38&&sector==7?"water":"sector-"+sector;
                Selectable(collider.transform,id);
            }
            foreach(var part in Layout.Geometry.Root.GetComponentsInChildren<Transform>()){
                string key=part.name=="Lagerkeil 2 / HAVARIERT"?"water-1":part.name=="Lagerkeil 3 / HAVARIERT"?"water-2":null;
                for(int axis=0;axis<4;axis++)if(part.name=="Reaktor-Servicegang "+(axis+1))key="service-"+axis;
                if(key!=null){Selectable(part,key);foreach(var s in part.GetComponentsInChildren<StationSelectable>())s.elementId=key;if(key.StartsWith("water-")){int slot=int.Parse(key.Substring(6));var bounds=Group("Wrack / Auswahl und Sperrbereich",part,RingWalkthrough.At(46,RingWalkthrough.StorageAngle(slot),2.75f),Quaternion.Euler(0,RingWalkthrough.StorageAngle(slot),0));bounds.gameObject.layer=25;bounds.gameObject.AddComponent<BoxCollider>().size=new Vector3(6,5.5f,16);}}
            }
            foreach(var renderer in Layout.RepairDrone.GetComponentsInChildren<Renderer>())renderer.gameObject.layer=25;
            for(int s=0;s<12;s++)if(s!=0&&s!=1&&s!=7&&!RingWalkthrough.DamagedRingSector(s))for(int slot=0;slot<3;slot++)AddElement("expansion-"+s+"-"+slot,"Versorgungsmodul S"+(s+1)+" / "+(slot+1),"Ausbauslot","Zusätzliche Lagerkapazität",s,0);
            foreach(var e in State.elements)if(e.id.StartsWith("expansion-"))e.built=false;
            initialJson=JsonUtility.ToJson(State);RefreshPresentation();
            CombineInfrastructure();
            if(Live)CombineStationHull();
        }
        private void CombineInfrastructure()
        {
            foreach(var material in ownedMaterials){var combine=new List<CombineInstance>();var originals=new List<MeshRenderer>();
                foreach(var filter in presentation.GetComponentsInChildren<MeshFilter>()){
                    var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null||renderer.sharedMaterial!=material||!renderer.enabled||filter.name=="Leuchtfläche"||filter.transform==helperCargo||filter.GetComponentInParent<CommandRoomBlock>()!=null||filter.GetComponentInParent<MaintenanceLaserVisual>()!=null||ExcludeWorkshopDynamic(filter)||sparkRenderers.Contains(renderer))continue;
                    bool moving=false;foreach(var source in debris.Values)if(filter.transform.IsChildOf(source)){moving=true;break;}if(moving)continue;
                    combine.Add(new CombineInstance{mesh=filter.sharedMesh,transform=presentation.worldToLocalMatrix*filter.transform.localToWorldMatrix});originals.Add(renderer);
                }
                if(combine.Count<2)continue;var mesh=new Mesh{name="Gebündelte Infrastruktur / "+material.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(combine.ToArray(),true,true);ownedMeshes.Add(mesh);var root=Group(mesh.name,presentation,Vector3.zero,Quaternion.identity);root.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;root.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;foreach(var renderer in originals)renderer.enabled=false;
            }
        }
        private void BuildMachine(string id,string label,Vector3 position,Vector3 size,bool open)
        {
            var root=Group(label,presentation,position,Quaternion.identity);Selectable(root,id);
            Shape("Anlagenbasis",root,new Vector3(0,.15f,0),new Vector3(size.x,.3f,size.z),graphite,true);
            foreach(float x in new[]{-1f,1f})Shape("Rahmensäule",root,new Vector3(x*size.x*.45f,size.y/2,0),new Vector3(.15f,size.y,size.z),alloy,true);
            Shape("Anlagendach",root,new Vector3(0,size.y,0),new Vector3(size.x,.16f,size.z),alloy,true);
            Shape(open?"Einlegewanne":"Drucker-Arbeitsbett",root,new Vector3(0,size.y*.4f,0),new Vector3(size.x*.75f,.12f,size.z*.8f),blue,true);
            Shape("Arbeitskopf / Baugruppe",root,new Vector3(0,size.y*.75f,0),new Vector3(.4f,.4f,.4f),graphite);
            Text(label,root,new Vector3(0,size.y+.3f,-size.z*.45f),.07f);
        }
        private void BuildToolRack()
        {
            var rack=Group("Werkzeugregal / Greifer Bohrer Laser",presentation,RingWalkthrough.At(46,36),Quaternion.Euler(0,270,0));Selectable(rack,"hall");
            foreach(float x in new[]{-2f,2f})Shape("Regalpfosten",rack,new Vector3(x,1.8f,0),new Vector3(.15f,3.6f,.65f),alloy,true);
            for(int row=0;row<3;row++)Shape("Werkzeugaufnahme",rack,new Vector3(0,.25f+row*1.15f,0),new Vector3(4.2f,.12f,1),alloy,true);
            for(int i=0;i<3;i++)
            {
                var tool=Group(i==0?"BOHRER":i==1?"GREIFER":"LASER",rack,new Vector3(0,.45f+i*1.1f,0),Quaternion.Euler(0,90,0));tool.localScale=Vector3.one*.65f;
                var detailed=tool.gameObject.AddComponent<MaintenanceLaserVisual>();if(i==0)detailed.BuildMiningHead();else if(i==1)detailed.BuildMiningArm();else detailed.Build();foreach(var renderer in tool.GetComponentsInChildren<Renderer>())renderer.gameObject.layer=25;detailed.CombineStaticSurfaces();
                Text(i==0?"BOHRER  /  Bestand":i==1?"GREIFER  /  Bestand":"LASER  /  Bestand",rack,new Vector3(-1.2f,.8f+i*1.1f,-.56f),.045f);
            }
        }
        private void BuildStores()
        {
            if(Live)BuildIcePlantGeometry();
            var root=Group("Getrennte Werkstoffbehälter",presentation,RingWalkthrough.At(23,26),Quaternion.Euler(0,30,0));
            string[] names={"C / KONZENTRAT","Fe / EISEN","? / UNBEKANNT"};
            for(int i=0;i<3;i++){var holder=Group(names[i]+" / einzeln planbar",root,new Vector3((i-1)*1.4f,0,0),Quaternion.identity);materialStores.Add(holder);var box=Shape(names[i],holder,new Vector3(0,.7f,0),new Vector3(1.1f,1.4f,1.1f),i==2?graphite:alloy,true);Selectable(box,"helper");Text(names[i],holder,new Vector3(0,1,-.57f),.04f);}
            helperCargo=Shape("R-01 / interner Transportbehälter",presentation,Layout.RepairDrone.localPosition,new Vector3(.45f,.35f,.45f),blue);helperCargo.gameObject.SetActive(false);
        }
        private void BuildDebris()
        {
            BuildWreckContacts();
            for(int i=0;i<6;i++){var root=Shape("Analysierbare kohlenstoffhaltige Probe",presentation,RingWalkthrough.At(130+i*6,60+i*7,13),new Vector3(1.3f,.8f,1.1f),graphite,true,PrimitiveType.Sphere);var id="carbon-"+i;debris[id]=root;debrisOrigins[id]=root.localPosition;AddElement(id,"Kohlenstofffund "+(i+1),"Rohstoff","Bekannte Prozessprobe / begrenzter Vorrat",2,.9f);Selectable(root,id);}
        }
        private Vector3 WorkTarget(StationWorkOrder order)
        {
            if(order.kind==StationJobKind.Repair&&order.target=="battery")return BatteryServicePoint;
            if(order.kind==StationJobKind.Refit&&WorkshopRefitState.HasData(order.workshop))
                return order.workshop.stage<4
                    ? (primaryRepairCell!=null?primaryRepairCell.position+Layout.transform.up*order.workshop.pivotHeight:workshop.TransformPoint(new Vector3(-4.3f,order.workshop.pivotHeight,8.1f)))
                    : RefitDrone(order).HomePosition;
            if(debrisOrigins.TryGetValue(order.target,out var origin))return presentation.TransformPoint(origin+Vector3.up*5);
            var e=Element(order.target);float radius=56,angle=(e?.sector??0)*30;
            if(e!=null){
                if(e.id.StartsWith("sector-"))radius=33.5f;
                else if(e.id.StartsWith("expansion-")){radius=42;angle=ExpansionAngle(e);}
                else if(e.id.StartsWith("water-")){radius=46;angle=ExpansionAngle(e);}
                else if(e.id=="water"){radius=46;angle=RingWalkthrough.StorageAngle(0);}
                else if(e.id.StartsWith("service-")){int axis=int.Parse(e.id.Substring(8));RingWalkthrough.ServiceBreak(axis,out float from,out float to);radius=(from+to)*.5f;}
                else if(e.id=="reactor")radius=0;
                else if(e.id=="solar-a"||e.id=="solar-b")radius=60;
                else if(e.id=="hall"||e.id=="command")radius=46;
                else if(e.id=="helper")radius=24.5f;
            }
            return Layout.transform.TransformPoint(RingWalkthrough.At(radius,angle,11));
        }
        private void AnimateOrder(StationWorkOrder order,float seconds)
        {
            var r=StationRecipes.Get(order.kind);float p=order.elapsed/r.Seconds;
            if(order.kind==StationJobKind.Expand&&!order.cancelled){BuildExpansion(order.target);RefreshExpansionProgress(order.target);}
            if(Actor(order)==WorkActor.External)
            {
                var drone=Scenario.Drones[int.Parse(order.actorId.Substring(6))];
                if((order.flightPath==null||order.flightPath.Length==0)&&!PlanWorkFlight(order,drone,out _))return;
                float travel=p<.4f?p/.4f:p>.7f?(1-p)/.3f:1;
                drone.SetStationWorkPose(PathPoint(order.flightPath,travel),seconds);
                if(p>=.4f&&p<=.94f&&!order.cancelled&&debris.TryGetValue(order.target,out var cargo))cargo.position=drone.transform.TransformPoint(new Vector3(0,2,0));
            }
            else if(Actor(order)==WorkActor.Internal&&State.helper)
            {
                Layout.RepairDrone.position=HelperPoint(order,p);helperCargo.gameObject.SetActive(p>.1f&&p<.95f);helperCargo.position=Layout.RepairDrone.position+Vector3.up*(IndoorDroneVisual.Height*.5f+.175f+.01f);
            }
        }
        private void ApplyRefit(string target)
        {
            var bits=target.Split(':');int i=int.Parse(bits[0]);string next=bits[1],old=State.tools[i];
            if(next==old)return;
            if(next=="Bohrer")State.spareDrills--;else if(next=="Greifer")State.spareGrippers--;else State.spareLasers--;
            if(old=="Bohrer")State.spareDrills++;else if(old=="Greifer")State.spareGrippers++;else State.spareLasers++;
            State.tools[i]=next;Scenario.Drones[i].GetComponent<TripoMiningDroneVisual>()?.SetTool(next);
        }
        private void UpdateRepairedSector(StationElementState e)
        {
            if(e.id.StartsWith("sector-")&&RingWalkthrough.DamagedRingSector(e.sector))RepairRingShell(e);
            if(e.id.StartsWith("water-")&&e.condition>=.6f)RepairWaterModule(e);
            if(e.id.StartsWith("service-")&&e.condition>=.6f)RepairService(e);
        }
        private void RefreshPresentation()
        {
            if(presentation==null)return;
            RefreshDebrisPresentation();
            foreach(var e in State.elements)if(e.id.StartsWith("expansion-")&&e.built){BuildExpansion(e.id);RefreshExpansionProgress(e.id);}
            foreach(var e in State.elements)if(e.id.StartsWith("sector-")&&RingWalkthrough.DamagedRingSector(e.sector)&&e.condition>=.6f)RepairRingShell(e);
            foreach(var e in State.elements)if(e.condition>=.6f&&(e.id.StartsWith("water-")||e.id.StartsWith("service-")))UpdateRepairedSector(e);
            for(int i=0;i<2;i++)Scenario.Drones[i].GetComponent<TripoMiningDroneVisual>()?.SetTool(State.tools[i]);
            if(!State.orders.Exists(o=>o.started&&!o.complete&&o.actorId=="R-01"))PlaceHelperAtHome();
            if(!State.helper)helperCargo.gameObject.SetActive(false);
            Layout.RepairDrone.GetComponentInChildren<IndoorDroneVisual>()?.SetOperational(State.helper);
            foreach(var renderer in Layout.RepairDrone.GetComponentsInChildren<Renderer>())if(renderer.sharedMaterial!=null&&renderer.sharedMaterial.name=="Drone status"){var block=new MaterialPropertyBlock();block.SetColor("_Color",State.helper?new Color(.1f,.7f,.6f):Color.gray);block.SetColor("_EmissionColor",State.helper?new Color(.05f,.3f,.2f):Color.black);renderer.SetPropertyBlock(block);}
            RefreshRingGates();
            RefreshWorkshopVisuals();
            UpdateIcePlantVisuals(0);
            foreach(var gate in Layout.Geometry.PressureGates)if(gate!=null){for(int slot=1;slot<3;slot++)if(gate.Label=="Wasserlager "+(slot+1).ToString("00")+" / HAVARIE")gate.SetSafetyLock(Element("water-"+slot).condition<.6f);for(int axis=1;axis<4;axis++)if(gate.Label=="Reaktorschott / "+(axis*90))gate.SetSafetyLock(Element("service-"+axis).condition<.6f);}
        }
        private void UpdateLights(Vector3? forcedEye=null)
        {
            if(Camera.main==null)return;UpdateAdaptiveLighting(forcedEye??Camera.main.transform.position);
            for(int i=0;i<sparkRenderers.Count;i++){var e=Element("sector-"+(i+3));sparkRenderers[i].enabled=e!=null&&e.power&&e.condition<.3f&&Mathf.Repeat(Time.unscaledTime+i*.73f,4)>.0f&&Mathf.Repeat(Time.unscaledTime+i*.73f,4)<.08f;}
        }
        internal void RefreshViewLighting(Camera view,bool inside){UpdateInteriorLighting(inside);UpdateLights(view.transform.position);}
        private void UpdateInteriorLighting(bool? forcedInside=null)
        {
            bool inside=forcedInside??StationInteriorMode.IsInside;if(inside==dimmedInterior)return;dimmedInterior=inside;
            RenderSettings.ambientLight=inside?new Color(.035f,.045f,.065f):exteriorAmbient;
            var probe=new SphericalHarmonicsL2();probe.AddAmbientLight(RenderSettings.ambientLight*(inside?1:1.6f));RenderSettings.ambientProbe=probe;
            for(int i=0;i<exteriorSuns.Count;i++)if(exteriorSuns[i]!=null)exteriorSuns[i].cullingMask=inside?exteriorMasks[i]&~(1<<25):exteriorMasks[i];
            if(legacyFill!=null)legacyFill.intensity=inside?.035f:.8f;
        }
        private void DisposePresentation(){Font.textureRebuilt-=FontRebuilt;if(dimmedInterior){RenderSettings.ambientLight=exteriorAmbient;for(int i=0;i<exteriorSuns.Count;i++)if(exteriorSuns[i]!=null)exteriorSuns[i].cullingMask=exteriorMasks[i];}foreach(var m in ownedMaterials)if(m!=null)Destroy(m);foreach(var m in ownedMeshes)if(m!=null)Destroy(m);ui.Dispose();}
    }
}
