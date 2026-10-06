#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using UnityEngine;

namespace SpaceMiner
{
    public sealed class ConfigurationAudioCheck : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-configurationAudioCheck") >= 0)
                new GameObject("Configuration Audio Check").AddComponent<ConfigurationAudioCheck>();
        }
        private IEnumerator Start()
        {
            yield return null;
            var menu = FindFirstObjectByType<SettingsMenu>();
            var audio = FindFirstObjectByType<ConfigurationAudio>();
            Require(menu != null && audio != null, "bootstrap");
            var source = audio.PlaybackSource;
            Require(source.clip != null && source.clip.name == "Deep-Space-Configuration-Loop" && source.clip.channels == 2 && Mathf.Abs(source.clip.length - 172) < .1f, "prepared stereo loop loaded");
            Require(source.loop && !source.isPlaying && source.volume == 0, "silent before opening");
            var space = audio.AmbienceSource;
            Require(space.clip != null && space.clip.name == "Asteroid-Solitude-Loop" && Mathf.Abs(space.clip.length - 337) < .1f, "gameplay loop loaded");
            yield return new WaitForSecondsRealtime(1.5f);
            Require(space.isPlaying && space.volume <= .11f, "quiet ambience during intro");
            var intro = FindFirstObjectByType<IntroSequence>();
            if (intro != null) intro.Skip();
            yield return new WaitForSecondsRealtime(1.5f);
            Require(space.isPlaying && space.volume >= .24f, "gameplay ambience active");
            float oldTime = Time.timeScale, oldMaster = AudioListener.volume;
            Time.timeScale = 0;
            menu.Open(); menu.Open();
            yield return new WaitForSecondsRealtime(1.5f);
            Require(source.isPlaying && source.volume > .3f, "fade in while paused");
            Require(!space.isPlaying && space.volume == 0, "gameplay fades and pauses in menu");
            float pausedPosition = space.time;
            Require(FindObjectsByType<ConfigurationAudio>(FindObjectsSortMode.None).Length == 1, "one player on repeated open");
            AudioListener.volume = 0;
            Require(source.isPlaying && AudioListener.volume == 0, "master mute preserves playback");
            AudioListener.volume = oldMaster;
            menu.Cancel();
            yield return new WaitForSecondsRealtime(.3f);
            Require(source.isPlaying && source.volume > 0 && source.volume < .35f, "fade out");
            menu.Open();
            yield return new WaitForSecondsRealtime(1.5f);
            Require(source.isPlaying && source.volume > .3f, "reopen during fade");
            Require(!space.isPlaying && space.time >= pausedPosition, "ambience preserved across quick reopen");
            source.time = source.clip.length - .4f;
            yield return new WaitForSecondsRealtime(1);
            Require(source.isPlaying && source.time < 3, "loop boundary");
            menu.Cancel();
            yield return new WaitForSecondsRealtime(1.5f);
            Require(!source.isPlaying && source.volume == 0, "stop after fade");
            Require(space.isPlaying && space.volume >= .24f && space.time >= pausedPosition, "gameplay resumes after menu");
            space.time = space.clip.length - .4f;
            yield return new WaitForSecondsRealtime(1);
            Require(space.isPlaying && space.time < 3, "gameplay loop boundary");
            menu.Open();
            yield return new WaitForSecondsRealtime(.2f);
            Require(source.isPlaying && source.time < 1, "fresh start after stop");
            yield return new WaitForSecondsRealtime(1.3f);
            var preview = SettingsStore.Current.Copy(); preview.Audio.Background = .2f;
            PlayerAudio.Preview(preview.Audio); yield return null; yield return null;
            Require(Mathf.Abs(source.volume - .07f) < .005f, "background slider immediately changes playing track");
            preview.Audio.BackgroundEnabled = false; PlayerAudio.Preview(preview.Audio); yield return null; yield return null;
            Require(source.isPlaying && source.volume == 0, "background toggle mutes without restarting");
            menu.Cancel(); yield return null; yield return null;
            Require(source.volume > .25f, "cancel restores background channel");
            audio.enabled = false;
            Require(!source.isPlaying && !space.isPlaying, "disable stops both tracks");
            menu.Cancel(); Time.timeScale = oldTime; AudioListener.volume = oldMaster;
            Debug.Log("CONFIGURATION AUDIO CHECK PASSED (no listening verification)");
            Application.Quit(0);
        }
        private static void Require(bool condition, string message)
        {
            if (condition) return;
            Debug.LogError("CONFIGURATION AUDIO CHECK FAILED: " + message);
            Application.Quit(1);
            throw new Exception(message);
        }
    }
}
#endif
