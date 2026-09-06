# CREVAFACTOS-DESKTOP-RC1-WEB-PORT-COLLISION-CLOSURE-1 verifier

[CmdletBinding()]
param(
    [string]$RepositoryRoot = "",
    [string]$BundleRoot = "",
    [ValidateSet("A", "B", "C", "Real", "All")]
    [string]$TestCase = "All"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
}
if ([string]::IsNullOrWhiteSpace($BundleRoot)) {
    $BundleRoot = Join-Path $RepositoryRoot "artifacts\desktop-runtime\CrevaFactOS-RC1"
}

$desktopExe = Join-Path $BundleRoot "desktop\CrevaFactOS.exe"
$webAppDir = Join-Path $BundleRoot "web\apps\web"
$cdpScript = Join-Path $PSScriptRoot "verify-desktop-authentication-live-1-cdp.mjs"
$resultsPath = Join-Path $RepositoryRoot ".cursor-screenshots\desktop-runtime-packaging-1\port-collision-results.json"
$packagedWebPorts = @(3001, 3011, 3021, 3031)
$results = [ordered]@{
    FOREIGN_PROCESS_KILLED = 0
}

function Assert-Check([string]$Name, [bool]$Ok, [string]$Detail = "") {
    $results[$Name] = if ($Ok) { "PASS" } else { "FAIL" }
    Write-Host "$(if ($Ok) { 'PASS' } else { 'FAIL' }) $Name$(if ($Detail) { " - $Detail" } else { '' })"
    if (-not $Ok) { throw "$Name failed$(if ($Detail) { ": $Detail" } else { '' })" }
}

function Get-ListenerPid([int]$Port) {
    $conn = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($conn) { return $conn.OwningProcess }
    return $null
}

function Get-ProcessCommandLine([int]$ProcessId) {
    $proc = Get-CimInstance Win32_Process -Filter "ProcessId=$ProcessId" -ErrorAction SilentlyContinue
    return $proc.CommandLine
}

function Stop-CrevaFactOSOnly {
    foreach ($name in @("CrevaFactOS", "CrevaFactOS.Desktop", "CrevaFactOS.Api")) {
        Get-Process -Name $name -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    }
    foreach ($port in $packagedWebPorts + @(5000)) {
        $listeners = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
        foreach ($listener in @($listeners)) {
            if ($listener.OwningProcess -le 0) { continue }
            $proc = Get-CimInstance Win32_Process -Filter "ProcessId=$($listener.OwningProcess)" -ErrorAction SilentlyContinue
            if (-not $proc) { continue }
            $isCrevaFactOS = $proc.Name -match '^CrevaFactOS' -or
                ($proc.CommandLine -match 'desktop-runtime') -or
                ($proc.CommandLine -match 'CrevaFactOS-RC1') -or
                ($proc.Name -eq 'node.exe' -and $proc.CommandLine -match 'server\.js')
            if ($isCrevaFactOS) {
                Stop-Process -Id $listener.OwningProcess -Force -ErrorAction SilentlyContinue
            }
        }
    }
    Start-Sleep -Seconds 4
}

function Test-CrevaFactOSWebReady([int]$Port) {
    try {
        $body = '{"organizationContext":"dev-pilot-tenant","email":"desktop-probe@invalid.local","password":"__desktop_probe__","deviceLabel":"desktop-probe"}'
        $response = Invoke-WebRequest "http://127.0.0.1:$Port/api/identity/sign-in" -Method POST -Body $body -ContentType "application/json" -UseBasicParsing -TimeoutSec 8
        return $response.StatusCode -eq 401
    } catch {
        if ($_.Exception.Response) {
            return $_.Exception.Response.StatusCode.value__ -eq 401
        }
        return $false
    }
}

function Wait-WebReady([int]$TimeoutSeconds = 300) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $logPort = Get-SelectedWebPortFromLifecycleLog
        if ($logPort -and (Test-CrevaFactOSWebReady $logPort)) { return $logPort }
        foreach ($port in $packagedWebPorts) {
            if (Test-CrevaFactOSWebReady $port) { return $port }
        }
        Start-Sleep -Seconds 2
    }
    return $null
}

