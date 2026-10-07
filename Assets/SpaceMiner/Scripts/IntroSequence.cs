using System;
using UnityEngine;

namespace SpaceMiner
{
    // Run before camera and simulation input, including the frame that consumes Escape.
    [DefaultExecutionOrder(-1000)]
    [RequireComponent(typeof(Camera), typeof(OrbitCamera))]
    public sealed class IntroSequence : MonoBehaviour
    {
        [Serializable] public sealed class Cue
        {
            public int Stage;
            public string Heading;
            public string Text;
            public string Audio;
        }

        [Serializable] private sealed class Script
        {
            public string Voice;
            public Cue[] Cues;
        }

        public static bool IsPlaying { get; private set; }
        private static int completedFrame = -1;
        public static bool BlocksGameplay => IsPlaying || completedFrame == Time.frameCount;
        public int CueIndex { get; private set; }
        public int CueCount => script?.Cues?.Length ?? 0;
        public float CueDuration => durations[CueIndex];
        public bool HasCompleteVoiceTrack { get; private set; }
        public event Action Finished;

        private Script script;
        private AudioClip[] clips;
        private float[] durations;
        private AudioSource voice;
        private OrbitCamera orbit;
        private Camera view;
        private float elapsed, stageElapsed;
        private GUIStyle heading, caption, speaker, hint;
        private Font introFont;
        private Font hintFont;
        private bool initialized; private bool settingsPausedVoice;

        private void Awake()
        {
            IsPlaying = true;
            completedFrame = -1;
            orbit = GetComponent<OrbitCamera>();
            view = GetComponent<Camera>();
            voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0;
            voice.volume = 0.85f;
            PlayerAudioSource.Attach(voice, PlayerAudioChannel.Voices);
        }

        private void Start() { if (StartMenu.IsOpen) IsPlaying = false; else PlayIntro(); }

        public void PlayIntro(bool replay = false)
        {
            if (!initialized)
            {
                TextAsset data = Resources.Load<TextAsset>("Intro/intro");
                if (data == null) { Debug.LogWarning("Mira intro script is missing."); Skip(); return; }
                script = JsonUtility.FromJson<Script>(data.text);
                if (script?.Cues == null || script.Cues.Length == 0) { Skip(); return; }
                clips = new AudioClip[CueCount];
                durations = new float[CueCount];
                HasCompleteVoiceTrack = true;
                for (int i = 0; i < CueCount; i++)
                {
                    Cue cue = script.Cues[i];
                    clips[i] = Resources.Load<AudioClip>(cue.Audio);
                    HasCompleteVoiceTrack &= clips[i] != null;
                    float readingTime = cue.Text.Split(' ').Length / 2.1f;
                    durations[i] = Mathf.Max(3.5f, clips[i] != null ? clips[i].length + 1.2f : readingTime + 1.5f);
                }
                initialized = true;
            }
            IsPlaying = true;
            completedFrame = -1;
            elapsed = stageElapsed = 0;
            CueIndex = 0;
            BeginCue();
            SetCamera();
            if (!replay) CinematicLibrary.MarkSeen(CinematicLibrary.MiraAwakening);
        }

        private void Update()
        {
            if (!IsPlaying || !initialized) return;
            if (SettingsMenu.BlocksInput) { if (!settingsPausedVoice) { voice.Pause(); settingsPausedVoice = true; } return; }
            if (settingsPausedVoice) { voice.UnPause(); settingsPausedVoice = false; }
            if (Input.GetKeyDown(KeyCode.Escape)) { Skip(); return; }
            AdvancePlayback(Time.unscaledDeltaTime);
            if (IsPlaying) SetCamera();
        }

        // Also used by the opt-in player integration check to exercise timed completion.
        public void AdvancePlayback(float seconds)
        {
            if (!IsPlaying || !initialized || seconds <= 0) return;
            elapsed += seconds;
            stageElapsed += seconds;
            while (IsPlaying && elapsed >= CueDuration)
            {
                elapsed -= CueDuration;
                int previousStage = script.Cues[CueIndex].Stage;
                CueIndex++;
                if (CueIndex >= CueCount) { Skip(); return; }
                if (previousStage != script.Cues[CueIndex].Stage) stageElapsed = elapsed;
                BeginCue();
            }
        }

        private void BeginCue()
        {
            voice.Stop();
            voice.clip = clips[CueIndex];
            if (voice.clip != null) voice.Play();
        }

        public void Skip()
        {
            if (!IsPlaying) return;
            IsPlaying = false;
            completedFrame = Time.frameCount;
            if (voice != null) { voice.Stop(); voice.clip = null; }
            if (orbit != null) orbit.ResetView();
            Finished?.Invoke();
        }

