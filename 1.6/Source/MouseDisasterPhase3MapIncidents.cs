using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MouseDisaster
{
    internal static class MouseDisasterPhase3Utility
    {
        private static readonly string[][] PreferredRetaliationTraitKeywords =
        {
            new[] { "greed", "greedy", "贪" },
            new[] { "explosive", "blast", "爆" },
            new[] { "gas", "toxic", "毒" },
            new[] { "noble", "royal", "贵族" },
            new[] { "burn", "fire", "flame", "燃" }
        };

        public static Faction ResolveVisitorFaction()
        {
            MouseDisasterUtility.TryFindFormerFaction(out Faction faction);
            return faction;
        }

        public static Pawn CreatePregnantVisitor(Faction faction, bool infect)
        {
            Pawn pawn = MouseDisasterUtility.GenerateFactionRatkinPawn(MouseDisasterDefOf.MouseDisaster_BeggarRatkinAdult, faction, DevelopmentalStage.Adult, 0.24f);
            if (pawn == null || !ModsConfig.BiotechActive || pawn.gender != Gender.Female)
            {
                return pawn;
            }

            Hediff_Pregnant pregnancy = (Hediff_Pregnant)HediffMaker.MakeHediff(HediffDefOf.PregnantHuman, pawn);
            pregnancy.Severity = Rand.Range(0.72f, 0.94f);
            pregnancy.SetParents(pawn, null, PregnancyUtility.GetInheritedGeneSet(null, pawn));
            pawn.health.AddHediff(pregnancy);
            if (infect)
            {
                MouseDisasterPhase2Utility.InfectWithPlague(pawn);
            }

            return pawn;
        }

        public static void StartImmediateLabor(Pawn pawn)
        {
            Hediff_Pregnant pregnancy = PregnancyUtility.GetPregnancyHediff(pawn) as Hediff_Pregnant;
            if (pregnancy == null)
            {
                return;
            }

            pregnancy.StartLabor();
            pawn.health.RemoveHediff(pregnancy);
        }

        public static Pawn CreateRatEggPawn(Faction faction, bool babyStage, bool thiefLike, bool pureNegative, bool infect, float foodLevel = 0.12f)
        {
            Pawn pawn = thiefLike
                ? MouseDisasterUtility.GenerateThiefPawn(MouseDisasterDefOf.MouseDisaster_ThiefRatkinChild, faction, babyStage ? DevelopmentalStage.Baby : DevelopmentalStage.Child)
                : MouseDisasterUtility.GenerateBeggarPawn(MouseDisasterDefOf.MouseDisaster_BeggarRatkinChild, faction, babyStage ? DevelopmentalStage.Baby : DevelopmentalStage.Child);
            if (pawn == null)
            {
                return null;
            }

            MouseDisasterUtility.SetBiologicalAgeYears(pawn, babyStage ? Rand.Range(1.05f, 2.8f) : Rand.Range(3.1f, 6.8f));
            MouseDisasterUtility.StripRatEggInventory(pawn);
            if (pureNegative)
            {
                MouseDisasterUtility.MakeRatEggPureNegative(pawn, babyStage ? 2 : 3, PreferredRetaliationTraitKeywords);
            }

            if (infect)
            {
                MouseDisasterPhase2Utility.InfectWithPlague(pawn);
            }

            if (pawn.needs?.food != null)
            {
                pawn.needs.food.CurLevelPercentage = Mathf.Clamp01(foodLevel);
            }

            return pawn;
        }

        public static Pawn CreateMisguidedKinshipPawn(bool infect)
        {
            Pawn pawn = MouseDisasterUtility.GenerateWildPawn(MouseDisasterDefOf.MouseDisaster_WildRatkinChild, null, DevelopmentalStage.Child);
            if (pawn == null)
            {
                return null;
            }

            MouseDisasterUtility.MakeRatEggPureNegative(pawn, 3, PreferredRetaliationTraitKeywords);
            MouseDisasterUtility.StripRatEggInventory(pawn);
            if (infect)
            {
                MouseDisasterPhase2Utility.InfectWithPlague(pawn);
            }

            if (pawn.needs?.food != null)
            {
                pawn.needs.food.CurLevelPercentage = 0.18f;
            }

            return pawn;
        }

        public static Pawn CreateAnyAgeFaminePawn(Faction faction, bool infect)
        {
            float roll = Rand.Value;
            DevelopmentalStage stage = roll < 0.2f ? DevelopmentalStage.Baby : (roll < 0.65f ? DevelopmentalStage.Child : DevelopmentalStage.Adult);
            PawnKindDef kindDef = stage.Adult() ? MouseDisasterDefOf.MouseDisaster_ThiefRatkinAdult : MouseDisasterDefOf.MouseDisaster_ThiefRatkinChild;
            Pawn pawn = MouseDisasterUtility.GenerateThiefPawn(kindDef, faction, stage);
            if (pawn == null)
            {
                return null;
            }

            if (infect)
            {
                MouseDisasterPhase2Utility.InfectWithPlague(pawn);
            }

            if (pawn.needs?.food != null)
            {
                pawn.needs.food.CurLevelPercentage = 0.04f;
            }

            return pawn;
        }

        public static Pawn CreatePlagueRevengePawn(Faction faction)
        {
            Pawn pawn = CreateAnyAgeFaminePawn(faction, infect: true);
            if (pawn == null)
            {
                return null;
            }

            Hediff plague = pawn.health?.hediffSet?.GetFirstHediffOfDef(MouseDisasterDefOf.MouseDisaster_Plague);
            if (plague != null)
            {
                plague.Severity = Mathf.Max(plague.Severity, 0.5f);
            }

            Hediff bloodLoss = pawn.health.GetOrAddHediff(HediffDefOf.BloodLoss);
            bloodLoss.Severity = Mathf.Max(bloodLoss.Severity, 0.72f);
            HealthUtility.DamageUntilDowned(pawn, false);
            return pawn;
        }

        public static void PrepareAirdroppedEgg(Pawn pawn, int stayHours)
        {
            if (pawn == null)
            {
                return;
            }

            MouseDisasterUtility.SetBiologicalAgeYears(pawn, 3f / GenDate.DaysPerYear);
            MouseDisasterUtility.RegisterAirDropStayPawn(pawn, GenDate.TicksPerHour * stayHours);
            MouseDisasterUtility.ConfigureNoRescueJoinForIncidentVisitor(pawn);
        }

        public static void PrepareStrandedAirdroppedEgg(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            MouseDisasterUtility.SetBiologicalAgeYears(pawn, 3f / GenDate.DaysPerYear);
            MouseDisasterUtility.ConfigureNoRescueJoinForIncidentVisitor(pawn);
        }

        public static bool IsPlagueCarrier(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.GetFirstHediffOfDef(MouseDisasterDefOf.MouseDisaster_Plague) != null;
        }

        public static void TriggerEggBombRetaliation(Map map, int requestedCount, bool infect, Faction sourceFaction)
        {
            if (map == null)
            {
                return;
            }

            int actualCount = Mathf.Clamp(requestedCount, 4, 96);
            Faction faction = ResolveVisitorFaction();
            List<Thing> payload = new List<Thing>();
            for (int i = 0; i < actualCount; i++)
            {
                Pawn pawn = CreateRatEggPawn(faction, babyStage: true, thiefLike: true, pureNegative: true, infect: infect, foodLevel: 0.08f);
                if (pawn == null)
                {
                    continue;
                }

                PrepareAirdroppedEgg(pawn, 24);
                payload.Add(pawn);
            }

            if (payload.Count == 0)
            {
                return;
            }

            DropPodUtility.DropThingsNear(DropCellFinder.TradeDropSpot(map), map, payload, 110, canInstaDropDuringInit: false, leaveSlag: false, canRoofPunch: true, forbid: true, allowFogged: true, faction);
            Find.LetterStack.ReceiveLetter(
                "空投报复",
                sourceFaction == null
                    ? $"你最近空投了过量的鼠灾婴儿鼠蛋。现在，{payload.Count}只带着纯负面特性的鼠蛋被空投回了你的殖民地。"
                    : $"你对{sourceFaction.Name}的空投行为引来了报复。现在，{payload.Count}只带着纯负面特性的鼠蛋被空投回了你的殖民地。",
                infect ? LetterDefOf.ThreatBig : LetterDefOf.ThreatSmall,
                payload);
        }

        public static void SpawnPlagueRevengeWave(Map map, int count)
        {
            if (map == null)
            {
                return;
            }

            Faction faction = ResolveVisitorFaction();
            List<Thing> payload = new List<Thing>();
            for (int i = 0; i < Mathf.Max(1, count); i++)
            {
                Pawn pawn = CreatePlagueRevengePawn(faction);
                if (pawn != null)
                {
                    payload.Add(pawn);
                }
            }

            if (payload.Count > 0)
            {
                DropPodUtility.DropThingsNear(DropCellFinder.TradeDropSpot(map), map, payload, 110, canInstaDropDuringInit: false, leaveSlag: false, canRoofPunch: true, forbid: true, allowFogged: true, faction);
            }
        }
    }

    public abstract class IncidentWorker_MouseDisasterLaboringRefugeesBase : IncidentWorker
    {
        protected abstract bool InfectsWithPlague { get; }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map map &&
                   base.CanFireNowSub(parms) &&
                   ModsConfig.BiotechActive &&
                   MouseDisasterUtility.TryFindEntryCell(map, out _);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!MouseDisasterUtility.TryFindEntryCell(map, out IntVec3 entryCell))
            {
                return false;
            }

            Faction faction = MouseDisasterPhase3Utility.ResolveVisitorFaction();
            if (faction != null)
            {
                MouseDisasterUtility.MakeFactionNeutralToPlayer(faction, force: true);
            }

            int count = Rand.RangeInclusive(1, 3);
            List<Pawn> pawns = new List<Pawn>();
            for (int i = 0; i < count; i++)
            {
                Pawn pawn = MouseDisasterPhase3Utility.CreatePregnantVisitor(faction, InfectsWithPlague);
                if (pawn == null)
                {
                    continue;
                }

                GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(entryCell, map, 5), map);
                MouseDisasterPhase3Utility.StartImmediateLabor(pawn);
                pawn.jobs?.StartJob(MouseDisasterUtility.CreateGotoJob(map.Center), JobCondition.InterruptForced);
                pawns.Add(pawn);
            }

            if (pawns.Count == 0)
            {
                return false;
            }

            MouseDisasterVisitorUtility.RegisterVisitors(pawns);
            if (!MouseDisasterVisitorUtility.SendVisitorChoiceLetter(def, parms, map, pawns))
            {
                SendStandardLetter(def.letterLabel, def.letterText, def.letterDef, parms, pawns);
            }
            return true;
        }
    }

    public class IncidentWorker_MouseDisasterLaboringRefugees : IncidentWorker_MouseDisasterLaboringRefugeesBase
    {
        protected override bool InfectsWithPlague => false;
    }

    public class IncidentWorker_MouseDisasterPlagueLaboringRefugees : IncidentWorker_MouseDisasterLaboringRefugeesBase
    {
        protected override bool InfectsWithPlague => true;
    }

    public abstract class IncidentWorker_MouseDisasterStrongSiegeBase : IncidentWorker
    {
        protected abstract bool InfectsWithPlague { get; }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map map &&
                   base.CanFireNowSub(parms) &&
                   MouseDisasterUtility.TryFindEntryCell(map, out _);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!MouseDisasterUtility.TryFindEntryCell(map, out IntVec3 cell))
            {
                return false;
            }

            Faction faction = MouseDisasterPhase3Utility.ResolveVisitorFaction();
            if (faction != null)
            {
                MouseDisasterUtility.MakeFactionNeutralToPlayer(faction, force: true);
            }

            int count = MouseDisasterUtility.CalculateEscalatingGroupCount(parms.points, 4, 16, 80f);
            List<Pawn> pawns = new List<Pawn>(MouseDisasterUtility.SpawnThiefGroup(map, cell, count, childOnly: false));
            if (pawns.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                MouseDisasterUtility.RegisterStrongSiegePawn(pawns[i]);
            }

            if (InfectsWithPlague)
            {
                MouseDisasterPhase2Utility.InfectMany(pawns);
            }

            MouseDisasterVisitorUtility.RegisterVisitors(pawns);
            if (!MouseDisasterVisitorUtility.SendVisitorChoiceLetter(def, parms, map, pawns))
            {
                SendStandardLetter(def.letterLabel, def.letterText, def.letterDef, parms, pawns);
            }
            return true;
        }
    }

    public class IncidentWorker_MouseDisasterStrongSiege : IncidentWorker_MouseDisasterStrongSiegeBase
    {
        protected override bool InfectsWithPlague => false;
    }

    public class IncidentWorker_MouseDisasterPlagueStrongSiege : IncidentWorker_MouseDisasterStrongSiegeBase
    {
        protected override bool InfectsWithPlague => true;
    }

    public abstract class IncidentWorker_MouseDisasterPassersbyBase : IncidentWorker
    {
        protected abstract bool InfectsWithPlague { get; }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map map && base.CanFireNowSub(parms) && MouseDisasterUtility.TryFindEntryCell(map, out _);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!MouseDisasterUtility.TryFindEntryCell(map, out IntVec3 entryCell))
            {
                return false;
            }

            if (!MouseDisasterUtility.TryFindFarEdgeCell(map, entryCell, out IntVec3 exitCell))
            {
                exitCell = map.Center;
            }

            int total = MouseDisasterUtility.CalculateEscalatingGroupCount(parms.points, 4, 12, 85f);
            int children = Find.Storyteller.difficulty.ChildrenAllowed ? Mathf.Clamp(Mathf.RoundToInt(total * 0.35f), 0, total - 1) : 0;
            int adults = Mathf.Max(1, total - children);
            Faction faction = MouseDisasterPhase3Utility.ResolveVisitorFaction();
            if (faction != null)
            {
                MouseDisasterUtility.MakeFactionNeutralToPlayer(faction, force: true);
            }

            List<Pawn> pawns = new List<Pawn>(MouseDisasterUtility.SpawnTravelerGroup(map, entryCell, adults, children, faction));
            if (pawns.Count == 0)
            {
                return false;
            }

            if (InfectsWithPlague)
            {
                MouseDisasterPhase2Utility.InfectMany(pawns);
            }

            MouseDisasterUtility.MakeTravelAndExitLord(map, pawns, exitCell, includeBabiesInExit: false);
            MouseDisasterVisitorUtility.RegisterVisitors(pawns);
            if (!MouseDisasterVisitorUtility.SendVisitorChoiceLetter(def, parms, map, pawns))
            {
                SendStandardLetter(def.letterLabel, def.letterText, def.letterDef, parms, pawns);
            }
            return true;
        }
    }

    public class IncidentWorker_MouseDisasterPassersby : IncidentWorker_MouseDisasterPassersbyBase
    {
        protected override bool InfectsWithPlague => false;
    }

    public class IncidentWorker_MouseDisasterPlaguePassersbyPhase3 : IncidentWorker_MouseDisasterPassersbyBase
    {
        protected override bool InfectsWithPlague => true;
    }
}
