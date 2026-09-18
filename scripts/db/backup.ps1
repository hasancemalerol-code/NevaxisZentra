# pg_dump wrapper for local compose PostgreSQL (UM-9 enterprise reliability)
param(
    [string]$OutputPath = "",
    [string]$ContainerName = "",
    [string]$Database = "crevafactos_local",
    [string]$Username = "crevafactos"
)

$ErrorActionPreference = "Stop"

function Resolve-PostgresContainer {
    param([string]$Preferred)
    if ($Preferred) {
        $exists = docker ps --filter "name=$Preferred" --format "{{.Names}}" 2>$null
        if ($exists) { return $Preferred.Trim() }
    }
    foreach ($name in @("NevaxisZentra-local-postgres-1", "crevafactos-local-postgres-1", "nevaxiszentra-local-postgres-1")) {
        $match = docker ps --filter "name=$name" --format "{{.Names}}" 2>$null
        if ($match) { return $match.Trim() }
    }
    throw "PostgreSQL container not found. Start infrastructure with: docker compose -f infrastructure/compose.yaml up -d postgres"
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
$backupDir = Join-Path $repoRoot "artifacts/backups"
if (-not (Test-Path $backupDir)) {
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
if (-not $OutputPath) {
    $OutputPath = Join-Path $backupDir "crevafactos_local-$timestamp.sql"
}

$container = Resolve-PostgresContainer -Preferred $ContainerName
Write-Host "Backing up database '$Database' from container '$container' to '$OutputPath'..."

docker exec $container pg_dump -U $Username -d $Database --no-owner --no-acl | Out-File -FilePath $OutputPath -Encoding utf8

if (-not (Test-Path $OutputPath)) {
    throw "Backup file was not created: $OutputPath"
}

$size = (Get-Item $OutputPath).Length
if ($size -lt 1024) {
    throw "Backup file is suspiciously small ($size bytes): $OutputPath"
}

Write-Host "UM9_BACKUP = PASS"
Write-Host "BackupPath = $OutputPath"
Write-Host "BackupSizeBytes = $size"
