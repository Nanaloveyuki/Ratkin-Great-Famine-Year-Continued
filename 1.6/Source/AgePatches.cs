using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MouseDisaster
{
    [HarmonyPatch(typeof(Pawn_AgeTracker), "BirthdayBiological")]
    public static class AgePatches
    {
        private static readonly AccessTools.FieldRef<Pawn_AgeTracker, Pawn> PawnField =
            AccessTools.FieldRefAccess<Pawn_AgeTracker, Pawn>("pawn");

        public static void Postfix(Pawn_AgeTracker __instance)
        {
            Pawn pawn = PawnField(__instance);
            MouseDisasterUtility.NotifyMouseDisasterPawnIdentityOrLifeStageChanged(pawn);

            MouseDisasterSettings settings = MouseDisasterMod.Settings;
            if (settings == null || !settings.enableAgeCapAdjustment)
            {
                return;
            }

            if (!MouseDisasterUtility.IsMouseDisasterPawn(pawn))
            {
                return;
            }

            int age = __instance.AgeBiologicalYears;
            if (age < settings.maxRatkinAge)
            {
                return;
            }

            float effectiveAge = pawn.def.race.lifeExpectancy * (age / (float)settings.maxRatkinAge);
            int guaranteedPasses = Mathf.Max(1, Mathf.FloorToInt(settings.ageDiseaseMultiplier));
            float extraChance = Mathf.Clamp01(settings.ageDiseaseMultiplier - guaranteedPasses);

            for (int pass = 0; pass < guaranteedPasses; pass++)
            {
                ApplyBirthdayAilments(pawn, effectiveAge);
            }

            if (Rand.Chance(extraChance))
            {
                ApplyBirthdayAilments(pawn, effectiveAge);
            }
        }

        private static void ApplyBirthdayAilments(Pawn pawn, float effectiveAge)
        {
            foreach (HediffGiver_Birthday giver in AgeInjuryUtility.RandomHediffsToGainOnBirthday(pawn, effectiveAge))
            {
                giver.TryApply(pawn);
            }
        }
    }
}
