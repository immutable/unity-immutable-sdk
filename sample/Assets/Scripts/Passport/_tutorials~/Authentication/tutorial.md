<div class="display-none">

# Authentication

</div>

The Authentication feature group provides essential tools for integrating user authentication into your Unity application using the Immutable Passport SDK. These features allow players to log in, log out, and maintain their authentication state across sessions, which is fundamental for any blockchain application.

<div class="button-component">

[View feature group on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport) <span class="button-component-arrow">→</span>

</div>

## Authentication Overview

The Authentication feature group contains four key features:

- **Login**: Authenticate users using Device Code Auth or PKCE flow
- **Logout**: End a user's authenticated session
- **Relogin**: Restore a previously authenticated session using cached credentials
- **Reconnect**: Restore authentication and blockchain connections in a single operation

These features work together to create a complete authentication flow for your application. The Login and Logout features handle the primary authentication process, while Relogin and Reconnect provide convenience methods for maintaining session state across application restarts or network interruptions.

## Unity SDK Authentication Features

### Feature: Login

The Login feature allows users to authenticate with Immutable Passport using either Device Code Auth or PKCE (Proof Key for Code Exchange) authentication flows.

```csharp title="Login" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/Login/LoginScript.cs"
public async void Login()
{
    var timeoutMs = GetDeviceCodeTimeoutMs();
    string formattedTimeout = timeoutMs != null ? $"{timeoutMs} ms" : "none";
    ShowOutput($"Logging in (timeout: {formattedTimeout})...");
    try
    {
        if (SampleAppManager.UsePKCE)
        {
            await Passport.LoginPKCE();
        }
        else
        {
            await Passport.Login(timeoutMs: timeoutMs);
        }
        NavigateToAuthenticatedScene();
    }
    catch (OperationCanceledException)
    {
        ShowOutput("Failed to login: cancelled");
    }
    catch (Exception ex)
    {
        ShowOutput($"Failed to login: {ex.Message}");
    }
}
```

This implementation checks which authentication method to use based on the `SampleAppManager.UsePKCE` flag. For Device Code Auth (the default on non-WebGL platforms), it calls `Passport.Login()` with an optional timeout parameter. For PKCE auth (required for WebGL), it calls `Passport.LoginPKCE()`. Upon successful login, it navigates to the authenticated scene.

### Feature: Logout

The Logout feature ends the user's authenticated session with Immutable Passport.

```csharp title="Logout" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/Logout/LogoutScript.cs"
private async UniTaskVoid LogoutAsync()
{
    if (Passport.Instance == null)
    {
        Debug.LogError("Passport instance is null");
        return;
    }
    try
    {
        if (SampleAppManager.UsePKCE)
        {
            await Passport.Instance.LogoutPKCE();
        }
        else
        {
            await Passport.Instance.Logout();
        }
        SampleAppManager.IsConnectedToImx = false;
        SampleAppManager.IsConnectedToZkEvm = false;
        AuthenticatedSceneManager.NavigateToUnauthenticatedScene();
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"Failed to logout: {ex.Message}");
    }
}
```

Similar to the Login feature, Logout checks the authentication method and calls the appropriate logout function (`LogoutPKCE()` or `Logout()`). It also resets the connection states for IMX and zkEVM before navigating back to the unauthenticated scene.

### Feature: Relogin

The Relogin feature allows users to authenticate again using cached credentials, providing a smoother user experience for returning users.

```csharp title="Relogin" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/Relogin/ReloginScript.cs"
private async UniTaskVoid ReloginAsync()
{
    if (Passport.Instance == null)
    {
        ShowOutput("Passport Instance is null");
        return;
    }
    ShowOutput("Re-logging into Passport using saved credentials...");
    try
    {
        bool loggedIn = await Passport.Instance.Login(useCachedSession: true);
        if (loggedIn)
        {
            NavigateToAuthenticatedScene();
        }
        else
        {
            ShowOutput("Could not re-login using saved credentials");
        }
    }
    catch (System.Exception ex)
    {
        ShowOutput($"Failed to re-login: {ex.Message}");
    }
}
```

