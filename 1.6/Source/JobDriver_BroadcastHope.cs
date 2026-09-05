using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace MouseDisaster
{
    public class JobDriver_BroadcastHope : JobDriver
    {
        private const int BroadcastWarmupTicks = 150;

        private Building_CommsConsole CommsConsole => job.GetTarget(TargetIndex.A).Thing as Building_CommsConsole;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => CommsConsole == null || !CommsConsole.CanUseCommsNow);

            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.InteractionCell).FailOn(() => CommsConsole == null || !CommsConsole.CanUseCommsNow);

            Toil warmup = Toils_General.WaitWith(TargetIndex.A, BroadcastWarmupTicks, true);
            warmup.WithEffect(EffecterDefOf.Radiotalking, TargetIndex.A);
            warmup.handlingFacing = true;
            yield return warmup;

            Toil broadcast = ToilMaker.MakeToil("BroadcastHope");
            broadcast.initAction = delegate
            {
                Building_CommsConsole commsConsole = CommsConsole;
                Map map = commsConsole?.Map;
                if (map == null)
                {
                    return;
                }

                if (GameComponent_MouseDisasterBroadcastHope.TryQueueBroadcast(map, out int count, out float delayHours))
                {
                    string message = "\u5e7f\u64ad\u5df2\u5b89\u6392\uff1a\u5c06\u5728" + delayHours.ToString("0.0") + "h\u540e\u5f00\u59cb\uff0c\u4e4b\u540e\u6bcf6h\u89e6\u53d1\u4e00\u6b21\uff0c\u5171" + count + "\u6b21\u9f20\u707e\u4e8b\u4ef6\u3002";
                    Messages.Message(message, new TargetInfo(commsConsole.Position, map), MessageTypeDefOf.NeutralEvent, historical: true);
                }
                else
                {
                    Messages.Message("\u5e7f\u64ad\u5931\u8d25\uff1a\u5f53\u524d\u5730\u56fe\u65e0\u6cd5\u5b89\u6392\u9f20\u707e\u4e8b\u4ef6\u3002", new TargetInfo(pawn.PositionHeld, pawn.MapHeld), MessageTypeDefOf.RejectInput, historical: false);
                }
            };
            broadcast.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return broadcast;
        }
    }
}
