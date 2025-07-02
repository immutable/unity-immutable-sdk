<div class="display-none">

# Set Call Timeout

</div>

The Set Call Timeout feature demonstrates how to configure the timeout duration for Passport SDK operations that use the browser communications manager. This feature allows developers to customize the timeout period based on their application's requirements and network conditions, ensuring optimal performance and reliability for Passport API calls.

<div class="button-component">

[View feature on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/Other/SetCallTimeout) <span class="button-component-arrow">→</span>

</div>

## Unity SDK Set Call Timeout Implementation

The Set Call Timeout feature provides a simple interface for configuring the timeout duration for Passport SDK operations. By default, the Passport SDK uses a 60-second timeout for browser communication calls, but this can be adjusted to accommodate different network conditions or application requirements.

```csharp title="Set Call Timeout Implementation" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/Other/SetCallTimeout/SetCallTimeoutScript.cs"
public void SetTimeout()
{
    if (Passport.Instance == null)
    {
        ShowOutput("Passport instance is null");
        return;
    }
    if (TimeoutInput == null)
    {
        Debug.LogError("[SetCallTimeoutScript] TimeoutInput is not assigned in the Inspector.");
        ShowOutput("Timeout input field is not assigned.");
        return;
    }
    if (!int.TryParse(TimeoutInput.text, out int timeout))
    {
        ShowOutput("Invalid timeout value");
        return;
    }
    Passport.Instance.SetCallTimeout(timeout);
    ShowOutput($"Set call timeout to: {timeout}ms");
}
```

The implementation validates user input to ensure a valid timeout value is provided, then calls the Passport SDK's `SetCallTimeout` method. This method configures the timeout for all subsequent Passport operations that use the browser communications manager, including authentication, blockchain transactions, and API calls.

The timeout value is specified in milliseconds and affects operations such as:
- Login and authentication flows
- IMX and zkEVM blockchain interactions  
- User information retrieval
- Token and NFT operations

Setting an appropriate timeout is crucial for balancing user experience with network reliability. Shorter timeouts provide faster feedback for failed operations, while longer timeouts accommodate slower network conditions or complex operations.

## Running the Feature Example

### Prerequisites

Before running the Set Call Timeout example, ensure you have:

- Unity 2021.3 or later installed
- The Immutable Unity SDK properly configured in your project
- Access to [Immutable Hub](https://hub.immutable.com/) for environment setup and configuration
- A properly initialized Passport instance

### Step-by-Step Instructions

1. **Open the Sample Project**
   - Navigate to the `sample` directory in the Unity Immutable SDK
   - Open the project in Unity Editor

2. **Initialize Passport**
   - Ensure Passport is properly initialized before attempting to set the timeout
   - Run the PassportInitialisation scene if needed

3. **Navigate to Set Call Timeout Scene**
   - From the AuthenticatedScene, click the "Set Call Timeout" button
   - This will load the SetCallTimeout scene with the timeout configuration interface

4. **Configure Timeout Value**
   - Enter a timeout value in milliseconds in the input field (default placeholder shows 60000)
   - The value should be a positive integer representing milliseconds
   - Common values: 30000 (30 seconds), 60000 (60 seconds), 120000 (2 minutes)

5. **Apply the Timeout Setting**
   - Click the "Set Timeout" button to apply the new timeout value
   - The system will display a confirmation message showing the applied timeout
   - The new timeout will affect all subsequent Passport SDK operations

6. **Verify the Setting**
   - Test other Passport operations to verify the timeout is working as expected
   - Monitor operation behavior under different network conditions

## Summary

The Set Call Timeout feature provides essential control over Passport SDK operation timeouts, enabling developers to optimize their applications for different network environments and use cases. By allowing customization of the timeout duration, this feature helps ensure reliable operation while maintaining responsive user experiences.

Key benefits include:
- **Network Adaptation**: Adjust timeouts based on expected network conditions
- **User Experience**: Balance responsiveness with operation reliability
- **Application Control**: Fine-tune timeout behavior for specific use cases
- **Error Handling**: Provide predictable timeout behavior for better error management

When implementing timeout configuration in production applications, consider factors such as target network conditions, operation complexity, and user expectations to determine optimal timeout values. 