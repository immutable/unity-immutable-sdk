using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Immutable.Passport;
using System;

namespace Immutable.Passport.Sample.PassportFeatures
{
    public class ImxIsRegisteredOffchainScript : MonoBehaviour
    {
        [Header("IsRegisteredOffchain UI")]
        public Button checkRegistrationButton;
        public Text output;

        public void CheckIsRegisteredOffchain()
        {
            CheckIsRegisteredOffchainAsync().Forget();
        }

        private async UniTaskVoid CheckIsRegisteredOffchainAsync()
        {
            if (Passport.Instance == null)
            {
                ShowOutput("Passport not initialized.");
                return;
            }

            if (!SampleAppManager.IsConnectedToImx)
            {
                ShowOutput("Please connect to Immutable X first.");
                return;
            }

            ShowOutput("Checking if registered offchain...");
            try
            {
                bool isRegistered = await SampleAppManager.PassportInstance.IsRegisteredOffchain();
                
                if (isRegistered)
                {
                    ShowOutput("User is registered offchain.");
                }
                else
                {
                    ShowOutput("User is NOT registered offchain.");
                }
            }
            catch (System.Exception ex)
            {
                ShowOutput($"Failed to check registration: {ex.Message}");
            }
        }

        private void ShowOutput(string message)
        {
            Debug.Log($"[ImxIsRegisteredOffchainScript] {message}");
            if (output != null)
                output.text = message;
        }
    }
}