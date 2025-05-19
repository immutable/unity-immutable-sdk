<div class="display-none">

# Get User Info

</div>

The Get User Info feature allows you to retrieve various pieces of information about the currently logged-in user through the Immutable Passport SDK.

<div class="button-component">

[View feature on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/GetUserInfo) <span class="button-component-arrow">→</span>

</div>

## Feature Overview

The Get User Info feature provides easy access to user data from the Immutable Passport. This includes:

- User email
- Passport ID
- Access token
- ID token
- Linked addresses (external wallets connected to the Passport account)

## SDK Integration Details

The Get User Info feature utilizes five key methods from the Passport SDK to retrieve different types of user information. Each method returns specific data related to the authenticated user.

### Retrieving User Email

```csharp title="GetEmail" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/GetUserInfo/GetUserInfoScript.cs"
public void GetEmail()
{
    GetEmailAsync();
}

private async UniTaskVoid GetEmailAsync()
{
    if (Passport.Instance == null)
    {
        ShowOutput("Passport instance is null");
        return;
    }
    try
    {
        string email = await Passport.Instance.GetEmail();
        ShowOutput(email);
    }
    catch (System.Exception ex)
    {
        ShowOutput($"Failed to get email: {ex.Message}");
    }
}
```

This code retrieves the email address of the currently authenticated user. It first checks if the Passport instance is available, then calls the `GetEmail()` method which returns the email as a string. The method is asynchronous and uses UniTask to handle the async operation without blocking the main thread.

### Retrieving Passport ID

```csharp title="GetPassportId" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/GetUserInfo/GetUserInfoScript.cs"
public void GetPassportId()
{
    GetPassportIdAsync();
}

private async UniTaskVoid GetPassportIdAsync()
{
    if (Passport.Instance == null)
    {
        ShowOutput("Passport instance is null");
        return;
    }
    try
    {
        string passportId = await Passport.Instance.GetPassportId();
        ShowOutput(passportId);
    }
    catch (System.Exception ex)
    {
        ShowOutput($"Failed to get Passport ID: {ex.Message}");
    }
}
```

The `GetPassportId()` method retrieves the unique identifier for the user's Passport account. This ID can be used to identify the user across different Immutable services. The method performs a null check on the Passport instance before making the async call and handles any exceptions that might occur during the process.

### Retrieving Access Token

```csharp title="GetAccessToken" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/GetUserInfo/GetUserInfoScript.cs"
public void GetAccessToken()
{
    GetAccessTokenAsync();
}

private async UniTaskVoid GetAccessTokenAsync()
{
    if (Passport.Instance == null)
    {
        ShowOutput("Passport instance is null");
        return;
    }
    try
    {
        string accessToken = await Passport.Instance.GetAccessToken();
        ShowOutput(accessToken);
    }
    catch (System.Exception ex)
    {
        ShowOutput($"Failed to get access token: {ex.Message}");
    }
}
```

The `GetAccessToken()` method retrieves the current OAuth access token for the authenticated user. This token is used for authenticating API calls to Immutable services. The method follows the same pattern of checking for a valid Passport instance, making the async call, and handling any exceptions.

### Retrieving ID Token

```csharp title="GetIdToken" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/GetUserInfo/GetUserInfoScript.cs"
public void GetIdToken()
{
    GetIdTokenAsync();
}

private async UniTaskVoid GetIdTokenAsync()
{
    if (Passport.Instance == null)
    {
        ShowOutput("Passport instance is null");
        return;
    }
    try
    {
        string idToken = await Passport.Instance.GetIdToken();
        ShowOutput(idToken);
    }
    catch (System.Exception ex)
    {
        ShowOutput($"Failed to get ID token: {ex.Message}");
    }
}
```

The `GetIdToken()` method retrieves the ID token, which contains claims about the identity of the authenticated user. This token follows the OpenID Connect standard and can be used to verify the user's identity. Like the other methods, it performs proper error handling and operates asynchronously.

### Retrieving Linked Addresses

```csharp title="GetLinkedAddresses" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/GetUserInfo/GetUserInfoScript.cs"
public void GetLinkedAddresses()
{
    GetLinkedAddressesAsync();
}

private async UniTaskVoid GetLinkedAddressesAsync()
{
    if (Passport.Instance == null)
    {
        ShowOutput("Passport instance is null");
        return;
    }
    try
    {
        List<string> addresses = await Passport.Instance.GetLinkedAddresses();
        string outputMessage = addresses.Count > 0 ? string.Join(", ", addresses) : "No linked addresses";
        ShowOutput(outputMessage);
    }
    catch (System.Exception ex)
    {
        ShowOutput($"Failed to get linked addresses: {ex.Message}");
    }
}
```

The `GetLinkedAddresses()` method retrieves a list of external wallet addresses that the user has linked to their Passport account through the Passport dashboard. This is particularly useful for games that need to know which external wallets a user has connected. The method returns a List of strings containing the wallet addresses, and formats them for display.

## Running the Feature Example

### Prerequisites

- Set up your development environment as described in the [Immutable Hub](https://hub.immutable.com/docs/overview)
- Have Unity installed (version 2021.3 LTS or newer)
- Clone the Immutable Unity SDK repository

### Step-by-step Instructions

1. Open the sample project in Unity Editor
2. Navigate to the Passport initialization scene
3. Play the scene to initialize Passport
4. Log in using your Immutable Passport credentials
5. Navigate to the "Get User Info" example in the sample app
6. Click on any of the buttons ("Get Email", "Get Passport ID", "Get Access Token", "Get ID Token", or "Get Linked Addresses") to retrieve the corresponding user information
7. The retrieved information will be displayed in the output text area

## Summary

The Get User Info feature provides simple but powerful methods to access user information from the Immutable Passport. By implementing these methods, games can easily retrieve user email, Passport ID, tokens, and linked wallet addresses. This information can be used for user identification, authentication of API calls, and connecting to the user's blockchain assets. 