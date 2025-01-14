import sys
import time
import unittest
from pathlib import Path
import subprocess

from selenium import webdriver
from selenium.webdriver.chrome.service import Service
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.common.by import By as SeleniumBy
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.common.keys import Keys

from test import TestConfig, UnityTest
from test_mac_helpers import open_sample_app, bring_sample_app_to_foreground, stop_sample_app

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / 'src'))
from fetch_otp import fetch_code

class MacTest2(unittest.TestCase):

    seleniumdriver = None

    @classmethod
    def login(cls):
        print("Waiting for new window...")
        WebDriverWait(cls.seleniumdriver, 30).until(EC.number_of_windows_to_be(2))

        # Switch to the new window
        all_windows = cls.seleniumdriver.window_handles
        new_window = [window for window in all_windows if window != cls.seleniumdriver.current_window_handle][0]
        cls.seleniumdriver.switch_to.window(new_window)
        print("Switched to new window")

        # Wait for confirmation screen
        continue_btn = WebDriverWait(cls.seleniumdriver, 60).until(EC.presence_of_element_located((SeleniumBy.CLASS_NAME, '"Button--primary"')))
        continue_btn.click()

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

    def test_case(self):
        print("Connect to Chrome")
        # chrome_options = Options()
        # chrome_options.add_argument('--remote-debugging-port=9222')

        print("Initialise Chrome driver")
        seleniumdriver = webdriver.Safari() #webdriver.Chrome(options=chrome_options)
        seleniumdriver.switch_to.window(seleniumdriver.window_handles[-1])
        print(seleniumdriver.title)
        # seleniumdriver.get('https://www.google.com')

        # print("Open a window on Chrome")
        # seleniumdriver.current_window_handle
        # print("Current window handles:", seleniumdriver.window_handles)

        # time.sleep(5)

        # Login
        # print("Logging in...")
        # open_sample_app()

        # time.sleep(10)

        # print("Waiting for new window...")
        # print("Current URL:", seleniumdriver.current_url)
        # time.sleep(10)

        # print("New URL:", seleniumdriver.current_url)
        # print("Number of windows:", len(seleniumdriver.window_handles))
        # WebDriverWait(seleniumdriver, 30).until(EC.number_of_windows_to_be(2))

        # # Switch to the new window
        # all_windows = seleniumdriver.window_handles
        # new_window = [window for window in all_windows if window != seleniumdriver.current_window_handle][0]
        # seleniumdriver.switch_to.window(new_window)
        # print("Switched to new window")

        # Wait for confirmation screen
        continue_btn = WebDriverWait(seleniumdriver, 60).until(EC.presence_of_element_located((SeleniumBy.CLASS_NAME, 'Button--primary')))
        continue_btn.click()

        # Wait for email input and enter email
        email_field = WebDriverWait(seleniumdriver, 60).until(EC.presence_of_element_located((SeleniumBy.ID, ':r1:')))
        print("Entering email...")
        email_field.send_keys(TestConfig.EMAIL)
        email_field.send_keys(Keys.RETURN)

        # Wait for OTP
        print("Waiting for OTP...")
        time.sleep(10)
        print("Fetching OTP from MailSlurp...")
        code = fetch_code()
        
        if not code:
            seleniumdriver.quit()
            raise AssertionError("Failed to fetch OTP from MailSlurp")
        
        print(f"Successfully fetched OTP: {code}")

        # Find OTP input and enter the code
        otp_field = WebDriverWait(seleniumdriver, 60).until(EC.presence_of_element_located((SeleniumBy.CSS_SELECTOR, 'input[data-testid="passwordless_passcode__TextInput--0__input"]')))
        print("Entering OTP...")
        otp_field.send_keys(code)

        # Wait for success page and confirm
        success = WebDriverWait(seleniumdriver, 60).until(EC.presence_of_element_located((SeleniumBy.CSS_SELECTOR, 'h1[data-testid="device_success_title"]')))
        print("Connected to Passport!")
        
        seleniumdriver.quit()
