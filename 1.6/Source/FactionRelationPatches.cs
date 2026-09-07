using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MouseDisaster
{
    [HarmonyPatch(typeof(FactionGenerator), nameof(FactionGenerator.GenerateFactionsIntoWorldLayer))]
    internal static class MouseDisasterFactionGenerationDiagnostics
    {
        public static void Postfix(PlanetLayer layer, List<FactionDef> factions)
        {
            if (layer == null || !layer.IsRootSurface || factions == null || Find.FactionManager == null) return;
            var missing = factions.Where(def => def != null && !def.hidden && !def.isPlayer &&
                (def.layerBlacklist.NullOrEmpty() || !def.layerBlacklist.Contains(layer.Def)) &&
                (def.layerWhitelist.NullOrEmpty() || def.layerWhitelist.Contains(layer.Def)))
                .GroupBy(def => def).Where(group => Find.FactionManager.AllFactionsListForReading.Count(f => f.def == group.Key) < group.Count())
                .Select(group => group.Key.defName).ToList();
            if (missing.Count > 0)
                Log.Warning("[MouseDisaster][FactionGeneration] Configured factions missing after world generation: " +
                    string.Join(", ", missing) + ". No factions were added or removed by this diagnostic.");
        }
    }

    [HarmonyPatch(typeof(Faction), nameof(Faction.RelationWith))]
    public static class MouseDisasterFactionRelationPatch
    {
        [System.ThreadStatic]
        private static bool repairingRelation;

        public static bool Prefix(Faction __instance, Faction other, bool allowNull, ref FactionRelation __result)
        {
            if (Current.CreatingWorld != null || allowNull || repairingRelation || __instance == null || other == null || __instance == other)
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
