<div class="display-none">

# Request Accounts

</div>

Retrieve wallet addresses from the user's Immutable Passport account using the ZkEvmRequestAccounts feature. This feature allows your Unity application to access the Ethereum addresses associated with the logged-in user's Passport account.

<div class="button-component">

[View feature on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/ZkEvm/ZkEvmRequestAccounts) <span class="button-component-arrow">→</span>

</div>

## Feature Overview

The ZkEvmRequestAccounts feature provides a simple way to request the list of Ethereum addresses associated with the user's Immutable Passport account. This is a crucial step for any application that needs to interact with the user's blockchain wallet or perform blockchain operations on their behalf.

## SDK Integration Details

The ZkEvmRequestAccounts feature allows you to retrieve the Ethereum addresses from the user's Passport account using a simple asynchronous call.

```csharp title="ZkEvmRequestAccountsScript" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ZkEvm/ZkEvmRequestAccounts/ZkEvmRequestAccountsScript.cs"
private async UniTaskVoid RequestAccountsAsync()
{
    if (SampleAppManager.PassportInstance == null)
    {
        ShowOutput("Passport not initialized.");
        return;
    }

    ShowOutput("Requesting wallet accounts...");
    try
    {
        List<string> accounts = await SampleAppManager.PassportInstance.ZkEvmRequestAccounts();
        ShowOutput(accounts.Count > 0 ? string.Join(", ", accounts) : "No accounts found.");
    }
    catch (System.Exception ex)
    {
        ShowOutput($"Failed to request wallet accounts: {ex.Message}");
    }
}
```

The code works by:

1. Checking if the Passport instance is properly initialized
2. Making an asynchronous call to `ZkEvmRequestAccounts()` method from the Passport SDK
3. Receiving a list of Ethereum addresses associated with the user's account
4. Displaying the results or handling any errors that occur during the process

This feature should be used after a user has successfully logged in to Passport and before performing any blockchain operations that require their wallet address.

## Running the Feature Example

### Prerequisites

Before running the feature example, ensure you have:

1. Set up your Immutable Passport application on [Immutable Hub](https://hub.immutable.com)
2. Configured your Unity project with the Immutable Passport SDK
3. Successfully initialized Passport in your application
4. User must be logged in to Passport

### Steps to Run

1. Open the sample Unity project in the Unity Editor
2. Navigate to the Scenes folder, open the "AuthenticatedScene" scene, and click on the "Connect to EVM" button. 
3. Then, click on the "Request accounts" button containing the ZkEvmRequestAccounts example
4. Log in to your Passport account if not already logged in
5. Click the "Request Accounts" button in the UI
6. The addresses associated with the user's Passport account will be displayed in the output area

## Summary

The ZkEvmRequestAccounts feature provides a straightforward way to retrieve Ethereum addresses associated with the user's Passport account. This feature is essential for any dApp that needs to interact with the blockchain on behalf of the user, as it provides the necessary wallet addresses for transaction signing and other operations. 