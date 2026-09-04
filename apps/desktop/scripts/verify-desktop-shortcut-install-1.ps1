# CREVAFACTOS-DESKTOP-SHELL-SHORTCUT-INSTALL-CLOSURE-1
# Verifies Windows desktop shortcut install and live launch behavior.

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
$artifactExe = Join-Path $repoRoot "artifacts\desktop\CrevaFactOS\CrevaFactOS.exe"
$artifactDir = Split-Path $artifactExe -Parent
$desktopFolder = [Environment]::GetFolderPath("Desktop")
$shortcutPath = Join-Path $desktopFolder "CrevaFactOS.lnk"
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

public static class ShortcutInstallCapture
{
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    [DllImport("user32.dll")] public static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);

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

    public static void CaptureScreen(string path)
    {
        int width = GetSystemMetrics(0);
        int height = GetSystemMetrics(1);
        using (var bmp = new Bitmap(width, height))
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(0, 0, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
            bmp.Save(path, ImageFormat.Png);
        }
    }

    public static void CaptureWindow(IntPtr hWnd, string path)
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
}
"@

function Get-CrevaFactOSProcessCount {
    $count = 0
    foreach ($name in @("CrevaFactOS", "CrevaFactOS.Desktop")) {
        $count += @(Get-Process -Name $name -ErrorAction SilentlyContinue).Count
    }
    return $count
}

function Stop-CrevaFactOSDesktop {
    Get-Process -Name "CrevaFactOS", "CrevaFactOS.Desktop" -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 2
}

function Launch-DesktopShortcut {
    if (-not (Test-Path $shortcutPath)) {
        throw "Shortcut missing: $shortcutPath"
    }
    return Start-Process -FilePath $shortcutPath -PassThru
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

New-Item -ItemType Directory -Force -Path $screenshotDir | Out-Null
Stop-CrevaFactOSDesktop

Assert-Check "shortcut exists" (Test-Path $shortcutPath) $shortcutPath

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
Assert-Check "shortcut target exists" (Test-Path $shortcut.TargetPath) $shortcut.TargetPath
Assert-Check "shortcut target path" ($shortcut.TargetPath -eq $artifactExe) "expected=$artifactExe actual=$($shortcut.TargetPath)"
Assert-Check "shortcut working directory" ((Test-Path $shortcut.WorkingDirectory) -and ($shortcut.WorkingDirectory -eq $artifactDir)) $shortcut.WorkingDirectory
Assert-Check "shortcut icon location" ($shortcut.IconLocation -like "$artifactExe,*") $shortcut.IconLocation

$duplicateCount = @(Get-ChildItem $desktopFolder -Filter "CrevaFactOS*.lnk" -ErrorAction SilentlyContinue).Count
Assert-Check "no duplicate shortcuts" ($duplicateCount -eq 1) "count=$duplicateCount"

$desktopShot = Join-Path $screenshotDir "08-windows-desktop-shortcut.png"
try {
    $explorer = New-Object -ComObject Shell.Application
    $explorer.MinimizeAll()
    Start-Sleep -Seconds 2
    [ShortcutInstallCapture]::CaptureScreen($desktopShot)
} catch {
    [ShortcutInstallCapture]::CaptureScreen($desktopShot)
}
Assert-Check "desktop shortcut screenshot" (Test-Path $desktopShot) $desktopShot

Write-Host "Launching from desktop shortcut..."
$dashboardShot = Join-Path $screenshotDir "09-shortcut-launched-dashboard.png"
Remove-Item $dashboardShot, "$dashboardShot.ready", "$dashboardShot.configured", "$dashboardShot.error" -Force -ErrorAction SilentlyContinue
$env:CREVAFACTOS_DESKTOP_ENTRY_PATH = "/dashboard"
$env:CREVAFACTOS_DESKTOP_SCREENSHOT_PATH = $dashboardShot
$launch = Launch-DesktopShortcut
try {
    $startupHandle = [ShortcutInstallCapture]::WaitForWindow({ param($p) $p.MainWindowHandle -ne [IntPtr]::Zero }, 30000)
    Start-Sleep -Seconds 2
    $splashSize = [ShortcutInstallCapture]::Measure($startupHandle)
    if ($splashSize.Width -lt 700) {
        [ShortcutInstallCapture]::CaptureWindow($startupHandle, (Join-Path $screenshotDir "09-shortcut-splash-phase.png"))
    }

    $mainHandle = $null
    $deadline = (Get-Date).AddMinutes(4)
    while ((Get-Date) -lt $deadline) {
        foreach ($processName in @("CrevaFactOS", "CrevaFactOS.Desktop")) {
            foreach ($process in Get-Process -Name $processName -ErrorAction SilentlyContinue) {
                $process.Refresh()
                if ($process.MainWindowHandle -eq [IntPtr]::Zero) { continue }
                $size = [ShortcutInstallCapture]::Measure($process.MainWindowHandle)
                if ($size.Visible -and $size.Width -ge 1000 -and $size.Height -ge 700) {
                    $mainHandle = $process.MainWindowHandle
                    break
                }
            }
            if ($mainHandle) { break }
        }
        if ($mainHandle) { break }
        if (-not (Get-Process -Id $launch.Id -ErrorAction SilentlyContinue)) {
            throw "Desktop process exited before main window became visible."
        }
        Start-Sleep -Seconds 2
    }
    if (-not $mainHandle) { throw "Main window not visible after shortcut launch." }
    $captured = Wait-ForWebViewScreenshot $dashboardShot 180
    if (-not $captured) {
        Start-Sleep -Seconds 8
        [ShortcutInstallCapture]::CaptureWindow($mainHandle, $dashboardShot)
        $captured = Test-Path $dashboardShot
    }
    Assert-Check "shortcut launch" $captured

    $beforeCount = Get-CrevaFactOSProcessCount
    if ($beforeCount -lt 1) { $beforeCount = 1 }
    Launch-DesktopShortcut | Out-Null
    Start-Sleep -Seconds 4
    $afterCount = Get-CrevaFactOSProcessCount
    Assert-Check "second launch single instance" ($afterCount -eq $beforeCount) "before=$beforeCount after=$afterCount"

    Stop-CrevaFactOSDesktop
    Start-Sleep -Seconds 3
    Launch-DesktopShortcut | Out-Null
    $reopenHandle = [ShortcutInstallCapture]::WaitForWindow({ param($p) $p.MainWindowHandle -ne [IntPtr]::Zero }, 240000)
    Assert-Check "close and reopen" ($reopenHandle -ne [IntPtr]::Zero)
}
finally {
    Remove-Item Env:CREVAFACTOS_DESKTOP_ENTRY_PATH -ErrorAction SilentlyContinue
    Remove-Item Env:CREVAFACTOS_DESKTOP_SCREENSHOT_PATH -ErrorAction SilentlyContinue
    Stop-CrevaFactOSDesktop
}

$resultsPath = Join-Path $screenshotDir "shortcut-install-results.json"
$results | ConvertTo-Json | Set-Content -Path $resultsPath -Encoding UTF8

Write-Host "SHORTCUT-INSTALL-CLOSURE-1 verification: PASS"
Write-Host "Screenshots: $screenshotDir"
