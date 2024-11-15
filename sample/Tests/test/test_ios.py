import sys
import time
import unittest
from pathlib import Path

from appium import webdriver
from appium.options.ios import XCUITestOptions
from appium.webdriver.common.appiumby import AppiumBy
from appium.webdriver.webdriver import WebDriver
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC

from alttester import AltDriver, By

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / 'src'))
from fetch_otp import EMAIL, fetch_code

# To run this test on an actual Android device: appium --base-path /wd/hub --allow-insecure chromedriver_autodownload
class TestBase(unittest.TestCase):
    altdriver = None
    appium_driver = None

    @classmethod
    def setUpClass(cls):
        # https://appium.github.io/appium-xcuitest-driver/latest/preparation/real-device-config/
        options = XCUITestOptions()
        options.app = "./Payload.ipa"
        options.show_xcode_log = True
        options.xcode_org_id = "APPLE_TEAM_ID" # Replace with Apple Team ID
        options.auto_accept_alerts = True

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

        # Wait for the ASWebAuthenticationSession context to appear
        WebDriverWait(driver, 30).until(lambda d: len(d.contexts) > 2)
        contexts = driver.contexts

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

                email_field.send_keys("imx.game.sdk.demo@gmail.com")
                submit_button = driver.find_element(by=AppiumBy.XPATH, value="//form/div/div/div[2]/button")
                submit_button.click()

                time.sleep(10) # Wait for OTP

                code = fetch_gmail_code()
                assert code, "Failed to fetch OTP from Gmail"
                print(f"Successfully fetched OTP: {code}")

                # Unlike on Android, each digit must be entered into a separate input field on iOS.
                for i, digit in enumerate(code):
                    otp_field = driver.find_element(by=AppiumBy.XPATH, value=f"//div[@id='passwordless_container']/div[{i + 1}]/input")
                    otp_field.send_keys(digit)

                # Wait for authenticated screen
                self.altdriver.wait_for_current_scene_to_be("AuthenticatedScene")
                
                break
            except:
                # If the field is not found, continue to the next context
                print(f"Email field not found in context: {context}")

        # If target context was not found, raise an error
        if not target_context:
            raise Exception("Could not find the email field in any webview context.")