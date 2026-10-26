using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    public sealed partial class StationOperations
    {
        private static readonly bool AutomaticMiraDialogues = !Array.Exists(Environment.GetCommandLineArgs(),
            a => a.EndsWith("Check", StringComparison.OrdinalIgnoreCase));
        // Additive save field: quest mechanics and campaign version remain unchanged.
        public void RememberMiraLine(string id)
        {
            if (string.IsNullOrEmpty(id) || !id.StartsWith("quest_", StringComparison.Ordinal)) return;
            State.miraDialogues ??= new List<string>();
            if (!State.miraDialogues.Contains(id)) State.miraDialogues.Add(id);
        }
        private bool MiraHeard(string id) => State.miraDialogues != null && State.miraDialogues.Contains(id);
        private void RestoreMiraQuestProgress()
        {
            State.miraDialogues ??= new List<string>();
            // Older saves have no narrative history. Do not replay their old successes.
            if (State.miraDialogues.Count != 0) return;
            for (int q = 0; q < questTitles.Length; q++) if (QuestDone(q))
            {
                RememberMiraLine("quest_" + q.ToString("00") + "_start");
                RememberMiraLine("quest_" + q.ToString("00") + "_complete");
            }
        }
        public bool PresentQuestDialogue(int quest, bool complete)
        {
            if (quest < 0 || quest >= questTitles.Length || Scenario?.Scanner == null) return false;
            ClosePanel();
            Scenario.Scanner.Say(MiraQuestDialogues.Quest(quest).Cues[complete ? 1 : 0]);
            return true;
        }
        public bool TickMiraQuestDialogue()
        {
            if (!Live || Scenario?.Scanner == null || StartMenu.IsOpen || IntroSequence.BlocksGameplay
                || SettingsMenu.IsOpen || SaveGameMenu.IsOpen || MemoryCinematic.IsPlaying || ForeignModal) return false;
            var current = Scenario.Scanner.DialogueModule?.Current;
            if (current != null) { RememberMiraLine(current.Id); return false; }
            if (Scenario.Scanner.Dialogue != null || PanelOpen || Scenario.Scanner.AtConsole) return false;
            // Re-evaluate after every acknowledgement. Parallel work never overwrites speech.
            for (int q = 0; q < questTitles.Length; q++)
                if (QuestDone(q) && !MiraHeard("quest_" + q.ToString("00") + "_complete"))
                {
                    RememberMiraLine("quest_" + q.ToString("00") + "_start");
                    return PresentQuestDialogue(q, true);
                }
            int next = CurrentQuest;
            if (!QuestDone(next) && !MiraHeard("quest_" + next.ToString("00") + "_start"))
                return PresentQuestDialogue(next, false);
            string helper = null;
            if (State.helper && State.inventory.scrap.amount >= 10
                && !State.orders.Exists(o => o.kind == StationJobKind.Recycle && o.complete && !o.cancelled)
                && !MiraHeard("quest_support_recycle")) helper = "quest_support_recycle";
            else if (State.helper && State.inventory.iron.amount >= 2
                && !State.orders.Exists(o => o.kind == StationJobKind.PrintParts && o.complete && !o.cancelled)
                && !MiraHeard("quest_support_print_parts")) helper = "quest_support_print_parts";
            else if (State.technologies.Contains("repair") && State.repairExperience == 0
                && !MiraHeard("quest_support_repair_researched")) helper = "quest_support_repair_researched";
            else if (Array.Exists(State.tools, tool => tool == "Laser") && State.repairExperience == 0
                && !MiraHeard("quest_support_laser_ready")) helper = "quest_support_laser_ready";
            if (helper == null) return false;
            Scenario.Scanner.Say(MiraQuestDialogues.Find(helper));
            return true;
        }
        private void LateUpdate()
        {
            // Existing acceptance suites control dialogs explicitly. The quest suite calls
            // Tick directly; ordinary play observes actual state changes automatically.
            if (!AutomaticMiraDialogues) return;
            TickMiraQuestDialogue();
        }
    }
}
