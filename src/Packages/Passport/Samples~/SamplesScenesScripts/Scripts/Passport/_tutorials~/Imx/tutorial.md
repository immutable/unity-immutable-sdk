<div class="display-none">

# Immutable X Integration

</div>

Immutable X is a Layer 2 scaling solution for NFTs on Ethereum that provides gas-free minting and trading. The Passport SDK enables your Unity game to integrate seamlessly with Immutable X, giving your players the ability to connect to IMX, check registration status, register if needed, retrieve wallet addresses, and transfer NFTs.

<div class="button-component">

[View feature group on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport) <span class="button-component-arrow">→</span>

</div>

## Immutable X Overview

The Imx feature group includes the following features:

- **ImxConnect**: Connect to Immutable X using saved credentials
- **ImxRegister**: Register the user with Immutable X (off-chain)
- **ImxGetAddress**: Retrieve the user's Immutable X wallet address
- **ImxNftTransfer**: Transfer NFTs to specified receivers
- **ImxIsRegisteredOffchain**: Check if the user is registered off-chain with Immutable X

These features work together to provide a complete Immutable X integration flow. First, you connect to IMX, then check if the user is registered. If not, you can register them. Once registered, you can retrieve their wallet address and perform NFT transfers.

## Unity SDK Immutable X Features

### Feature: ImxConnect

Connect users to Immutable X by initializing the user's wallet and setting up the Immutable X provider using saved credentials.

```csharp title="ImxConnectScript.cs" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ImxConnect/ImxConnectScript.cs"
public async UniTaskVoid ConnectImxAsync()
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

        // Update UI states based on connection
        var authSceneManager = FindObjectOfType<AuthenticatedSceneManager>();
        if (authSceneManager != null)
        {
            authSceneManager.UpdateImxButtonStates();
            authSceneManager.OnImxConnected?.Invoke(); 
        }
    }
    catch (System.Exception ex)
    {
        ShowOutput($"Failed to connect to IMX: {ex.Message}");
    }
}
```

The `ConnectImx` method initializes the connection to Immutable X through the Passport SDK. It uses an async operation with UniTask to connect without blocking the main thread. After successful connection, it updates the static `SampleAppManager.IsConnectedToImx` flag and notifies the scene manager to update UI elements accordingly.

### Feature: ImxIsRegisteredOffchain

Check whether a user is registered off-chain with Immutable X.

```csharp title="ImxIsRegisteredOffchainScript.cs" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ImxIsRegisteredOffchain/ImxIsRegisteredOffchainScript.cs"
private async UniTaskVoid CheckIsRegisteredOffchainAsync()
{
    if (Passport.Instance == null)
    {
        ShowOutput("Passport not initialised.");
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

This feature first verifies that the user is connected to IMX before checking if they're registered off-chain. The `IsRegisteredOffchain` method returns a boolean indicating registration status, which determines whether the user can perform operations like transferring NFTs.

### Feature: ImxRegister

Register the user with Immutable X to enable off-chain operations.

```csharp title="ImxRegisterScript.cs" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ImxRegister/ImxRegisterScript.cs"
public async void RegisterOffchain()
{
    ShowOutput("Registering off-chain...");
    try
    {
        RegisterUserResponse response = await Passport.RegisterOffchain();
        if (response != null)
        {
            ShowOutput($"Successfully registered");
        }
        else
        {
            ShowOutput("Registration failed");
        }
    }
    catch (PassportException e)
    {
        ShowOutput($"Unable to register off-chain: {e.Message} ({e.Type})");
    }
    catch (Exception e)
    {
        ShowOutput($"Unable to register off-chain {e.Message}");
    }
}
```

The `RegisterOffchain` method handles user registration with Immutable X. It returns a `RegisterUserResponse` object containing information about the registration. This step is required before users can perform operations like transferring assets on the Immutable X platform.

### Feature: ImxGetAddress

Retrieve the user's Immutable X wallet address.

```csharp title="ImxGetAddressScript.cs" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ImxGetAddress/ImxGetAddressScript.cs"
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

The `GetAddress` method retrieves the wallet address associated with the user's Immutable X account. This address is essential for identifying the user in blockchain transactions and can be used for various operations like checking balances or transaction history.

### Feature: ImxNftTransfer

Transfer NFTs to specified receivers.

