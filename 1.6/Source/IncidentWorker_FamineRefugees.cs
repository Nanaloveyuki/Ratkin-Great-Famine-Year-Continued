using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MouseDisaster
{
    public class IncidentWorker_FamineRefugees : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return base.CanFireNowSub(parms) && MouseDisasterUtility.TryFindEntryCell((Map)parms.target, out _);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!MouseDisasterUtility.TryFindEntryCell(map, out IntVec3 cell))
            {
                return false;
            }

            int count = Mathf.Clamp(Mathf.RoundToInt(parms.points / 200f) + 1, 1, 4);
            List<Pawn> pawns = new List<Pawn>();
            bool allowChildren = Find.Storyteller.difficulty.ChildrenAllowed;
            for (int i = 0; i < count; i++)
            {
                bool spawnChild = allowChildren && Rand.Chance(0.5f);
                PawnKindDef kindDef = spawnChild ? MouseDisasterDefOf.MouseDisaster_WildRatkinChild : MouseDisasterDefOf.MouseDisaster_WildRatkinAdult;
                DevelopmentalStage stage = spawnChild ? DevelopmentalStage.Child : DevelopmentalStage.Adult;
                Pawn pawn = MouseDisasterUtility.GenerateFactionRatkinPawn(kindDef, null, stage, 0.28f);
                if (pawn == null)
                {
                    continue;
                }

                pawn.SetFaction(null);
                GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(cell, map, 4), map);
                MouseDisasterUtility.StripRatEggInventory(pawn);
                pawn.health.AddHediff(MouseDisasterDefOf.MouseDisaster_FamineRefugee);
                pawns.Add(pawn);
            }

            if (pawns.Count == 0)
            {
                return false;
            }

            MouseDisasterVisitorUtility.RegisterVisitors(pawns);
            ChoiceLetter_FamineRefugees letter = LetterMaker.MakeLetter(def.letterLabel, def.letterText, MouseDisasterDefOf.MouseDisaster_AcceptFamineRefugees, pawns) as ChoiceLetter_FamineRefugees;
            if (letter == null)
            {
                MouseDisasterUtility.DestroyFailedIncidentPawns(pawns);
                return false;
            }

            letter.refugees = pawns;
            letter.map = map;
            Find.LetterStack.ReceiveLetter(letter, null);
            return true;
        }
    }
}
