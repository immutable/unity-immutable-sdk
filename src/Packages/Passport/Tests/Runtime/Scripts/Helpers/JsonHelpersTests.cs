using System.Collections.Generic;
using NUnit.Framework;
using Immutable.Passport.Helpers;

namespace Immutable.Passport.Core
{
    [TestFixture]
    public class JsonHelpersTests
    {
        [Test]
        public void ArrayToJson()
        {
            var array = new[] { "a", "b", "c" };
            Assert.AreEqual("[\"a\",\"b\",\"c\"]", array.ToJson());
            
            array = new[] { "Items" };
            Assert.AreEqual("[\"Items\"]", array.ToJson());
            
            array = new[] { "{Items:" };
            Assert.AreEqual("[\"{Items:\"]", array.ToJson());
            
            array = new string[] {};
            Assert.AreEqual("[]", array.ToJson());
        }
        
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

            properties = new Dictionary<string, object>() { { "null", null } };
            Assert.AreEqual("{}", properties.ToJson());
        }
    }
}