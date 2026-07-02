using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    public class MessageBuilderTests
    {
        private const string PackageVersion = "1.2.3";
        private const string AnonId = "anon-1";
        private const string DeviceId = "device-1";
        private const string Consent = "anonymous";

        [Test]
        public void Track_RequiredFieldsPresent()
        {
            var result = MessageBuilder.Track("level_complete", AnonId, null, null, PackageVersion, Consent);

            Assert.AreEqual("track", result["type"]);
            Assert.IsTrue(result.ContainsKey("messageId"));
            Assert.IsTrue(result.ContainsKey("eventTimestamp"));
            Assert.IsTrue(result.ContainsKey("context"));
            Assert.IsTrue(result.ContainsKey("surface"));
            Assert.AreEqual("level_complete", result["eventName"]);
        }

        [Test]
        public void Track_EventNameLongerThan256Chars_TruncatedTo256()
        {
            var longName = new string('x', 300);

            var result = MessageBuilder.Track(longName, null, null, null, PackageVersion, Consent);

            Assert.AreEqual(256, ((string)result["eventName"]).Length);
        }

        [Test]
        public void Track_NullUserId_NotPresentInDict()
        {
            var result = MessageBuilder.Track("evt", AnonId, null, null, PackageVersion, Consent);

            Assert.IsFalse(result.ContainsKey("userId"));
        }

        [Test]
        public void Track_NonNullUserId_PresentInDict()
        {
            var result = MessageBuilder.Track("evt", AnonId, "user-99", null, PackageVersion, Consent);

            Assert.IsTrue(result.ContainsKey("userId"));
            Assert.AreEqual("user-99", result["userId"]);
        }

        [Test]
        public void Track_DeviceId_PresentWhenProvided()
        {
            var result = MessageBuilder.Track("evt", AnonId, null, DeviceId, PackageVersion, Consent);

            Assert.IsTrue(result.ContainsKey("deviceId"));
            Assert.AreEqual(DeviceId, result["deviceId"]);
        }

        [Test]
        public void Track_DeviceId_AbsentWhenNull()
        {
            var result = MessageBuilder.Track("evt", AnonId, null, null, PackageVersion, Consent);

            Assert.IsFalse(result.ContainsKey("deviceId"));
        }

        [Test]
        public void Identify_TypeAndIdentityFieldsPresent()
        {
            var result = MessageBuilder.Identify("anon-42", "user-42", null, "steam", PackageVersion, "full");

            Assert.AreEqual("identify", result["type"]);
            Assert.AreEqual("anon-42", result["anonymousId"]);
            Assert.AreEqual("user-42", result["userId"]);
            Assert.AreEqual("steam", result["identityType"]);
        }

        [Test]
        public void Identify_DeviceId_PresentWhenProvided()
        {
            var result = MessageBuilder.Identify("anon-42", "user-42", DeviceId, "steam", PackageVersion, "full");

            Assert.IsTrue(result.ContainsKey("deviceId"));
            Assert.AreEqual(DeviceId, result["deviceId"]);
        }

        [Test]
        public void Alias_AllFourFieldsPresent()
        {
            var result = MessageBuilder.Alias("from-id", "email", "to-id", "steam", null, PackageVersion, "full");

            Assert.AreEqual("alias", result["type"]);
            Assert.AreEqual("from-id", result["fromId"]);
            Assert.AreEqual("email", result["fromType"]);
            Assert.AreEqual("to-id", result["toId"]);
            Assert.AreEqual("steam", result["toType"]);
        }

        [Test]
        public void Alias_DeviceId_PresentWhenProvided()
        {
            var result = MessageBuilder.Alias("from-id", "email", "to-id", "steam", DeviceId, PackageVersion, "full");

            Assert.IsTrue(result.ContainsKey("deviceId"));
            Assert.AreEqual(DeviceId, result["deviceId"]);
        }

        [Test]
        public void AllMessages_ContextContainsLibraryAndLibraryVersion()
        {
            var track = MessageBuilder.Track("evt", null, null, null, PackageVersion, Consent);
            var identify = MessageBuilder.Identify(null, "u1", null, "steam", PackageVersion, "full");
            var alias = MessageBuilder.Alias("f", "t1", "t", "t2", null, PackageVersion, "full");

            foreach (var msg in new[] { track, identify, alias })
            {
                var ctx = (Dictionary<string, object>)msg["context"];
                Assert.AreEqual(Constants.LibraryName, ctx["library"]);
                Assert.AreEqual(PackageVersion, ctx["libraryVersion"]);
            }
        }

        [Test]
        public void AllMessages_SurfaceIsUnity()
        {
            var track = MessageBuilder.Track("evt", null, null, null, PackageVersion, Consent);
            var identify = MessageBuilder.Identify(null, "u1", null, "steam", PackageVersion, "full");
            var alias = MessageBuilder.Alias("f", "t1", "t", "t2", null, PackageVersion, "full");

            Assert.AreEqual("unity", track["surface"]);
            Assert.AreEqual("unity", identify["surface"]);
            Assert.AreEqual("unity", alias["surface"]);
        }

        [Test]
        public void AllMessages_ConsentLevelStamped()
        {
            // Every message carries the consent level it was built under, so the
            // backend records the explicit level instead of inferring it.
            var track = MessageBuilder.Track("evt", null, null, null, PackageVersion, "anonymous");
            var identify = MessageBuilder.Identify(null, "u1", null, "steam", PackageVersion, "full");
            var alias = MessageBuilder.Alias("f", "t1", "t", "t2", null, PackageVersion, "full");

            Assert.AreEqual("anonymous", track["consentLevel"]);
            Assert.AreEqual("full", identify["consentLevel"]);
            Assert.AreEqual("full", alias["consentLevel"]);
        }

        [Test]
        public void Track_ConsentLevelFull_NoUserId_IsFullButUnidentified()
        {
            // Full consent does not require a userId (e.g. before Identify()); the
            // explicit consentLevel is exactly what distinguishes this from
            // anonymous traffic.
            var result = MessageBuilder.Track("evt", AnonId, null, null, PackageVersion, "full");

            Assert.AreEqual("full", result["consentLevel"]);
            Assert.IsFalse(result.ContainsKey("userId"));
        }

        [Test]
        public void AllMessages_MessageId_ParsesAsGuid()
        {
            foreach (var msg in EveryMessageType())
            {
                var id = (string)msg["messageId"];
                Assert.IsTrue(Guid.TryParse(id, out _),
                    $"messageId must parse as Guid; got: '{id}'");
            }
        }

        [Test]
        public void Track_MessageId_IsUniquePerCall()
        {
            // Backend deduplicates on messageId; collisions silently drop events.
            var ids = new HashSet<string>();
            for (var i = 0; i < 1000; i++)
                ids.Add((string)MessageBuilder.Track("evt", null, null, null, PackageVersion, Consent)["messageId"]);
            Assert.AreEqual(1000, ids.Count);
        }

        [Test]
        public void AllMessages_EventTimestamp_IsRoundTripIso8601Utc()
        {
            // SDK uses DateTime.UtcNow.ToString("o"): round-trippable ISO 8601.
            // Backend schema requires this exact shape; previously only verified
            // indirectly via backend rejection.
            var before = DateTime.UtcNow.AddSeconds(-2);
            foreach (var msg in EveryMessageType())
            {
                var ts = (string)msg["eventTimestamp"];
                Assert.IsTrue(
                    DateTime.TryParseExact(ts, "o", CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind, out var parsed),
                    $"eventTimestamp must parse as ISO 8601 round-trip ('o') format; got: '{ts}'");
                Assert.AreEqual(DateTimeKind.Utc, parsed.Kind, "eventTimestamp must be UTC");
                Assert.That(parsed, Is.GreaterThanOrEqualTo(before),
                    "eventTimestamp must be ~now, not stale");
                Assert.That(parsed, Is.LessThanOrEqualTo(DateTime.UtcNow.AddSeconds(2)),
                    "eventTimestamp must be ~now, not future-dated");
            }
        }

        [Test]
        public void AllMessages_Context_LibraryAndLibraryVersionAreNonEmptyStrings()
        {
            foreach (var msg in EveryMessageType())
            {
                var ctx = (Dictionary<string, object>)msg["context"];
                var library = ctx["library"] as string;
                var libraryVersion = ctx["libraryVersion"] as string;
                Assert.IsFalse(string.IsNullOrEmpty(library), "context.library must be non-empty string");
                Assert.IsFalse(string.IsNullOrEmpty(libraryVersion), "context.libraryVersion must be non-empty string");
            }
        }

        [Test]
        public void Track_TestModeTrue_IncludesTestFlag()
        {
            var result = MessageBuilder.Track("evt", null, null, null, PackageVersion, Consent, testMode: true);
            Assert.IsTrue(result.ContainsKey("test"), "test field must be present when testMode is true");
            Assert.AreEqual(true, result["test"]);
        }

        [Test]
        public void Track_TestModeFalse_ExcludesTestFlag()
        {
            var result = MessageBuilder.Track("evt", null, null, null, PackageVersion, Consent, testMode: false);
            Assert.IsFalse(result.ContainsKey("test"), "test field must not be present when testMode is false");
        }

        [Test]
        public void AllMessages_TestModeTrue_AllIncludeTestFlag()
        {
            var track = MessageBuilder.Track("evt", null, null, null, PackageVersion, Consent, testMode: true);
            var identify = MessageBuilder.Identify(null, "u1", null, "steam", PackageVersion, "full", testMode: true);
            var alias = MessageBuilder.Alias("f", "t1", "t", "t2", null, PackageVersion, "full", testMode: true);

            foreach (var msg in new[] { track, identify, alias })
            {
                Assert.IsTrue(msg.ContainsKey("test"), $"{msg["type"]} must include test field in test mode");
                Assert.AreEqual(true, msg["test"]);
            }
        }

        private static IEnumerable<Dictionary<string, object>> EveryMessageType()
        {
            yield return MessageBuilder.Track("evt", null, null, null, PackageVersion, Consent);
            yield return MessageBuilder.Identify(null, "u1", null, "steam", PackageVersion, "full");
            yield return MessageBuilder.Alias("f", "t1", "t", "t2", null, PackageVersion, "full");
        }
    }
}
