using System;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class IdentityTypeTests
    {
        [TestCase(IdentityType.Passport, "passport")]
        [TestCase(IdentityType.Steam, "steam")]
        [TestCase(IdentityType.Epic, "epic")]
        [TestCase(IdentityType.Google, "google")]
        [TestCase(IdentityType.Apple, "apple")]
        [TestCase(IdentityType.Discord, "discord")]
        [TestCase(IdentityType.Email, "email")]
        [TestCase(IdentityType.Custom, "custom")]
        public void ToLowercaseString_MapsEachEnumValueToLowercaseBackendString(IdentityType type, string expected)
        {
            Assert.AreEqual(expected, type.ToLowercaseString());
        }

        [Test]
        public void ToLowercaseString_UnknownValue_Throws()
        {
            // CDP matches identify / alias events by identityType during data
            // deletion; an out-of-range cast would otherwise ship an event
            // with an empty namespace that CDP cannot clean up. Fail loud so
            // the programmer error surfaces at the call site instead.
            var invalid = (IdentityType)999;

            Assert.Throws<ArgumentOutOfRangeException>(() => invalid.ToLowercaseString());
        }
    }
}
