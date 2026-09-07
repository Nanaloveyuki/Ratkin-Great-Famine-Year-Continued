using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace MouseDisaster
{
    public class JobGiver_MouseDisasterBeggar : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (MouseDisasterUtility.IsPendingAbandonedChild(pawn)) return null;
            if (MouseDisasterUtility.IsPlayerAffiliatedRatkin(pawn) ||
                !MouseDisasterUtility.IsBeggarPawn(pawn) ||
                (!MouseDisasterUtility.IsInBeggarMentalState(pawn) && (!MouseDisasterFeeding.HasSatisfied(pawn) || pawn.InMentalState)) ||
                pawn.Map == null ||
                pawn.Downed || pawn.DevelopmentalStage == DevelopmentalStage.Baby)
            {
                return null;
            }

            if (pawn.InAggroMentalState || (pawn.Faction != null && Faction.OfPlayer != null && pawn.Faction.HostileTo(Faction.OfPlayer)))
            {
                return null;
            }

            bool mustStayOnMap = MouseDisasterUtility.MustStayForAirDropError(pawn);
            if (MouseDisasterFeeding.HasSatisfied(pawn))
                return mustStayOnMap ? null : MouseDisasterUtility.ExitMapJob(pawn);
            if (MouseDisasterFeeding.HasTemporarySatiety(pawn)) return null;
            if (!MouseDisasterUtility.CanBegAgain(pawn))
                return MouseDisasterUtility.TryCreateReliefFoodJob(pawn, allowInventorySearch: false);

            if (MouseDisasterUtility.IsSiegeBeggar(pawn))
            {
                if (!mustStayOnMap && MouseDisasterUtility.HasSiegeBeggarLeaveCondition(pawn))
                {
                    return MouseDisasterUtility.ExitMapJob(pawn);
                }

                Job reliefFoodJob = MouseDisasterUtility.TryCreateReliefFoodJob(pawn, allowInventorySearch: false);
                if (reliefFoodJob != null)
                {
                    return reliefFoodJob;
                }

                Pawn repeatTarget = FindClosestReachableColonist(pawn, preferUnbegged: true);
                if (repeatTarget == null)
                {
                    Job theftJob = MouseDisasterUtility.TryCreateImproperFoodJob(pawn, true);
                    if (theftJob != null)
                    {
                        return theftJob;
                    }

                    Job recoveryJob = TryCreateRecoveryJob(pawn);
                    if (recoveryJob != null)
                    {
                        return recoveryJob;
                    }

                    MouseDisasterUtility.ClearBeggarTargetHistory(pawn);
                    repeatTarget = FindClosestReachableColonist(pawn, preferUnbegged: false);
                }

                if (repeatTarget != null)
                {
                    return JobMaker.MakeJob(MouseDisasterDefOf.MouseDisaster_BegForFood, repeatTarget);
                }

                return MouseDisasterUtility.TryCreateImproperFoodJob(pawn, true) ?? TryCreateRecoveryJob(pawn);
            }

            if (!mustStayOnMap && (MouseDisasterUtility.HasBeggarSucceeded(pawn) || pawn.needs?.food?.CurLevelPercentage >= 0.75f))
            {
                return MouseDisasterUtility.ExitMapJob(pawn);
            }

            Job directReliefFoodJob = MouseDisasterUtility.TryCreateReliefFoodJob(pawn, allowInventorySearch: false);
            if (directReliefFoodJob != null)
            {
                return directReliefFoodJob;
            }

            int colonistCount = pawn.Map.mapPawns.FreeColonistsSpawnedCount;
            if (colonistCount > 0 && MouseDisasterUtility.GetBegAttempts(pawn) >= colonistCount)
            {
                return MouseDisasterUtility.TryCreateImproperFoodJob(pawn, false) ??
                       TryCreateRecoveryJob(pawn) ??
                       (mustStayOnMap ? null : MouseDisasterUtility.ExitMapJob(pawn));
            }

            Pawn targetColonist = FindClosestReachableColonist(pawn, preferUnbegged: true);
            if (targetColonist == null)
            {
                return MouseDisasterUtility.TryCreateImproperFoodJob(pawn, false) ??
                       TryCreateRecoveryJob(pawn) ??
                       (mustStayOnMap ? null : MouseDisasterUtility.ExitMapJob(pawn));
            }

            return JobMaker.MakeJob(MouseDisasterDefOf.MouseDisaster_BegForFood, targetColonist);
        }

        private static Pawn FindClosestReachableColonist(Pawn pawn, bool preferUnbegged)
        {
            IReadOnlyList<Pawn> colonists = pawn.Map.mapPawns.FreeColonistsSpawned;
            int px = pawn.Position.x;
            int pz = pawn.Position.z;
            Pawn bestUnbegged = null;
            int bestUnbeggedDistSq = int.MaxValue;
            Pawn bestAny = null;
            int bestAnyDistSq = int.MaxValue;

            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn colonist = colonists[i];
                if (!MouseDisasterUtility.CanReceiveBegging(colonist) || !MouseDisasterUtility.CanBegAgain(colonist) || !pawn.CanReserve(colonist) ||
                    !pawn.CanReach(colonist, PathEndMode.Touch, Danger.Some))
                {
                    continue;
                }

                int dx = colonist.Position.x - px;
                int dz = colonist.Position.z - pz;
                int distSq = dx * dx + dz * dz;

                if (distSq < bestAnyDistSq)
                {
                    bestAnyDistSq = distSq;
                    bestAny = colonist;
                }

                if (preferUnbegged && distSq < bestUnbeggedDistSq && !MouseDisasterUtility.HasBeggedColonist(pawn, colonist))
                {
                    bestUnbeggedDistSq = distSq;
                    bestUnbegged = colonist;
                }
            }

            return preferUnbegged ? bestUnbegged : bestAny;
        }

        private static Job TryCreateRecoveryJob(Pawn pawn)
        {
            if (!MouseDisasterUtility.HasAnyStealableFoodOnMap(pawn?.Map))
            {
                return null;
            }

            return MouseDisasterUtility.TryCreatePathRecoveryJob(pawn);
        }
    }
}
