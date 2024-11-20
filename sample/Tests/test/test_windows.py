import time

from alttester import *

from test import TestConfig, UnityTest
from test_windows_helpers import login, open_sample_app, launch_chrome, bring_sample_app_to_foreground, stop_chrome, stop_sample_app

class WindowsTest(UnityTest):

    altdriver = None

    @classmethod
    def setUpClass(cls):
        open_sample_app()
        cls.altdriver = AltDriver()

    @classmethod
    def tearDownClass(cls):
        cls.altdriver.stop()
        stop_sample_app()        

    def test_1_device_code_login(self):
        launch_chrome()

        bring_sample_app_to_foreground()

        self.altdriver.wait_for_current_scene_to_be("UnauthenticatedScene")

        # Login
        print("Logging in...")
        self.altdriver.wait_for_object(By.NAME, "LoginBtn").tap()
        login()
        bring_sample_app_to_foreground()

        # Wait for authenticated screen
        self.altdriver.wait_for_current_scene_to_be("AuthenticatedScene")
        print("Logged in")

        stop_chrome()

    def test_2_other_functions(self):
        self.test_0_other_functions()

    def test_3_passport_functions(self):
        self.test_1_passport_functions()

    def test_4_imx_functions(self):
        self.test_2_imx_functions()

    def test_5_zkevm_functions(self):
        self.test_3_zkevm_functions()

    def test_6_relogin(self):
        # Close and reopen app
        stop_sample_app()
        open_sample_app()

        # Restart AltTester
        self.altdriver.stop()
        self.altdriver = AltDriver()
        time.sleep(5)

        # Relogin
        print("Re-logging in...")
        self.altdriver.wait_for_object(By.NAME, "ReloginBtn").tap()

        # Wait for authenticated screen
        self.altdriver.wait_for_current_scene_to_be("AuthenticatedScene")
        print("Re-logged in")

        # Get access token
        self.altdriver.find_object(By.NAME, "GetAccessTokenBtn").tap()
        output = self.altdriver.find_object(By.NAME, "Output")
        self.assertTrue(len(output.get_text()) > 50)

        # Click Connect to IMX button
        self.altdriver.find_object(By.NAME, "ConnectBtn").tap()
        self.assertEqual("Connected to IMX", output.get_text())

        self.altdriver.stop()

    def test_7_reconnect_device_code_connect_imx(self):
        # Close and reopen app
        stop_sample_app()
        open_sample_app()

        # Restart AltTester
        self.altdriver.stop()
        self.altdriver = AltDriver()
        time.sleep(5)

        # Reconnect
        print("Reconnecting...")
        self.altdriver.wait_for_object(By.NAME, "ReconnectBtn").tap()

        # Wait for authenticated screen
        self.altdriver.wait_for_current_scene_to_be("AuthenticatedScene")
        print("Reconnected")

        # Get access token
        self.altdriver.find_object(By.NAME, "GetAccessTokenBtn").tap()
        output = self.altdriver.find_object(By.NAME, "Output")
        self.assertTrue(len(output.get_text()) > 50)

        # Get address without having to click Connect to IMX button
        self.altdriver.find_object(By.NAME, "GetAddressBtn").tap()
        self.assertEqual(TestConfig.WALLET_ADDRESS, output.get_text())

        # Logout
        print("Logging out...")
        launch_chrome()
        bring_sample_app_to_foreground()
        self.altdriver.find_object(By.NAME, "LogoutBtn").tap()
        time.sleep(5)
        bring_sample_app_to_foreground()

        # Wait for authenticated screen
        self.altdriver.wait_for_current_scene_to_be("UnauthenticatedScene")
        stop_chrome()
        print("Logged out")

        # Connect IMX
        print("Logging in and connecting to IMX...")
        launch_chrome()
        bring_sample_app_to_foreground()
        self.altdriver.wait_for_object(By.NAME, "ConnectBtn").tap()
        login()
        bring_sample_app_to_foreground()
        
        # Wait for authenticated screen
        self.altdriver.wait_for_current_scene_to_be("AuthenticatedScene")
        print("Logged in and connected to IMX")
        stop_chrome()

        # Get access token
        self.altdriver.find_object(By.NAME, "GetAccessTokenBtn").tap()
        output = self.altdriver.find_object(By.NAME, "Output")
        self.assertTrue(len(output.get_text()) > 50)

        # Get address without having to click Connect to IMX button
        self.altdriver.find_object(By.NAME, "GetAddressBtn").tap()
        self.assertEqual(TestConfig.WALLET_ADDRESS, output.get_text())

        # Logout
        launch_chrome()
        bring_sample_app_to_foreground()
        print("Logging out...")
        self.altdriver.find_object(By.NAME, "LogoutBtn").tap()
        time.sleep(5)
        bring_sample_app_to_foreground()
        
        # Wait for authenticated screen
        self.altdriver.wait_for_current_scene_to_be("UnauthenticatedScene")
        stop_chrome()
        print("Logged out")
