<div class="display-none">

# IMX Connect

</div>

The IMX Connect feature allows your application to connect to Immutable X, initializing the user's wallet and setting up the Immutable X provider. This is a crucial step required before performing any Immutable X-specific operations like token transfers or NFT interactions.

<div class="button-component">

[View feature on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/imxconnect) <span class="button-component-arrow">→</span>

</div>

## Feature Overview

This atomic feature demonstrates how to connect to Immutable X using the Passport SDK.

## SDK Integration Details

The IMX Connect feature establishes a connection to Immutable X using the user's Passport credentials. It sets up the Immutable X provider, allowing your application to interact with the Immutable X blockchain.

```csharp title="IMXConnect" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/imxconnect/ImxConnectScript.cs"
public void ConnectImx()
{
    ConnectImxAsync();
}

private async UniTaskVoid ConnectImxAsync()
{
    if (Passport.Instance == null)
    {
        ShowOutput("Passport instance is null");
        return;
    }
    // Set the static property for global access
    SampleAppManager.PassportInstance = Passport.Instance;
    ShowOutput("Connecting to Passport using saved credentials...");
    try
    {
        await Passport.Instance.ConnectImx();
        
        SampleAppManager.IsConnectedToImx = true;
        ShowOutput("Connected to IMX");
    }
    catch (System.Exception ex)
    {
        ShowOutput($"Failed to connect to IMX: {ex.Message}");
    }
}
```

The code works by:

1. When `ConnectImx()` is called, it triggers the asynchronous function `ConnectImxAsync()`
2. The function first checks if a valid Passport instance exists
3. It then calls `Passport.Instance.ConnectImx()`, which:
   - Uses saved credentials if available (access token or refresh token)
   - If credentials don't exist or are invalid, it opens the user's browser for authentication via device code flow
   - Sets up the Immutable X provider after successful authentication
4. Upon successful connection, it updates the application state to reflect that the user is connected to Immutable X

## Running the Feature Example

### Prerequisites

- Set up your environment following the [Immutable Hub documentation](https://docs.immutable.com/docs/hub/setup)
- Unity Editor (2021.3 LTS or later)
- Immutable Unity SDK installed and configured

### Steps to Run the Example

1. Open the sample project in Unity Editor
2. Navigate to the Passport scene that contains the IMXConnect feature
3. Enter Play mode in the Unity Editor
4. Click the "Connect IMX" button in the sample UI
5. If you're not already logged in, a browser window will open for authentication
6. After successful authentication, you will see "Connected to IMX" in the output text

## Summary

The IMX Connect feature is essential for any application that needs to interact with the Immutable X blockchain. It handles the authentication flow and sets up the Immutable X provider, allowing your game to perform operations like token transfers and NFT interactions. This feature provides a seamless way to connect users to Immutable X while managing the complexities of authentication and provider initialization behind the scenes. 