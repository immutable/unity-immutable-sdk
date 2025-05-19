<div class="display-none">

# Relogin

</div>

Use saved credentials to re-login to Passport, allowing users to seamlessly resume their session without requiring re-authentication.

<div class="button-component">

[View feature on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/Relogin) <span class="button-component-arrow">→</span>

</div>

## Feature Overview

The Relogin feature demonstrates how to use existing cached credentials to log back into Passport without requiring users to authenticate again. This is particularly useful for improving user experience after app restarts or temporary disconnections.

## SDK Integration Details

The Relogin functionality leverages the Passport SDK's ability to use cached sessions. When implemented, it allows the application to attempt logging in using previously stored credentials.

```csharp title="ReloginScript.cs" manualLink="https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/Relogin/ReloginScript.cs"
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

The key part of this implementation is passing `useCachedSession: true` to the `Passport.Instance.Login()` method. This parameter instructs the SDK to attempt logging in using previously saved credentials instead of triggering a new authentication flow.

When successful, the user is automatically logged in and redirected to the authenticated experience. If the cached credentials are invalid or expired, the login attempt will fail, and the application can then prompt for manual login.

## Running the Feature Example

### Prerequisites
- Unity Editor 2022.3 or later
- Immutable Unity SDK installed
- Environment set up using [Immutable Hub](https://hub.immutable.com)

### Steps
1. Open the sample project in Unity Editor
2. Navigate to the Scenes folder and open the Unauthenticated scene
3. In the Hierarchy panel, locate and select the Relogin button
4. Play the scene in the Editor
5. First login normally to create cached credentials
6. Stop the playback and start it again
7. Click the "Relogin" button to test the re-login functionality

## Summary

The Relogin feature provides a seamless way to maintain user sessions across app restarts or reconnections by leveraging cached credentials. By using the `useCachedSession: true` parameter with the Login method, developers can create a frictionless authentication experience for returning users. 