using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MouseDisaster
{
    public class GameComponent_MouseDisasterBroadcastHope : GameComponent
    {
        private class BroadcastHopeSchedule : IExposable
        {
            public int mapId = -1;
            public int remainingTriggers;
            public int nextTriggerTick;

            public void ExposeData()
            {
                Scribe_Values.Look(ref mapId, "mapId", -1);
                Scribe_Values.Look(ref remainingTriggers, "remainingTriggers", 0);
                Scribe_Values.Look(ref nextTriggerTick, "nextTriggerTick", 0);
            }
        }

        private const int BroadcastCheckIntervalTicks = 60;
        private const int RetryIntervalTicks = GenDate.TicksPerHour;
        private const int TriggerIntervalTicks = GenDate.TicksPerHour * 6;
        private const int MaxInitialDelayTicks = GenDate.TicksPerHour * 12;

        private Dictionary<int, int> broadcastCooldownUntilTickByMapId = new Dictionary<int, int>();
        private static readonly string[] BroadcastIncidentDefNames =
        {
            "MouseDisaster_LargeRefugeeWave",
            "MouseDisaster_AbandonedRatkinChildren",
            "MouseDisaster_ShatteredMother",
            "MouseDisaster_BeggarFamily",
            "MouseDisaster_BeggarGroup",
            "MouseDisaster_ThiefRatkinGroup",
            "MouseDisaster_ThiefRatkinChildGroup",
            "MouseDisaster_WildRatkinWandersIn",
            "MouseDisaster_WildRatkinChildWandersIn",
            "MouseDisaster_WildRatkinGroupWandersIn",
            "MouseDisaster_FamineRefugees",
            "MouseDisaster_RatkinTraderCaravan",
            "MouseDisaster_ChildExchange"
        };

        private List<BroadcastHopeSchedule> queuedBroadcasts = new List<BroadcastHopeSchedule>();

        public GameComponent_MouseDisasterBroadcastHope(Game game)
        {
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref queuedBroadcasts, "mouseDisaster_broadcastHopeQueue", LookMode.Deep);
            Scribe_Collections.Look(ref broadcastCooldownUntilTickByMapId, "mouseDisaster_broadcastHopeCooldowns", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && queuedBroadcasts == null)
            {
                queuedBroadcasts = new List<BroadcastHopeSchedule>();
            }
            if (Scribe.mode == LoadSaveMode.PostLoadInit && broadcastCooldownUntilTickByMapId == null)
            {
                broadcastCooldownUntilTickByMapId = new Dictionary<int, int>();
            }
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager == null || queuedBroadcasts == null || queuedBroadcasts.Count == 0)
            {
                return;
            }

            int nowTick = Find.TickManager.TicksGame;
            if (nowTick % BroadcastCheckIntervalTicks != 0)
            {
                return;
            }

            for (int i = queuedBroadcasts.Count - 1; i >= 0; i--)
            {
                BroadcastHopeSchedule schedule = queuedBroadcasts[i];
                if (schedule == null || schedule.remainingTriggers <= 0)
                {
                    queuedBroadcasts.RemoveAt(i);
                    continue;
                }

                Map map = Find.Maps?.FirstOrDefault(m => m.uniqueID == schedule.mapId && m.IsPlayerHome);
                if (map == null)
                {
                    queuedBroadcasts.RemoveAt(i);
                    continue;
                }

                if (nowTick < schedule.nextTriggerTick)
                {
                    continue;
                }

                if (TryExecuteRandomMouseDisaster(map))
                {
                    schedule.remainingTriggers--;
                    if (schedule.remainingTriggers <= 0)
                    {
                        queuedBroadcasts.RemoveAt(i);
                    }
                    else
                    {
                        schedule.nextTriggerTick = nowTick + TriggerIntervalTicks;
                    }
                }
                else
                {
                    schedule.nextTriggerTick = nowTick + RetryIntervalTicks;
                }
            }
        }

        public static bool TryQueueBroadcast(Map map, out int eventCount, out float initialDelayHours)
        {
            eventCount = 0;
            initialDelayHours = 0f;

            if (map == null || Current.Game == null || Find.TickManager == null)
            {
                return false;
            }

            GameComponent_MouseDisasterBroadcastHope component = Current.Game.GetComponent<GameComponent_MouseDisasterBroadcastHope>();
            if (component == null)
            {
                return false;
            }

            if (component.IsMapOnCooldown(map, out _))
            {
                return false;
            }

            int delayTicks = Rand.RangeInclusive(0, MaxInitialDelayTicks);
            int triggerCount = Rand.RangeInclusive(1, 3);
            int cooldownDays = MouseDisasterBroadcastHopePolicy.NormalizeCooldownDays(MouseDisasterMod.Settings?.broadcastHopeCooldownDays ?? 3);

            component.queuedBroadcasts.Add(new BroadcastHopeSchedule
            {
                mapId = map.uniqueID,
                remainingTriggers = triggerCount,
                nextTriggerTick = Find.TickManager.TicksGame + delayTicks
            });

            if (cooldownDays > 0)
            {
                component.broadcastCooldownUntilTickByMapId[map.uniqueID] = Find.TickManager.TicksGame + cooldownDays * GenDate.TicksPerDay;
            }
            else
            {
                component.broadcastCooldownUntilTickByMapId.Remove(map.uniqueID);
            }

            eventCount = triggerCount;
            initialDelayHours = delayTicks / (float)GenDate.TicksPerHour;
            return true;
        }

        public static bool TryGetBroadcastCooldownRemainingDays(Map map, out float remainingDays)
        {
            remainingDays = 0f;
            if (map == null || Current.Game == null || Find.TickManager == null)
            {
                return false;
            }

            GameComponent_MouseDisasterBroadcastHope component = Current.Game.GetComponent<GameComponent_MouseDisasterBroadcastHope>();
            return component != null && component.IsMapOnCooldown(map, out remainingDays);
        }

        private bool IsMapOnCooldown(Map map, out float remainingDays)
        {
            remainingDays = 0f;
            if (map == null || broadcastCooldownUntilTickByMapId == null || !broadcastCooldownUntilTickByMapId.TryGetValue(map.uniqueID, out int untilTick))
            {
                return false;
            }

            int nowTick = Find.TickManager?.TicksGame ?? 0;
            if (untilTick <= nowTick)
            {
                broadcastCooldownUntilTickByMapId.Remove(map.uniqueID);
                return false;
            }

            remainingDays = (untilTick - nowTick) / (float)GenDate.TicksPerDay;
            return true;
        }

        private static bool TryExecuteRandomMouseDisaster(Map map)
        {
            if (map == null)
            {
                return false;
            }

            List<IncidentDef> candidates = BroadcastIncidentDefNames
                .Select(DefDatabase<IncidentDef>.GetNamedSilentFail)
                .Where(def => def?.Worker != null)
                .InRandomOrder()
                .ToList();

            for (int i = 0; i < candidates.Count; i++)
            {
                IncidentDef incident = candidates[i];
                IncidentParms parms = StorytellerUtility.DefaultParmsNow(incident.category, map);
                if (!incident.Worker.CanFireNow(parms))
                {
                    continue;
                }

                if (incident.Worker.TryExecute(parms))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
