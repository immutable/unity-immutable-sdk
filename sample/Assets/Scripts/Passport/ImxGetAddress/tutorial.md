<div class="display-none">

# IMX Get Address

</div>

Get the connected Immutable Wallet address quickly and easily.

<div class="button-component">

[View feature on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/ImxGetAddress) <span class="button-component-arrow">→</span>

</div>

## Feature Overview

The IMX Get Address feature allows developers to retrieve the Immutable wallet address of the currently logged-in user. This is a fundamental operation for many blockchain applications that need to identify the user's wallet address to perform transactions, display balances, or interact with smart contracts.

## SDK Integration Details

This feature provides a simple way to retrieve the user's wallet address through the Passport SDK. The implementation is straightforward: simply call `GetAddress()` on the Passport instance.

```csharp title="ImxGetAddressScript.cs" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ImxGetAddress/ImxGetAddressScript.cs"
/// <summary>
/// Gets the wallet address of the currently logged-in user.
/// </summary>
public async void GetAddress()
{
    ShowOutput("Retrieving wallet address...");
    try
    {
        string address = await Passport.GetAddress();
        ShowOutput(string.IsNullOrEmpty(address) ? "No address found" : address);
    }
    catch (PassportException e)
    {
        ShowOutput($"Unable to retrieve address: {e.Message} ({e.Type})");
    }
    catch (Exception)
    {
        ShowOutput("Unable to retrieve address");
    }
}
```

### How it works

The code performs these steps:
1. Calls the asynchronous `GetAddress()` method from the Passport SDK
2. Awaits the response which returns the wallet address as a string
3. Handles potential exceptions that might occur during the request
4. Displays the address (or an error message) to the user

This method requires the user to be authenticated with Passport first, so make sure your application handles login before calling this method.

## Running the Feature Example

### Prerequisites
- Unity Editor 2022.3 or later
- [Immutable Hub](https://hub.immutable.com/) account for environment setup

### Step-by-step instructions
1. Clone the Unity Immutable SDK repository
2. Open the sample project in Unity Editor
3. Navigate to SelectAuthMethod and login using Immutable Passport
4. In the UnauthenticatedScene, first click "Connect" to authenticate with Passport and connect to the IMX Provider
5. Once logged in, locate and click the "Get IMX Address" button
6. The wallet address will be displayed in the output area

## Summary

The IMX Get Address feature provides a simple way to retrieve a user's Immutable wallet address. This functionality is essential for applications that need to identify users, initiate transactions, or interact with blockchain data. By integrating this feature, developers can seamlessly connect their Unity applications with users' Immutable wallets. 