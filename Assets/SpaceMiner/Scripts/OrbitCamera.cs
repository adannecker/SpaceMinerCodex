using UnityEngine;

namespace SpaceMiner
{
    [RequireComponent(typeof(Camera))]
    public sealed class OrbitCamera : MonoBehaviour
    {
        public const float MinimumDistance = 3f;
        public const float MaximumDistance = 200000000f;
        public Vector3 Pivot { get; private set; }
        public float Distance { get; private set; }
        public SpaceObject Selected { get; private set; }
        public bool IsCelestialView { get; private set; }
        public bool ShowHud = true;

        private float yaw;
        private float pitch;
        private Camera view;
        private float normalFieldOfView;
        private Vector3 lastMouse;
        private GUIStyle titleStyle;
        private GUIStyle textStyle;
        private GUIStyle mutedStyle;
        private Texture2D panel;
        private Transform followTarget;

        private void Awake()
        {
            view = GetComponent<Camera>();
            normalFieldOfView = view.fieldOfView;
            ResetView();
        }

        private void Update()
        {
            if (IntroSequence.BlocksGameplay || SettingsMenu.BlocksInput)
            {
                lastMouse = Input.mousePosition;
                return;
            }
            if (PlayerInput.Pressed(CameraAction.Reset) || Input.GetKeyDown(KeyCode.Home)) ResetView();
            if (PlayerInput.Pressed(CameraAction.Overview)) Overview();
            if (PlayerInput.Pressed(CameraAction.Focus) && Selected != null) Focus(Selected);
            if (PlayerInput.Pressed(CameraAction.ToggleHud)) ShowHud = !ShowHud;
            Vector3 mouse = Input.mousePosition;
            Vector3 delta = mouse - lastMouse;
            // Ignore the initial delta after clicking or re-entering the game window.
            if (Input.GetMouseButton(1) && !Input.GetMouseButtonDown(1))
                Orbit(new Vector2(delta.x, delta.y * (SettingsStore.Current.Controls.InvertY ? -1 : 1)) * (0.18f * SettingsStore.Current.Controls.Sensitivity));
            if (Input.GetMouseButton(2) && !Input.GetMouseButtonDown(2))
                Pan(new Vector2(delta.x, delta.y));
            lastMouse = mouse;

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.001f && !IsOverHud(mouse))
                Zoom(scroll * SettingsStore.Current.Controls.ZoomSpeed, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

            float horizontal = (PlayerInput.Held(CameraAction.Right) ? 1f : 0f) - (PlayerInput.Held(CameraAction.Left) ? 1f : 0f);
            float forward = (PlayerInput.Held(CameraAction.Forward) ? 1f : 0f) - (PlayerInput.Held(CameraAction.Backward) ? 1f : 0f);
            float vertical = (PlayerInput.Held(CameraAction.Up) ? 1f : 0f) - (PlayerInput.Held(CameraAction.Down) ? 1f : 0f);
            Move(new Vector3(horizontal, vertical, forward), Time.unscaledDeltaTime,
                Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

            if (Input.GetMouseButtonDown(0) && !IsOverHud(mouse))
            {
                if (Physics.Raycast(view.ScreenPointToRay(mouse), out RaycastHit hit, view.farClipPlane))
                {FindFirstObjectByType<RuinedWorld>()?.SelectPlanet(-1);Select(hit.collider.GetComponentInParent<SpaceObject>());}
                else
                {
                    var world=FindFirstObjectByType<RuinedWorld>();
                    if(world!=null&&world.TrySelectPlanet(mouse)){Select(null);return;}
                    var spiral = GetComponent<SpiralBelt>();
                    Select(spiral != null ? spiral.PickOverview(mouse, view) : null);
                }
            }
            ApplyPose();
        }

        public void ResetView()
        {
            IsCelestialView=false;
            if(view!=null)view.fieldOfView=normalFieldOfView;
            followTarget = null;
            Pivot = Vector3.zero;
            Distance = 140f;
            yaw = -24f;
            pitch = 18f;
            Selected = null;
            ApplyPose();
        }

        public void Overview()
        {
            IsCelestialView=false;
            view.fieldOfView=normalFieldOfView;
            followTarget = null;
            var root = GameObject.Find("Asteroids (1 unit = 1 metre)");
            Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 24f);
            if (root != null)
                foreach (Transform body in root.transform)
                {
                    var info = body.GetComponent<SpaceObject>();
                    if (info != null) bounds.Encapsulate(new Bounds(body.position, Vector3.one * info.DiameterMeters));
                }
            var field = GetComponent<SpiralBelt>();
            if (field != null && field.IsReady) bounds = field.FieldBounds;
            Pivot = bounds.center;
            if (view == null) view = GetComponent<Camera>();
            float halfVertical = view.fieldOfView * Mathf.Deg2Rad * 0.5f;
            float halfHorizontal = Mathf.Atan(Mathf.Tan(halfVertical) * view.aspect);
            // Bounding sphere also covers the field when the window is tall and narrow.
            Distance = Mathf.Clamp(bounds.extents.magnitude / Mathf.Sin(Mathf.Min(halfVertical, halfHorizontal)) * 1.12f,
                MinimumDistance, MaximumDistance);
            yaw = -18f;
            var spiral = FindFirstObjectByType<SpiralBelt>();
            pitch = spiral != null && spiral.IsReady ? 78f : 28f;
            ApplyPose();
        }

