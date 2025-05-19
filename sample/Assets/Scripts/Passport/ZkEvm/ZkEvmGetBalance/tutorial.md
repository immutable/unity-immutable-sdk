<div class="display-none">

# ZkEvm GetBalance

</div>

This feature demonstrates how to retrieve an account's balance from the Immutable zkEVM blockchain using the Passport SDK.

<div class="button-component">

[View feature on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/ZkEvm/ZkEvmGetBalance) <span class="button-component-arrow">→</span>

</div>

## Feature Overview

The ZkEvmGetBalance feature lets developers query the balance of any Ethereum address on the Immutable zkEVM blockchain. This is essential for games and applications that need to display user balances, check if users have sufficient funds for transactions, or monitor balance changes.

## SDK Integration Details

The feature utilizes the `ZkEvmGetBalance` method from the Passport SDK to retrieve the account balance in Wei (the smallest unit of Ether).

```csharp title="ZkEvmGetBalanceScript.cs" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ZkEvm/ZkEvmGetBalance/ZkEvmGetBalanceScript.cs"
public void GetBalance()
{
    GetBalanceAsync();
}

private async UniTaskVoid GetBalanceAsync()
{
    if (SampleAppManager.PassportInstance == null)
    {
        ShowOutput("Passport instance is null");
        return;
    }
    ShowOutput("Getting account balance...");
    try
    {
        string balanceHex = await SampleAppManager.PassportInstance.ZkEvmGetBalance(AddressInput.text);
        var balanceDec = BigInteger.Parse(balanceHex.Replace("0x", ""), NumberStyles.HexNumber);
        if (balanceDec < 0)
        {
            balanceDec = BigInteger.Parse("0" + balanceHex.Replace("0x", ""), NumberStyles.HexNumber);
        }
        ShowOutput($"Balance:\nHex: {balanceHex}\nDec: {balanceDec}");
    }
    catch (System.Exception ex)
    {
        ShowOutput($"Failed to get balance: {ex.Message}");
    }
}
```

The implementation works as follows:

1. The `GetBalance` method is called when the user initiates a balance check.
2. Inside `GetBalanceAsync`, the code first verifies that the Passport instance is available.
3. It then calls `ZkEvmGetBalance` with the address input provided by the user (the account address to check).
4. The balance is returned in hexadecimal format (e.g., "0x1a2b3c4d").
5. The code converts this hexadecimal value to a decimal representation using `BigInteger.Parse`.
6. If parsing results in a negative number (which can happen with very large hex values), the code prepends a "0" to ensure proper parsing.
7. Both the hexadecimal and decimal representations of the balance are displayed to the user.

## Running the Feature Example

### Prerequisites
- Unity Editor (2021.3 LTS or newer)
- An Immutable Passport account (create one at [Immutable Hub](https://hub.immutable.com))
- The Immutable Unity SDK properly installed and configured

### Steps to Run the Example

1. Open the sample project in Unity Editor.
2. Make sure you have properly configured the Passport SDK with your credentials.
3. Build and run the sample app.
4. Log in to your Passport account when prompted.
5. Navigate to the authenticated screen.
6. Connect to zkEVM by clicking the "Connect zkEVM" button.
7. After connecting, click the "Get Balance" button.
8. Enter an Ethereum address in the input field.
9. Submit the request to view the account balance in both hexadecimal and decimal format.

## Summary

The ZkEvmGetBalance feature provides a straightforward way to query account balances on the Immutable zkEVM blockchain. By integrating this feature, developers can enable their games and applications to check user balances, which is essential for features like displaying wallet information, verifying sufficient funds before transactions, or implementing conditional logic based on user balances. 