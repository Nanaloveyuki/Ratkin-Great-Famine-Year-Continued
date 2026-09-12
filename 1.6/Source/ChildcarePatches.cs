using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MouseDisaster
{
    internal static class MouseDisasterChildcarePolicy
    {
        internal static bool AllowsColonistCare(Faction carerFaction, Pawn baby)
        {
            if (MouseDisasterMod.Settings?.allowColonistChildcareForMouseDisasterEggs != true ||
                carerFaction == null ||
                Faction.OfPlayer == null ||
                carerFaction != Faction.OfPlayer ||
                baby == null ||
                !IsMouseDisasterNeutralFaction(baby.Faction) ||
                !MouseDisasterUtility.IsMouseEggBaby(baby))
            {
                return false;
            }

            return MouseDisasterUtility.IsMouseDisasterPawn(baby) ||
                   MouseDisasterUtility.IsMouseDisasterIncidentVisitor(baby);
        }

        internal static bool IsColonistCarer(Pawn pawn)
        {
            return pawn != null &&
                   pawn.IsColonist &&
                   pawn.Faction == Faction.OfPlayer &&
                   MouseDisasterMod.Settings?.allowColonistChildcareForMouseDisasterEggs == true;
        }

        internal static IEnumerable<Pawn> AllowedBabies(Map map, bool includeUnspawned)
        {
            if (map?.mapPawns == null)
            {
                yield break;
            }

            IReadOnlyList<Pawn> pawns = includeUnspawned ? map.mapPawns.AllPawns : map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn baby = pawns[i];
                if (baby != null && !baby.Dead && AllowsColonistCare(Faction.OfPlayer, baby))
                {
                    yield return baby;
                }
            }
        }

        internal static IEnumerable<Thing> AppendAllowedBabies(IEnumerable<Thing> original, Map map)
        {
            if (original != null)
            {
                foreach (Thing thing in original)
                {
                    yield return thing;
                }
            }

            foreach (Pawn baby in AllowedBabies(map, includeUnspawned: false))
            {
                yield return baby;
            }
        }

        private static bool IsMouseDisasterNeutralFaction(Faction faction)
        {
            return faction != null &&
                   (faction.def == MouseDisasterDefOf.MouseDisaster_HiddenFaction ||
                    faction.def == MouseDisasterDefOf.MouseDisaster_NeutralVisitors) &&
                   Faction.OfPlayer != null &&
                   !faction.HostileTo(Faction.OfPlayer);
        }
    }

    [HarmonyPatch(typeof(ChildcareUtility), nameof(ChildcareUtility.HasBreastfeedCompatibleFactions), new[] { typeof(Faction), typeof(Pawn) })]
    internal static class MouseDisasterChildcareFactionPatch
    {
        public static void Postfix(Faction faction, Pawn baby, ref bool __result)
        {
            if (!__result && MouseDisasterChildcarePolicy.AllowsColonistCare(faction, baby))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(WorkGiver_PlayWithBaby), nameof(WorkGiver_PlayWithBaby.PotentialWorkThingsGlobal))]
    internal static class MouseDisasterPlayWithBabyTargetsPatch
    {
        public static void Postfix(Pawn pawn, ref IEnumerable<Thing> __result)
        {
            if (MouseDisasterChildcarePolicy.IsColonistCarer(pawn))
            {
                __result = MouseDisasterChildcarePolicy.AppendAllowedBabies(__result, pawn.Map);
            }
        }
    }

    [HarmonyPatch(typeof(ChildcareUtility), nameof(ChildcareUtility.FindAutofeedBaby))]
    internal static class MouseDisasterAutofeedBabyPatch
    {
        public static void Postfix(Pawn mom, AutofeedMode priorityLevel, ref Thing food, ref Pawn __result)
        {
            if (__result != null ||
                priorityLevel == AutofeedMode.Never ||
                !MouseDisasterChildcarePolicy.IsColonistCarer(mom) ||
                mom.MapHeld == null)
            {
                return;
            }

            ChildcareUtility.BreastfeedFailReason? breastfeedFailReason;
            bool canBreastfeed = ChildcareUtility.CanBreastfeedNow(mom, out breastfeedFailReason);
            foreach (Pawn baby in MouseDisasterChildcarePolicy.AllowedBabies(mom.MapHeld, includeUnspawned: true))
            {
                if (baby.mindState == null ||
                    baby.Suspended ||
                    !ChildcareUtility.WantsSuckle(baby, out breastfeedFailReason) ||
                    baby.mindState.AutofeedSetting(mom) != priorityLevel ||
                    !ChildcareUtility.CanFeedBaby(mom, baby, out breastfeedFailReason) ||
                    !ChildcareUtility.CanHaulBabyToMomNow(mom, mom, baby, false, out breastfeedFailReason))
                {
                    continue;
                }

                Thing candidateFood = canBreastfeed ? mom : ChildcareUtility.FindBabyFoodForBaby(mom, baby);
                if (candidateFood == null)
                {
                    continue;
                }

                food = candidateFood;
                __result = baby;
                return;
            }
        }
    }

    [HarmonyPatch(typeof(ChildcareUtility), nameof(ChildcareUtility.FindUnsafeBaby))]
    internal static class MouseDisasterUnsafeBabyPatch
    {
        public static void Postfix(Pawn mom, AutofeedMode priorityLevel, ref Pawn __result)
        {
            if (__result != null ||
                priorityLevel == AutofeedMode.Never ||
                !MouseDisasterChildcarePolicy.IsColonistCarer(mom) ||
                mom.MapHeld == null)
            {
                return;
            }

            ChildcareUtility.BreastfeedFailReason? breastfeedFailReason;
            foreach (Pawn baby in MouseDisasterChildcarePolicy.AllowedBabies(mom.MapHeld, includeUnspawned: false))
            {
                if (baby.mindState == null ||
                    baby.mindState.AutofeedSetting(mom) != priorityLevel ||
                    CaravanFormingUtility.IsFormingCaravanOrDownedPawnToBeTakenByCaravan(baby) ||
                    !ChildcareUtility.CanSuckle(baby, out breastfeedFailReason))
                {
                    continue;
                }

                LocalTargetInfo safePlace = ChildcareUtility.SafePlaceForBaby(baby, mom, false);
                if (!safePlace.IsValid)
                {
                    continue;
                }

                Building_Bed bed = safePlace.Thing as Building_Bed;
                if ((bed != null && baby.CurrentBed() == bed) ||
                    (bed == null && baby.Spawned && baby.Position == safePlace.Cell))
                {
                    continue;
                }

                __result = baby;
                return;
            }
        }
    }
}
