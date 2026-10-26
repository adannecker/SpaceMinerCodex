#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    public sealed class MiraQuestDialogueCheck : MonoBehaviour
    {
        private const string Folder = "Logs/MiraQuests";
        private int checks, pictures;
        private bool failed;
        private bool game;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-miraQuestsCheck") >= 0 || Array.IndexOf(args, "-miraQuestGameCheck") >= 0)
                new GameObject("Mira quest acceptance").AddComponent<MiraQuestDialogueCheck>();
        }
        private void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("MIRA QUEST: " + message);
            checks++; Debug.Log("MIRA QUEST OK: " + message);
        }
        private void Step(Action action)
        {
            if (failed) return;
            try { action(); }
            catch (Exception e) { failed = true; File.WriteAllText(Folder + "/error.txt", e.ToString()); Debug.LogException(e); Application.Quit(1); }
        }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Folder + "/" + name + ".png", image.EncodeToPNG());
            Destroy(image); pictures++;
        }
        private IEnumerator Start()
        {
            Directory.CreateDirectory(Folder);
            game = Array.IndexOf(Environment.GetCommandLineArgs(), "-miraQuestGameCheck") >= 0;
            yield return new WaitForSecondsRealtime(.7f);
            yield return game ? Game() : Demo();
            if (failed) yield break;
            File.WriteAllText(Folder + "/result.json", "{\"passed\":true,\"game\":" + (game ? "true" : "false")
                + ",\"checks\":" + checks + ",\"pictures\":" + pictures + ",\"width\":" + Screen.width + ",\"height\":" + Screen.height + "}");
            Debug.Log("MIRA QUEST CHECK PASSED: " + checks); Application.Quit(0);
        }
        private IEnumerator Demo()
        {
            var demo = FindFirstObjectByType<MiraDialogueDemo>();
            Step(() => Require(demo != null && demo.Script.Cues.Length == 16, "awakening remains separate default"));
            if (failed) yield break;
            var module = demo.Dialogue; module.AutomaticClock = false;
            for (int q = 0; q < 9; q++)
            {
                int index = q;
                Step(() => {
                    demo.SelectQuest(index);
                    Require(demo.SelectedQuest == index && demo.Continuous && demo.Selected == 0, "quest starts its own whole conversation " + index);
                    Require(demo.Script.Cues.Length == 2 && demo.ConversationDuration > 10, "introduction and completion grouped " + index);
                });
                for (int part = 0; part < 2; part++)
                {
                    int section = part;
                    Step(() => {
                        demo.Play(section);
                        Require(module.Current.Id == "quest_" + index.ToString("00") + (section == 0 ? "_start" : "_complete"), "correct independent recording");
                        Require(!module.TextOnly && !module.MissingRecording && module.Speech.clip != null, "real local quest audio");
                        Require(Resources.Load<TextAsset>("MiraVisemes/" + module.Speech.clip.name) != null, "recording has offline lip timings");
                        Require(module.Current.Acting != null, "quest has attentive acting");
                    });
                    yield return new WaitForSecondsRealtime(.18f);
                    Step(() => Require(module.Speech.isPlaying && module.Speech.time > .03f && module.Portrait.Performance.HasTiming,
                        "AudioSource and phonetic face performance advance"));
                }
                Step(() => {
                    demo.PlayWholeConversation(); module.Advance(module.Duration + 1);
                    Require(demo.Selected == 1 && demo.SelectedQuest == index, "whole conversation advances within selected quest");
                    module.Advance(module.Duration + 1);
                    Require(!demo.Continuous && demo.Selected == 1 && demo.SelectedQuest == index, "quest ends without entering another quest");
                });
                if (failed) yield break;
            }
            Step(() => { demo.SelectQuest(1); module.SetVolume(.43f); });
            yield return new WaitForSecondsRealtime(.6f);
            float time = module.Speech.time;
            Step(() => module.SetPaused(true));
            yield return new WaitForSecondsRealtime(.3f);
            Step(() => Require(Mathf.Abs(module.Speech.time - time) < .08f && module.Portrait.Performance.CurrentViseme == "X", "pause retains voice and closes lips"));
            Step(() => module.SetPaused(false));
            yield return new WaitForSecondsRealtime(.25f);
            Step(() => Require(module.Speech.time > time + .1f, "quest speech resumes"));
            yield return Capture("demo-quest-water");
            Step(() => {
                demo.SelectHelpers(); Require(demo.ShowingHelpers && demo.Script.Cues.Length == 4 && !demo.Continuous, "support has its own category");
                foreach (var line in demo.Script.Cues) { module.Play(line); Require(!module.TextOnly, "each workflow is recorded"); }
                demo.SelectQuest(8); demo.PlayWholeConversation();
                Require(module.Speech.time < .15f && !module.Paused && Mathf.Abs(module.Volume - .43f) < .001f, "selection restarts clip and retains volume");
            });
            yield return Capture("demo-quest-repair");
            Step(() => {
                demo.SelectAwakening(); Require(demo.SelectedQuest == -1 && demo.Script.Cues.Length == 16 && !demo.Continuous, "return to intro resets sequence scope");
                demo.SelectQuest(0); module.Close(); Require(!demo.Continuous && !module.Speech.isPlaying, "stop releases whole quest playback");
            });
        }
        private IEnumerator Game()
        {
            var scenario = FindFirstObjectByType<WaterScenario>();
            var intro = FindFirstObjectByType<IntroSequence>();
            var start = FindFirstObjectByType<StartMenu>();
            StationOperations ops = null;
            Step(() => {
                Require(scenario != null && intro != null && start != null, "real game host");
                FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride = Path.GetFullPath(Folder + "/isolated-saves");
                CinematicLibrary.PreferencePrefix = "SpaceMiner.Test.MiraQuests.";
                start.StartDemo(); intro.PlayIntro(true); intro.AdvancePlayback(10000);
                ops = scenario.Operations; Require(ops != null && ops.Live, "real campaign quest backend installed");
                ops.ClosePanel(); scenario.Scanner.DismissDialogue(); ops.State.miraDialogues.Clear();
            });
            if (failed) yield break;
            // Intro completion deliberately consumes its input frame. Normal gameplay's
            // LateUpdate waits for that guard too; the fixture must cross the same boundary.
            yield return null; yield return null;
            Step(() => Require(ops.TickMiraQuestDialogue(), "first quest introduced automatically"));
            if (failed) yield break;
            var module = scenario.Scanner.DialogueModule;
            yield return new WaitForSecondsRealtime(.5f);
            Step(() => {
                Require(module.Current.Id == "quest_00_start" && module.Speech.isPlaying, "in-game quest uses same local recording");
                ops.State.diagnosed = true;
                Require(!ops.TickMiraQuestDialogue() && module.Current.Id == "quest_00_start", "parallel completion cannot interrupt speaking");
                ops.State.diagnosed = false;
            });
            yield return Capture("game-quest-scan");
            for (int q = 0; q < 9; q++)
            {
                int index = q;
                Step(() => {
                    scenario.Scanner.DismissDialogue();
                    if (index == 0) { scenario.Scanner.Restore(0, true, false, false, false); scenario.Scanner.DismissDialogue(); }
                    if (index == 1) ops.State.waterSecured = true;
                    if (index == 2) ops.State.diagnosed = true;
                    if (index == 3) { ops.State.debrisBatches = 1; ops.State.collected.Add("debris-0"); }
                    if (index == 4) ops.State.helper = true;
                    if (index == 5) { ops.State.carbonBatches = 1; ops.State.collected.Add("carbon-0"); }
                    if (index == 6) ops.State.foodProduced = true;
                    if (index == 7) ops.State.second = true;
                    if (index == 8) ops.State.repairExperience = 1;
                    Require(ops.TickMiraQuestDialogue(), "real quest predicate dispatches completion " + index);
                    Require(module.Current.Id == "quest_" + index.ToString("00") + "_complete" && !module.TextOnly, "matching spoken success " + index);
                    Require(!ops.TickMiraQuestDialogue(), "no duplicate while visible");
                    scenario.Scanner.DismissDialogue();
                    if (index < 8)
                    {
                        Require(ops.TickMiraQuestDialogue(), "next unfinished quest is explained");
                        Require(module.Current.Id == "quest_" + (index + 1).ToString("00") + "_start", "next quest instruction matches progress");
                    }
                });
                yield return null;
            }
            Step(() => {
                scenario.Scanner.DismissDialogue();
                Require(!ops.TickMiraQuestDialogue(), "all quests complete has no repeated final quest");
                var saved = ops.Capture();
                Require(saved.miraDialogues.Count == 18 && StationOperations.Valid(saved), "all eighteen once-only states captured in valid save");
                ops.Restore(saved); Require(!ops.TickMiraQuestDialogue(), "save restore preserves narrative history");
                saved.miraDialogues.Clear(); ops.Restore(saved);
                Require(ops.State.miraDialogues.Count == 18 && !ops.TickMiraQuestDialogue(), "legacy save seeds already completed quests silently");
                ops.ExplainQuest(8);
                Require(module.Current.Id == "quest_08_complete" && module.Speech.isPlaying, "completed quest remains manually replayable");
            });
            yield return Capture("game-quest-complete");
        }
    }
}
#endif
