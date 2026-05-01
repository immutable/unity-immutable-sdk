#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine.UIElements;

namespace Immutable.Audience.Samples.SampleApp
{
    // Events partial of AudienceSample — see AudienceSample.cs for the partial layout.
    public sealed partial class AudienceSample
    {
        // ---- Event DSL types ----

        internal enum FieldKind { String, Number, Enum }

        internal readonly struct EventField
        {
            public readonly string Key;
            public readonly FieldKind Kind;
            public readonly bool Optional;
            public readonly string[]? EnumValues;

            private EventField(string key, FieldKind kind, bool optional, string[]? enumValues)
            { Key = key; Kind = kind; Optional = optional; EnumValues = enumValues; }

            public static EventField Text(string key, bool optional = false)   => new EventField(key, FieldKind.String, optional, null);
            public static EventField Number(string key, bool optional = false) => new EventField(key, FieldKind.Number, optional, null);
            public static EventField Enum(string key, string[] values, bool optional = false) => new EventField(key, FieldKind.Enum, optional, values);
        }

        internal readonly struct EventSpec
        {
            public readonly string Name;
            public readonly EventField[] Fields;
            public EventSpec(string name, EventField[] fields) { Name = name; Fields = fields; }
        }

        private const string OptionalEnumSentinel = "(not set)";

        private static readonly string[] ProgressionStatusValues =
            Enum.GetValues(typeof(ProgressionStatus))
                .Cast<ProgressionStatus>()
                .Select(s => s.ToLowercaseString())
                .ToArray();

        private static readonly string[] ResourceFlowValues =
            Enum.GetValues(typeof(ResourceFlow))
                .Cast<ResourceFlow>()
                .Select(f => f.ToLowercaseString())
                .ToArray();

        // ---- Event catalogue ----

        internal static readonly EventSpec[] Catalogue =
        {
            new EventSpec("sign_up",        new[] { EventField.Text("method", optional: true) }),
            new EventSpec("sign_in",        new[] { EventField.Text("method", optional: true) }),
            new EventSpec("email_acquired", new[] { EventField.Text("source", optional: true) }),
            new EventSpec("wishlist_add",   new[] {
                EventField.Text("gameId"),
                EventField.Text("source",   optional: true),
                EventField.Text("platform", optional: true),
            }),
            new EventSpec("wishlist_remove", new[] { EventField.Text("gameId") }),
            new EventSpec(EventNames.Purchase, new[] {
                EventField.Text(EventPropertyKeys.Currency),
                EventField.Number(EventPropertyKeys.Value),
                EventField.Text(EventPropertyKeys.ItemId,        optional: true),
                EventField.Text(EventPropertyKeys.ItemName,      optional: true),
                EventField.Number(EventPropertyKeys.Quantity,    optional: true),
                EventField.Text(EventPropertyKeys.TransactionId, optional: true),
            }),
            // game_launch is deliberately absent. The Event Reference v1 defines
            // it as auto-tracked on Init with no public typed class; firing it
            // from the Send button would double-emit.
            new EventSpec(EventNames.Progression, new[] {
                EventField.Enum(EventPropertyKeys.Status, ProgressionStatusValues),
                EventField.Text(EventPropertyKeys.World,         optional: true),
                EventField.Text(EventPropertyKeys.Level,         optional: true),
                EventField.Text(EventPropertyKeys.Stage,         optional: true),
                EventField.Number(EventPropertyKeys.Score,       optional: true),
                EventField.Number(EventPropertyKeys.DurationSec, optional: true),
            }),
            new EventSpec(EventNames.Resource, new[] {
                EventField.Enum(EventPropertyKeys.Flow, ResourceFlowValues),
                EventField.Text(EventPropertyKeys.Currency),
                EventField.Number(EventPropertyKeys.Amount),
                EventField.Text(EventPropertyKeys.ItemType, optional: true),
                EventField.Text(EventPropertyKeys.ItemId,   optional: true),
            }),
            new EventSpec(EventNames.MilestoneReached, new[] { EventField.Text(EventPropertyKeys.Name) }),
            new EventSpec("game_page_viewed",  new[] {
                EventField.Text("gameId"),
                EventField.Text("gameName", optional: true),
                EventField.Text("slug",     optional: true),
            }),
            new EventSpec("link_clicked", new[] {
                EventField.Text("url"),
                EventField.Text("label",  optional: true),
                EventField.Text("source", optional: true),
                EventField.Text("gameId", optional: true),
            }),
        };

        // ---- Typed event construction ----

