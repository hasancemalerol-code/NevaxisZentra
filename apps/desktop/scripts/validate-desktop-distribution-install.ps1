param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$UpgradeVersion = "",

    [string]$ArtifactRoot = "",

    [switch]$SkipUpgradeValidation
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-IsAdministrator)) {
    throw "validate-desktop-distribution-install.ps1 must run in an elevated PowerShell session for per-machine MSI validation."
}

if ([string]::IsNullOrWhiteSpace($ArtifactRoot)) {
    $ArtifactRoot = Join-Path $PSScriptRoot "..\..\..\artifacts\desktop-distribution"
}

$ArtifactRoot = (Resolve-Path $ArtifactRoot).Path
$artifactDir = Join-Path $ArtifactRoot $Version
$msiPath = Join-Path $artifactDir "CrevaFactOS-Desktop-Setup-$Version-x64.msi"
$profilePath = Join-Path $env:LOCALAPPDATA "CrevaFactOS\Desktop\WebView2"
$results = [ordered]@{}

function Record([string]$Name, [bool]$Success, [string]$Detail = "") {
    $results[$Name] = [ordered]@{
        success = $Success
        detail = $Detail
    }
    $mark = if ($Success) { "PASS" } else { "FAIL" }
    $suffix = if ([string]::IsNullOrWhiteSpace($Detail)) { "" } else { " ($Detail)" }
    Write-Host "[$mark] $Name$suffix"
}

function Stop-DesktopApp {
    Get-Process -Name "CrevaFactOS.Desktop" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
}

function Invoke-Msi {
    param(
        [Parameter(Mandatory = $true)][string[]]$ArgumentList
    )

    $process = Start-Process -FilePath "msiexec.exe" -ArgumentList $ArgumentList -Wait -PassThru
    return $process.ExitCode
}

function Get-ArpEntry {
    $uninstallKey = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"
    return Get-ChildItem $uninstallKey -ErrorAction SilentlyContinue | ForEach-Object {
        Get-ItemProperty $_.PSPath -ErrorAction SilentlyContinue
    } | Where-Object {
        $null -ne $_ -and $_.PSObject.Properties.Name -contains "DisplayName" -and $_.DisplayName -eq "CrevaFactOS"
    } | Select-Object -First 1
}

function Uninstall-CrevaFactOSDesktop {
    $entry = Get-ArpEntry
    if ($entry -and $entry.PSChildName) {
        $null = Invoke-Msi @("/x", $entry.PSChildName, "/qn", "/norestart")
        Start-Sleep -Seconds 2
    }
}

if (-not (Test-Path $msiPath)) {
    throw "MSI not found: $msiPath"
}

Uninstall-CrevaFactOSDesktop
Stop-DesktopApp

$programFilesExe = Join-Path ${env:ProgramFiles} "CrevaFactOS\Desktop\CrevaFactOS.Desktop.exe"
$startMenuShortcut = Join-Path $env:ProgramData "Microsoft\Windows\Start Menu\Programs\CrevaFactOS\CrevaFactOS.lnk"
$desktopShortcut = Join-Path $env:Public "Desktop\CrevaFactOS.lnk"
$logFile = Join-Path $artifactDir "install-smoke.log"
$profileMarker = Join-Path $profilePath "distribution-smoke-profile.txt"
New-Item -ItemType Directory -Force -Path $profilePath | Out-Null
"profile-preservation-test" | Set-Content -Path $profileMarker -Encoding ASCII

$installExit = Invoke-Msi @(
    "/i", $msiPath,
    "/qn", "/norestart",
    "/l*v", $logFile
)
Record "Installer launches and completes" ($installExit -eq 0) "exit=$installExit"
Record "Application files installed" (Test-Path $programFilesExe)
Record "Start Menu shortcut exists" (Test-Path $startMenuShortcut)
Record "Desktop shortcut exists" (Test-Path $desktopShortcut)

$arpEntry = Get-ArpEntry
Record "Add/Remove Programs entry exists" ($null -ne $arpEntry)
if ($arpEntry) {
    Record "Installed product version shown" ($arpEntry.DisplayVersion -like "$Version*") $arpEntry.DisplayVersion
    Record "Publisher shown" ($arpEntry.Publisher -eq "CrevaFactOS") $arpEntry.Publisher
}

