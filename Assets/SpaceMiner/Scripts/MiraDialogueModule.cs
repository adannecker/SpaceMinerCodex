using System;
using UnityEngine;

namespace SpaceMiner
{
    [Serializable]
    public sealed class MiraDialogueLine
    {
        public string Id;
        public int Stage;
        public string Heading;
        public string Text;
        public string Audio;
        public MiraAvatar.Mood Mood = MiraAvatar.Mood.Focused;
        public MiraDialogueActing Acting;

        public float Duration(AudioClip clip)
        {
            float reading = (Text ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length / 2.1f;
            return Mathf.Max(3.5f, clip != null ? clip.length + 1.2f : reading + 1.5f);
        }
    }

    [Serializable]
    public sealed class MiraDialogueScript
    {
        public string Title;
        public string Voice;
        public MiraDialogueLine[] Cues;

        public static MiraDialogueScript LoadAwakening()
        {
            var asset = Resources.Load<TextAsset>("Intro/intro");
            if (asset == null) throw new InvalidOperationException("Mira dialogue script missing: Intro/intro");
            var script = JsonUtility.FromJson<MiraDialogueScript>(asset.text);
            if (script?.Cues == null || script.Cues.Length == 0)
                throw new InvalidOperationException("Mira dialogue script has no cues");
            for (int i = 0; i < script.Cues.Length; i++)
            {
                script.Cues[i].Mood = i <= 2 ? MiraAvatar.Mood.Friendly : i <= 6 ? MiraAvatar.Mood.Concerned
                    : i >= 14 ? MiraAvatar.Mood.Encouraging : MiraAvatar.Mood.Focused;
                script.Cues[i].Acting = MiraDialogueActing.Awakening(i);
            }
            return script;
        }
    }

    // The game and the isolated demo own their navigation; this component owns
    // one line's voice, reading time, pause state and the shared visor presenter.
    public sealed class MiraDialogueModule : MonoBehaviour
    {
        public MiraDialogueLine Current { get; private set; }
        public AudioSource Speech { get; private set; }
        public MiraPortraitView Portrait { get; private set; }
        public bool Visible => Current != null;
        public bool Paused { get; private set; }
        public bool Completed { get; private set; }
        public bool TextOnly => Visible && Speech.clip == null;
        public bool MissingRecording => TextOnly && !string.IsNullOrEmpty(Current.Audio);
        public float Elapsed { get; private set; }
        public float Duration { get; private set; }
        public float Volume { get; private set; } = .85f;
        public bool Subtitles = true;
        public bool AutomaticClock = true;
        public event Action LineCompleted;
        public event Action Closed;
        private readonly MiraVisorOverlay overlay = new MiraVisorOverlay();

        public static MiraDialogueModule Create(GameObject owner)
        {
            var child = new GameObject("Mira dialogue module");
            child.transform.SetParent(owner.transform, false);
            return child.AddComponent<MiraDialogueModule>();
        }

        private void Awake()
        {
            Speech = gameObject.AddComponent<AudioSource>();
            Speech.playOnAwake = false;
            Speech.spatialBlend = 0;
            Speech.ignoreListenerPause = true;
            Portrait = gameObject.AddComponent<MiraPortraitView>();
            Portrait.Speech = Speech;
            ApplyVolume();
        }

        public void Play(MiraDialogueLine line)
        {
            if (line == null) throw new ArgumentNullException(nameof(line));
            Speech.Stop();
            Current = line;
            Portrait.Mood = line.Mood;
            Portrait.Acting = line.Acting;
            Portrait.Visible = true;
            Speech.clip = string.IsNullOrEmpty(line.Audio) ? null : Resources.Load<AudioClip>(line.Audio);
            Duration = line.Duration(Speech.clip);
            Elapsed = 0;
            Paused = Completed = false;
            ApplyVolume();
            if (Speech.clip != null) Speech.Play();
        }

        public void SetPaused(bool paused)
        {
            ApplyVolume();
            if (!Visible || Paused == paused) return;
            Paused = paused;
            if (paused) Speech.Pause(); else Speech.UnPause();
        }

        public void SetVolume(float volume)
        {
            Volume = float.IsNaN(volume) || float.IsInfinity(volume) ? .85f : Mathf.Clamp01(volume);
            ApplyVolume();
        }

        public void Advance(float seconds)
        {
            if (!Visible || Paused || Completed || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            Elapsed = Mathf.Min(Duration, Elapsed + seconds);
            if (Elapsed < Duration) return;
            Completed = true;
            LineCompleted?.Invoke();
        }

        public void Close()
        {
            if (Speech != null) { Speech.Stop(); Speech.clip = null; }
            bool wasVisible = Visible;
            Current = null;
            if (Portrait != null) { Portrait.Visible = false; Portrait.Acting = null; }
            Paused = Completed = false;
            Elapsed = Duration = 0;
            if (wasVisible) Closed?.Invoke();
        }

        public bool Draw(Rect bounds, string hint = "STATIONSKANAL / MIRA", string action = null, float alpha = 1)
        {
            if (!Visible) return false;
            bool acknowledged = overlay.Draw(bounds, Current.Heading, Current.Text, Current.Mood,
                hint, action, Subtitles, alpha, Portrait.Mode == MiraVisualMode.Image ? null : Portrait.Texture);
            if (acknowledged) Close();
            return acknowledged;
        }

        private void ApplyVolume()
        {
            if (Speech != null) Speech.volume = Volume * PlayerAudio.Gain(PlayerAudioChannel.Voices);
        }
        private void Update()
        {
            ApplyVolume();
            if (AutomaticClock) Advance(Time.unscaledDeltaTime);
        }
        private void OnDisable() { Close(); }
        private void OnDestroy() { Close(); overlay.Dispose(); }
    }
}
