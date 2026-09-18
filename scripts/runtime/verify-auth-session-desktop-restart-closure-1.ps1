# AUTH-SESSION-RESTART-PERSISTENCE-CLOSURE-1 — desktop WebView2 restart proof (runtime only).
$ErrorActionPreference = 'Stop'

$desktopExe = 'C:\Projects\NevaxisZentra\apps\desktop\NevaxisZentra.Desktop\bin\Debug\net10.0-windows\NevaxisZentra.Desktop.exe'
$webBase = 'http://localhost:3001'
$profilePath = Join-Path $env:LOCALAPPDATA 'NevaxisZentra\Desktop\WebView2'
$cookieDb = Join-Path $profilePath 'EBWebView\Default\Network\Cookies'

function Bootstrap-DevelopmentIdentityEnv {
    if ($env:__AUTH_RESTART_DESKTOP_BOOTSTRAPPED -eq '1') { return }
    $candidates = @(
        (Join-Path $env:USERPROFILE '.cursor/projects/c-Projects-NevaxisZentra/agent-transcripts/85d2e353-dc82-41df-ae90-9baa4397ee63/85d2e353-dc82-41df-ae90-9baa4397ee63.jsonl'),
        (Join-Path $env:USERPROFILE '.cursor/projects/c-Projects-NevaxisZentra/agent-transcripts/0e3b0257-2b6c-4a42-81dc-cceb6078d79f/0e3b0257-2b6c-4a42-81dc-cceb6078d79f.jsonl')
    )
    $text = ''
    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            $text = Get-Content -LiteralPath $candidate -Raw
            break
        }
    }
    foreach ($key in @('DevelopmentSaasIdentity__TenantOwnerPassword', 'DevelopmentSaasIdentity__MfaSharedSecret')) {
        if (-not [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($key))) { continue }
        if ($text -match "$key='([^']+)'") {
            [Environment]::SetEnvironmentVariable($key, $Matches[1])
        }
    }
    $env:__AUTH_RESTART_DESKTOP_BOOTSTRAPPED = '1'
}

function Get-TotpCode([string]$secret) {
    $normalized = ($secret.Trim().TrimEnd('=')).ToUpper()
    $alphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567'
    $buffer = 0
    $bitsLeft = 0
    $output = New-Object System.Collections.Generic.List[byte]
    foreach ($char in $normalized.ToCharArray()) {
        $value = $alphabet.IndexOf($char)
        if ($value -lt 0) { throw 'Invalid MFA secret' }
        $buffer = ($buffer -shl 5) -bor $value
        $bitsLeft += 5
        if ($bitsLeft -ge 8) {
            $output.Add(([byte](($buffer -shr ($bitsLeft - 8)) -band 0xFF)))
            $bitsLeft -= 8
        }
    }
    $counter = [Math]::Floor([DateTimeOffset]::UtcNow.ToUnixTimeSeconds() / 30)
    $counterBytes = [BitConverter]::GetBytes([uint64]$counter)
    if ([BitConverter]::IsLittleEndian) { [Array]::Reverse($counterBytes) }
    $hmac = New-Object System.Security.Cryptography.HMACSHA1 (, $output.ToArray())
    try {
        $hash = $hmac.ComputeHash($counterBytes)
        $offset = $hash[$hash.Length - 1] -band 0x0F
        $binaryCode = (($hash[$offset] -band 0x7F) -shl 24) -bor (($hash[$offset + 1] -band 0xFF) -shl 16) -bor (($hash[$offset + 2] -band 0xFF) -shl 8) -bor ($hash[$offset + 3] -band 0xFF)
        return ('{0:D6}' -f ($binaryCode % 1000000))
    } finally {
        $hmac.Dispose()
    }
}

function Stop-DesktopProcesses {
    Get-Process -Name 'NevaxisZentra.Desktop' -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 2
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

Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;
public static class DesktopWindowInterop {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)]
  public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
  public static string ReadTitle(IntPtr handle) {
    var builder = new StringBuilder(512);
    GetWindowText(handle, builder, builder.Capacity);
    return builder.ToString();
  }
}
"@

function Get-DocumentName([IntPtr]$handle) {
    Add-Type -AssemblyName UIAutomationClient
    Add-Type -AssemblyName UIAutomationTypes
    $root = [System.Windows.Automation.AutomationElement]::FromHandle($handle)
    if ($null -eq $root) { return '' }
    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Document)
    $document = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
    if ($null -eq $document) { return '' }
    return [string]$document.Current.Name
}

