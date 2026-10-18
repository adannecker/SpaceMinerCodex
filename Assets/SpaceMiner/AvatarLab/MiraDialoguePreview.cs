using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    public sealed class MiraDialoguePreview : MonoBehaviour
    {
        [Serializable] private sealed class Dialogue { public IntroSequence.Cue[] Cues; }
        public MiraAvatar Avatar;
        public AudioSource Speech;
        public MiraAvatar3D Avatar3D;
        public string ModelLabel;
        public bool TripoPrototype;
        public int CueCount => cues != null ? cues.Length : 0;
        public int CurrentCue => current;
        public bool IsPaused => paused;
        public bool TextOnly => clips != null && clips[current] == null;
        public float ReadingDuration => cues != null ? Mathf.Clamp(cues[current].Text.Length / 14f, 3f, 15f) : 3f;
        private IntroSequence.Cue[] cues;
        private AudioClip[] clips;
        private int current;
        private bool playing, paused;
        private float silence;
        private GUIStyle title, text, small, button;
        private int animationSamples;
        private bool testRun;
        private MiraFacePerformance performance;
        private readonly SpaceMinerUi visor = new SpaceMinerUi();
        private readonly AccessibilitySettings previewStyle = new AccessibilitySettings();

        private IEnumerator Start()
        {
            Application.runInBackground = true;
            performance=GetComponent<MiraFacePerformance>();
            var arguments = Environment.GetCommandLineArgs();
            testRun = Array.IndexOf(arguments, "-miraTripoCheck") >= 0 || Array.IndexOf(arguments, "-miraAvatarCheck") >= 0;
            Screen.SetResolution(TripoPrototype ? 1600 : 1200, TripoPrototype ? 1000 : 760, FullScreenMode.Windowed);
            cues = JsonUtility.FromJson<Dialogue>(Resources.Load<TextAsset>("Intro/intro").text).Cues;
            clips = new AudioClip[cues.Length];
            for (int i = 0; i < clips.Length; i++) if (!string.IsNullOrEmpty(cues[i].Audio)) clips[i] = Resources.Load<AudioClip>(cues[i].Audio);
            Speech.spatialBlend = 0; Speech.volume = .85f;
            Avatar.Speech = Speech;
            yield return null;
            Play(0);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-miraAvatarCheck") < 0) yield break;
            yield return new WaitForSecondsRealtime(1.4f);
            if (clips.Length != 16 || Avatar.Expressions == null)
                throw new Exception("Avatar preview assets missing");
            for (int i = 0; i < clips.Length; i++)
                if (!string.IsNullOrEmpty(cues[i].Audio) && clips[i] == null) throw new Exception("Assigned voice clip missing: " + cues[i].Audio);
            string path = Path.Combine(Environment.CurrentDirectory, "Logs/mira-avatar-preview.png");
            ScreenCapture.CaptureScreenshot(path);
            Pause(); yield return new WaitForSecondsRealtime(.3f);
            if (Speech.isPlaying || Avatar.Mouth > .03f) throw new Exception("Paused avatar must become quiet");
            Pause(); Play(1); yield return new WaitForSecondsRealtime(1.2f);
            if (animationSamples == 0) throw new Exception("Speech did not animate the mouth");
            if (Avatar.DialoguePortraits != null)
            {
                Play(4);
                if (Avatar.CurrentMood != MiraAvatar.Mood.Concerned) throw new Exception("Concerned portrait missing");
                Play(12);
                if (Avatar.CurrentMood != MiraAvatar.Mood.Focused) throw new Exception("Focused portrait missing");
                Play(15);
                if (Avatar.CurrentMood != MiraAvatar.Mood.Encouraging) throw new Exception("Encouraging portrait missing");
                if (FindFirstObjectByType<WaterScenario>() != null || FindFirstObjectByType<RuinedWorld>() != null)
                    throw new Exception("Portrait preview is not isolated");
            }
            if (Avatar3D != null && (Avatar3D.AnimatedFrames == 0 || FindFirstObjectByType<WaterScenario>() != null || FindFirstObjectByType<RuinedWorld>() != null))
                throw new Exception("3D animation or isolated-scene check failed");
            Speech.Stop(); playing = false;
            Debug.Log("MIRA AVATAR CHECK PASSED: 16 dialogue cues, assigned Aoede clips, speech signal, pause, replay, screenshot; portrait moods checked when configured");
            Application.Quit(0);
        }

        private void Update()
        {
            if (Avatar.Mouth > .07f) animationSamples++;
            if (!testRun && Input.GetKeyDown(KeyCode.Space)) Pause();
            if (!testRun && Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
            if (!playing || paused || Speech.isPlaying) { silence = 0; return; }
            silence += Time.unscaledDeltaTime;
            if (silence < (TextOnly ? ReadingDuration : .55f)) return;
            if (current + 1 < clips.Length) Play(current + 1); else playing = false;
        }

        internal void Play(int index)
        {
            current = Mathf.Clamp(index, 0, clips.Length - 1);
            Avatar.SetMood(current <= 2 ? MiraAvatar.Mood.Friendly :
                current <= 6 ? MiraAvatar.Mood.Concerned :
                current >= 14 ? MiraAvatar.Mood.Encouraging : MiraAvatar.Mood.Focused);
            Speech.Stop(); Speech.clip = clips[current]; if (Speech.clip != null) Speech.Play();
            playing = true; paused = false; silence = 0;
        }
        internal void Pause()
        {
            if (!playing) return;
            paused = !paused;
            if (paused) Speech.Pause(); else Speech.UnPause();
        }

        private void OnGUI()
        {
            if (cues == null) return;
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
                title.normal.textColor = new Color(.48f,.88f,.94f);
                text = new GUIStyle(GUI.skin.label) { fontSize = 25, wordWrap = true };
                small = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
                small.normal.textColor = new Color(.64f,.73f,.79f);
                button = new GUIStyle(GUI.skin.button) { fontSize = 16 };
            }
            Matrix4x4 matrix = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1200f, Screen.height / 760f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            GUI.Label(new Rect(40,25,1100,40), "MIRA  /  DIALOG-VORSCHAU", title);
            GUI.Label(new Rect(40,70,1080,40), "Erwachen · Aoede · " + (!string.IsNullOrEmpty(ModelLabel) ? ModelLabel : Avatar.DialoguePortraits != null ? "gezeichnete Mira · vier Ausdrucksvarianten" : "interne Avatar-Testszene"), small);
            if (TripoPrototype) { visor.Configure(previewStyle); visor.Panel(new Rect(35,130,455,490)); button = visor.Button; }
            else GUI.Box(new Rect(35,130,455,490), GUIContent.none);
            if (Avatar3D != null) GUI.DrawTexture(new Rect(48,145,430,430), Avatar3D.Portrait, ScaleMode.ScaleToFit);
            else Avatar.Draw(new Rect(48,145,430,430));
            GUI.Label(new Rect(65,585,400,25), paused ? "PAUSIERT" : Speech.isPlaying ? "MIRA SPRICHT" : playing && TextOnly ? "TEXT · AUFNAHME AUSSTEHEND" : "MIRA HÖRT ZU", small);
            if(performance!=null)
            {
                bool head=GUI.Toggle(new Rect(55,641,140,30),Avatar.Motion,"Kopfbewegung");
                bool eyes=GUI.Toggle(new Rect(215,641,115,30),performance.EyeMotion,"Augen");
                bool expression=GUI.Toggle(new Rect(350,641,115,30),performance.Expressions,"Mimik");
                if(!testRun){Avatar.Motion=head;performance.EyeMotion=eyes;performance.Expressions=expression;}
            }
            GUI.Label(new Rect(540,170,600,50), cues[current].Heading, title);
            GUI.Label(new Rect(540,235,600,180), cues[current].Text, text);
            GUI.Label(new Rect(540,425,600,35), "Dialog " + (current + 1) + " / " + cues.Length, small);
            if (GUI.Button(new Rect(540,475,185,44), paused ? "Weiter" : "Pause", button) && !testRun) Pause();
            if (GUI.Button(new Rect(735,475,185,44), "Erneut hören", button) && !testRun) Play(current);
            if (GUI.Button(new Rect(930,475,210,44), "Von Anfang", button) && !testRun) Play(0);
            if (GUI.Button(new Rect(540,530,185,44), "Vorheriger", button) && !testRun) Play(current - 1);
            if (GUI.Button(new Rect(735,530,185,44), "Nächster", button) && !testRun) Play(current + 1);
            if (Avatar.DialoguePortraits == null && performance==null)
            { bool motion = GUI.Toggle(new Rect(940,537,220,30), Avatar.Motion, TripoPrototype ? "Kopfbewegung" : "Blinzeln / Bewegung"); if (!testRun) Avatar.Motion = motion; }
            GUI.Label(new Rect(540,602,180,28), "Stimmenlautstärke", small);
            float volume = GUI.HorizontalSlider(new Rect(735,615,405,20), Speech.volume, 0, 1); if (!testRun) Speech.volume = volume;
            if (Avatar.DialoguePortraits == null)
            {
                GUI.Label(new Rect(540,645,180,28), "Mundbewegung", small);
                float sensitivity = GUI.HorizontalSlider(new Rect(735,658,405,20), Avatar.Sensitivity, 5, 35); if (!testRun) Avatar.Sensitivity = sensitivity;
            }
            else GUI.Label(new Rect(540,645,600,35), "Freundlich · besorgt · konzentriert · ermutigend", small);
            if (TripoPrototype)
            {
                string mood=Avatar.CurrentMood==MiraAvatar.Mood.Concerned?"besorgt":Avatar.CurrentMood==MiraAvatar.Mood.Focused?"konzentriert":Avatar.CurrentMood==MiraAvatar.Mood.Encouraging?"ermutigend":"freundlich";
                GUI.Label(new Rect(540,681,600,24), performance!=null && performance.HasTiming ? "Sprachformen zur Aufnahme · " + mood : "Mund reagiert auf die Stimme · vorläufige Sprachbewegung", small);
            }
            GUI.Label(new Rect(40,708,1110,30), "Leertaste: Pause / Weiter     Escape: Schließen" + (Avatar3D != null ? "     Gesicht ziehen: Drehen · R: Zurücksetzen" : ""), small);
            GUI.matrix = matrix;
        }
        private void OnDestroy() { visor.Dispose(); }
    }
}
