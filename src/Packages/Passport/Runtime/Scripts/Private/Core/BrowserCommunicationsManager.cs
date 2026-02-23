using System;
using Cysharp.Threading.Tasks;
using Immutable.Browser.Core;
using Immutable.Passport.Model;
using UnityEngine.Scripting;
using Immutable.Passport.Helpers;
using Immutable.Passport.Core.Logging;
using Immutable.Passport.Event;

namespace Immutable.Passport.Core
{
    public delegate void OnBrowserReadyDelegate();

    public interface IBrowserCommunicationsManager
    {
#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX || UNITY_WEBGL
        /// <summary>
        /// Raised when auth-specific messages are received from the game bridge.
        /// </summary>
        event OnUnityPostMessageDelegate OnAuthPostMessage;

        /// <summary>
        /// Raised when the game bridge reports a post-message error.
        /// </summary>
        event OnUnityPostMessageErrorDelegate OnPostMessageError;
#endif

        /// <summary>
        /// Sets the default timeout used by calls.
        /// </summary>
        void SetCallTimeout(int ms);

        /// <summary>
        /// Opens the auth URL in the platform browser.
        /// </summary>
        void LaunchAuthURL(string url, string redirectUri);

        /// <summary>
        /// Sends a function call to the game bridge.
        /// </summary>
        UniTask<string> Call(string fxName, string? data = null, bool ignoreTimeout = false, Nullable<long> timeoutMs = null);
#if (UNITY_IPHONE && !UNITY_EDITOR) || (UNITY_ANDROID && !UNITY_EDITOR)
        /// <summary>
        /// Clears browser cache used by the embedded web view.
        /// </summary>
        void ClearCache(bool includeDiskFiles);

        /// <summary>
        /// Clears browser storage used by the embedded web view.
        /// </summary>
        void ClearStorage();
#endif
    }

    /// <summary>
    /// Orchestrates communication between Unity and the game bridge.
    /// Delegates serialization to <see cref="BrowserMessageCodec"/>,
    /// error mapping to <see cref="BrowserResponseErrorMapper"/>,
    /// and request lifecycle to <see cref="PendingRequestRegistry"/>.
    /// </summary>
    [Preserve]
    public class BrowserCommunicationsManager : IBrowserCommunicationsManager
    {
        private const string TAG = "[Browser Communications Manager]";
        private const int DEFAULT_CALL_TIMEOUT_MS = 60000;

        internal const string INIT = "init";
        internal const string INIT_REQUEST_ID = "1";

        private readonly PendingRequestRegistry _pendingRequests = new PendingRequestRegistry();
        private readonly IWebBrowserClient _webBrowserClient;

        /// <summary>
        /// Raised when the game bridge signals that it is ready.
        /// </summary>
        public event OnBrowserReadyDelegate? OnReady;

        /// <summary>
        /// PKCE in some platforms such as iOS and macOS will not trigger a deeplink
        /// and a proper callback needs to be setup.
        /// </summary>
        public event OnUnityPostMessageDelegate? OnAuthPostMessage;
        public event OnUnityPostMessageErrorDelegate? OnPostMessageError;

        /// <summary>
        /// Timeout for waiting for each call to respond in milliseconds. Default: 1 minute.
        /// </summary>
        private int _callTimeout = DEFAULT_CALL_TIMEOUT_MS;

        /// <summary>
        /// Wires browser client callbacks into this manager.
        /// </summary>
        public BrowserCommunicationsManager(IWebBrowserClient webBrowserClient)
        {
            _webBrowserClient = webBrowserClient;
            _webBrowserClient.OnUnityPostMessage += InvokeOnUnityPostMessage;
#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX || UNITY_WEBGL
            _webBrowserClient.OnAuthPostMessage += InvokeOnAuthPostMessage;
            _webBrowserClient.OnPostMessageError += InvokeOnPostMessageError;
#endif
        }

        #region Unity to Game Bridge

        /// <summary>
        /// Updates the call timeout.
        /// </summary>
        public void SetCallTimeout(int ms)
        {
            _callTimeout = ms;
        }

        /// <summary>
        /// Calls a function in the game bridge.
        /// </summary>
        public UniTask<string> Call(string fxName, string? data = null, bool ignoreTimeout = false, long? timeoutMs = null)
        {
            var requestId = Guid.NewGuid().ToString();
            var completion = _pendingRequests.Register(requestId);

            var request = new BrowserRequest(fxName, requestId, data);
            var js = BrowserMessageCodec.BuildJsCall(request);

            LogOutgoingCall(fxName, requestId, data, js);

            _webBrowserClient.ExecuteJs(js);

            return ignoreTimeout
                ? completion.Task
                : completion.Task.Timeout(TimeSpan.FromMilliseconds(timeoutMs ?? _callTimeout));
        }

