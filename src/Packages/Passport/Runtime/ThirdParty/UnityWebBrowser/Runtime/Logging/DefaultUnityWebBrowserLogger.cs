#if !IMMUTABLE_CUSTOM_BROWSER && (UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN))

using Immutable.Passport.Core.Logging;

namespace VoltstroStudios.UnityWebBrowser.Logging
{
    public sealed class DefaultUnityWebBrowserLogger : IWebBrowserLogger
    {
        private const string LoggingTag = "[Web Browser]";

        public void Debug(object message)
        {
            PassportLogger.Debug($"{LoggingTag} {message}");
        }

        public void Warn(object message)
        {
            PassportLogger.Warn($"{LoggingTag} {message}");
        }

        public void Error(object message)
        {
            PassportLogger.Error($"{LoggingTag} {message}");
        }
    }
}

#endif