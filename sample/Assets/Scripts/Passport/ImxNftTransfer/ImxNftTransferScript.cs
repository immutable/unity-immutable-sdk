using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using Immutable.Passport;
using Immutable.Passport.Model;

public class ImxNftTransferScript : MonoBehaviour
{
    [SerializeField] private Text Output;
    [SerializeField] private InputField TokenIdInput1;
    [SerializeField] private InputField TokenAddressInput1;
    [SerializeField] private InputField ReceiverInput1;
    [SerializeField] private InputField TokenIdInput2;
    [SerializeField] private InputField TokenAddressInput2;
    [SerializeField] private InputField ReceiverInput2;

    /// <summary>
    /// Transfers NFTs to the specified receivers based on the provided details.
    /// </summary>
    public void Transfer()
    {
        TransferAsync();
    }

    private async UniTaskVoid TransferAsync()
    {
        if (Passport.Instance == null)
        {
            ShowOutput("Passport instance is null");
            return;
        }
        if (!string.IsNullOrWhiteSpace(TokenIdInput1.text) &&
            !string.IsNullOrWhiteSpace(TokenAddressInput1.text) &&
            !string.IsNullOrWhiteSpace(ReceiverInput1.text))
        {
            ShowOutput("Transferring NFTs...");
            try
            {
                List<NftTransferDetails> transferDetails = GetTransferDetails();
                if (transferDetails.Count > 1)
                {
                    CreateBatchTransferResponse response = await Passport.Instance.ImxBatchNftTransfer(transferDetails.ToArray());
                    ShowOutput($"Successfully transferred {response.transfer_ids.Length} NFTs.");
                }
                else
                {
                    NftTransferDetails nftTransferDetail = transferDetails[0];
                    UnsignedTransferRequest transferRequest = UnsignedTransferRequest.ERC721(
                        nftTransferDetail.receiver,
                        nftTransferDetail.tokenId,
                        nftTransferDetail.tokenAddress
                    );
                    CreateTransferResponseV1 response = await Passport.Instance.ImxTransfer(transferRequest);
                    ShowOutput($"NFT transferred successfully. Transfer ID: {response.transfer_id}");
                }
            }
            catch (System.Exception ex)
            {
                ShowOutput($"Failed to transfer NFTs: {ex.Message}");
            }
        }
        else
        {
            ShowOutput("Please fill in all required fields for the first NFT transfer.");
        }
    }

    private List<NftTransferDetails> GetTransferDetails()
    {
        List<NftTransferDetails> details = new List<NftTransferDetails>();
        if (!string.IsNullOrWhiteSpace(TokenIdInput1.text) &&
            !string.IsNullOrWhiteSpace(TokenAddressInput1.text) &&
            !string.IsNullOrWhiteSpace(ReceiverInput1.text))
        {
            details.Add(new NftTransferDetails(
                ReceiverInput1.text,
                TokenIdInput1.text,
                TokenAddressInput1.text
            ));
        }
        if (!string.IsNullOrWhiteSpace(TokenIdInput2.text) &&
            !string.IsNullOrWhiteSpace(TokenAddressInput2.text) &&
            !string.IsNullOrWhiteSpace(ReceiverInput2.text))
        {
            details.Add(new NftTransferDetails(
                ReceiverInput2.text,
                TokenIdInput2.text,
                TokenAddressInput2.text
            ));
        }
        return details;
    }

    public void Cancel()
    {
        SceneManager.LoadScene("AuthenticatedScene");
    }

    private void ShowOutput(string message)
    {
        if (Output != null)
        {
            Output.text = message;
        }
    }
}