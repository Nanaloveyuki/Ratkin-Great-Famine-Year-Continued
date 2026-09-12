using HarmonyLib;
using RimWorld;
using Verse;

namespace MouseDisaster
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    public static class MouseDisasterSpawnSetupPatch
    {
        public static void Postfix(Pawn __instance)
        {
            // SpawnSetup 会和并行渲染交错发生，这里只标记缓存失效，
            // 避免在生成期做基因/身份/兼容状态重写，把 Verse 的共享缓存打坏。
            MouseDisasterUtility.MarkMapPawnCacheDirty(__instance);
            GameComponent_MouseDisasterEventBehavior.Component?.NotifySpawned(__instance);
        }
    }

    [HarmonyPatch(typeof(GenSpawn), nameof(GenSpawn.Spawn), new[]
    {
        typeof(Thing),
        typeof(IntVec3),
        typeof(Map),
        typeof(Rot4),
        typeof(WipeMode),
        typeof(bool),
        typeof(bool)
    })]
    public static class MouseDisasterTemperatureApparelSpawnPatch
    {
        public static void Prefix(Thing newThing, Map map)
        {
            Pawn pawn = newThing as Pawn;
            if (pawn != null && MouseDisasterUtility.IsMouseDisasterPawn(pawn))
            {
                MouseDisasterUtility.ApplyTemperatureProtectionApparel(pawn, map);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SetFaction))]
    public static class MouseDisasterSetFactionPatch
    {
        public static void Postfix(Pawn __instance)
        {
            MouseDisasterUtility.NotifyMouseDisasterPawnIdentityOrLifeStageChanged(__instance);
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.DeSpawn))]
    public static class MouseDisasterDeSpawnPatch
    {
        public static void Prefix(Pawn __instance)
        {
            MouseDisasterUtility.MarkMapPawnCacheDirty(__instance);
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class MouseDisasterKillPatch
    {
        public static void Prefix(Pawn __instance)
        {
            MouseDisasterUtility.MarkMapPawnCacheDirty(__instance);
        }
    }

    [HarmonyPatch(typeof(Pawn_GuestTracker), nameof(Pawn_GuestTracker.SetGuestStatus), new[] { typeof(Faction), typeof(GuestStatus) })]
    public static class MouseDisasterGuestStatusPatch
    {
        private static readonly AccessTools.FieldRef<Pawn_GuestTracker, Pawn> PawnField =
            AccessTools.FieldRefAccess<Pawn_GuestTracker, Pawn>("pawn");

        public static void Postfix(Pawn_GuestTracker __instance)
        {
            Pawn pawn = PawnField(__instance);
            if (pawn == null || pawn.Dead || !MouseDisasterUtility.IsRatkin(pawn))
            {
                return;
            }

            MouseDisasterUtility.NotifyMouseDisasterPawnIdentityOrLifeStageChanged(pawn);
            MouseDisasterUtility.ConfigureNoRescueJoinForIncidentVisitor(pawn);
            MouseDisasterUtility.TryRecoverIncidentVisitorFromPlayerGuest(pawn);
        }
    }

    [HarmonyPatch(typeof(Pawn_GuestTracker), "Notify_PawnUndowned")]
    public static class MouseDisasterGuestUndownedPatch
    {
        private static readonly AccessTools.FieldRef<Pawn_GuestTracker, Pawn> PawnField =
            AccessTools.FieldRefAccess<Pawn_GuestTracker, Pawn>("pawn");

        public static void Postfix(Pawn_GuestTracker __instance)
        {
            Pawn pawn = PawnField(__instance);
            MouseDisasterUtility.TryRecoverIncidentVisitorFromPlayerGuest(pawn);
        }
    }

}
