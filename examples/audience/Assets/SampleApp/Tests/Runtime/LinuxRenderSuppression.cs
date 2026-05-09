#nullable enable

#if UNITY_STANDALONE_LINUX
using NUnit.Framework;
using UnityEngine;

namespace Immutable.Audience.Samples.SampleApp.Tests
{
    // Linux-only test suppression: clamp the player's frame rate to 1 fps
    // and disable vsync. The SampleApp PlayMode tests assert on UI Toolkit
    // visual-element state, which is layout-driven, not paint-driven.
    // Painting fewer frames between coroutine yields removes llvmpipe
    // fragment-fill cost without changing what tests observe.
    //
    // Scope: every test in the Tests assembly when the build target is
    // StandaloneLinux64. UNITY_STANDALONE_LINUX is defined by Unity for
    // PlayMode runs invoked with -testPlatform StandaloneLinux64.
    [SetUpFixture]
    public sealed class LinuxRenderSuppression
    {
        private int _priorTargetFrameRate;
        private int _priorVSyncCount;

        [OneTimeSetUp]
        public void Suppress()
        {
            _priorTargetFrameRate = Application.targetFrameRate;
            _priorVSyncCount = QualitySettings.vSyncCount;

            Application.targetFrameRate = 1;
            QualitySettings.vSyncCount = 0;
        }

        [OneTimeTearDown]
        public void Restore()
        {
            Application.targetFrameRate = _priorTargetFrameRate;
            QualitySettings.vSyncCount = _priorVSyncCount;
        }
    }
}
#endif
