import time
import unittest
import requests
import re

from alttester import *

class UnityTest(unittest.TestCase):

    altdriver = None

    @classmethod
    def setUpClass(cls):
        cls.altdriver = AltDriver()

    @classmethod
    def tearDownClass(cls):
        cls.altdriver.stop()

    def test_1_passport_functions(self):
        output = self.altdriver.find_object(By.NAME, "Output")

        # Get access token
        self.altdriver.find_object(By.NAME, "GetAccessTokenBtn").tap()
        self.assertTrue(len(output.get_text()) > 50)

        # Get ID token
        self.altdriver.find_object(By.NAME, "GetIdTokenBtn").tap()
        self.assertTrue(len(output.get_text()) > 50)

        # Get email
        self.altdriver.find_object(By.NAME, "GetEmail").tap()
        self.assertEqual("imx.game.sdk.demo@gmail.com", output.get_text())

        # Get Passport ID
        self.altdriver.find_object(By.NAME, "GetPassportId").tap()
        self.assertEqual("google-oauth2|112226901974607178559", output.get_text())

        # Get linked addresses
        self.altdriver.find_object(By.NAME, "GetLinkedAddresses").tap()
        time.sleep(1)
        self.assertEqual("No linked addresses", output.get_text())

    def test_2_imx_functions(self):
        output = self.altdriver.find_object(By.NAME, "Output")

        # Connect to IMX
        self.altdriver.find_object(By.NAME, "ConnectBtn").tap()
        self.assertEqual("Connected to IMX", output.get_text())

        # Is registered off-chain
        self.altdriver.wait_for_object(By.NAME, "IsRegisteredOffchainBtn").tap()
        time.sleep(1)
        self.assertEqual("Registered", output.get_text())

        # Register off-chain
        self.altdriver.find_object(By.NAME, "RegisterOffchainBtn").tap()
        self.assertEqual("Registering off-chain...", output.get_text())
        time.sleep(20)
        self.assertTrue("Passport account already registered" in output.get_text())

        # Get address
        self.altdriver.find_object(By.NAME, "GetAddressBtn").tap()
        self.assertEqual("0x9cb14f273de4a8c3d8e9b4c5decbb53519dfa7bd", output.get_text())

        # Show NFT transfer scene
        self.altdriver.find_object(By.NAME, "NftTransferBtn").tap()
        self.altdriver.wait_for_current_scene_to_be("ImxNftTransfer")

        # Get all NFTs the user owns
        collection = "0x3765d19d5bc39b60718e43b4b12b30e87d383181"
        api_url = f"https://api.sandbox.immutable.com/v1/assets?collection={collection}&user=0x9cb14f273de4a8c3d8e9b4c5decbb53519dfa7bd&page_size=3"
        token_ids = []
        try:
            # Make the API request
            response = requests.get(api_url)

            # Raise an exception if the request was unsuccessful
            response.raise_for_status()

            # Parse the JSON response
            data = response.json()

            # Extract the token_ids
            token_ids = [item['token_id'] for item in data['result']]

            # Check that there's enough NFTs to test transfer
            if len(token_ids) < 3:
                raise SystemExit(f"Not enough NFTs to test transfer")

        except requests.exceptions.HTTPError as err:
            raise SystemExit(f"HTTP error occurred: {err}")
        except Exception as err:
            raise SystemExit(f"An error occurred: {err}")

        # Single transfer
        tokenId = self.altdriver.wait_for_object(By.NAME, "TokenId1")
        tokenId.set_text(token_ids[0])
        tokenAddress = self.altdriver.wait_for_object(By.NAME, "TokenAddress1")
        tokenAddress.set_text(collection)
        receiver = self.altdriver.wait_for_object(By.NAME, "Receiver1")
        receiver.set_text("0x0000000000000000000000000000000000000000")
        self.altdriver.find_object(By.NAME, "TransferButton").tap()
        time.sleep(10)
        output = self.altdriver.find_object(By.NAME, "Output")
        self.assertTrue(output.get_text().startswith("NFT transferred successfully"))

        # Batch transfer
        tokenId = self.altdriver.wait_for_object(By.NAME, "TokenId1")
        tokenId.set_text(token_ids[1])
        tokenAddress = self.altdriver.wait_for_object(By.NAME, "TokenAddress1")
        tokenAddress.set_text(collection)
        receiver = self.altdriver.wait_for_object(By.NAME, "Receiver1")
        receiver.set_text("0x0000000000000000000000000000000000000000")
        tokenId = self.altdriver.wait_for_object(By.NAME, "TokenId2")
        tokenId.set_text(token_ids[2])
        tokenAddress = self.altdriver.wait_for_object(By.NAME, "TokenAddress2")
        tokenAddress.set_text(collection)
        receiver = self.altdriver.wait_for_object(By.NAME, "Receiver2")
        receiver.set_text("0x0000000000000000000000000000000000000000")
        self.altdriver.find_object(By.NAME, "TransferButton").tap()
        time.sleep(10)
        output = self.altdriver.find_object(By.NAME, "Output")
        self.assertEqual("Successfully transferred 2 NFTs.", output.get_text())

        # Go back to authenticated scene
        self.altdriver.find_object(By.NAME, "CancelButton").tap()
        self.altdriver.wait_for_current_scene_to_be("AuthenticatedScene")

    def test_3_zkevm_functions(self):
        output = self.altdriver.find_object(By.NAME, "Output")

        # Connect to zkEVM
        self.altdriver.find_object(By.NAME, "ConnectEvmBtn").tap()
        self.assertEqual("Connected to EVM", output.get_text())

        # Initiliase wallet and get address
        self.altdriver.wait_for_object(By.NAME, "RequestAccountsBtn").tap()
        time.sleep(5)
        self.assertEqual("0x9cb14f273de4a8c3d8e9b4c5decbb53519dfa7bd", output.get_text())

        # Show get balance scene
        self.altdriver.find_object(By.NAME, "GetBalanceBtn").tap()
        self.altdriver.wait_for_current_scene_to_be("ZkEvmGetBalance")

        # Get balance of account
        address = self.altdriver.wait_for_object(By.NAME, "AddressInput")
        address.set_text("0x9cb14f273de4a8c3d8e9b4c5decbb53519dfa7bd")
        self.altdriver.find_object(By.NAME, "GetBalanceButton").tap()
        time.sleep(2)
        output = self.altdriver.find_object(By.NAME, "Output")
        self.assertEqual("Balance:\nHex: 0x0\nDec: 0", output.get_text())

        # Go back to authenticated scene
        self.altdriver.find_object(By.NAME, "CancelButton").tap()
        self.altdriver.wait_for_current_scene_to_be("AuthenticatedScene")

        # Show send transaction scene
        self.altdriver.find_object(By.NAME, "SendTransactionBtn").tap()
        self.altdriver.wait_for_current_scene_to_be("ZkEvmSendTransaction")
        output = self.altdriver.find_object(By.NAME, "Output")

        # Send transaction with confirmation
        to = self.altdriver.wait_for_object(By.NAME, "ToInput")
        to.set_text("0x912cd5f1cd67F1143b7a5796fd9e5063D755DAbe")
        amount = self.altdriver.wait_for_object(By.NAME, "ValueInput")
        amount.set_text("0")
        data = self.altdriver.wait_for_object(By.NAME, "DataInput")
        data.set_text("0x1e957f1e")
        self.altdriver.find_object(By.NAME, "SendButton").tap()
        time.sleep(15)
        self.assertTrue(output.get_text().startswith("Transaction hash"))
        self.assertTrue(output.get_text().endswith("Status: Success"))

        # Send transaction without confirmation and get transaction receipt
        self.altdriver.wait_for_object(By.NAME, "WithConfirmationToggle").tap()
        self.altdriver.find_object(By.NAME, "SendButton").tap()
        time.sleep(20)
        self.assertTrue(output.get_text().startswith("Transaction hash"))
        self.assertTrue(output.get_text().endswith("Status: Success"))

        # Send transaction without confirmation and don't get transaction receipt
        self.altdriver.wait_for_object(By.NAME, "GetTransactionReceiptToggle").tap()
        self.altdriver.find_object(By.NAME, "SendButton").tap()
        time.sleep(15)
        self.assertTrue(output.get_text().startswith("Transaction hash"))

        # Grab the transaction hash
        match = re.search(r"0x[0-9a-fA-F]+", output.get_text())
        transactionHash = ""
        if match:
            transactionHash = match.group()
        else:
            raise SystemExit(f"Could not find transaction hash")
        
        # Go back to authenticated scene
        self.altdriver.find_object(By.NAME, "CancelButton").tap()
        self.altdriver.wait_for_current_scene_to_be("AuthenticatedScene")

        # Show get transaction receipt scene
        self.altdriver.find_object(By.NAME, "GetTransactionReceiptBtn").tap()
        self.altdriver.wait_for_current_scene_to_be("ZkEvmGetTransactionReceipt")

        # Get transaction receipt
        hash = self.altdriver.wait_for_object(By.NAME, "HashInput")
        hash.set_text(transactionHash)
        self.altdriver.find_object(By.NAME, "GetReceiptButton").tap()
        time.sleep(2)
        output = self.altdriver.find_object(By.NAME, "Output")
        self.assertEqual("Status: Success", output.get_text())

        # Go back to authenticated scene
        self.altdriver.find_object(By.NAME, "CancelButton").tap()
        self.altdriver.wait_for_current_scene_to_be("AuthenticatedScene")