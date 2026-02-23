using System.Collections.Generic;
using NUnit.Framework;

namespace Immutable.Passport.Core
{
    [TestFixture]
    public class PendingRequestRegistryTests
    {
        private PendingRequestRegistry _registry;

        [SetUp]
        public void Init()
        {
            _registry = new PendingRequestRegistry();
        }

        [Test]
        public void Register_ReturnsCompletionSource()
        {
            var completion = _registry.Register("req-1");

            Assert.IsNotNull(completion);
        }

        [Test]
        public void Contains_AfterRegister_ReturnsTrue()
        {
            _registry.Register("req-1");

            Assert.IsTrue(_registry.Contains("req-1"));
        }

        [Test]
        public void Contains_UnregisteredId_ReturnsFalse()
        {
            Assert.IsFalse(_registry.Contains("does-not-exist"));
        }

        [Test]
        public void Get_ReturnsTheSameCompletionSource()
        {
            var registered = _registry.Register("req-1");
            var retrieved = _registry.Get("req-1");

            Assert.AreSame(registered, retrieved);
        }

        [Test]
        public void Remove_AfterRegister_ContainsReturnsFalse()
        {
            _registry.Register("req-1");
            _registry.Remove("req-1");

            Assert.IsFalse(_registry.Contains("req-1"));
        }

        [Test]
        public void Register_MultipleRequests_TracksAllIndependently()
        {
            var c1 = _registry.Register("req-1");
            var c2 = _registry.Register("req-2");

            Assert.IsTrue(_registry.Contains("req-1"));
            Assert.IsTrue(_registry.Contains("req-2"));
            Assert.AreNotSame(c1, c2);
        }

        [Test]
        public void Remove_OneRequest_DoesNotAffectOthers()
        {
            _registry.Register("req-1");
            _registry.Register("req-2");

            _registry.Remove("req-1");

            Assert.IsFalse(_registry.Contains("req-1"));
            Assert.IsTrue(_registry.Contains("req-2"));
        }
    }
}
