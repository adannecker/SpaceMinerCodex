using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceMiner
{
    public sealed class StationUiAudioCheck : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private string folder;
        private int checks;
        private StationInteriorMode mode;
        private WaterScenario scenario;
        private StationInteractionAudio audioSystem;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-stationUiAudioCheck") >= 0 || Array.IndexOf(args, "-stationUiAudioPreview") >= 0)
                SceneManager.sceneLoaded += Loaded;
        }
        static void Loaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= Loaded;
            new GameObject("Station audio and UI check").AddComponent<StationUiAudioCheck>();
        }
        private void Require(bool condition, string label)
        { checks++; if (!condition) throw new Exception("Station UI/audio: " + label); }
        private bool Guard(Action action)
        {
            try { action(); return true; }
            catch (Exception error) { Debug.LogException(error); Application.Quit(1); return false; }
        }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.25f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png"));
            yield return new WaitForSecondsRealtime(.3f);
        }
        private void RoomPose(Vector3 local, bool console = false)
        {
            mode.RestoreInterior(mode.Room.TransformPoint(local), console, mode.Room.eulerAngles.y, 0);
            scenario.Scanner.DismissDialogue();
        }
        private IEnumerator Start()
        {
            folder = Path.GetFullPath("Logs/StationUiAudio"); Directory.CreateDirectory(folder);
            yield return new WaitForSecondsRealtime(1);
            mode = StationInteriorMode.Current;
            scenario = FindFirstObjectByType<WaterScenario>();
            var saves = FindFirstObjectByType<SaveGameMenu>();
            saves.StorageDirectoryOverride = Path.Combine(folder, "Saves");
            FindFirstObjectByType<StartMenu>().StartDemo(); FindFirstObjectByType<IntroSequence>().Skip();
            yield return null; yield return null;
            scenario.SetSimulationRate(0);
            audioSystem = StationInteractionAudio.Current;
            bool preview = Array.IndexOf(Environment.GetCommandLineArgs(), "-stationUiAudioPreview") >= 0;
            if (preview)
            {
                Screen.SetResolution(1280, 800, FullScreenMode.Windowed);
                Debug.Log("STATION UI/AUDIO PREVIEW: isolated settings and saves in " + folder);
                yield break;
            }
            if (!Guard(() =>
            {
                Require(mode != null && audioSystem != null, "single installed station audio system");
                Require(FindObjectsByType<StationInteractionAudio>(FindObjectsSortMode.None).Length == 1, "no duplicate audio component");
                Require(SettingsStore.PathName.StartsWith(folder), "settings isolated from personal preferences");
                Require(SettingsStore.Save(new SpaceMinerPlayerSettings(), out _), "test defaults apply");
                mode.Airlock.ResetClosed(); audioSystem.Synchronize();
                Require(audioSystem.Ambient.isPlaying && audioSystem.Ambient.loop, "habitat ambience plays");
                Require(!audioSystem.DoorSource(0).isPlaying && !audioSystem.Pressure.isPlaying, "idle airlock silent");
                var samples = new float[audioSystem.Ambient.clip.samples]; audioSystem.Ambient.clip.GetData(samples, 0);
                float energy = 0, peak = 0;
                foreach (float sample in samples) { energy += sample * sample; peak = Mathf.Max(peak, Mathf.Abs(sample)); }
                Require(energy / samples.Length > .001f && peak < .99f, "original ambience contains signal without clipping");
                float toneError = 0;
                for (int i = 0; i < samples.Length; i++)
                {
                    float t = (float)i / audioSystem.Ambient.clip.frequency;
                    toneError = Mathf.Max(toneError, Mathf.Abs(samples[i] - (.07f * Mathf.Sin(2 * Mathf.PI * 60 * t) + .025f * Mathf.Sin(2 * Mathf.PI * 120 * t))));
                }
                Require(toneError < .000001f, "continuous ambience has no broadband noise");
                Require(Mathf.Sqrt(energy / samples.Length) * audioSystem.Ambient.volume < .002f, "very quiet continuous room tone at full effects gain");
                var mute = SettingsStore.Current.Copy().Audio; mute.EffectsEnabled = false;
                PlayerAudio.Preview(mute); audioSystem.Synchronize();
                Require(audioSystem.Ambient.volume == 0 && audioSystem.Ambient.isPlaying, "effects mute preserves ambience playback");
                PlayerAudio.EndPreview(); audioSystem.Synchronize();
                Require(audioSystem.Ambient.volume > 0, "cancel restores effects level");
                mute.MasterEnabled = false; PlayerAudio.Preview(mute);
                Require(AudioListener.volume == 0, "master mute applies to new sources"); PlayerAudio.EndPreview();
                RoomPose(new Vector3(0, .025f, -4));
                Require(mode.Airlock.Request(mode.FeetPosition), "real airlock entry request"); audioSystem.Synchronize();
                Require(audioSystem.DoorSource(0).isPlaying && !audioSystem.DoorSource(1).isPlaying, "only entry motor audible");
            })) yield break;
            yield return new WaitForSecondsRealtime(.12f);
            var openingClip = audioSystem.DoorSource(0).clip;
            if (!Guard(() =>
            {
                FindFirstObjectByType<SettingsMenu>().Open(); audioSystem.Synchronize();
                Require(!audioSystem.DoorSource(0).isPlaying, "menu pause stops motor");
                FindFirstObjectByType<SettingsMenu>().Cancel();
            })) yield break;
            yield return null;
            if (!Guard(() =>
            {
                audioSystem.Synchronize(); Require(audioSystem.DoorSource(0).isPlaying, "motor resumes after menu");
                mode.Airlock.Advance(2, mode.FeetPosition); audioSystem.Synchronize();
                Require(!audioSystem.DoorSource(0).isPlaying, "motor stops at open limit");
                RoomPose(new Vector3(0, .025f, -7.7f));
                mode.Airlock.Advance(.01f, mode.FeetPosition); audioSystem.Synchronize();
                Require(audioSystem.DoorSource(0).isPlaying && audioSystem.DoorSource(0).clip != openingClip, "distinct closing servo");
                mode.Airlock.Advance(2, mode.FeetPosition); audioSystem.Synchronize();
                Require(audioSystem.Pressure.isPlaying && !audioSystem.DoorSource(0).isPlaying, "pressure cycle audible after sealed door");
                mode.Airlock.Advance(1.3f, mode.FeetPosition); audioSystem.Synchronize();
                Require(audioSystem.DoorSource(1).isPlaying && !audioSystem.Pressure.isPlaying, "exit motor replaces pressure cycle");
                mode.Exit(); audioSystem.Synchronize();
                Require(!audioSystem.Ambient.isPlaying && !audioSystem.DoorSource(1).isPlaying && !audioSystem.Pressure.isPlaying, "no physical station ambience in VR");
                Require(audioSystem.Signals.isPlaying, "VR activation signal plays");
                mode.Enter();
                Require(audioSystem.Signals.isPlaying, "VR deactivation signal plays");
                // Both camera spaces must survive normal switches; a new session still resets.
                mode.RestoreInterior(mode.Room.TransformPoint(new Vector3(0,.025f,-7.7f)), false, mode.Room.eulerAngles.y+53, -16);
                var feet = mode.FeetPosition; float yaw = mode.ViewYaw, pitch = mode.ViewPitch;
                var airlockState = mode.Airlock.CaptureState();
                mode.Exit();
                var orbit = FindFirstObjectByType<OrbitCamera>();
                orbit.Orbit(new Vector2(37,12)); orbit.Zoom(-3); orbit.Pan(new Vector2(90,45));
                var cameraPosition = orbit.transform.position; var cameraRotation = orbit.transform.rotation;
                float cameraFov = orbit.GetComponent<Camera>().fieldOfView;
                mode.Enter();
                Require(Vector3.Distance(feet,mode.FeetPosition)<.001f && Mathf.Approximately(yaw,mode.ViewYaw) && Mathf.Approximately(pitch,mode.ViewPitch), "VR return preserves interior feet and look");
                Require(JsonUtility.ToJson(mode.Airlock.CaptureState())==JsonUtility.ToJson(airlockState), "VR return preserves airlock cycle");
                scenario.Scanner.OpenVirtualView();
                Require(Vector3.Distance(cameraPosition,orbit.transform.position)<.001f && Quaternion.Angle(cameraRotation,orbit.transform.rotation)<.001f && Mathf.Approximately(cameraFov,orbit.GetComponent<Camera>().fieldOfView), "scanner VR button preserves exterior camera instead of resetting");
                scenario.Scanner.ReturnToStation();
                Require(Vector3.Distance(feet,mode.FeetPosition)<.001f, "scanner return preserves interior location");
                scenario.Scanner.ResetProgress();
                Require(Vector3.Distance(mode.FeetPosition,mode.Room.TransformPoint(new Vector3(0,.05f,-2.5f)))<.001f && mode.Airlock.Phase==AirlockPhase.Idle, "new demo starts at default pose with closed airlock");
                RoomPose(new Vector3(0, .025f, 1.5f));
                Require(mode.OpenConsole(), "console interaction with sound");
                Require(audioSystem.Signals.isPlaying, "console opening signal"); mode.CloseConsole();
                scenario.Scanner.Advance(10);
                Require(mode.OpenConsole() && scenario.Scanner.Scan(), "scanner success with confirmation sound");
                Require(audioSystem.Signals.isPlaying, "scan completion signal"); mode.CloseConsole();
                for (int i = 0; i < 3; i++)
                {
                    float requested = new[] { .75f, 1f, 1.5f }[i];
                    foreach (var size in new[] { new Vector2(800,600), new Vector2(1280,800), new Vector2(1920,1080), new Vector2(3840,2160) })
                    {
                        float scale = UiLayout.Fit(size.x,size.y,1440,900,requested);
                        Require(1440 * scale <= size.x + .01f && 900 * scale <= size.y + .01f, "HUD fits " + size + " at " + requested);
                        if (size.x >= 3840) Require(Mathf.Approximately(scale,requested), "4K respects requested pixel scale");
                    }
                }
                var session = new SettingsSession(); session.Begin(); session.Draft.Interface.Scale = .75f;
                UiLayout.Preview(session.Draft.Interface.Scale); Require(UiLayout.RequestedScale == .75f, "live scale preview");
                session.Cancel(); Require(UiLayout.RequestedScale == 1, "Cancel restores scale");
                session.Begin(); session.Draft.Interface.Scale = 1.25f; UiLayout.Preview(1.25f);
                Require(session.Apply(out _), "Apply persists scale");
                var restarted = new PlayerSettingsService(new LocalPlayerSettingsStorage(SettingsStore.PathName), _ => { });
                Require(restarted.Load(out _) && restarted.Current.Interface.Scale == 1.25f, "scale survives settings service restart");
                session.Begin(); session.RestoreDefaults(); Require(UiLayout.RequestedScale == 1, "Defaults previews scale");
                session.Cancel(); Require(UiLayout.RequestedScale == 1.25f, "Cancel Defaults restores applied scale");
            })) yield break;
            var display = FindFirstObjectByType<DisplayModeController>();
            var native = DisplayModeController.MonitorResolution;
            bool exclusiveAvailable = false;
            Debug.Log("DISPLAY REGRESSION: native monitor " + native);
            foreach (int index in new[] { 2, 1 })
            {
                display.ApplySettings(new GraphicsSettings { DisplayMode=0, Width=1280, Height=800 });
                while (display.IsChanging) yield return null;
                if (!Guard(() => Require(Screen.width==1280 && Screen.height==800 && !Screen.fullScreen,"window resolution before fullscreen"))) yield break;
                display.ApplySettings(new GraphicsSettings { DisplayMode=index, Width=0, Height=0 });
                while (display.IsChanging) yield return null;
                yield return new WaitForEndOfFrame();
                if (!Guard(() =>
                {
                    bool fullscreenMode = Screen.fullScreenMode==DisplayModeController.Mode(index)
                        || index==2 && Screen.fullScreenMode==FullScreenMode.FullScreenWindow;
                    if(index==2)exclusiveAvailable=Screen.fullScreenMode==FullScreenMode.ExclusiveFullScreen;
                    Debug.Log("DISPLAY RESULT: requested="+DisplayModeController.Mode(index)+", actual="+Screen.fullScreenMode+", pixels="+Screen.width+"x"+Screen.height);
                    Require(fullscreenMode && Screen.width==native.x && Screen.height==native.y,"native fullscreen framebuffer including supported Windows fallback, requested " + index);
                    var pixels = ScreenCapture.CaptureScreenshotAsTexture();
                    Require(pixels.width==native.x && pixels.height==native.y,"captured fullscreen pixels match monitor " + index);
                    File.WriteAllBytes(Path.Combine(folder,"native-fullscreen-"+index+".png"),pixels.EncodeToPNG()); Destroy(pixels);
                })) yield break;
                display.SetFullscreen(false);
                while (display.IsChanging) yield return null;
                if (!Guard(() => Require(Screen.width==1280 && Screen.height==800 && !Screen.fullScreen,"fullscreen toggle restores previous window"))) yield break;
                display.SetFullscreen(true);
                while (display.IsChanging) yield return null;
                if (!Guard(() => Require(Screen.width==native.x && Screen.height==native.y && Screen.fullScreen,"F11 path uses native resolution"))) yield break;
                display.ApplySettings(new GraphicsSettings { DisplayMode=0, Width=0, Height=0 });
                while (display.IsChanging) yield return null;
                if (!Guard(() => Require(Screen.width==1280 && Screen.height==800 && !Screen.fullScreen,"automatic settings restore window size"))) yield break;
            }
            var sizes = new[] { new Vector2Int(800,600), new Vector2Int(1280,800), new Vector2Int(1920,1080), new Vector2Int(3840,2160) };
            foreach (var size in sizes)
            {
                Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);
                yield return new WaitForSecondsRealtime(.5f);
                foreach (float scale in new[] { .75f, 1.5f })
                {
                    var preferences = SettingsStore.Current.Copy(); preferences.Interface.Scale = scale;
                    preferences.Accessibility.TextScale = 1.4f;
                    if (!Guard(() => Require(SettingsStore.Save(preferences,out _), "apply screenshot scale"))) yield break;
                    string id = size.x + "x" + size.y + "-" + scale.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture);
                    RoomPose(new Vector3(0,.025f,-2.5f));
                    scenario.Scanner.Say("ERSTER AUFTRAG · UMGEBUNG SCANNEN", "Geh an die Konsole und führe einen Nahbereichsscan aus. Der Scanner ist zu 97 Prozent geladen. In zehn Sekunden ist er bereit. Die Aussenansicht ist ein virtueller Raum: Dort siehst du nur, was wir bereits gescannt haben.");
                    yield return Capture(id + "-mira-quest");
                    RoomPose(new Vector3(0,.025f,1.5f)); mode.OpenConsole();
                    yield return Capture(id + "-console");
                    mode.CloseConsole(); mode.Exit();
                    yield return null;
                    if (!Guard(() =>
                    {
                        float effective = UiLayout.Scale();
                        Vector3 button = new Vector3((UiLayout.Width*.5f)*effective, Screen.height-(UiLayout.Height-32)*effective);
                        Require(StationInteriorMode.OwnsScreenPoint(button), "scaled view switch hit test " + id);
                        Vector3 settingsButton = new Vector3((UiLayout.Width-35)*effective,Screen.height-33*effective);
                        Require(SettingsMenu.OwnsScreenPoint(settingsButton), "scaled settings hit test " + id);
                        Vector3 treeButton = new Vector3((UiLayout.Width-79)*effective,Screen.height-33*effective);
                        Require(TechTreeMenu.OwnsScreenPoint(treeButton), "scaled technology hit test " + id);
                    })) yield break;
                    yield return Capture(id + "-vr");
                    var menu = FindFirstObjectByType<SettingsMenu>(); menu.Open();
                    menu.ShowInterfacePage(); yield return Capture(id + "-settings");
                    menu.Cancel(); yield return null;
                }
            }
            var defaults = new SpaceMinerPlayerSettings(); SettingsStore.Save(defaults,out _);
            File.WriteAllText(Path.Combine(folder,"result.json"),"{\"passed\":true,\"checks\":" + checks + ",\"screenshots\":34,\"nativeWidth\":"+native.x+",\"nativeHeight\":"+native.y+",\"exclusiveFullscreenAvailable\":"+exclusiveAvailable.ToString().ToLowerInvariant()+"}");
            Debug.Log("STATION UI/AUDIO CHECK PASSED: " + checks + " checks, 34 UI/fullscreen screenshots");
            Application.Quit(0);
        }
#endif
    }
}
