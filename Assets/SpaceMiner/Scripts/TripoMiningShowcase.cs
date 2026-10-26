using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    public sealed class TripoMiningShowcase : MonoBehaviour
    {
        private const string Evidence="Logs/TripoMining";
        private readonly SpaceMinerUi ui=new SpaceMinerUi();
        private WaterScenario scenario;
        private DroneAgent agent;
        private AsteroidResource source;
        private TripoMiningDroneVisual visual;
        private Camera view;
        private Transform cradle;
        private DroneFlightPlanner navigator;
        private float berthFitDrop;
        private Vector2 scroll;
        private float yaw=-55,pitch=20,distance=7.3f,clock,startRaw,startWater,startBattery,mined,simulatedSeconds;
        private bool ready,paused,checking,complete,detail,collectorDetail,unloadDetail;
        private int checks;
        private DronePhase previousPhase;
        private string safetyMessage;
        private Mesh receiverPortMesh;
        private bool receiverBulkheadPresent;
        private bool ModalOpen => SettingsMenu.BlocksInput;
        private bool Blocked => paused || ModalOpen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"-tripoMiningShowcase")>=0 || Array.IndexOf(args,"-tripoMiningCheck")>=0)
                new GameObject("First Tripo mining drone / actual resource loop").AddComponent<TripoMiningShowcase>();
        }
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1);
            FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride=Path.GetFullPath(Evidence+"/Saves");
            CinematicLibrary.PreferencePrefix="SpaceMiner.TripoMining.";
            FindFirstObjectByType<StartMenu>().StartDemo(); FindFirstObjectByType<IntroSequence>().Skip();
            yield return null; yield return null;
            if(!Guard(() => {
                scenario=FindFirstObjectByType<WaterScenario>(); scenario.SetSimulationRate(0); scenario.enabled=false;
                scenario.Scanner.DismissDialogue(); scenario.Scanner.enabled=false;
                var interior=StationInteriorMode.Current; interior.Exit(true); interior.enabled=false;
                var orbit=FindFirstObjectByType<OrbitCamera>(); orbit.ShowHud=false; orbit.enabled=false;
                view=Camera.main; Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
                agent=scenario.Worker;
                source=Array.Find(scenario.Asteroids,a=>a.IsStarterWaterSource && a.WaterFraction>0);
                if(source==null) throw new Exception("No starter ice asteroid available");
                source.IsScanned=source.WaterIdentified=true;
                var fleet=scenario.GetComponent<CampaignDroneFleet>();if(fleet!=null)fleet.enabled=false;
                visual=agent.GetComponent<TripoMiningDroneVisual>();if(visual==null){visual=agent.gameObject.AddComponent<TripoMiningDroneVisual>();visual.Build(agent);}
                ReplaceOriginalBerth(); BuildCradle(); BuildReceiver();
                foreach(var parked in scenario.Drones) if(parked!=agent)
                {
                    var bounds=TripoDockPreview.LocalBounds(parked.transform,parked.transform);
                    var collider=parked.gameObject.AddComponent<BoxCollider>(); collider.center=bounds.center; collider.size=bounds.size;
                }
                Physics.SyncTransforms();
                navigator=new DroneFlightPlanner(agent.transform) { HullBoxes=visual.FlightBoxes,DockHullPoints=visual.DockHullPoints,CradleCollider=cradle.GetComponentInChildren<MeshCollider>() }; agent.ConfigureNavigation(navigator);
                // The original cradle has raised ribs. Fit them against actual landing geometry, not only its centre floor.
                for(int fit=0;!navigator.PoseClear(agent.HomePosition,agent.HomeRotation,out string blocked);fit++)
                {
                    if(fit>=30 || !blocked.StartsWith("ChargingCradle")) throw new Exception("Berth fit blocked: "+blocked);
                    cradle.position-=Vector3.up*.02f; berthFitDrop+=.02f; Physics.SyncTransforms();
                }
                Debug.Log("MINING BERTH FIT: lower cradle by "+berthFitDrop+" m against actual landing geometry");
                source.GetComponent<LODGroup>()?.ForceLOD(0);
                QualitySettings.antiAliasing=8; view.allowMSAA=true;
                QualitySettings.shadowProjection=ShadowProjection.StableFit; QualitySettings.shadowDistance=60;
                Restart(); ready=true;
                checking=Array.IndexOf(Environment.GetCommandLineArgs(),"-tripoMiningCheck")>=0;
                CameraPose();
            })) yield break;
            if(checking) yield return Validate();
        }
        private void BuildCradle()
        {
            cradle=new GameObject("Tripo mining charging cradle").transform; cradle.SetPositionAndRotation(agent.HomePosition,agent.HomeRotation);
            var model=new GameObject("Normalised original charging cradle").transform; model.SetParent(cradle,false);
            Instantiate(Resources.Load<GameObject>("Tripo/ChargingCradle"),model,false);
            model.localScale=Vector3.one*(3.05f/TripoDockPreview.LocalBounds(model,cradle).size.x);
            Bounds b=TripoDockPreview.LocalBounds(model,cradle);
            model.localPosition=-new Vector3(b.center.x,b.min.y,b.center.z);
            foreach(var filter in cradle.GetComponentsInChildren<MeshFilter>())
            { var collider=filter.gameObject.AddComponent<MeshCollider>(); collider.sharedMesh=filter.sharedMesh; collider.gameObject.layer=30; }
            Physics.SyncTransforms();
            Vector3 origin=cradle.position+cradle.up*10;
            if(!Physics.Raycast(origin,-cradle.up,out var hit,20,1<<30)) throw new Exception("Mining cradle floor missing");
            float floor=Vector3.Dot(hit.point-cradle.position,cradle.up);
            cradle.position+=cradle.up*(-.65f-floor+.015f);
            // Retain the real concave cradle collider for navigation and berth-clearance verification.
        }
        private void ReplaceOriginalBerth()
        {
            var station=scenario.GetComponentInChildren<StationInteriorLayout>();
            foreach(var item in station.GetComponentsInChildren<Transform>())
            {
                if(item.name=="Aussen-Ladebucht 01" || item.name=="Dockaufnahme" && Vector3.Distance(item.position,agent.HomePosition-Vector3.up*.85f)<.1f) item.gameObject.SetActive(false);
            }
        }
        private void BuildReceiver()
        {
            var socket=scenario.GetComponent<StationVisual>().TankSocket;
            var walls=scenario.GetComponentsInChildren<MeshFilter>();
            var receiverWall=Array.Find(walls,f=>f.name=="WASSERLAGER Anschlusswand") ?? Array.Find(walls,f=>f.name=="WASSERLAGER Abschlusswand");
            if(receiverWall!=null) CutReceiverPort(receiverWall,socket);
            // Current wedge geometry already builds the aperture from four separate wall sections.
            receiverBulkheadPresent=receiverWall!=null || Array.Exists(walls,f=>f.name=="Wasserlager Abschlusswand");
            foreach(Transform item in socket) item.gameObject.SetActive(false);
            var oldPipe=socket.parent.Find("Transfer pipe"); if(oldPipe!=null) oldPipe.gameObject.SetActive(false);
            var receiver=new GameObject("Wide sealed cargo receiver / sliding-gate docking").transform; receiver.SetParent(socket,false);
            var surface=receiver.gameObject.AddComponent<MaintenanceLaserVisual>(); surface.BuildCargoReceiver(); surface.UseHullBlue(new Color(.23f,.31f,.47f));
            foreach(var filter in receiver.GetComponentsInChildren<MeshFilter>())
                if(filter.name=="Wide unloading receiver side" || filter.name=="Enclosed unloading receiver floor" || filter.name=="Enclosed unloading receiver roof") { var collider=filter.gameObject.AddComponent<BoxCollider>(); collider.center=filter.sharedMesh.bounds.center; collider.size=filter.sharedMesh.bounds.size; }
            visual.ConfigureReceiver(surface); agent.ConfigureUnloading(40,200);
            Physics.SyncTransforms();
        }
        private void CutReceiverPort(MeshFilter wall,Transform socket)
        {
            var sourceMesh=wall.sharedMesh; var sourceVertices=sourceMesh.vertices; var triangles=sourceMesh.triangles;
            var vertices=new List<Vector3>(); var indices=new List<int>();
            List<Vector3> Clip(List<Vector3> polygon,int axis,float edge,bool inside)
            {
                var result=new List<Vector3>();
                for(int i=0;i<polygon.Count;i++)
                {
                    Vector3 a=polygon[i],b=polygon[(i+1)%polygon.Count];
                    bool aIn=inside?a[axis]>=edge:a[axis]<=edge,bIn=inside?b[axis]>=edge:b[axis]<=edge;
                    if(aIn) result.Add(a);
                    if(aIn!=bIn) result.Add(Vector3.Lerp(a,b,(edge-a[axis])/(b[axis]-a[axis])));
                }
                return result;
            }
            void Emit(List<Vector3> polygon)
            {
                for(int i=1;i+1<polygon.Count;i++) foreach(var p in new[]{polygon[0],polygon[i],polygon[i+1]})
                { indices.Add(vertices.Count); vertices.Add(wall.transform.InverseTransformPoint(socket.TransformPoint(p))); }
            }
            for(int i=0;i<triangles.Length;i+=3)
            {
                var polygon=new List<Vector3>(); for(int j=0;j<3;j++) polygon.Add(socket.InverseTransformPoint(wall.transform.TransformPoint(sourceVertices[triangles[i+j]])));
                Emit(Clip(polygon,0,-.96f,false)); polygon=Clip(polygon,0,-.96f,true);
                Emit(Clip(polygon,0,.96f,true)); polygon=Clip(polygon,0,.96f,false);
                Emit(Clip(polygon,1,-.65f,false)); polygon=Clip(polygon,1,-.65f,true);
                Emit(Clip(polygon,1,.65f,true));
            }
            receiverPortMesh=new Mesh {name="Water storage bulkhead with actual cargo aperture"}; receiverPortMesh.SetVertices(vertices); receiverPortMesh.SetTriangles(indices,0); receiverPortMesh.RecalculateNormals(); receiverPortMesh.RecalculateBounds();
            wall.sharedMesh=receiverPortMesh; wall.GetComponent<MeshCollider>().sharedMesh=receiverPortMesh;
        }
        private void Restart()
        {
            scenario.ResetScenario(); scenario.SetSimulationRate(0);
            agent.UseLegacyCargoProfile(); agent.CargoCapacityKg=12; agent.ResetDrone();
            source.IsScanned=source.WaterIdentified=true;
            startRaw=source.RemainingRawKg; startWater=scenario.WaterLiters; startBattery=agent.BatteryKwh;
            paused=complete=false; mined=clock=simulatedSeconds=0; previousPhase=agent.Phase;
            safetyMessage=null;
            visual.ResetCollection(); visual.Refresh(0);
            yaw=collectorDetail?125:-125;
        }
        private float Rate => agent.Phase==DronePhase.Mining?2:agent.Phase==DronePhase.Unloading?20:80;
        private void Step(float dt)
        {
            if(Blocked) { visual.Refresh(0); return; }
            clock+=dt;
            if(agent.Phase==DronePhase.Ready && clock>3 && scenario.Deliveries==0)
            {
                if(!agent.CanCompleteTrip(source,out string reason)) throw new Exception(reason);
                agent.Assign(source);
            }
            float remaining=dt;
            while(remaining>.0001f && !complete)
            {
                Vector3 before=agent.transform.position; Quaternion turn=agent.transform.rotation;
                float rate=Rate, slice=Mathf.Min(.25f,remaining*rate); scenario.Advance(slice); remaining-=slice/rate; simulatedSeconds+=slice;
                bool moving=agent.IsFlying || agent.Phase==DronePhase.Launching || agent.Phase==DronePhase.Berthing || agent.Phase==DronePhase.TankDocking || agent.Phase==DronePhase.TankUndocking;
                if(moving && !navigator.PoseClear(agent.transform.position,agent.transform.rotation,out string obstruction))
                { agent.transform.SetPositionAndRotation(before,turn); throw new Exception("Mining drone clearance blocked in "+agent.Phase+" by "+obstruction+" at "+agent.transform.position); }
                if(agent.Phase==DronePhase.Stranded) throw new Exception("Mining drone exhausted its return supply");
                if(agent.Phase==DronePhase.Servicing && scenario.Deliveries>0) { complete=true; paused=true; yaw=collectorDetail?125:-125; }
            }
            mined=startRaw-source.RemainingRawKg;
            visual.Refresh(complete?0:dt);
            if(visual.GateOpening>0 && (!visual.CollarSealed || visual.CollarContactGap>.005f)) throw new Exception("Rear gate cannot open before collar seals against cartridge");
            if(agent.Phase!=previousPhase)
            {
                if(agent.Phase==DronePhase.Outbound) yaw=collectorDetail?55:-55;
                Debug.Log("TRIPO MINING PHASE: "+agent.Phase+" cargo="+agent.CargoKg+" raw="+source.RemainingRawKg+" tank="+scenario.WaterLiters); previousPhase=agent.Phase;
            }
        }
        private void Update()
        {
            if(!ready || checking) return;
            Guard(() => Step(Mathf.Min(Time.unscaledDeltaTime,.1f)));
            if(ModalOpen) return;
            if(Input.mousePosition.x<Screen.width-365)
            {
                if(Input.GetMouseButton(1)) { yaw+=Input.GetAxis("Mouse X")*4; pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*3,-65,75); }
                distance=Mathf.Clamp(distance-Input.mouseScrollDelta.y*.4f,detail || collectorDetail?1.1f:3,25);
            }
            CameraPose();
        }
        private void CameraPose()
        {
            view.rect=new Rect(0,0,Mathf.Clamp01((Screen.width-365f)/Screen.width),1);
            Vector3 focus=unloadDetail?visual.GateCentre:collectorDetail?agent.transform.TransformPoint(new Vector3(-.45f,1.70f,.35f)):detail?visual.Tip-agent.transform.forward*.45f:agent.transform.TransformPoint(new Vector3(0,.90f,.5f));
            view.transform.position=focus+agent.transform.rotation*Quaternion.Euler(pitch,yaw,0)*Vector3.back*distance;
            view.transform.LookAt(focus); view.fieldOfView=50; view.nearClipPlane=.12f; view.farClipPlane=20000;
        }
        private bool AtHome => Vector3.Distance(agent.transform.position,agent.HomePosition)<3;
        private void SetDetail(bool value) { detail=value; collectorDetail=unloadDetail=false; distance=value?2.4f:7.3f; yaw=AtHome?-125:-55; pitch=20; CameraPose(); }
        private void SetCollectorDetail(bool value) { collectorDetail=value; detail=unloadDetail=false; distance=value?5.8f:7.3f; yaw=(value?1:-1)*(AtHome?125:55); pitch=value?25:20; CameraPose(); }
        private void SetUnloadDetail(bool value) { unloadDetail=value; detail=collectorDetail=false; distance=value?5:7.3f; yaw=value?-85:-55; pitch=value?15:20; CameraPose(); }
        private void OnGUI()
        {
            if(!ready || ModalOpen) return;
            ui.Configure(SettingsStore.Current.Accessibility); GUI.depth=-100;
            ui.Panel(new Rect(Screen.width-355,65,340,Screen.height-85));
            GUILayout.BeginArea(new Rect(Screen.width-343,77,316,Screen.height-109)); scroll=GUILayout.BeginScrollView(scroll);
            GUILayout.Label("MINING-DROHNE 01",ui.Heading);
            GUILayout.Label(complete?"Erster Abbau abgeschlossen · im Dock":agent.Phase==DronePhase.Ready?"Start in Kürze":agent.Status,ui.Text);
            if(safetyMessage!=null) GUILayout.Label("Flugweg blockiert · Vorschau sicher angehalten.",ui.Text);
            GUILayout.Label("Quelle: "+source.Info.DisplayName+"\nEisanteil: "+source.KnownWaterPercent.ToString("0")+" %",ui.Small);
            GUILayout.Space(12);
            GUILayout.Label("Fracht   "+agent.CargoKg.ToString("0.00")+" / "+agent.CargoCapacityKg.ToString("0")+" kg\nAbgebaut   "+mined.ToString("0.00")+" kg\nAbgeliefert   "+scenario.DeliveredLiters.ToString("0.00")+" L Wasser\nStationstank   "+scenario.WaterLiters.ToString("0.00")+" L",ui.Text);
            GUILayout.Space(12);
            GUILayout.Label("Akku   "+(agent.BatteryKwh/agent.BatteryCapacityKwh*100).ToString("0")+" %\nTreibwasser   "+agent.FuelLiters.ToString("0.00")+" L\nRückkehrreserve   "+agent.EnergyReserveKwh.ToString("0.0")+" kWh\nFörderrate   "+agent.EffectiveMiningRate.ToString("0.00")+" kg / Spielsekunde",ui.Small);
            GUILayout.Space(15); GUI.enabled=!checking;
            GUI.enabled=!checking && !complete;
            if(GUILayout.Button(complete?"Im Dock angekommen":paused?"Fortsetzen":"Pause",ui.Button)) { paused=!paused; visual.Refresh(0); }
            GUI.enabled=!checking;
            if(GUILayout.Button("Abbau neu starten",ui.Button)) Restart();
            GUI.enabled=!checking && agent.HasTankOrder && !complete;
            if(GUILayout.Button("Mit Fracht zurückkehren",ui.Button)) agent.ReturnToShip();
            GUI.enabled=!checking;
            if(GUILayout.Button(detail?"Gesamte Drohne":"Bohrer im Detail",ui.Button)) SetDetail(!detail);
            if(GUILayout.Button(collectorDetail?"Gesamte Drohne":"Greifer und Frachtbox",ui.Button)) SetCollectorDetail(!collectorDetail);
            if(GUILayout.Button(unloadDetail?"Gesamte Drohne":"Entladeklappe ansehen",ui.Button)) SetUnloadDetail(!unloadDetail);
            GUI.enabled=true; GUILayout.Space(14);
            GUILayout.Label("Greifer: ein Brocken nach dem anderen\nBox: "+visual.VisibleCargoPieces+" / "+TripoMiningDroneVisual.SlotCapacity+" belegte Plätze\nBox: 1,32 × 1,64 × 0,90 m\nDeckel: "+(visual.LidAngle/110*100).ToString("0")+" % geöffnet\nManschette: "+(visual.CollarSealed?"angesaugt / dicht":visual.CollarExtension>0?"legt an":"eingezogen")+"\nSchiebeklappe: "+(visual.GateOpening*100).ToString("0")+" %\nVerankerung: Tragrahmen am Rumpf\n12 kg sind vorläufige Testkapazität.",ui.Small);
            GUILayout.Label("Zeitraffung: Abbau 2× · Entladen 20×\nFlug / Manöver 80×\nSpielzeit: "+WaterScenario.Duration(simulatedSeconds),ui.Small);
            GUILayout.Label("Rechte Maustaste: drehen\nMausrad: Abstand · F11: Vollbild\nESC: Beenden / Weiterspielen",ui.Small);
            GUILayout.Space(12);
            GUILayout.Label("Eigenständiger Test mit echter Abbau-, Fracht-, Energie- und Lieferlogik. Nach der ersten Rückkehr pausiert die Vorschau. Der gespeicherte Kampagnenfortschritt bleibt erhalten.",ui.Small);
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
        private void Require(bool condition,string label)
        { checks++; if(!condition) throw new Exception("Tripo mining: "+label); Debug.Log("TRIPO MINING OK: "+label); }
        private bool Guard(Action action)
        { try { action(); return true; } catch(Exception e) { Debug.LogException(e); safetyMessage=e.Message; paused=true; if(checking || !ready) Application.Quit(1); return false; } }
        private IEnumerator Capture(string name)
        { CameraPose(); yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.GetFullPath(Evidence+"/"+name+".png")); yield return new WaitForSecondsRealtime(.3f); }
        private IEnumerator Validate()
        {
            Directory.CreateDirectory(Evidence);
            File.Delete(Evidence+"/result.json");
            if(!Guard(() => {
                Require(SettingsStore.PathName.Contains("TripoMining-settings"),"isolated settings");
                Require(agent.IsReady && agent.CargoKg==0,"empty operational mining drone in real dock");
                Require(visual.DrillTriangles>4000,"detailed independent auger geometry");
                Require(visual.LidAngle<.1f,"cargo lid closed at starting berth");
                Require(visual.HullMountCount==4,"cargo frame and anchor rails have four measured original-hull roof mounts");
                Require(visual.VisibleCargoPieces==0 && visual.GateOpening==0,"initial cargo is empty and sliding gate shut");
                Require(visual.RearWallClosesCargo,"closed rear armour covers cargo aperture and compression seal meets rear edge within 5 mm");
                Require(visual.CompactGateGuides,"two side guides finish at cartridge height and no overhead gate frame remains");
                Require(visual.EmittedSamples==0 && visual.LooseSamples==0,"no rock emitted before actual drilling");
                Require(visual.CollarExtension==0 && !visual.CollarSealed,"suction collar starts retracted and unsealed");
                var socket=scenario.GetComponent<StationVisual>().TankSocket;
                Require(receiverBulkheadPresent,"unloading receiver has an actual storage bulkhead");
                // Probe inside the channel, excluding its deliberately solid side/floor/roof profiles.
                foreach(float x in new[]{-.7f,0,.7f})foreach(float y in new[]{-.45f,0,.45f})
                {
                    bool blocked=Physics.Raycast(socket.TransformPoint(new Vector3(x,y,.05f)),-socket.forward,out var hit,1.4f,~0,QueryTriggerInteraction.Ignore);
                    Require(!blocked,"wide unloading throat passes through a real opening in storage bulkhead / "+x+" / "+y+" / "+(blocked?hit.collider.name:"clear"));
                }
            })) yield break;
            var quit=FindFirstObjectByType<QuitMenu>();
            if(!Guard(() => {
                Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false;
                quit.HandleEscape();
                Require(QuitMenu.IsOpen && ModalOpen && Blocked,"ESC opens modal quit menu and blocks showcase controls");
                Require(Cursor.lockState==CursorLockMode.None && Cursor.visible,"ESC immediately releases and shows mouse pointer");
                Vector3 position=agent.transform.position; float seconds=simulatedSeconds,battery=agent.BatteryKwh;
                Step(.5f);
                Require(position==agent.transform.position && seconds==simulatedSeconds && battery==agent.BatteryKwh,"quit dialog freezes actual showcase simulation");
                // A modal must retain the pointer even if another view previously captured it.
                Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false;
            })) yield break;
            yield return Capture("13-escape-menu");
            if(!Guard(() => {
                Require(Cursor.lockState==CursorLockMode.None && Cursor.visible,"quit dialog keeps pointer available across frames");
                quit.HandleEscape();
                Require(!QuitMenu.IsOpen && ModalOpen,"closing ESC is consumed before gameplay resumes");
            })) yield break;
            yield return null;
            if(!Guard(() => {
                float seconds=simulatedSeconds; Step(.1f);
                Require(!ModalOpen && simulatedSeconds>seconds,"cancelled quit menu resumes prior simulation");
                paused=true; quit.Open(); quit.Cancel();
            })) yield break;
            yield return null;
            if(!Guard(() => {
                float seconds=simulatedSeconds; Step(.5f);
                Require(paused && simulatedSeconds==seconds,"manual showcase pause survives opening and cancelling quit menu");
                Restart();
            })) yield break;
            yield return Capture("01-dock"); SetDetail(true); yield return Capture("02-tool-stowed"); SetDetail(false);
            yield return Capture("19-static-material-a"); yield return Capture("20-static-material-b");
            bool outbound=false,cutting=false,returning=false,unloading=false,gripping=false,carrying=false,dropping=false,largeChunk=false,gateOpen=false,gateDischarge=false;
            bool singleRock=false,collarClosing=false,collarSealed=false,filled=false;
            int previousEmissions=0;
            float elapsed=0,minimumRimClearance=float.PositiveInfinity; int armGeometrySamples=0;
            while(!complete && elapsed<180)
            {
                float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);
                if(!Guard(() => Step(dt))) yield break; elapsed+=dt; CameraPose();
                if(!Guard(() => { if(visual.CollectorIntersectsLid) throw new Exception("Moving collecting geometry intersects cargo lid"); })) yield break;
                if(!Guard(() => {
                    if(agent.Phase!=DronePhase.Unloading && visual.GateOpening>.001f) throw new Exception("Rear gate opened away from receiver");
                    if(visual.GateOpening>.001f && visual.GateIntersectsLid) throw new Exception("Rear sliding wall intersects closed top lid or latches");
                    if(agent.Phase==DronePhase.Mining && visual.VisibleCargoPieces!=visual.DepositedSamples) throw new Exception("Cargo piece appeared without a gripper deposit");
                    if(visual.LooseSamples>1 || visual.EmittedSamples>visual.DepositedSamples+1) throw new Exception("More than one uncollected drill rock exists");
                    if(visual.EmittedSamples>previousEmissions && !visual.Cutting) throw new Exception("Rock was emitted while drill was stopped");
                    previousEmissions=visual.EmittedSamples;
                    if(visual.GateOpening>0 && (!visual.CollarSealed || visual.CollarContactGap>.005f)) throw new Exception("Gate opened without sealed collar contact");
                })) yield break;
                if(!singleRock && visual.LooseSamples==1 && visual.Cutting)
                {
                    singleRock=true; SetCollectorDetail(true); yield return Capture("22-single-drill-rock"); SetDetail(false);
                }
                if(agent.Phase==DronePhase.Mining)
                {
                    if(!Guard(() => { if(visual.CollectorIntersectsCargo) throw new Exception("Moving collecting geometry intersects cargo box: "+visual.CollectorCollisionDetail); })) yield break;
                    armGeometrySamples++;
                    if(visual.TransferAboveRim && visual.HoldingSample) minimumRimClearance=Mathf.Min(minimumRimClearance,visual.SampleRimClearance);
                }
                if(!outbound && agent.Phase==DronePhase.Outbound)
                {
                    outbound=true;
                    if(!Guard(() => Require(visual.LidAngle<.1f,"cargo lid closed during outbound flight"))) yield break;
                    yield return Capture("03-flight");
                }
                if(!gripping && visual.HoldingSample)
                {
                    gripping=true;
                    if(!Guard(() => {
                        Require(visual.HeldSampleError<.001f,"collected chunk follows actual centre between articulated jaws");
                        Vector3 grip=visual.Grip,chunk=visual.SamplePosition; float lid=visual.LidAngle; int emissions=visual.EmittedSamples; paused=true; Step(.5f);
                        Require(Vector3.Distance(grip,visual.Grip)<.001f && Vector3.Distance(chunk,visual.SamplePosition)<.001f,"pause freezes collecting joints and held chunk");
                        Require(Mathf.Abs(lid-visual.LidAngle)<.001f,"pause freezes articulated lid");
                        Require(emissions==visual.EmittedSamples && !visual.Cutting,"stopped drilling and pause emit no additional block");
                        paused=false;
                    })) yield break;
                    SetCollectorDetail(true); yield return Capture("09-gripper-pickup"); SetDetail(false);
                }
                if(!carrying && visual.TransferAboveRim && visual.HoldingSample)
                {
                    carrying=true;
                    if(!Guard(() => Require(visual.HeldSampleError<.001f,"carried chunk remains in jaws above cargo rim"))) yield break;
                    SetCollectorDetail(true); yield return Capture("10-gripper-transfer"); SetDetail(false);
                }
                if(!dropping && visual.DroppingSample && visual.SampleInsideBox)
                {
                    dropping=true;
                    if(!Guard(() => {
                        Require(!visual.HoldingSample && visual.SampleInsideBox,"released sample drops through open top into transparent box");
                        Require(visual.LidAngle>109,"cargo lid fully open before depositing sample");
                    })) yield break;
                    SetCollectorDetail(true); yield return Capture("11-gripper-deposit"); SetDetail(false);
                }
                if(!largeChunk && visual.HoldingSample && visual.SampleSize>.4f)
                {
                    largeChunk=true; SetCollectorDetail(true); yield return Capture("12-large-chunk"); SetDetail(false);
                }
                if(!cutting && agent.Phase==DronePhase.Mining && agent.CargoKg>3)
                {
                    cutting=true;
                    if(!Guard(() => {
                        Require(visual.Cutting && visual.VisibleCargoPieces>0 && visual.VisibleCargoPieces==visual.DepositedSamples,"only actual gripper deposits fill visible cargo");
                        Require(visual.SupportsAttached,"both stabilisers touch actual asteroid collider");
                        Require(Vector3.Distance(visual.Tip,agent.SurfacePoint)<.025f,"actual auger tip contacts mining surface");
                        Require(Mathf.Abs(mined-agent.CargoKg)<.02f,"source loss equals carried raw mass");
                        Require(agent.BatteryKwh<startBattery,"mining consumes actual battery");
                        Require(agent.BatteryKwh>=agent.RequiredReturnEnergyKwh && agent.FuelLiters>=agent.RequiredReturnFuelLiters,"return supplies protected while mining");
                        float raw=source.RemainingRawKg,kg=agent.CargoKg,battery=agent.BatteryKwh;
                        Vector3 point=agent.transform.position; paused=true; Step(.5f);
                        Require(raw==source.RemainingRawKg && kg==agent.CargoKg && battery==agent.BatteryKwh && point==agent.transform.position && !visual.Cutting,"pause freezes resource extraction and effects");
                        paused=false; Step(.02f);
                    })) yield break;
                    yield return Capture("04-mining"); SetDetail(true); yield return Capture("05-drill-contact"); SetDetail(false);
                }
                if(!returning && agent.Phase==DronePhase.Returning)
                {
                    returning=true;
                    if(!Guard(() => Require(visual.LidAngle<.1f && visual.ArmStowed,"collecting arm stowed and cargo lid closed for loaded return"))) yield break;
                    yield return Capture("06-loaded-return");
                    SetUnloadDetail(true); yaw=-55; distance=3.6f; pitch=20;
                    yield return Capture("21-compact-closed-rear-wall"); SetDetail(false);
                }
                if(!filled && visual.ContainerFull && agent.Phase==DronePhase.Mining)
                {
                    filled=true;
                    if(!Guard(() => {
                        Require(visual.VisibleCargoPieces==TripoMiningDroneVisual.SlotCapacity && visual.EmittedSamples==TripoMiningDroneVisual.SlotCapacity,"single sequential drill blocks fill all twelve visible cargo slots");
                        Require(visual.LooseSamples==0 && visual.PackedCargoInside,"filled rocks remain inside cartridge walls floor and lid clearance");
                    })) yield break;
                    SetCollectorDetail(true); yield return Capture("23-filled-cartridge"); SetDetail(false);
                }
                if(!unloading && agent.Phase==DronePhase.Unloading)
                {
                    unloading=true;
                    if(!Guard(() => Require(Vector3.Distance(agent.CargoOutletPoint,agent.TankInlet)<.025f,"new rear cargo coupling meets station tank socket"))) yield break;
                    yield return Capture("07-tank-transfer");
                }
                if(!gateOpen && agent.Phase==DronePhase.Unloading && visual.GateOpening>.89f)
                {
                    gateOpen=true;
                    if(!Guard(() => {
                        Require(Vector3.Distance(visual.GateCentre,agent.TankInlet)<.025f,"wide rear sliding wall seals at real receiver before opening");
                        Require(visual.GateOpening<=.9001f,"rear wall opens at most 90 percent and retains overlap in short guides");
                        Require(visual.CollarSealed && visual.CollarContactGap<.005f,"gate opens only after collar touches and seals cartridge");
                        float opening=visual.GateOpening; paused=true; Step(.5f);
                        Require(Mathf.Abs(opening-visual.GateOpening)<.001f,"pause preserves rear sliding gate pose"); paused=false;
                    })) yield break;
                    SetUnloadDetail(true); yield return Capture("17-sliding-gate-open"); SetDetail(false);
                }
                if(!gateDischarge && agent.Phase==DronePhase.Unloading && visual.UnloadedPieces>0)
                { gateDischarge=true; SetUnloadDetail(true); yield return Capture("18-rocks-in-receiver"); SetDetail(false); }
                if(!collarClosing && agent.Phase==DronePhase.Unloading && visual.CollarExtension>.15f && visual.CollarExtension<.9f)
                {
                    collarClosing=true;
                    if(!Guard(() => Require(visual.GateOpening==0 && visual.UnloadedPieces==0,"collar approaches while rear hatch and cargo remain closed"))) yield break;
                    SetUnloadDetail(true); yield return Capture("25-collar-approach"); SetDetail(false);
                }
                if(!collarSealed && agent.Phase==DronePhase.Unloading && visual.CollarSealed && visual.GateOpening==0)
                {
                    collarSealed=true;
                    if(!Guard(() => {
                        Require(visual.CollarContactGap<.005f,"suction collar contacts cartridge before hatch opening");
                        float extension=visual.CollarExtension; paused=true; Step(.5f);
                        Require(visual.CollarExtension==extension && visual.CollarSealed && visual.GateOpening==0,"pause freezes attached collar and closed hatch"); paused=false;
                    })) yield break;
                    SetUnloadDetail(true); yield return Capture("24-collar-sealed-hatch-closed"); SetDetail(false);
                }
                yield return null;
            }
            if(!Guard(() => {
                Require(complete && outbound && cutting && returning && unloading,"complete actual mining and delivery mission");
                Require(scenario.Deliveries==1 && scenario.DeliveredLiters>0,"one real water delivery recorded");
                Require(Mathf.Abs(mined-12)<.02f && agent.CargoKg<.01f,"12 kg extracted and unloaded without duplicate cargo");
                Require(Mathf.Abs(scenario.DeliveredLiters-mined*source.WaterFraction)<.025f,"raw mixture converted using actual ice fraction");
                Require(Mathf.Abs(scenario.WaterLiters-startWater-scenario.DeliveredLiters)<.025f,"station tank mass balance before refuelling");
                Require(Vector3.Distance(agent.transform.position,agent.HomePosition)<.025f && Quaternion.Angle(agent.transform.rotation,agent.HomeRotation)<.1f,"returned to exact real charging berth");
                Require(scenario.Mining.CompletedTrips==1,"productive completed trip awards real material experience");
                Require(agent.BatteryKwh>=agent.EnergyReserveKwh && agent.FuelLiters>=agent.FuelReserveLiters,"mission completes with safety reserves");
                Require(!visual.Cutting && visual.VisibleCargoPieces==0,"parked drill stops and cargo display empties");
                Require(gripping && carrying && dropping && visual.DepositedSamples>=3,"collecting arm completes pickup transfer and release cycles");
                Require(visual.CollectedSizes==3,"articulated jaws collect three different sample sizes");
                Require(visual.ArmStowed && !visual.HoldingSample,"collecting tool folds after mining and holds no phantom cargo");
                Require(armGeometrySamples>30 && !visual.CollectorIntersectsCargo,"moving collecting mesh stays outside cargo box throughout mining");
                Require(minimumRimClearance>.06f,"all transferred chunk sizes clear actual cargo rim by at least 6 cm");
                Require(visual.LidAngle<.1f && !visual.CollectorIntersectsLid,"cargo lid closed and collecting geometry clear at final berth");
                Require(gateOpen && gateDischarge && visual.UnloadedPieces==visual.DepositedSamples,"same gripper-loaded pieces pass through receiver and rear gate closes");
                Require(visual.GateOpening==0,"rear sliding gate closed before leaving receiver");
                Require(visual.RearWallClosesCargo,"rear wall seals against cartridge again after unloading and return");
                Require(singleRock && filled && visual.EmittedSamples==TripoMiningDroneVisual.SlotCapacity && visual.EmissionsWithoutCutting==0 && visual.MaximumEmissionTipDistance<.5f,"all cargo came from one-at-a-time drilling emissions at the auger tip");
                Require(collarClosing && collarSealed && visual.CollarExtension==0 && !visual.CollarSealed,"collar approach seal gate sequence completed and collar retracted after hatch closure");
                Require(visual.DischargeClearanceSamples>10 && visual.MinimumDischargeClearance>.015f,"all unloading rock vertices clear the 90 percent gate aperture by at least 15 mm / samples="+visual.DischargeClearanceSamples+" margin="+visual.MinimumDischargeClearance+" "+visual.DischargeClearanceDetail);
                Require(navigator.ClearanceSamples>100,"actual flight and dock poses checked against station asteroid and cradle colliders");
            })) yield break;
            yield return Capture("08-delivered-dock");
            if(!Guard(() => {
                quit.Open(); quit.RequestReturnToStart();
                Require(SaveGameMenu.IsOpen && !QuitMenu.IsOpen && ModalOpen && Cursor.visible && Cursor.lockState==CursorLockMode.None,"quit action opens usable save-before-leaving dialog");
            })) yield break;
            yield return Capture("14-before-leaving");
            var saves=FindFirstObjectByType<SaveGameMenu>();
            saves.CancelDialog(); yield return null;
            if(!Guard(() => {
                Require(!ModalOpen && complete && paused,"cancel leaving keeps completed showcase and manual pause");
                quit.Open(); quit.RequestReturnToStart(); saves.LeaveWithoutManualSave();
                Require(StartMenu.IsOpen && !SaveGameMenu.IsOpen && !QuitMenu.IsOpen,"leave action reaches start menu without stuck modal");
            })) yield break;
            File.WriteAllText(Evidence+"/result.json","{\"pass\":true,\"checks\":"+checks+",\"minedKg\":"+mined.ToString("0.000",System.Globalization.CultureInfo.InvariantCulture)+",\"deliveredLiters\":"+scenario.DeliveredLiters.ToString("0.000",System.Globalization.CultureInfo.InvariantCulture)+",\"collectedSizes\":"+visual.CollectedSizes+",\"depositedSamples\":"+visual.DepositedSamples+",\"armGeometrySamples\":"+armGeometrySamples+",\"minimumRimClearanceMeters\":"+minimumRimClearance.ToString("0.000",System.Globalization.CultureInfo.InvariantCulture)+",\"savedCampaignModified\":false}");
            Debug.Log("MINING NAVIGATION: routes="+navigator.PlannedRoutes+" detours="+navigator.Detours+" poseSamples="+navigator.ClearanceSamples+" dockVertexSamples="+navigator.DockVertexSamples+" berthFitDrop="+berthFitDrop+" unloadedPieces="+visual.UnloadedPieces);
            Debug.Log("MINING GATE: maxOpening="+MaintenanceLaserVisual.MaxCargoGateOpening+" dischargeSamples="+visual.DischargeClearanceSamples+" minimumClearance="+visual.MinimumDischargeClearance);
            Debug.Log("MINING COLLECTION: emitted="+visual.EmittedSamples+" deposited="+visual.DepositedSamples+" unloaded="+visual.UnloadedPieces+" tipDistance="+visual.MaximumEmissionTipDistance+" emissionWithoutCutting="+visual.EmissionsWithoutCutting);
            Debug.Log("TRIPO MINING CHECK PASSED: "+checks+" checks, actual extraction and delivery"); Application.Quit(0);
        }
        private void OnDestroy() { ui.Dispose(); if(receiverPortMesh!=null) Destroy(receiverPortMesh); }
    }
}
