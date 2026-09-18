# Email runtime closure-1 master orchestrator
$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "../..")
. "$PSScriptRoot/email-closure-constants.ps1"

$artifacts = Join-Path $root "artifacts/email-closure"
$logsDir = Join-Path $artifacts "logs"
New-Item -ItemType Directory -Force -Path $artifacts, $logsDir | Out-Null

function Stop-ClosureRuntime {
    $statePath = Join-Path $artifacts "runtime-state.json"
    if (-not (Test-Path $statePath)) { return }
    $state = Get-Content $statePath | ConvertFrom-Json
    foreach ($procId in @($state.webPid, $state.apiPid, $state.smtpDoublePid)) {
        if ($procId) {
            try { Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue } catch {}
        }
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
            $owner = Get-Process -Id $listener.OwningProcess -ErrorAction SilentlyContinue
            if ($owner -and $owner.ProcessName -eq "node") {
                Stop-Process -Id $listener.OwningProcess -Force -ErrorAction SilentlyContinue
                Start-Sleep -Seconds 1
            }
        }
    }
    $smtpLog = Join-Path $logsDir "smtp-double-$Mode.log"
    $smtpErr = Join-Path $logsDir "smtp-double-$Mode.err.log"
    $smtpEnv = @{
        NEVAXIS_FACTOS_EMAIL_TEST_DOUBLE_MODE = $Mode
        NEVAXIS_FACTOS_EMAIL_TEST_DOUBLE_PORT = "$EmailSmtpDoublePort"
        NEVAXIS_FACTOS_EMAIL_TEST_DOUBLE_HTTP_PORT = "$EmailSmtpDoublePreviewPort"
        NEVAXIS_FACTOS_EMAIL_CAPTURE_DIR = $artifacts
    }
    foreach ($k in $smtpEnv.Keys) { Set-Item -Path "env:$k" -Value ([string]$smtpEnv[$k]) }
    $proc = Start-Process node -ArgumentList "scripts/smtp-email-provider-test-double.mjs" -WorkingDirectory (Join-Path $root "apps/web") -PassThru -WindowStyle Hidden -RedirectStandardOutput $smtpLog -RedirectStandardError $smtpErr
    Start-Sleep -Seconds 2
    $ready = $false
    for ($i = 0; $i -lt 20; $i++) {
        try {
            $r = Invoke-WebRequest -Uri "http://127.0.0.1:$EmailSmtpDoublePreviewPort/health" -UseBasicParsing -TimeoutSec 2
            if ($r.StatusCode -eq 200) { $ready = $true; break }
        } catch {}
        Start-Sleep -Seconds 1
    }
    if (-not $ready) { throw "SMTP double restart failed mode=$Mode" }
    $State.smtpDoublePid = $proc.Id
    $statePath = Join-Path $artifacts "runtime-state.json"
    $State | ConvertTo-Json -Depth 4 | Set-Content -Path $statePath -Encoding UTF8
    Start-Sleep -Seconds 2
}

Write-Host "=== PRE-FLIGHT ==="
Push-Location $root
git status --short | Out-Host
if ((git diff --cached --name-only).Length -gt 0) { throw "STAGED must be 0" }
Pop-Location

Stop-ClosureRuntime

Write-Host "=== DATABASE SETUP ==="
& powershell -NoProfile -ExecutionPolicy Bypass -File "$PSScriptRoot/setup-isolated-database.ps1"
if ($LASTEXITCODE -ne 0) { throw "DB setup failed" }

Write-Host "=== START RUNTIME ==="
& powershell -NoProfile -ExecutionPolicy Bypass -File "$PSScriptRoot/email-closure-supervisor.ps1" --detach
if ($LASTEXITCODE -ne 0) { throw "Supervisor failed" }
Start-Sleep -Seconds 5

$state = Get-Content (Join-Path $artifacts "runtime-state.json") | ConvertFrom-Json
$apiBase = "http://127.0.0.1:$($state.apiPort)"
$webBase = "http://127.0.0.1:$($state.webPort)"
$env:NEVAXIS_FACTOS_API_BASE_URL = $apiBase
$env:NEVAXIS_FACTOS_WEB_BASE_URL = $webBase
$env:NEVAXIS_FACTOS_EMAIL_TEST_DOUBLE_HTTP_URL = "http://127.0.0.1:$EmailSmtpDoublePreviewPort"

