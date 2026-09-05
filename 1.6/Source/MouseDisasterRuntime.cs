namespace MouseDisaster
{
    public static class MouseDisasterRuntime
    {
        public static bool AllowsNewContent => GameComponent_MouseDisasterNarrative.DebugForcing || MouseDisasterMod.Settings == null || MouseDisasterMod.Settings.enableNewContent;
    }
}
