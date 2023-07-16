using System;
using System.Threading;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using VoltstroStudios.UnityWebBrowser.Core;
#endif
using Immutable.Passport.Auth;
using Immutable.Passport.Model;
using Immutable.Passport.Core;
using Cysharp.Threading.Tasks;
#if UNITY_EDITOR
using UnityEngine;
#endif

namespace Immutable.Passport
{
    public class Passport
    {
        private const string TAG = "[Passport]";

        public static Passport? Instance { get; private set; }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        private readonly WebBrowserClient webBrowserClient = new();
#endif

        private static bool? readySignalReceived = null;
        private PassportImpl? passportImpl = null;

        /// <summary>
        /// <param name="productName">The name of the game. This is used to clean up resources associated with
        /// Passport when the game quits. Use Application.productName.</param>
        /// </summary> 
        private Passport()
        {
#if UNITY_EDITOR_WIN
            Application.quitting += OnQuit;
#endif
        }

        public static UniTask<Passport> Init(string clientId)
        {
            if (Instance == null)
            {
                Instance = new Passport();
                Instance.Initialise();
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                // Wait until we get a ready signal
                return UniTask.WaitUntil(() => readySignalReceived != null)
                    .ContinueWith(async () =>
                    {
                        if (readySignalReceived == true)
                        {
                            await Instance.GetPassportImpl().Init(clientId);
                            return Instance;
                        }
                        else
                        {
                            throw new PassportException("Failed to initiliase Passport");
                        }
                    });
#else
                readySignalReceived = true;
                return UniTask.FromResult(Instance);
#endif
            }
            else
            {
                readySignalReceived = true;
                return UniTask.FromResult(Instance);
            }
        }

        private async void Initialise()
        {
            try
            {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                BrowserCommunicationsManager communicationsManager = new(webBrowserClient);
                communicationsManager.OnReady += () => readySignalReceived = true;
                await webBrowserClient.Init();
#else
                BrowserCommunicationsManager communicationsManager = new();
#endif
                passportImpl = new PassportImpl(communicationsManager);
            }
            catch (Exception)
            {
                // Reset values
                readySignalReceived = false;
                Instance = null;
            }
        }

#if UNITY_EDITOR_WIN
        private void OnQuit()
        {
            // Need to clean up UWB resources when quitting the game in the editor
            // as the child engine process would still be alive
            Debug.Log($"{TAG} Quitting the Player");
            webBrowserClient.Dispose();
        }
#endif

        /// <summary>
        ///     Sets the timeout time for` waiting for each call to respond (in milliseconds).
        ///     This only applies to functions that uses the browser communications manager.
        /// </summary>
        public void SetCallTimeout(int ms)
        {
            GetPassportImpl().communicationsManager.SetCallTimeout(ms);
        }

        /// <summary>
        /// Connects the user to Passport.
        ///
        /// If code confirmation is required, call ConfirmCode().
        /// <returns>
        /// The end-user verification code if confirmation is required, otherwise null;
        /// </returns>
        /// </summary>
        public async UniTask<ConnectResponse?> Connect()
        {
            return await GetPassportImpl().Connect();
        }

        /// <summary>
        /// Attempts to connect the user to Passport using the saved access or refresh token. It will not fallback
        /// to device code auth like Connect().
        /// </summary>
        public async UniTask ConnectSilent()
        {
            await GetPassportImpl().ConnectSilent();
        }

        public async UniTask ConfirmCode(long? timeoutMs = null)
        {
            await GetPassportImpl().ConfirmCode(timeoutMs);
        }

        public async UniTask<string?> GetAddress()
        {
            return await GetPassportImpl().GetAddress();
        }

        public async UniTask Logout()
        {
            await GetPassportImpl().Logout();
        }

        /// <summary>
        /// Checks if credentials exist but does not check if they're valid
        /// </summary>
        public UniTask<bool> HasCredentialsSaved()
        {
            return GetPassportImpl().HasCredentialsSaved();
        }

        public async UniTask<string?> GetEmail()
        {
            string? email = await GetPassportImpl().GetEmail();
            return email;
        }

        public UniTask<string?> GetAccessToken()
        {
            return GetPassportImpl().GetAccessToken();
        }

        public UniTask<string?> GetIdToken()
        {
            return GetPassportImpl().GetIdToken();
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
