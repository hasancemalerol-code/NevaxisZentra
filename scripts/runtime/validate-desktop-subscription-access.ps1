$ErrorActionPreference = 'Stop'
$desktopExe = 'C:\Projects\CrevaFactOS\apps\desktop\CrevaFactOS.Desktop\bin\Debug\net10.0-windows\CrevaFactOS.Desktop.exe'
$webUrl = 'http://127.0.0.1:3001/dashboard'

function Stop-DesktopProcesses {
    Get-Process -Name 'CrevaFactOS.Desktop' -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 1
}

function Wait-MainWindow([System.Diagnostics.Process]$process, [int]$timeoutMs) {
    $deadline = [Environment]::TickCount + $timeoutMs
    while ([Environment]::TickCount -lt $deadline) {
        $process.Refresh()
        if ($process.MainWindowHandle -ne [IntPtr]::Zero) {
            return $process.MainWindowHandle
        }
        Start-Sleep -Milliseconds 250
    }
    throw 'Desktop main window handle not available.'
}

function Get-WindowTitle([IntPtr]$handle) {
    if (-not ('NativeWindowTitle' -as [type])) {
        Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;
public static class NativeWindowTitle {
  [DllImport("user32.dll", CharSet=CharSet.Unicode)]
  public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
  public static string Read(IntPtr handle) {
    var builder = new StringBuilder(512);
    GetWindowText(handle, builder, builder.Capacity);
    return builder.ToString();
  }
}
"@
    }
    return [NativeWindowTitle]::Read($handle)
}

Write-Host 'Stopping existing desktop instances...'
Stop-DesktopProcesses

Write-Host 'Launching desktop against canonical web runtime...'
$env:CREVAFACTOS_DESKTOP_WEB_URL = $webUrl
$first = Start-Process -FilePath $desktopExe -PassThru
$firstHandle = Wait-MainWindow $first 45000
Start-Sleep -Seconds 12
$firstTitle = Get-WindowTitle $firstHandle
Write-Host "FIRST_TITLE=$firstTitle"

Stop-DesktopProcesses
Start-Sleep -Seconds 2

Write-Host 'Relaunching desktop to verify WebView2 session persistence...'
$second = Start-Process -FilePath $desktopExe -PassThru
$secondHandle = Wait-MainWindow $second 45000
Start-Sleep -Seconds 12
$secondTitle = Get-WindowTitle $secondHandle
Write-Host "SECOND_TITLE=$secondTitle"

if ($firstTitle -match 'abonelik-erisimi' -or $secondTitle -match 'abonelik-erisimi') {
    throw 'Desktop navigated to subscription recovery route.'
}

Stop-DesktopProcesses
Write-Host 'DESKTOP_SUBSCRIPTION_CHECK=PASS'
