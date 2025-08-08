# PassportManager Prefab

The **PassportManager** is a drag-and-drop prefab that provides an easy way to integrate Immutable Passport authentication into your Unity game.

## 🚀 Quick Start

### Option 1: Just Authentication (Code-based)

1. **Import the Sample**: In Unity Package Manager, find "Immutable Passport" and import the "PassportManager Prefab" sample
2. **Drag the Prefab**: Drag `PassportManager.prefab` from the Samples folder into your scene
3. **Configure Settings**: In the Inspector, set your Client ID and redirect URIs
4. **Use Events or Code**: Subscribe to events or call methods directly
5. **Test**: Hit Play - the prefab will automatically initialize Passport!

### Option 2: Complete UI Integration (No Code Required!)

1. **Import the Sample**: Same as above
2. **Drag the Prefab**: Same as above  
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
- **Auto Initialize**: Automatically initialize on Start (default: true)
- **Auto Login**: Automatically attempt login after initialization (default: false)
- **Direct Login Method**: Pre-select login method (None, Google, Apple, Facebook)
- **Log Level**: Control debug output verbosity

### UI Integration (Optional - No Code Required!)

Simply drag UI elements from your scene into these fields and they'll be automatically configured:

- **Login Button**: Triggers default login method
- **Google Login Button**: Triggers Google-specific login
- **Apple Login Button**: Triggers Apple-specific login  
- **Facebook Login Button**: Triggers Facebook-specific login
- **Logout Button**: Triggers logout
- **Status Text**: Shows current authentication status (Legacy Text OR TextMeshPro)
- **User Info Text**: Shows logged-in user's access token preview (Legacy Text OR TextMeshPro)

**✨ Magic**: Buttons are automatically enabled/disabled based on authentication state!

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

## 🎮 Using Events

The prefab exposes Unity Events that you can wire up in the Inspector:

### Initialization Events

- `OnPassportInitialized`: Fired when Passport is ready
- `OnPassportError`: Fired if initialization fails

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

// Manual login with specific method
manager.Login(DirectLoginMethod.Google);

// Manual logout
manager.Logout();
```

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
       // Your game-specific cursor behavior
       Cursor.lockState = YourGame.GetDesiredCursorMode();
   }
   ```

## 🔗 Deep Linking Setup

For native platforms (Windows/Mac), you'll need to register your custom URL scheme:

### Windows

The SDK automatically handles Windows deep linking registration.

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

### "Cursor behavior conflicts with my game"

**Issue**: PassportUIController's aggressive cursor management interferes with game controls.

**Solution**:

1. **Disable** `Force Cursor Always Available` in PassportUIController Inspector
2. **Implement** your own cursor management in the authentication event handlers
3. **Consider** using PassportManager events directly instead of PassportUIController for full control

## 📚 Learn More

- [Immutable Passport Documentation](https://docs.immutable.com/build/unity/)
- [Unity Quickstart Guide](https://docs.immutable.com/build/unity/quickstart)
