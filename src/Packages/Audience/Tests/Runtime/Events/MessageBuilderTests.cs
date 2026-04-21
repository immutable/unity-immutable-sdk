using System.Collections.Generic;
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
            var result = MessageBuilder.Track("level_complete", "anon-1", null, PackageVersion);

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

            var result = MessageBuilder.Track(longName, null, null, PackageVersion);

            Assert.AreEqual(256, ((string)result["eventName"]).Length);
        }

        [Test]
        public void Track_NullUserId_NotPresentInDict()
        {
            var result = MessageBuilder.Track("evt", "anon-1", null, PackageVersion);

            Assert.IsFalse(result.ContainsKey("userId"));
        }

        [Test]
        public void Track_NonNullUserId_PresentInDict()
        {
            var result = MessageBuilder.Track("evt", "anon-1", "user-99", PackageVersion);

            Assert.IsTrue(result.ContainsKey("userId"));
            Assert.AreEqual("user-99", result["userId"]);
        }

        [Test]
        public void Identify_TypeAndIdentityFieldsPresent()
        {
            var result = MessageBuilder.Identify("anon-42", "user-42", "steam", PackageVersion);

            Assert.AreEqual("identify", result["type"]);
            Assert.AreEqual("anon-42", result["anonymousId"]);
            Assert.AreEqual("user-42", result["userId"]);
            Assert.AreEqual("steam", result["identityType"]);
        }

        [Test]
        public void Alias_AllFourFieldsPresent()
        {
            var result = MessageBuilder.Alias("from-id", "email", "to-id", "steam", PackageVersion);

            Assert.AreEqual("alias", result["type"]);
            Assert.AreEqual("from-id", result["fromId"]);
            Assert.AreEqual("email", result["fromType"]);
            Assert.AreEqual("to-id", result["toId"]);
            Assert.AreEqual("steam", result["toType"]);
        }

        [Test]
        public void AllMessages_ContextContainsLibraryAndLibraryVersion()
        {
            var track = MessageBuilder.Track("evt", null, null, PackageVersion);
            var identify = MessageBuilder.Identify(null, "u1", null, PackageVersion);
            var alias = MessageBuilder.Alias("f", "t1", "t", "t2", PackageVersion);

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
            var track = MessageBuilder.Track("evt", null, null, PackageVersion);
            var identify = MessageBuilder.Identify(null, "u1", null, PackageVersion);
            var alias = MessageBuilder.Alias("f", "t1", "t", "t2", PackageVersion);

            Assert.AreEqual("unity", track["surface"]);
            Assert.AreEqual("unity", identify["surface"]);
            Assert.AreEqual("unity", alias["surface"]);
        }
    }
}
