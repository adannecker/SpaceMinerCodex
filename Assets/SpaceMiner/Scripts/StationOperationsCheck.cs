#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    public sealed class StationOperationsCheck:MonoBehaviour
    {
        private const string Folder="Logs/StationOperations";
        private int checks;
        private StationOperations ops;
        private WaterScenario scenario;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-stationOperationsCheck")>=0)new GameObject("Station operations acceptance").AddComponent<StationOperationsCheck>();}
        private void Require(bool value,string name){if(!value)throw new Exception("OPERATIONS: "+name);checks++;Debug.Log("OPERATIONS OK: "+name);}
        private StationWorkOrder Finish(StationJobKind kind,string target="",float limit=2000)
        {
            Require(ops.Enqueue(kind,target),"enqueue "+kind);var job=ops.State.orders[ops.State.orders.Count-1];
            for(float t=0;t<limit&&!job.complete;t+=5)scenario.Advance(5);
            Require(job.complete&&!job.cancelled,"complete "+kind+" / "+job.status+" / "+(job.workshop==null?"":JsonUtility.ToJson(job.workshop)));return job;
        }
        private void Render(string file,Vector3 eye,Vector3 target,float fov=65)
        {
            var camera=new GameObject("Operations render camera").AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;
            camera.transform.position=ops.Layout.transform.TransformPoint(eye);camera.transform.LookAt(ops.Layout.transform.TransformPoint(target));camera.fieldOfView=fov;camera.nearClipPlane=.08f;camera.farClipPlane=500;
            var rt=new RenderTexture(1600,1000,24);var previous=RenderTexture.active;camera.targetTexture=rt;
            ops.RefreshViewLighting(camera,!(file.StartsWith("04-")||file.StartsWith("06-")||file.StartsWith("07-")));
            try{camera.Render();RenderTexture.active=rt;var pixels=new Texture2D(1600,1000,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1600,1000),0,0);pixels.Apply();File.WriteAllBytes(Folder+"/"+file+".png",pixels.EncodeToPNG());Destroy(pixels);}
            finally{RenderTexture.active=previous;camera.targetTexture=null;rt.Release();Destroy(rt);Destroy(camera.gameObject);ops.RefreshViewLighting(Camera.main,StationInteriorMode.IsInside);}
        }
        private IEnumerator Start()
        {
            Directory.CreateDirectory(Folder);File.Delete(Folder+"/result.json");File.Delete(Folder+"/error.txt");yield return new WaitForSecondsRealtime(1);
            FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride=Path.GetFullPath(Folder+"/Saves");
            CinematicLibrary.PreferencePrefix="SpaceMiner.Test.StationOperations.";
            FindFirstObjectByType<StartMenu>().StartDemo();FindFirstObjectByType<IntroSequence>().Skip();yield return null;yield return null;
            try{
                scenario=FindFirstObjectByType<WaterScenario>();scenario.SetSimulationRate(0);scenario.Scanner.DismissDialogue();ops=scenario.Operations;
                Require(ops!=null&&ops.Live,"live campaign facade installed");Require(ops.TerminalCount==6,"five command terminals including power plus local ice plant terminal; freestanding sector desks removed");Require(ops.LampCount>=20,"authored interior/exterior lights");
                Require(ReferenceEquals(scenario.WaterTank,ops.State.inventory.water),"water has a single shared reservoir");
                Require(StationOperations.Valid(ops.Capture()),"initial serialized campaign is valid");
                Physics.SyncTransforms();Vector3 portEye=ops.Layout.transform.TransformPoint(RingWalkthrough.At(48,51.5f,2));Require(Physics.Raycast(portEye,ops.Layout.transform.TransformDirection(-RingWalkthrough.At(1,51.5f)),out var portHit,15,1<<25)&&ops.ElementAtHit(portHit)=="expansion-2-0","capped mounting slot selects its own build action in the world");
                var a=new FluidTank(StationResources.Water,10,7);var b=new FluidTank(StationResources.Water,10,8);
                Require(!a.TransferTo(b,3)&&a.amount==7&&b.amount==8,"transfer overflow is atomic");Require(a.TransferTo(b,2)&&a.amount==5&&b.amount==10,"valid tank transfer conserves water");
                bool bad=false;try{new SolidStore(StationResources.Water,10);}catch(ArgumentException){bad=true;}Require(bad,"wrong physical storage phase rejected");
                float startWater=scenario.WaterLiters;Require(!ops.State.inventory.TryConsume(new[]{new ResourceAmount("water",1),new ResourceAmount("parts",1000)})&&scenario.WaterLiters==startWater,"multi-resource recipe is atomic");
                var mode=StationInteriorMode.Current;mode.RestoreInterior(ops.Layout.Console.position-Vector3.forward, true);scenario.Advance(11);Require(scenario.Scanner.Scan(),"real console scan");scenario.Scanner.DismissDialogue();mode.CloseConsole();
                var ice=Array.Find(scenario.Asteroids,s=>s.CanMineWater);Require(scenario.AssignTankOrder(ice),"water mission starts with shared live station supply");
                // A delivery now counts each real raw transfer. Wait for the whole cargo,
                // rather than cancelling the flight after its first few kilograms.
                for(int t=0;t<36000&&(scenario.Deliveries==0||scenario.Worker.CargoKg>.001f);t+=10)scenario.Advance(10);
                Require(scenario.Deliveries>0&&scenario.Worker.CargoKg<=.001f,"actual imported drone completes raw delivery with normal water buffer and live supply");
                // Explicit operator choice: reserve scarce starting power for charging
                // and later workshop jobs. Paid refining is accepted in the plant suite.
                ops.IcePlant.ProcessingEnabled=false;scenario.Scanner.DismissDialogue();
                for(int t=0;t<36000&&(!scenario.Worker.IsReady||scenario.Worker.HasTankOrder);t+=10)scenario.Advance(10);
                Require(scenario.Worker.IsReady&&!scenario.Worker.HasTankOrder,"water worker safely returns and actually recharges for subsequent jobs");
                Finish(StationJobKind.Diagnose);Require(ops.State.diagnosed,"diagnosis persisted in state");
                Finish(StationJobKind.Salvage);Require(ops.State.debrisBatches==1&&ops.State.inventory.scrap.amount==12,"finite debris becomes actual inventory");
                Require(!ops.Enqueue(StationJobKind.Salvage,"debris-0"),"same collected debris cannot be harvested twice");
                Finish(StationJobKind.ActivateHelper);Require(ops.State.helper,"internal helper actually unlocked");
                Finish(StationJobKind.Recycle);Require(ops.State.inventory.scrap.amount==2&&ops.State.inventory.iron.amount==6&&ops.State.inventory.unknown.amount==3,"sorting conserves the ten kilogram batch");
                Finish(StationJobKind.PrintParts);Require(ops.State.inventory.parts.amount>=3&&ops.State.inventory.iron.amount==4,"printer consumes known material");
                // Supply fixture allows independent tests of later jobs without waiting hours for drone charging.
                scenario.DepositWater(60);ops.State.power.storedKwh=20;
                ops.State.inventory.carbon.Set(0); // Explicit missing-input fixture; recycling yielded 1 kg earlier.
                Require(ops.Enqueue(StationJobKind.Food),"queue food before carbon exists");scenario.Advance(1);var blocked=ops.State.orders[ops.State.orders.Count-1];Require(!blocked.started&&blocked.status.Contains("Material"),"missing carbon blocks production");ops.Cancel(blocked.id);
                Finish(StationJobKind.Carbon);Require(ops.State.inventory.carbon.amount>=8,"carbon sample returns from finite external target");
                float portions=ops.State.inventory.food.amount,supplements=ops.State.inventory.supplement.amount;
                Finish(StationJobKind.Food);Require(ops.State.inventory.food.amount>portions+3.9f&&ops.State.inventory.supplement.amount==supplements-1,"food needs finite supplement cartridge");
                Finish(StationJobKind.ResearchRepair);Require(ops.State.technologies.Contains("repair"),"campaign repair research unlock");
                Require(ops.Enqueue(StationJobKind.Refit,"0:Greifer"),"queue temporary tool maintenance");scenario.Advance(1);var maintenance=ops.State.orders[ops.State.orders.Count-1];Require(maintenance.started&&ops.IsRefitting(0)&&!scenario.AssignTankOrder(ice),"actual refit exclusively reserves target drone against water missions");ops.Cancel(maintenance.id);for(int i=0;i<200&&!maintenance.complete;i++)scenario.Advance(1);Require(maintenance.complete&&!ops.IsRefitting(0)&&ops.State.tools[0]=="Bohrer","cancelled maintenance releases unchanged target drone after return");
                int laserStock=ops.State.spareLasers,drillStock=ops.State.spareDrills;
                Finish(StationJobKind.Refit,"0:Laser");Require(ops.State.tools[0]=="Laser"&&ops.State.spareLasers==laserStock-1&&ops.State.spareDrills==drillStock+1,"refit transfers actual tool inventory");
                Finish(StationJobKind.Repair,"solar-a");Require(ops.Element("solar-a").condition>.999f&&ops.State.repairExperience==1,"targeted repair improves actual condition");
                Finish(StationJobKind.ActivateSecond);Require(ops.State.second&&scenario.Drones[1].IsOperational&&scenario.Drones[1].BatteryKwh<scenario.Drones[1].BatteryCapacityKwh,"second drone unlocked without free full battery");
                Require(scenario.Drones[2].Phase==DronePhase.Disabled,"other exterior drones remain inactive");
                // Dedicated later-build fixture isolates geometry/cost/persistence from repeated salvage trips.
                ops.State.inventory.parts.Add(20);ops.State.inventory.iron.Add(20);
                Finish(StationJobKind.ResearchExpansion);Require(ops.State.technologies.Contains("expansion"),"module technology unlock");
                float ironBefore=ops.State.inventory.iron.amount,partsBefore=ops.State.inventory.parts.amount;
                Finish(StationJobKind.Expand,"expansion-2-0");Require(ops.Element("expansion-2-0").built&&ops.State.inventory.iron.amount==ironBefore-8&&ops.State.inventory.parts.amount==partsBefore-6,"module construction consumes actual parts and material");
                var insideModule=ops.Layout.transform.TransformPoint(RingWalkthrough.At(42,51.5f,.05f));Require(ops.Layout.ContainsPosition(insideModule),"new wedge is a valid persistent walk area");
                Physics.SyncTransforms();mode.RestoreInterior(ops.Layout.transform.TransformPoint(RingWalkthrough.At(36,51.5f,.05f)),false,51.5f,0);
                for(int step=0;step<65;step++)mode.Walk(Vector2.up,.02f);
                Vector3 walked=ops.Layout.transform.InverseTransformPoint(mode.FeetPosition);Require(new Vector2(walked.x,walked.z).magnitude>40,"actual CharacterController crosses installed module aperture");
                Finish(StationJobKind.Repair,"sector-3");Require(ops.Element("sector-3").condition>.999f&&ops.Layout.ContainsPosition(ops.Layout.transform.TransformPoint(RingWalkthrough.At(33.5f,90,.05f))),"ring repair restores accessible structural area");
                Physics.SyncTransforms();Require(Physics.Raycast(ops.Layout.transform.TransformPoint(RingWalkthrough.At(33.5f,90,1)),Vector3.down,2,1<<25),"repaired ring has a real upward collision floor");
                Finish(StationJobKind.Repair,"water-1");Require(scenario.TankCapacityLiters==300&&ops.Layout.ContainsPosition(ops.Layout.transform.TransformPoint(RingWalkthrough.At(43,210,.05f))),"water wreck repair restores walk area and real 100 L tank capacity");
                Finish(StationJobKind.Repair,"service-2");Physics.SyncTransforms();Require(ops.Layout.ContainsPosition(ops.Layout.transform.TransformPoint(RingWalkthrough.At(15,180,.05f)))&&Physics.Raycast(ops.Layout.transform.TransformPoint(RingWalkthrough.At(15,180,1)),Vector3.down,2,1<<25),"broken reactor link receives real continuous collision floor");
                ops.ToggleWater(9);Require(!ops.WaterConnected(0),"intermediate sector valve severs water route through intact ring");ops.ToggleWater(9);
                Render("06-neuer-keilanbau",RingWalkthrough.At(54,51.5f,14),RingWalkthrough.At(42,51.5f,2),65);
                Render("07-reparierter-ring",RingWalkthrough.At(42,90,16),RingWalkthrough.At(33.5f,90,2),65);
                ops.TogglePower(0);float battery=ops.State.power.storedKwh;scenario.Advance(5);Require(ops.State.power.GenerationKw<scenario.ReactorPowerKw,"sector isolation changes real generation");ops.TogglePower(0);
                ops.ToggleWater(7);Require(ops.Enqueue(StationJobKind.Food),"queue while water isolated");var waiting=ops.State.orders[ops.State.orders.Count-1];scenario.Advance(2);Require(!waiting.started&&waiting.status.Contains("Wasser"),"closed sector valve blocks water recipe");ops.ToggleWater(7);ops.Cancel(waiting.id);
                Require(ops.Enqueue(StationJobKind.PrintParts)&&ops.Enqueue(StationJobKind.Recycle),"multiple jobs queue");var last=ops.State.orders[ops.State.orders.Count-1];Require(ops.MoveOrder(last.id,0)&&ops.State.orders[0].id==last.id,"priority reorder keeps stable identity");ops.Cancel(last.id);
                int meshCount=ops.OwnedMeshCount;var saved=SaveGameStore.Capture(scenario,"Aufbau-Abnahme","");File.WriteAllText(Folder+"/before-validation.json",JsonUtility.ToJson(saved,true));SaveGameStore.Validate(saved,scenario);string json=JsonUtility.ToJson(saved);var roundTrip=JsonUtility.FromJson<SavedGame>(json);SaveGameStore.Apply(roundTrip,scenario);Require(ops.OwnedMeshCount==meshCount,"load rebuilds geometry without retaining discarded meshes");
                Require(ops.State.helper&&ops.State.second&&ops.State.technologies.Contains("repair")&&scenario.Drones[1].IsOperational,"save restores unlocks and second drone policy");
                Require(ReferenceEquals(scenario.WaterTank,ops.State.inventory.water),"restored inventory still shares water");
                Require(ops.Layout.ContainsPosition(insideModule)&&Mathf.Abs(ops.State.inventory.scrap.capacity-200)<.001f,"built module and capacity restored without double growth");
                var legacy=SaveGameStore.Capture(scenario,"Alter Spielstand","");legacy.stationCampaign=null;foreach(var oldDrone in legacy.drones){oldDrone.iceDeliveryVersion=0;oldDrone.rawIceDelivery=false;oldDrone.retainedCargoFlight=false;oldDrone.unloadInitialCargoKg=0;}legacy.water=250;SaveGameStore.Apply(legacy,scenario);Require(!ops.State.helper&&!ops.State.second&&!ops.Element("expansion-2-0").built,"legacy save resets new campaign systems safely");Require(scenario.TankCapacityLiters==300&&scenario.WaterLiters==250,"legacy enlarged tank retains water above new starting capacity");
                SaveGameStore.Apply(roundTrip,scenario);Require(ops.State.second&&ops.Element("expansion-2-0").built&&ops.State.inventory.scrap.capacity==200,"reload rebuilds real module exactly once after legacy reset");
                foreach(var pendingOrder in ops.State.orders.FindAll(o=>!o.started&&!o.complete))ops.Cancel(pendingOrder.id);
                float refundIron=ops.State.inventory.iron.amount;Require(ops.Enqueue(StationJobKind.PrintParts),"queue reserved-input cancellation test");scenario.Advance(1);var interrupted=ops.State.orders[ops.State.orders.Count-1];Require(interrupted.started&&ops.State.inventory.iron.amount==refundIron-2,"active job really reserves iron");string interruptedId=interrupted.id;
                var midJob=SaveGameStore.Capture(scenario,"Aktiver Auftrag","");SaveGameStore.Apply(JsonUtility.FromJson<SavedGame>(JsonUtility.ToJson(midJob)),scenario);ops.Cancel(interruptedId);for(int i=0;i<200&&!ops.State.orders.Find(o=>o.id==interruptedId).complete;i++)scenario.Advance(1);
                Require(ops.State.orders.Find(o=>o.id==interruptedId).complete&&Mathf.Abs(ops.State.inventory.iron.amount-refundIron)<.0001f,"active saved job cancels after safe return and refunds exactly once");
                var copied=ops.Capture();bool finiteGuard=false;try{copied.inventory.carbon.Set(float.NaN);}catch(ArgumentException){finiteGuard=true;}Require(finiteGuard,"container API rejects nonfinite values");JsonUtility.FromJsonOverwrite("{\"storedAmount\":-1}",copied.inventory.carbon);Require(!StationOperations.Valid(copied),"corrupt serialized reservoir rejected");
                copied=ops.Capture();copied.orders.Add(copied.orders[0]);Require(!StationOperations.Valid(copied),"duplicate work identity rejected");
                copied=ops.Capture();copied.orders.Find(o=>o.kind==StationJobKind.Repair).target="missing-module";Require(!StationOperations.Valid(copied),"unknown saved repair target rejected");
                Require(ops.Enqueue(StationJobKind.Salvage)&&ops.Enqueue(StationJobKind.Salvage),"successive salvage orders choose different unclaimed sources");var pending=ops.State.orders.FindAll(o=>o.kind==StationJobKind.Salvage&&!o.complete);Require(pending.Count==2&&pending[0].target!=pending[1].target,"queued finite sources have distinct reservations");foreach(var queued in pending)ops.Cancel(queued.id);
                var research=Array.Find(ops.Layout.GetComponentsInChildren<StationTerminal>(),terminal=>terminal.kind==StationTerminalKind.Research);
                var near=ops.NearestTerminal(research.transform.TransformPoint(new Vector3(0,.04f,-1.55f)));Require(near!=null&&near.kind==StationTerminalKind.Research&&ops.InteractionPrompt(near.transform.position).StartsWith("E"),"physical research terminal has contextual E prompt");
                ops.OpenTerminal(near);Require(ops.PanelOpen&&StationOperations.BlocksInput&&Cursor.lockState==CursorLockMode.None,"terminal releases mouse and blocks world input");ops.ClosePanel();
                Render("01-kommando",new Vector3(0,2.7f,50),new Vector3(0,1.8f,41),75);
                Render("02-versorgungsring",RingWalkthrough.At(33.5f,325,1.7f),RingWalkthrough.At(34,345,3.2f),75);
                Render("03-werkzeugregal",RingWalkthrough.At(43,30,2.8f),RingWalkthrough.At(46,36,1.8f),70);
                Render("04-schrott-und-dock",RingWalkthrough.At(72,24,13),RingWalkthrough.At(48,32,2),70);
                Render("05-werkstofflager",RingWalkthrough.At(26,34,2),RingWalkthrough.At(23,26,1),75);
                Require(StationModelLod.GroupCount==20,"ten drone and ten cradle distance groups");
                File.WriteAllText(Folder+"/result.json","{\"passed\":true,\"checks\":"+checks+",\"scope\":\"domain, actual jobs, finite resources, unlocks, persistence, terminal contract; water supply fixture for later recipes\"}");Debug.Log("OPERATIONS CHECK PASSED: "+checks);StartCoroutine(MeasureFrames());
            }catch(Exception e){File.WriteAllText(Folder+"/error.txt",e.ToString());Debug.LogException(e);Application.Quit(1);}
        }
        [Serializable] private sealed class Sample {public string view;public bool original;public float medianMs,p95Ms;public long batches,triangles,setPasses;}
        [Serializable] private sealed class Performance {public string gpu,cpu;public int width,height;public string conditions="Synchronous offscreen render + one-pixel GPU readback; fixed camera; simulation paused. Comparison of rendering cost, NOT playable FPS or isolated GPU time.";public System.Collections.Generic.List<Sample> samples=new System.Collections.Generic.List<Sample>();}
        private IEnumerator MeasureFrames()
        {
            var mode=StationInteriorMode.Current;mode.Exit(true);var orbit=Camera.main.GetComponent<OrbitCamera>();orbit.enabled=false;scenario.SetSimulationRate(0);scenario.Scanner.DismissDialogue();
            QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
            var report=new Performance{gpu=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,width=1600,height=1000};
            var renderTarget=new RenderTexture(1600,1000,24);var readback=new Texture2D(1,1,TextureFormat.RGB24,false);var previousTarget=Camera.main.targetTexture;var active=RenderTexture.active;Camera.main.targetTexture=renderTarget;
            Vector3[] eyes={new Vector3(0,2.5f,48),RingWalkthrough.At(80,30,10),new Vector3(110,90,120)};
            Vector3[] targets={new Vector3(0,2,41),RingWalkthrough.At(65,30,3),Vector3.zero};
            for(int pose=0;pose<eyes.Length;pose++)for(int variant=0;variant<2;variant++){
                StationModelLod.ForceOriginal(variant==0);ops.ForceUnbatchedHull(variant==0);MaintenanceLaserVisual.ForceUnbatchedTools(variant==0);Camera.main.transform.position=ops.Layout.transform.TransformPoint(eyes[pose]);Camera.main.transform.LookAt(ops.Layout.transform.TransformPoint(targets[pose]));
                using var batches=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render,"Batches Count");using var triangles=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render,"Triangles Count");using var passes=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render,"SetPass Calls Count");
                for(int warm=0;warm<3;warm++){Camera.main.Render();RenderTexture.active=renderTarget;readback.ReadPixels(new Rect(0,0,1,1),0,0);yield return null;}
                var times=new float[24];for(int sample=0;sample<times.Length;sample++){var clock=System.Diagnostics.Stopwatch.StartNew();Camera.main.Render();RenderTexture.active=renderTarget;readback.ReadPixels(new Rect(0,0,1,1),0,0);clock.Stop();times[sample]=(float)clock.Elapsed.TotalMilliseconds;yield return null;}
                Array.Sort(times);report.samples.Add(new Sample{view=pose==0?"command":pose==1?"drone_bay":"overview",original=variant==0,medianMs=times[12],p95Ms=times[22],batches=batches.Valid?batches.LastValue:-1,triangles=triangles.Valid?triangles.LastValue:-1,setPasses=passes.Valid?passes.LastValue:-1});
            }
            Camera.main.targetTexture=previousTarget;RenderTexture.active=active;renderTarget.Release();Destroy(renderTarget);Destroy(readback);StationModelLod.ForceOriginal(false);ops.ForceUnbatchedHull(false);MaintenanceLaserVisual.ForceUnbatchedTools(false);File.WriteAllText(Folder+"/performance.json",JsonUtility.ToJson(report,true));Debug.Log("OPERATIONS PERFORMANCE SAMPLES RECORDED");Application.Quit(0);
        }
    }
}
#endif
