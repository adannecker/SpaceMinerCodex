using System;
using System.Collections;
using UnityEngine;

namespace SpaceMiner
{
    [DefaultExecutionOrder(-2050)]
    public sealed class RingConstructionSimulator : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        private static int closedFrame=-1;
        public static bool BlocksInput=>IsOpen||closedFrame==Time.frameCount;
        public static Rect EntryButton=>new Rect(UiLayout.Width-230,18,124,30);
        internal RingConstructionModel Model { get; private set; }
        private readonly SpaceMinerUi ui=new SpaceMinerUi();
        private RingConstructionView view;
        private RingWalkthrough walkthrough;
        private DisplayModeController display;
        internal DisplayModeController Display => display != null ? display : display = FindFirstObjectByType<DisplayModeController>();
        internal RingWalkthrough Walkthrough => walkthrough;
        internal bool Walking { get; private set; }
        private RingConstructionModel.Tool tool;
        private RingConstructionModel.Product storageProduct=RingConstructionModel.Product.Parts;
        private int activeLevel;
        private bool isolateLevel;
        private Rect viewport;
        private Vector2 leftScroll,rightScroll;
        private int selectedRing=0,selectedSector=6;
        private bool selectedOutside;
        private float speed=1,elapsed;
        private Vector2 lastMouse;
        private bool dragging;
        private string status="Wähle links ein Bauwerkzeug oder beauftrage einen neuen Ring.";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetState(){IsOpen=false;closedFrame=-1;}
        public void Open()
        {
            if(CommandRoomPlanner.BlocksInput)return;
            if(SettingsMenu.IsOpen)GetComponent<SettingsMenu>().Cancel();
            if(TechTreeMenu.IsOpen)GetComponent<TechTreeMenu>().Close();
            Model??=new RingConstructionModel();view??=new RingConstructionView();view.SetActive(true);IsOpen=true;
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        public void Close(){IsOpen=false;closedFrame=Time.frameCount;dragging=false;view?.SetActive(false);walkthrough?.SetActive(false);Walking=false;}
        private void OnDisable(){if(IsOpen)Close();}
        private void OnDestroy(){if(IsOpen)Close();view?.Dispose();walkthrough?.Dispose();ui.Dispose();}
        internal void EnterWalkthrough()
        {
            if(CommandRoomPlanner.BlocksInput)return;
            if(!IsOpen)Open();
            view.SetActive(false);walkthrough??=new RingWalkthrough();walkthrough.SetActive(true);Walking=true;dragging=false;
        }
        internal void LeaveWalkthrough()
        {
            walkthrough?.SetActive(false);Walking=false;view?.SetActive(true);
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        private void Update()
        {
            if(CommandRoomPlanner.BlocksInput)return;
            bool allowed=!StationOperations.TacticalViewActive&&StationOperations.Current?.PanelOpen!=true&&!StartMenu.IsOpen&&!SaveGameMenu.IsOpen&&!QuitMenu.BlocksInput&&!IntroSequence.BlocksGameplay&&!MemoryCinematic.IsPlaying&&!SettingsMenu.IsOpen;
            if(Input.GetKeyDown(KeyCode.F6)&&allowed){if(IsOpen)Close();else Open();}
            if(!IsOpen)return;
            if(Input.GetKeyDown(KeyCode.Escape)){if(Walking)LeaveWalkthrough();else Close();return;}
            if(Walking){walkthrough.Tick(Time.unscaledDeltaTime,allowed);return;}
            float dt=Mathf.Min(Time.unscaledDeltaTime,.1f)*speed;Model.Tick(dt);elapsed+=dt;
            var point=UiLayout.Point(Input.mousePosition,1600,1000);
            if(viewport.Contains(point)&&point.y>viewport.y+94){
                if(Input.GetMouseButtonDown(1)){dragging=true;lastMouse=point;}
                float wheel=Input.mouseScrollDelta.y;if(wheel!=0)view.Zoom(-wheel);
                var normalized=new Vector2((point.x-viewport.x)/viewport.width,1-(point.y-viewport.y)/viewport.height);
                if(view.Pick(normalized,Model,tool,out int r,out int s,out bool outside)){
                    bool valid=Model.CanPlace(r,s,tool,out _);view.Hover(Model,r,s,tool,valid);
                    if(Input.GetMouseButtonDown(0)){
                        selectedRing=r;selectedSector=s;selectedOutside=tool==RingConstructionModel.Tool.Inspect?outside:tool==RingConstructionModel.Tool.Storage;
                        if(tool!=RingConstructionModel.Tool.Inspect)Model.Place(r,s,tool,out status,storageProduct);
                    }
                }else view.Hover(Model,-1,-1,tool,false);
            }else view.Hover(Model,-1,-1,tool,false);
            if(!Input.GetMouseButton(1))dragging=false;
            if(dragging){view.Orbit(point-lastMouse);lastMouse=point;}
        }
        private void LateUpdate()
        {
            if(CommandRoomPlanner.BlocksInput||!IsOpen||view==null)return;
            if(Walking){walkthrough.ApplyView(!StartMenu.IsOpen&&!SettingsMenu.IsOpen&&!QuitMenu.BlocksInput&&!SaveGameMenu.IsOpen);return;}
            view.Update(Model,elapsed);
        }
        private IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();bool walk=Array.IndexOf(args,"-ringWalkthrough")>=0;bool direct=walk||Array.IndexOf(args,"-ringSimulator")>=0;
#if RING_SIMULATOR_DEMO
            direct|=!Array.Exists(args,a=>a.EndsWith("Check",StringComparison.Ordinal));
#endif
            if(!direct)yield break;
            yield return null;
            GetComponent<StartMenu>().StartDemo();FindFirstObjectByType<IntroSequence>()?.Skip();
            FindFirstObjectByType<WaterScenario>()?.Scanner.DismissDialogue();Open();
            if(walk)EnterWalkthrough();
        }
        private void OnGUI()
        {
            using(var scope=new UiLayout.Scope(UiLayout.Scale())){
                if(CommandRoomPlanner.BlocksInput||QuitMenu.BlocksInput||SaveGameMenu.IsOpen)return;
                ui.Configure(SettingsStore.Current.Accessibility);
                if(IsOpen&&Walking){int oldDepth=GUI.depth;try{GUI.depth=-216;walkthrough.Draw(LeaveWalkthrough,Display);}finally{GUI.depth=oldDepth;}return;}
                if(!IsOpen){
                    if(StationOperations.TacticalViewActive||StationOperations.Current?.PanelOpen==true||StartMenu.IsOpen||SettingsMenu.IsOpen||TechTreeMenu.IsOpen||IntroSequence.BlocksGameplay||MemoryCinematic.IsPlaying)return;
                    if(GUI.Button(EntryButton,new GUIContent("Ausbau · F6","Ringbau-Simulator öffnen"),ui.Button))Open();return;
                }
            }
            var matrix=GUI.matrix;int depth=GUI.depth;
            try{GUI.matrix=Matrix4x4.Scale(Vector3.one*UiLayout.Scale(1600,1000));GUI.depth=-215;Draw();}
            finally{GUI.matrix=matrix;GUI.depth=depth;}
        }
        private void Draw()
        {
            float width=Screen.width/UiLayout.Scale(1600,1000),height=Screen.height/UiLayout.Scale(1600,1000);
            SpaceMinerUi.Fill(new Rect(0,0,width,height),new Color(.008f,.018f,.03f));
            GUI.Label(new Rect(26,18,width-220,40),"STATIONSPLANUNG // RINGAUSBAU",ui.Heading);
            GUI.Label(new Rect(28,62,width-250,27),"3D-TESTMODELL  ·  Eigene Testressourcen  ·  Spielwelt pausiert",ui.Small);
            if(GUI.Button(new Rect(width-174,24,146,42),"Schließen",ui.Button))Close();
            var display=Display;
            bool enabled=GUI.enabled;GUI.enabled=enabled&&display!=null&&display.CanSwitch;
            if(GUI.Button(new Rect(width-194,70,166,30),display!=null&&display.IsFullscreen?"Fenster / F11":"Vollbild / F11",ui.Button))display.Toggle();
            GUI.enabled=enabled;
            var left=new Rect(22,108,250,height-194);var right=new Rect(width-312,108,290,height-194);
            ui.Panel(left);ui.Panel(right);
            viewport=new Rect(284,108,width-608,height-194);
            view.SetViewport(Mathf.RoundToInt(viewport.width*UiLayout.Scale(1600,1000)),Mathf.RoundToInt(viewport.height*UiLayout.Scale(1600,1000)));
            if(view.Texture!=null)GUI.DrawTexture(viewport,view.Texture,ScaleMode.StretchToFill);
            SpaceMinerUi.Border(viewport,new Color(.17f,.4f,.5f));
            GUI.Label(new Rect(viewport.x+14,viewport.y+12,viewport.width-28,46),"AUSSEN: LAGER    /    INNEN: PRODUKTION",ui.Small);
            for(int r=0;r<Model.Rings.Count;r++)if(GUI.Button(new Rect(viewport.x+14+r*132,viewport.y+48,124,34),"Ebene "+(r+1),r==activeLevel?ui.Primary:ui.Button))SelectLevel(r,isolateLevel);
            if(GUI.Button(new Rect(viewport.xMax-176,viewport.y+48,162,34),isolateLevel?"Beide Ebenen":"Nur diese Ebene",ui.Button))SelectLevel(activeLevel,!isolateLevel);
            GUI.Label(new Rect(viewport.x+14,viewport.yMax-64,viewport.width-28,54),"Linksklick: wählen / bauen\nRechts ziehen: drehen  ·  Mausrad: zoomen",ui.Small);
            DrawTools(left);DrawDetails(right);
            GUI.Label(new Rect(26,height-72,width-52,54),status,ui.Text);
        }
        private void DrawTools(Rect panel)
        {
            GUILayout.BeginArea(new Rect(panel.x+16,panel.y+22,panel.width-32,panel.height-36));
            leftScroll=GUILayout.BeginScrollView(leftScroll);
            if(GUILayout.Button("BEGEHBARER ENTWURF\nGroessenprobe / alles offen",ui.Primary,GUILayout.MinHeight(66)))EnterWalkthrough();
            GUILayout.Space(16);
            GUILayout.Label("BAUEN",ui.Heading);GUILayout.Space(16);
            foreach(RingConstructionModel.Tool value in Enum.GetValues(typeof(RingConstructionModel.Tool))){
                string label=RingConstructionModel.Name(value)+(value==RingConstructionModel.Tool.Inspect?"":"\n"+RingConstructionModel.Cost(value)+" Testbauteile");
                if(GUILayout.Button(label,tool==value?ui.Primary:ui.Button,GUILayout.MinHeight(58))){tool=value;status=value==RingConstructionModel.Tool.Inspect?"Modul oder freien Bauplatz anklicken.":"Bauplatz "+(value==RingConstructionModel.Tool.Storage?"außen":"innen")+" am Ring anklicken. Grün: frei · Rot: gesperrt.";}
                GUILayout.Space(9);
            }
            if(tool==RingConstructionModel.Tool.Storage){
                GUILayout.Label("LAGER NUR FÜR",ui.Small);
                foreach(RingConstructionModel.Product product in Enum.GetValues(typeof(RingConstructionModel.Product)))
                    if(GUILayout.Button(RingConstructionModel.ProductName(product),storageProduct==product?ui.Primary:ui.Button,GUILayout.Height(36)))storageProduct=product;
                GUILayout.Space(12);
            }
            GUILayout.Space(14);GUI.enabled=Model.Rings.Count<2&&Model.Rings[Model.Rings.Count-1].Complete;
            if(GUILayout.Button(Model.Rings.Count>=2?"Zweite Ebene angebaut":"+ Ring darüber bauen\n16 Segmente · 192 Teile",ui.Button,GUILayout.MinHeight(65))){if(Model.AddRing(out status))SelectLevel(1,false);}
            GUI.enabled=true;GUILayout.Space(16);
            GUILayout.Label("ZEIT",ui.Small);
            GUILayout.BeginHorizontal();foreach(float value in new[]{0f,1f,5f})if(GUILayout.Button(value==0?"Pause":value+"×",speed==value?ui.Primary:ui.Button,GUILayout.Height(35)))speed=value;GUILayout.EndHorizontal();
            GUILayout.Space(10);if(GUILayout.Button("Ansicht zurücksetzen",ui.Button,GUILayout.Height(40))){view.Home();SelectLevel(activeLevel,false);}
            if(GUILayout.Button("Draufsicht der Ebene",ui.Button,GUILayout.Height(40))){SelectLevel(activeLevel,true);view.Home(true);}
            GUILayout.Space(16);if(GUILayout.Button("Erz liefern (+20)",ui.Button,GUILayout.Height(42)))status=Model.SupplyOre()?"20 Erz im Testlager eingetroffen.":"Erzlager voll. Zusätzlichen Lagercontainer bauen.";
            GUILayout.Space(16);if(GUILayout.Button("Testmodell neu starten",ui.Button,GUILayout.Height(42))){view.Dispose();view=new RingConstructionView();Model=new RingConstructionModel();elapsed=0;speed=1;activeLevel=0;isolateLevel=false;selectedRing=0;selectedSector=6;selectedOutside=false;status="Testmodell zurückgesetzt.";}
            GUILayout.EndScrollView();GUILayout.EndArea();
        }
        private void DrawDetails(Rect panel)
        {
            GUILayout.BeginArea(new Rect(panel.x+16,panel.y+22,panel.width-32,panel.height-36));rightScroll=GUILayout.BeginScrollView(rightScroll);
            GUILayout.Label("STATION",ui.Heading);GUILayout.Space(12);
            GUILayout.Label(Model.Rings.Count+" Ringe  ·  "+Model.Modules.FindAll(m=>m.Built).Count+" Module\n"+Model.ConstructionParts+" Testbauteile verfügbar",ui.Text);
            GUILayout.Space(14);
            for(int r=0;r<Model.Rings.Count;r++){
                var ring=Model.Rings[r];int built=Array.FindAll(ring.Segments,b=>b).Length;
                GUILayout.Label("Ebene "+(r+1)+"  /  "+built+" von "+ring.Segments.Length+" Segmenten\n"+Model.Slots(r)+" Lagerplätze + "+Model.Slots(r)+" Produktionsplätze",ui.Small);
                GUILayout.Space(10);
            }
            GUILayout.Space(8);GUILayout.Label("LAGER / PRODUKTION",ui.Heading);
            foreach(RingConstructionModel.Product product in Enum.GetValues(typeof(RingConstructionModel.Product)))
                GUILayout.Label(RingConstructionModel.ProductName(product)+"  "+Model.Stock(product)+" / "+Model.CapacityFor(product)+(Model.CapacityFor(product)==0?" · Lager fehlt":""),ui.Text);
            GUILayout.Space(8);GUILayout.Label("Jeder Container lagert nur ein Produkt. Neue Produkte brauchen eigene Lager.\nTrennung: Erz → Metall\nFertigung: Metall → Bauteil",ui.Small);
            GUILayout.Space(18);GUILayout.Label("BAUAUFTRÄGE",ui.Heading);
            var job=Model.ActiveJob;
            if(job==null)GUILayout.Label("Baudrohne bereit",ui.Text);
            else{
                GUILayout.Label((job.Module==null?"Ringsegment":RingConstructionModel.Name(job.Module.Kind))+"\nEbene "+(job.Ring+1)+" · Platz "+(job.Sector+1)+"\n"+Mathf.RoundToInt(job.Progress*100)+" % · "+Model.Jobs.Count+" Aufträge",ui.Text);
                var rect=GUILayoutUtility.GetRect(20,8);SpaceMinerUi.Fill(rect,new Color(.07f,.13f,.18f));SpaceMinerUi.Fill(new Rect(rect.x,rect.y,rect.width*job.Progress,8),SpaceMinerUi.Amber);
            }
            GUILayout.Space(18);GUILayout.Label("AUSWAHL",ui.Heading);
            GUILayout.Label("Ebene "+(selectedRing+1)+" · Platz "+(selectedSector+1)+"\n"+(selectedOutside?"Außenseite / Lager":"Innenseite / Produktion"),ui.Text);
            var module=Model.At(selectedRing,selectedSector,selectedOutside);
            if(Model.Reserved(selectedRing,selectedSector))GUILayout.Label("Reservierter Übergang. Hier bleiben Gehweg und Drohnenroute frei.",ui.Small);
            else if(module!=null){GUILayout.Label(RingConstructionModel.Name(module.Kind),ui.Text);GUILayout.Label(Model.Status(module),ui.Small);}
            else GUILayout.Label(Model.Rings[selectedRing].Segments[selectedSector]?"Freier Bauplatz":"Ringsegment noch im Bau",ui.Small);
            GUILayout.Space(20);GUILayout.Label("MODELLGRENZEN",ui.Small);
            GUILayout.Label("Offenes Schnittmodell. Feste Bauplätze verhindern Überlappungen; Übergänge sind reserviert. Drohnen zeigen Abläufe schematisch. Dock-Umbau, begehbare Ringe und echte Verkehrssteuerung folgen später.",ui.Small);
            GUILayout.EndScrollView();GUILayout.EndArea();
        }
        internal bool CheckPicking()=>view.CheckPicking(Model);
        internal void SelectLevel(int level,bool isolate)
        {
            activeLevel=level;isolateLevel=isolate;selectedRing=level;view.SelectLevel(level,isolate);
        }
    }
}
