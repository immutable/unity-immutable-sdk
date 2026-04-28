using System;
using System.Collections.Generic;
using System.Globalization;
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
            public readonly string[] EnumValues;

            private EventField(string key, FieldKind kind, bool optional, string[] enumValues)
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
            new EventSpec("purchase",        new[] {
                EventField.Text("currency"),
                EventField.Number("value"),
                EventField.Text("itemId",        optional: true),
                EventField.Text("itemName",      optional: true),
                EventField.Number("quantity",    optional: true),
                EventField.Text("transactionId", optional: true),
            }),
            // game_launch is deliberately absent. The Event Reference v1 defines
            // it as auto-tracked on Init with no public typed class; firing it
            // from the Send button would double-emit.
            new EventSpec("progression", new[] {
                EventField.Enum("status", new[] { "start", "complete", "fail" }),
                EventField.Text("world",         optional: true),
                EventField.Text("level",         optional: true),
                EventField.Text("stage",         optional: true),
                EventField.Number("score",       optional: true),
                EventField.Number("durationSec", optional: true),
            }),
            new EventSpec("resource", new[] {
                EventField.Enum("flow", new[] { "sink", "source" }),
                EventField.Text("currency"),
                EventField.Number("amount"),
                EventField.Text("itemType", optional: true),
                EventField.Text("itemId",   optional: true),
            }),
            new EventSpec("milestone_reached", new[] { EventField.Text("name") }),
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
        private static IEvent BuildTypedEvent(string name, Dictionary<string, object> props)
        {
            switch (name)
            {
                case "progression":
                    return new Progression
                    {
                        Status = ParseProgressionStatus(props),
                        World = OptionalString(props, "world"),
                        Level = OptionalString(props, "level"),
                        Stage = OptionalString(props, "stage"),
                        Score = OptionalInt(props, "score"),
                        DurationSec = OptionalFloat(props, "durationSec"),
                    };
                case "resource":
                    return new Resource
                    {
                        Flow = ParseResourceFlow(props),
                        Currency = OptionalString(props, "currency") ?? "",
                        Amount = OptionalFloat(props, "amount") ?? 0f,
                        ItemType = OptionalString(props, "itemType"),
                        ItemId = OptionalString(props, "itemId"),
                    };
                case "purchase":
                    return new Purchase
                    {
                        Currency = OptionalString(props, "currency") ?? "",
                        Value = OptionalDecimal(props, "value") ?? 0m,
                        ItemId = OptionalString(props, "itemId"),
                        ItemName = OptionalString(props, "itemName"),
                        Quantity = OptionalInt(props, "quantity"),
                        TransactionId = OptionalString(props, "transactionId"),
                    };
                case "milestone_reached":
                    return new MilestoneReached { Name = OptionalString(props, "name") ?? "" };
                default:
                    return null;
            }
        }

        private static ProgressionStatus? ParseProgressionStatus(Dictionary<string, object> props)
        {
            var s = OptionalString(props, "status");
            if (string.IsNullOrEmpty(s)) return null;
            return s switch
            {
                "start"    => ProgressionStatus.Start,
                "complete" => ProgressionStatus.Complete,
                "fail"     => ProgressionStatus.Fail,
                _          => (ProgressionStatus?)null,
            };
        }

        private static ResourceFlow? ParseResourceFlow(Dictionary<string, object> props)
        {
            var s = OptionalString(props, "flow");
            if (string.IsNullOrEmpty(s)) return null;
            return s switch
            {
                "source" => ResourceFlow.Source,
                "sink"   => ResourceFlow.Sink,
                _        => (ResourceFlow?)null,
            };
        }

        private static string OptionalString(Dictionary<string, object> props, string key) =>
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
