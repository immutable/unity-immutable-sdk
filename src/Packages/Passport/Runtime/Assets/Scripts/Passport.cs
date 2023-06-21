using System;
using UnityEngine;
using VoltstroStudios.UnityWebBrowser.Core;
using Immutable.Passport.Auth;
using Newtonsoft.Json;
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

        public static Passport? Instance { get; private set; }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        private readonly WebBrowserClient webBrowserClient = new();
#endif

        private static bool? readySignalReceived = null;

        // private readonly AuthManager auth = new();
        private PassportImpl? passportImpl = null;

        private Passport()
        {
        }

        public static UniTask<Passport> Init()
        {
            if (Instance == null)
            {
                // Create game object, so we dispose the browser on destroy
                GameObject go = new(GAME_OBJECT_NAME);
                // Add passport to the game object
                go.AddComponent<Passport>();
                // Save passport instance
                Instance = go.GetComponent<Passport>();
            }
            else
            {
                readySignalReceived = true;
            }

            // Wait until we get a ready signal
            return UniTask.WaitUntil(() => readySignalReceived != null)
                .ContinueWith(() =>
                {
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

#pragma warning disable IDE0051
        private async void Start()
        {
            try
            {
                communicationsManager = new BrowserCommunicationsManager(webBrowserClient);
                communicationsManager.OnReady += () => readySignalReceived = true;
                await webBrowserClient.Init();
                passportImpl = new PassportImpl(new AuthManager(), communicationsManager);
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
#pragma warning restore IDE0051

        /// <summary>
        ///     Sets the timeout time for waiting for each call to respond (in milliseconds).
        ///     This only applies to functions that uses the browser communications manager.
        /// </summary>
        public void setCallTimeout(int ms) 
        {
            GetBrowserCommunicationsManager().callTimeout = ms;
        }

        /// <summary>
        /// Connects the user to Passport.
        ///
        /// If code confirmation is required, call ConfirmCode().
        /// <returns>
        /// The end-user verification code if confirmation is required, otherwise null;
        /// </returns>
        /// </summary>
        public async UniTask<string?> Connect()
        {
            return await GetPassportImpl().Connect();
        }

        public async UniTask ConfirmCode() 
        {
            await GetPassportImpl().ConfirmCode();
        }

        public async UniTask<string?> GetAddress() 
        {
            return await GetPassportImpl().GetAddress();
        }

        public void Logout()
        {
            GetPassportImpl().Logout();
        }

        public string? GetAccessToken()
        {
            return GetPassportImpl().GetAccessToken();
        }

        public string? GetIdToken()
        {
            return GetPassportImpl().GetIdToken();
        }

        public async UniTask<string?> SignMessage(string message)
        {
            return await GetPassportImpl().SignMessage(message);
        }

        private PassportImpl GetPassportImpl()
        {
            if (passportImpl != null)
            {
                return passportImpl;
            }
            throw new PassportException("Passport not initialised");
        }
    }
}
