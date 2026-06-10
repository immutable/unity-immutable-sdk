#nullable enable

using UnityEditor;
using UnityEngine;

namespace Immutable.Audience.Editor
{
    /// <summary>
    /// Build-time iOS settings injected into the generated Xcode project's
    /// <c>Info.plist</c> by <see cref="iOSInfoPlistPostProcessor"/>.
    /// </summary>
    /// <remarks>
    /// The runtime <c>AudienceConfig</c> can't be read at build time, so the
    /// iOS post-processor needs an asset-backed source of truth for values
    /// that must land in <c>Info.plist</c> before the binary is signed. The
    /// post-processor finds the asset by type, so studios can keep it
    /// wherever fits their project layout.
    /// </remarks>
    public sealed class AudienceMobileBuildSettings : ScriptableObject
    {
        // Fallback used when no asset exists or the field is left blank.
        // Apple reviewers reject generic strings — studios MUST replace this
        // with copy that names their app and explains the specific use case
        // (e.g. "Game Name uses your advertising identifier to attribute app
        // installs and measure ad campaign performance."). Submitting this
        // default will likely result in an App Store rejection.
        internal const string DefaultTrackingUsageDescription =
            "This app uses your device's advertising identifier to attribute " +
            "app installs and measure ad campaign performance. You can change " +
            "this preference at any time in Settings > Privacy & Security > Tracking.";

        [SerializeField]
        [Tooltip("REQUIRED: Customise this before submitting to the App Store. " +
                 "Apple rejects generic strings — describe what YOUR app collects " +
                 "and why (e.g. 'Game Name uses your advertising identifier to " +
                 "attribute installs and measure ad performance.'). " +
                 "Left blank, the SDK default is used but will likely be rejected.")]
        private string trackingUsageDescription = DefaultTrackingUsageDescription;

        [SerializeField]
        [Tooltip("SKAdNetwork IDs (e.g. \"abc123.skadnetwork\") to register " +
                 "with the App Store as supported ad networks. Provided by " +
                 "your ad partners.")]
        private string[] skAdNetworkIds = new string[0];

        public string TrackingUsageDescription =>
            string.IsNullOrWhiteSpace(trackingUsageDescription)
                ? DefaultTrackingUsageDescription
                : trackingUsageDescription;

        public string[] SKAdNetworkIds => skAdNetworkIds ?? new string[0];

        [MenuItem("Assets/Create/Immutable Audience/Mobile Build Settings", priority = 100)]
        private static void CreateAsset()
        {
            var asset = CreateInstance<AudienceMobileBuildSettings>();
            ProjectWindowUtil.CreateAsset(asset, "AudienceMobileBuildSettings.asset");
        }

        /// <summary>
        /// Locates the first asset under <c>Assets/</c>, or <c>null</c> if
        /// none exists.
        /// </summary>
        internal static AudienceMobileBuildSettings? FindAsset()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(AudienceMobileBuildSettings)}");
            if (guids.Length == 0) return null;

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            if (guids.Length > 1)
            {
                Debug.LogWarning(
                    $"[ImmutableAudience] Multiple AudienceMobileBuildSettings assets found - " +
                    $"using '{path}'. Remove the duplicates to avoid unexpected build behaviour.");
            }
            return AssetDatabase.LoadAssetAtPath<AudienceMobileBuildSettings>(path);
        }
    }
}
