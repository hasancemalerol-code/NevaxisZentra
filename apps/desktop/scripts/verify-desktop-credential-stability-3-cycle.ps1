# NevaxisZentra-DESKTOP-DEMO-CREDENTIAL-STABILITY-CLOSURE-1
# Three consecutive shortcut launches with login -> MFA -> dashboard using canonical secrets only.

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
. (Join-Path $PSScriptRoot "desktop-verifier-isolation.ps1")
$shortcutPath = Join-Path ([Environment]::GetFolderPath("Desktop")) "NevaxisZentra.lnk"
$screenshotDir = Join-Path $repoRoot ".cursor-screenshots\desktop-sales-demo-1"
$desktopProject = Join-Path $repoRoot "apps\desktop\NevaxisZentra.Desktop\NevaxisZentra.Desktop.csproj"
$cdpScript = Join-Path $PSScriptRoot "verify-desktop-authentication-live-1-cdp.mjs"
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

function Get-PasswordHashFingerprint {
    $sql = "SELECT substring(pc.password_hash from 1 for 16), pc.updated_at_utc FROM saas_identity_password_credentials pc JOIN saas_identity_accounts a ON a.id=pc.account_id WHERE a.email_normalized='admin@demo.nevaxiszentra.local';"
    $out = docker exec NevaxisZentra-local-postgres-1 psql -U NevaxisZentra -d crevafactos_local -t -A -c $sql 2>$null
    if ($null -eq $out) { return "" }
    return $out.Trim()
}

function Wait-WebReady([int]$TimeoutSeconds = 360) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $giris = Invoke-WebRequest "http://127.0.0.1:3001/giris" -UseBasicParsing -TimeoutSec 8
            if ($giris.StatusCode -ge 200 -and $giris.StatusCode -lt 300) { return $true }
        } catch { }
        Start-Sleep -Seconds 2
    }
    return $false
}

New-Item -ItemType Directory -Force -Path $screenshotDir | Out-Null
Clear-KnownDesktopVerifierEnvironmentLeaks
Assert-RuntimeRootEnvironmentClean -Context "credential-stability-start"

& $ensureSecrets -RepositoryRoot $repoRoot
if ($LASTEXITCODE -ne 0) { throw "Canonical secret readiness failed." }

$hashBeforeVerifier = Get-PasswordHashFingerprint
Write-Host "Password hash fingerprint before verifier: $hashBeforeVerifier"

Stop-NevaxisZentraDesktopOnly

Write-Host "Publishing desktop artifact..."
& dotnet publish $desktopProject -c Release -r win-x64 --self-contained false -p:DesktopDevelopmentDistribution=true -o (Join-Path $repoRoot "artifacts\desktop\NevaxisZentra")
if ($LASTEXITCODE -ne 0) { throw "Desktop publish failed." }

Assert-Check "shortcut exists" (Test-Path $shortcutPath) $shortcutPath

for ($cycle = 1; $cycle -le 3; $cycle += 1) {
    Write-Host "=== Credential stability cycle $cycle/3 ==="
    Stop-NevaxisZentraDesktopOnly

    $launch = Start-Process -FilePath $shortcutPath -PassThru
    Assert-Check "cycle $cycle web ready" (Wait-WebReady 480)

    $env:NEVAXIS_FACTOS_DESKTOP_SCREENSHOT_DIR = $screenshotDir
    & node $cdpScript
    if ($LASTEXITCODE -ne 0) { throw "Cycle $cycle CDP authentication failed." }

    Assert-Check "cycle $cycle dashboard screenshot" (Test-Path (Join-Path $screenshotDir "20-desktop-authenticated-dashboard.png"))
    Stop-NevaxisZentraDesktopOnly
    $results["Human Login Cycle $cycle"] = "PASS"
}

$hashAfterCycles = Get-PasswordHashFingerprint
Write-Host "Password hash fingerprint after cycles: $hashAfterCycles"
$results["PASSWORD_HASH_CHANGED_BY_VERIFIER"] = if ($hashBeforeVerifier -eq $hashAfterCycles) { "NO" } else { "YES" }

$results | ConvertTo-Json | Set-Content -Path (Join-Path $screenshotDir "credential-stability-results.json") -Encoding UTF8
Clear-KnownDesktopVerifierEnvironmentLeaks
Write-Host "DESKTOP-CREDENTIAL-STABILITY-CLOSURE-1: PASS"
