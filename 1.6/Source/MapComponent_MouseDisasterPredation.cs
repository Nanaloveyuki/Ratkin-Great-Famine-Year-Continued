using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace MouseDisaster
{
    public sealed class MouseDisasterPredator : IExposable
    {
        public Pawn pawn;
        public bool outside;
        public bool firstHunt = true;
        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref outside, "outside");
            Scribe_Values.Look(ref firstHunt, "firstHunt", true);
        }
    }

    public sealed class MapComponent_MouseDisasterPredation : MapComponent
    {
        private List<Pawn> prey = new List<Pawn>();
        private List<int> selectedGroups = new List<int>();
        private List<MouseDisasterPredator> predators = new List<MouseDisasterPredator>();
        private readonly Dictionary<Pawn, MouseDisasterPredator> byPawn = new Dictionary<Pawn, MouseDisasterPredator>();
        private readonly Dictionary<Pawn, int> nextSearch = new Dictionary<Pawn, int>();
        private readonly HashSet<Pawn> vanillaFallback = new HashSet<Pawn>();
        private int pendingTick = -1;
        private int nextTick;

        public MapComponent_MouseDisasterPredation(Map map) : base(map) { }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref prey, "predationPrey", LookMode.Reference);
            Scribe_Collections.Look(ref selectedGroups, "predationGroups", LookMode.Value);
            Scribe_Collections.Look(ref predators, "predationPredators", LookMode.Deep);
            Scribe_Values.Look(ref pendingTick, "predationPendingTick", -1);
            if (Scribe.mode != LoadSaveMode.PostLoadInit) return;
            prey ??= new List<Pawn>();
            selectedGroups ??= new List<int>();
            predators ??= new List<MouseDisasterPredator>();
            byPawn.Clear();
            foreach (var predator in predators)
                if (predator?.pawn != null) byPawn[predator.pawn] = predator;
        }

        public void Register(MouseDisasterEventGroup group, IEnumerable<Pawn> pawns)
        {
            if (!MouseDisasterRuntime.AllowsNewContent) return;
            if (!group.predationRolledMaps.Contains(map.uniqueID))
            {
                group.predationRolledMaps.Add(map.uniqueID);
                if (Rand.Chance((MouseDisasterMod.Settings?.refugeePredationChancePercent ?? 10f) / 100f))
                {
                    selectedGroups.Add(group.id);
                    if (pendingTick < 0) pendingTick = Find.TickManager.TicksGame + 120;
                }
            }
            if (!selectedGroups.Contains(group.id)) return;
            foreach (Pawn pawn in pawns)
                if (!prey.Contains(pawn)) prey.Add(pawn);
        }

        private static bool WildPredator(Pawn pawn) => pawn != null && pawn.Spawned && !pawn.Dead &&
            !pawn.Downed && pawn.Faction == null && pawn.RaceProps.Animal && pawn.RaceProps.predator &&
            !pawn.InMentalState && pawn.meleeVerbs?.TryGetMeleeVerb(null) != null;

        public override void MapComponentTick()
        {
            int now = Find.TickManager.TicksGame;
            if (now < nextTick || (predators.Count == 0 && pendingTick < 0)) return;
            nextTick = now + 250;
            if (pendingTick >= 0 && now >= pendingTick)
            {
                pendingTick = -1;
                // The delayed arrival may outlive the visitors or their recruitment.
                if (MouseDisasterRuntime.AllowsNewContent && prey.Any(p => p != null && p.Spawned &&
                    !p.Dead && p.Map == map && !MouseDisasterUtility.IsPlayerAffiliatedRatkin(p)))
                {
                    var wild = map.mapPawns.AllPawnsSpawned.Where(WildPredator).ToList();
                    if (wild.Count > 0)
                    {
                        AddPredator(wild.RandomElement(), false);
                        Find.LetterStack.ReceiveLetter("MouseDisaster_Predation_Label".Translate(),
                            "MouseDisaster_Predation_Text".Translate(), LetterDefOf.ThreatSmall, new LookTargets(prey));
                    }
                    else SpawnOutside();
                }
            }
            for (int i = predators.Count - 1; i >= 0; i--)
            {
                var record = predators[i];
                Pawn pawn = record.pawn;
                if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.Map != map || pawn.Faction != null)
                {
                    if (pawn != null) { byPawn.Remove(pawn); nextSearch.Remove(pawn); vanillaFallback.Remove(pawn); }
                    predators.RemoveAt(i);
                    continue;
                }
                if (!WildPredator(pawn)) continue;
                if (pawn.CurJobDef == JobDefOf.PredatorHunt || pawn.CurJobDef == JobDefOf.Ingest ||
                    pawn.CurJob?.exitMapOnArrival == true) continue;
                if (!record.firstHunt && !Hungry(pawn)) continue;
                if (TryFoodJob(pawn, out Job job) && job != null)
                    pawn.jobs.StartJob(job, JobCondition.InterruptForced);
            }
            prey.RemoveAll(p => p == null || (!p.Dead && p.MapHeld != map) ||
                (p.Dead && (p.Corpse == null || p.Corpse.Destroyed)));
        }

        private void AddPredator(Pawn pawn, bool outside)
        {
            if (byPawn.TryGetValue(pawn, out var existing)) { existing.firstHunt = true; return; }
            var record = new MouseDisasterPredator { pawn = pawn, outside = outside };
            predators.Add(record);
            byPawn[pawn] = record;
        }

        private void SpawnOutside()
        {
            var kinds = map.Biome.AllWildAnimals.Where(k => k.race.race.predator).ToList();
            if (kinds.Count == 0) kinds.Add(DefDatabase<PawnKindDef>.GetNamed("Wolf_Timber"));
            if (!RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 cell, map, 0f, true))
            {
                Log.Warning("MouseDisaster: no entry cell for outside predators on map " + map.uniqueID);
                return;
            }
            Pawn pawn = PawnGenerator.GeneratePawn(kinds.RandomElement());
            GenSpawn.Spawn(pawn, cell, map);
            AddPredator(pawn, true);
            Find.LetterStack.ReceiveLetter("MouseDisaster_OutsidePredators_Label".Translate(),
                "MouseDisaster_OutsidePredators_Text".Translate(), LetterDefOf.ThreatSmall, pawn);
        }

        private static bool Hungry(Pawn pawn) => pawn.needs?.food != null &&
            pawn.needs.food.CurLevelPercentage < pawn.RaceProps.FoodLevelPercentageWantEat;

        internal bool IsOutside(Pawn pawn) => pawn != null && byPawn.TryGetValue(pawn, out var record) && record.outside;

        internal static bool ShouldFlee(Pawn predator, Pawn victim) =>
            MouseDisasterMod.Settings?.refugeePredationFightBack == false &&
            predator?.CurJobDef == JobDefOf.PredatorHunt && predator.CurJob.targetA.Pawn == victim &&
            GameComponent_MouseDisasterEventBehavior.Component?.TryGetGroup(victim, out _) == true;

        internal bool ForbiddenCorpse(Thing thing)
        {
            Corpse corpse = thing as Corpse ?? (thing as Pawn)?.Corpse;
            return corpse != null && !corpse.Destroyed && corpse.MapHeld == map && MouseDisasterUtility.IsRatkin(corpse.InnerPawn) &&
                map.areaManager.Home[corpse.PositionHeld];
        }

        // Return true only while this component owns the predator's food choice.
        internal bool TryFoodJob(Pawn pawn, out Job job)
        {
            job = null;
            if (!byPawn.TryGetValue(pawn, out var record) || !WildPredator(pawn)) return false;
            bool follow = record.outside && MouseDisasterMod.Settings?.outsidePredatorsFollowDifficulty == true;
            if (!record.firstHunt && !Hungry(pawn)) return !follow;
            int now = Find.TickManager.TicksGame;
            if (nextSearch.TryGetValue(pawn, out int until) && now < until) return !(follow && vanillaFallback.Contains(pawn));
            nextSearch[pawn] = now + 250;
            vanillaFallback.Remove(pawn);
            // Bounded scans only on hungry, registered predators; never patch global food searches.
            IEnumerable<Pawn> targets = record.outside ? map.mapPawns.AllPawnsSpawned : prey;
            Pawn victim = targets.Where(p => p != null && p.Spawned && p.Map == map && !p.Dead &&
                MouseDisasterUtility.IsRatkin(p) && !MouseDisasterUtility.IsPlayerAffiliatedRatkin(p) &&
                p.RaceProps.canBePredatorPrey && pawn.RaceProps.CanEverEat(p.RaceProps.corpseDef))
                .OrderBy(p => p.ageTracker.AgeBiologicalYearsFloat <= 8f ? 0 : 1)
                .ThenBy(p => p.Position.DistanceToSquared(pawn.Position))
                .FirstOrDefault(p => pawn.CanReach(p, PathEndMode.Touch, Danger.Deadly));
            if (victim != null)
            {
                record.firstHunt = false;
                job = JobMaker.MakeJob(JobDefOf.PredatorHunt, victim);
                job.killIncappedTarget = true;
                return true;
            }
            if (record.outside)
            {
                Corpse corpse = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).OfType<Corpse>()
                    .Where(c => MouseDisasterUtility.IsRatkin(c.InnerPawn) && !ForbiddenCorpse(c) &&
                        c.IngestibleNow && pawn.RaceProps.CanEverEat(c) && pawn.CanReach(c, PathEndMode.Touch, Danger.Deadly))
                    .OrderBy(c => c.Position.DistanceToSquared(pawn.Position)).FirstOrDefault();
                if (corpse != null)
                {
                    record.firstHunt = false;
                    job = JobMaker.MakeJob(JobDefOf.Ingest, corpse);
                    job.count = 1;
                    return true;
                }
                record.firstHunt = false;
                if (follow) { vanillaFallback.Add(pawn); return false; }
                if (Hungry(pawn) && RCellFinder.TryFindRandomExitSpot(pawn, out IntVec3 exit))
                {
                    job = JobMaker.MakeJob(JobDefOf.Goto, exit);
                    job.exitMapOnArrival = true;
                }
                return true;
            }
            byPawn.Remove(pawn);
            predators.Remove(record);
            nextSearch.Remove(pawn);
            return false;
        }
    }

    [HarmonyPatch(typeof(JobGiver_GetFood), "TryGiveJob")]
    public static class MouseDisasterPredatorFoodPatch
    {
        public static bool Prefix(Pawn pawn, ref Job __result)
        {
            if (!pawn.RaceProps.Animal || !pawn.RaceProps.predator || pawn.Faction != null) return true;
            var component = pawn.Map?.GetComponent<MapComponent_MouseDisasterPredation>();
            if (component == null || !component.TryFoodJob(pawn, out Job job)) return true;
            __result = job;
            return false;
        }

        public static void Postfix(Pawn pawn, ref Job __result)
        {
            if (__result == null || !pawn.RaceProps.Animal || !pawn.RaceProps.predator || pawn.Faction != null) return;
            var component = pawn.Map?.GetComponent<MapComponent_MouseDisasterPredation>();
            if (__result != null && component?.IsOutside(pawn) == true && component.ForbiddenCorpse(__result.targetA.Thing))
                __result = null;
        }
    }

    [HarmonyPatch(typeof(JobDriver_PredatorHunt), "MakeNewToils")]
    public static class MouseDisasterPredatorCorpsePatch
    {
        public static IEnumerable<Toil> Postfix(IEnumerable<Toil> __result, JobDriver_PredatorHunt __instance)
        {
            var component = __instance.pawn.Map?.GetComponent<MapComponent_MouseDisasterPredation>();
            foreach (Toil toil in __result)
            {
                if (component?.IsOutside(__instance.pawn) == true)
                    toil.FailOn(() => component.ForbiddenCorpse(__instance.job.targetA.Thing));
                yield return toil;
            }
        }
    }

    [HarmonyPatch(typeof(JobDriver_Ingest), "MakeNewToils")]
    public static class MouseDisasterPredatorIngestPatch
    {
        public static IEnumerable<Toil> Postfix(IEnumerable<Toil> __result, JobDriver_Ingest __instance)
        {
            var component = __instance.pawn.RaceProps.predator ?
                __instance.pawn.Map?.GetComponent<MapComponent_MouseDisasterPredation>() : null;
            foreach (Toil toil in __result)
            {
                if (component?.IsOutside(__instance.pawn) == true)
                    toil.FailOn(() => component.ForbiddenCorpse(__instance.job.targetA.Thing));
                yield return toil;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.PostApplyDamage))]
    public static class MouseDisasterPredationResponsePatch
    {
        public static void Postfix(Pawn ___pawn, DamageInfo dinfo)
        {
            Pawn predator = dinfo.Instigator as Pawn;
            if (___pawn == null || !___pawn.Spawned || ___pawn.Dead || ___pawn.Downed ||
                predator?.CurJobDef != JobDefOf.PredatorHunt || predator.CurJob.targetA.Pawn != ___pawn ||
                GameComponent_MouseDisasterEventBehavior.Component?.TryGetGroup(___pawn, out _) != true) return;
            Job response;
            if (MouseDisasterMod.Settings?.refugeePredationFightBack != false && !___pawn.WorkTagIsDisabled(WorkTags.Violent))
            {
                response = JobMaker.MakeJob(JobDefOf.AttackMelee, predator);
                response.expiryInterval = 250;
                response.maxNumMeleeAttacks = 1;
            }
            else response = FleeUtility.FleeJob(___pawn, predator, 16);
            if (response != null) ___pawn.jobs.StartJob(response, JobCondition.InterruptForced);
        }
    }

    [HarmonyPatch(typeof(JobGiver_ReactToCloseMeleeThreat), "TryGiveJob")]
    public static class MouseDisasterPredationNoRetaliationPatch
    {
        public static bool Prefix(Pawn pawn, ref Job __result)
        {
            Pawn predator = pawn.mindState.meleeThreat;
            if (MouseDisasterMod.Settings?.refugeePredationFightBack != false ||
                predator?.CurJobDef != JobDefOf.PredatorHunt || predator.CurJob.targetA.Pawn != pawn ||
                GameComponent_MouseDisasterEventBehavior.Component?.TryGetGroup(pawn, out _) != true) return true;
            __result = FleeUtility.FleeJob(pawn, predator, 16);
            return false;
        }
    }
}
