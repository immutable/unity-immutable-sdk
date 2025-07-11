#nullable enable

using UnityEngine;
using UnityEngine.UI;

namespace Immutable.Passport.Sample.PassportFeatures
{
    public class ZkEvmConnectScript : MonoBehaviour
    {
        [SerializeField] public Text? output;

        public async void ConnectZkEvm()
        {
            if (Passport.Instance == null)
            {
                ShowOutput("Passport not initialised.");
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

                ShowOutput("Connected to EVM");
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