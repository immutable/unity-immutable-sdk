using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using VoltstroStudios.UnityWebBrowser.Core;
using Immutable.Passport.Model;
using UnityEngine;
using Newtonsoft.Json;

namespace Immutable.Passport.Core {
    internal class BrowserCommunicationsManager 
    {
        private const string TAG = "[Browser Communications Manager]";

        // Request ID to TaskCompletionSource
        // Storing TaskCompletionSource as an object as C# doesn't support wildcards like TaskCompletionSource<Any>
        // and using TaskCompletionSource<object> doesn't work. 
        private IDictionary<string, object> requestTaskMap = new Dictionary<string, object>();

        private WebBrowserClient webBrowserClient;

        internal BrowserCommunicationsManager(WebBrowserClient webBrowserClient) {
            this.webBrowserClient = webBrowserClient;
            this.webBrowserClient.OnUnityPostMessage += OnUnityPostMessage;
        }

        #region Unity to Browser

        internal Task<T> Call<T>(string fxName, string? data = null) {
            var t = new TaskCompletionSource<T>();
            string requestId = CallFunction(fxName, data);

            // Add task completion source to the map so we can return the reponse
            requestTaskMap.Add(requestId, t);

            return t.Task;
        }

        private string CallFunction(string fxName, string? data = null) {
            string requestId = Guid.NewGuid().ToString();
            Debug.Log($"{TAG} Call {fxName} (request ID: {requestId})");

            Request request = new Request(fxName, requestId, data);
            string requestJson = JsonConvert.SerializeObject(request).Replace("\\", "\\\\").Replace("\"", "\\\"");

            // Call the function on the JS side
            string js = @$"callFunction(""{requestJson}"")";
            webBrowserClient.ExecuteJs(js);

            return requestId;
        }

        #endregion

        #region Browser to Unity

        private void OnUnityPostMessage(string message) {
            Debug.Log($"[Unity] OnUnityPostMessage: {message}");
            HandleResponse(message);
        }

        private void HandleResponse(string message) {
            Response? response = JsonUtility.FromJson<Response>(message);

            // Check if the reponse returned is valid and the task to return the reponse exists
            if (response == null || response.responseFor == null || response.requestId == null)
                return;

            string requestId = response.requestId;
            PassportException? exception = ParseError(response);

            if (requestTaskMap.ContainsKey(requestId)) {
                switch (response.responseFor) {
                    case PassportFunction.GET_ADDRESS:
                        NotifyRequestResult<string?>(requestId, JsonUtility.FromJson<AddressResponse>(message)?.address, exception);
                        break;
                    case PassportFunction.GET_IMX_PROVIDER:
                        NotifyRequestResult<bool>(requestId, response.success, exception);
                        break;
                    case PassportFunction.SIGN_MESSAGE:
                        NotifyRequestResult<string?>(requestId, JsonUtility.FromJson<SignMessageResponse>(message)?.result, exception);
                        break;
                    default:
                        break;
                }
            } else {
                throw new PassportException($"No TaskCompletionSource for request id {requestId} found.");
            }
        }

        private PassportException? ParseError(Response? response) {
            if (response != null && (response.success == false || response.error != null)) {
                // Failed or error occured
                try {
                    if (response.errorType != null) {
                        PassportErrorType type = (PassportErrorType) System.Enum.Parse(typeof(PassportErrorType), response.errorType);
                        return new PassportException(response.error, type);
                    }
                } catch (Exception ex) {
                    Debug.Log($"{TAG} Parse passport type error: {ex.Message}");
                }
                return new PassportException(response.error);
            } else {
                // No error
                return null;
            }
        }
        
        private void NotifyRequestResult<T>(string requestId, T result, PassportException? e)
        {
            TaskCompletionSource<T?> completion = requestTaskMap[requestId] as TaskCompletionSource<T?>;
            try {
                if (e != null) {
                    if (!completion.TrySetException(e))
                        throw new PassportException($"Unable to set exception for for request id {requestId}. Task has already been completed.");
                } else {
                    if(!completion.TrySetResult(result))
                        throw new PassportException($"Unable to set result for for request id {requestId}. Task has already been completed.");
                }
            } catch (ObjectDisposedException exception) {
                throw new PassportException($"Task for request id {requestId} has already been disposed and can't be updated.");
            }

            requestTaskMap.Remove(requestId);
        }

        #endregion

    }
}