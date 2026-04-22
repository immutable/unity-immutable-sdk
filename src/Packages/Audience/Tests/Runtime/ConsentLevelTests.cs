using System;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class ConsentLevelTests
    {
        [TestCase(ConsentLevel.None, "none")]
        [TestCase(ConsentLevel.Anonymous, "anonymous")]
        [TestCase(ConsentLevel.Full, "full")]
        public void ToLowercaseString_MapsEachEnumValueToLowercaseBackendString(ConsentLevel level, string expected)
        {
            Assert.AreEqual(expected, level.ToLowercaseString());
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
