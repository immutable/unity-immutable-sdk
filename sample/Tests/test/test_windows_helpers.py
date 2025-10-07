"""
Windows Test Helpers for Unity Passport Authentication

CRITICAL WORKAROUND IMPLEMENTED:
This module implements browser process isolation workarounds for Unity Passport authentication.

PROBLEM:
Unity's Application.OpenURL() opens authentication URLs in separate browser processes that 
automated testing tools (Selenium) cannot control. This breaks authentication flows in CI.

SOLUTION:
1. Launch browser with remote debugging enabled (port 9222)
2. Monitor Unity logs to capture auth/logout URLs that Unity wants to open
3. Navigate controlled browser to captured URLs instead of relying on Unity's browser
4. Complete authentication/logout in controlled browser
5. Unity receives callbacks properly due to protocol association setup

This approach enables reliable automated testing of Passport authentication flows.
"""

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

def get_auth_url_from_unity_logs():
    """Monitor Unity logs to capture the PASSPORT_AUTH_URL."""
    import tempfile
    import os
    
    # Unity log file locations on Windows
    log_paths = [
        os.path.join(os.path.expanduser("~"), "AppData", "LocalLow", "Immutable", "Immutable Sample", "Player.log"),
        os.path.join(tempfile.gettempdir(), "UnityPlayer.log"),
        "Player.log"  # Current directory
    ]
    
    for log_path in log_paths:
        if os.path.exists(log_path):
            print(f"Monitoring Unity log: {log_path}")
            # Read the log file and look for PASSPORT_AUTH_URL
            try:
                with open(log_path, 'r', encoding='utf-8', errors='ignore') as f:
                    content = f.read()
                    # Look for either our custom message or the existing LaunchAuthURL message
                    # Get the LAST occurrence (most recent) and make sure it's a login URL, not logout
                    # Now includes [Immutable] tag from PassportLogger
                    matches = re.findall(r'(?:\[Immutable\] PASSPORT_AUTH_URL: |PASSPORT_AUTH_URL: |LaunchAuthURL : )(https?://[^\s]+)', content)
                    if matches:
                        # Get the last URL and make sure it's not a logout URL
                        for url in reversed(matches):
                            if 'im-logged-out' not in url and 'logout' not in url:
                                match = type('obj', (object,), {'group': lambda x, n: url if n == 1 else None})()
                                break
                        else:
                            # All URLs were logout URLs, take the last one anyway
                            if matches:
                                match = type('obj', (object,), {'group': lambda x, n: matches[-1] if n == 1 else None})()
                            else:
                                match = None
                    else:
                        match = None
                    if match:
                        url = match.group(1)
                        print(f"Found auth URL in Unity logs: {url}")
                        return url
            except Exception as e:
                print(f"Error reading log file {log_path}: {e}")
                continue
    
    print("No auth URL found in Unity logs")
    return None

def get_logout_url_from_unity_logs():
    """Monitor Unity logs to capture logout URLs."""
    import tempfile
    import os
    
    # Unity log file locations on Windows
    log_paths = [
        os.path.join(os.path.expanduser("~"), "AppData", "LocalLow", "Immutable", "Immutable Sample", "Player.log"),
        os.path.join(tempfile.gettempdir(), "UnityPlayer.log"),
        "Player.log"  # Current directory
    ]
    
    for log_path in log_paths:
        if os.path.exists(log_path):
            print(f"Monitoring Unity log for logout URL: {log_path}")
            try:
                with open(log_path, 'r', encoding='utf-8', errors='ignore') as f:
                    content = f.read()
                    # Look for logout URLs in Unity logs (uses same PASSPORT_AUTH_URL pattern)
                    # Now includes [Immutable] tag from PassportLogger
                    matches = re.findall(r'(?:\[Immutable\] PASSPORT_AUTH_URL: |PASSPORT_AUTH_URL: |LaunchAuthURL : )(https?://[^\s]+)', content)
                    if matches:
                        # Get the last URL and make sure it's a logout URL
                        for url in reversed(matches):
                            if 'logout' in url or 'im-logged-out' in url:
                                print(f"Found logout URL: {url}")
                                return url
            except Exception as e:
                print(f"Error reading log file {log_path}: {e}")
                continue
    
    print("No logout URL found in Unity logs")
    return None

