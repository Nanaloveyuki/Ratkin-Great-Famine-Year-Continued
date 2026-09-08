using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace MouseDisaster
{
    public sealed class MouseDisasterEventGroup : IExposable
    {
        public int id;
        public string incidentDefName;
        public MouseDisasterEventAttitude attitude;
        public bool hostile;
        public bool leaving;
        public List<Pawn> pawns = new List<Pawn>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref id, "id");
            Scribe_Values.Look(ref incidentDefName, "incidentDefName");
            Scribe_Values.Look(ref attitude, "attitude", MouseDisasterEventAttitude.Neutral);
            Scribe_Values.Look(ref hostile, "hostile");
            Scribe_Values.Look(ref leaving, "leaving");
            Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit) pawns ??= new List<Pawn>();
        }
    }

    public sealed class MouseDisasterEventExecution
    {
        [ThreadStatic] public static MouseDisasterEventExecution Current;
        public MouseDisasterEventExecution previous;
        public int groupId;
        public HashSet<Pawn> before;
    }

    public sealed class GameComponent_MouseDisasterEventBehavior : GameComponent
    {
        private List<MouseDisasterEventGroup> groups = new List<MouseDisasterEventGroup>();
        private readonly Dictionary<int, MouseDisasterEventGroup> byId = new Dictionary<int, MouseDisasterEventGroup>();
        private readonly Dictionary<Pawn, MouseDisasterEventGroup> byPawn = new Dictionary<Pawn, MouseDisasterEventGroup>();
        private readonly Dictionary<Pawn, MouseDisasterPawnBehavior> profiles = new Dictionary<Pawn, MouseDisasterPawnBehavior>();
        private readonly HashSet<Pawn> pendingSpawn = new HashSet<Pawn>();
        private int nextId;
        private HashSet<int> fedPawnIds = new HashSet<int>();
        private HashSet<int> refeedingPawnIds = new HashSet<int>();

        internal bool HasCompletedFeeding(Pawn pawn) => pawn != null && fedPawnIds?.Contains(pawn.thingIDNumber) == true;
        internal void CompleteFeeding(Pawn pawn) => (fedPawnIds ??= new HashSet<int>()).Add(pawn.thingIDNumber);
        internal bool HasAppliedRefeeding(Pawn pawn) => refeedingPawnIds?.Contains(pawn.thingIDNumber) == true;
        internal void RecordRefeeding(Pawn pawn) => (refeedingPawnIds ??= new HashSet<int>()).Add(pawn.thingIDNumber);
        internal bool HasFoodSeekingProfile(Pawn pawn) => TryGetGroup(pawn, out _) &&
            profiles.TryGetValue(pawn, out var profile) && (profile & MouseDisasterPawnBehavior.SeekFood) != 0;

        private static Game cachedGame;
        private static GameComponent_MouseDisasterEventBehavior cachedComponent;
        public static GameComponent_MouseDisasterEventBehavior Component
        {
            get
            {
                if (cachedGame != Current.Game) { cachedGame = Current.Game; cachedComponent = null; }
                return cachedComponent ?? (cachedComponent = cachedGame?.GetComponent<GameComponent_MouseDisasterEventBehavior>());
            }
        }
        public GameComponent_MouseDisasterEventBehavior(Game game) { }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref groups, "groups", LookMode.Deep);
            Scribe_Values.Look(ref nextId, "nextId");
            Scribe_Collections.Look(ref fedPawnIds, "fedPawnIds", LookMode.Value);
            Scribe_Collections.Look(ref refeedingPawnIds, "refeedingPawnIds", LookMode.Value);
            if (Scribe.mode != LoadSaveMode.PostLoadInit) return;
            fedPawnIds ??= new HashSet<int>();
            refeedingPawnIds ??= new HashSet<int>();
            groups ??= new List<MouseDisasterEventGroup>();
            byId.Clear(); byPawn.Clear(); profiles.Clear(); pendingSpawn.Clear();
            foreach (var group in groups)
            {
                if (group == null) continue;
                nextId = Math.Max(nextId, group.id);
                byId[group.id] = group;
                foreach (Pawn pawn in group.pawns)
                    if (pawn != null) IndexPawn(group, pawn);
            }
        }

        public int CreateGroup(IncidentDef incident) => CreateGroup(incident.defName);

        public int CreateGroup(string settingsKey)
        {
            var group = new MouseDisasterEventGroup
            {
                id = ++nextId, incidentDefName = settingsKey,
                attitude = MouseDisasterMod.Settings?.GetEventAttitude(settingsKey) ?? MouseDisasterEventAttitude.Neutral
            };
            group.hostile = group.attitude == MouseDisasterEventAttitude.Hostile;
            groups.Add(group); byId[group.id] = group;
            return group.id;
        }

        private void IndexPawn(MouseDisasterEventGroup group, Pawn pawn)
        {
            byPawn[pawn] = group;
            profiles[pawn] = MouseDisasterEventPolicy.Compose(MouseDisasterUtility.IsThiefPawn(pawn), MouseDisasterUtility.IsBeggarPawn(pawn), group.attitude);
        }

        public void Register(int groupId, IEnumerable<Pawn> pawns, bool apply = true)
        {
            if (!byId.TryGetValue(groupId, out var group) || pawns == null) return;
            var added = new List<Pawn>();
            var pending = new Queue<Pawn>(pawns);
            while (pending.Count > 0)
            {
                Pawn pawn = pending.Dequeue();
                if (pawn == null || !MouseDisasterUtility.IsRatkin(pawn) || MouseDisasterUtility.IsPlayerAffiliatedRatkin(pawn)) continue;
                if (!byPawn.ContainsKey(pawn))
                {
                    group.pawns.Add(pawn); IndexPawn(group, pawn);
                    if (pawn.inventory != null)
                        foreach (Thing held in pawn.inventory.innerContainer)
                            if (held is Pawn child) pending.Enqueue(child);
                }
                if (byPawn[pawn] == group) added.Add(pawn);
            }
            if (apply) Apply(group, added);
        }

        public bool TryGetGroup(Pawn pawn, out MouseDisasterEventGroup group)
        {
            group = null;
            return pawn != null && !pawn.Dead && !pawn.Destroyed && !MouseDisasterUtility.IsPlayerAffiliatedRatkin(pawn) && byPawn.TryGetValue(pawn, out group);
        }

        public static MouseDisasterPawnBehavior Profile(Pawn pawn)
        {
            var component = Component;
            if (component == null || !component.TryGetGroup(pawn, out var group) || !component.profiles.TryGetValue(pawn, out var profile))
                return MouseDisasterPawnBehavior.None;
            if (MouseDisasterFeeding.IsSeekingSuppressed(pawn))
                profile &= ~(MouseDisasterPawnBehavior.SeekFood | MouseDisasterPawnBehavior.Beg | MouseDisasterPawnBehavior.Steal);
            return group.leaving ? profile & MouseDisasterPawnBehavior.ReliefOnly : profile;
        }

        public static bool HasBehavior(Pawn pawn, MouseDisasterPawnBehavior behavior) => (Profile(pawn) & behavior) != 0;

        public bool React(IEnumerable<Pawn> pawns, bool forcedAway, out int affected)
        {
            affected = 0;
            var handled = new HashSet<int>();
            foreach (Pawn pawn in pawns)
            {
                if (pawn == null || MouseDisasterUtility.IsPlayerAffiliatedRatkin(pawn) || !byPawn.TryGetValue(pawn, out var group) || !handled.Add(group.id)) continue;
                var reaction = MouseDisasterEventPolicy.React(group.attitude, forcedAway);
                if (reaction == MouseDisasterEventReaction.None) continue;
                int memberCount = group.pawns.Count(p => p != null && p.Spawned && !p.Dead && !MouseDisasterUtility.IsPlayerAffiliatedRatkin(p));
                if (reaction == MouseDisasterEventReaction.GroupHostile)
                {
                    if (group.hostile) { affected += memberCount; continue; }
                    group.hostile = true; group.leaving = false;
                    Apply(group, group.pawns);
                }
                else if (reaction == MouseDisasterEventReaction.GroupFlee)
                {
                    if (group.leaving) { affected += memberCount; continue; }
                    group.leaving = true;
                    Flee(group.pawns);
                }
                affected += memberCount;
            }
            return affected > 0;
        }

        public void NotifyDamage(Pawn pawn)
        {
            if (pawn == null || MouseDisasterUtility.IsPlayerAffiliatedRatkin(pawn) || !byPawn.TryGetValue(pawn, out var group) ||
                group.hostile || group.leaving || MouseDisasterEventPolicy.React(group.attitude, false) == MouseDisasterEventReaction.None) return;
            React(new[] { pawn }, false, out _);
        }

        private void Apply(MouseDisasterEventGroup group, IEnumerable<Pawn> members)
        {
            var active = members.Where(p => p != null && p.Spawned && !p.Dead && !MouseDisasterUtility.IsPlayerAffiliatedRatkin(p)).Distinct().ToList();
            if (active.Count == 0) return;
            Faction faction = MouseDisasterUtility.GetEventFaction(group.hostile, group.attitude == MouseDisasterEventAttitude.Friendly);
            if (faction == null) return;
            var oldLords = new HashSet<Lord>();
            foreach (Pawn pawn in active)
            {
                Lord lord = pawn.GetLord();
                if (lord != null) oldLords.Add(lord);
                if (group.hostile || lord?.LordJob is LordJob_AssaultColony)
                {
                    lord?.RemovePawn(pawn);
                    pawn.jobs?.StopAll();
                    pawn.mindState.duty = null;
                }
                if (pawn.Faction != faction) pawn.SetFaction(faction);
                if (group.hostile || group.attitude == MouseDisasterEventAttitude.Friendly)
                    pawn.mindState?.mentalStateHandler?.Reset();
                if (!group.hostile && !group.leaving) MapComponent_MouseDisasterFoodTargets.Prime(pawn);
            }
            foreach (Lord lord in oldLords)
                if (lord.ownedPawns.Count > 0 && lord.ownedPawns.All(p => p.Faction == faction)) lord.faction = faction;
            if (group.leaving) Flee(active);
            else if (group.hostile)
                foreach (var mapPawns in active.Where(p => !p.Downed && p.DevelopmentalStage != DevelopmentalStage.Baby).GroupBy(p => p.Map))
                    LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction, canKidnap: false, canTimeoutOrFlee: false, canSteal: true), mapPawns.Key, mapPawns.ToList());
        }

        private static void Flee(IEnumerable<Pawn> pawns)
        {
            foreach (var members in pawns.Where(p => p != null && p.Spawned && !p.Dead && !p.Downed &&
                !MouseDisasterUtility.IsPlayerAffiliatedRatkin(p)).GroupBy(p => p.Map))
            {
                var list = members.ToList();
                foreach (Pawn pawn in list)
                {
                    pawn.GetLord()?.RemovePawn(pawn);
                    pawn.mindState?.mentalStateHandler?.Reset();
                    pawn.jobs?.StopAll();
                }
                Pawn anchor = list.FirstOrDefault(p => p.DevelopmentalStage != DevelopmentalStage.Baby);
                if (anchor != null && RCellFinder.TryFindBestExitSpot(anchor, out IntVec3 exit))
                    MouseDisasterUtility.MakeTravelAndExitLord(members.Key, list, exit, includeBabiesInExit: true);
            }
        }

        public void NotifySpawned(Pawn pawn)
        {
            if (pawn != null && byPawn.ContainsKey(pawn)) pendingSpawn.Add(pawn);
        }

        public override void GameComponentTick()
        {
            if (pendingSpawn.Count > 0)
            {
                var pending = pendingSpawn.ToList(); pendingSpawn.Clear();
                foreach (Pawn pawn in pending)
                    if (TryGetGroup(pawn, out var group)) Apply(group, new[] { pawn });
            }
            if (Find.TickManager.TicksGame % GenDate.TicksPerHour != 0) return;
            for (int i = groups.Count - 1; i >= 0; i--)
            {
                var group = groups[i];
                if (group == null) { groups.RemoveAt(i); continue; }
                for (int j = group.pawns.Count - 1; j >= 0; j--)
                {
                    Pawn pawn = group.pawns[j];
                    if (pawn != null && !pawn.Destroyed && !pawn.Dead && !MouseDisasterUtility.IsPlayerAffiliatedRatkin(pawn)) continue;
                    if (pawn != null) { byPawn.Remove(pawn); profiles.Remove(pawn); }
                    group.pawns.RemoveAt(j);
                }
                if (group.pawns.Count == 0 && Current.Game.GetComponent<GameComponent_MouseDisasterPawnGeneration>()?.HasPendingBehaviorGroup(group.id) != true)
                {
                    byId.Remove(group.id); groups.RemoveAt(i);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PostApplyDamage))]
    internal static class MouseDisasterEventDamagePatch
    {
        public static void Postfix(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            if (totalDamageDealt > 0f && dinfo.Instigator?.Faction == Faction.OfPlayer && dinfo.Def.ExternalViolenceFor(__instance))
                GameComponent_MouseDisasterEventBehavior.Component?.NotifyDamage(__instance);
        }
    }
}
