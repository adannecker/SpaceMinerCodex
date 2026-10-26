using UnityEngine;

namespace SpaceMiner
{
    // Shared visual surfaces only. Nine-slicing keeps corners and strokes the same
    // size at every panel size, without changing layout, hit areas or GUI.matrix.
    internal static class UiChrome
    {
        private static Texture2D fillTexture, edgeTexture, barFillTexture, barEdgeTexture;
        private static GUIStyle fillStyle, edgeStyle, barFillStyle, barEdgeStyle;
        private static readonly Color ClearWhite = new Color(1, 1, 1, 0);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Release();
            Application.quitting -= Release;
            Application.quitting += Release;
        }

        private static void Release()
        {
            if (fillTexture != null) Object.Destroy(fillTexture);
            if (edgeTexture != null) Object.Destroy(edgeTexture);
            if (barFillTexture != null) Object.Destroy(barFillTexture);
            if (barEdgeTexture != null) Object.Destroy(barEdgeTexture);
            fillTexture = edgeTexture = barFillTexture = barEdgeTexture = null;
            fillStyle = edgeStyle = barFillStyle = barEdgeStyle = null;
        }

        private static void Ensure()
        {
            if (fillTexture != null) return;
            fillTexture = CreateTexture(Color.white, Color.white, 10);
            edgeTexture = CreateTexture(ClearWhite, Color.white, 10);
            fillStyle = Style(fillTexture);
            edgeStyle = Style(edgeTexture);
            barFillTexture = CreateTexture(Color.white, Color.white, 2);
            barEdgeTexture = CreateTexture(ClearWhite, Color.white, 2);
            barFillStyle = Style(barFillTexture, 3);
            barEdgeStyle = Style(barEdgeTexture, 3);
        }

        private static GUIStyle Style(Texture2D texture, int inset = 12)
        {
            var style = new GUIStyle { border = new RectOffset(inset, inset, inset, inset) };
            style.normal.background = texture;
            return style;
        }

        private static void Draw(Rect rect, GUIStyle style, Color tint)
        {
            if (rect.width <= 0 || rect.height <= 0) return;
            var previous = GUI.color;
            try
            {
                GUI.color = previous * tint;
                style.Draw(rect, GUIContent.none, false, false, false, false);
            }
            finally { GUI.color = previous; }
        }

        public static void Surface(Rect rect, Color fill, Color edge)
        {
            if (Event.current.type != EventType.Repaint) return;
            Ensure();
            Draw(rect, fillStyle, fill);
            Draw(rect, edgeStyle, edge);
        }

        public static void Panel(Rect rect, Color accent, bool highContrast = false)
        {
            if (Event.current.type != EventType.Repaint) return;
            Ensure();
            Draw(new Rect(rect.x, rect.y + 3, rect.width, rect.height), fillStyle, new Color(0, .005f, .012f, .24f));
            Surface(rect, highContrast ? new Color(.008f, .016f, .026f, 1) : new Color(.025f, .058f, .088f, .97f),
                highContrast ? new Color(.36f, .75f, .87f) : new Color(.18f, .47f, .59f, .8f));
            // Short illuminated cap and a quiet inner header line, clear of text.
            SpaceMinerUi.Fill(new Rect(rect.x + 14, rect.y + 1, Mathf.Min(44, Mathf.Max(0, rect.width - 28)), 2), accent);
            if (rect.width > 100)
                SpaceMinerUi.Fill(new Rect(rect.x + 64, rect.y + 1, rect.width - 80, 1), new Color(accent.r, accent.g, accent.b, .12f));
        }

        public static void Progress(Rect rect, float fraction, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;
            Ensure();
            // Slim bars need their own small corner slices; panel-sized slices
            // would overlap inside a 6-16px track and leave visible seam lines.
            Draw(rect, barFillStyle, new Color(.065f, .12f, .16f));
            Draw(rect, barEdgeStyle, new Color(.17f, .29f, .35f, .6f));
            float width = rect.width * Mathf.Clamp01(fraction);
            if (width <= 0) return;
            var filled = new Rect(rect.x, rect.y, width, rect.height);
            if (width < 6 || rect.height < 6) SpaceMinerUi.Fill(filled, color);
            else
            {
                Draw(filled, barFillStyle, color);
                Draw(filled, barEdgeStyle, new Color(1, 1, 1, .15f));
            }
        }

        // Created once per style, then reused by IMGUI; no texture work per frame.
        internal static Texture2D CreateTexture(Color fill, Color edge, float radius = 8)
        {
            const int size = 40;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "SpaceMiner rounded UI surface",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float qx = Mathf.Abs(x + .5f - size / 2f) - (size / 2f - radius);
                float qy = Mathf.Abs(y + .5f - size / 2f) - (size / 2f - radius);
                float distance = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude
                    + Mathf.Min(Mathf.Max(qx, qy), 0) - radius;
                float coverage = Mathf.Clamp01(.5f - distance);
                float interior = Mathf.Clamp01(-.5f - distance);
                var body = fill;
                float shade = .97f + .06f * y / (size - 1);
                body.r *= shade; body.g *= shade; body.b *= shade;
                var color = Color.Lerp(edge, body, interior);
                color.a *= coverage;
                pixels[y * size + x] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
