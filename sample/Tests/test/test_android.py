import sys
import time
import unittest
from pathlib import Path

from appium import webdriver
from appium.options.android import UiAutomator2Options
from appium.webdriver.common.appiumby import AppiumBy
from selenium.webdriver.support.ui import WebDriverWait

from alttester import *

from test import TestConfig, UnityTest

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / 'src'))
from fetch_otp import EMAIL, fetch_code

# To run this test on an actual Android device: appium --base-path /wd/hub --allow-insecure chromedriver_autodownload
class TestBase(UnityTest):
    altdriver = None
    appium_driver = None

    @classmethod
    def setUpClass(cls):
        options = UiAutomator2Options()
        options.auto_web_view = True
        options.ensure_webviews_have_pages = True
        options.native_web_screenshot = True
        options.new_command_timeout = 3600

        cls.appium_driver = webdriver.Remote('https://hub-cloud.browserstack.com/wd/hub/', options=options)

        time.sleep(10)
        cls.altdriver = AltDriver()

    @classmethod
    def tearDownClass(cls):
        print("\nEnding")
        cls.altdriver.stop()
        cls.appium_driver.quit()

    @classmethod
    def login(cls):
        driver = cls.appium_driver

        # Wait for the Chrome Custom Tabs context to appear
        WebDriverWait(driver, 30).until(lambda d: 'WEBVIEW_chrome' in d.contexts)
        driver.switch_to.context("WEBVIEW_chrome")

        # Ensure the latest window is active as the previous window might have been closed
        handles = driver.window_handles
        driver.switch_to.window(handles[-1])

        email_field = driver.find_element(by=AppiumBy.XPATH, value="//input[@name=\"address\"]")
        email_field.send_keys(EMAIL)
        submit_button = driver.find_element(by=AppiumBy.XPATH, value="//form/div/div/div[2]/button")
        submit_button.click()

        time.sleep(10) # Wait for OTP

        code = fetch_code()
        if code:
            print(f"Successfully fetched OTP: {code}")
        else:
            print("Failed to fetch OTP from email")

        otp_field = driver.find_element(by=AppiumBy.XPATH, value="//div[@id=\"passwordless_container\"]/div[1]/input")
        otp_field.send_keys(code)

    @classmethod
    def close_and_open_app(cls):
        driver = cls.appium_driver

        # Close app
        time.sleep(5)
        print("Closing app...")
        driver.terminate_app(TestConfig.ANDROID_PACKAGE)
        time.sleep(5)
        print("Closed app")

        # Reopen app
        print("Opening app...")
        driver.activate_app(TestConfig.ANDROID_PACKAGE)
        time.sleep(10)
        print("Opened app")

    def test_1_pkce_login(self):
        # Select use PKCE auth
        self.altdriver.find_object(By.NAME, "PKCE").tap()

        # Wait for unauthenticated screen
        self.altdriver.wait_for_current_scene_to_be("UnauthenticatedScene")

        # Login
        loginBtn = self.altdriver.wait_for_object(By.NAME, "LoginBtn")
        loginBtn.tap()

        self.login()

        # Wait for authenticated screen
        self.altdriver.wait_for_current_scene_to_be("AuthenticatedScene")

    def test_2_other_functions(self):
        self.test_0_other_functions()

    def test_3_passport_functions(self):
        self.test_1_passport_functions()

    def test_4_imx_functions(self):
        self.test_2_imx_functions()

    def test_5_zkevm_functions(self):
        self.test_3_zkevm_functions()

    def test_6_pkce_relogin(self):
        driver = self.appium_driver

        self.close_and_open_app()

        # Restart AltTester
        self.altdriver.stop()
        self.altdriver = AltDriver()
        time.sleep(5)

        # # Select use PKCE auth
        self.altdriver.find_object(By.NAME, "PKCE").tap()
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

    def test_7_pkce_reconnect(self):
        self.close_and_open_app()

        # Restart AltTester
        self.altdriver.stop()
        self.altdriver = AltDriver()
        time.sleep(5)

        # Select use PKCE auth
        self.altdriver.find_object(By.NAME, "PKCE").tap()
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
        self.altdriver.find_object(By.NAME, "LogoutBtn").tap()
        time.sleep(5)
        
        # Wait for authenticated screen
        self.altdriver.wait_for_current_scene_to_be("UnauthenticatedScene")
        time.sleep(5)
        print("Logged out")

        self.altdriver.stop()

    def test_8_pkce_connect_imx(self):
        self.close_and_open_app()

        # Restart AltTester
        self.altdriver.stop()
        self.altdriver = AltDriver()
        time.sleep(5)

        # Select use PKCE auth
        self.altdriver.find_object(By.NAME, "PKCE").tap()
        # Wait for unauthenticated screen
        self.altdriver.wait_for_current_scene_to_be("UnauthenticatedScene")
        
        # Connect IMX
        print("Logging in and connecting to IMX...")
        self.altdriver.wait_for_object(By.NAME, "ConnectBtn").tap()

        self.login()

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
        self.altdriver.find_object(By.NAME, "LogoutBtn").tap()
        time.sleep(5)

        # Wait for authenticated screen
        self.altdriver.wait_for_current_scene_to_be("UnauthenticatedScene")
        time.sleep(5)
        print("Logged out")