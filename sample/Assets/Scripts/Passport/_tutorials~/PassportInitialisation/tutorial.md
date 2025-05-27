<div class="display-none">

# Passport Initialisation

</div>

Passport initialization is a critical first step in integrating Immutable's blockchain capabilities into your Unity game. This feature demonstrates how to properly initialize the Passport SDK, which is required before using any other Passport functionality like authentication, wallet operations, or blockchain interactions.

<div class="button-component">

[View feature on Github](https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/PassportInitialisation/PassportInitialisationScript.cs) <span class="button-component-arrow">→</span>

</div>

## Unity SDK Passport Initialisation Implementation

### Feature: Passport Initialisation

The Passport Initialisation feature demonstrates how to properly initialize the Immutable Passport SDK in your Unity application. The initialization process configures the SDK with your application's credentials and environment settings, preparing it for authentication and blockchain interactions.

```csharp title="Passport Initialisation" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/PassportInitialisation/PassportInitialisationScript.cs"
// Set the log level for the SDK
Passport.LogLevel = LogLevel.Info;

// Don't redact token values from logs
Passport.RedactTokensInLogs = false;

// Initialise Passport
string environment = Immutable.Passport.Model.Environment.SANDBOX;
string clientId = "mp6rxfMDwwZDogcdgNrAaHnG0qMlXuMK";
Passport passport = await Passport.Init(clientId, environment, redirectUri, logoutRedirectUri);
```

This initialization process handles several important tasks:
1. **Log Level Configuration**: Sets the logging verbosity to help with debugging.
2. **Token Redaction**: Controls whether sensitive token information appears in logs.
3. **Environment Selection**: Determines whether to connect to the sandbox (for testing) or production environment.
4. **Client ID**: Specifies your application's unique identifier from the Immutable Hub.
5. **Redirect URIs**: Configures the authentication flow's redirect paths.

The implementation supports two authentication methods:
- **Device Code Auth**: Used for desktop applications where the user authenticates in an external browser.
- **PKCE Flow**: Used for web and mobile platforms where authentication happens in an embedded browser.

The code automatically selects the appropriate authentication method based on the platform, with WebGL defaulting to PKCE due to platform limitations:

```csharp title="Authentication Method Selection" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/PassportInitialisation/PassportInitialisationScript.cs"
// WebGL does not support Device Code Auth, so we'll use PKCE by default instead.
#if UNITY_WEBGL
    UsePKCE();
#endif
```

For PKCE authentication, the redirect URIs must be configured correctly based on the platform:

```csharp title="Platform-specific PKCE Configuration" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/PassportInitialisation/PassportInitialisationScript.cs"
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
```

## Running the Feature Example

### Prerequisites
- Create an Immutable Hub account and get your client ID from [Immutable Hub](https://hub.immutable.com)
- Set up the Unity project with the Immutable Passport SDK
- Configure your project for the appropriate platform (WebGL, Android, iOS, macOS, Windows)

### Steps to Run the Example
1. Open the Unity project and load the sample scene for Passport Initialisation
2. Replace the placeholder `clientId` with your own client ID from Immutable Hub
3. For non-WebGL platforms, you may need to set up custom URL schemes for your application
4. Run the application in the Unity Editor or build for your target platform
5. Once the application runs, it will automatically initialize Passport with the appropriate authentication method
6. If initialization is successful, the application will navigate to the unauthenticated scene, ready for login

## Summary

Properly initializing the Passport SDK is a crucial first step in integrating Immutable's blockchain features into your Unity game. The initialization process sets up the environment, configures authentication methods, and prepares the SDK for subsequent operations like user login, wallet management, and blockchain interactions.

When implementing this feature in your own game:
- Always store your client ID securely and consider using environment variables for different build configurations
- Select the appropriate environment (SANDBOX for testing, PRODUCTION for release)
- Configure platform-specific redirect URIs based on your application's structure
- Handle exceptions during initialization to provide meaningful feedback to users
- Consider adjusting log levels based on your build configuration (verbose for debugging, minimal for production) 