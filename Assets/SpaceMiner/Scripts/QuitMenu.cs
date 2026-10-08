using UnityEngine;

namespace SpaceMiner
{
    [DefaultExecutionOrder(-900)]
    public sealed class QuitMenu : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        private static int closedFrame = -1;
        public static bool BlocksInput => IsOpen || closedFrame == Time.frameCount;
        private readonly SpaceMinerUi ui = new SpaceMinerUi();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetState() { IsOpen = false; closedFrame = -1; }

        public void Open()
        {
            if (StartMenu.IsOpen || IntroSequence.BlocksGameplay || SettingsMenu.BlocksInput) return;
            IsOpen = true;
        }
        public void Cancel() { IsOpen = false; closedFrame = Time.frameCount; }
        public void ReturnToStart() { Cancel(); FindFirstObjectByType<StartMenu>()?.ReturnToStart(); }
        private void Update()
        {
            if (SaveGameMenu.BlocksInput) return;
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            HandleEscape();
        }
        internal void HandleEscape()
        {
            if (StationInteriorMode.HasConsoleOpen && !SettingsMenu.BlocksInput) { StationInteriorMode.Current.CloseConsole(); return; }
            if (IsOpen) Cancel(); else Open();
        }
        private void OnGUI()
        {
            if (!IsOpen || SaveGameMenu.IsOpen) return;
            var matrix = GUI.matrix;
            int depth = GUI.depth;
            try
            {
                ui.Configure(SettingsStore.Current.Accessibility);
                GUI.depth = -300;
                float scale = Mathf.Min(Screen.width / 960f, Screen.height / 720f);
                GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
                float width = Screen.width / scale, height = Screen.height / scale;
                SpaceMinerUi.Fill(new Rect(0, 0, width, height), new Color(0, .01f, .025f, .7f));
                Rect panel = new Rect((width - 500) / 2, (height - 300) / 2, 500, 300);
                ui.Panel(panel);
                GUI.Label(new Rect(panel.x + 28, panel.y + 26, 444, 60), "Spiel beenden?", ui.Heading);
                GUI.Label(new Rect(panel.x + 28, panel.y + 94, 444, 80), "Das Spiel ist pausiert.\nZurück zum Startbildschirm?", ui.Text);
                if (GUI.Button(new Rect(panel.x + 28, panel.y + 212, 212, 54), "Weiterspielen", ui.Primary)) { Cancel(); SettingsUiAudio.Activate(); }
                if (GUI.Button(new Rect(panel.x + 260, panel.y + 212, 212, 54), "Beenden", ui.Button))
                {
                    Cancel(); GetComponent<SaveGameMenu>()?.Leave(ReturnToStart);
                }
            }
            finally { GUI.matrix = matrix; GUI.depth = depth; }
        }
        private void OnDestroy() { IsOpen = false; ui.Dispose(); }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool Check(System.Action action)
        {
            try { action(); return true; }
            catch (System.Exception error) { Debug.LogException(error); Application.Quit(1); return false; }
        }
        private System.Collections.IEnumerator Start()
        {
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-quitMenuCheck") < 0) yield break;
            yield return null;
            var scenario = FindFirstObjectByType<WaterScenario>();
            var intro = FindFirstObjectByType<IntroSequence>();
            bool pausePolicy = SettingsStore.Current.Gameplay.PauseInMenu;
            float rate = scenario.SimulationRate;
            try
            {
                intro.Skip();
                HandleEscape();
                if (!Check(() => { if (IsOpen) throw new System.Exception("Intro skip must not open quit dialog in same frame"); })) yield break;
                yield return null;
                SettingsStore.Current.Gameplay.PauseInMenu = false;
                scenario.SimulationRate = 500;
                HandleEscape();
                Vector3 position = scenario.Worker.transform.position;
                float water = scenario.WaterLiters;
                if (!Check(() =>
                {
                    if (!IsOpen || !SettingsMenu.PausesSimulation || !SettingsMenu.BlocksInput) throw new System.Exception("Quit dialog must pause regardless of menu policy");
                })) yield break;
                yield return new WaitForSecondsRealtime(.7f);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath, "../quit-menu.png"));
                yield return new WaitForSecondsRealtime(.3f);
                if (!Check(() =>
                {
                    if (scenario.Worker.transform.position != position || scenario.WaterLiters != water || scenario.SimulationRate != 500) throw new System.Exception("Quit dialog must preserve simulation state and speed");
                    HandleEscape();
                    if (IsOpen || !BlocksInput) throw new System.Exception("Cancel must consume closing frame");
                })) yield break;
                yield return null;
                if (!Check(() =>
                {
                    if (BlocksInput || SettingsMenu.PausesSimulation || scenario.SimulationRate != 500) throw new System.Exception("Cancel must resume previous speed");
                    scenario.SimulationRate = 0;
                    Open(); Cancel();
                    if (scenario.SimulationRate != 0) throw new System.Exception("Manual pause must survive dialog");
                    var config = DeveloperGameConfiguration.Capture(scenario);
                    config.SimulationRate = 700; config.Apply(scenario);
                    if (scenario.SimulationRate != 500) throw new System.Exception("Dev speed must cap at 500");
                    config.SimulationRate = -1; config.Apply(scenario);
                    if (scenario.SimulationRate != 0) throw new System.Exception("Dev zero must pause");
                })) yield break;
                ReturnToStart();yield return null;
                if(!Check(()=>{if(!StartMenu.IsOpen||IsOpen||!SettingsMenu.PausesSimulation)throw new System.Exception("Quit must return to paused start menu");}))yield break;
                Debug.Log("QUIT MENU AND SPEED CHECK PASSED: return to start screen");
                yield return new WaitForSecondsRealtime(.3f);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath, "../speed-pause.png"));
                yield return new WaitForSecondsRealtime(.3f);
                Application.Quit(0);
            }
            finally { SettingsStore.Current.Gameplay.PauseInMenu = pausePolicy; scenario.SimulationRate = rate; }
        }
#endif
    }
}
