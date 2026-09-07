using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace MouseDisaster
{

    public class IncidentWorker_MouseDisasterPlagueRevenge : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map map && base.CanFireNowSub(parms) && map.IsPlayerHome;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            MouseDisasterPhase3Utility.SpawnPlagueRevengeWave(map, 16);
            SendStandardLetter(def.letterLabel, def.letterText, def.letterDef, parms, new TargetInfo(map.Center, map));
            return true;
        }
    }
}
