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

        // ISO 4217 currency codes used by Purchase / Resource tests.
        internal const string UsdCurrency = "USD";
        internal const string EurCurrency = "EUR";

        // Progression.World fixture (used in three typed-event tests).
        internal const string ProgressionWorldTutorial = "tutorial";

        // Exception message fixture for the ContextProvider sabotage test.
        internal const string ContextProviderBoomMessage = "boom";

        // File body that occupies the queue directory path so directory creation fails.
        internal const string DiskBlockerContent = "blocker";

        // Mixed-case "Steam" / "Passport" fixtures shared by DistributionPlatform and IdentityType tests.
        internal const string SteamPascalCase = "Steam";
        internal const string SteamUpperCase = "STEAM";
        internal const string PassportPascalCase = "Passport";

        // Steam-prefixed-but-not-exact-match fixture for the Custom-fallback test.
        internal const string SteamSuffixed = "steamX";

        // Unity Application.platform string for the GameLaunch.Platform test.
        internal const string PlatformWindows = "WindowsPlayer";

        // Longer generic alias endpoint fixtures, for readability in Alias calls.
        internal const string GenericAliasFromId = "fromId";
        internal const string GenericAliasToId = "toId";
        internal const string GenericAliasFromShort = "from";

        // Real-shape 64-bit Steam community ID. Asserts the input is carried through faithfully.
        internal const string SteamId64 = "76561198012345";

        // Generic Steam / Passport ID fixtures used by alias and consent tests.
        internal const string SteamId = "steam123";
        internal const string PassportId = "user_456";

        // Generic single-user fixture for tests that just need any userId.
        internal const string GenericUserSingleId = "user1";

        // Progression payload values used by TypedEventTests and the JsonReaderTests round-trip.
        internal const string ProgressionLevelFixture = "1";
        internal const int ProgressionScoreFixture = 1500;
        internal const float ProgressionDurationSecFixture = 120.5f;

        // Resource.Amount fixture used by three TypedEventTests scenarios
        // (Source flow, WithoutFlow guard, WithoutAmount guard).
        internal const float ResourceAmountFixture = 100f;

        // Purchase.Value fixtures.
        // PurchaseValueFixture (9.99m) is the standard shape; PurchaseValueLowFixture (5.00m) is the optional-fields-omitted variant.
        internal const decimal PurchaseValueFixture = 9.99m;
        internal const decimal PurchaseValueLowFixture = 5.00m;
        internal const int PurchaseQuantityFixture = 1;

        // One-char placeholders for identifier slots when the value content isn't tested.
        internal const string MinimalIdentifierA = "a";
        internal const string MinimalIdentifierU = "u";
    }
}
