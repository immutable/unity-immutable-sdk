# Installs the Unity editor and (for IL2CPP cells) the windows-il2cpp module.
# Idempotent. Sets UNITY_PATH in GITHUB_ENV so the playmode step picks it up.
# Workflow caller: .github/workflows/test-audience-sample-app.yml (playmode job).
#
# Inputs (env): UNITY_VERSION, UNITY_CHANGESET, BACKEND.

$ErrorActionPreference = 'Continue'
$hub = "C:\Program Files\Unity Hub\Unity Hub.exe"

Write-Output "::group::install editor"
& $hub -- --headless install --version $env:UNITY_VERSION --changeset $env:UNITY_CHANGESET --architecture x86_64 2>&1 | Write-Output
if ($LASTEXITCODE -ne 0) { Write-Output "(install non-zero, OK if 'Editor already installed in this location')" }
$global:LASTEXITCODE = 0
Write-Output "::endgroup::"

if ($env:BACKEND -eq 'IL2CPP') {
    Write-Output "::group::install windows-il2cpp module"
    & $hub -- --headless install-modules --version $env:UNITY_VERSION --changeset $env:UNITY_CHANGESET --architecture x86_64 --module windows-il2cpp 2>&1 | Write-Output
    if ($LASTEXITCODE -ne 0) { Write-Output "(install-modules non-zero, OK if 'No modules found to install')" }
    $global:LASTEXITCODE = 0
    Write-Output "::endgroup::"
}

$editor = "C:\Program Files\Unity\Hub\Editor\$env:UNITY_VERSION\Editor\Unity.exe"
$il2cpp = "C:\Program Files\Unity\Hub\Editor\$env:UNITY_VERSION\Editor\Data\PlaybackEngines\windowsstandalonesupport\Variations\win64_player_nondevelopment_il2cpp"
$missing = @()
if (-not (Test-Path $editor)) { $missing += 'editor' }
if ($env:BACKEND -eq 'IL2CPP' -and -not (Test-Path $il2cpp)) { $missing += 'windows-il2cpp' }
if ($missing.Count -gt 0) {
    Write-Output "::error::Unity $env:UNITY_VERSION missing: $($missing -join '+')"
    Get-ChildItem "C:\Program Files\Unity\Hub\Editor\" -ErrorAction SilentlyContinue | Format-Table
    & $hub -- --headless editors --installed
    exit 1
}

Write-Output "Found Unity:  $editor"
if ($env:BACKEND -eq 'IL2CPP') { Write-Output "Found IL2CPP: $il2cpp" }

"UNITY_PATH=$editor" | Out-File -FilePath $env:GITHUB_ENV -Append -Encoding utf8
