using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MouseDisaster
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PreTraded))]
    public static class TradePatches
    {
        public static void Postfix(Pawn __instance, TradeAction action)
        {
            bool markedChattel = MouseDisasterUtility.IsMarkedTradableChattel(__instance);
            bool forcePrisoner = MouseDisasterUtility.IsForcedPrisonerOnPurchase(__instance);
            if (action != TradeAction.PlayerBuys || (!markedChattel && !forcePrisoner) || __instance == null)
            {
                return;
            }

            if (__instance.guest == null)
            {
                PawnComponentsUtility.AddAndRemoveDynamicComponents(__instance, actAsIfSpawned: true);
            }

            if (__instance.guest == null)
            {
                return;
            }

            MouseDisasterVisitorUtility.RemoveVisitorRecord(__instance);
            MouseDisasterUtility.ApplyPurchasedTradePawnPrisonerState(__instance);
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
            MouseDisasterUtility.ApplyPurchasedTradePawnPrisonerState(p);
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
            Widgets.Label(infoRect, "MouseDisaster_UI_PurchasedAsPrisonerLabel".Translate().Resolve());
            if (Mouse.IsOver(infoRect))
            {
                Widgets.DrawHighlight(infoRect);
                TooltipHandler.TipRegion(infoRect, "MouseDisaster_UI_PurchasedAsPrisonerDescription".Translate().Resolve());
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
}
