using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MouseDisaster
{
    public class MouseDisasterMod : Mod
    {
        public static MouseDisasterSettings Settings;
        private Vector2 settingsScrollPosition;

        public MouseDisasterMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<MouseDisasterSettings>();
        }

        public override string SettingsCategory()
        {
            return "MouseDisaster_ModName".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            float incidentSectionHeight = MouseDisasterIncidentCatalog.AllEntries.Count * 30f;
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, Mathf.Max(1640f + incidentSectionHeight, inRect.height + 620f + incidentSectionHeight));
            Widgets.BeginScrollView(inRect, ref settingsScrollPosition, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(new Rect(0f, 0f, viewRect.width - 16f, viewRect.height));

            DrawHeader(listing);

            DrawSectionTitle(listing, "MouseDisaster_Settings_Section_General");
            DrawCheckbox(listing, "MouseDisaster_Settings_EnableGnawing", ref Settings.enableGnawing, "MouseDisaster_Settings_EnableGnawing_Tooltip");
            DrawCheckbox(listing, "MouseDisaster_Settings_EnableExperimentalTailBite", ref Settings.enableExperimentalTailBite, "MouseDisaster_Settings_EnableExperimentalTailBite_Tooltip");
            DrawDaysSlider(listing, "MouseDisaster_Settings_BroadcastHopeCooldown", ref Settings.broadcastHopeCooldownDays, 0, 10);
            DrawIncidentSection(listing);

            DrawSectionTitle(listing, "MouseDisaster_Settings_Section_FamineYear");
            DrawCheckbox(listing, "MouseDisaster_Settings_EnableFamineYearSystem", ref Settings.enableFamineYearSystem, "MouseDisaster_Settings_EnableFamineYearSystem_Tooltip");
            if (Settings.enableFamineYearSystem)
            {
                DrawPercentSlider(listing, "MouseDisaster_Settings_FamineYearChance", ref Settings.famineYearChancePercent, MouseDisasterSettings.MinFamineYearChancePercent, MouseDisasterSettings.MaxFamineYearChancePercent);
                DrawPercentSlider(listing, "MouseDisaster_Settings_FamineYearDisasterBonus", ref Settings.famineYearDisasterBonusPercent, MouseDisasterSettings.MinFamineYearDisasterBonusPercent, MouseDisasterSettings.MaxFamineYearDisasterBonusPercent);
            }
            else
            {
                listing.Label("MouseDisaster_Settings_FamineYearDisabledHint".Translate());
            }

            DrawSectionTitle(listing, "MouseDisaster_Settings_Section_Age");
            DrawCheckbox(listing, "MouseDisaster_Settings_EnableAgeCap", ref Settings.enableAgeCapAdjustment, "MouseDisaster_Settings_EnableAgeCap_Tooltip");
            if (Settings.enableAgeCapAdjustment)
            {
                listing.Label("MouseDisaster_Settings_MaxAge".Translate(Settings.maxRatkinAge));
                Settings.maxRatkinAge = Mathf.RoundToInt(listing.Slider(Settings.maxRatkinAge, MouseDisasterSettings.MinRatkinAge, MouseDisasterSettings.MaxRatkinAge));

                DrawDecimalSlider(listing, "MouseDisaster_Settings_DiseaseMult", ref Settings.ageDiseaseMultiplier, MouseDisasterSettings.MinAgeDiseaseMultiplier, MouseDisasterSettings.MaxAgeDiseaseMultiplier, "0.0");
            }
            else
            {
                listing.Label("MouseDisaster_Settings_AgeDisabledHint".Translate());
            }

            DrawSectionTitle(listing, "MouseDisaster_Settings_Section_Genes");
            DrawCheckbox(listing, "MouseDisaster_Settings_ChaosPregnancyEnabled", ref Settings.enableChaosRoomPregnancy, "MouseDisaster_Settings_ChaosPregnancyEnabled_Tooltip");
            if (Settings.enableChaosRoomPregnancy)
            {
                DrawCheckbox(listing, "MouseDisaster_Settings_EnableExperimentalIdentityInheritance", ref Settings.enableExperimentalIdentityInheritance, "MouseDisaster_Settings_EnableExperimentalIdentityInheritance_Tooltip");
                DrawDecimalSlider(listing, "MouseDisaster_Settings_ChaosChance", ref Settings.chaosPregnancyChancePercent, MouseDisasterSettings.MinChaosPregnancyChancePercent, MouseDisasterSettings.MaxChaosPregnancyChancePercent, "0.0");
                DrawSecondsSlider(listing, "MouseDisaster_Settings_ChaosInterval", ref Settings.chaosPregnancyCheckIntervalTicks, MouseDisasterSettings.MinChaosPregnancyIntervalTicks, MouseDisasterSettings.MaxChaosPregnancyIntervalTicks);
            }
            else
            {
                listing.Label("MouseDisaster_Settings_ChaosDisabledHint".Translate());
            }

            DrawSectionTitle(listing, "MouseDisaster_Settings_Section_Compat");
            DrawCheckbox(listing, "MouseDisaster_Settings_RatEggTraitBridge", ref Settings.enableRatEggTraitsBridge, "MouseDisaster_Settings_RatEggTraitBridge_Tooltip");
            if (Settings.enableRatEggTraitsBridge)
            {
                DrawFactorAsPercentSlider(listing, "MouseDisaster_Settings_RatEggTraitChance", ref Settings.ratEggTraitGenerationChance, MouseDisasterSettings.MinRatEggTraitGenerationChance, MouseDisasterSettings.MaxRatEggTraitGenerationChance);
            }
            else
            {
                listing.Label("MouseDisaster_Settings_CompatDisabledHint".Translate());
            }

            DrawSectionTitle(listing, "MouseDisaster_Settings_Section_DisplayDebug");
            DrawCheckbox(listing, "MouseDisaster_Settings_EnableFloatingText", ref Settings.enableFloatingText, "MouseDisaster_Settings_EnableFloatingText_Tooltip");

            listing.Gap(10f);
            if (listing.ButtonText("MouseDisaster_Settings_ResetDefaults".Translate()))
            {
                Settings.ResetToDefaults();
            }

            listing.GapLine();
            listing.Label("MouseDisaster_Settings_ImplNote".Translate());

            listing.End();
            Widgets.EndScrollView();
            Settings.ClampValues();
            Settings.Write();
        }

        private static void DrawHeader(Listing_Standard listing)
        {
            int enabledIncidentCount = MouseDisasterIncidentCatalog.CountEnabledIncidents(Settings.disabledIncidentDefNames);
            int totalIncidentCount = MouseDisasterIncidentCatalog.AllEntries.Count;
            string gnawingStatus = Settings.enableGnawing ? "MouseDisaster_Settings_Status_On".TranslateSimple() : "MouseDisaster_Settings_Status_Off".TranslateSimple();
            string tailBiteStatus = Settings.enableExperimentalTailBite ? "MouseDisaster_Settings_Status_On".TranslateSimple() : "MouseDisaster_Settings_Status_Off".TranslateSimple();
            listing.Label("事件: " + enabledIncidentCount + "/" + totalIncidentCount + "  啃食: " + gnawingStatus + "  咬尾巴: " + tailBiteStatus);
            listing.GapLine();
        }

        private static void DrawIncidentSection(Listing_Standard listing)
        {
            DrawSectionTitle(listing, "MouseDisaster_Settings_Section_Incidents");
            DrawIncidentCategorySection(listing, MouseDisasterIncidentCategory.MouseDisaster, "MouseDisaster_Settings_IncidentCategory_MouseDisaster");
            DrawIncidentCategorySection(listing, MouseDisasterIncidentCategory.Plague, "MouseDisaster_Settings_IncidentCategory_Plague");
        }

        private static void DrawIncidentCategorySection(Listing_Standard listing, MouseDisasterIncidentCategory category, string titleKey)
        {
            listing.Label(titleKey.Translate());
            listing.Gap(2f);
            IReadOnlyList<MouseDisasterIncidentEntry> entries = MouseDisasterIncidentCatalog.GetEntries(category);
            for (int i = 0; i < entries.Count; i++)
            {
                MouseDisasterIncidentEntry entry = entries[i];
                bool enabled = Settings.IsIncidentEnabled(entry.DefName);
                bool newEnabled = enabled;
                listing.CheckboxLabeled(entry.DisplayLabel, ref newEnabled, entry.DefName);
                if (newEnabled != enabled)
                {
                    Settings.SetIncidentEnabled(entry.DefName, newEnabled);
                }
                listing.Gap(2f);
            }

            listing.GapLine();
        }

        private static void DrawPercentSlider(Listing_Standard listing, string labelKey, ref float value, float min, float max)
        {
            listing.Label(labelKey.Translate(value.ToString("0")));
            value = listing.Slider(value, min, max);
        }

        private static void DrawDecimalSlider(Listing_Standard listing, string labelKey, ref float value, float min, float max, string format)
        {
            listing.Label(labelKey.Translate(value.ToString(format)));
            value = listing.Slider(value, min, max);
        }

        private static void DrawSecondsSlider(Listing_Standard listing, string labelKey, ref int ticksValue, int min, int max)
        {
            float seconds = ticksValue / 60f;
            listing.Label(labelKey.Translate(seconds.ToString("0.0")));
            ticksValue = Mathf.RoundToInt(listing.Slider(ticksValue, min, max));
        }

        private static void DrawDaysSlider(Listing_Standard listing, string labelKey, ref int daysValue, int min, int max)
        {
            listing.Label(labelKey.Translate(daysValue.ToString()));
            daysValue = Mathf.RoundToInt(listing.Slider(daysValue, min, max));
        }

        private static void DrawFactorAsPercentSlider(Listing_Standard listing, string labelKey, ref float value, float min, float max)
        {
            listing.Label(labelKey.Translate((value * 100f).ToString("0")));
            value = listing.Slider(value, min, max);
        }

        private static void DrawSectionTitle(Listing_Standard listing, string key)
        {
            listing.Gap(4f);
            listing.Label(key.Translate());
            listing.GapLine();
        }

        private static void DrawCheckbox(Listing_Standard listing, string labelKey, ref bool value, string tooltipKey)
        {
            listing.CheckboxLabeled(labelKey.Translate(), ref value, tooltipKey.Translate());
            listing.Gap(2f);
        }
    }
}
