using System;
using UnityEngine;

namespace SpaceMiner
{
    // Pixel-sized UI on large displays; fit down only when a window is too small.
    public static class UiLayout
    {
        private static float? preview;
        public static float RequestedScale => preview ?? SettingsStore.Current.Interface.Scale;
        public static void Preview(float scale) => preview = Mathf.Clamp(scale, .75f, 1.5f);
        public static void EndPreview() => preview = null;
        public static float Fit(float width, float height, float referenceWidth, float referenceHeight, float requested)
            => Mathf.Max(.1f, Mathf.Min(requested, Mathf.Min(width / referenceWidth, height / referenceHeight)));
        public static float Scale(float referenceWidth = 1440, float referenceHeight = 900)
            => Fit(Screen.width, Screen.height, referenceWidth, referenceHeight, RequestedScale);
        public static float Width => Screen.width / Scale();
        public static float Height => Screen.height / Scale();
        public static Vector2 Point(Vector3 screenPoint, float referenceWidth = 1440, float referenceHeight = 900)
            => new Vector2(screenPoint.x, Screen.height - screenPoint.y) / Scale(referenceWidth, referenceHeight);
        public readonly struct Scope : IDisposable
        {
            private readonly Matrix4x4 previous;
            public Scope(float scale) { previous = GUI.matrix; GUI.matrix = Matrix4x4.Scale(Vector3.one * scale); }
            public void Dispose() => GUI.matrix = previous;
        }
    }
}
