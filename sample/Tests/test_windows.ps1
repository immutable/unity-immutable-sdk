# Function to stop the Unity sample app if it's running
function Stop-SampleApp {
    $process = Get-Process -Name "SampleApp" -ErrorAction SilentlyContinue
    if ($process) {
        Stop-Process -Id $process.Id
        Write-Output "SampleApp.exe has been closed."
    } else {
        Write-Output "SampleApp.exe is not running."
    }
    Start-Sleep -Seconds 5
}

# Function to start the Unity sample app
function Start-SampleApp {
    Write-Output "Starting Unity sample app..."
    Start-Process -FilePath "SampleApp.exe"
    Start-Sleep -Seconds 10
}

# Function to bring the Unity sample app to the foreground
function Bring-SampleAppToForeground {
    $POWERSHELL_SCRIPT_PATH = "./switch-app.ps1"
    Write-Output "Bringing Unity sample app to the foreground..."
    powershell.exe -Command "Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process; & '$POWERSHELL_SCRIPT_PATH' -appName 'Immutable Sample'"
}

# Function to run pytest tests
function Run-Pytest {
    param (
        [string]$testFile
    )
    Write-Output "Running pytest for $testFile..."
    Start-Process -FilePath "pytest" -ArgumentList $testFile -NoNewWindow -PassThru | Wait-Process
}

# Function to stop Chrome if it's running
function Stop-Chrome {
    Write-Output "Stopping Chrome.."
    $process = Get-Process -Name "chrome" -ErrorAction SilentlyContinue
    if ($process) {
        $process | ForEach-Object {
            Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
        }
        Write-Output "All Chrome processes have been closed."
    } else {
        Write-Output "Chrome is not running."
    }
    
    Start-Sleep -Seconds 10
}

# Login
function Login {
    param (
        [string]$testFile
    )
    # Start Chrome for remote debugging
    Write-Output "Starting Chrome..."
    
    $chromePath = "C:\Program Files\Google\Chrome\Application\chrome.exe"

    if (-not (Test-Path $chromePath)) {
        $chromePath = "C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"
    }

    if (-not (Test-Path $chromePath)) {
        Write-Output "Chrome executable not found."
        exit
    }

    Start-Process -FilePath $chromePath -ArgumentList "--remote-debugging-port=9222"

    # Run Python script for login
    Write-Output "Running python script to login..."
    $pythonProcess = Start-Process -FilePath "python" -ArgumentList "src/device_code_login_windows.py" -NoNewWindow -PassThru
    Write-Output "Python script running in the background..."

    Start-Sleep -Seconds 5

    Bring-SampleAppToForeground

    Write-Output "Running login test..."
    $pytestProcess = Start-Process -FilePath "pytest" -ArgumentList $testFile -NoNewWindow -PassThru

    $pythonProcess | Wait-Process

    Bring-SampleAppToForeground

    $pytestProcess | Wait-Process

    Stop-Chrome
}

# Logout
function Logout {
    # Start Chrome for remote debugging
    Write-Output "Starting Chrome..."
    $chromePath = (Get-Command chrome.exe).Source
    Start-Process -FilePath $chromePath -ArgumentList "--remote-debugging-port=9222"

    Write-Output "Running python script to logout..."
    $pythonProcess = Start-Process -FilePath "python" -ArgumentList "src/device_code_logout_windows.py" -NoNewWindow -PassThru
    Start-Sleep -Seconds 5

    Bring-SampleAppToForeground

    Write-Output "Running logout test..."
    $pytestProcess = Start-Process -FilePath "pytest" -ArgumentList "test/test_mac_device_code_logout.py" -NoNewWindow -PassThru

    $pythonProcess | Wait-Process

    Bring-SampleAppToForeground

    $pytestProcess | Wait-Process

    Stop-Chrome
}

# Capture the start time
$startTime = Get-Date

# Start Unity sample app
Start-SampleApp

# Login
Login "test/test_windows.py::WindowsTest::test_1_device_code_login"

# Run IMX and zkEVM tests
Run-Pytest "test/test.py"
if (-not $?) {
    Write-Output "Tests failed. Stopping execution."
    exit 1
}

# Relogin
Stop-SampleApp
Start-SampleApp
Run-Pytest "test/test_windows.py::WindowsTest::test_3_device_code_relogin"
if (-not $?) {
    Write-Output "Relogin test failed. Stopping execution."
    exit 1
}

# Reconnect
Stop-SampleApp
Start-SampleApp
Run-Pytest "test/test_windows.py::WindowsTest::test_4_device_code_reconnect"
if (-not $?) {
    Write-Output "Reconnect test failed. Stopping execution."
    exit 1
}

# Logout
Logout

# Connect IMX
Stop-SampleApp
Start-SampleApp
Write-Output "Connect to IMX..."
Login "test/test_windows.py::WindowsTest::test_2_device_code_connect_imx"

# Bring the Unity sample app to the foreground
Bring-SampleAppToForeground

# Logout
Logout

# Final stop of Unity sample app
Stop-SampleApp

# Capture the end time
$endTime = Get-Date

# Calculate and display the elapsed time
$elapsedTime = $endTime - $startTime
Write-Output "All tests completed."
Write-Output "Elapsed time: $($elapsedTime.TotalMinutes) minutes"