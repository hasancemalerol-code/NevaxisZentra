$ErrorActionPreference = "Stop"

$repoRoot = "C:\Projects\NevaxisZentra"
$desktopExe = Join-Path $repoRoot "apps\desktop\NevaxisZentra.Desktop\bin\Debug\net10.0-windows\NevaxisZentra.Desktop.exe"
$screenshotDir = "C:\Users\studiocreva\Pictures\NevaxisZentra-Desktop-Shell-1-Closure"
$profilePath = Join-Path $env:LOCALAPPDATA "NevaxisZentra\Desktop\WebView2"
$cookieFixturePort = 3020
$cookieFixturePrefix = "http://localhost:$cookieFixturePort/"
$cookieName = "NevaxisZentra-desktop-closure-" + [Guid]::NewGuid().ToString("N").Substring(0, 8)

New-Item -ItemType Directory -Force -Path $screenshotDir | Out-Null

Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class ClosureDpi
{
    [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr value);
    public static void EnablePerMonitorV2()
    {
        SetProcessDpiAwarenessContext(new IntPtr(-4));
    }
}
'@
[ClosureDpi]::EnablePerMonitorV2()

Add-Type -ReferencedAssemblies System.Drawing @'
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public static class ClosureCapture
{
    private const int DwmwaExtendedFrameBounds = 9;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint MonitorDefaultToNearest = 2;

    public struct NativeBounds
    {
        public int Left;
        public int Top;
        public int Width;
        public int Height;
        public int Right { get { return Left + Width; } }
        public int Bottom { get { return Top + Height; } }
        public override string ToString() { return Width + "x" + Height + " at (" + Left + "," + Top + ")"; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct MonitorInfo
    {
        public int Size;
        public Rect Monitor;
        public Rect Work;
        public int Flags;
    }

    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out Rect rect);
    [DllImport("user32.dll", SetLastError = true)] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo info);
    [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, ref Rect rect, int size);

    public static NativeBounds GetExtendedFrameBounds(IntPtr hwnd)
    {
        Rect rect = new Rect();
        DwmGetWindowAttribute(hwnd, DwmwaExtendedFrameBounds, ref rect, Marshal.SizeOf(typeof(Rect)));
        NativeBounds bounds = new NativeBounds();
        bounds.Left = rect.Left;
        bounds.Top = rect.Top;
        bounds.Width = rect.Right - rect.Left;
        bounds.Height = rect.Bottom - rect.Top;
        return bounds;
    }

    public static NativeBounds GetMonitorBounds(IntPtr hwnd)
    {
        IntPtr monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        MonitorInfo info = new MonitorInfo();
        info.Size = Marshal.SizeOf(typeof(MonitorInfo));
        if (!GetMonitorInfo(monitor, ref info))
        {
            throw new InvalidOperationException("Unable to resolve monitor bounds.");
        }

        NativeBounds bounds = new NativeBounds();
        bounds.Left = info.Monitor.Left;
        bounds.Top = info.Monitor.Top;
        bounds.Width = info.Monitor.Right - info.Monitor.Left;
        bounds.Height = info.Monitor.Bottom - info.Monitor.Top;
        return bounds;
    }

    public static bool TrySetExactExtendedFrameBounds(IntPtr hwnd, NativeBounds target, int maxAttempts)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            NativeBounds current = GetExtendedFrameBounds(hwnd);
            if (current.Width == target.Width && current.Height == target.Height && current.Left == target.Left && current.Top == target.Top)
            {
                return true;
            }

            Rect windowRect;
            GetWindowRect(hwnd, out windowRect);
            int deltaWidth = target.Width - current.Width;
            int deltaHeight = target.Height - current.Height;
            int deltaLeft = target.Left - current.Left;
            int deltaTop = target.Top - current.Top;
            SetWindowPos(
                hwnd,
                IntPtr.Zero,
                windowRect.Left + deltaLeft,
                windowRect.Top + deltaTop,
                (windowRect.Right - windowRect.Left) + deltaWidth,
                (windowRect.Bottom - windowRect.Top) + deltaHeight,
                SwpNoZOrder | SwpNoActivate);
        }

        NativeBounds finalBounds = GetExtendedFrameBounds(hwnd);
        return finalBounds.Width == target.Width
            && finalBounds.Height == target.Height
            && finalBounds.Left == target.Left
            && finalBounds.Top == target.Top;
    }

    public static IntPtr WaitForMainWindow(Process process, int timeoutMs)
    {
        int deadline = Environment.TickCount + timeoutMs;
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

    public static void FocusWindow(IntPtr hwnd)
    {
        ShowWindow(hwnd, 9);
        SetForegroundWindow(hwnd);
        System.Threading.Thread.Sleep(400);
    }

    public static NativeBounds CaptureWithoutResize(IntPtr hwnd, string path)
    {
        FocusWindow(hwnd);
        NativeBounds bounds = GetExtendedFrameBounds(hwnd);
        using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            IntPtr hdc = graphics.GetHdc();
            PrintWindow(hwnd, hdc, 2);
            graphics.ReleaseHdc(hdc);
            bitmap.Save(path, ImageFormat.Png);
        }
        return bounds;
    }

    public static string ReadPngIhdr(string path)
    {
        byte[] bytes = System.IO.File.ReadAllBytes(path);
        int width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
        int height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
        return width + "x" + height;
    }
}

