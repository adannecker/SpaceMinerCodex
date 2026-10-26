using System;
using System.Collections.Generic;

namespace SpaceMiner
{
    public enum StationJobKind { Diagnose, Salvage, ActivateHelper, Carbon, Recycle, PrintParts, Food, ActivateSecond, ResearchRepair, ResearchExpansion, Repair, Expand, Refit, Oxygen }
    public enum WorkActor { Manual, External, Internal, Research }
    [Serializable] public sealed class StationWorkOrder
    {
        public string id,target,status="Wartet",actorId;
        public StationJobKind kind;
        public float elapsed;
        public bool started,complete,cancelled;
        public ResourceAmount[] reserved;
        public UnityEngine.Vector3[] flightPath;
        public WorkshopRefitState workshop;
        public StationWorkOrder(StationJobKind type,string destination){id=Guid.NewGuid().ToString("N");kind=type;target=destination;}
    }
    public sealed class StationRecipe
    {
        public readonly StationJobKind Kind;
        public readonly string Name,Category,Technology;
        public readonly WorkActor Actor;
        public readonly float Seconds,PowerKw;
        public readonly ResourceAmount[] Input,Output;
        public StationRecipe(StationJobKind kind,string name,string category,WorkActor actor,float seconds,float power,ResourceAmount[] input,ResourceAmount[] output,string technology="")
        {Kind=kind;Name=name;Category=category;Actor=actor;Seconds=seconds;PowerKw=power;Input=input;Output=output;Technology=technology;}
    }
    public static class StationRecipes
    {
        private static ResourceAmount R(string id,float n)=>new ResourceAmount(id,n);
        private static readonly ResourceAmount[] None=new ResourceAmount[0];
        // Prototype process values, explicitly not a chemically validated complete nutrition recipe.
        public static readonly StationRecipe[] All={
            new StationRecipe(StationJobKind.Diagnose,"Versorgung diagnostizieren","Versorgung",WorkActor.Manual,45,.15f,None,None),
            new StationRecipe(StationJobKind.Salvage,"Trümmer bergen","Bergung",WorkActor.External,600,.15f,None,new[]{R("scrap",12),R("parts",2)}),
            new StationRecipe(StationJobKind.ActivateHelper,"Interne R-01 aktivieren","Drohnen",WorkActor.Manual,120,.3f,new[]{R("parts",2)},None),
            new StationRecipe(StationJobKind.Carbon,"Kohlenstoffprobe bergen","Bergung",WorkActor.External,720,.2f,None,new[]{R("carbon",8),R("unknown",2)}),
            new StationRecipe(StationJobKind.Recycle,"Schrott analysieren und trennen","Produktion",WorkActor.Internal,300,.8f,new[]{R("scrap",10)},new[]{R("iron",6),R("unknown",3),R("carbon",1)}),
            new StationRecipe(StationJobKind.PrintParts,"Reparaturteile fertigen","Produktion",WorkActor.Internal,240,1.4f,new[]{R("iron",2)},new[]{R("parts",2)}),
            new StationRecipe(StationJobKind.Food,"Grundnahrung synthetisieren","Lebenserhaltung",WorkActor.Internal,360,1.2f,new[]{R("carbon",.4f),R("water",1),R("supplement",1)},new[]{R("food",4)}),
            new StationRecipe(StationJobKind.ActivateSecond,"Drohne 02 instand setzen","Drohnen",WorkActor.Internal,300,.5f,new[]{R("parts",2)},None),
            new StationRecipe(StationJobKind.ResearchRepair,"Mira: Reparaturrolle","Forschung",WorkActor.Research,300,.6f,None,None),
            new StationRecipe(StationJobKind.ResearchExpansion,"Mira: Modulmontage","Forschung",WorkActor.Research,480,.8f,new[]{R("parts",1)},None,"repair"),
            new StationRecipe(StationJobKind.Repair,"Element instand setzen","Wartung",WorkActor.External,360,.3f,new[]{R("parts",2)},None,"repair"),
            new StationRecipe(StationJobKind.Expand,"Versorgungsmodul anbauen","Stationsbau",WorkActor.External,900,.4f,new[]{R("parts",6),R("iron",8)},None,"expansion"),
            new StationRecipe(StationJobKind.Refit,"Werkzeug umrüsten","Drohnen",WorkActor.Internal,180,.4f,None,None),
            new StationRecipe(StationJobKind.Oxygen,"Sauerstoff aus Wasser gewinnen","Lebenserhaltung",WorkActor.Internal,7200,6,new[]{R("water",2)},new[]{R("oxygen",1.7778f),R("hydrogen",.2222f)})
        };
        public static StationRecipe Get(StationJobKind kind)=>Array.Find(All,r=>r.Kind==kind);
    }
    [Serializable] public sealed class StationElementState
    {
        public string id,name,kind,use;
        public int sector;
        public float condition=1;
        public bool power=true,water=true,built=true;
        public StationElementState(string key,string title,string type,string usage,int zone,float state=1)
        {id=key;name=title;kind=type;use=usage;sector=zone;condition=state;}
    }
    [Serializable] public sealed class StationPowerSystem
    {
        public float capacityKwh=24,storedKwh=12;
        [NonSerialized] public float GenerationKw,ConsumptionKw,ChargeAllocationKw;
        public bool JobPowered {get;private set;}
        public bool LifePowered {get;private set;}
        public void Advance(float seconds,float generation,float essentials,float work,float charging)
        {
            GenerationKw=Math.Max(0,generation);
            float available=GenerationKw+Math.Min(8,storedKwh*3600/Math.Max(.001f,seconds));
            float basic=Math.Min(available,essentials);available-=basic;LifePowered=basic>=essentials-.001f;
            float allocatedWork=Math.Min(available,work);available-=allocatedWork;JobPowered=allocatedWork>=work-.001f;
            // A brownout pauses the entire job; it cannot silently consume partial work power.
            if(!JobPowered){available+=allocatedWork;allocatedWork=0;}
            ChargeAllocationKw=Math.Min(available,charging);
            ConsumptionKw=basic+allocatedWork+ChargeAllocationKw;
            storedKwh=Math.Max(0,Math.Min(capacityKwh,storedKwh+(GenerationKw-ConsumptionKw)*seconds/3600));
        }
        public bool Valid=>ResourceContainer.Finite(capacityKwh)&&capacityKwh>0&&ResourceContainer.Finite(storedKwh)&&storedKwh>=0&&storedKwh<=capacityKwh;
    }
    [Serializable] public sealed class StationCampaignState
    {
        public int version=1;
        public StationInventory inventory=new StationInventory();
        public StationPowerSystem power=new StationPowerSystem();
        public StationIcePlant.SavedState icePlant;
        public List<StationElementState> elements=new List<StationElementState>();
        public List<StationWorkOrder> orders=new List<StationWorkOrder>();
        public List<string> technologies=new List<string>();
        public List<string> collected=new List<string>();
        public List<string> miraDialogues=new List<string>();
        public bool diagnosed,helper,second,waterSecured,foodProduced;
        public int debrisBatches,carbonBatches,repairExperience;
        public string[] tools={"Bohrer","Bohrer","Bohrer","Bohrer","Bohrer","Bohrer","Bohrer","Bohrer","Bohrer","Bohrer"};
        public int spareDrills=3,spareGrippers=3,spareLasers=3;
        public float elapsed,wearClock;
    }
}
