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
            // An out-of-range cast like `(IdentityType)999` must throw.
            // ToLowercaseString emits the "identityType" string on every
            // identify / alias event (e.g. "passport"), and the backend uses
            // that string to find and delete a user's events on request.
            //
            // An unknown enum value must throw so the bug surfaces at the
            // cast site. Returning a default string instead would ship the
            // event with a blank identityType (invisible to the deletion
            // lookup) and hide the bug.
            var invalid = (IdentityType)999;

            Assert.Throws<ArgumentOutOfRangeException>(() => invalid.ToLowercaseString());
        }
    }
}
