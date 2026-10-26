using System;
using UnityEngine;

namespace SpaceMiner
{
    [Serializable]
    public sealed class MiraDialogueGesture
    {
        public float Start, Duration;
        public Vector3 Head;
        public float Shoulder;
        public MiraDialogueGesture(float start, float duration, Vector3 head, float shoulder = 0)
        { Start = start; Duration = duration; Head = head; Shoulder = shoulder; }
        public float Weight(float seconds)
        {
            if (Duration <= 0 || seconds < Start || seconds >= Start + Duration) return 0;
            float s = Mathf.Sin(Mathf.PI * (seconds - Start) / Duration);
            return s * s;
        }
    }

    [Serializable]
    public sealed class MiraDialogueActing
    {
        public float MoodIntensity = .6f;
        public MiraDialogueGesture[] Gestures;

        // Authored for the existing awakening recordings, in AudioSource seconds.
        // Other dialogue lines retain their previous performance unless explicitly assigned.
        public static MiraDialogueActing Awakening(int line)
        {
            var profile = new MiraDialogueActing { MoodIntensity = line <= 2 ? .65f : line <= 6 ? .55f : line >= 14 ? .8f : .6f };
            switch (line)
            {
                case 0: profile.Gestures = new[] { G(.45f, 1.6f, -1, 2, 1.8f), G(2.65f, .9f, 3, 0, 0, -.3f) }; break;
                case 1: profile.Gestures = new[] { G(.5f, 1.9f, -1, -3.8f, -1.5f, -.3f) }; break;
                case 2: profile.Gestures = new[] { G(.6f, 2.5f, -1.4f, 2.5f, 2, -.5f) }; break;
                case 3: profile.Gestures = new[] { G(.6f, 2, 1.8f, -1, -.6f, .25f) }; break;
                case 4: profile.Gestures = new[] { G(.7f, 2.1f, 1, -3.2f, -.8f, .45f), G(3.5f, 1.5f, 1.8f, 1.5f, 0) }; break;
                case 5: profile.Gestures = new[] { G(.7f, 2, 1.5f, 2, .6f, .4f), G(4, 2, 2, -2.5f, -.7f) }; break;
                case 6: profile.Gestures = new[] { G(.6f, 2.1f, -1, 2.8f, .8f, -.25f), G(3.5f, 2, 1.5f, -2, -.7f, .3f) }; break;
                case 7: profile.Gestures = new[] { G(.7f, 2.1f, 0, 4, -.6f) }; break;
                case 8: profile.Gestures = new[] { G(.7f, 2.1f, 1.8f, -3, 0), G(4.6f, 1.8f, 1.8f, 1.8f, .5f) }; break;
                case 9: profile.Gestures = new[] { G(.8f, 2.2f, 1, 2.8f, .5f), G(4, 1.6f, 2.5f, 0, 0) }; break;
                case 10: profile.Gestures = new[] { G(.7f, 2.5f, -.5f, -3.5f, -1, -.3f) }; break;
                case 11: profile.Gestures = new[] { G(.7f, 2, 2, 1.8f, 0), G(4, 1.7f, 2, -2, 0), G(7.7f, 2.5f, -.5f, 3, 1) }; break;
                case 12: profile.Gestures = new[] { G(.7f, 2, 1.5f, 2, .4f), G(3.4f, 1.5f, 1.8f, -1, 0) }; break;
                case 13: profile.Gestures = new[] { G(.7f, 2.1f, 0, -2.5f, -.5f), G(4.4f, 1.8f, 1.6f, 1.5f, 0), G(8.4f, 2.2f, -.5f, 2, .6f) }; break;
                case 14: profile.Gestures = new[] { G(.4f, 1.35f, 3, 0, -.6f, -.4f) }; break;
                case 15: profile.Gestures = new[] { G(.2f, 1.15f, -1.5f, 1, 1.6f, -.5f) }; break;
            }
            return profile;
        }
        private static MiraDialogueGesture G(float time, float duration, float pitch, float yaw, float roll, float shoulder = 0)
            => new MiraDialogueGesture(time, duration, new Vector3(pitch, yaw, roll), shoulder);
    }
}
