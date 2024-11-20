import sys
import time
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
    seleniumdriver = None

    @classmethod
    def setUpClass(cls):
        open_sample_app()
        cls.altdriver = AltDriver()

    @classmethod
    def tearDownClass(cls):
        stop_sample_app()
        cls.altdriver.stop()

    @classmethod
    def setupChrome(cls):
        print("Connect to Chrome")
        chrome_options = Options()
        chrome_options.add_argument('--remote-debugging-port=9222')

        # Initialise Chrome driver
        cls.seleniumdriver = webdriver.Chrome(options=chrome_options)

        print("Open a window on Chrome")
        cls.seleniumdriver.current_window_handle

    @classmethod
    def login(cls):
        print("Waiting for new window...")
        WebDriverWait(cls.seleniumdriver, 30).until(EC.number_of_windows_to_be(2))

        # Switch to the new window
        all_windows = cls.seleniumdriver.window_handles
        new_window = [window for window in all_windows if window != cls.seleniumdriver.current_window_handle][0]
        cls.seleniumdriver.switch_to.window(new_window)
        print("Switched to new window")

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

        # Wait for success page and confirm
        success = WebDriverWait(cls.seleniumdriver, 60).until(EC.presence_of_element_located((SeleniumBy.CSS_SELECTOR, 'h1[data-testid="device_success_title"]')))
        print("Connected to Passport!")
        
        cls.seleniumdriver.quit()

    def test_1_device_code_login(self):
        # Select use device code auth
        self.altdriver.find_object(By.NAME, "DeviceCodeAuth").tap()

        # Wait for unauthenticated screen
        self.altdriver.wait_for_current_scene_to_be("UnauthenticatedScene")

        # Login
        print("Logging in...")
        self.setupChrome()
        bring_sample_app_to_foreground()
        self.altdriver.wait_for_object(By.NAME, "LoginBtn").tap()
        self.login()
        bring_sample_app_to_foreground()

        # Wait for authenticated screen
        self.altdriver.wait_for_current_scene_to_be("AuthenticatedScene")
        print("Logged in")

    def test_2_other_functions(self):
        self.test_0_other_functions()

    def test_3_passport_functions(self):
        self.test_1_passport_functions()

    def test_4_imx_functions(self):
        self.test_2_imx_functions()

    def test_5_zkevm_functions(self):
        self.test_3_zkevm_functions()

    def test_6_device_code_relogin(self):
        # Close and reopen app
        stop_sample_app()
        open_sample_app()

        # Restart AltTester
        self.altdriver.stop()
        self.altdriver = AltDriver()
        time.sleep(5)

        # Select use device code auth
        self.altdriver.find_object(By.NAME, "DeviceCodeAuth").tap()
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

        # Select use device code auth
        self.altdriver.find_object(By.NAME, "DeviceCodeAuth").tap()
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
        print("Logging out...")
        self.setupChrome()
        bring_sample_app_to_foreground()
        self.altdriver.find_object(By.NAME, "LogoutBtn").tap()
        time.sleep(5)
        bring_sample_app_to_foreground()
        
        # Wait for authenticated screen
        self.altdriver.wait_for_current_scene_to_be("UnauthenticatedScene")
        self.seleniumdriver.quit()
        print("Logged out")
        
        # Connect IMX
        print("Logging in and connecting to IMX...")
        self.setupChrome()
        bring_sample_app_to_foreground()
        self.altdriver.wait_for_object(By.NAME, "ConnectBtn").tap()
        self.login()
        bring_sample_app_to_foreground()

        # Wait for authenticated screen
        self.altdriver.wait_for_current_scene_to_be("AuthenticatedScene")
        print("Logged in and connected to IMX")

        # Get access token
        self.altdriver.find_object(By.NAME, "GetAccessTokenBtn").tap()
        output = self.altdriver.find_object(By.NAME, "Output")
        self.assertTrue(len(output.get_text()) > 50)

        # Get address without having to click Connect to IMX button
        self.altdriver.find_object(By.NAME, "GetAddressBtn").tap()
        self.assertEqual(TestConfig.WALLET_ADDRESS, output.get_text())

        # Logout
        print("Logging out...")
        self.setupChrome()
        bring_sample_app_to_foreground()
        self.altdriver.find_object(By.NAME, "LogoutBtn").tap()
        time.sleep(5)
        bring_sample_app_to_foreground()

        # Wait for authenticated screen
        self.altdriver.wait_for_current_scene_to_be("UnauthenticatedScene")
        self.seleniumdriver.quit()
        print("Logged out")