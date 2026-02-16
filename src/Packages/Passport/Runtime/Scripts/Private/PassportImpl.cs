using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using Immutable.Passport.Event;
using Immutable.Passport.Model;
using Immutable.Passport.Core;
using Immutable.Passport.Helpers;
using Cysharp.Threading.Tasks;
using Immutable.Passport.Core.Logging;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace Immutable.Passport
{
#if UNITY_ANDROID
    public class PassportImpl : PKCECallback
#else
    public class PassportImpl
#endif
    {
        private const string TAG = "[Passport Implementation]";
        private readonly IBrowserCommunicationsManager _communicationsManager;
        private readonly PassportAnalytics _analytics = new();

        private DirectLoginOptions _directLoginOptions; // Store the direct login options for current operation
        private UniTaskCompletionSource<bool>? _pkceCompletionSource;
        private string _redirectUri;
        private string _logoutRedirectUri;

#if UNITY_ANDROID
        // Used for the PKCE callback
        internal static bool completingPKCE;
        internal static string loginPKCEUrl;
#endif

        // Used to prevent calling login/connect functions multiple times
        private bool _isLoggedIn;

        public event OnAuthEventDelegate? OnAuthEvent;

        public PassportImpl(IBrowserCommunicationsManager communicationsManager)
        {
            _communicationsManager = communicationsManager;
        }

        public async UniTask Init(string clientId, string environment, string redirectUri,
            string logoutRedirectUri, string? deeplink = null)
        {
            _redirectUri = redirectUri;
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
            if (redirectUri.Contains(GetHostedLoginPageUrl(environment))) {
                Uri uri = new Uri(redirectUri);
                string redirectUriParam = uri.GetQueryParameter("redirect_uri");
                _redirectUri = redirectUriParam;
            }
#endif
            _logoutRedirectUri = logoutRedirectUri;

#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX || UNITY_WEBGL
            _communicationsManager.OnAuthPostMessage += OnDeepLinkActivated;
            _communicationsManager.OnPostMessageError += OnPostMessageError;
#endif

            var versionInfo = new VersionInfo
            {
                engine = "unity",
                engineVersion = Application.unityVersion,
                engineSdkVersion = SdkVersionInfoHelpers.GetSdkVersionInfo(),
                platform = Application.platform.ToString(),
                platformVersion = SystemInfo.operatingSystem,
                deviceModel = SystemInfo.deviceModel
            };

            var initRequest = JsonUtility.ToJson(new InitRequestWithRedirectUri
            {
                clientId = clientId,
                environment = environment,
                redirectUri = redirectUri,
                logoutRedirectUri = logoutRedirectUri,
                engineVersion = versionInfo
            });

            var response = await _communicationsManager.Call(PassportFunction.INIT, initRequest);
            var initResponse = response.OptDeserializeObject<BrowserResponse>();

            if (initResponse?.success == false)
            {
                Track(PassportAnalytics.EventName.INIT_PASSPORT, success: false);
                throw new PassportException(initResponse.error ?? "Unable to initialise Passport");
            }

            if (deeplink != null)
            {
                OnDeepLinkActivated(deeplink);
            }

            Track(PassportAnalytics.EventName.INIT_PASSPORT, success: true);
        }

        public void SetCallTimeout(int ms)
        {
            _communicationsManager.SetCallTimeout(ms);
        }

        public UniTask<bool> Login(bool useCachedSession = false, DirectLoginOptions? directLoginOptions = null)
        {
            if (useCachedSession)
            {
                return Relogin();
            }

            try
            {
                Track(PassportAnalytics.EventName.START_LOGIN_PKCE);
                SendAuthEvent(PassportAuthEvent.LoggingInPKCE);

                var task = new UniTaskCompletionSource<bool>();
                _pkceCompletionSource = task;
                _directLoginOptions = directLoginOptions;
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
                WindowsDeepLink.Initialise(_redirectUri, OnDeepLinkActivated);
#endif
                _ = LaunchAuthUrl();
                return task.Task;
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to log in using PKCE flow: {ex.Message}";
                PassportLogger.Error($"{TAG} {errorMessage}");

                Track(PassportAnalytics.EventName.COMPLETE_LOGIN_PKCE, success: false);
                SendAuthEvent(PassportAuthEvent.LoginPKCEFailed);
                throw new PassportException(errorMessage, PassportErrorType.AUTHENTICATION_ERROR);
            }
        }

        /// <summary>
        /// Attempts to re-login using saved credentials.
        /// </summary>
        /// <returns>
        /// Returns true if re-login is successful, otherwise false.
        /// </returns>
        private async UniTask<bool> Relogin()
        {
            try
            {
                SendAuthEvent(PassportAuthEvent.ReloggingIn);

                string callResponse = await _communicationsManager.Call(PassportFunction.RELOGIN);
                Track(PassportAnalytics.EventName.COMPLETE_RELOGIN, success: true);
                SendAuthEvent(PassportAuthEvent.ReloginSuccess);
                _isLoggedIn = true;

                return true;
            }
            catch (Exception ex)
            {
                // Log a warning if re-login fails.
                PassportLogger.Warn($"{TAG} Failed to login using saved credentials. " +
                    $"Please check if user has saved credentials first by calling HasCredentialsSaved() : {ex.Message}");
                _isLoggedIn = false;
            }

            Track(PassportAnalytics.EventName.COMPLETE_RELOGIN, success: false);
            SendAuthEvent(PassportAuthEvent.ReloginFailed);
            return false;
        }

        public async void OnDeepLinkActivated(string url)
        {
            try
            {
                PassportLogger.Info($"{TAG} Received deeplink URL: {url}");

                Uri uri = new Uri(url);
                string hostWithPort = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";

                string domain = $"{uri.Scheme}://{hostWithPort}{uri.AbsolutePath}";

                if (domain.EndsWith("/"))
                {
                    domain = domain.Remove(domain.Length - 1);
                }

                if (domain.Equals(_logoutRedirectUri))
                {
                    HandleLogoutPkceSuccess();
                }
                else if (domain.Equals(_redirectUri))
                {
                    await CompleteLoginPKCEFlow(url);
                }
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Deeplink error {url}: {ex.Message}");
            }
        }

        private async UniTask LaunchAuthUrl()
        {
            try
            {
                // Create the request using a serializable class for clean JSON generation
                var request = new AuthUrlRequest
                {
                    directLoginOptions = _directLoginOptions != null ? new DirectLoginRequestOptions
                    {
                        directLoginMethod = _directLoginOptions.directLoginMethod.ToString().ToLower(),
                        email = _directLoginOptions.IsEmailValid() ? _directLoginOptions.email : null,
                        marketingConsentStatus = _directLoginOptions.marketingConsentStatus?.ToApiString()
                    } : null
                };

                var requestJson = JsonUtility.ToJson(request);

                var callResponse = await _communicationsManager.Call(PassportFunction.GET_PKCE_AUTH_URL, requestJson);
                var response = callResponse.OptDeserializeObject<StringResponse>();

                if (response != null && response.success == true && response.result != null)
                {
                    var url = response.result.Replace(" ", "+");

#if UNITY_ANDROID && !UNITY_EDITOR
                    loginPKCEUrl = url;
                    SendAuthEvent(PassportAuthEvent.LoginPKCELaunchingCustomTabs);
                    LaunchAndroidUrl(url);
#else
                    SendAuthEvent(PassportAuthEvent.LoginPKCEOpeningWebView);
                    _communicationsManager.LaunchAuthURL(url, _redirectUri);
#endif
                    return;
                }
                else
                {
                    PassportLogger.Error($"{TAG} Failed to get the Auth URL");
                }
            }
            catch (Exception e)
            {
                PassportLogger.Error($"{TAG} Get the Auth URL error: {e.Message}");
            }

            await UniTask.SwitchToMainThread();
            TrySetPkceException(new PassportException(
                "Something went wrong, please call Login() again",
                PassportErrorType.AUTHENTICATION_ERROR
            ));
        }

        public async UniTask CompleteLoginPKCEFlow(string uriString)
        {
#if UNITY_ANDROID
            completingPKCE = true;
#endif
            try
            {
                SendAuthEvent(PassportAuthEvent.CompletingLoginPKCE);
                var uri = new Uri(uriString);
                var state = uri.GetQueryParameter("state");
                var authCode = uri.GetQueryParameter("code");

                if (string.IsNullOrEmpty(state) || string.IsNullOrEmpty(authCode))
                {
                    Track(PassportAnalytics.EventName.COMPLETE_LOGIN_PKCE, success: false);
                    SendAuthEvent(PassportAuthEvent.LoginPKCEFailed);
                    await UniTask.SwitchToMainThread();
                    TrySetPkceException(new PassportException(
                        "Uri was missing state and/or code. Please call Login() again",
                        PassportErrorType.AUTHENTICATION_ERROR
                    ));
                }
                else
                {
                    var request = new ConnectPKCERequest()
                    {
                        authorizationCode = authCode,
                        state = state
                    };

                    var callResponse = await _communicationsManager.Call(PassportFunction.LOGIN_PKCE, JsonUtility.ToJson(request));

                    var response = callResponse.OptDeserializeObject<BrowserResponse>();
                    await UniTask.SwitchToMainThread();

                    if (response != null && response.success != true)
                    {
                        Track(PassportAnalytics.EventName.COMPLETE_LOGIN_PKCE, success: false);
                        SendAuthEvent(PassportAuthEvent.LoginPKCEFailed);
                        TrySetPkceException(new PassportException(
                            response.error ?? "Something went wrong, please call Login() again",
                            PassportErrorType.AUTHENTICATION_ERROR
                        ));
                    }
                    else
                    {
                        if (!_isLoggedIn)
                        {
                            TrySetPkceResult(true);
                        }

                        Track(PassportAnalytics.EventName.COMPLETE_LOGIN_PKCE, success: true);
                        SendAuthEvent(PassportAuthEvent.LoginPKCESuccess);
                        _isLoggedIn = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Track(PassportAnalytics.EventName.COMPLETE_LOGIN_PKCE, success: false);
                SendAuthEvent(PassportAuthEvent.LoginPKCEFailed);
                // Ensure any failure results in completing the flow regardless.
                TrySetPkceException(ex);
            }

            _pkceCompletionSource = null;
#if UNITY_ANDROID
            completingPKCE = false;
#endif
        }

#if UNITY_ANDROID
        public void OnLoginPKCEDismissed(bool completing)
        {
            if (!completing && !_isLoggedIn)
            {
                // User hasn't entered all required details (e.g. email address) into Passport yet
                PassportLogger.Info($"{TAG} Login PKCE dismissed before completing the flow");
                TrySetPkceCanceled();
            }
            else
            {
                PassportLogger.Info($"{TAG} Login PKCE dismissed by user or SDK");
            }
            loginPKCEUrl = null;
        }

        public void OnDeeplinkResult(string url)
        {
            OnDeepLinkActivated(url);
        }
#endif

        private async UniTask<string> GetLogoutUrl()
        {
            var response = await _communicationsManager.Call(PassportFunction.LOGOUT);
            var logoutUrl = response.GetStringResult();
            if (string.IsNullOrEmpty(logoutUrl))
            {
                throw new PassportException("Failed to get logout URL", PassportErrorType.AUTHENTICATION_ERROR);
            }

            return logoutUrl;
        }

        public async UniTask<bool> Logout(bool hardLogout = true)
        {
            try
            {
                SendAuthEvent(PassportAuthEvent.LoggingOutPKCE);

                var task = new UniTaskCompletionSource<bool>();
                _pkceCompletionSource = task;
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
                WindowsDeepLink.Initialise(_logoutRedirectUri, OnDeepLinkActivated);
#endif
                LaunchLogoutPkceUrl(hardLogout);
                return await task.Task;
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to log out: {ex.Message}";
                PassportLogger.Error($"{TAG} {errorMessage}");

                Track(PassportAnalytics.EventName.COMPLETE_LOGOUT_PKCE, success: false);
                SendAuthEvent(PassportAuthEvent.LogoutPKCEFailed);
                throw new PassportException(errorMessage, PassportErrorType.AUTHENTICATION_ERROR);
            }
        }

        private async void HandleLogoutPkceSuccess()
        {
            await UniTask.SwitchToMainThread();
            if (_isLoggedIn)
            {
                TrySetPkceResult(true);
            }
            Track(PassportAnalytics.EventName.COMPLETE_LOGOUT_PKCE, success: true);
            SendAuthEvent(PassportAuthEvent.LogoutPKCESuccess);
            _isLoggedIn = false;
            _pkceCompletionSource = null;
        }

        private async void LaunchLogoutPkceUrl(bool hardLogout)
        {
            var logoutUrl = await GetLogoutUrl();
            if (hardLogout)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                LaunchAndroidUrl(logoutUrl);
#else
                _communicationsManager.LaunchAuthURL(logoutUrl, _logoutRedirectUri);
#endif
            }
            else
            {
                HandleLogoutPkceSuccess();
            }
        }

        public async UniTask<bool> HasCredentialsSaved()
        {
            try
            {
                SendAuthEvent(PassportAuthEvent.CheckingForSavedCredentials);
                var accessToken = await GetAccessToken();
                var idToken = await GetIdToken();
                SendAuthEvent(PassportAuthEvent.CheckForSavedCredentialsSuccess);
                return accessToken != null && idToken != null;
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to check if there are credentials saved: {ex.Message}";
                PassportLogger.Debug($"{TAG} {errorMessage}");
                SendAuthEvent(PassportAuthEvent.CheckForSavedCredentialsFailed);
                return false;
            }
        }

        public async UniTask<bool> CompleteLogin(TokenResponse request)
        {
            var json = JsonUtility.ToJson(request);
            var callResponse = await _communicationsManager.Call(PassportFunction.STORE_TOKENS, json);
            return callResponse.GetBoolResponse() ?? false;
        }

        public async UniTask<string?> GetEmail()
        {
            var response = await _communicationsManager.Call(PassportFunction.GET_EMAIL);
            return response.GetStringResult();
        }

        public async UniTask<string?> GetPassportId()
        {
            var response = await _communicationsManager.Call(PassportFunction.GET_PASSPORT_ID);
            return response.GetStringResult();
        }

        public async UniTask<string?> GetAccessToken()
        {
            var response = await _communicationsManager.Call(PassportFunction.GET_ACCESS_TOKEN);
            return response.GetStringResult();
        }


        public async UniTask<string?> GetIdToken()
        {
            var response = await _communicationsManager.Call(PassportFunction.GET_ID_TOKEN);
            return response.GetStringResult();
        }

        public async UniTask<List<string>> GetLinkedAddresses()
        {
            var response = await _communicationsManager.Call(PassportFunction.GET_LINKED_ADDRESSES);
            var addresses = response.GetStringListResult();
            return addresses != null ? addresses.ToList() : new List<string>();
        }

        // ZkEvm
        public async UniTask ConnectEvm()
        {
            await _communicationsManager.Call(PassportFunction.ZK_EVM.CONNECT_EVM);
        }

        private string SerialiseTransactionRequest(TransactionRequest request)
        {
            string json;
            // Nulls are serialised as empty strings when using JsonUtility
            // so we need to use another model that doesn't have the 'data' field instead
            if (string.IsNullOrEmpty(request.data))
            {
                json = JsonUtility.ToJson(new TransactionRequestNoData()
                {
                    to = request.to,
                    value = request.value
                });
            }
            else
            {
                json = JsonUtility.ToJson(request);
            }
            return json;
        }

        public async UniTask<string?> ZkEvmSendTransaction(TransactionRequest request)
        {
            var json = SerialiseTransactionRequest(request);
            var callResponse = await _communicationsManager.Call(PassportFunction.ZK_EVM.SEND_TRANSACTION, json);
            return callResponse.GetStringResult();
        }

        public async UniTask<TransactionReceiptResponse?> ZkEvmSendTransactionWithConfirmation(TransactionRequest request)
        {
            var json = SerialiseTransactionRequest(request);
            var callResponse = await _communicationsManager.Call(PassportFunction.ZK_EVM.SEND_TRANSACTION_WITH_CONFIRMATION, json);
            return callResponse.OptDeserializeObject<TransactionReceiptResponse>();
        }

        public async UniTask<TransactionReceiptResponse?> ZkEvmGetTransactionReceipt(string hash)
        {
            var json = JsonUtility.ToJson(new TransactionReceiptRequest()
            {
                txHash = hash
            });
            var jsonResponse = await _communicationsManager.Call(PassportFunction.ZK_EVM.GET_TRANSACTION_RECEIPT, json);
            return jsonResponse.OptDeserializeObject<TransactionReceiptResponse>();
        }

        public async UniTask<string?> ZkEvmSignTypedDataV4(string payload)
        {
            var callResponse = await _communicationsManager.Call(PassportFunction.ZK_EVM.SIGN_TYPED_DATA_V4, payload);
            return callResponse.GetStringResult();
        }

        public async UniTask<List<string>> ZkEvmRequestAccounts()
        {
            var callResponse = await _communicationsManager.Call(PassportFunction.ZK_EVM.REQUEST_ACCOUNTS);
            var accountsResponse = callResponse.OptDeserializeObject<RequestAccountsResponse>();
            return accountsResponse != null ? accountsResponse.accounts.ToList() : new List<string>();
        }

        public async UniTask<string> ZkEvmGetBalance(string address, string blockNumberOrTag)
        {
            var json = JsonUtility.ToJson(new GetBalanceRequest()
            {
                address = address,
                blockNumberOrTag = blockNumberOrTag
            });
            var callResponse = await _communicationsManager.Call(PassportFunction.ZK_EVM.GET_BALANCE, json);
            return callResponse.GetStringResult() ?? "0x0";
        }

        private async void OnPostMessageError(string id, string message)
        {
            if (id == "CallFromAuthCallbackError" && _pkceCompletionSource != null)
            {
                await CallFromAuthCallbackError(id, message);
            }
            else
            {
                PassportLogger.Error($"{TAG} id: {id} err: {message}");
            }
        }

        private async UniTask CallFromAuthCallbackError(string id, string message)
        {
            await UniTask.SwitchToMainThread();

            if (message == "")
            {
                PassportLogger.Warn($"{TAG} Get PKCE Auth URL user cancelled");
                TrySetPkceCanceled();
            }
            else
            {
                PassportLogger.Error($"{TAG} Get PKCE Auth URL error: {message}");
                TrySetPkceException(new PassportException(
                    "Something went wrong, please call LoginPKCE() again",
                    PassportErrorType.AUTHENTICATION_ERROR
                ));
            }

            _pkceCompletionSource = null;
        }

        private void TrySetPkceResult(bool result)
        {
            PassportLogger.Debug($"{TAG} Trying to set PKCE result to {result}...");
            if (_pkceCompletionSource != null)
            {
                _pkceCompletionSource.TrySetResult(result);
            }
            else
            {
                PassportLogger.Error($"{TAG} PKCE completed with {result} but unable to bind result");
            }
        }

        private void TrySetPkceException(Exception exception)
        {
            PassportLogger.Debug($"{TAG} Trying to set PKCE exception...");
            if (_pkceCompletionSource != null)
            {
                _pkceCompletionSource.TrySetException(exception);
            }
            else
            {
                PassportLogger.Error($"{TAG} {exception.Message}");
            }
        }

        private void TrySetPkceCanceled()
        {
            PassportLogger.Debug($"{TAG} Trying to set PKCE canceled...");
            if (_pkceCompletionSource != null)
            {
                _pkceCompletionSource.TrySetCanceled();
            }
            else
            {
                PassportLogger.Warn($"{TAG} PKCE canceled");
            }
        }

        private void SendAuthEvent(PassportAuthEvent authEvent)
        {
            PassportLogger.Debug($"{TAG} Send auth event: {authEvent}");
            OnAuthEvent?.Invoke(authEvent);
        }

        protected virtual void OpenUrl(string url)
        {
            Application.OpenURL(url);
        }

#if UNITY_ANDROID
        private void LaunchAndroidUrl(string url)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass customTabLauncher = new AndroidJavaClass("com.immutable.unity.ImmutableActivity");
            customTabLauncher.CallStatic("startActivity", activity, url, new AndroidPKCECallback(this));
        }
#endif

#if (UNITY_IPHONE && !UNITY_EDITOR) || (UNITY_ANDROID && !UNITY_EDITOR)
        public void ClearCache(bool includeDiskFiles)
        {
            _communicationsManager.ClearCache(includeDiskFiles);
        }

        public void ClearStorage()
        {
            _communicationsManager.ClearStorage();
        }
#endif

        protected virtual async void Track(string eventName, bool? success = null, Dictionary<string, object>? properties = null)
        {
            await _analytics.Track(_communicationsManager, eventName, success, properties);
        }

#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
        /// <summary>
        /// Gets the hosted login page URL for the specified environment.
        /// This is used for Windows login redirect flow.
        /// </summary>
        public static string GetHostedLoginPageUrl(string environment)
        {
            return environment == Immutable.Passport.Model.Environment.DEVELOPMENT
                ? "https://auth.dev.immutable.com/im-logged-in"
                : "https://auth.immutable.com/im-logged-in";
        }
#endif
    }

#if UNITY_ANDROID
    public interface PKCECallback
    {

        /// <summary>
        /// Called when the Android Chrome Custom Tabs is hidden.
        /// Note that you won't be able to tell whether it was closed by the user or the SDK.
        /// <param name="completing">True if the user has entered everything required (e.g. email address),
        /// Chrome Custom Tabs have closed, and the SDK is trying to complete the PKCE flow.
        /// See <see cref="PassportImpl.CompleteLoginPKCEFlow"></param>
        /// </summary>
        void OnLoginPKCEDismissed(bool completing);

        void OnDeeplinkResult(string url);
    }

    class AndroidPKCECallback : AndroidJavaProxy
    {
        private PKCECallback callback;

        public AndroidPKCECallback(PKCECallback callback) : base("com.immutable.unity.ImmutableActivity$Callback")
        {
            this.callback = callback;
        }

        async void onCustomTabsDismissed(string url)
        {
            await UniTask.SwitchToMainThread();

            // To differentiate what triggered this
            if (url == PassportImpl.loginPKCEUrl)
            {
                // Custom tabs dismissed for login flow
                callback.OnLoginPKCEDismissed(PassportImpl.completingPKCE);
            }
        }

        async void onDeeplinkResult(string url)
        {
            await UniTask.SwitchToMainThread();
            callback.OnDeeplinkResult(url);
        }
    }
#endif

    /// <summary>
    /// Serializable request class for LaunchAuthUrl to replace manual JSON string concatenation
    /// </summary>
    [Serializable]
    internal class AuthUrlRequest
    {
        public DirectLoginRequestOptions directLoginOptions;
    }

    /// <summary>
    /// Serializable class for directLoginOptions within AuthUrlRequest
    /// </summary>
    [Serializable]
    internal class DirectLoginRequestOptions
    {
        public string directLoginMethod;
        public string email;
        public string marketingConsentStatus;
    }
}