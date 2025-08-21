# PassportManager Prefab

The **PassportManager** provides multiple drag-and-drop prefabs for easy Immutable Passport authentication integration into your Unity game.

## 🎯 No-Code Setup Example

Want authentication with zero scripting? Here's how:

1. **Create UI**: Add Canvas → Button (Login), Button (Logout), Text (Status)
2. **Drag Components**: In PassportUIController Inspector, drag the **COMPONENTS** (not GameObjects) to the UI fields:
   - Expand the GameObject in hierarchy
   - Drag the **Button** component (not the GameObject)
   - For text: Drag **Text** component OR **TextMeshPro - Text (UI)** component (not the GameObject)
   - PassportUIController supports both text types - use whichever your project uses
3. **Configure**: Set your Client ID and redirect URIs
4. **Done!**: Hit Play - your buttons now handle authentication automatically

That's it! No scripts, no event handling, no code. Pure drag-and-drop! 🚀

## 🚀 Quick Start

### Option 1: Complete UI (Zero Config! 🎯)

**Perfect for:** Quick prototyping, testing, or developers who want instant authentication UI

1. **Import the Sample**: In Unity Package Manager, find "Immutable Passport" and import the "PassportManager Prefab" sample
2. **Drag the Complete Prefab**: Drag `PassportManagerComplete.prefab` into your scene
3. **Configure Settings**: Set Client ID and redirect URIs (e.g., `mygame://callback`, `mygame://logout`)
4. **Test**: Hit Play - you'll see a complete login UI with Google, Apple, Facebook, and default login buttons!

**What you get:**

- ✅ Mobile-first responsive UI that works on all screen sizes
- ✅ Pre-styled social login buttons (Google red, Apple black, Facebook blue)
- ✅ Automatic panel switching (login → logged-in state)
- ✅ Status messages and user info display
- ✅ Zero code required - just configure and play!

### Option 2: Authentication Only (Code-based)

**Perfect for:** Developers with custom UI who want just the authentication logic

1. **Import the Sample**: Same as above
2. **Drag the Minimal Prefab**: Drag `PassportManager.prefab` from the Samples folder into your scene
3. **Configure Settings**: In the Inspector, set your Client ID and redirect URIs
4. **Use Events or Code**: Subscribe to events or call methods directly
5. **Test**: Hit Play - the prefab will automatically initialise Passport!

### Option 3: Custom UI Integration (Advanced)

**Perfect for:** Developers who want their own UI but with helper scripts

1. **Import the Sample**: Same as above
2. **Drag the Minimal Prefab**: Use `PassportManager.prefab`
3. **Configure Settings**: Same as above
4. **Add PassportUIController**: Add the `PassportUIController` script to a GameObject and assign your UI elements (supports both Legacy Text and TextMeshPro)
5. **Wire Events**: In PassportManager Inspector, connect the events to PassportUIController methods
6. **⚠️ Production Note**: Review cursor management settings for your game type
7. **Test**: Hit Play - full authentication flow with automatic UI management!

## ⚙️ Configuration

### Required Settings

- **Client ID**: Your Immutable Passport client ID
- **Redirect URI**: Authentication callback URL (e.g., `mygame://callback`)
- **Logout Redirect URI**: Logout callback URL (e.g., `mygame://logout`)

### Optional Settings

- **Environment**: `SANDBOX` (default) or `PRODUCTION`
- **Auto Initialise**: Automatically initialise on Start (default: true)
- **Auto Login**: Automatically attempt login after initialisation (default: false)
- **Direct Login Method**: Default login method for auto-login and generic login button (None, Google, Apple, Facebook)
- **Default Marketing Consent**: Default consent status for marketing communications (Unsubscribed, OptedIn)
- **Log Level**: Control debug output verbosity

### UI Customisation (PassportManagerComplete)

The complete prefab creates UI at runtime, which means you can customise the appearance:

**Runtime Customisation:**

