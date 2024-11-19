import os
import sys
import subprocess
import time
from pathlib import Path

from selenium import webdriver
from selenium.webdriver.chrome.service import Service
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.common.keys import Keys
import time

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / 'src'))
from fetch_otp import EMAIL, fetch_code

# brew install chromedriver

def login():
    print("Connect to Chrome")
    # Set up Chrome options to connect to the existing Chrome instance
    chrome_options = Options()
    chrome_options.add_argument('--remote-debugging-port=9222')
    # Connect to the existing Chrome instance
    driver = webdriver.Chrome(options=chrome_options)

    print("Open a window on Chrome")
    # Get the original window handle
    original_window = driver.current_window_handle

    print("Waiting for new window...")
    WebDriverWait(driver, 30).until(EC.number_of_windows_to_be(2))

    # Get all window handles
    all_windows = driver.window_handles

    print("Find the new window")
    new_window = [window for window in all_windows if window != driver.current_window_handle][0]

    print("Switch to the new window")
    driver.switch_to.window(new_window)
    
    wait = WebDriverWait(driver, 60)

    print("Wait for email input...")
    email_field = wait.until(EC.presence_of_element_located((By.ID, ':r1:')))
    print("Enter email")
    email_field.send_keys(EMAIL)
    email_field.send_keys(Keys.RETURN)

    # Wait for the OTP to arrive and page to load
    print("Wait for OTP...")
    time.sleep(10)

    print("Get OTP from Mailslurp...")
    code = fetch_code()
    if code:
        print(f"Successfully fetched OTP: {code}")
    else:
        print("Failed to fetch OTP from MailSlurp")
        driver.quit()

    print("Find OTP input...")
    otp_field = wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, 'input[data-testid="passwordless_passcode__TextInput--0__input"]')))
    print("Enter OTP")
    otp_field.send_keys(code)

    print("Wait for success page...")
    success = wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, 'h1[data-testid="device_success_title"]')))
    print("Connected to Passport!")

    driver.quit()

def open_sample_app():
    print("Opening Unity sample app...")
    subprocess.Popen(["open", "SampleApp.app"], shell=False)
    time.sleep(5)
    print("Unity sample app opened successfully.")

def stop_sample_app():
    print("Stopping sample app...")
    try:
        # Get the PID of the sample app using ps, grep, and awk
        cmd = f"ps aux | grep 'Sample.app' | grep -v grep | awk '{{print $2}}'"
        pid = subprocess.check_output(cmd, shell=True, text=True).strip()

        if pid:
            # Terminate the process using the PID
            subprocess.run(["kill", pid])
            print(f"Sample app (PID {pid}) has been terminated.")
        else:
            print("Sample app is not running.")
    except subprocess.CalledProcessError:
        print("Failed to find the sample app process.")

    time.sleep(5)
    print("Stopped sample app.")

def bring_sample_app_to_foreground(app_name):
    print("Bringing Unity sample app to the foreground...")
    subprocess.run(
            ['osascript', '-e', f'tell application "{app_name}" to activate'],
            check=True
        )
    
def stop_chrome():
    print("Stopping Chrome all Chrome instances...")
    subprocess.run(["pkill", "-f", "chrome"], check=True)
    print("Stopped Chrome.")