        // Returns null for events not covered by the typed surface; callers
        // fall back to the string overload.
        private static IEvent? BuildTypedEvent(string name, Dictionary<string, object> props)
        {
            switch (name)
            {
                case EventNames.Progression:
                    return new Progression
                    {
                        Status = ParseProgressionStatus(props),
                        World = OptionalString(props, EventPropertyKeys.World),
                        Level = OptionalString(props, EventPropertyKeys.Level),
                        Stage = OptionalString(props, EventPropertyKeys.Stage),
                        Score = OptionalInt(props, EventPropertyKeys.Score),
                        DurationSec = OptionalFloat(props, EventPropertyKeys.DurationSec),
                    };
                case EventNames.Resource:
                    return new Resource
                    {
                        Flow = ParseResourceFlow(props),
                        Currency = OptionalString(props, EventPropertyKeys.Currency) ?? "",
                        Amount = OptionalFloat(props, EventPropertyKeys.Amount) ?? 0f,
                        ItemType = OptionalString(props, EventPropertyKeys.ItemType),
                        ItemId = OptionalString(props, EventPropertyKeys.ItemId),
                    };
                case EventNames.Purchase:
                    return new Purchase
                    {
                        Currency = OptionalString(props, EventPropertyKeys.Currency) ?? "",
                        Value = OptionalDecimal(props, EventPropertyKeys.Value) ?? 0m,
                        ItemId = OptionalString(props, EventPropertyKeys.ItemId),
                        ItemName = OptionalString(props, EventPropertyKeys.ItemName),
                        Quantity = OptionalInt(props, EventPropertyKeys.Quantity),
                        TransactionId = OptionalString(props, EventPropertyKeys.TransactionId),
                    };
                case EventNames.MilestoneReached:
                    return new MilestoneReached { Name = OptionalString(props, EventPropertyKeys.Name) ?? "" };
                default:
                    return null;
            }
        }

        private static ProgressionStatus? ParseProgressionStatus(Dictionary<string, object> props)
        {
            var s = OptionalString(props, EventPropertyKeys.Status);
            if (string.IsNullOrEmpty(s)) return null;
            foreach (ProgressionStatus value in Enum.GetValues(typeof(ProgressionStatus)))
                if (value.ToLowercaseString() == s) return value;
            return null;
        }

        private static ResourceFlow? ParseResourceFlow(Dictionary<string, object> props)
        {
            var s = OptionalString(props, EventPropertyKeys.Flow);
            if (string.IsNullOrEmpty(s)) return null;
            foreach (ResourceFlow value in Enum.GetValues(typeof(ResourceFlow)))
                if (value.ToLowercaseString() == s) return value;
            return null;
        }

        private static string? OptionalString(Dictionary<string, object> props, string key) =>
            props.TryGetValue(key, out var v) && v is string s && !string.IsNullOrEmpty(s) ? s : null;

        private static int? OptionalInt(Dictionary<string, object> props, string key) =>
            props.TryGetValue(key, out var v) ? v switch { int i => i, double d => (int)d, _ => (int?)null } : null;

        private static float? OptionalFloat(Dictionary<string, object> props, string key) =>
            props.TryGetValue(key, out var v) ? v switch { double d => (float)d, int i => (float)i, _ => (float?)null } : null;

        private static decimal? OptionalDecimal(Dictionary<string, object> props, string key) =>
            props.TryGetValue(key, out var v) ? v switch { double d => (decimal)d, int i => (decimal)i, _ => (decimal?)null } : null;

        // ---- Property collection (UI form inputs → props dictionary) ----

        // Number kinds parse via invariant culture (no locale commas) and
        // collapse non-fractional doubles to int — keeps Quantity / Score off
        // the wire as 1 instead of 1.0.
        private static Dictionary<string, object> BuildPropsDictionary(EventSpec spec, Dictionary<string, VisualElement> inputs)
        {
            var props = new Dictionary<string, object>();
            foreach (var field in spec.Fields)
            {
                string raw = inputs[field.Key] switch
                {
                    DropdownField dd when dd.value != OptionalEnumSentinel => dd.value ?? "",
                    DropdownField _                                        => "",
                    TextField tf                                           => tf.value ?? "",
                    _                                                      => "",
                };
                if (string.IsNullOrEmpty(raw)) continue;
                if (field.Kind == FieldKind.Number)
                {
                    if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var n)) continue;
                    props[field.Key] = (Math.Abs(n % 1) < double.Epsilon && Math.Abs(n) < int.MaxValue) ? (object)(int)n : n;
                }
                else props[field.Key] = raw;
            }
            return props;
        }
    }
}
