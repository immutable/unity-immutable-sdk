using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Immutable.Passport
{
    /// <summary>
    /// Builds a complete UI for PassportManager at runtime.
    /// Creates a mobile-first, simple UI that developers can easily customize.
    /// </summary>
    public class PassportUIBuilder : MonoBehaviour
    {
        [Header("UI Configuration")]
        [SerializeField]
        [Tooltip("The PassportManager to connect this UI to")]
        private PassportManager passportManager;

        [Header("Layout Settings")]
        [SerializeField] private int canvasOrder = 100;
        [SerializeField] private Vector2 panelSize = new Vector2(300, 400);
        [SerializeField] private Vector2 buttonSize = new Vector2(280, 45);
        [SerializeField] private float elementSpacing = 10f;

        [Header("Cursor Management (Demo Feature)")]
        [SerializeField]
        [Tooltip("WARNING: Aggressive cursor management for demo purposes. May conflict with game cursor logic.")]
        private bool forceCursorAlwaysAvailable = true;

        [Header("Generated UI References (Auto-populated)")]
        [SerializeField] private Canvas uiCanvas;
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject loggedInPanel;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button googleLoginButton;
        [SerializeField] private Button appleLoginButton;
        [SerializeField] private Button facebookLoginButton;
        [SerializeField] private Button logoutButton;
        [SerializeField] private Text statusText;
        [SerializeField] private Text userInfoText;

        private bool uiBuilt = false;

        private void Awake()
        {
            // Find PassportManager if not assigned
            if (passportManager == null)
            {
                passportManager = FindObjectOfType<PassportManager>();
                if (passportManager == null)
                {
                    Debug.LogError("[PassportUIBuilder] No PassportManager found in scene. Please assign one in the Inspector.");
                    return;
                }
            }

            BuildUI();
        }

        /// <summary>
        /// Build the complete UI hierarchy at runtime
        /// </summary>
        public void BuildUI()
        {
            if (uiBuilt)
            {
                Debug.LogWarning("[PassportUIBuilder] UI already built.");
                return;
            }

            Debug.Log("[PassportUIBuilder] Building Passport UI...");

            // Clean up any existing UI first
            if (uiCanvas != null)
            {
                Debug.Log("[PassportUIBuilder] Cleaning up existing UI...");
                DestroyImmediate(uiCanvas.gameObject);
                uiCanvas = null;
            }

            CreateCanvas();
            CreateLoginPanel();
            CreateLoggedInPanel();
            WireUpPassportManager();

            // Start with login panel visible
            ShowLoginPanel();

            uiBuilt = true;
            Debug.Log("[PassportUIBuilder] Passport UI built successfully!");

            // Start cursor management if enabled
            if (forceCursorAlwaysAvailable)
            {
                InvokeRepeating(nameof(EnsureCursorAvailable), 0.1f, 0.1f);
            }
        }

        /// <summary>
        /// Ensure cursor is always available for UI interaction (demo feature)
        /// WARNING: This is aggressive cursor management for demo purposes
        /// </summary>
        private void EnsureCursorAvailable()
        {
            if (forceCursorAlwaysAvailable)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        /// <summary>
        /// Create the main canvas
        /// </summary>
        private void CreateCanvas()
        {
            // Create Canvas GameObject
            GameObject canvasObj = new GameObject("PassportUI");
            canvasObj.transform.SetParent(transform);

            // Add Canvas component
            uiCanvas = canvasObj.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiCanvas.sortingOrder = canvasOrder;

            // Add CanvasScaler for responsive design
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(360, 640); // Mobile reference
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // Add GraphicRaycaster for input
            canvasObj.AddComponent<GraphicRaycaster>();

            // Ensure EventSystem exists
            EnsureEventSystem();
        }

        /// <summary>
        /// Create the login panel with all login buttons
        /// </summary>
        private void CreateLoginPanel()
        {
            // Create login panel
            loginPanel = CreatePanel("LoginPanel", uiCanvas.transform);

            // Create title
            CreateText("Login", loginPanel.transform, new Vector2(0, 150), 24, TextAnchor.UpperCenter);

            // Create status text
            statusText = CreateText("Initializing Passport...", loginPanel.transform, new Vector2(0, 100), 16, TextAnchor.UpperCenter);
            statusText.color = Color.yellow;

            // Create login buttons with proper spacing
            float startY = 50f;
            float spacing = buttonSize.y + elementSpacing;

            googleLoginButton = CreateButton("Google Login", "Continue with Google", loginPanel.transform,
                new Vector2(0, startY - (spacing * 0)), Color.white, new Color(0.86f, 0.27f, 0.22f, 1f)); // Google Red

            appleLoginButton = CreateButton("Apple Login", "Continue with Apple", loginPanel.transform,
                new Vector2(0, startY - (spacing * 1)), Color.white, Color.black);

            facebookLoginButton = CreateButton("Facebook Login", "Continue with Facebook", loginPanel.transform,
                new Vector2(0, startY - (spacing * 2)), Color.white, new Color(0.26f, 0.40f, 0.70f, 1f)); // Facebook Blue

            loginButton = CreateButton("Default Login", "Login", loginPanel.transform,
                new Vector2(0, startY - (spacing * 3)), Color.black, new Color(0.9f, 0.9f, 0.9f, 1f)); // Light gray for visibility
        }

        /// <summary>
        /// Create the logged-in panel with user info and logout
        /// </summary>
        private void CreateLoggedInPanel()
        {
            // Create logged-in panel
            loggedInPanel = CreatePanel("LoggedInPanel", uiCanvas.transform);

            // Create welcome text
            CreateText("Welcome!", loggedInPanel.transform, new Vector2(0, 100), 24, TextAnchor.UpperCenter);

            // Create user info text
            userInfoText = CreateText("Logged in successfully", loggedInPanel.transform, new Vector2(0, 50), 14, TextAnchor.UpperCenter);
            userInfoText.color = Color.green;

            // Create logout button
            logoutButton = CreateButton("Logout", "Logout", loggedInPanel.transform,
                new Vector2(0, -50), Color.white, new Color(0.8f, 0.3f, 0.3f, 1f)); // Red for logout
        }

        /// <summary>
        /// Create a panel GameObject with background
        /// </summary>
        private GameObject CreatePanel(string name, Transform parent)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            // Add RectTransform
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = panelSize;

            // Add background image
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f); // Semi-transparent dark background

            Debug.Log($"[PassportUIBuilder] Created panel: '{name}' with size: {panelSize}");

            return panel;
        }

        /// <summary>
        /// Create a button with text and styling
        /// </summary>
        private Button CreateButton(string name, string text, Transform parent, Vector2 position, Color textColor, Color buttonColor)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);

            // Add RectTransform
            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = buttonSize;

            // Add Image component for button background
            Image image = buttonObj.AddComponent<Image>();
            image.color = buttonColor;

            // Add Button component
            Button button = buttonObj.AddComponent<Button>();

            Debug.Log($"[PassportUIBuilder] Created button '{name}' at position {position} with text '{text}'");

            // Create button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            Text buttonText = textObj.AddComponent<Text>();
            buttonText.text = text;

            // Try to get a reliable font
            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                Debug.Log("[PassportUIBuilder] Using LegacyRuntime font");
            }
            else
            {
                Debug.Log("[PassportUIBuilder] Using Arial font");
            }

            if (font == null)
            {
                // As a last resort, try to find any font
                Font[] allFonts = Resources.FindObjectsOfTypeAll<Font>();
                if (allFonts.Length > 0)
                {
                    font = allFonts[0];
                    Debug.Log($"[PassportUIBuilder] Using fallback font: {font.name}");
                }
                else
                {
                    Debug.LogError("[PassportUIBuilder] No fonts available! Text will not display.");
                }
            }

            buttonText.font = font;
            buttonText.fontSize = 16;
            buttonText.color = textColor;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.horizontalOverflow = HorizontalWrapMode.Overflow;
            buttonText.verticalOverflow = VerticalWrapMode.Overflow;

            Debug.Log($"[PassportUIBuilder] Created button text: '{text}' with font: {(font ? font.name : "NULL")} and color: {textColor}");

            return button;
        }

        /// <summary>
        /// Create a text element
        /// </summary>
        private Text CreateText(string content, Transform parent, Vector2 position, int fontSize, TextAnchor alignment)
        {
            GameObject textObj = new GameObject("Text_" + content.Replace(" ", ""));
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(panelSize.x - 20, 30);

            Text text = textObj.AddComponent<Text>();
            text.text = content;

            // Use the same font finding logic as buttons
            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            if (font == null)
            {
                Font[] allFonts = Resources.FindObjectsOfTypeAll<Font>();
                if (allFonts.Length > 0)
                {
                    font = allFonts[0];
                    Debug.Log($"[PassportUIBuilder] Using fallback font for text: {font.name}");
                }
            }

            text.font = font;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            Debug.Log($"[PassportUIBuilder] Created text: '{content}' with font: {(font ? font.name : "NULL")} at position: {position}");

            return text;
        }

        /// <summary>
        /// Connect the generated UI to the PassportManager
        /// </summary>
        private void WireUpPassportManager()
        {
            if (passportManager == null) return;

            // Set UI references using the public method
            passportManager.SetUIReferences(
                loginButton,
                googleLoginButton,
                appleLoginButton,
                facebookLoginButton,
                logoutButton,
                statusText,
                userInfoText
            );

            Debug.Log("[PassportUIBuilder] UI wired to PassportManager successfully!");
            Debug.Log($"[PassportUIBuilder] Button click listeners: Google={googleLoginButton.onClick.GetPersistentEventCount()}");
        }

        /// <summary>
        /// Show the login panel and hide the logged-in panel
        /// </summary>
        public void ShowLoginPanel()
        {
            if (loginPanel != null) loginPanel.SetActive(true);
            if (loggedInPanel != null) loggedInPanel.SetActive(false);
        }

        /// <summary>
        /// Show the logged-in panel and hide the login panel
        /// </summary>
        public void ShowLoggedInPanel()
        {
            if (loginPanel != null) loginPanel.SetActive(false);
            if (loggedInPanel != null) loggedInPanel.SetActive(true);
        }

        /// <summary>
        /// Ensure EventSystem exists for UI input
        /// </summary>
        private void EnsureEventSystem()
        {
            EventSystem eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                Debug.Log("[PassportUIBuilder] Creating EventSystem for UI input...");
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }
            else
            {
                Debug.Log("[PassportUIBuilder] EventSystem already exists.");
            }
        }

        /// <summary>
        /// Public method to rebuild UI if needed
        /// </summary>
        [ContextMenu("Rebuild UI")]
        public void RebuildUI()
        {
            // Destroy existing UI
            if (uiCanvas != null)
            {
                DestroyImmediate(uiCanvas.gameObject);
            }

            uiBuilt = false;
            BuildUI();
        }
    }
}
