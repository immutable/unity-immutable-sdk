#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Immutable.Passport.WebViewTesting
{
    /// <summary>
    /// Editor utility to create WebView test scene
    /// </summary>
    public class WebViewTestSceneSetup
    {
        [MenuItem("Immutable/WebView Testing/Create WebView Test Scene")]
        public static void CreateWebViewTestScene()
        {
            CreateWebViewTestSceneInternal(false);
        }
        
        [MenuItem("Immutable/WebView Testing/Recreate WebView Test Scene")]
        public static void RecreateWebViewTestScene()
        {
            CreateWebViewTestSceneInternal(true);
        }
        
        private static void CreateWebViewTestSceneInternal(bool forceRecreate)
        {
            string scenePath = "Assets/Scenes/Passport/WebViewTest.unity";
            
            if (forceRecreate)
            {
                Debug.Log("Recreating WebView Test Scene with updated UI (including dropdown)...");
            }
            
            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Create Canvas
            GameObject canvasGO = new GameObject("Canvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            
            // Create EventSystem
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            
            // Create WebView Test Manager
            GameObject managerGO = new GameObject("WebViewTestManager");
            WebViewTestManager manager = managerGO.AddComponent<WebViewTestManager>();
            
            // Create UI Panel
            GameObject panelGO = CreateUIPanel(canvasGO.transform);
            
            // Create UI Elements
            CreateTestUI(panelGO.transform, manager);
            
            // Save scene
            EditorSceneManager.SaveScene(scene, scenePath);
            
            Debug.Log($"WebView Test Scene created at: {scenePath}");
        }
        
        private static GameObject CreateUIPanel(Transform parent)
        {
            GameObject panelGO = new GameObject("TestPanel");
            panelGO.transform.SetParent(parent, false);
            
            RectTransform rectTransform = panelGO.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(0, 0);
            rectTransform.offsetMax = new Vector2(0, 0);
            rectTransform.pivot = new Vector2(0, 0);
            
            Image image = panelGO.AddComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            return panelGO;
        }
        
        private static void CreateTestUI(Transform parent, WebViewTestManager manager)
        {
            // Create control panel (red section - always visible)
            GameObject controlPanel = CreateControlPanel(parent);
            
            // Create WebView area (green section - for WebView display)
            GameObject webViewArea = CreateWebViewArea(parent);
            
            // Setup controls in the control panel
            CreateControlsInPanel(controlPanel.transform, manager);
            
            // Store reference to WebView area for the manager
            manager.webViewContainer = webViewArea;
        }
        
        private static GameObject CreateButton(Transform parent, string text, Vector2 position)
        {
            GameObject buttonGO = new GameObject($"Button_{text.Replace(" ", "")}");
            buttonGO.transform.SetParent(parent, false);
            
            RectTransform rectTransform = buttonGO.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = new Vector2(90, 25);
            
            Image image = buttonGO.AddComponent<Image>();
            image.color = new Color(0.2f, 0.6f, 1f, 1f);
            
            Button button = buttonGO.AddComponent<Button>();
            button.targetGraphic = image;
            
            // Button text
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            Text textComponent = textGO.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = 11;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleCenter;
            
            return buttonGO;
        }
        
        private static GameObject CreateText(Transform parent, string text, Vector2 position, int fontSize, TextAnchor alignment, string name = null)
        {
            GameObject textGO = new GameObject(name ?? $"Text_{text.Replace(" ", "").Substring(0, Mathf.Min(10, text.Length))}");
            textGO.transform.SetParent(parent, false);
            
            RectTransform rectTransform = textGO.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = new Vector2(400, 30);
            
            Text textComponent = textGO.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = fontSize;
            textComponent.color = Color.white;
            textComponent.alignment = alignment;
            
            return textGO;
        }
        
        private static GameObject CreateInputField(Transform parent, string placeholder, Vector2 position)
        {
            GameObject inputFieldGO = new GameObject("InputField_URL");
            inputFieldGO.transform.SetParent(parent, false);
            
            RectTransform rectTransform = inputFieldGO.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = new Vector2(140, 25);
            
            Image image = inputFieldGO.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.8f);
            
            InputField inputField = inputFieldGO.AddComponent<InputField>();
            
            // Create Text component for the input field
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(inputFieldGO.transform, false);
            
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);
            
            Text textComponent = textGO.AddComponent<Text>();
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = 14;
            textComponent.color = Color.black;
            textComponent.alignment = TextAnchor.MiddleLeft;
            
            // Create Placeholder
            GameObject placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(inputFieldGO.transform, false);
            
            RectTransform placeholderRect = placeholderGO.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(10, 0);
            placeholderRect.offsetMax = new Vector2(-10, 0);
            
            Text placeholderComponent = placeholderGO.AddComponent<Text>();
            placeholderComponent.text = placeholder;
            placeholderComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            placeholderComponent.fontSize = 14;
            placeholderComponent.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            placeholderComponent.alignment = TextAnchor.MiddleLeft;
            
            // Wire up InputField
            inputField.textComponent = textComponent;
            inputField.placeholder = placeholderComponent;
            
            return inputFieldGO;
        }
        
        private static GameObject CreateControlPanel(Transform parent)
        {
            GameObject controlPanel = new GameObject("ControlPanel");
            controlPanel.transform.SetParent(parent, false);
            RectTransform rectTransform = controlPanel.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 0);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0, 0);
            rectTransform.sizeDelta = new Vector2(0, 50);
            rectTransform.anchoredPosition = new Vector2(0, 1030);

            Image background = controlPanel.AddComponent<Image>();
            background.color = new Color(0.247f, 0.247f, 0.247f, 0.9f); // Grey background
            
            // Add vertical layout group for proper spacing
            VerticalLayoutGroup layoutGroup = controlPanel.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 5f;
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            
            return controlPanel;
        }
        
        private static GameObject CreateWebViewArea(Transform parent)
        {
            GameObject webViewArea = new GameObject("WebViewArea");
            webViewArea.transform.SetParent(parent, false);
            
            RectTransform rectTransform = webViewArea.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0, 0);
            rectTransform.sizeDelta = new Vector2(0, -45);
            
            Image background = webViewArea.AddComponent<Image>();
            background.color = new Color(0.2f, 0.8f, 0.2f, 0.9f); // Green background
            
            // Add label (positioned properly within the area)
            GameObject labelGO = new GameObject("WebViewLabel");
            labelGO.transform.SetParent(webViewArea.transform, false);
            
            RectTransform labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.9f);
            labelRect.anchorMax = new Vector2(1, 1f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            
            Text labelText = labelGO.AddComponent<Text>();
            labelText.text = "WebView Display Area (Press D to see dimensions)";
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 14;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleCenter;
            
            return webViewArea;
        }
        
        private static void CreateControlsInPanel(Transform parent, WebViewTestManager manager)
        {          
            GameObject buttonRow1 = CreateHorizontalRow(parent);
            GameObject testLoginBtn = CreateLayoutButton(buttonRow1.transform, "Test Login");
            GameObject testMsgBtn = CreateLayoutButton(buttonRow1.transform, "Test Messaging");
            GameObject closeBtn = CreateLayoutButton(buttonRow1.transform, "Close WebView");
            GameObject findWebViewBtn = CreateLayoutButton(buttonRow1.transform, "Find WebView");
            GameObject urlLabel = CreateLayoutText(buttonRow1.transform, "URL:", 12);
            GameObject urlInputGO = CreateLayoutInputField(buttonRow1.transform, "Enter URL...");
            GameObject navigateBtn = CreateLayoutButton(buttonRow1.transform, "Go");
            GameObject refreshBtn = CreateLayoutButton(buttonRow1.transform, "Refresh");
            GameObject backBtn = CreateLayoutButton(buttonRow1.transform, "Back");
            GameObject forwardBtn = CreateLayoutButton(buttonRow1.transform, "Forward");
            GameObject testInputBtn = CreateLayoutButton(buttonRow1.transform, "Test Input");
            GameObject testPopupBtn = CreateLayoutButton(buttonRow1.transform, "Test Popup");
            GameObject statusOutput = CreateLayoutText(buttonRow1.transform, "Status: Ready", 10, "StatusOutput");
            GameObject performanceOutput = CreateLayoutText(buttonRow1.transform, "Performance: -", 10, "PerformanceOutput");
            
            // Wire up manager references
            manager.testLoginButton = testLoginBtn.GetComponent<Button>();
            manager.testMessagingButton = testMsgBtn.GetComponent<Button>();
            manager.closeWebViewButton = closeBtn.GetComponent<Button>();
            manager.urlInputField = urlInputGO.GetComponent<InputField>();
            manager.navigateButton = navigateBtn.GetComponent<Button>();
            manager.backButton = backBtn.GetComponent<Button>();
            manager.forwardButton = forwardBtn.GetComponent<Button>();
            manager.refreshButton = refreshBtn.GetComponent<Button>();
            manager.testInputButton = testInputBtn.GetComponent<Button>();
            manager.findWebViewButton = findWebViewBtn.GetComponent<Button>();
            manager.testPopupButton = testPopupBtn.GetComponent<Button>();
            manager.statusOutput = statusOutput.GetComponent<Text>();
            manager.performanceOutput = performanceOutput.GetComponent<Text>();
            
            // Set to UWB (only option)
            manager.selectedPackage = WebViewTestManager.WebViewPackage.VoltUnityWebBrowser;
        }
        
        private static GameObject CreateHorizontalRow(Transform parent)
        {
            GameObject row = new GameObject("Row");
            row.transform.SetParent(parent, false);
            
            RectTransform rectTransform = row.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 0);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            
            HorizontalLayoutGroup layoutGroup = row.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 10f;
            layoutGroup.padding = new RectOffset(5, 5, 0, 0);
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            
            LayoutElement layoutElement = row.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 30f;
            
            return row;
        }
        
        private static GameObject CreateLayoutButton(Transform parent, string text)
        {
            GameObject buttonGO = new GameObject($"Button_{text.Replace(" ", "")}");
            buttonGO.transform.SetParent(parent, false);
            
            RectTransform rectTransform = buttonGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(90, 25);
            
            Image image = buttonGO.AddComponent<Image>();
            image.color = new Color(0.2f, 0.6f, 1f, 1f);
            
            Button button = buttonGO.AddComponent<Button>();
            button.targetGraphic = image;
            
            // Button text
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            Text textComponent = textGO.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = 10;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleCenter;
            
            LayoutElement layoutElement = buttonGO.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 90f;
            layoutElement.preferredHeight = 25f;
            
            return buttonGO;
        }
        
        private static GameObject CreateLayoutText(Transform parent, string text, int fontSize, string name = null)
        {
            GameObject textGO = new GameObject(name ?? $"Text_{text.Replace(" ", "").Substring(0, Mathf.Min(10, text.Length))}");
            textGO.transform.SetParent(parent, false);
            
            RectTransform rectTransform = textGO.AddComponent<RectTransform>();

            if (text.StartsWith("URL:"))
            {
                rectTransform.sizeDelta = new Vector2(30, 25);
            }
            else
            {
                rectTransform.sizeDelta = new Vector2(200, 25);
            }
            
            Text textComponent = textGO.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = fontSize;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleLeft;
            
            LayoutElement layoutElement = textGO.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 200f;
            layoutElement.preferredHeight = 25f;
            
            return textGO;
        }
        
        private static GameObject CreateLayoutInputField(Transform parent, string placeholder)
        {
            GameObject inputFieldGO = new GameObject("InputField_URL");
            inputFieldGO.transform.SetParent(parent, false);
            
            RectTransform rectTransform = inputFieldGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(300, 25);
            
            Image image = inputFieldGO.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.8f);
            
            InputField inputField = inputFieldGO.AddComponent<InputField>();
            inputField.transition = Selectable.Transition.ColorTint;
            
            // Text component for input field
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(inputFieldGO.transform, false);
            
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 0);
            textRect.offsetMax = new Vector2(-5, 0);
            
            Text textComponent = textGO.AddComponent<Text>();
            textComponent.text = "";
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = 12;
            textComponent.color = Color.black;
            textComponent.alignment = TextAnchor.MiddleLeft;
            
            // Placeholder
            GameObject placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(inputFieldGO.transform, false);
            
            RectTransform placeholderRect = placeholderGO.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(5, 0);
            placeholderRect.offsetMax = new Vector2(-5, 0);
            
            Text placeholderComponent = placeholderGO.AddComponent<Text>();
            placeholderComponent.text = placeholder;
            placeholderComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            placeholderComponent.fontSize = 12;
            placeholderComponent.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            placeholderComponent.alignment = TextAnchor.MiddleLeft;
            
            inputField.textComponent = textComponent;
            inputField.placeholder = placeholderComponent;
            
            LayoutElement layoutElement = inputFieldGO.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 200f;
            layoutElement.preferredHeight = 25f;
            
            return inputFieldGO;
        }
    }
}
#endif
