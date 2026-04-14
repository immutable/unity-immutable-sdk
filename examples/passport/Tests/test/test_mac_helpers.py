import subprocess
import time
import os

def get_app_name():
    """Get the app name from environment variable, falling back to default"""
    return os.getenv("UNITY_APP_NAME", "SampleApp")

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