using System;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class ConsentLevelTests
    {
        [TestCase(ConsentLevel.None, ConsentLevelWireFormat.None)]
        [TestCase(ConsentLevel.Anonymous, ConsentLevelWireFormat.Anonymous)]
        [TestCase(ConsentLevel.Full, ConsentLevelWireFormat.Full)]
        public void ToLowercaseString_MapsEachEnumValueToLowercaseBackendString(ConsentLevel level, string expected)
        {
            Assert.AreEqual(expected, level.ToLowercaseString());
        }

        [Test]
        public void ConsentLevelWireFormat_PinsExactStringValues()
        {
            // Pins each emitted string directly so a typo or backend rename fails the build.
            Assert.AreEqual("none", ConsentLevelWireFormat.None);
            Assert.AreEqual("anonymous", ConsentLevelWireFormat.Anonymous);
            Assert.AreEqual("full", ConsentLevelWireFormat.Full);
        }

        [Test]
        public void ToLowercaseString_UnknownValue_Throws()
        {
            var invalid = (ConsentLevel)999;

            Assert.Throws<ArgumentOutOfRangeException>(() => invalid.ToLowercaseString());
        }

        [TestCase(ConsentLevel.None, false)]
        [TestCase(ConsentLevel.Anonymous, true)]
        [TestCase(ConsentLevel.Full, true)]
        public void CanTrack_TrueForAnonymousAndFull(ConsentLevel level, bool expected)
        {
            Assert.AreEqual(expected, level.CanTrack());
        }

        [Test]
        public void CanTrack_UnknownValue_ReturnsTrue()
        {
            var invalid = (ConsentLevel)999;

            Assert.IsTrue(invalid.CanTrack());
        }

        [TestCase(ConsentLevel.None, false)]
        [TestCase(ConsentLevel.Anonymous, false)]
        [TestCase(ConsentLevel.Full, true)]
        public void CanIdentify_TrueOnlyForFull(ConsentLevel level, bool expected)
        {
            Assert.AreEqual(expected, level.CanIdentify());
        }

        [Test]
        public void CanIdentify_UnknownValue_ReturnsFalse()
        {
            var invalid = (ConsentLevel)999;

            Assert.IsFalse(invalid.CanIdentify());
        }
    }
}
