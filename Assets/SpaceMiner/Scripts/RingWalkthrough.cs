using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpaceMiner
{
    // Shared metre-scale geometry: isolated full expansion preview or the built campaign start.
    internal sealed class RingWalkthrough : IDisposable
    {
        internal const float InnerRadius = 29, OuterRadius = 38, StageDepth = 16, DeckHeight = 8;
        internal const float CentreRadius = (InnerRadius + OuterRadius) / 2, LiftRadius = InnerRadius - 2.5f;
        private const float RingJointHalfAngle = 2.2f, ModuleJointHalfLength = 1.1f;
        internal static readonly float[] LiftAngles = { 135, 255 };
        internal const int SegmentCount = 12, Layer = 27;
        private readonly bool startStation;
        private readonly int geometryLayer;
        internal readonly List<Transform> CampaignBerths = new List<Transform>();
        internal readonly List<Transform> WorkshopPortals = new List<Transform>();
        internal Transform CommandConsole { get; private set; }
        internal int CommandWindowCount { get; private set; }
        private readonly GameObject root;
        private readonly Transform[] levels = new Transform[2];
        private readonly List<GameObject> roofs = new List<GameObject>();
        private readonly List<Material> materials = new List<Material>();
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<Transform> movingDrones = new List<Transform>();
        private readonly List<float> droneAngles = new List<float>();
        private readonly List<Transform> liftPads = new List<Transform>();
        private Transform ridingPad;
        private readonly Material floor, wall, frame, cyan, amber, green, dark, white, hullA, hullB, glass;
        private readonly Font signFont;
        private readonly Material signMaterial;
        private readonly CharacterController walker;
        private readonly Camera camera;
        private readonly SpaceMinerUi ui = new SpaceMinerUi();
        private RenderTexture texture;
        private bool active, capture = true, map;
        private float yaw = 180, pitch, verticalSpeed, tripTime = -1;
        private Vector3 tripFrom, tripTo;
        private int cursorFrame, targetLevel;
        private readonly CursorLockMode previousLock;
        private readonly bool previousVisible;
        internal readonly PlayerVitals Vitals = new PlayerVitals();
        internal Vector3 Feet => walker.transform.position - root.transform.position;
        internal bool IsMap => map;
        internal bool Riding => tripTime >= 0;
        internal int Level => Mathf.Clamp(Mathf.RoundToInt(Feet.y / DeckHeight), 0, 1);
        internal int FloorSegments { get; private set; }
        internal int ParkedDrones { get; private set; }
        internal int ConnectorCount { get; private set; }
        internal int ModuleCount { get; private set; }
        internal int ModuleJoinCount { get; private set; }
        internal int LiftLandings => liftPads.Count;
        internal Texture Texture => texture;
        internal Transform Root => root.transform;
        private static readonly string[] Names = { "KOMMANDOZENTRALE", "DROHNENDECK", "ERZLAGER", "SOLARZUGANG", "METALLLAGER", "BAUTEILLAGER", "AUSBAURESERVE", "WASSERLAGER", "VERBUNDLAGER", "SOLARZUGANG", "ELEKTRONIKLAGER", "WOHNEN" };

        internal RingWalkthrough() : this(null) { }
        internal RingWalkthrough(Transform campaignParent)
        {
            startStation = campaignParent != null; geometryLayer = startStation ? 25 : Layer;
            previousLock = Cursor.lockState; previousVisible = Cursor.visible;
            root = new GameObject("Ring-Rundgang: begehbarer Groessenentwurf");
            root.layer = geometryLayer;
            if (startStation) { root.name = "Startstation / Ringmodell"; root.transform.SetParent(campaignParent, false); }
            else root.transform.position = new Vector3(0, -10000, 0);
            floor = Surface(new Color(.19f, .26f, .31f), .35f);
            wall = Surface(new Color(.09f, .14f, .19f), .5f);
            frame = Surface(new Color(.32f, .4f, .45f), .65f);
            dark = Surface(new Color(.025f, .055f, .075f), .2f);
            cyan = Surface(new Color(.12f, .73f, .85f), .1f, true);
            amber = Surface(new Color(.95f, .54f, .16f), .1f, true);
            green = Surface(new Color(.3f, .85f, .6f), .1f, true);
            white = Surface(new Color(.72f, .88f, .95f), .1f, true);
            hullA = Surface(new Color(.4f, .5f, .55f), .55f);
            hullB = Surface(new Color(.18f, .26f, .32f), .55f);
            glass = new Material(Resources.Load<Shader>("CargoGlass")) { name = "Bridge observation glass", color = new Color(.18f, .47f, .6f, .025f) };
            materials.Add(glass);
            signFont=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            signMaterial=new Material(Resources.Load<Shader>("RingWalkthroughText"));materials.Add(signMaterial);
            signMaterial.mainTexture=signFont.material.mainTexture;Font.textureRebuilt+=FontRebuilt;
            for (int level = 0; level < (startStation ? 1 : 2); level++) BuildLevel(level);
            if (startStation)
            {
                var fill = Child("Stations-Innenlicht", root.transform).gameObject.AddComponent<Light>();
                fill.type = LightType.Directional; fill.intensity = .8f; fill.cullingMask = 1 << geometryLayer;
                fill.transform.localRotation = Quaternion.Euler(55, -30, 0);
                active = true; return;
            }
            var player = Child("Rundgang-Spieler", root.transform);
            walker = player.gameObject.AddComponent<CharacterController>();
            walker.height = 1.75f; walker.radius = .3f; walker.center = Vector3.up * .875f;
            walker.skinWidth = .025f; walker.stepOffset = .2f; walker.minMoveDistance = 0;
            var cameraObject = Child("Rundgang-Kamera", root.transform);
            camera = cameraObject.gameObject.AddComponent<Camera>();
            camera.cullingMask = 1 << Layer; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.008f, .018f, .032f); camera.nearClipPlane = .08f;
            camera.farClipPlane = 250; camera.fieldOfView = 70; camera.depth = -31;
            camera.allowHDR = false;
            var lamp = Child("Rundgang-Kopflicht", camera.transform).gameObject.AddComponent<Light>();
            lamp.type = LightType.Point; lamp.color = new Color(.78f, .88f, 1); lamp.intensity = 2.5f;
            lamp.range = 28; lamp.cullingMask = 1 << Layer; lamp.shadows = LightShadows.None;
            var sun = Child("Rundgang-Fuelllicht", root.transform).gameObject.AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.1f; sun.color = new Color(.65f, .78f, .9f);
            sun.cullingMask = 1 << Layer; sun.transform.localRotation = Quaternion.Euler(55, -30, 0);
            Teleport(At(CentreRadius, 0) + Vector3.up * .04f, 90);
            ridingPad=Box("Fahrender Aufzugsboden",root.transform,Vector3.zero,new Vector3(2.6f,.1f,4.2f),cyan,0,false);
            ridingPad.gameObject.SetActive(false);
            SetActive(true);
            Tick(0, false);
        }

        private Material Surface(Color color, float metal, bool glow = false)
        {
            var m = new Material(Shader.Find("Standard")) { color = color };
            m.SetFloat("_Metallic", metal); m.SetFloat("_Glossiness", .25f);
            if (glow) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * .8f); }
            materials.Add(m); return m;
        }
        private Transform Child(string name, Transform parent)
        {
            var go = new GameObject(name); go.layer = geometryLayer; go.transform.SetParent(parent, false); return go.transform;
        }
        internal static Vector3 At(float radius, float angle, float height = 0)
            => new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * radius, height, Mathf.Cos(angle * Mathf.Deg2Rad) * radius);
        private Transform Box(string name, Transform parent, Vector3 position, Vector3 size, Material material, float angle = 0, bool collide = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.layer = geometryLayer;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = size;
            go.transform.localRotation = Quaternion.Euler(0, angle, 0); go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collide) go.GetComponent<Collider>().enabled = false;
            return go.transform;
        }
        private Transform Solid(string name, Transform parent, float inside, float outside, float begin, float end, float bottom, float top, Material material, bool collide = true)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                int n = vertices.Count; vertices.AddRange(new[] { a, b, c, d });
                triangles.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
            }
            int steps = Mathf.Max(1, Mathf.CeilToInt((end - begin) / 2));
            for (int j = 0; j < steps; j++)
            {
                float a = Mathf.Lerp(begin, end, (float)j / steps), b = Mathf.Lerp(begin, end, (float)(j + 1) / steps);
                var ia = At(inside, a, top); var oa = At(outside, a, top); var ob = At(outside, b, top); var ib = At(inside, b, top);
                var shift = Vector3.up * (bottom - top);
                Quad(ia, oa, ob, ib); Quad(ib + shift, ob + shift, oa + shift, ia + shift);
                Quad(ia + shift, oa + shift, oa, ia); Quad(oa + shift, ob + shift, ob, oa);
                Quad(ob + shift, ib + shift, ib, ob); Quad(ib + shift, ia + shift, ia, ib);
            }
            var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); meshes.Add(mesh);
            var node = Child(name, parent); node.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            node.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (collide) node.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
            return node;
        }
        private void Sign(string text, Transform parent, Vector3 pos, float angle, float scale = .12f)
        {
            var node = Child(text, parent); node.localPosition = pos; node.localRotation = Quaternion.Euler(0, angle, 0);
            var mesh = node.gameObject.AddComponent<TextMesh>();mesh.font=signFont;mesh.fontSize = 64;mesh.text = text;
            node.GetComponent<MeshRenderer>().sharedMaterial=signMaterial;
            mesh.characterSize = scale; mesh.anchor = TextAnchor.MiddleCenter; mesh.alignment = TextAlignment.Center;
            mesh.color = new Color(.7f, .94f, 1);
        }
        private void FontRebuilt(Font font){if(font==signFont&&signMaterial!=null)signMaterial.mainTexture=signFont.material.mainTexture;}
        private void BuildLevel(int level)
        {
            var t = Child("Ring " + (level + 1), root.transform); t.localPosition = Vector3.up * (level * DeckHeight); levels[level] = t;
            for (int s = 0; s < SegmentCount; s++)
            {
                float a = s * 30, begin = a - 15, end = a + 15;
                var shell = s % 2 == 0 ? hullA : hullB;
                Solid("Ringsegment " + (s + 1), t, InnerRadius, OuterRadius, begin + RingJointHalfAngle, end - RingJointHalfAngle, -.55f, 0, floor); FloorSegments++;
                // A solid flush joint, not a hole through the floor.
                Solid("Verbindungsfuge " + (s + 1), t, InnerRadius, OuterRadius, begin - RingJointHalfAngle, begin + RingJointHalfAngle, -.35f, 0, dark); ConnectorCount++;
                Solid("Fugenlinie", t, InnerRadius, OuterRadius, begin - .07f, begin + .07f, .001f, .008f, cyan, false);
                foreach (float r in new[] { InnerRadius + .15f, OuterRadius - .15f })
                    if (!(r < CentreRadius && LiftBoundary(begin)))
                    {
                        float inset = r < CentreRadius ? InnerRadius + .35f : OuterRadius - .35f;
                        Solid("Eingezogene Ringkupplung", t, inset - .1f, inset + .1f, begin - RingJointHalfAngle, begin + RingJointHalfAngle, 0, 5.3f, dark);
                        foreach (float edge in new[] { begin - RingJointHalfAngle, begin + RingJointHalfAngle })
                            Box("Kupplungsrahmen", t, At(r, edge, 2.75f), new Vector3(.3f, 5.5f, .5f), frame, edge);
                    }
                var roof = Solid("Ringdecke", t, InnerRadius, OuterRadius, begin + RingJointHalfAngle, end - RingJointHalfAngle, 5.5f, 5.75f, shell); roofs.Add(roof.gameObject);
                var linkRoof = Solid("Ringkupplung Decke", t, InnerRadius + .25f, OuterRadius - .25f, begin - RingJointHalfAngle, begin + RingJointHalfAngle, 5.15f, 5.35f, dark); roofs.Add(linkRoof.gameObject);
                foreach (float edge in new[] { begin - RingJointHalfAngle, begin + RingJointHalfAngle })
                {
                    var rim = Solid("Ringkupplung Dachkragen", t, InnerRadius - .15f, OuterRadius + .15f, edge - .18f, edge + .18f, 5.45f, 5.9f, frame); roofs.Add(rim.gameObject);
                    Box("Ringuebergang Licht", t, At(CentreRadius, edge, 5.15f), new Vector3(.14f, .12f, 8.4f), white, edge, false);
                }
                // Distinct pedestrian lane and two full-size drone lanes, no raised curbs across doors.
                foreach (float r in new[] { InnerRadius + .15f, InnerRadius + 2, InnerRadius + 2.5f, InnerRadius + 5.5f, OuterRadius - .5f })
                    Solid("Bodenmarkierung", t, r, r + .05f, begin + .3f, end - .3f, .01f, .018f, r <= InnerRadius + 2 ? amber : cyan, false);
                foreach (float r in new[] { InnerRadius, OuterRadius })
                {
                    bool opening = r == OuterRadius ? OuterStages(s) > 0 : HasInnerArea(s);
                    if (opening)
                    {
                        float door = s == 1 && r == OuterRadius ? 10 : 6;
                        // Lift portals straddle a segment boundary, separate from the room doors.
                        float wallBegin = begin + (r == InnerRadius && LiftBoundary(begin) ? 3.5f : RingJointHalfAngle);
                        float wallEnd = end - (r == InnerRadius && LiftBoundary(end) ? 3.5f : RingJointHalfAngle);
                        Solid("Ringwand links", t, r - .1f, r + .1f, wallBegin, a - door, 0, 5.5f, shell);
                        Solid("Ringwand rechts", t, r - .1f, r + .1f, a + door, wallEnd, 0, 5.5f, shell);
                        Solid("Portalsturz", t, r - .1f, r + .1f, a - door, a + door, 3.1f, 5.5f, frame);
                        Sign(r == OuterRadius ? Names[s] : InnerName(s), t, At(r + (r == OuterRadius ? -.5f : .18f), a, 3.6f), r == OuterRadius ? a : a + 180, .09f);
                    }
                    else
                    {
                        Solid("Geschlossener Ausbauanschluss", t, r - .1f, r + .1f, begin + RingJointHalfAngle, end - RingJointHalfAngle, 0, 5.5f, shell);
                        if (startStation) Sign("AUSBAUANSCHLUSS", t, At(r + (r == OuterRadius ? -.2f : .2f), a, 2.5f), r == OuterRadius ? a : a + 180, .065f);
                    }
                }
                Sign("S" + (s + 1).ToString("00") + "   /   RING " + (level + 1), t, At(CentreRadius, a, 4.8f), a + 90, .1f);
                if (OuterStages(s) > 0) BuildOuter(t, s, level);
                if (HasInnerArea(s)) BuildInner(t, s, level);
                // Light strips sit above people and loads.
                Solid("Deckenlicht", t, InnerRadius + 3.9f, InnerRadius + 4.1f, a - 8, a + 8, 5.42f, 5.45f, white, false);
            }
            if (!startStation) foreach (float angle in LiftAngles) BuildLift(t, angle, level);
            else foreach (float angle in LiftAngles) Sign("AUFZUGANSCHLUSS\nSPAETERER RING", t, At(InnerRadius + .2f, angle, 3.5f), angle + 180, .045f);
            if (startStation) return;
            for (int i = 0; i < 6; i++)
            {
                float a = 70 + i * 50;
                var drone = Drone(t, "Verkehrsdrohne", Vector3.zero, cyan, false); movingDrones.Add(drone);
                droneAngles.Add(a);
            }
        }
        internal int OuterStages(int s) => startStation ? s == 0 ? 3 : s == 1 ? 2 : s == 3 || s == 7 || s == 9 || s == 11 ? 1 : 0
            : s == 0 ? 3 : s == 1 ? 2 : s == 11 || s == 3 || s == 9 || s == 6 ? 1 : 3;
        private bool HasInnerArea(int s) => startStation ? s == 1 || s == 11 : s >= 0 && s < SegmentCount;
        private bool LiftBoundary(float angle) => !startStation && Array.Exists(LiftAngles, value => Mathf.Abs(Mathf.DeltaAngle(value, angle)) < .01f);
        private static string InnerName(int s) => s == 1 ? "INTERNE DROHNENWERKSTATT" : s == 11 ? "VERSORGUNG" : s==0||s==6 ? "PRODUKTION / TRENNUNG" : s==3||s==9 ? "FERTIGUNG" : "WARTEBUCHT";
        private Transform Room(Transform parent, string name, float inside, float outside, float angle, float half, Material surface)
        {
            Solid(name + " Boden", parent, inside, outside, angle - half, angle + half, -.35f, 0, surface);
            bool inner = outside <= InnerRadius + .01f;
            int stages = inner ? 1 : Mathf.RoundToInt((outside - inside) / StageDepth);
            for (int stage = 0; stage < stages; stage++)
            {
                float from = inner ? inside : inside + stage * StageDepth;
                float to = inner ? outside : inside + (stage + 1) * StageDepth;
                float bodyFrom = from + (!inner || stage > 0 ? ModuleJointHalfLength : 0);
                float bodyTo = to - (inner || stage < stages - 1 ? ModuleJointHalfLength : 0);
                var shell = (stage + Mathf.RoundToInt(angle / 30)) % 2 == 0 ? hullA : hullB;
                bool exteriorDock = name == Names[1] && stage == 1;
                bool roofed = !exteriorDock;
                if (roofed)
                {
                    var ceiling = Solid(name + " Decke / Modul " + (stage + 1), parent, bodyFrom, bodyTo, angle - half, angle + half, 5.5f, 5.75f, shell); roofs.Add(ceiling.gameObject);
                    // Separate raised edge strips outline each module in exterior views.
                    foreach (float radius in new[] { bodyFrom + .15f, bodyTo - .15f })
                    {
                        var rim = Solid("Modul Dachrand", parent, radius - .12f, radius + .12f, angle - half, angle + half, 5.75f, 5.9f, frame, false); roofs.Add(rim.gameObject);
                    }
                }
                foreach (float side in new[] { angle - half, angle + half })
                {
                    if (name == Names[0] && stage == 2)
                        BuildSideWindows(parent, bodyFrom, bodyTo, side, shell);
                    else
                        Box(name + " Seitenwand / Modul " + (stage + 1), parent, At((bodyFrom + bodyTo) / 2, side, exteriorDock ? .5f : 2.75f), new Vector3(.3f, exteriorDock ? 1 : 5.5f, bodyTo - bodyFrom), shell, side);
                }
                ModuleCount++;
                if (exteriorDock) BuildWorkshopFront(parent, from, angle, half);
                else if (!inner)
                    BuildModuleJoin(parent, name, from, angle, half, stage == 0 ? (name == Names[1] ? 10 : 6) : Mathf.Asin(6 / from) * Mathf.Rad2Deg, stage == 0 ? 3.1f : 4.3f);
                else BuildModuleJoin(parent, name, to, angle, half, 6, 3.1f);
            }
            Transform closingWall;
            if (name == Names[0]) closingWall = BuildFrontWindows(parent, outside, angle, half);
            else closingWall=Solid(name + " Abschlusswand", parent, outside - .18f, outside, angle - half, angle + half, 0, name == Names[1] ? 1 : 5.5f, wall);
            Solid(name + " Licht", parent, inside + 1, inside + 1.15f, angle - half + 1, angle + half - 1, 5.4f, 5.44f, cyan, false);
            return closingWall;
        }
        private void BuildSideWindows(Transform parent, float from, float to, float side, Material shell)
        {
            float centre = (from + to) / 2, length = to - from;
            Box("Brueckenfenster Bruestung", parent, At(centre, side, .525f), new Vector3(.3f, 1.05f, length), shell, side);
            Box("Brueckenfenster Sturz", parent, At(centre, side, 4.9f), new Vector3(.3f, 1.2f, length), shell, side);
            var pane = Box("Brueckenfenster seitlich", parent, At(centre, side, 2.675f), new Vector3(.07f, 3.25f, length), glass, side);
            pane.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; CommandWindowCount++;
            for (int i = 0; i <= 3; i++)
                Box("Fensterpfosten", parent, At(Mathf.Lerp(from, to, i / 3f), side, 2.675f), new Vector3(.36f, 3.4f, .18f), frame, side);
        }
        private Transform BuildFrontWindows(Transform parent, float radius, float angle, float half)
        {
            var sill = Solid("Brueckenfenster Frontbruestung", parent, radius - .18f, radius, angle - half, angle + half, 0, 1.05f, hullA);
            Solid("Brueckenfenster Frontsturz", parent, radius - .18f, radius, angle - half, angle + half, 4.3f, 5.5f, frame);
            for (int i = 0; i < 6; i++)
            {
                float begin = angle - half + i * half / 3, end = begin + half / 3;
                var pane = Solid("Brueckenfenster Front", parent, radius - .1f, radius - .05f, begin, end, 1.05f, 4.3f, glass);
                pane.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; CommandWindowCount++;
                Box("Frontfensterpfosten", parent, At(radius - .1f, begin, 2.675f), new Vector3(.18f, 3.4f, .36f), frame, begin);
            }
            return sill;
        }
        private void BuildWorkshopFront(Transform parent, float radius, float angle, float half)
        {
            var front = Child("Werkstatthalle / Drei Aussenschleusen", parent); ModuleJoinCount++;
            float previous = angle - half;
            for (int i = -1; i <= 1; i++)
            {
                float heading = angle + i * 6.5f, begin = heading - 1.7f, end = heading + 1.7f;
                Solid("Hallenfront geschlossen", front, radius - .18f, radius + .18f, previous, begin, 0, 5.5f, hullB);
                Solid("Schleusensturz", front, radius - .3f, radius + .3f, begin, end, 3.4f, 5.5f, frame);
                var portal = Child("Aussenschleuse " + (i + 2), front); portal.localPosition = At(radius, heading); portal.localRotation = Quaternion.Euler(0, heading, 0);
                WorkshopPortals.Add(portal);
                foreach (float side in new[] { -1.65f, 1.65f })
                {
                    Box("Schleusenrahmen", portal, new Vector3(side, 1.7f, 0), new Vector3(.18f, 3.4f, .65f), frame);
                    Box("Schleusenstatus", portal, new Vector3(side, 2.5f, -.38f), new Vector3(.08f, .6f, .08f), i == 0 ? green : amber, 0, false);
                }
                Solid("Schleusenschwelle", front, radius - 1.1f, radius + 1.1f, begin, end, .001f, .015f, frame, false);
                Sign("SCHLEUSE " + (i + 2).ToString("00") + (i == 0 ? " / AKTIV" : " / RESERVE OFFEN"), portal, new Vector3(0, 3.75f, -.4f), 0, .035f);
                Sign("WERKSTATT " + (i + 2).ToString("00"), portal, new Vector3(0, 3.75f, .4f), 180, .045f);
                previous = end;
            }
            Solid("Hallenfront geschlossen", front, radius - .18f, radius + .18f, previous, angle + half, 0, 5.5f, hullB);
            var canopy = Solid("Hallenaussenfront Dach", front, radius - 1.1f, radius + .4f, angle - half, angle + half, 5.5f, 5.75f, hullB); roofs.Add(canopy.gameObject);
        }
        private void BuildModuleJoin(Transform parent, string name, float radius, float angle, float half, float door, float height)
        {
            var join = Child("Modulkupplung / " + name + " / " + radius, parent); ModuleJoinCount++;
            // Recessed neck and full-height shoulders make a real break in the outer shell.
            Solid("Kupplung Schulter links", join, radius - ModuleJointHalfLength, radius + ModuleJointHalfLength, angle - half, angle - door, 0, height, dark);
            Solid("Kupplung Schulter rechts", join, radius - ModuleJointHalfLength, radius + ModuleJointHalfLength, angle + door, angle + half, 0, height, dark);
            var roof = Solid("Kupplung Decke", join, radius - ModuleJointHalfLength, radius + ModuleJointHalfLength, angle - half, angle + half, height, height + .25f, dark); roofs.Add(roof.gameObject);
            Solid("Modul Bodenschwelle", join, radius - ModuleJointHalfLength, radius + ModuleJointHalfLength, angle - door, angle + door, .001f, .015f, frame, false);
            foreach (float edge in new[] { radius - ModuleJointHalfLength, radius + ModuleJointHalfLength })
            {
                Solid("Modul Stirnblende", join, edge - .08f, edge + .08f, angle - half, angle + half, height + .25f, 5.5f, frame);
                var rim = Solid("Kupplung Portalsturz", join, edge - .12f, edge + .12f, angle - door, angle + door, height - .18f, height + .25f, frame); roofs.Add(rim.gameObject);
                Solid("Schwellenlicht", join, edge - .045f, edge + .045f, angle - door, angle + door, .016f, .023f, cyan, false);
                foreach (float side in new[] { angle - door, angle + door })
                    Box("Kupplung Portalpfosten", join, At(edge, side, height / 2), new Vector3(.28f, height, .3f), frame, side);
            }
            Sign("MODULUEBERGANG", join, At(radius, angle, height + .52f), angle, .045f);
        }
        private void BuildOuter(Transform parent, int sector, int level)
        {
            float angle = sector * 30;
            int stages = OuterStages(sector);
            float end = OuterRadius + stages * StageDepth;
            Room(parent, Names[sector], OuterRadius, end, angle, 13.5f, floor);
            var mount = Child("ModelMount / " + Names[sector], parent); mount.localPosition = At((OuterRadius + end) / 2, angle);
            if (sector == 0)
            {
                BuildCommandConsole(parent, angle, level);
                for (int j = -1; j <= 1; j += 2) Box("Konsole", parent, At(OuterRadius + StageDepth * 1.375f, angle, .6f) + Vector3.right * j * 6, new Vector3(1.5f, 1.2f, 3), frame);
            }
            else if (sector == 11)
            {
                for (int j = -1; j <= 1; j += 2)
                    Box("Schlafkoje", parent, At(OuterRadius + StageDepth * .625f, angle, .5f) + Quaternion.Euler(0, angle, 0) * Vector3.right * j * 4, new Vector3(2, 1, 3), frame, angle);
                Sign(startStation ? "WOHNEN" : "WOHNEN\nGROESSENPROBE", parent, At(end - .4f, angle, 2.6f), angle);
            }
            else if (sector == 1) BuildDock(parent, angle);
            else if (sector == 3 || sector == 9)
            {
                Sign("SOLAR / WARTUNGSZUGANG", parent, At(end - 1, angle, 3), angle, .1f);
                if (!startStation) Box("Solarmodell", parent, At(end + 4, angle, 1.8f), new Vector3(12, .25f, 7), cyan, angle);
            }
            else if (sector != 6 && !startStation)
            {
                for (int j = 0; j < stages; j++)
                {
                    float radius = OuterRadius + StageDepth * (j + .625f);
                    for (int side = -1; side <= 1; side += 2)
                        Box("Produktbehaelter", parent, At(radius, angle, 1.3f) + Quaternion.Euler(0, angle, 0) * Vector3.right * side * (radius * .16f), new Vector3(2.2f, 2.6f, 3), frame, angle);
                }
            }
        }
        private void BuildDock(Transform parent, float angle)
        {
            // Keep a central walk-through lane across the repair area.
            for (int side = -1; side <= 1; side += 2)
                Box("Reparaturbank", parent, At(OuterRadius + StageDepth * .625f, angle, .7f) + Quaternion.Euler(0, angle, 0) * Vector3.right * side * 5, new Vector3(2, 1.4f, 3), frame, angle);
            for (int row = 0; row < 2; row++)
                for (int i = -2; i <= 2; i++)
                {
                    // Stagger the upper row inward: lower drones can launch upward without crossing it.
                    var pos = At(row == 0 ? 66.5f : 63, angle, row == 0 ? 1 : 4.3f) + Quaternion.Euler(0, angle, 0) * Vector3.right * i * 3.1f;
                    if (startStation)
                    {
                        var berth = Child("Drohnenplatz " + CampaignBerths.Count, parent); berth.localPosition = pos;
                        berth.localRotation = Quaternion.Euler(0, angle, 0); CampaignBerths.Add(berth);
                    }
                    else { Drone(parent, "Parkdrohne " + (row * 5 + i + 3), pos, amber, true); ParkedDrones++; }
                    Box("Dockaufnahme", parent, pos + Vector3.down * .85f, new Vector3(2.4f, .15f, 2.4f), dark, angle, false);
                    var bay = Child("Aussen-Ladebucht " + (row * 5 + i + 3).ToString("00"), parent); bay.localPosition = pos; bay.localRotation = Quaternion.Euler(0, angle, 0);
                    Box("Ladebucht Sockel", bay, new Vector3(0, -.78f, 0), new Vector3(2.65f, .2f, 2.8f), frame);
                    foreach (float side in new[] { -1.3f, 1.3f })
                        Box("Buchtfuehrung", bay, new Vector3(side, -.5f, 0), new Vector3(.1f, .35f, 2.8f), amber);
                    Box("Ladesaeule", bay, new Vector3(0, -.1f, -1.65f), new Vector3(.45f, 1.35f, .4f), dark);
                    Box("Ladekontakt", bay, new Vector3(0, -.05f, -1.38f), new Vector3(.25f, .4f, .16f), cyan, 0, false);
                    Box("Ladezustand", bay, new Vector3(.3f, .5f, -1.65f), new Vector3(.13f, .35f, .15f), row == 0 && i == -2 ? green : amber, 0, false);
                    if (row == 1) foreach (float side in new[] { -1.2f, 1.2f })
                        Box("Docktraeger", bay, new Vector3(side, -2.55f, -.9f), new Vector3(.18f, 3.5f, .22f), frame);
                    Sign("D" + (row * 5 + i + 3).ToString("00"), bay, new Vector3(0, -.65f, 1.44f), 180, .045f);
                }
            Box("Aussendock Schild", parent, At(54.4f, angle, 4.8f), new Vector3(8, 1, .1f), dark, angle, false);
            Sign("AUSSENDOCK / 10 PLAETZE\n5 OBEN + 5 UNTEN", parent, At(54.47f, angle, 4.8f), angle + 180, .035f);
        }
        private void BuildCommandConsole(Transform parent, float angle, int level)
        {
            var desk = Child("Stationspult / V1", parent); desk.localPosition = At(72, angle); desk.localRotation = Quaternion.Euler(0, angle, 0);
            if (level == 0) CommandConsole = desk;
            foreach (float side in new[] { -2.1f, 2.1f })
                Box("Pultfuss", desk, new Vector3(side, .48f, 0), new Vector3(.65f, .96f, 1.35f), dark);
            Box("Pultplatte", desk, new Vector3(0, 1.02f, 0), new Vector3(5.8f, .16f, 1.8f), frame);
            Box("Pultkante", desk, new Vector3(0, .93f, -.92f), new Vector3(5.8f, .25f, .15f), dark);
            Box("Pultleiste", desk, new Vector3(0, 1.08f, -.99f), new Vector3(5.3f, .025f, .03f), cyan, 0, false);
            string[] titles = { "SCANNER", "STATION / MIRA", "DROHNEN" };
            for (int i = -1; i <= 1; i++)
            {
                var monitor = Child("Bedienmonitor / " + titles[i + 1], desk); monitor.localPosition = new Vector3(i * 1.8f, 1.42f, .32f); monitor.localRotation = Quaternion.Euler(28, 0, 0);
                Box("Monitorrahmen", monitor, Vector3.zero, new Vector3(1.62f, .82f, .13f), dark);
                Box("Display", monitor, new Vector3(0, 0, -.075f), new Vector3(1.46f, .65f, .02f), wall, 0, false);
                Sign(titles[i + 1], monitor, new Vector3(0, .21f, -.094f), 0, .025f);
                for (int line = 0; line < 3; line++)
                    Box("Displayzeile", monitor, new Vector3(-.12f, .06f - line * .13f, -.096f), new Vector3(line == 0 ? 1.1f : .7f, .025f, .008f), cyan, 0, false);
                for (int key = 0; key < 4; key++)
                    Box("Bedientaste", desk, new Vector3(i * 1.8f - .45f + key * .3f, 1.115f, -.55f), new Vector3(.18f, .035f, .2f), key == 0 ? cyan : dark, 0, false);
            }
            Sign("STATIONSPULT", desk, new Vector3(0, .86f, -1.01f), 0, .022f);
            Sign(level == 0 ? "KOMMANDOZENTRALE" : "LEITSTELLEN-RESERVE", parent, At(83, angle, 4.7f), angle, .1f);
        }
        private void BuildLift(Transform parent, float angle, int level)
        {
            var shaft = Child("Aufzugsschacht / " + angle, parent);
            shaft.localPosition = At(LiftRadius, angle); shaft.localRotation = Quaternion.Euler(0, angle, 0);
            Box("Aufzugsvorplatz", shaft, new Vector3(0, -.15f, 2.35f), new Vector3(3.2f, .3f, 1.1f), floor);
            foreach (float side in new[] { -1f, 1f })
                Box("Schachtwand", shaft, new Vector3(side * 1.5f, DeckHeight / 2, 0), new Vector3(.2f, DeckHeight, 4.8f), wall);
            Box("Schacht Rueckwand", shaft, new Vector3(0, DeckHeight / 2, -2.35f), new Vector3(3.2f, DeckHeight, .2f), wall);
            Box("Aufzug Portalsturz", shaft, new Vector3(0, 4.3f, 2.5f), new Vector3(3.4f, 2.4f, .2f), frame);
            liftPads.Add(Box("Aufzugsplattform", shaft, Vector3.zero, new Vector3(2.6f, .06f, 4.2f), floor));
            Box("Aufzug Statuslicht", shaft, new Vector3(1.4f, 1.5f, 2.5f), new Vector3(.08f, .6f, .08f), cyan, 0, false);
            Sign("AUFZUG / E", parent, At(InnerRadius + .2f, angle, 3.7f), angle + 180, .065f);
            Sign("RING " + (level + 1), shaft, new Vector3(0, 2.4f, -2.2f), 180, .08f);
        }
        private void BuildInner(Transform parent, int sector, int level)
        {
            float angle = sector * 30;
            bool facility = sector == 1 || sector == 11;
            bool production=sector==0||sector==3||sector==6||sector==9;
            float inside = InnerRadius - (facility || production ? 20 : 10);
            // Four neighboring rooms leave narrow service gaps for the two discrete lift shafts.
            float half = sector == 4 || sector == 5 || sector == 8 || sector == 9 ? 10.5f : 12;
            string roomName=InnerName(sector)+" - S"+(sector+1);
            var closingWall=Room(parent, roomName, inside, InnerRadius, angle, half, floor);
            closingWall.gameObject.SetActive(false);Object.Destroy(closingWall.gameObject);
            Solid("Innenraum Rueckwand", parent, inside, inside + .2f, angle - half, angle + half, 0, 5.5f, wall);
            if (facility||production)
            {
                var mount = Child("ModelMount / " + InnerName(sector), parent); mount.localPosition = At((inside + InnerRadius) / 2, angle);
                Box(facility?"Defekte Anlage":"Produktionsplatzhalter", parent, At(inside + 2.4f, angle, .8f), new Vector3(2.3f, 1.6f, 2), dark, angle);
                string label = sector == 1 ? "DROHNEN-\nWERKSTATT" : sector == 11 ? "VERSORGUNG" : "PRODUKTION";
                Sign(label + (facility ? startStation ? "\nDEFEKT / ZUGANG OFFEN" : "\nDEFEKT / TEST OFFEN" : "\nTEST OFFEN"), parent, At(inside + .4f, angle, 3), angle + 180, .045f);
                if(facility)foreach (float side in new[] { -1.5f, 1.5f }) Box("Beschaedigter Schleusenrahmen", parent, At(InnerRadius - .3f, angle, 1.4f) + Quaternion.Euler(0, angle, 0) * Vector3.right * side, new Vector3(.15f, 2.8f, .2f), amber, angle);
            }
            else
            {
                Sign("WARTEBUCHT\nDURCHGANG FREIHALTEN", parent, At(inside + .3f, angle, 2), angle + 180, .07f);
                Box("Buchtenmarkierung", parent, At(InnerRadius - 5, angle, .012f), new Vector3(3, .02f, 2.5f), green, angle, false);
            }
        }
        private Transform Drone(Transform parent, string name, Vector3 pos, Material signal, bool collide)
        {
            var t = Child(name, parent); t.localPosition = pos;
            Box("Drohnenhuelle / 2m", t, Vector3.zero, new Vector3(2, 1.2f, 2), frame, 0, collide);
            Box("Statuslicht", t, Vector3.up * .64f, new Vector3(1, .08f, 1), signal, 0, false);
            foreach (float side in new[] { -1f, 1f }) Box("Greifer", t, new Vector3(side * .75f, -.65f, .8f), new Vector3(.25f, .3f, .6f), dark, 0, collide);
            return t;
        }

        internal void SetActive(bool value)
        {
            active = value; if(root!=null)root.SetActive(value);
            if (!value) { Cursor.lockState = previousLock; Cursor.visible = previousVisible; }
            else cursorFrame = Time.frameCount;
        }
        internal void Teleport(Vector3 feet, float heading = 0, float lookPitch = 0)
        {
            walker.enabled = false; walker.transform.position = root.transform.position + feet;
            walker.enabled = true; yaw = heading; pitch = lookPitch; verticalSpeed = 0;
            Physics.SyncTransforms();
        }
        internal void Walk(Vector2 input, float seconds, bool sprint)
        {
            if (!active || map || Riding || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            float sprintTime = Vitals.Advance(seconds, sprint, input.sqrMagnitude > .001f);
            var direction = Quaternion.Euler(0, yaw, 0) * new Vector3(input.x, 0, input.y);
            if (walker.isGrounded) verticalSpeed = -2; else verticalSpeed = Mathf.Max(-10, verticalSpeed - 9.81f * seconds);
            walker.Move(Vector3.ClampMagnitude(direction, 1) * (seconds * PlayerVitals.WalkSpeed + sprintTime * (PlayerVitals.SprintSpeed - PlayerVitals.WalkSpeed)) + Vector3.up * verticalSpeed * seconds);
        }
        internal bool UseLift()
        {
            if (Riding || map) return false;
            foreach (float angle in LiftAngles)
            {
                Vector3 point = At(LiftRadius, angle, Level * DeckHeight);
                if (Vector3.Distance(Feet, point) > 2.4f) continue;
                targetLevel = 1 - Level; tripFrom = point + Vector3.up * .08f;
                tripTo = At(LiftRadius, angle, targetLevel * DeckHeight + .08f);
                foreach(var pad in liftPads)if(Vector2.Distance(new Vector2(pad.position.x-root.transform.position.x,pad.position.z-root.transform.position.z),new Vector2(point.x,point.z))<.2f)pad.gameObject.SetActive(false);
                ridingPad.gameObject.SetActive(true);ridingPad.rotation=Quaternion.Euler(0,angle,0);
                Teleport(tripFrom, angle); walker.enabled = false; tripTime = 0; return true;
            }
            return false;
        }
        internal void SetMap(bool value)
        {
            map = value;
            capture = !map;
            foreach (var roof in roofs) roof.SetActive(!map);
            for (int i = 0; i < levels.Length; i++) levels[i].gameObject.SetActive(!map || i == Level);
            cursorFrame = Time.frameCount;
        }
        internal void ToggleMouse()
        {
            if (map) return;
            capture = !capture; cursorFrame = Time.frameCount;
        }
        internal bool MovementEnabled => active && capture && !map && Application.isFocused && !Riding;
        internal bool LookEnabled => MovementEnabled && Time.frameCount > cursorFrame + 1;
        internal Vector2 LookAngles => new Vector2(yaw, pitch);
        internal void ProcessMovement(Vector2 movement, Vector2 mouse, bool sprint, float seconds)
        {
            if (!MovementEnabled) return;
            if (LookEnabled)
            {
                float sensitivity = SettingsStore.Current.Controls.Sensitivity * 2;
                yaw += mouse.x * sensitivity;
                pitch = Mathf.Clamp(pitch - mouse.y * sensitivity * (SettingsStore.Current.Controls.InvertY ? -1 : 1), -80, 80);
            }
            Walk(movement, seconds, sprint);
        }
        internal void Tick(float seconds, bool input)
        {
            if (!active) return;
            if (Riding)
            {
                tripTime += seconds;
                walker.transform.position = root.transform.position + Vector3.Lerp(tripFrom, tripTo, Mathf.SmoothStep(0, 1, tripTime / 3));
                ridingPad.position=walker.transform.position-Vector3.up*.08f;
                if (tripTime >= 3) { tripTime = -1; foreach(var pad in liftPads)pad.gameObject.SetActive(true);ridingPad.gameObject.SetActive(false);walker.enabled = true; verticalSpeed = 0; Physics.SyncTransforms(); }
            }
            if (input)
            {
                if (Input.GetKeyDown(KeyCode.Tab) && !Riding) ToggleMouse();
                if (Input.GetKeyDown(KeyCode.M) && !Riding) SetMap(!map);
                if (Input.GetKeyDown(KeyCode.E)) UseLift();
                ProcessMovement(new Vector2((PlayerInput.Held(CameraAction.Right) ? 1 : 0) - (PlayerInput.Held(CameraAction.Left) ? 1 : 0), (PlayerInput.Held(CameraAction.Forward) ? 1 : 0) - (PlayerInput.Held(CameraAction.Backward) ? 1 : 0)),
                    new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")), Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift), seconds);
            }
            for (int i = 0; i < movingDrones.Count; i++)
            {
                int level = i / 6, lane = i % 2; float direction = lane == 0 ? 1 : -1, radius = InnerRadius + (lane == 0 ? 4 : 7);
                var drone = movingDrones[i]; var next = At(radius, droneAngles[i] + direction * seconds * 2.2f, 1.3f);
                // Scale demo only: stop for people rather than pass through the walker.
                if (Vector3.Distance(next + Vector3.up * (level * DeckHeight), Feet + Vector3.up) > 3.5f) droneAngles[i] += direction * seconds * 2.2f;
                drone.localPosition = At(radius, droneAngles[i], 1.3f); drone.localRotation = Quaternion.Euler(0, droneAngles[i] + direction * 90, 0);
            }
        }
        internal void ApplyView(bool allowCapture)
        {
            if (!active) return;
            if (texture == null || texture.width != Screen.width || texture.height != Screen.height)
            {
                if (texture != null) { camera.targetTexture = null; texture.Release(); Object.Destroy(texture); }
                texture = new RenderTexture(Screen.width, Screen.height, 24) { antiAliasing = 2, name = "Begehbarer Ring" }; texture.Create(); camera.targetTexture = texture;
            }
            camera.orthographic = map;
            if (map)
            {
                camera.orthographicSize = (OuterRadius + 3 * StageDepth + 8) * 1.3f / Mathf.Min(1, camera.aspect);
                camera.transform.SetPositionAndRotation(root.transform.position + Vector3.up * (100 + Level * DeckHeight), Quaternion.Euler(90, 0, 0));
            }
            else camera.transform.SetPositionAndRotation(walker.transform.position + Vector3.up * 1.6f, Quaternion.Euler(pitch, yaw, 0));
            bool locked = allowCapture && capture && !map && Application.isFocused;
            if (Cursor.lockState != (locked ? CursorLockMode.Locked : CursorLockMode.None)) cursorFrame = Time.frameCount;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !locked;
        }
        internal bool ContainsInView(Vector3 localPoint)
        {
            var point = camera.WorldToViewportPoint(root.transform.position + localPoint);
            float bottom = map ? 126 * UiLayout.Scale() / Screen.height : 0;
            float top = map ? 1 - 106 * UiLayout.Scale() / Screen.height : 1;
            return point.z >= camera.nearClipPlane && point.z <= camera.farClipPlane && point.x > 0 && point.x < 1 && point.y > bottom && point.y < top;
        }
        internal string Area
        {
            get
            {
                Vector3 p = Feet; float r = new Vector2(p.x, p.z).magnitude;
                int sector = (Mathf.RoundToInt(Mathf.Atan2(p.x, p.z) * Mathf.Rad2Deg / 30) + 12) % 12;
                if (r < InnerRadius && Array.Exists(LiftAngles, a => Mathf.Abs(Mathf.DeltaAngle(a, Mathf.Atan2(p.x, p.z) * Mathf.Rad2Deg)) < 4)) return "AUFZUG";
                return r < InnerRadius ? InnerName(sector) : r > OuterRadius ? Names[sector] : "VERKEHRSRING / S" + (sector + 1).ToString("00");
            }
        }
        internal void Draw(Action leave, DisplayModeController display)
        {
            ui.Configure(SettingsStore.Current.Accessibility);
            float width = UiLayout.Width, height = UiLayout.Height;
            if (texture != null) GUI.DrawTexture(new Rect(0, 0, width, height), texture, ScaleMode.StretchToFill);
            SpaceMinerUi.Fill(new Rect(0, 0, width, 96), new Color(.015f, .035f, .05f, .94f));
            GUI.Label(new Rect(22, 12, width - 500, 38), "RING " + (Level + 1) + " // " + Area, ui.Heading);
            GUI.Label(new Rect(24, 53, width - 200, 33), "12 SEGMENTE / RINGBREITE 9 m / INNENRADIUS 29 m / AUSBAUSTUFE 16 m / ALLES OFFEN", ui.Small);
            if (ui.Tab(new Rect(width - 470, 15, 150, 34), map ? "Rundgang / M" : "Grundriss / M",false)) SetMap(!map);
            if (!map && ui.Tab(new Rect(width - 310, 15, 140, 34), capture ? "Maus frei / Tab" : "Umsehen / Tab",false)) ToggleMouse();
            if (ui.Tab(new Rect(width - 160, 15, 140, 34), "Planung / Esc",false)) leave();
            bool enabled = GUI.enabled; GUI.enabled = enabled && display != null && display.CanSwitch;
            if (ui.Tab(new Rect(width - 160, 54, 140, 30), display != null && display.IsFullscreen ? "Fenster / F11" : "Vollbild / F11", false)) display.Toggle();
            GUI.enabled = enabled;
            if (!map)
            {
                GUI.Label(new Rect(width / 2 - 8, height / 2 - 12, 24, 24), "+", ui.Text);
                ui.Panel(new Rect(18, height - 174, 276, 156));
                string[] labels = { "STAMINA", "MUEDIGKEIT", "HUNGER", "DURST", "GESUNDHEIT" };
                float[] values = { Vitals.Stamina, Vitals.Fatigue, Vitals.Hunger, Vitals.Thirst, Vitals.Health };
                for (int i = 0; i < labels.Length; i++)
                {
                    float y = height - 162 + i * 27;
                    GUI.Label(new Rect(30, y, 136, 24), labels[i], ui.Small);
                    SpaceMinerUi.Fill(new Rect(167, y + 5, 70, 12), new Color(.06f, .12f, .16f));
                    SpaceMinerUi.Fill(new Rect(167, y + 5, 70 * values[i] / 100, 12), i == 0 ? SpaceMinerUi.Amber : SpaceMinerUi.Cyan);
                    GUI.Label(new Rect(244, y, 47, 24), Mathf.RoundToInt(values[i]).ToString(), ui.Small);
                }
            }
            string lift = Riding ? "AUFZUG FAHRT ZU RING " + (targetLevel + 1) : NearLift() ? "E: AUFZUG ZU RING " + (2 - Level) : Area=="DROHNENDECK" ? "10 PARKPLAETZE (5 + 5) / STUFE 3: SPAETER 12" : "E: Aufzug am inneren Ringrand";
            float x = map ? 22 : 314;
            SpaceMinerUi.Fill(new Rect(x - 8, height - 116, width - x - 12, 100), new Color(.015f, .035f, .05f, .92f));
            GUI.Label(new Rect(x, height - 108, width - x - 20, 94), lift + "\n" + (!capture && !map ? "MAUS FREI / Tab: Steuerung fortsetzen" : "WASD: gehen / Shift: sprinten / Maus: umsehen") + "\nTab: Maus frei / M: Grundriss / Esc: Planung / F11: Vollbild", ui.Small);
        }
        private bool NearLift()
        {
            foreach (float angle in LiftAngles) if (Vector3.Distance(Feet, At(LiftRadius, angle, Level * DeckHeight)) < 2.4f) return true;
            return false;
        }
        public void Dispose()
        {
            Font.textureRebuilt-=FontRebuilt;
            if (startStation) { if (root != null) root.SetActive(false); } else SetActive(false);
            if(camera!=null)camera.targetTexture = null;
            if (texture != null) { texture.Release(); Object.Destroy(texture); }
            Object.Destroy(root); foreach (var m in meshes) Object.Destroy(m); foreach (var m in materials) Object.Destroy(m); ui.Dispose();
        }
    }
}
