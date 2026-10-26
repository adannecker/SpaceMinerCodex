using System;
using UnityEngine;

namespace SpaceMiner
{
    [Serializable]
    public sealed class MiraQuestDialogueGroup
    {
        public int QuestIndex;
        public string Title;
        public MiraDialogueLine[] Cues;
        public MiraDialogueScript Script(string voice)
            => new MiraDialogueScript { Title = Title, Voice = voice, Cues = Cues };
    }

    [Serializable]
    public sealed class MiraQuestDialogueCatalog
    {
        public string Voice;
        public MiraQuestDialogueGroup[] Quests;
        public MiraDialogueLine[] Helpers;
    }

    // Both hosts read these same texts, audio resources, moods and lip-timing assets.
    public static class MiraQuestDialogues
    {
        private static MiraQuestDialogueCatalog catalog;
        public static MiraQuestDialogueCatalog Catalog
        {
            get
            {
                if (catalog != null) return catalog;
                var asset = Resources.Load<TextAsset>("MiraQuests/catalog");
                if (asset == null) throw new InvalidOperationException("Mira quest catalog missing");
                catalog = JsonUtility.FromJson<MiraQuestDialogueCatalog>(asset.text);
                if (catalog?.Quests == null || catalog.Quests.Length != 9 || catalog.Helpers == null)
                    throw new InvalidOperationException("Invalid Mira quest catalog");
                foreach (var quest in catalog.Quests)
                    foreach (var line in quest.Cues) ApplyActing(line);
                foreach (var line in catalog.Helpers) ApplyActing(line);
                return catalog;
            }
        }
        public static MiraDialogueScript Quest(int index)
        {
            if (index < 0 || index >= Catalog.Quests.Length) throw new ArgumentOutOfRangeException(nameof(index));
            return Catalog.Quests[index].Script(Catalog.Voice);
        }
        public static MiraDialogueScript Helpers()
            => new MiraDialogueScript { Title = "Arbeitsabläufe", Voice = Catalog.Voice, Cues = Catalog.Helpers };
        public static MiraDialogueLine Find(string id)
        {
            foreach (var quest in Catalog.Quests)
                foreach (var line in quest.Cues) if (line.Id == id) return line;
            foreach (var line in Catalog.Helpers) if (line.Id == id) return line;
            throw new ArgumentException("Unknown Mira quest line: " + id);
        }
        private static void ApplyActing(MiraDialogueLine line)
        {
            // Gentle attentive posture; precise mouth movement comes from the recorded clip.
            line.Acting = new MiraDialogueActing { MoodIntensity = line.Mood == MiraAvatar.Mood.Encouraging ? .7f : .55f,
                Gestures = new[] {
                    new MiraDialogueGesture(.8f, 2.2f, new Vector3(-.5f, 2.2f, .7f), -.2f),
                    new MiraDialogueGesture(4.5f, 1.8f, new Vector3(1.4f, -1.5f, 0)),
                    new MiraDialogueGesture(9.2f, 2.1f, new Vector3(-.4f, 2, -.6f), .2f),
                    new MiraDialogueGesture(15.1f, 2.3f, new Vector3(1.2f, -1.7f, .4f)),
                    new MiraDialogueGesture(21.4f, 2, new Vector3(-.4f, 1.8f, .5f), -.2f),
                    new MiraDialogueGesture(28.1f, 2.3f, new Vector3(1.3f, -1.5f, 0)),
                    new MiraDialogueGesture(35.2f, 2, new Vector3(-.3f, 1.8f, -.5f)) } };
        }
    }
}
