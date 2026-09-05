using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MouseDisaster
{
    public enum MouseDisasterN005Outcome
    {
        Pending,
        Accepted,
        Rejected,
        TimedOut,
        Broken
    }

    public sealed class MouseDisasterN005Record : IExposable
    {
        public int traderId = -1;
        public Pawn trader;
        public Pawn offeredBaby;
        public List<Pawn> exchangeChildren = new List<Pawn>();
        public int mapId = -1;
        public int createdTick;
        public MouseDisasterN005Outcome outcome;

        public void ExposeData()
        {
            Scribe_Values.Look(ref traderId, "traderId", -1);
            Scribe_References.Look(ref trader, "trader");
            Scribe_References.Look(ref offeredBaby, "offeredBaby");
            Scribe_Collections.Look(ref exchangeChildren, "exchangeChildren", LookMode.Reference);
            Scribe_Values.Look(ref mapId, "mapId", -1);
            Scribe_Values.Look(ref createdTick, "createdTick", 0);
            Scribe_Values.Look(ref outcome, "outcome", MouseDisasterN005Outcome.Pending);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                exchangeChildren ??= new List<Pawn>();
                exchangeChildren.RemoveAll(child => child == null);
            }
        }
    }

    public partial class GameComponent_MouseDisasterNarrative
    {
        private List<MouseDisasterN005Record> n005Records = new List<MouseDisasterN005Record>();

        private void ExposeN005Data()
        {
            Scribe_Collections.Look(ref n005Records, "mouseDisaster_narrativeN005Records", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                n005Records ??= new List<MouseDisasterN005Record>();
                n005Records.RemoveAll(record => record == null || record.traderId <= 0 || record.outcome == MouseDisasterN005Outcome.Pending);
            }
        }

        public void RecordN005Outcome(int traderId, Pawn trader, IEnumerable<Pawn> exchangeChildren, Pawn offeredBaby, Map map, MouseDisasterN005Outcome outcome)
        {
            if (traderId <= 0 || outcome == MouseDisasterN005Outcome.Pending)
            {
                return;
            }

            n005Records ??= new List<MouseDisasterN005Record>();
            if (n005Records.Any(record => record != null && record.traderId == traderId))
            {
                return;
            }

            MouseDisasterN005Record record = new MouseDisasterN005Record
            {
                traderId = traderId,
                trader = trader,
                offeredBaby = offeredBaby,
                exchangeChildren = exchangeChildren?.Where(child => child != null).Distinct().ToList() ?? new List<Pawn>(),
                mapId = map?.uniqueID ?? trader?.Map?.uniqueID ?? -1,
                createdTick = CurrentNarrativeTick,
                outcome = outcome
            };
            n005Records.Add(record);
            ChangeNarratorTrust(TrustDeltaForN005Outcome(outcome));

            if (!IsNarratorActive())
            {
                return;
            }

            string suffix = outcome switch
            {
                MouseDisasterN005Outcome.Accepted => "Accepted",
                MouseDisasterN005Outcome.Rejected => "Rejected",
                MouseDisasterN005Outcome.TimedOut => "TimedOut",
                MouseDisasterN005Outcome.Broken => "Broken",
                _ => "Broken"
            };
            ReceiveNarrativeLetter(
                "MouseDisaster_N005_" + suffix + "_Label",
                "MouseDisaster_N005_" + suffix + "_Text",
                ResolveN005Map(record));
        }

        private static int TrustDeltaForN005Outcome(MouseDisasterN005Outcome outcome)
        {
            switch (outcome)
            {
                case MouseDisasterN005Outcome.Accepted:
                    return 3;
                case MouseDisasterN005Outcome.Rejected:
                case MouseDisasterN005Outcome.TimedOut:
                    return -1;
                case MouseDisasterN005Outcome.Broken:
                    return -2;
                default:
                    return 0;
            }
        }

        private static Map ResolveN005Map(MouseDisasterN005Record record)
        {
            Map map = record?.trader?.Map;
            if (map != null)
            {
                return map;
            }

            map = record?.exchangeChildren?.FirstOrDefault(child => child?.Map != null)?.Map;
            if (map != null)
            {
                return map;
            }

            return Find.Maps?.FirstOrDefault(candidate => candidate.uniqueID == record?.mapId);
        }
    }
}
