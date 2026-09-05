using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

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
            int saleChildren = Rand.RangeInclusive(5, 30);
            return GameComponent_MouseDisasterPawnGeneration.TryStartTraderCaravan(def, parms, map, cell, faction, saleChildren);
        }

        internal static void PrepareChattelChild(Pawn child, Faction faction)
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

        internal static Pawn GenerateFemaleTraderPawn(Faction faction)
        {
            Pawn pawn = MouseDisasterUtility.GenerateFactionRatkinPawn(
                MouseDisasterDefOf.MouseDisaster_TraderRatkinAdult,
                faction,
                DevelopmentalStage.Adult,
                0.65f,
                allowViolenceDisabledTraits: false,
                fixedGender: Gender.Female);
            return pawn;
        }

        internal static void FillTraderInventory(Pawn traderPawn)
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
