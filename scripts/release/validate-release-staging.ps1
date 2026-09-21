# Fails customer release packaging when embedded PostgreSQL template or schema metadata is invalid.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$StagingRoot,
    [string]$RepositoryRoot = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
}

$migrationsDir = Join-Path $RepositoryRoot "packages\NevaxisZentra.Persistence.Migrations\Migrations"
$registered = Get-ChildItem $migrationsDir -Filter "*.Designer.cs" |
    ForEach-Object { $_.BaseName -replace '\.Designer$','' }
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

Write-Host "RELEASE_STAGING_VALIDATION: PASS"
Write-Host "  template files: $($templateFiles.Count)"
Write-Host "  template bytes: $((Get-ChildItem $templateRoot -Recurse -File | Measure-Object -Property Length -Sum).Sum)"
Write-Host "  schema version: $expectedSchema"
