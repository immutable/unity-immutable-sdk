<div class="display-none">

# ZkEvm Features

</div>

The ZkEvm feature group demonstrates how to interact with Immutable's zkEVM blockchain through the Passport SDK. These features enable developers to perform essential blockchain operations including connecting to the zkEVM provider, sending transactions, checking balances, retrieving transaction receipts, signing typed data, and requesting account information.

<div class="button-component">

[View feature group on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/ZkEvm) <span class="button-component-arrow">→</span>

</div>

## ZkEvm Overview

The ZkEvm feature group includes six interconnected features that work together to provide comprehensive zkEVM blockchain functionality:

- **ZkEvmConnect** - Establishes connection to the zkEVM provider
- **ZkEvmSendTransaction** - Sends transactions with optional confirmation and receipt polling
- **ZkEvmGetBalance** - Retrieves account balance in both hex and decimal formats
- **ZkEvmGetTransactionReceipt** - Gets transaction status and detailed receipt information
- **ZkEvmSignTypedData** - Signs EIP-712 structured data for secure message verification
- **ZkEvmRequestAccounts** - Retrieves list of wallet addresses associated with the user

These features follow a common pattern where `ZkEvmConnect` must be called first to establish the provider connection, after which other features can perform their specific blockchain operations.

## Unity SDK ZkEvm Features

### Feature: ZkEvmConnect

The ZkEvmConnect feature establishes a connection to the zkEVM provider, which is required before any other zkEVM operations can be performed. This feature updates the global connection state and refreshes the UI to enable other zkEVM features.

