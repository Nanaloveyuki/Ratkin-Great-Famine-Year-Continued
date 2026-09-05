using Verse;

namespace MouseDisasterContinuedGuard
{
    [StaticConstructorOnStartup]
    internal static class DuplicateModGuard
    {
        private const string OriginalPackageId = "lezhizhong.mouse.disaster.famine";

        static DuplicateModGuard()
        {
            if (ModLister.GetActiveModWithIdentifier(OriginalPackageId, true) == null)
            {
                return;
            }

            DelayedErrorWindowRequest.Add(
                "MouseDisasterContinued_DuplicateMod_Body".Translate().ToString(),
                "MouseDisasterContinued_DuplicateMod_Title".Translate().ToString());
        }
    }
}
