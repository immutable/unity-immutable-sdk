<div align="center">
  <p align="center">
    <a  href="https://docs.x.immutable.com/docs">
      <img src="https://cdn.dribbble.com/users/1299339/screenshots/7133657/media/837237d447d36581ebd59ec36d30daea.gif" width="280"/>
    </a>
  </p>
</div>

---

# Immutable Unity SDK

## Prerequisites
- [git-lfs](https://git-lfs.github.com/): since `.dll` files are stored on Git Large File Storage, you must download and install git-lfs from [here](https://git-lfs.github.com/).

## Supported Platforms

* Windows

## Installation

1. Uncompress the zip file
1. Open the Package Manager
2. Click the add + button and select "Add package from disk..."
3. Navigate to the package root folder
4. Double clik the `package.json` file

TODO

## Quick Start

### Initialise Passport

Create a script with the following code and bind it to an object:

```
public class ImmutableUnity : MonoBehaviour
{
    void Start()
    {
        Passport passport = await Passport.Init("YOUR_IMMUTABLE_CLIENT_ID");
    }
}
```

Passport is now accessible from anywhere via `Passport.Instance`.

### Connect to Immutable Passport

We use the [Device Code Authorisation](https://auth0.com/docs/get-started/authentication-and-authorization-flow/device-authorization-flow#:~:text=Your%20Auth0%20Authorization%20Server%20redirects,authorized%20to%20access%20the%20API.) flow to authenticate and authorise users.
```
ConnectResponse? response = await passport.Connect();

if (response != null)
{
    // Display the code to the user
    userCodeText.text = response.code;
    
}
else
{
    // Code confirmation is not required, proceed to authenticated flow
}
```

If the `response` is not null, display the code on the game UI so the user can use it to authenticate. To confirm the code, call `await passport.ConfirmCode(response.code)`

This will open the user's default browser and take them through the auth flow. If this doesn't happen, you may use the returned URL to manually open the browser, e.g. ` Application.OpenURL(response.url)`

#### Credentials

Once the user is connected to Passport, the SDK will store the credentials (access, ID, and refresh tokens) for you.

You may use `await passport.ConnectSilent()` to connect to Passport using the saved credentials. This is similar to `Connect()`. However, if the saved access token is no longer valid and the refresh token cannot be used, it will not fall back to the Device Code Authorisation flow.

```
try 
{
    bool hasCredsSaved = await passport.HasCredentialsSaved();
    if (hasCredsSaved)
    {
        await passport.ConnectSilent();
        // Successfully connected to Passport
    }
}
catch (Exception)
{
    // Attempted to connect to Passport silently but could not use saved credentials
}
```

### Log out of Immutable Passport

```
await passport.Logout();
```

## Supported Functions

* Get address
* Get access token
* Get ID token.


* Get email
* Checks if there are any credentials saved

## Examples

* **Sample code** - see the [sample](https://github.com/immutable/unity-immutable-sdk/tree/main/sample) folder for examples of how to use the Immutable Unity SDK.

## Changelog Management

The following headings should be used as appropriate

- Added
- Changed
- Deprecated
- Removed
- Fixed

What follows is an example with all the change headings, for real world use only use headings when appropriate.
This goes at the top of the CHANGELOG.md above the most recent release.

```markdown
...

## [Unreleased]

### Added

for new features.

### Changed

for changes in existing functionality.

### Deprecated

for soon-to-be removed features.

### Removed

for now removed features.

### Fixed

for any bug fixes.

...
```

The version.yml will hold the value of the previous release

```yaml
version: 0.1.0
```

## Contributing

If you would like to contribute, please read the following:

- We use the [Conventional Commits specification](https://www.conventionalcommits.org/en/v1.0.0/#specification) when writing our commit messages. Why use Conventional Commits? Read [here](https://www.conventionalcommits.org/en/v1.0.0/#why-use-conventional-commits).

## Getting Help

Immutable X is open to all to build on, with no approvals required. If you want to talk to us to learn more, or apply for developer grants, click below:

[Contact us](https://www.immutable.com/contact)

### Project Support

To get help from other developers, discuss ideas, and stay up-to-date on what's happening, become a part of our community on Discord.

[Join us on Discord](https://discord.gg/TkVumkJ9D6)

You can also join the conversation, connect with other projects, and ask questions in our Immutable X Discourse forum.

[Visit the forum](https://forum.immutable.com/)

#### Still need help?

You can also apply for marketing support for your project. Or, if you need help with an issue related to what you're building with Immutable X, click below to submit an issue. Select _I have a question_ or _issue related to building on Immutable X_ as your issue type.

[Contact support](https://support.immutable.com/hc/en-us/requests/new)

## License
Immutable Unity SDK repository is distributed under the terms of the [Apache License (Version 2.0)](LICENSE).