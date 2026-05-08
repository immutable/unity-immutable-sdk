#nullable enable

using System;
using System.IO;
using System.Threading;
using Immutable.Audience;
#if UNITY_ANDROID && AUDIENCE_MOBILE_ATTRIBUTION
using UnityEngine;
#endif

namespace Immutable.Audience.Unity.Mobile
{
    /// <summary>
    /// Bridge to Google Play's Install Referrer Library. The referrer
    /// describes where the install came from (Play Store campaign, organic,
    /// deep link, etc.) and is the highest-value attribution signal on
    /// Android.
    ///
    /// The Install Referrer service returns the same value for the lifetime
    /// of the install (until uninstall), so we fetch once and cache to disk
    /// (file missing = not yet fetched; file present = terminal state, with
    /// empty content meaning known-no-referrer). First app launch likely
    /// won't include the referrer in <c>game_launch</c> (the async fetch
    /// usually completes after the launch event has fired); subsequent
    /// launches read from cache.
    /// </summary>
    internal static class InstallReferrerBridge
    {
        // Test seams. Tests inject without touching disk or the Android API.
        internal static Func<string, string?> ReadCachedImpl = ReadCachedFromDisk;
        internal static Action<string> StartFetchImpl = StartFetchNative;

        // Per-process gate so a second EnsureFetchStarted call during a
        // single session doesn't double up the Android Java connection.
        private static int _fetchStarted;

        /// <summary>
        /// Returns the cached install referrer, or null if not yet fetched
        /// or the device reported no referrer.
        /// </summary>
        internal static string? GetCachedInstallReferrer(string persistentDataPath)
        {
            if (string.IsNullOrEmpty(persistentDataPath)) return null;
            return ReadCachedImpl(persistentDataPath);
        }

        /// <summary>
        /// Starts the async fetch from Google Play if no terminal cache entry
        /// exists yet. Idempotent within a process; idempotent across launches
        /// once a terminal state is cached.
        /// </summary>
        internal static void EnsureFetchStarted(string persistentDataPath)
        {
            if (string.IsNullOrEmpty(persistentDataPath)) return;
            if (Interlocked.CompareExchange(ref _fetchStarted, 1, 0) != 0) return;

            // If we already have a terminal cache entry, skip the fetch.
            // The cache survives across launches; once written it never
            // changes (Google's referrer is stable for the install).
            if (File.Exists(AudiencePaths.InstallReferrerFile(persistentDataPath)))
                return;

            try
            {
                StartFetchImpl(persistentDataPath);
            }
            catch (Exception)
            {
                // Re-arm the gate so a later EnsureFetchStarted retries.
                Interlocked.Exchange(ref _fetchStarted, 0);
                throw;
            }
        }

        // Test-only reset; production code never calls this. Lets fixtures
        // start each test from a clean state (cache file + in-process gate).
        internal static void ResetForTesting()
        {
            Interlocked.Exchange(ref _fetchStarted, 0);
        }

        private static string? ReadCachedFromDisk(string persistentDataPath)
        {
            try
            {
                var path = AudiencePaths.InstallReferrerFile(persistentDataPath);
                if (!File.Exists(path)) return null;
                var content = File.ReadAllText(path);
                return string.IsNullOrEmpty(content) ? null : content;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Writes a terminal cache entry. Empty string marks "fetched, no
        // referrer" so subsequent launches don't re-call the service for a
        // permanent no-data state (organic install, FEATURE_NOT_SUPPORTED,
        // etc.). Transient errors must NOT call this.
        internal static void WriteCacheEntry(string persistentDataPath, string referrerOrEmpty)
        {
            try
            {
                var path = AudiencePaths.InstallReferrerFile(persistentDataPath);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var tmp = path + ".tmp";
                File.WriteAllText(tmp, referrerOrEmpty);
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
            }
            catch (Exception)
            {
                // Cache miss costs one extra service call next launch.
            }
        }

#if UNITY_ANDROID && AUDIENCE_MOBILE_ATTRIBUTION
        // Response codes from com.android.installreferrer.api.InstallReferrerClient.InstallReferrerResponse.
        private const int ResponseOk = 0;
        private const int ResponseServiceUnavailable = 1;
        private const int ResponseFeatureNotSupported = 2;
        private const int ResponseDeveloperError = 3;
        private const int ResponseServiceDisconnected = 4;
        private const int ResponsePermissionError = 5;

        private static void StartFetchNative(string persistentDataPath)
        {
            // currentActivity is the standard Unity → Android entry point.
            // The Install Referrer client binds to Google Play and calls
            // back on a worker thread; we never touch Unity APIs from there.
            //
            // Each AndroidJavaClass / AndroidJavaObject holds a JNI global
            // reference; leaking them stranded a JNI handle every Init.
            // `client` is the exception — ownership transfers to the
            // listener which disposes it in its endConnection finally.
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var clientClass = new AndroidJavaClass("com.android.installreferrer.api.InstallReferrerClient"))
            using (var builder = clientClass.CallStatic<AndroidJavaObject>("newBuilder", activity))
            {
                var client = builder.Call<AndroidJavaObject>("build");
                var listener = new ReferrerStateListener(client, persistentDataPath);
                client.Call("startConnection", listener);
            }
        }

        private class ReferrerStateListener : AndroidJavaProxy
        {
            private readonly AndroidJavaObject _client;
            private readonly string _persistentDataPath;

            public ReferrerStateListener(AndroidJavaObject client, string persistentDataPath)
                : base("com.android.installreferrer.api.InstallReferrerStateListener")
            {
                _client = client;
                _persistentDataPath = persistentDataPath;
            }

            // Java method name; AndroidJavaProxy dispatches by name.
            public void onInstallReferrerSetupFinished(int responseCode)
            {
                try
                {
                    switch (responseCode)
                    {
                        case ResponseOk:
                            using (var details = _client.Call<AndroidJavaObject>("getInstallReferrer"))
                            {
                                var referrer = details.Call<string>("getInstallReferrer");
                                WriteCacheEntry(_persistentDataPath, referrer ?? string.Empty);
                            }
                            break;

                        case ResponseFeatureNotSupported:
                        case ResponseDeveloperError:
                        case ResponsePermissionError:
                            // Permanent: never retry on this device/app.
                            WriteCacheEntry(_persistentDataPath, string.Empty);
                            break;

                        case ResponseServiceUnavailable:
                        case ResponseServiceDisconnected:
                        default:
                            // Transient: leave cache missing so next launch retries.
                            break;
                    }
                }
                finally
                {
                    try { _client.Call("endConnection"); } catch { /* swallow */ }
                    _client.Dispose();
                }
            }

            // The service can drop after a successful setup. We don't depend
            // on the live connection (we already wrote the cache), so this
            // is just a no-op.
            public void onInstallReferrerServiceDisconnected() { }
        }
#else
        private static void StartFetchNative(string persistentDataPath)
        {
            // Editor / non-Android / build-time gate not set: no-op.
        }
#endif
    }
}
