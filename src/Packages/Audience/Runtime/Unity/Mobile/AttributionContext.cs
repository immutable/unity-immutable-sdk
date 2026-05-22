#nullable enable

using System.Collections.Generic;

namespace Immutable.Audience.Unity.Mobile
{
    // Builds the platform attribution snapshot that ships on game_launch when
    // EnableMobileAttribution is true.
    //
    // iOS: ATT status is always read; IDFA is only included when status is
    // authorized (Apple returns the all-zeros UUID otherwise, which the native
    // bridge filters to null).
    //
    // Android: gaid + gaidLimitAdTracking are read from the GAIDBridge disk
    // cache populated by the previous launch's background fetch (Google's
    // AdvertisingIdClient is sync + must run off main thread, so first launch
    // ships nothing; gaidLimitAdTracking shows up on launch #2 onwards).
    internal static class AttributionContext
    {
        // Maps Apple's ATTrackingManagerAuthorizationStatus to the wire
        // strings the analytics pipeline expects. Stable values shared with
        // backend dashboards.
        internal static string AttStatusToString(int status)
        {
            switch (status)
            {
                case 0: return "notDetermined";
                case 1: return "restricted";
                case 2: return "denied";
                case 3: return "authorized";
                default: return "unknown";
            }
        }

        // persistentDataPath is required for Android (GAID disk cache); iOS
        // ignores it. Returns a possibly-empty dict (never null) so callers
        // can merge unconditionally.
        internal static IReadOnlyDictionary<string, object> Capture(string? persistentDataPath = null)
        {
            var props = new Dictionary<string, object>();

#if UNITY_IOS || UNITY_EDITOR
            // Compiled in on iOS device builds AND in the editor (any target)
            // so AttributionContextTests can drive Capture() via the ATTBridge
            // test seams. Excluded on real Android device builds so attStatus
            // never ships there. Native ATTBridge calls are themselves gated
            // by #if UNITY_IOS, so non-iOS editor targets get the safe stubs.
            var status = ATTBridge.GetStatus();
            props["att_status"] = AttStatusToString(status);

            // Only ship IDFA when the user has authorized tracking. The native
            // bridge already returns null for the zero-UUID case, but gating
            // here makes the contract explicit and survives a future native
            // change.
            if (status == 3)
            {
                var idfa = ATTBridge.GetIDFA();
                if (!string.IsNullOrEmpty(idfa))
                    props["idfa"] = idfa!;
            }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR && AUDIENCE_MOBILE_ATTRIBUTION
            // Gated on AUDIENCE_MOBILE_ATTRIBUTION so a build that disables
            // GAID at compile time can't read a stale cache file written by
            // a previous install where the define was on.
            if (!string.IsNullOrEmpty(persistentDataPath))
            {
                var info = GAIDBridge.GetCached(persistentDataPath!);
                if (info.HasValue)
                    EmitGaidProps(info.Value, props);
            }
#endif

            return props;
        }

        // Defensive emission gate: even if a stale cache from a pre-fix build
        // retained a non-empty GAID under opt-out, this method never ships
        // the raw identifier when LimitAdTracking is true. gaidLimitAdTracking
        // always ships so the pipeline can distinguish "fetched, opted out"
        // from "not fetched yet".
        internal static void EmitGaidProps(GAIDInfo info, IDictionary<string, object> props)
        {
            if (!info.LimitAdTracking && !string.IsNullOrEmpty(info.Gaid))
                props["gaid"] = info.Gaid;
            props["gaid_limit_ad_tracking"] = info.LimitAdTracking;
        }
    }
}
