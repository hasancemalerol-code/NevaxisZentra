# FACTOS-FULL-CURRENT-WINDOWS-RELEASE publisher
# Packages complete Nevaxis Zentra for standalone Windows installation.

[CmdletBinding()]
param(
    [string]$RepositoryRoot = "",
    [string]$ReleaseVersion = "2026.09.16-production",
    [ValidateSet("DEMO", "PRESENTATION", "PRODUCTION")]
    [string]$DeploymentChannel = "DEMO",
    [string]$ExpectedFreezeSha = "fc8919b91d4ce47c352d22ae675b63925b4b72c8",
    [string]$OutputRoot = "",
    [switch]$SkipWebBuild,
    [switch]$SkipDatabaseTemplate,
    [switch]$SkipInstaller,
    [switch]$AllowDirty
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "desktop-runtime-filesystem.ps1")

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $RepositoryRoot "release"
}

$stagingRoot = Join-Path $OutputRoot "staging"
$artifactRoot = Join-Path $OutputRoot "artifacts"
$cacheRoot = Join-Path $RepositoryRoot "artifacts\desktop-release-cache"
$desktopProject = Join-Path $RepositoryRoot "apps\desktop\NevaxisZentra.Desktop\NevaxisZentra.Desktop.csproj"
$apiProject = Join-Path $RepositoryRoot "apps\api\NevaxisZentra.Api.csproj"
$databaseToolProject = Join-Path $RepositoryRoot "tools\NevaxisZentra.DatabaseTool\NevaxisZentra.DatabaseTool.csproj"
$webDir = Join-Path $RepositoryRoot "apps\web"
$packagingConfig = Join-Path $RepositoryRoot "apps\desktop\runtime-packaging\config"
$migrationsDir = Join-Path $RepositoryRoot "packages\NevaxisZentra.Persistence.Migrations\Migrations"
$nodeVersion = "22.18.0"
$postgresVersion = "17.4-1"
$postgresPort = 55432

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

function Get-Sha256Hex([string]$Path) {
    return (Get-FileHash -Algorithm SHA256 -Path $Path).Hash.ToLowerInvariant()
}

function Format-Megabytes([long]$Bytes) {
    return [math]::Round($Bytes / 1MB, 2)
}

Write-Step "Verify freeze baseline"
$currentSha = (git -C $RepositoryRoot rev-parse HEAD).Trim()
if ($currentSha -ne $ExpectedFreezeSha) {
    Write-Warning "HEAD ($currentSha) differs from expected freeze SHA ($ExpectedFreezeSha). Continuing with current HEAD."
}
$dirty = git -C $RepositoryRoot status --short
if ($dirty -and -not $AllowDirty) {
    throw "Working tree is not clean. Commit or stash release-scoped changes first, or pass -AllowDirty."
}

$latestMigration = Get-LatestMigrationId
$buildTimestampUtc = (Get-Date).ToUniversalTime().ToString("o")

Write-Step "Recreate release staging"
if (Test-Path $stagingRoot) {
    try {
        Remove-Item -Recurse -Force $stagingRoot -ErrorAction Stop
    }
    catch {
        Write-Warning "Existing staging root is locked ($stagingRoot). Using versioned staging root."
        $stagingRoot = Join-Path $OutputRoot ("staging-" + (Get-Date -Format "yyyyMMdd-HHmmss"))
    }
}
$dirs = @(
    (Join-Path $stagingRoot "desktop"),
    (Join-Path $stagingRoot "api"),
    (Join-Path $stagingRoot "web"),
    (Join-Path $stagingRoot "node"),
    (Join-Path $stagingRoot "postgres\bin"),
    (Join-Path $stagingRoot "postgres\template"),
    (Join-Path $stagingRoot "config"),
    (Join-Path $stagingRoot "manifests"),
    (Join-Path $stagingRoot "migrations"),
    (Join-Path $stagingRoot "tools"),
    (Join-Path $stagingRoot "logs")
)
foreach ($dir in $dirs) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }

if (-not $SkipWebBuild) {
    Write-Step "Web production build"
    Push-Location $RepositoryRoot
    try {
        & pnpm --filter @nevaxiszentra/web typecheck
        if ($LASTEXITCODE -ne 0) { throw "Web typecheck failed." }
        & pnpm --filter @nevaxiszentra/web build
        if ($LASTEXITCODE -ne 0) { throw "Web build failed." }
    }
    finally { Pop-Location }
}

