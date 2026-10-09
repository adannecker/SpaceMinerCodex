using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpaceMiner
{
    // Geometry/input state is independent of IMGUI and of research progression.
    public sealed class ResearchTreeViewport
    {
        [System.Serializable] public class LayoutNode { public string id; public float x,y,width,height; }
        [System.Serializable] public class Edge { public string source,target; public bool sharedBus; public Vector2[] points; }
        [System.Serializable] public class Layout { public string id; public float width,height; public LayoutNode[] nodes; public Edge[] edges; }
        [System.Serializable] public class Layouts { public Layout[] trees; }
        private static Layouts layouts;
        private Layout approved;
        public readonly List<Edge> Edges=new List<Edge>();
        public readonly Dictionary<string,Rect> Nodes=new Dictionary<string,Rect>();
        public readonly Dictionary<string,List<string>> Children=new Dictionary<string,List<string>>();
        public readonly Dictionary<string,string> Parents=new Dictionary<string,string>();
        public Rect Bounds { get; private set; }
        public float Zoom { get; private set; }
        public float Pan { get; private set; }
        public float PanY { get; private set; }
        private Vector2 pressed,last;
        private string pressedNode;
        private bool dragging,moved;
        public void SetTree(ResearchTree tree)
        {
            Nodes.Clear();Children.Clear();Parents.Clear();Edges.Clear();
            var ids=new HashSet<string>(tree.nodes.Select(n=>n.id));
            foreach(var id in ids)Children[id]=new List<string>();
            var roots=new List<string>();
            foreach(var n in tree.nodes){
                string parent=n.parents.FirstOrDefault(ids.Contains);
                if(parent==null)roots.Add(n.id);
                else {Parents[n.id]=parent;Children[parent].Add(n.id);}
            }
            layouts??=JsonUtility.FromJson<Layouts>(Resources.Load<TextAsset>("Research/layouts").text);
            approved=layouts.trees.First(t=>t.id==tree.id);
            ApplySpacing(1);
            Reset();
        }
        public void StretchTo(Rect viewport) { }
        private void ApplySpacing(float factor)
        {
            Nodes.Clear();Edges.Clear();
            foreach(var n in approved.nodes)Nodes.Add(n.id,new Rect(n.x,n.y,n.width,n.height));
            Edges.AddRange(approved.edges);
            Bounds=new Rect(0,0,approved.width,approved.height);
        }
        public void Reset(){Zoom=.5f;Pan=PanY=0;dragging=false;}
        public float Fit(Rect viewport)=>Mathf.Min(viewport.width/Bounds.width,viewport.height/Bounds.height);
        public float Scale(Rect viewport)=>Mathf.Lerp(.5f,1.5f,Zoom);
        private float MinPan(Rect viewport)=>Mathf.Min(0,viewport.width-Bounds.width*Scale(viewport));
        private float MinPanY(Rect viewport)=>Mathf.Min(0,viewport.height-Bounds.height*Scale(viewport));
        public void SetZoom(float zoom,Rect viewport)
        {
            Zoom=Mathf.Clamp01(zoom);
            Pan=Mathf.Clamp(Pan,MinPan(viewport),0);PanY=Mathf.Clamp(PanY,MinPanY(viewport),0);
        }
        public Vector2 Offset(Rect viewport)
        {
            float scale=Scale(viewport);
            Pan=Mathf.Clamp(Pan,MinPan(viewport),0);PanY=Mathf.Clamp(PanY,MinPanY(viewport),0);
            return new Vector2(Mathf.Max(0,(viewport.width-Bounds.width*scale)/2)+Pan,Mathf.Max(0,(viewport.height-Bounds.height*scale)/2)+PanY);
        }
        public Vector2 Point(Vector2 p,Rect viewport)=>viewport.position+Offset(viewport)+p*Scale(viewport);
        public Rect NodeRect(string id,Rect viewport)
        {
            var r=Nodes[id];
            return new Rect(Point(r.position,viewport),r.size*Scale(viewport));
        }
        public string Hit(Vector2 mouse,Rect viewport)
        {
            if(!viewport.Contains(mouse))return null;
            foreach(var id in Nodes.Keys)if(NodeRect(id,viewport).Contains(mouse))return id;
            return null;
        }
        public void Press(Vector2 point,Rect viewport)
        {dragging=true;moved=false;pressed=last=point;pressedNode=Hit(point,viewport);}
        public void Drag(Vector2 point,Rect viewport)
        {
            if(!dragging)return;
            if(Vector2.Distance(point,pressed)>5)moved=true;
            if(moved){Pan=Mathf.Clamp(Pan+point.x-last.x,MinPan(viewport),0);PanY=Mathf.Clamp(PanY+point.y-last.y,MinPanY(viewport),0);}
            last=point;
        }
        public string Release(Vector2 point,Rect viewport)
        {
            if(!dragging)return null;
            dragging=false;
            return !moved&&Hit(point,viewport)==pressedNode?pressedNode:null;
        }
    }
}
