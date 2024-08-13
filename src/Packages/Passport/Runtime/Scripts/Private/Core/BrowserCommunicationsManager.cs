using System;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Immutable.Browser.Core;
using Immutable.Passport.Model;
using UnityEngine;
using UnityEngine.Scripting;
using Immutable.Passport.Helpers;
using Immutable.Passport.Core.Logging;
using Immutable.Passport.Event;

namespace Immutable.Passport.Core
{
    public delegate void OnBrowserReadyDelegate();

    public interface IBrowserCommunicationsManager
    {
#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX
        event OnUnityPostMessageDelegate OnAuthPostMessage;
        event OnUnityPostMessageErrorDelegate OnPostMessageError;
#endif
        void SetCallTimeout(int ms);
        void LaunchAuthURL(string url, string redirectUri);
        UniTask<string> Call(string fxName, string data = null, bool ignoreTimeout = false, Nullable<long> timeoutMs = null);
#if (UNITY_IPHONE && !UNITY_EDITOR) || (UNITY_ANDROID && !UNITY_EDITOR)
        void ClearCache(bool includeDiskFiles);
        void ClearStorage();
#endif
    }

    [Preserve]
    public class BrowserCommunicationsManager : IBrowserCommunicationsManager
    {
        private const string TAG = "[Browser Communications Manager]";

        // Used to notify that index.js file is loaded
        public const string INIT = "init";
        public const string INIT_REQUEST_ID = "1";

        private readonly IDictionary<string, UniTaskCompletionSource<string>> requestTaskMap = new Dictionary<string, UniTaskCompletionSource<string>>();
        private readonly IWebBrowserClient webBrowserClient;
        public event OnBrowserReadyDelegate OnReady;

        /// <summary>
        ///  PKCE in some platforms such as iOS and macOS will not trigger a deeplink and a proper callback needs to be
        ///  setup.
        /// </summary>
        public event OnUnityPostMessageDelegate OnAuthPostMessage;
        public event OnUnityPostMessageErrorDelegate OnPostMessageError;

        /// <summary>
        ///     Timeout time for waiting for each call to respond in milliseconds
        ///     Default value: 1 minute
        /// </summary>
        private int callTimeout = 60000;

        public BrowserCommunicationsManager(IWebBrowserClient webBrowserClient)
        {
            this.webBrowserClient = webBrowserClient;
            this.webBrowserClient.OnUnityPostMessage += InvokeOnUnityPostMessage;
#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX
            this.webBrowserClient.OnAuthPostMessage += InvokeOnAuthPostMessage;
            this.webBrowserClient.OnPostMessageError += InvokeOnPostMessageError;
#endif
        }

        #region Unity to Browser

        public void SetCallTimeout(int ms)
        {
            callTimeout = ms;
        }

        public UniTask<string> Call(string fxName, string data = null, bool ignoreTimeout = false, Nullable<long> timeoutMs = null)
        {
            var t = new UniTaskCompletionSource<string>();
            string requestId = Guid.NewGuid().ToString();
            // Add task completion source to the map so we can return the response
            requestTaskMap.Add(requestId, t);
            CallFunction(requestId, fxName, data);
            if (ignoreTimeout)
                return t.Task;
            else
                return t.Task.Timeout(TimeSpan.FromMilliseconds(timeoutMs ?? callTimeout));
        }

        private void CallFunction(string requestId, string fxName, string data = null)
        {
            BrowserRequest request = new BrowserRequest()
            {
                fxName = fxName,
                requestId = requestId,
                data = data
            };
            string requestJson = JsonUtility.ToJson(request).Replace("\\", "\\\\").Replace("\"", "\\\"");

            // Call the function on the JS side
            string js = $"callFunction(\"{requestJson}\")";

            if (fxName != PassportAnalytics.TRACK)
            {
                string dataString = data != null ? $": {data}" : "";
                PassportLogger.Info($"{TAG} Call {fxName} (request ID: {requestId}){dataString}");
            }
            else
            {
                PassportLogger.Debug($"{TAG} Call {fxName} (request ID: {requestId}): {js}");
            }

            webBrowserClient.ExecuteJs(js);
        }

