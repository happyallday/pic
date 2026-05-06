using System.Drawing;
using System.Drawing.Imaging;

namespace Pic.Capture;

public class ScrollingCapture
{
    public Bitmap? CaptureScroll(IntPtr hWnd, int maxScrolls = 20)
    {
        if (!NativeMethods.GetWindowRect(hWnd, out var winRect))
            return null;
        if (winRect.Width <= 0 || winRect.Height <= 0)
            return null;

        GetClientRect(hWnd, out var clientRect);

        var pt = new NativeMethods.POINT { X = 0, Y = 0 };
        NativeMethods.ClientToScreen(hWnd, ref pt);

        var capRect = new Rectangle(pt.X, pt.Y, clientRect.Width, clientRect.Height);
        if (capRect.Width <= 0 || capRect.Height <= 0)
            return null;

        NativeMethods.SetForegroundWindow(hWnd);
        System.Threading.Thread.Sleep(200);

        var images = new List<Bitmap>();

        for (var i = 0; i < maxScrolls; i++)
        {
            var shot = CaptureClientArea(capRect);
            if (shot == null) break;
            images.Add(shot);

            SendScrollDown(hWnd);
            System.Threading.Thread.Sleep(250);

            if (i == 0 && i < maxScrolls - 1)
            {
                var checkShot = CaptureClientArea(capRect);
                if (checkShot != null)
                {
                    if (IsContentSame(shot, checkShot, 0.98))
                    {
                        checkShot.Dispose();
                        break;
                    }
                    checkShot.Dispose();
                }
            }
        }

        if (images.Count == 0) return null;
        if (images.Count == 1) return images[0];

        return StitchBitmaps(images);
    }

    private static Bitmap? CaptureClientArea(Rectangle clientScreenRect)
    {
        var hdcSrc = NativeMethods.GetDC(IntPtr.Zero);
        try
        {
            var shot = new Bitmap(clientScreenRect.Width, clientScreenRect.Height,
                PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(shot);
            var hdcDest = g.GetHdc();
            try
            {
                NativeMethods.BitBlt(hdcDest, 0, 0, clientScreenRect.Width, clientScreenRect.Height,
                    hdcSrc, clientScreenRect.Left, clientScreenRect.Top,
                    NativeMethods.SRCCOPY | NativeMethods.CAPTUREBLT);
            }
            finally
            {
                g.ReleaseHdc(hdcDest);
            }
            return shot;
        }
        catch
        {
            return null;
        }
        finally
        {
            NativeMethods.ReleaseDC(IntPtr.Zero, hdcSrc);
        }
    }

    private static bool IsContentSame(Bitmap a, Bitmap b, double threshold)
    {
        if (a.Width != b.Width || a.Height != b.Height) return false;

        var rect = new Rectangle(0, 0, a.Width, a.Height);
        var dataA = a.LockBits(rect, ImageLockMode.ReadOnly, a.PixelFormat);
        var dataB = b.LockBits(rect, ImageLockMode.ReadOnly, b.PixelFormat);

        try
        {
            var stride = dataA.Stride;
            var bytes = stride * a.Height;
            var bufA = new byte[bytes];
            var bufB = new byte[bytes];
            System.Runtime.InteropServices.Marshal.Copy(dataA.Scan0, bufA, 0, bytes);
            System.Runtime.InteropServices.Marshal.Copy(dataB.Scan0, bufB, 0, bytes);

            var sameCount = 0;
            for (var i = 0; i < bytes; i++)
            {
                if (bufA[i] == bufB[i]) sameCount++;
            }

            return (double)sameCount / bytes >= threshold;
        }
        finally
        {
            a.UnlockBits(dataA);
            b.UnlockBits(dataB);
        }
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

        images.ForEach(img => img.Dispose());

        return stitched;
    }

    private static void SendScrollDown(IntPtr hWnd)
    {
        // Send WM_MOUSEWHEEL (most apps respond to this)
        const int WM_MOUSEWHEEL = 0x020A;
        PostMessage(hWnd, WM_MOUSEWHEEL, (IntPtr)unchecked((int)0xFF880000), IntPtr.Zero);

        // Also send WM_VSCROLL as fallback (some apps need it)
        const int WM_VSCROLL = 0x0115;
        const int SB_LINEDOWN = 1;
        PostMessage(hWnd, WM_VSCROLL, (IntPtr)SB_LINEDOWN, IntPtr.Zero);
    }

    public static string GetWindowTitle(IntPtr hWnd)
    {
        var length = NativeMethods.GetWindowText(hWnd, 0, 0);
        if (length <= 0) return hWnd.ToString("X");
        var lpString = System.Runtime.InteropServices.Marshal.AllocHGlobal((length + 1) * sizeof(char));
        try
        {
            NativeMethods.GetWindowText(hWnd, lpString, length + 1);
            return System.Runtime.InteropServices.Marshal.PtrToStringAuto(lpString) ?? hWnd.ToString("X");
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.FreeHGlobal(lpString);
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetClientRect")]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "PostMessageW")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }
}
