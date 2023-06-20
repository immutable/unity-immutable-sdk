using NUnit.Framework;
using Immutable.Passport.Utility;

namespace Immutable.Passport.Utility.Tests
{
    public class JwtUtilityTests
    {
        [Test]
        public void DecodeJwtTest()
        {
            string jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmF" + 
                "tZSI6IkltbXV0YWJsZSIsImlhdCI6MTUxNjIzOTAyMn0.3uo_vJDobJs97abKHJY48qaYaFdUj0o02admvkedyp4";
            string actual = JwtUtility.DecodeJwt(jwt);

            string expected = @"{""sub"":""1234567890"",""name"":""Immutable"",""iat"":1516239022}";
            StringAssert.AreEqualIgnoringCase(expected, actual);
        }

        [Test]
        public void DecodeJwtTestFailed()
        {
            Assert.Null(JwtUtility.DecodeJwt("Immutable"));
        }
    }
}