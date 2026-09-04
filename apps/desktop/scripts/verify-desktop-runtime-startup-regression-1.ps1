# CREVAFACTOS-DESKTOP-RUNTIME-STARTUP-REGRESSION-CLOSURE-1
# Three consecutive real shortcut cold launches with web lifecycle proof.

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
. (Join-Path $PSScriptRoot "desktop-verifier-isolation.ps1")
$shortcutPath = Join-Path ([Environment]::GetFolderPath("Desktop")) "CrevaFactOS.lnk"
$screenshotDir = Join-Path $repoRoot ".cursor-screenshots\desktop-sales-demo-1"
$desktopProject = Join-Path $repoRoot "apps\desktop\CrevaFactOS.Desktop\CrevaFactOS.Desktop.csproj"
$cdpScript = Join-Path $PSScriptRoot "verify-desktop-authentication-live-1-cdp.mjs"
$results = [ordered]@{}

function Assert-Check([string]$Name, [bool]$Ok, [string]$Detail = "") {
    $results[$Name] = if ($Ok) { "PASS" } else { "FAIL" }
    Write-Host "$(if ($Ok) { 'PASS' } else { 'FAIL' }) $Name$(if ($Detail) { " - $Detail" } else { '' })"
    if (-not $Ok) { throw "$Name failed$(if ($Detail) { ": $Detail" } else { '' })" }
}

function Stop-CrevaFactOSDesktopOnly {
    Get-Process -Name "CrevaFactOS", "CrevaFactOS.Desktop" -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 4
}

function Get-Port3001State {
    $listeners = @(Get-NetTCPConnection -LocalPort 3001 -State Listen -ErrorAction SilentlyContinue)
    if ($listeners.Count -eq 0) { return "NOT_RUNNING" }
    $listenerPid = $listeners[0].OwningProcess
    $healthy = $false
    try {
        $response = Invoke-WebRequest "http://127.0.0.1:3001/giris" -UseBasicParsing -TimeoutSec 8
        $healthy = ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300)
    } catch { $healthy = $false }
    if ($healthy) { return "RUNNING_HEALTHY pid=$listenerPid" }
    return "RUNNING_BROKEN pid=$listenerPid"
}

function Test-IdentityProxyReady {
    try {
        $body = '{"organizationContext":"__probe__","email":"probe@invalid.local","password":"__probe__","deviceLabel":"probe"}'
        $response = Invoke-WebRequest "http://127.0.0.1:3001/api/identity/sign-in" -Method POST -Body $body -ContentType "application/json" -UseBasicParsing -TimeoutSec 8
        $contentType = [string]$response.Headers["Content-Type"]
        return ($contentType -like "*json*" -or $response.Content.TrimStart().StartsWith("{"))
    } catch {
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $text = $reader.ReadToEnd()
            return $text.TrimStart().StartsWith("{")
        }
        return $false
    }
}

function Wait-WebReady([int]$TimeoutSeconds = 360) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $giris = Invoke-WebRequest "http://127.0.0.1:3001/giris" -UseBasicParsing -TimeoutSec 8
            if ($giris.StatusCode -ge 200 -and $giris.StatusCode -lt 300 -and (Test-IdentityProxyReady)) {
                return $true
            }
        } catch { }
        Start-Sleep -Seconds 2
    }
    return $false
}

Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing @"
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
public static class RuntimeRegressionWindowProbe {
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
  public struct RECT { public int Left, Top, Right, Bottom; }
  public static bool MainWindowVisible(int timeoutMs) {
    var deadline = Environment.TickCount + timeoutMs;
    while (Environment.TickCount < deadline) {
      foreach (var name in new[] { "CrevaFactOS", "CrevaFactOS.Desktop" }) {
        foreach (var process in Process.GetProcessesByName(name)) {
          process.Refresh();
          if (process.MainWindowHandle == IntPtr.Zero || !IsWindowVisible(process.MainWindowHandle)) continue;
          RECT rect;
          GetWindowRect(process.MainWindowHandle, out rect);
          var width = Math.Max(0, rect.Right - rect.Left);
          var height = Math.Max(0, rect.Bottom - rect.Top);
          if (width >= 1000 && height >= 700) return true;
        }
      }
      System.Threading.Thread.Sleep(500);
    }
    return false;
  }
}
"@

function Wait-MainWindow([int]$TimeoutSeconds = 360) {
    return [RuntimeRegressionWindowProbe]::MainWindowVisible($TimeoutSeconds * 1000)
}

