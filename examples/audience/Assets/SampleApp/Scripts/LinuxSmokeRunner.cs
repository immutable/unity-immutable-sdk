#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Immutable.Audience.Samples.SampleApp
{
    // Headless smoke runner used by the linux-build-smoke CI job. Boots the
    // Audience SDK, sends one marker event, awaits a flush, then quits with
    // exit code 0 on success or 1 on any failure. Attached to a runtime
    // GameObject by LinuxSmokeBuilder; does not run during normal SampleApp
    // execution.
    public sealed class LinuxSmokeRunner : MonoBehaviour
    {
        private const string KeyEnv  = "AUDIENCE_TEST_PUBLISHABLE_KEY";
        private const string RunEnv  = "AUDIENCE_TEST_RUN_ID";
        private const string CellEnv = "AUDIENCE_TEST_CELL_ID";

        private const string EventName = "linux_smoke_ci_marker";
        private const int FlushTimeoutSeconds = 20;
        private const int OverallTimeoutSeconds = 30;

        private void Start()
        {
            StartCoroutine(RunSmoke());
        }

        private IEnumerator RunSmoke()
        {
            var deadline = Time.realtimeSinceStartup + OverallTimeoutSeconds;
            AudienceError? capturedError = null;

            string? key = Environment.GetEnvironmentVariable(KeyEnv);
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError($"[LinuxSmoke] {KeyEnv} is unset. Cannot init.");
                Application.Quit(1);
                yield break;
            }

            var config = new AudienceConfig
            {
                PublishableKey = key,
                Consent = ConsentLevel.Full,
                Debug = true,
                OnError = err => capturedError = err,
            };

            try
            {
                ImmutableAudience.Init(config);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LinuxSmoke] Init threw: {ex}");
                Application.Quit(1);
                yield break;
            }

            var props = new Dictionary<string, object>
            {
                ["runId"]  = Environment.GetEnvironmentVariable(RunEnv)  ?? "(unset)",
                ["cellId"] = Environment.GetEnvironmentVariable(CellEnv) ?? "(unset)",
                ["host"]   = "linux-build-smoke",
            };

            try
            {
                ImmutableAudience.Track(EventName, props);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LinuxSmoke] Track threw: {ex}");
                Application.Quit(1);
                yield break;
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(FlushTimeoutSeconds));
            var flushTask = ImmutableAudience.FlushAsync(cts.Token);

            while (!flushTask.IsCompleted)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Debug.LogError("[LinuxSmoke] Overall deadline reached before flush completed.");
                    Application.Quit(1);
                    yield break;
                }
                yield return null;
            }

            if (flushTask.IsFaulted)
            {
                Debug.LogError($"[LinuxSmoke] FlushAsync faulted: {flushTask.Exception}");
                Application.Quit(1);
                yield break;
            }

            if (capturedError != null)
            {
                Debug.LogError($"[LinuxSmoke] AudienceError fired during run: {capturedError}");
                Application.Quit(1);
                yield break;
            }

            Debug.Log("[LinuxSmoke] OK. Init + Track + FlushAsync completed without errors.");
            Application.Quit(0);
        }
    }
}
