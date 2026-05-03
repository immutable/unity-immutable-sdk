#nullable enable

using System.Collections.Generic;
using NUnit.Framework;
using Immutable.Audience.Unity;

namespace Immutable.Audience.Tests.Editor
{
    // Editor-only (DeviceCollector needs a Unity domain; skipped by the headless dotnet build).
    // Pins emitted payload keys against GameLaunchPropertyKeys and ContextKeys.
    [TestFixture]
    internal class DeviceCollectorTests
    {
        [Test]
        public void CollectGameLaunchProperties_EmitsTheExpectedKeySet()
        {
            var props = DeviceCollector.CollectGameLaunchProperties();

            // Always-present keys. ScreenDpi is conditional (0 on some Linux WMs).
            CollectionAssert.Contains(props.Keys, GameLaunchPropertyKeys.Platform);
            CollectionAssert.Contains(props.Keys, GameLaunchPropertyKeys.Version);
            CollectionAssert.Contains(props.Keys, GameLaunchPropertyKeys.BuildGuid);
            CollectionAssert.Contains(props.Keys, GameLaunchPropertyKeys.UnityVersion);
            CollectionAssert.Contains(props.Keys, GameLaunchPropertyKeys.OsFamily);
            CollectionAssert.Contains(props.Keys, GameLaunchPropertyKeys.DeviceModel);
            CollectionAssert.Contains(props.Keys, GameLaunchPropertyKeys.Gpu);
            CollectionAssert.Contains(props.Keys, GameLaunchPropertyKeys.GpuVendor);
            CollectionAssert.Contains(props.Keys, GameLaunchPropertyKeys.Cpu);
            CollectionAssert.Contains(props.Keys, GameLaunchPropertyKeys.CpuCores);
            CollectionAssert.Contains(props.Keys, GameLaunchPropertyKeys.RamMb);
        }

        [Test]
        public void CollectGameLaunchProperties_EmitsNoUnknownKeys()
        {
            // Confirms the payload only carries known GameLaunchPropertyKeys entries.
            var allowed = new HashSet<string>
            {
                GameLaunchPropertyKeys.Platform,
                GameLaunchPropertyKeys.Version,
                GameLaunchPropertyKeys.BuildGuid,
                GameLaunchPropertyKeys.UnityVersion,
                GameLaunchPropertyKeys.OsFamily,
                GameLaunchPropertyKeys.DeviceModel,
                GameLaunchPropertyKeys.Gpu,
                GameLaunchPropertyKeys.GpuVendor,
                GameLaunchPropertyKeys.Cpu,
                GameLaunchPropertyKeys.CpuCores,
                GameLaunchPropertyKeys.RamMb,
                GameLaunchPropertyKeys.ScreenDpi,
            };

            var props = DeviceCollector.CollectGameLaunchProperties();
            foreach (var key in props.Keys)
                Assert.IsTrue(allowed.Contains(key),
                    $"DeviceCollector.CollectGameLaunchProperties emitted unknown key '{key}' "
                    + "with no matching GameLaunchPropertyKeys constant");
        }

        [Test]
        public void CollectGameLaunchProperties_TruncatesStringValuesToMaxFieldLength()
        {
            // Every string value respects MaxFieldLength; an untruncated .ToString() would fail here.
            var props = DeviceCollector.CollectGameLaunchProperties();
            foreach (var kv in props)
            {
                if (kv.Value is string s)
                    Assert.LessOrEqual(s.Length, Constants.MaxFieldLength,
                        $"GameLaunchPropertyKeys.{kv.Key} value exceeds Constants.MaxFieldLength");
            }
        }

        [Test]
        public void CollectContext_EmitsTheExpectedKeySet()
        {
            var ctx = DeviceCollector.CollectContext();

            // UserAgent is unconditional. Timezone / Locale / Screen are
            // best-effort and may be absent under unusual hosts.
            CollectionAssert.Contains(ctx.Keys, ContextKeys.UserAgent);
        }

        [Test]
        public void CollectContext_EmitsNoUnknownKeys()
        {
            var allowed = new HashSet<string>
            {
                ContextKeys.UserAgent,
                ContextKeys.Timezone,
                ContextKeys.Locale,
                ContextKeys.Screen,
            };

            var ctx = DeviceCollector.CollectContext();
            foreach (var key in ctx.Keys)
                Assert.IsTrue(allowed.Contains(key),
                    $"DeviceCollector.CollectContext emitted unknown key '{key}' "
                    + "with no matching ContextKeys constant");
        }

        [Test]
        public void CollectContext_TruncatesStringValuesToMaxFieldLength()
        {
            var ctx = DeviceCollector.CollectContext();
            foreach (var kv in ctx)
            {
                if (kv.Value is string s)
                    Assert.LessOrEqual(s.Length, Constants.MaxFieldLength,
                        $"ContextKeys.{kv.Key} value exceeds Constants.MaxFieldLength");
            }
        }
    }
}
