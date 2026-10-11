using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    // Imported hull and removable tools follow the actual DroneAgent, never a second resource simulation.
    public sealed class TripoMiningDroneVisual : MonoBehaviour
    {
        private DroneAgent agent;
        private Transform rig, head, cargoRoot, port, armRoot, sample;
        private MaintenanceLaserVisual cassette, drill;
        private MaintenanceLaserVisual collector;
        private MaintenanceLaserVisual cargoVisual;
        private MaintenanceLaserVisual gateVisual;
        private MaintenanceLaserVisual receiverVisual;
        public const int SlotCapacity=12;
        internal void ConfigureReceiver(MaintenanceLaserVisual receiver) { receiverVisual=receiver; }
        public int EmittedSamples { get; private set; }
        public int EmissionsWithoutCutting { get; private set; }
        public float MaximumEmissionTipDistance { get; private set; }
        public int LooseSamples { get { int count=0; for(int i=0;i<cargo.Length;i++) if(!deposited[i] && cargo[i].gameObject.activeSelf) count++; return count; } }
        public bool ContainerFull=>DepositedSamples==SlotCapacity;
        public bool CollarSealed=>receiverVisual!=null && receiverVisual.CollarSealed;
        public float CollarExtension=>receiverVisual!=null?receiverVisual.CollarExtension:0;
        public float CollarContactGap=>receiverVisual==null?float.PositiveInfinity:Vector3.Distance(receiverVisual.CollarContactPoint,GateCentre-transform.forward*.085f);
        public bool PackedCargoInside
        {
            get
            {
                foreach(var item in cargo) if(item.gameObject.activeSelf && item.parent==cargoRoot)
                {
                    var box=RockBounds(item,cargoRoot);
                    if(box.min.x<-.60f || box.max.x>.60f || box.min.y<.065f || box.max.y>.875f || box.min.z<-.75f || box.max.z>.75f) return false;
                }
                return true;
            }
        }
        private readonly List<Mesh> rockMeshes=new List<Mesh>();
        private readonly bool[] deposited=new bool[12];
        private readonly Vector3[] cargoSlots=new Vector3[12];
        private float gateOpening;
        public float GateOpening=>gateOpening;
        public int UnloadedPieces { get; private set; }
        public int HullMountCount { get; private set; }
        public Vector3 GateCentre=>port.position;
        public bool RearWallClosesCargo
        {
            get
            {
                var panel=TripoDockPreview.LocalBounds(gateVisual.CargoGate.Find("Armoured sliding discharge wall"),cargoRoot);
                var seal=TripoDockPreview.LocalBounds(port.Find("Rear wall side compression seal"),cargoRoot);
                return gateOpening<.001f && panel.min.x<=-MaintenanceLaserVisual.CargoWidth*.5f+.05f && panel.max.x>=MaintenanceLaserVisual.CargoWidth*.5f-.05f
                    && panel.min.y<=.045f && panel.max.y>=MaintenanceLaserVisual.CargoHeight-.023f
                    && Mathf.Abs(seal.max.z+MaintenanceLaserVisual.CargoDepth*.5f)<.005f;
            }
        }
        public bool CompactGateGuides
        {
            get
            {
                if(port.Find("Rear gate lifting motor")!=null) return false;
                int guides=0;
                foreach(Transform part in port) if(part.name=="Compact rear sliding gate side guide")
                {
                    guides++; var bounds=TripoDockPreview.LocalBounds(part,cargoRoot);
                    if(bounds.max.y>MaintenanceLaserVisual.CargoHeight+.03f) return false;
                }
                return guides==2;
            }
        }
        public int DischargeClearanceSamples { get; private set; }
        public float MinimumDischargeClearance { get; private set; }=float.PositiveInfinity;
        public string DischargeClearanceDetail { get; private set; }
        public bool GateIntersectsLid
        {
            get
            {
                var gate=TripoDockPreview.LocalBounds(gateVisual.CargoGate,cargoRoot);
                if(gate.Intersects(TripoDockPreview.LocalBounds(cargoVisual.CargoLid,cargoRoot))) return true;
                foreach(Transform part in cargoRoot) if(part.name=="Rear lid latch" && gate.Intersects(TripoDockPreview.LocalBounds(part,cargoRoot))) return true;
                return false;
            }
        }
        internal Bounds[] FlightBoxes { get; private set; }
        internal Vector3[] DockHullPoints { get; private set; }
        private float lidOpening;
        public float LidAngle => Quaternion.Angle(Quaternion.Euler(0,180,0),cargoVisual.CargoLid.localRotation);
        private readonly MaintenanceLaserVisual[] anchorRails=new MaintenanceLaserVisual[2];
        private readonly List<Material> materials=new List<Material>();
        private readonly Transform[] cargo=new Transform[SlotCapacity], plumes=new Transform[4];
        private readonly Vector3[] anchors=new Vector3[2];
        private float rotorAngle, previousCargo, jawWidth;
        private bool hadProductiveStep;
        private Mesh regolithMesh;
        private float collectionClock, sampleSize, retractTime=1;
        private int sizeMask;
        private Vector3 pickup, lastGrip;
        private Quaternion lastOrientation=Quaternion.identity;
        private bool sampleHeld, wasMining, released;
        public int CollectedSizes => ((sizeMask&1)!=0?1:0)+((sizeMask&2)!=0?1:0)+((sizeMask&4)!=0?1:0);
        public int DepositedSamples { get; private set; }
        public bool HoldingSample => sampleHeld && sample.gameObject.activeSelf;
        public Vector3 Grip => collector.GripPoint.position;
        public float HeldSampleError => Vector3.Distance(sample.position,Grip);
        public float SampleSize => sampleSize;
        public bool ArmStowed => !wasMining && retractTime>=1;
        public bool DroppingSample { get; private set; }
        public Vector3 SamplePosition => sample.position;
        public bool SampleInsideBox => new Bounds(new Vector3(0,MaintenanceLaserVisual.CargoHeight*.5f,0),new Vector3(MaintenanceLaserVisual.CargoWidth-.06f,MaintenanceLaserVisual.CargoHeight-.04f,MaintenanceLaserVisual.CargoDepth-.06f)).Contains(cargoRoot.InverseTransformPoint(sample.position));
        public bool TransferAboveRim { get; private set; }
        public float SampleRimClearance
        {
            get
            {
                float lowest=float.PositiveInfinity;
                foreach(var vertex in regolithMesh.vertices) lowest=Mathf.Min(lowest,cargoRoot.InverseTransformPoint(sample.TransformPoint(vertex)).y-MaintenanceLaserVisual.CargoHeight-.025f);
                return lowest;
            }
        }
        public bool CollectorIntersectsCargo
        {
            get
            {
                // Verify actual moving vertices, rather than only joint centres.
                var volume=new Bounds(new Vector3(0,MaintenanceLaserVisual.CargoHeight*.5f+.02f,0),new Vector3(MaintenanceLaserVisual.CargoWidth+.05f,MaintenanceLaserVisual.CargoHeight+.06f,MaintenanceLaserVisual.CargoDepth+.05f));
                foreach(var filter in armRoot.GetComponentsInChildren<MeshFilter>()) foreach(var vertex in filter.sharedMesh.vertices)
                    if(volume.Contains(cargoRoot.InverseTransformPoint(filter.transform.TransformPoint(vertex)))) { CollectorCollisionDetail=filter.name+" at "+cargoRoot.InverseTransformPoint(filter.transform.TransformPoint(vertex))+" size "+sampleSize+" cycle "+collectionClock; return true; }
                return false;
            }
        }
        public string CollectorCollisionDetail { get; private set; }
        public bool CollectorIntersectsLid
        {
            get
            {
                var volume=new Bounds(new Vector3(0,0,MaintenanceLaserVisual.CargoDepth*.5f),new Vector3(MaintenanceLaserVisual.CargoWidth+.07f,.085f,MaintenanceLaserVisual.CargoDepth+.07f));
                foreach(var filter in armRoot.GetComponentsInChildren<MeshFilter>()) foreach(var vertex in filter.sharedMesh.vertices)
                    if(volume.Contains(cargoVisual.CargoLid.InverseTransformPoint(filter.transform.TransformPoint(vertex)))) return true;
                return false;
            }
        }
        public float AxisX { get; private set; }
        public Vector3 Tip => drill.MiningTip.position;
        public Vector3 Intake => cargoRoot.TransformPoint(new Vector3(0,MaintenanceLaserVisual.CargoHeight,0));
        public bool Cutting { get; private set; }
        public int VisibleCargoPieces { get; private set; }
        public bool SupportsAttached { get; private set; }
        public int DrillTriangles => drill.TriangleCount;
        private const float FeetOffset=-.65f, AxisY=1.30f;

        private Material Surface(string name,Color color,float metal=.6f)
        {
            var material=new Material(Shader.Find("Standard")) { name=name,color=color };
            material.SetFloat("_Metallic",metal); material.SetFloat("_Glossiness",.38f); materials.Add(material); return material;
        }
        private Transform Part(string name,PrimitiveType type,Transform parent,Vector3 point,Vector3 size,Material surface)
        {
            var go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(parent,false); go.transform.localPosition=point; go.transform.localScale=size;
            var collision=go.GetComponent<Collider>(); collision.enabled=false; Destroy(collision);
            go.GetComponent<Renderer>().sharedMaterial=surface; return go.transform;
        }
        public void Build(DroneAgent owner)
        {
            agent=owner;
            var old=agent.GetComponent<MiningDroneVisual>(); if(old!=null) old.enabled=false;
            var oldBody=transform.Find("Mining Drone Geometry"); if(oldBody!=null) oldBody.gameObject.SetActive(false);
            rig=new GameObject("Tripo mining hull / bottom-centred metre assembly").transform; rig.SetParent(transform,false); rig.localPosition=Vector3.up*FeetOffset;
            var body=new GameObject("Uniformly scaled original Tripo hull").transform; body.SetParent(rig,false);
            Instantiate(Resources.Load<GameObject>("Tripo/MaintenanceDrone"),body,false);
            body.localScale=Vector3.one*(2/TripoDockPreview.LocalBounds(body,rig).size.x);
            var bounds=TripoDockPreview.LocalBounds(body,rig); body.localPosition=-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            DroneToolMount.FindMountSurface(body,rig,AxisY,out float envelope,out float rear,out float front);
            AxisX=envelope+MaintenanceLaserVisual.CassetteHalfWidth+.07f;
            var socket=new GameObject("Bolted mining tool cassette").transform; socket.SetParent(rig,false); socket.localPosition=new Vector3(AxisX,AxisY,0);
            cassette=socket.gameObject.AddComponent<MaintenanceLaserVisual>(); cassette.BuildCassette(rear-AxisX,front-AxisX);
            head=new GameObject("Interchangeable ice / rock auger").transform; head.SetParent(rig,false); head.localRotation=Quaternion.Euler(0,180,0);
            drill=head.gameObject.AddComponent<MaintenanceLaserVisual>(); drill.BuildMiningHead();
            // Seal lip meets the rear cartridge edge; keep the moving armour outside the closed top lid.
            Vector3 outlet=new Vector3(0,FeetOffset+1.77f+MaintenanceLaserVisual.CargoHeight*.5f,-.05f-MaintenanceLaserVisual.CargoDepth*.5f-.08f);
            agent.ConfigureToolGeometry(new Vector3(AxisX,AxisY+FeetOffset,0),2.4f,outlet);
            var dark=Surface("Mining attachments / graphite",new Color(.07f,.10f,.14f));
            var ice=Surface("Fresh ice-rich regolith",new Color(.55f,.78f,.85f),.08f);
            var glass=new Material(Resources.Load<Shader>("CargoGlass")) { name="Mining cartridge inspection glass",color=new Color(.30f,.72f,.88f,.18f) }; materials.Add(glass);
            cargoRoot=new GameObject("Large transparent roof cargo cartridge").transform; cargoRoot.SetParent(rig,false); cargoRoot.localPosition=new Vector3(0,1.77f,-.05f);
            cargoVisual=cargoRoot.gameObject.AddComponent<MaintenanceLaserVisual>(); cargoVisual.BuildCargoCartridge();
            foreach(float side in new[] { -1f,1f })
            {
                if(side>0) GlassPane("Front inspection glass",cargoRoot,new Vector3(0,MaintenanceLaserVisual.CargoHeight*.5f+.01f,MaintenanceLaserVisual.CargoDepth*.5f),new Vector2(MaintenanceLaserVisual.CargoWidth-.10f,MaintenanceLaserVisual.CargoHeight-.09f),Quaternion.identity,glass);
                GlassPane("Single surface side inspection glass",cargoRoot,new Vector3(side*MaintenanceLaserVisual.CargoWidth*.5f,MaintenanceLaserVisual.CargoHeight*.5f+.01f,0),new Vector2(MaintenanceLaserVisual.CargoDepth-.10f,MaintenanceLaserVisual.CargoHeight-.09f),Quaternion.Euler(0,90,0),glass);
            }
            for(int i=0;i<cargo.Length;i++)
            {
                cargoSlots[i]=new Vector3(i%2==0?-.29f:.29f,.25f+(i/6)*.41f,((i/2)%3-1)*.46f);
                cargo[i]=Chip("Rock carried into cargo by gripper "+i,cargoRoot,cargoSlots[i],Vector3.one*.3f,ice,i+7); cargo[i].gameObject.SetActive(false);
            }
            armRoot=new GameObject("Opposite-side articulated collecting arm").transform; armRoot.SetParent(rig,false); armRoot.localPosition=new Vector3(-.94f,1.38f,.20f);
            collector=armRoot.gameObject.AddComponent<MaintenanceLaserVisual>(); collector.BuildMiningArm();
            sample=cargo[0]; regolithMesh=sample.GetComponent<MeshFilter>().sharedMesh;
            port=new GameObject("Wide rear unloading gate docking plane").transform; port.SetParent(transform,false); port.localPosition=outlet;
            gateVisual=port.gameObject.AddComponent<MaintenanceLaserVisual>(); gateVisual.BuildCargoGate();
            var roofPoints=new Vector3[4]; int roofIndex=0;
            foreach(float x in new[]{-.38f,.38f}) foreach(float z in new[]{-.53f,.43f}) roofPoints[roofIndex++]=new Vector3(x,DroneToolMount.RoofSurface(body,rig,x,z),z);
            rig.gameObject.AddComponent<MaintenanceLaserVisual>().BuildCargoMounts(roofPoints); HullMountCount=roofPoints.Length;
            for(int i=0;i<2;i++)
            {
                anchors[i]=new Vector3((i==0?-1:1)*(MaintenanceLaserVisual.CargoWidth*.5f+.19f),2.12f,-.05f);
                var rail=new GameObject("Cargo-mounted telescopic anchoring rail "+i).transform; rail.SetParent(rig,false); rail.localPosition=anchors[i];
                anchorRails[i]=rail.gameObject.AddComponent<MaintenanceLaserVisual>(); anchorRails[i].BuildAnchorRail();
            }
            var glow=Surface("Mining thruster cyan",new Color(.08f,.5f,.8f),.1f); glow.EnableKeyword("_EMISSION"); glow.SetColor("_EmissionColor",new Color(.1f,.7f,1));
            for(int i=0;i<plumes.Length;i++) plumes[i]=Part("Mining manoeuvre jet "+i,PrimitiveType.Sphere,rig,new Vector3(i%2==0?-.68f:.68f,.1f,i<2?-.65f:.65f),new Vector3(.06f,.18f,.06f),glow);
            foreach(var attachment in GetComponentsInChildren<MaintenanceLaserVisual>()) attachment.UseHullBlue(new Color(.23f,.31f,.47f));
            ResetCollection(); Refresh(0);
            var groups=new[]{body,cargoRoot,port,armRoot,cassette.transform,drill.transform,anchorRails[0].transform,anchorRails[1].transform};
            FlightBoxes=new Bounds[groups.Length];
            for(int i=0;i<groups.Length;i++) FlightBoxes[i]=TripoDockPreview.LocalBounds(groups[i],transform);
            var dockPoints=new List<Vector3>();
            foreach(var filter in GetComponentsInChildren<MeshFilter>())
            {
                var vertices=filter.sharedMesh.vertices; int stride=Mathf.Max(1,vertices.Length/3000);
                var matrix=transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                for(int i=0;i<vertices.Length;i+=stride) dockPoints.Add(matrix.MultiplyPoint3x4(vertices[i]));
            }
            DockHullPoints=dockPoints.ToArray();
        }
        public void ResetCollection()
        {
            collectionClock=0; sizeMask=0; DepositedSamples=UnloadedPieces=0; wasMining=sampleHeld=released=false; retractTime=1; previousCargo=0;
            EmittedSamples=EmissionsWithoutCutting=0; MaximumEmissionTipDistance=0;
            receiverVisual?.PoseCargoCollar(0,false);
            lastGrip=new Vector3(-1.30f,1.87f,-.50f); lastOrientation=Quaternion.identity;
            for(int i=0;i<cargo.Length;i++) { deposited[i]=false; cargo[i].SetParent(cargoRoot,false); cargo[i].localPosition=cargoSlots[i]; cargo[i].gameObject.SetActive(false); }
            sample=cargo[0]; regolithMesh=sample.GetComponent<MeshFilter>().sharedMesh;
            gateOpening=0; gateVisual.PoseCargoGate(0);
            DischargeClearanceSamples=0; MinimumDischargeClearance=float.PositiveInfinity;
            lidOpening=0; cargoVisual.PoseCargoLid(0);
        }
        private static void Rod(Transform item,Vector3 start,Vector3 end,float radius)
        { item.position=(start+end)*.5f; item.rotation=Quaternion.FromToRotation(Vector3.up,end-start); item.localScale=new Vector3(radius,(end-start).magnitude*.5f,radius); }
        public void Refresh(float realSeconds)
        {
            float extension=agent.Phase==DronePhase.Mining?1:agent.Phase==DronePhase.Docking?Mathf.SmoothStep(0,1,(agent.PhaseElapsed-60)/60)
                :agent.Phase==DronePhase.Undocking?1-Mathf.SmoothStep(0,1,agent.PhaseElapsed/60):0;
            cassette.PoseCassette(extension); head.localPosition=new Vector3(AxisX,AxisY,Mathf.Lerp(MaintenanceLaserVisual.StowedHead,MaintenanceLaserVisual.ExtendedHead,extension));
            hadProductiveStep=realSeconds>0 && agent.CargoKg>previousCargo+.000001f;
            Cutting=agent.Phase==DronePhase.Mining && hadProductiveStep;
            if(Cutting) rotorAngle=Mathf.Repeat(rotorAngle+realSeconds*720,360);
            drill.Rotor.localRotation=Quaternion.Euler(0,0,rotorAngle);
            previousCargo=agent.CargoKg;
            RefreshCollector(realSeconds);
            bool openLid=agent.Phase==DronePhase.Mining || (agent.Phase==DronePhase.Docking && agent.PhaseElapsed>=75) || !ArmStowed;
            lidOpening=Mathf.MoveTowards(lidOpening,openLid?1:0,realSeconds*2.5f);
            cargoVisual.PoseCargoLid(lidOpening);
            RefreshUnloading();
            SupportsAttached=true;
            for(int i=0;i<2;i++)
            {
                bool active=extension>.01f && agent.Target!=null;
                if(!active) { SupportsAttached=false; anchorRails[i].PoseAnchorRail(.82f,Quaternion.identity); continue; }
                Vector3 start=rig.TransformPoint(anchors[i]);
                var ray=new Ray(start,transform.forward);
                if(!agent.Target.GetComponent<Collider>().Raycast(ray,out RaycastHit hit,5))
                { SupportsAttached=false; anchorRails[i].PoseAnchorRail(.82f,Quaternion.identity); continue; }
                float reach=Vector3.Dot(hit.point-start,transform.forward)-.13f;
                if(reach>2.75f || reach<.82f) SupportsAttached=false;
                anchorRails[i].PoseAnchorRail(Mathf.Lerp(.82f,reach,extension),Quaternion.FromToRotation(Vector3.forward,rig.InverseTransformDirection(-hit.normal)));
            }
            bool flying=agent.IsFlying || agent.Phase==DronePhase.Launching || agent.Phase==DronePhase.Berthing;
            foreach(var plume in plumes) plume.gameObject.SetActive(flying && realSeconds>0);
        }
        private void RefreshCollector(float dt)
        {
            Vector3 rest=new Vector3(-1.30f,1.87f,-.50f), outside=new Vector3(-1.58f,3.17f,.30f), above=new Vector3(-.20f,3.17f,-.10f);
            bool mining=agent.Phase==DronePhase.Mining;
            DroppingSample=false; TransferAboveRim=false;
            if(!mining)
            {
                if(wasMining) retractTime=0;
                wasMining=false; sampleHeld=false; if(!released) sample.gameObject.SetActive(false);
                retractTime=Mathf.Min(1,retractTime+dt*2);
                // Finish outside the open box before folding alongside the hull.
                Vector3 from=lastGrip;
                if(from.x>-.84f && from.y>2.6f) from=Vector3.Lerp(from,outside,Mathf.Clamp01(retractTime*2));
                Vector3 grip=Vector3.Lerp(from,rest,Mathf.SmoothStep(0,1,Mathf.Clamp01((retractTime-.5f)*2)));
                collector.PoseMiningArm(grip-armRoot.localPosition,Quaternion.Slerp(lastOrientation,Quaternion.identity,retractTime),.12f,retractTime>=.5f);
                return;
            }
            float before=collectionClock;
            // One cycle per twelfth of real mined cargo. Pausing extraction cannot emit another rock.
            if(Cutting) collectionClock=Mathf.Min(SlotCapacity*5-.0001f,agent.CargoKg/agent.CargoCapacityKg*SlotCapacity*5);
            int cycle=Mathf.FloorToInt(collectionClock/5);
            float t=collectionClock-cycle*5;
            if(!wasMining || Mathf.FloorToInt(before/5)!=cycle)
            {
                if(!Cutting || DepositedSamples>=SlotCapacity) return;
                sampleSize=new[] { .30f,.42f,.56f }[cycle%3]; released=false; sampleHeld=false;
                sample=cargo[Mathf.Min(DepositedSamples,cargo.Length-1)]; sample.SetParent(rig,true); regolithMesh=sample.GetComponent<MeshFilter>().sharedMesh;
                sample.localRotation=Quaternion.Euler(0,cycle*53,0);
                sample.localScale=Vector3.one*sampleSize;
                var box=RockBounds(sample,rig);
                sample.localScale*=Mathf.Min(1,.54f/box.size.x,.34f/box.size.y,.43f/box.size.z);
                jawWidth=RockBounds(sample,rig).size.x;
                EmittedSamples++; if(!Cutting) EmissionsWithoutCutting++;
                sample.position=Tip-transform.forward*(sampleSize*.7f+.04f);
                MaximumEmissionTipDistance=Mathf.Max(MaximumEmissionTipDistance,Vector3.Distance(sample.position,Tip));
                Vector3 origin=rig.TransformPoint(new Vector3(-1.30f,.90f,.90f));
                pickup=new Vector3(-1.30f,.90f,2.1f);
                if(agent.Target.GetComponent<Collider>().Raycast(new Ray(origin,transform.forward),out var hit,5))
                    pickup=rig.InverseTransformPoint(hit.point-transform.forward*(sampleSize*.68f+.03f));
            }
            wasMining=true; retractTime=0;
            Vector3 gripPosition;
            Quaternion orientation;
            float closed=jawWidth+.035f, opening=closed+.18f;
            if(t<1) { gripPosition=Vector3.Lerp(cycle==0?rest:outside,pickup,Mathf.SmoothStep(0,1,t)); orientation=Quaternion.identity; }
            else if(t<1.35f) { gripPosition=pickup; orientation=Quaternion.identity; opening=Mathf.Lerp(opening,closed,(t-1)/.35f); }
            else if(t<2.45f)
            {
                float p=Mathf.SmoothStep(0,1,(t-1.35f)/1.10f);
                // Lift ahead of the front wall, then move back while outside the side glass.
                gripPosition=Vector3.Lerp(pickup,outside,p)+Vector3.up*(Mathf.Sin(p*Mathf.PI)*.36f);
                orientation=Quaternion.Slerp(Quaternion.identity,Quaternion.Euler(90,0,0),p); opening=closed;
            }
            else if(t<3.25f) { gripPosition=Vector3.Lerp(outside,above,Mathf.SmoothStep(0,1,(t-2.45f)/.8f)); orientation=Quaternion.Euler(90,0,0); opening=closed; TransferAboveRim=true; }
            else if(t<3.65f) { gripPosition=above; orientation=Quaternion.Euler(90,0,0); opening=Mathf.Lerp(closed,closed+.18f,(t-3.25f)/.4f); DroppingSample=true; }
            else { gripPosition=Vector3.Lerp(above,outside,Mathf.SmoothStep(0,1,(t-3.65f)/1.35f)); orientation=Quaternion.Euler(90,0,0); }
            collector.PoseMiningArm(gripPosition-armRoot.localPosition,orientation,opening,false);
            lastGrip=gripPosition; lastOrientation=orientation;
            sampleHeld=t>=1.35f && t<3.25f;
            sample.gameObject.SetActive(agent.CargoKg>0);
            if(sampleHeld) { sample.position=Grip; sizeMask|=1<<(cycle%3); }
            else if(t<1.35f) sample.position=Vector3.Lerp(Tip-transform.forward*(sampleSize*.7f+.04f),rig.TransformPoint(pickup),Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.85f)));
            else if(DroppingSample)
            {
                int index=Mathf.Min(DepositedSamples,cargo.Length-1);
                if(!released) sample.position=Vector3.Lerp(rig.TransformPoint(above),cargoRoot.TransformPoint(cargoSlots[index]),Mathf.Clamp01((t-3.25f)/.35f));
                if(!released && t>=3.60f)
                { released=true; deposited[index]=true; DepositedSamples++; sample.SetParent(cargoRoot,true); sample.localPosition=cargoSlots[index]; }
            }
        }
        private void RefreshUnloading()
        {
            bool unloading=agent.Phase==DronePhase.Unloading;
            bool docked=Vector3.Distance(GateCentre,agent.TankInlet)<.025f && Quaternion.Angle(agent.transform.rotation,agent.TankDockRotation)<.1f;
            float time=agent.PhaseElapsed;
            float collar=unloading && docked?Mathf.Min(Mathf.Clamp01(time/12f),Mathf.Clamp01((240f-time)/12f)):0;
            receiverVisual?.PoseCargoCollar(collar,unloading && docked && time>=12 && time<=228);
            gateOpening=unloading && docked && CollarSealed?MaintenanceLaserVisual.MaxCargoGateOpening*Mathf.Min(Mathf.Clamp01((time-20)/20f),Mathf.Clamp01((220f-time)/20f)):0;
            gateVisual.PoseCargoGate(gateOpening);
            VisibleCargoPieces=0; UnloadedPieces=0;
            for(int i=0;i<cargo.Length;i++) if(deposited[i])
            {
                float p=unloading && docked?Mathf.Clamp01(agent.UnloadProgress*DepositedSamples-i):0;
                if(agent.Phase==DronePhase.TankUndocking || agent.Phase==DronePhase.Parking || agent.Phase==DronePhase.Berthing || agent.Phase==DronePhase.Servicing || (agent.Phase==DronePhase.Ready && agent.CargoKg<.01f)) p=1;
                cargo[i].gameObject.SetActive(p<1);
                if(p>=1) { UnloadedPieces++; continue; }
                VisibleCargoPieces++;
                Vector3 discharge=Vector3.Lerp(cargoSlots[i],new Vector3(0,.25f,-MaintenanceLaserVisual.CargoDepth*.5f-2.5f),p);
                discharge.x=Mathf.Lerp(cargoSlots[i].x,0,Mathf.SmoothStep(0,1,Mathf.Clamp01(p/.18f)));
                discharge.y=Mathf.Lerp(cargoSlots[i].y,.25f,Mathf.SmoothStep(0,1,Mathf.Clamp01(p/.18f)));
                cargo[i].localPosition=discharge;
                if(unloading && p>0)
                {
                    var rock=RockBounds(cargo[i],port);
                    // Only measure pieces while they actually cross the gate and its seals.
                    if(rock.min.z<.078f && rock.max.z>-.08f)
                    {
                        var panel=TripoDockPreview.LocalBounds(gateVisual.CargoGate.Find("Armoured sliding discharge wall"),port);
                        float clearance=Mathf.Min(panel.min.y-rock.max.y,.40f-rock.max.y,rock.min.y+.40f,(MaintenanceLaserVisual.CargoWidth*.5f-.06f)-Mathf.Max(Mathf.Abs(rock.min.x),Mathf.Abs(rock.max.x)));
                        if(clearance<MinimumDischargeClearance) { MinimumDischargeClearance=clearance; DischargeClearanceDetail="piece="+i+" progress="+p+" opening="+gateOpening+" rock="+rock+" panel="+panel; }
                        DischargeClearanceSamples++;
                    }
                }
            }
        }
        private static Bounds RockBounds(Transform item,Transform relative)
        {
            var vertices=item.GetComponent<MeshFilter>().sharedMesh.vertices;
            var bounds=new Bounds(relative.InverseTransformPoint(item.TransformPoint(vertices[0])),Vector3.zero);
            for(int i=1;i<vertices.Length;i++) bounds.Encapsulate(relative.InverseTransformPoint(item.TransformPoint(vertices[i])));
            return bounds;
        }
        private void GlassPane(string name,Transform parent,Vector3 point,Vector2 size,Quaternion turn,Material material)
        {
            var mesh=new Mesh { name=name,vertices=new[]{new Vector3(-size.x/2,-size.y/2,0),new Vector3(size.x/2,-size.y/2,0),new Vector3(size.x/2,size.y/2,0),new Vector3(-size.x/2,size.y/2,0)},triangles=new[]{0,1,2,0,2,3} }; mesh.RecalculateNormals(); rockMeshes.Add(mesh);
            var pane=new GameObject(name); pane.transform.SetParent(parent,false); pane.transform.localPosition=point; pane.transform.localRotation=turn;
            pane.AddComponent<MeshFilter>().sharedMesh=mesh; var renderer=pane.AddComponent<MeshRenderer>(); renderer.sharedMaterial=material;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows=false;
        }
        private Transform Chip(string name,Transform parent,Vector3 position,Vector3 size,Material surface,int seed)
        {
            var go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localScale=size;
            var mesh=CreateRegolithMesh(seed); rockMeshes.Add(mesh);
            go.AddComponent<MeshFilter>().sharedMesh=mesh; go.AddComponent<MeshRenderer>().sharedMaterial=surface; return go.transform;
        }
        private static Mesh CreateRegolithMesh(int seed)
        {
            float t=(1+Mathf.Sqrt(5))*.5f;
            var points=new[]{new Vector3(-1,t,0),new Vector3(1,t,0),new Vector3(-1,-t,0),new Vector3(1,-t,0),new Vector3(0,-1,t),new Vector3(0,1,t),new Vector3(0,-1,-t),new Vector3(0,1,-t),new Vector3(t,0,-1),new Vector3(t,0,1),new Vector3(-t,0,-1),new Vector3(-t,0,1)};
            var random=new System.Random(seed);
            for(int i=0;i<points.Length;i++) points[i]=points[i].normalized*(.40f+(float)random.NextDouble()*.20f)+new Vector3((float)random.NextDouble()-.5f,(float)random.NextDouble()-.5f,(float)random.NextDouble()-.5f)*.10f;
            int[] faces={0,11,5,0,5,1,0,1,7,0,7,10,0,10,11,1,5,9,5,11,4,11,10,2,10,7,6,7,1,8,3,9,4,3,4,2,3,2,6,3,6,8,3,8,9,4,9,5,2,4,11,6,2,10,8,6,7,9,8,1};
            var vertices=new Vector3[faces.Length]; var indices=new int[faces.Length];
            for(int i=0;i<faces.Length;i++) { vertices[i]=points[faces[i]]; indices[i]=i; }
            var mesh=new Mesh { name="Angular ice / rock fragment",vertices=vertices,triangles=indices }; mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        private void OnDestroy() { foreach(var material in materials) if(material!=null) Destroy(material); foreach(var mesh in rockMeshes) if(mesh!=null) Destroy(mesh); }
    }
}
