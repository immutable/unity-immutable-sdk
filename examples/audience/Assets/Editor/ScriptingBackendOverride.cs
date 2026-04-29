#nullable enable

using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Immutable.Audience.Samples.SampleApp.Editor
{
    // Lets the test runner pick the scripting backend per-build via the
    // AUDIENCE_SCRIPTING_BACKEND env var ("IL2CPP" or "Mono2x"). Unity has
    // no built-in CLI flag for this, so we hook the build pre-process and
    // patch PlayerSettings before the player is compiled.
    //
    // Stripping is also flipped to the realistic per-backend default:
    //   IL2CPP → High (the aggressive linker config studios ship under).
    //   Mono   → Disabled (Mono studios rarely strip; High under Mono can
    //                      strip Net.Http SSL chain code paths).
    //
    // Usage:
    //   AUDIENCE_SCRIPTING_BACKEND=Mono2x Unity -batchmode -runTests ...
    //   AUDIENCE_SCRIPTING_BACKEND=IL2CPP Unity -batchmode -runTests ...
    //
    // Unset means "respect ProjectSettings.asset as-is".
    internal sealed class ScriptingBackendOverride : IPreprocessBuildWithReport
    {
        private const string EnvVar = "AUDIENCE_SCRIPTING_BACKEND";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var requested = Environment.GetEnvironmentVariable(EnvVar);
            if (string.IsNullOrEmpty(requested)) return;

            ScriptingImplementation backend = requested switch
            {
                "IL2CPP" => ScriptingImplementation.IL2CPP,
                "Mono2x" => ScriptingImplementation.Mono2x,
                _ => throw new BuildFailedException(
                    $"{EnvVar} must be 'IL2CPP' or 'Mono2x'; got '{requested}'"),
            };

            var group = BuildTargetGroup.Standalone;
            var currentBackend = PlayerSettings.GetScriptingBackend(group);
            if (currentBackend != backend)
            {
                PlayerSettings.SetScriptingBackend(group, backend);
                Debug.Log($"[{nameof(ScriptingBackendOverride)}] backend {currentBackend} → {backend}.");
            }

            var stripping = backend == ScriptingImplementation.IL2CPP
                ? ManagedStrippingLevel.High
                : ManagedStrippingLevel.Disabled;
            var currentStripping = PlayerSettings.GetManagedStrippingLevel(group);
            if (currentStripping != stripping)
            {
                PlayerSettings.SetManagedStrippingLevel(group, stripping);
                Debug.Log($"[{nameof(ScriptingBackendOverride)}] managedStrippingLevel {currentStripping} → {stripping}.");
            }
        }
    }
}
