using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceMiner
{
    // Real display surfaces, not transparent lettering that can be read backwards through a room.
    // Both faces carry independently oriented text; the opaque panel hides the reverse of each face.
    public sealed class StationDisplay : MonoBehaviour
    {
        public string Caption { get; private set; }
        public TextMesh Front { get; private set; }
        public TextMesh Back { get; private set; }
        public Vector2 PanelSize { get; private set; }
        private float maximumCharacterSize;

        public void SetCaption(string caption)
        {
            Caption = caption ?? string.Empty;
            if (Front == null || Back == null) return;
            if (maximumCharacterSize <= 0) maximumCharacterSize = Front.characterSize;
            Front.text = Back.text = Caption;
            // Keep the authored panel and both face orientations. A longer status
            // must fit its physical screen instead of growing into the passage.
            Front.characterSize = Back.characterSize = maximumCharacterSize;
            Vector3 bounds = Front.GetComponent<Renderer>().localBounds.size;
            float fit = Mathf.Min(1, Mathf.Max(.01f, PanelSize.x - .12f) / Mathf.Max(.001f, bounds.x),
                Mathf.Max(.01f, PanelSize.y - .08f) / Mathf.Max(.001f, bounds.y));
            Front.characterSize = Back.characterSize = maximumCharacterSize * fit;
        }

        internal static StationDisplay Create(string caption, Transform parent, Vector3 position,
            Quaternion rotation, float characterSize, Font font, Material letters,
            Material panel, Material trim, int layer, float maxWidth = 0)
        {
            var root = new GameObject(caption); root.layer = layer;
            root.transform.SetParent(parent, false); root.transform.localPosition = position;
            root.transform.localRotation = rotation;
            var display = root.AddComponent<StationDisplay>(); display.Caption = caption;
            bool overhead = caption.Contains("RING ") || caption == "DROHNENDECK"
                || caption == "REAKTOR-SERVICE" || caption == "KOMMANDOZENTRALE";
            float widthLimit = maxWidth > 0 ? maxWidth : overhead ? 3.3f : characterSize <= .045f ? 1.4f : 3.2f;
            float heightLimit = overhead ? .62f : .5f;
            display.Front = Face("Display / Vorderseite", root.transform, caption, font, letters,
                characterSize, new Vector3(0, 0, -.044f), Quaternion.identity, layer);
            var bounds = display.Front.GetComponent<Renderer>().localBounds;
            float fit = Mathf.Min(1, (widthLimit - .18f) / Mathf.Max(.01f, bounds.size.x),
                (heightLimit - .12f) / Mathf.Max(.01f, bounds.size.y));
            display.Front.characterSize *= fit;
            bounds = display.Front.GetComponent<Renderer>().localBounds;
            display.PanelSize = new Vector2(Mathf.Clamp(bounds.size.x + .2f, .4f, widthLimit),
                Mathf.Clamp(bounds.size.y + .15f, .22f, heightLimit));
            display.Back = Face("Display / Rückseite", root.transform, caption, font, letters,
                display.Front.characterSize, new Vector3(0, 0, .044f), Quaternion.Euler(0, 180, 0), layer);
            display.maximumCharacterSize = display.Front.characterSize;
            Surface("Display / Gehäuse", root.transform, Vector3.zero,
                new Vector3(display.PanelSize.x, display.PanelSize.y, .075f), panel, layer);
            foreach (float side in new[] { -1f, 1f })
                Surface("Display / Leuchtkante", root.transform,
                    new Vector3(0, display.PanelSize.y * .5f - .018f, side * .04f),
                    new Vector3(display.PanelSize.x - .04f, .018f, .009f), trim, layer);
            return display;
        }

        private static TextMesh Face(string name, Transform parent, string caption, Font font,
            Material surface, float size, Vector3 position, Quaternion rotation, int layer)
        {
            var go = new GameObject(name); go.layer = layer; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localRotation = rotation;
            var text = go.AddComponent<TextMesh>(); text.font = font; text.fontSize = 64;
            text.text = caption; text.characterSize = size; text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center; text.color = new Color(.7f, .94f, 1);
            var renderer = text.GetComponent<MeshRenderer>(); renderer.sharedMaterial = surface;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            return text;
        }

        private static void Surface(string name, Transform parent, Vector3 position,
            Vector3 size, Material surface, int layer)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.layer = layer;
            go.transform.SetParent(parent, false); go.transform.localPosition = position;
            go.transform.localScale = size; var collider = go.GetComponent<Collider>();
            collider.enabled = false; Destroy(collider);
            var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = surface;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
    }
}
