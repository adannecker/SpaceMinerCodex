using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Build.Reporting;
namespace SpaceMiner.Editor
{
    public static class TouchConsoleBuild
    {
        [Serializable] private class Group {public string name;public float[] vertices,normals,uv;public int[] triangles;}
        [Serializable] private class Data {public Group[] groups;}
        const string Folder="Assets/SpaceMiner/Resources/TouchConsoleR04";
        public static void Prepare()
        {
            AssetDatabase.Refresh();Directory.CreateDirectory(Folder);
            var data=JsonUtility.FromJson<Data>(File.ReadAllText("Assets/SpaceMiner/Art/TouchConsoleR04/geometry.json"));
            foreach(bool main in new[]{false,true})
            {
                string variant=main?"Main":"Standard";var root=new GameObject(variant);root.AddComponent<TouchConsoleVisual>().Main=main;
                try {foreach(var group in data.groups)
                {
                    var mesh=new Mesh{name=variant+group.name,indexFormat=IndexFormat.UInt32};
                    var vertices=new Vector3[group.vertices.Length/3];var normals=new Vector3[vertices.Length];var uv=new Vector2[vertices.Length];
                    for(int i=0;i<vertices.Length;i++){
                        float x=group.vertices[i*3];
                        // Expand middle, translate the side cheeks; do not stretch the metal frame.
                        if(main)x=Mathf.Abs(x)>=.6f?x+Mathf.Sign(x)*1.45f:x*(4.1f/1.2f);
                        vertices[i]=new Vector3(x,group.vertices[i*3+1],group.vertices[i*3+2]);
                        normals[i]=new Vector3(group.normals[i*3],group.normals[i*3+1],group.normals[i*3+2]);uv[i]=new Vector2(group.uv[i*2],group.uv[i*2+1]);
                    }
                    mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.triangles=group.triangles;mesh.RecalculateBounds();mesh.RecalculateTangents();
                    string meshPath=Folder+"/"+mesh.name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if(old==null)AssetDatabase.CreateAsset(mesh,meshPath);else{EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}
                    string matPath=Folder+"/"+group.name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if(material==null){material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,matPath);}
                    ConfigureMaterial(material,group.name.Contains("Surface"),group.name.Contains("Display"));
                    EditorUtility.SetDirty(material);
                    var part=new GameObject(group.name);part.transform.SetParent(root.transform,false);part.AddComponent<MeshFilter>().sharedMesh=mesh;part.AddComponent<MeshRenderer>().sharedMaterial=material;
                }
                PrefabUtility.SaveAsPrefabAsset(root,Folder+"/"+variant+".prefab");
                }finally{UnityEngine.Object.DestroyImmediate(root);}
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("R04 STANDARD AND WIDE CONSOLE ASSETS PREPARED");
        }
        private static Texture2D Texture(string path)
        {
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(texture==null)throw new FileNotFoundException("R04 console texture missing",path);
            return texture;
        }
        private static void ConfigureMaterial(Material material,bool housing,bool display)
        {
            material.shader=Shader.Find("Standard");material.enableInstancing=true;
            material.SetFloat("_Mode",0);material.SetFloat("_SpecularHighlights",housing?1:0);
            material.SetFloat("_GlossyReflections",housing?1:0);
            material.SetFloat("_SmoothnessTextureChannel",0);
            material.SetTexture("_EmissionMap",null);material.SetColor("_EmissionColor",Color.black);
            material.DisableKeyword("_EMISSION");
            if(housing)
            {
                material.color=Color.white;
                material.mainTexture=Texture(Folder+"/TouchConsole_BlueAlbedo.png");
                // Restore the authored per-material metal/smoothness variation:
                // painted cabinet, titanium frame, rubber and fasteners differ.
                material.SetTexture("_MetallicGlossMap",Texture("Assets/SpaceMiner/Resources/TouchConsole/Textures/TouchConsole_MetalSmooth.png"));
                material.SetFloat("_Metallic",1);material.SetFloat("_GlossMapScale",.82f);
                material.SetFloat("_Glossiness",.45f);material.EnableKeyword("_METALLICGLOSSMAP");
                material.SetTexture("_BumpMap",Texture("Assets/SpaceMiner/Resources/TouchConsole/Textures/TouchConsole_Normal.png"));
                material.SetFloat("_BumpScale",.32f);material.EnableKeyword("_NORMALMAP");
                material.DisableKeyword("_GLOSSYREFLECTIONS_OFF");
                material.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
            }
            else
            {
                material.mainTexture=null;material.SetTexture("_MetallicGlossMap",null);
                material.SetTexture("_BumpMap",null);material.DisableKeyword("_METALLICGLOSSMAP");
                material.DisableKeyword("_NORMALMAP");material.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
                material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                material.color=display?new Color(.006f,.018f,.035f):new Color(.009f,.019f,.032f);
                material.SetFloat("_Metallic",display?.08f:.25f);
                material.SetFloat("_Glossiness",display?.34f:.30f);
                // Both direct lamp glare and the unrelated exterior sky probe
                // are suppressed on the powered screen backing, not the metal.
                // This keeps the readout blue-black beneath the warm room lamps.
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor",display?new Color(.001f,.009f,.026f):new Color(.001f,.004f,.010f));
            }
        }
        public static void Build()
        {
            Prepare();
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/SpaceMiner/Scenes/AsteroidBelt.unity"},locationPathName="Builds/TouchConsoleR04/SpaceMiner.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("R04 build failed");
        }
    }
}
