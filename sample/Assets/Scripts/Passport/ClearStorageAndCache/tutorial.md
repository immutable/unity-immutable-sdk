<div class="display-none">

# Clear Storage and Cache

</div>

The Clear Storage and Cache feature allows you to clear the underlying WebView storage and cache on mobile devices. This is particularly useful for resetting saved credentials and other locally stored data during development or when implementing logout functionality.

<div class="button-component">

[View feature on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/ClearStorageAndCache) <span class="button-component-arrow">→</span>

</div>

## Feature Overview

This atomic feature demonstrates how to use the Immutable Passport SDK to clear the WebView's storage and cache on mobile platforms (Android and iOS).

## SDK Integration Details

The Clear Storage and Cache feature allows developers to clear all locally stored data in the WebView used by the Passport SDK. This includes:
- Local storage data
- Session storage
- WebSQL databases
- Indexed databases
- Memory and disk caches

```csharp title="ClearStorageAndCache" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ClearStorageAndCache/ClearStorageAndCacheScript.cs"
public void ClearStorageAndCache()
{
    if (Passport.Instance == null)
    {
        ShowOutput("Passport instance is null. Initialize Passport first.");
        return;
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    ShowOutput("Clearing storage and cache...");
    Passport.Instance.ClearStorage();
    Passport.Instance.ClearCache(true);
    ShowOutput("Storage and cache cleared (on Android).");
#elif UNITY_IPHONE && !UNITY_EDITOR
    ShowOutput("Clearing storage and cache...");
    Passport.Instance.ClearStorage();
    Passport.Instance.ClearCache(true);
    ShowOutput("Storage and cache cleared (on iOS).");
#else
    ShowOutput("ClearStorageAndCache is only available on Android and iOS devices.");
#endif
}
```

The function first checks if the Passport instance is initialized. If it is, it calls two methods:

1. `Passport.Instance.ClearStorage()` - Clears all data stored using JavaScript storage APIs, including local storage, session storage, WebSQL databases, and IndexedDB.
2. `Passport.Instance.ClearCache(true)` - Clears the WebView's resource cache, including both memory and disk cache when the `includeDiskFiles` parameter is set to `true`.

These methods are only available on Android and iOS platforms, and will not work in the Unity Editor or on desktop platforms.

## Running the Feature Example

### Prerequisites
- Set up your Immutable Passport application on [Immutable Hub](https://hub.immutable.com/)
- Configure your Unity project with the Immutable Passport SDK

### Steps to Run
1. Open the sample project in Unity
2. Navigate to the Authenticated/Unauthenticated scene
3. Build and run the application on an Android or iOS device
4. Press the "Clear Storage and Cache" button to execute the feature
5. Observe the output message confirming that storage and cache have been cleared

## Summary

The Clear Storage and Cache feature provides a simple way to clear all locally stored data in the WebView used by the Passport SDK. This can be useful for:
- Debugging authentication issues
- Implementing complete logout functionality
- Resetting the application state
- Clearing saved credentials

Remember that this feature only works on actual Android and iOS devices, not in the Unity Editor or on desktop platforms. 