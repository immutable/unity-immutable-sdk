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

        // Separate from Writer so production wiring can route this to a real
        // Debug.LogError (red Editor console entry, picked up by crash/error
        // reporting integrations) while Writer stays on plain Debug.Log.
        // Tests set this to capture output; AudienceUnityHooks sets it to Debug.LogError.
        internal static Action<string>? ErrorWriter { get; set; }

        internal static void Debug(string message)
        {
            if (!Enabled) return;
            Emit(Writer, $"{Prefix} {message}");
        }

        internal static void Warn(string message) =>
            Emit(Writer, $"{Prefix} WARN: {message}");

        // Fires unconditionally, independent of Enabled, like Warn.
        internal static void Error(string message) =>
            Emit(ErrorWriter, $"{Prefix} ERROR: {message}");

        private static void Emit(Action<string>? writer, string line)
        {
            // Swallow anything the writer or Console throws so the Log methods
            // never throw themselves. If they did, an exception from logging
            // inside a catch block would reach the background timer and
            // crash the game on modern .NET.
            try
            {
                if (writer != null)
                {
                    writer(line);
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
        // Null event / empty event name are caller bugs: thrown as
        // ArgumentException/ArgumentNullException, not logged (see ImmutableAudience.Track).

        internal const string TrackIEventNull =
            "Track(IEvent) called with null event.";

        internal const string TrackStringEmptyName =
            "Track(string) called with null or empty event name.";

        internal static string TrackIEventThrew(string evtTypeName, Exception ex) =>
            $"Track(IEvent): {evtTypeName}.ToProperties()/EventName threw {ex.GetType().Name}: {ex.Message}. Dropping.";

        internal static string TrackIEventEmptyName(string evtTypeName) =>
            $"Track(IEvent): {evtTypeName}.EventName returned null or empty.";

        // ---- Identify / Alias ----
        // Empty/malformed ids are caller bugs: thrown as ArgumentException, not
        // logged (see ImmutableAudience.Identify/Alias). IdentifyDiscarded/
        // AliasDiscarded below are different: consent gating is a routine no-op,
        // not a bug, so those stay as warnings.

        internal const string IdentifyEmptyUserId =
            "Identify called with null or empty userId.";

        internal const string AliasEmptyIds =
            "Alias called with null or empty fromId/toId.";

        internal const string AliasIdenticalIds =
            "Alias called with identical fromId and toId.";

        internal static string IdentifyDiscarded(ConsentLevel current) =>
            $"Identify discarded. Requires Full consent, current is {current}.";

        internal static string IdentifyPassportIdInvalidFormat(string userId) =>
            $"Identify called with identityType Passport but userId \"{userId}\" doesn't look like a " +
            "Passport ID (expected a format like \"email|123\" or a UUID). Check you're passing the " +
            "Passport user ID, not your own internal user ID.";

        internal static string AliasDiscarded(ConsentLevel current) =>
            $"Alias discarded. Requires Full consent, current is {current}.";

        internal static string AliasPassportIdInvalidFormat(string side, string id) =>
            $"Alias called with {side}Type Passport but {side}Id \"{id}\" doesn't look like a " +
            "Passport ID (expected a format like \"email|123\" or a UUID). Check you're passing the " +
            "Passport user ID, not your own internal user ID.";

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

        internal static string FlushOutcome(bool ok, int count) =>
            $"flush {(ok ? "ok" : "failed")} ({count} messages)";

        internal static string ParseRejectedResultThrew(Exception ex) =>
            $"ParseRejectedResult threw {ex.GetType().Name}: {ex.Message}";

        internal static string MessageRejectedByServer(int count, string detail) =>
            $"{count} message(s) rejected by the server:\n{detail}";

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

        // ---- Install-time device/context collection ----

        internal static string LaunchPropertiesCollectionFailed(Exception ex) =>
            $"CollectGameLaunchProperties threw {ex.GetType().Name}: {ex.Message} during Install(). " +
            "game_launch will ship without auto-detected Unity context this session.";

        internal static string ContextCollectionFailed(Exception ex) =>
            $"CollectContext threw {ex.GetType().Name}: {ex.Message} during Install(). " +
            "All events will ship without locale/screen/timezone/userAgent this session.";

        internal static string ScreenResolutionReadFailed(Exception ex) =>
            $"Screen resolution read threw {ex.GetType().Name}: {ex.Message}. " +
            "Continuing without a screen field.";

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
