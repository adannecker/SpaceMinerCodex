#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    // End-to-end acceptance for the revised station, including real CharacterController and water delivery.
    public sealed class KeilStationCheck : MonoBehaviour
    {
        private const string Folder="Logs/KeilStation";
        private int checks;
        private StationInteriorMode mode;
        private StationInteriorLayout layout;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"-stationKeilCheck")>=0 || Array.IndexOf(args,"-startStationCheck")>=0)new GameObject("Keil station acceptance").AddComponent<KeilStationCheck>();
        }
        private void Require(bool ok,string label)
        {checks++;if(!ok)throw new Exception("KEIL: "+label);Debug.Log("KEIL OK: "+label);}
        private Vector3 LocalFeet=>layout.transform.InverseTransformPoint(mode.FeetPosition);
        private void At(float radius,float angle)
        {mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(radius,angle,.04f)),false,angle,0);Physics.SyncTransforms();}
        private void Walk(float seconds)
        {
            for(float t=0;t<seconds;t+=.02f){mode.Walk(Vector2.up,.02f);layout.Airlock.Advance(.02f,mode.FeetPosition);}
        }
        private void CycleToExit()
        {
            for(int i=0;i<80 && layout.Airlock.Phase!=AirlockPhase.AwaitingExit;i++)
            {
                layout.Airlock.Advance(.1f,mode.FeetPosition);
                Require(!(layout.Airlock.HabitatOpen>0 && layout.Airlock.RingOpen>0),"two bay doors remain mutually interlocked / "+i);
            }
            Require(layout.Airlock.Phase==AirlockPhase.AwaitingExit,"airlock completes close/equalize/open sequence");
        }
        private void Render(string name,Vector3 eye,Vector3 target,float fov=55)
        {
            var go=new GameObject("Keil inspection camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;
            camera.transform.position=layout.transform.TransformPoint(eye);camera.transform.LookAt(layout.transform.TransformPoint(target));
            camera.nearClipPlane=.08f;camera.farClipPlane=1500;camera.fieldOfView=fov;
            var rt=new RenderTexture(1600,1000,24);var previous=RenderTexture.active;camera.targetTexture=rt;
            try{camera.Render();RenderTexture.active=rt;var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(Folder+"/"+name+".png",image.EncodeToPNG());Destroy(image);}
            finally{RenderTexture.active=previous;camera.targetTexture=null;rt.Release();Destroy(rt);Destroy(go);}
        }
        private IEnumerator Start()
        {
            Directory.CreateDirectory(Folder);yield return new WaitForSecondsRealtime(1);
            FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride=Path.GetFullPath(Folder+"/Saves");
            CinematicLibrary.PreferencePrefix="SpaceMiner.KeilCheck."+Guid.NewGuid()+".";
            FindFirstObjectByType<StartMenu>().StartDemo();FindFirstObjectByType<IntroSequence>().Skip();
            yield return null;yield return null;
            try
            {
                mode=StationInteriorMode.Current;layout=mode.Layout;var geometry=layout.Geometry;
                var scenario=FindFirstObjectByType<WaterScenario>();scenario.SetSimulationRate(0);scenario.Scanner.DismissDialogue();
                Require(layout!=null && StationInteriorLayout.Revision==15,"current interior migration revision");
                Require(geometry.DamagedStorageStages==2 && geometry.SideCouplings==8,"three joined wedge modules, two distinct wrecks");
                Require(geometry.DamagedReactorServices==3 && geometry.ReactorServiceCount==4,"one surviving and three broken reactor links");
                Require(geometry.HoneycombSurfaces>100,"double-layer station skins");
                Require(geometry.HullSupportFrames>50,"shield layers carried by perimeter frames and stand-off ribs");
                var hallRoof=geometry.Root.Find("Ring 1/Drohnenhalle Wabendecke");
                Require(Array.TrueForAll(hallRoof.GetComponentsInChildren<Transform>(),part=>part.name!="Waben / Drucklage"),"authored hall interior retained behind exterior shield");
                Require(scenario.GetComponent<CampaignDroneFleet>().ModelCount==10,"all exterior drones use approved imported mining model");
                var fleet=scenario.GetComponent<CampaignDroneFleet>();
                Require(fleet.Navigator.PoseClear(scenario.Worker.HomePosition,scenario.Worker.HomeRotation,out string berthIssue),"imported active hull fits real cradle / "+berthIssue);
                Require(geometry.ExpansionSlots>=18,"three capped interfaces on free ring segments");
                Require(layout.Airlock!=null && !layout.Airlock.PermanentlyOpen,"physical bay airlock");
                Require(geometry.PressureGates.Count>=17,"ring and module compartment bulkheads");
                Require(layout.RepairDrone.GetComponentInChildren<DroneAgent>()==null,"small repair drone is a passive instance of the same geometry");
                var repairStatus=layout.RepairDrone.Find("Sensor visor").GetComponent<Renderer>();var repairBlock=new MaterialPropertyBlock();repairStatus.GetPropertyBlock(repairBlock);
                Require(repairStatus.HasPropertyBlock() && repairBlock.GetColor("_EmissionColor").maxColorComponent==0,"inactive repair drone status is dark");
                for(int i=0;i<10;i++)
                {
                    Require(scenario.Drones[i].IsOperational==(i==0),"fleet operational policy "+i);
                    Require(scenario.Drones[i].transform.Find("Mining Drone Geometry")!=null,"same drone model "+i);
                    Require(scenario.Drones[i].GetComponent<TripoMiningDroneVisual>()!=null && !scenario.Drones[i].transform.Find("Mining Drone Geometry").gameObject.activeSelf,"old primitive model hidden behind approved hull "+i);
                    if(i>0)Require(scenario.Drones[i].Phase==DronePhase.Disabled,"inactive drone remains disabled "+i);
                    Vector3 home=Quaternion.Euler(0,-30,0)*layout.transform.InverseTransformPoint(scenario.Drones[i].HomePosition);
                    Require(Mathf.Abs(home.y-1)<.002f&&Mathf.Abs(home.x-(i%5-2)*6.6f)<.002f&&Mathf.Abs(home.z-(i<5?79:67.5f))<.002f,
                        "same-level homes match the enlarged bay rows / "+i);
                    Require(fleet.CradleRoot(i).IsChildOf(fleet.BerthRoot(i)),"actual imported cradle follows its authored berth anchor / "+i);
                }
                Require(Mathf.Abs(geometry.BuiltOuterRadius(1)-84)<.001f,"drone bay floor and envelope extend to radius84");
                foreach(float angle in new[]{90f,120f,210f,220f})
                {
                    float radius=angle==90||angle==120?33.5f:46;
                    var point=layout.transform.TransformPoint(RingWalkthrough.At(radius,angle,.04f));
                    Require(!layout.ContainsPosition(point),"unsafe floor excluded "+angle);
                    At(radius,angle);Require(Vector3.Distance(mode.FeetPosition,layout.SpawnPosition)<.1f,"teleport refuses damaged area "+angle);
                }
                At(36,200);var storage=geometry.NearestGate(mode.FeetPosition);Require(storage!=null,"active storage bulkhead reachable");
                storage.Request(false,mode.FeetPosition);storage.Advance(2,null);Walk(.8f);
                Require(new Vector2(LocalFeet.x,LocalFeet.z).magnitude<38.1f,"closed storage gate blocks actual player");
                storage.Request(true,mode.FeetPosition);storage.Advance(2,null);Walk(1.3f);
                Require(new Vector2(LocalFeet.x,LocalFeet.z).magnitude>42,"open storage gate admits actual player into wedge01");
                var airlock=layout.Airlock;
                At(50.8f,30);Walk(.35f);Require(new Vector2(LocalFeet.x,LocalFeet.z).magnitude<51.6f,"closed inner bay door blocks player");
                At(50.8f,30);Require(airlock.Request(mode.FeetPosition),"airlock request from pressurized hall");
                airlock.Advance(2,mode.FeetPosition);Walk(.6f);CycleToExit();Walk(.95f);
                Require(new Vector2(LocalFeet.x,LocalFeet.z).magnitude>60.3f,"actual player traverses enlarged bay airlock outward");
                for(int i=0;i<30;i++)airlock.Advance(.1f,mode.FeetPosition);
                Require(airlock.Phase==AirlockPhase.Idle,"outer door closes after exit");
                At(61.2f,30);mode.Look(new Vector2(180,0));Require(airlock.Request(mode.FeetPosition),"airlock request from vacuum bay");
                airlock.Advance(2,mode.FeetPosition);Walk(.6f);CycleToExit();Walk(.95f);
                Require(new Vector2(LocalFeet.x,LocalFeet.z).magnitude<51.3f,"actual player traverses airlock inward");
                for(int i=0;i<30;i++)airlock.Advance(.1f,mode.FeetPosition);
                At(33.5f,74);mode.Look(new Vector2(90,0));Walk(2);
                Require(layout.ContainsPosition(mode.FeetPosition),"left ring casualty closed bulkhead prevents entering vacuum");
                var old=SaveGameStore.Capture(scenario,"Legacy station","migration to keil layout");old.interiorLayout=7;
                old.interiorFeet=layout.transform.TransformPoint(RingWalkthrough.At(46,210,.04f));old.insideStation=true;
                old.drones[1].operational=true;old.drones[1].phase=DronePhase.Ready;old.drones[1].battery=2;
                float water=old.water;SaveGameStore.Apply(old,scenario);
                Require(Vector3.Distance(mode.FeetPosition,layout.SpawnPosition)<.1f,"legacy unsafe save relocates to console");
                Require(Mathf.Abs(scenario.WaterLiters-water)<.001f,"save migration preserves water");
                Require(!scenario.Drones[1].IsOperational && scenario.Drones[1].Phase==DronePhase.Disabled && scenario.Drones[1].BatteryKwh==2,"legacy second worker disabled without deleting stored energy");
                mode.OpenConsole();scenario.Scanner.Advance(10);Require(scenario.Scanner.Scan(),"initial console scan reveals resource sources");
                scenario.Scanner.DismissDialogue();mode.CloseConsole();
                mode.Exit(true);var source=Array.Find(scenario.Asteroids,item=>item.CanMineWater);
                Require(source!=null && scenario.AssignTankOrder(source),"water mining order starts");scenario.Scanner.DismissDialogue();
                bool aligned=false,collar=false;
                for(int i=0;i<600 && scenario.Deliveries==0;i++)
                {
                    scenario.Advance(30);
                    if(scenario.Worker.Phase==DronePhase.Launching || scenario.Worker.Phase==DronePhase.TankDocking || scenario.Worker.Phase==DronePhase.TankUndocking)
                        Require(fleet.Navigator.PoseClear(scenario.Worker.transform.position,scenario.Worker.transform.rotation,out string issue),"actual imported hull maneuver clears station / "+scenario.Worker.Phase+" / "+issue);
                    if(scenario.Worker.Phase==DronePhase.Unloading)aligned|=Vector3.Distance(scenario.Worker.CargoOutletPoint,scenario.Worker.TankInlet)<.002f;
                    if(scenario.Worker.Phase==DronePhase.Unloading && scenario.Worker.PhaseElapsed>20 && scenario.Worker.PhaseElapsed<220)
                        collar|=scenario.Worker.GetComponent<TripoMiningDroneVisual>().CollarSealed && scenario.Worker.GetComponent<TripoMiningDroneVisual>().CollarContactGap<.01f;
                }
                Require(scenario.Deliveries>0 && scenario.WaterLiters>water,"real mining flight unloads at new front socket");
                Require(aligned,"cargo outlet seals at relocated tank");
                Require(collar,"imported cargo gate uses the real suction collar");
                foreach(var agent in scenario.Drones)if(agent!=scenario.Worker)Require(agent.Phase==DronePhase.Disabled,"other drones remain inactive through real trip");
                Physics.SyncTransforms();
                Render("01-gesamtstation",new Vector3(115,150,130),new Vector3(0,1,0),48);
                Render("02-grundriss",new Vector3(0,165,.1f),Vector3.zero,55);
                Render("03-wasserkeile",RingWalkthrough.At(46,210)+new Vector3(-22,27,-32),RingWalkthrough.At(46,210,2),55);
                Render("04-ringhavarie",RingWalkthrough.At(43,105,22),RingWalkthrough.At(33.5f,105,2),65);
                Render("05-bay-schleuse",RingWalkthrough.At(61,30,7)+new Vector3(8,0,0),RingWalkthrough.At(54,30,1.8f),65);
                Render("06-flotte",RingWalkthrough.At(101,30,15),RingWalkthrough.At(72,30,1.5f),65);
                At(47,30);Render("07-halle-zur-schleuse",LocalFeet+Vector3.up*1.6f,RingWalkthrough.At(55,30,1.7f),70);
                Render("08-integrierte-andockrahmen",RingWalkthrough.At(54,330,7),RingWalkthrough.At(38.5f,330,2.6f),55);
                var roof=Array.Find(geometry.Root.GetComponentsInChildren<MeshFilter>(),filter=>filter.name=="Ringdecke");
                var skin=Array.Find(roof.GetComponentsInChildren<Renderer>(),renderer=>renderer.name=="Waben / Aussenschutz");
                skin.enabled=false;
                try {Render("09-huellrahmen-schnitt",new Vector3(12,15,39),new Vector3(0,5.7f,34),60);} finally {skin.enabled=true;}
                File.WriteAllText(Folder+"/result.json","{\"passed\":true,\"checks\":"+checks+",\"layout\":"+StationInteriorLayout.Revision+"}");
                Debug.Log("KEIL COMPLETE: "+checks+" checks");Application.Quit(0);
            }
            catch(Exception error){Debug.LogException(error);File.WriteAllText(Folder+"/error.txt",error.ToString());Application.Quit(1);}
        }
    }
}
#endif
