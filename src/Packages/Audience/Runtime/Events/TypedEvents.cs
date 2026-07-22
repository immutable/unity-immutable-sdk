#nullable enable

using System;
using System.Collections.Generic;

namespace Immutable.Audience
{
    /// <summary>
    /// State of a progression step.
    /// </summary>
    public enum ProgressionStatus
    {
        /// <summary>
        /// Player has begun this progression step.
        /// </summary>
        Start,

        /// <summary>
        /// Player finished this progression step successfully.
        /// </summary>
        Complete,

        /// <summary>
        /// Player failed or abandoned this progression step.
        /// </summary>
        Fail
    }

    internal static class ProgressionStatusExtensions
    {
        // Throws on unknown casts. Progression.ToProperties propagates, and
        // Track(IEvent) catches and drops with a warning.
        internal static string ToLowercaseString(this ProgressionStatus status) => status switch
        {
            ProgressionStatus.Start => "start",
            ProgressionStatus.Complete => "complete",
            ProgressionStatus.Fail => "fail",
            _ => throw new ArgumentOutOfRangeException(
                nameof(status), status, "Unhandled ProgressionStatus"),
        };
    }

    /// <summary>
    /// Player progressing through a world / level / stage. Track via
    /// <see cref="ImmutableAudience.Track(IEvent)"/>.
    /// </summary>
    public class Progression : IBuiltInEvent
    {
        /// <summary>
        /// Required. Where the player is in the progression flow.
        /// </summary>
        public ProgressionStatus? Status { get; set; }

        /// <summary>
        /// Optional. The world the player is in.
        /// </summary>
        public string? World { get; set; }

        /// <summary>
        /// Optional. The level within the world.
        /// </summary>
        public string? Level { get; set; }

        /// <summary>
        /// Optional. The stage within the level.
        /// </summary>
        public string? Stage { get; set; }

        /// <summary>
        /// Optional. The player's current score.
        /// </summary>
        public int? Score { get; set; }

        /// <summary>
        /// Optional. Time the player spent on this progression step, in
        /// seconds.
        /// </summary>
        public float? DurationSec { get; set; }

        /// <inheritdoc/>
        public string EventName => "progression";

        /// <inheritdoc/>
        public Dictionary<string, object> ToProperties()
        {
            if (Status is null)
                throw new ArgumentException("Progression.Status is required. Set it before calling Track(IEvent).");

            var props = new Dictionary<string, object>
            {
                ["status"] = Status.Value.ToLowercaseString()
            };

            if (World != null) props["world"] = World;
            if (Level != null) props["level"] = Level;
            if (Stage != null) props["stage"] = Stage;
            if (Score.HasValue) props["score"] = Score.Value;
            if (DurationSec.HasValue) props["duration_sec"] = DurationSec.Value;

            return props;
        }
    }

    /// <summary>
    /// Direction an in-game resource flowed.
    /// </summary>
    public enum ResourceFlow
    {
        /// <summary>
        /// Player gained the resource (loot, daily bonus, quest reward).
        /// </summary>
        Source,

        /// <summary>
        /// Player spent the resource (purchase, consumed, lost).
        /// </summary>
        Sink
    }

    internal static class ResourceFlowExtensions
    {
        // Throws on unknown casts. Resource.ToProperties propagates, and
        // Track(IEvent) catches and drops with a warning.
        internal static string ToLowercaseString(this ResourceFlow flow) => flow switch
        {
            ResourceFlow.Source => "source",
            ResourceFlow.Sink => "sink",
            _ => throw new ArgumentOutOfRangeException(
                nameof(flow), flow, "Unhandled ResourceFlow"),
        };
    }

    /// <summary>
    /// In-game currency earned or spent. Track via
    /// <see cref="ImmutableAudience.Track(IEvent)"/>.
    /// </summary>
    public class Resource : IBuiltInEvent
    {
        /// <summary>
        /// Required. Whether this is a gain or a spend.
        /// </summary>
        public ResourceFlow? Flow { get; set; }

        /// <summary>
        /// Required. The in-game currency name (for example, gold, gems,
        /// energy).
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>
        /// Required. How much of the currency was gained or spent.
        /// </summary>
        public float? Amount { get; set; }

        /// <summary>
        /// Optional. The kind of item involved (for example, weapon, skin).
        /// </summary>
        public string? ItemType { get; set; }

        /// <summary>
        /// Optional. The specific item identifier.
        /// </summary>
        public string? ItemId { get; set; }

        /// <inheritdoc/>
        public string EventName => "resource";

        /// <inheritdoc/>
        public Dictionary<string, object> ToProperties()
        {
            if (Flow is null)
                throw new ArgumentException("Resource.Flow is required. Set it before calling Track(IEvent).");
            if (string.IsNullOrEmpty(Currency))
                throw new ArgumentException("Resource.Currency is required. Set a non-empty string before calling Track(IEvent).");
            if (Amount is null)
                throw new ArgumentException("Resource.Amount is required. Set it before calling Track(IEvent).");

            var props = new Dictionary<string, object>
            {
                ["flow"] = Flow.Value.ToLowercaseString(),
                ["currency"] = Currency,
                ["amount"] = Amount.Value
            };

            if (ItemType != null) props["item_type"] = ItemType;
            if (ItemId != null) props["item_id"] = ItemId;

            return props;
        }
    }

