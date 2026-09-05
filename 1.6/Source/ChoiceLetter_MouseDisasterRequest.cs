using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MouseDisaster
{
    public class ChoiceLetter_MouseDisasterRequest : ChoiceLetter
    {
        public Map map;
        public int amount;
        public MouseDisasterRequestKind requestKind;
        public bool createsIntelSite;
        public MouseDisasterIntelSiteKind intelSiteKind;

        public override bool CanDismissWithRightClick => false;

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                if (ArchivedOnly)
                {
                    yield return Option_Close;
                    yield break;
                }

                DiaOption accept = new DiaOption("\u4ea4\u4ed8");
                accept.action = delegate
                {
                    Site site = null;
                    if (createsIntelSite && !MouseDisasterPhase2Utility.TryCreateIntelSite(map, intelSiteKind, out site, out string intelFailure))
                    {
                        Messages.Message(intelFailure, MessageTypeDefOf.RejectInput);
                        return;
                    }

                    if (!MouseDisasterPhase2Utility.TryConsumeRequest(map, requestKind, amount, out string failure))
                    {
                        if (site != null)
                        {
                            Find.WorldObjects.Remove(site);
                        }

                        Messages.Message(failure, MessageTypeDefOf.RejectInput);
                        return;
                    }

                    if (createsIntelSite)
                    {
                        Find.LetterStack.ReceiveLetter("\u9f20\u707e\u60c5\u62a5", "\u9f20\u707e\u63d0\u4f9b\u4e86\u4e00\u4e2a\u65b0\u5730\u70b9\u7684\u60c5\u62a5\u3002", LetterDefOf.PositiveEvent, site);
                    }
                    else
                    {
                        Messages.Message("\u4f60\u6ee1\u8db3\u4e86\u8fd9\u6b21\u9f20\u707e\u8bf7\u6c42\u3002", MessageTypeDefOf.PositiveEvent);
                    }

                    Find.LetterStack.RemoveLetter(this);
                };
                accept.resolveTree = true;

                DiaOption reject = new DiaOption("\u62d2\u7edd");
                reject.action = delegate
                {
                    Messages.Message("\u4f60\u62d2\u7edd\u4e86\u8fd9\u6b21\u9f20\u707e\u8bf7\u6c42\u3002", MessageTypeDefOf.NeutralEvent);
                    Find.LetterStack.RemoveLetter(this);
                };
                reject.resolveTree = true;

                yield return accept;
                yield return reject;
                yield return Option_Postpone;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref map, "map");
            Scribe_Values.Look(ref amount, "amount", 0);
            Scribe_Values.Look(ref requestKind, "requestKind", MouseDisasterRequestKind.SimpleMeal);
            Scribe_Values.Look(ref createsIntelSite, "createsIntelSite", false);
            Scribe_Values.Look(ref intelSiteKind, "intelSiteKind", MouseDisasterIntelSiteKind.Treasure);
        }
    }
}
