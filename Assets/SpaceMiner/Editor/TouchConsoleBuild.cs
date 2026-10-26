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
                    material.enableInstancing=true;material.color=Color.white;
                    if(group.name.Contains("Surface")){
                        material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SpaceMiner/Resources/TouchConsole/Textures/TouchConsole_Albedo.png");
                        material.SetFloat("_Metallic",.65f);material.SetFloat("_Glossiness",.35f);
                        material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SpaceMiner/Resources/TouchConsole/Textures/TouchConsole_Normal.png"));material.EnableKeyword("_NORMALMAP");
                    }else{material.color=new Color(.012f,.025f,.03f);material.SetFloat("_Glossiness",.25f);}
                    EditorUtility.SetDirty(material);
                    var part=new GameObject(group.name);part.transform.SetParent(root.transform,false);part.AddComponent<MeshFilter>().sharedMesh=mesh;part.AddComponent<MeshRenderer>().sharedMaterial=material;
                }
                PrefabUtility.SaveAsPrefabAsset(root,Folder+"/"+variant+".prefab");
                }finally{UnityEngine.Object.DestroyImmediate(root);}
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("R04 STANDARD AND WIDE CONSOLE ASSETS PREPARED");
        }
        public static void Build()
        {
            Prepare();
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/SpaceMiner/Scenes/AsteroidBelt.unity"},locationPathName="Builds/TouchConsoleR04/SpaceMiner.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("R04 build failed");
        }
    }
}
