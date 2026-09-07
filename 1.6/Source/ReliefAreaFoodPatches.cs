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
            if (__result && food?.Spawned == true && GameComponent_MouseDisasterEventBehavior.HasBehavior(p, MouseDisasterPawnBehavior.ReliefOnly) &&
                MouseDisasterUtility.GetReliefArea(food.Map)?[food.Position] != true)
                __result = false;
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

        public static bool Prefix(Pawn getter, Pawn eater, bool desperate, ref Thing foodSource, ref ThingDef foodDef,
            bool canRefillDispenser, bool canUseInventory, bool canUsePackAnimalInventory, bool allowForbidden,
            bool allowCorpse, ref bool allowSociallyImproper, ref bool allowHarvest, bool forceScanWholeMap,
            bool ignoreReservations, bool calculateWantedStackCount, bool allowVenerated, FoodPreferability minPrefOverride,
            ref bool __result, out MouseDisasterFoodSearchState __state)
        {
            __state = null;
            if (getter != eater || !MapComponent_MouseDisasterFoodTargets.Eligible(getter)) return true;
            if (GameComponent_MouseDisasterEventBehavior.HasBehavior(getter, MouseDisasterPawnBehavior.ReliefOnly))
            {
                allowSociallyImproper = false;
                allowHarvest = false;
            }
            var cache = getter.Map.GetComponent<MapComponent_MouseDisasterFoodTargets>();
            int key = MapComponent_MouseDisasterFoodTargets.Key(desperate, canRefillDispenser, canUseInventory,
                canUsePackAnimalInventory, allowForbidden, allowCorpse, allowSociallyImproper, allowHarvest,
                forceScanWholeMap, ignoreReservations, calculateWantedStackCount, allowVenerated, minPrefOverride) |
                (MouseDisasterUtility.IsReliefAreaPostfixSuppressed ? 16384 : 0);
            __state = new MouseDisasterFoodSearchState { cache = cache, key = key };
            if (!cache.TryRead(getter, key, out foodSource, out foodDef, out bool found)) return true;
            __state.hit = true;
            __result = found;
            return false;
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
            ref bool __result, MouseDisasterFoodSearchState __state)
        {
            if (__state?.hit == true) return;
            try
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
                    allowSociallyImproper || GameComponent_MouseDisasterEventBehavior.HasBehavior(getter, MouseDisasterPawnBehavior.ReliefOnly),
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
            finally
            {
                if (__result && foodSource?.Spawned == true &&
                    GameComponent_MouseDisasterEventBehavior.HasBehavior(getter, MouseDisasterPawnBehavior.ReliefOnly) &&
                    MouseDisasterUtility.GetReliefArea(foodSource.Map)?[foodSource.Position] != true)
                {
                    foodSource = null; foodDef = null; __result = false;
                }
                __state?.cache.Store(getter, __state.key, foodSource, foodDef, __result);
            }
        }
    }
}
