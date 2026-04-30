# Immutable Audience

Typed C# tracking SDK for Unity games. Captures `game_launch`, `session_start` / `session_heartbeat` / `session_end` automatically; predefined events (`Progression`, `Resource`, `Purchase`, `MilestoneReached`) and custom events on demand.

> **Status:** alpha. APIs and behavior may change between releases.

## Install

In Unity, open **Window → Package Manager**, click **+ → Add package from git URL...**, and paste:

```
https://github.com/immutable/unity-immutable-sdk.git?path=src/Packages/Audience#main
```

For reproducible builds, replace `#main` with a release tag or a specific commit SHA.

Requires Unity 2021.3 or later. Works under Mono and IL2CPP.

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

        ImmutableAudience.Track(new Purchase { Currency = "USD", Value = 9.99m });
    }
}
```

Press Play; `ImmutableAudience.Initialized` returns `true` and `AnonymousId` becomes a non-null GUID. The SDK warns to the Unity Console with prefix `[ImmutableAudience]` only on errors.

## Documentation

- Integration guide and API reference: <https://docs.immutable.com/docs/products/audience/unity-sdk>
- Sample Unity project: [`examples/audience`](https://github.com/immutable/unity-immutable-sdk/tree/main/examples/audience)

## License

See the repository [LICENSE](https://github.com/immutable/unity-immutable-sdk/blob/main/LICENSE.md).
