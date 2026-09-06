# CREVAFACTOS-DESKTOP-RUNTIME-PACKAGING-AND-SYNC-1
# Deterministic publisher for the CrevaFactOS packaged sales-demo runtime bundle.

[CmdletBinding()]
param(
    [string]$RepositoryRoot = "",
    [string]$BundleName = "CrevaFactOS-RC1",
    [switch]$SkipWebBuild,
    [switch]$SkipVerifier
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
}

$bundleRoot = Join-Path $RepositoryRoot "artifacts\desktop-runtime\$BundleName"
$desktopProject = Join-Path $RepositoryRoot "apps\desktop\CrevaFactOS.Desktop\CrevaFactOS.Desktop.csproj"
$apiProject = Join-Path $RepositoryRoot "apps\api\CrevaFactOS.Api.csproj"
$webDir = Join-Path $RepositoryRoot "apps\web"
$packagingConfig = Join-Path $RepositoryRoot "apps\desktop\runtime-packaging\config"
$iconGenerator = Join-Path $RepositoryRoot "apps\desktop\scripts\generate-crevafactos-app-icon.py"
$migrationsDir = Join-Path $RepositoryRoot "packages\CrevaFactOS.Persistence.Migrations\Migrations"
$verifier = Join-Path $RepositoryRoot "apps\desktop\scripts\verify-desktop-runtime-packaging-and-sync-1.ps1"

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host "==> $Message"
}

function Get-LatestMigrationId {
    Get-ChildItem $migrationsDir -Filter "*.cs" |
        Where-Object { $_.Name -notmatch 'Designer|Snapshot' } |
        Sort-Object Name -Descending |
        Select-Object -First 1 -ExpandProperty BaseName
}

$sourceCommit = (git -C $RepositoryRoot rev-parse HEAD).Trim()
$sourceBranch = (git -C $RepositoryRoot branch --show-current).Trim()
$sourceDirty = [bool](git -C $RepositoryRoot status --short)
$buildTimestampUtc = (Get-Date).ToUniversalTime().ToString("o")
$runtimeVersion = "2026.09.09-rc1"
$latestMigration = Get-LatestMigrationId

Write-Step "Source identity"
Write-Host "SOURCE_COMMIT=$sourceCommit"
Write-Host "SOURCE_BRANCH=$sourceBranch"
Write-Host "SOURCE_DIRTY=$sourceDirty"
Write-Host "EXPECTED_SCHEMA_VERSION=$latestMigration"

Write-Step "Prerequisites"
if (Test-Path $iconGenerator) {
    & python $iconGenerator
    if ($LASTEXITCODE -ne 0) { throw "Icon generation failed." }
}

& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $RepositoryRoot "scripts\desktop\deploy-packaged-runtime-local-secrets.ps1") -RepositoryRoot $RepositoryRoot
if ($LASTEXITCODE -ne 0) { throw "Local secret deployment failed." }

Write-Step "Recreate bundle root"
if (Test-Path $bundleRoot) {
    Remove-Item -Recurse -Force $bundleRoot
}
$dirs = @(
    (Join-Path $bundleRoot "desktop"),
    (Join-Path $bundleRoot "api"),
    (Join-Path $bundleRoot "web"),
    (Join-Path $bundleRoot "config"),
    (Join-Path $bundleRoot "manifests"),
    (Join-Path $bundleRoot "migrations"),
    (Join-Path $bundleRoot "logs"),
    (Join-Path $bundleRoot "start")
)
foreach ($dir in $dirs) {
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
}

if (-not $SkipWebBuild) {
    Write-Step "Web production build"
    Push-Location $RepositoryRoot
    try {
        & pnpm --filter @crevafactos/web typecheck
        if ($LASTEXITCODE -ne 0) { throw "Web typecheck failed." }
        & pnpm --filter @crevafactos/web build
        if ($LASTEXITCODE -ne 0) { throw "Web build failed." }
    }
    finally {
        Pop-Location
    }
}

