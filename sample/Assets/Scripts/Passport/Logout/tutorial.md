<div class="display-none">

# Logout

</div>

The Passport SDK provides a simple way to log out users from their authenticated session. This feature demonstrates how to properly implement the logout functionality in your Unity application.

<div class="button-component">

[View feature on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/logout) <span class="button-component-arrow">→</span>

</div>

## Feature Overview

This atomic example demonstrates how to implement the logout functionality in the Immutable Passport SDK, allowing users to securely end their authenticated session.

## SDK Integration Details

The logout feature provides a clean way to end a user's authenticated session. It handles both standard logout and PKCE (Proof Key for Code Exchange) authentication methods.

```csharp title="Logout Implementation" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/logout/LogoutScript.cs"
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

The implementation works as follows:

1. First, it checks if the Passport instance is available
2. Based on the authentication method used (standard or PKCE), it calls the appropriate logout method:
   - `Passport.Instance.Logout()` for standard authentication
   - `Passport.Instance.LogoutPKCE()` for PKCE authentication
3. After successful logout, it updates the application state to reflect the disconnected status
4. Finally, it navigates the user back to the unauthenticated scene
5. Error handling is implemented to catch and log any issues that occur during the logout process

## Running the Feature Example

### Prerequisites

- Unity Editor 2022.3 or later
- Immutable Unity SDK installed
- Properly configured Passport environment (see [Immutable Hub](https://hub.immutable.com/) for setup instructions)

### Steps

1. Open the sample project in Unity Editor
2. Login using your Immutable Passport in the Unauthenticated Scene.
3. In the "AuthenticatedScene" scene, enter Play mode in the Unity Editor
5. Click the "Logout" button
6. Observe that the user is successfully logged out and redirected to the unauthenticated scene

## Summary

The logout feature provides a straightforward way to end a user's authenticated session in your application. By supporting both standard and PKCE authentication methods, it ensures compatibility with different authentication flows. Properly handling the logout process is crucial for maintaining security and providing a seamless user experience in your application. 