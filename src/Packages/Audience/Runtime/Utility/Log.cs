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
            // Swallow anything the Writer (or Console.WriteLine) throws so
            // callers can treat Log.Warn / Log.Debug as never-throwing. The
            // SDK's safety wrappers (Session.SafeTrack, SafePerformanceSnapshot,
            // Shutdown's flush-timeout path) log from inside their own catch
            // blocks; a throwing Writer would otherwise escape the wrapper and
            // propagate to the Timer thread (process kill on .NET 5+) or to
            // Application.quitting (blocking shutdown).
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
