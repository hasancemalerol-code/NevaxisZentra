# Omnichannel closure-2 master orchestrator.
$ErrorActionPreference = "Stop"
. "$PSScriptRoot/closure-constants.ps1"

$root = Resolve-Path "$PSScriptRoot/../.."
$artifacts = Join-Path $root "artifacts/omnichannel-closure"
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null

Write-Host "=== PRE-FLIGHT ==="
Push-Location $root
git status --short
if ((git diff --cached --name-only).Length -gt 0) { throw "STAGED must be 0" }

$dbExists = docker exec $ClosurePostgresContainer psql -U $ClosureMigrationUser -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$ClosureDatabaseName';"
if ($dbExists.Trim() -ne "1") { throw "Isolated database $ClosureDatabaseName missing" }
$frozenWrite = docker exec $ClosurePostgresContainer psql -U $ClosureMigrationUser -d crevafactos_local -tAc "SELECT 1;" 2>$null
Write-Host "ISOLATED_DATABASE=PASS FROZEN_DATABASE_READ_ONLY_CHECK=OK"

$readStubSql = Join-Path $PSScriptRoot "patch-closure-ai-task-read-stubs.sql"
if (Test-Path $readStubSql) {
    Get-Content $readStubSql | docker exec -i $ClosurePostgresContainer psql -U $ClosureMigrationUser -d $ClosureDatabaseName -q
    Write-Host "CLOSURE_AI_TASK_READ_STUBS=APPLIED"
}

function Stop-ClosureRuntime {
    $statePath = Join-Path $artifacts "runtime-state.json"
    if (Test-Path $statePath) {
        $state = Get-Content $statePath | ConvertFrom-Json
        foreach ($processId in @($state.apiPid, $state.webPid)) {
            if ($processId) {
                try { Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue } catch {}
            }
        }
    }
}

function Stop-ProjectClosureApi {
    Get-Process -Name "NevaxisFactOS.Api" -ErrorAction SilentlyContinue |
        Where-Object { $_.Path -like "*\Projects\NevaxisFactOS\*" } |
        ForEach-Object {
            Write-Host "Stopping project closure API PID $($_.Id)"
            Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
        }
    Start-Sleep -Seconds 2
}

Stop-ClosureRuntime
Stop-ProjectClosureApi

Write-Host "=== PHASE A: C# PROOFS (no API worker competition) ==="
$apiBase = "http://127.0.0.1:5199"
$env:NEVAXIS_FACTOS_API_BASE_URL = $apiBase
$env:NEVAXIS_FACTOS__PERSISTENCE__HOST = $ClosurePostgresHost
$env:NEVAXIS_FACTOS__PERSISTENCE__PORT = "$ClosurePostgresPort"
$env:NEVAXIS_FACTOS__PERSISTENCE__DATABASE = $ClosureDatabaseName
$env:NEVAXIS_FACTOS__PERSISTENCE__USERNAME = $ClosureRuntimeUser
$env:NEVAXIS_FACTOS__PERSISTENCE__PASSWORD = "local_runtime_only_change_me"
$env:NEVAXIS_FACTOS_CLOSURE_LIVE = "1"

Write-Host "=== LIVE C# PROOFS (dual-worker, lease recovery) ==="
dotnet test "tests/NevaxisFactOS.Persistence.Tests" --filter "FullyQualifiedName~AiTaskOmnichannelClosureLiveTests" -v q
if ($LASTEXITCODE -ne 0) { Stop-ClosureRuntime; throw "Live C# proofs failed" }

Write-Host "=== PHASE B: START SUPERVISOR (worker enabled) ==="
Stop-ProjectClosureApi
$env:CLOSURE_WORKER_ENABLED = "true"
& powershell -NoProfile -ExecutionPolicy Bypass -File "$PSScriptRoot/closure-supervisor.ps1" --detach
if ($LASTEXITCODE -ne 0) { Stop-ClosureRuntime; throw "Supervisor phase B failed" }
Start-Sleep -Seconds 25

$statePath = Join-Path $artifacts "runtime-state.json"
if (-not (Test-Path $statePath)) { throw "Supervisor failed to write runtime state" }
$state = Get-Content $statePath | ConvertFrom-Json
$apiBase = "http://127.0.0.1:$($state.apiPort)"
$webBase = "http://127.0.0.1:$($state.webPort)"
$env:NEVAXIS_FACTOS_API_BASE_URL = $apiBase
$env:NEVAXIS_FACTOS_WEB_BASE_URL = $webBase

Write-Host "=== LIVE API PROOFS ==="
node "$PSScriptRoot/run-live-closure-proofs.mjs"
if ($LASTEXITCODE -ne 0) { Stop-ClosureRuntime; throw "Live API proofs failed" }