        private void SetCamera()
        {
            int stage = script.Cues[CueIndex].Stage;
            Vector3 position, target;
            float drift = Mathf.Clamp01(stageElapsed / 22f);
            if (stage == 2)
            {
                position = Vector3.Lerp(new Vector3(0, 400, -2500), new Vector3(900, 800, -4000), drift);
                target = new Vector3(1000, 0, 2500);
            }
            else if (stage == 3)
            {
                position = Vector3.Lerp(new Vector3(-36, 12, -45), new Vector3(-55, 23, -90), drift);
                target = new Vector3(-8, 0, 0);
            }
            else
            {
                position = Vector3.Lerp(new Vector3(32, 14, -48), new Vector3(55, 24, -75), drift);
                target = Vector3.zero;
            }
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
            OrbitCamera.ConfigureDepth(view, Vector3.Distance(position, target));
        }

        private void OnGUI()
        {
            if (!IsPlaying || !initialized || SettingsMenu.IsOpen || TechTreeMenu.IsOpen) return;
            EnsureStyles();
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            GUI.depth = -100;
            float scale = Mathf.Clamp(Mathf.Min(Screen.height / 900f, Screen.width / 1440f), 0.35f, 2f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float width = Screen.width / scale, height = Screen.height / scale;
            Cue cue = script.Cues[CueIndex];
            float fade = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / 0.7f))
                * Mathf.SmoothStep(0, 1, Mathf.Clamp01((CueDuration - elapsed) / 0.7f));
            float darkness = cue.Stage == 0 ? 1 : Mathf.Lerp(1, 0.45f, Mathf.Clamp01(stageElapsed / 2.5f));
            Fill(new Rect(0, 0, width, height), new Color(0.003f, 0.007f, 0.015f, darkness));
            Fill(new Rect(0, 0, width, 92), new Color(0, 0, 0, 0.85f));
            Fill(new Rect(0, height - 108, width, 108), new Color(0, 0, 0, 0.85f));

            // A dedicated, prewarmed font atlas keeps captions and the Escape hint
            // intact when the gameplay HUD uses different font sizes.
            introFont.RequestCharactersInTexture(cue.Heading, 21, FontStyle.Normal);
            introFont.RequestCharactersInTexture(cue.Text, 31, FontStyle.Normal);
            introFont.RequestCharactersInTexture("MIRA", 14, FontStyle.Bold);
            hintFont.RequestCharactersInTexture("SPACE MINER  /  ERWACHEN ESC  ·  Intro überspringen  F11  Vollbild Fenster 0123456789/", 13, FontStyle.Normal);

            float textWidth = Mathf.Min(900, width - 100);
            float left = (width - textWidth) * 0.5f;
            GUI.color = new Color(1, 1, 1, fade);
            GUI.Label(new Rect(left, height * 0.26f, textWidth, 58), cue.Heading, heading);
            Fill(new Rect(width * 0.5f - 35, height * 0.36f, 70, 2), new Color(0.35f, 0.85f, 0.95f, fade));
            GUI.Label(new Rect(left, height * 0.40f, textWidth, 28), "MIRA", speaker);
            if (SettingsStore.Current.Accessibility.Subtitles) GUI.Label(new Rect(left, height * 0.47f, textWidth, height * 0.28f), cue.Text, caption);
            GUI.color = Color.white;
            GUI.Label(new Rect(35, 28, width - 70, 30), "SPACE MINER  /  ERWACHEN", hint);
            GUI.Label(new Rect(left, height - 71, textWidth, 26), "ESC  ·  Intro überspringen     F11  ·  Vollbild / Fenster", hint);
            GUI.Label(new Rect(left, height - 39, textWidth, 22), (CueIndex + 1) + " / " + CueCount, hint);
            GUI.depth = previousDepth;
            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }

        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (heading != null) return;
            introFont = Font.CreateDynamicFontFromOSFont("Segoe UI", 31);
            hintFont = Font.CreateDynamicFontFromOSFont("Consolas", 13);
            heading = new GUIStyle(GUI.skin.label) { font = introFont, fontSize = 21, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            caption = new GUIStyle(heading) { fontSize = 31, alignment = TextAnchor.UpperCenter };
            speaker = new GUIStyle(heading) { fontSize = 14, fontStyle = FontStyle.Bold };
            hint = new GUIStyle(heading) { font = hintFont, fontSize = 13 };
            heading.normal.textColor = new Color(0.6f, 0.8f, 0.87f);
            caption.normal.textColor = new Color(0.9f, 0.94f, 0.97f);
            speaker.normal.textColor = new Color(0.35f, 0.85f, 0.95f);
            hint.normal.textColor = new Color(0.55f, 0.64f, 0.7f);
        }

        private void OnDisable()
        {
            if (IsPlaying) Skip();
        }

        private void OnDestroy()
        {
            if (introFont != null) Destroy(introFont);
            if (hintFont != null) Destroy(hintFont);
        }
    }
}
