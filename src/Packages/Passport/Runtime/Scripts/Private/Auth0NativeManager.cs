using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace Immutable.Passport
{
    /// <summary>
    /// Manages Auth0 native social login on Android
    ///
    /// This singleton receives callbacks from Auth0NativeHelper.kt via UnitySendMessage
    /// and provides a clean async API for Unity code.
    ///
    /// Flow:
    /// 1. Unity calls LoginWithNative()
    /// 2. Invokes Auth0NativeHelper.loginWithGoogle() on Android
    /// 3. Android shows native Google picker → Auth0 SDK authentication
    /// 4. Android calls OnAuth0Success/OnAuth0Error via UnitySendMessage
    /// 5. Unity completes the UniTask with result
    /// </summary>
    public class Auth0NativeManager : MonoBehaviour
    {
        private const string TAG = "[Auth0NativeManager]";

        // Android class name
        private const string AUTH0_HELPER_CLASS = "com.immutable.unity.passport.Auth0NativeHelper";

        // Singleton instance
        private static Auth0NativeManager? _instance;
        public static Auth0NativeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Create GameObject with this component
                    var go = new GameObject("PassportManager"); // Name must match UNITY_GAME_OBJECT in Auth0NativeHelper.kt
                    _instance = go.AddComponent<Auth0NativeManager>();
                    DontDestroyOnLoad(go);

                    Debug.Log($"{TAG} Singleton instance created");
                }
                return _instance;
            }
        }

        // Current login task completion source
        private UniTaskCompletionSource<Auth0Credentials>? _loginCompletionSource;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Login with native Google authentication via Auth0
        ///
        /// Shows native Android Google account picker, authenticates with Auth0,
        /// and returns Auth0 credentials (access token, ID token, refresh token).
        ///
        /// Requires Android 9+ (API 28+) for native Google Credential Manager.
        /// </summary>
        /// <returns>Auth0 credentials on success</returns>
        /// <exception cref="PassportException">If login fails or is cancelled</exception>
        public async UniTask<Auth0Credentials> LoginWithNative()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Debug.Log($"{TAG} Starting native Auth0 login...");

            // Create completion source for this login attempt
            _loginCompletionSource = new UniTaskCompletionSource<Auth0Credentials>();

            try
            {
                // Get Auth0NativeHelper instance from Android
                using (AndroidJavaClass helperClass = new AndroidJavaClass(AUTH0_HELPER_CLASS))
                {
                    AndroidJavaObject helper = helperClass.CallStatic<AndroidJavaObject>("getInstance");

                    if (helper == null)
                    {
                        throw new PassportException("Failed to get Auth0NativeHelper instance");
                    }

                    Debug.Log($"{TAG} Calling Auth0NativeHelper.loginWithGoogle()...");

                    // Call loginWithGoogle() - this is async on Android side
                    // Result will come back via OnAuth0Success/OnAuth0Error callbacks
                    helper.Call("loginWithGoogle");

                    Debug.Log($"{TAG} Waiting for Auth0 authentication result...");
                }

                // Wait for callback from Android
                return await _loginCompletionSource.Task;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{TAG} Login failed: {ex.Message}");
                _loginCompletionSource?.TrySetException(new PassportException($"Auth0 native login failed: {ex.Message}"));
                throw;
            }
            finally
            {
                _loginCompletionSource = null;
            }
#else
            Debug.LogWarning($"{TAG} Auth0 native login is only available on Android devices");
            throw new PassportException("Auth0 native login is only available on Android");
#endif
        }

        /// <summary>
        /// Called by Auth0NativeHelper.kt via UnitySendMessage when authentication succeeds
        ///
        /// Message format (JSON):
        /// {
        ///   "access_token": "...",
        ///   "id_token": "...",
        ///   "refresh_token": "...",
        ///   "token_type": "Bearer",
        ///   "expires_at": 1234567890000,
        ///   "scope": "openid profile email"
        /// }
        /// </summary>
        /// <param name="json">JSON string containing Auth0 credentials</param>
        public void OnAuth0Success(string json)
        {
            Debug.Log($"{TAG} OnAuth0Success called");

            try
            {
                // Parse credentials from JSON
                var credentials = JsonUtility.FromJson<Auth0Credentials>(json);

                if (credentials == null)
                {
                    Debug.LogError($"{TAG} Failed to parse credentials JSON");
                    _loginCompletionSource?.TrySetException(new PassportException("Failed to parse Auth0 credentials"));
                    return;
                }

                Debug.Log($"{TAG} Successfully received Auth0 credentials");
                Debug.Log($"{TAG} - Access token length: {credentials.access_token?.Length ?? 0}");
                Debug.Log($"{TAG} - ID token length: {credentials.id_token?.Length ?? 0}");
                Debug.Log($"{TAG} - Token type: {credentials.token_type}");
                Debug.Log($"{TAG} - Expires at: {credentials.expires_at}");

                // Complete the login task
                _loginCompletionSource?.TrySetResult(credentials);
            }
            catch (Exception ex)
            {
                Debug.LogError($"{TAG} Error processing Auth0 success: {ex.Message}");
                _loginCompletionSource?.TrySetException(new PassportException($"Error processing Auth0 response: {ex.Message}"));
            }
        }

        /// <summary>
        /// Called by Auth0NativeHelper.kt via UnitySendMessage when authentication fails
        /// </summary>
        /// <param name="errorMessage">User-friendly error message</param>
        public void OnAuth0Error(string errorMessage)
        {
            Debug.LogError($"{TAG} OnAuth0Error: {errorMessage}");

            _loginCompletionSource?.TrySetException(new PassportException(errorMessage));
        }
    }

    /// <summary>
    /// Auth0 credentials returned from native authentication
    ///
    /// This matches the JSON structure sent from Auth0NativeHelper.kt
    /// </summary>
    [Serializable]
    public class Auth0Credentials
    {
        public string? access_token;
        public string? id_token;
        public string? refresh_token;
        public string? token_type;
        public long expires_at;
        public string? scope;
    }
}
