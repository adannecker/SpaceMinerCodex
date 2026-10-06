using UnityEngine;
namespace SpaceMiner
{
    // Short original synthesized console sounds, no downloaded assets or runtime service.
    public sealed class SettingsUiAudio : MonoBehaviour
    {
        private static SettingsUiAudio instance;
        private AudioSource source;
        private AudioClip hover, activate;
        private float nextHover;
        private string lastHover;
        private int hoverFrame;
        private void Awake()
        {
            instance = this; source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false; source.spatialBlend = 0;
            hover = Tone("Console hover", .035f, 1700, 1100);
            activate = Tone("Console activate", .065f, 950, 1900);
        }
        public static void Observe(Rect rect, string id)
        {
            if (instance == null || Event.current.type != EventType.Repaint || !rect.Contains(Event.current.mousePosition)) return;
            instance.hoverFrame = Time.frameCount;
            if (instance.lastHover == id) return;
            instance.lastHover = id;
            if (Time.unscaledTime >= instance.nextHover) { instance.Play(false); instance.nextHover = Time.unscaledTime + .09f; }
        }
        public static void Activate() { if (instance != null) instance.Play(true); }
        private void Play(bool click) { source.PlayOneShot(click ? activate : hover, (click ? .22f : .09f) * PlayerAudio.Gain(PlayerAudioChannel.Ui)); }
        private void Update() { if (Time.frameCount - hoverFrame > 1) lastHover = null; }
        private static AudioClip Tone(string name, float duration, float start, float end)
        {
            const int rate = 48000; var samples = new float[Mathf.RoundToInt(duration * rate)]; float phase = 0;
            for (int i = 0; i < samples.Length; i++) {
                float t = (float)i / samples.Length;
                phase += 2 * Mathf.PI * Mathf.Lerp(start, end, t) / rate;
                samples[i] = Mathf.Sin(phase) * Mathf.Sin(Mathf.PI * t) * Mathf.Exp(-5 * t);
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        private void OnDestroy() { if(instance == this) instance = null; Destroy(hover); Destroy(activate); }
    }
}
