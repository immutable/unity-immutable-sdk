<div class="display-none">

# IMX NFT Transfer

</div>

The IMX NFT Transfer feature allows developers to transfer NFTs to other accounts using the Immutable X protocol. This feature provides both single and batch transfer capabilities, enabling efficient management of digital assets within your Unity application.

<div class="button-component">

[View feature on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/ImxNftTransfer) <span class="button-component-arrow">→</span>

</div>

## Feature Overview
This example demonstrates how to use the Passport SDK to transfer NFTs on Immutable X, including:
- Single NFT transfers using `ImxTransfer`
- Batch transfers of multiple NFTs using `ImxBatchNftTransfer`

## SDK Integration Details
### Single NFT Transfer
To transfer a single NFT to another address:

```csharp title="Single NFT Transfer" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ImxNftTransfer/ImxNftTransferScript.cs"
NftTransferDetails nftTransferDetail = new NftTransferDetails(
    receiverAddress,  // Ethereum address of the receiver
    tokenId,          // ID of the NFT to transfer
    tokenAddress      // Contract address of the NFT
);

UnsignedTransferRequest transferRequest = UnsignedTransferRequest.ERC721(
    nftTransferDetail.receiver,
    nftTransferDetail.tokenId,
    nftTransferDetail.tokenAddress
);

CreateTransferResponseV1 response = await Passport.Instance.ImxTransfer(transferRequest);
```

The code creates an `UnsignedTransferRequest` specifically for ERC721 tokens (NFTs) with the helper method `ERC721()`, then calls the Passport SDK's `ImxTransfer` method to execute the transfer. The response contains the transfer ID and status information.

### Batch NFT Transfer
For transferring multiple NFTs in a single operation:

```csharp title="Batch NFT Transfer" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ImxNftTransfer/ImxNftTransferScript.cs"
List<NftTransferDetails> transferDetails = new List<NftTransferDetails>();

// Add first NFT transfer details
transferDetails.Add(new NftTransferDetails(
    receiver1,    // First receiver address
    tokenId1,     // First NFT token ID
    tokenAddress1 // First NFT contract address
));

// Add second NFT transfer details
transferDetails.Add(new NftTransferDetails(
    receiver2,    // Second receiver address
    tokenId2,     // Second NFT token ID
    tokenAddress2 // Second NFT contract address
));

// Execute batch transfer
CreateBatchTransferResponse response = await Passport.Instance.ImxBatchNftTransfer(transferDetails.ToArray());
```

The batch transfer method allows you to transfer multiple NFTs in a single call, which is more efficient than making individual transfer calls. The response contains an array of transfer IDs for each successfully transferred NFT.

## Running the Feature Example
### Prerequisites
- Unity Editor 2021.3 or higher
- [Immutable Hub](https://hub.immutable.com/) account for environment setup
- Passport SDK integrated into your Unity project

### Steps to Run the Example
1. Open the sample project in Unity Editor
2. Navigate to the `sample/Assets/Scenes/Passport/Imx/ImxNftTransfer.unity` scene
3. Ensure you have already logged in to Passport (you can use the Login feature example first)
4. Enter the required information for at least one NFT transfer:
   - Token ID: The unique identifier of the NFT
   - Token Address: The smart contract address for the NFT collection
   - Receiver: The Ethereum address of the recipient
5. Click the "Transfer" button to execute the transfer
6. Check the output message to confirm successful transfer or identify any errors

## Summary
The IMX NFT Transfer feature provides a straightforward way to transfer NFTs on Immutable X directly from your Unity application. It handles both single NFT transfers and batch operations, giving developers flexibility when building NFT-enabled games and applications. The implementation abstracts away the complexity of blockchain transactions, allowing you to focus on creating engaging user experiences. 