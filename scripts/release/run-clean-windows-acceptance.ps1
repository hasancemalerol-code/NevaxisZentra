# Nevaxis FactOS — Clean Windows Release Acceptance Procedure
# Run on an isolated Windows VM or physical PC with ONLY customer release artifacts.
#
# RULE: The acceptance machine must NOT have the repository, dev tools, or manual file copies.
# INPUT: Nevaxis-FactOS-Setup.msi (or official release ZIP)
#
# Usage (on clean Windows machine):
#   1. Copy only Nevaxis-FactOS-Setup-<version>.msi to the machine
#   2. Run: msiexec /i Nevaxis-FactOS-Setup-<version>.msi /L*v C:\Temp\factos-install.log
#   3. Launch FactOS from Start Menu
#   4. Run this script AFTER install to record evidence (optional helper)

[CmdletBinding()]
param(
    [string]$ReportPath = "",
    [string]$ExpectedMsiSha256 = "",
    [string]$InstallerPath = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ReportPath)) {
    $ReportPath = Join-Path $env:TEMP "nevaxis-factos-clean-acceptance-report.json"
}

$stages = @(
    @{ name = "clean-windows-baseline"; manual = $true; instruction = "Fresh Windows 11/10 x64 VM snapshot with no dev tooling" },
    @{ name = "installer-only"; manual = $true; instruction = "Only MSI/ZIP copied; no repo or dev dependencies" },
    @{ name = "install"; manual = $true; instruction = "msiexec /i Nevaxis-FactOS-Setup.msi /L*v install.log" },
    @{ name = "first-launch"; manual = $true; instruction = "Launch from Start Menu; window must appear" },
    @{ name = "runtime-health"; manual = $true; instruction = "Check C:\ProgramData\Nevaxis\FactOS\Logs\startup.log" },
    @{ name = "login"; manual = $true; instruction = "Sign in with demo credentials from README-DEMO.txt" },
    @{ name = "core-flow"; manual = $true; instruction = "Dashboard, Dijital Is Gucu, AI Gorev Merkezi, Fabrika Karar, Fabrika Zekasi, Entegrasyon" },
    @{ name = "close-relaunch"; manual = $true; instruction = "Close app, wait 30s, relaunch; no duplicate processes" },
    @{ name = "reboot-relaunch"; manual = $true; instruction = "Reboot Windows, launch FactOS without manual intervention" },
    @{ name = "backup-restore"; manual = $true; instruction = "Create backup, modify data, restore, verify" },
    @{ name = "upgrade"; manual = $true; instruction = "Install previous version, create data, upgrade MSI" },
    @{ name = "security-scan"; manual = $false; instruction = "Run scan-release-artifacts.ps1 on release bundle before distribution" }
)

$report = [ordered]@{
    product = "Nevaxis FactOS"
    procedure = "clean-windows-acceptance"
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    machineName = $env:COMPUTERNAME
    osVersion = [System.Environment]::OSVersion.VersionString
    stages = @()
    customerReleaseStatus = "PENDING"
}

foreach ($stage in $stages) {
    $entry = [ordered]@{
        name = $stage.name
        status = if ($stage.manual) { "PENDING_MANUAL" } else { "NOT_RUN" }
        instruction = $stage.instruction
        timestampUtc = (Get-Date).ToUniversalTime().ToString("o")
    }
    $report.stages += $entry
}

# Automated checks available on acceptance machine
$installDir = "${env:ProgramFiles}\Nevaxis FactOS"
$desktopExe = Join-Path $installDir "desktop\NevaxisFactOS.exe"
$manifestPath = Join-Path $installDir "manifests\runtime-manifest.json"
$dataRoot = "${env:ProgramData}\Nevaxis\FactOS"

$autoStage = [ordered]@{ name = "automated-install-checks"; status = "PASS"; details = @() }

if (-not (Test-Path $desktopExe)) {
    $autoStage.status = "FAIL"
    $autoStage.details += "Missing desktop exe: $desktopExe"
}
else {
    $autoStage.details += "desktop-exe=present"
}

if (-not (Test-Path $manifestPath)) {
    $autoStage.status = "FAIL"
    $autoStage.details += "Missing runtime manifest"
}
else {
    $autoStage.details += "runtime-manifest=present"
    try {
        $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
        $autoStage.details += "deploymentChannel=$($manifest.deploymentChannel)"
        $autoStage.details += "sourceCommit=$($manifest.sourceCommit)"
    }
    catch {
        $autoStage.status = "FAIL"
        $autoStage.details += "manifest-parse-failed"
    }
}

if (Test-Path $InstallerPath) {
    $hash = (Get-FileHash -Algorithm SHA256 -Path $InstallerPath).Hash.ToLowerInvariant()
    $autoStage.details += "installer-sha256=$hash"
    if ($ExpectedMsiSha256 -and $hash -ne $ExpectedMsiSha256.ToLowerInvariant()) {
        $autoStage.status = "FAIL"
        $autoStage.details += "installer-hash-mismatch"
    }
}

if (Test-Path $dataRoot) {
    $autoStage.details += "data-root=present"
}
else {
    $autoStage.details += "data-root=not-yet-created (expected before first launch completes)"
}

$report.stages += $autoStage
$report | ConvertTo-Json -Depth 6 | Set-Content -Path $ReportPath -Encoding UTF8

Write-Host ""
Write-Host "NEVAXIS FACTOS CLEAN WINDOWS ACCEPTANCE"
Write-Host "Report: $ReportPath"
Write-Host ""
Write-Host "MANDATORY MANUAL GATES (all must PASS for customer release):"
foreach ($stage in $stages | Where-Object { $_.manual }) {
    Write-Host "  [ ] $($stage.name): $($stage.instruction)"
}
Write-Host ""
Write-Host "Automated install checks: $($autoStage.status)"
Write-Host "CUSTOMER RELEASE STATUS: PENDING (complete manual gates above)"