Write-Host "=== GOLDEN FLOW ==="
node (Join-Path $root "apps/web/scripts/verify-ai-task-email-delivery-adapter-1-golden-flow.mjs")
if ($LASTEXITCODE -ne 0) { Stop-ClosureRuntime; throw "Golden flow failed" }

Write-Host "=== IDEMPOTENCY / CROSS-TENANT ==="
node (Join-Path $root "apps/web/scripts/verify-ai-task-email-delivery-adapter-1-runtime-flows.mjs") --phase=idempotency
if ($LASTEXITCODE -ne 0) { Stop-ClosureRuntime; throw "Idempotency failed" }
node (Join-Path $root "apps/web/scripts/verify-ai-task-email-delivery-adapter-1-runtime-flows.mjs") --phase=cross-tenant
if ($LASTEXITCODE -ne 0) { Stop-ClosureRuntime; throw "Cross-tenant failed" }

Write-Host "=== RETRY FLOW ==="
Restart-SmtpDouble -Mode "temporary-failure" -State $state
Start-Sleep -Seconds 3
node (Join-Path $root "apps/web/scripts/verify-ai-task-email-delivery-adapter-1-runtime-flows.mjs") --phase=retry-fail
if ($LASTEXITCODE -ne 0) {
    Restart-SmtpDouble -Mode "accept" -State $state
    Stop-ClosureRuntime
    throw "Retry fail phase failed"
}
Restart-SmtpDouble -Mode "accept" -State $state
Start-Sleep -Seconds 3
node (Join-Path $root "apps/web/scripts/verify-ai-task-email-delivery-adapter-1-runtime-flows.mjs") --phase=retry-complete
if ($LASTEXITCODE -ne 0) {
    Restart-SmtpDouble -Mode "accept" -State $state
    Stop-ClosureRuntime
    throw "Retry flow failed"
}
Restart-SmtpDouble -Mode "accept" -State $state

Write-Host "=== PERMANENT FAILURE FLOW ==="
Restart-SmtpDouble -Mode "permanent-recipient" -State $state
node (Join-Path $root "apps/web/scripts/verify-ai-task-email-delivery-adapter-1-runtime-flows.mjs") --phase=permanent
if ($LASTEXITCODE -ne 0) {
    Restart-SmtpDouble -Mode "accept" -State $state
    Stop-ClosureRuntime
    throw "Permanent failure flow failed"
}
Restart-SmtpDouble -Mode "accept" -State $state

Write-Host "=== SCREENSHOTS ==="
node (Join-Path $root "apps/web/scripts/capture-ai-task-email-delivery-adapter-1-screenshots.mjs")
if ($LASTEXITCODE -ne 0) { Stop-ClosureRuntime; throw "Screenshot capture failed" }

Write-Host "=== CLASSIFICATION FLOWS ==="
docker exec $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d $EmailClosureDatabaseName -c "DELETE FROM ai_task_delivery_attempts WHERE delivery_id IN (SELECT id FROM ai_task_deliveries WHERE channel = 'email'); DELETE FROM ai_task_deliveries WHERE channel = 'email';" | Out-Null
Restart-SmtpDouble -Mode "timeout" -State $state
Start-Sleep -Seconds 3
node (Join-Path $root "apps/web/scripts/verify-ai-task-email-delivery-adapter-1-runtime-flows.mjs") --phase=timeout
if ($LASTEXITCODE -ne 0) { Stop-ClosureRuntime; throw "Timeout classification failed" }

docker exec $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d $EmailClosureDatabaseName -c "UPDATE ai_task_email_channel_configurations SET security_mode = 'tls' WHERE tenant_id = 'bb000001-0001-4000-8000-000000000001';" | Out-Null
Restart-SmtpDouble -Mode "tls-config" -State $state
Start-Sleep -Seconds 2
node (Join-Path $root "apps/web/scripts/verify-ai-task-email-delivery-adapter-1-runtime-flows.mjs") --phase=tls
if ($LASTEXITCODE -ne 0) { Stop-ClosureRuntime; throw "TLS classification failed" }
docker exec $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d $EmailClosureDatabaseName -c "UPDATE ai_task_email_channel_configurations SET security_mode = 'none', readiness_status = 'ready' WHERE tenant_id = 'bb000001-0001-4000-8000-000000000001';" | Out-Null

