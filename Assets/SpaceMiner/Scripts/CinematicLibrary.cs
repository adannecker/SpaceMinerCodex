using System;
using UnityEngine;

namespace SpaceMiner
{
    public enum StorySequenceKind { Cinematic, Cutscene }

    public static class CinematicLibrary
    {
        public const string FirstMemories = "first-memories-v1";
        public static bool IsAvailable(string id) => id == FirstMemories || IsSeen(id);
        public const string MiraAwakening = "mira-awakening-v1";
        public sealed class Entry
        {
            public readonly string Id, Title, Description;
            public readonly StorySequenceKind Kind;
            public Entry(string id, string title, string description, StorySequenceKind kind)
            { Id = id; Title = title; Description = description; Kind = kind; }
        }

        // Only playable story sequences belong here. Production drafts are not unlocks.
        public static readonly Entry[] Entries =
        {
            new Entry(FirstMemories,"Erinnerungen · Teile 1–12","Von unserer alten Welt bis zu deinem Neubeginn. Enceladus.",StorySequenceKind.Cinematic),
            new Entry(MiraAwakening, "Mira · Erwachen", "Das Erwachen auf der beschädigten Station.", StorySequenceKind.Cutscene)
        };
        internal static string PreferencePrefix = "SpaceMiner.Story.Seen.";
        public static bool IsSeen(string id) => PlayerPrefs.GetInt(PreferencePrefix + id, 0) == 1;
        public static void MarkSeen(string id)
        {
            // Existing integration checks must not unlock the player's story gallery.
            if (PreferencePrefix == "SpaceMiner.Story.Seen.")
                foreach (string arg in Environment.GetCommandLineArgs())
                {
                    if (arg == "-quitMenuCheck") return;
                    if (arg == "-spaceMinerSmokeTest" || arg == "-startMenuCheck" || arg == "-settingsPreview" || arg == "-splashAudioCheck" || arg == "-configurationAudioCheck" || arg == "-techTreeCheck") return;
                }
            PlayerPrefs.SetInt(PreferencePrefix + id, 1);
            PlayerPrefs.Save();
        }
    }
}
