#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    public sealed class RingWalkthroughCheck : MonoBehaviour
    {
        private int checks;
        private string folder;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-ringWalkthroughCheck") >= 0)
                new GameObject("Begehbarer Ring integration check").AddComponent<RingWalkthroughCheck>();
        }
        private void Require(bool condition, string label)
        { checks++; if (!condition) throw new Exception("Ring walkthrough: " + label); Debug.Log("RUNDGANG OK: " + label); }
        private bool Guard(Action action)
        { try { action(); return true; } catch (Exception e) { Debug.LogException(e); Application.Quit(1); return false; } }
        private static Vector2 RadialBounds(Transform node)
        {
            float min = float.MaxValue, max = 0;
            foreach (var point in node.GetComponent<MeshFilter>().sharedMesh.vertices)
            {
                float radius = new Vector2(point.x, point.z).magnitude;
                min = Mathf.Min(min, radius); max = Mathf.Max(max, radius);
            }
            return new Vector2(min, max);
        }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.25f); yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png")); yield return new WaitForSecondsRealtime(.25f);
        }
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1);
            var args = Environment.GetCommandLineArgs(); int width = 1600, height = 1000;
            int wi = Array.IndexOf(args, "-screen-width"), hi = Array.IndexOf(args, "-screen-height");
            if (wi >= 0 && wi + 1 < args.Length) int.TryParse(args[wi + 1], out width);
            if (hi >= 0 && hi + 1 < args.Length) int.TryParse(args[hi + 1], out height);
            folder = Path.GetFullPath("Logs/RingWalkthrough-" + width + "x" + height); Directory.CreateDirectory(folder);
            var prefs = new SpaceMinerPlayerSettings(); prefs.Graphics.DisplayMode = 0; prefs.Graphics.Width = width; prefs.Graphics.Height = height;
            prefs.Interface.Scale=1.5f;prefs.Accessibility.TextScale=1.4f;
            if (!Guard(() => {
                Require(SettingsStore.PathName.Contains("RingWalkthrough-settings"), "isolated preferences");
                Require(SettingsStore.Save(prefs, out _), "window preferences apply");
                FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride = Path.Combine(folder, "Saves");
                CinematicLibrary.PreferencePrefix = "SpaceMiner.Test.Rundgang." + Guid.NewGuid() + ".";
                FindFirstObjectByType<StartMenu>().StartDemo(); FindFirstObjectByType<IntroSequence>().Skip();
            })) yield break;
            yield return null; yield return new WaitForSecondsRealtime(.5f);
            var scenario = FindFirstObjectByType<WaterScenario>(); scenario.Scanner.DismissDialogue();
            var simulator = FindFirstObjectByType<RingConstructionSimulator>();
            if (!StationInteriorMode.IsInside) StationInteriorMode.Current.Enter();
            float water = scenario.WaterLiters; Vector3 drone = scenario.Worker.transform.position;
            Vector3 habitat = StationInteriorMode.Current.FeetPosition;
            if (!Guard(() => {
                simulator.Open(); simulator.EnterWalkthrough();
                Require(simulator.Walking && RingConstructionSimulator.IsOpen && SettingsMenu.PausesSimulation, "walking remains isolated and pauses campaign");
                Require(simulator.Walkthrough.FloorSegments == 24 && simulator.Walkthrough.ConnectorCount == 24, "two rings each with twelve connected segments");
                Require(simulator.Walkthrough.ParkedDrones == 20, "ten parked drone size placeholders per ring");
                Require(RingWalkthrough.OuterRadius-RingWalkthrough.InnerRadius == 9, "nine metre circulation width");
                var ringBounds = RadialBounds(simulator.Walkthrough.Root.Find("Ring 1/Ringsegment 1"));
                Require(Mathf.Abs(ringBounds.x-29)<.01f && Mathf.Abs(ringBounds.y-38)<.01f,"ring geometry begins at former first storage stage: 29 to 38 metres");
                Require(RingWalkthrough.StageDepth==16,"radial expansion depth doubled from eight to sixteen metres");
                var storageBounds = RadialBounds(simulator.Walkthrough.Root.Find("Ring 1/ERZLAGER Boden"));
                Require(Mathf.Abs(storageBounds.x-38)<.01f && Mathf.Abs(storageBounds.y-86)<.01f,"storage floor spans three enlarged stages up to radius 86");
                var innerBounds = RadialBounds(simulator.Walkthrough.Root.Find("Ring 1/INTERNE DROHNENWERKSTATT - S2 Boden"));
                Require(Mathf.Abs(innerBounds.x-9)<.01f && Mathf.Abs(innerBounds.y-29)<.01f,"interior workshop provides twenty metres of depth");
                Require(simulator.Walkthrough.LiftLandings==4,"two discrete shafts each connect two ring levels");
            })) yield break;
            var walk = simulator.Walkthrough;
            // Keep the normal habitat controller running: it previously unlocked the mouse every frame.
            for (int frame = 0; frame < 6; frame++)
            {
                yield return new WaitForEndOfFrame();
                if (!Guard(() => Require(StationInteriorMode.IsInside && Cursor.lockState == CursorLockMode.Locked,
                    "walkthrough owns cursor while habitat controller is active / frame " + frame))) yield break;
            }
            if (!Guard(() => {
                Require(walk.LookEnabled, "mouse look becomes available after capture settles");
                var angles = walk.LookAngles;
                walk.ProcessMovement(Vector2.zero, new Vector2(12, 5), false, .01f);
                Require(walk.LookAngles.x > angles.x && walk.LookAngles.y < angles.y, "live movement path turns horizontally and vertically");
                walk.Teleport(RingWalkthrough.At(RingWalkthrough.CentreRadius,0,.08f),0);walk.Vitals.Reset();
                walk.ProcessMovement(Vector2.up,Vector2.zero,false,.5f);float distance=walk.Feet.z-RingWalkthrough.CentreRadius;
                walk.Teleport(RingWalkthrough.At(RingWalkthrough.CentreRadius,0,.08f),0);
                walk.ProcessMovement(Vector2.up,Vector2.zero,true,.5f);
                Require(Mathf.Abs(distance-1.1f)<.03f && Mathf.Abs(walk.Feet.z-RingWalkthrough.CentreRadius-5.2f)<.03f && Mathf.Abs(walk.Vitals.Stamina-91)<.01f,
                    "development movement walks at 2.2 m/s and sprints at 10.4 m/s with unchanged stamina drain");
                walk.ToggleMouse();walk.ApplyView(true);var before=walk.Feet;angles=walk.LookAngles;
                walk.ProcessMovement(Vector2.up,Vector2.one,true,1);
                Require(Cursor.lockState==CursorLockMode.None && walk.Feet==before && walk.LookAngles==angles,
                    "Tab releases mouse and suspends movement for overlay buttons");
                walk.ToggleMouse();walk.ApplyView(true);
            })) yield break;
            yield return null;yield return null;yield return new WaitForEndOfFrame();
            if (!Guard(()=>Require(walk.LookEnabled && Cursor.lockState==CursorLockMode.Locked,"Tab resumes mouse look without habitat interference")))yield break;
            var display=FindFirstObjectByType<DisplayModeController>();var native=DisplayModeController.MonitorResolution;
            if (!Guard(()=>Require(simulator.Display==display && simulator.Display.CanSwitch,"overlay fullscreen buttons connect to active display controller")))yield break;
            if (!Guard(()=>Require(display.HandleShortcut(true,false),"F11 shortcut accepted while ring overlay pauses gameplay")))yield break;
            float deadline=Time.realtimeSinceStartup+5;
            while(display.IsChanging && Time.realtimeSinceStartup<deadline)yield return null;
            yield return new WaitForEndOfFrame();
            if (!Guard(()=> {
                Require(display.IsFullscreen && Screen.width==native.x && Screen.height==native.y,"walkthrough switches to native fullscreen");
                Require(walk.Texture.width==Screen.width && walk.Texture.height==Screen.height,"walkthrough render target resizes with fullscreen");
            }))yield break;
            yield return Capture("09-vollbild");
            if (!Guard(()=>Require(display.HandleShortcut(false,true),"Alt Enter shortcut accepted inside walkthrough")))yield break;
            deadline=Time.realtimeSinceStartup+5;
            while(display.IsChanging && Time.realtimeSinceStartup<deadline)yield return null;
            yield return new WaitForEndOfFrame();
            if (!Guard(()=> {
                Require(!display.IsFullscreen && Screen.width==width && Screen.height==height,"Alt Enter restores original window dimensions");
                walk.Teleport(RingWalkthrough.At(RingWalkthrough.CentreRadius,0,.08f),90);walk.Vitals.Reset();
            }))yield break;
            yield return Capture("00-start");
            if (!Guard(() => {
                // Enter every outer module through its actual ring doorway using the real controller.
                for (int level = 0; level < 2; level++)
                    for (int sector = 0; sector < 12; sector++)
                    {
                        walk.Teleport(RingWalkthrough.At(RingWalkthrough.CentreRadius, sector*30, level*8+.08f), sector*30);
                        for (int j=0;j<45;j++) walk.Walk(Vector2.up,.1f,false);
                        float radius = new Vector2(walk.Feet.x,walk.Feet.z).magnitude;
                        Require(radius > RingWalkthrough.OuterRadius+4 && Mathf.Abs(walk.Feet.y-level*8)<.25f, "outer module reachable / ring "+(level+1)+" sector "+(sector+1));
                    }
                for(int level=0;level<2;level++)for(int sector=0;sector<12;sector++)
                {
                    walk.Teleport(RingWalkthrough.At(RingWalkthrough.CentreRadius,sector*30,level*8+.08f),sector*30+180);
                    for(int j=0;j<35;j++)walk.Walk(Vector2.up,.1f,false);
                    Require(new Vector2(walk.Feet.x,walk.Feet.z).magnitude < RingWalkthrough.InnerRadius-3 && Mathf.Abs(walk.Feet.y-level*8)<.25f, "inner area reachable / ring "+(level+1)+" sector "+(sector+1));
                }
                walk.Teleport(RingWalkthrough.At(RingWalkthrough.CentreRadius,13,.08f),103);
                for(int j=0;j<30;j++)walk.Walk(Vector2.up,.1f,false);
                Require(Mathf.Atan2(walk.Feet.x,walk.Feet.z)*Mathf.Rad2Deg>20 && Mathf.Abs(walk.Feet.y)<.2f,"flush segment joint can be crossed without falling");
                walk.Teleport(RingWalkthrough.At(RingWalkthrough.OuterRadius+5,0,.08f),0);
                for(int j=0;j<20;j++)walk.Walk(Vector2.right,.1f,false);
                for(int j=0;j<220;j++)walk.Walk(Vector2.up,.1f,false);
                Require(walk.Feet.z>RingWalkthrough.OuterRadius+RingWalkthrough.StageDepth*2.8f,"command centre third radial stage reachable around furniture");
                walk.Teleport(RingWalkthrough.At(RingWalkthrough.CentreRadius,60,.08f),60);
                for(int j=0;j<225;j++)walk.Walk(Vector2.up,.1f,false);
                Require(new Vector2(walk.Feet.x,walk.Feet.z).magnitude>RingWalkthrough.OuterRadius+RingWalkthrough.StageDepth*2.8f,"all three storage stages reachable");
                for(int level=0;level<2;level++)foreach(int sector in new[]{0,1,3,6,9,11})
                {
                    float angle=sector*30;
                    walk.Teleport(RingWalkthrough.At(RingWalkthrough.InnerRadius-1,angle,level*8+.08f)+Quaternion.Euler(0,angle,0)*Vector3.right*2,angle+180);
                    for(int j=0;j<100;j++)walk.Walk(Vector2.up,.1f,false);
                    Require(new Vector2(walk.Feet.x,walk.Feet.z).magnitude<14 && Mathf.Abs(walk.Feet.y-level*8)<.25f,
                        "enlarged interior depth reachable alongside equipment / ring "+(level+1)+" sector "+(sector+1));
                }
                foreach(var portal in walk.WorkshopPortals)
                {
                    var pos=portal.position-walk.Root.position;
                    float heading=Mathf.Atan2(pos.x,pos.z)*Mathf.Rad2Deg;
                    walk.Teleport(RingWalkthrough.At(52,heading,pos.y+.08f),heading);for(int j=0;j<40;j++)walk.Walk(Vector2.up,.1f,false);
                    Require(new Vector2(walk.Feet.x,walk.Feet.z).magnitude>60 && Mathf.Abs(walk.Feet.y-pos.y)<.2f,"external workshop airlock open / "+portal.name+" / level "+pos.y);
                }
                walk.Teleport(RingWalkthrough.At(RingWalkthrough.CentreRadius,15,.08f),15);
                for(int j=0;j<60;j++)walk.Walk(Vector2.up,.1f,false);
                Require(new Vector2(walk.Feet.x,walk.Feet.z).magnitude<RingWalkthrough.OuterRadius,"wall collision prevents leaving between doorways");
                walk.Teleport(RingWalkthrough.At(RingWalkthrough.CentreRadius,0,.08f),0); walk.Vitals.Reset();
                walk.Walk(Vector2.up,.5f,false); float walking = Vector3.Distance(walk.Feet,RingWalkthrough.At(RingWalkthrough.CentreRadius,0,walk.Feet.y));
                walk.Teleport(RingWalkthrough.At(RingWalkthrough.CentreRadius,0,.08f),0); walk.Walk(Vector2.up,.5f,true);
                Require(Vector3.Distance(walk.Feet,RingWalkthrough.At(RingWalkthrough.CentreRadius,0,walk.Feet.y))>walking*1.6f && walk.Vitals.Stamina<100,"sprint is faster and uses isolated stamina");
                walk.SetMap(true); var before=walk.Feet; walk.Walk(Vector2.up,1,true);
                Require(walk.Feet==before,"map freezes movement");walk.SetMap(false);
                for(int level=0;level<2;level++)foreach(float angle in RingWalkthrough.LiftAngles)
                {
                    walk.Teleport(RingWalkthrough.At(RingWalkthrough.CentreRadius,angle,level*8+.08f),angle+180);
                    for(int j=0;j<35;j++)walk.Walk(Vector2.up,.1f,false);
                    float radius=new Vector2(walk.Feet.x,walk.Feet.z).magnitude;
                    Require(radius<27.5f && radius>25 && Mathf.Abs(walk.Feet.y-level*8)<.25f && walk.Area=="AUFZUG",
                        "boundary lift accessible from ring without crossing room wall / "+angle+" ring "+(level+1));
                    Require(walk.UseLift(),"boundary lift interaction / "+angle+" ring "+(level+1));walk.Tick(3.1f,false);
                    for(int j=0;j<20;j++)walk.Walk(Vector2.up,.1f,false);
                    Require(new Vector2(walk.Feet.x,walk.Feet.z).magnitude>29 && Mathf.Abs(walk.Feet.y-(1-level)*8)<.25f,
                        "lift exits onto other ring through separate portal / "+angle+" ring "+(level+1));
                }
                walk.Teleport(RingWalkthrough.At(RingWalkthrough.LiftRadius,135,.08f),135);
                Require(walk.UseLift() && walk.Riding,"lower lift starts");
                walk.Tick(1.5f,false);Require(walk.Feet.y>3 && walk.Feet.y<5,"lift carries player through shaft");
                walk.Tick(1.6f,false);Require(walk.Level==1 && !walk.Riding,"upper landing reached");
                walk.Walk(Vector2.up,1,false);Require(walk.Feet.y>7.8f,"upper platform and exit support player");
                walk.Teleport(RingWalkthrough.At(RingWalkthrough.LiftRadius,255,8.08f),255);
                Require(walk.UseLift(),"second shaft works from upper ring");walk.Tick(3.1f,false);
                Require(walk.Level==0,"return lift reaches lower ring");
                walk.Teleport(RingWalkthrough.At(RingWalkthrough.CentreRadius,0,.08f),90);
            })) yield break;
            yield return Capture("01-verkehrsring");
            walk.Teleport(RingWalkthrough.At(RingWalkthrough.OuterRadius+5,0,.08f),0);yield return Capture("02-kommandozentrale");
            walk.Teleport(RingWalkthrough.At(RingWalkthrough.OuterRadius+5,330,.08f),330);yield return Capture("03-wohnen");
            walk.Teleport(RingWalkthrough.At(RingWalkthrough.OuterRadius+RingWalkthrough.StageDepth-.3f,30,.08f),30);yield return Capture("04-drohnendeck");
            walk.Teleport(RingWalkthrough.At(RingWalkthrough.InnerRadius-2,30,.08f),210);yield return Capture("05-interne-werkstatt");
            walk.Teleport(RingWalkthrough.At(RingWalkthrough.InnerRadius-2,330,.08f),150);yield return Capture("06-versorgung");
            walk.Teleport(RingWalkthrough.At(RingWalkthrough.InnerRadius+2,135,.08f),315);yield return Capture("07-aufzug");
            walk.SetMap(true);walk.ApplyView(true);
            if (!Guard(()=> {
                foreach(float angle in new[]{0f,90,180,270})Require(walk.ContainsInView(RingWalkthrough.At(86,angle)),"map fits enlarged outer extent / "+angle);
            }))yield break;
            yield return Capture("08-grundriss");walk.SetMap(false);
            if (!Guard(() => {
                Require(water==scenario.WaterLiters && drone==scenario.Worker.transform.position,"campaign resources and drone unchanged");
                Require(habitat==StationInteriorMode.Current.FeetPosition,"campaign habitat pose unchanged");
                var settings=FindFirstObjectByType<SettingsMenu>();settings.Open();
                Require(!simulator.Walking&&!RingConstructionSimulator.IsOpen,"settings closes walkthrough cleanly");
                simulator.Open();simulator.EnterWalkthrough();
                Require(simulator.Walking&&!SettingsMenu.IsOpen,"walkthrough can reopen after settings");
                simulator.LeaveWalkthrough();Require(!simulator.Walking&&RingConstructionSimulator.IsOpen,"return to construction view");
                Require(display.HandleShortcut(true,false),"F11 shortcut also works in construction planning");
            }))yield break;
            deadline=Time.realtimeSinceStartup+5;
            while(display.IsChanging && Time.realtimeSinceStartup<deadline)yield return null;
            if (!Guard(()=>Require(display.IsFullscreen,"construction planning reaches fullscreen")))yield break;
            display.Toggle();deadline=Time.realtimeSinceStartup+5;
            while(display.IsChanging && Time.realtimeSinceStartup<deadline)yield return null;
            if (!Guard(() => {
                Require(!display.IsFullscreen,"construction fullscreen button path returns to window");
                simulator.EnterWalkthrough();simulator.Close();Require(!simulator.Walking&&RingConstructionSimulator.BlocksInput,"closing consumes input frame");
            })) yield break;
            yield return null;
            if (!Guard(()=>Require(!RingConstructionSimulator.BlocksInput,"controls released next frame")))yield break;
            Debug.Log("RING WALKTHROUGH CHECK PASSED: "+checks+" checks"); Application.Quit();
        }
    }
}
#endif
