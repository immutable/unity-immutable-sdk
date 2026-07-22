# Immutable Audience

Typed C# tracking SDK for Unity games. Captures `game_launch`, `session_start` / `session_heartbeat` / `session_end` automatically; predefined events (`Progression`, `Resource`, `Purchase`, `AchievementUnlocked`, `MilestoneReached`) and custom events on demand.

> **Status:** alpha. APIs and behavior may change between releases.

## Install

In Unity, open **Window → Package Manager**, click **+ → Add package from git URL...**, and paste:

```
https://github.com/immutable/unity-immutable-sdk.git?path=src/Packages/Audience#main
```

For reproducible builds, replace `#main` with a release tag or a specific commit SHA.

Requires Unity 2021.3 or later. Works under Mono and IL2CPP. Supported platforms: Android, iOS, Windows 10+ (64-bit), macOS, Linux (64-bit).

## First event

```csharp
using Immutable.Audience;
using UnityEngine;

public static class Analytics
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        ImmutableAudience.Init(new AudienceConfig
        {
            PublishableKey = "YOUR_PUBLISHABLE_KEY",
            Consent = ConsentLevel.Anonymous,
            DistributionPlatform = DistributionPlatforms.Steam,
            Debug = true,
        });

        ImmutableAudience.Track(new Purchase { Currency = "USD", Value = "9.99" });
    }
}
```

Press Play; `ImmutableAudience.Initialized` returns `true` and `AnonymousId` becomes a non-null GUID. The SDK warns to the Unity Console with prefix `[ImmutableAudience]` only on errors.

## Documentation

- Integration guide and API reference: <https://docs.immutable.com/docs/products/audience/unity-sdk>
- Sample Unity project: [`examples/audience`](https://github.com/immutable/unity-immutable-sdk/tree/main/examples/audience)

## Android dependencies

Mobile install attribution (the `AUDIENCE_MOBILE_ATTRIBUTION` define) needs the Google Play Install Referrer Library. The package does **not** bundle it as a prebuilt `.aar` — bundled AARs can't be de-duplicated by Gradle, so shipping `installreferrer-2.2.aar` directly caused class/version conflicts when a game already pulled in the same library via another SDK. Instead it's resolved from Maven:

| Dependency | Version | Source |
| --- | --- | --- |
| `com.android.installreferrer:installreferrer` | 2.2 | [maven.google.com](https://maven.google.com/web/index.html#com.android.installreferrer:installreferrer:2.2) |

**With [EDM4U](https://github.com/googlesamples/unity-jar-resolver) (recommended):** no action needed. `Editor/ImmutableAudienceDependencies.xml` declares the dependency and EDM4U's Android Resolver fetches it, resolving any version conflict with other SDKs to a single highest version. Install EDM4U as a project prerequisite — do not embed a second copy.

**Without EDM4U:** add the dependency to your `Assets/Plugins/Android/mainTemplate.gradle`:

```gradle
dependencies {
    implementation 'com.android.installreferrer:installreferrer:2.2'
}
```

`Runtime/Plugins/Android/proguard-user.txt` ships explicit R8 keep rules for the Install Referrer Library; Unity's gradle build merges them regardless of how the dependency is resolved.

`play-services-ads-identifier` (GAID) is intentionally left for the studio to add, so games that don't collect the advertising ID never declare the `AD_ID` permission.

Before tagging a release, check `maven.google.com` for a newer version and bump the pinned version above and in `ImmutableAudienceDependencies.xml` if needed.

## License

See the repository [LICENSE](https://github.com/immutable/unity-immutable-sdk/blob/main/LICENSE.md).
