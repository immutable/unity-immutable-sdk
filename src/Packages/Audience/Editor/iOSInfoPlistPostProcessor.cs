#nullable enable

using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace Immutable.Audience.Editor
{
    /// <summary>
    /// Injects mobile-attribution keys into the generated iOS Xcode project's
    /// <c>Info.plist</c>: <c>NSUserTrackingUsageDescription</c> (the ATT
    /// prompt copy) and <c>SKAdNetworkItems</c>.
    /// </summary>
    /// <remarks>
    /// Both keys are gated by the <c>AUDIENCE_MOBILE_ATTRIBUTION</c>
    /// scripting define so a studio that hasn't opted into attribution
    /// ships a clean <c>Info.plist</c>. Apple flags apps that include
    /// either key without the corresponding code paths.
    ///
    /// Values come from the <see cref="AudienceMobileBuildSettings"/>
    /// asset. If the asset is missing, a default
    /// <c>NSUserTrackingUsageDescription</c> is still written (Apple
    /// rejects builds with the key missing) but no <c>SKAdNetworkItems</c>.
    ///
    /// <c>callbackOrder = 9050</c> runs above Unity's own post-processors
    /// (order 1) so studio post-processors with low orders run first,
    /// while higher-order post-processors that extend
    /// <c>SKAdNetworkItems</c> can still merge their entries on top.
    /// </remarks>
    internal static class iOSInfoPlistPostProcessor
    {
        internal const int CallbackOrder = 9050;
        internal const string AttributionDefine = "AUDIENCE_MOBILE_ATTRIBUTION";

        [PostProcessBuild(CallbackOrder)]
        internal static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS) return;

#if UNITY_IOS
            if (!AttributionDefineEnabled()) return;

            var plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            if (!File.Exists(plistPath))
            {
                Debug.LogWarning(
                    $"[ImmutableAudience] iOS post-processor: Info.plist not found at {plistPath}. Skipping.");
                return;
            }

            var settings = AudienceMobileBuildSettings.FindAsset();

            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            ApplyTrackingUsageDescription(plist.root, settings);
            ApplySKAdNetworkItems(plist.root, settings);

            plist.WriteToFile(plistPath);
#endif
        }

        // Sanity-check the settings asset without running a full iOS build.
        [MenuItem("Tools/Immutable/Audience/Validate iOS Build Settings")]
        private static void ValidateBuildSettings()
        {
            if (!AttributionDefineEnabled())
            {
                Debug.LogWarning(
                    "[ImmutableAudience] AUDIENCE_MOBILE_ATTRIBUTION scripting define is not set " +
                    "for the iOS player target. The post-processor will not modify Info.plist. " +
                    "Add the define under Player Settings → Other Settings → Scripting Define Symbols.");
                return;
            }

            var settings = AudienceMobileBuildSettings.FindAsset();
            var description = settings != null
                ? settings.TrackingUsageDescription
                : AudienceMobileBuildSettings.DefaultTrackingUsageDescription;
            var ids = settings?.SKAdNetworkIds ?? new string[0];

            Debug.Log(
                "[ImmutableAudience] iOS Info.plist injection preview\n" +
                $"  NSUserTrackingUsageDescription: {description}\n" +
                $"  SKAdNetworkItems: {ids.Length} id(s)\n" +
                (ids.Length == 0
                    ? "  (no SKAdNetwork ids configured - set them on the AudienceMobileBuildSettings asset)\n"
                    : string.Concat(System.Array.ConvertAll(ids, id => $"    - {id}\n"))));
        }

        // Reads the iOS-target define list specifically. The post-processor
        // mutates iOS build output regardless of which target the editor is
        // currently focused on.
        private static bool AttributionDefineEnabled()
        {
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS) ?? string.Empty;
            foreach (var define in defines.Split(';'))
            {
                if (define.Trim() == AttributionDefine) return true;
            }
            return false;
        }

#if UNITY_IOS
        internal static void ApplyTrackingUsageDescription(
            PlistElementDict root,
            AudienceMobileBuildSettings? settings)
        {
            var description = settings != null
                ? settings.TrackingUsageDescription
                : AudienceMobileBuildSettings.DefaultTrackingUsageDescription;

            // Always overwrite. The settings asset is the source of truth,
            // beating any placeholder a lower-order post-processor wrote.
            root.SetString("NSUserTrackingUsageDescription", description);
        }

        internal static void ApplySKAdNetworkItems(
            PlistElementDict root,
            AudienceMobileBuildSettings? settings)
        {
            var ids = settings?.SKAdNetworkIds ?? new string[0];
            if (ids.Length == 0) return;

            // Merge with any existing list so a lower-order post-processor's
            // entries aren't clobbered. Dedup is case-insensitive per Apple's
            // SKAdNetwork spec.
            PlistElementArray array;
            if (root.values.TryGetValue("SKAdNetworkItems", out var existing) &&
                existing is PlistElementArray existingArray)
            {
                array = existingArray;
            }
            else
            {
                array = root.CreateArray("SKAdNetworkItems");
            }

            var existingIds = new System.Collections.Generic.HashSet<string>(
                System.StringComparer.OrdinalIgnoreCase);
            foreach (var item in array.values)
            {
                if (item is PlistElementDict dict &&
                    dict.values.TryGetValue("SKAdNetworkIdentifier", out var idValue) &&
                    idValue is PlistElementString idString)
                {
                    existingIds.Add(idString.value);
                }
            }

            foreach (var id in ids)
            {
                if (string.IsNullOrWhiteSpace(id)) continue;
                if (existingIds.Contains(id)) continue;

                var dict = array.AddDict();
                dict.SetString("SKAdNetworkIdentifier", id);
                existingIds.Add(id);
            }
        }
#endif
    }
}
