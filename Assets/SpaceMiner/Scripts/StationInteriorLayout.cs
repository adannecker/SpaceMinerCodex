using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    public sealed class StationInteriorLayout : MonoBehaviour
    {
        public Transform Habitat { get; private set; }
        public Transform Ring { get; private set; }
        public Transform Console { get; private set; }
        public Transform RepairDrone { get; private set; }
        public StationAirlock Airlock { get; private set; }
        private readonly List<Material> materials = new List<Material>();
        private readonly List<Mesh> meshes = new List<Mesh>();
        private Material Surface(string name, Color color, bool glow = false)
        {
            var material = new Material(Shader.Find("Standard")) { name = name, color = color };
            material.SetFloat("_Metallic", .05f); material.SetFloat("_Glossiness", .15f);
            if (glow) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color); }
            materials.Add(material); return material;
        }
        private Transform Part(Transform parent, string name, Vector3 position, Vector3 size, Material material, PrimitiveType kind = PrimitiveType.Cube)
        {
            var part = GameObject.CreatePrimitive(kind); part.name = name; part.layer = 25;
            part.transform.SetParent(parent, false); part.transform.localPosition = position; part.transform.localScale = size;
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part.transform;
        }
        private void Lamp(Transform parent, string name, Vector3 position, float range, float intensity)
        {
            var lamp = new GameObject(name, typeof(Light)); lamp.transform.SetParent(parent, false); lamp.transform.localPosition = position;
            var light = lamp.GetComponent<Light>(); light.type = LightType.Point; light.color = new Color(.8f, .91f, 1);
            light.intensity = intensity; light.range = range; light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel; light.cullingMask = 1 << 25;
        }
        public void Build(StationVisual station)
        {
            if (Habitat != null) return;
            var floor = Surface("Interior deck", new Color(.18f, .24f, .29f));
            var wall = Surface("Interior cladding", new Color(.42f, .49f, .54f));
            var frame = Surface("Interior frames", new Color(.06f, .1f, .14f));
            var cyan = Surface("Steady interior strips", new Color(.18f, .58f, .7f), true);
            var amber = Surface("Maintenance yellow", new Color(.87f, .42f, .08f));
            var status = Surface("Airlock status", new Color(.08f, .7f, .65f), true);
            Habitat = new GameObject("Habitat module").transform; Habitat.SetParent(transform, false);
            Habitat.localRotation = Quaternion.Euler(0, 330, 0); Habitat.localPosition = Habitat.localRotation * Vector3.forward * 24;
            Part(Habitat, "Floor", new Vector3(0, -.15f, 0), new Vector3(8.4f, .3f, 10.4f), floor);
            Part(Habitat, "Ceiling", new Vector3(0, 3.15f, 0), new Vector3(8.4f, .3f, 10.4f), wall);
            for (int side = -1; side <= 1; side += 2)
            {
                Part(Habitat, "Solid side wall " + side, new Vector3(side * 4.1f, 1.5f, 0), new Vector3(.2f, 3, 10.4f), wall);
                Part(Habitat, "Ceiling strip " + side, new Vector3(side * 2.7f, 2.98f, 0), new Vector3(.09f, .03f, 8.8f), cyan);
                for (int panel = 0; panel < 3; panel++)
                    Part(Habitat, "Future equipment panel " + side + "/" + panel, new Vector3(side * 3.97f, 1.45f, -2.8f + panel * 2.8f), new Vector3(.035f, 1.65f, 2.1f), frame);
                Part(Habitat, "Rear wall side " + side, new Vector3(side * 2.625f, 1.5f, -5.1f), new Vector3(3.15f, 3, .2f), wall);
            }
            Part(Habitat, "Rear door lintel", new Vector3(0, 2.7f, -5.1f), new Vector3(2.1f, .6f, .2f), wall);
            Part(Habitat, "Front sill", new Vector3(0, .55f, 5.1f), new Vector3(8.4f, 1.1f, .2f), wall);
            Part(Habitat, "Front lintel", new Vector3(0, 2.8f, 5.1f), new Vector3(8.4f, .4f, .2f), wall);
            var glass = new Material(Resources.Load<Shader>("CargoGlass")) { name = "Habitat front glass", color = new Color(.19f, .45f, .55f, .10f) };
            materials.Add(glass);
            var window = Part(Habitat, "Single front window", new Vector3(0, 1.85f, 5.1f), new Vector3(8, 1.5f, 1), glass, PrimitiveType.Quad);
            Destroy(window.GetComponent<Collider>()); var windowCollider = window.gameObject.AddComponent<BoxCollider>(); windowCollider.size = new Vector3(1, 1, .05f);
            window.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Lamp(Habitat, "Habitat lamp rear", new Vector3(0, 2.5f, -2.5f), 9, 1.1f);
            Lamp(Habitat, "Habitat lamp front", new Vector3(0, 2.5f, 2.5f), 9, 1.1f);
            BuildAirlock(floor, wall, frame, status);
            BuildRing(station, floor, wall, frame, cyan);
            BuildConsole(frame, cyan, amber);
            BuildRepairDrone(frame, wall, amber);
            Physics.SyncTransforms();
        }
        private void BuildAirlock(Material floor, Material wall, Material frame, Material status)
        {
            Part(Habitat, "Airlock deck", new Vector3(0, -.13f, -7.85f), new Vector3(2.6f, .3f, 5.7f), floor);
            Part(Habitat, "Airlock roof", new Vector3(0, 3.15f, -7.85f), new Vector3(2.6f, .3f, 5.7f), wall);
            for (int side = -1; side <= 1; side += 2)
                Part(Habitat, "Airlock side " + side, new Vector3(side * 1.3f, 1.5f, -7.85f), new Vector3(.2f, 3, 5.7f), wall);
            var leaves = new Transform[2, 2];
            for (int door = 0; door < 2; door++)
            {
                float z = door == 0 ? StationAirlock.HabitatDoorZ : StationAirlock.RingDoorZ;
                for (int side = 0; side < 2; side++)
                {
                    float sign = side == 0 ? -1 : 1;
                    Part(Habitat, "Airlock door frame " + door + "/" + side, new Vector3(sign * 1.2f, 1.5f, z), new Vector3(.24f, 3, .3f), frame);
                    leaves[door, side] = Part(Habitat, "Electric door " + door + "/" + side, new Vector3(sign * .52f, 1.2f, z), new Vector3(1.04f, 2.4f, .12f), frame);
                    for (int face = -1; face <= 1; face += 2)
                        Part(leaves[door, side], "Door status stripe " + face, new Vector3(0, 0, face * .56f), new Vector3(.06f, .78f, .02f), status).GetComponent<Collider>().enabled = false;
                }
                Part(Habitat, "Door motor " + door, new Vector3(0, 2.7f, z), new Vector3(2.4f, .6f, .3f), frame);
            }
            Lamp(Habitat, "Airlock ceiling lamp", new Vector3(0, 2.6f, -7.8f), 6, 1);
            Airlock = Habitat.gameObject.AddComponent<StationAirlock>();
            Airlock.Configure(Habitat, leaves[0, 0], leaves[0, 1], leaves[1, 0], leaves[1, 1], status);
        }
        private void BuildRing(StationVisual station, Material floor, Material wall, Material frame, Material cyan)
        {
            var oldRing = station.transform.Find("Station Geometry/Access Ring");
            if (oldRing != null) foreach (Transform part in oldRing) part.gameObject.SetActive(false);
            foreach (string name in new[] { "Capped module port 5", "Module hatch 5" })
            { var cap = station.transform.Find("Station Geometry/" + name); if (cap != null) cap.gameObject.SetActive(false); }
            // The original station was solid scenery. End its supports at the new
            // corridor walls instead of leaving beams through the walking space.
            var bridge = station.DroneDock.Find("Dock access bridge");
            bridge.localPosition = new Vector3(0, -.4f, -4.7f);
            bridge.localScale = new Vector3(2.2f, .6f, 7.4f);
            for (int i = 0; i < 4; i++)
            {
                foreach (string name in new[] { "Access spoke " + i, "Spoke cover " + i })
                {
                    var support = station.transform.Find("Station Geometry/" + name);
                    if (support != null) { var size = support.localScale; size.z = 6.4f; support.localScale = size; }
                }
            }
            for (int i = 0; i < 5; i++)
            {
                var cap = station.transform.Find("Station Geometry/Capped module port " + i);
                if (cap != null) cap.localPosition = Quaternion.Euler(0, 30 + i * 60, 0) * Vector3.forward * 14.3f;
            }
            Ring = new GameObject("Walkable station ring").transform; Ring.SetParent(transform, false);
            Band("Ring deck", 10.5f, 13.5f, -.25f, 0, 0, 360, floor);
            Band("Ring roof", 10.5f, 13.5f, 3, 3.25f, 0, 360, wall);
            Band("Ring inner wall", 10.4f, 10.6f, 0, 3, 0, 360, wall);
            // An exact portal at the existing 330-degree module connection.
            Band("Ring outer wall", 13.4f, 13.6f, 0, 3, 335, 685, wall);
            for (int i = 0; i < 12; i++)
            {
                var turn = Quaternion.Euler(0, i * 30, 0);
                var strip = Part(Ring, "Ring guide light " + i, turn * new Vector3(0, 2.94f, 12), new Vector3(1.4f, .035f, .12f), cyan);
                strip.localRotation = turn;
                if (i % 2 == 0) Lamp(Ring, "Ring lamp " + i, turn * new Vector3(0, 2.5f, 12), 9, .8f);
            }
        }
        private void Band(string name, float inner, float outer, float bottom, float top, float start, float end, Material material)
        {
            int count = Mathf.CeilToInt((end - start) / 3.75f);
            var vertices = new List<Vector3>(); var indices = new List<int>();
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                int n = vertices.Count; vertices.AddRange(new[] { a, b, c, d });
                indices.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
            }
            Vector3 Point(float angle, float radius, float y) => Quaternion.Euler(0, angle, 0) * new Vector3(0, y, radius);
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.Lerp(start, end, i / (float)count), b = Mathf.Lerp(start, end, (i + 1) / (float)count);
                Quad(Point(a, inner, top), Point(a, outer, top), Point(b, outer, top), Point(b, inner, top));
                Quad(Point(a, inner, bottom), Point(b, inner, bottom), Point(b, outer, bottom), Point(a, outer, bottom));
                Quad(Point(a, outer, bottom), Point(b, outer, bottom), Point(b, outer, top), Point(a, outer, top));
                Quad(Point(b, inner, bottom), Point(a, inner, bottom), Point(a, inner, top), Point(b, inner, top));
            }
            if (end - start < 359)
            {
                Quad(Point(start, inner, bottom), Point(start, outer, bottom), Point(start, outer, top), Point(start, inner, top));
                Quad(Point(end, outer, bottom), Point(end, inner, bottom), Point(end, inner, top), Point(end, outer, top));
            }
            var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetTriangles(indices, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); meshes.Add(mesh);
            var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider)); part.layer = 25;
            part.transform.SetParent(Ring, false); part.GetComponent<MeshFilter>().sharedMesh = mesh;
            part.GetComponent<MeshRenderer>().sharedMaterial = material; part.GetComponent<MeshCollider>().sharedMesh = mesh;
        }
        private void BuildConsole(Material frame, Material cyan, Material amber)
        {
            Console = new GameObject("Command console").transform; Console.SetParent(Habitat, false); Console.localPosition = new Vector3(0, 0, 2.8f);
            Part(Console, "Console pedestal", new Vector3(0, .4f, 0), new Vector3(.85f, .8f, .6f), frame);
            Part(Console, "Console desk", new Vector3(0, .86f, 0), new Vector3(2.1f, .12f, .85f), frame);
            Part(Console, "Monitor housing", new Vector3(0, 1.25f, .18f), new Vector3(1.4f, .7f, .15f), frame);
            Part(Console, "Live monitor", new Vector3(0, 1.25f, .09f), new Vector3(1.24f, .53f, .025f), cyan);
            for (int i = 0; i < 4; i++) Part(Console, "Control key " + i, new Vector3(-.45f + i * .3f, .94f, -.23f), new Vector3(.15f, .035f, .15f), i == 0 ? amber : cyan);
        }
        private void BuildRepairDrone(Material frame, Material hull, Material amber)
        {
            Part(Habitat, "Repair drone charging shelf", new Vector3(-3.7f, .6f, .6f), new Vector3(.55f, .1f, .7f), frame);
            RepairDrone = new GameObject("Repair drone R-01 - offline").transform; RepairDrone.SetParent(Habitat, false); RepairDrone.localPosition = new Vector3(-3.58f, .82f, .6f);
            Part(RepairDrone, "Compact body", Vector3.zero, new Vector3(.38f, .22f, .34f), hull);
            Part(RepairDrone, "Powered-off sensor", new Vector3(.195f, .02f, 0), new Vector3(.02f, .07f, .18f), frame);
            for (int side = -1; side <= 1; side += 2)
            {
                Part(RepairDrone, "Folded repair arm " + side, new Vector3(0, -.04f, side * .20f), new Vector3(.29f, .06f, .06f), amber);
                Part(RepairDrone, "Small attitude nozzle " + side, new Vector3(-.19f, -.04f, side * .10f), new Vector3(.06f, .06f, .06f), frame, PrimitiveType.Sphere);
            }
        }
        private void OnDestroy()
        {
            foreach (var material in materials) if (material != null) Destroy(material);
            foreach (var mesh in meshes) if (mesh != null) Destroy(mesh);
        }
    }
}