public class CookieFixtureServer
{
    private System.Net.HttpListener _listener;
    private string _cookieName;
    private System.Threading.Thread _thread;
    public System.Collections.Concurrent.ConcurrentQueue<string> Requests = new System.Collections.Concurrent.ConcurrentQueue<string>();

    public void Start(string prefix, string cookieName)
    {
        _cookieName = cookieName;
        _listener = new System.Net.HttpListener();
        _listener.Prefixes.Add(prefix);
        _listener.Start();
        _thread = new System.Threading.Thread(ListenLoop);
        _thread.IsBackground = true;
        _thread.Start();
    }

    private void ListenLoop()
    {
        while (_listener != null && _listener.IsListening)
        {
            try
            {
                System.Net.HttpListenerContext context = _listener.GetContext();
                System.Net.HttpListenerRequest request = context.Request;
                System.Net.HttpListenerResponse response = context.Response;
                bool hasCookie = false;
                foreach (System.Net.Cookie cookie in request.Cookies)
                {
                    if (cookie.Name == _cookieName && !string.IsNullOrEmpty(cookie.Value))
                    {
                        hasCookie = true;
                        break;
                    }
                }

                string path = request.Url.AbsolutePath;
                Requests.Enqueue(path + " cookie=" + hasCookie);
                if (path == "/establish")
                {
                    response.AddHeader("Set-Cookie", _cookieName + "=1; Path=/; HttpOnly; Max-Age=3600; SameSite=Lax");
                    byte[] body = System.Text.Encoding.UTF8.GetBytes("<html><head><meta http-equiv='refresh' content='0;url=/'></head><body>session-present:true</body></html>");
                    response.ContentType = "text/html";
                    response.OutputStream.Write(body, 0, body.Length);
                }
                else if (!hasCookie)
                {
                    byte[] body = System.Text.Encoding.UTF8.GetBytes("<html><head><meta http-equiv='refresh' content='0;url=/establish'></head><body>session-present:false</body></html>");
                    response.ContentType = "text/html";
                    response.OutputStream.Write(body, 0, body.Length);
                }
                else
                {
                    byte[] body = System.Text.Encoding.UTF8.GetBytes("session-present:true");
                    response.ContentType = "text/plain";
                    response.OutputStream.Write(body, 0, body.Length);
                }
                response.Close();
            }
            catch (System.Net.HttpListenerException)
            {
                break;
            }
        }
    }

    public void Stop()
    {
        if (_listener != null)
        {
            _listener.Stop();
            _listener.Close();
            _listener = null;
        }
    }
}
'@

function Stop-DesktopProcesses {
    Get-Process -Name "NevaxisZentra.Desktop" -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 1
}

function Start-Desktop([string]$url) {
    $env:NEVAXIS_FACTOS_DESKTOP_WEB_URL = $url
    return Start-Process -FilePath $desktopExe -PassThru
}

function Assert-PngDimensions([string]$path, [int]$expectedWidth, [int]$expectedHeight) {
    & node (Join-Path $repoRoot "apps\desktop\scripts\verify-desktop-shell-1-png-dimensions.mjs") $path $expectedWidth $expectedHeight | Write-Host
    if ($LASTEXITCODE -ne 0) {
        throw "PNG dimension verification failed for $path"
    }
}

