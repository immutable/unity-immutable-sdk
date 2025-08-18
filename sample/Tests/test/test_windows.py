import time

from alttester import *

from test import TestConfig, UnityTest
from test_windows_helpers import login, open_sample_app, launch_browser, bring_sample_app_to_foreground, stop_browser, stop_sample_app, logout_with_controlled_browser

class WindowsTest(UnityTest):

    @classmethod
    def setUpClass(cls):
        open_sample_app()
        time.sleep(5) # Give time for the app to open
        # Initialize AltDriver with longer timeout for flaky CI environment
        cls.altdriver = AltDriver(timeout=120)  # 120 seconds instead of default 20

    @classmethod
    def tearDownClass(cls):
        super().tearDownClass()
        stop_sample_app()

    def restart_app_and_altdriver(self):
        self.stop_altdriver()
        stop_sample_app()
        open_sample_app()
        time.sleep(5) # Give time for the app to open
        # Use same timeout as setUpClass
        self.__class__.altdriver = AltDriver(timeout=120)

    def login(self):
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
                login()
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

    def test_1_login(self):
        print("=" * 60)
        print("STARTING TEST: test_1_login")
        print("=" * 60)
        self.login()
        print("COMPLETED TEST: test_1_login")
        print("=" * 60)

    def test_2_other_functions(self):
        print("=" * 60)
        print("STARTING TEST: test_2_other_functions")
        print("=" * 60)
        self.test_0_other_functions()
        print("COMPLETED TEST: test_2_other_functions")
        print("=" * 60)

    def test_3_passport_functions(self):
        print("=" * 60)
        print("STARTING TEST: test_3_passport_functions")
        print("=" * 60)
        self.test_1_passport_functions()
        print("COMPLETED TEST: test_3_passport_functions")
        print("=" * 60)

    def test_4_imx_functions(self):
        print("=" * 60)
        print("STARTING TEST: test_4_imx_functions")
        print("=" * 60)
        self.test_2_imx_functions()
        print("COMPLETED TEST: test_4_imx_functions")
        print("=" * 60)

    def test_5_zkevm_functions(self):
        print("=" * 60)
        print("STARTING TEST: test_5_zkevm_functions")
        print("=" * 60)
        self.test_3_zkevm_functions()
        print("COMPLETED TEST: test_5_zkevm_functions")
        print("=" * 60)

    def test_6_relogin(self):
        print("=" * 60)
        print("STARTING TEST: test_6_relogin")
        print("=" * 60)
        self.restart_app_and_altdriver()

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
        
        print("COMPLETED TEST: test_6_relogin")
        print("=" * 60)

    def test_7_reconnect_connect_imx(self):
        print("=" * 60)
        print("STARTING TEST: test_7_reconnect_connect_imx")
        print("=" * 60)
        self.restart_app_and_altdriver()

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
        
        # Use controlled browser logout instead of waiting for scene change
        logout_with_controlled_browser()
        
        # Give Unity time to process the logout callback
        time.sleep(5)
        bring_sample_app_to_foreground()

        # Wait for authenticated screen
        self.get_altdriver().wait_for_current_scene_to_be("UnauthenticatedScene")
        
        stop_browser()
        print("Logged out")

        print("COMPLETED TEST: test_7_reconnect_connect_imx")
        print("=" * 60)

    def test_8_connect_imx(self):
        print("=" * 60)
        print("STARTING TEST: test_8_connect_imx")
        print("=" * 60)
        # Ensure clean state regardless of previous tests
        self.restart_app_and_altdriver()
    
        # Wait for initial scene
        self.get_altdriver().wait_for_current_scene_to_be("UnauthenticatedScene")

        # Connect IMX
        print("Logging in and connecting to IMX...")
        launch_browser()
        bring_sample_app_to_foreground()
        self.get_altdriver().wait_for_object(By.NAME, "ConnectBtn").tap()
        login()
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
        logout_with_controlled_browser()
        time.sleep(5)
        bring_sample_app_to_foreground()

        # Wait for authenticated screen
        self.get_altdriver().wait_for_current_scene_to_be("UnauthenticatedScene")
        stop_browser()
        print("Logged out")
        print("COMPLETED TEST: test_8_connect_imx")
        print("=" * 60)
