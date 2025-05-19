<div class="display-none">

# ZkEVM Sign Typed Data

</div>

Sign EIP-712 structured data using the Passport SDK, enabling secure and user-friendly off-chain message signing for on-chain use.

<div class="button-component">

[View feature on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/ZkEvm/ZkEvmSignTypedData) <span class="button-component-arrow">→</span>

</div>

## Feature Overview

The ZkEVM Sign Typed Data feature demonstrates how to sign EIP-712 structured data messages using the Immutable Passport SDK. EIP-712 is a standard for hashing and signing of typed structured data as opposed to just byte strings, making off-chain message signing more secure and user-friendly.

## SDK Integration Details

The ZkEvmSignTypedDataV4 method in the Passport SDK allows developers to sign EIP-712 structured messages in JSON string format with the logged-in user's Passport account.

```csharp title="ZkEvmSignTypedData" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/ZkEvm/ZkEvmSignTypedData/ZkEvmSignTypedDataScript.cs"
private async UniTaskVoid SignTypedDataAsync()
{
    if (Passport.Instance == null)
    {
        ShowOutput("Passport instance is null");
        return;
    }
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

The code above demonstrates the following process:
1. First, it ensures the Passport instance is initialized
2. It connects to the EVM network using `ConnectEvm()`
3. It calls the `ZkEvmSignTypedDataV4()` method, passing in the EIP-712 formatted payload as a JSON string
4. The method returns a signature string that can be used for verification purposes

The payload follows the EIP-712 standard JSON format, which includes:
- A `types` object defining the structure of your data
- A `domain` object with chain-specific information to prevent cross-chain replay attacks
- A `primaryType` field indicating the main struct being signed
- A `message` object containing the actual data to sign

## Running the Feature Example

### Prerequisites
- Unity Editor (2022.3 LTS or newer)
- Immutable Passport SDK installed
- [Immutable Hub](https://hub.immutable.com/) account for environment setup

### Steps to Run
1. Open the sample project in Unity Editor
2. Navigate to the `/Assets/Scenes/Passport/ZkEvm/ZkEvmSignTypedData.unity` scene
3. Enter your EIP-712 formatted payload in the text field
   - Ensure it's in valid JSON format following the EIP-712 standard
4. Click the "Sign Typed Data" button
5. The signature will be displayed in the output field if successful

## Summary

The ZkEVM Sign Typed Data feature allows developers to implement secure message signing in their games using the standardized EIP-712 format. This enables various use cases like:
- User authentication
- Proving ownership of assets
- Creating off-chain orders for on-chain execution
- Implementing gasless transactions with meta-transactions

By using structured, typed data instead of opaque hex strings, users get a clearer understanding of what they're signing, enhancing the security and usability of your blockchain integration. 