    /// <summary>
    /// Real-money transaction. Track via
    /// <see cref="ImmutableAudience.Track(IEvent)"/>.
    /// </summary>
    public class Purchase : IBuiltInEvent
    {
        /// <summary>
        /// Required. ISO 4217 three-letter uppercase currency code (for
        /// example, <c>USD</c>, <c>EUR</c>).
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>
        /// Required. The transaction amount in <see cref="Currency"/>, as a
        /// numeric-looking string (e.g. <c>"9.99"</c>), not a number, to
        /// avoid floating-point precision loss.
        /// </summary>
        public string? Value { get; set; }

        /// <summary>
        /// Optional. Stable identifier of the item purchased.
        /// </summary>
        public string? ItemId { get; set; }

        /// <summary>
        /// Optional. Human-readable name of the item.
        /// </summary>
        public string? ItemName { get; set; }

        /// <summary>
        /// Optional. Number of items in this transaction.
        /// </summary>
        public int? Quantity { get; set; }

        /// <summary>
        /// Optional. Studio's own transaction identifier.
        /// </summary>
        public string? TransactionId { get; set; }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public Dictionary<string, object> ToProperties()
        {
            if (Currency == null || !IsIso4217(Currency))
                throw new ArgumentException(
                    $"Purchase.Currency '{Currency}' must be a three-letter uppercase ISO 4217 code");
            if (string.IsNullOrEmpty(Value))
                throw new ArgumentException("Purchase.Value is required. Set it before calling Track(IEvent).");

            var props = new Dictionary<string, object>
            {
                ["currency"] = Currency,
                ["value"] = Value
            };

            if (ItemId != null) props["item_id"] = ItemId;
            if (ItemName != null) props["item_name"] = ItemName;
            if (Quantity.HasValue) props["quantity"] = Quantity.Value;
            if (TransactionId != null) props["transaction_id"] = TransactionId;

            return props;
        }
    }

    /// <summary>
    /// Category of an achievement.
    /// </summary>
    public enum AchievementType
    {
        /// <summary>
        /// Introductory achievements tied to first-time player actions.
        /// </summary>
        Onboarding,

        /// <summary>
        /// Achievements tied to advancing through story or level content.
        /// </summary>
        Progression,

        /// <summary>
        /// Skill- or challenge-based achievements.
        /// </summary>
        Mastery,

        /// <summary>
        /// Multiplayer or community achievements.
        /// </summary>
        Social,

        /// <summary>
        /// Completionist achievements for gathering or finding items.
        /// </summary>
        Collection
    }

    internal static class AchievementTypeExtensions
    {
        // Throws on unknown casts. AchievementUnlocked.ToProperties propagates, and
        // Track(IEvent) catches and drops with a warning.
        internal static string ToLowercaseString(this AchievementType type) => type switch
        {
            AchievementType.Onboarding => "onboarding",
            AchievementType.Progression => "progression",
            AchievementType.Mastery => "mastery",
            AchievementType.Social => "social",
            AchievementType.Collection => "collection",
            _ => throw new ArgumentOutOfRangeException(
                nameof(type), type, "Unhandled AchievementType"),
        };
    }

    /// <summary>
    /// Player unlocked an achievement. Track via
    /// <see cref="ImmutableAudience.Track(IEvent)"/>.
    /// </summary>
    public class AchievementUnlocked : IBuiltInEvent
    {
        /// <summary>
        /// Required. Stable identifier for the achievement (for example,
        /// <c>ach_enemies_100</c>).
        /// </summary>
        public string? AchievementId { get; set; }

        /// <summary>
        /// Required. Display name of the achievement (for example,
        /// <c>100 Enemies Defeated</c>).
        /// </summary>
        public string? AchievementName { get; set; }

        /// <summary>
        /// Optional. Category of the achievement.
        /// </summary>
        public AchievementType? AchievementType { get; set; }

        /// <inheritdoc/>
        public string EventName => "achievement_unlocked";

        /// <inheritdoc/>
        public Dictionary<string, object> ToProperties()
        {
            if (string.IsNullOrEmpty(AchievementId))
                throw new ArgumentException("AchievementUnlocked.AchievementId is required. Set a non-empty string before calling Track(IEvent).");
            if (string.IsNullOrEmpty(AchievementName))
                throw new ArgumentException("AchievementUnlocked.AchievementName is required. Set a non-empty string before calling Track(IEvent).");

            var props = new Dictionary<string, object>
            {
                ["achievement_id"] = AchievementId,
                ["achievement_name"] = AchievementName
            };

            if (AchievementType.HasValue) props["achievement_type"] = AchievementType.Value.ToLowercaseString();

            return props;
        }
    }

    /// <summary>
    /// Named milestone or achievement reached by the player. Track via
    /// <see cref="ImmutableAudience.Track(IEvent)"/>.
    /// </summary>
    public class MilestoneReached : IBuiltInEvent
    {
        /// <summary>
        /// Required. The milestone identifier (for example,
        /// <c>tutorial_complete</c>).
        /// </summary>
        public string? Name { get; set; }

        /// <inheritdoc/>
        public string EventName => "milestone_reached";

        /// <inheritdoc/>
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
