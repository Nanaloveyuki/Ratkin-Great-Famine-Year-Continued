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
        public int deadlineTick;
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
            Scribe_Values.Look(ref deadlineTick, "deadlineTick", 0);
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
        private const int MaxBatchLifetimeTicks = 10000;
        private const int MaxGeneratedSlots = 64;

        private List<MouseDisasterPawnBatch> batches = new List<MouseDisasterPawnBatch>();

        public GameComponent_MouseDisasterPawnGeneration(Game game)
        {
        }

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving && batches != null)
            {
                batches.RemoveAll(batch => !IsBatchMapAvailable(batch));
            }

            Scribe_Collections.Look(ref batches, "mouseDisaster_pawnGenerationBatches", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                batches ??= new List<MouseDisasterPawnBatch>();
                batches.RemoveAll(batch => batch == null);
                int nowTick = Find.TickManager?.TicksGame ?? 0;
                for (int i = 0; i < batches.Count; i++)
                {
                    NormalizeLoadedBatch(batches[i], nowTick);
                }
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
                    Log.Warning("[MouseDisaster] Discarding staged pawn generation because its map was removed: " + (batch?.kind.ToString() ?? "unknown") + ".");
                    batches.RemoveAt(i);
                    i--;
                    continue;
                }

                if (!HasRequiredState(batch))
                {
                    TryTruncateBatch(batch, "required saved state is missing");
                    batches.RemoveAt(i);
                    return;
                }

                if (batch.Complete)
                {
                    FinalizeBatch(batch);
                    batches.RemoveAt(i);
                    return;
                }

                if (batch.generatedSlots >= MaxGeneratedSlots || nowTick >= batch.deadlineTick)
                {
                    TryTruncateBatch(batch, "generation limit or deadline reached");
                    batches.RemoveAt(i);
                    return;
                }

                if (nowTick < batch.nextSpawnTick)
                {
                    continue;
                }

                bool finalizing = false;
                try
                {
                    ProcessNextPawn(batch);
                    if (batch.Complete)
                    {
                        finalizing = true;
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
                    if (!finalizing)
                    {
                        TryTruncateBatch(batch, "an exception interrupted generation");
                    }
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
            if (!MouseDisasterRuntime.AllowsNewContent || component == null || !IsBatchMapAvailable(batch) || batch.incidentDef == null || batch.parms == null || totalCount <= 0)
            {
                return false;
            }

            batch.intervalTicks = Mathf.Max(1, Mathf.RoundToInt(TargetGenerationTicks / (float)totalCount));
            batch.randomSeed = Rand.Int;
            batch.deadlineTick = Find.TickManager.TicksGame + MaxBatchLifetimeTicks;
            try
            {
                ProcessNextPawn(batch);
            }
            catch (Exception exception)
            {
                Log.Error("[MouseDisaster] Could not start staged pawn generation for " + batch.kind + ": " + exception);
                TryTruncateBatch(batch, "the initial generation step failed");
                return false;
            }

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
            return batch?.map != null && !batch.map.Disposed && Find.Maps != null && Find.Maps.Contains(batch.map);
        }

        private static bool HasRequiredState(MouseDisasterPawnBatch batch)
        {
            return batch != null &&
                   Enum.IsDefined(typeof(MouseDisasterPawnBatchKind), batch.kind) &&
                   batch.incidentDef != null &&
                   batch.parms != null &&
                   batch.entryCell.IsValid;
        }

        private static void NormalizeLoadedBatch(MouseDisasterPawnBatch batch, int nowTick)
        {
            batch.intervalTicks = Mathf.Clamp(batch.intervalTicks, 1, TargetGenerationTicks);
            batch.generatedSlots = Mathf.Clamp(batch.generatedSlots, 0, MaxGeneratedSlots);
            batch.pawns ??= new List<Pawn>();
            batch.pawns.RemoveAll(pawn => pawn == null);

            switch (batch.kind)
            {
                case MouseDisasterPawnBatchKind.LargeRefugeeWave:
                    batch.adultsRemaining = Mathf.Clamp(batch.adultsRemaining, 0, 50);
                    batch.childrenRemaining = Mathf.Clamp(batch.childrenRemaining, 0, 50 - batch.adultsRemaining);
                    batch.remainingCount = 0;
                    break;
                case MouseDisasterPawnBatchKind.GreatFamine:
                    batch.adultsRemaining = 0;
                    batch.childrenRemaining = 0;
                    batch.remainingCount = Mathf.Clamp(batch.remainingCount, 0, 28);
                    break;
                case MouseDisasterPawnBatchKind.TraderCaravan:
                    batch.adultsRemaining = 0;
                    batch.childrenRemaining = 0;
                    batch.remainingCount = Mathf.Clamp(batch.remainingCount, 0, 32);
                    break;
            }

            if (batch.deadlineTick <= 0)
            {
                batch.deadlineTick = nowTick + MaxBatchLifetimeTicks;
            }
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
                    default:
                        throw new InvalidOperationException("Unknown pawn batch kind: " + batch.kind);
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
            batch.pawns.Add(pawn);
            MouseDisasterUtility.StripRatEggInventory(pawn);
            pawn.jobs?.StartJob(MouseDisasterUtility.CreateGotoJob(batch.map.Center), JobCondition.InterruptForced);
            pawn.health.AddHediff(stage == DevelopmentalStage.Adult
                ? MouseDisasterDefOf.MouseDisaster_LargeRefugeeAdult
                : MouseDisasterDefOf.MouseDisaster_LargeRefugeeChild);
            pawn.mindState?.mentalStateHandler?.Reset();
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
            batch.pawns.Add(pawn);
            pawn.health.AddHediff(pawn.DevelopmentalStage == DevelopmentalStage.Adult
                ? MouseDisasterDefOf.MouseDisaster_GreatFamineAdult
                : MouseDisasterDefOf.MouseDisaster_GreatFamineChild);
            pawn.mindState?.mentalStateHandler?.Reset();
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
            batch.pawns.Add(pawn);
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
                    if (!FinalizeTraderCaravan(batch, pawns))
                    {
                        SendIncompleteTraderCaravanAway(batch, pawns);
                    }
                    break;
            }
        }

        private static void TruncateBatch(MouseDisasterPawnBatch batch, string reason)
        {
            Log.Warning("[MouseDisaster] Truncating staged pawn generation for " + batch.kind + ": " + reason + ".");
            List<Pawn> pawns = ActivePawns(batch);
            if (pawns.Count == 0 || !IsBatchMapAvailable(batch))
            {
                return;
            }

            if (batch.kind == MouseDisasterPawnBatchKind.TraderCaravan)
            {
                if (!HasRequiredState(batch) || !FinalizeTraderCaravan(batch, pawns))
                {
                    SendIncompleteTraderCaravanAway(batch, pawns);
                }
                return;
            }

            Faction faction = batch.faction ?? pawns[0].Faction;
            if (faction != null)
            {
                MouseDisasterUtility.MakeFactionHostileToPlayer(faction, explicitDriveAway: true);
                LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction, canKidnap: false, canTimeoutOrFlee: false, canSteal: true, breachers: true), batch.map, pawns);
            }

            if (HasRequiredState(batch))
            {
                SendBatchLetter(batch, pawns);
            }
        }

        private static void TryTruncateBatch(MouseDisasterPawnBatch batch, string reason)
        {
            try
            {
                TruncateBatch(batch, reason);
            }
            catch (Exception exception)
            {
                Log.Error("[MouseDisaster] Could not finish truncating staged pawn generation for " + (batch?.kind.ToString() ?? "unknown") + ": " + exception);
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

            SendBatchLetter(batch, pawns);
        }

        private static bool FinalizeTraderCaravan(MouseDisasterPawnBatch batch, List<Pawn> pawns)
        {
            Pawn traderPawn = batch.traderPawn;
            Pawn escortPawn = batch.escortPawn;
            List<Pawn> children = batch.pawns.Where(pawn => pawn != traderPawn && pawn != escortPawn && pawns.Contains(pawn)).ToList();
            if (traderPawn == null || escortPawn == null || children.Count == 0 || !pawns.Contains(traderPawn) || !pawns.Contains(escortPawn))
            {
                Log.Warning("[MouseDisaster] Trader caravan generation did not produce its required members.");
                return false;
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
            return true;
        }

        private static void SendIncompleteTraderCaravanAway(MouseDisasterPawnBatch batch, List<Pawn> pawns)
        {
            MouseDisasterUtility.EnsureMouseDisasterFactionNeutralOnMap(batch.map, batch.faction);
            MouseDisasterUtility.MakeTravelAndExitLord(batch.map, pawns, batch.map.Center);
        }

        private static void SendBatchLetter(MouseDisasterPawnBatch batch, List<Pawn> pawns)
        {
            IncidentWorker.SendIncidentLetter(batch.incidentDef.letterLabel, batch.incidentDef.letterText, batch.incidentDef.letterDef, batch.parms, pawns, batch.incidentDef);
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
