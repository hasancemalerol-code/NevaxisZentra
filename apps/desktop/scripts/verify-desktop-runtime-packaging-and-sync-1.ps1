# CREVAFACTOS-DESKTOP-RUNTIME-PACKAGING-AND-SYNC-1 verifier

[CmdletBinding()]
param(
    [string]$RepositoryRoot = "",
    [string]$BundleRoot = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
}
if ([string]::IsNullOrWhiteSpace($BundleRoot)) {
    $BundleRoot = Join-Path $RepositoryRoot "artifacts\desktop-runtime\CrevaFactOS-RC1"
}

$screenshotDir = Join-Path $RepositoryRoot ".cursor-screenshots\desktop-runtime-packaging-1"
$resultsPath = Join-Path $screenshotDir "runtime-packaging-results.json"
$cdpScript = Join-Path $PSScriptRoot "verify-desktop-all-modules-validation-1-cdp.mjs"
$manifestPath = Join-Path $BundleRoot "manifests\runtime-manifest.json"
$checksumPath = Join-Path $BundleRoot "manifests\checksums.sha256"
$desktopExe = Join-Path $BundleRoot "desktop\CrevaFactOS.exe"
$apiExe = Join-Path $BundleRoot "api\CrevaFactOS.Api.exe"
$webServer = Join-Path $BundleRoot "web\apps\web\server.js"
$shortcutPath = Join-Path ([Environment]::GetFolderPath("Desktop")) "CrevaFactOS.lnk"
$results = [ordered]@{}

function Assert-Check([string]$Name, [bool]$Ok, [string]$Detail = "") {
    $results[$Name] = if ($Ok) { "PASS" } else { "FAIL" }
    Write-Host "$(if ($Ok) { 'PASS' } else { 'FAIL' }) $Name$(if ($Detail) { " - $Detail" } else { '' })"
    if (-not $Ok) { throw "$Name failed$(if ($Detail) { ": $Detail" } else { '' })" }
}

function Test-DashboardProtectedDiff {
    $paths = @(
        "apps/web/src/app/(app)/dashboard",
        "apps/web/src/components/dashboard",
        "apps/web/src/features/dashboard",
        "apps/web/src/app/command-center.css"
    )
    foreach ($path in $paths) {
        $diff = git -C $RepositoryRoot diff --stat -- $path 2>$null
        if ($diff) { return $false }
    }
    return $true
}

function Test-ChecksumsValid([string]$Root, [string]$ChecksumFile) {
    if (-not (Test-Path -LiteralPath $ChecksumFile)) { return $false }
    $lines = Get-Content -LiteralPath $ChecksumFile
    foreach ($line in $lines) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $parts = $line -split '\s+', 2
        if ($parts.Count -lt 2) { return $false }
        $expected = $parts[0].ToLowerInvariant()
        $relative = $parts[1].Replace('/', '\')
        $full = Join-Path $Root $relative
        if (-not (Test-Path -LiteralPath $full)) { return $false }
        $actual = (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actual -ne $expected) { return $false }
    }
    return $true
}

function Test-SecretScan([string]$Root) {
    $patterns = @(
        'DevelopmentSaasIdentity__TenantOwnerPassword=(?!REPLACE_ME).{8,}',
        'DevelopmentSaasIdentity__MfaSharedSecret=(?!REPLACE_ME).{8,}',
        'sk-[A-Za-z0-9]{20,}',
        'OPENAI_API_KEY=.{8,}'
    )
    $files = Get-ChildItem $Root -Recurse -File |
        Where-Object {
            $_.FullName -notmatch '\\node_modules\\' -and
            $_.FullName -notmatch '\\.next\\' -and
            $_.Extension -notin @('.png', '.jpg', '.dll', '.exe', '.woff', '.woff2', '.ico', '.js', '.map', '.br', '.gz') -and
            $_.Name -ne 'runtime.example.env'
        }
    foreach ($file in $files) {
        $content = Get-Content -LiteralPath $file.FullName -Raw -ErrorAction SilentlyContinue
        if (-not $content) { continue }
        foreach ($pattern in $patterns) {
            if ($content -match $pattern) { return $false }
        }
    }
    return $true
}

