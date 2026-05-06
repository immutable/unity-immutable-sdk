#nullable enable

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Immutable.Audience.Unity;
using Immutable.Audience.Unity.Mobile;
using UnityEngine;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class DeviceCollectorTests
    {
        // -----------------------------------------------------------------
        // CollectContext
        // -----------------------------------------------------------------

        [Test]
        public void CollectContext_AlwaysContainsUserAgent()
        {
            var ctx = DeviceCollector.CollectContext();
            Assert.IsTrue(ctx.ContainsKey("userAgent"), "userAgent must always be present");
            Assert.IsNotNull(ctx["userAgent"]);
        }

        [Test]
        public void CollectContext_UserAgent_DoesNotExceed256Chars()
        {
            var ctx = DeviceCollector.CollectContext();
            Assert.LessOrEqual(ctx["userAgent"].ToString()!.Length, 256);
        }

        [Test]
        public void CollectContext_KnownStringKeys_AreWithin256Chars()
        {
            var ctx = DeviceCollector.CollectContext();
            foreach (var key in new[] { "userAgent", "timezone", "locale", "screen" })
            {
                if (!ctx.TryGetValue(key, out var val)) continue;
                Assert.LessOrEqual(val.ToString()!.Length, 256,
                    $"context[{key}] exceeds 256 chars");
            }
        }

        // -----------------------------------------------------------------
        // CollectGameLaunchProperties
        // -----------------------------------------------------------------

        [Test]
        public void CollectGameLaunchProperties_AlwaysContainsCrossPlatformFields()
        {
            var props = DeviceCollector.CollectGameLaunchProperties();
            foreach (var key in new[] {
                "platform", "version", "buildGuid", "unityVersion",
                "osFamily", "deviceModel", "gpu", "gpuVendor",
                "cpu", "cpuCores", "ramMb" })
            {
                Assert.IsTrue(props.ContainsKey(key), $"expected key '{key}' to be present");
            }
        }

        [Test]
        public void CollectGameLaunchProperties_StringFields_DoNotExceed256Chars()
        {
            var props = DeviceCollector.CollectGameLaunchProperties();
            foreach (var key in new[] {
                "platform", "version", "buildGuid", "unityVersion",
                "osFamily", "deviceModel", "gpu", "gpuVendor", "cpu" })
            {
                if (!props.TryGetValue(key, out var val)) continue;
                if (val is not string s) continue;
                Assert.LessOrEqual(s.Length, 256, $"props[{key}] exceeds 256 chars");
            }
        }

        [Test]
        public void CollectGameLaunchProperties_NonAndroid_DoesNotContainAndroidId()
        {
            // Regression guard: androidId must only appear on Android. Running
            // tests on Editor/Standalone should never populate this field.
            var props = DeviceCollector.CollectGameLaunchProperties();
            Assert.IsFalse(props.ContainsKey("androidId"),
                "androidId must not be present on non-Android platforms");
        }

        [Test]
        public void CollectGameLaunchProperties_ScreenDpi_AbsentWhenZero()
        {
            // Screen.dpi returns 0 on some Linux WMs; the implementation
            // must omit the key rather than forwarding a zero value.
            var props = DeviceCollector.CollectGameLaunchProperties();
            if (props.TryGetValue("screenDpi", out var dpi))
                Assert.Greater((int)dpi, 0, "screenDpi must not be 0 when present");
        }

        // -----------------------------------------------------------------
        // iOS-specific (IDFVBridge + screenScale)
        // -----------------------------------------------------------------

        private Func<string?>? _originalIDFVImpl;

        [SetUp]
        public void SetUp() => _originalIDFVImpl = IDFVBridge.Impl;

        [TearDown]
        public void TearDown() => IDFVBridge.Impl = _originalIDFVImpl!;

        [Test]
        public void CollectGameLaunchProperties_iOS_IdfvPresentWhenBridgeReturnsValue()
        {
            IDFVBridge.Impl = () => "12345678-ABCD-ABCD-ABCD-123456789ABC";
            var props = DeviceCollector.CollectGameLaunchProperties(RuntimePlatform.IPhonePlayer);
            Assert.IsTrue(props.ContainsKey("idfv"), "idfv must be present when bridge returns a value");
            Assert.AreEqual("12345678-ABCD-ABCD-ABCD-123456789ABC", props["idfv"]);
        }

        [Test]
        public void CollectGameLaunchProperties_iOS_IdfvAbsentWhenBridgeReturnsNull()
        {
            IDFVBridge.Impl = () => null;
            var props = DeviceCollector.CollectGameLaunchProperties(RuntimePlatform.IPhonePlayer);
            Assert.IsFalse(props.ContainsKey("idfv"), "idfv must be absent when bridge returns null");
        }

        [Test]
        public void CollectGameLaunchProperties_NonIOS_DoesNotContainIdfv()
        {
            // IDFVBridge must never be called on non-iOS platforms.
            IDFVBridge.Impl = () => "should-not-appear";
            var props = DeviceCollector.CollectGameLaunchProperties();
            Assert.IsFalse(props.ContainsKey("idfv"),
                "idfv must not be present on non-iOS platforms");
        }

        [Test]
        public void CollectGameLaunchProperties_iOS_ContainsPlatformIPhonePlayer()
        {
            IDFVBridge.Impl = () => null;
            var props = DeviceCollector.CollectGameLaunchProperties(RuntimePlatform.IPhonePlayer);
            Assert.AreEqual("iOS", props["platform"]);
        }
    }
}
