#nullable enable

using System;
using System.Collections.Generic;

namespace Immutable.Audience
{
    // Progression event state.
    public enum ProgressionStatus
    {
        Start,
        Complete,
        Fail
    }

    internal static class ProgressionStatusExtensions
    {
        // Throws on unknown casts. Progression.ToProperties propagates, and
        // Track(IEvent) catches + drops with a warning.
        internal static string ToLowercaseString(this ProgressionStatus status) => status switch
        {
            ProgressionStatus.Start => "start",
            ProgressionStatus.Complete => "complete",
            ProgressionStatus.Fail => "fail",
            _ => throw new ArgumentOutOfRangeException(
                nameof(status), status, "Unhandled ProgressionStatus"),
        };
    }

    // Player progressing through a world / level / stage.
    public class Progression : IEvent
    {
        // Required. Nullable so an unset caller produces a clear validation
        // error at send time instead of silently shipping the enum default.
        public ProgressionStatus? Status { get; set; }
        // Optional.
        public string? World { get; set; }
        public string? Level { get; set; }
        public string? Stage { get; set; }
        public int? Score { get; set; }
        public float? DurationSec { get; set; }

        public string EventName => "progression";

        public Dictionary<string, object> ToProperties()
        {
            if (Status is null)
                throw new ArgumentException("Progression.Status is required — set it before calling Track(IEvent)");

            var props = new Dictionary<string, object>
            {
                ["status"] = Status.Value.ToLowercaseString()
            };

            if (World != null) props["world"] = World;
            if (Level != null) props["level"] = Level;
            if (Stage != null) props["stage"] = Stage;
            if (Score.HasValue) props["score"] = Score.Value;
            if (DurationSec.HasValue) props["durationSec"] = DurationSec.Value;

            return props;
        }
    }

    // Resource flow direction.
    public enum ResourceFlow
    {
        Source,
        Sink
    }

    internal static class ResourceFlowExtensions
    {
        // Throws on unknown casts. Resource.ToProperties propagates, and
        // Track(IEvent) catches + drops with a warning.
        internal static string ToLowercaseString(this ResourceFlow flow) => flow switch
        {
            ResourceFlow.Source => "source",
            ResourceFlow.Sink => "sink",
            _ => throw new ArgumentOutOfRangeException(
                nameof(flow), flow, "Unhandled ResourceFlow"),
        };
    }

    // In-game currency earned or spent.
    public class Resource : IEvent
    {
        // Required. Nullable so an unset caller produces a clear validation
        // error at send time instead of silently shipping the enum / zero
        // default.
        public ResourceFlow? Flow { get; set; }
        public string? Currency { get; set; }
        public float? Amount { get; set; }
        // Optional.
        public string? ItemType { get; set; }
        public string? ItemId { get; set; }

        public string EventName => "resource";

        public Dictionary<string, object> ToProperties()
        {
            if (Flow is null)
                throw new ArgumentException("Resource.Flow is required — set it before calling Track(IEvent)");
            if (string.IsNullOrEmpty(Currency))
                throw new ArgumentException("Resource.Currency is required — set a non-empty string before calling Track(IEvent)");
            if (Amount is null)
                throw new ArgumentException("Resource.Amount is required — set it before calling Track(IEvent)");

            var props = new Dictionary<string, object>
            {
                ["flow"] = Flow.Value.ToLowercaseString(),
                ["currency"] = Currency,
                ["amount"] = Amount.Value
            };

            if (ItemType != null) props["itemType"] = ItemType;
            if (ItemId != null) props["itemId"] = ItemId;

            return props;
        }
    }

    // Real-money transaction.
    public class Purchase : IEvent
    {
        // Required. ISO 4217 three-letter uppercase currency code.
        public string? Currency { get; set; }
        // Required. Nullable so an unset caller produces a clear validation
        // error at send time instead of silently shipping a zero-value
        // purchase that breaks attribution and conversion reporting.
        public decimal? Value { get; set; }
        // Optional.
        public string? ItemId { get; set; }
        public string? ItemName { get; set; }
        public int? Quantity { get; set; }
        public string? TransactionId { get; set; }

        public string EventName => "purchase";

        // Hand-rolled to avoid pulling System.Text.RegularExpressions into the IL2CPP build.
        private static bool IsIso4217(string s)
        {
            if (s.Length != 3) return false;
            for (var i = 0; i < 3; i++)
            {
                var c = s[i];
                if (c < 'A' || c > 'Z') return false;
            }
            return true;
        }

        public Dictionary<string, object> ToProperties()
        {
            if (Currency == null || !IsIso4217(Currency))
                throw new ArgumentException(
                    $"Purchase.Currency '{Currency}' must be a three-letter uppercase ISO 4217 code");
            if (Value is null)
                throw new ArgumentException("Purchase.Value is required — set it before calling Track(IEvent)");

            var props = new Dictionary<string, object>
            {
                ["currency"] = Currency,
                ["value"] = Value.Value
            };

            if (ItemId != null) props["itemId"] = ItemId;
            if (ItemName != null) props["itemName"] = ItemName;
            if (Quantity.HasValue) props["quantity"] = Quantity.Value;
            if (TransactionId != null) props["transactionId"] = TransactionId;

            return props;
        }
    }

    // Named milestone or achievement.
    public class MilestoneReached : IEvent
    {
        // Required.
        public string? Name { get; set; }

        public string EventName => "milestone_reached";

        public Dictionary<string, object> ToProperties()
        {
            if (string.IsNullOrEmpty(Name))
                throw new ArgumentException("MilestoneReached.Name must not be null or empty");

            return new Dictionary<string, object>
            {
                ["name"] = Name
            };
        }
    }
}
