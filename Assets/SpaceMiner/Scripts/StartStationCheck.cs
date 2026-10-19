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
            if (Array.IndexOf(args, "-startStationPreview") >= 0
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                || Array.IndexOf(args, "-startStationCheck") >= 0
#endif
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
                Require(model.ModuleCount == 11 && model.ModuleJoinCount == 11, "eleven built room stages with individual hulls and couplings");
                Require(model.WorkshopPortals.Count == 3, "three external workshop airlocks");
                Require(model.CommandWindowCount == 8 && model.CommandConsole != null, "front and side bridge windows with command desk");
                Require(model.Root.Find("Ring 1/DROHNENDECK Decke / Modul 2") == null, "external dock has open sky");
                Require(model.OuterStages(0) == 3 && model.OuterStages(1) == 2 && model.OuterStages(11) == 1 && model.OuterStages(7) == 1, "start module depths");
                Require(model.OuterStages(2) == 0 && model.Root.Find("Ring 1/ERZLAGER Boden") == null, "future storage not built");
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
                    for (int n = 0; n < 60; n++) mode.Walk(Vector2.up, .05f);
                    local = layout.transform.InverseTransformPoint(mode.FeetPosition);
                    Require(new Vector2(local.x, local.z).magnitude > 58 && local.y > -.1f, "walk through external airlock " + portal.name);
                })) yield break;
            }
            // Exercise the campaign CharacterController, not a separate preview controller.
            for (int s = 0; s < 12; s++)
            {
                if (!Guard(() => {
                    mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(33.5f, s * 30 + 12, .04f)), false, s * 30 + 102, 0);
                    for (int n = 0; n < 40; n++) mode.Walk(Vector2.up, .05f);
                    Require(mode.FeetPosition.y > -.1f && mode.FeetPosition.y < .15f, "ring floor and sector join " + s);
                    Require(Vector3.Distance(mode.FeetPosition, layout.transform.TransformPoint(RingWalkthrough.At(33.5f, s * 30 + 12))) > 3.8f, "walk across join " + s);
                })) yield break;
            }
            foreach (int s in new[] { 0, 1, 3, 7, 9, 11 })
            {
                if (!Guard(() => {
                    mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(33.5f, s * 30, .04f)), false, s * 30, 0);
                    for (int n = 0; n < 105; n++) mode.Walk(Vector2.up, .05f);
                    Vector3 p = layout.transform.InverseTransformPoint(mode.FeetPosition);
                    Require(new Vector2(p.x, p.z).magnitude > 44 && p.y > -.1f, "open exterior module " + s);
                })) yield break;
            }
            foreach (int s in new[] { 1, 11 })
            {
                if (!Guard(() => {
                    mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(33.5f, s * 30, .04f)), false, s * 30 + 180, 0);
                    for (int n = 0; n < 150; n++) mode.Walk(Vector2.up, .05f);
                    Vector3 p = layout.transform.InverseTransformPoint(mode.FeetPosition);
                    Require(new Vector2(p.x, p.z).magnitude < 18 && p.y > -.1f, "open interior facility " + s);
                })) yield break;
            }
            foreach (var route in new[] { new Vector3(0, .04f, 50), new Vector3(4, .04f, 67), RingWalkthrough.At(50, 30, .04f) })
            {
                if (!Guard(() => {
                    float heading = route.x > 10 ? 30 : 0;
                    mode.RestoreInterior(layout.transform.TransformPoint(route), false, heading, 0);
                    Vector3 before = mode.FeetPosition;
                    for (int n = 0; n < 60; n++) mode.Walk(Vector2.up, .05f);
                    Require(Vector3.Distance(mode.FeetPosition, before) > 6.3f && mode.FeetPosition.y > -.1f, "stage coupling traversable at " + route);
                })) yield break;
            }
            if (!Guard(() => {
                mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(33.5f, 60, .04f)), false, 60, 0);
                for (int n = 0; n < 100; n++) mode.Walk(Vector2.up, .05f);
                Vector3 p = layout.transform.InverseTransformPoint(mode.FeetPosition);
                Require(new Vector2(p.x, p.z).magnitude < 38 && p.y > -.1f, "unbuilt port has solid wall");
                mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(33.5f, 0, .04f)), false, 0, 0);
                mode.Vitals.Reset(); Vector3 begin = mode.FeetPosition;
                mode.Walk(Vector2.up, .2f, true);
                float sprintDistance = new Vector2(mode.FeetPosition.x - begin.x, mode.FeetPosition.z - begin.z).magnitude;
                Require(Mathf.Abs(sprintDistance - 2.08f) < .025f && Mathf.Abs(mode.Vitals.Stamina - 96.4f) < .01f, "development campaign sprint at 10.4 m/s retains stamina drain");
                mode.Look(new Vector2(25, 12));
                var saved = SaveGameStore.Capture(scenario, "Startstation test", "new layout");
                string file = SaveGameStore.Write(saved, false, Path.GetFullPath(Folder + "/Saves"));
                SaveGameStore.Apply(SaveGameStore.Read(file), scenario);
                Require(Vector3.Distance(mode.FeetPosition, saved.interiorFeet) < .01f && Mathf.Abs(mode.ViewYaw - saved.interiorYaw) < .01f, "new layout save position and view round trip");
                Require(Mathf.Abs(mode.Vitals.Stamina - saved.playerVitals.Stamina) < .01f, "saved stamina retained");
                Vector3 originalSavedFeet=saved.interiorFeet;
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
                for(int n=0;n<100;n++)mode.Walk(Vector2.up,.05f);
                var local=layout.transform.InverseTransformPoint(mode.FeetPosition);
                Require(new Vector2(local.x,local.z).magnitude>44 && Mathf.Abs(local.y)<.12f,"physical ring to detailed hall entry");
                mode.Look(new Vector2(180,0));for(int n=0;n<100;n++)mode.Walk(Vector2.up,.05f);
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
                for(int n=0;n<100;n++)mode.Walk(Vector2.up,.05f);
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
                for(int n=0;n<50;n++)mode.Walk(Vector2.up,.05f);
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
            mode.RestoreInterior(layout.transform.TransformPoint(new Vector3(0, .04f, 69.5f)), false, 0, 5);
            yield return Capture("10-stationspult-fenster");
            mode.RestoreInterior(layout.transform.TransformPoint(RingWalkthrough.At(33.5f, 12, .04f)), false, 102, 0);
            yield return Capture("05-ringkupplung-innen");
            mode.RestoreInterior(layout.transform.TransformPoint(new Vector3(0, .04f, 64)), false, 0, 0);
            yield return Capture("06-moduluebergang-zentrale");
            if (!Guard(() => {
                mode.Exit(); Vector3 outside = Camera.main.transform.position; float fov = Camera.main.fieldOfView;
                Require(mode.Enter(), "return inside from virtual exterior"); mode.Exit();
                Require(Vector3.Distance(Camera.main.transform.position, outside) < .001f && Mathf.Abs(Camera.main.fieldOfView - fov) < .001f, "exterior view restored");
                orbit.Focus(scenario.GetComponent<SpaceObject>());
                Require(scenario.AssignTankOrder(Array.Find(scenario.Asteroids, a => a.CanMineWater)), "real water transport starts from new dock");
                scenario.Scanner.DismissDialogue();
                float startWater = scenario.WaterLiters;
                for (int i = 0; i < 400 && scenario.Deliveries == 0; i++) scenario.Advance(30);
                Require(scenario.Deliveries > 0 && scenario.WaterLiters > startWater, "real mining trip unloads at relocated tank");
                scenario.Worker.ReturnToShip();
                for (int i = 0; i < 800 && !scenario.Worker.IsReady; i++) scenario.Advance(30);
                Require(scenario.Worker.IsReady && Vector3.Distance(scenario.Worker.transform.position, scenario.Worker.HomePosition) < .01f,
                    "real drone returns to enlarged deck / " + scenario.Worker.Phase + " / battery " + scenario.Worker.BatteryKwh);
                scenario.Scanner.DismissDialogue();
            })) yield break;
            yield return Capture("04-startstation-aussen");
            if (!Guard(() => CaptureExterior(layout))) yield break;
            if (!Guard(() => CaptureExterior(layout, RingWalkthrough.At(64, 30) + Quaternion.Euler(0, 30, 0) * new Vector3(22, 12, 16), RingWalkthrough.At(58, 30, 2), "11-aussendock-uebersicht.png"))) yield break;
            Debug.Log("STARTSTATION CHECK PASSED: " + checks + " checks"); Application.Quit(0);
#endif
        }
    }
}
