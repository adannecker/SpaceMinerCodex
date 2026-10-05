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

        private void Update()
        {
            bool altEnter = Input.GetKeyDown(KeyCode.Return)
                && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));
            if (Input.GetKeyDown(KeyCode.F11) || altEnter) Toggle();
        }

        public void Toggle() => SetFullscreen(!IsFullscreen);

        public void SetFullscreen(bool fullscreen)
        {
            if (!CanSwitch || fullscreen == IsFullscreen) return;
            if (fullscreen)
            {
                windowWidth = Screen.width;
                windowHeight = Screen.height;
            }
            StartCoroutine(ChangeMode(fullscreen));
        }

        private IEnumerator ChangeMode(bool fullscreen)
        {
            IsChanging = true;
            FullScreenMode target = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            Resolution desktop = Screen.currentResolution;
            Screen.SetResolution(fullscreen ? desktop.width : windowWidth,
                fullscreen ? desktop.height : windowHeight, target);
            // Unity applies this at the end of a frame; suppress double requests while it settles.
            yield return new WaitForEndOfFrame();
            yield return null;
            float deadline = Time.realtimeSinceStartup + 3f;
            while (Screen.fullScreenMode != target && Time.realtimeSinceStartup < deadline) yield return null;
            IsChanging = false;
        }
    }
}
