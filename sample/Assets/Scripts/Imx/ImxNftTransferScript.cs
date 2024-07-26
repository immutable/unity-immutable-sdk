using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;
using Immutable.Passport.Model;

public class ImxNftTransferScript : MonoBehaviour
{
#pragma warning disable CS8618
    [SerializeField] private Text Output;

    [SerializeField] private InputField TokenIdInput1;
    [SerializeField] private InputField TokenAddressInput1;
    [SerializeField] private InputField ReceiverInput1;

    [SerializeField] private InputField TokenIdInput2;
    [SerializeField] private InputField TokenAddressInput2;
    [SerializeField] private InputField ReceiverInput2;

    private Passport Passport;
#pragma warning restore CS8618

    void Start()
    {
        if (Passport.Instance != null)
        {
            // Get Passport instance
            Passport = Passport.Instance;
        }
        else
        {
            ShowOutput("Passport instance is null");
        }
    }

    /// <summary>
    /// Transfers NFTs to the specified receivers based on the provided details.
    /// </summary>
    public async void Transfer()
    {
        // Check if all necessary input fields are filled
        if (!string.IsNullOrWhiteSpace(TokenIdInput1.text) &&
            !string.IsNullOrWhiteSpace(TokenAddressInput1.text) &&
            !string.IsNullOrWhiteSpace(ReceiverInput1.text))
        {
            ShowOutput("Transferring NFTs...");

            try
            {
                // Gather all NFT transfer details
                List<NftTransferDetails> transferDetails = GetTransferDetails();

                // Perform batch transfer if multiple NFTs are specified
                if (transferDetails.Count > 1)
                {
                    CreateBatchTransferResponse response = await Passport.ImxBatchNftTransfer(transferDetails.ToArray());
                    ShowOutput($"Successfully transferred {response.transfer_ids.Length} NFTs.");
                }
                // Perform a single transfer if only one NFT is specified
                else
                {
                    NftTransferDetails nftTransferDetail = transferDetails[0];
                    UnsignedTransferRequest transferRequest = UnsignedTransferRequest.ERC721(
                        nftTransferDetail.receiver,
                        nftTransferDetail.tokenId,
                        nftTransferDetail.tokenAddress
                    );
                    CreateTransferResponseV1 response = await Passport.ImxTransfer(transferRequest);
                    ShowOutput($"NFT transferred successfully. Transfer ID: {response.transfer_id}");
                }
            }
            catch (Exception ex)
            {
                ShowOutput($"Failed to transfer NFTs: {ex.Message}");
            }
        }
        else
        {
            ShowOutput("Please fill in all required fields for the first NFT transfer.");
        }
    }

    /// <summary>
    /// Constructs a list of NFT transfer details from the input fields.
    /// </summary>
    /// <returns>A list of <see cref="NftTransferDetails"/> objects representing the NFTs to transfer.</returns>
    private List<NftTransferDetails> GetTransferDetails()
    {
        // Initialise the list to store transfer details
        List<NftTransferDetails> details = new List<NftTransferDetails>();

        // Add the first NFT transfer details
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

        // Add the second NFT transfer details if provided
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


    /// <summary>
    /// Navigates back to the authenticated scene.
    /// </summary>
    public void Cancel()
    {
        SceneManager.LoadScene("AuthenticatedScene");
    }

    /// <summary>
    /// Prints the specified <code>message</code> to the output box.
    /// </summary>
    /// <param name="message">The message to print</param>
    private void ShowOutput(string message)
    {
        if (Output != null)
        {
            Output.text = message;
        }
    }
}
