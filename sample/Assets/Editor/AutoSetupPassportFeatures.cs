using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Reflection;

public class AutoSetupPassportFeatures : EditorWindow
{
    [MenuItem("Tools/Auto-Setup Passport Feature Scripts")]
    public static void ShowWindow()
    {
        GetWindow<AutoSetupPassportFeatures>("Auto-Setup Passport Features");
    }

    void OnGUI()
    {
        if (GUILayout.Button("Auto-Setup All Passport Feature Scripts in Active Scene"))
        {
            SetupAllFeatures();
        }
    }

    static void SetupAllFeatures()
    {
        // List of features and their expected GameObject names
        (string scriptName, string goName)[] features = new (string, string)[]
        {
            ("LoginScript", "LoginPanel"),
            ("LogoutScript", "LogoutPanel"),
            ("ReloginScript", "ReloginPanel"),
            ("ReconnectScript", "ReconnectPanel"),
            ("GetAccessTokenScript", "GetAccessTokenPanel"),
            ("GetIdTokenScript", "GetIdTokenPanel"),
            ("GetUserInfoScript", "GetUserInfoPanel"),
            ("GetLinkedAddressesScript", "GetLinkedAddressesPanel"),
            ("LinkWalletScript", "LinkWalletPanel"),
            ("ImxConnectScript", "ImxConnectPanel"),
            ("ImxRegisterScript", "ImxRegisterPanel"),
            ("ImxGetAddressScript", "ImxGetAddressPanel"),
            ("ImxNftTransferScript", "ImxNftTransferPanel"),
            ("ZkEvmSendTransactionScript", "ZkEvmSendTransactionPanel"),
            ("ZkEvmGetBalanceScript", "ZkEvmGetBalancePanel"),
            ("ZkEvmGetTransactionReceiptScript", "ZkEvmGetTransactionReceiptPanel"),
            ("ZkEvmSignTypedDataScript", "ZkEvmSignTypedDataPanel"),
            ("SetCallTimeoutScript", "SetCallTimeoutPanel"),
        };

        foreach (var (scriptName, goName) in features)
        {
            GameObject go = GameObject.Find(goName);
            if (go == null)
            {
                Debug.LogWarning($"GameObject '{goName}' not found in scene. Skipping {scriptName}.");
                continue;
            }

            // Add the script if not already present
            Type scriptType = GetTypeByName(scriptName);
            if (scriptType == null)
            {
                Debug.LogWarning($"Script type '{scriptName}' not found. Make sure the script is compiled.");
                continue;
            }
            if (go.GetComponent(scriptType) == null)
            {
                go.AddComponent(scriptType);
                Debug.Log($"Added {scriptName} to {goName}");
            }

            // Try to auto-assign UI fields by name
            var script = go.GetComponent(scriptType);
            if (script != null)
            {
                var fields = scriptType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                foreach (var field in fields)
                {
                    if (typeof(Component).IsAssignableFrom(field.FieldType))
                    {
                        var uiGo = GameObject.Find(field.Name);
                        if (uiGo != null)
                        {
                            var comp = uiGo.GetComponent(field.FieldType);
                            if (comp != null)
                            {
                                field.SetValue(script, comp);
                                Debug.Log($"Assigned {field.Name} to {scriptName} on {goName}");
                            }
                        }
                    }
                }
            }

            // Try to auto-wire Button OnClick events
            var buttons = go.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                string methodName = button.name.Replace("Button", "");
                var method = scriptType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                {
                    // Remove all existing persistent listeners
                    int listenerCount = button.onClick.GetPersistentEventCount();
                    for (int i = listenerCount - 1; i >= 0; i--)
                    {
                        UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick, i);
                    }
                    // Add the new persistent listener
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), script, method) as UnityEngine.Events.UnityAction);
                    Debug.Log($"Wired {button.name} to {methodName} on {scriptName}");
                }
            }
        }

        Debug.Log("Auto-setup complete! Please check the Inspector for any fields that could not be auto-assigned.");
    }

    static Type GetTypeByName(string typeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(typeName);
            if (type != null)
                return type;
        }
        return null;
    }
}