# NevaxisFactOS-DESKTOP-SHELL-SPLASH-TO-MAINWINDOW-LIVE-CLOSURE-1
# Proves real desktop shortcut splash-to-main-window handoff.

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
. (Join-Path $PSScriptRoot "desktop-verifier-isolation.ps1")
$artifactExe = Join-Path $repoRoot "artifacts\desktop\NevaxisFactOS\NevaxisFactOS.exe"
$artifactDir = Split-Path $artifactExe -Parent
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

public static class SplashMainWindowProbe
{
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    public struct WindowMetrics
    {
        public int Width;
        public int Height;
        public bool Visible;
    }

    public static WindowMetrics Measure(IntPtr hWnd)
    {
        RECT rect;
        GetWindowRect(hWnd, out rect);
        return new WindowMetrics
        {
            Width = Math.Max(0, rect.Right - rect.Left),
            Height = Math.Max(0, rect.Bottom - rect.Top),
            Visible = IsWindowVisible(hWnd)
        };
    }

    public static void CaptureWindow(IntPtr hWnd, string path)
    {
        ShowWindow(hWnd, 9);
        SetForegroundWindow(hWnd);
        System.Threading.Thread.Sleep(1000);
        RECT rect;
        GetWindowRect(hWnd, out rect);
        int width = Math.Max(800, rect.Right - rect.Left);
        int height = Math.Max(600, rect.Bottom - rect.Top);
        using (var bmp = new Bitmap(width, height))
        using (var g = Graphics.FromImage(bmp))
        {
            IntPtr hdc = g.GetHdc();
            PrintWindow(hWnd, hdc, 2);
            g.ReleaseHdc(hdc);
            bmp.Save(path, ImageFormat.Png);
        }
    }

    public static IntPtr? FindNevaxisFactOSWindow(Func<WindowMetrics, bool> predicate, int timeoutMs)
    {
        var deadline = Environment.TickCount + timeoutMs;
        while (Environment.TickCount < deadline)
        {
            foreach (var processName in new[] { "crevafactos", "NevaxisFactOS.Desktop" })
            {
                foreach (var process in Process.GetProcessesByName(processName))
                {
                    process.Refresh();
                    if (process.MainWindowHandle == IntPtr.Zero) continue;
                    var size = Measure(process.MainWindowHandle);
                    if (predicate(size)) return process.MainWindowHandle;
                }
            }
            System.Threading.Thread.Sleep(300);
        }
        return null;
    }
}
"@

function Get-NevaxisFactOSProcessCount {
    $count = 0
    foreach ($name in @("crevafactos", "NevaxisFactOS.Desktop")) {
        $count += @(Get-Process -Name $name -ErrorAction SilentlyContinue).Count
    }
    return $count
}

function Stop-NevaxisFactOSDesktop {
    Get-Process -Name "crevafactos", "NevaxisFactOS.Desktop" -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 2
}

function Reset-DesktopRuntimeEnvironment {
    Stop-NevaxisFactOSDesktop
    foreach ($port in @(3001, 5000)) {
        $connections = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
        foreach ($connection in $connections) {
            $proc = Get-Process -Id $connection.OwningProcess -ErrorAction SilentlyContinue
            if (-not $proc) { continue }
            $healthy = $false
            if ($port -eq 5000) {
                try { $healthy = (Invoke-WebRequest "http://127.0.0.1:5000/health/ready" -UseBasicParsing -TimeoutSec 3).StatusCode -eq 200 } catch { $healthy = $false }
            } else {
                try { $healthy = (Invoke-WebRequest "http://127.0.0.1:3001/giris" -UseBasicParsing -TimeoutSec 3).StatusCode -eq 200 } catch { $healthy = $false }
            }
            if (-not $healthy -and $proc.ProcessName -in @("node", "dotnet")) {
                Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
            }
        }
    }
    Start-Sleep -Seconds 2
}

function Launch-DesktopShortcut {
    if (-not (Test-Path $shortcutPath)) { throw "Shortcut missing: $shortcutPath" }
    return Start-Process -FilePath $shortcutPath -PassThru
}

function Test-MainWindowVisible {
    param([int]$TimeoutSeconds = 240)
    $handle = [SplashMainWindowProbe]::FindNevaxisFactOSWindow({ param($s) $s.Visible -and $s.Width -ge 1000 -and $s.Height -ge 700 }, $TimeoutSeconds * 1000)
    if ($handle -eq $null) { return $null }
    return $handle
}

function Test-SplashWindowVisible {
    param([int]$TimeoutSeconds = 30)
    $handle = [SplashMainWindowProbe]::FindNevaxisFactOSWindow({ param($s) $s.Visible -and $s.Width -lt 700 }, $TimeoutSeconds * 1000)
    if ($handle -eq $null) { return $null }
    return $handle
}

function Wait-ProcessExit([int]$ProcessId, [int]$TimeoutSeconds = 30) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (-not (Get-Process -Id $ProcessId -ErrorAction SilentlyContinue)) { return $true }
        Start-Sleep -Milliseconds 500
    }
    return $false
}

function Invoke-CdpNavigate([string]$Path) {
    try {
        $cdpScript = Join-Path $PSScriptRoot "verify-desktop-sales-demo-1-cdp.mjs"
        & node $cdpScript $Path | Out-Null
        return $LASTEXITCODE -eq 0
    } catch {
        return $false
    }
}

New-Item -ItemType Directory -Force -Path $screenshotDir | Out-Null
Clear-KnownDesktopVerifierEnvironmentLeaks
Reset-DesktopRuntimeEnvironment
Assert-RuntimeRootEnvironmentClean -Context "splash-start"

