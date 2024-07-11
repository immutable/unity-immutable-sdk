using System.Net;
using System;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
using VoltstroStudios.UnityWebBrowser.Core;
#else
using Immutable.Browser.Gree;
#endif
using Immutable.Browser.Core;
using Immutable.Passport.Model;
using UnityEngine;
using UnityEngine.Scripting;
using Immutable.Passport.Helpers;

namespace Immutable.Passport.Core
{
    public delegate void OnBrowserReadyDelegate();

    public interface IBrowserCommunicationsManager
    {
        event OnUnityPostMessageDelegate OnAuthPostMessage;
        event OnUnityPostMessageErrorDelegate OnPostMessageError;
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
            this.webBrowserClient.OnAuthPostMessage += InvokeOnAuthPostMessage;
            this.webBrowserClient.OnPostMessageError += InvokeOnPostMessageError;
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
            Debug.Log($"{TAG} Call {fxName} (request ID: {requestId}, js: {js})");
            webBrowserClient.ExecuteJs(js);
        }

        public void LaunchAuthURL(string url, string redirectUri)
        {
            Debug.Log($"{TAG} LaunchAuthURL : {url}");
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
            Debug.Log($"{TAG} InvokeOnUnityPostMessage: {message}");
            HandleResponse(message);
        }

        private void InvokeOnAuthPostMessage(string message)
        {
            Debug.Log($"{TAG} InvokeOnAuthPostMessage: {message}");
            if (OnAuthPostMessage != null)
            {
                OnAuthPostMessage.Invoke(message);
            }
        }

        private void InvokeOnPostMessageError(string id, string message)
        {
            Debug.Log($"{TAG} InvokeOnPostMessageError id: {id} message: {message}");
            if (OnPostMessageError != null)
            {
                OnPostMessageError.Invoke(id, message);
            }
        }

        private void HandleResponse(string message)
        {
            Debug.Log($"{TAG} HandleResponse message: " + message);
            BrowserResponse response = message.OptDeserializeObject<BrowserResponse>();

            // Check if the reponse returned is valid and the task to return the reponse exists
            if (response == null || String.IsNullOrEmpty(response.responseFor) || String.IsNullOrEmpty(response.requestId))
            {
                throw new PassportException($"Response from browser is incorrect. Check HTML/JS files.");
            }

            // Special case to detect if index.js is loaded
            if (response.responseFor == INIT && response.requestId == INIT_REQUEST_ID)
            {
                Debug.Log($"{TAG} Browser is ready");
                if (OnReady != null)
                {
                    OnReady.Invoke();
                }
                return;
            }

            string requestId = response.requestId;
            if (requestTaskMap.ContainsKey(requestId))
            {
                NotifyRequestResult(requestId, message);
            }
            else
            {
                throw new PassportException($"No TaskCompletionSource for request id {requestId} found.");
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
                Debug.LogError($"{TAG} Parse passport type error: {ex.Message}");
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