```csharp title="ImxNftTransferScript.cs" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ImxNftTransfer/ImxNftTransferScript.cs"
private async UniTaskVoid TransferAsync()
{
    if (Passport.Instance == null)
    {
        ShowOutput("Passport instance is null");
        return;
    }
    if (!string.IsNullOrWhiteSpace(TokenIdInput1.text) &&
        !string.IsNullOrWhiteSpace(TokenAddressInput1.text) &&
        !string.IsNullOrWhiteSpace(ReceiverInput1.text))
    {
        ShowOutput("Transferring NFTs...");
        try
        {
            List<NftTransferDetails> transferDetails = GetTransferDetails();
            if (transferDetails.Count > 1)
            {
                CreateBatchTransferResponse response = await Passport.Instance.ImxBatchNftTransfer(transferDetails.ToArray());
                ShowOutput($"Successfully transferred {response.transfer_ids.Length} NFTs.");
            }
            else
            {
                NftTransferDetails nftTransferDetail = transferDetails[0];
                UnsignedTransferRequest transferRequest = UnsignedTransferRequest.ERC721(
                    nftTransferDetail.receiver,
                    nftTransferDetail.tokenId,
                    nftTransferDetail.tokenAddress
                );
                CreateTransferResponseV1 response = await Passport.Instance.ImxTransfer(transferRequest);
                ShowOutput($"NFT transferred successfully. Transfer ID: {response.transfer_id}");
            }
        }
        catch (System.Exception ex)
        {
            ShowOutput($"Failed to transfer NFTs: {ex.Message}");
        }
    }
    else
    {
        ShowOutput("Please fill in all required fields for the first NFT transfer.");
    }
}

private List<NftTransferDetails> GetTransferDetails()
{
    List<NftTransferDetails> details = new List<NftTransferDetails>();
    if (!string.IsNullOrWhiteSpace(TokenIdInput1.text) &&
        !string.IsNullOrWhiteSpace(TokenAddressInput1.text) &&
        !string.IsNullOrWhiteSpace(ReceiverInput1.text))
    {
        details.Add(new NftTransferDetails(
            ReceiverInput1.text,
            TokenIdInput1.text,
            TokenAddressInput1.text
        ));
    }
    if (!string.IsNullOrWhiteSpace(TokenIdInput2.text) &&
        !string.IsNullOrWhiteSpace(TokenAddressInput2.text) &&
        !string.IsNullOrWhiteSpace(ReceiverInput2.text))
    {
        details.Add(new NftTransferDetails(
            ReceiverInput2.text,
            TokenIdInput2.text,
            TokenAddressInput2.text
        ));
    }
    return details;
}
```

The `ImxNftTransfer` feature demonstrates how to transfer NFTs to specified recipients. The code supports both single and batch transfers, depending on how many inputs are provided. For batch transfers, it uses the `ImxBatchNftTransfer` method, while for single transfers, it uses the `ImxTransfer` method with an `UnsignedTransferRequest` object.

## Running the Feature Group Examples

### Prerequisites

1. Create an account on [Immutable Hub](https://hub.immutable.com/) to set up your development environment
2. Set up the Unity Editor with the Immutable Passport SDK installed
3. Configure your Passport settings with your application's client ID and redirect URLs

### Step-by-Step Instructions

1. **Connect to Immutable X**
   - Open the sample scene containing the IMX features
   - Click the "Connect" button to initialize the connection to Immutable X
   - Confirm your connection is successful when "Connected to IMX" message appears

2. **Check Registration Status**
   - After connecting, click the "Check Registration" button to verify if your account is registered off-chain
   - If the result shows "Not registered", proceed to the next step

3. **Register Off-chain**
   - Click the "Register" button to register your account with Immutable X
   - Wait for the "Successfully registered" message to confirm completion

4. **Get Wallet Address**
   - Click the "Get Address" button to retrieve your Immutable X wallet address
   - The address will be displayed in the output field

5. **Transfer NFTs**
   - Navigate to the NFT Transfer screen
   - Enter the token ID, token address, and receiver address for the NFT you want to transfer
   - Click "Transfer" to initiate the transfer
   - Wait for the success message with the transfer ID

### Feature Sequence Dependencies

For proper functionality, follow this sequence:
1. Connect to IMX first (ImxConnect)
2. Check registration status (ImxIsRegisteredOffchain)
3. Register if needed (ImxRegister)
4. Then proceed with address retrieval (ImxGetAddress) and transfers (ImxNftTransfer)

## Summary

The Immutable X feature group provides a complete integration path for your Unity game with Immutable's Layer 2 solution. By implementing these features, you enable your players to interact with blockchain assets in a gas-free, carbon-neutral environment while maintaining Ethereum's security.

When using these features together, remember to:
- Always check for IMX connection before performing operations
- Verify registration status before attempting transfers
- Handle exceptions appropriately as blockchain operations may sometimes fail
- Consider the asynchronous nature of these operations in your UI design

The Immutable X integration through Passport SDK simplifies what would otherwise be complex blockchain interactions, allowing you to focus on creating engaging gameplay while providing your players with true ownership of digital assets. 