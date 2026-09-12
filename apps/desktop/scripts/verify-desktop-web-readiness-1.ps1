# NevaxisFactOS-DESKTOP-SHELL-WEB-READINESS-TIMEOUT-CLOSURE-1
# Validates web cold start from real desktop shortcut.

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
. (Join-Path $PSScriptRoot "desktop-verifier-isolation.ps1")
$shortcutPath = Join-Path ([Environment]::GetFolderPath("Desktop")) "NevaxisFactOS.lnk"
$screenshotDir = Join-Path $repoRoot ".cursor-screenshots\desktop-sales-demo-1"
$results = [ordered]@{}

function Assert-Check([string]$Name, [bool]$Ok, [string]$Detail = "") {
    $results[$Name] = if ($Ok) { "PASS" } else { "FAIL" }
    Write-Host "$(if ($Ok) { 'PASS' } else { 'FAIL' }) $Name$(if ($Detail) { " - $Detail" } else { '' })"
    if (-not $Ok) { throw "$Name failed$(if ($Detail) { ": $Detail" } else { '' })" }
}

Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing @"
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public static class WebReadinessProbe {
  public struct WindowMetrics { public int Width; public int Height; public bool Visible; }
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
  public struct RECT { public int Left, Top, Right, Bottom; }
  public static WindowMetrics Measure(IntPtr hWnd) {
    RECT rect; GetWindowRect(hWnd, out rect);
    return new WindowMetrics {
      Width = Math.Max(0, rect.Right - rect.Left),
      Height = Math.Max(0, rect.Bottom - rect.Top),
      Visible = IsWindowVisible(hWnd)
    };
  }
  public static void CaptureWindow(IntPtr hWnd, string path) {
    ShowWindow(hWnd, 9);
    SetForegroundWindow(hWnd);
    System.Threading.Thread.Sleep(1000);
    RECT rect; GetWindowRect(hWnd, out rect);
    int width = Math.Max(400, rect.Right - rect.Left);
    int height = Math.Max(300, rect.Bottom - rect.Top);
    using (var bmp = new Bitmap(width, height))
    using (var g = Graphics.FromImage(bmp)) {
      IntPtr hdc = g.GetHdc();
      PrintWindow(hWnd, hdc, 2);
      g.ReleaseHdc(hdc);
      bmp.Save(path, ImageFormat.Png);
    }
  }
  public static IntPtr? FindMainWindow(int timeoutMs) {
    var deadline = Environment.TickCount + timeoutMs;
    while (Environment.TickCount < deadline) {
      foreach (var name in new[] { "crevafactos", "NevaxisFactOS.Desktop" }) {
        foreach (var process in Process.GetProcessesByName(name)) {
          process.Refresh();
          if (process.MainWindowHandle == IntPtr.Zero) continue;
          var m = Measure(process.MainWindowHandle);
          if (m.Visible && m.Width >= 1000 && m.Height >= 700) return process.MainWindowHandle;
        }
      }
      System.Threading.Thread.Sleep(500);
    }
    return null;
  }
  public static bool MainWindowVisible(int timeoutMs) {
    return FindMainWindow(timeoutMs).HasValue;
  }
}
"@

function Stop-NevaxisFactOSDesktop {
    Get-Process -Name "crevafactos", "NevaxisFactOS.Desktop" -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 3
    Get-Process -Name "crevafactos", "NevaxisFactOS.Desktop" -ErrorAction SilentlyContinue | Stop-Process -Force
}

function Stop-UnhealthyWebListener {
    $listeners = Get-NetTCPConnection -LocalPort 3001 -State Listen -ErrorAction SilentlyContinue
    foreach ($listener in $listeners) {
        $healthy = $false
        try {
            $response = Invoke-WebRequest "http://127.0.0.1:3001/giris" -UseBasicParsing -TimeoutSec 5
            $healthy = ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300)
        } catch { $healthy = $false }
        if (-not $healthy) {
            Stop-Process -Id $listener.OwningProcess -Force -ErrorAction SilentlyContinue
        }
    }
    Start-Sleep -Seconds 2
}

function Test-WebHttpReadiness {
    try {
        $response = Invoke-WebRequest "http://127.0.0.1:3001/giris" -UseBasicParsing -TimeoutSec 10
        return ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300)
    } catch { return $false }
}

function Get-NevaxisFactOSProcessCount {
    $count = 0
    foreach ($name in @("crevafactos", "NevaxisFactOS.Desktop")) {
        $count += @(Get-Process -Name $name -ErrorAction SilentlyContinue).Count
    }
    return $count
}

