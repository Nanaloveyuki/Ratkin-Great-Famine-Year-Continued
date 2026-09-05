using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace MouseDisaster
{
    public enum MouseDisasterPawnBatchKind
    {
        LargeRefugeeWave,
        GreatFamine,
        TraderCaravan
    }

    public class MouseDisasterPawnBatch : IExposable
    {
        public MouseDisasterPawnBatchKind kind;
        public Map map;
        public Faction faction;
        public IncidentDef incidentDef;
        public IncidentParms parms;
        public IntVec3 entryCell;
        public int adultsRemaining;
        public int childrenRemaining;
        public int remainingCount;
        public int nextSpawnTick;
        public int intervalTicks = 1;
        public int randomSeed;
        public int generatedSlots;
        public bool infectsWithPlague;
        public List<Pawn> pawns = new List<Pawn>();
        public Pawn traderPawn;
        public Pawn escortPawn;

        public bool Complete => adultsRemaining <= 0 && childrenRemaining <= 0 && remainingCount <= 0;

        public void ExposeData()
        {
            Scribe_Values.Look(ref kind, "kind", MouseDisasterPawnBatchKind.LargeRefugeeWave);
            Scribe_References.Look(ref map, "map");
            Scribe_References.Look(ref faction, "faction");
            Scribe_Defs.Look(ref incidentDef, "incidentDef");
            Scribe_Deep.Look(ref parms, "parms");
            Scribe_Values.Look(ref entryCell, "entryCell", IntVec3.Invalid);
            Scribe_Values.Look(ref adultsRemaining, "adultsRemaining", 0);
            Scribe_Values.Look(ref childrenRemaining, "childrenRemaining", 0);
            Scribe_Values.Look(ref remainingCount, "remainingCount", 0);
            Scribe_Values.Look(ref nextSpawnTick, "nextSpawnTick", 0);
            Scribe_Values.Look(ref intervalTicks, "intervalTicks", 1);
            Scribe_Values.Look(ref randomSeed, "randomSeed", 0);
            Scribe_Values.Look(ref generatedSlots, "generatedSlots", 0);
            Scribe_Values.Look(ref infectsWithPlague, "infectsWithPlague", false);
            Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);
            Scribe_References.Look(ref traderPawn, "traderPawn");
            Scribe_References.Look(ref escortPawn, "escortPawn");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                pawns ??= new List<Pawn>();
                intervalTicks = Mathf.Max(1, intervalTicks);
            }
        }
    }

    public class GameComponent_MouseDisasterPawnGeneration : GameComponent
    {
        private const int TargetGenerationTicks = 100;

        private List<MouseDisasterPawnBatch> batches = new List<MouseDisasterPawnBatch>();

        public GameComponent_MouseDisasterPawnGeneration(Game game)
        {
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref batches, "mouseDisaster_pawnGenerationBatches", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                batches ??= new List<MouseDisasterPawnBatch>();
                batches.RemoveAll(batch => batch == null || batch.Complete);
            }
        }

        public override void GameComponentTick()
        {
            TickManager tickManager = Find.TickManager;
            if (tickManager == null || tickManager.TicksThisFrame != 0 || batches == null || batches.Count == 0)
            {
                return;
            }

            int nowTick = tickManager.TicksGame;
            for (int i = 0; i < batches.Count; i++)
            {
                MouseDisasterPawnBatch batch = batches[i];
                if (!IsBatchMapAvailable(batch))
                {
                    batches.RemoveAt(i);
                    i--;
                    continue;
                }

                if (nowTick < batch.nextSpawnTick)
                {
                    continue;
                }

                try
                {
                    ProcessNextPawn(batch);
                    if (batch.Complete)
                    {
                        FinalizeBatch(batch);
                        batches.RemoveAt(i);
                    }
                    else
                    {
                        batch.nextSpawnTick = nowTick + batch.intervalTicks;
                    }
                }
                catch (Exception exception)
                {
                    Log.Error("[MouseDisaster] Staged pawn generation failed for " + batch.kind + ": " + exception);
                    TryFinalizePartialBatch(batch);
                    batches.RemoveAt(i);
                }

                return;
            }
        }

        public static bool TryStartLargeRefugeeWave(IncidentDef incidentDef, IncidentParms parms, Map map, IntVec3 entryCell, int adults, int children)
        {
            MouseDisasterUtility.TryFindFormerFaction(out Faction faction);
            if (faction != null)
            {
                MouseDisasterUtility.MakeFactionNeutralToPlayer(faction, force: true);
                MouseDisasterUtility.EnsureMouseDisasterFactionNeutralOnMap(map, faction);
            }

            return TryStartBatch(new MouseDisasterPawnBatch
            {
                kind = MouseDisasterPawnBatchKind.LargeRefugeeWave,
                map = map,
                faction = faction,
                incidentDef = incidentDef,
                parms = parms?.ShallowCopy(),
                entryCell = entryCell,
                adultsRemaining = adults,
                childrenRemaining = children
            }, adults + children);
        }

        public static bool TryStartGreatFamine(IncidentDef incidentDef, IncidentParms parms, Map map, IntVec3 entryCell, Faction faction, int count, bool infectsWithPlague)
        {
            return TryStartBatch(new MouseDisasterPawnBatch
            {
                kind = MouseDisasterPawnBatchKind.GreatFamine,
                map = map,
                faction = faction,
                incidentDef = incidentDef,
                parms = parms?.ShallowCopy(),
                entryCell = entryCell,
                remainingCount = count,
                infectsWithPlague = infectsWithPlague
            }, count);
        }

        public static bool TryStartTraderCaravan(IncidentDef incidentDef, IncidentParms parms, Map map, IntVec3 entryCell, Faction faction, int saleChildren)
        {
            return TryStartBatch(new MouseDisasterPawnBatch
            {
                kind = MouseDisasterPawnBatchKind.TraderCaravan,
                map = map,
                faction = faction,
                incidentDef = incidentDef,
                parms = parms?.ShallowCopy(),
                entryCell = entryCell,
                remainingCount = saleChildren + 2
            }, saleChildren + 2);
        }

        private static bool TryStartBatch(MouseDisasterPawnBatch batch, int totalCount)
        {
            GameComponent_MouseDisasterPawnGeneration component = Current.Game?.GetComponent<GameComponent_MouseDisasterPawnGeneration>();
            if (component == null || !IsBatchMapAvailable(batch) || batch.incidentDef == null || batch.parms == null || totalCount <= 0)
            {
                return false;
            }

            batch.intervalTicks = Mathf.Max(1, Mathf.RoundToInt(TargetGenerationTicks / (float)totalCount));
            batch.randomSeed = Rand.Int;
            ProcessNextPawn(batch);
            if (batch.pawns.Count == 0)
            {
                return false;
            }

            if (batch.Complete)
            {
                FinalizeBatch(batch);
            }
            else
            {
                batch.nextSpawnTick = Find.TickManager.TicksGame + batch.intervalTicks;
                component.batches.Add(batch);
            }

            return true;
        }

        private static bool IsBatchMapAvailable(MouseDisasterPawnBatch batch)
        {
            return batch?.map != null && Find.Maps != null && Find.Maps.Contains(batch.map);
        }

        private static void ProcessNextPawn(MouseDisasterPawnBatch batch)
        {
            int slot = batch.generatedSlots++;
            Rand.PushState(Gen.HashCombineInt(batch.randomSeed, slot));
            try
            {
                switch (batch.kind)
                {
                    case MouseDisasterPawnBatchKind.LargeRefugeeWave:
                        GenerateLargeRefugee(batch);
                        break;
                    case MouseDisasterPawnBatchKind.GreatFamine:
                        GenerateGreatFaminePawn(batch);
                        break;
                    case MouseDisasterPawnBatchKind.TraderCaravan:
                        GenerateTraderCaravanPawn(batch, slot);
                        break;
                }
            }
            finally
            {
                Rand.PopState();
            }
        }

        private static void GenerateLargeRefugee(MouseDisasterPawnBatch batch)
        {
            DevelopmentalStage stage;
            PawnKindDef kindDef;
            if (batch.adultsRemaining > 0)
            {
                batch.adultsRemaining--;
                stage = DevelopmentalStage.Adult;
                kindDef = MouseDisasterDefOf.MouseDisaster_BeggarRatkinAdult;
            }
            else
            {
                batch.childrenRemaining--;
                stage = DevelopmentalStage.Child;
                kindDef = MouseDisasterDefOf.MouseDisaster_BeggarRatkinChild;
            }

            Pawn pawn = MouseDisasterUtility.GenerateBeggarPawn(kindDef, batch.faction, stage, allowViolenceDisabledTraits: true);
            if (pawn == null)
            {
                return;
            }

            GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(batch.entryCell, batch.map, 6), batch.map);
            MouseDisasterUtility.StripRatEggInventory(pawn);
            pawn.jobs?.StartJob(MouseDisasterUtility.CreateGotoJob(batch.map.Center), JobCondition.InterruptForced);
            pawn.health.AddHediff(stage == DevelopmentalStage.Adult
                ? MouseDisasterDefOf.MouseDisaster_LargeRefugeeAdult
                : MouseDisasterDefOf.MouseDisaster_LargeRefugeeChild);
            pawn.mindState?.mentalStateHandler?.Reset();
            batch.pawns.Add(pawn);
        }

        private static void GenerateGreatFaminePawn(MouseDisasterPawnBatch batch)
        {
            batch.remainingCount--;
            Pawn pawn = MouseDisasterPhase3Utility.CreateAnyAgeFaminePawn(batch.faction, batch.infectsWithPlague);
            if (pawn == null)
            {
                return;
            }

            GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(batch.entryCell, batch.map, 7), batch.map);
            pawn.health.AddHediff(pawn.DevelopmentalStage == DevelopmentalStage.Adult
                ? MouseDisasterDefOf.MouseDisaster_GreatFamineAdult
                : MouseDisasterDefOf.MouseDisaster_GreatFamineChild);
            pawn.mindState?.mentalStateHandler?.Reset();
            batch.pawns.Add(pawn);
        }

        private static void GenerateTraderCaravanPawn(MouseDisasterPawnBatch batch, int slot)
        {
            batch.remainingCount--;
            Pawn pawn;
            if (slot == 0)
            {
                pawn = IncidentWorker_RatkinTraderCaravan.GenerateFemaleTraderPawn(batch.faction);
            }
            else if (slot == 1)
            {
                pawn = MouseDisasterUtility.GenerateFactionRatkinPawn(MouseDisasterDefOf.MouseDisaster_TraderRatkinEscort, batch.faction, DevelopmentalStage.Adult, 0.6f);
            }
            else
            {
                pawn = MouseDisasterUtility.GenerateFactionRatkinPawn(MouseDisasterDefOf.MouseDisaster_BeggarRatkinChild, batch.faction, DevelopmentalStage.Baby, 0.5f);
                if (pawn != null)
                {
                    MouseDisasterUtility.SetBiologicalAgeYears(pawn, Rand.Range(1f, 2.9f));
                }
            }

            if (pawn == null)
            {
                return;
            }

            GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(batch.entryCell, batch.map, 6), batch.map);
            if (slot == 0)
            {
                batch.traderPawn = pawn;
                MouseDisasterUtility.EnsureTradeLeader(pawn, MouseDisasterUtility.ResolveSlaveTraderKind());
                pawn.health.AddHediff(MouseDisasterDefOf.MouseDisaster_TraderCaravanTrader);
            }
            else if (slot == 1)
            {
                batch.escortPawn = pawn;
                pawn.health.AddHediff(MouseDisasterDefOf.MouseDisaster_TraderCaravanEscort);
            }
            else
            {
                MouseDisasterUtility.StripRatEggInventory(pawn);
                IncidentWorker_RatkinTraderCaravan.PrepareChattelChild(pawn, batch.faction);
                pawn.health.AddHediff(MouseDisasterDefOf.MouseDisaster_TraderCaravanChild);
            }

            batch.pawns.Add(pawn);
        }

        private static void FinalizeBatch(MouseDisasterPawnBatch batch)
        {
            List<Pawn> pawns = ActivePawns(batch);
            if (pawns.Count == 0)
            {
                Log.Warning("[MouseDisaster] Staged pawn generation completed without any active pawns for " + batch.kind + ".");
                return;
            }

            switch (batch.kind)
            {
                case MouseDisasterPawnBatchKind.LargeRefugeeWave:
                case MouseDisasterPawnBatchKind.GreatFamine:
                    FinalizeAssaultBatch(batch, pawns);
                    break;
                case MouseDisasterPawnBatchKind.TraderCaravan:
                    FinalizeTraderCaravan(batch, pawns);
                    break;
            }
        }

        private static void TryFinalizePartialBatch(MouseDisasterPawnBatch batch)
        {
            if (batch.kind != MouseDisasterPawnBatchKind.TraderCaravan)
            {
                FinalizeBatch(batch);
            }
        }

        private static void FinalizeAssaultBatch(MouseDisasterPawnBatch batch, List<Pawn> pawns)
        {
            Faction faction = batch.faction ?? pawns[0].Faction;
            if (faction != null)
            {
                MouseDisasterUtility.MakeFactionHostileToPlayer(faction, explicitDriveAway: true);
                LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction, canKidnap: false, canTimeoutOrFlee: false, canSteal: true, breachers: true), batch.map, pawns);
            }

            IncidentWorker.SendIncidentLetter(batch.incidentDef.letterLabel, batch.incidentDef.letterText, batch.incidentDef.letterDef, batch.parms, pawns, batch.incidentDef);
        }

        private static void FinalizeTraderCaravan(MouseDisasterPawnBatch batch, List<Pawn> pawns)
        {
            Pawn traderPawn = batch.traderPawn;
            Pawn escortPawn = batch.escortPawn;
            List<Pawn> children = batch.pawns.Where(pawn => pawn != traderPawn && pawn != escortPawn && pawns.Contains(pawn)).ToList();
            if (traderPawn == null || escortPawn == null || children.Count == 0 || !pawns.Contains(traderPawn) || !pawns.Contains(escortPawn))
            {
                Log.Warning("[MouseDisaster] Trader caravan generation did not produce its required members.");
                return;
            }

            MouseDisasterUtility.MarkChildExchangeMoodChildren(children);
            List<Pawn> childrenWithMother = children
                .InRandomOrder()
                .Take(MouseDisasterGeneRestorePolicy.ResolveTraderCaravanChildrenToLinkCount(children.Count))
                .ToList();
            MouseDisasterUtility.LinkIncidentParentToChildren(traderPawn, childrenWithMother);
            MouseDisasterUtility.TryStartLeadYourPetRelatedAdultLeashes(pawns);
            IncidentWorker_RatkinTraderCaravan.FillTraderInventory(traderPawn);

            if (!RCellFinder.TryFindRandomSpotJustOutsideColony(traderPawn.Position, batch.map, traderPawn, out IntVec3 exitCell))
            {
                exitCell = batch.map.Center;
            }

            Lord tradeLord = LordMaker.MakeNewLord(batch.faction, new LordJob_TradeWithColony(batch.faction, exitCell), batch.map, pawns);
            if (tradeLord != null)
            {
                MouseDisasterUtility.TryAssignLeadYourPetTravelMouseEggs(tradeLord);
            }

            MouseDisasterUtility.EnsureMouseDisasterFactionNeutralOnMap(batch.map, batch.faction);
            MouseDisasterVisitorUtility.RegisterVisitors(pawns);
            if (!MouseDisasterVisitorUtility.SendVisitorChoiceLetter(batch.incidentDef, batch.parms, batch.map, pawns))
            {
                IncidentWorker.SendIncidentLetter(batch.incidentDef.letterLabel, batch.incidentDef.letterText, batch.incidentDef.letterDef, batch.parms, pawns, batch.incidentDef);
            }
        }

        private static List<Pawn> ActivePawns(MouseDisasterPawnBatch batch)
        {
            return batch.pawns
                .Where(pawn => pawn != null && !pawn.Dead && !pawn.Destroyed && pawn.Spawned && pawn.Map == batch.map)
                .Distinct()
                .ToList();
        }
    }
}
