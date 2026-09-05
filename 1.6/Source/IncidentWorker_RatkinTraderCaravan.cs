using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace MouseDisaster
{
    public class IncidentWorker_RatkinTraderCaravan : IncidentWorker
    {
        private static readonly string[] OptionalGoods =
        {
            "RatEgg_Meat",
            "RatEgg_Ear",
            "RatEgg_Tail",
            "RatEgg_Brain",
            "RatEgg_Viscera",
            "RatEgg_SilkSkin"
        };

        private static readonly string[] RatEggCuisineKeywords =
        {
            "rategg",
            "rat_egg",
            "rat egg",
            "鼠蛋"
        };

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return base.CanFireNowSub(parms) &&
                   Find.Storyteller.difficulty.ChildrenAllowed &&
                   MouseDisasterUtility.TryFindEntryCell((Map)parms.target, out _) &&
                   MouseDisasterUtility.TryFindFormerFaction(out _);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!MouseDisasterUtility.TryFindEntryCell(map, out IntVec3 cell) || !MouseDisasterUtility.TryFindFormerFaction(out Faction faction))
            {
                return false;
            }

            MouseDisasterUtility.MakeFactionNeutralToPlayer(faction, force: true);
            MouseDisasterUtility.EnsureMouseDisasterFactionNeutralOnMap(map, faction);

            List<Pawn> pawns = new List<Pawn>();
            Pawn traderPawn = GenerateFemaleTraderPawn(faction);
            if (traderPawn == null)
            {
                return false;
            }

            GenSpawn.Spawn(traderPawn, CellFinder.RandomClosewalkCellNear(cell, map, 6), map);
            MouseDisasterUtility.EnsureTradeLeader(traderPawn, MouseDisasterUtility.ResolveSlaveTraderKind());
            traderPawn.health.AddHediff(MouseDisasterDefOf.MouseDisaster_TraderCaravanTrader);
            pawns.Add(traderPawn);

            Pawn escortPawn = MouseDisasterUtility.GenerateFactionRatkinPawn(MouseDisasterDefOf.MouseDisaster_TraderRatkinEscort, faction, DevelopmentalStage.Adult, 0.6f);
            if (escortPawn == null)
            {
                return false;
            }

            GenSpawn.Spawn(escortPawn, CellFinder.RandomClosewalkCellNear(cell, map, 6), map);
            escortPawn.health.AddHediff(MouseDisasterDefOf.MouseDisaster_TraderCaravanEscort);
            pawns.Add(escortPawn);

            int saleChildren = Rand.RangeInclusive(5, 30);
            List<Pawn> children = new List<Pawn>();

            for (int i = 0; i < saleChildren; i++)
            {
                Pawn child = MouseDisasterUtility.GenerateFactionRatkinPawn(MouseDisasterDefOf.MouseDisaster_BeggarRatkinChild, faction, DevelopmentalStage.Baby, 0.5f);
                if (child == null)
                {
                    continue;
                }

                MouseDisasterUtility.SetBiologicalAgeYears(child, Rand.Range(1f, 2.9f));
                GenSpawn.Spawn(child, CellFinder.RandomClosewalkCellNear(cell, map, 6), map);
                MouseDisasterUtility.StripRatEggInventory(child);
                PrepareChattelChild(child, faction);
                child.health.AddHediff(MouseDisasterDefOf.MouseDisaster_TraderCaravanChild);
                children.Add(child);
                pawns.Add(child);
            }

            if (children.Count == 0)
            {
                return false;
            }

            MouseDisasterUtility.MarkChildExchangeMoodChildren(children);
            List<Pawn> childrenWithMother = children
                .InRandomOrder()
                .Take(MouseDisasterGeneRestorePolicy.ResolveTraderCaravanChildrenToLinkCount(children.Count))
                .ToList();
            MouseDisasterUtility.LinkIncidentParentToChildren(traderPawn, childrenWithMother);
            MouseDisasterUtility.TryStartLeadYourPetRelatedAdultLeashes(pawns);
            FillTraderInventory(traderPawn);

            if (!RCellFinder.TryFindRandomSpotJustOutsideColony(traderPawn.Position, map, traderPawn, out IntVec3 result))
            {
                result = map.Center;
            }

            List<Pawn> tradeGroup = new List<Pawn> { traderPawn, escortPawn };
            tradeGroup.AddRange(children);
            Lord tradeLord = LordMaker.MakeNewLord(faction, new LordJob_TradeWithColony(faction, result), map, tradeGroup);
            if (tradeLord != null)
            {
                MouseDisasterUtility.TryAssignLeadYourPetTravelMouseEggs(tradeLord);
            }
            MouseDisasterUtility.EnsureMouseDisasterFactionNeutralOnMap(map, faction);
            MouseDisasterVisitorUtility.RegisterVisitors(pawns);
            if (!MouseDisasterVisitorUtility.SendVisitorChoiceLetter(def, parms, map, pawns))
            {
                SendStandardLetter(def.letterLabel, def.letterText, def.letterDef, parms, pawns);
            }
            return true;
        }

        private static void PrepareChattelChild(Pawn child, Faction faction)
        {
            if (child == null)
            {
                return;
            }

            if (child.Faction != faction)
            {
                child.SetFaction(faction);
            }

            MouseDisasterUtility.PrepareTradablePrisoner(child, faction);
        }

        private static Pawn GenerateFemaleTraderPawn(Faction faction)
        {
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Pawn candidate = MouseDisasterUtility.GenerateFactionRatkinPawn(
                    MouseDisasterDefOf.MouseDisaster_TraderRatkinAdult,
                    faction,
                    DevelopmentalStage.Adult,
                    0.65f);
                if (candidate == null)
                {
                    continue;
                }

                if (candidate.gender == Gender.Female)
                {
                    return candidate;
                }

                candidate.Destroy(DestroyMode.Vanish);
            }

            Pawn fallback = MouseDisasterUtility.GenerateFactionRatkinPawn(
                MouseDisasterDefOf.MouseDisaster_TraderRatkinAdult,
                faction,
                DevelopmentalStage.Adult,
                0.65f);
            if (fallback != null)
            {
                fallback.gender = Gender.Female;
            }

            return fallback;
        }

        private static void FillTraderInventory(Pawn traderPawn)
        {
            TraderKindDef traderKind = traderPawn?.trader?.traderKind;
            ThingDef meatDef = DefDatabase<ThingDef>.GetNamedSilentFail("Meat_Human");
            if (CanTraderActuallySell(traderKind, meatDef))
            {
                Thing meat = ThingMaker.MakeThing(meatDef);
                meat.stackCount = 50;
                traderPawn.inventory.innerContainer.TryAdd(meat);
            }

            HashSet<ThingDef> goodsToAdd = new HashSet<ThingDef>();
            for (int i = 0; i < OptionalGoods.Length; i++)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(OptionalGoods[i]);
                if (def != null)
                {
                    goodsToAdd.Add(def);
                }
            }

            List<ThingDef> linkedCuisineGoods = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(IsRatEggCuisineIngredient)
                .InRandomOrder()
                .Take(16)
                .ToList();
            for (int i = 0; i < linkedCuisineGoods.Count; i++)
            {
                goodsToAdd.Add(linkedCuisineGoods[i]);
            }

            foreach (ThingDef def in goodsToAdd)
            {
                if (!CanTraderActuallySell(traderKind, def))
                {
                    continue;
                }

                Thing thing = ThingMaker.MakeThing(def);
                thing.stackCount = def.stackLimit > 1 ? System.Math.Min(def.stackLimit, Rand.RangeInclusive(3, 15)) : 1;
                traderPawn.inventory.innerContainer.TryAdd(thing);
            }
        }

        private static bool IsRatEggCuisineIngredient(ThingDef def)
        {
            if (def == null || def.category != ThingCategory.Item || def.IsCorpse || def.BaseMarketValue <= 0f)
            {
                return false;
            }

            string source = ((def.defName ?? string.Empty) + " " + (def.label ?? string.Empty)).ToLowerInvariant();
            for (int i = 0; i < RatEggCuisineKeywords.Length; i++)
            {
                if (source.Contains(RatEggCuisineKeywords[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CanTraderActuallySell(TraderKindDef traderKind, ThingDef def)
        {
            if (traderKind == null || def == null)
            {
                return false;
            }

            return (def.tradeability.TraderCanSell() ||
                    MouseDisasterTraderTradePolicy.CanTraderStockRatEggTradeGood(traderKind.defName, def.defName, def.label)) &&
                   traderKind.WillTrade(def);
        }
    }
}
