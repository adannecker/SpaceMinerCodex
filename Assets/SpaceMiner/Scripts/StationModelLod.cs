using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    public sealed class StationModelLod:MonoBehaviour
    {
        private static readonly List<LODGroup> groups=new List<LODGroup>();
        public static int GroupCount=>groups.Count;
        internal static void Add(Transform hierarchy,string family)
        {
            var medium=Resources.Load<Mesh>("StationLOD/"+family+"Medium");var far=Resources.Load<Mesh>("StationLOD/"+family+"Far");if(medium==null||far==null)return;
            foreach(var filter in hierarchy.GetComponentsInChildren<MeshFilter>())
            {
                if(filter.sharedMesh.vertexCount<100000||filter.GetComponent<LODGroup>()!=null)continue;
                var original=filter.GetComponent<Renderer>();var lod=filter.gameObject.AddComponent<LODGroup>();
                Renderer Make(Mesh mesh){var child=new GameObject(mesh.name);child.layer=filter.gameObject.layer;child.transform.SetParent(filter.transform,false);child.AddComponent<MeshFilter>().sharedMesh=mesh;var r=child.AddComponent<MeshRenderer>();r.sharedMaterials=original.sharedMaterials;var tint=new MaterialPropertyBlock();original.GetPropertyBlock(tint);r.SetPropertyBlock(tint);return r;}
                lod.SetLODs(new[]{new LOD(.35f,new[]{original}),new LOD(.075f,new[]{Make(medium)}),new LOD(.001f,new[]{Make(far)})});lod.fadeMode=LODFadeMode.None;lod.RecalculateBounds();groups.Add(lod);filter.gameObject.AddComponent<StationModelLod>();
            }
        }
        public static void ForceOriginal(bool original){foreach(var group in groups)if(group!=null)group.ForceLOD(original?0:-1);}
        private void OnDestroy(){groups.Remove(GetComponent<LODGroup>());}
    }
}
