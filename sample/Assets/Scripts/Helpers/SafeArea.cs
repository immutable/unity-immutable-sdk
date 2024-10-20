using UnityEngine;

/// <summary>
/// Adjusts the UI to fit within the safe area on mobile devices.
/// </summary>
public class SafeArea : MonoBehaviour
{
#if (UNITY_ANDROID && !UNITY_EDITOR) || (UNITY_IPHONE && !UNITY_EDITOR)
    private RectTransform rectTransform;
    private Rect safeArea;
    private Vector2 minAnchor;
    private Vector2 maxAnchor;
#endif

    void Start()
    {
#if (UNITY_ANDROID && !UNITY_EDITOR) || (UNITY_IPHONE && !UNITY_EDITOR)
        rectTransform = GetComponent<RectTransform>();
        safeArea = Screen.safeArea;

        minAnchor = safeArea.position;
        maxAnchor = minAnchor + safeArea.size;

        minAnchor.x /= Screen.width;
        minAnchor.y /= Screen.height;
        maxAnchor.x /= Screen.width;
        maxAnchor.y /= Screen.height;

        rectTransform.anchorMin = minAnchor;
        rectTransform.anchorMax = maxAnchor;
#endif
    }
}
