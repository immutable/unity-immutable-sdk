using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VoltstroStudios.UnityWebBrowser.Core;
using Immutable.Passport.Auth;
using Newtonsoft.Json;
using Immutable.Passport.Utility;

namespace Immutable.Passport
{
    public class Passport : MonoBehaviour
    {
        public static Passport Instance { get; private set; }

        #if UNITY_STANDALONE_WIN || UNITY_STANDALONE_WIN
            [SerializeField] private BaseUwbClientManager clientManager;
            private WebBrowserClient webBrowserClient;
        #endif

        // Request ID to TaskCompletionSource
        // Storing TaskCompletionSource as an object as C# doesn't support wildcards like TaskCompletionSource<Any>
        // and using TaskCompletionSource<object> doesn't work. 
        // Future considerations: we could create a base class for the response type too TaskCompletionSource<BaseClass>
        IDictionary<string, object> requestTaskMap = new Dictionary<string, object>();

        // TODO find a better way of notifying when the web app is ready
        public event PassportReady OnReady;

        private AuthManager auth = new();

        void Start()
        {
            webBrowserClient = clientManager.browserClient;
            webBrowserClient.OnUnityPostMessage += OnUnityPostMessage;

            hideBrowser();
        }

        void Awake() {
            if (Instance == null) {
                Instance = this;
                // Keep this alive in every scene
                DontDestroyOnLoad(this.gameObject);
            }
        }

        #region Passport request

        public async Task<string> Connect() {
            string code = await auth.Login();
            Debug.Log($"Got code: {code}");
            return code;
        }

        User? u;

        public async Task ConfirmCode() {
            User user = await auth.ConfirmCode();
            bool success = await GetImxProvider(user);
            if (!success) {
                throw new Exception("Failed to get IMX provider");
            }
        }

        private Task<bool> GetImxProvider(User u) {
            // Only send necessary values
            GetImxProviderRequest user = new GetImxProviderRequest(u.idToken, u.accessToken, u.refreshToken, u.profile, u.etherKey);
            string data = JsonConvert.SerializeObject(user);

            return createCallTask<bool>(PassportFunction.GET_IMX_PROVIDER, data);
        }

        public Task<string?> GetAddress() {
            return createCallTask<string?>(PassportFunction.GET_ADDRESS);
        }

        public void Logout() {
            hideBrowser();
            call(PassportFunction.LOGOUT);
        }

        public string? GetAccessToken() {
            User? user = auth.GetUser();
            if (user != null) {
                return user.accessToken;
            } else {
                return null;
            }
        }

        public string? GetIdToken() {
            User? user = auth.GetUser();
            if (user != null) {
                return user.idToken;
            } else {
                return null;
            }
        }

        private Task<T> createCallTask<T>(string fxName, string? data = null) {
            return Task.Run(() => {
                var t = new TaskCompletionSource<T>();
                string requestId = call(fxName, data);

                // Add task completion source to the map so we can return the reponse
                requestTaskMap.Add(requestId, t);

                return t.Task;
            });
        }

        private string call(string fxName, string? data = null) {
            string requestId = Guid.NewGuid().ToString();
            Request request = new Request(fxName, requestId, data);
            // string requestJson = JsonUtility.ToJson(request).Replace("\\", "\\\\").Replace("\"", "\\\"");
            string requestJson = JsonConvert.SerializeObject(request).Replace("\\", "\\\\").Replace("\"", "\\\"");

            Debug.Log($"call: requestJson {requestJson}");

            // Call the function on the JS side
            string js = @$"callFunction(""{requestJson}"")";
            Debug.Log($"call js {js}");
            webBrowserClient.ExecuteJs(js);

            return requestId;
        }

        #endregion

        #region Passport response

        private void OnUnityPostMessage(string message) {
            Debug.Log($"[Unity] OnUnityPostMessage: {message}");
            switch (message) {
                // May change based on how we design the web app
                case "IMX_FUNCTIONS_READY":
                    OnReady?.Invoke();
                    break;
                default:
                    handleResponse(message);
                    break;
            }
        }

        private void handleResponse(string message) {
            Response? response = JsonUtility.FromJson<Response>(message);

            // Check if the reponse returned is valid and the task to return the reponse exists
            if (response != null && response.responseFor != null && response.requestId != null && requestTaskMap.ContainsKey(response.requestId)) {
                string requestId = response.requestId;

                // TODO handle errors from TS SDK
                // TODO throw error if task for request ID does not exist
                // TODO refactor dupliate code when get getting response and setting result
                switch (response.responseFor) {
                    case PassportFunction.GET_ADDRESS:
                        AddressResponse? addressResponse = JsonUtility.FromJson<AddressResponse>(message);
                        TaskCompletionSource<string?> addressCompletion = requestTaskMap[requestId] as TaskCompletionSource<string?>;
                        addressCompletion.TrySetResult(addressResponse?.address);
                        requestTaskMap.Remove(requestId);
                        break;
                    case PassportFunction.GET_IMX_PROVIDER:
                        GetImxProviderResponse? providerResponse = JsonUtility.FromJson<GetImxProviderResponse>(message);
                        TaskCompletionSource<bool> providerCompletion = requestTaskMap[requestId] as TaskCompletionSource<bool>;
                        providerCompletion.TrySetResult(providerResponse?.success == true);
                        requestTaskMap.Remove(requestId);
                        break;
                    default:
                        break;
                }
            }
        }

        #endregion

        #region Browser

        private void hideBrowser() {
            gameObject.transform.localScale = new Vector3(0, 0, 0);
        }

        #endregion
    }
}
