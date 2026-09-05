using System.Collections.Generic;
using Verse;

namespace MouseDisaster
{
    public class GameComponent_MouseDisasterPendingState : GameComponent
    {
        private List<int> forcePrisonerOnPurchasePawnIds = new List<int>();
        private List<int> tradableChattelPawnIds = new List<int>();
        private List<int> childExchangeMoodPawnIds = new List<int>();
        private List<int> siegeBeggarPawnIds = new List<int>();
        private List<int> siegeBeggarStoleFoodSuccessPawnIds = new List<int>();
        private List<int> strongSiegePawnIds = new List<int>();
        private Dictionary<int, int> airDropStayUntilTickByPawnId = new Dictionary<int, int>();
        private Dictionary<int, int> wallGnawCounts = new Dictionary<int, int>();

        public GameComponent_MouseDisasterPendingState(Game game)
        {
        }

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                forcePrisonerOnPurchasePawnIds = MouseDisasterUtility.CopyForcePrisonerOnPurchasePawnIds();
                tradableChattelPawnIds = MouseDisasterUtility.CopyTradableChattelPawnIds();
                childExchangeMoodPawnIds = MouseDisasterUtility.CopyChildExchangeMoodPawnIds();
                siegeBeggarPawnIds = MouseDisasterUtility.CopySiegeBeggarPawnIds();
                siegeBeggarStoleFoodSuccessPawnIds = MouseDisasterUtility.CopySiegeBeggarStoleFoodSuccessPawnIds();
                strongSiegePawnIds = MouseDisasterUtility.CopyStrongSiegePawnIds();
                airDropStayUntilTickByPawnId = MouseDisasterUtility.CopyAirDropStayUntilTickByPawnId();
                wallGnawCounts = MouseDisasterUtility.CopyWallGnawCounts();
            }

            Scribe_Collections.Look(ref forcePrisonerOnPurchasePawnIds, "mouseDisaster_forcePrisonerOnPurchasePawnIds", LookMode.Value);
            Scribe_Collections.Look(ref tradableChattelPawnIds, "mouseDisaster_tradableChattelPawnIds", LookMode.Value);
            Scribe_Collections.Look(ref childExchangeMoodPawnIds, "mouseDisaster_childExchangeMoodPawnIds", LookMode.Value);
            Scribe_Collections.Look(ref siegeBeggarPawnIds, "mouseDisaster_siegeBeggarPawnIds", LookMode.Value);
            Scribe_Collections.Look(ref siegeBeggarStoleFoodSuccessPawnIds, "mouseDisaster_siegeBeggarStoleFoodSuccessPawnIds", LookMode.Value);
            Scribe_Collections.Look(ref strongSiegePawnIds, "mouseDisaster_strongSiegePawnIds", LookMode.Value);
            Scribe_Collections.Look(ref airDropStayUntilTickByPawnId, "mouseDisaster_airDropStayUntilTickByPawnId", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref wallGnawCounts, "mouseDisaster_wallGnawCounts", LookMode.Value, LookMode.Value);

            MouseDisasterPhase2Utility.ExposePendingStateData();
            MouseDisasterUtility.ExposePendingStateData();

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                forcePrisonerOnPurchasePawnIds ??= new List<int>();
                tradableChattelPawnIds ??= new List<int>();
                childExchangeMoodPawnIds ??= new List<int>();
                siegeBeggarPawnIds ??= new List<int>();
                siegeBeggarStoleFoodSuccessPawnIds ??= new List<int>();
                strongSiegePawnIds ??= new List<int>();
                airDropStayUntilTickByPawnId ??= new Dictionary<int, int>();
                wallGnawCounts ??= new Dictionary<int, int>();
                MouseDisasterUtility.RestorePendingMarkerState(
                    forcePrisonerOnPurchasePawnIds,
                    tradableChattelPawnIds,
                    childExchangeMoodPawnIds,
                    siegeBeggarPawnIds,
                    siegeBeggarStoleFoodSuccessPawnIds,
                    strongSiegePawnIds,
                    airDropStayUntilTickByPawnId,
                    wallGnawCounts);
            }
        }

        public override void StartedNewGame()
        {
            MouseDisasterPhase2Utility.ResetPendingState();
            MouseDisasterUtility.ResetPendingState();
        }

        public override void LoadedGame()
        {
            MouseDisasterPhase2Utility.CleanupLoadedPendingState();
            MouseDisasterUtility.CleanupLoadedPendingState();
        }
    }
}
