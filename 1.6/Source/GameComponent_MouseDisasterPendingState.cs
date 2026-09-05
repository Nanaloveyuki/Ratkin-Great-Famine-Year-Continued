using Verse;

namespace MouseDisaster
{
    public class GameComponent_MouseDisasterPendingState : GameComponent
    {
        public GameComponent_MouseDisasterPendingState(Game game)
        {
        }

        public override void ExposeData()
        {
            MouseDisasterPhase2Utility.ExposePendingStateData();
            MouseDisasterUtility.ExposePendingStateData();
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
