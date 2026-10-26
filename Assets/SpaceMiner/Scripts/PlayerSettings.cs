using System;
using System.IO;
using UnityEngine;
namespace SpaceMiner
{
    [Serializable] public sealed class GameplaySettings { public bool PauseInMenu = true; public bool ShowControlHints = true; }
    [Serializable] public sealed class GraphicsSettings { public bool VSync = true; public int FrameLimit = 120; public int Quality = -1; public int Width; public int Height; public int DisplayMode = -1; }
    [Serializable] public sealed class AudioSettings
    {
        public float Master = 1, Background = 1, Effects = 1, Voices = 1, Ui = .65f;
        public bool MasterEnabled = true, BackgroundEnabled = true, EffectsEnabled = true, VoicesEnabled = true, UiEnabled = true;
    }
    [Serializable] public sealed class ControlSettings { public CameraBindings Bindings = new CameraBindings(); public float Sensitivity = 1; public float ZoomSpeed = 1; public float CameraSpeed = 1; public bool InvertY; }
    [Serializable] public sealed class InterfaceSettings { public float Scale = 1; public bool ShowHud = true; }
    [Serializable] public sealed class AccessibilitySettings { public bool Subtitles = true; public bool HighContrast; public float TextScale = 1; }
    [Serializable] public sealed class SpaceMinerPlayerSettings
    {
        public int Version = 1;
        public GameplaySettings Gameplay = new GameplaySettings();
        public GraphicsSettings Graphics = new GraphicsSettings();
        public AudioSettings Audio = new AudioSettings();
        public ControlSettings Controls = new ControlSettings();
        public InterfaceSettings Interface = new InterfaceSettings();
        public AccessibilitySettings Accessibility = new AccessibilitySettings();
        public SpaceMinerPlayerSettings Copy() => JsonUtility.FromJson<SpaceMinerPlayerSettings>(JsonUtility.ToJson(this));
        public void Validate()
        {
            Gameplay ??= new GameplaySettings(); Graphics ??= new GraphicsSettings(); Audio ??= new AudioSettings();
            Controls ??= new ControlSettings(); Interface ??= new InterfaceSettings(); Accessibility ??= new AccessibilitySettings();
            Controls.Bindings ??= new CameraBindings(); Controls.Bindings.Validate();
            Graphics.DisplayMode = Mathf.Clamp(Graphics.DisplayMode, -1, 2);
            if (Graphics.Width <= 0 || Graphics.Height <= 0) { Graphics.Width = 0; Graphics.Height = 0; }
            else { Graphics.Width = Mathf.Clamp(Graphics.Width, 640, 7680); Graphics.Height = Mathf.Clamp(Graphics.Height, 480, 4320); }
            Controls.CameraSpeed = Clamp(Controls.CameraSpeed, .2f, 3, 1);
            Graphics.FrameLimit = Mathf.Clamp(Graphics.FrameLimit, 30, 240);
            Graphics.Quality = Mathf.Clamp(Graphics.Quality, -1, QualitySettings.names.Length - 1);
            Audio.Master = Clamp(Audio.Master, 0, 1, 1);
            Audio.Background = Clamp(Audio.Background, 0, 1, 1); Audio.Effects = Clamp(Audio.Effects, 0, 1, 1);
            Audio.Voices = Clamp(Audio.Voices, 0, 1, 1); Audio.Ui = Clamp(Audio.Ui, 0, 1, .65f);
            Controls.Sensitivity = Clamp(Controls.Sensitivity, .2f, 3, 1); Controls.ZoomSpeed = Clamp(Controls.ZoomSpeed, .2f, 3, 1);
            Interface.Scale = Clamp(Interface.Scale, .75f, 1.5f, 1); Accessibility.TextScale = Clamp(Accessibility.TextScale, 1, 1.4f, 1);
        }
        private static float Clamp(float value, float min, float max, float fallback) => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);
    }
    public static class PlayerSettingsFile
    {
        public static SpaceMinerPlayerSettings Read(string path)
        {
            var data = File.Exists(path) ? JsonUtility.FromJson<SpaceMinerPlayerSettings>(File.ReadAllText(path)) : new SpaceMinerPlayerSettings();
            data ??= new SpaceMinerPlayerSettings(); data.Validate(); return data;
        }
        public static void Write(string path, SpaceMinerPlayerSettings data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path + ".tmp", JsonUtility.ToJson(data, true));
            if (File.Exists(path)) File.Replace(path + ".tmp", path, path + ".bak");
            else File.Move(path + ".tmp", path);
        }
    }
    public static class SettingsStore
    {
        private static PlayerSettingsService service;
        private static PlayerSettingsService Service => service ??= new PlayerSettingsService(new LocalPlayerSettingsStorage(PathName), ApplyRuntime);
        public static SpaceMinerPlayerSettings Current => Service.Current;
        public static event Action<SpaceMinerPlayerSettings> Applied;
        public static int DefaultQuality { get; } = QualitySettings.GetQualityLevel();
        public static string PathName
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                var args = Environment.GetCommandLineArgs();
                if(Array.IndexOf(args,"-adaptiveLightingCheck")>=0)return Path.GetFullPath("Logs/AdaptiveLighting/settings/player-settings.json");
                if(Array.IndexOf(args,"-touchConsoleCheck")>=0)return Path.GetFullPath("Logs/TouchConsole/settings/player-settings.json");
                if (Array.IndexOf(args, "-miraQuestPreview") >= 0)
                    return Path.GetFullPath("Logs/MiraQuestPreview/settings/player-settings.json");
                if (Array.IndexOf(args, "-miraDialogueIntegrationCheck") >= 0 || Array.IndexOf(args, "-miraDialogueCheck") >= 0 || Array.IndexOf(args, "-miraModelsCheck") >= 0
                    || Array.IndexOf(args, "-miraQuestsCheck") >= 0 || Array.IndexOf(args, "-miraQuestGameCheck") >= 0)
                    return Path.GetFullPath("Logs/MiraDialogue/settings/player-settings.json");
                if(Array.IndexOf(args,"-commandRoomPlannerCheck")>=0)return Path.GetFullPath("Logs/CommandRoomPlannerCheck/settings/player-settings.json");
                if(Array.IndexOf(args,"-stationRoomPlanningCheck")>=0)return Path.GetFullPath("Logs/StationRoomPlanning/settings/player-settings.json");
                if(Array.IndexOf(args,"-stationDebrisCheck")>=0)return Path.GetFullPath("Logs/StationDebris/settings/player-settings.json");
                if(Array.IndexOf(args,"-stationHandsCheck")>=0)return Path.GetFullPath("Logs/StationHands/settings/player-settings.json");
                if(Array.IndexOf(args,"-stationConsoleTouchCheck")>=0)return Path.GetFullPath("Logs/StationConsoleTouch/settings/player-settings.json");
                if(Array.IndexOf(args,"-stationDroneSizingCheck")>=0)return Path.GetFullPath("Logs/DroneSizing/settings/player-settings.json");
                if(Array.IndexOf(args,"-stationIcePlantCheck")>=0)return Path.GetFullPath("Logs/StationIcePlant/settings/player-settings.json");
                if(Array.IndexOf(args,"-stationDroneConsoleCheck")>=0)return Path.GetFullPath("Logs/StationDroneConsole/settings/player-settings.json");
                if(Array.IndexOf(args,"-stationTacticalCheck")>=0)return Path.GetFullPath("Logs/StationTactical/settings/player-settings.json");
                if(Array.IndexOf(args,"-stationCameraVerticalCheck")>=0)return Path.GetFullPath("Logs/CameraVertical/settings/player-settings.json");
                if(Array.IndexOf(args,"-stationPowerConsoleCheck")>=0)return Path.GetFullPath("Logs/StationPowerConsole/settings/player-settings.json");
                if(Array.IndexOf(args,"-stationIndoorTrafficCheck")>=0)return Path.GetFullPath("Logs/StationIndoorTraffic/settings/player-settings.json");
                if(Array.IndexOf(args,"-stationBatteryBayCheck")>=0)return Path.GetFullPath("Logs/StationBatteryBays/settings/player-settings.json");
                if(Array.IndexOf(args,"-stationWorkshopCheck")>=0)return Path.GetFullPath("Logs/StationWorkshop/settings/player-settings.json");
                if(Array.Exists(args,a=>a=="-stationOperationsCheck"||a=="-stationOperationsUiCheck"||a=="-stationOperationsPreview"))return Path.GetFullPath("Logs/StationOperations-settings/player-settings.json");
                if (Array.IndexOf(args,"-stationKeilCheck")>=0 || Array.IndexOf(args,"-stationKeilPreview")>=0 || Array.IndexOf(args,"-startStationCheck")>=0)
                    return Path.GetFullPath("Logs/KeilStation-settings/player-settings.json");
                if (Array.IndexOf(args, "-stationConstructionPreview") >= 0 || Array.IndexOf(args, "-stationConstructionCheck") >= 0)
                    return Path.GetFullPath("Logs/StationConstruction-settings/player-settings.json");
                if (Array.IndexOf(args, "-tripoMiningShowcase") >= 0 || Array.IndexOf(args, "-tripoMiningCheck") >= 0)
                    return Path.GetFullPath("Logs/TripoMining-settings/player-settings.json");
                if (Array.IndexOf(args, "-maintenanceShowcase") >= 0 || Array.IndexOf(args, "-maintenanceShowcaseCheck") >= 0)
                    return Path.GetFullPath("Logs/MaintenanceShowcase-settings/player-settings.json");
                if (Array.IndexOf(args, "-tripoModelPreview") >= 0 || Array.IndexOf(args, "-tripoModelCheck") >= 0)
                    return Path.GetFullPath("Logs/TripoModels-settings/player-settings.json");
                if (Array.IndexOf(args,"-storagePreview")>=0)
                    return Path.GetFullPath("Logs/StoragePreview-settings/player-settings.json");
                if (Array.IndexOf(args, "-startStationCheck") >= 0)
                    return Path.GetFullPath("Logs/Startstation-settings/player-settings.json");
                if (Array.IndexOf(args, "-ringWalkthroughCheck") >= 0)
                    return Path.GetFullPath("Logs/RingWalkthrough-settings/player-settings.json");
                if (Array.IndexOf(args, "-playerVitalsCheck") >= 0)
                    return Path.GetFullPath("Logs/PlayerVitals/player-settings.json");
                if (Array.IndexOf(args, "-ringSimulatorCheck") >= 0)
                    return Path.GetFullPath("Logs/RingSimulator-settings/player-settings.json");
                if (Array.IndexOf(args, "-presentationCheck") >= 0)
                    return Path.GetFullPath("Logs/Presentation-settings/player-settings.json");
                if (Array.IndexOf(args, "-stationUiAudioCheck") >= 0 || Array.IndexOf(args, "-stationUiAudioPreview") >= 0)
                    return Path.GetFullPath("Logs/StationUiAudio/player-settings.json");
#endif
                return Path.Combine(Application.persistentDataPath, "player-settings.json");
            }
        }
        public static void Load()
        {
            if (!Service.Load(out string error)) Debug.LogWarning("Settings load: " + error);
        }
        public static bool Save(SpaceMinerPlayerSettings draft, out string error) => Service.Save(draft, out error);
        private static void ApplyRuntime(SpaceMinerPlayerSettings settings)
        {
            SettingsRuntime.Apply(settings, DefaultQuality);
            Applied?.Invoke(settings);
        }
    }
}
