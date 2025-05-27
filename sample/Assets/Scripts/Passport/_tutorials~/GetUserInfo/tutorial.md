<div class="display-none">

# Get User Info

</div>

The Passport GetUserInfo feature group provides methods to access authenticated user information from Immutable Passport. This feature group enables developers to retrieve important user identifiers, authentication tokens, and linked wallet addresses that can be used for user management, authentication verification, and integration with other systems.

<div class="button-component">

[View feature group on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/GetUserInfo) <span class="button-component-arrow">→</span>

</div>

## GetUserInfo Overview

The GetUserInfo feature group consists of a single feature that provides multiple methods to retrieve different pieces of user information:

- Get user email
- Get Passport ID
- Get access token
- Get ID token
- Get linked addresses

These methods help developers access authenticated user data that can be used for user management, displaying user information, or integrating with other systems that require authentication tokens.

## Unity SDK GetUserInfo Features

### Feature: GetUserInfo

The GetUserInfo feature provides several methods to retrieve authenticated user information from Immutable Passport.

```csharp title="GetUserInfo" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/GetUserInfo/GetUserInfoScript.cs"
// Retrieving the user's email
string email = await Passport.Instance.GetEmail();

// Retrieving the user's Passport ID
string passportId = await Passport.Instance.GetPassportId();

// Retrieving the user's access token
string accessToken = await Passport.Instance.GetAccessToken();

// Retrieving the user's ID token
string idToken = await Passport.Instance.GetIdToken();

// Retrieving the user's linked external wallets
List<string> addresses = await Passport.Instance.GetLinkedAddresses();
```

#### How It Works

The GetUserInfo feature provides a simple interface to access user information from the authenticated Passport session:

1. **GetEmail()**: Retrieves the email address associated with the user's Passport account. This is useful for user identification and communication purposes.

2. **GetPassportId()**: Returns the unique identifier for the user's Passport account. This ID can be used to uniquely identify users in your application.

3. **GetAccessToken()**: Provides the OAuth access token that can be used to make authenticated API calls to Immutable services or your own backend services. This token proves the user's identity and authorization.

4. **GetIdToken()**: Returns the ID token containing the user's identity information in JWT format. This token can be decoded to access additional user profile information.

5. **GetLinkedAddresses()**: Retrieves a list of external wallet addresses that the user has linked to their Passport account. This is useful for identifying which external wallets the user has associated with their account.

All methods are asynchronous and return `UniTask` results, which can be awaited in async methods. Each method performs validation checks on the Passport instance before attempting to retrieve information, and proper error handling is implemented to catch and report any exceptions.

## Running the GetUserInfo Example

### Prerequisites

- Unity Editor (2020.3 LTS or later)
- Immutable SDK imported into your project
- Configured Immutable Hub environment ([Configure Immutable Hub](https://docs.immutable.com/docs/x/sdks/unity))

### Steps to Run the Example

1. Open the sample app in Unity Editor.
2. Ensure you have already set up and initialized Passport. You must be logged in to retrieve user information.
3. Navigate to the GetUserInfo scene or component in the sample app.
4. Each user information method can be tested by clicking the corresponding button in the UI:
   - Click "Get Email" to retrieve the user's email address
   - Click "Get Passport ID" to retrieve the user's Passport ID
   - Click "Get Access Token" to retrieve the user's access token
   - Click "Get ID Token" to retrieve the user's ID token
   - Click "Get Linked Addresses" to retrieve the user's linked wallet addresses
5. The retrieved information will be displayed in the output text field.

Note: You must successfully authenticate (log in) with Passport before you can retrieve user information. If not logged in, the methods will return appropriate error messages.

## Summary

The GetUserInfo feature group provides essential functionality for accessing authenticated user information from Immutable Passport. With these methods, developers can:

- Access user identifiers for account management
- Retrieve authentication tokens for API requests
- Get linked wallet addresses for blockchain interactions

Best practices when using the GetUserInfo feature:

1. Always check if the user is authenticated before attempting to retrieve user information
2. Implement proper error handling for cases where retrieving information fails
3. Store sensitive information (like access tokens) securely, following security best practices
4. Use the appropriate method for your specific needs rather than retrieving all information unnecessarily
5. Consider caching non-sensitive information to reduce API calls 