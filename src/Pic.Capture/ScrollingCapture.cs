using System.Drawing;
using System.Drawing.Imaging;

namespace Pic.Capture;

public class ScrollingCapture
{
    public Bitmap? CaptureScroll(IntPtr hWnd, int maxScrolls = 20)
    {
        NativeMethods.GetWindowRect(hWnd, out var rect);
        if (rect.Width <= 0 || rect.Height <= 0)
            return null;

        var images = new List<Bitmap>();
        using var firstShot = new ScreenCapture().CaptureWindow(hWnd);
        if (firstShot == null) return null;
        images.Add(new Bitmap(firstShot));

        var clientHeight = GetClientHeight(hWnd);
        var scrollAmount = clientHeight > 0 ? clientHeight : rect.Height;
        var totalScrolled = scrollAmount;

        for (var i = 0; i < maxScrolls; i++)
        {
            SendScrollDown(hWnd);
            System.Threading.Thread.Sleep(300);

            var hdc = NativeMethods.GetDC(IntPtr.Zero);
            try
            {
                var shot = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
                using var g = Graphics.FromImage(shot);
                var hdcDest = g.GetHdc();
                try
                {
                    NativeMethods.BitBlt(hdcDest, 0, 0, rect.Width, rect.Height,
                        hdc, rect.Left, rect.Top, NativeMethods.SRCCOPY);
                }
                finally
                {
                    g.ReleaseHdc(hdcDest);
                }
                images.Add(shot);
            }
            finally
            {
                NativeMethods.ReleaseDC(IntPtr.Zero, hdc);
            }

            totalScrolled += scrollAmount;
            if (totalScrolled > 100000) break;
        }

        if (images.Count == 1)
            return images[0];

        return StitchBitmaps(images);
    }

    private static Bitmap StitchBitmaps(List<Bitmap> images)
    {
        var width = images[0].Width;
        var totalHeight = images.Sum(img => img.Height);

        var stitched = new Bitmap(width, totalHeight, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(stitched);
        var currentY = 0;

        foreach (var img in images)
        {
            g.DrawImage(img, 0, currentY);
            currentY += img.Height;
        }

        return stitched;
    }

    private static int GetClientHeight(IntPtr hWnd)
    {
        GetClientRect(hWnd, out var rect);
        return rect.Height;
    }

    private static void SendScrollDown(IntPtr hWnd)
    {
        const int WM_MOUSEWHEEL = 0x020A;
        const int WHEEL_DELTA = 120;
        var wParam = (IntPtr)((-WHEEL_DELTA) << 16);
        PostMessage(hWnd, WM_MOUSEWHEEL, wParam, IntPtr.Zero);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
        public int Height => Bottom - Top;
    }
}
