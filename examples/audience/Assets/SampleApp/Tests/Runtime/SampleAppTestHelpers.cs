#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Immutable.Audience.Samples.SampleApp.Tests
{
    // Test-only utilities for inspecting and driving the sample app's UI.
    // Mirrors the LogEntry struct shape from AudienceSample.UI.cs and queries
    // the log pane via the userData stash on each log row.
    internal static class SampleAppTestHelpers
    {
        // Polls predicate once per frame until it returns true or the deadline
        // elapses. Calls Assert.Fail with description when the deadline is hit.
        // Use this instead of WaitForSecondsRealtime when a test is waiting
        // "at most N seconds for X to become true" — the polling exits as soon
        // as the condition is satisfied rather than burning the full N seconds.
        internal static IEnumerator WaitForCondition(
            Func<bool> predicate, float timeoutSeconds, string description)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (predicate()) yield break;
                yield return null;
            }
            Assert.Fail($"Timed out after {timeoutSeconds:F1}s waiting for: {description}");
        }

        // Wait until the log pane contains an entry whose label matches `label`
        // and whose level matches `level`. Yields one frame per check.
        // Throws TimeoutException on deadline.
        internal static IEnumerator WaitForLogEntry(
            VisualElement root, string label, int level, float timeoutSec)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSec;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (HasLogEntry(root, label, level))
                    yield break;
                yield return null;
            }
            throw new TimeoutException(
                $"Log entry not found within {timeoutSec}s. " +
                $"Looking for label='{label}', level={level}. " +
                $"Current entries: {DescribeLogEntries(root)}");
        }

        internal static bool HasLogEntry(VisualElement root, string label, int level)
        {
            foreach (var entry in EnumerateLogEntries(root))
            {
                if (entry.LabelMatches(label) && entry.LevelEquals(level))
                    return true;
            }
            return false;
        }

        internal static int CountLogEntriesAtLevel(VisualElement root, int level)
        {
            var n = 0;
            foreach (var entry in EnumerateLogEntries(root))
                if (entry.LevelEquals(level)) n++;
            return n;
        }

        internal static string DescribeLogEntries(VisualElement root)
        {
            var entries = EnumerateLogEntries(root).ToList();
            if (entries.Count == 0) return "(none)";
            return string.Join(" | ", entries.Select(e =>
            {
                var body = e.Body;
                return string.IsNullOrEmpty(body) ? $"{e.Label}@{e.Level}" : $"{e.Label}@{e.Level}: {body}";
            }));
        }

        // The log pane stashes an opaque LogEntry on each row's userData.
        // Read by reflection so this helper compiles without InternalsVisibleTo.
        private static IEnumerable<LogEntryShim> EnumerateLogEntries(VisualElement root)
        {
            var logView = root.Q<ScrollView>(SampleAppUi.LogScrollView);
            if (logView == null) yield break;

            // Each direct child of the contentContainer is a log row.
            foreach (var row in logView.contentContainer.Children())
            {
                if (row.userData == null) continue;
                yield return new LogEntryShim(row.userData);
            }
        }

        // Adapter over the opaque LogEntry stashed on each row's userData.
        // Reads via reflection so this helper compiles without InternalsVisibleTo.
        // If the LogEntry struct shape changes, update this adapter.
        private readonly struct LogEntryShim
        {
            private readonly object _entry;
            internal LogEntryShim(object entry) { _entry = entry; }

            internal string Label =>
                (string)(_entry.GetType().GetField("Label")?.GetValue(_entry) ?? "");

            internal string Body =>
                (string)(_entry.GetType().GetField("Body")?.GetValue(_entry) ?? "");

            internal int Level
            {
                get
                {
                    var v = _entry.GetType().GetField("Level")?.GetValue(_entry);
                    return v == null ? -1 : Convert.ToInt32(v);
                }
            }

            internal bool LabelMatches(string expected) =>
                Label.IndexOf(expected, StringComparison.Ordinal) >= 0;

            internal bool LevelEquals(int expected) => Level == expected;
        }
    }

    // Mirrors AudienceSample.UI.cs LogLevel enum: Info=0, Ok=1, Warn=2, Err=3, Debug=4.
    // Verified by reading the enum at AudienceSample.UI.cs:675 — matches plan-stated ordering.
    internal static class LogLevels
    {
        internal const int Info = 0;
        internal const int Ok = 1;
        internal const int Warn = 2;
        internal const int Err = 3;
        internal const int Debug = 4;
    }

    // UI Toolkit's Button.clicked event has custom add/remove accessors that
    // delegate to Clickable.clicked. The backing delegate lives on the
    // Clickable instance, not on Button itself. Reflect on it to invoke
    // synchronously without going through the panel's event loop.
    internal static class ButtonTestExtensions
    {
        internal static void Click(this Button button)
        {
            var clickable = button?.clickable;
            if (clickable == null) return;
            var field = typeof(Clickable).GetField("clicked",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var handler = field?.GetValue(clickable) as Delegate;
            handler?.DynamicInvoke();
        }
    }
}
