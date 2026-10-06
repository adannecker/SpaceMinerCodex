using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    // Presentation definitions. These do not claim that a research simulation exists.
    public sealed class TechnologyNode
    {
        public readonly string Id, Title, Icon, Description, Requirements, Practical;
        public readonly int Column;
        public readonly float Row;
        public readonly bool Installed;
        public readonly string[] Parents;
        public TechnologyNode(string id, string title, string icon, int column, float row, bool installed,
            string[] parents, string description, string requirements, string practical)
        { Id=id; Title=title; Icon=icon; Column=column; Row=row; Installed=installed; Parents=parents; Description=description; Requirements=requirements; Practical=practical; }
    }

    public static class TechnologyCatalog
    {
        public static readonly TechnologyNode[] TierOne = {
            N("start","Stationssysteme","station",0,3.5f,true,Array.Empty<string>(),"Die beschädigte Raumstation besitzt grundlegende Technik. Forschung soll diese Fähigkeiten später erweitern.","Vorhandene Systeme an Bord.","Betriebsfähige Anlagen, Ressourcen und Energie."),
            N("mining","Basisabbau","pickaxe",1,1,true,P("start"),"Drohne 01 baut Eis an bekannten Quellen ab.","Drohne 01 und bekannte Eisquelle.","Batterie, Treibwasser und freie Ladekapazität."),
            N("science","Ressourcenanalyse","flask",1,2,false,P("start"),"Proben untersuchen und unbekannte Materialanteile identifizieren.","Probe und wiederhergestellter Analyseplatz.","Analysegerät, Probe und elektrische Energie."),
            N("explore","Nahbereichsscan","radar",1,3,false,P("start"),"Weitere Ressourcen finden. Die drei bekannten Wasserquellen sind derzeit vorgegeben.","Funktionsfähiger Scanner.","Sensoren und elektrische Energie."),
            N("energy","Stationsversorgung","power",1,4,true,P("start"),"Solarflächen und kleiner Kernreaktor sind vorhandene Technik. Vollständiger Notstrombetrieb ist noch offen.","Vorhandene Stromquelle und Ladestation.","Nutzbare elektrische Leistung."),
            N("logistics","Wassertransport","cargo",1,5,true,P("start"),"Der vorhandene Drohnenkreislauf bringt Eis zur Station und befüllt den Tank.","Drohne 01 und Stationstank.","Batterie, Treibwasser und freie Tankkapazität."),
            N("robotics","Drohnensteuerung","robot",1,6,true,P("start"),"Drohne 01 übernimmt den wiederholten Wasserauftrag.","Einsatzfähige Drohne.","Versorgung und erreichbare Arbeitsquelle."),
            N("ice","Gezielte Eisgewinnung","ice",2,1,false,P("mining"),"Abbauparameter an die bekannte Eisquelle anpassen.","Basisabbau, Wasser entdeckt und praktische Erfahrung. Wissensschwellen noch offen.","Abbaudrohne und analysierte Eisquelle."),
            N("quality","Wasserqualität","water",2,2,false,P("science"),"Verunreinigungen erkennen und geeignete Aufbereitung bestimmen.","Ressourcenanalyse und Wasserprobe.","Analyseplatz; passende Aufbereitung für Trinkwasser."),
            N("survey","Prospektion","scan",2,3,false,P("explore","science"),"Scans und Proben zu einer Lagerstättenbewertung verbinden.","Nahbereichsscan und Ressourcenanalyse.","Scanner, Analyseplatz und Probendrohne."),
            N("load","Lastmanagement","battery",2,4,false,P("energy"),"Verbraucher priorisieren und verfügbare Leistung aufteilen.","Stationsversorgung und Energy Knowledge; Schwelle offen.","Steuerung, Stromverteilung und Leistungsreserven."),
            N("routes","Transportplanung","route",2,5,false,P("logistics"),"Lieferziele und Transportprioritäten planen.","Wassertransport, Logistics Knowledge und Drone Knowledge; Schwellen offen.","Drohne, Lagerplätze und Versorgung."),
            N("queue","Auftragswarteschlange","tasks",2,6,false,P("robotics"),"Aufträge geordnet ausführen und bei fehlender Versorgung warten.","Drohnensteuerung und praktische Betriebs-/Transporterfahrung.","Steuerung und versorgte Drohne."),
            N("processing","Wasseraufbereitung","filter",3,1.5f,false,P("ice","quality"),"Passende Verfahren für die Wasserqualität erschliessen. Reale Verfahren sind noch fachlich zu recherchieren.","Gezielte Eisgewinnung und Wasserqualität.","Aufbereitungsanlage, Energie und Verbrauchsmaterialien. Die bestehende vereinfachte Eisaufbereitung ist noch keine Trinkwasserprüfung."),
            N("materials","Materialeigenschaften","gem",3,2.8f,false,P("quality","survey"),"Identifizierte Proben eröffnen ressourcenspezifische Forschungswege. Die genaue Verknüpfung ist ein Entwurf.","Materialprobe und Prospektion; endgültige Voraussetzungen offen.","Analyseplatz und identifizierte Probe."),
            N("coordination","Versorgungsplanung","workflow",3,5,false,P("load","routes","queue"),"Transport, Arbeit und Energiebedarf aufeinander abstimmen.","Lastmanagement, Transportplanung und Auftragswarteschlange.","Steuerung, versorgte Drohne und Leistungsbudget."),
            N("auto","Autonome Wasserversorgung","autowater",4,3.5f,false,P("processing","coordination"),"Qualität, Tankbedarf und Energie in einem selbstständigen Versorgungskreislauf verbinden.","Wasseraufbereitung, Versorgungsplanung und Erfahrung. Wissensschwellen offen.","Drohne, Tank, Aufbereitung und stabile Energieversorgung.")
        };
        private static string[] P(params string[] ids) => ids;
        private static TechnologyNode N(string id,string title,string icon,int col,float row,bool installed,string[] parents,string desc,string req,string use)
            => new TechnologyNode(id,title,icon,col,row,installed,parents,desc,req,use);
        public static int IndexOf(string id) => Array.FindIndex(TierOne,n=>n.Id==id);
    }

    public sealed class TechnologyEdge
    {
        public int Source, Target;
        public Vector2[] Points;
    }
    public static class TechnologyLayout
    {
        public static Rect[] Nodes(Rect field)
        {
            float[] columns={.035f,.245f,.475f,.735f,.965f};
            var result=new Rect[TechnologyCatalog.TierOne.Length];
            for(int i=0;i<result.Length;i++) {
                var n=TechnologyCatalog.TierOne[i];
                result[i]=new Rect(field.x+field.width*columns[n.Column]-25,field.y+(n.Row-1)*60,50,50);
            }
            return result;
        }
        public static List<TechnologyEdge> Edges(Rect field,Rect[] nodes)
        {
            var result=new List<TechnologyEdge>();
            for(int t=0;t<nodes.Length;t++) foreach(string parent in TechnologyCatalog.TierOne[t].Parents)
                result.Add(new TechnologyEdge { Source=TechnologyCatalog.IndexOf(parent), Target=t });
            foreach(var edge in result) {
                var outs=result.FindAll(e=>e.Source==edge.Source); outs.Sort((a,b)=>nodes[a.Target].center.y.CompareTo(nodes[b.Target].center.y));
                var ins=result.FindAll(e=>e.Target==edge.Target); ins.Sort((a,b)=>nodes[a.Source].center.y.CompareTo(nodes[b.Source].center.y));
                int oi=outs.IndexOf(edge),ii=ins.IndexOf(edge); var a=nodes[edge.Source];var b=nodes[edge.Target];
                float y1=a.center.y+(oi-(outs.Count-1)*.5f)*7,y2=b.center.y+(ii-(ins.Count-1)*.5f)*7;
                if(Mathf.Abs(a.center.y-b.center.y)<1) { float aligned=outs.Count>=ins.Count?y1:y2;y1=aligned;y2=aligned; }
                float x1=a.xMax+3,x2=b.xMin-3,mid=(x1+x2)*.5f;
                string p=TechnologyCatalog.TierOne[edge.Source].Id,t=TechnologyCatalog.TierOne[edge.Target].Id;
                if(p=="start") { int rank=oi<3?oi:outs.Count-1-oi;mid=x1+(x2-x1)*(.25f+rank*.2f); }
                else if(t=="processing")mid=field.x+field.width*.57f+ii*8;
                else if(t=="materials")mid=field.x+field.width*.65f+ii*8;
                else if(t=="coordination")mid=field.x+field.width*.59f+ii*8;
                else if(t=="auto")mid=field.x+field.width*.83f+ii*10;
                else if(t=="survey"&&p=="science")mid=field.x+field.width*.4f;
                mid=Mathf.Clamp(mid,x1+5,x2-5);
                edge.Points=Mathf.Abs(y1-y2)<.1f?new[]{new Vector2(x1,y1),new Vector2(x2,y2)}:
                    new[]{new Vector2(x1,y1),new Vector2(mid,y1),new Vector2(mid,y2),new Vector2(x2,y2)};
            }
            return result;
        }
    }
}
