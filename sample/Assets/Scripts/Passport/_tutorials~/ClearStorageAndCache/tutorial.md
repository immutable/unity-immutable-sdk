<div class="display-none">

# Clear Storage and Cache

</div>

The Clear Storage and Cache feature provides essential WebView maintenance functionality for mobile applications using Immutable Passport. This feature allows developers to clear the underlying WebView's cached resources and JavaScript storage data, which is crucial for troubleshooting authentication issues, managing storage space, and ensuring clean application states on Android and iOS devices.

<div class="button-component">

[View feature on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/ClearStorageAndCache) <span class="button-component-arrow">→</span>

</div>

## Feature Introduction

The Clear Storage and Cache feature demonstrates how to use Immutable Passport's WebView maintenance capabilities to clear cached data and storage. This functionality is particularly valuable for:

- Resolving authentication issues by clearing stale session data
- Managing device storage by removing cached resources
- Ensuring clean application states during development and testing
- Troubleshooting WebView-related problems

This feature is only available on Android and iOS devices, as it directly interacts with the native WebView components used by Passport for authentication and communication.

## Unity SDK Clear Storage and Cache Implementation

### Feature: Clear Storage and Cache

The Clear Storage and Cache feature provides two distinct but complementary operations for maintaining WebView data:

```csharp title="Clear Storage and Cache" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ClearStorageAndCache/ClearStorageAndCacheScript.cs"
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

The implementation demonstrates several key aspects:

**Platform Availability**: The feature uses conditional compilation directives to ensure it only executes on Android and iOS devices. In the Unity editor or other platforms, it displays an informative message about platform limitations.

**Passport Instance Validation**: Before attempting to clear data, the code verifies that the Passport instance has been properly initialized, providing clear feedback if initialization is required.

**Dual Clearing Operations**: The feature calls both `ClearStorage()` and `ClearCache(true)` methods:
- `ClearStorage()` removes JavaScript storage data including localStorage, sessionStorage, WebSQL databases, and IndexedDB data
- `ClearCache(true)` clears the WebView's resource cache, with the `true` parameter indicating that disk-based cache files should also be removed

**User Feedback**: The implementation provides clear status messages to inform users about the operation's progress and completion, enhancing the debugging and development experience.

## Running the Feature Example

### Prerequisites

Before running the Clear Storage and Cache example, ensure you have:

- Unity 2022.3 LTS or later installed
- Immutable Passport SDK properly configured in your project
- A valid Passport client ID from [Immutable Hub](https://hub.immutable.com)
- An Android or iOS device for testing (this feature is not available in the Unity editor)

### Step-by-Step Instructions

1. **Open the Sample Project**
   - Navigate to the Unity sample project in the SDK repository
   - Open the project in Unity Editor

2. **Configure Passport Settings**
   - Ensure your Passport client ID is properly configured
   - Verify that redirect URLs are set up correctly in Immutable Hub

3. **Build for Mobile Platform**
   - Switch to Android or iOS build target in Build Settings
   - Configure platform-specific settings as needed
   - Build and deploy to your target device

4. **Initialize Passport**
   - Launch the application on your mobile device
   - Navigate to the Passport initialization section
   - Complete the Passport initialization process

5. **Test Clear Storage and Cache**
   - Locate the "Clear Storage & Cache" button in the sample application
   - Tap the button to execute the clearing operation
   - Observe the status messages confirming the operation completion

6. **Verify Results**
   - Check that cached authentication data has been cleared
   - Verify that subsequent authentication flows work as expected
   - Monitor device storage to confirm cache reduction

### Development Considerations

When integrating this feature into your own application, consider:

- **Timing**: Only call these methods when necessary, as clearing cache and storage will require users to re-authenticate
- **User Experience**: Provide clear communication to users about what data will be cleared and why
- **Error Handling**: Implement appropriate error handling for edge cases where clearing operations might fail
- **Platform Detection**: Always check platform availability before calling these methods

## Summary

The Clear Storage and Cache feature provides essential WebView maintenance capabilities for Immutable Passport applications on mobile platforms. By offering both cache clearing and storage clearing operations, developers can effectively manage WebView data, resolve authentication issues, and maintain optimal application performance.

Key takeaways for developers:

- **Mobile-Only Functionality**: This feature is exclusively available on Android and iOS devices due to its direct interaction with native WebView components
- **Dual-Purpose Operations**: The feature provides both cache clearing (for resource management) and storage clearing (for session data management)
- **Development Tool**: Particularly valuable during development and testing phases for ensuring clean application states
- **User Impact Awareness**: Clearing storage and cache will require users to re-authenticate, so use judiciously in production applications

This feature exemplifies best practices for WebView maintenance in Unity applications, providing developers with the tools needed to maintain robust and reliable authentication experiences across mobile platforms. 