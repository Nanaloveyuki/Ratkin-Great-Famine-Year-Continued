using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace MouseDisaster
{
    public abstract class IncidentWorker_MouseDisasterAirdropMistakeBase : IncidentWorker
    {
        protected abstract bool InfectsWithPlague { get; }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map map &&
                   base.CanFireNowSub(parms) &&
                   MouseDisasterUtility.TryFindEntryCell(map, out _) &&
                   Find.Storyteller.difficulty.ChildrenAllowed;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            Faction faction = MouseDisasterPhase3Utility.ResolveVisitorFaction();
            List<Thing> payload = new List<Thing>();
            int count = MouseDisasterUtility.CalculateEscalatingGroupCount(parms.points, 4, 14, 80f);
            for (int i = 0; i < count; i++)
            {
                Pawn pawn = MouseDisasterPhase3Utility.CreateRatEggPawn(faction, babyStage: true, thiefLike: false, pureNegative: false, infect: InfectsWithPlague, foodLevel: 0.14f);
                if (pawn == null)
                {
                    continue;
                }

                MouseDisasterPhase3Utility.PrepareStrandedAirdroppedEgg(pawn);
                payload.Add(pawn);
            }

            if (payload.Count == 0)
            {
                return false;
            }

            DropPodUtility.DropThingsNear(DropCellFinder.TradeDropSpot(map), map, payload, 110, canInstaDropDuringInit: false, leaveSlag: false, canRoofPunch: true, forbid: true, allowFogged: true, faction);
            SendStandardLetter(def.letterLabel, def.letterText, def.letterDef, parms, payload);
            return true;
        }
    }

    public class IncidentWorker_MouseDisasterAirdropMistake : IncidentWorker_MouseDisasterAirdropMistakeBase
    {
        protected override bool InfectsWithPlague => false;
    }

    public class IncidentWorker_MouseDisasterPlagueAirdropMistake : IncidentWorker_MouseDisasterAirdropMistakeBase
    {
        protected override bool InfectsWithPlague => true;
    }

    public class ChoiceLetter_MouseDisasterMisguidedKinship : ChoiceLetter
    {
        public List<Pawn> babies;
        public Map map;

        public override bool CanDismissWithRightClick => false;

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                if (ArchivedOnly)
                {
                    yield return Option_Close;
                    yield break;
                }

                List<Pawn> valid = babies?.Where(pawn => pawn != null && !pawn.Dead && pawn.Spawned).ToList() ?? new List<Pawn>();
                if (valid.Count == 0)
                {
                    yield return Option_Close;
                    yield break;
                }

                DiaOption accept = new DiaOption("接受认亲");
                accept.action = delegate
                {
                    for (int i = 0; i < valid.Count; i++)
                    {
                        valid[i].SetFaction(Faction.OfPlayer);
                        MouseDisasterUtility.NotifyMouseDisasterPawnIdentityOrLifeStageChanged(valid[i]);
                    }

                    Messages.Message("你接受了这群鼠蛋的认亲。", valid, MessageTypeDefOf.PositiveEvent, historical: false);
                    Find.LetterStack.RemoveLetter(this);
                };
                accept.resolveTree = true;
                yield return accept;

                DiaOption reject = new DiaOption("拒绝");
                reject.action = delegate
                {
                    Messages.Message("你拒绝了这群鼠蛋。它们把这里当成了新鼠窝，已经开始在殖民地里乱窜找吃的。", valid, MessageTypeDefOf.NeutralEvent, historical: false);
                    Find.LetterStack.RemoveLetter(this);
                };
                reject.resolveTree = true;
                yield return reject;

                if (lookTargets.IsValid())
                {
                    yield return Option_JumpToLocationAndPostpone;
                }

                yield return Option_Postpone;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref babies, "babies", LookMode.Reference);
            Scribe_References.Look(ref map, "map");
        }
    }

    public abstract class IncidentWorker_MouseDisasterMisguidedKinshipBase : IncidentWorker
    {
        protected abstract bool InfectsWithPlague { get; }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map map &&
                   base.CanFireNowSub(parms) &&
                   Find.Storyteller.difficulty.ChildrenAllowed &&
                   MouseDisasterUtility.TryFindEntryCell(map, out _);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!MouseDisasterUtility.TryFindEntryCell(map, out IntVec3 entryCell))
            {
                return false;
            }

            List<Pawn> babies = new List<Pawn>();
            for (int i = 0; i < Rand.RangeInclusive(2, 5); i++)
            {
                Pawn pawn = MouseDisasterPhase3Utility.CreateMisguidedKinshipPawn(InfectsWithPlague);
                if (pawn == null)
                {
                    continue;
                }

                GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(entryCell, map, 3), map);
                babies.Add(pawn);
            }

            if (babies.Count == 0)
            {
                return false;
            }

            ChoiceLetter_MouseDisasterMisguidedKinship letter =
                LetterMaker.MakeLetter(def.letterLabel, def.letterText, DefDatabase<LetterDef>.GetNamed("MouseDisaster_MisguidedKinshipLetter"), babies) as ChoiceLetter_MouseDisasterMisguidedKinship;
            if (letter == null)
            {
                return false;
            }

            letter.babies = babies;
            letter.map = map;
            Find.LetterStack.ReceiveLetter(letter);
            return true;
        }
    }

    public class IncidentWorker_MouseDisasterMisguidedKinship : IncidentWorker_MouseDisasterMisguidedKinshipBase
    {
        protected override bool InfectsWithPlague => false;
    }

    public class IncidentWorker_MouseDisasterPlagueMisguidedKinship : IncidentWorker_MouseDisasterMisguidedKinshipBase
    {
        protected override bool InfectsWithPlague => true;
    }

    public abstract class IncidentWorker_MouseDisasterGreatFamineBase : IncidentWorker
    {
        protected abstract bool InfectsWithPlague { get; }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map map &&
                   base.CanFireNowSub(parms) &&
                   Find.Storyteller.difficulty.ChildrenAllowed &&
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
                MouseDisasterUtility.MakeFactionHostileToPlayer(faction, explicitDriveAway: true);
            }

            int count = MouseDisasterUtility.CalculateEscalatingGroupCount(parms.points, 8, 28, 55f);
            return GameComponent_MouseDisasterPawnGeneration.TryStartGreatFamine(def, parms, map, entryCell, faction, count, InfectsWithPlague);
        }
    }

    public class IncidentWorker_MouseDisasterGreatFamine : IncidentWorker_MouseDisasterGreatFamineBase
    {
        protected override bool InfectsWithPlague => false;
    }

    public class IncidentWorker_MouseDisasterPlagueGreatFamine : IncidentWorker_MouseDisasterGreatFamineBase
    {
        protected override bool InfectsWithPlague => true;
    }

    public class IncidentWorker_MouseDisasterPlagueRevenge : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map map && base.CanFireNowSub(parms) && map.IsPlayerHome;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            MouseDisasterPhase3Utility.SpawnPlagueRevengeWave(map, 16);
            SendStandardLetter(def.letterLabel, def.letterText, def.letterDef, parms, new TargetInfo(map.Center, map));
            return true;
        }
    }

    public class MouseDisasterEggAirdropRecord : IExposable
    {
        public int factionId;
        public int tick;
        public int count;
        public bool plague;

        public void ExposeData()
        {
            Scribe_Values.Look(ref factionId, "factionId", 0);
            Scribe_Values.Look(ref tick, "tick", 0);
            Scribe_Values.Look(ref count, "count", 0);
            Scribe_Values.Look(ref plague, "plague", false);
        }
    }

    public class GameComponent_MouseDisasterPhase3 : GameComponent
    {
        private const int EggRetaliationWindowTicks = GenDate.TicksPerDay * 3;

        private List<MouseDisasterEggAirdropRecord> eggAirdropRecords = new List<MouseDisasterEggAirdropRecord>();

        public GameComponent_MouseDisasterPhase3(Game game)
        {
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref eggAirdropRecords, "mouseDisaster_eggAirdropRecords", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                eggAirdropRecords ??= new List<MouseDisasterEggAirdropRecord>();
            }

            if (Scribe.mode != LoadSaveMode.Saving)
            {
                PruneExpiredAirdropRecords();
            }
        }

        public void RecordAirdroppedEggs(Faction faction, int eggCount, bool plague)
        {
            if (faction == null || eggCount <= 0)
            {
                return;
            }

            int nowTick = Find.TickManager?.TicksGame ?? 0;
            PruneExpiredAirdropRecords(nowTick);
            eggAirdropRecords.Add(new MouseDisasterEggAirdropRecord
            {
                factionId = faction.loadID,
                tick = nowTick,
                count = eggCount,
                plague = plague
            });

            List<MouseDisasterEggAirdropRecord> recent = eggAirdropRecords
                .Where(record => record != null && record.factionId == faction.loadID && nowTick - record.tick <= EggRetaliationWindowTicks)
                .ToList();
            int total = recent.Sum(record => record.count);
            if (total <= 15)
            {
                return;
            }

            Map map = Find.AnyPlayerHomeMap ?? Find.CurrentMap;
            if (map != null)
            {
                MouseDisasterPhase3Utility.TriggerEggBombRetaliation(map, total * 4, recent.Any(record => record.plague), faction);
            }

            eggAirdropRecords.RemoveAll(record => record != null && record.factionId == faction.loadID);
        }

        private void PruneExpiredAirdropRecords()
        {
            PruneExpiredAirdropRecords(Find.TickManager?.TicksGame ?? 0);
        }

        private void PruneExpiredAirdropRecords(int nowTick)
        {
            eggAirdropRecords ??= new List<MouseDisasterEggAirdropRecord>();
            eggAirdropRecords.RemoveAll(record => record == null || MouseDisasterGeneRestorePolicy.ShouldPruneExpiredRecord(nowTick, record.tick, EggRetaliationWindowTicks));
        }
    }

    [HarmonyPatch(typeof(TransportersArrivalAction_GiveGift), nameof(TransportersArrivalAction_GiveGift.Arrived))]
    public static class MouseDisasterGiftAirdropRetaliationPatch
    {
        private static readonly AccessTools.FieldRef<TransportersArrivalAction_GiveGift, Settlement> SettlementField =
            AccessTools.FieldRefAccess<TransportersArrivalAction_GiveGift, Settlement>("settlement");

        public static void Prefix(TransportersArrivalAction_GiveGift __instance, List<ActiveTransporterInfo> transporters)
        {
            Record(SettlementField(__instance)?.Faction, transporters);
        }

        private static void Record(Faction faction, List<ActiveTransporterInfo> transporters)
        {
            int total = CountEggs(transporters, out bool plague);
            if (total > 0)
            {
                Current.Game.GetComponent<GameComponent_MouseDisasterPhase3>()?.RecordAirdroppedEggs(faction, total, plague);
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
