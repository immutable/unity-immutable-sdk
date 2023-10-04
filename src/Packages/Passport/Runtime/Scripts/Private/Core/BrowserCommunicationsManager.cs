using System.Net;
using System;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using VoltstroStudios.UnityWebBrowser.Core;
#else
using Immutable.Browser.Gree;
#endif
using Immutable.Browser.Core;
using Immutable.Passport.Model;
using UnityEngine;
using Newtonsoft.Json;
using Immutable.Passport;

namespace Immutable.Passport.Core
{
    public delegate void OnBrowserReadyDelegate();

    public interface IBrowserCommunicationsManager
    {
        public event OnUnityPostMessageDelegate? OnAuthPostMessage;
        public event OnUnityPostMessageErrorDelegate? OnPostMessageError;
        public void SetCallTimeout(int ms);
        public void LaunchAuthURL(string url);
        public UniTask<string> Call(string fxName, string? data = null, bool ignoreTimeout = false);
    }

    public class BrowserCommunicationsManager : IBrowserCommunicationsManager
    {
        private const string TAG = "[Browser Communications Manager]";

        // Used to notify that index.js file is loaded
        public const string INIT = "init";
        public const string INIT_REQUEST_ID = "1";

        private readonly IDictionary<string, UniTaskCompletionSource<string>> requestTaskMap = new Dictionary<string, UniTaskCompletionSource<string>>();
        private readonly IWebBrowserClient webBrowserClient;
        public event OnBrowserReadyDelegate? OnReady;

        /// <summary>
        ///  PKCE in some platforms such as iOS and macOS will not trigger a deeplink and a proper callback needs to be
        ///  setup.
        /// </summary>
        public event OnUnityPostMessageDelegate? OnAuthPostMessage;
        public event OnUnityPostMessageErrorDelegate? OnPostMessageError;

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

        public UniTask<string> Call(string fxName, string? data = null, bool ignoreTimeout = false)
        {
            var t = new UniTaskCompletionSource<string>();
            string requestId = Guid.NewGuid().ToString();
            // Add task completion source to the map so we can return the response
            requestTaskMap.Add(requestId, t);
            CallFunction(requestId, fxName, data);
            if (ignoreTimeout)
                return t.Task;
            else
                return t.Task.Timeout(TimeSpan.FromMilliseconds(callTimeout));
        }

        private void CallFunction(string requestId, string fxName, string? data = null)
        {
            Debug.Log($"{TAG} Call {fxName} (request ID: {requestId})");

            BrowserRequest request = new()
            {
                FxName = fxName,
                RequestId = requestId,
                Data = data
            };
            string requestJson = JsonConvert.SerializeObject(request).Replace("\\", "\\\\").Replace("\"", "\\\"");

            // Call the function on the JS side
            string js = @$"callFunction(""{requestJson}"")";
            webBrowserClient.ExecuteJs(js);
        }

        public void LaunchAuthURL(string url)
        {
            Debug.Log($"{TAG} LaunchAuthURL : {url}");
            webBrowserClient.LaunchAuthURL(url);
        }

        private void InvokeOnUnityPostMessage(string message)
        {
            Debug.Log($"{TAG} InvokeOnUnityPostMessage: {message}");
            HandleResponse(message);
        }

        #endregion

        #region Browser to Unity

        private void InvokeOnAuthPostMessage(string message)
        {
            Debug.Log($"{TAG} InvokeOnAuthPostMessage: {message}");
            OnAuthPostMessage?.Invoke(message);
        }

        private void InvokeOnPostMessageError(string id, string message)
        {
            Debug.Log($"{TAG} InvokeOnPostMessageError id: {id} message: {message}");
            OnPostMessageError?.Invoke(id, message);
        }

        private void HandleResponse(string message)
        {
            Debug.Log($"{TAG} HandleResponse message: " + message);
            BrowserResponse? response = JsonConvert.DeserializeObject<BrowserResponse?>(message);

            // Check if the reponse returned is valid and the task to return the reponse exists
            if (response == null || response.ResponseFor == null || response.RequestId == null)
            {
                throw new PassportException($"Response from browser is incorrect. Check HTML/JS files.");
            }

            // Special case to detect if index.js is loaded
            if (response.ResponseFor == INIT && response.RequestId == INIT_REQUEST_ID)
            {
                Debug.Log($"{TAG} Browser is ready");
                OnReady?.Invoke();
                return;
            }

            string requestId = response.RequestId;
            PassportException? exception = ParseError(response);

            if (requestTaskMap.ContainsKey(requestId))
            {
                NotifyRequestResult(requestId, message, exception);
            }
            else
            {
                throw new PassportException($"No TaskCompletionSource for request id {requestId} found.");
            }
        }

        private PassportException? ParseError(BrowserResponse response)
        {
            if (response.Success == false || !String.IsNullOrEmpty(response.Error))
            {
                // Failed or error occured
                try
                {
                    if (response.Error != null && response.ErrorType != null)
                    {
                        PassportErrorType type = (PassportErrorType)System.Enum.Parse(typeof(PassportErrorType), response.ErrorType);
                        return new PassportException(response.Error, type);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{TAG} Parse passport type error: {ex.Message}");
                }
                return new PassportException(response.Error ?? "Failed to parse error");
            }
            else
            {
                // No error
                return null;
            }
        }

        private void NotifyRequestResult(string requestId, string result, PassportException? e)
        {
            UniTaskCompletionSource<string>? completion = requestTaskMap[requestId] as UniTaskCompletionSource<string>;
            try
            {
                if (e != null)
                {
                    if (!completion.TrySetException(e))
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