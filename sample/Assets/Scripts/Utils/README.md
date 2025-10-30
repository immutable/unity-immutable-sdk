# Utility Scripts

## Click-to-Copy Functionality

This folder contains utility scripts for adding click-to-copy functionality to Unity UI Text components.

### Quick Start

To enable click-to-copy on any `Text` component, use the `ClickToCopyHelper`:

```csharp
using UnityEngine;
using UnityEngine.UI;

public class MyScript : MonoBehaviour
{
    [SerializeField] private Text outputText;

    void Start()
    {
        // Enable click-to-copy functionality
        ClickToCopyHelper.EnableClickToCopy(outputText);
    }
}
```

### Features

- **One-line setup**: Just call `ClickToCopyHelper.EnableClickToCopy(textComponent)`
- **Visual feedback**: Text flashes green when copied
- **Debug logging**: See what was copied in the Console
- **Automatic duplicate prevention**: Won't add multiple click handlers

### Components

#### 1. `ClickToCopyHelper.cs` (Recommended)

Static helper class for easy integration:

```csharp
// Basic usage
ClickToCopyHelper.EnableClickToCopy(myText);

// Disable debug logging
ClickToCopyHelper.EnableClickToCopy(myText, showDebugLog: false);

// Manually copy text
ClickToCopyHelper.CopyTextToClipboard(myText);
```

#### 2. `ClickToCopyText.cs` (Component-based)

MonoBehaviour component that can be attached directly to a GameObject with a Text component:

1. Select the GameObject with your `Text` component in the Unity Editor
2. Click "Add Component"
3. Search for "Click To Copy Text"
4. Done! The text is now clickable

**Inspector Options:**
- `Show Visual Feedback`: Toggle the green flash effect when copying

### How It Works

1. Adds an `EventTrigger` component to the Text GameObject
2. Listens for `PointerClick` events
3. Copies the text content to `GUIUtility.systemCopyBuffer`
4. Shows visual feedback (optional)

### Example Scripts

See `LoginScript.cs` for an example implementation:

```csharp
public class LoginScript : MonoBehaviour
{
    [SerializeField] private Text Output;

    void Start()
    {
        // Enable click-to-copy
        ClickToCopyHelper.EnableClickToCopy(Output);
    }

    private void ShowOutput(string message)
    {
        if (Output != null)
        {
            Output.text = message;
            // Users can now click the output to copy it!
        }
    }
}
```

### Platform Support

- ✅ Windows
- ✅ macOS
- ✅ Linux
- ✅ Android (copies to system clipboard)
- ✅ iOS (copies to system clipboard)
- ✅ WebGL (copies to browser clipboard)

### Troubleshooting

**Issue: Text is not clickable**
- Ensure the Text GameObject has a `RectTransform` component
- Check that the Text is not blocked by other UI elements
- Verify that the Canvas has a `GraphicRaycaster` component

**Issue: Visual feedback doesn't show**
- The green flash requires the `CoroutineRunner` singleton to be active
- Check Unity Console for any errors during initialization

**Issue: Multiple click handlers firing**
- `EnableClickToCopy()` automatically prevents duplicates
- If using `ClickToCopyText` component, remove any duplicate components

### Notes

- Text color briefly changes to green as visual feedback (0.2 seconds)
- Uses `GUIUtility.systemCopyBuffer` for cross-platform clipboard access
- Debug logs show what was copied (can be disabled)
- Original text color is automatically restored after feedback

### Advanced Usage

For custom integration, you can directly use the static methods:

```csharp
// Copy any text to clipboard
GUIUtility.systemCopyBuffer = "My custom text";

// Or use the helper for Text components
ClickToCopyHelper.CopyTextToClipboard(myTextComponent, showDebugLog: true);
```
