using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering;

namespace SpaceMiner.Editor
{
    public static class HallExperimentBuild
    {
        private const string Art="Assets/SpaceMiner/Art/HallExperiment";
        private const string Scene="Assets/SpaceMiner/HallLab/HallExperiment.unity";
        private static readonly Dictionary<string,Material> surfaces=new Dictionary<string,Material>();
        public static void BuildStation()
        {
            PrepareStationPrefab();
            StartMenuBuild.Run();
            Debug.Log("DETAILED HALL STATION BUILD PASSED");
        }
        public static void PrepareStationPrefab()
        {
            Prepare();
            var app=UnityEngine.Object.FindFirstObjectByType<HallExperimentView>();
            var module=new GameObject("Drohnenhalle / ausgearbeitet");
            app.HallRoot.SetParent(module.transform,true);
            // The standalone test's closed connector and short apron must not cap the real ring/dock.
            foreach(var t in app.HallRoot.GetComponentsInChildren<Transform>(true).ToArray())
                if(t.name.StartsWith("Connector",StringComparison.Ordinal) || t.name.StartsWith("COL_Connector",StringComparison.Ordinal)
                    || t.name.StartsWith("Apron",StringComparison.Ordinal) || t.name.StartsWith("COL_Apron",StringComparison.Ordinal))
                    UnityEngine.Object.DestroyImmediate(t.gameObject);
            foreach(var t in app.HallRoot.GetComponentsInChildren<Transform>(true))
                if(t.name.StartsWith("COL_ServiceCabinet",StringComparison.Ordinal))
                {
                    // Solid volume also detects a saved player capsule entirely inside the cabinet.
                    var mesh=t.GetComponent<MeshCollider>();var bounds=mesh.sharedMesh.bounds;
                    UnityEngine.Object.DestroyImmediate(mesh);
                    var box=t.gameObject.AddComponent<BoxCollider>();box.center=bounds.center;box.size=bounds.size;
                }
            app.ScaleDrone.SetParent(module.transform,true);
            foreach(var light in app.WorkLights){light.transform.SetParent(module.transform,true);light.cullingMask=1<<25;}
            app.Reflection.transform.SetParent(module.transform,true);app.Reflection.cullingMask=1<<25;
            var controller=module.AddComponent<StationDroneHall>();
            controller.Portals=app.DoorFrames;controller.LeftLeaves=app.DoorLeft;controller.RightLeaves=app.DoorRight;
            foreach(var t in module.GetComponentsInChildren<Transform>(true))t.gameObject.layer=25;
            Directory.CreateDirectory("Assets/SpaceMiner/Resources/Station");
            PrefabUtility.SaveAsPrefabAsset(module,"Assets/SpaceMiner/Resources/Station/DetailedDroneHall.prefab");
            AssetDatabase.SaveAssets();
            Debug.Log("DETAILED HALL PREFAB PREPARED");
        }
        public static void Build()
        {
            Prepare();
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName="Builds/HallExperiment/HallExperiment.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Hall experiment build failed");
            Debug.Log("HALL EXPERIMENT BUILD PASSED");
        }
        public static void Prepare()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            Directory.CreateDirectory(Art+"/Materials");Directory.CreateDirectory("Assets/SpaceMiner/HallLab");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Maps();
            AssetDatabase.ImportAsset(Art+"/Textures/PaintedAlloy_Albedo.png",ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            if(AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/PaintedAlloy_Albedo.png")==null)throw new Exception("Albedo texture import failed");
            Materials();
            var importer=(ModelImporter)AssetImporter.GetAtPath(Art+"/StationHall-v01.fbx");
            if(importer==null)throw new Exception("Generate StationHall-v01.fbx with CreateHallExperiment.py first");
            importer.globalScale=1;importer.isReadable=true;importer.importAnimation=false;importer.addCollider=false;
            importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
            var hall=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/StationHall-v01.fbx"));hall.name="Station / experimenteller Hallenausschnitt";
            Transform[] children=hall.GetComponentsInChildren<Transform>(true);
            Transform Find(string name)=>children.First(t=>t.name==name);
            if(Find("Door_02").position.z<0)hall.transform.rotation=Quaternion.Euler(0,180,0);
            int colliderCount=0;
            foreach(var renderer in hall.GetComponentsInChildren<MeshRenderer>(true))
            {
                renderer.gameObject.SetActive(true);
                if(renderer.name.StartsWith("COL_",StringComparison.Ordinal))
                {
                    renderer.enabled=false;var collider=renderer.gameObject.AddComponent<MeshCollider>();
                    collider.sharedMesh=renderer.GetComponent<MeshFilter>().sharedMesh;colliderCount++;continue;
                }
                string[] split=renderer.name.Split(new[]{"__"},StringSplitOptions.None);
                if(split.Length<2 || !surfaces.TryGetValue(split[1],out var material))throw new Exception("Unknown hall surface: "+renderer.name);
                renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();
                renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
                renderer.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;
                if(renderer.name.StartsWith("DoorPanel__",StringComparison.Ordinal))
                {
                    var collider=renderer.gameObject.AddComponent<BoxCollider>();var bounds=renderer.GetComponent<MeshFilter>().sharedMesh.bounds;
                    collider.center=bounds.center;collider.size=bounds.size;colliderCount++;
                }
            }
            var app=new GameObject("Hallenexperiment").AddComponent<HallExperimentView>();app.HallRoot=hall.transform;
            var starMaterial=new Material(Shader.Find("Unlit/Color")){name="Sternhintergrund",color=new Color(.65f,.76f,.87f)};
            Save(starMaterial,Art+"/Materials/Stars.mat");app.StarMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/Stars.mat");
            app.DoorFrames=Enumerable.Range(1,3).Select(i=>Find("Door_"+i.ToString("00"))).ToArray();
            app.DoorLeft=Enumerable.Range(1,3).Select(i=>Find("Door_"+i.ToString("00")+"_Left")).ToArray();
            app.DoorRight=Enumerable.Range(1,3).Select(i=>Find("Door_"+i.ToString("00")+"_Right")).ToArray();
            var camera=new GameObject("Hallen-Kamera").AddComponent<Camera>();camera.gameObject.AddComponent<AudioListener>();
            camera.nearClipPlane=.06f;camera.farClipPlane=150;camera.fieldOfView=72;camera.allowHDR=true;camera.allowMSAA=true;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.008f,.014f,.025f);app.ViewCamera=camera;
            var player=new GameObject("Versuch / Besucher").AddComponent<CharacterController>();
            player.height=1.8f;player.radius=.3f;player.center=Vector3.up*.9f;player.stepOffset=.18f;player.skinWidth=.025f;player.minMoveDistance=0;app.Walker=player;
            app.gameObject.AddComponent<DisplayModeController>();
            app.gameObject.AddComponent<HallExperimentCheck>();
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.20f,.25f,.30f);
            RenderSettings.ambientEquatorColor=new Color(.11f,.15f,.18f);RenderSettings.ambientGroundColor=new Color(.07f,.08f,.09f);
            RenderSettings.reflectionIntensity=.8f;RenderSettings.fog=false;
            QualitySettings.pixelLightCount=12;QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.High;QualitySettings.shadowDistance=38;QualitySettings.antiAliasing=4;
            var lights=new List<Light>();
            foreach(float depth in new[]{2.5f,7,11.5f,15f})foreach(float x in new[]{-5f,5f})
            {
                var light=new GameObject("Arbeitslicht").AddComponent<Light>();light.type=LightType.Point;light.transform.position=new Vector3(x,4.9f,depth);
                light.color=new Color(1,.86f,.68f);light.range=10;light.intensity=2.0f;
                light.shadows=depth==7||depth==15?LightShadows.Soft:LightShadows.None;light.shadowStrength=.75f;light.shadowBias=.035f;light.shadowNormalBias=.25f;lights.Add(light);
            }
            var outside=new GameObject("Dock / kaltes Streiflicht").AddComponent<Light>();outside.type=LightType.Directional;
            outside.color=new Color(.50f,.69f,1);outside.intensity=.7f;outside.transform.rotation=Quaternion.Euler(33,165,0);outside.shadows=LightShadows.Soft;
            app.WorkLights=lights.ToArray();app.DockLight=outside;
            // Reuse the already imported drone as a stationary scale reference.
            var prefab=Resources.Load<GameObject>("Tripo/MaintenanceDrone");
            if(prefab==null)throw new Exception("Existing maintenance drone prefab missing");
            var stand=new GameObject("Massstabsreferenz / Wartungsdrohne");
            var drone=UnityEngine.Object.Instantiate(prefab,stand.transform);var droneRenderers=drone.GetComponentsInChildren<Renderer>();
            Bounds droneBounds=droneRenderers[0].bounds;foreach(var r in droneRenderers.Skip(1))droneBounds.Encapsulate(r.bounds);
            float scale=2.5f/Mathf.Max(droneBounds.size.x,droneBounds.size.z);drone.transform.localScale*=scale;
            droneBounds=droneRenderers[0].bounds;foreach(var r in droneRenderers.Skip(1))droneBounds.Encapsulate(r.bounds);
            drone.transform.position+=new Vector3(-4,.95f,10.1f)-new Vector3(droneBounds.center.x,droneBounds.min.y,droneBounds.center.z);
            app.ScaleDrone=stand.transform;
            var probe=new GameObject("Hallenreflexion").AddComponent<ReflectionProbe>();probe.transform.position=new Vector3(0,2.7f,8);
            probe.size=new Vector3(29,8,26);probe.boxProjection=true;probe.resolution=128;probe.hdr=true;probe.intensity=1;
            probe.clearFlags=ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=camera.backgroundColor;probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.OnAwake;
            app.Reflection=probe;
            var renders=hall.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();app.HallSurfaces=renders;
            app.DetailedMaterials=renders.Select(r=>r.sharedMaterial).ToArray();
            var plain=new Material(Shader.Find("Standard")){name="Vergleich / schlichter Blockout",color=new Color(.25f,.34f,.4f)};
            Save(plain,Art+"/Materials/Blockout.mat");app.PlainMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/Blockout.mat");
            // Resettable scene assets; no campaign scene or game state is involved.
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),Scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/HallExperiment");
            File.WriteAllText("Logs/HallExperiment/import.json","{\"colliders\":"+colliderCount+",\"renderers\":"+renders.Length+",\"doors\":3,\"metres\":true}");
            Debug.Log("HALL IMPORT PREPARED: renderers="+renders.Length+" colliders="+colliderCount+" door2="+Find("Door_02").position);
        }
        private static void Maps()
        {
            const int n=512;var normal=new Texture2D(n,n,TextureFormat.RGB24,false,true);var paint=new Texture2D(n,n,TextureFormat.RGBA32,false,true);var steel=new Texture2D(n,n,TextureFormat.RGBA32,false,true);
            var a=new Color32[n*n];var b=new Color32[n*n];var c=new Color32[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                int i=y*n+x;float noise=Mathf.PerlinNoise(x*.04f,y*.04f),grain=Mathf.Sin(x*1.67f+y*.31f)*.035f;
                a[i]=new Color(.5f+grain,.5f+Mathf.Sin(y*1.73f)*.025f,1);
                b[i]=new Color(.16f,.16f,.16f,.29f+.10f*noise);
                c[i]=new Color(.82f,.82f,.82f,.48f+.16f*noise);
            }
            normal.SetPixels32(a);normal.Apply();paint.SetPixels32(b);paint.Apply();steel.SetPixels32(c);steel.Apply();
            Write("MicroNormal.png",normal);Write("Paint_MetalSmooth.png",paint);Write("Steel_MetalSmooth.png",steel);
            foreach(string file in Directory.GetFiles(Art+"/Textures","*.png"))
            {
                var imp=(TextureImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));
                imp.textureType=file.EndsWith("MicroNormal.png")?TextureImporterType.NormalMap:TextureImporterType.Default;
                imp.textureShape=TextureImporterShape.Texture2D;
                imp.sRGBTexture=file.EndsWith("Albedo.png");imp.maxTextureSize=2048;imp.anisoLevel=8;imp.wrapMode=TextureWrapMode.Repeat;
                imp.textureCompression=TextureImporterCompression.CompressedHQ;imp.SaveAndReimport();
            }
        }
        private static void Write(string name,Texture2D texture){string path=Art+"/Textures/"+name;File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);}
        private static void Materials()
        {
            void Surface(string name,Color color,float metal,float smooth,bool textured=false,bool emit=false)
            {
                var m=new Material(Shader.Find("Standard")){name=name,color=color};m.SetFloat("_Metallic",metal);m.SetFloat("_Glossiness",smooth);
                if(textured)
                {
                    if(name.StartsWith("Paint"))m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/PaintedAlloy_Albedo.png");
                    m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/MicroNormal.png"));m.SetFloat("_BumpScale",name.StartsWith("Paint")?.22f:.16f);m.EnableKeyword("_NORMALMAP");
                    m.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/"+(name.StartsWith("Steel")?"Steel":"Paint")+"_MetalSmooth.png"));m.EnableKeyword("_METALLICGLOSSMAP");m.SetFloat("_GlossMapScale",1);
                }
                if(emit){m.SetColor("_EmissionColor",color*2.2f);m.EnableKeyword("_EMISSION");}
                Save(m,Art+"/Materials/"+name+".mat");surfaces[name]=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/"+name+".mat");
            }
            surfaces.Clear();
            Surface("Paint_Ivory",new Color(.88f,.90f,.86f),.16f,.35f,true);Surface("Paint_Navy",new Color(.12f,.25f,.32f),.16f,.35f,true);
            Surface("Steel_Dark",new Color(.10f,.14f,.17f),.82f,.60f,true);Surface("Steel_Brush",new Color(.48f,.55f,.59f),.82f,.60f,true);
            Surface("Rubber",new Color(.025f,.032f,.038f),0,.15f);Surface("Safety_Yellow",new Color(.98f,.60f,.12f),.12f,.32f);
            Surface("Light_Warm",new Color(1,.84f,.61f),.05f,.3f,false,true);Surface("Signal_Cyan",new Color(.08f,.68f,.9f),.05f,.3f,false,true);
            Surface("Signal_Amber",new Color(1,.25f,.045f),.05f,.3f,false,true);Surface("Screen",new Color(.018f,.067f,.09f),.15f,.7f);
            Surface("Label",new Color(.86f,.92f,.90f),.05f,.2f);Surface("Glass",new Color(.13f,.31f,.4f),.25f,.85f);
        }
        private static void Save(UnityEngine.Object value,string path)
        {
            var previous=AssetDatabase.LoadMainAssetAtPath(path);
            if(previous==null)AssetDatabase.CreateAsset(value,path);else{EditorUtility.CopySerialized(value,previous);UnityEngine.Object.DestroyImmediate(value);EditorUtility.SetDirty(previous);}
        }
    }
}
