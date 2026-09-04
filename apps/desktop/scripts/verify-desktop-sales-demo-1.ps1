# CREVAFACTOS-DESKTOP-SHELL-SALES-DEMO-1 verification
# Captures screenshots and validates desktop shell acceptance flow.

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
. (Join-Path $PSScriptRoot "desktop-verifier-isolation.ps1")
$desktopExe = Join-Path $repoRoot "apps\desktop\CrevaFactOS.Desktop\bin\Release\net10.0-windows\win-x64\CrevaFactOS.exe"
if (-not (Test-Path $desktopExe)) {
    $desktopExe = Join-Path $repoRoot "apps\desktop\CrevaFactOS.Desktop\bin\Debug\net10.0-windows\win-x64\CrevaFactOS.exe"
}
$screenshotDir = Join-Path $repoRoot ".cursor-screenshots\desktop-sales-demo-1"
$results = [ordered]@{}
$failures = New-Object System.Collections.Generic.List[string]

function Assert-Check([string]$Name, [bool]$Ok, [string]$Detail = "") {
    $results[$Name] = if ($Ok) { "PASS" } else { "FAIL" }
    if (-not $Ok) { $failures.Add("$Name$(if ($Detail) { ": $Detail" } else { '' })") }
    Write-Host "$(if ($Ok) { 'PASS' } else { 'FAIL' }) $Name$(if ($Detail) { " - $Detail" } else { '' })"
}

