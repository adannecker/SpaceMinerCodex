using System;
using UnityEngine;

namespace SpaceMiner
{
    // Isolated host for exactly the same module as awakening and station dialogue.
    public sealed class MiraDialogueDemo : MonoBehaviour
    {
        public MiraDialogueModule Dialogue { get; private set; }
        public MiraDialogueScript Script { get; private set; }
        public int Selected { get; private set; }
        public bool Continuous { get; private set; }
        public int SelectedQuest { get; private set; } = -1;
        public bool ShowingHelpers { get; private set; }
        public float ConversationDuration { get; private set; }
        private readonly SpaceMinerUi ui = new SpaceMinerUi();
        private Vector2 listScroll, navigationScroll;
        private bool questsExpanded = true;
        private float demoScale = 1;
        private float textScale = 1;

        private void Awake()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            // Preview changes are session-local; never read or write campaign saves/settings.
            PlayerAudio.Apply(new AudioSettings());
            Script = MiraDialogueScript.LoadAwakening();
            Dialogue = MiraDialogueModule.Create(gameObject);
            Dialogue.Portrait.PreviewWhenClosed = true;
            Dialogue.Portrait.SetMode(MiraVisualMode.HighModel);
            Dialogue.LineCompleted += Completed;
            Dialogue.Closed += () => Continuous = false;
            MeasureConversation();
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-miraQuests") >= 0) SelectQuest(0);
            else Play(0);
        }

        public void SelectAwakening()
        {
            SelectedQuest = -1; ShowingHelpers = false;
            navigationScroll = Vector2.zero;
            SelectScript(MiraDialogueScript.LoadAwakening(), false);
        }
        public void SelectQuest(int quest)
        {
            var script = MiraQuestDialogues.Quest(quest);
            SelectedQuest = quest; ShowingHelpers = false; questsExpanded = true;
            navigationScroll.y = Mathf.Max(0, (quest + 2) * 58 - 240);
            SelectScript(script, true);
        }
        public void SelectHelpers()
        {
            SelectedQuest = -1; ShowingHelpers = true;
            navigationScroll.y = 1000;
            SelectScript(MiraQuestDialogues.Helpers(), false);
        }
        private void SelectScript(MiraDialogueScript script, bool playAll)
        {
            Dialogue.Close(); Script = script; listScroll = Vector2.zero;
            MeasureConversation(); Continuous = playAll; Play(0);
        }
        private void MeasureConversation()
        {
            ConversationDuration = 0;
            foreach (var line in Script.Cues)
            {
                var clip = string.IsNullOrEmpty(line.Audio) ? null : Resources.Load<AudioClip>(line.Audio);
                ConversationDuration += line.Duration(clip);
            }
        }
        public void PlayWholeConversation() { Continuous = true; Play(0); }

