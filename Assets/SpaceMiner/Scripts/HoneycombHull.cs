using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceMiner
{
    // A clipped hexagonal tiling. Full polygon backs meet; inset faces form visible bevelled seams.
    // The supporting campaign hull still supplies collision and the continuous pressure backing.
    internal static class HoneycombHull
    {
        internal const float Radius = .78f, StandOff = .28f, Thickness = .08f;
        internal static List<List<Vector2>> Cells(float width, float height)
        {
            var result = new List<List<Vector2>>();
            float dx = Mathf.Sqrt(3) * Radius, dy = Radius * 1.5f;
            for (int row = -1; row <= Mathf.CeilToInt(height / dy) + 1; row++)
                for (int col = -1; col <= Mathf.CeilToInt(width / dx) + 1; col++) {
                    Vector2 centre = new Vector2(col * dx + (Math.Abs(row) % 2) * dx / 2, row * dy);
                    var polygon = new List<Vector2>();
                    for (int i = 0; i < 6; i++) {
                        float a = (30 + i * 60) * Mathf.Deg2Rad;
                        polygon.Add(centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Radius);
                    }
                    polygon = Clip(Clip(Clip(Clip(polygon, 0, 0, true), 0, width, false), 1, 0, true), 1, height, false);
                    if (Area(polygon) > .0001f) result.Add(polygon);
                }
            return result;
        }
        private static List<Vector2> Clip(List<Vector2> input, int axis, float edge, bool above)
        {
            var output = new List<Vector2>();
            if (input.Count == 0) return output;
            Vector2 a = input[input.Count - 1]; bool ai = above ? a[axis] >= edge : a[axis] <= edge;
            foreach (var b in input) {
                bool bi = above ? b[axis] >= edge : b[axis] <= edge;
                if (ai != bi) output.Add(Vector2.Lerp(a, b, (edge - a[axis]) / (b[axis] - a[axis])));
                if (bi) output.Add(b); a = b; ai = bi;
            }
            return output;
        }
        internal static float Area(List<Vector2> polygon)
        {
            float area = 0;
            for (int i = 0; i < polygon.Count; i++) { var a = polygon[i]; var b = polygon[(i + 1) % polygon.Count]; area += a.x * b.y - b.x * a.y; }
            return Mathf.Abs(area) / 2;
        }
        internal static List<List<Vector2>> Disc(float radius)
        {
            var result=new List<List<Vector2>>();
            foreach(var cell in Cells(radius*2,radius*2)) {
                var polygon=new List<Vector2>(); foreach(var p in cell) polygon.Add(p-Vector2.one*radius);
                // The 48-sided rim is shared by all clipped edge tiles, leaving no square overhang.
                for(int side=0;side<48 && polygon.Count>0;side++) {
                    float angle=side*Mathf.PI/24; Vector2 normal=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                    float edge=radius*Mathf.Cos(Mathf.PI/48); var output=new List<Vector2>();
                    Vector2 a=polygon[polygon.Count-1]; float da=Vector2.Dot(a,normal)-edge;
                    foreach(var b in polygon) {
                        float db=Vector2.Dot(b,normal)-edge;
                        if((da<=0)!=(db<=0)) output.Add(Vector2.Lerp(a,b,da/(da-db)));
                        if(db<=0) output.Add(b); a=b; da=db;
                    }
                    polygon=output;
                }
                if(Area(polygon)>.0001f) result.Add(polygon);
            }
            return result;
        }
        internal static Mesh Mesh(string name, IList<List<Vector2>> cells, Func<Vector2, float, Vector3> map)
        {
            var vertices = new List<Vector3>(); var faces = new List<int>(); var rims = new List<int>();
            void Triangle(Vector3 a, Vector3 b, Vector3 c, List<int> indices) {
                int n = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c); indices.Add(n); indices.Add(n + 1); indices.Add(n + 2);
            }
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, List<int> indices) { Triangle(a,b,c,indices); Triangle(a,c,d,indices); }
            foreach (var polygon in cells) {
                Vector2 centre = Vector2.zero; foreach (var p in polygon) centre += p; centre /= polygon.Count;
                var normal = map(centre, 1) - map(centre, 0);
                bool reverse = Vector3.Dot(Vector3.Cross(map(centre+Vector2.right*.01f,0)-map(centre,0),map(centre+Vector2.up*.01f,0)-map(centre,0)),normal) < 0;
                for (int i = 0; i < polygon.Count; i++) {
                    Vector2 a = polygon[i], b = polygon[(i + 1) % polygon.Count]; if (reverse) { var swap = a; a = b; b = swap; }
                    Vector2 ia = Vector2.Lerp(centre,a,.96f), ib = Vector2.Lerp(centre,b,.96f);
                    Triangle(map(centre,Thickness/2), map(ia,Thickness/2), map(ib,Thickness/2), faces);
                    Quad(map(ia,Thickness/2),map(a,.008f),map(b,.008f),map(ib,Thickness/2),rims);
                    Quad(map(a,.008f),map(a,-Thickness/2),map(b,-Thickness/2),map(b,.008f),rims);
                    Triangle(map(centre,-Thickness/2),map(b,-Thickness/2),map(a,-Thickness/2),rims);
                }
            }
            var mesh = new Mesh { name = name, indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices); mesh.subMeshCount = 2; mesh.SetTriangles(faces,0); mesh.SetTriangles(rims,1); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            StationSurfaceMaterials.WorldUvs(mesh);
            return mesh;
        }
        internal static Transform Surface(string name, Transform parent, float width, float height, Func<Vector2,float,Vector3> map,
            Material face, Material edge, List<Mesh> owned, int layer)
        {
            var cells = Cells(width,height); var mesh = Mesh(name,cells,map); owned.Add(mesh);
            var node = new GameObject(name).transform; node.SetParent(parent,false); node.gameObject.layer = layer;
            node.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            node.gameObject.AddComponent<MeshRenderer>().sharedMaterials = new[] {face,edge}; return node;
        }
    }
}
