import sys
import time
import os
import subprocess
from pathlib import Path

from selenium import webdriver
from selenium.webdriver.chrome.service import Service
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.common.by import By as SeleniumBy
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.common.keys import Keys

from alttester import *

from test import TestConfig, UnityTest
from test_mac_helpers import open_sample_app, bring_sample_app_to_foreground, stop_sample_app

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / 'src'))
from fetch_otp import fetch_code

class MacTest(UnityTest):

    altdriver = None

    @classmethod
    def setUpClass(cls):
        open_sample_app()
        cls.altdriver = AltDriver()
        cls.stop_browser()

    @classmethod
    def tearDownClass(cls):
        stop_sample_app()
        cls.altdriver.stop()
        cls.stop_browser()

    @classmethod
    def launch_browser(cls):
        print("Starting Browser...")
        browser_paths = [
            "/Applications/Brave Browser.app/Contents/MacOS/Brave Browser"
        ]

        browser_path = None
        for path in browser_paths:
            if os.path.exists(path):
                browser_path = path
                break

        if not browser_path:
            print("Brave executable not found.")
            exit(1)

        subprocess.Popen([
            browser_path,
            "--remote-debugging-port=9222"
        ])

        time.sleep(5)

    @classmethod
    def stop_browser(cls):
        print("Stopping Brave...")
        try:
            # First try graceful shutdown using AppleScript
            subprocess.run([
                "osascript", "-e", 
                'tell application "Brave Browser" to quit'
            ], check=False, capture_output=True)
            time.sleep(2)
            
            # Check if still running, then force kill
            result = subprocess.run(["pgrep", "-f", "Brave Browser"], 
                                  capture_output=True, text=True)
            if result.returncode == 0:
                # Still running, force kill
                subprocess.run(["pkill", "-f", "Brave Browser"], 
                             check=False, capture_output=True)
            
            print("All Brave processes have been closed.")
        except Exception as e:
            print("Brave might not be running.")
        
        time.sleep(3)
        print("Stopped Brave")

    @classmethod
    def login(cls):
        print("Connect to Chrome")
        # Set up Chrome options to connect to the existing Chrome instance
        chrome_options = Options()
        chrome_options.add_experimental_option("debuggerAddress", "localhost:9222")
        # Connect to the existing Chrome instance
        cls.seleniumdriver = webdriver.Chrome(options=chrome_options)

        print("Open a window on Chrome")

        wait = WebDriverWait(cls.seleniumdriver, 60)

        # Wait for email input and enter email
        email_field = WebDriverWait(cls.seleniumdriver, 60).until(EC.presence_of_element_located((SeleniumBy.ID, ':r1:')))
        print("Entering email...")
        email_field.send_keys(TestConfig.EMAIL)
        email_field.send_keys(Keys.RETURN)

        # Wait for OTP
        print("Waiting for OTP...")
        time.sleep(10)
        print("Fetching OTP from MailSlurp...")
        code = fetch_code()
        
        if not code:
            cls.seleniumdriver.quit()
            raise AssertionError("Failed to fetch OTP from MailSlurp")
        
        print(f"Successfully fetched OTP: {code}")

        # Find OTP input and enter the code
        otp_field = WebDriverWait(cls.seleniumdriver, 60).until(EC.presence_of_element_located((SeleniumBy.CSS_SELECTOR, 'input[data-testid="passwordless_passcode__TextInput--0__input"]')))
        print("Entering OTP...")
        otp_field.send_keys(code)

        time.sleep(5)

        cls.seleniumdriver.quit()

    @classmethod
    def logout(cls):
        print("Logging out...")
        cls.launch_browser()
        bring_sample_app_to_foreground()
        cls.altdriver.find_object(By.NAME, "LogoutBtn").tap()
        time.sleep(5)
        cls.altdriver.wait_for_current_scene_to_be("UnauthenticatedScene")
        time.sleep(2)
        cls.stop_browser()
        print("Logged out")

    def test_1_login(self):
        # Wait for unauthenticated screen
        self.altdriver.wait_for_current_scene_to_be("UnauthenticatedScene")

        for attempt in range(2):
            try:
                # Check app state
                login_button = self.altdriver.find_object(By.NAME, "LoginBtn")
                print("Found login button, app is in the correct state")

                # Login
                print("Logging in...")
                self.launch_browser()
                bring_sample_app_to_foreground()
                login_button.tap()
                self.login()

                # Wait for authenticated screen
                self.altdriver.wait_for_current_scene_to_be("AuthenticatedScene")
                print("Logged in")
                
                self.stop_browser()
                return
            except Exception as err:
                if attempt == 0:
                    # Reset app

                    # Relogin
                    print("Try reset the app and log out once...")
                    self.altdriver.wait_for_object(By.NAME, "ReloginBtn").tap()

                    # Wait for authenticated screen
                    self.altdriver.wait_for_current_scene_to_be("AuthenticatedScene")
                    print("Re-logged in")

                    # Logout
                    self.logout()
                    print("Logged out and successfully reset app")
                    time.sleep(2)
                    bring_sample_app_to_foreground()

                    time.sleep(5)
                else:
                    raise SystemExit(f"Failed to reset app {err}")

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
        self.__class__.altdriver = AltDriver()
        time.sleep(5)

        # Wait for unauthenticated screen
        self.altdriver.wait_for_current_scene_to_be("UnauthenticatedScene")

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
        time.sleep(5)
        self.assertEqual("Connected to IMX", output.get_text())

        self.altdriver.stop()

    def test_7_reconnect_connect_imx(self):
        # Close and reopen app
        stop_sample_app()
        open_sample_app()

        # Restart AltTester
        self.altdriver.stop()
        self.__class__.altdriver = AltDriver()
        time.sleep(5)

        # Wait for unauthenticated screen
        self.altdriver.wait_for_current_scene_to_be("UnauthenticatedScene")
        
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
        self.logout()
        
        # Connect IMX
        time.sleep(5)
        print("Logging in and connecting to IMX...")
        self.launch_browser()
        bring_sample_app_to_foreground()
        self.altdriver.wait_for_object(By.NAME, "ConnectBtn").tap()
        self.login()

        self.altdriver.wait_for_current_scene_to_be("AuthenticatedScene")
        
        self.stop_browser()
        print("Logged in and connected to IMX")

        # Get access token
        self.altdriver.find_object(By.NAME, "GetAccessTokenBtn").tap()
        output = self.altdriver.find_object(By.NAME, "Output")
        self.assertTrue(len(output.get_text()) > 50)

        # Get address without having to click Connect to IMX button
        self.altdriver.find_object(By.NAME, "GetAddressBtn").tap()
        self.assertEqual(TestConfig.WALLET_ADDRESS, output.get_text())

        # Logout
        self.logout()