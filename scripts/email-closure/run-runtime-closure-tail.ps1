# Continue email closure from screenshots/classification (DB already seeded).
$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "../..")
. "$PSScriptRoot/email-closure-constants.ps1"
$artifacts = Join-Path $root "artifacts/email-closure"
$logsDir = Join-Path $artifacts "logs"
$apiBase = "http://127.0.0.1:$EmailClosureApiPort"
$webBase = "http://127.0.0.1:$EmailClosureWebPort"
$env:NEVAXIS_FACTOS_API_BASE_URL = $apiBase
$env:NEVAXIS_FACTOS_WEB_BASE_URL = $webBase
$env:NEVAXIS_FACTOS_EMAIL_TEST_DOUBLE_HTTP_URL = "http://127.0.0.1:$EmailSmtpDoublePreviewPort"

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
        NEVAXIS_FACTOS_EMAIL_TEST_DOUBLE_MODE = $Mode
        NEVAXIS_FACTOS_EMAIL_TEST_DOUBLE_PORT = "$EmailSmtpDoublePort"
        NEVAXIS_FACTOS_EMAIL_TEST_DOUBLE_HTTP_PORT = "$EmailSmtpDoublePreviewPort"
        NEVAXIS_FACTOS_EMAIL_CAPTURE_DIR = $artifacts
    }
    foreach ($k in $smtpEnv.Keys) { Set-Item -Path "env:$k" -Value ([string]$smtpEnv[$k]) }
    $proc = Start-Process node -ArgumentList "scripts/smtp-email-provider-test-double.mjs" -WorkingDirectory (Join-Path $root "apps/web") -PassThru -WindowStyle Hidden
    Start-Sleep -Seconds 2
    $State.smtpDoublePid = $proc.Id
    $State | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $artifacts "runtime-state.json") -Encoding UTF8
}

Stop-ClosureRuntime
& "$PSScriptRoot/email-closure-supervisor.ps1" --detach
Start-Sleep -Seconds 5
$state = Get-Content (Join-Path $artifacts "runtime-state.json") | ConvertFrom-Json

Write-Host "=== CLASSIFICATION FLOWS ==="
Get-Content (Join-Path $PSScriptRoot "seed-email-channel-config.sql") | docker exec -i $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d $EmailClosureDatabaseName | Out-Null
docker exec $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d $EmailClosureDatabaseName -c "DELETE FROM ai_task_delivery_attempts WHERE delivery_id IN (SELECT id FROM ai_task_deliveries WHERE channel = 'email'); DELETE FROM ai_task_deliveries WHERE channel = 'email';" | Out-Null
Restart-SmtpDouble -Mode "timeout" -State $state
node (Join-Path $root "apps/web/scripts/verify-ai-task-email-delivery-adapter-1-runtime-flows.mjs") --phase=timeout
if ($LASTEXITCODE -ne 0) { Stop-ClosureRuntime; throw "Timeout failed" }

docker exec $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d $EmailClosureDatabaseName -c "UPDATE ai_task_email_channel_configurations SET security_mode = 'tls' WHERE tenant_id = 'bb000001-0001-4000-8000-000000000001';" | Out-Null
Restart-SmtpDouble -Mode "tls-config" -State $state
node (Join-Path $root "apps/web/scripts/verify-ai-task-email-delivery-adapter-1-runtime-flows.mjs") --phase=tls
if ($LASTEXITCODE -ne 0) { Stop-ClosureRuntime; throw "TLS failed" }
docker exec $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d $EmailClosureDatabaseName -c "UPDATE ai_task_email_channel_configurations SET security_mode = 'none', readiness_status = 'ready' WHERE tenant_id = 'bb000001-0001-4000-8000-000000000001';" | Out-Null

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
    NEVAXIS_FACTOS__PERSISTENCE__ENABLED = "true"
    NEVAXIS_FACTOS__PERSISTENCE__HOST = $EmailClosurePostgresHost
    NEVAXIS_FACTOS__PERSISTENCE__PORT = "$EmailClosurePostgresPort"
    NEVAXIS_FACTOS__PERSISTENCE__DATABASE = $EmailClosureDatabaseName
    NEVAXIS_FACTOS__PERSISTENCE__USERNAME = $EmailClosureRuntimeUser
    NEVAXIS_FACTOS__PERSISTENCE__PASSWORD = "local_runtime_only_change_me"
    NEVAXIS_FACTOS__PERSISTENCE__SSL_MODE = "Disable"
    DevelopmentPilot__Enabled = "false"
    NEVAXIS_FACTOS__AI_TASK_DELIVERY__WORKER_ENABLED = "true"
    NEVAXIS_FACTOS__AI_TASK_DELIVERY__EMAIL__ENABLED = "false"
    ASPNETCORE_URLS = $apiBase
}
foreach ($k in $apiNoEmailEnv.Keys) { Set-Item -Path "env:$k" -Value ([string]$apiNoEmailEnv[$k]) }
Remove-Item Env:NEVAXIS_FACTOS__AI_TASK_DELIVERY__EMAIL__TEST_SMTP__HOST -ErrorAction SilentlyContinue
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
Get-Content (Join-Path $PSScriptRoot "seed-email-channel-config.sql") | docker exec -i $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d $EmailClosureDatabaseName | Out-Null

node (Join-Path $root "apps/web/scripts/verify-ai-task-email-delivery-adapter-1-runtime-flows.mjs") --phase=security-scan
Push-Location $root
dotnet test tests/NevaxisZentra.Persistence.Tests --filter "FullyQualifiedName~Email|FullyQualifiedName~WhatsApp|FullyQualifiedName~AiTaskOmnichannel" -v q
node apps/web/scripts/verify-ai-task-email-delivery-adapter-1-contract.mjs
$env:NEVAXIS_FACTOS_EMAIL_MIGRATION_VERIFY_DB = $EmailClosureDatabaseName
node apps/web/scripts/verify-ai-task-email-delivery-adapter-1-migration.mjs
$pristine = $EmailClosurePristineDatabaseName
docker exec $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d postgres -c "DROP DATABASE IF EXISTS $pristine WITH (FORCE);" | Out-Null
docker exec $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d postgres -c "CREATE DATABASE $pristine;" | Out-Null
$env:NEVAXIS_FACTOS__MIGRATIONS__HOST = $EmailClosurePostgresHost
$env:NEVAXIS_FACTOS__MIGRATIONS__PORT = "$EmailClosurePostgresPort"
$env:NEVAXIS_FACTOS__MIGRATIONS__DATABASE = $pristine
$env:NEVAXIS_FACTOS__MIGRATIONS__USERNAME = $EmailClosureMigrationUser
$env:NEVAXIS_FACTOS__MIGRATIONS__PASSWORD = "local_dev_only_change_me"
$env:NEVAXIS_FACTOS__MIGRATIONS__SSL_MODE = "Disable"
$env:NEVAXIS_FACTOS__MIGRATIONS__COMMAND_TIMEOUT_SECONDS = "300"
dotnet tool run dotnet-ef -- database update --project packages/NevaxisZentra.Persistence.Migrations/NevaxisZentra.Persistence.Migrations.csproj --startup-project packages/NevaxisZentra.Persistence.Migrations/NevaxisZentra.Persistence.Migrations.csproj --context NevaxisZentraDbContext
Pop-Location
Stop-ClosureRuntime
Write-Host "EMAIL_RUNTIME_CLOSURE_TAIL=PASS"
