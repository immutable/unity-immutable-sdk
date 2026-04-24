using System.Collections.Generic;
using UnityEngine;

namespace Immutable.Audience.Samples.QuickStart
{
    public sealed class AudienceDemo : MonoBehaviour
    {
        [Header("Publishable key")]
        [Tooltip("Your publishable key. Test keys start with pk_imapik-test-.")]
        public string PublishableKey = "pk_imapik-test-REPLACE_ME";

        [Header("Environment")]
        [Tooltip("Which Immutable backend to send events to. Sandbox is the safe default " +
                 "for development; switch to Production explicitly when shipping to live " +
                 "players. Dev is reserved for Immutable engineers.")]
        public AudienceEnvironment Environment = AudienceEnvironment.Sandbox;

        [Header("Starting consent")]
        [Tooltip("Starting consent level. Studios normally collect this from the player.")]
        public ConsentLevel StartingConsent = ConsentLevel.Anonymous;

        [Header("Distribution platform")]
        [Tooltip("Optional — use DistributionPlatforms.Steam / .Epic / .GOG / .Itch / .Standalone for autocomplete, " +
                 "or any custom string. Sent as a property on game_launch. " +
                 "Defaults to Standalone so first-run sample data does not falsely tag every integrator as Steam.")]
        public string DistributionPlatform = DistributionPlatforms.Standalone;

        [Tooltip("Enable ambient [ImmutableAudience] log lines.")]
        public bool DebugLogging = true;

        public void InitSdk()
        {
            ImmutableAudience.Init(new AudienceConfig
            {
                PublishableKey = PublishableKey,
                Environment = Environment,
                Consent = StartingConsent,
                DistributionPlatform = DistributionPlatform,
                Debug = DebugLogging,
                OnError = err => Debug.LogWarning($"[AudienceDemo] SDK error: {err.Code} — {err.Message}"),
            });
        }

        public void ShutdownSdk() => ImmutableAudience.Shutdown();

        // async void swallows exceptions; try/catch routes them through OnError instead.
        public async void FlushNow()
        {
            try
            {
                await ImmutableAudience.FlushAsync();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[AudienceDemo] FlushAsync threw: {ex.Message}");
            }
        }

        public void RequestGdprErasure() => ImmutableAudience.DeleteData();

        public void FireProgressionStart() => ImmutableAudience.Track(new Progression
        {
            Status = ProgressionStatus.Start,
            World = "overworld",
            Level = "stone_age",
        });

        public void FireProgressionComplete() => ImmutableAudience.Track(new Progression
        {
            Status = ProgressionStatus.Complete,
            World = "overworld",
            Level = "stone_age",
            Score = 1500,
            DurationSec = 120f,
        });

        public void FireResourceEarn() => ImmutableAudience.Track(new Resource
        {
            Flow = ResourceFlow.Source,
            Currency = "gold",
            Amount = 100,
            ItemType = "monster_kill",
            ItemId = "zombie",
        });

        public void FireResourceSpend() => ImmutableAudience.Track(new Resource
        {
            Flow = ResourceFlow.Sink,
            Currency = "gold",
            Amount = 50,
            ItemType = "weapon",
            ItemId = "diamond_sword",
        });

        // Production: use the payment provider's stable order id, not a fresh GUID.
        public void FirePurchase() => ImmutableAudience.Track(new Purchase
        {
            Currency = "USD",
            Value = 9.99m,
            ItemId = "skin_pack_knight",
            ItemName = "Knight Skin Pack",
            Quantity = 1,
            TransactionId = System.Guid.NewGuid().ToString(),
        });

        public void FireMilestone() => ImmutableAudience.Track(new MilestoneReached
        {
            Name = "dragon_defeated",
        });

        public void FireCustomEvent() => ImmutableAudience.Track("crafting_started", new Dictionary<string, object>
        {
            ["recipe_id"] = "diamond_sword",
            ["station"] = "crafting_table",
            ["player_level"] = 20,
        });

        public void IdentifyAsSteam() =>
            ImmutableAudience.Identify("76561198012345", IdentityType.Steam);

        public void AliasSteamToPassport() => ImmutableAudience.Alias(
            fromId: "76561198012345", fromType: IdentityType.Steam,
            toId: "user_abc", toType: IdentityType.Passport);

        public void ResetIdentity() => ImmutableAudience.Reset();

