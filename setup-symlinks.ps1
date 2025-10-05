# PowerShell script for Windows to create symlinks
# Requires Developer Mode enabled OR running PowerShell as Administrator

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sampleUnity6Assets = Join-Path $scriptDir "sample-unity6\Assets"
$sampleUnity6 = Join-Path $scriptDir "sample-unity6"

Write-Output "Setting up symlinks for sample-unity6..."

# Check if running with sufficient privileges
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Output "WARNING: Not running as Administrator."
    Write-Output "If symlink creation fails, either:"
    Write-Output "  1. Enable Developer Mode (Settings > Update & Security > For developers)"
    Write-Output "  2. Run PowerShell as Administrator"
    Write-Output ""
}

# Remove existing directories/symlinks if they exist in Assets
Set-Location $sampleUnity6Assets

if (Test-Path "Scenes") { Remove-Item -Path "Scenes" -Recurse -Force }
if (Test-Path "Scripts") { Remove-Item -Path "Scripts" -Recurse -Force }
if (Test-Path "Editor") { Remove-Item -Path "Editor" -Recurse -Force }
if (Test-Path "Scenes.meta") { Remove-Item -Path "Scenes.meta" -Force }
if (Test-Path "Scripts.meta") { Remove-Item -Path "Scripts.meta" -Force }
if (Test-Path "Editor.meta") { Remove-Item -Path "Editor.meta" -Force }

# Create symlinks for Assets
try {
    New-Item -ItemType SymbolicLink -Path "Scenes" -Target "..\..\sample\Assets\Scenes" | Out-Null
    New-Item -ItemType SymbolicLink -Path "Scripts" -Target "..\..\sample\Assets\Scripts" | Out-Null
    New-Item -ItemType SymbolicLink -Path "Editor" -Target "..\..\sample\Assets\Editor" | Out-Null
    New-Item -ItemType SymbolicLink -Path "Scenes.meta" -Target "..\..\sample\Assets\Scenes.meta" | Out-Null
    New-Item -ItemType SymbolicLink -Path "Scripts.meta" -Target "..\..\sample\Assets\Scripts.meta" | Out-Null
    New-Item -ItemType SymbolicLink -Path "Editor.meta" -Target "..\..\sample\Assets\Editor.meta" | Out-Null

    Write-Output ""
    Write-Output "✅ Asset symlinks created successfully!"
    Write-Output ""
    Write-Output "Scenes, Scripts, and Editor in sample-unity6 now point to sample/Assets"
    Get-ChildItem | Where-Object { $_.Name -match "Scenes|Scripts|Editor" } | Format-Table Name, LinkType, Target

    # Create symlink for Tests
    Set-Location $sampleUnity6
    if (Test-Path "Tests") { Remove-Item -Path "Tests" -Recurse -Force }
    New-Item -ItemType SymbolicLink -Path "Tests" -Target "..\sample\Tests" | Out-Null

    Write-Output ""
    Write-Output "✅ Tests symlink created successfully!"
    Write-Output "Tests in sample-unity6 now points to sample/Tests"
    Get-ChildItem | Where-Object { $_.Name -eq "Tests" } | Format-Table Name, LinkType, Target
}
catch {
    Write-Output ""
    Write-Output "❌ Failed to create symlinks!"
    Write-Output "Error: $_"
    Write-Output ""
    Write-Output "Please enable Developer Mode:"
    Write-Output "  Settings > Update & Security > For developers > Developer Mode: ON"
    Write-Output "Then run this script again."
    exit 1
}

