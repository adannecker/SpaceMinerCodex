using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    // Presentation job only: no campaign resources, saves, damage or DroneAgent assignments.
    public sealed class MaintenanceDroneShowcase : MonoBehaviour
    {
        private enum Stage { Charging, Takeoff, Approach, Scan, Weld, Inspect, Return, Landing }
        private static readonly float[] Durations = { 3, 3, 4, 2, 9, 2, 4, 3 };
        private static readonly string[] Labels = { "Laden im Außendock", "Abheben / Freiflug", "Hülle anfliegen", "Schadstelle untersuchen", "Hüllenriss verschweißen", "Schweißnaht prüfen", "Zum Dock zurückfliegen", "Absenken / Andocken" };
        private const string Evidence = "Logs/MaintenanceShowcase";
        private Transform frame, body, rig, torch, nozzle, drill, drillRotor;
        private Transform scarRoot;
        private MaintenanceLaserVisual laserVisual;
        private MaintenanceLaserVisual cassetteVisual;
        private const float ToolAxisY = 1.30f;
        private float hullSide, toolAxisX;
        internal Vector3 ToolFocus
        {
            get
            {
                float extension=Mathf.InverseLerp(MaintenanceLaserVisual.StowedHead,MaintenanceLaserVisual.ExtendedHead,torch.localPosition.z);
                return frame.InverseTransformPoint(rig.TransformPoint(new Vector3(toolAxisX,ToolAxisY,Mathf.Lerp(.86f,1.84f,extension))));
            }
        }
        internal Quaternion ToolCameraRotation => Quaternion.Inverse(frame.rotation) * rig.rotation * Quaternion.Euler(0,180,0);
        internal bool IsChecking => checking;
        private readonly List<Material> materials = new List<Material>();
        private readonly Transform[] exhausts = new Transform[4];
        private Vector3 parkedFeet, wallPoint, liftFeet, workFeet, bodyAnchor;
        private Quaternion parkedRotation;
        private LineRenderer arc, arcHalo, seam, scanner;
        private Texture2D sparkTexture;
        private Light weldLight;
        private ParticleSystem sparks;
        private Material cyan, amber;
        private Stage stage;
        private float elapsed, charge = 100, progress, sparkRemainder, weldSeconds, drillAngle;
        private int jobs, checks;
        private bool paused, mining, ready;
        private readonly System.Random random = new System.Random(704);
        private bool checking;
        private string obstruction;
        private Vector3 Feet => frame.InverseTransformPoint(body.TransformPoint(bodyAnchor));
        private bool IsWelding => stage == Stage.Weld && !paused && !mining && ready;
        private bool Blocked => paused || SettingsMenu.PausesSimulation || StartMenu.IsOpen || SaveGameMenu.IsOpen;

        public void Initialise(Transform reference, Transform importedBody, float deckHeight)
        {
            frame = reference; body = importedBody;
            parkedRotation = body.localRotation; parkedFeet = Vector3.up * deckHeight;
            bodyAnchor = body.InverseTransformPoint(frame.TransformPoint(parkedFeet));
            liftFeet = new Vector3(0, 2.65f, .45f); workFeet = new Vector3(.45f, 1.25f, -1.2f);
            // Locate the real solid wall rather than placing a repair effect in empty space.
            Physics.SyncTransforms();
            if (!Physics.Raycast(frame.TransformPoint(new Vector3(0, 2.2f, 0)), -frame.forward, out var hit, 5, 1 << 25))
                throw new Exception("Maintenance showcase: no solid hull between airlocks");
            wallPoint = frame.InverseTransformPoint(hit.point) + Vector3.forward * .035f;
            if (Vector3.Dot(hit.normal, frame.forward) < .9f) throw new Exception("Unexpected maintenance wall orientation");
            BuildTools(); BuildRepairSite();
            ready = true; ApplyPose(parkedFeet); UpdateEffects(0);
            checking = Array.IndexOf(Environment.GetCommandLineArgs(), "-maintenanceShowcaseCheck") >= 0;
            if (checking) StartCoroutine(Validate());
        }

        private Material Surface(string name, Color color, bool glow = false)
        {
            var material = new Material(Shader.Find("Standard")) { name = name, color = color };
            material.SetFloat("_Metallic", glow ? .1f : .65f); material.SetFloat("_Glossiness", .42f);
            if (glow) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * 1.6f); }
            materials.Add(material); return material;
        }
        private Transform Part(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            // These small visual attachments must not leave primitive physics bodies behind.
            var collider = go.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go.transform;
        }
        private LineRenderer Line(string name, Color color, float width)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>(); line.transform.SetParent(frame, false);
            var material = new Material(Shader.Find("Sprites/Default")); materials.Add(material);
            line.sharedMaterial = material; line.useWorldSpace = false; line.positionCount = 2;
            line.startColor = line.endColor = color; line.widthMultiplier = width; return line;
        }
        private void BuildTools()
        {
            var steel = Surface("Modular tool / steel", new Color(.35f, .42f, .47f));
            var dark = Surface("Modular tool / graphite", new Color(.055f, .085f, .11f));
            cyan = Surface("Maneuvering thrusters / cyan", new Color(.12f, .75f, 1), true);
            amber = Surface("Tool socket / amber", new Color(1, .46f, .08f), true);
            rig = new GameObject("Interchangeable metric tool assembly").transform; rig.SetParent(body, false);
            // The FBX wrapper is uniformly scaled; compensate only for the added metre-size tools.
            rig.localScale = Vector3.one / body.localScale.x;
            rig.position = frame.TransformPoint(parkedFeet); rig.rotation = frame.rotation;
            // Sample the original mesh before adding attachments. A conservative surface envelope keeps
            // every moving tool part outside the hull; only the two bolted saddles touch the hull.
            DroneToolMount.FindMountSurface(body, rig, ToolAxisY, out hullSide, out float rearMount, out float frontMount);
            toolAxisX = hullSide + MaintenanceLaserVisual.CassetteHalfWidth + .07f;
            var cassette = new GameObject("Hull-mounted retractable tool cassette");
            cassette.transform.SetParent(rig,false); cassette.transform.localPosition = new Vector3(toolAxisX,ToolAxisY,0);
            cassetteVisual = cassette.AddComponent<MaintenanceLaserVisual>();
            cassetteVisual.BuildCassette(rearMount-toolAxisX,frontMount-toolAxisX);
            workFeet.x = toolAxisX; workFeet.y = 2.2f-ToolAxisY;
            Debug.Log("MAINTENANCE CASSETTE: hull side " + hullSide + ", tool axis " + toolAxisX + ", saddles " + rearMount + "/" + frontMount);
            torch = new GameObject("Replaceable welding head").transform; torch.SetParent(rig, false);
            torch.localRotation = Quaternion.Euler(0, 180, 0); // Tool mesh emits along -Z; drone front is +Z.
            laserVisual = torch.gameObject.AddComponent<MaintenanceLaserVisual>(); laserVisual.Build();
            Debug.Log("MAINTENANCE LASER: detailed independent head, " + laserVisual.TriangleCount + " triangles");
            nozzle = laserVisual.Aperture;
            drill = new GameObject("Replaceable mining drill head / preview").transform; drill.SetParent(rig, false);
            drill.localRotation = torch.localRotation;
            Part("Drill motor", PrimitiveType.Cylinder, drill, Vector3.zero, new Vector3(.25f, .18f, .25f), dark).localRotation = Quaternion.Euler(90, 0, 0);
            drillRotor = new GameObject("Independent rotating drill").transform; drillRotor.SetParent(drill, false);
            for (int i = 0; i < 4; i++)
            {
                var tooth = Part("Drill cutting tooth", PrimitiveType.Cube, drillRotor, new Vector3(0, 0, -.22f - i * .065f),
                    new Vector3(.26f - i * .045f, .065f, .1f), steel); tooth.localRotation = Quaternion.Euler(0, 0, i * 55);
            }
            for (int i = 0; i < 4; i++)
                exhausts[i] = Part("Flight thruster " + i, PrimitiveType.Sphere, rig,
                    new Vector3((i % 2 == 0 ? -1 : 1) * .68f, .1f, (i < 2 ? -1 : 1) * .65f), new Vector3(.065f, .22f, .065f), cyan);
            arc = Line("Short welding arc / bright core", new Color(.8f, .96f, 1), .0045f);
            arcHalo = Line("Short welding arc / soft corona", new Color(.15f, .65f, 1, .22f), .014f);
            weldLight = new GameObject("Weld contact illumination").AddComponent<Light>(); weldLight.transform.SetParent(frame, false);
            weldLight.type = LightType.Point; weldLight.color = new Color(.45f, .8f, 1); weldLight.range = 4;
            weldLight.shadows = LightShadows.None;
            sparks = new GameObject("Welding sparks / vacuum particles").AddComponent<ParticleSystem>(); sparks.transform.SetParent(frame, false);
            var main = sparks.main; main.loop = false; main.playOnAwake = false; main.maxParticles = 120;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.startLifetime = .45f; main.startSize = .025f;
            main.gravityModifier = 0; main.useUnscaledTime = true;
            var emission = sparks.emission; emission.enabled = false;
            var shape = sparks.shape; shape.enabled = false;
            var renderer = sparks.GetComponent<ParticleSystemRenderer>();
            sparkTexture = new Texture2D(32,32,TextureFormat.RGBA32,false) { name = "Soft welding spark" };
            var sparkPixels = new Color[32*32];
            for(int y=0;y<32;y++) for(int x=0;x<32;x++)
            {
                float r = new Vector2((x-15.5f)/15.5f,(y-15.5f)/15.5f).sqrMagnitude;
                sparkPixels[y*32+x] = new Color(1,1,1,Mathf.Exp(-r*5)*Mathf.Clamp01(1-r));
            }
            sparkTexture.SetPixels(sparkPixels); sparkTexture.Apply(false,true);
            var particleMaterial = new Material(Shader.Find("Sprites/Default")) { mainTexture = sparkTexture };
            materials.Add(particleMaterial); renderer.sharedMaterial = particleMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Stretch; renderer.lengthScale = 1.6f; renderer.velocityScale = .025f;
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        private void BuildRepairSite()
        {
            var plate = Surface("Hull service plate", new Color(.24f, .31f, .36f));
            Part("Repair site on actual station hull", PrimitiveType.Cube, frame, wallPoint - Vector3.forward * .012f, new Vector3(1.55f, 1.2f, .03f), plate);
            foreach (float side in new[] { -1f, 1f })
            {
                Part("Service plate side marker", PrimitiveType.Cube, frame, wallPoint + new Vector3(side * .8f, 0, .015f), new Vector3(.025f, 1.25f, .02f), amber);
                Part("Service plate edge", PrimitiveType.Cube, frame, wallPoint + new Vector3(0, side * .63f, .015f), new Vector3(1.6f, .025f, .02f), amber);
            }
            scarRoot = new GameObject("Open hull crack / cosmetic").transform; scarRoot.SetParent(frame, false);
            var damage = Surface("Dark hull crack", new Color(.015f, .02f, .025f));
            for (int i = 0; i < 9; i++)
            {
                var scar = Part("Crack segment", PrimitiveType.Cube, scarRoot, Contact(i / 8f), new Vector3(.15f, .045f, .012f), damage);
                scar.localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? 14 : -14);
            }
            seam = Line("Completed repair seam", new Color(.35f, .9f, 1), .022f);
            scanner = Line("Inspection beam", new Color(.15f, .7f, .85f, .7f), .008f);
        }
        private Vector3 Contact(float t) => wallPoint + new Vector3(Mathf.Lerp(-.52f, .52f, t), Mathf.Sin(t * Mathf.PI * 4) * .05f, .025f);
        private void ApplyPose(Vector3 feet, float yaw = 0)
        {
            // Rotate about the model's normalized bottom-centre, not its off-centre FBX pivot.
            body.localRotation = parkedRotation * Quaternion.Euler(0, yaw, 0);
            body.localPosition = feet - body.localRotation * Vector3.Scale(body.localScale, bodyAnchor);
        }
        private void PoseTools(float extension)
        {
            Vector3 head = new Vector3(toolAxisX,ToolAxisY,Mathf.Lerp(MaintenanceLaserVisual.StowedHead,MaintenanceLaserVisual.ExtendedHead,extension));
            cassetteVisual.PoseCassette(extension);
            torch.localPosition = drill.localPosition = head;
            torch.gameObject.SetActive(!mining); drill.gameObject.SetActive(mining);
            drillRotor.localRotation = Quaternion.Euler(0, 0, drillAngle);
        }
        private void Update()
        {
            if (!ready || checking) return;
            Tick(Time.unscaledDeltaTime);
        }
        private void Tick(float dt)
        {
            if (Blocked || mining)
            {
                UpdateEffects(Blocked ? 0 : dt); return;
            }
            float remaining = dt;
            while (remaining > 0)
            {
                float slice = Mathf.Min(remaining, Durations[(int)stage] - elapsed);
                elapsed += slice; remaining -= slice;
                if (stage == Stage.Weld) { progress = elapsed / Durations[(int)stage]; weldSeconds += slice; }
                charge = Mathf.Clamp(charge + (stage == Stage.Charging ? 5 : -.32f) * slice, 0, 100);
                if (elapsed < Durations[(int)stage]) break;
                elapsed = 0;
                if (stage == Stage.Weld) { progress = 1; scarRoot.gameObject.SetActive(false); jobs++; }
                stage = (Stage)(((int)stage + 1) % Durations.Length);
                if (stage == Stage.Takeoff) { progress = 0; scarRoot.gameObject.SetActive(true); }
            }
            PhasePose(stage, elapsed / Durations[(int)stage], out Vector3 feet, out float heading);
            // Hold safely when the imported body's conservative box encounters station structure.
            if (HullBlocked(feet, heading)) { paused = true; obstruction = "Flugweg blockiert – Showcase angehalten"; UpdateEffects(0); return; }
            ApplyPose(feet, heading); UpdateEffects(dt);
        }
        private void PhasePose(Stage phase, float phaseTime, out Vector3 feet, out float heading)
        {
            float t = Mathf.SmoothStep(0, 1, phaseTime);
            feet = parkedFeet; heading = 180;
            switch (phase)
            {
                case Stage.Charging: heading = 0; break;
                case Stage.Takeoff:
                    feet = Vector3.Lerp(parkedFeet, liftFeet, Mathf.SmoothStep(0, 1, phaseTime / .6f));
                    heading = 180 * Mathf.SmoothStep(0, 1, (phaseTime - .6f) / .4f); break;
                case Stage.Approach: feet = Vector3.Lerp(liftFeet, workFeet, t); break;
                case Stage.Scan: case Stage.Weld: case Stage.Inspect: feet = workFeet; break;
                case Stage.Return: feet = Vector3.Lerp(workFeet, liftFeet, t); break;
                case Stage.Landing:
                    // Back away from the hull, turn over the clear dock, then descend without spinning in the cradle.
                    heading = 180 * (1 - Mathf.SmoothStep(0, 1, phaseTime / .4f));
                    feet = Vector3.Lerp(liftFeet, parkedFeet, Mathf.SmoothStep(0, 1, (phaseTime - .4f) / .6f)); break;
            }
        }
        private bool HullBlocked(Vector3 feet, float heading)
        {
            var turn=Quaternion.Euler(0,heading,0);
            bool bodyBlocked=Physics.CheckBox(frame.TransformPoint(feet + new Vector3(0, .85f, 0)), new Vector3(1.03f, .83f, 1.13f),
                frame.rotation * turn, 1 << 25, QueryTriggerInteraction.Ignore);
            // Reserve even the fully extended tool envelope during the whole flight and both turns.
            return bodyBlocked || Physics.CheckBox(frame.TransformPoint(feet+turn*new Vector3(toolAxisX-.08f,ToolAxisY,1.035f)),
                new Vector3(.30f,.23f,1.195f),frame.rotation*turn,1<<25,QueryTriggerInteraction.Ignore);
        }
        private void UpdateEffects(float dt)
        {
            if (mining && !Blocked) drillAngle = Mathf.Repeat(drillAngle + dt * 240, 360);
            bool working = stage == Stage.Scan || stage == Stage.Weld || stage == Stage.Inspect;
            float extension = mining ? 1 : working ? 1 : stage == Stage.Approach ? Mathf.SmoothStep(0, 1, elapsed / 4)
                : stage == Stage.Return ? 1 - Mathf.SmoothStep(0, 1, elapsed) : 0;
            PoseTools(extension);
            bool active = IsWelding && !Blocked && dt > 0;
            laserVisual.SetWelding(active);
            Vector3 contact = Contact(progress);
            arc.enabled = active;
            arc.SetPosition(0, frame.InverseTransformPoint(nozzle.position)); arc.SetPosition(1, contact);
            arcHalo.enabled = active; arcHalo.SetPosition(0, arc.GetPosition(0)); arcHalo.SetPosition(1, contact);
            weldLight.enabled = active; weldLight.transform.localPosition = contact + Vector3.forward * .12f;
            weldLight.intensity = 1.8f + Mathf.Sin(elapsed * 73) * .35f;
            bool inspecting = !mining && !Blocked && (stage == Stage.Scan || stage == Stage.Inspect);
            scanner.enabled = inspecting; scanner.SetPosition(0, frame.InverseTransformPoint(nozzle.position));
            scanner.SetPosition(1, Contact(Mathf.PingPong(elapsed * .6f, 1)));
            seam.enabled = progress > .001f && !mining;
            seam.positionCount = 25;
            for (int i = 0; i < 25; i++) seam.SetPosition(i, Contact(progress * i / 24f));
            if (active)
            {
                sparkRemainder += dt * 85;
                int count = Mathf.Min(40, (int)sparkRemainder); sparkRemainder -= count;
                sparks.Play();
                for (int i = 0; i < count; i++)
                {
                    Vector3 direction = new Vector3((float)random.NextDouble() * 2 - 1, (float)random.NextDouble() * 2 - 1, .3f + (float)random.NextDouble()).normalized;
                    sparks.Emit(new ParticleSystem.EmitParams { position = frame.TransformPoint(contact), velocity = frame.TransformDirection(direction) * (1 + (float)random.NextDouble() * 2),
                        startColor = new Color(1, .5f + (float)random.NextDouble() * .45f, .12f), startLifetime = .25f + (float)random.NextDouble() * .3f, startSize = .018f + (float)random.NextDouble() * .02f }, 1);
                }
            }
            else { sparkRemainder = 0; sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }
            bool flying = !Blocked && !mining && stage != Stage.Charging;
            foreach (var exhaust in exhausts)
            {
                exhaust.gameObject.SetActive(flying);
                exhaust.localScale = new Vector3(.055f, .14f + .035f * Mathf.Sin(elapsed * 37), .055f);
            }
        }
        private void Restart()
        {
            stage = Stage.Charging; elapsed = progress = 0; paused = false; obstruction = null;
            scarRoot.gameObject.SetActive(true); ApplyPose(parkedFeet); UpdateEffects(0);
        }
        private void SetMining(bool value) { mining = value; Restart(); }
        internal void DrawControls(SpaceMinerUi ui)
        {
            GUILayout.Label(mining ? "MINING-UMBAU · WERKZEUGPROBE" : "DROHNE 01 · SHOWCASE", ui.Small);
            GUILayout.Label(obstruction ?? (mining ? "Gemeinsamer Rumpf mit Bohrerkopf" : Labels[(int)stage]), ui.Text);
            GUILayout.Label("Akku " + Mathf.RoundToInt(charge) + " %   ·   Naht " + Mathf.RoundToInt(progress * 100) + " %\nReparaturdurchläufe " + jobs, ui.Small);
            if (!mining && GUILayout.Button(paused ? "Ablauf fortsetzen" : "Ablauf pausieren", ui.Button)) { paused = !paused; UpdateEffects(0); }
            if (GUILayout.Button("Neu im Dock starten", ui.Button)) Restart();
            if (GUILayout.Button(mining ? "Schweißwerkzeug einsetzen" : "Mining-Bohrerkopf ansehen", ui.Button)) SetMining(!mining);
            GUILayout.Label(mining ? "Bohrer als separates bewegliches Anbauteil. Abbau, Greifer und Frachttransfer folgen später." :
                "Abheben → Hülle prüfen → schweißen → zurück zum Laden. Kleine Manövrierdüsen, Licht und Funken zeigen die Arbeit.", ui.Small);
            GUILayout.Label("Erster visueller Ablauf. Akku und Hüllenzustand gelten nur für diese Probe.", ui.Small);
            GUILayout.Space(15);
        }
        private void Require(bool condition, string label)
        {
            checks++; if (!condition) throw new Exception("Maintenance showcase: " + label);
            Debug.Log("MAINTENANCE OK: " + label);
        }
        private bool Guard(Action action)
        {
            try { action(); return true; } catch (Exception e) { Debug.LogException(e); Application.Quit(1); return false; }
        }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.GetFullPath(Evidence + "/" + name + ".png"));
            yield return new WaitForSecondsRealtime(.3f);
        }
        private IEnumerator Validate()
        {
            Directory.CreateDirectory(Evidence); yield return null;
            if (!Guard(() => {
                Require(SettingsStore.PathName.Contains("MaintenanceShowcase-settings"), "isolated settings");
                Require(wallPoint.z < -3.5f && wallPoint.z > -4.1f, "repair panel lies on actual exterior hull");
                for (int i = 0; i <= 40; i++)
                {
                    float t = i / 40f;
                    PhasePose(Stage.Takeoff, t, out Vector3 feet, out float heading);
                    Require(!HullBlocked(feet, heading), "clear conservative body box / takeoff and turn " + i);
                    PhasePose(Stage.Approach, t, out feet, out heading);
                    Require(!HullBlocked(feet, heading), "clear conservative body box / front-first approach " + i);
                    PhasePose(Stage.Landing, t, out feet, out heading);
                    Require(!HullBlocked(feet, heading), "clear conservative body box / dock turn and landing " + i);
                }
                Require(!arc.enabled && !weldLight.enabled && sparks.particleCount == 0, "no welding effects in dock");
                // Check transformed rendered vertices, not just the head pivot: the previous pivot check
                // passed even though the back of the head intersected the imported drone.
                for(int i=0;i<=10;i++)
                {
                    PoseTools(i/10f);
                    float rear=float.PositiveInfinity, side=float.PositiveInfinity, front=float.NegativeInfinity;
                    foreach(var filter in torch.GetComponentsInChildren<MeshFilter>()) foreach(var vertex in filter.sharedMesh.vertices)
                    {
                        Vector3 p=rig.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                        rear=Mathf.Min(rear,p.z); side=Mathf.Min(side,p.x); front=Mathf.Max(front,p.z);
                    }
                    Require(side>hullSide+.025f,"rendered laser clears original hull envelope / extension " + i);
                    Require(rear>MaintenanceLaserVisual.CassetteFront+.015f,"laser rear stays outside cassette mouth / extension " + i);
                    if(i==0) Require(front<1.665f && toolAxisX+.215f<1.325f,"stowed module remains inside cradle footprint");
                }
                PoseTools(0);
                Require(cassetteVisual.StowedSleevesContained(),"telescopic sleeve meshes fit inside cassette with only coupling tip exposed");
            })) yield break;
            yield return Capture("01-dock");
            var dockPreview=FindFirstObjectByType<TripoDockPreview>();
            dockPreview.SetToolCloseup(true); yield return Capture("09-stowed-cassette"); dockPreview.SetToolCloseup(false);
            bool takeoff = false, approach = false, weld = false, returning = false, landed = false;
            float timeout = 0;
            // Exercise the actual state machine continuously through a complete job and charge stop.
            while (timeout < 36)
            {
                float dt = Mathf.Min(Time.unscaledDeltaTime, .1f); Tick(dt); timeout += dt;
                if (paused) { Guard(() => Require(false, obstruction ?? "unexpected pause")); yield break; }
                if (!takeoff && stage == Stage.Takeoff && elapsed > 1.5f) { takeoff = true; yield return Capture("02-takeoff"); }
                if (!approach && stage == Stage.Scan) { approach = true; yield return Capture("03-scan"); }
                if (!weld && stage == Stage.Weld && elapsed > 4)
                {
                    weld = true;
                    if (!Guard(() => {
                        Require(arc.enabled && weldLight.enabled && sparks.particleCount > 0, "welding arc, contact light and sparks active");
                        Require(Vector3.Distance(arc.GetPosition(1), Contact(progress)) < .001f, "arc ends on hull seam");
                        Require(Vector3.Distance(arc.GetPosition(0), arc.GetPosition(1)) < 1.5f, "short reachable welding arc");
                        Require(Vector3.Dot(body.forward, -frame.forward) > .99f, "gripper and sensor front faces hull during repair");
                        Require(torch.localPosition.z > 1 && torch.localPosition.x > .3f, "laser is beside front gripper and sensors");
                        Require(Vector3.Dot(-nozzle.forward, body.forward) > .99f, "laser aperture points forward with drone");
                        paused = true; Vector3 before = body.localPosition; float previous = elapsed;
                        Tick(.5f); Require(body.localPosition == before && elapsed == previous, "pause freezes flight and job");
                        Require(!arc.enabled && !weldLight.enabled && sparks.particleCount == 0, "pause clears all welding effects");
                        paused = false; Tick(.12f);
                    })) yield break;
                    yield return Capture("04-welding");
                    var preview = FindFirstObjectByType<TripoDockPreview>();
                    preview.SetToolCloseup(true); yield return Capture("08-laser-detail"); preview.SetToolCloseup(false);
                }
                if (!returning && stage == Stage.Return) { returning = true; yield return Capture("05-completed-seam"); }
                if (jobs == 1 && stage == Stage.Charging)
                {
                    landed = true;
                    if (!Guard(() => {
                        Require(Vector3.Distance(Feet, parkedFeet) < .001f, "exact dock pose restored after full flight");
                        Require(Quaternion.Angle(body.localRotation, parkedRotation) < .01f, "original dock heading restored after airborne turn");
                        Require(progress == 1 && !scarRoot.gameObject.activeSelf, "completed seam replaces cosmetic crack");
                        Require(!arc.enabled && !weldLight.enabled && sparks.particleCount == 0, "return and charging have no stray welding effects");
                        float previous = charge; Tick(.3f); Require(charge > previous, "charging replenishes showcase battery");
                    })) yield break;
                    yield return Capture("06-returned"); break;
                }
                yield return null;
            }
            if (!Guard(() => {
                Require(takeoff && approach && weld && returning && landed, "full ordered maintenance job completed");
                Require(weldSeconds >= 9 && weldSeconds < 9.1f, "one complete timed welding pass");
                SetMining(true); Tick(.1f);
                Require(drill.gameObject.activeSelf && !torch.gameObject.activeSelf && !arc.enabled, "interchangeable mining head without welding");
                Require(Vector3.Distance(Feet, parkedFeet) < .001f, "tool swap resets safely to dock");
            })) yield break;
            yield return Capture("07-mining-tool");
            if (!Guard(() => {
                SetMining(false); Require(torch.gameObject.activeSelf && !drill.gameObject.activeSelf, "welding tool restored");
                File.WriteAllText(Evidence + "/result.json", "{\"checks\":" + checks + ",\"completedJobs\":" + jobs + ",\"pass\":true,\"campaignDamageModified\":false}");
            })) yield break;
            Debug.Log("MAINTENANCE SHOWCASE CHECK PASSED: " + checks + " checks, complete flight/weld/return/charge and modular mining tool");
            Application.Quit(0);
        }
        private void OnDestroy()
        {
            foreach (var material in materials) if (material != null) Destroy(material);
            if(sparkTexture != null) Destroy(sparkTexture);
        }
    }
}
