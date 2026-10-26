using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    // Explicit command-line entry only. Normal starts keep their menu and Mira intro.
    public sealed class StartStationCheck : MonoBehaviour
    {
        private int checks;
        private const string Folder = "Logs/Startstation";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-startStationPreview") >= 0 || Array.IndexOf(args,"-storagePreview")>=0
                ) new GameObject("Start station preview/check").AddComponent<StartStationCheck>();
        }
        private void Require(bool condition, string label)
        { checks++; if (!condition) throw new Exception("Startstation: " + label); Debug.Log("STARTSTATION OK: " + label); }
        private bool Guard(Action action)
        { try { action(); return true; } catch (Exception e) { Debug.LogException(e); Application.Quit(1); return false; } }
        private IEnumerator Capture(string name)
        {
            yield return null; yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Folder + "/" + name + ".png"));
            yield return new WaitForSecondsRealtime(.25f);
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void CaptureExterior(StationInteriorLayout layout, Vector3? eye = null, Vector3? focus = null, string file = "07-modulhuellen-aussen.png")
        {
            var node = new GameObject("Exterior module inspection");
            var camera = node.AddComponent<Camera>(); camera.CopyFrom(Camera.main); camera.enabled = false;
            camera.transform.position = layout.transform.TransformPoint(eye ?? new Vector3(110, 135, 140));
            camera.transform.LookAt(layout.transform.TransformPoint(focus ?? new Vector3(0, 1, 8)));
            camera.fieldOfView = eye.HasValue ? 60 : 38; camera.nearClipPlane = .1f; camera.farClipPlane = 1000;
            var target = new RenderTexture(1600, 1000, 24); camera.targetTexture = target;
            var previous = RenderTexture.active;
            try {
                camera.Render(); RenderTexture.active = target;
                var pixels = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); pixels.Apply();
                File.WriteAllBytes(Path.GetFullPath(Folder + "/" + file), pixels.EncodeToPNG()); Destroy(pixels);
            } finally { RenderTexture.active = previous; camera.targetTexture = null; target.Release(); Destroy(target); Destroy(node); }
        }
