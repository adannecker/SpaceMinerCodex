using System;
using UnityEngine;

namespace SpaceMiner
{
    public sealed partial class StationOperations
    {
        private readonly SpaceMinerUi ui=new SpaceMinerUi();
        private StationTerminalKind? panel;
        private string selected="command",draggedOrder,category="Alle";
        private Vector2 panelScroll,orderScroll,selectionScroll;
        private bool tasksOpen=true;
        private int preferredDrone;
        private static int consumedFrame=-1;
        public static bool BlocksInput=>Current!=null&&Current.Live&&(Current.panel.HasValue||consumedFrame==Time.frameCount);
        public bool PanelOpen=>panel.HasValue;
        public static bool TacticalViewActive=>Current!=null&&Current.Live&&!StationInteriorMode.IsInside;
        public bool DroneConsoleCanCommand=>Live&&StationInteriorMode.IsInside&&panel==StationTerminalKind.Drones&&!ForeignModal&&Scenario.Scanner.Dialogue==null;
        private float ResourceRow=>40*SettingsStore.Current.Accessibility.TextScale+10;
        private Rect ResourcesRect=>new Rect(Mathf.Max(18,(UiLayout.Width-880)/2),12,Mathf.Min(880,UiLayout.Width-160),ResourceRow*2+12);
        private Rect TasksRect=>new Rect(18,ResourcesRect.yMax+12,285,tasksOpen?Mathf.Clamp(UiLayout.Height-ResourcesRect.yMax-270,100,278):42);
        private Rect SelectionRect=>new Rect(UiLayout.Width-306,TasksRect.y,288,Mathf.Min(380,UiLayout.Height-TasksRect.y-170));
        private Rect ClockRect=>new Rect(UiLayout.Width-306,UiLayout.Height-142,288,78);
        private Rect OutsideVitalsRect
        {
            get{float height=Mathf.Max(168,6*Mathf.Ceil(17*SettingsStore.Current.Accessibility.TextScale*1.3f)+24);return new Rect(18,UiLayout.Height-66-height,285,height);}
        }
        internal Rect HudResourceBounds=>ResourcesRect;
        internal Rect HudTasksBounds=>TasksRect;
        public static bool OwnsScreenPoint(Vector3 mouse)
        {
            if(Current==null||!Current.Live)return false;var p=UiLayout.Point(mouse);
            if(Camera.main!=null&&!Camera.main.GetComponent<OrbitCamera>().ShowHud&&!Current.PanelOpen)return CommandRoomPlanner.BlocksInput;
            if(CommandRoomPlanner.BlocksInput||Current.PanelOpen||Current.ResourcesRect.Contains(p)||Current.ClockRect.Contains(p))return true;
            return StationInteriorMode.IsInside?Current.TasksRect.Contains(p)||Current.SelectionRect.Contains(p)
                :Current.TacticalOwnsScreenPoint(p)||Current.OutsideVitalsRect.Contains(p)||Current.OwnsDroneMarker(p)||Current.OwnsDebrisMarker(p);
        }
        public StationTerminal NearestTerminal(Vector3 feet)
        {StationTerminal best=null;float nearest=2.4f;foreach(var t in terminals){float distance=Vector3.Distance(t.transform.position,feet);if(distance>=nearest)continue;if(Physics.Linecast(feet+Vector3.up*1.6f,t.transform.position+Vector3.up*1.15f,out var hit,1<<25,QueryTriggerInteraction.Ignore)&&!hit.transform.IsChildOf(t.transform))continue;best=t;nearest=distance;}return best;}
        internal StationTerminal ReachableTerminal(Vector3 feet,StationFirstPersonHands hands,Camera view,out StationConsoleTouchSurface surface,out float score)
        {
            StationTerminal best=null;surface=null;score=float.PositiveInfinity;
            foreach(var terminal in terminals)
            {
                if(terminal==null||Vector3.Distance(terminal.transform.position,feet)>2.4f)continue;
                var candidate=StationConsoleTouchSurface.Find(terminal.transform);
                if(!hands.CanReachConsole(candidate))continue;
                float next=candidate.ViewScore(view);
                if(next>=score)continue;best=terminal;surface=candidate;score=next;
            }
            return best;
        }
        public string InteractionPrompt(Vector3 feet)
        {
            var mode=StationInteriorMode.Current;
            if(mode!=null&&Vector3.Distance(feet,mode.FeetPosition)<.01f&&mode.TrySelectConsole(out var chosen,out _))
                return chosen==null?"":"E – "+chosen.label+" berühren";
            var t=NearestTerminal(feet);if(t==null)return "";var hands=mode?.Hands;
            return hands!=null&&hands.CanReachConsole(StationConsoleTouchSurface.Find(t.transform))?"E – "+t.label+" berühren":t.label+" · näher herantreten und ansehen";
        }
        public void OpenTerminal(StationTerminal terminal)
        {if(terminal==null||CommandRoomPlanner.BlocksInput||TacticalViewActive)return;StationInteriorMode.Current?.Hands?.CancelTouch();panel=terminal.kind;if(!string.IsNullOrEmpty(terminal.target))selected=terminal.target;panelScroll=Vector2.zero;consumedFrame=Time.frameCount;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        public void OpenDroneConsole()
        {if(!StationInteriorMode.IsInside||ForeignModal)return;StationInteriorMode.Current?.Hands?.CancelTouch();panel=StationTerminalKind.Drones;panelScroll=Vector2.zero;consumedFrame=Time.frameCount;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        public void ClosePanel(){ResetDroneConsoleTextInput();DisposePowerModels();panel=null;consumedFrame=Time.frameCount;draggedOrder=null;}
        internal string ElementAtHit(RaycastHit hit)
        {
            Vector3 p=Layout.transform.InverseTransformPoint(hit.point);float radius=new Vector2(p.x,p.z).magnitude;
            if(SolarPanelA!=null&&hit.transform.IsChildOf(SolarPanelA))return "solar-a";
            if(SolarPanelB!=null&&hit.transform.IsChildOf(SolarPanelB))return "solar-b";
            for(var t=hit.transform;t!=null&&t!=Layout.transform.parent;t=t.parent){
                if(t.name=="Solar structural boom 0")return "solar-b";
                if(t.name=="Solar structural boom 1")return "solar-a";
            }
            if(radius<4.5f&&hit.transform.IsChildOf(Layout.transform))return "reactor";
            if(radius>37.5f&&radius<39&&p.y>=0&&p.y<4.5f){float angle=Mathf.Atan2(p.x,p.z)*Mathf.Rad2Deg;foreach(var e in State.elements)if(e.id.StartsWith("expansion-")&&!e.built&&Mathf.Abs(Mathf.DeltaAngle(angle,ExpansionAngle(e)))<2.7f)return e.id;}
            string id=hit.collider.GetComponentInParent<StationSelectable>()?.elementId;
            return WreckId(id)&&!IsDebrisKnown(id)?null:id;
        }
        private bool ForeignModal=>CommandRoomPlanner.BlocksInput||StartMenu.IsOpen||IntroSequence.BlocksGameplay||MemoryCinematic.IsPlaying||SettingsMenu.IsOpen||SaveGameMenu.IsOpen||QuitMenu.BlocksInput||TechTreeMenu.IsOpen;
        private void Update()
        {
            if(!Live||Scenario==null)return;UpdateInteriorLighting();
            lightClock-=Time.unscaledDeltaTime;if(lightClock<=0){lightClock=.25f;UpdateLights();}
            if(ForeignModal)return;
            if(Scenario.Scanner.Dialogue!=null)return;
            if(panel.HasValue){if(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.E)&&!DroneConsoleTextInputActive)ClosePanel();return;}
            // InteriorMode is the sole owner of E activation. It plays the physical
            // touch first; direct API callers above intentionally remain immediate.
            if(!TacticalTextInputActive&&Input.GetKeyDown(KeyCode.J)){tasksOpen=!tasksOpen;}
            if(Input.GetMouseButtonDown(0)&&!OwnsScreenPoint(Input.mousePosition)&&Cursor.lockState!=CursorLockMode.Locked)
            {
                if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition),out var hit,Camera.main.farClipPlane,~0,QueryTriggerInteraction.Ignore))
                {
                    string id=ElementAtHit(hit);
                    if(id!=null){if(WreckId(id))SelectDebrisContact(id);else selected=id;if(!StationInteriorMode.IsInside)SelectTacticalObject(id);}
                    else if(!StationInteriorMode.IsInside){var asteroid=hit.collider.GetComponentInParent<AsteroidResource>();if(asteroid!=null)SelectTacticalObject("asteroid-"+Array.IndexOf(Scenario.Asteroids,asteroid));}
                }
            }
        }
        private void OnGUI()
        {
            if(!Live||Scenario==null||ForeignModal||Scenario.Scanner.Dialogue!=null)return;
            var orbit=Camera.main.GetComponent<OrbitCamera>();if(!orbit.ShowHud&&!panel.HasValue)return;
            using var scope=new UiLayout.Scope(UiLayout.Scale());ui.Configure(SettingsStore.Current.Accessibility);int previousDepth=GUI.depth;
            try{
                GUI.depth=-100;
                if(!panel.HasValue){
                    DrawResources();
                    if(StationInteriorMode.IsInside){DrawTasks();DrawSelection(SelectionRect,false);}
                    else{DrawTacticalOverview(TacticalOverviewBounds);DrawOutsideVitals();DrawDroneMarkers();DrawDebrisMarkers();}
                    DrawClock();return;
                }
                SpaceMinerUi.Fill(new Rect(0,0,UiLayout.Width,UiLayout.Height),new Color(0,.015f,.025f,.75f));
                bool fleet=panel==StationTerminalKind.Drones||panel==StationTerminalKind.Power;
                float w=fleet?UiLayout.Width-32:Mathf.Min(1160,UiLayout.Width-32),h=fleet?UiLayout.Height-32:Mathf.Min(790,UiLayout.Height-32);
                GUI.Window(140025,new Rect((UiLayout.Width-w)/2,(UiLayout.Height-h)/2,w,h),id=>DrawTerminalWindow(w,h),GUIContent.none,GUIStyle.none);
            }finally{GUI.depth=previousDepth;}
        }
        private void DrawTerminalWindow(float w,float h)
        {
            if(panel==StationTerminalKind.Drones){DrawDroneConsole(w,h);return;}
            if(panel==StationTerminalKind.Power){DrawPowerConsole(w,h);return;}
            var bounds=new Rect(0,0,w,h);ui.Panel(bounds);
            string title=panel==StationTerminalKind.Drones?"DROHNENVERWALTUNG":panel==StationTerminalKind.Planning?"STATIONSPLANUNG":panel==StationTerminalKind.Research?"FORSCHUNG / TECHTREE":panel==StationTerminalKind.Utilities?"SEKTORVERSORGUNG":"FERTIGUNG / LAGER";
            GUI.Label(new Rect(bounds.x+20,bounds.y+14,w-140,38),"STATION // "+title,ui.Heading);
            GUI.Label(new Rect(bounds.x+20,bounds.y+44,w-150,24),Scenario.SimulationRate==0?"Simulation pausiert":SettingsStore.Current.Gameplay.PauseInMenu?"Menüpause aktiv – zum Arbeiten Pult schließen":"Simulation läuft im Hintergrund",ui.Small);
            if(ui.Tab(new Rect(bounds.xMax-106,bounds.y+12,90,34),"E / Zu",false))ClosePanel();
            GUILayout.BeginArea(new Rect(bounds.x+20,bounds.y+60,w*.56f-30,h-80));panelScroll=GUILayout.BeginScrollView(panelScroll);
            switch(panel){case StationTerminalKind.Drones:DrawDrones();break;case StationTerminalKind.Research:DrawResearch();break;case StationTerminalKind.Planning:DrawPlanning();break;case StationTerminalKind.Utilities:DrawUtilities();break;case StationTerminalKind.Production:DrawProduction();break;}
            GUILayout.EndScrollView();GUILayout.EndArea();
            DrawOrders(new Rect(bounds.x+w*.57f,bounds.y+64,w*.43f-20,h-86));
        }
        private void DrawResources()
        {
            Rect r=ResourcesRect;ui.Panel(r);var p=State.power;var i=State.inventory;
            string[] cells={"ERZEUGUNG\n"+p.GenerationKw.ToString("0.00")+" kW","VERBRAUCH\n"+p.ConsumptionKw.ToString("0.00")+" kW","STATIONSAKKU\n"+(100*p.storedKwh/p.capacityKwh).ToString("0")+" %",p.storedKwh.ToString("0.0")+" / "+p.capacityKwh.ToString("0")+" kWh","WASSER "+Scenario.WaterLiters.ToString("0.0")+" L\nROH-EIS "+(IcePlant==null?"—":(RawIceOnStationKg/1000).ToString("0.00")+" t"),"LUFT / O₂-RESERVE\n"+i.oxygen.amount.ToString("0.00")+" kg","NAHRUNG\n"+i.food.amount.ToString("0.0")+" Portionen","C "+i.carbon.amount.ToString("0.0")+" kg\nFe "+i.iron.amount.ToString("0.0")+" kg"};
            float width=(r.width-24)/4;for(int n=0;n<cells.Length;n++)GUI.Label(new Rect(r.x+12+n%4*width,r.y+6+n/4*ResourceRow,width-8,ResourceRow),cells[n],ui.Small);
        }
        private readonly string[] questTitles={"Umgebung scannen","Wasser sichern","Versorgung diagnostizieren","Trümmer bergen","Interne R-01 aktivieren","Kohlenstoff beschaffen","Grundnahrung herstellen","Zweite Drohne aktivieren","Station reparieren / erforschen"};
        private bool QuestDone(int q)=>q==0?Scenario.Scanner.FirstScanComplete:q==1?State.waterSecured:q==2?State.diagnosed:q==3?State.debrisBatches>0:q==4?State.helper:q==5?State.carbonBatches>0:q==6?State.foodProduced||State.orders.Exists(o=>o.kind==StationJobKind.Food&&o.complete&&!o.cancelled):q==7?State.second:State.repairExperience>0;
        public int CurrentQuest {get{for(int i=0;i<questTitles.Length;i++)if(!QuestDone(i))return i;return questTitles.Length-1;}}
        public void ExplainQuest(int q)
        {
            PresentQuestDialogue(q, q >= 0 && q < questTitles.Length && QuestDone(q));
        }
        private void DrawTasks()
        {
            var r=TasksRect;ui.Panel(r);if(GUI.Button(new Rect(r.x+8,r.y+5,r.width-16,31),tasksOpen?"AUFGABEN  ▾  [J]":"AUFGABEN  ▸  [J]",ui.Button))tasksOpen=!tasksOpen;if(!tasksOpen)return;
            int current=CurrentQuest,start=Math.Max(0,current-1);float step=43*SettingsStore.Current.Accessibility.TextScale;for(int row=0;row<5&&start+row<questTitles.Length&&45+(row+1)*step<=r.height;row++){int q=start+row;if(GUI.Button(new Rect(r.x+10,r.y+45+row*step,r.width-20,step-5),(QuestDone(q)?"✓  ":q==current?"→  ":"    ")+questTitles[q],ui.Button))ExplainQuest(q);}
        }
        private void DrawOutsideVitals()
        {
            var v=StationInteriorMode.Current?.Vitals;if(v==null)return;Rect r=OutsideVitalsRect;ui.Panel(r);
            GUI.Label(new Rect(r.x+12,r.y+8,r.width-24,r.height-16),"VITALWERTE\nGesundheit  "+v.Health.ToString("0")+" %\nStamina  "+v.Stamina.ToString("0")+" %\nMüdigkeit  "+v.Fatigue.ToString("0")+" %\nHunger  "+v.Hunger.ToString("0")+" %\nDurst  "+v.Thirst.ToString("0")+" %",ui.Text);
        }
        private void DrawClock()
        {
            Rect r=ClockRect;ui.Panel(r);GUI.Label(new Rect(r.x+10,r.y+5,270,25),Scenario.SimulationRate==0?"PAUSE":"ZEIT  "+Scenario.SimulationRate.ToString("0")+"×",ui.Small);
            float[] rates={0,1,5,20,100};for(int n=0;n<rates.Length;n++)if(ui.Tab(new Rect(r.x+8+n*55,r.y+34,51,31),rates[n]==0?"Ⅱ":rates[n]+"×",Scenario.SimulationRate==rates[n]))Scenario.SetSimulationRate(rates[n]);
        }
        private void DrawDrones()
        {
            GUILayout.Label("FLOTTE / AUSRÜSTUNG",ui.Text);
            for(int i=0;i<Scenario.Drones.Length;i++)
            {
                var drone=Scenario.Drones[i];GUILayout.Label("Drohne "+(i+1).ToString("00")+" · "+State.tools[i]+" · "+(drone.Phase==DronePhase.StationWork?"Arbeitsauftrag läuft":drone.Status)+"\nAkku "+drone.BatteryKwh.ToString("0.0")+" / "+drone.BatteryCapacityKwh+" kWh · Treibwasser "+drone.FuelLiters.ToString("0.0")+" / "+drone.FuelCapacityLiters+" L",ui.Small);
                if(i<2){
                    if(drone.IsReady&&drone.CargoKg>.0001f&&GUILayout.Button("Vorhandene Fracht zur Annahme liefern",ui.Button)){
                        drone.DeliverExistingCargo(out string cargoReason);SetMessage(cargoReason);
                    }
                    GUILayout.Label("Fracht "+drone.CargoKg.ToString("0.0")+" / "+drone.EffectiveCargoCapacityKg.ToString("0.0")+" kg · Box "+(drone.CargoFillFraction*100).ToString("0.0")+" %",ui.Small);
                    if(drone.CargoProfile.VolumeLimited)GUILayout.Label("Volles Ladeziel: ca. "+WaterScenario.Duration(drone.EffectiveCargoCapacityKg/Mathf.Max(.001f,drone.EffectiveMiningRate))+" Schürfen · Rohannahme begrenzt den Auftrag",ui.Small);
                    else if(GUILayout.Button("Neue 90-%-Auslegung am leeren Dock übernehmen",ui.Button)){
                        drone.ApplyNewSizingAtDock(out string reason);Scenario.Notify(reason);
                    }
                }
                if(i<2&&CanUseDrone(i)){
                    if(GUILayout.Button(preferredDrone==i?"✓ Für Außenaufträge gewählt":"Für Außenaufträge wählen",ui.Button))preferredDrone=i;
                    bool before=GUI.enabled;GUI.enabled=drone.IsReady;
                    foreach(var source in Scenario.Asteroids)if(source.CanMineWater&&GUILayout.Button("Wasserauftrag: "+source.Info.DisplayName,ui.Button))Scenario.AssignTankOrder(source,i);
                    GUILayout.BeginHorizontal();foreach(string tool in new[]{"Bohrer","Greifer","Laser"})if(GUILayout.Button(tool,ui.Button))Enqueue(StationJobKind.Refit,i+":"+tool);GUILayout.EndHorizontal();GUI.enabled=before;
                    if(GUILayout.Button("Sicher zurückrufen",ui.Button)){if(drone.HasTankOrder)drone.ReturnToShip();foreach(var order in State.orders)if(order.started&&!order.complete&&order.actorId=="drone-"+i)Cancel(order.id);}
                }
            }
            GUILayout.Label("R-01: "+(State.helper?"einsatzfähig":"noch ausgeschaltet"),ui.Text);
            ActionButton(StationJobKind.ActivateHelper);ActionButton(StationJobKind.ActivateSecond);
            GUILayout.Label("Werkzeugbestand: Bohrer "+State.spareDrills+" / Greifer "+State.spareGrippers+" / Laser "+State.spareLasers,ui.Small);
        }
        private void ActionButton(StationJobKind kind,string target="")
        {var r=StationRecipes.Get(kind);GUILayout.Label(r.Name+" · "+WaterScenario.Duration(r.Seconds)+" · "+r.PowerKw+" kW",ui.Small);if(GUILayout.Button("Auftrag: "+r.Name,ui.Button))Enqueue(kind,target);}
        private void DrawResearch()
        {
            GUILayout.Label("MIRA / KAMPAGNENFORSCHUNG",ui.Text);GUILayout.Label("Vorhandenes Abbauwissen bleibt erhalten. Neue Verfahren schalten konkrete Stationsaktionen frei.",ui.Small);
            GUILayout.Label("Reparaturrolle: "+(State.technologies.Contains("repair")?"erforscht":"offen – R-01 benötigt"),ui.Text);ActionButton(StationJobKind.ResearchRepair);
            GUILayout.Label("Modulmontage: "+(State.technologies.Contains("expansion")?"erforscht":"offen – Reparaturrolle benötigt"),ui.Text);ActionButton(StationJobKind.ResearchExpansion);
            GUILayout.Label("Praktische Reparaturerfahrung: "+State.repairExperience,ui.Small);
            if(GUILayout.Button("Vollständigen Technologiebaum / Archiv öffnen",ui.Button)){var tree=FindFirstObjectByType<TechTreeMenu>();if(tree!=null){ClosePanel();tree.Open();}else SetMessage("Technologiearchiv ist momentan nicht verfügbar.");}
        }
        private void DrawProduction()
        {
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Vorräte / Fertigung",ui.Button))selected="printer";
            if(IcePlant!=null&&GUILayout.Button("Eisannahme / Wasser",ui.Button))selected="ice-plant";
            GUILayout.EndHorizontal();
            if(selected=="ice-plant"){DrawIcePlantPanel();return;}
            GUILayout.Label("VORRÄTE / PRODUKTION",ui.Text);foreach(var c in State.inventory.Containers)GUILayout.Label(c.Definition.Name+": "+c.amount.ToString("0.00")+" / "+c.capacity+" "+c.Definition.Unit,ui.Small);
            foreach(var kind in new[]{StationJobKind.Recycle,StationJobKind.PrintParts,StationJobKind.Food,StationJobKind.Oxygen}){
                var recipe=StationRecipes.Get(kind);string input="";foreach(var r in recipe.Input)input+=StationResources.Get(r.id).Name+" "+r.quantity+" · ";GUILayout.Label(input,ui.Small);ActionButton(kind);
            }
            GUILayout.Label("Nährstoffkartuschen sind endlich. Wasser, Kohlenstoff und Energie allein ersetzen ihren Vorrat noch nicht.",ui.Small);
        }
        private void DrawPlanning()
        {
            if(GUILayout.Button("Raumplanung · 4 Räume [F7]",ui.Button))CommandRoomPlanner.Current?.Open();
            ActionButton(StationJobKind.Diagnose);GUILayout.Label("ELEMENTE / KATEGORIEN",ui.Text);
            GUILayout.BeginHorizontal();foreach(string c in new[]{"Alle","Ringsegment","Lager","Ausbauslot"})if(GUILayout.Button(c,ui.Button))category=c;GUILayout.EndHorizontal();
            foreach(var e in State.elements)if(IsDebrisKnown(e.id)&&(category=="Alle"||e.kind==category))if(GUILayout.Button(e.name+" · "+(e.built?(e.condition*100).ToString("0")+" %":"ungebaut"),ui.Button))selected=e.id;
            DrawElementActions();
        }
        private void DrawUtilities(){DrawElementActions();foreach(var e in State.elements)if(e.id.StartsWith("sector-")&&GUILayout.Button(e.name+" · Strom "+(e.power?"AN":"AUS")+" · Wasser "+(e.water?"offen":"zu"),ui.Button))selected=e.id;}
        private void DrawElementActions()
        {
            if (DrawBatteryBayDetails(selected)) return;
            var e=Element(selected=="ice-plant"?"water":selected);if(e==null)return;if(WreckId(e.id)){DrawDebrisDetails(e);return;}GUILayout.Space(10);GUILayout.Label(e.name+"\n"+e.kind+" / "+e.use+"\nZustand "+(e.condition*100).ToString("0.0")+" %",ui.Text);GUILayout.Label("Sektorstrom: "+(Powered(e.sector)?"verbunden":"unterbrochen")+" · Wasserleitung: "+(WaterConnected(e.sector)?"verbunden":"unterbrochen"),ui.Small);
            if(e.id=="water"&&IcePlant!=null){DrawIcePlantSummary();if(GUILayout.Button("Eisannahme / Aufbereitung öffnen",ui.Button)){panel=StationTerminalKind.Production;selected="ice-plant";}}
            if(e.id.StartsWith("sector-")){if(GUILayout.Button(e.power?"Stromkreis trennen":"Stromkreis verbinden",ui.Button))TogglePower(e.sector);if(GUILayout.Button(e.water?"Wasserventil schließen":"Wasserventil öffnen",ui.Button))ToggleWater(e.sector);}
            if(e.id.StartsWith("debris-"))ActionButton(StationJobKind.Salvage,e.id);else if(e.id.StartsWith("carbon-"))ActionButton(StationJobKind.Carbon,e.id);else if(!e.built){GUILayout.Label(State.technologies.Contains("expansion")?"Bauart freigeschaltet":"Gesperrt: Mira muss Modulmontage erforschen",ui.Small);if(State.technologies.Contains("expansion"))ActionButton(StationJobKind.Expand,e.id);}else ActionButton(StationJobKind.Repair,e.id);
        }
        private void DrawSelection(Rect r,bool unused)
        {
            if(Element(selected)==null&&selected!="ice-plant"&&BatteryBay(selected)==null)return;ui.Panel(r);
            GUILayout.BeginArea(new Rect(r.x+12,r.y+12,r.width-24,r.height-24));selectionScroll=GUILayout.BeginScrollView(selectionScroll);
            var body=Camera.main.GetComponent<OrbitCamera>().Selected;
            var rock=body!=null?body.GetComponent<AsteroidResource>():null;
            var drone=body!=null?body.GetComponent<DroneAgent>():null;
            if(BatteryBay(selected)!=null)DrawBatteryBayDetails(selected);
            else if(drone!=null)DrawDroneDetail(drone);
            else if(rock!=null){
                GUILayout.Label(rock.IsScanned?body.DisplayName:"Unbekannter Kontakt",ui.Text);
                GUILayout.Label(rock.WaterIdentified?"Wasser "+(rock.WaterFraction*100).ToString("0")+" % / Rest unbekannt\nVorrat "+rock.RemainingRawKg.ToString("0")+" kg":"Zusammensetzung unbekannt",ui.Small);
                if(rock.CanMineWater&&GUILayout.Button("Eine Ladung Roh-Eis liefern",ui.Button))Scenario.AssignTankOrder(rock,preferredDrone);
            }else DrawElementActions();
            if(GUILayout.Button("Drohnenpult",ui.Button)){panel=StationTerminalKind.Drones;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
            if(GUILayout.Button("Stationsplanung",ui.Button)){panel=StationTerminalKind.Planning;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
            GUILayout.EndScrollView();GUILayout.EndArea();
        }
        private StationWorkOrder DroneWork(DroneAgent drone)=>State.orders.Find(o=>o.started&&!o.complete&&(o.actorId=="drone-"+Array.IndexOf(Scenario.Drones,drone)||o.kind==StationJobKind.Refit&&o.target.StartsWith(Array.IndexOf(Scenario.Drones,drone)+":")));
        private void DrawDroneDetail(DroneAgent d)
        {
            var work=DroneWork(d);GUILayout.Label(d.Info.DisplayName+" / "+(work==null?d.Status:StationRecipes.Get(work.kind).Name),ui.Text);
            float eta=work==null?d.PhaseSecondsRemaining:StationRecipes.Get(work.kind).Seconds-work.elapsed;
            GUILayout.Label("Akku "+d.BatteryKwh.ToString("0.00")+" / "+d.BatteryCapacityKwh+" kWh\nTreibwasser "+d.FuelLiters.ToString("0.00")+" / "+d.FuelCapacityLiters+" L\nLadung "+d.CargoKg.ToString("0.0")+" / "+d.EffectiveCargoCapacityKg.ToString("0.0")+" kg\nBoxfüllung "+(d.CargoFillFraction*100).ToString("0.0")+" %\nGeschwindigkeit "+d.Speed.ToString("0.00")+" m/s\nZielentfernung "+(work==null?d.TargetDistance:Vector3.Distance(d.transform.position,WorkTarget(work))).ToString("0.0")+" m\nRestdauer ca. "+WaterScenario.Duration(eta),ui.Small);
            if(d.CargoProfile.VolumeLimited)GUILayout.Label("Ladeziel "+(d.CargoProfile.TargetFillFraction*100).ToString("0")+" % · "+d.CargoVolumeM3.ToString("0.00")+" / "+d.CargoProfile.UsableVolumeM3.ToString("0.00")+" m³",ui.Small);
            if(work!=null)GUILayout.Label(work.status+" / "+(100*work.elapsed/StationRecipes.Get(work.kind).Seconds).ToString("0")+" %",ui.Small);
        }
        private bool HudPanelOverlaps(Rect bounds)
        {
            if(bounds.Overlaps(ResourcesRect)||bounds.Overlaps(ClockRect)||(Scenario.Scanner.VirtualView&&bounds.Overlaps(Scenario.Scanner.VirtualToolbarRect)))return true;
            return StationInteriorMode.IsInside?bounds.Overlaps(TasksRect)||bounds.Overlaps(SelectionRect)
                :bounds.Overlaps(TacticalOverviewBounds)||bounds.Overlaps(OutsideVitalsRect);
        }
        private bool DroneMarker(DroneAgent drone,out Rect bounds)
        {
            bounds=default;if(!drone.IsOperational)return false;
            var point=Camera.main.WorldToScreenPoint(drone.transform.position+Vector3.up*3);
            if(point.z<0||point.x<0||point.x>Screen.width||point.y<0||point.y>Screen.height)return false;
            var pos=UiLayout.Point(point);bounds=new Rect(pos.x-95,pos.y-26,190,58);return !HudPanelOverlaps(bounds);
        }
        private bool OwnsDroneMarker(Vector2 point)
        {foreach(var drone in Scenario.Drones)if(DroneMarker(drone,out var bounds)&&bounds.Contains(point))return true;return false;}
        private void DrawDroneMarkers()
        {
            foreach(var d in Scenario.Drones){if(!DroneMarker(d,out var r))continue;
                var job=DroneWork(d);float p=job==null?d.PhaseProgress:job.elapsed/StationRecipes.Get(job.kind).Seconds;ui.Panel(r);
                if(ui.Tab(new Rect(r.x+4,r.y+3,182,43),d.Info.DisplayName+" · "+(100*p).ToString("0")+" %",Camera.main.GetComponent<OrbitCamera>().FollowingTarget==d.transform)){SelectTacticalTab(TacticalOverviewTab.Drones);FocusTacticalDrone("drone-"+Array.IndexOf(Scenario.Drones,d));}
                SpaceMinerUi.Fill(new Rect(r.x+8,r.yMax-8,174*Mathf.Clamp01(p),4),SpaceMinerUi.Cyan);
            }
        }
        private void DrawOrders(Rect r)
        {
            ui.Panel(r);GUILayout.BeginArea(new Rect(r.x+12,r.y+10,r.width-24,r.height-20));GUILayout.Label("ARBEITSAUFTRÄGE / ZIEHEN = PRIORITÄT",ui.Text);GUILayout.Label(Message,ui.Small);orderScroll=GUILayout.BeginScrollView(orderScroll);
            for(int i=0;i<State.orders.Count;i++){
                var order=State.orders[i];var recipe=StationRecipes.Get(order.kind);var row=GUILayoutUtility.GetRect(80,86,GUILayout.ExpandWidth(true));
                string worker=order.actorId=="drone-0"?"Drohne 01":order.actorId=="drone-1"?"Drohne 02":string.IsNullOrEmpty(order.actorId)?"Noch kein Arbeiter zugewiesen":order.actorId;
                SpaceMinerUi.Fill(row,new Color(.05f,.12f,.16f,.9f));GUI.Label(new Rect(row.x+8,row.y+5,row.width-70,74),"≡ "+recipe.Name+"\n"+order.status+" · "+(100*order.elapsed/recipe.Seconds).ToString("0")+" %\n"+worker,ui.Small);
                if(GUI.Button(new Rect(row.xMax-56,row.y+8,48,30),order.complete||order.cancelled?"×":"Stop",ui.Button)){if(order.complete||order.cancelled&&!order.started){if(RemoveFinishedOrder(order.id))i--;}else Cancel(order.id);}
                var evt=Event.current;if(evt.type==EventType.MouseDown&&row.Contains(evt.mousePosition)&&evt.mousePosition.x<row.xMax-65){draggedOrder=order.id;evt.Use();}
                if(evt.type==EventType.MouseUp&&draggedOrder!=null&&row.Contains(evt.mousePosition)){MoveOrder(draggedOrder,i);draggedOrder=null;evt.Use();}
            }
            if(Event.current.type==EventType.MouseUp)draggedOrder=null;GUILayout.EndScrollView();GUILayout.EndArea();
        }
    }
}
