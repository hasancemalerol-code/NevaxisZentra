# Creates isolated PostgreSQL database and applies canonical EF migrations.
# NEVER targets crevafactos_local.
param(
    [string]$PostgresPassword = $env:CREVAFACTOS_POSTGRES_PASSWORD
)

$ErrorActionPreference = "Stop"
. "$PSScriptRoot/closure-constants.ps1"

if ([string]::IsNullOrWhiteSpace($PostgresPassword)) {
    $PostgresPassword = "local_dev_only_change_me"
}

$root = Resolve-Path (Join-Path $PSScriptRoot "../..")
$migrationProject = Join-Path $root "packages/CrevaFactOS.Persistence.Migrations/CrevaFactOS.Persistence.Migrations.csproj"

Write-Host "Creating isolated database: $ClosureDatabaseName"
$existsResult = docker exec $ClosurePostgresContainer psql -U $ClosureMigrationUser -d postgres -t -A -c "SELECT 1 FROM pg_database WHERE datname = '$ClosureDatabaseName'" 2>&1
$exists = if ($existsResult) { "$existsResult".Trim() } else { "" }
if ($exists -ne "1") {
    docker exec $ClosurePostgresContainer psql -U $ClosureMigrationUser -d postgres -c "CREATE DATABASE $ClosureDatabaseName OWNER $ClosureMigrationUser"
    Write-Host "Database created."
} else {
    Write-Host "Database already exists - reusing for closure."
}

$env:CREVAFACTOS__MIGRATIONS__HOST = $ClosurePostgresHost
$env:CREVAFACTOS__MIGRATIONS__PORT = "$ClosurePostgresPort"
$env:CREVAFACTOS__MIGRATIONS__DATABASE = $ClosureDatabaseName
$env:CREVAFACTOS__MIGRATIONS__USERNAME = $ClosureMigrationUser
$env:CREVAFACTOS__MIGRATIONS__PASSWORD = $PostgresPassword
$env:CREVAFACTOS__MIGRATIONS__SSL_MODE = "Disable"
$env:CREVAFACTOS__MIGRATIONS__COMMAND_TIMEOUT_SECONDS = "300"

Write-Host "Applying EF migrations to $ClosureDatabaseName ..."
Push-Location $root
dotnet tool run dotnet-ef -- database update --project $migrationProject --startup-project $migrationProject --context CrevaFactOSDbContext
if ($LASTEXITCODE -ne 0) { throw "Migration apply failed." }
Pop-Location

$migrationCheck = docker exec $ClosurePostgresContainer psql -U $ClosureMigrationUser -d $ClosureDatabaseName -t -A -c "SELECT COUNT(*) FROM \"__EFMigrationsHistory\" WHERE migration_id = '20260906180000_AddAiTaskOmnichannelDeliverySecureAccessFoundation'"
if ($migrationCheck.Trim() -ne "1") {
    throw "Omnichannel migration not present in history."
}

$tableCheck = docker exec $ClosurePostgresContainer psql -U $ClosureMigrationUser -d $ClosureDatabaseName -t -A -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_name IN ('ai_task_deliveries','ai_task_delivery_attempts','ai_task_secure_access_grants')"
if ($tableCheck.Trim() -ne "3") {
    throw "Omnichannel tables missing."
}

Write-Host "ISOLATED_DATABASE = PASS"
Write-Host "MIGRATION_APPLY = PASS"
Write-Host "OMNICHANNEL_MIGRATION_PRESENT = PASS"
