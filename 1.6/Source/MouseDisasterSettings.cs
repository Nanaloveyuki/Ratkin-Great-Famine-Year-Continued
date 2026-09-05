using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace MouseDisaster
{
    public enum PrisonerScavengePoisonMode
    {
        Safe = 0,
        Normal = 1,
        Abuse = 2
    }

    public class MouseDisasterSettings : ModSettings
    {
        public const int MinRatkinAge = 30;
        public const int MaxRatkinAge = 80;
        public const float MinAgeDiseaseMultiplier = 0.5f;
        public const float MaxAgeDiseaseMultiplier = 3f;
        public const float MinChaosPregnancyChancePercent = 0f;
        public const float MaxChaosPregnancyChancePercent = 20f;
        public const int MinChaosPregnancyIntervalTicks = 60;
        public const int MaxChaosPregnancyIntervalTicks = 15000;
        public const float MinRatEggTraitGenerationChance = 0f;
        public const float MaxRatEggTraitGenerationChance = 1f;
        public const float MinFamineYearChancePercent = 0f;
        public const float MaxFamineYearChancePercent = 100f;
        public const float MinFamineYearDisasterBonusPercent = 0f;
        public const float MaxFamineYearDisasterBonusPercent = 100f;
        public const float ScavengeToxicBuildupSafe = 0f;
        public const float ScavengeToxicBuildupNormal = 0.02f;
        public const float ScavengeToxicBuildupAbuse = 0.05f;

        public bool enableNewContent = true;
        public bool enableAgeCapAdjustment = true;
        public bool enableWildRatkinIncidents = true;
        public bool enableThiefIncidents = true;
        public bool enableBeggarIncidents = true;
        public bool incidentToggleMigrationApplied = false;
        public bool enableGnawing = true;
        public bool enablePrisonerScavenge = true;
        public bool prisonerScavengeMigrationApplied = false;
        public bool enableFloatingText = true;
        public bool enablePrisonerScavengeDebugLog = false;
        public bool enableExperimentalTailBite = false;
        public PrisonerScavengePoisonMode prisonerScavengePoisonMode = PrisonerScavengePoisonMode.Normal;
        public bool enableChaosRoomPregnancy = true;
        public bool enableExperimentalIdentityInheritance = false;
        public float chaosPregnancyChancePercent = 2f;
        public int chaosPregnancyCheckIntervalTicks = 3000;
        public int broadcastHopeCooldownDays = 3;
        public bool enableFamineYearSystem = false;
        public float famineYearChancePercent = 40f;
        public float famineYearDisasterBonusPercent = 20f;
        public bool enableRatEggTraitsBridge = true;
        public float ratEggTraitGenerationChance = 0.5f;
        public List<string> disabledIncidentDefNames = new List<string>();
        public int maxRatkinAge = 50;
        public float ageDiseaseMultiplier = 1f;

        public void ResetToDefaults()
        {
            enableNewContent = true;
            enableAgeCapAdjustment = true;
            enableWildRatkinIncidents = true;
            enableThiefIncidents = true;
            enableBeggarIncidents = true;
            incidentToggleMigrationApplied = true;
            enableGnawing = true;
            enablePrisonerScavenge = true;
            prisonerScavengeMigrationApplied = true;
            enableFloatingText = true;
            enablePrisonerScavengeDebugLog = false;
            enableExperimentalTailBite = false;
            prisonerScavengePoisonMode = PrisonerScavengePoisonMode.Normal;
            enableChaosRoomPregnancy = true;
            enableExperimentalIdentityInheritance = false;
            chaosPregnancyChancePercent = 2f;
            chaosPregnancyCheckIntervalTicks = 3000;
            broadcastHopeCooldownDays = 3;
            enableFamineYearSystem = false;
            famineYearChancePercent = 40f;
            famineYearDisasterBonusPercent = 20f;
            enableRatEggTraitsBridge = true;
            ratEggTraitGenerationChance = 0.5f;
            disabledIncidentDefNames = new List<string>();
            maxRatkinAge = 50;
            ageDiseaseMultiplier = 1f;
            ClampValues();
        }

        public bool IsIncidentEnabled(string defName)
        {
            return MouseDisasterIncidentCatalog.IsIncidentEnabled(defName, disabledIncidentDefNames);
        }

        public void SetIncidentEnabled(string defName, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(defName))
            {
                return;
            }

            disabledIncidentDefNames ??= new List<string>();
            disabledIncidentDefNames.RemoveAll(entry => entry.Equals(defName, System.StringComparison.OrdinalIgnoreCase));
            if (!enabled)
            {
                disabledIncidentDefNames.Add(defName);
            }

            NormalizeIncidentToggleState();
        }

        public void ClampValues()
        {
            maxRatkinAge = Mathf.Clamp(maxRatkinAge, MinRatkinAge, MaxRatkinAge);
            ageDiseaseMultiplier = Mathf.Clamp(ageDiseaseMultiplier, MinAgeDiseaseMultiplier, MaxAgeDiseaseMultiplier);
            chaosPregnancyChancePercent = Mathf.Clamp(chaosPregnancyChancePercent, MinChaosPregnancyChancePercent, MaxChaosPregnancyChancePercent);
            chaosPregnancyCheckIntervalTicks = Mathf.Clamp(chaosPregnancyCheckIntervalTicks, MinChaosPregnancyIntervalTicks, MaxChaosPregnancyIntervalTicks);
            broadcastHopeCooldownDays = MouseDisasterBroadcastHopePolicy.NormalizeCooldownDays(broadcastHopeCooldownDays);
            famineYearChancePercent = Mathf.Clamp(famineYearChancePercent, MinFamineYearChancePercent, MaxFamineYearChancePercent);
            famineYearDisasterBonusPercent = Mathf.Clamp(famineYearDisasterBonusPercent, MinFamineYearDisasterBonusPercent, MaxFamineYearDisasterBonusPercent);
            ratEggTraitGenerationChance = Mathf.Clamp(ratEggTraitGenerationChance, MinRatEggTraitGenerationChance, MaxRatEggTraitGenerationChance);
            if (!System.Enum.IsDefined(typeof(PrisonerScavengePoisonMode), prisonerScavengePoisonMode))
            {
                prisonerScavengePoisonMode = PrisonerScavengePoisonMode.Normal;
            }
        }

        public float GetPrisonerScavengeToxicBuildupPerEat()
        {
            switch (prisonerScavengePoisonMode)
            {
                case PrisonerScavengePoisonMode.Safe:
                    return ScavengeToxicBuildupSafe;
                case PrisonerScavengePoisonMode.Abuse:
                    return ScavengeToxicBuildupAbuse;
                default:
                    return ScavengeToxicBuildupNormal;
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableNewContent, "enableNewContent", true);
            Scribe_Values.Look(ref enableAgeCapAdjustment, "enableAgeCapAdjustment", true);
            Scribe_Values.Look(ref enableWildRatkinIncidents, "enableWildRatkinIncidents", true);
            Scribe_Values.Look(ref enableThiefIncidents, "enableThiefIncidents", true);
            Scribe_Values.Look(ref enableBeggarIncidents, "enableBeggarIncidents", true);
            Scribe_Values.Look(ref incidentToggleMigrationApplied, "incidentToggleMigrationApplied", false);
            Scribe_Values.Look(ref enableGnawing, "enableGnawing", true);
            Scribe_Values.Look(ref enablePrisonerScavenge, "enablePrisonerScavenge", true);
            Scribe_Values.Look(ref prisonerScavengeMigrationApplied, "prisonerScavengeMigrationApplied", false);
            Scribe_Values.Look(ref enableFloatingText, "enableFloatingText", true);
            Scribe_Values.Look(ref enablePrisonerScavengeDebugLog, "enablePrisonerScavengeDebugLog", false);
            Scribe_Values.Look(ref enableExperimentalTailBite, "enableExperimentalTailBite", false);
            int poisonModeRaw = (int)prisonerScavengePoisonMode;
            Scribe_Values.Look(ref poisonModeRaw, "prisonerScavengePoisonMode", (int)PrisonerScavengePoisonMode.Normal);
            prisonerScavengePoisonMode = System.Enum.IsDefined(typeof(PrisonerScavengePoisonMode), poisonModeRaw)
                ? (PrisonerScavengePoisonMode)poisonModeRaw
                : PrisonerScavengePoisonMode.Normal;
            Scribe_Values.Look(ref enableChaosRoomPregnancy, "enableChaosRoomPregnancy", true);
            Scribe_Values.Look(ref enableExperimentalIdentityInheritance, "enableExperimentalIdentityInheritance", false);
            Scribe_Values.Look(ref chaosPregnancyChancePercent, "chaosPregnancyChancePercent", 2f);
            Scribe_Values.Look(ref chaosPregnancyCheckIntervalTicks, "chaosPregnancyCheckIntervalTicks", 3000);
            Scribe_Values.Look(ref broadcastHopeCooldownDays, "broadcastHopeCooldownDays", 3);
            Scribe_Values.Look(ref enableFamineYearSystem, "enableFamineYearSystem", false);
            Scribe_Values.Look(ref famineYearChancePercent, "famineYearChancePercent", 40f);
            Scribe_Values.Look(ref famineYearDisasterBonusPercent, "famineYearDisasterBonusPercent", 20f);
            Scribe_Values.Look(ref enableRatEggTraitsBridge, "enableRatEggTraitsBridge", true);
            Scribe_Values.Look(ref ratEggTraitGenerationChance, "ratEggTraitGenerationChance", 0.5f);
            Scribe_Collections.Look(ref disabledIncidentDefNames, "disabledIncidentDefNames", LookMode.Value);
            Scribe_Values.Look(ref maxRatkinAge, "maxRatkinAge", 50);
            Scribe_Values.Look(ref ageDiseaseMultiplier, "ageDiseaseMultiplier", 1f);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && !prisonerScavengeMigrationApplied)
            {
                enablePrisonerScavenge = true;
                prisonerScavengeMigrationApplied = true;
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit && !incidentToggleMigrationApplied)
            {
                disabledIncidentDefNames = MouseDisasterIncidentCatalog.BuildMigratedDisabledIncidentDefNames(
                    enableWildRatkinIncidents,
                    enableThiefIncidents,
                    enableBeggarIncidents,
                    disabledIncidentDefNames);
                enableWildRatkinIncidents = true;
                enableThiefIncidents = true;
                enableBeggarIncidents = true;
                incidentToggleMigrationApplied = true;
            }

            NormalizeIncidentToggleState();
            ClampValues();
        }

        private void NormalizeIncidentToggleState()
        {
            disabledIncidentDefNames ??= new List<string>();
            disabledIncidentDefNames = disabledIncidentDefNames
                .Where(defName => !string.IsNullOrWhiteSpace(defName))
                .Select(defName => defName.Trim())
                .Distinct(System.StringComparer.OrdinalIgnoreCase)
                .Where(MouseDisasterIncidentCatalog.IsKnownIncident)
                .OrderBy(defName => defName)
                .ToList();
        }
    }
}
