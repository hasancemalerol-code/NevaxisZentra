# NevaxisFactOS-DESKTOP-SHELL-RUNTIME-ROOT-TEST-LEAK-CLOSURE-1
# Proves verifier negative tests do not leak NEVAXIS_FACTOS_DESKTOP_RUNTIME_ROOT.

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
. (Join-Path $PSScriptRoot "desktop-verifier-isolation.ps1")

function Run-PollutionCheck([string]$Label) {
    $state = Get-RuntimeRootEnvironmentState
    $leaks = @()
    foreach ($scope in $state.Keys) {
        if (Test-KnownVerifierRuntimeRootLeak $state[$scope]) {
            $leaks += "$scope=$($state[$scope])"
        }
    }
    if ($leaks.Count -gt 0) {
        throw "[$Label] verifier pollution detected: $($leaks -join '; ')"
    }
    Write-Host "[$Label] pollution residual = 0 (Process/User/Machine)"
}

Clear-KnownDesktopVerifierEnvironmentLeaks
Run-PollutionCheck "pre-run"

Write-Host "Running web readiness verifier (includes negative runtime-root test)..."
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "verify-desktop-web-readiness-1.ps1")
if ($LASTEXITCODE -ne 0) { throw "verify-desktop-web-readiness-1.ps1 failed." }

Run-PollutionCheck "post-run-1"

Write-Host "Running web readiness verifier again..."
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "verify-desktop-web-readiness-1.ps1")
if ($LASTEXITCODE -ne 0) { throw "second verify-desktop-web-readiness-1.ps1 run failed." }

Run-PollutionCheck "post-run-2"
Assert-RepositoryRootValid -RepositoryRoot $repoRoot -Context "runtime-root-leak-closure"
Write-Host "RUNTIME-ROOT-TEST-LEAK-CLOSURE-1: PASS"
