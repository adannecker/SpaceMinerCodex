using System;
using UnityEngine;
namespace SpaceMiner
{
    // Reusable IMGUI components; contains no settings persistence or gameplay behavior.
    public sealed class SpaceMinerUi : IDisposable
    {
        public static readonly Color Cyan = new Color(.35f, .85f, 1);
        public static readonly Color Amber = new Color(1, .68f, .25f);
        public static readonly Color Error = new Color(1, .35f, .35f);
        public static readonly Color Success = new Color(.4f, .9f, .65f);
        public static readonly Color Disabled = new Color(.48f, .54f, .6f);
        public GUIStyle Heading, Text, Small, Button, Primary, ToggleStyle, Track, Thumb;
        private Texture2D normal, hover, pressed, track, thumb, primary, primaryHover, primaryPressed;
        private float textScale = -1; private bool highContrast; private string expanded;
        public void Configure(AccessibilitySettings settings)
        {
            if (normal == null)
            {
                normal = UiChrome.CreateTexture(new Color(.065f, .12f, .17f), new Color(.22f, .43f, .53f));
                hover = UiChrome.CreateTexture(new Color(.10f, .23f, .29f), Cyan);
                pressed = UiChrome.CreateTexture(new Color(.18f, .18f, .14f), Amber);
                track = UiChrome.CreateTexture(new Color(.025f, .10f, .16f), new Color(.12f, .35f, .45f), 2);
                thumb = UiChrome.CreateTexture(Amber, new Color(1, .85f, .6f), 5);
                primary = UiChrome.CreateTexture(Amber, new Color(1,.8f,.5f));
                primaryHover = UiChrome.CreateTexture(new Color(1, .77f, .40f), new Color(1, .92f, .76f));
                primaryPressed = UiChrome.CreateTexture(new Color(.87f, .52f, .15f), new Color(1, .76f, .40f));
            }
            if (Heading != null && textScale == settings.TextScale && highContrast == settings.HighContrast) return;
            textScale = settings.TextScale; highContrast = settings.HighContrast;
            Color textColor = highContrast ? Color.white : new Color(.83f, .93f, 1);
            Heading = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(24 * textScale), fontStyle = FontStyle.Bold, wordWrap = true }; Heading.normal.textColor = Amber;
            Text = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(17 * textScale), wordWrap = true }; Text.normal.textColor = textColor;
            Small = new GUIStyle(Text) { fontSize = Mathf.RoundToInt(14 * textScale) }; Small.normal.textColor = highContrast ? Color.white : new Color(.65f, .8f, .9f);
            Button = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(16 * textScale), wordWrap = true, padding = new RectOffset(12,12,8,8), border = new RectOffset(10,10,10,10) };
            foreach (GUIStyleState state in new[] { Button.normal, Button.onNormal }) { state.background = normal; state.textColor = Cyan; }
            foreach (GUIStyleState state in new[] { Button.hover, Button.focused, Button.onHover, Button.onFocused }) { state.background = hover; state.textColor = Color.white; }
            foreach (GUIStyleState state in new[] { Button.active, Button.onActive }) { state.background = pressed; state.textColor = Color.white; }
            ToggleStyle = new GUIStyle(Button) { alignment = TextAnchor.MiddleLeft }; ToggleStyle.onNormal.background = pressed;
            Primary = new GUIStyle(Button);
            foreach (GUIStyleState state in new[] { Primary.normal, Primary.hover, Primary.focused, Primary.active }) { state.background = primary; state.textColor = new Color(.035f,.065f,.09f); }
            Primary.hover.background = Primary.focused.background = primaryHover;
            Primary.active.background = primaryPressed;
            Track = new GUIStyle(GUI.skin.horizontalSlider) { fixedHeight = 8, border = new RectOffset(3,3,3,3) }; Track.normal.background = track;
            Thumb = new GUIStyle(GUI.skin.horizontalSliderThumb) { fixedWidth = 16, fixedHeight = 20, border = new RectOffset(6,6,6,6) }; Thumb.normal.background = thumb; Thumb.hover.background = thumb; Thumb.active.background = thumb;
        }
        public bool Toggle(string title, bool value, string tooltip = "")
        { bool old = value; value = GUILayout.Toggle(value, new GUIContent((value ? "[ ON ]  " : "[ OFF ]  ") + title, tooltip), ToggleStyle, GUILayout.MinHeight(42 * textScale)); SettingsUiAudio.Observe(GUILayoutUtility.GetLastRect(), title); if(old != value) SettingsUiAudio.Activate(); GUILayout.Space(12); return value; }
        public float Slider(string title, float value, float min, float max, string tooltip = "")
        { GUILayout.Label(new GUIContent(title + "    " + value.ToString("0.00"), tooltip), Text); GUILayout.Space(8); value = GUILayout.HorizontalSlider(value, min, max, Track, Thumb, GUILayout.Height(22)); GUILayout.Space(18); return value; }
        public int Dropdown(string title, int selected, string[] choices, string tooltip = "")
        {
            if (choices.Length == 0) return selected;
            selected = Mathf.Clamp(selected, 0, choices.Length - 1);
            GUILayout.Label(title, Text);
            if (GUILayout.Button(new GUIContent(choices[selected] + "  ▾", tooltip), Button)) { SettingsUiAudio.Activate(); expanded = expanded == title ? null : title; GUIUtility.ExitGUI(); }
            SettingsUiAudio.Observe(GUILayoutUtility.GetLastRect(), title);
            if (expanded == title)
                for (int i = 0; i < choices.Length; i++) if (GUILayout.Button(choices[i], Button)) { SettingsUiAudio.Activate(); selected = i; expanded = null; }
            GUILayout.Space(14); return selected;
        }
        public bool Tab(Rect rect, string title, bool selected)
        {
            var style = new GUIStyle(selected ? Primary : Button) { wordWrap = false };
            var content = new GUIContent(title);
            while (style.fontSize > 12 && style.CalcSize(content).x > rect.width - 8) style.fontSize--;
            bool clicked = GUI.Button(rect, content, style);
            SettingsUiAudio.Observe(rect, title); if(clicked) SettingsUiAudio.Activate();
            return clicked;
        }
        public void Panel(Rect rect)
        {
            UiChrome.Panel(rect, Cyan, highContrast);
        }
        public void Status(Rect rect, string message, Color color) { var style = new GUIStyle(Small); style.normal.textColor = color; GUI.Label(rect, message, style); }
        public void Tooltip(float scale, float width, float height)
        {
            if (string.IsNullOrEmpty(GUI.tooltip)) return;
            var point = Event.current.mousePosition;
            Rect rect = new Rect(Mathf.Clamp(point.x+14,0,width-360),Mathf.Clamp(point.y+18,0,height-90),360,80);
            Panel(rect); GUI.Label(new Rect(rect.x+12,rect.y+8,336,64),GUI.tooltip,Small);
        }
        public bool Dialog(Rect rect, string title, string body, string confirm)
        {
            Panel(rect); GUI.Label(new Rect(rect.x+16,rect.y+12,rect.width-32,50),title,Heading);
            GUI.Label(new Rect(rect.x+16,rect.y+72,rect.width-32,rect.height-140),body,Text);
            return GUI.Button(new Rect(rect.x+16,rect.yMax-54,rect.width-32,38),confirm,Button);
        }
        public static void Fill(Rect rect, Color color) { var old = GUI.color; GUI.color = color; GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color = old; }
        public static void Border(Rect r, Color c) { Fill(new Rect(r.x,r.y,r.width,1),c); Fill(new Rect(r.x,r.yMax,r.width,1),c); Fill(new Rect(r.x,r.y,1,r.height),c); Fill(new Rect(r.xMax,r.y,1,r.height),c); }
        public void Dispose() { foreach(var t in new[] { normal,hover,pressed,track,thumb,primary,primaryHover,primaryPressed }) if(t != null) UnityEngine.Object.Destroy(t); }
    }
}