- The UI is built using Unity's default UI style
- All UI elements can be accessed and modified after creation
- Button colours, text, and layout are easily customisable via code
- Mobile-responsive design automatically adapts to screen size

**Layout Configuration:**

- Panel Size: 300x400 (adjustable in PassportUIBuilder)
- Button Size: 280x45 (adjustable)
- Element Spacing: 10px (adjustable)
- Canvas Order: 100 (prevents UI conflicts)

**Colour Scheme:**

- Google Button: Red (#DB4437) with white text
- Apple Button: Black with white text
- Facebook Button: Blue (#4267B2) with white text
- Default Button: White with black text
- Background Panel: Semi-transparent dark (10% opacity)

### UI Integration (Option 3 - Custom UI)

Simply drag UI elements from your scene into these fields and they'll be automatically configured:

- **Login Button**: Triggers default login method
- **Google Login Button**: Triggers Google-specific login
- **Apple Login Button**: Triggers Apple-specific login
- **Facebook Login Button**: Triggers Facebook-specific login
- **Logout Button**: Triggers logout
- **Status Text**: Shows current authentication status (Legacy Text OR TextMeshPro)
- **User Info Text**: Shows logged-in user's access token preview (Legacy Text OR TextMeshPro)

**✨ Magic**: Buttons are automatically enabled/disabled based on authentication state!

## 🎮 Using Events

The prefab exposes Unity Events that you can wire up in the Inspector:

### Initialisation Events

- `OnPassportInitialized`: Fired when Passport is ready
- `OnPassportError`: Fired if initialisation fails

### Authentication Events

- `OnLoginSucceeded`: User successfully logged in
- `OnLoginFailed`: Login failed or was cancelled
- `OnLogoutSucceeded`: User successfully logged out
- `OnLogoutFailed`: Logout failed

## 💻 Using from Code

You can also interact with the PassportManager from your scripts:

```csharp
// Access the singleton instance
var manager = PassportManager.Instance;

// Check states
if (manager.IsInitialized && manager.IsLoggedIn)
{
    // Access the underlying Passport instance
    var email = await manager.PassportInstance.GetEmail();
    var accessToken = await manager.PassportInstance.GetAccessToken();

    // Note: GetAddress() requires IMX connection via ConnectImx()
    // var address = await manager.PassportInstance.GetAddress();
}

// Manual login with specific method and marketing consent
var loginOptions = new DirectLoginOptions(DirectLoginMethod.Google, marketingConsentStatus: MarketingConsentStatus.Unsubscribed);
manager.Login(loginOptions);

// Or use the simpler method (uses default marketing consent from Inspector)
manager.Login(); // Uses configured directLoginMethod

// Manual logout
manager.Logout();
```

## 📧 Marketing Consent

The PassportManager supports marketing consent for compliance with privacy regulations (GDPR, etc.):

### Default Marketing Consent

Configure the default marketing consent status in the Inspector:

- **Unsubscribed** (default): Users are not subscribed to marketing communications by default
- **OptedIn**: Users are opted into marketing communications by default

This setting is used when:

- Users authenticate via direct login methods (Google, Apple, Facebook)
- No explicit marketing consent is provided in code

### Advanced Marketing Consent

For programmatic control over marketing consent:

```csharp
// Create login options with specific marketing consent
var loginOptions = new DirectLoginOptions(
    DirectLoginMethod.Google,
    marketingConsentStatus: MarketingConsentStatus.Unsubscribed
);

// Login with explicit marketing consent
await PassportManager.Instance.LoginAsync(loginOptions);
```

### Marketing Consent Flow

1. **Unity Game**: Sets marketing consent via `DirectLoginOptions` or uses default from Inspector
2. **Authentication Server**: Receives and processes the marketing consent preference
3. **User Profile**: Marketing consent status is stored and applied to user's profile
4. **Compliance**: Ensures proper consent handling for marketing communications

**Note**: Marketing consent is automatically handled during the authentication flow and integrated with Immutable's user profile system.

## 📁 Sample Scripts

Check out the included example script:

- `PassportUIController.cs`: Complete no-code UI integration (recommended)

## ⚠️ Important: Cursor Management for Production

The `PassportUIController` uses **aggressive cursor management** designed for demos and prototypes. It continuously unlocks the cursor to ensure UI remains clickable.

### For Production Games

1. **Disable `forceCursorAlwaysAvailable`** in PassportUIController Inspector
2. **Implement custom cursor logic** based on your game's needs:
   - **FPS games**: May want `Cursor.lockState = CursorLockMode.Locked` during gameplay
   - **RTS games**: May want `Cursor.lockState = CursorLockMode.None` always
   - **Menu-driven games**: May want cursor unlocked for UI, locked for gameplay
3. **Handle cursor state** in the authentication event handlers:

   ```csharp
   void OnLoginSucceeded() {
       // Your game-specific cursor behaviour
       Cursor.lockState = YourGame.GetDesiredCursorMode();
   }
   ```

## 🆘 Troubleshooting

### "Can't drag UI elements to Inspector fields"

**Issue**: You're dragging the GameObject instead of the component, or using wrong text type.

**Solution**:

1. In the Hierarchy, click the **arrow** next to your GameObject to expand it
2. For buttons: Drag the **Button** component (with the icon), NOT the GameObject name
3. For text components: Drag either:
   - **Text** component (Legacy UI) → to "Status Text" or "User Info Text" fields
   - **TextMeshPro - Text (UI)** component → to "Status Text TMP" or "User Info Text TMP" fields
4. The field should highlight blue when you can drop it

### "Redirect URI must be configured"

Make sure you've set both redirect URIs in the Inspector.

### "Client ID not set"

Enter your Immutable Passport client ID in the Inspector.

### "Error loading user info: No IMX provider"

**Issue**: This occurs when trying to access IMX-specific features (like wallet address) without connecting to IMX first.

**Solution**: The PassportManager now uses access token instead of wallet address for user info display. If you need wallet addresses, call `ConnectImx()` first:

```csharp
await PassportManager.Instance.PassportInstance.ConnectImx();
var address = await PassportManager.Instance.PassportInstance.GetAddress();
```

### Deep linking issues on Windows

- Run Unity as Administrator if you get permission errors
- Check Windows Firewall settings
- Ensure no other applications are using the same URL scheme

### "UI buttons don't work"

- Make sure you dragged the **Button component**, not the GameObject
- Check that the PassportManager prefab is in the scene (not just in Project window)
- Verify the buttons are actually assigned in the PassportManager or PassportUIController Inspector
- If using PassportUIController, ensure the script is enabled on the GameObject

### "Mouse clicks don't register after authentication"

**Issue**: The authentication webview can interfere with Unity's input system.
**Solution**: Handle cursor state in your game code:

```csharp
void Start() {
    PassportManager.Instance.OnLoginSucceeded.AddListener(RestoreInput);
    PassportManager.Instance.OnLoginFailed.AddListener(RestoreInput);
}

void RestoreInput() {
    // For FPS games:
    Cursor.lockState = CursorLockMode.Locked;

    // For menu-driven games:
    Cursor.lockState = CursorLockMode.None;

    // Ensure cursor is visible for UI
    Cursor.visible = true;
}
```

### "Cursor behaviour conflicts with my game"

**Issue**: PassportUIController's aggressive cursor management interferes with game controls.

**Solution**:

1. **Disable** `Force Cursor Always Available` in PassportUIController Inspector
2. **Implement** your own cursor management in the authentication event handlers
3. **Consider** using PassportManager events directly instead of PassportUIController for full control

## 📚 Learn More

- [Immutable Passport Documentation](https://docs.immutable.com/build/unity/)
- [Unity Quickstart Guide](https://docs.immutable.com/build/unity/quickstart)
