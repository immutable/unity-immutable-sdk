<div class="display-none">

# Clear Storage and Cache

</div>

The Clear Storage and Cache feature in the Immutable Passport SDK provides a way to clear WebView storage and cache on mobile devices (Android and iOS). This is particularly useful for testing login flows, handling user log out completely, or managing authentication state for your application.

<div class="button-component">

[View feature on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/ClearStorageAndCache) <span class="button-component-arrow">→</span>

</div>

#### Unity SDK Clear Storage and Cache Implementation

The Clear Storage and Cache feature provides two main functions:

1. `ClearStorage()` - Clears all underlying WebView storage currently being used by JavaScript storage APIs, including Web SQL Database and HTML5 Web Storage APIs.
2. `ClearCache(bool includeDiskFiles)` - Clears the underlying WebView resource cache, with an option to include disk files.

##### Feature: Clear Storage and Cache

```csharp title="ClearStorageAndCacheScript" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ClearStorageAndCache/ClearStorageAndCacheScript.cs"
public void ClearStorageAndCache()
{
    if (Passport.Instance == null)
    {
        ShowOutput("Passport instance is null. Initialise Passport first.");
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

The implementation is straightforward:

1. First, it checks if the Passport instance has been initialized
2. Then, based on the platform:
   - On Android and iOS devices, it calls `ClearStorage()` to clear WebView storage and `ClearCache(true)` to clear the WebView cache including disk files
   - On other platforms, it displays a message indicating that the feature is only available on mobile devices

Note that this feature is specifically designed for mobile platforms (Android and iOS) and is not available in the Unity Editor or on desktop platforms.

#### Running the Feature Example

##### Prerequisites

1. Make sure you have set up your Unity project with the Immutable Passport SDK. For detailed setup instructions, visit [Immutable Hub](https://hub.immutable.com/).
2. For testing on mobile devices, you need to build the application for Android or iOS.

##### Steps to Run the Example

1. Open the sample project in Unity
2. Configure the Passport SDK with your application credentials
3. Build and deploy the application to an Android or iOS device
4. Navigate to the ClearStorageAndCache feature in the sample app
5. First login to Passport to initialize the SDK
6. Click the "Clear Storage and Cache" button to execute the feature
7. The application will display a message indicating that the storage and cache have been cleared

Note: This feature is particularly useful when testing authentication flows or when you need to clear user data from the device.

#### Summary

The Clear Storage and Cache feature provides a simple way to clear WebView storage and cache on mobile devices. This is useful for:

- Testing authentication flows
- Ensuring user privacy by clearing stored data
- Resolving WebView caching issues
- Completely logging out users and removing saved credentials

When implementing this feature in your own application, remember that it is only available on Android and iOS devices, and requires that the Passport SDK is properly initialized before use. 