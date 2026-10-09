using UnityEngine;

namespace SpaceMiner
{
    [DefaultExecutionOrder(-2100)]
    public sealed class TechTreeMenu : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        private static int closedFrame=-1;
        public static bool BlocksInput=>IsOpen||closedFrame==Time.frameCount;
        public static Rect EntryButton=>new Rect(UiLayout.Width-96,18,34,30);
        private readonly SpaceMinerUi ui=new SpaceMinerUi();
        private readonly TechnologyIcons icons=new TechnologyIcons();
        private ResearchLaboratory laboratory;
        internal ResearchLaboratory Presentation=>laboratory;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetState(){IsOpen=false;closedFrame=-1;}
        internal static bool ShortcutAllowed=>!StartMenu.IsOpen&&!SettingsMenu.IsOpen&&!SaveGameMenu.IsOpen&&!QuitMenu.BlocksInput&&!IntroSequence.BlocksGameplay&&!MemoryCinematic.IsPlaying;
        internal void ToggleFromShortcut(){if(!ShortcutAllowed)return;if(IsOpen)Close();else Open();SettingsUiAudio.Activate();}
        private void Update()
        {
            if(PlayerInput.Pressed(CameraAction.Technology))ToggleFromShortcut();
            if(IsOpen)laboratory?.Tick(Time.unscaledDeltaTime);
            if(IsOpen&&Input.GetKeyDown(KeyCode.Escape)){
                if(laboratory!=null&&laboratory.DismissOverlay())return;
                Close();
            }
        }
        public void Open()
        {
            var settings=GetComponent<SettingsMenu>();if(SettingsMenu.IsOpen&&settings!=null)settings.Cancel();
            laboratory??=new ResearchLaboratory();IsOpen=true;
        }
        public void Close(){laboratory?.OnClosed();IsOpen=false;closedFrame=Time.frameCount;}
        public static bool OwnsScreenPoint(Vector3 point)=>IsOpen||EntryButton.Contains(UiLayout.Point(point));
        private void OnDisable(){if(IsOpen)Close();}
        private void OnDestroy(){laboratory?.Dispose();ui.Dispose();icons.Dispose();}
        private void OnGUI()
        {
            var matrix=GUI.matrix;int depth=GUI.depth;Color color=GUI.color;
            try{GUI.matrix=Matrix4x4.Scale(Vector3.one*UiLayout.Scale());Draw();}
            finally{GUI.matrix=matrix;GUI.depth=depth;GUI.color=color;}
        }
        private void Draw()
        {
            if(QuitMenu.BlocksInput||SaveGameMenu.IsOpen)return;
            ui.Configure(SettingsStore.Current.Accessibility);GUI.depth=-210;
            if(!IsOpen){
                if(StartMenu.IsOpen||SettingsMenu.IsOpen||IntroSequence.BlocksGameplay||MemoryCinematic.IsPlaying)return;
                string shortcut=SettingsStore.Current.Controls.Bindings.Get(CameraAction.Technology).ToString();
                if(GUI.Button(EntryButton,new GUIContent("","Technologiebaum öffnen ("+shortcut+")"),ui.Button)){Open();SettingsUiAudio.Activate();}
                icons.Draw(new Rect(EntryButton.x+7,EntryButton.y+5,20,20),"research",SpaceMinerUi.Cyan);
                SettingsUiAudio.Observe(EntryButton,"techtree-entry");ui.Tooltip(1,UiLayout.Width,UiLayout.Height);return;
            }
            float scale=UiLayout.Scale(1600,1000);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            float width=Screen.width/scale,height=Screen.height/scale;
            SpaceMinerUi.Fill(new Rect(0,0,width,height),new Color(.005f,.012f,.03f,.94f));
            var panel=new Rect(15,20,width-30,height-40);
            laboratory.Draw(panel);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(GUI.Button(new Rect(panel.xMax-350,panel.y+20,275,34),laboratory.PlayerPresentation?"Testlabor":"Spielansicht",ui.Button)){
                laboratory.OnClosed();laboratory.PlayerPresentation=!laboratory.PlayerPresentation;
                if(laboratory.PlayerPresentation)laboratory.Simulation.Speed=1;
            }
#endif
            if(GUI.Button(new Rect(panel.xMax-57,panel.y+20,34,34),"×",ui.Button))Close();
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal void PreviewMiningForCheck(bool largeText)
        {
            Open();laboratory.PlayerPresentation=true;laboratory.Preview(5,"drohnen-01");
            laboratory.PreviewTheme=largeText?new AccessibilitySettings{TextScale=1.4f,HighContrast=true}:null;
        }
        private System.Collections.IEnumerator Start()
        {
            var args=System.Environment.GetCommandLineArgs();
            bool demoCheck=System.Array.IndexOf(args,"-techTreeDemoCheck")>=0;
            bool directDemo=System.Array.IndexOf(args,"-techTreeDemo")>=0;
#if TECHTREE_DEMO
            // Dedicated EXE starts here even when double-clicked, while check flags retain their routes.
            directDemo|=!System.Array.Exists(args,a=>a.EndsWith("Check",System.StringComparison.Ordinal))&&System.Array.IndexOf(args,"-researchLab")<0;
#endif
            if(directDemo||demoCheck){
                yield return null;GetComponent<StartMenu>().StartDemo();
                var openingDemo=FindFirstObjectByType<IntroSequence>();if(openingDemo!=null)openingDemo.Skip();
                laboratory=new ResearchLaboratory{PlayerPresentation=true};Open();
                if(!demoCheck)yield break;
                yield return new WaitForSecondsRealtime(.5f);
                Check(IsOpen&&SettingsMenu.BlocksInput&&laboratory.PlayerPresentation,"game presentation modal");
                var demoScenario=FindFirstObjectByType<WaterScenario>();float initialWater=demoScenario.WaterLiters;
                laboratory.PreviewOverview(5);
                yield return new WaitForSecondsRealtime(.3f);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(System.Environment.CurrentDirectory,"Logs/techtree-game-drones.png"));
                yield return new WaitForSecondsRealtime(.3f);
                laboratory.Preview(5,"drohnen-02");
                yield return new WaitForSecondsRealtime(.3f);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(System.Environment.CurrentDirectory,"Logs/techtree-game-details.png"));
                yield return new WaitForSecondsRealtime(.3f);
                Check(laboratory.DismissOverlay()&&IsOpen,"escape clears sidebar before tree");
                laboratory.PlayerPresentation=false;laboratory.Preview(4,"mira-04");laboratory.Tick(7);
                Check(laboratory.Simulation.State("mira-04").work>0,"test laboratory advances independent research");
                laboratory.OnClosed();laboratory.PlayerPresentation=true;laboratory.Simulation.Speed=1;laboratory.PreviewOverview(4);
                yield return new WaitForSecondsRealtime(.3f);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(System.Environment.CurrentDirectory,"Logs/techtree-game-progress.png"));
                yield return new WaitForSecondsRealtime(.3f);
                Check(Mathf.Approximately(initialWater,demoScenario.WaterLiters),"presentation does not change water");
                var demoSettings=GetComponent<SettingsMenu>();demoSettings.Open();
                Check(SettingsMenu.IsOpen&&!IsOpen,"settings replaces game presentation");
                Open();Check(IsOpen&&!SettingsMenu.IsOpen&&laboratory.PlayerPresentation,"reopen retains game presentation");
                Close();Check(BlocksInput,"game presentation close consumes frame");
                yield return null;Check(!BlocksInput,"game presentation releases input");
                Debug.Log("TECHTREE GAME PRESENTATION CHECK PASSED");Application.Quit();yield break;
            }
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-researchLab")>=0) {
                yield return null;GetComponent<StartMenu>().StartDemo();
                var introLab=FindFirstObjectByType<IntroSequence>();if(introLab!=null)introLab.Skip();
                Open();laboratory.PlayerPresentation=false;yield break;
            }
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-researchLabCheck")>=0) {
                yield return null;
                GetComponent<StartMenu>().StartDemo();
                var opening=FindFirstObjectByType<IntroSequence>();if(opening!=null)opening.Skip();
                yield return new WaitForSecondsRealtime(.5f);
                Open();laboratory.PlayerPresentation=false;
                laboratory.Preview(4,"mira-04");
                yield return new WaitForSecondsRealtime(.5f);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(System.Environment.CurrentDirectory,"Logs/research-laboratory.png"));
                yield return new WaitForSecondsRealtime(.5f);
                Check(IsOpen&&SettingsMenu.BlocksInput,"laboratory modal");
                laboratory.PreviewOverview(2);
                yield return new WaitForSecondsRealtime(.3f);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(System.Environment.CurrentDirectory,"Logs/research-materials-overview.png"));
                yield return new WaitForSecondsRealtime(.3f);
                laboratory.PreviewOverview(3,true);
                yield return new WaitForSecondsRealtime(.3f);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(System.Environment.CurrentDirectory,"Logs/research-connections-zoom.png"));
                yield return new WaitForSecondsRealtime(.3f);
                laboratory.Preview(2,"materialien-02");
                yield return new WaitForSecondsRealtime(.5f);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(System.Environment.CurrentDirectory,"Logs/research-materials.png"));
                yield return new WaitForSecondsRealtime(.5f);
                var preferences=GetComponent<SettingsMenu>();preferences.Open();
                Check(SettingsMenu.IsOpen&&!IsOpen,"settings replaces laboratory");
                Open();Check(IsOpen&&!SettingsMenu.IsOpen,"laboratory replaces settings");
                Close();Check(BlocksInput,"laboratory close consumes frame");
                Debug.Log("RESEARCH LABORATORY PLAYER CHECK PASSED");Application.Quit();yield break;
            }
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-techTreeCheck")<0)yield break;
            yield return null;
            var intro=FindFirstObjectByType<IntroSequence>();if(intro!=null)intro.Skip();
            yield return new WaitForSecondsRealtime(.5f);
            string folder=System.IO.Path.Combine(System.Environment.CurrentDirectory,"Logs");
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,"techtree-entry.png"));
            yield return new WaitForSecondsRealtime(.4f);
            var scenario=FindFirstObjectByType<WaterScenario>();
            if(StartMenu.IsOpen)GetComponent<StartMenu>().StartDemo();
            if(intro!=null)intro.Skip();
            var interior=StationInteriorMode.Current;
            interior.RestoreInterior(interior.Layout.Console.position-interior.Room.forward*1.5f,false);
            scenario.Scanner.OpenConsole();scenario.Scanner.Advance(10);
            Check(scenario.Scanner.Scan(),"first scan before water order");
            scenario.Scanner.OpenVirtualView();scenario.Scanner.DismissDialogue();
            bool assigned=false;foreach(var source in scenario.Asteroids)if(source.CanMineWater&&scenario.AssignTankOrder(source)){assigned=true;break;}
            Check(assigned,"active water order for menu pause check");
            Open();yield return new WaitForSecondsRealtime(.3f);
            Check(IsOpen&&SettingsMenu.BlocksInput,"modal input blocked");
            float water=scenario.WaterLiters;Vector3 workerPosition=scenario.Worker.transform.position;
            Check(!SettingsStore.Current.Gameplay.PauseInMenu||SettingsMenu.PausesSimulation,"menu pause policy");
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,"techtree-menu.png"));
            yield return new WaitForSecondsRealtime(.4f);
            Check(Mathf.Approximately(water,scenario.WaterLiters),"view does not mutate resources");
            if(SettingsStore.Current.Gameplay.PauseInMenu)Check(Vector3.Distance(workerPosition,scenario.Worker.transform.position)<.001f,"active drone pauses in tree");
            laboratory.Preview(0,"energie-02");
            yield return new WaitForSecondsRealtime(.2f);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,"techtree-info.png"));
            yield return new WaitForSecondsRealtime(.3f);
            PreviewMiningForCheck(true);
            yield return new WaitForSecondsRealtime(.2f);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,"techtree-large-text.png"));
            yield return new WaitForSecondsRealtime(.3f);laboratory.PreviewTheme=null;
            var settings=GetComponent<SettingsMenu>();settings.Open();
            Check(SettingsMenu.IsOpen&&!IsOpen,"settings replaces tree");
            Open();Check(IsOpen&&!SettingsMenu.IsOpen,"tree replaces settings");
            Close();Check(SettingsMenu.BlocksInput,"close consumes frame");
            yield return null;Check(!SettingsMenu.BlocksInput,"input released next frame");
            Debug.Log("TECHTREE MENU CHECK PASSED");Application.Quit();
        }
        private static void Check(bool condition,string message){if(!condition){Debug.LogError("TECHTREE CHECK FAILED: "+message);Application.Quit(1);throw new System.Exception(message);}}
#endif
    }
}
