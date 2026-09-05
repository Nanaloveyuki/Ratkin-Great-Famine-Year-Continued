using HarmonyLib;
using RimWorld;
using Verse;

namespace MouseDisaster
{
    [HarmonyPatch(typeof(Faction), nameof(Faction.RelationWith))]
    public static class MouseDisasterFactionRelationPatch
    {
        [System.ThreadStatic]
        private static bool repairingRelation;

        public static bool Prefix(Faction __instance, Faction other, bool allowNull, ref FactionRelation __result)
        {
            if (allowNull || repairingRelation || __instance == null || other == null || __instance == other)
            {
                return true;
            }

            FactionDef hiddenDef = MouseDisasterDefOf.MouseDisaster_HiddenFaction;
            if (hiddenDef == null || (__instance.def != hiddenDef && other.def != hiddenDef))
            {
                return true;
            }

            try
            {
                repairingRelation = true;
                FactionRelation existing = __instance.RelationWith(other, allowNull: true);
                if (existing != null && existing.other == other)
                {
                    __result = existing;
                    return false;
                }

                __instance.SetRelation(new FactionRelation
                {
                    other = other,
                    kind = FactionRelationKind.Neutral,
                    baseGoodwill = other == Faction.OfPlayer ? 20 : 0
                });

                FactionRelation relationToOther = __instance.RelationWith(other, allowNull: true);
                if (relationToOther != null)
                {
                    relationToOther.baseGoodwill = other == Faction.OfPlayer ? 20 : 0;
                }

                FactionRelation relationToSelf = other.RelationWith(__instance, allowNull: true);
                if (relationToSelf != null)
                {
                    relationToSelf.baseGoodwill = __instance == Faction.OfPlayer ? 20 : 0;
                }

                __result = relationToOther ?? new FactionRelation
                {
                    other = other,
                    kind = FactionRelationKind.Neutral,
                    baseGoodwill = other == Faction.OfPlayer ? 20 : 0
                };
                return false;
            }
            finally
            {
                repairingRelation = false;
            }
        }
    }
}
