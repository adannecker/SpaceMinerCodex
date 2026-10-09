using System;
using UnityEngine;

namespace SpaceMiner
{
    public enum StationSound { ConsoleOpen, ConsoleClose, VrOn, VrOff, ScanComplete }

    // Original synthesized sounds, local and service-free. Physical sound stays inside the station.
    [DefaultExecutionOrder(300)]
    public sealed class StationInteractionAudio : MonoBehaviour
    {
        public static StationInteractionAudio Current { get; private set; }
        public AudioSource Ambient { get; private set; }
        public AudioSource Signals { get; private set; }
        public AudioSource Pressure { get; private set; }
        public AudioSource DoorSource(int index) => doors[index];
        private readonly AudioSource[] doors = new AudioSource[2];
        private AudioClip opening, closing, hiss, room, latch;
        private readonly AudioClip[] signals = new AudioClip[5];
        private readonly int[] motion = new int[2];
        private bool pressurizing;

        private AudioSource Source(string name, bool spatial)
        {
            var item = new GameObject(name); item.transform.SetParent(transform, false);
            var source = item.AddComponent<AudioSource>();
            source.playOnAwake = false; source.spatialBlend = spatial ? 1 : 0;
            source.rolloffMode = AudioRolloffMode.Linear; source.minDistance = 2; source.maxDistance = 22;
            source.dopplerLevel = 0; source.priority = 100; return source;
        }
        private void Awake()
        {
            Current = this;
            opening = Clip("Airlock opening servo", 1, 0, true);
            closing = Clip("Airlock closing servo", 1, 1, true);
            hiss = Clip("Airlock pressure circulation", 1, 2, true);
            room = Clip("Quiet habitat electrical hum", 2, 3, true);
            latch = Clip("Airlock seal latch", .16f, 4, false);
            for (int i = 0; i < signals.Length; i++) signals[i] = Clip(((StationSound)i).ToString(), i == 4 ? .55f : .28f, i + 5, false);
            Ambient = Source("Habitat ambience", false); Ambient.clip = room; Ambient.loop = true;
            Signals = Source("Station interaction signals", false);
            Pressure = Source("Airlock pressure audio", true); Pressure.clip = hiss; Pressure.loop = true;
            for (int i = 0; i < 2; i++) { doors[i] = Source("Airlock door audio " + i, true); doors[i].loop = true; }
        }
        public static void Play(StationSound sound)
        {
            var audio = Current;
            if (audio == null || StartMenu.IsOpen || IntroSequence.IsPlaying) return;
            audio.Signals.volume = .18f * PlayerAudio.Gain(PlayerAudioChannel.Effects);
            audio.Signals.PlayOneShot(audio.signals[(int)sound]);
        }
        private void LateUpdate() => Synchronize();
        public void Synchronize()
        {
            float gain = PlayerAudio.Gain(PlayerAudioChannel.Effects);
            Signals.volume = .18f * gain;
            var interior = StationInteriorMode.Current;
            bool inside = StationInteriorMode.IsInside && !StartMenu.IsOpen && !MemoryCinematic.IsPlaying;
            Loop(Ambient, inside, .02f * gain);
            var airlock = interior != null ? interior.Airlock : null;
            bool running = inside && airlock != null && !SettingsMenu.BlocksInput && !IntroSequence.BlocksGameplay;
            if (airlock != null)
            {
                for (int i = 0; i < 2; i++)
                    doors[i].transform.position = interior.Room.TransformPoint(new Vector3(0, 1.2f, i == 0 ? StationAirlock.HabitatDoorZ : StationAirlock.RingDoorZ));
                Pressure.transform.position = interior.Room.TransformPoint(new Vector3(0, 1.2f, -7.8f));
            }
            int moving = -1, direction = 0;
            if (running)
            {
                bool entry = airlock.Phase == AirlockPhase.OpeningEntry || airlock.Phase == AirlockPhase.ClosingEntry;
                bool exit = airlock.Phase == AirlockPhase.OpeningExit || airlock.Phase == AirlockPhase.ClosingExit;
                if (entry || exit)
                {
                    int entryIndex = airlock.TowardsRing ? 0 : 1;
                    moving = entry ? entryIndex : 1 - entryIndex;
                    direction = airlock.Phase == AirlockPhase.OpeningEntry || airlock.Phase == AirlockPhase.OpeningExit ? 1 : -1;
                }
            }
            for (int i = 0; i < 2; i++)
            {
                int next = i == moving ? direction : 0;
                if (motion[i] != next && next != 0) { doors[i].Stop(); doors[i].clip = next == 1 ? opening : closing; }
                if (running && motion[i] != 0 && next == 0) Signals.PlayOneShot(latch, .6f);
                motion[i] = next; Loop(doors[i], next != 0, .24f * gain);
            }
            bool pressureNow = running && airlock.Phase == AirlockPhase.Equalizing;
            if (pressurizing && running && !pressureNow) Signals.PlayOneShot(signals[1], .45f);
            pressurizing = pressureNow; Loop(Pressure, pressureNow, .14f * gain);
        }
        private static void Loop(AudioSource source, bool active, float level)
        {
            source.volume = level;
            if (active) { if (!source.isPlaying) source.Play(); }
            else if (source.isPlaying) source.Stop();
        }
        private static AudioClip Clip(string name, float seconds, int kind, bool loop)
        {
            const int rate = 24000;
            var samples = new float[Mathf.RoundToInt(seconds * rate)];
            var noise = new System.Random(3901 + kind); float low = 0, phase = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / rate, p = (float)i / samples.Length;
                low += .16f * ((float)noise.NextDouble() * 2 - 1 - low);
                float value;
                if (kind < 2)
                    value = .28f * Mathf.Sin(2 * Mathf.PI * (kind == 0 ? 144 : 112) * t) + .15f * Mathf.Sin(2 * Mathf.PI * 336 * t) + low * .5f;
                else if (kind == 2) value = low * .9f;
                else if (kind == 3) value = .07f * Mathf.Sin(2 * Mathf.PI * 60 * t) + .025f * Mathf.Sin(2 * Mathf.PI * 120 * t);
                else if (kind == 4) value = (low * .6f + .4f * Mathf.Sin(2 * Mathf.PI * 180 * t)) * Mathf.Exp(-18 * p);
                else
                {
                    float frequency = kind switch { 5 => Mathf.Lerp(440, 880, p), 6 => Mathf.Lerp(620, 310, p),
                        7 => Mathf.Lerp(300, 1400, p), 8 => Mathf.Lerp(1000, 240, p), _ => p < .45f ? 660 : 990 };
                    phase += 2 * Mathf.PI * frequency / rate;
                    value = .55f * Mathf.Sin(phase) + .08f * Mathf.Sin(phase * 2);
                }
                float envelope = loop ? 1 : Mathf.Min(1, t / .015f) * Mathf.Min(1, (seconds - t) / .045f);
                samples[i] = value * envelope;
            }
            // Smooth the noise seam while leaving tonal frequencies periodic.
            if (loop && kind != 3) for (int i = 0; i < 512; i++)
            {
                int index = samples.Length - 512 + i;
                samples[index] = Mathf.Lerp(samples[index], samples[0], Mathf.SmoothStep(0, 1, (i + 1) / 512f));
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        private void OnDestroy()
        {
            if (Current == this) Current = null;
            foreach (var clip in signals) if (clip != null) Destroy(clip);
            foreach (var clip in new[] { opening, closing, hiss, room, latch }) if (clip != null) Destroy(clip);
            foreach (var source in new[] { Ambient, Signals, Pressure, doors[0], doors[1] }) if (source != null) Destroy(source.gameObject);
        }
    }
}
