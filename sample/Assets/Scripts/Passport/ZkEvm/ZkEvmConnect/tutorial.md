<div class="display-none">

# ZkEVM Connect

</div>

Connect to Immutable zkEVM to enable blockchain transactions and interactions in your Unity game. This feature allows players to connect their Passport wallet to the zkEVM network, enabling a wide range of blockchain operations including sending transactions, checking balances, and signing data.

<div class="button-component">

[View feature on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/ZkEvm/ZkEvmConnect) <span class="button-component-arrow">→</span>

</div>

## Feature Overview

This example demonstrates how to connect to the Immutable zkEVM network using the Passport SDK. The connection to zkEVM is a prerequisite for performing any other zkEVM operations like sending transactions or checking balances.

## SDK Integration Details

The ZkEvmConnect feature establishes a connection between your game and the Immutable zkEVM network through the user's Passport wallet. This connection enables subsequent blockchain operations like sending transactions, signing data, and checking balances.

```csharp title="ZkEvmConnectScript.cs" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ZkEvm/ZkEvmConnect/ZkEvmConnectScript.cs"
private async UniTaskVoid ConnectZkEvmAsync()
{
    if (Passport.Instance == null)
    {
        ShowOutput("Passport not initialized.");
        return;
    }
    
    ShowOutput("Connecting to zkEVM...");
    try
    {
        await Passport.Instance.ConnectEvm();
        
        // Update connection state and refresh UI
        SampleAppManager.IsConnectedToZkEvm = true;
        var sceneManager = FindObjectOfType<AuthenticatedSceneManager>();
        if (sceneManager != null)
        {
            sceneManager.UpdateZkEvmButtonStates();
        }
        
        ShowOutput("Connected to EVM");
    }
    catch (System.Exception ex)
    {
        ShowOutput($"Failed to connect to zkEVM: {ex.Message}");
    }
}
```

The implementation works as follows:

1. First, it checks if the Passport instance is initialized, which is a prerequisite for connecting to zkEVM.
2. Then it calls `Passport.Instance.ConnectEvm()`, which initiates the connection to the Immutable zkEVM network.
3. After successful connection, it updates the application state to reflect the connected status.
4. Finally, it updates the UI to enable buttons for other zkEVM operations that require an active connection.

## Running the Feature Example

### Prerequisites

- Unity 2021.3 LTS or newer
- Immutable SDK package imported into your project
- Passport configured with appropriate credentials from [Immutable Hub](https://hub.immutable.com)
- User must be logged in to Passport before connecting to zkEVM

### Steps to Run

1. Open the sample scene in Unity Editor
2. Enter Play mode
3. Log in to Passport if not already logged in
4. Navigate to the zkEVM section in the sample app
5. Click the "Connect to zkEVM" button
6. If successful, you'll see "Connected to EVM" in the output text area
7. Other zkEVM-related buttons will become active, indicating you can now perform those operations

## Summary

The ZkEvmConnect feature provides a straightforward way to establish a connection to the Immutable zkEVM network, which is required for any blockchain interactions on this network. Once connected, your application can perform various blockchain operations such as sending transactions, checking balances, and signing data. 