Write-Step "Copy Next.js standalone web"
$standaloneSrc = Join-Path $webDir ".next\standalone"
$standaloneAppSrc = Join-Path $standaloneSrc "apps\web"
$staticSrc = Join-Path $webDir ".next\static"
$publicSrc = Join-Path $webDir "public"
$webDest = Join-Path $stagingRoot "web"
$webAppDest = Join-Path $webDest "apps\web"
if (-not (Test-Path (Join-Path $standaloneAppSrc "server.js"))) {
    throw "Missing standalone server.js. Run web build first."
}
Invoke-RobocopyDirectory -Source $standaloneSrc -Destination $webDest
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $RepositoryRoot "scripts\desktop\repair-standalone-junctions.ps1") -StandaloneRoot $webDest
Assert-StandaloneRuntimeModuleResolution -StandaloneRoot $webDest
New-Item -ItemType Directory -Force -Path (Join-Path $webAppDest ".next") | Out-Null
Invoke-RobocopyDirectory -Source $staticSrc -Destination (Join-Path $webAppDest ".next\static")
if (Test-Path $publicSrc) {
    Invoke-RobocopyDirectory -Source $publicSrc -Destination (Join-Path $webAppDest "public")
}

Write-Step "Publish API (self-contained Release)"
& dotnet publish $apiProject -c Release -r win-x64 --self-contained true /p:PublishTrimmed=false -o (Join-Path $stagingRoot "api")
if ($LASTEXITCODE -ne 0) { throw "API publish failed." }

Write-Step "Publish Desktop (self-contained Release)"
& dotnet publish $desktopProject -c Release -r win-x64 --self-contained true /p:DesktopDevelopmentDistribution=true /p:DesktopIncludeDebugSymbolsInPublish=false -o (Join-Path $stagingRoot "desktop")
if ($LASTEXITCODE -ne 0) { throw "Desktop publish failed." }

Write-Step "Publish DatabaseTool (self-contained Release)"
& dotnet publish $databaseToolProject -c Release -r win-x64 --self-contained true -o (Join-Path $stagingRoot "tools")
if ($LASTEXITCODE -ne 0) { throw "DatabaseTool publish failed." }

Write-Step "Bundle Node.js runtime"
New-Item -ItemType Directory -Force -Path $cacheRoot | Out-Null
$nodeZip = Join-Path $cacheRoot "node-v$nodeVersion-win-x64.zip"
if (-not (Test-Path $nodeZip)) {
    $nodeUrl = "https://nodejs.org/dist/v$nodeVersion/node-v$nodeVersion-win-x64.zip"
    Write-Host "Downloading $nodeUrl"
    Invoke-WebRequest -Uri $nodeUrl -OutFile $nodeZip -UseBasicParsing
}
Expand-Archive -Path $nodeZip -DestinationPath (Join-Path $cacheRoot "node-extract") -Force
$nodeSrc = Join-Path $cacheRoot "node-extract\node-v$nodeVersion-win-x64"
Copy-Item -Path (Join-Path $nodeSrc "node.exe") -Destination (Join-Path $stagingRoot "node\node.exe") -Force

Write-Step "Bundle PostgreSQL binaries"
$postgresZip = Join-Path $cacheRoot "postgresql-$postgresVersion-windows-x64-binaries.zip"
if (-not (Test-Path $postgresZip)) {
    $pgUrl = "https://get.enterprisedb.com/postgresql/postgresql-$postgresVersion-windows-x64-binaries.zip"
    Write-Host "Downloading $pgUrl"
    Invoke-WebRequest -Uri $pgUrl -OutFile $postgresZip -UseBasicParsing
}
$pgExtract = Join-Path $cacheRoot "postgres-extract"
if (Test-Path $pgExtract) { Remove-Item -Recurse -Force $pgExtract }
Expand-Archive -Path $postgresZip -DestinationPath $pgExtract -Force
$pgRootSrc = Get-ChildItem $pgExtract -Recurse -Directory -Filter "pgsql" | Select-Object -First 1
if (-not $pgRootSrc) { throw "PostgreSQL pgsql root directory not found in archive." }
Invoke-RobocopyDirectory -Source $pgRootSrc.FullName -Destination (Join-Path $stagingRoot "postgres")

