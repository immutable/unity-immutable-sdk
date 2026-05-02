#nullable enable

namespace Immutable.Audience.Samples.SampleApp
{
    // Strings shared between sample-app production code (UXML markup,
    // AudienceSample.UI.cs, AudienceSample.cs) and the live-fire tests
    // under Tests/Runtime/. Lives in Scripts so production can reference
    // it; the Tests assembly already references Scripts.
    //
    // If a production string drifts from a constant here, the matching
    // test fails (Q<> returns null on .Click(), or WaitForLogEntry times
    // out).
    internal static class SampleAppUi
    {
        // Scene asset name registered in EditorBuildSettings.
        internal const string SceneName = "SampleApp";

        // The env var that carries the sandbox publishable key into the
        // built player at test time. Test runs inject it on the Unity CLI;
        // CI wires it from the AUDIENCE_TEST_PUBLISHABLE_KEY repo secret.
        internal const string EnvKey = "AUDIENCE_TEST_PUBLISHABLE_KEY";

        // Mirrors Constants.SandboxBaseUrl — used by the BaseUrl-override test.
        internal const string SandboxBaseUrl = "https://api.sandbox.immutable.com";

        // ---- UXML element names ----
        // All names verified against examples/audience/Assets/SampleApp/Resources/AudienceSample.uxml.

        internal static class Setup
        {
            internal const string PublishableKey = "publishable-key";
            internal const string InitialConsent = "initial-consent";
            internal const string BaseUrl = "base-url";
            internal const string Debug = "debug";
            internal const string FlushInterval = "flush-interval";
            internal const string FlushSize = "flush-size";
        }

        internal static class Buttons
        {
            internal const string Init = "btn-init";
            internal const string Page = "btn-page";
            internal const string Flush = "btn-flush";
            internal const string Reset = "btn-reset";
            internal const string Shutdown = "btn-shutdown";
            internal const string DeleteData = "btn-delete-data";
            internal const string ConsentNone = "btn-consent-none";
            internal const string ConsentAnon = "btn-consent-anon";
            internal const string ConsentFull = "btn-consent-full";
            internal const string CustomEvent = "btn-custom-event";
            internal const string Identify = "btn-identify";
            internal const string IdentifyTraits = "btn-identify-traits";
            internal const string Alias = "btn-alias";
            internal const string ClearLog = "btn-clear-log";
            internal const string CopyLog = "btn-copy-log";

            // Mirrors AudienceSample.UI.cs PopulateTypedEventAccordions naming:
            //   send.name = $"btn-typed-{spec.Name.Replace('_', '-')}";
            internal static string TypedEvent(string specName) =>
                "btn-typed-" + specName.Replace('_', '-');
        }

        internal static class CustomEvent
        {
            internal const string Name = "custom-event-name";
            internal const string Props = "custom-event-props";
        }

        internal static class IdentityFields
        {
            internal const string Id = "id-id";
            internal const string Traits = "id-traits";
            internal const string Type = "id-type";
            internal const string TraitsUpdate = "traits-update";
            internal const string AliasFromId = "alias-from-id";
            internal const string AliasFromType = "alias-from-type";
            internal const string AliasToId = "alias-to-id";
            internal const string AliasToType = "alias-to-type";
        }

        internal static class StatusBar
        {
            internal const string Consent = "status-consent";
            internal const string Endpoint = "status-endpoint";
            internal const string Anon = "status-anon";
            internal const string User = "status-user";
            internal const string Session = "status-session";
            internal const string Queue = "status-queue";

            // Mirrors AudienceSample.UI.cs SetStatusCell — the placeholder
            // used when a status cell has no value.
            internal const string EmptyText = "—";
        }

        internal static class IdentityPanel
        {
            internal const string UserId = "identity-user-id";
            internal const string IdentityType = "identity-identity-type";
            internal const string Traits = "identity-traits";
            internal const string Aliases = "identity-aliases";
        }

        internal static class Tabs
        {
            internal const string Setup = "tab-setup";
            internal const string Consent = "tab-consent";
            internal const string TypedEvents = "tab-typed-events";
            internal const string Identity = "tab-identity";
        }

        internal static class Panels
        {
            internal const string Setup = "panel-setup";
            internal const string Consent = "panel-consent";
            internal const string TypedEvents = "panel-typed-events";
            internal const string Identity = "panel-identity";
        }

        // The ScrollView that holds log rows. SampleAppTestHelpers queries
        // it as `root.Q<ScrollView>(SampleAppUi.LogScrollView)`.
        internal const string LogScrollView = "log";

