import os
import re
import subprocess
import sys
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

def get_product_name():
    """Get the product name from ProjectSettings.asset"""
    project_settings_path = Path(__file__).resolve().parent.parent.parent / 'ProjectSettings' / 'ProjectSettings.asset'

    if not project_settings_path.exists():
        print(f"Warning: ProjectSettings.asset not found at {project_settings_path}")
        return "SampleApp"  # Fallback to default

    with open(project_settings_path, 'r') as f:
        content = f.read()

    # Extract productName using regex
    match = re.search(r'productName: (.+)', content)
    if match:
        product_name = match.group(1).strip()
        return product_name

    # If regex fails, return default
    return "SampleApp"

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
    
    print(f"Found {len(all_windows)} new windows to check: {all_windows}")
    
    # Find the window with email input
    target_window = None
    for window in all_windows:
        try:
            print(f"Checking window: {window}")
            driver.switch_to.window(window)
            driver.find_element(By.ID, ':r1:')
            target_window = window
            print(f"Found email input in window: {window}")
            break
        except:
            print(f"Email input not found in window: {window}, trying next...")
            continue
    
    if not target_window:
        print("Could not find email input field in any window!")
        driver.quit()
        return
    
    print("Switch to the target window")
    driver.switch_to.window(target_window)

    wait = WebDriverWait(driver, 60)

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
    wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, 'h1[data-testid="checking_title"]')))
    print("Connected to Passport!")

    driver.quit()

def open_sample_app():
    product_name = get_product_name()
    print(f"Opening {product_name}...")
    subprocess.Popen([f"{product_name}.exe"], shell=True)
    time.sleep(10)
    print(f"{product_name} opened successfully.")

def stop_sample_app():
    product_name = get_product_name()
    print(f"Stopping {product_name}...")
    powershell_command = f"""
    $process = Get-Process -Name "{product_name}" -ErrorAction SilentlyContinue
    if ($process) {{
        Stop-Process -Id $process.Id
        Write-Output "{product_name}.exe has been closed."
    }} else {{
        Write-Output "{product_name}.exe is not running."
    }}
    """
    subprocess.run(["powershell.exe", "-Command", powershell_command], check=True)
    time.sleep(5)
    print(f"{product_name} stopped successfully.")

def bring_sample_app_to_foreground():
    product_name = get_product_name()
    powershell_script_path = "./switch-app.ps1"

    print(f"Bring {product_name} to the foreground.")

    command = [
        "powershell.exe",
        "-ExecutionPolicy", "Bypass",
        "-File", powershell_script_path,
        "-appName", product_name
    ]

    subprocess.run(command, check=True)
    time.sleep(10)

def launch_browser():
    print("Starting Brave...")
    browser_paths = [
        r"C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe"
    ]

    browser_path = None
    for path in browser_paths:
        if os.path.exists(path):
            browser_path = path
            break

    if not browser_path:
        print("Brave executable not found.")
        exit(1)

    subprocess.run([
        "powershell.exe",
        "-Command",
        f"Start-Process -FilePath '{browser_path}' -ArgumentList '--remote-debugging-port=9222'"
    ], check=True)

    time.sleep(5)

def stop_browser():
    print("Stopping Brave...")
    powershell_command = """
    $process = Get-Process -Name "brave" -ErrorAction SilentlyContinue
    if ($process) {
        $process | ForEach-Object {
            Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
        }
        Write-Output "All Brave processes have been closed."
    } else {
        Write-Output "Brave is not running."
    }
    """
    subprocess.run(["powershell.exe", "-Command", powershell_command], check=True)
    time.sleep(5)
    print("Stopped Brave")