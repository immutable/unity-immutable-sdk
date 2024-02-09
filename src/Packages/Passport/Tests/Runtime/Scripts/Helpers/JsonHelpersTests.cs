using System.Collections.Generic;
using NUnit.Framework;
using Immutable.Passport.Helpers;

namespace Immutable.Passport.Core
{
    [TestFixture]
    public class JsonHelpersTests
    {
        [Test]
        public void DictionaryToJson()
        {
            var properties = new Dictionary<string, object>(){
                    {"boolean", true},
                    {"string", "immutable"},
                    {"int", 1},
                    {"long", (long) 2},
                    {"double", (double) 3}
                };
            Assert.AreEqual("{\"boolean\":true,\"string\":\"immutable\",\"int\":1,\"long\":2,\"double\":3}", properties.ToJson());

            properties = new Dictionary<string, object>() { { "boolean", false } };
            Assert.AreEqual("{\"boolean\":false}", properties.ToJson());

            properties = new Dictionary<string, object>();
            Assert.AreEqual("{}", properties.ToJson());
        }
    }
}