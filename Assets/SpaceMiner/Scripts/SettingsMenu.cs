using UnityEngine;
using UnityEngine.SceneManagement;
namespace SpaceMiner
{
    [DefaultExecutionOrder(-2000)]
    public sealed class SettingsMenu : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        private static int closedFrame = -1;
        public static bool BlocksInput => IsOpen || closedFrame == Time.frameCount || TechTreeMenu.BlocksInput;
        public static bool PausesSimulation => BlocksInput && SettingsStore.Current.Gameplay.PauseInMenu;
        private readonly SettingsSession session = new SettingsSession();
        private SpaceMinerPlayerSettings draft => session.Draft;
        private int page; private int bindingCapture = -1;
        private static Rect EntryButton => new Rect(Screen.width - 52, 18, 34, 30);
        private readonly TechnologyIcons entryIcons = new TechnologyIcons();
        private Vector2 scroll;
        private string status = "PLAYER PREFERENCES // LOCAL STORAGE";
        private readonly SpaceMinerUi ui = new SpaceMinerUi();
        private GUIStyle heading => ui.Heading;
        private GUIStyle label => ui.Text;
        private GUIStyle action => ui.Button;
        private GUIStyle small => ui.Small;
        private AccessibilitySettings themeOverride;
        private Color statusColor = SpaceMinerUi.Cyan;
        private readonly string[] pages = { "Gameplay", "Graphics", "Audio", "Controls", "Interface", "Accessibility" };
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private DeveloperGameConfiguration developer => session.Developer;
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            IsOpen = false; closedFrame = -1;
            SceneManager.sceneLoaded -= SceneLoaded; SceneManager.sceneLoaded += SceneLoaded;
        }
        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (FindFirstObjectByType<SettingsMenu>() == null && FindFirstObjectByType<OrbitCamera>() != null)
                new GameObject("SpaceMiner Settings").AddComponent<SettingsMenu>();
        }
        private void Awake() { SettingsStore.Load(); gameObject.AddComponent<SettingsUiAudio>(); gameObject.AddComponent<TechTreeMenu>(); }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private System.Collections.IEnumerator Start()
        {
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-settingsPreview") < 0) yield break;
            yield return null;
            var intro = FindFirstObjectByType<IntroSequence>(); if (intro != null) intro.Skip();
            yield return new WaitForSecondsRealtime(.5f);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(System.Environment.CurrentDirectory, "Logs/settings-entry.png"));
            yield return new WaitForSecondsRealtime(.5f);
            Open();
            yield return new WaitForSecondsRealtime(2);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(System.Environment.CurrentDirectory, "Logs/settings-menu.png"));
            yield return new WaitForSecondsRealtime(1);
            for (int category = 1; category <= 6; category++)
            {
                if (category == 6 && developer == null) break;
                page = category; scroll = Vector2.zero;
                yield return new WaitForSecondsRealtime(.3f);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(System.Environment.CurrentDirectory, "Logs/settings-category-" + category + ".png"));
                yield return new WaitForSecondsRealtime(.3f);
            }
            themeOverride = new AccessibilitySettings { TextScale = 1.4f, HighContrast = true }; page = 5;
            yield return new WaitForSecondsRealtime(.3f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(System.Environment.CurrentDirectory, "Logs/settings-accessibility-large.png"));
            yield return new WaitForSecondsRealtime(.3f);
            themeOverride = null;
            Cancel();
            if (!BlocksInput) throw new System.Exception("Cancel must consume closing frame");
            yield return null;
            if (BlocksInput) throw new System.Exception("Cancel must release input next frame");
            Debug.Log("SETTINGS MENU PREVIEW AND CANCEL CHECK PASSED");
            Application.Quit();
        }
#endif
        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetKeyDown(KeyCode.F10)) { if (IsOpen) Cancel(); else Open(); }
