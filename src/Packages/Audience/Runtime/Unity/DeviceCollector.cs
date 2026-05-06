#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using Immutable.Audience.Unity.Mobile;
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
                ["userAgent"] = Truncate(SystemInfo.operatingSystem, 256),
            };

            var timezone = SafeTimezone();
            if (timezone != null) ctx["timezone"] = Truncate(timezone, 256);

            var locale = LocaleString();
            if (locale != null) ctx["locale"] = Truncate(locale, 256);

            var screen = TryResolveScreenString();
            if (screen != null) ctx["screen"] = Truncate(screen, 256);

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

        internal static Dictionary<string, object> CollectGameLaunchProperties(
            RuntimePlatform? platformOverride = null)
        {
            var platform = platformOverride ?? Application.platform;
            var props = new Dictionary<string, object>
            {
                ["platform"] = PlatformName(platform),
                ["version"] = Truncate(Application.version, 256),
                ["buildGuid"] = Truncate(Application.buildGUID, 256),
                ["unityVersion"] = Truncate(Application.unityVersion, 256),
                ["osFamily"] = SystemInfo.operatingSystemFamily.ToString(),
                ["deviceModel"] = Truncate(SystemInfo.deviceModel, 256),
                ["gpu"] = Truncate(SystemInfo.graphicsDeviceName, 256),
                ["gpuVendor"] = Truncate(SystemInfo.graphicsDeviceVendor, 256),
                ["cpu"] = Truncate(SystemInfo.processorType, 256),
                ["cpuCores"] = SystemInfo.processorCount,
                ["ramMb"] = SystemInfo.systemMemorySize,
            };

            // Screen.dpi can be 0 on some Linux WMs.
            var dpi = (int)Screen.dpi;
            if (dpi > 0) props["screenDpi"] = dpi;

            if (platform == RuntimePlatform.Android)
                props["androidId"] = Truncate(SystemInfo.deviceUniqueIdentifier, 256);

            if (platform == RuntimePlatform.IPhonePlayer)
            {
                var idfv = IDFVBridge.GetIDFV();
                if (idfv != null) props["idfv"] = Truncate(idfv, 256);

                // iOS baseline is 163 DPI (1×); 326 → 2×, 401-460 → 3×.
                if (dpi > 0) props["screenScale"] = (int)Math.Round(dpi / 163.0);
            }

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

        private static string PlatformName(RuntimePlatform platform) => platform switch
        {
            RuntimePlatform.IPhonePlayer => "iOS",
            _ => platform.ToString(),
        };

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
