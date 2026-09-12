# Creates NEVAXIS_FACTOS_whatsapp_closure_1 and applies canonical EF migrations + closure seeds.
param(
    [string]$PostgresPassword = $env:NEVAXIS_FACTOS_POSTGRES_PASSWORD
)

$ErrorActionPreference = "Stop"
. "$PSScriptRoot/whatsapp-closure-constants.ps1"

if ([string]::IsNullOrWhiteSpace($PostgresPassword)) {
    $PostgresPassword = "local_dev_only_change_me"
}

$root = Resolve-Path (Join-Path $PSScriptRoot "../..")
$migrationProject = Join-Path $root "packages/NevaxisFactOS.Persistence.Migrations/NevaxisFactOS.Persistence.Migrations.csproj"
$db = $WhatsAppClosureDatabaseName

Write-Host "Creating isolated WhatsApp closure database: $db"
$existsRaw = docker exec $WhatsAppClosurePostgresContainer psql -U $WhatsAppClosureMigrationUser -d postgres -t -A -c "SELECT 1 FROM pg_database WHERE datname = '$db'" 2>&1 | Out-String
$exists = if ($existsRaw) { $existsRaw.Trim() } else { "" }
if ($exists -ne "1") {
    docker exec $WhatsAppClosurePostgresContainer psql -U $WhatsAppClosureMigrationUser -d postgres -c "CREATE DATABASE $db OWNER $WhatsAppClosureMigrationUser"
    Write-Host "Database created."
} else {
    Write-Host "Database exists - reusing after verification."
}

$env:NEVAXIS_FACTOS__MIGRATIONS__HOST = $WhatsAppClosurePostgresHost
$env:NEVAXIS_FACTOS__MIGRATIONS__PORT = "$WhatsAppClosurePostgresPort"
$env:NEVAXIS_FACTOS__MIGRATIONS__DATABASE = $db
$env:NEVAXIS_FACTOS__MIGRATIONS__USERNAME = $WhatsAppClosureMigrationUser
$env:NEVAXIS_FACTOS__MIGRATIONS__PASSWORD = $PostgresPassword
$env:NEVAXIS_FACTOS__MIGRATIONS__SSL_MODE = "Disable"
$env:NEVAXIS_FACTOS__MIGRATIONS__COMMAND_TIMEOUT_SECONDS = "600"

Push-Location $root
dotnet tool run dotnet-ef -- database update --project $migrationProject --startup-project $migrationProject --context NevaxisFactOSDbContext
if ($LASTEXITCODE -ne 0) { throw "Migration apply failed." }
Pop-Location

$waTables = docker exec $WhatsAppClosurePostgresContainer psql -U $WhatsAppClosureMigrationUser -d $db -t -A -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_name IN ('ai_task_whatsapp_channel_configurations','ai_task_delivery_provider_events')" 2>$null
$waTables = if ($waTables) { ($waTables | Out-String).Trim() } else { "0" }
if ($waTables -ne "2") { throw "WhatsApp tables missing (count=$waTables)." }

$seedDir = Join-Path $root "scripts/omnichannel-closure"
foreach ($seed in @("seed-closure-fixtures.sql", "seed-closure-subscription.sql")) {
    $path = Join-Path $seedDir $seed
    if (Test-Path $path) {
        Get-Content $path | docker exec -i $WhatsAppClosurePostgresContainer psql -U $WhatsAppClosureMigrationUser -d $db -q
    }
}

$readStub = Join-Path $seedDir "patch-closure-ai-task-read-stubs.sql"
if (Test-Path $readStub) {
    Get-Content $readStub | docker exec -i $WhatsAppClosurePostgresContainer psql -U $WhatsAppClosureMigrationUser -d $db -q
}

$schemaPatch = Join-Path $seedDir "patch-closure-ai-tasks-schema.sql"
if (Test-Path $schemaPatch) {
    Get-Content $schemaPatch | docker exec -i $WhatsAppClosurePostgresContainer psql -U $WhatsAppClosureMigrationUser -d $db -q
}

$webhookPatch = Join-Path $PSScriptRoot "patch-whatsapp-webhook-delivery-resolve.sql"
if (Test-Path $webhookPatch) {
    Get-Content $webhookPatch | docker exec -i $WhatsAppClosurePostgresContainer psql -U $WhatsAppClosureMigrationUser -d $db -q
}

Write-Host "WHATSAPP_CLOSURE_DATABASE=PASS"
