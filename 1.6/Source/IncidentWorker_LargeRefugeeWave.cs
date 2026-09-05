using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace MouseDisaster
{
    public class IncidentWorker_LargeRefugeeWave : IncidentWorker
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

            int count = MouseDisasterUtility.CalculateEscalatingFixedCount(parms.points, 10, 50, 50f);
            int children = Find.Storyteller.difficulty.ChildrenAllowed ? Mathf.Clamp(Mathf.RoundToInt(count * 0.35f), 0, count - 1) : 0;
            int adults = Mathf.Max(1, count - children);
            List<Pawn> pawns = new List<Pawn>(MouseDisasterUtility.SpawnBeggarGroup(map, cell, adults, children));
            if (pawns.Count == 0)
            {
                return false;
            }

            foreach (Pawn pawn in pawns)
            {
                if (pawn.DevelopmentalStage == DevelopmentalStage.Adult)
                {
                    pawn.health.AddHediff(MouseDisasterDefOf.MouseDisaster_LargeRefugeeAdult);
                }
                else
                {
                    pawn.health.AddHediff(MouseDisasterDefOf.MouseDisaster_LargeRefugeeChild);
                }

                pawn.mindState?.mentalStateHandler?.Reset();
            }

            Faction faction = pawns[0]?.Faction;
            if (faction != null)
            {
                MouseDisasterUtility.MakeFactionHostileToPlayer(faction, explicitDriveAway: true);
                LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction, canKidnap: false, canTimeoutOrFlee: false, canSteal: true, breachers: true), map, pawns);
            }

            SendStandardLetter(def.letterLabel, def.letterText, def.letterDef, parms, pawns);
            return true;
        }
    }
}
