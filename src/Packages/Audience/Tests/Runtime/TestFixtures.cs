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

        // Track properties scenario data
        internal const string CustomPropKeyRecipeId = "recipe_id";
        internal const string CraftingRecipeIronSword = "iron_sword";

        // Distribution platform fixture for the "platform from outside SDK" path.
        internal const string ProviderValue = "provider_value";

        // IdentityTypeExtensions.ParseLowercaseString fallback fixture.
        internal const string UnknownProvider = "unknown_provider";

        // Identity persistence fixtures (id read back from disk on next launch).
        internal const string PreExistingId = "pre-existing-id";
        internal const string PreExistingIdFromLastLaunch = "pre-existing-id-from-last-launch";

        // ConsentStore corruption fixture (non-integer file content).
        internal const string NotAnInt = "not-an-int";

        // ThreadSafetyStressTests userId for race-stress scenarios.
        internal const string UserRaceStress = "user_race_stress";

        // DeleteDataTests generic userId.
        internal const string SomeUser = "some-user";

        // Prefix for GzipTests' $"anon-{i}" loop (per-message anonymous IDs).
        internal const string AnonIdPrefix = "anon-";

        // Placeholders for fixture slots where the value itself is not under test.
        internal const string GenericUserId = "u1";
        internal const string GenericFromId = "f";
        internal const string GenericToId = "t";
        internal const string GenericFromType = "t1";
        internal const string GenericToType = "t2";
    }
}
