using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VoltstroStudios.UnityWebBrowser.Core;
using Immutable.Passport.Auth;

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

        // TODO to remove and replace it with device code auth
        TaskCompletionSource<bool> loginTask;

        // TODO find a better way of notifying when the web app is ready
        public event PassportReady OnReady;

        private DeviceCodeAuth auth = new();

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

        // TODO replace with device code auth
        public async Task<string> Connect() {
            string code = await auth.Login();
            Debug.Log($"Got code: {code}");
            return code;
            // showBrowser();
            // return Task.Run(() => {
            //     var t = new TaskCompletionSource<bool>();
            //     call(PassportFunction.CONNECT);
            //     loginTask = t;
            //     return t.Task;
            // });
        }

        public async Task<TokenResponse> ConfirmCode() {
            TokenResponse tokenResponse = await auth.ConfirmCode();
            return tokenResponse;
        }

        public Task<string?> GetAddress() {
            return createCallTask<string?>(PassportFunction.GET_ADDRESS);
        }

        public void Logout() {
            hideBrowser();
            call(PassportFunction.LOGOUT);
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
            string requestJson = JsonUtility.ToJson(request).Replace("\"", "\\\"");

            // Call the function on the JS side
            string js = @$"callFunction(""{requestJson}"")";
            webBrowserClient.ExecuteJs(js);

            return requestId;
        }

        #endregion

        #region Passport response

        private void OnUnityPostMessage(string message) {
            Debug.Log($"[Unity] OnUnityPostMessage: {message}");
            switch (message) {
                // Login case for now
                // TODO to remove this switch case after we implement device auth code
                case "IMX_PROVIDER_SET":
                    hideBrowser();
                    loginTask?.TrySetResult(true);
                    loginTask = null;
                    break;
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
                switch (response.responseFor) {
                    case PassportFunction.GET_ADDRESS:
                        AddressResponse? addressResponse = JsonUtility.FromJson<AddressResponse>(message);
                        // Get the task for this request ID
                        TaskCompletionSource<string?> completion = requestTaskMap[response.requestId] as TaskCompletionSource<string?>;
                        // Return the address
                        completion.TrySetResult(addressResponse?.address);
                        // Remove task from the map as we don't need it anymore
                        requestTaskMap.Remove(addressResponse.requestId);
                        break;
                    default:
                        break;
                }
            }
        }

        #endregion

        #region Browser

        // TODO to remove once we implement device auth code
        private void showBrowser() {
            // Match browser resolution ratio with browser size so the content is not distorted
            RectTransform rectTransform = clientManager.GetComponent<RectTransform>();
            int w = (int) Math.Round(rectTransform.rect.width * 1.25);
            int h = (int) Math.Round(rectTransform.rect.height * 1.25);
            webBrowserClient.Resize(new VoltstroStudios.UnityWebBrowser.Shared.Resolution((uint) w, (uint) h));
            gameObject.transform.localScale = new Vector3(1, 1, 1);
        }

        private void hideBrowser() {
            gameObject.transform.localScale = new Vector3(0, 0, 0);
        }

        #endregion
    }
}
