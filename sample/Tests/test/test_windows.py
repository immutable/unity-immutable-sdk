"""
Unity Passport Windows UI Tests

For test setup and configuration requirements (especially Passport SDK log level),
see: sample/Tests/README.md

These tests require proper authentication URL logging to work correctly.
"""

import time
import unittest

from alttester import *

from test import TestConfig, UnityTest
from test_windows_helpers import login, open_sample_app, launch_browser, bring_sample_app_to_foreground, stop_browser, stop_sample_app, logout_with_controlled_browser, get_product_name

class WindowsTest(UnityTest):

    @classmethod
    def setUpClass(cls):
        # Clear cached login state at the start of the test suite
        open_sample_app(clear_data=True)
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
        open_sample_app()  # Normal restart without clearing data
        time.sleep(5) # Give time for the app to open
        # Use same timeout as setUpClass
        self.__class__.altdriver = AltDriver(timeout=120)

    def login(self):
        """
        Smart login method that handles different app states:
        - UnauthenticatedScene: Proceed with normal login
        - AuthenticatedScene: Logout first, then login
        - Other scenes: Wait for proper state
        """
        print("=== SMART LOGIN: Checking app state ===")
        
        # Check what scene we're starting in
        try:
            current_scene = self.get_altdriver().get_current_scene()
            print(f"Current scene: {current_scene}")
        except Exception as e:
            print(f"Could not get current scene: {e}")
            raise SystemExit("Failed to determine app state")
        
        # Handle different starting states
        if current_scene == "UnauthenticatedScene":
            print("[OK] App is already unauthenticated - proceeding with login")
            self._perform_login()
            
        elif current_scene == "AuthenticatedScene":
            print("[WARNING] App is already authenticated - need to logout first")
            self._logout_and_login()
            
        else:
            print(f"[ERROR] Unexpected scene: {current_scene}")
            # Try to wait for a known state
            print("Waiting for app to reach a known state...")
            for wait_attempt in range(3):
                try:
                    current_scene = self.get_altdriver().get_current_scene()
                    if current_scene in ["UnauthenticatedScene", "AuthenticatedScene"]:
                        print(f"App reached known state: {current_scene}")
                        return self.login()  # Recursive call with known state
                    time.sleep(5)
                except Exception as e:
                    print(f"Wait attempt {wait_attempt + 1} failed: {e}")
                    
            raise SystemExit(f"App stuck in unknown scene: {current_scene}")

    def _perform_login(self):
        """Perform normal login flow when app is in UnauthenticatedScene"""
        try:
            # Debug: Check what scene we're actually in and what objects exist
            try:
                current_scene = self.get_altdriver().get_current_scene()
                print(f"DEBUG: _perform_login - current scene: {current_scene}")
            except Exception as e:
                print(f"DEBUG: Could not get current scene: {e}")
            
            # Wait a moment for UI to stabilize and check if app is still running
            time.sleep(3)
            
            # Debug: Check if we can still communicate with the app
            try:
                connection_test = self.get_altdriver().get_current_scene()
                print(f"DEBUG: App still responsive, scene: {connection_test}")
            except Exception as e:
                print(f"DEBUG: App may have crashed or lost connection: {e}")
                raise SystemExit("App connection lost during login attempt")
            
            # Debug: Try to find any buttons to see what's available
            try:
                all_objects = self.get_altdriver().get_all_elements()
                button_objects = [obj for obj in all_objects if 'btn' in obj.name.lower() or 'button' in obj.name.lower()]
                print(f"DEBUG: Found button-like objects: {[obj.name for obj in button_objects]}")
            except Exception as e:
                print(f"DEBUG: Could not get all objects: {e}")
            
            # Check for login button
            login_button = self.get_altdriver().find_object(By.NAME, "LoginBtn")
            print("Found login button - performing login")

            # Login
            launch_browser()
            bring_sample_app_to_foreground()
            login_button.tap()
            login()
            bring_sample_app_to_foreground()

            # Wait for authenticated screen
            # Default AltTester timeout for this command is ~20s; CI often needs longer,
            # especially when the browser auto-handles the deep-link without a dialog.
            self.get_altdriver().wait_for_current_scene_to_be("AuthenticatedScene", timeout=90)
            stop_browser()
            print("[SUCCESS] Login successful")
            
        except Exception as err:
            # Dump Player.log tail to help diagnose why the deep-link callback
            # wasn't processed (or why Unity failed after receiving it).
            try:
                import os
                product_name = os.getenv("UNITY_APP_NAME", get_product_name())
                log_path = os.path.join(
                    "C:\\Users\\WindowsBuildsdkServi\\AppData\\LocalLow\\Immutable",
                    product_name,
                    "Player.log",
                )
                print(f"Attempting to dump Unity Player.log tail: {log_path}")
                if os.path.exists(log_path):
                    with open(log_path, "r", encoding="utf-8", errors="ignore") as f:
                        lines = f.read().splitlines()
                    # The tail is often dominated by AltTester noise. Print:
                    # 1) last lines, and 2) last relevant lines (Passport/Immutable/URLs/errors).
                    tail = lines[-200:] if len(lines) > 200 else lines
                    print("----- Player.log (tail) -----")
                    for line in tail:
                        print(line)
                    print("----- end Player.log (tail) -----")

                    needles = (
                        "immutable",
                        "passport",
                        "launchauthurl",
                        "passport_auth_url",
                        "immutablerunner",
                        "error",
                        "exception",
                        "gb:",
                    )
                    relevant = [ln for ln in lines if any(n in ln.lower() for n in needles)]
                    relevant_tail = relevant[-200:] if len(relevant) > 200 else relevant
                    print("----- Player.log (relevant tail) -----")
                    for line in relevant_tail:
                        print(line)
                    print("----- end Player.log (relevant tail) -----")
                else:
                    print("Player.log not found.")
            except Exception as e:
                print(f"Failed to dump Player.log: {e}")

            stop_browser()
            raise SystemExit(f"Login failed: {err}")

    def _logout_and_login(self):
        """Handle logout and then login when app starts authenticated"""
        print("Attempting logout to reset to unauthenticated state...")
        
        try:
            # Use our improved logout method
            print("Using controlled browser logout...")
            logout_with_controlled_browser()
            
            # Wait for unauthenticated state
            print("Waiting for UnauthenticatedScene after logout...")
            self.get_altdriver().wait_for_current_scene_to_be("UnauthenticatedScene", timeout=30)
            print("[SUCCESS] Successfully logged out")
            
            # Now perform normal login
            self._perform_login()
            
        except Exception as logout_err:
            print(f"Controlled logout failed: {logout_err}")
            print("Trying fallback logout method...")
            
            try:
                # Fallback: Direct logout button approach
                launch_browser()
                bring_sample_app_to_foreground()
                logout_button = self.get_altdriver().find_object(By.NAME, "LogoutBtn")
                logout_button.tap()
                time.sleep(10)  # Give more time for logout
                bring_sample_app_to_foreground()
                
                # Wait for unauthenticated screen
                self.get_altdriver().wait_for_current_scene_to_be("UnauthenticatedScene", timeout=30)
                stop_browser()
                print("[SUCCESS] Fallback logout successful")
                
                # Now perform normal login
                self._perform_login()
                
            except Exception as fallback_err:
                stop_browser()
                print(f"[ERROR] Both logout methods failed:")
                print(f"  - Controlled logout: {logout_err}")
                print(f"  - Fallback logout: {fallback_err}")
                raise SystemExit("Could not logout to reset app state")

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

    @unittest.skip("IMX (StarkEx) scenarios deprecated; removing from E2E suite")
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

    @unittest.skip("IMX (StarkEx) scenarios deprecated; removing from E2E suite")
    def test_6_relogin(self):
        print("=" * 60)
        print("STARTING TEST: test_6_relogin")
        print("=" * 60)
        self.restart_app_and_altdriver()

        # Relogin
        print("Re-logging in...")
        self.get_altdriver().wait_for_object(By.NAME, "ReloginBtn").tap()

        # Wait for authenticated screen
        self.get_altdriver().wait_for_current_scene_to_be("AuthenticatedScene", timeout=90)
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

    @unittest.skip("IMX (StarkEx) scenarios deprecated; removing from E2E suite")
    def test_7_reconnect_connect_imx(self):
        print("=" * 60)
        print("STARTING TEST: test_7_reconnect_connect_imx")
        print("=" * 60)
        self.restart_app_and_altdriver()

        # Reconnect
        print("Reconnecting...")
        self.get_altdriver().wait_for_object(By.NAME, "ReconnectBtn").tap()

        # Wait for authenticated screen
        self.get_altdriver().wait_for_current_scene_to_be("AuthenticatedScene", timeout=90)
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

    @unittest.skip("IMX (StarkEx) scenarios deprecated; removing from E2E suite")
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
        self.get_altdriver().wait_for_current_scene_to_be("AuthenticatedScene", timeout=90)
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
