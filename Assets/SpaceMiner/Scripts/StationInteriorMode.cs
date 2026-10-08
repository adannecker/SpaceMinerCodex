using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceMiner
{
    // Connected habitat, airlock and ring. Local walking gravity is a presentation assumption.
    [RequireComponent(typeof(OrbitCamera), typeof(Camera))]
    [DefaultExecutionOrder(150)]
    public sealed class StationInteriorMode : MonoBehaviour
    {
        public static StationInteriorMode Current { get; private set; }
        public static bool IsInside => Current != null && Current.inside;
        public static bool HasConsoleOpen => IsInside && Current.consoleOpen;
        public Vector3 FeetPosition => walker != null ? walker.transform.position : Vector3.zero;
        public Transform Room { get; private set; }
        public StationInteriorLayout Layout { get; private set; }
        public StationAirlock Airlock => Layout != null ? Layout.Airlock : null;
        public bool ConsoleOpen => consoleOpen;
        public bool NearConsole => inside && Layout != null && Vector3.Distance(FeetPosition, Layout.Console.position) <= 2.4f;
        public void RestoreInterior(Vector3 feet, bool console) { Enter(true); if (!inside) return; walker.enabled=false; walker.transform.position=feet; walker.enabled=true; consoleOpen=console && NearConsole; UpdateCursor(); }
        private OrbitCamera orbit;
        private Camera view;
        private CharacterController walker;
        private bool inside, captureMouse;
        private bool consoleOpen;
        private WaterScenario scenario;
        private AsteroidResource consoleSource;
        private Vector2 consoleScroll;
        private int cursorChangedFrame = -1;
        private float yaw, pitch, verticalSpeed, outsideFov;
        private CursorLockMode outsideCursorLock;
        private bool outsideCursorVisible;
        private readonly SpaceMinerUi ui = new SpaceMinerUi();
        private static Rect SwitchRect => new Rect(Screen.width * .5f - 120, Screen.height - 48, 240, 32);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            Current = null;
            SceneManager.sceneLoaded -= Loaded;
            SceneManager.sceneLoaded += Loaded;
        }
        static void Loaded(Scene scene, LoadSceneMode mode)
        {
            var camera = FindFirstObjectByType<OrbitCamera>();
            if (camera != null && camera.GetComponent<StationInteriorMode>() == null)
                camera.gameObject.AddComponent<StationInteriorMode>();
        }
        private void Awake() { Current = this; orbit = GetComponent<OrbitCamera>(); view = GetComponent<Camera>(); }
        private void Start() => BuildRoom();

        private void BuildRoom()
        {
            if (Room != null) return;
            var station = FindFirstObjectByType<StationVisual>();
            if (station == null) return;
            var root = new GameObject("Connected station interior");
            root.transform.SetParent(station.transform, false);
            Layout = root.AddComponent<StationInteriorLayout>();
            Layout.Build(station);
            Room = Layout.Habitat;
            scenario = station.GetComponent<WaterScenario>();
            var player = new GameObject("Interior walker", typeof(CharacterController));
            walker = player.GetComponent<CharacterController>();
            walker.height = 1.8f; walker.radius = .3f; walker.center = new Vector3(0, .9f, 0);
            walker.skinWidth = .025f; walker.stepOffset = .2f; walker.minMoveDistance = 0;
            walker.enabled = false;
            Physics.SyncTransforms();
        }
        public bool Enter(bool duringIntro = false)
        {
            if (inside || (!duringIntro && (StartMenu.IsOpen || IntroSequence.BlocksGameplay || SettingsMenu.BlocksInput || MemoryCinematic.IsPlaying))) return false;
            BuildRoom();
            if (Room == null || walker == null) return false;
            outsideFov = view.fieldOfView; outsideCursorLock = Cursor.lockState; outsideCursorVisible = Cursor.visible;
            walker.transform.position = Room.TransformPoint(new Vector3(0, .05f, -2.5f));
            walker.enabled = true;
            Airlock.ResetClosed();
            yaw = Room.eulerAngles.y; pitch = 0; verticalSpeed = 0;
            inside = true; captureMouse = true; orbit.enabled = false;
            ApplyView(); UpdateCursor();
            return true;
        }
        public void Exit()
        {
            if (!inside) return;
            consoleOpen = false; Airlock.ResetClosed();
            inside = false; walker.enabled = false; orbit.enabled = true;
            view.fieldOfView = outsideFov; orbit.RestoreViewPose();
            Cursor.lockState = outsideCursorLock; Cursor.visible = outsideCursorVisible;
        }
        public void Walk(Vector2 input, float seconds, bool fast = false)
        {
            if (!inside || consoleOpen || SettingsMenu.BlocksInput || seconds <= 0) return;
            Vector3 direction = Quaternion.Euler(0, yaw, 0) * new Vector3(input.x, 0, input.y);
            if (walker.isGrounded) verticalSpeed = -2;
            else verticalSpeed = Mathf.Max(-10, verticalSpeed - 9.81f * seconds);
            walker.Move((Vector3.ClampMagnitude(direction, 1) * (fast ? 4f : 2.2f) + Vector3.up * verticalSpeed) * seconds);
        }
        public void Look(Vector2 degrees)
        {
            yaw += degrees.x; pitch = Mathf.Clamp(pitch - degrees.y, -80, 80);
        }
        public bool OpenConsole()
        {
            if (!inside || consoleOpen || SettingsMenu.BlocksInput || Vector3.Distance(FeetPosition, Layout.Console.position) > 2.4f) return false;
            consoleOpen = true; consoleScroll = Vector2.zero;
            foreach (var source in scenario.Asteroids) if (source.CanMineWater) { consoleSource = source; break; }
            UpdateCursor(); return true;
        }
        public void CloseConsole() { consoleOpen = false; UpdateCursor(); }
        public bool AcceptWaterOrder(AsteroidResource source) => inside && consoleOpen && !SettingsMenu.BlocksInput && scenario.AssignTankOrder(source);
        private bool Near(Transform target, float distance) => target != null && Vector3.Distance(FeetPosition, target.position) < distance;
        private void Update()
        {
            if (StartMenu.IsOpen || MemoryCinematic.IsPlaying)
            { if (inside) Exit(); return; }
            if (IntroSequence.BlocksGameplay) return;
            if (!SettingsMenu.BlocksInput && Input.GetKeyDown(KeyCode.V)) { if (inside) Exit(); else Enter(); }
            if (!inside) return;
            UpdateCursor();
            if (SettingsMenu.BlocksInput) return;
            if (consoleOpen)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E)) CloseConsole();
                return;
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (Near(Layout.Console, 2.4f)) OpenConsole();
                else if (Airlock.CanRequest(FeetPosition)) Airlock.Request(FeetPosition);
            }
            if (Input.GetKeyDown(KeyCode.Tab)) { captureMouse = !captureMouse; UpdateCursor(); }
            if (!captureMouse || !Application.isFocused || scenario.Scanner.Dialogue != null) return;
            float sensitivity = SettingsStore.Current.Controls.Sensitivity * 2;
            if (Time.frameCount != cursorChangedFrame)
            {
                Look(new Vector2(Input.GetAxisRaw("Mouse X") * sensitivity, Input.GetAxisRaw("Mouse Y") * sensitivity * (SettingsStore.Current.Controls.InvertY ? -1 : 1)));
            }
            Walk(new Vector2((PlayerInput.Held(CameraAction.Right) ? 1 : 0) - (PlayerInput.Held(CameraAction.Left) ? 1 : 0),
                (PlayerInput.Held(CameraAction.Forward) ? 1 : 0) - (PlayerInput.Held(CameraAction.Backward) ? 1 : 0)), Time.unscaledDeltaTime,
                Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        }
        private void LateUpdate() { if (inside) ApplyView(); }
        private void ApplyView()
        {
            transform.SetPositionAndRotation(FeetPosition + Vector3.up * 1.6f, Quaternion.Euler(pitch, yaw, 0));
            view.fieldOfView = 70; view.nearClipPlane = .08f; view.farClipPlane = 1200000;
            QualitySettings.shadowDistance = 32;
        }
        private void UpdateCursor()
        {
            bool locked = inside && captureMouse && !consoleOpen && (scenario.Scanner.Dialogue == null || IntroSequence.IsPlaying) && !SettingsMenu.BlocksInput && Application.isFocused;
            var state = locked ? CursorLockMode.Locked : CursorLockMode.None;
            if (Cursor.lockState != state) { Cursor.lockState = state; cursorChangedFrame = Time.frameCount; }
            if (Cursor.visible == locked) Cursor.visible = !locked;
        }
        public static bool OwnsScreenPoint(Vector3 mouse) => !StartMenu.IsOpen && SwitchRect.Contains(new Vector2(mouse.x, Screen.height - mouse.y));
        private void OnGUI()
        {
            if (StartMenu.IsOpen || IntroSequence.BlocksGameplay || SettingsMenu.BlocksInput || MemoryCinematic.IsPlaying) return;
            ui.Configure(SettingsStore.Current.Accessibility);
            if (GUI.Button(SwitchRect, inside ? "Station verlassen  [V]" : "Station betreten  [V]", ui.Button)) { if (inside) Exit(); else Enter(); }
            if (!inside) return;
            ui.Panel(new Rect(18, Screen.height - 116, Mathf.Min(600, Screen.width - 36), 56));
            GUI.Label(new Rect(30, Screen.height - 108, Screen.width - 60, 44), "INNENANSICHT  ·  Wohnmodul / Schleuse / Stationsring\nWASD + Maus  ·  E Interaktion  ·  Tab Maus freigeben  ·  V Außenansicht", ui.Small);
            string prompt = Near(Layout.Console, 2.4f) ? "E  Stationspult bedienen"
                : Airlock.CanRequest(FeetPosition) ? "E  Schleusendurchgang starten"
                : Near(Layout.RepairDrone, 1.7f) ? "R-01  ·  Reparaturdrohne ausgeschaltet" : "";
            Vector3 local = Room.InverseTransformPoint(FeetPosition);
            if (local.z < -3.1f && local.z > -12.4f && Mathf.Abs(local.x) < 1.5f && Airlock.Phase != AirlockPhase.Idle) prompt = Airlock.Status;
            if (!string.IsNullOrEmpty(prompt)) GUI.Label(new Rect(Screen.width * .5f - 230, Screen.height - 164, 460, 40), prompt, ui.Text);
            if (Cursor.lockState == CursorLockMode.Locked) GUI.Label(new Rect(Screen.width / 2f - 5, Screen.height / 2f - 10, 20, 20), "+", ui.Small);
            if (consoleOpen) DrawConsole();
        }
        private void DrawConsole()
        {
            int depth = GUI.depth; GUI.depth = -30;
            float width = Mathf.Min(820, Screen.width - 32), height = Mathf.Min(650, Screen.height - 32);
            Rect panel = new Rect((Screen.width - width) * .5f, (Screen.height - height) * .5f, width, height);
            SpaceMinerUi.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0, .01f, .02f, .65f)); ui.Panel(panel);
            GUILayout.BeginArea(new Rect(panel.x + 24, panel.y + 20, width - 48, height - 40));
            consoleScroll = GUILayout.BeginScrollView(consoleScroll);
            GUILayout.Label("STATIONSPULT  /  SCANNER & AUFTRÄGE", ui.Heading);
            var scanner = scenario.Scanner;
            GUILayout.Label("Nahbereich: 10 km · Scanner " + (scanner.Charge * 100).ToString("F1") + " %", ui.Text);
            bool scanEnabled=GUI.enabled; GUI.enabled=scanEnabled && scanner.Ready && !scanner.FirstScanComplete;
            if (GUILayout.Button("Nahbereichsscan ausführen", ui.Primary)) scanner.Scan();
            GUI.enabled=scanEnabled;
            GUILayout.Label("Stationstank: " + scenario.WaterLiters.ToString("F1") + " / " + scenario.TankCapacityLiters.ToString("F0") + " L\nDrohne 01: " + scenario.Worker.Status, ui.Text);
            GUILayout.Space(12); GUILayout.Label("VERSORGUNGSAUFTRAG: WASSER SICHERN", ui.Text);
            GUILayout.Label(scenario.QuestComplete ? "Auftrag erfüllt: Tank voll." : "Eine bestätigte Eisquelle zuweisen und den Stationstank mit der Bergbaudrohne befüllen.", ui.Small);
            GUILayout.BeginHorizontal();
            foreach (var source in scenario.Asteroids)
                if (source.WaterIdentified && GUILayout.Button(source.Info.DisplayName, consoleSource == source ? ui.Primary : ui.Button)) consoleSource = source;
            GUILayout.EndHorizontal();
            if (consoleSource != null) GUILayout.Label("Quelle: " + consoleSource.Info.DisplayName + "  ·  Wasseranteil " + (consoleSource.WaterFraction * 100).ToString("F0") + "%", ui.Small);
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && consoleSource != null && consoleSource.CanMineWater && scenario.Worker.IsReady && !scenario.QuestComplete;
            if (GUILayout.Button("Wasserauftrag übernehmen", ui.Primary)) AcceptWaterOrder(consoleSource);
            GUI.enabled = previousEnabled && scenario.Worker.HasTankOrder;
            if (GUILayout.Button("Auftrag abbrechen und Drohne zurückrufen", ui.Button)) scenario.Worker.ReturnToShip();
            GUI.enabled = previousEnabled;
            GUILayout.Label(scenario.Message + "\nLieferungen: " + scenario.Deliveries + "  ·  Batterie: " + scenario.Worker.BatteryKwh.ToString("F1") + " kWh", ui.Small);
            GUILayout.Space(12); GUILayout.Label("STEUERUNG", ui.Text);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(scenario.SimulationRate == 0 ? "Simulation fortsetzen" : "Simulation pausieren", ui.Button)) scenario.TogglePause();
            foreach (float rate in new[] { 1f, 5f }) if (GUILayout.Button(rate.ToString("F0") + "×", ui.Button)) scenario.SetSimulationRate(rate);
            GUILayout.EndHorizontal(); GUILayout.BeginHorizontal();
            if (GUILayout.Button("Außenansicht", ui.Button)) Exit();
            if (GUILayout.Button("Drohne verfolgen", ui.Button)) { Exit(); orbit.Focus(scenario.Worker.Info); }
            GUILayout.EndHorizontal();
            GUILayout.Label("R-01: kleine Reparaturdrohne, ausgeschaltet.", ui.Small);
            if (GUILayout.Button("Pult schließen  [E / Escape]", ui.Button)) CloseConsole();
            GUILayout.EndScrollView(); GUILayout.EndArea(); GUI.depth = depth;
        }
        private void OnDestroy()
        {
            if (inside) { Cursor.lockState = outsideCursorLock; Cursor.visible = outsideCursorVisible; }
            if (walker != null) Destroy(walker.gameObject);
            if (Layout != null) Destroy(Layout.gameObject);
            if (Current == this) Current = null;
            ui.Dispose();
        }
    }
}