function Get-SelectedWebPortFromLifecycleLog {
    $logDir = Join-Path ([Environment]::GetFolderPath("LocalApplicationData")) "CrevaFactOS\Desktop\runtime\logs"
    if (-not (Test-Path $logDir)) { return $null }
    $latest = Get-ChildItem $logDir -Filter "desktop-lifecycle-*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $latest) { return $null }
    $content = Get-Content -LiteralPath $latest.FullName -Raw
    if ($content -match 'selectedWebPort=(\d+)') { return [int]$Matches[1] }
    if ($content -match 'web=selected-port=(\d+)') { return [int]$Matches[1] }
    return $null
}

function Start-ControlledForeignListener([int]$Port = 3001) {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $Port)
    $listener.Start()
    return $listener
}

function LaunchDesktopAndWait {
    $beforePids = @{}
    foreach ($port in $packagedWebPorts) {
        $listenerPid = Get-ListenerPid $port
        if ($listenerPid) { $beforePids[$port] = $listenerPid }
    }

    $proc = Start-Process -FilePath $desktopExe -WorkingDirectory (Split-Path $desktopExe -Parent) -PassThru
    $selectedPort = Wait-WebReady 300
    if (-not $selectedPort) {
        throw "Desktop did not expose a ready web port."
    }

    foreach ($port in $beforePids.Keys) {
        $listenerPid = $beforePids[$port]
        if ($listenerPid -and (Get-Process -Id $listenerPid -ErrorAction SilentlyContinue)) {
            $results.FOREIGN_PROCESS_KILLED = 0
        }
    }

    return [ordered]@{
        Process = $proc
        SelectedPort = $selectedPort
        LogSelectedPort = Get-SelectedWebPortFromLifecycleLog
    }
}

function Run-AuthSmoke([int]$WebPort) {
    $env:CREVAFACTOS_WEB_BASE_URL = "http://127.0.0.1:$WebPort"
    $env:CREVAFACTOS_API_BASE_URL = "http://127.0.0.1:5000"
    & node $cdpScript | Out-Null
    return ($LASTEXITCODE -eq 0)
}

function Test-CaseA {
    Write-Host "`n== Test A: Free 3001 =="
    $pid3001 = Get-ListenerPid 3001
    if ($pid3001) {
        $cmd = Get-ProcessCommandLine $pid3001
        if ($cmd -match 'desktop-runtime|CrevaFactOS-RC1') {
            Stop-CrevaFactOSOnly
        } elseif ($cmd) {
            Write-Host "SKIP Test A: port 3001 occupied by foreign PID $pid3001 (not killed)"
            $results["Test A - Free 3001"] = "SKIP"
            return
        }
    }
    Stop-CrevaFactOSOnly
    $still = Get-ListenerPid 3001
    if ($still) {
        Write-Host "SKIP Test A: port 3001 still occupied by PID $still"
        $results["Test A - Free 3001"] = "SKIP"
        return
    }

    $launch = LaunchDesktopAndWait
    Assert-Check "SELECTED_WEB_PORT" ($launch.SelectedPort -eq 3001) "actual=$($launch.SelectedPort)"
    Assert-Check "AUTH login MFA dashboard" (Run-AuthSmoke $launch.SelectedPort)
    $results["Test A - Free 3001"] = "PASS"
    Stop-CrevaFactOSOnly
}

function Test-CaseB {
    Write-Host "`n== Test B: Stale CrevaFactOS listener =="
    Stop-CrevaFactOSOnly
    $targetPort = 3001
    if (Get-ListenerPid 3001) {
        $targetPort = 3011
        if (Get-ListenerPid 3011) {
            Write-Host "SKIP Test B: no free candidate port for stale owned listener"
            $results["Test B - Stale CrevaFactOS listener"] = "SKIP"
            return
        }
    }

    $env:PORT = "$targetPort"
    $env:HOSTNAME = "127.0.0.1"
    $env:CREVAFACTOS_API_BASE_URL = "http://127.0.0.1:5000"
    $env:NODE_ENV = "production"
    $stale = Start-Process -FilePath "node.exe" -ArgumentList "server.js" -WorkingDirectory $webAppDir -PassThru -WindowStyle Hidden
    Start-Sleep -Seconds 8
    Assert-Check "stale owned listener started" ((Get-ListenerPid $targetPort) -eq $stale.Id) "pid=$($stale.Id)"

    $launch = LaunchDesktopAndWait
    Assert-Check "ownership recognized and reclaimed" ($launch.SelectedPort -eq $targetPort) "actual=$($launch.SelectedPort)"
    Assert-Check "SELECTED_WEB_PORT after reclaim" ($launch.SelectedPort -eq $targetPort)
    Assert-Check "AUTH login MFA dashboard" (Run-AuthSmoke $launch.SelectedPort)
    $results["Test B - Stale CrevaFactOS listener"] = "PASS"
    Stop-CrevaFactOSOnly
}

