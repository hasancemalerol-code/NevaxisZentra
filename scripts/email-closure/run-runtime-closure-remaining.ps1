# Complete remaining email closure steps (assumes timeout already proven).
$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "../..")
. "$PSScriptRoot/email-closure-constants.ps1"
$artifacts = Join-Path $root "artifacts/email-closure"
$apiBase = "http://127.0.0.1:$EmailClosureApiPort"
$webBase = "http://127.0.0.1:$EmailClosureWebPort"
$env:CREVAFACTOS_API_BASE_URL = $apiBase
$env:CREVAFACTOS_WEB_BASE_URL = $webBase
$env:CREVAFACTOS_EMAIL_TEST_DOUBLE_HTTP_URL = "http://127.0.0.1:$EmailSmtpDoublePreviewPort"

function Stop-ClosureRuntime {
    $statePath = Join-Path $artifacts "runtime-state.json"
    if (-not (Test-Path $statePath)) { return }
    $state = Get-Content $statePath | ConvertFrom-Json
    foreach ($procId in @($state.webPid, $state.apiPid, $state.smtpDoublePid)) {
        if ($procId) { try { Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue } catch {} }
    }
}

function Restart-SmtpDouble {
    param([string]$Mode, [object]$State)
    if ($State.smtpDoublePid) {
        Stop-Process -Id $State.smtpDoublePid -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
    }
    foreach ($port in @($EmailSmtpDoublePort, $EmailSmtpDoublePreviewPort)) {
        $listener = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($listener) {
            Stop-Process -Id $listener.OwningProcess -Force -ErrorAction SilentlyContinue -ErrorVariable _ | Out-Null
        }
    }
    $smtpEnv = @{
        CREVAFACTOS_EMAIL_TEST_DOUBLE_MODE = $Mode
        CREVAFACTOS_EMAIL_TEST_DOUBLE_PORT = "$EmailSmtpDoublePort"
        CREVAFACTOS_EMAIL_TEST_DOUBLE_HTTP_PORT = "$EmailSmtpDoublePreviewPort"
        CREVAFACTOS_EMAIL_CAPTURE_DIR = $artifacts
    }
    foreach ($k in $smtpEnv.Keys) { Set-Item -Path "env:$k" -Value ([string]$smtpEnv[$k]) }
    $proc = Start-Process node -ArgumentList "scripts/smtp-email-provider-test-double.mjs" -WorkingDirectory (Join-Path $root "apps/web") -PassThru -WindowStyle Hidden
    Start-Sleep -Seconds 2
    $State.smtpDoublePid = $proc.Id
    $State | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $artifacts "runtime-state.json") -Encoding UTF8
}

Write-Host "=== RESEED EMAIL CONFIG ==="
Get-Content (Join-Path $PSScriptRoot "seed-email-channel-config.sql") | docker exec -i $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d $EmailClosureDatabaseName | Out-Null

Stop-ClosureRuntime
& "$PSScriptRoot/email-closure-supervisor.ps1" --detach
Start-Sleep -Seconds 5
$state = Get-Content (Join-Path $artifacts "runtime-state.json") | ConvertFrom-Json

Write-Host "=== TLS CLASSIFICATION ==="
docker exec $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d $EmailClosureDatabaseName -c "DELETE FROM ai_task_delivery_attempts WHERE delivery_id IN (SELECT id FROM ai_task_deliveries WHERE channel = 'email'); DELETE FROM ai_task_deliveries WHERE channel = 'email';" | Out-Null
docker exec $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d $EmailClosureDatabaseName -c "UPDATE ai_task_email_channel_configurations SET security_mode = 'tls', readiness_status = 'ready' WHERE tenant_id = 'bb000001-0001-4000-8000-000000000001';" | Out-Null
Restart-SmtpDouble -Mode "accept" -State $state
node (Join-Path $root "apps/web/scripts/verify-ai-task-email-delivery-adapter-1-runtime-flows.mjs") --phase=tls
if ($LASTEXITCODE -ne 0) { Stop-ClosureRuntime; throw "TLS failed" }
docker exec $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d $EmailClosureDatabaseName -c "UPDATE ai_task_email_channel_configurations SET security_mode = 'none', readiness_status = 'ready' WHERE tenant_id = 'bb000001-0001-4000-8000-000000000001';" | Out-Null

Write-Host "=== AUTH CLASSIFICATION ==="
Restart-SmtpDouble -Mode "authentication-failure" -State $state
node (Join-Path $root "apps/web/scripts/verify-ai-task-email-delivery-adapter-1-runtime-flows.mjs") --phase=auth
if ($LASTEXITCODE -ne 0) { Stop-ClosureRuntime; throw "Auth failed" }
Restart-SmtpDouble -Mode "accept" -State $state

