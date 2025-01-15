import sys
import time
import unittest
from pathlib import Path

from appium import webdriver
from appium.options.android import UiAutomator2Options
from appium.webdriver.common.appiumby import AppiumBy
from appium.webdriver.webdriver import WebDriver
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC

from alttester import *

from test import UnityTest

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

    def test_1_pkce_login(self):
        # Select use PKCE auth
        self.altdriver.find_object(By.NAME, "PKCE").tap()

        # Wait for unauthenticated screen
        self.altdriver.wait_for_current_scene_to_be("UnauthenticatedScene")

        # Login
        loginBtn = self.altdriver.wait_for_object(By.NAME, "LoginBtn")
        loginBtn.tap()

        driver = self.appium_driver

        # Wait for the Chrome Custom Tabs context to appear
        WebDriverWait(driver, 30).until(lambda d: 'WEBVIEW_chrome' in d.contexts)
        driver.switch_to.context("WEBVIEW_chrome")

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
