#!/bin/bash

# Function to open the sample app
open_sample_app() {
    local app_path="$1"
    echo "Opening Unity sample app..."
    open "$app_path"
    echo "Unity sample app launched. Waiting for 5 seconds..."
    sleep 5
}

# Function to close the sample app
close_sample_app() {
    local app_path="$1"
    echo "Closing sample app..."
    local PID=$(ps aux | grep "$app_path" | grep -v grep | awk '{print $2}')
    if [ -n "$PID" ]; then
        kill $PID
        echo "Sample app (PID $PID) has been terminated."
    else
        echo "Sample app is not running."
    fi
    echo "Waiting for 5 seconds..."
    sleep 5
}

# Function to run Python scripts in the background
run_python_script() {
    local script_path="$1"
    echo "Running $script_path script..."
    python3 "$script_path" &
    echo "$script_path script running in the background..."
}

# Function to bring the sample app to the foreground
activate_sample_app() {
    local app_name="$1"
    echo "Bringing Unity sample app to the foreground..."
    osascript -e "tell application \"$app_name\" to activate"
}

close_chrome() {
    echo "Closing all Chrome instances..."
    pkill -f chrome
    if [ $? -eq 0 ]; then
    echo "Chrome closed successfully."
    else
    echo "No Chrome instances were running."
    fi
}

# Main script execution
app_path="${UNITY_APP_PATH:-SampleApp.app}"
app_name="${UNITY_APP_NAME:-SampleApp}"

# Capture the start time
start_time=$(date +%s)

# Set permissions for the app bundle
chmod -R 755 "$app_path"

echo "Starting Unity sample app..."
open_sample_app "$app_path"

# Login
run_python_script "src/device_code_login.py"
sleep 5
activate_sample_app "$app_name"
echo "Running Mac device code login test..."
pytest test/test_mac.py::MacTest::test_1_device_code_login
wait
close_chrome

# SDK functions
echo "Running SDK functions tests..."
activate_sample_app "$app_name"
pytest test/test.py
wait

# Relogin
close_sample_app "$app_path"
open_sample_app "$app_path"
echo "Running Mac relogin test..."
pytest test/test_mac.py::MacTest::test_3_device_code_relogin
wait

# Reconnect
close_sample_app "$app_path"
open_sample_app "$app_path"
echo "Running Mac reconnect test..."
pytest test/test_mac.py::MacTest::test_4_device_code_reconnect
wait

# Logout
run_python_script "src/device_code_logout.py"
sleep 5
activate_sample_app "$app_name"
echo "Running Mac device code logout test..."
pytest test/test_mac_device_code_logout.py
wait
close_chrome

# Connect IMX
close_sample_app "$app_path"
open_sample_app "$app_path"
run_python_script "src/device_code_login.py"
sleep 5
activate_sample_app "$app_name"
echo "Running Mac device code connect IMX test..."
pytest test/test_mac.py::MacTest::test_2_device_code_connect_imx
wait
close_chrome

activate_sample_app "$app_name"

# Logout
run_python_script "src/device_code_logout.py"
sleep 5
activate_sample_app "$app_name"
echo "Running Mac device code logout test..."
pytest test/test_mac_device_code_logout.py
wait
close_chrome

# Final stop of Unity sample app
close_sample_app "$app_path"

# Capture the end time
end_time=$(date +%s)

# Calculate the duration
execution_time=$((end_time - start_time))
minutes=$((execution_time / 60))
seconds=$((execution_time % 60))

echo "All tests completed."
echo "Elapsed time: $minutes minutes and $seconds seconds."