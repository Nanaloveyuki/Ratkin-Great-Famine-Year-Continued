using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MouseDisaster
{
    public class IncidentWorker_MouseDisasterSiege : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms))
            {
                return false;
            }

            return MouseDisasterUtility.TryFindEntryCell((Map)parms.target, out _);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!MouseDisasterUtility.TryFindEntryCell(map, out IntVec3 cell))
            {
                return false;
            }

            if (MouseDisasterUtility.TryFindFormerFaction(out Faction faction))
            {
                MouseDisasterUtility.MakeFactionNeutralToPlayer(faction, force: true);
            }

            int total = Rand.RangeInclusive(3, 20);
            int children = Find.Storyteller.difficulty.ChildrenAllowed ? Mathf.Clamp(Mathf.RoundToInt(total * 0.35f), 0, total - 1) : 0;
            int adults = Mathf.Max(1, total - children);

            List<Pawn> pawns = new List<Pawn>(MouseDisasterUtility.SpawnBeggarGroup(map, cell, adults, children));
            if (pawns.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                MouseDisasterUtility.RegisterSiegeBeggar(pawns[i]);
                pawns[i].health.AddHediff(MouseDisasterDefOf.MouseDisaster_SiegeBeggar);
            }

            MouseDisasterVisitorUtility.RegisterVisitors(pawns);
            if (!MouseDisasterVisitorUtility.SendVisitorChoiceLetter(def, parms, map, pawns))
            {
                SendStandardLetter(def.letterLabel, def.letterText, def.letterDef, parms, pawns);
            }
            return true;
        }
    }
}
