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
    IOS_BUNDLE_ID = "com.immutable.Immutable-Sample-GameSDK"

class UnityTest(unittest.TestCase):

    altdriver = None

    @classmethod
    def setUpClass(cls):
        cls.altdriver = AltDriver()

    @classmethod
    def tearDownClass(cls):
        if cls.altdriver:
            cls.altdriver.stop()

    def get_altdriver(self):
        return self.__class__.altdriver

    def start_altdriver(self):
        self.__class__.altdriver = AltDriver()

    def stop_altdriver(self):
        if self.__class__.altdriver:
            self.__class__.altdriver.stop()

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