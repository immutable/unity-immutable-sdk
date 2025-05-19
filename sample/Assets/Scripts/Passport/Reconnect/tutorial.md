<div class="display-none">

# Reconnect

</div>

Easily reconnect to Passport using saved credentials for a seamless user experience without requiring users to log in again.

<div class="button-component">

[View feature on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/Reconnect) <span class="button-component-arrow">→</span>

</div>

## Feature Overview
The Reconnect feature allows you to reconnect a user to Passport by utilizing previously saved credentials, eliminating the need for users to authenticate again when they return to your application. This enhances user experience by providing a seamless, frictionless re-entry into your application.

## SDK Integration Details
The Passport SDK provides a straightforward way to reconnect users with their saved credentials through the `ConnectImx` method with the `useCachedSession` parameter set to `true`.

```csharp title="Reconnect" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/Reconnect/ReconnectScript.cs"
public void Reconnect()
{
    ReconnectAsync();
}

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

The reconnect process works by:
1. Calling `ConnectImx` with `useCachedSession` set to `true`, which attempts to use saved credentials
2. If successful, updating the application state to reflect the authenticated user
3. Navigating to the authenticated scene
4. If unsuccessful, showing an appropriate error message

Behind the scenes, the SDK checks for saved credentials and automatically refreshes the authentication tokens if necessary, without requiring any user interaction.

## Running the Feature Example
### Prerequisites
- Unity Editor 2021.3 or later
- Immutable SDK installed in your project
- A registered application in [Immutable Hub](https://hub.immutable.com)
- Configured environment variables (Client ID, etc.)

### Steps to Run
1. Open the sample project in Unity Editor
2. Navigate to the Passport scene
3. Ensure you have previously logged in at least once to create saved credentials
4. Click the "Reconnect" button in the Passport demo UI
5. The app will attempt to reconnect using saved credentials
6. If successful, you'll be redirected to the authenticated scene

## Summary
The Reconnect feature provides a seamless way to improve user experience by allowing returning users to bypass the login process. By utilizing saved credentials, your application can provide a frictionless authentication experience that reduces friction and encourages user retention. This approach is particularly valuable for games and applications where maintaining engagement is critical. 