Add-Type -ReferencedAssemblies System.Drawing @"
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public static class DesktopSalesDemoCapture
{
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    public static IntPtr WaitForWindow(Func<Process, bool> predicate, int timeoutMs)
    {
        var deadline = Environment.TickCount + timeoutMs;
        while (Environment.TickCount < deadline)
        {
            foreach (var processName in new[] { "CrevaFactOS", "CrevaFactOS.Desktop" })
            {
                foreach (var process in Process.GetProcessesByName(processName))
                {
                    process.Refresh();
                    if (process.MainWindowHandle != IntPtr.Zero && predicate(process))
                    {
                        return process.MainWindowHandle;
                    }
                }
            }
            System.Threading.Thread.Sleep(300);
        }
        throw new TimeoutException("Desktop window not found.");
    }

    public static void Capture(IntPtr hWnd, string path)
    {
        ShowWindow(hWnd, 9);
        SetForegroundWindow(hWnd);
        System.Threading.Thread.Sleep(800);
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
}
"@

function Get-CrevaFactOSProcessCount {
    $names = @("CrevaFactOS", "CrevaFactOS.Desktop")
    $count = 0
    foreach ($name in $names) {
        $count += @(Get-Process -Name $name -ErrorAction SilentlyContinue).Count
    }
    return $count
}

function Stop-CrevaFactOSDesktop {
    Get-Process -Name "CrevaFactOS", "CrevaFactOS.Desktop" -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 2
}

function Ensure-DesktopBuilt {
    Stop-CrevaFactOSDesktop
    & dotnet build (Join-Path $repoRoot "apps\desktop\CrevaFactOS.Desktop\CrevaFactOS.Desktop.csproj") -c Release -p:DesktopDevelopmentDistribution=true
    if ($LASTEXITCODE -ne 0) { throw "Desktop build failed." }
    if (-not (Test-Path $desktopExe)) { throw "Desktop executable missing after build: $desktopExe" }
}

function Start-DesktopRoute([string]$EntryPath, [string]$ScreenshotPath = "") {
    $env:CREVAFACTOS_DESKTOP_ENTRY_PATH = $EntryPath
    $arguments = @()
    if (-not [string]::IsNullOrWhiteSpace($ScreenshotPath)) {
        $env:CREVAFACTOS_DESKTOP_SCREENSHOT_PATH = $ScreenshotPath
        $arguments += "--desktop-screenshot=`"$ScreenshotPath`""
    }
    return Start-Process -FilePath $desktopExe -ArgumentList $arguments -WorkingDirectory (Split-Path $desktopExe -Parent) -PassThru
}

function Wait-ForWebViewScreenshot([string]$ShotPath, [int]$TimeoutSeconds = 180) {
    $readyPath = "$ShotPath.ready"
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if ((Test-Path $readyPath) -and (Test-Path $ShotPath)) {
            Remove-Item $readyPath -Force -ErrorAction SilentlyContinue
            return $true
        }
        Start-Sleep -Seconds 1
    }
    return $false
}

function Capture-RouteScreenshot([string]$EntryPath, [string]$ShotName, [int]$TimeoutSeconds = 180) {
    Stop-CrevaFactOSDesktop
    $shot = Join-Path $screenshotDir $ShotName
    Remove-Item $shot, "$shot.ready", "$shot.configured", "$shot.error" -Force -ErrorAction SilentlyContinue
    $proc = Start-DesktopRoute $EntryPath $shot
    $captured = $false
    try {
        $handle = [DesktopSalesDemoCapture]::WaitForWindow({ param($p) $p.MainWindowHandle -ne [IntPtr]::Zero }, $TimeoutSeconds * 1000)
        $captured = Wait-ForWebViewScreenshot $shot $TimeoutSeconds
        if (-not $captured) {
            Start-Sleep -Seconds 8
            [DesktopSalesDemoCapture]::Capture($handle, $shot)
            $captured = Test-Path $shot
        }
    }
    finally {
        Remove-Item Env:CREVAFACTOS_DESKTOP_SCREENSHOT_PATH -ErrorAction SilentlyContinue
        Stop-CrevaFactOSDesktop
    }
    return $captured
}

New-Item -ItemType Directory -Force -Path $screenshotDir | Out-Null
Clear-KnownDesktopVerifierEnvironmentLeaks
Ensure-DesktopBuilt
Assert-Check "desktop build" (Test-Path $desktopExe) $desktopExe
Assert-RuntimeRootEnvironmentClean -Context "sales-demo-start"

Stop-CrevaFactOSDesktop

Write-Host "Launching CrevaFactOS desktop shell (startup)..."
$dashboardShot = Join-Path $screenshotDir "02-main-window-dashboard.png"
Remove-Item $dashboardShot, "$dashboardShot.ready" -Force -ErrorAction SilentlyContinue
$env:CREVAFACTOS_DESKTOP_SCREENSHOT_PATH = $dashboardShot
$process = Start-DesktopRoute "/dashboard" $dashboardShot
try {
    Start-Sleep -Seconds 2
    $startupHandle = [DesktopSalesDemoCapture]::WaitForWindow({ param($p) $p.MainWindowTitle -like "*CrevaFactOS*" }, 20000)
    $startupShot = Join-Path $screenshotDir "01-startup.png"
    [DesktopSalesDemoCapture]::Capture($startupHandle, $startupShot)
    Assert-Check "startup screenshot" (Test-Path $startupShot)

    $mainHandle = [DesktopSalesDemoCapture]::WaitForWindow({ param($p) $p.MainWindowHandle -ne [IntPtr]::Zero }, 180000)

    $beforeCount = Get-CrevaFactOSProcessCount
    if ($beforeCount -lt 1 -and $process -and -not $process.HasExited) {
        $beforeCount = 1
    }
    Start-Process -FilePath $desktopExe -WorkingDirectory (Split-Path $desktopExe -Parent) | Out-Null
    Start-Sleep -Seconds 4
    $afterCount = Get-CrevaFactOSProcessCount
    Assert-Check "single instance" ($beforeCount -ge 1 -and $afterCount -eq $beforeCount) "before=$beforeCount after=$afterCount"

    $dashboardCaptured = Wait-ForWebViewScreenshot $dashboardShot 180
    if (-not $dashboardCaptured) {
        Start-Sleep -Seconds 8
        [DesktopSalesDemoCapture]::Capture($mainHandle, $dashboardShot)
        $dashboardCaptured = Test-Path $dashboardShot
    }
    Remove-Item Env:CREVAFACTOS_DESKTOP_SCREENSHOT_PATH -ErrorAction SilentlyContinue
    Assert-Check "main window dashboard" $dashboardCaptured

    Assert-Check "API readiness" ((Invoke-WebRequest "http://127.0.0.1:5000/health/ready" -UseBasicParsing -TimeoutSec 10).StatusCode -eq 200)

    $webReady = $false
    try {
        $response = Invoke-WebRequest "http://127.0.0.1:3001/giris" -UseBasicParsing -TimeoutSec 10
        $webReady = ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300)
    } catch { $webReady = $false }
    Assert-Check "web readiness" $webReady "http://127.0.0.1:3001/giris 2xx"
}
finally {
    Stop-CrevaFactOSDesktop
}

Assert-Check "ai task center route" (Capture-RouteScreenshot "/" "03-ai-task-center.png")
Assert-Check "production route" (Capture-RouteScreenshot "/uretim" "04-production.png")
Assert-Check "playbooks route" (Capture-RouteScreenshot "/ai-gorevleri/playbooklar" "05-playbooks.png")
Assert-Check "playbook run detail route" (Capture-RouteScreenshot "/ai-gorevleri/playbooklar?gorunum=calisan" "06-playbook-run-detail.png")

Stop-CrevaFactOSDesktop
Start-Sleep -Seconds 3
$restartOk = Capture-RouteScreenshot "/dashboard" "02-main-window-dashboard.png" 180
Assert-Check "restart" $restartOk

Write-Host "Verifying authenticated desktop session (WebView2 CDP)..."
$authCdpScript = Join-Path $PSScriptRoot "verify-desktop-authentication-live-1-cdp.mjs"
& node $authCdpScript
Assert-Check "authenticated desktop session" ($LASTEXITCODE -eq 0) "verify-desktop-authentication-live-1-cdp.mjs"
Assert-Check "authenticated dashboard screenshot" (Test-Path (Join-Path $screenshotDir "20-desktop-authenticated-dashboard.png"))

$invalidRoot = Join-Path $env:TEMP "crevafactos-invalid-runtime-root"
Remove-Item Env:CREVAFACTOS_DESKTOP_ENTRY_PATH -ErrorAction SilentlyContinue
$failShot = Join-Path $screenshotDir "07-startup-failure.png"
$null = Invoke-NegativeRuntimeRootTest -ArtifactExe $desktopExe -ArtifactDir (Split-Path $desktopExe -Parent) -InvalidRoot $invalidRoot -OnFailureWindow {
    param($Process, $TimeoutSeconds)
    Start-Sleep -Seconds 4
    $failHandle = [DesktopSalesDemoCapture]::WaitForWindow({ param($p) $p.MainWindowTitle -like "*CrevaFactOS*" }, 20000)
    [DesktopSalesDemoCapture]::Capture($failHandle, $failShot)
    Assert-Check "startup failure screenshot" (Test-Path $failShot)
}
Stop-CrevaFactOSDesktop
Assert-RepositoryRootValid -RepositoryRoot $repoRoot -Context "sales-demo-final"
Assert-RuntimeRootEnvironmentClean -Context "sales-demo-final"
Clear-KnownDesktopVerifierEnvironmentLeaks

$resultsPath = Join-Path $screenshotDir "results.json"
$results | ConvertTo-Json | Set-Content -Path $resultsPath -Encoding UTF8

if ($failures.Count -gt 0) {
    Write-Error ("DESKTOP-SALES-DEMO-1 verification failed:`n" + ($failures -join "`n"))
    exit 1
}

Write-Host "DESKTOP-SALES-DEMO-1 verification: PASS"
Write-Host "Screenshots: $screenshotDir"