        public void ConsentNone() => ImmutableAudience.SetConsent(ConsentLevel.None);
        public void ConsentAnonymous() => ImmutableAudience.SetConsent(ConsentLevel.Anonymous);
        public void ConsentFull() => ImmutableAudience.SetConsent(ConsentLevel.Full);

        private void OnGUI()
        {
            const float padding = 8f;
            const float buttonHeight = 44f;
            const float maxPanelWidth = 800f;
            var panelWidth = Mathf.Min(Screen.width - padding * 2, maxPanelWidth);
            var panelX = (Screen.width - panelWidth) * 0.5f;

            EnsureStyles();

            var init = ImmutableAudience.Initialized;
            var consent = ImmutableAudience.CurrentConsent;
            var canTrack = init && consent != ConsentLevel.None;
            var canIdentify = init && consent == ConsentLevel.Full;

            GUILayout.BeginArea(new Rect(panelX, padding, panelWidth, Screen.height - padding * 2));
            _scroll = GUILayout.BeginScrollView(_scroll);

            GUILayout.Label("Immutable Audience — QuickStart", _titleStyle);
            GUILayout.Label(
                "Press a button to send a sample event. The panel below shows what the SDK is doing. " +
                "Check the Unity Console for log output.",
                _introStyle);

            DrawStatusPanel();

            DrawSection("SDK lifecycle",
                "Start, stop, and flush the SDK. Press Start the SDK first — every " +
                "other button stays disabled until the SDK is initialised.");
            TwoColumnButtons(buttonHeight,
                ("Start the SDK\n(Init)", InitSdk, !init),
                ("Turn off the SDK\n(Shutdown)", ShutdownSdk, init),
                ("Send queued events now\n(FlushAsync)", FlushNow, init));

            DrawSection("Typed events",
                "Standard event types Immutable's dashboards chart automatically: " +
                "player progression, currency in/out, purchases, achievements.");
            TwoColumnButtons(buttonHeight,
                ("Player started a level\n(Progression.Start)", FireProgressionStart, canTrack),
                ("Player finished a level\n(Progression.Complete)", FireProgressionComplete, canTrack),
                ("Player earned currency\n(Resource.Source)", FireResourceEarn, canTrack),
                ("Player spent currency\n(Resource.Sink)", FireResourceSpend, canTrack),
                ("Player made a purchase\n(Purchase)", FirePurchase, canTrack),
                ("Player reached a milestone\n(MilestoneReached)", FireMilestone, canTrack));

            DrawSection("Custom event",
                "Send any event you want. You pick the name and the data — Immutable stores both.");
            TwoColumnButtons(buttonHeight,
                ("Send a custom event\n(Track(\"crafting_started\"))", FireCustomEvent, canTrack));

            DrawSection("Identity",
                "Tell Immutable who's playing. Identify links events to a player. " +
                "Alias merges two accounts into one. Reset clears the link.");
            TwoColumnButtons(buttonHeight,
                ("Identify player by Steam ID\n(Identify)", IdentifyAsSteam, canIdentify),
                ("Link Steam → Passport\n(Alias)", AliasSteamToPassport, canIdentify),
                ("⚠ Forget who's playing\n(Reset)", ResetIdentity, init));

            DrawSection("Privacy consent",
                "What can Immutable track? None: nothing. Anonymous: counts only, no player id. " +
                "Full: keep the player id alongside events.");
            TwoColumnButtons(buttonHeight,
                ("Stop tracking\n(SetConsent(None))", ConsentNone, init && consent != ConsentLevel.None),
                ("Track anonymously\n(SetConsent(Anonymous))", ConsentAnonymous, init && consent != ConsentLevel.Anonymous),
                ("Track with player ID\n(SetConsent(Full))", ConsentFull, init && consent != ConsentLevel.Full));

            DrawSection("Advanced",
                "Danger zone. Deleting a player's data asks Immutable to erase " +
                "everything the backend has stored for that player — GDPR / " +
                "right-to-be-forgotten territory, not recoverable.");
            TwoColumnButtons(buttonHeight,
                ("⚠ Delete this player's data (GDPR)\n(DeleteData)", RequestGdprErasure, init));

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private Vector2 _scroll;

        private static GUIStyle _titleStyle;
        private static GUIStyle _introStyle;
        private static GUIStyle _sectionHeaderStyle;
        private static GUIStyle _sectionDescStyle;
        private static GUIStyle _statusBoxStyle;
        private static GUIStyle _statusLabelStyle;
        private static GUIStyle _statusValueStyle;
        private static GUIStyle _statusValueWarnStyle;
        private static GUIStyle _copyButtonStyle;

        private static void EnsureStyles()
        {
            if (_titleStyle != null) return;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 15,
            };
            _introStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                fontStyle = FontStyle.Italic,
            };
            _sectionHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 13,
                margin = new RectOffset(0, 0, 6, 2),
            };
            _sectionDescStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                fontStyle = FontStyle.Italic,
                margin = new RectOffset(0, 0, 0, 4),
            };
            _statusBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(8, 8, 6, 6),
            };
            _statusLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
            };
            _statusValueStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
            };
            _statusValueWarnStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                normal = { textColor = new Color(1f, 0.55f, 0.4f) },
            };
            _copyButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                padding = new RectOffset(4, 4, 2, 2),
                margin = new RectOffset(4, 0, 2, 0),
            };
        }

        private void DrawSection(string title, string description)
        {
            GUILayout.Label(title, _sectionHeaderStyle);
            GUILayout.Label(description, _sectionDescStyle);
        }

        private void DrawStatusPanel()
        {
            GUILayout.Space(4);
            GUILayout.BeginVertical(_statusBoxStyle);

            GUILayout.Label("SDK status", _sectionHeaderStyle);

            DrawStatusRow("Initialized", ImmutableAudience.Initialized ? "yes" : "no");
            DrawStatusRow("Environment", ImmutableAudience.CurrentEnvironment.ToString());
            DrawStatusRow("Consent", ImmutableAudience.CurrentConsent.ToString());
            DrawStatusRow("Pub key", FormatPublishableKey(out var pubKeyIsWarning), pubKeyIsWarning,
                copyValue: pubKeyIsWarning ? null : PublishableKey);
            DrawStatusRow("User ID", ImmutableAudience.UserId ?? "(none)",
                copyValue: ImmutableAudience.UserId);
            DrawStatusRow("Anon ID", ImmutableAudience.AnonymousId ?? "(none — needs consent above None)",
                copyValue: ImmutableAudience.AnonymousId);
            DrawStatusRow("Session ID", ImmutableAudience.SessionId ?? "(none)",
                copyValue: ImmutableAudience.SessionId);
            DrawStatusRow("Queued", ImmutableAudience.QueueSize.ToString());

            GUILayout.EndVertical();
            GUILayout.Space(4);
        }

        private static void DrawStatusRow(string label, string value, bool warn = false, string copyValue = null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _statusLabelStyle, GUILayout.Width(100));
            GUILayout.Label(value, warn ? _statusValueWarnStyle : _statusValueStyle);
            if (!string.IsNullOrEmpty(copyValue))
            {
                if (GUILayout.Button("Copy", _copyButtonStyle, GUILayout.Width(50)))
                {
                    GUIUtility.systemCopyBuffer = copyValue;
                }
            }
            GUILayout.EndHorizontal();
        }

        private string FormatPublishableKey(out bool isWarning)
        {
            if (string.IsNullOrEmpty(PublishableKey))
            {
                isWarning = true;
                return "⚠ (not set — set in Inspector)";
            }
            if (PublishableKey.EndsWith("REPLACE_ME"))
            {
                isWarning = true;
                return "⚠ " + PublishableKey + " — set your real key in the Inspector";
            }
            isWarning = false;
            return PublishableKey;
        }

        private static void TwoColumnButtons(float buttonHeight, params (string label, System.Action action, bool enabled)[] buttons)
        {
            for (var i = 0; i < buttons.Length; i += 2)
            {
                GUILayout.BeginHorizontal();
                DrawCellButton(buttons[i], buttonHeight);
                if (i + 1 < buttons.Length)
                {
                    DrawCellButton(buttons[i + 1], buttonHeight);
                }
                else
                {
                    GUILayout.Box(GUIContent.none, GUIStyle.none,
                        GUILayout.Height(buttonHeight),
                        GUILayout.ExpandWidth(true));
                }
                GUILayout.EndHorizontal();
            }
        }

        private static void DrawCellButton((string label, System.Action action, bool enabled) button, float buttonHeight)
        {
            var prev = GUI.enabled;
            GUI.enabled = prev && button.enabled;
            if (GUILayout.Button(button.label,
                GUILayout.Height(buttonHeight),
                GUILayout.ExpandWidth(true)))
            {
                button.action();
            }
            GUI.enabled = prev;
        }
    }
}
