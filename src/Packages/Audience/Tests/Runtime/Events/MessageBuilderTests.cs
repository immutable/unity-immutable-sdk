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

        [Test]
        public void Track_RequiredFieldsPresent()
        {
            var result = MessageBuilder.Track(TestEventNames.LevelComplete, TestFixtures.AnonId1, null, PackageVersion);

            Assert.AreEqual(MessageTypes.Track, result[MessageFields.Type]);
            Assert.IsTrue(result.ContainsKey(MessageFields.MessageId));
            Assert.IsTrue(result.ContainsKey(MessageFields.EventTimestamp));
            Assert.IsTrue(result.ContainsKey(MessageFields.Context));
            Assert.IsTrue(result.ContainsKey(MessageFields.Surface));
            Assert.AreEqual(TestEventNames.LevelComplete, result[MessageFields.EventName]);
        }

        [Test]
        public void Track_EventNameLongerThan256Chars_TruncatedTo256()
        {
            var longName = new string('x', 300);

            var result = MessageBuilder.Track(longName, null, null, PackageVersion);

            Assert.AreEqual(256, ((string)result[MessageFields.EventName]).Length);
        }

        [Test]
        public void Track_NullUserId_NotPresentInDict()
        {
            var result = MessageBuilder.Track("evt", TestFixtures.AnonId1, null, PackageVersion);

            Assert.IsFalse(result.ContainsKey(MessageFields.UserId));
        }

        [Test]
        public void Track_NonNullUserId_PresentInDict()
        {
            var result = MessageBuilder.Track("evt", TestFixtures.AnonId1, TestFixtures.UserId99, PackageVersion);

            Assert.IsTrue(result.ContainsKey(MessageFields.UserId));
            Assert.AreEqual(TestFixtures.UserId99, result[MessageFields.UserId]);
        }

        [Test]
        public void Identify_TypeAndIdentityFieldsPresent()
        {
            var result = MessageBuilder.Identify(TestFixtures.AnonId42, TestFixtures.UserId42, IdentityType.Steam.ToLowercaseString(), PackageVersion);

            Assert.AreEqual(MessageTypes.Identify, result[MessageFields.Type]);
            Assert.AreEqual(TestFixtures.AnonId42, result[MessageFields.AnonymousId]);
            Assert.AreEqual(TestFixtures.UserId42, result[MessageFields.UserId]);
            Assert.AreEqual(IdentityType.Steam.ToLowercaseString(), result[MessageFields.IdentityType]);
        }

        [Test]
        public void Alias_AllFourFieldsPresent()
        {
            var result = MessageBuilder.Alias(
                TestFixtures.AliasFromId, IdentityType.Email.ToLowercaseString(),
                TestFixtures.AliasToId, IdentityType.Steam.ToLowercaseString(),
                PackageVersion);

            Assert.AreEqual(MessageTypes.Alias, result[MessageFields.Type]);
            Assert.AreEqual(TestFixtures.AliasFromId, result[MessageFields.FromId]);
            Assert.AreEqual(IdentityType.Email.ToLowercaseString(), result[MessageFields.FromType]);
            Assert.AreEqual(TestFixtures.AliasToId, result[MessageFields.ToId]);
            Assert.AreEqual(IdentityType.Steam.ToLowercaseString(), result[MessageFields.ToType]);
        }

        [Test]
        public void AllMessages_ContextContainsLibraryAndLibraryVersion()
        {
            var track = MessageBuilder.Track("evt", null, null, PackageVersion);
            var identify = MessageBuilder.Identify(null, "u1", IdentityType.Steam.ToLowercaseString(), PackageVersion);
            var alias = MessageBuilder.Alias("f", "t1", "t", "t2", PackageVersion);

            foreach (var msg in new[] { track, identify, alias })
            {
                var ctx = (Dictionary<string, object>)msg[MessageFields.Context];
                Assert.AreEqual(Constants.LibraryName, ctx[MessageFields.Library]);
                Assert.AreEqual(PackageVersion, ctx[MessageFields.LibraryVersion]);
            }
        }

        [Test]
        public void AllMessages_SurfaceIsUnity()
        {
            var track = MessageBuilder.Track("evt", null, null, PackageVersion);
            var identify = MessageBuilder.Identify(null, "u1", IdentityType.Steam.ToLowercaseString(), PackageVersion);
            var alias = MessageBuilder.Alias("f", "t1", "t", "t2", PackageVersion);

            Assert.AreEqual(Constants.Surface, track[MessageFields.Surface]);
            Assert.AreEqual(Constants.Surface, identify[MessageFields.Surface]);
            Assert.AreEqual(Constants.Surface, alias[MessageFields.Surface]);
        }

        [Test]
        public void AllMessages_MessageId_ParsesAsGuid()
        {
            foreach (var msg in EveryMessageType())
            {
                var id = (string)msg[MessageFields.MessageId];
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
                ids.Add((string)MessageBuilder.Track("evt", null, null, PackageVersion)[MessageFields.MessageId]);
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
                var ts = (string)msg[MessageFields.EventTimestamp];
                Assert.IsTrue(
                    DateTime.TryParseExact(ts, Constants.IsoTimestampFormat, CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind, out var parsed),
                    $"eventTimestamp must parse as ISO 8601 round-trip ('{Constants.IsoTimestampFormat}') format; got: '{ts}'");
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
                var ctx = (Dictionary<string, object>)msg[MessageFields.Context];
                var library = ctx[MessageFields.Library] as string;
                var libraryVersion = ctx[MessageFields.LibraryVersion] as string;
                Assert.IsFalse(string.IsNullOrEmpty(library), "context.library must be non-empty string");
                Assert.IsFalse(string.IsNullOrEmpty(libraryVersion), "context.libraryVersion must be non-empty string");
            }
        }

        private static IEnumerable<Dictionary<string, object>> EveryMessageType()
        {
            yield return MessageBuilder.Track("evt", null, null, PackageVersion);
            yield return MessageBuilder.Identify(null, "u1", IdentityType.Steam.ToLowercaseString(), PackageVersion);
            yield return MessageBuilder.Alias("f", "t1", "t", "t2", PackageVersion);
        }
    }
}
