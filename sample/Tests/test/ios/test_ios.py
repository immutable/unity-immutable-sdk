import os
import sys
import time
from pathlib import Path
import pytest

from appium.webdriver.common.appiumby import AppiumBy
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.common.by import By

from alttester import *

PROJECT_ROOT = Path(__file__).resolve().parents[2]  
sys.path.insert(0, str(PROJECT_ROOT / "test"))
sys.path.insert(0, str(PROJECT_ROOT / "src"))
from test import TestConfig, UnityTest
from fetch_otp import fetch_code

@pytest.mark.usefixtures('setWebdriver')
class TestIos:

    def login(self):
        driver = self.driver

        # Wait for the ASWebAuthenticationSession context to appear
        WebDriverWait(driver, 30).until(lambda d: len(d.contexts) > 1)
        time.sleep(5) # Refresh contexts by waiting before fetching again
        print("Available contexts:", driver.contexts)
        contexts = driver.contexts
        driver.switch_to.context(driver.contexts[-1])
        print("Current context:", driver.current_context)

        target_context = None

        # Since it's unclear which WebView context contains the email field on iOS, 
        # we need to iterate through each context to identify the correct one.
        for context in contexts:
            if context == "NATIVE_APP":
                continue

            driver.switch_to.context(context)
            
            try:
                # Attempt to find the email input field
                email_field = WebDriverWait(driver, 5).until(
                    EC.presence_of_element_located((AppiumBy.XPATH, "//input[@name='address']"))
                )
                # Found email
                target_context = context

                email_field.send_keys(TestConfig.EMAIL)
                submit_button = driver.find_element(by=AppiumBy.XPATH, value="//form/div/div/div[2]/button")
                submit_button.click()

                time.sleep(10) # Wait for OTP

                code = fetch_code()
                assert code, "Failed to fetch OTP from MailSlurp"
                print(f"Successfully fetched OTP: {code}")

                # Unlike on Android, each digit must be entered into a separate input field on iOS.
                for i, digit in enumerate(code):
                    otp_field = driver.find_element(by=AppiumBy.XPATH, value=f"//div[@id='passwordless_container']/div[{i + 1}]/input")
                    otp_field.send_keys(digit)

                break
            except:
                # If the field is not found, continue to the next context
                print(f"Email field not found in context: {context}")

        # If target context was not found, raise an error
        if not target_context:
            raise Exception("Could not find the email field in any webview context.")
        
    # @classmethod
    # def close_and_open_app(cls):
    #     driver = cls.driver

    #     # Close app
    #     time.sleep(5)
    #     print("Closing app...")
    #     driver.terminate_app(TestConfig.IOS_BUNDLE_ID)
    #     time.sleep(5)
    #     print("Closed app")

    #     # Reopen app
    #     print("Opening app...")
    #     driver.activate_app(TestConfig.IOS_BUNDLE_ID)
    #     time.sleep(10)
    #     print("Opened app")

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

    # def test_2_other_functions(self):
    #     self.test_0_other_functions()

    # def test_3_passport_functions(self):
    #     self.test_1_passport_functions()

    # def test_4_imx_functions(self):
    #     self.test_2_imx_functions()

    # def test_5_zkevm_functions(self):
    #     self.test_3_zkevm_functions()

    # def test_6_pkce_relogin(self):
    #     driver = self.appium_driver

    #     self.close_and_open_app()

    #     # Restart AltTester
    #     self.altdriver.stop()
    #     self.altdriver = AltDriver()
    #     time.sleep(5)

    #     # # Select use PKCE auth
    #     self.altdriver.find_object(By.NAME, "PKCE").tap()
    #     # Wait for unauthenticated screen
    #     self.altdriver.wait_for_current_scene_to_be("UnauthenticatedScene")

    #     # Relogin
    #     print("Re-logging in...")
    #     self.altdriver.wait_for_object(By.NAME, "ReloginBtn").tap()

    #     # Wait for authenticated screen
    #     self.altdriver.wait_for_current_scene_to_be("AuthenticatedScene")
    #     print("Re-logged in")

    #     # Get access token
    #     self.altdriver.find_object(By.NAME, "GetAccessTokenBtn").tap()
    #     output = self.altdriver.find_object(By.NAME, "Output")
    #     self.assertTrue(len(output.get_text()) > 50)

    #     # Click Connect to IMX button
    #     self.altdriver.find_object(By.NAME, "ConnectBtn").tap()
    #     self.assertEqual("Connected to IMX", output.get_text())

    #     self.altdriver.stop()

    # def test_7_pkce_reconnect(self):
    #     self.close_and_open_app()

    #     # Restart AltTester
    #     self.altdriver.stop()
    #     self.altdriver = AltDriver()
    #     time.sleep(5)

    #     # Select use PKCE auth
    #     self.altdriver.find_object(By.NAME, "PKCE").tap()
    #     # Wait for unauthenticated screen
    #     self.altdriver.wait_for_current_scene_to_be("UnauthenticatedScene")
        
    #     # Reconnect
    #     print("Reconnecting...")
    #     self.altdriver.wait_for_object(By.NAME, "ReconnectBtn").tap()

    #     # Wait for authenticated screen
    #     self.altdriver.wait_for_current_scene_to_be("AuthenticatedScene")
    #     print("Reconnected")

    #     # Get access token
    #     self.altdriver.find_object(By.NAME, "GetAccessTokenBtn").tap()
    #     output = self.altdriver.find_object(By.NAME, "Output")
    #     self.assertTrue(len(output.get_text()) > 50)

    #     # Get address without having to click Connect to IMX button
    #     self.altdriver.find_object(By.NAME, "GetAddressBtn").tap()
    #     self.assertEqual(TestConfig.WALLET_ADDRESS, output.get_text())

    #     # Logout
    #     print("Logging out...")
    #     self.altdriver.find_object(By.NAME, "LogoutBtn").tap()
    #     time.sleep(5)
        
    #     # Wait for authenticated screen
    #     self.altdriver.wait_for_current_scene_to_be("UnauthenticatedScene")
    #     time.sleep(5)
    #     print("Logged out")

    #     self.altdriver.stop()

    # def test_8_pkce_connect_imx(self):
    #     self.close_and_open_app()

    #     # Restart AltTester
    #     self.altdriver.stop()
    #     self.altdriver = AltDriver()
    #     time.sleep(5)

    #     # Select use PKCE auth
    #     self.altdriver.find_object(By.NAME, "PKCE").tap()
    #     # Wait for unauthenticated screen
    #     self.altdriver.wait_for_current_scene_to_be("UnauthenticatedScene")
        
    #     # Connect IMX
    #     print("Logging in and connecting to IMX...")
    #     self.altdriver.wait_for_object(By.NAME, "ConnectBtn").tap()

    #     self.login()

    #     # Wait for authenticated screen
    #     self.altdriver.wait_for_current_scene_to_be("AuthenticatedScene")
    #     print("Logged in and connected to IMX")

    #     # Get access token
    #     self.altdriver.find_object(By.NAME, "GetAccessTokenBtn").tap()
    #     output = self.altdriver.find_object(By.NAME, "Output")
    #     self.assertTrue(len(output.get_text()) > 50)

    #     # Get address without having to click Connect to IMX button
    #     self.altdriver.find_object(By.NAME, "GetAddressBtn").tap()
    #     self.assertEqual(TestConfig.WALLET_ADDRESS, output.get_text())

    #     # Logout
    #     print("Logging out...")
    #     self.altdriver.find_object(By.NAME, "LogoutBtn").tap()
    #     time.sleep(5)

    #     # Wait for authenticated screen
    #     self.altdriver.wait_for_current_scene_to_be("UnauthenticatedScene")
    #     time.sleep(5)
    #     print("Logged out")