        public void Focus(SpaceObject target)
        {
            if (target == null) return;
            IsCelestialView=false;
            view.fieldOfView=normalFieldOfView;
            Selected = target;
            followTarget = target.transform;
            Pivot = target.transform.position;
            Distance = Mathf.Clamp(target.DiameterMeters * 2.2f, 6f, MaximumDistance);
            ApplyPose();
        }

        public void Zoom(float wheelSteps, bool fast = false)
        {
            // Clamp before exponentiation so large wheel deltas cannot overflow.
            float logDistance = Mathf.Log(Distance) - wheelSteps * (fast ? 0.64f : 0.16f);
            if (logDistance <= Mathf.Log(MinimumDistance)) Distance = MinimumDistance;
            else if (logDistance >= Mathf.Log(MaximumDistance)) Distance = MaximumDistance;
            else Distance = Mathf.Exp(logDistance);
            ApplyPose();
        }

        public void LookAtCelestial(Vector3 direction, float fieldOfView)
        {
            IsCelestialView=true;
            followTarget=null;Selected=null;
            Vector3 target=transform.position+direction*1000;
            view.fieldOfView=fieldOfView;
            for(int i=0;i<3;i++)
            {
                Vector3 angles=Quaternion.LookRotation(target-transform.position).eulerAngles;
                yaw=angles.y;pitch=angles.x>180?angles.x-360:angles.x;ApplyPose();
            }
        }
        public void SolarOverview(Vector3 centerKilometres,float radiusKilometres)
        {
            IsCelestialView=false;followTarget=null;Selected=null;
            view.fieldOfView=normalFieldOfView;Pivot=centerKilometres*1000;
            float halfAngle=Mathf.Min(normalFieldOfView*Mathf.Deg2Rad*.5f,Mathf.Atan(Mathf.Tan(normalFieldOfView*Mathf.Deg2Rad*.5f)*view.aspect));
            Distance=Mathf.Clamp(radiusKilometres*1000/Mathf.Sin(halfAngle)*1.12f,MinimumDistance,MaximumDistance);
            yaw=0;pitch=72;ApplyPose();
        }

        public void Orbit(Vector2 deltaDegrees)
        {
            yaw = Mathf.Repeat(yaw + deltaDegrees.x, 360f);
            pitch = Mathf.Clamp(pitch - deltaDegrees.y, -85f, 85f);
            ApplyPose();
        }

        public void Pan(Vector2 pixels)
        {
            followTarget = null;
            float metersPerPixel = 2f * Distance * Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * 0.5f)
                / Mathf.Max(1, view.pixelHeight);
            Pivot -= (transform.right * pixels.x + transform.up * pixels.y) * metersPerPixel;
            ApplyPose();
        }

        public void Move(Vector3 localDirection, float seconds, bool fast)
        {
            if (localDirection.sqrMagnitude < 0.001f) return;
            followTarget = null;
            float speed = Mathf.Max(2f, Distance * 0.35f) * SettingsStore.Current.Controls.CameraSpeed * (fast ? 3f : 1f);
            Pivot += transform.TransformDirection(Vector3.ClampMagnitude(localDirection, 1f)) * (speed * seconds);
            ApplyPose();
        }

        private void ApplyPose()
        {
            if (view == null) view = GetComponent<Camera>();
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.SetPositionAndRotation(Pivot - rotation * Vector3.forward * Distance, rotation);
            ConfigureDepth(view, Distance);
            QualitySettings.shadowDistance = Mathf.Clamp(Distance * 2.5f, 20f, 30000f);
        }

        internal static void ConfigureDepth(Camera camera, float focusDistance)
        {
            // A centimetre-scale near plane at a distant station wastes depth precision:
            // thin cladding and solar cells then compete for the same depth values.
            // Still retain the close clipping needed when inspecting a two-metre drone.
            camera.nearClipPlane = Mathf.Clamp(focusDistance * 0.01f, 0.05f, 200f);
            camera.farClipPlane = Mathf.Max(1200000f, focusDistance * 2f + 40000f);
        }

        internal void RestoreViewPose() => ApplyPose();

        private static float HudScale => Mathf.Clamp(Screen.height / 900f, 0.65f, 1.5f);

        private bool IsOverHud(Vector3 mouse)
        {
            if (StationInteriorMode.OwnsScreenPoint(mouse)) return true;
            var world=FindFirstObjectByType<RuinedWorld>();if(world!=null&&world.OwnsScreenPoint(mouse))return true;
            if (SettingsMenu.OwnsScreenPoint(mouse)) return true;
            if (!ShowHud) return false;
            var scenarioHud = GetComponent<WaterScenarioHud>();
            if (scenarioHud != null) return scenarioHud.OwnsScreenPoint(mouse);
            Vector2 point = new Vector2(mouse.x, Screen.height - mouse.y) / HudScale;
            return new Rect(22, 22, 340, 134).Contains(point)
                || new Rect(22, Screen.height / HudScale - 172, 470, 150).Contains(point)
                || new Rect(Screen.width / HudScale - 310, 22, 288, 146).Contains(point);
        }

