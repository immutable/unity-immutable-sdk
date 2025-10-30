using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Static helper class to easily add click-to-copy functionality to Text components
/// Usage: ClickToCopyHelper.EnableClickToCopy(myTextComponent);
/// </summary>
public static class ClickToCopyHelper
{
    /// <summary>
    /// Enables click-to-copy functionality on a Text component
    /// </summary>
    /// <param name="textComponent">The Text component to make clickable</param>
    /// <param name="showDebugLog">Whether to show debug logs when text is copied</param>
    /// <returns>True if successfully enabled, false otherwise</returns>
    public static bool EnableClickToCopy(Text textComponent, bool showDebugLog = true)
    {
        if (textComponent == null)
        {
            Debug.LogWarning("[ClickToCopyHelper] Cannot enable click-to-copy: Text component is null");
            return false;
        }

        GameObject textObject = textComponent.gameObject;

        // Add EventTrigger component if it doesn't exist
        EventTrigger trigger = textObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = textObject.AddComponent<EventTrigger>();
        }

        // Check if click handler already exists to avoid duplicates
        bool hasClickHandler = false;
        foreach (var entry in trigger.triggers)
        {
            if (entry.eventID == EventTriggerType.PointerClick)
            {
                hasClickHandler = true;
                break;
            }
        }

        if (!hasClickHandler)
        {
            // Add PointerClick event
            EventTrigger.Entry clickEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerClick
            };
            clickEntry.callback.AddListener((data) =>
            {
                CopyTextToClipboard(textComponent, showDebugLog);
            });
            trigger.triggers.Add(clickEntry);

            if (showDebugLog)
            {
                Debug.Log($"[ClickToCopyHelper] Click-to-copy enabled for: {textObject.name}");
            }
        }

        return true;
    }

    /// <summary>
    /// Copies the text from a Text component to the system clipboard
    /// </summary>
    /// <param name="textComponent">The Text component whose text to copy</param>
    /// <param name="showDebugLog">Whether to show debug logs</param>
    public static void CopyTextToClipboard(Text textComponent, bool showDebugLog = true)
    {
        if (textComponent != null && !string.IsNullOrEmpty(textComponent.text))
        {
            GUIUtility.systemCopyBuffer = textComponent.text;

            if (showDebugLog)
            {
                Debug.Log($"[ClickToCopyHelper] Copied to clipboard: {textComponent.text}");
            }

            // Optional: Show visual feedback
            ShowCopyFeedback(textComponent);
        }
    }

    /// <summary>
    /// Shows a brief visual feedback when text is copied
    /// </summary>
    private static void ShowCopyFeedback(Text textComponent)
    {
        // Store original color
        Color originalColor = textComponent.color;

        // Flash green briefly
        textComponent.color = Color.green;

        // Use a MonoBehaviour helper to run the coroutine
        CoroutineRunner.Instance.StartCoroutine(RestoreColorAfterDelay(textComponent, originalColor, 0.2f));
    }

    /// <summary>
    /// Coroutine to restore text color after a delay
    /// </summary>
    private static System.Collections.IEnumerator RestoreColorAfterDelay(Text textComponent, Color originalColor, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (textComponent != null)
        {
            textComponent.color = originalColor;
        }
    }
}

/// <summary>
/// Singleton MonoBehaviour helper to run coroutines from static contexts
/// </summary>
public class CoroutineRunner : MonoBehaviour
{
    private static CoroutineRunner _instance;

    public static CoroutineRunner Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject obj = new GameObject("CoroutineRunner");
                _instance = obj.AddComponent<CoroutineRunner>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }
}
