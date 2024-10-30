param (
    [string]$appName
)

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class User32 {
    [DllImport("user32.dll")]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
}
"@

$hWnd = [User32]::FindWindow([NullString]::Value, $appName)
if ($hWnd -ne [IntPtr]::Zero) {
    [User32]::SetForegroundWindow($hWnd) | Out-Null
} else {
    Write-Output "Window not found"
}