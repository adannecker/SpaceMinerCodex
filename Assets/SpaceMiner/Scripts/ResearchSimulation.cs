using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpaceMiner
{
    // Independent balancing sandbox. Never mutates live scenario resources or save games.
    [Serializable] public sealed class ResearchCatalog { public ResearchTree[] trees; }
    [Serializable] public sealed class ResearchTree { public string id,title,icon,note,requirements; public ResearchDefinition[] nodes; }
    [Serializable] public sealed class ResearchDefinition
    { public string id,name; public string[] parents; public bool known,upgrade; public float minutes; }
    [Serializable] public sealed class ResearchProgress
    { public string id; public int level; public float work; public bool evidence,hardware; }
    [Serializable] public sealed class ResearchSnapshot
    { public int version=1; public ResearchProgress[] nodes; public string[] queue; public double seconds; public int completions; }
    public sealed class ResearchSimulation
    {
        public readonly ResearchCatalog Catalog;
        public readonly Dictionary<string,ResearchDefinition> Definitions=new Dictionary<string,ResearchDefinition>();
        public readonly Dictionary<string,ResearchProgress> Progress=new Dictionary<string,ResearchProgress>();
        public readonly List<string> Queue=new List<string>();
        public float Speed=1; public int Slots=1; public bool Powered=true;
        public double Seconds; public int Completions;
        public int MiraLevel=>1+Completions/10;
        public ResearchSimulation(ResearchCatalog catalog)
        {
            Catalog=catalog;
            foreach(var tree in catalog.trees)foreach(var n in tree.nodes)Definitions.Add(n.id,n);
            AddGate("CPU-Erweiterung","Chipherstellung","Elektronikmontage");
            AddGate("Parallelrechnen","CPU-Erweiterung","Forschungsstufe 2","Erweiterte Kühlung");
            AddGate("Abgestimmte Schnellladung","Lastmanagement","Wärmeabfuhr erweitern");
            AddGate("Modulmontage","Reparaturrolle","Bauteilfertigung");
            AddGate("Glasdom","Druckprüfung");
            Reset();
        }
        private void AddGate(string target,params string[] names)
        {
            var node=Definitions.Values.FirstOrDefault(n=>n.name==target);
            if(node==null)throw new Exception("Fehlendes Forschungsziel: "+target);
            var parents=new List<string>(node.parents);
            foreach(var name in names){
                var parent=Definitions.Values.FirstOrDefault(n=>n.name==name);
                if(parent==null)throw new Exception("Fehlende Voraussetzung: "+name);
                if(!parents.Contains(parent.id))parents.Add(parent.id);
            }
            node.parents=parents.ToArray();
        }
        // Shared water-treatment capability: one research, two category references.
        public string Canonical(string id)
        {
            var n=Definitions[id];
            if(n.name=="Wasseraufbereitung") {
                var match=Definitions.Values.FirstOrDefault(x=>x.name==n.name&&x.id.StartsWith("materialien-"));
                if(match!=null)return match.id;
            }
            return id;
        }
        public ResearchProgress State(string id)=>Progress[Canonical(id)];
        public void Reset()
        {
            Queue.Clear();Progress.Clear();Seconds=0;Completions=0;Speed=1;Slots=1;Powered=true;
            foreach(var n in Definitions.Values)Progress.Add(n.id,new ResearchProgress{id=n.id,level=n.known?1:0});
        }
        public float Duration(string id)=>Definitions[Canonical(id)].minutes*60*Mathf.Pow(1.65f,State(id).level);
        public string Blocker(string id)
        {
            id=Canonical(id);var n=Definitions[id];var p=State(id);
            if(n.name=="Forschungsstufe 2"&&MiraLevel<2)return "Mira benötigt 10 Forschungsabschlüsse (Testschwelle)";
            if(p.level>0&&!n.upgrade)return "Abgeschlossen";
            if(p.level>=5)return "Maximale Simulationsstufe erreicht";
            foreach(var parent in n.parents)if(State(parent).level==0)return "Voraussetzung: "+Definitions[parent].name;
            if(!p.evidence)return "Daten / Probe / Praxiserfahrung fehlt";
            if(!p.hardware)return "Geeignete Arbeitsmittel fehlen";
            return null;
        }
        public bool Enqueue(string id)
        {
            id=Canonical(id);if(Blocker(id)!=null||Queue.Contains(id))return false;
            Queue.Add(id);return true;
        }
        public void Advance(float realSeconds)
        {
            if(realSeconds<=0||Speed<=0||!Powered)return;
            float remaining=realSeconds*Speed;
            // Event stepping keeps results identical for one large tick and many small ticks.
            while(remaining>0&&Queue.Count>0) {
                var running=Queue.Where(id=>Blocker(id)==null).Take(Mathf.Clamp(Slots,1,3)).ToArray();
                if(running.Length==0)break;
                float rate=1+Mathf.Min(1,(MiraLevel-1)*.05f);
                float step=remaining;
                foreach(var id in running)step=Mathf.Min(step,Mathf.Max(0,Duration(id)-State(id).work)/rate);
                foreach(var id in running)State(id).work+=step*rate;
                Seconds+=step;remaining-=step;
                foreach(var id in running)if(State(id).work+.001f>=Duration(id)) {
                    State(id).level++;State(id).work=0;Queue.Remove(id);Completions++;
                    if(MiraLevel>=2)Progress["mira-02"].level=Mathf.Max(1,Progress["mira-02"].level);
                }
                if(step==0&&running.All(id=>Queue.Contains(id)))break;
            }
            Seconds+=remaining;
        }
        public string Export()=>JsonUtility.ToJson(new ResearchSnapshot{nodes=Progress.Values.ToArray(),queue=Queue.ToArray(),seconds=Seconds,completions=Completions},true);
        public void Import(string json)
        {
            var snapshot=JsonUtility.FromJson<ResearchSnapshot>(json);
            if(snapshot==null||snapshot.version!=1||snapshot.nodes==null||snapshot.queue==null)throw new Exception("Unbekanntes Laborformat");
            var replacement=new Dictionary<string,ResearchProgress>();
            foreach(var p in snapshot.nodes) {
                if(!Definitions.ContainsKey(p.id)||replacement.ContainsKey(p.id)||p.level<0||p.level>5||float.IsNaN(p.work)||float.IsInfinity(p.work)||p.work<0)throw new Exception("Ungültiger Forschungsstand");
                if(Definitions[p.id].known&&p.level<1)throw new Exception("Startwissen fehlt");
                replacement.Add(p.id,p);
            }
            if(replacement.Count!=Definitions.Count||snapshot.queue.Distinct().Count()!=snapshot.queue.Length||snapshot.queue.Any(id=>!Definitions.ContainsKey(id)||Canonical(id)!=id)
                ||double.IsNaN(snapshot.seconds)||double.IsInfinity(snapshot.seconds)||snapshot.seconds<0||snapshot.completions<0)throw new Exception("Unvollständiger Forschungsstand");
            foreach(var pair in replacement) {
                float duration=Definitions[pair.Key].minutes*60*Mathf.Pow(1.65f,pair.Value.level);
                if(pair.Value.work>=duration)throw new Exception("Ungültiger Fortschritt");
            }
            Progress.Clear();foreach(var pair in replacement)Progress.Add(pair.Key,pair.Value);
            Queue.Clear();Queue.AddRange(snapshot.queue);Seconds=snapshot.seconds;Completions=snapshot.completions;Speed=0;
        }
    }
}
