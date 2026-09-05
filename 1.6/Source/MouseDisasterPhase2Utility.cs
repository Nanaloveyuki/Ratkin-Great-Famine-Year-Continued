using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace MouseDisaster
{
    public enum MouseDisasterRequestKind
    {
        SimpleMeal,
        FineMeal,
        Medicine,
        Silver,
        PrisonerOrSlaveBaby,
        HerbalMedicine
    }

    public enum MouseDisasterIntelSiteKind
    {
        Treasure,
        StructureCluster,
        SmallSettlement
    }

    public static class MouseDisasterPhase2Utility
    {
        private const float PlagueStartSeverityMin = 0f;
        private const float PlagueStartSeverityMax = 0.1f;
        private const int AidRequestVisitDurationTicks = 60000;

        private sealed class AidRequestState
        {
            public int mapId;
            public int targetPawnId;
            public List<int> pawnIds;
            public MouseDisasterRequestKind requestKind;
            public int amount;
            public bool createsIntelSite;
            public MouseDisasterIntelSiteKind intelSiteKind;
            public int expireTick;
        }

        private static readonly Dictionary<int, AidRequestState> ActiveAidRequestsByTargetPawnId = new Dictionary<int, AidRequestState>();

        public static float GetCurrentColonyWealth(Map map)
        {
            return map?.wealthWatcher?.WealthTotal ?? 0f;
        }

        public static int CalculateAidAmount(Map map, MouseDisasterRequestKind kind)
        {
            float wealth = GetCurrentColonyWealth(map);
            switch (kind)
            {
                case MouseDisasterRequestKind.SimpleMeal:
                    return Mathf.Clamp(6 + Mathf.RoundToInt(wealth / 12000f) + Rand.RangeInclusive(0, 4), 6, 28);
                case MouseDisasterRequestKind.FineMeal:
                    return Mathf.Clamp(4 + Mathf.RoundToInt(wealth / 18000f) + Rand.RangeInclusive(0, 3), 4, 18);
                case MouseDisasterRequestKind.Medicine:
                    return Mathf.Clamp(2 + Mathf.RoundToInt(wealth / 25000f) + Rand.RangeInclusive(0, 2), 2, 10);
                case MouseDisasterRequestKind.Silver:
                    return Mathf.Clamp(80 + Mathf.RoundToInt(wealth / 40f) + Rand.RangeInclusive(0, 120), 80, 1200);
                case MouseDisasterRequestKind.HerbalMedicine:
                    return Mathf.Clamp(3 + Mathf.RoundToInt(wealth / 22000f) + Rand.RangeInclusive(0, 3), 3, 15);
                case MouseDisasterRequestKind.PrisonerOrSlaveBaby:
                    return 1;
                default:
                    return 1;
            }
        }

        public static bool SupportsVisitorDelivery(MouseDisasterRequestKind kind)
        {
            return ResolveRequestedThingDef(kind) != null;
        }

        public static bool HasActiveAidRequestVisitors()
        {
            return ActiveAidRequestsByTargetPawnId.Count > 0;
        }

        public static ThingDef ResolveRequestedThingDef(MouseDisasterRequestKind kind)
        {
            switch (kind)
            {
                case MouseDisasterRequestKind.SimpleMeal:
                    return ThingDefOf.MealSimple;
                case MouseDisasterRequestKind.FineMeal:
                    return ThingDefOf.MealFine;
                case MouseDisasterRequestKind.Medicine:
                    return ThingDefOf.MedicineIndustrial;
                case MouseDisasterRequestKind.HerbalMedicine:
                    return ThingDefOf.MedicineHerbal;
                case MouseDisasterRequestKind.Silver:
                    return ThingDefOf.Silver;
                default:
                    return null;
            }
        }

        public static string BuildVisitorDeliveryLetterText(IncidentDef incidentDef, Pawn target, MouseDisasterRequestKind kind, int amount, bool createsIntelSite)
        {
            ThingDef requestedThingDef = ResolveRequestedThingDef(kind);
            string baseText = incidentDef?.letterText ?? string.Empty;
            if (target == null || requestedThingDef == null)
            {
                return baseText;
            }

            string interactionText = "\n\n选中殖民者，右键 " + target.LabelShortCap + "，交付 " + amount + "x " + requestedThingDef.LabelCap + "。";
            string followupText = createsIntelSite
                ? "\n交付完成后，对方会留下对应情报并离开。"
                : "\n交付完成后，对方会带着物资离开。";
            string timeoutText = "\n若长时间不处理，对方会像原版乞丐一样自行离开。";
            return baseText + interactionText + followupText + timeoutText;
        }

        public static bool TrySpawnAidRequestVisitors(Map map, MouseDisasterRequestKind kind, int amount, bool createsIntelSite, MouseDisasterIntelSiteKind intelSiteKind, out Pawn targetPawn, out List<Pawn> allPawns, out string failureReason)
        {
            targetPawn = null;
            allPawns = new List<Pawn>();
            failureReason = string.Empty;

            ThingDef requestedThingDef = ResolveRequestedThingDef(kind);
            if (map == null || requestedThingDef == null)
            {
                failureReason = "无法生成援助来客。";
                return false;
            }

            if (!MouseDisasterUtility.TryFindEntryCell(map, out IntVec3 entryCell) || !MouseDisasterUtility.TryFindFormerFaction(out Faction faction))
            {
                failureReason = "找不到合适的来访位置。";
                return false;
            }

            MouseDisasterUtility.MakeFactionNeutralToPlayer(faction, force: true);
            MouseDisasterUtility.EnsureMouseDisasterFactionNeutralOnMap(map, faction);

            int adultCount = Mathf.Clamp(1 + Mathf.RoundToInt(map.mapPawns.FreeColonistsSpawnedCount * 0.15f), 1, 3);
            int childCount = Find.Storyteller.difficulty.ChildrenAllowed ? Mathf.Clamp(Mathf.RoundToInt(adultCount * 0.5f), 0, 2) : 0;

            targetPawn = MouseDisasterUtility.GenerateFactionRatkinPawn(MouseDisasterDefOf.MouseDisaster_BeggarRatkinAdult, faction, DevelopmentalStage.Adult, 0.3f);
            if (targetPawn == null)
            {
                failureReason = "鼠灾难民未能生成。";
                return false;
            }

            GenSpawn.Spawn(targetPawn, CellFinder.RandomClosewalkCellNear(entryCell, map, 5), map);
            PrepareAidVisitorPawn(targetPawn, requestedThingDef);
            allPawns.Add(targetPawn);

            for (int i = 1; i < adultCount; i++)
            {
                Pawn adult = MouseDisasterUtility.GenerateFactionRatkinPawn(MouseDisasterDefOf.MouseDisaster_BeggarRatkinAdult, faction, DevelopmentalStage.Adult, 0.32f);
                if (adult == null)
                {
                    continue;
                }

                GenSpawn.Spawn(adult, CellFinder.RandomClosewalkCellNear(entryCell, map, 6), map);
                PrepareAidVisitorPawn(adult, requestedThingDef);
                allPawns.Add(adult);
            }

            List<Pawn> children = new List<Pawn>();
            for (int i = 0; i < childCount; i++)
            {
                Pawn child = MouseDisasterUtility.GenerateFactionRatkinPawn(MouseDisasterDefOf.MouseDisaster_BeggarRatkinChild, faction, DevelopmentalStage.Child, 0.34f);
                if (child == null)
                {
                    continue;
                }

                GenSpawn.Spawn(child, CellFinder.RandomClosewalkCellNear(entryCell, map, 6), map);
                PrepareAidVisitorPawn(child, requestedThingDef);
                children.Add(child);
                allPawns.Add(child);
            }

            if (children.Count > 0)
            {
                MouseDisasterUtility.LinkIncidentParentToChildren(targetPawn, children);
            }

            if (!RCellFinder.TryFindRandomSpotJustOutsideColony(targetPawn, out IntVec3 idleSpot))
            {
                idleSpot = map.Center;
            }

            LordMaker.MakeNewLord(faction, new LordJob_BegForItems(faction, idleSpot, targetPawn, requestedThingDef, amount), map, allPawns);
            RegisterAidRequest(targetPawn, allPawns, kind, amount, createsIntelSite, intelSiteKind);
            return true;
        }

        public static void ProcessAidRequestVisitors(Map map)
        {
            if (map == null || ActiveAidRequestsByTargetPawnId.Count == 0)
            {
                return;
            }

            int nowTick = Find.TickManager?.TicksGame ?? 0;
            Dictionary<int, Pawn> pawnLookup = BuildSpawnedPawnLookup(map.mapPawns?.AllPawnsSpawned);
            List<int> targetIds = ActiveAidRequestsByTargetPawnId.Keys.ToList();
            for (int i = 0; i < targetIds.Count; i++)
            {
                int targetId = targetIds[i];
                if (!ActiveAidRequestsByTargetPawnId.TryGetValue(targetId, out AidRequestState state) || state == null || state.mapId != map.uniqueID)
                {
                    continue;
                }

                if (!pawnLookup.TryGetValue(targetId, out Pawn targetPawn) || targetPawn == null || targetPawn.Dead || !targetPawn.Spawned)
                {
                    ActiveAidRequestsByTargetPawnId.Remove(targetId);
                    continue;
                }

                ThingDef requestedThingDef = ResolveRequestedThingDef(state.requestKind);
                if (requestedThingDef == null)
                {
                    ActiveAidRequestsByTargetPawnId.Remove(targetId);
                    continue;
                }

                if (GiveItemsToPawnUtility.GetCountRemaining(targetPawn, requestedThingDef, state.amount) <= 0)
                {
                    NotifyAidRequestCompleted(map, state, targetPawn);
                    ActiveAidRequestsByTargetPawnId.Remove(targetId);
                    continue;
                }

                if (nowTick < state.expireTick)
                {
                    continue;
                }

                List<Pawn> pawns = ResolveAidRequestPawns(state, pawnLookup);
                if (pawns.Count > 0)
                {
                    if (!MouseDisasterUtility.TryFindFarEdgeCell(map, targetPawn.Position, out IntVec3 exitCell))
                    {
                        exitCell = map.Center;
                    }

                    MouseDisasterUtility.MakeTravelAndExitLord(map, pawns, exitCell);
                }

                Messages.Message("鼠灾来客久候未果，已经离开了。", targetPawn, MessageTypeDefOf.NeutralEvent, historical: false);
                ActiveAidRequestsByTargetPawnId.Remove(targetId);
            }
        }

        public static bool TryConsumeRequest(Map map, MouseDisasterRequestKind kind, int amount, out string failureReason)
        {
            failureReason = string.Empty;
            if (map == null)
            {
                failureReason = "\u5f53\u524d\u5730\u56fe\u65e0\u6548\u3002";
                return false;
            }

            switch (kind)
            {
                case MouseDisasterRequestKind.SimpleMeal:
                    return TryConsumeThingByDef(map, ThingDefOf.MealSimple, amount, out failureReason);
                case MouseDisasterRequestKind.FineMeal:
                    return TryConsumeThingByDef(map, ThingDefOf.MealFine, amount, out failureReason);
                case MouseDisasterRequestKind.Medicine:
                    return TryConsumeMedicine(map, amount, out failureReason);
                case MouseDisasterRequestKind.HerbalMedicine:
                    return TryConsumeThingByDef(map, ThingDefOf.MedicineHerbal, amount, out failureReason);
                case MouseDisasterRequestKind.Silver:
                    return TryConsumeThingByDef(map, ThingDefOf.Silver, amount, out failureReason);
                case MouseDisasterRequestKind.PrisonerOrSlaveBaby:
                    return TryConsumePrisonerOrSlaveBaby(map, out failureReason);
                default:
                    failureReason = "\u4e0d\u652f\u6301\u7684\u8bf7\u6c42\u7c7b\u578b\u3002";
                    return false;
            }
        }

        public static bool TryCreateIntelSite(Map map, MouseDisasterIntelSiteKind kind, out Site site, out string failureReason)
        {
            site = null;
            failureReason = string.Empty;
            if (map == null || Find.World == null)
            {
                failureReason = "\u4e16\u754c\u5730\u56fe\u4e0d\u53ef\u7528\u3002";
                return false;
            }

            if (!TileFinder.TryFindNewSiteTile(out PlanetTile tile, 5, 22))
            {
                failureReason = "\u627e\u4e0d\u5230\u5408\u9002\u7684\u60c5\u62a5\u5730\u70b9\u3002";
                return false;
            }

            switch (kind)
            {
                case MouseDisasterIntelSiteKind.Treasure:
                    site = SiteMaker.MakeSite(DefDatabase<SitePartDef>.GetNamed("ItemStash"), tile, null, ifHostileThenMustRemainHostile: false);
                    break;
                case MouseDisasterIntelSiteKind.StructureCluster:
                    site = SiteMaker.MakeSite(DefDatabase<SitePartDef>.GetNamed("Outpost"), tile, Find.FactionManager.RandomEnemyFaction());
                    break;
                case MouseDisasterIntelSiteKind.SmallSettlement:
                    site = SiteMaker.MakeSite(DefDatabase<SitePartDef>.GetNamed("BanditCamp"), tile, Find.FactionManager.RandomEnemyFaction());
                    break;
            }

            if (site == null)
            {
                failureReason = "\u60c5\u62a5\u5730\u70b9\u751f\u6210\u5931\u8d25\u3002";
                return false;
            }

            Find.WorldObjects.Add(site);
            return true;
        }

        public static bool IsPlagueCarrierMouseDisasterPawn(Pawn pawn)
        {
            return pawn != null &&
                   !pawn.Dead &&
                   pawn.Spawned &&
                   MouseDisasterUtility.IsRatkin(pawn) &&
                   pawn.health?.hediffSet?.HasHediff(MouseDisasterDefOf.MouseDisaster_Plague) == true;
        }

        public static void InfectWithPlague(Pawn pawn, float? severity = null)
        {
            if (pawn?.health == null || MouseDisasterDefOf.MouseDisaster_Plague == null)
            {
                return;
            }

            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(MouseDisasterDefOf.MouseDisaster_Plague);
            if (existing != null)
            {
                if (severity.HasValue)
                {
                    existing.Severity = Mathf.Max(existing.Severity, severity.Value);
                }
                return;
            }

            Hediff hediff = HediffMaker.MakeHediff(MouseDisasterDefOf.MouseDisaster_Plague, pawn);
            hediff.Severity = severity ?? Rand.Range(PlagueStartSeverityMin, PlagueStartSeverityMax);
            pawn.health.AddHediff(hediff);
        }

        public static void InfectMany(IEnumerable<Pawn> pawns, bool leaderOnly = false)
        {
            if (pawns == null)
            {
                return;
            }

            List<Pawn> list = pawns.Where(p => p != null).ToList();
            if (leaderOnly)
            {
                if (list.Count > 0)
                {
                    InfectWithPlague(list[0]);
                }
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                InfectWithPlague(list[i]);
            }
        }

        public static void DoPlagueSpreadCheck(Map map)
        {
            if (map == null || MouseDisasterDefOf.MouseDisaster_Plague == null)
            {
                return;
            }

            int carrierCount = map.mapPawns.AllPawnsSpawned.Count(IsPlagueCarrierMouseDisasterPawn);
            if (carrierCount <= 0)
            {
                return;
            }

            float chance = Mathf.Min(0.005f * carrierCount, 0.30f);
            List<Pawn> infected = new List<Pawn>();
            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned.Where(p => p != null && !p.Dead && p.health?.capacities != null).ToList();
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn pawn = colonists[i];
                if (pawn.health.hediffSet.HasHediff(MouseDisasterDefOf.MouseDisaster_Plague))
                {
                    continue;
                }

                float bloodPumpingPercent = pawn.health.capacities.GetLevel(PawnCapacityDefOf.BloodPumping) * 100f;
                if (bloodPumpingPercent >= 120f)
                {
                    continue;
                }

                if (!Rand.Chance(chance))
                {
                    continue;
                }

                InfectWithPlague(pawn);
                infected.Add(pawn);
            }

            if (infected.Count > 0)
            {
                Messages.Message("\u9f20\u75ab\u5f00\u59cb\u5728\u6b96\u6c11\u5730\u5185\u6269\u6563\u4e86\u3002", infected, MessageTypeDefOf.NegativeHealthEvent, historical: true);
            }
        }

        private static bool TryConsumeThingByDef(Map map, ThingDef def, int amount, out string failureReason)
        {
            failureReason = string.Empty;
            if (def == null)
            {
                failureReason = "\u8bf7\u6c42\u7269\u8d44\u4e0d\u5b58\u5728\u3002";
                return false;
            }

            List<Thing> things = map.listerThings.AllThings.Where(t => t?.def == def && t.stackCount > 0 && t.Spawned).ToList();
            int total = things.Sum(t => t.stackCount);
            if (total < amount)
            {
                failureReason = "\u5e93\u5b58\u4e0d\u8db3\u3002";
                return false;
            }

            int remaining = amount;
            for (int i = 0; i < things.Count && remaining > 0; i++)
            {
                Thing thing = things[i];
                int consume = Math.Min(thing.stackCount, remaining);
                if (consume >= thing.stackCount)
                {
                    remaining -= thing.stackCount;
                    thing.Destroy();
                }
                else
                {
                    thing.SplitOff(consume).Destroy();
                    remaining -= consume;
                }
            }

            return true;
        }

        private static bool TryConsumeMedicine(Map map, int amount, out string failureReason)
        {
            failureReason = string.Empty;
            List<Thing> medicines = map.listerThings.AllThings
                .Where(t => t?.def != null && t.def.IsMedicine && t.stackCount > 0 && t.Spawned)
                .OrderByDescending(t => t.def.BaseMarketValue)
                .ToList();
            int total = medicines.Sum(t => t.stackCount);
            if (total < amount)
            {
                failureReason = "\u836f\u54c1\u4e0d\u8db3\u3002";
                return false;
            }

            int remaining = amount;
            for (int i = 0; i < medicines.Count && remaining > 0; i++)
            {
                Thing med = medicines[i];
                int consume = Math.Min(med.stackCount, remaining);
                if (consume >= med.stackCount)
                {
                    remaining -= med.stackCount;
                    med.Destroy();
                }
                else
                {
                    med.SplitOff(consume).Destroy();
                    remaining -= consume;
                }
            }

            return true;
        }

        private static bool TryConsumePrisonerOrSlaveBaby(Map map, out string failureReason)
        {
            failureReason = string.Empty;
            Pawn baby = MouseDisasterUtility.FindExchangeOfferBaby(map, MouseDisasterUtility.ChildExchangeModePrisoner) ??
                        MouseDisasterUtility.FindExchangeOfferBaby(map, MouseDisasterUtility.ChildExchangeModeSlave);
            if (baby == null)
            {
                failureReason = "\u6ca1\u6709\u53ef\u4ea4\u4ed8\u7684\u56da\u72af/\u5974\u96b6\u5a74\u513f\u3002";
                return false;
            }

            if (baby.Spawned)
            {
                baby.Destroy();
            }
            return true;
        }

        private static void RegisterAidRequest(Pawn targetPawn, IEnumerable<Pawn> pawns, MouseDisasterRequestKind requestKind, int amount, bool createsIntelSite, MouseDisasterIntelSiteKind intelSiteKind)
        {
            if (targetPawn?.Map == null)
            {
                return;
            }

            List<int> pawnIds = pawns?
                .Where(pawn => pawn != null && !pawn.Dead)
                .Select(pawn => pawn.thingIDNumber)
                .Distinct()
                .ToList() ?? new List<int>();
            if (pawnIds.Count == 0)
            {
                return;
            }

            ActiveAidRequestsByTargetPawnId[targetPawn.thingIDNumber] = new AidRequestState
            {
                mapId = targetPawn.Map.uniqueID,
                targetPawnId = targetPawn.thingIDNumber,
                pawnIds = pawnIds,
                requestKind = requestKind,
                amount = amount,
                createsIntelSite = createsIntelSite,
                intelSiteKind = intelSiteKind,
                expireTick = (Find.TickManager?.TicksGame ?? 0) + AidRequestVisitDurationTicks
            };
        }

        private static void PrepareAidVisitorPawn(Pawn pawn, ThingDef requestedThingDef)
        {
            if (pawn == null)
            {
                return;
            }

            pawn.jobs?.StopAll();
            pawn.mindState?.mentalStateHandler?.Reset();
            pawn.mindState?.duty = null;
            pawn.guest?.SetGuestStatus(null, GuestStatus.Guest);
            MouseDisasterUtility.StripRatEggInventory(pawn);
            RemoveRequestedItemsFromInventory(pawn, requestedThingDef);
        }

        private static void RemoveRequestedItemsFromInventory(Pawn pawn, ThingDef requestedThingDef)
        {
            if (pawn?.inventory?.innerContainer == null || requestedThingDef == null)
            {
                return;
            }

            for (int i = pawn.inventory.innerContainer.Count - 1; i >= 0; i--)
            {
                Thing thing = pawn.inventory.innerContainer[i];
                if (thing?.def == requestedThingDef)
                {
                    thing.Destroy();
                }
            }
        }

        private static Dictionary<int, Pawn> BuildSpawnedPawnLookup(IReadOnlyList<Pawn> pawns)
        {
            Dictionary<int, Pawn> lookup = new Dictionary<int, Pawn>();
            if (pawns == null)
            {
                return lookup;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn != null)
                {
                    lookup[pawn.thingIDNumber] = pawn;
                }
            }

            return lookup;
        }

        private static List<Pawn> ResolveAidRequestPawns(AidRequestState state, Dictionary<int, Pawn> pawnLookup)
        {
            if (state?.pawnIds == null || pawnLookup == null)
            {
                return new List<Pawn>();
            }

            return state.pawnIds
                .Where(id => pawnLookup.TryGetValue(id, out Pawn pawn) && pawn != null && pawn.Spawned && !pawn.Dead)
                .Select(id => pawnLookup[id])
                .ToList();
        }

        private static void NotifyAidRequestCompleted(Map map, AidRequestState state, Pawn targetPawn)
        {
            if (state == null || targetPawn == null)
            {
                return;
            }

            if (state.createsIntelSite)
            {
                if (TryCreateIntelSite(map, state.intelSiteKind, out Site site, out string intelFailure))
                {
                    Find.LetterStack.ReceiveLetter("鼠灾情报", "鼠灾来客留下了新的地点情报，随后离开了。", LetterDefOf.PositiveEvent, site);
                }
                else
                {
                    Messages.Message(intelFailure, targetPawn, MessageTypeDefOf.RejectInput, historical: false);
                }
            }
            else
            {
                Messages.Message("你满足了这次鼠灾请求，对方带着物资离开了。", targetPawn, MessageTypeDefOf.PositiveEvent, historical: false);
            }
        }
    }
}
