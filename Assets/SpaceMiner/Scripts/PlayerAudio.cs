using System;
using UnityEngine;
namespace SpaceMiner
{
    public enum PlayerAudioChannel { Background, Effects, Voices, Ui }
    // Live audio preview is deliberately separate from saved settings and graphic/gameplay application.
    public static class PlayerAudio
    {
        private static AudioSettings preview, applied = new AudioSettings();
        private static AudioSettings Current => preview ?? applied;
        public static float Gain(PlayerAudioChannel channel)
        {
            var a = Current;
            return channel switch {
                PlayerAudioChannel.Background => a.BackgroundEnabled ? a.Background : 0,
                PlayerAudioChannel.Effects => a.EffectsEnabled ? a.Effects : 0,
                PlayerAudioChannel.Voices => a.VoicesEnabled ? a.Voices : 0,
                _ => a.UiEnabled ? a.Ui : 0
            };
        }
        public static void Apply(AudioSettings data) { applied = data; RefreshMaster(); }
        public static void Preview(AudioSettings data) { preview = data; RefreshMaster(); }
        public static void EndPreview() { preview = null; RefreshMaster(); }
        private static void RefreshMaster() { var a = Current; AudioListener.volume = a.MasterEnabled ? a.Master : 0; }
    }

    // Reusable channel adapter: mute preserves playback and each owner's fades/source level.
    public sealed class PlayerAudioSource : MonoBehaviour
    {
        private AudioSource source;
        private PlayerAudioChannel channel;
        private float baseLevel;
        public static void Attach(AudioSource source, PlayerAudioChannel channel)
        {
            var binding = source.gameObject.AddComponent<PlayerAudioSource>();
            binding.source = source; binding.channel = channel;
            binding.baseLevel = source.volume;
        }
        private void LateUpdate()
        {
            if (source == null) return;
            source.volume = baseLevel * PlayerAudio.Gain(channel);
        }
    }
}