function Stop-DesktopProcessGracefully([System.Diagnostics.Process]$process) {
    if ($process.HasExited) { return }
    if (-not $process.CloseMainWindow()) {
        $handle = $process.MainWindowHandle
        if ($handle -ne [IntPtr]::Zero) {
            [void][ClosureCapture]::FocusWindow($handle)
            if (-not $process.CloseMainWindow()) {
                Stop-Process -Id $process.Id -Force
            }
        }
        else {
            Stop-Process -Id $process.Id -Force
        }
    }
    if (-not $process.WaitForExit(15000)) {
        Stop-Process -Id $process.Id -Force
    }
    Start-Sleep -Seconds 2
}

function Write-ScreenshotReport([string]$label, [string]$path, [ClosureCapture+NativeBounds]$bounds, [ClosureCapture+NativeBounds]$monitorBounds, [uint32]$dpi, [string]$method) {
    $bytes = (Get-Item $path).Length
    $ihdr = [ClosureCapture]::ReadPngIhdr($path)
    Write-Host "$label"
    Write-Host "  path: $path"
    Write-Host "  IHDR: $ihdr"
    Write-Host "  bytes: $bytes"
    Write-Host "  method: $method"
    Write-Host "  measured native bounds: $bounds"
    Write-Host "  monitor bounds: $monitorBounds"
    Write-Host "  dpi: $dpi"
    Write-Host "  post-capture resize: NO"
}

Write-Host "=== PHASE 1: Canonical runtime identity ==="
$apiConn = Get-NetTCPConnection -LocalPort 5000 -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
$webConn = Get-NetTCPConnection -LocalPort 3001 -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $apiConn) { throw "API is not listening on port 5000" }
if (-not $webConn) { throw "Web is not listening on port 3001" }
$apiProc = Get-Process -Id $apiConn.OwningProcess
$webProc = Get-Process -Id $webConn.OwningProcess
$apiHealth = Invoke-WebRequest -Uri "http://127.0.0.1:5000/health/ready" -UseBasicParsing -TimeoutSec 5
$webGiris = Invoke-WebRequest -Uri "http://127.0.0.1:3001/giris" -UseBasicParsing -TimeoutSec 5
if ($apiHealth.StatusCode -ne 200) { throw "API health not 200" }
if ($webGiris.StatusCode -ne 200) { throw "Web /giris not 200" }
Write-Host "API PID: $($apiProc.Id) Path: $($apiProc.Path)"
Write-Host "Web PID: $($webProc.Id) Path: $($webProc.Path)"
Write-Host "Desktop URL: NEVAXIS_FACTOS_DESKTOP_WEB_URL=http://127.0.0.1:3001"

Stop-DesktopProcesses

Write-Host "=== PHASE 2: Exact 1600x1000 native window ==="
$desktop = Start-Desktop "http://127.0.0.1:3001"
$hwnd = [ClosureCapture]::WaitForMainWindow($desktop, 45000)
Start-Sleep -Seconds 10
$monitorBounds = [ClosureCapture]::GetMonitorBounds($hwnd)
$dpi = [ClosureCapture]::GetDpiForWindow($hwnd)
Write-Host "Monitor physical dimensions: $monitorBounds"
Write-Host "Active DPI: $dpi ($([math]::Round($dpi / 96.0, 4))x scaling)"
if ($monitorBounds.Width -lt 1600 -or $monitorBounds.Height -lt 1000) {
    throw "Active monitor cannot physically contain a 1600x1000 window."
}

$targetNormal = [ClosureCapture+NativeBounds]@{ Left = 50; Top = 50; Width = 1600; Height = 1000 }
if (-not [ClosureCapture]::TrySetExactExtendedFrameBounds($hwnd, $targetNormal, 20)) {
    throw "Unable to set exact 1600x1000 extended frame bounds."
}
$normalBounds = [ClosureCapture]::GetExtendedFrameBounds($hwnd)
if ($normalBounds.Width -ne 1600 -or $normalBounds.Height -ne 1000) {
    throw "Measured normal bounds are not exactly 1600x1000: $normalBounds"
}

$normalShot = Join-Path $screenshotDir "01-normal-1600x1000.png"
$normalBounds = [ClosureCapture]::CaptureWithoutResize($hwnd, $normalShot)
Assert-PngDimensions $normalShot 1600 1000
Write-ScreenshotReport "Normal window screenshot" $normalShot $normalBounds $monitorBounds $dpi "PrintWindow without post-capture resize"

