#nullable enable

using System.Collections.Generic;

namespace Immutable.Audience.Unity.Mobile
{
    // Builds the iOS attribution snapshot that ships on game_launch when
    // EnableMobileAttribution is true. ATT status is always read; IDFA is
    // only included when status is authorized (Apple returns the all-zeros
    // UUID otherwise, which the native bridge filters to null).
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

        // Always returns a non-null dictionary — at minimum
        // { attStatus: "notDetermined" }. The provider field type is
        // nullable for forward-compat with future implementations that may
        // want to opt out, but this implementation never returns null.
        internal static IReadOnlyDictionary<string, object> Capture()
        {
            var status = ATTBridge.GetStatus();
            var props = new Dictionary<string, object>
            {
                ["attStatus"] = AttStatusToString(status),
            };

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

            return props;
        }
    }
}
