import subprocess
import time
import os
import re
from pathlib import Path
from selenium import webdriver
from selenium.webdriver.chrome.options import Options

def get_app_name():
    """Get the app name from environment variable, falling back to default"""
    return os.getenv("UNITY_APP_NAME", "SampleApp")

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

def get_logout_url_from_unity_logs():
    """Monitor Unity logs to capture logout URLs."""
    import tempfile
    
    product_name = os.getenv("UNITY_APP_NAME", get_product_name())
    
    # Unity log file locations on macOS
    log_paths = [
        os.path.join(os.path.expanduser("~"), "Library", "Logs", "Unity", product_name, "Player.log"),
        os.path.join(os.path.expanduser("~"), "Library", "Logs", product_name, "Player.log"),
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
    
    # Brave binary location on macOS
    brave_path = "/Applications/Brave Browser.app/Contents/MacOS/Brave Browser"
    chrome_options.binary_location = brave_path
    
    try:
        # Connect to the existing browser instance
        driver = webdriver.Chrome(options=chrome_options)
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
            
            # Wait for logout page to load
            time.sleep(3)
            print("Logout completed in controlled browser")
            
            # Check final page
            current_url = driver.current_url
            print(f"Final logout URL: {current_url}")
            
            # Extract the deep-link from the redirect
            # Look for immutablerunner://logout in the response or extract from returnTo parameter
            if 'returnTo=' in logout_url:
                # Extract returnTo parameter
                match = re.search(r'returnTo=([^&]+)', logout_url)
                if match:
                    from urllib.parse import unquote
                    return_to = unquote(match.group(1))
                    print(f"Extracted returnTo deep-link: {return_to}")
                    
                    # Trigger the deep-link manually
                    print(f"Triggering deep-link manually: {return_to}")
                    subprocess.run(['open', return_to], check=False)
                    time.sleep(2)
            
        else:
            print("Could not find logout URL in Unity logs - logout may complete without browser interaction")
        
    except Exception as e:
        print(f"Error during controlled logout: {e}")
        print("Logout may need to be handled by Unity directly")
    
    print("Controlled logout process completed")

def open_sample_app():
    app_name = get_app_name()
    print(f"Opening Unity sample app ({app_name})...")
    subprocess.Popen(["open", f"{app_name}.app"], shell=False)
    time.sleep(5)
    print(f"Unity sample app ({app_name}) opened successfully.")

def stop_sample_app():
    app_name = get_app_name()
    print(f"Stopping sample app ({app_name})...")

    bash_script = f"""
    app_path="{app_name}.app"
    echo "Closing sample app..."
    PID=$(ps aux | grep "$app_path" | grep -v grep | awk '{{print $2}}')
    if [ -n "$PID" ]; then
        kill $PID
        echo "Sample app (PID $PID) has been terminated."
    else
        echo "Sample app is not running."
    fi
    echo "Waiting for 5 seconds..."
    sleep 5
    """

    subprocess.run(bash_script, shell=True, check=True, text=True)

    time.sleep(5)
    print(f"Stopped sample app ({app_name}).")

def bring_sample_app_to_foreground():
    app_name = get_app_name()
    print(f"Bringing Unity sample app ({app_name}) to the foreground...")
    subprocess.run(
            ['osascript', '-e', f'tell application "{app_name}" to activate'],
            check=True
        )