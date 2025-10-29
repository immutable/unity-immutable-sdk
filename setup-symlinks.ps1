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

# Create junctions for directories (Unity on Windows needs junctions, not symlinks)
# Get absolute paths for the targets
$samplesDir = Join-Path $scriptDir "sample"
$scenesTarget = Join-Path $samplesDir "Assets\Scenes"
$scriptsTarget = Join-Path $samplesDir "Assets\Scripts"
$editorTarget = Join-Path $samplesDir "Assets\Editor"
$scenesMetaTarget = Join-Path $samplesDir "Assets\Scenes.meta"
$scriptsMetaTarget = Join-Path $samplesDir "Assets\Scripts.meta"
$editorMetaTarget = Join-Path $samplesDir "Assets\Editor.meta"

try {
    # Create directory symbolic links (Unity recognizes these on Windows)
    # Note: Requires administrator privileges
    cmd /c mklink /D "Scenes" "$scenesTarget" | Out-Null
    cmd /c mklink /D "Scripts" "$scriptsTarget" | Out-Null
    cmd /c mklink /D "Editor" "$editorTarget" | Out-Null

    # Create file symbolic links for .meta files
    cmd /c mklink "Scenes.meta" "$scenesMetaTarget" | Out-Null
    cmd /c mklink "Scripts.meta" "$scriptsMetaTarget" | Out-Null
    cmd /c mklink "Editor.meta" "$editorMetaTarget" | Out-Null

    Write-Output ""
    Write-Output "✅ Asset symlinks created successfully!"
    Write-Output ""
    Write-Output "Scenes, Scripts, and Editor in sample-unity6 now point to sample/Assets"
    Get-ChildItem | Where-Object { $_.Name -match "Scenes|Scripts|Editor" } | Format-Table Name, LinkType, Target

    # Create directory symbolic link for Tests
    Set-Location $sampleUnity6
    if (Test-Path "Tests") { Remove-Item -Path "Tests" -Recurse -Force }
    $testsTarget = Join-Path $samplesDir "Tests"
    cmd /c mklink /D "Tests" "$testsTarget" | Out-Null

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
