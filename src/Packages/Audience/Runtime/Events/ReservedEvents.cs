#nullable enable

using System.Collections.Generic;

namespace Immutable.Audience
{
    /// <summary>
    /// Required property names per reserved event, enforced at runtime by
    /// <see cref="ImmutableAudience.Track(string, Dictionary{string, object})"/>.
    /// The typed <see cref="IBuiltInEvent"/> classes enforce the same rules
    /// at compile time (constructor/property level) and again in
    /// <c>ToProperties</c>; this closes the gap for callers who use the
    /// string overload instead and so bypass all of that.
    /// </summary>
    internal static class ReservedEvents
    {
        internal static readonly IReadOnlyDictionary<string, string[]> RequiredProperties =
            new Dictionary<string, string[]>
            {
                ["purchase"] = new[] { "currency", "value" },
                ["progression"] = new[] { "status" },
                ["resource"] = new[] { "flow", "currency", "amount" },
                ["achievement_unlocked"] = new[] { "achievement_id", "achievement_name" },
                ["milestone_reached"] = new[] { "name" },
            };
    }
}
