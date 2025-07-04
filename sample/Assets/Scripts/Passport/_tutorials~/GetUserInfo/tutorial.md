<div class="display-none">

# Unity SDK User Information Retrieval

</div>

The GetUserInfo feature demonstrates how to retrieve various types of user information from Immutable Passport, including email addresses, Passport IDs, authentication tokens, and linked wallet addresses. This feature is essential for games that need to access user profile data and manage user accounts effectively.

<div class="button-component">

[View feature on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/GetUserInfo) <span class="button-component-arrow">→</span>

</div>

## Unity SDK GetUserInfo Implementation

The GetUserInfo feature provides five core methods for retrieving different types of user information from Passport:

### Feature: GetUserInfo

The GetUserInfo feature enables developers to access comprehensive user information from authenticated Passport accounts. This includes personal details, authentication credentials, and linked external wallet addresses, making it a crucial component for user profile management and account verification.

```csharp title="GetUserInfo Methods" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/GetUserInfo/GetUserInfoScript.cs"
// Get user's email address
public async void GetEmail()
{
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

// Get user's Passport ID
public async void GetPassportId()
{
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

// Get user's access token
public async void GetAccessToken()
{
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

// Get user's ID token
public async void GetIdToken()
{
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

// Get user's linked external wallet addresses
public async void GetLinkedAddresses()
{
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

The implementation uses async/await patterns with UniTask for non-blocking operations and includes comprehensive error handling. Each method checks for Passport instance availability before making API calls, ensuring robust operation. The GetLinkedAddresses method specifically handles both populated and empty address lists, providing clear feedback to users about their linked wallet status.

## Running the Feature Example

### Prerequisites

- Unity 2021.3 or newer
- Immutable Unity SDK installed
- [Immutable Hub](https://hub.immutable.com) account for environment configuration
- Active Passport authentication session

### Step-by-Step Instructions

1. **Open the Sample Project**
   - Navigate to the Unity sample project in the SDK
   - Open the `AuthenticatedScene` scene

2. **Ensure Passport Authentication**
   - The user must be logged in through Passport before accessing user information
   - Use the Authentication features to log in if not already authenticated

3. **Test User Information Retrieval**
   - Click the "Get Email" button to retrieve the user's email address
   - Click the "Get Passport ID" button to get the unique Passport identifier
   - Click the "Get Access Token" button to retrieve the current access token
   - Click the "Get ID Token" button to get the ID token
   - Click the "Get Linked Addresses" button to view connected external wallets

4. **Verify Results**
   - Check the output display for successful data retrieval
   - Observe error messages if the user is not authenticated or if network issues occur
   - Test with different authentication states to understand the feature's behavior

## Summary

The GetUserInfo feature provides comprehensive access to user profile information and authentication credentials within Immutable Passport. It demonstrates best practices for async API calls, error handling, and user data management in Unity games. Developers can use these methods to create personalized user experiences, implement account verification systems, and manage user profiles effectively.

Key takeaways for developers:
- Always verify Passport instance availability before making API calls
- Implement proper error handling for network and authentication failures
- Use async/await patterns to maintain responsive UI during API operations
- Handle empty or null responses gracefully to provide clear user feedback
- Consider user privacy when displaying sensitive information like tokens and email addresses 