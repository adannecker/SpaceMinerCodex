#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    // Explicit, disposable construction study. Never changes campaign inventory, ports or saves.
    public sealed class StationConstructionPreview : MonoBehaviour
    {
        private const string Evidence = "Logs/StationConstruction";
        private const float Cycle = 900, Inspection = 1800, Sector = 60, PanelWidth = 360;
        private const float Inside=RingWalkthrough.OuterRadius, Depth=RingWalkthrough.StorageStageDepth, Outside=Inside+Depth, Middle=(Inside+Outside)/2;
        private static readonly string[] Stages = { "Anschluss vorbereiten", "Tragwerk montieren", "Boden einsetzen", "Medien einziehen", "Druckhuelle schliessen", "Aussenschutz montieren", "Pruefen und abnehmen" };
        private static readonly string[] Details = {
            "Geschlossenes Stationsschott bleibt zu. Knoten und Anschlussrahmen setzen.",
            "Kurze Profile herstellen, transportieren, ausrichten und an den Knoten fuegen.",
            "Sandwich-Bodenplatten einzeln zwischen die Traeger einsetzen.",
            "Kabelkanaele und Rohrtraeger montieren. Spezialtechnik aus dem Lager einsetzen.",
            "Seiten, Stirnwand und Dach aus kleinen Paneelen schliessen und abdichten.",
            "Aussenlagen und austauschbare Schutzplatten auf Abstand montieren.",
            "Verbindungen pruefen, Dichtheit und Medienversorgung simuliert abnehmen." };
        private sealed class Piece
        {
            internal Transform Node;
            internal Vector3 Target, Scale, PackedSize;
            internal Quaternion TargetRotation, PackRotation;
            internal float Kg;
            internal int Stage;
            internal bool Stock;
        }
        private sealed class Batch { internal readonly List<Piece> Pieces = new List<Piece>(); internal int Stage; internal Transform Installed; }
        private readonly List<Piece> pieces = new List<Piece>();
        private readonly List<Batch> batches = new List<Batch>();
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<Material> materials = new List<Material>();
        private readonly SpaceMinerUi ui = new SpaceMinerUi();
        private Transform root, carrier, welder, printHead, fabricator, handler;
        private Transform transfer, innerLeft, innerRight, outerLeft, outerRight;
        private float innerOpening, outerOpening;
        private bool cutaway = true;
        private LineRenderer weldLine, printLine;
        private readonly List<LineRenderer> grippers = new List<LineRenderer>();
        private Camera view;
        private StationInteriorLayout layout;
        private WaterScenario scenario;
        private Material metal, hull, dark, cyan, amber;
        private Vector3 printer, pivot, lastMouse;
        private Vector2 scroll;
        private int job, completedPieces, checks;
        private float jobTime, inspectionTime, completedKg, speed = 1280, distance = 60, yaw = 242, pitch = 36;
        private bool ready, checking, paused, complete;
        private string failure;
        private float TotalKg { get { float result = 0; foreach (var p in pieces) result += p.Kg; return result; } }
        private float WorkDone => job * Cycle + jobTime + inspectionTime;
        private float TotalWork => batches.Count * Cycle + Inspection;
        private int Stage => job < batches.Count ? batches[job].Stage : 6;
        private float U => jobTime / Cycle;
        private int Printed => completedPieces + (job < batches.Count ? Mathf.FloorToInt(Mathf.Clamp01(U / .26f) * batches[job].Pieces.Count + .0001f) : 0);
        private int Installed => completedPieces + (job < batches.Count ? Mathf.FloorToInt(Mathf.Clamp01((U - .80f) / .16f) * batches[job].Pieces.Count + .0001f) : 0);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-stationConstructionPreview") >= 0 || Array.IndexOf(args, "-stationConstructionCheck") >= 0)
                new GameObject("Station construction / disposable study").AddComponent<StationConstructionPreview>();
        }
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1);
            checking = Array.IndexOf(Environment.GetCommandLineArgs(), "-stationConstructionCheck") >= 0;
            FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride = Path.GetFullPath(Evidence + "/Saves");
            CinematicLibrary.PreferencePrefix = "SpaceMiner.StationConstruction.";
            FindFirstObjectByType<StartMenu>().StartDemo(); FindFirstObjectByType<IntroSequence>().Skip();
            yield return null; yield return null;
            if (!Guard(() => {
                scenario = FindFirstObjectByType<WaterScenario>(); scenario.SetSimulationRate(0); scenario.enabled = false;
                scenario.Scanner.DismissDialogue(); scenario.Scanner.enabled = false;
                var interior = StationInteriorMode.Current; layout = interior.Layout; interior.Exit(true); interior.enabled = false;
                var orbit = FindFirstObjectByType<OrbitCamera>(); orbit.ShowHud = false; orbit.enabled = false;
                FindFirstObjectByType<WaterScenarioHud>().enabled = false;
                FindFirstObjectByType<RingConstructionSimulator>().enabled = false;
                var saves = FindFirstObjectByType<SaveGameMenu>(); saves.EndSession(); saves.enabled = false;
                view = Camera.main; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
                root = new GameObject("S03 / construction study - not saved").transform; root.SetParent(layout.transform, false);
                metal = Surface("Assembly frame", new Color(.38f, .48f, .54f), .65f);
                hull = Surface("Pressure shell", new Color(.24f, .34f, .43f), .5f);
                dark = Surface("Workshop graphite", new Color(.045f, .065f, .08f), .4f);
                cyan = Surface("Work status", new Color(.15f, .8f, .9f), .1f, true);
                amber = Surface("Assembly markings", new Color(.95f, .54f, .14f), .1f, true);
                BuildPlan(); BuildWorkshop(); BuildBatches(); BuildInstalledMeshes(); layout.Geometry.ConstructionCutaway(cutaway); ResetStudy(); ready = true; CameraPose();
            })) yield break;
            if (checking) yield return Validate();
        }
        private Material Surface(string name, Color color, float metallic, bool glow = false)
        {
            var material = new Material(Shader.Find("Standard")) { name = name, color = color };
            material.SetFloat("_Metallic", metallic); material.SetFloat("_Glossiness", .2f);
            if (glow) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * .7f); }
            materials.Add(material); return material;
        }
        private Transform Box(string name, Transform parent, Vector3 position, Vector3 size, Material material, Quaternion rotation = default)
        {
            var node = GameObject.CreatePrimitive(PrimitiveType.Cube); node.name = name; node.layer = 25;
            node.transform.SetParent(parent, false); node.transform.localPosition = position; node.transform.localScale = size;
            node.transform.localRotation = rotation.Equals(default(Quaternion)) ? Quaternion.identity : rotation;
            node.GetComponent<Renderer>().sharedMaterial = material; node.GetComponent<Collider>().enabled = false;
            return node.transform;
        }
        private void Add(Transform node, int stage, float kg, Quaternion packedRotation, bool stock = false)
        {
            var p = new Piece { Node = node, Target = node.localPosition, Scale = node.localScale, TargetRotation = node.localRotation,
                PackRotation = packedRotation, Kg = kg, Stage = stage, Stock = stock };
            var mesh = node.GetComponent<MeshFilter>().sharedMesh;
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var vertex in mesh.vertices) bounds.Encapsulate(packedRotation * Vector3.Scale(vertex, p.Scale));
            p.PackedSize = bounds.size; pieces.Add(p); node.gameObject.SetActive(false);
        }
        private void Beam(Vector3 a, Vector3 b)
        {
            int n = Mathf.CeilToInt(Vector3.Distance(a, b) / 2.2f);
            for (int i = 0; i < n; i++) {
                Vector3 from = Vector3.Lerp(a, b, (float)i / n), to = Vector3.Lerp(a, b, (float)(i + 1) / n);
                var node = Box("Profile / short segment", root, (from + to) / 2, new Vector3(.16f, Vector3.Distance(from, to), .16f), metal, Quaternion.FromToRotation(Vector3.up, to - from));
                Add(node, 1, 12, Quaternion.Euler(0, 0, 90));
            }
        }
        private void HullParts(float width, float height, Func<Vector2,float,Vector3> map, int stage, Material material, float kg)
        {
            foreach (var polygon in HoneycombHull.Cells(width,height)) {
                Vector2 centre=Vector2.zero; foreach(var p in polygon) centre+=p; centre/=polygon.Count;
                Vector3 position=map(centre,0), normal=map(centre,1)-position;
                Quaternion rotation=Quaternion.LookRotation(normal,map(centre+Vector2.up*.01f,0)-position);
                Quaternion inverse=Quaternion.Inverse(rotation);
                var mesh=HoneycombHull.Mesh("Individual clipped honeycomb panel",new[]{polygon},(p,d)=>inverse*(map(p,d)-position)); meshes.Add(mesh);
                var node=new GameObject("Hex panel / "+Stages[stage]).transform; node.SetParent(root,false); node.gameObject.layer=25;
                node.localPosition=position; node.localRotation=rotation; node.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
                node.gameObject.AddComponent<MeshRenderer>().sharedMaterials=new[]{material,metal};
                Add(node,stage,kg,Quaternion.Euler(90,0,0));
            }
        }
        private void DeckParts(float y, int stage, Material material)
        {
            float width=Middle*27*Mathf.Deg2Rad;
            HullParts(width,Depth,(p,d)=>RingWalkthrough.At(Inside+p.y,Sector-13.5f+p.x/Middle*Mathf.Rad2Deg,y+d),stage,material,stage==5?4:10);
        }
        private void BuildPlan()
        {
            foreach (float angle in new[] { Sector - 13.5f, Sector + 13.5f })
                foreach (float y in new[] { .25f, 2.75f, 5.3f })
                    Add(Box("Connector node", root, RingWalkthrough.At(38.2f, angle, y), new Vector3(.65f, .4f, .22f), metal, Quaternion.Euler(0, angle, 0)), 0, 6, Quaternion.Euler(90, 0, 0));
            foreach (float r in new[] { Inside+.4f, Middle, Outside-.2f }) {
                for (int i = 0; i < 12; i++) foreach (float y in new[] { .12f, 5.5f })
                    Beam(RingWalkthrough.At(r, Sector - 13.5f + i * 2.25f, y), RingWalkthrough.At(r, Sector - 13.5f + (i + 1) * 2.25f, y));
                foreach (float angle in new[] { Sector - 13.5f, Sector + 13.5f }) Beam(RingWalkthrough.At(r, angle, .12f), RingWalkthrough.At(r, angle, 5.5f));
            }
            foreach (float angle in new[] { Sector - 13.5f, Sector + 13.5f }) foreach (float y in new[] { .12f, 5.5f })
                Beam(RingWalkthrough.At(38.4f, angle, y), RingWalkthrough.At(Outside-.2f, angle, y));
            // Both layers cover roof, floor, side walls and the end wall, not just the roof.
            DeckParts(-.06f,2,metal); DeckParts(5.65f,4,hull);
            DeckParts(-.06f-HoneycombHull.StandOff,5,dark); DeckParts(5.65f+HoneycombHull.StandOff,5,dark);
            foreach(float side in new[]{-1f,1f}) foreach(bool outside in new[]{false,true}) {
                float angle=Sector+side*13.5f; Vector3 normal=Quaternion.Euler(0,angle,0)*Vector3.right*side;
                float offset=outside?HoneycombHull.StandOff:0;
                HullParts(Depth,5.5f,(p,d)=>RingWalkthrough.At(Inside+p.x,angle,p.y)+normal*(offset+d),outside?5:4,outside?dark:hull,outside?4:10);
            }
            foreach(bool outside in new[]{false,true}) {
                float r=Outside+(outside?HoneycombHull.StandOff:0);
                HullParts(r*27*Mathf.Deg2Rad,5.5f,(p,d)=>RingWalkthrough.At(r+d,Sector-13.5f+p.x/r*Mathf.Rad2Deg,p.y),outside?5:4,outside?dark:hull,outside?4:10);
            }
            for (int row = 0; row < 3; row++) foreach (float side in new[] { -1f, 1f })
                Add(Box("Media tray",root,RingWalkthrough.At(Inside+(row+.5f)*Depth/3,Sector+side*12,.45f),new Vector3(.22f,.16f,Depth/3-.2f),metal,Quaternion.Euler(0,Sector+side*12,0)),3,8,Quaternion.identity);
            for (int i = 0; i < 3; i++)
                Add(Box("Prebuilt control equipment",root,RingWalkthrough.At(Inside+.9f+i*Depth/3,Sector-11,1.5f),new Vector3(.6f,.4f,.3f),amber),3,15,Quaternion.Euler(90,0,0),true);
            var outline = new List<Vector3>();
            for (int i = 0; i <= 12; i++) outline.Add(RingWalkthrough.At(38, Sector - 13.5f + i * 2.25f, .08f));
            for (int i = 12; i >= 0; i--) outline.Add(RingWalkthrough.At(Outside, Sector - 13.5f + i * 2.25f, .08f));
            outline.Add(outline[0]); Line("Construction boundary", root, outline.ToArray(), cyan, .035f);
        }
        private LineRenderer Line(string name, Transform parent, Vector3[] points, Material material, float width)
        {
            var node = new GameObject(name); node.layer = 25; node.transform.SetParent(parent, false);
            var line = node.AddComponent<LineRenderer>(); line.useWorldSpace = false; line.sharedMaterial = material;
            line.positionCount = points.Length; line.SetPositions(points); line.startWidth = line.endWidth = width; return line;
        }
        private Transform Drone(string name, bool large)
        {
            var node = new GameObject(name).transform; node.SetParent(root, false);
            float k = large ? 1 : .5f;
            Box("Assembly drone body", node, Vector3.zero, new Vector3(1.4f, .65f, 1.5f) * k, metal);
            foreach (float x in new[] { -1f, 1f }) foreach (float z in new[] { -1f, 1f })
                Box("Thruster pod", node, new Vector3(x * .85f, 0, z * .7f) * k, new Vector3(.3f, .35f, .4f) * k, dark);
            Box("Tool and sensor bar", node, new Vector3(0, -.4f, .8f) * k, new Vector3(1, .12f, .2f) * k, amber);
            return node;
        }
        private void BuildWorkshop()
        {
            printer = RingWalkthrough.At(24.5f,30);
            Box("Internal fabrication bed",root,printer+Vector3.up*.5f,new Vector3(2.7f,.4f,2.7f),dark);
            foreach(float x in new[]{-1.5f,1.5f}) foreach(float z in new[]{-1.5f})
                Box("Printer upright",root,printer+new Vector3(x,2.1f,z),new Vector3(.12f,4.2f,.12f),metal);
            foreach(float x in new[]{-1.5f,1.5f}) Box("Printer cantilever rail",root,printer+new Vector3(x,4.2f,0),new Vector3(.12f,.15f,3.2f),metal);
            Box("Printer crossbeam",root,printer+new Vector3(0,4.2f,-1.5f),new Vector3(3.2f,.15f,.12f),metal);
            printHead=Box("Fabrication head",root,printer+Vector3.up*3.5f,new Vector3(.4f,.25f,.4f),amber);
            Box("Prepared feedstock",root,printer+Quaternion.Euler(0,30,0)*new Vector3(2,.7f,0),new Vector3(.6f,1.4f,2),hull);
            fabricator=Drone("B01 / internal fabrication",false);
            handler=Drone("B02 / internal staging",true);
            carrier=Drone("B03 / exterior multi-part transport",true);
            welder=Drone("B04 / exterior joining",false);
            Box("B01 fabrication arm",fabricator,new Vector3(.3f,-.4f,.4f),new Vector3(.1f,.65f,.1f),cyan);
            Box("B02 cargo support",handler,new Vector3(0,-2.19f,0),new Vector3(2.1f,.08f,2.1f),amber);
            Box("B03 cargo support",carrier,new Vector3(0,-2.34f,0),new Vector3(2.1f,.08f,2.1f),metal);
            foreach(float x in new[]{-1.1f,1.1f}) foreach(float z in new[]{-.85f,.85f}) {
                Box("B02 cargo restraint",handler,new Vector3(x,-1.05f,z),new Vector3(.10f,2.1f,.10f),amber);
                Box("B03 extended gripper",carrier,new Vector3(x,-1.15f,z),new Vector3(.12f,2.3f,.12f),cyan);
            }
            Box("B04 welding tool",welder,new Vector3(0,-.4f,.65f),new Vector3(.12f,.6f,.12f),amber);
            for(int i=0;i<4;i++) grippers.Add(Line("Cargo grip "+i,root,new[]{Vector3.zero,Vector3.zero},metal,.045f));
            weldLine=Line("Joining seam tool",root,new[]{Vector3.zero,Vector3.zero},cyan,.035f);
            printLine=Line("Fabrication path",root,new[]{Vector3.zero,Vector3.zero},amber,.025f);
            transfer=new GameObject("Demonstration material transfer chamber").transform; transfer.SetParent(root,false);
            transfer.localPosition=RingWalkthrough.At(56.5f,30); transfer.localRotation=Quaternion.Euler(0,30,0);
            Box("Transfer floor",transfer,new Vector3(0,-.1f,0),new Vector3(4,.2f,4),metal);
            foreach(float side in new[]{-1f,1f}) {
                Box("Transfer side",transfer,new Vector3(side*1.9f,1.7f,0),new Vector3(.2f,3.4f,4),hull);
                foreach(float z in new[]{-2f,2f}) Box("Gate upright",transfer,new Vector3(side*1.85f,1.7f,z),new Vector3(.2f,3.4f,.2f),metal);
            }
            Box("Rack handoff cradle",transfer,new Vector3(0,.12f,0),new Vector3(2.5f,.24f,2.5f),dark);
            innerLeft=Gate("Internal gate left",-1,-2); innerRight=Gate("Internal gate right",1,-2);
            outerLeft=Gate("External gate left",-1,2); outerRight=Gate("External gate right",1,2);
            // Roof intentionally omitted in this cutaway study, not a pressure simulation.
            foreach(var portal in layout.Geometry.DetailedHall.Portals) layout.Geometry.DetailedHall.Request(Array.IndexOf(layout.Geometry.DetailedHall.Portals,portal),true);
        }
        private Transform Gate(string name,float side,float z)
            => Box(name,transfer,new Vector3(side*.9f,1.7f,z),new Vector3(1.8f,3.4f,.16f),hull);
        private void Gates(float u)
        {
            innerOpening=u<.54f?1:u<.56f?1-(u-.54f)/.02f:u<.74f?0:u<.76f?(u-.74f)/.02f:1;
            outerOpening=u<.59f?0:u<.61f?(u-.59f)/.02f:u<.70f?1:u<.72f?1-(u-.70f)/.02f:0;
            void Pose(Transform leaf,float side,float z,float opening) => leaf.localPosition=new Vector3(side*(.9f+opening*1.9f),1.7f,z);
            Pose(innerLeft,-1,-2,innerOpening); Pose(innerRight,1,-2,innerOpening);
            Pose(outerLeft,-1,2,outerOpening); Pose(outerRight,1,2,outerOpening);
        }
        private void BuildBatches()
        {
            // Parts remain printer-sized. The tug transports a rack of at most eight flat pieces.
            for (int stage = 0; stage < 6; stage++) foreach (bool stock in new[] { false, true }) {
                Batch batch = null;
                foreach (var piece in pieces) if (piece.Stage == stage && piece.Stock == stock) {
                    float height=0; if(batch!=null) foreach(var p in batch.Pieces) height+=p.PackedSize.y+.04f;
                    if (batch == null || batch.Pieces.Count == 8 || height+piece.PackedSize.y+.04f>1.8f) { batch = new Batch { Stage = stage }; batches.Add(batch); }
                    batch.Pieces.Add(piece);
                }
            }
        }
        private void BuildInstalledMeshes()
        {
            // Completed racks become shared draw calls; the original pieces remain available for restart.
            foreach(var batch in batches) {
                var groups=new Dictionary<Material,List<CombineInstance>>();
                foreach(var piece in batch.Pieces) {
                    var mesh=piece.Node.GetComponent<MeshFilter>().sharedMesh; var surfaces=piece.Node.GetComponent<Renderer>().sharedMaterials;
                    for(int sub=0;sub<mesh.subMeshCount;sub++) {
                        var material=surfaces[Mathf.Min(sub,surfaces.Length-1)];
                        if(!groups.TryGetValue(material,out var list)) { list=new List<CombineInstance>(); groups.Add(material,list); }
                        list.Add(new CombineInstance { mesh=mesh,subMeshIndex=sub,transform=Matrix4x4.TRS(piece.Target,piece.TargetRotation,piece.Scale) });
                    }
                }
                var parts=new List<CombineInstance>(); var surfacesOut=new List<Material>();
                foreach(var pair in groups) {
                    var combined=new Mesh { name="Installed rack / material" }; combined.CombineMeshes(pair.Value.ToArray(),true,true);
                    parts.Add(new CombineInstance { mesh=combined,transform=Matrix4x4.identity }); surfacesOut.Add(pair.Key);
                }
                var final=new Mesh { name="Installed rack / shared geometry" }; final.CombineMeshes(parts.ToArray(),false,true); meshes.Add(final);
                foreach(var part in parts) Destroy(part.mesh);
                batch.Installed=new GameObject("Installed rack / "+Stages[batch.Stage]).transform; batch.Installed.SetParent(root,false); batch.Installed.gameObject.layer=25;
                batch.Installed.gameObject.AddComponent<MeshFilter>().sharedMesh=final;
                batch.Installed.gameObject.AddComponent<MeshRenderer>().sharedMaterials=surfacesOut.ToArray(); batch.Installed.gameObject.SetActive(false);
            }
        }
        private void ResetStudy()
        {
            job = completedPieces = 0; jobTime = inspectionTime = completedKg = 0; paused = complete = false; failure = null;
            foreach (var p in pieces) p.Node.gameObject.SetActive(false);
            foreach(var batch in batches) batch.Installed.gameObject.SetActive(false);
            pivot = RingWalkthrough.At(Middle, Sector, 2); distance = 60; yaw = 242; pitch = 36; RenderWork();
        }
        private static Vector3 Arc(Vector3 from, Vector3 to, float u)
            => Vector3.Lerp(from, to, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 7;
        private void Place(Piece p) { p.Node.gameObject.SetActive(true); p.Node.localPosition = p.Target; p.Node.localRotation = p.TargetRotation; p.Node.localScale = p.Scale; }
        private void Advance(float seconds)
        {
            if (paused || complete || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            while (seconds > 0 && job < batches.Count) {
                float step = Mathf.Min(seconds, Cycle - jobTime); jobTime += step; seconds -= step;
                if (jobTime >= Cycle) {
                    foreach (var piece in batches[job].Pieces) { Place(piece); piece.Node.gameObject.SetActive(false); completedKg += piece.Kg; completedPieces++; }
                    batches[job].Installed.gameObject.SetActive(true); job++; jobTime = 0;
                }
            }
            if (job == batches.Count) { inspectionTime = Mathf.Min(Inspection, inspectionTime + seconds); complete = inspectionTime >= Inspection; }
            RenderWork();
        }
        private Vector3 ChamberBase => RingWalkthrough.At(56.5f,30,.25f);
        private Vector3 OutsideBase => RingWalkthrough.At(59.75f,30,.55f);
        private Vector3 CargoPose(float u,Vector3 work)
        {
            Vector3 bed=printer+Vector3.up*.76f, lifted=bed+Vector3.up*.2f, corridor=RingWalkthrough.At(26,30,.35f);
            Vector3 chamber=ChamberBase;
            if(u<.34f) return bed;
            if(u<.38f) return Vector3.Lerp(bed,lifted,(u-.34f)/.04f);
            if(u<.40f) return Vector3.Lerp(lifted,corridor,(u-.38f)/.02f);
            if(u<.49f) return Vector3.Lerp(corridor,chamber+Vector3.up*.1f,(u-.40f)/.09f);
            if(u<.51f) return Vector3.Lerp(chamber+Vector3.up*.1f,chamber,(u-.49f)/.02f);
            if(u<.64f) return chamber;
            if(u<.67f) return Vector3.Lerp(chamber,chamber+Vector3.up*.3f,(u-.64f)/.03f);
            if(u<.70f) return Vector3.Lerp(chamber+Vector3.up*.3f,OutsideBase,(u-.67f)/.03f);
            if(u<.72f) return OutsideBase;
            Vector3 high=OutsideBase+Vector3.up*5.5f;
            if(u<.74f) return Vector3.Lerp(OutsideBase,high,(u-.72f)/.02f);
            if(u<.80f) return Arc(high,work,(u-.74f)/.06f);
            if(u<.96f) return work;
            if(u<.99f) return Arc(work,high,(u-.96f)/.03f);
            return Vector3.Lerp(high,OutsideBase,(u-.99f)/.01f);
        }
        private void RenderWork()
        {
            float u=job==batches.Count?0:U; Gates(u);
            Vector3 home=RingWalkthrough.At(26,30,2.5f), bed=printer+Vector3.up*.76f;
            Vector3 idle=RingWalkthrough.At(58,Sector,8);
            fabricator.localPosition=printer+new Vector3(-1.05f,2.4f,-1.05f); fabricator.localRotation=Quaternion.Euler(0,30,0);
            handler.localPosition=home; carrier.localPosition=OutsideBase+Vector3.up*2.3f; welder.localPosition=idle;
            printHead.localPosition=printer+new Vector3(Mathf.Sin(u*120)*.8f,3.6f,Mathf.Cos(u*100)*.8f);
            printLine.enabled=weldLine.enabled=false; foreach(var grip in grippers) grip.enabled=false;
            if(job==batches.Count) return;
            var batch=batches[job]; Vector3 target=Vector3.zero; foreach(var p in batch.Pieces) target+=p.Target;
            Vector3 work=target/batch.Pieces.Count+Vector3.up*5.7f;
            Vector3 cargoBase=CargoPose(u,work);
            if(u>=.30f && u<.34f) handler.localPosition=Vector3.Lerp(home,bed+Vector3.up*2.15f,(u-.30f)/.04f);
            else if(u>=.34f && u<.51f) handler.localPosition=cargoBase+Vector3.up*2.15f;
            else if(u>=.51f && u<.54f) handler.localPosition=Vector3.Lerp(ChamberBase+Vector3.up*2.15f,RingWalkthrough.At(52,30,2.5f),(u-.51f)/.03f);
            else if(u>=.54f && u<.74f) handler.localPosition=Vector3.Lerp(RingWalkthrough.At(52,30,2.5f),home,(u-.54f)/.20f);
            if(u>=.61f && u<.64f) carrier.localPosition=Vector3.Lerp(OutsideBase+Vector3.up*2.3f,ChamberBase+Vector3.up*2.3f,(u-.61f)/.03f);
            else if(u>=.64f) carrier.localPosition=cargoBase+Vector3.up*2.3f;
            handler.localRotation=carrier.localRotation=Quaternion.Euler(0,30,0);
            float stack=0;
            for(int i=0;i<batch.Pieces.Count;i++) {
                var p=batch.Pieces[i]; float print=Mathf.Clamp01(u/.26f*batch.Pieces.Count-i);
                float mount=Mathf.Clamp01((u-.80f)/.16f*batch.Pieces.Count-i);
                p.Node.gameObject.SetActive(print>0);
                Vector3 packed=cargoBase+Vector3.up*(stack+p.PackedSize.y/2); stack+=p.PackedSize.y+.04f;
                float place=Mathf.Clamp01(mount/.35f);
                p.Node.localPosition=Vector3.Lerp(packed,p.Target,place);
                p.Node.localRotation=Quaternion.Slerp(p.PackRotation,p.TargetRotation,place); p.Node.localScale=p.Scale;
                if(print>0 && print<1) {
                    Vector3 normal=Quaternion.Inverse(p.PackRotation)*Vector3.up, scale=p.Scale;
                    int axis=Mathf.Abs(normal.x)>.9f?0:Mathf.Abs(normal.z)>.9f?2:1;
                    scale[axis]*=Mathf.Max(.005f,print); p.Node.localScale=scale;
                    p.Node.localPosition-=Vector3.up*p.PackedSize.y*(1-print)/2;
                    printLine.enabled=!p.Stock; printLine.SetPosition(0,printHead.localPosition); printLine.SetPosition(1,p.Node.localPosition);
                }
                if(mount>0 && mount<1) {
                    Vector3 tool=p.Target+Vector3.up*1.6f+RingWalkthrough.At(1.8f,Sector);
                    float approach=mount<.35f?mount/.35f:mount>.85f?(1-mount)/.15f:1;
                    welder.localPosition=Vector3.Lerp(idle,tool,approach);
                    weldLine.enabled=mount>=.35f && mount<=.85f;
                    weldLine.SetPosition(0,welder.localPosition); weldLine.SetPosition(1,p.Target);
                }
            }
            bool heldInside=u>=.34f && u<.51f, heldOutside=u>=.64f && u<.96f;
            if(heldInside || heldOutside) for(int i=0;i<grippers.Count;i++) {
                Vector3 corner=Quaternion.Euler(0,30,0)*new Vector3(i<2?-1.05f:1.05f,0,i%2==0?-1.05f:1.05f);
                Vector3 drone=heldInside?handler.localPosition:carrier.localPosition;
                grippers[i].enabled=true; grippers[i].SetPosition(0,drone+corner*.65f-Vector3.up*.3f);
                grippers[i].SetPosition(1,cargoBase+corner+Vector3.up*Mathf.Max(.1f,stack-.15f));
            }
        }
        private void Update()
        {
            if (!ready || checking || StartMenu.IsOpen || QuitMenu.BlocksInput || SettingsMenu.BlocksInput || SaveGameMenu.BlocksInput) return;
            if (Input.GetKeyDown(KeyCode.Space)) paused = !paused;
            Advance(Mathf.Min(Time.unscaledDeltaTime, .1f) * speed);
            Vector3 mouse = Input.mousePosition, delta = mouse - lastMouse;
            if (UiLayout.Point(mouse).x < UiLayout.Width - PanelWidth) {
                if (Input.GetMouseButton(1) && !Input.GetMouseButtonDown(1)) { yaw += delta.x * .2f; pitch = Mathf.Clamp(pitch - delta.y * .2f, 10, 78); }
                distance = Mathf.Clamp(distance * Mathf.Exp(-Input.mouseScrollDelta.y * .13f), 12, 240);
            }
            lastMouse = mouse;
        }
        private void LateUpdate() { if (ready) CameraPose(); }
        private void CameraPose()
        {
            view.rect = new Rect(0, 0, Mathf.Max(.35f, 1 - PanelWidth / UiLayout.Width), 1);
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            view.transform.SetPositionAndRotation(root.TransformPoint(pivot) - rotation * Vector3.forward * distance, rotation);
            view.fieldOfView = 52; OrbitCamera.ConfigureDepthRange(view, distance); view.farClipPlane = 2000;
        }
        private void OnGUI()
        {
            if (!ready || StartMenu.IsOpen || QuitMenu.BlocksInput || SettingsMenu.BlocksInput) return;
            using var scope = new UiLayout.Scope(UiLayout.Scale()); ui.Configure(SettingsStore.Current.Accessibility);
            float width = UiLayout.Width, height = UiLayout.Height; GUI.depth = -30;
            ui.Panel(new Rect(16, 16, Mathf.Min(520, width - PanelWidth - 32), 88));
            GUI.Label(new Rect(30, 28, width - PanelWidth - 60, 65), "BAUPROBE S03  /  5,33 m ROHSTOFFLAGER\nWabenhuelle · 2 Lagen · 4 Drohnenrollen", ui.Text);
            var panel = new Rect(width - PanelWidth, 0, PanelWidth, height); GUI.depth = 5; ui.Panel(panel); GUI.depth = -30;
            GUILayout.BeginArea(new Rect(panel.x + 18, 16, PanelWidth - 36, height - 32)); scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("STATION ERWEITERN", ui.Heading);
            GUILayout.Label(complete ? "Baulich fertig / Abnahme simuliert" : Stages[Stage], ui.Text);
            GUILayout.Label(Details[Stage], ui.Small); GUILayout.Space(10);
            Rect bar = GUILayoutUtility.GetRect(1, 12); SpaceMinerUi.Fill(bar, new Color(.05f,.1f,.14f)); SpaceMinerUi.Fill(new Rect(bar.x,bar.y,bar.width*WorkDone/TotalWork,bar.height),SpaceMinerUi.Cyan);
            GUILayout.Label((WorkDone / TotalWork * 100).ToString("0") + " %   |   " + Installed + " / " + pieces.Count + " Teile montiert", ui.Small);
            string operation = complete ? "Abgenommen" : job>=batches.Count?"Dicht-/Medienpruefung (simuliert)"
                : U<.26f?"B01: intern fertigen / Lagerteile pruefen":U<.30f?"B01: Kuehl-/Masskontrolle":U<.51f?"B02: abholen und intern bereitstellen"
                : U<.61f?"Uebergabe sichern / Druckwechsel simuliert":U<.72f?"B03: Ladung greifen und ausfahren"
                : U<.80f?"B03: mehrere Teile zur Baustelle":U<.96f?"B04: ausrichten, schweissen und abdichten":"B03: leer zurueckfliegen";
            GUILayout.Label("Aufgabe: " + operation + "\nGefertigt/bereitgestellt: " + Printed + " Teile\nModellzeit: " + WaterScenario.Duration(WorkDone), ui.Small);
            GUILayout.Space(10);
            for (int i = 0; i < Stages.Length; i++) GUILayout.Label((i < Stage || complete ? "✓ " : i == Stage ? "> " : "   ") + Stages[i], ui.Small);
            GUILayout.Space(12); GUI.enabled = !checking;
            if (GUILayout.Button(paused ? "Fortsetzen [Leertaste]" : "Pause [Leertaste]", ui.Button)) paused = !paused;
            GUILayout.BeginHorizontal(); foreach (float rate in new[] { 80f, 320f, 1280f, 5120f }) if (GUILayout.Button(rate + "x", ui.Button)) speed = rate; GUILayout.EndHorizontal();
            GUILayout.Label("Zeitraffer " + speed + "x  ·  Zeiten sind Entwurfswerte", ui.Small);
            if (GUILayout.Button("Baustelle ansehen", ui.Button)) { pivot = RingWalkthrough.At(Middle, Sector, 2); distance = 60; }
            if (GUILayout.Button("Drucker im Detail", ui.Button)) { pivot = printer + Vector3.up * 1.8f; distance = 14; pitch=68; yaw=210; }
            if (GUILayout.Button("Interne Transportstrecke",ui.Button)) { pivot=RingWalkthrough.At(41,30,1.5f); distance=45; pitch=68; yaw=210; }
            if (GUILayout.Button("Uebergabekammer",ui.Button)) { pivot=ChamberBase+Vector3.up; distance=20; }
            if (GUILayout.Button(cutaway?"Huelle geschlossen zeigen":"Schnittansicht zeigen",ui.Button)) { cutaway=!cutaway; layout.Geometry.ConstructionCutaway(cutaway); }
            if (GUILayout.Button("Gesamte Station", ui.Button)) { pivot = Vector3.zero; distance = 180; }
            if (GUILayout.Button("Bauprobe neu starten", ui.Button)) ResetStudy(); GUI.enabled = true;
            GUILayout.Space(12);
            GUILayout.Label("Druckraum: 2,4 m je Achse\nLieferung: maximal 8 kleine Teile\nBauteilmasse: ca. " + TotalKg.ToString("0") + " kg (Modellwert)\nElektronik: vorgefertigte Lagerteile\nDrucklage + Schutzlage (28 cm Abstand)\nWaben mit passenden Randstuecken", ui.Small);
            GUILayout.Space(12);
            GUILayout.Label("B01  Fertigungsdrohne / innen\nB02  Bereitstellungsdrohne / innen\nB03  Lastdrohne mit vier Greifern / aussen\nB04  Schweissdrohne / aussen\n\nUebergabe: Innentor "+(innerOpening>.01f?"offen":"zu")+" · Aussentor "+(outerOpening>.01f?"offen":"zu"),ui.Small);
            GUILayout.Space(12);
            GUILayout.Label("Eigenstaendige Ablaufprobe: keine Kampagnenkosten oder gespeicherten Anbauten. Das Stationsschott bleibt geschlossen. Druck-, Strom- und Materialpruefung sind noch keine physikalische Simulation.\n\nRechte Maustaste: drehen\nMausrad: Abstand · F11: Vollbild\nEscape: Spielmenue", ui.Small);
            if (failure != null) GUILayout.Label(failure, ui.Text);
            GUILayout.EndScrollView(); GUILayout.EndArea();
            foreach(var drone in new[]{fabricator,handler,carrier,welder}) {
                Vector3 point=view.WorldToScreenPoint(drone.position); Vector2 screen=UiLayout.Point(point);
                if(point.z>0 && screen.x>0 && screen.x<width-PanelWidth-140 && screen.y>110 && screen.y<height-30)
                    GUI.Label(new Rect(screen.x+8,screen.y,240,24),drone.name.Substring(0,3),ui.Small);
            }
        }
        private bool Guard(Action action) { try { action(); return true; } catch (Exception e) { Debug.LogException(e); failure = e.Message; paused = true; if (checking || !ready) Application.Quit(1); return false; } }
        private void Require(bool condition, string label) { checks++; if (!condition) throw new Exception("Construction: " + label); Debug.Log("CONSTRUCTION OK: " + label); }
        private void Capture(string name)
        {
            CameraPose(); var texture = new RenderTexture(1600, 1000, 24); var previous = RenderTexture.active; var oldTarget = view.targetTexture; var rect = view.rect;
            try {
                view.rect = new Rect(0, 0, 1, 1); view.targetTexture = texture; view.Render(); RenderTexture.active = texture;
                var pixels = new Texture2D(1600, 1000, TextureFormat.RGB24, false); pixels.ReadPixels(new Rect(0,0,1600,1000),0,0); pixels.Apply();
                File.WriteAllBytes(Path.GetFullPath(Evidence + "/" + name + ".png"), pixels.EncodeToPNG()); Destroy(pixels);
            } finally { RenderTexture.active = previous; view.targetTexture = oldTarget; view.rect = rect; texture.Release(); Destroy(texture); }
        }
        private IEnumerator Validate()
        {
            Directory.CreateDirectory(Evidence); float water = scenario.WaterLiters; float battery = scenario.Worker.BatteryKwh;
            if (!Guard(() => {
                Require(SettingsStore.PathName.Contains("StationConstruction"), "isolated settings and saves");
                Require(pieces.Count > 300 && batches.Count > 30 && Installed == 0, "module starts as many separate uninstalled pieces");
                float largest = 0; foreach (var p in pieces) largest = Mathf.Max(largest, Mathf.Max(p.PackedSize.x, Mathf.Max(p.PackedSize.y,p.PackedSize.z)));
                Require(largest <= 2.4f, "every separate piece fits the2.4m printer envelope");
                bool racksFit = true; foreach (var batch in batches) { float stack = 0; foreach (var p in batch.Pieces) stack += p.PackedSize.y + .04f; racksFit &= batch.Pieces.Count <= 8 && stack <= 1.8f; }
                Require(racksFit, "all transport racks fit the envelope and eight-piece limit");
                Require(fabricator!=handler && handler!=carrier && carrier!=welder && fabricator!=welder, "four independent construction roles");
                Require(layout.ContainsPosition(layout.transform.TransformPoint(printer+Vector3.up*.04f)), "fabrication cell is in existing internal workshop");
                foreach(var domain in new[]{new Vector2(.11f,.2f),new Vector2(3.5f,3.2f),new Vector2(21.7f,16),new Vector2(12.5f,5.5f)}) {
                    float area=0; foreach(var polygon in HoneycombHull.Cells(domain.x,domain.y)) area+=HoneycombHull.Area(polygon);
                    Require(Mathf.Abs(area-domain.x*domain.y)<.004f, "clipped honeycomb covers complete rectangular domain "+domain);
                }
                float discArea=0; foreach(var cell in HoneycombHull.Disc(4.325f)) discArea+=HoneycombHull.Area(cell);
                Require(Mathf.Abs(discArea-24*4.325f*4.325f*Mathf.Sin(Mathf.PI/24))<.004f,"reactor cap tiles cover their circular rim without square overhang");
                Bounds inner=new Bounds(), outer=new Bounds(); bool firstInner=true,firstOuter=true; float farthestPanel=0;
                foreach(var piece in pieces) if(piece.Node.name.StartsWith("Hex panel")) foreach(var vertex in piece.Node.GetComponent<MeshFilter>().sharedMesh.vertices) {
                    Vector3 position=piece.Target+piece.TargetRotation*vertex;
                    farthestPanel=Mathf.Max(farthestPanel,new Vector2(position.x,position.z).magnitude);
                    if(piece.Stage==5) { if(firstOuter) { outer=new Bounds(position,Vector3.zero); firstOuter=false; } outer.Encapsulate(position); }
                    else { if(firstInner) { inner=new Bounds(position,Vector3.zero); firstInner=false; } inner.Encapsulate(position); }
                }
                Require(outer.min.y<inner.min.y-.15f && outer.max.y>inner.max.y+.15f && outer.max.x>inner.max.x+.1f && outer.max.z>inner.max.z+.1f
                    && outer.min.x<inner.min.x-.1f && outer.min.z<inner.min.z-.1f,"outer tile envelope protects roof, underside, both side walls and end wall");
                Require(farthestPanel>Outside+.1f && farthestPanel<Outside+.4f,"actual construction panels stop at the first short storage stage including outer shield");
                bool continuous=true, interlocked=true, crossings=true, insideRoute=true, routeClear=true; string routeIssue="", crossingIssue="";
                Physics.SyncTransforms();
                Vector3 work=RingWalkthrough.At(46,Sector,8);
                foreach(float boundary in new[]{.34f,.38f,.40f,.49f,.51f,.64f,.67f,.70f,.72f,.74f,.80f,.96f,.99f})
                    continuous &= Vector3.Distance(CargoPose(boundary-.00001f,work),CargoPose(boundary+.00001f,work))<.03f;
                for(int i=0;i<=1000;i++) {
                    jobTime=Cycle*i/1000f; RenderWork(); interlocked &= innerOpening*outerOpening<.0001f;
                    foreach(var drone in new[]{handler,carrier}) {
                        var p=transfer.InverseTransformPoint(root.TransformPoint(drone.localPosition));
                        if(Mathf.Abs(p.x)<3 && p.y>-.4f && p.y<3.75f) {
                            if((Mathf.Abs(p.z+2)<1.2f && innerOpening<.999f) || (Mathf.Abs(p.z-2)<1.2f && outerOpening<.999f)) {
                                if(crossings) crossingIssue=drone.name+" u="+(i/1000f)+" / "+p; crossings=false;
                            }
                        }
                        Vector3 centre=drone.localPosition+Vector3.down*(drone==carrier?1:.9f);
                        Vector3 half=drone==carrier?new Vector3(1.18f,1.4f,1.12f):new Vector3(1.18f,1.3f,1.12f);
                        var hits=Physics.OverlapBox(root.TransformPoint(centre),half,root.rotation*drone.localRotation,1<<25,QueryTriggerInteraction.Ignore);
                        if(hits.Length>0) { foreach(var hit in hits) { string issue=drone.name+" / "+hit.name; if(!routeIssue.Contains(issue)) routeIssue+=issue+" u="+(i/1000f)+"; "; } routeClear=false; }
                    }
                    float u=i/1000f;
                    if(u>.40f && u<.482f) insideRoute &= handler.localPosition.y<2.9f && handler.localPosition.y+ .33f<3.1f;
                }
                Require(continuous,"cargo handoffs and flight have continuous positions");
                Require(interlocked,"transfer doors never open together throughout a full delivery");
                if(!routeClear) Debug.Log("Construction route issue: "+routeIssue);
                Require(crossings,"both drones cross only fully opened transfer gates: "+crossingIssue);
                Require(insideRoute,"internal handler stays below existing 3.1m door headers"); ResetStudy();
                Require(routeClear,"delivery drone envelopes clear actual station colliders: "+routeIssue);
                Advance(90); Require(Printed > 0 && Installed == 0, "manufacture precedes installation");
                paused = true; float before = WorkDone; Advance(1000); Require(WorkDone == before, "pause freezes work"); paused = false;
                Advance(float.NaN); Advance(-1); Require(WorkDone == before, "invalid time ignored"); ResetStudy();
                Advance(Cycle*.34f); Vector3 onBed = batches[0].Pieces[0].Node.localPosition; Advance(Cycle*.02f);
                float lift = batches[0].Pieces[0].Node.localPosition.y - onBed.y;
                Require(lift > .05f && lift < .8f && Installed == 0, "cargo lifts progressively from printer bed before flight"); ResetStudy();
            })) yield break;
            Capture("01-anschluss-und-drucker");
            Advance(90); pivot = printer + Vector3.up * 1.8f; distance = 14; pitch=68; yaw=210; Capture("02-drucker-einzelteile"); ResetStudy();
            Advance(Cycle*.465f); pivot=RingWalkthrough.At(44,30,1.8f); distance=25; Capture("03-interne-bereitstellung"); ResetStudy();
            Advance(Cycle*.58f); pivot=ChamberBase+Vector3.up; distance=17; Capture("04-uebergabe-gesichert"); ResetStudy();
            Advance(Cycle*.655f); pivot=ChamberBase+Vector3.up; distance=17; Capture("05-aussendrohne-uebernimmt"); ResetStudy();
            int captured = -1; int previousInstalled = 0, safetySamples = 0;
            while (!complete) {
                if (!Guard(() => {
                    Advance(60);
                    if (Installed < previousInstalled || Installed > Printed || Printed > pieces.Count) throw new Exception("Invalid printed/installed piece order"); previousInstalled = Installed;
                    if (scenario.WaterLiters != water || scenario.Worker.BatteryKwh != battery) throw new Exception("Construction changed campaign resources");
                    if (job >= batches.Count && Installed != pieces.Count) throw new Exception("Inspection started before assembly finished");
                    safetySamples++;
                })) yield break;
                if (Stage != captured) { captured = Stage; Capture("stage-" + Stage); }
                yield return null;
            }
            if (!Guard(() => {
                Require(safetySamples > 500, "full sequential run preserves production order, resources and inspection prerequisites");
                int shown=0; foreach(var batch in batches) if(batch.Installed.gameObject.activeSelf) shown++;
                Require(shown==batches.Count,"completed geometry uses reusable grouped racks without duplicate rendered pieces");
                Require(completedPieces == pieces.Count && Mathf.Abs(completedKg - TotalKg) < .1f, "every planned piece installed once with accounted model mass");
                Require(Physics.Raycast(layout.transform.TransformPoint(RingWalkthrough.At(37, Sector, 1.4f)), layout.transform.TransformDirection(RingWalkthrough.At(1,Sector)), 2, 1 << 25), "campaign bulkhead stays closed");
                Require(layout.Geometry.OuterStages(2) == 0 && !layout.ContainsPosition(layout.transform.TransformPoint(RingWalkthrough.At(Middle,Sector,.04f))), "preview does not become saved campaign floor");
                int count = root.childCount; ResetStudy(); Require(Installed == 0 && Printed == 0 && root.childCount == count && !complete, "restart reuses parts without duplicates");
                Advance(TotalWork); Require(complete && Installed == pieces.Count, "large step matches complete sequential build");
                File.WriteAllText(Path.GetFullPath(Evidence + "/result.json"), "{\"passed\":true,\"checks\":"+checks+",\"pieces\":"+pieces.Count+",\"batches\":"+batches.Count+",\"massKg\":"+TotalKg.ToString("0",System.Globalization.CultureInfo.InvariantCulture)+",\"modelSeconds\":"+TotalWork.ToString("0",System.Globalization.CultureInfo.InvariantCulture)+"}");
            })) yield break;
            Capture("08-fertiges-modul");
            pivot=RingWalkthrough.At(Middle,Sector,4); distance=30; Capture("09-wabenhuelle-detail");
            layout.Geometry.ConstructionCutaway(false); pivot=Vector3.zero; distance=180; Capture("10-station-zweischichtig");
            Debug.Log("CONSTRUCTION CHECK PASSED: " + checks); Application.Quit(0);
        }
        private void OnDestroy()
        {
            if(layout!=null && layout.Geometry!=null) layout.Geometry.ConstructionCutaway(false);
            if (view != null) view.rect = new Rect(0,0,1,1);
            if (root != null) Destroy(root.gameObject); foreach (var mesh in meshes) Destroy(mesh); foreach (var material in materials) Destroy(material); ui.Dispose();
        }
    }
}
#endif
