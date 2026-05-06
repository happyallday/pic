using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Pic.Shared;

namespace Pic.Capture;

public class ScreenCapture
{
    public Bitmap CaptureFullScreen()
    {
        var bounds = GetVirtualScreenBounds();
        return CaptureRectangle(bounds);
    }

    public Bitmap CaptureActiveWindow()
    {
        var hWnd = NativeMethods.GetForegroundWindow();
        return CaptureWindow(hWnd);
    }

    public Bitmap CaptureWindow(IntPtr hWnd)
    {
        if (!NativeMethods.GetWindowRect(hWnd, out var rect))
            return CaptureFullScreen();

        var isMinimized = ((ulong)NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_STYLE) & NativeMethods.WS_MINIMIZE) != 0;
        if (isMinimized)
        {
            NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
            System.Threading.Thread.Sleep(100);
            NativeMethods.SetForegroundWindow(hWnd);
            System.Threading.Thread.Sleep(100);
            NativeMethods.GetWindowRect(hWnd, out rect);
        }

        var bitmap = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bitmap);
        var hdcSrc = NativeMethods.GetDC(IntPtr.Zero);
        var hdcDest = g.GetHdc();

        try
        {
            NativeMethods.BitBlt(hdcDest, 0, 0, rect.Width, rect.Height,
                hdcSrc, rect.Left, rect.Top, NativeMethods.SRCCOPY | NativeMethods.CAPTUREBLT);
        }
        finally
        {
            g.ReleaseHdc(hdcDest);
            NativeMethods.ReleaseDC(IntPtr.Zero, hdcSrc);
        }

        return bitmap;
    }

    public Bitmap CaptureRectangle(Rectangle bounds)
    {
        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bitmap);
        var hdcSrc = NativeMethods.GetDC(IntPtr.Zero);
        var hdcDest = g.GetHdc();

        try
        {
            NativeMethods.BitBlt(hdcDest, 0, 0, bounds.Width, bounds.Height,
                hdcSrc, bounds.Left, bounds.Top, NativeMethods.SRCCOPY | NativeMethods.CAPTUREBLT);
        }
        finally
        {
            g.ReleaseHdc(hdcDest);
            NativeMethods.ReleaseDC(IntPtr.Zero, hdcSrc);
        }

        return bitmap;
    }

    public static Rectangle GetVirtualScreenBounds()
    {
        return new Rectangle(
            (int)SystemParameters.VirtualScreenLeft,
            (int)SystemParameters.VirtualScreenTop,
            (int)SystemParameters.VirtualScreenWidth,
            (int)SystemParameters.VirtualScreenHeight);
    }

    public static List<WindowInfo> GetVisibleWindows()
    {
        var windows = new List<WindowInfo>();
        NativeMethods.EnumWindows((hWnd, lParam) =>
        {
            if (!NativeMethods.IsWindowVisible(hWnd)) return true;
            var title = GetWindowTitle(hWnd);
            if (string.IsNullOrWhiteSpace(title)) return true;
            if (!NativeMethods.GetWindowRect(hWnd, out var rect)) return true;
            if (rect.Width <= 0 || rect.Height <= 0) return true;

            var exStyle = (ulong)NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_EXSTYLE);
            var isToolWindow = (exStyle & NativeMethods.WS_EX_TOOLWINDOW) != 0;
            var isAppWindow = (exStyle & NativeMethods.WS_EX_APPWINDOW) != 0;
            var owner = NativeMethods.GetWindow(hWnd, NativeMethods.GW_OWNER);

            if (isToolWindow && !isAppWindow) return true;
            if (owner != IntPtr.Zero && !isAppWindow) return true;

            windows.Add(new WindowInfo
            {
                Handle = hWnd,
                Title = title,
                Bounds = rect.ToRectangle()
            });
            return true;
        }, IntPtr.Zero);

        return windows;
    }

    private static string GetWindowTitle(IntPtr hWnd)
    {
        var length = NativeMethods.GetWindowText(hWnd, 0, 0);
        if (length <= 0) return string.Empty;
        var lpString = Marshal.AllocHGlobal((length + 1) * sizeof(char));
        try
        {
            NativeMethods.GetWindowText(hWnd, lpString, length + 1);
            return Marshal.PtrToStringAuto(lpString) ?? string.Empty;
        }
        finally
        {
            Marshal.FreeHGlobal(lpString);
        }
    }

    public static System.Drawing.Imaging.ImageFormat GetImageFormat(Pic.Shared.ImageFormat format)
    {
        return format switch
        {
            Pic.Shared.ImageFormat.Jpeg => System.Drawing.Imaging.ImageFormat.Jpeg,
            Pic.Shared.ImageFormat.Bmp => System.Drawing.Imaging.ImageFormat.Bmp,
            Pic.Shared.ImageFormat.Gif => System.Drawing.Imaging.ImageFormat.Gif,
            _ => System.Drawing.Imaging.ImageFormat.Png
        };
    }
}

public class WindowInfo
{
    public IntPtr Handle { get; set; }
    public string Title { get; set; } = string.Empty;
    public Rectangle Bounds { get; set; }
}

public static class SystemParameters
{
    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    public static int VirtualScreenLeft => GetSystemMetrics(SM_XVIRTUALSCREEN);
    public static int VirtualScreenTop => GetSystemMetrics(SM_YVIRTUALSCREEN);
    public static int VirtualScreenWidth => GetSystemMetrics(SM_CXVIRTUALSCREEN);
    public static int VirtualScreenHeight => GetSystemMetrics(SM_CYVIRTUALSCREEN);
}
