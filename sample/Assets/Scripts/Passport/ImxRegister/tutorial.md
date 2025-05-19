<div class="display-none">

# IMX Register

</div>

Register users with Immutable X to enable them to interact with the IMX blockchain.

<div class="button-component">

[View feature on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/imxregister) <span class="button-component-arrow">→</span>

</div>

## Feature Overview

The IMX Register feature enables users to register with Immutable X's Layer 2 solution, which is required before they can perform transactions on the IMX blockchain.

## SDK Integration Details

The Immutable Passport SDK provides a simple method to register users with Immutable X. This registration is required before users can perform transactions such as transferring NFTs or tokens on the IMX blockchain.

```csharp title="IMX Register" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/imxregister/ImxRegisterScript.cs"
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

The `RegisterOffchain()` method handles the registration process with Immutable X. Upon successful registration, users can interact with the IMX blockchain. The method returns a `RegisterUserResponse` object which confirms successful registration.

## Running the Feature Example

### Prerequisites
- Set up your development environment with [Immutable Hub](https://hub.immutable.com/)
- Unity Editor (2022.2 or newer recommended)
- The Immutable Passport SDK installed in your Unity project

### Step-by-step Instructions
1. Open your Unity project with the Immutable Passport SDK properly initialized
2. Navigate to the IMX Register sample scene in the project
3. Ensure you've logged in to Passport first using the login feature
4. Connect to Immutable X via the IMX Connect feature
5. Click the "Register Offchain" button to initiate the registration
6. View the output message confirming successful registration

## Summary

The IMX Register feature allows users to register with Immutable X's Layer 2 solution, which is a necessary step before they can perform any transactions on the IMX blockchain. This simple implementation enables quick and easy registration for your users, enhancing their experience with blockchain functionality in your game. 