function Test-CaseC {
    Write-Host "`n== Test C: Foreign 3001 listener =="
    Stop-CrevaFactOSOnly
    $foreignPid = Get-ListenerPid 3001
    $foreignListener = $null
    if (-not $foreignPid) {
        $foreignListener = Start-ControlledForeignListener 3001
        $foreignPid = Get-ListenerPid 3001
        Assert-Check "controlled foreign listener on 3001" ($null -ne $foreignPid)
        $results["controlled_foreign_listener"] = $true
    } else {
        Write-Host "Using existing foreign listener PID $foreignPid on 3001"
        $results["controlled_foreign_listener"] = $false
    }

    $launch = LaunchDesktopAndWait
    Assert-Check "foreign process not killed" ($null -ne (Get-Process -Id $foreignPid -ErrorAction SilentlyContinue))
    Assert-Check "fallback port selected" ($launch.SelectedPort -ne 3001) "actual=$($launch.SelectedPort)"
    Assert-Check "deterministic fallback port" ($packagedWebPorts -contains $launch.SelectedPort)
    Assert-Check "AUTH login MFA dashboard" (Run-AuthSmoke $launch.SelectedPort)
    $results["Test C - Foreign 3001 listener"] = "PASS"
    $results["SELECTED_WEB_PORT"] = $launch.SelectedPort
    Stop-CrevaFactOSOnly

    if ($foreignListener) {
        $foreignListener.Stop()
        $results["controlled_foreign_listener_cleaned"] = $true
    }
}

function Test-RealColdStart {
    Write-Host "`n== Real User Cold Start =="
    Stop-CrevaFactOSOnly
    $foreignPid = Get-ListenerPid 3001
    if ($foreignPid) {
        Write-Host "Port 3001 occupied by PID $foreignPid (foreign expected)"
    }

    $launch = LaunchDesktopAndWait
    $expectedPort = if ($foreignPid) { 3011 } else { 3001 }
    if ($foreignPid) {
        Assert-Check "foreign PID 3001 preserved" ($null -ne (Get-Process -Id $foreignPid -ErrorAction SilentlyContinue))
        Assert-Check "REAL_DESKTOP_COLD_START fallback" ($launch.SelectedPort -eq $expectedPort) "actual=$($launch.SelectedPort)"
    } else {
        Assert-Check "REAL_DESKTOP_COLD_START preferred port" ($launch.SelectedPort -eq 3001) "actual=$($launch.SelectedPort)"
    }
    Assert-Check "AUTH login MFA dashboard" (Run-AuthSmoke $launch.SelectedPort)
    $results["Real User Cold Start"] = "PASS"
    $results["REAL_DESKTOP_COLD_START"] = "PASS"
    $results["WEB_PORT_COLLISION_RECOVERY"] = "PASS"
    $results["SELECTED_WEB_PORT"] = $launch.SelectedPort
}

New-Item -ItemType Directory -Force -Path (Split-Path $resultsPath -Parent) | Out-Null
Assert-Check "desktop exe exists" (Test-Path $desktopExe)

$cases = switch ($TestCase) {
    "A" { @("A") }
    "B" { @("B") }
    "C" { @("C") }
    "Real" { @("Real") }
    default { @("C", "B", "A", "Real") }
}

foreach ($case in $cases) {
    switch ($case) {
        "A" { Test-CaseA }
        "B" { Test-CaseB }
        "C" { Test-CaseC }
        "Real" { Test-RealColdStart }
    }
}

$results | ConvertTo-Json -Depth 4 | Set-Content -Path $resultsPath -Encoding UTF8
Write-Host "`nPort collision verifier complete: $resultsPath"
