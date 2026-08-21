$ErrorActionPreference = "Stop"
$repoRoot = "C:\Projects\CrevaFactOS"
$desktopExe = Join-Path $repoRoot "apps\desktop\CrevaFactOS.Desktop\bin\Debug\net10.0-windows\CrevaFactOS.Desktop.exe"
$shotDir = "C:\Users\studiocreva\Pictures\CrevaFactOS-Desktop-Shell-1-Fullscreen-Visual-Closure"
$rejected = "C:\Users\studiocreva\Pictures\CrevaFactOS-Desktop-Shell-1-Closure\02-fullscreen-borderless.png"
New-Item -ItemType Directory -Force -Path $shotDir | Out-Null

Add-Type @'
using System; using System.Runtime.InteropServices;
public static class FcDpi { [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr v); public static void Go(){ SetProcessDpiAwarenessContext(new IntPtr(-4)); } }
'@
[FcDpi]::Go()

Add-Type -ReferencedAssemblies System.Drawing @'
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public static class FcCapture
{
    public struct NB
    {
        public int L, T, W, H;
        public override string ToString() { return W + "x" + H + " at (" + L + "," + T + ")"; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect { public int L, T, R, B; }

    [StructLayout(LayoutKind.Sequential)]
    public struct Pt { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct MI { public int Size; public Rect Monitor; public Rect Work; public int Flags; }

    private const int DwmExt = 9;
    private const uint SwpNoZ = 4;
    private const uint SwpNoA = 16;
    private const uint MonNear = 2;

    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int c);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr dc, int f);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out Rect r);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out Rect r);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref Pt p);
    [DllImport("user32.dll", SetLastError = true)] public static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f);
    [DllImport("user32.dll")] public static extern IntPtr MonitorFromWindow(IntPtr h, uint f);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern bool GetMonitorInfo(IntPtr m, ref MI i);
    [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr h);
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(IntPtr h, int a, ref Rect r, int s);

    public static NB Ext(IntPtr h)
    {
        Rect r = new Rect();
        DwmGetWindowAttribute(h, DwmExt, ref r, Marshal.SizeOf(typeof(Rect)));
        return new NB { L = r.L, T = r.T, W = r.R - r.L, H = r.B - r.T };
    }

    public static NB Mon(IntPtr h)
    {
        IntPtr m = MonitorFromWindow(h, MonNear);
        MI i = new MI { Size = Marshal.SizeOf(typeof(MI)) };
        GetMonitorInfo(m, ref i);
        return new NB { L = i.Monitor.L, T = i.Monitor.T, W = i.Monitor.R - i.Monitor.L, H = i.Monitor.B - i.Monitor.T };
    }

    public static NB Work(IntPtr h)
    {
        IntPtr m = MonitorFromWindow(h, MonNear);
        MI i = new MI { Size = Marshal.SizeOf(typeof(MI)) };
        GetMonitorInfo(m, ref i);
        return new NB { L = i.Work.L, T = i.Work.T, W = i.Work.R - i.Work.L, H = i.Work.B - i.Work.T };
    }

    public static NB ClientScreen(IntPtr h)
    {
        Rect c;
        GetClientRect(h, out c);
        Pt p = new Pt();
        p.X = c.L;
        p.Y = c.T;
        ClientToScreen(h, ref p);
        NB result = new NB();
        result.L = p.X;
        result.T = p.Y;
        result.W = c.R - c.L;
        result.H = c.B - c.T;
        return result;
    }

    public static bool SetExt(IntPtr h, NB t, int n)
    {
        for (int i = 0; i < n; i++)
        {
            NB c = Ext(h);
            if (c.W == t.W && c.H == t.H && c.L == t.L && c.T == t.T) return true;
            Rect w;
            GetWindowRect(h, out w);
            int dw = t.W - c.W, dh = t.H - c.H, dl = t.L - c.L, dt = t.T - c.T;
            SetWindowPos(h, IntPtr.Zero, w.L + dl, w.T + dt, (w.R - w.L) + dw, (w.B - w.T) + dh, SwpNoZ | SwpNoA);
        }
        NB f = Ext(h);
        return f.W == t.W && f.H == t.H && f.L == t.L && f.T == t.T;
    }

    public static IntPtr Wait(Process p, int ms)
    {
        int d = Environment.TickCount + ms;
        while (Environment.TickCount < d)
        {
            p.Refresh();
            if (p.MainWindowHandle != IntPtr.Zero) return p.MainWindowHandle;
            System.Threading.Thread.Sleep(200);
        }
        throw new TimeoutException("Main window not available.");
    }

    public static void Focus(IntPtr h)
    {
        ShowWindow(h, 9);
        SetForegroundWindow(h);
        System.Threading.Thread.Sleep(400);
    }

    public static NB PrintCap(IntPtr h, string path)
    {
        Focus(h);
        NB b = Ext(h);
        using (Bitmap bmp = new Bitmap(b.W, b.H))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            IntPtr dc = g.GetHdc();
            PrintWindow(h, dc, 2);
            g.ReleaseHdc(dc);
            bmp.Save(path, ImageFormat.Png);
        }
        return b;
    }

    public static NB ScreenCap(NB mon, string path)
    {
        using (Bitmap bmp = new Bitmap(mon.W, mon.H))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(mon.L, mon.T, 0, 0, new Size(mon.W, mon.H));
            bmp.Save(path, ImageFormat.Png);
        }
        return mon;
    }

    public static string Ihdr(string path)
    {
        byte[] b = System.IO.File.ReadAllBytes(path);
        int w = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
        int h = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
        return w + "x" + h + " bytes=" + b.Length;
    }
}
'@