function Wait-ForWebAndMainWindow {
    param([DateTime]$Deadline)
    $webReady = $false
    $mainReady = $false
    $mainHandle = [IntPtr]::Zero
    while ((Get-Date) -lt $Deadline) {
        if (-not $webReady) { $webReady = Test-WebHttpReadiness }
        if (-not $mainReady) {
            $found = [WebReadinessProbe]::FindMainWindow(1500)
            if ($found.HasValue) {
                $mainReady = $true
                $mainHandle = $found.Value
            }
        }
        if ($webReady -and $mainReady) { return @{ WebReady = $true; MainReady = $true; MainHandle = $mainHandle } }
        if ((Get-NevaxisFactOSProcessCount) -eq 0 -and -not $webReady) { break }
        Start-Sleep -Seconds 2
    }
    return @{ WebReady = $webReady; MainReady = $mainReady; MainHandle = $mainHandle }
}

function Stop-NevaxisFactOSWebListener {
    $listeners = @(Get-NetTCPConnection -LocalPort 3001 -State Listen -ErrorAction SilentlyContinue)
    foreach ($listener in $listeners) {
        Stop-Process -Id $listener.OwningProcess -Force -ErrorAction SilentlyContinue
    }
    Start-Sleep -Seconds 2
}

function Invoke-RetryButton([IntPtr]$WindowHandle) {
    if ($WindowHandle -eq [IntPtr]::Zero) { return $false }
    try {
        Add-Type -AssemblyName UIAutomationClient -ErrorAction Stop
        Add-Type -AssemblyName UIAutomationTypes -ErrorAction Stop
    } catch {
        if (-not ("System.Windows.Automation.AutomationElement" -as [type])) {
            return $false
        }
    }

    $deadline = (Get-Date).AddSeconds(15)
    while ((Get-Date) -lt $deadline) {
        $root = [System.Windows.Automation.AutomationElement]::FromHandle($WindowHandle)
        if ($null -ne $root) {
            $nameCondition = New-Object System.Windows.Automation.PropertyCondition(
                [System.Windows.Automation.AutomationElement]::NameProperty,
                "Yeniden Dene")
            $button = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $nameCondition)
            if ($null -ne $button) {
                $pattern = $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
                $invoke = [System.Windows.Automation.InvokePattern]$pattern
                $invoke.Invoke()
                return $true
            }
        }
        Start-Sleep -Milliseconds 500
    }
    return $false
}

New-Item -ItemType Directory -Force -Path $screenshotDir | Out-Null
Clear-KnownDesktopVerifierEnvironmentLeaks
Stop-NevaxisFactOSDesktop
Stop-UnhealthyWebListener
Assert-RuntimeRootEnvironmentClean -Context "web-readiness-start"

$desktopProject = Join-Path $repoRoot "apps\desktop\NevaxisFactOS.Desktop\NevaxisFactOS.Desktop.csproj"
Write-Host "Publishing desktop artifact..."
& dotnet publish $desktopProject -c Release -r win-x64 --self-contained false -p:DesktopDevelopmentDistribution=true -o (Join-Path $repoRoot "artifacts\desktop\NevaxisFactOS")
if ($LASTEXITCODE -ne 0) { throw "Desktop publish failed." }

Assert-Check "shortcut exists" (Test-Path $shortcutPath) $shortcutPath

# Phase 1: cold web start (stop only web listener; keep API/postgres)
Stop-NevaxisFactOSDesktop
Stop-NevaxisFactOSWebListener
Assert-Check "web port cleared before cold start" (-not (Get-NetTCPConnection -LocalPort 3001 -State Listen -ErrorAction SilentlyContinue))

$startedAt = Get-Date
$launch = Start-Process -FilePath $shortcutPath -PassThru
$waitResult = Wait-ForWebAndMainWindow -Deadline (Get-Date).AddMinutes(5)
$webReady = $waitResult.WebReady
$webElapsed = [math]::Round(((Get-Date) - $startedAt).TotalSeconds, 1)

$listeners = @(Get-NetTCPConnection -LocalPort 3001 -State Listen -ErrorAction SilentlyContinue)
Assert-Check "web port 3001 listening" ($listeners.Count -eq 1) "count=$($listeners.Count)"
Assert-Check "web HTTP readiness /giris" $webReady "elapsed=${webElapsed}s status=2xx"
if ($listeners.Count -ge 1) {
    Assert-Check "web listener pid" ($listeners[0].OwningProcess -gt 0) "pid=$($listeners[0].OwningProcess)"
}

