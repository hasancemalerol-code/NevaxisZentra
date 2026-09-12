using System.Runtime.InteropServices;

namespace NevaxisFactOS.Desktop.Services;

public readonly record struct NativeWindowBounds(int Left, int Top, int Width, int Height)
{
    public int Right => Left + Width;

    public int Bottom => Top + Height;

    public bool Matches(NativeWindowBounds other) =>
        Left == other.Left
        && Top == other.Top
        && Width == other.Width
        && Height == other.Height;
}

public static class NativeWindowInterop
{
    private const int DwmwaExtendedFrameBounds = 9;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint MonitorDefaultToNearest = 2;
    private const int MonitorDefaultToNull = 0;

    public static NativeWindowBounds GetExtendedFrameBounds(IntPtr windowHandle)
    {
        var rect = new Rect();
        _ = DwmGetWindowAttribute(
            windowHandle,
            DwmwaExtendedFrameBounds,
            ref rect,
            Marshal.SizeOf<Rect>());

        return new NativeWindowBounds(
            rect.Left,
            rect.Top,
            rect.Right - rect.Left,
            rect.Bottom - rect.Top);
    }

    public static NativeWindowBounds GetMonitorBounds(IntPtr windowHandle)
    {
        var monitor = MonitorFromWindow(windowHandle, MonitorDefaultToNearest);
        var info = new MonitorInfo
        {
            Size = Marshal.SizeOf<MonitorInfo>(),
        };

        if (!GetMonitorInfo(monitor, ref info))
        {
            throw new InvalidOperationException("Unable to resolve monitor bounds.");
        }

        return new NativeWindowBounds(
            info.Monitor.Left,
            info.Monitor.Top,
            info.Monitor.Right - info.Monitor.Left,
            info.Monitor.Bottom - info.Monitor.Top);
    }

    public static uint GetWindowDpi(IntPtr windowHandle) =>
        GetDpiForWindow(windowHandle);

    public static bool TrySetExactExtendedFrameBounds(
        IntPtr windowHandle,
        NativeWindowBounds target,
        int maxAttempts = 16)
    {
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var current = GetExtendedFrameBounds(windowHandle);
            if (current.Matches(target))
            {
                return true;
            }

            GetWindowRect(windowHandle, out var windowRect);
            var deltaWidth = target.Width - current.Width;
            var deltaHeight = target.Height - current.Height;
            var deltaLeft = target.Left - current.Left;
            var deltaTop = target.Top - current.Top;

            SetWindowPos(
                windowHandle,
                IntPtr.Zero,
                windowRect.Left + deltaLeft,
                windowRect.Top + deltaTop,
                (windowRect.Right - windowRect.Left) + deltaWidth,
                (windowRect.Bottom - windowRect.Top) + deltaHeight,
                SwpNoZOrder | SwpNoActivate);
        }

        return GetExtendedFrameBounds(windowHandle).Matches(target);
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(
        IntPtr hwnd,
        int attribute,
        ref Rect rect,
        int attributeSize);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfo
    {
        public int Size;
        public Rect Monitor;
        public Rect Work;
        public int Flags;
    }
}
