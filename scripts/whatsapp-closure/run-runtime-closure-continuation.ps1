# Continue runtime closure from failure delivery setup (after golden flow + webhook proofs)
$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "../..")
. "$PSScriptRoot/whatsapp-closure-constants.ps1"

$artifacts = Join-Path $root "artifacts/whatsapp-closure"
$state = Get-Content (Join-Path $artifacts "runtime-state.json") | ConvertFrom-Json
$apiBase = "http://127.0.0.1:$($state.apiPort)"
$webBase = "http://127.0.0.1:$($state.webPort)"

$env:CREVAFACTOS_API_BASE_URL = $apiBase
$env:CREVAFACTOS_WEB_BASE_URL = $webBase
$env:CREVAFACTOS__AI_TASK_DELIVERY__WHATSAPP__APP_SECRET = "whatsapp-test-app-secret"
$env:CREVAFACTOS__AI_TASK_DELIVERY__WHATSAPP__WEBHOOK_VERIFY_TOKEN = "whatsapp-test-verify-token"

Write-Host "=== FAILURE DELIVERY SETUP ==="
Stop-Process -Id $state.providerDoublePid -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
$failDoubleLog = Join-Path $artifacts "logs/provider-double-invalid.log"
$failDoubleErr = Join-Path $artifacts "logs/provider-double-invalid.err.log"
$env:CREVAFACTOS_WHATSAPP_TEST_DOUBLE_MODE = "invalid-recipient"
$failDouble = Start-Process node -ArgumentList "scripts/meta-whatsapp-provider-test-double.mjs" -WorkingDirectory (Join-Path $root "apps/web") -PassThru -WindowStyle Hidden -RedirectStandardOutput $failDoubleLog -RedirectStandardError $failDoubleErr
Start-Sleep -Seconds 2
$failKey = "whatsapp-failure-proof-$([DateTimeOffset]::UtcNow.ToUnixTimeSeconds())"
$issue = Invoke-RestMethod -Method POST -Uri "$apiBase/api/ai-tasks/bb000001-03d0-4000-8000-0000000003d0/secure-access?facilityId=bb000001-0005-4000-8000-000000000005" -Headers @{
    "X-CrevaFactOS-Dev-Issuer"="crevafactos-dev"; "X-CrevaFactOS-Dev-Subject"="planning-pilot-user"; "X-CrevaFactOS-Tenant"="omnichannel-closure-tenant-a"; "Content-Type"="application/json"
} -Body '{"ttlPolicy":"24h","scope":"read-only"}'
Invoke-RestMethod -Method POST -Uri "$apiBase/api/ai-tasks/bb000001-03d0-4000-8000-0000000003d0/deliveries?facilityId=bb000001-0005-4000-8000-000000000005" -Headers @{
    "X-CrevaFactOS-Dev-Issuer"="crevafactos-dev"; "X-CrevaFactOS-Dev-Subject"="planning-pilot-user"; "X-CrevaFactOS-Tenant"="omnichannel-closure-tenant-a"; "Content-Type"="application/json"; "Idempotency-Key"=$failKey
} -Body (@{
    channel="whatsapp"; recipientKind="external"; maskedRecipient="+90 5** *** ** 99"; recipientE164="+905551112234"
    consentStatus="known"; secureAccessGrantId=$issue.grant.id; secureTaskUrl="$webBase/gorev#access=opaque"
} | ConvertTo-Json) | Out-Null
Start-Sleep -Seconds 15
Stop-Process -Id $failDouble.Id -Force -ErrorAction SilentlyContinue
Remove-Item Env:CREVAFACTOS_WHATSAPP_TEST_DOUBLE_MODE -ErrorAction SilentlyContinue
$acceptDoubleLog = Join-Path $artifacts "logs/provider-double-accept.log"
$acceptDoubleErr = Join-Path $artifacts "logs/provider-double-accept.err.log"
$acceptDouble = Start-Process node -ArgumentList "scripts/meta-whatsapp-provider-test-double.mjs" -WorkingDirectory (Join-Path $root "apps/web") -PassThru -WindowStyle Hidden -RedirectStandardOutput $acceptDoubleLog -RedirectStandardError $acceptDoubleErr
Start-Sleep -Seconds 2
$state.providerDoublePid = $acceptDouble.Id
Write-Host "WHATSAPP_SAFE_FAILURE_UI_SETUP=PASS"

Write-Host "=== SCREENSHOTS (configured) ==="
node (Join-Path $root "apps/web/scripts/capture-ai-task-whatsapp-business-adapter-1-screenshots.mjs")
if ($LASTEXITCODE -ne 0) { throw "Screenshot capture failed" }

Write-Host "=== NOT-CONFIGURED SCREENSHOT ==="
Stop-Process -Id $state.apiPid -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 3
$apiLog = Join-Path $artifacts "logs/closure-api-no-wa.log"
$apiNoWaEnv = @{
    ASPNETCORE_ENVIRONMENT = "Development"
    CREVAFACTOS__PERSISTENCE__ENABLED = "true"
    CREVAFACTOS__PERSISTENCE__HOST = $WhatsAppClosurePostgresHost
    CREVAFACTOS__PERSISTENCE__PORT = "$WhatsAppClosurePostgresPort"
    CREVAFACTOS__PERSISTENCE__DATABASE = $WhatsAppClosureDatabaseName
    CREVAFACTOS__PERSISTENCE__USERNAME = $WhatsAppClosureRuntimeUser
    CREVAFACTOS__PERSISTENCE__PASSWORD = "local_runtime_only_change_me"
    CREVAFACTOS__PERSISTENCE__SSL_MODE = "Disable"
    DevelopmentPilot__Enabled = "false"
    CREVAFACTOS__AI_TASK_DELIVERY__WORKER_ENABLED = "true"
    ASPNETCORE_URLS = $apiBase
}
foreach ($k in $apiNoWaEnv.Keys) { Set-Item -Path "env:$k" -Value ([string]$apiNoWaEnv[$k]) }
Remove-Item Env:CREVAFACTOS__AI_TASK_DELIVERY__WHATSAPP__ENABLED -ErrorAction SilentlyContinue
Remove-Item Env:CREVAFACTOS__AI_TASK_DELIVERY__WHATSAPP__TEST_API_BASE_URL -ErrorAction SilentlyContinue
Remove-Item Env:CREVAFACTOS__AI_TASK_DELIVERY__WHATSAPP__ACCESS_TOKEN -ErrorAction SilentlyContinue
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
node (Join-Path $root "apps/web/scripts/capture-ai-task-whatsapp-not-configured-screenshot.mjs")
if ($LASTEXITCODE -ne 0) { Stop-Process -Id $apiNoWa.Id -Force -ErrorAction SilentlyContinue; throw "Not-configured screenshot failed" }
Stop-Process -Id $apiNoWa.Id -Force -ErrorAction SilentlyContinue

$state | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $artifacts "runtime-state.json") -Encoding UTF8
Write-Host "WHATSAPP_SCREENSHOTS=PASS"
