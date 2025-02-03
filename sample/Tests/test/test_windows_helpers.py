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

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / 'src'))
from fetch_otp import EMAIL, fetch_code

# Add chrome.exe to environment variable
# Download chrome driver and add to environment variable

def login():
    print("Connect to Chrome")
    # Set up Chrome options to connect to the existing Chrome instance
    chrome_options = Options()
    chrome_options.add_experimental_option("debuggerAddress", "localhost:9222")
    # Connect to the existing Chrome instance
    driver = webdriver.Chrome(options=chrome_options)

    print("Open a window on Chrome")
    # Get the original window handle
    original_window = driver.current_window_handle

    print("Waiting for new window...")
    WebDriverWait(driver, 60).until(EC.number_of_windows_to_be(2))

    # Get all window handles
    all_windows = driver.window_handles

    print("Find the new window")
    new_window = [window for window in all_windows if window != driver.current_window_handle][0]

    print("Switch to the new window")
    driver.switch_to.window(new_window)
    
    wait = WebDriverWait(driver, 60)

    print("Wait for device confirmation...")
    contine_button = wait.until(EC.element_to_be_clickable((By.XPATH, "//button[span[text()='Continue']]")))
    contine_button.click()
    print("Confirmed device")

    print("Wait for email input...")
    email_field = wait.until(EC.presence_of_element_located((By.ID, ':r1:')))
    print("Enter email...")
    email_field.send_keys(EMAIL)
    email_field.send_keys(Keys.RETURN)
    print("Entered email")

    # Wait for the OTP to arrive and page to load
    print("Wait for OTP...")
    time.sleep(10)

    print("Get OTP from MailSlurp...")
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
    subprocess.Popen(["SampleApp.exe"], shell=True)
    time.sleep(10)
    print("Unity sample app opened successfully.")

def stop_sample_app():
    print("Stopping sample app...")
    powershell_command = """
    $process = Get-Process -Name "SampleApp" -ErrorAction SilentlyContinue
    if ($process) {
        Stop-Process -Id $process.Id
        Write-Output "SampleApp.exe has been closed."
    } else {
        Write-Output "SampleApp.exe is not running."
    }
    """
    subprocess.run(["powershell.exe", "-Command", powershell_command], check=True)
    time.sleep(5)
    print("Stopped sample app.")

def bring_sample_app_to_foreground():
    powershell_script_path = "./switch-app.ps1"
    
    print("Bring Unity sample app to the foreground.")
    
    command = [
        "powershell.exe", 
        "-Command", 
        f"Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process; & '{powershell_script_path}' -appName 'Immutable Sample'"
    ]
    
    subprocess.run(command, check=True)
    time.sleep(10)

def launch_chrome():
    print("Starting Chrome...")
    chrome_paths = [
        r"C:\Program Files\Google\Chrome\Application\chrome.exe",
        r"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"
    ]

    chrome_path = None
    for path in chrome_paths:
        if os.path.exists(path):
            chrome_path = path
            break

    if not chrome_path:
        print("Chrome executable not found.")
        exit(1)

    subprocess.run([
        "powershell.exe",
        "-Command",
        f"Start-Process -FilePath '{chrome_path}' -ArgumentList '--remote-debugging-port=9222'"
    ], check=True)

    time.sleep(5)

def stop_chrome():
    print("Stopping Chrome...")
    powershell_command = """
    $process = Get-Process -Name "chrome" -ErrorAction SilentlyContinue
    if ($process) {
        $process | ForEach-Object {
            Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
        }
        Write-Output "All Chrome processes have been closed."
    } else {
        Write-Output "Chrome is not running."
    }
    """
    subprocess.run(["powershell.exe", "-Command", powershell_command], check=True)
    time.sleep(5)
    print("Stopped Chrome.")