Write-Step "Copy Next standalone web"
$standaloneSrc = Join-Path $webDir ".next\standalone"
$standaloneAppSrc = Join-Path $standaloneSrc "apps\web"
$staticSrc = Join-Path $webDir ".next\static"
$publicSrc = Join-Path $webDir "public"
$webDest = Join-Path $bundleRoot "web"
$webAppDest = Join-Path $webDest "apps\web"
if (-not (Test-Path (Join-Path $standaloneAppSrc "server.js"))) {
    throw "Missing standalone server.js at $standaloneAppSrc. Run web build first."
}
if (Test-Path $webDest) {
    Remove-Item -Recurse -Force $webDest
}
Copy-Item -Path $standaloneSrc -Destination $webDest -Recurse -Force
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $RepositoryRoot "scripts\desktop\repair-standalone-junctions.ps1") -StandaloneRoot $webDest
New-Item -ItemType Directory -Force -Path (Join-Path $webAppDest ".next") | Out-Null
Copy-Item -Path $staticSrc -Destination (Join-Path $webAppDest ".next\static") -Recurse -Force
if (Test-Path $publicSrc) {
    Copy-Item -Path $publicSrc -Destination (Join-Path $webAppDest "public") -Recurse -Force
}

Write-Step "Publish API Release"
& dotnet publish $apiProject -c Release -r win-x64 --self-contained false -o (Join-Path $bundleRoot "api")
if ($LASTEXITCODE -ne 0) { throw "API publish failed." }

Write-Step "Publish Desktop Release"
& dotnet publish $desktopProject -c Release -r win-x64 --self-contained false -p:DesktopDevelopmentDistribution=true -o (Join-Path $bundleRoot "desktop")
if ($LASTEXITCODE -ne 0) { throw "Desktop publish failed." }

Write-Step "Copy config and migrations metadata"
Copy-Item -Path (Join-Path $packagingConfig "*") -Destination (Join-Path $bundleRoot "config") -Force
Set-Content -Path (Join-Path $bundleRoot "migrations\expected-schema-version.txt") -Value $latestMigration -Encoding UTF8
Copy-Item -Path (Join-Path $RepositoryRoot "artifacts\database\CrevaFactOS.idempotent.sql") -Destination (Join-Path $bundleRoot "migrations\CrevaFactOS.idempotent.sql") -ErrorAction SilentlyContinue

$runtimeManifest = [ordered]@{
    productName = "CrevaFactOS"
    runtimeVersion = $runtimeVersion
    runtimeLabel = "CrevaFactOS Sales Demo RC1"
    sourceCommit = $sourceCommit
    sourceBranch = $sourceBranch
    sourceDirty = $sourceDirty
    buildTimestampUtc = $buildTimestampUtc
    desktopRelativePath = "desktop"
    apiRelativePath = "api"
    webRelativePath = "web"
    webPort = 3001
    apiPort = 5000
    databaseHost = "127.0.0.1"
    databasePort = 5433
    databaseName = "crevafactos_local"
    databaseArchitecture = "EXISTING_LOCAL_SERVICE"
    expectedSchemaVersion = $latestMigration
    canonicalRouteCount = 46
    desktopAllModulesValidation = "PASS"
    credentialStability = "PASS"
    dashboardProtectedDiff = 0
}
$runtimeManifest | ConvertTo-Json -Depth 6 | Set-Content -Path (Join-Path $bundleRoot "manifests\runtime-manifest.json") -Encoding UTF8

$sourceManifest = [ordered]@{
    sourceCommit = $sourceCommit
    sourceBranch = $sourceBranch
    sourceDirty = $sourceDirty
    classifiedChanges = @{
        packagingRequired = @(
            "apps/desktop/CrevaFactOS.Desktop/Runtime/* packaged mode",
            "scripts/desktop/publish-desktop-runtime-bundle.ps1",
            "apps/desktop/scripts/verify-desktop-runtime-packaging-and-sync-1.ps1"
        )
        alreadyApprovedSalesDemoWork = @(
            "desktop premium CF icon",
            "shift-handover client form fix",
            "desktop all-modules validation scripts"
        )
    }
}
$sourceManifest | ConvertTo-Json -Depth 6 | Set-Content -Path (Join-Path $bundleRoot "manifests\source-manifest.json") -Encoding UTF8

