#nullable enable

#if UNITY_IOS
using NUnit.Framework;
using UnityEditor.iOS.Xcode;
using UnityEngine;
using Immutable.Audience.Editor;

namespace Immutable.Audience.Editor.Tests
{
    [TestFixture]
    internal class iOSInfoPlistPostProcessorTests
    {
        // -----------------------------------------------------------------
        // ApplyTrackingUsageDescription
        // -----------------------------------------------------------------

        [Test]
        public void ApplyTrackingUsageDescription_NoSettings_WritesDefault()
        {
            // Safe-ship guarantee: Apple rejects builds with the key missing
            // or empty, so a default is always written.
            var root = new PlistDocument().root;

            iOSInfoPlistPostProcessor.ApplyTrackingUsageDescription(root, settings: null);

            var description = ReadString(root, "NSUserTrackingUsageDescription");
            Assert.AreEqual(AudienceMobileBuildSettings.DefaultTrackingUsageDescription, description);
        }

        [Test]
        public void ApplyTrackingUsageDescription_WithCustomCopy_WritesIt()
        {
            var settings = ScriptableObject.CreateInstance<AudienceMobileBuildSettings>();
            SetPrivate(settings, "trackingUsageDescription", "Custom prompt copy");

            var root = new PlistDocument().root;

            iOSInfoPlistPostProcessor.ApplyTrackingUsageDescription(root, settings);

            Assert.AreEqual("Custom prompt copy", ReadString(root, "NSUserTrackingUsageDescription"));
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void ApplyTrackingUsageDescription_WithEmptyCopy_FallsBackToDefault()
        {
            // Whitespace falls through to the default. Apple rejects empty.
            var settings = ScriptableObject.CreateInstance<AudienceMobileBuildSettings>();
            SetPrivate(settings, "trackingUsageDescription", "   ");

            var root = new PlistDocument().root;

            iOSInfoPlistPostProcessor.ApplyTrackingUsageDescription(root, settings);

            Assert.AreEqual(AudienceMobileBuildSettings.DefaultTrackingUsageDescription,
                ReadString(root, "NSUserTrackingUsageDescription"));
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void ApplyTrackingUsageDescription_OverwritesExistingValue()
        {
            // Settings asset is the source of truth. Beat any placeholder
            // a lower-order post-processor wrote.
            var root = new PlistDocument().root;
            root.SetString("NSUserTrackingUsageDescription", "stale placeholder");

            var settings = ScriptableObject.CreateInstance<AudienceMobileBuildSettings>();
            SetPrivate(settings, "trackingUsageDescription", "fresh copy");

            iOSInfoPlistPostProcessor.ApplyTrackingUsageDescription(root, settings);

            Assert.AreEqual("fresh copy", ReadString(root, "NSUserTrackingUsageDescription"));
            Object.DestroyImmediate(settings);
        }

        // -----------------------------------------------------------------
        // ApplySKAdNetworkItems
        // -----------------------------------------------------------------

        [Test]
        public void ApplySKAdNetworkItems_NoSettings_LeavesPlistUntouched()
        {
            var root = new PlistDocument().root;

            iOSInfoPlistPostProcessor.ApplySKAdNetworkItems(root, settings: null);

            Assert.IsFalse(root.values.ContainsKey("SKAdNetworkItems"),
                "No settings asset → no SKAdNetworkItems key should be written");
        }

        [Test]
        public void ApplySKAdNetworkItems_EmptyIds_LeavesPlistUntouched()
        {
            var settings = ScriptableObject.CreateInstance<AudienceMobileBuildSettings>();
            SetPrivate(settings, "skAdNetworkIds", new string[0]);

            var root = new PlistDocument().root;

            iOSInfoPlistPostProcessor.ApplySKAdNetworkItems(root, settings);

            Assert.IsFalse(root.values.ContainsKey("SKAdNetworkItems"),
                "Empty ID array → no SKAdNetworkItems key should be written");
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void ApplySKAdNetworkItems_WithIds_WritesArrayOfDicts()
        {
            var settings = ScriptableObject.CreateInstance<AudienceMobileBuildSettings>();
            SetPrivate(settings, "skAdNetworkIds", new[] { "abc123.skadnetwork", "def456.skadnetwork" });

            var root = new PlistDocument().root;

            iOSInfoPlistPostProcessor.ApplySKAdNetworkItems(root, settings);

            var array = (PlistElementArray)root.values["SKAdNetworkItems"];
            Assert.AreEqual(2, array.values.Count);

            var first = (PlistElementDict)array.values[0];
            Assert.AreEqual("abc123.skadnetwork", ((PlistElementString)first.values["SKAdNetworkIdentifier"]).value);

            var second = (PlistElementDict)array.values[1];
            Assert.AreEqual("def456.skadnetwork", ((PlistElementString)second.values["SKAdNetworkIdentifier"]).value);
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void ApplySKAdNetworkItems_MergesWithExistingArray()
        {
            // Preserve entries written by lower-order post-processors.
            var root = new PlistDocument().root;
            var existing = root.CreateArray("SKAdNetworkItems");
            existing.AddDict().SetString("SKAdNetworkIdentifier", "existing.skadnetwork");

            var settings = ScriptableObject.CreateInstance<AudienceMobileBuildSettings>();
            SetPrivate(settings, "skAdNetworkIds", new[] { "added.skadnetwork" });

            iOSInfoPlistPostProcessor.ApplySKAdNetworkItems(root, settings);

            var array = (PlistElementArray)root.values["SKAdNetworkItems"];
            Assert.AreEqual(2, array.values.Count);
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void ApplySKAdNetworkItems_DedupesCaseInsensitive()
        {
            // Apple's registry is case-insensitive; dupes fail validation.
            var root = new PlistDocument().root;
            var existing = root.CreateArray("SKAdNetworkItems");
            existing.AddDict().SetString("SKAdNetworkIdentifier", "ABC123.skadnetwork");

            var settings = ScriptableObject.CreateInstance<AudienceMobileBuildSettings>();
            SetPrivate(settings, "skAdNetworkIds",
                new[] { "abc123.skadnetwork", "DEF456.skadnetwork" });

            iOSInfoPlistPostProcessor.ApplySKAdNetworkItems(root, settings);

            var array = (PlistElementArray)root.values["SKAdNetworkItems"];
            Assert.AreEqual(2, array.values.Count,
                "abc123 already present (different case) should not be added a second time");
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void ApplySKAdNetworkItems_SkipsNullOrWhitespaceIds()
        {
            // Blank inspector rows shouldn't produce empty identifier dicts.
            var settings = ScriptableObject.CreateInstance<AudienceMobileBuildSettings>();
            SetPrivate(settings, "skAdNetworkIds",
                new[] { "abc123.skadnetwork", "   ", null!, "def456.skadnetwork" });

            var root = new PlistDocument().root;

            iOSInfoPlistPostProcessor.ApplySKAdNetworkItems(root, settings);

            var array = (PlistElementArray)root.values["SKAdNetworkItems"];
            Assert.AreEqual(2, array.values.Count);
            Object.DestroyImmediate(settings);
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private static string ReadString(PlistElementDict dict, string key) =>
            ((PlistElementString)dict.values[key]).value;

        // [SerializeField] private fields aren't reachable from tests.
        private static void SetPrivate(object target, string fieldName, object? value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType()}");
            field!.SetValue(target, value);
        }
    }
}
#endif
