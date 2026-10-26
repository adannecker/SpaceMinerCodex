#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    public sealed class MiraDialogueModelsCheck : MonoBehaviour
    {
        private const string Folder = "Logs/MiraModels";
        private int checks;
        private bool failed;
        private MiraDialogueModule module;
        private string host;
        private readonly List<string> pictures = new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-miraModelsCheck") >= 0)
                new GameObject("Mira models acceptance").AddComponent<MiraDialogueModelsCheck>();
        }
        private void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("MIRA MODELS: " + message);
            checks++; Debug.Log("MIRA MODELS OK: " + message);
        }
        private void Step(Action action)
        {
            if (failed) return;
            try { action(); }
            catch (Exception e)
            {
                failed = true; File.WriteAllText(Folder + "/" + host + "-error.txt", e.ToString());
                Debug.LogException(e); Application.Quit(1);
            }
        }
        private IEnumerator Capture(string label)
        {
            yield return new WaitForEndOfFrame();
            string file = Path.GetFullPath(Folder + "/" + host + "-" + label + ".png");
            ScreenCapture.CaptureScreenshot(file);
            yield return new WaitForSecondsRealtime(.2f);
            Step(() => Require(File.Exists(file) && new FileInfo(file).Length > 1000, "screen capture " + label));
            pictures.Add(file);
        }
        private void CapturePortrait(string label)
        {
            var target = module.Portrait.Texture;
            var previous = RenderTexture.active;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = target; image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply();
                var pixels = image.GetPixels32(); int varied = 0;
                Color32 first = pixels[0];
                foreach (var pixel in pixels) if (Mathf.Abs(pixel.r-first.r)+Mathf.Abs(pixel.g-first.g)+Mathf.Abs(pixel.b-first.b)>35) varied++;
                Require(varied > pixels.Length / 10, "actual nonblank 3D portrait " + label);
                string file = Path.GetFullPath(Folder + "/" + host + "-" + label + "-portrait.png");
                File.WriteAllBytes(file, image.EncodeToPNG()); pictures.Add(file);
            }
            finally { RenderTexture.active = previous; Destroy(image); }
        }
        private static float Weight(MiraAvatar3D avatar, string shape)
            => avatar.Face.GetBlendShapeWeight(avatar.Face.sharedMesh.GetBlendShapeIndex(shape));

        private IEnumerator Start()
        {
            Directory.CreateDirectory(Folder);
            yield return new WaitForSecondsRealtime(1);
            var demo = FindFirstObjectByType<MiraDialogueDemo>();
            host = demo != null ? "demo" : "game";
            var script = MiraDialogueScript.LoadAwakening();
            Step(() => {
                if (demo != null) module = demo.Dialogue;
                else
                {
                    CinematicLibrary.PreferencePrefix = "SpaceMiner.Test.MiraModels.";
                    FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride = Path.GetFullPath(Folder + "/isolated-saves");
                    FindFirstObjectByType<StartMenu>().StartDemo();
                    FindFirstObjectByType<IntroSequence>().Skip();
                    FindFirstObjectByType<WaterScenario>().Scanner.Say(script.Cues[1]);
                    module = FindFirstObjectByType<WaterScenario>().Scanner.DialogueModule;
                }
                Require(module != null && module.Portrait.Mode == (demo != null ? MiraVisualMode.HighModel : MiraVisualMode.Image), "host-specific default model");
                module.AutomaticClock = false;
            });
            if (failed) yield break;
            int lowVertices = 0;
            foreach (var mode in new[] { MiraVisualMode.LowModel, MiraVisualMode.HighModel })
            {
                string label = mode == MiraVisualMode.LowModel ? "low" : "high";
                Step(() => {
                    if (demo != null) demo.Play(1); else module.Play(script.Cues[1]);
                });
                yield return new WaitForSecondsRealtime(.65f);
                Step(() => {
                    module.SetPaused(true);
                    var speech = module.Speech; var clip = speech.clip; float audioPosition = speech.time;
                    module.Portrait.SetMode(mode);
                    Require(audioPosition > .1f && module.Speech == speech && speech.clip == clip && module.Paused && Mathf.Abs(speech.time-audioPosition)<.01f,
                        "model switch preserves shared voice, pause and position " + label);
                    var avatar = module.Portrait.ActiveAvatar;
                    Require(avatar != null && avatar.Driver.Speech == speech, "rig bound to actual dialogue AudioSource " + label);
                    Require(avatar.Face.sharedMesh.blendShapeCount == 13 && avatar.Face.bones.Length == (mode == MiraVisualMode.HighModel ? 5 : 2), "complete model-specific face/head/body rig " + label);
                    Require(Mathf.Abs(avatar.Bust.InverseTransformPoint(avatar.Face.bones[1].position).y - (mode == MiraVisualMode.HighModel ? .19f : .245f)) < .005f,
                        "model-specific neck pivot " + label);
                    Require(module.Portrait.Texture.width == (mode == MiraVisualMode.HighModel ? 1024 : 512), "model-specific render resolution " + label);
                    if (mode == MiraVisualMode.LowModel) lowVertices = avatar.Face.sharedMesh.vertexCount;
                    else Require(avatar.Face.sharedMesh.vertexCount != lowVertices, "H3.1 uses its own geometry, not H2.5 relabelled");
                    module.SetPaused(false);
                });
                if (failed) yield break;
                float maxJaw = 0, minJaw = 100; int previousVisemes = module.Portrait.Performance.VisemeChanges;
                float until = Time.realtimeSinceStartup + 2.3f;
                while (Time.realtimeSinceStartup < until)
                {
                    yield return null;
                    float jaw = Weight(module.Portrait.ActiveAvatar, "JawOpen"); maxJaw = Mathf.Max(maxJaw, jaw); minJaw = Mathf.Min(minJaw, jaw);
                }
                Step(() => {
                    Require(module.Portrait.Performance.HasTiming && module.Portrait.Performance.VisemeChanges > previousVisemes + 2,
                        "offline speech positions animate against AudioSource.time " + label);
                    Require(maxJaw > minJaw + 5, "actual jaw shape changes during speech " + label);
                    Require(module.Portrait.Performance.HeadAngles.sqrMagnitude > .0001f && module.Portrait.Performance.GazeChanges > 0,
                        "head and gaze animation runs on the calibrated rig " + label);
                    Require(!module.Portrait.ActiveAvatar.Face.enabled, "model surface hidden from world cameras " + label);
                    CapturePortrait(label + "-speaking");
                });
                yield return Capture(label + "-speaking");
                Step(() => module.SetPaused(true));
                yield return new WaitForSecondsRealtime(.5f);
                Step(() => {
                    Require(Weight(module.Portrait.ActiveAvatar,"JawOpen") < .5f && Weight(module.Portrait.ActiveAvatar,"LipClose") > 50,
                        "pause closes and relaxes lips " + label);
                    CapturePortrait(label + "-paused");
                });
                foreach (var mood in new[] { MiraAvatar.Mood.Friendly, MiraAvatar.Mood.Concerned, MiraAvatar.Mood.Focused, MiraAvatar.Mood.Encouraging })
                {
                    Step(() => module.Play(new MiraDialogueLine { Heading = "MIMIK / " + mood, Text = "Ausdrucksvergleich ohne Sprachaufnahme.", Mood = mood }));
                    yield return new WaitForSecondsRealtime(.7f);
                    Step(() => {
                        string shape = mood == MiraAvatar.Mood.Concerned ? "BrowConcern" : mood == MiraAvatar.Mood.Focused ? "BrowFocus" : "Smile";
                        Require(Weight(module.Portrait.ActiveAvatar,shape) > 5, "mood drives calibrated " + shape + " " + label);
                        Require(module.Portrait.Performance.CurrentViseme == "X" && Weight(module.Portrait.ActiveAvatar,"JawOpen") < .5f,
                            "text-only dialogue does not invent lip speech " + label);
                        CapturePortrait(label + "-" + mood);
                    });
                }
                Step(() => {
                    var performance = module.Portrait.Performance;
                    performance.DiagnosticPose = true;
                    var avatar = module.Portrait.ActiveAvatar;
                    for (int i = 0; i < 13; i++) avatar.Face.SetBlendShapeWeight(i,0);
                    avatar.Face.SetBlendShapeWeight(avatar.Face.sharedMesh.GetBlendShapeIndex("BlinkLeft"),100);
                    avatar.Face.SetBlendShapeWeight(avatar.Face.sharedMesh.GetBlendShapeIndex("BlinkRight"),100);
                });
                yield return null;
                Step(() => CapturePortrait(label + "-closed-eyes"));
                Step(() => {
                    var avatar = module.Portrait.ActiveAvatar;
                    for (int i = 0; i < 13; i++) avatar.Face.SetBlendShapeWeight(i,0);
                    avatar.Face.SetBlendShapeWeight(avatar.Face.sharedMesh.GetBlendShapeIndex("JawOpen"),100);
                    avatar.Face.SetBlendShapeWeight(avatar.Face.sharedMesh.GetBlendShapeIndex("Round"),70);
                });
                yield return null;
                Step(() => CapturePortrait(label + "-mouth-pose"));
                Step(() => {
                    module.Portrait.Performance.DiagnosticPose = false;
                    module.Portrait.HeadMotion = module.Portrait.EyeMotion = module.Portrait.Expressions = false;
                });
                yield return new WaitForSecondsRealtime(.6f);
                Step(() => {
                    Require(Weight(module.Portrait.ActiveAvatar,"Smile") < 10 && module.Portrait.Performance.Blink == 0,
                        "expression/eye controls return to rest " + label);
                    bool fog = RenderSettings.fog; var ambient = RenderSettings.ambientMode;
                    module.Portrait.Render();
                    Require(RenderSettings.fog == fog && RenderSettings.ambientMode == ambient, "portrait render restores scene lighting state " + label);
                    Vector3 position = module.transform.localPosition; Quaternion rotation = module.transform.localRotation;
                    try
                    {
                        module.transform.localPosition += new Vector3(10,20,30);
                        module.transform.localRotation = Quaternion.Euler(20,35,10);
                        module.Portrait.Render();
                        CapturePortrait(label + "-moving-host");
                    }
                    finally { module.transform.localPosition = position; module.transform.localRotation = rotation; }
                    module.Portrait.HeadMotion = module.Portrait.EyeMotion = module.Portrait.Expressions = true;
                });
            }
            Step(() => {
                module.Portrait.SetMode(MiraVisualMode.Image);
                Require(module.Portrait.ActiveAvatar == null && module.Portrait.Mode == MiraVisualMode.Image, "image mode releases active 3D rig");
                if (demo != null) demo.Play(0); else module.Play(script.Cues[0]);
            });
            yield return Capture("image-restored");
            if (failed) yield break;
            File.WriteAllText(Folder + "/" + host + "-result.json", "{\"passed\":true,\"checks\":" + checks
                + ",\"pictures\":" + pictures.Count + ",\"lowVertices\":" + lowVertices + "}");
            Debug.Log("MIRA MODELS " + host + " CHECK PASSED: " + checks);
            Application.Quit(0);
        }
    }
}
#endif
