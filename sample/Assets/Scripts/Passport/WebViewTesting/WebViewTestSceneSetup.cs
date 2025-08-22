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
            rectTransform.offsetMin = new Vector2(50, 50);
            rectTransform.offsetMax = new Vector2(-50, -50);
            
            Image image = panelGO.AddComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            return panelGO;
        }
        
        private static void CreateTestUI(Transform parent, WebViewTestManager manager)
        {
            // Title
            CreateText(parent, "WebView Testing Framework", new Vector2(0, 200), 24, TextAnchor.MiddleCenter);
            
            // Package Selection
            CreateText(parent, "Select WebView Package:", new Vector2(-200, 150), 16, TextAnchor.MiddleLeft);
            CreateDropdown(parent, new Vector2(100, 150), manager);
            
            // Test Buttons
            GameObject testLoginBtn = CreateButton(parent, "Test Login Page", new Vector2(-100, 100));
            GameObject testMsgBtn = CreateButton(parent, "Test Messaging", new Vector2(100, 100));
            GameObject closeBtn = CreateButton(parent, "Close WebView", new Vector2(0, 50));
            
            // Navigation Controls Section
            CreateText(parent, "Navigation Controls:", new Vector2(0, 0), 16, TextAnchor.MiddleCenter);
            
            // URL Input Field
            GameObject urlInputGO = CreateInputField(parent, "Enter URL...", new Vector2(0, -30));
            
            // Navigation Buttons
            GameObject navigateBtn = CreateButton(parent, "Navigate", new Vector2(-150, -70));
            GameObject backBtn = CreateButton(parent, "Back", new Vector2(-50, -70));
            GameObject forwardBtn = CreateButton(parent, "Forward", new Vector2(50, -70));
            GameObject refreshBtn = CreateButton(parent, "Refresh", new Vector2(150, -70));
            
            // Debug/Test Buttons
            GameObject testInputBtn = CreateButton(parent, "Test Input", new Vector2(-100, -110));
            GameObject findWebViewBtn = CreateButton(parent, "Find WebView", new Vector2(0, -110));
            GameObject testPopupBtn = CreateButton(parent, "Test Popup", new Vector2(100, -110));
            
            // Status Output
            CreateText(parent, "Status: Ready", new Vector2(0, -150), 14, TextAnchor.MiddleCenter, "StatusOutput");
            
            // Performance Output
            CreateText(parent, "Performance: -", new Vector2(0, -200), 12, TextAnchor.MiddleCenter, "PerformanceOutput");
            
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
            manager.statusOutput = GameObject.Find("StatusOutput")?.GetComponent<Text>();
            manager.performanceOutput = GameObject.Find("PerformanceOutput")?.GetComponent<Text>();
        }
        
        private static GameObject CreateButton(Transform parent, string text, Vector2 position)
        {
            GameObject buttonGO = new GameObject($"Button_{text.Replace(" ", "")}");
            buttonGO.transform.SetParent(parent, false);
            
            RectTransform rectTransform = buttonGO.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = new Vector2(180, 40);
            
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
            textComponent.fontSize = 14;
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
            rectTransform.sizeDelta = new Vector2(400, 30);
            
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
        
        private static GameObject CreateDropdown(Transform parent, Vector2 position, WebViewTestManager manager)
        {
            GameObject dropdownGO = new GameObject("PackageDropdown");
            dropdownGO.transform.SetParent(parent, false);
            
            RectTransform rectTransform = dropdownGO.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = new Vector2(200, 30);
            
            Image background = dropdownGO.AddComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.9f);
            
            UnityEngine.UI.Dropdown dropdown = dropdownGO.AddComponent<UnityEngine.UI.Dropdown>();
            dropdown.targetGraphic = background;
            
            // Create dropdown template
            CreateDropdownTemplate(dropdownGO, dropdown);
            
            // Add options
            dropdown.options.Clear();
            dropdown.options.Add(new UnityEngine.UI.Dropdown.OptionData("Volt Unity Web Browser"));
            dropdown.options.Add(new UnityEngine.UI.Dropdown.OptionData("Alacrity WebView"));
            dropdown.options.Add(new UnityEngine.UI.Dropdown.OptionData("UWebView2"));
            dropdown.options.Add(new UnityEngine.UI.Dropdown.OptionData("ZenFulcrum Browser"));
            dropdown.options.Add(new UnityEngine.UI.Dropdown.OptionData("Vuplex 3D WebView"));
            
            // Set initial value (Volt Unity Web Browser = index 0)
            dropdown.value = 0;
            dropdown.RefreshShownValue();
            
            // Wire up event
            dropdown.onValueChanged.AddListener((int index) => {
                manager.selectedPackage = (WebViewTestManager.WebViewPackage)index;
                Debug.Log($"Selected WebView package: {manager.selectedPackage}");
            });
            
            return dropdownGO;
        }
        
        private static void CreateDropdownTemplate(GameObject dropdownGO, UnityEngine.UI.Dropdown dropdown)
        {
            // Create label (caption text)
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(dropdownGO.transform, false);
            
            RectTransform labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10, 2);
            labelRect.offsetMax = new Vector2(-25, -2);
            
            Text labelText = labelGO.AddComponent<Text>();
            labelText.text = "Volt Unity Web Browser";
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 12;
            labelText.color = Color.black;
            labelText.alignment = TextAnchor.MiddleLeft;
            
            dropdown.captionText = labelText;
            
            // Create arrow
            GameObject arrowGO = new GameObject("Arrow");
            arrowGO.transform.SetParent(dropdownGO.transform, false);
            
            RectTransform arrowRect = arrowGO.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0.5f);
            arrowRect.anchorMax = new Vector2(1, 0.5f);
            arrowRect.sizeDelta = new Vector2(20, 20);
            arrowRect.anchoredPosition = new Vector2(-15, 0);
            
            Text arrowText = arrowGO.AddComponent<Text>();
            arrowText.text = "▼";
            arrowText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            arrowText.fontSize = 12;
            arrowText.color = Color.black;
            arrowText.alignment = TextAnchor.MiddleCenter;
            
            // Create proper template with Toggle component
            GameObject templateGO = new GameObject("Template");
            templateGO.transform.SetParent(dropdownGO.transform, false);
            templateGO.SetActive(false);
            
            RectTransform templateRect = templateGO.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.anchoredPosition = new Vector2(0, 2);
            templateRect.sizeDelta = new Vector2(0, 150);
            
            Image templateImage = templateGO.AddComponent<Image>();
            templateImage.color = new Color(1f, 1f, 1f, 0.95f);
            
            // Create Viewport
            GameObject viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(templateGO.transform, false);
            
            RectTransform viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;
            
            Image viewportImage = viewportGO.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0f); // Transparent
            viewportImage.raycastTarget = false;
            
            UnityEngine.UI.Mask viewportMask = viewportGO.AddComponent<UnityEngine.UI.Mask>();
            viewportMask.showMaskGraphic = false;
            
            // Create Content
            GameObject contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 20);
            
            // Create Item (with Toggle component)
            GameObject itemGO = new GameObject("Item");
            itemGO.transform.SetParent(contentGO.transform, false);
            
            RectTransform itemRect = itemGO.AddComponent<RectTransform>();
            itemRect.anchorMin = Vector2.zero;
            itemRect.anchorMax = new Vector2(1, 1);
            itemRect.sizeDelta = Vector2.zero;
            itemRect.anchoredPosition = Vector2.zero;
            
            UnityEngine.UI.Toggle itemToggle = itemGO.AddComponent<UnityEngine.UI.Toggle>();
            itemToggle.targetGraphic = null;
            itemToggle.isOn = false;
            
            // Create Item Background
            GameObject itemBgGO = new GameObject("Item Background");
            itemBgGO.transform.SetParent(itemGO.transform, false);
            
            RectTransform itemBgRect = itemBgGO.AddComponent<RectTransform>();
            itemBgRect.anchorMin = Vector2.zero;
            itemBgRect.anchorMax = Vector2.one;
            itemBgRect.sizeDelta = Vector2.zero;
            itemBgRect.anchoredPosition = Vector2.zero;
            
            Image itemBgImage = itemBgGO.AddComponent<Image>();
            itemBgImage.color = new Color(0.96f, 0.96f, 0.96f, 1f);
            
            itemToggle.targetGraphic = itemBgImage;
            
            // Create Item Label
            GameObject itemLabelGO = new GameObject("Item Label");
            itemLabelGO.transform.SetParent(itemGO.transform, false);
            
            RectTransform itemLabelRect = itemLabelGO.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(10, 1);
            itemLabelRect.offsetMax = new Vector2(-10, -2);
            
            Text itemLabelText = itemLabelGO.AddComponent<Text>();
            itemLabelText.text = "Option";
            itemLabelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            itemLabelText.fontSize = 12;
            itemLabelText.color = Color.black;
            itemLabelText.alignment = TextAnchor.MiddleLeft;
            
            // Set dropdown references
            dropdown.template = templateRect;
            dropdown.captionText = labelText;
            dropdown.itemText = itemLabelText;
        }
    }
}
#endif
