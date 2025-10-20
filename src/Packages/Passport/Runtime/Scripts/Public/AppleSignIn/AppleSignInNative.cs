#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Immutable.Passport.Core.Logging;

namespace Immutable.Passport.AppleSignIn
{
    /// <summary>
    /// Native iOS Apple Sign In wrapper
    /// Slice 2: Stub implementation for testing P/Invoke bridge
    /// </summary>
    public class AppleSignInNative
    {
        private const string TAG = "[AppleSignInNative]";

        // P/Invoke declarations
        [DllImport("__Internal")]
        private static extern void AppleSignIn_Init();

        [DllImport("__Internal")]
        private static extern bool AppleSignIn_IsAvailable();

        [DllImport("__Internal")]
        private static extern void AppleSignIn_Start();

        // Callback delegates
        private delegate void OnSuccessDelegate(string identityToken, string authorizationCode, string userID, string email, string fullName);
        private delegate void OnErrorDelegate(string errorCode, string errorMessage);
        private delegate void OnCancelDelegate();

        [DllImport("__Internal")]
        private static extern void AppleSignIn_SetOnSuccessCallback(OnSuccessDelegate callback);

        [DllImport("__Internal")]
        private static extern void AppleSignIn_SetOnErrorCallback(OnErrorDelegate callback);

        [DllImport("__Internal")]
        private static extern void AppleSignIn_SetOnCancelCallback(OnCancelDelegate callback);

        // Keep delegates alive to prevent garbage collection
        private static OnSuccessDelegate _onSuccessDelegate;
        private static OnErrorDelegate _onErrorDelegate;
        private static OnCancelDelegate _onCancelDelegate;

        // Events that Unity code can subscribe to
        public static event Action<string, string, string, string, string> OnSuccess;
        public static event Action<string, string> OnError;
        public static event Action OnCancel;

        private static bool _isInitialized = false;

        /// <summary>
        /// Initialize the native plugin and set up callbacks
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized)
            {
                PassportLogger.Warn($"{TAG} Already initialized");
                return;
            }

            PassportLogger.Info($"{TAG} Initializing native Apple Sign In plugin...");

            try
            {
                // Initialize native plugin
                AppleSignIn_Init();

                // Set up callbacks (using static methods)
                _onSuccessDelegate = new OnSuccessDelegate(OnSuccessCallback);
                _onErrorDelegate = new OnErrorDelegate(OnErrorCallback);
                _onCancelDelegate = new OnCancelDelegate(OnCancelCallback);

                AppleSignIn_SetOnSuccessCallback(_onSuccessDelegate);
                AppleSignIn_SetOnErrorCallback(_onErrorDelegate);
                AppleSignIn_SetOnCancelCallback(_onCancelDelegate);

                _isInitialized = true;
                PassportLogger.Info($"{TAG} ✅ Native plugin initialized successfully");
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} ❌ Failed to initialize: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Check if Apple Sign In is available on this device
        /// </summary>
        public static bool IsAvailable()
        {
            if (!_isInitialized)
            {
                PassportLogger.Warn($"{TAG} Not initialized, calling Initialize()");
                Initialize();
            }

            try
            {
                bool available = AppleSignIn_IsAvailable();
                PassportLogger.Info($"{TAG} IsAvailable: {available}");
                return available;
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Error checking availability: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Start the Apple Sign In flow
        /// </summary>
        public static void Start()
        {
            if (!_isInitialized)
            {
                PassportLogger.Error($"{TAG} Not initialized! Call Initialize() first");
                throw new InvalidOperationException("AppleSignInNative not initialized");
            }

            PassportLogger.Info($"{TAG} Starting Apple Sign In flow...");

            try
            {
                AppleSignIn_Start();
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Error starting Apple Sign In: {ex.Message}");
                throw;
            }
        }

        // Callback implementations (called from native code)
        [AOT.MonoPInvokeCallback(typeof(OnSuccessDelegate))]
        private static void OnSuccessCallback(string identityToken, string authorizationCode, string userID, string email, string fullName)
        {
            PassportLogger.Info($"{TAG} 🎉 Native callback - Success!");
            PassportLogger.Info($"{TAG}   UserID: {userID}");
            PassportLogger.Info($"{TAG}   Email: {email}");
            PassportLogger.Info($"{TAG}   Full Name: {fullName}");
            PassportLogger.Info($"{TAG}   Identity Token: {identityToken.Substring(0, Math.Min(50, identityToken.Length))}...");

            // Invoke event on main thread
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                OnSuccess?.Invoke(identityToken, authorizationCode, userID, email, fullName);
            });
        }

        [AOT.MonoPInvokeCallback(typeof(OnErrorDelegate))]
        private static void OnErrorCallback(string errorCode, string errorMessage)
        {
            PassportLogger.Error($"{TAG} ❌ Native callback - Error: {errorCode} - {errorMessage}");

            // Invoke event on main thread
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                OnError?.Invoke(errorCode, errorMessage);
            });
        }

        [AOT.MonoPInvokeCallback(typeof(OnCancelDelegate))]
        private static void OnCancelCallback()
        {
            PassportLogger.Info($"{TAG} ⚠️ Native callback - Cancelled");

            // Invoke event on main thread
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                OnCancel?.Invoke();
            });
        }
    }

    /// <summary>
    /// Helper to dispatch callbacks to Unity main thread
    /// </summary>
    internal static class UnityMainThreadDispatcher
    {
        private static readonly System.Collections.Generic.Queue<Action> _executionQueue = new System.Collections.Generic.Queue<Action>();

        public static void Enqueue(Action action)
        {
            lock (_executionQueue)
            {
                _executionQueue.Enqueue(action);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // Create a GameObject to run Update loop
            var go = new GameObject("AppleSignInDispatcher");
            GameObject.DontDestroyOnLoad(go);
            go.AddComponent<MainThreadDispatcher>();
        }

        private class MainThreadDispatcher : MonoBehaviour
        {
            private void Update()
            {
                lock (_executionQueue)
                {
                    while (_executionQueue.Count > 0)
                    {
                        _executionQueue.Dequeue()?.Invoke();
                    }
                }
            }
        }
    }
}
#endif

