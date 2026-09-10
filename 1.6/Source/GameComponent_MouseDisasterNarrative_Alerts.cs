using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MouseDisaster
{
    public sealed class MouseDisasterJournalEntry : IExposable
    {
        public string label;
        public string text;
        public bool read;
        public void ExposeData()
        {
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref text, "text");
            Scribe_Values.Look(ref read, "read");
        }
    }

    public partial class GameComponent_MouseDisasterNarrative
    {
        private List<MouseDisasterJournalEntry> journalEntries = new List<MouseDisasterJournalEntry>();
        private int nextAlertRefresh = -1;
        private int unreadNarratives;
        private int pendingNarratives;
        public int UnreadNarrativeCount { get { RefreshNarrativeAlert(); return unreadNarratives; } }
        public int PendingNarrativeCount { get { RefreshNarrativeAlert(); return pendingNarratives; } }

        private void RefreshNarrativeAlert()
        {
            if (CurrentNarrativeTick < nextAlertRefresh) return;
            nextAlertRefresh = CurrentNarrativeTick + 60;
            unreadNarratives = journalEntries.Count(e => !e.read);
            pendingNarratives = narrativeVisits.Count(v => v.showLetter && !v.resolved) +
            n004Records.Count(r => r.outcome == MouseDisasterN004Outcome.Pending) +
            n005Records.Count(r => r.outcome == MouseDisasterN005Outcome.Accepted && !r.careResolved) +
            n006Records.Count(r => r.phase != MouseDisasterN006Phase.Tracking && r.phase != MouseDisasterN006Phase.Resolved) +
            n007Records.Count(r => r.phase != MouseDisasterN007Phase.Resolved) +
            (envoyPhase > 0 && envoyPhase < 4 ? 1 : 0) + (relicStarted && relicOutcome == 0 ? 1 : 0) +
            (returnPhase == 2 ? 1 : 0);
        }

        private void ExposeNarrativeAlerts()
        {
            Scribe_Collections.Look(ref journalEntries, "mouseDisaster_journalEntries", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                journalEntries ??= new List<MouseDisasterJournalEntry>();
                journalEntries.RemoveAll(e => e == null);
            }
        }

        public void OpenNarrativeJournal()
        {
            var options = new List<FloatMenuOption>();
            string progress = "MouseDisaster_Story_AlertProgress".Translate(
                narrativeVisits.Count(v => v.showLetter && !v.resolved),
                n005Records.Count(r => r.outcome == MouseDisasterN005Outcome.Accepted && !r.careResolved),
                n007Records.Count(r => r.phase != MouseDisasterN007Phase.Resolved),
                envoyPhase > 0 && envoyPhase < 4 ? 1 : 0, relicStarted && relicOutcome == 0 ? 1 : 0);
            options.Add(new FloatMenuOption("MouseDisaster_Story_Progress".Translate(),
                () => Find.WindowStack.Add(new Dialog_MessageBox(progress))));
            foreach (var record in n004Records.Where(r => r.outcome == MouseDisasterN004Outcome.Pending))
                AddNarrativeTarget(options, "N004", record.mother);
            foreach (var record in n005Records.Where(r => r.outcome == MouseDisasterN005Outcome.Accepted && !r.careResolved))
                AddNarrativeTarget(options, "N005", record.exchangeChildren.FirstOrDefault(p => p != null && p.Spawned));
            foreach (var record in n006Records.Where(r => r.phase != MouseDisasterN006Phase.Tracking && r.phase != MouseDisasterN006Phase.Resolved))
                AddNarrativeTarget(options, "N006", record.burrow);
            foreach (var record in n007Records.Where(r => r.phase != MouseDisasterN007Phase.Resolved))
                AddNarrativeTarget(options, "N007", record.pawnRecords.Select(p => p.pawn).FirstOrDefault(p => p != null && p.Spawned));
            if (envoyPhase > 0 && envoyPhase < 4) AddNarrativeTarget(options, "N008", envoy);
            if (relicStarted && relicOutcome == 0)
            {
                if (storyCache?.Spawned == true) AddNarrativeTarget(options, "N009", storyCache);
                else if (storySite != null) options.Add(new FloatMenuOption("MouseDisaster_Story_ToggleN009".Translate(),
                    () => CameraJumper.TryJumpAndSelect(storySite)));
            }
            if (returnPhase == 2) AddNarrativeTarget(options, "N007Return", recoveredVisitor);
            foreach (var entry in journalEntries.AsEnumerable().Reverse())
            {
                var captured = entry;
                options.Add(new FloatMenuOption((entry.read ? "" : "* ") + entry.label,
                    () => Find.WindowStack.Add(new Dialog_MessageBox(captured.text,
                        "MouseDisaster_Story_MarkRead".Translate(), () => { captured.read = true; nextAlertRefresh = -1; },
                        "Close".Translate(), null, captured.label))));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void AddNarrativeTarget(List<FloatMenuOption> options, string id, Thing target)
        {
            if (target?.Spawned != true) return;
            options.Add(new FloatMenuOption(("MouseDisaster_Story_Toggle" + id).Translate() + ": " + target.LabelShortCap,
                () => CameraJumper.TryJumpAndSelect(target)));
        }

        private static void ShowNarrativeEnding(string label, string body)
        {
            GameVictoryUtility.ShowCredits(label + "\n\n" + body, null, exitToMainMenu: false);
            ReceiveNarrativeLetterText(label, body, null);
        }
    }

    public class Alert_MouseDisasterNarrative : Alert
    {
        private GameComponent_MouseDisasterNarrative Narrative => Current.Game?.GetComponent<GameComponent_MouseDisasterNarrative>();
        public Alert_MouseDisasterNarrative() { defaultPriority = AlertPriority.Medium; }
        public override string GetLabel() => Suin
            ? "MouseDisaster_Story_AlertTrustLabel".Translate(Narrative?.NarratorTrust ?? 0).ToString()
            : "MouseDisaster_Story_AlertLabel".Translate().ToString();
        private bool Suin => Find.Storyteller?.def?.defName == GameComponent_MouseDisasterNarrative.NarratorDefName;
        public override TaggedString GetExplanation() => "MouseDisaster_Story_AlertText".Translate(
            Narrative?.UnreadNarrativeCount ?? 0, Narrative?.PendingNarrativeCount ?? 0).ToString() +
            (Suin ? "\n\n" + "MouseDisaster_Story_Trust".Translate(Narrative?.NarratorTrust ?? 0).ToString() : "");
        public override AlertReport GetReport() => Narrative != null &&
            (Suin || Narrative.UnreadNarrativeCount > 0 || Narrative.PendingNarrativeCount > 0);
        protected override void OnClick() { Narrative?.OpenNarrativeJournal(); }
    }
}
