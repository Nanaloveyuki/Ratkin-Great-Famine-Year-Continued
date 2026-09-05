using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace MouseDisaster
{
    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
    public static class MouseDisasterStartJobGuardPatch
    {
        private static readonly System.Reflection.FieldInfo PawnField = AccessTools.Field(typeof(Pawn_JobTracker), "pawn");

        public static bool Prefix(Pawn_JobTracker __instance, Job newJob)
        {
            if (!MouseDisasterUtility.IsExtremeCompatJobModeEnabled || newJob?.def?.defName == null)
            {
                return true;
            }

            if (newJob.def.defName.IndexOf("BePlayedWith", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return true;
            }

            Pawn pawn = PawnField?.GetValue(__instance) as Pawn;
            if (pawn == null)
            {
                return true;
            }

            if ((newJob.targetA.IsValid && newJob.targetA.Thing == pawn) ||
                (newJob.targetB.IsValid && newJob.targetB.Thing == pawn))
            {
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Log), nameof(Log.Error), new[] { typeof(string) })]
    public static class MouseDisasterErrorLogThrottlePatch
    {
        private static readonly Dictionary<string, int> LastEmitTickByKey = new Dictionary<string, int>();

        public static bool Prefix(string text)
        {
            if (!MouseDisasterUtility.IsExtremeCompatJobModeEnabled || text.NullOrEmpty())
            {
                return true;
            }

            string key = null;
            if (text.IndexOf("Error in PotentialWorkThingsGlobal", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                key = "trainingfacility_potentialwork";
            }
            else if (text.IndexOf("Constructed TargetInfo with cell=", StringComparison.OrdinalIgnoreCase) >= 0 &&
                     text.IndexOf("null map", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                key = "null_map_targetinfo";
            }

            if (key == null)
            {
                return true;
            }

            int nowTick = Find.TickManager?.TicksGame ?? Environment.TickCount;
            if (LastEmitTickByKey.TryGetValue(key, out int lastTick) && nowTick - lastTick < 600)
            {
                return false;
            }

            LastEmitTickByKey[key] = nowTick;
            return true;
        }
    }
}
