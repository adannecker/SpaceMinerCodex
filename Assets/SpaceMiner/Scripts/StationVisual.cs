using UnityEngine;

namespace SpaceMiner
{
    // Lightweight visual placeholder; logistics continue to use the existing root.
    public sealed class StationVisual : MonoBehaviour
    {
        private readonly Transform[] berths = new Transform[10];
        public const int ChargingBerthCount = 8;
        public Transform TankSocket { get; private set; }
        public Transform DroneDock { get; private set; }
        public Transform Berth(int index) { Build(); return berths[index]; }
        private void Awake() => Build();

        private Material Surface(string name, Color color, float metal, bool glow = false)
        {
            var material = new Material(Shader.Find("Standard")) { name = name, color = color };
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
