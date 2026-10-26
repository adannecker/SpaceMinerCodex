#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    public sealed class MiraDialogueModuleCheck : MonoBehaviour
    {
        private const string Folder = "Logs/MiraDialogue";
        private int checks;
        private bool failed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-miraDialogueCheck") >= 0 || Array.IndexOf(args, "-miraDialogueIntegrationCheck") >= 0)
                new GameObject("Mira dialogue acceptance").AddComponent<MiraDialogueModuleCheck>();
        }
        private void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("MIRA DIALOGUE: " + message);
            checks++;
            Debug.Log("MIRA DIALOGUE OK: " + message);
        }
        private void Step(Action action)
        {
            if (failed) return;
            try { action(); }
            catch (Exception e)
            {
                failed = true;
                File.WriteAllText(Folder + "/error.txt", e.ToString());
                Debug.LogException(e); Application.Quit(1);
            }
        }
        private IEnumerator Capture(string name)
        {
            string path = Path.GetFullPath(Folder + "/" + name + ".png");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSecondsRealtime(.25f);
            Step(() => Require(File.Exists(path) && new FileInfo(path).Length > 1000, "framebuffer screenshot saved: " + name));
        }
        private IEnumerator Start()
        {
            Directory.CreateDirectory(Folder);
            yield return null;
            bool integration = Array.IndexOf(Environment.GetCommandLineArgs(), "-miraDialogueIntegrationCheck") >= 0;
            if (integration) yield return Integration();
            else yield return Demo();
            if (failed) yield break;
            File.WriteAllText(Folder + (integration ? "/game-result.json" : "/demo-result.json"),
                "{\"passed\":true,\"checks\":" + checks + ",\"width\":" + Screen.width + ",\"height\":" + Screen.height + "}");
            Debug.Log("MIRA DIALOGUE " + (integration ? "INTEGRATION" : "DEMO") + " CHECK PASSED: " + checks);
            Application.Quit(0);
        }
        private IEnumerator Demo()
        {
            var demo = FindFirstObjectByType<MiraDialogueDemo>();
            MiraDialogueModule module = null;
            Step(() => {
                Require(demo != null, "isolated demo host present");
                module = demo.Dialogue;
                Require(FindFirstObjectByType<WaterScenario>() == null && FindFirstObjectByType<OrbitCamera>() == null
                    && FindFirstObjectByType<SettingsMenu>() == null, "no game world, campaign menu or simulation in demo");
                Require(demo.Script.Cues.Length == 16, "shared awakening has sixteen cues");
                int voices = 0;
                for (int i = 0; i < demo.Script.Cues.Length; i++)
                {
                    demo.Play(i);
                    if (!module.TextOnly) voices++;
                    Require(module.Current == demo.Script.Cues[i], "actual catalog line " + i);
                    Require(!module.MissingRecording, "assigned recording exists " + i);
                    Require(module.Duration >= 3.5f, "reading/voice duration " + i);
                }
                Require(voices == 16, "sixteen recorded awakening lines");
                demo.Play(1);
            });
            if (failed) yield break;
            yield return new WaitForSecondsRealtime(.5f);
            Step(() => Require(module.Speech.isPlaying && module.Speech.time > .1f, "real AudioSource playback advances"));
            float audioTime = module.Speech.time;
            float elapsed = module.Elapsed;
            Step(() => module.SetPaused(true));
            yield return new WaitForSecondsRealtime(.3f);
            Step(() => {
                Require(!module.Speech.isPlaying && Mathf.Abs(module.Speech.time - audioTime) < .1f, "pause retains audio position");
                Require(module.Elapsed == elapsed, "pause retains caption clock");
                module.SetPaused(false);
            });
            yield return new WaitForSecondsRealtime(.25f);
            Step(() => {
                Require(module.Speech.isPlaying && module.Speech.time > audioTime, "resume continues same recording");
                module.SetVolume(.4f);
                PlayerAudio.Preview(new AudioSettings { VoicesEnabled = false });
            });
            yield return null;
            Step(() => Require(module.Speech.volume == 0 && module.Speech.isPlaying, "voice mute preserves playback"));
            Step(() => PlayerAudio.EndPreview());
            yield return null;
            Step(() => {
                Require(Mathf.Abs(module.Speech.volume - .4f) < .001f, "voice gain restored");
                demo.Play(0);
                Require(module.Speech.time < .1f && !module.Paused, "replacement/replay resets voice and pause");
            });
            yield return Capture("demo-voiced-" + Screen.width);
            Step(() => { module.Subtitles = false; });
            yield return Capture("demo-subtitles-off-" + Screen.width);
            Step(() => {
                module.Subtitles = true;
                module.Play(new MiraDialogueLine { Heading = "FEHLENDE AUFNAHME", Text = "Dieser Testtext besitzt bewusst keine Sprachdatei.", Audio = "Mira/missing-acceptance-clip" });
                Require(module.TextOnly && module.MissingRecording && !module.Speech.isPlaying, "missing voice is explicit text fallback without stale audio");
                module.AutomaticClock = false;
                int completions = 0;
                module.LineCompleted += () => completions++;
                module.Advance(module.Duration + 1); module.Advance(100);
                Require(module.Completed && completions == 1, "completion event once, acknowledgement stays visible");
                module.Close();
                Require(!module.Visible && module.Speech.clip == null && !module.Speech.isPlaying, "close clears voice and presentation");
                module.Play(new MiraDialogueLine { Heading = "LANGE ANWEISUNG", Text = new string('W', 2500), Mood = MiraAvatar.Mood.Concerned });
            });
            yield return Capture("demo-long-text-" + Screen.width);
            Step(() => {
                demo.SetContinuous(true); demo.Play(0);
                for (int i = 0; i < 16; i++) module.Advance(module.Duration + 1);
                Require(demo.Selected == 15 && !demo.Continuous && module.Completed, "continuous sequence finishes all sixteen lines");
                demo.Play(4); Require(module.Current.Mood == MiraAvatar.Mood.Concerned, "shared concerned portrait");
                demo.Play(12); Require(module.Current.Mood == MiraAvatar.Mood.Focused, "shared focused portrait");
                demo.Play(15); Require(module.Current.Mood == MiraAvatar.Mood.Encouraging, "shared encouraging portrait");
                module.enabled = false;
                Require(!module.Visible && !module.Speech.isPlaying, "disabling module stops voice");
            });
        }
        private IEnumerator Integration()
        {
            yield return new WaitForSecondsRealtime(1);
            var intro = FindFirstObjectByType<IntroSequence>();
            var start = FindFirstObjectByType<StartMenu>();
            var scenario = FindFirstObjectByType<WaterScenario>();
            Step(() => {
                Require(intro != null && start != null && scenario != null, "real main-game components present");
                CinematicLibrary.PreferencePrefix = "SpaceMiner.Test.MiraDialogue.";
                FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride = Path.GetFullPath(Folder + "/isolated-saves");
                start.StartDemo();
                intro.PlayIntro(true);
                Require(intro.DialogueModule.Visible && intro.VoicedCueCount == 16, "awakening uses shared module with sixteen recordings");
                intro.AdvancePlayback(1);
                Require(Mathf.Abs(intro.DialogueModule.Elapsed - 1) < .01f, "awakening clock advances shared module");
            });
            if (failed) yield break;
            yield return Capture("game-awakening");
            Step(() => {
                intro.AdvancePlayback(10000);
                Require(!IntroSequence.IsPlaying && !intro.DialogueModule.Visible && !intro.DialogueModule.Speech.isPlaying,
                    "complete awakening closes shared module");
                scenario.Scanner.Say(new MiraDialogueLine { Heading = "MODULTEST IM SPIEL", Text = "Ich bin Mira. Dein Bordcomputer.", Audio = "Intro/mira_01", Mood = MiraAvatar.Mood.Friendly });
            });
            yield return new WaitForSecondsRealtime(.4f);
            Step(() => {
                Require(scenario.Scanner.DialogueCanRender && scenario.Scanner.DialogueModule.Visible, "station dialogue uses renderable shared module");
                Require(scenario.Scanner.DialogueModule.Speech.isPlaying, "station dialogue supports same voice playback");
            });
            yield return Capture("game-station-dialogue");
            Step(() => {
                scenario.Scanner.DismissDialogue();
                Require(scenario.Scanner.Dialogue == null && !scenario.Scanner.DialogueModule.Visible
                    && !scenario.Scanner.DialogueModule.Speech.isPlaying, "acknowledgement releases station dialog and audio");
                scenario.Scanner.Say("TEXT OHNE AUFNAHME", "Bestehende Questtexte bleiben lesbar und verlangen eine Bestätigung.");
                Require(scenario.Scanner.DialogueModule.TextOnly && !scenario.Scanner.DialogueModule.MissingRecording,
                    "existing text-only game quest remains explicit");
                scenario.Scanner.DismissDialogue();
            });
        }
    }
}
#endif
