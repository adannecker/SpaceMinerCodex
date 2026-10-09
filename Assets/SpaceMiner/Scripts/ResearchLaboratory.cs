#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SpaceMiner
{
    public sealed class ResearchLaboratory : IDisposable
    {
        public readonly ResearchSimulation Simulation;
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
            ui.Configure(SettingsStore.Current.Accessibility);
            SpaceMinerUi.Fill(panel,new Color(.018f,.035f,.06f));ui.Panel(panel);
            var e=Event.current;
            if(selected!=null&&!resetPrompt&&e.type==EventType.MouseDown&&!infoRect.Contains(e.mousePosition)){
                selected=null;e.Use();
            }
            GUI.Label(new Rect(panel.x+24,panel.y+16,1100,36),"MIRA // FORSCHUNGSLABOR",ui.Heading);
            GUI.Label(new Rect(panel.x+24,panel.y+54,1150,36),"DEBUG-SIMULATION · 11 Bereiche / 159 Referenzen · Zeiten, Bedingungen und Boni sind Testwerte",ui.Small);
            bool originalEnabled=GUI.enabled;
            GUI.enabled=originalEnabled&&!resetPrompt&&selected==null;
            float x=panel.x+24,y=panel.y+101;
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
            float top=y+43,bottom=panel.yMax-70;
            float itemHeight=(bottom-top)/Simulation.Catalog.trees.Length;
            for(int i=0;i<Simulation.Catalog.trees.Length;i++){
                var tree=Simulation.Catalog.trees[i];
                int complete=tree.nodes.Count(n=>Simulation.State(n.id).level>0);
                var style=new GUIStyle(i==category?ui.Primary:ui.Button){fontSize=ui.Small.fontSize,padding=new RectOffset(7,7,3,3)};
                if(GUI.Button(new Rect(panel.x+20,top+i*itemHeight,215,itemHeight-4),tree.title+"  "+complete+"/"+tree.nodes.Length,style))ChooseCategory(i);
            }
            var field=new Rect(panel.x+252,top,panel.width-276,bottom-top);
            lastField=field;
            DrawTree(field);
            if(GUI.Button(new Rect(field.x,panel.yMax-60,140,30),"1:1 Ansicht",new GUIStyle(ui.Button){fontSize=14,padding=new RectOffset(5,5,4,4)}))view.Reset();
            GUI.Label(new Rect(field.x+139,panel.yMax-58,55,27),"Zoom",ui.Small);
            float zoom=GUI.HorizontalSlider(new Rect(field.x+192,panel.yMax-48,230,18),view.Zoom,0,1,ui.Track,ui.Thumb);
            if(!Mathf.Approximately(zoom,view.Zoom))view.SetZoom(zoom,field);
            GUI.Label(new Rect(field.x+438,panel.yMax-58,450,27),"Einheitliche Icons · grosse Bäume frei verschieben",ui.Small);
            GUI.Label(new Rect(panel.x+24,panel.yMax-28,panel.width-48,24),message,ui.Small);
            GUI.enabled=originalEnabled;
            if(selected!=null)DrawInfo(panel);
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
            if(GUI.enabled&&selected==null){
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
                Color color=Simulation.State(edge.target).level>0?SpaceMinerUi.Cyan:new Color(.32f,.65f,.77f);
                for(int i=1;i<edge.points.Length;i++)Line(view.Point(edge.points[i-1],local),view.Point(edge.points[i],local),color);
                Dock(view.Point(edge.points[0],local),color,5);Dock(view.Point(edge.points[edge.points.Length-1],local),color,5);
            }
            foreach(var n in Simulation.Catalog.trees[category].nodes){
                var r=view.NodeRect(n.id,local);var p=Simulation.State(n.id);
                bool queued=Simulation.Queue.Contains(Simulation.Canonical(n.id));
                Color state=queued?SpaceMinerUi.Amber:p.level>0?SpaceMinerUi.Cyan:Simulation.Blocker(n.id)==null?SpaceMinerUi.Cyan:SpaceMinerUi.Disabled;
                SpaceMinerUi.Fill(r,new Color(.025f,.075f,.11f));
                SpaceMinerUi.Border(r,state);
                if(selected==n.id)SpaceMinerUi.Border(new Rect(r.x-2,r.y-2,r.width+4,r.height+4),SpaceMinerUi.Amber);
                miniatures.Draw(new Rect(r.x+2,r.y+2,r.width-4,r.height-4),n.id);
                if(queued){
                    float progress=Mathf.Clamp01(p.work/Simulation.Duration(n.id));
                    SpaceMinerUi.Fill(new Rect(r.x,r.y,Mathf.Max(2,r.width*progress),Mathf.Max(2,3*scale)),SpaceMinerUi.Amber);
                }
            }
            GUI.EndGroup();
        }
        private void DrawInfo(Rect panel)
        {
            var n=Simulation.Definitions[selected];var p=Simulation.State(selected);
            var node=view.Nodes.ContainsKey(selected)?view.NodeRect(selected,lastField):new Rect(lastField.center,Vector2.zero);
            float width=460,height=Mathf.Min(508,panel.height-152);
            float x=node.xMax+18;if(x+width>panel.xMax-20)x=node.x-width-18;
            infoRect=new Rect(Mathf.Clamp(x,panel.x+242,panel.xMax-width-20),Mathf.Clamp(node.y,panel.y+145,panel.yMax-height-20),width,height);
            SpaceMinerUi.Fill(infoRect,new Color(.018f,.045f,.075f,1));ui.Panel(infoRect);
            Rect close=new Rect(infoRect.xMax-43,infoRect.y+12,28,28);
            if(GUI.Button(close,GUIContent.none,ui.Button)){selected=null;return;}
            icons.Draw(new Rect(close.x+7,close.y+7,14,14),"close",SpaceMinerUi.Cyan);
            GUILayout.BeginArea(new Rect(infoRect.x+18,infoRect.y+44,width-36,height-62));
            // Overlay content can be wheeled; the tree itself never scrolls vertically.
            infoScroll=GUILayout.BeginScrollView(infoScroll,false,false,GUIStyle.none,GUIStyle.none);
            Rect picture=GUILayoutUtility.GetRect(110,110,GUILayout.ExpandWidth(false));miniatures.Draw(picture,selected);
            GUILayout.Label(n.name,ui.Heading);
            bool queued=Simulation.Queue.Contains(Simulation.Canonical(selected));
            GUILayout.Label((queued?"AUFTRAG AKTIV · ":"")+(n.known?"STARTWISSEN · ":"")+(p.level>0?"Entwickelt · Stufe "+p.level:"Noch nicht entwickelt"),ui.Text);
            GUILayout.Space(10);
            GUILayout.Label("Dauer: "+(Simulation.Duration(selected)/60).ToString("0.0")+" Simulationsminuten  ·  Fortschritt: "+(100*p.work/Simulation.Duration(selected)).ToString("0")+" %",ui.Small);
            if(n.upgrade)GUILayout.Label("Verbesserbar bis Stufe 5 · +"+Mathf.Max(0,p.level-1)*10+" % Modellleistung (Testwert).",ui.Small);
            GUILayout.Space(10);
            foreach(string parent in n.parents)GUILayout.Label((Simulation.State(parent).level>0?"✓ ":"• ")+Simulation.Definitions[parent].name,ui.Small);
            p.evidence=GUILayout.Toggle(p.evidence,"Daten / Probe / Praxis bereit [Debug]",ui.ToggleStyle);
            p.hardware=GUILayout.Toggle(p.hardware,"Arbeitsmittel bereit [Debug]",ui.ToggleStyle);
            string blocker=Simulation.Blocker(selected);GUILayout.Label(blocker??"Bereit für Mira",ui.Small);
            bool enabled=GUI.enabled;GUI.enabled=enabled&&(queued||blocker==null);
            if(GUILayout.Button(queued?"Auftrag abbrechen · Fortschritt behalten":p.level>0?"Verbesserung einreihen":"Forschung einreihen",ui.Button)){
                if(queued)Simulation.Queue.Remove(Simulation.Canonical(selected));else Simulation.Enqueue(selected);
            }
            GUI.enabled=enabled;
            GUILayout.Space(10);
            if(GUILayout.Button(showNotes?"Details ausblenden":"Weitere Bedingungen und Hinweise",ui.Button))showNotes=!showNotes;
            if(showNotes){
                var tree=Simulation.Catalog.trees.First(t=>t.nodes.Any(item=>item.id==selected));
                GUILayout.Label(tree.requirements.Replace("\\n","\n")+"\n\n"+tree.note,ui.Small);
                GUILayout.Label("Wissen, Bau/Umbau und echte Versorgung bleiben separate Schritte. Laborwerte verändern keine realen Anlagen.",ui.Small);
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
    }
}
#endif
