using UnityEngine;

namespace SpaceMiner
{
    // Lightweight visual placeholder; logistics continue to use the existing root.
    public sealed class StationVisual : MonoBehaviour
    {
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
            Part(PrimitiveType.Cube,"Ice unloading inlet frame",geometry,new Vector3(5.7f,3.3f,-5.96f),new Vector3(0.8f,0.65f,0.13f),cyan);
            Part(PrimitiveType.Cube,"Ice unloading inlet",geometry,new Vector3(5.7f,3.3f,-6.04f),new Vector3(0.6f,0.45f,0.03f),dark);
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
    }
}