Write-Step "Copy runtime config"
Copy-Item -Path (Join-Path $packagingConfig "*") -Destination (Join-Path $stagingRoot "config") -Force
Set-Content -Path (Join-Path $stagingRoot "migrations\expected-schema-version.txt") -Value $latestMigration -Encoding UTF8

$customerTemplateSrc = Join-Path $RepositoryRoot "release\customer-import-templates"
$customerTemplateDest = Join-Path $stagingRoot "customer-import-templates"
if (Test-Path $customerTemplateSrc) {
    Write-Step "Copy customer import templates"
    if (Test-Path $customerTemplateDest) { Remove-Item -Recurse -Force $customerTemplateDest }
    Copy-Item -Path $customerTemplateSrc -Destination $customerTemplateDest -Recurse -Force
}

if (-not $SkipDatabaseTemplate) {
    Write-Step "Build pre-seeded PostgreSQL template"
    $templateBuilder = Join-Path $PSScriptRoot "build-release-database-template.ps1"
    if (-not (Test-Path $templateBuilder)) {
        throw "Missing database template builder: $templateBuilder"
    }
    & powershell -NoProfile -ExecutionPolicy Bypass -File $templateBuilder `
        -RepositoryRoot $RepositoryRoot `
        -PostgresBinDirectory (Join-Path $stagingRoot "postgres\bin") `
        -TemplateOutputDirectory (Join-Path $stagingRoot "postgres\template") `
        -Port $postgresPort
    if ($LASTEXITCODE -ne 0) { throw "Database template build failed." }
}

$runtimeManifest = [ordered]@{
    productName = "Nevaxis Zentra"
    runtimeVersion = $ReleaseVersion
    runtimeLabel = "Nevaxis Zentra Full Current Windows Release"
    sourceCommit = $currentSha
    freezeTag = "factos-final-sales-freeze-2026-09"
    buildTimestampUtc = $buildTimestampUtc
    desktopRelativePath = "desktop"
    apiRelativePath = "api"
    webRelativePath = "web"
    webPort = 3001
    apiPort = 5000
    databaseHost = "127.0.0.1"
    databasePort = $postgresPort
    databaseName = "crevafactos_local"
    databaseArchitecture = "EMBEDDED_LOCAL"
    deploymentChannel = $DeploymentChannel
    expectedSchemaVersion = $latestMigration
}
$runtimeManifest | ConvertTo-Json -Depth 6 | Set-Content -Path (Join-Path $stagingRoot "manifests\runtime-manifest.json") -Encoding UTF8

Write-Step "Generate staging checksums"
$checksumPath = Join-Path $stagingRoot "manifests\checksums.sha256"
$hashLines = New-Object System.Collections.Generic.List[string]
Get-ChildItem $stagingRoot -Recurse -File |
    Where-Object { $_.FullName -notmatch '\\logs\\' -and $_.Name -ne "checksums.sha256" } |
    Sort-Object FullName |
    ForEach-Object {
        $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        $relative = $_.FullName.Substring($stagingRoot.Length).TrimStart('\').Replace('\', '/')
        $hashLines.Add("$hash  $relative")
    }
Set-Content -Path $checksumPath -Value $hashLines -Encoding UTF8

if (-not $SkipInstaller) {
    Write-Step "Build WiX installer"
    $installerScript = Join-Path $RepositoryRoot "apps\desktop\scripts\publish-full-windows-installer.ps1"
    if (-not (Test-Path $installerScript)) {
        throw "Missing installer publisher: $installerScript"
    }
    & powershell -NoProfile -ExecutionPolicy Bypass -File $installerScript `
        -StagingRoot $stagingRoot `
        -ArtifactRoot $artifactRoot `
        -Version $ReleaseVersion `
        -SourceCommit $currentSha
    if ($LASTEXITCODE -ne 0) { throw "Installer build failed." }
}

Write-Host ""
Write-Host "FULL_WINDOWS_RELEASE_PUBLISH: READY"
Write-Host "Staging: $stagingRoot"
Write-Host "Artifacts: $artifactRoot"
