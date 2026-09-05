using System;
using System.Collections.Generic;
using System.Linq;

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

        public string DisplayLabel { get; }

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
            DisplayLabel = displayLabel;
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
            new MouseDisasterIncidentEntry("MouseDisaster_LargeRefugeeWave", "大型流民冲击", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Beggar, true, 700f),
            new MouseDisasterIncidentEntry("MouseDisaster_AbandonedRatkinChildren", "鼠蛋遗弃", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 400f),
            new MouseDisasterIncidentEntry("MouseDisaster_ShatteredMother", "耗子分妈", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 400f),
            new MouseDisasterIncidentEntry("MouseDisaster_BeggarFamily", "乞讨的灾荒鼠妈", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Beggar, true, 200f),
            new MouseDisasterIncidentEntry("MouseDisaster_BeggarGroup", "乞讨的鼠族队伍", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Beggar, true, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_ThiefRatkinGroup", "偷窃的鼠族", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Thief, true, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_ThiefRatkinChildGroup", "偷窃的鼠蛋", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Thief, true, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_WildRatkinWandersIn", "游荡的野生鼠族", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Wild, true, 150f),
            new MouseDisasterIncidentEntry("MouseDisaster_WildRatkinChildWandersIn", "游荡的野生鼠蛋", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Wild, true, 150f),
            new MouseDisasterIncidentEntry("MouseDisaster_WildRatkinGroupWandersIn", "游荡的野生鼠群", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Wild, true, 400f),
            new MouseDisasterIncidentEntry("MouseDisaster_FamineRefugees", "灾荒逃难者", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 350f),
            new MouseDisasterIncidentEntry("MouseDisaster_RatkinTraderCaravan", "流民商队", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 350f),
            new MouseDisasterIncidentEntry("MouseDisaster_ChildExchange", "易子而食", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 200f),
            new MouseDisasterIncidentEntry("MouseDisaster_BeggarSiege", "鼠灾围攻", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Beggar, true, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_Aid_SimpleMeal", "难民接济（简单食物）", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_Aid_FineMeal", "难民接济（精致食物）", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_Aid_Medicine", "难民接济（药品）", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_Aid_Silver", "难民接济（白银）", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_Aid_Baby", "难民接济（婴儿）", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_Intel_Treasure_SimpleMeal", "情报交易（宝藏-简单食物）", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_Intel_Treasure_Herbal", "情报交易（宝藏-草药）", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_Intel_Treasure_Silver", "情报交易（宝藏-白银）", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_Intel_Structure_SimpleMeal", "情报交易（建筑群-简单食物）", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_Intel_Structure_Herbal", "情报交易（建筑群-草药）", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_Intel_Structure_Silver", "情报交易（建筑群-白银）", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_Intel_Settlement_SimpleMeal", "情报交易（小居住点-简单食物）", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_Intel_Settlement_Herbal", "情报交易（小居住点-草药）", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_Intel_Settlement_Silver", "情报交易（小居住点-白银）", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_LaboringRefugees", "待产流民", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Beggar, false, 260f),
            new MouseDisasterIncidentEntry("MouseDisaster_StrongSiege", "强势围攻", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Thief, false, 320f),
            new MouseDisasterIncidentEntry("MouseDisaster_Passersby", "鼠灾路过", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Beggar, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_AirdropMistake", "空投失误", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_MisguidedKinship", "错误认知", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 200f),
            new MouseDisasterIncidentEntry("MouseDisaster_GreatFamine", "大灾荒", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Thief, false, 600f),
            new MouseDisasterIncidentEntry("MouseDisaster_CaravanMuggers", "流民抢夺商队（商队）", MouseDisasterIncidentCategory.MouseDisaster, MouseDisasterIncidentTargetKind.Caravan, MouseDisasterIncidentLegacyToggleGroup.Thief, false, 320f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueWanderers", "鼠疫游荡", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Wild, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueAbandonedBabies", "鼠疫托孤", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueTraderCaravan", "鼠疫商队", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 350f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlaguePassersby", "鼠疫逃难者", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Beggar, false, 350f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueRefugees", "鼠疫流亡者", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Beggar, false, 350f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueOrphan", "鼠疫孤儿", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Wild, false, 200f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueBeggarGroup", "鼠疫流民", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Beggar, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueThiefGroup", "鼠疫小偷", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Thief, true, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueLaboringRefugees", "鼠疫待产流民", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Beggar, false, 260f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueStrongSiege", "鼠疫强势围攻", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Thief, false, 320f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueAirdropMistake", "鼠疫空投失误", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 300f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueMisguidedKinship", "鼠疫错误认知", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 200f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueGreatFamine", "鼠疫大灾荒", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.Thief, false, 600f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueRevenge", "鼠疫报复", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Map, MouseDisasterIncidentLegacyToggleGroup.None, false, 100f),
            new MouseDisasterIncidentEntry("MouseDisaster_PlagueCaravanMuggers", "鼠疫流民抢夺商队（商队）", MouseDisasterIncidentCategory.Plague, MouseDisasterIncidentTargetKind.Caravan, MouseDisasterIncidentLegacyToggleGroup.Thief, false, 320f)
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
