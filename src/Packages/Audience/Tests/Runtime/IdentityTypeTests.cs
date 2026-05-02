using System;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class IdentityTypeTests
    {
        [TestCase(IdentityType.Passport, IdentityTypeWireFormat.Passport)]
        [TestCase(IdentityType.Steam, IdentityTypeWireFormat.Steam)]
        [TestCase(IdentityType.Epic, IdentityTypeWireFormat.Epic)]
        [TestCase(IdentityType.Google, IdentityTypeWireFormat.Google)]
        [TestCase(IdentityType.Apple, IdentityTypeWireFormat.Apple)]
        [TestCase(IdentityType.Discord, IdentityTypeWireFormat.Discord)]
        [TestCase(IdentityType.Email, IdentityTypeWireFormat.Email)]
        [TestCase(IdentityType.Custom, IdentityTypeWireFormat.Custom)]
        public void ToLowercaseString_MapsEachEnumValueToLowercaseBackendString(IdentityType type, string expected)
        {
            Assert.AreEqual(expected, type.ToLowercaseString());
        }

        [TestCase(IdentityTypeWireFormat.Passport, IdentityType.Passport)]
        [TestCase(IdentityTypeWireFormat.Steam, IdentityType.Steam)]
        [TestCase(IdentityTypeWireFormat.Epic, IdentityType.Epic)]
        [TestCase(IdentityTypeWireFormat.Google, IdentityType.Google)]
        [TestCase(IdentityTypeWireFormat.Apple, IdentityType.Apple)]
        [TestCase(IdentityTypeWireFormat.Discord, IdentityType.Discord)]
        [TestCase(IdentityTypeWireFormat.Email, IdentityType.Email)]
        [TestCase(IdentityTypeWireFormat.Custom, IdentityType.Custom)]
        public void ParseLowercaseString_MapsKnownStringToEnum(string wire, IdentityType expected)
        {
            Assert.AreEqual(expected, IdentityTypeExtensions.ParseLowercaseString(wire));
        }

        [Test]
        public void IdentityTypeWireFormat_PinsExactStringValues()
        {
            // Pins each constant's exact string so a typo or backend rename fails the build.
            Assert.AreEqual("passport", IdentityTypeWireFormat.Passport);
            Assert.AreEqual("steam", IdentityTypeWireFormat.Steam);
            Assert.AreEqual("epic", IdentityTypeWireFormat.Epic);
            Assert.AreEqual("google", IdentityTypeWireFormat.Google);
            Assert.AreEqual("apple", IdentityTypeWireFormat.Apple);
            Assert.AreEqual("discord", IdentityTypeWireFormat.Discord);
            Assert.AreEqual("email", IdentityTypeWireFormat.Email);
            Assert.AreEqual("custom", IdentityTypeWireFormat.Custom);
        }

        [TestCase("Steam", IdentityType.Steam)]
        [TestCase("STEAM", IdentityType.Steam)]
        [TestCase("Passport", IdentityType.Passport)]
        public void ParseLowercaseString_AcceptsMixedCase(string wire, IdentityType expected)
        {
            Assert.AreEqual(expected, IdentityTypeExtensions.ParseLowercaseString(wire));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(TestFixtures.UnknownProvider)]
        [TestCase("steamX")]
        public void ParseLowercaseString_FallsBackToCustomForUnknownOrEmpty(string? wire)
        {
            // ParseLowercaseString never throws; unknown values map to Custom.
            Assert.AreEqual(IdentityType.Custom, IdentityTypeExtensions.ParseLowercaseString(wire));
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
