#nullable enable

using System;

namespace Immutable.Audience
{
    internal static class Log
    {
        // Prepended to every SDK log line. Internal so the sample-app log adapter can strip it.
        internal const string Prefix = "[ImmutableAudience]";
        internal const string WarnPrefix = Prefix + " WARN:";

        internal static bool Enabled { get; set; }

        // Tests set this to capture output; AudienceUnityHooks sets it to Debug.Log.
        internal static Action<string>? Writer { get; set; }

        internal static void Debug(string message)
        {
            if (!Enabled) return;
            Emit($"{Prefix} {message}");
        }

        internal static void Warn(string message) =>
            Emit($"{WarnPrefix} {message}");

        private static void Emit(string line)
        {
            // Swallow anything the Writer or Console throws so Log.Warn and
            // Log.Debug never throw themselves. If they did, an exception from
            // logging inside a catch block would reach the background timer
            // and crash the game on modern .NET.
            try
            {
                if (Writer != null)
                {
                    Writer(line);
                    return;
                }
                Console.WriteLine(line);
            }
            catch
            {
            }
        }
    }

    // ArgumentException messages thrown from public SDK methods.
    internal static class AudienceArgumentMessages
    {
        // Init / config validation
        internal const string PublishableKeyRequired = "PublishableKey is required";
        internal const string PersistentDataPathRequired = "PersistentDataPath is required";

        // Typed-event ToProperties validation
        internal const string ProgressionStatusRequired = "Progression.Status is required. Set it before calling Track(IEvent).";
        internal const string ResourceFlowRequired = "Resource.Flow is required. Set it before calling Track(IEvent).";
        internal const string ResourceCurrencyRequired = "Resource.Currency is required. Set a non-empty string before calling Track(IEvent).";
        internal const string ResourceAmountRequired = "Resource.Amount is required. Set it before calling Track(IEvent).";
        internal const string PurchaseValueRequired = "Purchase.Value is required. Set it before calling Track(IEvent).";
        internal const string MilestoneReachedNameRequired = "MilestoneReached.Name must not be null or empty";

        internal static string PurchaseCurrencyInvalid(string? currency) =>
            $"Purchase.Currency '{currency}' must be a three-letter uppercase ISO 4217 code";
    }

    // Error messages we pass to AudienceConfig.OnError.
    internal static class AudienceErrorMessages
    {
        internal static string LocalStorageReadFailed(Exception ex) =>
            $"Local storage read failed: {ex.Message}";

        internal static string BatchPartiallyRejected(int rejected, int total) =>
            $"Batch partially rejected: {rejected} of {total} events dropped";

        internal const string BatchRejectedPrefix = "Batch rejected";
        internal const string ServerErrorWillRetryPrefix = "Server error, will retry";

        internal static string ConsentSyncFailedWithStatus(int statusCode) =>
            $"Consent sync failed with status {statusCode}";

        internal static string ConsentSyncThrew(Exception ex) =>
            $"Consent sync threw: {ex.Message}";
    }

    internal static class AudienceLogs
    {
        // Marker shared by Track / Identify / Alias dropped-event log messages.
        internal const string DroppingMarker = "Dropping";


        // ---- Init / config validation ----

        internal const string InitCalledTwice =
            "Init called more than once. Ignoring; original config retained. " +
            "Call Shutdown() first if reconfiguring is intended.";

        internal static readonly string TestKeyAgainstProduction =
            $"Publishable key has the test prefix ({Constants.TestKeyPrefix}) but BaseUrl points to production. " +
            "The backend will reject events with 401. Either remove the BaseUrl override (test keys " +
            "default to sandbox) or use a non-test publishable key.";

        internal static readonly string NonTestKeyAgainstSandbox =
            "Publishable key is not a test key but BaseUrl points to sandbox. " +
            "The backend will reject events with 401. Either remove the BaseUrl override (non-test " +
            $"keys default to production) or use a test publishable key ({Constants.TestKeyPrefix}).";

        // ---- Track ----

        internal const string TrackIEventNull =
            "Track(IEvent) called with null event. Dropping.";

        internal const string TrackStringEmptyName =
            "Track(string) called with null or empty event name. Dropping.";

        internal static string TrackIEventThrew(string evtTypeName, Exception ex) =>
            $"Track(IEvent): {evtTypeName}.ToProperties()/EventName threw {ex.GetType().Name}: {ex.Message}. Dropping.";

        internal static string TrackIEventEmptyName(string evtTypeName) =>
            $"Track(IEvent): {evtTypeName}.EventName returned null or empty. Dropping.";

        // ---- Identify / Alias ----

        internal const string IdentifyEmptyUserId =
            "Identify called with null or empty userId. Dropping.";

        internal const string AliasEmptyIds =
            "Alias called with null or empty fromId/toId. Dropping.";

        internal static string IdentifyDiscarded(ConsentLevel current) =>
            $"Identify discarded. Requires Full consent, current is {current}.";

        internal static string AliasDiscarded(ConsentLevel current) =>
            $"Alias discarded. Requires Full consent, current is {current}.";

        // ---- Consent / Shutdown ----

        internal static string ConsentPersistFailed(Exception ex) =>
            $"SetConsent: failed to persist consent level. {ex.GetType().Name}: {ex.Message}. " +
            "In-memory level is updated but will revert on next launch.";

        internal static string ShutdownFlushExceeded(int timeoutMs) =>
            $"Shutdown flush exceeded {timeoutMs}ms. Abandoning. " +
            "Queued events remain on disk and will retry on next startup.";

        internal static string ShutdownFlushThrew(Exception ex) =>
            $"Shutdown flush threw: {ex.GetType().Name}: {ex.Message}";

        // ---- onError handler swallow ----

        internal static string OnErrorThrew(Exception ex) =>
            $"onError threw {ex.GetType().Name}: {ex.Message}";

        // ---- Send loop ----

        internal static string SendBatchUnexpected(Exception ex) =>
            $"SendBatch unexpected exception: {ex.GetType().Name}: {ex.Message}";

        internal static string ParseRejectedCountThrew(Exception ex) =>
            $"ParseRejectedCount threw {ex.GetType().Name}: {ex.Message}";

        // ---- Session ----

        internal const string SessionPauseAlreadyPaused =
            "Session: Pause while already paused. Ignoring.";

        internal const string SessionHeartbeatTimeout =
            "Session: heartbeat callback did not complete within 1s on timer stop. " +
            "A trailing session_heartbeat may race with the next session lifecycle event.";

        internal static string SessionTrackCallbackThrew(string eventName, Exception ex) =>
            $"Session: {eventName} track callback threw {ex.GetType().Name}. Event dropped.";

        // ---- Context providers ----

        internal static string ContextProviderThrew(Exception ex) =>
            $"ContextProvider threw {ex.GetType().Name}: {ex.Message}. " +
            "Event ships with base context only.";

        internal static string LaunchContextProviderThrew(Exception ex) =>
            $"LaunchContextProvider threw {ex.GetType().Name}: {ex.Message}. " +
            "game_launch will ship without auto-detected Unity context.";
    }
}
