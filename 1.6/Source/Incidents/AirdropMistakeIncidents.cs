using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace MouseDisaster
{
    public abstract class IncidentWorker_MouseDisasterAirdropMistakeBase : IncidentWorker
    {
        protected abstract bool InfectsWithPlague { get; }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map map &&
                   base.CanFireNowSub(parms) &&
                   MouseDisasterUtility.TryFindEntryCell(map, out _) &&
                   Find.Storyteller.difficulty.ChildrenAllowed;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            Faction faction = MouseDisasterPhase3Utility.ResolveVisitorFaction();
            List<Thing> payload = new List<Thing>();
            int count = MouseDisasterUtility.CalculateEscalatingGroupCount(parms.points, 4, 14, 80f);
            for (int i = 0; i < count; i++)
            {
                Pawn pawn = MouseDisasterPhase3Utility.CreateRatEggPawn(faction, babyStage: true, thiefLike: false, pureNegative: false, infect: InfectsWithPlague, foodLevel: 0.14f);
                if (pawn == null)
                {
                    continue;
                }

                MouseDisasterPhase3Utility.PrepareStrandedAirdroppedEgg(pawn);
                payload.Add(pawn);
            }

            if (payload.Count == 0)
            {
                return false;
            }

            DropPodUtility.DropThingsNear(DropCellFinder.TradeDropSpot(map), map, payload, 110, canInstaDropDuringInit: false, leaveSlag: false, canRoofPunch: true, forbid: true, allowFogged: true, faction);
            SendStandardLetter(def.letterLabel, def.letterText, def.letterDef, parms, payload);
            return true;
        }
    }

    public class IncidentWorker_MouseDisasterAirdropMistake : IncidentWorker_MouseDisasterAirdropMistakeBase
    {
        protected override bool InfectsWithPlague => false;
    }

    public class IncidentWorker_MouseDisasterPlagueAirdropMistake : IncidentWorker_MouseDisasterAirdropMistakeBase
    {
        protected override bool InfectsWithPlague => true;
    }
}
