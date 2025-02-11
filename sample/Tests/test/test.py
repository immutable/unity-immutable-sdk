import time
import unittest
import requests
import re
import pytest

from alttester import *

class TestConfig:
    EMAIL = "unity-sdk@mailslurp.net"
    PASSPORT_ID="email|67480492219c150aceeb1f37"
    WALLET_ADDRESS = "0x547044ea95f03651139081241c99ffedbefdc5e8"
    ANDROID_PACKAGE = "com.immutable.ImmutableSample"
    
class UnityTest(unittest.TestCase):

    altdriver = None

    @classmethod
    def setUpClass(cls):
        cls.altdriver = AltDriver()

    @classmethod
    def tearDownClass(cls):
        cls.altdriver.stop()

    @pytest.mark.skip(reason="Base test should not be executed directly")
    def test_0_other_functions(self):
        # Show set call timeout scene
        self.altdriver.find_object(By.NAME, "CallTimeout").tap()
        self.altdriver.wait_for_current_scene_to_be("SetCallTimeout")

        milliseconds = self.altdriver.wait_for_object(By.NAME, "MsInput")
        milliseconds.set_text("600000")
        self.altdriver.find_object(By.NAME, "SetButton").tap()
        output = self.altdriver.find_object(By.NAME, "Output")
        text = output.get_text()
        print(f"CallTimeout output: {text}")
        self.assertEqual("Set call timeout to: 600000ms", text)

        # Go back to authenticated scene
        self.altdriver.find_object(By.NAME, "CancelButton").tap()
        self.altdriver.wait_for_current_scene_to_be("AuthenticatedScene")

    @pytest.mark.skip(reason="Base test should not be executed directly")
    def test_1_passport_functions(self):
        output = self.altdriver.find_object(By.NAME, "Output")

        # Get access token
        self.altdriver.find_object(By.NAME, "GetAccessTokenBtn").tap()
        text = output.get_text()
        self.assertTrue(len(text) > 50)

        # Get ID token
        self.altdriver.find_object(By.NAME, "GetIdTokenBtn").tap()
        text = output.get_text()
        self.assertTrue(len(text) > 50)

        # Get email
        self.altdriver.find_object(By.NAME, "GetEmail").tap()
        text = output.get_text()
        print(f"GetEmail output: {text}")
        self.assertEqual(TestConfig.EMAIL, text)

        # Get Passport ID
        self.altdriver.find_object(By.NAME, "GetPassportId").tap()
        text = output.get_text()
        print(f"GetPassportId output: {text}")
        self.assertEqual(TestConfig.PASSPORT_ID, text)

        # Get linked addresses
        self.altdriver.find_object(By.NAME, "GetLinkedAddresses").tap()
        time.sleep(1)
        text = output.get_text()
        print(f"GetLinkedAddresses output: {text}")
        self.assertEqual("No linked addresses", text)

    @pytest.mark.skip(reason="Base test should not be executed directly")
    def test_2_imx_functions(self):
        output = self.altdriver.find_object(By.NAME, "Output")

        # Connect to IMX
        self.altdriver.find_object(By.NAME, "ConnectBtn").tap()
        text = output.get_text()
        print(f"ConnectBtn output: {text}")
        self.assertEqual("Connected to IMX", text)

        # Is registered off-chain
        self.altdriver.wait_for_object(By.NAME, "IsRegisteredOffchainBtn").tap()
        time.sleep(1)
        text = output.get_text()
        print(f"IsRegisteredOffchainBtn output: {text}")
        self.assertEqual("Registered", text)

        # Register off-chain
        # Wait up to 3 times for "Passport account already registered" to appear
        attempts = 0
        while attempts < 3:
           self.altdriver.find_object(By.NAME, "RegisterOffchainBtn").tap()
           text = output.get_text()
           print(f"RegisterOffchainBtn output: {text}")
           self.assertEqual("Registering off-chain...", text)
           time.sleep(20)
           if "Passport account already registered" in output.get_text():
               break
           attempts += 1

        # Assert that the desired text is found after waiting
        self.assertTrue(
           "Passport account already registered" in output.get_text(),
           f"Expected 'Passport account already registered' not found. Actual output: '{output.get_text()}'"
        )

        # Get address
        self.altdriver.find_object(By.NAME, "GetAddressBtn").tap()
        text = output.get_text()
        print(f"GetAddressBtn output: {text}")
        self.assertEqual(TestConfig.WALLET_ADDRESS, text)

        # Show NFT transfer scene
        self.altdriver.find_object(By.NAME, "NftTransferBtn").tap()
        self.altdriver.wait_for_current_scene_to_be("ImxNftTransfer")

        # Get all NFTs the user owns
        collection = "0x3765d19d5bc39b60718e43b4b12b30e87d383181"
        api_url = f"https://api.sandbox.immutable.com/v1/assets?collection={collection}&user={TestConfig.WALLET_ADDRESS}&page_size=3"
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
        time.sleep(30)
        output = self.altdriver.find_object(By.NAME, "Output")
        text = output.get_text()
        print(f"Single transfer output: {text}")
        self.assertTrue(text.startswith("NFT transferred successfully"))

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
        time.sleep(30)
        output = self.altdriver.find_object(By.NAME, "Output")
        text = output.get_text()
        print(f"Batch transfer output: {text}")
        self.assertEqual("Successfully transferred 2 NFTs.", text)

        # Go back to authenticated scene
        self.altdriver.find_object(By.NAME, "CancelButton").tap()
        self.altdriver.wait_for_current_scene_to_be("AuthenticatedScene")

    @pytest.mark.skip(reason="Base test should not be executed directly")
    def test_3_zkevm_functions(self):
        output = self.altdriver.find_object(By.NAME, "Output")

        # Connect to zkEVM
        self.altdriver.find_object(By.NAME, "ConnectEvmBtn").tap()
        text = output.get_text()
        print(f"ConnectEvmBtn output: {text}")
        self.assertEqual("Connected to EVM", text)

        # Initiliase wallet and get address
        self.altdriver.wait_for_object(By.NAME, "RequestAccountsBtn").tap()
        time.sleep(5)
        text = output.get_text()
        print(f"RequestAccountsBtn output: {text}")
        self.assertEqual(TestConfig.WALLET_ADDRESS, text)

        # Show get balance scene
        self.altdriver.find_object(By.NAME, "GetBalanceBtn").tap()
        self.altdriver.wait_for_current_scene_to_be("ZkEvmGetBalance")

        # Get balance of account
        address = self.altdriver.wait_for_object(By.NAME, "AddressInput")
        address.set_text(TestConfig.WALLET_ADDRESS)
        self.altdriver.find_object(By.NAME, "GetBalanceButton").tap()
        time.sleep(2)
        output = self.altdriver.find_object(By.NAME, "Output")
        text = output.get_text()
        print(f"Get balance output: {text}")
        self.assertRegex(text, r"Balance:\nHex: 0x[0-9a-fA-F]+\nDec: \d+")

        # Go back to authenticated scene
        self.altdriver.find_object(By.NAME, "CancelButton").tap()
        self.altdriver.wait_for_current_scene_to_be("AuthenticatedScene")

        # Show send transaction scene
        self.altdriver.find_object(By.NAME, "SendTransactionBtn").tap()
        self.altdriver.wait_for_current_scene_to_be("ZkEvmSendTransaction")
        output = self.altdriver.find_object(By.NAME, "Output")

        # Send transaction with confirmation
        to = self.altdriver.wait_for_object(By.NAME, "ToInput")
        to.set_text("0xb237501b35dfdcad274299236a141425469ab9ba")
        amount = self.altdriver.wait_for_object(By.NAME, "ValueInput")
        amount.set_text("0")
        data = self.altdriver.wait_for_object(By.NAME, "DataInput")
        data.set_text("0x1e957f1e")
        self.altdriver.find_object(By.NAME, "SendButton").tap()
        time.sleep(15)
        text = output.get_text()
        print(f"Send transaction with confirmation output: {text}")
        self.assertTrue(text.startswith("Transaction hash"))
        self.assertTrue(text.endswith("Status: Success"))
        time.sleep(20)

        # Send transaction without confirmation and get transaction receipt
        self.altdriver.wait_for_object(By.NAME, "WithConfirmationToggle").tap()
        self.altdriver.find_object(By.NAME, "SendButton").tap()
        time.sleep(20)
        text = output.get_text()
        print(f"Send transaction without confirmation and get transaction receipt output: {text}")
        self.assertTrue(text.startswith("Transaction hash"))
        self.assertTrue(text.endswith("Status: Success"))
        time.sleep(20)

        # Send transaction without confirmation and don't get transaction receipt
        self.altdriver.wait_for_object(By.NAME, "GetTransactionReceiptToggle").tap()
        self.altdriver.find_object(By.NAME, "SendButton").tap()
        time.sleep(15)
        text = output.get_text()
        print(f"Send transaction without confirmation and don't get transaction receipt output: {text}")
        self.assertTrue(text.startswith("Transaction hash"))

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
        text = output.get_text()
        print(f"Get transaction receipt output: {text}")
        self.assertEqual("Status: Success", text)

        # Go back to authenticated scene
        self.altdriver.find_object(By.NAME, "CancelButton").tap()
        self.altdriver.wait_for_current_scene_to_be("AuthenticatedScene")