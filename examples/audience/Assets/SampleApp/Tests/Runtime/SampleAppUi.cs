#nullable enable

namespace Immutable.Audience.Samples.SampleApp.Tests
{
    // Single source of truth for the literal strings that the live-fire tests
    // touch — UXML element names, log labels, consent values, CSS classes,
    // SDK paths.
    //
    // Every constant below is mirrored in production code (UXML markup,
    // AudienceSample.UI.cs, AudienceSample.cs, ConsentLevel.cs,
    // AudiencePaths.cs). Each section's docstring names the source so a
    // future change in production can be traced back to here.
    //
    // If the production string drifts, the corresponding test fails loudly
    // (Q<> returns null → NRE on .Click(), or WaitForLogEntry times out).
    // Drift is caught at the next CI run, not silently.
    internal static class SampleAppUi
    {
        // Scene asset name registered in EditorBuildSettings.
        internal const string SceneName = "SampleApp";

        // The env var that carries the sandbox publishable key into the
        // built player at test time. Test runs inject it on the Unity CLI;
        // CI wires it from the AUDIENCE_TEST_PUBLISHABLE_KEY repo secret.
        internal const string EnvKey = "AUDIENCE_TEST_PUBLISHABLE_KEY";

        // Mirrors AudiencePaths.RootDirName — the SDK persists consent /
        // identity / queue under <persistentDataPath>/imtbl_audience. SetUp
        // wipes this between tests so on-disk state can't leak.
        internal const string SdkPersistedDirName = "imtbl_audience";

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
            internal const string AliasToId = "alias-to-id";
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

        // ---- Log labels ----
        // Mirrors AudienceSample.cs: every RunAndLog(label, ...) and
        // AppendLog(label, ...) call. Tests await these via WaitForLogEntry.

        internal static class LogLabels
        {
            internal const string Init = "INIT";
            internal const string Flush = "flush()";
            internal const string Reset = "reset()";
            internal const string Shutdown = "shutdown()";
            internal const string DeleteData = "deleteData()";
            internal const string Track = "track()";
            internal const string SetConsent = "setConsent()";
            internal const string Identify = "identify()";
            internal const string IdentifyTraits = "identify(traits)";
            internal const string Alias = "alias()";
        }

        // ---- Consent dropdown / status values ----
        // Mirrors ConsentLevel.ToLowercaseString().

        internal static class Consent
        {
            internal const string None = "none";
            internal const string Anonymous = "anonymous";
            internal const string Full = "full";
        }
    }
}
