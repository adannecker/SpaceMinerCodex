using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceMiner
{
    // Opt-in development player check. A second process loads the first one's files.
    public sealed class SaveSessionCheck : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private int checks;
        private string folder;
        private WaterScenario scenario;
        private SaveGameMenu saves;
        private StationInteriorMode room;
        private StartMenu start;
        private QuitMenu quit;
        [Serializable] private sealed class Manifest { public string[] files; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"-saveSessionCheck")>=0 || Array.IndexOf(args,"-saveSessionUi")>=0)SceneManager.sceneLoaded+=Loaded;
        }
        static void Loaded(Scene scene,LoadSceneMode mode)
        {
            SceneManager.sceneLoaded-=Loaded;
            new GameObject("Save and exit session check").AddComponent<SaveSessionCheck>();
        }
        private void Require(bool condition,string text) { checks++;if(!condition)throw new Exception("Save session: "+text); }
        private bool Guard(Action action)
        {
            try { action();return true; }catch(Exception error) { Debug.LogException(error);Application.Quit(1);return false; }
        }
        private void Verify(SavedGame state,string label)
        {
            Require(StationInteriorMode.IsInside==state.insideStation,label+" mode");
            if(state.insideStation)
            {
                Require(Vector3.Distance(room.FeetPosition,state.interiorFeet)<.001f,label+" exact feet");
                Require(Mathf.Abs(Mathf.DeltaAngle(room.ViewYaw,state.interiorYaw))<.001f && Mathf.Abs(room.ViewPitch-state.interiorPitch)<.001f,label+" view direction");
                Require(Quaternion.Angle(Camera.main.transform.rotation,Quaternion.Euler(state.interiorPitch,state.interiorYaw,0))<.01f,label+" camera applied immediately");
                Require(room.ConsoleOpen==state.console,label+" desk");
                var actual=room.Airlock.CaptureState();
                Require(actual.phase==state.airlock.phase && actual.towardsRing==state.airlock.towardsRing &&
                    Mathf.Abs(actual.habitatOpen-state.airlock.habitatOpen)<.00001f && Mathf.Abs(actual.ringOpen-state.airlock.ringOpen)<.00001f &&
                    Mathf.Abs(actual.equalizing-state.airlock.equalizing)<.00001f,label+" door phase/progress/pressure timer");
            }
            Require(scenario.Worker.HasTankOrder==state.drones[0].tankOrder && scenario.Worker.Phase==state.drones[0].phase &&
                Vector3.Distance(scenario.Worker.transform.position,state.drones[0].position)<.001f,label+" working drone");
        }
        private string ManualSave(string label)
        {
            var before=new HashSet<string>(Directory.GetFiles(folder,"save-*.json"));
            saves.OpenSave();Require(saves.ConfirmSave(),label+" confirm manual save");
            foreach(var path in Directory.GetFiles(folder,"save-*.json"))if(!before.Contains(path))return path;
            throw new Exception("Manual save produced no file");
        }
        private void Place(int place)
        {
            room.RestoreInterior(room.Room.TransformPoint(new Vector3(-1.4f,.025f,.6f)),false,room.Room.eulerAngles.y+53,-16);
            room.Airlock.ResetClosed();
            if(place==1)room.RestoreInterior(room.Layout.Console.position-room.Room.forward*1.5f,true,room.Room.eulerAngles.y,0);
            if(place==2 || place==3 || place==4)
            {
                float z=place==2?-7.7f:place==3?StationAirlock.HabitatDoorZ:StationAirlock.RingDoorZ;
                room.RestoreInterior(room.Room.TransformPoint(new Vector3(0,.025f,z)),false,room.Room.eulerAngles.y+180,6);
                room.Airlock.RestoreState(new StationAirlock.SavedState {
                    phase=place==2?AirlockPhase.Equalizing:place==3?AirlockPhase.OpeningEntry:AirlockPhase.OpeningExit,
                    towardsRing=true,equalizing=place==2?.55f:0,habitatOpen=place==3?.6f:0,ringOpen=place==4?.6f:0 });
            }
            if(place==5)room.RestoreInterior(room.Layout.Ring.TransformPoint(Quaternion.Euler(0,170,0)*new Vector3(0,.025f,12)),false,110,-12);
            if(place==6)scenario.Scanner.OpenVirtualView();
            scenario.Scanner.DismissDialogue();
        }
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1);
            folder=Path.GetFullPath("Logs/SaveSession");Directory.CreateDirectory(folder);
            scenario=FindFirstObjectByType<WaterScenario>();saves=FindFirstObjectByType<SaveGameMenu>();
            room=StationInteriorMode.Current;start=FindFirstObjectByType<StartMenu>();quit=FindFirstObjectByType<QuitMenu>();
            saves.StorageDirectoryOverride=folder;
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-saveSessionUi")>=0)
            {
                saves.StorageDirectoryOverride=Path.Combine(folder,"UI");
                Screen.SetResolution(1280,800,FullScreenMode.Windowed);
                Debug.Log("SAVE SESSION UI: isolated saves at "+saves.StorageDirectoryOverride);
                yield break;
            }
            bool restart=Array.IndexOf(Environment.GetCommandLineArgs(),"-restoreSessionCheck")>=0;
            if(restart)
            {
                if(!Guard(()=>
                {
                    Require(StartMenu.IsOpen && !saves.HasSession,"fresh process has no active game");
                    var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(folder,"manifest.json")));
                    Require(manifest.files.Length==14,"restart manifest covers seven manual and seven autosaves");
                    foreach(var path in manifest.files)
                    {
                        var state=SaveGameStore.Read(path);saves.OpenLoad();Require(saves.LoadGame(path),"load after process restart "+Path.GetFileName(path));
                        Verify(state,"restart "+Path.GetFileName(path));scenario.Scanner.DismissDialogue();
                    }
                }))yield break;
            }
            else
            {
                if(!Guard(()=>
                {
                    Require(start.gameObject!=saves.gameObject && quit.gameObject!=saves.gameObject,"reproduce separate menu/storage objects");
                    start.StartDemo();Require(!StartMenu.IsOpen && saves.HasSession,"actual Demo button starts save session");
                    FindFirstObjectByType<IntroSequence>().Skip();scenario.SetSimulationRate(0);
                }))yield break;
                yield return null;
                var manifest=new List<string>();
                if(!Guard(()=>
                {
                    room.RestoreInterior(room.Layout.Console.position-room.Room.forward*1.5f,false);room.OpenConsole();
                    scenario.Scanner.Advance(10);Require(scenario.Scanner.Scan(),"scan before water order");scenario.Scanner.DismissDialogue();
                    Require(scenario.AssignTankOrder(scenario.Asteroids[0]),"assign real water order");scenario.Advance(200);
                    Require(scenario.Worker.HasTankOrder,"water order still active");
                    for(int place=0;place<7;place++)
                    {
                        Place(place);
                        var expected=SaveGameStore.Capture(scenario,"Position "+place,"");
                        string path=ManualSave("place "+place);manifest.Add(path);
                        Verify(SaveGameStore.Read(path),"manual disk "+place);
                        saves.AdvanceAutosave(60);
                        string auto=Path.Combine(folder,"autosave.json"),copy=Path.Combine(folder,"position-"+place+"-auto.json");
                        File.Copy(auto,copy,true);manifest.Add(copy);Verify(SaveGameStore.Read(copy),"autosave disk "+place);
                        room.Exit();Require(saves.LoadGame(path),"load through real save menu "+place);Verify(expected,"manual load "+place);
                        scenario.Scanner.DismissDialogue();
                        // Same drone timestep after a load must match uninterrupted simulation.
                        scenario.Advance(.25f);var continued=scenario.Worker.CaptureState();SaveGameStore.Apply(expected,scenario);scenario.Advance(.25f);
                        Require(scenario.Worker.Phase==continued.phase && Vector3.Distance(scenario.Worker.transform.position,continued.position)<.001f &&
                            Mathf.Abs(scenario.Worker.BatteryKwh-continued.battery)<.00001f,"continuation after load "+place);
                    }
                    File.WriteAllText(Path.Combine(folder,"manifest.json"),JsonUtility.ToJson(new Manifest { files=manifest.ToArray() },true));
                }))yield break;
                yield return null;
                foreach(int place in new[]{0,1,5,6})
                {
                    if(!Guard(()=>Place(place)))yield break;
                    yield return null;
                    if(place==1) { quit.HandleEscape();Require(!room.ConsoleOpen && !QuitMenu.IsOpen,"first Escape closes desk only");yield return null; }
                    if(!Guard(()=>
                    {
                        quit.Open();Require(QuitMenu.IsOpen,"open quit dialog "+place);
                        quit.RequestReturnToStart();Require(SaveGameMenu.IsOpen && !StartMenu.IsOpen,"actual Quit button asks to save "+place);
                        var expected=SaveGameStore.Capture(scenario,"Exit","");Verify(SaveGameStore.Read(Path.Combine(folder,"autosave.json")),"exit autosave "+place);
                        saves.CancelDialog();Require(!StartMenu.IsOpen && saves.HasSession,"cancel exit retains session "+place);
                        Verify(expected,"cancelled exit "+place);
                    }))yield break;
                    yield return null;
                }
                if(!Guard(()=>
                {
                    Place(5);quit.RequestReturnToStart();Require(saves.ConfirmSave(),"save and leave");
                    Require(StartMenu.IsOpen && !saves.HasSession && !StationInteriorMode.IsInside,"return to start ends session");
                    string auto=Path.Combine(folder,"autosave.json"),before=File.ReadAllText(auto);
                    start.StartDemo();Require(saves.HasSession,"second Demo activates session");
                    Require(File.ReadAllText(auto)==before,"new Demo does not overwrite saved ring position with menu pose");
                    FindFirstObjectByType<IntroSequence>().Skip();
                }))yield break;
                yield return null;
                if(!Guard(()=>
                {
                    Place(0);quit.RequestReturnToStart();saves.LeaveWithoutManualSave();Require(StartMenu.IsOpen,"leave using existing autosave");
                    saves.OpenLoad();Require(saves.LoadGame(manifest[10]) && saves.HasSession && !StartMenu.IsOpen,"load from start menu reactivates session");
                    var invalid=SaveGameStore.Capture(scenario,"Invalid","");invalid.airlock.habitatOpen=invalid.airlock.ringOpen=1;
                    Vector3 before=room.FeetPosition;bool rejected=false;
                    try { SaveGameStore.Apply(invalid,scenario); }catch(IOException) { rejected=true; }
                    Require(rejected && room.FeetPosition==before,"invalid interlock rejected before world mutation");
                    Place(2);var legacy=SaveGameStore.Capture(scenario,"Legacy","");legacy.airlock=null;legacy.hasInteriorView=false;
                    room.Exit();SaveGameStore.Apply(legacy,scenario);
                    Require(Vector3.Distance(room.FeetPosition,legacy.interiorFeet)<.001f,"old v1 keeps chamber position");
                    for(int i=0;i<30;i++)room.Airlock.Advance(.1f,room.FeetPosition);
                    Require(room.Airlock.RingOpen==1 && room.Airlock.HabitatOpen==0,"old chamber save cannot trap occupant");
                    string blocked=Path.Combine(folder,"blocked-storage");File.WriteAllText(blocked,"Intentional test fixture");
                    saves.StorageDirectoryOverride=blocked;quit.RequestReturnToStart();Require(!saves.ConfirmSave() && !StartMenu.IsOpen,"failed save cannot silently leave game");
                    saves.CancelDialog();saves.StorageDirectoryOverride=folder;
                }))yield break;
                yield return null;
            }
            if(!Guard(()=>
            {
                File.WriteAllText(Path.Combine(folder,restart?"restart-result.json":"result.json"),"{\"passed\":true,\"checks\":"+checks+"}");
                Debug.Log("SAVE SESSION CHECK PASSED: "+checks+" checks; "+(restart?"new-process restore":"real menu handlers, exit, manual/autosave, positions, orientation, airlock and running drone"));
            }))yield break;
            Application.Quit(0);
        }
#endif
    }
}
