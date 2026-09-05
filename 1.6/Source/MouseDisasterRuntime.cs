namespace MouseDisaster
{
    public static class MouseDisasterRuntime
    {
        public static bool AllowsNewContent => MouseDisasterMod.Settings == null || MouseDisasterMod.Settings.enableNewContent;
    }
}
