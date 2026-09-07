using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MouseDisaster
{

    public abstract class IncidentWorker_MouseDisasterPassersbyBase : IncidentWorker
    {
        protected abstract bool InfectsWithPlague { get; }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map map && base.CanFireNowSub(parms) && MouseDisasterUtility.TryFindEntryCell(map, out _);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!MouseDisasterUtility.TryFindEntryCell(map, out IntVec3 entryCell))
            {
                return false;
            }

            if (!MouseDisasterUtility.TryFindFarEdgeCell(map, entryCell, out IntVec3 exitCell))
            {
                exitCell = map.Center;
            }

            int total = MouseDisasterUtility.CalculateEscalatingGroupCount(parms.points, 4, 12, 85f);
            int children = Find.Storyteller.difficulty.ChildrenAllowed ? Mathf.Clamp(Mathf.RoundToInt(total * 0.35f), 0, total - 1) : 0;
            int adults = Mathf.Max(1, total - children);
            Faction faction = MouseDisasterPhase3Utility.ResolveVisitorFaction();
            if (faction != null)
            {
                MouseDisasterUtility.MakeFactionNeutralToPlayer(faction, force: true);
            }

            List<Pawn> pawns = new List<Pawn>(MouseDisasterUtility.SpawnTravelerGroup(map, entryCell, adults, children, faction));
            if (pawns.Count == 0)
            {
                return false;
            }

            if (InfectsWithPlague)
            {
                MouseDisasterPlagueUtility.InfectMany(pawns);
            }

            MouseDisasterUtility.MakeTravelAndExitLord(map, pawns, exitCell, includeBabiesInExit: false);
            MouseDisasterVisitorUtility.RegisterVisitors(pawns);
            if (!MouseDisasterVisitorUtility.SendVisitorChoiceLetter(def, parms, map, pawns))
            {
                SendStandardLetter(def.letterLabel, def.letterText, def.letterDef, parms, pawns);
            }
            return true;
        }
    }

    public class IncidentWorker_MouseDisasterPassersby : IncidentWorker_MouseDisasterPassersbyBase
    {
        protected override bool InfectsWithPlague => false;
    }

    public class IncidentWorker_MouseDisasterPlaguePassersbyPhase3 : IncidentWorker_MouseDisasterPassersbyBase
    {
        protected override bool InfectsWithPlague => true;
    }
}
