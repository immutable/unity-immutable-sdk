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
        private const string GAME_OBJECT_NAME = "Passport";

        private const string INITIAL_URL = "https://www.immutable.com/";
        private const string SCHEME_FILE = "file:///";
        private const string PASSPORT_PACKAGE_RESOURCES_DIRECTORY = "Packages/com.immutable.passport/Runtime/Assets/Resources";
        private const string PASSPORT_DATA_DIRECTORY_NAME = "/Passport";
        private const string PASSPORT_HTML_FILE_NAME = "/passport.html";

        public static Passport Instance { get; private set; }

        #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            private WebBrowserClient webBrowserClient = new();
        #endif

        // Request ID to TaskCompletionSource
        // Storing TaskCompletionSource as an object as C# doesn't support wildcards like TaskCompletionSource<Any>
        // and using TaskCompletionSource<object> doesn't work. 
        IDictionary<string, object> requestTaskMap = new Dictionary<string, object>();

        public event PassportReadyDelegate OnReady;

        private AuthManager auth = new();

        public static Passport Init() {
            if (Instance == null) {
                // Create game object, so we dispose the browser on destroy
                GameObject go = new GameObject(GAME_OBJECT_NAME);
                // Add passport to the game object
                go.AddComponent<Passport>();
                // Save passport instance
                Instance = go.GetComponent<Passport>();
            }
            return Instance;
        }

        private void Start()
        {
            webBrowserClient.Init();
            webBrowserClient.OnUnityPostMessage += OnUnityPostMessage;
            webBrowserClient.OnLoadFinish += OnLoadFinish;
        }

        private void Awake() {
            // Keep this alive in every scene
            DontDestroyOnLoad(this.gameObject);
        }

        private void OnDestroy()
        {
            webBrowserClient.Dispose();
        }

        private void OnLoadFinish(string url) {
            Debug.Log($"{TAG} On load finish: {url}");
            if (url.StartsWith(INITIAL_URL)) {
                Debug.Log($"{TAG} Browser is ready");
                // Browser is considered ready to load local HTML file
                // Once the initial URL is loaded 
                string filePath = "";
#if UNITY_EDITOR
                filePath = SCHEME_FILE + Path.GetFullPath($"{PASSPORT_PACKAGE_RESOURCES_DIRECTORY}{PASSPORT_HTML_FILE_NAME}");
#else
#if UNITY_STANDALONE_WIN
                filePath = SCHEME_FILE + Path.GetFullPath(Application.dataPath) + PASSPORT_DATA_DIRECTORY_NAME + PASSPORT_HTML_FILE_NAME;
#endif     
#endif
                webBrowserClient.LoadUrl(filePath);
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

        public async Task ConfirmCode() {
            User user = await auth.ConfirmCode();
            await GetImxProvider(user);
        }

        private async Task GetImxProvider(User u) {
            // Only send necessary values
            GetImxProviderRequest request = new GetImxProviderRequest(u.idToken, u.accessToken, u.refreshToken, u.profile, u.etherKey);
            string data = JsonConvert.SerializeObject(request);

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
            var t = new TaskCompletionSource<T>();
            string requestId = call(fxName, data);

            // Add task completion source to the map so we can return the reponse
            requestTaskMap.Add(requestId, t);

            return t.Task;
        }

        private string call(string fxName, string? data = null) {
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

        #region Passport response

        private void OnUnityPostMessage(string message) {
            Debug.Log($"[Unity] OnUnityPostMessage: {message}");
            handleResponse(message);
        }

        private void handleResponse(string message) {
            Response? response = JsonUtility.FromJson<Response>(message);

            // Check if the reponse returned is valid and the task to return the reponse exists
            if (response == null || response.responseFor == null || response.requestId == null)
                return;

            string requestId = response.requestId;
            Exception? exception = (response.success == false || response.error != null) ? new Exception(response.error) : null;

            if (requestTaskMap.ContainsKey(requestId)) {
                switch (response.responseFor) {
                    case PassportFunction.GET_ADDRESS:
                        notifyRequestResult<string?>(requestId, JsonUtility.FromJson<AddressResponse>(message)?.address, exception);
                        break;
                    case PassportFunction.GET_IMX_PROVIDER:
                        notifyRequestResult<bool>(requestId, response.success, exception);
                        break;
                    case PassportFunction.SIGN_MESSAGE:
                        notifyRequestResult<string?>(requestId, JsonUtility.FromJson<SignMessageResponse>(message)?.result, exception);
                        break;
                    default:
                        break;
                }
            } else {
                throw new Exception($"No TaskCompletionSource for request id {requestId} found.");
            }
        }
        
        private void notifyRequestResult<T>(string requestId, T result, Exception? e)
        {
            TaskCompletionSource<T?> completion = requestTaskMap[requestId] as TaskCompletionSource<T?>;
            try {
                if (e != null) {
                    if (!completion.TrySetException(e))
                        throw new Exception($"Unable to set exception for for request id {requestId}. Task has already been completed.");
                } else {
                    if(!completion.TrySetResult(result))
                        throw new Exception($"Unable to set result for for request id {requestId}. Task has already been completed.");
                }
            } catch (ObjectDisposedException exception) {
                throw new Exception($"Task for request id {requestId} has already been disposed and can't be updated.");
            }

            requestTaskMap.Remove(requestId);
        }

        #endregion
    }
}
