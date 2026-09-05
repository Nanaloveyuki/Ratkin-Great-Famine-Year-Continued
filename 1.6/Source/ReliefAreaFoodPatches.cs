using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MouseDisaster
{
    [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.WillEat), new[] { typeof(Pawn), typeof(Thing), typeof(Pawn), typeof(bool), typeof(bool) })]
    public static class MouseDisasterReliefAreaWillEatPatch
    {
        public static void Postfix(Pawn p, Thing food, ref bool __result)
        {
            if (__result && MouseDisasterUtility.ShouldBlockColonistReliefFood(p, food))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch]
    public static class MouseDisasterReliefAreaFoodSearchPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(FoodUtility),
                nameof(FoodUtility.TryFindBestFoodSourceFor),
                new[]
                {
                    typeof(Pawn),
                    typeof(Pawn),
                    typeof(bool),
                    typeof(Thing).MakeByRefType(),
                    typeof(ThingDef).MakeByRefType(),
                    typeof(bool),
                    typeof(bool),
                    typeof(bool),
                    typeof(bool),
                    typeof(bool),
                    typeof(bool),
                    typeof(bool),
                    typeof(bool),
                    typeof(bool),
                    typeof(bool),
                    typeof(bool),
                    typeof(FoodPreferability)
                });
        }

        public static void Postfix(
            Pawn getter,
            Pawn eater,
            bool desperate,
            ref Thing foodSource,
            ref ThingDef foodDef,
            bool canRefillDispenser,
            bool canUseInventory,
            bool canUsePackAnimalInventory,
            bool allowForbidden,
            bool allowCorpse,
            bool allowSociallyImproper,
            bool allowHarvest,
            bool forceScanWholeMap,
            bool ignoreReservations,
            bool calculateWantedStackCount,
            bool allowVenerated,
            FoodPreferability minPrefOverride,
            ref bool __result)
        {
            Map map = getter?.Map;
            if (map == null)
            {
                return;
            }

            if (!MouseDisasterUtility.MapHasReliefArea(map))
            {
                return;
            }

            if (MouseDisasterUtility.IsReliefAreaPostfixSuppressed)
            {
                return;
            }

            if (MouseDisasterUtility.ShouldPrioritizeReliefAreaFood(getter) &&
                MouseDisasterUtility.TryFindBestReliefFoodSourceFor(
                    getter,
                    eater,
                    desperate,
                    allowHarvest,
                    out Thing reliefFoodSource,
                    out ThingDef reliefFoodDef,
                    allowForbidden,
                    allowCorpse,
                    allowSociallyImproper,
                    ignoreReservations,
                    calculateWantedStackCount,
                    allowVenerated,
                    minPrefOverride))
            {
                foodSource = reliefFoodSource;
                foodDef = reliefFoodDef;
                __result = true;
                return;
            }

            if (__result &&
                MouseDisasterUtility.ShouldBlockColonistReliefFood(getter, foodSource) &&
                MouseDisasterUtility.TryFindBestNonReliefFoodSourceFor(
                    getter,
                    eater,
                    desperate,
                    allowHarvest,
                    out Thing replacementFoodSource,
                    out ThingDef replacementFoodDef,
                    allowForbidden,
                    allowCorpse,
                    allowSociallyImproper,
                    ignoreReservations,
                    calculateWantedStackCount,
                    allowVenerated,
                    minPrefOverride))
            {
                foodSource = replacementFoodSource;
                foodDef = replacementFoodDef;
                __result = true;
                return;
            }

            if (__result && MouseDisasterUtility.ShouldBlockColonistReliefFood(getter, foodSource))
            {
                foodSource = null;
                foodDef = null;
                __result = false;
            }
        }
    }
}