Assert-Check "shortcut exists" (Test-Path $shortcutPath) $shortcutPath
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
Assert-Check "shortcut target" ($shortcut.TargetPath -eq $artifactExe) $shortcut.TargetPath
Assert-Check "shortcut working directory" ($shortcut.WorkingDirectory -eq $artifactDir) $shortcut.WorkingDirectory

Write-Host "Launching from desktop shortcut (real user flow)..."
$launch = Launch-DesktopShortcut
$splashHandle = Test-SplashWindowVisible 30
Assert-Check "splash visible" ($splashHandle -ne $null)
if ($splashHandle -ne $null) {
    [SplashMainWindowProbe]::CaptureWindow($splashHandle, (Join-Path $screenshotDir "10-real-shortcut-splash.png"))
}

$mainHandle = Test-MainWindowVisible 300
Assert-Check "main window visible after splash" ($mainHandle -ne $null)
Assert-Check "desktop process alive after handoff" (-not (Wait-ProcessExit $launch.Id 10)) "pid=$($launch.Id)"

Start-Sleep -Seconds 3
[SplashMainWindowProbe]::CaptureWindow($mainHandle, (Join-Path $screenshotDir "11-real-shortcut-mainwindow-dashboard.png"))
Assert-Check "mainwindow screenshot" (Test-Path (Join-Path $screenshotDir "11-real-shortcut-mainwindow-dashboard.png"))

try {
    $apiReady = (Invoke-WebRequest "http://127.0.0.1:5000/health/ready" -UseBasicParsing -TimeoutSec 10).StatusCode -eq 200
} catch { $apiReady = $false }
Assert-Check "API readiness during shortcut launch" $apiReady

$webReady = $false
try {
    $response = Invoke-WebRequest "http://127.0.0.1:3001/giris" -UseBasicParsing -TimeoutSec 10
    $webReady = ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300)
} catch { $webReady = $false }
Assert-Check "web readiness during shortcut launch" $webReady "http://127.0.0.1:3001/giris 2xx"

Write-Host "Waiting 60 seconds for main window persistence..."
$persistDeadline = (Get-Date).AddSeconds(60)
$persistOk = $true
while ((Get-Date) -lt $persistDeadline) {
    if (Wait-ProcessExit $launch.Id 1) { $persistOk = $false; break }
    $stillVisible = Test-MainWindowVisible 2
    if ($stillVisible -eq $null) { $persistOk = $false; break }
    Start-Sleep -Seconds 5
}
Assert-Check "60-second persistence" $persistOk

$navOk = Invoke-CdpNavigate "/ai-gorevleri/playbooklar?gorunum=calisan"
Assert-Check "playbook detail navigation" $navOk
Start-Sleep -Seconds 4
$mainHandle = Test-MainWindowVisible 10
if ($mainHandle -ne $null) {
    [SplashMainWindowProbe]::CaptureWindow($mainHandle, (Join-Path $screenshotDir "12-real-shortcut-playbook-detail.png"))
}
Assert-Check "playbook detail screenshot" (Test-Path (Join-Path $screenshotDir "12-real-shortcut-playbook-detail.png"))

$beforeCount = Get-NevaxisFactOSProcessCount
Launch-DesktopShortcut | Out-Null
Start-Sleep -Seconds 4
$afterCount = Get-NevaxisFactOSProcessCount
Assert-Check "second launch single instance" ($afterCount -eq $beforeCount) "before=$beforeCount after=$afterCount"

Stop-NevaxisFactOSDesktop
Start-Sleep -Seconds 3
$relaunch = Launch-DesktopShortcut
$reopenHandle = Test-MainWindowVisible 300
Assert-Check "close and reopen from shortcut" ($reopenHandle -ne $null)
Stop-NevaxisFactOSDesktop

Write-Host "Producing controlled startup failure..."
$invalidRoot = Join-Path $env:TEMP "NevaxisFactOS-invalid-runtime-root"
$failureHandle = $null
$failLaunch = Invoke-NegativeRuntimeRootTest -ArtifactExe $artifactExe -ArtifactDir $artifactDir -InvalidRoot $invalidRoot -OnFailureWindow {
    param($Process, $TimeoutSeconds)
    Remove-Item Env:NEVAXIS_FACTOS_DESKTOP_ENTRY_PATH -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 6
    $script:failureHandle = [SplashMainWindowProbe]::FindNevaxisFactOSWindow({ param($s) $s.Visible -and $s.Height -ge 400 -and $s.Width -ge 530 -and $s.Width -lt 1000 }, $TimeoutSeconds * 1000)
    Assert-Check "startup failure window visible" ($script:failureHandle -ne $null)
    if ($script:failureHandle -ne $null) {
        [SplashMainWindowProbe]::CaptureWindow($script:failureHandle, (Join-Path $screenshotDir "13-real-startup-failure-window.png"))
    }
    Assert-Check "failure process remains alive" (-not (Wait-ProcessExit $Process.Id 5))
}
Stop-NevaxisFactOSDesktop
Assert-RepositoryRootValid -RepositoryRoot $repoRoot -Context "splash-final"
Assert-RuntimeRootEnvironmentClean -Context "splash-final"
Clear-KnownDesktopVerifierEnvironmentLeaks

$resultsPath = Join-Path $screenshotDir "splash-to-mainwindow-results.json"
$results | ConvertTo-Json | Set-Content -Path $resultsPath -Encoding UTF8
Write-Host "SPLASH-TO-MAINWINDOW-LIVE-CLOSURE-1: PASS"