        public void Play(int index)
        {
            Selected = Mathf.Clamp(index, 0, Script.Cues.Length - 1);
            Dialogue.Play(Script.Cues[Selected]);
        }
        public void SetContinuous(bool value) { Continuous = value; }
        private void Completed()
        {
            if (Continuous && Selected + 1 < Script.Cues.Length) Play(Selected + 1);
            else Continuous = false;
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space)) Dialogue.SetPaused(!Dialogue.Paused);
            if (Input.GetKeyDown(KeyCode.LeftArrow)) Play(Selected - 1);
            if (Input.GetKeyDown(KeyCode.RightArrow)) Play(Selected + 1);
            if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
        }

        private void OnGUI()
        {
            UiLayout.Preview(demoScale);
            var accessibility = new AccessibilitySettings { TextScale = textScale };
            ui.Configure(accessibility);
            // The shared panel reads the same session-local accessibility settings in both hosts.
            SettingsStore.Current.Accessibility.TextScale = textScale;
            using (var layout = new UiLayout.Scope(UiLayout.Scale()))
            {
                float width = UiLayout.Width, height = UiLayout.Height;
                GUI.Label(new Rect(28, 22, width - 56, 40), "MIRA / DIALOGSTUDIO", ui.Heading);
                GUI.Label(new Rect(28, 66, width - 56, 32), (Script.Title ?? "Erwachen") + " · " + Script.Voice, ui.Small);
                float contentHeight = Mathf.Min(360, height - 460);
                var navigation = new Rect(28, 112, 240, contentHeight);
                ui.Panel(navigation);
                float navHeight = questsExpanded ? 12 * 58 : 3 * 58;
                navigationScroll = GUI.BeginScrollView(new Rect(navigation.x + 8, navigation.y + 8, navigation.width - 16, navigation.height - 16),
                    navigationScroll, new Rect(0, 0, navigation.width - 40, navHeight));
                float ny = 0;
                if (GUI.Button(new Rect(0, ny, 200, 50), (SelectedQuest < 0 && !ShowingHelpers ? "● " : "") + "Erwachen", ui.Button)) SelectAwakening();
                ny += 58;
                if (GUI.Button(new Rect(0, ny, 200, 50), (questsExpanded ? "▾ " : "▸ ") + "Quests", ui.Button)) questsExpanded = !questsExpanded;
                ny += 58;
                if (questsExpanded)
                    for (int q = 0; q < MiraQuestDialogues.Catalog.Quests.Length; q++)
                    {
                        if (GUI.Button(new Rect(12, ny, 188, 50), (q == SelectedQuest ? "● " : "") + (q + 1).ToString("00") + " · " + MiraQuestDialogues.Catalog.Quests[q].Title, ui.Button)) SelectQuest(q);
                        ny += 58;
                    }
                if (GUI.Button(new Rect(0, ny, 200, 50), (ShowingHelpers ? "● " : "") + "Arbeitsabläufe", ui.Button)) SelectHelpers();
                GUI.EndScrollView();
                var list = new Rect(284, 112, 270, contentHeight);
                ui.Panel(list);
                listScroll = GUI.BeginScrollView(new Rect(list.x + 10, list.y + 10, list.width - 20, list.height - 20), listScroll,
                    new Rect(0, 0, list.width - 44, Script.Cues.Length * 60));
                for (int i = 0; i < Script.Cues.Length; i++)
                    if (GUI.Button(new Rect(0, i * 60, list.width - 44, 54),
                        (i == Selected ? "▶ " : "") + (i + 1).ToString("00") + " · " + Script.Cues[i].Heading, ui.Button)) Play(i);
                GUI.EndScrollView();

                var portraitBounds = new Rect(574, 112, 320, 320);
                ui.Panel(portraitBounds);
                Dialogue.Portrait.Draw(new Rect(582, 120, 304, 304));
                float x = 918, w = width - x - 28;
                GUI.Label(new Rect(x, 116, w, 32), "ABSCHNITT " + (Selected + 1) + " / " + Script.Cues.Length, ui.Heading);
                string state = !Dialogue.Visible ? "GESCHLOSSEN" : Dialogue.Paused ? "PAUSIERT"
                    : Dialogue.TextOnly ? "TEXT · AUFNAHME AUSSTEHEND" : Dialogue.Speech.isPlaying ? "MIRA SPRICHT" : "WIEDERGABE BEENDET";
                GUI.Label(new Rect(x, 160, w, 30), state, ui.Small);
                GUI.Label(new Rect(x, 195, w, 30), Dialogue.Elapsed.ToString("F1") + " / " + Dialogue.Duration.ToString("F1") + " s", ui.Small);
                float bw = (w - 20) / 3;
                if (GUI.Button(new Rect(x, 240, bw, 38), Dialogue.Paused ? "Fortsetzen" : "Pause", ui.Button)) Dialogue.SetPaused(!Dialogue.Paused);
                if (GUI.Button(new Rect(x + bw + 10, 240, bw, 38), "Wiederholen", ui.Button)) Play(Selected);
                if (GUI.Button(new Rect(x + 2 * (bw + 10), 240, bw, 38), "Stoppen", ui.Button)) { Continuous = false; Dialogue.Close(); }
                if (GUI.Button(new Rect(x, 290, w, 36), "Ganzes Gespräch anhören · " + Mathf.CeilToInt(ConversationDuration) + " s", ui.Button)) PlayWholeConversation();
                Continuous = GUI.Toggle(new Rect(x, 332, w, 30), Continuous, "Alle Abschnitte nacheinander", ui.ToggleStyle);
                Dialogue.Subtitles = GUI.Toggle(new Rect(574, 440, 320, 30), Dialogue.Subtitles, "Untertitel anzeigen", ui.ToggleStyle);
                GUI.Label(new Rect(x, 372, 165, 28), "Stimme", ui.Small);
                Dialogue.SetVolume(GUI.HorizontalSlider(new Rect(x + 170, 384, w - 170, 20), Dialogue.Volume, 0, 1));
                GUI.Label(new Rect(x, 408, 165, 28), "UI-Größe", ui.Small);
                demoScale = GUI.HorizontalSlider(new Rect(x + 170, 420, w - 170, 20), demoScale, .75f, 1.5f);
                GUI.Label(new Rect(x, 444, 165, 28), "Textgröße", ui.Small);
                textScale = GUI.HorizontalSlider(new Rect(x + 170, 456, w - 170, 20), textScale, 1, 1.4f);
                string[] models = { "Bild", "Lowmodell · Tripo 2.5", "Highmodell · Tripo 3.1" };
                for (int i = 0; i < models.Length; i++)
                {
                    var mode = (MiraVisualMode)i;
                    if (GUI.Button(new Rect(574 + i * 280, 488, 270, 36),
                        (Dialogue.Portrait.Mode == mode ? "● " : "") + models[i], ui.Button)) Dialogue.Portrait.SetMode(mode);
                }
                if (Dialogue.Portrait.Mode != MiraVisualMode.Image)
                {
                    Dialogue.Portrait.HeadMotion = GUI.Toggle(new Rect(574, 535, 190, 28), Dialogue.Portrait.HeadMotion, "Kopfbewegung", ui.ToggleStyle);
                    Dialogue.Portrait.EyeMotion = GUI.Toggle(new Rect(774, 535, 140, 28), Dialogue.Portrait.EyeMotion, "Augen", ui.ToggleStyle);
                    Dialogue.Portrait.Expressions = GUI.Toggle(new Rect(924, 535, 140, 28), Dialogue.Portrait.Expressions, "Mimik", ui.ToggleStyle);
                    GUI.Label(new Rect(1084, 535, 80, 28), "Mund", ui.Small);
                    Dialogue.Portrait.MouthStrength = GUI.HorizontalSlider(new Rect(1164, 548, 138, 20), Dialogue.Portrait.MouthStrength, .4f, 1.4f);
                    Dialogue.Portrait.BodyMotion = GUI.Toggle(new Rect(28, 488, 526, 28), Dialogue.Portrait.BodyMotion, "Schultern / Oberkörper", ui.ToggleStyle);
                    GUI.Label(new Rect(28, 535, 150, 28), "Bewegung", ui.Small);
                    Dialogue.Portrait.MovementStrength = GUI.HorizontalSlider(new Rect(180, 548, 170, 20), Dialogue.Portrait.MovementStrength, 0, 1.5f);
                    string[] moods = { "freundlich", "besorgt", "konzentriert", "ermutigend" };
                    GUI.Label(new Rect(x + 210, 195, w - 210, 28), moods[(int)Dialogue.Portrait.Mood], ui.Small);
                }
                var bounds = MiraVisorOverlay.Bounds(width, height);
                Dialogue.Draw(bounds, "DIALOGSTUDIO / MIRA · " + (Selected + 1) + " / " + Script.Cues.Length, "Verstanden");
                GUI.Label(new Rect(28, height - 24, width - 56, 22), "← / → Abschnitt   ·   Leertaste Pause / Weiter   ·   Escape Schließen", ui.Small);
            }
        }
        private void OnDestroy() { UiLayout.EndPreview(); ui.Dispose(); }
    }
}
