# Fails customer release packaging when embedded PostgreSQL template or schema metadata is invalid.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$StagingRoot,
    [string]$RepositoryRoot = "",
    [ValidateSet("DEMO", "PRESENTATION", "PRODUCTION")]
    [string]$DeploymentChannel = "PRODUCTION",
    [string]$ApiBaseUrl = "http://127.0.0.1:5000"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
}

$migrationsDir = Join-Path $RepositoryRoot "packages\NevaxisZentra.Persistence.Migrations\Migrations"
# Registered = a [Migration("<id>")] attribute exists in the Designer file or inline.
$registered = Get-ChildItem $migrationsDir -Filter "*.cs" |
    Where-Object { $_.Name -notmatch 'Snapshot' -and (Select-String -LiteralPath $_.FullName -Pattern '\[Migration\("' -Quiet) } |
    ForEach-Object { $_.BaseName -replace '\.Designer$','' } |
    Select-Object -Unique
$latestMigration = Get-ChildItem $migrationsDir -Filter "*.cs" |
    Where-Object { $_.Name -notmatch 'Designer|Snapshot' -and $registered -contains $_.BaseName } |
    Sort-Object Name -Descending |
    Select-Object -First 1 -ExpandProperty BaseName

$templateRoot = Join-Path $StagingRoot "postgres\template"
$schemaVersionPath = Join-Path $StagingRoot "migrations\expected-schema-version.txt"

if (-not (Test-Path $templateRoot)) {
    throw "Release gate failed: postgres/template is missing at $templateRoot"
}

if (-not (Test-Path (Join-Path $templateRoot "PG_VERSION"))) {
    throw "Release gate failed: postgres/template is not a PostgreSQL data directory (PG_VERSION missing)."
}

$templateFiles = Get-ChildItem $templateRoot -Recurse -File
if ($templateFiles.Count -lt 100) {
    throw "Release gate failed: postgres/template has too few files ($($templateFiles.Count))."
}

if (-not (Test-Path $schemaVersionPath)) {
    throw "Release gate failed: migrations/expected-schema-version.txt is missing."
}

$expectedSchema = (Get-Content $schemaVersionPath -Raw).Trim()
if ([string]::IsNullOrWhiteSpace($expectedSchema)) {
    throw "Release gate failed: expected-schema-version.txt is empty."
}

if ($expectedSchema -ne $latestMigration) {
    throw "Release gate failed: expected-schema-version ($expectedSchema) does not match HEAD migration ($latestMigration)."
}

# --- credential / secret hygiene of the staged payload -------------------------------------
$configDir = Join-Path $StagingRoot "config"
$demoSecrets = Join-Path $configDir "release-demo-secrets.env"
if ($DeploymentChannel -eq "PRODUCTION" -and (Test-Path -LiteralPath $demoSecrets)) {
    throw "Release gate failed: release-demo-secrets.env must not be packaged in a PRODUCTION payload."
}

$packagedDevSettings = Join-Path $StagingRoot "api\appsettings.Development.json"
if (Test-Path -LiteralPath $packagedDevSettings) {
    if ($DeploymentChannel -eq "PRODUCTION") {
        throw "Release gate failed: api/appsettings.Development.json must not be packaged in a PRODUCTION payload."
    }
    if (Select-String -LiteralPath $packagedDevSettings -Pattern '"(Persistence|Password)"' -Quiet) {
        throw "Release gate failed: api/appsettings.Development.json still carries a Persistence/Password section."
    }
}

$databaseCredentialKeys = '^\s*(NEVAXIS_FACTOS__(PERSISTENCE|MIGRATIONS)__PASSWORD|NEVAXIS_FACTOS_POSTGRES_PASSWORD)\s*='
foreach ($envFile in Get-ChildItem -LiteralPath $configDir -File -Filter "*.env*" -ErrorAction SilentlyContinue) {
    if (Select-String -LiteralPath $envFile.FullName -Pattern $databaseCredentialKeys -Quiet) {
        throw "Release gate failed: $($envFile.Name) contains a database credential key. Credentials are generated per machine."
    }
}

# --- credential hygiene scan (known secret values, template verifiers, PRODUCTION demo literals) ---
$knownSecretSources = @(
    (Join-Path $RepositoryRoot "apps\desktop\runtime-packaging\config\release-demo-secrets.env"),
    (Join-Path $RepositoryRoot "infrastructure\.env")
) | Where-Object { Test-Path -LiteralPath $_ }
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "scan-release-credentials.ps1") `
    -PayloadRoot $StagingRoot -DeploymentChannel $DeploymentChannel -KnownSecretSources ($knownSecretSources -join ',')
if ($LASTEXITCODE -ne 0) { throw "Release gate failed: credential scan found a reusable credential or demo literal." }

# --- packaged web must proxy /api/* to the API ----------------------------------------------
$webAppRoot = Join-Path $StagingRoot "web\apps\web"
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "validate-web-api-rewrites.ps1") -WebAppRoot $webAppRoot -ApiBaseUrl $ApiBaseUrl
if ($LASTEXITCODE -ne 0) { throw "Release gate failed: packaged web API rewrites are missing." }

Write-Host "RELEASE_STAGING_VALIDATION: PASS"
Write-Host "  template files: $($templateFiles.Count)"
Write-Host "  template bytes: $((Get-ChildItem $templateRoot -Recurse -File | Measure-Object -Property Length -Sum).Sum)"
Write-Host "  schema version: $expectedSchema"