New-Item -ItemType Directory -Force -Path $screenshotDir | Out-Null
Clear-KnownDesktopVerifierEnvironmentLeaks
Stop-CrevaFactOSDesktopOnly
Assert-RuntimeRootEnvironmentClean -Context "runtime-regression-start"

Write-Host "Publishing desktop artifact..."
& dotnet publish $desktopProject -c Release -r win-x64 --self-contained false -p:DesktopDevelopmentDistribution=true -o (Join-Path $repoRoot "artifacts\desktop\CrevaFactOS")
if ($LASTEXITCODE -ne 0) { throw "Desktop publish failed." }

Assert-Check "shortcut exists" (Test-Path $shortcutPath) $shortcutPath

$cycles = @(
    @{ Name = "Cold Launch Cycle 1"; Screenshot = "27-runtime-regression-cold-start.png" },
    @{ Name = "Cold Launch Cycle 2"; Screenshot = "29-runtime-regression-second-launch.png" },
    @{ Name = "Cold Launch Cycle 3"; Screenshot = "30-runtime-regression-third-launch.png" }
)

$cycleIndex = 0
foreach ($cycle in $cycles) {
    $cycleIndex++
    Write-Host "=== $($cycle.Name) ==="
    Stop-CrevaFactOSDesktopOnly
    $closedState = Get-Port3001State
    $results["cycle-$cycleIndex-port-when-desktop-closed"] = $closedState
    Write-Host "Port 3001 when desktop closed: $closedState"

    $launch = Start-Process -FilePath $shortcutPath -PassThru
    $sw = [Diagnostics.Stopwatch]::StartNew()
    $webReady = Wait-WebReady
    $sw.Stop()
    Assert-Check "$($cycle.Name) web readiness" $webReady "elapsed=$($sw.Elapsed.TotalSeconds.ToString('F1'))s"
    $results["cycle-$cycleIndex-web-startup-seconds"] = [math]::Round($sw.Elapsed.TotalSeconds, 1)

    Assert-Check "$($cycle.Name) API /health/ready" ((Invoke-WebRequest "http://127.0.0.1:5000/health/ready" -UseBasicParsing -TimeoutSec 10).StatusCode -eq 200)
    Assert-Check "$($cycle.Name) main window" (Wait-MainWindow)

    Write-Host "Running CDP auth flow for $($cycle.Name)..."
    & node $cdpScript
    if ($LASTEXITCODE -ne 0) { throw "CDP authentication failed on $($cycle.Name)" }

    if ($cycleIndex -eq 1) {
        Copy-Item (Join-Path $screenshotDir "20-desktop-authenticated-dashboard.png") (Join-Path $screenshotDir "28-runtime-regression-dashboard.png") -Force
        Copy-Item (Join-Path $screenshotDir "20-desktop-authenticated-dashboard.png") (Join-Path $screenshotDir $cycle.Screenshot) -Force
    } else {
        Copy-Item (Join-Path $screenshotDir "20-desktop-authenticated-dashboard.png") (Join-Path $screenshotDir $cycle.Screenshot) -Force
    }

    Assert-Check "$($cycle.Name) /dashboard" ((Invoke-WebRequest "http://127.0.0.1:3001/dashboard" -UseBasicParsing -TimeoutSec 15).StatusCode -eq 200)

    if ($cycleIndex -eq 3) {
        Write-Host "Cycle 3: 60-second stability hold..."
        Start-Sleep -Seconds 60
        Assert-Check "60 second stability desktop alive" ([bool](Get-Process -Name CrevaFactOS -ErrorAction SilentlyContinue))
        Assert-Check "60 second stability /dashboard" ((Invoke-WebRequest "http://127.0.0.1:3001/dashboard" -UseBasicParsing -TimeoutSec 15).StatusCode -eq 200)
        Assert-Check "60 second stability API" ((Invoke-WebRequest "http://127.0.0.1:5000/health/ready" -UseBasicParsing -TimeoutSec 10).StatusCode -eq 200)
    }

    Stop-CrevaFactOSDesktopOnly
    $results[$cycle.Name] = "PASS"
}

Assert-RepositoryRootValid -RepositoryRoot $repoRoot -Context "runtime-regression-final"
Assert-RuntimeRootEnvironmentClean -Context "runtime-regression-final"
$results["FINAL VERDICT"] = "DESKTOP_SALES_DEMO_READY"
$results | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $screenshotDir "runtime-startup-regression-results.json") -Encoding UTF8
Clear-KnownDesktopVerifierEnvironmentLeaks
Write-Host "CREVAFACTOS-DESKTOP-RUNTIME-STARTUP-REGRESSION-CLOSURE-1: PASS"
