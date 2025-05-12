using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Immutable.Passport.Sample.PassportFeatures
{
    public class ZkEvmConnectScript : MonoBehaviour
    {
        [SerializeField] private Button ConnectEvmButton;
        [SerializeField] private Button SendTransactionButton;
        [SerializeField] private Button RequestAccountsButton;
        [SerializeField] private Button GetBalanceButton;
        [SerializeField] private Button GetTransactionReceiptButton;
        [SerializeField] private Button SignTypedDataButton;
        [SerializeField] private Text Output;

        private Passport Passport;

        private void Awake()
        {
            if (Passport.Instance != null)
            {
                Passport = Passport.Instance;
            }
        }

        public async void ConnectZkEvm()
        {
            try
            {
                await Passport.ConnectEvm();
                SampleAppManager.IsConnectedToZkEvm = true;
                if (ConnectEvmButton != null) ConnectEvmButton.gameObject.SetActive(false);
                if (SendTransactionButton != null) SendTransactionButton.gameObject.SetActive(true);
                if (RequestAccountsButton != null) RequestAccountsButton.gameObject.SetActive(true);
                if (GetBalanceButton != null) GetBalanceButton.gameObject.SetActive(true);
                if (GetTransactionReceiptButton != null) GetTransactionReceiptButton.gameObject.SetActive(true);
                if (SignTypedDataButton != null) SignTypedDataButton.gameObject.SetActive(true);
                ShowOutput("Connected to EVM");
            }
            catch (Exception ex)
            {
                ShowOutput($"Failed to instantiate zkEVM provider: {ex.Message}");
            }
        }

        private void ShowOutput(string message)
        {
            if (Output != null)
            {
                Output.text = message;
            }
        }
    }
} 