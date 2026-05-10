#nullable enable

#if UNITY_STANDALONE_LINUX && UNITY_6000_0_OR_NEWER
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Immutable.Audience.Samples.SampleApp.Tests
{
    // Hides log pane on Unity 6 Linux. Skips llvmpipe rasterising
    // thousands of UI Toolkit triangles per frame.
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

        // Fires on every scene load. Idempotent.
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
