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
    public static class MouseDisasterUtility
    {
        private const string RatkinRaceDefName = "Ratkin";
        private const string RatkinXenotypeDefName = "Ratkin";
        private static readonly string[] PreferredRatkinXenotypeDefNames =
        {
            "Ratkin_HouseMouse",
            "Ratkin_Mole",
            "Ratkin_LabRat",
            "Ratkin_Hamster",
            "Ratkin_Squirrel",
            "Ratkin_Vole",
            "Ratkin_VolePrototype",
            "Ratkin_Waster",
            "OAGene_RatkinBase",
            "OAGene_PrimordialRatkin",
            "OAGene_WhiteRatkin",
            "OAGene_TravelRatkin",
            "OAGene_RockRatkin",
            "OAGene_SnowRatkin",
            "OAGene_LowlandRatkin",
            "OAGene_BiochemicalRatkinI",
            "OAGene_BiochemicalRatkinII",
            "OAGene_BiochemicalRatkinIII",
            "OAGene_BiochemicalRatkinIV",
            "Ratkin"
        };
        private const string ClothStuffDefName = "Cloth";
        private const string HumanleatherStuffDefName = "Humanleather";
        private const string RatTailPartDefName = "RK_RatTail";
        private const float RatEggMinAgeYears = 1f;
        private const float RatEggMaxAgeYears = 2.9f;
        private const float RatkinYoungChildMinAgeYears = 3f;
        private const float RatkinYoungChildMaxAgeYears = 6.9f;
        private const float RatkinAdultMinAgeYears = 16f;
        private const float RatkinAdultMaxAgeYears = 50f;
        private const string EarPartDefName = "Ear";
        private const string FingerPartDefName = "Finger";
        private const string ToePartDefName = "Toe";
        private const float SleepPoopSpawnChance = 0.0004f;
        private const int DefaultChaosPregnancyCheckInterval = 3000;
        private const float DefaultChaosPregnancyChancePerCheck = 0.02f;
        private const float MinimumMouseDisasterPregnancyAgeYears = 1f;
        private const float MouseDisasterFoodMinPercent = 0.30f;
        private const float MouseDisasterFoodMaxPercent = 0.70f;
        private const float MouseDisasterFoodMinAbsolute = 30f;
        private const float MouseDisasterFoodMaxAbsolute = 70f;
        private const int PrisonerScavengeBurstMinCount = 3;
        private const int PrisonerScavengeBurstMaxCount = 5;
        private const int PrisonerScavengeBurstCooldownTicks = GenDate.TicksPerHour;
        private const float NegativeTraitTargetRatio = 0.80f;
        private const float RandomShootingAdultKeepChance = 0.08f;
        private const float RandomShootingChildKeepChance = 0.04f;
        private const float RandomShootingBabyKeepChance = 0.02f;
        private const float DisasterApparelMinDurability = 0.10f;
        private const float DisasterApparelMaxDurability = 0.60f;
        private const int PrisonerScavengeLogIntervalTicks = 600;
        private const int PrisonerScavengeCareDelayTicks = GenDate.TicksPerHour * 3;
        private const int MissingFactionRelationRepairIntervalTicks = 1800;
        private const int TailBiteAttemptCooldownTicks = 2400;
        private const int TailBiteVictimCooldownTicks = 6000;
        private const int TailBiteStartTextCooldownTicks = 900;
        private const int TailBiteResultTextCooldownTicks = 240;
        private const int TailBiteNotifyCooldownTicks = 600;
        private const string VanillaNewbornSpawnCategory = "Newborn";
        private const int SiegeBeggarLeaveFoodUnits = 5;
        private const int HiddenFactionExplicitHostilityDurationTicks = GenDate.TicksPerDay;
        private const string HiddenFactionChineseName = "\u9f20\u707e\u5e78\u5b58\u8005";
        private const string BabyExpansionPackageId = "cj.rimtalk.toddlers";
        private const string HospitalityPackageId = "Orion.Hospitality";
        private const string VanillaNutrientPasteExpandedPackageId = "VanillaExpanded.VNutrientE";
        private const string BabyExpansionVisitorMoodThoughtDefName = "RimTalk_VisitorBabyNewEnvironment";
        public const int ChildExchangeModeColonist = 0;
        public const int ChildExchangeModeSlave = 1;
        public const int ChildExchangeModePrisoner = 2;
        private static readonly Dictionary<int, int> BegAttempts = new Dictionary<int, int>();
        private static readonly Dictionary<int, HashSet<int>> BeggedColonists = new Dictionary<int, HashSet<int>>();
        private static readonly HashSet<int> BegSuccess = new HashSet<int>();
        private static readonly HashSet<int> SiegeBeggarPawnIds = new HashSet<int>();
        private static readonly HashSet<int> SiegeBeggarStoleFoodSuccess = new HashSet<int>();
        private static readonly HashSet<int> StrongSiegePawnIds = new HashSet<int>();
        private static readonly Dictionary<int, int> AirDropStayUntilTickByPawnId = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> WallGnawCounts = new Dictionary<int, int>();
        private static readonly HashSet<int> ForcePrisonerOnPurchasePawnIds = new HashSet<int>();
        private static readonly HashSet<int> TradableChattelPawnIds = new HashSet<int>();
        private static readonly HashSet<int> ChildExchangeMoodPawnIds = new HashSet<int>();
        private static readonly Dictionary<int, ChildExchangeState> ActiveChildExchangeByTraderId = new Dictionary<int, ChildExchangeState>();
        private static readonly Dictionary<int, AbandonedDeliveryState> ActiveAbandonedDeliveryByAdultId = new Dictionary<int, AbandonedDeliveryState>();
        private static readonly Dictionary<int, MapPawnClassificationCache> MapPawnCaches = new Dictionary<int, MapPawnClassificationCache>();
        private static readonly Dictionary<int, int> PrisonerScavengeDelayStateByPawnId = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> PrisonerScavengeBurstRemainingByPawnId = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> PrisonerScavengeBurstCooldownByPawnId = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> PrisonerScavengeLastLogTickByPawnId = new Dictionary<int, int>();
        private static readonly Dictionary<int, string> PrisonerScavengeLastLogMessageByPawnId = new Dictionary<int, string>();
        private static readonly Dictionary<int, int> TailBiteLastAttemptTickByPawnId = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> TailBiteLastVictimTickByPawnId = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> TailBiteLastNotifyTickByPawnId = new Dictionary<int, int>();
        private static readonly Dictionary<string, int> FloatingTextLastTickByKey = new Dictionary<string, int>();
        private static readonly Dictionary<ThingDef, List<ThingDef>> ApparelPreferredStuffCache = new Dictionary<ThingDef, List<ThingDef>>();
        private static readonly Dictionary<ThingDef, bool> IsRatkinRaceDefCache = new Dictionary<ThingDef, bool>();
        private static readonly Dictionary<XenotypeDef, bool> IsRatkinXenotypeDefCache = new Dictionary<XenotypeDef, bool>();
        private static readonly string[] RatkinRaceKeywords =
        {
            "ratkin",
            "鼠族"
        };
        private static readonly string[] RatkinXenotypeKeywords =
        {
            "ratkin",
            "鼠族"
        };
        private static readonly string[] RandomShootingTraitKeywords =
        {
            "trigger",
            "shoot",
            "gun",
            "\u67aa",
            "\u5f00\u706b",
            "\u5f00\u67aa",
            "乱射"
        };
        private static readonly string[] RandomMissingPartPool =
        {
            "Ear",
            "Ear",
            "Ear",
            "RK_RatTail",
            "RK_RatTail",
            "Finger",
            "Toe"
        };
        private static readonly string[] BabyRestrictedApparelDefNames =
        {
            "Apparel_BabyOnesie",
            "Apparel_WarmerHat",
            "Apparel_SunHat"
        };
        private static readonly string[] ChildRestrictedApparelDefNames =
        {
            "Apparel_WarmerHat",
            "Apparel_SunHat",
            "Apparel_KidTribal"
        };
        private static readonly string[] SlaveTraderKindDefNames =
        {
            "MouseDisaster_RatkinTrader",
            "Caravan_Neolithic_Slaver",
            "Caravan_Outlander_Slaver",
            "Caravan_Pirate_Slaver"
        };
        private static readonly string[] PrisonerScavengeFloorDustKeywords =
        {
            "dirt",
            "dust",
            "ash",
            "sand",
            "\u7070",
            "\u5c18",
            "\u571f"
        };
        private static readonly string[] PrisonerScavengeVomitKeywords =
        {
            "vomit",
            "puke",
            "bile",
            "呕吐"
        };
        private static readonly string[] PrisonerScavengeAmnioticKeywords =
        {
            "amniotic",
            "amni",
            "羊水",
            "胎水"
        };
        private static readonly string[] PrisonerScavengeBloodKeywords =
        {
            "blood",
            "hemorr",
            "血"
        };
        private static readonly string[] PrisonerScavengeFloorDustTextKeys =
        {
            "MouseDisaster_PrisonerScavenge_Text_FloorDust_1",
            "MouseDisaster_PrisonerScavenge_Text_FloorDust_2",
            "MouseDisaster_PrisonerScavenge_Text_FloorDust_3"
        };
        private static readonly string[] PrisonerScavengeDirtyTextKeys =
        {
            "MouseDisaster_PrisonerScavenge_Text_Dirty_1",
            "MouseDisaster_PrisonerScavenge_Text_Dirty_2",
            "MouseDisaster_PrisonerScavenge_Text_Dirty_3"
        };
        private static readonly string[] PrisonerScavengeVomitTextKeys =
        {
            "MouseDisaster_PrisonerScavenge_Text_Vomit_1",
            "MouseDisaster_PrisonerScavenge_Text_Vomit_2",
            "MouseDisaster_PrisonerScavenge_Text_Vomit_3"
        };
        private static readonly string[] PrisonerScavengeAmnioticTextKeys =
        {
            "MouseDisaster_PrisonerScavenge_Text_Amniotic_1",
            "MouseDisaster_PrisonerScavenge_Text_Amniotic_2",
            "MouseDisaster_PrisonerScavenge_Text_Amniotic_3"
        };
        private static readonly string[] PrisonerScavengeBloodTextKeys =
        {
            "MouseDisaster_PrisonerScavenge_Text_Blood_1",
            "MouseDisaster_PrisonerScavenge_Text_Blood_2",
            "MouseDisaster_PrisonerScavenge_Text_Blood_3"
        };
        private static readonly string[] PrisonerScavengePoisonedTextKeys =
        {
            "MouseDisaster_PrisonerScavenge_Text_Poisoned_1",
            "MouseDisaster_PrisonerScavenge_Text_Poisoned_2"
        };
        private static readonly string[] ScavengeInterruptBlockedJobKeywords =
        {
            "Carried",
            "Carry"
        };
        private static readonly string[] ExtremeCompatModKeywords =
        {
            "RimTalk",
            "BabyAngel",
            "TrainingFacility"
        };
        private const string BabyOnesieApparelDefName = "Apparel_BabyOnesie";
        private const string KidTribalApparelDefName = "Apparel_KidTribal";
        private const string SunHatApparelDefName = "Apparel_SunHat";
        private const string WarmerHatApparelDefName = "Apparel_WarmerHat";
        private static ThingDef ratEggTailThingDef;
        private static ThingDef ratkinRaceDef;
        private static XenotypeDef ratkinXenotypeDef;
        private static List<XenotypeDef> cachedAllowedRatkinXenotypes;
        private static bool cachedAllowedRatkinXenotypesResolved;
        private static ThingDef clothStuffDef;
        private static ThingDef humanleatherStuffDef;
        private static PawnKindDef playerAffiliatedRatkinKindDef;
        private static bool playerAffiliatedRatkinKindResolved;
        private static HediffDef toddlersLearningToWalkDef;
        private static HediffDef toddlersLearningManipulationDef;
        private static HediffDef toddlersLonelyDef;
        private static HediffDef rimTalkBabyBabblingDef;
        private static HediffDef rimTalkToddlerLanguageLearningDef;
        private static bool toddlerCompatDefsResolved;
        private static bool syncingToddlerCompat;
        private static List<TraitDef> ratEggTraitPool;
        private static List<TraitDef> negativeTraitPool;
        private static List<GeneDef> cachedMouseDisasterGenePool;
        private static bool cachedMouseDisasterGenePoolResolved;
        private static bool ratEggTraitPoolResolved;
        private static bool negativeTraitPoolResolved;
        private static bool ratkinRaceResolved;
        private static bool ratkinXenotypeResolved;
        private static bool apparelStuffResolved;
        private static bool slaveTraderKindResolved;
        private static Faction mouseDisasterHiddenFaction;
        private static TraderKindDef slaveTraderKind;
        private static bool ratEggTailResolved;
        private static AnimationDef toddlerCrawlAnimationDef;
        private static bool toddlerCrawlAnimationResolved;
        private static List<ThingDef> adultApparelDefsCache;
        private static bool adultApparelDefsResolved;
        private static List<BackstoryDef> vanillaNewbornBackstoryPool;
        private static bool vanillaNewbornBackstoryPoolResolved;
        private static int lastFactionRelationRepairTick = int.MinValue;
        private static int lastFactionRelationRepairFactionCount = -1;
        private static int hiddenFactionExplicitHostilityUntilTick = -1;
        private static bool cachedHostilePawnExists;
        private static int cachedHostilePawnCheckTick = -1;
        private static int lastHiddenFactionRelationSyncFactionCount = -1;
        private static bool suppressReliefAreaPostfix;
        private static readonly Dictionary<int, Pawn> ReusablePawnLookup = new Dictionary<int, Pawn>();
        private static readonly List<int> ReusableIntList = new List<int>();
        private static readonly List<Pawn> ReusablePawnList = new List<Pawn>();
        private static bool? extremeCompatJobModeCached;
        private static bool? babyExpansionEnabledCached;
        private static bool? leadYourPetEnabledCached;
        private static bool? hospitalityEnabledCached;
        private static bool? vanillaNutrientPasteExpandedEnabledCached;
        private static ThoughtDef babyExpansionVisitorMoodThoughtDef;
        private static bool babyExpansionVisitorMoodThoughtResolved;
        private static Type leadYourPetComponentType;
        private static MethodInfo leadYourPetTryStartRatkinMotherLeashMethod;
        private static MethodInfo leadYourPetStartLeashMethod;
        private static MethodInfo leadYourPetTryAssignTravelMouseEggsMethod;
        private static MethodInfo leadYourPetAnchorLeashedPetsToCellMethod;
        private static MethodInfo leadYourPetEndLeashForPetMethod;
        private static MethodInfo gameGetComponentMethod;
        private sealed class ChildExchangeState
        {
            public int mapId;
            public int expireTick;
            public List<int> escortPawnIds;
            public List<int> childPawnIds;
        }

        private sealed class AbandonedDeliveryState
        {
            public int mapId;
            public IntVec3 foodCell;
            public List<int> childPawnIds;
            public bool adultHasLeft;
            public int adultArrivedAtDropoffTick = -1;
        }

        private sealed class MapPawnClassificationCache
        {
            public bool dirty = true;
            public readonly List<Pawn> ratkinPawns = new List<Pawn>();
            public readonly List<Pawn> mouseDisasterPawns = new List<Pawn>();
            public readonly List<Pawn> incidentMoodChildren = new List<Pawn>();
            public readonly List<Pawn> doorStuckCandidates = new List<Pawn>();
            public readonly Dictionary<int, Pawn> spawnedPawnById = new Dictionary<int, Pawn>();
        }

        public sealed class PrisonerScavengeProfile
        {
            public readonly ThoughtDef thoughtDef;
            public readonly string[] textKeys;
            public readonly float nutritionGain;

            public PrisonerScavengeProfile(ThoughtDef thoughtDef, string[] textKeys, float nutritionGain)
            {
                this.thoughtDef = thoughtDef;
                this.textKeys = textKeys;
                this.nutritionGain = nutritionGain;
            }
        }

        public static bool IsRatkin(Pawn pawn)
        {
            if (pawn?.def == null)
            {
                return false;
            }

            ThingDef expectedRace = ResolveRatkinRaceDef(pawn.kindDef);
            if (expectedRace != null)
            {
                return pawn.def == expectedRace;
            }

            return IsRatkinRaceDef(pawn.def);
        }

        public static bool IsMouseDisasterPawn(Pawn pawn)
        {
            if (!IsRatkin(pawn) || pawn.story == null)
            {
                return false;
            }

            bool hasChildhood = pawn.story.Childhood == MouseDisasterDefOf.MouseDisaster_Newborn;
            bool hasAdulthood = pawn.story.Adulthood == null || pawn.story.Adulthood == MouseDisasterDefOf.MouseDisaster_Refugee;
            return hasChildhood && hasAdulthood;
        }

        public static bool IsMouseEggBaby(Pawn pawn)
        {
            return IsRatkin(pawn) && pawn?.ageTracker != null && pawn.ageTracker.AgeBiologicalYearsFloat < 3f;
        }

        public static bool IsMouseEggOrChild(Pawn pawn)
        {
            if (!IsRatkin(pawn) || pawn == null)
            {
                return false;
            }

            if (pawn.DevelopmentalStage == DevelopmentalStage.Baby || pawn.DevelopmentalStage == DevelopmentalStage.Child)
            {
                return true;
            }

            return pawn.ageTracker != null && pawn.ageTracker.AgeBiologicalYearsFloat < 7f;
        }

        public static bool IsPlayerAffiliatedRatkin(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            return pawn.Faction == Faction.OfPlayer || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony;
        }

        public static bool IsEligibleForMouseDisasterGenes(Pawn pawn)
        {
            return ModsConfig.BiotechActive &&
                   pawn?.genes != null &&
                   IsMouseDisasterPawn(pawn);
        }

        public static void MarkMapPawnCacheDirty(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            MarkMapPawnCacheDirty(pawn.MapHeld ?? pawn.Map);
        }

        public static void MarkMapPawnCacheDirty(Map map)
        {
            if (map == null)
            {
                return;
            }

            if (!MapPawnCaches.TryGetValue(map.uniqueID, out MapPawnClassificationCache cache))
            {
                cache = new MapPawnClassificationCache();
                MapPawnCaches[map.uniqueID] = cache;
            }

            cache.dirty = true;
        }

        private static List<Pawn> GetCachedRatkinPawns(Map map)
        {
            return GetMapPawnCache(map).ratkinPawns;
        }

        private static List<Pawn> GetCachedMouseDisasterPawns(Map map)
        {
            return GetMapPawnCache(map).mouseDisasterPawns;
        }

        private static List<Pawn> GetCachedIncidentMoodChildren(Map map)
        {
            return GetMapPawnCache(map).incidentMoodChildren;
        }

        private static List<Pawn> GetCachedDoorStuckCandidates(Map map)
        {
            return GetMapPawnCache(map).doorStuckCandidates;
        }

        private static Dictionary<int, Pawn> GetCachedSpawnedPawnLookup(Map map)
        {
            return GetMapPawnCache(map).spawnedPawnById;
        }

        private static GameComponent_MouseDisasterGeneRestoreState GeneRestoreState => Current.Game?.GetComponent<GameComponent_MouseDisasterGeneRestoreState>();

        private static MapPawnClassificationCache GetMapPawnCache(Map map)
        {
            if (map == null)
            {
                return new MapPawnClassificationCache();
            }

            if (!MapPawnCaches.TryGetValue(map.uniqueID, out MapPawnClassificationCache cache))
            {
                cache = new MapPawnClassificationCache();
                MapPawnCaches[map.uniqueID] = cache;
            }

            if (cache.dirty)
            {
                RebuildMapPawnCache(map, cache);
            }

            return cache;
        }

        private static void RebuildMapPawnCache(Map map, MapPawnClassificationCache cache)
        {
            cache.dirty = false;
            cache.ratkinPawns.Clear();
            cache.mouseDisasterPawns.Clear();
            cache.incidentMoodChildren.Clear();
            cache.doorStuckCandidates.Clear();
            cache.spawnedPawnById.Clear();

            IReadOnlyList<Pawn> pawns = map?.mapPawns?.AllPawnsSpawned;
            if (pawns == null)
            {
                return;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.Dead)
                {
                    continue;
                }

                cache.spawnedPawnById[pawn.thingIDNumber] = pawn;

                bool isRatkin = IsRatkin(pawn);
                if (!isRatkin)
                {
                    continue;
                }

                cache.ratkinPawns.Add(pawn);

                if (IsMouseDisasterPawn(pawn))
                {
                    cache.mouseDisasterPawns.Add(pawn);
                }

                if (HasChildExchangeMoodMarker(pawn) || IsMouseDisasterIncidentChild(pawn))
                {
                    cache.incidentMoodChildren.Add(pawn);
                }

                if (IsDoorStuckCandidate(pawn))
                {
                    cache.doorStuckCandidates.Add(pawn);
                }
            }
        }

        public static bool CanParticipateInMouseDisasterPregnancy(Pawn pawn)
        {
            return pawn?.ageTracker != null && pawn.ageTracker.AgeBiologicalYearsFloat >= MinimumMouseDisasterPregnancyAgeYears;
        }

        public static bool CanPrisonerScavenge(Pawn pawn)
        {
            if (!IsPrisonerScavengeEnabled)
            {
                return false;
            }

            return pawn != null &&
                   !pawn.Dead &&
                   !pawn.Downed &&
                   pawn.Spawned &&
                   pawn.Map != null &&
                   pawn.needs?.food != null &&
                   IsMouseEggOrChild(pawn) &&
                   IsColonyCaptiveForScavenge(pawn);
        }

        private static bool IsColonyCaptiveForScavenge(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony)
            {
                return true;
            }

            return pawn.guest != null &&
                   pawn.guest.HostFaction == Faction.OfPlayer &&
                   (pawn.guest.IsPrisoner || pawn.guest.IsSlave);
        }

        private static bool IsCompatibilitySensitivePawn(Pawn pawn)
        {
            if (pawn == null || !IsMouseEggOrChild(pawn))
            {
                return false;
            }

            return pawn.Faction == Faction.OfPlayer || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony;
        }

        private static bool ShouldUsePassiveAutoOrderMode(Pawn pawn)
        {
            return IsExtremeCompatJobModeEnabled && IsCompatibilitySensitivePawn(pawn);
        }

        private static bool CanAutoOrderJob(Pawn pawn, bool requireStarving)
        {
            if (pawn?.jobs == null || pawn.Dead || !pawn.Spawned)
            {
                return false;
            }

            if (!ShouldUsePassiveAutoOrderMode(pawn))
            {
                return true;
            }

            if (pawn.CurJob != null)
            {
                JobDef currentDef = pawn.CurJob.def;
                if (currentDef != JobDefOf.Wait && currentDef != JobDefOf.Wait_Wander && currentDef != JobDefOf.LayDown && currentDef != JobDefOf.GotoWander)
                {
                    return false;
                }
            }

            if (!requireStarving)
            {
                return true;
            }

            return pawn.needs?.food != null && pawn.needs.food.CurCategory == HungerCategory.Starving;
        }

        private static bool TryTakeAutoOrderedJob(Pawn pawn, Job job, JobTag tag, bool requireStarving)
        {
            if (job == null || !CanAutoOrderJob(pawn, requireStarving))
            {
                return false;
            }

            return pawn.jobs.TryTakeOrderedJob(job, tag);
        }

        public static bool CanPrisonerBiteTail(Pawn pawn)
        {
            return IsExperimentalTailBiteEnabled &&
                   pawn != null &&
                   pawn.IsPrisonerOfColony &&
                   CanPrisonerScavenge(pawn) &&
                   pawn.needs.food.CurCategory >= HungerCategory.Hungry;
        }

        public static bool IsWildMouseDisasterKind(Pawn pawn)
        {
            return pawn != null && pawn.kindDef != null &&
                   (pawn.kindDef == MouseDisasterDefOf.MouseDisaster_WildRatkinAdult ||
                    pawn.kindDef == MouseDisasterDefOf.MouseDisaster_WildRatkinChild);
        }

        public static bool IsThiefPawn(Pawn pawn)
        {
            return pawn != null && pawn.kindDef != null &&
                   (pawn.kindDef == MouseDisasterDefOf.MouseDisaster_ThiefRatkinAdult ||
                    pawn.kindDef == MouseDisasterDefOf.MouseDisaster_ThiefRatkinChild);
        }

        public static bool IsInThiefMentalState(Pawn pawn)
        {
            return pawn?.MentalStateDef == MouseDisasterDefOf.MouseDisaster_ThievingState;
        }

        public static bool IsThiefChildPawn(Pawn pawn)
        {
            return pawn != null && pawn.kindDef == MouseDisasterDefOf.MouseDisaster_ThiefRatkinChild;
        }

        public static bool IsBeggarPawn(Pawn pawn)
        {
            return pawn != null && pawn.kindDef != null &&
                   (pawn.kindDef == MouseDisasterDefOf.MouseDisaster_BeggarRatkinAdult ||
                    pawn.kindDef == MouseDisasterDefOf.MouseDisaster_BeggarRatkinChild);
        }

        public static bool IsMouseDisasterIncidentVisitor(Pawn pawn)
        {
            return IsBeggarPawn(pawn) ||
                   IsThiefPawn(pawn) ||
                   IsWildMouseDisasterKind(pawn) ||
                   IsMouseDisasterTraderAdult(pawn) ||
                   IsMouseEggOrChild(pawn);
        }

        public static bool IsInBeggarMentalState(Pawn pawn)
        {
            return pawn?.MentalStateDef == MouseDisasterDefOf.MouseDisaster_BeggingState;
        }

        public static bool CanMouseDisasterVisitorRetaliate(Pawn pawn)
        {
            if (pawn?.mindState == null)
            {
                return false;
            }

            if (pawn.Faction != null && Faction.OfPlayer != null && pawn.Faction.HostileTo(Faction.OfPlayer))
            {
                return true;
            }

            int nowTick = Find.TickManager?.TicksGame ?? 0;
            return nowTick - pawn.mindState.lastHarmTick <= 2500;
        }

        public static bool AreWildIncidentsEnabled => MouseDisasterMod.Settings == null || MouseDisasterMod.Settings.enableWildRatkinIncidents;

        public static bool AreThiefIncidentsEnabled => MouseDisasterMod.Settings == null || MouseDisasterMod.Settings.enableThiefIncidents;

        public static bool AreBeggarIncidentsEnabled => MouseDisasterMod.Settings == null || MouseDisasterMod.Settings.enableBeggarIncidents;

        public static bool IsGnawingEnabled => MouseDisasterMod.Settings == null || MouseDisasterMod.Settings.enableGnawing;

        public static bool IsPrisonerScavengeEnabled => MouseDisasterMod.Settings == null || MouseDisasterMod.Settings.enablePrisonerScavenge;

        public static bool IsFloatingTextEnabled => MouseDisasterMod.Settings == null || MouseDisasterMod.Settings.enableFloatingText;

        public static bool IsPrisonerScavengeDebugLogEnabled => MouseDisasterMod.Settings != null && MouseDisasterMod.Settings.enablePrisonerScavengeDebugLog;

        public static bool IsExperimentalTailBiteEnabled => MouseDisasterMod.Settings != null && MouseDisasterMod.Settings.enableExperimentalTailBite;


        public static bool IsExtremeCompatJobModeEnabled
        {
            get
            {
                if (extremeCompatJobModeCached.HasValue)
                {
                    return extremeCompatJobModeCached.Value;
                }

                extremeCompatJobModeCached = ComputeExtremeCompatJobMode();
                return extremeCompatJobModeCached.Value;
            }
        }

        private static bool ComputeExtremeCompatJobMode()
        {
            List<ModContentPack> runningMods = LoadedModManager.RunningModsListForReading;
            for (int i = 0; i < runningMods.Count; i++)
            {
                ModContentPack mod = runningMods[i];
                string name = mod?.Name ?? string.Empty;
                string packageId = mod?.PackageIdPlayerFacing ?? string.Empty;
                for (int keyIndex = 0; keyIndex < ExtremeCompatModKeywords.Length; keyIndex++)
                {
                    string keyword = ExtremeCompatModKeywords[keyIndex];
                    if (name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        packageId.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsBabyExpansionEnabled
        {
            get
            {
                if (babyExpansionEnabledCached.HasValue)
                {
                    return babyExpansionEnabledCached.Value;
                }

                babyExpansionEnabledCached = LoadedModManager.RunningModsListForReading.Any(mod =>
                {
                    string packageId = mod?.PackageIdPlayerFacing ?? string.Empty;
                    return packageId.Equals(BabyExpansionPackageId, StringComparison.OrdinalIgnoreCase);
                });
                return babyExpansionEnabledCached.Value;
            }
        }

        public static bool IsLeadYourPetEnabled
        {
            get
            {
                if (leadYourPetEnabledCached.HasValue)
                {
                    return leadYourPetEnabledCached.Value;
                }

                leadYourPetEnabledCached = LoadedModManager.RunningModsListForReading.Any(mod =>
                {
                    string packageId = mod?.PackageIdPlayerFacing ?? string.Empty;
                    return MouseDisasterBabyLeashPolicy.IsKnownLeadYourPetPackageId(packageId);
                });
                return leadYourPetEnabledCached.Value;
            }
        }

        public static bool IsHospitalityEnabled
        {
            get
            {
                if (hospitalityEnabledCached.HasValue)
                {
                    return hospitalityEnabledCached.Value;
                }

                hospitalityEnabledCached = LoadedModManager.RunningModsListForReading.Any(mod =>
                {
                    string packageId = mod?.PackageIdPlayerFacing ?? string.Empty;
                    return packageId.Equals(HospitalityPackageId, StringComparison.OrdinalIgnoreCase);
                });
                return hospitalityEnabledCached.Value;
            }
        }

        public static bool IsVanillaNutrientPasteExpandedEnabled
        {
            get
            {
                if (vanillaNutrientPasteExpandedEnabledCached.HasValue)
                {
                    return vanillaNutrientPasteExpandedEnabledCached.Value;
                }

                vanillaNutrientPasteExpandedEnabledCached = LoadedModManager.RunningModsListForReading.Any(mod =>
                {
                    string packageId = mod?.PackageIdPlayerFacing ?? string.Empty;
                    return packageId.Equals(VanillaNutrientPasteExpandedPackageId, StringComparison.OrdinalIgnoreCase);
                });
                return vanillaNutrientPasteExpandedEnabledCached.Value;
            }
        }

        public static bool TryFindFormerFaction(out Faction formerFaction)
        {
            return TryGetMouseDisasterHiddenFaction(out formerFaction);
        }

        public static bool TryGetMouseDisasterHiddenFaction(out Faction faction)
        {
            faction = null;
            if (Find.FactionManager == null || MouseDisasterDefOf.MouseDisaster_HiddenFaction == null)
            {
                return false;
            }

            if (mouseDisasterHiddenFaction != null && Find.FactionManager.AllFactionsListForReading.Contains(mouseDisasterHiddenFaction))
            {
                faction = mouseDisasterHiddenFaction;
                NormalizeHiddenFactionDisplayName(faction);
                EnsureHiddenFactionRelations(faction);
                return true;
            }

            faction = Find.FactionManager.FirstFactionOfDef(MouseDisasterDefOf.MouseDisaster_HiddenFaction);
            if (faction == null)
            {
                faction = CreateMouseDisasterHiddenFaction();
                if (faction == null)
                {
                    return false;
                }

                Find.FactionManager.Add(faction);
            }

            mouseDisasterHiddenFaction = faction;
            NormalizeHiddenFactionDisplayName(faction);
            EnsureHiddenFactionRelations(faction);
            return faction != null;
        }

        private static Faction CreateMouseDisasterHiddenFaction()
        {
            FactionDef factionDef = MouseDisasterDefOf.MouseDisaster_HiddenFaction;
            if (factionDef == null || Find.UniqueIDsManager == null)
            {
                return null;
            }

            Faction faction = new Faction
            {
                def = factionDef,
                loadID = Find.UniqueIDsManager.GetNextFactionID(),
                hidden = true
            };
            faction.colorFromSpectrum = FactionGenerator.NewRandomColorFromSpectrum(faction);

            if (factionDef.humanlikeFaction)
            {
                faction.ideos = new FactionIdeosTracker(faction);
                faction.ideos.ChooseOrGenerateIdeo(new IdeoGenerationParms(
                    factionDef,
                    forceNoExpansionIdeo: false,
                    forcedMemes: factionDef.forcedMemes,
                    classicExtra: false,
                    forceNoWeaponPreference: false,
                    forNewFluidIdeo: false,
                    fixedIdeo: factionDef.fixedIdeo,
                    name: factionDef.ideoName,
                    styles: factionDef.styles,
                    deities: factionDef.deityPresets,
                    hidden: factionDef.hiddenIdeo,
                    description: factionDef.ideoDescription,
                    requiredPreceptsOnly: factionDef.requiredPreceptsOnly));
            }

            faction.Name = !factionDef.fixedName.NullOrEmpty()
                ? factionDef.fixedName
                : HiddenFactionChineseName;
            return faction;
        }

        public static void TryRefreshHiddenFactionRelations()
        {
            if (TryGetMouseDisasterHiddenFaction(out Faction faction))
            {
                EnsureHiddenFactionRelations(faction);
            }
        }

        public static void TryRepairMissingFactionRelations()
        {
            if (Find.FactionManager == null)
            {
                return;
            }

            int nowTick = Find.TickManager?.TicksGame ?? 0;
            IReadOnlyList<Faction> factions = Find.FactionManager.AllFactionsListForReading;
            if (factions == null || factions.Count == 0)
            {
                return;
            }

            if (factions.Count == lastFactionRelationRepairFactionCount && nowTick - lastFactionRelationRepairTick < MissingFactionRelationRepairIntervalTicks)
            {
                return;
            }

            lastFactionRelationRepairTick = nowTick;
            lastFactionRelationRepairFactionCount = factions.Count;
            int repairedRelations = 0;

            for (int i = 0; i < factions.Count; i++)
            {
                Faction first = factions[i];
                if (first == null)
                {
                    continue;
                }

                for (int j = i + 1; j < factions.Count; j++)
                {
                    Faction second = factions[j];
                    if (second == null)
                    {
                        continue;
                    }

                    if (EnsureFactionRelationExists(first, second))
                    {
                        repairedRelations++;
                    }

                    if (EnsureFactionRelationExists(second, first))
                    {
                        repairedRelations++;
                    }
                }
            }

            if (repairedRelations > 0 && IsPrisonerScavengeDebugLogEnabled)
            {
                Log.Message("[MouseDisaster][FactionRelationRepair] repaired missing relations: " + repairedRelations);
            }
        }

        public static bool TryFindEntryCell(Map map, out IntVec3 cell)
        {
            return CellFinder.TryFindRandomEdgeCellWith(c => map.reachability.CanReachColony(c), map, CellFinder.EdgeRoadChance_Ignore, out cell);
        }

        private static Pawn GenerateRatkinPawn(PawnKindDef kindDef, Faction formerFaction, DevelopmentalStage stage, bool allowViolenceDisabledTraits = false)
        {
            if (kindDef == null)
            {
                return null;
            }

            ThingDef expectedRace = ResolveRatkinRaceDef(kindDef);
            PawnKindDef generationKindDef = ResolveRatkinKindDef(kindDef, stage, expectedRace);
            expectedRace ??= ResolveRatkinRaceDef(generationKindDef);
            if (generationKindDef == null || expectedRace == null)
            {
                return null;
            }

            Pawn pawn = null;
            for (int attempt = 0; attempt < 4; attempt++)
            {
                Faction requestFaction = attempt == 0 ? formerFaction : null;
                Pawn generated = PawnGenerator.GeneratePawn(CreateRatkinGenerationRequest(generationKindDef, requestFaction, stage, allowViolenceDisabledTraits));
                if (generated != null && (expectedRace == null || generated.def == expectedRace))
                {
                    pawn = generated;
                    break;
                }

                generated?.Destroy(DestroyMode.Vanish);
            }

            if (!IsRatkin(pawn))
            {
                return null;
            }

            EnsureRatkinIdentity(pawn, generationKindDef, stage);
            EnsureMouseDisasterBackstories(pawn, stage);
            NormalizeMouseEggAge(pawn, stage);
            EnsureRatEggMobility(pawn);
            EnsureStageInventoryClear(pawn, stage);
            StripRatEggEquipmentIfNeeded(pawn, stage);
            TryApplyRandomRatEggMissingParts(pawn);
            ApplyMouseDisasterGenes(pawn);
            TryApplyRatEggExtendedTrait(pawn, stage);
            NormalizeGeneratedTraits(pawn, stage);
            AssignDisasterApparel(pawn, stage);

            if (stage.Adult() && (pawn.ageTracker.AgeBiologicalYearsFloat < RatkinAdultMinAgeYears || pawn.ageTracker.AgeBiologicalYearsFloat > RatkinAdultMaxAgeYears))
            {
                SetBiologicalAgeYears(pawn, Rand.Range(RatkinAdultMinAgeYears, RatkinAdultMaxAgeYears));
            }

            if (stage == DevelopmentalStage.Child && (pawn.ageTracker.AgeBiologicalYearsFloat < RatkinYoungChildMinAgeYears || pawn.ageTracker.AgeBiologicalYearsFloat > RatkinYoungChildMaxAgeYears))
            {
                SetBiologicalAgeYears(pawn, Rand.Range(RatkinYoungChildMinAgeYears, RatkinYoungChildMaxAgeYears));
            }

            RefreshRatkinDevelopmentalPresentation(pawn);

            return pawn;
        }

        private static PawnGenerationRequest CreateRatkinGenerationRequest(PawnKindDef kindDef, Faction faction, DevelopmentalStage stage, bool allowViolenceDisabledTraits)
        {
            return new PawnGenerationRequest(
                kindDef,
                faction,
                canGeneratePawnRelations: false,
                mustBeCapableOfViolence: !allowViolenceDisabledTraits && stage != DevelopmentalStage.Baby,
                developmentalStages: stage,
                forceBaselinerChance: 0f,
                allowedXenotypes: ResolveAllowedRatkinXenotypes());
        }

        private static void EnsureRatkinIdentity(Pawn pawn, PawnKindDef generatedKindDef, DevelopmentalStage stage)
        {
            if (pawn == null)
            {
                return;
            }

            if (generatedKindDef != null && generatedKindDef.race == pawn.def)
            {
                pawn.kindDef = generatedKindDef;
            }

            if (!ModsConfig.BiotechActive || pawn.genes == null)
            {
                return;
            }
        }

        private static void EnsureMouseDisasterBackstories(Pawn pawn, DevelopmentalStage stage)
        {
            if (pawn?.story == null)
            {
                return;
            }

            pawn.story.Childhood = MouseDisasterDefOf.MouseDisaster_Newborn;
            pawn.story.Adulthood = stage.Adult() ? MouseDisasterDefOf.MouseDisaster_Refugee : null;
        }

        private static void NormalizeMouseEggAge(Pawn pawn, DevelopmentalStage stage)
        {
            if (pawn?.ageTracker == null)
            {
                return;
            }

            float ageYears = pawn.ageTracker.AgeBiologicalYearsFloat;
            if (stage == DevelopmentalStage.Baby)
            {
                if (ageYears < RatEggMinAgeYears || ageYears >= 3f)
                {
                    SetBiologicalAgeYears(pawn, Rand.Range(RatEggMinAgeYears, RatEggMaxAgeYears));
                }
                return;
            }

            if (stage == DevelopmentalStage.Child && (ageYears < RatkinYoungChildMinAgeYears || ageYears > RatkinYoungChildMaxAgeYears))
            {
                SetBiologicalAgeYears(pawn, Rand.Range(RatkinYoungChildMinAgeYears, RatkinYoungChildMaxAgeYears));
                return;
            }

            if (stage.Adult() && (ageYears < RatkinAdultMinAgeYears || ageYears > RatkinAdultMaxAgeYears))
            {
                SetBiologicalAgeYears(pawn, Rand.Range(RatkinAdultMinAgeYears, RatkinAdultMaxAgeYears));
            }
        }

        private static void AssignBodyTypeForCurrentStage(Pawn pawn)
        {
            if (pawn?.story == null)
            {
                return;
            }

            DevelopmentalStage stage = pawn.DevelopmentalStage;
            if (ModsConfig.BiotechActive)
            {
                if (stage == DevelopmentalStage.Baby && BodyTypeDefOf.Baby != null)
                {
                    pawn.story.bodyType = BodyTypeDefOf.Baby;
                    return;
                }

                if (stage == DevelopmentalStage.Child && BodyTypeDefOf.Child != null)
                {
                    pawn.story.bodyType = BodyTypeDefOf.Child;
                    return;
                }
            }

            if (stage.Adult())
            {
                pawn.story.bodyType = BodyTypeDefOf.Thin ?? (pawn.gender == Gender.Female ? BodyTypeDefOf.Female : BodyTypeDefOf.Male);
            }
        }

        private static void AssignDisasterApparel(Pawn pawn, DevelopmentalStage stage)
        {
            if (pawn?.apparel == null)
            {
                return;
            }

            if (stage.Adult())
            {
                EnsureAdultDisasterApparel(pawn);
                return;
            }

            for (int index = pawn.apparel.WornApparel.Count - 1; index >= 0; index--)
            {
                Apparel current = pawn.apparel.WornApparel[index];
                pawn.apparel.Remove(current);
                current.Destroy();
            }

            if (stage == DevelopmentalStage.Baby && TryAssignSeasonalBabyDisasterApparel(pawn))
            {
                return;
            }

            List<ThingDef> apparelPool;
            if (stage == DevelopmentalStage.Baby)
            {
                apparelPool = GetRestrictedApparelDefs(BabyRestrictedApparelDefNames);
            }
            else
            {
                apparelPool = GetRestrictedApparelDefs(ChildRestrictedApparelDefNames);
            }

            if (apparelPool.Count == 0)
            {
                return;
            }

            int targetCount = Mathf.Min(GetDesiredApparelCount(stage), apparelPool.Count);
            if (targetCount <= 0)
            {
                return;
            }

            int equipped = 0;
            List<ThingDef> randomizedPool = apparelPool.InRandomOrder().ToList();
            for (int attempt = 0; attempt < randomizedPool.Count && equipped < targetCount; attempt++)
            {
                if (TryWearRandomizedApparel(pawn, randomizedPool[attempt]))
                {
                    equipped++;
                }
            }
        }

        private static bool TryAssignSeasonalBabyDisasterApparel(Pawn pawn)
        {
            if (pawn == null || pawn.DevelopmentalStage != DevelopmentalStage.Baby)
            {
                return false;
            }

            switch (ResolveCurrentSeasonForDisasterPawn(pawn))
            {
                case Season.Summer:
                case Season.PermanentSummer:
                    return TryWearSpecificDisasterApparel(pawn, KidTribalApparelDefName) |
                           TryWearSpecificDisasterApparel(pawn, SunHatApparelDefName);
                case Season.Winter:
                case Season.PermanentWinter:
                    return TryWearSpecificDisasterApparel(pawn, BabyOnesieApparelDefName) |
                           TryWearSpecificDisasterApparel(pawn, WarmerHatApparelDefName);
                case Season.Spring:
                case Season.Fall:
                default:
                    return TryWearSpecificDisasterApparel(pawn, BabyOnesieApparelDefName);
            }
        }

        private static Season ResolveCurrentSeasonForDisasterPawn(Pawn pawn)
        {
            Map map = pawn?.MapHeld ?? Find.AnyPlayerHomeMap ?? Find.CurrentMap;
            if (map == null || !map.Tile.Valid)
            {
                return Season.Undefined;
            }

            return GenLocalDate.Season(map.Tile);
        }

        private static bool TryWearSpecificDisasterApparel(Pawn pawn, string apparelDefName)
        {
            if (pawn?.apparel == null || apparelDefName.NullOrEmpty())
            {
                return false;
            }

            ThingDef apparelDef = DefDatabase<ThingDef>.GetNamedSilentFail(apparelDefName);
            if (apparelDef == null || !apparelDef.IsApparel || !apparelDef.MadeFromStuff)
            {
                return false;
            }

            return TryWearRandomizedApparel(pawn, apparelDef);
        }

        private static void EnsureAdultDisasterApparel(Pawn pawn)
        {
            if (pawn?.apparel == null)
            {
                return;
            }

            if (pawn.apparel.WornApparel.Count > 0)
            {
                for (int i = 0; i < pawn.apparel.WornApparel.Count; i++)
                {
                    Apparel worn = pawn.apparel.WornApparel[i];
                    ApplyRandomizedApparelQuality(worn);
                    ApplyRandomizedApparelDurability(worn);
                }

                return;
            }

            List<ThingDef> adultPool = GetAdultApparelDefs(pawn);
            if (adultPool.Count == 0)
            {
                return;
            }

            int targetCount = Mathf.Clamp(RollWeightedCount(0.02f, 0.16f, 0.34f, 0.30f, 0.18f), 1, 4);
            int equipped = 0;
            List<ThingDef> randomizedPool = adultPool.InRandomOrder().ToList();
            for (int i = 0; i < randomizedPool.Count && equipped < targetCount; i++)
            {
                if (TryWearRandomizedApparel(pawn, randomizedPool[i]))
                {
                    equipped++;
                }
            }
        }

        private static bool TryWearRandomizedApparel(Pawn pawn, ThingDef apparelDef)
        {
            if (pawn?.apparel == null || apparelDef == null)
            {
                return false;
            }

            ThingDef stuff = TryResolvePreferredApparelStuff(apparelDef);
            if (stuff == null)
            {
                return false;
            }

            Apparel apparel = ThingMaker.MakeThing(apparelDef, stuff) as Apparel;
            if (apparel == null ||
                !apparel.PawnCanWear(pawn, ignoreGender: true) ||
                !ApparelUtility.HasPartsToWear(pawn, apparel.def) ||
                !pawn.apparel.CanWearWithoutDroppingAnything(apparel.def))
            {
                apparel?.Destroy();
                return false;
            }

            ApplyRandomizedApparelQuality(apparel);
            pawn.apparel.Wear(apparel, dropReplacedApparel: false);
            ApplyRandomizedApparelDurability(apparel);
            return true;
        }

        private static int GetDesiredApparelCount(DevelopmentalStage stage)
        {
            if (stage == DevelopmentalStage.Baby)
            {
                return RollWeightedCount(0.30f, 0.50f, 0.18f, 0.02f);
            }

            if (stage == DevelopmentalStage.Child)
            {
                return RollWeightedCount(0.16f, 0.34f, 0.30f, 0.16f, 0.04f);
            }

            return 0;
        }

        private static int RollWeightedCount(params float[] weights)
        {
            if (weights == null || weights.Length == 0)
            {
                return 0;
            }

            float total = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                total += Mathf.Max(0f, weights[i]);
            }

            if (total <= 0f)
            {
                return 0;
            }

            float roll = Rand.Value * total;
            float cumulative = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += Mathf.Max(0f, weights[i]);
                if (roll <= cumulative)
                {
                    return i;
                }
            }

            return weights.Length - 1;
        }

        private static void TryApplyRatEggExtendedTrait(Pawn pawn, DevelopmentalStage stage)
        {
            MouseDisasterSettings settings = MouseDisasterMod.Settings;
            if ((settings != null && !settings.enableRatEggTraitsBridge) || pawn?.story?.traits == null || !IsRatkin(pawn))
            {
                return;
            }

            if (stage != DevelopmentalStage.Baby && stage != DevelopmentalStage.Child)
            {
                return;
            }

            float chance = Mathf.Clamp01(settings?.ratEggTraitGenerationChance ?? 0.5f);
            if (!Rand.Chance(chance))
            {
                return;
            }

            List<TraitDef> pool = GetRatEggTraitPool();
            if (pool.Count == 0)
            {
                return;
            }

            for (int attempt = 0; attempt < 32; attempt++)
            {
                TraitDef selected = pool.RandomElement();
                if (selected == null || !CanAssignTraitDef(pawn, selected))
                {
                    continue;
                }

                int degree = 0;
                if (selected.degreeDatas != null && selected.degreeDatas.Count > 0)
                {
                    degree = selected.degreeDatas[0].degree;
                }

                pawn.story.traits.GainTrait(new Trait(selected, degree, false), suppressConflicts: false);
                return;
            }
        }

        private static void NormalizeGeneratedTraits(Pawn pawn, DevelopmentalStage stage)
        {
            if (pawn?.story?.traits == null)
            {
                return;
            }

            TraitSet traitSet = pawn.story.traits;
            List<Trait> generatedTraits = traitSet.allTraits
                .Where(trait => trait != null && trait.sourceGene == null)
                .ToList();

            float randomShootingKeepChance = GetRandomShootingKeepChance(stage);
            for (int i = generatedTraits.Count - 1; i >= 0; i--)
            {
                Trait trait = generatedTraits[i];
                if (!IsRandomShootingTrait(trait) || Rand.Chance(randomShootingKeepChance))
                {
                    continue;
                }

                traitSet.RemoveTrait(trait);
                generatedTraits.RemoveAt(i);
            }

            int desiredCount = GetDesiredGeneratedTraitCount(stage);
            if (generatedTraits.Count > desiredCount)
            {
                int desiredNegativeCount = desiredCount > 0 ? Mathf.Clamp(Mathf.CeilToInt(desiredCount * NegativeTraitTargetRatio), 0, desiredCount) : 0;
                List<Trait> negatives = generatedTraits.Where(IsNegativeTrait).ToList();
                List<Trait> neutrals = generatedTraits.Where(trait => !IsNegativeTrait(trait)).ToList();
                List<Trait> kept = new List<Trait>();

                for (int i = 0; i < negatives.Count && kept.Count < desiredNegativeCount; i++)
                {
                    kept.Add(negatives[i]);
                }

                for (int i = 0; i < negatives.Count && kept.Count < desiredCount; i++)
                {
                    if (!kept.Contains(negatives[i]))
                    {
                        kept.Add(negatives[i]);
                    }
                }

                for (int i = 0; i < neutrals.Count && kept.Count < desiredCount; i++)
                {
                    kept.Add(neutrals[i]);
                }

                for (int i = generatedTraits.Count - 1; i >= 0; i--)
                {
                    Trait trait = generatedTraits[i];
                    if (!kept.Contains(trait))
                    {
                        traitSet.RemoveTrait(trait);
                    }
                }

                generatedTraits = kept.ToList();
            }

            int targetNegative = desiredCount > 0 ? Mathf.Clamp(Mathf.CeilToInt(desiredCount * NegativeTraitTargetRatio), 0, desiredCount) : 0;
            int currentNegative = generatedTraits.Count(IsNegativeTrait);
            int addAttempts = 0;
            while ((generatedTraits.Count < desiredCount || currentNegative < targetNegative) && addAttempts < 24)
            {
                addAttempts++;
                if (!TryAddRandomNegativeTrait(pawn, out Trait added))
                {
                    break;
                }

                generatedTraits.Add(added);
                currentNegative++;
            }

            if (generatedTraits.Count > desiredCount)
            {
                List<Trait> removable = generatedTraits.Where(trait => !IsNegativeTrait(trait)).ToList();
                for (int i = 0; i < removable.Count && generatedTraits.Count > desiredCount; i++)
                {
                    traitSet.RemoveTrait(removable[i]);
                    generatedTraits.Remove(removable[i]);
                }

                for (int i = generatedTraits.Count - 1; i >= 0 && generatedTraits.Count > desiredCount; i--)
                {
                    traitSet.RemoveTrait(generatedTraits[i]);
                    generatedTraits.RemoveAt(i);
                }
            }
        }

        private static int GetDesiredGeneratedTraitCount(DevelopmentalStage stage)
        {
            if (stage == DevelopmentalStage.Baby)
            {
                return RollWeightedCount(0.34f, 0.34f, 0.20f, 0.08f, 0.04f);
            }

            if (stage == DevelopmentalStage.Child)
            {
                return RollWeightedCount(0.26f, 0.28f, 0.24f, 0.14f, 0.08f);
            }

            return RollWeightedCount(0.20f, 0.26f, 0.26f, 0.16f, 0.12f);
        }

        private static bool TryAddRandomNegativeTrait(Pawn pawn, out Trait addedTrait)
        {
            addedTrait = null;
            if (pawn?.story?.traits == null)
            {
                return false;
            }

            List<TraitDef> pool = GetNegativeTraitPool();
            if (pool.Count == 0)
            {
                return false;
            }

            for (int attempt = 0; attempt < 32; attempt++)
            {
                TraitDef traitDef = pool.RandomElement();
                if (traitDef == null || !CanAssignTraitDef(pawn, traitDef))
                {
                    continue;
                }

                if (IsRandomShootingTrait(traitDef) && !Rand.Chance(RandomShootingAdultKeepChance))
                {
                    continue;
                }

                if (!TryPickNegativeDegree(traitDef, out int degree))
                {
                    continue;
                }

                Trait trait = new Trait(traitDef, degree, false);
                pawn.story.traits.GainTrait(trait, suppressConflicts: false);
                addedTrait = pawn.story.traits.allTraits
                    .LastOrDefault(existing => existing != null && existing.def == traitDef && existing.Degree == degree);
                return addedTrait != null;
            }

            return false;
        }

        private static List<TraitDef> GetNegativeTraitPool()
        {
            if (negativeTraitPoolResolved)
            {
                return negativeTraitPool ?? new List<TraitDef>();
            }

            negativeTraitPoolResolved = true;
            negativeTraitPool = DefDatabase<TraitDef>.AllDefsListForReading
                .Where(def => def != null && IsNegativeTraitDef(def) &&
                              def != TraitDefOf.Gay &&
                              def != TraitDefOf.Bisexual &&
                              def != TraitDefOf.Asexual)
                .ToList();
            return negativeTraitPool;
        }

        private static bool CanAssignTraitDef(Pawn pawn, TraitDef candidate)
        {
            if (pawn?.story?.traits == null || candidate == null || pawn.story.traits.HasTrait(candidate))
            {
                return false;
            }

            List<Trait> allTraits = pawn.story.traits.allTraits;
            for (int i = 0; i < allTraits.Count; i++)
            {
                Trait existing = allTraits[i];
                if (existing == null)
                {
                    continue;
                }

                if (existing.def.ConflictsWith(candidate) || candidate.ConflictsWith(existing.def))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryPickNegativeDegree(TraitDef traitDef, out int degree)
        {
            degree = 0;
            if (traitDef == null || traitDef.degreeDatas.NullOrEmpty())
            {
                return false;
            }

            List<TraitDegreeData> negativeDegrees = traitDef.degreeDatas
                .Where(data => data != null && IsNegativeTraitDef(traitDef, data.degree))
                .ToList();
            if (negativeDegrees.Count == 0)
            {
                return false;
            }

            TraitDegreeData picked = negativeDegrees.RandomElementByWeight(data => Mathf.Max(0.01f, data.commonality));
            degree = picked.degree;
            return true;
        }

        public static bool TryAddNegativeTraitByKeywords(Pawn pawn, params string[] keywords)
        {
            if (pawn?.story?.traits == null || keywords.NullOrEmpty())
            {
                return false;
            }

            List<TraitDef> matches = GetNegativeTraitPool()
                .Where(def => TraitMatchesAnyKeyword(def, keywords))
                .ToList();
            for (int i = 0; i < matches.Count; i++)
            {
                TraitDef match = matches[i];
                if (match == null || !CanAssignTraitDef(pawn, match) || !TryPickNegativeDegree(match, out int degree))
                {
                    continue;
                }

                pawn.story.traits.GainTrait(new Trait(match, degree, false), suppressConflicts: false);
                return true;
            }

            return false;
        }

        public static void MakeRatEggPureNegative(Pawn pawn, int minimumNegativeTraits, IEnumerable<string[]> preferredKeywordSets = null)
        {
            if (pawn?.story?.traits == null)
            {
                return;
            }

            TraitSet traitSet = pawn.story.traits;
            List<Trait> removable = traitSet.allTraits
                .Where(trait => trait != null && trait.sourceGene == null && !IsNegativeTrait(trait))
                .ToList();
            for (int i = 0; i < removable.Count; i++)
            {
                traitSet.RemoveTrait(removable[i]);
            }

            if (preferredKeywordSets != null)
            {
                foreach (string[] keywordSet in preferredKeywordSets)
                {
                    if (traitSet.allTraits.Count(IsNegativeTrait) >= minimumNegativeTraits)
                    {
                        break;
                    }

                    TryAddNegativeTraitByKeywords(pawn, keywordSet);
                }
            }

            int attempts = 0;
            while (traitSet.allTraits.Count(IsNegativeTrait) < minimumNegativeTraits && attempts < 24)
            {
                attempts++;
                if (!TryAddRandomNegativeTrait(pawn, out _))
                {
                    break;
                }
            }
        }

        private static bool TraitMatchesAnyKeyword(TraitDef traitDef, IEnumerable<string> keywords)
        {
            if (traitDef == null || keywords == null)
            {
                return false;
            }

            string source = ((traitDef.defName ?? string.Empty) + " " + (traitDef.label ?? string.Empty)).ToLowerInvariant();
            foreach (string keyword in keywords)
            {
                if (!keyword.NullOrEmpty() && source.Contains(keyword.ToLowerInvariant()))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsNegativeTrait(Trait trait)
        {
            return trait != null && IsNegativeTraitDef(trait.def, trait.Degree);
        }

        private static float GetRandomShootingKeepChance(DevelopmentalStage stage)
        {
            if (stage == DevelopmentalStage.Baby)
            {
                return RandomShootingBabyKeepChance;
            }

            if (stage == DevelopmentalStage.Child)
            {
                return RandomShootingChildKeepChance;
            }

            return RandomShootingAdultKeepChance;
        }

        private static bool IsRandomShootingTrait(Trait trait)
        {
            return trait != null && IsRandomShootingTrait(trait.def, trait.Degree);
        }

        private static bool IsRandomShootingTrait(TraitDef traitDef)
        {
            if (traitDef == null || traitDef.degreeDatas.NullOrEmpty())
            {
                return false;
            }

            for (int i = 0; i < traitDef.degreeDatas.Count; i++)
            {
                if (IsRandomShootingTrait(traitDef, traitDef.degreeDatas[i].degree))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsRandomShootingTrait(TraitDef traitDef, int degree)
        {
            if (traitDef == null)
            {
                return false;
            }

            TraitDegreeData data = traitDef.DataAtDegree(degree);
            if (data == null)
            {
                return false;
            }

            string source = ((traitDef.defName ?? string.Empty) + " " + (traitDef.label ?? string.Empty) + " " + (data.label ?? string.Empty)).ToLowerInvariant();
            for (int i = 0; i < RandomShootingTraitKeywords.Length; i++)
            {
                if (source.Contains(RandomShootingTraitKeywords[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsNegativeTraitDef(TraitDef traitDef)
        {
            if (traitDef == null || traitDef.degreeDatas.NullOrEmpty())
            {
                return false;
            }

            for (int i = 0; i < traitDef.degreeDatas.Count; i++)
            {
                TraitDegreeData data = traitDef.degreeDatas[i];
                if (data != null && IsNegativeTraitDef(traitDef, data.degree))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsNegativeTraitDef(TraitDef traitDef, int degree)
        {
            if (traitDef == null)
            {
                return false;
            }

            if (IsRandomShootingTrait(traitDef, degree))
            {
                return false;
            }

            TraitDegreeData data = traitDef.DataAtDegree(degree);
            if (data == null)
            {
                return false;
            }

            if (!traitDef.disabledWorkTypes.NullOrEmpty() || traitDef.disabledWorkTags != WorkTags.None)
            {
                return true;
            }

            if (data.forcedMentalState != null || data.randomMentalState != null || data.socialFightChanceFactor > 1.05f)
            {
                return true;
            }

            if (data.hungerRateFactor > 1.05f || data.painOffset > 0.05f || data.painFactor > 1.02f)
            {
                return true;
            }

            if (data.randomDiseaseMtbDays > 0f && data.randomDiseaseMtbDays < 999f)
            {
                return true;
            }

            string source = ((traitDef.defName ?? string.Empty) + " " + (data.label ?? string.Empty) + " " + (traitDef.label ?? string.Empty)).ToLowerInvariant();
            return source.Contains("lazy") ||
                   source.Contains("wimp") ||
                   source.Contains("slow") ||
                   source.Contains("greedy") ||
                   source.Contains("jealous") ||
                   source.Contains("pess") ||
                   source.Contains("depress") ||
                   source.Contains("neuro") ||
                   source.Contains("abrasive") ||
                   source.Contains("annoy") ||
                   source.Contains("病弱") ||
                   source.Contains("\u61d2") ||
                   source.Contains("迟缓") ||
                   source.Contains("\u8d2a") ||
                   source.Contains("悲观") ||
                   source.Contains("抑郁") ||
                   source.Contains("神经质");
        }

        private static List<TraitDef> GetRatEggTraitPool()
        {
            if (ratEggTraitPoolResolved)
            {
                return ratEggTraitPool ?? new List<TraitDef>();
            }

            ratEggTraitPoolResolved = true;
            ratEggTraitPool = DefDatabase<TraitDef>.AllDefsListForReading
                .Where(def => def != null &&
                              !def.degreeDatas.NullOrEmpty() &&
                              def != TraitDefOf.Gay &&
                              def != TraitDefOf.Bisexual &&
                              def != TraitDefOf.Asexual)
                .ToList();

            return ratEggTraitPool;
        }

        private static void ApplyMouseDisasterGenes(Pawn pawn)
        {
            if (!IsEligibleForMouseDisasterGenes(pawn))
            {
                StripMouseDisasterGenes(pawn);
                CleanupOrphanedChemicalDependencies(pawn);
                return;
            }

            if (HasManualMouseDisasterGeneRemovalMarker(pawn))
            {
                StripMouseDisasterGenes(pawn);
                CleanupOrphanedChemicalDependencies(pawn);
                return;
            }

            StripNonMouseDisasterGenes(pawn);
            StripMouseDisasterGenes(pawn);
            MouseDisasterSubtypeUtility.ApplySubtypeGenes(pawn);
            CleanupOrphanedChemicalDependencies(pawn);
        }

        public static bool HasActiveGene(Pawn pawn, GeneDef geneDef)
        {
            return ModsConfig.BiotechActive && geneDef != null && pawn?.genes != null && pawn.genes.HasActiveGene(geneDef);
        }

        public static bool HasAnyMouseDisasterGene(Pawn pawn)
        {
            if (!ModsConfig.BiotechActive || pawn?.genes == null)
            {
                return false;
            }

            return GetActiveMouseDisasterGenes(pawn).Count > 0;
        }

        public static void TryAssignBirthMouseDisasterGenes(Pawn newborn, Pawn parent)
        {
            if (!ModsConfig.BiotechActive || newborn?.genes == null || parent == null || !IsRatkin(newborn) || !IsRatkin(parent))
            {
                return;
            }

            if (HasManualMouseDisasterGeneRemovalMarker(newborn))
            {
                StripMouseDisasterGenes(newborn);
                CleanupOrphanedChemicalDependencies(newborn);
                return;
            }

            if (!IsEligibleForMouseDisasterGenes(parent) || !HasAnyMouseDisasterGene(parent))
            {
                StripMouseDisasterGenes(newborn);
                CleanupOrphanedChemicalDependencies(newborn);
                return;
            }

            StripNonMouseDisasterGenes(newborn);
            StripMouseDisasterGenes(newborn);
            MouseDisasterSubtypeUtility.ApplySubtypeGenes(newborn, parent);
            CleanupOrphanedChemicalDependencies(newborn);
        }

        public static void TryForceRatkinBirthXenotype(Pawn newborn, Pawn mother)
        {
            if (!ModsConfig.BiotechActive || newborn?.genes == null || mother == null)
            {
                return;
            }

            if (!MouseDisasterBirthPolicy.ShouldForceMouseDisasterBirthXenotype(
                    shouldUseMouseDisasterBirthIdentity: ShouldUseMouseDisasterBirthIdentity(newborn, mother),
                    motherIsRatkin: IsRatkin(mother),
                    newbornIsRatkin: IsRatkin(newborn),
                    newbornAlreadyRatkinXenotype: IsRatkinXenotypeDef(newborn.genes.Xenotype)))
            {
                return;
            }

            XenotypeDef ratkinXenotype = ResolveRatkinXenotypeDef();
            if (ratkinXenotype != null)
            {
                newborn.genes.SetXenotype(ratkinXenotype);
            }
        }

        private static List<GeneDef> GetMouseDisasterGenePool()
        {
            if (cachedMouseDisasterGenePoolResolved)
            {
                return cachedMouseDisasterGenePool;
            }

            cachedMouseDisasterGenePoolResolved = true;
            cachedMouseDisasterGenePool = new List<GeneDef>
            {
                MouseDisasterDefOf.MouseDisaster_Gene_MoreLitter,
                MouseDisasterDefOf.MouseDisaster_Gene_PrimalFertility,
                MouseDisasterDefOf.MouseDisaster_Gene_HyperBreeding,
                MouseDisasterDefOf.MouseDisaster_Gene_ChaosFertility,
                MouseDisasterDefOf.MouseDisaster_Gene_MoreComing
            }.Where(def => def != null).Distinct().ToList();
            return cachedMouseDisasterGenePool;
        }

        public static void StripMouseDisasterGenes(Pawn pawn)
        {
            if (!ModsConfig.BiotechActive || pawn?.genes == null)
            {
                return;
            }

            List<GeneDef> pool = GetMouseDisasterGenePool();
            if (pool.Count == 0)
            {
                return;
            }

            List<Gene> removable = pawn.genes.GenesListForReading
                .Where(gene => gene != null && gene.def != null && pool.Contains(gene.def))
                .ToList();

            using (new MouseDisasterGeneRemovalScope(pawn))
            {
                for (int i = 0; i < removable.Count; i++)
                {
                    pawn.genes.RemoveGene(removable[i]);
                }
            }
        }

        public static bool HasManualMouseDisasterGeneRemovalMarker(Pawn pawn)
        {
            return GeneRestoreState?.HasManualRemovalMarker(pawn) ?? false;
        }

        public static void MarkManualMouseDisasterGeneRemoval(Pawn pawn)
        {
            GeneRestoreState?.MarkManualRemoval(pawn);
        }

        public static void ClearManualMouseDisasterGeneRemovalMarker(Pawn pawn)
        {
            GeneRestoreState?.ClearManualRemoval(pawn);
        }

        public static void BeginInternalMouseDisasterGeneRemoval(Pawn pawn)
        {
            GeneRestoreState?.BeginInternalGeneRemoval(pawn);
        }

        public static void EndInternalMouseDisasterGeneRemoval(Pawn pawn)
        {
            GeneRestoreState?.EndInternalGeneRemoval(pawn);
        }

        public static bool IsInternalMouseDisasterGeneRemovalInProgress(Pawn pawn)
        {
            return GeneRestoreState?.IsInternalGeneRemovalInProgress(pawn) ?? false;
        }

        public static bool IsMouseDisasterManagedGenePawn(Pawn pawn)
        {
            return pawn != null && ModsConfig.BiotechActive && pawn.genes != null && (IsMouseDisasterPawn(pawn) || IsMouseDisasterIncidentVisitor(pawn));
        }

        public static bool IsMouseDisasterGeneDef(GeneDef geneDef)
        {
            return geneDef != null && GetMouseDisasterGenePool().Contains(geneDef);
        }

        public static void NotifyMouseDisasterGeneRemoved(Pawn pawn, GeneDef geneDef)
        {
            if (!MouseDisasterGeneRestorePolicy.ShouldTrackManualRemovalOnGeneRemoved(
                    isManagedPawn: IsMouseDisasterManagedGenePawn(pawn),
                    removedMouseDisasterGene: IsMouseDisasterGeneDef(geneDef)) ||
                IsInternalMouseDisasterGeneRemovalInProgress(pawn))
            {
                return;
            }

            MarkManualMouseDisasterGeneRemoval(pawn);
        }

        public static void NotifyMouseDisasterGeneAdded(Pawn pawn, GeneDef geneDef)
        {
            if (!MouseDisasterGeneRestorePolicy.ShouldClearManualRemovalOnGeneAdded(
                    hasManualRemovalMarker: HasManualMouseDisasterGeneRemovalMarker(pawn),
                    isManagedPawn: IsMouseDisasterManagedGenePawn(pawn),
                    addedMouseDisasterGene: IsMouseDisasterGeneDef(geneDef)))
            {
                return;
            }

            ClearManualMouseDisasterGeneRemovalMarker(pawn);
        }

        public static void CleanupOrphanedChemicalDependencies(Pawn pawn)
        {
            if (!ModsConfig.BiotechActive || pawn?.health?.hediffSet?.hediffs == null)
            {
                return;
            }

            List<Hediff> removable = pawn.health.hediffSet.hediffs
                .Where(hediff => hediff is Hediff_ChemicalDependency dependency &&
                                 (dependency.chemical == null || dependency.LinkedGene == null))
                .ToList();

            for (int i = 0; i < removable.Count; i++)
            {
                pawn.health.RemoveHediff(removable[i]);
            }
        }

        private static void StripNonMouseDisasterGenes(Pawn pawn)
        {
            if (!ModsConfig.BiotechActive || pawn?.genes == null)
            {
                return;
            }

            List<GeneDef> pool = GetMouseDisasterGenePool();
            if (pool.Count == 0)
            {
                return;
            }

            List<Gene> removable = pawn.genes.GenesListForReading
                .Where(gene => gene != null &&
                               gene.def != null &&
                               pawn.genes.IsXenogene(gene) &&
                               !pool.Contains(gene.def))
                .ToList();

            using (new MouseDisasterGeneRemovalScope(pawn))
            {
                for (int i = 0; i < removable.Count; i++)
                {
                    pawn.genes.RemoveGene(removable[i]);
                }
            }
        }

        private static List<GeneDef> GetActiveMouseDisasterGenes(Pawn pawn)
        {
            if (!ModsConfig.BiotechActive || pawn?.genes == null)
            {
                return new List<GeneDef>();
            }

            List<GeneDef> pool = GetMouseDisasterGenePool();
            return pool.Where(pawn.genes.HasActiveGene).ToList();
        }

        public static Pawn GenerateWildPawn(PawnKindDef kindDef, Faction formerFaction, DevelopmentalStage stage, bool allowViolenceDisabledTraits = false)
        {
            Pawn pawn = GenerateRatkinPawn(kindDef, formerFaction, stage, allowViolenceDisabledTraits);
            if (pawn == null)
            {
                return null;
            }

            if (formerFaction != null && pawn.Faction != formerFaction)
            {
                pawn.SetFaction(formerFaction);
            }

            SetMouseDisasterFoodLevel(pawn);
            return pawn;
        }

        public static Pawn GenerateThiefPawn(PawnKindDef kindDef, Faction formerFaction, DevelopmentalStage stage, bool allowViolenceDisabledTraits = false)
        {
            Pawn pawn = GenerateWildPawn(kindDef, formerFaction, stage, allowViolenceDisabledTraits);
            if (pawn == null)
            {
                return null;
            }

            pawn.mindState?.mentalStateHandler?.TryStartMentalState(MouseDisasterDefOf.MouseDisaster_ThievingState, null, forced: true, forceWake: true, causedByMood: false, otherPawn: null, transitionSilently: true);
            if (pawn.inventory != null)
            {
                pawn.inventory.innerContainer.ClearAndDestroyContents();
            }

            SetMouseDisasterFoodLevel(pawn);
            return pawn;
        }

        public static Pawn GenerateBeggarPawn(PawnKindDef kindDef, Faction formerFaction, DevelopmentalStage stage, bool allowViolenceDisabledTraits = false)
        {
            Pawn pawn = GenerateThiefPawn(kindDef, formerFaction, stage, allowViolenceDisabledTraits);
            if (pawn == null)
            {
                return null;
            }

            pawn.mindState?.mentalStateHandler?.TryStartMentalState(MouseDisasterDefOf.MouseDisaster_BeggingState, null, forced: true, forceWake: true, causedByMood: false, otherPawn: null, transitionSilently: true);
            ResetBeggarState(pawn);
            return pawn;
        }

        public static Pawn GenerateFactionRatkinPawn(PawnKindDef kindDef, Faction faction, DevelopmentalStage stage, float foodPercentage = 0.35f, bool allowViolenceDisabledTraits = false)
        {
            Pawn pawn = GenerateRatkinPawn(kindDef, faction, stage, allowViolenceDisabledTraits);
            if (pawn == null)
            {
                return null;
            }

            if (faction != null && pawn.Faction != faction)
            {
                pawn.SetFaction(faction);
            }

            SetMouseDisasterFoodLevel(pawn, foodPercentage);

            return pawn;
        }

        public static IEnumerable<Pawn> SpawnTravelerGroup(Map map, IntVec3 center, int adults, int children, Faction faction = null, float foodPercentage = 0.35f)
        {
            List<Pawn> pawns = new List<Pawn>();
            if (map == null)
            {
                return pawns;
            }

            if (faction == null)
            {
                TryFindFormerFaction(out faction);
            }

            for (int index = 0; index < adults; index++)
            {
                Pawn pawn = GenerateFactionRatkinPawn(MouseDisasterDefOf.MouseDisaster_BeggarRatkinAdult, faction, DevelopmentalStage.Adult, foodPercentage, allowViolenceDisabledTraits: true);
                if (pawn == null)
                {
                    continue;
                }

                GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(center, map, 6), map);
                pawns.Add(pawn);
            }

            for (int index = 0; index < children; index++)
            {
                Pawn pawn = GenerateFactionRatkinPawn(MouseDisasterDefOf.MouseDisaster_BeggarRatkinChild, faction, DevelopmentalStage.Child, foodPercentage, allowViolenceDisabledTraits: true);
                if (pawn == null)
                {
                    continue;
                }

                GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(center, map, 6), map);
                StripRatEggInventory(pawn);
                pawns.Add(pawn);
            }

            return pawns;
        }

        private static void SetMouseDisasterFoodLevel(Pawn pawn, float? preferredPercent = null)
        {
            if (pawn?.needs?.food == null)
            {
                return;
            }

            float maxLevel = Mathf.Max(0.001f, pawn.needs.food.MaxLevel);
            float normalizedPercent = preferredPercent.HasValue
                ? Mathf.Clamp(preferredPercent.Value, MouseDisasterFoodMinPercent, MouseDisasterFoodMaxPercent)
                : Rand.Range(MouseDisasterFoodMinPercent, MouseDisasterFoodMaxPercent);

            if (maxLevel >= MouseDisasterFoodMaxAbsolute)
            {
                float absoluteLevel = Mathf.Lerp(MouseDisasterFoodMinAbsolute, MouseDisasterFoodMaxAbsolute, Mathf.InverseLerp(MouseDisasterFoodMinPercent, MouseDisasterFoodMaxPercent, normalizedPercent));
                pawn.needs.food.CurLevel = Mathf.Clamp(absoluteLevel, 0f, maxLevel);
                return;
            }

            pawn.needs.food.CurLevel = Mathf.Clamp(maxLevel * normalizedPercent, 0f, maxLevel);
        }

        public static void SendIncidentLetter(IncidentDef def, IncidentParms parms, LookTargets lookTargets)
        {
            Find.LetterStack.ReceiveLetter(def.letterLabel, def.letterText, def.letterDef, lookTargets, parms.faction);
        }

        public static bool SendFoodGiveLetter(IncidentDef def, IncidentParms parms, Map map, IEnumerable<Pawn> recipients)
        {
            List<Pawn> validRecipients = recipients?
                .Where(pawn => pawn != null && !pawn.Dead && pawn.Spawned && pawn.Map == map)
                .Distinct()
                .ToList() ?? new List<Pawn>();
            if (def == null || map == null || validRecipients.Count == 0)
            {
                return false;
            }

            ChoiceLetter_MouseDisasterFoodGive letter =
                LetterMaker.MakeLetter(def.letterLabel, def.letterText, MouseDisasterDefOf.MouseDisaster_FoodGiveLetter, validRecipients) as ChoiceLetter_MouseDisasterFoodGive;
            if (letter == null)
            {
                return false;
            }

            letter.map = map;
            letter.recipients = validRecipients;
            Find.LetterStack.ReceiveLetter(letter, null);
            return true;
        }

        public static void EnsureTradeLeader(Pawn pawn, TraderKindDef preferredTraderKind)
        {
            if (pawn == null)
            {
                return;
            }

            if (pawn.mindState != null)
            {
                pawn.mindState.wantsToTradeWithColony = true;
            }
            if (pawn.kindDef != null && !pawn.kindDef.trader)
            {
                pawn.kindDef.trader = true;
            }

            PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn, actAsIfSpawned: true);
            if (pawn.trader != null)
            {
                pawn.trader.traderKind = preferredTraderKind ?? pawn.trader.traderKind;
            }
        }

        public static void ClearTradeLeaderState(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            if (pawn.mindState != null)
            {
                pawn.mindState.wantsToTradeWithColony = false;
            }

            if (pawn.trader != null)
            {
                pawn.trader.traderKind = null;
            }

            PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn, actAsIfSpawned: pawn.Spawned);
        }

        public static void LinkIncidentParentToChildren(Pawn adult, IEnumerable<Pawn> children)
        {
            if (adult == null || children == null)
            {
                return;
            }

            foreach (Pawn child in children)
            {
                if (child == null || child.Dead)
                {
                    continue;
                }

                bool alreadyLinked = child.relations?.DirectRelations != null &&
                                     child.relations.DirectRelations.Any(r => r.def == PawnRelationDefOf.Parent && r.otherPawn == adult);
                if (!alreadyLinked)
                {
                    child.relations?.AddDirectRelation(PawnRelationDefOf.Parent, adult);
                }

                MarkMapPawnCacheDirty(child);
            }

            MarkMapPawnCacheDirty(adult);
        }

        public static void TryStartLeadYourPetMotherLeashes(Pawn mother, IEnumerable<Pawn> babies)
        {
            if (mother == null || babies == null)
            {
                return;
            }

            List<Pawn> pawns = new List<Pawn> { mother };
            pawns.AddRange(babies.Where(baby => baby != null));
            TryStartLeadYourPetRelatedAdultLeashes(pawns);
        }

        public static void TryStartLeadYourPetRelatedAdultLeashes(IEnumerable<Pawn> pawns)
        {
            if (pawns == null || !IsLeadYourPetEnabled || Current.Game == null)
            {
                return;
            }

            EnsureLeadYourPetReflection();
            if (leadYourPetComponentType == null ||
                leadYourPetTryStartRatkinMotherLeashMethod == null ||
                leadYourPetStartLeashMethod == null ||
                gameGetComponentMethod == null)
            {
                return;
            }

            object component = gameGetComponentMethod.MakeGenericMethod(leadYourPetComponentType).Invoke(Current.Game, null);
            if (component == null)
            {
                return;
            }

            List<Pawn> pawnList = pawns
                .Where(pawn => pawn != null && !pawn.Dead && pawn.Spawned)
                .Distinct()
                .ToList();
            List<Pawn> adults = pawnList
                .Where(pawn => pawn.DevelopmentalStage == DevelopmentalStage.Adult)
                .ToList();
            List<Pawn> babies = pawnList
                .Where(pawn => pawn.DevelopmentalStage == DevelopmentalStage.Baby)
                .ToList();
            for (int babyIndex = 0; babyIndex < babies.Count; babyIndex++)
            {
                Pawn baby = babies[babyIndex];
                IEnumerable<Pawn> relatedAdults = adults
                    .Where(adult => MouseDisasterBabyLeashPolicy.ShouldTryRelatedAdultBabyLeash(
                        IsLeadYourPetEnabled,
                        adult.DevelopmentalStage == DevelopmentalStage.Adult,
                        HasIncidentAdultSocialRelation(baby, adult)))
                    .OrderBy(adult => adult.Position.DistanceToSquared(baby.Position));
                foreach (Pawn adult in relatedAdults)
                {
                    object result = StartLeadYourPetBabyLeash(component, adult, baby);
                    if (result is bool started && started)
                    {
                        break;
                    }
                }
            }
        }

        private static object StartLeadYourPetBabyLeash(object component, Pawn adult, Pawn baby)
        {
            MouseDisasterBabyLeashMode leashMode = MouseDisasterBabyLeashPolicy.ResolveRelatedAdultLeashMode(
                hasParentRelation: HasIncidentParentRelation(baby, adult));
            switch (leashMode)
            {
                case MouseDisasterBabyLeashMode.RatkinMother:
                    return leadYourPetTryStartRatkinMotherLeashMethod.Invoke(component, new object[] { adult, baby, false });
                case MouseDisasterBabyLeashMode.GenericMouseEgg:
                    return leadYourPetStartLeashMethod.Invoke(component, new object[] { adult, baby, true, false });
                default:
                    return false;
            }
        }

        private static bool HasIncidentAdultSocialRelation(Pawn baby, Pawn adult)
        {
            return baby?.relations?.DirectRelations != null &&
                   adult != null &&
                   baby.relations.DirectRelations.Any(relation => relation.otherPawn == adult && relation.otherPawn.DevelopmentalStage == DevelopmentalStage.Adult);
        }

        private static bool HasIncidentParentRelation(Pawn baby, Pawn adult)
        {
            return baby?.relations?.DirectRelations != null &&
                   adult != null &&
                   baby.relations.DirectRelations.Any(relation => relation.def == PawnRelationDefOf.Parent && relation.otherPawn == adult);
        }

        public static void TryAssignLeadYourPetTravelMouseEggs(Lord lord)
        {
            if (lord == null || !IsLeadYourPetEnabled || Current.Game == null)
            {
                return;
            }

            EnsureLeadYourPetReflection();
            if (leadYourPetComponentType == null || leadYourPetTryAssignTravelMouseEggsMethod == null || gameGetComponentMethod == null)
            {
                return;
            }

            object component = gameGetComponentMethod.MakeGenericMethod(leadYourPetComponentType).Invoke(Current.Game, null);
            if (component == null)
            {
                return;
            }

            leadYourPetTryAssignTravelMouseEggsMethod.Invoke(component, new object[] { lord });
        }

        public static void TryStartLeadYourPetAbandonedDropoff(Pawn mother, IEnumerable<Pawn> babies, IntVec3 dropoffCell)
        {
            if (!MouseDisasterAbandonedDeliveryPolicy.ShouldUseLeadYourPetDropoff(IsLeadYourPetEnabled, babies?.Count(pawn => pawn != null && !pawn.Dead) ?? 0))
            {
                return;
            }

            TryStartLeadYourPetMotherLeashes(mother, babies);
            TryAnchorLeadYourPetLeashesToCell(mother, dropoffCell);
        }

        private static void TryAnchorLeadYourPetLeashesToCell(Pawn master, IntVec3 cell)
        {
            if (master == null || !cell.IsValid || !IsLeadYourPetEnabled || Current.Game == null)
            {
                return;
            }

            EnsureLeadYourPetReflection();
            if (leadYourPetComponentType == null || leadYourPetAnchorLeashedPetsToCellMethod == null || gameGetComponentMethod == null)
            {
                return;
            }

            object component = gameGetComponentMethod.MakeGenericMethod(leadYourPetComponentType).Invoke(Current.Game, null);
            if (component == null)
            {
                return;
            }

            leadYourPetAnchorLeashedPetsToCellMethod.Invoke(component, new object[] { master, cell });
        }

        private static void TryEndLeadYourPetLeashForPet(Pawn pet)
        {
            if (pet == null || !IsLeadYourPetEnabled || Current.Game == null)
            {
                return;
            }

            EnsureLeadYourPetReflection();
            if (leadYourPetComponentType == null || leadYourPetEndLeashForPetMethod == null || gameGetComponentMethod == null)
            {
                return;
            }

            object component = gameGetComponentMethod.MakeGenericMethod(leadYourPetComponentType).Invoke(Current.Game, null);
            if (component == null)
            {
                return;
            }

            leadYourPetEndLeashForPetMethod.Invoke(component, new object[] { pet, false });
        }

        public static void TryReleaseLeadYourPetTradePawn(Pawn pawn)
        {
            TryEndLeadYourPetLeashForPet(pawn);
        }

        private static void EnsureLeadYourPetReflection()
        {
            if (leadYourPetComponentType == null)
            {
                leadYourPetComponentType = AccessTools.TypeByName("LeadYourPet.LeadYourPetGameComponent");
            }

            if (leadYourPetTryStartRatkinMotherLeashMethod == null && leadYourPetComponentType != null)
            {
                leadYourPetTryStartRatkinMotherLeashMethod = AccessTools.Method(leadYourPetComponentType, "TryStartRatkinMotherLeash");
            }

            if (leadYourPetStartLeashMethod == null && leadYourPetComponentType != null)
            {
                leadYourPetStartLeashMethod = AccessTools.Method(leadYourPetComponentType, "StartLeash", new[] { typeof(Pawn), typeof(Pawn), typeof(bool), typeof(bool) });
            }

            if (leadYourPetTryAssignTravelMouseEggsMethod == null && leadYourPetComponentType != null)
            {
                leadYourPetTryAssignTravelMouseEggsMethod = AccessTools.Method(leadYourPetComponentType, "TryAssignTravelMouseEggs");
            }

            if (leadYourPetAnchorLeashedPetsToCellMethod == null && leadYourPetComponentType != null)
            {
                leadYourPetAnchorLeashedPetsToCellMethod = AccessTools.Method(leadYourPetComponentType, "AnchorLeashedPetsToCell");
            }

            if (leadYourPetEndLeashForPetMethod == null && leadYourPetComponentType != null)
            {
                leadYourPetEndLeashForPetMethod = AccessTools.Method(leadYourPetComponentType, "EndLeashForPet", new[] { typeof(Pawn), typeof(bool) });
            }

            if (gameGetComponentMethod == null)
            {
                gameGetComponentMethod = typeof(Game).GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .FirstOrDefault(method => method.Name == "GetComponent" && method.IsGenericMethod && method.GetParameters().Length == 0);
            }
        }

        public static bool IsMouseDisasterTraderAdult(Pawn pawn)
        {
            return pawn != null && pawn.kindDef == MouseDisasterDefOf.MouseDisaster_TraderRatkinAdult;
        }

        public static bool IsMouseDisasterTraderEscort(Pawn pawn)
        {
            return pawn != null && pawn.kindDef == MouseDisasterDefOf.MouseDisaster_TraderRatkinEscort;
        }

        public static bool IsMouseDisasterIncidentParentAdult(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.DevelopmentalStage != DevelopmentalStage.Adult)
            {
                return false;
            }

            return IsMouseDisasterTraderAdult(pawn) ||
                   pawn.kindDef == MouseDisasterDefOf.MouseDisaster_BeggarRatkinAdult;
        }

        public static bool PrepareTradablePrisoner(Pawn pawn, Faction ownerFaction)
        {
            if (pawn == null || ownerFaction == null)
            {
                return false;
            }

            if (pawn.Faction != ownerFaction)
            {
                pawn.SetFaction(ownerFaction);
            }

            MarkTradableChattel(pawn);
            MarkForcedPrisonerOnPurchase(pawn);

            PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn, actAsIfSpawned: true);
            if (pawn.guest != null)
            {
                pawn.guest.joinStatus = JoinStatus.JoinAsColonist;
                pawn.guest.SetGuestStatus(ownerFaction, GuestStatus.Prisoner);
            }

            return true;
        }

        public static bool TryOrderFoodDelivery(Map map, IEnumerable<Pawn> recipients, out string message)
        {
            message = "MouseDisaster_FoodGive_Fail".Translate();
            if (map == null)
            {
                return false;
            }

            List<Pawn> validRecipients = recipients?
                .Where(pawn => pawn != null && !pawn.Dead && pawn.Spawned && pawn.Map == map)
                .Distinct()
                .ToList() ?? new List<Pawn>();
            if (validRecipients.Count == 0)
            {
                return false;
            }

            ThingDef requestedFoodDef = null;
            Pawn worker = null;
            Pawn requester = validRecipients
                .Where(pawn => !pawn.Downed)
                .OrderByDescending(pawn => pawn.DevelopmentalStage == DevelopmentalStage.Adult)
                .ThenBy(pawn => pawn.Position.DistanceToSquared(map.Center))
                .FirstOrDefault() ?? validRecipients.First();

            Thing foodThing = null;
            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned
                .Where(colonist => colonist != null && !colonist.Downed && !colonist.Drafted && colonist.jobs != null)
                .OrderBy(colonist => colonist.Position.DistanceToSquared(requester.Position))
                .ToList();
            ThingDef[] preferredFoodDefs =
            {
                ThingDefOf.MealSimple,
                ThingDefOf.MealFine,
                ThingDefOf.MealSurvivalPack
            };

            for (int colonistIndex = 0; colonistIndex < colonists.Count && foodThing == null; colonistIndex++)
            {
                Pawn colonist = colonists[colonistIndex];
                if (!colonist.CanReach(requester, PathEndMode.Touch, Danger.Some))
                {
                    continue;
                }

                for (int defIndex = 0; defIndex < preferredFoodDefs.Length; defIndex++)
                {
                    ThingDef foodDef = preferredFoodDefs[defIndex];
                    Thing candidate = GiveItemsToPawnUtility.FindItemToGive(colonist, foodDef);
                    if (candidate == null || !colonist.CanReserve(candidate))
                    {
                        continue;
                    }

                    worker = colonist;
                    requestedFoodDef = foodDef;
                    foodThing = candidate;
                    break;
                }
            }

            if (worker == null || requestedFoodDef == null || foodThing == null)
            {
                message = "MouseDisaster_FoodGive_NoFood".Translate();
                return false;
            }

            Faction lordFaction = requester.Faction;
            if (lordFaction == null)
            {
                TryFindFormerFaction(out lordFaction);
            }

            if (lordFaction == null)
            {
                message = "MouseDisaster_FoodGive_Fail".Translate();
                return false;
            }

            if (!RCellFinder.TryFindRandomSpotJustOutsideColony(requester.Position, map, requester, out IntVec3 idleSpot))
            {
                idleSpot = requester.Position;
            }

            int requestedCount = Mathf.Clamp(validRecipients.Count, 1, 12);
            LordMaker.MakeNewLord(lordFaction, new LordJob_BegForItems(lordFaction, idleSpot, requester, requestedFoodDef, requestedCount), map, validRecipients);

            Job job = JobMaker.MakeJob(JobDefOf.GiveToPawn, foodThing, requester);
            job.haulMode = HaulMode.ToContainer;
            job.lord = requester.GetLord();
            worker.jobs.TryTakeOrderedJob(job, JobTag.Misc);

            message = "MouseDisaster_FoodGive_Success".Translate(worker.Named("PAWN"), requester.Named("TARGET"), requestedFoodDef.LabelCap);
            return true;
        }

        public static TraderKindDef ResolveSlaveTraderKind()
        {
            if (slaveTraderKindResolved)
            {
                return slaveTraderKind;
            }

            slaveTraderKindResolved = true;
            for (int i = 0; i < SlaveTraderKindDefNames.Length; i++)
            {
                TraderKindDef candidate = DefDatabase<TraderKindDef>.GetNamedSilentFail(SlaveTraderKindDefNames[i]);
                if (candidate != null)
                {
                    slaveTraderKind = candidate;
                    return slaveTraderKind;
                }
            }

            slaveTraderKind = DefDatabase<TraderKindDef>.AllDefsListForReading
                .FirstOrDefault(def => def != null &&
                                        ((!def.defName.NullOrEmpty() && def.defName.IndexOf("Slaver", StringComparison.OrdinalIgnoreCase) >= 0) ||
                                         (!def.label.NullOrEmpty() && (def.label.IndexOf("slaver", StringComparison.OrdinalIgnoreCase) >= 0 || def.label.IndexOf("奴隶", StringComparison.OrdinalIgnoreCase) >= 0))));

            if (slaveTraderKind == null)
            {
                slaveTraderKind = DefDatabase<TraderKindDef>.GetNamedSilentFail("Caravan_Outlander_BulkGoods");
            }

            return slaveTraderKind;
        }

        public static void SetBiologicalAgeYears(Pawn pawn, float years)
        {
            if (pawn?.ageTracker == null)
            {
                return;
            }

            long ticks = (long)(years * GenDate.TicksPerYear);
            pawn.ageTracker.AgeBiologicalTicks = ticks;
            pawn.ageTracker.AgeChronologicalTicks = ticks;
            RefreshRatkinDevelopmentalPresentation(pawn);
        }

        public static int CalculateGroupCount(float points)
        {
            int maxCount = Mathf.Clamp(Mathf.RoundToInt(points / 120f) + 2, 2, 10);
            return Rand.RangeInclusive(2, maxCount);
        }

        public static int CalculateEscalatingGroupCount(float points, int min, int max, float pointsPerStep)
        {
            int maxCount = Mathf.Clamp(Mathf.RoundToInt(points / pointsPerStep) + min, min, max);
            return Rand.RangeInclusive(min, maxCount);
        }

        public static int CalculateEscalatingFixedCount(float points, int min, int max, float pointsPerStep)
        {
            return Mathf.Clamp(Mathf.RoundToInt(points / pointsPerStep) + min, min, max);
        }

        public static PawnKindDef RandomWildKind(bool allowChildren)
        {
            if (allowChildren && Rand.Chance(0.5f))
            {
                return MouseDisasterDefOf.MouseDisaster_WildRatkinChild;
            }

            return MouseDisasterDefOf.MouseDisaster_WildRatkinAdult;
        }

        public static IEnumerable<Pawn> SpawnWildGroup(Map map, IntVec3 center, int count, Faction formerFaction)
        {
            List<Pawn> pawns = new List<Pawn>();
            bool allowChildren = Find.Storyteller.difficulty.ChildrenAllowed;

            for (int index = 0; index < count; index++)
            {
                PawnKindDef kindDef = RandomWildKind(allowChildren);
                DevelopmentalStage stage = kindDef == MouseDisasterDefOf.MouseDisaster_WildRatkinChild ? DevelopmentalStage.Child : DevelopmentalStage.Adult;
                Pawn pawn = GenerateWildPawn(kindDef, formerFaction, stage);
                if (pawn == null)
                {
                    continue;
                }

                GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(center, map, 6), map);
                StripRatEggInventory(pawn);
                pawns.Add(pawn);
            }

            return pawns;
        }

        public static IEnumerable<Pawn> SpawnThiefGroup(Map map, IntVec3 center, int count, bool childOnly)
        {
            List<Pawn> pawns = new List<Pawn>();
            TryFindFormerFaction(out Faction formerFaction);
            if (formerFaction != null)
            {
                MakeFactionNeutralToPlayer(formerFaction, force: true);
                EnsureMouseDisasterFactionNeutralOnMap(map, formerFaction);
            }

            bool allowChildren = childOnly || Find.Storyteller.difficulty.ChildrenAllowed;

            for (int index = 0; index < count; index++)
            {
                bool useChild = childOnly || (allowChildren && Rand.Chance(0.45f));
                PawnKindDef kindDef = useChild ? MouseDisasterDefOf.MouseDisaster_ThiefRatkinChild : MouseDisasterDefOf.MouseDisaster_ThiefRatkinAdult;
                DevelopmentalStage stage = useChild ? DevelopmentalStage.Child : DevelopmentalStage.Adult;
                Pawn pawn = GenerateThiefPawn(kindDef, formerFaction, stage, allowViolenceDisabledTraits: true);
                if (pawn == null)
                {
                    continue;
                }

                GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(center, map, 6), map);
                StripRatEggInventory(pawn);
                pawn.jobs?.StartJob(CreateGotoJob(map.Center), JobCondition.InterruptForced);
                pawns.Add(pawn);
            }

            return pawns;
        }

        public static IEnumerable<Pawn> SpawnBeggarGroup(Map map, IntVec3 center, int adults, int children)
        {
            List<Pawn> pawns = new List<Pawn>();
            TryFindFormerFaction(out Faction formerFaction);
            if (formerFaction != null)
            {
                MakeFactionNeutralToPlayer(formerFaction, force: true);
                EnsureMouseDisasterFactionNeutralOnMap(map, formerFaction);
            }

            for (int index = 0; index < adults; index++)
            {
                Pawn pawn = GenerateBeggarPawn(MouseDisasterDefOf.MouseDisaster_BeggarRatkinAdult, formerFaction, DevelopmentalStage.Adult, allowViolenceDisabledTraits: true);
                if (pawn == null)
                {
                    continue;
                }

                GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(center, map, 6), map);
                StripRatEggInventory(pawn);
                pawn.jobs?.StartJob(CreateGotoJob(map.Center), JobCondition.InterruptForced);
                pawns.Add(pawn);
            }

            for (int index = 0; index < children; index++)
            {
                Pawn pawn = GenerateBeggarPawn(MouseDisasterDefOf.MouseDisaster_BeggarRatkinChild, formerFaction, DevelopmentalStage.Child, allowViolenceDisabledTraits: true);
                if (pawn == null)
                {
                    continue;
                }

                GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(center, map, 6), map);
                StripRatEggInventory(pawn);
                pawn.jobs?.StartJob(CreateGotoJob(map.Center), JobCondition.InterruptForced);
                pawns.Add(pawn);
            }

            return pawns;
        }

        public static bool HasFoodInInventory(Pawn pawn)
        {
            if (pawn?.inventory == null)
            {
                return false;
            }

            ThingOwner<Thing> container = pawn.inventory.innerContainer;
            for (int i = 0; i < container.Count; i++)
            {
                Thing thing = container[i];
                if (thing != null && thing.def.IsNutritionGivingIngestible && thing.IngestibleNow)
                {
                    return true;
                }
            }

            return false;
        }

        public static Area_MouseDisasterRelief GetReliefArea(Map map)
        {
            return map?.areaManager?.Get<Area_MouseDisasterRelief>();
        }

        public static bool MapHasReliefArea(Map map)
        {
            return map?.areaManager?.Get<Area_MouseDisasterRelief>() is Area_MouseDisasterRelief area && area.TrueCount > 0;
        }

        public static bool IsReliefAreaPostfixSuppressed => suppressReliefAreaPostfix;

        public static void EnsureReliefAreaExists(Map map)
        {
            if (map?.areaManager == null || GetReliefArea(map) != null)
            {
                return;
            }

            map.areaManager.AllAreas.Add(new Area_MouseDisasterRelief(map.areaManager));
        }

        public static bool ShouldBlockColonistReliefFood(Pawn pawn, Thing food)
        {
            return IsPlayerColonist(pawn) && IsReliefAreaFood(food);
        }

        public static bool ShouldPrioritizeReliefAreaFood(Pawn pawn)
        {
            bool isMouseDisasterFoodConsumer = pawn != null &&
                                               (IsBeggarPawn(pawn) ||
                                                IsThiefPawn(pawn) ||
                                                IsWildMouseDisasterKind(pawn) ||
                                                IsMouseDisasterTraderAdult(pawn) ||
                                                IsMouseEggOrChild(pawn));
            return MouseDisasterReliefAreaPolicy.CanUseReliefFood(isMouseDisasterFoodConsumer, IsPlayerColonist(pawn));
        }

        private static bool IsPlayerColonist(Pawn pawn)
        {
            return pawn != null && pawn.Faction == Faction.OfPlayer && pawn.IsColonist;
        }

        private static bool IsReliefAreaFood(Thing food)
        {
            if (food?.MapHeld == null || !food.Spawned)
            {
                return false;
            }

            Area_MouseDisasterRelief reliefArea = GetReliefArea(food.MapHeld);
            return reliefArea != null && reliefArea[food.PositionHeld];
        }

        private static bool IsAreaFoodSourceThing(Thing thing, bool allowHarvest)
        {
            if (thing == null || !thing.Spawned)
            {
                return false;
            }

            if (MouseDisasterReliefAreaPolicy.ShouldAllowMealSourceBuildingInReliefAreaSearch(thing.def?.building?.isMealSource == true))
            {
                return true;
            }

            if (thing is Corpse)
            {
                return true;
            }

            if (thing.def.IsNutritionGivingIngestible && thing.IngestibleNow)
            {
                return true;
            }

            return allowHarvest &&
                   thing is Plant plant &&
                   plant.HarvestableNow &&
                   plant.def?.plant?.harvestedThingDef?.IsNutritionGivingIngestible == true;
        }

        public static int CountFoodInInventory(Pawn pawn)
        {
            if (pawn?.inventory?.innerContainer == null)
            {
                return 0;
            }

            int count = 0;
            ThingOwner<Thing> container = pawn.inventory.innerContainer;
            for (int i = 0; i < container.Count; i++)
            {
                Thing thing = container[i];
                if (thing != null && thing.stackCount > 0 && thing.def.IsNutritionGivingIngestible && thing.IngestibleNow)
                {
                    count += thing.stackCount;
                }
            }

            return count;
        }

        public static void RegisterSiegeBeggar(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            int pawnId = pawn.thingIDNumber;
            SiegeBeggarPawnIds.Add(pawnId);
            SiegeBeggarStoleFoodSuccess.Remove(pawnId);
        }

        public static void RegisterStrongSiegePawn(Pawn pawn)
        {
            if (pawn != null)
            {
                StrongSiegePawnIds.Add(pawn.thingIDNumber);
            }
        }

        public static bool IsStrongSiegePawn(Pawn pawn)
        {
            return pawn != null && StrongSiegePawnIds.Contains(pawn.thingIDNumber);
        }

        public static void RegisterAirDropStayPawn(Pawn pawn, int stayTicks)
        {
            if (pawn == null)
            {
                return;
            }

            int nowTick = Find.TickManager?.TicksGame ?? 0;
            AirDropStayUntilTickByPawnId[pawn.thingIDNumber] = nowTick + Mathf.Max(1, stayTicks);
        }

        public static bool MustStayForAirDropError(Pawn pawn)
        {
            if (pawn == null || !AirDropStayUntilTickByPawnId.TryGetValue(pawn.thingIDNumber, out int untilTick))
            {
                return false;
            }

            int nowTick = Find.TickManager?.TicksGame ?? 0;
            if (nowTick < untilTick)
            {
                return true;
            }

            AirDropStayUntilTickByPawnId.Remove(pawn.thingIDNumber);
            return false;
        }

        public static void MarkSiegeBeggarStoleFood(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            SiegeBeggarStoleFoodSuccess.Add(pawn.thingIDNumber);
        }

        public static bool IsSiegeBeggar(Pawn pawn)
        {
            return pawn != null && SiegeBeggarPawnIds.Contains(pawn.thingIDNumber);
        }

        public static bool HasSiegeBeggarLeaveCondition(Pawn pawn)
        {
            if (pawn == null)
            {
                return true;
            }

            if (pawn.Dead || pawn.MapHeld == null)
            {
                ResetBeggarState(pawn);
                return true;
            }

            int pawnId = pawn.thingIDNumber;
            if (SiegeBeggarStoleFoodSuccess.Contains(pawnId))
            {
                return true;
            }

            return CountFoodInInventory(pawn) >= SiegeBeggarLeaveFoodUnits;
        }

        private static void RemoveSiegeBeggarState(int pawnId)
        {
            SiegeBeggarPawnIds.Remove(pawnId);
            SiegeBeggarStoleFoodSuccess.Remove(pawnId);
            StrongSiegePawnIds.Remove(pawnId);
        }

        public static Job ExitMapJob(Pawn pawn)
        {
            if (pawn?.Map == null)
            {
                return null;
            }

            if (pawn.DevelopmentalStage == DevelopmentalStage.Baby)
            {
                return CreateGotoJob(pawn.Map.Center);
            }

            if (!RCellFinder.TryFindBestExitSpot(pawn, out IntVec3 exitSpot))
            {
                if (!TryFindFarEdgeCell(pawn.Map, pawn.Position, out exitSpot))
                {
                    return CreateGotoJob(pawn.Map.Center);
                }
            }

            Job job = JobMaker.MakeJob(JobDefOf.Goto, exitSpot);
            job.exitMapOnArrival = true;
            job.locomotionUrgency = LocomotionUrgency.Jog;
            return job;
        }

        public static Job TryCreatePathRecoveryJob(Pawn pawn)
        {
            if (pawn?.Map == null || pawn.Downed)
            {
                return null;
            }

            Thing tree = FindGnawableTree(pawn);
            if (tree != null)
            {
                return JobMaker.MakeJob(MouseDisasterDefOf.MouseDisaster_GnawTreeBark, tree);
            }

            Thing wall = FindGnawableWall(pawn);
            if (wall != null)
            {
                return JobMaker.MakeJob(MouseDisasterDefOf.MouseDisaster_GnawWall, wall);
            }

            return null;
        }

        public static bool HasAnyStealableFoodOnMap(Map map)
        {
            return map?.listerThings?.ThingsInGroup(ThingRequestGroup.FoodSource)
                ?.Any(thing => IsAreaFoodSourceThing(thing, allowHarvest: true)) == true;
        }

        public static Job TryCreateReliefFoodJob(Pawn pawn, bool allowInventorySearch)
        {
            if (pawn?.needs?.food == null || !ShouldPrioritizeReliefAreaFood(pawn))
            {
                return null;
            }

            if (!TryFindBestReliefFoodSourceFor(
                    pawn,
                    pawn,
                    pawn.needs.food.CurCategory == HungerCategory.Starving,
                    allowHarvest: true,
                    out Thing foodSource,
                    out ThingDef foodDef,
                    allowForbidden: false,
                    allowCorpse: true,
                    allowSociallyImproper: true,
                    ignoreReservations: false,
                    calculateWantedStackCount: false,
                    allowVenerated: false,
                    FoodPreferability.Undefined))
            {
                return null;
            }

            if (foodSource is Plant && foodSource.def.plant?.harvestedThingDef?.IsNutritionGivingIngestible == true)
            {
                return JobMaker.MakeJob(JobDefOf.Harvest, foodSource);
            }

            if (MouseDisasterReliefAreaPolicy.ShouldUseMealSourceInteractionFlow(foodSource.def?.building?.isMealSource == true))
            {
                IntVec3 interactionCell = foodSource.InteractionCell;
                if (!interactionCell.IsValid || !interactionCell.Standable(pawn.Map) || !pawn.CanReserveAndReach(foodSource, PathEndMode.InteractionCell, Danger.Some))
                {
                    return null;
                }

                Job mealSourceJob = JobMaker.MakeJob(JobDefOf.Ingest, foodSource);
                mealSourceJob.count = 1;
                return mealSourceJob;
            }

            if (!Toils_Ingest.TryFindChairOrSpot(pawn, foodSource, out _))
            {
                return null;
            }

            float nutrition = FoodUtility.GetNutrition(pawn, foodSource, foodDef);
            if (allowInventorySearch && foodSource.Spawned && pawn.CanReserveAndReach(foodSource, PathEndMode.ClosestTouch, Danger.Some))
            {
                Job takeInventory = JobMaker.MakeJob(JobDefOf.TakeInventory, foodSource);
                takeInventory.count = 1;
                takeInventory.takeInventoryDelay = 60;
                return takeInventory;
            }

            Job ingest = JobMaker.MakeJob(JobDefOf.Ingest, foodSource);
            ingest.count = Mathf.Max(1, FoodUtility.WillIngestStackCountOf(pawn, foodDef, nutrition));
            return ingest;
        }

        public static bool TryStartVisitorAssault(Pawn triggerPawn)
        {
            if (triggerPawn?.Map == null || triggerPawn.Downed || triggerPawn.Dead || !triggerPawn.Spawned)
            {
                return false;
            }

            Map map = triggerPawn.Map;
            Faction faction = triggerPawn.Faction;
            if (faction == null && !TryFindFormerFaction(out faction))
            {
                return false;
            }

            if (faction == null)
            {
                return false;
            }

            List<Pawn> attackers = map.mapPawns.AllPawnsSpawned
                .Where(pawn =>
                    pawn != null &&
                    !pawn.Dead &&
                    !pawn.Downed &&
                    pawn.Spawned &&
                    pawn.Faction == faction &&
                    (IsStrongSiegePawn(pawn) || IsThiefPawn(pawn) || IsBeggarPawn(pawn)))
                .ToList();
            if (attackers.Count == 0)
            {
                return false;
            }

            MakeFactionHostileToPlayer(faction);
            for (int i = 0; i < attackers.Count; i++)
            {
                Pawn pawn = attackers[i];
                pawn.mindState?.mentalStateHandler?.Reset();
                pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced, startNewJob: false);
            }

            LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction, canKidnap: false, canTimeoutOrFlee: false, canSteal: true, breachers: true), map, attackers);
            return true;
        }

        public static void PrepareNonCaravanBabyPawn(Pawn pawn, IntVec3 anchorCell)
        {
            if (pawn == null || pawn.DevelopmentalStage != DevelopmentalStage.Baby)
            {
                return;
            }

            if (pawn.mindState != null)
            {
                pawn.mindState.canFleeIndividual = false;
                pawn.mindState.exitMapAfterTick = Find.TickManager.TicksGame + GenDate.TicksPerDay * 20;
            }

            if (pawn.Spawned && anchorCell.IsValid)
            {
                TryTakeAutoOrderedJob(pawn, CreateGotoJob(anchorCell), JobTag.Misc, requireStarving: false);
            }
        }

        public static void ResetBeggarState(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            int pawnId = pawn.thingIDNumber;
            BegAttempts.Remove(pawnId);
            BeggedColonists.Remove(pawnId);
            BegSuccess.Remove(pawnId);
            RemoveSiegeBeggarState(pawnId);
            AirDropStayUntilTickByPawnId.Remove(pawnId);
        }

        public static void ClearBeggarTargetHistory(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            int pawnId = pawn.thingIDNumber;
            BegAttempts.Remove(pawnId);
            BeggedColonists.Remove(pawnId);
        }

        public static int GetBegAttempts(Pawn pawn)
        {
            return pawn != null && BegAttempts.TryGetValue(pawn.thingIDNumber, out int count) ? count : 0;
        }

        public static bool HasBeggedColonist(Pawn beggar, Pawn colonist)
        {
            return beggar != null &&
                   colonist != null &&
                   BeggedColonists.TryGetValue(beggar.thingIDNumber, out HashSet<int> targets) &&
                   targets.Contains(colonist.thingIDNumber);
        }

        public static void RecordBegAttempt(Pawn pawn, Pawn targetColonist, bool success)
        {
            if (pawn == null)
            {
                return;
            }

            BegAttempts[pawn.thingIDNumber] = GetBegAttempts(pawn) + 1;
            if (targetColonist != null)
            {
                if (!BeggedColonists.TryGetValue(pawn.thingIDNumber, out HashSet<int> targets))
                {
                    targets = new HashSet<int>();
                    BeggedColonists[pawn.thingIDNumber] = targets;
                }

                targets.Add(targetColonist.thingIDNumber);
            }

            if (success)
            {
                int pawnId = pawn.thingIDNumber;
                if (SiegeBeggarPawnIds.Contains(pawnId))
                {
                    return;
                }

                BegSuccess.Add(pawnId);
            }
        }

        public static bool TryConsumeBeggedFood(Pawn beggar, Pawn targetColonist)
        {
            if (beggar == null || targetColonist?.inventory?.innerContainer == null)
            {
                return false;
            }

            Thing food = targetColonist.inventory.innerContainer
                .Where(thing => thing.def.IsNutritionGivingIngestible && thing.IngestibleNow && thing.stackCount > 0)
                .OrderByDescending(thing => thing.GetStatValue(StatDefOf.Nutrition))
                .FirstOrDefault();
            if (food == null)
            {
                return false;
            }

            Thing beggedFood = food.SplitOff(1);
            if (beggedFood == null)
            {
                return false;
            }

            if (beggar.inventory?.innerContainer != null && beggar.inventory.innerContainer.TryAddOrTransfer(beggedFood))
            {
                return true;
            }

            if (beggar.MapHeld != null && beggar.PositionHeld.IsValid)
            {
                GenPlace.TryPlaceThing(beggedFood, beggar.PositionHeld, beggar.MapHeld, ThingPlaceMode.Near);
                return true;
            }

            beggedFood.Destroy();
            return false;
        }

        public static void RecordBeggarInteractionLog(Pawn beggar, Pawn targetColonist, bool success)
        {
            if (beggar == null || targetColonist == null)
            {
                return;
            }

            InteractionDef interaction = success ? InteractionDefOf.Chitchat : InteractionDefOf.Insult;
            Find.PlayLog?.Add(new PlayLogEntry_Interaction(interaction, beggar, targetColonist, null));
        }

        public static void ApplyBeggarWitnessThought(Pawn targetColonist)
        {
            if (targetColonist?.needs?.mood?.thoughts?.memories == null || targetColonist.story?.traits == null)
            {
                return;
            }

            if (targetColonist.story.traits.HasTrait(TraitDefOf.Psychopath))
            {
                targetColonist.needs.mood.thoughts.memories.TryGainMemory(MouseDisasterDefOf.MouseDisaster_SawBeggarRatkin_Psychopath);
                return;
            }

            if (targetColonist.story.traits.HasTrait(TraitDefOf.Kind))
            {
                targetColonist.needs.mood.thoughts.memories.TryGainMemory(MouseDisasterDefOf.MouseDisaster_SawBeggarRatkin_Kind);
            }
        }

        public static void TryGainBloodlinePregnancyThought(Pawn pawn)
        {
            TryGainNonStackingMemory(pawn, MouseDisasterDefOf.MouseDisaster_BloodlineContinuation);
        }

        public static void TryGainBloodlineBirthThought(Pawn pawn)
        {
            TryGainNonStackingMemory(pawn, MouseDisasterDefOf.MouseDisaster_BloodlineRenewed);
        }

        public static void TryNormalizeColonyBornRatkinBabyBackstory(Pawn newborn, Pawn mother)
        {
            if (newborn?.story == null || !IsRatkin(newborn) || !IsColonyBirthMother(mother))
            {
                return;
            }

            if (ShouldUseMouseDisasterBirthIdentity(newborn, mother))
            {
                newborn.story.Childhood = MouseDisasterDefOf.MouseDisaster_Newborn;
                newborn.story.Adulthood = null;
                return;
            }

            BackstoryDef vanillaBabyBackstory = ResolveVanillaNewbornBackstory();
            if (vanillaBabyBackstory != null)
            {
                newborn.story.Childhood = vanillaBabyBackstory;
            }

            newborn.story.Adulthood = null;
        }

        public static void TryRemoveMouseDisasterFatherRelationAfterBirth(Pawn newborn, Pawn mother)
        {
            if (newborn?.relations == null || mother == null || !IsRatkin(newborn) || !IsRatkin(mother) || !HasAnyMouseDisasterGene(mother))
            {
                return;
            }

            List<Pawn> fathersToRemove = newborn.relations.DirectRelations
                .Where(relation => relation != null &&
                                   relation.def == PawnRelationDefOf.Parent &&
                                   relation.otherPawn != null &&
                                   relation.otherPawn != mother &&
                                   relation.otherPawn.gender == Gender.Male)
                .Select(relation => relation.otherPawn)
                .Distinct()
                .ToList();

            for (int i = 0; i < fathersToRemove.Count; i++)
            {
                newborn.relations.TryRemoveDirectRelation(PawnRelationDefOf.Parent, fathersToRemove[i]);
            }
        }

        private static void TryGainNonStackingMemory(Pawn pawn, ThoughtDef thoughtDef)
        {
            if (pawn?.needs?.mood?.thoughts?.memories == null || thoughtDef == null || !IsRatkin(pawn))
            {
                return;
            }

            if (thoughtDef == MouseDisasterDefOf.MouseDisaster_BloodlineContinuation || thoughtDef == MouseDisasterDefOf.MouseDisaster_BloodlineRenewed)
            {
                pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(MouseDisasterDefOf.MouseDisaster_BloodlineContinuation);
                pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(MouseDisasterDefOf.MouseDisaster_BloodlineRenewed);
            }

            pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(thoughtDef);
            pawn.needs.mood.thoughts.memories.TryGainMemory(thoughtDef);
        }

        private static bool IsColonyBirthMother(Pawn mother)
        {
            return mother != null &&
                   (mother.Faction == Faction.OfPlayer || mother.IsPrisonerOfColony || mother.IsSlaveOfColony);
        }

        private static bool ShouldUseMouseDisasterBirthIdentity(Pawn newborn, Pawn mother)
        {
            return newborn != null &&
                   mother != null &&
                   IsRatkin(mother) &&
                   (IsMouseDisasterPawn(mother) ||
                    HasAnyMouseDisasterGene(mother) ||
                    IsMouseDisasterPawn(newborn) ||
                    HasAnyMouseDisasterGene(newborn));
        }

        private static BackstoryDef ResolveVanillaNewbornBackstory()
        {
            if (vanillaNewbornBackstoryPoolResolved)
            {
                return vanillaNewbornBackstoryPool.NullOrEmpty() ? null : vanillaNewbornBackstoryPool.RandomElement();
            }

            vanillaNewbornBackstoryPoolResolved = true;
            vanillaNewbornBackstoryPool = DefDatabase<BackstoryDef>.AllDefsListForReading
                .Where(def => def != null &&
                              def.slot == BackstorySlot.Childhood &&
                              def.spawnCategories != null &&
                              def.spawnCategories.Contains(VanillaNewbornSpawnCategory) &&
                              def != MouseDisasterDefOf.MouseDisaster_Newborn)
                .ToList();

            return vanillaNewbornBackstoryPool.NullOrEmpty() ? null : vanillaNewbornBackstoryPool.RandomElement();
        }

        public static void MarkForcedPrisonerOnPurchase(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            ForcePrisonerOnPurchasePawnIds.Add(pawn.thingIDNumber);
        }

        public static void MarkTradableChattel(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            TradableChattelPawnIds.Add(pawn.thingIDNumber);
        }

        public static bool IsMarkedTradableChattel(Pawn pawn)
        {
            return pawn != null && TradableChattelPawnIds.Contains(pawn.thingIDNumber);
        }

        public static void UnmarkTradableChattel(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            TradableChattelPawnIds.Remove(pawn.thingIDNumber);
        }

        public static bool ConsumeForcedPrisonerOnPurchase(Pawn pawn)
        {
            return pawn != null && ForcePrisonerOnPurchasePawnIds.Remove(pawn.thingIDNumber);
        }

        public static bool IsForcedPrisonerOnPurchase(Pawn pawn)
        {
            return pawn != null && ForcePrisonerOnPurchasePawnIds.Contains(pawn.thingIDNumber);
        }

        public static bool IsMouseDisasterTradePawn(Pawn pawn)
        {
            return pawn != null && (IsMarkedTradableChattel(pawn) || IsForcedPrisonerOnPurchase(pawn));
        }

        public static void ApplyPurchasedTradePawnPrisonerState(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn, actAsIfSpawned: true);
            if (pawn.guest == null)
            {
                return;
            }

            TryReleaseLeadYourPetTradePawn(pawn);
            RemoveAllIncidentVisitorHediffs(pawn);
            ClearTradeLeaderState(pawn);
            ResetBeggarState(pawn);
            RemoveChildExchangeTrackingForPurchasedPawn(pawn);
            ChildExchangeMoodPawnIds.Remove(pawn.thingIDNumber);
            pawn.jobs?.StopAll();
            pawn.GetLord()?.RemovePawn(pawn);
            pawn.guest.joinStatus = JoinStatus.JoinAsColonist;

            if (pawn.Faction == Faction.OfPlayer)
            {
                pawn.SetFaction(null);
            }

            pawn.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
            NotifyMouseDisasterPawnIdentityOrLifeStageChanged(pawn);
        }

        public static void MarkChildExchangeMoodChild(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            ChildExchangeMoodPawnIds.Add(pawn.thingIDNumber);
            MarkMapPawnCacheDirty(pawn);
        }

        public static void MarkChildExchangeMoodChildren(IEnumerable<Pawn> pawns)
        {
            if (pawns == null)
            {
                return;
            }

            foreach (Pawn pawn in pawns)
            {
                MarkChildExchangeMoodChild(pawn);
            }
        }

        public static bool HasChildExchangeMoodMarker(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (pawn.Dead || !IsMouseEggOrChild(pawn))
            {
                ChildExchangeMoodPawnIds.Remove(pawn.thingIDNumber);
                return false;
            }

            return ChildExchangeMoodPawnIds.Contains(pawn.thingIDNumber);
        }

        public static void RegisterChildExchange(Pawn trader, IEnumerable<Pawn> children, int durationTicks, IEnumerable<Pawn> escorts = null)
        {
            if (trader?.Map == null)
            {
                return;
            }

            List<int> childIds = children?
                .Where(child => child != null && !child.Dead)
                .Select(child => child.thingIDNumber)
                .Distinct()
                .ToList() ?? new List<int>();
            if (childIds.Count == 0)
            {
                return;
            }

            List<int> escortIds = escorts?
                .Where(escort => escort != null && !escort.Dead)
                .Select(escort => escort.thingIDNumber)
                .Distinct()
                .ToList() ?? new List<int>();

            ActiveChildExchangeByTraderId[trader.thingIDNumber] = new ChildExchangeState
            {
                mapId = trader.Map.uniqueID,
                expireTick = Find.TickManager.TicksGame + Mathf.Max(600, durationTicks),
                escortPawnIds = escortIds,
                childPawnIds = childIds
            };
        }

        public static void RemoveChildExchangeTrackingForPurchasedPawn(Pawn pawn)
        {
            if (pawn == null || ActiveChildExchangeByTraderId.Count == 0)
            {
                return;
            }

            ReusableIntList.Clear();
            foreach (KeyValuePair<int, ChildExchangeState> pair in ActiveChildExchangeByTraderId)
            {
                ChildExchangeState state = pair.Value;
                if (state?.childPawnIds == null || !state.childPawnIds.Remove(pawn.thingIDNumber))
                {
                    continue;
                }

                if (state.childPawnIds.Count == 0)
                {
                    ReusableIntList.Add(pair.Key);
                }
            }

            for (int i = 0; i < ReusableIntList.Count; i++)
            {
                ActiveChildExchangeByTraderId.Remove(ReusableIntList[i]);
            }
        }

        public static bool IsChildExchangeTrader(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (!ActiveChildExchangeByTraderId.TryGetValue(pawn.thingIDNumber, out ChildExchangeState state))
            {
                return false;
            }

            if (pawn.Map == null || pawn.Map.uniqueID != state.mapId)
            {
                ActiveChildExchangeByTraderId.Remove(pawn.thingIDNumber);
                return false;
            }

            return true;
        }

        public static bool TryExecuteChildExchange(Pawn trader, int mode, out string message)
        {
            message = "\u4ea4\u6613\u5931\u8d25\u3002";
            if (trader?.Map == null || !IsChildExchangeTrader(trader))
            {
                return false;
            }

            Pawn offeredBaby = FindExchangeOfferBaby(trader.Map, mode);
            if (offeredBaby == null)
            {
                message = "\u8be5\u7c7b\u578b\u65e0\u53ef\u7528\u5a74\u513f\u3002";
                return false;
            }

            return TryResolveChildExchange(trader, offeredBaby, out message);
        }

        public static Pawn FindExchangeOfferBaby(Map map, int mode)
        {
            if (map == null)
            {
                return null;
            }

            IEnumerable<Pawn> candidates = map.mapPawns.AllPawnsSpawned
                .Where(pawn => IsMouseEggBaby(pawn) && !pawn.Dead)
                .OrderBy(pawn => pawn.ageTracker.AgeBiologicalTicks);

            switch (mode)
            {
                case ChildExchangeModeColonist:
                    return candidates.FirstOrDefault(pawn => pawn.Faction == Faction.OfPlayer && !pawn.IsPrisonerOfColony && !pawn.IsSlaveOfColony);
                case ChildExchangeModeSlave:
                    return candidates.FirstOrDefault(pawn => pawn.IsSlaveOfColony);
                case ChildExchangeModePrisoner:
                    return candidates.FirstOrDefault(pawn => pawn.IsPrisonerOfColony);
                default:
                    return null;
            }
        }

        private static bool TryResolveChildExchange(Pawn trader, Pawn offeredBaby, out string message)
        {
            message = "\u4ea4\u6613\u5931\u8d25\u3002";
            if (trader?.Map == null || offeredBaby == null || !IsChildExchangeTrader(trader))
            {
                return false;
            }

            Dictionary<int, Pawn> pawnLookup = BuildSpawnedPawnLookup(trader.Map.mapPawns.AllPawnsSpawned);
            List<Pawn> exchangeChildren = ResolveChildExchangeChildren(trader, pawnLookup).Where(child => child.Spawned && !child.Dead).ToList();
            List<Pawn> exchangeEscorts = ResolveChildExchangeEscorts(trader, pawnLookup).Where(escort => escort.Spawned && !escort.Dead).ToList();
            if (exchangeChildren.Count == 0)
            {
                ActiveChildExchangeByTraderId.Remove(trader.thingIDNumber);
                message = "\u4ea4\u6613\u5931\u8d25\uff1a\u53ef\u4ea4\u6362\u9f20\u86cb\u5df2\u4e0d\u5b58\u5728\u3002";
                return false;
            }

            if (!TransferOfferedBabyToTrader(offeredBaby, trader))
            {
                message = "\u4ea4\u6613\u5931\u8d25\uff1a\u5a74\u513f\u79fb\u4ea4\u5931\u8d25\u3002";
                return false;
            }

            for (int i = 0; i < exchangeChildren.Count; i++)
            {
                Pawn child = exchangeChildren[i];
                if (child.Faction == Faction.OfPlayer)
                {
                    child.SetFaction(null);
                }

                child.guest?.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
            }

            ActiveChildExchangeByTraderId.Remove(trader.thingIDNumber);
            List<Pawn> leaving = new List<Pawn> { trader };
            leaving.AddRange(exchangeEscorts);
            if (offeredBaby.Spawned && !offeredBaby.Dead)
            {
                leaving.Add(offeredBaby);
            }

            MakeTravelAndExitLord(trader.Map, leaving, trader.Map.Center);
            EnsureMouseDisasterFactionNeutralOnMap(trader.Map, trader.Faction);
            message = "\u6613\u5b50\u800c\u98df\u6210\u4ea4\uff1a\u5bf9\u65b9\u5e26\u8d70\u4e86\u5a74\u513f\uff0c\u7559\u4e0b\u4e86\u5168\u90e8\u9f20\u86cb\u3002";
            return true;
        }

        private static bool TransferOfferedBabyToTrader(Pawn offeredBaby, Pawn trader)
        {
            if (offeredBaby == null || trader?.Faction == null || offeredBaby.Dead)
            {
                return false;
            }

            if (offeredBaby.Faction != trader.Faction)
            {
                offeredBaby.SetFaction(trader.Faction);
            }

            offeredBaby.guest?.SetGuestStatus(null, GuestStatus.Guest);
            offeredBaby.jobs?.StopAll();
            StripRatEggInventory(offeredBaby);
            if (offeredBaby.Spawned)
            {
                offeredBaby.DeSpawnOrDeselect();
            }

            if (!Find.WorldPawns.Contains(offeredBaby))
            {
                Find.WorldPawns.PassToWorld(offeredBaby);
            }

            return true;
        }

        public static void ProcessChildExchangeTimeouts(Map map)
        {
            if (map == null || ActiveChildExchangeByTraderId.Count == 0)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            int mapId = map.uniqueID;
            ReusableIntList.Clear();
            foreach (KeyValuePair<int, ChildExchangeState> pair in ActiveChildExchangeByTraderId)
            {
                if (pair.Value != null && pair.Value.mapId == mapId && now >= pair.Value.expireTick)
                {
                    ReusableIntList.Add(pair.Key);
                }
            }

            if (ReusableIntList.Count == 0)
            {
                return;
            }

            IReadOnlyList<Pawn> mapPawns = map.mapPawns.AllPawnsSpawned;
            Dictionary<int, Pawn> pawnLookup = BuildSpawnedPawnLookup(mapPawns);
            for (int i = 0; i < ReusableIntList.Count; i++)
            {
                int traderId = ReusableIntList[i];
                Pawn trader = ResolvePawnById(pawnLookup, traderId);
                if (trader == null)
                {
                    ActiveChildExchangeByTraderId.Remove(traderId);
                    continue;
                }

                ReusablePawnList.Clear();
                ReusablePawnList.Add(trader);
                ResolveChildExchangeEscortsInto(trader, pawnLookup, ReusablePawnList);
                ResolveChildExchangeChildrenInto(trader, pawnLookup, ReusablePawnList);
                ActiveChildExchangeByTraderId.Remove(traderId);
                MakeTravelAndExitLord(map, ReusablePawnList, map.Center);
                Messages.Message("\u6613\u5b50\u800c\u98df\u5546\u4eba\u7b49\u5f85\u8d85\u65f6\uff0c\u5df2\u5e26\u7740\u9f20\u86cb\u79bb\u5f00\u3002", trader, MessageTypeDefOf.NeutralEvent, historical: false);
            }
        }

        private static List<Pawn> ResolveChildExchangeEscorts(Pawn trader, Dictionary<int, Pawn> pawnLookup)
        {
            List<Pawn> result = new List<Pawn>();
            ResolveChildExchangeEscortsInto(trader, pawnLookup, result);
            return result;
        }

        private static void ResolveChildExchangeEscortsInto(Pawn trader, Dictionary<int, Pawn> pawnLookup, List<Pawn> result)
        {
            if (trader?.Map == null || !ActiveChildExchangeByTraderId.TryGetValue(trader.thingIDNumber, out ChildExchangeState state) || state?.escortPawnIds == null)
            {
                return;
            }

            for (int i = 0; i < state.escortPawnIds.Count; i++)
            {
                Pawn pawn = ResolvePawnById(pawnLookup, state.escortPawnIds[i]);
                if (pawn != null)
                {
                    result.Add(pawn);
                }
            }
        }

        private static List<Pawn> ResolveChildExchangeChildren(Pawn trader, Dictionary<int, Pawn> pawnLookup)
        {
            List<Pawn> result = new List<Pawn>();
            ResolveChildExchangeChildrenInto(trader, pawnLookup, result);
            return result;
        }

        private static void ResolveChildExchangeChildrenInto(Pawn trader, Dictionary<int, Pawn> pawnLookup, List<Pawn> result)
        {
            if (trader?.Map == null || !ActiveChildExchangeByTraderId.TryGetValue(trader.thingIDNumber, out ChildExchangeState state) || state?.childPawnIds == null)
            {
                return;
            }

            for (int i = 0; i < state.childPawnIds.Count; i++)
            {
                Pawn pawn = ResolvePawnById(pawnLookup, state.childPawnIds[i]);
                if (pawn != null)
                {
                    result.Add(pawn);
                }
            }
        }

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
            }

            if (childIds.Count == 0)
            {
                return;
            }

            ActiveAbandonedDeliveryByAdultId[adult.thingIDNumber] = new AbandonedDeliveryState
            {
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
                    if (child != null && !child.Dead && (child.Spawned || child.ParentHolder is Pawn_CarryTracker))
                    {
                        ReusablePawnList.Add(child);
                    }
                }

                if (ReusablePawnList.Count == 0)
                {
                    ActiveAbandonedDeliveryByAdultId.Remove(adultId);
                    continue;
                }

                bool allChildrenArrived = AreAllPawnsNearCell(ReusablePawnList, state.foodCell, 3f);
                for (int childIndex = 0; childIndex < ReusablePawnList.Count; childIndex++)
                {
                    Pawn child = ReusablePawnList[childIndex];
                    if (!child.Spawned)
                    {
                        continue;
                    }

                    if (child.Position.InHorDistOf(state.foodCell, 3f))
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
                            TryStartAbandonedDeliveryCarryJob(adult, ReusablePawnList, state.foodCell))
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
                                Messages.Message("\u8001\u9f20\u5988\u5728\u6295\u653e\u70b9\u9644\u8fd1\u53cd\u590d\u5361\u4f4f\u8fc7\u4e45\uff0c\u653e\u5f03\u7b49\u5f85\u5e76\u79bb\u5f00\u4e86\u5730\u56fe\u3002", adult, MessageTypeDefOf.NeutralEvent, historical: false);
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
                            Messages.Message("\u8001\u9f20\u5988\u786e\u8ba4\u9f20\u86cb\u5230\u4f4d\u540e\uff0c\u8f6c\u8eab\u9003\u79bb\u4e86\u5730\u56fe\u3002", adult, MessageTypeDefOf.NeutralEvent, historical: false);
                        }
                    }
                }

                if (!allChildrenArrived)
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
                Messages.Message("\u8001\u9f20\u5988\u79bb\u5f00\u540e\uff0c\u8fd9\u4e9b\u9f20\u86cb\u7559\u5728\u7cae\u4ed3\u9644\u8fd1\uff0c\u53d8\u6210\u4e86\u6e38\u8361\u7684\u91ce\u751f\u9f20\u65cf\u3002", ReusablePawnList, MessageTypeDefOf.NeutralEvent, historical: false);
            }
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
            if (pawn == null || pawn.Dead)
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
            ResetBeggarState(pawn);
            SetMouseDisasterFoodLevel(pawn);
            if (pawn.Spawned)
            {
                TryTakeAutoOrderedJob(pawn, CreateGotoJob(fallbackCell), JobTag.Misc, requireStarving: false);
            }
        }

        public static void ProcessChaosPregnancies(Map map)
        {
            MouseDisasterSettings settings = MouseDisasterMod.Settings;
            if (!ModsConfig.BiotechActive || map == null || !Find.Storyteller.difficulty.ChildrenAllowed || (settings != null && !settings.enableChaosRoomPregnancy))
            {
                return;
            }

            int checkIntervalTicks = Mathf.Max(60, settings?.chaosPregnancyCheckIntervalTicks ?? DefaultChaosPregnancyCheckInterval);
            float chancePerCheck = Mathf.Clamp01((settings?.chaosPregnancyChancePercent ?? (DefaultChaosPregnancyChancePerCheck * 100f)) / 100f);
            if (chancePerCheck <= 0f)
            {
                return;
            }

            List<Pawn> ratkinPawns = GetCachedRatkinPawns(map);
            ReusablePawnList.Clear();
            for (int i = 0; i < ratkinPawns.Count; i++)
            {
                Pawn pawn = ratkinPawns[i];
                if (pawn == null ||
                    !pawn.Spawned ||
                    pawn.gender != Gender.Female ||
                    !pawn.IsHashIntervalTick(checkIntervalTicks) ||
                    !CanParticipateInMouseDisasterPregnancy(pawn) ||
                    !HasActiveGene(pawn, MouseDisasterDefOf.MouseDisaster_Gene_ChaosFertility) ||
                    PregnancyUtility.GetPregnancyHediff(pawn) != null ||
                    !TryGetChaosPregnancyIdentity(pawn, out _))
                {
                    continue;
                }

                ReusablePawnList.Add(pawn);
            }
            if (ReusablePawnList.Count == 0)
            {
                return;
            }

            Dictionary<Room, Dictionary<ChaosPregnancyIdentity, List<Pawn>>> malesByRoom = new Dictionary<Room, Dictionary<ChaosPregnancyIdentity, List<Pawn>>>();
            for (int i = 0; i < ratkinPawns.Count; i++)
            {
                Pawn male = ratkinPawns[i];
                if (male == null || !male.Spawned || male.gender != Gender.Male || !CanParticipateInMouseDisasterPregnancy(male) || !TryGetChaosPregnancyIdentity(male, out ChaosPregnancyIdentity maleIdentity))
                {
                    continue;
                }

                Room maleRoom = male.GetRoom();
                if (maleRoom == null || maleRoom.PsychologicallyOutdoors)
                {
                    continue;
                }

                if (!malesByRoom.TryGetValue(maleRoom, out Dictionary<ChaosPregnancyIdentity, List<Pawn>> byIdentity))
                {
                    byIdentity = new Dictionary<ChaosPregnancyIdentity, List<Pawn>>();
                    malesByRoom[maleRoom] = byIdentity;
                }

                if (!byIdentity.TryGetValue(maleIdentity, out List<Pawn> malesInRoom))
                {
                    malesInRoom = new List<Pawn>();
                    byIdentity[maleIdentity] = malesInRoom;
                }

                malesInRoom.Add(male);
            }

            for (int i = 0; i < ReusablePawnList.Count; i++)
            {
                Pawn female = ReusablePawnList[i];
                if (!TryGetChaosPregnancyIdentity(female, out ChaosPregnancyIdentity femaleIdentity))
                {
                    continue;
                }

                Room room = female.GetRoom();
                if (room == null || room.PsychologicallyOutdoors)
                {
                    continue;
                }

                if (!malesByRoom.TryGetValue(room, out Dictionary<ChaosPregnancyIdentity, List<Pawn>> malesByIdentity) ||
                    !malesByIdentity.TryGetValue(femaleIdentity, out List<Pawn> malesInRoom) ||
                    malesInRoom.Count == 0)
                {
                    continue;
                }

                Pawn father = malesInRoom.RandomElement();
                float effectiveChance = chancePerCheck;
                if (!Rand.Chance(effectiveChance))
                {
                    continue;
                }

                bool inheritedSuccess;
                GeneSet inheritedGeneSet = PregnancyUtility.GetInheritedGeneSet(father, female, out inheritedSuccess);
                if (!inheritedSuccess)
                {
                    continue;
                }

                Hediff_Pregnant pregnancy = (Hediff_Pregnant)HediffMaker.MakeHediff(HediffDefOf.PregnantHuman, female);
                pregnancy.SetParents(female, father, inheritedGeneSet);
                female.health.AddHediff(pregnancy);
            }
        }

        public static void ProcessMouseDisasterIdentityRestrictions(Map map, bool explicitRefreshRequested = false)
        {
            if (map == null || !MouseDisasterGeneRestorePolicy.ShouldRunIdentityRestrictionScan(explicitRefreshRequested))
            {
                return;
            }

            List<Pawn> pawns = GetCachedRatkinPawns(map);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.Dead)
                {
                    continue;
                }

                NormalizeMouseDisasterPawnIdentityOrLifeStageChanged(pawn, markMapCacheDirty: false);
            }
        }

        public static void ProcessOpenDoorStuckJobs(Map map)
        {
            if (map == null)
            {
                return;
            }

            List<Pawn> pawns = GetCachedDoorStuckCandidates(map);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (!IsDoorStuckCandidate(pawn))
                {
                    continue;
                }

                Job curJob = pawn.jobs.curJob;
                if (curJob?.def != JobDefOf.Open || !curJob.targetA.IsValid)
                {
                    continue;
                }

                Building_Door targetDoor = curJob.targetA.Thing as Building_Door;
                if (targetDoor == null || !targetDoor.Open)
                {
                    continue;
                }

                pawn.jobs.EndCurrentJob(JobCondition.Succeeded);
            }
        }

        public static void ProcessBabyExpansionMoodReplacement(Map map)
        {
            if (!IsBabyExpansionEnabled || map == null)
            {
                return;
            }

            ThoughtDef thoughtDef = ResolveBabyExpansionVisitorMoodThoughtDef();
            if (thoughtDef == null)
            {
                return;
            }

            List<Pawn> pawns = GetCachedIncidentMoodChildren(map);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.needs?.mood?.thoughts?.memories == null)
                {
                    continue;
                }

                pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(thoughtDef);
            }
        }

        public static void NotifyMouseDisasterPawnIdentityOrLifeStageChanged(Pawn pawn)
        {
            NormalizeMouseDisasterPawnIdentityOrLifeStageChanged(pawn, markMapCacheDirty: true);
        }

        private static void NormalizeMouseDisasterPawnIdentityOrLifeStageChanged(Pawn pawn, bool markMapCacheDirty)
        {
            if (markMapCacheDirty)
            {
                MarkMapPawnCacheDirty(pawn);
            }

            if (pawn == null || pawn.Dead || !IsRatkin(pawn))
            {
                return;
            }

            bool hostileIncidentVisitor = MouseDisasterGeneRestorePolicy.ShouldSkipVisitorNormalizationWhileTurningHostile(
                isIncidentVisitor: IsMouseDisasterIncidentVisitor(pawn),
                factionHostileToPlayer: pawn.Faction != null && Faction.OfPlayer != null && pawn.Faction.HostileTo(Faction.OfPlayer));
            if (hostileIncidentVisitor)
            {
                TryEnsureToddlerCompatibilityHediffs(pawn);
                return;
            }

            if (IsMouseDisasterPawn(pawn) || IsMouseDisasterIncidentVisitor(pawn))
            {
                NormalizeMouseDisasterPawnGenes(pawn);
            }

            if (IsMouseDisasterIncidentVisitor(pawn))
            {
                ConfigureNoRescueJoinForIncidentVisitor(pawn);
            }

            if (IsPlayerAffiliatedRatkin(pawn))
            {
                TryNormalizePlayerAffiliatedMouseDisasterState(pawn);
            }

            TryEnsureToddlerCompatibilityHediffs(pawn);
        }

        public static bool IsMouseDisasterIncidentChild(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || !IsMouseEggOrChild(pawn))
            {
                return false;
            }

            return pawn.relations?.DirectRelations != null &&
                   pawn.relations.DirectRelations.Any(r => r.def == PawnRelationDefOf.Parent &&
                                                          r.otherPawn != null &&
                                                          IsMouseDisasterIncidentParentAdult(r.otherPawn));
        }

        private static ThoughtDef ResolveBabyExpansionVisitorMoodThoughtDef()
        {
            if (babyExpansionVisitorMoodThoughtResolved)
            {
                return babyExpansionVisitorMoodThoughtDef;
            }

            babyExpansionVisitorMoodThoughtResolved = true;
            babyExpansionVisitorMoodThoughtDef = DefDatabase<ThoughtDef>.GetNamedSilentFail(BabyExpansionVisitorMoodThoughtDefName);
            return babyExpansionVisitorMoodThoughtDef;
        }

        private static bool IsDoorStuckCandidate(Pawn pawn)
        {
            if (pawn?.jobs == null || pawn.Dead || pawn.Downed || !pawn.Spawned || pawn.Map == null)
            {
                return false;
            }

            PawnKindDef kindDef = pawn.kindDef;
            string kindDefName = kindDef?.defName;
            if (kindDefName.NullOrEmpty())
            {
                return false;
            }

            return kindDefName.StartsWith("MouseDisaster_", StringComparison.OrdinalIgnoreCase) && !IsPlayerAffiliatedRatkin(pawn);
        }

        private static void NormalizeMouseDisasterPawnGenes(Pawn pawn)
        {
            if (!ModsConfig.BiotechActive || pawn?.genes == null)
            {
                return;
            }

            StripNonMouseDisasterGenes(pawn);

            MouseDisasterGeneRestoreDecision decision = MouseDisasterGeneRestorePolicy.DecideMissingGeneRestore(
                isIncidentVisitor: IsMouseDisasterIncidentVisitor(pawn),
                hasMouseDisasterGenes: HasAnyMouseDisasterGene(pawn),
                hasManualRemovalMarker: HasManualMouseDisasterGeneRemovalMarker(pawn));

            if (decision == MouseDisasterGeneRestoreDecision.SuppressRestore)
            {
                StripMouseDisasterGenes(pawn);
                CleanupOrphanedChemicalDependencies(pawn);
                return;
            }

            if (decision == MouseDisasterGeneRestoreDecision.RestoreMissingGenes)
            {
                MouseDisasterSubtypeUtility.ApplySubtypeGenes(pawn);
            }

            CleanupOrphanedChemicalDependencies(pawn);
        }

        private static void TryNormalizePlayerAffiliatedMouseDisasterState(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            if (MouseDisasterGeneRestorePolicy.ShouldPreserveTraderKindForIncidentVisitor(
                    isIncidentVisitor: IsMouseDisasterIncidentVisitor(pawn),
                    isTraderAdult: IsMouseDisasterTraderAdult(pawn)))
            {
                pawn.mindState?.duty = null;
                return;
            }

            if (IsThiefPawn(pawn) ||
                IsBeggarPawn(pawn) ||
                IsWildMouseDisasterKind(pawn) ||
                IsMouseDisasterTraderAdult(pawn) ||
                IsMouseDisasterTraderEscort(pawn))
            {
                PawnKindDef fallbackKind = ResolvePlayerAffiliatedRatkinKindDef(pawn);
                if (fallbackKind != null && fallbackKind != pawn.kindDef)
                {
                    pawn.ChangeKind(fallbackKind);
                }
            }

            if (IsInThiefMentalState(pawn) || IsInBeggarMentalState(pawn))
            {
                pawn.mindState?.mentalStateHandler?.Reset();
            }

            pawn.mindState?.duty = null;
        }

        public static void ConfigureNoRescueJoinForIncidentVisitor(Pawn pawn)
        {
            if (pawn == null || !IsMouseDisasterIncidentVisitor(pawn) || MouseDisasterVisitorUtility.IsShelteredVisitor(pawn))
            {
                return;
            }

            if (pawn.mindState != null)
            {
                pawn.mindState.WillJoinColonyIfRescued = false;
            }

            if (pawn.guest != null)
            {
                pawn.guest.leftAfterRescue = true;
                pawn.guest.getRescuedThoughtOnUndownedBecauseOfPlayer = false;
            }
        }

        public static void TryRecoverIncidentVisitorFromPlayerGuest(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || !IsMouseDisasterIncidentVisitor(pawn) || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony || MouseDisasterVisitorUtility.IsShelteredVisitor(pawn))
            {
                return;
            }

            if (pawn.guest == null || pawn.guest.HostFaction != Faction.OfPlayer)
            {
                return;
            }

            ConfigureNoRescueJoinForIncidentVisitor(pawn);
            if (pawn.guest.HostFaction != null)
            {
                pawn.guest.SetGuestStatus(null, GuestStatus.Guest);
            }

            if (IsHospitalityEnabled)
            {
                pawn.mindState?.mentalStateHandler?.Reset();
                pawn.jobs?.StopAll();
                pawn.GetLord()?.RemovePawn(pawn);
            }

            if (pawn.Map != null && pawn.Spawned)
            {
                Job exitJob = ExitMapJob(pawn);
                if (exitJob != null)
                {
                    pawn.jobs?.TryTakeOrderedJob(exitJob);
                }
            }
        }

        private static PawnKindDef ResolvePlayerAffiliatedRatkinKindDef(Pawn pawn)
        {
            bool juvenile = pawn != null && !pawn.DevelopmentalStage.Adult();
            if (!juvenile && playerAffiliatedRatkinKindResolved)
            {
                return playerAffiliatedRatkinKindDef;
            }

            ThingDef raceDef = ResolveRatkinRaceDef(null);
            if (raceDef == null)
            {
                return null;
            }

            List<PawnKindDef> candidates = DefDatabase<PawnKindDef>.AllDefsListForReading
                .Where(def => def != null &&
                              def.race == raceDef &&
                              !def.defName.NullOrEmpty() &&
                              !def.defName.StartsWith("MouseDisaster_", StringComparison.OrdinalIgnoreCase) &&
                              !def.defName.StartsWith("WildMan", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (candidates.Count == 0)
            {
                return null;
            }

            IEnumerable<PawnKindDef> stageFiltered = juvenile
                ? candidates.Where(def => def.maxGenerationAge <= RatkinYoungChildMaxAgeYears + 0.1f)
                : candidates.Where(def => def.maxGenerationAge >= RatkinAdultMinAgeYears);
            List<PawnKindDef> filteredCandidates = stageFiltered.ToList();
            if (filteredCandidates.Count == 0)
            {
                filteredCandidates = candidates;
            }

            PawnKindDef resolved = filteredCandidates
                .OrderByDescending(def => def.canMeleeAttack)
                .ThenByDescending(def => def.isFighter)
                .ThenByDescending(def => (def.defName ?? string.Empty).IndexOf("colonist", StringComparison.OrdinalIgnoreCase) >= 0)
                .ThenByDescending(def => (def.label ?? string.Empty).IndexOf("殖民", StringComparison.OrdinalIgnoreCase) >= 0)
                .ThenBy(def => (def.defName ?? string.Empty).Length)
                .FirstOrDefault();

            if (!juvenile)
            {
                playerAffiliatedRatkinKindResolved = true;
                playerAffiliatedRatkinKindDef = resolved;
            }

            return resolved;
        }

        private static void TryEnsureToddlerCompatibilityHediffs(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return;
            }

            bool shouldApplyCompatibility = MouseDisasterGeneRestorePolicy.ShouldApplyRimTalkToddlerCompatibility(
                isMouseDisasterPawn: IsMouseDisasterPawn(pawn),
                isIncidentVisitor: IsMouseDisasterIncidentVisitor(pawn),
                isPlayerAffiliatedRatkin: IsPlayerAffiliatedRatkin(pawn));
            if (!shouldApplyCompatibility)
            {
                return;
            }

            if (syncingToddlerCompat)
            {
                return;
            }

            syncingToddlerCompat = true;
            try
            {
                ResolveToddlerCompatibilityDefsIfNeeded();
                float ageYears = pawn.ageTracker?.AgeBiologicalYearsFloat ?? -1f;
                bool toddlerAge = ageYears >= 1f && ageYears < 3f;
                bool infantAge = ageYears >= 0f && ageYears < 1f;

                if (toddlerAge)
                {
                    EnsureHediffPresentIfMissing(pawn, toddlersLearningToWalkDef, 0.001f);
                    EnsureHediffPresentIfMissing(pawn, toddlersLearningManipulationDef, 0.001f);
                    EnsureHediffPresentIfMissing(pawn, toddlersLonelyDef, 0.001f);
                    EnsureHediffPresentIfMissing(pawn, rimTalkToddlerLanguageLearningDef, 0.001f);
                    RemoveHediffIfPresent(pawn, rimTalkBabyBabblingDef);
                    return;
                }

                if (infantAge)
                {
                    EnsureHediffPresentIfMissing(pawn, rimTalkBabyBabblingDef, 1f);
                    return;
                }

                RemoveHediffIfPresent(pawn, toddlersLearningToWalkDef);
                RemoveHediffIfPresent(pawn, toddlersLearningManipulationDef);
                RemoveHediffIfPresent(pawn, toddlersLonelyDef);
                RemoveHediffIfPresent(pawn, rimTalkBabyBabblingDef);
                RemoveHediffIfPresent(pawn, rimTalkToddlerLanguageLearningDef);
            }
            finally
            {
                syncingToddlerCompat = false;
            }
        }

        public static bool IsToddlerCompatibilityHediffDef(HediffDef hediffDef)
        {
            if (hediffDef == null)
            {
                return false;
            }

            string defName = hediffDef.defName ?? string.Empty;
            return defName.Equals("LearningToWalk", StringComparison.OrdinalIgnoreCase) ||
                   defName.Equals("LearningManipulation", StringComparison.OrdinalIgnoreCase) ||
                   defName.Equals("ToddlerLonely", StringComparison.OrdinalIgnoreCase) ||
                   defName.Equals("RimTalk_BabyBabbling", StringComparison.OrdinalIgnoreCase) ||
                   defName.Equals("RimTalk_ToddlerLanguageLearning", StringComparison.OrdinalIgnoreCase);
        }

        private static void ResolveToddlerCompatibilityDefsIfNeeded()
        {
            if (toddlerCompatDefsResolved)
            {
                return;
            }

            toddlerCompatDefsResolved = true;
            toddlersLearningToWalkDef = DefDatabase<HediffDef>.GetNamedSilentFail("LearningToWalk");
            toddlersLearningManipulationDef = DefDatabase<HediffDef>.GetNamedSilentFail("LearningManipulation");
            toddlersLonelyDef = DefDatabase<HediffDef>.GetNamedSilentFail("ToddlerLonely");
            rimTalkBabyBabblingDef = DefDatabase<HediffDef>.GetNamedSilentFail("RimTalk_BabyBabbling");
            rimTalkToddlerLanguageLearningDef = DefDatabase<HediffDef>.GetNamedSilentFail("RimTalk_ToddlerLanguageLearning");
        }

        private static void EnsureHediffPresentIfMissing(Pawn pawn, HediffDef hediffDef, float minimumSeverity)
        {
            if (pawn?.health?.hediffSet == null || hediffDef == null || pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef) != null)
            {
                return;
            }

            Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn);
            if (hediff != null && hediff.Severity < minimumSeverity)
            {
                hediff.Severity = minimumSeverity;
            }

            if (hediff != null)
            {
                pawn.health.AddHediff(hediff);
            }
        }

        private static void RemoveHediffIfPresent(Pawn pawn, HediffDef hediffDef)
        {
            if (pawn?.health?.hediffSet == null || hediffDef == null)
            {
                return;
            }

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (hediff != null)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }

        private enum ChaosPregnancyIdentity
        {
            Colonist,
            Prisoner,
            Slave,
            Enemy
        }

        private static bool TryGetChaosPregnancyIdentity(Pawn pawn, out ChaosPregnancyIdentity identity)
        {
            identity = ChaosPregnancyIdentity.Colonist;
            if (pawn == null || pawn.RaceProps == null || !pawn.RaceProps.Humanlike || pawn.RaceProps.Animal || pawn.RaceProps.IsMechanoid)
            {
                return false;
            }

            if (pawn.IsPrisonerOfColony)
            {
                identity = ChaosPregnancyIdentity.Prisoner;
                return true;
            }

            if (pawn.IsSlaveOfColony)
            {
                identity = ChaosPregnancyIdentity.Slave;
                return true;
            }

            if (pawn.Faction == Faction.OfPlayer)
            {
                identity = ChaosPregnancyIdentity.Colonist;
                return true;
            }

            if (pawn.Faction != null && Faction.OfPlayer != null && pawn.Faction.HostileTo(Faction.OfPlayer))
            {
                identity = ChaosPregnancyIdentity.Enemy;
                return true;
            }

            return false;
        }

        public static bool HasBeggarSucceeded(Pawn pawn)
        {
            return pawn != null && BegSuccess.Contains(pawn.thingIDNumber);
        }

        public static Pawn FindPlayerOwnedBabyForExchange(Map map)
        {
            if (map == null)
            {
                return null;
            }

            return map.mapPawns.AllPawnsSpawned
                .Where(pawn => IsMouseEggBaby(pawn) &&
                               (pawn.Faction == Faction.OfPlayer || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony))
                .OrderBy(pawn => pawn.ageTracker.AgeBiologicalTicks)
                .FirstOrDefault();
        }

        public static void MakeFactionHostileToPlayer(Faction faction, bool explicitDriveAway = false)
        {
            if (faction == null || Faction.OfPlayer == null || faction.HostileTo(Faction.OfPlayer))
            {
                return;
            }

            bool hiddenFaction = IsMouseDisasterHiddenFaction(faction);
            if (hiddenFaction &&
                !MouseDisasterVisitorHostilityPolicy.ShouldForceHiddenFactionHostility(explicitDriveAway, hiddenFaction))
            {
                EnsureFactionRelationWithPlayer(faction, FactionRelationKind.Neutral, 0);
                return;
            }

            if (hiddenFaction && explicitDriveAway)
            {
                hiddenFactionExplicitHostilityUntilTick = (Find.TickManager?.TicksGame ?? 0) + HiddenFactionExplicitHostilityDurationTicks;
            }

            Faction.OfPlayer.TryAffectGoodwillWith(faction, Faction.OfPlayer.GoodwillToMakeHostile(faction), canSendMessage: false, canSendHostilityLetter: false);
            faction.TryAffectGoodwillWith(Faction.OfPlayer, faction.GoodwillToMakeHostile(Faction.OfPlayer), canSendMessage: false, canSendHostilityLetter: false);
            EnsureFactionRelationWithPlayer(faction, FactionRelationKind.Hostile, -100);
        }

        public static void MakeFactionNeutralToPlayer(Faction faction, bool force = false)
        {
            if (faction == null || Faction.OfPlayer == null)
            {
                return;
            }

            bool hiddenFaction = IsMouseDisasterHiddenFaction(faction);
            if (hiddenFaction)
            {
                hiddenFactionExplicitHostilityUntilTick = 0;
                cachedHostilePawnCheckTick = -1;
                cachedHostilePawnExists = false;
            }

            EnsureFactionRelationWithPlayer(faction, FactionRelationKind.Neutral, hiddenFaction ? 0 : 20);
        }

        private static void EnsureHiddenFactionRelations(Faction faction)
        {
            if (faction == null || Find.FactionManager == null)
            {
                return;
            }

            if (Faction.OfPlayer != null && !IsHiddenFactionExplicitHostilityActive())
            {
                EnsureFactionRelationPair(faction, Faction.OfPlayer, FactionRelationKind.Neutral, 0);
            }

            IReadOnlyList<Faction> allFactions = Find.FactionManager.AllFactionsListForReading;
            if (allFactions == null || allFactions.Count == lastHiddenFactionRelationSyncFactionCount)
            {
                return;
            }

            lastHiddenFactionRelationSyncFactionCount = allFactions.Count;
            for (int i = 0; i < allFactions.Count; i++)
            {
                Faction other = allFactions[i];
                if (other == null || other == faction || other == Faction.OfPlayer)
                {
                    continue;
                }

                EnsureFactionRelationPair(faction, other, FactionRelationKind.Hostile, -100);
            }
        }

        public static void EnsureMouseDisasterFactionNeutralOnMap(Map map, Faction faction)
        {
            if (map == null || faction == null)
            {
                return;
            }

            MakeFactionNeutralToPlayer(faction, force: true);
            IReadOnlyList<Pawn> pawns = map.mapPawns?.AllPawnsSpawned;
            if (pawns == null)
            {
                return;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.Dead || pawn.Faction != faction || !IsRatkin(pawn))
                {
                    continue;
                }

                if (pawn.InAggroMentalState)
                {
                    pawn.mindState?.mentalStateHandler?.Reset();
                }

                pawn.mindState?.duty = null;
            }
        }

        private static void EnsureFactionRelationWithPlayer(Faction faction, FactionRelationKind kind, int goodwill)
        {
            if (faction == null || Faction.OfPlayer == null)
            {
                return;
            }

            bool hiddenFaction = IsMouseDisasterHiddenFaction(faction);
            bool explicitDriveAway = hiddenFaction && kind == FactionRelationKind.Hostile && IsHiddenFactionExplicitHostilityActive();
            if (!MouseDisasterVisitorHostilityPolicy.ShouldKeepRequestedRelationKind(hiddenFaction, explicitDriveAway))
            {
                kind = FactionRelationKind.Neutral;
                goodwill = 0;
            }

            EnsureFactionRelationPair(faction, Faction.OfPlayer, kind, goodwill);
        }

        private static bool IsMouseDisasterHiddenFaction(Faction faction)
        {
            return faction != null &&
                   MouseDisasterDefOf.MouseDisaster_HiddenFaction != null &&
                   faction.def == MouseDisasterDefOf.MouseDisaster_HiddenFaction;
        }

        private static bool IsHiddenFactionExplicitHostilityActive()
        {
            int nowTick = Find.TickManager?.TicksGame ?? 0;
            if (hiddenFactionExplicitHostilityUntilTick > nowTick)
            {
                return true;
            }

            if (cachedHostilePawnCheckTick == nowTick)
            {
                return cachedHostilePawnExists;
            }

            cachedHostilePawnCheckTick = nowTick;
            cachedHostilePawnExists = false;

            if (Find.Maps == null)
            {
                return false;
            }

            for (int mapIndex = 0; mapIndex < Find.Maps.Count; mapIndex++)
            {
                Map map = Find.Maps[mapIndex];
                if (map?.mapPawns == null)
                {
                    continue;
                }

                IReadOnlyList<Pawn> spawned = map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < spawned.Count; i++)
                {
                    Pawn pawn = spawned[i];
                    if (pawn != null &&
                        !pawn.Dead &&
                        IsRatkin(pawn) &&
                        pawn.Faction != null &&
                        IsMouseDisasterHiddenFaction(pawn.Faction) &&
                        Faction.OfPlayer != null &&
                        pawn.Faction.HostileTo(Faction.OfPlayer))
                    {
                        cachedHostilePawnExists = true;
                        return true;
                    }
                }
            }

            return false;
        }

        private static void EnsureFactionRelationPair(Faction first, Faction second, FactionRelationKind kind, int goodwill)
        {
            if (first == null || second == null || first == second)
            {
                return;
            }

            FactionRelation relationToSecond = first.RelationWith(second, allowNull: true);
            if (relationToSecond == null)
            {
                first.SetRelation(new FactionRelation
                {
                    other = second,
                    kind = kind,
                    baseGoodwill = goodwill
                });
                relationToSecond = first.RelationWith(second, allowNull: true);
            }

            if (relationToSecond != null)
            {
                relationToSecond.kind = kind;
                relationToSecond.baseGoodwill = goodwill;
            }

            FactionRelation relationToFirst = second.RelationWith(first, allowNull: true);
            if (relationToFirst == null)
            {
                second.SetRelation(new FactionRelation
                {
                    other = first,
                    kind = kind,
                    baseGoodwill = goodwill
                });
                relationToFirst = second.RelationWith(first, allowNull: true);
            }

            if (relationToFirst != null)
            {
                relationToFirst.kind = kind;
                relationToFirst.baseGoodwill = goodwill;
            }
        }

        private static bool EnsureFactionRelationExists(Faction owner, Faction other)
        {
            if (owner == null || other == null || owner == other)
            {
                return false;
            }

            FactionRelation relation = owner.RelationWith(other, allowNull: true);
            if (relation != null)
            {
                return false;
            }

            owner.SetRelation(new FactionRelation
            {
                other = other,
                kind = FactionRelationKind.Neutral,
                baseGoodwill = other == Faction.OfPlayer ? 20 : 0
            });
            return true;
        }

        private static ThingDef ResolveRatkinRaceDef(PawnKindDef kindDef)
        {
            if (kindDef?.race != null && IsRatkinRaceDef(kindDef.race))
            {
                return kindDef.race;
            }

            if (ratkinRaceResolved)
            {
                return ratkinRaceDef;
            }

            ratkinRaceResolved = true;
            ratkinRaceDef = DefDatabase<ThingDef>.GetNamedSilentFail(RatkinRaceDefName);
            if (ratkinRaceDef?.race == null)
            {
                ratkinRaceDef = DefDatabase<ThingDef>.AllDefsListForReading
                    .Where(def => def?.race != null)
                    .OrderByDescending(def => def.defName.EqualsIgnoreCase(RatkinRaceDefName))
                    .ThenByDescending(def => IsRatkinRaceDef(def))
                    .FirstOrDefault(def => IsRatkinRaceDef(def));
            }

            return ratkinRaceDef;
        }

        private static XenotypeDef ResolveRatkinXenotypeDef()
        {
            if (ratkinXenotypeResolved)
            {
                return ratkinXenotypeDef;
            }

            ratkinXenotypeResolved = true;
            bool preferChineseLabel = IsChineseLanguageActive();
            List<XenotypeDef> ratkinXenotypes = DefDatabase<XenotypeDef>.AllDefsListForReading
                .Where(def => def != null && IsRatkinXenotypeDef(def))
                .ToList();

            XenotypeDef localDefaultSubtype = DefDatabase<XenotypeDef>.GetNamedSilentFail("MouseDisasterSubtype_ratkin");
            if (localDefaultSubtype != null)
            {
                ratkinXenotypeDef = localDefaultSubtype;
            }

            for (int i = 0; i < PreferredRatkinXenotypeDefNames.Length; i++)
            {
                XenotypeDef preferred = DefDatabase<XenotypeDef>.GetNamedSilentFail(PreferredRatkinXenotypeDefNames[i]);
                if (preferred != null && IsRatkinXenotypeDef(preferred))
                {
                    ratkinXenotypeDef = preferred;
                    return ratkinXenotypeDef;
                }
            }

            if (ratkinXenotypeDef != null)
            {
                return ratkinXenotypeDef;
            }

            if (preferChineseLabel)
            {
                XenotypeDef localized = ratkinXenotypes
                    .Where(def => HasChineseCharacters(def.label) || def.defName.EqualsIgnoreCase(RatkinXenotypeDefName))
                    .OrderByDescending(def => def.defName.EqualsIgnoreCase("MouseDisasterSubtype_ratkin"))
                    .ThenByDescending(def => def.defName.EqualsIgnoreCase(RatkinXenotypeDefName))
                    .ThenByDescending(def => HasChineseCharacters(def.label))
                    .ThenBy(def => def.defName)
                    .FirstOrDefault();
                if (localized != null)
                {
                    ratkinXenotypeDef = localized;
                    return ratkinXenotypeDef;
                }
            }

            ratkinXenotypeDef = ratkinXenotypes
                .OrderBy(def => (def.defName ?? string.Empty).IndexOf("waster", StringComparison.OrdinalIgnoreCase) >= 0)
                .ThenByDescending(IsRatkinXenotypeDef)
                .ThenByDescending(def => def.defName.EqualsIgnoreCase("MouseDisasterSubtype_ratkin"))
                .ThenByDescending(def => def.defName.EqualsIgnoreCase(RatkinXenotypeDefName))
                .FirstOrDefault(IsRatkinXenotypeDef);
            return ratkinXenotypeDef;
        }

        private static List<XenotypeDef> ResolveAllowedRatkinXenotypes()
        {
            if (cachedAllowedRatkinXenotypesResolved)
            {
                return cachedAllowedRatkinXenotypes;
            }

            cachedAllowedRatkinXenotypesResolved = true;
            List<XenotypeDef> allowed = DefDatabase<XenotypeDef>.AllDefsListForReading
                .Where(def => def != null && IsRatkinXenotypeDef(def))
                .OrderByDescending(def => PreferredRatkinXenotypeDefNames.Contains(def.defName))
                .ThenBy(def => def.defName)
                .ToList();

            if (IsChineseLanguageActive())
            {
                List<XenotypeDef> localized = allowed
                    .Where(def => HasChineseCharacters(def.label) || def.defName.EqualsIgnoreCase(RatkinXenotypeDefName))
                    .ToList();
                if (localized.Count > 0)
                {
                    allowed = localized;
                }
            }

            cachedAllowedRatkinXenotypes = allowed;
            return cachedAllowedRatkinXenotypes;
        }

        private static void NormalizeHiddenFactionDisplayName(Faction faction)
        {
            if (faction == null || MouseDisasterDefOf.MouseDisaster_HiddenFaction == null || faction.def != MouseDisasterDefOf.MouseDisaster_HiddenFaction)
            {
                return;
            }

            if (!IsChineseLanguageActive())
            {
                return;
            }

            string currentName = faction.Name ?? string.Empty;
            if (!HasChineseCharacters(currentName) ||
                currentName.IndexOf("MouseDisaster", StringComparison.OrdinalIgnoreCase) >= 0 ||
                currentName.IndexOf("survivor", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                faction.Name = HiddenFactionChineseName;
            }
        }

        private static bool IsChineseLanguageActive()
        {
            LoadedLanguage language = LanguageDatabase.activeLanguage;
            if (language == null)
            {
                return false;
            }

            string folder = language.folderName ?? string.Empty;
            string englishName = language.FriendlyNameEnglish ?? string.Empty;
            string nativeName = language.FriendlyNameNative ?? string.Empty;
            return folder.IndexOf("Chinese", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   englishName.IndexOf("Chinese", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   nativeName.IndexOf("中文", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool HasChineseCharacters(string text)
        {
            if (text.NullOrEmpty())
            {
                return false;
            }

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if ((c >= '\u3400' && c <= '\u4DBF') || (c >= '\u4E00' && c <= '\u9FFF'))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsRatkinXenotypeDef(XenotypeDef xenotypeDef)
        {
            if (xenotypeDef == null)
            {
                return false;
            }

            if (IsRatkinXenotypeDefCache.TryGetValue(xenotypeDef, out bool cached))
            {
                return cached;
            }

            bool result = MouseDisasterBirthPolicy.IsRatkinLikeXenotype(xenotypeDef.defName, xenotypeDef.label);
            IsRatkinXenotypeDefCache[xenotypeDef] = result;
            return result;
        }

        private static bool IsRatkinRaceDef(ThingDef raceDef)
        {
            if (raceDef?.race == null)
            {
                return false;
            }

            if (IsRatkinRaceDefCache.TryGetValue(raceDef, out bool cached))
            {
                return cached;
            }

            bool result = false;
            if (raceDef.defName.EqualsIgnoreCase(RatkinRaceDefName))
            {
                result = true;
            }
            else
            {
                string source = ((raceDef.defName ?? string.Empty) + " " + (raceDef.label ?? string.Empty)).ToLowerInvariant();
                for (int i = 0; i < RatkinRaceKeywords.Length; i++)
                {
                    if (source.Contains(RatkinRaceKeywords[i]))
                    {
                        result = true;
                        break;
                    }
                }
            }

            IsRatkinRaceDefCache[raceDef] = result;
            return result;
        }

        private static PawnKindDef ResolveRatkinKindDef(PawnKindDef preferredKindDef, DevelopmentalStage stage, ThingDef expectedRace)
        {
            if (stage == DevelopmentalStage.Baby)
            {
                PawnKindDef mappedBabyKind = ResolveMappedBabyRatkinKindDef(preferredKindDef, expectedRace);
                if (mappedBabyKind != null)
                {
                    return mappedBabyKind;
                }
            }

            if (preferredKindDef != null &&
                (expectedRace == null || preferredKindDef.race == expectedRace) &&
                CanGenerateRequestedStage(preferredKindDef, stage))
            {
                return preferredKindDef;
            }

            string defName = preferredKindDef?.defName ?? string.Empty;
            bool isAdult = stage.Adult();
            PawnKindDef categorized = null;
            if (defName.IndexOf("Trader", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                categorized = MouseDisasterDefOf.MouseDisaster_TraderRatkinAdult;
            }
            else if (defName.IndexOf("Thief", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                categorized = isAdult ? MouseDisasterDefOf.MouseDisaster_ThiefRatkinAdult : MouseDisasterDefOf.MouseDisaster_ThiefRatkinChild;
            }
            else if (defName.IndexOf("Beggar", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                categorized = isAdult ? MouseDisasterDefOf.MouseDisaster_BeggarRatkinAdult : MouseDisasterDefOf.MouseDisaster_BeggarRatkinChild;
            }

            if (categorized != null &&
                (expectedRace == null || categorized.race == expectedRace) &&
                CanGenerateRequestedStage(categorized, stage))
            {
                return categorized;
            }

            PawnKindDef fallback = isAdult ? MouseDisasterDefOf.MouseDisaster_WildRatkinAdult : MouseDisasterDefOf.MouseDisaster_WildRatkinChild;
            if (!CanGenerateRequestedStage(fallback, stage))
            {
                fallback = null;
            }

            if (expectedRace != null && (fallback == null || fallback.race != expectedRace))
            {
                fallback = DefDatabase<PawnKindDef>.AllDefsListForReading
                    .FirstOrDefault(kind => kind != null &&
                                            kind.race == expectedRace &&
                                            CanGenerateRequestedStage(kind, stage));
            }

            return fallback;
        }

        private static PawnKindDef ResolveMappedBabyRatkinKindDef(PawnKindDef preferredKindDef, ThingDef expectedRace)
        {
            string preferredDefName = preferredKindDef?.defName;
            if (preferredDefName.NullOrEmpty())
            {
                return null;
            }

            string candidateDefName = preferredDefName.Replace("Child", "Baby");
            if (candidateDefName == preferredDefName)
            {
                return null;
            }

            PawnKindDef mapped = DefDatabase<PawnKindDef>.GetNamedSilentFail(candidateDefName);
            if (mapped == null)
            {
                return null;
            }

            if (expectedRace != null && mapped.race != expectedRace)
            {
                return null;
            }

            return mapped;
        }

        private static bool CanGenerateRequestedStage(PawnKindDef kindDef, DevelopmentalStage stage)
        {
            if (kindDef == null)
            {
                return false;
            }

            if (stage == DevelopmentalStage.Baby)
            {
                return kindDef.minGenerationAge < RatkinYoungChildMinAgeYears;
            }

            if (stage == DevelopmentalStage.Child)
            {
                return kindDef.maxGenerationAge > RatkinYoungChildMinAgeYears &&
                       kindDef.minGenerationAge < RatkinAdultMinAgeYears;
            }

            return kindDef.maxGenerationAge >= RatkinAdultMinAgeYears;
        }

        private static void EnsureStageInventoryClear(Pawn pawn, DevelopmentalStage stage)
        {
            if (pawn?.inventory?.innerContainer == null)
            {
                return;
            }

            if (stage == DevelopmentalStage.Baby || stage == DevelopmentalStage.Child)
            {
                pawn.inventory.innerContainer.ClearAndDestroyContents();
            }
        }

        public static void StripRatEggInventory(Pawn pawn)
        {
            if (pawn == null || !IsMouseEggBaby(pawn) || pawn.inventory?.innerContainer == null)
            {
                return;
            }

            pawn.inventory.innerContainer.ClearAndDestroyContents();
        }

        private static readonly HediffDef[] IncidentVisitorHediffs =
        {
            MouseDisasterDefOf.MouseDisaster_AbandonedEgg,
            MouseDisasterDefOf.MouseDisaster_AbandoningMother,
            MouseDisasterDefOf.MouseDisaster_ShatteredMother_Dying,
            MouseDisasterDefOf.MouseDisaster_ShatteredMother_Children,
            MouseDisasterDefOf.MouseDisaster_BeggarMother,
            MouseDisasterDefOf.MouseDisaster_BeggarChild,
            MouseDisasterDefOf.MouseDisaster_BeggarGroupAdult,
            MouseDisasterDefOf.MouseDisaster_BeggarGroupChild,
            MouseDisasterDefOf.MouseDisaster_ThiefAdult,
            MouseDisasterDefOf.MouseDisaster_ThiefChild,
            MouseDisasterDefOf.MouseDisaster_ThiefChildOnly,
            MouseDisasterDefOf.MouseDisaster_LargeRefugeeAdult,
            MouseDisasterDefOf.MouseDisaster_LargeRefugeeChild,
            MouseDisasterDefOf.MouseDisaster_FamineRefugee,
            MouseDisasterDefOf.MouseDisaster_WildWanderer,
            MouseDisasterDefOf.MouseDisaster_WildChildWanderer,
            MouseDisasterDefOf.MouseDisaster_WildGroupWanderer,
            MouseDisasterDefOf.MouseDisaster_TraderCaravanTrader,
            MouseDisasterDefOf.MouseDisaster_TraderCaravanEscort,
            MouseDisasterDefOf.MouseDisaster_TraderCaravanChild,
            MouseDisasterDefOf.MouseDisaster_ChildExchangeTrader,
            MouseDisasterDefOf.MouseDisaster_ChildExchangeChild,
            MouseDisasterDefOf.MouseDisaster_SiegeBeggar,
            MouseDisasterDefOf.MouseDisaster_GreatFamineAdult,
            MouseDisasterDefOf.MouseDisaster_GreatFamineChild,
        };

        public static void RemoveAllIncidentVisitorHediffs(Pawn pawn)
        {
            if (pawn?.health == null)
            {
                return;
            }

            for (int i = 0; i < IncidentVisitorHediffs.Length; i++)
            {
                RemoveHediffByDef(pawn, IncidentVisitorHediffs[i]);
            }
        }

        private static void StripRatEggEquipmentIfNeeded(Pawn pawn, DevelopmentalStage stage)
        {
            if (pawn == null || stage != DevelopmentalStage.Baby)
            {
                return;
            }

            pawn.equipment?.DestroyAllEquipment();
        }

        public static void RefreshRatkinDevelopmentalPresentation(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            AssignBodyTypeForCurrentStage(pawn);
            pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            pawn.ageTracker?.PostResolveLifeStageChange();
            TryEnsureToddlerCompatibilityHediffs(pawn);
        }

        public static void EnsureRatEggMobility(Pawn pawn)
        {
            if (pawn == null || !IsMouseEggBaby(pawn) || pawn.ageTracker == null)
            {
                return;
            }

            if (pawn.ageTracker.AgeBiologicalYearsFloat < RatEggMinAgeYears)
            {
                SetBiologicalAgeYears(pawn, Rand.Range(RatEggMinAgeYears, RatEggMaxAgeYears));
            }
        }

        private static void TryApplyRandomRatEggMissingParts(Pawn pawn)
        {
            if (!IsMouseEggBaby(pawn) || pawn.health?.hediffSet == null || !Rand.Chance(0.2f))
            {
                return;
            }

            int missingCount = 1;
            if (Rand.Chance(0.08f))
            {
                missingCount++;
            }

            if (missingCount > 1 && Rand.Chance(0.03f))
            {
                missingCount++;
            }

            HashSet<string> usedPartKinds = new HashSet<string>();
            int attempts = 0;
            while (usedPartKinds.Count < missingCount && attempts < 16)
            {
                attempts++;
                string partKind = RandomMissingPartPool.RandomElement();
                if (partKind == null || usedPartKinds.Contains(partKind))
                {
                    continue;
                }

                if (TryRemoveSinglePartByKind(pawn, partKind))
                {
                    usedPartKinds.Add(partKind);
                }
            }
        }

        private static bool TryRemoveSinglePartByKind(Pawn pawn, string partKind)
        {
            if (pawn?.health?.hediffSet == null || partKind.NullOrEmpty())
            {
                return false;
            }

            BodyPartRecord part = null;
            switch (partKind)
            {
                case EarPartDefName:
                    part = FindRemovablePart(pawn, EarPartDefName, null);
                    break;
                case RatTailPartDefName:
                    part = FindRemovablePart(pawn, RatTailPartDefName, null) ?? FindRemovablePart(pawn, "Tail", null);
                    break;
                case FingerPartDefName:
                    part = FindRemovablePart(pawn, FingerPartDefName, null);
                    break;
                case ToePartDefName:
                    part = FindRemovablePart(pawn, ToePartDefName, null);
                    break;
            }

            if (part == null)
            {
                return false;
            }

            pawn.health.AddHediff(HediffDefOf.MissingBodyPart, part);
            return true;
        }

        private static BodyPartRecord FindRemovablePart(Pawn pawn, string defName, string labelContains)
        {
            if (pawn?.health?.hediffSet == null || defName.NullOrEmpty())
            {
                return null;
            }

            IEnumerable<BodyPartRecord> candidates = pawn.health.hediffSet.GetNotMissingParts()
                .Where(part => part.def?.defName == defName &&
                               !pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(part) &&
                               (labelContains == null || part.Label.ToLowerInvariant().Contains(labelContains.ToLowerInvariant())));
            if (!candidates.Any())
            {
                return null;
            }

            return candidates.RandomElement();
        }

        private static void ResolvePreferredApparelStuffDefs()
        {
            if (apparelStuffResolved)
            {
                return;
            }

            apparelStuffResolved = true;
            clothStuffDef = DefDatabase<ThingDef>.GetNamedSilentFail(ClothStuffDefName);
            humanleatherStuffDef = DefDatabase<ThingDef>.GetNamedSilentFail(HumanleatherStuffDefName);
        }

        private static ThingDef TryResolvePreferredApparelStuff(ThingDef apparelDef)
        {
            if (apparelDef == null)
            {
                return null;
            }

            ResolvePreferredApparelStuffDefs();
            if (!ApparelPreferredStuffCache.TryGetValue(apparelDef, out List<ThingDef> options))
            {
                options = GenStuff.AllowedStuffsFor(apparelDef)
                    .Where(stuff => stuff == clothStuffDef || stuff == humanleatherStuffDef)
                    .Distinct()
                    .ToList();
                ApparelPreferredStuffCache[apparelDef] = options;
            }

            if (options == null || options.Count == 0)
            {
                return null;
            }

            return options.RandomElementByWeight(stuff => stuff == humanleatherStuffDef ? 0.35f : 0.65f);
        }

        private static List<ThingDef> GetRestrictedApparelDefs(IEnumerable<string> defNames)
        {
            if (defNames == null)
            {
                return new List<ThingDef>();
            }

            return defNames
                .Select(DefDatabase<ThingDef>.GetNamedSilentFail)
                .Where(def => def != null && def.IsApparel && def.MadeFromStuff && TryResolvePreferredApparelStuff(def) != null)
                .Distinct()
                .ToList();
        }

        private static List<ThingDef> GetAdultApparelDefs(Pawn pawn)
        {
            if (pawn?.kindDef == null)
            {
                return new List<ThingDef>();
            }

            if (!adultApparelDefsResolved)
            {
                adultApparelDefsResolved = true;
                HashSet<string> restricted = new HashSet<string>(BabyRestrictedApparelDefNames.Concat(ChildRestrictedApparelDefNames));
                adultApparelDefsCache = DefDatabase<ThingDef>.AllDefsListForReading
                    .Where(def =>
                        def != null &&
                        def.IsApparel &&
                        def.MadeFromStuff &&
                        def.apparel != null &&
                        def.apparel.countsAsClothingForNudity &&
                        !restricted.Contains(def.defName) &&
                        TryResolvePreferredApparelStuff(def) != null)
                    .Distinct()
                    .ToList();
            }

            if (adultApparelDefsCache == null || adultApparelDefsCache.Count == 0)
            {
                return new List<ThingDef>();
            }

            return adultApparelDefsCache
                .Where(def => ApparelUtility.HasPartsToWear(pawn, def))
                .ToList();
        }

        private static void ApplyRandomizedApparelQuality(Apparel apparel)
        {
            CompQuality qualityComp = apparel?.TryGetComp<CompQuality>();
            if (qualityComp == null)
            {
                return;
            }

            float roll = Rand.Value;
            QualityCategory quality;
            if (roll < 0.35f)
            {
                quality = QualityCategory.Awful;
            }
            else if (roll < 0.85f)
            {
                quality = QualityCategory.Poor;
            }
            else
            {
                quality = QualityCategory.Normal;
            }
            qualityComp.SetQuality(quality, ArtGenerationContext.Outsider);
        }

        private static void ApplyRandomizedApparelDurability(Apparel apparel)
        {
            if (apparel == null || apparel.MaxHitPoints <= 1)
            {
                return;
            }

            int randomHitPoints = Mathf.RoundToInt(apparel.MaxHitPoints * Rand.Range(DisasterApparelMinDurability, DisasterApparelMaxDurability));
            apparel.HitPoints = Mathf.Clamp(randomHitPoints, 1, apparel.MaxHitPoints);
        }

        public static int RecordWallGnaw(Pawn pawn)
        {
            if (pawn == null)
            {
                return 0;
            }

            int count = WallGnawCounts.TryGetValue(pawn.thingIDNumber, out int current) ? current + 1 : 1;
            WallGnawCounts[pawn.thingIDNumber] = count;
            return count;
        }

        public static bool IsMalnourished(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.GetFirstHediffOfDef(HediffDefOf.Malnutrition) != null;
        }

        public static bool HasAccessibleFood(Pawn pawn)
        {
            return HasAccessibleFood(pawn, out _);
        }

        public static bool HasAccessibleFood(Pawn pawn, out string reason)
        {
            reason = string.Empty;
            if (pawn == null)
            {
                reason = "pawn_null";
                return false;
            }

            if (HasFoodInInventory(pawn))
            {
                reason = "inventory_food";
                return true;
            }

            Job job = TryCreateImproperFoodJob(pawn, false);
            if (job == null)
            {
                reason = "no_food_job";
                return false;
            }

            if (job.def == JobDefOf.TakeFromOtherInventory)
            {
                Pawn owner = job.targetB.Thing as Pawn;
                if (owner != null && pawn.CanReserveAndReach(owner, PathEndMode.Touch, Danger.Some))
                {
                    reason = "reachable_other_inventory";
                    return true;
                }

                reason = "other_inventory_unreachable";
                return false;
            }

            Thing targetThing = job.targetA.Thing;
            if (targetThing != null)
            {
                if (targetThing.Spawned && pawn.CanReserveAndReach(targetThing, PathEndMode.ClosestTouch, Danger.Some))
                {
                    reason = "reachable_map_food";
                    return true;
                }

                reason = targetThing.Spawned ? "map_food_unreachable" : "food_not_spawned";
                return false;
            }

            if (job.targetA.IsValid && job.targetA.Cell.IsValid)
            {
                if (pawn.CanReach(job.targetA.Cell, PathEndMode.OnCell, Danger.Some))
                {
                    reason = "reachable_food_cell";
                    return true;
                }

                reason = "food_cell_unreachable";
                return false;
            }

            reason = "food_job_unknown_target";
            return false;
        }

        public static Job TryCreatePrisonerScavengeJob(Pawn pawn, bool logDecision)
        {
            if (!CanPrisonerScavenge(pawn))
            {
                if (pawn != null)
                {
                    ClearPrisonerScavengeState(pawn);
                }

                if (logDecision)
                {
                    LogPrisonerScavenge(pawn, "skip: not a valid prisoner baby on map");
                }

                return null;
            }

            if (pawn.needs.food.CurCategory < HungerCategory.Hungry)
            {
                ResetPrisonerScavengeDelayStateIfNotHungry(pawn, logDecision);

                if (logDecision)
                {
                    LogPrisonerScavenge(pawn, "skip: hunger below Hungry");
                }

                return null;
            }

            if (IsPrisonerScavengeBurstOnCooldown(pawn))
            {
                if (logDecision)
                {
                    LogPrisonerScavenge(pawn, "skip: burst cooldown active");
                }

                return null;
            }

            bool burstActive = IsPrisonerScavengeBurstActive(pawn);

            if (ShouldDeferPrisonerScavengeForCare(pawn, logDecision))
            {
                return null;
            }

            bool colonyCaptive = IsColonyCaptiveForScavenge(pawn);
            bool desperateHunger = pawn.needs?.food != null && pawn.needs.food.CurCategory >= HungerCategory.UrgentlyHungry;
            if (HasAccessibleFood(pawn, out string foodReason) && !desperateHunger && !colonyCaptive)
            {
                if (burstActive)
                {
                    EndPrisonerScavengeBurst(pawn, enterCooldown: false, logDecision, "reset: burst ended because normal food became available");
                }

                if (logDecision)
                {
                    LogPrisonerScavenge(pawn, "skip: accessible food exists (" + foodReason + ")");
                }

                return null;
            }

            if ((desperateHunger || colonyCaptive) && logDecision)
            {
                LogPrisonerScavenge(pawn, desperateHunger
                    ? "pass: desperate hunger allows scavenge despite available food"
                    : "pass: colony captive mode allows scavenge despite available food");
            }

            Filth filth = FindScavengeableFilth(pawn);
            if (filth == null)
            {
                if (burstActive)
                {
                    EndPrisonerScavengeBurst(pawn, enterCooldown: false, logDecision, "reset: burst ended because no filth target");
                }

                if (logDecision)
                {
                    LogPrisonerScavenge(pawn, "skip: no scavengeable filth in range");
                }

                return null;
            }

            if (!burstActive)
            {
                StartPrisonerScavengeBurst(pawn, logDecision);
            }

            if (logDecision)
            {
                int remaining = GetPrisonerScavengeBurstRemaining(pawn);
                LogPrisonerScavenge(pawn, "pass: target=" + filth.def.defName + " at " + filth.Position + ", burstRemaining=" + remaining);
            }

            return JobMaker.MakeJob(MouseDisasterDefOf.MouseDisaster_PrisonerScavenge, filth);
        }

        private static int GetPrisonerScavengeBurstRemaining(Pawn pawn)
        {
            if (pawn == null || !PrisonerScavengeBurstRemainingByPawnId.TryGetValue(pawn.thingIDNumber, out int remaining))
            {
                return 0;
            }

            return Mathf.Max(0, remaining);
        }

        private static void ClearPrisonerScavengeState(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            int pawnId = pawn.thingIDNumber;
            PrisonerScavengeDelayStateByPawnId.Remove(pawnId);
            PrisonerScavengeBurstRemainingByPawnId.Remove(pawnId);
            PrisonerScavengeBurstCooldownByPawnId.Remove(pawnId);
        }

        private static bool IsPrisonerScavengeBurstActive(Pawn pawn)
        {
            return GetPrisonerScavengeBurstRemaining(pawn) > 0;
        }

        private static bool IsPrisonerScavengeBurstOnCooldown(Pawn pawn)
        {
            if (pawn == null || !PrisonerScavengeBurstCooldownByPawnId.TryGetValue(pawn.thingIDNumber, out int untilTick) || untilTick <= 0)
            {
                return false;
            }

            int nowTick = Find.TickManager?.TicksGame ?? 0;
            if (untilTick > nowTick)
            {
                return true;
            }

            PrisonerScavengeBurstCooldownByPawnId.Remove(pawn.thingIDNumber);
            return false;
        }

        private static void StartPrisonerScavengeBurst(Pawn pawn, bool logDecision)
        {
            if (pawn == null)
            {
                return;
            }

            int remaining = Rand.RangeInclusive(PrisonerScavengeBurstMinCount, PrisonerScavengeBurstMaxCount);
            PrisonerScavengeBurstRemainingByPawnId[pawn.thingIDNumber] = remaining;
            PrisonerScavengeBurstCooldownByPawnId.Remove(pawn.thingIDNumber);
            if (logDecision)
            {
                LogPrisonerScavenge(pawn, "start: burst initialized with remaining=" + remaining);
            }
        }

        private static void EndPrisonerScavengeBurst(Pawn pawn, bool enterCooldown, bool logDecision, string reason)
        {
            if (pawn == null)
            {
                return;
            }

            bool hadState = PrisonerScavengeBurstRemainingByPawnId.Remove(pawn.thingIDNumber);
            if (enterCooldown)
            {
                int nowTick = Find.TickManager?.TicksGame ?? 0;
                PrisonerScavengeBurstCooldownByPawnId[pawn.thingIDNumber] = nowTick + PrisonerScavengeBurstCooldownTicks;
            }
            else
            {
                PrisonerScavengeBurstCooldownByPawnId.Remove(pawn.thingIDNumber);
            }

            if (logDecision && hadState)
            {
                LogPrisonerScavenge(pawn, reason);
            }
        }

        public static void NotifyPrisonerScavengeCompleted(Pawn pawn)
        {
            if (pawn == null || !PrisonerScavengeBurstRemainingByPawnId.TryGetValue(pawn.thingIDNumber, out int remaining) || remaining <= 0)
            {
                return;
            }

            remaining--;
            if (remaining > 0)
            {
                PrisonerScavengeBurstRemainingByPawnId[pawn.thingIDNumber] = remaining;
                if (IsPrisonerScavengeDebugLogEnabled)
                {
                    LogPrisonerScavenge(pawn, "progress: burst remaining=" + remaining);
                }

                return;
            }

            EndPrisonerScavengeBurst(pawn, enterCooldown: true, IsPrisonerScavengeDebugLogEnabled, "finish: burst completed and cooldown started");
        }

        public static bool IsPrisonerScavengeDeferred(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            ResetPrisonerScavengeDelayStateIfNotHungry(pawn, logDecision: false);
            if (!PrisonerScavengeDelayStateByPawnId.TryGetValue(pawn.thingIDNumber, out int stateTick) || stateTick <= 0)
            {
                return false;
            }

            int nowTick = Find.TickManager?.TicksGame ?? 0;
            if (stateTick > nowTick)
            {
                return true;
            }

            PrisonerScavengeDelayStateByPawnId[pawn.thingIDNumber] = 0;
            return false;
        }

        public static void ResetPrisonerScavengeDelayStateIfNotHungry(Pawn pawn, bool logDecision)
        {
            if (pawn?.needs?.food == null)
            {
                return;
            }

            if (pawn.needs.food.CurCategory >= HungerCategory.Hungry)
            {
                return;
            }

            if (PrisonerScavengeDelayStateByPawnId.Remove(pawn.thingIDNumber) && logDecision)
            {
                LogPrisonerScavenge(pawn, "reset: delay state cleared after feeding");
            }

            EndPrisonerScavengeBurst(pawn, enterCooldown: false, logDecision, "reset: burst ended after feeding");
        }

        private static bool ShouldDeferPrisonerScavengeForCare(Pawn pawn, bool logDecision)
        {
            if (pawn == null || pawn.DevelopmentalStage != DevelopmentalStage.Baby)
            {
                return false;
            }

            int pawnId = pawn.thingIDNumber;
            int nowTick = Find.TickManager?.TicksGame ?? 0;
            if (PrisonerScavengeDelayStateByPawnId.TryGetValue(pawnId, out int stateTick))
            {
                if (stateTick > nowTick)
                {
                    if (logDecision)
                    {
                        int remain = stateTick - nowTick;
                        LogPrisonerScavenge(pawn, "skip: waiting colony care feed window (" + remain + " ticks left)");
                    }

                    return true;
                }

                if (stateTick > 0)
                {
                    PrisonerScavengeDelayStateByPawnId[pawnId] = 0;
                    if (logDecision)
                    {
                        LogPrisonerScavenge(pawn, "pass: colony care delay window elapsed");
                    }
                }

                return false;
            }

            if (!HasPendingPrisonerBabyCare(pawn))
            {
                return false;
            }

            int deferUntilTick = nowTick + PrisonerScavengeCareDelayTicks;
            PrisonerScavengeDelayStateByPawnId[pawnId] = deferUntilTick;
            if (logDecision)
            {
                LogPrisonerScavenge(pawn, "skip: colony care detected, defer scavenging for 3h");
            }

            return true;
        }

        private static bool HasPendingPrisonerBabyCare(Pawn pawn)
        {
            if (pawn?.Map == null || pawn.needs?.food == null)
            {
                return false;
            }

            if (!FoodUtility.ShouldBeFedBySomeone(pawn))
            {
                return false;
            }

            List<Pawn> feeders = pawn.Map.mapPawns?.FreeColonistsSpawned;
            if (feeders == null || feeders.Count == 0)
            {
                return false;
            }

            bool desperate = pawn.needs.food.CurCategory == HungerCategory.Starving;
            for (int i = 0; i < feeders.Count; i++)
            {
                Pawn feeder = feeders[i];
                if (!IsPotentialPrisonerBabyFeeder(feeder, pawn))
                {
                    continue;
                }

                Job curJob = feeder.CurJob;
                if (curJob?.def == JobDefOf.FeedPatient && curJob.targetB.Thing == pawn)
                {
                    return true;
                }

                if (FoodUtility.TryFindBestFoodSourceFor(feeder, pawn, desperate, out Thing _, out ThingDef _, canRefillDispenser: false, canUseInventory: true, canUsePackAnimalInventory: false, allowForbidden: false, allowCorpse: false))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPotentialPrisonerBabyFeeder(Pawn feeder, Pawn baby)
        {
            if (feeder == null || baby == null || feeder == baby || feeder.Map != baby.Map || feeder.Dead || feeder.Downed || feeder.Drafted || feeder.InMentalState || !feeder.Awake())
            {
                return false;
            }

            if (!feeder.RaceProps.Humanlike)
            {
                return false;
            }

            if (!feeder.CanReserveAndReach(baby, PathEndMode.Touch, Danger.Some, 1, -1, null, ignoreOtherReservations: true))
            {
                return false;
            }

            bool childcareActive = IsWorkTypeActive(feeder, WorkTypeDefOf.Childcare);
            bool wardenActive = IsWorkTypeActive(feeder, WorkTypeDefOf.Warden);
            return childcareActive || wardenActive;
        }

        private static bool IsWorkTypeActive(Pawn pawn, WorkTypeDef workType)
        {
            return pawn?.workSettings != null && workType != null && !pawn.WorkTypeIsDisabled(workType) && pawn.workSettings.WorkIsActive(workType);
        }

        public static void SetPrisonerScavengeCrawlAnimation(Pawn pawn)
        {
            if (pawn?.Drawer?.renderer == null)
            {
                return;
            }

            AnimationDef animation = ResolveToddlerCrawlAnimation();
            if (animation != null)
            {
                pawn.Drawer.renderer.SetAnimation(animation);
            }
        }

        public static void ClearPrisonerScavengeCrawlAnimation(Pawn pawn)
        {
            if (pawn?.Drawer?.renderer == null)
            {
                return;
            }

            pawn.Drawer.renderer.SetAnimation(null);
        }

        private static AnimationDef ResolveToddlerCrawlAnimation()
        {
            if (toddlerCrawlAnimationResolved)
            {
                return toddlerCrawlAnimationDef;
            }

            toddlerCrawlAnimationResolved = true;
            toddlerCrawlAnimationDef = DefDatabase<AnimationDef>.GetNamedSilentFail("ToddlerCrawl");
            return toddlerCrawlAnimationDef;
        }

        public static bool TryStartPrisonerScavengeJob(Pawn pawn, bool logDecision)
        {
            if (pawn?.jobs == null)
            {
                if (logDecision)
                {
                    LogPrisonerScavenge(pawn, "skip: no job tracker");
                }

                return false;
            }

            if (pawn.CurJobDef == MouseDisasterDefOf.MouseDisaster_PrisonerScavenge)
            {
                if (logDecision)
                {
                    LogPrisonerScavenge(pawn, "skip: already scavenging");
                }

                return false;
            }

            if (!CanInterruptCurrentJobForScavenge(pawn, logDecision))
            {
                return false;
            }

            Job job = TryCreatePrisonerScavengeJob(pawn, logDecision);
            if (job == null)
            {
                return false;
            }

            bool accepted = pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            if (logDecision)
            {
                LogPrisonerScavenge(pawn, accepted ? "start: ordered scavenging job accepted" : "fail: ordered scavenging job rejected", force: true);
            }

            return accepted;
        }

        private static bool CanInterruptCurrentJobForScavenge(Pawn pawn, bool logDecision)
        {
            bool burstActive = IsPrisonerScavengeBurstActive(pawn);

            Job currentJob = pawn?.CurJob;
            if (currentJob == null || currentJob.def == null)
            {
                return true;
            }

            if (IsScavengeInterruptBlockedJob(currentJob.def))
            {
                if (logDecision)
                {
                    LogPrisonerScavenge(pawn, "skip: blocked by compatibility job=" + currentJob.def.defName);
                }

                return false;
            }

            if (currentJob.playerForced)
            {
                if (logDecision)
                {
                    LogPrisonerScavenge(pawn, "skip: current job is player-forced");
                }

                return false;
            }

            JobDef currentDef = currentJob.def;
            if (currentDef == JobDefOf.Wait || currentDef == JobDefOf.Wait_Wander || currentDef == JobDefOf.LayDown || currentDef == JobDefOf.GotoWander)
            {
                return true;
            }

            if (!burstActive && pawn.needs?.food != null && pawn.needs.food.CurCategory == HungerCategory.Starving)
            {
                return true;
            }

            if (logDecision)
            {
                LogPrisonerScavenge(pawn, burstActive
                    ? "skip: burst keeps current job=" + currentDef.defName
                    : "skip: keep current job=" + currentDef.defName);
            }

            return false;
        }

        private static bool IsScavengeInterruptBlockedJob(JobDef jobDef)
        {
            if (jobDef == null)
            {
                return false;
            }

            string defName = jobDef.defName ?? string.Empty;
            for (int i = 0; i < ScavengeInterruptBlockedJobKeywords.Length; i++)
            {
                if (defName.IndexOf(ScavengeInterruptBlockedJobKeywords[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            string driverClassName = jobDef.driverClass?.FullName;
            if (!driverClassName.NullOrEmpty())
            {
                for (int i = 0; i < ScavengeInterruptBlockedJobKeywords.Length; i++)
                {
                    if (driverClassName.IndexOf(ScavengeInterruptBlockedJobKeywords[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void LogPrisonerScavenge(Pawn pawn, string message, bool force = false)
        {
            if (!IsPrisonerScavengeDebugLogEnabled || pawn == null)
            {
                return;
            }

            if (!force && PrisonerScavengeLastLogMessageByPawnId.TryGetValue(pawn.thingIDNumber, out string lastMessage) && lastMessage == message)
            {
                return;
            }

            int nowTick = Find.TickManager?.TicksGame ?? 0;
            if (!force && PrisonerScavengeLastLogTickByPawnId.TryGetValue(pawn.thingIDNumber, out int lastTick) && nowTick - lastTick < PrisonerScavengeLogIntervalTicks)
            {
                return;
            }

            PrisonerScavengeLastLogTickByPawnId[pawn.thingIDNumber] = nowTick;
            PrisonerScavengeLastLogMessageByPawnId[pawn.thingIDNumber] = message;
            Log.Message("[MouseDisaster][PrisonerScavenge] t=" + nowTick + " pawn=" + pawn.LabelShortCap + "#" + pawn.thingIDNumber + " " + message);
        }

        public static BodyPartRecord GetNaturalRatTail(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return null;
            }

            return pawn.health.hediffSet.GetNotMissingParts().FirstOrDefault(part =>
                part.def?.defName == RatTailPartDefName && !pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(part));
        }

        public static bool HasActiveChildExchangeState()
        {
            return ActiveChildExchangeByTraderId.Count > 0;
        }

        public static bool HasActiveAbandonedDeliveryState()
        {
            return ActiveAbandonedDeliveryByAdultId.Count > 0;
        }

        public static List<BodyPartRecord> GetNaturalEars(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return new List<BodyPartRecord>();
            }

            return pawn.health.hediffSet.GetNotMissingParts()
                .Where(part => part.def?.defName == "Ear" && !pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(part))
                .ToList();
        }

        public static BodyPartRecord GetRandomNaturalEar(Pawn pawn)
        {
            return GetNaturalEars(pawn).RandomElementWithFallback();
        }

        public static BodyPartRecord GetPreferredNaturalEar(Pawn pawn)
        {
            List<BodyPartRecord> ears = GetNaturalEars(pawn);
            if (ears.Count == 0)
            {
                return null;
            }

            BodyPartRecord leftEar = ears.FirstOrDefault(record =>
                !record.Label.NullOrEmpty() &&
                (record.Label.IndexOf("left", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 record.Label.IndexOf("\u5de6", StringComparison.OrdinalIgnoreCase) >= 0));
            return leftEar ?? ears[0];
        }

        public static bool IsDefenselessTailTarget(Pawn pawn)
        {
            return pawn != null &&
                   (pawn.Downed ||
                    !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving) ||
                    !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) ||
                    !pawn.health.capacities.CanBeAwake);
        }

        public static bool CanBeTailBiteTarget(Pawn target, Pawn actor)
        {
            return target != null &&
                   target != actor &&
                   target.Spawned &&
                   !target.Dead &&
                   target.IsPrisonerOfColony &&
                   IsMouseEggBaby(target) &&
                   CanReceiveTailBiteNow(target) &&
                   GetNaturalRatTail(target) != null &&
                   (!target.Awake() || IsDefenselessTailTarget(target));
        }

        private static bool CanReceiveTailBiteNow(Pawn target)
        {
            if (target == null)
            {
                return false;
            }

            int nowTick = Find.TickManager?.TicksGame ?? 0;
            return !TailBiteLastVictimTickByPawnId.TryGetValue(target.thingIDNumber, out int lastTick) || nowTick - lastTick >= TailBiteVictimCooldownTicks;
        }

        public static Pawn FindTailBiteTarget(Pawn pawn)
        {
            return GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                PathEndMode.Touch,
                TraverseParms.For(pawn, Danger.Some),
                30f,
                thing => thing is Pawn target && CanBeTailBiteTarget(target, pawn) && pawn.CanReserve(thing)) as Pawn;
        }

        public static bool CanAttemptTailBite(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            int nowTick = Find.TickManager?.TicksGame ?? 0;
            return !TailBiteLastAttemptTickByPawnId.TryGetValue(pawn.thingIDNumber, out int lastTick) || nowTick - lastTick >= TailBiteAttemptCooldownTicks;
        }

        public static void RecordTailBiteAttempt(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            TailBiteLastAttemptTickByPawnId[pawn.thingIDNumber] = Find.TickManager?.TicksGame ?? 0;
        }

        public static void RecordTailBiteVictim(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            TailBiteLastVictimTickByPawnId[pawn.thingIDNumber] = Find.TickManager?.TicksGame ?? 0;
        }

        public static int TailBiteStartTextCooldown => TailBiteStartTextCooldownTicks;

        public static int TailBiteResultTextCooldown => TailBiteResultTextCooldownTicks;

        public static void NotifyTailBiteOffEvent(Pawn biter, Pawn target)
        {
            if (biter == null || target == null)
            {
                return;
            }

            int nowTick = Find.TickManager?.TicksGame ?? 0;
            if (TailBiteLastNotifyTickByPawnId.TryGetValue(target.thingIDNumber, out int lastNotifyTick) && nowTick - lastNotifyTick < TailBiteNotifyCooldownTicks)
            {
                return;
            }

            TailBiteLastNotifyTickByPawnId[target.thingIDNumber] = nowTick;

            string text = "MouseDisaster_TailBite_Event".Translate(target.Named("TARGET"), biter.Named("BITER")).Resolve();
            Messages.Message(text, target, MessageTypeDefOf.NegativeEvent, historical: true);
            if (IsPrisonerScavengeDebugLogEnabled)
            {
                Log.Message("[MouseDisaster][TailBite] " + text);
            }
        }

        public static void TryGainTailBittenHatredMemory(Pawn target, Pawn offender)
        {
            if (target?.needs?.mood?.thoughts?.memories == null || offender == null || MouseDisasterDefOf.MouseDisaster_TailBittenHatred == null)
            {
                return;
            }

            target.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(MouseDisasterDefOf.MouseDisaster_TailBittenHatred, offender);
            target.needs.mood.thoughts.memories.TryGainMemory(MouseDisasterDefOf.MouseDisaster_TailBittenHatred, offender);
        }

        public static Filth FindScavengeableFilth(Pawn pawn)
        {
            if (pawn?.Map == null || !pawn.Position.IsValid)
            {
                return null;
            }

            Filth found = GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.Filth),
                PathEndMode.Touch,
                TraverseParms.For(pawn, Danger.Some),
                40f,
                thing => thing is Filth filth && IsScavengeableFilth(filth) && pawn.CanReserve(thing)) as Filth;

            if (found != null)
            {
                return found;
            }

            if (pawn.needs?.food == null || pawn.needs.food.CurCategory < HungerCategory.Starving)
            {
                return null;
            }

            return TryCreateEmergencyScavengeFilth(pawn);
        }

        private static Filth TryCreateEmergencyScavengeFilth(Pawn pawn)
        {
            if (pawn?.Map == null || !pawn.Position.IsValid)
            {
                return null;
            }

            ThingDef fallbackFilthDef = MouseDisasterDefOf.Filth_MouseDisasterPoop ?? ThingDefOf.Filth_Dirt;
            if (fallbackFilthDef == null)
            {
                return null;
            }

            FilthMaker.TryMakeFilth(pawn.Position, pawn.Map, fallbackFilthDef, 1, FilthSourceFlags.None);
            Filth filth = pawn.Position.GetThingList(pawn.Map).OfType<Filth>().FirstOrDefault(item => item.def == fallbackFilthDef && pawn.CanReserve(item));
            if (IsPrisonerScavengeDebugLogEnabled && filth != null)
            {
                LogPrisonerScavenge(pawn, "fallback: spawned emergency filth target=" + filth.def.defName);
            }

            return filth;
        }

        public static bool IsScavengeableFilth(Filth filth)
        {
            return filth != null && filth.Spawned && filth.MapHeld != null && ResolvePrisonerScavengeProfile(filth) != null;
        }

        public static PrisonerScavengeProfile ResolvePrisonerScavengeProfile(Filth filth)
        {
            if (filth == null)
            {
                return BuildPrisonerScavengeProfile(MouseDisasterDefOf.MouseDisaster_ScavengedDirtyFilth, PrisonerScavengeDirtyTextKeys);
            }

            string source = BuildFilthMatchSource(filth);
            if (ContainsAnyKeyword(source, PrisonerScavengeVomitKeywords))
            {
                return BuildPrisonerScavengeProfile(MouseDisasterDefOf.MouseDisaster_ScavengedVomit, PrisonerScavengeVomitTextKeys);
            }

            if (ContainsAnyKeyword(source, PrisonerScavengeAmnioticKeywords))
            {
                return BuildPrisonerScavengeProfile(MouseDisasterDefOf.MouseDisaster_ScavengedAmnioticFluid, PrisonerScavengeAmnioticTextKeys);
            }

            if (ContainsAnyKeyword(source, PrisonerScavengeBloodKeywords))
            {
                return BuildPrisonerScavengeProfile(MouseDisasterDefOf.MouseDisaster_ScavengedBlood, PrisonerScavengeBloodTextKeys);
            }

            if (ContainsAnyKeyword(source, PrisonerScavengeFloorDustKeywords))
            {
                return BuildPrisonerScavengeProfile(MouseDisasterDefOf.MouseDisaster_ScavengedFloorDust, PrisonerScavengeFloorDustTextKeys);
            }

            return BuildPrisonerScavengeProfile(MouseDisasterDefOf.MouseDisaster_ScavengedDirtyFilth, PrisonerScavengeDirtyTextKeys);
        }

        public static string RandomPrisonerScavengePoisonedText()
        {
            return RandomText(PrisonerScavengePoisonedTextKeys);
        }

        private static PrisonerScavengeProfile BuildPrisonerScavengeProfile(ThoughtDef thoughtDef, string[] textKeys)
        {
            ThoughtDef resolvedThought = thoughtDef ?? MouseDisasterDefOf.MouseDisaster_ScavengedDirtyFilth;
            string[] resolvedText = textKeys ?? PrisonerScavengeDirtyTextKeys;
            return new PrisonerScavengeProfile(resolvedThought, resolvedText, 0.1f);
        }

        private static string BuildFilthMatchSource(Filth filth)
        {
            if (filth?.def == null)
            {
                return string.Empty;
            }

            string defName = filth.def.defName ?? string.Empty;
            string label = filth.def.label ?? string.Empty;
            return (defName + " " + label).ToLowerInvariant();
        }

        private static bool ContainsAnyKeyword(string source, string[] keywords)
        {
            if (source.NullOrEmpty() || keywords == null || keywords.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < keywords.Length; i++)
            {
                if (source.Contains(keywords[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public static void TrySpawnSleepPoopFilth(Map map)
        {
            if (map == null || MouseDisasterDefOf.Filth_MouseDisasterPoop == null)
            {
                return;
            }

            List<Pawn> pawns = GetCachedMouseDisasterPawns(map);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.Dead || pawn.Downed || pawn.Awake() || pawn.CurJobDef != JobDefOf.LayDown)
                {
                    continue;
                }

                if (Rand.Chance(SleepPoopSpawnChance))
                {
                    FilthMaker.TryMakeFilth(pawn.PositionHeld, map, MouseDisasterDefOf.Filth_MouseDisasterPoop, 1, FilthSourceFlags.Pawn);
                }
            }
        }

        public static Thing FindGnawableTree(Pawn pawn)
        {
            return GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.Plant),
                PathEndMode.Touch,
                TraverseParms.For(pawn, Danger.Some),
                9999f,
                thing => thing is Plant plant && plant.def.plant != null && plant.def.plant.IsTree && pawn.CanReserve(thing));
        }

        public static Thing FindGnawableWall(Pawn pawn)
        {
            return GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial),
                PathEndMode.Touch,
                TraverseParms.For(pawn, Danger.Some),
                9999f,
                thing => thing.def.building != null && !thing.def.building.isNaturalRock && thing.def.Fillage == FillCategory.Full && thing.def.passability == Traversability.Impassable && !thing.def.IsDoor && !thing.def.IsFrame && pawn.CanReserve(thing));
        }

        public static void AddNutrition(Pawn pawn, float percentage)
        {
            if (pawn?.needs?.food == null)
            {
                return;
            }

            pawn.needs.food.CurLevelPercentage = Mathf.Clamp01(pawn.needs.food.CurLevelPercentage + percentage);
        }

        public static bool TryApplyPrisonerScavengePoison(Pawn pawn)
        {
            if (pawn?.health == null)
            {
                return false;
            }

            float amount = MouseDisasterMod.Settings?.GetPrisonerScavengeToxicBuildupPerEat() ?? MouseDisasterSettings.ScavengeToxicBuildupNormal;
            if (amount <= 0f)
            {
                return false;
            }

            Hediff toxic = pawn.health.GetOrAddHediff(HediffDefOf.ToxicBuildup);
            if (toxic == null)
            {
                return false;
            }

            toxic.Severity += amount;
            return true;
        }

        public static void ApplyPrisonerScavengeMood(Pawn pawn, ThoughtDef thoughtDef)
        {
            if (pawn?.needs?.mood?.thoughts?.memories == null)
            {
                return;
            }

            ThoughtDef resolved = thoughtDef ?? MouseDisasterDefOf.MouseDisaster_ScavengedDirtyFilth;
            if (resolved == null)
            {
                return;
            }

            pawn.needs.mood.thoughts.memories.TryGainMemory(resolved);
        }

        public static string RandomText(string[] keys, params object[] args)
        {
            if (keys == null || keys.Length == 0)
            {
                return string.Empty;
            }

            string key = keys.RandomElement();
            string translated = key.TranslateSimple();
            if (args == null || args.Length == 0)
            {
                return translated;
            }

            return string.Format(translated, args);
        }

        public static void TryThrowText(Map map, Vector3 drawPos, string text, float size = 3f)
        {
            if (!IsFloatingTextEnabled || map == null || text.NullOrEmpty())
            {
                return;
            }

            MoteMaker.ThrowText(drawPos, map, text, size);
        }

        public static bool TryThrowTextThrottled(Map map, Vector3 drawPos, string text, string throttleKey, int minIntervalTicks, float size = 3f)
        {
            if (!IsFloatingTextEnabled || map == null || text.NullOrEmpty())
            {
                return false;
            }

            if (!throttleKey.NullOrEmpty())
            {
                int nowTick = Find.TickManager?.TicksGame ?? 0;
                if (FloatingTextLastTickByKey.TryGetValue(throttleKey, out int lastTick) && nowTick - lastTick < Mathf.Max(1, minIntervalTicks))
                {
                    return false;
                }

                FloatingTextLastTickByKey[throttleKey] = nowTick;
            }

            MoteMaker.ThrowText(drawPos, map, text, size);
            return true;
        }

        public static bool TrySpawnRatEggTailDrop(Pawn sourcePawn)
        {
            return TrySpawnOptionalDrop(sourcePawn, ref ratEggTailThingDef, ref ratEggTailResolved, "RatEgg_Tail", "MouseDisaster_Drop_Tail");
        }

        public static void ClampFoodToMax(Pawn pawn)
        {
            if (pawn?.needs?.food == null)
            {
                return;
            }

            pawn.needs.food.CurLevel = Mathf.Min(pawn.needs.food.CurLevel, pawn.needs.food.MaxLevel);
        }

        public static void AddOrRefreshHediff(Pawn pawn, HediffDef hediffDef, float minimumSeverity = 0.2f)
        {
            if (pawn == null || hediffDef == null)
            {
                return;
            }

            Hediff hediff = pawn.health.GetOrAddHediff(hediffDef);
            if (hediff.Severity < minimumSeverity)
            {
                hediff.Severity = minimumSeverity;
            }

            HediffComp_Disappears disappears = hediff.TryGetComp<HediffComp_Disappears>();
            disappears?.ResetElapsedTicks();
        }

        public static void RemoveHediffByDef(Pawn pawn, HediffDef hediffDef)
        {
            if (pawn?.health?.hediffSet == null || hediffDef == null)
            {
                return;
            }

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (hediff != null)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }

        private static bool TrySpawnOptionalDrop(Pawn sourcePawn, ref ThingDef cachedDef, ref bool resolved, string defName, string textKey)
        {
            if (sourcePawn?.MapHeld == null)
            {
                return false;
            }

            if (!resolved)
            {
                cachedDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                resolved = true;
            }

            if (cachedDef == null)
            {
                return false;
            }

            Thing drop = ThingMaker.MakeThing(cachedDef);
            drop.stackCount = 1;
            GenPlace.TryPlaceThing(drop, sourcePawn.PositionHeld, sourcePawn.MapHeld, ThingPlaceMode.Near);
            if (sourcePawn.Spawned && !textKey.NullOrEmpty())
            {
                string text = textKey.Translate(drop.LabelCapNoCount).Resolve();
                TryThrowText(sourcePawn.MapHeld, sourcePawn.DrawPos, text, 3f);
                Messages.Message(text, sourcePawn, MessageTypeDefOf.NeutralEvent, historical: false);
            }
            return true;
        }

        public static Job TryCreateImproperFoodJob(Pawn pawn, bool allowInventorySearch)
        {
            if (pawn?.needs?.food == null)
            {
                return null;
            }

            Job reliefFoodJob = TryCreateReliefFoodJob(pawn, allowInventorySearch);
            if (reliefFoodJob != null)
            {
                return reliefFoodJob;
            }

            bool desperate = pawn.needs.food.CurCategory == HungerCategory.Starving;
            suppressReliefAreaPostfix = true;
            bool foodFound = false;
            Thing foodSource = null;
            ThingDef foodDef = null;
            try
            {
                foodFound = FoodUtility.TryFindBestFoodSourceFor(
                    pawn,
                    pawn,
                    desperate,
                    out foodSource,
                    out foodDef,
                    canRefillDispenser: true,
                    canUseInventory: true,
                    canUsePackAnimalInventory: false,
                    allowForbidden: false,
                    allowCorpse: true,
                    allowSociallyImproper: true,
                    allowHarvest: false,
                    forceScanWholeMap: true,
                    ignoreReservations: false,
                    calculateWantedStackCount: false,
                    allowVenerated: false);
            }
            finally
            {
                suppressReliefAreaPostfix = false;
            }

            if (!foodFound)
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
                takeOtherInventory.count = Mathf.Max(1, FoodUtility.WillIngestStackCountOf(pawn, foodDef, nutrition));
                return takeOtherInventory;
            }

            if (allowInventorySearch && foodSource.Spawned && pawn.CanReserveAndReach(foodSource, PathEndMode.ClosestTouch, Danger.Some))
            {
                Job takeInventory = JobMaker.MakeJob(JobDefOf.TakeInventory, foodSource);
                takeInventory.count = 1;
                takeInventory.takeInventoryDelay = 60;
                return takeInventory;
            }

            Job ingest = JobMaker.MakeJob(JobDefOf.Ingest, foodSource);
            ingest.count = Mathf.Max(1, FoodUtility.WillIngestStackCountOf(pawn, foodDef, nutrition));
            return ingest;
        }

        public static bool TryFindStoredFoodCell(Map map, out IntVec3 cell)
        {
            if (TryFindDiningRoomCell(map, out cell))
            {
                return true;
            }

            Thing foodThing = map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree)
                .Where(thing => thing.Spawned && thing.def.IsNutritionGivingIngestible)
                .OrderBy(thing => thing.Position.DistanceToSquared(map.Center))
                .FirstOrDefault();

            if (foodThing != null)
            {
                cell = foodThing.Position;
                return true;
            }

            cell = map.Center;
            return false;
        }

        public static bool TryFindAbandonedDeliveryFoodCell(Map map, IntVec3 entryCell, out IntVec3 cell)
        {
            bool foundReliefFood = TryFindNearestReliefAreaFoodCellToEvent(map, entryCell, out cell);
            if (foundReliefFood)
            {
                return true;
            }

            bool foundHomeAreaFood = TryFindNearestFoodCellToEvent(map, entryCell, requireHomeArea: true, out cell);
            if (foundHomeAreaFood)
            {
                return true;
            }

            bool foundAnyFoodThing = map?.listerThings != null &&
                                     map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSource)
                                         .Any(thing => IsAreaFoodSourceThing(thing, allowHarvest: true));
            if (MouseDisasterAbandonedDeliveryPolicy.ShouldTryAnyFoodFallback(foundHomeAreaFood, foundAnyFoodThing) &&
                TryFindNearestFoodCellToEvent(map, entryCell, requireHomeArea: false, out cell))
            {
                return true;
            }

            if (MouseDisasterAbandonedDeliveryPolicy.ShouldFallbackToStoredFoodCell(foundHomeAreaFood, foundAnyFoodThing))
            {
                return TryFindStoredFoodCell(map, out cell);
            }

            cell = IntVec3.Invalid;
            return false;
        }

        private static bool TryFindNearestReliefAreaFoodCellToEvent(Map map, IntVec3 entryCell, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            Area_MouseDisasterRelief reliefArea = GetReliefArea(map);
            if (map?.listerThings == null || reliefArea == null)
            {
                return false;
            }

            List<Thing> foodThings = map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSource)
                .Where(thing => thing != null &&
                                thing.Spawned &&
                                reliefArea[thing.PositionHeld] &&
                                IsAreaFoodSourceThing(thing, allowHarvest: true))
                .OrderBy(thing => entryCell.IsValid ? entryCell.DistanceToSquared(thing.PositionHeld) : 0f)
                .ToList();

            for (int i = 0; i < foodThings.Count; i++)
            {
                Thing foodThing = foodThings[i];
                if (TryFindReachableStandCellNear(map, entryCell, foodThing.PositionHeld, requireHomeArea: false, out cell))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindNearestFoodCellToEvent(Map map, IntVec3 entryCell, bool requireHomeArea, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (map?.listerThings == null || (requireHomeArea && map.areaManager?.Home == null))
            {
                return false;
            }

            List<Thing> foodThings = map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSource)
                .Where(thing => thing != null &&
                                thing.Spawned &&
                                (!requireHomeArea || map.areaManager.Home[thing.Position]) &&
                                IsAreaFoodSourceThing(thing, allowHarvest: true))
                .OrderBy(thing => entryCell.IsValid ? entryCell.DistanceToSquared(thing.Position) : 0f)
                .ToList();

            for (int i = 0; i < foodThings.Count; i++)
            {
                Thing foodThing = foodThings[i];
                if (TryFindReachableStandCellNear(map, entryCell, foodThing.Position, requireHomeArea, out cell))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryFindBestReliefFoodSourceFor(
            Pawn getter,
            Pawn eater,
            bool desperate,
            bool allowHarvest,
            out Thing foodSource,
            out ThingDef foodDef,
            bool allowForbidden,
            bool allowCorpse,
            bool allowSociallyImproper,
            bool ignoreReservations,
            bool calculateWantedStackCount,
            bool allowVenerated,
            FoodPreferability minPrefOverride)
        {
            return TryFindBestAreaFilteredFoodSourceFor(
                getter,
                eater,
                desperate,
                allowHarvest,
                onlyReliefAreaFood: true,
                out foodSource,
                out foodDef,
                allowForbidden,
                allowCorpse,
                allowSociallyImproper,
                ignoreReservations,
                calculateWantedStackCount,
                allowVenerated,
                minPrefOverride);
        }

        public static bool TryFindBestNonReliefFoodSourceFor(
            Pawn getter,
            Pawn eater,
            bool desperate,
            bool allowHarvest,
            out Thing foodSource,
            out ThingDef foodDef,
            bool allowForbidden,
            bool allowCorpse,
            bool allowSociallyImproper,
            bool ignoreReservations,
            bool calculateWantedStackCount,
            bool allowVenerated,
            FoodPreferability minPrefOverride)
        {
            return TryFindBestAreaFilteredFoodSourceFor(
                getter,
                eater,
                desperate,
                allowHarvest,
                onlyReliefAreaFood: false,
                out foodSource,
                out foodDef,
                allowForbidden,
                allowCorpse,
                allowSociallyImproper,
                ignoreReservations,
                calculateWantedStackCount,
                allowVenerated,
                minPrefOverride);
        }

        private static bool TryFindBestAreaFilteredFoodSourceFor(
            Pawn getter,
            Pawn eater,
            bool desperate,
            bool allowHarvest,
            bool onlyReliefAreaFood,
            out Thing foodSource,
            out ThingDef foodDef,
            bool allowForbidden,
            bool allowCorpse,
            bool allowSociallyImproper,
            bool ignoreReservations,
            bool calculateWantedStackCount,
            bool allowVenerated,
            FoodPreferability minPrefOverride)
        {
            foodSource = null;
            foodDef = null;
            if (getter?.Map == null || eater == null)
            {
                return false;
            }

            Area_MouseDisasterRelief reliefArea = GetReliefArea(getter.Map);
            if (reliefArea == null)
            {
                return false;
            }

            List<Thing> foodThings = getter.Map.listerThings?.ThingsInGroup(ThingRequestGroup.FoodSource);
            if (foodThings == null)
            {
                return false;
            }

            FoodPreferability minPref = ResolveReliefFoodMinPreferability(eater, desperate, minPrefOverride);
            Thing bestThing = null;
            ThingDef bestFoodDef = null;
            float bestScore = float.MinValue;
            for (int i = 0; i < foodThings.Count; i++)
            {
                Thing candidate = foodThings[i];
                if (candidate == null || !candidate.Spawned)
                {
                    continue;
                }

                bool isReliefFood = reliefArea[candidate.PositionHeld];
                if (onlyReliefAreaFood != isReliefFood)
                {
                    continue;
                }

                bool isHarvestablePlant = allowHarvest &&
                                          candidate is Plant plant &&
                                          plant.HarvestableNow &&
                                          plant.def?.plant?.harvestedThingDef?.IsNutritionGivingIngestible == true;
                bool isCorpse = candidate is Corpse;
                if (!MouseDisasterReliefAreaPolicy.ShouldAllowFoodTypeInReliefAreaSearch(
                        candidate.def.IsNutritionGivingIngestible,
                        candidate.IngestibleNow,
                        isHarvestablePlant,
                        isCorpse,
                        allowCorpse))
                {
                    continue;
                }

                ThingDef candidateFoodDef = isHarvestablePlant ? candidate.def.plant.harvestedThingDef : candidate.def;
                if ((int)candidateFoodDef.ingestible.preferability < (int)minPref)
                {
                    continue;
                }

                if (!allowForbidden && candidate.IsForbidden(getter))
                {
                    continue;
                }

                if (!desperate && candidate.IsNotFresh())
                {
                    continue;
                }

                if (candidate.IsDessicated())
                {
                    continue;
                }

                if (!allowSociallyImproper)
                {
                    bool animalsCare = !getter.IsAnimal;
                    if (!isHarvestablePlant &&
                        !candidate.IsSociallyProper(getter) &&
                        !candidate.IsSociallyProper(eater, eater.IsPrisonerOfColony, animalsCare))
                    {
                        continue;
                    }
                }

                if (isHarvestablePlant)
                {
                    if (!eater.WillEat(candidateFoodDef, getter, careIfNotAcceptableForTitle: true, allowVenerated))
                    {
                        continue;
                    }
                }
                else if (!eater.WillEat(candidate, getter, careIfNotAcceptableForTitle: true, allowVenerated))
                {
                    continue;
                }

                float nutrition = FoodUtility.GetNutrition(eater, candidate, candidateFoodDef);
                int wantedStackCount = calculateWantedStackCount
                    ? Mathf.Max(1, FoodUtility.WillIngestStackCountOf(eater, candidateFoodDef, nutrition))
                    : 1;
                if (!ignoreReservations && !getter.CanReserve(candidate, 10, wantedStackCount))
                {
                    continue;
                }

                if (!getter.CanReach(candidate, PathEndMode.ClosestTouch, Danger.Some))
                {
                    continue;
                }

                float score = FoodUtility.FoodOptimality(eater, candidate, candidateFoodDef, (getter.Position - candidate.PositionHeld).LengthManhattan);
                if (score <= bestScore)
                {
                    continue;
                }

                bestThing = candidate;
                bestFoodDef = candidateFoodDef;
                bestScore = score;
            }

            if (bestThing == null || bestFoodDef == null)
            {
                return false;
            }

            foodSource = bestThing;
            foodDef = bestFoodDef;
            return true;
        }

        private static FoodPreferability ResolveReliefFoodMinPreferability(Pawn eater, bool desperate, FoodPreferability minPrefOverride)
        {
            if (minPrefOverride != FoodPreferability.Undefined)
            {
                return minPrefOverride;
            }

            bool eaterNonHumanlikeOrWildMan = eater.NonHumanlikeOrWildMan();
            bool eaterDoesNotMindRawFood = eater.genes != null && eater.genes.DontMindRawFood;
            if (MouseDisasterReliefAreaPolicy.ShouldAllowRawFoodAsMinimumPreferability(desperate, eaterNonHumanlikeOrWildMan, eaterDoesNotMindRawFood))
            {
                return FoodPreferability.NeverForNutrition;
            }

            return FoodPreferability.RawBad;
        }

        private static bool TryFindReachableStandCellNear(Map map, IntVec3 startCell, IntVec3 targetCell, bool requireHomeArea, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (map == null || !targetCell.IsValid || !targetCell.InBounds(map) || (requireHomeArea && map.areaManager?.Home == null))
            {
                return false;
            }

            TraverseParms traverseParms = TraverseParms.For(TraverseMode.PassDoors);
            if (targetCell.Standable(map) &&
                (!requireHomeArea || map.areaManager.Home[targetCell]) &&
                (!startCell.IsValid || map.reachability.CanReach(startCell, targetCell, PathEndMode.OnCell, traverseParms)))
            {
                cell = targetCell;
                return true;
            }

            IEnumerable<IntVec3> candidates = CellRect.CenteredOn(targetCell, 3)
                .Cells
                .Where(candidate => candidate.InBounds(map) &&
                                    candidate.Standable(map) &&
                                    (!requireHomeArea || map.areaManager.Home[candidate]))
                .OrderBy(candidate => candidate.DistanceToSquared(targetCell));
            foreach (IntVec3 candidate in candidates)
            {
                if (!startCell.IsValid || map.reachability.CanReach(startCell, candidate, PathEndMode.OnCell, traverseParms))
                {
                    cell = candidate;
                    return true;
                }
            }

            return false;
        }

        public static bool TryFindDiningRoomCell(Map map, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (map?.regionGrid?.AllRooms == null)
            {
                return false;
            }

            Room diningRoom = map.regionGrid.AllRooms
                .Where(room => room != null &&
                               !room.PsychologicallyOutdoors &&
                               room.Role != null &&
                               room.Role.workerClass != null &&
                               room.Role.workerClass.Name.IndexOf("DiningRoom", StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderByDescending(room => room.CellCount)
                .FirstOrDefault();
            if (diningRoom == null)
            {
                return false;
            }

            IEnumerable<IntVec3> candidates = diningRoom.Cells.Where(c => c.Standable(map));
            if (!candidates.Any())
            {
                return false;
            }

            cell = candidates.RandomElement();
            return true;
        }

        public static Job CreateGotoJob(IntVec3 cell, bool exitMapOnArrival = false)
        {
            Job job = JobMaker.MakeJob(JobDefOf.Goto, cell);
            job.locomotionUrgency = LocomotionUrgency.Jog;
            job.exitMapOnArrival = exitMapOnArrival;
            return job;
        }

        public static bool TryFindFarEdgeCell(Map map, IntVec3 awayFrom, out IntVec3 cell)
        {
            return CellFinder.TryFindRandomEdgeCellWith(
                candidate => candidate.Walkable(map) && map.reachability.CanReach(awayFrom, candidate, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors)) && (candidate - awayFrom).LengthHorizontalSquared > 400,
                map,
                CellFinder.EdgeRoadChance_Ignore,
                out cell);
        }

        public static void MakeTravelAndExitLord(Map map, IEnumerable<Pawn> pawns, IntVec3 travelDest, bool includeBabiesInExit = true)
        {
            List<Pawn> pawnList = pawns?.Where(pawn => pawn != null && pawn.Spawned).ToList();
            if (pawnList == null || pawnList.Count == 0)
            {
                return;
            }

            if (!includeBabiesInExit)
            {
                for (int i = 0; i < pawnList.Count; i++)
                {
                    if (pawnList[i].DevelopmentalStage == DevelopmentalStage.Baby)
                    {
                        PrepareNonCaravanBabyPawn(pawnList[i], travelDest);
                    }
                }

                pawnList = pawnList.Where(pawn => pawn.DevelopmentalStage != DevelopmentalStage.Baby).ToList();
                if (pawnList.Count == 0)
                {
                    return;
                }
            }

            Faction faction = pawnList
                .Select(pawn => pawn.Faction)
                .FirstOrDefault(currentFaction => currentFaction != null);
            Lord lord = LordMaker.MakeNewLord(faction, new LordJob_TravelAndExit(travelDest), map, pawnList);
            if (lord != null)
            {
                TryAssignLeadYourPetTravelMouseEggs(lord);
            }
        }

        private static Dictionary<int, Pawn> BuildSpawnedPawnLookup(IReadOnlyList<Pawn> pawns)
        {
            ReusablePawnLookup.Clear();
            if (pawns == null)
            {
                return ReusablePawnLookup;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn != null)
                {
                    ReusablePawnLookup[pawn.thingIDNumber] = pawn;
                }
            }

            return ReusablePawnLookup;
        }

        private static Pawn ResolvePawnById(Dictionary<int, Pawn> pawnLookup, int pawnId)
        {
            if (pawnLookup != null && pawnLookup.TryGetValue(pawnId, out Pawn pawn))
            {
                return pawn;
            }

            return null;
        }

        public static void RemoveBodyPartByName(Pawn pawn, string defName, string labelContains = null)
        {
            if (pawn == null)
            {
                return;
            }

            BodyPartRecord part = pawn.health.hediffSet.GetNotMissingParts()
                .FirstOrDefault(record => record.def.defName == defName && (labelContains == null || record.Label.ToLowerInvariant().Contains(labelContains.ToLowerInvariant())));

            if (part == null)
            {
                part = pawn.health.hediffSet.GetNotMissingParts().FirstOrDefault(record => record.def.defName == defName);
            }

            if (part != null)
            {
                pawn.health.AddHediff(HediffDefOf.MissingBodyPart, part);
            }
        }
    }
}

