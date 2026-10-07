#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using UnityEngine;
namespace SpaceMiner
{
    public sealed class SplashAudioCheck : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-splashAudioCheck") >= 0)
                new GameObject("Splash audio check").AddComponent<SplashAudioCheck>();
        }
        private IEnumerator Start()
        {
            yield return null;
            // Isolated live preview; never writes the user's saved audio settings.
            PlayerAudio.Preview(new AudioSettings());
            var audio = FindFirstObjectByType<ConfigurationAudio>();
            var menu = FindFirstObjectByType<SettingsMenu>();
            var start = FindFirstObjectByType<StartMenu>();
            Require(audio != null && menu != null && start != null && StartMenu.IsOpen, "bootstrap");
            var splash = audio.SplashSource;
            Require(splash.clip != null && splash.clip.name == "Orbal-Observation-Loop" && Mathf.Abs(splash.clip.length - 185) < .1f, "splash track");
            yield return new WaitForSecondsRealtime(1.5f);
            Require(splash.isPlaying && splash.volume > .3f && !audio.PlaybackSource.isPlaying && !audio.AmbienceSource.isPlaying, "splash only");
            PlayerAudio.Preview(new AudioSettings { BackgroundEnabled = false });
            yield return null;
            Require(splash.volume == 0 && splash.isPlaying, "background mute");
            PlayerAudio.Preview(new AudioSettings());
            splash.time = splash.clip.length - .4f;
            yield return new WaitForSecondsRealtime(1);
            Require(splash.isPlaying && splash.time < 3, "splash loop");
            menu.Open();
            yield return new WaitForSecondsRealtime(1.5f);
            Require(!splash.isPlaying && splash.volume == 0 && audio.PlaybackSource.isPlaying, "configuration switch");
            float position = splash.time;
            menu.Cancel();
            yield return new WaitForSecondsRealtime(1.5f);
            Require(splash.isPlaying && splash.time >= position && splash.volume > .3f && !audio.PlaybackSource.isPlaying, "return to splash");
            start.StartDemo();
            yield return new WaitForSecondsRealtime(1.5f);
            Require(!splash.isPlaying && splash.volume == 0 && audio.AmbienceSource.isPlaying && IntroSequence.IsPlaying, "intro handoff");
            PlayerAudio.EndPreview();
            Debug.Log("SPLASH AUDIO CHECK PASSED");
            Application.Quit(0);
        }
        private static void Require(bool condition, string message)
        {
            if (condition) return;
            Debug.LogError("SPLASH AUDIO CHECK FAILED: " + message);
            PlayerAudio.EndPreview(); Application.Quit(1); throw new Exception(message);
        }
    }
}
#endif
