using UnityEngine;

namespace SpaceMiner
{
    // Lightweight visual placeholder; logistics continue to use the existing root.
    public sealed class StationVisual : MonoBehaviour
    {
        private readonly Transform[] berths = new Transform[10];
        public const int ChargingBerthCount = 8;
        public Transform TankSocket { get; private set; }
        private readonly Transform[] storageDocks = new Transform[RingWalkthrough.StorageStageCount];
        public int StorageDockCount => storageDocks.Length;
        public Transform StorageDock(int stage) => storageDocks[stage];
        public bool StorageDockOperational(int stage) => stage==0 && storageDocks[stage]!=null;
        public Transform DroneDock { get; private set; }
        public Transform Berth(int index) { Build(); return berths[index]; }
        private void Awake() => Build();

        internal void InstallStartLayout(StationInteriorLayout layout)
        {
            Build();
            var old = transform.Find("Station Geometry");
            var hull = old.Find("Water Ice Tank").GetComponent<Renderer>().sharedMaterial;
            var dark = old.Find("Tank cap").GetComponent<Renderer>().sharedMaterial;
            var cyan = old.Find("Tank blue band").GetComponent<Renderer>().sharedMaterial;
            // Larger surviving arrays on exterior structural booms; no extra inhabited modules.
            for (int i = 0; i < 2; i++)
            {
                var wing = old.Find(i == 0 ? "Solar Wing Left" : "Solar Wing Right");
                wing.SetParent(layout.transform, false);
                wing.localPosition = RingWalkthrough.At(60, i == 0 ? 270 : 90, 1.8f);
                wing.localScale = new Vector3(2, 1, 2);
                float angle = i == 0 ? 270 : 90;
                var boom = new GameObject("Solar structural boom " + i).transform;
                boom.SetParent(layout.transform, false); boom.localRotation = Quaternion.Euler(0, angle, 0);
                Part(PrimitiveType.Cube, "Solar boom spine", boom, new Vector3(0, .3f, 49), new Vector3(1.1f, .7f, 22), dark);
                Part(PrimitiveType.Cube, "Solar hull mounting", boom, new Vector3(0, .8f, 38.5f), new Vector3(3, 2, 1.2f), hull);
                for (int brace = 0; brace < 4; brace++)
                    Part(PrimitiveType.Cube, "Solar boom brace " + brace, boom, new Vector3(0, .85f, 41 + brace * 5), new Vector3(1.35f, .12f, .3f), hull);
            }
            var amber=Surface("Storage disabled / steady amber",new Color(.85f,.32f,.06f),.2f,true);
            for(int stage=0;stage<RingWalkthrough.StorageStageCount;stage++) BuildStorageStage(layout,stage,hull,dark,cyan,amber);
            DroneDock = new GameObject("Campaign Drone Dock").transform;
            DroneDock.SetParent(layout.transform, false);
            DroneDock.localPosition = RingWalkthrough.At(64.75f, 30);
            DroneDock.localRotation = Quaternion.Euler(0, 30, 0);
            var scenario = GetComponent<WaterScenario>();
            for (int i = 0; i < berths.Length; i++)
            {
                berths[i] = layout.Geometry.CampaignBerths[i];
                if (scenario != null && scenario.Drones != null) scenario.Drones[i].RebindHome(berths[i]);
            }
            foreach (Transform child in old)
                if (!child.name.StartsWith("Reactor") && child.name != "Shielded Reactor") child.gameObject.SetActive(false);
            // A replaceable metre-scale blockout; the detailed reactor is designed separately.
            old.Find("Shielded Reactor").localScale = new Vector3(8, 3, 8);
            old.Find("Shielded Reactor").localPosition = Vector3.up * 3;
            old.Find("Reactor cap").localScale = new Vector3(7.6f, .12f, 7.6f);
            old.Find("Reactor cap").localPosition = Vector3.up * 6.12f;
            old.Find("Reactor status band").localScale = new Vector3(8.03f, .045f, 8.03f);
            old.Find("Reactor status band").localPosition = Vector3.up * 4.5f;
            old.Find("Reactor mast").gameObject.SetActive(false);
            var info = GetComponent<SpaceObject>();
            info.DiameterMeters = 172; // 108-m ring body with the ground-level exterior dock reaching r=84.
            info.Description = "Havarierte Startstation / Kommandozentrale, Drohnenhalle mit Aussendock, Eis-Wasserlager gegenueber, interne Werkstatt; ein Reaktorgang intakt";
        }
        private void BuildStorageStage(StationInteriorLayout layout,int stage,Material hull,Material dark,Material cyan,Material amber)
        {
            var tank=new GameObject("Campaign water storage / Stage "+(stage+1)).transform;
            tank.SetParent(layout.transform,false); float radius=46, heading=RingWalkthrough.StorageAngle(stage);
            tank.localPosition=RingWalkthrough.At(radius,heading); tank.localRotation=Quaternion.Euler(0,heading,0);
            var body=Part(PrimitiveType.Cylinder,stage==0?"Water Ice Tank":"Ruptured tank / offline",tank,new Vector3(1.6f,1.65f,0),new Vector3(2.6f,1.65f,2.6f),hull);
            if(stage==0) {
                Part(PrimitiveType.Cylinder,"Tank cap",tank,new Vector3(1.6f,3.4f,0),new Vector3(2.7f,.1f,2.7f),dark);
                Part(PrimitiveType.Cube,"Tank status",tank,new Vector3(1.6f,1.8f,-1.34f),new Vector3(1,.4f,.08f),cyan);
            } else {
                // A split visual tank, not a second working resource container.
                body.SetActive(false);
                Part(PrimitiveType.Cylinder,"Broken tank end",tank,new Vector3(1.6f,.5f,0),new Vector3(2.6f,.35f,2.6f),dark,Quaternion.Euler(stage==1?9:31,0,stage==1?-12:24));
                TornTankShell(tank,stage,hull);
                for(int rib=0;rib<5;rib++) {
                    float a=rib*51; Vector3 p=new Vector3(1.6f+Mathf.Sin(a*Mathf.Deg2Rad)*1.25f,1.8f,Mathf.Cos(a*Mathf.Deg2Rad)*1.25f);
                    Part(PrimitiveType.Cube,"Torn tank rib",tank,p,new Vector3(.12f,stage==1?2.7f:1.4f,.18f),hull,Quaternion.Euler(stage*11,rib*51,rib%2==0?19:-14));
                }
            }
            Vector3 normal=Quaternion.Euler(0,heading,0)*Vector3.forward;
            Vector3 inlet=RingWalkthrough.At(54.8f,heading,3.3f);
            Transform socket;
            if(stage==0) socket=TankSocket;
            else socket=new GameObject("Storage dock "+(stage+1)+" / damaged offline").transform;
            socket.SetParent(tank,false);
            socket.localPosition=Quaternion.Inverse(tank.localRotation)*(inlet-tank.localPosition);
            socket.localRotation=Quaternion.Inverse(tank.localRotation)*Quaternion.LookRotation(normal,Vector3.up);
            storageDocks[stage]=socket;
            if(stage==0) {
                Vector3 start=new Vector3(1.6f,3.3f,0), end=socket.localPosition-socket.localRotation*Vector3.forward*.35f;
                Part(PrimitiveType.Cube,"Transfer pipe",tank,(start+end)/2,new Vector3(.24f,.24f,(end-start).magnitude),dark,Quaternion.LookRotation(end-start));
            } else {
                Part(PrimitiveType.Cube,"Disabled receiver cap",socket,new Vector3(0,0,-.1f),new Vector3(.8f,.65f,.16f),dark);
                foreach(float sign in new[]{-1f,1f})
                    Part(PrimitiveType.Cube,"Broken docking jaw",socket,new Vector3(sign*.55f,0,.15f),new Vector3(.14f,.8f,.6f),hull,Quaternion.Euler(stage*17,sign*stage*11,sign*24));
                Part(PrimitiveType.Cube,"Severed transfer pipe",socket,new Vector3(0,-.25f,-.5f),new Vector3(.24f,.24f,.7f),dark,Quaternion.Euler(stage*19,0,0));
            }
            Part(PrimitiveType.Cube,stage==0?"Storage dock 01 / active":"Storage dock / locked out",socket,new Vector3(0,.6f,-.12f),new Vector3(.8f,.12f,.1f),stage==0?cyan:amber);
        }

