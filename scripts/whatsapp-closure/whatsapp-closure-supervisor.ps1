# WhatsApp closure runtime supervisor — API + Web + provider double metadata.
$ErrorActionPreference = "Stop"
. "$PSScriptRoot/whatsapp-closure-constants.ps1"

$root = Resolve-Path "$PSScriptRoot/../.."
$artifactsDir = Join-Path $root "artifacts/whatsapp-closure"
$logsDir = Join-Path $artifactsDir "logs"
New-Item -ItemType Directory -Force -Path $artifactsDir, $logsDir | Out-Null

$apiPort = $WhatsAppClosureApiPort
$webPort = $WhatsAppClosureWebPort
$doublePort = $WhatsAppProviderDoublePort
$apiBase = "http://127.0.0.1:$apiPort"
$webBase = "http://127.0.0.1:$webPort"
$doubleBase = "http://127.0.0.1:$doublePort"

function Test-PortFree([int]$Port) {
    return -not (Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
}

function Wait-HttpReady([string]$Url, [int]$TimeoutSec = 120) {
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        try {
            $r = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 3
            if ($r.StatusCode -ge 200 -and $r.StatusCode -lt 500) { return $true }
        } catch {
            if ($_.Exception.Response -and [int]$_.Exception.Response.StatusCode -lt 500) { return $true }
        }
        Start-Sleep -Seconds 1
    }
    return $false
}

function Stop-Owned([int]$Pid, [string]$Label) {
    if (-not $Pid) { return }
    $p = Get-Process -Id $Pid -ErrorAction SilentlyContinue
    if ($p) {
        Write-Host "Stopping $Label PID $Pid"
        Stop-Process -Id $Pid -Force -ErrorAction SilentlyContinue
    }
}

function Reclaim-ProjectPort([int]$Port, [string]$Kind) {
    if (Test-PortFree $Port) { return }
    $listener = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
    $ownerPid = $listener.OwningProcess
    $owner = Get-Process -Id $ownerPid -ErrorAction SilentlyContinue
    $path = if ($owner) { $owner.Path } else { "" }
    $cmd = ""
    try { $cmd = (Get-CimInstance Win32_Process -Filter "ProcessId=$ownerPid").CommandLine } catch {}
    $ok = $false
    if ($Kind -eq "api" -and $owner -and $owner.ProcessName -eq "CrevaFactOS.Api" -and $path -like "*\Projects\CrevaFactOS\*") { $ok = $true }
    if ($Kind -eq "web" -and $owner -and $owner.ProcessName -eq "node" -and $cmd -like "*\Projects\CrevaFactOS\*") { $ok = $true }
    if ($Kind -eq "double" -and $owner -and $owner.ProcessName -eq "node" -and $cmd -like "*meta-whatsapp-provider-test-double*") { $ok = $true }
    if ($ok) {
        Write-Host "Reclaiming $Kind port $Port PID $ownerPid"
        Stop-Process -Id $ownerPid -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
    } elseif (-not (Test-PortFree $Port)) {
        throw "Port $Port occupied by foreign PID $ownerPid"
    }
}

Reclaim-ProjectPort $apiPort "api"
Reclaim-ProjectPort $webPort "web"
Reclaim-ProjectPort $doublePort "double"

Write-Host "Building API for WhatsApp closure..."
Push-Location $root
dotnet build "apps/api" -v q | Out-Null
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }
Pop-Location

