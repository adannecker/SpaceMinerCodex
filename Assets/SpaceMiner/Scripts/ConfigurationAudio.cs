using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceMiner
{
    // Scene-local tracks for splash, configuration and gameplay, using existing audio settings.
    [DefaultExecutionOrder(-1900)]
    public sealed class ConfigurationAudio : MonoBehaviour
    {
        private const float Level = .35f;
        private const float FadeSeconds = 1.2f;
        private AudioSource source;
        private AudioSource ambience;
        private AudioSource splash;
        private bool splashStarted;
        private float splashEnvelope;
        private bool ambienceStarted;
        private float menuEnvelope, ambienceEnvelope;
        internal AudioSource PlaybackSource => source;
        internal AudioSource AmbienceSource => ambience;
        internal AudioSource SplashSource => splash;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var camera = Object.FindFirstObjectByType<OrbitCamera>();
            if (camera != null && Object.FindFirstObjectByType<ConfigurationAudio>() == null)
                camera.gameObject.AddComponent<ConfigurationAudio>();
        }

        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0;
            source.volume = 0;
            source.clip = Resources.Load<AudioClip>("Audio/Deep-Space-Configuration-Loop");
            if (source.clip == null)
                source.clip = Resources.Load<AudioClip>("Audio/Deep Space Configuration");
            if (source.clip == null) Debug.LogWarning("Configuration audio: WAV asset missing.");
            ambience = gameObject.AddComponent<AudioSource>();
            ambience.playOnAwake = false;
            ambience.loop = true;
            ambience.spatialBlend = 0;
            ambience.volume = 0;
            ambience.clip = Resources.Load<AudioClip>("Audio/Asteroid-Solitude-Loop");
            if (ambience.clip == null) Debug.LogWarning("Space ambience: WAV asset missing.");
            splash = gameObject.AddComponent<AudioSource>();
            splash.playOnAwake = false; splash.loop = true; splash.spatialBlend = 0; splash.volume = 0;
            splash.clip = Resources.Load<AudioClip>("Audio/Orbal-Observation-Loop");
            if (splash.clip == null) Debug.LogWarning("Splash music: WAV asset missing.");
        }

        private void Update()
        {
            bool configuration = SettingsMenu.IsOpen || TechTreeMenu.IsOpen;
            bool start = StartMenu.IsOpen && !configuration && !MemoryCinematic.IsPlaying;
            bool open = StartMenu.IsOpen || configuration;
            if (splash.clip != null)
            {
                if (start && !splash.isPlaying)
                {
                    if (splashStarted) splash.UnPause();
                    else { splash.Play(); splashStarted = true; }
                }
                splashEnvelope = Mathf.MoveTowards(splashEnvelope, start ? Level : 0,
                    Level * Time.unscaledDeltaTime / FadeSeconds);
                splash.volume = splashEnvelope * PlayerAudio.Gain(PlayerAudioChannel.Background);
                if (!start && splashEnvelope == 0)
                {
                    if (StartMenu.IsOpen) { if (splash.isPlaying) splash.Pause(); }
                    else { splash.Stop(); splashStarted = false; }
                }
            }
            if (source.clip != null)
            {
                if (configuration && !source.isPlaying) source.Play();
                menuEnvelope = Mathf.MoveTowards(menuEnvelope, configuration ? Level : 0,
                    Level * Time.unscaledDeltaTime / FadeSeconds);
                source.volume = menuEnvelope * PlayerAudio.Gain(PlayerAudioChannel.Background);
                if (!configuration && menuEnvelope == 0 && source.isPlaying) source.Stop();
            }
            if (ambience.clip == null) return;
            if (!open && !ambience.isPlaying)
            {
                if (ambienceStarted) ambience.UnPause();
                else { ambience.Play(); ambienceStarted = true; }
            }
            float target = open ? 0 : IntroSequence.IsPlaying ? .10f : .25f;
            ambienceEnvelope = Mathf.MoveTowards(ambienceEnvelope, target,
                .25f * Time.unscaledDeltaTime / FadeSeconds);
            ambience.volume = ambienceEnvelope * PlayerAudio.Gain(PlayerAudioChannel.Background);
            if (open && ambienceEnvelope == 0 && ambience.isPlaying) ambience.Pause();
        }

        private void OnDisable()
        {
            if (source == null) return;
            source.Stop(); source.volume = 0;
            menuEnvelope = ambienceEnvelope = 0;
            splashEnvelope = 0;
            if (splash != null) { splash.Stop(); splash.volume = 0; splashStarted = false; }
            if (ambience != null) { ambience.Stop(); ambience.volume = 0; ambienceStarted = false; }
        }

        private void OnDestroy()
        {
            if (source != null) Destroy(source);
            if (ambience != null) Destroy(ambience);
            if (splash != null) Destroy(splash);
        }
    }
}
