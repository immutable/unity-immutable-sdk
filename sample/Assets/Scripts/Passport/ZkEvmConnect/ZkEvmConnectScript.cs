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

            ShowOutput("Connecting to zkEVM...");
            try
            {
                await Passport.Instance.ConnectEvm();
                ShowOutput("zkEVM connection successful.");
            }
            catch (System.Exception ex)
            {
                ShowOutput($"zkEVM connection failed: {ex.Message}");
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