        /// <summary>
        /// Opens the external auth flow using the game bridge.
        /// </summary>
        public void LaunchAuthURL(string url, string redirectUri)
        {
            PassportLogger.Info($"{TAG} LaunchAuthURL : {url}");
            _webBrowserClient.LaunchAuthURL(url, redirectUri);
        }

#if (UNITY_IPHONE && !UNITY_EDITOR) || (UNITY_ANDROID && !UNITY_EDITOR)
        /// <summary>
        /// Clears cache in mobile runtime environments.
        /// </summary>
        public void ClearCache(bool includeDiskFiles)
        {
            _webBrowserClient.ClearCache(includeDiskFiles);
        }

        /// <summary>
        /// Clears persisted storage in mobile runtime environments.
        /// </summary>
        public void ClearStorage()
        {
            _webBrowserClient.ClearStorage();
        }
#endif

        #endregion

        #region Game Bridge to Unity

        /// <summary>
        /// Entry point for generic post messages from the game bridge.
        /// </summary>
        private void InvokeOnUnityPostMessage(string message)
        {
            HandleResponse(message);
        }

        /// <summary>
        /// Forwards auth-specific game bridge messages to consumers.
        /// </summary>
        private void InvokeOnAuthPostMessage(string message)
        {
            PassportLogger.Info($"{TAG} Auth message received: {message}");
            OnAuthPostMessage?.Invoke(message);
        }

        /// <summary>
        /// Forwards game bridge post-message errors to consumers.
        /// </summary>
        private void InvokeOnPostMessageError(string id, string message)
        {
            PassportLogger.Info($"{TAG} Error message received ({id}): {message}");
            OnPostMessageError?.Invoke(id, message);
        }

        /// <summary>
        /// Handles a game bridge response message.
        /// </summary>
        private void HandleResponse(string message)
        {
            PassportLogger.Debug($"{TAG} Handle response message: " + message);
            var response = BrowserMessageCodec.ParseAndValidateResponse(message);

            LogIncomingResponse(response, message);

            if (response.responseFor == INIT && response.requestId == INIT_REQUEST_ID)
            {
                PassportLogger.Info($"{TAG} Game bridge is ready");
                OnReady?.Invoke();
                return;
            }

            string requestId = response.requestId;
            if (!_pendingRequests.Contains(requestId))
            {
                string errorMsg = $"No TaskCompletionSource for request id {requestId} found.";
                PassportLogger.Error(errorMsg);
                throw new PassportException(errorMsg);
            }

            NotifyRequestResult(requestId, response, message);
        }

        /// <summary>
        /// Completes the pending request task with success or failure.
        /// </summary>
        private void NotifyRequestResult(string requestId, BrowserResponse response, string rawMessage)
        {
            var completion = _pendingRequests.Get(requestId);
            try
            {
                if (response.success == false || !string.IsNullOrEmpty(response.error))
                {
                    var exception = BrowserResponseErrorMapper.MapToException(response);
                    if (!completion.TrySetException(exception))
                        throw new PassportException($"Unable to set exception for request id {requestId}. Task has already been completed.");
                }
                else
                {
                    if (!completion.TrySetResult(rawMessage))
                        throw new PassportException($"Unable to set result for request id {requestId}. Task has already been completed.");
                }
            }
            catch (ObjectDisposedException)
            {
                throw new PassportException($"Task for request id {requestId} has already been disposed and can't be updated.");
            }

            _pendingRequests.Remove(requestId);
        }

        /// <summary>
        /// Logs game bridge responses, using debug level for analytics tracking calls.
        /// </summary>
        private static void LogIncomingResponse(BrowserResponse response, string rawMessage)
        {
            string logMessage = $"{TAG} Response for: {response.responseFor} (request ID: {response.requestId}) : {rawMessage}";
            if (response.responseFor != PassportAnalytics.TRACK)
            {
                PassportLogger.Info(logMessage);
            }
            else
            {
                PassportLogger.Debug(logMessage);
            }
        }

        /// <summary>
        /// Logs outgoing calls, using debug level for analytics tracking calls.
        /// </summary>
        private static void LogOutgoingCall(string fxName, string requestId, string? data, string js)
        {
            if (fxName != PassportAnalytics.TRACK)
            {
                string dataString = data != null ? $": {data}" : "";
                PassportLogger.Info($"{TAG} Call {fxName} (request ID: {requestId}){dataString}");
            }
            else
            {
                PassportLogger.Debug($"{TAG} Call {fxName} (request ID: {requestId}): {js}");
            }
        }

        #endregion
    }
}
