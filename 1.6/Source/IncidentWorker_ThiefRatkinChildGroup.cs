using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MouseDisaster
{
    public class IncidentWorker_ThiefRatkinChildGroup : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms))
            {
                return false;
            }

            if (!Find.Storyteller.difficulty.ChildrenAllowed)
            {
                return false;
            }

            IntVec3 cell;
            return MouseDisasterUtility.TryFindEntryCell((Map)parms.target, out cell);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!Find.Storyteller.difficulty.ChildrenAllowed)
            {
                return false;
            }

            Map map = (Map)parms.target;
            IntVec3 cell;
            if (!MouseDisasterUtility.TryFindEntryCell(map, out cell))
            {
                return false;
            }

            if (MouseDisasterUtility.TryFindFormerFaction(out Faction faction))
            {
                MouseDisasterUtility.MakeFactionNeutralToPlayer(faction, force: true);
            }

            int count = MouseDisasterUtility.CalculateEscalatingGroupCount(parms.points, 3, 20, 75f);
            List<Pawn> pawns = new List<Pawn>(MouseDisasterUtility.SpawnThiefGroup(map, cell, count, childOnly: true));
            if (pawns.Count == 0)
            {
                return false;
            }

            foreach (Pawn pawn in pawns)
            {
                pawn.health.AddHediff(MouseDisasterDefOf.MouseDisaster_ThiefChildOnly);
            }

            MouseDisasterVisitorUtility.RegisterVisitors(pawns);
            if (!MouseDisasterVisitorUtility.SendVisitorChoiceLetter(def, parms, map, pawns))
            {
                Messages.Message("MouseDisaster_UI_ChildThievesArrived".Translate().Resolve(), pawns, MessageTypeDefOf.NeutralEvent, false);
            }
            Find.TickManager.slower.SignalForceNormalSpeedShort();
            return true;
        }
    }
}
