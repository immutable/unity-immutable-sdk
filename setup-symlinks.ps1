---
# PowerShell script for Windows to create symlinks
# Requires Developer Mode enabled OR running PowerShell as Administrator

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sampleUnity6Assets = Join-Path $scriptDir "sample-unity6\Assets"

Write-Host "Setting up symlinks for sample-unity6..." -ForegroundColor Cyan

# Check if running with sufficient privileges
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "WARNING: Not running as Administrator." -ForegroundColor Yellow
    Write-Host "If symlink creation fails, either:" -ForegroundColor Yellow
    Write-Host "  1. Enable Developer Mode (Settings > Update & Security > For developers)" -ForegroundColor Yellow
    Write-Host "  2. Run PowerShell as Administrator" -ForegroundColor Yellow
    Write-Host ""
}

# Remove existing directories/symlinks if they exist
Set-Location $sampleUnity6Assets

if (Test-Path "Scenes") { Remove-Item -Path "Scenes" -Recurse -Force }
if (Test-Path "Scripts") { Remove-Item -Path "Scripts" -Recurse -Force }
if (Test-Path "Scenes.meta") { Remove-Item -Path "Scenes.meta" -Force }
if (Test-Path "Scripts.meta") { Remove-Item -Path "Scripts.meta" -Force }

# Create symlinks
try {
    New-Item -ItemType SymbolicLink -Path "Scenes" -Target "..\..\sample\Assets\Scenes" | Out-Null
    New-Item -ItemType SymbolicLink -Path "Scripts" -Target "..\..\sample\Assets\Scripts" | Out-Null
    New-Item -ItemType SymbolicLink -Path "Scenes.meta" -Target "..\..\sample\Assets\Scenes.meta" | Out-Null
    New-Item -ItemType SymbolicLink -Path "Scripts.meta" -Target "..\..\sample\Assets\Scripts.meta" | Out-Null
    
    Write-Host ""
    Write-Host "✅ Symlinks created successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Scenes and Scripts in sample-unity6 now point to sample/Assets" -ForegroundColor Green
    Get-ChildItem | Where-Object { $_.Name -match "Scenes|Scripts" } | Format-Table Name, LinkType, Target
}
catch {
    Write-Host ""
    Write-Host "❌ Failed to create symlinks!" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please enable Developer Mode:" -ForegroundColor Yellow
    Write-Host "  Settings > Update & Security > For developers > Developer Mode: ON" -ForegroundColor Yellow
    Write-Host "Then run this script again." -ForegroundColor Yellow
    exit 1
}

