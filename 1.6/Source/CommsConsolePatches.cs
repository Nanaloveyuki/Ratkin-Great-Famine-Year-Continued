using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace MouseDisaster
{
    [HarmonyPatch(typeof(Building_CommsConsole), nameof(Building_CommsConsole.GetFloatMenuOptions))]
    public static class CommsConsolePatches
    {
        private const string BroadcastHopeLabel = "\u5e7f\u64ad\u5e0c\u671b";

        public static void Postfix(Building_CommsConsole __instance, Pawn myPawn, ref IEnumerable<FloatMenuOption> __result)
        {
            List<FloatMenuOption> options = __result?.ToList() ?? new List<FloatMenuOption>();
            FloatMenuOption option = BuildBroadcastHopeOption(__instance, myPawn);
            if (option != null)
            {
                options.Insert(0, option);
            }

            __result = options;
        }

        private static FloatMenuOption BuildBroadcastHopeOption(Building_CommsConsole console, Pawn pawn)
        {
            if (!MouseDisasterRuntime.AllowsNewContent ||
                console == null ||
                console.Map == null ||
                !console.Map.IsPlayerHome ||
                pawn == null ||
                pawn.Dead)
            {
                return null;
            }

            if (!pawn.CanReach(console, PathEndMode.InteractionCell, Danger.Some))
            {
                return new FloatMenuOption(BroadcastHopeLabel + "\uff08\u65e0\u6cd5\u5230\u8fbe\uff09", null);
            }

            if (console.Spawned && console.Map != null && console.Map.gameConditionManager.ElectricityDisabled(console.Map))
            {
                return new FloatMenuOption(BroadcastHopeLabel + "\uff08\u592a\u9633\u8000\u6591\uff09", null);
            }

            CompPowerTrader powerComp = console.GetComp<CompPowerTrader>();
            if (powerComp != null && !powerComp.PowerOn)
            {
                return new FloatMenuOption(BroadcastHopeLabel + "\uff08\u65e0\u7535\u529b\uff09", null);
            }

            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking))
            {
                return new FloatMenuOption(BroadcastHopeLabel + "\uff08\u65e0\u6cd5\u4ea4\u8c08\uff09", null);
            }

            if (GameComponent_MouseDisasterBroadcastHope.TryGetBroadcastCooldownRemainingDays(console.Map, out float remainingDays))
            {
                return new FloatMenuOption(BroadcastHopeLabel + "\uff08\u51b7\u5374\u4e2d " + remainingDays.ToString("0.0") + "\u5929\uff09", null);
            }

            if (!pawn.CanReserve(console))
            {
                return new FloatMenuOption(BroadcastHopeLabel + "\uff08\u5df2\u88ab\u5360\u7528\uff09", null);
            }

            FloatMenuOption option = new FloatMenuOption(BroadcastHopeLabel, delegate
            {
                Job job = JobMaker.MakeJob(MouseDisasterDefOf.MouseDisaster_BroadcastHope, console);
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.OpeningComms, KnowledgeAmount.Total);
            }, MenuOptionPriority.InitiateSocial);

            return FloatMenuUtility.DecoratePrioritizedTask(option, pawn, console);
        }
    }
}