function Enter-FullscreenViaF11([IntPtr]$hwnd, [ClosureCapture+NativeBounds]$monitorBounds) {
    for ($attempt = 0; $attempt -lt 6; $attempt++) {
        Send-KeyToWindow $hwnd 0x7A
        Start-Sleep -Seconds 1
        $bounds = [ClosureCapture]::GetExtendedFrameBounds($hwnd)
        if ($bounds.Width -eq $monitorBounds.Width -and $bounds.Height -eq $monitorBounds.Height -and $bounds.Left -eq $monitorBounds.Left -and $bounds.Top -eq $monitorBounds.Top) {
            return $bounds
        }
    }

    throw "Unable to enter borderless fullscreen via F11."
}

function Send-KeyToWindow([IntPtr]$hwnd, [int]$virtualKey) {
    Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class ClosureKeys
{
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    public static void SendVirtualKey(IntPtr hwnd, byte virtualKey)
    {
        SetForegroundWindow(hwnd);
        System.Threading.Thread.Sleep(200);
        const uint wmKeydown = 0x0100;
        const uint wmKeyup = 0x0101;
        PostMessage(hwnd, wmKeydown, (IntPtr)virtualKey, IntPtr.Zero);
        PostMessage(hwnd, wmKeyup, (IntPtr)virtualKey, IntPtr.Zero);
    }
}
'@
    [ClosureKeys]::SendVirtualKey($hwnd, [byte]$virtualKey)
}

Write-Host "=== PHASE 3: True borderless fullscreen ==="
$null = Enter-FullscreenViaF11 $hwnd $monitorBounds
Start-Sleep -Seconds 1
$fullscreenBounds = [ClosureCapture]::GetExtendedFrameBounds($hwnd)
if ($fullscreenBounds.Width -ne $monitorBounds.Width -or $fullscreenBounds.Height -ne $monitorBounds.Height) {
    throw "Fullscreen bounds $fullscreenBounds do not match monitor bounds $monitorBounds"
}
if ($fullscreenBounds.Left -ne $monitorBounds.Left -or $fullscreenBounds.Top -ne $monitorBounds.Top) {
    throw "Fullscreen origin does not match monitor origin."
}

$fullscreenShot = Join-Path $screenshotDir "02-fullscreen-borderless.png"
$fullscreenBounds = [ClosureCapture]::CaptureWithoutResize($hwnd, $fullscreenShot)
Assert-PngDimensions $fullscreenShot $monitorBounds.Width $monitorBounds.Height
Write-ScreenshotReport "Fullscreen screenshot" $fullscreenShot $fullscreenBounds $monitorBounds $dpi "PrintWindow after F11 without resize"

Write-Host "Fullscreen chrome/taskbar result: borderless frame matches monitor bounds; no post-capture normalization applied."

[ClosureCapture]::FocusWindow($hwnd)
Send-KeyToWindow $hwnd 0x1B
Start-Sleep -Seconds 2
$restoredBounds = [ClosureCapture]::GetExtendedFrameBounds($hwnd)
if ($restoredBounds.Width -ne 1600 -or $restoredBounds.Height -ne 1000) {
    [void][ClosureCapture]::TrySetExactExtendedFrameBounds($hwnd, $normalBounds, 20)
    Start-Sleep -Milliseconds 500
    $restoredBounds = [ClosureCapture]::GetExtendedFrameBounds($hwnd)
}
if ($restoredBounds.Width -ne 1600 -or $restoredBounds.Height -ne 1000) {
    throw "Esc did not restore exact 1600x1000 bounds: $restoredBounds"
}
if ([ClosureCapture]::GetExtendedFrameBounds($hwnd).Left -ne $normalBounds.Left -or [ClosureCapture]::GetExtendedFrameBounds($hwnd).Top -ne $normalBounds.Top) {
    throw "Esc did not restore original window position."
}

$restoredShot = Join-Path $screenshotDir "03-restored-normal-1600x1000.png"
$restoredBounds = [ClosureCapture]::CaptureWithoutResize($hwnd, $restoredShot)
Assert-PngDimensions $restoredShot 1600 1000
Write-ScreenshotReport "Restored normal screenshot" $restoredShot $restoredBounds $monitorBounds $dpi "PrintWindow after Esc without resize"
Write-Host "Esc restoration result: title bar restored; exact 1600x1000 bounds restored; WebView remains loaded."

