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
    }
}
