import time

from alttester import *

from test import TestConfig, UnityTest
from test_windows_helpers import login, open_sample_app, launch_browser, bring_sample_app_to_foreground, stop_browser, stop_sample_app

class WindowsTest(UnityTest):

    @classmethod
    def setUpClass(cls):
        open_sample_app()
        time.sleep(5) # Give time for the app to open
        super().setUpClass()

    @classmethod
    def tearDownClass(cls):
        super().tearDownClass()
        stop_sample_app()

    def restart_app_and_altdriver(self):
        self.stop_altdriver()
        stop_sample_app()
        open_sample_app()
        time.sleep(5) # Give time for the app to open
        self.start_altdriver()

    def select_auth_type(self, use_pkce: bool):
        auth_type = "PKCE" if use_pkce else "DeviceCodeAuth"
        self.get_altdriver().find_object(By.NAME, auth_type).tap()

    def login(self, use_pkce: bool):
        self.select_auth_type(use_pkce)

        # Wait for unauthenticated screen
        self.get_altdriver().wait_for_current_scene_to_be("UnauthenticatedScene")

        for attempt in range(2):
            try:
                # Check app state
                login_button = self.get_altdriver().find_object(By.NAME, "LoginBtn")
                print("Found login button, app is in the correct state")

                # Login
                print("Logging in...")
                launch_browser()
                bring_sample_app_to_foreground()
                login_button.tap()
                login(use_pkce)
                bring_sample_app_to_foreground()

                # Wait for authenticated screen
                self.get_altdriver().wait_for_current_scene_to_be("AuthenticatedScene")
                stop_browser()
                print("Logged in")
                return
            except Exception as err:
                stop_browser()

                if attempt == 0:
                    # Reset app

                    # Relogin (optional: only if the button is present)
                    print("Try reset the app and log out once...")
                    try:
                        self.get_altdriver().wait_for_object(By.NAME, "ReloginBtn").tap()
                    except Exception as e:
                        print("ReloginBtn not found, skipping relogin step. User may already be in AuthenticatedScene.")

                    # Wait for authenticated screen
                    self.get_altdriver().wait_for_current_scene_to_be("AuthenticatedScene")
                    print("Re-logged in")

                    # Logout
                    print("Logging out...")
                    launch_browser()
                    bring_sample_app_to_foreground()
                    self.get_altdriver().find_object(By.NAME, "LogoutBtn").tap()
                    time.sleep(5)
                    bring_sample_app_to_foreground()

                    # Wait for unauthenticated screen
                    self.get_altdriver().wait_for_current_scene_to_be("UnauthenticatedScene")
                    stop_browser()
                    print("Logged out and successfully reset app")

                    time.sleep(5)
                else:
                    raise SystemExit(f"Failed to reset app {err}")

    def test_1a_pkce_login(self):
        self.login(True)

    def test_1b_device_code_login(self):
        self.restart_app_and_altdriver()
        self.login(False)

    def test_2_other_functions(self):
        self.test_0_other_functions()

    def test_3_passport_functions(self):
        self.test_1_passport_functions()

    def test_4_imx_functions(self):
        self.test_2_imx_functions()

    def test_5_zkevm_functions(self):
        self.test_3_zkevm_functions()

    def test_6_relogin(self):
        self.restart_app_and_altdriver()

        # Select use device code auth
        self.select_auth_type(use_pkce=False)

        # Relogin
        print("Re-logging in...")
        self.get_altdriver().wait_for_object(By.NAME, "ReloginBtn").tap()

        # Wait for authenticated screen
        self.get_altdriver().wait_for_current_scene_to_be("AuthenticatedScene")
        print("Re-logged in")

        # Get access token
        self.get_altdriver().find_object(By.NAME, "GetAccessTokenBtn").tap()
        output = self.get_altdriver().find_object(By.NAME, "Output")
        self.assertTrue(len(output.get_text()) > 50)

        # Click Connect to IMX button
        self.get_altdriver().find_object(By.NAME, "ConnectBtn").tap()
        time.sleep(5)
        self.assertEqual("Connected to IMX", output.get_text())

    def test_7_reconnect_device_code_connect_imx(self):
        self.restart_app_and_altdriver()

        use_pkce = False
        self.select_auth_type(use_pkce)

        # Reconnect
        print("Reconnecting...")
        self.get_altdriver().wait_for_object(By.NAME, "ReconnectBtn").tap()

        # Wait for authenticated screen
        self.get_altdriver().wait_for_current_scene_to_be("AuthenticatedScene")
        print("Reconnected")

        # Get access token
        self.get_altdriver().find_object(By.NAME, "GetAccessTokenBtn").tap()
        output = self.get_altdriver().find_object(By.NAME, "Output")
        self.assertTrue(len(output.get_text()) > 50)

        # Get address without having to click Connect to IMX button
        self.get_altdriver().find_object(By.NAME, "GetAddressBtn").tap()
        self.assertEqual(TestConfig.WALLET_ADDRESS, output.get_text())

        # Logout
        print("Logging out...")
        launch_browser()
        bring_sample_app_to_foreground()
        self.get_altdriver().find_object(By.NAME, "LogoutBtn").tap()
        time.sleep(5)
        bring_sample_app_to_foreground()

        # Wait for authenticated screen
        self.get_altdriver().wait_for_current_scene_to_be("UnauthenticatedScene")
        stop_browser()
        print("Logged out")

        # Connect IMX
        print("Logging in and connecting to IMX...")
        launch_browser()
        bring_sample_app_to_foreground()
        self.get_altdriver().wait_for_object(By.NAME, "ConnectBtn").tap()
        login(use_pkce)
        bring_sample_app_to_foreground()

        # Wait for authenticated screen
        self.get_altdriver().wait_for_current_scene_to_be("AuthenticatedScene")
        print("Logged in and connected to IMX")
        stop_browser()

        # Get access token
        self.get_altdriver().find_object(By.NAME, "GetAccessTokenBtn").tap()
        output = self.get_altdriver().find_object(By.NAME, "Output")
        self.assertTrue(len(output.get_text()) > 50)

        # Get address without having to click Connect to IMX button
        self.get_altdriver().find_object(By.NAME, "GetAddressBtn").tap()
        self.assertEqual(TestConfig.WALLET_ADDRESS, output.get_text())

        # Logout
        launch_browser()
        bring_sample_app_to_foreground()
        print("Logging out...")
        self.get_altdriver().find_object(By.NAME, "LogoutBtn").tap()
        time.sleep(5)
        bring_sample_app_to_foreground()

        # Wait for authenticated screen
        self.get_altdriver().wait_for_current_scene_to_be("UnauthenticatedScene")
        stop_browser()
        print("Logged out")
