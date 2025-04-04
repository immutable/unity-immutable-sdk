#if !IMMUTABLE_CUSTOM_BROWSER && (UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN))

// UnityWebBrowser (UWB)
// Copyright (c) 2021-2022 Voltstro-Studios
// 
// This project is under the MIT license. See the LICENSE.md file for more details.

using System;
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
        
        /// <summary>
        /// A function that defines how sensitive data should be redacted.
        /// If null, no redaction is applied.
        /// </summary>
        public Func<string, string>? redactionHandler;

        public DefaultUnityWebBrowserLogger(Func<string, string>? redactionHandler = null)
        {
            logger = UnityEngine.Debug.unityLogger;
            this.redactionHandler = redactionHandler;
        }

        public void Debug(object message)
        {
            logger.Log(LogType.Log, LoggingTag, redactIfRequired(message));
        }

        public void Warn(object message)
        {
            logger.LogWarning(LoggingTag, redactIfRequired(message));
        }

        public void Error(object message)
        {
            logger.LogError(LoggingTag, redactIfRequired(message));
        }

        private object redactIfRequired(object message)
        {
            if (redactionHandler != null && message is string)
            {
                return redactionHandler((string)message);
            }

            return message;
        }
    }
}

#endif