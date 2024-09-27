using System;
using Immutable.Passport.Helpers;
using NUnit.Framework;

namespace Immutable.Passport.Core
{
    [TestFixture]
    public class UriHelpersTests
    {
        private const string DOMAIN = "https://auth.immutable.com";
        private const string QUERY_PARAMETER_KEY1 = "state";
        private const string QUERY_PARAMETER_VALUE1 = "state-value";
        private const string QUERY_PARAMETER_KEY2 = "code";
        private const string QUERY_PARAMETER_VALUE2 = "code%20value";

        [Test]
        public void GetQueryParameter_Success()
        {
            var uri = new Uri(
                $"{DOMAIN}?{QUERY_PARAMETER_KEY1}={QUERY_PARAMETER_VALUE1}&{QUERY_PARAMETER_KEY2}={QUERY_PARAMETER_VALUE2}");
            Assert.True(uri.GetQueryParameter(QUERY_PARAMETER_KEY1) == QUERY_PARAMETER_VALUE1);
            Assert.True(uri.GetQueryParameter(QUERY_PARAMETER_KEY2) == QUERY_PARAMETER_VALUE2);
        }

        [Test]
        public void GetQueryParameter_NoQueryParameterWithKey()
        {
            var uri = new Uri($"{DOMAIN}?noKey=some-value");
            Assert.Null(uri.GetQueryParameter(QUERY_PARAMETER_KEY1));
        }

        [Test]
        public void GetQueryParameter_NoQueryParameters()
        {
            var uri = new Uri(DOMAIN);
            Assert.Null(uri.GetQueryParameter(QUERY_PARAMETER_KEY1));
            uri = new Uri($"{DOMAIN}?");
            Assert.Null(uri.GetQueryParameter(QUERY_PARAMETER_KEY1));
            uri = new Uri($"{DOMAIN}/?");
            Assert.Null(uri.GetQueryParameter(QUERY_PARAMETER_KEY1));
        }
    }
}