using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MouseDisaster
{
    public class ChoiceLetter_ChildExchange : ChoiceLetter
    {
        public Pawn trader;

        public Map map;

        public override bool CanDismissWithRightClick => false;

        public override bool CanShowInLetterStack
        {
            get
            {
                return base.CanShowInLetterStack && MouseDisasterUtility.IsChildExchangeTrader(trader);
            }
        }

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                if (ArchivedOnly)
                {
                    yield return Option_Close;
                    yield break;
                }

                if (!MouseDisasterUtility.IsChildExchangeTrader(trader))
                {
                    yield return Option_Close;
                    yield break;
                }

                yield return BuildOption(MouseDisasterUtility.ChildExchangeModeColonist, "\u7528\u6b96\u6c11\u8005\u5a74\u513f\u4ea4\u6362");
                yield return BuildOption(MouseDisasterUtility.ChildExchangeModeSlave, "\u7528\u5974\u96b6\u5a74\u513f\u4ea4\u6362");
                yield return BuildOption(MouseDisasterUtility.ChildExchangeModePrisoner, "\u7528\u56da\u72af\u5a74\u513f\u4ea4\u6362");
                yield return BuildRejectOption();

                if (lookTargets.IsValid())
                {
                    yield return Option_JumpToLocationAndPostpone;
                }

                yield return Option_Postpone;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref trader, "trader");
            Scribe_References.Look(ref map, "map");
        }

        public override void OpenLetter()
        {
            if (!MouseDisasterUtility.IsChildExchangeTrader(trader))
            {
                Find.LetterStack.RemoveLetter(this);
                return;
            }

            base.OpenLetter();
        }

        private DiaOption BuildOption(int mode, string prefix)
        {
            Pawn baby = MouseDisasterUtility.FindExchangeOfferBaby(map ?? trader?.Map, mode);
            string label = baby == null
                ? prefix + "\uff08\u65e0\u53ef\u7528\u76ee\u6807\uff09"
                : prefix + "\uff08" + baby.LabelShortCap + "\uff09";

            DiaOption option = new DiaOption(label);
            if (baby == null)
            {
                option.Disable(null);
                return option;
            }

            option.action = delegate
            {
                if (MouseDisasterUtility.TryExecuteChildExchange(trader, mode, out string message))
                {
                    Messages.Message(message, trader, MessageTypeDefOf.PositiveEvent, historical: false);
                }
                else
                {
                    Messages.Message(message, trader, MessageTypeDefOf.RejectInput, historical: false);
                }

                Find.LetterStack.RemoveLetter(this);
            };
            option.resolveTree = true;
            return option;
        }

        private DiaOption BuildRejectOption()
        {
            DiaOption option = new DiaOption("MouseDisaster_N005_Reject".Translate());
            option.action = delegate
            {
                if (MouseDisasterUtility.TryRejectChildExchange(trader, out string message))
                {
                    Messages.Message(message, trader, MessageTypeDefOf.NeutralEvent, historical: false);
                }
                else
                {
                    Messages.Message(message, trader, MessageTypeDefOf.RejectInput, historical: false);
                }

                Find.LetterStack.RemoveLetter(this);
            };
            option.resolveTree = true;
            return option;
        }

    }
}