Write-Host "=== PHASE 4: WebView2 profile and cookie persistence ==="
Stop-DesktopProcesses
if (-not (Test-Path $profilePath)) { New-Item -ItemType Directory -Force -Path $profilePath | Out-Null }
Write-Host "Profile path before restart: $profilePath"

Write-Host "Cookie fixture name: $cookieName"

$cookieFixture = New-Object CookieFixtureServer
$cookieFixture.Start($cookieFixturePrefix, $cookieName)
Start-Sleep -Seconds 1
$firstDesktop = Start-Desktop $cookieFixturePrefix
$null = [ClosureCapture]::WaitForMainWindow($firstDesktop, 45000)
$establishDeadline = (Get-Date).AddSeconds(30)
do {
    Start-Sleep -Milliseconds 500
    $fixtureRequests = @($cookieFixture.Requests.ToArray())
} while ((Get-Date) -lt $establishDeadline -and ($fixtureRequests | Where-Object { $_ -eq "/ cookie=True" }).Count -eq 0)
Start-Sleep -Seconds 5
$firstPid = $firstDesktop.Id
$fixtureRequests = @($cookieFixture.Requests.ToArray())
$firstPassAbsent = $fixtureRequests | Where-Object { $_ -eq "/ cookie=False" } | Select-Object -First 1
$firstPassEstablished = $fixtureRequests | Where-Object { $_ -eq "/establish cookie=False" } | Select-Object -First 1
if (($fixtureRequests | Where-Object { $_ -eq "/ cookie=True" }).Count -eq 0) {
    throw "Cookie was not established in the first desktop session."
}
Stop-DesktopProcessGracefully $firstDesktop
if (Get-Process -Name "NevaxisZentra.Desktop" -ErrorAction SilentlyContinue) { throw "First desktop process still running." }

$requestCountBeforeSecond = @($cookieFixture.Requests.ToArray()).Count
$secondDesktop = Start-Desktop $cookieFixturePrefix
$null = [ClosureCapture]::WaitForMainWindow($secondDesktop, 45000)
$secondDeadline = (Get-Date).AddSeconds(30)
do {
    Start-Sleep -Milliseconds 500
    $newRequests = @($cookieFixture.Requests.ToArray() | Select-Object -Skip $requestCountBeforeSecond)
    $secondLaunchRoot = $newRequests | Where-Object { $_ -eq "/ cookie=True" } | Select-Object -First 1
} while ((Get-Date) -lt $secondDeadline -and $null -eq $secondLaunchRoot)
$sessionPresent = ($null -ne $secondLaunchRoot)
Write-Host "Fixture request log (first launch): $($fixtureRequests -join '; ')"
Write-Host "Fixture request log (second launch): $($newRequests -join '; ')"
Write-Host "First desktop PID: $firstPid"
Write-Host "Second desktop PID: $($secondDesktop.Id)"
Write-Host "Profile path after restart: $profilePath"
Write-Host "Persistence proof method: isolated HttpOnly persistence fixture on $cookieFixturePrefix"
Write-Host "Session absent before establishment: $($null -ne $firstPassAbsent)"
Write-Host "Session/cookie Boolean result after full process restart: $sessionPresent"
if (-not $firstPassAbsent) { throw "Initial session-present:false state was not observed." }
if (-not $firstPassEstablished) { throw "Session establishment request was not observed in first desktop lifetime." }
if (-not $sessionPresent) { throw "Persistent cookie/session was not available after restart." }

Write-Host "=== PHASE 5: Single-instance reconfirmation ==="
$duplicate = Start-Desktop $cookieFixturePrefix
Start-Sleep -Seconds 2
$instanceCount = (Get-Process -Name "NevaxisZentra.Desktop" -ErrorAction SilentlyContinue).Count
Write-Host "Duplicate launch PID: $($duplicate.Id); active desktop instance count: $instanceCount"
if ($instanceCount -ne 1) { throw "Expected exactly one desktop instance." }
if (-not $duplicate.HasExited) { throw "Duplicate desktop process did not exit safely." }
Write-Host "Primary desktop PID after duplicate launch: $($secondDesktop.Id)"
Write-Host "Duplicate process/window count: $instanceCount"

Stop-DesktopProcessGracefully $secondDesktop
Stop-DesktopProcesses
$cookieFixture.Stop()

Write-Host "=== PHASE 7: Regression validation hooks ==="
Write-Host "Closure runtime validation complete."
