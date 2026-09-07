# Start API against isolated closure database for live verification.
$ErrorActionPreference = "Stop"
. "$PSScriptRoot/closure-constants.ps1"

$root = Resolve-Path "$PSScriptRoot/../.."
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:CREVAFACTOS__PERSISTENCE__ENABLED = "true"
$env:CREVAFACTOS__PERSISTENCE__HOST = $ClosurePostgresHost
$env:CREVAFACTOS__PERSISTENCE__PORT = "$ClosurePostgresPort"
$env:CREVAFACTOS__PERSISTENCE__DATABASE = $ClosureDatabaseName
$env:CREVAFACTOS__PERSISTENCE__USERNAME = $ClosureRuntimeUser
$env:CREVAFACTOS__PERSISTENCE__PASSWORD = "local_runtime_only_change_me"
$env:CREVAFACTOS__PERSISTENCE__SSL_MODE = "Disable"
$env:DevelopmentPilot__Enabled = "false"
$env:ASPNETCORE_URLS = "http://localhost:5199"

Write-Host "Starting closure API on port 5199 with database $ClosureDatabaseName ..."
Push-Location (Join-Path $root "apps/api")
dotnet run --no-build --no-launch-profile --urls "http://localhost:5199" 2>&1 | ForEach-Object { Write-Host $_ }
