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
        private Texture2D normal, hover, pressed, track, thumb, primary;
        private float textScale = -1; private bool highContrast; private string expanded;
        public void Configure(AccessibilitySettings settings)
        {
            if (normal == null)
            {
                normal = Texture(new Color(.09f, .15f, .20f), new Color(.3f, .4f, .47f));
                hover = Texture(new Color(.26f, .22f, .15f), Amber); pressed = Texture(new Color(.35f, .27f, .13f), Amber);
                track = Texture(new Color(.025f, .10f, .16f), new Color(.12f, .35f, .45f)); thumb = Texture(Amber, Color.white);
                primary = Texture(Amber, new Color(1,.8f,.5f));
            }
            if (Heading != null && textScale == settings.TextScale && highContrast == settings.HighContrast) return;
            textScale = settings.TextScale; highContrast = settings.HighContrast;
            Color textColor = highContrast ? Color.white : new Color(.83f, .93f, 1);
            Heading = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(24 * textScale), fontStyle = FontStyle.Bold, wordWrap = true }; Heading.normal.textColor = Amber;
            Text = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(17 * textScale), wordWrap = true }; Text.normal.textColor = textColor;
            Small = new GUIStyle(Text) { fontSize = Mathf.RoundToInt(14 * textScale) }; Small.normal.textColor = highContrast ? Color.white : new Color(.65f, .8f, .9f);
            Button = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(16 * textScale), wordWrap = true, padding = new RectOffset(12,12,8,8), border = new RectOffset(2,2,2,2) };
            foreach (GUIStyleState state in new[] { Button.normal, Button.onNormal }) { state.background = normal; state.textColor = Cyan; }
            foreach (GUIStyleState state in new[] { Button.hover, Button.focused, Button.onHover, Button.onFocused }) { state.background = hover; state.textColor = Color.white; }
            foreach (GUIStyleState state in new[] { Button.active, Button.onActive }) { state.background = pressed; state.textColor = Color.white; }
            ToggleStyle = new GUIStyle(Button) { alignment = TextAnchor.MiddleLeft }; ToggleStyle.onNormal.background = hover;
            Primary = new GUIStyle(Button);
            foreach (GUIStyleState state in new[] { Primary.normal, Primary.hover, Primary.focused, Primary.active }) { state.background = primary; state.textColor = new Color(.035f,.065f,.09f); }
            Track = new GUIStyle(GUI.skin.horizontalSlider) { fixedHeight = 8, border = new RectOffset(2,2,2,2) }; Track.normal.background = track;
            Thumb = new GUIStyle(GUI.skin.horizontalSliderThumb) { fixedWidth = 16, fixedHeight = 20 }; Thumb.normal.background = thumb; Thumb.hover.background = thumb; Thumb.active.background = thumb;
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
            bool clicked = GUI.Button(rect, title, selected ? Primary : Button);
            SettingsUiAudio.Observe(rect, title); if(clicked) SettingsUiAudio.Activate();
            if (selected) Fill(new Rect(rect.x, rect.y, 4, rect.height), Amber);
            return clicked;
        }
        public void Panel(Rect rect)
        {
            Fill(rect, highContrast ? new Color(.008f,.015f,.025f,.99f) : new Color(.025f,.055f,.09f,.94f));
            for (int i = 0; i < 4; i++) Border(new Rect(rect.x-i,rect.y-i,rect.width+i*2,rect.height+i*2), new Color(.1f,.75f,1,.2f/(i+1)));
            Fill(new Rect(rect.x,rect.y,rect.width,4), Amber);
            Fill(new Rect(rect.x+8,rect.y+12,22,2),Cyan); Fill(new Rect(rect.x+8,rect.y+12,2,22),Cyan);
            Fill(new Rect(rect.xMax-30,rect.yMax-12,22,2),Cyan); Fill(new Rect(rect.xMax-10,rect.yMax-34,2,22),Cyan);
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
        private static Texture2D Texture(Color fill, Color edge)
        { var t = new Texture2D(16,16,TextureFormat.RGBA32,false) { hideFlags = HideFlags.HideAndDontSave }; for (int y=0;y<16;y++) for(int x=0;x<16;x++) t.SetPixel(x,y,x==0||y==0||x==15||y==15?edge:fill); t.Apply(); return t; }
        public void Dispose() { foreach(var t in new[] { normal,hover,pressed,track,thumb,primary }) if(t != null) UnityEngine.Object.Destroy(t); }
    }
}
