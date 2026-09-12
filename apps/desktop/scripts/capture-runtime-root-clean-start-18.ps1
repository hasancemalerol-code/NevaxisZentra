# Captures 18-runtime-root-clean-cold-start-dashboard.png from real desktop shortcut.
$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
$screenshotDir = Join-Path $repoRoot ".cursor-screenshots\desktop-sales-demo-1"
$shortcutPath = Join-Path ([Environment]::GetFolderPath("Desktop")) "NevaxisFactOS.lnk"
. (Join-Path $PSScriptRoot "desktop-verifier-isolation.ps1")

Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing @"
using System; using System.Diagnostics; using System.Drawing; using System.Drawing.Imaging; using System.Runtime.InteropServices;
public static class Capture18 {
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
  public struct RECT { public int Left, Top, Right, Bottom; }
  public static IntPtr? FindMain(int timeoutMs) {
    var deadline = Environment.TickCount + timeoutMs;
    while (Environment.TickCount < deadline) {
      foreach (var name in new[] { "crevafactos", "NevaxisFactOS.Desktop" }) {
        foreach (var process in Process.GetProcessesByName(name)) {
          process.Refresh();
          if (process.MainWindowHandle == IntPtr.Zero) continue;
          RECT rect; GetWindowRect(process.MainWindowHandle, out rect);
          int w = Math.Max(0, rect.Right - rect.Left), h = Math.Max(0, rect.Bottom - rect.Top);
          if (IsWindowVisible(process.MainWindowHandle) && w >= 1000 && h >= 700) return process.MainWindowHandle;
        }
      }
      System.Threading.Thread.Sleep(500);
    }
    return null;
  }
  public static void Save(IntPtr hWnd, string path) {
    ShowWindow(hWnd, 9); SetForegroundWindow(hWnd); System.Threading.Thread.Sleep(1000);
    RECT rect; GetWindowRect(hWnd, out rect);
    using (var bmp = new Bitmap(Math.Max(800, rect.Right - rect.Left), Math.Max(600, rect.Bottom - rect.Top)))
    using (var g = Graphics.FromImage(bmp)) {
      IntPtr hdc = g.GetHdc(); PrintWindow(hWnd, hdc, 2); g.ReleaseHdc(hdc); bmp.Save(path, ImageFormat.Png);
    }
  }
}
"@

Clear-KnownDesktopVerifierEnvironmentLeaks
Get-Process -Name "crevafactos","NevaxisFactOS.Desktop" -ErrorAction SilentlyContinue | Stop-Process -Force
foreach ($listener in @(Get-NetTCPConnection -LocalPort 3001 -State Listen -ErrorAction SilentlyContinue)) {
    Stop-Process -Id $listener.OwningProcess -Force -ErrorAction SilentlyContinue
}
Start-Sleep -Seconds 3
New-Item -ItemType Directory -Force -Path $screenshotDir | Out-Null
Start-Process -FilePath $shortcutPath | Out-Null
$deadline = (Get-Date).AddMinutes(5)
$handle = $null
while ((Get-Date) -lt $deadline) {
    try {
        $response = Invoke-WebRequest "http://127.0.0.1:3001/giris" -UseBasicParsing -TimeoutSec 5
        if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300) {
            $found = [Capture18]::FindMain(5000)
            if ($found.HasValue) { $handle = $found; break }
        }
    } catch { }
    Start-Sleep -Seconds 2
}
if (-not $handle.HasValue) { throw "Main window not found for screenshot 18." }
$shot = Join-Path $screenshotDir "18-runtime-root-clean-cold-start-dashboard.png"
[Capture18]::Save($handle.Value, $shot)
if (-not (Test-Path $shot)) { throw "Screenshot 18 was not created." }
Write-Host "Captured $shot"
