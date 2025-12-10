using Immutable.Passport.Core.Logging;
using VoltstroStudios.UnityWebBrowser.Core;
using VoltstroStudios.UnityWebBrowser.Logging;
using VoltstroStudios.UnityWebBrowser.Shared;

namespace Immutable.Passport
{
    internal static class UwbLogConfig
    {
        internal static void ApplyTo(WebBrowserClient browserClient)
        {
            if (browserClient == null)
            {
                return;
            }

            browserClient.logSeverity = PassportLogger.CurrentLogLevel switch
            {
                LogLevel.Debug => LogSeverity.Debug,
                LogLevel.Warn  => LogSeverity.Warn,
                LogLevel.Error => LogSeverity.Error,
                _              => LogSeverity.Info
            };

            if (browserClient.Logger is not DefaultUnityWebBrowserLogger)
            {
                browserClient.Logger = new DefaultUnityWebBrowserLogger();
            }
        }
    }
}