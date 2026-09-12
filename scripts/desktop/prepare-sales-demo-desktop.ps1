# Prepares NevaxisFactOS Windows desktop sales-demo artifact and desktop shortcut.
# Run once before the Wednesday presentation — NOT during the meeting.

[CmdletBinding()]
param(
    [string]$RepositoryRoot = "",
    [string]$ArtifactRoot = "",
    [switch]$SkipShortcut,
    [switch]$SkipBootstrap,
    [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
}

if ([string]::IsNullOrWhiteSpace($ArtifactRoot)) {
    $ArtifactRoot = Join-Path $RepositoryRoot "artifacts\desktop\NevaxisFactOS"
}

$desktopProject = Join-Path $RepositoryRoot "apps\desktop\NevaxisFactOS.Desktop\NevaxisFactOS.Desktop.csproj"
$desktopExe = Join-Path $ArtifactRoot "NevaxisFactOS.exe"
$bootstrapScript = Join-Path $RepositoryRoot "apps\web\scripts\bootstrap-sales-demo-pilot-full-access-1.mjs"
$iconGenerator = Join-Path $RepositoryRoot "apps\desktop\scripts\generate-NevaxisFactOS-app-icon.py"

function Assert-Command([string]$Name) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Gerekli araç bulunamadı: $Name"
    }
}

Write-Host "NevaxisFactOS sales-demo desktop preparation"
Write-Host "Repository: $RepositoryRoot"

function Test-KnownVerifierRuntimeRootLeak {
    param([string]$Value)
    if ([string]::IsNullOrWhiteSpace($Value)) { return $false }
    return $Value -match '(?i)NevaxisFactOS-web-readiness-|NevaxisFactOS-invalid-runtime|NevaxisFactOS-invalid-|invalid-runtime-root|desktop-negative-runtime-'
}

function Clear-KnownDesktopVerifierEnvironmentLeaks {
    foreach ($scope in @("User", "Machine")) {
        $value = [Environment]::GetEnvironmentVariable("NEVAXIS_FACTOS_DESKTOP_RUNTIME_ROOT", $scope)
        if (Test-KnownVerifierRuntimeRootLeak $value) {
            [Environment]::SetEnvironmentVariable("NEVAXIS_FACTOS_DESKTOP_RUNTIME_ROOT", $null, $scope)
            Write-Warning "Removed leaked NEVAXIS_FACTOS_DESKTOP_RUNTIME_ROOT from $scope scope: $value"
        }
    }
    if (Test-KnownVerifierRuntimeRootLeak $env:NEVAXIS_FACTOS_DESKTOP_RUNTIME_ROOT) {
        Remove-Item Env:NEVAXIS_FACTOS_DESKTOP_RUNTIME_ROOT -ErrorAction SilentlyContinue
        Write-Warning "Removed leaked NEVAXIS_FACTOS_DESKTOP_RUNTIME_ROOT from Process scope"
    }
}

Clear-KnownDesktopVerifierEnvironmentLeaks
foreach ($scope in @("Process", "User", "Machine")) {
    $value = [Environment]::GetEnvironmentVariable("NEVAXIS_FACTOS_DESKTOP_RUNTIME_ROOT", $scope)
    if (Test-KnownVerifierRuntimeRootLeak $value) {
        throw "Desktop preparation blocked: NEVAXIS_FACTOS_DESKTOP_RUNTIME_ROOT at $scope scope points to verifier fixture: $value"
    }
}

Assert-Command dotnet
if (-not $SkipBootstrap) {
    Assert-Command node
    Assert-Command docker
}

$ensureSecrets = Join-Path $RepositoryRoot "scripts\desktop\ensure-development-saas-identity-secrets.ps1"
if (Test-Path $ensureSecrets) {
    Write-Host "Verifying canonical Development Pilot credentials (no password mutation)..."
    & $ensureSecrets -RepositoryRoot $RepositoryRoot
    if ($LASTEXITCODE -ne 0) {
        throw "Development Pilot credential readiness check failed."
    }
} else {
    Write-Warning "Credential readiness script missing: $ensureSecrets"
}

if (-not $SkipBootstrap) {
    if (-not (Test-Path $bootstrapScript)) {
        throw "Sales-demo bootstrap script missing: $bootstrapScript"
    }

    Write-Host "Applying Development Pilot sales-demo profile..."
    & node $bootstrapScript
    if ($LASTEXITCODE -ne 0) {
        throw "Sales-demo bootstrap failed."
    }
} else {
    Write-Host "Skipping sales-demo bootstrap (-SkipBootstrap)."
}

if ($SkipPublish -and (Test-Path $desktopExe)) {
    Write-Host "Skipping desktop publish (-SkipPublish); using existing artifact."
} else {
    Write-Host "Publishing desktop shell..."
    if (Test-Path $iconGenerator) {
        Write-Host "Generating canonical NevaxisFactOS desktop icon..."
        & python $iconGenerator
        if ($LASTEXITCODE -ne 0) {
            throw "Desktop icon generation failed."
        }
    }

    if (Test-Path $ArtifactRoot) {
        Remove-Item -Recurse -Force $ArtifactRoot
    }
    New-Item -ItemType Directory -Force -Path $ArtifactRoot | Out-Null

    & dotnet publish $desktopProject `
        -c Release `
        -r win-x64 `
        --self-contained false `
        -p:DesktopDevelopmentDistribution=true `
        -o $ArtifactRoot

    if ($LASTEXITCODE -ne 0) {
        throw "Desktop publish failed."
    }
}

if (-not (Test-Path $desktopExe)) {
    throw "Expected executable not found: $desktopExe"
}

Write-Host "Desktop artifact: $desktopExe"

if (-not $SkipShortcut) {
    $shell = New-Object -ComObject WScript.Shell
    $desktopFolder = [Environment]::GetFolderPath("Desktop")
    $shortcutPath = Join-Path $desktopFolder "NevaxisFactOS.lnk"

    foreach ($duplicateName in @("NevaxisFactOS.lnk", "NevaxisFactOS Desktop.lnk")) {
        $duplicatePath = Join-Path $desktopFolder $duplicateName
        if ($duplicatePath -ne $shortcutPath -and (Test-Path $duplicatePath)) {
            Remove-Item $duplicatePath -Force
            Write-Host "Removed duplicate shortcut: $duplicatePath"
        }
    }

    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $desktopExe
    $shortcut.WorkingDirectory = $ArtifactRoot
    $shortcut.Description = "NevaxisFactOS AI Factory Operations OS"
    $shortcut.IconLocation = "$desktopExe,0"
    $shortcut.Save()
    # Re-save refreshes Explorer icon cache when the embedded executable icon changes.
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.IconLocation = "$desktopExe,0"
    $shortcut.Save()
    Write-Host "Desktop shortcut: $shortcutPath"
    Write-Host "Shortcut target: $($shortcut.TargetPath)"
    Write-Host "Shortcut working directory: $($shortcut.WorkingDirectory)"
    Write-Host "Shortcut icon: $($shortcut.IconLocation)"
}

Write-Host "DESKTOP_SALES_DEMO_PREPARE: READY"