        private void TornTankShell(Transform parent,int style,Material material)
        {
            var vertices=new System.Collections.Generic.List<Vector3>();var indices=new System.Collections.Generic.List<int>();
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});indices.AddRange(new[]{n,n+1,n+2,n,n+2,n+3,n+2,n+1,n,n+3,n+2,n});}
            for(int i=0;i<18;i++)
            {
                float a=(style==1?25:115)+i*(style==1?13:8),b=a+(style==1?13:8);
                Vector3 Point(float angle,float radius,float y)=>new Vector3(1.6f+Mathf.Sin(angle*Mathf.Deg2Rad)*radius,y,Mathf.Cos(angle*Mathf.Deg2Rad)*radius);
                float h0=(style==1?3.3f:2.7f)+Mathf.Sin(i*1.7f)*.2f,h1=(style==1?3.3f:2.7f)+Mathf.Sin((i+1)*1.7f)*.2f;
                Quad(Point(a,1.3f,.45f),Point(b,1.3f,.45f),Point(b,1.3f,h1),Point(a,1.3f,h0));
                Quad(Point(b,1.23f,.45f),Point(a,1.23f,.45f),Point(a,1.23f,h0),Point(b,1.23f,h1));
                Quad(Point(a,1.3f,h0),Point(b,1.3f,h1),Point(b,1.23f,h1),Point(a,1.23f,h0));
            }
            var mesh=new Mesh{name="Ruptured cylindrical tank shell / "+style};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();StationSurfaceMaterials.WorldUvs(mesh);
            var shell=new GameObject("Attached torn tank shell");shell.transform.SetParent(parent,false);
            shell.AddComponent<MeshFilter>().sharedMesh=mesh;shell.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        private Material Surface(string name, Color color, float metal, bool glow = false)
        {
            var material = StationSurfaceMaterials.Create(color,metal,glow);material.name=name;
            material.SetFloat("_Metallic", metal); material.SetFloat("_Glossiness", 0.25f);
            if (glow) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * 1.2f); }
            return material;
        }

        private GameObject Part(PrimitiveType kind, string name, Transform parent, Vector3 position, Vector3 scale, Material material, Quaternion rotation = default)
        {
            var part = GameObject.CreatePrimitive(kind); part.name = name;
            part.transform.SetParent(parent, false); part.transform.localPosition = position;
            part.transform.localScale = scale; part.transform.localRotation = rotation.Equals(default(Quaternion)) ? Quaternion.identity : rotation;
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
        }

        public void Build()
        {
            if (transform.Find("Station Geometry") != null) return;
            foreach (string legacy in new[] { "Hull", "Nose", "Rear", "Emergency Strip", "Solar Panel L", "Solar Panel R (damaged)" })
            {
                var old = transform.Find(legacy);
                if (old != null) { old.gameObject.SetActive(false); }
            }
            var info = GetComponent<SpaceObject>();
            info.DisplayName = "HAVARIERTE RAUMSTATION"; info.DiameterMeters = 60f;
            info.Description = "Modulare Raumstation / Reaktor, Zugangsring, Wasserbehälter und beschädigte Solarflächen";
            var geometry = new GameObject("Station Geometry").transform; geometry.SetParent(transform, false);
            Material hull = Surface("Station hull", new Color(0.46f,0.53f,0.57f),0.6f);
            Material dark = Surface("Station frame",new Color(0.10f,0.14f,0.18f),0.7f);
            Material solar = Surface("Station solar cells",new Color(0.025f,0.12f,0.26f),0.45f);
            Material cyan = Surface("Station status",new Color(0.04f,0.65f,0.8f),0.2f,true);
            Material amber = Surface("Station emergency",new Color(1f,0.32f,0.04f),0f,true);
            Part(PrimitiveType.Cylinder,"Shielded Reactor",geometry,Vector3.zero,new Vector3(7f,2.5f,7f),hull);
            Part(PrimitiveType.Cylinder,"Reactor cap",geometry,new Vector3(0,2.7f,0),new Vector3(5.5f,0.3f,5.5f),dark);
            Part(PrimitiveType.Cylinder,"Reactor status band",geometry,new Vector3(0,0.8f,0),new Vector3(7.05f,0.08f,7.05f),cyan);
            Part(PrimitiveType.Cylinder,"Reactor mast",geometry,new Vector3(0,4.1f,0),new Vector3(0.4f,1.1f,0.4f),dark);
            var ring = new GameObject("Access Ring").transform; ring.SetParent(geometry,false);
            for (int i = 0; i < 24; i++)
            {
                float angle = i * 15f; var turn = Quaternion.Euler(0,angle,0);
                Vector3 position = turn * Vector3.forward * 12f;
                Part(PrimitiveType.Cube,"Ring segment " + i,ring,position,new Vector3(3.13f,2.3f,2.6f),hull,turn);
                Part(PrimitiveType.Cube,"Ring rim " + i,ring,position + Vector3.up * 1.22f,new Vector3(2.95f,0.16f,2.3f),dark,turn);
                if (i % 4 == 0) Part(PrimitiveType.Cube,"Ring light " + i,ring,position + Vector3.up * 1.33f,new Vector3(1.1f,0.06f,0.18f),cyan,turn);
            }
            for (int i = 0; i < 4; i++)
            {
                var turn = Quaternion.Euler(0,i * 90f,0);
                Part(PrimitiveType.Cube,"Access spoke " + i,geometry,turn * Vector3.forward * 7.2f,new Vector3(1.8f,1.8f,8.2f),dark,turn);
                Part(PrimitiveType.Cube,"Spoke cover " + i,geometry,turn * Vector3.forward * 7.2f + Vector3.up,new Vector3(1.6f,0.14f,8f),hull,turn);
            }
            for (int i = 0; i < 6; i++)
            {
                var turn = Quaternion.Euler(0,30f+i*60f,0);
                Part(PrimitiveType.Cylinder,"Capped module port " + i,geometry,turn * Vector3.forward * 14f,new Vector3(2.7f,0.8f,2.7f),dark,turn * Quaternion.Euler(90,0,0));
                Part(PrimitiveType.Cylinder,"Module hatch " + i,geometry,turn * Vector3.forward * 14.85f,new Vector3(2.3f,0.06f,2.3f),hull,turn * Quaternion.Euler(90,0,0));
            }
            Part(PrimitiveType.Cylinder,"Water Ice Tank",geometry,new Vector3(5.7f,0.5f,-4.3f),new Vector3(3.2f,3f,3.2f),hull);
            Part(PrimitiveType.Cylinder,"Tank cap",geometry,new Vector3(5.7f,3.6f,-4.3f),new Vector3(3.35f,0.12f,3.35f),dark);
            Part(PrimitiveType.Cube,"Tank blue band",geometry,new Vector3(5.7f,1.4f,-5.95f),new Vector3(1.4f,0.5f,0.1f),cyan);
            TankSocket = new GameObject("Tank transfer socket").transform;
            TankSocket.SetParent(geometry,false); TankSocket.localPosition = new Vector3(5.7f,3.3f,-6.30f);
            TankSocket.localRotation = Quaternion.Euler(0,180,0);
            Part(PrimitiveType.Cube,"Ice unloading inlet",TankSocket,new Vector3(0,0,-0.10f),new Vector3(0.62f,0.42f,0.10f),dark);
            for(int side=-1;side<=1;side+=2) {
                Part(PrimitiveType.Cube,"Receiver side " + side,TankSocket,new Vector3(side*0.38f,0,-0.08f),new Vector3(0.12f,0.6f,0.35f),hull);
                Part(PrimitiveType.Cube,"Receiver rim " + side,TankSocket,new Vector3(0,side*0.27f,-0.08f),new Vector3(0.74f,0.08f,0.35f),cyan);
                Part(PrimitiveType.Cube,"Dock guide " + side,TankSocket,new Vector3(side*0.65f,-0.70f,0.55f),new Vector3(0.12f,0.12f,1.7f),dark);
            }
            BuildDroneDock(geometry,hull,dark,cyan,amber);
            Part(PrimitiveType.Cube,"Tank service pipe",geometry,new Vector3(3.9f,-1.7f,-4.3f),new Vector3(3.4f,0.3f,0.3f),dark);
            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? -1 : 1;
                Part(PrimitiveType.Cube,"Solar support " + side,geometry,new Vector3(sign*18f,-0.4f,0),new Vector3(12f,0.5f,0.7f),dark);
                var wing = new GameObject(side == 0 ? "Solar Wing Left" : "Solar Wing Right").transform;
                wing.SetParent(geometry,false); wing.localPosition = new Vector3(sign*24f,0,0);
                wing.localRotation = Quaternion.Euler(side == 0 ? 6f : -11f,0,side == 0 ? -3f : 5f);
                Part(PrimitiveType.Cube,"Panel frame",wing,Vector3.zero,new Vector3(11.4f,0.16f,10.7f),dark);
                for (int x=0;x<7;x++) for(int z=0;z<5;z++)
                {
                    if ((side==0 && x==1 && z==3) || (side==1 && x>=5 && z==0) || (side==1 && x==3 && z==2)) continue;
                    Part(PrimitiveType.Cube,"Solar cell " + x + "-" + z,wing,new Vector3((x-3)*1.55f,0.13f,(z-2)*2.05f),new Vector3(1.43f,0.08f,1.93f),solar);
                }
                Part(PrimitiveType.Cube,"Damaged panel brace",wing,new Vector3(side==0?-3.1f:3.1f,0.25f,side==0?2.05f:-4.1f),new Vector3(2.8f,0.16f,0.18f),hull,Quaternion.Euler(0,side==0?23f:-30f,0));
            }
            Part(PrimitiveType.Cube,"Dock warning",geometry,new Vector3(-7f,1.2f,-1.1f),new Vector3(1.5f,0.1f,0.2f),amber);
        }

        private void BuildDroneDock(Transform geometry, Material hull, Material dark, Material cyan, Material amber)
        {
            DroneDock = new GameObject("Drone Charging Dock").transform;
            DroneDock.SetParent(geometry,false);
            DroneDock.localRotation = Quaternion.Euler(0,150,0);
            DroneDock.localPosition = DroneDock.localRotation * Vector3.forward * 22f + Vector3.up * 1.6f;
            Part(PrimitiveType.Cube,"Dock access bridge",DroneDock,new Vector3(0,-0.4f,-6f),new Vector3(2.2f,0.6f,10f),dark);
            Part(PrimitiveType.Cube,"Charging deck",DroneDock,new Vector3(0,-0.15f,1f),new Vector3(14.4f,0.3f,12.5f),dark);
            Part(PrimitiveType.Cube,"Central maneuver aisle",DroneDock,new Vector3(0,0.015f,0),new Vector3(13.7f,0.025f,2.5f),hull);
            for(int index=0;index<10;index++) {
                bool maintenance=index>=ChargingBerthCount;
                var bay = new GameObject((maintenance ? "Maintenance Bay " : "Charging Bay ") + (index+1).ToString("00")).transform;
                bay.SetParent(DroneDock,false);
                int row=index/4;
                bay.localPosition=maintenance ? new Vector3(index==8 ? -5f : 5f,0,5.5f)
                    : new Vector3((index%4-1.5f)*3.2f,0,row==0 ? -3.1f : 3.1f);
                bay.localRotation=Quaternion.Euler(0,maintenance || row==1 ? 180f : 0f,0);
                Material light=maintenance ? amber : cyan;
                Part(PrimitiveType.Cube,"Bay back wall",bay,new Vector3(0,0.95f,-1.28f),new Vector3(2.6f,1.8f,0.18f),hull);
                Part(PrimitiveType.Cube,"Bay canopy",bay,new Vector3(0,1.85f,-0.48f),new Vector3(2.6f,0.15f,1.7f),hull);
                for(int side=-1;side<=1;side+=2) {
                    Part(PrimitiveType.Cube,"Bay post " + side,bay,new Vector3(side*1.22f,0.9f,0.30f),new Vector3(0.14f,1.8f,0.14f),dark);
                    Part(PrimitiveType.Cube,"Guide rail " + side,bay,new Vector3(side*0.55f,0.25f,-0.05f),new Vector3(0.17f,0.13f,2.1f),hull);
                    Part(PrimitiveType.Cube,"Entry light " + side,bay,new Vector3(side*1.10f,0.04f,1.0f),new Vector3(0.12f,0.04f,0.8f),light);
                }
                Part(PrimitiveType.Cube,"Charging connector",bay,new Vector3(0,0.85f,-1.10f),new Vector3(0.46f,0.24f,0.30f),light);
                berths[index]=new GameObject("Parked drone pose").transform;
                berths[index].SetParent(bay,false); berths[index].localPosition=new Vector3(0,0.88f,-0.12f);
            }
        }
    }
}
