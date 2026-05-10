# Runs the audience PlayMode tests on Windows. Captures Player.log into artifacts/.
# Surfaces Unity compile errors as ::error:: annotations.
# Workflow caller: .github/workflows/test-audience-sample-app.yml (playmode job).
#
# Inputs (env): UNITY_PATH (set by install-unity-windows.ps1), TARGET.

$ErrorActionPreference = 'Continue'
$logFile = "$pwd\artifacts\unity.log"
$resultsFile = "$pwd\artifacts\test-results.xml"

New-Item -ItemType Directory -Force -Path artifacts | Out-Null

$unityArgs = @(
    '-batchmode','-nographics',
    '-projectPath','examples/audience',
    '-runTests',
    '-testPlatform',$env:TARGET,
    '-testResults',$resultsFile,
    '-logFile',$logFile
)
Write-Output "Launching Unity: $env:UNITY_PATH $($unityArgs -join ' ')"
$p = Start-Process -FilePath $env:UNITY_PATH -ArgumentList $unityArgs -Wait -PassThru -NoNewWindow
Write-Output "::group::Unity log"
Get-Content $logFile -ErrorAction SilentlyContinue | Write-Output
Write-Output "::endgroup::"
Write-Output "Unity exited with code $($p.ExitCode)"

# Copy Player.log files into artifacts so HTTP traces and OnError fires survive.
$src = "$env:USERPROFILE\AppData\LocalLow"
if (Test-Path $src) {
    Get-ChildItem -Path $src -Recurse -Filter "Player.log" -ErrorAction SilentlyContinue |
        ForEach-Object {
            $name = $_.Directory.Name
            Copy-Item -Path $_.FullName -Destination "artifacts/Player-$name.log" -ErrorAction SilentlyContinue
        }
}

# Promote Unity compile errors to ::error:: annotations. Sanitize '::' so log
# lines containing workflow commands cannot terminate the annotation early.
if (Test-Path $logFile) {
    Get-Content $logFile |
        Select-String -Pattern '(error CS\d+:|Compilation failed:)' |
        ForEach-Object { $_.Line.Trim() } |
        Sort-Object -Unique |
        ForEach-Object {
            $sanitized = $_ -replace '::', '%3A%3A'
            Write-Output "::error::$sanitized"
        }
}

exit $p.ExitCode
