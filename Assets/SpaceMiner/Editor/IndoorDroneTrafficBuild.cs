using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpaceMiner.Editor
{
    public static class IndoorDroneTrafficBuild
    {
        private const string Folder="Assets/SpaceMiner/Resources/IndoorDroneI75";
        private static Bounds BoundsIn(Transform root)
        {
            bool found=false; Bounds bounds=default;
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var box=filter.sharedMesh.bounds;
                for(int corner=0;corner<8;corner++)
                {
                    var sign=new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1);
                    var p=root.InverseTransformPoint(filter.transform.TransformPoint(box.center+Vector3.Scale(box.extents,sign)));
                    if(!found){bounds=new Bounds(p,Vector3.zero);found=true;}else bounds.Encapsulate(p);
                }
            }
            return bounds;
        }
        public static void Prepare()
        {
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            string source=Folder+"/I75_P20_V02.fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(source);
            importer.isReadable=true; importer.importCameras=false; importer.importLights=false;
            importer.materialImportMode=ModelImporterMaterialImportMode.None; importer.SaveAndReimport();
            var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/I75_Metal.mat");
            if(material==null){material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,Folder+"/I75_Metal.mat");}
            material.color=new Color(.18f,.27f,.39f);material.SetFloat("_Metallic",.5f);material.SetFloat("_Glossiness",.28f);material.enableInstancing=true;
            var status=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/I75_Status.mat");
            if(status==null){status=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(status,Folder+"/I75_Status.mat");}
            status.color=new Color(.08f,.68f,.78f);status.EnableKeyword("_EMISSION");status.SetColor("_EmissionColor",status.color*1.4f);
            var root=new GameObject("I75Traffic");
            try
            {
                var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(source),root.transform,false);
                model.name="Smart Mesh V02 / temporary metal finish";
                var before=BoundsIn(root.transform);
                if(before.size.x>before.size.z) model.transform.localRotation=Quaternion.Euler(0,-90,0)*model.transform.localRotation;
                var rotated=BoundsIn(root.transform); float scale=.75f/rotated.size.z; model.transform.localScale*=scale;
                var normalized=BoundsIn(root.transform);model.transform.localPosition-=new Vector3(normalized.center.x,normalized.min.y,normalized.center.z);
                foreach(var renderer in root.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial=material;
                foreach(var node in root.GetComponentsInChildren<Transform>()) node.gameObject.layer=26;
                var indicator=GameObject.CreatePrimitive(PrimitiveType.Cube);indicator.name="Provisional cyan status / no textured UVs";indicator.layer=26;
                indicator.transform.SetParent(root.transform,false);indicator.transform.localPosition=new Vector3(0,BoundsIn(root.transform).max.y*.78f,.75f*.49f);
                indicator.transform.localScale=new Vector3(.18f,.014f,.008f);Object.DestroyImmediate(indicator.GetComponent<Collider>());indicator.GetComponent<Renderer>().sharedMaterial=status;
                PrefabUtility.SaveAsPrefabAsset(root,Folder+"/I75Traffic.prefab");
                var bounds=BoundsIn(root.transform);Directory.CreateDirectory("Logs");File.WriteAllText("Logs/indoor-drone-model.txt","Original Smart Mesh V02; length="+bounds.size.z+"; bounds="+bounds.size+"; forward +Z; provisional untextured material.");
            }
            finally{Object.DestroyImmediate(root);}
            EditorUtility.SetDirty(material);EditorUtility.SetDirty(status);AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        }
        public static void Run(){Prepare();StartMenuBuild.Run();}
    }
}
