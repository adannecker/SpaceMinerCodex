using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    [DefaultExecutionOrder(-2100)]
    public sealed class TechTreeMenu : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        private static int closedFrame=-1;
        public static bool BlocksInput => IsOpen || closedFrame==Time.frameCount;
        public static Rect EntryButton => new Rect(Screen.width-96,18,34,30);
        private readonly SpaceMinerUi ui=new SpaceMinerUi();
        private readonly TechnologyIcons icons=new TechnologyIcons();
        private int pinned=-1;
        private GUIStyle nodeButton;
        private Rect[] nodeRects;
        private List<TechnologyEdge> edges;
        private int previewHover=-1;
        private AccessibilitySettings previewTheme;
        private Rect lastInfoRect;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetState(){IsOpen=false;closedFrame=-1;}
        private void Update(){if(IsOpen&&Input.GetKeyDown(KeyCode.Escape))Close();}
        public void Open()
        {
            var settings=GetComponent<SettingsMenu>();if(SettingsMenu.IsOpen&&settings!=null)settings.Cancel();
            pinned=-1;IsOpen=true;
        }
        public void Close(){IsOpen=false;closedFrame=Time.frameCount;pinned=-1;previewHover=-1;lastInfoRect=default;}
        public static bool OwnsScreenPoint(Vector3 point)=>IsOpen||EntryButton.Contains(new Vector2(point.x,Screen.height-point.y));
        private void OnDisable(){if(IsOpen)Close();}
        private void OnDestroy(){ui.Dispose();icons.Dispose();}
        private void OnGUI()
        {
            var matrix=GUI.matrix;int depth=GUI.depth;Color color=GUI.color;
            try {Draw();} finally {GUI.matrix=matrix;GUI.depth=depth;GUI.color=color;}
        }
        private void Draw()
        {
            ui.Configure(previewTheme??SettingsStore.Current.Accessibility);GUI.depth=-210;
            if(!IsOpen) {
                if(SettingsMenu.IsOpen)return;
                if(GUI.Button(EntryButton,new GUIContent("","Technologiebaum öffnen"),ui.Button)){Open();SettingsUiAudio.Activate();}
                icons.Draw(new Rect(EntryButton.x+7,EntryButton.y+5,20,20),"research",SpaceMinerUi.Cyan);
                SettingsUiAudio.Observe(EntryButton,"techtree-entry");ui.Tooltip(1,Screen.width,Screen.height);return;
            }
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            float width=Screen.width/scale,height=Screen.height/scale;
            SpaceMinerUi.Fill(new Rect(0,0,width,height),new Color(.005f,.012f,.03f,.94f));
            Rect panel=new Rect((width-1250)/2,(height-680)/2,1250,680);ui.Panel(panel);
            GUI.Label(new Rect(panel.x+30,panel.y+20,1100,38),"SPACE MINER // TECHNOLOGY RESEARCH",ui.Heading);
            GUI.Label(new Rect(panel.x+30,panel.y+67,1110,28),"MINING PULSE     /     OPERATOR CONSOLE     /     TIER I - PIONEER AGE",ui.Small);
            Rect closeRect=new Rect(panel.xMax-57,panel.y+21,34,34);
            if(GUI.Button(closeRect,new GUIContent("","Technologiebaum schliessen"),ui.Button)){Close();SettingsUiAudio.Activate();}
            icons.Draw(new Rect(closeRect.x+8,closeRect.y+8,18,18),"close",SpaceMinerUi.Cyan);
            SpaceMinerUi.Fill(new Rect(panel.x+24,panel.y+105,panel.width-48,1),SpaceMinerUi.Cyan*.55f);
            Legend(new Rect(panel.x+30,panel.y+119,220,26),"Vorhandene Technik",SpaceMinerUi.Success);
            Legend(new Rect(panel.x+275,panel.y+119,330,26),"Geplantes Forschungsfeld",SpaceMinerUi.Disabled);
            GUI.Label(new Rect(panel.x+660,panel.y+119,550,26),"Hover: Info     |     Klick: anheften     |     Esc: schliessen",ui.Small);
            string[] stages={"START","GRUNDLAGEN","FORSCHUNG","KOMBINATION","VERSORGUNG"};
            Rect field=new Rect(panel.x+36,panel.y+205,panel.width-72,350);
            float[] positions={.035f,.245f,.475f,.735f,.965f};
            var stageStyle=new GUIStyle(ui.Small){alignment=TextAnchor.MiddleCenter};
            for(int i=0;i<stages.Length;i++)GUI.Label(new Rect(field.x+field.width*positions[i]-80,panel.y+166,160,28),stages[i],stageStyle);
            nodeRects=TechnologyLayout.Nodes(field);edges=TechnologyLayout.Edges(field,nodeRects);
            int hover=previewHover;
            bool overPinnedInfo=pinned>=0&&lastInfoRect.Contains(Event.current.mousePosition);
            if(hover<0&&!overPinnedInfo)for(int i=0;i<nodeRects.Length;i++)if(nodeRects[i].Contains(Event.current.mousePosition)){hover=i;break;}
            int shown=hover>=0?hover:pinned;
            var highlighted=new HashSet<int>();if(shown>=0)Ancestors(shown,highlighted);
            // Draw subdued edges first so highlighted routes remain visible through junction areas.
            foreach(bool active in new[]{false,true})foreach(var edge in edges) {
                bool match=highlighted.Contains(edge.Source)&&highlighted.Contains(edge.Target);if(match!=active)continue;
                Color edgeColor=active?SpaceMinerUi.Amber:new Color(.20f,.39f,.48f);
                for(int i=1;i<edge.Points.Length;i++)Segment(edge.Points[i-1],edge.Points[i],edgeColor,active?2:1);
            }
            nodeButton??=new GUIStyle(ui.Button){padding=new RectOffset(0,0,0,0)};
            bool enabled=GUI.enabled;GUI.enabled=enabled&&!overPinnedInfo;
            for(int i=0;i<nodeRects.Length;i++) {
                var node=TechnologyCatalog.TierOne[i];Rect rect=nodeRects[i];
                if(GUI.Button(rect,GUIContent.none,nodeButton)){pinned=i;shown=i;SettingsUiAudio.Activate();}
                Color tint=node.Installed?SpaceMinerUi.Success:SpaceMinerUi.Disabled;
                SpaceMinerUi.Border(rect,tint);if(i==shown){SpaceMinerUi.Border(new Rect(rect.x-3,rect.y-3,rect.width+6,rect.height+6),SpaceMinerUi.Amber);tint=SpaceMinerUi.Amber;}
                icons.Draw(new Rect(rect.x+10,rect.y+10,30,30),node.Icon,tint);
                SettingsUiAudio.Observe(rect,"tech-"+node.Id);
            }
            GUI.enabled=enabled;
            foreach(var edge in edges){Color tint=highlighted.Contains(edge.Source)&&highlighted.Contains(edge.Target)?SpaceMinerUi.Amber:SpaceMinerUi.Cyan;Port(edge.Points[0],tint);Port(edge.Points[edge.Points.Length-1],tint);}
            SpaceMinerUi.Fill(new Rect(panel.x+24,panel.y+594,panel.width-48,1),new Color(.15f,.7f,.9f,.6f));
            GUI.Label(new Rect(panel.x+30,panel.y+613,1150,26),"RESEARCH CORE ONLINE  |  TIER I  |  Forschung, Kosten und Wissenslevel folgen im nächsten Schritt",ui.Small);
            if(shown>=0)DrawInfo(shown,hover<0,panel);else lastInfoRect=default;
        }
        private void DrawInfo(int index,bool isPinned,Rect panel)
        {
            var n=TechnologyCatalog.TierOne[index];
            string status=n.Installed?"VORHANDENE TECHNIK":"GEPLANTES FORSCHUNGSFELD";
            string body=n.Description+"\n\nFORSCHUNGSVORAUSSETZUNGEN\n"+n.Requirements+"\n\nPRAKTISCHE NUTZUNG\n"+n.Practical;
            float bodyHeight=ui.Small.CalcHeight(new GUIContent(body),406);
            float titleHeight=ui.Text.CalcHeight(new GUIContent(n.Title),374);
            float boxHeight=bodyHeight+titleHeight+105;
            Vector2 mouse=Event.current.mousePosition;
            if(previewHover>=0||isPinned)mouse=nodeRects[index].center;
            float x=mouse.x+22,y=mouse.y+18;
            if(isPinned){x=nodeRects[index].xMax+20;y=nodeRects[index].y;}
            if(x+438>panel.xMax-16)x=isPinned?nodeRects[index].xMin-458:mouse.x-458;
            x=Mathf.Clamp(x,panel.x+16,panel.xMax-454);y=Mathf.Clamp(y,panel.y+16,panel.yMax-boxHeight-16);
            Rect rect=new Rect(x,y,438,boxHeight);
            lastInfoRect=rect;
            SpaceMinerUi.Fill(rect,new Color(.025f,.055f,.09f,1));ui.Panel(rect);
            GUI.Label(new Rect(x+16,y+12,380,25),status,ui.Small);
            var title=new GUIStyle(ui.Text);title.normal.textColor=SpaceMinerUi.Amber;
            GUI.Label(new Rect(x+16,y+44,374,titleHeight),n.Title,title);
            GUI.Label(new Rect(x+16,y+49+titleHeight,406,bodyHeight),body,ui.Small);
            GUI.Label(new Rect(x+16,rect.yMax-32,406,24),n.Installed?"Aktuelle Startfähigkeit":"Entwurf · noch keine Forschungsfreigabe",ui.Small);
            if(pinned>=0){Rect close=new Rect(rect.xMax-42,rect.y+9,28,28);if(GUI.Button(close,GUIContent.none,ui.Button))pinned=-1;icons.Draw(new Rect(close.x+7,close.y+7,14,14),"close",SpaceMinerUi.Cyan);}
        }
        private void Legend(Rect rect,string text,Color color){SpaceMinerUi.Fill(new Rect(rect.x,rect.y+9,8,8),color);GUI.Label(new Rect(rect.x+17,rect.y,rect.width-17,rect.height),text,ui.Small);}
        private static void Ancestors(int index,HashSet<int> result){if(!result.Add(index))return;foreach(string id in TechnologyCatalog.TierOne[index].Parents)Ancestors(TechnologyCatalog.IndexOf(id),result);}
        private static void Port(Vector2 p,Color color){var rect=new Rect(p.x-3,p.y-3,6,6);SpaceMinerUi.Fill(rect,new Color(.025f,.055f,.09f));SpaceMinerUi.Border(rect,color);}
        private static void Segment(Vector2 a,Vector2 b,Color color,float thickness)
        { if(Mathf.Abs(a.y-b.y)<.1f)SpaceMinerUi.Fill(new Rect(Mathf.Min(a.x,b.x),a.y-thickness/2,Mathf.Abs(a.x-b.x),thickness),color);
          else SpaceMinerUi.Fill(new Rect(a.x-thickness/2,Mathf.Min(a.y,b.y),thickness,Mathf.Abs(a.y-b.y)),color); }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private System.Collections.IEnumerator Start()
        {
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-techTreeCheck")<0)yield break;
            yield return null;
            var intro=FindFirstObjectByType<IntroSequence>();if(intro!=null)intro.Skip();
            yield return new WaitForSecondsRealtime(.5f);
            string folder=System.IO.Path.Combine(System.Environment.CurrentDirectory,"Logs");
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,"techtree-entry.png"));
            yield return new WaitForSecondsRealtime(.4f);
            var scenario=FindFirstObjectByType<WaterScenario>();
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
            previewHover=TechnologyCatalog.IndexOf("processing");
            yield return new WaitForSecondsRealtime(.2f);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,"techtree-info.png"));
            yield return new WaitForSecondsRealtime(.3f);previewHover=-1;
            pinned=TechnologyCatalog.IndexOf("auto");previewTheme=new AccessibilitySettings{TextScale=1.4f,HighContrast=true};
            yield return new WaitForSecondsRealtime(.2f);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,"techtree-large-text.png"));
            yield return new WaitForSecondsRealtime(.3f);previewTheme=null;
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
