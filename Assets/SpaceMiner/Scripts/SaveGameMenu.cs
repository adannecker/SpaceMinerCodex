using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    [DefaultExecutionOrder(-950)]
    public sealed class SaveGameMenu : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        private static int closedFrame=-1;
        public static bool BlocksInput => IsOpen || closedFrame==Time.frameCount;
        internal string StorageDirectoryOverride;
        public bool HasSession { get; private set; }
        private readonly SpaceMinerUi ui=new SpaceMinerUi();
        private WaterScenario scenario;
        private string saveName="Meine Station", comment="", status="";
        private bool loading, allowQuit;
        private Action exitAction;
        private Vector2 scroll;
        private float autosaveSeconds;
        private readonly List<string> saves=new List<string>();
        private readonly List<SavedGame> entries=new List<SavedGame>();
        private readonly List<string> warnings=new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void ResetStatic(){IsOpen=false;closedFrame=-1;}
        private void Start(){scenario=FindFirstObjectByType<WaterScenario>();Application.wantsToQuit+=WantsToQuit;}
        public bool NewSession(){if(HasSession && !Save(true))return false;HasSession=true;autosaveSeconds=0;saveName="Meine Station";comment="";return true;}
        public void OpenSave(Action after=null){if(scenario==null)return;loading=false;exitAction=after;status="";IsOpen=true;}
        public void OpenLoad(){loading=true;exitAction=null;status="";scroll=Vector2.zero;Refresh();IsOpen=true;}
        private void Refresh()
        {
            saves.Clear();entries.Clear();warnings.Clear();
            string directory=StorageDirectoryOverride??SaveGameStore.DirectoryPath;
            if(!Directory.Exists(directory))return;
            var available=new List<string>(Directory.GetFiles(directory,"*.json"));
            available.AddRange(Directory.GetFiles(directory,"*.json.bak"));
            var paths=available.ToArray();
            Array.Sort(paths,(a,b)=>File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));
            foreach(var path in paths) try { var state=SaveGameStore.Read(path);saves.Add(path);entries.Add(state); }
                catch(Exception){warnings.Add(Path.GetFileName(path)+": nicht lesbar");}
        }
        private bool Save(bool auto)
        {
            try {
                if(!auto && string.IsNullOrWhiteSpace(saveName)){status="Bitte einen Namen eingeben.";return false;}
                SaveGameStore.Write(SaveGameStore.Capture(scenario,auto?"Autosave · "+saveName:saveName,comment),auto,StorageDirectoryOverride);
                autosaveSeconds=0;status=auto?"Autosave gespeichert.":"Spielstand gespeichert.";return true;
            } catch(Exception error){autosaveSeconds=0;status="Speichern fehlgeschlagen: "+error.Message;scenario.Notify(status);return false;}
        }
        private void Close(){IsOpen=false;closedFrame=Time.frameCount;exitAction=null;}
        public void CancelDialog(){Close();}
        public void Leave(Action action){if(HasSession){bool saved=Save(true);string message=status;OpenSave(action);if(!saved)status=message;}else action();}
        private bool WantsToQuit()
        {
            if(Array.Exists(Environment.GetCommandLineArgs(),a=>a.EndsWith("Check")||a=="-spaceMinerSmokeTest"))return true;
            return RequestApplicationExit();
        }
        internal bool RequestApplicationExit()
        {
            if(allowQuit || !HasSession || StartMenu.IsOpen)return true;
            Leave(()=>{allowQuit=true;Application.Quit();});return false;
        }
        private void Update()
        {
            if(IsOpen){if(Input.GetKeyDown(KeyCode.Escape))Close();return;}
            if(!HasSession || StartMenu.IsOpen || IntroSequence.BlocksGameplay || SettingsMenu.PausesSimulation)return;
            if(Input.GetKeyDown(KeyCode.F5))OpenSave();
            if(Input.GetKeyDown(KeyCode.F9))OpenLoad();
            AdvanceAutosave(Time.unscaledDeltaTime);
        }
        internal void AdvanceAutosave(float seconds)
        {
            autosaveSeconds+=Mathf.Max(0,seconds);
            if(autosaveSeconds>=60)Save(true);
        }
        private void OnApplicationPause(bool paused){if(paused && HasSession && !StartMenu.IsOpen)Save(true);}
        private void OnGUI()
        {
            if(!IsOpen)return;
            ui.Configure(SettingsStore.Current.Accessibility);var matrix=GUI.matrix;int depth=GUI.depth;
            float scale=Mathf.Min(Screen.width/1000f,Screen.height/800f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);GUI.depth=-350;
            float width=Screen.width/scale,height=Screen.height/scale;
            SpaceMinerUi.Fill(new Rect(0,0,width,height),new Color(.002f,.009f,.02f,.96f));
            var panel=new Rect((width-900)/2,30,900,height-60);ui.Panel(panel);
            GUILayout.BeginArea(new Rect(panel.x+28,panel.y+24,panel.width-56,panel.height-48));
            GUILayout.Label(loading?"SPIELSTAND LADEN":exitAction!=null?"VOR DEM VERLASSEN SPEICHERN?":"SPIEL SPEICHERN",ui.Heading);GUILayout.Space(18);
            if(loading)
            {
                scroll=GUILayout.BeginScrollView(scroll);
                for(int i=0;i<entries.Count;i++) {
                    var entry=entries[i];GUILayout.Label((saves[i].EndsWith(".bak")?"Sicherung · ":"")+entry.name+" · "+entry.savedAt,ui.Text);GUILayout.Label(entry.Summary,ui.Small);
                    if(!string.IsNullOrEmpty(entry.comment))GUILayout.Label(entry.comment,ui.Text);
                    if(GUILayout.Button("Diesen Spielstand laden",ui.Button,GUILayout.Height(42))) {
                        try {
                            SaveGameStore.Validate(entry,scenario);
                            if(HasSession && !Save(true))break;
                            FindFirstObjectByType<StartMenu>()?.ResumeSavedGame();
                            SaveGameStore.Apply(entry,scenario);saveName=entry.name;comment=entry.comment;HasSession=true;autosaveSeconds=0;Close();
                        }catch(Exception error){status="Laden fehlgeschlagen: "+error.Message;}
                    }
                    GUILayout.Space(22);
                }
                foreach(string warning in warnings)GUILayout.Label(warning,ui.Small);
                if(entries.Count==0)GUILayout.Label("Noch keine gespeicherten Spielstände.",ui.Text);
                GUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label("Name",ui.Text);saveName=GUILayout.TextField(saveName,80,GUILayout.Height(36));GUILayout.Space(12);
                GUILayout.Label("Kommentar · Wo stehst du, was ist als Nächstes geplant?",ui.Text);
                comment=GUILayout.TextArea(comment,2000,GUILayout.Height(145));GUILayout.Space(18);
                GUILayout.Label(SaveGameStore.Capture(scenario,saveName,comment).Summary,ui.Text);GUILayout.Space(20);
                if(GUILayout.Button(exitAction!=null?"Speichern und verlassen":"Speichern",ui.Primary,GUILayout.Height(48)))
                    if(Save(false)){var action=exitAction;Close();action?.Invoke();}
                if(exitAction!=null && GUILayout.Button("Ohne zusätzlichen Spielstand verlassen",ui.Button,GUILayout.Height(44))) {var action=exitAction;Close();action();}
                GUILayout.Label("Autosave alle 60 Sekunden und beim Verlassen. Vorheriger Autosave bleibt als .bak erhalten.",ui.Small);
            }
            GUILayout.Label(status,ui.Small);
            if(GUILayout.Button("Abbrechen",ui.Button,GUILayout.Height(44)))Close();
            GUILayout.EndArea();GUI.matrix=matrix;GUI.depth=depth;
        }
        private void OnDestroy(){Application.wantsToQuit-=WantsToQuit;IsOpen=false;ui.Dispose();}
    }
}
