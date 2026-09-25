#nullable enable

using NUnit.Framework;
using Immutable.Audience.Editor;

namespace Immutable.Audience.Editor.Tests
{
    [TestFixture]
    internal class MobileAttributionDefineTests
    {
        [Test]
        public void Contains_NullOrEmpty_ReturnsFalse()
        {
            Assert.IsFalse(MobileAttributionDefine.Contains(null));
            Assert.IsFalse(MobileAttributionDefine.Contains(""));
        }

        [Test]
        public void Contains_SymbolAmongOthers_ReturnsTrue()
        {
            Assert.IsTrue(MobileAttributionDefine.Contains("FOO;AUDIENCE_MOBILE_ATTRIBUTION;BAR"));
        }

        [Test]
        public void Contains_SymbolWithStrayWhitespace_ReturnsTrue()
        {
            Assert.IsTrue(MobileAttributionDefine.Contains("FOO; AUDIENCE_MOBILE_ATTRIBUTION ;BAR"));
        }

        [Test]
        public void Contains_OtherDefinesOnly_ReturnsFalse()
        {
            Assert.IsFalse(MobileAttributionDefine.Contains("FOO;BAR"));
        }

        [Test]
        public void WithSymbol_EnableOnEmpty_AddsOnlySymbol()
        {
            Assert.AreEqual("AUDIENCE_MOBILE_ATTRIBUTION", MobileAttributionDefine.WithSymbol(null, enabled: true));
        }

        [Test]
        public void WithSymbol_EnableWithOthers_KeepsOthersAndAddsSymbol()
        {
            var result = MobileAttributionDefine.WithSymbol("FOO;BAR", enabled: true);

            Assert.IsTrue(MobileAttributionDefine.Contains(result));
            Assert.IsTrue(result.Contains("FOO"));
            Assert.IsTrue(result.Contains("BAR"));
        }

        [Test]
        public void WithSymbol_EnableWhenAlreadyPresent_DoesNotDuplicate()
        {
            var result = MobileAttributionDefine.WithSymbol("FOO;AUDIENCE_MOBILE_ATTRIBUTION;BAR", enabled: true);

            var symbolCount = System.Array.FindAll(result.Split(';'), d => d == "AUDIENCE_MOBILE_ATTRIBUTION").Length;
            Assert.AreEqual(1, symbolCount);
        }

        [Test]
        public void WithSymbol_DisableRemovesSymbolOnly()
        {
            var result = MobileAttributionDefine.WithSymbol("FOO;AUDIENCE_MOBILE_ATTRIBUTION;BAR", enabled: false);

            Assert.IsFalse(MobileAttributionDefine.Contains(result));
            Assert.IsTrue(result.Contains("FOO"));
            Assert.IsTrue(result.Contains("BAR"));
        }

        [Test]
        public void WithSymbol_DisableWhenAbsent_LeavesOthersUnchanged()
        {
            var result = MobileAttributionDefine.WithSymbol("FOO;BAR", enabled: false);

            Assert.AreEqual("FOO;BAR", result);
        }
    }
}
