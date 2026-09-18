# Creates NEVAXIS_FACTOS_email_closure_1 and applies canonical EF migrations + closure seeds.
param(
    [string]$PostgresPassword = $env:NEVAXIS_FACTOS_POSTGRES_PASSWORD
)

$ErrorActionPreference = "Stop"
. "$PSScriptRoot/email-closure-constants.ps1"

if ([string]::IsNullOrWhiteSpace($PostgresPassword)) {
    $PostgresPassword = "local_dev_only_change_me"
}

$root = Resolve-Path (Join-Path $PSScriptRoot "../..")
$migrationProject = Join-Path $root "packages/NevaxisZentra.Persistence.Migrations/NevaxisZentra.Persistence.Migrations.csproj"
$db = $EmailClosureDatabaseName

Write-Host "Creating isolated Email closure database: $db"
$existsRaw = docker exec $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d postgres -t -A -c "SELECT 1 FROM pg_database WHERE datname = '$db'" 2>&1 | Out-String
$exists = if ($existsRaw) { $existsRaw.Trim() } else { "" }
if ($exists -ne "1") {
    docker exec $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d postgres -c "CREATE DATABASE $db OWNER $EmailClosureMigrationUser"
    Write-Host "Database created."
} else {
    Write-Host "Database exists - reusing after verification."
}

$env:NEVAXIS_FACTOS__MIGRATIONS__HOST = $EmailClosurePostgresHost
$env:NEVAXIS_FACTOS__MIGRATIONS__PORT = "$EmailClosurePostgresPort"
$env:NEVAXIS_FACTOS__MIGRATIONS__DATABASE = $db
$env:NEVAXIS_FACTOS__MIGRATIONS__USERNAME = $EmailClosureMigrationUser
$env:NEVAXIS_FACTOS__MIGRATIONS__PASSWORD = $PostgresPassword
$env:NEVAXIS_FACTOS__MIGRATIONS__SSL_MODE = "Disable"
$env:NEVAXIS_FACTOS__MIGRATIONS__COMMAND_TIMEOUT_SECONDS = "600"

Push-Location $root
dotnet tool run dotnet-ef -- database update --project $migrationProject --startup-project $migrationProject --context NevaxisZentraDbContext
if ($LASTEXITCODE -ne 0) { throw "Migration apply failed." }
Pop-Location

$emailTables = docker exec $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d $db -t -A -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'ai_task_email_channel_configurations'" 2>$null
$emailTables = if ($emailTables) { ($emailTables | Out-String).Trim() } else { "0" }
if ($emailTables -ne "1") { throw "Email config table missing (count=$emailTables)." }

$seedDir = Join-Path $root "scripts/omnichannel-closure"
foreach ($seed in @("seed-closure-fixtures.sql", "seed-closure-subscription.sql")) {
    $path = Join-Path $seedDir $seed
    if (Test-Path $path) {
        Get-Content $path | docker exec -i $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d $db -q
    }
}

foreach ($seed in @("seed-email-closure-permissions.sql", "seed-email-channel-config.sql")) {
    $path = Join-Path $PSScriptRoot $seed
    if (Test-Path $path) {
        Get-Content $path | docker exec -i $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d $db -q
    }
}

$readStub = Join-Path $seedDir "patch-closure-ai-task-read-stubs.sql"
if (Test-Path $readStub) {
    Get-Content $readStub | docker exec -i $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d $db -q
}

$schemaPatch = Join-Path $seedDir "patch-closure-ai-tasks-schema.sql"
if (Test-Path $schemaPatch) {
    Get-Content $schemaPatch | docker exec -i $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d $db -q
}

Write-Host "EMAIL_CLOSURE_DATABASE=PASS"
