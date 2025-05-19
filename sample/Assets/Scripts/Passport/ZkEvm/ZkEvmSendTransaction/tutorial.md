<div class="display-none">

# ZkEvm Send Transaction

</div>

Send transactions to the Immutable zkEVM network from your Unity game using Passport SDK. This feature allows your users to sign and submit transactions to execute smart contract methods or transfer assets.

<div class="button-component">

[View feature on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/ZkEvm/ZkEvmSendTransaction) <span class="button-component-arrow">→</span>

</div>

## Feature Overview
This Passport feature demonstrates how to send transactions to the Immutable zkEVM network using the authenticated Passport user's account. It provides two transaction modes:
- Basic transaction submission (`ZkEvmSendTransaction`)
- Transaction with confirmation (`ZkEvmSendTransactionWithConfirmation`)

## SDK Integration Details

### Sending a Transaction

The Passport SDK provides a simple way to send transactions to the zkEVM network:

```csharp title="ZkEvmSendTransactionScript.cs" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ZkEvm/ZkEvmSendTransaction/ZkEvmSendTransactionScript.cs"
// Create transaction request
TransactionRequest request = new TransactionRequest
{
    to = ToInputField.text,  // Destination address
    value = ValueInputField.text,  // Amount of IMX to send (in wei)
    data = DataInputField.text  // Transaction data (for contract interactions)
};

// Send transaction without confirmation
string transactionHash = await SampleAppManager.PassportInstance.ZkEvmSendTransaction(request);
```

This method returns a transaction hash immediately after submission, allowing your application to continue while the transaction is being processed by the network.

### Sending a Transaction with Confirmation

For use cases where you need to know the transaction result before proceeding:

```csharp title="ZkEvmSendTransactionScript.cs" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ZkEvm/ZkEvmSendTransaction/ZkEvmSendTransactionScript.cs"
// Send transaction with confirmation and display transaction status upon completion
TransactionReceiptResponse response = await SampleAppManager.PassportInstance.ZkEvmSendTransactionWithConfirmation(request);
```

This method waits for the transaction to be included in a block and returns the transaction receipt, which includes the transaction status (success or failure).

### Checking Transaction Status

You can also manually poll for transaction status using the transaction hash:

```csharp title="ZkEvmSendTransactionScript.cs" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ZkEvm/ZkEvmSendTransaction/ZkEvmSendTransactionScript.cs"
// Poll for the receipt and display transaction status
TransactionReceiptResponse response = await SampleAppManager.PassportInstance.ZkEvmGetTransactionReceipt(transactionHash);
```

## Running the Feature Example

### Prerequisites
- Set up your application with the Immutable SDK using [Immutable Hub](https://hub.immutable.com)
- Complete the Passport login process

### Steps to Run in Unity Editor
1. Navigate to the ZkEvmSendTransaction scene in `sample/Assets/Scenes/Passport/ZkEvm/ZkEvmSendTransaction.unity`
2. Run the scene in the Unity Editor
3. Enter the following in the form:
   - **To**: The destination address (e.g., another wallet address)
   - **Value**: Amount to send in wei (e.g., "1000000000000000" for 0.001 IMX)
   - **Data**: Optional data for contract interactions
4. Select whether to wait for confirmation using the checkbox
5. Click "Send Transaction" to execute the transaction
6. View the transaction result displayed in the UI

## Summary
The ZkEvmSendTransaction feature enables your game to interact with the Immutable zkEVM blockchain by sending transactions from the user's Passport wallet. This allows for implementing various blockchain operations like token transfers, smart contract interactions, and more. The ability to either fire-and-forget transactions or wait for confirmations gives developers flexibility to implement different user experience flows based on their game's requirements. 