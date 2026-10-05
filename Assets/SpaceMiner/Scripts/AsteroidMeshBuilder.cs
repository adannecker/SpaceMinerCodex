using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    // Closed indexed icospheres: shared edges, no UV seam. Every LOD evaluates the same seed field.
    public static class AsteroidMeshBuilder
    {
        public sealed class Result
        {
            public Mesh[] Levels;
            public Mesh Collision => Levels[2];
        }

        private struct Region { public Vector3 Direction; public float Radius; }

        private sealed class Field
        {
            private readonly AsteroidType type;
            private readonly Vector3 offset, axes;
            private readonly Region[] craters, exposures, accents;
            private readonly Vector3[] planes;
            private readonly float[] planeDistances;

            public Field(AsteroidType definition, int seed)
            {
                type = definition;
                var random = new System.Random(seed);
                offset = new Vector3(Range(random, 20, 200), Range(random, 20, 200), Range(random, 20, 200));
                float v = type.Shape.AxisVariation;
                axes = Vector3.Scale(type.Shape.AxisScale,
                    new Vector3(Range(random, 1-v, 1+v), Range(random, 1-v, 1+v), Range(random, 1-v, 1+v)));
                craters = Regions(random, Mathf.Clamp(type.Shape.CraterCount, 0, 16), type.Shape.CraterRadius);
                exposures = Regions(random, Mathf.Clamp(type.Surface.ExposureCount, 0, 8), type.Surface.ExposureRadius);
                accents = Regions(random, Mathf.Clamp(type.Surface.AccentCount, 0, 8), type.Surface.AccentRadius);
                planes = new Vector3[Mathf.Clamp(type.Shape.FracturePlanes, 0, 20)];
                planeDistances = new float[planes.Length];
                for (int i = 0; i < planes.Length; i++)
                {
                    planes[i] = Direction(random);
                    planeDistances[i] = Range(random, 1f - type.Shape.FractureDepth, 1.02f);
                }
            }

            public Vector3 Evaluate(Vector3 direction, out Color color)
            {
                var s = type.Shape;
                float macro = Noise(direction * s.MacroFrequency + offset);
                float ridges = 1f - Mathf.Abs(Noise(direction * s.RidgeFrequency + offset * 0.7f));
                float radius = 1 + s.MacroAmplitude * macro + s.RidgeAmplitude * (ridges - 0.5f);
                // Neck plus unequal lobes, rather than arbitrary vertex jitter.
                radius *= 1 - s.LobeStrength * Mathf.Exp(-direction.x * direction.x * 8f);
                radius *= 1 + s.Asymmetry * (direction.x * 0.7f + direction.y * direction.z);
                radius = Mathf.Lerp(radius, Mathf.Round(radius*16f)/16f, s.TerraceStrength);
                for (int i = 0; i < planes.Length; i++)
                {
                    float dot = Vector3.Dot(direction, planes[i]);
                    if (dot > 0) radius = Mathf.Min(radius, planeDistances[i] / dot);
                }
                foreach (Region crater in craters)
                {
                    float distance = Vector3.Distance(direction, crater.Direction) / crater.Radius;
                    if (distance >= 1.15f) continue;
                    float bowl = Mathf.Max(0, 1 - distance * distance);
                    float rim = Mathf.Exp(-Mathf.Pow((distance - 0.93f) * 10f, 2f));
                    radius += s.CraterDepth * (-bowl * bowl + rim * 0.16f);
                }
                float exposure = Mask(direction, exposures);
                float accent = Mask(direction, accents) * (1 - exposure);
                radius -= exposure * type.Surface.ExposureRecess;
                float rubble = Noise(direction * 65f + offset * 1.3f);
                radius += s.RubbleAmplitude * rubble * (1-exposure*0.7f);
                float shade = Mathf.Clamp01(0.7f + macro * 0.15f);
                color = new Color(exposure, accent, shade, 1);
                return Vector3.Scale(direction * Mathf.Max(0.22f, radius), axes);
            }

            private float Mask(Vector3 direction, Region[] regions)
            {
                float mask = 0;
                float edge = Noise(direction * 22f + offset) * 0.018f;
                foreach (Region region in regions)
                {
                    float d = Vector3.Distance(direction, region.Direction) + edge;
                    float t = Mathf.InverseLerp(region.Radius * 1.12f, region.Radius * 0.72f, d);
                    mask = Mathf.Max(mask, t * t * (3 - 2 * t));
                }
                return mask;
            }

            private static Region[] Regions(System.Random random, int count, Vector2 radii)
            {
                var regions = new Region[count];
                for (int i = 0; i < count; i++)
                {
                    Vector3 direction = Vector3.zero;
                    float radius = Range(random, Mathf.Max(0.05f, radii.x), Mathf.Max(0.05f, radii.y));
                    for (int attempt = 0; attempt < 100; attempt++)
                    {
                        direction = Direction(random);
                        bool clear = true;
                        for (int j = 0; j < i; j++)
                            if (Vector3.Distance(direction, regions[j].Direction) < radius + regions[j].Radius) { clear = false; break; }
                        if (clear) break;
                    }
                    regions[i] = new Region { Direction = direction, Radius = radius };
                }
                return regions;
            }
        }

        public static Result Build(AsteroidType type, int seed, int highestSubdivision = 5)
        {
            if (type == null || type.Shape == null || type.Surface == null) throw new ArgumentException("Ein vollständiger Asteroidentyp ist erforderlich.");
            Vector3 axes = type.Shape.AxisScale;
            if (!(axes.x > 0 && axes.y > 0 && axes.z > 0) || float.IsInfinity(axes.sqrMagnitude))
                throw new ArgumentException("Die Formachsen müssen endlich und positiv sein.");
            if (highestSubdivision < 3 || highestSubdivision > 6) throw new ArgumentOutOfRangeException(nameof(highestSubdivision));
            var field = new Field(type, seed);
            var directions = new List<Vector3>();
            var faces = new List<int>();
            Icosahedron(directions, faces);
            var levelDirections = new List<Vector3>[4];
            var levelFaces = new List<int>[4];
            for (int depth = 0; depth <= highestSubdivision; depth++)
            {
                int level = highestSubdivision - depth;
                if (level < 4)
                {
                    levelDirections[level] = new List<Vector3>(directions);
                    levelFaces[level] = new List<int>(faces);
                }
                if (depth < highestSubdivision) Subdivide(directions, ref faces);
            }
            var results = new Mesh[4];
            Vector3 centre = Vector3.zero;
            float scale = 1;
            for (int level = 0; level < 4; level++)
            {
                var vertices = new Vector3[levelDirections[level].Count];
                var colors = new Color[vertices.Length];
                for (int i = 0; i < vertices.Length; i++) vertices[i] = field.Evaluate(levelDirections[level][i], out colors[i]);
                if (level == 0)
                {
                    var bounds = new Bounds(vertices[0], Vector3.zero);
                    foreach (Vector3 p in vertices) bounds.Encapsulate(p);
                    centre = bounds.center;
                    scale = 1 / Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                }
                // The same normalization is essential: LODs must not change size or shift origin.
                for (int i = 0; i < vertices.Length; i++) vertices[i] = (vertices[i] - centre) * scale;
                var mesh = new Mesh { name = type.name + "_" + seed + "_LOD" + level };
                mesh.SetVertices(vertices);
                mesh.SetColors(colors);
                mesh.SetTriangles(levelFaces[level], 0);
                mesh.RecalculateNormals();
                var normals = mesh.normals;
                var tangents = new Vector4[vertices.Length];
                for (int i = 0; i < tangents.Length; i++)
                {
                    Vector3 tangent = Vector3.Cross(Mathf.Abs(normals[i].y) > 0.95f ? Vector3.right : Vector3.up, normals[i]).normalized;
                    tangents[i] = new Vector4(tangent.x, tangent.y, tangent.z, 1);
                }
                mesh.tangents = tangents;
                mesh.RecalculateBounds();
                results[level] = mesh;
            }
            return new Result { Levels = results };
        }

        // Symmetric 3D sampling of Perlin avoids axis-dependent 2D banding.
        private static float Noise(Vector3 p)
        {
            return Mathf.Clamp(((Mathf.PerlinNoise(p.x, p.y) + Mathf.PerlinNoise(p.y, p.z) + Mathf.PerlinNoise(p.z, p.x)) * (2f / 3f) - 1)*1.8f,-1,1);
        }

        private static float Range(System.Random random, float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
        private static Vector3 Direction(System.Random random)
        {
            float z = Range(random, -1, 1), angle = Range(random, 0, Mathf.PI * 2);
            float r = Mathf.Sqrt(1-z*z);
            return new Vector3(r * Mathf.Cos(angle), z, r * Mathf.Sin(angle));
        }

        private static void Icosahedron(List<Vector3> vertices, List<int> faces)
        {
            float t = (1 + Mathf.Sqrt(5)) / 2;
            vertices.AddRange(new[] {
                new Vector3(-1,t,0), new Vector3(1,t,0), new Vector3(-1,-t,0), new Vector3(1,-t,0),
                new Vector3(0,-1,t), new Vector3(0,1,t), new Vector3(0,-1,-t), new Vector3(0,1,-t),
                new Vector3(t,0,-1), new Vector3(t,0,1), new Vector3(-t,0,-1), new Vector3(-t,0,1) });
            for (int i = 0; i < vertices.Count; i++) vertices[i] = vertices[i].normalized;
            faces.AddRange(new[] { 0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 });
        }

        private static void Subdivide(List<Vector3> vertices, ref List<int> faces)
        {
            var edges = new Dictionary<long, int>();
            var next = new List<int>(faces.Count * 4);
            for (int i = 0; i < faces.Count; i += 3)
            {
                int a = faces[i], b = faces[i+1], c = faces[i+2];
                int ab = Midpoint(a,b,vertices,edges), bc = Midpoint(b,c,vertices,edges), ca = Midpoint(c,a,vertices,edges);
                next.AddRange(new[] { a,ab,ca, b,bc,ab, c,ca,bc, ab,bc,ca });
            }
            faces = next;
        }

        private static int Midpoint(int a, int b, List<Vector3> vertices, Dictionary<long,int> edges)
        {
            long key = ((long)Mathf.Min(a,b) << 32) | (uint)Mathf.Max(a,b);
            if (edges.TryGetValue(key, out int index)) return index;
            index = vertices.Count;
            vertices.Add((vertices[a] + vertices[b]).normalized);
            edges.Add(key,index);
            return index;
        }
    }
}
