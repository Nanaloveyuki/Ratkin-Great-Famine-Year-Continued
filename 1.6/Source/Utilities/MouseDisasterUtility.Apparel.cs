using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace MouseDisaster
{
    public static partial class MouseDisasterUtility
    {

        private static void AssignDisasterApparel(Pawn pawn, DevelopmentalStage stage)
        {
            if (pawn?.apparel == null)
            {
                return;
            }

            if (stage.Adult())
            {
                EnsureAdultDisasterApparel(pawn);
                return;
            }

            for (int index = pawn.apparel.WornApparel.Count - 1; index >= 0; index--)
            {
                Apparel current = pawn.apparel.WornApparel[index];
                pawn.apparel.Remove(current);
                current.Destroy();
            }

            if (stage == DevelopmentalStage.Baby && TryAssignSeasonalBabyDisasterApparel(pawn))
            {
                return;
            }

            List<ThingDef> apparelPool;
            if (stage == DevelopmentalStage.Baby)
            {
                apparelPool = GetRestrictedApparelDefs(BabyRestrictedApparelDefNames);
            }
            else
            {
                apparelPool = GetRestrictedApparelDefs(ChildRestrictedApparelDefNames);
            }

            if (apparelPool.Count == 0)
            {
                return;
            }

            int targetCount = Mathf.Min(GetDesiredApparelCount(stage), apparelPool.Count);
            if (targetCount <= 0)
            {
                return;
            }

            int equipped = 0;
            List<ThingDef> randomizedPool = apparelPool.InRandomOrder().ToList();
            for (int attempt = 0; attempt < randomizedPool.Count && equipped < targetCount; attempt++)
            {
                if (TryWearRandomizedApparel(pawn, randomizedPool[attempt]))
                {
                    equipped++;
                }
            }
        }

        private static bool TryAssignSeasonalBabyDisasterApparel(Pawn pawn)
        {
            if (pawn == null || pawn.DevelopmentalStage != DevelopmentalStage.Baby)
            {
                return false;
            }

            switch (ResolveCurrentSeasonForDisasterPawn(pawn))
            {
                case Season.Summer:
                case Season.PermanentSummer:
                    return TryWearSpecificDisasterApparel(pawn, KidTribalApparelDefName) |
                           TryWearSpecificDisasterApparel(pawn, SunHatApparelDefName);
                case Season.Winter:
                case Season.PermanentWinter:
                    return TryWearSpecificDisasterApparel(pawn, BabyOnesieApparelDefName) |
                           TryWearSpecificDisasterApparel(pawn, WarmerHatApparelDefName);
                case Season.Spring:
                case Season.Fall:
                default:
                    return TryWearSpecificDisasterApparel(pawn, BabyOnesieApparelDefName);
            }
        }

        private static Season ResolveCurrentSeasonForDisasterPawn(Pawn pawn)
        {
            Map map = pawn?.MapHeld ?? Find.AnyPlayerHomeMap ?? Find.CurrentMap;
            if (map == null || !map.Tile.Valid)
            {
                return Season.Undefined;
            }

            return GenLocalDate.Season(map.Tile);
        }

        private static bool TryWearSpecificDisasterApparel(Pawn pawn, string apparelDefName)
        {
            if (pawn?.apparel == null || apparelDefName.NullOrEmpty())
            {
                return false;
            }

            ThingDef apparelDef = DefDatabase<ThingDef>.GetNamedSilentFail(apparelDefName);
            if (apparelDef == null || !apparelDef.IsApparel || !apparelDef.MadeFromStuff)
            {
                return false;
            }

            return TryWearRandomizedApparel(pawn, apparelDef);
        }

        private static void EnsureAdultDisasterApparel(Pawn pawn)
        {
            if (pawn?.apparel == null)
            {
                return;
            }

            for (int i = pawn.apparel.WornApparel.Count - 1; i >= 0; i--)
            {
                Apparel worn = pawn.apparel.WornApparel[i];
                pawn.apparel.Remove(worn);
                worn.Destroy();
            }

            List<ThingDef> adultPool = GetAdultApparelDefs(pawn);
            if (adultPool.Count == 0)
            {
                return;
            }

            int targetCount = Mathf.Clamp(RollWeightedCount(0.02f, 0.16f, 0.34f, 0.30f, 0.18f), 1, 4);
            int equipped = 0;
            List<ThingDef> randomizedPool = adultPool.InRandomOrder().ToList();
            for (int i = 0; i < randomizedPool.Count && equipped < targetCount; i++)
            {
                if (TryWearRandomizedApparel(pawn, randomizedPool[i]))
                {
                    equipped++;
                }
            }
        }

        private static bool TryWearRandomizedApparel(Pawn pawn, ThingDef apparelDef)
        {
            if (pawn?.apparel == null || apparelDef == null)
            {
                return false;
            }

            ThingDef stuff = TryResolvePreferredApparelStuff(apparelDef);
            if (stuff == null)
            {
                return false;
            }

            Apparel apparel = ThingMaker.MakeThing(apparelDef, stuff) as Apparel;
            if (apparel == null ||
                !apparel.PawnCanWear(pawn, ignoreGender: true) ||
                !ApparelUtility.HasPartsToWear(pawn, apparel.def) ||
                !pawn.apparel.CanWearWithoutDroppingAnything(apparel.def))
            {
                apparel?.Destroy();
                return false;
            }

            ApplyRandomizedApparelQuality(apparel);
            apparel.WornByCorpse = Rand.Chance(0.25f);
            pawn.apparel.Wear(apparel, dropReplacedApparel: false);
            ApplyRandomizedApparelDurability(apparel);
            return true;
        }

        private static int GetDesiredApparelCount(DevelopmentalStage stage)
        {
            if (stage == DevelopmentalStage.Baby)
            {
                return RollWeightedCount(0.30f, 0.50f, 0.18f, 0.02f);
            }

            if (stage == DevelopmentalStage.Child)
            {
                return RollWeightedCount(0.16f, 0.34f, 0.30f, 0.16f, 0.04f);
            }

            return 0;
        }

        private static int RollWeightedCount(params float[] weights)
        {
            if (weights == null || weights.Length == 0)
            {
                return 0;
            }

            float total = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                total += Mathf.Max(0f, weights[i]);
            }

            if (total <= 0f)
            {
                return 0;
            }

            float roll = Rand.Value * total;
            float cumulative = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += Mathf.Max(0f, weights[i]);
                if (roll <= cumulative)
                {
                    return i;
                }
            }

            return weights.Length - 1;
        }

        private static void ResolvePreferredApparelStuffDefs()
        {
            if (apparelStuffResolved)
            {
                return;
            }

            apparelStuffResolved = true;
            clothStuffDef = DefDatabase<ThingDef>.GetNamedSilentFail(ClothStuffDefName);
            humanleatherStuffDef = DefDatabase<ThingDef>.GetNamedSilentFail(HumanleatherStuffDefName);
        }

        private static ThingDef TryResolvePreferredApparelStuff(ThingDef apparelDef)
        {
            if (apparelDef == null)
            {
                return null;
            }

            ResolvePreferredApparelStuffDefs();
            if (!ApparelPreferredStuffCache.TryGetValue(apparelDef, out List<ThingDef> options))
            {
                options = GenStuff.AllowedStuffsFor(apparelDef)
                    .Where(stuff => stuff == clothStuffDef || stuff == humanleatherStuffDef)
                    .Distinct()
                    .ToList();
                ApparelPreferredStuffCache[apparelDef] = options;
            }

            if (options == null || options.Count == 0)
            {
                return null;
            }

            return options.RandomElementByWeight(stuff => stuff == humanleatherStuffDef ? 0.35f : 0.65f);
        }

        private static List<ThingDef> GetRestrictedApparelDefs(IEnumerable<string> defNames)
        {
            if (defNames == null)
            {
                return new List<ThingDef>();
            }

            return defNames
                .Select(DefDatabase<ThingDef>.GetNamedSilentFail)
                .Where(def => def != null && def.IsApparel && def.MadeFromStuff && TryResolvePreferredApparelStuff(def) != null)
                .Distinct()
                .ToList();
        }

        private static List<ThingDef> GetAdultApparelDefs(Pawn pawn)
        {
            if (pawn?.kindDef == null)
            {
                return new List<ThingDef>();
            }

            if (!adultApparelDefsResolved)
            {
                adultApparelDefsResolved = true;
                HashSet<string> restricted = new HashSet<string>(BabyRestrictedApparelDefNames.Concat(ChildRestrictedApparelDefNames));
                adultApparelDefsCache = DefDatabase<ThingDef>.AllDefsListForReading
                    .Where(def =>
                        MouseDisasterGenerationPolicy.AllowsApparel(def) &&
                        def.IsApparel &&
                        def.MadeFromStuff &&
                        def.apparel != null &&
                        def.apparel.countsAsClothingForNudity &&
                        !restricted.Contains(def.defName) &&
                        TryResolvePreferredApparelStuff(def) != null)
                    .Distinct()
                    .ToList();
            }

            if (adultApparelDefsCache == null || adultApparelDefsCache.Count == 0)
            {
                return new List<ThingDef>();
            }

            return adultApparelDefsCache
                .Where(def => ApparelUtility.HasPartsToWear(pawn, def))
                .ToList();
        }

        private static void ApplyRandomizedApparelQuality(Apparel apparel)
        {
            CompQuality qualityComp = apparel?.TryGetComp<CompQuality>();
            if (qualityComp == null)
            {
                return;
            }

            float roll = Rand.Value;
            QualityCategory quality;
            if (roll < 0.35f)
            {
                quality = QualityCategory.Awful;
            }
            else if (roll < 0.85f)
            {
                quality = QualityCategory.Poor;
            }
            else
            {
                quality = QualityCategory.Normal;
            }
            qualityComp.SetQuality(quality, ArtGenerationContext.Outsider);
        }

        private static void ApplyRandomizedApparelDurability(Apparel apparel)
        {
            if (apparel == null || apparel.MaxHitPoints <= 1)
            {
                return;
            }

            int randomHitPoints = Mathf.RoundToInt(apparel.MaxHitPoints * Rand.Range(DisasterApparelMinDurability, DisasterApparelMaxDurability));
            apparel.HitPoints = Mathf.Clamp(randomHitPoints, 1, apparel.MaxHitPoints);
        }
    }
}
