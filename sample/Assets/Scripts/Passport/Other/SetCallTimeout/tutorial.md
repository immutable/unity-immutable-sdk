<div class="display-none">

# Set Call Timeout

</div>

The Set Call Timeout feature allows developers to configure the timeout duration for calls made through the Passport SDK's browser communications manager. This provides greater control over network operations and improves user experience by allowing customized timeout handling.

<div class="button-component">

[View feature on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/Other/SetCallTimeout) <span class="button-component-arrow">→</span>

</div>

## Feature Overview

This feature demonstrates how to use the `SetCallTimeout` method from the Passport SDK to customize the timeout period for API calls. The timeout is specified in milliseconds and applies to all subsequent calls that use the browser communications manager.

## SDK Integration Details

The SetCallTimeout feature is implemented with a simple UI that allows users to input a custom timeout value in milliseconds. When the user enters a value and confirms the action, the timeout is set using the Passport SDK.

```csharp title="SetCallTimeoutScript" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/Other/SetCallTimeout/SetCallTimeoutScript.cs"
public void SetTimeout()
{
    if (Passport.Instance == null)
    {
        ShowOutput("Passport instance is null");
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

The code works as follows:
1. It validates that the Passport SDK is properly initialized
2. It parses the user input to get the timeout value in milliseconds
3. It calls `Passport.Instance.SetCallTimeout(timeout)` to set the timeout
4. It displays a confirmation message to the user

This timeout value affects how long the SDK will wait for responses from the Passport service before timing out. Setting an appropriate timeout can improve user experience by ensuring that operations don't hang indefinitely when network issues occur.

## Running the Feature Example

### Prerequisites
- Unity Editor (2021.3 LTS or later recommended)
- Immutable SDK installed and configured
- Environment variables set up in [Immutable Hub](https://hub.immutable.com)

### Steps to Run the Example
1. Open the Unity project containing the Immutable SDK
2. Ensure you're logged in to Passport (use the Login feature first)
3. Navigate to the "SetCallTimeout" scene in the Passport/Other directory
4. Enter a timeout value in milliseconds in the input field
5. Click the "Set Timeout" button to apply the setting
6. The confirmation message will display the newly set timeout value

## Summary

The SetCallTimeout feature provides a simple way to control how long API calls wait for a response before timing out. By setting an appropriate timeout, developers can enhance their application's resilience to network issues and provide better feedback to users when operations take longer than expected. 