```csharp title="ZkEvmConnect" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ZkEvm/ZkEvmConnect/ZkEvmConnectScript.cs"
private async UniTaskVoid ConnectZkEvmAsync()
{
    if (Passport.Instance == null)
    {
        ShowOutput("Passport not initialised.");
        return;
    }
    
    SampleAppManager.PassportInstance = Passport.Instance;
    ShowOutput("Connecting to zkEVM...");
    
    try
    {
        await Passport.Instance.ConnectEvm();
        
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

This implementation calls `Passport.Instance.ConnectEvm()` to establish the provider connection, then updates the global connection state and UI. The connection is essential for all subsequent zkEVM operations.

### Feature: ZkEvmSendTransaction

The ZkEvmSendTransaction feature enables sending transactions to the zkEVM network with flexible options for confirmation and receipt polling. It supports both immediate transaction submission and confirmed transaction processing.

```csharp title="ZkEvmSendTransaction" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ZkEvm/ZkEvmSendTransaction/ZkEvmSendTransactionScript.cs"
private async UniTaskVoid SendTransactionAsync()
{
    await SampleAppManager.PassportInstance.ConnectEvm();
    
    TransactionRequest request = new TransactionRequest
    {
        to = ToInputField != null ? ToInputField.text : "",
        value = ValueInputField != null ? ValueInputField.text : "",
        data = DataInputField != null ? DataInputField.text : ""
    };

    if (ConfirmToggle != null && ConfirmToggle.isOn)
    {
        TransactionReceiptResponse response = await SampleAppManager.PassportInstance.ZkEvmSendTransactionWithConfirmation(request);
        ShowOutput($"Transaction hash: {response.hash}\nStatus: {GetTransactionStatusString(response.status)}");
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
```

The feature provides two transaction modes: `ZkEvmSendTransaction` for immediate submission and `ZkEvmSendTransactionWithConfirmation` for confirmed transactions. It also includes status polling functionality to track transaction completion.

### Feature: ZkEvmGetBalance

The ZkEvmGetBalance feature retrieves the balance of any Ethereum address on the zkEVM network, displaying results in both hexadecimal and decimal formats for developer convenience.

```csharp title="ZkEvmGetBalance" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ZkEvm/ZkEvmGetBalance/ZkEvmGetBalanceScript.cs"
private async UniTaskVoid GetBalanceAsync()
{
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

This implementation uses `ZkEvmGetBalance` to retrieve the balance and includes proper hex-to-decimal conversion with error handling for edge cases.

### Feature: ZkEvmGetTransactionReceipt

The ZkEvmGetTransactionReceipt feature retrieves detailed information about a specific transaction, including its status and execution details, which is essential for transaction verification and monitoring.

```csharp title="ZkEvmGetTransactionReceipt" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ZkEvm/ZkEvmGetTransactionReceipt/ZkEvmGetTransactionReceiptScript.cs"
private async UniTaskVoid GetZkEvmTransactionReceiptAsync()
{
    ShowOutput("Getting transaction receipt...");
    try
    {
        await Passport.Instance.ConnectEvm();
        TransactionReceiptResponse response = await Passport.Instance.ZkEvmGetTransactionReceipt(TransactionHash.text);
        string status = $"Status: {GetTransactionStatusString(response.status)}";
        ShowOutput(status);
    }
    catch (System.Exception ex)
    {
        ShowOutput($"Failed to retrieve transaction receipt: {ex.Message}");
    }
}

private string GetTransactionStatusString(string status)
{
    switch (status)
    {
        case "1":
        case "0x1":
            return "Success";
        case "0":
        case "0x0":
            return "Failed";
        case null:
            return "Still processing";
        default:
            return "Unknown status";
    }
}
```

The feature includes status interpretation logic to convert blockchain status codes into human-readable formats, making transaction monitoring more intuitive.

### Feature: ZkEvmSignTypedData

The ZkEvmSignTypedData feature enables signing of EIP-712 structured data, which is essential for secure message verification and authentication in blockchain applications.

```csharp title="ZkEvmSignTypedData" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ZkEvm/ZkEvmSignTypedData/ZkEvmSignTypedDataScript.cs"
private async UniTaskVoid SignTypedDataAsync()
{
    ShowOutput("Signing payload...");
    try
    {
        await Passport.Instance.ConnectEvm();
        string signature = await Passport.Instance.ZkEvmSignTypedDataV4(Payload.text);
        ShowOutput(signature);
    }
    catch (System.Exception ex)
    {
        ShowOutput($"Failed to sign typed data: {ex.Message}");
    }
}
```

This implementation uses `ZkEvmSignTypedDataV4` to sign EIP-712 structured data, following the latest EIP-712 specification for secure message signing.

### Feature: ZkEvmRequestAccounts

The ZkEvmRequestAccounts feature retrieves all wallet addresses associated with the authenticated user, providing essential account information for blockchain operations.

```csharp title="ZkEvmRequestAccounts" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ZkEvm/ZkEvmRequestAccounts/ZkEvmRequestAccountsScript.cs"
private async UniTaskVoid RequestAccountsAsync()
{
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

The feature returns a list of account addresses and handles cases where no accounts are found, providing clear feedback to the user.

## Running the Feature Group Examples

### Prerequisites

- Unity 2022.3 LTS or later
- Immutable Passport SDK integrated into your project
- Valid Passport configuration with zkEVM environment setup
- Environment setup completed through [Immutable Hub](https://hub.immutable.com/)

### Step-by-Step Instructions

1. **Open the Sample Project**: Load the Unity sample project containing the ZkEvm features

2. **Configure Passport**: Ensure your Passport instance is properly configured for zkEVM operations

3. **Authenticate User**: Complete user authentication through Passport before accessing zkEVM features

4. **Connect to zkEVM**: 
   - Navigate to the ZkEvmConnect feature
   - Click "Connect" to establish the zkEVM provider connection
   - Verify successful connection before proceeding to other features

5. **Test Individual Features**:
   - **Get Accounts**: Use ZkEvmRequestAccounts to retrieve user wallet addresses
   - **Check Balance**: Use ZkEvmGetBalance with a valid address to check account balance
   - **Send Transaction**: Use ZkEvmSendTransaction to send a test transaction (ensure sufficient balance)
   - **Get Receipt**: Use ZkEvmGetTransactionReceipt with a transaction hash to check status
   - **Sign Data**: Use ZkEvmSignTypedData with valid EIP-712 structured data

6. **Sequence Dependencies**: 
   - Always run ZkEvmConnect first
   - ZkEvmRequestAccounts should be run early to get available addresses
   - ZkEvmGetBalance requires a valid address (from ZkEvmRequestAccounts)
   - ZkEvmGetTransactionReceipt requires a transaction hash (from ZkEvmSendTransaction)

## Summary

The ZkEvm feature group provides comprehensive blockchain functionality for Unity applications using the Immutable Passport SDK. These features demonstrate essential patterns for zkEVM integration, including provider connection, transaction management, balance checking, receipt verification, message signing, and account management.

Key best practices when using these features together:
- Always establish the zkEVM connection first using ZkEvmConnect
- Handle async operations properly with UniTask for optimal performance
- Implement comprehensive error handling for blockchain operations
- Use the confirmation and polling features for reliable transaction processing
- Validate user inputs and provide clear feedback for all operations

These features provide the foundation for building robust blockchain-enabled Unity applications on Immutable's zkEVM platform. 