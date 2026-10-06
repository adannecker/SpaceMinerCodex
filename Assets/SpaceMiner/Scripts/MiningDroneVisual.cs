using UnityEngine;

namespace SpaceMiner
{
    public sealed class MiningDroneVisual : MonoBehaviour
    {
        private DroneAgent agent;
        private Transform drill;
        private Transform arm;
        private Transform upperArm, forearm, elbow, palm, hatch, chunk, drillShaft;
        private readonly Transform[] clamps = new Transform[3];
        private readonly Transform[] crystals = new Transform[36];
        private readonly Transform[] unloadChunks = new Transform[8];
        public Vector3 ContactPoint { get; private set; }
        public bool ClampDeployed { get; private set; }
        public bool HatchOpen { get; private set; }
        public bool IceSprayActive { get; private set; }
        public bool ChunkVisible => chunk != null && chunk.gameObject.activeSelf;
        private Material[] materials;

        private Material Surface(string name, Color color, float metal, bool glow = false)
        {
            var material = new Material(Shader.Find("Standard")) { name = name, color = color };
            material.SetFloat("_Metallic", metal); material.SetFloat("_Glossiness", 0.28f);
            if (glow) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color); }
            return material;
        }

        private Transform Part(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material, Quaternion rotation = default)
        {
            var item = GameObject.CreatePrimitive(type); item.name = name;
            item.transform.SetParent(parent,false); item.transform.localPosition = position;
            item.transform.localScale = scale;
            item.transform.localRotation = rotation.Equals(default(Quaternion)) ? Quaternion.identity : rotation;
            item.GetComponent<Renderer>().sharedMaterial = material;
            return item.transform;
        }

        public void Build(DroneAgent owner)
        {
            agent = owner;
            if (transform.Find("Mining Drone Geometry") != null) return;
            foreach (string legacy in new[] { "Body", "Status Light" })
            {
                var old = transform.Find(legacy); if (old != null) old.gameObject.SetActive(false);
            }
            bool ready = owner.IsOperational && !owner.NeedsInitialCharge;
            Material hull = Surface("Drone hull",new Color(0.45f,0.52f,0.56f),0.65f);
            Material dark = Surface("Drone frame",new Color(0.08f,0.11f,0.15f),0.7f);
            Material metal = Surface("Drone tools",new Color(0.62f,0.66f,0.68f),0.85f);
            Material amber = Surface("Drone markings",new Color(0.95f,0.46f,0.08f),0.2f);
            Material status = Surface("Drone status",ready ? new Color(0.06f,0.7f,0.9f) : new Color(0.13f,0.16f,0.18f),0f,ready);
            Material ice = Surface("Fresh ice", new Color(0.50f,0.88f,1f),0.1f,true);
            materials = new[] { hull,dark,metal,amber,status,ice };
            var body = new GameObject("Mining Drone Geometry").transform; body.SetParent(transform,false);
            Part(PrimitiveType.Cube,"Main chassis",body,Vector3.zero,new Vector3(1.25f,0.8f,1.45f),hull);
            Part(PrimitiveType.Cube,"Cargo container",body,new Vector3(0,0.46f,-0.15f),new Vector3(1.05f,0.42f,0.9f),dark);
            Part(PrimitiveType.Cube,"Cargo opening",body,new Vector3(0,0.69f,-0.15f),new Vector3(0.98f,0.025f,0.83f),dark);
            hatch = new GameObject("Cargo hatch hinge").transform; hatch.SetParent(body,false); hatch.localPosition = new Vector3(0,0.7f,-0.565f);
            Part(PrimitiveType.Cube,"Cargo lid",hatch,new Vector3(0,0,0.415f),new Vector3(0.98f,0.06f,0.83f),hull);
            Part(PrimitiveType.Cube,"Sensor visor",body,new Vector3(0,0.22f,0.74f),new Vector3(0.65f,0.15f,0.05f),status);
            Part(PrimitiveType.Cube,"Warning stripe",body,new Vector3(0,-0.26f,0.74f),new Vector3(0.9f,0.1f,0.04f),amber);
            for (int side=-1;side<=1;side+=2)
            {
                Part(PrimitiveType.Cube,"Thruster pod " + side,body,new Vector3(side*0.78f,-0.10f,-0.27f),new Vector3(0.44f,0.42f,0.8f),dark);
                Part(PrimitiveType.Cylinder,"Rear nozzle " + side,body,new Vector3(side*0.78f,-0.10f,-0.76f),new Vector3(0.28f,0.12f,0.28f),metal,Quaternion.Euler(90,0,0));
                Part(PrimitiveType.Cube,"Pod marking " + side,body,new Vector3(side*0.78f,0.13f,-0.27f),new Vector3(0.35f,0.06f,0.4f),amber);
                Part(PrimitiveType.Cube,"Landing rail " + side,body,new Vector3(side*0.52f,-0.52f,-0.12f),new Vector3(0.13f,0.12f,1.4f),metal);
            }
            var armRoot = new GameObject("Gripper Arm").transform; armRoot.SetParent(body,false); armRoot.localPosition = new Vector3(-0.56f,0,0.52f); arm = armRoot;
            Part(PrimitiveType.Cylinder,"Shoulder joint",arm,Vector3.zero,new Vector3(0.28f,0.17f,0.28f),metal,Quaternion.Euler(0,0,90));
            upperArm = Part(PrimitiveType.Cube,"Upper arm",body,Vector3.zero,Vector3.one,hull);
            elbow = Part(PrimitiveType.Sphere,"Elbow",body,Vector3.zero,Vector3.one*0.28f,dark);
            forearm = Part(PrimitiveType.Cube,"Forearm",body,Vector3.zero,Vector3.one,metal);
            palm = Part(PrimitiveType.Cube,"Gripper palm",body,Vector3.zero,new Vector3(0.36f,0.14f,0.16f),dark);
            for(int finger=-1;finger<=1;finger+=2)
                Part(PrimitiveType.Cube,"Gripper finger " + finger,palm,new Vector3(finger*0.44f,0,1.1f),new Vector3(0.20f,0.9f,1.8f),metal,Quaternion.Euler(0,-finger*12,0));
            Part(PrimitiveType.Cube,"Drill mount",body,new Vector3(0.47f,-0.10f,0.68f),new Vector3(0.34f,0.36f,0.5f),amber);
            var bit = new GameObject("Mining Drill").transform; bit.SetParent(body,false); bit.localPosition = new Vector3(0.47f,-0.10f,0.96f); drill = bit;
            for(int step=0;step<5;step++)
                Part(PrimitiveType.Cylinder,"Drill step " + step,bit,new Vector3(0,0,step*0.12f),new Vector3(0.30f-step*0.05f,0.075f,0.30f-step*0.05f),metal,Quaternion.Euler(90,0,0));
            for(int tooth=0;tooth<3;tooth++)
            {
                float a=tooth*120f*Mathf.Deg2Rad;
                Part(PrimitiveType.Cube,"Drill flute " + tooth,bit,new Vector3(Mathf.Sin(a)*0.12f,Mathf.Cos(a)*0.12f,0.14f),new Vector3(0.06f,0.06f,0.33f),dark,Quaternion.Euler(0,0,tooth*120f));
            }
            drillShaft = Part(PrimitiveType.Cube,"Telescopic drill shaft",body,Vector3.zero,Vector3.one,metal);
            for (int i=0;i<3;i++)
            {
                clamps[i] = Part(PrimitiveType.Cube,"Docking clamp " + i,body,Vector3.zero,Vector3.one,metal);
                Part(PrimitiveType.Cube,"Clamp foot",clamps[i],new Vector3(0,0,0.5f),new Vector3(2.4f,2.4f,0.12f),amber);
                for(int side=-1;side<=1;side+=2)
                    Part(PrimitiveType.Cube,"Clamp jaw " + side,clamps[i],new Vector3(side*1.1f,0,0.51f),new Vector3(0.45f,2f,0.18f),metal,Quaternion.Euler(0,-side*25f,0));
            }
            chunk = Part(PrimitiveType.Sphere,"Collected ice chunk",body,Vector3.zero,new Vector3(0.23f,0.16f,0.2f),ice);
            for (int i=0;i<crystals.Length;i++)
                crystals[i] = Part(PrimitiveType.Cube,"Ice crystal " + i,body,Vector3.zero,Vector3.one*0.04f,ice);
            for (int i=0;i<unloadChunks.Length;i++)
                unloadChunks[i] = Part(PrimitiveType.Sphere,"Tank transfer ice " + i,body,Vector3.zero,Vector3.one*0.15f,ice);
            // Moving presentation pieces must not interfere with selection rays or surface queries.
            foreach (var collider in body.GetComponentsInChildren<Collider>()) collider.enabled = false;
            // Keep one hull collider for drone selection.
            body.Find("Main chassis").GetComponent<Collider>().enabled = true;
            RefreshPose();
        }

        private void LateUpdate()
        {
            if (agent == null || drill == null) return;
            RefreshPose();
        }

        // Animation follows simulation time, including pause, settings pause and fast-forward.
        public void RefreshPose()
        {
            bool mining = agent.Phase == DronePhase.Mining;
            float time = agent.PhaseElapsed;
            float dock = agent.Phase == DronePhase.Docking ? Mathf.Clamp01((time-50f)/35f)
                : mining ? 1f : agent.Phase == DronePhase.Undocking ? 1f-Mathf.Clamp01(time/60f) : 0f;
            float deploy = agent.Phase == DronePhase.Docking ? Mathf.Clamp01((time-85f)/35f)
                : mining ? 1f : agent.Phase == DronePhase.Undocking ? 1f-Mathf.Clamp01(time/30f) : 0f;
            ClampDeployed = dock > 0.99f;
            Vector3 mount = new Vector3(0.47f,-0.1f,0.78f);
            ContactPoint = SurfaceContact(transform.TransformPoint(mount));
            Vector3 contact = transform.InverseTransformPoint(ContactPoint);
            Vector3 bitPosition = Vector3.Lerp(new Vector3(0.47f,-0.1f,0.96f),contact-Vector3.forward*0.55f,deploy);
            drill.localPosition = bitPosition + (mining ? new Vector3(Mathf.Sin(time*2.7f),Mathf.Cos(time*3.1f),Mathf.Sin(time*3.7f))*0.014f : Vector3.zero);
            drill.localRotation = Quaternion.Euler(0,0,mining ? time*12f : 0);
            Link(drillShaft,mount,bitPosition,0.12f);
            for (int i=0;i<3;i++)
            {
                float a=(i*120f+30f)*Mathf.Deg2Rad;
                Vector3 anchor=new Vector3(Mathf.Cos(a)*0.68f,Mathf.Sin(a)*0.5f,0.65f);
                Vector3 foot=transform.InverseTransformPoint(SurfaceContact(transform.TransformPoint(anchor)));
                Link(clamps[i],anchor,Vector3.Lerp(anchor+Vector3.forward*0.22f,foot,dock),0.10f);
            }
            bool unloading=agent.Phase == DronePhase.Unloading && agent.CargoKg > 0;
            HatchOpen=mining || unloading;
            hatch.localRotation=Quaternion.Euler(HatchOpen ? -105f : 0,0,0);
            float cycle=Mathf.Repeat(time,120f)/120f;
            Vector3 rest=new Vector3(-0.56f,-0.05f,1.72f), mouth=new Vector3(0,0.87f,-0.15f);
            Vector3 loose=contact+new Vector3(-0.25f,0.18f,-0.22f);
            Vector3 grip=rest;
            if(mining)
            {
                if(cycle<0.4f) grip=Vector3.Lerp(rest,loose,Mathf.SmoothStep(0,1,Mathf.Clamp01((cycle-0.12f)/0.28f)));
                else if(cycle<0.8f) grip=Vector3.Lerp(loose,mouth,Mathf.SmoothStep(0,1,(cycle-0.4f)/0.4f))+Vector3.up*Mathf.Sin((cycle-0.4f)/0.4f*Mathf.PI)*0.65f;
                else grip=Vector3.Lerp(mouth,rest,Mathf.SmoothStep(0,1,(cycle-0.8f)/0.2f));
            }
            Vector3 shoulder=arm.localPosition;
            Vector3 joint=(shoulder+grip)*0.5f+Vector3.up*0.30f;
            elbow.localPosition=joint;
            Link(upperArm,shoulder,joint,0.18f); Link(forearm,joint,grip,0.14f);
            palm.localPosition=grip; palm.localRotation=Quaternion.identity;
            foreach(Transform finger in palm)
                finger.localRotation=Quaternion.Euler(0,(finger.localPosition.x<0 ? 1 : -1)*(mining && cycle>=0.4f && cycle<0.8f ? 28f : 8f),0);
            chunk.gameObject.SetActive(mining && cycle<0.83f);
            chunk.localPosition=cycle<0.4f ? Vector3.Lerp(contact,loose,Mathf.Clamp01(cycle/0.2f)) : cycle<0.8f ? grip : Vector3.Lerp(mouth,mouth-Vector3.up*0.5f,(cycle-0.8f)/0.03f);
            chunk.localRotation=Quaternion.Euler(time*3f,time*2f,time*5f);
            IceSprayActive=mining;
            for(int i=0;i<crystals.Length;i++)
            {
                crystals[i].gameObject.SetActive(mining);
                float age=Mathf.Repeat(time+i*2.1f,70f)/70f;
                float a=i*2.39996f;
                Vector3 eject=new Vector3(Mathf.Cos(a)*0.7f,Mathf.Sin(a)*0.7f,-0.5f-(i%4)*0.2f);
                crystals[i].localPosition=contact+eject*age;
                crystals[i].localScale=Vector3.one*(0.045f*(1f-age));
                crystals[i].localRotation=Quaternion.Euler(i*17f+time*2f,i*43f,time*4f);
            }
            for(int i=0;i<unloadChunks.Length;i++)
            {
                unloadChunks[i].gameObject.SetActive(unloading && time>30f);
                float progress=Mathf.Repeat((time-30f)/65f+i/8f,1f);
                unloadChunks[i].position=Vector3.Lerp(transform.TransformPoint(mouth),agent.TankInlet,progress)+Vector3.up*Mathf.Sin(progress*Mathf.PI)*0.5f;
            }
        }

        private Vector3 SurfaceContact(Vector3 origin)
        {
            var surface=agent.Target != null ? agent.Target.GetComponent<Collider>() : null;
            if(surface != null && surface.Raycast(new Ray(origin,transform.forward),out RaycastHit hit,8f)) return hit.point;
            return agent.SurfacePoint;
        }

        private static void Link(Transform link, Vector3 start, Vector3 end, float width)
        {
            Vector3 delta=end-start;
            link.localPosition=(start+end)*0.5f;
            link.localRotation=delta.sqrMagnitude>0.000001f ? Quaternion.LookRotation(delta) : Quaternion.identity;
            link.localScale=new Vector3(width,width,Mathf.Max(0.01f,delta.magnitude));
        }

        private void OnDestroy() { if (materials != null) foreach (var material in materials) Destroy(material); }
    }
}
