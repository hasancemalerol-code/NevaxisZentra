$ErrorActionPreference = "Stop"

$desktopExe = "C:\Projects\NevaxisZentra\apps\desktop\NevaxisZentra.Desktop\bin\Debug\net10.0-windows\NevaxisZentra.Desktop.exe"
$screenshotDir = "C:\Users\studiocreva\Pictures\NevaxisZentra-Desktop-Shell-1"

New-Item -ItemType Directory -Force -Path $screenshotDir | Out-Null

Add-Type -ReferencedAssemblies System.Drawing @"
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public static class DesktopCapture
{
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);
    [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    public static IntPtr WaitForMainWindow(Process process, int timeoutMs)
    {
        var deadline = Environment.TickCount + timeoutMs;
        while (Environment.TickCount < deadline)
        {
            process.Refresh();
            if (process.MainWindowHandle != IntPtr.Zero)
            {
                return process.MainWindowHandle;
            }

            System.Threading.Thread.Sleep(250);
        }

        throw new TimeoutException("Desktop main window handle not available.");
    }

    public static void PrepareWindow(IntPtr hWnd, int width, int height)
    {
        ShowWindow(hWnd, 9);
        MoveWindow(hWnd, 50, 50, width, height, true);
        SetForegroundWindow(hWnd);
        System.Threading.Thread.Sleep(800);
    }

    public static void CaptureWindow(IntPtr hWnd, string path)
    {
        PrepareWindow(hWnd, 1600, 1000);
        RECT rect;
        GetWindowRect(hWnd, out rect);
        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;
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

function Stop-DesktopProcesses {
    Get-Process -Name "NevaxisZentra.Desktop" -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 1
}

function Start-DesktopApp([string]$url) {
    $env:NEVAXIS_FACTOS_DESKTOP_WEB_URL = $url
    return Start-Process -FilePath $desktopExe -PassThru
}

function Get-PngDimensions([string]$path) {
    Add-Type -AssemblyName System.Drawing
    $img = [System.Drawing.Image]::FromFile($path)
    try { return "$($img.Width)x$($img.Height)" } finally { $img.Dispose() }
}

Stop-DesktopProcesses

Write-Host "Starting reachable runtime proof..."
$process = Start-DesktopApp "http://127.0.0.1:3001"
$handle = [DesktopCapture]::WaitForMainWindow($process, 45000)
Start-Sleep -Seconds 10

$shot1 = Join-Path $screenshotDir "01-desktop-shell-1600x1000.png"
[DesktopCapture]::CaptureWindow($handle, $shot1)
Write-Host "Screenshot 1: $shot1 ($(Get-PngDimensions $shot1))"

Add-Type -AssemblyName System.Windows.Forms
[DesktopCapture]::SetForegroundWindow($handle) | Out-Null
Start-Sleep -Milliseconds 400
[System.Windows.Forms.SendKeys]::SendWait("{F11}")
Start-Sleep -Seconds 2
$shot2 = Join-Path $screenshotDir "02-desktop-shell-fullscreen.png"
[DesktopCapture]::CaptureWindow($handle, $shot2)
Write-Host "Screenshot 2: $shot2 ($(Get-PngDimensions $shot2))"
[System.Windows.Forms.SendKeys]::SendWait("{ESC}")
Start-Sleep -Seconds 1

$second = Start-DesktopApp "http://127.0.0.1:3001"
Start-Sleep -Seconds 2
$instanceCount = (Get-Process -Name "NevaxisZentra.Desktop" -ErrorAction SilentlyContinue).Count
Write-Host "Desktop PID: $($process.Id); duplicate instance launch PID: $($second.Id); active instance count: $instanceCount"
if ($second.HasExited -eq $false) { Stop-Process -Id $second.Id -Force -ErrorAction SilentlyContinue }

Stop-DesktopProcesses

Write-Host "Starting failure-state proof..."
$failureProcess = Start-DesktopApp "http://127.0.0.1:59999"
$failureHandle = [DesktopCapture]::WaitForMainWindow($failureProcess, 30000)
Start-Sleep -Seconds 8
$shot3 = Join-Path $screenshotDir "03-desktop-shell-connection-failure.png"
[DesktopCapture]::CaptureWindow($failureHandle, $shot3)
Write-Host "Screenshot 3: $shot3 ($(Get-PngDimensions $shot3))"

$listener = [System.Net.HttpListener]::new()
$listener.Prefixes.Add("http://127.0.0.1:59999/")
$listener.Start()
$listenerTask = [System.Threading.Tasks.Task]::Run([Action]{
    while ($listener.IsListening) {
        $context = $listener.GetContext()
        $bytes = [System.Text.Encoding]::UTF8.GetBytes("<html><body>NevaxisZentra retry proof</body></html>")
        $context.Response.ContentType = "text/html"
        $context.Response.OutputStream.Write($bytes, 0, $bytes.Length)
        $context.Response.Close()
    }
})

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$root = [System.Windows.Automation.AutomationElement]::FromHandle($failureHandle)
$retryCondition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, "Yeniden dene")
$retryButton = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $retryCondition)
if ($retryButton -eq $null) { throw "Retry button not found" }
$invokePattern = $retryButton.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
$invokePattern.Invoke()
Start-Sleep -Seconds 4
$shot4 = Join-Path $screenshotDir "04-desktop-shell-retry-success.png"
[DesktopCapture]::CaptureWindow($failureHandle, $shot4)
Write-Host "Screenshot 4 (retry): $shot4 ($(Get-PngDimensions $shot4))"

$listener.Stop()
$listenerTask.Wait(1000) | Out-Null
Stop-Process -Id $failureProcess.Id -Force

Write-Host "Runtime validation complete."
