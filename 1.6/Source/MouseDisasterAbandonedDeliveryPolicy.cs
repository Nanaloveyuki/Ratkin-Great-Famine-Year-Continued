namespace MouseDisaster
{
    public static class MouseDisasterAbandonedDeliveryPolicy
    {
        public const int AdultArrivalStallTimeoutTicks = 3 * 60 * 60;

        public static bool ShouldUseLeadYourPetDropoff(bool leadYourPetEnabled, int childCount)
        {
            return leadYourPetEnabled && childCount > 0;
        }

        public static bool ShouldSpawnChildrenAtDropoff(bool leadYourPetEnabled)
        {
            return false;
        }

        public static bool ShouldUseVanillaCarryDelivery(bool leadYourPetEnabled, bool allChildrenArrived)
        {
            return !leadYourPetEnabled && !allChildrenArrived;
        }

        public static bool ShouldRegisterAsVisitorChoiceTargets()
        {
            return false;
        }

        public static bool ShouldTryAnyFoodFallback(bool foundHomeAreaFood, bool foundAnyFoodThing)
        {
            return !foundHomeAreaFood && foundAnyFoodThing;
        }

        public static bool ShouldFallbackToStoredFoodCell(bool foundHomeAreaFood, bool foundAnyFoodThing)
        {
            return !foundHomeAreaFood && !foundAnyFoodThing;
        }

        public static bool ShouldReleaseLeadYourPetDropoffLeashes(bool leadYourPetEnabled, bool allChildrenArrived)
        {
            return leadYourPetEnabled && allChildrenArrived;
        }

        public static bool ShouldForceAdultLeaveAfterArrivalStall(bool adultArrivedAtDropoff, int ticksWaitingSinceArrival)
        {
            return adultArrivedAtDropoff && ticksWaitingSinceArrival >= AdultArrivalStallTimeoutTicks;
        }
    }
}
