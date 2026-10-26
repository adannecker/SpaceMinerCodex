using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceMiner
{
    // Physical quick disconnects reuse the campaign's actual tank and power network.
    // Decorative pipework never owns another reservoir or bypasses the charging rules.
    public sealed class StationDockInfrastructure : MonoBehaviour
    {
        private static Vector3 WaterFeedPoint=>new Vector3(RingWalkthrough.BerthSupplyOffset,-.5f,-1.15f);
        private static Vector3 PowerFeedPoint=>new Vector3(RingWalkthrough.BerthSupplyOffset,-.6f,-1.15f);
        private sealed class Feed
        {
            internal DroneAgent drone;
            internal Renderer power,water;
            internal Transform root,contact;
            internal Transform[] waterBridge,powerBridge;
            internal Vector3 fixedWaterTap,fixedPowerTap;
            internal bool Connected,Power,Water;
        }
        private sealed class Rupture
        {
            internal Transform root;
            internal string element;
            internal int sector;
            internal LineRenderer arc;
            internal float phase;
        }
        private readonly List<Feed> feeds=new List<Feed>();
        private readonly List<Rupture> ruptures=new List<Rupture>();
        private readonly List<Material> materials=new List<Material>();
        private readonly List<Mesh> meshes=new List<Mesh>();
        private WaterScenario scenario;
        private StationInteriorLayout layout;
        private Material alloy,dark,blue,cable,copper,signal,arcSurface,letters;
        private Font font;
        private float nextRefresh;
        public int FeedCount=>feeds.Count;
        public int RuptureCount=>ruptures.Count;
        internal bool FeedConnected(int index)=>feeds[index].Connected;
        internal bool FeedPowered(int index)=>feeds[index].Power;
        internal bool FeedWatered(int index)=>feeds[index].Water;
        internal Transform FeedRoot(int index)=>feeds[index].root;
        internal bool ArcVisible(int index)=>ruptures[index].root.gameObject.activeInHierarchy&&ruptures[index].arc.enabled;
        internal bool DamageVisible(int index)=>ruptures[index].root.gameObject.activeSelf;
        internal static StationDockInfrastructure Install(WaterScenario scenario,StationInteriorLayout layout)
        {
            var holder=new GameObject("Aussendock / Medienanschluesse und Havarien");holder.transform.SetParent(layout.transform,false);
            var system=holder.AddComponent<StationDockInfrastructure>();system.scenario=scenario;system.layout=layout;system.Build();return system;
        }
        private Material Surface(string name,Color color,bool emission=false)
        {
            var material=new Material(Shader.Find("Standard")){name=name,color=color};material.SetFloat("_Metallic",.55f);material.SetFloat("_Glossiness",.24f);material.enableInstancing=true;
            if(emission){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",color*1.5f);}materials.Add(material);return material;
        }
        private Transform Group(string name,Transform parent,Vector3 position,Quaternion rotation)
        {var group=new GameObject(name).transform;group.SetParent(parent,false);group.localPosition=position;group.localRotation=rotation;return group;}
        private Transform Box(string name,Transform parent,Vector3 position,Vector3 size,Material surface)
        {
            var item=GameObject.CreatePrimitive(PrimitiveType.Cube);item.name=name;item.layer=25;item.transform.SetParent(parent,false);item.transform.localPosition=position;item.transform.localScale=size;
            item.GetComponent<Renderer>().sharedMaterial=surface;item.GetComponent<Collider>().enabled=false;Destroy(item.GetComponent<Collider>());return item.transform;
        }
        private void Pipe(string name,Transform parent,Vector3 a,Vector3 b,float width,Material surface)
        {var part=Box(name,parent,(a+b)*.5f,new Vector3(width,Vector3.Distance(a,b),width),surface);part.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
        private void Build()
        {
            alloy=Surface("Dock / matte Metallhalter",new Color(.24f,.30f,.33f));dark=Surface("Dock / geschuetzte Anschluesse",new Color(.035f,.045f,.055f));
            blue=Surface("Dock / isolierte Wasserleitung",new Color(.1f,.34f,.49f));cable=Surface("Dock / isoliertes Leistungskabel",new Color(.34f,.24f,.11f));
            copper=Surface("Bruchstelle / freies Kupfer",new Color(.62f,.29f,.10f));signal=Surface("Dock / Betriebszustand",Color.white,true);
            arcSurface=new Material(Resources.Load<Shader>("StationArc")){name="Elektrischer Lichtbogen / Vakuum",color=new Color(.45f,.83f,1)};materials.Add(arcSurface);
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");letters=new Material(Resources.Load<Shader>("RingWalkthroughText"));letters.mainTexture=font.material.mainTexture;materials.Add(letters);Font.textureRebuilt+=FontRebuilt;
            var supplies=Group("Zehn getrennte Lade- und Wasseranschluesse",transform,Vector3.zero,Quaternion.identity);
            for(int i=0;i<scenario.Drones.Length;i++)BuildFeed(supplies,i);
            // All scars lie on already damaged structural remains, never across the safe walking lane.
            BuildRupture("Ring S04 / abgerissene Aussenleitung","sector-3",3,RingWalkthrough.At(37.95f,77.5f,5.95f),77.5f,0);
            BuildRupture("Ring S05 / geschaelte Schutzhaut","sector-4",4,RingWalkthrough.At(37.95f,108,5.95f),108,1);
            BuildRupture("Lager02 / aufgerissene Medienkassette","water-1",7,RingWalkthrough.At(48.8f,211.7f,5.85f),211.7f,2);
            BuildRupture("Lager03 / zerstoerter Kabelkanal","water-2",7,RingWalkthrough.At(42.7f,218,5.9f),218,3);
            BuildRupture("Reaktor-Service02 / offenes Leitungsende","service-1",3,RingWalkthrough.At(21.1f,90,3.65f),90,4);
            BuildImpact("Bay / aufgebogene aeussere Schutzkante",RingWalkthrough.At(73,17.1f,.23f),17.1f,1);
            BuildImpact("Halle / Schramme an der Aussenschutzlage",RingWalkthrough.At(51.6f,41.8f,5.85f),41.8f,-1);
            Refresh();RefreshDamage(Time.unscaledTime);
        }
        private void BuildImpact(string name,Vector3 point,float heading,int direction)
        {
            var scar=Group(name,transform,point,Quaternion.Euler(0,heading,0));
            Box("Freigelegte dunkle Zwischentraglage / Druckhuelle bleibt dicht",scar,Vector3.zero,new Vector3(1.7f,.045f,2.9f),dark);
            for(int i=0;i<3;i++)
            {
                var flap=Box("Angerissene Panzerplatte / noch am Rahmen",scar,new Vector3((i-1)*.49f,.13f+i*.025f,(i-1)*.22f),new Vector3(.58f,.065f,2.3f-i*.31f),alloy);
                flap.localRotation=Quaternion.Euler(direction*(9+i*7),i*8-8,direction*(6+i*5));
                Box("Verbliebene Randverschraubung",scar,new Vector3((i-1)*.49f,.08f,-1.22f),new Vector3(.11f,.10f,.11f),copper);
            }
            Combine(scar);
        }
        private void BuildFeed(Transform parent,int index)
        {
            // Fixed station pipework follows the deck axis, independent of the drone's
            // heading. Turning a parked hull must never move its floor supply riser.
            var drone=scenario.Drones[index];var node=Group("D"+(index+1).ToString("00")+" / Wasser und Strom",parent,layout.transform.InverseTransformPoint(drone.HomePosition),Quaternion.Euler(0,30,0));
            // The compact riser stays inside the 3.4-m planned dock footprint;
            // the intervening full-size drone aisles remain unobstructed.
            float supply=RingWalkthrough.BerthSupplyOffset;
            Box("Bodengebundene Versorgungssaeule",node,new Vector3(supply,-.12f,-1.15f),new Vector3(.32f,1.15f,.45f),dark);
            Box("Wasserabsperrblock",node,new Vector3(supply,.05f,-1.42f),new Vector3(.34f,.26f,.16f),alloy);
            Box("Elektrischer Lasttrenner",node,new Vector3(supply,-.25f,-1.42f),new Vector3(.34f,.2f,.16f),alloy);
            Pipe("Wasserzulauf zur Aufnahme",node,WaterFeedPoint,new Vector3(1.2f,-.5f,-1.15f),.10f,blue);
            Pipe("Isoliertes Ladekabel zur Aufnahme",node,PowerFeedPoint,new Vector3(1.18f,-.6f,-.8f),.075f,cable);
            var contact=Group("Verriegelte Schnellkupplung am Dock",node,new Vector3(1.22f,-.45f,-1),Quaternion.identity);
            Box("Getrennte Medienkupplung",contact,Vector3.zero,new Vector3(.25f,.38f,.4f),alloy);
            Box("Wasserkontakt / dicht geschlossen",contact,new Vector3(-.14f,.09f,0),new Vector3(.06f,.1f,.1f),blue);
            Box("Leistungskontakt / geschuetzt",contact,new Vector3(-.14f,-.1f,0),new Vector3(.06f,.1f,.13f),copper);
            var power=Box("Strom / Status",node,new Vector3(supply-.12f,.34f,-1.39f),new Vector3(.12f,.08f,.03f),signal).GetComponent<Renderer>();
            var water=Box("Wasser / Status",node,new Vector3(supply+.11f,.34f,-1.39f),new Vector3(.12f,.08f,.03f),signal).GetComponent<Renderer>();
            StationDisplay.Create("D"+(index+1).ToString("00")+" / H₂O · STROM",node,new Vector3(supply,.55f,-1.15f),Quaternion.identity,.032f,font,letters,dark,blue,25,.65f);
            var feed=new Feed{drone=drone,root=node,power=power,water=water,contact=contact,
                fixedWaterTap=layout.transform.InverseTransformPoint(node.TransformPoint(WaterFeedPoint)),
                fixedPowerTap=layout.transform.InverseTransformPoint(node.TransformPoint(PowerFeedPoint))};
            // Independent clusters can move with their own planned berth. A single
            // cross-dock batch would leave the visible connections at their old poses.
            Combine(node);node.SetParent(layout.Geometry.CampaignBerths[index],true);
            feed.waterBridge=BuildBridge(parent,"D"+(index+1).ToString("00")+" / Wasser-Verbindungsleitung",blue,.1f);
            feed.powerBridge=BuildBridge(parent,"D"+(index+1).ToString("00")+" / Strom-Verbindungsleitung",cable,.075f);
            feeds.Add(feed);
        }
        private Transform[] BuildBridge(Transform parent,string name,Material material,float width)
        {
            var bridge=Group(name,parent,Vector3.zero,Quaternion.identity);var result=new Transform[3];
            for(int i=0;i<result.Length;i++)
            {result[i]=Box("Flexible Medienverbindung / Teil "+(i+1),bridge,Vector3.zero,new Vector3(width,1,width),material);result[i].gameObject.SetActive(false);}
            return result;
        }
        internal void BindFeedToBerth(int index,Transform berth)
        {
            if(index<0||index>=feeds.Count||berth==null)return;
            var node=feeds[index].root;
            if(node.parent==berth)return;
            Vector3 position=node.localPosition,scale=node.localScale;Quaternion rotation=node.localRotation;
            node.SetParent(berth,false);node.localPosition=position;node.localRotation=rotation;node.localScale=scale;
        }
        private void RefreshBridge(Transform[] links,Vector3 start,Vector3 end)
        {
            bool moved=(end-start).sqrMagnitude>.0001f;
            // New runs use the service layer below the walking deck, with only
            // their risers exposed. A moved dock must not string a pipe across an aisle.
            var points=new[]{start,new Vector3(start.x,-.18f,start.z),new Vector3(end.x,-.18f,end.z),end};
            for(int i=0;i<links.Length;i++)
            {
                var link=links[i];link.gameObject.SetActive(moved);if(!moved)continue;
                Vector3 a=layout.transform.TransformPoint(points[i]),b=layout.transform.TransformPoint(points[i+1]);
                link.SetPositionAndRotation((a+b)*.5f,Quaternion.FromToRotation(Vector3.up,b-a));
                Vector3 size=link.localScale;size.y=Vector3.Distance(a,b);link.localScale=size;
            }
        }
        private void BuildRupture(string name,string element,int sector,Vector3 point,float heading,int style)
        {
            var root=Group(name,transform,point,Quaternion.Euler(0,heading,0));
            // The tray bolts remain attached, while two torn cover wings expose the break.
            Box("Befestigte Kabelkanal-Unterseite",root,Vector3.zero,new Vector3(2.8f,.13f,.8f),dark);
            for(int side=-1;side<=1;side+=2)
            {
                float x=side*.96f;var cover=Box("Angerissene Schutzhaut / befestigt",root,new Vector3(x,.17f,.18f),new Vector3(.9f,.09f,.95f),alloy);
                cover.localRotation=Quaternion.Euler(side*(17+style*4),side*11,side*(23+style*7));
                Box("Am Rahmen verschraubter Halter",root,new Vector3(side*1.28f,-.01f,0),new Vector3(.16f,.35f,.95f),alloy);
                foreach(float z in new[]{-.22f,.15f})
                {
                    Pipe("Gerissene Isolation",root,new Vector3(side*1.3f,.14f,z),new Vector3(side*.42f,.19f,z+side*.09f),.075f,cable);
                    Pipe("Freiliegendes Kupfer / Bruchende",root,new Vector3(side*.42f,.19f,z+side*.09f),new Vector3(side*.16f,.29f,z-side*.035f),.045f,copper);
                }
            }
            var arc=new GameObject("Sichtbarer kurzer Lichtbogen");arc.layer=25;arc.transform.SetParent(root,false);
            var line=arc.AddComponent<LineRenderer>();line.useWorldSpace=false;line.sharedMaterial=arcSurface;line.positionCount=5;line.startWidth=.032f;line.endWidth=.019f;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
            line.SetPositions(new[]{new Vector3(-.18f,.29f,-.2f),new Vector3(-.06f,.40f,-.11f),new Vector3(.015f,.31f,-.22f),new Vector3(.08f,.45f,-.17f),new Vector3(.18f,.29f,-.18f)});
            ruptures.Add(new Rupture{root=root,element=element,sector=sector,arc=line,phase=style*.67f});Combine(root);
        }
        private void Combine(Transform parent)
        {
            // Static details cost one draw per material per cluster. Indicators and sparks stay independent.
            foreach(var material in materials)
            {
                if(material==signal||material==letters||material==arcSurface)continue;
                var parts=new List<CombineInstance>();var sources=new List<Renderer>();
                foreach(var filter in parent.GetComponentsInChildren<MeshFilter>())
                {
                    var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null||!renderer.enabled||renderer.sharedMaterial!=material)continue;
                    parts.Add(new CombineInstance{mesh=filter.sharedMesh,transform=parent.worldToLocalMatrix*filter.transform.localToWorldMatrix});sources.Add(renderer);
                }
                if(parts.Count<2)continue;
                var mesh=new Mesh{name="Gebundene Dockdetails / "+material.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(parts.ToArray(),true,true);meshes.Add(mesh);
                var combined=Group(mesh.name,parent,Vector3.zero,Quaternion.identity);combined.gameObject.layer=25;combined.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;combined.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
                foreach(var renderer in sources)renderer.enabled=false;
            }
        }
        private void FontRebuilt(Font rebuilt){if(rebuilt==font&&letters!=null)letters.mainTexture=font.material.mainTexture;}
        private void Update()
        {
            if(Time.unscaledTime>=nextRefresh){Refresh();nextRefresh=Time.unscaledTime+.2f;}
            RefreshDamage(Time.unscaledTime);
        }
        internal void RefreshDamage(float clock)
        {
            foreach(var rupture in ruptures)
            {
                var operations=scenario.Operations;var e=operations?.Element(rupture.element);var circuit=operations?.Element("sector-"+rupture.sector);
                bool damaged=e==null||e.condition<.6f;rupture.root.gameObject.SetActive(damaged);
                // A local breaker isolates its severed branch. No flames or smoke in vacuum.
                bool live=operations==null||circuit!=null&&circuit.power&&operations.State.power.LifePowered;
                float cycle=Mathf.Repeat(clock+rupture.phase,3.6f);
                rupture.arc.enabled=damaged&&live&&(cycle<.12f||cycle>.24f&&cycle<.29f);
            }
        }
        internal void Refresh()
        {
            if(scenario==null)return;var operations=scenario.Operations;
            foreach(var feed in feeds)
            {
                RefreshBridge(feed.waterBridge,feed.fixedWaterTap,layout.transform.InverseTransformPoint(feed.root.TransformPoint(WaterFeedPoint)));
                RefreshBridge(feed.powerBridge,feed.fixedPowerTap,layout.transform.InverseTransformPoint(feed.root.TransformPoint(PowerFeedPoint)));
                feed.Connected=feed.drone.IsOperational&&(feed.drone.transform.position-feed.drone.HomePosition).sqrMagnitude<.09f;
                feed.Power=feed.Connected&&(operations==null||operations.Powered(1)&&operations.State.power.LifePowered);
                feed.Water=feed.Connected&&scenario.WaterLiters>0&&(operations==null||operations.WaterConnected(1));
                Status(feed.power,feed.Power);Status(feed.water,feed.Water);
            }
        }
        private static void Status(Renderer renderer,bool supplied)
        {
            var color=supplied?new Color(.1f,.8f,.58f):new Color(.35f,.16f,.06f);var block=new MaterialPropertyBlock();block.SetColor("_Color",color);block.SetColor("_EmissionColor",supplied?color*1.2f:color*.12f);renderer.SetPropertyBlock(block);
        }
        private void OnDestroy(){Font.textureRebuilt-=FontRebuilt;foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);foreach(var material in materials)if(material!=null)Destroy(material);}
    }
}
