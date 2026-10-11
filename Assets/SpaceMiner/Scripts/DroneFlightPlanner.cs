using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    // Optional route planner for the larger imported hull. Sweeps include its folded tools and cargo.
    internal sealed class DroneFlightPlanner
    {
        private readonly Transform owner;
        private readonly Dictionary<string,Vector3[]> cache=new Dictionary<string,Vector3[]>();
        internal Bounds[] HullBoxes;
        internal Vector3[] DockHullPoints;
        internal MeshCollider CradleCollider;
        internal int DockVertexSamples { get; private set; }
        internal int ClearanceSamples { get; private set; }
        private const float Radius=3.05f;
        internal int Detours { get; private set; }
        internal int PlannedRoutes { get; private set; }
        internal DroneFlightPlanner(Transform owner) { this.owner=owner; }
        internal bool PoseClear(Vector3 position,Quaternion rotation,out string obstruction)
        {
            ClearanceSamples++; obstruction=null;
            foreach(var box in HullBoxes)
                foreach(var collider in Physics.OverlapBox(position+rotation*box.center,box.extents,rotation,~0,QueryTriggerInteraction.Ignore))
                {
                    if(collider.transform.IsChildOf(owner)) continue;
                    if(collider==CradleCollider && DockMeshClear(position,rotation)) continue;
                    obstruction=collider.name+" / hull volume "+box; return false;
                }
            return true;
        }
        private bool DockMeshClear(Vector3 position,Quaternion rotation)
        {
            bool previous=Physics.queriesHitBackfaces; Physics.queriesHitBackfaces=true;
            try
            {
                foreach(var local in DockHullPoints)
                {
                    Vector3 point=position+rotation*local;
                    if(!CradleCollider.bounds.Contains(point)) continue;
                    DockVertexSamples++;
                    Vector3 origin=point+Vector3.up*6; float left=6; int crossings=0;
                    for(int i=0;i<12 && left>.0005f;i++)
                    {
                        if(!CradleCollider.Raycast(new Ray(origin,Vector3.down),out var hit,left)) break;
                        if(left-hit.distance<.003f) break; // Contact tolerance is three millimetres.
                        crossings++; float step=hit.distance+.0002f; origin+=Vector3.down*step; left-=step;
                    }
                    if(crossings%2!=0) return false;
                }
                return true;
            }
            finally { Physics.queriesHitBackfaces=previous; }
        }
        private sealed class Node { internal Vector3 Point; internal float Cost; internal int Parent; internal bool Closed; }
        private bool Blocked(Vector3 from,Vector3 to,out Collider obstacle)
        {
            Vector3 offset=to-from; float length=offset.magnitude; obstacle=null;
            if(length<.001f) return false;
            float nearest=float.PositiveInfinity;
            foreach(var hit in Physics.SphereCastAll(from+Vector3.up,Radius,offset/length,length,~0,QueryTriggerInteraction.Ignore))
            {
                if(hit.transform.IsChildOf(owner) || hit.collider.isTrigger || hit.distance>=nearest) continue;
                nearest=hit.distance; obstacle=hit.collider;
            }
            return obstacle!=null;
        }
        internal Vector3[] Plan(Vector3 from,Vector3 to)
        {
            string key=from.ToString("F2")+"/"+to.ToString("F2");
            if(cache.TryGetValue(key,out var saved)) return saved;
            PlannedRoutes++;
            var nodes=new List<Node>{new Node{Point=from,Parent=-1}};
            var seen=new HashSet<string>{from.ToString("F1")};
            for(int iteration=0;iteration<256;iteration++)
            {
                int current=-1; float best=float.PositiveInfinity;
                for(int i=0;i<nodes.Count;i++) if(!nodes[i].Closed)
                { float score=nodes[i].Cost+Vector3.Distance(nodes[i].Point,to); if(score<best){best=score;current=i;} }
                if(current<0) break;
                var node=nodes[current]; node.Closed=true;
                if(!Blocked(node.Point,to,out var obstacle))
                {
                    var route=new List<Vector3>{to};
                    for(int i=current;nodes[i].Parent>=0;i=nodes[i].Parent) route.Add(nodes[i].Point);
                    route.Reverse(); var result=route.ToArray(); cache[key]=result;
                    if(result.Length>1) Detours++;
                    return result;
                }
                Bounds box=obstacle.bounds; box.Expand((Radius+.55f)*2); box.center-=Vector3.up;
                var candidates=new List<Vector3>();
                for(int x=-1;x<=1;x+=2) for(int y=-1;y<=1;y+=2) for(int z=-1;z<=1;z+=2)
                    candidates.Add(box.center+Vector3.Scale(box.extents,new Vector3(x,y,z)));
                foreach(var direction in new[]{Vector3.up,Vector3.down,Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
                    candidates.Add(box.center+Vector3.Scale(box.extents,direction));
                foreach(var point in candidates)
                {
                    if(seen.Contains(point.ToString("F1")) || Blocked(node.Point,point,out _)) continue;
                    seen.Add(point.ToString("F1")); nodes.Add(new Node{Point=point,Cost=node.Cost+Vector3.Distance(node.Point,point),Parent=current});
                }
            }
            throw new Exception("No collision-free mining drone route: "+from+" -> "+to);
        }
    }
}
