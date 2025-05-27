<div class="display-none">

# Set Call Timeout

</div>

The Set Call Timeout feature allows developers to configure the timeout duration for API calls within the Immutable Passport SDK. This enables greater control over network operations, allowing customization of how long the application should wait for responses before timing out

<div class="button-component">

[View feature on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/Other/SetCallTimeout) <span class="button-component-arrow">→</span>

</div>

## Unity SDK Set Call Timeout Implementation

### Feature: SetCallTimeout

The Set Call Timeout feature provides a simple way to adjust the timeout duration for network calls made through the Passport SDK. When making API calls to blockchain networks or authentication services, network latency can sometimes cause delays. This feature gives developers control over how long their application should wait before considering a request as failed due to timeout.

```csharp title="SetCallTimeoutScript" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/Other/SetCallTimeout/SetCallTimeoutScript.cs"
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;

public class SetCallTimeoutScript : MonoBehaviour
{
    [SerializeField] private Text Output;
    [SerializeField] private InputField TimeoutInput;

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
}
```

The implementation is straightforward:

1. The script takes a timeout value in milliseconds from a UI input field.
2. After validating the input, it calls `Passport.Instance.SetCallTimeout(timeout)` to set the timeout duration.
3. The default timeout value in the SDK is 60,000 milliseconds (1 minute).

This timeout applies to functions that use the browser communications manager in the Passport SDK, which includes most network operations like authentication and blockchain transactions.

## Running the Feature Example

### Prerequisites

- Unity Editor installed (2021.3 LTS or newer recommended)
- Immutable SDK integrated into your project
- Basic knowledge of Unity UI system
- Valid API credentials set up on [Immutable Hub](https://hub.immutable.com/)

### Steps to Run the Example

1. Open the sample project in Unity Editor.
2. Ensure you have configured your Passport settings with valid client ID and environment in the Passport initialization scene.
3. Run the project in the Unity Editor.
4. Log in to Passport if not already authenticated.
5. Navigate to the Set Call Timeout scene.
6. Enter a timeout value in milliseconds in the input field (e.g., 30000 for 30 seconds).
7. Click the "Set Timeout" button to apply the new timeout value.
8. The output text will confirm that the timeout has been set to the specified value.

## Summary

The Set Call Timeout feature provides developers with the ability to customize network timeout durations in their applications. This is particularly useful when:

- Working with networks that have varying latency conditions
- Developing for different platforms with different network capabilities
- Fine-tuning application responsiveness and error handling
- Testing network resilience with different timeout settings

By implementing appropriate timeout values, developers can create a better user experience by controlling how long the application will wait for network responses before considering the operation failed, allowing for more graceful error handling. 