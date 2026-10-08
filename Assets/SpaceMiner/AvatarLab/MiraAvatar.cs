using UnityEngine;

namespace SpaceMiner
{
    // A reusable portrait presenter: bind any dialogue AudioSource and draw in its UI rect.
    public sealed class MiraAvatar : MonoBehaviour
    {
        public Texture2D Expressions;
        public Texture2D DialoguePortraits;
        public enum Mood { Friendly, Concerned, Focused, Encouraging }
        public Mood CurrentMood { get; private set; }
        private int previousMood;
        private float moodChangedAt = -1;
        public void SetMood(Mood mood)
        {
            if (CurrentMood == mood) return;
            previousMood = (int)CurrentMood;
            CurrentMood = mood;
            moodChangedAt = Time.unscaledTime;
        }
        public AudioSource Speech;
        public float Sensitivity = 12;
        public bool Motion = true;
        public float Mouth { get; private set; }
        public bool Blinking { get; private set; }
        private readonly float[] samples = new float[256];
        private float blinkAt = 3.8f, blinkUntil;

        private void Update()
        {
            float rms = 0;
            if (Speech != null && Speech.isPlaying)
            {
                Speech.GetOutputData(samples, 0);
                foreach (float sample in samples) rms += sample * sample;
                rms = Mathf.Sqrt(rms / samples.Length);
            }
            float target = Mathf.Clamp01((rms - .003f) * Sensitivity);
            Mouth = Mathf.Lerp(Mouth, target, 1 - Mathf.Exp(-Time.unscaledDeltaTime * (target > Mouth ? 35 : 16)));
            if (Time.unscaledTime >= blinkAt)
            {
                blinkUntil = Time.unscaledTime + .13f;
                blinkAt = Time.unscaledTime + Random.Range(3.2f, 5.8f);
            }
            Blinking = Motion && Time.unscaledTime < blinkUntil;
        }

        public void Draw(Rect rect)
        {
            if (DialoguePortraits != null)
            {
                Color color = GUI.color;
                GUI.color = Color.white;
                DrawPortrait(rect, previousMood);
                float blend = Mathf.SmoothStep(0, 1, Mathf.Clamp01((Time.unscaledTime - moodChangedAt) / .45f));
                GUI.color = new Color(1, 1, 1, blend);
                DrawPortrait(rect, (int)CurrentMood);
                GUI.color = color;
                return;
            }
            if (Expressions == null) return;
            var oldMatrix = GUI.matrix;
            var oldColor = GUI.color;
            if (Motion)
            {
                rect.y += Mathf.Sin(Time.unscaledTime * .8f) * 2;
                GUIUtility.RotateAroundPivot(Mathf.Sin(Time.unscaledTime * .46f) * .35f, rect.center);
            }
            GUI.color = Color.white;
            DrawPart(rect, new Rect(0, 0, 1, 1), 0);
            // Keep the portrait stationary: replace only mouth and eye patches, not full poses.
            if (Mouth > .07f) DrawPart(rect, new Rect(.40f, .60f, .28f, .15f), Mouth > .30f ? 2 : 1);
            if (Blinking) DrawPart(rect, new Rect(.32f, .38f, .44f, .12f), 3);
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        private void DrawPortrait(Rect rect, int cell)
        {
            GUI.DrawTextureWithTexCoords(rect, DialoguePortraits,
                new Rect((cell % 2) * .5f, cell < 2 ? .5f : 0, .5f, .5f), true);
        }

        private void DrawPart(Rect portrait, Rect part, int cell)
        {
            float x = (cell % 2) * .5f, y = cell < 2 ? .5f : 0;
            var target = new Rect(portrait.x + part.x * portrait.width, portrait.y + part.y * portrait.height,
                part.width * portrait.width, part.height * portrait.height);
            var uv = new Rect(x + part.x * .5f, y + (1 - part.y - part.height) * .5f, part.width * .5f, part.height * .5f);
            GUI.DrawTextureWithTexCoords(target, Expressions, uv, true);
        }
    }
}