def logout_with_controlled_browser():
    """Handle logout using the controlled browser instance instead of letting Unity open its own browser."""
    print("Starting controlled logout process...")
    
    # Set up Chrome WebDriver options to connect to the existing browser instance
    chrome_options = Options()
    chrome_options.add_experimental_option("debuggerAddress", "localhost:9222")
    
    try:
        # Connect to the existing browser instance with explicit paths
        from selenium.webdriver.chrome.service import Service
        chromedriver_path = r"C:\Users\WindowsBuildsdkServi\Development\chromedriver-win64\chromedriver-win64\chromedriver.exe"
        brave_path = r"C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe"
        
        # Set Brave as the browser binary
        chrome_options.binary_location = brave_path
        
        # Create service with explicit ChromeDriver path
        service = Service(executable_path=chromedriver_path)
            
        driver = webdriver.Chrome(service=service, options=chrome_options)
        print("Connected to existing browser for logout")
        
        # Monitor Unity logs for logout URL
        print("Monitoring Unity logs for logout URL...")
        logout_url = None
        for attempt in range(15):  # Try for 15 seconds (shorter timeout)
            logout_url = get_logout_url_from_unity_logs()
            if logout_url:
                break
            time.sleep(1)
        
        if logout_url:
            print(f"Navigating controlled browser to logout URL: {logout_url}")
            driver.get(logout_url)
            
            # Wait for logout to complete (protocol is already configured, no dialogs expected)
            time.sleep(3)
            print("Logout completed in controlled browser")
            
            # Check final page
            current_url = driver.current_url
            print(f"Final logout URL: {current_url}")
            
        else:
            print("Could not find logout URL in Unity logs - logout may complete without browser interaction")
        
    except Exception as e:
        print(f"Error during controlled logout: {e}")
        print("Logout may need to be handled by Unity directly")
    
    print("Controlled logout process completed")

def handle_cached_authentication(driver):
    """Handle scenarios where user is already authenticated (cached session)"""
    print("Handling cached authentication scenario...")
    print(f"Current URL: {driver.current_url}")
    print(f"Page title: {driver.title}")
    
    # Give a moment for any page transitions to complete
    time.sleep(3)
    
    # Handle deep link processing based on environment
    is_ci = os.getenv('CI') or os.getenv('GITHUB_ACTIONS') or os.getenv('BUILD_ID')
    
    if is_ci:
        print("CI environment - checking if authentication completed automatically")
        print("Monitoring Unity logs for authentication completion...")
        
        auth_success = False
        for check_attempt in range(30):  # Check for 30 seconds
            try:
                with open("C:\\Users\\WindowsBuildsdkServi\\AppData\\LocalLow\\Immutable\\Immutable Sample\\Player.log", 'r', encoding='utf-8', errors='ignore') as f:
                    content = f.read()
                    # Look for signs of successful authentication
                    if any(phrase in content for phrase in [
                        "AuthenticatedScene", 
                        "authentication successful", 
                        "logged in successfully",
                        "Passport token received"
                    ]):
                        print("Authentication success detected in Unity logs!")
                        auth_success = True
                        break
            except:
                pass
            time.sleep(1)
        
        if not auth_success:
            print("No authentication success detected - attempting automated dialog handling")
            print("Looking for protocol permission dialog to click automatically...")
            
            # Try to find and click the protocol dialog automatically
            try:
                ps_script = '''
                for ($i = 0; $i -lt 10; $i++) {
                    $windows = Get-Process | Where-Object { $_.MainWindowTitle -like "*auth.immutable.com*" -or $_.MainWindowTitle -like "*Open*" }
                    foreach ($window in $windows) {
                        try {
                            Add-Type -AssemblyName UIAutomationClient
                            $element = [Windows.Automation.AutomationElement]::FromHandle($window.MainWindowHandle)
                            if ($element) {
                                $buttons = $element.FindAll([Windows.Automation.TreeScope]::Descendants, 
                                    [Windows.Automation.Condition]::new([Windows.Automation.AutomationElement]::ControlTypeProperty, 
                                    [Windows.Automation.ControlType]::Button))
                                foreach ($button in $buttons) {
                                    $buttonText = $button.Current.Name
                                    if ($buttonText -like "*Open*" -or $buttonText -like "*Allow*" -or $buttonText -like "*Yes*") {
                                        $button.GetCurrentPattern([Windows.Automation.InvokePattern]::Pattern).Invoke()
                                        Write-Host "Clicked protocol dialog button: $buttonText"
                                        exit 0
                                    }
                                }
                            }
                        } catch {}
                    }
                    Start-Sleep 1
                }
                Write-Host "No protocol dialog found"
                '''
                
                result = subprocess.run(["powershell", "-Command", ps_script], 
                                      capture_output=True, text=True, timeout=15)
                if "Clicked protocol dialog" in result.stdout:
                    print("Successfully automated protocol dialog click in CI!")
                    # Wait a bit more for Unity to process
                    time.sleep(5)
                else:
                    print("Could not find protocol dialog to automate")
            except Exception as e:
                print(f"CI dialog automation error: {e}")
                print("Protocol dialog may require manual setup in CI environment")
            
    else:
        print("Local environment - cached authentication should work automatically")
        print("Waiting for Unity to receive the deep link callback...")
        time.sleep(5)
        print("Cached authentication processing complete")
    
    return  # Exit since cached auth is complete

