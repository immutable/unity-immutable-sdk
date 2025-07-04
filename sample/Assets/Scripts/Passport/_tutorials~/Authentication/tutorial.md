<div class="display-none">

# Authentication

</div>

The Authentication feature group demonstrates the core authentication capabilities of the Immutable Passport SDK. These features enable secure user authentication and session management in Unity games, providing seamless login experiences across different platforms and maintaining user sessions between game sessions.

<div class="button-component">

[View feature group on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport) <span class="button-component-arrow">→</span>

</div>

## Authentication Overview

The Authentication feature group includes four essential features that work together to provide a complete authentication system:

- **Login**: Primary authentication using PKCE (Proof Key for Code Exchange) flow
- **Logout**: Secure session termination and credential cleanup  
- **Relogin**: Silent re-authentication using cached credentials
- **Reconnect**: Re-authentication with automatic IMX provider setup

These features work together to create a seamless authentication experience. Login establishes the initial session, Relogin provides quick re-authentication without user interaction, Reconnect combines re-authentication with IMX connectivity, and Logout ensures secure session cleanup.

## Unity SDK Authentication Features

### Feature: Login

The Login feature implements the primary authentication flow using PKCE (Proof Key for Code Exchange), which opens the user's default browser on desktop or an in-app browser on mobile platforms for secure authentication.

```csharp title="Login Implementation" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/Login/LoginScript.cs"
public async void Login()
{
    try
    {
        await Passport.Login();
        SceneManager.LoadScene("AuthenticatedScene");
    }
    catch (OperationCanceledException ex)
    {
        ShowOutput($"Failed to login: cancelled {ex.Message}\\n{ex.StackTrace}");
    }
    catch (Exception ex)
    {
        ShowOutput($"Failed to login: {ex.Message}");
    }
}
```

The Login method uses the Passport SDK's PKCE authentication flow, which provides enhanced security by generating a code verifier and challenge. When successful, the user is automatically navigated to the authenticated scene where they can access protected features.

### Feature: Logout

The Logout feature securely terminates the user's session and cleans up stored credentials, ensuring proper session management and security.

```csharp title="Logout Implementation" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/Logout/LogoutScript.cs"
public void Logout()
{
    LogoutAsync();
}

private async UniTaskVoid LogoutAsync()
{
    if (Passport.Instance == null)
    {
        Debug.LogError("Passport instance is null");
        return;
    }
    try
    {
        await Passport.Instance.Logout();
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

The Logout implementation not only calls the Passport logout method but also resets the application's connection states for both IMX and zkEVM, ensuring a clean slate for the next authentication session.

### Feature: Relogin

The Relogin feature enables silent re-authentication using previously stored credentials, providing a smooth user experience by avoiding repeated login prompts when credentials are still valid.

```csharp title="Relogin Implementation" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/Relogin/ReloginScript.cs"
public void Relogin()
{
    ReloginAsync();
}

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

The Relogin feature uses the `useCachedSession: true` parameter to attempt authentication with stored credentials. This provides a seamless experience for returning users while gracefully handling cases where credentials may have expired.

### Feature: Reconnect

The Reconnect feature combines re-authentication with automatic IMX provider setup, streamlining the process of restoring both authentication state and blockchain connectivity.

```csharp title="Reconnect Implementation" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/Reconnect/ReconnectScript.cs"
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

The Reconnect feature uses `ConnectImx(useCachedSession: true)` to both authenticate the user and establish the IMX connection in a single operation. It also updates the UI state to reflect the successful connection to both IMX and zkEVM networks.

## Running the Feature Group Examples

### Prerequisites

Before running the authentication examples, ensure you have:

- Unity 2021.3 or later installed
- The Immutable Unity SDK properly configured in your project
- Access to [Immutable Hub](https://hub.immutable.com/) for environment setup and configuration
- A valid Passport client ID configured in your project

### Step-by-Step Instructions

1. **Open the Sample Project**
   - Navigate to the `sample` directory in the Unity Immutable SDK
   - Open the project in Unity Editor

2. **Configure Passport Settings**
   - Ensure your Passport client ID is properly set in the `PassportInitialisationScript.cs`
   - Verify the redirect URIs match your application configuration

3. **Run the Authentication Flow**
   - Start with the PassportInitialisation scene to initialize the SDK
   - The application will automatically navigate to the UnauthenticatedScene
   - Test the Login feature by clicking the "Login" button
   - After successful authentication, you'll be redirected to the AuthenticatedScene

4. **Test Session Management**
   - Use the Logout feature to terminate your session
   - Return to the UnauthenticatedScene and test the Relogin feature
   - Test the Reconnect feature to verify IMX connectivity restoration

5. **Verify State Management**
   - Check that connection states (IMX/zkEVM) are properly updated
   - Ensure UI elements reflect the current authentication and connection status

### Sequence Dependencies

The authentication features should be tested in this recommended sequence:
1. **Login** - Establish initial authentication
2. **Logout** - Test session termination
3. **Relogin** - Test cached credential authentication
4. **Reconnect** - Test authentication with IMX connectivity

## Summary

The Authentication feature group provides a comprehensive authentication system for Unity games using the Immutable Passport SDK. The four features work together to cover all aspects of user session management:

- **Login** handles initial user authentication using secure PKCE flow
- **Logout** ensures proper session cleanup and security
- **Relogin** provides seamless re-authentication for returning users
- **Reconnect** combines authentication with blockchain connectivity setup

### Best Practices

When implementing these authentication features:

- Always check for null Passport instances before making authentication calls
- Implement proper error handling for network issues and authentication failures
- Update application state consistently after authentication state changes
- Use the cached session options appropriately to improve user experience
- Ensure UI state reflects the current authentication and connection status

### Key Takeaways

- The PKCE authentication flow provides enhanced security for OAuth 2.0 authentication
- Cached sessions enable seamless re-authentication without user interaction
- Proper state management is crucial for maintaining consistent application behavior
- The Reconnect feature streamlines the process of restoring both authentication and blockchain connectivity
- All authentication operations are asynchronous and require proper exception handling 