#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    public sealed class MiraDialogueActingCheck : MonoBehaviour
    {
        private const string Folder = "Logs/MiraActing";
        private int checks, pictures;
        private bool failed;
        private MiraDialogueDemo demo;
        private MiraDialogueModule module;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-miraActingCheck") >= 0)
                new GameObject("Mira awakening acting acceptance").AddComponent<MiraDialogueActingCheck>();
        }
        private void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("MIRA ACTING: " + message);
            checks++; Debug.Log("MIRA ACTING OK: " + message);
        }
        private void Step(Action action)
        {
            if (failed) return;
            try { action(); }
            catch (Exception e) { failed = true; File.WriteAllText(Folder + "/error.txt", e.ToString()); Debug.LogException(e); Application.Quit(1); }
        }
        private IEnumerator Capture(string label)
        {
            yield return new WaitForEndOfFrame();
            var previous = RenderTexture.active; var target = module.Portrait.Texture;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
                File.WriteAllBytes(Folder + "/" + label + "-portrait.png", image.EncodeToPNG()); pictures++;
            }
            finally { RenderTexture.active = previous; Destroy(image); }
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Folder + "/" + label + "-studio.png"));
            yield return new WaitForSecondsRealtime(.2f);
            Step(() => Require(File.Exists(Folder + "/" + label + "-studio.png"), "rendered dialogue " + label)); pictures++;
        }
        private IEnumerator Start()
        {
            Directory.CreateDirectory(Folder);
            yield return new WaitForSecondsRealtime(.5f);
            Step(() => {
                demo = FindFirstObjectByType<MiraDialogueDemo>(); Require(demo != null, "standalone host"); module = demo.Dialogue;
                Require(module.Portrait.Mode == MiraVisualMode.HighModel, "studio opens preferred High model");
                Require(module.Portrait.Performance.Torso != null && module.Portrait.Performance.LeftShoulder != null && module.Portrait.Performance.RightShoulder != null,
                    "independent torso and both shoulder bones");
                Require(FindFirstObjectByType<WaterScenario>() == null, "no campaign simulation");
                module.AutomaticClock = false;
                var mesh = module.Portrait.ActiveAvatar.Face.sharedMesh;
                foreach (var weight in mesh.boneWeights)
                    if (Mathf.Abs(weight.weight0 + weight.weight1 + weight.weight2 + weight.weight3 - 1) > .0001f)
                        throw new Exception("Invalid skin weights");
                Require(true, "all vertex weights normalized");
            });
            if (failed) yield break;
            float maxAngle = 0, maxBody = 0, maxShoulder = 0; int totalVisemes = 0;
            for (int line = 0; line < 16; line++)
            {
                int index = line;
                Step(() => { demo.Play(index); Require(module.Current.Acting != null, "authored awakening profile " + index); });
                yield return null;
                if (failed) yield break;
                Step(() => Require(module.Portrait.Performance.Acting == module.Current.Acting, "shared module forwards acting " + index));
                var performance = module.Portrait.Performance;
                int before = performance.VisemeChanges;
                float began = Time.realtimeSinceStartup;
                bool captured = false;
                do
                {
                    yield return null;
                    maxAngle = Mathf.Max(maxAngle, performance.HeadAngles.magnitude);
                    maxBody = Mathf.Max(maxBody, performance.TorsoAngles.magnitude);
                    maxShoulder = Mathf.Max(maxShoulder, Mathf.Abs(performance.ShoulderLift));
                    if (!captured && Time.realtimeSinceStartup - began > (line == 15 ? .65f : 1.2f) && (line == 0 || line == 4 || line == 8 || line == 15))
                    { captured = true; yield return Capture("cue-" + line.ToString("00")); }
                } while ((module.Speech.isPlaying || Time.realtimeSinceStartup - began < 1.5f) && Time.realtimeSinceStartup - began < 16);
                Step(() => {
                    Require(!module.Speech.isPlaying, "recording completes " + index);
                    if (module.TextOnly) Require(performance.CurrentViseme == "X", "unvoiced instruction has no speech " + index);
                    else Require(performance.VisemeChanges > before + 2, "lip timing preserved through complete cue " + index);
                });
                totalVisemes += performance.VisemeChanges - before;
            }
            Step(() => {
                Require(maxAngle > 3 && maxAngle < 9, "visible but restrained head range");
                Require(maxBody > .1f && maxBody < 2, "subtle upper body motion");
                Require(maxShoulder > .0005f && maxShoulder < .005f, "independent shoulders move within five millimetres");
                demo.Play(0);
            });
            yield return new WaitForSecondsRealtime(1.1f);
            float position = module.Speech.time;
            Step(() => module.SetPaused(true));
            yield return new WaitForSecondsRealtime(.6f);
            Step(() => {
                Require(Mathf.Abs(module.Speech.time - position) < .05f, "paused gesture clock retains actual voice position");
                Require(module.Portrait.Performance.CurrentViseme == "X", "pause closes mouth");
                module.SetPaused(false);
            });
            yield return new WaitForSecondsRealtime(.4f);
            Step(() => { Require(module.Speech.time > position + .15f, "resume continues voice and acting timeline"); demo.Play(0); });
            yield return null;
            Step(() => {
                Require(module.Speech.time < .2f && module.Current.Acting == demo.Script.Cues[0].Acting, "replay restarts authored performance");
                module.Portrait.BodyMotion = false; module.Portrait.HeadMotion = false; module.Portrait.MovementStrength = 0;
            });
            yield return new WaitForSecondsRealtime(2);
            Step(() => {
                Require(module.Portrait.Performance.HeadAngles.magnitude < .04f && module.Portrait.Performance.TorsoAngles.magnitude < .02f
                    && Mathf.Abs(module.Portrait.Performance.ShoulderLift) < .00001f, "motion controls settle entire rig");
                module.Portrait.HeadMotion = module.Portrait.BodyMotion = true; module.Portrait.MovementStrength = 1;
                module.Play(new MiraDialogueLine { Heading = "Unabhängiger Dialog", Text = "Ohne Erwachen-Regie.", Mood = MiraAvatar.Mood.Focused });
            });
            yield return new WaitForSecondsRealtime(2);
            Step(() => {
                Require(module.Portrait.Performance.Acting == null && module.Portrait.Performance.TorsoAngles.magnitude < .02f,
                    "unassigned dialogue does not inherit awakening acting");
                module.Close(); Require(module.Portrait.Acting == null, "closing clears performance");
            });
            if (failed) yield break;
            File.WriteAllText(Folder + "/result.json", "{\"passed\":true,\"checks\":" + checks + ",\"pictures\":" + pictures
                + ",\"maxHeadDegrees\":" + maxAngle.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)
                + ",\"maxTorsoDegrees\":" + maxBody.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)
                + ",\"maxShoulderMetres\":" + maxShoulder.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)
                + ",\"visemeChanges\":" + totalVisemes + "}");
            Debug.Log("MIRA ACTING CHECK PASSED: " + checks); Application.Quit(0);
        }
    }
}
#endif
