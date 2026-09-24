#nullable enable

using System.Collections.Generic;
using UnityEditor;

namespace Immutable.Audience.Editor
{
    /// <summary>
    /// Reads and writes the <c>AUDIENCE_MOBILE_ATTRIBUTION</c> scripting
    /// define on Player Settings.
    /// </summary>
    /// <remarks>
    /// No state is cached or serialized here. Player Settings is the only
    /// source of truth, so this always reflects whatever a studio has
    /// already set, including manually, and nothing here can go stale.
    /// </remarks>
    internal static class MobileAttributionDefine
    {
        internal const string Symbol = "AUDIENCE_MOBILE_ATTRIBUTION";

        internal static bool IsEnabled(BuildTargetGroup group) =>
            Contains(PlayerSettings.GetScriptingDefineSymbolsForGroup(group));

        internal static void SetEnabled(BuildTargetGroup group, bool enabled)
        {
            var current = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, WithSymbol(current, enabled));
        }

        internal static bool Contains(string? defines)
        {
            foreach (var define in (defines ?? string.Empty).Split(';'))
            {
                if (define.Trim() == Symbol) return true;
            }
            return false;
        }

        internal static string WithSymbol(string? defines, bool enabled)
        {
            var result = new List<string>();
            foreach (var define in (defines ?? string.Empty).Split(';', System.StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = define.Trim();
                if (trimmed.Length == 0 || trimmed == Symbol) continue;
                result.Add(trimmed);
            }
            if (enabled) result.Add(Symbol);
            return string.Join(";", result);
        }
    }
}
