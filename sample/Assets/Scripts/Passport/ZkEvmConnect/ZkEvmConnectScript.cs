using System;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Immutable.Passport;

namespace Immutable.Passport.Sample.PassportFeatures
{
    public class ZkEvmConnectScript : MonoBehaviour
    {
        [Header("zkEVM Connect UI")]
        public Button connectButton;
        public Text output;

        // Optionally remove Start() if wiring up in Inspector
        // private void Start()
        // {
        //     if (connectButton != null)
        //     {
        //         connectButton.onClick.RemoveAllListeners();
        //         connectButton.onClick.AddListener(() => { ConnectZkEvmAsync().Forget(); });
        //     }
        // }

        public void ConnectZkEvm()
        {
            ConnectZkEvmAsync().Forget();
        }

        private async UniTaskVoid ConnectZkEvmAsync()
        {
            if (Passport.Instance == null)
            {
                ShowOutput("Passport not initialized.");
                return;
            }
            // Set the static property for global access
            SampleAppManager.PassportInstance = Passport.Instance;

            ShowOutput("Connecting to zkEVM...");
            try
            {
                await Passport.Instance.ConnectEvm();
                
                // Add these lines to update connection state and refresh UI
                SampleAppManager.IsConnectedToZkEvm = true;
                var sceneManager = FindObjectOfType<AuthenticatedSceneManager>();
                if (sceneManager != null)
                {
                    sceneManager.UpdateZkEvmButtonStates();
                    Debug.Log("Updated zkEVM button states after connection");
                }
                else
                {
                    Debug.LogWarning("Could not find AuthenticatedSceneManager to update button states");
                }
                
                ShowOutput("Connected to zkEVM");
            }
            catch (System.Exception ex)
            {
                ShowOutput($"Failed to connect to zkEVM: {ex.Message}");
            }
        }

        private void ShowOutput(string message)
        {
            Debug.Log($"[ZkEvmConnectScript] {message}");
            if (output != null)
                output.text = message;
        }
    }
} 