$doubleLog = Join-Path $logsDir "provider-double.log"
$doubleErr = Join-Path $logsDir "provider-double.err.log"
$doubleProc = Start-Process -FilePath "node" `
    -ArgumentList "scripts/meta-whatsapp-provider-test-double.mjs" `
    -WorkingDirectory (Join-Path $root "apps/web") `
    -RedirectStandardOutput $doubleLog `
    -RedirectStandardError $doubleErr `
    -PassThru -WindowStyle Hidden

Start-Sleep -Seconds 2
if (-not (Wait-HttpReady "$doubleBase/" 15)) {
    # double has no health route; port listen is enough
    if (Test-PortFree $doublePort) { throw "Provider double failed to start" }
}

$apiLog = Join-Path $logsDir "closure-api.log"
$apiErr = Join-Path $logsDir "closure-api.err.log"
$apiEnv = @{
    ASPNETCORE_ENVIRONMENT = "Development"
    CREVAFACTOS__PERSISTENCE__ENABLED = "true"
    CREVAFACTOS__PERSISTENCE__HOST = $WhatsAppClosurePostgresHost
    CREVAFACTOS__PERSISTENCE__PORT = "$WhatsAppClosurePostgresPort"
    CREVAFACTOS__PERSISTENCE__DATABASE = $WhatsAppClosureDatabaseName
    CREVAFACTOS__PERSISTENCE__USERNAME = $WhatsAppClosureRuntimeUser
    CREVAFACTOS__PERSISTENCE__PASSWORD = "local_runtime_only_change_me"
    CREVAFACTOS__PERSISTENCE__SSL_MODE = "Disable"
    DevelopmentPilot__Enabled = "false"
    CREVAFACTOS__AI_TASK_DELIVERY__ENABLE_CLOSURE_TTL_POLICIES = "true"
    CREVAFACTOS__AI_TASK_DELIVERY__LEASE_SECONDS = "5"
    CREVAFACTOS__AI_TASK_DELIVERY__WORKER_ENABLED = "true"
    CREVAFACTOS__AI_TASK_DELIVERY__WHATSAPP__ENABLED = "true"
    CREVAFACTOS__AI_TASK_DELIVERY__WHATSAPP__PHONE_NUMBER_ID = "test-phone-id"
    CREVAFACTOS__AI_TASK_DELIVERY__WHATSAPP__BUSINESS_ACCOUNT_ID = "test-business-account"
    CREVAFACTOS__AI_TASK_DELIVERY__WHATSAPP__ACCESS_TOKEN = "test-access-token"
    CREVAFACTOS__AI_TASK_DELIVERY__WHATSAPP__APP_SECRET = "whatsapp-test-app-secret"
    CREVAFACTOS__AI_TASK_DELIVERY__WHATSAPP__WEBHOOK_VERIFY_TOKEN = "whatsapp-test-verify-token"
    CREVAFACTOS__AI_TASK_DELIVERY__WHATSAPP__DEFAULT_TEMPLATE_NAME = "crevafactos_task_notification"
    CREVAFACTOS__AI_TASK_DELIVERY__WHATSAPP__DEFAULT_TEMPLATE_LANGUAGE_CODE = "tr"
    CREVAFACTOS__AI_TASK_DELIVERY__WHATSAPP__TEST_API_BASE_URL = $doubleBase
    ASPNETCORE_URLS = $apiBase
}
foreach ($k in $apiEnv.Keys) { Set-Item -Path "env:$k" -Value ([string]$apiEnv[$k]) }

$apiProc = Start-Process -FilePath "dotnet" `
    -ArgumentList "run --no-launch-profile --project `"$(Join-Path $root 'apps/api')`" --urls $apiBase" `
    -WorkingDirectory $root `
    -RedirectStandardOutput $apiLog `
    -RedirectStandardError $apiErr `
    -PassThru -WindowStyle Hidden

if (-not (Wait-HttpReady "$apiBase/health/ready")) {
    Stop-Owned $apiProc.Id "API"
    Stop-Owned $doubleProc.Id "provider-double"
    throw "API readiness timeout"
}

$webLog = Join-Path $logsDir "closure-web.log"
$webErr = Join-Path $logsDir "closure-web.err.log"
$webEnv = @{
    CREVAFACTOS_API_BASE_URL = $apiBase
    CREVAFACTOS_PRODUCTION_EXECUTION_RUNTIME_MODE = "development-api"
    CREVAFACTOS_DEV_AUTH_ISSUER = "crevafactos-dev"
    CREVAFACTOS_DEV_AUTH_SUBJECT = "planning-pilot-user"
    CREVAFACTOS_DEV_TENANT_KEY = "omnichannel-closure-tenant-a"
    CREVAFACTOS_DEV_FACILITY_ID = "bb000001-0005-4000-8000-000000000005"
    PORT = "$webPort"
}
foreach ($k in $webEnv.Keys) { Set-Item -Path "env:$k" -Value ([string]$webEnv[$k]) }

$webProc = Start-Process -FilePath "npm.cmd" `
    -ArgumentList "run dev -- --port $webPort --hostname 127.0.0.1" `
    -WorkingDirectory (Join-Path $root "apps/web") `
    -RedirectStandardOutput $webLog `
    -RedirectStandardError $webErr `
    -PassThru -WindowStyle Hidden

if (-not (Wait-HttpReady $webBase)) {
    Stop-Owned $webProc.Id "Web"
    Stop-Owned $apiProc.Id "API"
    Stop-Owned $doubleProc.Id "provider-double"
    throw "Web readiness timeout"
}

$state = @{
    startedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    database = $WhatsAppClosureDatabaseName
    apiPort = $apiPort
    webPort = $webPort
    providerDoublePort = $doublePort
    apiPid = $apiProc.Id
    webPid = $webProc.Id
    providerDoublePid = $doubleProc.Id
    apiLog = $apiLog
    webLog = $webLog
    providerDoubleLog = $doubleLog
    supervisor = "whatsapp-closure-supervisor.ps1"
}
$statePath = Join-Path $artifactsDir "runtime-state.json"
$state | ConvertTo-Json -Depth 4 | Set-Content -Path $statePath -Encoding UTF8

Write-Host "WHATSAPP_CLOSURE_RUNTIME=PASS"
Write-Host "API_READY=PASS WEB_READY=PASS PROVIDER_DOUBLE_PID=$($doubleProc.Id)"
Write-Host "State: $statePath"

if ($args -contains "--detach") { exit 0 }

try {
    while (-not $apiProc.HasExited -and -not $webProc.HasExited) { Start-Sleep -Seconds 2 }
} finally {
    Stop-Owned $webProc.Id "Web"
    Stop-Owned $apiProc.Id "API"
    Stop-Owned $doubleProc.Id "provider-double"
}
