using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MouseDisaster
{
    public class ChoiceLetter_FamineRefugees : ChoiceLetter
    {
        public List<Pawn> refugees;

        public Map map;

        public override bool CanDismissWithRightClick => false;

        public override bool CanShowInLetterStack
        {
            get
            {
                return base.CanShowInLetterStack && GetValidRefugees().Count > 0;
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

                List<Pawn> validRefugees = GetValidRefugees();
                if (validRefugees.Count == 0)
                {
                    yield return Option_Close;
                    yield break;
                }

                DiaOption accept = new DiaOption("\u5141\u8bb8\u52a0\u5165");
                accept.action = delegate
                {
                    List<Pawn> acceptedRefugees = GetValidRefugees();
                    for (int i = 0; i < acceptedRefugees.Count; i++)
                    {
                        if (!MouseDisasterVisitorUtility.TryJoin(acceptedRefugees[i]))
                        {
                            acceptedRefugees[i].SetFaction(Faction.OfPlayer);
                        }
                    }

                    Messages.Message("\u4f60\u540c\u610f\u4e86\u707e\u8352\u9003\u96be\u8005\u7684\u52a0\u5165\u8bf7\u6c42\u3002", acceptedRefugees, MessageTypeDefOf.PositiveEvent, historical: false);
                    Find.LetterStack.RemoveLetter(this);
                };
                accept.resolveTree = true;

                List<Pawn> recruitableRefugees = MouseDisasterVisitorUtility.GetRecruitableVisitors(validRefugees);
                DiaOption temporaryRecruit = new DiaOption("MouseDisaster_VisitorControl_TemporaryRecruit".Translate(MouseDisasterVisitorUtility.TemporaryRecruitDurationDays.ToString()));
                if (recruitableRefugees.Count == 0)
                {
                    temporaryRecruit.Disable(null);
                }
                else
                {
                    temporaryRecruit.action = delegate
                    {
                        if (MouseDisasterVisitorUtility.TryTemporaryRecruitAll(recruitableRefugees, out int recruitedCount))
                        {
                            Messages.Message("MouseDisaster_VisitorControl_TemporaryRecruit_Success".Translate(recruitedCount, MouseDisasterVisitorUtility.TemporaryRecruitDurationDays), recruitableRefugees, MessageTypeDefOf.PositiveEvent, historical: false);
                        }
                        else
                        {
                            Messages.Message("MouseDisaster_VisitorControl_NoValidTarget".Translate(), MessageTypeDefOf.RejectInput);
                        }

                        Find.LetterStack.RemoveLetter(this);
                    };
                    temporaryRecruit.resolveTree = true;
                }

                DiaOption attackAll = new DiaOption("MouseDisaster_VisitorControl_AttackAll".Translate());
                if (recruitableRefugees.Count == 0)
                {
                    attackAll.Disable(null);
                }
                else
                {
                    attackAll.action = delegate
                    {
                        if (MouseDisasterVisitorUtility.TryMakeHostile(recruitableRefugees, out int hostileCount) && hostileCount > 0)
                        {
                            Messages.Message("MouseDisaster_VisitorControl_AttackAll_Success".Translate(hostileCount), recruitableRefugees, MessageTypeDefOf.ThreatBig, historical: false);
                        }
                        else
                        {
                            Messages.Message("MouseDisaster_VisitorControl_NoValidTarget".Translate(), MessageTypeDefOf.RejectInput);
                        }

                        Find.LetterStack.RemoveLetter(this);
                    };
                    attackAll.resolveTree = true;
                }

                DiaOption reject = new DiaOption("\u62d2\u7edd");
                reject.action = delegate
                {
                    List<Pawn> currentRefugees = GetValidRefugees();
                    Map targetMap = ResolveMap(currentRefugees);
                    if (targetMap != null && currentRefugees.Count > 0)
                    {
                        MouseDisasterUtility.MakeTravelAndExitLord(targetMap, currentRefugees, targetMap.Center, includeBabiesInExit: false);
                    }

                    Messages.Message("\u4f60\u62d2\u7edd\u4e86\u707e\u8352\u9003\u96be\u8005\uff0c\u4ed6\u4eec\u6b63\u5728\u79bb\u5f00\u5730\u56fe\u3002", currentRefugees, MessageTypeDefOf.NeutralEvent, historical: false);
                    Find.LetterStack.RemoveLetter(this);
                };
                reject.resolveTree = true;

                yield return accept;
                yield return temporaryRecruit;
                yield return attackAll;
                yield return reject;
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
            Scribe_Collections.Look(ref refugees, "refugees", LookMode.Reference);
            Scribe_References.Look(ref map, "map");
        }

        public override void OpenLetter()
        {
            if (GetValidRefugees().Count == 0)
            {
                Find.LetterStack.RemoveLetter(this);
                return;
            }

            base.OpenLetter();
        }

        private List<Pawn> GetValidRefugees()
        {
            return refugees?.Where(pawn => pawn != null && !pawn.Dead && pawn.Spawned).ToList() ?? new List<Pawn>();
        }

        private Map ResolveMap(List<Pawn> validRefugees)
        {
            if (map != null)
            {
                return map;
            }

            return validRefugees.FirstOrDefault()?.Map;
        }
    }
}
