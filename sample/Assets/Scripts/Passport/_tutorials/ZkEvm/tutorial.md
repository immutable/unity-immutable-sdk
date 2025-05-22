<div class="display-none">

# ZkEvm Integration

</div>

The ZkEvm feature group demonstrates how to integrate Immutable's ZkEvm blockchain capabilities into your Unity game. These features allow games to connect to the Immutable ZkEvm blockchain, perform transactions, check balances, and interact with smart contracts.

<div class="button-component">

[View feature group on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/ZkEvm) <span class="button-component-arrow">→</span>

</div>

## ZkEvm Overview

The ZkEvm feature group includes the following features:

- **ZkEvmConnect**: Connect to the Immutable ZkEvm network
- **ZkEvmSendTransaction**: Send transactions to the blockchain
- **ZkEvmGetBalance**: Check account balances
- **ZkEvmGetTransactionReceipt**: Verify transaction status
- **ZkEvmSignTypedData**: Sign structured data using EIP-712
- **ZkEvmRequestAccounts**: Request user accounts from Passport

These features work together to provide a complete integration with Immutable's ZkEvm blockchain, allowing games to leverage blockchain functionality while maintaining a seamless user experience.

## Unity SDK ZkEvm Features

### Feature: ZkEvm Connect

The ZkEvmConnect feature initializes the connection to the Immutable ZkEvm network. This is a prerequisite for all other ZkEvm operations.

```csharp title="ZkEvmConnectScript" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ZkEvm/ZkEvmConnect/ZkEvmConnectScript.cs"
public void ConnectZkEvm()
{
    ConnectZkEvmAsync();
}

private async UniTaskVoid ConnectZkEvmAsync()
{
    if (Passport.Instance == null)
    {
        ShowOutput("Passport not initialised.");
        return;
    }
    // Set the static property for global access
    SampleAppManager.PassportInstance = Passport.Instance;

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
            Debug.Log("Updated zkEVM button states after connection");
        }
        
        ShowOutput("Connected to EVM");
    }
    catch (System.Exception ex)
    {
        ShowOutput($"Failed to connect to zkEVM: {ex.Message}");
    }
}
```

This script establishes a connection to the ZkEvm network through the Passport instance. It updates the UI to reflect the connection status and handles any errors that might occur during the connection process.

### Feature: ZkEvm Send Transaction

The ZkEvmSendTransaction feature allows you to send transactions to the Immutable ZkEvm blockchain.

```csharp title="ZkEvmSendTransactionScript" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ZkEvm/ZkEvmSendTransaction/ZkEvmSendTransactionScript.cs"
private async UniTaskVoid SendTransactionAsync()
{
    if (SampleAppManager.PassportInstance == null)
    {
        ShowOutput("Passport instance is null");
        return;
    }
    // Ensure EVM provider is connected
    try
    {
        ShowOutput("Connecting to zkEVM provider...");
        await SampleAppManager.PassportInstance.ConnectEvm();
    }
    catch (Exception ex)
    {
        ShowOutput($"Failed to connect to zkEVM provider: {ex.Message}");
        return;
    }
    ShowOutput("Sending transaction...");
    try
    {
        TransactionRequest request = new TransactionRequest
        {
            to = ToInputField != null ? ToInputField.text : "",
            value = ValueInputField != null ? ValueInputField.text : "",
            data = DataInputField != null ? DataInputField.text : ""
        };
        
        if (ConfirmToggle != null && ConfirmToggle.isOn)
        {
            TransactionReceiptResponse response = await SampleAppManager.PassportInstance.ZkEvmSendTransactionWithConfirmation(request);
            ShowOutput($"Transaction hash: {response.transactionHash}\nStatus: {GetTransactionStatusString(response.status)}");
        }
        else
        {
            string transactionHash = await SampleAppManager.PassportInstance.ZkEvmSendTransaction(request);
            
            if (GetTransactionReceiptToggle != null && GetTransactionReceiptToggle.isOn)
            {
                string? status = await PollStatus(transactionHash);
                ShowOutput($"Transaction hash: {transactionHash}\nStatus: {GetTransactionStatusString(status)}");
            }
            else
            {
                ShowOutput($"Transaction hash: {transactionHash}");
            }
        }
    }
    catch (Exception ex)
    {
        ShowOutput($"Failed to send transaction: {ex.Message}");
    }
}
```

This script demonstrates sending transactions to the ZkEvm blockchain. It supports both regular transactions and transactions with confirmation, which wait for the transaction to be included in a block. Users can input the target address, value, and transaction data through the UI.

### Feature: ZkEvm Get Balance

The ZkEvmGetBalance feature retrieves the balance of a specific address on the ZkEvm blockchain.