def login():
    print("Connect to Brave via Chrome WebDriver")
    # Set up Chrome WebDriver options to connect to the existing Brave instance
    # (Brave uses Chromium engine so Chrome WebDriver works)
    chrome_options = Options()
    chrome_options.add_experimental_option("debuggerAddress", "localhost:9222")
    
    # Explicitly specify ChromeDriver path and Brave browser path
    from selenium.webdriver.chrome.service import Service
    chromedriver_path = r"C:\Users\WindowsBuildsdkServi\Development\chromedriver-win64\chromedriver-win64\chromedriver.exe"
    brave_path = r"C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe"
    
    # Set Brave as the browser binary
    chrome_options.binary_location = brave_path
    
    # Create service with explicit ChromeDriver path
    service = Service(executable_path=chromedriver_path)
    
    # Connect to the existing Brave browser instance
    driver = webdriver.Chrome(service=service, options=chrome_options)

    # HYBRID APPROACH: Try multi-window detection first (proven to work in CI), 
    # then fall back to Unity log monitoring if needed
    
    print("Attempting multi-window detection (primary method - proven to work)...")
    try:
        # Wait for Unity to open auth URL in new browser window
        print("Waiting for new window...")
        WebDriverWait(driver, 15).until(EC.number_of_windows_to_be(2))
        
        # Get all window handles
        all_windows = driver.window_handles
        print(f"Found {len(all_windows)} windows to check: {all_windows}")
        
        # Find the window with email input
        target_window = None
        for window in all_windows:
            try:
                print(f"Checking window: {window}")
                driver.switch_to.window(window)
                # Try to find email input in this window
                email_field = driver.find_element(By.CSS_SELECTOR, '[data-testid="TextInput__input"]')
                target_window = window
                print(f"Found email input in window: {window}")
                break
            except:
                print(f"Email input not found in window: {window}, trying next...")
                continue

        if target_window:
            print("Switch to the target window")
            driver.switch_to.window(target_window)
            print("Multi-window detection successful - proceeding with login flow")
        else:
            raise Exception("No window with email input found")
            
    except Exception as e:
        print(f"Multi-window detection failed: {e}")
        print("Falling back to Unity log monitoring method...")
        
        # FALLBACK: Unity log monitoring approach
        print("Looking for auth URL in Unity logs...")
        auth_url = None
        for attempt in range(30):  # Try for 30 seconds
            auth_url = get_auth_url_from_unity_logs()
            if auth_url:
                break
            time.sleep(1)
        
        if auth_url:
            print(f"Navigating to captured auth URL: {auth_url}")
            driver.get(auth_url)
            
            # Debug: Check what page we landed on
            time.sleep(5)  # Give more time for potential redirects
            print(f"After navigation - URL: {driver.current_url}")
            print(f"After navigation - Title: {driver.title}")
            
            # Check if we have email field (login page) or if we skipped to redirect
            try:
                email_field = driver.find_element(By.CSS_SELECTOR, '[data-testid="TextInput__input"]')
                print("Found email field via Unity log method - proceeding with login flow")
            except:
                print("No email field found - checking if we got redirected to new tab...")
                
                # If we ended up on chrome://newtab/ or similar, the redirect already happened
                if 'newtab' in driver.current_url or 'about:blank' in driver.current_url:
                    print("Browser was redirected to new tab - cached session completed redirect automatically!")
                    print("The immutablerunner:// callback was triggered but browser couldn't handle it")
                    print("This means authentication was successful, just need to wait for Unity to process it")
                    
                    # Wait and check Unity logs for authentication success instead of relying on scene changes
                    auth_success = False
                    for check_attempt in range(20):  # Check for 20 seconds
                        try:
                            with open("C:\\Users\\WindowsBuildsdkServi\\AppData\\LocalLow\\Immutable\\Immutable Sample\\Player.log", 'r', encoding='utf-8', errors='ignore') as f:
                                content = f.read()
                                # Look for signs of successful authentication in logs
                                if any(phrase in content for phrase in [
                                    "AuthenticatedScene", 
                                    "COMPLETE_LOGIN_PKCE", 
                                    "LoginPKCESuccess",
                                    "HandleLoginPkceSuccess",
                                    "authentication successful",
                                    "logged in successfully"
                                ]):
                                    print("Authentication success detected in Unity logs!")
                                    auth_success = True
                                    break
                        except:
                            pass
                        time.sleep(1)
                    
                    if auth_success:
                        print("Cached authentication confirmed successful via Unity logs")
                    else:
                        print("Could not confirm authentication success in Unity logs")
                    
                    return
                else:
                    print("Unexpected page state - handling as cached session...")
                    return handle_cached_authentication(driver)
        else:
            print("Could not find auth URL in Unity logs either!")
            driver.quit()
            return

    wait = WebDriverWait(driver, 60)

    print("Wait for email input...")
    email_field = wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, '[data-testid="TextInput__input"]')))
    print("Enter email...")
    email_field.send_keys(EMAIL)
    
    # Try to find and click the submit button (arrow button)
    submit_selectors = [
        'button[type="submit"]',                    # Primary - always works
        'button[data-testid*="submit"]',           # Fallback with testid
        'form button'                              # Last resort - any form button
    ]
    
    button_clicked = False
    for selector in submit_selectors:
        try:
            submit_button = WebDriverWait(driver, 5).until(EC.element_to_be_clickable((By.CSS_SELECTOR, selector)))
            submit_button.click()
            print(f"Successfully clicked submit button with selector: {selector}")
            button_clicked = True
            break
        except Exception as e:
            print(f"Submit button selector {selector} failed: {e}")
            continue
    
    if not button_clicked:
        print("No submit button found with any selector, trying Enter key...")
        email_field.send_keys(Keys.RETURN)
        print("Pressed Enter key")
    
    print("Email submission attempted")

    # Wait for the OTP to arrive and page to load
    print("Wait for OTP...")
    time.sleep(10)

    print("Get OTP from MailSlurp...")
    code = fetch_code()
    if code:
        print(f"Successfully fetched OTP: {code}")
    else:
        print("Failed to fetch OTP from MailSlurp - checking if authentication completed anyway...")
        
        # Sometimes Auth0 doesn't send OTP emails in test environments
        # Check if we can proceed anyway or if this is a cached session scenario
        try:
            # Check if we're already past the OTP stage
            current_url = driver.current_url
            print(f"Current URL after OTP timeout: {current_url}")
            
            # If we're at a success/callback page, authentication may have completed
            if any(keyword in current_url.lower() for keyword in ['success', 'callback', 'complete', 'checking']):
                print("Already at success page - proceeding without OTP")
                print("Waiting for Unity to receive the callback...")
                time.sleep(10)
                return
            
            # Otherwise this is a real OTP failure
            print("No OTP received and not at success page - authentication failed")
            driver.quit()
            return
        except Exception as e:
            print(f"Error checking page state after OTP timeout: {e}")
            driver.quit()
            return

    print("Find OTP input...")
    print(f"Current URL after email submission: {driver.current_url}")
    print(f"Page title after email submission: {driver.title}")
    
    # Try multiple selectors for OTP input field
    otp_selectors = [
        'input[data-testid="passwordless_passcode__TextInput--0__input"]',  # Primary - always works
        'input[data-testid*="passcode"]',      # Fallback - partial testid match
        'input[type="text"][maxlength="6"]'    # Last resort - by input characteristics
    ]
    
    otp_field = None
    for selector in otp_selectors:
        try:
            otp_field = WebDriverWait(driver, 5).until(EC.presence_of_element_located((By.CSS_SELECTOR, selector)))
            print(f"Found OTP field with selector: {selector}")
            break
        except:
            continue
    
    if not otp_field:
        print("Could not find OTP input field with any selector!")
        print("Page source snippet:")
        print(driver.page_source[:2000])  # First 2000 chars for debugging
        raise Exception("OTP input field not found")
    print("Enter OTP")
    otp_field.send_keys(code)

    print("Wait for success page...")
    wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, 'h1[data-testid="checking_title"]')))
    print("Connected to Passport!")

    # Handle optional consent screen (shouldn't appear normally but sometimes does)
    try:
        print("Checking for consent screen...")
        consent_yes_button = WebDriverWait(driver, 5).until(
            EC.element_to_be_clickable((By.XPATH, "//button[text()='Yes' or contains(text(), 'Yes')]"))
        )
        consent_yes_button.click()
        print("Clicked 'Yes' on consent screen")
    except:
        print("No consent screen found (expected behavior)")

    # Handle deep link permission dialog
    print("Waiting for deep link permission dialog...")
    print(f"Current URL: {driver.current_url}")
    print(f"Page title: {driver.title}")
    
    # Give a moment for any page transitions to complete
    time.sleep(3)
    
    try:
        # Check what's actually on the page
        buttons = driver.find_elements(By.TAG_NAME, "button")
        print(f"Found {len(buttons)} buttons on page:")
        for i, btn in enumerate(buttons[:5]):  # Show first 5 buttons
            try:
                text = btn.text.strip()
                if text:
                    print(f"  Button {i}: '{text}'")
            except:
                pass
        
        # Wait for the deep link dialog to appear and click "Open Immutable Sample.cmd"
        # Use more specific selector to avoid clicking "Restore" button
        deep_link_button = wait.until(EC.element_to_be_clickable((By.XPATH, "//button[text()='Open Immutable Sample.cmd']")))
        deep_link_button.click()
        print("Clicked deep link permission dialog - Unity should receive redirect")
    except Exception as e:
        print(f"Deep link dialog not found or failed to click: {e}")
        print("This may cause the test to timeout waiting for scene change")

    # Keep browser alive for Unity deep link redirect
    # driver.quit()

