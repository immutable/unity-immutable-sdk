<div class="display-none">

# Login

</div>

## Introduction
The Login feature demonstrates how to authenticate a user using the Immutable Passport SDK in Unity. This atomic example focuses on the login process, showing how to trigger authentication and handle its result in a Unity scene.

This feature leverages the Passport SDK's login capabilities, allowing users to authenticate via PKCE or device code flow.

<div class="button-component">

[View feature on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/login) <span class="button-component-arrow">→</span>

</div>

## Feature Overview
This atomic example demonstrates the **Login** feature of the Passport SDK, focusing solely on the user authentication process.

> **Note:** This tutorial covers only the login logic. Passport SDK initialization and unrelated features are excluded.

## SDK Integration Details
The Login feature enables user authentication using either PKCE or device code flow, depending on the configuration. The implementation provides feedback to the user and navigates to an authenticated scene upon success.

```csharp title="LoginScript.cs" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/login/LoginScript.cs"
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

**How it works:**
- The `Login` method checks which authentication method to use (PKCE or device code).
- It calls the appropriate Passport SDK login method.
- On success, it navigates to the authenticated scene.
- On failure, it displays an error message to the user.

## Running the Feature Example
- **Prerequisites:**
  - Unity Editor installed
  - Immutable Passport SDK integrated
  - [Immutable Hub](https://hub.immutable.com/) account and environment setup
- **Steps:**
  1. Open the project in Unity Editor.
  2. Navigate to the Login feature scene.
  3. Enter a device code timeout (optional).
  4. Click the login button to trigger authentication.
  5. On success, you will be redirected to the authenticated scene.

## Summary
This tutorial demonstrated the Passport SDK's Login feature in Unity, focusing on user authentication via PKCE or device code. Key takeaways:
- Simple integration of Passport login methods
- Clear user feedback and error handling
- Scene navigation upon successful authentication 