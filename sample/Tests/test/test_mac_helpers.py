import subprocess
import time
import os
import re
from pathlib import Path

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

def get_company_name():
    """Get the company name from ProjectSettings.asset (used for log paths on some macOS setups)."""
    project_settings_path = Path(__file__).resolve().parent.parent.parent / 'ProjectSettings' / 'ProjectSettings.asset'

    if not project_settings_path.exists():
        print(f"Warning: ProjectSettings.asset not found at {project_settings_path}")
        return None

    try:
        with open(project_settings_path, 'r') as f:
            content = f.read()
        match = re.search(r'companyName: (.+)', content)
        if match:
            return match.group(1).strip()
    except Exception as e:
        print(f"Warning: failed to read companyName from ProjectSettings.asset: {e}")
    return None

def get_logout_url_from_unity_logs():
    """Monitor Unity logs to capture logout URLs."""
    import tempfile
    from pathlib import Path as _Path
    
    product_name = os.getenv("UNITY_APP_NAME", get_product_name())
    company_name = get_company_name()
    
    # Unity log file locations on macOS
    log_paths = [
        os.path.join(os.path.expanduser("~"), "Library", "Logs", "Unity", product_name, "Player.log"),
        os.path.join(os.path.expanduser("~"), "Library", "Logs", product_name, "Player.log"),
        # Common standalone player paths
        os.path.join(os.path.expanduser("~"), "Library", "Logs", "Unity", "Player.log"),
        os.path.join(os.path.expanduser("~"), "Library", "Logs", "Unity", product_name, "Player.log"),
        os.path.join(os.path.expanduser("~"), "Library", "Logs", "Unity", "Player.log"),
        os.path.join(tempfile.gettempdir(), "UnityPlayer.log"),
        "Player.log"  # Current directory
    ]

    if company_name:
        log_paths.insert(
            0,
            os.path.join(os.path.expanduser("~"), "Library", "Logs", company_name, product_name, "Player.log"),
        )
    
    # De-dup while preserving order
    seen = set()
    log_paths = [p for p in log_paths if not (p in seen or seen.add(p))]

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

    # Fallback: scan ~/Library/Logs for any Player.log and try the most recently modified few.
    # This helps on standalone runners where Unity logs may be under Company/Product folders.
    try:
        logs_root = _Path(os.path.expanduser("~")) / "Library" / "Logs"
        if logs_root.exists():
            candidates = []
            for p in logs_root.rglob("Player.log"):
                try:
                    candidates.append((p.stat().st_mtime, str(p)))
                except Exception:
                    continue

            # Newest first; try a small number to avoid slow scans.
            candidates.sort(reverse=True)
            for _, p in candidates[:10]:
                if p in seen:
                    continue
                print(f"Monitoring Unity log for logout URL (fallback): {p}")
                try:
                    with open(p, 'r', encoding='utf-8', errors='ignore') as f:
                        content = f.read()
                        matches = re.findall(r'(?:\[Immutable\] PASSPORT_AUTH_URL: |PASSPORT_AUTH_URL: |LaunchAuthURL : )(https?://[^\s]+)', content)
                        if matches:
                            for url in reversed(matches):
                                if 'logout' in url or 'im-logged-out' in url:
                                    print(f"Found logout URL: {url}")
                                    return url
                except Exception as e:
                    print(f"Error reading log file {p}: {e}")
                    continue
    except Exception as e:
        print(f"Warning: fallback Player.log scan failed: {e}")
    
    print("No logout URL found in Unity logs")
    return None

def logout_with_controlled_browser():
    """Handle logout without relying on Selenium/ChromeDriver.

    The Unity sample app already opens the logout URL in the system browser when LogoutBtn is tapped.
    Here we monitor Unity logs to capture that logout URL, extract its `returnTo` deep-link, and
    trigger it via `open` so Unity receives the callback deterministically.
    """
    print("Starting controlled logout process...")

    try:
        # Monitor Unity logs for logout URL
        print("Monitoring Unity logs for logout URL...")
        logout_url = None
        for attempt in range(15):  # Try for 15 seconds (shorter timeout)
            logout_url = get_logout_url_from_unity_logs()
            if logout_url:
                break
            time.sleep(1)
        
        if logout_url:
            print(f"Captured logout URL from Unity logs: {logout_url}")

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
                    print("Warning: returnTo parameter present but could not be parsed.")
            else:
                print("Warning: logout URL did not include returnTo; cannot trigger deep-link callback.")
            
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