        // The banner that warns when the publishable key looks like a prod
        // key. Toggled via the "hidden" CSS class.
        internal const string ProdWarning = "prod-warning";

        // ---- Layout-internal element names ----
        // Tests do not currently reference these, but production AudienceSample.UI.cs
        // looks them up by name via Require<>() / Q<>(). Listed here so a UXML
        // rename has a single home to update.
        internal static class Layout
        {
            internal const string SdkVersion = "sdk-version";
            internal const string TabBar = "tab-bar";
            internal const string TypedEventsHost = "typed-events-host";
            internal const string LogResizeHandle = "log-resize-handle";
            internal const string PageScroll = "page-scroll";
            internal const string ControlsColumn = "controls-column";
            internal const string LogColumn = "log-column";
            internal const string SampleAppGrid = "sample-app-grid";
            internal const string LogCount = "log-count";
            internal const string AccordionItem = "accordion-item";
            internal const string AccordionTitle = "accordion-title";
            internal const string AccordionContent = "accordion-content";
        }

        // ---- Log badge text ----
        // Two-letter pill text on every log row, picked by LogSource.
        internal static class LogBadgeText
        {
            internal const string Sdk = "SDK";
            internal const string App = "APP";
        }

        // ---- Button text ----
        // Visible text on dynamically-built buttons (UXML-defined buttons
        // carry their text as XML attributes).
        internal static class ButtonText
        {
            internal const string Send = "Send";
            internal const string Copy = "Copy";
            internal const string Copied = "Copied";
        }

        // ---- Resources paths ----
        // Names passed to Resources.Load<T>() for sample-app assets. Must
        // mirror filenames under examples/audience/Assets/SampleApp/Resources/.
        internal static class Resources
        {
            internal const string PanelSettings = "AudienceSampleAppPanelSettings";
            internal const string SampleAppTree = "AudienceSample";
            internal const string SampleAppStyleSheet = "AudienceSample";
            internal const string AccordionTemplate = "Templates/Accordion";
        }

        // ---- Human-readable side messages ----
        // Strings the sample app surfaces to the user via the log pane on
        // consent / identity flows. Listed here so a translation pass would
        // touch one file.
        internal static class Messages
        {
            internal const string QueueStartedSessionCreated = "queue started, session created";
            internal const string QueuePurgedAnonymousIdCleared = "queue purged, anonymous ID cleared";
            internal const string UserIdCleared = "userId cleared";
            internal const string NoActiveIdentity = "no active identity, call Identify first";
            internal const string TraitsRequired = "traits required";
            internal const string Ready = "Sample app loaded. Paste a publishable key and click Init.";

            // Status messages emitted by AudienceSample's RunAndLog handlers.
            internal const string SdkStopped = "SDK stopped";
            internal const string AnonymousIdRegeneratedQueueCleared = "anonymous ID regenerated, queue cleared";
            internal const string QueueFlushed = "queue flushed";
            internal const string ErasureRequestDispatched = "erasure request dispatched";
            internal const string BackendAcknowledged = "backend acknowledged";

            // Formatted variants for use with string.Format or interpolation.
            internal const string TrackDroppedConsentFmt =
                "track dropped, consent is {0}; raise to anonymous or full to queue events";
            internal const string FlushIntervalBelowOneSecondClampedFmt =
                "flushInterval {0}ms below 1s, clamped";
        }

        // Mirrors AudienceSample.UI.cs PopulateTypedEventAccordions naming:
        //   input.name = $"typed-{spec.Name.Replace('_', '-')}-{field.Key.ToLowerInvariant().Replace('_', '-')}";
        internal static string TypedEventField(string specName, string fieldKey) =>
            "typed-" + specName.Replace('_', '-') + "-" + fieldKey.ToLowerInvariant().Replace('_', '-');

        // ---- CSS classes ----

        // Toggled by ActivateTab on tab buttons + panels, by RefreshConsentPills
        // on consent buttons, etc.
        internal const string ActiveClass = "active";

        // Toggled by RefreshStatusBar on the prod-warning banner.
        internal const string HiddenClass = "hidden";

        // Every other CSS class string the sample's Scripts manipulate. Each
        // is also defined in AudienceSample.uxml and AudienceSample.uss; a
        // rename in the stylesheet must update the matching entry here.
        internal static class Css
        {
            // Status-cell colour variants applied via SetStatusCell.
            internal const string StateOk = "state-ok";
            internal const string StateWarn = "state-warn";
            internal const string StateErr = "state-err";
            internal const string Dim = "dim";

