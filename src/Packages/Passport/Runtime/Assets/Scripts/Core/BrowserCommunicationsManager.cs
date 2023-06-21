using System.Net;
using System;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using VoltstroStudios.UnityWebBrowser.Core;
using Immutable.Passport.Model;
using UnityEngine;
using Newtonsoft.Json;
using Immutable.Passport;

namespace Immutable.Passport.Core
{
    public delegate void OnBrowserReadyDelegate();
    public class BrowserCommunicationsManager
    {
        private const string TAG = "[Browser Communications Manager]";

        // Used to notify that index.js file is loaded
        public const string INIT = "init";
        public const string INIT_REQUEST_ID = "1";

        private readonly IDictionary<string, UniTaskCompletionSource<string>> requestTaskMap = new Dictionary<string, UniTaskCompletionSource<string>>();
        private readonly IWebBrowserClient webBrowserClient;
        public event OnBrowserReadyDelegate OnReady;

        /// <summary>
        ///     Timeout time for waiting for each call to respond in milliseconds
        ///     Default value: 1 minute
        /// </summary>
        public int callTimeout = 60000;

        public BrowserCommunicationsManager(IWebBrowserClient webBrowserClient)
        {
            this.webBrowserClient = webBrowserClient;
            this.webBrowserClient.OnUnityPostMessage += OnUnityPostMessage;
        }

        #region Unity to Browser

        public UniTask<string> Call(string fxName, string? data = null)
        {
            var t = new UniTaskCompletionSource<string>();
            string requestId = Guid.NewGuid().ToString();
            // Add task completion source to the map so we can return the response
            requestTaskMap.Add(requestId, t);
            CallFunction(requestId, fxName, data);
            return t.Task
                .Timeout(TimeSpan.FromMilliseconds(callTimeout));;
        }

        private void CallFunction(string requestId, string fxName, string? data = null)
        {
            Debug.Log($"{TAG} Call {fxName} (request ID: {requestId})");

            Request request = new(fxName, requestId, data);
            string requestJson = JsonConvert.SerializeObject(request).Replace("\\", "\\\\").Replace("\"", "\\\"");

            // Call the function on the JS side
            string js = @$"callFunction(""{requestJson}"")";
            webBrowserClient.ExecuteJs(js);
        }

        #endregion

        #region Browser to Unity

        private void OnUnityPostMessage(string message)
        {
            Debug.Log($"{TAG} OnUnityPostMessage: {message}");
            HandleResponse(message);
        }

        private void HandleResponse(string message)
        {
            Response? response = JsonUtility.FromJson<Response>(message);

            // Check if the reponse returned is valid and the task to return the reponse exists
            if (response == null || response.responseFor == null || response.requestId == null)
            {
                Debug.Log($"{TAG} Response from browser is incorrect. Check HTML/JS files.");
                return;
            }

            // Special case to detect if index.js is loaded
            if (response.responseFor == INIT && response.requestId == INIT_REQUEST_ID) {
                Debug.Log($"{TAG} Browser is ready");
                OnReady?.Invoke();
                return;
            }

            string requestId = response.requestId;
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

        private PassportException? ParseError(Response response)
        {
            if (response.success == false || !String.IsNullOrEmpty(response.error))
            {
                // Failed or error occured
                try
                {
                    if (response.error != null && response.errorType != null)
                    {
                        PassportErrorType type = (PassportErrorType)System.Enum.Parse(typeof(PassportErrorType), response.errorType);
                        return new PassportException(response.error, type);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log($"{TAG} Parse passport type error: {ex.Message}");
                }
                return new PassportException(response.error ?? "Failed to parse error");
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