Add-Type @'
using System; using System.Runtime.InteropServices;
public static class FcKeys {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
  public static void Key(IntPtr h,byte vk){ SetForegroundWindow(h); System.Threading.Thread.Sleep(200); const uint dn=0x0100,up=0x0101; PostMessage(h,dn,(IntPtr)vk,IntPtr.Zero); PostMessage(h,up,(IntPtr)vk,IntPtr.Zero);} 
}
'@

function Invoke-PixelVerifier([string]$path, [int]$mw, [int]$mh, [switch]$ExpectReject) {
  $args = @($path, $mw, $mh)
  if ($ExpectReject) { $args += "--expect-reject" }
  & node (Join-Path $repoRoot "apps\desktop\scripts\verify-fullscreen-png-pixels.mjs") @args
  if ($LASTEXITCODE -ne 0 -and -not $ExpectReject) { throw "Pixel verifier failed for $path" }
  if ($LASTEXITCODE -ne 0 -and $ExpectReject -and $LASTEXITCODE -ne 0) { return }
}

function Report-Shot([string]$label, [string]$path, [FcCapture+NB]$win, [FcCapture+NB]$client, [FcCapture+NB]$mon, [uint32]$dpi, [string]$method, [string]$webviewBounds) {
  Write-Host $label
  Write-Host "  path: $path"
  Write-Host "  IHDR: $([FcCapture]::Ihdr($path))"
  Write-Host "  method: $method"
  Write-Host "  window bounds (DWM ext): $win"
  Write-Host "  client bounds (screen): $client"
  Write-Host "  WebView bounds: $webviewBounds"
  Write-Host "  monitor bounds: $mon"
  Write-Host "  dpi: $dpi ($([math]::Round($dpi/96.0,4))x)"
  Write-Host "  crop/resize/composition: NO"
}

Get-Process CrevaFactOS.Desktop -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep 1
$api = Get-NetTCPConnection -LocalPort 5000 -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
$web = Get-NetTCPConnection -LocalPort 3001 -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $api) { throw "API not listening on port 5000" }
if (-not $web) { throw "Web not listening on port 3001" }
Invoke-WebRequest -Uri "http://127.0.0.1:5000/health/ready" -UseBasicParsing -TimeoutSec 5 | Out-Null
Invoke-WebRequest -Uri "http://127.0.0.1:3001/giris" -UseBasicParsing -TimeoutSec 5 | Out-Null
$env:CREVAFACTOS_DESKTOP_WEB_URL = "http://127.0.0.1:3001"
$p = Start-Process -FilePath $desktopExe -PassThru
$h = [FcCapture]::Wait($p,45000)
$loadDeadline = (Get-Date).AddSeconds(60)
do {
  Start-Sleep -Seconds 2
  [FcCapture]::Focus($h)
  $probePath = Join-Path $env:TEMP "crevafactos-load-probe.png"
  [void][FcCapture]::PrintCap($h, $probePath)
  $probeSize = (Get-Item $probePath).Length
  Write-Host "Load probe PNG bytes: $probeSize"
} while ((Get-Date) -lt $loadDeadline -and $probeSize -lt 80000)
if ($probeSize -lt 80000) { throw "CrevaFactOS /giris did not render before timeout (probe bytes=$probeSize)" }
Remove-Item $probePath -ErrorAction SilentlyContinue
$mon = [FcCapture]::Mon($h)
$dpi = [FcCapture]::GetDpiForWindow($h)
Write-Host "Monitor: $mon WorkingArea: $([FcCapture]::Work($h)) DPI: $dpi"
[FcCapture]::Focus($h)
Start-Sleep -Seconds 3
[void][FcCapture]::SetExt($h, (New-Object FcCapture+NB -Property @{L=50;T=50;W=1600;H=1000}), 20)
Start-Sleep 1

