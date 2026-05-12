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
    /// Patches <c>UnityFramework/PrivacyInfo.xcprivacy</c> to add IDFA tracking
    /// declarations when <c>AUDIENCE_MOBILE_ATTRIBUTION</c> is enabled.
    /// Unity auto-merges the default manifest; this post-processor adds
    /// <c>NSPrivacyTracking=true</c> and <c>NSPrivacyCollectedDataTypeAdvertisingData</c>
    /// in-place so Unity's own Required Reason API entries are preserved.
    /// Runs at callbackOrder 9052, after the Info.plist (9050) and framework (9051) post-processors.
    /// </summary>
    internal static class iOSPrivacyManifestPostProcessor
    {
        internal const int CallbackOrder = 9052;
        private const string BuiltManifestName = "PrivacyInfo.xcprivacy";

        [PostProcessBuild(CallbackOrder)]
        internal static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS) return;

#if UNITY_IOS
            if (!AttributionDefineEnabled()) return;

            var builtManifestPath = FindBuiltManifest(pathToBuiltProject);
            if (builtManifestPath == null)
            {
                Debug.LogWarning(
                    $"[ImmutableAudience] iOS privacy manifest post-processor: {BuiltManifestName} not found " +
                    $"under {pathToBuiltProject}. Skipping attribution manifest update.");
                return;
            }

            var plist = new PlistDocument();
            plist.ReadFromFile(builtManifestPath);
            ApplyAttributionPrivacyEntries(plist.root);
            plist.WriteToFile(builtManifestPath);
#endif
        }

        private static string? FindBuiltManifest(string pathToBuiltProject)
        {
            // Unity 2019.3+ places the merged manifest here.
            var candidate = Path.Combine(pathToBuiltProject, "UnityFramework", BuiltManifestName);
            if (File.Exists(candidate)) return candidate;

            // Fall back to a recursive search for older Unity layout variants.
            var found = Directory.GetFiles(pathToBuiltProject, BuiltManifestName, SearchOption.AllDirectories);
            return found.Length > 0 ? found[0] : null;
        }

#if UNITY_IOS
        /// <summary>
        /// Adds the attribution-specific privacy declarations to an existing
        /// (already Unity-merged) <c>PrivacyInfo.xcprivacy</c> plist root.
        /// Idempotent. Safe to call on a manifest that already has these entries.
        /// </summary>
        internal static void ApplyAttributionPrivacyEntries(PlistElementDict root)
        {
            // IDFA collection constitutes tracking under Apple's definition.
            root.SetBoolean("NSPrivacyTracking", true);

            PlistElementArray dataTypes;
            if (root.values.TryGetValue("NSPrivacyCollectedDataTypes", out var existing) &&
                existing is PlistElementArray existingArray)
            {
                dataTypes = existingArray;
            }
            else
            {
                dataTypes = root.CreateArray("NSPrivacyCollectedDataTypes");
            }

            // Avoid duplicate entries if the post-processor is re-run.
            const string advertisingType = "NSPrivacyCollectedDataTypeAdvertisingData";
            foreach (var item in dataTypes.values)
            {
                if (item is PlistElementDict d &&
                    d.values.TryGetValue("NSPrivacyCollectedDataType", out var v) &&
                    v is PlistElementString s &&
                    s.value == advertisingType)
                    return;
            }

            var entry = dataTypes.AddDict();
            entry.SetString("NSPrivacyCollectedDataType", advertisingType);
            entry.SetBoolean("NSPrivacyCollectedDataTypeLinked", true);
            entry.SetBoolean("NSPrivacyCollectedDataTypeTracking", true);
            var purposes = entry.CreateArray("NSPrivacyCollectedDataTypePurposes");
            purposes.AddString("NSPrivacyCollectedDataTypePurposeAnalytics");
        }
#endif

        private static bool AttributionDefineEnabled()
        {
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS) ?? string.Empty;
            foreach (var define in defines.Split(';'))
            {
                if (define.Trim() == iOSInfoPlistPostProcessor.AttributionDefine) return true;
            }
            return false;
        }
    }
}
