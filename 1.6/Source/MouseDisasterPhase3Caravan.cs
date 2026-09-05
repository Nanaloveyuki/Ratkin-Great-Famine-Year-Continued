using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace MouseDisaster
{
    internal static class MouseDisasterPhase3CaravanUtility
    {
        public static List<ThingCount> GenerateCaravanDemands(Caravan caravan, float targetValue)
        {
            List<Thing> candidates = caravan.AllThings
                .Where(thing =>
                    thing != null &&
                    !(thing is Pawn) &&
                    thing.stackCount > 0 &&
                    thing.def.category == ThingCategory.Item &&
                    (thing.def.IsNutritionGivingIngestible || thing.def.IsMedicine || thing.def == ThingDefOf.Silver))
                .OrderByDescending(thing => thing.MarketValue)
                .ToList();

            List<ThingCount> demands = new List<ThingCount>();
            float remainingValue = Mathf.Max(120f, targetValue);
            for (int i = 0; i < candidates.Count && remainingValue > 30f; i++)
            {
                Thing thing = candidates[i];
                int count = Mathf.Clamp(Mathf.CeilToInt(remainingValue / Mathf.Max(1f, thing.MarketValue)), 1, thing.stackCount);
                demands.Add(new ThingCount(thing, count));
                remainingValue -= thing.MarketValue * count;
            }

            return demands;
        }

        public static void TakeDemandFromCaravan(Caravan caravan, List<ThingCount> demands)
        {
            if (caravan == null || demands.NullOrEmpty())
            {
                return;
            }

            for (int i = 0; i < demands.Count; i++)
            {
                ThingCount demand = demands[i];
                demand.Thing?.SplitOff(demand.Count).Destroy();
            }
        }

        public static List<Pawn> CreateCaravanMuggerParty(Faction faction, float points, bool infect)
        {
            int total = MouseDisasterUtility.CalculateEscalatingGroupCount(points, 3, 10, 85f);
            int children = Find.Storyteller.difficulty.ChildrenAllowed ? Mathf.Clamp(Mathf.RoundToInt(total * 0.35f), 0, total - 1) : 0;
            int adults = Mathf.Max(1, total - children);
            List<Pawn> pawns = new List<Pawn>();
            for (int i = 0; i < adults; i++)
            {
                Pawn pawn = MouseDisasterUtility.GenerateFactionRatkinPawn(MouseDisasterDefOf.MouseDisaster_BeggarRatkinAdult, faction, DevelopmentalStage.Adult, 0.28f);
                if (pawn == null)
                {
                    continue;
                }

                if (infect)
                {
                    MouseDisasterPhase2Utility.InfectWithPlague(pawn);
                }

                pawns.Add(pawn);
            }

            for (int i = 0; i < children; i++)
            {
                Pawn pawn = MouseDisasterPhase3Utility.CreateRatEggPawn(faction, babyStage: false, thiefLike: true, pureNegative: false, infect: infect, foodLevel: 0.16f);
                if (pawn != null)
                {
                    pawns.Add(pawn);
                }
            }

            return pawns;
        }
    }

    public abstract class IncidentWorker_MouseDisasterCaravanMuggersBase : IncidentWorker
    {
        protected abstract bool InfectsWithPlague { get; }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Caravan caravan &&
                   caravan.Visibility >= 0.5f &&
                   CaravanIncidentUtility.CanFireIncidentWhichWantsToGenerateMapAt(caravan.Tile) &&
                   base.CanFireNowSub(parms) &&
                   MouseDisasterPhase3Utility.ResolveVisitorFaction() != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Caravan caravan = (Caravan)parms.target;
            Faction faction = MouseDisasterPhase3Utility.ResolveVisitorFaction();
            List<ThingCount> demands = MouseDisasterPhase3CaravanUtility.GenerateCaravanDemands(caravan, caravan.PlayerWealthForStoryteller * Rand.Range(0.03f, 0.08f));
            if (demands.NullOrEmpty())
            {
                return false;
            }

            List<Pawn> attackers = MouseDisasterPhase3CaravanUtility.CreateCaravanMuggerParty(faction, parms.points, InfectsWithPlague);
            if (attackers.Count == 0)
            {
                return false;
            }

            CameraJumper.TryJumpAndSelect(caravan);
            DiaNode root = new DiaNode($"{faction.Name}派来的流民拦住了你的远行队。它们要求交出{GenLabel.ThingsLabel(demands)}，否则就会动手。");

            DiaOption give = new DiaOption("交出物资");
            give.action = delegate
            {
                MouseDisasterPhase3CaravanUtility.TakeDemandFromCaravan(caravan, demands);
                for (int i = 0; i < attackers.Count; i++)
                {
                    Find.WorldPawns.PassToWorld(attackers[i], PawnDiscardDecideMode.Discard);
                }
            };
            give.resolveTree = true;
            root.options.Add(give);

            DiaOption fight = new DiaOption("不交，开打");
            fight.action = delegate
            {
                TaleRecorder.RecordTale(TaleDefOf.CaravanAmbushedByHumanlike, caravan.RandomOwner());
                LongEventHandler.QueueLongEvent(delegate
                {
                    Map map = CaravanIncidentUtility.SetupCaravanAttackMap(caravan, attackers, sendLetterIfRelatedPawns: true);
                    LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction, canKidnap: true, canTimeoutOrFlee: false), map, attackers);
                    Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
                    CameraJumper.TryJump(attackers[0]);
                }, "GeneratingMapForNewEncounter", doAsynchronously: false, null);
            };
            fight.resolveTree = true;
            root.options.Add(fight);

            TaggedString title = def.letterLabel.NullOrEmpty() ? "流民抢夺商队" : def.letterLabel;
            Find.WindowStack.Add(new Dialog_NodeTreeWithFactionInfo(root, faction, delayInteractivity: true, radioMode: false, title));
            Find.Archive.Add(new ArchivedDialog(root.text, title, faction));
            return true;
        }
    }

    public class IncidentWorker_MouseDisasterCaravanMuggers : IncidentWorker_MouseDisasterCaravanMuggersBase
    {
        protected override bool InfectsWithPlague => false;
    }

    public class IncidentWorker_MouseDisasterPlagueCaravanMuggers : IncidentWorker_MouseDisasterCaravanMuggersBase
    {
        protected override bool InfectsWithPlague => true;
    }

    [HarmonyPatch(typeof(TransportersArrivalAction_AttackSettlement), nameof(TransportersArrivalAction_AttackSettlement.Arrived))]
    public static class MouseDisasterAttackSettlementRetaliationPatch
    {
        private static readonly AccessTools.FieldRef<TransportersArrivalAction_AttackSettlement, Settlement> SettlementField =
            AccessTools.FieldRefAccess<TransportersArrivalAction_AttackSettlement, Settlement>("settlement");

        public static void Prefix(TransportersArrivalAction_AttackSettlement __instance, List<ActiveTransporterInfo> transporters)
        {
            int total = CountEggs(transporters, out bool plague);
            if (total > 0)
            {
                Current.Game.GetComponent<GameComponent_MouseDisasterPhase3>()?.RecordAirdroppedEggs(SettlementField(__instance)?.Faction, total, plague);
            }
        }

        private static int CountEggs(IEnumerable<ActiveTransporterInfo> transporters, out bool plague)
        {
            plague = false;
            int total = 0;
            foreach (ActiveTransporterInfo transporter in transporters)
            {
                if (transporter?.innerContainer == null)
                {
                    continue;
                }

                for (int i = 0; i < transporter.innerContainer.Count; i++)
                {
                    if (!(transporter.innerContainer[i] is Pawn pawn) || !MouseDisasterUtility.IsMouseEggBaby(pawn))
                    {
                        continue;
                    }

                    total++;
                    plague |= MouseDisasterPhase3Utility.IsPlagueCarrier(pawn);
                }
            }

            return total;
        }
    }

    [HarmonyPatch(typeof(TransportersArrivalAction_FormCaravan), nameof(TransportersArrivalAction_FormCaravan.Arrived))]
    public static class MouseDisasterVisitSettlementRetaliationPatch
    {
        private static readonly AccessTools.FieldRef<TransportersArrivalAction_VisitSettlement, Settlement> SettlementField =
            AccessTools.FieldRefAccess<TransportersArrivalAction_VisitSettlement, Settlement>("settlement");

        public static void Prefix(TransportersArrivalAction_FormCaravan __instance, List<ActiveTransporterInfo> transporters)
        {
            if (!(__instance is TransportersArrivalAction_VisitSettlement visitSettlement))
            {
                return;
            }

            int total = CountEggs(transporters, out bool plague);
            if (total > 0)
            {
                Current.Game.GetComponent<GameComponent_MouseDisasterPhase3>()?.RecordAirdroppedEggs(SettlementField(visitSettlement)?.Faction, total, plague);
            }
        }

        private static int CountEggs(IEnumerable<ActiveTransporterInfo> transporters, out bool plague)
        {
            plague = false;
            int total = 0;
            foreach (ActiveTransporterInfo transporter in transporters)
            {
                if (transporter?.innerContainer == null)
                {
                    continue;
                }

                for (int i = 0; i < transporter.innerContainer.Count; i++)
                {
                    if (!(transporter.innerContainer[i] is Pawn pawn) || !MouseDisasterUtility.IsMouseEggBaby(pawn))
                    {
                        continue;
                    }

                    total++;
                    plague |= MouseDisasterPhase3Utility.IsPlagueCarrier(pawn);
                }
            }

            return total;
        }
    }
}
