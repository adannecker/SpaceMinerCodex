using System;
using System.Collections.Generic;

namespace SpaceMiner
{
    public enum ResourcePhase { Solid, Liquid, Gas, Food }
    public sealed class ResourceDefinition
    {
        public readonly string Id, Name, Unit;
        public readonly ResourcePhase Phase;
        public ResourceDefinition(string id,string name,string unit,ResourcePhase phase){Id=id;Name=name;Unit=unit;Phase=phase;}
    }
    public static class StationResources
    {
        public static readonly ResourceDefinition Water=new ResourceDefinition("water","Wasser","L",ResourcePhase.Liquid);
        public static readonly ResourceDefinition Oxygen=new ResourceDefinition("oxygen","Sauerstoffreserve","kg",ResourcePhase.Gas);
        public static readonly ResourceDefinition Hydrogen=new ResourceDefinition("hydrogen","Prozesswasserstoff","kg",ResourcePhase.Gas);
        public static readonly ResourceDefinition Carbon=new ResourceDefinition("carbon","Kohlenstoff-Konzentrat","kg",ResourcePhase.Solid);
        public static readonly ResourceDefinition Iron=new ResourceDefinition("iron","Eisenwerkstoff","kg",ResourcePhase.Solid);
        public static readonly ResourceDefinition Scrap=new ResourceDefinition("scrap","Unsortierter Schrott","kg",ResourcePhase.Solid);
        public static readonly ResourceDefinition Unknown=new ResourceDefinition("unknown","Unbekannte Rückstände","kg",ResourcePhase.Solid);
        public static readonly ResourceDefinition Parts=new ResourceDefinition("parts","Geprüfte Ersatzteile","Stk",ResourcePhase.Solid);
        public static readonly ResourceDefinition Supplement=new ResourceDefinition("supplement","Nährstoffkartuschen","Stk",ResourcePhase.Solid);
        public static readonly ResourceDefinition Food=new ResourceDefinition("food","Grundnahrung / Notrationen","Port.",ResourcePhase.Food);
        public static readonly ResourceDefinition[] All={Water,Oxygen,Hydrogen,Carbon,Iron,Scrap,Unknown,Parts,Supplement,Food};
        public static ResourceDefinition Get(string id)=>Array.Find(All,r=>r.Id==id);
    }
    // Composition: contents define the material; the container defines admissible phase and capacity.
    [Serializable] public abstract class ResourceContainer
    {
        [UnityEngine.SerializeField] private string definitionId;
        public string resourceId=>definitionId;
        [UnityEngine.SerializeField] private float maximumCapacity,storedAmount;
        public float capacity {get=>maximumCapacity;set{if(!Finite(value)||value<=0)throw new ArgumentException("Invalid capacity");maximumCapacity=value;storedAmount=Math.Min(storedAmount,maximumCapacity);}}
        public float amount=>storedAmount;
        protected abstract ResourcePhase Phase {get;}
        protected ResourceContainer(ResourceDefinition resource,float size,float initial=0)
        {definitionId=resource.Id;capacity=size;if(resource.Phase!=Phase)throw new ArgumentException("Incompatible container phase");Set(initial);}
        public ResourceDefinition Definition=>StationResources.Get(resourceId);
        public float Free=>Math.Max(0,capacity-amount);
        public bool Valid=>Definition!=null&&Definition.Phase==Phase&&Finite(capacity)&&capacity>0&&Finite(amount)&&amount>=0&&amount<=capacity+.001f;
        public static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
        public void Set(float value){if(!Finite(value)||!Finite(capacity)||capacity<=0)throw new ArgumentException("Invalid reservoir value");storedAmount=Math.Max(0,Math.Min(capacity,value));}
        public float Add(float value){if(!Finite(value)||value<0)throw new ArgumentException("Invalid addition");float accepted=Math.Min(value,Free);storedAmount+=accepted;return accepted;}
        public float Remove(float value){if(!Finite(value)||value<0)throw new ArgumentException("Invalid removal");float removed=Math.Min(value,amount);storedAmount-=removed;return removed;}
        public bool TransferTo(ResourceContainer other,float quantity)
        {if(other==null||other==this||!Finite(quantity)||quantity<0||other.resourceId!=resourceId||quantity>amount||quantity>other.Free)return false;Remove(quantity);other.Add(quantity);return true;}
    }
    [Serializable] public sealed class FluidTank:ResourceContainer
    {protected override ResourcePhase Phase=>ResourcePhase.Liquid;public FluidTank(ResourceDefinition resource,float size,float initial=0):base(resource,size,initial){} }
    [Serializable] public sealed class GasTank:ResourceContainer
    {protected override ResourcePhase Phase=>ResourcePhase.Gas;public GasTank(ResourceDefinition resource,float size,float initial=0):base(resource,size,initial){} }
    [Serializable] public sealed class SolidStore:ResourceContainer
    {protected override ResourcePhase Phase=>ResourcePhase.Solid;public SolidStore(ResourceDefinition resource,float size,float initial=0):base(resource,size,initial){} }
    [Serializable] public sealed class FoodStore:ResourceContainer
    {protected override ResourcePhase Phase=>ResourcePhase.Food;public FoodStore(float size,float initial=0):base(StationResources.Food,size,initial){} }
    [Serializable] public sealed class ResourceAmount
    {public string id;public float quantity;public ResourceAmount(string resource,float value){id=resource;quantity=value;}}
    [Serializable] public sealed class StationInventory
    {
        public FluidTank water=new FluidTank(StationResources.Water,200,20);
        public GasTank oxygen=new GasTank(StationResources.Oxygen,20,8);
        public GasTank hydrogen=new GasTank(StationResources.Hydrogen,10);
        public SolidStore carbon=new SolidStore(StationResources.Carbon,100),iron=new SolidStore(StationResources.Iron,200),
            scrap=new SolidStore(StationResources.Scrap,150),unknown=new SolidStore(StationResources.Unknown,100),
            parts=new SolidStore(StationResources.Parts,100,3),supplement=new SolidStore(StationResources.Supplement,20,12);
        public FoodStore food=new FoodStore(100,18);
        public IEnumerable<ResourceContainer> Containers {get{yield return water;yield return oxygen;yield return hydrogen;yield return carbon;yield return iron;yield return scrap;yield return unknown;yield return parts;yield return supplement;yield return food;}}
        public ResourceContainer Get(string id){switch(id){case "water":return water;case "oxygen":return oxygen;case "hydrogen":return hydrogen;case "carbon":return carbon;case "iron":return iron;case "scrap":return scrap;case "unknown":return unknown;case "parts":return parts;case "supplement":return supplement;case "food":return food;default:return null;}}
        public bool Valid {get{int index=0;foreach(var c in Containers){if(c==null||!c.Valid||c.resourceId!=StationResources.All[index].Id)return false;index++;}return index==StationResources.All.Length;}}
        public bool CanConsume(ResourceAmount[] recipe)
        {for(int i=0;i<recipe.Length;i++){var r=recipe[i];if(r==null||!ResourceContainer.Finite(r.quantity)||r.quantity<0)return false;float total=0;foreach(var part in recipe)if(part!=null&&part.id==r.id)total+=part.quantity;if(Get(r.id)==null||Get(r.id).amount+.0001f<total)return false;}return true;}
        public bool TryConsume(ResourceAmount[] recipe){if(!CanConsume(recipe))return false;foreach(var r in recipe)Get(r.id).Remove(r.quantity);return true;}
        public bool CanReceive(ResourceAmount[] recipe){for(int i=0;i<recipe.Length;i++){var r=recipe[i];if(r==null||!ResourceContainer.Finite(r.quantity)||r.quantity<0)return false;float total=0;foreach(var part in recipe)if(part!=null&&part.id==r.id)total+=part.quantity;if(Get(r.id)==null||Get(r.id).Free+.0001f<total)return false;}return true;}
        public void Receive(ResourceAmount[] recipe){if(!CanReceive(recipe))throw new InvalidOperationException("Output storage full");foreach(var r in recipe)Get(r.id).Add(r.quantity);}
    }
}
