#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Immutable.Audience.Unity
{
    internal static class DeviceCollector
    {
        internal static Dictionary<string, object> CollectContext()
        {
            // 256-char cap mirrors Web SDK's identifier truncation.
            var ctx = new Dictionary<string, object>
            {
                [ContextKeys.UserAgent] = Truncate(SystemInfo.operatingSystem, Constants.MaxFieldLength),
            };

            var timezone = SafeTimezone();
            if (timezone != null) ctx[ContextKeys.Timezone] = Truncate(timezone, Constants.MaxFieldLength);

            var locale = LocaleString();
            if (locale != null) ctx[ContextKeys.Locale] = Truncate(locale, Constants.MaxFieldLength);

            var screen = TryResolveScreenString();
            if (screen != null) ctx[ContextKeys.Screen] = Truncate(screen, Constants.MaxFieldLength);

            return ctx;
        }

        private static string? TryResolveScreenString()
        {
            var resolution = Screen.currentResolution;
            int width = resolution.width;
            int height = resolution.height;

            if (width <= 0 || height <= 0)
            {
                width = Screen.width;
                height = Screen.height;
            }

            if (width <= 0 || height <= 0) return null;
            return $"{width}x{height}";
        }

        internal static Dictionary<string, object> CollectGameLaunchProperties()
        {
            var props = new Dictionary<string, object>
            {
                [GameLaunchPropertyKeys.Platform] = Application.platform.ToString(),
                [GameLaunchPropertyKeys.Version] = Truncate(Application.version, Constants.MaxFieldLength),
                [GameLaunchPropertyKeys.BuildGuid] = Truncate(Application.buildGUID, Constants.MaxFieldLength),
                [GameLaunchPropertyKeys.UnityVersion] = Truncate(Application.unityVersion, Constants.MaxFieldLength),
                [GameLaunchPropertyKeys.OsFamily] = SystemInfo.operatingSystemFamily.ToString(),
                [GameLaunchPropertyKeys.DeviceModel] = Truncate(SystemInfo.deviceModel, Constants.MaxFieldLength),
                [GameLaunchPropertyKeys.Gpu] = Truncate(SystemInfo.graphicsDeviceName, Constants.MaxFieldLength),
                [GameLaunchPropertyKeys.GpuVendor] = Truncate(SystemInfo.graphicsDeviceVendor, Constants.MaxFieldLength),
                [GameLaunchPropertyKeys.Cpu] = Truncate(SystemInfo.processorType, Constants.MaxFieldLength),
                [GameLaunchPropertyKeys.CpuCores] = SystemInfo.processorCount,
                [GameLaunchPropertyKeys.RamMb] = SystemInfo.systemMemorySize,
            };

            // Screen.dpi can be 0 on some Linux WMs.
            var dpi = (int)Screen.dpi;
            if (dpi > 0) props[GameLaunchPropertyKeys.ScreenDpi] = dpi;

            return props;
        }

        private static string? LocaleString()
        {
            var culture = CultureInfo.CurrentCulture;
            if (!string.IsNullOrEmpty(culture?.Name))
                return culture.Name;
            return null;
        }

        private static string? SafeTimezone()
        {
            try
            {
                return TimeZoneInfo.Local.Id;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s;
            // Step back one if the cut would split a surrogate pair. Leaving
            // a lone high-surrogate produces invalid UTF-16 on the wire.
            var cut = max;
            if (char.IsHighSurrogate(s[cut - 1])) cut--;
            return s.Substring(0, cut);
        }
    }
}