Stop-DesktopApp
$proc = Start-Process -FilePath $programFilesExe -PassThru
Start-Sleep -Seconds 4
Record "Application launches" (-not $proc.HasExited)
Stop-DesktopApp
Record "Application closes cleanly" (-not (Get-Process -Name "CrevaFactOS.Desktop" -ErrorAction SilentlyContinue))

if ($arpEntry -and $arpEntry.PSChildName) {
    $repairExit = Invoke-Msi @("/fa", $arpEntry.PSChildName, "/qn", "/norestart")
    Record "Repair works" ($repairExit -eq 0) "exit=$repairExit"
}

$reinstallExit = Invoke-Msi @(
    "/i", $msiPath,
    "/qn", "/norestart",
    "REINSTALL=ALL",
    "REINSTALLMODE=vomus"
)
Record "Same-version reinstall safe" ($reinstallExit -eq 0) "exit=$reinstallExit"

if (-not $SkipUpgradeValidation) {
    if ([string]::IsNullOrWhiteSpace($UpgradeVersion)) {
        $UpgradeVersion = if ($Version -eq "1.0.0") { "1.0.1" } else { "$Version.1" }
    }

    $upgradeMsiPath = Join-Path (Join-Path $ArtifactRoot $UpgradeVersion) "CrevaFactOS-Desktop-Setup-$UpgradeVersion-x64.msi"
    if (-not (Test-Path $upgradeMsiPath)) {
        Write-Host "Building upgrade MSI $UpgradeVersion..."
        & (Join-Path $PSScriptRoot "publish-desktop-distribution.ps1") -Version $UpgradeVersion -AllowDevelopmentDistribution | Out-Host
    }

    if (Test-Path $upgradeMsiPath) {
        $upgradeExit = Invoke-Msi @(
            "/i", $upgradeMsiPath,
            "/qn", "/norestart"
        )
        Record "Higher-version upgrade succeeds" ($upgradeExit -eq 0) "exit=$upgradeExit"
        $upgradedEntry = Get-ArpEntry
        Record "Upgraded version shown in ARP" ($upgradedEntry.DisplayVersion -like "$UpgradeVersion*") $upgradedEntry.DisplayVersion

        $downgradeExit = Invoke-Msi @(
            "/i", $msiPath,
            "/qn", "/norestart"
        )
        Record "Downgrade rejected" ($downgradeExit -ne 0) "exit=$downgradeExit"
        $msiPath = $upgradeMsiPath
        $arpEntry = Get-ArpEntry
    }
    else {
        Record "Higher-version upgrade succeeds" $false "upgrade MSI missing"
        Record "Downgrade rejected" $false "upgrade MSI missing"
    }
}

if ($arpEntry -and $arpEntry.PSChildName) {
    $uninstallExit = Invoke-Msi @("/x", $arpEntry.PSChildName, "/qn", "/norestart")
    Record "Uninstall completes" ($uninstallExit -eq 0) "exit=$uninstallExit"
}

Record "WebView2 profile preserved after uninstall" (Test-Path $profileMarker)
Record "Program files removed after uninstall" (-not (Test-Path $programFilesExe))
Record "Start Menu shortcut removed" (-not (Test-Path $startMenuShortcut))
Record "Desktop shortcut removed" (-not (Test-Path $desktopShortcut))
Record "No orphan desktop process" (-not (Get-Process -Name "CrevaFactOS.Desktop" -ErrorAction SilentlyContinue))

$reinstallAfterExit = Invoke-Msi @(
    "/i", $msiPath,
    "/qn", "/norestart"
)
Record "Reinstall after uninstall works" ($reinstallAfterExit -eq 0) "exit=$reinstallAfterExit"
if ($reinstallAfterExit -eq 0) {
    Uninstall-CrevaFactOSDesktop
}

$reportPath = Join-Path $artifactDir "install-smoke-report.json"
$results | ConvertTo-Json -Depth 4 | Set-Content -Path $reportPath -Encoding UTF8

$failed = @($results.Values | Where-Object { -not $_.success }).Count
if ($failed -gt 0) {
    throw "Install smoke validation failed ($failed checks)."
}

Write-Host "Install smoke validation passed. Report: $reportPath"
