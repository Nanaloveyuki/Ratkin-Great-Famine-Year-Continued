using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MouseDisaster
{
    public class IncidentWorker_ThiefRatkinGroup : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms))
            {
                return false;
            }

            IntVec3 cell;
            return MouseDisasterUtility.TryFindEntryCell((Map)parms.target, out cell);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
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
            List<Pawn> pawns = new List<Pawn>(MouseDisasterUtility.SpawnThiefGroup(map, cell, count, childOnly: false));
            if (pawns.Count == 0)
            {
                return false;
            }

            foreach (Pawn pawn in pawns)
            {
                if (pawn.DevelopmentalStage == DevelopmentalStage.Adult)
                {
                    pawn.health.AddHediff(MouseDisasterDefOf.MouseDisaster_ThiefAdult);
                }
                else
                {
                    pawn.health.AddHediff(MouseDisasterDefOf.MouseDisaster_ThiefChild);
                }
            }

            MouseDisasterVisitorUtility.RegisterVisitors(pawns);
            if (!MouseDisasterVisitorUtility.SendVisitorChoiceLetter(def, parms, map, pawns))
            {
                Messages.Message("一帮饥饿的鼠族偷偷摸进了殖民地，它们只在找食物。", pawns, MessageTypeDefOf.NeutralEvent, false);
            }
            Find.TickManager.slower.SignalForceNormalSpeedShort();
            return true;
        }
    }
}
