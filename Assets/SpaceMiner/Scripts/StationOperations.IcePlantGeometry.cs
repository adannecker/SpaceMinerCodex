using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceMiner
{
    public sealed partial class StationOperations
    {
        private const float IceHalfAngle = 4.85f, IceNear = 39.45f, IceFar = 51, IceSkin = .12f;
        private readonly List<Transform> iceCells = new List<Transform>(), iceFills = new List<Transform>();
        private readonly List<StationDisplay> iceCellDisplays = new List<StationDisplay>();
        private readonly HashSet<Transform> iceMoving = new HashSet<Transform>();
        private readonly List<Material> iceMaterials = new List<Material>();
        private readonly Transform[] iceRotors = new Transform[2];
        private readonly float[] iceCellNear = {39.57f,43.40f,47.17f}, iceCellFar = {43.28f,47.05f,50.88f};
        private IcePlantVisualResources iceResources;
        private Material icePaint, iceDark, iceSteel, iceBlue, iceGlass, iceSignal, iceRubble;
        private Transform iceRam, icePump, iceBufferLevel, iceResidueLevel;
        private StationDisplay iceIntakeDisplay, iceProcessorDisplay, iceResidueDisplay, iceMainDisplay;
        private double iceCrusherClock, icePressClock, iceProcessorClock;
        private float iceDisplayClock;
        public Transform IcePlantFrame { get; private set; }
        public IReadOnlyList<Transform> IcePlantColdCells => iceCells;
        public IReadOnlyList<Transform> IcePlantColdFill => iceFills;
        public Transform IcePlantCrusher { get; private set; }
        public Transform IcePlantCompactor { get; private set; }
        public Transform IcePlantProcessor { get; private set; }
        public Transform IcePlantBuffer { get; private set; }
        public Transform IcePlantResidue { get; private set; }
        public Transform IcePlantInletChannel { get; private set; }
        public StationTerminal IcePlantTerminal { get; private set; }
        public Vector3[] IcePlantEntryPath
        {
            get
            {
                if(IcePlantFrame==null)return Array.Empty<Vector3>();
                // Turn behind the z38.44 gate shoulders, before the closed tank
                // starts at z39.45. The centre line is not an aisle through the tank.
                return new[]{IcePlantFrame.TransformPoint(new Vector3(0,.03f,37.2f)),
                    IcePlantFrame.TransformPoint(new Vector3(0,.03f,38.9f)),
                    IcePlantFrame.TransformPoint(new Vector3(IceServiceX(38.9f),.03f,38.9f)),
                    IcePlantServicePath[0]};
            }
        }
        public Vector3[] IcePlantServicePath
        {
            get
            {
                if(IcePlantFrame==null)return Array.Empty<Vector3>();
                float[] z={39.15f,40,42,44,46,48,50,51.2f,52,53.1f};var points=new Vector3[z.Length];
                for(int i=0;i<z.Length;i++)points[i]=IcePlantFrame.TransformPoint(new Vector3(IceServiceX(z[i]),.03f,z[i]));
                return points;
            }
        }
        private static float IceCos => Mathf.Cos(IceHalfAngle*Mathf.Deg2Rad);
        private static float IceTan => Mathf.Tan(IceHalfAngle*Mathf.Deg2Rad);
        private static float IceLeft(float z,bool inner=false)=>-z*IceTan+(.185f+.45f+(inner?IceSkin:0))/IceCos;
        private static float IceRight(float z,bool inner=false)=>z*IceTan-(.185f+1+(inner?IceSkin:0))/IceCos;
        private static float IceServiceX(float z)=>z*IceTan-(.185f+.5f)/IceCos;
        private Material IceSurface(string name,Material source)
        {
            var material=new Material(source){name=name,enableInstancing=true};iceMaterials.Add(material);iceResources.Materials.Add(material);return material;
        }
        private Transform IceShape(string name,Transform parent,Vector3 position,Vector3 size,Material material,bool solid=false,PrimitiveType type=PrimitiveType.Cube)
        {
            var part=Shape(name,parent,position,size,material,solid,type);var renderer=part.GetComponent<Renderer>();
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return part;
        }
        private Transform IcePrism(string name,Transform parent,Vector2[] footprint,float bottom,float top,Material material,bool solid)
        {
            var vertices=new List<Vector3>();var indices=new List<int>();
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d){int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});indices.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            var low=new Vector3[4];var high=new Vector3[4];
            for(int i=0;i<4;i++){low[i]=new Vector3(footprint[i].x,bottom,footprint[i].y);high[i]=new Vector3(footprint[i].x,top,footprint[i].y);}
            Quad(low[0],low[1],low[2],low[3]);Quad(high[3],high[2],high[1],high[0]);
            for(int i=0;i<4;i++){int j=(i+1)%4;Quad(low[i],high[i],high[j],low[j]);}
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();iceResources.Meshes.Add(mesh);
            var root=Group(name,parent,Vector3.zero,Quaternion.identity);root.gameObject.layer=25;
            root.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=root.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            if(solid)root.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh;
            return root;
        }
        private static Vector2[] IceFootprint(float near,float far,bool inner=false)=>new[]{new Vector2(IceLeft(near,inner),near),new Vector2(IceRight(near,inner),near),new Vector2(IceRight(far,inner),far),new Vector2(IceLeft(far,inner),far)};
        private static Vector2[] IceWallFootprint(float near,float far,bool right)
        {
            float Outer(float z)=>right?IceRight(z):IceLeft(z);
            float Inner(float z)=>right?IceRight(z,true):IceLeft(z,true);
            return right?new[]{new Vector2(Inner(near),near),new Vector2(Outer(near),near),new Vector2(Outer(far),far),new Vector2(Inner(far),far)}
                :new[]{new Vector2(Outer(near),near),new Vector2(Inner(near),near),new Vector2(Inner(far),far),new Vector2(Outer(far),far)};
        }
        private StationDisplay IceDisplay(string caption,Transform parent,Vector3 position,Quaternion rotation,float width=1.5f)
            =>StationDisplay.Create(caption,parent,position,rotation,.055f,signageFont,signageMaterial,iceDark,iceSignal,25,width);
        private void IcePipe(string name,Transform parent,Vector3 start,Vector3 end,float diameter,Material material,bool solid=false)
        {
            var delta=end-start;var pipe=IceShape(name,parent,(start+end)*.5f,new Vector3(diameter,delta.magnitude*.5f,diameter),material,solid,PrimitiveType.Cylinder);
            pipe.localRotation=Quaternion.FromToRotation(Vector3.up,delta);
            foreach(float t in new[]{.08f,.92f}){var flange=IceShape(name+" / collar",parent,Vector3.Lerp(start,end,t),new Vector3(diameter*1.45f,.035f,diameter*1.45f),iceSteel,false,PrimitiveType.Cylinder);flange.localRotation=pipe.localRotation;}
        }
        private void IceDuct(string name,Transform parent,Vector3 start,Vector3 end,float width,float height)
        {
            var duct=Group(name,parent,(start+end)*.5f,Quaternion.LookRotation(end-start));float length=(end-start).magnitude;
            foreach(float side in new[]{-1f,1f})
            {
                IceShape("Closed feed duct / side",duct,new Vector3(side*(width*.5f+.035f),0,0),new Vector3(.07f,height+.14f,length),icePaint,true);
                IceShape("Closed feed duct / cover",duct,new Vector3(0,side*(height*.5f+.035f),0),new Vector3(width,.07f,length),iceSteel,true);
            }
        }
        private void BuildIcePlantGeometry()
        {
            if(!Live||IcePlantFrame!=null)return;
            var old=Layout.transform.Find("Campaign water storage / Stage 1");
            if(old!=null)foreach(Transform part in old)if(part.name=="Water Ice Tank"||part.name=="Tank cap"||part.name=="Tank status"||part.name=="Transfer pipe")part.gameObject.SetActive(false);
            foreach(var part in Layout.Geometry.Root.GetComponentsInChildren<Transform>(true))if(part.name=="Eis-Wasserlager / Behaelter")part.gameObject.SetActive(false);
            IcePlantFrame=Group("Eisannahme 01 / geschlossene Kaltlageranlage",presentation,Vector3.zero,Quaternion.Euler(0,200,0));Selectable(IcePlantFrame,"water");
            iceResources=IcePlantFrame.gameObject.AddComponent<IcePlantVisualResources>();
            icePaint=IceSurface("Eislager / isolierte blaue Kassetten",alloy);iceDark=IceSurface("Eislager / geschlossene Graphitrahmen",graphite);iceSteel=IceSurface("Eislager / mechanische Fuehrungen",alloy);iceSteel.color=new Color(.4f,.46f,.49f);
            // Light matte paint stays readable under the real, capped interior
            // work lights; no emissive replacement of the physical lighting.
            icePaint.color=new Color(.35f,.43f,.47f);icePaint.SetFloat("_Metallic",.18f);icePaint.SetFloat("_Glossiness",.16f);
            iceDark.color=new Color(.15f,.19f,.22f);iceDark.SetFloat("_Metallic",.16f);iceDark.SetFloat("_Glossiness",.15f);
            iceSteel.color=new Color(.48f,.53f,.55f);iceSteel.SetFloat("_Metallic",.35f);iceSteel.SetFloat("_Glossiness",.18f);
            iceBlue=IceSurface("Eislager / gereinigtes Wasser",blue);iceSignal=IceSurface("Eislager / ruhige Statusleisten",cyan);iceRubble=IceSurface("Eislager / zurueckgehaltenes Rohgemisch",blue);iceRubble.color=new Color(.44f,.65f,.70f);
            iceGlass=new Material(Resources.Load<Shader>("CargoGlass")){name="Eislager / einzelne Drucksichtscheibe",color=new Color(.22f,.62f,.72f,.14f)};iceMaterials.Add(iceGlass);iceResources.Materials.Add(iceGlass);
            for(int i=0;i<3;i++)BuildIceColdCell(i);
            BuildIceProcessMachines();BuildIceServiceTerminal();
            // Embedded lighting uses the existing capped light budget, without shadows.
            foreach(float z in new[]{42f,50f,52.1f})
            {
                int lightIndex=lamps.Count;
                LightAt(Quaternion.Euler(0,200,0)*new Vector3(IceServiceX(z),4.95f,z),new Color(.65f,.80f,.88f),1.0f,10);
                lamps[lightIndex].transform.localRotation=Quaternion.Euler(0,200,0);
                // Visible hangers connect each actual fixture to the existing roof.
                foreach(float side in new[]{-.2f,.2f})
                {
                    IceShape("Arbeitsleuchte / befestigter Deckenabhaenger",IcePlantFrame,new Vector3(IceServiceX(z)+side,5.20f,z),new Vector3(.045f,.43f,.045f),iceSteel);
                    IceShape("Arbeitsleuchte / Deckenanker",IcePlantFrame,new Vector3(IceServiceX(z)+side,5.405f,z),new Vector3(.14f,.025f,.16f),iceSteel);
                }
            }
            BatchIcePlantStatic();UpdateIcePlantVisuals(0);
        }
        private void BuildIceColdCell(int index)
        {
            float near=iceCellNear[index],far=iceCellFar[index],outerNear=index==0?IceNear:near-IceSkin*.5f,outerFar=index==2?IceFar:far+IceSkin*.5f;
            var cell=Group("Kaltzelle "+(index+1).ToString("00")+" / isolierter Keilspeicher",IcePlantFrame,Vector3.zero,Quaternion.identity);iceCells.Add(cell);
            IcePrism("Kaltzelle / 12cm Boden und Isolation",cell,IceFootprint(outerNear,outerFar),.25f,.37f,iceDark,true);
            IcePrism("Kaltzelle / 12cm geschlossene Decke",cell,IceFootprint(outerNear,outerFar),4.73f,4.85f,icePaint,true);
            IcePrism("Kaltzelle / linke Medienwand",cell,IceWallFootprint(outerNear,outerFar,false),.37f,4.73f,icePaint,true);
            IcePrism("Kaltzelle / rechte untere Druckwand",cell,IceWallFootprint(outerNear,outerFar,true),.37f,.95f,icePaint,true);
            IcePrism("Kaltzelle / rechte obere Druckwand",cell,IceWallFootprint(outerNear,outerFar,true),2.25f,4.73f,icePaint,true);
            float windowNear=near+.30f,windowFar=far-.30f;
            IcePrism("Kaltzelle / Fensterschulter innen",cell,IceWallFootprint(outerNear,windowNear,true),.95f,2.25f,icePaint,true);
            IcePrism("Kaltzelle / Fensterschulter aussen",cell,IceWallFootprint(windowFar,outerFar,true),.95f,2.25f,icePaint,true);
            IcePrism("Kaltzelle / isolierte Drucksichtscheibe",cell,IceWallFootprint(windowNear,windowFar,true),.95f,2.25f,iceGlass,true);
            if(index==0)IcePrism("Kaltzelle / ringseitige Abschlusswand",cell,IceFootprint(IceNear,near),.37f,4.73f,icePaint,true);
            if(index==2)IcePrism("Kaltzelle / technikseitige Abschlusswand",cell,IceFootprint(far,IceFar),.37f,4.73f,icePaint,true);
            else IcePrism("Kaltzelle / 12cm innere Trennwand",cell,IceFootprint(far,far+IceSkin),.37f,4.73f,iceSteel,true);
            foreach(float z in new[]{near+.4f,far-.4f})
            {
                float left=IceLeft(z),right=IceRight(z);IceShape("Tankunterbau / Lastverteilung",cell,new Vector3((left+right)*.5f,.15f,z),new Vector3(right-left,.20f,.28f),iceSteel,true);
                IcePipe("Kuehl- und Absperrleitung",cell,new Vector3(left+.04f,4.5f,z),new Vector3(right-.03f,4.5f,z),.06f,iceBlue);
            }
            var fill=IcePrism("Kaltzelle / tatsaechlich gespeichertes Roh-Eis",cell,IceFootprint(near+.02f,far-.02f,true),0,4.34f,iceRubble,false);
            fill.localPosition=Vector3.up*.38f;fill.gameObject.SetActive(false);iceFills.Add(fill);iceMoving.Add(fill);
            float middle=(near+far)*.5f;var heading=Quaternion.Euler(0,IceHalfAngle-90,0);
            var display=IceDisplay("ZELLE "+(index+1).ToString("00")+" | 000.0 t\nBELEGT 000.0 %",cell,new Vector3(IceRight(middle)-.025f,2.65f,middle),heading,1.9f);iceCellDisplays.Add(display);
            var valve=Group("Zelle / absperrbare Servicekupplung",cell,new Vector3(IceRight(middle)-.08f,3.3f,middle),heading);
            IceShape("Versenkte Absperrkassette",valve,Vector3.zero,new Vector3(.55f,.4f,.12f),iceSteel);
            IceShape("Handrad / Zellenisolation",valve,new Vector3(0,0,-.085f),new Vector3(.25f,.025f,.25f),iceBlue,false,PrimitiveType.Cylinder).localRotation=Quaternion.Euler(90,0,0);
            IceShape("Gangseitige Leuchtlinie",cell,new Vector3(IceRight(middle)+.005f,4.65f,middle),new Vector3(.025f,.035f,far-near-.3f),iceSignal).localRotation=Quaternion.Euler(0,IceHalfAngle,0);
        }
        private void BuildIceProcessMachines()
        {
            IcePlantCrusher=Group("Kaltbrecher / geschlossene Doppelwelle",IcePlantFrame,new Vector3(-2.3f,0,52.55f),Quaternion.identity);Selectable(IcePlantCrusher,"water");
            IceShape("Brecher / tragender Sockel",IcePlantCrusher,new Vector3(0,.18f,0),new Vector3(2.3f,.32f,1.45f),iceDark,true);
            IceShape("Brecher / geschlossene Unterwanne",IcePlantCrusher,new Vector3(0,.7f,0),new Vector3(2.2f,.8f,1.35f),icePaint,true);
            foreach(float x in new[]{-1.07f,1.07f})IceShape("Brecher / Seitenrahmen",IcePlantCrusher,new Vector3(x,1.4f,0),new Vector3(.12f,1.4f,1.4f),x<0?icePaint:iceGlass,true);
            foreach(float z in new[]{-.64f,.64f})IceShape("Brecher / dichte Stirnwand",IcePlantCrusher,new Vector3(0,1.45f,z),new Vector3(2.1f,1.25f,.09f),iceSteel,true);
            IceShape("Brecher / geschlossene Wartungsdecke",IcePlantCrusher,new Vector3(0,2.1f,0),new Vector3(2.3f,.16f,1.45f),icePaint,true);
            for(int i=0;i<2;i++)
            {
                var rotor=Group("Brecher / reale Welle "+i,IcePlantCrusher,new Vector3((i==0?-1:1)*.46f,1.4f,0),Quaternion.identity);iceRotors[i]=rotor;iceMoving.Add(rotor);
                IceShape("Brecherwelle",rotor,Vector3.zero,new Vector3(.52f,.5f,.52f),iceSteel,false,PrimitiveType.Cylinder).localRotation=Quaternion.Euler(90,0,0);
                for(int tooth=0;tooth<6;tooth++){float a=tooth*60*Mathf.Deg2Rad;IceShape("Langsamer Brechzahn",rotor,new Vector3(Mathf.Cos(a)*.26f,Mathf.Sin(a)*.26f,0),new Vector3(.16f,.14f,.9f),iceDark).localRotation=Quaternion.Euler(0,0,tooth*60);}
            }
            iceIntakeDisplay=IceDisplay("KALTBRECHER\nANNAHME 0000 kg",IcePlantCrusher,new Vector3(1.14f,1.8f,0),Quaternion.Euler(0,-90,0),1.3f);
            IcePlantCompactor=Group("Verdichter / gefuehrter Kolbenschieber",IcePlantFrame,new Vector3(-2.3f,0,51.4f),Quaternion.identity);Selectable(IcePlantCompactor,"water");
            IceShape("Verdichter / Bodenlager",IcePlantCompactor,new Vector3(0,.5f,0),new Vector3(1.9f,.8f,.62f),iceDark,true);
            IceShape("Verdichter / geschlossene Presskammer",IcePlantCompactor,new Vector3(0,1.25f,0),new Vector3(1.85f,.75f,.62f),icePaint,true);
            // 2.35 m3 inner envelope after .12-m walls, above the crusher's
            // supported frame: sufficient for the separate 1500-kg 80/20 buffer.
            IceShape("Verdichter / geschlossener 1500kg Zwischenpuffer",IcePlantCompactor,new Vector3(0,2.85f,1.15f),new Vector3(2.15f,1.4f,1.3f),icePaint,true);
            foreach(float x in new[]{-.95f,.95f})IceShape("Zwischenpuffer / lasttragende Maschinenstuetze",IcePlantCompactor,new Vector3(x,2.25f,1.15f),new Vector3(.14f,.65f,1.3f),iceSteel,true);
            iceRam=Group("Verdichter / realer kleiner Hub",IcePlantCompactor,new Vector3(0,1.25f,0),Quaternion.identity);iceMoving.Add(iceRam);
            IceShape("Sichtbare Kolbenstange",iceRam,Vector3.zero,new Vector3(.2f,.2f,.7f),iceSteel);
            IceDuct("Brecher / geschlossener Vorschub",IcePlantFrame,new Vector3(-2.3f,1.2f,51.88f),new Vector3(-2.3f,1.2f,51.70f),.55f,.45f);
            IceDuct("Verdichter / Zelle03 Anschluss",IcePlantFrame,new Vector3(-2.3f,1.25f,51.10f),new Vector3(-2.3f,1.25f,50.96f),.55f,.45f);
            IcePlantInletChannel=Group("Receiver / geschlossener gefuehrter Umlenker",IcePlantFrame,Vector3.zero,Quaternion.identity);
            // The last receiver opening remains at z51.5. The shallow sealed head
            // takes material sideways without changing the outer drone coupling.
            IceShape("Umlenker / hintere Druckplatte",IcePlantInletChannel,new Vector3(0,3.3f,51.05f),new Vector3(1.82f,1.12f,.08f),icePaint,true);
            foreach(float side in new[]{-1f,1f})IceShape("Umlenker / geschlossene Kante",IcePlantInletChannel,new Vector3(0,3.3f+side*.54f,51.29f),new Vector3(1.82f,.08f,.48f),iceSteel,true);
            IceShape("Umlenker / rechter Abschluss",IcePlantInletChannel,new Vector3(.87f,3.3f,51.29f),new Vector3(.08f,1,.48f),icePaint,true);
            IceDuct("Gefuehrter kalter Seitenkanal",IcePlantInletChannel,new Vector3(-.91f,3.3f,51.29f),new Vector3(-2.3f,3.3f,51.29f),.34f,.72f);
            IceDuct("Kalter Kanal / Maschinenzulauf",IcePlantInletChannel,new Vector3(-2.3f,3.3f,51.29f),new Vector3(-2.3f,3.3f,52.55f),.55f,.65f);
            IceDuct("Gefuehrter Brechereinlass",IcePlantInletChannel,new Vector3(-2.3f,3.3f,52.55f),new Vector3(-2.3f,2.15f,52.55f),.55f,.65f);
            BuildIceResidueAndWater();
            // Media remain in the left .45-m gap or above head height, never on the floor aisle.
            for(int i=0;i<3;i++)
            {
                float near=39.7f,far=51.3f;Vector3 a=new Vector3(IceLeft(near)-.22f,1.0f+i*.24f,near),b=new Vector3(IceLeft(far)-.22f,1.0f+i*.24f,far);
                IcePipe(i==0?"Kaltlager / geschlossener Verteiler":i==1?"Kuehlmittel / Ruecklauf":"Strom / geschuetzte Medienfuehrung",IcePlantFrame,a,b,i==0?.16f:.075f,i==2?iceDark:iceBlue);
            }
        }
        private void BuildIceResidueAndWater()
        {
            IcePlantResidue=Group("Reststoffspeicher / geschlossene 45t Kassette",IcePlantFrame,new Vector3(1.15f,0,52.43f),Quaternion.identity);Selectable(IcePlantResidue,"water");
            IceShape("Reststoffspeicher / vollstaendig geschlossene Huelle",IcePlantResidue,new Vector3(0,2.55f,0),new Vector3(3.15f,4.6f,2.30f),iceDark,true);
            foreach(float z in new[]{-.95f,.95f})IceShape("Reststoffspeicher / bodenfester Rahmen",IcePlantResidue,new Vector3(0,.15f,z),new Vector3(3.15f,.2f,.24f),iceSteel,true);
            foreach(float y in new[]{.6f,2.55f,4.5f})IceShape("Reststoffspeicher / Lastgurt",IcePlantResidue,new Vector3(0,y,0),new Vector3(3.19f,.1f,2.34f),icePaint);
            iceResidueDisplay=IceDisplay("RESTSTOFF\n00000 / 45000 kg",IcePlantResidue,new Vector3(1.64f,3.6f,0),Quaternion.Euler(0,-90,0),1.5f);
            iceResidueLevel=IceShape("Reststoff / echter Vorratsbalken",IcePlantResidue,new Vector3(1.64f,.6f,0),new Vector3(.025f,.035f,1.4f),iceSignal);iceMoving.Add(iceResidueLevel);
            IcePlantProcessor=Group("Aufbereiter / bedarfsweise Schmelz- und Filterkassette",IcePlantFrame,new Vector3(2.99f,0,51.93f),Quaternion.identity);Selectable(IcePlantProcessor,"water");
            IceShape("Aufbereiter / bodenfeste Stuetze",IcePlantProcessor,new Vector3(0,.55f,0),new Vector3(.38f,1.0f,.75f),iceSteel,true);
            IceShape("Aufbereiter / thermische Trennkammer",IcePlantProcessor,new Vector3(0,1.55f,0),new Vector3(.4f,1.05f,.8f),icePaint,true);
            IceShape("Aufbereiter / Filterkassette",IcePlantProcessor,new Vector3(0,2.25f,0),new Vector3(.4f,.32f,.8f),iceBlue,true);
            icePump=Group("Aufbereiter / ruhiger Pumpenantrieb",IcePlantProcessor,new Vector3(.17f,1.6f,0),Quaternion.identity);iceMoving.Add(icePump);
            IceShape("Pumpenkupplung",icePump,Vector3.zero,new Vector3(.15f,.035f,.15f),iceSteel,false,PrimitiveType.Cylinder).localRotation=Quaternion.Euler(0,0,90);
            IceShape("Aufbereiter / Displayhalter",IcePlantProcessor,new Vector3(0,2.55f,0),new Vector3(.12f,.4f,.3f),iceSteel);
            iceProcessorDisplay=IceDisplay("AUFBEREITUNG\nBEREIT",IcePlantProcessor,new Vector3(.15f,2.75f,0),Quaternion.Euler(0,-90,0),1.1f);
            IcePlantBuffer=Group("Verbrauchswasser / separater 200L Puffer",IcePlantFrame,new Vector3(3.015f,0,53.05f),Quaternion.identity);Selectable(IcePlantBuffer,"water");
            // Outer .56 x 1.10 m; .02-m walls/endcaps and .95 operational
            // filling still leave more than 200 L available inside.
            IceShape("200L Puffer / senkrechter Druckzylinder",IcePlantBuffer,new Vector3(0,.95f,0),new Vector3(.56f,.55f,.56f),iceBlue,true,PrimitiveType.Cylinder);
            foreach(float y in new[]{.41f,1.49f})IceShape("200L Puffer / Enddeckel",IcePlantBuffer,new Vector3(0,y,0),new Vector3(.58f,.01f,.58f),iceSteel,true,PrimitiveType.Cylinder);
            foreach(float z in new[]{-.18f,.18f})IceShape("200L Puffer / Bodenfuss",IcePlantBuffer,new Vector3(0,.2f,z),new Vector3(.36f,.4f,.08f),iceSteel,true);
            iceBufferLevel=IceShape("200L Puffer / Wasserstand",IcePlantBuffer,new Vector3(.27f,.95f,0),new Vector3(.025f,.9f,.055f),iceSignal);iceMoving.Add(iceBufferLevel);
            IceShape("200L Puffer / Displaystuetze",IcePlantBuffer,new Vector3(0,1.65f,0),new Vector3(.1f,.3f,.08f),iceSteel);
            IceShape("200L Puffer / Displayhalter",IcePlantBuffer,new Vector3(.10f,1.8f,0),new Vector3(.3f,.05f,.08f),iceSteel);
            IceDisplay("VERBRAUCH\n200 L",IcePlantBuffer,new Vector3(.25f,1.8f,0),Quaternion.Euler(0,-90,0),.8f);
            IcePipe("Aufbereiter / gereinigter Wasseranschluss",IcePlantFrame,new Vector3(3.015f,1.0f,52.35f),new Vector3(3.015f,1.0f,52.77f),.06f,iceBlue);
            IcePipe("Verbrauchswasser / Stationsleitung",IcePlantFrame,new Vector3(3.015f,.45f,53.05f),new Vector3(2.7f,.45f,53.05f),.06f,iceBlue);
            IcePipe("Reststoff / geschlossener Rueckhalt",IcePlantFrame,new Vector3(2.79f,2.2f,51.93f),new Vector3(2.70f,2.2f,51.93f),.08f,iceSteel);
            IcePipe("Aufbereiter / Entnahme aus Kaltzelle03",IcePlantFrame,new Vector3(2.8f,2.6f,51.93f),new Vector3(2.8f,2.6f,50.95f),.08f,iceBlue);
        }
        private void BuildIceServiceTerminal()
        {
            float z=49.95f;var root=Group("Eisannahme / E-Produktionspult am Wartungsgang",IcePlantFrame,new Vector3(IceRight(z)+.03f,0,z),Quaternion.Euler(0,IceHalfAngle-90,0));
            IcePlantTerminal=root.gameObject.AddComponent<StationTerminal>();IcePlantTerminal.kind=StationTerminalKind.Production;IcePlantTerminal.target="ice-plant";IcePlantTerminal.label="Eisannahme und Aufbereitung";terminals.Add(IcePlantTerminal);
            IceShape("Wandbuendiges Produktionspult",root,new Vector3(0,1.35f,.025f),new Vector3(1.15f,.72f,.09f),iceDark);
            var touch=root.gameObject.AddComponent<StationConsoleTouchSurface>();
            touch.Configure(null,new Vector3(0,1.4f,-.0415f),Vector3.back,Vector3.up,new Vector2(.40f,.20f));
            iceMainDisplay=IceDisplay("EISANNAHME 01\nKALTLAGER / AUFBEREITUNG",root,new Vector3(0,1.4f,-.04f),Quaternion.identity,1.1f);
            IceDisplay("E  /  PRODUKTION",root,new Vector3(0,.95f,-.04f),Quaternion.identity,1.0f);
        }
        public void UpdateIcePlantVisuals(float seconds)
        {
            if(IcePlantFrame==null||IcePlant==null)return;var plant=IcePlant;float dt=Mathf.Max(0,seconds);
            if(plant.CrusherActive)iceCrusherClock+=dt;if(plant.CompactorActive)icePressClock+=dt;if(plant.ProcessorActive)iceProcessorClock+=dt;
            for(int i=0;i<iceRotors.Length;i++)iceRotors[i].localRotation=Quaternion.Euler(0,0,(float)(iceCrusherClock*36%360)*(i==0?1:-1));
            iceRam.localPosition=new Vector3(0,1.25f,Mathf.Sin((float)(icePressClock%20)*Mathf.PI*.3f)*.045f);
            icePump.localRotation=Quaternion.Euler((float)(iceProcessorClock*55%360),0,0);
            for(int i=0;i<iceFills.Count;i++)
            {
                float fraction=Mathf.Clamp01((float)(plant.State.cells[i].VolumeM3/StationIcePlant.CellCapacityM3(i)))*.95f;
                iceFills[i].gameObject.SetActive(fraction>.000001f);iceFills[i].localScale=new Vector3(1,fraction,1);
            }
            float water=Mathf.Clamp01(Scenario.WaterLiters/Mathf.Max(.001f,Scenario.TankCapacityLiters));
            iceBufferLevel.localScale=new Vector3(.025f,Mathf.Max(.003f,.9f*water),.055f);iceBufferLevel.localPosition=new Vector3(.27f,.5f+.45f*water,0);
            float rest=Mathf.Clamp01((float)(plant.ResidueKg/StationIcePlant.ResidueCapacityKg));iceResidueLevel.localScale=new Vector3(.025f,.035f,Mathf.Max(.003f,1.4f*rest));
            iceDisplayClock+=dt;if(dt>0&&iceDisplayClock<.5f)return;iceDisplayClock=0;
            void Caption(StationDisplay display,string text){if(display.Caption!=text||display.Front.text!=text||display.Back.text!=text)display.SetCaption(text);}
            for(int i=0;i<iceCellDisplays.Count;i++)Caption(iceCellDisplays[i],"ZELLE "+(i+1).ToString("00")+" | "+(plant.State.cells[i].RawKg/1000).ToString("000.0")+" t\nBELEGT "+(100*plant.State.cells[i].VolumeM3/StationIcePlant.CellCapacityM3(i)).ToString("000.0")+" %");
            Caption(iceIntakeDisplay,"KALTBRECHER\nANNAHME "+plant.IntakeKg.ToString("0000")+" kg");
            Caption(iceProcessorDisplay,"AUFBEREITUNG\n"+(plant.ProcessorActive?"IN BETRIEB":"BEREIT"));
            Caption(iceResidueDisplay,"RESTSTOFF\n"+plant.ResidueKg.ToString("00000")+" / "+StationIcePlant.ResidueCapacityKg.ToString("0")+" kg");
            Caption(iceMainDisplay,"EISANNAHME 01\n"+(plant.State.receivingEnabled?"ANNAHME FREI":"ANNAHME GESPERRT"));
        }
        private void BatchIcePlantStatic()
        {
            foreach(var material in iceMaterials)
            {
                if(material==iceGlass)continue;var combine=new List<CombineInstance>();var originals=new List<MeshRenderer>();
                foreach(var filter in IcePlantFrame.GetComponentsInChildren<MeshFilter>(true))
                {
                    var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null||renderer.sharedMaterial!=material||!renderer.enabled)continue;
                    bool moving=false;foreach(var dynamicRoot in iceMoving)if(filter.transform==dynamicRoot||filter.transform.IsChildOf(dynamicRoot)){moving=true;break;}if(moving)continue;
                    combine.Add(new CombineInstance{mesh=filter.sharedMesh,transform=IcePlantFrame.worldToLocalMatrix*filter.transform.localToWorldMatrix});originals.Add(renderer);
                }
                if(combine.Count<2)continue;var mesh=new Mesh{name="Eisannahme / gebuendelte feste Geometrie / "+material.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(combine.ToArray(),true,true);iceResources.Meshes.Add(mesh);
                var root=Group(mesh.name,IcePlantFrame,Vector3.zero,Quaternion.identity);root.gameObject.layer=25;root.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var combined=root.gameObject.AddComponent<MeshRenderer>();combined.sharedMaterial=material;combined.shadowCastingMode=ShadowCastingMode.Off;combined.receiveShadows=false;foreach(var renderer in originals)renderer.enabled=false;
            }
        }
    }
    internal sealed class IcePlantVisualResources:MonoBehaviour
    {
        internal readonly List<Mesh> Meshes=new List<Mesh>();internal readonly List<Material> Materials=new List<Material>();
        private void OnDestroy(){foreach(var mesh in Meshes)if(mesh!=null)Destroy(mesh);foreach(var material in Materials)if(material!=null)Destroy(material);}
    }
}
