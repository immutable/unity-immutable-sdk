using System;
using System.Threading;
using VoltstroStudios.UnityWebBrowser.Core;
using Immutable.Passport.Auth;
using Newtonsoft.Json;
using System.IO;
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

        // private readonly AuthManager auth = new();
        private PassportImpl? passportImpl = null;

        /// <summary>
        /// <param name="productName">The name of the game. This is used to clean up resources associated with
        /// Passport when the game quits. Use Application.productName.</param>
        /// </summary> 
        private Passport()
        {
#if UNITY_EDITOR
            Application.quitting += OnQuit;
#endif
        }

        public static UniTask<Passport> Init()
        {
            if (Instance == null)
            {
                Instance = new Passport();
                Instance.Initialise();
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

        private async void Initialise()
        {
            try
            {
                BrowserCommunicationsManager communicationsManager = new(webBrowserClient);
                communicationsManager.OnReady += () => readySignalReceived = true;
                await webBrowserClient.Init();
                passportImpl = new PassportImpl(new AuthManager(), communicationsManager);
            }
            catch (Exception)
            {
                // Reset values
                readySignalReceived = false;
                Instance = null;
            }
        }

#if UNITY_EDITOR
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
        public async UniTask<ConnectResponse?> Connect(CancellationToken? token = null)
        {
            return await GetPassportImpl().Connect(token);
        }

        public async UniTask ConfirmCode(CancellationToken? token = null)
        {
            await GetPassportImpl().ConfirmCode(token);
        }

        public async UniTask<string?> GetAddress()
        {
            return await GetPassportImpl().GetAddress();
        }

        public void Logout()
        {
            GetPassportImpl().Logout();
        }

        /// <summary>
        /// Checks if credentials exist but does not check if they're valid
        /// </summary>
        public bool HasCredentialsSaved()
        {
            return GetPassportImpl().HasCredentialsSaved();
        }

        public string? GetEmail()
        {
            return GetPassportImpl().GetEmail();
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
