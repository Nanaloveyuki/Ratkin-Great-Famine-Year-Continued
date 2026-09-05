using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace MouseDisaster
{
    [HarmonyPatch(typeof(IncidentWorker), nameof(IncidentWorker.CanFireNow))]
    public static class MouseDisasterIncidentTogglePatch
    {
        public static bool Prefix(IncidentWorker __instance, IncidentParms parms, ref bool __result)
        {
            if (!MouseDisasterRuntime.AllowsNewContent && MouseDisasterIncidentCatalog.IsKnownIncident(__instance?.def?.defName))
            {
                __result = false;
                return false;
            }

            if (!MouseDisasterIncidentTargetPolicy.ShouldBlockCanFireNowForInvalidTarget(
                    __instance?.def?.defName,
                    parms?.target != null,
                    parms?.target is Map,
                    parms?.target is Caravan))
            {
                return true;
            }

            __result = false;
            return false;
        }

        public static void Postfix(IncidentWorker __instance, IncidentParms parms, ref bool __result)
        {
            if (!__result || __instance?.def?.defName == null || MouseDisasterMod.Settings == null)
            {
                return;
            }

            if (!MouseDisasterIncidentCatalog.IsIncidentEnabled(__instance.def.defName, MouseDisasterMod.Settings.disabledIncidentDefNames))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(IncidentWorker), nameof(IncidentWorker.TryExecute))]
    public static class MouseDisasterIncidentExecuteTogglePatch
    {
        public static bool Prefix(IncidentWorker __instance, IncidentParms parms, ref bool __result, out System.Collections.Generic.HashSet<Pawn> __state)
        {
            __state = null;
            string defName = __instance?.def?.defName;
            if (!MouseDisasterIncidentCatalog.IsKnownIncident(defName))
            {
                return true;
            }

            if (MouseDisasterRuntime.AllowsNewContent &&
                (GameComponent_MouseDisasterNarrative.DebugForcing || MouseDisasterIncidentCatalog.IsIncidentEnabled(defName, MouseDisasterMod.Settings?.disabledIncidentDefNames)))
            {
                if (parms?.target is Map map) __state = new System.Collections.Generic.HashSet<Pawn>(map.mapPawns.AllPawns);
                return true;
            }

            __result = false;
            return false;
        }

        public static void Postfix(IncidentWorker __instance, IncidentParms parms, bool __result, System.Collections.Generic.HashSet<Pawn> __state)
        {
            if (!__result || !MouseDisasterIncidentCatalog.IsKnownIncident(__instance?.def?.defName))
            {
                return;
            }

            var participants = new System.Collections.Generic.List<Pawn>();
            if (__state != null && parms?.target is Map map)
                foreach (Pawn pawn in map.mapPawns.AllPawns)
                    if (!__state.Contains(pawn)) participants.Add(pawn);
            Current.Game?.GetComponent<GameComponent_MouseDisasterNarrative>()?.RecordIncidentExecuted(__instance.def, parms, participants);
        }
    }

    [HarmonyPatch(typeof(IncidentDef), nameof(IncidentDef.TargetAllowed))]
    public static class MouseDisasterIncidentTargetAllowedPatch
    {
        public static bool Prefix(IncidentDef __instance, IIncidentTarget target, ref bool __result)
        {
            if (__instance == null)
            {
                return true;
            }

            if (target == null)
            {
                __result = false;
                return false;
            }

            if (MouseDisasterIncidentTargetPolicy.TryEvaluateSafeTargetAllowed(
                    __instance.defName,
                    target is Map,
                    target is Map map && map.IsPlayerHome,
                    target is Caravan,
                    target is Caravan caravan && caravan.IsPlayerControlled,
                    out bool safeResult))
            {
                __result = safeResult;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(IncidentWorker), "get_BaseChanceThisGame")]
    public static class MouseDisasterIncidentBaseChancePatch
    {
        public static void Postfix(IncidentWorker __instance, ref float __result)
        {
            if (__instance?.def?.defName == null)
            {
                return;
            }

            if (!__instance.def.defName.StartsWith("MouseDisaster_", System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (__instance.def.defName.IndexOf("Plague", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                __result *= 0.5f;
            }
        }
    }

    [HarmonyPatch(typeof(IncidentWorker), nameof(IncidentWorker.ChanceFactorNow))]
    public static class MouseDisasterIncidentSeasonChancePatch
    {
        public static void Postfix(IncidentWorker __instance, IIncidentTarget target, ref float __result)
        {
            if (__instance?.def?.defName == null || target == null)
            {
                return;
            }

            if (!__instance.def.defName.StartsWith("MouseDisaster_", System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!target.Tile.Valid)
            {
                return;
            }

            Season season = GenLocalDate.Season(target.Tile);
            if (season == Season.Spring || season == Season.Winter)
            {
                __result *= 1.1f;
            }
        }
    }
}
