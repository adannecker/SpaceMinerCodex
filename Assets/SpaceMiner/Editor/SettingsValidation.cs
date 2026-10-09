using System;
using UnityEngine;
namespace SpaceMiner.Editor
{
    public static class SettingsValidation
    {
        public static void BuildChecks()
        {
            Run();
            foreach (bool development in new[] { true, false })
            {
                var report = UnityEditor.BuildPipeline.BuildPlayer(new UnityEditor.BuildPlayerOptions
                {
                    scenes = new[] { "Assets/SpaceMiner/Scenes/AsteroidBelt.unity" },
                    locationPathName = development ? "Builds/SettingsDevelopment/SpaceMiner.exe" : "Builds/SettingsRelease/SpaceMiner.exe",
                    target = UnityEditor.BuildTarget.StandaloneWindows64,
                    options = development ? UnityEditor.BuildOptions.Development : UnityEditor.BuildOptions.None
                });
                if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Settings build failed");
            }
            Debug.Log("SETTINGS DEVELOPMENT AND RELEASE BUILDS PASSED");
        }
        public static void Run()
        {
            string restartPath = System.IO.Path.GetFullPath("Logs/settings-test/restart-" + Guid.NewGuid().ToString("N") + ".json");
            int applications = 0;
            var first = new PlayerSettingsService(new LocalPlayerSettingsStorage(restartPath), data => applications++);
            Require(first.Load(out _) && applications == 1, "first startup defaults");
            var edited = first.Current.Copy(); edited.Audio.Master = .42f; edited.Controls.CameraSpeed = 2;
            Require(first.Save(edited, out _), "Apply saves settings");
            edited.Audio.Master = .9f;
            var restarted = new PlayerSettingsService(new LocalPlayerSettingsStorage(restartPath), data => applications++);
            Require(restarted.Load(out _) && restarted.Current.Audio.Master == .42f && restarted.Current.Controls.CameraSpeed == 2, "restart restores applied values");
            var failing = new PlayerSettingsService(new FailingStorage(), data => applications++);
            int before = applications;
            Require(!failing.Save(edited, out string failure) && failure != null && failing.Current.Audio.Master == 1 && applications == before, "save failure preserves runtime");
            Require(!failing.Load(out _) && failing.Current.Audio.Master == 1, "load failure defaults");
            Require(first.Save(new SpaceMinerPlayerSettings(), out _) && restarted.Load(out _) && restarted.Current.Controls.CameraSpeed == 1, "saved defaults survive restart");
            System.IO.File.Delete(restartPath); System.IO.File.Delete(restartPath + ".bak");
            var session = new SettingsSession(); session.Begin();
            string original = JsonUtility.ToJson(SettingsStore.Current);
            session.Draft.Controls.CameraSpeed = 2.5f; session.RestoreDefaults();
            Require(session.Draft.Controls.CameraSpeed == 1, "restore defaults edits draft");
            session.Draft.Accessibility.Subtitles = false; session.Cancel();
            Require(session.Draft == null && JsonUtility.ToJson(SettingsStore.Current) == original, "cancel preserves live settings");
            Require(!session.Apply(out _), "cannot apply cancelled session");
            var bindings = new CameraBindings();
            Require(bindings.TryAssign(CameraAction.Forward, KeyCode.S) && bindings.Get(CameraAction.Backward) == KeyCode.W, "binding conflict swaps");
            Require(!bindings.TryAssign(CameraAction.Forward, KeyCode.F10), "developer shortcut reserved");
            bindings.Keys = null; bindings.Validate(); Require(bindings.Get(CameraAction.Forward) == KeyCode.W, "legacy binding defaults");
            Require(bindings.Get(CameraAction.Technology)==KeyCode.T,"technology default shortcut");
            var legacyKeys=new CameraBindings();legacyKeys.TryAssign(CameraAction.Forward,KeyCode.T);
            legacyKeys.Keys=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Take(legacyKeys.Keys,10));
            var oldKeys=(KeyCode[])legacyKeys.Keys.Clone();legacyKeys.Validate();
            Require(System.Linq.Enumerable.SequenceEqual(oldKeys,System.Linq.Enumerable.Take(legacyKeys.Keys,10)),"legacy custom camera keys preserved");
            Require(legacyKeys.Keys.Length==11&&legacyKeys.Get(CameraAction.Technology)!=KeyCode.T,"legacy T conflict migrated to unused shortcut");
            var settings = new SpaceMinerPlayerSettings();
            var copy = settings.Copy(); copy.Controls.Sensitivity = 2;
            Require(settings.Controls.Sensitivity == 1, "draft isolation");
            copy.Audio.Master = float.NaN; copy.Interface.Scale = 99; copy.Graphics.FrameLimit = -1;
            copy.Graphics.DisplayMode = 99; copy.Graphics.Width = 99999; copy.Graphics.Height = 99999; copy.Controls.CameraSpeed = float.PositiveInfinity; copy.Accessibility.Subtitles = false;
            copy.Validate();
            Require(copy.Graphics.DisplayMode == 2 && copy.Graphics.Width == 7680 && copy.Graphics.Height == 4320 && copy.Controls.CameraSpeed == 1, "display and camera validation");
            Require(copy.Audio.Master == 1 && copy.Interface.Scale == 1.5f && copy.Graphics.FrameLimit == 30, "validation");
            var legacy = JsonUtility.FromJson<SpaceMinerPlayerSettings>("{\"Version\":1,\"Controls\":null}"); legacy.Validate();
            Require(legacy.Controls != null && legacy.Accessibility != null, "missing categories");
            var oldAudio = JsonUtility.FromJson<SpaceMinerPlayerSettings>("{\"Audio\":{\"Master\":0.5}}"); oldAudio.Validate();
            Require(oldAudio.Audio.Background == 1 && oldAudio.Audio.VoicesEnabled && oldAudio.Audio.Ui > 0, "legacy audio channels default enabled");
            session.Begin(); float savedMaster = AudioListener.volume;
            session.Draft.Audio.Master = .2f; session.Draft.Audio.Background = .3f; session.Draft.Audio.VoicesEnabled = false; session.PreviewAudio();
            Require(Mathf.Approximately(AudioListener.volume,.2f) && Mathf.Approximately(PlayerAudio.Gain(PlayerAudioChannel.Background),.3f) && PlayerAudio.Gain(PlayerAudioChannel.Voices)==0, "live audio preview");
            session.Cancel(); Require(Mathf.Approximately(AudioListener.volume,savedMaster), "cancel restores audio preview");
            copy.Audio.Background = .31f; copy.Audio.EffectsEnabled = false; copy.Audio.Ui = .22f;
            Require(new SpaceMinerPlayerSettings().Gameplay.PauseInMenu, "default pause");
            string path = System.IO.Path.GetFullPath("Logs/settings-test/player-settings.json");
            PlayerSettingsFile.Write(path, settings);
            PlayerSettingsFile.Write(path, copy);
            var stored = PlayerSettingsFile.Read(path);
            Require(stored.Controls.Sensitivity == 2 && stored.Graphics.FrameLimit == 30, "disk roundtrip");
            Require(!stored.Accessibility.Subtitles && stored.Graphics.DisplayMode == 2, "extended settings roundtrip");
            Require(stored.Audio.Background == .31f && !stored.Audio.EffectsEnabled && stored.Audio.Ui == .22f, "audio channels persistence");
            Require(PlayerSettingsFile.Read(path + ".bak").Controls.Sensitivity == 1, "previous file backup");
            Debug.Log("SETTINGS VALIDATION PASSED: draft isolation, invalid values, missing categories, defaults, disk roundtrip, backup");
        }
        private sealed class FailingStorage : IPlayerSettingsStorage
        {
            public SpaceMinerPlayerSettings Load() => throw new System.IO.IOException("test load failure");
            public void Save(SpaceMinerPlayerSettings data) => throw new System.IO.IOException("test save failure");
        }
        private static void Require(bool success, string message) { if (!success) throw new Exception(message); }
    }
}