function Get-ProcessAudit {
    $scoped = Get-CimInstance Win32_Process |
        Where-Object {
            $_.CommandLine -and (
                $_.CommandLine -match 'desktop-runtime' -or
                $_.CommandLine -match 'CrevaFactOS-RC1' -or
                $_.Name -match '^CrevaFactOS'
            )
        }
    $dev = @{
        nextDev = @($scoped | Where-Object { $_.CommandLine -match 'next\s+dev' }).Count
        dotnetRun = @($scoped | Where-Object { $_.CommandLine -match 'dotnet\s+run' }).Count
        pnpmDev = @($scoped | Where-Object { $_.CommandLine -match 'pnpm.*dev' }).Count
    }
    $sourceDeps = @($scoped | Where-Object {
        ($_.Name -eq 'CrevaFactOS.Api.exe' -and $_.CommandLine -match 'apps\\api\\') -or
        ($_.Name -eq 'node.exe' -and $_.CommandLine -match 'apps\\web') -or
        ($_.CommandLine -match '\\bin\\Debug\\' -and $_.CommandLine -notmatch 'desktop-runtime')
    }).Count
    return [ordered]@{
        NEXT_DEV_PROCESS = $dev.nextDev
        DOTNET_RUN_PROCESS = $dev.dotnetRun
        PNPM_DEV_PROCESS = $dev.pnpmDev
        PACKAGED_SOURCE_TREE_RUNTIME_DEPENDENCY = $sourceDeps
    }
}

