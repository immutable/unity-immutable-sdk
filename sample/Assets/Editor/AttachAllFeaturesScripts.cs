using UnityEditor;
using UnityEngine;
using System;
using Object = UnityEngine.Object;

public class AttachAllFeatureScripts
{
    [MenuItem("Tools/Attach All Passport Feature Scripts To All GameObjects")]
    static void AttachScripts()
    {
        string[] scriptNames = new string[]
        {
            "LoginScript",
            "LogoutScript",
            "ReloginScript",
            "ReconnectScript",
            "GetAccessTokenScript",
            "GetIdTokenScript",
            "GetUserInfoScript",
            "GetLinkedAddressesScript",
            "LinkWalletScript",
            "ImxConnectScript",
            "ImxRegisterScript",
            "ImxGetAddressScript",
            "ImxNftTransferScript",
            "ZkEvmSendTransactionScript",
            "ZkEvmGetBalanceScript",
            "ZkEvmGetTransactionReceiptScript",
            "ZkEvmSignTypedDataScript",
            "SetCallTimeoutScript"
        };

        foreach (GameObject go in Object.FindObjectsOfType<GameObject>())
        {
            if (!go.scene.isLoaded) continue;
            foreach (string scriptName in scriptNames)
            {
                Type scriptType = GetTypeByName(scriptName);
                if (scriptType != null && go.GetComponent(scriptType) == null)
                {
                    go.AddComponent(scriptType);
                    Debug.Log($"Added {scriptName} to {go.name}");
                }
            }
        }
        Debug.Log("All feature scripts attached to all GameObjects in the scene.");
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