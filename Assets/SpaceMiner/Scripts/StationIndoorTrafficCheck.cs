#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    public sealed class StationIndoorTrafficCheck : MonoBehaviour
    {
        private const string Folder="Logs/StationIndoorTraffic";
        private StationIndoorTraffic traffic;
        private StationOperations ops;
        private WaterScenario scenario;
        private StationInteriorMode interior;
        private int checks, samples, guidedSamples;
        private int closeEnvelopeChecks;
        private bool failed;
        private float minSeparation=float.PositiveInfinity;
        private readonly List<string> pictures=new List<string>();
        [Serializable] private sealed class Trip { public string id,route; public float meters; public int stops,roomVisits,overtakes; public Vector3 position; }
        [Serializable] private sealed class Report
        {
            public bool passed; public int checks,samples,navigationSources,guidedSamples,overtakes,crossings,closeEnvelopeChecks; public float minimumSeparation,simulatedSeconds;
            public Trip[] drones; public string[] pictures;
            public string scope="Ten opt-in authored I75 R03 actors, 0.783m including coupling; shared R-01 visual, real collider-built navigation, doors, floor support, room visits, movement, pause and physical battery placement. Four native game-camera framebuffer captures plus three separate close-up camera renders of an actual scene actor. Rotated full-size body envelopes checked with5mm clearance per body. No productive worker grants, manual comfort, OS-input, headset or frame-rate acceptance.";
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-stationIndoorTrafficCheck")>=0)
                new GameObject("Indoor traffic acceptance").AddComponent<StationIndoorTrafficCheck>();
        }
        private void Require(bool condition,string label)
        { if(!condition)throw new Exception("INDOOR TRAFFIC: "+label);checks++;Debug.Log("INDOOR TRAFFIC OK: "+label); }
        private bool Guard(Action action)
        {
            try { action();return true; }
            catch(Exception error){failed=true;Time.timeScale=1;string detail=error+"\n"+(traffic==null?"":traffic.Diagnostic());File.WriteAllText(Folder+"/error.txt",detail);Debug.LogError(detail);Debug.LogException(error);Application.Quit(1);return false;}
        }
        private Vector3 World(Vector3 point)=>ops.Layout.transform.TransformPoint(point);
        private IEnumerator Capture(string name)
        {
            yield return null;yield return new WaitForEndOfFrame();
            string path=Path.GetFullPath(Folder+"/"+name+".png");ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSecondsRealtime(.35f);
            Guard(()=>{
                Require(File.Exists(path),"native framebuffer exists / "+name);
                var image=new Texture2D(2,2);
                try {
                    Require(image.LoadImage(File.ReadAllBytes(path))&&image.width==Screen.width&&image.height==Screen.height,"native framebuffer dimensions / "+name);
                    int lit=0;foreach(var pixel in image.GetPixels32())if(pixel.r+pixel.g+pixel.b>100)lit++;
                    Require(lit>1000,"nonempty actual station view / "+name);pictures.Add(path);
                } finally {Destroy(image);}
            });
        }
        private void Sample()
        {
            if(!string.IsNullOrEmpty(traffic.Failure))throw new Exception(traffic.Failure);
            foreach(var drone in traffic.Vehicles)
            {
                var p=drone.Agent.transform.position;var local=ops.Layout.transform.InverseTransformPoint(p);
                if(!drone.Agent.isOnNavMesh || Mathf.Abs(local.y)>.25f || !ops.Layout.ContainsPosition(p))
                    throw new Exception("Unsupported route / "+drone.Id+" / "+local);
                if(!Physics.Raycast(p+Vector3.up*.35f,Vector3.down,.8f,1<<25,QueryTriggerInteraction.Ignore))
                    throw new Exception("Missing physical floor / "+drone.Id+" / "+local);
                float angle=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;
                if(drone.Guided&&!StationRingLanes.Junction(local))
                {
                    float r=StationRingLanes.Radius(local);
                    if((drone.Direction>0&&(r<30.7f||r>34.0f))||(drone.Direction<0&&(r<33.7f||r>37.0f)))
                        throw new Exception("Wrong directional lane pair / "+drone.Id+" / "+local+" / direction="+drone.Direction);
                    guidedSamples++;
                }
                if(Mathf.Abs(Mathf.DeltaAngle(angle,90))<15)throw new Exception("Traffic entered damaged sector / "+drone.Id);
                var overlaps=Physics.OverlapCapsule(p+Vector3.up*.26f,p+Vector3.up*.4f,.20f,1<<25,QueryTriggerInteraction.Ignore);
                if(overlaps.Length>0)throw new Exception("Traffic crosses solid geometry / "+drone.Id+" / "+local+" / "+overlaps[0].name);
                foreach(var other in traffic.Vehicles)
                {
                    if(other==drone)continue;
                    float separation=Vector3.Distance(p,other.Agent.transform.position);
                    minSeparation=Mathf.Min(minSeparation,separation);
                    // Circular broad phase, then separating-axis test of the actual
                    // rotated 0.482 x 0.783m envelope, with 5mm clearance per body.
                    if(separation<.94f)
                    {
                        closeEnvelopeChecks++;
                        var a=drone.Agent.transform;var b=other.Agent.transform;var delta=b.position-a.position;
                        bool separate=false;
                        foreach(var axis in new[]{a.right,a.forward,b.right,b.forward})
                        {
                            float ra=(IndoorDroneVisual.Width*.5f+.005f)*Mathf.Abs(Vector3.Dot(a.right,axis))
                                +(IndoorDroneVisual.OverallLength*.5f+.005f)*Mathf.Abs(Vector3.Dot(a.forward,axis));
                            float rb=(IndoorDroneVisual.Width*.5f+.005f)*Mathf.Abs(Vector3.Dot(b.right,axis))
                                +(IndoorDroneVisual.OverallLength*.5f+.005f)*Mathf.Abs(Vector3.Dot(b.forward,axis));
                            if(Mathf.Abs(Vector3.Dot(delta,axis))>=ra+rb){separate=true;break;}
                        }
                        if(!separate)throw new Exception("R03 oriented envelopes intersect / "+drone.Id+" / "+other.Id+" / "+separation);
                    }
                }
            }
            samples++;
        }
        private bool Completed()
        {
            int passes=0;
            foreach(var drone in traffic.Vehicles)
            {
                passes+=drone.Overtakes;
                if(drone.Distance<15 || drone.StopsReached<drone.Stops.Length*3+2 || (drone.Route!="Ringgang"&&drone.RoomVisits<12))return false;
            }
            return passes>0;
        }
        private void CaptureModel(string name, Vector3 offset)
        {
            // Actual in-game actor, materials and station lighting; only the camera
            // changes. These close-ups are separate from native framebuffer captures.
            var actor=traffic.Vehicles[0].Agent.transform;
            var camera=new GameObject("R03 inspection camera").AddComponent<Camera>();
            camera.CopyFrom(Camera.main);camera.enabled=false;
            camera.transform.position=actor.TransformPoint(offset);
            camera.transform.LookAt(actor.TransformPoint(Vector3.up*.23f));
            camera.fieldOfView=43;camera.nearClipPlane=.025f;
            var rt=new RenderTexture(1600,1000,24);var previous=RenderTexture.active;
            camera.targetTexture=rt;ops.RefreshViewLighting(camera,true);
            try
            {
                camera.Render();RenderTexture.active=rt;
                var pixels=new Texture2D(1600,1000,TextureFormat.RGB24,false);
                pixels.ReadPixels(new Rect(0,0,1600,1000),0,0);pixels.Apply();
                string path=Path.GetFullPath(Folder+"/"+name+".png");
                File.WriteAllBytes(path,pixels.EncodeToPNG());Destroy(pixels);pictures.Add(path);
                Require(File.Exists(path),"actual R03 scene close-up / "+name);
            }
            finally{RenderTexture.active=previous;camera.targetTexture=null;rt.Release();Destroy(rt);Destroy(camera.gameObject);ops.RefreshViewLighting(Camera.main,true);}
        }
        private void CheckLaneGeometry()
        {
            int boundaries=0,rails=0,beds=0,arrows=0;
            foreach(var mesh in ops.Layout.Geometry.Root.GetComponentsInChildren<MeshFilter>())
            {
                string name=mesh.name;
                if(name.StartsWith("Bodenmarkierung / durchgehend /"))boundaries++;
                else if(name.StartsWith("Magnetleitung / Spur"))rails++;
                else if(name.StartsWith("Magnetfahrbahn / Spur"))beds++;
                else if(name=="Magnetfahrbahn / Richtungspfeile")arrows++;
                else continue;
                Require(mesh.GetComponent<Collider>()==null,"flush magnetic road creates no raised obstacle / "+name);
            }
            Require(boundaries==70&&rails==80&&beds==40&&arrows==10,"four separated lanes, eight magnetic conductors and arrows in every healthy sector");
            Require(StationRingLanes.Direction(0)==StationRingLanes.Direction(1)&&StationRingLanes.Direction(2)==StationRingLanes.Direction(3)
                &&StationRingLanes.Direction(0)==-StationRingLanes.Direction(2),"two lanes per opposing direction");
            Require(Mathf.Abs(StationRingLanes.MagnetGauge-.35f)<.001f,"magnetic conductor gauge matches measured I75 foot-contact centres");
        }
        private void ViewDrone(int index)
        {
            var drone=traffic.Vehicles[index];var target=drone.Agent.transform.position;
            for(int angle=0;angle<360;angle+=30)
            {
                var candidate=target+Quaternion.Euler(0,angle,0)*new Vector3(0,0,-2.2f);
                var local=ops.Layout.transform.InverseTransformPoint(candidate);local.y=.04f;candidate=World(local);
                if(!ops.Layout.ContainsPosition(candidate)||!ops.Layout.HasStandingClearance(candidate))continue;
                var direction=target-candidate;
                interior.RestoreInterior(candidate,false,Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg,31);
                return;
            }
            throw new Exception("No supported camera position next to "+drone.Id);
        }
        private IEnumerator Start()
        {
            Directory.CreateDirectory(Folder);File.Delete(Folder+"/error.txt");File.Delete(Folder+"/result.json");
            yield return new WaitForSecondsRealtime(1);
            if(!Guard(()=>{
                FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride=Path.GetFullPath(Folder+"/Saves");
                CinematicLibrary.PreferencePrefix="SpaceMiner.Test.IndoorTraffic."+Guid.NewGuid().ToString("N");
                Require(SettingsStore.PathName.StartsWith(Path.GetFullPath(Folder),StringComparison.OrdinalIgnoreCase),"isolated settings");
                FindFirstObjectByType<StartMenu>().StartDemo();FindFirstObjectByType<IntroSequence>().Skip();
            }))yield break;
            for(int frame=0;frame<180;frame++)
            {
                traffic=FindFirstObjectByType<StationIndoorTraffic>();
                if(traffic!=null&&(traffic.Ready||!string.IsNullOrEmpty(traffic.Failure)))break;
                yield return null;
            }
            if(!Guard(()=>{
                Require(traffic!=null&&traffic.Ready,"ten drones created / "+traffic?.Failure);
                ops=StationOperations.Current;scenario=ops.Scenario;interior=StationInteriorMode.Current;
                scenario.SetSimulationRate(0);scenario.Scanner.DismissDialogue();ops.ClosePanel();
                foreach(var room in ops.RoomPlans)Require(room.SavePath.StartsWith(Path.GetFullPath(Folder),StringComparison.OrdinalIgnoreCase),"isolated room plan / "+room.RoomId);
                Require(traffic.Vehicles.Count==10&&traffic.NavigationSourceCount>100,"ten actual agents on collider navigation");
                CheckLaneGeometry();
                foreach(var drone in traffic.Vehicles)
                {
                    var bounds=new Bounds();bool first=true;
                    foreach(var mesh in drone.Agent.GetComponentsInChildren<MeshFilter>())
                    {
                        var box=mesh.sharedMesh.bounds;
                        for(int corner=0;corner<8;corner++)
                        {
                            var sign=new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1);
                            var point=drone.Agent.transform.InverseTransformPoint(mesh.transform.TransformPoint(box.center+Vector3.Scale(box.extents,sign)));
                            if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);
                        }
                    }
                    var visual=drone.Agent.GetComponentInChildren<IndoorDroneVisual>();
                    Require(visual!=null&&visual.highBody!=null,"authored R03 replaces temporary visual / "+drone.Id);
                    Require(!first&&Mathf.Abs(bounds.size.z-IndoorDroneVisual.OverallLength)<.004f
                        &&Mathf.Abs(bounds.size.x-IndoorDroneVisual.Width)<.004f,"R03 real width and length including coupling / "+drone.Id);
                    Require(bounds.min.y>=-.002f&&bounds.max.y<.45f,"R03 floor pivot and height / "+drone.Id);
                    Require(new Vector2(bounds.extents.x,bounds.extents.z).magnitude<drone.Agent.radius,"full R03 envelope inside navigation clearance / "+drone.Id);
                    var lod=visual.GetComponent<LODGroup>();
                    Require(lod!=null&&lod.GetLODs().Length==3&&visual.indicators.Length==3,"three detail levels and real status lights / "+drone.Id);
                }
                Require(ops.Layout.RepairDrone.GetComponentInChildren<IndoorDroneVisual>()!=null,"productive R-01 uses same authored R03 visual");
                CaptureModel("05-r03-front-in-game",new Vector3(.48f,.65f,1.05f));
                CaptureModel("06-r03-side-in-game",new Vector3(1.15f,.53f,.05f));
                CaptureModel("07-r03-rear-in-game",new Vector3(-.48f,.65f,-1.05f));
                Require(ops.BatteryBays[0].Installed&&ops.BatteryBays[0].Coupled&&ops.BatteryBays[1].Installed&&!ops.BatteryBays[1].Coupled,"physical pair across command entrance");
                Require(!ops.BatteryBays[4].Installed&&!ops.BatteryBays[5].Installed&&ops.State.power.capacityKwh==24,"far pair empty and original 24kWh retained");
                interior.RestoreInterior(World(new Vector3(2,.04f,40.5f)),false,ops.Layout.transform.eulerAngles.y+180,3);
                Camera.main.GetComponent<OrbitCamera>().ShowHud=false;
            }))yield break;
            yield return null;yield return null;
            var paused=new List<Vector3>();foreach(var drone in traffic.Vehicles)paused.Add(drone.Agent.transform.position);
            yield return new WaitForSecondsRealtime(.3f);
            if(!Guard(()=>{for(int i=0;i<paused.Count;i++)Require(Vector3.Distance(paused[i],traffic.Vehicles[i].Agent.transform.position)<.01f,"simulation pause stops visual drone / "+i);}))yield break;
            if(!Guard(()=>{
                checks+=StationWorkshopCheck.CheckRoadOnly(ops.Layout,interior);
                interior.RestoreInterior(World(new Vector3(2,.04f,40.5f)),false,ops.Layout.transform.eulerAngles.y+180,3);
            }))yield break;
            Screen.SetResolution(1440,900,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.3f);
            Screen.SetResolution(1600,1000,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.6f);
            yield return Capture("01-batterien-gegenueber-zentrale");if(failed)yield break;
            if(!Guard(()=>{
                traffic.PreparePassingConflict();
                scenario.SetSimulationRate(1);Time.timeScale=6;
                // Close a healthy passage; traffic must request and wait for its real leaves.
                foreach(var gate in ops.Layout.Geometry.PressureGates)
                    if(!gate.Locked&&Vector3.Distance(gate.transform.position,World(RingWalkthrough.At(38,30)))<4){gate.Request(false,World(new Vector3(0,0,45)));gate.Advance(3,null);}
            }))yield break;
            float began=Time.time,deadline=Time.realtimeSinceStartup+600,progressAt=Time.realtimeSinceStartup+30;
            bool passingConflictChecked=false;
            float livenessAt=Time.time+120;
            var previousDistances=new float[10];
            for(int i=0;i<10;i++)previousDistances[i]=traffic.Vehicles[i].Distance;
            while(Time.realtimeSinceStartup<deadline&&!Completed())
            {
                yield return null;
                if(!Guard(Sample))yield break;
                if(!passingConflictChecked&&Time.time-began>=60)
                {
                    if(!Guard(()=>Require(traffic.Vehicles[7].Crossings>0&&traffic.Vehicles[8].Crossings>0,
                        "ticket holder and later overtaker both clear seeded turnaround conflict")))yield break;
                    passingConflictChecked=true;
                }
                if(Time.time>=livenessAt)
                {
                    if(!Guard(()=>{
                        for(int i=0;i<10;i++)
                        {
                            var v=traffic.Vehicles[i];
                            Require(v.Distance-previousDistances[i]>2,"individual progress in 120 simulation seconds / "+v.Id+" / junction="+v.Junction);
                            previousDistances[i]=v.Distance;
                        }
                    }))yield break;
                    livenessAt=Time.time+120;
                }
                if(Time.realtimeSinceStartup>=progressAt)
                {
                    string progress="";foreach(var drone in traffic.Vehicles)progress+=drone.Id+":"+drone.Distance.ToString("0")+"m/"+drone.StopsReached+" "+drone.Status+" J"+drone.Junction+(drone.CrossingGranted?"*":"")+" "+ops.Layout.transform.InverseTransformPoint(drone.Agent.transform.position)+"; ";
                    Debug.Log("INDOOR PROGRESS "+progress);progressAt=Time.realtimeSinceStartup+30;
                }
            }
            if(!Guard(()=>{
                foreach(var drone in traffic.Vehicles)Debug.Log("INDOOR TRIP "+drone.Id+" / "+drone.Route+" / "+drone.Distance+"m / stops="+drone.StopsReached+" / visits="+drone.RoomVisits+" / overtakes="+drone.Overtakes);
                Require(Completed(),"all ten complete three full route circuits, including return trips");
                Require(traffic.CompletedCrossings>100,"more than one hundred reserved crossings finish");
                foreach(var gate in ops.Layout.Geometry.PressureGates)if(gate.Locked)Require(!gate.TargetOpen&&gate.Opening<.01f,"damaged/locked gate stays closed / "+gate.Label);
                var hallGate=ops.Layout.Geometry.PressureGates.Find(g=>g.Label=="Hallenschott");
                Require(hallGate!=null&&hallGate.TargetOpen&&hallGate.Opening>.98f,"traffic reopens the physically closed healthy hall bulkhead");
                Require(ops.State.orders.Count==0,"visual trial creates no campaign work orders");
                scenario.SetSimulationRate(0);Time.timeScale=1;
                ViewDrone(0);
            }))yield break;
            yield return Capture("02-i75-aussenwerkstatt-route");if(failed)yield break;
            if(!Guard(()=>ViewDrone(3)))yield break;
            yield return Capture("03-i75-interne-werkstatt-route");if(failed)yield break;
            if(!Guard(()=>interior.RestoreInterior(World(RingWalkthrough.At(30.2f,0,.04f)),false,ops.Layout.transform.eulerAngles.y+55,25)))yield break;
            yield return Capture("04-vier-magnetspuren-im-ring");if(failed)yield break;
            if(!Guard(()=>{
                var drones=new List<Trip>();int passes=0;foreach(var drone in traffic.Vehicles){passes+=drone.Overtakes;drones.Add(new Trip{id=drone.Id,route=drone.Route,meters=drone.Distance,stops=drone.StopsReached,roomVisits=drone.RoomVisits,overtakes=drone.Overtakes,position=ops.Layout.transform.InverseTransformPoint(drone.Agent.transform.position)});}
                File.WriteAllText(Folder+"/result.json",JsonUtility.ToJson(new Report{passed=true,checks=checks,samples=samples,guidedSamples=guidedSamples,closeEnvelopeChecks=closeEnvelopeChecks,overtakes=passes,crossings=traffic.CompletedCrossings,simulatedSeconds=Time.time-began,navigationSources=traffic.NavigationSourceCount,minimumSeparation=minSeparation,drones=drones.ToArray(),pictures=pictures.ToArray()},true));
                Debug.Log("STATION INDOOR TRAFFIC CHECK PASSED / "+checks);Application.Quit(0);
            }))yield break;
        }
    }
}
#endif
