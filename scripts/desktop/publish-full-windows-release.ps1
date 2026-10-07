# FACTOS-FULL-CURRENT-WINDOWS-RELEASE publisher
# Packages complete Nevaxis Zentra for standalone Windows installation.

[CmdletBinding()]
param(
    [string]$RepositoryRoot = "",
    [string]$ReleaseVersion = "2026.09.22-production",
    [ValidateSet("DEMO", "PRESENTATION", "PRODUCTION")]
    [string]$DeploymentChannel = "PRODUCTION",
    [string]$ExpectedFreezeSha = "fc8919b91d4ce47c352d22ae675b63925b4b72c8",
    [string]$OutputRoot = "",
    [switch]$SkipWebBuild,
    [switch]$SkipDatabaseTemplate,
    [switch]$EngineeringArtifact,
    [switch]$SkipInstaller,
    [switch]$AllowDirty
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "desktop-runtime-filesystem.ps1")

if ($SkipDatabaseTemplate -and -not $EngineeringArtifact) {
    throw "Customer release builds require the embedded database template. Pass -EngineeringArtifact together with -SkipDatabaseTemplate only for non-customer engineering payloads."
}

if (-not $SkipDatabaseTemplate -and -not $EngineeringArtifact -and $DeploymentChannel -eq "PRODUCTION" -and $SkipInstaller) {
    throw "Customer release candidate cannot use -SkipInstaller. Build the MSI with a populated postgres/template."
}

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
$packagedApiPort = 5000
$packagedApiBaseUrl = "http://127.0.0.1:$packagedApiPort"

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host "==> $Message"
}

function Get-LatestMigrationId {
    # Registered = a [Migration("<id>")] attribute exists in the Designer file or inline.
    $registered = Get-ChildItem $migrationsDir -Filter "*.cs" |
        Where-Object { $_.Name -notmatch 'Snapshot' -and (Select-String -LiteralPath $_.FullName -Pattern '\[Migration\("' -Quiet) } |
        ForEach-Object { $_.BaseName -replace '\.Designer$','' } |
        Select-Object -Unique
    Get-ChildItem $migrationsDir -Filter "*.cs" |
        Where-Object { $_.Name -notmatch 'Designer|Snapshot' -and $registered -contains $_.BaseName } |
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
    # next.config.ts bakes the /api/* rewrites at build time from this variable. Supply it
    # explicitly (never rely on a developer shell) so the packaged web server proxies to the API.
    $previousApiBaseUrl = $env:NEVAXIS_FACTOS_API_BASE_URL
    $env:NEVAXIS_FACTOS_API_BASE_URL = $packagedApiBaseUrl
    try {
        & pnpm --filter @nevaxiszentra/web typecheck
        if ($LASTEXITCODE -ne 0) { throw "Web typecheck failed." }
        & pnpm --filter @nevaxiszentra/web build
        if ($LASTEXITCODE -ne 0) { throw "Web build failed." }
    }
    finally {
        $env:NEVAXIS_FACTOS_API_BASE_URL = $previousApiBaseUrl
        Pop-Location
    }
}

