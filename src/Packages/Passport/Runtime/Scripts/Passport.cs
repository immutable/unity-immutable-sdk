using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VoltstroStudios.UnityWebBrowser.Core;
using Immutable.Passport.Auth;
using Newtonsoft.Json;
using Immutable.Passport.Utility;
using System.IO;

namespace Immutable.Passport
{
    public class Passport : MonoBehaviour
    {
        private const string TAG = "[Passport]";

        private const string INITIAL_URL = "https://www.immutable.com/";

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

        public event PassportReadyDelegate OnReady;

        private AuthManager auth = new();

        async void Start()
        {
            webBrowserClient = clientManager.browserClient;
            webBrowserClient.OnUnityPostMessage += OnUnityPostMessage;
            webBrowserClient.OnLoadFinish += OnLoadFinish;
        }

        void Awake() {
            if (Instance == null) {
                Instance = this;

                // Keep this alive in every scene
                DontDestroyOnLoad(this.gameObject);
            }
        }

        public void Destroy() {
            Instance = null;
            Destroy(this.gameObject);
        }

        private void OnLoadFinish(string url) {
            Debug.Log($"{TAG} On load finish: {url}");
            if (url.StartsWith(INITIAL_URL)) {
                Debug.Log($"{TAG} Browser is ready");
                // Browser is considered ready to load local HTML file
                // Once the initial URL is loaded 
                string filePath = Path.GetFullPath("Packages/com.immutable.passport/Runtime/Assets/Resources/passport.html");
                webBrowserClient.LoadUrl($"file:///{filePath}");
                OnReady?.Invoke();
                // Clean up listener
                webBrowserClient.OnLoadFinish -= OnLoadFinish;
            }
        }

        #region Passport request

        /// <summary>
        /// Connects the user to Passport.
        ///
        /// If code confirmation is required, call ConfirmCode().
        /// <returns>
        /// The end-user verification code if confirmation is required, otherwise null;
        /// </returns>
        /// </summary>
        public async Task<string?> Connect() {
            string? code = await auth.Login();
            User? user = auth.GetUser();
            if (code != null) {
                // Code confirmation required
                return code;
            } else if (user != null) {
                // Credentials are still valid, get provider
                await GetImxProvider(user);
                return null;
            } else {
                // Should never get to here, but if it happens, log the user to reset everything
                auth.Logout();
                throw new InvalidOperationException("Something went wrong, call Connect() again");
            }
        }

        User? u;

        public async Task ConfirmCode() {
            User user = await auth.ConfirmCode();
            await GetImxProvider(user);
        }

        private async Task GetImxProvider(User u) {
            // Only send necessary values
            GetImxProviderRequest user = new GetImxProviderRequest(u.idToken, u.accessToken, u.refreshToken, u.profile, u.etherKey);
            string data = JsonConvert.SerializeObject(user);

            bool success = await createCallTask<bool>(PassportFunction.GET_IMX_PROVIDER, data);
            if (!success) {
                throw new Exception("Failed to get IMX provider");
            }
        }

        public Task<string?> GetAddress() {
            return createCallTask<string?>(PassportFunction.GET_ADDRESS);
        }

        public void Logout() {
            auth.Logout();
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

        public Task<string?> SignMessage(string message) {
            return createCallTask<string?>(PassportFunction.SIGN_MESSAGE, message);
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
            string requestJson = JsonConvert.SerializeObject(request).Replace("\\", "\\\\").Replace("\"", "\\\"");

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
            handleResponse(message);
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
                        addressCompletion.SetResult(addressResponse?.address);
                        requestTaskMap.Remove(requestId);
                        break;
                    case PassportFunction.GET_IMX_PROVIDER:
                        GetImxProviderResponse? providerResponse = JsonUtility.FromJson<GetImxProviderResponse>(message);
                        TaskCompletionSource<bool> providerCompletion = requestTaskMap[requestId] as TaskCompletionSource<bool>;
                        bool success = providerResponse?.success == true;
                        if (success) {
                            providerCompletion.SetResult(success);
                        } else {
                            providerCompletion.SetException(new Exception(providerResponse?.error));
                        }
                        requestTaskMap.Remove(requestId);
                        break;
                    case PassportFunction.SIGN_MESSAGE:
                        SignMessageResponse? signResponse = JsonUtility.FromJson<SignMessageResponse>(message);
                        TaskCompletionSource<string?> signCompletion = requestTaskMap[requestId] as TaskCompletionSource<string?>;
                        signCompletion.SetResult(signResponse?.result);
                        requestTaskMap.Remove(requestId);
                        break;
                    default:
                        break;
                }
            }
        }

        #endregion
    }
}