#endif
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1);
            bool test = Array.IndexOf(Environment.GetCommandLineArgs(), "-startStationCheck") >= 0;
            bool storagePreview=Array.IndexOf(Environment.GetCommandLineArgs(),"-storagePreview")>=0;
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-stationKeilPreview")>=0) {
                FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride=Path.GetFullPath("Logs/KeilPreview/Saves");
                CinematicLibrary.PreferencePrefix="SpaceMiner.KeilPreview.";
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-stationOperationsPreview")>=0) {
                FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride=Path.GetFullPath("Logs/AufbauphasePreview/Saves");
                CinematicLibrary.PreferencePrefix="SpaceMiner.AufbauphasePreview.";
            }
            if(storagePreview) {
                FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride=Path.GetFullPath("Logs/StoragePreview/Saves");
                CinematicLibrary.PreferencePrefix="SpaceMiner.StoragePreview.";
            }
            if (test)
            {
                Directory.CreateDirectory(Folder);
                FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride = Path.GetFullPath(Folder + "/Saves");
                CinematicLibrary.PreferencePrefix = "SpaceMiner.Test.Startstation." + Guid.NewGuid() + ".";
            }
            FindFirstObjectByType<StartMenu>().StartDemo(); FindFirstObjectByType<IntroSequence>().Skip();
            yield return null; yield return null;
            var mode = StationInteriorMode.Current; var scenario = FindFirstObjectByType<WaterScenario>();
            scenario.Scanner.DismissDialogue();
            if (!test)
            {
                if (!StationInteriorMode.IsInside) mode.Enter();
                if(Array.IndexOf(Environment.GetCommandLineArgs(),"-droneHallPreview")>=0)
                    mode.RestoreInterior(mode.Layout.transform.TransformPoint(RingWalkthrough.At(33.5f,30,.04f)),false,30,0);
                if(Array.IndexOf(Environment.GetCommandLineArgs(),"-indoorTrafficPreview")>=0)
                    mode.RestoreInterior(mode.Layout.transform.TransformPoint(new Vector3(2,.04f,40.5f)),false,mode.Layout.transform.eulerAngles.y+180,3);
                if(storagePreview) {
                    mode.Exit(true); scenario.SetSimulationRate(0);
                    var camera=FindFirstObjectByType<OrbitCamera>(); camera.ResetView(); camera.Orbit(new Vector2(129,-20));
                    camera.Zoom(Mathf.Log(140f/52)/.16f);
                    Vector3 offset=mode.Layout.transform.TransformPoint(RingWalkthrough.At(46,210,2))-camera.Pivot;
                    float speed=Mathf.Max(2,camera.Distance*.35f)*SettingsStore.Current.Controls.CameraSpeed;
                    camera.Move(camera.transform.InverseTransformDirection(offset.normalized),offset.magnitude/speed,false);
                }
                yield break;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            scenario.SetSimulationRate(0);
            var layout = mode.Layout; var model = layout.Geometry; var orbit = FindFirstObjectByType<OrbitCamera>();
            if (!Guard(() => {
                Require(SettingsStore.PathName.Contains("Startstation-settings"), "isolated preferences");
                Require(StationInteriorMode.IsInside && mode.NearConsole, "normal intro starts at command console");
                Require(model.FloorSegments == 12 && model.ConnectorCount == 12, "one complete ring with twelve joins");
                Require(model.Root.IsChildOf(scenario.transform) && model.Root.localPosition == Vector3.zero, "real campaign station coordinates");
                Require(model.Root.Find("Ring 2") == null && model.LiftLandings == 0, "no second ring or active lifts");
                Require(model.ParkedDrones == 0 && model.CampaignBerths.Count == 10, "ten real berths without duplicate demo drones");
                Require(model.ModuleCount == 5 && model.ModuleJoinCount == 5 && model.InnerModuleCount == 1, "command, drone hall and dock, water storage and one internal workshop only");
                Require(model.ReactorServiceCount == 4 && model.DamagedReactorServices == 3, "one intact and three ruptured reactor corridors");
                Require(model.HoneycombSurfaces > 150, "two honeycomb skins installed around ring, rooms and surviving service spans");
                int coveredRoofs=0; foreach(var mesh in model.Root.GetComponentsInChildren<MeshFilter>(true))
                    if(mesh.name=="Ringdecke" && mesh.transform.childCount==2 && !mesh.GetComponent<Renderer>().enabled) coveredRoofs++;
                Require(coveredRoofs==12,"all twelve ring roofs use both geometric skins without coplanar legacy backing");
                Require(Mathf.Abs(model.BuiltOuterRadius(7)-(38+16f/3))<.001f && model.DamagedStorageStages==2,"one short working storage stage and two ruined extensions");
                var storageVisual=scenario.GetComponent<StationVisual>();
                Require(storageVisual.StorageDockCount==3 && storageVisual.StorageDock(0)==storageVisual.TankSocket,"three separate storage dock positions with active first inlet");
                for(int stage=0;stage<3;stage++) {
                    var socket=storageVisual.StorageDock(stage); Vector3 local=layout.transform.InverseTransformPoint(socket.position);
                    Require(storageVisual.StorageDockOperational(stage)==(stage==0),"only intact storage dock enabled "+stage);
                    Require(Mathf.Abs(new Vector2(local.x,local.z).magnitude-RingWalkthrough.StorageRadius(stage))<.02f,"dock belongs to its own short storage stage "+stage);
                    if(stage>0) {
                        Require(Vector3.Distance(socket.position,storageVisual.StorageDock(stage-1).position)>5,"independent storage dock clearance "+stage);
                        Vector3 gap=layout.transform.TransformPoint(RingWalkthrough.At(RingWalkthrough.StorageRadius(stage),210,.04f));
                        Require(!layout.ContainsPosition(gap) && !Physics.Raycast(gap+Vector3.up*2,Vector3.down,4,1<<25),"damaged storage has no hidden usable floor "+stage);
                    }
                }
                Require(!Physics.Raycast(storageVisual.TankSocket.position+storageVisual.TankSocket.forward*.05f,-storageVisual.TankSocket.forward,1.4f,1<<25),"storage side receiver passes through actual aperture");
                Require(!Physics.CheckBox(scenario.Worker.TankDockPoint,new Vector3(.95f,.95f,.95f),scenario.Worker.TankDockRotation,1<<25),"working storage dock fits the campaign drone outside the hull");
                Require(model.WorkshopPortals.Count == 3, "three external workshop airlocks");
                Require(model.CommandWindowCount == 8 && model.CommandConsole != null, "front and side bridge windows with command desk");
                Require(model.Root.Find("Ring 1/DROHNENDECK Decke / Modul 2") == null, "external dock has open sky");
                Require(model.OuterStages(0) == 1 && model.OuterStages(1) == 2 && model.OuterStages(11) == 0 && model.OuterStages(7) == 1, "built starting rooms and retained external dock only");
                Require(model.OuterStages(2) == 0 && model.Root.Find("Ring 1/ERZLAGER Boden") == null, "future outer expansions absent");
                Vector3 hallPosition = layout.transform.InverseTransformPoint(model.DetailedHall.transform.position);
                Vector3 storagePosition = scenario.GetComponent<StationVisual>().TankSocket.parent.localPosition;
                Require(Vector3.Dot(hallPosition.normalized, storagePosition.normalized) < -.999f, "water storage lies opposite drone hall");
                foreach (string name in new[] { "Solar Wing Left", "Solar Wing Right" }) {
                    var wing = layout.transform.Find(name); var panel = wing.Find("Panel frame");
                    float width = wing.TransformVector(Vector3.right * panel.localScale.x).magnitude;
                    float depth = wing.TransformVector(Vector3.forward * panel.localScale.z).magnitude;
                    Require(Mathf.Abs(width - 22.8f) < .01f && Mathf.Abs(depth - 21.4f) < .01f, "enlarged solar frame " + name);
                }
                Vector3 repairPosition = layout.transform.InverseTransformPoint(layout.RepairDrone.position); repairPosition.y = .04f;
                Require(new Vector2(repairPosition.x, repairPosition.z).magnitude < 29 && layout.ContainsPosition(layout.transform.TransformPoint(repairPosition)), "internal repair drone remains inside its workshop");
                var reactor = scenario.transform.Find("Station Geometry/Shielded Reactor").GetComponent<Renderer>().bounds;
                Require(Mathf.Abs(reactor.size.x - 8) < .01f && Mathf.Abs(reactor.size.y - 6) < .01f, "reactor housing metre-scale diameter8 height6");
                Require(layout.Airlock.PermanentlyOpen && !layout.Airlock.Request(mode.FeetPosition), "existing access remains open");
                for (int i = 0; i < scenario.Drones.Length; i++) {
                    Require(Vector3.Distance(scenario.Drones[i].HomePosition, model.CampaignBerths[i].position) < .001f, "real drone home " + i);
                    Require(Vector3.Distance(scenario.Drones[i].transform.position, scenario.Drones[i].HomePosition) < .001f, "real drone parked " + i);
                    Vector3 localHome = layout.transform.InverseTransformPoint(scenario.Drones[i].HomePosition);
                    Require(localHome.y == (i < 5 ? 1 : 4.3f), "external lower or upper dock row " + i);
                    var drone = scenario.Drones[i]; int mask = 1 << 25;
                    Vector3 launch = drone.HomeApproachPoint - drone.HomePosition;
                    Require(!Physics.SphereCast(drone.HomePosition, .6f, launch.normalized, out _, launch.magnitude, mask), "unobstructed bay exit " + i);
                    Require(!Physics.SphereCast(drone.HomeApproachPoint, .6f, Vector3.up, out _, 8, mask), "unobstructed vertical launch " + i);
                }
                Require(mode.OpenConsole(), "real command console opens");
                scenario.Scanner.Advance(10); Require(scenario.Scanner.Scan(), "first scan from new console");
                scenario.Scanner.DismissDialogue(); mode.CloseConsole();
            })) yield break;
            yield return Capture("01-kommandozentrale");
            foreach (var portal in model.WorkshopPortals)
            {
                if (!Guard(() => {
                    Vector3 local = layout.transform.InverseTransformPoint(portal.position);
                    float heading = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
                    Require(Mathf.Abs(new Vector2(local.x, local.z).magnitude - 54) < .01f, "airlock is on workshop exterior front");
                    mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(52, heading, .04f)), false, heading, 0);
                    for (int n = 0; n < 60; n++) mode.Walk(Vector2.up, .05f * PlayerVitals.WalkSpeed / StationInteriorMode.DeveloperWalkSpeed);
                    local = layout.transform.InverseTransformPoint(mode.FeetPosition);
                    Require(new Vector2(local.x, local.z).magnitude > 58 && local.y > -.1f, "walk through external airlock " + portal.name);
                })) yield break;
            }
            // Exercise the campaign CharacterController, not a separate preview controller.
            for (int s = 0; s < 12; s++)
            {
                if (!Guard(() => {
                    mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(33.5f, s * 30 + 12, .04f)), false, s * 30 + 102, 0);
                    for (int n = 0; n < 40; n++) mode.Walk(Vector2.up, .05f * PlayerVitals.WalkSpeed / StationInteriorMode.DeveloperWalkSpeed);
                    Require(mode.FeetPosition.y > -.1f && mode.FeetPosition.y < .15f, "ring floor and sector join " + s);
                    Require(Vector3.Distance(mode.FeetPosition, layout.transform.TransformPoint(RingWalkthrough.At(33.5f, s * 30 + 12))) > 3.8f, "walk across join " + s);
                })) yield break;
            }
            for (int s = 0; s < 12; s++)
            {
                if (!Guard(() => {
                    mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(33.5f, s * 30, .04f)), false, s * 30, 0);
                    for (int n = 0; n < 105; n++) mode.Walk(Vector2.up, .05f * PlayerVitals.WalkSpeed / StationInteriorMode.DeveloperWalkSpeed);
                    Vector3 p = layout.transform.InverseTransformPoint(mode.FeetPosition);
                    float radius = new Vector2(p.x, p.z).magnitude;
                    Require((s==7?radius>40 && radius<model.BuiltOuterRadius(7)-.25f:model.OuterStages(s) > 0 ? radius > 44 : radius > 37 && radius < 38) && p.y > -.1f, "built exterior entrance or capped future port " + s);
                })) yield break;
            }
            foreach (int s in new[] { 1, 2, 4, 5, 7, 8, 10, 11 })
            {
                if (!Guard(() => {
                    mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(33.5f, s * 30, .04f)), false, s * 30 + 180, 0);
                    for (int n = 0; n < 150; n++) mode.Walk(Vector2.up, .05f * PlayerVitals.WalkSpeed / StationInteriorMode.DeveloperWalkSpeed);
                    Vector3 p = layout.transform.InverseTransformPoint(mode.FeetPosition);
                    float radius = new Vector2(p.x, p.z).magnitude;
                    Require((s == 1 ? radius < 23 && radius > 18 : radius > 29 && radius < 30) && Mathf.Abs(p.y) < .12f, "internal workshop or sealed future inner port " + s);
                    mode.Look(new Vector2(180, 0));
                    for (int n = 0; n < 150; n++) mode.Walk(Vector2.up, .05f * PlayerVitals.WalkSpeed / StationInteriorMode.DeveloperWalkSpeed);
                    p = layout.transform.InverseTransformPoint(mode.FeetPosition);
                    Require(new Vector2(p.x, p.z).magnitude > 30 && Mathf.Abs(p.y) < .12f, "inward sector returns to ring " + s);
                })) yield break;
            }
            for (int axis = 0; axis < 4; axis++)
            {
                if (!Guard(() => {
                    float heading = axis * 90;
                    mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(33.5f, heading, .04f)), false, heading + 180, 0);
                    for (int n = 0; n < 250; n++) mode.Walk(Vector2.up, .05f * PlayerVitals.WalkSpeed / StationInteriorMode.DeveloperWalkSpeed);
                    Vector3 p = layout.transform.InverseTransformPoint(mode.FeetPosition);
                    float radius = new Vector2(p.x, p.z).magnitude;
                    RingWalkthrough.ServiceBreak(axis, out float breakFrom, out float breakTo);
                    Require((axis == 0 ? radius > 5.2f && radius < 7 : radius > breakTo + .7f && radius < breakTo + 1.2f) && Mathf.Abs(p.y) < .12f, "intact route reaches collar; damaged route stops at pressure bulkhead " + axis);
                    if (axis != 0) mode.Walk(Vector2.down, .1f); // Release pressure against the wall before checking a saved standing capsule.
                    Require(layout.HasStandingClearance(mode.FeetPosition), "service endpoint can be restored safely " + axis);
                    mode.Look(new Vector2(180, 0));
                    for (int n = 0; n < 250; n++) mode.Walk(Vector2.up, .05f * PlayerVitals.WalkSpeed / StationInteriorMode.DeveloperWalkSpeed);
                    p = layout.transform.InverseTransformPoint(mode.FeetPosition);
                    Require(new Vector2(p.x, p.z).magnitude > 32 && Mathf.Abs(p.y) < .12f, "maintenance collar back to ring " + axis);
                    mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(27, heading, .04f)), false, heading, 0);
                    for (int n = 0; n < 60; n++) mode.Walk(Vector2.right, .05f * PlayerVitals.WalkSpeed / StationInteriorMode.DeveloperWalkSpeed);
                    p = Quaternion.Euler(0, -heading, 0) * layout.transform.InverseTransformPoint(mode.FeetPosition);
                    Require(p.x > 1 && p.x < 1.3f && Mathf.Abs(p.y) < .12f, "three-metre service walls contain capsule " + axis);
                    if (axis != 0) {
                        float gap = (breakFrom + breakTo) / 2;
                        Vector3 gapWorld = layout.transform.TransformPoint(RingWalkthrough.At(gap, heading, .04f));
                        Require(!layout.ContainsPosition(gapWorld) && !Physics.Raycast(gapWorld + Vector3.up * 2, Vector3.down, 4, 1 << 25), "rupture has no hidden floor or valid save position " + axis);
                        mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(6, heading, .04f)), false, heading, 0);
                        for (int n = 0; n < 160; n++) mode.Walk(Vector2.up, .05f * PlayerVitals.WalkSpeed / StationInteriorMode.DeveloperWalkSpeed);
                        p = layout.transform.InverseTransformPoint(mode.FeetPosition); radius = new Vector2(p.x, p.z).magnitude;
                        Require(radius > breakFrom - 1.2f && radius < breakFrom - .7f && Mathf.Abs(p.y) < .12f, "reactor-side bulkhead prevents walking into rupture " + axis);
                    }
                })) yield break;
            }
            if (!Guard(() => {
                mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(6, 45, .04f)), false, 225, 0);
                for (int n = 0; n < 80; n++) mode.Walk(Vector2.up, .05f * PlayerVitals.WalkSpeed / StationInteriorMode.DeveloperWalkSpeed);
                Vector3 p = layout.transform.InverseTransformPoint(mode.FeetPosition);
                Require(new Vector2(p.x, p.z).magnitude > 4.5f && new Vector2(p.x, p.z).magnitude < 5, "sealed reactor capsule blocks entry");
                Require(!layout.ContainsPosition(layout.transform.TransformPoint(Vector3.zero)), "reactor core is not a walkable saved position");
            })) yield break;
            mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(6, 0, .04f)), false, 0, 0);
            yield return Capture("16-wartungskragen-zum-ring");
            mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(25, 0, .04f)), false, 180, 0);
            yield return Capture("17-servicegang-zum-reaktor");
            foreach (var route in new[] { new Vector3(0, .04f, 35), RingWalkthrough.At(50, 30, .04f) })
            {
                if (!Guard(() => {
                    float heading = route.x > 10 ? 30 : 0;
                    mode.RestoreInterior(layout.transform.TransformPoint(route), false, heading, 0);
                    Vector3 before = mode.FeetPosition;
                    for (int n = 0; n < 60; n++) mode.Walk(Vector2.up, .05f * PlayerVitals.WalkSpeed / StationInteriorMode.DeveloperWalkSpeed);
                    Require(Vector3.Distance(mode.FeetPosition, before) > 6.3f && mode.FeetPosition.y > -.1f, "stage coupling traversable at " + route);
                })) yield break;
            }
            if (!Guard(() => {
                mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(33.5f, 60, .04f)), false, 60, 0);
                for (int n = 0; n < 220; n++) mode.Walk(Vector2.up, .05f * PlayerVitals.WalkSpeed / StationInteriorMode.DeveloperWalkSpeed);
                Vector3 p = layout.transform.InverseTransformPoint(mode.FeetPosition);
                Require(new Vector2(p.x, p.z).magnitude > 37 && new Vector2(p.x, p.z).magnitude < 38 && p.y > -.1f, "unbuilt outer expansion is closed by ring hull");
                mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(33.5f, 0, .04f)), false, 0, 0);
                mode.Vitals.Reset(); Vector3 begin = mode.FeetPosition;
                mode.Walk(Vector2.up, .2f, true);
                float sprintDistance = new Vector2(mode.FeetPosition.x - begin.x, mode.FeetPosition.z - begin.z).magnitude;
                Require(Mathf.Abs(sprintDistance - 4.16f) < .025f && mode.Vitals.Stamina == 100 && !mode.Vitals.Exhausted, "development campaign sprint at 20.8 m/s without stamina drain");
                mode.Look(new Vector2(25, 12));
                var saved = SaveGameStore.Capture(scenario, "Startstation test", "new layout");
                string file = SaveGameStore.Write(saved, false, Path.GetFullPath(Folder + "/Saves"));
                SaveGameStore.Apply(SaveGameStore.Read(file), scenario);
                Require(Vector3.Distance(mode.FeetPosition, saved.interiorFeet) < .01f && Mathf.Abs(mode.ViewYaw - saved.interiorYaw) < .01f, "new layout save position and view round trip");
                Require(Mathf.Abs(mode.Vitals.Stamina - saved.playerVitals.Stamina) < .01f, "saved stamina retained");
                Vector3 originalSavedFeet=saved.interiorFeet;
                saved.interiorLayout=5; saved.interiorFeet=layout.transform.TransformPoint(RingWalkthrough.At(45,330,.04f));
                SaveGameStore.Apply(saved,scenario);
                Require(Vector3.Distance(mode.FeetPosition,layout.SpawnPosition)<.01f,"removed outer expansion relocates old save safely");
                saved.interiorFeet=layout.transform.TransformPoint(RingWalkthrough.At(24,150,.04f));
                SaveGameStore.Apply(saved,scenario);
                Require(Vector3.Distance(mode.FeetPosition,layout.SpawnPosition)<.01f,"removed inner expansion relocates old save safely");
                for (int axis=1;axis<4;axis++) {
                    RingWalkthrough.ServiceBreak(axis,out float from,out float to);
                    saved.interiorFeet=layout.transform.TransformPoint(RingWalkthrough.At((from+to)/2,axis*90,.04f));
                    SaveGameStore.Apply(saved,scenario);
                    Require(Vector3.Distance(mode.FeetPosition,layout.SpawnPosition)<.01f,"old save in new reactor gap relocates safely "+axis);
                }
                saved.interiorLayout=6;
                foreach(float radius in new[]{46f,51.333f}) {
                    saved.interiorFeet=layout.transform.TransformPoint(RingWalkthrough.At(radius,210,.04f));
                    SaveGameStore.Apply(saved,scenario);
                    Require(Vector3.Distance(mode.FeetPosition,layout.SpawnPosition)<.01f,"old save in damaged storage relocates safely "+radius);
                    Require(Mathf.Abs(scenario.WaterLiters-saved.water)<.001f,"storage migration keeps water inventory "+radius);
                }
                saved.interiorFeet=layout.transform.TransformPoint(RingWalkthrough.At(40.7f,210,.04f));
                SaveGameStore.Apply(saved,scenario);
                Require(Vector3.Distance(mode.FeetPosition,saved.interiorFeet)<.01f,"old save in intact storage remains in place");
                saved.interiorLayout=4; saved.interiorFeet=layout.transform.TransformPoint(new Vector3(0,.04f,69.2f));
                SaveGameStore.Apply(saved,scenario);
                Require(Vector3.Distance(mode.FeetPosition,layout.SpawnPosition)<.01f,"old extended command wing relocates safely to compact console");
                saved.interiorFeet=layout.transform.TransformPoint(RingWalkthrough.At(12,30,.04f));
                SaveGameStore.Apply(saved,scenario);
                Require(Vector3.Distance(mode.FeetPosition,layout.SpawnPosition)<.01f,"old deep inner room relocates outside new empty core");
                saved.interiorLayout=3;
                saved.interiorFeet=model.DetailedHall.transform.TransformPoint(new Vector3(8,.04f,13.8f));
                SaveGameStore.Apply(saved,scenario);
                Require(Vector3.Distance(mode.FeetPosition,layout.SpawnPosition)<.01f,"old layout pose inside new service cabinet relocates safely");
                saved.interiorFeet=originalSavedFeet;
                saved.interiorLayout = 2; saved.drones[0].position = layout.transform.TransformPoint(RingWalkthrough.At(63.6f, 30, 1));
                SaveGameStore.Apply(saved, scenario);
                Require(Vector3.Distance(mode.FeetPosition, saved.interiorFeet) < .01f && Mathf.Abs(mode.ViewYaw - saved.interiorYaw) < .01f, "previous ring layout retains player position and view");
                Require(Vector3.Distance(scenario.Worker.transform.position, scenario.Worker.HomePosition) < .01f && Mathf.Abs(scenario.Worker.BatteryKwh - saved.drones[0].battery) < .001f, "previous ring dock migrates without losing energy");
                saved.interiorFeet = layout.transform.TransformPoint(RingWalkthrough.At(54, 33.25f, .04f));
                SaveGameStore.Apply(saved, scenario);
                Require(Vector3.Distance(mode.FeetPosition, layout.SpawnPosition) < .01f && mode.NearConsole, "previous pose inside new hall front relocates safely");
                saved.interiorLayout = 0; saved.interiorFeet = new Vector3(200, 30, 500); saved.drones[0].position = Vector3.one * -20;
                SaveGameStore.Apply(saved, scenario);
                Require(Vector3.Distance(mode.FeetPosition, layout.SpawnPosition) < .01f && mode.NearConsole, "legacy interior moved safely to new console");
                Require(Vector3.Distance(scenario.Worker.transform.position, scenario.Worker.HomePosition) < .01f && Mathf.Abs(scenario.Worker.BatteryKwh - saved.drones[0].battery) < .001f, "legacy docked drone relocated with energy intact");
                Require(Mathf.Abs(scenario.WaterLiters - saved.water) < .001f && scenario.Scanner.FirstScanComplete, "legacy resources and scan retained");
                scenario.Scanner.DismissDialogue();
                mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(33.5f, 30, .04f)), false, 30, 0);
            })) yield break;
            yield return Capture("02-ring-zum-drohnendeck");
            // Walk the real campaign capsule from the ring into the imported hall and back.
            if(!Guard(()=>{
                var hall=model.DetailedHall;
                Require(hall!=null && hall.Portals.Length==3,"detailed hall installed at real station");
                Require(hall.GetComponentInChildren<HallExperimentView>()==null
                    && hall.GetComponentsInChildren<Camera>().Length==0,"module has no experiment UI or second camera");
                Require(hall.GetComponentsInChildren<Transform>().Length>100,"imported modular geometry present");
                Require(Array.TrueForAll(hall.GetComponentsInChildren<Transform>(true),t=>t.gameObject.layer==25),"complete module uses station geometry layer");
                Require(Array.TrueForAll(hall.GetComponentsInChildren<Transform>(true),t=>!t.name.StartsWith("Connector") && !t.name.StartsWith("COL_Connector") && !t.name.StartsWith("Apron") && !t.name.StartsWith("COL_Apron")),"standalone connector and apron removed");
                Require(model.Root.Find("Ring 1/DROHNENDECK Decke / Modul 1")==null
                    && model.Root.Find("Ring 1/Reparaturbank")==null,"old workshop shell and furniture replaced");
                mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(33.5f,30,.04f)),false,30,0);
                for(int n=0;n<100;n++)mode.Walk(Vector2.up, .05f * PlayerVitals.WalkSpeed / StationInteriorMode.DeveloperWalkSpeed);
                var local=layout.transform.InverseTransformPoint(mode.FeetPosition);
                Require(new Vector2(local.x,local.z).magnitude>44 && Mathf.Abs(local.y)<.12f,"physical ring to detailed hall entry");
                mode.Look(new Vector2(180,0));for(int n=0;n<100;n++)mode.Walk(Vector2.up, .05f * PlayerVitals.WalkSpeed / StationInteriorMode.DeveloperWalkSpeed);
                local=layout.transform.InverseTransformPoint(mode.FeetPosition);
                Require(new Vector2(local.x,local.z).magnitude<34 && Mathf.Abs(local.y)<.12f,"physical detailed hall back to ring");
            }))yield break;
            yield return Capture("12-halle-zum-ring");
            mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(33.5f,30,.04f)),false,30,0);
            yield return Capture("13-ring-eingang-detailhalle");
            mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(43,30,.04f)),false,5,-2);
            yield return Capture("14-detailhalle-arbeitsplatz");
            var detailed=model.DetailedHall;
            Vector3 gate=layout.transform.InverseTransformPoint(detailed.Portals[1].position);gate.y=.04f;
            float gateHeading=Mathf.Atan2(gate.x,gate.z)*Mathf.Rad2Deg;
            mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(50,gateHeading,.04f)),false,gateHeading,0);
            Vector3 initiallyOpen=detailed.LeftLeaves[1].position;
            if(!Guard(()=>Require(detailed.Request(1,false),"unoccupied hall gate accepts closing")))yield break;
            float doorDeadline=Time.realtimeSinceStartup+6;
            while(detailed.Amount(1)>.001f && Time.realtimeSinceStartup<doorDeadline)yield return null;
            if(!Guard(()=>{
                Require(detailed.Amount(1)<.001f && Vector3.Distance(initiallyOpen,detailed.LeftLeaves[1].position)>1.6f,"station hall gate closes with metre scale travel");
                for(int n=0;n<100;n++)mode.Walk(Vector2.up, .05f * PlayerVitals.WalkSpeed / StationInteriorMode.DeveloperWalkSpeed);
                var local=layout.transform.InverseTransformPoint(mode.FeetPosition);
                Require(new Vector2(local.x,local.z).magnitude<53.6f,"closed hall gate blocks real campaign capsule");
                Require(detailed.Nearest(mode.FeetPosition)==1 && detailed.Interact(mode.FeetPosition),"same station interaction reaches gate");
            }))yield break;
            doorDeadline=Time.realtimeSinceStartup+6;
            while(detailed.Amount(1)<.999f && Time.realtimeSinceStartup<doorDeadline)yield return null;
            if(!Guard(()=>{
                Require(detailed.Amount(1)>.999f && Vector3.Distance(initiallyOpen,detailed.LeftLeaves[1].position)<.005f,"station gate reopens to exact initial pose");
                mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(53.65f,gateHeading,.04f)),false,gateHeading,0);
                Require(!detailed.Request(1,false) && detailed.Target(1),"campaign visitor prevents closing in doorway");
                for(int n=0;n<50;n++)mode.Walk(Vector2.up, .05f * PlayerVitals.WalkSpeed / StationInteriorMode.DeveloperWalkSpeed);
                var local=layout.transform.InverseTransformPoint(mode.FeetPosition);
                Require(new Vector2(local.x,local.z).magnitude>58 && Mathf.Abs(local.y)<.12f,"physical hall gate passage to existing exterior dock");
            }))yield break;
            yield return Capture("15-detailhalle-aussendock");
            mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(57, 30, .04f)), false, 30, 0);
            yield return Capture("03-drohnendeck");
            mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(42, 30, .04f)), false, 30, 0);
            yield return Capture("08-halle-drei-aussenschleusen");
            mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(68, 30, .04f)), false, 210, 0);
            yield return Capture("09-aussendock-zur-halle");
            mode.RestoreInterior(layout.transform.TransformPoint(new Vector3(0, .04f, 46.5f)), false, 0, 5);
            yield return Capture("10-stationspult-fenster");
            mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(33.5f, 12, .04f)), false, 102, 0);
            yield return Capture("05-ringkupplung-innen");
            mode.RestoreInterior(layout.transform.TransformPoint(new Vector3(0, .04f, 36)), false, 0, 0);
            yield return Capture("06-moduluebergang-zentrale");
            if (!Guard(() => {
                mode.Exit(); Vector3 outside = Camera.main.transform.position; float fov = Camera.main.fieldOfView;
                Require(mode.Enter(), "return inside from virtual exterior"); mode.Exit();
                Require(Vector3.Distance(Camera.main.transform.position, outside) < .001f && Mathf.Abs(Camera.main.fieldOfView - fov) < .001f, "exterior view restored");
                orbit.Focus(scenario.GetComponent<SpaceObject>());
                Require(scenario.AssignTankOrder(Array.Find(scenario.Asteroids, a => a.CanMineWater)), "real water transport starts from new dock");
                scenario.Scanner.DismissDialogue();
                float startWater = scenario.WaterLiters; bool aligned=false,migrated=false;
                for (int i = 0; i < 400 && scenario.Deliveries == 0; i++) {
                    scenario.Advance(30);
                    if(scenario.Worker.Phase==DronePhase.Unloading) {
                        aligned|=Vector3.Distance(scenario.Worker.CargoOutletPoint,scenario.Worker.TankInlet)<.002f;
                        if(!migrated) {
                            var old=SaveGameStore.Capture(scenario,"Old storage docking","revision6 receiver migration"); old.interiorLayout=6;
                            old.drones[0].position=layout.transform.TransformPoint(RingWalkthrough.At(54.6f,210,3));
                            SaveGameStore.Apply(old,scenario); migrated=true;
                            Require(Vector3.Distance(scenario.Worker.transform.position,scenario.Worker.TankDockPoint)<.001f && scenario.Worker.Phase==DronePhase.Unloading,"old unloading pose moves to intact new storage dock");
                            Require(Mathf.Abs(scenario.Worker.CargoKg-old.drones[0].cargo)<.001f && Mathf.Abs(scenario.Worker.BatteryKwh-old.drones[0].battery)<.001f,"dock migration keeps cargo and energy");
                        }
                    }
                }
                Require(aligned && migrated,"actual water delivery seals against active first-stage socket");
                Require(scenario.Deliveries > 0 && scenario.WaterLiters > startWater, "real mining trip unloads at relocated tank");
                scenario.Worker.ReturnToShip();
                for (int i = 0; i < 800 && !scenario.Worker.IsReady; i++) scenario.Advance(30);
                Require(scenario.Worker.IsReady && Vector3.Distance(scenario.Worker.transform.position, scenario.Worker.HomePosition) < .01f,
                    "real drone returns to enlarged deck / " + scenario.Worker.Phase + " / battery " + scenario.Worker.BatteryKwh);
                scenario.Scanner.DismissDialogue();
            })) yield break;
            yield return Capture("04-startstation-aussen");
            if (!Guard(() => CaptureExterior(layout))) yield break;
            if (!Guard(() => CaptureExterior(layout, new Vector3(0, 145, .1f), Vector3.zero, "18-stationsgrundriss.png"))) yield break;
            if (!Guard(() => CaptureExterior(layout, new Vector3(42, 48, 46), new Vector3(0, 1, 0), "19-reaktor-innenmodule.png"))) yield break;
            if (!Guard(() => CaptureExterior(layout, new Vector3(35, 26, -20), new Vector3(17, 1, 0), "20-gangbruch-schotten.png"))) yield break;
            if (!Guard(() => CaptureExterior(layout, new Vector3(92, 48, 30), new Vector3(57, 1, 0), "21-solarfeld-tragarm.png"))) yield break;
            if (!Guard(() => CaptureExterior(layout, RingWalkthrough.At(64, 30) + Quaternion.Euler(0, 30, 0) * new Vector3(22, 12, 16), RingWalkthrough.At(58, 30, 2), "11-aussendock-uebersicht.png"))) yield break;
            if (!Guard(() => CaptureExterior(layout, RingWalkthrough.At(46,210)+Quaternion.Euler(0,210,0)*new Vector3(30,27,18), RingWalkthrough.At(46,210,2), "22-wasserlager-drei-stufen.png"))) yield break;
            if (!Guard(() => CaptureExterior(layout, RingWalkthrough.At(40.7f,223.5f,3.3f)+Quaternion.Euler(0,223.5f,0)*new Vector3(11,5,3), RingWalkthrough.At(40.7f,223.5f,3.3f), "23-wasserlager-aktiver-anschluss.png"))) yield break;
            Debug.Log("STARTSTATION CHECK PASSED: " + checks + " checks"); Application.Quit(0);
#endif
        }
    }
}
