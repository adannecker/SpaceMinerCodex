using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceMiner
{
    public sealed partial class StationOperations
    {
        private readonly List<Renderer> unbatchedHull=new List<Renderer>(),batchedHull=new List<Renderer>();
        public int BatchedHullSourceCount=>unbatchedHull.Count;
        public int HullBatchCount=>batchedHull.Count;
        private void CombineStationHull()
        {
            // Spatial batches retain local bounds for culling and point lights. Doors, damaged modules,
            // future cut ports and authored hall moving parts remain independent.
            var meshes=new Dictionary<(Material,int),List<CombineInstance>>();
            var sources=new Dictionary<(Material,int),List<Renderer>>();
            foreach(var filter in Layout.Geometry.Root.GetComponentsInChildren<MeshFilter>()){
                var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null||!renderer.enabled||filter.sharedMesh==null||filter.GetComponent<TextMesh>()!=null||filter.GetComponentInParent<CommandRoomBlock>()!=null||filter.GetComponentInParent<StationPressureGate>()!=null||filter.GetComponentInParent<StationAirlock>()!=null||filter.GetComponentInParent<StationDroneHall>()!=null||filter.GetComponentInParent<MaintenanceLaserVisual>()!=null||filter.GetComponentInParent<StationBatteryBay>()!=null)continue;
                bool dynamic=false;for(var t=filter.transform;t!=null&&t!=Layout.Geometry.Root;t=t.parent)if(t.name.Contains("Lagerkeil")||t.name.Contains("DRUCKLOS GESPERRT")||t.name.StartsWith("Reaktor-Servicegang")||t.name.StartsWith("Anbauslot ")||t.name.Contains("Geschlossener Ausbauanschluss")||t.name.StartsWith("Ringwand Lager")||t.name.Contains("Schleusen")){dynamic=true;break;}if(dynamic)continue;
                Vector3 point=Layout.transform.InverseTransformPoint(renderer.bounds.center);int sector=((Mathf.RoundToInt(Mathf.Atan2(point.x,point.z)*Mathf.Rad2Deg/30)%12)+12)%12;int region=sector*3+(point.magnitude<29?0:point.magnitude<39?1:2);
                bool opaque=true;foreach(var surface in renderer.sharedMaterials)if(surface==null||surface.renderQueue>2500)opaque=false;if(!opaque)continue;
                for(int sub=0;sub<filter.sharedMesh.subMeshCount;sub++){
                    var key=(renderer.sharedMaterials[Mathf.Min(sub,renderer.sharedMaterials.Length-1)],region);
                    if(!meshes.TryGetValue(key,out var list)){list=new List<CombineInstance>();meshes[key]=list;sources[key]=new List<Renderer>();}
                    list.Add(new CombineInstance{mesh=filter.sharedMesh,subMeshIndex=sub,transform=presentation.worldToLocalMatrix*filter.transform.localToWorldMatrix});sources[key].Add(renderer);
                }
            }
            foreach(var pair in meshes){var mesh=new Mesh{name="Station / räumlich gebündelte Hülle",indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray(),true,true);ownedMeshes.Add(mesh);var root=Group(mesh.name,presentation,Vector3.zero,Quaternion.identity);root.gameObject.layer=25;root.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=root.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=pair.Key.Item1;batchedHull.Add(renderer);foreach(var original in sources[pair.Key])if(!unbatchedHull.Contains(original))unbatchedHull.Add(original);}
            foreach(var original in unbatchedHull)original.enabled=false;
        }
        internal void ForceUnbatchedHull(bool original)
        {foreach(var source in unbatchedHull)if(source!=null)source.enabled=original;foreach(var batch in batchedHull)if(batch!=null)batch.enabled=!original;}
    }
}
