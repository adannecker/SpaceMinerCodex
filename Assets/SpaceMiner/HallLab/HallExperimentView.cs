using System;
using UnityEngine;

namespace SpaceMiner
{
    public sealed class HallExperimentView : MonoBehaviour
    {
        public Transform HallRoot,ScaleDrone;
        public Camera ViewCamera;
        public CharacterController Walker;
        public Transform[] DoorFrames,DoorLeft,DoorRight;
        public Renderer[] HallSurfaces;
        public Material[] DetailedMaterials;
        public Material PlainMaterial;
        public Material StarMaterial;
        public Light[] WorkLights;
        public Light DockLight;
        public ReflectionProbe Reflection;
        public readonly PlayerVitals Vitals=new PlayerVitals();
        internal readonly bool[] DoorTargets={false,true,false};
        internal readonly float[] DoorAmounts=new float[3];
        internal bool Detailed {get;private set;}=true;
        internal bool Details {get;private set;}=true;
        internal bool Captured {get;private set;}
        internal int LightingMode {get;private set;}
        internal bool Automated {get;private set;}
        internal Vector3 Feet=>Walker.transform.position;
        private Vector3[] closedLeft,closedRight,slideAxis;
        private float yaw,pitch,fall,messageUntil;
        private string message;
        private int captureFrame;
        private readonly SpaceMinerUi ui=new SpaceMinerUi();
        private readonly AccessibilitySettings style=new AccessibilitySettings();
        private DisplayModeController display;
        private static readonly Vector3[] Eyes={new Vector3(6.5f,1.72f,1.4f),new Vector3(3.4f,1.72f,9.8f),new Vector3(-4.5f,1.72f,11.5f),new Vector3(-8.2f,1.72f,5)};
        private static readonly Vector3[] Targets={new Vector3(-1,2.1f,12),new Vector3(-4,1.8f,10.1f),new Vector3(3.5f,2.2f,15.7f),new Vector3(-9.9f,2.9f,7)};
        private void Start()
        {
            Application.runInBackground=true;Application.targetFrameRate=90;
            Automated=Array.IndexOf(Environment.GetCommandLineArgs(),"-hallExperimentCheck")>=0;
            if(!Application.isEditor)Screen.SetResolution(1600,1000,FullScreenMode.Windowed);
            display=GetComponent<DisplayModeController>();
            closedLeft=new Vector3[3];closedRight=new Vector3[3];slideAxis=new Vector3[3];
            for(int i=0;i<3;i++)
            {
                closedLeft[i]=DoorLeft[i].position;closedRight[i]=DoorRight[i].position;
                slideAxis[i]=(closedRight[i]-closedLeft[i]).normalized;
                DoorAmounts[i]=DoorTargets[i]?1:0;ApplyDoor(i);
            }
            SetView(0);SetCapture(false);SetLighting(0);Stars();
        }
        internal void SetView(int index)=>LookFrom(Eyes[Mathf.Clamp(index,0,3)],Targets[Mathf.Clamp(index,0,3)]);
        internal void LookFrom(Vector3 eye,Vector3 target)
        {
            Walker.enabled=false;Walker.transform.position=eye-Vector3.up*1.65f;Walker.enabled=true;fall=0;
            Vector3 d=(target-eye).normalized;yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;pitch=-Mathf.Asin(d.y)*Mathf.Rad2Deg;
            SetCapture(false);ApplyView();Physics.SyncTransforms();
        }
        internal void SetCapture(bool value)
        {
            Captured=value;captureFrame=Time.frameCount;
            Cursor.lockState=value && !Automated?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!value || Automated;
        }
        private void Update()
        {
            float dt=Mathf.Min(Time.unscaledDeltaTime,.05f);
            if(!Automated)
            {
                if(Input.GetKeyDown(KeyCode.Tab))SetCapture(!Captured);
                if(Input.GetKeyDown(KeyCode.Escape))SetCapture(false);
                for(int i=0;i<4;i++)if(Input.GetKeyDown(KeyCode.Alpha1+i))SetView(i);
                if(Input.GetKeyDown(KeyCode.T))SetDetailed(!Detailed);
                if(Input.GetKeyDown(KeyCode.L))SetLighting((LightingMode+1)%3);
                if(Input.GetKeyDown(KeyCode.E))
                {
                    int door=NearestDoor();if(door>=0)RequestDoor(door,!DoorTargets[door]);
                }
                if(Captured && Application.isFocused)
                {
                    if(Time.frameCount>captureFrame+2){yaw+=Input.GetAxisRaw("Mouse X")*1.9f;pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*1.9f,-80,80);}
                    Move(new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical")),dt,Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift));
                }
                else Vitals.Advance(dt,false,false);
            }
            for(int i=0;i<3;i++)
            {
                if(!DoorTargets[i] && DoorAmounts[i]>.001f && DoorwayOccupied(i)){DoorTargets[i]=true;message="Durchgang belegt / Tor bleibt offen";messageUntil=Time.unscaledTime+2;}
                DoorAmounts[i]=Mathf.MoveTowards(DoorAmounts[i],DoorTargets[i]?1:0,dt*.55f);ApplyDoor(i);
            }
            ApplyView();
        }
        internal void Move(Vector2 input,float dt,bool sprint)
        {
            input=Vector2.ClampMagnitude(input,1);float sprintTime=Vitals.Advance(dt,sprint,input.sqrMagnitude>.001f);
            float distance=PlayerVitals.WalkSpeed*dt+(PlayerVitals.SprintSpeed-PlayerVitals.WalkSpeed)*sprintTime;
            var movement=Quaternion.Euler(0,yaw,0)*new Vector3(input.x,0,input.y)*distance;
            if(Walker.isGrounded && fall<0)fall=-1;else fall=Mathf.Max(-12,fall-12*dt);
            Walker.Move(movement+Vector3.up*fall*dt);
        }
        private void ApplyView(){ViewCamera.transform.SetPositionAndRotation(Feet+Vector3.up*1.65f,Quaternion.Euler(pitch,yaw,0));}
        private void ApplyDoor(int i)
        {
            float t=Mathf.SmoothStep(0,1,DoorAmounts[i]);
            DoorLeft[i].position=closedLeft[i]-slideAxis[i]*1.66f*t;DoorRight[i].position=closedRight[i]+slideAxis[i]*1.66f*t;
        }
        internal bool DoorwayOccupied(int index)
        {
            Vector3 delta=Feet-DoorFrames[index].position;
            Vector3 forward=Vector3.Cross(slideAxis[index],Vector3.up).normalized;
            return Mathf.Abs(Vector3.Dot(delta,slideAxis[index]))<1.92f && Mathf.Abs(Vector3.Dot(delta,forward))<.85f && Feet.y<3.5f;
        }
        internal bool RequestDoor(int index,bool open)
        {
            if(!open && DoorwayOccupied(index)){message="Durchgang belegt / Tor bleibt offen";messageUntil=Time.unscaledTime+2;return false;}
            DoorTargets[index]=open;return true;
        }
        private int NearestDoor()
        {
            int best=-1;float distance=3;
            for(int i=0;i<3;i++){Vector3 d=Feet-DoorFrames[i].position;d.y=0;if(d.magnitude<distance){distance=d.magnitude;best=i;}}
            return best;
        }
        internal void SetDetailed(bool enabled)
        {Detailed=enabled;for(int i=0;i<HallSurfaces.Length;i++)HallSurfaces[i].sharedMaterial=enabled?DetailedMaterials[i]:PlainMaterial;}
        internal void SetDetails(bool enabled)
        {
            Details=enabled;
            foreach(var r in HallSurfaces)if(r.name.StartsWith("Fasteners__")||r.name.StartsWith("VentBlades__")||r.name.StartsWith("UtilityPipes__")||r.name.StartsWith("CableTray__")||r.name.StartsWith("BenchTools__")||r.name.StartsWith("DeckLocks__"))r.enabled=enabled;
        }
        internal void SetLighting(int mode)
        {
            LightingMode=mode;
            foreach(var light in WorkLights)
            {
                light.color=mode==0?new Color(1,.86f,.68f):mode==1?new Color(.83f,.93f,1):new Color(1,.28f,.07f);
                light.intensity=mode==2?.30f:mode==1?2.7f:2;
            }
            DockLight.intensity=mode==2?.20f:.7f;
        }
        private void Stars()
        {
            var vertices=new Vector3[180*3];var tris=new int[vertices.Length];var random=new System.Random(39);
            for(int i=0;i<180;i++)
            {
                Vector3 p=new Vector3((float)random.NextDouble()*180-90,(float)random.NextDouble()*65-12,70+(float)random.NextDouble()*35);
                float radius=.025f+(float)random.NextDouble()*.065f;
                vertices[i*3]=p+new Vector3(-radius,-radius,0);vertices[i*3+1]=p+new Vector3(0,radius,0);vertices[i*3+2]=p+new Vector3(radius,-radius,0);
                for(int j=0;j<3;j++)tris[i*3+j]=i*3+j;
            }
            var mesh=new Mesh{name="Sternhintergrund",vertices=vertices,triangles=tris};mesh.RecalculateBounds();
            var stars=new GameObject("Experiment / Sternhintergrund");stars.AddComponent<MeshFilter>().sharedMesh=mesh;
            stars.AddComponent<MeshRenderer>().sharedMaterial=StarMaterial;
        }
        private void OnApplicationFocus(bool focused){if(!focused)SetCapture(false);}
        private void OnGUI()
        {
            float s=Mathf.Min(Screen.width/1600f,Screen.height/1000f);var old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(Vector3.one*s);
            ui.Configure(style);
            SpaceMinerUi.Fill(new Rect(22,20,570,83),new Color(.012f,.026f,.038f,.91f));
            GUI.Label(new Rect(40,29,540,34),"S02 / DROHNENWERKSTATT",ui.Heading);
            GUI.Label(new Rect(42,68,530,26),"MATERIALVERSUCH 01   /   16 m TIEFE   /   5,5 m HÖHE",ui.Small);
            if(!Captured)
            {
                ui.Panel(new Rect(1280,20,298,620));
                GUI.Label(new Rect(1300,38,250,35),"HALLE ERKUNDEN",ui.Heading);
                if(GUI.Button(new Rect(1300,85,258,43),"Begehen / Tab",ui.Button)&&!Automated)SetCapture(true);
                string[] labels={"01 / Raum und Maßstab","02 / Drohnenarbeitsplatz","03 / Äußere Schleusen","04 / Material im Detail"};
                for(int i=0;i<4;i++)if(GUI.Button(new Rect(1300,147+i*44,258,37),labels[i],ui.Button)&&!Automated)SetView(i);
                if(GUI.Button(new Rect(1300,340,258,38),Detailed?"Oberflächen: ausgearbeitet / T":"Oberflächen: schlicht / T",ui.Button)&&!Automated)SetDetailed(!Detailed);
                if(GUI.Button(new Rect(1300,386,258,38),Details?"Bauteildetails: an":"Bauteildetails: aus",ui.Button)&&!Automated)SetDetails(!Details);
                string light=LightingMode==0?"Arbeitslicht":LightingMode==1?"Inspektionslicht":"Notlicht";
                if(GUI.Button(new Rect(1300,432,258,38),light+" / L",ui.Button)&&!Automated)SetLighting((LightingMode+1)%3);
                if(GUI.Button(new Rect(1300,478,258,38),Screen.fullScreen?"Fenster / F11":"Vollbild / F11",ui.Button)&&!Automated)display.Toggle();
                if(GUI.Button(new Rect(1300,571,258,40),"Experiment schließen",ui.Button)&&!Automated)Application.Quit();
            }
            else GUI.Label(new Rect(791,481,20,25),"+",ui.Text);
            SpaceMinerUi.Fill(new Rect(22,914,1556,67),new Color(.012f,.026f,.038f,.92f));
            string door=NearestDoor()>=0?"E: TOR ÖFFNEN / SCHLIESSEN   /   ":"";
            GUI.Label(new Rect(40,922,1500,25),door+"WASD: Gehen   /   Shift: Sprinten   /   Maus: Umsehen   /   Tab oder Esc: Maus frei",ui.Small);
            GUI.Label(new Rect(40,950,1500,25),Time.unscaledTime<messageUntil?message:"1–4: Blickpunkte   /   T: Materialvergleich   /   L: Licht wechseln   /   F11: Vollbild",ui.Small);
            if(Captured)
            {
                ui.Panel(new Rect(22,721,270,177));
                string[] labels={"STAMINA","MÜDIGKEIT","HUNGER","DURST","GESUNDHEIT"};float[] values={Vitals.Stamina,Vitals.Fatigue,Vitals.Hunger,Vitals.Thirst,Vitals.Health};
                for(int i=0;i<5;i++)
                {
                    GUI.Label(new Rect(38,733+i*30,140,27),labels[i],ui.Small);
                    SpaceMinerUi.Fill(new Rect(183,741+i*30,90,10),new Color(.08f,.14f,.18f));
                    SpaceMinerUi.Fill(new Rect(183,741+i*30,90*values[i]/100,10),i==0?SpaceMinerUi.Amber:SpaceMinerUi.Cyan);
                }
            }
            GUI.matrix=old;
        }
        private void OnDestroy(){SetCapture(false);ui.Dispose();}
    }
}
