#nullable enable

using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class AudienceConfigTests
    {
        [Test]
        public void EnableMobileAttribution_DefaultsToFalse()
        {
            // Default-off matters: a studio that never opts in must ship a
            // clean binary, no AD_ID permission, NSPrivacyTracking false.
            var config = new AudienceConfig();
            Assert.IsFalse(config.EnableMobileAttribution);
        }

        [Test]
        public void SKAdNetworkIds_DefaultsToNull()
        {
            var config = new AudienceConfig();
            Assert.IsNull(config.SKAdNetworkIds);
        }

        [Test]
        public void EnableMobileAttribution_RoundTrips()
        {
            var config = new AudienceConfig { EnableMobileAttribution = true };
            Assert.IsTrue(config.EnableMobileAttribution);
        }

        [Test]
        public void SKAdNetworkIds_RoundTrips()
        {
            var ids = new[] { "abc123.skadnetwork", "def456.skadnetwork" };
            var config = new AudienceConfig { SKAdNetworkIds = ids };
            Assert.AreSame(ids, config.SKAdNetworkIds);
        }
    }
}
