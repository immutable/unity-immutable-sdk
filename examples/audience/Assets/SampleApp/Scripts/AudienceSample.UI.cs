#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Immutable.Audience.Samples.SampleApp
{
    // UI partial of AudienceSample — see AudienceSample.cs for the partial layout.
    public sealed partial class AudienceSample
    {
        // ---- Constants & tables ----

        private static readonly ConsentLevel[] ConsentOrder = { ConsentLevel.None, ConsentLevel.Anonymous, ConsentLevel.Full };
        private static readonly string[] ConsentStateClass = { SampleAppUi.Css.StateErr, SampleAppUi.Css.StateWarn, SampleAppUi.Css.StateOk };

        private static readonly (string TabId, string PanelId)[] Tabs =
        {
            (SampleAppUi.Tabs.Setup,       SampleAppUi.Panels.Setup),
            (SampleAppUi.Tabs.Consent,     SampleAppUi.Panels.Consent),
            (SampleAppUi.Tabs.TypedEvents, SampleAppUi.Panels.TypedEvents),
            (SampleAppUi.Tabs.Identity,    SampleAppUi.Panels.Identity),
        };

        private static readonly string[] StateClasses = { SampleAppUi.Css.StateOk, SampleAppUi.Css.StateWarn, SampleAppUi.Css.StateErr, SampleAppUi.Css.Dim };

        private const int CollapseThreshold = 240;
        private const int StatusPollIntervalMs = 500;
        private const float NarrowBreakpointPx = 1024f;

        // Log pane drag-resize bounds and main-column padding tracker.
        private const float LogResizeMinHeight = 120f;
        private const float LogResizeMaxHeight = 1400f;
        private const float MainPaddingBottomPx = 64f;
        // Pixel slack used when deciding whether the log was at the bottom
        // before a content mutation; treated as "at bottom" if within this.
        private const float LogScrollBottomEpsilonPx = 4f;

        // Toast / flash timings.
        private const int CopyButtonRevertMs = 800;
        private const int CopiedFlashDurationMs = 1500;

        // Local-time format for the per-row timestamp (header) and the
        // round-trip ISO format used in the copy-to-clipboard payload.
        private const string LogRowTimestampFormat = "HH:mm:ss.fff";

        // ✓ glyph injected into the runtime Toggle so the checked state
        // shows a tick rather than a bare coloured square.
        private const string DebugToggleTickGlyph = "✓";

        // ---- UI document state ----
        // All fields below are populated by BindElements before any other access.
#pragma warning disable 8618

        private VisualElement _root;

        // ---- Threading capture (for AppendLog's main-thread marshal) ----

        // _logView.schedule.Execute silently drops off-main callers in Unity
        // 2021.3 runtime panels, and SDK OnError fires from background flush
        // threads — so AppendLog uses these to detect off-main and marshal back.
        private System.Threading.SynchronizationContext _mainSyncContext;
        private int _mainThreadId;

        // ---- UXML element fields (Setup tab) ----

        private TextField _publishableKey, _baseUrl, _flushInterval, _flushSize;
        private DropdownField _initialConsent;
        private Toggle _debug;
        private Button _btnInit, _btnPage, _btnFlush, _btnReset, _btnShutdown, _btnDeleteData;

        // ---- UXML element fields (Consent tab) ----

        private Dictionary<ConsentLevel, Button> _consentPills;

        // ---- UXML element fields (Typed events tab) ----

        private VisualElement _typedEventsHost;
        private TextField _customEventName, _customEventProps;
        private Button _btnCustomEvent;

        // ---- UXML element fields (Identity tab) ----

        private Label _identityUserId, _identityIdentityType, _identityTraits, _identityAliases;
        private TextField _identifyId, _identifyTraits, _traitsUpdate, _aliasFromId, _aliasToId;
        private DropdownField _identifyType, _aliasFromType, _aliasToType;
        private Button _btnIdentify, _btnIdentifyTraits, _btnAlias;

        // ---- UXML element fields (Tabs + status bar + header) ----

        private readonly List<Button> _tabButtons = new List<Button>();
        private Label _prodWarning, _sdkVersionLabel;
        private Label _statusEndpoint, _statusConsent, _statusAnon, _statusUser, _statusSession, _statusQueue;

        // ---- UXML element fields (Log pane) ----

        private ScrollView _logView;
        private Label _logCount;

#pragma warning restore 8618

        // ---- UI lifecycle (single entry point called from main's Awake) ----

        // Full UI lifecycle: thread capture, document load, binding, callbacks,
        // initial paint, status-bar polling. Returns silently if LoadUiDocument fails.
        private void InitializeUi()
        {
            // Captured here because AppendLog can fire off-main from SDK OnError.
            _mainSyncContext = System.Threading.SynchronizationContext.Current!;
            _mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            if (!LoadUiDocument()) return;

            BindElements();
            PopulateDropdowns();
            PopulateTypedEventAccordions();
            RegisterHandlers();
            RegisterAccordionToggles();
            RegisterPageLayoutTracking();
            RegisterResponsiveLayout();

            _sdkVersionLabel.text = $"v{Immutable.Audience.Constants.LibraryVersion}";
            OnSdkStateChanged();
            AppendLog(SampleAppUi.LogLabels.Ready, SampleAppUi.Messages.Ready, LogLevel.Info, LogSource.App);

            // Status bar mirrors live SDK state (UserId, SessionId, QueueSize) —
            // poll instead of subscribing because the SDK doesn't expose changes.
            _root.schedule.Execute(RefreshStatusBar).Every(StatusPollIntervalMs);
        }

        // ---- UI document load ----

        // Loads the UXML/USS resources and clones the tree into the panel
        // root. Returns false (and logs) when the visual tree asset is missing.
        private bool LoadUiDocument()
        {
            var doc = GetComponent<UIDocument>() ?? gameObject.AddComponent<UIDocument>();
            if (doc.panelSettings == null)
                doc.panelSettings = Resources.Load<PanelSettings>(SampleAppUi.Resources.PanelSettings);

            var tree = Resources.Load<VisualTreeAsset>(SampleAppUi.Resources.SampleAppTree);
            if (tree == null)
            {
                Debug.LogError("[Audience Sample] missing Resources/AudienceSample.uxml");
                return false;
            }
            doc.visualTreeAsset = tree;
            _root = doc.rootVisualElement;
            // Unity 2021.3 sometimes leaves runtime panel roots sized to
            // content; without this, .root's flex-grow: 1 has no height to
            // grow into and the viewport paints only the sticky header.
            _root.style.flexGrow = 1;

            _root.Clear();
            tree.CloneTree(_root);
            var uss = Resources.Load<StyleSheet>(SampleAppUi.Resources.SampleAppStyleSheet);
            if (uss != null) _root.styleSheets.Add(uss);
            return true;
        }

        // ---- Element binding ----

        private T Require<T>(string name) where T : VisualElement =>
            _root.Q<T>(name) ?? throw new InvalidOperationException(
                $"[Audience Sample] required UI element '{name}' of type {typeof(T).Name} not found");

        private void BindElements()
        {
            _prodWarning      = Require<Label>(SampleAppUi.ProdWarning);
            _sdkVersionLabel  = Require<Label>(SampleAppUi.Layout.SdkVersion);

            _statusEndpoint = Require<Label>(SampleAppUi.StatusBar.Endpoint);
            _statusConsent  = Require<Label>(SampleAppUi.StatusBar.Consent);
            _statusAnon     = Require<Label>(SampleAppUi.StatusBar.Anon);
            _statusUser     = Require<Label>(SampleAppUi.StatusBar.User);
            _statusSession  = Require<Label>(SampleAppUi.StatusBar.Session);
            _statusQueue    = Require<Label>(SampleAppUi.StatusBar.Queue);

            foreach (var (tabId, _) in Tabs) _tabButtons.Add(Require<Button>(tabId));

            _publishableKey = Require<TextField>(SampleAppUi.Setup.PublishableKey);
            _baseUrl        = Require<TextField>(SampleAppUi.Setup.BaseUrl);
            _initialConsent = Require<DropdownField>(SampleAppUi.Setup.InitialConsent);
            _debug          = Require<Toggle>(SampleAppUi.Setup.Debug);
            // Inject a tick Label — Unity 2021.3 runtime panels render the
            // checked state as a plain coloured square otherwise. USS hides
            // the tick when unchecked.
            var debugCheckmark = _debug.Q<VisualElement>(className: SampleAppUi.Css.UnityToggleCheckmark);
            if (debugCheckmark != null)
            {
                var tick = new Label(DebugToggleTickGlyph);
                tick.AddToClassList(SampleAppUi.Css.DebugTick);
                tick.pickingMode = PickingMode.Ignore;
                debugCheckmark.Add(tick);
            }
            _flushInterval = Require<TextField>(SampleAppUi.Setup.FlushInterval);
            _flushSize     = Require<TextField>(SampleAppUi.Setup.FlushSize);
            _btnInit       = Require<Button>(SampleAppUi.Buttons.Init);
            _btnPage       = Require<Button>(SampleAppUi.Buttons.Page);
            _btnFlush      = Require<Button>(SampleAppUi.Buttons.Flush);
            _btnReset      = Require<Button>(SampleAppUi.Buttons.Reset);
            _btnShutdown   = Require<Button>(SampleAppUi.Buttons.Shutdown);
            _btnDeleteData = Require<Button>(SampleAppUi.Buttons.DeleteData);

            _consentPills = new Dictionary<ConsentLevel, Button>
            {
                [ConsentLevel.None]      = Require<Button>(SampleAppUi.Buttons.ConsentNone),
                [ConsentLevel.Anonymous] = Require<Button>(SampleAppUi.Buttons.ConsentAnon),
                [ConsentLevel.Full]      = Require<Button>(SampleAppUi.Buttons.ConsentFull),
            };

            _typedEventsHost  = Require<VisualElement>(SampleAppUi.Layout.TypedEventsHost);
            _customEventName  = Require<TextField>(SampleAppUi.CustomEvent.Name);
            _customEventProps = Require<TextField>(SampleAppUi.CustomEvent.Props);
            _btnCustomEvent   = Require<Button>(SampleAppUi.Buttons.CustomEvent);

            _identityUserId       = Require<Label>(SampleAppUi.IdentityPanel.UserId);
            _identityIdentityType = Require<Label>(SampleAppUi.IdentityPanel.IdentityType);
            _identityTraits       = Require<Label>(SampleAppUi.IdentityPanel.Traits);
            _identityAliases      = Require<Label>(SampleAppUi.IdentityPanel.Aliases);
            _identifyId           = Require<TextField>(SampleAppUi.IdentityFields.Id);
            _identifyType         = Require<DropdownField>(SampleAppUi.IdentityFields.Type);
            _identifyTraits       = Require<TextField>(SampleAppUi.IdentityFields.Traits);
            _btnIdentify          = Require<Button>(SampleAppUi.Buttons.Identify);
            _traitsUpdate         = Require<TextField>(SampleAppUi.IdentityFields.TraitsUpdate);
            _btnIdentifyTraits    = Require<Button>(SampleAppUi.Buttons.IdentifyTraits);
            _aliasFromId          = Require<TextField>(SampleAppUi.IdentityFields.AliasFromId);
            _aliasFromType        = Require<DropdownField>(SampleAppUi.IdentityFields.AliasFromType);
            _aliasToId            = Require<TextField>(SampleAppUi.IdentityFields.AliasToId);
            _aliasToType          = Require<DropdownField>(SampleAppUi.IdentityFields.AliasToType);
            _btnAlias             = Require<Button>(SampleAppUi.Buttons.Alias);

            _logView = Require<ScrollView>(SampleAppUi.LogScrollView);
            _logView.mode = ScrollViewMode.Vertical;

            // USS selector for this property doesn't match in Unity 2021.3
            // runtime panels; applying via runtime API. Without flex-shrink:
            // 0 the content-container compresses to viewport height and the
            // scroller never engages.
            _logView.contentContainer.style.flexShrink = 0;

            _logCount = Require<Label>(SampleAppUi.Layout.LogCount);
        }

        private void PopulateDropdowns()
        {
            _initialConsent.choices = ConsentOrder.Select(c => c.ToLowercaseString()).ToList();
            // Default Anonymous, not None. None silently drops every Track
            // (SDK gate, no log) — first-time users see Ok rows but nothing
            // queued. Anonymous lets the queue fill so Flush is observable.
            _initialConsent.index = Array.IndexOf(ConsentOrder, ConsentLevel.Anonymous);
            var identityTypes = System.Enum.GetValues(typeof(IdentityType))
                .Cast<IdentityType>()
                .Select(t => t.ToLowercaseString())
                .ToList();
            foreach (var d in new[] { _identifyType, _aliasFromType, _aliasToType }) { d.choices = identityTypes; d.index = 0; }
        }

        // ---- Callback registration ----

        // Subscriptions are panel-scoped; GameObject destruction releases them,
        // so no matching UnregisterCallback is needed.
        private void RegisterHandlers()
        {
            // Log pane drag-resize — UI Toolkit has no `resize: vertical`.
            var logHandle = Require<VisualElement>(SampleAppUi.Layout.LogResizeHandle);
            float dragStartY = 0, dragStartH = 0;
            logHandle.RegisterCallback<PointerDownEvent>(evt =>
            {
                logHandle.CapturePointer(evt.pointerId);
                dragStartY = evt.position.y;
                dragStartH = _logView.resolvedStyle.height;
            });
            logHandle.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!logHandle.HasPointerCapture(evt.pointerId)) return;
                // Snapshot "was at bottom?" before the resize so we re-pin
                // once the new viewport height settles the scrollable rect.
                var s = _logView.verticalScroller;
                bool wasAtBottom = s.highValue <= 0 || (s.highValue - s.value) < LogScrollBottomEpsilonPx;
                var h = Mathf.Clamp(dragStartH + evt.position.y - dragStartY, LogResizeMinHeight, LogResizeMaxHeight);
                _logView.style.height = h;
                _logView.style.minHeight = h;
                _logView.style.maxHeight = h;
                if (wasAtBottom)
                    _logView.schedule.Execute(() =>
                        _logView.scrollOffset = new Vector2(_logView.scrollOffset.x, float.MaxValue)
                    ).StartingIn(0);
            });
            logHandle.RegisterCallback<PointerUpEvent>(evt => logHandle.ReleasePointer(evt.pointerId));

            foreach (var field in new[] {
                _publishableKey, _baseUrl, _flushInterval, _flushSize,
                _customEventName, _customEventProps,
                _identifyId, _identifyTraits, _traitsUpdate,
            }) RegisterPlaceholder(field);

            _publishableKey.RegisterValueChangedCallback(_ => { _btnInit.SetEnabled(!_initialised && !string.IsNullOrWhiteSpace(_publishableKey.value)); RefreshStatusBar(); });
            _initialConsent.RegisterValueChangedCallback(_ => RefreshStatusBar());
            _baseUrl.RegisterValueChangedCallback(_ => RefreshStatusBar());

            _btnInit.clicked += OnInit;
            _btnPage.clicked += OnPage;
            _btnFlush.clicked += async () => await OnFlushAsync();
            _btnReset.clicked += OnReset;
            _btnShutdown.clicked += OnShutdown;
            _btnDeleteData.clicked += async () => await OnDeleteDataAsync();
            _btnIdentify.clicked += OnIdentify;
            _btnIdentifyTraits.clicked += OnIdentifyTraits;
            _btnAlias.clicked += OnAlias;
            _btnCustomEvent.clicked += OnSendCustomEvent;

            var btnCopyLog = Require<Button>(SampleAppUi.Buttons.CopyLog);
            btnCopyLog.clicked += () =>
            {
                var sb = new StringBuilder();
                foreach (var child in _logView.contentContainer.Children())
                    if (child.userData is LogEntry e)
                        sb.Append(FormatLogEntry(e, singleLine: true)).Append('\n');
                GUIUtility.systemCopyBuffer = sb.ToString();
                btnCopyLog.text = SampleAppUi.ButtonText.Copied;
                btnCopyLog.schedule.Execute(() => btnCopyLog.text = SampleAppUi.ButtonText.Copy).StartingIn(CopyButtonRevertMs);
            };
            Require<Button>(SampleAppUi.Buttons.ClearLog).clicked += () => { _logView.Clear(); _logCount.text = "0"; };

            foreach (var pill in _consentPills) pill.Value.clicked += () => OnSetConsent(pill.Key);

            foreach (var tab in _tabButtons)
            {
                var panelId = Tabs.First(t => t.TabId == tab.name).PanelId;
                tab.clicked += () => ActivateTab(panelId, moveFocus: false);
            }
            Require<VisualElement>(SampleAppUi.Layout.TabBar).RegisterCallback<KeyDownEvent>(evt =>
            {
                int current = -1;
                for (int i = 0; i < _tabButtons.Count; i++)
                    if (_tabButtons[i].focusController?.focusedElement == _tabButtons[i]) { current = i; break; }
                if (current == -1) return;
                int next = evt.keyCode switch
                {
                    KeyCode.RightArrow or KeyCode.DownArrow => (current + 1) % _tabButtons.Count,
                    KeyCode.LeftArrow  or KeyCode.UpArrow   => (current - 1 + _tabButtons.Count) % _tabButtons.Count,
                    KeyCode.Home                             => 0,
                    KeyCode.End                              => _tabButtons.Count - 1,
                    _                                        => -1,
                };
                if (next == -1) return;
                evt.StopPropagation(); evt.PreventDefault();
                ActivateTab(Tabs.First(t => t.TabId == _tabButtons[next].name).PanelId, moveFocus: true);
            }, TrickleDown.TrickleDown);

            foreach (var field in new INotifyValueChanged<string>[] { _aliasFromId, _aliasToId, _aliasFromType, _aliasToType })
                field.RegisterValueChangedCallback(_ => _btnAlias.SetEnabled(_initialised && IsAliasReady()));

            foreach (var label in _root.Query<Label>(className: SampleAppUi.Css.StatusValue).ToList())
                label.RegisterCallback<ClickEvent>(_ =>
                {
                    // Ignore re-clicks during the flash — label.text is
                    // "Copied!" right now, so re-copying would put that
                    // on the clipboard.
                    if (label.ClassListContains(SampleAppUi.Css.Copied)) return;
                    if (string.IsNullOrEmpty(label.text) || label.text == SampleAppUi.StatusBar.EmptyText) return;
                    GUIUtility.systemCopyBuffer = label.text;
                    label.text = "Copied!";
                    FlashCopied(label);
                });
        }

        // Placeholder fades when the host carries .has-value or .is-focused (USS).
        private static void RegisterPlaceholder(TextField? field)
        {
            if (field == null) return;
            var host = field.parent;
            if (host == null || !host.ClassListContains(SampleAppUi.Css.PlaceholderHost)) return;
            var placeholder = host.Q<Label>(className: SampleAppUi.Css.FieldPlaceholder);
            if (placeholder != null) placeholder.pickingMode = PickingMode.Ignore;
            void Refresh() => host.EnableInClassList(SampleAppUi.Css.HasValue, !string.IsNullOrEmpty(field.value));
            field.RegisterValueChangedCallback(_ => Refresh());
            field.RegisterCallback<FocusInEvent>(_ => host.AddToClassList(SampleAppUi.Css.IsFocused));
            field.RegisterCallback<FocusOutEvent>(_ => host.RemoveFromClassList(SampleAppUi.Css.IsFocused));
            Refresh();
        }

        // Click + Enter/Space toggle .open on every .accordion-header in the tree.
        private void RegisterAccordionToggles()
        {
            foreach (var header in _root.Query<VisualElement>(className: SampleAppUi.Css.AccordionHeader).ToList())
            {
                var item = header.parent;
                var arrow = header.Q<Label>(className: SampleAppUi.Css.AccordionArrow);
                void Toggle()
                {
                    var nowOpen = !item.ClassListContains(SampleAppUi.Css.Open);
                    item.EnableInClassList(SampleAppUi.Css.Open, nowOpen);
                    if (arrow != null) arrow.text = nowOpen ? "▾" : "▸";
                }
                header.RegisterCallback<ClickEvent>(evt => { if (!(evt.target is Button)) Toggle(); });
                header.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter && evt.keyCode != KeyCode.Space) return;
                    evt.StopPropagation(); evt.PreventDefault(); Toggle();
                });
            }
        }

        // Pins the page-scroll's content-container to the taller of
        // controls/log column plus .main's bottom padding. Without this,
        // switching tabs resizes the controls column and the page tracks
        // it — visible layout jump.
        private void RegisterPageLayoutTracking()
        {
            var pageScroll = Require<ScrollView>(SampleAppUi.Layout.PageScroll);
            var controls   = Require<VisualElement>(SampleAppUi.Layout.ControlsColumn);
            var logCol     = Require<VisualElement>(SampleAppUi.Layout.LogColumn);
            void Update()
            {
                var needed = Mathf.Max(controls.layout.height, logCol.layout.height) + MainPaddingBottomPx;
                if (float.IsNaN(needed) || needed <= 0f) return;
                pageScroll.contentContainer.style.minHeight = needed;
            }
            controls.RegisterCallback<GeometryChangedEvent>(_ => Update());
            logCol.RegisterCallback<GeometryChangedEvent>(_ => Update());
            pageScroll.contentContainer.schedule.Execute(Update).StartingIn(0);
        }

        // Mirrors the web sample's `@media (min-width: 1024px)`. USS 2021.3
        // has no @media; toggle a .narrow class on .sample-app-grid via
        // GeometryChangedEvent. Idempotent — only mutates on the boolean flip.
        private void RegisterResponsiveLayout()
        {
            var grid = Require<VisualElement>(SampleAppUi.Layout.SampleAppGrid);
            void Update()
            {
                var w = _root.layout.width;
                if (float.IsNaN(w) || w <= 0f) return;
                var shouldBeNarrow = w < NarrowBreakpointPx;
                var isNarrow = grid.ClassListContains(SampleAppUi.Css.Narrow);
                if (shouldBeNarrow == isNarrow) return;
                if (shouldBeNarrow) grid.AddToClassList(SampleAppUi.Css.Narrow);
                else grid.RemoveFromClassList(SampleAppUi.Css.Narrow);
            }
            _root.RegisterCallback<GeometryChangedEvent>(_ => Update());
            _root.schedule.Execute(Update).StartingIn(0);
        }

        // ---- Typed-events accordion construction ----

        // Each accordion's Send button captures its EventSpec + input dictionary
        // and routes the click to OnSendCatalogueEvent (in main partial).
        private void PopulateTypedEventAccordions()
        {
            _typedEventsHost.Clear();
            var template = Resources.Load<VisualTreeAsset>(SampleAppUi.Resources.AccordionTemplate);
            if (template == null) { Debug.LogError("[Audience Sample] missing Resources/Templates/Accordion.uxml"); return; }

            foreach (var spec in Catalogue)
            {
                var item = template.Instantiate().Q<VisualElement>(SampleAppUi.Layout.AccordionItem);
                item.Q<Label>(SampleAppUi.Layout.AccordionTitle).text = spec.Name.ToUpperInvariant();
                var content = item.Q<VisualElement>(SampleAppUi.Layout.AccordionContent);

                var inputs = new Dictionary<string, VisualElement>();
                foreach (var field in spec.Fields)
                {
                    VisualElement input;
                    if (field.Kind == FieldKind.Enum)
                    {
                        var dd = new DropdownField();
                        dd.choices = (field.Optional ? new[] { OptionalEnumSentinel } : Array.Empty<string>())
                            .Concat(field.EnumValues).ToList();
                        dd.index = 0;
                        input = dd;
                    }
                    else input = new TextField();
                    input.name = $"typed-{spec.Name.Replace('_', '-')}-{field.Key.ToLowerInvariant().Replace('_', '-')}";

                    var row = new VisualElement();
                    row.AddToClassList(SampleAppUi.Css.Field);
                    var typeHint = field.EnumValues != null ? string.Join(" | ", field.EnumValues) : field.Kind.ToString().ToLowerInvariant();
                    var label = new Label((field.Key + (field.Optional ? "?" : "") + ": " + typeHint).ToUpperInvariant());
                    label.AddToClassList(SampleAppUi.Css.FieldLabel);
                    row.Add(label);
                    row.Add(input);
                    content.Add(row);
                    inputs[field.Key] = input;
                }

                var actions = new VisualElement();
                actions.AddToClassList(SampleAppUi.Css.Actions);
                actions.AddToClassList(SampleAppUi.Css.Last);
                var send = new Button { text = SampleAppUi.ButtonText.Send };
                send.name = $"btn-typed-{spec.Name.Replace('_', '-')}";
                send.SetEnabled(false);
                var capturedSpec = spec;
                send.clicked += () => OnSendCatalogueEvent(capturedSpec, inputs);
                actions.Add(send);
                content.Add(actions);

                _typedEventsHost.Add(item);
            }
        }

        // ---- Tab activation ----

        private void ActivateTab(string panelId, bool moveFocus)
        {
            foreach (var tab in _tabButtons)
            {
                bool isActive = Tabs.First(t => t.TabId == tab.name).PanelId == panelId;
                tab.EnableInClassList(SampleAppUi.ActiveClass, isActive);
                tab.tabIndex = isActive ? 0 : -1;
                if (isActive && moveFocus) tab.Focus();
            }
            foreach (var (_, pId) in Tabs)
                Require<VisualElement>(pId).EnableInClassList(SampleAppUi.ActiveClass, pId == panelId);
        }

        // ---- Refresh / state-driven re-render ----

        private void RefreshStatusBar()
        {
            var key = (_publishableKey.value ?? "").Trim();
            var overrideUrl = (_baseUrl?.value ?? "").Trim();
            bool keyEmpty = string.IsNullOrEmpty(key);
            bool isTest = !keyEmpty && IsTestKey(key);
            bool hasOverride = !string.IsNullOrEmpty(overrideUrl);
            // BaseUrl override skips prefix-based routing, so the prod-warning
            // rule no longer applies (studio is in explicit-target mode).
            string? derivedFromKey = keyEmpty ? null : (isTest ? Constants.SandboxBaseUrl : Constants.ProductionBaseUrl);
            string? endpoint = hasOverride ? overrideUrl : derivedFromKey;
            bool warnState = hasOverride || (!keyEmpty && !isTest);
            SetStatusCell(_statusEndpoint, endpoint, warnState ? SampleAppUi.Css.StateWarn : SampleAppUi.Css.StateOk);
            _prodWarning.EnableInClassList(SampleAppUi.HiddenClass, hasOverride || keyEmpty || isTest);

            var consent = _initialised ? ImmutableAudience.CurrentConsent : ConsentOrder[Mathf.Clamp(_initialConsent?.index ?? 0, 0, ConsentOrder.Length - 1)];
            int cIdx = Array.IndexOf(ConsentOrder, consent);
            SetStatusCell(_statusConsent, consent.ToLowercaseString(), cIdx >= 0 ? ConsentStateClass[cIdx] : SampleAppUi.Css.Dim);

            SetStatusCell(_statusAnon,    _initialised ? ImmutableAudience.AnonymousId : null, SampleAppUi.Css.Dim);
            SetStatusCell(_statusUser,    _initialised ? ImmutableAudience.UserId      : null, SampleAppUi.Css.Dim);
            SetStatusCell(_statusSession, _initialised ? ImmutableAudience.SessionId   : null, SampleAppUi.Css.Dim);
            SetStatusCell(_statusQueue,   _initialised ? ImmutableAudience.QueueSize.ToString(CultureInfo.InvariantCulture) : null, SampleAppUi.Css.Dim);
        }

        private void RefreshConsentPills()
        {
            foreach (var kvp in _consentPills)
                kvp.Value.EnableInClassList(SampleAppUi.ActiveClass, _initialised && ImmutableAudience.CurrentConsent == kvp.Key);
        }

        private void RefreshInitState()
        {
            foreach (var b in new[] { _btnPage, _btnFlush, _btnReset, _btnShutdown, _btnDeleteData, _btnCustomEvent, _btnIdentify, _btnIdentifyTraits })
                b.SetEnabled(_initialised);
            foreach (var p in _consentPills.Values) p.SetEnabled(_initialised);
            foreach (var btn in _typedEventsHost.Query<Button>().ToList()) btn.SetEnabled(_initialised);
            _btnInit.SetEnabled(!_initialised && !string.IsNullOrWhiteSpace(_publishableKey.value));
            _btnAlias.SetEnabled(_initialised && IsAliasReady());
        }

        private void RefreshIdentityPanel()
        {
            _identityUserId.text       = ImmutableAudience.UserId ?? SampleAppUi.StatusBar.EmptyText;
            _identityIdentityType.text = _mirrorIdentityType ?? SampleAppUi.StatusBar.EmptyText;
            _identityTraits.text       = _mirrorTraits != null ? Json.Serialize(_mirrorTraits, 2) : SampleAppUi.StatusBar.EmptyText;
            _identityAliases.text      = _mirrorAliases.Count == 0 ? SampleAppUi.StatusBar.EmptyText : string.Join("\n", _mirrorAliases);
        }

        // ---- Form capture (UXML state → DTOs handed to main) ----

        // Snapshot of the Setup tab. Captured on click so main can build the
        // AudienceConfig without reading UXML directly.
        internal readonly struct InitForm
        {
            public readonly string PublishableKey;
            public readonly string BaseUrl;
            public readonly ConsentLevel Consent;
            public readonly bool Debug;
            public readonly int? FlushIntervalMs;
            public readonly int? FlushSize;

            public InitForm(string publishableKey, string baseUrl, ConsentLevel consent, bool debug, int? flushIntervalMs, int? flushSize)
            {
                PublishableKey = publishableKey;
                BaseUrl = baseUrl;
                Consent = consent;
                Debug = debug;
                FlushIntervalMs = flushIntervalMs;
                FlushSize = flushSize;
            }
        }

        internal InitForm CaptureInitForm()
        {
            var consentIdx = Mathf.Clamp(_initialConsent.index, 0, ConsentOrder.Length - 1);
            int? flushIntervalMs = int.TryParse((_flushInterval.value ?? "").Trim(), out var ms) && ms > 0 ? ms : (int?)null;
            int? flushSize = int.TryParse((_flushSize.value ?? "").Trim(), out var size) && size > 0 ? size : (int?)null;
            return new InitForm(
                publishableKey:  (_publishableKey.value ?? "").Trim(),
                baseUrl:         (_baseUrl.value ?? "").Trim(),
                consent:         ConsentOrder[consentIdx],
                debug:           _debug.value,
                flushIntervalMs: flushIntervalMs,
                flushSize:       flushSize);
        }

        // Snapshot of the identify form on the Identity tab.
        internal readonly struct IdentifyForm
        {
            public readonly string Id;
            public readonly string Type;
            public readonly string RawTraits;
            public IdentifyForm(string id, string type, string rawTraits)
            { Id = id; Type = type; RawTraits = rawTraits; }
        }

        internal IdentifyForm CaptureIdentifyForm() => new IdentifyForm(
            id:        (_identifyId.value ?? "").Trim(),
            type:      _identifyType.value ?? IdentityType.Custom.ToLowercaseString(),
            rawTraits: _identifyTraits.value);

        // Single value — no wrapper struct.
        internal string CaptureTraitsUpdate() => _traitsUpdate.value;

        // Snapshot of the alias form on the Identity tab.
        internal readonly struct AliasForm
        {
            public readonly string FromId;
            public readonly string FromType;
            public readonly string ToId;
            public readonly string ToType;
            public AliasForm(string fromId, string fromType, string toId, string toType)
            { FromId = fromId; FromType = fromType; ToId = toId; ToType = toType; }
        }

        internal AliasForm CaptureAliasForm()
        {
            var defaultType = IdentityType.Custom.ToLowercaseString();
            return new AliasForm(
                fromId:   (_aliasFromId.value ?? "").Trim(),
                fromType: _aliasFromType.value ?? defaultType,
                toId:     (_aliasToId.value ?? "").Trim(),
                toType:   _aliasToType.value ?? defaultType);
        }

        // Snapshot of the custom-event submit form.
        internal readonly struct CustomEventForm
        {
            public readonly string Name;
            public readonly string RawProps;
            public CustomEventForm(string name, string rawProps) { Name = name; RawProps = rawProps; }
        }

        internal CustomEventForm CaptureCustomEventForm() => new CustomEventForm(
            name:     (_customEventName.value ?? "").Trim(),
            rawProps: (_customEventProps.value ?? "").Trim());

        // ---- UI helpers ----

        private static void SetStatusCell(Label label, string? value, string stateClass)
        {
            // Don't overwrite a label mid-flash — without this the next
            // RefreshStatusBar poll tick would wipe "Copied!". FlashCopied
            // removes .copied when the flash window ends.
            if (label.ClassListContains(SampleAppUi.Css.Copied)) return;
            bool empty = string.IsNullOrEmpty(value);
            label.text = empty ? SampleAppUi.StatusBar.EmptyText : value;
            foreach (var c in StateClasses) label.RemoveFromClassList(c);
            label.AddToClassList(empty ? SampleAppUi.Css.Dim : stateClass);
        }

        private bool IsAliasReady()
        {
            var fromId = (_aliasFromId.value ?? "").Trim();
            var toId   = (_aliasToId.value ?? "").Trim();
            return !string.IsNullOrEmpty(fromId) && !string.IsNullOrEmpty(toId)
                && (fromId != toId || (_aliasFromType.value ?? "") != (_aliasToType.value ?? ""));
        }

        private static bool IsTestKey(string? key) =>
            !string.IsNullOrEmpty(key) && key!.StartsWith(Constants.TestKeyPrefix, StringComparison.Ordinal);

        private static void FlashCopied(VisualElement ve)
        {
            ve.AddToClassList(SampleAppUi.Css.Copied);
            ve.schedule.Execute(() => ve.RemoveFromClassList(SampleAppUi.Css.Copied)).StartingIn(CopiedFlashDurationMs);
        }

        // ---- Log pane mechanics (types) ----

        internal enum LogLevel { Info, Ok, Warn, Err, Debug }
        internal enum LogSource { App, Sdk }

        internal readonly struct LogEntry
        {
            public readonly DateTime Timestamp;
            public readonly string Label;
            public readonly string Body;
            public readonly LogLevel Level;
            public readonly LogSource Source;
            public LogEntry(DateTime ts, string? label, string? body, LogLevel level, LogSource source)
            { Timestamp = ts; Label = label ?? ""; Body = body ?? ""; Level = level; Source = source; }
        }

        // ---- Log pane mechanics (append + render) ----

        // Append a row. Marshals to main when invoked off it — Unity 2021.3
        // runtime panels can't be mutated from background threads.
        private void AppendLog(string label, string? body, LogLevel level, LogSource source)
        {
            if (System.Threading.Thread.CurrentThread.ManagedThreadId != _mainThreadId)
            {
                _mainSyncContext.Post(_ => AppendLog(label, body, level, source), null);
                return;
            }

            // Snapshot "was at bottom?" before adding — stateless per-row check.
            var s = _logView.verticalScroller;
            bool wasAtBottom = s.highValue <= 0 || (s.highValue - s.value) < LogScrollBottomEpsilonPx;

            var row = BuildLogRow(new LogEntry(DateTime.Now, label, body, level, source));
            _logView.Add(row);
            _logCount.text = _logView.contentContainer.childCount.ToString(CultureInfo.InvariantCulture);

            // contentContainer.flexShrink = 0 (set in BindElements) makes the
            // scrollable range update synchronously; one tick is enough for
            // the scrollOffset clamp to land on the new bottom.
            if (wasAtBottom)
                _logView.schedule.Execute(() =>
                    _logView.scrollOffset = new Vector2(_logView.scrollOffset.x, float.MaxValue)
                ).StartingIn(0);
        }

        // Builds a row WITHOUT adding to _logView; caller does the Add so it
        // can register a one-shot GeometryChangedEvent before the first
        // layout pass fires.
        private VisualElement BuildLogRow(LogEntry entry)
        {
            var row = new VisualElement();
            // Stash the entry so the "Copy log" button can iterate visual
            // children and reconstruct the clipboard payload — single source
            // of truth for log data is the visual tree.
            row.userData = entry;
            row.AddToClassList(SampleAppUi.Css.LogRow);
            row.AddToClassList(SampleAppUi.Css.LogLevelPrefix + entry.Level.ToString().ToLowerInvariant());

            var head = new VisualElement();
            head.AddToClassList(SampleAppUi.Css.LogRowHead);

            var ts = new Label(entry.Timestamp.ToString(LogRowTimestampFormat, CultureInfo.InvariantCulture));
            ts.AddToClassList(SampleAppUi.Css.LogTs);
            head.Add(ts);

            var badge = new Label(entry.Source == LogSource.Sdk ? SampleAppUi.LogBadgeText.Sdk : SampleAppUi.LogBadgeText.App);
            badge.AddToClassList(SampleAppUi.Css.LogBadge);
            badge.AddToClassList(entry.Source == LogSource.Sdk ? SampleAppUi.Css.BadgeSdk : SampleAppUi.Css.BadgeApp);
            head.Add(badge);

            var label = new Label(entry.Label);
            label.AddToClassList(SampleAppUi.Css.LogLabel);
            head.Add(label);

            // Two overlapping 10×10 squares drawn entirely via USS borders —
            // font-independent, unlike a Unicode glyph which Roboto-Regular
            // doesn't reliably carry past the basic Latin range.
            var copy = new Button();
            copy.AddToClassList(SampleAppUi.Css.LogCopy);
            var copyBack = new VisualElement();
            copyBack.AddToClassList(SampleAppUi.Css.LogCopyBack);
            copyBack.pickingMode = PickingMode.Ignore;
            var copyFront = new VisualElement();
            copyFront.AddToClassList(SampleAppUi.Css.LogCopyFront);
            copyFront.pickingMode = PickingMode.Ignore;
            copy.Add(copyBack);
            copy.Add(copyFront);
            copy.clicked += () => { GUIUtility.systemCopyBuffer = FormatLogEntry(entry, singleLine: false); FlashCopied(copy); };
            head.Add(copy);
            row.Add(head);

            if (!string.IsNullOrEmpty(entry.Body))
            {
                var bodyLabel = new Label(entry.Body);
                bodyLabel.AddToClassList(SampleAppUi.Css.LogBody);
                row.Add(bodyLabel);
                if (entry.Body.Length > CollapseThreshold) row.AddToClassList(SampleAppUi.Css.Collapsed);
                head.RegisterCallback<ClickEvent>(evt =>
                {
                    if (evt.target is Button) return;
                    row.EnableInClassList(SampleAppUi.Css.Collapsed, !row.ClassListContains(SampleAppUi.Css.Collapsed));
                });
            }
            return row;
        }

        private static string FormatLogEntry(LogEntry entry, bool singleLine)
        {
            var sb = new StringBuilder()
                .Append(entry.Timestamp.ToString(Constants.IsoTimestampFormat, CultureInfo.InvariantCulture))
                .Append(" [").Append(entry.Source == LogSource.Sdk ? SampleAppUi.LogBadgeText.Sdk : SampleAppUi.LogBadgeText.App).Append("] ")
                .Append(entry.Label);
            if (!string.IsNullOrEmpty(entry.Body))
                sb.Append(singleLine ? ' ' : '\n').Append(entry.Body);
            return sb.ToString();
        }
    }
}
