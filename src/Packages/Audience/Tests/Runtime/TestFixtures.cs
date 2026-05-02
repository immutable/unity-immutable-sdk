namespace Immutable.Audience.Tests
{
    // Per-fixture data values shared across the SDK test suite.
    internal static class TestFixtures
    {
        // Anonymous IDs
        internal const string AnonId1 = "anon-1";
        internal const string AnonId42 = "anon-42";
        internal const string AnonId123 = "anon-123";

        // User IDs
        internal const string UserId42 = "user-42";
        internal const string UserId99 = "user-99";

        // Alias endpoints
        internal const string AliasFromId = "from-id";
        internal const string AliasToId = "to-id";

        // Identity values keyed to IdentityType.Custom / Steam / Passport
        internal const string PlayerCustomId = "player-42";
        internal const string PlayerSteamId = "player_steam";
        internal const string PlayerPassportId = "player_passport";

        // Resource event payload values
        internal const string ResourceCurrency = "gold";
        internal const string ResourceItemType = "quest_reward";
        internal const string ResourceItemId = "main_quest_01";

        // Purchase event payload values
        internal const string PurchaseItemId = "gem_pack_01";
        internal const string PurchaseItemName = "Starter Gem Pack";
        internal const string PurchaseTransactionId = "txn_abc123";

        // MilestoneReached event payload values
        internal const string MilestoneName = "first_boss_defeated";
    }
}
