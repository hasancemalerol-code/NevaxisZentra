# Applies omnichannel delivery migration SQL directly to isolated closure DB.
$ErrorActionPreference = "Stop"
. "$PSScriptRoot/closure-constants.ps1"

$migrationFile = Join-Path (Resolve-Path "$PSScriptRoot/../..") "packages/CrevaFactOS.Persistence.Migrations/Migrations/20260906180000_AddAiTaskOmnichannelDeliverySecureAccessFoundation.cs"
if (-not (Test-Path $migrationFile)) { throw "Migration file missing." }

$exists = Get-Content "$PSScriptRoot/tmp-check-omnichannel.sql" -ErrorAction SilentlyContinue
@"
SELECT COUNT(*) FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260906180000_AddAiTaskOmnichannelDeliverySecureAccessFoundation';
"@ | Set-Content "$PSScriptRoot/tmp-check-omnichannel.sql"

$present = (Get-Content "$PSScriptRoot/tmp-check-omnichannel.sql" | docker exec -i $ClosurePostgresContainer psql -U $ClosureMigrationUser -d $ClosureDatabaseName -t -A).Trim()
if ($present -eq "1") {
    Write-Host "Omnichannel migration already applied."
    exit 0
}

Write-Host "Applying omnichannel migration via EF to target only ..."
$root = Resolve-Path "$PSScriptRoot/../.."
$migrationProject = Join-Path $root "packages/CrevaFactOS.Persistence.Migrations/CrevaFactOS.Persistence.Migrations.csproj"
$env:CREVAFACTOS__MIGRATIONS__HOST = $ClosurePostgresHost
$env:CREVAFACTOS__MIGRATIONS__PORT = "$ClosurePostgresPort"
$env:CREVAFACTOS__MIGRATIONS__DATABASE = $ClosureDatabaseName
$env:CREVAFACTOS__MIGRATIONS__USERNAME = $ClosureMigrationUser
$env:CREVAFACTOS__MIGRATIONS__PASSWORD = $(if ($env:CREVAFACTOS_POSTGRES_PASSWORD) { $env:CREVAFACTOS_POSTGRES_PASSWORD } else { "local_dev_only_change_me" })
$env:CREVAFACTOS__MIGRATIONS__SSL_MODE = "Disable"
$env:CREVAFACTOS__MIGRATIONS__COMMAND_TIMEOUT_SECONDS = "120"

Push-Location $root
dotnet tool run dotnet-ef -- database update 20260906180000_AddAiTaskOmnichannelDeliverySecureAccessFoundation --project $migrationProject --startup-project $migrationProject --context CrevaFactOSDbContext
if ($LASTEXITCODE -ne 0) { throw "Targeted migration apply failed." }
Pop-Location
Write-Host "OMNICHANNEL_MIGRATION_PRESENT = PASS"