The Relogin feature calls `Passport.Instance.Login()` with the `useCachedSession` parameter set to `true`, which attempts to restore the user's previous session without requiring them to go through the full authentication flow again. If successful, it navigates to the authenticated scene.

### Feature: Reconnect

The Reconnect feature combines re-authentication with reconnecting to blockchain services (IMX) in a single operation.

```csharp title="Reconnect" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/Reconnect/ReconnectScript.cs"
private async UniTaskVoid ReconnectAsync()
{
    if (Passport.Instance == null)
    {
        ShowOutput("Passport Instance is null");
        return;
    }
    ShowOutput("Reconnecting to Passport using saved credentials...");
    try
    {
        bool connected = await Passport.Instance.ConnectImx(useCachedSession: true);
        if (connected)
        {
            // Set IMX and zkEVM state and update UI as if user clicked Connect to IMX/EVM
            SampleAppManager.IsConnectedToImx = true;
            SampleAppManager.IsConnectedToZkEvm = true;
            SampleAppManager.PassportInstance = Passport.Instance;
            var sceneManager = GameObject.FindObjectOfType<AuthenticatedSceneManager>();
            if (sceneManager != null)
            {
                sceneManager.UpdateImxButtonStates();
                sceneManager.UpdateZkEvmButtonStates();
            }
            NavigateToAuthenticatedScene();
        }
        else
        {
            ShowOutput("Could not reconnect using saved credentials");
        }
    }
    catch (System.Exception ex)
    {
        ShowOutput($"Failed to reconnect: {ex.Message}");
    }
}
```

The Reconnect feature calls `Passport.Instance.ConnectImx()` with the `useCachedSession` parameter set to `true`, which not only tries to reestablish the authentication session but also reconnects to the IMX blockchain. If successful, it updates the connection states for both IMX and zkEVM, updates the UI, and navigates to the authenticated scene.

## Running the Authentication Examples

### Prerequisites

Before running the authentication examples, you need to:

1. Set up an Immutable Hub account at [Immutable Hub](https://hub.immutable.com/)
2. Clone the Unity Immutable SDK repository
3. Open the sample app in Unity Editor (2022.3 LTS or newer recommended)
4. Ensure you have the required packages installed (UniTask, TextMeshPro)

### Step-by-Step Instructions

1. Open the sample app scene located at `sample/Assets/Scenes/Passport/InitialisationScene.unity`
2. Enter Play mode in the Unity Editor
3. In the Initialisation Scene:
   - For non-WebGL builds, choose between "Use Device Code Auth" or "Use PKCE"
   - For WebGL builds, PKCE is automatically selected
4. After initialization, you'll be taken to the Unauthenticated Scene where you can:
   - Use "Login" to authenticate with a new session
   - Use "Relogin" to try authenticating with cached credentials
   - Use "Reconnect" to authenticate and reconnect to blockchain services

### Authentication Flow Sequence

For optimal testing:
1. Start with "Login" to create a new authenticated session
2. Use the "Logout" button on the Authenticated Scene to end your session
3. Try "Relogin" to test session restoration
4. If you previously connected to IMX, try "Reconnect" to test combined authentication and blockchain reconnection

## Summary

The Authentication feature group provides a comprehensive set of tools for handling user authentication in your Unity application with Immutable Passport. It supports both Device Code Auth and PKCE authentication methods, allowing for cross-platform compatibility including WebGL builds.

### Best Practices

- Initialize Passport before attempting any authentication operations
- Handle authentication exceptions appropriately in your implementation
- For WebGL applications, always use PKCE authentication
- For returning users, try the Relogin or Reconnect features before falling back to a full Login
- Always check if the Passport instance exists before attempting operations
- Clear connection states when logging out to maintain proper application state

These authentication features provide the foundation for all other Immutable operations in your Unity application, as users must be authenticated before interacting with blockchain services like IMX and zkEVM. 