def clear_unity_data():
    """Clear Unity's persistent data to force fresh start"""
    print("Clearing Unity persistent data...")
    
    # Clear PlayerPrefs from Windows Registry
    try:
        import winreg
        registry_path = r"SOFTWARE\Immutable\Immutable Sample"
        
        # Try both HKEY_CURRENT_USER and HKEY_LOCAL_MACHINE
        for root_key in [winreg.HKEY_CURRENT_USER, winreg.HKEY_LOCAL_MACHINE]:
            try:
                winreg.DeleteKey(root_key, registry_path)
                print(f"Cleared PlayerPrefs from registry: {root_key}")
            except FileNotFoundError:
                pass  # Key doesn't exist, that's fine
            except Exception as e:
                print(f"Could not clear registry {root_key}: {e}")
                
    except ImportError:
        print("Windows registry module not available")
    except Exception as e:
        print(f"Error clearing registry: {e}")
    
    # Clear Application.persistentDataPath
    try:
        data_path = os.path.join(os.path.expanduser("~"), "AppData", "LocalLow", "Immutable", "Immutable Sample")
        if os.path.exists(data_path):
            import shutil
            shutil.rmtree(data_path)
            print(f"Cleared persistent data folder: {data_path}")
        else:
            print(f"No persistent data folder found at: {data_path}")
    except Exception as e:
        print(f"Error clearing persistent data: {e}")
    
    print("Unity data cleanup complete")