$normalWin = [FcCapture]::Ext($h)
$normalClient = [FcCapture]::ClientScreen($h)
$normalPath = Join-Path $shotDir "01-normal-regression-1600x1000.png"
$normalWin = [FcCapture]::PrintCap($h, $normalPath)
& node (Join-Path $repoRoot "apps\desktop\scripts\verify-desktop-shell-1-png-dimensions.mjs") $normalPath 1600 1000 | Write-Host
if ($LASTEXITCODE -ne 0) { throw "Normal PNG dimension check failed" }
Report-Shot "Normal regression" $normalPath $normalWin $normalClient $mon $dpi "PrintWindow" "fills client area below menu"

Write-Host "=== Rejected screenshot pixel audit (expect FAIL) ==="
Invoke-PixelVerifier $rejected $mon.W $mon.H -ExpectReject

[FcCapture]::Focus($h)
for ($i=0; $i -lt 6; $i++) { [FcKeys]::Key($h,0x7A); Start-Sleep -Seconds 1; $fs=[FcCapture]::Ext($h); if ($fs.W -eq $mon.W -and $fs.H -eq $mon.H) { break } }
Start-Sleep -Seconds 1
$fsWin = [FcCapture]::Ext($h)
$fsClient = [FcCapture]::ClientScreen($h)
if ($fsWin.W -ne $mon.W -or $fsWin.H -ne $mon.H) { throw "Fullscreen window bounds $fsWin != monitor $mon" }
if ($fsClient.L -ne $mon.L -or $fsClient.T -ne $mon.T) { throw "Fullscreen client origin $($fsClient.L),$($fsClient.T) != monitor origin" }
if ($fsClient.W -ne $mon.W -or $fsClient.H -ne $mon.H) { throw "Fullscreen client size $fsClient != monitor $mon" }

$fsPath = Join-Path $shotDir "02-fullscreen-edge-to-edge.png"
[FcCapture]::Focus($h)
Start-Sleep -Milliseconds 800
$fsCap = [FcCapture]::PrintCap($h, $fsPath)
Report-Shot "Corrected fullscreen" $fsPath $fsWin $fsClient $mon $dpi "PrintWindow (native window, no post-capture resize)" "fills fullscreen client ($fsClient)"

Write-Host "=== New fullscreen pixel audit (expect PASS) ==="
Invoke-PixelVerifier $fsPath $mon.W $mon.H

[FcCapture]::Focus($h)
[FcKeys]::Key($h,0x1B)
Start-Sleep -Seconds 2
$restWin = [FcCapture]::Ext($h)
if ($restWin.W -ne 1600 -or $restWin.H -ne 1000) {
  [void][FcCapture]::SetExt($h, (New-Object FcCapture+NB -Property @{L=50;T=50;W=1600;H=1000}), 20)
  Start-Sleep -Milliseconds 500
  $restWin = [FcCapture]::Ext($h)
}
$restClient = [FcCapture]::ClientScreen($h)
$restPath = Join-Path $shotDir "03-restored-regression-1600x1000.png"
$restWin = [FcCapture]::PrintCap($h, $restPath)
& node (Join-Path $repoRoot "apps\desktop\scripts\verify-desktop-shell-1-png-dimensions.mjs") $restPath 1600 1000 | Write-Host
if ($LASTEXITCODE -ne 0) { throw "Restored PNG dimension check failed" }
Report-Shot "Restored normal" $restPath $restWin $restClient $mon $dpi "PrintWindow" "fills client area below menu"

$dup = Start-Process -FilePath $desktopExe -PassThru
Start-Sleep -Seconds 2
$count = (Get-Process -Name CrevaFactOS.Desktop -ErrorAction SilentlyContinue).Count
Write-Host "Process/window count after duplicate launch: $count (duplicate PID $($dup.Id))"
if ($count -ne 1) { throw "Expected one desktop instance" }
if (-not $dup.HasExited) { throw "Duplicate process did not exit" }

Stop-Process -Id $p.Id -Force
Write-Host "Fullscreen visual closure validation complete."
