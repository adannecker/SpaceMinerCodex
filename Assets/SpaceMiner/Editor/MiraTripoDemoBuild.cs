using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceMiner.Editor
{
    public static class MiraTripoDemoBuild
    {
        private const string Folder="Assets/SpaceMiner/AvatarLab/TripoH25";
        private static Mesh mesh;
        private static Material material;
        public static void Inspect()
        {
            Prepare();
            CreateScene();
            var camera=GameObject.Find("Mira portrait camera").GetComponent<Camera>();
            Directory.CreateDirectory("Logs/MiraTripo");
            foreach(float angle in new[]{0f,90f,180f,270f})
            {
                camera.transform.position=Quaternion.Euler(0,angle,0)*new Vector3(0,0,1.5f)+new Vector3(0,.34f,0);
                camera.transform.LookAt(new Vector3(0,.34f,0));
                Capture(camera,"Logs/MiraTripo/inspect-"+angle+".png");
            }
            camera.transform.position=new Vector3(0,.34f,1.5f);camera.transform.LookAt(new Vector3(0,.34f,0));
            var face=GameObject.Find("Mira Tripo face").GetComponent<SkinnedMeshRenderer>();
            foreach(var pose in new[]{"BlinkLeft","BlinkRight","LipClose","Round","Wide","Smile","BrowConcern","BrowFocus","LookLeft"})
            {
                face.SetBlendShapeWeight(mesh.GetBlendShapeIndex(pose),100);
                Capture(camera,"Logs/MiraTripo/pose-"+pose+".png");
                face.SetBlendShapeWeight(mesh.GetBlendShapeIndex(pose),0);
            }
            var head=GameObject.Find("Mira head pivot").transform;head.localRotation=Quaternion.Euler(2,8,1);
            Capture(camera,"Logs/MiraTripo/pose-head.png");head.localRotation=Quaternion.identity;
            using(var writer=new BinaryWriter(File.Create("Logs/MiraTripo/vertices.bin")))
            {
                writer.Write(mesh.vertexCount);
                foreach(var v in mesh.vertices){writer.Write(v.x);writer.Write(v.y);writer.Write(v.z);}
                foreach(var uv in mesh.uv){writer.Write(uv.x);writer.Write(uv.y);}
                writer.Write(mesh.triangles.Length);foreach(var i in mesh.triangles)writer.Write(i);
            }
            Debug.Log("MIRA TRIPO INSPECT: vertices="+mesh.vertexCount+" bounds="+mesh.bounds);
        }
        public static void Build()
        {
            Prepare();
            CreateScene();
            const string scene="Assets/SpaceMiner/AvatarLab/MiraTripoDemo.unity";
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),scene);
            AssetDatabase.SaveAssets();
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{scene},
                locationPathName="Builds/MiraTripoDemo/MiraTripoDemo.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development });
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Mira Tripo build failed");
            Debug.Log("MIRA TRIPO BUILD PASSED");
        }
        private static void Prepare()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            AssetDatabase.Refresh();
            string fbx=Directory.GetFiles(Folder,"*.fbx").Single().Replace('\\','/');
            var importer=(ModelImporter)AssetImporter.GetAtPath(fbx);
            importer.isReadable=true;importer.importAnimation=false;importer.addCollider=false;
            importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
            foreach(string file in Directory.GetFiles(Folder,"*",SearchOption.AllDirectories))
            {
                var texture=AssetImporter.GetAtPath(file.Replace('\\','/')) as TextureImporter;
                if(texture==null)continue;
                texture.maxTextureSize=2048;texture.isReadable=true;
                texture.sRGBTexture=Path.GetFileName(file)=="Color.jpg";
                texture.textureType=Path.GetFileName(file)=="Normal.png"?TextureImporterType.NormalMap:TextureImporterType.Default;
                texture.textureCompression=TextureImporterCompression.CompressedHQ;texture.filterMode=FilterMode.Trilinear;texture.anisoLevel=4;
                texture.SaveAndReimport();
            }
            Texture2D Map(string suffix)=>AssetDatabase.LoadAssetAtPath<Texture2D>(Directory.GetFiles(Folder,"*",SearchOption.AllDirectories).Single(p=>p.EndsWith(suffix,StringComparison.OrdinalIgnoreCase)).Replace('\\','/'));
            var instance=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));
            var filter=instance.GetComponentsInChildren<MeshFilter>().Single();
            var original=filter.sharedMesh;
            var vertices=original.vertices.Select(v=>filter.transform.TransformPoint(v)).ToArray();
            var bounds=new Bounds(vertices[0],Vector3.zero);foreach(var v in vertices)bounds.Encapsulate(v);
            float scale=.62f/bounds.size.y;
            for(int i=0;i<vertices.Length;i++)vertices[i]=(vertices[i]-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z))*scale;
            var normalMatrix=filter.transform.localToWorldMatrix.inverse.transpose;
            mesh=new Mesh { name="Mira Tripo H2.5 dialogue mesh",indexFormat=IndexFormat.UInt32,vertices=vertices,
                normals=original.normals.Select(v=>normalMatrix.MultiplyVector(v).normalized).ToArray(),uv=original.uv,triangles=original.triangles };
            mesh.RecalculateBounds();mesh.RecalculateTangents();
            MiraTripoFaceShapes.Refine(mesh);
            MiraTripoFaceShapes.Add(mesh);
            AddSpeechShape(mesh);
            UnityEngine.Object.DestroyImmediate(instance);
            Save(mesh,Folder+"/MiraTripoMesh.asset");mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/MiraTripoMesh.asset");
            var metal=Map("_metallic.JPEG");var rough=Map("_roughness.JPEG");
            var packed=new Texture2D(metal.width,metal.height,TextureFormat.RGBA32,false,true);
            var a=metal.GetPixels32();var b=rough.GetPixels32();
            for(int i=0;i<a.Length;i++)a[i]=new Color32(a[i].r,a[i].r,a[i].r,(byte)(255-b[i].r));
            packed.SetPixels32(a);packed.Apply();File.WriteAllBytes(Folder+"/MetalSmoothness.png",packed.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(packed);AssetDatabase.ImportAsset(Folder+"/MetalSmoothness.png");
            var packedImporter=(TextureImporter)AssetImporter.GetAtPath(Folder+"/MetalSmoothness.png");
            packedImporter.sRGBTexture=false;packedImporter.maxTextureSize=2048;packedImporter.textureCompression=TextureImporterCompression.CompressedHQ;packedImporter.SaveAndReimport();
            material=new Material(Shader.Find("Standard")){name="Mira Tripo PBR",color=Color.white,mainTexture=Map("Color.jpg")};
            material.SetTexture("_BumpMap",Map("Normal.png"));material.SetFloat("_BumpScale",.22f);material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/MetalSmoothness.png"));
            material.EnableKeyword("_METALLICGLOSSMAP");material.SetFloat("_GlossMapScale",.65f);
            Save(material,Folder+"/MiraTripoPBR.mat");material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/MiraTripoPBR.mat");
        }
        private static void AddSpeechShape(Mesh target)
        {
            // Small surface deformation only. The source has no separate dental/jaw rig.
            // Keep eyes, nose, skull, hair and clothing fixed; do not fabricate phonemes.
            var delta=new Vector3[target.vertexCount];var vertices=target.vertices;
            int affected=0;float maximum=0;
            for(int i=0;i<vertices.Length;i++)
            {
                var v=vertices[i];float x=(v.x+.025f)/.083f;
                float horizontal=Mathf.SmoothStep(0,1,Mathf.Clamp01(1-x*x));
                float seam=.292f+.18f*(v.x+.02f);
                float lower=Mathf.SmoothStep(0,1,Mathf.Clamp01((seam-.003f-v.y)/.018f));
                float chin=Mathf.SmoothStep(0,1,Mathf.Clamp01((v.y-.205f)/.035f));
                float front=Mathf.SmoothStep(0,1,Mathf.Clamp01((v.z-.115f)/.035f));
                float weight=horizontal*lower*chin*front;
                delta[i]=new Vector3(0,-.008f,.001f)*weight;
                if(weight>.001f)affected++;maximum=Mathf.Max(maximum,delta[i].magnitude);
            }
            if(affected<20 || maximum>.009f)throw new Exception("Invalid conservative speech shape");
            target.AddBlendShapeFrame("JawOpen",100,delta,null,null);
            Debug.Log("MIRA TRIPO SPEECH SHAPE: affected="+affected+" maximumMeters="+maximum);
        }
        private static void Save(UnityEngine.Object item,string path)
        {
            item.name=Path.GetFileNameWithoutExtension(path);
            if(item is Mesh)
            {
                // Recreate native skinning buffers; CopySerialized retained an incompatible
                // vertex/bone layout when upgrading the old, unskinned prototype mesh.
                AssetDatabase.CreateAsset(item,path);
                AssetDatabase.SaveAssets();
                return;
            }
            var previous=AssetDatabase.LoadMainAssetAtPath(path);
            if(previous!=null)
            {
                EditorUtility.CopySerialized(item,previous);
                UnityEngine.Object.DestroyImmediate(item);EditorUtility.SetDirty(previous);
            }
            else AssetDatabase.CreateAsset(item,path);
        }
        private static void CreateScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var background=new GameObject("Preview background camera").AddComponent<Camera>();
            background.gameObject.AddComponent<AudioListener>();
            background.clearFlags=CameraClearFlags.SolidColor;background.backgroundColor=new Color(.025f,.045f,.075f);background.cullingMask=0;
            var actor=new GameObject("Mira Tripo dialogue").AddComponent<MiraAvatar>();
            actor.Expressions=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SpaceMiner/AvatarLab/MiraExpressions.png");
            var speech=actor.gameObject.AddComponent<AudioSource>();speech.playOnAwake=false;
            var demo=actor.gameObject.AddComponent<MiraDialoguePreview>();demo.Avatar=actor;demo.Speech=speech;
            demo.TripoPrototype=true;demo.ModelLabel="Tripo-Mira H2.5 · echter 3D-Kopf mit Schultern";
            actor.gameObject.AddComponent<MiraTripoDialogueCheck>();
            var avatar=actor.gameObject.AddComponent<MiraAvatar3D>();avatar.Driver=actor;demo.Avatar3D=avatar;
            avatar.JawWeight=100;avatar.EnhancedFace=true;
            var bust=new GameObject("Mira Tripo head and shoulders");avatar.Bust=bust.transform;
            var head=new GameObject("Mira head pivot").transform;head.SetParent(bust.transform,false);head.localPosition=new Vector3(0,.245f,0);
            var face=new GameObject("Mira Tripo face");face.layer=27;face.transform.SetParent(bust.transform,false);
            var renderer=face.AddComponent<SkinnedMeshRenderer>();renderer.sharedMesh=mesh;renderer.sharedMaterial=material;
            renderer.bones=new[]{bust.transform,head};renderer.rootBone=bust.transform;
            renderer.localBounds=mesh.bounds;renderer.updateWhenOffscreen=true;avatar.Face=renderer;
            var performance=actor.gameObject.AddComponent<MiraFacePerformance>();performance.Avatar=avatar;performance.Head=head;
            var portrait=new RenderTexture(1000,1000,24){antiAliasing=4,name="Mira Tripo portrait"};
            Save(portrait,Folder+"/MiraTripoPortrait.renderTexture");avatar.Portrait=AssetDatabase.LoadAssetAtPath<RenderTexture>(Folder+"/MiraTripoPortrait.renderTexture");
            var camera=new GameObject("Mira portrait camera").AddComponent<Camera>();camera.targetTexture=avatar.Portrait;
            camera.cullingMask=1<<27;camera.allowMSAA=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=background.backgroundColor;
            camera.transform.position=new Vector3(0,.34f,1.5f);camera.transform.LookAt(new Vector3(0,.34f,0));
            camera.orthographic=true;camera.orthographicSize=.335f;
            Light("Portrait key",new Vector3(-.7f,.9f,1),new Color(1,.92f,.85f),1.05f);
            Light("Portrait fill",new Vector3(.8f,.6f,.9f),new Color(.72f,.86f,1),.45f);
            Light("Portrait rim",new Vector3(.5f,.8f,-1),new Color(.28f,.75f,1),.7f);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.36f,.36f,.38f);
        }
        private static void Light(string name,Vector3 position,Color color,float strength)
        {
            var light=new GameObject(name).AddComponent<Light>();light.type=LightType.Directional;light.color=color;
            light.intensity=strength;light.cullingMask=1<<27;light.transform.position=position;
            light.transform.LookAt(new Vector3(0,.34f,0));light.shadows=LightShadows.None;
        }
        private static void Capture(Camera camera,string path)
        {
            // Camera.Render in batch mode does not advance the skinning player loop.
            // Bake the *current* bones/shapes on CPU so every inspected pose is real.
            var skinned=GameObject.Find("Mira Tripo face").GetComponent<SkinnedMeshRenderer>();
            var baked=new Mesh();skinned.BakeMesh(baked);
            var proxy=new GameObject("Pose inspection surface");proxy.layer=27;
            proxy.transform.SetParent(skinned.transform,false);
            proxy.AddComponent<MeshFilter>().sharedMesh=baked;
            proxy.AddComponent<MeshRenderer>().sharedMaterial=skinned.sharedMaterial;
            skinned.enabled=false;
            camera.Render();var previous=RenderTexture.active;RenderTexture.active=camera.targetTexture;
            var image=new Texture2D(camera.targetTexture.width,camera.targetTexture.height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,image.width,image.height),0,0);image.Apply();
            File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=previous;
            skinned.enabled=true;UnityEngine.Object.DestroyImmediate(proxy);UnityEngine.Object.DestroyImmediate(baked);
        }
    }
}
