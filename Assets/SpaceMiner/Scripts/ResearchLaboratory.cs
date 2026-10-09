using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SpaceMiner
{
    public sealed class ResearchLaboratory : IDisposable
    {
        public readonly ResearchSimulation Simulation;
        internal AccessibilitySettings PreviewTheme;
        private bool playerPresentation=true;
        public bool PlayerPresentation { get=>playerPresentation; set=>playerPresentation=value||!(Application.isEditor||Debug.isDebugBuild); }
        private readonly TechnologyIcons icons=new TechnologyIcons();
        private readonly ResearchMiniatures miniatures=new ResearchMiniatures();
        private readonly SpaceMinerUi ui=new SpaceMinerUi();
        private readonly ResearchTreeViewport view=new ResearchTreeViewport();
        private int category;
        private string selected;
        private Vector2 infoScroll;
        private bool resetPrompt,showNotes;
        private string message="Knoten anklicken · Ziehen: Ansicht verschieben · Mausrad: Zoom";
        private Rect infoRect,lastField;
        internal Rect SidebarRect=>infoRect;
        internal Rect TreeRect=>lastField;
        internal void ScrollDetailsForCheck(float y){infoScroll.y=y;}
        private int dragControl;
        private string SavePath=>Path.Combine(Application.persistentDataPath,"research-laboratory.json");
        public ResearchLaboratory()
        {
            Simulation=new ResearchSimulation(JsonUtility.FromJson<ResearchCatalog>(Resources.Load<TextAsset>("Research/catalog").text));
            view.SetTree(Simulation.Catalog.trees[0]);
        }
        internal void Preview(int tree,string id)
        {
            ChooseCategory(tree);selected=id;
            if(id=="mira-04"){Simulation.State(id).evidence=true;Simulation.State(id).hardware=true;Simulation.Enqueue(id);Simulation.Speed=30;}
        }
        internal void PreviewOverview(int tree,bool enlarged=false)
        {ChooseCategory(tree);if(enlarged)view.SetZoom(1,lastField);}
        public void OnClosed(){if(GUIUtility.hotControl==dragControl)GUIUtility.hotControl=0;selected=null;resetPrompt=false;}
        public bool DismissOverlay()
        {
            if(resetPrompt){resetPrompt=false;return true;}
            if(selected==null)return false;
            selected=null;return true;
        }
        private void ChooseCategory(int index)
        {
            category=index;selected=null;infoScroll=Vector2.zero;showNotes=false;
            view.SetTree(Simulation.Catalog.trees[index]);
        }
        public void Tick(float seconds)=>Simulation.Advance(seconds);
        public void Dispose(){miniatures.Dispose();icons.Dispose();ui.Dispose();}
        public void Draw(Rect panel)
        {
            ui.Configure(PreviewTheme??SettingsStore.Current.Accessibility);
            SpaceMinerUi.Fill(panel,new Color(.018f,.035f,.06f));ui.Panel(panel);
            var e=Event.current;
            GUI.Label(new Rect(panel.x+24,panel.y+16,panel.width-410,36),PlayerPresentation?"MIRA // FORSCHUNG":"MIRA // FORSCHUNGSLABOR",ui.Heading);
            GUI.Label(new Rect(panel.x+24,panel.y+54,panel.width-48,36),PlayerPresentation?
                "KAPITEL 1   /   "+Simulation.Catalog.trees[category].title+"   /   Mira Stufe "+Simulation.MiraLevel:
                "DEBUG-SIMULATION · 11 Bereiche / 159 Referenzen · Zeiten, Bedingungen und Boni sind Testwerte",ui.Small);
            bool originalEnabled=GUI.enabled;
            GUI.enabled=originalEnabled&&!resetPrompt;
            float x=panel.x+24,y=panel.y+101;
            if(!PlayerPresentation){
            foreach(float speed in new[]{0f,1f,5f,30f,120f,600f}){
                if(GUI.Button(new Rect(x,y,82,32),speed==0?"Pause":speed+"×",Simulation.Speed==speed?ui.Primary:ui.Button))Simulation.Speed=speed;
                x+=88;
            }
            if(GUI.Button(new Rect(x,y,145,32),"Parallel: "+Simulation.Slots,ui.Button))Simulation.Slots=Simulation.Slots%3+1;
            x+=151;
            if(GUI.Button(new Rect(x,y,158,32),Simulation.Powered?"Versorgung: AN":"Versorgung: AUS",ui.Button))Simulation.Powered=!Simulation.Powered;
            y+=40;
            GUI.Label(new Rect(panel.x+24,y,750,32),"Laborzeit "+((int)(Simulation.Seconds/3600)).ToString("00")+":"+TimeSpan.FromSeconds(Simulation.Seconds).ToString(@"mm\:ss")+"  |  Mira Stufe "+Simulation.MiraLevel+"  |  Abschlüsse "+Simulation.Completions+"  |  Aufträge "+Simulation.Queue.Count,ui.Small);
            if(GUI.Button(new Rect(panel.xMax-324,y,90,30),"Sichern",ui.Button))Safe(()=>{File.WriteAllText(SavePath,Simulation.Export());message="Laborstand gespeichert.";});
            if(GUI.Button(new Rect(panel.xMax-228,y,90,30),"Laden",ui.Button))Safe(()=>{Simulation.Import(File.ReadAllText(SavePath));message="Laborstand geladen, Tempo pausiert.";});
            if(GUI.Button(new Rect(panel.xMax-132,y,108,30),"Neustart",ui.Button))resetPrompt=true;
            } else {
                string active=Simulation.Queue.Count==0?"Mira ist bereit · Kein Forschungsauftrag":
                    "Forschung: "+Simulation.Definitions[Simulation.Queue[0]].name+" · "+(100*Simulation.State(Simulation.Queue[0]).work/Simulation.Duration(Simulation.Queue[0])).ToString("0")+" %";
                GUI.Label(new Rect(panel.x+252,y,panel.width-276,28),active,ui.Small);
            }
            float top=PlayerPresentation?y+40:y+43,bottom=panel.yMax-70;
            float itemHeight=(bottom-top)/Simulation.Catalog.trees.Length;
            for(int i=0;i<Simulation.Catalog.trees.Length;i++){
                var tree=Simulation.Catalog.trees[i];
                int complete=tree.nodes.Count(n=>Simulation.State(n.id).level>0);
                var style=new GUIStyle(i==category?ui.Primary:ui.Button){fontSize=ui.Small.fontSize,padding=new RectOffset(7,7,3,3)};
                if(GUI.Button(new Rect(panel.x+20,top+i*itemHeight,215,itemHeight-4),tree.title+"  "+complete+"/"+tree.nodes.Length,style))ChooseCategory(i);
            }
            float sidebarWidth=410;
            infoRect=new Rect(panel.xMax-sidebarWidth-20,top,sidebarWidth,bottom-top);
            var field=new Rect(panel.x+252,top,infoRect.x-panel.x-270,bottom-top);
            lastField=field;
            DrawTree(field);
            if(GUI.Button(new Rect(field.x,panel.yMax-60,140,30),"1:1 Ansicht",new GUIStyle(ui.Button){fontSize=14,padding=new RectOffset(5,5,4,4)}))view.Reset();
            GUI.Label(new Rect(field.x+139,panel.yMax-58,55,27),"Zoom",ui.Small);
            float zoom=GUI.HorizontalSlider(new Rect(field.x+192,panel.yMax-48,230,18),view.Zoom,0,1,ui.Track,ui.Thumb);
            if(!Mathf.Approximately(zoom,view.Zoom))view.SetZoom(zoom,field);
            GUI.Label(new Rect(field.x+438,panel.yMax-58,Mathf.Max(0,field.width-438),27),"Ziehen: Ansicht verschieben",ui.Small);
            GUI.Label(new Rect(panel.x+24,panel.yMax-28,panel.width-48,24),message,ui.Small);
            GUI.enabled=originalEnabled;
            DrawInfo(panel);
            if(resetPrompt)DrawReset(panel);
        }
        private void DrawReset(Rect panel)
        {
            var r=new Rect(panel.center.x-220,panel.center.y-100,440,200);
            SpaceMinerUi.Fill(r,new Color(.025f,.055f,.09f));ui.Panel(r);
            GUI.Label(new Rect(r.x+20,r.y+20,400,80),"Laborfortschritt zurücksetzen? Der echte Spielstand bleibt erhalten.",ui.Text);
            if(GUI.Button(new Rect(r.x+20,r.y+120,190,45),"Zurücksetzen",ui.Button)){Simulation.Reset();resetPrompt=false;selected=null;}
            if(GUI.Button(new Rect(r.x+230,r.y+120,190,45),"Abbrechen",ui.Button))resetPrompt=false;
        }
        private void Safe(Action action){try{action();}catch(Exception e){message="Labor: "+e.Message;}}
        private void DrawTree(Rect viewport)
        {
            view.StretchTo(viewport);
            var e=Event.current;
            dragControl=GUIUtility.GetControlID("ResearchTreePan".GetHashCode(),FocusType.Passive,viewport);
            if(GUI.enabled){
                if(e.type==EventType.ScrollWheel&&viewport.Contains(e.mousePosition)){
                    view.SetZoom(view.Zoom-e.delta.y*.08f,viewport);e.Use();
                }
                if(e.type==EventType.MouseDown&&e.button==0&&viewport.Contains(e.mousePosition)){
                    GUIUtility.hotControl=dragControl;view.Press(e.mousePosition,viewport);e.Use();
                } else if(e.type==EventType.MouseDrag&&GUIUtility.hotControl==dragControl){
                    view.Drag(e.mousePosition,viewport);e.Use();
                } else if(e.type==EventType.MouseUp&&GUIUtility.hotControl==dragControl){
                    selected=view.Release(e.mousePosition,viewport);GUIUtility.hotControl=0;infoScroll=Vector2.zero;showNotes=false;e.Use();
                }
            }
            GUI.BeginGroup(viewport);
            var local=new Rect(0,0,viewport.width,viewport.height);
            float scale=view.Scale(local);
            foreach(var edge in view.Edges){
                Color color=Available(edge.target)?SpaceMinerUi.Cyan:new Color(.19f,.23f,.27f);
                for(int i=1;i<edge.points.Length;i++)Line(view.Point(edge.points[i-1],local),view.Point(edge.points[i],local),color);
                Dock(view.Point(edge.points[0],local),color,5);Dock(view.Point(edge.points[edge.points.Length-1],local),color,5);
            }
            var scenario=UnityEngine.Object.FindFirstObjectByType<WaterScenario>();
            foreach(var n in Simulation.Catalog.trees[category].nodes){
                var r=view.NodeRect(n.id,local);var p=Simulation.State(n.id);
                bool available=Available(n.id);
                Color state=available?SpaceMinerUi.Cyan:new Color(.28f,.31f,.34f);
                SpaceMinerUi.Fill(r,new Color(.025f,.075f,.11f));
                SpaceMinerUi.Border(r,state);
                if(selected==n.id)SpaceMinerUi.Border(new Rect(r.x-2,r.y-2,r.width+4,r.height+4),SpaceMinerUi.Amber);
                var previousColor=GUI.color;GUI.color=available?Color.white:new Color(.34f,.34f,.34f,1);
                miniatures.Draw(new Rect(r.x+2,r.y+2,r.width-4,r.height-4),n.id);GUI.color=previousColor;
                float progress=n.id=="drohnen-01"&&scenario!=null?scenario.Mining.Progress:Mathf.Clamp01(p.work/Simulation.Duration(n.id));
                if(progress>0){
                    ProgressBorder(r,progress,Mathf.Max(2,3*scale));
                }
            }
            GUI.EndGroup();
        }
        private void DrawInfo(Rect panel)
        {
            SpaceMinerUi.Fill(infoRect,new Color(.018f,.045f,.075f,1));ui.Panel(infoRect);
            if(selected==null){GUI.Label(new Rect(infoRect.x+20,infoRect.y+24,infoRect.width-40,40),"TECHNOLOGIE-DETAILS",ui.Text);GUI.Label(new Rect(infoRect.x+20,infoRect.y+80,infoRect.width-40,100),"Wähle links eine Technologie. Hier erscheinen Voraussetzungen, Fortschritt und weitere Informationen.",ui.Small);return;}
            var n=Simulation.Definitions[selected];var p=Simulation.State(selected);
            float width=infoRect.width,height=infoRect.height;
            Rect close=new Rect(infoRect.xMax-43,infoRect.y+12,28,28);
            if(GUI.Button(close,GUIContent.none,ui.Button)){selected=null;return;}
            icons.Draw(new Rect(close.x+7,close.y+7,14,14),"close",SpaceMinerUi.Cyan);
            GUILayout.BeginArea(new Rect(infoRect.x+18,infoRect.y+44,width-36,height-62));
            infoScroll=GUILayout.BeginScrollView(infoScroll,false,false,GUIStyle.none,GUI.skin.verticalScrollbar);
            Rect picture=GUILayoutUtility.GetRect(110,110,GUILayout.ExpandWidth(false));miniatures.Draw(picture,selected);
            GUILayout.Label(n.name,ui.Heading);
            if(PlayerPresentation&&selected=="drohnen-01")DrawMining();
            bool queued=Simulation.Queue.Contains(Simulation.Canonical(selected));
            GUILayout.Label((queued?"AUFTRAG AKTIV · ":"")+(n.known?"STARTWISSEN · ":"")+(p.level>0?"Entwickelt · Stufe "+p.level:"Noch nicht entwickelt"),ui.Text);
            GUILayout.Space(10);
            GUILayout.Label("Dauer: "+(Simulation.Duration(selected)/60).ToString("0.0")+(PlayerPresentation?" Minuten":" Simulationsminuten")+"  ·  Fortschritt: "+(100*p.work/Simulation.Duration(selected)).ToString("0")+" %",ui.Small);
            if(n.upgrade)GUILayout.Label(PlayerPresentation?"Durch weitere Forschung verbesserbar · Der gelbe Rand zeigt den Fortschritt zur nächsten Stufe.":"Verbesserbar bis Stufe 5 · +"+Mathf.Max(0,p.level-1)*10+" % Modellleistung (Testwert).",ui.Small);
            GUILayout.Space(10);
            foreach(string parent in n.parents)GUILayout.Label((Simulation.State(parent).level>0?"✓ ":"• ")+Simulation.Definitions[parent].name,ui.Small);
            if(PlayerPresentation){
                GUILayout.Label((p.evidence?"✓ ":"• ")+"Daten / Probe / Praxiserfahrung",ui.Small);
                GUILayout.Label((p.hardware?"✓ ":"• ")+"Geeignete Arbeitsmittel",ui.Small);
            } else {
                p.evidence=GUILayout.Toggle(p.evidence,"Daten / Probe / Praxis bereit [Debug]",ui.ToggleStyle);
                p.hardware=GUILayout.Toggle(p.hardware,"Arbeitsmittel bereit [Debug]",ui.ToggleStyle);
            }
            string blocker=Simulation.Blocker(selected);
            if(PlayerPresentation&&blocker!=null)blocker=blocker.Replace(" (Testschwelle)","").Replace("Maximale Simulationsstufe","Maximale Forschungsstufe");
            GUILayout.Label(blocker??"Bereit für Mira",ui.Small);
            bool enabled=GUI.enabled;GUI.enabled=enabled&&(queued||blocker==null);
            if(GUILayout.Button(queued?"Auftrag abbrechen · Fortschritt behalten":p.level>0?"Verbesserung einreihen":"Forschung einreihen",ui.Button)){
                if(queued)Simulation.Queue.Remove(Simulation.Canonical(selected));else Simulation.Enqueue(selected);
            }
            GUI.enabled=enabled;
            GUILayout.Space(10);
            if(!PlayerPresentation&&GUILayout.Button(showNotes?"Details ausblenden":"Weitere Bedingungen und Hinweise",ui.Button))showNotes=!showNotes;
            if(PlayerPresentation||showNotes){
                var tree=Simulation.Catalog.trees.First(t=>t.nodes.Any(item=>item.id==selected));
                GUILayout.Label(tree.requirements.Replace("\\n","\n")+"\n\n"+tree.note,ui.Small);
                GUILayout.Label(PlayerPresentation?"Erforschtes Wissen und der anschließende Bau oder Umbau sind separate Schritte.":"Wissen, Bau/Umbau und echte Versorgung bleiben separate Schritte. Laborwerte verändern keine realen Anlagen.",ui.Small);
            }
            if(Simulation.Queue.Count>0){
                GUILayout.Space(10);GUILayout.Label("Warteschlange",ui.Text);
                foreach(string id in Simulation.Queue.ToArray())if(GUILayout.Button(Simulation.Definitions[id].name,ui.Button)){
                    int target=Array.FindIndex(Simulation.Catalog.trees,t=>t.nodes.Any(item=>item.id==id));
                    ChooseCategory(target);selected=id;
                }
            }
            GUILayout.EndScrollView();GUILayout.EndArea();
        }
        private static void Line(Vector2 a,Vector2 b,Color c)
        {SpaceMinerUi.Fill(new Rect(Mathf.Min(a.x,b.x),Mathf.Min(a.y,b.y),Mathf.Max(1.5f,Mathf.Abs(a.x-b.x)),Mathf.Max(1.5f,Mathf.Abs(a.y-b.y))),c);}
        private static void Dock(Vector2 p,Color c,float size)
        {var r=new Rect(p.x-size/2,p.y-size/2,size,size);SpaceMinerUi.Fill(r,new Color(.025f,.055f,.09f));SpaceMinerUi.Border(r,c);}
        private static void ProgressBorder(Rect r,float progress,float thickness)
        {
            float remaining=2*(r.width+r.height)*progress;
            float segment=Mathf.Min(remaining,r.width);if(segment>0)SpaceMinerUi.Fill(new Rect(r.x,r.y,segment,thickness),SpaceMinerUi.Amber);remaining-=segment;
            segment=Mathf.Min(remaining,r.height);if(segment>0)SpaceMinerUi.Fill(new Rect(r.xMax-thickness,r.y,thickness,segment),SpaceMinerUi.Amber);remaining-=segment;
            segment=Mathf.Min(remaining,r.width);if(segment>0)SpaceMinerUi.Fill(new Rect(r.xMax-segment,r.yMax-thickness,segment,thickness),SpaceMinerUi.Amber);remaining-=segment;
            if(remaining>0)SpaceMinerUi.Fill(new Rect(r.x,r.yMax-remaining,thickness,remaining),SpaceMinerUi.Amber);
        }
        internal bool Available(string id)=>Simulation.State(id).level>0||Simulation.Blocker(id)==null;
        private void DrawMining()
        {
            var scenario=UnityEngine.Object.FindFirstObjectByType<WaterScenario>();if(scenario==null)return;
            GUILayout.Label("WASSERABBAU · PRAXISWISSEN",ui.Text);
            GUILayout.Label("Wissenslevel "+scenario.Mining.Level+" · "+scenario.Mining.TripsIntoLevel+" / "+scenario.Mining.TripsPerLevel+" beendete Fahrten\nFörderrate: "+scenario.Worker.EffectiveMiningRate.ToString("0.000")+" kg/s\nAbbauleistung: "+scenario.Worker.EffectiveMiningPower.ToString("0.0")+" kW",ui.Small);
            GUILayout.Label("Lernschwerpunkt für den nächsten Level",ui.Small);
            if(GUILayout.Button((scenario.Mining.Focus==MiningFocus.Throughput?"✓ ":"")+"Förderrate",ui.Button))scenario.Mining.Focus=MiningFocus.Throughput;
            if(GUILayout.Button((scenario.Mining.Focus==MiningFocus.Efficiency?"✓ ":"")+"Energieeffizienz",ui.Button))scenario.Mining.Focus=MiningFocus.Efficiency;
            GUILayout.Label("Nächster Level: +"+(scenario.Mining.ImprovementPerLevel*100).ToString("0")+" % Förderrate oder weniger Abbauleistung. Gelerntes bleibt bei Schwerpunktwechsel erhalten.",ui.Small);
        }
    }
}