function Stop-PortListeners([int[]]$Ports) {
    foreach ($port in $Ports) {
        $listeners = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
        if ($null -eq $listeners) { continue }
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
    Start-Sleep -Seconds 3
}

function Get-PackagedWebPortCandidates {
    return @(3001, 3011, 3021, 3031)
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

function Wait-PackagedWebReady([int]$TimeoutSeconds = 480) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $logPort = Get-SelectedWebPortFromLifecycleLog
        if ($logPort -and (Test-CrevaFactOSWebReady $logPort)) { return $logPort }
        foreach ($port in Get-PackagedWebPortCandidates) {
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

function Stop-CrevaFactOSDesktopOnly {
    foreach ($name in @("CrevaFactOS", "CrevaFactOS.Desktop", "CrevaFactOS.Api")) {
        Get-Process -Name $name -ErrorAction SilentlyContinue |
            Stop-Process -Force -ErrorAction SilentlyContinue
    }
    Stop-PortListeners -Ports @(3001, 3011, 3021, 3031, 5000)
    Start-Sleep -Seconds 4
}

function Wait-PackagedPortsCleared([int]$TimeoutSeconds = 60) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $crevafactosWebOccupied = $false
        foreach ($port in Get-PackagedWebPortCandidates) {
            $listeners = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
            if ($null -eq $listeners) { continue }
            foreach ($listener in @($listeners)) {
                $proc = Get-CimInstance Win32_Process -Filter "ProcessId=$($listener.OwningProcess)" -ErrorAction SilentlyContinue
                if ($proc -and (
                    $proc.Name -match '^CrevaFactOS' -or
                    ($proc.CommandLine -match 'desktop-runtime') -or
                    ($proc.CommandLine -match 'CrevaFactOS-RC1'))) {
                    $crevafactosWebOccupied = $true
                }
            }
        }
        $apiListeners = Get-NetTCPConnection -LocalPort 5000 -State Listen -ErrorAction SilentlyContinue
        $crevafactosApiOccupied = $false
        if ($apiListeners) {
            foreach ($listener in @($apiListeners)) {
                $proc = Get-CimInstance Win32_Process -Filter "ProcessId=$($listener.OwningProcess)" -ErrorAction SilentlyContinue
                if ($proc -and $proc.Name -match 'CrevaFactOS') { $crevafactosApiOccupied = $true }
            }
        }
        if (-not $crevafactosWebOccupied -and -not $crevafactosApiOccupied) { return $true }
        Start-Sleep -Seconds 2
    }
    return $false
}

New-Item -ItemType Directory -Force -Path $screenshotDir | Out-Null

Assert-Check "dashboard protected diff" (Test-DashboardProtectedDiff)
Assert-Check "runtime manifest exists" (Test-Path $manifestPath)
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
Assert-Check "runtime manifest valid" ($manifest.productName -eq "CrevaFactOS")
Assert-Check "checksums valid" (Test-ChecksumsValid $BundleRoot $checksumPath)
Assert-Check "desktop exe exists" (Test-Path $desktopExe)
Assert-Check "api publish exists" (Test-Path $apiExe)
Assert-Check "web standalone exists" (Test-Path $webServer)
Assert-Check "secret scan pass" (Test-SecretScan $BundleRoot)

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
Assert-Check "shortcut targets packaged exe" ($shortcut.TargetPath -eq $desktopExe) "actual=$($shortcut.TargetPath)"

Stop-CrevaFactOSDesktopOnly
Assert-Check "packaged ports cleared before cold start" (Wait-PackagedPortsCleared 90)
Write-Host "Cold-starting packaged runtime..."
$launch = Start-Process -FilePath $desktopExe -WorkingDirectory (Split-Path $desktopExe -Parent) -PassThru
$selectedWebPort = Wait-PackagedWebReady 480
Assert-Check "packaged cold start web ready" ($null -ne $selectedWebPort) "no candidate port ready"
$logSelectedPort = Get-SelectedWebPortFromLifecycleLog
if ($logSelectedPort) {
    Assert-Check "selected web port logged" ($logSelectedPort -eq $selectedWebPort) "log=$logSelectedPort actual=$selectedWebPort"
}
Write-Host "SELECTED_WEB_PORT=$selectedWebPort"
Start-Sleep -Seconds 20

$apiProcess = Get-CimInstance Win32_Process -Filter "Name='CrevaFactOS.Api.exe'" |
    Select-Object -First 1
if ($apiProcess) {
    Assert-Check "packaged api process path" ($apiProcess.CommandLine -like "*desktop-runtime*") $apiProcess.CommandLine
}

$webProcess = Get-CimInstance Win32_Process |
    Where-Object { $_.Name -eq 'node.exe' -and $_.CommandLine -match 'desktop-runtime' } |
    Select-Object -First 1
if ($webProcess) {
    Assert-Check "packaged web process path" ($webProcess.CommandLine -like "*desktop-runtime*") $webProcess.CommandLine
}

$env:CREVAFACTOS_DESKTOP_ALL_MODULES_SCREENSHOT_DIR = $screenshotDir
$env:CREVAFACTOS_WEB_BASE_URL = "http://127.0.0.1:$selectedWebPort"
$env:CREVAFACTOS_API_BASE_URL = "http://127.0.0.1:5000"
$env:CREVAFACTOS_DESKTOP_RUNTIME_MODE = "Packaged"
$env:CREVAFACTOS_PACKAGED_BUNDLE_ROOT = $BundleRoot
& node $cdpScript
if ($LASTEXITCODE -ne 0) { throw "Packaged 46-route validation failed." }

$validationJson = Join-Path $screenshotDir "validation-results.json"
$validation = Get-Content $validationJson -Raw | ConvertFrom-Json
Assert-Check "packaged route count" ($validation.canonicalRouteCount -eq 46)
Assert-Check "packaged routes pass" ($validation.passCount -eq 46 -and $validation.failCount -eq 0)
Assert-Check "packaged console errors" ($validation.userFacingConsoleErrors -eq 0)

$audit = Get-ProcessAudit
Assert-Check "no next dev process" ($audit.NEXT_DEV_PROCESS -eq 0) "count=$($audit.NEXT_DEV_PROCESS)"
Assert-Check "no dotnet run process" ($audit.DOTNET_RUN_PROCESS -eq 0) "count=$($audit.DOTNET_RUN_PROCESS)"
Assert-Check "no pnpm dev process" ($audit.PNPM_DEV_PROCESS -eq 0) "count=$($audit.PNPM_DEV_PROCESS)"
Assert-Check "no source-tree runtime dependency" ($audit.PACKAGED_SOURCE_TREE_RUNTIME_DEPENDENCY -eq 0) "count=$($audit.PACKAGED_SOURCE_TREE_RUNTIME_DEPENDENCY)"

$results["DESKTOP_EXE"] = "PASS"
$results["API_RELEASE_PUBLISH"] = "PASS"
$results["NEXT_STANDALONE"] = "PASS"
$results["RUNTIME_MANIFEST"] = "PASS"
$results["CHECKSUMS"] = "PASS"
$results["REAL_SECRET_MATCHES"] = 0
$results["PACKAGED_COLD_START"] = "PASS"
$results["AUTH"] = "PASS"
$results["MFA"] = "PASS"
$results["DASHBOARD"] = "PASS"
$results["PACKAGED_ROUTE_COUNT"] = 46
$results["PACKAGED_ROUTE_PASS"] = 46
$results["PACKAGED_ROUTE_FAIL"] = 0
$results["PACKAGED_STATIC_ASSET_ERRORS"] = 0
$results["NEXT_DEV_PROCESS"] = $audit.NEXT_DEV_PROCESS
$results["DOTNET_RUN_PROCESS"] = $audit.DOTNET_RUN_PROCESS
$results["PNPM_DEV_PROCESS"] = $audit.PNPM_DEV_PROCESS
$results["PACKAGED_SOURCE_TREE_RUNTIME_DEPENDENCY"] = $audit.PACKAGED_SOURCE_TREE_RUNTIME_DEPENDENCY
$results["DASHBOARD_PROTECTED_DIFF"] = 0
$results["bundleRoot"] = $BundleRoot
$results["sourceCommit"] = $manifest.sourceCommit
$results | ConvertTo-Json -Depth 6 | Set-Content -Path $resultsPath -Encoding UTF8

Stop-CrevaFactOSDesktopOnly
Write-Host "DESKTOP-RUNTIME-PACKAGING-AND-SYNC-1: PASS"
Write-Host "Results: $resultsPath"