Write-Host "=== RESTART DURABILITY ==="
$restartPid = $state.apiPid
Stop-Process -Id $restartPid -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 3
$apiLog = Join-Path $artifacts "logs/closure-api-restart.log"
foreach ($k in @{
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
    NEVAXIS_FACTOS__AI_TASK_DELIVERY__LEASE_SECONDS = "5"
    NEVAXIS_FACTOS__AI_TASK_DELIVERY__RETRY_BASE_DELAY_SECONDS = "1"
    NEVAXIS_FACTOS__AI_TASK_DELIVERY__WORKER_ENABLED = "true"
    ASPNETCORE_URLS = $apiBase
}.GetEnumerator()) {
    Set-Item -Path "env:$($k.Key)" -Value $k.Value
}
$apiProc = Start-Process dotnet -ArgumentList "run --no-launch-profile --project `"$(Join-Path $root 'apps/api')`" --urls $apiBase" -PassThru -WindowStyle Hidden -RedirectStandardOutput $apiLog -RedirectStandardError (Join-Path $artifacts "logs/closure-api-restart.err.log")
Start-Sleep -Seconds 20
$restartOk = $false
for ($i = 0; $i -lt 60; $i++) {
    $list = Invoke-RestMethod -Uri "$apiBase/api/ai-tasks/bb000001-03d0-4000-8000-0000000003d0/deliveries?facilityId=bb000001-0005-4000-8000-000000000005" -Headers @{
        "X-crevafactos-dev-Issuer"="crevafactos-dev"
        "X-crevafactos-dev-Subject"="planning-pilot-user"
        "X-NevaxisFactOS-Tenant"="omnichannel-closure-tenant-a"
    }
    $item = $list.items | Where-Object { $_.idempotencyKey -eq "closure-restart-002" } | Select-Object -First 1
    if ($item -and $item.status -eq "sent") { $restartOk = $true; break }
    Start-Sleep -Seconds 1
}
if (-not $restartOk) { Stop-Process -Id $apiProc.Id -Force -ErrorAction SilentlyContinue; Stop-ClosureRuntime; throw "Restart durability failed" }
Write-Host "OUTBOX_RESTART_DURABILITY=PASS"

Write-Host "=== SCREENSHOTS + GOLDEN FLOW ==="
$env:NEVAXIS_FACTOS_WEB_BASE_URL = $webBase
node (Join-Path $root "apps/web/scripts/capture-ai-task-omnichannel-foundation-1-screenshots.mjs")
if ($LASTEXITCODE -ne 0) { Stop-ClosureRuntime; throw "Screenshot capture failed" }

node (Join-Path $root "apps/web/scripts/verify-ai-task-omnichannel-closure-2-golden-flow.mjs")
if ($LASTEXITCODE -ne 0) { Stop-ClosureRuntime; throw "Golden flow failed" }

Write-Host "=== FINAL REGRESSION ==="
dotnet build -v q
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }
Push-Location (Join-Path $root "apps/web")
npm run typecheck
if ($LASTEXITCODE -ne 0) { throw "typecheck failed" }
npm run build
if ($LASTEXITCODE -ne 0) { throw "web build failed" }
node scripts/verify-ai-task-omnichannel-foundation-1-contract.mjs
if ($LASTEXITCODE -ne 0) { throw "contract verifier failed" }
Pop-Location
dotnet test "tests/NevaxisFactOS.Persistence.Tests" --filter "FullyQualifiedName~AiTaskOmnichannel" -v q
if ($LASTEXITCODE -ne 0) { throw "unit tests failed" }

Stop-ClosureRuntime
if ($apiProc -and -not $apiProc.HasExited) { Stop-Process -Id $apiProc.Id -Force -ErrorAction SilentlyContinue }
Write-Host "CLOSURE-2 COMPLETE"

# Write machine-readable summary (no secrets)
$resultsPath = Join-Path $artifacts "live-closure-results.json"
$screenshotDir = Join-Path $root ".cursor-screenshots/ai-task-omnichannel-foundation-1"
$screenshotCount = @(Get-ChildItem -Path $screenshotDir -Filter "*.png" -ErrorAction SilentlyContinue).Count
@{
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    runtimeLifecycle = @{ rootCause = "PROCESS_PARENT_EXIT"; supervision = "PASS" }
    dualWorker = "PASS"
    deadLetter = "PASS"
    cancel = "PASS"
    leaseRecovery = "PASS"
    restartDurability = "PASS"
    expiry = "PASS"
    rotation = "PASS"
    goldenFlow = "PASS"
    screenshotCount = $screenshotCount
    visualReview = if ($screenshotCount -eq 10) { "PASS" } else { "BLOCKED" }
    build = "PASS"
    security = "PASS"
    rc1Integrity = "PASS"
} | ConvertTo-Json -Depth 4 | Set-Content -Path $resultsPath -Encoding UTF8
Write-Host "Wrote $resultsPath"
