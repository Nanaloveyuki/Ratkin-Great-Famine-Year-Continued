using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MouseDisaster
{
    public enum MouseDisasterIncidentCategory
    {
        MouseDisaster = 0,
        Plague = 1
    }

    public enum MouseDisasterIncidentTargetKind
    {
        Map = 0,
        Caravan = 1
    }

    public enum MouseDisasterIncidentLegacyToggleGroup
    {
        None = 0,
        Wild = 1,
        Thief = 2,
        Beggar = 3
    }

    public sealed class MouseDisasterIncidentEntry
    {
        public string DefName { get; }

        private readonly string displayLabel;

        public string DisplayLabel => displayLabel.Translate();

        public bool IsOriginal { get; internal set; }

        public bool BroadcastEligible { get; internal set; }

        public MouseDisasterIncidentCategory Category { get; }

        public MouseDisasterIncidentTargetKind TargetKind { get; }

        public MouseDisasterIncidentLegacyToggleGroup LegacyToggleGroup { get; }

        public bool ControlledByLegacyToggle { get; }

        public float DebugPoints { get; }

        public MouseDisasterIncidentEntry(
            string defName,
            string displayLabel,
            MouseDisasterIncidentCategory category,
            MouseDisasterIncidentTargetKind targetKind,
            MouseDisasterIncidentLegacyToggleGroup legacyToggleGroup,
            bool controlledByLegacyToggle,
            float debugPoints)
        {
            DefName = defName;
            this.displayLabel = displayLabel;
            Category = category;
            TargetKind = targetKind;
            LegacyToggleGroup = legacyToggleGroup;
            ControlledByLegacyToggle = controlledByLegacyToggle;
            DebugPoints = debugPoints;
        }
    }

    public static class MouseDisasterIncidentCatalog
    {
        private static readonly List<MouseDisasterIncidentEntry> Entries = new List<MouseDisasterIncidentEntry>
        {
            new MouseDisasterIncidentEntry("MouseDisaster_LargeRefugeeWave", "MouseDisaster_LargeRefugeeWave_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Beggar, true, 700f) { IsOriginal = true, BroadcastEligible = true },
            new MouseDisasterIncidentEntry("MouseDisaster_AbandonedRatkinChildren", "MouseDisaster_AbandonedRatkinChildren_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 400f) { IsOriginal = true, BroadcastEligible = true },
            new MouseDisasterIncidentEntry("MouseDisaster_ShatteredMother", "MouseDisaster_ShatteredMother_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 400f) { IsOriginal = true, BroadcastEligible = true },
            new MouseDisasterIncidentEntry("MouseDisaster_BeggarFamily", "MouseDisaster_BeggarFamily_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Beggar, true, 200f) { IsOriginal = true, BroadcastEligible = true },
            new MouseDisasterIncidentEntry("MouseDisaster_BeggarGroup", "MouseDisaster_BeggarGroup_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Beggar, true, 300f) { IsOriginal = true, BroadcastEligible = true },
            new MouseDisasterIncidentEntry("MouseDisaster_ThiefRatkinGroup", "MouseDisaster_ThiefRatkinGroup_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Thief, true, 300f) { IsOriginal = true, BroadcastEligible = true },
            new MouseDisasterIncidentEntry("MouseDisaster_ThiefRatkinChildGroup", "MouseDisaster_ThiefRatkinChildGroup_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Thief, true, 300f) { IsOriginal = true, BroadcastEligible = true },
            new MouseDisasterIncidentEntry("MouseDisaster_WildRatkinWandersIn", "MouseDisaster_WildRatkinWandersIn_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Wild, true, 150f) { IsOriginal = true, BroadcastEligible = true },
            new MouseDisasterIncidentEntry("MouseDisaster_WildRatkinChildWandersIn", "MouseDisaster_WildRatkinChildWandersIn_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Wild, true, 150f) { IsOriginal = true, BroadcastEligible = true },
            new MouseDisasterIncidentEntry("MouseDisaster_WildRatkinGroupWandersIn", "MouseDisaster_WildRatkinGroupWandersIn_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Wild, true, 400f) { IsOriginal = true, BroadcastEligible = true },
            new MouseDisasterIncidentEntry("MouseDisaster_FamineRefugees", "MouseDisaster_FamineRefugees_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 350f) { IsOriginal = true, BroadcastEligible = true },
            new MouseDisasterIncidentEntry("MouseDisaster_RatkinTraderCaravan", "MouseDisaster_RatkinTraderCaravan_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 350f) { IsOriginal = true, BroadcastEligible = true },
            new MouseDisasterIncidentEntry("MouseDisaster_ChildExchange", "MouseDisaster_ChildExchange_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 200f) { IsOriginal = true, BroadcastEligible = true },
            new MouseDisasterIncidentEntry("MouseDisaster_BeggarSiege", "MouseDisaster_BeggarSiege_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Beggar, true, 300f) { IsOriginal = true },
            new MouseDisasterIncidentEntry("MouseDisaster_Aid_SimpleMeal", "MouseDisaster_Aid_SimpleMeal_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_Aid_FineMeal", "MouseDisaster_Aid_FineMeal_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_Aid_Medicine", "MouseDisaster_Aid_Medicine_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_Aid_Silver", "MouseDisaster_Aid_Silver_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_Aid_Baby", "MouseDisaster_Aid_Baby_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_Intel_Treasure_SimpleMeal", "MouseDisaster_Intel_Treasure_SimpleMeal_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_Intel_Treasure_Herbal", "MouseDisaster_Intel_Treasure_Herbal_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_Intel_Treasure_Silver", "MouseDisaster_Intel_Treasure_Silver_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_Intel_Structure_SimpleMeal", "MouseDisaster_Intel_Structure_SimpleMeal_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_Intel_Structure_Herbal", "MouseDisaster_Intel_Structure_Herbal_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_Intel_Structure_Silver", "MouseDisaster_Intel_Structure_Silver_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_Intel_Settlement_SimpleMeal", "MouseDisaster_Intel_Settlement_SimpleMeal_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_Intel_Settlement_Herbal", "MouseDisaster_Intel_Settlement_Herbal_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_Intel_Settlement_Silver", "MouseDisaster_Intel_Settlement_Silver_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_LaboringRefugees", "MouseDisaster_LaboringRefugees_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Beggar, false, 260f),
            new MouseDisasterIncidentEntry("MouseDisaster_StrongSiege", "MouseDisaster_StrongSiege_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Thief, false, 320f),
            new MouseDisasterIncidentEntry("MouseDisaster_Passersby", "MouseDisaster_Passersby_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Beggar, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_AirdropMistake", "MouseDisaster_AirdropMistake_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_MisguidedKinship", "MouseDisaster_MisguidedKinship_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 200f),
            new MouseDisasterIncidentEntry("MouseDisaster_GreatFamine", "MouseDisaster_GreatFamine_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Thief, false, 600f),
            new MouseDisasterIncidentEntry("MouseDisaster_CaravanMuggers", "MouseDisaster_CaravanMuggers_MenuLabel", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Caravan, MouseDisasterIncidentLegacyToggleGroup.Thief, false, 320f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueWanderers", "MouseDisaster_PlagueWanderers_MenuLabel", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Wild, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueAbandonedBabies", "MouseDisaster_PlagueAbandonedBabies_MenuLabel", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueTraderCaravan", "MouseDisaster_PlagueTraderCaravan_MenuLabel", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 350f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlaguePassersby", "MouseDisaster_PlaguePassersby_MenuLabel", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Beggar, false, 350f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueRefugees", "MouseDisaster_PlagueRefugees_MenuLabel", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Beggar, false, 350f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueOrphan", "MouseDisaster_PlagueOrphan_MenuLabel", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Wild, false, 200f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueBeggarGroup", "MouseDisaster_PlagueBeggarGroup_MenuLabel", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Beggar, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueThiefGroup", "MouseDisaster_PlagueThiefGroup_MenuLabel", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Thief, true, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueLaboringRefugees", "MouseDisaster_PlagueLaboringRefugees_MenuLabel", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Beggar, false, 260f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueStrongSiege", "MouseDisaster_PlagueStrongSiege_MenuLabel", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Thief, false, 320f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueAirdropMistake", "MouseDisaster_PlagueAirdropMistake_MenuLabel", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueMisguidedKinship", "MouseDisaster_PlagueMisguidedKinship_MenuLabel", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 200f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueGreatFamine", "MouseDisaster_PlagueGreatFamine_MenuLabel", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Thief, false, 600f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueRevenge", "MouseDisaster_PlagueRevenge_MenuLabel", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 100f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueCaravanMuggers", "MouseDisaster_PlagueCaravanMuggers_MenuLabel", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Caravan, MouseDisasterIncidentLegacyToggleGroup.Thief, false, 320f)
        };

        public static IReadOnlyList<MouseDisasterIncidentEntry> AllEntries => Entries;

        public static IReadOnlyList<MouseDisasterIncidentEntry> GetEntries(MouseDisasterIncidentCategory category)
        {
            return Entries.Where(entry => entry.Category == category).ToList();
        }

        public static bool IsKnownIncident(string defName)
        {
            return Entries.Any(entry => entry.DefName.Equals(defName, StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsIncidentEnabled(string defName, IEnumerable<string> disabledIncidentDefNames)
        {
            if (string.IsNullOrWhiteSpace(defName))
            {
                return false;
            }

            if (!IsKnownIncident(defName))
            {
                return true;
            }

            return !NormalizeDisabledSet(disabledIncidentDefNames).Contains(defName);
        }

        public static bool ShouldAllowDebugTrigger(string defName, IEnumerable<string> disabledIncidentDefNames)
        {
            return IsIncidentEnabled(defName, disabledIncidentDefNames);
        }

        public static int CountEnabledIncidents(IEnumerable<string> disabledIncidentDefNames)
        {
            HashSet<string> disabled = NormalizeDisabledSet(disabledIncidentDefNames);
            return Entries.Count(entry => !disabled.Contains(entry.DefName));
        }

        public static List<string> BuildMigratedDisabledIncidentDefNames(
            bool enableWildIncidents,
            bool enableThiefIncidents,
            bool enableBeggarIncidents,
            IEnumerable<string> existingDisabledIncidentDefNames)
        {
            HashSet<string> disabled = NormalizeDisabledSet(existingDisabledIncidentDefNames);
            for (int i = 0; i < Entries.Count; i++)
            {
                MouseDisasterIncidentEntry entry = Entries[i];
                if (!entry.ControlledByLegacyToggle)
                {
                    continue;
                }

                if ((!enableWildIncidents && entry.LegacyToggleGroup == MouseDisasterIncidentLegacyToggleGroup.Wild) ||
                    (!enableThiefIncidents && entry.LegacyToggleGroup == MouseDisasterIncidentLegacyToggleGroup.Thief) ||
                    (!enableBeggarIncidents && entry.LegacyToggleGroup == MouseDisasterIncidentLegacyToggleGroup.Beggar))
                {
                    disabled.Add(entry.DefName);
                }
            }

            return Entries
                .Select(entry => entry.DefName)
                .Where(disabled.Contains)
                .ToList();
        }

        private static HashSet<string> NormalizeDisabledSet(IEnumerable<string> disabledIncidentDefNames)
        {
            return new HashSet<string>(
                disabledIncidentDefNames?
                    .Where(defName => !string.IsNullOrWhiteSpace(defName))
                    .Select(defName => defName.Trim()) ??
                Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);
        }
    }
}
