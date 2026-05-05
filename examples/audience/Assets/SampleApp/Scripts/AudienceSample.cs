#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Immutable.Audience.Samples.SampleApp
{
    // Audience SDK sample — UI Toolkit port of the web sample-app. Exercises
    // every public ImmutableAudience API plus an event log that mirrors SDK
    // debug output.
    //
    // Partial layout (single source of truth):
    //
    //   AudienceSample.cs        SDK calls, On* handlers, mirror state, SDK
    //                            callbacks, config builders. Reads UXML
    //                            state ONLY via UI's Capture*Form accessors
    //                            — never touches a UXML field directly.
    //   AudienceSample.UI.cs     UXML fields, binding, rendering, log pane,
    //                            Refresh* methods, Capture*Form accessors.
    //                            No SDK calls, no mirror-state knowledge.
    //   AudienceSample.Events.cs Catalogue, typed-event factory, props
    //                            builder. Pure factory — no UXML, no SDK.
    public sealed partial class AudienceSample : MonoBehaviour
    {
        // ---- State ----

        private bool _initialised;
        private Action<string>? _priorSdkLogWriter;

        // Sample-side identity mirror. SDK owns UserId; type, traits, and
        // aliases are tracked here for the Identity panel.
        private string? _mirrorIdentityType;
        private Dictionary<string, object>? _mirrorTraits;
        private readonly List<string> _mirrorAliases = new List<string>();

        // ---- Lifecycle ----

        private void Awake()
        {
            // InitializeUi must precede the Log.Writer swap — _logView has
            // to be bound before any Log.Warn can land in RouteSdkLogToPane.
            InitializeUi();
            _priorSdkLogWriter = Immutable.Audience.Log.Writer;
            Immutable.Audience.Log.Writer = RouteSdkLogToPane;
        }

        private void OnDestroy()
        {
            Immutable.Audience.Log.Writer = _priorSdkLogWriter;
        }

        // ---- SDK action handlers: SDK lifecycle ----

        private void OnInit() => RunAndLog("INIT", () =>
        {
            var form = CaptureInitForm();
            var config = BuildAudienceConfig(form, OnSdkError);
            ImmutableAudience.Init(config);
            _initialised = true;
            OnSdkStateChanged();
            return Json.Serialize(BuildConfigEcho(config), 2);
        });

        private void OnShutdown() => RunAndLog("shutdown()", () =>
        {
            ImmutableAudience.Shutdown();
            _initialised = false;
            ResetIdentityMirror();
            OnSdkStateChanged();
            return "SDK stopped";
        });

        private void OnReset() => RunAndLog("reset()", () =>
        {
            ImmutableAudience.Reset();
            ResetIdentityMirror();
            OnSdkStateChanged();
            return "anonymous ID regenerated, queue cleared";
        });

        private async Task OnFlushAsync()
        {
            try { await ImmutableAudience.FlushAsync(); AppendLog("flush()", "queue flushed", LogLevel.Ok, LogSource.App); OnSdkStateChanged(); }
            catch (Exception ex) { AppendLog("flush()", ex.Message, LogLevel.Err, LogSource.App); }
        }

        private async Task OnDeleteDataAsync()
        {
            AppendLog("deleteData()", "erasure request dispatched", LogLevel.Info, LogSource.App);
            try
            {
                await ImmutableAudience.DeleteData();
                AppendLog("deleteData()", "backend acknowledged", LogLevel.Ok, LogSource.App);
            }
            catch (Exception ex)
            {
                AppendLog("deleteData()", ex.Message, LogLevel.Err, LogSource.App);
            }
        }

        // ---- SDK action handlers: telemetry ----

        // Prefers the typed overload for the four events with public C#
        // classes (Progression, Resource, Purchase, MilestoneReached); the
        // rest stay on the string overload. Typed validation errors are
        // expected for user input — let them propagate through RunAndLog.
        private void OnSendCatalogueEvent(EventSpec spec, Dictionary<string, VisualElement> inputs) =>
            RunAndLog("track()", () =>
            {
                GuardConsentForTrack();
                var props = BuildPropsDictionary(spec, inputs);
                var typed = BuildTypedEvent(spec.Name, props);
                if (typed != null)
                {
                    ImmutableAudience.Track(typed);
                    return Json.Serialize(new Dictionary<string, object>
                    {
                        ["event"] = spec.Name,
                        ["overload"] = "typed",
                        ["properties"] = typed.ToProperties(),
                    }, 2);
                }

                ImmutableAudience.Track(spec.Name, props.Count > 0 ? props : null);
                return Json.Serialize(new Dictionary<string, object>
                {
                    ["event"] = spec.Name,
                    ["overload"] = "string",
                    ["properties"] = props,
                }, 2);
            });

        // SDK drops via Log.Warn when name is empty or consent is None; that
        // warning surfaces in the pane via Log.Writer, so no sample-side
        // check is needed beyond GuardConsentForTrack.
        private void OnSendCustomEvent() => RunAndLog("track()", () =>
        {
            GuardConsentForTrack();
            var f = CaptureCustomEventForm();
            var props = string.IsNullOrEmpty(f.RawProps) ? null : JsonReader.DeserializeObject(f.RawProps);
            ImmutableAudience.Track(f.Name, props);
            var echo = new Dictionary<string, object> { ["event"] = f.Name };
            if (props != null) echo["properties"] = props;
            return Json.Serialize(echo, 2);
        });

        // ---- SDK action handlers: consent ----

        // None purges the queue + clears the anonymous ID; dropping below Full
        // clears UserId. Mirror is reset whenever the new level can no longer
        // identify.
        private void OnSetConsent(ConsentLevel level) => RunAndLog("setConsent()", () =>
        {
            var previous = ImmutableAudience.CurrentConsent;
            ImmutableAudience.SetConsent(level);
            if (!level.CanIdentify()) ResetIdentityMirror();
            var payload = new Dictionary<string, object>
            {
                ["from"] = previous.ToLowercaseString(),
                ["to"] = level.ToLowercaseString(),
            };
            var effects = new List<string>();
            if (previous == ConsentLevel.None && level != ConsentLevel.None) effects.Add("queue started, session created");
            if (level == ConsentLevel.None) effects.Add("queue purged, anonymous ID cleared");
            if (!level.CanIdentify() && previous.CanIdentify()) effects.Add("userId cleared");
            if (effects.Count > 0) payload["effects"] = effects;
            OnSdkStateChanged();
            return Json.Serialize(payload, 2);
        });

        // ---- SDK action handlers: identity ----

        private void OnIdentify() => RunAndLog("identify()", () =>
        {
            var f = CaptureIdentifyForm();
            var traits = ParseTraits(f.RawTraits);
            ImmutableAudience.Identify(f.Id, ParseIdentityType(f.Type), traits);
            // SDK drops via Log.Warn when id is empty or consent < Full. Mirror
            // only when accepted — otherwise the panel would show stale state.
            var accepted = !string.IsNullOrEmpty(f.Id)
                           && string.Equals(ImmutableAudience.UserId, f.Id, StringComparison.Ordinal);
            if (accepted) { _mirrorIdentityType = f.Type; _mirrorTraits = traits; }
            OnSdkStateChanged();
            var payload = new Dictionary<string, object>
            {
                ["id"]           = f.Id,
                ["identityType"] = f.Type,
                ["accepted"]     = accepted,
            };
            if (traits != null) payload["traits"] = traits;
            return Json.Serialize(payload, 2);
        });

        private void OnIdentifyTraits() => RunAndLog("identify(traits)", () =>
        {
            var userId = ImmutableAudience.UserId;
            if (string.IsNullOrEmpty(userId)) throw new InvalidOperationException("no active identity — call Identify first");
            var traits = ParseTraits(CaptureTraitsUpdate());
            if (traits == null || traits.Count == 0) throw new InvalidOperationException("traits required");
            ImmutableAudience.Identify(userId, ParseIdentityType(_mirrorIdentityType), traits);
            _mirrorTraits = traits;
            OnSdkStateChanged();
            return Json.Serialize(traits, 2);
        });

        private void OnAlias() => RunAndLog("alias()", () =>
        {
            var f = CaptureAliasForm();
            ImmutableAudience.Alias(f.FromId, ParseIdentityType(f.FromType), f.ToId, ParseIdentityType(f.ToType));
            // SDK drops via Log.Warn when fromId/toId is empty or consent < Full.
            // The IsAliasReady gate keeps empty endpoints unreachable from the
            // UI; this post-call check is defense-in-depth.
            var accepted = !string.IsNullOrEmpty(f.FromId) && !string.IsNullOrEmpty(f.ToId);
            if (accepted)
            {
                _mirrorAliases.Add($"{f.FromType}:{f.FromId} → {f.ToType}:{f.ToId}");
                OnSdkStateChanged();
            }
            return Json.Serialize(new Dictionary<string, object>
            {
                ["from"]     = new Dictionary<string, object> { ["id"] = f.FromId, ["identityType"] = f.FromType },
                ["to"]       = new Dictionary<string, object> { ["id"] = f.ToId,   ["identityType"] = f.ToType },
                ["accepted"] = accepted,
            }, 2);
        });

        // ---- SDK callbacks (passed to SDK at Init time) ----

        // Fires from background flush threads; AppendLog marshals to main.
        // Body is JSON for parity with handler "Copy" output.
        private void OnSdkError(AudienceError err) =>
            AppendLog("onError", Json.Serialize(new Dictionary<string, object>
            {
                ["code"] = err.Code.ToString(),
                ["message"] = err.Message,
            }, 2), LogLevel.Err, LogSource.Sdk);

        // SDK Log.Writer adapter. May fire from any thread; AppendLog handles
        // the main-thread marshal.
        private void RouteSdkLogToPane(string msg)
        {
            const string warnTag = "[ImmutableAudience] WARN:";
            const string prefix = "[ImmutableAudience]";
            string body = msg;
            var level = LogLevel.Debug;
            if (msg.StartsWith(warnTag, StringComparison.Ordinal))
            {
                level = LogLevel.Warn;
                body = msg.Substring(warnTag.Length).TrimStart();
            }
            else if (msg.StartsWith(prefix, StringComparison.Ordinal))
            {
                body = msg.Substring(prefix.Length).TrimStart();
            }
            AppendLog("sdk", body, level, LogSource.Sdk);
        }

        // ---- Handler scaffolding ----

        private void RunAndLog(string label, Func<string> body)
        {
            try { AppendLog(label, body(), LogLevel.Ok, LogSource.App); }
            catch (Exception ex)
            {
                var source = ex is InvalidOperationException ? LogSource.App : LogSource.Sdk;
                AppendLog(label, ex.Message, LogLevel.Err, source);
            }
        }

        // Track silently drops when consent < Anonymous (CanTrack false, no
        // SDK log). Throw here so RunAndLog renders an App-tier error row
        // instead of a misleading Ok with a payload that was never queued.
        private static void GuardConsentForTrack()
        {
            var consent = ImmutableAudience.CurrentConsent;
            if (!consent.CanTrack())
                throw new InvalidOperationException(
                    $"track dropped — consent is {consent.ToLowercaseString()}; raise to anonymous or full to queue events");
        }

        // Refresh* are idempotent reads, so calling all four every time is
        // cheaper than tracking subsets per handler.
        private void OnSdkStateChanged()
        {
            RefreshInitState();
            RefreshConsentPills();
            RefreshIdentityPanel();
            RefreshStatusBar();
        }

        // ---- Config builders ----

        // Maps the captured Setup form to AudienceConfig. BaseUrl null → SDK
        // derives the endpoint from the publishable key prefix (test → sandbox,
        // else production). The flushInterval clamp emits a warn row when
        // the user requests <1s.
        private AudienceConfig BuildAudienceConfig(InitForm form, Action<AudienceError> onError)
        {
            var config = new AudienceConfig
            {
                PublishableKey = form.PublishableKey,
                BaseUrl        = string.IsNullOrEmpty(form.BaseUrl) ? null : form.BaseUrl,
                Consent        = form.Consent,
                Debug          = form.Debug,
                OnError        = onError,
            };
            if (form.FlushIntervalMs is int flushMs && flushMs > 0)
            {
                if (flushMs < 1000)
                    AppendLog("INIT", $"flushInterval {flushMs}ms below 1s — clamped", LogLevel.Warn, LogSource.App);
                config.FlushIntervalSeconds = Math.Max(1, flushMs / 1000);
            }
            if (form.FlushSize is int flushSize && flushSize > 0)
                config.FlushSize = flushSize;
            return config;
        }

        // For the Init "Ok" row. Nullable fields omitted when unset;
        // publishableKey redacted.
        private static Dictionary<string, object> BuildConfigEcho(AudienceConfig config)
        {
            var echo = new Dictionary<string, object>
            {
                ["consent"]                = config.Consent.ToString(),
                ["debug"]                  = config.Debug,
                ["flushIntervalSeconds"]   = config.FlushIntervalSeconds,
                ["flushSize"]              = config.FlushSize,
                ["packageVersion"]         = config.PackageVersion,
                ["shutdownFlushTimeoutMs"] = config.ShutdownFlushTimeoutMs,
            };
            if (!string.IsNullOrEmpty(config.PublishableKey))
                echo["publishableKey"] = RedactPublishableKey(config.PublishableKey);
            if (!string.IsNullOrEmpty(config.PersistentDataPath))
                echo["persistentDataPath"] = config.PersistentDataPath;
            return echo;
        }

        // Keeps the pk_imapik-test- / pk_imapik- prefix visible; masks the rest.
        // Caller must guard against null/empty; signature non-nullable so the
        // dictionary insertion in BuildInitConfigEcho doesn't trip CS8601.
        private static string RedactPublishableKey(string key)
        {
            const int PrefixChars = 16;
            const string Mask = "…****";
            return key.Length <= PrefixChars ? Mask : key.Substring(0, PrefixChars) + Mask;
        }

        // ---- Identity helpers ----

        private void ResetIdentityMirror()
        {
            _mirrorIdentityType = null;
            _mirrorTraits = null;
            _mirrorAliases.Clear();
        }

        private static Dictionary<string, object>? ParseTraits(string? raw) =>
            string.IsNullOrWhiteSpace(raw) ? null : JsonReader.DeserializeObject(raw!);

        // Parses a wire-format identity string (e.g. "steam") back into the
        // IdentityType enum the SDK now requires. Falls back to Custom for
        // unknown or empty values.
        private static IdentityType ParseIdentityType(string? value) => (value ?? "").ToLowerInvariant() switch
        {
            "passport" => IdentityType.Passport,
            "steam"    => IdentityType.Steam,
            "epic"     => IdentityType.Epic,
            "google"   => IdentityType.Google,
            "apple"    => IdentityType.Apple,
            "discord"  => IdentityType.Discord,
            "email"    => IdentityType.Email,
            _          => IdentityType.Custom,
        };
    }
}
