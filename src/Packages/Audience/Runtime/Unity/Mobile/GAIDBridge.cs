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
    /// Reads gaid + limitAdTracking via AdvertisingIdClient. Google requires
    /// the call run off the main thread, so we dispatch on a dedicated worker
    /// and cache the result to disk for the next launch. GAID can change
    /// (user reset), so we refresh every launch. First launch ships nothing;
    /// launch #2+ ships the previously-cached value.
    /// </summary>
    internal static class GAIDBridge
    {
        // Test seams.
        internal static Func<string, GAIDInfo?> ReadCachedImpl = ReadCachedFromDisk;
        internal static Action<string> StartFetchImpl = StartFetchNative;

        // Per-process gate: one fetch per session.
        private static int _fetchStarted;

        internal static GAIDInfo? GetCached(string persistentDataPath)
        {
            if (string.IsNullOrEmpty(persistentDataPath)) return null;
            return ReadCachedImpl(persistentDataPath);
        }

        internal static void EnsureFetchStarted(string persistentDataPath)
        {
            if (string.IsNullOrEmpty(persistentDataPath)) return;
            if (Interlocked.CompareExchange(ref _fetchStarted, 1, 0) != 0) return;

            try
            {
                StartFetchImpl(persistentDataPath);
            }
            catch (Exception)
            {
                // Re-arm so a later EnsureFetchStarted retries.
                Interlocked.Exchange(ref _fetchStarted, 0);
                throw;
            }
        }

        // Test-only.
        internal static void ResetForTesting()
        {
            Interlocked.Exchange(ref _fetchStarted, 0);
        }

        // Cache: line 1 = gaid (empty on opt-out), line 2 = "1"|"0" for limit flag.
        // File missing = no fetch yet.
        internal static void WriteCacheEntry(string persistentDataPath, string gaidOrEmpty, bool limitAdTracking)
        {
            try
            {
                var path = AudiencePaths.GAIDFile(persistentDataPath);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var content = (gaidOrEmpty ?? string.Empty) + "\n" + (limitAdTracking ? "1" : "0");
                var tmp = path + ".tmp";
                File.WriteAllText(tmp, content);
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
            }
            catch (Exception)
            {
                // Cache miss costs one wasted fetch on next launch.
            }
        }

        private static GAIDInfo? ReadCachedFromDisk(string persistentDataPath)
        {
            try
            {
                var path = AudiencePaths.GAIDFile(persistentDataPath);
                if (!File.Exists(path)) return null;

                var lines = File.ReadAllText(path).Split('\n');
                var gaid = lines.Length > 0 ? lines[0] : string.Empty;
                var limit = lines.Length > 1 && lines[1] == "1";

                return new GAIDInfo(gaid, limit);
            }
            catch (Exception)
            {
                return null;
            }
        }

#if UNITY_ANDROID && AUDIENCE_MOBILE_ATTRIBUTION
        private static void StartFetchNative(string persistentDataPath)
        {
            // Dedicated Thread (not ThreadPool) so Attach/Detach pair on the
            // same one-shot worker. ThreadPool reuse strands JVM state.
            var thread = new Thread(() => FetchOnWorkerThread(persistentDataPath))
            {
                IsBackground = true,
                Name = "Audience.GAIDFetch",
            };
            thread.Start();
        }

        private static void FetchOnWorkerThread(string persistentDataPath)
        {
            // Unity 2021 does not auto-attach managed threads to the JVM;
            // first JNI call segfaults libunity.so without this.
            AndroidJNI.AttachCurrentThread();
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var clientClass = new AndroidJavaClass("com.google.android.gms.ads.identifier.AdvertisingIdClient"))
                using (var info = clientClass.CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", activity))
                {
                    var gaid = info.Call<string>("getId");
                    var limit = info.Call<bool>("isLimitAdTrackingEnabled");
                    // Honor the user's opt-out at the cache layer: never
                    // persist the raw GAID when isLimitAdTrackingEnabled
                    // returned true, even though Google's API still hands it
                    // back. AttributionContext also filters at emission, but
                    // dropping it here keeps an opted-out identifier off disk.
                    var cachedGaid = limit ? string.Empty : (gaid ?? string.Empty);
                    WriteCacheEntry(persistentDataPath, cachedGaid, limit);
                }
            }
            catch (Exception ex)
            {
                // Play Services missing, network, or user disabled ads.
                // Cache stays empty; next launch retries.
                Log.Warn(AudienceLogs.GAIDFetchThrew(ex));
            }
            finally
            {
                // After using-block disposal (DeleteGlobalRef needs an
                // attached thread); detaching first would crash dispose.
                AndroidJNI.DetachCurrentThread();
            }
        }
#else
        private static void StartFetchNative(string persistentDataPath)
        {
            // Editor / non-Android / define off: no-op.
        }
#endif
    }

    internal readonly struct GAIDInfo
    {
        internal readonly string Gaid;
        internal readonly bool LimitAdTracking;

        internal GAIDInfo(string gaid, bool limitAdTracking)
        {
            Gaid = gaid;
            LimitAdTracking = limitAdTracking;
        }
    }
}
