using UnityEngine;
namespace SpaceMiner
{
    // Editing transaction, shared by menus and tests. Cancel never touches runtime settings.
    public sealed class SettingsSession
    {
        public SpaceMinerPlayerSettings Draft { get; private set; }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public DeveloperGameConfiguration Developer { get; private set; }
        private DeveloperGameConfiguration developerDefaults;
#endif
        public void Begin()
        {
            Draft = SettingsStore.Current.Copy();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var scenario = Object.FindFirstObjectByType<WaterScenario>();
            Developer = scenario != null && scenario.Worker != null ? DeveloperGameConfiguration.Capture(scenario) : null;
            if (Developer != null && developerDefaults == null) developerDefaults = Developer.Copy();
#endif
        }
        public void RestoreDefaults()
        {
            Draft = new SpaceMinerPlayerSettings();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Developer = developerDefaults?.Copy();
#endif
        }
        public void PreviewAudio() { if (Draft != null) PlayerAudio.Preview(Draft.Audio); }
        public void Cancel() { Draft = null; PlayerAudio.EndPreview(); }
        public bool Apply(out string error)
        {
            if (Draft == null) { error = "Kein aktiver Einstellungsentwurf."; return false; }
            if (!SettingsStore.Save(Draft, out error)) return false;
            PlayerAudio.EndPreview();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var scenario = Object.FindFirstObjectByType<WaterScenario>();
            if (Developer != null && scenario != null && scenario.Worker != null) Developer.Apply(scenario);
#endif
            Draft = SettingsStore.Current.Copy(); return true;
        }
    }
    public static class SettingsRuntime
    {
        public static void Apply(SpaceMinerPlayerSettings settings, int defaultQuality)
        {
            QualitySettings.SetQualityLevel(settings.Graphics.Quality < 0 ? defaultQuality : settings.Graphics.Quality);
            QualitySettings.vSyncCount = settings.Graphics.VSync ? 1 : 0;
            Application.targetFrameRate = settings.Graphics.FrameLimit;
            PlayerAudio.Apply(settings.Audio);
            var camera = Object.FindFirstObjectByType<OrbitCamera>();
            if (camera != null) camera.ShowHud = settings.Interface.ShowHud;
            var display = Object.FindFirstObjectByType<DisplayModeController>();
            if (display != null) display.ApplySettings(settings.Graphics);
        }
    }
}
