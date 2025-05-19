<div class="display-none">

# Passport Initialisation

</div>

Learn how to properly initialize the Immutable Passport SDK in your Unity game. This foundational step is required before utilizing any other Passport features.

<div class="button-component">

[View feature on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/PassportInitialisation) <span class="button-component-arrow">→</span>

</div>

## Feature Overview

This example demonstrates how to initialize the Immutable Passport SDK with the appropriate configuration settings for both Device Code Auth and PKCE (Proof Key for Code Exchange) authentication methods.

## SDK Integration Details

### Initializing Passport with Device Code Auth

Device Code Auth is suitable for most desktop platforms, especially Windows. This method opens the player's default browser for authentication.

```csharp title="PassportInitialisationScript.cs" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/PassportInitialisation/PassportInitialisationScript.cs"
public void UseDeviceCodeAuth()
{
    SampleAppManager.UsePKCE = false;
    InitialisePassport(logoutRedirectUri: "https://www.immutable.com");
}

private async void InitialisePassport(string? redirectUri = null, string? logoutRedirectUri = null)
{
    try {
        // Set the log level for the SDK
        Passport.LogLevel = LogLevel.Info;

        // Don't redact token values from logs
        Passport.RedactTokensInLogs = false;

        // Initialise Passport
        string environment = Immutable.Passport.Model.Environment.SANDBOX;
        string clientId = "mp6rxfMDwwZDogcdgNrAaHnG0qMlXuMK";
        Passport passport = await Passport.Init(clientId, environment, redirectUri, logoutRedirectUri);
        SampleAppManager.PassportInstance = passport;

        // Navigate to the unauthenticated scene after initialising Passport
        SceneManager.LoadScene("UnauthenticatedScene");
    }
    catch (Exception ex) {
        Debug.LogException(ex, this);
        ShowOutput($"Initialise Passport error: {ex.Message}");
    }
}
```

### Initializing Passport with PKCE Auth

PKCE (Proof Key for Code Exchange) is recommended for mobile platforms (Android, iOS) and macOS. It provides a more seamless experience with in-app browsers or pop-up windows.

```csharp title="PassportInitialisationScript.cs" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/PassportInitialisation/PassportInitialisationScript.cs"
public void UsePKCE()
{
    SampleAppManager.UsePKCE = true;
#if UNITY_WEBGL
    string url = Application.absoluteURL;
    Uri uri = new Uri(url);
    string scheme = uri.Scheme;
    string hostWithPort = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
    string fullPath = uri.AbsolutePath.EndsWith("/") ? uri.AbsolutePath : uri.AbsolutePath.Substring(0, uri.AbsolutePath.LastIndexOf('/') + 1);

    string redirectUri = $"{scheme}://{hostWithPort}{fullPath}callback.html";
    string logoutRedirectUri = $"{scheme}://{hostWithPort}{fullPath}logout.html";
    
    InitialisePassport(redirectUri: redirectUri, logoutRedirectUri: logoutRedirectUri);
#else
    InitialisePassport(redirectUri: "immutablerunner://callback", logoutRedirectUri: "immutablerunner://logout");
#endif
}
```

### How It Works

1. **Configure Authentication Method**:
   - The sample allows selecting between Device Code Auth and PKCE
   - WebGL builds automatically default to PKCE

2. **Setting Up Passport**:
   - Set log level for appropriate debugging feedback
   - Configure token redaction behavior for security
   - Specify the environment (SANDBOX or PRODUCTION)
   - Provide your client ID obtained from Immutable Hub
   - Pass the appropriate redirect URIs based on authentication method

3. **Handling Platform-Specific Requirements**:
   - WebGL builds need special handling to dynamically determine callback URIs
   - Other platforms use custom URI schemes (e.g., "immutablerunner://callback")

4. **Managing Errors**:
   - Initialization errors are properly caught and logged

## Running the Feature Example

### Prerequisites

- Unity 2021.3 or newer
- Basic understanding of Unity concepts
- Immutable Hub account (set up at [Immutable Hub](https://hub.immutable.com/))

### Steps

1. **Import the SDK**:
   - Install the Immutable Unity SDK via the Unity Package Manager
   - Add the package from https://github.com/immutable/unity-immutable-sdk.git?path=/src/Packages/Passport

2. **Configure Your Client**:
   - Register your game as an OAuth 2.0 Native client in Immutable Hub
   - Take note of your client ID
   - Configure appropriate redirect URIs in your Hub settings

3. **Run the Example**:
   - Open the "SelectAuthScene" scene in Unity Editor
   - Enter Play mode
   - Click either the "Use Device Code Auth" or "Use PKCE" button to initialize Passport
   - For Device Code Auth, your browser will open for authentication
   - For PKCE on platforms that support it, a pop-up or in-app browser will appear

## Summary

The Passport Initialisation feature demonstrates how to properly set up and configure the Immutable Passport SDK in your Unity application. This is a critical first step before using any other Passport features like login, wallet operations, or blockchain interactions. The sample shows how to handle both authentication methods and platform-specific requirements, ensuring your game can work across desktop, mobile, and web platforms. 