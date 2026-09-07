using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MouseDisaster
{
    public class JobGiver_MouseDisasterThief : ThinkNode_JobGiver
    {
        private const float LeaveFoodLevel = 0.82f;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (MouseDisasterUtility.IsPlayerAffiliatedRatkin(pawn) ||
                !MouseDisasterUtility.IsThiefPawn(pawn) ||
                !MouseDisasterUtility.IsInThiefMentalState(pawn) ||
                pawn.Map == null ||
                pawn.Downed)
            {
                return null;
            }

            if (pawn.InAggroMentalState || (pawn.Faction != null && Faction.OfPlayer != null && pawn.Faction.HostileTo(Faction.OfPlayer)))
            {
                return null;
            }

            bool mustStayOnMap = MouseDisasterUtility.MustStayForAirDropError(pawn);
            if (GameComponent_MouseDisasterEventBehavior.HasBehavior(pawn, MouseDisasterPawnBehavior.ReliefOnly))
                return MouseDisasterUtility.TryCreateReliefFoodJob(pawn, allowInventorySearch: false);

            if (pawn.needs?.food == null)
            {
                return MouseDisasterUtility.IsThiefChildPawn(pawn)
                    ? TryCreateExtraFoodJob(pawn)
                    : (mustStayOnMap ? null : MouseDisasterUtility.ExitMapJob(pawn));
            }

            if (pawn.needs.food.CurLevelPercentage >= LeaveFoodLevel)
            {
                Job extraFoodJob = TryCreateExtraFoodJob(pawn);
                if (extraFoodJob != null)
                {
                    return extraFoodJob;
                }

                return mustStayOnMap ? null : MouseDisasterUtility.ExitMapJob(pawn);
            }

            Job ingestJob = TryCreateFoodJob(pawn);
            if (ingestJob != null)
            {
                return ingestJob;
            }

            Job recoveryJob = TryCreateRecoveryOrAssaultJob(pawn);
            if (recoveryJob != null)
            {
                return recoveryJob;
            }

            if (MouseDisasterUtility.IsThiefChildPawn(pawn))
            {
                return TryCreateExtraFoodJob(pawn) ?? (mustStayOnMap ? null : MouseDisasterUtility.ExitMapJob(pawn));
            }

            return mustStayOnMap ? null : MouseDisasterUtility.ExitMapJob(pawn);
        }

        private Job TryCreateFoodJob(Pawn pawn)
        {
            bool desperate = pawn.needs.food.CurCategory == HungerCategory.Starving;
            Thing foodSource;
            ThingDef foodDef;
            if (!FoodUtility.TryFindBestFoodSourceFor(
                    pawn,
                    pawn,
                    desperate,
                    out foodSource,
                    out foodDef,
                    canRefillDispenser: true,
                    canUseInventory: true,
                    canUsePackAnimalInventory: false,
                    allowForbidden: false,
                    allowCorpse: false,
                    allowSociallyImproper: true,
                    allowHarvest: false,
                    forceScanWholeMap: true,
                    ignoreReservations: false,
                    calculateWantedStackCount: false,
                    allowVenerated: false))
            {
                return null;
            }

            if (foodSource is Building_NutrientPasteDispenser)
            {
                return null;
            }

            if (!Toils_Ingest.TryFindChairOrSpot(pawn, foodSource, out _))
            {
                return null;
            }

            float nutrition = FoodUtility.GetNutrition(pawn, foodSource, foodDef);
            Pawn inventoryOwner = (foodSource.ParentHolder as Pawn_InventoryTracker)?.pawn;
            if (inventoryOwner != null && inventoryOwner != pawn)
            {
                Job takeOtherInventory = JobMaker.MakeJob(JobDefOf.TakeFromOtherInventory, foodSource, inventoryOwner);
                takeOtherInventory.count = FoodUtility.WillIngestStackCountOf(pawn, foodDef, nutrition);
                return takeOtherInventory;
            }

            Job ingest = JobMaker.MakeJob(JobDefOf.Ingest, foodSource);
            ingest.count = Mathf.Max(1, FoodUtility.WillIngestStackCountOf(pawn, foodDef, nutrition));
            return ingest;
        }

        private Job TryCreateExtraFoodJob(Pawn pawn)
        {
            if (!MouseDisasterUtility.IsThiefChildPawn(pawn) || MouseDisasterUtility.HasFoodInInventory(pawn))
            {
                return null;
            }

            Thing foodSource;
            ThingDef foodDef;
            if (!FoodUtility.TryFindBestFoodSourceFor(
                    pawn,
                    pawn,
                    desperate: false,
                    out foodSource,
                    out foodDef,
                    canRefillDispenser: false,
                    canUseInventory: false,
                    canUsePackAnimalInventory: false,
                    allowForbidden: false,
                    allowCorpse: false,
                    allowSociallyImproper: true,
                    allowHarvest: false,
                    forceScanWholeMap: true,
                    ignoreReservations: false,
                    calculateWantedStackCount: false,
                    allowVenerated: false))
            {
                return null;
            }

            if (foodSource == null || !foodSource.Spawned || !foodSource.IngestibleNow || !foodSource.def.IsNutritionGivingIngestible)
            {
                return null;
            }

            if (!pawn.CanReserveAndReach(foodSource, PathEndMode.ClosestTouch, Danger.Some))
            {
                return null;
            }

            Job takeInventory = JobMaker.MakeJob(JobDefOf.TakeInventory, foodSource);
            takeInventory.count = 1;
            takeInventory.takeInventoryDelay = 60;
            return takeInventory;
        }

        private static Job TryCreateRecoveryOrAssaultJob(Pawn pawn)
        {
            if (!MouseDisasterUtility.HasAnyStealableFoodOnMap(pawn?.Map))
            {
                return null;
            }

            Job recoveryJob = MouseDisasterUtility.TryCreatePathRecoveryJob(pawn);
            if (recoveryJob != null)
            {
                return recoveryJob;
            }

            if (MouseDisasterUtility.IsStrongSiegePawn(pawn))
            {
                MouseDisasterUtility.TryStartVisitorAssault(pawn);
            }

            return null;
        }
    }
}
