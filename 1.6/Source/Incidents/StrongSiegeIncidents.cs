using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MouseDisaster
{

    public abstract class IncidentWorker_MouseDisasterStrongSiegeBase : IncidentWorker
    {
        protected abstract bool InfectsWithPlague { get; }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map map &&
                   base.CanFireNowSub(parms) &&
                   MouseDisasterUtility.TryFindEntryCell(map, out _);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!MouseDisasterUtility.TryFindEntryCell(map, out IntVec3 cell))
            {
                return false;
            }

            Faction faction = MouseDisasterPhase3Utility.ResolveVisitorFaction();
            if (faction != null)
            {
                MouseDisasterUtility.MakeFactionNeutralToPlayer(faction, force: true);
            }

            int count = MouseDisasterUtility.CalculateEscalatingGroupCount(parms.points, 4, 16, 80f);
            List<Pawn> pawns = new List<Pawn>(MouseDisasterUtility.SpawnThiefGroup(map, cell, count, childOnly: false));
            if (pawns.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                MouseDisasterUtility.RegisterStrongSiegePawn(pawns[i]);
            }

            if (InfectsWithPlague)
            {
                MouseDisasterPlagueUtility.InfectMany(pawns);
            }

            MouseDisasterVisitorUtility.RegisterVisitors(pawns);
            if (!MouseDisasterVisitorUtility.SendVisitorChoiceLetter(def, parms, map, pawns))
            {
                SendStandardLetter(def.letterLabel, def.letterText, def.letterDef, parms, pawns);
            }
            return true;
        }
    }

    public class IncidentWorker_MouseDisasterStrongSiege : IncidentWorker_MouseDisasterStrongSiegeBase
    {
        protected override bool InfectsWithPlague => false;
    }

    public class IncidentWorker_MouseDisasterPlagueStrongSiege : IncidentWorker_MouseDisasterStrongSiegeBase
    {
        protected override bool InfectsWithPlague => true;
    }
}
