<div class="display-none">

# Passport Initialisation

</div>

The Passport Initialisation feature demonstrates how to properly set up and configure the Immutable Passport SDK in your Unity game. This is the foundational step required before using any other Passport functionality, establishing the connection between your game and Immutable's authentication and wallet services.

<div class="button-component">

[View feature on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/PassportInitialisation) <span class="button-component-arrow">→</span>

</div>

## Unity SDK Passport Initialisation Implementation

### Feature: Passport Initialisation

The Passport Initialisation feature handles the essential setup process for the Immutable Passport SDK. This includes configuring platform-specific redirect URIs, setting up logging preferences, and establishing the initial connection to Immutable's services.

```csharp title="Passport Initialisation" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/PassportInitialisation/PassportInitialisationScript.cs"
private async void InitialisePassport()
{
    ShowOutput("Initialising Passport...");

    string redirectUri;
    string logoutRedirectUri;

#if UNITY_WEBGL
        var url = Application.absoluteURL;
        var uri = new Uri(url);
        var scheme = uri.Scheme;
        var hostWithPort = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
        var fullPath = uri.AbsolutePath.EndsWith("/")
            ? uri.AbsolutePath
            : uri.AbsolutePath.Substring(0, uri.AbsolutePath.LastIndexOf('/') + 1);

        redirectUri = $"{scheme}://{hostWithPort}{fullPath}callback.html";
        logoutRedirectUri = $"{scheme}://{hostWithPort}{fullPath}logout.html";
#else
    redirectUri = "immutablerunner://callback";
    logoutRedirectUri = "immutablerunner://logout";
#endif

    try
    {
        // Set the log level for the SDK
        Passport.LogLevel = LogLevel.Info;

        // Don't redact token values from logs
        Passport.RedactTokensInLogs = false;

        // Initialise Passport
        const string environment = Immutable.Passport.Model.Environment.SANDBOX;
        const string clientId = "mp6rxfMDwwZDogcdgNrAaHnG0qMlXuMK";
        var passport = await Passport.Init(clientId, environment, redirectUri, logoutRedirectUri);
        SampleAppManager.PassportInstance = passport;

        // Navigate to the unauthenticated scene after initialising Passport
        SceneManager.LoadScene("UnauthenticatedScene");
    }
    catch (Exception ex)
    {
        Debug.LogException(ex, this);
        ShowOutput($"Initialise Passport error: {ex.Message}");
    }
}
```

#### How the Code Works

The Passport initialisation process follows these key steps:

1. **Platform-Specific URI Configuration**: The code dynamically configures redirect URIs based on the target platform. For WebGL builds, it constructs URIs using the current application URL with specific callback and logout paths. For other platforms, it uses custom URI schemes (`immutablerunner://`).

2. **Logging Configuration**: The SDK's logging level is set to `Info` to provide detailed information during development, and token redaction is disabled to allow full visibility of authentication tokens in logs.

3. **SDK Initialisation**: The `Passport.Init()` method is called with essential parameters:
   - **Client ID**: A unique identifier for your application registered with Immutable
   - **Environment**: Set to `SANDBOX` for testing (use `PRODUCTION` for live applications)
   - **Redirect URIs**: URLs where users are redirected after authentication and logout

4. **Instance Management**: The successfully initialised Passport instance is stored in `SampleAppManager.PassportInstance` for use throughout the application.

5. **Scene Transition**: After successful initialisation, the application automatically navigates to the "UnauthenticatedScene" to begin the user authentication flow.

6. **Error Handling**: Comprehensive exception handling ensures that any initialisation failures are properly logged and displayed to the user.

## Running the Feature Example

### Prerequisites

Before running the Passport Initialisation example, ensure you have:

- Unity 2022.3 LTS or later installed
- The Immutable Unity SDK imported into your project
- A valid client ID from [Immutable Hub](https://hub.immutable.com/) for environment setup

### Step-by-Step Instructions

1. **Open the Sample Project**: Navigate to the `sample` directory in the Unity Immutable SDK and open it in Unity Editor.

2. **Configure Client ID**: In the `PassportInitialisationScript.cs`, ensure the `clientId` variable contains your application's client ID from Immutable Hub.

3. **Set Build Target**: 
   - For testing: Set your build target to your preferred platform (Windows, macOS, or WebGL)
   - For WebGL: Ensure you have the appropriate callback and logout HTML files in your build output

4. **Run the Scene**: 
   - Open the scene containing the `PassportInitialisationScript` component
   - Press Play in Unity Editor or build and run the application
   - Observe the initialisation process in the console and output display

5. **Verify Success**: 
   - Check that "Initialising Passport..." appears in the output
   - Confirm that the scene transitions to "UnauthenticatedScene" upon successful initialisation
   - Monitor the console for any error messages

6. **Handle Errors**: If initialisation fails, check:
   - Network connectivity
   - Client ID validity
   - Proper URI configuration for your platform

## Summary

The Passport Initialisation feature provides the essential foundation for integrating Immutable Passport into Unity games. It demonstrates proper SDK setup with platform-aware configuration, comprehensive error handling, and seamless integration with Unity's scene management system.

**Key takeaways for developers:**

- Always initialise Passport before attempting any authentication or wallet operations
- Configure platform-specific redirect URIs to ensure proper authentication flow
- Implement robust error handling to gracefully manage initialisation failures
- Use appropriate environment settings (SANDBOX for development, PRODUCTION for live applications)
- Store the Passport instance globally for access throughout your application

This initialisation pattern ensures a reliable foundation for all subsequent Passport functionality in your Unity game. 