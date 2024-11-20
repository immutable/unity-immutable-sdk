import subprocess
import time

def open_sample_app():
    print("Opening Unity sample app...")
    subprocess.Popen(["open", "SampleApp.app"], shell=False)
    time.sleep(5)
    print("Unity sample app opened successfully.")

def stop_sample_app():
    print("Stopping sample app...")

    bash_script = """
    app_path="SampleApp.app"
    echo "Closing sample app..."
    PID=$(ps aux | grep "$app_path" | grep -v grep | awk '{print $2}')
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
    print("Stopped sample app.")

def bring_sample_app_to_foreground():
    print("Bringing Unity sample app to the foreground...")
    subprocess.run(
            ['osascript', '-e', f'tell application "SampleApp" to activate'],
            check=True
        )