Assert-Check "main window visible" $waitResult.MainReady
Assert-Check "main window handle for screenshot" ($waitResult.MainHandle -ne [IntPtr]::Zero)
[WebReadinessProbe]::CaptureWindow($waitResult.MainHandle, (Join-Path $screenshotDir "18-runtime-root-clean-cold-start-dashboard.png"))
Assert-Check "clean cold start screenshot" (Test-Path (Join-Path $screenshotDir "18-runtime-root-clean-cold-start-dashboard.png"))
try {
    $apiReady = (Invoke-WebRequest "http://127.0.0.1:5000/health/ready" -UseBasicParsing -TimeoutSec 10).StatusCode -eq 200
} catch { $apiReady = $false }
Assert-Check "API readiness" $apiReady

# Phase 2: close/reopen without stale port drift
$firstWebPid = if ($listeners.Count -ge 1) { $listeners[0].OwningProcess } else { 0 }
Stop-NevaxisFactOSDesktop
Start-Sleep -Seconds 3
$reopen = Start-Process -FilePath $shortcutPath -PassThru
$reopenResult = Wait-ForWebAndMainWindow -Deadline (Get-Date).AddMinutes(5)
Assert-Check "close reopen main window" $reopenResult.MainReady
$reopenListeners = @(Get-NetTCPConnection -LocalPort 3001 -State Listen -ErrorAction SilentlyContinue)
Assert-Check "close reopen single listener" ($reopenListeners.Count -eq 1) "count=$($reopenListeners.Count)"
Assert-Check "close reopen HTTP readiness" $reopenResult.WebReady
Stop-NevaxisFactOSDesktop
Start-Sleep -Seconds 2

# Phase 3: failure window -> Yeniden Dene -> recovery (child-process env only)
Stop-NevaxisFactOSDesktop
$invalidRoot = Join-Path $env:TEMP ("desktop-negative-runtime-" + [guid]::NewGuid().ToString("N"))
$artifactExe = Join-Path $repoRoot "artifacts\desktop\NevaxisFactOS\NevaxisFactOS.exe"
$artifactDir = Split-Path $artifactExe -Parent
$script:failureHandle = [IntPtr]::Zero
$failLaunch = Invoke-NegativeRuntimeRootTest -ArtifactExe $artifactExe -ArtifactDir $artifactDir -InvalidRoot $invalidRoot -OnFailureWindow {
    param($Process, $TimeoutSeconds)
    Start-Sleep -Seconds 8
    $failureDeadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $failureDeadline -and $script:failureHandle -eq [IntPtr]::Zero) {
        foreach ($name in @("crevafactos", "NevaxisFactOS.Desktop")) {
            foreach ($process in Get-Process -Name $name -ErrorAction SilentlyContinue) {
                $process.Refresh()
                if ($process.MainWindowHandle -eq [IntPtr]::Zero) { continue }
                $metrics = [WebReadinessProbe]::Measure($process.MainWindowHandle)
                if ($metrics.Visible -and $metrics.Height -ge 400 -and $metrics.Width -ge 530 -and $metrics.Width -lt 1000) {
                    $script:failureHandle = $process.MainWindowHandle
                    break
                }
            }
            if ($script:failureHandle -ne [IntPtr]::Zero) { break }
        }
        Start-Sleep -Milliseconds 500
    }
    Assert-Check "failure window visible" ($script:failureHandle -ne [IntPtr]::Zero)
    if ($script:failureHandle -ne [IntPtr]::Zero) {
        [WebReadinessProbe]::CaptureWindow($script:failureHandle, (Join-Path $screenshotDir "17-runtime-root-leak-diagnostics.png"))
        Assert-Check "runtime root leak diagnostics screenshot" (Test-Path (Join-Path $screenshotDir "17-runtime-root-leak-diagnostics.png"))
    }
    Assert-Check "retry button click" (Invoke-RetryButton $script:failureHandle)
}
$retryResult = Wait-ForWebAndMainWindow -Deadline (Get-Date).AddMinutes(5)
Assert-Check "retry recovery main window" ($retryResult.MainReady -and $retryResult.WebReady)
Stop-NevaxisFactOSDesktop

Assert-RepositoryRootValid -RepositoryRoot $repoRoot -Context "web-readiness-final"
Assert-RuntimeRootEnvironmentClean -Context "web-readiness-final"

$results["web startup elapsed seconds"] = "$webElapsed"
$results["web cold start port"] = "3001"
$results["web readiness url"] = "http://127.0.0.1:3001/giris"
$results["runtime root environment clean"] = "PASS"
$results["resolved repository root"] = $repoRoot
$results | ConvertTo-Json | Set-Content -Path (Join-Path $screenshotDir "web-readiness-results.json") -Encoding UTF8
Write-Host "WEB-READINESS-TIMEOUT-CLOSURE-1: PASS (web startup ${webElapsed}s)"
Stop-NevaxisFactOSDesktop
Clear-KnownDesktopVerifierEnvironmentLeaks
Assert-RuntimeRootEnvironmentClean -Context "web-readiness-exit"
