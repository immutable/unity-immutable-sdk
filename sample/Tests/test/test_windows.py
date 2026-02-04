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
from test_windows_helpers import login, open_sample_app, launch_browser, bring_sample_app_to_foreground, stop_browser, stop_sample_app, logout_with_controlled_browser

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
            self.get_altdriver().wait_for_current_scene_to_be("AuthenticatedScene")
            stop_browser()
            print("[SUCCESS] Login successful")
            
        except Exception as err:
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

    def test_5_zkevm_functions(self):
        print("=" * 60)
        print("STARTING TEST: test_5_zkevm_functions")
        print("=" * 60)
        self.test_3_zkevm_functions()
        print("COMPLETED TEST: test_5_zkevm_functions")
        print("=" * 60)
