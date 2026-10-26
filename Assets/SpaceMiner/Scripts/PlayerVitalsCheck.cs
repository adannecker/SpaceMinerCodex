using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceMiner
{
    public sealed class PlayerVitalsCheck : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private int checks;
        private string folder;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-playerVitalsCheck") >= 0) SceneManager.sceneLoaded += Loaded;
        }
        private static void Loaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= Loaded;
            new GameObject("Player stamina and HUD check").AddComponent<PlayerVitalsCheck>();
        }
        private void Require(bool condition, string label)
        { checks++; if (!condition) throw new Exception("Player vitals: " + label); }
        private bool Guard(Action action)
        {
            try { action(); return true; }
            catch (Exception error) { Debug.LogException(error); Application.Quit(1); return false; }
        }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png"));
            yield return new WaitForSecondsRealtime(.3f);
        }
        private void CheckJump(StationInteriorMode mode, WaterScenario scenario, Vector3 spawn, float yaw, SettingsMenu settings)
        {
            mode.RestoreInterior(spawn, false, yaw, 0);
            for (int i = 0; i < 40; i++) mode.Walk(Vector2.zero, .01f);
            Vector3 floor = mode.FeetPosition;
            Require(mode.IsGrounded, "jump fixture stands on the real ring floor");
            mode.HandleJumpInput(false, true);
            Require(!mode.HandleJumpInput(true, false), "unfocused Space cannot jump");
            Require(!mode.HandleJumpInput(true, true), "regaining focus while Space is held cannot defer a jump");
            mode.HandleJumpInput(false, false);
            Require(!mode.HandleJumpInput(true, true), "focus regain cannot turn a reset input state into a jump edge");
            mode.HandleJumpInput(false, true);
            Require(mode.HandleJumpInput(true, true), "fresh grounded Space starts the actual interior jump");
            float launch = mode.VerticalVelocity;
            Require(!mode.HandleJumpInput(true, true) && mode.VerticalVelocity == launch, "holding Space cannot add another launch impulse");
            for (int i = 0; i < 12; i++) mode.Walk(Vector2.zero, .01f);
            float height = mode.FeetPosition.y - floor.y;
            Require(height > .15f && mode.VerticalVelocity > 0, "real CharacterController rises after jumping");
            bool airSpamRejected = true;
            for (int i = 0; i < 8; i++)
            {
                mode.HandleJumpInput(false, true); float before = mode.VerticalVelocity;
                airSpamRejected &= !mode.HandleJumpInput(true, true) && mode.VerticalVelocity == before;
            }
            Require(airSpamRejected, "airborne repeated Space cannot boost velocity");
            float peak = height;
            bool heldRejected = true;
            for (int i = 0; i < 140; i++)
            {
                heldRejected &= !mode.HandleJumpInput(true, true);
                mode.Walk(Vector2.zero, .01f); peak = Mathf.Max(peak, mode.FeetPosition.y - floor.y);
            }
            Require(heldRejected, "held Space remains one press across flight and landing");
            Require(peak > .40f && peak <= StationInteriorMode.JumpHeight + .06f, "small jump follows the bounded configured height");
            Require(mode.IsGrounded && Mathf.Abs(mode.FeetPosition.y - floor.y) < .04f, "jump lands on the same actual floor without falling through");
            Require(mode.Vitals.Stamina == 100 && !mode.Vitals.Exhausted, "developer jump and landing preserve full stamina");
            mode.HandleJumpInput(false, true);
            scenario.Scanner.Say("SPRUNGPRÜFUNG", "Temporärer Dialog für die Eingabesicherheit.");
            Require(!mode.HandleJumpInput(true, true), "Mira dialogue blocks jumping");
            Vector3 paused = mode.FeetPosition; mode.Walk(Vector2.zero, .2f);
            Require(mode.FeetPosition == paused, "Mira dialogue cannot advance an interior jump");
            scenario.Scanner.DismissDialogue();
            Require(!mode.HandleJumpInput(true, true), "dismissing Mira while Space is held does not launch a deferred jump");
            mode.HandleJumpInput(false, true);
            Require(mode.HandleJumpInput(true, true), "release then fresh press after Mira allows jumping again");
            mode.RestoreInterior(spawn, false, yaw, 0);
            for (int i = 0; i < 40; i++) mode.Walk(Vector2.zero, .01f);
            floor = mode.FeetPosition;
            // Explicit temporary ceiling fixture; the real campaign controller
            // and its normal collision response remain responsible for impact.
            var ceiling = new GameObject("Jump acceptance fixture / low ceiling", typeof(BoxCollider));
            var collider = ceiling.GetComponent<BoxCollider>(); ceiling.layer = 25;
            ceiling.transform.position = floor + Vector3.up * 2.10f; collider.size = new Vector3(3, .10f, 3);
            Physics.SyncTransforms();
            try
            {
                Require(Physics.Raycast(floor + Vector3.up * 1.85f, Vector3.up, out var hit, .4f, 1 << 25)
                    && hit.collider == collider, "low ceiling is actual blocking physics geometry");
                mode.HandleJumpInput(false, true);
                Require(mode.HandleJumpInput(true, true), "grounded jump can begin below a low ceiling");
                peak = 0; bool stoppedUpward = false;
                for (int i = 0; i < 140; i++)
                {
                    mode.Walk(Vector2.zero, .01f);
                    float current = mode.FeetPosition.y - floor.y; peak = Mathf.Max(peak, current);
                    if (current > .1f && mode.VerticalVelocity <= 0) stoppedUpward = true;
                }
                Require(peak > .10f && peak < .33f && stoppedUpward, "ceiling impact stops upward velocity without penetrating the ceiling");
                Require(mode.IsGrounded && Mathf.Abs(mode.FeetPosition.y - floor.y) < .04f, "ceiling impact returns safely to the floor");
            }
            finally { collider.enabled = false; Destroy(ceiling); Physics.SyncTransforms(); }
            mode.HandleJumpInput(false, true);
            settings.Open();
            Require(!mode.HandleJumpInput(true, true), "settings modal blocks the fresh jump press");
            settings.Cancel();
            Require(!mode.HandleJumpInput(true, true), "closing a modal with Space held does not queue a jump");
            mode.HandleJumpInput(false, true);
        }
        private IEnumerator Start()
        {
            folder = Path.GetFullPath("Logs/PlayerVitals"); Directory.CreateDirectory(folder);
            yield return new WaitForSecondsRealtime(1);
            var mode = StationInteriorMode.Current;
            var scenario = FindFirstObjectByType<WaterScenario>();
            var saves = FindFirstObjectByType<SaveGameMenu>();
            saves.StorageDirectoryOverride = Path.Combine(folder, "Saves");
            CinematicLibrary.PreferencePrefix="SpaceMiner.Test.PlayerVitals.";
            var settings = FindFirstObjectByType<SettingsMenu>();
            if (!Guard(() =>
            {
                Require(mode != null && scenario != null, "interior and scenario installed");
                Require(SettingsStore.PathName.StartsWith(folder), "settings isolated");
                Require(SettingsStore.Save(new SpaceMinerPlayerSettings(), out _), "apply isolated defaults");
                FindFirstObjectByType<StartMenu>().StartDemo(); FindFirstObjectByType<IntroSequence>().Skip();
            })) yield break;
            yield return null; yield return null;
            scenario.Scanner.DismissDialogue(); scenario.SetSimulationRate(0);
            // The old workshop lane now contains material bins. Measure speed on a clear ring lane.
            Vector3 spawn = mode.Layout.transform.TransformPoint(RingWalkthrough.At(33.5f,330,.05f));
            float testYaw=mode.Layout.transform.eulerAngles.y+60;
            if (!Guard(() =>
            {
                var body = new PlayerVitals();
                Require(body.IsValid && body.Stamina == 100 && body.Health == 100 && body.Fatigue == 0 && body.Hunger == 0 && body.Thirst == 0, "healthy defaults and need direction");
                Require(body.Advance(1, true, false) == 0 && body.Stamina == 100, "shift while standing does not drain");
                body.Advance(1, true, true);
                Require(Mathf.Abs(body.Stamina - 82) < .001f && body.IsSprinting, "moving sprint consumes stamina");
                body.Advance(1, false, false);
                Require(body.Stamina == 82, "recovery waits");
                body.Advance(1, false, false);
                Require(Mathf.Abs(body.Stamina - 88) < .001f, "recovery uses remaining part of delayed interval");
                body.Reset(); float available = body.Advance(10, true, true);
                Require(Mathf.Abs(available - 100f / PlayerVitals.SprintDrain) < .001f && body.Exhausted, "long step sprints only while available");
                body.Advance(10, true, true);
                Require(body.Stamina == 100 && !body.IsSprinting && body.Exhausted, "holding shift after exhaustion recovers without burst loop");
                body.Advance(.01f, false, false); body.Advance(.1f, true, true);
                Require(body.IsSprinting, "release and adequate recovery allow next sprint");
                Require(body.Fatigue == 0 && body.Hunger == 0 && body.Thirst == 0 && body.Health == 100, "unimplemented needs do not secretly punish player");
                float before = body.Stamina; body.Advance(float.NaN, true, true); body.Advance(-1, true, true);
                Require(body.Stamina == before, "invalid time ignored");
                mode.RestoreInterior(spawn, false, testYaw, 0); mode.Vitals.Reset();
                mode.Walk(Vector2.up, .1f); float walk = Vector3.ProjectOnPlane(mode.FeetPosition-spawn, Vector3.up).magnitude;
                mode.RestoreInterior(spawn, false, testYaw, 0); mode.Vitals.Reset();
                mode.Walk(Vector2.up, .1f, true); float run = Vector3.ProjectOnPlane(mode.FeetPosition-spawn, Vector3.up).magnitude;
                Require(Mathf.Abs(walk - .66f) < .03f && Mathf.Abs(run - 2.08f) < .03f && mode.Vitals.Stamina == 100, "development station walks at 6.6 m/s and sprints at 20.8 m/s without stamina drain");
                float reserve = mode.Vitals.Stamina;
                mode.Walk(Vector2.zero, .25f, true);
                Require(mode.Vitals.Stamina == reserve, "standing actual player does not consume stamina");
                settings.Open(); Vector3 position = mode.FeetPosition;
                mode.Walk(Vector2.up, 10, true);
                Require(mode.FeetPosition == position && mode.Vitals.Stamina == reserve, "modal menu freezes motion and stamina");
                settings.Cancel();
            })) yield break;
            yield return null; yield return null;
            if (!Guard(() => CheckJump(mode, scenario, spawn, testYaw, settings))) yield break;
            yield return null; yield return null;
            if (!Guard(() =>
            {
                mode.Vitals.Reset(); mode.Vitals.Advance(100f/18, true, true);
                mode.RestoreInterior(spawn, false, testYaw, 0);
                mode.Walk(Vector2.up, .1f, true);
                Require(Mathf.Abs(Vector3.ProjectOnPlane(mode.FeetPosition-spawn,Vector3.up).magnitude - 2.08f) < .03f && !mode.Vitals.Exhausted && mode.Vitals.Stamina == 100, "developer movement clears saved exhaustion and permits immediate sprint");
                mode.Vitals.Exhausted = true; mode.Vitals.RecoveryDelay = .7f;
                mode.Vitals.Stamina = 43; mode.Vitals.Fatigue = 12; mode.Vitals.Hunger = 27; mode.Vitals.Thirst = 34; mode.Vitals.Health = 91;
                var state = SaveGameStore.Capture(scenario,"Vitals check", "isolated");
                mode.Vitals.Stamina = 99;
                Require(state.playerVitals.Stamina == 43, "save capture is detached snapshot");
                var path=SaveGameStore.Write(state, false, Path.Combine(folder,"Saves"));
                var restored=SaveGameStore.Read(path); SaveGameStore.Apply(restored,scenario);
                Require(mode.Vitals.Stamina == 43 && mode.Vitals.Fatigue == 12 && mode.Vitals.Hunger == 27 && mode.Vitals.Thirst == 34 && mode.Vitals.Health == 91, "all five values survive file save/load");
                Require(mode.Vitals.Exhausted && mode.Vitals.RecoveryDelay > 0, "exhaustion and delay survive loading");
                restored.playerVitals.Stamina = -1; bool rejected=false;
                try { SaveGameStore.Validate(restored,scenario); } catch(IOException) { rejected=true; }
                Require(rejected && mode.Vitals.Stamina == 43, "invalid saved needs rejected before mutation");
                restored.playerVitals = null; SaveGameStore.Apply(restored,scenario);
                Require(mode.Vitals.Stamina == 100 && mode.Vitals.Health == 100, "legacy save without vitals has safe defaults");
                mode.Vitals.Stamina=25; mode.Exit();
                mode.Walk(Vector2.up,5,true);
                Require(mode.Vitals.Stamina==25, "exterior does not sprint or consume");
                mode.Enter(); Require(mode.Vitals.Stamina==25, "view switch preserves stamina");
                scenario.ResetScenario(); Require(mode.Vitals.Stamina==100 && !mode.Vitals.Exhausted, "new scenario resets body");
                scenario.Scanner.DismissDialogue();
                mode.RestoreInterior(spawn,false,testYaw,0);
                mode.Vitals.Stamina=43; mode.Vitals.Fatigue=12; mode.Vitals.Hunger=27; mode.Vitals.Thirst=34; mode.Vitals.Health=91;
            })) yield break;
            foreach(var size in new[]{new Vector2Int(1600,1000),new Vector2Int(1280,800)})
            {
                Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);
                yield return new WaitForSecondsRealtime(.5f);
                var preferences=SettingsStore.Current.Copy(); preferences.Interface.Scale=1.5f; preferences.Accessibility.TextScale=1.4f;
                if(!Guard(()=>{
                    Require(SettingsStore.Save(preferences,out _),"large UI/text preferences apply");
                    var rect=mode.VitalsHudRect;
                    Require(rect.xMin>=0 && rect.yMin>=0 && rect.xMax<UiLayout.Width*.5f-120 && rect.yMax<=UiLayout.Height,"vitals fit lower left clear of mode button");
                }))yield break;
                yield return Capture(size.x+"x"+size.y+"-large-text");
            }
            SettingsStore.Save(new SpaceMinerPlayerSettings(),out _);
            mode.Vitals.Reset(); scenario.Scanner.DismissDialogue();
            File.WriteAllText(Path.Combine(folder,"result.json"),"{\"passed\":true,\"checks\":"+checks+",\"screenshots\":2}");
            Debug.Log("PLAYER VITALS CHECK PASSED: "+checks+" checks; sprint, exhaustion, recovery, modal pause, persistence and HUD");
            Application.Quit(0);
        }
#endif
    }
}