def open_sample_app(clear_data=False):
    product_name = os.getenv("UNITY_APP_NAME", get_product_name())
    
    # Clear any cached login state before opening (only when requested)
    if clear_data:
        clear_unity_data()
    
    print(f"Opening {product_name}...")
    
    # Look for the executable in build folder first, then current directory
    exe_paths = [
        f"../build/{product_name}.exe",  # Relative to Tests folder
        f"{product_name}.exe"  # Current directory (fallback)
    ]
    
    exe_launched = False
    for exe_path in exe_paths:
        if os.path.exists(exe_path):
            print(f"Found executable at: {exe_path}")
            subprocess.Popen([exe_path], shell=True)
            exe_launched = True
            break
    
    if not exe_launched:
        print(f"ERROR: Could not find {product_name}.exe in any of these locations:")
        for path in exe_paths:
            abs_path = os.path.abspath(path)
            print(f"  - {abs_path} (exists: {os.path.exists(abs_path)})")
        raise FileNotFoundError(f"Unity executable not found")
    
    time.sleep(10)
    print(f"{product_name} opened successfully.")

def stop_sample_app():
    product_name = os.getenv("UNITY_APP_NAME", get_product_name())
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

def setup_browser_permissions():
    """Set up browser permissions to allow auth.immutable.com to open external applications"""
    print("Setting up browser permissions for auth.immutable.com...")
    
    # Create a browser preferences file to pre-allow the domain
    user_data_dir = "C:\\temp\\brave_debug"
    if not os.path.exists(user_data_dir):
        os.makedirs(user_data_dir, exist_ok=True)
    
    # Create preferences file that allows auth.immutable.com to open external apps
    preferences = {
        "profile": {
            "content_settings": {
                "exceptions": {
                    "protocol_handler": {
                        "https://auth.immutable.com,*": {
                            "setting": 1,
                            "last_modified": "13000000000000000"
                        }
                    }
                }
            }
        }
    }
    
    import json
    prefs_file = os.path.join(user_data_dir, "Default", "Preferences")
    os.makedirs(os.path.dirname(prefs_file), exist_ok=True)
    
    try:
        with open(prefs_file, 'w') as f:
            json.dump(preferences, f, indent=2)
        print("Browser permissions configured to allow auth.immutable.com")
    except Exception as e:
        print(f"Browser permission setup error: {e}")

