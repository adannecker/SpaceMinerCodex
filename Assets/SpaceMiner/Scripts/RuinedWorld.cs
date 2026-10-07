using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceMiner
{
    // Distant scenery only; danger is communicated visually, not a new damage rule.
    [DefaultExecutionOrder(1000)] // Synchronize the sky after gameplay and menu camera movement.
    public sealed class RuinedWorld : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
        static void Loaded(Scene scene,LoadSceneMode mode){if(FindFirstObjectByType<OrbitCamera>()!=null&&FindFirstObjectByType<RuinedWorld>()==null)new GameObject("Sun and shattered homeworld").AddComponent<RuinedWorld>();}
        Material Mat(string name,Color color,bool glow=false)
        {
            var m=new Material(Shader.Find(glow ? "SpaceMiner/EmissiveSurface" : "SpaceMiner/SolarTerrain")){name=name,color=color};m.SetFloat("_Glossiness",0);
            var texture=new Texture2D(256,128,TextureFormat.RGB24,false);
            for(int y=0;y<128;y++)for(int x=0;x<256;x++){
                float n=Mathf.PerlinNoise(x*.065f,y*.065f)*.65f+Mathf.PerlinNoise(x*.19f+5,y*.19f)*.35f;
                float intensity=glow? .12f+n*n*.88f:.25f+n*.75f;texture.SetPixel(x,y,new Color(intensity,intensity,intensity));
            }
            texture.Apply();m.mainTexture=texture;
            if(glow){m.EnableKeyword("_EMISSION");m.SetTexture("_EmissionMap",texture);m.SetColor("_EmissionColor",color*1.5f);}return m;
        }
        // Deliberately compressed celestial display in kilometres; local docking remains in metres.
        public const float AstronomicalUnit = 1800f;
        public const float MoonDistance = 18f;
        public const float StationDistance = MoonDistance * 5;
        static readonly Vector3 InitialHome = new Vector3(-.25f,.02f,1).normalized * StationDistance;
        public static readonly Vector3 SunPosition = InitialHome - new Vector3(Mathf.Cos(-42.746f*Mathf.Deg2Rad),0,Mathf.Sin(-42.746f*Mathf.Deg2Rad))*AstronomicalUnit;
        public Camera SkyCamera { get; private set; }
        Camera localCamera; Light solarLight, companionLight;
        public static readonly Vector3 CompanionPosition = SunPosition + new Vector3(240,300,120);
        public const float SunRadius = 55;
        public const float HomeRadius = 4;
        public Vector3 HomePosition => orbits[3].position;
        public Transform HomeMoon { get; private set; }
        readonly Transform[] orbits = new Transform[9];
        readonly LineRenderer[] guides=new LineRenderer[9];
        readonly float[] distances = {650,1050,1400,AstronomicalUnit,2500,3500,4700,6100,7800};
        public float OverviewRadius => distances[8]+radii[8];
        readonly float[] radii = {1.5f,3.8f,1.9f,HomeRadius,2.1f,44,36,16,15};
        readonly float[] angles = {25,120,215,-42.746f,68,160,250,325,100};
        readonly float[] inclinations = {3,-5,7,0,4,-8,6,9,-4};
        Material crust, lava;
        readonly SpaceMinerUi planetUi=new SpaceMinerUi();
        int selectedPlanet=-1;Vector2 infoScroll;
        public int SelectedPlanet => selectedPlanet;
        float NavigationY => 120*Mathf.Clamp(Mathf.Min(Screen.height/900f,Screen.width/1440f)*SettingsStore.Current.Interface.Scale,.35f,1.5f);
        Rect InfoRect => new Rect((Screen.width-Mathf.Min(480,Screen.width-32))*.5f,NavigationY+40,Mathf.Min(480,Screen.width-32),Mathf.Min(370,Screen.height-NavigationY-60));
        public void SelectPlanet(int index)
        {selectedPlanet=index>=0&&index<orbits.Length?index:-1;infoScroll=Vector2.zero;}
        public bool TrySelectPlanet(Vector3 screenPoint)
        {
            int closest=-1;float best=float.MaxValue;
            for(int i=0;i<orbits.Length;i++)
            {
                Vector3 p=SkyCamera.WorldToScreenPoint(orbits[i].position);if(p.z<=0)continue;
                Vector3 rim=SkyCamera.WorldToScreenPoint(orbits[i].position+SkyCamera.transform.right*radii[i]);
                float radius=Mathf.Max(15,Vector2.Distance(p,rim));float score=Vector2.Distance(screenPoint,p)/radius;
                if(score<=1&&score<best){closest=i;best=score;}
            }
            if(closest<0)return false;SelectPlanet(closest);return true;
        }
        public bool OwnsScreenPoint(Vector3 mouse)
        {
            if(StartMenu.IsOpen||localCamera==null||!localCamera.GetComponent<OrbitCamera>().ShowHud)return false;
            Vector2 point=new Vector2(mouse.x,Screen.height-mouse.y);
            if(new Rect(Screen.width*.5f-195,NavigationY,390,28).Contains(point)||(selectedPlanet>=0&&InfoRect.Contains(point)))return true;
            if(localCamera.GetComponent<OrbitCamera>().Distance>500000)
                for(int i=0;i<orbits.Length;i++){Vector3 p=SkyCamera.WorldToScreenPoint(orbits[i].position);if(p.z>0&&new Rect(p.x-8,Screen.height-p.y-12,145,28).Contains(point))return true;}
            return false;
        }
        void Start()
        {
            crust=Mat("Scorched basalt",new Color(.27f,.24f,.20f));
            lava=Mat("Fractured molten interior",new Color(1,.21f,.035f),true);
            Sphere("Sun",SunPosition,SunRadius,Mat("Sun photosphere",new Color(1,.65f,.18f),true));
            Cloud("Solar corona",SunPosition,SunRadius*1.04f,new Color(1,.34f,.035f,.18f),.13f,.27f,35);
            Sphere("Companion sun",CompanionPosition,SunRadius*.72f,Mat("Companion photosphere",new Color(.7f,.84f,1),true));
            Cloud("Companion corona",CompanionPosition,SunRadius*.75f,new Color(.4f,.65f,1,.16f),.13f,.27f,28);
            var sunlight=new GameObject("Sunlight",typeof(Light));sunlight.transform.SetParent(transform);sunlight.transform.rotation=Quaternion.LookRotation(-SunPosition);var light=sunlight.GetComponent<Light>();light.type=LightType.Directional;light.color=new Color(1,.86f,.67f);light.intensity=1.5f;light.shadows=LightShadows.Soft;solarLight=light;
            light.intensity=2.4f;
            var companion=new GameObject("Companion sunlight",typeof(Light));companion.transform.SetParent(transform);companionLight=companion.GetComponent<Light>();companionLight.type=LightType.Directional;companionLight.color=new Color(.78f,.87f,1);companionLight.intensity=1.1f;companionLight.shadows=LightShadows.Soft;
            ConfigureLocalSun(light, SunPosition);
            ConfigureLocalSun(companionLight, CompanionPosition);
            foreach(var other in FindObjectsByType<Light>(FindObjectsSortMode.None))if(other!=light&&other!=companionLight&&other.type==LightType.Directional)other.enabled=false;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.65f,.70f,.78f);RenderSettings.ambientIntensity=1;RenderSettings.reflectionIntensity=.35f;RenderSettings.sun=light;
            var ambientProbe=new UnityEngine.Rendering.SphericalHarmonicsL2();ambientProbe.AddAmbientLight(RenderSettings.ambientLight*1.6f);RenderSettings.ambientProbe=ambientProbe;
            // Painted station cladding needs diffuse light rather than a black-sky metallic reflection.
            foreach(var station in FindObjectsByType<StationVisual>(FindObjectsSortMode.None))
                foreach(var renderer in station.GetComponentsInChildren<Renderer>())
                    foreach(var material in renderer.sharedMaterials)
                        if(material!=null&&material.HasProperty("_Metallic"))material.SetFloat("_Metallic",Mathf.Min(.2f,material.GetFloat("_Metallic")));
            Shader.SetGlobalVector("_SpaceMinerSunPosition",SunPosition);
            Shader.SetGlobalVector("_SpaceMinerCompanionPosition",CompanionPosition);
            localCamera=Camera.main;localCamera.cullingMask &= ~((1<<29)|(1<<28));localCamera.clearFlags=CameraClearFlags.Depth;
            SkyCamera=new GameObject("Celestial camera (kilometres)",typeof(Camera)).GetComponent<Camera>();
            SkyCamera.transform.SetParent(transform);SkyCamera.depth=localCamera.depth-1;SkyCamera.cullingMask=1<<29;SkyCamera.eventMask=0;SkyCamera.clearFlags=CameraClearFlags.SolidColor;SkyCamera.backgroundColor=Color.black;SkyCamera.nearClipPlane=.01f;SkyCamera.farClipPlane=1000000;
            foreach(var stars in FindObjectsByType<Starfield>(FindObjectsSortMode.None)){stars.gameObject.layer=28;stars.View=SkyCamera;stars.transform.localScale=Vector3.one*1000;}
            Color[] colors={new Color(.48f,.31f,.19f),new Color(.67f,.49f,.26f),new Color(.3f,.42f,.5f),Color.gray,new Color(.62f,.43f,.27f),new Color(.64f,.57f,.37f),new Color(.28f,.5f,.57f),new Color(.18f,.31f,.62f),new Color(.37f,.25f,.42f)};
            for(int i=0;i<9;i++)
            {
                var body=new GameObject("Planet "+(i+1)+(i==3?" - shattered homeworld":""));body.transform.SetParent(transform);orbits[i]=body.transform;PositionOrbit(i);
                if(i==3)BuildHomeworld(body.transform);
                else {var surface=Sphere("Planet "+(i+1)+" surface",body.transform.position,radii[i],Mat("Planet "+(i+1)+" terrain",colors[i]));surface.transform.SetParent(body.transform,true);}
                if(i==2||i==6||i==7)Ring(body.transform,radii[i],colors[i],i==5?24:-17);
                var guide=new GameObject("Orbit guide "+(i+1),typeof(LineRenderer));guide.transform.SetParent(transform);guide.layer=29;
                var line=guide.GetComponent<LineRenderer>();guides[i]=line;line.loop=true;line.positionCount=180;line.useWorldSpace=true;
                line.sharedMaterial=new Material(Shader.Find("SpaceMiner/OrbitGuide")){color=i==3?new Color(1,.55f,.2f,.45f):new Color(.25f,.5f,.65f,.28f)};
                line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;
                for(int p=0;p<180;p++){float a=p*Mathf.PI*2/180;line.SetPosition(p,SunPosition+Quaternion.Euler(inclinations[i],0,0)*new Vector3(Mathf.Cos(a)*distances[i],0,Mathf.Sin(a)*distances[i]));}
                line.enabled=false;
            }
            foreach(var child in GetComponentsInChildren<Renderer>())
            {
                child.gameObject.layer=29;
                // Celestial meshes are expressed in kilometres, not the local scene's metres.
                // Their shaders calculate sunlight independently; local shadow maps must not include them.
                child.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                child.receiveShadows=false;
            }
        }
        static void ConfigureLocalSun(Light light, Vector3 celestialPosition)
        {
            // A distant sun illuminates the station from a fixed world direction, independent of the observer.
            light.transform.rotation=Quaternion.LookRotation(-celestialPosition);
            light.cullingMask=~((1<<29)|(1<<28));
            light.renderMode=LightRenderMode.ForcePixel;
            light.shadowStrength=.65f;
            light.shadowResolution=UnityEngine.Rendering.LightShadowResolution.VeryHigh;
            light.shadowBias=.08f;
            light.shadowNormalBias=.15f;
        }
        void LateUpdate()
        {
            if(localCamera==null||SkyCamera==null)return;
            SkyCamera.transform.SetPositionAndRotation(localCamera.transform.position/1000f,localCamera.transform.rotation);
            SkyCamera.fieldOfView=localCamera.fieldOfView;SkyCamera.aspect=localCamera.aspect;
            SkyCamera.cullingMask=(1<<29)|(localCamera.GetComponent<OrbitCamera>().IsCelestialView&&!StationInteriorMode.IsInside?0:1<<28);
            var orbit=localCamera.GetComponent<OrbitCamera>();
            foreach(var line in guides){line.enabled=orbit.Distance>500000&&!orbit.IsCelestialView;line.startWidth=line.endWidth=orbit.Distance/1000f*.0007f;}
        }
        void OnGUI()
        {
            if(StationInteriorMode.IsInside)return;
            if(StartMenu.IsOpen || IntroSequence.BlocksGameplay || SettingsMenu.BlocksInput || localCamera==null)return;
            var orbit=localCamera.GetComponent<OrbitCamera>();if(orbit==null||!orbit.ShowHud)return;
            planetUi.Configure(SettingsStore.Current.Accessibility);
            bool telescope=orbit.IsCelestialView;
            if(GUI.Button(new Rect(Screen.width*.5f-195,NavigationY,190,28),telescope?"Zur Station (R)":"Heimatwelt ansehen",planetUi.Button))
            {if(telescope)orbit.ResetView();else orbit.LookAtCelestial(HomePosition-localCamera.transform.position/1000f,12);}
            if(GUI.Button(new Rect(Screen.width*.5f+5,NavigationY,190,28),"Sonnensystem",planetUi.Button))orbit.SolarOverview(SunPosition,distances[8]+radii[8]);
            if(orbit.Distance>500000&&!orbit.IsCelestialView)
            {
                LabelBody(SunPosition,"Hauptsonne");LabelBody(CompanionPosition,"Begleitsonne");
                for(int i=0;i<9;i++)LabelPlanet(i);
            }
            if(selectedPlanet>=0)DrawPlanetInfo(orbit);
        }
        void LabelPlanet(int index)
        {
            Vector3 p=SkyCamera.WorldToScreenPoint(orbits[index].position);if(p.z<=0||p.x<0||p.x>Screen.width||p.y<0||p.y>Screen.height)return;
            var style=new GUIStyle(GUI.skin.label);style.normal.textColor=index==3?SpaceMinerUi.Amber:SpaceMinerUi.Cyan;
            string name=PlanetLore.All[index].Name+(index==3?" · Heimat":"");
            if(GUI.Button(new Rect(p.x-8,Screen.height-p.y-12,145,28),"● "+name,style)){SelectPlanet(index);SettingsUiAudio.Activate();}
        }
        void DrawPlanetInfo(OrbitCamera orbit)
        {
            planetUi.Configure(SettingsStore.Current.Accessibility);var r=InfoRect;var item=PlanetLore.All[selectedPlanet];
            int oldDepth=GUI.depth;GUI.depth=-20;
            planetUi.Panel(r);
            GUI.Label(new Rect(r.x+20,r.y+14,r.width-80,36),item.Name+" · "+(selectedPlanet+1),planetUi.Heading);
            if(GUI.Button(new Rect(r.xMax-52,r.y+14,40,34),"×",planetUi.Button))SelectPlanet(-1);
            else
            {
                string text=item.Kind+"\n\n"+item.Description+"\n\nArchiv: "+item.Archive+"\n\nStatus: "+item.Status;
                var area=new Rect(r.x+20,r.y+62,r.width-40,r.height-130);
                float height=planetUi.Text.CalcHeight(new GUIContent(text),area.width-22);
                infoScroll=GUI.BeginScrollView(area,infoScroll,new Rect(0,0,area.width-22,height));
                GUI.Label(new Rect(0,0,area.width-22,height),text,planetUi.Text);GUI.EndScrollView();
                if(GUI.Button(new Rect(r.x+20,r.yMax-52,r.width-40,34),"Planet ansehen",planetUi.Button))orbit.LookAtCelestial(orbits[selectedPlanet].position-localCamera.transform.position/1000f,12);
            }
            GUI.depth=oldDepth;
        }
        void OnDestroy(){planetUi.Dispose();}
        void LabelBody(Vector3 position,string label)
        {
            Vector3 point=SkyCamera.WorldToScreenPoint(position);if(point.z<=0||point.x<0||point.x>Screen.width||point.y<0||point.y>Screen.height)return;
            float y=Screen.height-point.y;GUI.Label(new Rect(point.x-4,y-10,14,20),"●");GUI.Label(new Rect(point.x+7,y-9,130,24),label);
        }
        void Update()
        {
            if(SettingsMenu.PausesSimulation)return;
            AdvanceOrbits(Time.deltaTime);
        }
        internal void AdvanceOrbits(float seconds)
        {
            for(int i=0;i<9;i++){angles[i]+=seconds*(360f/(365.25f*86400f))*Mathf.Pow(AstronomicalUnit/distances[i],1.5f);PositionOrbit(i);}
        }
        void PositionOrbit(int i)
        {
            float angle=angles[i]*Mathf.Deg2Rad;
            Vector3 local=new Vector3(Mathf.Cos(angle)*distances[i],0,Mathf.Sin(angle)*distances[i]);
            orbits[i].position=SunPosition+Quaternion.Euler(inclinations[i],0,0)*local;
        }
        void BuildHomeworld(Transform parent)
        {
            FracturedBody("Planet 4",parent,Vector3.zero,HomeRadius,false);
            HomeMoon=new GameObject("Half destroyed moon").transform;HomeMoon.SetParent(parent);HomeMoon.localPosition=new Vector3(-.8f,.5f,-.1f).normalized*MoonDistance;
            FracturedBody("Moon",HomeMoon,Vector3.zero,1.1f,true);
            var random=new System.Random(7301);
            for(int i=0;i<38;i++)
            {
                float a=i*2.399f;Vector3 offset=new Vector3(Mathf.Cos(a)*HomeRadius*1.35f,Mathf.Sin(a)*HomeRadius*.95f,((float)random.NextDouble()-.5f)*HomeRadius);
                var debris=Sphere("Planetary debris "+i,parent.position+offset,HomeRadius*(.025f+(float)random.NextDouble()*.075f),crust);debris.transform.SetParent(parent,true);
                var filter=debris.GetComponent<MeshFilter>();var mesh=Instantiate(filter.sharedMesh);var v=mesh.vertices;
                for(int k=0;k<v.Length;k++)v[k]*=.65f+Mathf.PerlinNoise(v[k].x*13+4,v[k].y*13+3)*.7f;
                mesh.vertices=v;mesh.RecalculateNormals();mesh.RecalculateBounds();filter.sharedMesh=mesh;
            }
            var fog=Cloud("Toxic hot ejecta",parent.position,HomeRadius*1.12f,new Color(.54f,.6f,.13f,.11f),HomeRadius*.22f,HomeRadius*.38f,35);fog.transform.SetParent(parent,true);
            var fogMain=fog.main;fogMain.simulationSpace=ParticleSystemSimulationSpace.Local;
            var stream=Cloud("Moon material flowing into planet",HomeMoon.position,.25f,new Color(1,.48f,.11f,.7f),.06f,.12f,55);stream.transform.SetParent(parent,true);
            stream.transform.rotation=Quaternion.LookRotation(parent.position-HomeMoon.position);
            var main=stream.main;main.startLifetime=9;main.startSpeed=Vector3.Distance(HomeMoon.position,parent.position)/9;main.simulationSpace=ParticleSystemSimulationSpace.Local;
            var shape=stream.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=3;shape.radius=.25f;
        }
        GameObject Sphere(string name,Vector3 position,float radius,Material material)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(transform);g.transform.position=position;g.transform.localScale=Vector3.one*radius*2;Destroy(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=material;return g;
        }
        void FracturedBody(string name,Transform parent,Vector3 center,float radius,bool moon)
        {
            var core=Sphere(name+" buried molten interior",parent.position+center,radius*.79f,lava);core.transform.SetParent(parent,true);
            if(moon){var filter=core.GetComponent<MeshFilter>();var cut=Instantiate(filter.sharedMesh);var points=cut.vertices;var old=cut.triangles;var keep=new System.Collections.Generic.List<int>();for(int i=0;i<old.Length;i+=3){Vector3 mid=(points[old[i]]+points[old[i+1]]+points[old[i+2]])/3;if(mid.x>.06f&&mid.z<.05f)continue;keep.Add(old[i]);keep.Add(old[i+1]);keep.Add(old[i+2]);}cut.triangles=keep.ToArray();filter.sharedMesh=cut;}
            const int count=23, longitude=96,latitude=48;
            var seeds=new Vector3[count];for(int i=0;i<count;i++){float y=1-2*(i+.5f)/count;float a=i*2.399963f;float r=Mathf.Sqrt(1-y*y);seeds[i]=new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r);}
            int outer=(longitude+1)*(latitude+1);var directions=new Vector3[outer];var pointsOuter=new Vector3[outer];var uv=new Vector2[outer*2];
            for(int y=0;y<=latitude;y++)for(int x=0;x<=longitude;x++)
            {
                float u=x/(float)longitude,v=y/(float)latitude,theta=u*Mathf.PI*2,phi=v*Mathf.PI;int n=y*(longitude+1)+x;
                Vector3 d=new Vector3(Mathf.Sin(phi)*Mathf.Cos(theta),Mathf.Cos(phi),Mathf.Sin(phi)*Mathf.Sin(theta));directions[n]=d;
                float rock=Mathf.PerlinNoise(d.x*11+14,d.y*11+d.z*4+11),fine=Mathf.PerlinNoise(d.z*37+8,d.x*37+10);
                pointsOuter[n]=d*radius*(.91f+rock*.20f+fine*.07f);uv[n]=uv[n+outer]=new Vector2(u,v);
            }
            var plates=new System.Collections.Generic.List<int>[count];for(int i=0;i<count;i++)plates[i]=new System.Collections.Generic.List<int>();
            for(int y=0;y<latitude;y++)for(int x=0;x<longitude;x++)
            {
                int n=y*(longitude+1)+x;int[] cell={n,n+1,n+longitude+1,n+1,n+longitude+2,n+longitude+1};
                for(int t=0;t<6;t+=3){int a=cell[t],b=cell[t+1],c=cell[t+2];Vector3 d=(directions[a]+directions[b]+directions[c]).normalized;float best=-2,second=-2;int plate=0;
                    for(int i=0;i<count;i++){float dot=Vector3.Dot(d,seeds[i]);if(dot>best){second=best;best=dot;plate=i;}else if(dot>second)second=dot;}
                    if(best-second<.012f || (moon&&seeds[plate].x>.15f&&seeds[plate].z<.1f) || (!moon&&plate==13))continue;
                    plates[plate].Add(a);plates[plate].Add(b);plates[plate].Add(c);
                }
            }
            for(int i=0;i<count;i++)
            {
                var triangles=plates[i];if(triangles.Count==0)continue;var vertices=new Vector3[outer*2];Vector3 shift=seeds[i]*radius*.035f+Vector3.right*(seeds[i].x>0?.07f:-.07f)*radius;
                for(int k=0;k<outer;k++){vertices[k]=center+pointsOuter[k]+shift;vertices[k+outer]=center+pointsOuter[k]*.86f+shift;}
                var edges=new System.Collections.Generic.Dictionary<long,int[]>();int faces=triangles.Count;
                for(int t=0;t<faces;t+=3){int a=triangles[t],b=triangles[t+1],c=triangles[t+2];Boundary(edges,a,b);Boundary(edges,b,c);Boundary(edges,c,a);triangles.Add(a+outer);triangles.Add(c+outer);triangles.Add(b+outer);}
                foreach(var edge in edges.Values)if(edge[2]==1)Wall(triangles,edge[0],edge[1],outer);
                var mesh=new Mesh();mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
                var slab=new GameObject(name+" continental fragment "+i,typeof(MeshFilter),typeof(MeshRenderer));slab.transform.SetParent(parent,false);slab.GetComponent<MeshFilter>().sharedMesh=mesh;slab.GetComponent<MeshRenderer>().sharedMaterial=crust;
            }
        }
        static void Boundary(System.Collections.Generic.Dictionary<long,int[]> edges,int a,int b)
        {
            long key=((long)Mathf.Min(a,b)<<32)|(uint)Mathf.Max(a,b);
            if(edges.TryGetValue(key,out var edge))edge[2]++;else edges[key]=new[]{a,b,1};
        }
        static void Wall(System.Collections.Generic.List<int> triangles,int a,int b,int inner)
        {triangles.Add(a);triangles.Add(a+inner);triangles.Add(b);triangles.Add(b);triangles.Add(a+inner);triangles.Add(b+inner);}
        void Ring(Transform parent,float radius,Color color,float tilt)
        {
            const int segments=160;var vertices=new Vector3[(segments+1)*2];var uv=new Vector2[vertices.Length];var triangles=new int[segments*6];
            for(int i=0;i<=segments;i++){float a=i/(float)segments*Mathf.PI*2;for(int j=0;j<2;j++){int k=i*2+j;vertices[k]=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius*(j==0?1.45f:2.35f);uv[k]=new Vector2(i/(float)segments,j);}if(i<segments){int k=i*2,t=i*6;int[] face={k,k+2,k+1,k+1,k+2,k+3};for(int j=0;j<6;j++)triangles[t+j]=face[j];}}
            var mesh=new Mesh();mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();var ring=new GameObject("Planet rings",typeof(MeshFilter),typeof(MeshRenderer));ring.transform.SetParent(parent,false);ring.transform.localRotation=Quaternion.Euler(tilt,0,15);ring.GetComponent<MeshFilter>().sharedMesh=mesh;ring.GetComponent<MeshRenderer>().sharedMaterial=new Material(Shader.Find("SpaceMiner/PlanetRing")){color=color};
        }
        ParticleSystem Cloud(string name,Vector3 pos,float radius,Color color,float minSize,float maxSize,float rate)
        {
            var g=new GameObject(name,typeof(ParticleSystem));g.transform.SetParent(transform);g.transform.position=pos;var ps=g.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var m=ps.main;m.startLifetime=12;m.startSpeed=radius*.0005f;m.startSize=new ParticleSystem.MinMaxCurve(minSize,maxSize);m.startColor=color;m.maxParticles=1000;m.simulationSpace=ParticleSystemSimulationSpace.World;var emission=ps.emission;emission.rateOverTime=rate;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=radius;var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=new Material(Shader.Find("SpaceMiner/HazardParticle"));
            var tex=new Texture2D(32,32,TextureFormat.RGBA32,false);for(int y=0;y<32;y++)for(int x=0;x<32;x++){float d=Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f))/15.5f;tex.SetPixel(x,y,new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-d),2)));}tex.Apply();renderer.sharedMaterial.mainTexture=tex;renderer.sharedMaterial.SetFloat("_Mode",2);renderer.sharedMaterial.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);renderer.sharedMaterial.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.One);renderer.sharedMaterial.SetInt("_ZWrite",0);renderer.sharedMaterial.EnableKeyword("_ALPHABLEND_ON");ps.Play();return ps;
        }
    }
}

