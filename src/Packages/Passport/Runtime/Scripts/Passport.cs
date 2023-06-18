using System;
using System.Threading.Tasks;
using UnityEngine;
using VoltstroStudios.UnityWebBrowser.Core;
using Immutable.Passport.Auth;
using Newtonsoft.Json;
using Immutable.Passport.Utility;
using System.IO;
using Immutable.Passport.Model;
using Immutable.Passport.Core;
using Cysharp.Threading.Tasks;

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
        private const int START_UP_TIMEOUT_MS = 30000; // 30 seconds

        public static Passport Instance { get; private set; }

        #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            private WebBrowserClient webBrowserClient = new();
        #endif

        private static bool? readySignalReceived = null;

        private AuthManager auth = new();
        private BrowserCommunicationsManager communicationsManager;

        private Passport() 
        {
        }

        public static UniTask<Passport> Init() 
        {
            if (Instance == null) 
            {
                // Create game object, so we dispose the browser on destroy
                GameObject go = new GameObject(GAME_OBJECT_NAME);
                // Add passport to the game object
                go.AddComponent<Passport>();
                // Save passport instance
                Instance = go.GetComponent<Passport>();
            }
            else
            {
                readySignalReceived = true;
            }

            // Wait until we get a ready signal, or timeout
            return UniTask.WaitUntil(() => readySignalReceived != null)
                .ContinueWith(() => {
                    if (readySignalReceived == true) 
                    {
                        return Instance;
                    }
                    else
                    {
                        throw new PassportException("Failed to initiliase Passport");
                    }
                });
        }

        private async void Start()
        {
            try 
            {
                await webBrowserClient.Init();
                webBrowserClient.OnLoadFinish += OnLoadFinish;

                communicationsManager = new BrowserCommunicationsManager(webBrowserClient);
            } 
            catch (Exception ex) 
            {
                Debug.Log($"{TAG} Failed to initialise browser: {ex.Message}");

                // Reset values
                readySignalReceived = false;
                Instance = null;
                Destroy(this.gameObject);
            }
        }

        private void Awake() 
        {
            // Keep this alive in every scene
            DontDestroyOnLoad(this.gameObject);
        }

        private void OnDestroy()
        {
            webBrowserClient.Dispose();
        }

        private void OnLoadFinish(string url) 
        {
            Debug.Log($"{TAG} On load finish: {url}");
            if (url.StartsWith(INITIAL_URL)) 
            {
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
                readySignalReceived = true;
                // Clean up listener
                webBrowserClient.OnLoadFinish -= OnLoadFinish;
            }
        }

        /// <summary>
        /// Connects the user to Passport.
        ///
        /// If code confirmation is required, call ConfirmCode().
        /// <returns>
        /// The end-user verification code if confirmation is required, otherwise null;
        /// </returns>
        /// </summary>
        public async Task<string?> Connect() 
        {
            string? code = await auth.Login();
            User? user = auth.GetUser();
            if (code != null) 
            {
                // Code confirmation required
                return code;
            } 
            else if (user != null) 
            {
                // Credentials are still valid, get provider
                await GetImxProvider(user);
                return null;
            } 
            else 
            {
                // Should never get to here, but if it happens, log the user to reset everything
                auth.Logout();
                throw new InvalidOperationException("Something went wrong, call Connect() again");
            }
        }

        public async Task ConfirmCode() 
        {
            User user = await auth.ConfirmCode();
            await GetImxProvider(user);
        }

        private async Task GetImxProvider(User u) 
        {
            // Only send necessary values
            GetImxProviderRequest request = new GetImxProviderRequest(u.idToken, u.accessToken, u.refreshToken, u.profile, u.etherKey);
            string data = JsonConvert.SerializeObject(request);

            bool success = await communicationsManager.Call<bool>(PassportFunction.GET_IMX_PROVIDER, data);
            if (!success) 
            {
                throw new PassportException("Failed to get IMX provider", PassportErrorType.WALLET_CONNECTION_ERROR);
            }
        }

        public Task<string?> GetAddress() 
        {
            return communicationsManager.Call<string?>(PassportFunction.GET_ADDRESS);
        }

        public void Logout()
         {
            auth.Logout();
        }

        public string? GetAccessToken() 
        {
            User? user = auth.GetUser();
            if (user != null) 
            {
                return user.accessToken;
            } 
            else 
            {
                return null;
            }
        }

        public string? GetIdToken() 
        {
            User? user = auth.GetUser();
            if (user != null) 
            {
                return user.idToken;
            } 
            else
            {
                return null;
            }
        }

        public Task<string?> SignMessage(string message) 
        {
            return communicationsManager.Call<string?>(PassportFunction.SIGN_MESSAGE, message);
        }
    }
}
