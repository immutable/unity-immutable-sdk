namespace Immutable.Audience.Tests
{
    // Per-test event names. Each names what the test measures.
    internal static class TestEventNames
    {
        internal const string Warmup = "warmup";
        internal const string WarmupEvent = "warmup_event";
        internal const string ExplicitTrackEvent = "explicit_track_event";
        internal const string UnitTestEvent = "unit_test_event";
        internal const string CraftingStarted = "crafting_started";
        internal const string MainMenuOpened = "main_menu_opened";
        internal const string ShouldNotAppear = "should_not_appear";
        internal const string BeforeReset = "before_reset";
        internal const string AfterReset = "after_reset";
        internal const string EventUnderOldConsent = "event_under_old_consent";
        internal const string RacingEvent = "racing_event";
        internal const string RaceStress = "race_stress";
        internal const string ShouldNotCrash = "should_not_crash";
        internal const string EnsureNonemptyQueue = "ensure_nonempty_queue";
        internal const string TrackedBeforeDowngrade = "tracked_before_downgrade";
        internal const string TrackedAfterDowngrade = "tracked_after_downgrade";
        internal const string EventToSend = "event_to_send";
        internal const string EventPreBlock = "event_pre_block";
        internal const string EventAgainstProdWithTestKey = "event_against_prod_with_test_key";
        internal const string StressTrack = "stress_track";
        internal const string MixedLoadTrack = "mixed_load_track";
        internal const string SteadyState = "steady_state";
        internal const string LevelComplete = "level_complete";

        // EventQueue scenario names where the event name is meaningful in the test description.
        internal const string IntervalFlush = "interval_flush";
        internal const string DisposeTest = "dispose_test";

        // Placeholder event names for tests where the event name itself is irrelevant.
        internal const string PlaceholderA = "a";
        internal const string PlaceholderB = "b";
        internal const string PlaceholderX = "x";
        internal const string PlaceholderY = "y";
        internal const string PlaceholderTest = "test";
        internal const string PlaceholderTrack = "track";
        internal const string PlaceholderIgnored = "ignored";
        internal const string PlaceholderEvt = "evt";

        // Prefix for OfflineResilienceTests' $"blocked_{i}" loop.
        internal const string BlockedPrefix = "blocked_";
    }
}