#endif
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) { if (bindingCapture >= 0) bindingCapture = -1; else Cancel(); }
        }
        public void Open() { var tree = GetComponent<TechTreeMenu>(); if (TechTreeMenu.IsOpen && tree != null) tree.Close(); session.Begin(); scroll = Vector2.zero; IsOpen = true; status = "CORE ONLINE | SYSTEM NOMINAL | BUILD " + Application.version; statusColor = SpaceMinerUi.Cyan; }
        public void Cancel() { bindingCapture = -1; IsOpen = false; closedFrame = Time.frameCount; session.Cancel(); }
        public bool Apply()
        {
            if (!session.Apply(out string error)) { status = error; statusColor = SpaceMinerUi.Error; return false; }
            status = "EINSTELLUNGEN GESPEICHERT"; statusColor = SpaceMinerUi.Success; return true;
        }
        private void OnDestroy() { IsOpen = false; session.Cancel(); ui.Dispose(); entryIcons.Dispose(); }
        public static bool OwnsScreenPoint(Vector3 mouse) => IsOpen || TechTreeMenu.OwnsScreenPoint(mouse) || EntryButton.Contains(new Vector2(mouse.x, Screen.height - mouse.y));
        private void OnGUI()
        {
            var savedMatrix = GUI.matrix; int savedDepth = GUI.depth;
            try { DrawMenu(); }
            finally { GUI.matrix = savedMatrix; GUI.depth = savedDepth; }
        }
        private void DrawMenu()
        {
            GUI.tooltip = "";
            if (IsOpen && bindingCapture >= 0 && Event.current.type == EventType.KeyDown && Event.current.keyCode != KeyCode.Escape)
            { if (draft.Controls.Bindings.TryAssign((CameraAction)bindingCapture, Event.current.keyCode)) bindingCapture = -1; Event.current.Use(); }
            ui.Configure(themeOverride ?? SettingsStore.Current.Accessibility); int previousDepth = GUI.depth; GUI.depth = -200;
            if (!IsOpen)
            {
                if (TechTreeMenu.IsOpen) return;
                if (GUI.Button(EntryButton, new GUIContent("", "Einstellungen öffnen"), ui.Button)) { Open(); SettingsUiAudio.Activate(); }
                entryIcons.Draw(new Rect(EntryButton.x + 7, EntryButton.y + 5, 20, 20), "settings", SpaceMinerUi.Cyan);
                SettingsUiAudio.Observe(EntryButton, "settings-entry");
                ui.Tooltip(1, Screen.width, Screen.height);
                GUI.depth = previousDepth; return;
            }
            var previous = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1100f, Screen.height / 760f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float width = Screen.width / scale, height = Screen.height / scale;
            Fill(new Rect(0, 0, width, height), new Color(.005f, .012f, .03f, .91f));
            Rect panel = new Rect((width - 1060) / 2, (height - 700) / 2, 1060, 700);
            ui.Panel(panel);
            GUI.Label(new Rect(panel.x + 28, panel.y + 22, 1000, 44), "SPACE MINER // SYSTEM CONFIGURATION", heading);
            GUI.Label(new Rect(panel.x + 28, panel.y + 66, 800, 25), "MINING PULSE     /     OPERATOR CONSOLE     /     PERSONAL SYSTEMS", small);
            Fill(new Rect(panel.x + 24, panel.y + 102, 1012, 1), new Color(.15f, .7f, .9f, .6f));
            for (int i = 0; i < pages.Length; i++) Nav(panel, i, pages[i]);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (developer != null) Nav(panel, 6, "Developer");
            GUI.Label(new Rect(panel.x + 28, panel.y + 650, 150, 25), "DEV // F10", small);
#endif
            GUILayout.BeginArea(new Rect(panel.x + 40, panel.y + 180, 980, 390));
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label(page < 6 ? pages[page].ToUpperInvariant() : "DEVELOPER / GAME CONFIGURATION", heading);
            GUILayout.Space(20);
            switch (page)
            {
                case 0: draft.Gameplay.PauseInMenu = Toggle("Simulation im Menü pausieren", draft.Gameplay.PauseInMenu); draft.Gameplay.ShowControlHints = Toggle("Steuerungshinweise anzeigen", draft.Gameplay.ShowControlHints); break;
                case 1:
                    draft.Graphics.VSync = Toggle("VSync", draft.Graphics.VSync);
                    draft.Graphics.FrameLimit = Mathf.RoundToInt(Slider("FPS-Limit (ohne VSync)", draft.Graphics.FrameLimit, 30, 240));
                    int quality = draft.Graphics.Quality < 0 ? SettingsStore.DefaultQuality : draft.Graphics.Quality;
                    
                    draft.Graphics.Quality = ui.Dropdown("Grafikqualität", quality, QualitySettings.names);
                                        int mode = draft.Graphics.DisplayMode < 0 ? DisplayModeController.ModeIndex(Screen.fullScreenMode) : draft.Graphics.DisplayMode;
                    int chosenMode = ui.Dropdown("Anzeigemodus", mode, new[] { "Fenster", "Randlos", "Vollbild" });
                    if (chosenMode != mode) draft.Graphics.DisplayMode = chosenMode;
                    var sizes = DisplayModeController.ResolutionChoices();
                    int resolution = sizes.FindIndex(size => size.x == draft.Graphics.Width && size.y == draft.Graphics.Height);
                    if (resolution < 0) resolution = 0;
                    var names = sizes.ConvertAll(size => size.x == 0 ? "Aktuelle Auflösung" : size.x + " × " + size.y).ToArray();
                    int chosenResolution = ui.Dropdown("Auflösung", resolution, names);
                    if (chosenResolution != resolution) { draft.Graphics.Width = sizes[chosenResolution].x; draft.Graphics.Height = sizes[chosenResolution].y; }
                    GUILayout.Label(Application.isEditor ? "Anzeigemodus und Auflösung werden im Windows-Spiel angewendet." : "Fenster / Vollbild: F11 oder Alt + Enter", small); break;
                case 2:
                    AudioRow("Gesamtlautstärke", ref draft.Audio.Master, ref draft.Audio.MasterEnabled);
                    AudioRow("Hintergrund / Musik", ref draft.Audio.Background, ref draft.Audio.BackgroundEnabled);
                    AudioRow("Effekte", ref draft.Audio.Effects, ref draft.Audio.EffectsEnabled);
                    AudioRow("Stimmen / Mira", ref draft.Audio.Voices, ref draft.Audio.VoicesEnabled);
                    AudioRow("UI / technische Klicks", ref draft.Audio.Ui, ref draft.Audio.UiEnabled);
                    session.PreviewAudio();
                    GUILayout.Label("Direkt hörbare Vorschau · Cancel stellt die gespeicherten Werte wieder her.", small);
                    break;
                case 3:
                    draft.Controls.Sensitivity = Slider("Mausempfindlichkeit", draft.Controls.Sensitivity, .2f, 3);
                    draft.Controls.CameraSpeed = Slider("Kamerageschwindigkeit", draft.Controls.CameraSpeed, .2f, 3);
                    draft.Controls.ZoomSpeed = Slider("Zoomgeschwindigkeit", draft.Controls.ZoomSpeed, .2f, 3);
                    draft.Controls.InvertY = Toggle("Vertikale Kameraachse invertieren", draft.Controls.InvertY);
                    GUILayout.Label("Kamera-Tastenbelegung", heading);
                    string[] bindingNames = { "Vorwärts", "Rückwärts", "Links", "Rechts", "Aufwärts", "Abwärts", "Schiffsansicht", "Übersicht", "Fokus", "HUD ein/aus" };
                    for (int index = 0; index < bindingNames.Length; index++)
                    {
                        string key = bindingCapture == index ? "Taste drücken … (Esc bricht ab)" : draft.Controls.Bindings.Keys[index].ToString();
                        if (GUILayout.Button(bindingNames[index] + "    " + key, action)) bindingCapture = index;
                    }
                    GUILayout.Label("Buchstaben, Ziffern und Pfeiltasten. Doppelte Belegungen werden getauscht. Shift, Esc, Tab, Space, F10/F11 bleiben Systemtasten.", small);
                    break;
                case 4:
                    draft.Interface.Scale = Slider("HUD-Skalierung", draft.Interface.Scale, .75f, 1.5f);
                    draft.Interface.ShowHud = Toggle("HUD anzeigen", draft.Interface.ShowHud); break;
                case 5:
                    draft.Accessibility.Subtitles = Toggle("Untertitel im Mira-Intro", draft.Accessibility.Subtitles);
                    draft.Accessibility.HighContrast = Toggle("Stärkerer Menü-Kontrast", draft.Accessibility.HighContrast);
                    draft.Accessibility.TextScale = Slider("Menü-Textgröße", draft.Accessibility.TextScale, 1, 1.4f); break;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                case 6:
                    GUILayout.Label("Live-Testwerte // nur für diese Spielsitzung", small);
                    developer.SimulationRate = Slider("Simulationsgeschwindigkeit", developer.SimulationRate, 0, 1000);
                    developer.MiningRate = Slider("Abbaurate kg/s", developer.MiningRate, .01f, 10); break;
#endif
            }
            GUILayout.EndScrollView(); GUILayout.EndArea();
            ui.Status(new Rect(panel.x + 190, panel.y + 650, 840, 42), status, statusColor);
            if (ActionButton(new Rect(panel.x + 260, panel.y + 582, 260, 56), "RESTORE DEFAULTS")) { session.RestoreDefaults(); session.PreviewAudio(); }
            if (ActionButton(new Rect(panel.x + 540, panel.y + 582, 180, 56), "CANCEL")) Cancel();
            if (ActionButton(new Rect(panel.x + 740, panel.y + 582, 290, 56), "APPLY CHANGES")) Apply();
            ui.Tooltip(scale, width, height);
            GUI.matrix = previous; GUI.depth = previousDepth;
        }
        private void Nav(Rect panel, int index, string name)
        {
            var rect = new Rect(panel.x + 24 + index * 145, panel.y + 120, 140, 44);
            if(index == 5) name = "Access";
            if (ui.Tab(rect, name.ToUpperInvariant(), page == index)) { page = index; scroll = Vector2.zero; GUIUtility.ExitGUI(); }
        }
        private bool Toggle(string name, bool value) => ui.Toggle(name, value, name);
        private bool ActionButton(Rect rect, string name)
        { bool clicked = GUI.Button(rect,name,name == "APPLY CHANGES" ? ui.Primary : action); SettingsUiAudio.Observe(rect,name); if(clicked) SettingsUiAudio.Activate(); return clicked; }
        private void AudioRow(string name, ref float volume, ref bool enabled)
        {
            var audioCard = new GUIStyle(GUI.skin.box) { padding = new RectOffset(6,6,3,3) };
            GUILayout.BeginVertical(audioCard);
            GUILayout.BeginHorizontal(); GUILayout.Label(name + "    " + Mathf.RoundToInt(volume * 100) + " %", label);
            bool changed = GUILayout.Toggle(enabled, enabled ? "[ ON ]" : "[ OFF ]", ui.ToggleStyle, GUILayout.Width(125), GUILayout.Height(30));
            SettingsUiAudio.Observe(GUILayoutUtility.GetLastRect(),name);
            if(changed != enabled) { enabled = changed; session.PreviewAudio(); SettingsUiAudio.Activate(); }
            GUILayout.EndHorizontal();
            volume = GUILayout.HorizontalSlider(volume,0,1,ui.Track,ui.Thumb,GUILayout.Height(22));
            GUILayout.EndVertical(); GUILayout.Space(4);
        }
        private float Slider(string name, float value, float min, float max) => ui.Slider(name, value, min, max, name);
        private static void Fill(Rect rect, Color color) => SpaceMinerUi.Fill(rect, color);
    }
}