        public void LaunchAuthURL(string url, string redirectUri)
        {
            PassportLogger.Info($"{TAG} LaunchAuthURL : {url}");
            webBrowserClient.LaunchAuthURL(url, redirectUri);
        }

#if (UNITY_IPHONE && !UNITY_EDITOR) || (UNITY_ANDROID && !UNITY_EDITOR)
        public void ClearCache(bool includeDiskFiles)
        {
            webBrowserClient.ClearCache(includeDiskFiles);
        }

        public void ClearStorage()
        {
            webBrowserClient.ClearStorage();
        }
#endif

        #endregion

        #region Browser to Unity

        private void InvokeOnUnityPostMessage(string message)
        {
            HandleResponse(message);
        }

        private void InvokeOnAuthPostMessage(string message)
        {
            PassportLogger.Info($"{TAG} Auth message received: {message}");
            if (OnAuthPostMessage != null)
            {
                OnAuthPostMessage.Invoke(message);
            }
        }

        private void InvokeOnPostMessageError(string id, string message)
        {
            PassportLogger.Info($"{TAG} Error message received ({id}): {message}");
            if (OnPostMessageError != null)
            {
                OnPostMessageError.Invoke(id, message);
            }
        }

        private void HandleResponse(string message)
        {
            PassportLogger.Debug($"{TAG} Handle response message: " + message);
            BrowserResponse response = message.OptDeserializeObject<BrowserResponse>();

            // Validate the deserialised response object
            if (response == null || string.IsNullOrEmpty(response.responseFor) || string.IsNullOrEmpty(response.requestId))
            {
                throw new PassportException("Response from browser is incorrect. Check game bridge file.");
            }

            string logMessage = $"{TAG} Response for: {response.responseFor} (request ID: {response.requestId}) : {message}";
            if (response.responseFor != PassportAnalytics.TRACK)
            {
                // Log info messages for valid responses not related to tracking
                PassportLogger.Info(logMessage);
            }
            else
            {
                PassportLogger.Debug(logMessage);
            }

            // Handle special case where the response indicates that the browser is ready
            if (response.responseFor == INIT && response.requestId == INIT_REQUEST_ID)
            {
                PassportLogger.Info($"{TAG} Browser is ready");
                if (OnReady != null)
                {
                    OnReady.Invoke();
                }
                return;
            }

            // Handle the response if a matching task exists for the request ID
            string requestId = response.requestId;
            if (requestTaskMap.ContainsKey(requestId))
            {
                NotifyRequestResult(requestId, message);
            }
            else
            {
                string errorMsg = $"No TaskCompletionSource for request id {requestId} found.";
                PassportLogger.Error(errorMsg);
                throw new PassportException(errorMsg);
            }
        }

        private PassportException ParseError(BrowserResponse response)
        {
            // Failed or error occured
            try
            {
                if (!String.IsNullOrEmpty(response.error) && !String.IsNullOrEmpty(response.errorType))
                {
                    PassportErrorType type = (PassportErrorType)System.Enum.Parse(typeof(PassportErrorType), response.errorType);
                    return new PassportException(response.error, type);
                }
                else if (!String.IsNullOrEmpty(response.error))
                {
                    return new PassportException(response.error);
                }
                else
                {
                    return new PassportException("Unknown error");
                }
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Parse passport type error: {ex.Message}");
            }
            return new PassportException(response.error ?? "Failed to parse error");
        }

        private void NotifyRequestResult(string requestId, string result)
        {
            BrowserResponse response = result.OptDeserializeObject<BrowserResponse>();
            UniTaskCompletionSource<string> completion = requestTaskMap[requestId] as UniTaskCompletionSource<string>;
            try
            {
                if (response.success == false || !String.IsNullOrEmpty(response.error))
                {
                    PassportException exception = ParseError(response);
                    if (!completion.TrySetException(exception))
                        throw new PassportException($"Unable to set exception for for request id {requestId}. Task has already been completed.");
                }
                else
                {
                    if (!completion.TrySetResult(result))
                        throw new PassportException($"Unable to set result for for request id {requestId}. Task has already been completed.");
                }
            }
            catch (ObjectDisposedException)
            {
                throw new PassportException($"Task for request id {requestId} has already been disposed and can't be updated.");
            }

            requestTaskMap.Remove(requestId);
        }

        #endregion

    }
}