        private void OnGUI()
        {
            if (IntroSequence.BlocksGameplay || SettingsMenu.BlocksInput) return;
            if (!ShowHud || GetComponent<WaterScenarioHud>() != null) return;
            EnsureStyles();
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * HudScale);
            float width = Screen.width / HudScale;
            float height = Screen.height / HudScale;
            GUI.DrawTexture(new Rect(22, 22, 340, 134), panel);
            GUI.Label(new Rect(40, 34, 310, 30), "SPACE MINER", titleStyle);
            GUI.Label(new Rect(40, 69, 310, 24), "PROTOTYP 01  /  ASTEROIDENFELD", mutedStyle);
            GUI.Label(new Rect(40, 101, 310, 22), "Kameraabstand   " + FormatDistance(Distance), textStyle);
            GUI.Label(new Rect(40, 127, 310, 22), "Massstab   1 Einheit = 1 Meter", mutedStyle);

            GUI.DrawTexture(new Rect(22, height - 172, 470, 150), panel);
            GUI.Label(new Rect(40, height - 160, 430, 22), "STEUERUNG", mutedStyle);
            GUI.Label(new Rect(40, height - 134, 430, 24), "Mausrad  Zoom   |   Rechte Maustaste  Drehen", textStyle);
            GUI.Label(new Rect(40, height - 109, 430, 24), "Mittlere Maustaste  Verschieben   |   Shift + Rad  Schnellzoom", textStyle);
            GUI.Label(new Rect(40, height - 84, 430, 24), "W A S D  Bewegen   |   Q / E  Ab / Auf   |   Shift  Schnell", textStyle);
            GUI.Label(new Rect(40, height - 59, 430, 24), "R  Startansicht   |   B  Feldansicht   |   Klick + F  Fokus", textStyle);

            GUI.DrawTexture(new Rect(width - 310, 22, 288, 146), panel);
            GUI.Label(new Rect(width - 292, 35, 252, 22), "OBJEKTINFORMATION", mutedStyle);
            if (Selected != null)
            {
                GUI.Label(new Rect(width - 292, 65, 252, 24), Selected.DisplayName, textStyle);
                GUI.Label(new Rect(width - 292, 91, 252, 24), "Groesse   " + FormatDistance(Selected.DiameterMeters), textStyle);
                GUI.Label(new Rect(width - 292, 118, 252, 40), Selected.Description, mutedStyle);
            }
            else
            {
                GUI.Label(new Rect(width - 292, 68, 252, 55), "Objekt anklicken, dann F druecken.\nR bringt dich zum Schiff zurueck.", textStyle);
                GUI.Label(new Rect(width - 292, 133, 252, 22), "H  Anzeige ein / aus", mutedStyle);
            }
            DrawMarker(GameObject.Find("Stranded Ship"), "SCHIFF", new Color(0.4f, 0.85f, 0.95f));
            if (Selected != null) DrawMarker(Selected.gameObject, Selected.DisplayName, new Color(1f, 0.75f, 0.35f));
            GUI.matrix = previous;
        }

        private void DrawMarker(GameObject target, string label, Color color)
        {
            if (target == null) return;
            Vector3 screen = view.WorldToScreenPoint(target.transform.position);
            if (screen.z < view.nearClipPlane || screen.x < 0 || screen.x > Screen.width || screen.y < 0 || screen.y > Screen.height) return;
            float x = screen.x / HudScale;
            float y = (Screen.height - screen.y) / HudScale;
            Color previous = GUI.color;
            GUI.color = color;
            GUI.Label(new Rect(x - 5, y - 7, 20, 20), "+", textStyle);
            GUI.Label(new Rect(x + 12, y - 22, 230, 22), label, mutedStyle);
            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold };
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            mutedStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
            titleStyle.normal.textColor = new Color(0.83f, 0.94f, 1f);
            textStyle.normal.textColor = new Color(0.82f, 0.87f, 0.92f);
            mutedStyle.normal.textColor = new Color(0.52f, 0.65f, 0.74f);
            panel = new Texture2D(1, 1);
            panel.SetPixel(0, 0, new Color(0.018f, 0.035f, 0.055f, 0.92f));
            panel.Apply();
        }

        public static string FormatDistance(float meters)
        {
            return meters >= 1000f ? (meters / 1000f).ToString("0.00") + " km" : meters.ToString("0.0") + " m";
        }

        public void Select(SpaceObject target)
        {
            if (Selected != target) followTarget = null;
            Selected = target;
        }

        private void LateUpdate()
        {
            if (IntroSequence.BlocksGameplay || SettingsMenu.BlocksInput) return;
            if (followTarget != null) Pivot = followTarget.position;
            ApplyPose();
        }

        private void OnDestroy()
        {
            if (panel != null) Destroy(panel);
        }
    }
}
