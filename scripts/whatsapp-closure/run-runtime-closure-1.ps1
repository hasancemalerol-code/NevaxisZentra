# WhatsApp runtime closure-1 master orchestrator
$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "../..")
. "$PSScriptRoot/whatsapp-closure-constants.ps1"

$artifacts = Join-Path $root "artifacts/whatsapp-closure"
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null

function Stop-ClosureRuntime {
    $statePath = Join-Path $artifacts "runtime-state.json"
    if (-not (Test-Path $statePath)) { return }
    $state = Get-Content $statePath | ConvertFrom-Json
    foreach ($procId in @($state.webPid, $state.apiPid, $state.providerDoublePid)) {
        if ($procId) {
            try { Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue } catch {}
        }
    }
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
& powershell -NoProfile -ExecutionPolicy Bypass -File "$PSScriptRoot/whatsapp-closure-supervisor.ps1" --detach
if ($LASTEXITCODE -ne 0) { throw "Supervisor failed" }
Start-Sleep -Seconds 5

$state = Get-Content (Join-Path $artifacts "runtime-state.json") | ConvertFrom-Json
$apiBase = "http://127.0.0.1:$($state.apiPort)"
$webBase = "http://127.0.0.1:$($state.webPort)"
$env:NEVAXIS_FACTOS_API_BASE_URL = $apiBase
$env:NEVAXIS_FACTOS_WEB_BASE_URL = $webBase
$env:NEVAXIS_FACTOS__AI_TASK_DELIVERY__WHATSAPP__APP_SECRET = "whatsapp-test-app-secret"
$env:NEVAXIS_FACTOS__AI_TASK_DELIVERY__WHATSAPP__WEBHOOK_VERIFY_TOKEN = "whatsapp-test-verify-token"

Write-Host "=== CHANNEL READINESS ==="
$channels = Invoke-RestMethod -Uri "$apiBase/api/ai-task-delivery/channels?facilityId=bb000001-0005-4000-8000-000000000005" -Headers @{
    "X-crevafactos-dev-Issuer" = "crevafactos-dev"
    "X-crevafactos-dev-Subject" = "planning-pilot-user"
    "X-NevaxisZentra-Tenant" = "omnichannel-closure-tenant-a"
}
$wa = $channels.channels | Where-Object { $_.channelKey -eq "whatsapp" } | Select-Object -First 1
if (-not $wa -or -not $wa.available) { throw "WHATSAPP_CHANNEL_READINESS failed: $($wa | ConvertTo-Json -Compress)" }
Write-Host "WHATSAPP_CHANNEL_READINESS_HTTP_STATUS=200"
Write-Host "WHATSAPP_CHANNEL_READINESS=PASS"

Write-Host "=== GOLDEN FLOW ==="
node (Join-Path $root "apps/web/scripts/verify-ai-task-whatsapp-business-adapter-1-golden-flow.mjs")
if ($LASTEXITCODE -ne 0) { Stop-ClosureRuntime; throw "Golden flow failed" }

Write-Host "=== WEBHOOK LIVE PROOFS ==="
node (Join-Path $root "apps/web/scripts/verify-ai-task-whatsapp-business-adapter-1-webhook-live.mjs")
if ($LASTEXITCODE -ne 0) { Stop-ClosureRuntime; throw "Webhook live proofs failed" }

Write-Host "=== FAILURE DELIVERY SETUP ==="
Stop-Process -Id $state.providerDoublePid -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
$failDoubleLog = Join-Path $artifacts "logs/provider-double-invalid.log"
$failDoubleErr = Join-Path $artifacts "logs/provider-double-invalid.err.log"
$failEnv = @{ NEVAXIS_FACTOS_WHATSAPP_TEST_DOUBLE_MODE = "invalid-recipient" }
foreach ($k in $failEnv.Keys) { Set-Item -Path "env:$k" -Value $failEnv[$k] }
$failDouble = Start-Process node -ArgumentList "scripts/meta-whatsapp-provider-test-double.mjs" -WorkingDirectory (Join-Path $root "apps/web") -PassThru -WindowStyle Hidden -RedirectStandardOutput $failDoubleLog -RedirectStandardError $failDoubleErr
Start-Sleep -Seconds 2
$failKey = "whatsapp-failure-proof-$([DateTimeOffset]::UtcNow.ToUnixTimeSeconds())"
$issue = Invoke-RestMethod -Method POST -Uri "$apiBase/api/ai-tasks/bb000001-03d0-4000-8000-0000000003d0/secure-access?facilityId=bb000001-0005-4000-8000-000000000005" -Headers @{
    "X-crevafactos-dev-Issuer"="crevafactos-dev"; "X-crevafactos-dev-Subject"="planning-pilot-user"; "X-NevaxisZentra-Tenant"="omnichannel-closure-tenant-a"; "Content-Type"="application/json"
} -Body '{"ttlPolicy":"24h","scope":"read-only"}'
$failEnqueue = Invoke-RestMethod -Method POST -Uri "$apiBase/api/ai-tasks/bb000001-03d0-4000-8000-0000000003d0/deliveries?facilityId=bb000001-0005-4000-8000-000000000005" -Headers @{
    "X-crevafactos-dev-Issuer"="crevafactos-dev"; "X-crevafactos-dev-Subject"="planning-pilot-user"; "X-NevaxisZentra-Tenant"="omnichannel-closure-tenant-a"; "Content-Type"="application/json"; "Idempotency-Key"=$failKey
} -Body (@{
    channel="whatsapp"; recipientKind="external"; maskedRecipient="+90 5** *** ** 99"; recipientE164="+905551112234"
    consentStatus="known"; secureAccessGrantId=$issue.grant.id; secureTaskUrl="$webBase/gorev#access=opaque"
} | ConvertTo-Json)
Start-Sleep -Seconds 15
Stop-Process -Id $failDouble.Id -Force -ErrorAction SilentlyContinue
Remove-Item Env:NEVAXIS_FACTOS_WHATSAPP_TEST_DOUBLE_MODE -ErrorAction SilentlyContinue
$acceptDoubleLog = Join-Path $artifacts "logs/provider-double-accept.log"
$acceptDoubleErr = Join-Path $artifacts "logs/provider-double-accept.err.log"
$acceptDouble = Start-Process node -ArgumentList "scripts/meta-whatsapp-provider-test-double.mjs" -WorkingDirectory (Join-Path $root "apps/web") -PassThru -WindowStyle Hidden -RedirectStandardOutput $acceptDoubleLog -RedirectStandardError $acceptDoubleErr
Start-Sleep -Seconds 2
$state.providerDoublePid = $acceptDouble.Id

Write-Host "=== SCREENSHOTS (configured) ==="
node (Join-Path $root "apps/web/scripts/capture-ai-task-whatsapp-business-adapter-1-screenshots.mjs")
if ($LASTEXITCODE -ne 0) { Stop-ClosureRuntime; throw "Screenshot capture failed" }

Write-Host "=== NOT-CONFIGURED SCREENSHOT ==="
Stop-Process -Id $state.apiPid -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 3
$apiLog = Join-Path $artifacts "logs/closure-api-no-wa.log"
$apiNoWaEnv = @{
    ASPNETCORE_ENVIRONMENT = "Development"
    NEVAXIS_FACTOS__PERSISTENCE__ENABLED = "true"
    NEVAXIS_FACTOS__PERSISTENCE__HOST = $WhatsAppClosurePostgresHost
    NEVAXIS_FACTOS__PERSISTENCE__PORT = "$WhatsAppClosurePostgresPort"
    NEVAXIS_FACTOS__PERSISTENCE__DATABASE = $WhatsAppClosureDatabaseName
    NEVAXIS_FACTOS__PERSISTENCE__USERNAME = $WhatsAppClosureRuntimeUser
    NEVAXIS_FACTOS__PERSISTENCE__PASSWORD = "local_runtime_only_change_me"
    NEVAXIS_FACTOS__PERSISTENCE__SSL_MODE = "Disable"
    DevelopmentPilot__Enabled = "false"
    NEVAXIS_FACTOS__AI_TASK_DELIVERY__WORKER_ENABLED = "true"
    ASPNETCORE_URLS = $apiBase
}
foreach ($k in $apiNoWaEnv.Keys) { Set-Item -Path "env:$k" -Value ([string]$apiNoWaEnv[$k]) }
Remove-Item Env:NEVAXIS_FACTOS__AI_TASK_DELIVERY__WHATSAPP__ENABLED -ErrorAction SilentlyContinue
Remove-Item Env:NEVAXIS_FACTOS__AI_TASK_DELIVERY__WHATSAPP__TEST_API_BASE_URL -ErrorAction SilentlyContinue
Remove-Item Env:NEVAXIS_FACTOS__AI_TASK_DELIVERY__WHATSAPP__ACCESS_TOKEN -ErrorAction SilentlyContinue
$apiNoWa = Start-Process dotnet -ArgumentList "run --no-launch-profile --project `"$(Join-Path $root 'apps/api')`" --urls $apiBase" -PassThru -WindowStyle Hidden -RedirectStandardOutput $apiLog -RedirectStandardError (Join-Path $artifacts "logs/closure-api-no-wa.err.log")
$deadline = (Get-Date).AddSeconds(90)
$ready = $false
while ((Get-Date) -lt $deadline) {
    try {
        $r = Invoke-WebRequest -Uri "$apiBase/health/ready" -UseBasicParsing -TimeoutSec 3
        if ($r.StatusCode -eq 200) { $ready = $true; break }
    } catch {}
    Start-Sleep -Seconds 2
}
if (-not $ready) { throw "API without WhatsApp failed to start" }
# Capture only screenshot 10 via node one-liner would be heavy; re-run partial capture with env flag
$env:NEVAXIS_FACTOS_WHATSAPP_CAPTURE_NOT_CONFIGURED_ONLY = "1"
node (Join-Path $root "apps/web/scripts/capture-ai-task-whatsapp-not-configured-screenshot.mjs")
if ($LASTEXITCODE -ne 0) { Stop-Process -Id $apiNoWa.Id -Force -ErrorAction SilentlyContinue; Stop-ClosureRuntime; throw "Not-configured screenshot failed" }
Stop-Process -Id $apiNoWa.Id -Force -ErrorAction SilentlyContinue

Write-Host "=== REGRESSION ==="
Push-Location $root
dotnet build -v q
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }
dotnet test tests/NevaxisZentra.Persistence.Tests --filter "FullyQualifiedName~WhatsApp|FullyQualifiedName~AiTaskOmnichannel" -v q
if ($LASTEXITCODE -ne 0) { throw "tests failed" }
node apps/web/scripts/verify-ai-task-whatsapp-business-adapter-1-contract.mjs
if ($LASTEXITCODE -ne 0) { throw "contract failed" }
$env:NEVAXIS_FACTOS_WHATSAPP_MIGRATION_VERIFY_DB = $WhatsAppClosureDatabaseName
node apps/web/scripts/verify-ai-task-whatsapp-business-adapter-1-migration.mjs
if ($LASTEXITCODE -ne 0) { throw "migration verifier failed" }
Pop-Location

Write-Host "=== PRISTINE DB ==="
$pristine = "NEVAXIS_FACTOS_whatsapp_pristine_closure_1"
docker exec $WhatsAppClosurePostgresContainer psql -U $WhatsAppClosureMigrationUser -d postgres -c "DROP DATABASE IF EXISTS $pristine WITH (FORCE);" | Out-Null
docker exec $WhatsAppClosurePostgresContainer psql -U $WhatsAppClosureMigrationUser -d postgres -c "CREATE DATABASE $pristine;" | Out-Null
$env:NEVAXIS_FACTOS__MIGRATIONS__HOST = $WhatsAppClosurePostgresHost
$env:NEVAXIS_FACTOS__MIGRATIONS__PORT = "$WhatsAppClosurePostgresPort"
$env:NEVAXIS_FACTOS__MIGRATIONS__DATABASE = $pristine
$env:NEVAXIS_FACTOS__MIGRATIONS__USERNAME = $WhatsAppClosureMigrationUser
$env:NEVAXIS_FACTOS__MIGRATIONS__PASSWORD = "local_dev_only_change_me"
$env:NEVAXIS_FACTOS__MIGRATIONS__SSL_MODE = "Disable"
Push-Location $root
dotnet tool run dotnet-ef -- database update --project packages/NevaxisZentra.Persistence.Migrations/NevaxisZentra.Persistence.Migrations.csproj --startup-project packages/NevaxisZentra.Persistence.Migrations/NevaxisZentra.Persistence.Migrations.csproj --context NevaxisZentraDbContext
if ($LASTEXITCODE -ne 0) { throw "pristine migration failed" }
Pop-Location
Write-Host "PRISTINE_DATABASE_MIGRATION=PASS"

Stop-ClosureRuntime
Write-Host "WHATSAPP_RUNTIME_CLOSURE_1=PASS"