            // Click-to-copy flash on status cells and log rows.
            internal const string Copied = "copied";

            // Mobile-narrow grid breakpoint (USS lacks @media at runtime).
            internal const string Narrow = "narrow";

            // Placeholder fade-out trigger; both flip together.
            internal const string PlaceholderHost = "placeholder-host";
            internal const string FieldPlaceholder = "field-placeholder";
            internal const string HasValue = "has-value";
            internal const string IsFocused = "is-focused";

            // Field row layout in typed-event accordions.
            internal const string Field = "field";
            internal const string FieldLabel = "field-label";
            internal const string Actions = "actions";

            // Marks the trailing actions row in a typed-event accordion so
            // the USS rule can drop the bottom border on the last entry.
            internal const string Last = "last";

            // Status-cell click-to-copy hook.
            internal const string StatusValue = "status-value";

            // Accordion header / arrow / open state on typed-event rows.
            internal const string AccordionHeader = "accordion-header";
            internal const string AccordionArrow = "accordion-arrow";
            internal const string Open = "open";

            // Log row chrome.
            internal const string LogRow = "log-row";
            internal const string LogRowHead = "log-row-head";
            internal const string LogTs = "log-ts";
            internal const string LogBadge = "log-badge";
            internal const string LogLabel = "log-label";
            internal const string LogCopy = "log-copy";
            internal const string LogCopyBack = "log-copy-back";
            internal const string LogCopyFront = "log-copy-front";
            internal const string LogBody = "log-body";

            // Log row level-prefix; final class is e.g. "log-warn".
            internal const string LogLevelPrefix = "log-";

            // Badge variant for SDK vs App rows.
            internal const string BadgeSdk = "badge-sdk";
            internal const string BadgeApp = "badge-app";

            // Long-body collapse toggle on log rows.
            internal const string Collapsed = "collapsed";

            // Tick label injected into Unity's runtime Toggle so .checked
            // renders a glyph not a coloured square.
            internal const string DebugTick = "debug-tick";
            internal const string UnityToggleCheckmark = "unity-toggle__checkmark";
        }

        // ---- Log labels ----
        // Mirrors AudienceSample.cs: every RunAndLog(label, ...) and
        // AppendLog(label, ...) call. Tests await these via WaitForLogEntry.

        internal static class LogLabels
        {
            internal const string Init = "INIT";
            internal const string Page = "page()";
            internal const string Flush = "flush()";
            internal const string Reset = "reset()";
            internal const string Shutdown = "shutdown()";
            internal const string DeleteData = "deleteData()";
            internal const string Track = "track()";
            internal const string SetConsent = "setConsent()";
            internal const string Identify = "identify()";
            internal const string IdentifyTraits = "identify(traits)";
            internal const string Alias = "alias()";

            // Sample-app-only labels (no SDK action behind them).
            internal const string Ready = "READY";
            internal const string Sdk = "sdk";
            internal const string OnError = "onError";
        }

        // ---- Consent dropdown / status values ----
        // Mirrors ConsentLevel.ToLowercaseString().

        internal static class Consent
        {
            internal const string None = "none";
            internal const string Anonymous = "anonymous";
            internal const string Full = "full";
        }

        // Log payload JSON keys used by RunAndLog "Ok" row dictionaries.

        internal static class LogPayloadKeys
        {
            // Track outcomes
            internal const string Event = "event";
            internal const string Overload = "overload";
            internal const string Effects = "effects";

            // Identify / Alias outcomes
            internal const string Id = "id";
            internal const string Accepted = "accepted";
            internal const string From = "from";
            internal const string To = "to";

            // OnError row payload
            internal const string Code = "code";
            internal const string Message = "message";

            // Init config echo
            internal const string Consent = "consent";
            internal const string Debug = "debug";
            internal const string FlushIntervalSeconds = "flushIntervalSeconds";
            internal const string FlushSize = "flushSize";
            internal const string PackageVersion = "packageVersion";
            internal const string ShutdownFlushTimeoutMs = "shutdownFlushTimeoutMs";
            internal const string PublishableKey = "publishableKey";
            internal const string PersistentDataPath = "persistentDataPath";

            // Track overload values.
            internal static class OverloadValues
            {
                internal const string Typed = "typed";
                internal const string String = "string";
            }
        }
    }
}