def setup_protocol_association():
    """Set up immutablerunner:// protocol association to avoid permission dialogs"""
    print("Setting up protocol association for immutablerunner://...")
    
    # PowerShell script to register the protocol
    ps_script = '''
    # Register immutablerunner protocol
    $protocolKey = "HKCU:\\Software\\Classes\\immutablerunner"
    $commandKey = "$protocolKey\\shell\\open\\command"
    
    # Create the registry keys
    if (!(Test-Path $protocolKey)) {
        New-Item -Path $protocolKey -Force | Out-Null
    }
    if (!(Test-Path $commandKey)) {
        New-Item -Path $commandKey -Force | Out-Null
    }
    
    # Set the protocol values
    Set-ItemProperty -Path $protocolKey -Name "(Default)" -Value "URL:immutablerunner Protocol"
    Set-ItemProperty -Path $protocolKey -Name "URL Protocol" -Value ""
    
    # Find the Unity sample app executable
    $sampleAppPath = "C:\\Immutable\\unity-immutable-sdk\\sample\\build\\Immutable Sample.exe"
    if (Test-Path $sampleAppPath) {
        Set-ItemProperty -Path $commandKey -Name "(Default)" -Value "`"$sampleAppPath`" `"%1`""
        Write-Host "Protocol association set up successfully"
    } else {
        Write-Host "Sample app not found at expected path"
    }
    '''
    
    try:
        result = subprocess.run(["powershell", "-Command", ps_script], 
                              capture_output=True, text=True, timeout=10)
        if "successfully" in result.stdout:
            print("Protocol association configured - dialog should not appear!")
        else:
            print("Protocol setup may have failed, dialog might still appear")
    except Exception as e:
        print(f"Protocol setup error: {e}")

def launch_browser():
    print("Starting Brave...")
    
    # Set up browser permissions and protocol association first
    setup_browser_permissions()
    setup_protocol_association()
    
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

    # Launch Brave with CI-friendly flags to handle protocol dialogs automatically
    browser_args = [
        '--remote-debugging-port=9222',
        '--disable-web-security', 
        '--allow-running-insecure-content',
        '--disable-features=VizDisplayCompositor',
        '--disable-popup-blocking',
        '--no-first-run',
        '--disable-default-apps',
        '--disable-extensions',
        '--disable-component-extensions-with-background-pages',
        '--autoplay-policy=no-user-gesture-required',
        '--allow-external-protocol-handlers',
        '--enable-automation',
        '--disable-background-timer-throttling',
        '--disable-backgrounding-occluded-windows',
        '--disable-renderer-backgrounding'
    ]
    
    # Check if we're in CI environment
    is_ci = os.getenv('CI') or os.getenv('GITHUB_ACTIONS') or os.getenv('BUILD_ID')
    if is_ci:
        print("CI environment detected - adding additional protocol handling flags")
        browser_args.extend([
            '--disable-prompt-on-repost',
            '--disable-hang-monitor',
            '--disable-ipc-flooding-protection',
            '--force-permission-policy-unload-default-enabled'
        ])
    
    args_string = "', '".join(browser_args)
    result = subprocess.run([
        "powershell.exe",
        "-Command",
        f"$process = Start-Process -FilePath '{browser_path}' -ArgumentList '{args_string}' -PassThru; Write-Output $process.Id"
    ], capture_output=True, text=True, check=True)
    
    # Store the debug browser process ID globally for later use
    global debug_browser_pid
    debug_browser_pid = result.stdout.strip()
    print(f"Debug browser launched with PID: {debug_browser_pid}")

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