Restart-SmtpDouble -Mode "authentication-failure" -State $state
node (Join-Path $root "apps/web/scripts/verify-ai-task-email-delivery-adapter-1-runtime-flows.mjs") --phase=auth
if ($LASTEXITCODE -ne 0) { Stop-ClosureRuntime; throw "Auth classification failed" }

Restart-SmtpDouble -Mode "accept" -State $state

Write-Host "=== NOT-CONFIGURED SCREENSHOT ==="
docker exec $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d $EmailClosureDatabaseName -c "DELETE FROM ai_task_email_channel_configurations WHERE tenant_id = 'bb000001-0001-4000-8000-000000000001';" | Out-Null
Stop-Process -Id $state.apiPid -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 3
$apiLog = Join-Path $logsDir "closure-api-no-email.log"
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
$apiNoEmail = Start-Process dotnet -ArgumentList "run --no-launch-profile --project `"$(Join-Path $root 'apps/api')`" --urls $apiBase" -PassThru -WindowStyle Hidden -RedirectStandardOutput $apiLog -RedirectStandardError (Join-Path $logsDir "closure-api-no-email.err.log")
$deadline = (Get-Date).AddSeconds(90)
$ready = $false
while ((Get-Date) -lt $deadline) {
    try {
        $r = Invoke-WebRequest -Uri "$apiBase/health/ready" -UseBasicParsing -TimeoutSec 3
        if ($r.StatusCode -eq 200) { $ready = $true; break }
    } catch {}
    Start-Sleep -Seconds 2
}
if (-not $ready) { throw "API without Email failed to start" }
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
Pop-Location
dotnet test tests/NevaxisZentra.Persistence.Tests --filter "FullyQualifiedName~Email|FullyQualifiedName~WhatsApp|FullyQualifiedName~AiTaskOmnichannel" -v q
if ($LASTEXITCODE -ne 0) { throw "tests failed" }
node apps/web/scripts/verify-ai-task-email-delivery-adapter-1-contract.mjs
if ($LASTEXITCODE -ne 0) { throw "contract failed" }
$env:NEVAXIS_FACTOS_EMAIL_MIGRATION_VERIFY_DB = $EmailClosureDatabaseName
node apps/web/scripts/verify-ai-task-email-delivery-adapter-1-migration.mjs
if ($LASTEXITCODE -ne 0) { throw "migration verifier failed" }
Pop-Location

Write-Host "=== PRISTINE DB ==="
$pristine = $EmailClosurePristineDatabaseName
docker exec $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d postgres -c "DROP DATABASE IF EXISTS $pristine WITH (FORCE);" | Out-Null
docker exec $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d postgres -c "CREATE DATABASE $pristine;" | Out-Null
$env:NEVAXIS_FACTOS__MIGRATIONS__HOST = $EmailClosurePostgresHost
$env:NEVAXIS_FACTOS__MIGRATIONS__PORT = "$EmailClosurePostgresPort"
$env:NEVAXIS_FACTOS__MIGRATIONS__DATABASE = $pristine
$env:NEVAXIS_FACTOS__MIGRATIONS__USERNAME = $EmailClosureMigrationUser
$env:NEVAXIS_FACTOS__MIGRATIONS__PASSWORD = "local_dev_only_change_me"
$env:NEVAXIS_FACTOS__MIGRATIONS__SSL_MODE = "Disable"
Push-Location $root
dotnet tool run dotnet-ef -- database update --project packages/NevaxisZentra.Persistence.Migrations/NevaxisZentra.Persistence.Migrations.csproj --startup-project packages/NevaxisZentra.Persistence.Migrations/NevaxisZentra.Persistence.Migrations.csproj --context NevaxisZentraDbContext
if ($LASTEXITCODE -ne 0) { throw "pristine migration failed" }
$emailMigration = docker exec $EmailClosurePostgresContainer psql -U $EmailClosureMigrationUser -d $pristine -t -A -c "SELECT COUNT(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '20260908120000_AddAiTaskEmailDeliveryAdapterFoundation'"
if (($emailMigration | Out-String).Trim() -ne "1") { throw "Email migration not applied to pristine DB" }
Pop-Location
Write-Host "PRISTINE_DATABASE_MIGRATION=PASS"

Stop-ClosureRuntime
Write-Host "EMAIL_CLOSURE_RUNTIME_SUPERVISION=PASS"
Write-Host "EMAIL_RUNTIME_CLOSURE_1=PASS"
