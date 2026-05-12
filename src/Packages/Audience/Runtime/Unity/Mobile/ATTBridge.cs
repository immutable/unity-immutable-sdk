#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
#if UNITY_IOS
using System.Runtime.InteropServices;
using AOT;
using UnityEngine.Scripting;
#endif

namespace Immutable.Audience.Unity.Mobile
{
    // Thin wrapper around the iOS AppTrackingTransparency / AdSupport native
    // calls in AudienceMobileBridge.mm. The async request bridges Apple's
    // completion-handler callback into a Task; the synchronous getters mirror
    // the IDFV / SKAN bridges.
    //
    // Status codes (matching ATTrackingManagerAuthorizationStatus):
    //   0 = notDetermined, 1 = restricted, 2 = denied, 3 = authorized.
    internal static class ATTBridge
    {
        // Test seams. Default to the native impls; tests substitute fakes so
        // the editor playmode doesn't try to load __Internal.
        internal static Func<Task<int>> RequestImpl = NativeRequestAsync;
        internal static Func<int> GetStatusImpl = NativeGetStatus;
        internal static Func<string?> GetIDFAImpl = NativeGetIDFA;

        internal static Task<int> RequestAsync() => RequestImpl();
        internal static int GetStatus() => GetStatusImpl();
        internal static string? GetIDFA() => GetIDFAImpl();

#if UNITY_IOS
        // Marshalled as a function pointer to native code. The signature must
        // exactly match the AudienceATTStatusCallback typedef in the .mm.
        private delegate void NativeStatusCallback(int status);

        [DllImport("__Internal")]
        private static extern void _AudienceRequestATT(NativeStatusCallback callback);

        [DllImport("__Internal")]
        private static extern int _AudienceGetATTStatus();

        [DllImport("__Internal")]
        private static extern string _AudienceGetIDFA();

        // Hold a single delegate instance for the lifetime of the process. If
        // we passed a fresh delegate to each P/Invoke call, IL2CPP could GC
        // the wrapper before Apple's completion handler fires (which can be
        // seconds or minutes later. The user may swipe the prompt away or
        // background the app).
        private static readonly NativeStatusCallback _staticCallback = OnATTStatus;

        // Single-flight: ATT prompts once per app lifetime. A second request
        // while one is in flight returns the same task.
        private static readonly object _requestLock = new object();
        private static TaskCompletionSource<int>? _pendingTcs;

        // Required:
        // - MonoPInvokeCallback so IL2CPP emits the trampoline that lets
        //   native code call back into managed code.
        // - Preserve so managed-code stripping at High doesn't drop this
        //   method (no managed callers; only the function pointer).
        // Without both, the await in RequestAsync never completes on a
        // shipping build with stripping enabled.
        [MonoPInvokeCallback(typeof(NativeStatusCallback))]
        [Preserve]
        private static void OnATTStatus(int status)
        {
            TaskCompletionSource<int>? tcs;
            lock (_requestLock)
            {
                tcs = _pendingTcs;
                _pendingTcs = null;
            }
            // Apple may fire the handler on a background thread.
            // RunContinuationsAsynchronously (set on construction) ensures
            // awaiting code resumes on the thread pool, not Apple's thread.
            tcs?.TrySetResult(status);
        }

        private static Task<int> NativeRequestAsync()
        {
            TaskCompletionSource<int> tcs;
            lock (_requestLock)
            {
                if (_pendingTcs != null) return _pendingTcs.Task;
                tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                _pendingTcs = tcs;
            }

            try
            {
                _AudienceRequestATT(_staticCallback);
            }
            catch
            {
                lock (_requestLock)
                {
                    if (_pendingTcs == tcs) _pendingTcs = null;
                }
                throw;
            }

            return tcs.Task;
        }

        private static int NativeGetStatus() => _AudienceGetATTStatus();

        private static string? NativeGetIDFA() => _AudienceGetIDFA();
#else
        private static Task<int> NativeRequestAsync() => Task.FromResult(0);
        private static int NativeGetStatus() => 0;
        private static string? NativeGetIDFA() => null;
#endif
    }
}