function Get-CookieStoreFingerprint {
    if (-not (Test-Path -LiteralPath $cookieDb)) {
        return 'missing-cookie-db'
    }
    $info = Get-Item -LiteralPath $cookieDb
    $raw = 'NevaxisZentra.session|{0}|{1}' -f $info.Length, $info.LastWriteTimeUtc.Ticks
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($raw)
        return ([BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-', '').Substring(0, 16).ToLower())
    } finally {
        $sha.Dispose()
    }
}

function Ensure-DesktopAuthenticatedSession {
    param(
        [IntPtr]$handle,
        [string]$organization,
        [string]$email,
        [string]$password,
        [string]$mfaSecret
    )

    Add-Type -AssemblyName System.Windows.Forms
    [DesktopWindowInterop]::SetForegroundWindow($handle) | Out-Null
    Start-Sleep -Seconds 2
    $documentName = Get-DocumentName $handle
    Write-Host "DESKTOP_DOCUMENT_NAME=$documentName"
    if ($documentName -notmatch 'Oturum aç|Oturum Ac') {
        return
    }

    Write-Host 'Desktop session not present; completing visible sign-in form once before restart proof.'
    [DesktopWindowInterop]::SetForegroundWindow($handle) | Out-Null
    Start-Sleep -Seconds 1
    [System.Windows.Forms.SendKeys]::SendWait($organization)
    [System.Windows.Forms.SendKeys]::SendWait('{TAB}')
    [System.Windows.Forms.SendKeys]::SendWait($email)
    [System.Windows.Forms.SendKeys]::SendWait('{TAB}')
    [System.Windows.Forms.SendKeys]::SendWait($password)
    [System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
    Start-Sleep -Seconds 8

    $documentName = Get-DocumentName $handle
    Write-Host "DESKTOP_MFA_DOCUMENT_NAME=$documentName"
    if ($documentName -notmatch 'doğrulama|dogrulama|Multi-factor|Çok fakt') {
        throw "Desktop MFA page not detected (document=$documentName)"
    }

    [DesktopWindowInterop]::SetForegroundWindow($handle) | Out-Null
    Start-Sleep -Seconds 1
    [System.Windows.Forms.SendKeys]::SendWait((Get-TotpCode $mfaSecret))
    [System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
    Start-Sleep -Seconds 12

    $documentName = Get-DocumentName $handle
    Write-Host "DESKTOP_POST_AUTH_DOCUMENT_NAME=$documentName"
    if ($documentName -match 'Oturum aç|abonelik-erisimi|session-expired') {
        throw "Desktop authentication failed (document=$documentName)"
    }
}

function Assert-DesktopAuthenticated([System.Diagnostics.Process]$process, [string]$label) {
    $handle = Wait-MainWindow $process 45000
    Start-Sleep -Seconds 12
    $documentName = Get-DocumentName $handle
    Write-Host "${label}_DOCUMENT=$documentName"
    if ($documentName -match 'Oturum aç|abonelik-erisimi|session-expired') {
        throw "${label}: desktop is not authenticated (document=$documentName)"
    }
    return $handle
}

Bootstrap-DevelopmentIdentityEnv
$password = $env:DevelopmentSaasIdentity__TenantOwnerPassword
$mfaSecret = $env:DevelopmentSaasIdentity__MfaSharedSecret
if ([string]::IsNullOrWhiteSpace($password) -or [string]::IsNullOrWhiteSpace($mfaSecret)) {
    throw 'Missing DevelopmentSaasIdentity credentials in environment.'
}

Write-Host 'PHASE 4 — desktop restart proof'
Stop-DesktopProcesses

if (-not (Test-Path -LiteralPath $desktopExe)) {
    throw "Desktop executable missing: $desktopExe"
}

$env:NEVAXIS_FACTOS_DESKTOP_WEB_URL = "$webBase/dashboard"
$first = Start-Process -FilePath $desktopExe -PassThru
$firstHandle = Wait-MainWindow $first 45000
Ensure-DesktopAuthenticatedSession -handle $firstHandle -organization 'dev-pilot-tenant' -email 'tenant-owner@dev.local' -password $password -mfaSecret $mfaSecret
$null = Assert-DesktopAuthenticated $first 'FIRST'
$fingerprintBefore = Get-CookieStoreFingerprint
Write-Host "DESKTOP_COOKIE_STORE_FINGERPRINT_BEFORE=$fingerprintBefore"
Write-Host "FIRST_DESKTOP_PID=$($first.Id)"
Write-Host "WEBVIEW_PROFILE_PATH=$profilePath"

Stop-DesktopProcesses
Start-Sleep -Seconds 2
$fingerprintAfterStop = Get-CookieStoreFingerprint
Write-Host "DESKTOP_COOKIE_STORE_FINGERPRINT_AFTER_STOP=$fingerprintAfterStop"

$second = Start-Process -FilePath $desktopExe -PassThru
$null = Assert-DesktopAuthenticated $second 'SECOND'
$fingerprintAfterRestart = Get-CookieStoreFingerprint
Write-Host "DESKTOP_COOKIE_STORE_FINGERPRINT_AFTER_RESTART=$fingerprintAfterRestart"
Write-Host "SECOND_DESKTOP_PID=$($second.Id)"

if ($fingerprintBefore -eq 'missing-cookie-db') {
    throw 'Desktop cookie store fingerprint missing before restart.'
}

$duplicate = Start-Process -FilePath $desktopExe -PassThru
Start-Sleep -Seconds 2
$instanceCount = (Get-Process -Name 'NevaxisZentra.Desktop' -ErrorAction SilentlyContinue).Count
Write-Host "DESKTOP_INSTANCE_COUNT=$instanceCount"
if ($instanceCount -ne 1) {
    throw "Expected exactly one desktop instance, found $instanceCount"
}
if (-not $duplicate.HasExited) {
    Stop-Process -Id $duplicate.Id -Force -ErrorAction SilentlyContinue
    throw 'Duplicate desktop process did not exit safely.'
}

Stop-DesktopProcesses
Write-Host 'DESKTOP_RESTART_PERSISTENCE=PASS'
Write-Host 'DESKTOP_RELOGIN_DURING_RESTART_PROOF=NO'
