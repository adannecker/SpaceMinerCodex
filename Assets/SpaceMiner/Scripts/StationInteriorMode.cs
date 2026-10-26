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
        public float ViewYaw => yaw;
        public float ViewPitch => pitch;
        public PlayerVitals Vitals { get; private set; } = new PlayerVitals();
        public StationFirstPersonHands Hands { get; private set; }
        public const float JumpHeight = .55f;
        private const float Gravity = 9.81f;
        internal float VerticalVelocity => verticalSpeed;
        internal bool IsGrounded => walker != null && walker.enabled && walker.isGrounded;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal const float DeveloperWalkSpeed = 6.6f, DeveloperSprintSpeed = 20.8f;
#endif
        internal Rect VitalsHudRect => new Rect(18, UiLayout.Height - VitalsHudHeight - 18, 280, VitalsHudHeight);
        private float VitalRowHeight => Mathf.Max(30, 14 * SettingsStore.Current.Accessibility.TextScale * 1.3f + 13);
        private float VitalsHudHeight => 42 + VitalRowHeight * 5;
        public void RestoreVitals(PlayerVitals saved) => Vitals = saved != null && saved.IsValid ? saved.Copy() : new PlayerVitals();
        public void RestoreInterior(Vector3 feet, bool console, float? savedYaw=null, float? savedPitch=null)
        {
            Enter(true); if (!inside) return;
            walker.enabled=false; walker.transform.position=Layout.ContainsPosition(feet) && !Layout.OverlapsCommandBox(feet) ? feet : Layout.SpawnPosition; walker.enabled=true;
            verticalSpeed=0;
            ResetSafeGround();
            if(savedYaw.HasValue)yaw=savedYaw.Value;
            if(savedPitch.HasValue)pitch=Mathf.Clamp(savedPitch.Value,-80,80);
            consoleOpen=console && NearConsole;
            ApplyView();UpdateCursor();
        }
        public void RevalidateRoomPlanPosition()
        {
            // Editing a box must not leave the walker embedded in it. Normal viewpoints stay put.
            if (!inside || walker == null || Layout == null || !Layout.OverlapsCommandBox(FeetPosition)) return;
            RestoreInterior(Layout.SpawnPosition, false, yaw, pitch);
        }
        private OrbitCamera orbit;
        private Camera view;
        private Light helmetLight;
        private CharacterController walker;
        private bool inside, captureMouse;
        private bool consoleOpen;
        private bool hasInteriorPose;
        private WaterScenario scenario;
        private AsteroidResource consoleSource;
        private Vector2 consoleScroll;
        private int cursorChangedFrame = -1;
        private float yaw, pitch, verticalSpeed, outsideFov;
        private Vector3 lastSafeGround;
        private bool hasSafeGround;
        private bool jumpWasHeld, jumpHadFocus;
        internal int FallRecoveries { get; private set; }
        private CursorLockMode outsideCursorLock;
        private bool outsideCursorVisible;
        private readonly SpaceMinerUi ui = new SpaceMinerUi();
        private static Rect SwitchRect => new Rect(UiLayout.Width * .5f - 120, UiLayout.Height - 48, 240, 32);

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
        private void Awake()
        {
            Current = this; orbit = GetComponent<OrbitCamera>(); view = GetComponent<Camera>();
            if (GetComponent<StationInteractionAudio>() == null) gameObject.AddComponent<StationInteractionAudio>();
            Hands = GetComponent<StationFirstPersonHands>() ?? gameObject.AddComponent<StationFirstPersonHands>();
            Hands.Initialize(this, view);
        }
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
            var lamp = new GameObject("Helmet work light"); lamp.transform.SetParent(view.transform, false);
            helmetLight = lamp.AddComponent<Light>(); helmetLight.type = LightType.Point;
            helmetLight.color = new Color(.78f, .88f, 1); helmetLight.intensity = 2.5f;
            helmetLight.range = 28; helmetLight.cullingMask = (1 << 25) | 1; helmetLight.enabled = false;
            Physics.SyncTransforms();
        }
        public bool Enter(bool duringIntro = false)
        {
            if (inside || (!duringIntro && (StartMenu.IsOpen || IntroSequence.BlocksGameplay || SettingsMenu.BlocksInput || MemoryCinematic.IsPlaying))) return false;
            BuildRoom();
            if (Room == null || walker == null) return false;
            outsideFov = view.fieldOfView; outsideCursorLock = Cursor.lockState; outsideCursorVisible = Cursor.visible;
            if (duringIntro || !hasInteriorPose)
            {
                walker.transform.position = Layout.SpawnPosition;
                Airlock.ResetClosed();
                yaw = Layout.SpawnYaw; pitch = 0;
            }
            walker.enabled = true;
            hasInteriorPose = true; verticalSpeed = 0;
            inside = true; captureMouse = true; orbit.enabled = false;
            RevalidateRoomPlanPosition();
            if(duringIntro || !hasSafeGround)ResetSafeGround();
            RecoverFallenWalker();
            helmetLight.enabled = true;
            if (!duringIntro) StationInteractionAudio.Play(StationSound.VrOff);
            ApplyView(); UpdateCursor();
            return true;
        }
        public void Exit(bool silent = false)
        {
            if (!inside) return;
            if (!silent) StationInteractionAudio.Play(StationSound.VrOn);
            consoleOpen = false;
            Vitals.IsSprinting = false;
            inside = false; walker.enabled = false; orbit.enabled = true;
            helmetLight.enabled = false;
            view.fieldOfView = outsideFov; orbit.RestoreViewPose();
            Cursor.lockState = outsideCursorLock; Cursor.visible = outsideCursorVisible;
        }
        public void Walk(Vector2 input, float seconds, bool fast = false)
        {
            if (MovementBlocked || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            Vector3 direction = Quaternion.Euler(0, yaw, 0) * new Vector3(input.x, 0, input.y);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Vitals.Stamina = 100; Vitals.RecoveryDelay = 0; Vitals.Exhausted = false;
            Vitals.IsSprinting = fast && input.sqrMagnitude > .001f;
            float distance = (fast ? DeveloperSprintSpeed : DeveloperWalkSpeed) * seconds;
#else
            float sprintSeconds = Vitals.Advance(seconds, fast, input.sqrMagnitude > .001f);
            float distance = PlayerVitals.WalkSpeed * seconds + (PlayerVitals.SprintSpeed - PlayerVitals.WalkSpeed) * sprintSeconds;
#endif
            if (walker.isGrounded && verticalSpeed <= 0) verticalSpeed = -2;
            else verticalSpeed = Mathf.Max(-10, verticalSpeed - Gravity * seconds);
            RememberSafeGround();
            CollisionFlags collision = walker.Move(Vector3.ClampMagnitude(direction, 1) * distance + Vector3.up * verticalSpeed * seconds);
            if ((collision & CollisionFlags.Above) != 0 && verticalSpeed > 0) verticalSpeed = 0;
            if ((collision & CollisionFlags.Below) != 0 && verticalSpeed < 0) verticalSpeed = -2;
            if(!RecoverFallenWalker())RememberSafeGround();
        }
        private bool MovementBlocked => !inside || consoleOpen || SettingsMenu.BlocksInput || IntroSequence.BlocksGameplay
            || StartMenu.IsOpen || MemoryCinematic.IsPlaying || scenario?.Scanner?.Dialogue != null
            || StationOperations.TacticalTextInputActive || StationOperations.DroneConsoleTextInputActive;
        private bool JumpPressed(bool held, bool hasFocus)
        {
            bool pressed = held && !jumpWasHeld && hasFocus && jumpHadFocus;
            // Consume the edge even while a menu/dialogue owns input. Closing it
            // with Space held must not turn that press into a deferred jump.
            jumpWasHeld = held;
            jumpHadFocus = hasFocus;
            return pressed;
        }
        private bool TryJump(bool hasFocus)
        {
            if (!hasFocus || !captureMouse || MovementBlocked || !IsGrounded || verticalSpeed > 0) return false;
            verticalSpeed = Mathf.Sqrt(2 * Gravity * JumpHeight);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Vitals.Stamina = 100; Vitals.RecoveryDelay = 0; Vitals.Exhausted = false;
#endif
            return true;
        }
        // Same edge/guard path as native Space input; the explicit focus state
        // lets the isolated controller check cover focus loss without OS input.
        internal bool HandleJumpInput(bool held, bool hasFocus) => JumpPressed(held, hasFocus) && TryJump(hasFocus);
        private void ResetSafeGround()
        {
            lastSafeGround=Layout.SpawnPosition;hasSafeGround=true;
            RememberSafeGround();
        }
        private bool SupportedSafeGround(Vector3 feet)
        {
            if(!Layout.ContainsPosition(feet) || Layout.OverlapsCommandBox(feet))return false;
            // Only real station-floor collision qualifies. The player is on another layer,
            // so this ray cannot mistake the CharacterController itself for supporting floor.
            return Physics.Raycast(feet+Vector3.up*.3f,Vector3.down,out var hit,.65f,1<<25,QueryTriggerInteraction.Ignore)
                && hit.normal.y>.7f && Mathf.Abs(hit.point.y-feet.y)<.2f;
        }
        private void RememberSafeGround()
        {
            if(SupportedSafeGround(FeetPosition)){lastSafeGround=FeetPosition;hasSafeGround=true;}
        }
        internal bool RecoverFallenWalker()
        {
            if(!inside || walker==null || Layout==null)return false;
            Vector3 feet=FeetPosition,local=Layout.transform.InverseTransformPoint(feet);
            bool finite=!float.IsNaN(local.x)&&!float.IsNaN(local.y)&&!float.IsNaN(local.z)
                && !float.IsInfinity(local.x)&&!float.IsInfinity(local.y)&&!float.IsInfinity(local.z);
            // This is a final guard below the single built deck, not a horizontal movement
            // fence. Valid repaired floors, the airlock and the outside dock remain walkable.
            if(finite && local.y>=-1.25f)return false;
            Vector3 target=hasSafeGround&&SupportedSafeGround(lastSafeGround)&&Layout.HasStandingClearance(lastSafeGround)
                ? lastSafeGround:Layout.SpawnPosition;
            walker.enabled=false;walker.transform.position=target;walker.enabled=true;verticalSpeed=0;
            lastSafeGround=target;hasSafeGround=true;FallRecoveries++;Vitals.IsSprinting=false;
            Physics.SyncTransforms();return true;
        }
        public void Look(Vector2 degrees)
        {
            yaw += degrees.x; pitch = Mathf.Clamp(pitch - degrees.y, -80, 80);
        }
        public bool OpenConsole()
        {
            if (!inside || consoleOpen || SettingsMenu.BlocksInput || Vector3.Distance(FeetPosition, Layout.Console.position) > 2.4f) return false;
            consoleOpen = true; consoleScroll = Vector2.zero;
            Vitals.IsSprinting = false;
            StationInteractionAudio.Play(StationSound.ConsoleOpen);
            foreach (var source in scenario.Asteroids) if (source.CanMineWater) { consoleSource = source; break; }
            UpdateCursor(); return true;
        }
        public void CloseConsole() { if (consoleOpen) StationInteractionAudio.Play(StationSound.ConsoleClose); consoleOpen = false; UpdateCursor(); }
        public bool AcceptWaterOrder(AsteroidResource source) => inside && consoleOpen && !SettingsMenu.BlocksInput && scenario.AssignTankOrder(source);
        private bool Near(Transform target, float distance) => target != null && Vector3.Distance(FeetPosition, target.position) < distance;
        private void Update()
        {
            Vitals.IsSprinting = false;
            bool jumpPressed = JumpPressed(Input.GetKey(KeyCode.Space), Application.isFocused);
            if (StartMenu.IsOpen || MemoryCinematic.IsPlaying)
            { if (inside) Exit(true); return; }
            if (IntroSequence.BlocksGameplay) return;
            if (!SettingsMenu.BlocksInput && !StationOperations.TacticalTextInputActive && Input.GetKeyDown(KeyCode.V)) { if (inside) Exit(); else Enter(); }
            if (!inside) return;
            UpdateCursor();
            if (SettingsMenu.BlocksInput) return;
            if (PlayerInput.Pressed(CameraAction.ToggleHud)) orbit.ShowHud = !orbit.ShowHud;
            if (consoleOpen)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E)) CloseConsole();
                return;
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (Near(Layout.Console, 2.4f)) OpenConsole();
                else if (Airlock.CanRequest(FeetPosition)) Airlock.Request(FeetPosition);
                else if (Layout.Geometry.NearestGate(FeetPosition)!=null) { var gate=Layout.Geometry.NearestGate(FeetPosition);gate.Request(!gate.TargetOpen,FeetPosition); }
                else if (Layout.Geometry.DetailedHall!=null && Layout.Geometry.DetailedHall.Interact(FeetPosition)) { }
            }
            if (Input.GetKeyDown(KeyCode.Tab)) { captureMouse = !captureMouse; UpdateCursor(); }
            if (!captureMouse || !Application.isFocused || scenario.Scanner.Dialogue != null) return;
            if (jumpPressed) TryJump(Application.isFocused);
            float sensitivity = SettingsStore.Current.Controls.Sensitivity * 2;
            if (Time.frameCount != cursorChangedFrame)
            {
                Look(new Vector2(Input.GetAxisRaw("Mouse X") * sensitivity, Input.GetAxisRaw("Mouse Y") * sensitivity * (SettingsStore.Current.Controls.InvertY ? -1 : 1)));
            }
            Walk(new Vector2((PlayerInput.Held(CameraAction.Right) ? 1 : 0) - (PlayerInput.Held(CameraAction.Left) ? 1 : 0),
                (PlayerInput.Held(CameraAction.Forward) ? 1 : 0) - (PlayerInput.Held(CameraAction.Backward) ? 1 : 0)), Time.unscaledDeltaTime,
                Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        }
        private void LateUpdate() { if (inside) { RecoverFallenWalker();ApplyView(); } }
        private void ApplyView()
        {
            if(scenario!=null&&scenario.Operations!=null&&scenario.Operations.Live){helmetLight.intensity=scenario.Operations.Powered(0)?.3f:2.5f;helmetLight.range=12;}
            transform.SetPositionAndRotation(FeetPosition + Vector3.up * 1.6f, Quaternion.Euler(pitch, yaw, 0));
            view.fieldOfView = 70; view.nearClipPlane = .08f; view.farClipPlane = 1200000;
            QualitySettings.shadowDistance = 32;
        }
        private void UpdateCursor()
        {
            // The isolated ring demo owns the cursor while its overlay is open.
            if (RingConstructionSimulator.IsOpen) return;
            bool locked = inside && captureMouse && !consoleOpen && (scenario.Scanner.Dialogue == null || IntroSequence.IsPlaying) && !SettingsMenu.BlocksInput && Application.isFocused;
            var state = locked ? CursorLockMode.Locked : CursorLockMode.None;
            if (Cursor.lockState != state) { Cursor.lockState = state; cursorChangedFrame = Time.frameCount; }
            if (Cursor.visible == locked) Cursor.visible = !locked;
        }
        public static bool OwnsScreenPoint(Vector3 mouse) => !StartMenu.IsOpen && SwitchRect.Contains(UiLayout.Point(mouse));
        private void OnGUI()
        {
            if (StartMenu.IsOpen || IntroSequence.BlocksGameplay || SettingsMenu.BlocksInput || MemoryCinematic.IsPlaying || scenario.Scanner.Dialogue != null) return;
            using var layout = new UiLayout.Scope(UiLayout.Scale());
            ui.Configure(SettingsStore.Current.Accessibility);
            if (GUI.Button(SwitchRect, inside ? "Station verlassen  [V]" : "Station betreten  [V]", ui.Button)) { if (inside) Exit(); else Enter(); }
            if (!inside) return;
            const string hints = "STARTSTATION  ·  Ring / Wohnen / Zentrale / Drohnenhalle\nWASD + Maus  ·  Shift Sprinten  ·  Space Hüpfen  ·  E Pult / Drohnentor\nTab Maus freigeben  ·  V Außenansicht  ·  F7 Raumplanung";
            float hintLeft = orbit.ShowHud ? VitalsHudRect.xMax + 18 : 18;
            float hintWidth = Mathf.Min(660, UiLayout.Width - hintLeft - 18);
            float hintHeight = ui.Small.CalcHeight(new GUIContent(hints), hintWidth - 24) + 16;
            float hintTop = UiLayout.Height - 66 - hintHeight;
            if (SettingsStore.Current.Gameplay.ShowControlHints)
            {
                ui.Panel(new Rect(hintLeft, hintTop, hintWidth, hintHeight));
                GUI.Label(new Rect(hintLeft + 12, hintTop + 8, hintWidth - 24, hintHeight - 16), hints, ui.Small);
            }
            if (orbit.ShowHud) DrawVitals();
            string operationsPrompt=scenario.Operations!=null&&scenario.Operations.Live?scenario.Operations.InteractionPrompt(FeetPosition):"";
            string prompt = !string.IsNullOrEmpty(operationsPrompt)?operationsPrompt:Near(Layout.Console, 2.4f) ? "E  Stationspult bedienen"
                : Airlock.CanRequest(FeetPosition) ? "E  Schleusendurchgang starten"
                : Layout.Geometry.NearestGate(FeetPosition)!=null ? Layout.Geometry.NearestGate(FeetPosition).Prompt
                : Layout.Geometry.DetailedHall!=null && Layout.Geometry.DetailedHall.Nearest(FeetPosition)>=0 ? Layout.Geometry.DetailedHall.Prompt(FeetPosition)
                : Near(Layout.RepairDrone, 1.7f) ? "R-01  ·  Reparaturdrohne ausgeschaltet" : "";
            Vector3 local = Room.InverseTransformPoint(FeetPosition);
            if (Airlock.Nearby(FeetPosition) && Airlock.Phase != AirlockPhase.Idle) prompt = Airlock.Status;
            if (!string.IsNullOrEmpty(prompt)) GUI.Label(new Rect(UiLayout.Width * .5f - 260, hintTop - 52, 520, 48), prompt, ui.Text);
            if (Cursor.lockState == CursorLockMode.Locked) GUI.Label(new Rect(UiLayout.Width / 2f - 5, UiLayout.Height / 2f - 10, 20, 20), "+", ui.Small);
            if (consoleOpen) DrawConsole();
        }
        private void DrawVitals()
        {
            Rect panel = VitalsHudRect;
            ui.Panel(panel);
            string title = Vitals.Exhausted ? "STAMINA · ERSCHÖPFT" : Vitals.IsSprinting ? "STAMINA · SPRINT" : "KÖRPERSTATUS";
            GUI.Label(new Rect(panel.x + 12, panel.y + 10, panel.width - 24, 24), title, ui.Small);
            VitalBar(panel, 0, "Stamina", Vitals.Stamina, false, SpaceMinerUi.Cyan);
            VitalBar(panel, 1, "Müdigkeit", Vitals.Fatigue, true, SpaceMinerUi.Amber);
            VitalBar(panel, 2, "Hunger", Vitals.Hunger, true, SpaceMinerUi.Amber);
            VitalBar(panel, 3, "Durst", Vitals.Thirst, true, SpaceMinerUi.Cyan);
            VitalBar(panel, 4, "Gesundheit", Vitals.Health, false, SpaceMinerUi.Success);
        }
        private void VitalBar(Rect panel, int row, string label, float value, bool need, Color color)
        {
            float top = panel.y + 36 + row * VitalRowHeight;
            float textHeight = VitalRowHeight - 12;
            GUI.Label(new Rect(panel.x + 12, top, 186, textHeight), label, ui.Small);
            var right = new GUIStyle(ui.Small) { alignment = TextAnchor.UpperRight };
            GUI.Label(new Rect(panel.xMax - 76, top, 64, textHeight), value.ToString("F0") + " %", right);
            Rect track = new Rect(panel.x + 12, top + textHeight + 1, panel.width - 24, 6);
            SpaceMinerUi.Fill(track, new Color(.08f, .17f, .22f, .95f));
            bool warning = need ? value >= 75 : value <= 25;
            SpaceMinerUi.Fill(new Rect(track.x, track.y, track.width * Mathf.Clamp01(value / 100), track.height), warning ? SpaceMinerUi.Error : color);
        }
        private void DrawConsole()
        {
            int depth = GUI.depth; GUI.depth = -30;
            float width = Mathf.Min(820, UiLayout.Width - 32), height = Mathf.Min(650, UiLayout.Height - 32);
            Rect panel = new Rect((UiLayout.Width - width) * .5f, (UiLayout.Height - height) * .5f, width, height);
            SpaceMinerUi.Fill(new Rect(0, 0, UiLayout.Width, UiLayout.Height), new Color(0, .01f, .02f, .65f)); ui.Panel(panel);
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
            if(scenario.Operations!=null&&scenario.Operations.Live){
                GUILayout.Label("Nach dem Scan im Drohnenleitstand Drohne 01 und eine bekannte Eisquelle auswählen. Roh-Eis geht in die geschlossene Annahme; der Aufbereiter gewinnt daraus Verbrauchswasser. Ziel: erste Lieferung und 40 L saubere Reserve.",ui.Small);
                if(GUILayout.Button("Drohnenleitstand öffnen",ui.Primary)){CloseConsole();scenario.Operations.OpenDroneConsole();}
            }else{
            GUILayout.Label(scenario.QuestComplete ? "Auftrag erfüllt: Tank voll." : "Eine bestätigte Eisquelle zuweisen und den Stationstank mit der Bergbaudrohne befüllen.", ui.Small);
            foreach (var source in scenario.Asteroids)
                if (source.WaterIdentified && GUILayout.Button(source.Info.DisplayName, consoleSource == source ? ui.Primary : ui.Button)) consoleSource = source;
            if (consoleSource != null) GUILayout.Label("Quelle: " + consoleSource.Info.DisplayName + "  ·  Wasseranteil " + (consoleSource.WaterFraction * 100).ToString("F0") + "%", ui.Small);
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && consoleSource != null && consoleSource.CanMineWater && scenario.Worker.IsReady && !scenario.QuestComplete;
            if (GUILayout.Button("Wasserauftrag übernehmen", ui.Primary)) AcceptWaterOrder(consoleSource);
            GUI.enabled = previousEnabled && scenario.Worker.HasTankOrder;
            if (GUILayout.Button("Auftrag abbrechen und Drohne zurückrufen", ui.Button)) scenario.Worker.ReturnToShip();
            GUI.enabled = previousEnabled;
            GUILayout.Label(scenario.Message + "\nLieferungen: " + scenario.Deliveries + "  ·  Batterie: " + scenario.Worker.BatteryKwh.ToString("F1") + " kWh", ui.Small);
            }
            GUILayout.Space(12); GUILayout.Label("STEUERUNG", ui.Text);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(scenario.SimulationRate == 0 ? "Simulation fortsetzen" : "Simulation pausieren", ui.Button)) scenario.TogglePause();
            foreach (float rate in new[] { 1f, 5f }) if (GUILayout.Button(rate.ToString("F0") + "×", ui.Button)) scenario.SetSimulationRate(rate);
            GUILayout.EndHorizontal(); GUILayout.BeginHorizontal();
            if (GUILayout.Button("Außenansicht", ui.Button)) Exit();
            if (GUILayout.Button("Drohne verfolgen", ui.Button)) { Exit(); orbit.FollowDrone(scenario.Worker); }
            GUILayout.EndHorizontal();
            GUILayout.Label("R-01: "+(scenario.Operations!=null&&scenario.Operations.State.helper?"interne Servicekraft einsatzfähig.":"kleine Reparaturdrohne, ausgeschaltet."), ui.Small);
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
