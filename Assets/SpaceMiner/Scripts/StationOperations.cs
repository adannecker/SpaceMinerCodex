using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    public sealed partial class StationOperations : MonoBehaviour
    {
        public static StationOperations Current {get;private set;}
        public StationCampaignState State {get;private set;}=new StationCampaignState();
        public WaterScenario Scenario {get;private set;}
        public StationInteriorLayout Layout {get;private set;}
        public bool Live {get;private set;}
        public StationIcePlant IcePlant {get;private set;}
        private float preparedIcePowerKw;
        public float ChargePerDroneKw {get;private set;}
        public string Message {get;private set;}="Bestand prüfen, Wasser und Energie sichern.";
        private readonly List<StationWorkOrder> running=new List<StationWorkOrder>();
        private readonly HashSet<string> busyActors=new HashSet<string>();
        private readonly StationSupplyNetwork network=new StationSupplyNetwork();
        public static void Install(StationVisual station,StationInteriorLayout layout)
        {
            if(station.GetComponent<StationOperations>()!=null)return;
            var args=Environment.GetCommandLineArgs();
            if(Array.Exists(args,a=>a.Contains("tripo")||a.Contains("Tripo")||a.Contains("constructionPreview")))return;
            var system=station.gameObject.AddComponent<StationOperations>();system.Initialize(station.GetComponent<WaterScenario>(),layout);
        }
        private void Awake(){Current=this;}
        private void Initialize(WaterScenario scenario,StationInteriorLayout layout)
        {
            Scenario=scenario;Scenario.Operations=this;Layout=layout;State.inventory.water=scenario.WaterTank;
            Live=!Array.Exists(Environment.GetCommandLineArgs(),a=>a.EndsWith("Check",StringComparison.OrdinalIgnoreCase)&&a!="-miraQuestGameCheck"&&a!="-stationOperationsCheck"&&a!="-stationOperationsUiCheck"&&a!="-stationPerformanceCheck"&&a!="-stationWorkshopCheck"&&a!="-commandRoomPlannerCheck"&&a!="-stationRoomPlanningCheck"&&a!="-stationDebrisCheck"&&a!="-stationHandsCheck"&&a!="-stationDroneSizingCheck"&&a!="-stationIcePlantCheck"&&a!="-stationDroneConsoleCheck"&&a!="-stationTacticalCheck"&&a!="-stationCameraVerticalCheck"&&a!="-stationBatteryBayCheck"&&a!="-stationPowerConsoleCheck"&&a!="-adaptiveLightingCheck");
            BindIcePlant();
            for(int s=0;s<12;s++)State.elements.Add(new StationElementState("sector-"+s,"Ringsektor S"+(s+1).ToString("00"),"Ringsegment","Durchgang / Versorgungsverteilung",s,RingWalkthrough.DamagedRingSector(s)?.18f:.93f));
            AddElement("command","Kommandozentrale","Modul","Scanner / Drohnen / Forschung / Planung",0,.92f);
            AddElement("hall","Drohnenhalle","Werkstatt","Umbau / Werkzeugbestand",1,.85f);
            AddElement("helper","Interne Werkstatt","Werkstatt","R-01 / Aufbereitung",1,.78f);
            AddElement("water","Wasserlager 01","Lager","Eisannahme / Flüssigtank",7,.87f);
            AddElement("water-1","Wasserlager 02","Lager","Beschädigte zusätzliche Lagerhülle",7,.24f);
            AddElement("water-2","Wasserlager 03","Lager","Schwer beschädigte zusätzliche Lagerhülle",7,.12f);
            for(int axis=0;axis<4;axis++)AddElement("service-"+axis,"Reaktor-Servicegang "+(axis+1),"Servicegang","Zugang zum Wartungskragen",axis*3,axis==0?.91f:.18f);
            AddElement("solar-a","Solarfeld A","Energieanlage","Photovoltaik",3,.65f);
            AddElement("solar-b","Solarfeld B","Energieanlage","Photovoltaik",9,.81f);
            AddElement("reactor","Reaktorkapsel","Energieanlage","Bestandsreaktor / Stromerzeugung",0,.82f);
            AddElement("printer","Materialdrucker","Produktion","Einfache Reparaturteile",0,.8f);
            AddElement("food","Nahrungssynthese","Lebenserhaltung","Grundnahrung mit endlichen Zusatzkartuschen",0,.8f);
            AddElement("battery","Stationsakku","Speicher","24 kWh Energiespeicher / Lastspitzen",0,.9f);
            AddElement("scrap","Schrottannahme","Lager","Analyse / Trennung / Rückgewinnung",1,.9f);
            BuildPresentation();BalancePower(0);
        }
        private void AddElement(string id,string name,string kind,string use,int sector,float condition)=>State.elements.Add(new StationElementState(id,name,kind,use,sector,condition));
        private WorkActor Actor(StationWorkOrder order)=>order.kind==StationJobKind.Repair&&(order.target=="printer"||order.target=="food"||order.target=="battery"||order.target=="scrap")?WorkActor.Internal:StationRecipes.Get(order.kind).Actor;
        public StationElementState Element(string id)=>State.elements.Find(e=>e.id==id);
        public bool Powered(int sector)=>network.Connected(State.elements,0,sector,false);
        public bool WaterConnected(int sector)=>network.Connected(State.elements,7,sector,true);
        public bool IceReceivingAvailable=>Live&&IcePlant!=null&&Powered(7)&&Element("water").condition>=.3f;
        public double RawReceivingCapacity(double waterFraction)=>IceReceivingAvailable?IcePlant.MaxReceivableRawKg(waterFraction):0;
        private void BindIcePlant(){IcePlant=new StationIcePlant(State.icePlant);State.icePlant=IcePlant.State;UpdateIcePlantVisuals(0);}
        public bool CanUseDrone(int index)=>index==0||(index==1&&State.second);
        public bool IsRefitting(int index)=>State.orders.Exists(o=>o.kind==StationJobKind.Refit&&o.started&&!o.complete&&o.target.StartsWith(index+":"));
        private bool SourceAvailable(string target)=>!State.collected.Contains(target)&&!State.orders.Exists(o=>!o.complete&&(!o.cancelled||o.started)&&o.target==target);
        public bool IsAssigned(DroneAgent drone)=>State.orders.Exists(o=>o.started&&!o.complete&&!o.cancelled&&o.actorId=="drone-"+Array.IndexOf(Scenario.Drones,drone));
        public void SetMessage(string message){Message=message;Scenario.Notify(message);}
        public bool TogglePower(int sector){var e=Element("sector-"+sector);if(e==null)return false;e.power=!e.power;RefreshPresentation();UpdateLights();return true;}
        public bool ToggleWater(int sector){var e=Element("sector-"+sector);if(e==null)return false;e.water=!e.water;return true;}
        public bool Enqueue(StationJobKind kind,string target="")
        {
            var recipe=StationRecipes.Get(kind);if(recipe==null)return false;
            if(State.orders.Count>=80){SetMessage("Auftragsliste voll: abgeschlossene Einträge entfernen.");return false;}
            if(kind==StationJobKind.Salvage&&!Scenario.Scanner.FirstScanComplete){SetMessage("Zuerst den Nahbereich scannen: noch keine Wrackkontakte bekannt.");return false;}
            if((kind==StationJobKind.ActivateHelper&&State.helper)||(kind==StationJobKind.ActivateSecond&&State.second)||(kind==StationJobKind.Diagnose&&State.diagnosed))return false;
            if((kind==StationJobKind.ResearchRepair&&State.technologies.Contains("repair"))||(kind==StationJobKind.ResearchExpansion&&State.technologies.Contains("expansion")))return false;
            if(kind==StationJobKind.Salvage&&string.IsNullOrEmpty(target))for(int i=0;i<5;i++)if(SourceAvailable("debris-"+i)){target="debris-"+i;break;}
            if(kind==StationJobKind.Carbon&&string.IsNullOrEmpty(target))for(int i=0;i<6;i++)if(SourceAvailable("carbon-"+i)){target="carbon-"+i;break;}
            if((kind==StationJobKind.Repair||kind==StationJobKind.Expand)&&Element(target)==null)return false;
            if(kind==StationJobKind.Repair&&(!Element(target).built||Element(target).kind=="Bergung"||Element(target).kind=="Rohstoff"))return false;
            if(kind==StationJobKind.Expand&&!target.StartsWith("expansion-"))return false;
            if(kind==StationJobKind.Expand&&Element(target).built)return false;
            if(kind==StationJobKind.Salvage&&(!target.StartsWith("debris-")||!int.TryParse(target.Substring(7),out int salvageIndex)||salvageIndex<0||salvageIndex>=5||State.collected.Contains(target)))return false;
            if(kind==StationJobKind.Carbon&&(!target.StartsWith("carbon-")||!int.TryParse(target.Substring(7),out int carbonIndex)||carbonIndex<0||carbonIndex>=6||State.collected.Contains(target)))return false;
            if(kind==StationJobKind.Salvage&&State.debrisBatches>=5||kind==StationJobKind.Carbon&&State.carbonBatches>=6)return false;
            if(kind!=StationJobKind.PrintParts&&kind!=StationJobKind.Food&&kind!=StationJobKind.Recycle&&kind!=StationJobKind.Oxygen&&State.orders.Exists(o=>!o.complete&&(!o.cancelled||o.started)&&o.kind==kind&&o.target==target))return false;
            var order=new StationWorkOrder(kind,target);if(recipe.Actor==WorkActor.External)order.actorId="drone-"+preferredDrone;
            State.orders.Add(order);SetMessage(recipe.Name+" eingereiht.");return true;
        }
        public bool MoveOrder(string id,int index)
        {var order=State.orders.Find(o=>o.id==id);if(order==null||index<0||index>=State.orders.Count)return false;State.orders.Remove(order);State.orders.Insert(index,order);return true;}
        internal bool RemoveFinishedOrder(string id)
        {
            var order=State.orders.Find(o=>o.id==id);
            if(order==null||!order.complete&&!(order.cancelled&&!order.started)||order.reserved!=null&&order.reserved.Length>0)return false;
            if(order.kind==StationJobKind.Food&&order.complete&&!order.cancelled)State.foodProduced=true;
            return State.orders.Remove(order);
        }
        public void Cancel(string id)
        {
            var order=State.orders.Find(o=>o.id==id);if(order==null||order.complete||order.cancelled)return;
            if(order.started){
                if(order.kind==StationJobKind.Refit){CancelWorkshopRefit(order);RefreshPresentation();return;}
                float duration=StationRecipes.Get(order.kind).Seconds,p=order.elapsed/duration;var actor=Actor(order);
                float trip=actor==WorkActor.External?(p<.4f?p/.4f:p>.7f?(1-p)/.3f:1):(p<.25f?p*4:p>.8f?(1-p)*5:1);
                order.elapsed=actor==WorkActor.External?duration*(1-.3f*trip):actor==WorkActor.Internal?duration*(1-.2f*trip):duration;
                order.status="Rückkehr angefordert / sicherer Abschluss";order.cancelled=true;RefreshPresentation();return;
            }
            order.cancelled=true;order.complete=true;order.status="Abgebrochen";
        }
        private bool Refund(StationWorkOrder order)
        {if(order.reserved==null)return true;if(!State.inventory.CanReceive(order.reserved))return false;State.inventory.Receive(order.reserved);order.reserved=null;return true;}
        private bool Ready(StationWorkOrder order,out string reason)
        {
            var r=StationRecipes.Get(order.kind);var actor=Actor(order);reason="";
            if(order.kind==StationJobKind.Refit){var bits=order.target.Split(':');if(bits.Length!=2||!int.TryParse(bits[0],out int index)||index<0||index>1||!CanUseDrone(index)){reason="Ungültige Drohne";return false;}string tool=bits[1];if(tool!="Bohrer"&&tool!="Greifer"&&tool!="Laser"){reason="Unbekanntes Werkzeug";return false;}if(!Scenario.Drones[index].IsReady||Scenario.Drones[index].HasTankOrder){reason="Drohne nicht im Dock frei";return false;}if(tool=="Laser"&&!State.technologies.Contains("repair")){reason="Reparaturrolle erforschen";return false;}if(State.tools[index]==tool||(tool=="Laser"?State.spareLasers:tool=="Greifer"?State.spareGrippers:State.spareDrills)<=0){reason="Werkzeug bereits montiert / Bestand fehlt";return false;}}
            if(order.kind==StationJobKind.Refit&&!WorkshopRefitReady(order,out reason))return false;
            if(r.Technology!=""&&!State.technologies.Contains(r.Technology)){reason="Forschung: "+(r.Technology=="repair"?"Reparaturrolle":"Modulmontage");return false;}
            if((actor==WorkActor.Internal||order.kind==StationJobKind.ResearchRepair)&&!State.helper){reason="Interne R-01 fehlt";return false;}
            int sector=actor==WorkActor.Internal&&order.kind==StationJobKind.Recycle?1:0;
            if(!Powered(sector)){reason="Sektor stromlos";return false;}
            if((order.kind==StationJobKind.Food||order.kind==StationJobKind.Oxygen)&&(!WaterConnected(0)||!WaterConnected(7))){reason="Wasserleitung abgesperrt";return false;}
            if(order.kind==StationJobKind.Salvage&&!Scenario.Scanner.FirstScanComplete||order.kind==StationJobKind.Carbon&&!State.diagnosed){reason="Scan / Versorgungsdiagnose fehlt";return false;}
            if(order.kind==StationJobKind.ActivateSecond&&(!State.diagnosed||Scenario.WaterLiters<40||State.power.storedKwh<6)){reason="Diagnose / Reserve: 40 L Wasser / 6 kWh Stationsakku";return false;}
            string machine=order.kind==StationJobKind.PrintParts?"printer":order.kind==StationJobKind.Food||order.kind==StationJobKind.Oxygen?"food":order.kind==StationJobKind.Recycle?"scrap":null;
            if(machine!=null&&Element(machine).condition<.3f){reason="Anlage beschädigt: zuerst instand setzen";return false;}
            if(!State.inventory.CanConsume(r.Input)){reason="Material fehlt";return false;}
            if(!State.inventory.CanReceive(r.Output)){reason="Ziellager voll";return false;}
            if(actor==WorkActor.External){
                int wanted=order.actorId=="drone-1"?1:0;var drone=Scenario.Drones[wanted];
                if(!CanUseDrone(wanted)||!drone.IsReady||drone.HasTankOrder||IsRefitting(wanted)||!Powered(1)){reason="Außendrohne / Dock nicht frei";return false;}
                if(order.kind==StationJobKind.Repair&&State.tools[wanted]!="Laser"){reason="Laser-Werkzeug benötigt";return false;}
                if(order.kind==StationJobKind.Salvage&&State.tools[wanted]=="Laser"||order.kind==StationJobKind.Carbon&&State.tools[wanted]!="Bohrer"){reason="Passendes Greif-/Bohrwerkzeug fehlt";return false;}
                order.actorId="drone-"+wanted;
                if((order.flightPath==null||order.flightPath.Length==0)&&!PlanWorkFlight(order,drone,out reason))return false;
            }
            if(actor==WorkActor.Internal)order.actorId="R-01";
            if(actor==WorkActor.Research)order.actorId="Mira";
            if(actor==WorkActor.Manual)order.actorId="Bestandsautomatik / Inbetriebnahme";
            return true;
        }
        public void Prepare(float seconds)
        {
            if(!Live)return;
            State.waterSecured|=Scenario.Deliveries>0&&Scenario.WaterLiters>=40;
            running.Clear();busyActors.Clear();var actors=busyActors;
            foreach(var order in State.orders)
            {
                if(order.complete||order.cancelled&&!order.started)continue;
                if(order.started){running.Add(order);actors.Add(order.actorId);continue;}
                if(!Ready(order,out string why)){order.status="Wartet: "+why;continue;}
                if(actors.Contains(order.actorId)){order.status="Wartet: Arbeitsplatz belegt";continue;}
                var recipe=StationRecipes.Get(order.kind);
                if(Actor(order)==WorkActor.External&&!Scenario.Drones[int.Parse(order.actorId.Substring(6))].StartStationWork()){order.status="Wartet: Drohnenreserve";continue;}
                if(order.kind==StationJobKind.Refit&&!BeginWorkshopRefit(order)){order.status="Wartet: Reserve für Werkstattflug";continue;}
                if(!State.inventory.TryConsume(recipe.Input))continue;
                order.reserved=recipe.Input;order.started=true;order.status="In Arbeit";actors.Add(order.actorId);running.Add(order);
            }
            BalancePower(seconds);
        }
        internal void RefreshSupplyState(){BalancePower(0);UpdateIcePlantVisuals(0);}
        private void BalancePower(float seconds)
        {
            float work=0;foreach(var order in State.orders)if(order.started&&!order.complete&&!order.cancelled&&SupplyAvailable(order))work+=StationRecipes.Get(order.kind).PowerKw;
            int charging=0;foreach(var d in Scenario.Drones)if(Powered(1)&&d.IsOperational&&d.Phase==DronePhase.Servicing&&d.BatteryKwh<d.BatteryCapacityKwh-.001f)charging++;
            float generation=CaptureGeneration().TotalOutputKw;
            float loss=0;foreach(var e in State.elements)if(e.id.StartsWith("sector-")&&e.power&&e.condition<.3f)loss+=.04f;
            preparedIcePowerKw=Live&&IceReceivingAvailable?(float)IcePlant.RequestedPowerKw(Scenario.WaterTank.Free,WaterConnected(0)&&WaterConnected(7)):0;
            work+=preparedIcePowerKw;
            State.power.Advance(seconds,generation,.35f+NonLightingSystemsKw+LightingDemandKw+loss+(Scenario.Scanner.Ready?0:Scenario.Scanner.ChargePowerKw),work,charging*Scenario.DroneChargePowerKw);
            ObservePower(seconds,loss,work,charging*Scenario.DroneChargePowerKw);
            ChargePerDroneKw=charging==0?0:State.power.ChargeAllocationKw/charging;
        }
        public void Advance(float seconds)
        {
            if(!Live)return;State.elapsed+=seconds;
            double needed=IcePlant.RequestedPowerKw(Scenario.WaterTank.Free,WaterConnected(0)&&WaterConnected(7));
            IcePlant.Advance(seconds,IceReceivingAvailable&&State.power.JobPowered&&needed<=preparedIcePowerKw+.00001,
                WaterConnected(0)&&WaterConnected(7),Scenario.WaterTank.Free);
            Scenario.TransferProcessedWater();
            foreach(var order in running)
            {
                var r=StationRecipes.Get(order.kind);
                var actor=Actor(order);int sector=actor==WorkActor.Internal&&order.kind==StationJobKind.Recycle?1:0;
                if(order.kind==StationJobKind.Refit){
                    if(!AdvanceWorkshopRefit(order,seconds))continue;
                    if(order.cancelled){if(!Refund(order)){order.status="Rückgabe wartet auf freien Lagerplatz";continue;}}
                    else{ApplyRefit(order.target);order.reserved=null;}
                    RefitDrone(order).FinishStationWork();order.complete=true;order.status=order.cancelled?"Abgebrochen / sicher zurück am Dock":"Umbau in Platz 01 abgeschlossen / zurück am Dock";
                    RefreshWorkshopVisuals();continue;
                }
                if(!order.cancelled&&(!Powered(sector)||!State.power.JobPowered)){
                    if(actor==WorkActor.External){Cancel(order.id);order.status="Versorgungsausfall: autonome sichere Rückkehr";}
                    else{order.status="Pausiert: Stromversorgung";continue;}
                }
                if(!order.cancelled&&(order.kind==StationJobKind.Food||order.kind==StationJobKind.Oxygen)&&(!WaterConnected(0)||!WaterConnected(7))){order.status="Pausiert: Wasser abgesperrt";continue;}
                float previousElapsed=order.elapsed;
                order.elapsed=Math.Min(r.Seconds,order.elapsed+seconds);order.status=order.cancelled?"Sichere Rückkehr / Materialrückgabe":"In Arbeit";
                if(actor==WorkActor.Internal&&!HelperPathClear(order,previousElapsed,seconds)){order.elapsed=previousElapsed;order.status="Pausiert: interner Transportweg / "+helperObstruction;continue;}
                AnimateOrder(order,seconds);
                if(order.elapsed<r.Seconds)continue;
                if(!order.cancelled&&!State.inventory.CanReceive(r.Output)){order.status="Pausiert: Ziellager voll";continue;}
                if(order.cancelled){if(!Refund(order)){order.status="Rückgabe wartet auf freien Lagerplatz";continue;}if(order.kind==StationJobKind.Expand&&expansions.TryGetValue(order.target,out var unfinished)){unfinished.SetActive(false);ReleaseBuiltMeshes(unfinished);Destroy(unfinished);expansions.Remove(order.target);}}else{State.inventory.Receive(r.Output);Complete(order);order.reserved=null;}
                if(actor==WorkActor.External)Scenario.Drones[int.Parse(order.actorId.Substring(6))].FinishStationWork();
                order.complete=true;order.status=order.cancelled?"Abgebrochen / zurück am Dock":"Abgeschlossen";
            }
            // One occupant. Slow physical-time consumption; no free restoration from opening a menu.
            State.inventory.oxygen.Remove(seconds*.0000093f);
            float water=Scenario.TakeFuelWater(seconds*2/86400f),food=State.inventory.food.Remove(seconds*3/86400f);
            var vitals=StationInteriorMode.Current?.Vitals;
            if(vitals!=null){vitals.Thirst=Mathf.Clamp(vitals.Thirst+(water<seconds*2/86400f-.0000001f?seconds/300:-seconds/1200),0,100);vitals.Hunger=Mathf.Clamp(vitals.Hunger+(food<seconds*3/86400f-.0000001f?seconds/900:-seconds/1800),0,100);vitals.Fatigue=Mathf.Clamp(vitals.Fatigue+seconds/86400,0,100);if(State.inventory.oxygen.amount<=0||!State.power.LifePowered)vitals.Health=Mathf.Max(0,vitals.Health-seconds/600);}
            foreach(var e in State.elements){bool active=e.id=="reactor"&&Powered(0)||e.kind=="Energieanlage"&&Powered(e.sector)||running.Exists(o=>!o.cancelled&&(o.target==e.id||e.id=="printer"&&o.kind==StationJobKind.PrintParts||e.id=="food"&&(o.kind==StationJobKind.Food||o.kind==StationJobKind.Oxygen)||e.id=="helper"&&o.actorId=="R-01"));e.condition=Math.Max(0,e.condition-seconds/(86400*(active?60:300)));}
            State.wearClock+=seconds;if(State.wearClock>=1){State.wearClock=0;RefreshPresentation();}
            AdvanceWorkshopVisuals(seconds);
            UpdateIcePlantVisuals(seconds);
        }
        private bool SupplyAvailable(StationWorkOrder order)
        {int sector=Actor(order)==WorkActor.Internal&&order.kind==StationJobKind.Recycle?1:0;return Powered(sector)&&(!(order.kind==StationJobKind.Food||order.kind==StationJobKind.Oxygen)||WaterConnected(0)&&WaterConnected(7));}
        private void Complete(StationWorkOrder order)
        {
            switch(order.kind){
                case StationJobKind.Diagnose:State.diagnosed=true;break;
                case StationJobKind.Salvage:State.debrisBatches++;State.collected.Add(order.target);break;
                case StationJobKind.Carbon:State.carbonBatches++;State.collected.Add(order.target);break;
                case StationJobKind.ActivateHelper:State.helper=true;break;
                case StationJobKind.Food:State.foodProduced=true;break;
                case StationJobKind.ActivateSecond:State.second=true;Scenario.Drones[1].ActivateAtDock();break;
                case StationJobKind.ResearchRepair:if(!State.technologies.Contains("repair"))State.technologies.Add("repair");break;
                case StationJobKind.ResearchExpansion:if(!State.technologies.Contains("expansion"))State.technologies.Add("expansion");break;
                case StationJobKind.Repair:Element(order.target).condition=1;State.repairExperience++;UpdateRepairedSector(Element(order.target));break;
                case StationJobKind.Expand:Element(order.target).built=true;Element(order.target).condition=1;BuildExpansion(order.target);RefreshExpansionProgress(order.target);break;
                case StationJobKind.Refit:ApplyRefit(order.target);break;
            }
            SetMessage(StationRecipes.Get(order.kind).Name+" abgeschlossen.");RefreshPresentation();
        }
        public StationCampaignState Capture()=>JsonUtility.FromJson<StationCampaignState>(JsonUtility.ToJson(State));
        public static bool Valid(StationCampaignState s)
        {
            if(s==null||s.version!=1||s.inventory==null||!s.inventory.Valid||s.power==null||!s.power.Valid||s.elements==null||s.orders==null||s.orders.Count>80||s.technologies==null||s.collected==null||s.tools==null||s.tools.Length!=10)return false;
            if(!StationIcePlant.Valid(s.icePlant))return false;
            var ids=new HashSet<string>();foreach(var e in s.elements)if(e==null||string.IsNullOrEmpty(e.id)||!ids.Add(e.id)||e.sector<0||e.sector>=12||!ResourceContainer.Finite(e.condition)||e.condition<0||e.condition>1)return false;
            var known=new HashSet<string>();for(int sector=0;sector<12;sector++)known.Add("sector-"+sector);
            foreach(string key in new[]{"command","hall","helper","water","solar-a","solar-b","reactor","printer","food","battery","scrap","water-1","water-2","service-0","service-1","service-2","service-3"})known.Add(key);
            for(int i=0;i<5;i++)known.Add("debris-"+i);for(int i=0;i<6;i++)known.Add("carbon-"+i);
            for(int sector=0;sector<12;sector++)if(sector!=0&&sector!=1&&sector!=7&&!RingWalkthrough.DamagedRingSector(sector))for(int slot=0;slot<3;slot++)known.Add("expansion-"+sector+"-"+slot);
            if(!known.SetEquals(ids))return false;
            for(int sector=0;sector<12;sector++)if(!ids.Contains("sector-"+sector))return false;
            foreach(string key in new[]{"command","hall","helper","water","solar-a","solar-b","reactor","printer","food","battery","scrap"})if(!ids.Contains(key))return false;
            if(s.spareDrills<0||s.spareGrippers<0||s.spareLasers<0||!ResourceContainer.Finite(s.elapsed)||s.elapsed<0||!ResourceContainer.Finite(s.wearClock)||s.wearClock<0)return false;
            foreach(var tool in s.tools)if(tool!="Bohrer"&&tool!="Greifer"&&tool!="Laser")return false;
            var found=new HashSet<string>();foreach(var source in s.collected){if(source==null||!found.Add(source)||!(source.StartsWith("debris-")&&int.TryParse(source.Substring(7),out int di)&&di>=0&&di<5||source.StartsWith("carbon-")&&int.TryParse(source.Substring(7),out int ci)&&ci>=0&&ci<6))return false;}
            if(s.collected.Count!=s.debrisBatches+s.carbonBatches)return false;
            found.Clear();foreach(var tech in s.technologies)if((tech!="repair"&&tech!="expansion")||!found.Add(tech))return false;
            var elementIds=new HashSet<string>(ids);ids.Clear();var activeActors=new HashSet<string>();foreach(var o in s.orders){
                if(o==null||string.IsNullOrEmpty(o.id)||!ids.Add(o.id)||StationRecipes.Get(o.kind)==null||!ResourceContainer.Finite(o.elapsed)||o.elapsed<0||o.elapsed>StationRecipes.Get(o.kind).Seconds+.01f)return false;
                if((o.kind==StationJobKind.Repair||o.kind==StationJobKind.Expand)&&(!elementIds.Contains(o.target)||o.kind==StationJobKind.Expand&&!o.target.StartsWith("expansion-")))return false;
                if(o.kind==StationJobKind.Refit){var bits=(o.target??"").Split(':');if(bits.Length!=2||!int.TryParse(bits[0],out int d)||d<0||d>1||(bits[1]!="Bohrer"&&bits[1]!="Greifer"&&bits[1]!="Laser"))return false;}
                if(o.kind==StationJobKind.Salvage&&!(o.target!=null&&o.target.StartsWith("debris-")&&int.TryParse(o.target.Substring(7),out int di)&&di>=0&&di<5))return false;
                if(o.kind==StationJobKind.Carbon&&!(o.target!=null&&o.target.StartsWith("carbon-")&&int.TryParse(o.target.Substring(7),out int ci)&&ci>=0&&ci<6))return false;
                if(o.flightPath!=null&&o.flightPath.Length>0){if(o.flightPath.Length<4||o.flightPath.Length>128)return false;foreach(var point in o.flightPath)if(!ResourceContainer.Finite(point.x)||!ResourceContainer.Finite(point.y)||!ResourceContainer.Finite(point.z))return false;}
                if(!WorkshopRefitState.Valid(o.workshop)||WorkshopRefitState.HasData(o.workshop)&&o.kind!=StationJobKind.Refit)return false;
                if(o.started&&!o.complete&&(o.actorId==null||!(o.actorId=="drone-0"||o.actorId=="drone-1"||o.actorId=="R-01"||o.actorId=="Mira"||o.actorId=="Bestandsautomatik / Inbetriebnahme")||!activeActors.Add(o.actorId)||o.actorId=="drone-1"&&!s.second||o.actorId=="R-01"&&!s.helper||o.reserved==null))return false;
                // Unity serializes null arrays as empty arrays. Reservation ownership follows state.
                if(o.started&&!o.complete){var expected=StationRecipes.Get(o.kind).Input;if(o.reserved==null||o.reserved.Length!=expected.Length)return false;for(int i=0;i<expected.Length;i++)if(o.reserved[i]==null||o.reserved[i].id!=expected[i].id||!ResourceContainer.Finite(o.reserved[i].quantity)||Math.Abs(o.reserved[i].quantity-expected[i].quantity)>.0001f)return false;}
                else if(o.reserved!=null&&o.reserved.Length>0)return false;
            }
            return s.debrisBatches>=0&&s.debrisBatches<=5&&s.carbonBatches>=0&&s.carbonBatches<=6;
        }
        public void Restore(StationCampaignState saved)
        {
            if(saved==null){float capacity=Scenario.TankCapacityLiters,water=Scenario.WaterLiters;ResetCampaign();Scenario.TankCapacityLiters=capacity;Scenario.WaterTank.Set(water);return;}if(!Valid(saved))throw new ArgumentException("Ungültiger Aufbau-Spielstand");
            ClearBuiltPresentation();State=JsonUtility.FromJson<StationCampaignState>(JsonUtility.ToJson(saved));State.inventory.water=Scenario.WaterTank;
            State.foodProduced|=State.orders.Exists(o=>o.kind==StationJobKind.Food&&o.complete&&!o.cancelled);
            RestoreMiraQuestProgress();
            BindIcePlant();
            running.Clear();
            foreach(var order in State.orders)if(order.started&&!order.complete&&order.actorId.StartsWith("drone-"))Scenario.Drones[int.Parse(order.actorId.Substring(6))].ResumeStationWork();
            RestoreWorkshopRefits();BalancePower(0);RefreshPresentation();
        }
        public void ResetCampaign(){if(initialJson==null)return;ClearBuiltPresentation();State=JsonUtility.FromJson<StationCampaignState>(initialJson);Scenario.TankCapacityLiters=200;State.inventory.water=Scenario.WaterTank;BindIcePlant();running.Clear();foreach(var d in Scenario.Drones)if(d!=Scenario.Worker)d.LockInactive();BalancePower(0);RefreshPresentation();}
        private void OnDestroy(){if(Current==this)Current=null;DisposePowerModels(true);DisposePresentation();}
    }
}