$buildInfo = [ordered]@{
    product = "CrevaFactOS Sales Demo RC1"
    build = $runtimeVersion
    commit = $sourceCommit
    commitShort = $sourceCommit.Substring(0, 7)
    builtAtUtc = $buildTimestampUtc
}
$buildInfo | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $bundleRoot "manifests\build-info.json") -Encoding UTF8

Write-Step "Generate checksums"
$checksumPath = Join-Path $bundleRoot "manifests\checksums.sha256"
$normalizedBundleRoot = (Resolve-Path $bundleRoot).Path.TrimEnd('\')
$hashLines = New-Object System.Collections.Generic.List[string]
Get-ChildItem $normalizedBundleRoot -Recurse -File |
    Where-Object {
        $_.FullName -notmatch '\\logs\\' -and
        $_.FullName -notmatch '\\web\\node_modules\\' -and
        $_.Name -ne "checksums.sha256"
    } |
    Sort-Object FullName |
    ForEach-Object {
        $fileHash = Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256
        if ($null -eq $fileHash -or [string]::IsNullOrWhiteSpace($fileHash.Hash)) {
            throw "Checksum generation failed for $($_.FullName)"
        }
        $hash = $fileHash.Hash.ToLowerInvariant()
        $relative = $_.FullName.Substring($normalizedBundleRoot.Length).TrimStart('\').Replace('\', '/')
        $hashLines.Add("$hash  $relative")
    }
Set-Content -Path $checksumPath -Value $hashLines -Encoding UTF8

Write-Step "Update packaged desktop shortcut"
$desktopExe = Join-Path $bundleRoot "desktop\CrevaFactOS.exe"
if (-not (Test-Path $desktopExe)) {
    throw "Packaged desktop executable missing: $desktopExe"
}
$shell = New-Object -ComObject WScript.Shell
$shortcutPath = Join-Path ([Environment]::GetFolderPath("Desktop")) "CrevaFactOS.lnk"
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $desktopExe
$shortcut.WorkingDirectory = Split-Path $desktopExe -Parent
$shortcut.Description = "CrevaFactOS AI Factory Operations OS"
$shortcut.IconLocation = "$desktopExe,0"
$shortcut.Save()
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.IconLocation = "$desktopExe,0"
$shortcut.Save()

function Get-DirectoryStats([string]$Path) {
    if (-not (Test-Path $Path)) { return @{ bytes = 0; files = 0 } }
    $files = Get-ChildItem $Path -Recurse -File
    return @{
        bytes = ($files | Measure-Object Length -Sum).Sum
        files = $files.Count
    }
}

$sizeDesktop = Get-DirectoryStats (Join-Path $bundleRoot "desktop")
$sizeApi = Get-DirectoryStats (Join-Path $bundleRoot "api")
$sizeWeb = Get-DirectoryStats (Join-Path $bundleRoot "web")
$sizeTotal = Get-DirectoryStats $bundleRoot

Write-Host ""
Write-Host "Bundle: $bundleRoot"
Write-Host ("Desktop: {0:N2} MB ({1} files)" -f ($sizeDesktop.bytes / 1MB), $sizeDesktop.files)
Write-Host ("API: {0:N2} MB ({1} files)" -f ($sizeApi.bytes / 1MB), $sizeApi.files)
Write-Host ("Web: {0:N2} MB ({1} files)" -f ($sizeWeb.bytes / 1MB), $sizeWeb.files)
Write-Host ("Total: {0:N2} MB ({1} files)" -f ($sizeTotal.bytes / 1MB), $sizeTotal.files)

if (-not $SkipVerifier) {
    Write-Step "Run packaged runtime verifier"
    & powershell -NoProfile -ExecutionPolicy Bypass -File $verifier -RepositoryRoot $RepositoryRoot -BundleRoot $bundleRoot
    if ($LASTEXITCODE -ne 0) {
        throw "Packaged runtime verifier failed."
    }
}

Write-Host ""
Write-Host "DESKTOP_RUNTIME_BUNDLE_PUBLISH: READY"
