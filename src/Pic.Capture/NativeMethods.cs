using System.Runtime.InteropServices;

namespace Pic.Capture;

public static partial class NativeMethods
{
    private const string User32 = "user32.dll";
    private const string Gdi32 = "gdi32.dll";

    [LibraryImport(User32)]
    internal static partial IntPtr GetDesktopWindow();

    [LibraryImport(User32)]
    internal static partial IntPtr GetDC(IntPtr hWnd);

    [LibraryImport(User32)]
    internal static partial int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [LibraryImport(User32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [LibraryImport(User32)]
    public static partial IntPtr GetForegroundWindow();

    [LibraryImport(User32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [LibraryImport(User32, SetLastError = true, EntryPoint = "GetWindowTextW")]
    internal static partial int GetWindowText(IntPtr hWnd, nint lpString, int nMaxCount);

    [LibraryImport(User32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsWindowVisible(IntPtr hWnd);

    [LibraryImport(User32)]
    internal static partial IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [LibraryImport(User32, EntryPoint = "GetWindowLongW")]
    internal static partial int GetWindowLong(IntPtr hWnd, int nIndex);

    [LibraryImport(User32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetForegroundWindow(IntPtr hWnd);

    [LibraryImport(User32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [LibraryImport(Gdi32)]
    internal static partial IntPtr CreateCompatibleDC(IntPtr hDC);

    [LibraryImport(Gdi32)]
    internal static partial IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

    [LibraryImport(Gdi32)]
    internal static partial IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

    [LibraryImport(Gdi32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool BitBlt(IntPtr hDestDC, int x, int y, int nWidth, int nHeight,
        IntPtr hSrcDC, int xSrc, int ySrc, uint dwRop);

    [LibraryImport(Gdi32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DeleteDC(IntPtr hDC);

    [LibraryImport(Gdi32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DeleteObject(IntPtr hObject);

    [LibraryImport(Gdi32)]
    internal static partial IntPtr GetCurrentObject(IntPtr hDC, uint uObjectType);

    internal const uint SRCCOPY = 0x00CC0020;
    internal const uint CAPTUREBLT = 0x40000000;
    internal const uint OBJ_BITMAP = 7;

    internal const int GWL_EXSTYLE = -20;
    internal const int GWL_STYLE = -16;
    internal const ulong WS_EX_TOOLWINDOW = 0x00000080;
    internal const ulong WS_EX_APPWINDOW = 0x00040000;
    internal const ulong WS_EX_OVERLAPPEDWINDOW = 0x00000300;
    internal const ulong WS_VISIBLE = 0x10000000;
    internal const ulong WS_MINIMIZE = 0x20000000;
    internal const uint GW_OWNER = 4;
    internal const uint GW_HWNDNEXT = 2;
    internal const int SW_SHOW = 5;
    internal const int SW_RESTORE = 9;

    internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width => Right - Left;
        public int Height => Bottom - Top;
        public System.Drawing.Rectangle ToRectangle() =>
            new(Left, Top, Width, Height);
    }

    [LibraryImport(User32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int X;
        public int Y;
    }

    internal const int VK_SNAPSHOT = 0x2C;
    internal const int WH_KEYBOARD_LL = 13;
    internal const int WM_KEYDOWN = 0x0100;
    internal const int WM_SYSKEYDOWN = 0x0104;
}
