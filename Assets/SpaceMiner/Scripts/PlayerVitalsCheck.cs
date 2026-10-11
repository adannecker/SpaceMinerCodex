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
        private IEnumerator Start()
        {
            folder = Path.GetFullPath("Logs/PlayerVitals"); Directory.CreateDirectory(folder);
            yield return new WaitForSecondsRealtime(1);
            var mode = StationInteriorMode.Current;
            var scenario = FindFirstObjectByType<WaterScenario>();
            var saves = FindFirstObjectByType<SaveGameMenu>();
            saves.StorageDirectoryOverride = Path.Combine(folder, "Saves");
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
            Vector3 spawn = mode.Room.TransformPoint(new Vector3(0, .05f, -2.5f));
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
                mode.RestoreInterior(spawn, false, mode.Room.eulerAngles.y, 0); mode.Vitals.Reset();
                mode.Walk(Vector2.up, .5f); float walk = Vector3.ProjectOnPlane(mode.FeetPosition-spawn, Vector3.up).magnitude;
                mode.RestoreInterior(spawn, false, mode.Room.eulerAngles.y, 0); mode.Vitals.Reset();
                mode.Walk(Vector2.up, .5f, true); float run = Vector3.ProjectOnPlane(mode.FeetPosition-spawn, Vector3.up).magnitude;
                Require(Mathf.Abs(walk - 1.1f) < .03f && Mathf.Abs(run - 5.2f) < .03f && Mathf.Abs(mode.Vitals.Stamina - 91) < .01f, "development CharacterController walks at 2.2 m/s, sprints at 10.4 m/s and retains stamina drain");
                float reserve = mode.Vitals.Stamina;
                mode.Walk(Vector2.zero, .25f, true);
                Require(mode.Vitals.Stamina == reserve, "standing actual player does not consume stamina");
                settings.Open(); Vector3 position = mode.FeetPosition;
                mode.Walk(Vector2.up, 10, true);
                Require(mode.FeetPosition == position && mode.Vitals.Stamina == reserve, "modal menu freezes motion and stamina");
                settings.Cancel();
            })) yield break;
            yield return null; yield return null;
            if (!Guard(() =>
            {
                mode.Vitals.Reset(); mode.Vitals.Advance(100f/18, true, true);
                mode.RestoreInterior(spawn, false, mode.Room.eulerAngles.y, 0);
                mode.Walk(Vector2.up, .5f, true);
                Require(Vector3.ProjectOnPlane(mode.FeetPosition-spawn,Vector3.up).magnitude < 1.2f && mode.Vitals.Exhausted, "exhausted actual player remains at walking speed");
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
                mode.RestoreInterior(spawn,false,mode.Room.eulerAngles.y,0);
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
