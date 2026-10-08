using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering;

namespace SpaceMiner.Editor
{
    public static class Mira3DDemoBuild
    {
        [Serializable] private class Shape { public string name; public Vector3[] delta; }
        [Serializable] private class Part { public string name, texture; public bool cutout; public float[] color; public float roughness; public Vector3[] vertices, normals; public Vector2[] uv; public int[] triangles; public Shape[] morphs; }
        [Serializable] private class Model { public Part[] meshes; public float headTop; }
        public static void Build()
        {
            AssetDatabase.Refresh();
            const string folder = "Assets/SpaceMiner/AvatarLab/Model3D";
            var model = JsonUtility.FromJson<Model>(File.ReadAllText(folder + "/mira-geometry.json"));
            foreach(var part in model.meshes)
            {
                if(string.IsNullOrEmpty(part.texture))continue;
                var importer=AssetImporter.GetAtPath(folder+"/"+part.texture) as TextureImporter;
                if(importer==null)continue;
                importer.alphaIsTransparency=part.cutout;importer.mipmapEnabled=!part.cutout;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.maxTextureSize=2048;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var background = new GameObject("Preview background camera").AddComponent<Camera>();
            background.clearFlags = CameraClearFlags.SolidColor; background.backgroundColor = new Color(.025f,.045f,.075f); background.cullingMask = 0;
            var actor = new GameObject("Mira 3D").AddComponent<MiraAvatar>();
            actor.Expressions = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SpaceMiner/AvatarLab/MiraExpressions.png");
            var speech = actor.gameObject.AddComponent<AudioSource>(); speech.playOnAwake = false;
            var demo = actor.gameObject.AddComponent<MiraDialoguePreview>(); demo.Avatar=actor; demo.Speech=speech;
            var avatar = actor.gameObject.AddComponent<MiraAvatar3D>(); avatar.Driver=actor; demo.Avatar3D=avatar;
            var bust = new GameObject("Mira head and shoulders"); avatar.Bust=bust.transform;
            foreach(var part in model.meshes)
            {
                var obj=new GameObject(part.name); obj.layer=27; obj.transform.SetParent(bust.transform,false);
                if(part.name.Contains("individual dark chestnut") || part.name.Contains("HairCards"))avatar.HairDetail=obj.transform;
                var mesh=new Mesh { name=part.name, indexFormat=IndexFormat.UInt32, vertices=part.vertices, normals=part.normals, uv=part.uv };
                var triangles=part.triangles;
                mesh.triangles=triangles; mesh.RecalculateBounds();
                foreach(var shape in part.morphs) mesh.AddBlendShapeFrame(shape.name,100,shape.delta,null,null);
                string path=folder+"/"+part.name.Replace(" ","_")+".asset";
                var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(existing!=null){EditorUtility.CopySerialized(mesh,existing);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
                var material=new Material(Shader.Find(part.name=="Mira_AnatomicalBase"?"SpaceMiner/MiraSkin":part.cutout?"SpaceMiner/MiraHair":"Standard"));
                material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/"+part.texture);
                if(part.color!=null && part.color.Length==4)material.color=new Color(part.color[0],part.color[1],part.color[2],part.color[3]);
                material.SetFloat("_Glossiness",1-part.roughness);
                if(part.cutout)material.SetFloat("_Cutoff",part.name.Contains("eyelash")?.6f:part.name.Contains("HairCards")?.25f:part.name.Contains("short03")?.25f:.45f);
                
                if(part.name.Contains("cyan")){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",new Color(.06f,.30f,.35f));}
                var renderer=obj.AddComponent<SkinnedMeshRenderer>();renderer.sharedMesh=mesh;renderer.sharedMaterial=material;renderer.localBounds=mesh.bounds;renderer.updateWhenOffscreen=true;
                if(part.name=="Mira_AnatomicalBase")avatar.Face=renderer;
                if(part.name.Contains("teeth"))avatar.Teeth=renderer;
                string matPath=folder+"/"+part.name.Replace(" ","_")+".mat";
                var old=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(old!=null){EditorUtility.CopySerialized(material,old);renderer.sharedMaterial=old;}else AssetDatabase.CreateAsset(material,matPath);
            }
            float top=model.headTop,center=top-.15f;
            avatar.Portrait=AssetDatabase.LoadAssetAtPath<RenderTexture>(folder+"/MiraPortrait.renderTexture");
            if(avatar.Portrait==null){avatar.Portrait=new RenderTexture(700,700,24);AssetDatabase.CreateAsset(avatar.Portrait,folder+"/MiraPortrait.renderTexture");}
            var camera=new GameObject("Mira portrait camera").AddComponent<Camera>();avatar.Portrait.antiAliasing=4;camera.allowMSAA=true;camera.targetTexture=avatar.Portrait;camera.cullingMask=1<<27;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.045f,.075f);
            camera.transform.position=new Vector3(.08f,center,1.5f);camera.transform.LookAt(new Vector3(0,center,0));camera.orthographic=true;camera.orthographicSize=.205f;
            AddLight("Key",new Vector3(-.8f,top+.3f,1),new Color(1,.88f,.78f),1.15f,center);
            AddLight("Fill",new Vector3(.8f,top,1),new Color(.64f,.8f,1),.65f,center);
            AddLight("Cyan rim",new Vector3(.4f,top+.1f,-.6f),new Color(.15f,.8f,1),1.2f,center);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.3f,.32f,.35f);
            const string scenePath="Assets/SpaceMiner/AvatarLab/Mira3DDemo.unity";
            EditorSceneManager.SaveScene(scene,scenePath);AssetDatabase.SaveAssets();
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scenePath},locationPathName="Builds/Mira3DDemo/Mira3DDemo.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Mira 3D demo build failed");
        }
        private static void AddLight(string name,Vector3 position,Color color,float intensity,float center)
        {
            var light=new GameObject(name).AddComponent<Light>();light.type=LightType.Directional;light.color=color;light.intensity=intensity;light.cullingMask=1<<27;
            light.transform.position=position;light.transform.LookAt(new Vector3(0,center,0));light.shadows=LightShadows.None;
        }
    }
}
