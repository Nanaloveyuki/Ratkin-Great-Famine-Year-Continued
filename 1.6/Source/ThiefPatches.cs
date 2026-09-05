using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace MouseDisaster
{
    [HarmonyPatch(typeof(MentalStateHandler), nameof(MentalStateHandler.TryStartMentalState))]
    public static class ThiefMentalStatePatch
    {
        private static readonly AccessTools.FieldRef<MentalStateHandler, Pawn> PawnField =
            AccessTools.FieldRefAccess<MentalStateHandler, Pawn>("pawn");

        public static bool Prefix(MentalStateHandler __instance, MentalStateDef stateDef, ref bool __result)
        {
            Pawn pawn = PawnField(__instance);
            bool isThief = MouseDisasterUtility.IsThiefPawn(pawn);
            bool isBeggar = MouseDisasterUtility.IsBeggarPawn(pawn);
            if (!isThief && !isBeggar)
            {
                return true;
            }

            if (stateDef != null && stateDef.IsAggro)
            {
                if (!MouseDisasterUtility.CanMouseDisasterVisitorRetaliate(pawn))
                {
                    __result = false;
                    return false;
                }

                return true;
            }

            if (isBeggar && stateDef == MentalStateDefOf.PanicFlee && !MouseDisasterUtility.CanMouseDisasterVisitorRetaliate(pawn))
            {
                __result = false;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn_MindState), nameof(Pawn_MindState.StartFleeingBecauseOfPawnAction))]
    public static class ThiefFleePatch
    {
        private static readonly AccessTools.FieldRef<Pawn_MindState, Pawn> PawnField =
            AccessTools.FieldRefAccess<Pawn_MindState, Pawn>("pawn");

        public static bool Prefix(Pawn_MindState __instance)
        {
            Pawn pawn = PawnField(__instance);
            return !MouseDisasterUtility.IsBeggarPawn(pawn) || MouseDisasterUtility.CanMouseDisasterVisitorRetaliate(pawn);
        }
    }
}
