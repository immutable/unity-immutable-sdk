using System;

namespace Immutable.Audience
{
    internal static class Log
    {
        private const string Prefix = "[ImmutableAudience]";

        internal static bool Enabled { get; set; }

        // Tests set this to capture output; AudienceUnityHooks sets it to Debug.Log.
        internal static Action<string> Writer { get; set; }

        internal static void Debug(string message)
        {
            if (!Enabled) return;
            Emit($"{Prefix} {message}");
        }

        internal static void Warn(string message) =>
            Emit($"{Prefix} WARN: {message}");

        private static void Emit(string line)
        {
            // Swallow Writer/Console throws so Log.Warn/Debug is never-throwing.
            // SDK safety wrappers log from inside their own catches; a throwing
            // Writer would otherwise reach the Timer thread (process kill on .NET 5+).
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
