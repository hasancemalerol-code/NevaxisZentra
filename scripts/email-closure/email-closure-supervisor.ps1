# Email closure runtime supervisor — API + Web + SMTP test double.
$ErrorActionPreference = "Stop"
. "$PSScriptRoot/email-closure-constants.ps1"

$root = Resolve-Path "$PSScriptRoot/../.."
$artifactsDir = Join-Path $root "artifacts/email-closure"
$logsDir = Join-Path $artifactsDir "logs"
New-Item -ItemType Directory -Force -Path $artifactsDir, $logsDir | Out-Null

$apiPort = $EmailClosureApiPort
$webPort = $EmailClosureWebPort
$smtpPort = $EmailSmtpDoublePort
$smtpHttpPort = $EmailSmtpDoublePreviewPort
$apiBase = "http://127.0.0.1:$apiPort"
$webBase = "http://127.0.0.1:$webPort"

function Test-PortFree([int]$Port) {
    return -not (Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
}

function Wait-HttpReady([string]$Url, [int]$TimeoutSec = 180) {
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

function Stop-Owned([int]$ProcessId, [string]$Label) {
    if (-not $ProcessId) { return }
    $p = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if ($p) {
        Write-Host "Stopping $Label PID $ProcessId"
        Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
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
    if ($Kind -eq "api" -and $owner -and $owner.ProcessName -eq "NevaxisZentra.Api" -and $path -like "*\Projects\NevaxisZentra\*") { $ok = $true }
    if ($Kind -eq "web" -and $owner -and $owner.ProcessName -eq "node" -and $cmd -like "*\Projects\NevaxisZentra\*") { $ok = $true }
    if ($Kind -eq "smtp" -and $owner -and $owner.ProcessName -eq "node" -and $cmd -like "*smtp-email-provider-test-double*") { $ok = $true }
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
Reclaim-ProjectPort $smtpPort "smtp"
Reclaim-ProjectPort $smtpHttpPort "smtp"

# Next.js dev uses a single project lock — reclaim any NevaxisZentra web dev server.
Get-CimInstance Win32_Process -Filter "Name='node.exe'" -ErrorAction SilentlyContinue | ForEach-Object {
    $cmd = $_.CommandLine
    if ($cmd -like "*\Projects\NevaxisZentra\apps\web*" -and $cmd -like "*next*") {
        Write-Host "Reclaiming NevaxisZentra web dev PID $($_.ProcessId)"
        Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
    }
}
$nextLock = Join-Path $root "apps/web/.next/dev/lock"
if (Test-Path $nextLock) { Remove-Item $nextLock -Force -ErrorAction SilentlyContinue }
Start-Sleep -Seconds 2

Write-Host "Building API for Email closure..."
Push-Location $root
dotnet build "apps/api" -v q | Out-Null
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }
Pop-Location

$smtpLog = Join-Path $logsDir "smtp-double.log"
$smtpErr = Join-Path $logsDir "smtp-double.err.log"
$smtpEnv = @{
    NEVAXIS_FACTOS_EMAIL_TEST_DOUBLE_MODE = "accept"
    NEVAXIS_FACTOS_EMAIL_TEST_DOUBLE_PORT = "$smtpPort"
    NEVAXIS_FACTOS_EMAIL_TEST_DOUBLE_HTTP_PORT = "$smtpHttpPort"
    NEVAXIS_FACTOS_EMAIL_CAPTURE_DIR = $artifactsDir
}
foreach ($k in $smtpEnv.Keys) { Set-Item -Path "env:$k" -Value ([string]$smtpEnv[$k]) }

$smtpProc = Start-Process -FilePath "node" `
    -ArgumentList "scripts/smtp-email-provider-test-double.mjs" `
    -WorkingDirectory (Join-Path $root "apps/web") `
    -RedirectStandardOutput $smtpLog `
    -RedirectStandardError $smtpErr `
    -PassThru -WindowStyle Hidden

Start-Sleep -Seconds 2
if (-not (Wait-HttpReady "http://127.0.0.1:$smtpHttpPort/health" 15)) {
    Stop-Owned $smtpProc.Id "smtp-double"
    throw "SMTP test double failed to start"
}

$apiLog = Join-Path $logsDir "closure-api.log"
$apiErr = Join-Path $logsDir "closure-api.err.log"
$apiEnv = @{
    ASPNETCORE_ENVIRONMENT = "Development"
    NEVAXIS_FACTOS__PERSISTENCE__ENABLED = "true"
    NEVAXIS_FACTOS__PERSISTENCE__HOST = $EmailClosurePostgresHost
    NEVAXIS_FACTOS__PERSISTENCE__PORT = "$EmailClosurePostgresPort"
    NEVAXIS_FACTOS__PERSISTENCE__DATABASE = $EmailClosureDatabaseName
    NEVAXIS_FACTOS__PERSISTENCE__USERNAME = $EmailClosureRuntimeUser
    NEVAXIS_FACTOS__PERSISTENCE__PASSWORD = "local_runtime_only_change_me"
    NEVAXIS_FACTOS__PERSISTENCE__SSL_MODE = "Disable"
    DevelopmentPilot__Enabled = "false"
    NEVAXIS_FACTOS__AI_TASK_DELIVERY__ENABLE_CLOSURE_TTL_POLICIES = "true"
    NEVAXIS_FACTOS__AI_TASK_DELIVERY__LEASE_SECONDS = "5"
    NEVAXIS_FACTOS__AI_TASK_DELIVERY__RETRY_BASE_DELAY_SECONDS = "1"
    NEVAXIS_FACTOS__AI_TASK_DELIVERY__WORKER_ENABLED = "true"
    NEVAXIS_FACTOS__AI_TASK_DELIVERY__EMAIL__ENABLED = "true"
    NEVAXIS_FACTOS__AI_TASK_DELIVERY__EMAIL__TIMEOUT_SECONDS = "5"
    NEVAXIS_FACTOS__AI_TASK_DELIVERY__EMAIL__TEST_SMTP__HOST = "127.0.0.1"
    NEVAXIS_FACTOS__AI_TASK_DELIVERY__EMAIL__TEST_SMTP__PORT = "$smtpPort"
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
    Stop-Owned $smtpProc.Id "smtp-double"
    throw "API readiness timeout"
}

$webLog = Join-Path $logsDir "closure-web.log"
$webErr = Join-Path $logsDir "closure-web.err.log"
$webEnv = @{
    NEVAXIS_FACTOS_API_BASE_URL = $apiBase
    NEVAXIS_FACTOS_PRODUCTION_EXECUTION_RUNTIME_MODE = "development-api"
    NEVAXIS_FACTOS_DEV_AUTH_ISSUER = "crevafactos-dev"
    NEVAXIS_FACTOS_DEV_AUTH_SUBJECT = "planning-pilot-user"
    NEVAXIS_FACTOS_DEV_TENANT_KEY = "omnichannel-closure-tenant-a"
    NEVAXIS_FACTOS_DEV_FACILITY_ID = "bb000001-0005-4000-8000-000000000005"
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
    Stop-Owned $smtpProc.Id "smtp-double"
    throw "Web readiness timeout"
}

$state = @{
    startedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    database = $EmailClosureDatabaseName
    apiPort = $apiPort
    webPort = $webPort
    smtpPort = $smtpPort
    smtpHttpPort = $smtpHttpPort
    apiPid = $apiProc.Id
    webPid = $webProc.Id
    smtpDoublePid = $smtpProc.Id
    apiLog = $apiLog
    webLog = $webLog
    smtpLog = $smtpLog
    supervisor = "email-closure-supervisor.ps1"
}
$statePath = Join-Path $artifactsDir "runtime-state.json"
$state | ConvertTo-Json -Depth 4 | Set-Content -Path $statePath -Encoding UTF8

Write-Host "EMAIL_CLOSURE_RUNTIME=PASS"
Write-Host "API_READY=PASS WEB_READY=PASS SMTP_DOUBLE_PID=$($smtpProc.Id)"
Write-Host "State: $statePath"

if ($args -contains "--detach") { exit 0 }

try {
    while (-not $apiProc.HasExited -and -not $webProc.HasExited) { Start-Sleep -Seconds 2 }
} finally {
    Stop-Owned $webProc.Id "Web"
    Stop-Owned $apiProc.Id "API"
    Stop-Owned $smtpProc.Id "smtp-double"
}
