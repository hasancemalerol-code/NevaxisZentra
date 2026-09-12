# NevaxisFactOS-DESKTOP-SHELL-AUTHENTICATION-LIVE-CLOSURE-1
# Validates real shortcut login -> MFA -> authenticated dashboard in WebView2.

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
. (Join-Path $PSScriptRoot "desktop-verifier-isolation.ps1")
$shortcutPath = Join-Path ([Environment]::GetFolderPath("Desktop")) "NevaxisFactOS.lnk"
$screenshotDir = Join-Path $repoRoot ".cursor-screenshots\desktop-sales-demo-1"
$desktopProject = Join-Path $repoRoot "apps\desktop\NevaxisFactOS.Desktop\NevaxisFactOS.Desktop.csproj"
$cdpScript = Join-Path $PSScriptRoot "verify-desktop-authentication-live-1-cdp.mjs"
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
public static class DesktopAuthCapture {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
  public struct RECT { public int Left, Top, Right, Bottom; }
  public static IntPtr? FindMainWindow(int timeoutMs) {
    var deadline = Environment.TickCount + timeoutMs;
    while (Environment.TickCount < deadline) {
      foreach (var name in new[] { "crevafactos", "NevaxisFactOS.Desktop" }) {
        foreach (var process in Process.GetProcessesByName(name)) {
          process.Refresh();
          if (process.MainWindowHandle == IntPtr.Zero || !IsWindowVisible(process.MainWindowHandle)) continue;
          return process.MainWindowHandle;
        }
      }
      System.Threading.Thread.Sleep(500);
    }
    return null;
  }
  public static void Capture(IntPtr hWnd, string path) {
    ShowWindow(hWnd, 9);
    SetForegroundWindow(hWnd);
    System.Threading.Thread.Sleep(1000);
    RECT rect; GetWindowRect(hWnd, out rect);
    int width = Math.Max(800, rect.Right - rect.Left);
    int height = Math.Max(600, rect.Bottom - rect.Top);
    using (var bmp = new Bitmap(width, height))
    using (var g = Graphics.FromImage(bmp)) {
      IntPtr hdc = g.GetHdc();
      PrintWindow(hWnd, hdc, 2);
      g.ReleaseHdc(hdc);
      bmp.Save(path, ImageFormat.Png);
    }
  }
}
"@

function Stop-NevaxisFactOSDesktop {
    Get-Process -Name "crevafactos", "NevaxisFactOS.Desktop" -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 3
}

function Test-IdentityProxy {
    try {
        $body = '{"organizationContext":"__probe__","email":"probe@invalid.local","password":"__probe__","deviceLabel":"probe"}'
        $response = Invoke-WebRequest "http://127.0.0.1:3001/api/identity/sign-in" -Method POST -Body $body -ContentType "application/json" -UseBasicParsing -TimeoutSec 10
        return ($response.Headers["Content-Type"] -like "*json*")
    } catch {
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $text = $reader.ReadToEnd()
            return $text.TrimStart().StartsWith("{")
        }
        return $false
    }
}

New-Item -ItemType Directory -Force -Path $screenshotDir | Out-Null
Clear-KnownDesktopVerifierEnvironmentLeaks
Stop-NevaxisFactOSDesktop
Assert-RuntimeRootEnvironmentClean -Context "auth-live-start"

Write-Host "Publishing desktop artifact..."
& dotnet publish $desktopProject -c Release -r win-x64 --self-contained false -p:DesktopDevelopmentDistribution=true -o (Join-Path $repoRoot "artifacts\desktop\NevaxisFactOS")
if ($LASTEXITCODE -ne 0) { throw "Desktop publish failed." }

Assert-Check "shortcut exists" (Test-Path $shortcutPath) $shortcutPath

$launch = Start-Process -FilePath $shortcutPath -PassThru
$deadline = (Get-Date).AddMinutes(8)
$mainHandle = [IntPtr]::Zero
$webReady = $false
while ((Get-Date) -lt $deadline) {
    if (-not $webReady) {
        try {
            $response = Invoke-WebRequest "http://127.0.0.1:3001/giris" -UseBasicParsing -TimeoutSec 5
            $webReady = ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300)
        } catch { $webReady = $false }
    }
    if ($mainHandle -eq [IntPtr]::Zero) {
        $found = [DesktopAuthCapture]::FindMainWindow(1500)
        if ($found.HasValue) { $mainHandle = $found.Value }
    }
    if ($webReady) { break }
    if ($launch.HasExited) { break }
    Start-Sleep -Seconds 2
}

Assert-Check "web readiness /giris" $webReady
Assert-Check "identity proxy JSON" (Test-IdentityProxy)

Write-Host "Running WebView2 CDP authentication flow..."
& node $cdpScript
if ($LASTEXITCODE -ne 0) { throw "CDP authentication verification failed." }

Assert-Check "login screenshot" (Test-Path (Join-Path $screenshotDir "19-desktop-login.png"))
Assert-Check "authenticated dashboard screenshot (mandatory)" (Test-Path (Join-Path $screenshotDir "20-desktop-authenticated-dashboard.png"))
Assert-Check "authenticated playbook detail screenshot" (Test-Path (Join-Path $screenshotDir "21-desktop-authenticated-playbook-detail.png"))

for ($attempt = 0; $attempt -lt 30; $attempt++) {
    $found = [DesktopAuthCapture]::FindMainWindow(2000)
    if ($found.HasValue) { break }
    Start-Sleep -Seconds 2
}
Assert-Check "main window visible after auth" $found.HasValue

# Session restart test
Stop-NevaxisFactOSDesktop
Start-Sleep -Seconds 3
$relaunch = Start-Process -FilePath $shortcutPath -PassThru
$restartDeadline = (Get-Date).AddMinutes(5)
$restartOnDashboard = $false
while ((Get-Date) -lt $restartDeadline) {
    $found = [DesktopAuthCapture]::FindMainWindow(1500)
    if ($found.HasValue) {
        try {
            $me = Invoke-WebRequest "http://127.0.0.1:3001/api/identity/me" -UseBasicParsing -TimeoutSec 5
            if ($me.StatusCode -eq 200) { $restartOnDashboard = $true; break }
        } catch {
            try {
                $giris = Invoke-WebRequest "http://127.0.0.1:3001/giris" -UseBasicParsing -TimeoutSec 5
                if ($giris.StatusCode -ge 200 -and $giris.StatusCode -lt 300) { break }
            } catch { }
        }
    }
    Start-Sleep -Seconds 2
}
$restartBehavior = if ($restartOnDashboard) { "session-persists" } else { "login-required" }
$results["session restart behavior"] = $restartBehavior
Write-Host "Session restart behavior: $restartBehavior"

Stop-NevaxisFactOSDesktop
Assert-RepositoryRootValid -RepositoryRoot $repoRoot -Context "auth-live-final"
Assert-RuntimeRootEnvironmentClean -Context "auth-live-final"
$results | ConvertTo-Json | Set-Content -Path (Join-Path $screenshotDir "authentication-live-results.json") -Encoding UTF8
Clear-KnownDesktopVerifierEnvironmentLeaks
Write-Host "DESKTOP-AUTHENTICATION-LIVE-CLOSURE-1: PASS"
