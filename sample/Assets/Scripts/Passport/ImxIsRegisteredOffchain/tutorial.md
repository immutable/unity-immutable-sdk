<div class="display-none">

# Check Immutable X off-chain Registration Status

</div>

The IsRegisteredOffchain feature allows developers to check if a user's wallet has been registered off-chain with Immutable X. This off-chain registration is required for certain operations on Immutable X, such as trading or minting NFTs.

<div class="button-component">

[View feature on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/imxisregisteredoffchain) <span class="button-component-arrow">→</span>

</div>

## Feature Overview

The ImxIsRegisteredOffchain feature demonstrates how to check if a user's wallet is registered off-chain with Immutable X protocol using the Passport SDK.

## SDK Integration Details

This feature utilizes the `IsRegisteredOffchain()` method from the Passport SDK to determine if the current wallet is registered with Immutable X's off-chain system. 

Off-chain registration is a one-time process that's required before users can perform operations like trading assets on Immutable X. This check helps applications determine if a user needs to complete the registration process before attempting certain operations.

```csharp title="ImxIsRegisteredOffchainScript.cs" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/imxisregisteredoffchain/ImxIsRegisteredOffchainScript.cs"
private async UniTaskVoid CheckIsRegisteredOffchainAsync()
{
    if (Passport.Instance == null)
    {
        ShowOutput("Passport not initialized.");
        return;
    }

    if (!SampleAppManager.IsConnectedToImx)
    {
        ShowOutput("Please connect to Immutable X first.");
        return;
    }

    ShowOutput("Checking if registered offchain...");
    try
    {
        bool isRegistered = await SampleAppManager.PassportInstance.IsRegisteredOffchain();
        
        if (isRegistered)
        {
            ShowOutput("Registered");
        }
        else
        {
            ShowOutput("User is NOT registered offchain.");
        }
    }
    catch (System.Exception ex)
    {
        ShowOutput($"Failed to check registration: {ex.Message}");
    }
}
```

The implementation follows these steps:
1. First, it verifies that the Passport instance is initialized
2. It checks if the user is connected to Immutable X (a prerequisite for checking registration status)
3. It calls the `IsRegisteredOffchain()` method which returns a boolean indicating whether the user is registered
4. Based on the result, it displays the appropriate message to the user

## Running the Feature Example

### Prerequisites
- Unity Editor (2022.3 LTS or later recommended)
- Immutable Passport SDK imported into your project
- An Immutable Hub account (you can create one at [Immutable Hub](https://hub.immutable.com/))

### Steps to Run
1. Open the Passport sample scene in the Unity Editor
2. Connect to Passport by clicking the "Login" button 
3. Connect to Immutable X by clicking the "Connect to IMX" button
4. Navigate to the ImxIsRegisteredOffchain feature section
5. Click the "Check Registration" button to verify if the wallet is registered off-chain
6. The result will be displayed in the UI showing either "Registered" or "User is NOT registered offchain"

## Summary

The ImxIsRegisteredOffchain feature provides developers with an easy way to check if a user's wallet is registered with Immutable X's off-chain system. This verification is important before attempting operations that require off-chain registration, helping to provide a smoother user experience by guiding users to complete registration when necessary. 