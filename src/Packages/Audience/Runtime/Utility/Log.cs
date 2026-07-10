#nullable enable

using System;

namespace Immutable.Audience
{
    internal static class Log
    {
        private const string Prefix = "[ImmutableAudience]";

        internal static bool Enabled { get; set; }

        // Tests set this to capture output; AudienceUnityHooks sets it to Debug.Log.
        internal static Action<string>? Writer { get; set; }

        internal static void Debug(string message)
        {
            if (!Enabled) return;
            Emit($"{Prefix} {message}");
        }

        internal static void Warn(string message) =>
            Emit($"{Prefix} WARN: {message}");

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

    internal static class AudienceLogs
    {
        // ---- Init / config validation ----

        internal const string InitCalledTwice =
            "Init called more than once. Ignoring; original config retained. " +
            "Call Shutdown() first if reconfiguring is intended.";

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

        internal static string IdentifyPassportIdInvalidFormat(string userId) =>
            $"Identify called with identityType Passport but userId \"{userId}\" doesn't look like a " +
            "Passport ID (expected a format like \"email|123\" or a UUID). Check you're passing the " +
            "Passport user ID, not your own internal user ID. Call ignored.";

        internal static string AliasDiscarded(ConsentLevel current) =>
            $"Alias discarded. Requires Full consent, current is {current}.";

        internal static string AliasPassportIdInvalidFormat(string side, string id) =>
            $"Alias called with {side}Type Passport but {side}Id \"{id}\" doesn't look like a " +
            "Passport ID (expected a format like \"email|123\" or a UUID). Check you're passing the " +
            "Passport user ID, not your own internal user ID. Call ignored.";

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

        internal static string MobileAttributionProviderThrew(Exception ex) =>
            $"MobileAttributionProvider threw {ex.GetType().Name}: {ex.Message}. " +
            "game_launch will ship without skanRegistered.";

        internal static string MobileAttributionContextProviderThrew(Exception ex) =>
            $"MobileAttributionContextProvider threw {ex.GetType().Name}: {ex.Message}. " +
            "game_launch will ship without iOS attribution context.";

        internal static string TrackingAuthorizationRequestThrew(Exception ex) =>
            $"RequestTrackingAuthorizationAsync threw {ex.GetType().Name}: {ex.Message}. " +
            "Returning NotDetermined.";

        internal static string MobileInstallReferrerProviderThrew(Exception ex) =>
            $"MobileInstallReferrerProvider threw {ex.GetType().Name}: {ex.Message}. " +
            "install_referrer_received will not fire on this launch.";

        internal static string InstallReferrerSentMarkerWriteFailed(Exception ex) =>
            $"Failed to write install_referrer_sent marker: {ex.GetType().Name}: {ex.Message}. " +
            "install_referrer_received may re-fire on the next launch.";

        internal static string ATTStatusProviderThrew(Exception ex) =>
            $"MobileATTStatusProvider threw {ex.GetType().Name}: {ex.Message}. " +
            "tracking_authorization_changed check skipped.";

        internal static string ATTIDFAProviderThrew(Exception ex) =>
            $"MobileIDFAProvider threw {ex.GetType().Name}: {ex.Message}. " +
            "tracking_authorization_changed will ship without idfa.";

        internal static string GAIDFetchThrew(Exception ex) =>
            $"GAID fetch threw {ex.GetType().Name}: {ex.Message}. " +
            "gaid will not ship on game_launch this session; next launch retries.";

        // ---- Identity ----

        internal static string IdentityRotateFailed(Exception ex) =>
            $"RotateAnonymousId: failed to rewrite identity file. {ex.GetType().Name}: {ex.Message}";

        internal static string IdentityLoadOrGenerateFailed(Exception ex) =>
            $"Identity file read/write failed. {ex.GetType().Name}: {ex.Message}. " +
            "Events will ship without identity fields this session.";

        // ---- Steam auto-detection ----

        internal static string SteamPlatformDetectionFailed(Exception ex) =>
            $"Steam platform detection threw {ex.GetType().Name}: {ex.Message}. " +
            "distribution_platform will not be auto-set.";

        internal static string SteamAutoIdentified(string steamId) =>
            $"auto-identified steam user: {steamId}";

        internal static string SteamIdentityCollectionFailed(Exception ex) =>
            $"Steam identity collection threw {ex.GetType().Name}: {ex.Message}. " +
            "Steam user ID will not be auto-collected.";

        // ---- Epic auto-detection ----

        internal static string EpicPlatformDetectionFailed(Exception ex) =>
            $"Epic platform detection threw {ex.GetType().Name}: {ex.Message}. " +
            "distribution_platform will not be auto-set.";

        internal static string EpicAutoIdentified(string epicId) =>
            $"auto-identified epic user: {epicId}";

        internal static string EpicIdentityCollectionFailed(Exception ex) =>
            $"Epic identity collection threw {ex.GetType().Name}: {ex.Message}. " +
            "Epic user ID will not be auto-collected.";
    }
}
