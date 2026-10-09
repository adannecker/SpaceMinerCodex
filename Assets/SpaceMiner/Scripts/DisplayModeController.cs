using System.Collections;
using UnityEngine;

namespace SpaceMiner
{
    // Display shortcuts remain available during the intro and when the HUD is hidden.
    public sealed class DisplayModeController : MonoBehaviour
    {
        public bool IsFullscreen => Screen.fullScreen;
        public bool IsChanging { get; private set; }
        public bool CanSwitch => !Application.isEditor && !IsChanging;
        private int windowWidth = 1440;
        private int windowHeight = 900;
        public static Vector2Int MonitorResolution
        {
            get
            {
                var display = Screen.mainWindowDisplayInfo;
                return display.width > 0 && display.height > 0
                    ? new Vector2Int(display.width, display.height)
                    : new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
            }
        }
        private void Awake()
        {
            if (!Screen.fullScreen) { windowWidth = Screen.width; windowHeight = Screen.height; }
        }

        public static int ModeIndex(FullScreenMode mode) => mode == FullScreenMode.Windowed ? 0 : mode == FullScreenMode.ExclusiveFullScreen ? 2 : 1;
        public static FullScreenMode Mode(int index) => index == 0 ? FullScreenMode.Windowed : index == 2 ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.FullScreenWindow;
        public static System.Collections.Generic.List<Vector2Int> ResolutionChoices()
        {
            var sizes = new System.Collections.Generic.List<Vector2Int> { Vector2Int.zero };
            foreach (var size in new[] { new Vector2Int(Screen.width, Screen.height), new Vector2Int(1280,720), new Vector2Int(1440,900), new Vector2Int(1920,1080) }) if (!sizes.Contains(size)) sizes.Add(size);
            foreach (var resolution in Screen.resolutions) { var size = new Vector2Int(resolution.width, resolution.height); if (!sizes.Contains(size)) sizes.Add(size); }
            return sizes;
        }
        public void ApplySettings(GraphicsSettings settings)
        {
            if (!CanSwitch) return;
            var mode = settings.DisplayMode < 0 ? Screen.fullScreenMode : Mode(settings.DisplayMode);
            var automatic = mode == FullScreenMode.Windowed
                ? (Screen.fullScreen ? new Vector2Int(windowWidth, windowHeight) : new Vector2Int(Screen.width, Screen.height))
                : MonitorResolution;
            int width = settings.Width > 0 ? settings.Width : automatic.x;
            int height = settings.Height > 0 ? settings.Height : automatic.y;
            ChangeResolution(width, height, mode);
        }
        private void Update()
        {
            if (SettingsMenu.BlocksInput) return;
            bool altEnter = Input.GetKeyDown(KeyCode.Return)
                && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));
            if (Input.GetKeyDown(KeyCode.F11) || altEnter) Toggle();
        }

        public void Toggle() => SetFullscreen(!IsFullscreen);

        public void SetFullscreen(bool fullscreen)
        {
            if (!CanSwitch || fullscreen == IsFullscreen) return;
            var size = fullscreen ? MonitorResolution : new Vector2Int(windowWidth, windowHeight);
            ChangeResolution(size.x, size.y, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }

        private void ChangeResolution(int width, int height, FullScreenMode mode)
        {
            if (mode == Screen.fullScreenMode && width == Screen.width && height == Screen.height) return;
            if (!Screen.fullScreen) { windowWidth = Screen.width; windowHeight = Screen.height; }
            if (mode == FullScreenMode.Windowed) { windowWidth = width; windowHeight = height; }
            IsChanging = true;
            StartCoroutine(ChangeMode(width, height, mode));
        }
        private IEnumerator ChangeMode(int width, int height, FullScreenMode target)
        {
            Screen.SetResolution(width, height, target);
            // Unity applies this at the end of a frame; suppress double requests while it settles.
            yield return new WaitForEndOfFrame();
            yield return null;
            float deadline = Time.realtimeSinceStartup + 3f;
            while ((Screen.fullScreenMode != target || Screen.width != width || Screen.height != height)
                && Time.realtimeSinceStartup < deadline) yield return null;
            IsChanging = false;
        }
    }
}
