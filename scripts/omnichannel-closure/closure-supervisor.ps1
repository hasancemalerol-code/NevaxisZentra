# NevaxisFactOS omnichannel closure runtime supervisor.
# Starts API + Web, waits for readiness, records PIDs/logs, stops only owned processes.
$ErrorActionPreference = "Stop"
. "$PSScriptRoot/closure-constants.ps1"

$root = Resolve-Path "$PSScriptRoot/../.."
$artifactsDir = Join-Path $root "artifacts/omnichannel-closure"
$logsDir = Join-Path $artifactsDir "logs"
New-Item -ItemType Directory -Force -Path $artifactsDir, $logsDir | Out-Null

$apiPort = if ($env:CLOSURE_API_PORT) { [int]$env:CLOSURE_API_PORT } else { 5199 }
$webPort = if ($env:CLOSURE_WEB_PORT) { [int]$env:CLOSURE_WEB_PORT } else { 5299 }
$apiBase = "http://127.0.0.1:$apiPort"
$webBase = "http://127.0.0.1:$webPort"

function Test-PortFree([int]$Port) {
    return -not (Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
}

function Wait-HttpReady([string]$Url, [int]$TimeoutSec = 90) {
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

function Stop-OwnedProcess([System.Diagnostics.Process]$Proc, [string]$Label) {
    if ($null -eq $Proc -or $Proc.HasExited) { return }
    Write-Host "Stopping $Label (PID $($Proc.Id))..."
    try { Stop-Process -Id $Proc.Id -Force -ErrorAction SilentlyContinue } catch {}
}

$statePath = Join-Path $artifactsDir "runtime-state.json"
$owned = @()

if (-not (Test-PortFree $apiPort)) {
    $listener = Get-NetTCPConnection -LocalPort $apiPort -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
    $ownerPid = $listener.OwningProcess
    $owner = Get-Process -Id $ownerPid -ErrorAction SilentlyContinue
    $ownerPath = if ($owner) { $owner.Path } else { "" }
    if ($owner -and $owner.ProcessName -eq "NevaxisFactOS.Api" -and $ownerPath -like "*\Projects\NevaxisFactOS\*") {
        Write-Host "Reclaiming stale closure API on port $apiPort (PID $ownerPid)"
        Stop-Process -Id $ownerPid -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
    } else {
        throw "Port $apiPort is occupied by foreign PID $ownerPid. FOREIGN_PROCESS_KILLED policy forbids killing it."
    }
}

if (-not (Test-PortFree $webPort)) {
    $listener = Get-NetTCPConnection -LocalPort $webPort -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
    $ownerPid = $listener.OwningProcess
    $owner = Get-Process -Id $ownerPid -ErrorAction SilentlyContinue
    $ownerCmd = ""
    try {
        $ownerCmd = (Get-CimInstance Win32_Process -Filter "ProcessId=$ownerPid").CommandLine
    } catch {}
    $reclaimed = $false
    if (Test-Path $statePath) {
        try {
            $prior = Get-Content $statePath | ConvertFrom-Json
            if ($prior.webPid -eq $ownerPid) {
                Write-Host "Reclaiming prior closure web on port $webPort (PID $ownerPid)"
                Stop-Process -Id $ownerPid -Force -ErrorAction SilentlyContinue
                Start-Sleep -Seconds 2
                $reclaimed = $true
            }
        } catch {}
    }
    if (-not $reclaimed -and $owner -and $owner.ProcessName -eq "node" -and $ownerCmd -like "*\Projects\NevaxisFactOS\*") {
        Write-Host "Reclaiming stale closure web on port $webPort (PID $ownerPid)"
        Stop-Process -Id $ownerPid -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        $reclaimed = $true
    }
    if (-not $reclaimed -and -not (Test-PortFree $webPort)) {
        throw "Port $webPort is occupied by foreign PID $ownerPid. FOREIGN_PROCESS_KILLED policy forbids killing it."
    }
}

$apiOnly = $args -contains "--api-only"
$skipWeb = $apiOnly -or ($args -contains "--skip-web")

Write-Host "Building solution for closure runtime..."
Push-Location $root
dotnet build "apps/api" -v q | Out-Null
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }

$apiEnv = @{
    ASPNETCORE_ENVIRONMENT = "Development"
    NEVAXIS_FACTOS__PERSISTENCE__ENABLED = "true"
    NEVAXIS_FACTOS__PERSISTENCE__HOST = $ClosurePostgresHost
    NEVAXIS_FACTOS__PERSISTENCE__PORT = "$ClosurePostgresPort"
    NEVAXIS_FACTOS__PERSISTENCE__DATABASE = $ClosureDatabaseName
    NEVAXIS_FACTOS__PERSISTENCE__USERNAME = $ClosureRuntimeUser
    NEVAXIS_FACTOS__PERSISTENCE__PASSWORD = "local_runtime_only_change_me"
    NEVAXIS_FACTOS__PERSISTENCE__SSL_MODE = "Disable"
    DevelopmentPilot__Enabled = "false"
    NEVAXIS_FACTOS__AI_TASK_DELIVERY__ENABLE_CLOSURE_TTL_POLICIES = "true"
    NEVAXIS_FACTOS__AI_TASK_DELIVERY__LEASE_SECONDS = if ($env:CLOSURE_LEASE_SECONDS) { $env:CLOSURE_LEASE_SECONDS } else { "5" }
    NEVAXIS_FACTOS__AI_TASK_DELIVERY__RETRY_BASE_DELAY_SECONDS = if ($env:CLOSURE_RETRY_BASE_DELAY_SECONDS) { $env:CLOSURE_RETRY_BASE_DELAY_SECONDS } else { "1" }
    NEVAXIS_FACTOS__AI_TASK_DELIVERY__WORKER_ENABLED = if ($env:CLOSURE_WORKER_ENABLED) { $env:CLOSURE_WORKER_ENABLED } else { "true" }
    ASPNETCORE_URLS = $apiBase
}

$apiLog = Join-Path $logsDir "closure-api.log"
$apiErr = Join-Path $logsDir "closure-api.err.log"
foreach ($k in $apiEnv.Keys) { Set-Item -Path "env:$k" -Value ([string]$apiEnv[$k]) }
$apiProc = Start-Process -FilePath "dotnet" `
    -ArgumentList "run --no-launch-profile --project `"$(Join-Path $root 'apps/api')`" --urls $apiBase" `
    -WorkingDirectory $root `
    -RedirectStandardOutput $apiLog `
    -RedirectStandardError $apiErr `
    -PassThru `
    -WindowStyle Hidden
$owned += $apiProc

Write-Host "Waiting for API at $apiBase ..."
if (-not (Wait-HttpReady "$apiBase/health/ready")) {
    Stop-OwnedProcess $apiProc "API"
    throw "API readiness timeout"
}

$webProc = $null
if (-not $skipWeb) {
$webEnv = @{
    NEVAXIS_FACTOS_API_BASE_URL = $apiBase
    NEVAXIS_FACTOS_PRODUCTION_EXECUTION_RUNTIME_MODE = "development-api"
    NEVAXIS_FACTOS_DEV_AUTH_ISSUER = "crevafactos-dev"
    NEVAXIS_FACTOS_DEV_AUTH_SUBJECT = "planning-pilot-user"
    NEVAXIS_FACTOS_DEV_TENANT_KEY = "omnichannel-closure-tenant-a"
    NEVAXIS_FACTOS_DEV_FACILITY_ID = "bb000001-0005-4000-8000-000000000005"
    PORT = "$webPort"
}

$webLog = Join-Path $logsDir "closure-web.log"
$webErr = Join-Path $logsDir "closure-web.err.log"
foreach ($k in $webEnv.Keys) { Set-Item -Path "env:$k" -Value ([string]$webEnv[$k]) }
$webProc = Start-Process -FilePath "npm.cmd" `
    -ArgumentList "run dev -- --port $webPort" `
    -WorkingDirectory (Join-Path $root "apps/web") `
    -RedirectStandardOutput $webLog `
    -RedirectStandardError $webErr `
    -PassThru `
    -WindowStyle Hidden
$owned += $webProc

Write-Host "Waiting for Web at $webBase ..."
if (-not (Wait-HttpReady $webBase)) {
    foreach ($p in $owned) { Stop-OwnedProcess $p "runtime" }
    throw "Web readiness timeout"
}
}

$state = @{
    startedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    apiPort = $apiPort
    webPort = if ($skipWeb) { $null } else { $webPort }
    apiPid = $apiProc.Id
    webPid = if ($webProc) { $webProc.Id } else { $null }
    apiLog = $apiLog
    webLog = $webLog
    database = $ClosureDatabaseName
    supervisor = "closure-supervisor.ps1"
}
$state | ConvertTo-Json -Depth 4 | Set-Content -Path $statePath -Encoding UTF8

Write-Host "CLOSURE_RUNTIME_SUPERVISION=PASS"
if ($webProc) { Write-Host "API PID=$($apiProc.Id) WEB PID=$($webProc.Id)" } else { Write-Host "API PID=$($apiProc.Id) (api-only)" }
Write-Host "State written to $statePath"

if ($args -contains "--detach") {
    exit 0
}

try {
    Write-Host "Supervisor holding runtime alive. Press Ctrl+C to stop owned processes."
    while (-not $apiProc.HasExited -and (-not $webProc -or -not $webProc.HasExited)) {
        Start-Sleep -Seconds 2
    }
} finally {
    foreach ($p in $owned) { Stop-OwnedProcess $p "runtime" }
    Write-Host "Owned closure processes stopped."
}
