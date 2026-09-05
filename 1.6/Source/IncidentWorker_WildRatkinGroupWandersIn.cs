using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MouseDisaster
{
    public class IncidentWorker_WildRatkinGroupWandersIn : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms))
            {
                return false;
            }

            Map map = (Map)parms.target;
            IntVec3 cell;
            return MouseDisasterUtility.TryFindEntryCell(map, out cell);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            IntVec3 cell;
            if (!MouseDisasterUtility.TryFindEntryCell(map, out cell))
            {
                return false;
            }

            int count = MouseDisasterUtility.CalculateGroupCount(parms.points);
            List<Pawn> pawns = new List<Pawn>(MouseDisasterUtility.SpawnWildGroup(map, cell, count, null));
            if (pawns.Count == 0)
            {
                return false;
            }

            foreach (Pawn pawn in pawns)
            {
                pawn.health.AddHediff(MouseDisasterDefOf.MouseDisaster_WildGroupWanderer);
            }

            if (MouseDisasterVisitorChoicePolicy.ShouldUpgradeToVisitorChoiceControl(def.defName) &&
                MouseDisasterVisitorUtility.RegisterAndSendVisitorChoiceLetter(def, parms, map, pawns))
            {
                return true;
            }

            if (!MouseDisasterUtility.SendFoodGiveLetter(def, parms, map, pawns))
            {
                MouseDisasterUtility.SendIncidentLetter(def, parms, pawns);
            }
            return true;
        }
    }
}
