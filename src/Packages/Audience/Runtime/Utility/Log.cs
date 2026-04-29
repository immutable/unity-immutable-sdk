#nullable enable

using System;

namespace Immutable.Audience
{
    internal static class Log
    {
        private const string Prefix = "[ImmutableAudience]";

        internal static bool Enabled { get; set; }

        // Tests set this to capture output; AudienceUnityHooks sets it to Debug.Log.
        internal static Action<string>? Writer { get; set; }

        internal static void Debug(string message)
        {
            if (!Enabled) return;
            Emit($"{Prefix} {message}");
        }

        internal static void Warn(string message) =>
            Emit($"{Prefix} WARN: {message}");

        private static void Emit(string line)
        {
            // Swallow anything the Writer or Console throws so Log.Warn and
            // Log.Debug never throw themselves. If they did, an exception from
            // logging inside a catch block would reach the background timer
            // and crash the game on modern .NET.
            try
            {
                if (Writer != null)
                {
                    Writer(line);
                    return;
                }
                Console.WriteLine(line);
            }
            catch
            {
            }
        }
    }
}
