using System;

namespace Immutable.Passport.Core.Logging
{
    public static class PassportLogger
    {
        private const string TAG = "[Immutable]";

        public static LogLevel CurrentLogLevel { get; set; } = LogLevel.Info;

        /// <summary>
        /// A function that defines how sensitive data should be redacted.
        /// If null, no redaction is applied.
        /// </summary>
        public static Func<string, string>? RedactionHandler { get; set; }

        private static void Log(LogLevel level, string message)
        {
            if (level < CurrentLogLevel)
            {
                return; // Don't log messages below the current log level
            }

            if (RedactionHandler != null)
            {
                message = RedactionHandler(message);
            }

            switch (level)
            {
                case LogLevel.Debug:
                    UnityEngine.Debug.Log($"{TAG} {message}");
                    break;
                case LogLevel.Info:
                    UnityEngine.Debug.Log($"{TAG} {message}");
                    break;
                case LogLevel.Warn:
                    UnityEngine.Debug.LogWarning($"{TAG} {message}");
                    break;
                case LogLevel.Error:
                    UnityEngine.Debug.LogError($"{TAG} {message}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }

        public static void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        public static void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        public static void Warn(string message)
        {
            Log(LogLevel.Warn, message);
        }

        public static void Error(string message)
        {
            Log(LogLevel.Error, message);
        }
    }

}