Write-Host "=== NOT-CONFIGURED SCREENSHOT ==="
docker exec $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d $EmailClosureDatabaseName -c "DELETE FROM ai_task_email_channel_configurations WHERE tenant_id = 'bb000001-0001-4000-8000-000000000001';" | Out-Null
Stop-Process -Id $state.apiPid -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 3
$apiNoEmailEnv = @{
    ASPNETCORE_ENVIRONMENT = "Development"
    CREVAFACTOS__PERSISTENCE__ENABLED = "true"
    CREVAFACTOS__PERSISTENCE__HOST = $EmailClosurePostgresHost
    CREVAFACTOS__PERSISTENCE__PORT = "$EmailClosurePostgresPort"
    CREVAFACTOS__PERSISTENCE__DATABASE = $EmailClosureDatabaseName
    CREVAFACTOS__PERSISTENCE__USERNAME = $EmailClosureRuntimeUser
    CREVAFACTOS__PERSISTENCE__PASSWORD = "local_runtime_only_change_me"
    CREVAFACTOS__PERSISTENCE__SSL_MODE = "Disable"
    DevelopmentPilot__Enabled = "false"
    CREVAFACTOS__AI_TASK_DELIVERY__WORKER_ENABLED = "true"
    CREVAFACTOS__AI_TASK_DELIVERY__EMAIL__ENABLED = "false"
    ASPNETCORE_URLS = $apiBase
}
foreach ($k in $apiNoEmailEnv.Keys) { Set-Item -Path "env:$k" -Value ([string]$apiNoEmailEnv[$k]) }
Remove-Item Env:CREVAFACTOS__AI_TASK_DELIVERY__EMAIL__TEST_SMTP__HOST -ErrorAction SilentlyContinue
$apiNoEmail = Start-Process dotnet -ArgumentList "run --no-launch-profile --project `"$(Join-Path $root 'apps/api')`" --urls $apiBase" -PassThru -WindowStyle Hidden
$deadline = (Get-Date).AddSeconds(90)
$ready = $false
while ((Get-Date) -lt $deadline) {
    try {
        if ((Invoke-WebRequest -Uri "$apiBase/health/ready" -UseBasicParsing -TimeoutSec 3).StatusCode -eq 200) { $ready = $true; break }
    } catch {}
    Start-Sleep -Seconds 2
}
if (-not $ready) { Stop-ClosureRuntime; throw "API without Email failed to start" }
node (Join-Path $root "apps/web/scripts/capture-ai-task-email-not-configured-screenshot.mjs")
if ($LASTEXITCODE -ne 0) { Stop-Process -Id $apiNoEmail.Id -Force -ErrorAction SilentlyContinue; Stop-ClosureRuntime; throw "Not-configured screenshot failed" }
Stop-Process -Id $apiNoEmail.Id -Force -ErrorAction SilentlyContinue

Write-Host "=== SECURITY SCAN ==="
node (Join-Path $root "apps/web/scripts/verify-ai-task-email-delivery-adapter-1-runtime-flows.mjs") --phase=security-scan
if ($LASTEXITCODE -ne 0) { Stop-ClosureRuntime; throw "Security scan failed" }

Write-Host "=== REGRESSION ==="
Push-Location $root
dotnet build -v q
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }
Push-Location (Join-Path $root "apps/web")
npm run typecheck
if ($LASTEXITCODE -ne 0) { throw "web typecheck failed" }
npm run build
if ($LASTEXITCODE -ne 0) { throw "web build failed" }
Pop-Location
dotnet test tests/CrevaFactOS.Persistence.Tests --filter "FullyQualifiedName~Email|FullyQualifiedName~WhatsApp|FullyQualifiedName~AiTaskOmnichannel" -v q
if ($LASTEXITCODE -ne 0) { throw "tests failed" }
node apps/web/scripts/verify-ai-task-email-delivery-adapter-1-contract.mjs
if ($LASTEXITCODE -ne 0) { throw "contract failed" }
$env:CREVAFACTOS_EMAIL_MIGRATION_VERIFY_DB = $EmailClosureDatabaseName
node apps/web/scripts/verify-ai-task-email-delivery-adapter-1-migration.mjs
if ($LASTEXITCODE -ne 0) { throw "migration verifier failed" }
Pop-Location

Write-Host "=== PRISTINE DB ==="
$pristine = $EmailClosurePristineDatabaseName
docker exec $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d postgres -c "DROP DATABASE IF EXISTS $pristine WITH (FORCE);" | Out-Null
docker exec $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d postgres -c "CREATE DATABASE $pristine;" | Out-Null
$env:CREVAFACTOS__MIGRATIONS__HOST = $EmailClosurePostgresHost
$env:CREVAFACTOS__MIGRATIONS__PORT = "$EmailClosurePostgresPort"
$env:CREVAFACTOS__MIGRATIONS__DATABASE = $pristine
$env:CREVAFACTOS__MIGRATIONS__USERNAME = $EmailClosureMigrationUser
$env:CREVAFACTOS__MIGRATIONS__PASSWORD = "local_dev_only_change_me"
$env:CREVAFACTOS__MIGRATIONS__SSL_MODE = "Disable"
$env:CREVAFACTOS__MIGRATIONS__COMMAND_TIMEOUT_SECONDS = "300"
dotnet tool run dotnet-ef -- database update --project packages/CrevaFactOS.Persistence.Migrations/CrevaFactOS.Persistence.Migrations.csproj --startup-project packages/CrevaFactOS.Persistence.Migrations/CrevaFactOS.Persistence.Migrations.csproj --context CrevaFactOSDbContext
if ($LASTEXITCODE -ne 0) { Stop-ClosureRuntime; throw "pristine migration failed" }

Stop-ClosureRuntime
Write-Host "EMAIL_RUNTIME_CLOSURE_REMAINING=PASS"