# Gate: fail early (before the heavy packaging steps) when the build has no API rewrites.
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $RepositoryRoot "scripts\release\validate-web-api-rewrites.ps1") `
    -WebAppRoot $webDir -ApiBaseUrl $packagedApiBaseUrl
if ($LASTEXITCODE -ne 0) { throw "Web API rewrite validation failed for the web build." }

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

# PRODUCTION builds compile out demo credential literals (see Directory.Build.props).
$productionBuildProperty = if ($DeploymentChannel -eq "PRODUCTION") { "/p:ZentraProductionBuild=true" } else { "/p:ZentraProductionBuild=false" }

Write-Step "Publish API (self-contained Release)"
& dotnet publish $apiProject -c Release -r win-x64 --self-contained true /p:PublishTrimmed=false $productionBuildProperty -o (Join-Path $stagingRoot "api")
if ($LASTEXITCODE -ne 0) { throw "API publish failed." }
# Development settings carry a development database credential and must never ship as-is.
# PRODUCTION: removed. Other channels keep only the non-secret sections (pilot seed switches);
# the Persistence section (host/port/credential) is stripped and supplied by machine config.
$packagedDevSettings = Join-Path $stagingRoot "api\appsettings.Development.json"
if (Test-Path -LiteralPath $packagedDevSettings) {
    if ($DeploymentChannel -eq "PRODUCTION") {
        Remove-Item -LiteralPath $packagedDevSettings -Force
    }
    else {
        $devSettings = Get-Content -LiteralPath $packagedDevSettings -Raw | ConvertFrom-Json
        $devSettings.PSObject.Properties.Remove("Persistence")
        $devSettings | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $packagedDevSettings -Encoding UTF8
    }
}

Write-Step "Publish Desktop (self-contained Release)"
& dotnet publish $desktopProject -c Release -r win-x64 --self-contained true /p:DesktopDevelopmentDistribution=true /p:DesktopIncludeDebugSymbolsInPublish=false $productionBuildProperty -o (Join-Path $stagingRoot "desktop")
if ($LASTEXITCODE -ne 0) { throw "Desktop publish failed." }

Write-Step "Publish DatabaseTool (self-contained Release)"
& dotnet publish $databaseToolProject -c Release -r win-x64 --self-contained true $productionBuildProperty -o (Join-Path $stagingRoot "tools")
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
Get-ChildItem -LiteralPath $packagingConfig -File |
    Where-Object { $_.Name -ne "release-demo-secrets.env" -and $_.Name -notlike "*.example.env" } |
    ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $stagingRoot "config") -Force }
if ($DeploymentChannel -ne "PRODUCTION") {
    # Non-production channels may seed the application-level demo identity only. Database
    # credentials are generated per machine and are never taken from (or shipped in) this file.
    $demoSecretsSource = Join-Path $packagingConfig "release-demo-secrets.env"
    if (Test-Path -LiteralPath $demoSecretsSource) {
        Get-Content -LiteralPath $demoSecretsSource |
            Where-Object { $_ -match '^\s*(#|DevelopmentSaasIdentity__)' } |
            Set-Content -LiteralPath (Join-Path $stagingRoot "config\release-demo-secrets.env") -Encoding ASCII
    }
}
Set-Content -Path (Join-Path $stagingRoot "migrations\expected-schema-version.txt") -Value $latestMigration -Encoding UTF8

$customerTemplateSrc = Join-Path $RepositoryRoot "release\customer-import-templates"
$customerTemplateDest = Join-Path $stagingRoot "customer-import-templates"
if (Test-Path $customerTemplateSrc) {
    Write-Step "Copy customer import templates"
    if (Test-Path $customerTemplateDest) { Remove-Item -Recurse -Force $customerTemplateDest }
    Copy-Item -Path $customerTemplateSrc -Destination $customerTemplateDest -Recurse -Force
}

if (-not $SkipDatabaseTemplate) {
    Write-Step "Build embedded PostgreSQL customer template"
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

    $stagingValidator = Join-Path $RepositoryRoot "scripts\release\validate-release-staging.ps1"
    & powershell -NoProfile -ExecutionPolicy Bypass -File $stagingValidator -StagingRoot $stagingRoot -RepositoryRoot $RepositoryRoot -DeploymentChannel $DeploymentChannel -ApiBaseUrl $packagedApiBaseUrl
    if ($LASTEXITCODE -ne 0) { throw "Release staging validation failed." }
}
elseif (-not $SkipInstaller) {
    throw "Cannot build customer MSI without embedded postgres/template. Remove -SkipDatabaseTemplate or pass -EngineeringArtifact for engineering-only payloads."
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
    embeddedDatabaseTemplateIncluded = (-not $SkipDatabaseTemplate)
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
