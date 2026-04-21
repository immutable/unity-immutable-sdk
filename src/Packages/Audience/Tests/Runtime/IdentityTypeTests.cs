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
        public void ToLowercaseString_UnknownValue_ReturnsNull()
        {
            // Never-throw contract: an out-of-range cast should not surface an
            // exception on the game thread. Callers drop the event via their
            // null/empty check instead.
            var invalid = (IdentityType)999;

            Assert.IsNull(invalid.ToLowercaseString());
        }
    }
}
