# Ensures Visual Studio Build Tools (VC.Tools + Win10 SDK) are present on the runner.
# Workflow caller: .github/workflows/test-audience-sample-app.yml (Windows IL2CPP cells).

$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"

# Match Unity's detection logic: vswhere requires VC.Tools (any version), registry
# probe for any Win10 SDK at v10.0/InstallationFolder. Pinning a specific SDK
# version in -requires is too strict; VCTools ships with whatever Win10 SDK is
# current, and Unity accepts any.
function Test-Toolchain {
    $vc = if (Test-Path $vswhere) {
        & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath 2>$null
    } else { '' }
    $sdk = (Get-ItemProperty 'HKLM:\SOFTWARE\Wow6432Node\Microsoft\Microsoft SDKs\Windows\v10.0' -ErrorAction SilentlyContinue).InstallationFolder
    return @{ VcTools = $vc; Win10Sdk = $sdk }
}

$state = Test-Toolchain
if ($state.VcTools -and $state.Win10Sdk) {
    Write-Output "VC.Tools at: $($state.VcTools)"
    Write-Output "Win10 SDK at: $($state.Win10Sdk)"
    exit 0
}
Write-Output "Toolchain incomplete. VC.Tools='$($state.VcTools)' Win10Sdk='$($state.Win10Sdk)'"

Write-Output "::group::Install VS 2022 Build Tools (VCTools + Win10 SDK)"
$installer = "$env:RUNNER_TEMP\vs_BuildTools.exe"
Invoke-WebRequest -Uri 'https://aka.ms/vs/17/release/vs_BuildTools.exe' -OutFile $installer

$installArgs = @(
    '--quiet','--wait','--norestart','--nocache',
    '--add','Microsoft.VisualStudio.Workload.VCTools',
    '--add','Microsoft.VisualStudio.Component.VC.Tools.x86.x64',
    '--add','Microsoft.VisualStudio.Component.Windows10SDK.20348',
    '--includeRecommended'
)
$p = Start-Process -FilePath $installer -ArgumentList $installArgs -Wait -PassThru -NoNewWindow
# 3010 = success, reboot pending (tools are usable without reboot).
if ($p.ExitCode -ne 0 -and $p.ExitCode -ne 3010) {
    Write-Output "::error::VS Build Tools installer exited $($p.ExitCode)"
    exit $p.ExitCode
}
Write-Output "::endgroup::"

$state = Test-Toolchain
if (-not ($state.VcTools -and $state.Win10Sdk)) {
    Write-Output "::group::diagnostic"
    Write-Output "VC.Tools path (vswhere): '$($state.VcTools)'"
    Write-Output "Win10 SDK (registry v10.0/InstallationFolder): '$($state.Win10Sdk)'"
    Write-Output "--- all VS installations ---"
    if (Test-Path $vswhere) { & $vswhere -all -products * -format json }
    Write-Output "--- HKLM Win10 SDK roots ---"
    Get-ChildItem 'HKLM:\SOFTWARE\Wow6432Node\Microsoft\Microsoft SDKs\Windows' -ErrorAction SilentlyContinue | Format-List
    Write-Output "::endgroup::"
    Write-Output "::error::Install reported success but VC.Tools or Win10 SDK still not detected. Runner service account likely lacks admin to install system-wide. Install VS Build Tools manually on IMX_SDKBUILD: vs_BuildTools.exe --quiet --wait --add Microsoft.VisualStudio.Workload.VCTools --includeRecommended"
    exit 1
}
Write-Output "Verified VC.Tools at: $($state.VcTools)"
Write-Output "Verified Win10 SDK at: $($state.Win10Sdk)"
