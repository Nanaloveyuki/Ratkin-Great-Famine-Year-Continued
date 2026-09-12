using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace MouseDisaster
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PreTraded))]
    public static class TradePatches
    {
        public static void Prefix(Pawn __instance, TradeAction action)
        {
            if (action == TradeAction.PlayerBuys && MouseDisasterUtility.IsMouseDisasterTradePawn(__instance))
            {
                MouseDisasterUtility.PreparePurchasedTradePawnJoinStatus(__instance);
            }
        }

        public static void Postfix(Pawn __instance, TradeAction action)
        {
            bool markedChattel = MouseDisasterUtility.IsMarkedTradableChattel(__instance);
            bool forcePrisoner = MouseDisasterUtility.IsForcedPrisonerOnPurchase(__instance);
            if (action != TradeAction.PlayerBuys || (!markedChattel && !forcePrisoner) || __instance == null)
            {
                return;
            }

            MouseDisasterVisitorUtility.RemoveVisitorRecord(__instance);
            MouseDisasterUtility.ApplyPurchasedTradePawnState(__instance);
            MouseDisasterUtility.UnmarkTradableChattel(__instance);
            MouseDisasterUtility.ConsumeForcedPrisonerOnPurchase(__instance);
        }
    }

    [HarmonyPatch(typeof(Caravan), nameof(Caravan.AddPawn))]
    public static class MouseDisasterTradeCaravanAddPawnPatch
    {
        public static void Postfix(Pawn p)
        {
            if (p == null || !MouseDisasterUtility.IsMouseDisasterTradePawn(p) || !MouseDisasterUtility.IsForcedPrisonerOnPurchase(p))
            {
                return;
            }

            MouseDisasterVisitorUtility.RemoveVisitorRecord(p);
            MouseDisasterUtility.ApplyPurchasedTradePawnState(p);
            MouseDisasterUtility.UnmarkTradableChattel(p);
            MouseDisasterUtility.ConsumeForcedPrisonerOnPurchase(p);
        }
    }

    [HarmonyPatch(typeof(TransferableUIUtility), nameof(TransferableUIUtility.DrawCaptiveTradeInfo))]
    public static class MouseDisasterTradeCaptiveInfoPatch
    {
        public static bool Prefix(Transferable trad, Rect rect, ref float curX)
        {
            if (!(trad?.AnyThing is Pawn pawn) || !MouseDisasterUtility.IsMouseDisasterTradePawn(pawn))
            {
                return true;
            }

            float width = 140f;
            Rect infoRect = new Rect(curX, 0f, width, rect.height);
            MouseDisasterTradePawnJoinMode joinMode = MouseDisasterUtility.GetRatkinYoungTradeJoinMode();
            string modeLabel = ("MouseDisaster_Settings_RatkinYoungTradeJoinMode_" + joinMode).Translate().ToString();
            Widgets.Label(infoRect, "MouseDisaster_UI_PurchasedAsLabel".Translate(modeLabel).Resolve());
            if (Mouse.IsOver(infoRect))
            {
                Widgets.DrawHighlight(infoRect);
                TooltipHandler.TipRegion(infoRect, "MouseDisaster_UI_PurchasedAsDescription".Translate(modeLabel).Resolve());
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(TraderCaravanUtility), nameof(TraderCaravanUtility.GetTraderCaravanRole))]
    public static class MouseDisasterTraderCaravanRolePatch
    {
        public static void Postfix(Pawn p, ref TraderCaravanRole __result)
        {
            if (MouseDisasterUtility.IsMarkedTradableChattel(p))
            {
                __result = TraderCaravanRole.Chattel;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.ExitMap), new[] { typeof(bool), typeof(Rot4) })]
    internal static class MouseDisasterTraderExitMapPatch
    {
        public static void Prefix(Pawn __instance, Rot4 exitDir)
        {
            if (__instance == null || !__instance.Spawned || __instance.Map == null ||
                MouseDisasterUtility.IsPlayerAffiliatedRatkin(__instance) ||
                MouseDisasterDefOf.MouseDisaster_TraderRatkinAdult == null ||
                !MouseDisasterUtility.IsMouseDisasterTraderAdult(__instance))
            {
                return;
            }

            Lord lord = __instance.GetLord();
            if (lord == null || lord.ownedPawns == null ||
                (!(lord.LordJob is LordJob_TradeWithColony) && !(lord.LordJob is LordJob_TravelAndExit)))
            {
                return;
            }

            List<Pawn> children = lord.ownedPawns
                .Where(IsMouseDisasterTraderCaravanChild)
                .Distinct()
                .ToList();
            List<Pawn> linkedChildren = MouseDisasterUtility.GetLeadYourPetLinkedPawns(__instance)
                .Where(IsMouseDisasterTraderCaravanChild)
                .Where(child => !children.Contains(child))
                .ToList();
            children.AddRange(linkedChildren);
            for (int i = 0; i < children.Count; i++)
            {
                Pawn child = children[i];
                if (child.Spawned && child.Map == __instance.Map)
                {
                    TryExitChild(child, exitDir);
                }
            }
        }

        private static bool IsMouseDisasterTraderCaravanChild(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Destroyed || pawn.DevelopmentalStage == DevelopmentalStage.Adult ||
                MouseDisasterUtility.IsPlayerAffiliatedRatkin(pawn) ||
                MouseDisasterDefOf.MouseDisaster_BeggarRatkinChild == null ||
                pawn.kindDef != MouseDisasterDefOf.MouseDisaster_BeggarRatkinChild)
            {
                return false;
            }

            // Lead Your Pet can create the same child kind as travel stock without this Mod's marker.
            return true;
        }

        private static void TryExitChild(Pawn child, Rot4 exitDir)
        {
            try
            {
                // Use vanilla ExitMap so Lord membership, faction notifications and WorldPawns stay consistent.
                child.ExitMap(false, exitDir);
            }
            catch (Exception exception)
            {
                Log.Error("[MouseDisaster] Could not move a trader caravan child out with its trader: " + exception);
            }
        }
    }

    [HarmonyPatch(typeof(TraderKindDef), nameof(TraderKindDef.WillTrade))]
    public static class MouseDisasterTraderKindWillTradePatch
    {
        public static void Postfix(TraderKindDef __instance, ThingDef td, ref bool __result)
        {
            if (__result || __instance == null || td == null)
            {
                return;
            }

            if (!MouseDisasterTraderTradePolicy.ShouldForceTraderWillTrade(__instance.defName))
            {
                return;
            }

            if (MouseDisasterTraderTradePolicy.IsRatEggTradeGood(td.defName, td.label))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_TraderTracker), nameof(Pawn_TraderTracker.Goods), MethodType.Getter)]
    public static class MouseDisasterTraderGoodsPatch
    {
        private static readonly AccessTools.FieldRef<Pawn_TraderTracker, Pawn> PawnField =
            AccessTools.FieldRefAccess<Pawn_TraderTracker, Pawn>("pawn");

        public static void Postfix(Pawn_TraderTracker __instance, ref IEnumerable<Thing> __result)
        {
            Pawn pawn = PawnField(__instance);
            if (!MouseDisasterUtility.IsMouseDisasterTraderAdult(pawn) ||
                !(pawn.GetLord()?.LordJob is LordJob_TradeWithColony) ||
                pawn.inventory?.innerContainer == null)
            {
                return;
            }

            List<Thing> goods = __result?.ToList() ?? new List<Thing>();
            foreach (Thing thing in pawn.inventory.innerContainer)
            {
                if (thing != null && !pawn.inventory.NotForSale(thing) && !goods.Contains(thing))
                {
                    goods.Add(thing);
                }
            }

            __result = goods;
        }
    }
}
