import time
import unittest
import requests

from alttester import *

from test import TestConfig

class WindowsTest(unittest.TestCase):

    altdriver = None

    @classmethod
    def setUpClass(cls):
        cls.altdriver = AltDriver()

    @classmethod
    def tearDownClass(cls):
        cls.altdriver.stop()

    def test_1_device_code_login(self):
        # Login
        loginBtn = self.altdriver.wait_for_object(By.NAME, "LoginBtn")
        loginBtn.tap()

        # Wait for authenticated screen
        self.altdriver.wait_for_current_scene_to_be("AuthenticatedScene")

    def test_2_device_code_connect_imx(self):
        # Connect IMX
        connectBtn = self.altdriver.wait_for_object(By.NAME, "ConnectBtn")
        connectBtn.tap()

        # Wait for authenticated screen
        self.altdriver.wait_for_current_scene_to_be("AuthenticatedScene")

        # Get access token
        self.altdriver.find_object(By.NAME, "GetAccessTokenBtn").tap()
        output = self.altdriver.find_object(By.NAME, "Output")
        self.assertTrue(len(output.get_text()) > 50)

        # Get address without having to click Connect to IMX button
        self.altdriver.find_object(By.NAME, "GetAddressBtn").tap()
        self.assertEqual(TestConfig.WALLET_ADDRESS, output.get_text())

    def test_3_device_code_relogin(self):
        # Relogin
        reloginBtn = self.altdriver.wait_for_object(By.NAME, "ReloginBtn")
        reloginBtn.tap()

        # Wait for authenticated screen
        self.altdriver.wait_for_current_scene_to_be("AuthenticatedScene")

        # Get access token
        self.altdriver.find_object(By.NAME, "GetAccessTokenBtn").tap()
        output = self.altdriver.find_object(By.NAME, "Output")
        self.assertTrue(len(output.get_text()) > 50)

        # Click Connect to IMX button
        self.altdriver.find_object(By.NAME, "ConnectBtn").tap()
        self.assertEqual("Connected to IMX", output.get_text())

    def test_4_device_code_reconnect(self):
        # Relogin
        reloginBtn = self.altdriver.wait_for_object(By.NAME, "ReconnectBtn")
        reloginBtn.tap()

        # Wait for authenticated screen
        self.altdriver.wait_for_current_scene_to_be("AuthenticatedScene")

        # Get access token
        self.altdriver.find_object(By.NAME, "GetAccessTokenBtn").tap()
        output = self.altdriver.find_object(By.NAME, "Output")
        self.assertTrue(len(output.get_text()) > 50)

        # Get address without having to click Connect to IMX button
        self.altdriver.find_object(By.NAME, "GetAddressBtn").tap()
        self.assertEqual(TestConfig.WALLET_ADDRESS, output.get_text())
