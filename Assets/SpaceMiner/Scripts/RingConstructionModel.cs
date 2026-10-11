using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    // Deliberately separate from the campaign economy and save data.
    public sealed class RingConstructionModel
    {
        public enum Tool { Inspect, Storage, Separator, Fabricator }
        public enum Product { Ore, Metal, Parts }
        public sealed class Ring
        {
            public readonly float Radius;
            public readonly float Height;
            public readonly bool[] Segments;
            public Ring(float radius, int count,float height=0) { Radius=radius; Height=height; Segments=new bool[count]; }
            public bool Complete => Array.TrueForAll(Segments, b=>b);
        }
        public sealed class Module
        {
            public int Ring, Sector;
            public Tool Kind;
            public Product StoredProduct=Product.Parts;
            public bool Built;
            internal float Cycle;
        }
        public sealed class Job
        {
            public int Ring, Sector;
            public Module Module;
            public float Work;
            public float Duration => Module==null?3:5;
            public float Progress => Mathf.Clamp01(Work/Duration);
        }
        public readonly List<Ring> Rings=new List<Ring>();
        public readonly List<Module> Modules=new List<Module>();
        public readonly Queue<Job> Jobs=new Queue<Job>();
        public int ConstructionParts { get; private set; }=1800;
        public int Ore { get; private set; }=20;
        public int Metal { get; private set; }=5;
        public int Products { get; private set; }
        public int Revision { get; private set; }
        public int Delivered { get; private set; }
        public int CapacityFor(Product product)=>Modules.FindAll(m=>m.Kind==Tool.Storage&&m.Built&&m.StoredProduct==product).Count*25;
        public int Stock(Product product)=>product==Product.Ore?Ore:product==Product.Metal?Metal:Products;
        public static string ProductName(Product product)=>product==Product.Ore?"Erz":product==Product.Metal?"Metall":"Bauteile";
        public Job ActiveJob => Jobs.Count==0?null:Jobs.Peek();
        public RingConstructionModel()
        {
            var ring=new Ring(16,16);Array.Fill(ring.Segments,true);Rings.Add(ring);
            Modules.Add(new Module{Ring=0,Sector=2,Kind=Tool.Storage,StoredProduct=Product.Ore,Built=true});
            Modules.Add(new Module{Ring=0,Sector=10,Kind=Tool.Storage,StoredProduct=Product.Metal,Built=true});
            Modules.Add(new Module{Ring=0,Sector=6,Kind=Tool.Separator,Built=true});
            Modules.Add(new Module{Ring=0,Sector=13,Kind=Tool.Fabricator,Built=true});
        }
        public static string Name(Tool kind) => kind==Tool.Storage?"Lagercontainer":kind==Tool.Separator?"Trennanlage":kind==Tool.Fabricator?"Fertigung":"Auswahl";
        public static int Cost(Tool kind) => kind==Tool.Storage?35:kind==Tool.Separator?60:70;
        public bool Reserved(int ring,int sector) => sector%(Rings[ring].Segments.Length/4)==0;
        public int BuiltCount(Tool kind) => Modules.FindAll(m=>m.Kind==kind&&m.Built).Count;
        public int Slots(int ring) => Rings[ring].Segments.Length-4;
        public Module At(int ring,int sector,bool outside) => Modules.Find(m=>m.Ring==ring&&m.Sector==sector&&(m.Kind==Tool.Storage)==outside);
        public Vector3 Position(int ring,int sector,float offset=0)
        {
            float angle=sector*Mathf.PI*2/Rings[ring].Segments.Length;
            return new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*(Rings[ring].Radius+offset)+Vector3.up*Rings[ring].Height;
        }
        public bool AddRing(out string reason)
        {
            if(Rings.Count>=2){reason="Zwei übereinanderliegende Ringe bilden diesen Simulator.";return false;}
            if(!Rings[Rings.Count-1].Complete){reason="Zuerst den begonnenen Ring fertigbauen.";return false;}
            int count=16,cost=count*12;
            if(ConstructionParts<cost){reason="Nicht genug Testbauteile für den Ring.";return false;}
            int index=Rings.Count;Rings.Add(new Ring(16,count,12*index));ConstructionParts-=cost;
            for(int s=0;s<count;s++)Jobs.Enqueue(new Job{Ring=index,Sector=s});
            Revision++;reason="Ring "+(index+1)+": "+count+" Segmente beauftragt. Die Baudrohne beginnt.";return true;
        }
        public bool CanPlace(int ring,int sector,Tool tool,out string reason)
        {
            if(ring<0||ring>=Rings.Count||sector<0||sector>=Rings[ring].Segments.Length){reason="Einen Bauplatz am Ring auswählen.";return false;}
            if(tool==Tool.Inspect){reason="Bauwerkzeug links wählen.";return false;}
            if(!Rings[ring].Segments[sector]){reason="Dieses Ringsegment ist noch im Bau.";return false;}
            if(Reserved(ring,sector)){reason="Übergang freihalten: Gehweg und Drohnenroute.";return false;}
            if(At(ring,sector,tool==Tool.Storage)!=null){reason="Dieser Bauplatz ist bereits belegt oder beauftragt.";return false;}
            var footprint=Footprint(ring,sector,tool);
            foreach(var module in Modules){
                if(Mathf.Abs(Rings[module.Ring].Height-Rings[ring].Height)>=4)continue;
                if(Overlap(footprint,Footprint(module.Ring,module.Sector,module.Kind))){reason="Zu wenig Abstand zur benachbarten Anlage.";return false;}
            }
            for(int i=0;i<4;i++){
                float length=Rings[ring].Radius+2;
                var rotation=Quaternion.Euler(0,i*90,0);
                var corridor=Box(rotation*(Vector3.forward*(length+3.5f)*.5f),rotation,2.3f,length-3.5f);
                if(Overlap(footprint,corridor)){reason="Arbeitsraum würde einen Übergang blockieren.";return false;}
            }
            if(ConstructionParts<Cost(tool)){reason="Nicht genug Testbauteile.";return false;}
            reason=Name(tool)+" hier bauen ("+Cost(tool)+" Testbauteile).";return true;
        }
        private Vector3[] Footprint(int ring,int sector,Tool kind)=>Box(Position(ring,sector,kind==Tool.Storage?3.8f:-3.8f),Quaternion.Euler(0,sector*360f/Rings[ring].Segments.Length,0),3.5f,3.6f);
        private static Vector3[] Box(Vector3 center,Quaternion rotation,float width,float depth)=>new[]{
            center+rotation*new Vector3(-width*.5f,0,-depth*.5f),center+rotation*new Vector3(width*.5f,0,-depth*.5f),
            center+rotation*new Vector3(width*.5f,0,depth*.5f),center+rotation*new Vector3(-width*.5f,0,depth*.5f)};
        private static bool Overlap(Vector3[] a,Vector3[] b)
        {
            // Separating-axis test for oriented rectangular footprints, including queued modules.
            foreach(var rectangle in new[]{a,b})for(int edge=0;edge<2;edge++){
                var direction=rectangle[edge+1]-rectangle[edge];var axis=new Vector3(-direction.z,0,direction.x).normalized;
                float amin=float.MaxValue,amax=float.MinValue,bmin=float.MaxValue,bmax=float.MinValue;
                foreach(var p in a){float d=Vector3.Dot(p,axis);amin=Mathf.Min(amin,d);amax=Mathf.Max(amax,d);}
                foreach(var p in b){float d=Vector3.Dot(p,axis);bmin=Mathf.Min(bmin,d);bmax=Mathf.Max(bmax,d);}
                if(amax<=bmin||bmax<=amin)return false;
            }
            return true;
        }
        public bool Place(int ring,int sector,Tool tool,out string reason,Product product=Product.Parts)
        {
            if(!CanPlace(ring,sector,tool,out reason))return false;
            var module=new Module{Ring=ring,Sector=sector,Kind=tool,StoredProduct=product};Modules.Add(module);
            Jobs.Enqueue(new Job{Ring=ring,Sector=sector,Module=module});ConstructionParts-=Cost(tool);Revision++;
            reason=Name(tool)+(tool==Tool.Storage?" für "+ProductName(product):"")+" beauftragt. Material wird geliefert.";return true;
        }
        public bool SupplyOre()
        {
            if(Ore+20>CapacityFor(Product.Ore))return false;
            Ore+=20;return true;
        }
        public string Status(Module module)
        {
            if(!module.Built)return "Bauauftrag wartet auf die Baudrohne";
            if(module.Kind==Tool.Storage)return "Nur "+ProductName(module.StoredProduct)+" · Kapazität +25\nGesamtbestand: "+Stock(module.StoredProduct)+" / "+CapacityFor(module.StoredProduct);
            Product output=module.Kind==Tool.Separator?Product.Metal:Product.Parts;
            if(CapacityFor(output)==0)return "Wartet: Lager für "+ProductName(output)+" fehlt";
            if(module.Kind==Tool.Separator)return Ore==0?"Wartet: Rohstoff fehlt":Metal>=CapacityFor(Product.Metal)?"Wartet: Metalllager voll":"Trennt Erz → Metall";
            return Metal==0?"Wartet: Metall fehlt":Products>=CapacityFor(Product.Parts)?"Wartet: Bauteillager voll":"Fertigt Metall → Bauteil";
        }
        public void Tick(float seconds)
        {
            // Small steps keep newly completed modules and sequential recipes time-consistent.
            while(seconds>0){float step=Mathf.Min(seconds,.25f);Step(step);seconds-=step;}
        }
        private void Step(float seconds)
        {
            if(seconds<=0)return;
            float remaining=seconds;
            while(Jobs.Count>0&&remaining>0){
                var job=Jobs.Peek();float step=Mathf.Min(remaining,job.Duration-job.Work);job.Work+=step;remaining-=step;
                if(job.Work+0.0001f<job.Duration)break;
                if(job.Module==null)Rings[job.Ring].Segments[job.Sector]=true;else job.Module.Built=true;
                Jobs.Dequeue();Delivered++;Revision++;
            }
            foreach(var module in Modules){
                if(!module.Built||module.Kind==Tool.Storage)continue;
                bool available=module.Kind==Tool.Separator?Ore>0&&Metal<CapacityFor(Product.Metal):Metal>0&&Products<CapacityFor(Product.Parts);
                if(!available){module.Cycle=0;continue;}
                module.Cycle+=seconds;float duration=module.Kind==Tool.Separator?2:3;
                while(module.Cycle>=duration){
                    if(module.Kind==Tool.Separator){if(Ore==0||Metal>=CapacityFor(Product.Metal))break;Ore--;Metal++;}
                    else{if(Metal==0||Products>=CapacityFor(Product.Parts))break;Metal--;Products++;}
                    module.Cycle-=duration;
                }
                module.Cycle=Mathf.Min(module.Cycle,duration);
            }
        }
    }
}
