using System;
using System.Collections.Generic;
using UnityEngine;
using Object=UnityEngine.Object;

namespace SpaceMiner
{
    // Cutaway planning model. No colliders, campaign objects or campaign lights are modified.
    internal sealed class RingConstructionView : IDisposable
    {
        private const int Layer=27;
        private readonly GameObject root;
        private readonly Transform geometry,ghost,worker,carrier;
        private readonly Camera camera;
        private readonly List<Material> materials=new List<Material>();
        private readonly List<Mesh> meshes=new List<Mesh>();
        private readonly Material hull,walkway,storage,production,glass,planned,white,amber,green,red;
        private RenderTexture texture;
        private int revision=-1,hoverRing=-1,hoverSector=-1;
        private bool valid;
        private RingConstructionModel.Tool hoverTool;
        private float yaw=-32,pitch=30,zoom=1;
        private int activeRing;
        private bool isolate;
        private readonly List<Transform> levels=new List<Transform>();
        public Texture Texture=>texture;
        public void SetActive(bool active){if(root!=null)root.SetActive(active);}
        public RingConstructionView()
        {
            root=new GameObject("Ringbau: isoliertes Planungsmodell");root.transform.position=new Vector3(0,-20000,0);
            geometry=Child("Station",root.transform);ghost=Child("Bauvorschau",root.transform);
            hull=Surface(new Color(.17f,.23f,.29f),.65f);walkway=Surface(new Color(.30f,.40f,.45f),.5f);
            storage=Surface(new Color(.13f,.55f,.72f),.65f);production=Surface(new Color(.72f,.39f,.12f),.5f);
            glass=Surface(new Color(.05f,.18f,.23f),.8f);planned=Surface(new Color(.09f,.20f,.26f),.1f);
            white=Surface(new Color(.65f,.91f,1),.4f,true);amber=Surface(SpaceMinerUi.Amber,.2f,true);
            green=Surface(SpaceMinerUi.Success,0,true);red=Surface(SpaceMinerUi.Error,0,true);
            var cameraObject=new GameObject("Planungskamera");cameraObject.transform.SetParent(root.transform,false);
            camera=cameraObject.AddComponent<Camera>();camera.cullingMask=1<<Layer;camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.012f,.029f,.048f);camera.orthographic=true;camera.nearClipPlane=.1f;camera.farClipPlane=300;
            camera.allowHDR=false;camera.allowMSAA=true;camera.depth=-30;
            var lightObject=new GameObject("Modellbeleuchtung");lightObject.transform.SetParent(root.transform,false);
            var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.color=new Color(.78f,.88f,1);light.cullingMask=1<<Layer;
            lightObject.transform.localRotation=Quaternion.Euler(48,-38,0);
            worker=Drone("Baudrohne",amber);carrier=Drone("Transportdrohne",white);
        }
        private static Transform Child(string name,Transform parent){var g=new GameObject(name);g.layer=Layer;g.transform.SetParent(parent,false);return g.transform;}
        private Material Surface(Color color,float metal,bool glow=false)
        {
            var m=new Material(Shader.Find("Standard")){color=color};m.SetFloat("_Metallic",metal);m.SetFloat("_Glossiness",.35f);
            if(glow){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.7f);}materials.Add(m);return m;
        }
        private Transform Part(string name,Transform parent,PrimitiveType type,Vector3 pos,Vector3 size,Material material,float rotation=0)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.layer=Layer;go.transform.SetParent(parent,false);
            go.transform.localPosition=pos;go.transform.localScale=size;go.transform.localRotation=Quaternion.Euler(0,rotation,0);
            go.GetComponent<Renderer>().sharedMaterial=material;Object.Destroy(go.GetComponent<Collider>());return go.transform;
        }
        private Transform Drone(string name,Material color)
        {
            var t=Child(name,root.transform);Part("Gehäuse",t,PrimitiveType.Cube,Vector3.zero,new Vector3(.8f,.4f,.8f),hull);
            Part("Signal",t,PrimitiveType.Cube,Vector3.up*.28f,new Vector3(.5f,.12f,.5f),color);
            Part("Transportgut",t,PrimitiveType.Cube,Vector3.down*.45f,Vector3.one*.45f,storage);return t;
        }
        public void SetViewport(int width,int height)
        {
            width=Mathf.Clamp(width,128,2400);height=Mathf.Clamp(height,128,1600);
            if(texture!=null&&texture.width==width&&texture.height==height)return;
            if(texture!=null){camera.targetTexture=null;texture.Release();Object.Destroy(texture);}
            texture=new RenderTexture(width,height,24){antiAliasing=2,name="Ringbau-Modell"};texture.Create();camera.targetTexture=texture;
        }
        public void Orbit(Vector2 delta){yaw+=delta.x*.3f;pitch=Mathf.Clamp(pitch-delta.y*.2f,20,87);}
        public void Zoom(float delta){zoom=Mathf.Clamp(zoom+delta*.08f,.55f,1.8f);}
        public void Home(bool top=false){yaw=top?0:-32;pitch=top?87:30;zoom=1;}
        public void SelectLevel(int ring,bool onlyLevel){activeRing=ring;isolate=onlyLevel;revision=-1;hoverRing=-1;}
        internal bool CheckPicking(RingConstructionModel model)
        {
            foreach(var tool in new[]{RingConstructionModel.Tool.Storage,RingConstructionModel.Tool.Separator}){
                var p=camera.WorldToViewportPoint(root.transform.position+model.Position(activeRing,3,tool==RingConstructionModel.Tool.Storage?3.8f:-3.8f));
                if(!Pick(p,model,tool,out int r,out int s,out _)||r!=activeRing||s!=3)return false;
            }
            return true;
        }
        public bool Pick(Vector2 normalized,RingConstructionModel model,RingConstructionModel.Tool tool,out int ring,out int sector,out bool outside)
        {
            ring=sector=-1;outside=false;
            var ray=camera.ViewportPointToRay(new Vector3(normalized.x,normalized.y,0));
            activeRing=Mathf.Clamp(activeRing,0,model.Rings.Count-1);
            if(!new Plane(Vector3.up,root.transform.position+Vector3.up*model.Rings[activeRing].Height).Raycast(ray,out float distance))return false;
            var point=ray.GetPoint(distance)-root.transform.position;float radius=new Vector2(point.x,point.z).magnitude,best=4;
            for(int r=0;r<model.Rings.Count;r++){
                if(r!=activeRing)continue;
                float d;
                if(tool==RingConstructionModel.Tool.Inspect){d=Mathf.Min(Mathf.Abs(radius-model.Rings[r].Radius-3.8f),Mathf.Abs(radius-model.Rings[r].Radius+3.8f));}
                else d=Mathf.Abs(radius-model.Rings[r].Radius-(tool==RingConstructionModel.Tool.Storage?3.8f:-3.8f));
                if(d<best){best=d;ring=r;}
            }
            if(ring<0)return false;
            float angle=Mathf.Repeat(Mathf.Atan2(point.x,point.z),Mathf.PI*2);
            sector=Mathf.RoundToInt(angle/(Mathf.PI*2)*model.Rings[ring].Segments.Length)%model.Rings[ring].Segments.Length;
            outside=radius>model.Rings[ring].Radius;return true;
        }
        public void Hover(RingConstructionModel model,int ring,int sector,RingConstructionModel.Tool tool,bool allowed)
        {
            if(hoverRing==ring&&hoverSector==sector&&hoverTool==tool&&valid==allowed)return;
            hoverRing=ring;hoverSector=sector;hoverTool=tool;valid=allowed;
            foreach(Transform child in ghost)Object.Destroy(child.gameObject);
            if(ring<0||tool==RingConstructionModel.Tool.Inspect)return;
            var p=model.Position(ring,sector,tool==RingConstructionModel.Tool.Storage?3.8f:-3.8f);
            Part("Baufläche",ghost,PrimitiveType.Cube,p+Vector3.up*.08f,new Vector3(3.5f,.14f,3.5f),allowed?green:red,sector*360f/model.Rings[ring].Segments.Length);
        }
        public void Update(RingConstructionModel model,float time)
        {
            if(revision!=model.Revision){Rebuild(model);revision=model.Revision;}
            for(int r=0;r<levels.Count;r++)levels[r].gameObject.SetActive(!isolate||r==activeRing);
            float extent=model.Rings[model.Rings.Count-1].Radius+8;
            camera.orthographicSize=extent*zoom*Mathf.Max(.76f,1/camera.aspect);
            float targetHeight=isolate?model.Rings[activeRing].Height:model.Rings[model.Rings.Count-1].Height*.5f;
            var rot=Quaternion.Euler(pitch,yaw,0);camera.transform.localRotation=rot;camera.transform.localPosition=Vector3.up*targetHeight+rot*(Vector3.back*130);
            var job=model.ActiveJob;worker.gameObject.SetActive(job!=null&&(!isolate||job.Ring==activeRing));
            if(job!=null){
                var destination=model.Position(job.Ring,job.Sector,job.Module==null?0:job.Module.Kind==RingConstructionModel.Tool.Storage?3.8f:-3.8f);
                float travel=Mathf.Clamp01(job.Progress*3);worker.localPosition=Vector3.Lerp(Vector3.zero,destination,travel)+Vector3.up*(2+Mathf.Sin(travel*Mathf.PI)*4);
            }
            carrier.gameObject.SetActive(model.BuiltCount(RingConstructionModel.Tool.Separator)>0&&model.BuiltCount(RingConstructionModel.Tool.Storage)>0&&(!isolate||activeRing==0));
            // Schematic transport lane, distinct from the future physical traffic controller.
            float a=time*.28f,radius=model.Rings[0].Radius;carrier.localPosition=new Vector3(Mathf.Sin(a)*radius,1.3f,Mathf.Cos(a)*radius);
        }
        private void Rebuild(RingConstructionModel model)
        {
            foreach(Transform child in geometry)Object.Destroy(child.gameObject);
            levels.Clear();
            foreach(var mesh in meshes)Object.Destroy(mesh);meshes.Clear();
            Part("Zentrale Leitstelle",geometry,PrimitiveType.Cylinder,new Vector3(0,.8f,0),new Vector3(7,1.1f,7),hull);
            Part("Planungsdisplay",geometry,PrimitiveType.Cylinder,new Vector3(0,2,0),new Vector3(4,.14f,4),white);
            Part("Kernversorgung",geometry,PrimitiveType.Cylinder,new Vector3(0,2.6f,0),new Vector3(1.8f,.5f,1.8f),glass);
            for(int r=0;r<model.Rings.Count;r++){
                var ring=model.Rings[r];int count=ring.Segments.Length;
                var level=Child("Ebene "+(r+1),geometry);levels.Add(level);
                if(r>0)Part("Zentrale Plattform",level,PrimitiveType.Cylinder,new Vector3(0,ring.Height,0),new Vector3(7,.2f,7),hull);
                for(int s=0;s<count;s++){
                    Sector("Ring "+(r+1)+" / Segment "+(s+1),level,ring.Radius,ring.Height,s,count,ring.Segments[s]?(r==activeRing?walkway:hull):planned);
                    if(!ring.Segments[s])continue;
                    float angle=s*360f/count;
                    Part("Markierung",level,PrimitiveType.Cube,model.Position(r,s)+Vector3.up*.08f,new Vector3(.13f,.07f,1.7f),model.Reserved(r,s)?amber:white,angle);
                    if(model.Reserved(r,s))continue;
                    foreach(bool outside in new[]{false,true}){
                        if(model.At(r,s,outside)!=null)continue;
                        Part("Freier Bauplatz",level,PrimitiveType.Cube,model.Position(r,s,outside?3.8f:-3.8f)-Vector3.up*.18f,new Vector3(3.1f,.12f,3.1f),planned,angle);
                    }
                }
                for(int bridge=0;bridge<4;bridge++){
                    int s=bridge*count/4;float inner=3.5f;
                    float length=ring.Radius-inner,mid=(ring.Radius+inner)/2;
                    var center=model.Position(r,s);center.y=0;center=center*(mid/ring.Radius)+Vector3.up*ring.Height;
                    Part("Reservierter Übergang",level,PrimitiveType.Cube,center-Vector3.up*.13f,new Vector3(2.1f,.25f,length),ring.Segments[s]?hull:planned,bridge*90);
                    if(r>0){
                        var shaft=model.Position(r,s)-Vector3.up*ring.Height*.5f;
                        Part("Vertikale Verbindung",level,PrimitiveType.Cube,shaft,new Vector3(1.5f,ring.Height,1.5f),ring.Segments[s]?hull:planned,bridge*90);
                        Part("Verbindungslicht",level,PrimitiveType.Cube,shaft+new Vector3(.8f,0,0),new Vector3(.08f,ring.Height,.12f),white);
                    }
                }
            }
            foreach(var module in model.Modules)Module(model,module);
            Part("Drohnen-Dock",geometry,PrimitiveType.Cube,new Vector3(5,.3f,2),new Vector3(2.4f,.5f,3.4f),hull);
            Part("Dock-Beleuchtung",geometry,PrimitiveType.Cube,new Vector3(5,.6f,2),new Vector3(1.7f,.08f,2.6f),white);
        }
        private void Module(RingConstructionModel model,RingConstructionModel.Module module)
        {
            var t=Child(RingConstructionModel.Name(module.Kind),levels[module.Ring]);t.localPosition=model.Position(module.Ring,module.Sector,module.Kind==RingConstructionModel.Tool.Storage?3.8f:-3.8f);
            t.localRotation=Quaternion.Euler(0,module.Sector*360f/model.Rings[module.Ring].Segments.Length,0);
            var color=module.Built?(module.Kind==RingConstructionModel.Tool.Storage?storage:production):planned;
            Part("Fundament",t,PrimitiveType.Cube,new Vector3(0,-.1f,0),new Vector3(3.3f,.25f,3.4f),hull);
            if(module.Kind==RingConstructionModel.Tool.Storage){
                Part("Container",t,PrimitiveType.Cube,new Vector3(0,.9f,0),new Vector3(2.8f,1.7f,3),color);
                for(int i=-1;i<=1;i++)Part("Verstärkung",t,PrimitiveType.Cube,new Vector3(i*1.1f,1,0),new Vector3(.12f,2,3.15f),hull);
                Part("Lagerstatus",t,PrimitiveType.Cube,new Vector3(0,1,1.55f),new Vector3(.7f,.25f,.08f),module.Built?white:amber);
                var labelObject=new GameObject("Produktbeschriftung");labelObject.layer=Layer;labelObject.transform.SetParent(t,false);
                labelObject.transform.localPosition=new Vector3(0,1.8f,1.56f);labelObject.transform.localRotation=Quaternion.Euler(0,180,0);
                var label=labelObject.AddComponent<TextMesh>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");labelObject.GetComponent<MeshRenderer>().sharedMaterial=label.font.material;
                label.text=RingConstructionModel.ProductName(module.StoredProduct);label.characterSize=.18f;label.fontSize=48;label.anchor=TextAnchor.MiddleCenter;label.color=Color.white;
            }else{
                Part("Maschinenbett",t,PrimitiveType.Cube,new Vector3(0,.4f,0),new Vector3(2.9f,.7f,2.8f),color);
                if(module.Kind==RingConstructionModel.Tool.Separator){
                    for(int i=-1;i<=1;i++)Part("Trennsäule",t,PrimitiveType.Cylinder,new Vector3(i*.83f,1.6f,0),new Vector3(.65f,1,.65f),color);
                }else{
                    Part("Fertigungsportal",t,PrimitiveType.Cube,new Vector3(0,2,0),new Vector3(2.7f,.35f,1),color);
                    for(int i=-1;i<=1;i+=2)Part("Stütze",t,PrimitiveType.Cube,new Vector3(i*1.15f,1.2f,0),new Vector3(.25f,1.6f,.6f),hull);
                    Part("Werkstück",t,PrimitiveType.Cube,new Vector3(0,1,0),Vector3.one*.7f,white);
                }
                Part("Bedienpanel",t,PrimitiveType.Cube,new Vector3(0,1.1f,-1.3f),new Vector3(1,.5f,.12f),module.Built?white:amber);
            }
        }
        private void Sector(string name,Transform parent,float radius,float height,int sector,int count,Material material)
        {
            // Solid curved floor; roofs are intentionally omitted for the planning cutaway.
            const int steps=5;var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int j=0;j<steps;j++){
                float a=(sector-.49f+j*.98f/steps)*Mathf.PI*2/count,b=(sector-.49f+(j+1)*.98f/steps)*Mathf.PI*2/count;
                Vector3 pa=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a)),pb=new Vector3(Mathf.Sin(b),0,Mathf.Cos(b));
                var q=new[]{pa*(radius-1.1f),pb*(radius-1.1f),pb*(radius+1.1f),pa*(radius+1.1f)};
                Quad(q[0],q[1],q[2],q[3]);Quad(q[3]-Vector3.up*.35f,q[2]-Vector3.up*.35f,q[1]-Vector3.up*.35f,q[0]-Vector3.up*.35f);
                Quad(q[3],q[2],q[2]-Vector3.up*.35f,q[3]-Vector3.up*.35f);Quad(q[1],q[0],q[0]-Vector3.up*.35f,q[1]-Vector3.up*.35f);
            }
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d){int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});triangles.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});}
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();meshes.Add(mesh);
            var t=Child(name,parent);t.localPosition=Vector3.up*height;t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        public void Dispose()
        {
            if(camera!=null)camera.targetTexture=null;if(texture!=null){texture.Release();Object.Destroy(texture);}
            Object.Destroy(root);foreach(var m in materials)Object.Destroy(m);foreach(var m in meshes)Object.Destroy(m);
        }
    }
}