```csharp title="ZkEvmGetBalanceScript" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ZkEvm/ZkEvmGetBalance/ZkEvmGetBalanceScript.cs"
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

This script fetches the balance of an Ethereum address on the ZkEvm network. It displays both the hexadecimal and decimal representations of the balance, after parsing the hexadecimal response from the blockchain.

### Feature: ZkEvm Get Transaction Receipt

The ZkEvmGetTransactionReceipt feature allows you to check the status of a transaction on the ZkEvm blockchain.

```csharp title="ZkEvmGetTransactionReceiptScript" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ZkEvm/ZkEvmGetTransactionReceipt/ZkEvmGetTransactionReceiptScript.cs"
private async UniTaskVoid GetTransactionReceiptAsync()
{
    if (SampleAppManager.PassportInstance == null)
    {
        ShowOutput("Passport instance is null");
        return;
    }
    ShowOutput($"Getting transaction receipt for hash: {TransactionHashInput.text}");
    try
    {
        TransactionReceiptResponse receipt = await SampleAppManager.PassportInstance.ZkEvmGetTransactionReceipt(TransactionHashInput.text);
        
        string status = receipt.status != null ? GetTransactionStatusString(receipt.status) : "Pending";
        
        ShowOutput($"Transaction Hash: {receipt.transactionHash}\nStatus: {status}\nBlock Number: {receipt.blockNumber}\nGas Used: {receipt.gasUsed}");
    }
    catch (System.Exception ex)
    {
        ShowOutput($"Failed to get transaction receipt: {ex.Message}");
    }
}
```

This script retrieves the receipt for a transaction using its hash, which provides information about the transaction's status, block number, and gas used. This is useful for verifying whether a transaction has been successfully processed by the blockchain.

### Feature: ZkEvm Sign Typed Data

The ZkEvmSignTypedData feature enables signing of structured data according to EIP-712 standard.

```csharp title="ZkEvmSignTypedDataScript" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ZkEvm/ZkEvmSignTypedData/ZkEvmSignTypedDataScript.cs"
private async UniTaskVoid SignTypedDataAsync()
{
    if (SampleAppManager.PassportInstance == null)
    {
        ShowOutput("Passport instance is null");
        return;
    }
    ShowOutput("Signing typed data...");
    try
    {
        // Prepare the EIP-712 typed data
        string typedData = TypedDataInputField.text;
        
        // Sign the typed data
        string signature = await SampleAppManager.PassportInstance.ZkEvmSignTypedDataV4(typedData);
        
        ShowOutput($"Signature: {signature}");
    }
    catch (System.Exception ex)
    {
        ShowOutput($"Failed to sign typed data: {ex.Message}");
    }
}
```

This script demonstrates how to sign structured data (following the EIP-712 standard) using the Passport SDK. This is useful for various on-chain operations where cryptographic signatures are required to prove user intent or authorization.

### Feature: ZkEvm Request Accounts

The ZkEvmRequestAccounts feature retrieves the Ethereum addresses associated with the logged-in Passport user.

```csharp title="ZkEvmRequestAccountsScript" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ZkEvm/ZkEvmRequestAccounts/ZkEvmRequestAccountsScript.cs"
private async UniTaskVoid RequestAccountsAsync()
{
    if (SampleAppManager.PassportInstance == null)
    {
        ShowOutput("Passport instance is null");
        return;
    }
    ShowOutput("Requesting accounts...");
    try
    {
        List<string> accounts = await SampleAppManager.PassportInstance.ZkEvmRequestAccounts();
        
        if (accounts.Count > 0)
        {
            string accountsString = string.Join("\n", accounts);
            ShowOutput($"Accounts:\n{accountsString}");
        }
        else
        {
            ShowOutput("No accounts found");
        }
    }
    catch (System.Exception ex)
    {
        ShowOutput($"Failed to request accounts: {ex.Message}");
    }
}
```

This script retrieves the Ethereum addresses associated with the user's Passport account. These addresses can be used for various blockchain operations such as sending transactions or signing messages.

## Running the Feature Group Examples

### Prerequisites

Before running the ZkEvm examples, you need to:

1. Set up your development environment using [Immutable Hub](https://hub.immutable.com/)
2. Have a Passport account
3. Have the Unity SDK integrated into your project

### Step-by-Step Instructions

1. Open the Unity Editor with the Immutable SDK sample project
2. Ensure you have the Passport SDK initialized properly
3. Navigate to the Passport scene in the sample project
4. Log in to Passport using the Authentication feature
5. Once authenticated, you'll be able to access the ZkEvm features
6. Try connecting to ZkEvm first, which is required for all other ZkEvm operations
7. After connecting, you can try the other ZkEvm features in any order

### Feature Sequence Dependencies

While you can use most ZkEvm features independently once connected, some operations have dependencies:

1. **ZkEvmConnect** must be called before any other ZkEvm operations
2. **ZkEvmSendTransaction** should be used before **ZkEvmGetTransactionReceipt** if you want to check a transaction you just sent
3. **ZkEvmRequestAccounts** can be useful to get the user's address before using **ZkEvmGetBalance**

## Summary

The ZkEvm feature group provides a comprehensive set of tools for integrating Immutable's ZkEvm blockchain into Unity games. By using these features, developers can enable users to connect to the blockchain, send transactions, check balances, and perform other blockchain operations directly from within their games.

When using these features together, follow these best practices:

1. Always check if Passport is initialized and the user is logged in before attempting ZkEvm operations
2. Handle exceptions properly to provide clear feedback to users when blockchain operations fail
3. Remember that blockchain operations may take time to process, especially when waiting for transaction confirmations
4. Use the TransactionReceiptResponse to verify transaction status before proceeding with dependent game logic
5. Store transaction hashes for important game transactions to allow for later verification 