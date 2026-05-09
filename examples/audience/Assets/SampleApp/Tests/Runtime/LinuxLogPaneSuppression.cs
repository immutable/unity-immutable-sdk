#nullable enable

#if UNITY_STANDALONE_LINUX
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Immutable.Audience.Samples.SampleApp.Tests
{
    // Linux PlayMode test optimisation: hide the SampleApp log pane so UI
    // Toolkit does not generate triangles for its rows during test runs.
    //
    // The player profile captured on PR 765 showed Render Thread spending
    // roughly 4.5 seconds per frame in Gfx.PresentFrame self time on Unity
    // 6 Linux cells, with ~2920 batches and ~7520 triangles per frame.
    // Camera.Render is 2 ms, UI.RenderOverlays 1.45 ms; the rest is
    // llvmpipe rasterising the deferred command buffer at present time.
    // The bulk of those triangles come from the log pane, which
    // accumulates one row per logged event over the course of a session.
    //
    // display:none keeps elements in the visual tree (so VisualElement.Q
    // and contentContainer.Children() still find them) but skips layout
    // and render entirely. Tests assert on log entries via userData on
    // each row, which is reference-based, not layout-based, so the
    // assertions stay correct.
    //
    // Engages only on StandaloneLinux64 builds (gated by the #if). Mac
    // and Windows PlayMode runs are unaffected.
    [SetUpFixture]
    public sealed class LinuxLogPaneSuppression
    {
        [OneTimeSetUp]
        public void RegisterSceneHook()
        {
            SceneManager.sceneLoaded += HideLogPane;
        }

        [OneTimeTearDown]
        public void DeregisterSceneHook()
        {
            SceneManager.sceneLoaded -= HideLogPane;
        }

        // Re-fires for every scene load. The SampleApp's UIDocument runs
        // its UI initialisation in Awake, so by the time sceneLoaded
        // fires the log ScrollView is in the tree and ready to be
        // styled. The hook is idempotent across loads.
        private static void HideLogPane(Scene scene, LoadSceneMode mode)
        {
            var sample = Object.FindFirstObjectByType<AudienceSample>(FindObjectsInactive.Include);
            if (sample == null) return;

            var doc = sample.GetComponent<UIDocument>();
            if (doc == null) return;

            var root = doc.rootVisualElement;
            if (root == null) return;

            var log = root.Q<ScrollView>(SampleAppUi.LogScrollView);
            if (log == null) return;

            log.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            Debug.Log("[LinuxLogPaneSuppression] log pane hidden for Linux PlayMode test run.");
        }
    }
}
#endif
