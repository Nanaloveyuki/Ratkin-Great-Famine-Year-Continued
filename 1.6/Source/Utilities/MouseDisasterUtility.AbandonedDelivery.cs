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

        public static void RegisterAbandonedDelivery(Pawn adult, IEnumerable<Pawn> children, IntVec3 foodCell)
        {
            if (adult?.Map == null || children == null)
            {
                return;
            }

            List<int> childIds = new List<int>();
            List<Pawn> childPawns = new List<Pawn>();
            HashSet<int> seenIds = new HashSet<int>();
            foreach (Pawn child in children)
            {
                if (child == null || child.Dead)
                {
                    continue;
                }

                if (seenIds.Add(child.thingIDNumber))
                {
                    childIds.Add(child.thingIDNumber);
                }

                childPawns.Add(child);
                MouseDisasterPawnGroupUtility.HoldForDropoff(child, foodCell);
            }

            if (childIds.Count == 0)
            {
                return;
            }

            ActiveAbandonedDeliveryByAdultId[adult.thingIDNumber] = new AbandonedDeliveryState
            {
                adult = adult,
                childPawns = childPawns,
                mapId = adult.Map.uniqueID,
                foodCell = foodCell,
                childPawnIds = childIds,
                adultHasLeft = false,
                adultArrivedAtDropoffTick = -1
            };

            bool allChildrenArrived = AreAllPawnsNearCell(childPawns, foodCell, 3f);
            if (MouseDisasterAbandonedDeliveryPolicy.ShouldUseVanillaCarryDelivery(IsLeadYourPetEnabled, allChildrenArrived))
            {
                TryStartAbandonedDeliveryCarryJob(adult, childPawns, foodCell);
            }
        }

        public static void CancelAbandonedDelivery(Pawn adult, IEnumerable<Pawn> children = null)
        {
            if (adult != null)
            {
                ActiveAbandonedDeliveryByAdultId.Remove(adult.thingIDNumber);
                TryEndLeadYourPetLeashForPet(adult);
            }

            if (children == null)
            {
                return;
            }

            foreach (Pawn child in children)
            {
                TryEndLeadYourPetLeashForPet(child);
            }
        }

        public static void ProcessAbandonedDeliveries(Map map)
        {
            if (map == null || ActiveAbandonedDeliveryByAdultId.Count == 0)
            {
                return;
            }

            int nowTick = Find.TickManager?.TicksGame ?? 0;
            IReadOnlyList<Pawn> mapPawns = map.mapPawns.AllPawnsSpawned;
            Dictionary<int, Pawn> pawnLookup = BuildSpawnedPawnLookup(mapPawns);
            ReusableIntList.Clear();
            foreach (int key in ActiveAbandonedDeliveryByAdultId.Keys)
            {
                ReusableIntList.Add(key);
            }

            for (int i = 0; i < ReusableIntList.Count; i++)
            {
                int adultId = ReusableIntList[i];
                if (!ActiveAbandonedDeliveryByAdultId.TryGetValue(adultId, out AbandonedDeliveryState state) || state == null || state.mapId != map.uniqueID)
                {
                    continue;
                }

                Pawn adult = ResolvePawnById(pawnLookup, adultId);
                if (!state.adultHasLeft && (adult == null || adult.Dead || !adult.Spawned))
                {
                    state.adultHasLeft = true;
                }

                if (adult?.carryTracker?.CarriedThing is Pawn carriedPawn)
                {
                    pawnLookup[carriedPawn.thingIDNumber] = carriedPawn;
                }

                ReusablePawnList.Clear();
                for (int j = 0; j < state.childPawnIds.Count; j++)
                {
                    Pawn child = ResolvePawnById(pawnLookup, state.childPawnIds[j]);
                    if (child != null && !child.Dead && !IsPlayerAffiliatedRatkin(child) &&
                        (child.Spawned || child.ParentHolder is Pawn_CarryTracker))
                    {
                        ReusablePawnList.Add(child);
                    }
                }

                if (ReusablePawnList.Count == 0)
                {
                    if (adult != null && adult.Spawned && !adult.Dead && !IsPlayerAffiliatedRatkin(adult))
                        MakeTravelAndExitLord(map, new[] { adult }, map.Center);
                    ActiveAbandonedDeliveryByAdultId.Remove(adultId);
                    continue;
                }

                foreach (Pawn child in ReusablePawnList)
                    if (child.Spawned && child.Position.InHorDistOf(state.foodCell, 3f) &&
                        !state.deliveredChildIds.Contains(child.thingIDNumber))
                    {
                        state.deliveredChildIds.Add(child.thingIDNumber);
                        child.jobs?.StopAll();
                    }
                bool allChildrenArrived = ReusablePawnList.All(child => state.deliveredChildIds.Contains(child.thingIDNumber));
                for (int childIndex = 0; childIndex < ReusablePawnList.Count; childIndex++)
                {
                    Pawn child = ReusablePawnList[childIndex];
                    if (!child.Spawned)
                    {
                        continue;
                    }

                    if (state.deliveredChildIds.Contains(child.thingIDNumber))
                    {
                        continue;
                    }

                    if (MouseDisasterAbandonedDeliveryPolicy.ShouldUseVanillaCarryDelivery(IsLeadYourPetEnabled, allChildrenArrived))
                    {
                        continue;
                    }

                    bool hasValidGoto = child.CurJobDef == JobDefOf.Goto &&
                                        child.CurJob != null &&
                                        child.CurJob.targetA.IsValid &&
                                        child.CurJob.targetA.Cell.InHorDistOf(state.foodCell, 3f);
                    if (!hasValidGoto)
                    {
                        TryTakeAutoOrderedJob(child, CreateGotoJob(state.foodCell), JobTag.Misc, requireStarving: false);
                    }
                }

                if (!state.adultHasLeft)
                {
                    if (adult == null || adult.Dead || !adult.Spawned)
                    {
                        state.adultHasLeft = true;
                    }
                    else
                    {
                        if (MouseDisasterAbandonedDeliveryPolicy.ShouldUseVanillaCarryDelivery(IsLeadYourPetEnabled, allChildrenArrived) &&
                            TryStartAbandonedDeliveryCarryJob(adult, ReusablePawnList.Where(child =>
                                !state.deliveredChildIds.Contains(child.thingIDNumber)).ToList(), state.foodCell))
                        {
                            continue;
                        }

                        bool adultArrived = adult.Position.InHorDistOf(state.foodCell, 3f);
                        if (!adultArrived)
                        {
                            state.adultArrivedAtDropoffTick = -1;
                            TryTakeAutoOrderedJob(adult, CreateGotoJob(state.foodCell), JobTag.Misc, requireStarving: false);
                            continue;
                        }

                        if (state.adultArrivedAtDropoffTick < 0)
                        {
                            state.adultArrivedAtDropoffTick = nowTick;
                        }

                        if (!allChildrenArrived)
                        {
                            int ticksWaitingSinceArrival = nowTick - state.adultArrivedAtDropoffTick;
                            if (MouseDisasterAbandonedDeliveryPolicy.ShouldForceAdultLeaveAfterArrivalStall(
                                adultArrivedAtDropoff: true,
                                ticksWaitingSinceArrival))
                            {
                                if (!TryFindFarEdgeCell(map, state.foodCell, out IntVec3 forcedExitCell))
                                {
                                    forcedExitCell = map.Center;
                                }

                                bool alreadyExitingAfterTimeout = adult.GetLord()?.LordJob is LordJob_TravelAndExit;
                                if (!alreadyExitingAfterTimeout)
                                {
                                    MakeTravelAndExitLord(map, new[] { adult }, forcedExitCell);
                                }

                                state.adultHasLeft = true;
                                Messages.Message("MouseDisaster_UI_AbandoningMotherTimedOut".Translate().Resolve(), adult, MessageTypeDefOf.NeutralEvent, historical: false);
                            }

                            continue;
                        }

                        if (MouseDisasterAbandonedDeliveryPolicy.ShouldReleaseLeadYourPetDropoffLeashes(IsLeadYourPetEnabled, allChildrenArrived))
                        {
                            for (int childIndex = 0; childIndex < ReusablePawnList.Count; childIndex++)
                            {
                                TryEndLeadYourPetLeashForPet(ReusablePawnList[childIndex]);
                            }
                        }

                        if (!TryFindFarEdgeCell(map, state.foodCell, out IntVec3 exitCell))
                        {
                            exitCell = map.Center;
                        }

                        bool alreadyExiting = adult.GetLord()?.LordJob is LordJob_TravelAndExit;
                        if (!alreadyExiting)
                        {
                            MakeTravelAndExitLord(map, new[] { adult }, exitCell);
                            state.adultHasLeft = true;
                            Messages.Message("MouseDisaster_UI_AbandoningMotherLeft".Translate().Resolve(), adult, MessageTypeDefOf.NeutralEvent, historical: false);
                        }
                    }
                }

                if (!allChildrenArrived && !state.adultHasLeft)
                {
                    continue;
                }

                if (!state.adultHasLeft || (adult != null && adult.Spawned && !adult.Dead))
                {
                    continue;
                }

                for (int childIndex = 0; childIndex < ReusablePawnList.Count; childIndex++)
                {
                    ConvertToAbandonedWildChild(ReusablePawnList[childIndex], state.foodCell);
                }

                ActiveAbandonedDeliveryByAdultId.Remove(adultId);
                Messages.Message("MouseDisaster_UI_AbandonedChildrenRemain".Translate().Resolve(), ReusablePawnList, MessageTypeDefOf.NeutralEvent, historical: false);
            }
        }

        public static bool IsPendingAbandonedChild(Pawn pawn)
        {
            return pawn != null && ActiveAbandonedDeliveryByAdultId.Values.Any(state =>
                state != null && state.childPawnIds.Contains(pawn.thingIDNumber));
        }

        private static bool TryStartAbandonedDeliveryCarryJob(Pawn adult, IReadOnlyList<Pawn> children, IntVec3 foodCell)
        {
            if (adult == null || adult.Dead || !adult.Spawned || children == null || !foodCell.IsValid)
            {
                return false;
            }

            if (adult.CurJobDef == JobDefOf.DeliverToCell &&
                adult.CurJob != null &&
                adult.CurJob.targetB.IsValid &&
                adult.CurJob.targetB.Cell.InHorDistOf(foodCell, 3f))
            {
                return true;
            }

            Pawn targetChild = children
                .Where(child => child != null &&
                                !child.Dead &&
                                child.Spawned &&
                                !child.Position.InHorDistOf(foodCell, 3f) &&
                                adult.CanReserveAndReach(child, PathEndMode.OnCell, Danger.Deadly))
                .OrderBy(child => adult.Position.DistanceToSquared(child.Position))
                .FirstOrDefault();
            if (targetChild == null)
            {
                return false;
            }

            Job deliverJob = JobMaker.MakeJob(JobDefOf.DeliverToCell, targetChild, foodCell);
            deliverJob.locomotionUrgency = LocomotionUrgency.Jog;
            return TryTakeAutoOrderedJob(adult, deliverJob, JobTag.Misc, requireStarving: false);
        }

        private static bool AreAllPawnsNearCell(IReadOnlyList<Pawn> pawns, IntVec3 cell, float maxDistance)
        {
            if (pawns == null || pawns.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || !pawn.Spawned || pawn.Dead || !pawn.Position.InHorDistOf(cell, maxDistance))
                {
                    return false;
                }
            }

            return true;
        }

        private static void ConvertToAbandonedWildChild(Pawn pawn, IntVec3 fallbackCell)
        {
            if (pawn == null || pawn.Dead || IsPlayerAffiliatedRatkin(pawn))
            {
                return;
            }

            if (pawn.Faction != null)
            {
                pawn.SetFaction(null);
            }

            if (MouseDisasterDefOf.MouseDisaster_WildRatkinChild != null)
            {
                pawn.kindDef = MouseDisasterDefOf.MouseDisaster_WildRatkinChild;
            }

            pawn.guest?.SetGuestStatus(null, GuestStatus.Guest);
            pawn.mindState?.mentalStateHandler?.Reset();
            if (pawn.mindState != null) pawn.mindState.duty = null;
            ResetBeggarState(pawn);
            SetMouseDisasterFoodLevel(pawn);
            if (pawn.Spawned)
            {
                TryTakeAutoOrderedJob(pawn, CreateGotoJob(fallbackCell), JobTag.Misc, requireStarving: false);
            }
        }

        public static bool HasActiveAbandonedDeliveryState()
        {
            return ActiveAbandonedDeliveryByAdultId.Count > 0;
        }
    }
}
