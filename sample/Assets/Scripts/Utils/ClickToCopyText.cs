using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Utility component that adds click-to-copy functionality to any Text UI element
/// Simply attach this component to a GameObject with a Text component
/// </summary>
[RequireComponent(typeof(Text))]
public class ClickToCopyText : MonoBehaviour
{
    private Text textComponent;
    private EventTrigger eventTrigger;

    [Tooltip("Show a visual feedback when text is copied")]
    [SerializeField] private bool showVisualFeedback = true;

    [Tooltip("Original color of the text")]
    private Color originalColor;

    void Awake()
    {
        // Get the Text component
        textComponent = GetComponent<Text>();
        originalColor = textComponent.color;

        // Set up EventTrigger for click detection
        SetupClickHandler();
    }

    /// <summary>
    /// Sets up the click event handler
    /// </summary>
    private void SetupClickHandler()
    {
        // Add EventTrigger component if it doesn't exist
        eventTrigger = gameObject.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = gameObject.AddComponent<EventTrigger>();
        }

        // Add PointerClick event
        EventTrigger.Entry clickEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerClick
        };
        clickEntry.callback.AddListener((data) => { CopyTextToClipboard(); });
        eventTrigger.triggers.Add(clickEntry);

        // Add PointerEnter event for hover cursor change (optional)
        EventTrigger.Entry enterEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        enterEntry.callback.AddListener((data) => { OnPointerEnter(); });
        eventTrigger.triggers.Add(enterEntry);

        // Add PointerExit event to restore cursor
        EventTrigger.Entry exitEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerExit
        };
        exitEntry.callback.AddListener((data) => { OnPointerExit(); });
        eventTrigger.triggers.Add(exitEntry);

        Debug.Log($"[ClickToCopyText] Click-to-copy enabled for: {gameObject.name}");
    }

    /// <summary>
    /// Copies the current text to clipboard
    /// </summary>
    private void CopyTextToClipboard()
    {
        if (textComponent != null && !string.IsNullOrEmpty(textComponent.text))
        {
            GUIUtility.systemCopyBuffer = textComponent.text;
            Debug.Log($"[ClickToCopyText] Copied to clipboard: {textComponent.text}");

            if (showVisualFeedback)
            {
                ShowCopyFeedback();
            }
        }
    }

    /// <summary>
    /// Shows a brief visual feedback when text is copied
    /// </summary>
    private void ShowCopyFeedback()
    {
        // Flash the text color briefly
        StartCoroutine(FlashColor());
    }

    /// <summary>
    /// Coroutine to flash the text color
    /// </summary>
    private System.Collections.IEnumerator FlashColor()
    {
        textComponent.color = Color.green;
        yield return new WaitForSeconds(0.2f);
        textComponent.color = originalColor;
    }

    /// <summary>
    /// Called when pointer enters the text area
    /// </summary>
    private void OnPointerEnter()
    {
        // Change cursor to hand/pointer cursor (Unity doesn't have built-in cursor change,
        // but you could set a custom cursor texture here if desired)
        // Cursor.SetCursor(handCursorTexture, Vector2.zero, CursorMode.Auto);
    }

    /// <summary>
    /// Called when pointer exits the text area
    /// </summary>
    private void OnPointerExit()
    {
        // Restore default cursor
        // Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    /// <summary>
    /// Public method to programmatically copy text
    /// </summary>
    public void CopyToClipboard()
    {
        CopyTextToClipboard();
    }
}
