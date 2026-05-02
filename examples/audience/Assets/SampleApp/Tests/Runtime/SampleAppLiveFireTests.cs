#nullable enable

using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Immutable.Audience.Samples.SampleApp.Tests
{
    [TestFixture]
    internal class SampleAppLiveFireTests
    {
        // Cached allocations for fixed-delay yields (SDK needs time to flush
        // rather than "wait up to N seconds for a condition"). Static readonly
        // so the WaitForSecondsRealtime instance is created once per class load.
        private static readonly WaitForSecondsRealtime _twoSeconds = new(2f);

        private VisualElement? _root;
        private string _key = "";

        [SetUp]
        public void SetUp()
        {
            // ImmutableAudience is a static; tests must reset between runs.
            // ResetState is internal — reached via reflection (BindingFlags.NonPublic
            // bypasses C# access checks; no InternalsVisibleTo required).
            var t = typeof(ImmutableAudience);
            var m = t.GetMethod("ResetState",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            m?.Invoke(null, null);

            // ResetState only clears in-memory state. The SDK persists consent
            // (and identity/queue) to disk under <persistentDataPath>/imtbl_audience.
            // Without wiping it, a SetConsent(None) from a prior test leaks into
            // the next test's Init via ConsentStore.Load.
            var sdkDir = AudiencePaths.AudienceDir(Application.persistentDataPath);
            if (System.IO.Directory.Exists(sdkDir))
                System.IO.Directory.Delete(sdkDir, recursive: true);

            // Unity's bundled Mono runtime ships a curated root-CA set that
            // does not include the chain api.sandbox.immutable.com presents,
            // so HttpClient under Mono2x raises "SSL connection could not be
            // established" on every Flush. The cert is valid; only Mono's
            // verification fails. IL2CPP uses the OS CA store and is fine.
            //
            // Bypass cert validation IN THE TEST PROCESS ONLY so the same
            // suite exercises both backends. Production SDK code is
            // untouched. Acceptable risk: this test process talks only to
            // sandbox; live-fire payloads carry no real user data.
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                (_, _, _, _) => true;

            _root = null;
        }

        // ---- Helpers shared by every test ----

        // Loads the SampleApp scene, types the env-var key into publishable-key,
        // optionally sets initial-consent (defaults to Anonymous via dropdown),
        // runs the configure callback for any extra setup (base-url, flush-size,
        // debug toggle, etc.), clicks btn-init, and waits for the INIT@Ok row.
        // Stashes the root VisualElement on _root so callers can drive
        // subsequent buttons.
        private IEnumerator LoadAndInit(string? initialConsent = null, Action<VisualElement>? configure = null)
        {
            yield return LoadSceneOnly();

            _root!.Q<TextField>(SampleAppUi.Setup.PublishableKey).value = _key;
            if (!string.IsNullOrEmpty(initialConsent))
                _root.Q<DropdownField>(SampleAppUi.Setup.InitialConsent).value = initialConsent;
            configure?.Invoke(_root);

            _root.Q<Button>(SampleAppUi.Buttons.Init).Click();
            yield return SampleAppTestHelpers.WaitForLogEntry(_root, SampleAppUi.LogLabels.Init, LogLevels.Ok, 5f);
        }

        // Scene load + AudienceSample lookup + root capture, without clicking
        // btn-init. Useful for tests that need to inspect pre-init UI state
        // (e.g. prod-warning visibility for non-test keys).
        private IEnumerator LoadSceneOnly()
        {
            _key = Environment.GetEnvironmentVariable(SampleAppUi.EnvKey) ?? "";
            Assume.That(_key, Is.Not.Null.And.Not.Empty,
                $"{SampleAppUi.EnvKey} must be set for live-fire tests. Skipping.");

            yield return SceneManager.LoadSceneAsync(SampleAppUi.SceneName, LoadSceneMode.Single);
            yield return null;  // one extra frame for Awake/InitializeUi

#if UNITY_2022_2_OR_NEWER
            var sample = UnityEngine.Object.FindFirstObjectByType<AudienceSample>(FindObjectsInactive.Include);
#else
            var sample = UnityEngine.Object.FindObjectOfType<AudienceSample>(includeInactive: true);
#endif
            Assume.That(sample, Is.Not.Null, "AudienceSample MonoBehaviour expected in scene");

            _root = sample!.GetComponent<UIDocument>().rootVisualElement;
        }

        // Clicks btn-flush, waits for the flush() Ok row, then asserts the log
        // pane has zero Err entries. label (optional) prefixes the assertion
        // message so failures point at which test step misbehaved.
        private IEnumerator FlushAndAssertNoErrors(string label = "")
        {
            _root!.Q<Button>(SampleAppUi.Buttons.Flush).Click();
            yield return SampleAppTestHelpers.WaitForLogEntry(_root, SampleAppUi.LogLabels.Flush, LogLevels.Ok, 30f);
            AssertNoErrors(label);
        }

        private void AssertNoErrors(string label = "")
        {
            var errors = SampleAppTestHelpers.CountLogEntriesAtLevel(_root!, LogLevels.Err);
            var prefix = string.IsNullOrEmpty(label) ? "" : $"{label}: ";
            Assert.Zero(errors,
                $"{prefix}expected no error log entries; got {errors}. " +
                $"Entries: {SampleAppTestHelpers.DescribeLogEntries(_root!)}");
        }

        // Clicks one of btn-consent-{none|anon|full} and waits for the
        // setConsent() Ok row to confirm the SDK applied the transition.
        private IEnumerator SetConsentVia(string consentButtonName)
        {
            _root!.Q<Button>(consentButtonName).Click();
            yield return SampleAppTestHelpers.WaitForLogEntry(_root, SampleAppUi.LogLabels.SetConsent, LogLevels.Ok, 5f);
        }

        // ---- Tests ----

        [UnityTest]
        public IEnumerator InitTrackFlush_AgainstSandbox_FlushReportsOk()
        {
            yield return LoadAndInit();

            // Page-view track via btn-page; the test doesn't wait on its row.
            _root!.Q<Button>(SampleAppUi.Buttons.Page).Click();
            yield return null;

            yield return FlushAndAssertNoErrors();
        }

        [UnityTest]
        public IEnumerator Event_Progression_FlushReportsOk()
        {
            // Progression's only required field is `status` (enum, defaults to "start").
            yield return DriveTypedEventAndFlush(SampleAppUi.Buttons.TypedEvent(EventNames.Progression));
        }

        [UnityTest]
        public IEnumerator TypedEvent_Resource_FlushReportsOk()
        {
            yield return DriveTypedEventAndFlush(SampleAppUi.Buttons.TypedEvent(EventNames.Resource), root =>
            {
                root.Q<TextField>(SampleAppUi.TypedEventField(EventNames.Resource, EventPropertyKeys.Currency)).value = "GOLD";
                root.Q<TextField>(SampleAppUi.TypedEventField(EventNames.Resource, EventPropertyKeys.Amount)).value = "100";
            });
        }

        [UnityTest]
        public IEnumerator TypedEvent_Purchase_FlushReportsOk()
        {
            yield return DriveTypedEventAndFlush(SampleAppUi.Buttons.TypedEvent(EventNames.Purchase), root =>
            {
                root.Q<TextField>(SampleAppUi.TypedEventField(EventNames.Purchase, EventPropertyKeys.Currency)).value = "USD";
                root.Q<TextField>(SampleAppUi.TypedEventField(EventNames.Purchase, EventPropertyKeys.Value)).value = "9.99";
            });
        }

        [UnityTest]
        public IEnumerator TypedEvent_MilestoneReached_FlushReportsOk()
        {
            yield return DriveTypedEventAndFlush(SampleAppUi.Buttons.TypedEvent(EventNames.MilestoneReached), root =>
            {
                root.Q<TextField>(SampleAppUi.TypedEventField(EventNames.MilestoneReached, EventPropertyKeys.Name)).value = SampleAppLiveFireFixtures.MilestoneSmokeName;
            });
        }

        // Shared driver: Init → fill required fields → click typed-event Send → Flush →
        // assert no error rows. fillFields is null when the event has no required fields
        // beyond defaults.
        private IEnumerator DriveTypedEventAndFlush(
            string typedButtonName, Action<VisualElement>? fillFields = null)
        {
            yield return LoadAndInit();

            fillFields?.Invoke(_root!);
            yield return null;

            var btn = _root!.Q<Button>(typedButtonName);
            Assert.IsNotNull(btn, $"expected button {typedButtonName} to exist after PopulateTypedEventAccordions");
            btn.Click();
            yield return null;

            yield return FlushAndAssertNoErrors($"typed event {typedButtonName}");
        }

        [UnityTest]
        public IEnumerator Identify_AndFlush_FlushReportsOk()
        {
            // Identify requires consent ≥ Full — set it on the initial-consent
            // dropdown before Init rather than upgrading mid-test.
            yield return LoadAndInit(initialConsent: SampleAppUi.Consent.Full);

            _root!.Q<TextField>(SampleAppUi.IdentityFields.Id).value = "il2cpp-test-user-" + DateTime.UtcNow.Ticks;
            _root.Q<Button>(SampleAppUi.Buttons.Identify).Click();
            yield return null;

            yield return FlushAndAssertNoErrors();
        }

        [UnityTest]
        public IEnumerator Alias_AndFlush_FlushReportsOk()
        {
            yield return LoadAndInit(initialConsent: SampleAppUi.Consent.Full);

            _root!.Q<TextField>(SampleAppUi.IdentityFields.AliasFromId).value = "anon-" + DateTime.UtcNow.Ticks;
            _root.Q<TextField>(SampleAppUi.IdentityFields.AliasToId).value = "user-" + DateTime.UtcNow.Ticks;
            _root.Q<Button>(SampleAppUi.Buttons.Alias).Click();
            yield return null;

            yield return FlushAndAssertNoErrors();
        }

        [UnityTest]
        public IEnumerator SetConsent_None_PurgesQueueAndPersists()
        {
            // Init at default Anonymous; enqueue an event; revoke; flush — no errors.
            yield return LoadAndInit();

            _root!.Q<Button>(SampleAppUi.Buttons.Page).Click();
            yield return null;

            yield return SetConsentVia(SampleAppUi.Buttons.ConsentNone);

            // Flushing after revocation should be a no-op (queue purged) — no error.
            _root.Q<Button>(SampleAppUi.Buttons.Flush).Click();
            yield return _twoSeconds;

            AssertNoErrors();
        }

        [UnityTest]
        public IEnumerator SetConsent_FullAfterAnonymous_AcceptsIdentify()
        {
            // Init at default Anonymous, then upgrade to Full so Identify is accepted.
            yield return LoadAndInit();

            yield return SetConsentVia(SampleAppUi.Buttons.ConsentFull);

            _root!.Q<TextField>(SampleAppUi.IdentityFields.Id).value = "consent-test-" + DateTime.UtcNow.Ticks;
            _root.Q<Button>(SampleAppUi.Buttons.Identify).Click();
            yield return null;

            yield return FlushAndAssertNoErrors();
        }

        [UnityTest]
        public IEnumerator CustomTrack_WithDictionaryProps_FlushReportsOk()
        {
            yield return LoadAndInit();

            // Custom event name + JSON props (the sample app parses props as JSON
            // and forwards them as Dictionary<string, object> to ImmutableAudience.Track).
            _root!.Q<TextField>(SampleAppUi.CustomEvent.Name).value = SampleAppLiveFireFixtures.MilestoneSmokeName;
            _root.Q<TextField>(SampleAppUi.CustomEvent.Props).value =
                "{\"int_field\":42,\"str_field\":\"hello\",\"bool_field\":true,\"nested\":{\"a\":1}}";
            _root.Q<Button>(SampleAppUi.Buttons.CustomEvent).Click();
            yield return null;

            yield return FlushAndAssertNoErrors();
        }

        // ---- Lifecycle / control-plane tests ----

        [UnityTest]
        public IEnumerator Shutdown_AfterTrack_NoErrors()
        {
            // Shutdown drains pending events to disk, joins the background drain
            // thread, and disposes timers + HttpClient. IL2CPP can strip any of
            // those if their types/methods aren't reachable from a root assembly.
            yield return LoadAndInit();

            _root!.Q<Button>(SampleAppUi.Buttons.Page).Click();
            yield return null;

            _root.Q<Button>(SampleAppUi.Buttons.Shutdown).Click();
            yield return SampleAppTestHelpers.WaitForLogEntry(_root, SampleAppUi.LogLabels.Shutdown, LogLevels.Ok, 5f);

            AssertNoErrors();
        }

        [UnityTest]
        public IEnumerator Reset_RegeneratesAnonymousIdAndAcceptsTrack()
        {
            // Reset clears identity + queue and rolls a new anonymousId. A
            // following Track must serialise with the new anonymousId and
            // round-trip to sandbox without errors.
            yield return LoadAndInit();

            _root!.Q<Button>(SampleAppUi.Buttons.Page).Click();
            yield return null;

            _root.Q<Button>(SampleAppUi.Buttons.Reset).Click();
            yield return SampleAppTestHelpers.WaitForLogEntry(_root, SampleAppUi.LogLabels.Reset, LogLevels.Ok, 5f);

            _root.Q<Button>(SampleAppUi.Buttons.Page).Click();
            yield return null;

            yield return FlushAndAssertNoErrors();
        }

        [UnityTest]
        public IEnumerator DeleteData_AcknowledgesFromBackend()
        {
            // DeleteData hits the control-plane HTTP endpoint, distinct from
            // the event-batch POST. Catches IL2CPP strips on the control HttpClient
            // path that the regular Flush tests don't exercise.
            yield return LoadAndInit();

            _root!.Q<Button>(SampleAppUi.Buttons.DeleteData).Click();
            yield return SampleAppTestHelpers.WaitForLogEntry(_root, SampleAppUi.LogLabels.DeleteData, LogLevels.Ok, 30f);

            AssertNoErrors();
        }

        [UnityTest]
        public IEnumerator ReInit_AfterShutdown_AcceptsTrack()
        {
            // Shutdown clears _initialised; the same player must accept a
            // second Init using the same publishable-key and resume Track.
            // Catches IL2CPP strips on the lazy-init paths that only fire
            // on second Init (e.g. timer recycling, HttpClient re-creation).
            yield return LoadAndInit();

            _root!.Q<Button>(SampleAppUi.Buttons.Shutdown).Click();
            yield return SampleAppTestHelpers.WaitForLogEntry(_root, SampleAppUi.LogLabels.Shutdown, LogLevels.Ok, 5f);

            _root.Q<Button>(SampleAppUi.Buttons.Init).Click();
            // Two INIT@Ok rows now; WaitForLogEntry returns on the first
            // match it sees, but the original is already in the log so poll
            // by count until the second Ok row appears or the deadline elapses.
            yield return null;
            yield return SampleAppTestHelpers.WaitForCondition(
                () => SampleAppTestHelpers.CountLogEntriesAtLevel(_root, LogLevels.Ok) > 1,
                2f, "second INIT@Ok row after re-init");
            Assert.Greater(SampleAppTestHelpers.CountLogEntriesAtLevel(_root, LogLevels.Ok), 1,
                "expected a second INIT@Ok row after re-init");

            _root.Q<Button>(SampleAppUi.Buttons.Page).Click();
            yield return null;

            yield return FlushAndAssertNoErrors();
        }

        [UnityTest]
        public IEnumerator SetConsent_AnonymousFromFull_StripsUserIdFromTrack()
        {
            // Init at Full so Identify is allowed, then downgrade to Anonymous.
            // Subsequent Track calls must succeed with the userId stripped from
            // the wire payload (SDK behaviour); test asserts no Err entries
            // appear, which means the strip path is intact under IL2CPP.
            yield return LoadAndInit(initialConsent: SampleAppUi.Consent.Full);

            _root!.Q<TextField>(SampleAppUi.IdentityFields.Id).value = "downgrade-user-" + DateTime.UtcNow.Ticks;
            _root.Q<Button>(SampleAppUi.Buttons.Identify).Click();
            yield return SampleAppTestHelpers.WaitForLogEntry(_root, SampleAppUi.LogLabels.Identify, LogLevels.Ok, 5f);

            yield return SetConsentVia(SampleAppUi.Buttons.ConsentAnon);

            _root.Q<Button>(SampleAppUi.Buttons.Page).Click();
            yield return null;

            yield return FlushAndAssertNoErrors();
        }

        [UnityTest]
        public IEnumerator IdentifyTraits_AfterIdentify_FlushReportsOk()
        {
            // Identify(traits) requires an active identity (Identify first) and
            // exercises the identity overload that takes a traits Dictionary —
            // a different reflection/serialiser path than Identify(id) alone.
            yield return LoadAndInit(initialConsent: SampleAppUi.Consent.Full);

            _root!.Q<TextField>(SampleAppUi.IdentityFields.Id).value = "traits-user-" + DateTime.UtcNow.Ticks;
            _root.Q<Button>(SampleAppUi.Buttons.Identify).Click();
            yield return SampleAppTestHelpers.WaitForLogEntry(_root, SampleAppUi.LogLabels.Identify, LogLevels.Ok, 5f);

            // traits-update is the post-Identify traits field (id-traits is the
            // initial Identify path). btn-identify-traits reads traits-update.
            _root.Q<TextField>(SampleAppUi.IdentityFields.TraitsUpdate).value = "{\"plan\":\"premium\",\"level\":42}";
            _root.Q<Button>(SampleAppUi.Buttons.IdentifyTraits).Click();
            yield return SampleAppTestHelpers.WaitForLogEntry(_root, SampleAppUi.LogLabels.IdentifyTraits, LogLevels.Ok, 5f);

            yield return FlushAndAssertNoErrors();
        }

        // The remaining catalogue events have no typed factory in BuildTypedEvent
        // and fall through to ImmutableAudience.Track(string, props). Worth
        // exercising end-to-end so the catalogue UI's null-fallback path is
        // covered under IL2CPP.

        [UnityTest]
        public IEnumerator TypedEvent_SignUp_FlushReportsOk()
        {
            yield return DriveTypedEventAndFlush(SampleAppUi.Buttons.TypedEvent(SampleAppCustomEvents.SignUp));
        }

        [UnityTest]
        public IEnumerator TypedEvent_SignIn_FlushReportsOk()
        {
            yield return DriveTypedEventAndFlush(SampleAppUi.Buttons.TypedEvent(SampleAppCustomEvents.SignIn));
        }

        [UnityTest]
        public IEnumerator TypedEvent_EmailAcquired_FlushReportsOk()
        {
            yield return DriveTypedEventAndFlush(SampleAppUi.Buttons.TypedEvent(SampleAppCustomEvents.EmailAcquired));
        }

        [UnityTest]
        public IEnumerator TypedEvent_WishlistAdd_FlushReportsOk()
        {
            yield return DriveTypedEventAndFlush(SampleAppUi.Buttons.TypedEvent(SampleAppCustomEvents.WishlistAdd), root =>
            {
                root.Q<TextField>(SampleAppUi.TypedEventField(SampleAppCustomEvents.WishlistAdd, SampleAppCustomEventPropertyKeys.GameId)).value = SampleAppLiveFireFixtures.GameId;
            });
        }

        [UnityTest]
        public IEnumerator TypedEvent_WishlistRemove_FlushReportsOk()
        {
            yield return DriveTypedEventAndFlush(SampleAppUi.Buttons.TypedEvent(SampleAppCustomEvents.WishlistRemove), root =>
            {
                root.Q<TextField>(SampleAppUi.TypedEventField(SampleAppCustomEvents.WishlistRemove, SampleAppCustomEventPropertyKeys.GameId)).value = SampleAppLiveFireFixtures.GameId;
            });
        }

        [UnityTest]
        public IEnumerator TypedEvent_GamePageViewed_FlushReportsOk()
        {
            yield return DriveTypedEventAndFlush(SampleAppUi.Buttons.TypedEvent(SampleAppCustomEvents.GamePageViewed), root =>
            {
                root.Q<TextField>(SampleAppUi.TypedEventField(SampleAppCustomEvents.GamePageViewed, SampleAppCustomEventPropertyKeys.GameId)).value = SampleAppLiveFireFixtures.GameId;
            });
        }

        [UnityTest]
        public IEnumerator TypedEvent_LinkClicked_FlushReportsOk()
        {
            yield return DriveTypedEventAndFlush(SampleAppUi.Buttons.TypedEvent(SampleAppCustomEvents.LinkClicked), root =>
            {
                root.Q<TextField>(SampleAppUi.TypedEventField(SampleAppCustomEvents.LinkClicked, SampleAppCustomEventPropertyKeys.Url)).value = SampleAppLiveFireFixtures.LinkUrl;
            });
        }

        // ---- Init-config code paths ----

        [UnityTest]
        public IEnumerator Init_WithBaseUrlOverride_FlushReportsOk()
        {
            // Explicit BaseUrl skips the publishable-key prefix routing in
            // Constants. Same target endpoint here, but the override branch is
            // a different config setup path that IL2CPP could strip independently.
            yield return LoadAndInit(configure: root =>
            {
                root.Q<TextField>(SampleAppUi.Setup.BaseUrl).value = SampleAppUi.SandboxBaseUrl;
            });

            _root!.Q<Button>(SampleAppUi.Buttons.Page).Click();
            yield return null;

            yield return FlushAndAssertNoErrors();
        }

        [UnityTest]
        public IEnumerator Init_WithSubSecondFlushInterval_EmitsClampWarn()
        {
            // BuildAudienceConfig emits a Warn row when flushInterval < 1000ms,
            // then clamps to 1s and continues. Verifies both the Warn-level log
            // path and the clamp.
            yield return LoadAndInit(configure: root =>
            {
                root.Q<TextField>(SampleAppUi.Setup.FlushInterval).value = "500";
            });

            var warns = SampleAppTestHelpers.CountLogEntriesAtLevel(_root!, LogLevels.Warn);
            Assert.Greater(warns, 0,
                "expected at least one Warn row for sub-second flushInterval. " +
                $"Entries: {SampleAppTestHelpers.DescribeLogEntries(_root!)}");
            AssertNoErrors();
        }

        [UnityTest]
        public IEnumerator Init_WithCustomFlushSize_FlushReportsOk()
        {
            yield return LoadAndInit(configure: root =>
            {
                root.Q<TextField>(SampleAppUi.Setup.FlushSize).value = "5";
            });

            _root!.Q<Button>(SampleAppUi.Buttons.Page).Click();
            yield return null;

            yield return FlushAndAssertNoErrors();
        }

        [UnityTest]
        public IEnumerator Init_WithDebugDisabled_FlushReportsOk()
        {
            // Debug toggle controls the SDK's Log.Enabled. Off path is the
            // production default; verify it still functions end-to-end.
            yield return LoadAndInit(configure: root =>
            {
                root.Q<Toggle>(SampleAppUi.Setup.Debug).value = false;
            });

            _root!.Q<Button>(SampleAppUi.Buttons.Page).Click();
            yield return null;

            yield return FlushAndAssertNoErrors();
        }

        [UnityTest]
        public IEnumerator MultiEvent_SingleFlush_NoErrors()
        {
            // Five page Tracks queued up, single Flush. Exercises queue
            // batching + gzip + multi-event payload serialisation under IL2CPP.
            yield return LoadAndInit();

            for (var i = 0; i < 5; i++)
            {
                _root!.Q<Button>(SampleAppUi.Buttons.Page).Click();
                yield return null;
            }

            yield return FlushAndAssertNoErrors();
        }

        [UnityTest]
        public IEnumerator Flush_EmptyQueue_NoErrors()
        {
            // Flush with nothing queued. The batch path must short-circuit
            // cleanly without dispatching an HTTP request.
            yield return LoadAndInit();

            yield return FlushAndAssertNoErrors();
        }

        [UnityTest]
        public IEnumerator PersistedConsent_OverridesDropdownOnReInit()
        {
            // After SetConsent(Full), Shutdown, and a second Init at the
            // dropdown's default (Anonymous), the SDK must re-load Full from
            // disk. Init's log body echoes config (always reads dropdown), so
            // assert against the status-consent label which reflects the
            // SDK's runtime CurrentConsent.
            yield return LoadAndInit();

            yield return SetConsentVia(SampleAppUi.Buttons.ConsentFull);

            _root!.Q<Button>(SampleAppUi.Buttons.Shutdown).Click();
            yield return SampleAppTestHelpers.WaitForLogEntry(_root, SampleAppUi.LogLabels.Shutdown, LogLevels.Ok, 5f);

            // Dropdown is still at default Anonymous; re-Init must read disk.
            _root.Q<Button>(SampleAppUi.Buttons.Init).Click();
            // Poll until OnSdkStateChanged + RefreshStatusBar has run and the
            // label reflects the re-loaded consent level.
            yield return SampleAppTestHelpers.WaitForCondition(
                () => _root.Q<Label>(SampleAppUi.StatusBar.Consent).text == SampleAppUi.Consent.Full,
                2f, "status-consent label to show Full after re-Init from persisted consent");

            var statusConsent = _root.Q<Label>(SampleAppUi.StatusBar.Consent).text;
            Assert.AreEqual(SampleAppUi.Consent.Full, statusConsent,
                $"expected re-Init to restore Full consent from disk; status-consent shows '{statusConsent}'. " +
                $"Entries: {SampleAppTestHelpers.DescribeLogEntries(_root)}");

            AssertNoErrors();
        }

        // ---- Sample-app UI plumbing ----
        // These verify the SAMPLE APP's reactivity, not SDK code paths. They
        // catch sample-app regressions that would mask real SDK issues
        // (e.g. status bar showing stale state) — also exercise the
        // RefreshStatusBar / RefreshConsentPills / RefreshIdentityPanel paths
        // under IL2CPP.

        [UnityTest]
        public IEnumerator StatusBar_ReflectsConsentAfterInit()
        {
            yield return LoadAndInit(initialConsent: SampleAppUi.Consent.Full);
            yield return null;  // OnSdkStateChanged → RefreshStatusBar
            Assert.AreEqual(SampleAppUi.Consent.Full, _root!.Q<Label>(SampleAppUi.StatusBar.Consent).text);
        }

        [UnityTest]
        public IEnumerator StatusBar_PopulatesAnonymousIdAfterInit()
        {
            yield return LoadAndInit();
            yield return null;
            var anon = _root!.Q<Label>(SampleAppUi.StatusBar.Anon).text;
            Assert.AreNotEqual(SampleAppUi.StatusBar.EmptyText, anon,
                "status-anon should contain a non-empty anonymousId after Init");
            Assert.IsNotEmpty(anon);
        }

        [UnityTest]
        public IEnumerator StatusBar_QueueSizeIncrementsAfterTrack()
        {
            // Init auto-fires session_start + game_launch so the queue is
            // already non-zero by the time the status label paints. Capture
            // the baseline and assert btn-page bumps it by at least one.
            yield return LoadAndInit();
            yield return null;

            var queueLabel = _root!.Q<Label>(SampleAppUi.StatusBar.Queue);
            int.TryParse(queueLabel.text, out var baseline);

            _root.Q<Button>(SampleAppUi.Buttons.Page).Click();
            yield return null;
            var deadline = Time.realtimeSinceStartup + 2f;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (int.TryParse(queueLabel.text, out var current) && current > baseline)
                    yield break;
                yield return null;
            }

            int.TryParse(queueLabel.text, out var finalCount);
            Assert.Greater(finalCount, baseline,
                $"queue should grow past baseline {baseline} after btn-page Track; got {finalCount}");
        }

        [UnityTest]
        public IEnumerator ClearLog_RemovesAllRows()
        {
            yield return LoadAndInit();

            var logView = _root!.Q<ScrollView>(SampleAppUi.LogScrollView);
            Assert.Greater(logView.contentContainer.childCount, 0,
                "log should contain READY + INIT rows after LoadAndInit");

            _root.Q<Button>(SampleAppUi.Buttons.ClearLog).Click();
            yield return null;

            Assert.AreEqual(0, logView.contentContainer.childCount,
                "btn-clear-log should remove all rows");
        }

        [UnityTest]
        public IEnumerator TabNav_ClickingTabConsent_ActivatesPanel()
        {
            yield return LoadSceneOnly();

            // panel-setup is active by default; tab-consent should swap to panel-consent.
            _root!.Q<Button>(SampleAppUi.Tabs.Consent).Click();
            yield return null;

            Assert.IsTrue(_root.Q<VisualElement>(SampleAppUi.Panels.Consent).ClassListContains(SampleAppUi.ActiveClass),
                "panel-consent should have 'active' class after clicking tab-consent");
            Assert.IsFalse(_root.Q<VisualElement>(SampleAppUi.Panels.Setup).ClassListContains(SampleAppUi.ActiveClass),
                "panel-setup should lose 'active' when switching away");
        }

        [UnityTest]
        public IEnumerator ConsentPill_ActiveStateMirrorsSdkLevel()
        {
            yield return LoadAndInit();
            yield return null;

            var anonPill = _root!.Q<Button>(SampleAppUi.Buttons.ConsentAnon);
            var fullPill = _root.Q<Button>(SampleAppUi.Buttons.ConsentFull);
            Assert.IsTrue(anonPill.ClassListContains(SampleAppUi.ActiveClass),
                "anon pill should be active right after Init at default Anonymous");
            Assert.IsFalse(fullPill.ClassListContains(SampleAppUi.ActiveClass));

            yield return SetConsentVia(SampleAppUi.Buttons.ConsentFull);
            yield return null;

            Assert.IsTrue(fullPill.ClassListContains(SampleAppUi.ActiveClass),
                "full pill should be active after switching consent to Full");
            Assert.IsFalse(anonPill.ClassListContains(SampleAppUi.ActiveClass));
        }

        [UnityTest]
        public IEnumerator IdentityPanel_PopulatesUserIdAfterIdentify()
        {
            yield return LoadAndInit(initialConsent: SampleAppUi.Consent.Full);

            var userId = "panel-user-" + DateTime.UtcNow.Ticks;
            _root!.Q<TextField>(SampleAppUi.IdentityFields.Id).value = userId;
            _root.Q<Button>(SampleAppUi.Buttons.Identify).Click();
            yield return SampleAppTestHelpers.WaitForLogEntry(_root, SampleAppUi.LogLabels.Identify, LogLevels.Ok, 5f);
            yield return null;  // RefreshIdentityPanel

            Assert.AreEqual(userId, _root.Q<Label>(SampleAppUi.IdentityPanel.UserId).text,
                "identity-user-id label should reflect ImmutableAudience.UserId after Identify");
        }

        [UnityTest]
        public IEnumerator ProdWarning_HiddenForTestKey()
        {
            // The default env-var key is a test key (pk_imapik-test-…). The
            // prod-warning banner should stay hidden after RefreshStatusBar.
            yield return LoadSceneOnly();
            _root!.Q<TextField>(SampleAppUi.Setup.PublishableKey).value = _key;
            yield return null;

            Assert.IsTrue(_root.Q<Label>(SampleAppUi.ProdWarning).ClassListContains(SampleAppUi.HiddenClass),
                "prod-warning should be hidden when the publishable-key is a test key");
        }

        [UnityTest]
        public IEnumerator ProdWarning_VisibleForNonTestKey()
        {
            // A key without the "test-" segment looks production. Don't actually
            // Init so we don't live-fire to prod; just verify the warning UI.
            yield return LoadSceneOnly();
            _root!.Q<TextField>(SampleAppUi.Setup.PublishableKey).value = "pk_imapik-fakeprod-zzzz";
            yield return null;

            Assert.IsFalse(_root.Q<Label>(SampleAppUi.ProdWarning).ClassListContains(SampleAppUi.HiddenClass),
                "prod-warning should be visible when the publishable-key looks like a prod key");
        }
    }
}
