#if !IMMUTABLE_CUSTOM_BROWSER && (UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN))

// UnityWebBrowser (UWB)
// Copyright (c) 2021-2022 Voltstro-Studios
// 
// This project is under the MIT license. See the LICENSE.md file for more details.

using UnityEngine;

namespace VoltstroStudios.UnityWebBrowser.Logging
{
    /// <summary>
    ///     An <see cref="IWebBrowserLogger" /> using Unity's <see cref="ILogger" />
    /// </summary>
    public sealed class DefaultUnityWebBrowserLogger : IWebBrowserLogger
    {
        private const string LoggingTag = "[UWB]";

        private readonly ILogger logger;

        public DefaultUnityWebBrowserLogger()
        {
            logger = UnityEngine.Debug.unityLogger;
        }

        public void Debug(object message)
        {
            logger.Log(LogType.Log, LoggingTag, message);
        }

        public void Warn(object message)
        {
            logger.LogWarning(LoggingTag, message);
        }

        public void Error(object message)
        {
            logger.LogError(LoggingTag, message);
        }
    }
}

#endif