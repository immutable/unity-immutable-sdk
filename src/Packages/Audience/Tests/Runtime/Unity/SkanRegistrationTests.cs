#nullable enable

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Immutable.Audience.Unity.Mobile;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class SkanRegistrationTests
    {
        private Action? _originalBridgeImpl;
        private Func<bool>? _originalHasRegistered;
        private Action? _originalMarkRegistered;

        [SetUp]
        public void SetUp()
        {
            _originalBridgeImpl = SKANBridge.Impl;
            _originalHasRegistered = SkanRegistration.HasRegistered;
            _originalMarkRegistered = SkanRegistration.MarkRegistered;
        }

        [TearDown]
        public void TearDown()
        {
            SKANBridge.Impl = _originalBridgeImpl!;
            SkanRegistration.HasRegistered = _originalHasRegistered!;
            SkanRegistration.MarkRegistered = _originalMarkRegistered!;
        }

        [Test]
        public void RegisterIfFirstLaunch_FirstLaunch_CallsBridgeAndReturnsTrue()
        {
            var bridgeCalled = false;
            var marked = false;
            SKANBridge.Impl = () => { bridgeCalled = true; };
            SkanRegistration.HasRegistered = () => false;
            SkanRegistration.MarkRegistered = () => { marked = true; };

            var result = SkanRegistration.RegisterIfFirstLaunch();

            Assert.AreEqual(true, result, "first launch must return true");
            Assert.IsTrue(bridgeCalled, "bridge must be called on first launch");
            Assert.IsTrue(marked, "registered flag must be persisted");
        }

        [Test]
        public void RegisterIfFirstLaunch_AlreadyRegistered_SkipsBridgeAndReturnsNull()
        {
            var bridgeCalled = false;
            SKANBridge.Impl = () => { bridgeCalled = true; };
            SkanRegistration.HasRegistered = () => true;

            var result = SkanRegistration.RegisterIfFirstLaunch();

            Assert.IsNull(result, "subsequent launches must return null");
            Assert.IsFalse(bridgeCalled, "bridge must not be called if already registered");
        }

        [Test]
        public void RegisterIfFirstLaunch_PersistsAfterBridgeCall()
        {
            // Bridge must fire before MarkRegistered so the flag isn't set if the
            // native call throws (a future crash here won't block re-registration).
            var order = new List<string>();
            SKANBridge.Impl = () => order.Add("bridge");
            SkanRegistration.HasRegistered = () => false;
            SkanRegistration.MarkRegistered = () => order.Add("mark");

            SkanRegistration.RegisterIfFirstLaunch();

            Assert.AreEqual(new[] { "bridge", "mark" }, order,
                "bridge must fire before the registered flag is persisted");
        }
    }
}
