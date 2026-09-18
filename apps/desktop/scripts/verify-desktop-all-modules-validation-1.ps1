# NevaxisZentra-DESKTOP-SHELL-ALL-MODULES-VALIDATION-1
# Cold-starts real Desktop Shell shortcut and validates all 46 canonical sales-demo routes in WebView2.

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
. (Join-Path $PSScriptRoot "desktop-verifier-isolation.ps1")

$shortcutPath = Join-Path ([Environment]::GetFolderPath("Desktop")) "NevaxisZentra.lnk"
$screenshotDir = Join-Path $repoRoot ".cursor-screenshots\desktop-all-modules-validation-1"
$desktopProject = Join-Path $repoRoot "apps\desktop\NevaxisZentra.Desktop\NevaxisZentra.Desktop.csproj"
$manifestScript = Join-Path $PSScriptRoot "generate-desktop-all-modules-validation-manifest-1.mjs"
$cdpScript = Join-Path $PSScriptRoot "verify-desktop-all-modules-validation-1-cdp.mjs"
$ensureSecrets = Join-Path $repoRoot "scripts\desktop\ensure-development-saas-identity-secrets.ps1"
$results = [ordered]@{}

function Assert-Check([string]$Name, [bool]$Ok, [string]$Detail = "") {
    $results[$Name] = if ($Ok) { "PASS" } else { "FAIL" }
    Write-Host "$(if ($Ok) { 'PASS' } else { 'FAIL' }) $Name$(if ($Detail) { " - $Detail" } else { '' })"
    if (-not $Ok) { throw "$Name failed$(if ($Detail) { ": $Detail" } else { '' })" }
}

function Stop-NevaxisZentraDesktopOnly {
    Get-Process -Name "crevafactos", "NevaxisZentra.Desktop" -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 4
}

function Wait-DesktopWebReady([int]$TimeoutSeconds = 480) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $giris = Invoke-WebRequest "http://127.0.0.1:3001/giris" -UseBasicParsing -TimeoutSec 8
            if ($giris.StatusCode -ge 200 -and $giris.StatusCode -lt 500) { return $true }
        } catch { }
        Start-Sleep -Seconds 2
    }
    return $false
}

function Test-DashboardProtectedDiff {
    $paths = @(
        "apps/web/src/app/(app)/dashboard",
        "apps/web/src/components/dashboard",
        "apps/web/src/features/dashboard",
        "apps/web/src/app/command-center.css"
    )
    foreach ($path in $paths) {
        $diff = git -C $repoRoot diff --stat -- $path 2>$null
        if ($diff) { return $false }
    }
    return $true
}

$startingHead = (git -C $repoRoot rev-parse HEAD).Trim()
Write-Host "Starting HEAD: $startingHead"
Assert-Check "dashboard protected diff (before)" (Test-DashboardProtectedDiff)

New-Item -ItemType Directory -Force -Path $screenshotDir | Out-Null
Clear-KnownDesktopVerifierEnvironmentLeaks
Assert-RuntimeRootEnvironmentClean -Context "all-modules-start"

& $ensureSecrets -RepositoryRoot $repoRoot
if ($LASTEXITCODE -ne 0) { throw "Canonical secret readiness failed." }

& node $manifestScript
if ($LASTEXITCODE -ne 0) { throw "Manifest generation failed." }

Assert-Check "shortcut exists" (Test-Path $shortcutPath) $shortcutPath

Write-Host "Publishing desktop artifact (if not locked)..."
& dotnet publish $desktopProject -c Release -r win-x64 --self-contained false -p:DesktopDevelopmentDistribution=true -o (Join-Path $repoRoot "artifacts\desktop\NevaxisZentra") 2>&1 | Out-Null

Stop-NevaxisZentraDesktopOnly
Write-Host "Cold-starting Desktop Shell from shortcut..."
$launch = Start-Process -FilePath $shortcutPath -PassThru
Assert-Check "desktop web ready after cold start" (Wait-DesktopWebReady 480)

$env:NEVAXIS_FACTOS_DESKTOP_ALL_MODULES_SCREENSHOT_DIR = $screenshotDir
Write-Host "Running WebView2 all-modules CDP validation..."
& node $cdpScript
if ($LASTEXITCODE -ne 0) { throw "Desktop all-modules CDP validation failed." }

$validationJson = Join-Path $screenshotDir "validation-results.json"
Assert-Check "validation results json" (Test-Path $validationJson)
$fullPayload = Get-Content $validationJson -Raw | ConvertFrom-Json
Assert-Check "canonical route count" ($fullPayload.canonicalRouteCount -eq 46) "count=$($fullPayload.canonicalRouteCount)"
Assert-Check "all routes pass" ($fullPayload.passCount -eq 46 -and $fullPayload.failCount -eq 0) "pass=$($fullPayload.passCount) fail=$($fullPayload.failCount)"
Assert-Check "user-facing console errors" ($fullPayload.userFacingConsoleErrors -eq 0) "errors=$($fullPayload.userFacingConsoleErrors)"

# Final cold restart representative check
Stop-NevaxisZentraDesktopOnly
$relaunch = Start-Process -FilePath $shortcutPath -PassThru
Assert-Check "cold restart web ready" (Wait-DesktopWebReady 360)
$env:NEVAXIS_FACTOS_DESKTOP_ALL_MODULES_REPRESENTATIVE_ONLY = "1"
& node $cdpScript
if ($LASTEXITCODE -ne 0) { throw "Post-restart representative validation failed." }
Remove-Item Env:NEVAXIS_FACTOS_DESKTOP_ALL_MODULES_REPRESENTATIVE_ONLY -ErrorAction SilentlyContinue

$payload = Get-Content $validationJson -Raw | ConvertFrom-Json
Assert-Check "post-restart representative pass" ($payload.failCount -eq 0)

Assert-Check "dashboard protected diff (after)" (Test-DashboardProtectedDiff)

$results["startingHead"] = $startingHead
$results["canonicalRouteCount"] = 46
$results["passCount"] = $fullPayload.passCount
$results["failCount"] = $fullPayload.failCount
$results | ConvertTo-Json | Set-Content -Path (Join-Path $screenshotDir "validation-run-summary.json") -Encoding UTF8

Stop-NevaxisZentraDesktopOnly
Clear-KnownDesktopVerifierEnvironmentLeaks
Write-Host "DESKTOP-ALL-MODULES-VALIDATION-1: PASS"
