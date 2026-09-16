# Nevaxis FactOS release acceptance harness (engineering gate).
# Produces machine-readable evidence for stages executable in the current environment.

[CmdletBinding()]
param(
    [string]$RepositoryRoot = "",
    [string]$ArtifactRoot = "",
    [string]$ReportPath = "",
    [switch]$SkipMsiBuild
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $scriptRoot = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot "..\..")).Path
}

if ([string]::IsNullOrWhiteSpace($ArtifactRoot)) {
    $ArtifactRoot = Join-Path $RepositoryRoot "release\artifacts"
}
if ([string]::IsNullOrWhiteSpace($ReportPath)) {
    $ReportPath = Join-Path $ArtifactRoot "release-acceptance-report.json"
}

function New-Stage([string]$Name) {
    [ordered]@{
        name = $Name
        status = "PENDING"
        timestampUtc = (Get-Date).ToUniversalTime().ToString("o")
        details = @()
    }
}

$report = [ordered]@{
    product = "Nevaxis FactOS"
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    sourceCommit = (git -C $RepositoryRoot rev-parse HEAD)
    stages = @()
    engineeringStatus = "PENDING"
    customerReleaseStatus = "PENDING PHYSICAL CLEAN-PC ACCEPTANCE"
}

function Complete-Stage($stage, [string]$status, [string[]]$details) {
    $stage.status = $status
    $stage.timestampUtc = (Get-Date).ToUniversalTime().ToString("o")
    $stage.details = $details
}

$buildStage = New-Stage "solution-build"
try {
    & dotnet build (Join-Path $RepositoryRoot "NevaxisFactOS.sln") -c Release | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }
    Complete-Stage $buildStage "PASS" @("NevaxisFactOS.sln Release build")
}
catch {
    Complete-Stage $buildStage "FAIL" @($_.Exception.Message)
}
$report.stages += $buildStage

$testsStage = New-Stage "persistence-tests"
try {
    & dotnet test (Join-Path $RepositoryRoot "tests\NevaxisFactOS.Persistence.Tests\NevaxisFactOS.Persistence.Tests.csproj") -c Release --no-build | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "persistence tests failed" }
    Complete-Stage $testsStage "PASS" @("NevaxisFactOS.Persistence.Tests")
}
catch {
    Complete-Stage $testsStage "FAIL" @($_.Exception.Message)
}
$report.stages += $testsStage

$desktopTestsStage = New-Stage "desktop-tests"
try {
    & dotnet test (Join-Path $RepositoryRoot "tests\NevaxisFactOS.Desktop.Tests\NevaxisFactOS.Desktop.Tests.csproj") -c Release | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "desktop tests failed" }
    Complete-Stage $desktopTestsStage "PASS" @("NevaxisFactOS.Desktop.Tests")
}
catch {
    Complete-Stage $desktopTestsStage "FAIL" @($_.Exception.Message)
}
$report.stages += $desktopTestsStage

if (-not $SkipMsiBuild) {
    $msiStage = New-Stage "msi-build"
    try {
        & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $RepositoryRoot "scripts\desktop\publish-full-windows-release.ps1") -RepositoryRoot $RepositoryRoot | Out-Host
        Complete-Stage $msiStage "PASS" @("publish-full-windows-release")
    }
    catch {
        Complete-Stage $msiStage "FAIL" @($_.Exception.Message)
    }
    $report.stages += $msiStage
}

$scanStage = New-Stage "security-artifact-scan"
try {
    if (Test-Path $ArtifactRoot) {
        & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "scan-release-artifacts.ps1") -ArtifactRoot $ArtifactRoot
        Complete-Stage $scanStage "PASS" @("scan-release-artifacts.ps1")
    }
    else {
        Complete-Stage $scanStage "SKIPPED" @("Artifact root not found: $ArtifactRoot")
    }
}
catch {
    Complete-Stage $scanStage "FAIL" @($_.Exception.Message)
}
$report.stages += $scanStage

$msiPath = Join-Path $ArtifactRoot "Nevaxis-FactOS-Setup.msi"
$integrityStage = New-Stage "artifact-integrity"
if (Test-Path $msiPath) {
    $hash = (Get-FileHash -Algorithm SHA256 -Path $msiPath).Hash.ToLowerInvariant()
    Complete-Stage $integrityStage "PASS" @("sha256=$hash")
    $report.msiSha256 = $hash
}
else {
    Complete-Stage $integrityStage "SKIPPED" @("MSI not present")
}
$report.stages += $integrityStage

$mandatory = @("solution-build", "persistence-tests", "desktop-tests", "security-artifact-scan")
$failed = $report.stages | Where-Object { $mandatory -contains $_.name -and $_.status -eq "FAIL" }
if ($failed.Count -eq 0) {
    $report.engineeringStatus = "PASS"
}
else {
    $report.engineeringStatus = "FAIL"
}

$report | ConvertTo-Json -Depth 6 | Set-Content -Path $ReportPath -Encoding UTF8
Write-Host "Release acceptance report: $ReportPath"
Write-Host "ENGINEERING STATUS: $($report.engineeringStatus)"
Write-Host "CUSTOMER RELEASE STATUS: $($report.customerReleaseStatus)"

if ($report.engineeringStatus -eq "FAIL") {
    exit 1
}
