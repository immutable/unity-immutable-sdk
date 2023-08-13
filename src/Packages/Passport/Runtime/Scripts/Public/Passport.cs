using System;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using VoltstroStudios.UnityWebBrowser.Core;
#else
using Immutable.Browser.Gree;
#endif
using Immutable.Browser.Core;
using Immutable.Passport.Model;
using Immutable.Passport.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Immutable.Passport
{
    public class Passport
    {
        private const string TAG = "[Passport]";

        public static Passport? Instance { get; private set; }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        private readonly IWebBrowserClient webBrowserClient = new WebBrowserClient();
#else
        private readonly IWebBrowserClient webBrowserClient = new GreeBrowserClient();
#endif

        // Keeps track of the latest received deeplink
        private static string? deeplink = null;
        private static bool? readySignalReceived = null;
        private PassportImpl? passportImpl = null;

        private Passport()
        {
#if UNITY_EDITOR_WIN
            Application.quitting += OnQuit;
#elif UNITY_IPHONE || UNITY_ANDROID
            Application.deepLinkActivated += onDeepLinkActivated;
            if (!string.IsNullOrEmpty(Application.absoluteURL))
            {
                // Cold start and Application.absoluteURL not null so process Deep Link.
                onDeepLinkActivated(Application.absoluteURL);
            }
#endif
        }

        public static UniTask<Passport> Init(string clientId, string? redirectUri = null)
        {
            if (Instance == null)
            {
                Instance = new Passport();
                // Wait until we get a ready signal
                return Instance.Initialise()
                    .ContinueWith(async () =>
                    {
                        await UniTask.WaitUntil(() => readySignalReceived != null);
                    })
                    .ContinueWith(async () =>
                    {
                        if (readySignalReceived == true)
                        {
                            await Instance.GetPassportImpl().Init(clientId, redirectUri, deeplink);
                            return Instance;
                        }
                        else
                        {
                            throw new PassportException("Failed to initiliase Passport", PassportErrorType.INITALISATION_ERROR);
                        }
                    });
            }
            else
            {
                readySignalReceived = true;
                return UniTask.FromResult(Instance);
            }
        }

        private async UniTask Initialise()
        {
            try
            {
                BrowserCommunicationsManager communicationsManager = new(webBrowserClient);
                communicationsManager.OnReady += () => readySignalReceived = true;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                await ((WebBrowserClient)webBrowserClient).Init();
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
            ((WebBrowserClient)webBrowserClient).Dispose();
        }
#endif

        /// <summary>
        /// Sets the timeout time for` waiting for each call to respond (in milliseconds).
        /// This only applies to functions that use the browser communications manager.
        /// </summary>
        public void SetCallTimeout(int ms)
        {
            GetPassportImpl().communicationsManager.SetCallTimeout(ms);
        }

        /// <summary>
        /// Connects the user into Passport via device code auth and sets up the IMX provider. This will open the user's
        /// default browser and take them through Passport login.
        /// 
        /// Once authenticated, this function will set up the IMX provider.
        ///
        /// The user does not need to go through the device code auth flow if the saved access token is still valid or
        /// the refresh token can be used to get a new access token.
        /// </summary>
        public async UniTask Connect(long? timeoutMs = null)
        {
            await GetPassportImpl().Connect(timeoutMs);
        }

#if UNITY_ANDROID || UNITY_IPHONE
        /// <summary>
        /// Connects the user into Passport via PKCE auth and sets up the IMX provider.
        ///
        /// The user does not need to go through this flow if the saved access token is still valid or
        /// the refresh token can be used to get a new access token.
        /// </summary>
        public async UniTask ConnectPKCE()
        {
            await GetPassportImpl().ConnectPKCE();
        }
#endif

        /// <summary>
        /// Similar to Connect, however if the saved access token is no longer valid and the refresh token cannot be used,
        /// it will not fallback to device code.
        /// <returns>
        /// True if the user is connected to Passport
        /// </returns>
        /// </summary>
        public async UniTask<bool> ConnectSilent()
        {
            return await GetPassportImpl().ConnectSilent();
        }

        /// <summary>
        /// Gets the wallet address of the logged in user.
        /// <returns>
        /// The wallet address, otherwise null
        /// </returns>
        /// </summary>
        public async UniTask<string?> GetAddress()
        {
            return await GetPassportImpl().GetAddress();
        }

        /// <summary>
        /// Logs the user out of Passport and removes any stored credentials
        /// </summary>
        public async UniTask Logout()
        {
            await GetPassportImpl().Logout();
        }

        /// <summary>
        /// Checks if credentials exist but does not check if they're valid
        /// <returns>
        /// True if there are crendentials saved
        /// </returns>
        /// </summary>
        public UniTask<bool> HasCredentialsSaved()
        {
            return GetPassportImpl().HasCredentialsSaved();
        }

        /// <summary>
        /// Retrieves the email address of the user whose credentials are currently stored.
        /// <returns>
        /// The email address, otherwise null
        /// </returns>
        /// </summary>
        public async UniTask<string?> GetEmail()
        {
            string? email = await GetPassportImpl().GetEmail();
            return email;
        }

        /// <summary>
        /// Gets the currently saved access token without verifying its validity.
        /// <returns>
        /// The access token, otherwise null
        /// </returns>
        /// </summary>
        public UniTask<string?> GetAccessToken()
        {
            return GetPassportImpl().GetAccessToken();
        }

        /// <summary>
        /// Gets the currently saved ID token without verifying its validity.
        /// <returns>
        /// The ID token, otherwise null
        /// </returns>
        /// </summary>
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

        private async void onDeepLinkActivated(string url)
        {
            deeplink = url;

            if (passportImpl != null)
            {
                GetPassportImpl().OnDeepLinkActivated(url);
            }
        }
    }
}
